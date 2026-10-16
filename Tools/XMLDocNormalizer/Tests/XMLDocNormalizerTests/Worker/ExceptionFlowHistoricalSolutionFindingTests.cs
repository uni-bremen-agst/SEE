using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>B25 stop-rule regression: preserve B15 evidence, but do not activate final Solution Findings.</summary>
    public sealed class ExceptionFlowHistoricalSolutionFindingTests
    {
        [Theory]
        [InlineData(2, "chain")]
        [InlineData(3, "chain")]
        [InlineData(2, "source-type")]
        [InlineData(2, "mixed")]
        [InlineData(3, "mixed")]
        [InlineData(2, "multiple")]
        [InlineData(3, "multiple")]
        [InlineData(3, "supporting-sources")]
        [InlineData(2, "empty")]
        [InlineData(3, "empty")]
        [InlineData(2, "exact")]
        [InlineData(2, "base")]
        [InlineData(2, "invalid")]
        [InlineData(2, "inheritdoc")]
        [InlineData(2, "no-doc")]
        public async Task ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission(int count, string scenario)
        {
            var fixture = await Create(count, scenario);
            var projection = HistoricalWorkerPayloadProjection.Project(fixture.Input);
            Assert.True(projection.Succeeded, projection.Failure?.Message);
            var payload = projection.Value!.Payload; var native = Native(payload);
            var first = await Flow(fixture.Input); var repeat = await Flow(fixture.Input);
            Assert.True(first.Succeeded, Failure(first)); Assert.True(repeat.Succeeded, Failure(repeat));
            Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result));
            Assert.Equal(Json(first.Result), Json(repeat.Result));
            var graph = await Probe(payload, "--b15-graph", null);
            Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32());
            Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText());
            Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText());
            Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText());
            Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean());
            var owners = payload.Context!.Solution!.Projects!.SelectMany(node => node.Input!.Context!.Scope!.OrderedSourcePaths!
                .Select(path => new { Path = path, node.Identity })).ToDictionary(item => item.Path, item => item.Identity);
            foreach (var owner in native.GetProperty("ownership").EnumerateArray())
            { Assert.Equal(owner.GetProperty("assemblyName").GetString(), owners[owner.GetProperty("logicalPath").GetString()!]!.Assembly!.Name); }
            Assert.Empty(first.Result!.Uncertainties); Assert.All(first.Result.Entries, entry => Assert.False(entry.PathsTruncated));
            Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()));
            if (scenario == "empty") { Assert.Empty(first.Result.Entries); Assert.Empty(native.GetProperty("findings").EnumerateArray()); }
            if (scenario == "supporting-sources")
            {
                var throwOwners = first.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps)
                    .Where(step => step.Kind.ToString() == "ExplicitThrow").Select(step => step.FilePath).ToHashSet();
                Assert.Contains("/solution/p1/Source.cs", throwOwners); Assert.Contains("/solution/p2/Source.cs", throwOwners);
                Assert.Single(native.GetProperty("findings").EnumerateArray());
            }
            await AssertNoFindingAdmission(fixture.Input);
            foreach (var run in new[] { first, repeat })
            {
                Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, run.Dispatch!.Decision!.Selection);
                Assert.Null(run.Dispatch.Routing!.CurrentAnalysis); HistoricalWorkerLifetimeAssertions.Stopped(run.Dispatch.Routing.HistoricalAnalysis!);
            }
            Isolation();
            await Evidence($"baseline-{count}-{scenario}", new
            {
                Count = count,
                Scenario = scenario,
                Native = native,
                Flow = first.Result,
                Graph = graph,
                Owners = owners,
                FullFlowGraphParity = true,
                RepeatEqual = true,
                FinalFindingAdmission = false
            });
        }

        [Fact]
        public async Task AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted()
        {
            var fixture = await Create(2, "same-name-types");
            var payload = HistoricalWorkerPayloadProjection.Project(fixture.Input).Value!.Payload;
            var native = Native(payload); var historical = await Flow(fixture.Input);
            Assert.True(historical.Succeeded, Failure(historical));
            Assert.Equal(native.GetProperty("result").GetRawText(), Json(historical.Result));
            var graph = await Probe(payload, "--b15-graph", null);
            Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText());
            var summaries = native.GetProperty("summaries").EnumerateArray().Select(item => JsonDocument.Parse(item.GetString()!)).ToArray();
            try
            {
                var a = summaries.Single(item => item.RootElement.GetProperty("callable").GetProperty("containingType").GetProperty("assembly").GetProperty("name").GetString() == "Original.Solution.P0");
                var b = summaries.Single(item => item.RootElement.GetProperty("callable").GetProperty("containingType").GetProperty("assembly").GetProperty("name").GetString() == "Original.Solution.P1");
                string SourceOwner(JsonDocument summary) => summary.RootElement.GetProperty("sources")[0].GetProperty("exceptionType").GetProperty("assembly").GetProperty("name").GetString()!;
                Assert.Equal("Original.Solution.P0", SourceOwner(a)); Assert.Equal("Original.Solution.P1", SourceOwner(b));
                var entry = Assert.Single(historical.Result!.Entries);
                Assert.Equal("Original.Solution.P0", entry.ExceptionType.Assembly!.Name);
                Assert.Contains(entry.Paths.SelectMany(path => path.Steps), step => step.FilePath == "/solution/p1/Source.cs" && step.Kind.ToString() == "ExplicitThrow");
                Assert.Single(native.GetProperty("findings").EnumerateArray());
                Assert.Equal("DOC611", native.GetProperty("findings")[0].GetProperty("smell").GetProperty("id").GetString());
                await AssertNoFindingAdmission(fixture.Input);
                HistoricalWorkerLifetimeAssertions.Stopped(historical.Dispatch!.Routing!.HistoricalAnalysis!); Isolation();
                await Evidence("ownership-stop-witness", new
                {
                    Native = native,
                    Flow = historical.Result,
                    Graph = graph,
                    LocalATypeOwner = SourceOwner(a),
                    LocalBTypeOwner = SourceOwner(b),
                    EvaluatedTypeOwner = entry.ExceptionType.Assembly.Name,
                    TypeOwnershipLossObserved = true,
                    CurrentHistoricalParityDoesNotProveOwnership = true,
                    FinalFindingAdmission = false
                });
            }
            finally { foreach (var summary in summaries) { summary.Dispose(); } }
        }

        [Theory]
        [InlineData("root-project")]
        [InlineData("source-project")]
        [InlineData("dependency-project")]
        [InlineData("reference-mvid")]
        [InlineData("missing-edge")]
        [InlineData("duplicate-path")]
        [InlineData("root-member")]
        public async Task ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution(string scenario)
        {
            var fixture = await Create(3, "chain");
            var input = HistoricalWorkerPayloadProjection.Project(fixture.Input).Value!.Payload;
            var graph = input.Context!.Solution!; var a = graph.Projects![0]; var b = graph.Projects[1]; var c = graph.Projects[2];
            var wrong = scenario switch
            {
                "root-project" => graph with { Target = b.Identity },
                "source-project" => graph with { Projects = [a, b with { Input = c.Input }, c] },
                "dependency-project" => graph with { Projects = [a, b with { Dependencies = [a.Identity!] }, c] },
                "reference-mvid" => graph with { Projects = [a, b with { Identity = b.Identity! with { ModuleVersionId = Guid.NewGuid() } }, c] },
                "missing-edge" => graph with { Projects = [a with { Dependencies = [] }, b, c] },
                "duplicate-path" => graph with
                {
                    Projects = [a, b with { Input = b.Input! with { LogicalPath = input.LogicalPath,
                    Context = b.Input.Context! with { Scope = b.Input.Context!.Scope! with { RootLogicalPath = input.LogicalPath, OrderedSourcePaths = [input.LogicalPath!] } } } }, c]
                },
                "root-member" => graph,
                _ => throw new InvalidOperationException()
            };
            var invalid = input with { Context = input.Context with { Solution = wrong }, MethodName = scenario == "root-member" ? "Other" : input.MethodName };
            var rejected = await Client().AnalyzeAsync(invalid);
            Assert.False(rejected.Success); Assert.Null(rejected.ProcessId); Assert.Null(rejected.Result);
            await Evidence("graph-negative-" + scenario, new { Scenario = scenario, rejected.Failure, NoProcess = true });
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(2, true)]
        public async Task RealCli_SolutionJobRemainsUnsupported_NoReportOrEmptySuccess(int count, bool empty)
        {
            var fixture = await Create(count, empty ? "empty" : "chain");
            string report = Path.Combine(fixture.Directory, "report.json");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetTempPath() };
            start.ArgumentList.Add(Path.Combine(Root(), "src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll"));
            foreach (var argument in new[] { "--check", "--external-target", fixture.Job, "--format", "json", "--output", report }) { start.ArgumentList.Add(argument); }
            using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(90)); }
            finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
            var exit = OwnedProcessExit.Capture(process);
            Assert.Equal(4, exit.ExitCode); Assert.False(File.Exists(report)); Assert.DoesNotContain("Check succeeded", await stdout);
            Assert.Contains("not admitted", await stderr);
            await Evidence($"cli-unsupported-{count}-{empty}", new
            {
                Count = count,
                Empty = empty,
                ExitCode = exit.ExitCode,
                Stdout = await stdout,
                Stderr = await stderr,
                ProcessExit = exit,
                NoReport = true,
                NoEmptySuccess = true
            });
        }

        [Fact]
        public void StopRule_DoesNotIntroducePolicyRoutingAttestationOrPidWorkarounds()
        {
            string evaluator = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs"));
            Assert.Contains("GetFirstSymbolForDeclarationId", evaluator);
            string external = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Execution/ExternalTargetExecution.cs"));
            Assert.Contains("Solution is not admitted", external); Assert.DoesNotContain("SupportingProjects", external);
            string client = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs"));
            Assert.Contains("OwnedProcessExit.Capture(process)", client); Assert.DoesNotContain("GetProcessById", client); Isolation();
        }

        private static async Task AssertNoFindingAdmission(HistoricalWorkerPayloadProjectionInput input)
        {
            var result = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input, new(new(Client())));
            Assert.False(result.Succeeded); Assert.Empty(result.Findings); Assert.Null(result.Direct.Dispatch); Assert.Null(result.Summary);
            var directProfile = input with
            {
                Finding = new(WorkerDeclaredExecution.DirectEvidence,
                new(WorkerFindingContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitive))
            };
            var mismoded = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(directProfile, new(new(Client())));
            Assert.False(mismoded.Succeeded); Assert.Empty(mismoded.Findings); Assert.Null(mismoded.Direct.Dispatch);
        }
        private sealed record Fixture(HistoricalWorkerPayloadProjectionInput Input, string Directory, string Job);
        private static async Task<Fixture> Create(int count, string scenario)
        {
            string directory = Path.Combine(Root(), "artifacts/p5o2b25/audit-emit-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            var framework = (byte[][])Invoke(typeof(ExceptionFlowHistoricalReferenceIdentityTests), "FrameworkImages")!;
            byte[]? dependency = null; var inputs = new List<HistoricalWorkerPayloadProjectionInput>(); var projects = new List<object>();
            for (int index = count - 1; index >= 0; index--)
            {
                var payload = (WorkerAnalysisInput)Invoke(typeof(ExceptionFlowHistoricalSolutionScopeTests), "Local", index, count, scenario == "source-type", dependency == null ? framework : [.. framework, dependency])!;
                string source = payload.Source!;
                if (scenario == "empty" && index == count - 1) { source = source.Replace("throw Value;", "", StringComparison.Ordinal); }
                if (scenario == "mixed" && index == 0) { source = source.Replace("P1.Fixture.Root();", "if (Value != null) throw Value; P1.Fixture.Root();", StringComparison.Ordinal); }
                if (scenario == "supporting-sources" && index == 1) { source = source.Replace("P2.Fixture.Root();", "if (Value != null) throw Value; P2.Fixture.Root();", StringComparison.Ordinal); }
                if (scenario == "same-name-types")
                {
                    source = "namespace Shared { public class Fault : System.Exception {} } " + source.Replace("System.InvalidOperationException", "Shared.Fault", StringComparison.Ordinal);
                    if (index == 0) { source = source.Replace("P1.Fixture.Root();", "if (Value != null) throw Value; P1.Fixture.Root();", StringComparison.Ordinal); }
                }
                if (scenario == "multiple")
                {
                    if (index == count - 1) { source = source.Replace("public static void Root() { throw Value; }", "public static void Root() { One(); Two(); One(); } public static void One() { throw Value; } public static void Two() { throw Value; }", StringComparison.Ordinal); }
                    else if (index == 0) { source = source.Replace("P1.Fixture.Root();", "P1.Fixture.Root(); P1.Fixture.Root();", StringComparison.Ordinal); }
                }
                if (index == 0)
                {
                    string tag = scenario switch
                    {
                        "exact" => "<exception cref=\"System.InvalidOperationException\">Exact.</exception>",
                        "base" => "<exception cref=\"System.Exception\">Base.</exception>",
                        "invalid" => "<exception cref=\"MissingType\">Bad.</exception>",
                        "inheritdoc" => "<inheritdoc/>",
                        _ => ""
                    };
                    source = source.Replace("<summary>Controlled cross-project root.</summary>", "<summary>Controlled cross-project root.</summary>" + tag, StringComparison.Ordinal);
                    if (scenario == "no-doc") { source = source.Replace("/// <summary>Controlled cross-project root.</summary>\r\n", "", StringComparison.Ordinal); }
                }
                payload = payload with { Source = source }; string output = Path.Combine(directory, "p" + index); Directory.CreateDirectory(output);
                await Probe(payload, "--b14-emit", output); dependency = await File.ReadAllBytesAsync(Path.Combine(output, "input.dll"));
                var original = (HistoricalWorkerPayloadProjectionInput)Invoke(typeof(ExceptionFlowHistoricalSolutionScopeTests), "ReadOriginal", payload, dependency, await File.ReadAllBytesAsync(Path.Combine(output, "input.pdb")))!;
                inputs.Insert(0, original);
                string sourcePath = Path.Combine(output, "source.cs"); await File.WriteAllTextAsync(sourcePath, source, new UTF8Encoding(true));
                var references = new List<string>();
                for (int ordinal = 0; ordinal < payload.Context!.References!.Length; ordinal++)
                { string path = Path.Combine(output, "reference-" + ordinal + ".dll"); await File.WriteAllBytesAsync(path, payload.Context.References[ordinal].Image); references.Add(path); }
                projects.Insert(0, new
                {
                    targetPe = Path.Combine(output, "input.dll"),
                    portablePdb = Path.Combine(output, "input.pdb"),
                    sources = new[] { new { documentOrdinal = 0, path = sourcePath } },
                    references = references.ToArray(),
                    root = new { typeMetadataName = payload.TypeMetadataName, methodName = "Root", documentOrdinal = 0 }
                });
            }
            var root = JsonSerializer.SerializeToElement(projects[0]);
            var job = new
            {
                version = 1,
                targetPe = root.GetProperty("targetPe").GetString(),
                portablePdb = root.GetProperty("portablePdb").GetString(),
                sources = root.GetProperty("sources"),
                references = root.GetProperty("references"),
                root = root.GetProperty("root"),
                workerAssembly = Worker(),
                analysisMode = "solution-transitive"
            };
            string jobPath = Path.Combine(directory, "job.json"); await File.WriteAllTextAsync(jobPath, Json(job));
            return new(inputs[0] with { SupportingProjects = inputs.Skip(1).ToImmutableArray() }, directory, jobPath);
        }
        private static JsonElement Native(WorkerAnalysisInput input) => JsonSerializer.SerializeToElement(Invoke(typeof(ExceptionFlowHistoricalSolutionScopeTests), "Native", input, ExceptionAnalysisMode.SolutionTransitive), WorkerProtocol.JsonOptions);
        private static object? Invoke(Type type, string name, params object?[] args) => type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);
        private static Task<JsonElement> Probe(WorkerAnalysisInput input, string operation, string? output) => (Task<JsonElement>)Invoke(typeof(ExceptionFlowHistoricalReferenceIdentityTests), "Probe", input, operation, output)!;
        private static Task<ExternalExceptionFlowAnalysisResult> Flow(HistoricalWorkerPayloadProjectionInput input) => XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
        private static HistoricalWorkerClient Client() => new("dotnet", Worker(), TimeSpan.FromSeconds(60));
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static string Worker() => Path.Combine(Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll");
        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        private static string Failure(ExternalExceptionFlowAnalysisResult result) => result.PayloadProjection.Failure?.Message + ": " + result.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message;
        private static void Isolation() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        { string path = Path.Combine(Root(), "artifacts/p5o2b25/audit-cases", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!); await File.WriteAllTextAsync(path, Json(value)); }
    }
}
