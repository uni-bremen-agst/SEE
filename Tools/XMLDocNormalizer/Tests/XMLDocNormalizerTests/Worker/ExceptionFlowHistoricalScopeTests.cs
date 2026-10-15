using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
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
    /// <summary>Genuine complete project handoff, exact scope/flow/graph differentials and explicit unsupported closure.</summary>
    public sealed class ExceptionFlowHistoricalScopeTests
    {
        private const string RootPath = "/project-a/Target.cs";
        private const string HelperPath = "/project-a/Helper.cs";
        private const string Target = "public static partial class Fixture {\r\n/// <summary>Call the owned helper.</summary>\r\npublic static void Root() { Helper.Thrower(); } }";
        private const string Helper = "public static class Helper { public static System.InvalidOperationException Value = null!; public static void Thrower() {\r\n#if THROW\r\nthrow Value;\r\n#else\r\nreturn;\r\n#endif\r\n} }";
        private const string Direct = "public static class Fixture {\r\n/// <summary>Direct control.</summary>\r\npublic static void Root() { throw new System.InvalidOperationException(); } }";

        [Theory]
        [InlineData("direct", "12", 1, true, false)]
        [InlineData("direct", "12", 2, true, false)]
        [InlineData("project-transitive", "10", 2, true, false)]
        [InlineData("project-transitive", "12", 2, false, false)]
        [InlineData("project-transitive", "12", 3, true, true)]
        [InlineData("solution-transitive", "12", 2, true, true)]
        public async Task CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation(string mode, string language, int count, bool throwing, bool reversed)
        {
            var payload = Payload(mode, language, count, throwing, reversed);
            var native = Native(payload);
            Assert.Empty(native.Errors);
            if (mode == "direct" && count > 1 || !throwing) { Assert.Empty(native.Result.Entries); }
            else
            {
                Assert.NotEmpty(native.Result.Entries);
                Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0);
                if (count > 1) { Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == HelperPath); }
            }
            var input = await ValidatedInput(payload);
            Assert.Equal(count, input.ParseConfiguration!.SourceFileCount);
            var projection = HistoricalWorkerPayloadProjection.Project(input);
            Assert.True(projection.Succeeded, projection.Failure?.Message);
            Assert.Equal(payload.Context!.Scope!.OrderedSourcePaths, projection.Value!.Payload.Context!.Scope!.OrderedSourcePaths);
            Assert.Equal(mode, projection.Value.Payload.Context.AnalysisMode);
            Assert.Equal(WorkerProtocol.ReferenceWorldHash(payload), WorkerProtocol.ReferenceWorldHash(projection.Value.Payload));
            var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.True(first.Succeeded, first.PayloadProjection.Failure?.Message ?? first.Dispatch?.Routing?.HistoricalFailure?.Message);
            Assert.True(second.Succeeded); Assert.Equal(native.Result, first.Result); Assert.Equal(first.Result, second.Result);
            Assert.Null(first.Dispatch!.Routing!.CurrentAnalysis);
            var call = first.Dispatch.Routing.HistoricalAnalysis!;
            Assert.Equal(WorkerProtocol.Provenance(projection.Value.Payload), call.Provenance);
            Assert.Equal(mode, call.Provenance!.AnalysisMode);
            var graph = await Probe(payload, "--b14-graph");
            Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32());
            Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32());
            Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray());
            Assert.Equal(native.Summaries, graph.GetProperty("repeatedSummaries").EnumerateArray().Select(value => value.GetString()).ToArray());
            Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean());
            Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())));
            Assert.Equal(payload.Context.Scope.OrderedSourcePaths, graph.GetProperty("ownership").EnumerateArray().Select(item => item.GetProperty("logicalPath").GetString()).ToArray());
            Assert.All(graph.GetProperty("ownership").EnumerateArray(), item => { Assert.True(item.GetProperty("stableModel").GetBoolean()); Assert.Equal(payload.Context.AssemblyName, item.GetProperty("assemblyName").GetString()); });
            Stopped(call); Stopped(second.Dispatch!.Routing!.HistoricalAnalysis!); NoHistoricalLoaded();
            await Evidence("scope-" + mode + "-" + language + "-" + count + "-" + throwing + "-" + reversed, new
            {
                Mode = mode,
                Language = language,
                SourceCount = count,
                Throwing = throwing,
                Reversed = reversed,
                Native = native,
                Historical = first.Result,
                RepeatedHistorical = second.Result,
                Graph = graph,
                Payload = projection.Value.Payload with
                {
                    Context = projection.Value.Payload.Context! with
                    { References = projection.Value.Payload.Context.References!.Select(reference => reference with { Image = [] }).ToArray() }
                },
                call.Provenance,
                call.Identity,
                FirstProcess = call.ProcessId,
                FirstProcessExit = call.ProcessExit,
                SecondProcess = second.Dispatch.Routing.HistoricalAnalysis!.ProcessId,
                SecondProcessExit = second.Dispatch.Routing.HistoricalAnalysis.ProcessExit,
                OriginalP5GP5IP5JP5CP5E = true,
                NoCurrentFallback = true,
                ProcessesStopped = true,
                HistoricalLoadedInMain = false
            });
        }

        [Theory]
        [InlineData("missing-scope")]
        [InlineData("missing-source")]
        [InlineData("missing-additional")]
        [InlineData("source-identity")]
        [InlineData("duplicate-path")]
        [InlineData("missing-parse")]
        [InlineData("invalid-language")]
        [InlineData("missing-project")]
        [InlineData("foreign-project")]
        [InlineData("conflicting-project")]
        [InlineData("root-membership")]
        [InlineData("missing-mode")]
        [InlineData("unknown-mode")]
        [InlineData("declared-mode")]
        [InlineData("solution-without-projects")]
        [InlineData("supporting-compilation")]
        [InlineData("ordered-membership")]
        [InlineData("source-budget")]
        [InlineData("project-budget")]
        [InlineData("root-path")]
        public async Task MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart(string scenario)
        {
            var input = Payload("project-transitive", "12", 3, true, false);
            var context = input.Context!; var scope = context.Scope!; var sources = context.AdditionalSources!.ToArray();
            if (scenario == "missing-scope") { scope = null!; }
            if (scenario == "missing-source") { sources = sources.Take(1).ToArray(); }
            if (scenario == "missing-additional") { sources = null!; }
            if (scenario == "source-identity") { sources[0] = sources[0] with { Sha256 = new string('A', 64) }; }
            if (scenario == "duplicate-path") { scope = scope with { OrderedSourcePaths = [RootPath, HelperPath, HelperPath] }; }
            if (scenario == "missing-parse") { sources[0] = sources[0] with { ParseContext = null }; }
            if (scenario == "invalid-language") { sources[0] = sources[0] with { ParseContext = sources[0].ParseContext! with { LanguageVersion = "latest" } }; }
            if (scenario == "missing-project") { scope = scope with { AssemblyName = null }; }
            if (scenario == "foreign-project") { sources[0] = sources[0] with { AssemblyName = "Project.B" }; }
            if (scenario == "conflicting-project") { scope = scope with { AssemblyName = "Project.B" }; }
            if (scenario == "root-membership") { scope = scope with { RootLogicalPath = HelperPath }; }
            if (scenario == "missing-mode") { context = context with { AnalysisMode = null! }; }
            if (scenario == "unknown-mode") { context = context with { AnalysisMode = "transitive-ish" }; }
            if (scenario == "declared-mode") { context = context with { AnalysisMode = "project-transitive-declared-exceptions" }; }
            if (scenario == "solution-without-projects") { context = context with { AnalysisMode = "solution-transitive" }; scope = scope with { Kind = "solution-v1" }; }
            if (scenario == "supporting-compilation") { context = context with { SupportingCompilations = [new("B", [], [], context, "untrusted")] }; }
            if (scenario == "ordered-membership") { Array.Reverse(sources); }
            if (scenario == "source-budget") { sources[0] = sources[0] with { Text = new string(' ', WorkerProtocol.MaximumSourceCharacters) + "class H {}" }; }
            if (scenario == "project-budget") { scope = scope with { OrderedSourcePaths = Enumerable.Range(0, 17).Select(index => "/" + index + ".cs").ToArray() }; }
            if (scenario == "root-path") { input = input with { LogicalPath = null }; }
            var result = await Client().AnalyzeAsync(input with { Context = context with { Scope = scope, AdditionalSources = sources } });
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code); Assert.Null(result.ProcessId); Assert.Null(result.Result);
        }

        [Fact]
        public async Task SupportingSourceUnavailableLanguage_FailsInActualHistoricalCompilerWithoutDowngrade()
        {
            var input = Payload("project-transitive", "12", 2, true, false);
            var source = input.Context!.AdditionalSources![0];
            input = input with { Context = input.Context with { AdditionalSources = [source with { ParseContext = source.ParseContext! with { LanguageVersion = "999" } }] } };
            var result = await Client().AnalyzeAsync(input);
            Assert.Equal(WorkerFailureCode.UnsupportedContext, result.Failure!.WorkerFailure!.Code);
            Assert.Null(result.Result); Stopped(result); NoHistoricalLoaded();
            await Evidence("unsupported-source-language", new { result.Failure, NoDowngrade = true, NoFallback = true, ProcessStopped = true });
        }

        [Fact]
        public async Task OriginalProjectReceipt_RejectsCopiedIncompleteForeignAndUnvalidatedComposition()
        {
            var input = await ValidatedInput(Payload("project-transitive", "12", 2, true, false));
            var set = input.SourceSet!;
            var copied = new ExternalCSharpSyntaxTreeSet(set.Trees, set.SourceMaterials);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, HistoricalWorkerPayloadProjection.Project(input with { SourceSet = copied }).Failure!.Code);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, HistoricalWorkerPayloadProjection.Project(input with { SourceSet = null }).Failure!.Code);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, HistoricalWorkerPayloadProjection.Project(input with { ParseConfiguration = null }).Failure!.Code);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, HistoricalWorkerPayloadProjection.Project(input with { ReferenceSet = null }).Failure!.Code);
            Assert.True(ExternalCSharpSyntaxTreeSetFactory.TryGetOrigin(set, out var config, out var sources));
            Assert.False(ExternalCSharpSyntaxTreeSetFactory.TryCreate(config!, [input.Source.Document], [input.Source], out _));
            var foreign = await ValidatedInput(Payload("project-transitive", "10", 2, true, false));
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, HistoricalWorkerPayloadProjection.Project(input with { SourceSet = foreign.SourceSet }).Failure!.Code);
            var fake = new ExternalCSharpSyntaxTree(sources[1].Material, sources[1].Text, sources[1].Tree);
            Assert.True(ExternalCSharpSyntaxTreeSetFactory.TryCreate(config!, sources.Select(source => source.Document).ToArray(), [sources[0], fake], out var bad));
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, HistoricalWorkerPayloadProjection.Project(input with { SourceSet = bad }).Failure!.Code);
            await Evidence("original-receipts", new { CopiedRejected = true, IncompleteRejected = true, ForeignRejected = true, UnvalidatedP5IRejected = true, NoSecondValidation = true });
        }

        [Fact]
        public async Task DeclaredMode_IsGenuineDetectorPolicyNotInventedWorkerTraversal()
        {
            var input = Payload("project-transitive", "12", 2, true, false);
            var normal = Native(input); var declared = Native(input, ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions);
            Assert.False(declared.HasDeclaredExceptionTypes);
            Assert.NotEmpty(normal.Result.Entries); Assert.Empty(declared.Result.Entries);
            Assert.True(JsonSerializer.SerializeToElement(normal.Findings).GetArrayLength() > 0);
            Assert.Equal(0, JsonSerializer.SerializeToElement(declared.Findings).GetArrayLength());
            var original = await ValidatedInput(input);
            var rejected = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(original with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions }, new(new(Client())));
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, rejected.PayloadProjection.Failure!.Code); Assert.Null(rejected.Dispatch); Assert.Null(rejected.Result);
            await Evidence("declared-mode", new { Normal = normal, Declared = declared, ActualDetectorGuardDifference = true, HistoricalExplicitlyUnsupported = true, NoXmlContractExpansion = true });
        }

        [Fact]
        public async Task CrossProjectSourceClosure_IsRealCurrentInputAndExplicitB15Boundary()
        {
            var input = Payload("project-transitive", "12", 2, true, false);
            var references = input.Context!.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image)).ToArray();
            var helperTree = CSharpSyntaxTree.ParseText(Helper, new CSharpParseOptions(LanguageVersion.CSharp12, preprocessorSymbols: ["THROW"]), "/project-b/Helper.cs");
            var dependency = CSharpCompilation.Create("Project.B", [helperTree], references, new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            using var pe = new MemoryStream(); Assert.True(dependency.Emit(pe).Success);
            var targetTree = CSharpSyntaxTree.ParseText(Target, new CSharpParseOptions(LanguageVersion.CSharp12), RootPath);
            var target = CSharpCompilation.Create("Project.A", [targetTree], references.Append(MetadataReference.CreateFromImage(pe.ToArray())), new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            var root = targetTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var project = ProjectClosureSemanticContext.CreateSingleCompilationContext(targetTree, target);
            var projectFlow = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(new(project)).Analyze(root));
            var solution = ProjectClosureSemanticContext.CreateSingleCompilationContext(targetTree, target);
            solution.RegisterSupportingSource(dependency);
            var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(new(solution));
            var solutionFlow = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root));
            Assert.NotEmpty(solutionFlow.Entries); Assert.Empty(solutionFlow.Uncertainties);
            Assert.NotEqual(projectFlow, solutionFlow);
            Assert.Contains(solutionFlow.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == "/project-b/Helper.cs");
            Assert.Equal(solutionFlow, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)));
            var refused = await Client().AnalyzeAsync(input with { Context = input.Context with { AnalysisMode = "solution-transitive", Scope = input.Context.Scope! with { Kind = "solution-v1" } } });
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, refused.Failure!.Code); Assert.Null(refused.ProcessId); Assert.Null(refused.Result);
            await Evidence("cross-project-gap", new { ProjectFlow = projectFlow, SolutionFlow = solutionFlow, ActualSourceRegistry = true, RepeatedEqual = true, ExplicitUnsupported = true, FollowUp = "B15: exact dependency/assembly/source closure receipts and host reconstruction" });
        }

        [Fact]
        public async Task ForeignRootAndForgedSourceWorldOrPath_AreNotAccepted()
        {
            var input = Payload("project-transitive", "12", 2, true, false);
            var real = await Client().AnalyzeAsync(input); Assert.True(real.Success, real.Failure?.Message);
            var different = input with { Context = input.Context! with { Scope = input.Context.Scope! with { AssemblyName = "Foreign" } } };
            Assert.NotEqual(WorkerProtocol.ReferenceWorldHash(input), WorkerProtocol.ReferenceWorldHash(different));
            var response = new WorkerResponse(2, "analyze", true, real.Identity, real.Result, Provenance: WorkerProtocol.Provenance(different));
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, HistoricalWorkerResponseValidation.Validate(JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions), "", 0, input).Failure!.Code);
            var valid = response with { Provenance = WorkerProtocol.Provenance(input) };
            string forged = JsonSerializer.Serialize(valid, WorkerProtocol.JsonOptions).Replace(HelperPath, "/foreign/Helper.cs", StringComparison.Ordinal);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, HistoricalWorkerResponseValidation.Validate(forged, "", 0, input).Failure!.Code);
            var helper = input.Context!.AdditionalSources![0];
            var wrongRoot = input with
            {
                Source = helper.Text,
                LogicalPath = HelperPath,
                Context = input.Context with
                {
                    Scope = input.Context.Scope! with { RootLogicalPath = HelperPath },
                    AdditionalSources = [new(RootPath, input.Source!, WorkerProtocol.SourceHash(input.Source!), ParseContext: input.Context.ParseContext, AssemblyName: input.Context.AssemblyName)]
                }
            };
            var wrong = await Client().AnalyzeAsync(wrongRoot);
            Assert.Equal(WorkerFailureCode.InvalidRequest, wrong.Failure!.WorkerFailure!.Code); Assert.Null(wrong.Result);
            Stopped(real); Stopped(wrong);
            using var duplicates = JsonDocument.Parse("{\"scope\":{\"kind\":\"single-project-v1\",\"kind\":\"solution-v1\"}}");
            Assert.Throws<JsonException>(() => WorkerProtocol.RejectDuplicateProperties(duplicates.RootElement));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkerProjectScope>("{\"unknown\":true}", WorkerProtocol.JsonOptions));
            await Evidence("response-and-root", new { ForeignRootRejected = true, ForgedWorldRejected = true, ForgedPathRejected = true, StrictUnknownDuplicates = true, ProcessesStopped = true });
        }

        private sealed record Snapshot(CanonicalExceptionFlowAnalysisResult Result, int GraphCount, string[] Summaries, object Findings,
            bool RepeatedFindingsEqual, string[] Errors, bool HasDeclaredExceptionTypes, object[] Ownership);
        private static Snapshot Native(WorkerAnalysisInput input, ExceptionAnalysisMode? overrideMode = null)
        {
            var context = input.Context!; var documents = Documents(input);
            var trees = context.Scope!.OrderedSourcePaths!.Select(path =>
            {
                var source = documents.Single(document => document.LogicalPath == path);
                Assert.True(LanguageVersionFacts.TryParse(source.ParseContext!.LanguageVersion, out var language));
                return CSharpSyntaxTree.ParseText(source.Text, new CSharpParseOptions(language, DocumentationMode.Parse, SourceCodeKind.Regular, source.ParseContext.PreprocessorSymbols), path);
            }).ToArray();
            var references = context.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image,
                new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases: [.. reference.Aliases!]))).ToArray();
            var compilation = CSharpCompilation.Create(context.AssemblyName, trees, references, new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            var tree = trees.Single(candidate => candidate.FilePath == input.LogicalPath);
            var semantic = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            var environment = new ExceptionFlowSemanticEnvironment(semantic); var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(environment);
            var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var mode = overrideMode ?? Mode(context.AnalysisMode);
            bool direct = mode == ExceptionAnalysisMode.Direct || mode == ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions && !semantic.HasDeclaredExceptionTypesInReportingScope();
            CanonicalExceptionFlowAnalysisResult Analyze() => RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(direct
                ? ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions(root, environment) : session.Analyze(root));
            var result = Analyze(); var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
            var summaries = Summaries(graph); int count = graph.Count;
            Assert.Equal(result, Analyze()); Assert.Equal(count, graph.Count); Assert.Equal(summaries, Summaries(graph));
            var options = new XmlDocOptions { ExceptionAnalysisMode = mode };
            var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session);
            Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)));
            var ownership = trees.Select(owned => (object)new { LogicalPath = owned.FilePath, AssemblyName = compilation.AssemblyName, Owned = true, StableModel = true }).ToArray();
            return new(result, count, summaries, findings, true, compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).ToArray(), semantic.HasDeclaredExceptionTypesInReportingScope(), ownership);
        }
        private static string[] Summaries(ExceptionFlowSummaryGraph graph)
        {
            var entries = (Dictionary<ExceptionFlowCallableKey, ExceptionFlowSummary>)typeof(ExceptionFlowSummaryGraph).GetField("summaries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(graph)!;
            return entries.Select(entry => { Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary)); return JsonSerializer.Serialize(summary, WorkerProtocol.JsonOptions); }).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
        private static ExceptionAnalysisMode Mode(string mode) => mode switch
        {
            "direct" => ExceptionAnalysisMode.Direct,
            "project-transitive" => ExceptionAnalysisMode.ProjectTransitive,
            "solution-transitive" => ExceptionAnalysisMode.SolutionTransitive,
            _ => throw new ArgumentException("Unsupported test mode.")
        };
        private static object? B12(string method, params object?[] arguments) => typeof(ExceptionFlowHistoricalReferenceIdentityTests).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments);
        private static WorkerAnalysisInput Payload(string mode, string language, int count, bool throwing, bool reversed)
        {
            var input = (WorkerAnalysisInput)B12("Payload", count == 1 ? Direct : Target, (byte[][])B12("FrameworkImages")!, "Original.Scope.Target")!;
            var parse = new WorkerParseContext(language, throwing ? ["THROW"] : [], "parse", "regular");
            var paths = count == 1 ? new[] { RootPath } : count == 2 ? [RootPath, HelperPath] : [RootPath, HelperPath, "/project-a/Partial.cs"];
            if (reversed) { Array.Reverse(paths); }
            var extra = paths.Where(path => path != RootPath).Select(path =>
            {
                string text = path == HelperPath ? Helper : "public static partial class Fixture { public static int Additional = 1; }";
                return new WorkerSourceDocument(path, text, WorkerProtocol.SourceHash(text), ParseContext: parse, AssemblyName: input.Context!.AssemblyName);
            }).ToArray();
            return input with
            {
                LogicalPath = RootPath,
                Context = input.Context! with
                {
                    AnalysisMode = mode,
                    LanguageVersion = language,
                    ParseProfile = "validated-parse-v1",
                    ParseContext = parse,
                    Scope = new("single-project-v1", input.Context.AssemblyName, paths, RootPath),
                    AdditionalSources = extra
                }
            };
        }
        private static WorkerSourceDocument[] Documents(WorkerAnalysisInput input) => [new(input.LogicalPath!, input.Source!, WorkerProtocol.SourceHash(input.Source!), ParseContext: input.Context!.ParseContext, AssemblyName: input.Context.AssemblyName), .. input.Context.AdditionalSources!];
        private static Task<JsonElement> Probe(WorkerAnalysisInput input, string operation, string? output = null) => (Task<JsonElement>)B12("Probe", input, operation, output)!;
        private static async Task<HistoricalWorkerPayloadProjectionInput> ValidatedInput(WorkerAnalysisInput payload)
        {
            string output = Path.Combine(Root(), "artifacts", "p5o2b14", "emission-" + Guid.NewGuid().ToString("N"));
            await Probe(payload, "--b14-emit", output);
            var reference = MetadataReference.CreateFromImage(File.ReadAllBytes(Path.Combine(output, "input.dll")));
            var consumer = CSharpCompilation.Create("PayloadConsumer", references: ((byte[][])B12("FrameworkImages")!).Select(image => MetadataReference.CreateFromImage(image)).Append(reference), options: new(OutputKind.DynamicallyLinkedLibrary));
            var assembly = Assert.IsAssignableFrom<IAssemblySymbol>(consumer.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(consumer, assembly, out var expected));
            using var pe = new MemoryStream(File.ReadAllBytes(Path.Combine(output, "input.dll")), writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(expected, pe, out var debug));
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(debug, [.. File.ReadAllBytes(Path.Combine(output, "input.pdb"))], out var provenance));
            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(provenance, out var configuration));
            var originals = new List<ExternalCSharpSyntaxTree>(); var documents = Documents(payload);
            foreach (var path in payload.Context!.Scope!.OrderedSourcePaths!)
            {
                var document = provenance.PortablePdb.Documents.Single(document => document.Name == path);
                string text = documents.Single(source => source.LogicalPath == path).Text;
                using var bytes = new MemoryStream([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)], writable: false);
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(document, bytes, out var material));
                Assert.True(ExternalCSharpSyntaxTreeFactory.TryCreate(material, configuration, out var tree)); originals.Add(tree);
            }
            Assert.True(ExternalCSharpSyntaxTreeSetFactory.TryCreate(configuration, originals.Select(source => source.Document).ToArray(), originals, out var set));
            var materials = new List<ValidatedExternalMetadataReferenceMaterial>();
            var references = provenance.MetadataReferences!.References;
            for (int index = 0; index < references.Length; index++)
            {
                using var image = new MemoryStream(payload.Context.References![index].Image, writable: false);
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(references[index], image, out var material)); materials.Add(material);
            }
            Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(provenance, materials, out var referenceSet));
            var root = assembly.GetTypeByMetadataName("Fixture")!.GetMembers("Root").OfType<IMethodSymbol>().Single();
            return new(provenance, originals.Single(source => source.Document.Name == RootPath), root, Mode(payload.Context.AnalysisMode), referenceSet, configuration, set);
        }
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static void Stopped(HistoricalWorkerCallResult call)
            => HistoricalWorkerLifetimeAssertions.Stopped(call);
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        {
            string output = Path.Combine(Root(), "artifacts", "p5o2b14", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
