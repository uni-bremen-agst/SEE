using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Worker
{
    public sealed class ExceptionFlowHistoricalDeclaredProductionFindingTests
    {
        [Theory]
        [InlineData("local")]
        [InlineData("external")]
        [InlineData("mixed")]
        [InlineData("combined")]
        [InlineData("unused")]
        [InlineData("partial")]
        [InlineData("nested")]
        [InlineData("exact")]
        [InlineData("base")]
        [InlineData("inheritdoc")]
        [InlineData("invalid")]
        [InlineData("no-doc")]
        [InlineData("alias")]
        [InlineData("method")]
        [InlineData("duplicate")]
        [InlineData("empty-tag")]
        [InlineData("chain")]
        [InlineData("guard-direct")]
        public async Task ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat(string scenario)
        {
            var payload = Payload(scenario);
            var native = Native(payload);
            var production = Production(payload);
            var declared = production.Single(item => item.GetProperty("mode").GetString() == "projectTransitiveDeclaredExceptions");
            var input = (await Original(payload)) with { DeclaredPolicy = OptIn(), AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions };
            var first = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input, Dispatch());
            var repeat = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input, Dispatch());
            Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message
                + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message);
            Assert.True(repeat.Succeeded, repeat.Failure);
            Assert.True(first.GuardReached); Assert.True(repeat.GuardReached);
            Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings));
            Assert.Equal(Json(first.Findings), Json(repeat.Findings));
            bool expectedSummary = scenario != "no-doc" && native.GetProperty("hasDeclaredTypes").GetBoolean();
            Assert.Equal(expectedSummary, first.SummaryRequested); Assert.Equal(expectedSummary, repeat.SummaryRequested);
            Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0);
            var direct = first.Direct.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(direct, WorkerDeclaredExecution.DirectEvidence, out var directFacts));
            Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult));
            Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result));
            Assert.Equal(native.GetProperty("declarations").GetRawText(), directFacts.OwnershipSnapshot);
            var selected = first.Summary?.Dispatch!.Routing!.HistoricalAnalysis ?? direct;
            Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected,
                expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts));
            Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult));
            var documentInput = JsonSerializer.SerializeToNode(native)!;
            if (!expectedSummary) { documentInput["summary"] = JsonSerializer.SerializeToNode(facts.DirectResult, WorkerProtocol.JsonOptions); }
            var nativeDocument = typeof(ExceptionFlowHistoricalFindingContractTests).GetMethod("NativeDocument", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [payload, JsonSerializer.SerializeToElement(documentInput)])!;
            Assert.Equal(Json(nativeDocument), Json(selected.DeclaredDocumentation));
            Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result));
            Assert.NotNull(facts.DocumentationExecution);
            Assert.True(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis, out var repeatedFacts));
            Assert.NotSame(facts.DocumentationExecution, repeatedFacts!.DocumentationExecution);
            Assert.Equal("project-transitive-declared-exceptions", selected.Provenance!.AnalysisMode);
            Assert.Equal(WorkerProtocol.Provenance((first.Summary ?? first.Direct).PayloadProjection.Value!.Payload), selected.Provenance);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, first.Direct.Dispatch.Decision!.Selection);
            Assert.Null(first.Direct.Dispatch.Routing.CurrentAnalysis);
            Assert.Null(first.Summary?.Dispatch?.Routing?.CurrentAnalysis);
            Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { },
                expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _));
            var project = production.Single(item => item.GetProperty("mode").GetString() == "projectTransitive");
            var projectCall = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input with
            { AnalysisMode = ExceptionAnalysisMode.ProjectTransitive }, Dispatch());
            JsonElement? projectFindings = null;
            JsonElement? graph = null;
            if (scenario == "guard-direct")
            {
                Assert.False(projectCall.Succeeded); Assert.Equal(WorkerFailureCode.AnalysisFailure,
                    projectCall.Dispatch!.Routing!.HistoricalAnalysis!.Failure!.WorkerFailure!.Code);
                Assert.Single(first.Findings); Assert.Equal("DOC610", first.Findings[0].Smell.ID);
                Assert.Equal(0, declared.GetProperty("graphCount").GetInt32());
            }
            else
            {
                Assert.True(projectCall.Succeeded, projectCall.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message);
                Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result));
                var projectResult = projectCall.Dispatch!.Routing!.HistoricalAnalysis!;
                Assert.True(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(projectResult, out var projectFacts));
                Assert.NotNull(projectFacts!.DocumentationExecution);
                // Differential of the exact same Main renderer over genuine B21 Project
                // evidence. This does not activate a general Project reporting API.
                var rendered = typeof(XmlDocExceptionSemanticDetector).GetMethod("RenderFindingEvidence", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, [projectFacts, ExceptionAnalysisMode.ProjectTransitive])!;
                projectFindings = JsonSerializer.SerializeToElement(rendered, WorkerProtocol.JsonOptions);
                Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value));
                if (scenario is "chain" or "local" or "combined")
                {
                    graph = await Probe(payload);
                    Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32());
                    Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()));
                    Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()));
                    Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText());
                    Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean());
                }
            }
            if (scenario is "local" or "external")
            {
                Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")));
                Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings));
            }
            if (scenario == "local") { Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611"); }
            if (scenario == "mixed")
            { Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610"); Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611"); }
            // Old evidence-only mode activation remains fail closed, not a renamed engine.
            var legacy = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, Dispatch());
            Assert.False(legacy.Succeeded); Assert.Null(legacy.Dispatch);
            foreach (var run in new[] { first.Direct, first.Summary, repeat.Direct, repeat.Summary, projectCall }.Where(run => run != null))
            { Stopped(run!.Dispatch!.Routing!.HistoricalAnalysis!); }
            NoHistoricalLoaded();
            await Evidence("positive-" + scenario, new
            {
                Scenario = scenario,
                Native = native,
                Production = production,
                Historical = Snapshot(first),
                Repeated = Snapshot(repeat),
                Facts = FactsSnapshot(facts),
                DirectFacts = FactsSnapshot(directFacts),
                NativeDocument = nativeDocument,
                ProjectControl = projectCall.Dispatch!.Routing!.HistoricalAnalysis,
                HistoricalProjectPolicyFindings = projectFindings,
                HistoricalAuxiliaryGraphProbe = graph,
                FullFindingSetParity = true,
                GuardBeforeSummary = true,
                B21Required = true,
                RepeatEqual = true,
                LegacyEvidenceOnlyUnsupported = true,
                NoFallback = true,
                ProcessesStopped = true
            });
        }

        [Theory]
        [InlineData("missing-document")]
        [InlineData("missing-type")]
        [InlineData("missing-declarations")]
        [InlineData("missing-ownership")]
        [InlineData("missing-direct")]
        [InlineData("missing-summary")]
        [InlineData("conflicting-flow")]
        [InlineData("invalid-context")]
        [InlineData("wrong-mode")]
        [InlineData("missing-attestation")]
        [InlineData("malformed-attestation")]
        [InlineData("consumed-replay")]
        [InlineData("request-mismatch")]
        [InlineData("response-mismatch")]
        [InlineData("binding-tamper")]
        [InlineData("target-substitution")]
        [InlineData("source-substitution")]
        public async Task InvalidContractOrReplay_CannotReachGuardOrFindingPolicy(string scenario)
        {
            var input = (await Original(Payload("exact"))) with { DeclaredPolicy = OptIn(), AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions };
            var projected = HistoricalWorkerPayloadProjection.ProjectForFindings(input, WorkerDeclaredExecution.DirectEvidence);
            Assert.True(projected.Succeeded); var prepared = projected.Value!.Payload;
            var genuine = await Client().AnalyzeAsync(prepared); Assert.True(genuine.Success, genuine.Failure?.Message);
            Assert.True(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(genuine, out var facts));
            var response = JsonSerializer.SerializeToNode(new WorkerResponse(WorkerProtocol.Version, "analyze", true, genuine.Identity, genuine.Result,
                Provenance: genuine.Provenance, DeclaredPolicy: genuine.DeclaredPolicy, DeclaredDocumentation: genuine.DeclaredDocumentation), WorkerProtocol.JsonOptions)!;
            if (scenario == "missing-document") { response.AsObject().Remove("declaredDocumentation"); }
            if (scenario == "missing-type") { response["declaredDocumentation"]!["typeDisplays"] = new JsonArray(); }
            if (scenario == "missing-declarations") { response["declaredPolicy"]!.AsObject().Remove("declarations"); }
            if (scenario == "missing-ownership") { response["declaredPolicy"]!["declarations"]![0]!.AsObject().Remove("declaringSourcePaths"); }
            if (scenario == "missing-direct") { response["declaredPolicy"]!.AsObject().Remove("directResult"); }
            if (scenario == "missing-summary") { response.AsObject().Remove("result"); }
            if (scenario == "conflicting-flow") { response["result"]!["entries"] = new JsonArray(); }
            if (scenario == "invalid-context") { response["declaredDocumentation"]!["syntax"]!["anchor"] = -1; }
            if (scenario == "wrong-mode") { response["declaredDocumentation"]!["mode"] = "unspecified"; }
            if (scenario == "response-mismatch") { response["identity"]!["worker"]!["mvid"] = Guid.Empty.ToString(); }
            if (scenario == "binding-tamper")
            {
                var binding = response["declaredDocumentation"]!["bindings"]![0]!;
                binding["kind"] = "unresolved"; binding.AsObject().Remove("type"); binding.AsObject().Remove("isException");
            }
            if (scenario == "target-substitution") { response["declaredDocumentation"]!["method"]!["metadataName"] = "Other"; }
            if (scenario == "source-substitution") { response["declaredDocumentation"]!["syntax"]!["logicalPath"] = "/foreign/Root.cs"; }
            var request = scenario == "request-mismatch" ? prepared with { } : prepared;
            var attestation = scenario == "malformed-attestation" ? new HistoricalWorkerClient.DocumentationExecution()
                : scenario == "missing-attestation" ? null : facts!.DocumentationExecution;
            var rejected = HistoricalWorkerResponseValidation.Validate(response.ToJsonString(), genuine.Diagnostics, 0, request, genuine.ProcessId, attestation);
            Assert.False(rejected.Success); Assert.Null(rejected.Result);
            Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(rejected, WorkerDeclaredExecution.DirectEvidence, out _));
            Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(genuine with { }, WorkerDeclaredExecution.DirectEvidence, out _));
            Stopped(genuine); NoHistoricalLoaded();
            await Evidence("negative-" + scenario, new
            {
                Scenario = scenario,
                Rejection = rejected.Failure,
                NoGuardAdmission = true,
                NoFindingPolicy = true,
                NoFallback = true,
                ProcessesStopped = true
            });
        }

        [Theory]
        [InlineData("direct")]
        [InlineData("project")]
        [InlineData("solution")]
        [InlineData("default")]
        [InlineData("missing-document")]
        [InlineData("missing-sources")]
        [InlineData("cross-project")]
        public async Task UnsupportedFindingScopes_AreRejectedBeforeDispatch(string scenario)
        {
            var input = (await Original(Payload("local"))) with { DeclaredPolicy = OptIn(), AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions };
            if (scenario == "direct") { input = input with { AnalysisMode = ExceptionAnalysisMode.Direct }; }
            if (scenario == "project") { input = input with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitive }; }
            if (scenario == "solution") { input = input with { AnalysisMode = ExceptionAnalysisMode.SolutionTransitive }; }
            if (scenario == "default") { input = input with { AnalysisMode = (ExceptionAnalysisMode)999 }; }
            if (scenario == "missing-document") { input = input with { DeclaredPolicy = input.DeclaredPolicy! with { Documentation = null } }; }
            if (scenario == "missing-sources") { input = input with { SourceSet = null }; }
            if (scenario == "cross-project") { input = input with { SupportingProjects = [] }; }
            var rejected = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input, Dispatch());
            Assert.False(rejected.Succeeded); Assert.False(rejected.GuardReached); Assert.False(rejected.SummaryRequested);
            Assert.Empty(rejected.Findings); Assert.Null(rejected.Direct.Dispatch);
            await Evidence("scope-" + scenario, new
            {
                Scenario = scenario,
                rejected.Failure,
                rejected.GuardReached,
                rejected.SummaryRequested,
                rejected.Findings,
                ProjectionFailure = rejected.Direct.PayloadProjection.Failure,
                NoDispatch = true
            });
        }

        [Fact]
        public async Task SelectedSummaryWorkerFailure_DoesNotPublishPartialDirectFindings()
        {
            var payload = Payload("mixed");
            payload = payload with { Source = payload.Source!.Replace("Helper.A();", "System.GC.Collect();", StringComparison.Ordinal) };
            var input = (await Original(payload)) with { DeclaredPolicy = OptIn(), AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions };
            var result = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input, Dispatch());
            Assert.False(result.Succeeded); Assert.True(result.GuardReached); Assert.True(result.SummaryRequested); Assert.Empty(result.Findings);
            Assert.True(result.Direct.Succeeded); Assert.NotEmpty(result.Direct.Result!.Entries);
            Assert.Equal(WorkerFailureCode.AnalysisFailure, result.Summary!.Dispatch!.Routing!.HistoricalAnalysis!.Failure!.WorkerFailure!.Code);
            Assert.Null(result.Summary.Result); Assert.Null(result.Direct.Dispatch!.Routing!.CurrentAnalysis); Assert.Null(result.Summary.Dispatch.Routing.CurrentAnalysis);
            Stopped(result.Direct.Dispatch.Routing.HistoricalAnalysis!); Stopped(result.Summary.Dispatch.Routing.HistoricalAnalysis!); NoHistoricalLoaded();
            await Evidence("worker-error", new { Historical = Snapshot(result), NoPartialFindings = true, NoFallback = true, ProcessesStopped = true });
        }

        [Fact]
        public async Task B21ReceiptWithoutProductivePurpose_AndWrongPurpose_CannotReachGuard()
        {
            var input = (await Original(Payload("mixed"))) with { DeclaredPolicy = OptIn() };
            var legacy = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, Dispatch());
            Assert.True(legacy.Succeeded);
            Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(legacy.Dispatch!.Routing!.HistoricalAnalysis, WorkerDeclaredExecution.DirectEvidence, out _));
            var direct = HistoricalWorkerPayloadProjection.ProjectForFindings(input with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions }, WorkerDeclaredExecution.DirectEvidence);
            var call = await Client().AnalyzeAsync(direct.Value!.Payload); Assert.True(call.Success);
            Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(call, WorkerDeclaredExecution.SummaryEvidence, out _));
            Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(call, WorkerDeclaredExecution.Unspecified, out _));
            Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(call, (WorkerDeclaredExecution)999, out _));
            Stopped(call); Stopped(legacy.Dispatch.Routing.HistoricalAnalysis!); NoHistoricalLoaded();
            await Evidence("purpose-rejection", new { NoGuardAdmission = true, NoFindingPolicy = true, NoFallback = true, ProcessesStopped = true });
        }

        [Fact]
        public void PermanentBoundary_MainOwnsGuardAndPolicy_WorkerOnlyExecutesExplicitPurpose()
        {
            string detector = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs"));
            string findings = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.Findings.cs"));
            string external = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs"));
            string worker = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs"));
            Assert.Contains("RequiresSummary(options.ExceptionAnalysisMode", detector);
            Assert.Contains("AddExceptionFindings<INamedTypeSymbol>", detector);
            Assert.Contains("AddExceptionFindings(findings", external);
            Assert.True(external.IndexOf("if (!TryAdmitFindingEvidence", StringComparison.Ordinal) < external.IndexOf("RequiresSummary(input!", StringComparison.Ordinal));
            Assert.Contains("original!.DocumentationExecution == null", external);
            Assert.Contains("original.Execution != execution", external);
            Assert.Contains("ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions || hasDeclaredTypes", findings);
            Assert.DoesNotContain("RequiresSummary", worker); Assert.DoesNotContain("FindingPolicy", worker);
            Assert.DoesNotContain("IsDeclaredInReportingScope", worker); Assert.DoesNotContain("FindExceptionSmells", worker);
            Assert.Contains("WorkerDeclaredExecution.DirectEvidence", worker);
            Assert.DoesNotContain("CSharpCompilation", external); Assert.DoesNotContain("GetSemanticModel", external);
            Assert.DoesNotContain("Assembly.Load", external); NoHistoricalLoaded();
            // Preserve the old detector's lazy missing-Finding anchor evaluation:
            // an incomplete member at EOF with no Finding must not resolve an anchor.
            var incomplete = CSharpSyntaxTree.ParseText("/// <summary>Incomplete.</summary>\r\nclass",
                new CSharpParseOptions(LanguageVersion.CSharp12, DocumentationMode.Parse), "/incomplete.cs");
            var compilation = CSharpCompilation.Create("Incomplete", [incomplete],
                [MetadataReference.CreateFromFile(typeof(Exception).Assembly.Location)], new(OutputKind.DynamicallyLinkedLibrary));
            Assert.Empty(XmlDocExceptionSemanticDetector.FindExceptionSmells(incomplete, incomplete.FilePath, compilation.GetSemanticModel(incomplete)));
        }

        [Fact]
        public async Task ExactProductiveResponseFromWrongPhysicalWorker_IsRejectedBeforeGuard()
        {
            var input = (await Original(Payload("exact"))) with { DeclaredPolicy = OptIn(), AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions };
            var projection = HistoricalWorkerPayloadProjection.ProjectForFindings(input, WorkerDeclaredExecution.DirectEvidence);
            var genuine = await Client().AnalyzeAsync(projection.Value!.Payload); Assert.True(genuine.Success);
            string directory = Path.Combine(Root(), "artifacts", "p5o2b22", "physical-peer-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string fixture = Path.Combine(Root(), "artifacts", "p5o2b4", "test-process", "bin", "Debug", "net8.0", "BoundaryTestProcess");
            string peer = Path.Combine(directory, "XMLDocNormalizer.HistoricalWorker.dll");
            File.Copy(fixture + ".dll", peer);
            File.Copy(fixture + ".runtimeconfig.json", Path.Combine(directory, "XMLDocNormalizer.HistoricalWorker.runtimeconfig.json"));
            var response = new WorkerResponse(WorkerProtocol.Version, "analyze", true, genuine.Identity, genuine.Result,
                Provenance: genuine.Provenance, DeclaredPolicy: genuine.DeclaredPolicy, DeclaredDocumentation: genuine.DeclaredDocumentation);
            await File.WriteAllTextAsync(Path.Combine(directory, "b21-replayed-worker-response.json"), Json(response));
            var rejected = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input,
                new(new(new HistoricalWorkerClient("dotnet", peer, TimeSpan.FromSeconds(60)))));
            Assert.False(rejected.Succeeded); Assert.False(rejected.GuardReached); Assert.False(rejected.SummaryRequested); Assert.Empty(rejected.Findings);
            var call = rejected.Direct.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, call.Failure!.Code);
            Assert.Contains("execution attestation", call.Failure.Message, StringComparison.Ordinal);
            Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(call, WorkerDeclaredExecution.DirectEvidence, out _));
            Assert.Null(rejected.Direct.Dispatch.Routing.CurrentAnalysis);
            Stopped(genuine); Stopped(call); NoHistoricalLoaded();
            await Evidence("physical-worker", new
            {
                Rejection = call.Failure,
                GenuineWorker = genuine.Identity,
                FakeProcess = call.ProcessId,
                NoGuardAdmission = true,
                NoFindingPolicy = true,
                ExactGenuineProductiveResponse = true,
                NoFallback = true,
                ProcessesStopped = true
            });
        }

        private static WorkerAnalysisInput Payload(string scenario)
        {
            var payload = (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalFindingContractTests).GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [scenario is "chain" ? "local" : scenario is "guard-direct" ? "external" : scenario])!;
            if (scenario == "chain")
            {
                var helper = payload.Context!.AdditionalSources![0];
                string text = helper.Text.Replace("public static void A() { if (Flag) throw Local; else throw External; }",
                    "public static void A() { B(); } public static void B() { throw Local; }", StringComparison.Ordinal);
                payload = payload with { Context = payload.Context with { AdditionalSources = [helper with { Text = text, Sha256 = WorkerProtocol.SourceHash(text) }] } };
            }
            if (scenario == "guard-direct")
            { payload = payload with { Source = payload.Source!.Replace("Helper.A();", "if (Helper.External == null) throw new System.ArgumentException(); else System.GC.Collect();", StringComparison.Ordinal) }; }
            return payload;
        }
        private static JsonElement Native(WorkerAnalysisInput payload) => JsonSerializer.SerializeToElement(typeof(ExceptionFlowHistoricalDeclaredPolicyTests)
            .GetMethod("Native", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload]), WorkerProtocol.JsonOptions);
        private static JsonElement[] Production(WorkerAnalysisInput payload) => JsonSerializer.SerializeToElement(typeof(ExceptionFlowHistoricalProductionAdmissionTests)
            .GetMethod("Production", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload]), WorkerProtocol.JsonOptions).EnumerateArray().ToArray();
        private static Task<HistoricalWorkerPayloadProjectionInput> Original(WorkerAnalysisInput payload) => (Task<HistoricalWorkerPayloadProjectionInput>)typeof(ExceptionFlowHistoricalScopeTests)
            .GetMethod("ValidatedInput", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload])!;
        private static Task<JsonElement> Probe(WorkerAnalysisInput payload) => (Task<JsonElement>)typeof(ExceptionFlowHistoricalScopeTests)
            .GetMethod("Probe", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload, "--b14-graph", null])!;
        private static WorkerDeclaredPolicyRequest OptIn() => new(WorkerDeclaredPolicyContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions,
            Documentation: new(WorkerDeclaredDocumentationContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions));
        private static ExceptionFlowAnalysisDispatch Dispatch() => new(new(Client()));
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        private static object Snapshot(ExternalExceptionFindingAnalysisResult result) => new
        {
            result.Findings,
            result.GuardReached,
            result.SummaryRequested,
            result.Failure,
            Direct = result.Direct.Dispatch!.Routing!.HistoricalAnalysis,
            Summary = result.Summary?.Dispatch?.Routing?.HistoricalAnalysis
        };
        private static object FactsSnapshot(DeclaredPolicyFacts facts) => new
        {
            facts.Documentation,
            facts.DirectResult,
            facts.FlowResult,
            facts.OwnershipSnapshot,
            facts.Execution,
            B21OriginalExecutionReceipt = facts.DocumentationExecution != null,
            Declarations = facts.Declarations.OrderBy(type => Json(type), StringComparer.Ordinal).ToArray()
        };
        private static string[] FindingSet(object value)
        {
            var json = value is JsonElement element ? element : JsonSerializer.SerializeToElement(value, WorkerProtocol.JsonOptions);
            return json.EnumerateArray().Select(item => item.GetRawText()).OrderBy(item => item, StringComparer.Ordinal).ToArray();
        }
        private static void Stopped(HistoricalWorkerCallResult call)
        { Assert.NotNull(call.ProcessId); try { using var process = Process.GetProcessById(call.ProcessId.Value); Assert.True(process.HasExited); } catch (ArgumentException) { } }
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        { string path = Path.Combine(Root(), "artifacts", "p5o2b22", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!); await File.WriteAllTextAsync(path, Json(value)); }
    }
}
