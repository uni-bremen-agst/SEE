using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizerTests.Execution.Semantic;
using Request = XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalysisDispatchRequest;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Production detector execution seam, not another selection or Worker implementation.</summary>
    public sealed class ExceptionFlowProductionPipelineTests
    {
        private const string Source = "public static class Fixture { public static void Root() { throw null; } }";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Current_OriginalFlowsAndSessionWithoutWorker(bool brokenEndpoint)
        {
            var fixture = Fixture();
            object graph = Graph(fixture.Input.Session);
            var dispatch = new ExceptionFlowAnalysisDispatch(new(brokenEndpoint ? Client(broken: true) : null));
            var expected = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(fixture.Input.Session.Analyze(fixture.Input.Member));
            for (int i = 0; i < 3; i++)
            {
                var result = await Analyze(new Request.CurrentCompilation(fixture.Compilation, fixture.Input), dispatch);
                Assert.True(result.Succeeded);
                Assert.Equal(ExceptionFlowAnalyzerSelection.Current, result.Decision!.Selection);
                Assert.Equal(expected, result.Result);
                Assert.NotNull(result.Routing!.CurrentAnalysis);
                Assert.Null(result.Routing.HistoricalAnalysis);
                Assert.Same(graph, Graph(fixture.Input.Session));
            }
        }

        [Fact]
        public async Task Current_UncertaintyIsRetainedRatherThanHistoricalCompleteness()
        {
            var fixture = Fixture("public static class Fixture { public static void Root() { System.Console.WriteLine(); } }");
            var native = fixture.Input.Session.Analyze(fixture.Input.Member);
            var result = await Analyze(new Request.CurrentCompilation(fixture.Compilation, fixture.Input), new(new(Client(broken: true))));
            Assert.True(result.Succeeded);
            Assert.Equal(RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(native), result.Result);
            Assert.True(result.Routing!.CurrentAnalysis!.HasUncertainPaths);
        }

        [Fact]
        public async Task Historical_RealValidatedInputCrossesAllStagesWithIsolationAndCleanup()
        {
            var fixture = Fixture();
            var before = new[] { typeof(Compilation).Assembly.ManifestModule.ModuleVersionId, typeof(CSharpCompilation).Assembly.ManifestModule.ModuleVersionId };
            var current = await Analyze(new Request.CurrentCompilation(fixture.Compilation, fixture.Input), new(new()));
            var provenance = HistoricalPdb();
            var result = await Analyze(new Request.PortablePdb(provenance, new Request.ExecutionPayload.Worker(Input())), new(new(Client())));
            Assert.True(result.Succeeded, result.Routing?.HistoricalFailure?.Message);
            Assert.True(result.Projection.Succeeded);
            Assert.Equal(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, result.Projection.Context!.Provenance);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, result.Decision!.Selection);
            Assert.Equal(current.Result, result.Result);
            Assert.Equal("NullReferenceException", Assert.Single(result.Result!.Entries).ExceptionType.MetadataName);
            Assert.Null(result.Routing!.CurrentAnalysis);
            AssertStopped(result);
            Assert.Equal(before, new[] { typeof(Compilation).Assembly.ManifestModule.ModuleVersionId, typeof(CSharpCompilation).Assembly.ManifestModule.ModuleVersionId });
            AssertNoHistoricalLoaded();
            string path = Path.Combine(Root(), "artifacts", "p5o2b9", "production-boundary.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
            {
                Entry = "XmlDocExceptionSemanticDetector.AnalyzeConfiguredExceptionFlowAsync",
                CurrentContext = current.Projection.Context,
                HistoricalContext = result.Projection.Context,
                HistoricalIdentity = result.Routing.HistoricalAnalysis!.Identity,
                HistoricalProcessId = result.Routing.HistoricalAnalysis.ProcessId,
                CurrentMvids = before,
                CanonicalParity = true,
                ProcessStopped = true,
                HistoricalLoadedInCaller = false,
                ActualFactoryReceipt = ExternalCompilationProvenanceDescriptorFactory.IsValidated(provenance),
                RealPdbValidation = provenance.PortablePdb.ValidationKind.ToString(),
                CanonicalResult = result.Result,
                HistoricalPeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(HistoricalPath()))),
                HistoricalPdbSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.ChangeExtension(HistoricalPath(), ".pdb"))))
            }, WorkerProtocol.JsonOptions));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MissingOrUnvalidatedProvenance_StopsBeforeDecisionAndExecution(bool copied)
        {
            ExternalCompilationProvenanceDescriptor? provenance = null;
            if (copied)
            {
                var valid = HistoricalPdb();
                provenance = new(valid.DebugDirectory, valid.PortablePdb, valid.CompilationOptions, valid.MetadataReferences);
            }
            var result = await Analyze(new Request.PortablePdb(provenance!, new Request.ExecutionPayload.Worker(Input())), new(null!));
            Assert.False(result.Succeeded);
            Assert.Equal(copied ? ExceptionFlowAnalyzerSelectionProjectionFailureCode.UnvalidatedProvenance : ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingInput, result.Projection.Failure!.Code);
            Assert.Null(result.Decision);
            Assert.Null(result.Routing);
            Assert.Null(result.Result);
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("5.0.0.0")]
        public async Task UnknownValidatedCompiler_StopsBeforeAnalyzer(string version)
        {
            // Existing validator's options-schema unit fixture, not a positive PE/source-equivalence proof.
            var result = await Analyze(new Request.PortablePdb(SchemaPdb(version), new Request.ExecutionPayload.Worker(Input())), new(null!));
            Assert.True(result.Projection.Succeeded);
            Assert.False(result.Succeeded);
            Assert.Equal(ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedCompilerVersion, result.Decision!.Failure!.Code);
            Assert.Null(result.Routing);
            Assert.Null(result.Result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ContradictoryPayload_IsNotRepairedOrExecuted(bool historical)
        {
            var fixture = Fixture();
            var currentPdb = ExternalCompilationProvenanceDescriptorFactoryTests.ReadRequiredDescriptor(
                ExternalCompilationProvenanceDescriptorFactoryTests.EmitPortablePdb("ProductionCurrentPdb", Source,
                    new CSharpParseOptions(LanguageVersion.CSharp12), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)));
            Request.ExecutionPayload payload = historical ? new Request.ExecutionPayload.CurrentSession(fixture.Input) : new Request.ExecutionPayload.Worker(Input());
            var result = await Analyze(new Request.PortablePdb(historical ? HistoricalPdb() : currentPdb, payload), new(new(Client(broken: true))));
            Assert.False(result.Succeeded);
            Assert.True(result.Decision!.Succeeded);
            Assert.Equal(ExceptionFlowAnalysisRoutingFailureCode.InvalidInput, result.Routing!.RoutingFailure!.Code);
            Assert.Null(result.Routing.CurrentAnalysis);
            Assert.Null(result.Routing.HistoricalAnalysis);
            Assert.Null(result.Result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WorkerFailure_PropagatesOriginalFailureWithoutCurrentFallback(bool startup)
        {
            var result = await Analyze(new Request.PortablePdb(HistoricalPdb(), new Request.ExecutionPayload.Worker(
                Input(startup ? Source : "public static class Fixture { public static void Root() { Missing(); } }"))), new(new(Client(broken: startup))));
            Assert.False(result.Succeeded);
            Assert.Equal(startup ? HistoricalWorkerClientFailureCode.StartFailure : HistoricalWorkerClientFailureCode.StructuredFailure, result.Routing!.HistoricalFailure!.Code);
            Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure);
            Assert.Null(result.Routing.CurrentAnalysis);
            Assert.Null(result.Result);
            if (!startup) { AssertStopped(result); }
            else { Assert.Null(result.Routing.HistoricalAnalysis.ProcessId); }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PreCancellation_PreservesTheSelectedBranchFailure(bool historical)
        {
            var fixture = Fixture();
            using CancellationTokenSource token = new();
            token.Cancel();
            Request request = historical ? new Request.PortablePdb(HistoricalPdb(), new Request.ExecutionPayload.Worker(Input()))
                : new Request.CurrentCompilation(fixture.Compilation, fixture.Input);
            var result = await Analyze(request, new(new(Client())), token.Token);
            Assert.False(result.Succeeded);
            Assert.Null(result.Result);
            Assert.Null(result.Routing!.CurrentAnalysis);
            if (historical)
            {
                Assert.Equal(HistoricalWorkerClientFailureCode.Cancelled, result.Routing.HistoricalFailure!.Code);
                Assert.Null(result.Routing.HistoricalAnalysis!.ProcessId);
            }
            else { Assert.Equal(ExceptionFlowAnalysisRoutingFailureCode.Cancelled, result.Routing.RoutingFailure!.Code); }
        }

        [Fact]
        public void Architecture_RealDetectorUsesDispatchWithoutDuplicatingLowerOwners()
        {
            string detector = File.ReadAllText(Path.Combine(Root(), "src", "XMLDocNormalizer", "Checks", "XmlDocExceptionSemanticDetector.cs"));
            Assert.Contains("AnalyzeConfiguredExceptionFlowAsync(", detector);
            Assert.Contains("new ExceptionFlowAnalysisDispatchRequest.CurrentCompilation(", detector);
            Assert.Contains("=> dispatch.AnalyzeAsync(request, cancellationToken)", detector);
            Assert.Contains("return dispatched.Routing.CurrentAnalysis;", detector);
            foreach (string forbidden in new[] { "session.Analyze(", "ExceptionFlowAnalyzerSelectionPolicy.Select", "Projector.Project", "HistoricalWorkerClient", "Process.Start", "Assembly.Load", "5.0.0-", "IsValidated", "new WorkerAnalysisInput" })
            { Assert.DoesNotContain(forbidden, detector); }
            AssertNoHistoricalLoaded();
        }

        private static Task<ExceptionFlowAnalysisDispatchResult> Analyze(Request request, ExceptionFlowAnalysisDispatch dispatch, CancellationToken token = default)
            => XmlDocExceptionSemanticDetector.AnalyzeConfiguredExceptionFlowAsync(request, dispatch, token);
        private static object Graph(ExceptionFlowSummaryAnalysisSession session) => typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session)!;
        private static WorkerAnalysisInput Input(string source = Source) => new(source, "Fixture", "Root", new());
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static string Configuration() => new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        private static HistoricalWorkerClient Client(bool broken = false) => new(broken ? Path.Combine(Root(), "artifacts", "p5o2b9", "never-start") : "dotnet",
            Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", Configuration(), "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(30));
        private static string HistoricalPath() => Path.Combine(Root(), "artifacts", "p5o2b7", "historical-pdb", "historical-input.dll");
        private static ExternalCompilationProvenanceDescriptor HistoricalPdb()
        {
            byte[] pe = File.ReadAllBytes(HistoricalPath());
            var reference = MetadataReference.CreateFromImage(ImmutableArray.CreateRange(pe));
            var consumer = CSharpCompilation.Create("ProductionMetadataConsumer", references: Helpers.MetadataReferences.Default.Append(reference), options: new(OutputKind.DynamicallyLinkedLibrary));
            var assembly = Assert.IsAssignableFrom<IAssemblySymbol>(consumer.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(consumer, assembly, out var expected));
            using MemoryStream stream = new(pe, writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(expected, stream, out var debug));
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(debug, [.. File.ReadAllBytes(Path.ChangeExtension(HistoricalPath(), ".pdb"))], out var descriptor));
            Assert.Equal(PortablePdbValidationKind.IdentityAndChecksum, descriptor.PortablePdb.ValidationKind);
            return descriptor;
        }
        private static ExternalCompilationProvenanceDescriptor SchemaPdb(string version)
        {
            MetadataBuilder metadata = new();
            int[] counts = new int[MetadataTokens.TableCount];
            counts[(int)TableIndex.Module] = 1;
            byte[] blob = Encoding.UTF8.GetBytes(string.Join("\0", new[] { "language", "C#", "version", "2", "compiler-version", version }) + "\0");
            metadata.AddCustomDebugInformation(MetadataTokens.EntityHandle(TableIndex.Module, 1), metadata.GetOrAddGuid(new("b5feec05-8cd0-4a83-96da-466284bb4bd8")), metadata.GetOrAddBlob(blob));
            BlobBuilder image = new();
            new PortablePdbBuilder(metadata, [.. counts], entryPoint: default).Serialize(image);
            byte[] bytes = image.ToArray();
            using var reader = MetadataReaderProvider.FromPortablePdbImage([.. bytes]);
            BlobContentId id = new(reader.GetMetadataReader().DebugMetadataHeader!.Id);
            var expected = new ExternalPeDebugDirectoryDescriptor(new ExternalModuleIdentity("production-schema-fixture.dll", Guid.Empty), false, [], [id], []);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(expected, [.. bytes], out var descriptor));
            return descriptor;
        }
        private static (CSharpCompilation Compilation, CurrentExceptionFlowAnalysisInput Input) Fixture(string source = Source)
        {
            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "worker-input.cs");
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
                .Select(path => MetadataReference.CreateFromFile(path)).ToArray();
            Assert.Equal(7, references.Length);
            var compilation = CSharpCompilation.Create("HistoricalWorkerInput", [tree], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(new(context));
            return (compilation, new(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root"), session));
        }
        private static void AssertStopped(ExceptionFlowAnalysisDispatchResult result)
        {
            int processId = result.Routing!.HistoricalAnalysis!.ProcessId!.Value;
            Assert.NotEqual(Environment.ProcessId, processId);
            try { using var process = Process.GetProcessById(processId); Assert.True(process.HasExited); }
            catch (ArgumentException) { }
        }
        private static void AssertNoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
            assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
            || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
    }
}
