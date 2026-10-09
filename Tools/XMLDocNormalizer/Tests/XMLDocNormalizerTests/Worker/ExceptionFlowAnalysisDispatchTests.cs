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
    /// <summary>Real B7 -> B6 -> B5 composition, keeping all earlier boundary tests independent.</summary>
    public sealed class ExceptionFlowAnalysisDispatchTests
    {
        private const string Source = "public static class Fixture { public static void Root() { throw null; } }";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NativeCurrent_RetainsExistingSessionWithoutStartingWorker(bool unavailableWorker)
        {
            Fixture fixture = CreateFixture();
            ExceptionFlowAnalysisDispatch dispatch = new(new(unavailableWorker ? BrokenClient() : null));
            object graph = typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(fixture.Session)!;
            for (int i = 0; i < 3; i++)
            {
                var result = await dispatch.AnalyzeAsync(new Request.CurrentCompilation(fixture.Compilation, fixture.Input));
                AssertSuccess(result, ExceptionFlowAnalyzerSelection.Current);
                Assert.Equal(ExceptionFlowCompilerProvenance.CurrentCompilation, result.Projection.Context!.Provenance);
                Assert.Equal(Direct(fixture), result.Result);
                Assert.NotNull(result.Routing!.CurrentAnalysis);
                Assert.Null(result.Routing.HistoricalAnalysis);
                Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(fixture.Session));
            }
        }

        [Fact]
        public async Task RealCurrentPdb_SelectsAndRunsExistingCurrentSession()
        {
            Fixture fixture = CreateFixture();
            var result = await new ExceptionFlowAnalysisDispatch(new(BrokenClient())).AnalyzeAsync(
                new Request.PortablePdb(CurrentPdb(), new Request.ExecutionPayload.CurrentSession(fixture.Input)));
            AssertSuccess(result, ExceptionFlowAnalyzerSelection.Current);
            Assert.Equal(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, result.Projection.Context!.Provenance);
            Assert.Equal(Direct(fixture), result.Result);
            Assert.Null(result.Routing!.HistoricalAnalysis);
        }

        [Fact]
        public async Task RealHistoricalPdb_DispatchesWorkerWithParityDeterminismIsolationAndCleanup()
        {
            Fixture fixture = CreateFixture();
            Assembly common = typeof(Compilation).Assembly;
            Assembly csharp = typeof(CSharpCompilation).Assembly;
            Guid beforeCommon = common.ManifestModule.ModuleVersionId;
            Guid beforeCsharp = csharp.ManifestModule.ModuleVersionId;
            ExceptionFlowAnalysisDispatch dispatch = new(new(Client()));
            var current = await dispatch.AnalyzeAsync(new Request.CurrentCompilation(fixture.Compilation, fixture.Input));
            ExternalCompilationProvenanceDescriptor provenance = HistoricalPdb();
            Request request = new Request.PortablePdb(provenance, new Request.ExecutionPayload.Worker(Input()));
            var first = await dispatch.AnalyzeAsync(request);
            var second = await dispatch.AnalyzeAsync(request);
            AssertSuccess(current, ExceptionFlowAnalyzerSelection.Current);
            AssertSuccess(first, ExceptionFlowAnalyzerSelection.Historical);
            AssertSuccess(second, ExceptionFlowAnalyzerSelection.Historical);
            Assert.Equal(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, first.Projection.Context!.Provenance);
            Assert.Equal(Direct(fixture), first.Result);
            Assert.Equal(current.Result, first.Result);
            Assert.Equal(first.Result, second.Result);
            Assert.Equal("NullReferenceException", Assert.Single(first.Result!.Entries).ExceptionType.MetadataName);
            Assert.Null(first.Routing!.CurrentAnalysis);
            AssertStopped(first.Routing.HistoricalAnalysis!);
            AssertStopped(second.Routing!.HistoricalAnalysis!);
            Assert.NotEqual(first.Routing.HistoricalAnalysis!.ProcessId, second.Routing.HistoricalAnalysis!.ProcessId);
            Assert.Equal(beforeCommon, common.ManifestModule.ModuleVersionId);
            Assert.Equal(beforeCsharp, csharp.ManifestModule.ModuleVersionId);
            AssertNoHistoricalLoaded();
            string path = Path.Combine(Root(), "artifacts", "p5o2b8", "dispatch-boundary.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
            {
                CurrentContext = current.Projection.Context,
                HistoricalContext = first.Projection.Context,
                CurrentSelection = current.Decision!.Selection.ToString(),
                HistoricalSelection = first.Decision!.Selection.ToString(),
                HistoricalIdentity = first.Routing.HistoricalAnalysis.Identity,
                HistoricalProcessIds = new[] { first.Routing.HistoricalAnalysis.ProcessId, second.Routing.HistoricalAnalysis.ProcessId },
                CurrentMvids = new[] { beforeCommon, beforeCsharp },
                CanonicalParity = true,
                HistoricalDeterministic = true,
                HistoricalLoadedInCaller = false,
                ProcessesStopped = true,
                RealPdbValidation = provenance.PortablePdb.ValidationKind.ToString(),
                ActualFactoryReceipt = ExternalCompilationProvenanceDescriptorFactory.IsValidated(provenance),
                CanonicalResult = first.Result,
                HistoricalPeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(HistoricalPath()))),
                HistoricalPdbSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.ChangeExtension(HistoricalPath(), ".pdb"))))
            }, WorkerProtocol.JsonOptions));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task MissingProvenance_StopsBeforeSelectionAndRouter(int scenario)
        {
            Fixture fixture = CreateFixture();
            Request? request = scenario switch
            {
                0 => null,
                1 => new Request.CurrentCompilation(null!, fixture.Input),
                _ => new Request.PortablePdb(null!, new Request.ExecutionPayload.Worker(Input()))
            };
            var result = await new ExceptionFlowAnalysisDispatch(null!).AnalyzeAsync(request);
            AssertProjectionFailure(result, ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingInput);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RawOrCopiedProvenance_StopsBeforeSelectionAndRouter(bool rawVersion)
        {
            var valid = CurrentPdb();
            ExternalCompilationOptionsDescriptor options = rawVersion
                ? new([new("language", "C#"), new("version", "2"), new("compiler-version", ExceptionFlowAnalyzerSelectionPolicy.HistoricalCompilerVersion)])
                : valid.CompilationOptions!;
            var copy = new ExternalCompilationProvenanceDescriptor(valid.DebugDirectory, valid.PortablePdb, options, valid.MetadataReferences);
            var result = await new ExceptionFlowAnalysisDispatch(null!).AnalyzeAsync(new Request.PortablePdb(copy, new Request.ExecutionPayload.Worker(Input())));
            AssertProjectionFailure(result, ExceptionFlowAnalyzerSelectionProjectionFailureCode.UnvalidatedProvenance);
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("5.0.0.0")]
        public async Task ValidatedUnknownIdentity_StopsAtSelectionWithoutCallingRouter(string version)
        {
            var result = await new ExceptionFlowAnalysisDispatch(null!).AnalyzeAsync(new Request.PortablePdb(
                SchemaPdb(version), new Request.ExecutionPayload.Worker(Input())));
            Assert.True(result.Projection.Succeeded);
            Assert.False(result.Succeeded);
            Assert.Equal(version, result.Projection.Context!.CompilerVersion);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Unspecified, result.Decision!.Selection);
            Assert.Equal(ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedCompilerVersion, result.Decision.Failure!.Code);
            Assert.Null(result.Routing);
            Assert.Null(result.Result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ContradictoryEngineAndPayload_IsRejectedWithoutRepairOrFallback(bool historical)
        {
            Fixture fixture = CreateFixture();
            Request.ExecutionPayload payload = historical ? new Request.ExecutionPayload.CurrentSession(fixture.Input) : new Request.ExecutionPayload.Worker(Input());
            var result = await new ExceptionFlowAnalysisDispatch(new(BrokenClient())).AnalyzeAsync(new Request.PortablePdb(historical ? HistoricalPdb() : CurrentPdb(), payload));
            Assert.True(result.Projection.Succeeded);
            Assert.True(result.Decision!.Succeeded);
            AssertRoutingFailure(result, ExceptionFlowAnalysisRoutingFailureCode.InvalidInput);
            Assert.Null(result.Routing!.HistoricalAnalysis);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task MissingCurrentOrPayloadData_RetainsRouterInvalidInput(int scenario)
        {
            Fixture fixture = CreateFixture();
            var input = scenario switch { 0 => null!, 1 => fixture.Input with { Member = null! }, _ => fixture.Input with { Session = null! } };
            Request request = scenario == 3 ? new Request.PortablePdb(CurrentPdb(), null!) : new Request.CurrentCompilation(fixture.Compilation, input);
            AssertRoutingFailure(await new ExceptionFlowAnalysisDispatch(new()).AnalyzeAsync(request), ExceptionFlowAnalysisRoutingFailureCode.InvalidInput);
        }

        [Theory]
        [InlineData("public static class Fixture { public static void Root() { Missing(); } }", "CompilationFailure")]
        [InlineData("public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure")]
        public async Task WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess(string source, string expected)
        {
            var result = await DispatchHistorical(Input(source));
            AssertHistoricalFailure(result, HistoricalWorkerClientFailureCode.StructuredFailure);
            Assert.Equal(expected, result.Routing!.HistoricalFailure!.WorkerFailure!.Code.ToString());
            AssertStopped(result.Routing.HistoricalAnalysis!);
        }

        [Theory]
        [InlineData("crash", "WorkerCrash")]
        [InlineData("malformed", "MalformedResponse")]
        [InlineData("mismatch", "ProtocolMismatch")]
        [InlineData("nonzero", "NonZeroExit")]
        [InlineData("flood", "OutputLimit")]
        [InlineData("stderr-flood", "OutputLimit")]
        [InlineData("utf8", "TransportFailure")]
        public async Task ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess(string source, string expected)
        {
            var result = await DispatchHistorical(Input(source), Client(fake: true));
            AssertHistoricalFailure(result, Enum.Parse<HistoricalWorkerClientFailureCode>(expected));
            AssertStopped(result.Routing!.HistoricalAnalysis!);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TimeoutOrCancellation_PropagatesAndTerminatesOwnedProcess(bool cancel)
        {
            using CancellationTokenSource cancellation = cancel ? new(TimeSpan.FromSeconds(1)) : new();
            var result = await DispatchHistorical(Input("hang"), Client(fake: true, deadline: cancel ? 30 : 1), cancellation.Token);
            AssertHistoricalFailure(result, cancel ? HistoricalWorkerClientFailureCode.Cancelled : HistoricalWorkerClientFailureCode.Timeout);
            AssertStopped(result.Routing!.HistoricalAnalysis!);
        }

        [Fact]
        public async Task UnavailableEndpoint_IsRouterFailureNotCurrentFallback()
            => AssertRoutingFailure(await new ExceptionFlowAnalysisDispatch(new()).AnalyzeAsync(new Request.PortablePdb(HistoricalPdb(), new Request.ExecutionPayload.Worker(Input()))),
                ExceptionFlowAnalysisRoutingFailureCode.HistoricalUnavailable);

        [Fact]
        public async Task StartFailure_IsOriginalWorkerFailure()
        {
            var result = await DispatchHistorical(Input(), BrokenClient());
            AssertHistoricalFailure(result, HistoricalWorkerClientFailureCode.StartFailure);
            Assert.Null(result.Routing!.HistoricalAnalysis!.ProcessId);
        }

        [Fact]
        public async Task UnsupportedWorkerProfile_FailsBeforeProcessStart()
        {
            var result = await DispatchHistorical(Input() with { Context = new(LanguageVersion: "preview") });
            AssertHistoricalFailure(result, HistoricalWorkerClientFailureCode.UnsupportedInput);
            Assert.Null(result.Routing!.HistoricalAnalysis!.ProcessId);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PreCancellation_IsPassedUnchangedToSelectedExecution(bool historical)
        {
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            if (historical)
            {
                var result = await DispatchHistorical(Input(), token: cancellation.Token);
                AssertHistoricalFailure(result, HistoricalWorkerClientFailureCode.Cancelled);
                Assert.Null(result.Routing!.HistoricalAnalysis!.ProcessId);
            }
            else
            {
                Fixture fixture = CreateFixture();
                AssertRoutingFailure(await new ExceptionFlowAnalysisDispatch(new()).AnalyzeAsync(new Request.CurrentCompilation(fixture.Compilation, fixture.Input), cancellation.Token),
                    ExceptionFlowAnalysisRoutingFailureCode.Cancelled);
            }
        }

        [Fact]
        public async Task CurrentException_RemainsOriginalRouterFailure()
        {
            Fixture fixture = CreateFixture();
            AssertRoutingFailure(await new ExceptionFlowAnalysisDispatch(new()).AnalyzeAsync(new Request.CurrentCompilation(fixture.Compilation,
                fixture.Input with { Session = new(null!) })), ExceptionFlowAnalysisRoutingFailureCode.CurrentExecutionFailure);
        }

        [Fact]
        public async Task CurrentUncertainty_IsNotReplacedByWorkerCompletenessPolicy()
        {
            Fixture fixture = CreateFixture("public static class Fixture { public static void Root() { System.Console.WriteLine(); } }");
            var result = await new ExceptionFlowAnalysisDispatch(new()).AnalyzeAsync(new Request.CurrentCompilation(fixture.Compilation, fixture.Input));
            AssertSuccess(result, ExceptionFlowAnalyzerSelection.Current);
            Assert.Equal(Direct(fixture), result.Result);
            Assert.NotEmpty(result.Result!.Uncertainties);
        }

        [Fact]
        public void RequestForms_AreClosedRequiredTypedAndCannotCarryBothSourcesOrPayloads()
        {
            Assert.All(typeof(Request).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance), constructor => Assert.True(constructor.IsPrivate));
            Type[] forms = typeof(Request).GetNestedTypes(BindingFlags.NonPublic).Where(type => type.BaseType == typeof(Request)).ToArray();
            Assert.Equal(2, forms.Length);
            Assert.All(forms, form =>
            {
                Assert.True(form.IsSealed);
                var parameters = form.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single().GetParameters();
                Assert.Equal(2, parameters.Length);
                Assert.All(parameters, parameter => Assert.False(parameter.IsOptional || parameter.ParameterType == typeof(string) || parameter.ParameterType == typeof(bool)
                    || parameter.ParameterType == typeof(ExceptionFlowAnalyzerSelection)));
            });
        }

        [Fact]
        public void Architecture_DependencyDirectionHasNoBacklinksPolicyCopiesOrExecution()
        {
            string Read(string name) => File.ReadAllText(Path.Combine(Root(), "src", "XMLDocNormalizer", "Execution", "Analysis", name + ".cs"));
            string dispatch = Read("ExceptionFlowAnalysisDispatch");
            foreach (string required in new[] { "ExceptionFlowAnalyzerSelectionContext.Projector.Project", "ExceptionFlowAnalyzerSelectionPolicy.Select", "router.AnalyzeAsync" })
            {
                Assert.Contains(required, dispatch);
                Assert.Equal(1, dispatch.Split(required, StringSplitOptions.None).Length - 1);
            }
            Assert.True(dispatch.IndexOf("if (!projection.Succeeded)", StringComparison.Ordinal) < dispatch.IndexOf("ExceptionFlowAnalyzerSelectionPolicy.Select", StringComparison.Ordinal));
            Assert.True(dispatch.IndexOf("if (!decision.Succeeded)", StringComparison.Ordinal) < dispatch.IndexOf("router.AnalyzeAsync", StringComparison.Ordinal));
            string request = Read("ExceptionFlowAnalysisDispatchRequest");
            foreach (string forbidden in new[] { "5.0.0-", "CompilerVersion", "InformationalVersion", "TryCreate", "IsValidated", "Process.Start", "ProcessStartInfo", "Assembly.Load", "HistoricalWorkerClient",
                "File.", "Directory.", "JsonSerializer", "Dapper", "CSharpCompilation.Create", "CreateSummaryAnalysisSession", ".Session.Analyze", "Task.Run", "ExceptionFlowAnalyzerSelection.Current", "ExceptionFlowAnalyzerSelection.Historical" })
            {
                Assert.DoesNotContain(forbidden, dispatch);
                Assert.DoesNotContain(forbidden, request);
            }
            foreach (string lower in new[] { "ExceptionFlowAnalyzerSelectionContext.Projector", "ExceptionFlowAnalyzerSelectionPolicy", "ExceptionFlowAnalysisRouter" })
            {
                string source = Read(lower);
                Assert.DoesNotContain("ExceptionFlowAnalysisDispatch", source);
                if (lower != "ExceptionFlowAnalysisRouter") { Assert.DoesNotContain("ExceptionFlowAnalysisRouter", source); }
                if (lower != "ExceptionFlowAnalyzerSelectionPolicy") { Assert.DoesNotContain("ExceptionFlowAnalyzerSelectionPolicy", source); }
            }
            Assert.Single(typeof(ExceptionFlowAnalysisDispatch).GetFields(BindingFlags.Instance | BindingFlags.NonPublic), field => field.FieldType == typeof(ExceptionFlowAnalysisRouter));
            AssertNoHistoricalLoaded();
            Assert.DoesNotContain(typeof(ExceptionFlowAnalysisDispatch).Assembly.GetReferencedAssemblies(), assembly => assembly.Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        }

        private static void AssertSuccess(ExceptionFlowAnalysisDispatchResult result, ExceptionFlowAnalyzerSelection selection)
        {
            Assert.True(result.Succeeded, result.Routing?.HistoricalFailure?.Message ?? result.Routing?.RoutingFailure?.Message);
            Assert.True(result.Projection.Succeeded);
            Assert.True(result.Decision!.Succeeded);
            Assert.True(result.Routing!.Succeeded);
            Assert.Equal(selection, result.Decision.Selection);
            Assert.Equal(selection, result.Routing.Selection);
            Assert.Same(result.Routing.Result, result.Result);
        }
        private static void AssertProjectionFailure(ExceptionFlowAnalysisDispatchResult result, ExceptionFlowAnalyzerSelectionProjectionFailureCode code)
        {
            Assert.False(result.Succeeded);
            Assert.Equal(code, result.Projection.Failure!.Code);
            Assert.Null(result.Projection.Context);
            Assert.Null(result.Decision);
            Assert.Null(result.Routing);
            Assert.Null(result.Result);
        }
        private static void AssertRoutingFailure(ExceptionFlowAnalysisDispatchResult result, ExceptionFlowAnalysisRoutingFailureCode code)
        {
            Assert.False(result.Succeeded);
            Assert.True(result.Decision!.Succeeded);
            Assert.Equal(code, result.Routing!.RoutingFailure!.Code);
            Assert.Null(result.Result);
            Assert.Null(result.Routing.CurrentAnalysis);
            Assert.Null(result.Routing.HistoricalAnalysis);
        }
        private static void AssertHistoricalFailure(ExceptionFlowAnalysisDispatchResult result, HistoricalWorkerClientFailureCode code)
        {
            Assert.False(result.Succeeded);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, result.Decision!.Selection);
            Assert.Null(result.Result);
            Assert.Null(result.Routing!.Result);
            Assert.Null(result.Routing.CurrentAnalysis);
            Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure);
            Assert.Equal(code, result.Routing.HistoricalFailure!.Code);
        }
        private static void AssertStopped(HistoricalWorkerCallResult result)
        {
            Assert.NotNull(result.ProcessId);
            Assert.NotEqual(Environment.ProcessId, result.ProcessId);
            try { using Process process = Process.GetProcessById(result.ProcessId!.Value); Assert.True(process.HasExited); }
            catch (ArgumentException) { }
        }
        private static Task<ExceptionFlowAnalysisDispatchResult> DispatchHistorical(WorkerAnalysisInput input, HistoricalWorkerClient? client = null, CancellationToken token = default)
            => new ExceptionFlowAnalysisDispatch(new(client ?? Client())).AnalyzeAsync(new Request.PortablePdb(HistoricalPdb(), new Request.ExecutionPayload.Worker(input)), token);
        private static WorkerAnalysisInput Input(string source = Source) => new(source, "Fixture", "Root", new());
        private static HistoricalWorkerClient BrokenClient() => new(Path.Combine(Root(), "artifacts", "p5o2b8", "never-start"), WorkerPath(), TimeSpan.FromSeconds(30));
        private static HistoricalWorkerClient Client(bool fake = false, int deadline = 30) => new("dotnet", fake
            ? Path.Combine(Root(), "artifacts", "p5o2b4", "test-process", "bin", Configuration(), "net8.0", "BoundaryTestProcess.dll")
            : WorkerPath(), TimeSpan.FromSeconds(deadline));
        private static string Configuration() => new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        private static string WorkerPath() => Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", Configuration(), "net8.0", "XMLDocNormalizer.HistoricalWorker.dll");
        private static string HistoricalPath() => Path.Combine(Root(), "artifacts", "p5o2b7", "historical-pdb", "historical-input.dll");
        private static ExternalCompilationProvenanceDescriptor CurrentPdb() => ExternalCompilationProvenanceDescriptorFactoryTests.ReadRequiredDescriptor(
            ExternalCompilationProvenanceDescriptorFactoryTests.EmitPortablePdb("DispatchCurrentPdb", Source, new CSharpParseOptions(LanguageVersion.CSharp12), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)));
        private static ExternalCompilationProvenanceDescriptor HistoricalPdb()
        {
            byte[] pe = File.ReadAllBytes(HistoricalPath());
            byte[] pdb = File.ReadAllBytes(Path.ChangeExtension(HistoricalPath(), ".pdb"));
            PortableExecutableReference reference = MetadataReference.CreateFromImage(ImmutableArray.CreateRange(pe));
            CSharpCompilation consumer = CSharpCompilation.Create("DispatchMetadataConsumer", references: Helpers.MetadataReferences.Default.Append(reference), options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var assembly = Assert.IsAssignableFrom<IAssemblySymbol>(consumer.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(consumer, assembly, out var expected));
            using MemoryStream stream = new(pe, writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(expected, stream, out var debug));
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(debug, [.. pdb], out var descriptor));
            Assert.Equal(PortablePdbValidationKind.IdentityAndChecksum, descriptor.PortablePdb.ValidationKind);
            return descriptor;
        }
        private static ExternalCompilationProvenanceDescriptor SchemaPdb(string version)
        {
            // Options-unit fixture only; actual positive PE/PDB chains above use real target checksums.
            MetadataBuilder metadata = new();
            int[] counts = new int[MetadataTokens.TableCount];
            counts[(int)TableIndex.Module] = 1;
            byte[] blob = Encoding.UTF8.GetBytes(string.Join("\0", new[] { "language", "C#", "version", "2", "compiler-version", version }) + "\0");
            metadata.AddCustomDebugInformation(MetadataTokens.EntityHandle(TableIndex.Module, 1), metadata.GetOrAddGuid(new Guid("b5feec05-8cd0-4a83-96da-466284bb4bd8")), metadata.GetOrAddBlob(blob));
            BlobBuilder image = new();
            new PortablePdbBuilder(metadata, [.. counts], entryPoint: default).Serialize(image);
            byte[] bytes = image.ToArray();
            using MetadataReaderProvider reader = MetadataReaderProvider.FromPortablePdbImage([.. bytes]);
            BlobContentId id = new(reader.GetMetadataReader().DebugMetadataHeader!.Id);
            ExternalPeDebugDirectoryDescriptor expected = new(new ExternalModuleIdentity("dispatch-schema-fixture.dll", Guid.Empty), false, [], [id], []);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(expected, [.. bytes], out var descriptor));
            return descriptor;
        }
        private sealed record Fixture(CSharpCompilation Compilation, SyntaxTree Tree, ExceptionFlowSemanticEnvironment Environment, ExceptionFlowSummaryAnalysisSession Session)
        {
            internal CurrentExceptionFlowAnalysisInput Input => new(Tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root"), Session);
        }
        private static Fixture CreateFixture(string source = Source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "worker-input.cs");
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
                .Select(path => MetadataReference.CreateFromFile(path)).ToArray();
            Assert.Equal(7, references.Length);
            CSharpCompilation compilation = CSharpCompilation.Create("HistoricalWorkerInput", [tree], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            var environment = new ExceptionFlowSemanticEnvironment(ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation));
            return new(compilation, tree, environment, ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(environment));
        }
        private static CanonicalExceptionFlowAnalysisResult Direct(Fixture fixture) => RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(
            ExceptionFlowSummaryAnalysisSession.AnalyzeSolutionTransitivelyThrownExceptions(fixture.Input.Member, fixture.Environment));
        private static void AssertNoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
            assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
            || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
        private static string Root()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) { return directory.FullName; }
            }
            throw new InvalidOperationException("Cannot locate tool root.");
        }
    }
}
