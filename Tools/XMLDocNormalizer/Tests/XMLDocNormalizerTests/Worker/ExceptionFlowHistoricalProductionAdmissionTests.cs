using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    /// <summary>Original B20 attack fixtures; B21 now rejects their unchanged coherent substitutions.</summary>
    public sealed class ExceptionFlowHistoricalProductionAdmissionTests
    {
        [Theory]
        [InlineData("unresolved")]
        [InlineData("other-symbol")]
        [InlineData("redirect-named")]
        public async Task CoherentBindingSubstitution_IsRejectedWithoutExecutionAttestation_AndCannotEnterProduction(string substitution)
        {
            var payload = Payload("exact");
            if (substitution == "redirect-named")
            { payload = payload with { Source = payload.Source!.Replace("cref=\"System.ArgumentException\"", "cref=\"System.String\"", StringComparison.Ordinal) }; }
            var native = Native(payload);
            var input = (await Original(payload)) with { DeclaredPolicy = OptIn() };
            var projection = HistoricalWorkerPayloadProjection.Project(input); Assert.True(projection.Succeeded);
            var prepared = projection.Value!.Payload;
            var genuine = await Client().AnalyzeAsync(prepared); var repeat = await Client().AnalyzeAsync(prepared);
            Assert.True(genuine.Success, genuine.Failure?.Message); Assert.True(repeat.Success, repeat.Failure?.Message);
            Assert.Equal(Json(genuine.DeclaredDocumentation), Json(repeat.DeclaredDocumentation));
            Assert.Equal(native.GetProperty("summary").GetRawText(), Json(genuine.Result));
            Assert.Equal(native.GetProperty("direct").GetRawText(), Json(genuine.DeclaredPolicy!.DirectResult));
            var before = DeclaredFindingDocumentationContract.Read(genuine, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions);
            Assert.True(before.Succeeded); var realTag = Assert.Single(before.Facts!.Tags);
            Assert.Equal(WorkerCrefBindingKind.NamedType, realTag.Kind);
            Assert.Equal(substitution != "redirect-named", realTag.IsException);
            Assert.Equal(substitution == "redirect-named", Has(native, "DOC670"));
            Assert.Equal(substitution == "redirect-named", Has(native, "DOC610"));
            Assert.False(Has(native, "DOC660"));

            var response = JsonSerializer.SerializeToNode(new WorkerResponse(WorkerProtocol.Version, "analyze", true,
                genuine.Identity, genuine.Result, Provenance: genuine.Provenance, DeclaredPolicy: genuine.DeclaredPolicy,
                DeclaredDocumentation: genuine.DeclaredDocumentation), WorkerProtocol.JsonOptions)!;
            var document = response["declaredDocumentation"]!; var tag = document["bindings"]![0]!;
            if (substitution == "redirect-named")
            {
                // This identity is a real original PE type, already present in the independently proven Direct/Summary.
                // Its inheritance flag and display are correct; only its association with the original cref is false.
                tag["type"] = JsonSerializer.SerializeToNode(Assert.Single(genuine.DeclaredPolicy!.DirectResult!.Entries).ExceptionType, WorkerProtocol.JsonOptions);
                tag["isException"] = true;
                var displays = document["typeDisplays"]!.AsArray();
                for (int index = displays.Count - 1; index >= 0; index--)
                { if (displays[index]!["type"]!["metadataName"]!.GetValue<string>() == "String") { displays.RemoveAt(index); } }
            }
            else
            {
                tag["kind"] = substitution == "unresolved" ? "unresolved" : "otherSymbol";
                tag.AsObject().Remove("type"); tag.AsObject().Remove("isException");
            }
            string modifiedJson = response.ToJsonString();
            // The original prepared payload/source receipt is retained. No cloned payload or changed source is involved.
            // B21 must reject the exact B20 bypass, even with the original source/payload receipt retained.
            var admitted = HistoricalWorkerResponseValidation.Validate(modifiedJson, "", 0, prepared);
            var admittedRepeat = HistoricalWorkerResponseValidation.Validate(modifiedJson, "", 0, prepared);
            Assert.False(admitted.Success); Assert.Null(admitted.ProcessId); Assert.Null(admitted.Result); Assert.Null(admitted.DeclaredDocumentation);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, admitted.Failure!.Code);
            Assert.Contains("execution attestation", admitted.Failure.Message, StringComparison.Ordinal);
            Assert.Equal(Json(admitted.Failure), Json(admittedRepeat.Failure));
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence,
                DeclaredFindingDocumentationContract.Read(admitted, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Failure!.Code);
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence,
                DeclaredFindingDocumentationContract.Read(admittedRepeat, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Failure!.Code);
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence,
                DeclaredFindingDocumentationContract.Read(admitted with { }, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Failure!.Code);
            await RejectDeclared(input); Stopped(genuine); Stopped(repeat); NoHistoricalLoaded();
            await Evidence("admission-" + substitution, new
            {
                Substitution = substitution,
                Native = native,
                GenuineDocument = genuine.DeclaredDocumentation,
                ModifiedDocument = JsonSerializer.Deserialize<JsonElement>(document.ToJsonString()),
                GenuineFacts = before.Facts,
                Rejection = admitted.Failure,
                RepeatedRejection = admittedRepeat.Failure,
                Direct = genuine.DeclaredPolicy!.DirectResult,
                Summary = genuine.Result,
                Declarations = genuine.DeclaredPolicy.Declarations,
                genuine.Provenance,
                genuine.Identity,
                GenuineProcess = genuine.ProcessId,
                RepeatedProcess = repeat.ProcessId,
                OriginalPayloadRetained = true,
                SourceAndSpansUnchanged = true,
                DirectSummaryHashesUnchanged = true,
                CoherentSubstitutionAccepted = false,
                NoEvidenceReceiptWithoutExecution = true,
                BindingAssociationAttestationRequired = true,
                CompleteFindingParityNotProved = true,
                ProductionStillUnsupported = true,
                NoFallback = true,
                ProcessesStopped = true,
                NoHistoricalLoadedInMain = true
            });
        }

        [Theory]
        [InlineData("local")]
        [InlineData("external")]
        [InlineData("guard")]
        [InlineData("mixed")]
        [InlineData("chain")]
        [InlineData("unused")]
        [InlineData("no-doc")]
        public async Task ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation(string scenario)
        {
            var payload = Payload(scenario is "guard" ? "external" : scenario is "chain" ? "local" : scenario);
            if (scenario == "guard") { payload = payload with { Source = payload.Source!.Replace("Helper.A();", "System.GC.Collect();", StringComparison.Ordinal) }; }
            if (scenario == "chain")
            {
                var helper = payload.Context!.AdditionalSources![0];
                string text = helper.Text!.Replace("public static void A() { if (Flag)", "public static void A() { B(); } public static void B() { if (Flag)", StringComparison.Ordinal);
                Assert.NotEqual(helper.Text, text);
                payload = payload with { Context = payload.Context with { AdditionalSources = [helper with { Text = text, Sha256 = WorkerProtocol.SourceHash(text) }] } };
            }
            var native = Native(payload); var production = Production(payload);
            var declared = production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions);
            Assert.Equal(native.GetProperty("declaredFindings").GetRawText(), Json(declared.Findings));
            Assert.Equal(native.GetProperty("projectFindings").GetRawText(), Json(production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitive).Findings));
            if (scenario is "external" or "guard" or "no-doc") { Assert.Equal(0, declared.GraphCount); }
            else { Assert.True(declared.GraphCount > 0); }
            Assert.Equal(0, production.Single(item => item.Mode == ExceptionAnalysisMode.Direct).GraphCount);
            if (scenario is "local" or "mixed" or "chain")
            {
                Assert.True(Has(native, "DOC611"));
                Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText());
            }
            var input = (await Original(payload)) with { DeclaredPolicy = OptIn() };
            var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            var call = first.Dispatch!.Routing!.HistoricalAnalysis!; var repeat = second.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.Null(first.Dispatch.Routing.CurrentAnalysis); Assert.Null(second.Dispatch.Routing.CurrentAnalysis);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, first.Dispatch.Decision!.Selection);
            if (scenario == "guard")
            {
                Assert.False(first.Succeeded); Assert.False(second.Succeeded); Assert.Null(call.Result); Assert.Null(call.DeclaredDocumentation);
                Assert.Equal(WorkerFailureCode.AnalysisFailure, call.Failure!.WorkerFailure!.Code); Assert.Equal(Json(call.Failure), Json(repeat.Failure));
                Assert.Empty(declared.Findings); // Existing guard boundary, not misreported as a newly discovered gap.
            }
            else
            {
                Assert.True(first.Succeeded, call.Failure?.Message); Assert.True(second.Succeeded, repeat.Failure?.Message);
                Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result));
                Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult));
                Assert.Equal(native.GetProperty("declarations").GetRawText(), Json(call.DeclaredPolicy.Declarations));
                Assert.Equal(Json(call.Result), Json(repeat.Result)); Assert.Equal(Json(call.DeclaredDocumentation), Json(repeat.DeclaredDocumentation));
                Assert.True(DeclaredFindingDocumentationContract.Read(call, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Succeeded);
            }
            await RejectDeclared(input); Stopped(call); Stopped(repeat); NoHistoricalLoaded();
            await Evidence("production-" + scenario, new
            {
                Scenario = scenario,
                Native = native,
                ActualProduction = production,
                Historical = call,
                RepeatedHistorical = repeat,
                GuardObservedThroughProduction = true,
                EvidenceOnly = true,
                HistoricalFinalFindingsAvailable = false,
                ProductionStillUnsupported = true,
                NoFallback = true,
                ProcessesStopped = true,
                NoHistoricalLoadedInMain = true
            });
        }

        private sealed record ProductionSnapshot(ExceptionAnalysisMode Mode, JsonElement[] Findings, int GraphCount, string[] Summaries);
        private static ProductionSnapshot[] Production(WorkerAnalysisInput payload)
        {
            var context = payload.Context!;
            var sources = new[] { new WorkerSourceDocument(payload.LogicalPath!, payload.Source!, "", ParseContext: context.ParseContext) }.Concat(context.AdditionalSources!);
            var trees = context.Scope!.OrderedSourcePaths!.Select(path => CSharpSyntaxTree.ParseText(sources.Single(source => source.LogicalPath == path).Text,
                new CSharpParseOptions(LanguageVersion.CSharp12, DocumentationMode.Parse, preprocessorSymbols: context.ParseContext!.PreprocessorSymbols), path)).ToArray();
            var compilation = CSharpCompilation.Create(context.AssemblyName, trees, context.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image)),
                new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var tree = trees.Single(tree => tree.FilePath == payload.LogicalPath);
            var semantic = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            return Enum.GetValues<ExceptionAnalysisMode>().Select(mode =>
            {
                var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(new(semantic));
                var options = new XmlDocOptions { ExceptionAnalysisMode = mode };
                string first = Json(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, payload.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session));
                Assert.Equal(first, Json(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, payload.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)));
                var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
                var summaries = (string[])typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Summaries", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [graph])!;
                return new ProductionSnapshot(mode, JsonSerializer.Deserialize<JsonElement[]>(first)!, graph.Count, summaries);
            }).ToArray();
        }
        private static WorkerAnalysisInput Payload(string scenario) => (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalFindingContractTests)
            .GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [scenario])!;
        private static JsonElement Native(WorkerAnalysisInput payload) => JsonSerializer.SerializeToElement(typeof(ExceptionFlowHistoricalDeclaredPolicyTests)
            .GetMethod("Native", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload]), WorkerProtocol.JsonOptions);
        private static Task<HistoricalWorkerPayloadProjectionInput> Original(WorkerAnalysisInput payload) => (Task<HistoricalWorkerPayloadProjectionInput>)typeof(ExceptionFlowHistoricalScopeTests)
            .GetMethod("ValidatedInput", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload])!;
        private static WorkerDeclaredPolicyRequest OptIn() => new(WorkerDeclaredPolicyContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions,
            Documentation: new(WorkerDeclaredDocumentationContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions));
        private static async Task RejectDeclared(HistoricalWorkerPayloadProjectionInput input)
        {
            var rejected = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions }, new(new(Client())));
            Assert.False(rejected.Succeeded); Assert.Null(rejected.Result); Assert.Null(rejected.Dispatch);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, rejected.PayloadProjection.Failure!.Code);
        }
        private static bool Has(JsonElement native, string id) => native.GetProperty("declaredFindings").EnumerateArray().Any(item => item.GetProperty("smell").GetProperty("id").GetString() == id);
        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static void Stopped(HistoricalWorkerCallResult call)
            => HistoricalWorkerLifetimeAssertions.Stopped(call);
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        {
            string path = Path.Combine(Root(), "artifacts", "p5o2b20", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, Json(value));
        }
    }
}
