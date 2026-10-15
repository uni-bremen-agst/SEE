using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Explicit validated parse input; real production projection, genuine child and complete graph comparisons.</summary>
    public sealed class ExceptionFlowHistoricalSourceContextTests
    {
        private const string ConditionalSource = "public static class Fixture { public static System.InvalidOperationException Value = null!;\r\n/// <summary>Exercise the selected active body.</summary>\r\npublic static void Root() {\r\n#if FIRST && SECOND\r\nthrow Value;\r\n#elif FIRST\r\nThrower();\r\n#else\r\nreturn;\r\n#endif\r\n} static void Thrower() { throw Value; } }";

        [Theory]
        [InlineData("10", 0)]
        [InlineData("10", 1)]
        [InlineData("10", 2)]
        [InlineData("12", 0)]
        [InlineData("12", 1)]
        [InlineData("12", 2)]
        public async Task ValidatedParseContext_ActiveProgramFullFlowGraphFindingsProvenanceRepeatAndIsolation(string language, int defined)
        {
            string[] symbols = defined == 0 ? [] : defined == 1 ? ["FIRST"] : ["SECOND", "FIRST"];
            var payload = Payload(ConditionalSource, language, symbols);
            var native = Native(payload);
            Assert.Empty(native.Errors);
            if (defined == 0) { Assert.Empty(native.Result.Entries); }
            else
            {
                Assert.NotEmpty(native.Result.Entries);
                Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0);
            }
            var input = await ValidatedInput(payload);
            var originalParse = input.ParseConfiguration!.ParseOptions;
            await Evidence("validated-input-" + language + "-" + defined, new
            {
                ConcreteVersionDisplay = originalParse.SpecifiedLanguageVersion.ToDisplayString(),
                Kind = originalParse.Kind.ToString(),
                Documentation = originalParse.DocumentationMode.ToString(),
                Features = originalParse.Features.ToArray(),
                Defines = originalParse.PreprocessorSymbolNames.ToArray()
            });
            var projection = HistoricalWorkerPayloadProjection.Project(input);
            Assert.True(projection.Succeeded, projection.Failure?.Message);
            Assert.Equal(symbols, projection.Value!.Payload.Context!.ParseContext!.PreprocessorSymbols);
            Assert.Equal(language, projection.Value.Payload.Context.LanguageVersion);
            Assert.Equal("validated-parse-v1", projection.Value.Payload.Context.ParseProfile);
            if (defined > 0 || language != "12")
            { Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, HistoricalWorkerPayloadProjection.Project(input with { ParseConfiguration = null }).Failure!.Code); }
            var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.True(first.Succeeded, first.Dispatch?.Routing?.HistoricalFailure?.Message ?? first.PayloadProjection.Failure?.Message);
            Assert.True(second.Succeeded);
            Assert.Equal(native.Result, first.Result); Assert.Equal(first.Result, second.Result);
            Assert.Null(first.Dispatch!.Routing!.CurrentAnalysis);
            var result = first.Dispatch.Routing.HistoricalAnalysis!;
            Assert.Equal(WorkerProtocol.Provenance(projection.Value.Payload), result.Provenance);
            Assert.Equal(language, result.Provenance!.LanguageVersion);
            var graph = await Probe(payload, "--b13-graph");
            Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32());
            Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32());
            Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray());
            Assert.Equal(native.Summaries, graph.GetProperty("repeatedSummaries").EnumerateArray().Select(value => value.GetString()).ToArray());
            Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean());
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())));
            Stopped(result); Stopped(second.Dispatch!.Routing!.HistoricalAnalysis!);
            NoHistoricalLoaded();
            await Evidence("parse-" + language + "-" + defined, new
            {
                Language = language,
                Symbols = symbols,
                Native = native,
                Historical = first.Result,
                RepeatedHistorical = second.Result,
                Graph = graph,
                result.Provenance,
                result.Identity,
                FirstProcess = result.ProcessId,
                FirstProcessExit = result.ProcessExit,
                SecondProcess = second.Dispatch.Routing.HistoricalAnalysis!.ProcessId,
                SecondProcessExit = second.Dispatch.Routing.HistoricalAnalysis.ProcessExit,
                OriginalP5GP5IP5CP5E = true,
                NoCurrentFallback = true,
                ProcessesStopped = true,
                HistoricalLoadedInMain = false
            });
        }

        [Fact]
        public async Task ConcreteLanguageVersion_ChangesAdmissibilityWithoutCompilerEmulation()
        {
            const string source = "public static class Fixture { public static void Root() { _ = \"\"\"raw\"\"\"; throw null; } }";
            var old = Payload(source, "10", []); var modern = Payload(source, "12", []);
            var oldNative = Native(old); var modernNative = Native(modern);
            Assert.Contains(oldNative.Errors, error => error.StartsWith("CS8936", StringComparison.Ordinal));
            Assert.Empty(modernNative.Errors);
            var before = await Client().AnalyzeAsync(old); var after = await Client().AnalyzeAsync(modern);
            Assert.Equal(WorkerFailureCode.CompilationFailure, before.Failure!.WorkerFailure!.Code);
            Assert.Contains(before.Failure.WorkerFailure.Details, error => error.StartsWith("CS8936", StringComparison.Ordinal));
            Assert.Null(before.Result); Assert.True(after.Success, after.Failure?.Message); Assert.Equal(modernNative.Result, after.Result);
            Stopped(before); Stopped(after); NoHistoricalLoaded();
            await Evidence("language-admissibility", new { Current10 = oldNative, Current12 = modernNative, Historical10 = before.Failure, Historical12 = after.Result, NoDowngrade = true });
        }

        [Theory]
        [InlineData("missing-parse")]
        [InlineData("missing-profile")]
        [InlineData("missing-language")]
        [InlineData("invalid-language")]
        [InlineData("latest")]
        [InlineData("missing-symbols")]
        [InlineData("duplicate-symbols")]
        [InlineData("invalid-symbol")]
        [InlineData("true-symbol")]
        [InlineData("false-symbol")]
        [InlineData("symbol-budget")]
        [InlineData("language-conflict")]
        [InlineData("doc-mode")]
        [InlineData("source-kind")]
        [InlineData("missing-scope")]
        [InlineData("missing-path")]
        [InlineData("direct")]
        [InlineData("project-transitive")]
        [InlineData("project-transitive-declared-exceptions")]
        [InlineData("compiler-conflict")]
        [InlineData("extra-source")]
        public async Task MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart(string scenario)
        {
            var input = Payload(ConditionalSource, "12", []);
            var context = input.Context!; var parse = context.ParseContext!;
            if (scenario == "missing-parse") { context = context with { ParseContext = null }; }
            if (scenario == "missing-profile") { context = context with { ParseProfile = null }; }
            if (scenario == "missing-language") { parse = parse with { LanguageVersion = null }; }
            if (scenario == "invalid-language") { parse = parse with { LanguageVersion = "unknown" }; context = context with { LanguageVersion = "unknown" }; }
            if (scenario == "latest") { parse = parse with { LanguageVersion = "latest" }; context = context with { LanguageVersion = "latest" }; }
            if (scenario == "missing-symbols") { parse = parse with { PreprocessorSymbols = null }; }
            if (scenario == "duplicate-symbols") { parse = parse with { PreprocessorSymbols = ["FIRST", "FIRST"] }; }
            if (scenario == "invalid-symbol") { parse = parse with { PreprocessorSymbols = ["NOT-A-SYMBOL"] }; }
            if (scenario == "true-symbol") { parse = parse with { PreprocessorSymbols = ["true"] }; }
            if (scenario == "false-symbol") { parse = parse with { PreprocessorSymbols = ["false"] }; }
            if (scenario == "symbol-budget") { parse = parse with { PreprocessorSymbols = Enumerable.Range(0, 129).Select(index => "S" + index).ToArray() }; }
            if (scenario == "language-conflict") { parse = parse with { LanguageVersion = "10" }; }
            if (scenario == "doc-mode") { parse = parse with { DocumentationMode = "diagnose" }; }
            if (scenario == "source-kind") { parse = parse with { SourceKind = "script" }; }
            if (scenario == "missing-scope") { context = context with { AnalysisMode = null! }; }
            if (scenario == "missing-path") { input = input with { LogicalPath = null }; }
            if (scenario is "direct" or "project-transitive" or "project-transitive-declared-exceptions") { context = context with { AnalysisMode = scenario }; }
            if (scenario == "compiler-conflict") { context = context with { CompilerOptions = [new("unsafe", "true")] }; }
            if (scenario == "extra-source") { context = context with { AdditionalSources = [new("other.cs", "class Other {}", new string('A', 64))] }; }
            if (scenario != "missing-parse") { context = context with { ParseContext = parse }; }
            var result = await Client().AnalyzeAsync(input with { Context = context });
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code);
            Assert.Null(result.ProcessId); Assert.Null(result.Result);
        }

        [Fact]
        public async Task HistoricalUnavailableConcreteLanguage_IsTypedUnsupportedNotDefaultOrCurrent()
        {
            var input = Payload(ConditionalSource, "999", []);
            var result = await Client().AnalyzeAsync(input);
            Assert.Equal(WorkerFailureCode.UnsupportedContext, result.Failure!.WorkerFailure!.Code);
            Assert.Null(result.Result); Assert.NotNull(result.ProcessId); Stopped(result); NoHistoricalLoaded();
            await Evidence("unsupported-language", new { Language = "999", result.Failure, NoFallback = true, NoDowngrade = true, ProcessStopped = true });
        }

        [Fact]
        public async Task OriginalParseReceipt_RejectsCopiedConfigurationAndMissingExactReferenceContext()
        {
            var input = await ValidatedInput(Payload(ConditionalSource, "12", ["FIRST"]));
            var config = input.ParseConfiguration!;
            var copied = new ExternalCSharpCompilationConfiguration(config.ParseOptions, config.CompilationOptions,
                config.CompilerVersion, config.RuntimeVersion, config.SourceFileCount, config.DefaultEncodingWebName, config.FallbackEncodingWebName);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.ConflictingInputs, HistoricalWorkerPayloadProjection.Project(input with { ParseConfiguration = copied }).Failure!.Code);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, HistoricalWorkerPayloadProjection.Project(input with { ReferenceSet = null }).Failure!.Code);
            var another = await ValidatedInput(Payload(ConditionalSource, "10", ["FIRST"]));
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.ConflictingInputs, HistoricalWorkerPayloadProjection.Project(input with { ParseConfiguration = another.ParseConfiguration }).Failure!.Code);
            var copiedProvenance = new ExternalCompilationProvenanceDescriptor(input.Provenance.DebugDirectory,
                input.Provenance.PortablePdb, input.Provenance.CompilationOptions, input.Provenance.MetadataReferences);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnvalidatedProvenance, HistoricalWorkerPayloadProjection.Project(input with { Provenance = copiedProvenance }).Failure!.Code);
        }

        [Fact]
        public async Task ForgedReturnedParseWorld_IsNotAcceptedAsAnotherActiveProgram()
        {
            var input = Payload(ConditionalSource, "12", ["FIRST"]);
            var real = await Client().AnalyzeAsync(input); Assert.True(real.Success, real.Failure?.Message);
            var different = input with { Context = input.Context! with { ParseContext = input.Context.ParseContext! with { PreprocessorSymbols = [] } } };
            Assert.NotEqual(WorkerProtocol.ReferenceWorldHash(input), WorkerProtocol.ReferenceWorldHash(different));
            var response = new WorkerResponse(2, "analyze", true, real.Identity, real.Result, Provenance: WorkerProtocol.Provenance(different));
            var rejected = HistoricalWorkerResponseValidation.Validate(JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions), "", 0, input);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, rejected.Failure!.Code); Assert.Null(rejected.Result);
            using var duplicate = JsonDocument.Parse("{\"parseContext\":{\"languageVersion\":\"12\",\"languageVersion\":\"10\"}}");
            Assert.Throws<JsonException>(() => WorkerProtocol.RejectDuplicateProperties(duplicate.RootElement));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkerParseContext>("{\"unknown\":true}", WorkerProtocol.JsonOptions));
            Stopped(real);
        }

        [Theory]
        [InlineData("Direct")]
        [InlineData("ProjectTransitive")]
        [InlineData("ProjectTransitiveDeclaredExceptions")]
        public async Task ProductionScope_DoesNotSilentlySubstituteSolutionTransitive(string mode)
        {
            var input = await ValidatedInput(Payload(ConditionalSource, "12", ["FIRST"]));
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input with { AnalysisMode = Enum.Parse<ExceptionAnalysisMode>(mode) }, new(new(Client())));
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, result.PayloadProjection.Failure!.Code);
            Assert.Null(result.Dispatch); Assert.Null(result.Result);
        }

        private sealed record Snapshot(CanonicalExceptionFlowAnalysisResult Result, int GraphCount, string[] Summaries,
            object Findings, bool RepeatedFindingsEqual, string[] Errors);
        private static Snapshot Native(WorkerAnalysisInput input)
        {
            Assert.True(LanguageVersionFacts.TryParse(input.Context!.ParseContext!.LanguageVersion, out var language));
            var tree = CSharpSyntaxTree.ParseText(input.Source!, new CSharpParseOptions(language,
                preprocessorSymbols: input.Context.ParseContext.PreprocessorSymbols), input.LogicalPath!);
            var refs = input.Context.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image,
                new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases: [.. reference.Aliases!]))).ToArray();
            var compilation = CSharpCompilation.Create(input.Context.AssemblyName, [tree], refs,
                new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(new(context));
            var member = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var result = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member));
            var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
            var summaries = GraphSummaries(graph); int count = graph.Count;
            Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)));
            Assert.Equal(count, graph.Count); Assert.Equal(summaries, GraphSummaries(graph));
            var options = new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive };
            var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, options, session);
            Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, options, session)));
            return new(result, count, summaries, findings, true, compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).ToArray());
        }
        private static string[] GraphSummaries(ExceptionFlowSummaryGraph graph)
        {
            var entries = (Dictionary<ExceptionFlowCallableKey, ExceptionFlowSummary>)typeof(ExceptionFlowSummaryGraph).GetField("summaries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(graph)!;
            return entries.Select(entry => { Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary)); return JsonSerializer.Serialize(summary, WorkerProtocol.JsonOptions); })
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
        private static object? B12(string method, params object?[]? arguments)
            => typeof(ExceptionFlowHistoricalReferenceIdentityTests).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments);
        private static WorkerAnalysisInput Payload(string source, string language, string[] symbols)
        {
            var input = (WorkerAnalysisInput)B12("Payload", source, (byte[][])B12("FrameworkImages")!, "Original.Parse.Target")!;
            return input with { Context = input.Context! with { LanguageVersion = language, ParseProfile = "validated-parse-v1", ParseContext = new(language, symbols, "parse", "regular") } };
        }
        private static Task<JsonElement> Probe(WorkerAnalysisInput input, string operation, string? output = null)
            => (Task<JsonElement>)B12("Probe", input, operation, output)!;
        private static async Task<HistoricalWorkerPayloadProjectionInput> ValidatedInput(WorkerAnalysisInput payload)
        {
            string output = Path.Combine(Root(), "artifacts", "p5o2b13", "emission-" + Guid.NewGuid().ToString("N"));
            await Probe(payload, "--b13-emit", output);
            var input = (HistoricalWorkerPayloadProjectionInput)typeof(ExceptionFlowHistoricalPayloadProjectionTests).GetMethod("Read", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [File.ReadAllBytes(Path.Combine(output, "input.dll")), File.ReadAllBytes(Path.Combine(output, "input.pdb")), payload.Source, "Fixture", false])!;
            var expected = input.Provenance.MetadataReferences!.References; var materials = new List<ValidatedExternalMetadataReferenceMaterial>();
            Assert.Equal(payload.Context!.References!.Length, expected.Length);
            for (int index = 0; index < expected.Length; index++)
            {
                using var stream = new MemoryStream(payload.Context.References[index].Image, writable: false);
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(expected[index], stream, out var material)); materials.Add(material);
            }
            Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(input.Provenance, materials, out var set));
            Assert.True(ExternalCSharpSyntaxTreeFactory.TryGetConfiguration(input.Source, out var configuration));
            return input with { ReferenceSet = set, ParseConfiguration = configuration };
        }
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static void Stopped(HistoricalWorkerCallResult call)
            => HistoricalWorkerLifetimeAssertions.Stopped(call);
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        {
            string output = Path.Combine(Root(), "artifacts", "p5o2b13", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
