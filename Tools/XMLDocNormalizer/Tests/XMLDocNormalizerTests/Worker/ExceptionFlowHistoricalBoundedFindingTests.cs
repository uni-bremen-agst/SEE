using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Worker
{
    public sealed class ExceptionFlowHistoricalBoundedFindingTests
    {
        [Theory]
        [InlineData(false, "mixed")]
        [InlineData(false, "external")]
        [InlineData(false, "local")]
        [InlineData(false, "exact")]
        [InlineData(false, "base")]
        [InlineData(false, "invalid")]
        [InlineData(false, "alias")]
        [InlineData(false, "inheritdoc")]
        [InlineData(false, "no-doc")]
        [InlineData(false, "method")]
        [InlineData(false, "combined")]
        [InlineData(false, "empty")]
        [InlineData(false, "direct-only")]
        [InlineData(true, "local")]
        [InlineData(true, "chain")]
        [InlineData(true, "mixed")]
        [InlineData(true, "external")]
        [InlineData(true, "exact")]
        [InlineData(true, "base")]
        [InlineData(true, "invalid")]
        [InlineData(true, "alias")]
        [InlineData(true, "inheritdoc")]
        [InlineData(true, "no-doc")]
        [InlineData(true, "unused")]
        [InlineData(true, "method")]
        [InlineData(true, "combined")]
        [InlineData(true, "empty")]
        public async Task GenuineModeContract_FullNativeFindingEvidenceAndRepeat(bool project, string scenario)
        {
            var payload = Payload(scenario);
            var native = Native(payload);
            var production = JsonSerializer.SerializeToElement(Invoke(typeof(ExceptionFlowHistoricalProductionAdmissionTests), "Production", payload), WorkerProtocol.JsonOptions);
            var expected = production.EnumerateArray().Single(item => item.GetProperty("mode").GetString() == (project ? "projectTransitive" : "direct"));
            var input = await Input(payload, project);
            var first = await Find(input); var repeat = await Find(input);
            Assert.True(first.Succeeded, Failure(first)); Assert.True(repeat.Succeeded, Failure(repeat));
            Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings));
            Assert.Equal(Json(first.Findings), Json(repeat.Findings));
            bool summary = project && scenario != "no-doc";
            Assert.Equal(summary, first.SummaryRequested); Assert.Equal(summary, repeat.SummaryRequested);
            Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0);
            var firstCall = first.Direct.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.True(HistoricalWorkerResponseValidation.TryGetFindingFacts(firstCall, out var direct));
            Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult));
            Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult));
            var selectedRun = first.Summary ?? first.Direct;
            var call = selectedRun.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.True(HistoricalWorkerResponseValidation.TryGetFindingFacts(call, out var facts));
            Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult));
            var documentNative = JsonSerializer.SerializeToNode(native)!;
            if (!summary) { documentNative["summary"] = JsonSerializer.SerializeToNode(direct.DirectResult, WorkerProtocol.JsonOptions); }
            var document = (WorkerDeclaredDocumentationEvidence)Invoke(typeof(ExceptionFlowHistoricalFindingContractTests), "NativeDocument", payload,
                JsonSerializer.SerializeToElement(documentNative))!;
            document = document with { Profile = WorkerFindingContract.Profile, Mode = project ? WorkerDeclaredPolicyMode.ProjectTransitive : WorkerDeclaredPolicyMode.Direct };
            Assert.Equal(Json(document), Json(call.DeclaredDocumentation));
            Assert.Equal(Json(facts.Documentation.Method), Json(document.Method));
            Assert.Equal(Json(selectedRun.PayloadProjection.Value!.Payload.Context!.Scope), facts.OwnershipSnapshot);
            Assert.Equal(WorkerProtocol.Provenance(selectedRun.PayloadProjection.Value.Payload), call.Provenance);
            Assert.Equal(project ? "project-transitive" : "direct", call.Provenance!.AnalysisMode);
            Assert.Empty(facts.FlowResult.Uncertainties); Assert.All(facts.FlowResult.Entries, entry => Assert.False(entry.PathsTruncated));
            Assert.Null(firstCall.DeclaredPolicy); Assert.Null(call.DeclaredPolicy);
            Assert.False(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(call, out _));
            Assert.False(HistoricalWorkerResponseValidation.TryGetFindingFacts(call with { }, out _));
            var repeatedCall = (repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.True(HistoricalWorkerResponseValidation.TryGetFindingFacts(repeatedCall, out var repeatedFacts));
            Assert.NotSame(facts.DocumentationExecution, repeatedFacts!.DocumentationExecution);
            Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult));
            Assert.Equal(Json(facts.Documentation), Json(repeatedFacts.Documentation));
            object? graph = null;
            if (summary)
            {
                var graphResult = (Task<JsonElement>)Invoke(typeof(ExceptionFlowHistoricalScopeTests), "Probe", payload, "--b14-graph", null)!;
                var actual = await graphResult;
                Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32());
                Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText());
                Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText());
                Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText());
                Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean()); graph = actual;
            }
            if (scenario == "external" && project) { Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611"); }
            if (scenario == "local" && !project) { Assert.Empty(first.Findings); } // A callee throw is not Direct.
            foreach (var run in new[] { first.Direct, first.Summary, repeat.Direct, repeat.Summary }.Where(run => run != null))
            {
                Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, run!.Dispatch!.Decision!.Selection);
                Assert.Null(run.Dispatch.Routing!.CurrentAnalysis); AssertCompletedExchange(run.Dispatch.Routing.HistoricalAnalysis!);
            }
            NoHistoricalLoaded();
            await Evidence($"positive-{project}-{scenario}", new
            {
                Project = project,
                Scenario = scenario,
                Native = native,
                Production = expected,
                first.Findings,
                Direct = direct.DirectResult,
                Flow = facts.FlowResult,
                Document = document,
                Facts = new
                {
                    facts.Mode,
                    facts.Execution,
                    facts.Documentation,
                    facts.OwnershipSnapshot
                },
                call.Provenance,
                call.Identity,
                Graph = graph,
                FullFindingParity = true,
                FullEvidenceParity = true,
                RepeatEqual = true,
                NoDeclaredInventory = true,
                NoFallback = true
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UncertainSummary_IsNotRequestedByDirect_AndFailsClosedForProject(bool project)
        {
            var payload = Payload("guard-direct");
            var result = await Find(await Input(payload, project));
            if (project)
            {
                Assert.False(result.Succeeded); Assert.True(result.GuardReached); Assert.True(result.SummaryRequested); Assert.Empty(result.Findings);
                Assert.Equal(WorkerFailureCode.UnsupportedContext, result.Summary!.Dispatch!.Routing!.HistoricalAnalysis!.Failure!.WorkerFailure!.Code);
            }
            else
            {
                Assert.True(result.Succeeded, Failure(result)); Assert.False(result.SummaryRequested);
                Assert.Equal("DOC610", Assert.Single(result.Findings).Smell.ID);
            }
            Assert.Null((result.Summary ?? result.Direct).Dispatch!.Routing!.CurrentAnalysis);
            await Evidence("uncertain-" + project, new { Project = project, result.Succeeded, result.SummaryRequested, result.Findings, Failure = Failure(result), NoFallback = true });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MissingTamperedReplayAndWrongModeEvidence_CannotMintFindingReceipt(bool project)
        {
            var original = await Input(Payload("exact"), project);
            var execution = project ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence;
            original = original with { Finding = original.Finding! with { Execution = execution } };
            var projected = HistoricalWorkerPayloadProjection.ProjectForFindings(original, execution);
            Assert.True(projected.Succeeded); var input = projected.Value!.Payload;
            var call = await Client().AnalyzeAsync(input); Assert.True(call.Success, call.Failure?.Message);
            Assert.True(HistoricalWorkerResponseValidation.TryGetFindingFacts(call, out var facts));
            var cases = new[] { "root", "owner", "type", "direct", "result", "document", "bindings", "tamper", "mode", "summary-conflict", "unattested", "consumed", "cloned-input" };
            foreach (string scenario in cases)
            {
                var node = JsonSerializer.SerializeToNode(new WorkerResponse(WorkerProtocol.Version, "analyze", true, call.Identity, call.Result,
                    Provenance: call.Provenance, DeclaredDocumentation: call.DeclaredDocumentation, FindingDirect: facts!.DirectResult), WorkerProtocol.JsonOptions)!;
                if (scenario == "root") { node["declaredDocumentation"]!.AsObject().Remove("method"); }
                if (scenario == "owner") { node["declaredDocumentation"]!["syntax"]!["logicalPath"] = "/foreign/Root.cs"; }
                if (scenario == "type") { node["findingDirect"]!["entries"]![0]!["exceptionType"]!.AsObject().Remove("assembly"); }
                if (scenario == "direct") { node.AsObject().Remove("findingDirect"); }
                if (scenario == "result") { node.AsObject().Remove("result"); }
                if (scenario == "document") { node.AsObject().Remove("declaredDocumentation"); }
                if (scenario == "bindings") { node["declaredDocumentation"]!.AsObject().Remove("bindings"); }
                if (scenario == "tamper") { node["declaredDocumentation"]!["bindings"]![0]!["isException"] = false; }
                if (scenario == "mode") { node["declaredDocumentation"]!["mode"] = project ? "direct" : "projectTransitive"; }
                if (scenario == "summary-conflict") { node["result"]!["entries"] = new JsonArray(); }
                var request = scenario == "cloned-input" ? input with { } : input;
                var rejected = HistoricalWorkerResponseValidation.Validate(node.ToJsonString(), call.Diagnostics, 0, request, call.ProcessId,
                    scenario == "unattested" ? null : facts.DocumentationExecution);
                Assert.False(rejected.Success); Assert.Null(rejected.Result); Assert.False(HistoricalWorkerResponseValidation.TryGetFindingFacts(rejected, out _));
                await Evidence($"negative-{project}-{scenario}", new { Project = project, Execution = execution, Scenario = scenario, rejected.Failure, NoReceipt = true, NoFallback = true });
            }
            var noOrigin = await Client().AnalyzeAsync(input with { });
            Assert.False(noOrigin.Success); Assert.False(HistoricalWorkerResponseValidation.TryGetFindingFacts(noOrigin, out _));
            foreach (var invalid in new[]
            {
                original with { AnalysisMode = ExceptionAnalysisMode.SolutionTransitive },
                original with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions },
                original with { Finding = original.Finding! with { Documentation = original.Finding!.Documentation! with { Profile = "wrong" } } },
                original with { Root = null! },
                original with { SourceSet = null },
                original with { Source = null! }
            })
            { Assert.False(HistoricalWorkerPayloadProjection.ProjectForFindings(invalid, execution).Succeeded); }
            var wrong = input with { Context = input.Context! with { AnalysisMode = "solution-transitive" } };
            Assert.False(WorkerInputValidation.IsSupported(wrong));
            foreach (var context in new[] { input.Context! with { AdditionalSources = [] },
                input.Context! with { Scope = input.Context!.Scope! with { RootLogicalPath = "/foreign.cs" } } })
            {
                var missing = await Client().AnalyzeAsync(input with { Context = context });
                Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, missing.Failure!.Code); Assert.Null(missing.ProcessId);
            }
            string immutable = Json(facts!.Documentation);
            if (call.DeclaredDocumentation!.Bindings!.Length > 0) { call.DeclaredDocumentation.Bindings[0] = new(null, null, null, null); }
            Assert.Equal(immutable, Json(facts.Documentation));
            AssertCompletedExchange(call); AssertCompletedExchange(noOrigin); NoHistoricalLoaded();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GenuineResponseFromWrongPhysicalWorker_AndWorkerFailure_CannotReport(bool project)
        {
            var input = await Input(Payload("exact"), project);
            var projected = HistoricalWorkerPayloadProjection.ProjectForFindings(input, WorkerDeclaredExecution.DirectEvidence);
            var genuine = await Client().AnalyzeAsync(projected.Value!.Payload);
            Assert.True(genuine.Success, genuine.Failure?.Message);
            Assert.True(HistoricalWorkerResponseValidation.TryGetFindingFacts(genuine, out var facts));
            string directory = Path.Combine(Root(), "artifacts/p5o2b24/physical-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string fixture = Path.Combine(Root(), "artifacts/p5o2b4/test-process/bin/Debug/net8.0/BoundaryTestProcess");
            string peer = Path.Combine(directory, "XMLDocNormalizer.HistoricalWorker.dll");
            File.Copy(fixture + ".dll", peer);
            File.Copy(fixture + ".runtimeconfig.json", Path.ChangeExtension(peer, ".runtimeconfig.json"));
            await File.WriteAllTextAsync(Path.Combine(directory, "b21-replayed-worker-response.json"), Json(new WorkerResponse(2, "analyze", true,
                genuine.Identity, genuine.Result, Provenance: genuine.Provenance, DeclaredDocumentation: genuine.DeclaredDocumentation, FindingDirect: facts!.DirectResult)));
            foreach (string worker in new[] { peer, Path.Combine(directory, "missing-worker.dll") })
            {
                var rejected = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input,
                    new(new(new HistoricalWorkerClient("dotnet", worker, TimeSpan.FromSeconds(60)))));
                Assert.False(rejected.Succeeded); Assert.False(rejected.GuardReached); Assert.False(rejected.SummaryRequested); Assert.Empty(rejected.Findings);
                Assert.Null(rejected.Direct.Dispatch!.Routing!.CurrentAnalysis);
                Assert.False(HistoricalWorkerResponseValidation.TryGetFindingFacts(rejected.Direct.Dispatch.Routing.HistoricalAnalysis, out _));
                if (worker == peer)
                { Assert.Contains("exact B21 execution", rejected.Direct.Dispatch.Routing.HistoricalAnalysis!.Failure!.Message, StringComparison.Ordinal); }
                await Evidence($"physical-{project}-{Path.GetFileName(worker)}", new
                {
                    Project = project,
                    Worker = worker,
                    Failure = rejected.Direct.Dispatch.Routing.HistoricalAnalysis!.Failure,
                    NoGuard = true,
                    NoFallback = true
                });
            }
            AssertCompletedExchange(genuine); NoHistoricalLoaded();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task RealCli_ExplicitJobMode_UsesOrdinaryJsonExactlyOnce(bool project, bool empty)
        {
            string body = empty ? "" : project ? "Helper();" : "throw new System.ArgumentException();";
            string source = "public static class Fixture {\r\n/// <summary>Selected root.</summary>\r\npublic static void Root() { " + body
                + " }\r\npublic static void Helper() { throw new System.ArgumentException(); } }";
            var task = (Task)Invoke(typeof(ExceptionFlowExternalTargetCliTests), "Create", source, false, false, false)!;
            await task;
            var fixture = task.GetType().GetProperty("Result")!.GetValue(task)!;
            string jobPath = (string)fixture.GetType().GetProperty("Job")!.GetValue(fixture)!;
            var job = JsonNode.Parse(await File.ReadAllTextAsync(jobPath))!;
            job["analysisMode"] = project ? "project-transitive" : "direct";
            await File.WriteAllTextAsync(jobPath, job.ToJsonString());
            var runTask = (Task)Invoke(typeof(ExceptionFlowExternalTargetCliTests), "Run", fixture, "json", Array.Empty<string>())!;
            await runTask;
            var run = JsonSerializer.SerializeToElement(runTask.GetType().GetProperty("Result")!.GetValue(runTask), WorkerProtocol.JsonOptions);
            Assert.Equal(empty ? 0 : 1, run.GetProperty("exitCode").GetInt32()); Assert.Equal("", run.GetProperty("stderr").GetString());
            var report = JsonDocument.Parse(await File.ReadAllTextAsync(run.GetProperty("report").GetString()!));
            var findings = report.RootElement.GetProperty("Findings").EnumerateArray().ToArray();
            Assert.Equal(empty ? 0 : 1, findings.Length);
            Assert.Equal(findings.Length, report.RootElement.GetProperty("Metrics").GetProperty("FindingCount").GetInt32());
            if (!empty) { Assert.Contains(project ? "DOC611" : "DOC610", findings[0].GetRawText()); Assert.Contains("1 documentation issue(s)", run.GetProperty("stdout").GetString()); }
            Assert.DoesNotContain("historical", report.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
            await Evidence($"cli-{project}-{empty}", new
            {
                Project = project,
                Empty = empty,
                Run = run,
                Report = report.RootElement.Clone(),
                SingleCounting = true,
                SharedJsonReporter = true
            });
        }

        [Theory]
        [InlineData("direct", true)]
        [InlineData("project-transitive", true)]
        [InlineData("solution-transitive", false)]
        [InlineData("unknown", false)]
        public async Task RealCli_ConflictingOrUnsupportedJobMode_FailsClosed(string mode, bool conflictingAssertion)
        {
            var task = (Task)Invoke(typeof(ExceptionFlowExternalTargetCliTests), "Create",
                "public static class Fixture { /// <summary>Root.</summary>\r\npublic static void Root() { } }", false, false, false)!;
            await task; var fixture = task.GetType().GetProperty("Result")!.GetValue(task)!;
            string path = (string)fixture.GetType().GetProperty("Job")!.GetValue(fixture)!;
            var job = JsonNode.Parse(await File.ReadAllTextAsync(path))!; job["analysisMode"] = mode;
            await File.WriteAllTextAsync(path, job.ToJsonString());
            var extra = conflictingAssertion ? new[] { "--exception-analysis-mode", "project-transitive-declared-exceptions" } : Array.Empty<string>();
            var runTask = (Task)Invoke(typeof(ExceptionFlowExternalTargetCliTests), "Run", fixture, "json", extra)!;
            await runTask;
            var run = JsonSerializer.SerializeToElement(runTask.GetType().GetProperty("Result")!.GetValue(runTask), WorkerProtocol.JsonOptions);
            Assert.Equal(4, run.GetProperty("exitCode").GetInt32()); Assert.False(File.Exists(run.GetProperty("report").GetString()));
            Assert.DoesNotContain("Check succeeded", run.GetProperty("stdout").GetString());
            Assert.Contains(conflictingAssertion ? "conflicts" : "not admitted", run.GetProperty("stderr").GetString());
            await Evidence("cli-negative-" + mode, new { Mode = mode, ConflictingAssertion = conflictingAssertion, Run = run, NoFallback = true });
        }

        [Fact]
        public void Boundary_NewModesNeverUseDeclaredEligibilityOrMutateCurrentPolicy()
        {
            string external = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs"));
            string independent = external[(external.IndexOf("private static async Task<ExternalExceptionFindingAnalysisResult> FindIndependent", StringComparison.Ordinal))..];
            Assert.Contains("RequiresSummary(input.AnalysisMode, false)", independent);
            Assert.Contains("RenderBoundedFindingEvidence", independent); Assert.DoesNotContain(".Declarations", independent);
            string worker = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs"));
            Assert.DoesNotContain("AddExceptionFindings", worker); Assert.DoesNotContain("RequiresSummary", worker);
            Assert.DoesNotContain("FindExceptionSmells", worker); NoHistoricalLoaded();
            // A B21 receipt is minted only after the owned handle has exited and both pipes reached EOF.
            // Looking up a numeric PID later can observe a different, recycled process (or an inaccessible one).
            string client = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs"));
            Assert.Contains("Task exchange = ExchangeAsync();", client);
            Assert.Contains("List<Task> pending = [stdout, stderr, exchange]", client);
            Assert.Contains("await process.WaitForExitAsync(operation.Token)", client);
            Assert.True(client.IndexOf("await completed;", StringComparison.Ordinal) < client.IndexOf("documentationExecutions.Add", StringComparison.Ordinal));
        }

        private static WorkerAnalysisInput Payload(string scenario)
        {
            var payload = (WorkerAnalysisInput)Invoke(typeof(ExceptionFlowHistoricalDeclaredProductionFindingTests), "Payload", scenario == "empty" ? "local" : scenario)!;
            if (scenario == "empty") { payload = payload with { Source = payload.Source!.Replace("Helper.A();", "", StringComparison.Ordinal) }; }
            if (scenario == "direct-only") { payload = payload with { Source = payload.Source!.Replace("if (Helper.Flag) throw new System.ArgumentException(); else Helper.A();", "throw new System.ArgumentException();", StringComparison.Ordinal) }; }
            return payload;
        }
        private static JsonElement Native(WorkerAnalysisInput input) => (JsonElement)Invoke(typeof(ExceptionFlowHistoricalDeclaredProductionFindingTests), "Native", input)!;
        private static async Task<HistoricalWorkerPayloadProjectionInput> Input(WorkerAnalysisInput payload, bool project)
        {
            var original = await (Task<HistoricalWorkerPayloadProjectionInput>)Invoke(typeof(ExceptionFlowHistoricalScopeTests), "ValidatedInput", payload)!;
            return original with
            {
                AnalysisMode = project ? ExceptionAnalysisMode.ProjectTransitive : ExceptionAnalysisMode.Direct,
                DeclaredPolicy = null,
                Finding = new(WorkerDeclaredExecution.DirectEvidence, new(WorkerFindingContract.Profile,
                    project ? WorkerDeclaredPolicyMode.ProjectTransitive : WorkerDeclaredPolicyMode.Direct))
            };
        }
        private static object? Invoke(Type type, string name, params object?[] args) => type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);
        private static Task<ExternalExceptionFindingAnalysisResult> Find(HistoricalWorkerPayloadProjectionInput input) => XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input, new(new(Client())));
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        private static string[] Set(object value) => (value is JsonElement element ? element : JsonSerializer.SerializeToElement(value, WorkerProtocol.JsonOptions))
            .EnumerateArray().Select(item => item.GetRawText()).OrderBy(item => item, StringComparer.Ordinal).ToArray();
        private static string Failure(ExternalExceptionFindingAnalysisResult result) => result.Failure + ": " + result.Direct.PayloadProjection.Failure?.Message
            + ": " + result.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + result.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message;
        private static void AssertCompletedExchange(HistoricalWorkerCallResult call)
        {
            Assert.True(call.ProcessId is > 0);
            if (call.Success)
            {
                Assert.True(HistoricalWorkerResponseValidation.TryGetFindingFacts(call, out var facts));
                Assert.NotNull(facts!.DocumentationExecution); // Exact completed execution, not a mutable DTO or PID lookup.
            }
            else { Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, call.Failure!.Code); }
        }
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        { string path = Path.Combine(Root(), "artifacts/p5o2b24/cases", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!); await File.WriteAllTextAsync(path, Json(value)); }
    }
}
