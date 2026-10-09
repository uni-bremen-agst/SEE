using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Execution.Semantic;
using P5A = XMLDocNormalizerTests.Execution.Semantic.ExternalCompilationProvenanceDescriptorFactoryTests;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Real external handoffs automatically projected to the existing Worker, never a manually built payload.</summary>
    public sealed class ExceptionFlowHistoricalPayloadProjectionTests
    {
        private const string Source = "public static class Fixture { public static void Root() { throw null; } }";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RealInputs_MapEverySupportedFieldAndRetainOriginalProvenanceWithoutSelection(bool historical)
        {
            var input = historical ? Historical() : Current();
            var projected = HistoricalWorkerPayloadProjection.Project(input);
            Assert.True(projected.Succeeded, projected.Failure?.Message);
            var value = projected.Value!;
            Assert.Same(input.Provenance, value.Provenance);
            Assert.Same(input.Source, value.Source);
            Assert.Same(input.Root, value.Root);
            Assert.Equal(input.Source.Text.ToString(), value.Payload.Source);
            Assert.Equal(Source, value.Payload.Source);
            Assert.Equal("Fixture", value.Payload.TypeMetadataName);
            Assert.Equal("Root", value.Payload.MethodName);
            var context = value.Payload.Context!;
            Assert.Equal("solution-transitive", context.AnalysisMode);
            Assert.Equal("net8-runtime-bounded-v1", context.ReferenceProfile);
            Assert.Equal("12", context.LanguageVersion);
            Assert.Equal("enable", context.NullableContext);
            Assert.Equal("HistoricalWorkerInput", context.AssemblyName);
            Assert.Null(context.AdditionalSources);
            Assert.Null(context.References);
            Assert.Null(context.SupportingCompilations);
            Assert.Null(context.RootIdentity);
            Assert.Null(context.CompilerOptions);
            Assert.Null(context.PreprocessorSymbols);
            Assert.True(WorkerInputValidation.IsSupported(value.Payload));
            Assert.Equal(value.Payload, HistoricalWorkerPayloadProjection.Project(input).Value!.Payload);
            AssertNoHistoricalLoaded();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task MissingRequiredInput_IsTypedAndCannotReachDispatch(int missing)
        {
            HistoricalWorkerPayloadProjectionInput? input = Historical();
            input = missing switch { 0 => null, 1 => input with { Provenance = null! }, 2 => input with { Source = null! }, _ => input with { Root = null! } };
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, null!);
            AssertFailure(result.PayloadProjection, HistoricalWorkerPayloadProjectionFailureCode.MissingInput);
            Assert.Null(result.Dispatch);
            Assert.Null(result.Result);
        }

        [Fact]
        public void NestedAndKeywordNames_UseExactMetadataRatherThanCSharpDisplayStrings()
        {
            const string source = "namespace @namespace { public static class Outer { public static class Inner { public static void Root() { throw null; } } } }";
            var input = Current(source, "namespace.Outer+Inner");
            var result = HistoricalWorkerPayloadProjection.Project(input);
            Assert.True(result.Succeeded, result.Failure?.Message);
            Assert.Equal("namespace.Outer+Inner", result.Value!.Payload.TypeMetadataName);
            Assert.Equal("Root", result.Value.Payload.MethodName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RawOrCopiedHandoffs_CannotMintTrustedPayload(bool source)
        {
            var input = Historical();
            if (source) { input = input with { Source = new(input.Source.Material, input.Source.Text, input.Source.Tree) }; }
            else { input = input with { Provenance = new(input.Provenance.DebugDirectory, input.Provenance.PortablePdb, input.Provenance.CompilationOptions, input.Provenance.MetadataReferences) }; }
            AssertFailure(HistoricalWorkerPayloadProjection.Project(input), source ? HistoricalWorkerPayloadProjectionFailureCode.UnvalidatedSource : HistoricalWorkerPayloadProjectionFailureCode.UnvalidatedProvenance);
        }

        [Fact]
        public void MissingActualPeValidationReceipt_IsNotHiddenByARealPdb()
        {
            var input = Historical();
            var debug = input.Provenance.DebugDirectory;
            var copy = new ExternalPeDebugDirectoryDescriptor(debug.ManifestModule, debug.IsDeterministic, debug.CodeViewPdbReferences, debug.EmbeddedPortablePdbIds, debug.PdbChecksums);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(copy, [.. File.ReadAllBytes(Path.ChangeExtension(HistoricalPath(), ".pdb"))], out var provenance));
            AssertFailure(HistoricalWorkerPayloadProjection.Project(input with { Provenance = provenance }), HistoricalWorkerPayloadProjectionFailureCode.MissingValidatedPe);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConflictingSourceOrPeMethod_IsRejected(bool root)
        {
            var input = Historical();
            var other = Current();
            input = root ? input with { Root = other.Root } : input with { Source = other.Source };
            AssertFailure(HistoricalWorkerPayloadProjection.Project(input), HistoricalWorkerPayloadProjectionFailureCode.ConflictingInputs);
        }

        [Fact]
        public void CallerMintedConfiguration_DoesNotAcquireP5GOriginThroughP5I()
        {
            var input = Historical();
            Assert.True(ExternalCSharpSyntaxTreeFactory.TryGetConfiguration(input.Source, out var valid));
            var raw = new ExternalCSharpCompilationConfiguration(valid!.ParseOptions, valid.CompilationOptions, valid.SigningProvenance,
                valid.CompilerVersion, valid.RuntimeVersion, valid.SourceFileCount, valid.DefaultEncodingWebName, valid.FallbackEncodingWebName);
            Assert.True(ExternalCSharpSyntaxTreeFactory.TryCreate(input.Source.Material, raw, out var tree));
            AssertFailure(HistoricalWorkerPayloadProjection.Project(input with { Source = tree }), HistoricalWorkerPayloadProjectionFailureCode.ConflictingInputs);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void UnsupportedConcreteOptions_AreNotReplacedWithWorkerDefaults(int scenario)
        {
            var parse = new CSharpParseOptions(scenario == 0 ? LanguageVersion.CSharp13 : LanguageVersion.CSharp12,
                preprocessorSymbols: scenario == 1 ? ["RECORDED"] : []);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: scenario == 2 ? NullableContextOptions.Disable : NullableContextOptions.Enable,
                checkOverflow: scenario == 3, allowUnsafe: scenario == 4,
                optimizationLevel: scenario == 5 ? OptimizationLevel.Release : OptimizationLevel.Debug,
                platform: scenario == 6 ? Platform.X86 : Platform.AnyCpu);
            AssertFailure(HistoricalWorkerPayloadProjection.Project(Current(parse: parse, options: options)), HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile);
        }

        [Theory]
        [InlineData("Direct")]
        [InlineData("ProjectTransitive")]
        [InlineData("ProjectTransitiveDeclaredExceptions")]
        public void UnsupportedAnalysisModes_AreNotDefaulted(string mode)
            => AssertFailure(HistoricalWorkerPayloadProjection.Project(Historical() with { AnalysisMode = Enum.Parse<ExceptionAnalysisMode>(mode) }), HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile);

        [Theory]
        [InlineData("public class Fixture { public void Root() { throw null; } }", "Fixture")]
        [InlineData("public static class Fixture { public static void Root(int value) { throw null; } }", "Fixture")]
        [InlineData("public static class Fixture<T> { public static void Root() { throw null; } }", "Fixture`1")]
        public void UnsupportedMethodSelectors_AreNotSimplified(string source, string type)
            => AssertFailure(HistoricalWorkerPayloadProjection.Project(Current(source, type)), HistoricalWorkerPayloadProjectionFailureCode.UnsupportedSelector);

        [Fact]
        public void MultipleRecordedSourceFiles_AreNotDroppedToFitTheWorker()
        {
            var trees = new[]
            {
                CSharpSyntaxTree.ParseText(Source, new CSharpParseOptions(LanguageVersion.CSharp12), "/first.cs", Encoding.UTF8),
                CSharpSyntaxTree.ParseText("public static class Extra { public static void Other() { throw null; } }", new CSharpParseOptions(LanguageVersion.CSharp12), "/second.cs", Encoding.UTF8)
            };
            var compilation = CSharpCompilation.Create("MultipleSources", trees, Helpers.MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            using MemoryStream pe = new();
            using MemoryStream pdb = new();
            var emitted = compilation.Emit(pe, pdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
            Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
            var input = Read(pe.ToArray(), pdb.ToArray(), Source, "Fixture", firstDocument: true);
            Assert.Equal(2, input.Provenance.PortablePdb.Documents.Length);
            AssertFailure(HistoricalWorkerPayloadProjection.Project(input), HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile);
        }

        [Fact]
        public void ValidatedSourceBeyondWorkerBudget_IsRejectedWithoutTruncation()
            => AssertFailure(HistoricalWorkerPayloadProjection.Project(Current(Source + new string(' ', WorkerProtocol.MaximumSourceCharacters))), HistoricalWorkerPayloadProjectionFailureCode.UnsupportedPayload);

        [Fact]
        public void SourceOwnedSelector_CannotPretendToBePeBound()
        {
            var input = Historical();
            var compilation = CSharpCompilation.Create("UnvalidatedSelector", [input.Source.Tree], Helpers.MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var root = compilation.GetTypeByMetadataName("Fixture")!.GetMembers("Root").OfType<IMethodSymbol>().Single();
            AssertFailure(HistoricalWorkerPayloadProjection.Project(input with { Root = root }), HistoricalWorkerPayloadProjectionFailureCode.ConflictingInputs);
        }

        [Fact]
        public async Task RealHistoricalProductionFlow_AutomaticPayloadParityIsolationAndCleanup()
        {
            var input = Historical();
            var before = new[] { typeof(Compilation).Assembly.ManifestModule.ModuleVersionId, typeof(CSharpCompilation).Assembly.ManifestModule.ModuleVersionId };
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.True(result.Succeeded, result.PayloadProjection.Failure?.Message ?? result.Dispatch?.Routing?.HistoricalFailure?.Message);
            Assert.Equal(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, result.Dispatch!.Projection.Context!.Provenance);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, result.Dispatch.Decision!.Selection);
            Assert.Equal("NullReferenceException", Assert.Single(result.Result!.Entries).ExceptionType.MetadataName);
            Assert.Empty(result.Result.Uncertainties);
            Assert.Null(result.Dispatch.Routing!.CurrentAnalysis);
            AssertStopped(result);
            AssertNoHistoricalLoaded();
            Assert.Equal(before, new[] { typeof(Compilation).Assembly.ManifestModule.ModuleVersionId, typeof(CSharpCompilation).Assembly.ManifestModule.ModuleVersionId });
            Assert.Same(input.Provenance, result.PayloadProjection.Value!.Provenance);
            Assert.Same(input.Source.Material, result.PayloadProjection.Value.Source.Material);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Source.Text.ToString()))), result.Dispatch.Routing.HistoricalAnalysis!.Provenance!.SourceSha256);
            string path = Path.Combine(Root(), "artifacts", "p5o2b10", "payload-production-boundary.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
            {
                AutomaticPayload = true,
                RawPayloadInPositiveSetup = false,
                ActualFactoryReceipt = ExternalCompilationProvenanceDescriptorFactory.IsValidated(input.Provenance),
                RealPdbValidation = input.Provenance.PortablePdb.ValidationKind.ToString(),
                HistoricalContext = result.Dispatch.Projection.Context,
                Payload = result.PayloadProjection.Value.Payload,
                OriginalDocumentName = input.Source.Document.Name,
                OriginalChecksumAlgorithm = input.Source.Document.HashAlgorithm,
                OriginalChecksum = Convert.ToHexString(input.Source.Document.Hash.AsSpan()),
                OriginalSourceBytes = Convert.ToHexString(SHA256.HashData(input.Source.Material.Image.AsSpan())),
                OriginalSourceOrigin = input.Source.Material.Origin.ToString(),
                OriginalSourceExactness = input.Source.Material.Exactness.ToString(),
                RetainsOriginalObjects = true,
                HistoricalIdentity = result.Dispatch.Routing.HistoricalAnalysis.Identity,
                WorkerProvenance = result.Dispatch.Routing.HistoricalAnalysis.Provenance,
                ProcessId = result.Dispatch.Routing.HistoricalAnalysis.ProcessId,
                ProcessStopped = true,
                HistoricalLoadedInCaller = false,
                CurrentMvids = before,
                CanonicalResult = result.Result,
                PeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(HistoricalPath()))),
                PdbSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.ChangeExtension(HistoricalPath(), ".pdb"))))
            }, WorkerProtocol.JsonOptions));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WorkerUnavailableOrCancelled_RetainsFailureWithoutCurrentFallback(bool cancelled)
        {
            using CancellationTokenSource token = new();
            if (cancelled) { token.Cancel(); }
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(Historical(), new(new(Client(broken: !cancelled))), token.Token);
            Assert.True(result.PayloadProjection.Succeeded);
            Assert.False(result.Succeeded);
            Assert.Equal(cancelled ? HistoricalWorkerClientFailureCode.Cancelled : HistoricalWorkerClientFailureCode.StartFailure, result.Dispatch!.Routing!.HistoricalFailure!.Code);
            Assert.Null(result.Dispatch.Routing.CurrentAnalysis);
            Assert.Null(result.Dispatch.Routing.HistoricalAnalysis!.ProcessId);
            Assert.Null(result.Result);
        }

        [Fact]
        public async Task CurrentPdbDoesNotMakePayloadProjectionChooseAnEngineOrFallBack()
        {
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(Current(), new(new(Client(broken: true))));
            Assert.True(result.PayloadProjection.Succeeded);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Current, result.Dispatch!.Decision!.Selection);
            Assert.Equal(ExceptionFlowAnalysisRoutingFailureCode.InvalidInput, result.Dispatch.Routing!.RoutingFailure!.Code);
            Assert.Null(result.Dispatch.Routing.HistoricalAnalysis);
            Assert.Null(result.Dispatch.Routing.CurrentAnalysis);
            Assert.Null(result.Result);
        }

        [Fact]
        public void Architecture_NoSelectionValidationParsingExecutionOrRawPayloadInput()
        {
            string source = File.ReadAllText(Path.Combine(Root(), "src", "XMLDocNormalizer", "Execution", "Analysis", "HistoricalWorkerPayloadProjection.cs"));
            foreach (string forbidden in new[] { "ExceptionFlowAnalyzerSelectionPolicy", "ExceptionFlowAnalysisRouter", "HistoricalWorkerClient", "Process.Start", "Assembly.Load", "5.0.0-", "File.", "Stream", "MetadataReader", "PEReader", "ParseText", "SourceText.From", "TryCreate(", "GetChecksum", "SHA256", "JsonSerializer", "AnalyzeAsync" })
            { Assert.DoesNotContain(forbidden, source); }
            Assert.DoesNotContain(typeof(HistoricalWorkerPayloadProjectionInput).GetProperties(), property => property.PropertyType == typeof(string));
            AssertNoHistoricalLoaded();
        }

        private static HistoricalWorkerPayloadProjectionInput Historical()
            => Read(File.ReadAllBytes(HistoricalPath()), File.ReadAllBytes(Path.ChangeExtension(HistoricalPath(), ".pdb")), Source, "Fixture");
        private static HistoricalWorkerPayloadProjectionInput Current(string source = Source, string type = "Fixture", CSharpParseOptions? parse = null, CSharpCompilationOptions? options = null)
        {
            var emitted = P5A.EmitPortablePdb("PayloadCurrent", source, parse ?? new(LanguageVersion.CSharp12), options ?? new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            return Read(emitted.PeImage, emitted.PdbImage, source, type);
        }
        private static HistoricalWorkerPayloadProjectionInput Read(byte[] pe, byte[] pdb, string source, string type, bool firstDocument = false)
        {
            var reference = MetadataReference.CreateFromImage(ImmutableArray.CreateRange(pe));
            var consumer = CSharpCompilation.Create("PayloadConsumer", references: Helpers.MetadataReferences.Default.Append(reference), options: new(OutputKind.DynamicallyLinkedLibrary));
            var assembly = Assert.IsAssignableFrom<IAssemblySymbol>(consumer.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(consumer, assembly, out var expected));
            using MemoryStream stream = new(pe, writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(expected, stream, out var debug));
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(debug, [.. pdb], out var provenance));
            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(provenance, out var configuration));
            var document = firstDocument ? provenance.PortablePdb.Documents[0] : Assert.Single(provenance.PortablePdb.Documents);
            // Same original encoding/preamble as the actual emitter; P5H, not B10, checks these bytes.
            using MemoryStream sourceStream = new([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(source)], writable: false);
            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(document, sourceStream, out var material));
            Assert.True(ExternalCSharpSyntaxTreeFactory.TryCreate(material, configuration, out var tree));
            var root = assembly.GetTypeByMetadataName(type)!.GetMembers("Root").OfType<IMethodSymbol>().Single();
            return new(provenance, tree, root, ExceptionAnalysisMode.SolutionTransitive);
        }
        private static void AssertFailure(HistoricalWorkerPayloadProjectionResult result, HistoricalWorkerPayloadProjectionFailureCode code)
        {
            Assert.False(result.Succeeded);
            Assert.Equal(code, result.Failure!.Code);
            Assert.Null(result.Value);
        }
        private static HistoricalWorkerClient Client(bool broken = false) => new(broken ? Path.Combine(Root(), "artifacts", "p5o2b10", "never-start") : "dotnet",
            Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(30));
        private static string HistoricalPath() => Path.Combine(Root(), "artifacts", "p5o2b7", "historical-pdb", "historical-input.dll");
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static void AssertStopped(ExternalExceptionFlowAnalysisResult result)
        {
            int processId = result.Dispatch!.Routing!.HistoricalAnalysis!.ProcessId!.Value;
            Assert.NotEqual(Environment.ProcessId, processId);
            try { using var process = Process.GetProcessById(processId); Assert.True(process.HasExited); }
            catch (ArgumentException) { }
        }
        private static void AssertNoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
            assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
            || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
    }
}
