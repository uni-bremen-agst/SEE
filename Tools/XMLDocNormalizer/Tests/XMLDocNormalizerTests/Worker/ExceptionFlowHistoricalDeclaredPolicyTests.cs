using System.Collections.Immutable;
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
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Actual Current detector versus genuine isolated neutral facts and Main eligibility; no Declared activation.</summary>
    public sealed class ExceptionFlowHistoricalDeclaredPolicyTests
    {
        private static WorkerDeclaredPolicyRequest OptIn() => new(WorkerDeclaredPolicyContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions);
        private const string Local = "public partial class LocalException : System.Exception { public LocalException() { } }";
        private const string Values = "public static bool Flag; public static LocalException Local = null!; public static System.InvalidOperationException External = null!;";

        [Theory]
        [InlineData("local")]
        [InlineData("external")]
        [InlineData("mixed")]
        [InlineData("combined")]
        [InlineData("unused")]
        [InlineData("partial")]
        [InlineData("nested")]
        public async Task FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation(string scenario)
        {
            var payload = Payload(scenario);
            var native = Native(payload);
            var original = await Original(payload);
            var input = original with { DeclaredPolicy = OptIn() };
            var projected = HistoricalWorkerPayloadProjection.Project(input); Assert.True(projected.Succeeded, projected.Failure?.Message);
            var prepared = projected.Value!.Payload;
            var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.True(first.Succeeded, first.PayloadProjection.Failure?.Message ?? first.Dispatch?.Routing?.HistoricalFailure?.Message);
            Assert.True(second.Succeeded);
            var call = first.Dispatch!.Routing!.HistoricalAnalysis!; var repeat = second.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.Null(first.Dispatch.Routing.CurrentAnalysis); Assert.Null(second.Dispatch.Routing.CurrentAnalysis);
            Assert.Equal(native.Summary, call.Result); Assert.Equal(call.Result, repeat.Result);
            Assert.Equal(native.Direct, call.DeclaredPolicy!.DirectResult);
            Assert.Equal(JsonSerializer.Serialize(call.DeclaredPolicy, WorkerProtocol.JsonOptions), JsonSerializer.Serialize(repeat.DeclaredPolicy, WorkerProtocol.JsonOptions));
            Assert.Equal(WorkerProtocol.Provenance(prepared), call.Provenance);
            Assert.Equal("project-transitive", call.Provenance!.AnalysisMode); // Facts, not Declared mode aliasing.
            var actualDefinitions = call.DeclaredPolicy.Declarations!;
            Assert.Equal(JsonSerializer.Serialize(native.Declarations, WorkerProtocol.JsonOptions), JsonSerializer.Serialize(actualDefinitions, WorkerProtocol.JsonOptions));
            Assert.Equal(actualDefinitions.Select(item => item.Type), prepared.Context!.DeclaredPolicy!.ExpectedDeclarations);
            Assert.Equal(prepared.Context.Scope!.OrderedSourcePaths, call.DeclaredPolicy.ReportingSourcePaths);
            if (scenario == "partial") { Assert.Equal(2, Assert.Single(actualDefinitions).DeclaringSourcePaths!.Length); }
            Assert.NotEqual(JsonSerializer.Serialize(native.ProjectFindings), JsonSerializer.Serialize(native.DeclaredFindings));

            var decisions = new List<object>();
            foreach (var expected in native.Expected)
            {
                var query = new DeclaredFindingPolicyQuery(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, expected.Type, expected.Kind);
                var decision = DeclaredExceptionFindingPolicy.Evaluate(call, query);
                var repeated = DeclaredExceptionFindingPolicy.Evaluate(repeat, query);
                Assert.True(decision.Succeeded, decision.Failure?.Message); Assert.Equal(decision, repeated);
                Assert.Equal(expected.Local, decision.Decision!.LocallyDeclared);
                Assert.Equal(expected.Relevant, decision.Decision.Relevant);
                if (expected.Kind is DeclaredFindingSourceKind.Direct or DeclaredFindingSourceKind.Summary)
                {
                    string id = expected.Kind == DeclaredFindingSourceKind.Direct ? "DOC610" : "DOC611";
                    Assert.Equal(decision.Decision.Relevant, native.DeclaredFindings.Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id
                        && finding.GetProperty("message").GetString()!.Contains(expected.DisplayName, StringComparison.Ordinal)));
                }
                Assert.Equal(native.HasDeclaredTypes, decision.Decision.SummaryEnabled);
                Assert.Equal(native.Direct.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasDirectSource);
                Assert.Equal(native.Summary.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasSummarySource);
                decisions.Add(new { Query = query, Native = expected, Historical = decision, RepeatedHistorical = repeated });
            }
            if (scenario == "combined")
            {
                var combined = decisions.Select(item => JsonSerializer.SerializeToElement(item, WorkerProtocol.JsonOptions))
                    .Single(item => item.GetProperty("query").GetProperty("sourceKind").GetString() == "summary" && item.GetProperty("native").GetProperty("local").GetBoolean());
                var decision = combined.GetProperty("historical").GetProperty("decision");
                Assert.True(decision.GetProperty("hasDirectSource").GetBoolean()); Assert.True(decision.GetProperty("hasSummarySource").GetBoolean());
                Assert.False(decision.GetProperty("relevant").GetBoolean());
            }
            if (scenario == "unused")
            {
                Assert.Single(actualDefinitions); Assert.DoesNotContain(native.Summary.Entries, entry => entry.ExceptionType.Equals(actualDefinitions[0].Type));
                Assert.Contains(native.DeclaredFindings, item => item.GetProperty("smell").GetProperty("id").GetString() == "DOC632");
            }
            var graph = await Probe(payload);
            Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32());
            Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray());
            Assert.Equal(native.Summaries, graph.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()).ToArray());
            Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean());
            Assert.All(graph.GetProperty("ownership").EnumerateArray(), item => Assert.True(item.GetProperty("stableModel").GetBoolean()));
            Assert.True(graph.GetProperty("foreignTreeRejected").GetBoolean()); Assert.True(graph.GetProperty("foreignBindingRejected").GetBoolean());
            Stopped(call); Stopped(repeat); NoHistoricalLoaded();
            var disabled = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions }, new(new(Client())));
            Assert.False(disabled.Succeeded); Assert.Null(disabled.Dispatch);
            // A record clone cannot mint a receipt; mutating raw DTO arrays cannot mutate the validated immutable snapshot.
            var sample = native.Expected.First(); var sampleQuery = new DeclaredFindingPolicyQuery(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, sample.Type, sample.Kind);
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(call with { }, sampleQuery).Failure!.Code);
            string evidence = JsonSerializer.Serialize(call.DeclaredPolicy, WorkerProtocol.JsonOptions);
            if (actualDefinitions.Length > 0) { actualDefinitions[0] = new(null, null); }
            Assert.Equal(sample.Relevant, DeclaredExceptionFindingPolicy.Evaluate(call, sampleQuery).Decision!.Relevant);
            await Evidence("case-" + scenario, new
            {
                Scenario = scenario,
                Native = native,
                Historical = call.Result,
                PolicyEvidence = JsonSerializer.Deserialize<JsonElement>(evidence),
                RepeatedPolicyEvidence = repeat.DeclaredPolicy,
                Decisions = decisions,
                Graph = graph,
                call.Provenance,
                call.Identity,
                FirstProcess = call.ProcessId,
                FirstProcessExit = call.ProcessExit,
                SecondProcess = repeat.ProcessId,
                SecondProcessExit = repeat.ProcessExit,
                OriginalValidatedProjection = true,
                NoFallback = true,
                ProcessesStopped = true,
                NoHistoricalLoadedInMain = true,
                DeclaredStillUnsupported = true,
                NoFindingImport = true,
                CloneRejected = true,
                ImmutableReceipt = true
            });
        }

        [Theory]
        [InlineData("missing-policy")]
        [InlineData("missing-type")]
        [InlineData("assembly")]
        [InlineData("module")]
        [InlineData("missing-source")]
        [InlineData("foreign-source")]
        [InlineData("missing-manifest")]
        [InlineData("missing-count")]
        [InlineData("unused-omission-recounted")]
        [InlineData("duplicate-type")]
        [InlineData("missing-direct")]
        [InlineData("foreign-direct-path")]
        [InlineData("unknown-evidence-kind")]
        [InlineData("missing-mode")]
        [InlineData("default-mode")]
        [InlineData("conflicting-profile")]
        public async Task MissingConflictingOrForgedPolicyResponse_FailsClosed(string scenario)
        {
            var payload = Payload(scenario == "unused-omission-recounted" ? "unused" : "mixed");
            var native = Native(payload);
            var input = RawOptIn(payload, native);
            var call = await Client().AnalyzeAsync(input); Assert.True(call.Success, call.Failure?.Message);
            var response = JsonSerializer.SerializeToNode(new WorkerResponse(WorkerProtocol.Version, "analyze", true, call.Identity, call.Result,
                Provenance: call.Provenance, DeclaredPolicy: call.DeclaredPolicy), WorkerProtocol.JsonOptions)!;
            var policy = response["declaredPolicy"]!;
            if (scenario == "missing-policy") { response.AsObject().Remove("declaredPolicy"); }
            if (scenario == "missing-type") { policy["declarations"]![0]!.AsObject().Remove("type"); }
            if (scenario == "assembly") { policy["declarations"]![0]!["type"]!["assembly"]!["versionMajor"] = 321; policy["declarations"]![0]!["type"]!["module"]!["assembly"]!["versionMajor"] = 321; }
            if (scenario == "module") { policy["declarations"]![0]!["type"]!["module"]!["name"] = "Foreign.dll"; }
            if (scenario == "missing-source") { policy["declarations"]![0]!.AsObject().Remove("declaringSourcePaths"); }
            if (scenario == "foreign-source") { policy["declarations"]![0]!["declaringSourcePaths"]![0] = "/foreign/Helper.cs"; }
            if (scenario == "missing-manifest") { policy.AsObject().Remove("reportingSourcePaths"); }
            if (scenario == "missing-count") { policy.AsObject().Remove("declarationCount"); }
            if (scenario == "unused-omission-recounted") { policy["declarations"] = new JsonArray(); policy["declarationCount"] = 0; }
            if (scenario == "duplicate-type") { policy["declarations"]!.AsArray().Add(policy["declarations"]![0]!.DeepClone()); policy["declarationCount"] = 2; }
            if (scenario == "missing-direct") { policy.AsObject().Remove("directResult"); }
            if (scenario == "foreign-direct-path") { policy["directResult"]!["entries"]![0]!["paths"]![0]!["steps"]![0]!["filePath"] = "/project-a/Helper.cs"; }
            if (scenario == "unknown-evidence-kind") { policy["directResult"]!["entries"]![0]!["evidenceKind"] = "declared"; }
            if (scenario == "missing-mode") { policy.AsObject().Remove("mode"); }
            if (scenario == "default-mode") { policy["mode"] = "unspecified"; }
            if (scenario == "conflicting-profile") { policy["profile"] = "partial-v0"; }
            var rejected = HistoricalWorkerResponseValidation.Validate(response.ToJsonString(), "", 0, input);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, rejected.Failure!.Code); Assert.Null(rejected.Result); Assert.Null(rejected.DeclaredPolicy);
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected,
                new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code);
            Stopped(call); NoHistoricalLoaded();
            await Evidence("negative-response-" + scenario, new { Scenario = scenario, rejected.Failure, NoPolicyReceipt = true, NoResult = true, NoFallback = true, ProcessesStopped = true });
        }

        [Fact]
        public async Task QueryAdmissionAndLegacyEvidence_AreExplicitFailClosed_NoDefaultOrRelevantGuess()
        {
            var payload = Payload("mixed"); var native = Native(payload); var call = await Client().AnalyzeAsync(RawOptIn(payload, native)); Assert.True(call.Success);
            var valid = new DeclaredFindingPolicyQuery(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Direct);
            Assert.Equal(DeclaredFindingPolicyFailureCode.MissingTypeIdentity, DeclaredExceptionFindingPolicy.Evaluate(call, valid with { ExceptionType = null }).Failure!.Code);
            foreach (var mode in new WorkerDeclaredPolicyMode?[] { null, WorkerDeclaredPolicyMode.Unspecified, (WorkerDeclaredPolicyMode)999 })
            { Assert.Equal(DeclaredFindingPolicyFailureCode.InvalidMode, DeclaredExceptionFindingPolicy.Evaluate(call, valid with { Mode = mode }).Failure!.Code); }
            foreach (var kind in new[] { DeclaredFindingSourceKind.Unspecified, (DeclaredFindingSourceKind)999 })
            { Assert.Equal(DeclaredFindingPolicyFailureCode.InvalidSourceKind, DeclaredExceptionFindingPolicy.Evaluate(call, valid with { SourceKind = kind }).Failure!.Code); }
            var unknown = native.NonException;
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnknownExceptionType, DeclaredExceptionFindingPolicy.Evaluate(call, valid with { ExceptionType = unknown }).Failure!.Code);
            var legacy = await Client().AnalyzeAsync(payload); Assert.True(legacy.Success); Assert.Null(legacy.DeclaredPolicy);
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(legacy, valid).Failure!.Code);
            Stopped(call); Stopped(legacy); NoHistoricalLoaded();
            await Evidence("query-negative", new
            {
                MissingType = true,
                MissingDefaultInvalidMode = true,
                UnknownSourceKind = true,
                UnknownTypeRejected = true,
                LegacyCannotProducePolicyDecision = true,
                NoFallback = true,
                ProcessesStopped = true
            });
        }

        [Fact]
        public async Task IncompleteRequestOriginalReceiptsAndUnsupportedShapes_DoNotActivateOrDowngrade()
        {
            var payload = Payload("local"); var native = Native(payload); var raw = RawOptIn(payload, native); var request = raw.Context!.DeclaredPolicy!;
            var badRequests = new[] { request with { Mode = null }, request with { Mode = WorkerDeclaredPolicyMode.Unspecified }, request with { Profile = null }, request with { ExpectedDeclarations = null } };
            foreach (var bad in badRequests)
            { var result = await Client().AnalyzeAsync(raw with { Context = raw.Context with { DeclaredPolicy = bad } }); Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code); Assert.Null(result.ProcessId); }
            foreach (var mode in new[] { "direct", "solution-transitive", "project-transitive-declared-exceptions" })
            { Assert.False(WorkerInputValidation.IsSupported(raw with { Context = raw.Context with { AnalysisMode = mode } })); }
            var original = await Original(payload);
            Assert.False(HistoricalWorkerPayloadProjection.Project(original with { DeclaredPolicy = request }).Succeeded); // caller-invented inventory
            Assert.False(HistoricalWorkerPayloadProjection.Project(original with { DeclaredPolicy = OptIn(), SourceSet = null }).Succeeded);
            Assert.False(HistoricalWorkerPayloadProjection.Project(original with { DeclaredPolicy = OptIn(), SupportingProjects = ImmutableArray<HistoricalWorkerPayloadProjectionInput>.Empty }).Succeeded);
            var copied = new ExternalCSharpSyntaxTreeSet(original.SourceSet!.Trees, original.SourceSet.SourceMaterials);
            Assert.False(HistoricalWorkerPayloadProjection.Project(original with { DeclaredPolicy = OptIn(), SourceSet = copied }).Succeeded);
            var generic = await Original(Payload("generic"));
            Assert.False(HistoricalWorkerPayloadProjection.Project(generic with { DeclaredPolicy = OptIn() }).Succeeded);
            var mismatch = await Client().AnalyzeAsync(raw with { Context = raw.Context with { DeclaredPolicy = request with { ExpectedDeclarations = [] } } });
            Assert.Equal(WorkerFailureCode.UnsupportedContext, mismatch.Failure!.WorkerFailure!.Code); Assert.Null(mismatch.Result); Assert.Null(mismatch.DeclaredPolicy); Stopped(mismatch);
            var uncertain = Payload("uncertain"); var uncertainNative = Native(uncertain);
            var failed = await Client().AnalyzeAsync(RawOptIn(uncertain, uncertainNative));
            Assert.Equal(WorkerFailureCode.UnsupportedContext, failed.Failure!.WorkerFailure!.Code); Assert.Null(failed.DeclaredPolicy); Assert.Null(failed.Result); Stopped(failed);
            NoHistoricalLoaded();
            await Evidence("request-negative", new
            {
                RequiredModeProfileAndInventory = true,
                OtherModesRefused = true,
                CallerInventoryRefused = true,
                MissingCopiedScopeRefused = true,
                SolutionScopeRefused = true,
                GenericRefused = true,
                ActualInventoryMismatch = mismatch.Failure,
                Uncertainty = uncertainNative.Summary.Uncertainties,
                ActualUncertainWorker = failed.Failure,
                NoPartialPolicy = true,
                NoFallback = true,
                ProcessesStopped = true
            });
        }

        private sealed record Expected(CanonicalTypeIdentity Type, DeclaredFindingSourceKind Kind, bool Local, bool Relevant, string DisplayName);
        private sealed record Snapshot(CanonicalExceptionFlowAnalysisResult Summary, CanonicalExceptionFlowAnalysisResult Direct,
            WorkerExceptionDeclaration[] Declarations, bool HasDeclaredTypes, Expected[] Expected, JsonElement[] ProjectFindings,
            JsonElement[] DeclaredFindings, int GraphCount, string[] Summaries, CanonicalTypeIdentity NonException);
        private static Snapshot Native(WorkerAnalysisInput payload)
        {
            var context = payload.Context!;
            var documents = new[] { new WorkerSourceDocument(payload.LogicalPath!, payload.Source!, "", ParseContext: context.ParseContext) }.Concat(context.AdditionalSources!).ToArray();
            var trees = context.Scope!.OrderedSourcePaths!.Select(path => CSharpSyntaxTree.ParseText(documents.Single(item => item.LogicalPath == path).Text,
                new CSharpParseOptions(LanguageVersion.CSharp12, DocumentationMode.Parse, preprocessorSymbols: context.ParseContext!.PreprocessorSymbols), path)).ToArray();
            var compilation = CSharpCompilation.Create(context.AssemblyName, trees, context.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image)), new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var rootTree = trees.Single(tree => tree.FilePath == payload.LogicalPath);
            var root = rootTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var semantic = ProjectClosureSemanticContext.CreateSingleCompilationContext(rootTree, compilation);
            var environment = new ExceptionFlowSemanticEnvironment(semantic); var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(environment);
            var direct = ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions(root, environment); var summary = session.Analyze(root);
            var canonicalDirect = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(direct); var canonicalSummary = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(summary);
            Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)));
            var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
            var summaries = (string[])typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Summaries", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [graph])!;
            var exceptionBase = compilation.GetTypeByMetadataName("System.Exception")!;
            var definitions = new ExceptionFlowSemanticScope(compilation).SourceTypes.Where(type => type.InheritsFromOrEquals(exceptionBase))
                .Select(type => new WorkerExceptionDeclaration(RoslynCanonicalIdentityFactory.CreateTypeIdentity(type), trees.Where(tree => type.DeclaringSyntaxReferences.Any(reference => ReferenceEquals(reference.SyntaxTree, tree))).Select(tree => tree.FilePath).ToArray()))
                .OrderBy(item => JsonSerializer.Serialize(item.Type, WorkerProtocol.JsonOptions), StringComparer.Ordinal).ToArray();
            var thrownRelevant = typeof(XmlDocExceptionSemanticDetector).GetMethod("IsRelevantThrownException", BindingFlags.Static | BindingFlags.NonPublic)!;
            var declaredOptions = new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions };
            var expected = new List<Expected>();
            var symbols = direct.ThrownExceptions.Concat(summary.ThrownExceptions).Concat(new ExceptionFlowSemanticScope(compilation).SourceTypes.Where(type => type.InheritsFromOrEquals(exceptionBase)))
                .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var symbol in symbols)
            {
                bool local = semantic.IsDeclaredInReportingScope(symbol);
                bool relevant = (bool)thrownRelevant.Invoke(null, [symbol, exceptionBase, declaredOptions, semantic])!;
                Assert.Equal(local, relevant);
                var type = RoslynCanonicalIdentityFactory.CreateTypeIdentity(symbol);
                expected.Add(new(type, DeclaredFindingSourceKind.Direct, local, direct.ThrownExceptions.Contains(symbol), symbol.ToDisplayString()));
                expected.Add(new(type, DeclaredFindingSourceKind.Summary, local, semantic.HasDeclaredExceptionTypesInReportingScope()
                    && summary.ThrownExceptions.Contains(symbol) && !direct.ThrownExceptions.Contains(symbol) && relevant, symbol.ToDisplayString()));
                expected.Add(new(type, DeclaredFindingSourceKind.Documented, local, relevant, symbol.ToDisplayString()));
            }
            JsonElement[] Findings(ExceptionAnalysisMode mode)
            {
                var options = new XmlDocOptions { ExceptionAnalysisMode = mode };
                var first = XmlDocExceptionSemanticDetector.FindExceptionSmells(rootTree, payload.LogicalPath!, compilation.GetSemanticModel(rootTree), semantic, options, session);
                Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(rootTree, payload.LogicalPath!, compilation.GetSemanticModel(rootTree), semantic, options, session)));
                return JsonSerializer.SerializeToElement(first, WorkerProtocol.JsonOptions).EnumerateArray().ToArray();
            }
            foreach (var tree in trees) { Assert.True(environment.TryGetSemanticModel(tree, out var first)); Assert.True(environment.TryGetSemanticModel(tree, out var repeat)); Assert.Same(first, repeat); Assert.Same(compilation, first.Compilation); }
            return new(canonicalSummary, canonicalDirect, definitions, semantic.HasDeclaredExceptionTypesInReportingScope(), expected.ToArray(),
                Findings(ExceptionAnalysisMode.ProjectTransitive), Findings(ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions), graph.Count, summaries,
                RoslynCanonicalIdentityFactory.CreateTypeIdentity(compilation.GetTypeByMetadataName("Fixture")!));
        }

        private static WorkerAnalysisInput Payload(string scenario)
        {
            var input = (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, ["project-transitive", "12", 2, true, false])!;
            string body = "Helper.A();"; string docs = "/// <summary>Observe policy eligibility.</summary>\r\n";
            string helper = "public static class Helper { " + Values + " public static void A() { if (Flag) throw Local; else throw External; } }\r\n" + Local;
            if (scenario == "external") { helper = "public static class Helper { public static System.InvalidOperationException External = null!; public static void A() { throw External; } }"; }
            if (scenario == "mixed") { body = "if (Helper.Flag) throw new System.ArgumentException(); else Helper.A();"; helper = "public static class Helper { " + Values + " public static void A() { if (Flag) throw Local; else throw External; } }\r\n" + Local; }
            if (scenario == "combined") { body = "if (Helper.Flag) throw new LocalException(); else Helper.A();"; helper = "public static class Helper { " + Values + " public static void A() { if (Flag) throw Local; else throw External; } }\r\n" + Local; }
            if (scenario == "unused") { docs += "/// <exception cref=\"LocalException\">Not thrown.</exception>\r\n"; helper = "public static class Helper { " + Values + " public static void A() { throw External; } }\r\n" + Local; }
            if (scenario == "generic") { helper += "\r\npublic class GenericException<T> : System.Exception { }"; }
            if (scenario == "uncertain") { body = "System.GC.Collect();"; }
            string target = "public static class Fixture {\r\n" + docs + "public static void Root() { " + body + " } }";
            if (scenario == "partial") { target += "\r\npublic partial class LocalException { }"; }
            if (scenario == "nested")
            {
                helper = helper.Replace("public static LocalException Local", "public static Container.LocalException Local", StringComparison.Ordinal).Replace(Local,
                "public class Container { " + Local + " }", StringComparison.Ordinal);
            }
            var source = input.Context!.AdditionalSources![0];
            return input with { Source = target, Context = input.Context with { AdditionalSources = [source with { Text = helper, Sha256 = WorkerProtocol.SourceHash(helper) }] } };
        }
        private static WorkerAnalysisInput RawOptIn(WorkerAnalysisInput payload, Snapshot snapshot)
            => payload with { Context = payload.Context! with { DeclaredPolicy = OptIn() with { ExpectedDeclarations = snapshot.Declarations.Select(item => item.Type!).ToArray() } } };
        private static Task<HistoricalWorkerPayloadProjectionInput> Original(WorkerAnalysisInput payload) => (Task<HistoricalWorkerPayloadProjectionInput>)typeof(ExceptionFlowHistoricalScopeTests).GetMethod("ValidatedInput", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload])!;
        private static Task<JsonElement> Probe(WorkerAnalysisInput payload) => (Task<JsonElement>)typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Probe", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload, "--b14-graph", null])!;
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static void Stopped(HistoricalWorkerCallResult call)
            => HistoricalWorkerLifetimeAssertions.Stopped(call);
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        {
            string path = Path.Combine(Root(), "artifacts", "p5o2b17", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
