using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>B25B: independent native policy oracle, genuine isolated worker, and the productive receipt seam.</summary>
    public sealed class ExceptionFlowHistoricalSolutionAdoptionTests
    {
        internal static async Task AssertBoundedProductionBoundary()
        {
            var positive = await Fixture(2, "chain");
            var actual = await Find(positive);
            Assert.True(actual.Succeeded, Failure(actual));
            Assert.Equal("DOC611", Assert.Single(actual.Findings).Smell.ID);
            Assert.Equal(positive.Source.Document.Name, actual.Findings[0].FilePath);
            Assert.True(HistoricalWorkerResponseValidation.TryGetFindingFacts(actual.Direct.Dispatch!.Routing!.HistoricalAnalysis, out _));
            Assert.True(actual.SummaryRequested); Assert.True(actual.Summary!.Succeeded);
            var negative = await Find(await Fixture(4, "chain"));
            Assert.False(negative.Succeeded); Assert.Empty(negative.Findings);
            Assert.Null(negative.Direct.Dispatch); Assert.Null(negative.Summary);
        }

        [Theory]
        [InlineData(2, "chain", 1)] // W1
        [InlineData(3, "chain", 1)] // W2
        [InlineData(2, "same-name-types", 2)] // W3
        [InlineData(3, "multiple", 1)]
        public async Task W1_W2_W3_CurrentThenRealHistorical_ReportOnlyRoot(int count, string scenario, int expected)
        {
            var input = await Fixture(count, scenario);
            var payload = HistoricalWorkerPayloadProjection.Project(input).Value!.Payload;
            var current = Native(payload);
            Assert.Equal(expected, current.GetProperty("findings").GetArrayLength());
            Assert.All(current.GetProperty("findings").EnumerateArray(), finding =>
            { Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()); Assert.Equal("DOC611", finding.GetProperty("smell").GetProperty("id").GetString()); });
            var actual = await Find(input);
            Assert.True(actual.Succeeded, Failure(actual));
            Assert.Equal(expected, actual.Findings.Length);
            Assert.Equal(Sorted(current.GetProperty("findings").EnumerateArray().Select(item => item.GetRawText())), Sorted(actual.Findings.Select(Json)));
            Assert.Equal(current.GetProperty("result").GetRawText(), Json(actual.Summary!.Result));
            if (scenario == "same-name-types")
            { Assert.Equal(new[] { "Original.Solution.P0", "Original.Solution.P1" }, actual.Summary.Result!.Entries.Select(entry => entry.ExceptionType.Assembly!.Name).OrderBy(name => name, StringComparer.Ordinal)); }
            Assert.Contains(actual.Summary.Result!.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps),
                step => step.FilePath == $"/solution/p{count - 1}/Source.cs" && step.Kind.ToString() == "ExplicitThrow");
            AssertReal(actual);
            await Evidence($"W{count}-{scenario}", new { Current = current, Direct = actual.Direct.Result, Summary = actual.Summary.Result,
                actual.Findings, DirectExecution = actual.Direct.Dispatch!.Routing!.HistoricalAnalysis, SummaryExecution = actual.Summary.Dispatch!.Routing!.HistoricalAnalysis });
        }

        [Fact]
        public async Task W4_OnlyOriginalRegisteredStagesCanCompletePair_NoReplayCloneRawOrOtherRun()
        {
            var input = await Fixture(2, "chain");
            Assert.True(HistoricalSolutionFindingOperation.TryPrepare(input, out var first, out _));
            Assert.True(HistoricalSolutionFindingOperation.TryPrepare(input, out var other, out _));
            var d1 = await Client().AnalyzeAsync(first!.Direct.Value!.Payload);
            var s1 = await Client().AnalyzeAsync(first.Summary.Value!.Payload);
            var d2 = await Client().AnalyzeAsync(other!.Direct.Value!.Payload);
            var s2 = await Client().AnalyzeAsync(other.Summary.Value!.Payload);
            Assert.True(d1.Success, d1.Failure?.Message); Assert.True(s1.Success, s1.Failure?.Message);
            Assert.True(d2.Success, d2.Failure?.Message); Assert.True(s2.Success, s2.Failure?.Message);
            Assert.Equal(Json(s1.Result), Json(s2.Result));
            Assert.False(first.TryComplete(d1, s2, out _, out _));
            Assert.False(first.TryComplete(d2, s1, out _, out _));
            Assert.False(first.TryComplete(d1 with { }, s1, out _, out _));
            Assert.False(first.TryComplete(d1, s1 with { }, out _, out _));
            var raw = HistoricalWorkerResponseValidation.Validate(Json(new WorkerResponse(WorkerProtocol.Version, "analyze", true,
                s1.Identity, s1.Result, null, s1.Provenance)), "", 0, first.Summary.Value.Payload, s1.ProcessId);
            Assert.True(raw.Success, raw.Failure?.Message);
            Assert.False(first.TryComplete(d1, raw, out _, out _));
            Assert.True(first.TryComplete(d1, s1, out _, out var document)); Assert.NotNull(document);
            Assert.False(first.TryComplete(d1, s1, out _, out _));
            var reusedRequest = await Client().AnalyzeAsync(other.Direct.Value!.Payload);
            Assert.True(reusedRequest.Success, reusedRequest.Failure?.Message);
            Assert.False(other.TryAdmitDirect(reusedRequest, out _));
            Assert.False(other.TryComplete(reusedRequest, s2, out _, out _));
            Assert.True(other.TryComplete(d2, s2, out _, out _));
            HistoricalWorkerLifetimeAssertions.Stopped(reusedRequest);
            foreach (var call in new[] { d1, s1, d2, s2 }) { HistoricalWorkerLifetimeAssertions.Stopped(call); }
            await Evidence("W4-original-stages", new { d1, s1, d2, s2, RejectedOtherRun = true, RejectedClone = true, RejectedRaw = true, RejectedReplay = true });
        }

        [Fact]
        public async Task W4_MutatedGraphAfterRegistrationCannotMintReceipt()
        {
            var input = await Fixture(3, "chain");
            Assert.True(HistoricalSolutionFindingOperation.TryPrepare(input, out var operation, out _));
            var direct = await Client().AnalyzeAsync(operation!.Direct.Value!.Payload);
            Assert.True(direct.Success, direct.Failure?.Message);
            var nodes = operation.Summary.Value!.Payload.Context!.Solution!.Projects!;
            nodes[0] = nodes[0] with { Dependencies = [] };
            Assert.False(operation.TryAdmitDirect(direct, out _));
            HistoricalWorkerLifetimeAssertions.Stopped(direct);
        }

        [Theory]
        [InlineData("direct-summary", 2)]
        [InlineData("generic-catch", 1)]
        [InlineData("containing", 1)]
        public async Task IndependentDirectAndFiniteCrossProjectGenericContexts_RemainExact(string scenario, int expected)
        {
            var fixture = await Custom(scenario); var input = fixture.Input;
            var current = Native(HistoricalWorkerPayloadProjection.Project(input).Value!.Payload);
            Assert.Equal(expected, current.GetProperty("findings").GetArrayLength());
            var actual = await Find(input); Assert.True(actual.Succeeded, Failure(actual));
            Assert.Equal(expected, actual.Findings.Length);
            Assert.Equal(Sorted(current.GetProperty("findings").EnumerateArray().Select(item => item.GetRawText())), Sorted(actual.Findings.Select(Json)));
            Assert.Equal(current.GetProperty("result").GetRawText(), Json(actual.Summary!.Result));
            if (scenario == "direct-summary")
            {
                Assert.Single(actual.Direct.Result!.Entries); Assert.Equal(2, actual.Summary.Result!.Entries.Length);
                Assert.Equal("InvalidOperationException", actual.Direct.Result.Entries[0].ExceptionType.MetadataName);
                Assert.Contains(actual.Summary.Result.Entries, entry => entry.ExceptionType.MetadataName == "ArgumentException");
            }
            else { Assert.DoesNotContain(actual.Summary.Result!.Entries, entry => entry.ExceptionType.MetadataName!.StartsWith("Fault", StringComparison.Ordinal)); }
            AssertReal(actual); await Evidence(scenario, new { Current = current, actual.Findings, Direct = actual.Direct.Result, Summary = actual.Summary.Result });
        }

        [Theory]
        [InlineData("growing")]
        [InlineData("uncovered-call")]
        [InlineData("generic-final")]
        public async Task W5_OutsideCoverageAndOuterGenericFindingBoundary_NeverAdopt(string scenario)
        {
            var fixture = await Custom(scenario);
            var payload = HistoricalWorkerPayloadProjection.Project(fixture.Input).Value!.Payload;
            var current = Native(payload);
            var actual = await Find(fixture.Input);
            Assert.False(actual.Succeeded); Assert.Empty(actual.Findings); Assert.True(actual.SummaryRequested);
            if (scenario == "generic-final")
            {
                // A9 exact internally, but explicitly outside the narrower final nongeneric Finding profile.
                Assert.True(actual.Summary!.Succeeded); Assert.Single(actual.Summary.Result!.Entries);
                Assert.NotEmpty(actual.Summary.Result.Entries[0].ExceptionType.TypeArguments);
            }
            else
            {
                Assert.DoesNotContain(current.GetProperty("findings").EnumerateArray(), finding => finding.GetProperty("smell").GetProperty("id").GetString() is "DOC610" or "DOC611" or "DOC612");
                Assert.False(actual.Summary!.Succeeded); Assert.Null(actual.Summary.Result);
                var call = actual.Summary.Dispatch!.Routing!.HistoricalAnalysis!;
                Assert.Equal(WorkerFailureCode.UnsupportedContext, call.Failure!.WorkerFailure!.Code);
                Assert.Contains("A9-FiniteOrbit-v1", string.Join("\n", call.Failure.WorkerFailure.Details));
                Assert.False(HistoricalWorkerResponseValidation.TryGetFindingFacts(call, out _));
                HistoricalWorkerLifetimeAssertions.Stopped(call);
            }
            await Evidence("W5-" + scenario, new { Current = current, actual.Failure, Summary = actual.Summary.Dispatch!.Routing!.HistoricalAnalysis, actual.Findings });
        }

        [Fact]
        public async Task W4_EqualResultsFromDifferentGenuineGraphsCannotBeCrossAdopted()
        {
            var fixture = await Custom("equal-graphs", 3);
            var three = fixture.Input;
            var two = three with { SupportingProjects = [three.SupportingProjects[0]] };
            Assert.True(HistoricalSolutionFindingOperation.TryPrepare(two, out var a, out _));
            Assert.True(HistoricalSolutionFindingOperation.TryPrepare(three, out var b, out _));
            Assert.NotEqual(a!.GraphSnapshot, b!.GraphSnapshot);
            var ad = await Client().AnalyzeAsync(a.Direct.Value!.Payload); var ass = await Client().AnalyzeAsync(a.Summary.Value!.Payload);
            var bd = await Client().AnalyzeAsync(b.Direct.Value!.Payload); var bs = await Client().AnalyzeAsync(b.Summary.Value!.Payload);
            Assert.True(ad.Success, ad.Failure?.Message); Assert.True(ass.Success, ass.Failure?.Message);
            Assert.True(bd.Success, bd.Failure?.Message); Assert.True(bs.Success, bs.Failure?.Message);
            Assert.Equal(Json(ass.Result), Json(bs.Result));
            Assert.False(a.TryComplete(ad, bs, out _, out _)); Assert.False(b.TryComplete(bd, ass, out _, out _));
            Assert.True(a.TryComplete(ad, ass, out _, out _)); Assert.True(b.TryComplete(bd, bs, out _, out _));
            foreach (var call in new[] { ad, ass, bd, bs }) { HistoricalWorkerLifetimeAssertions.Stopped(call); }
            await Evidence("W4-equal-results-different-graphs", new { Two = a.GraphSnapshot, Three = b.GraphSnapshot, ad, SummaryTwo = ass, bd, SummaryThree = bs, CrossPairRejected = true });
        }

        [Theory]
        [InlineData(2, "console")]
        [InlineData(2, "json")]
        [InlineData(2, "sarif")]
        [InlineData(3, "console")]
        [InlineData(3, "json")]
        [InlineData(3, "sarif")]
        public async Task W6_ExplicitCompleteSolutionJob_UsesRealCliAndReporters(int count, string format)
        {
            var fixture = await Custom("chain", count);
            string report = Path.Combine(fixture.Directory, "report." + format);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetTempPath() };
            start.ArgumentList.Add(Path.Combine(Root(), "src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll"));
            foreach (var arg in new[] { "--check", "--external-target", fixture.Job, "--format", format, "--output", report }) { start.ArgumentList.Add(arg); }
            using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(90)); }
            finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
            var exit = OwnedProcessExit.Capture(process); string output = await stdout; string errors = await stderr;
            Assert.Equal(1, exit.ExitCode); Assert.Empty(errors); Assert.Contains("1 documentation issue(s)", output);
            if (format == "console") { Assert.Equal(1, output.Split("DOC611", StringSplitOptions.None).Length - 1); }
            else
            {
                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(report));
                var finding = Assert.Single((format == "json" ? document.RootElement.GetProperty("Findings") : document.RootElement.GetProperty("runs")[0].GetProperty("results")).EnumerateArray());
                Assert.Contains("DOC611", finding.GetRawText()); Assert.Contains("/solution/p0/Source.cs", finding.GetRawText());
                if (format == "json") { Assert.Equal(1, document.RootElement.GetProperty("Metrics").GetProperty("FindingCount").GetInt32()); }
            }
            await Evidence($"W6-cli-{count}-{format}", new { exit, Stdout = output, Stderr = errors, Report = format == "console" ? null : await File.ReadAllTextAsync(report) });
        }

        private sealed record CustomFixture(HistoricalWorkerPayloadProjectionInput Input, string Directory, string Job);
        [Fact]
        public async Task W6_ReplayedGenuineSummaryFromWrongPhysicalWorkerCannotCompletePair()
        {
            var input = await Fixture(2, "chain");
            Assert.True(HistoricalSolutionFindingOperation.TryPrepare(input, out var operation, out _));
            var direct = await Client().AnalyzeAsync(operation!.Direct.Value!.Payload);
            var summary = await Client().AnalyzeAsync(operation.Summary.Value!.Payload);
            Assert.True(direct.Success, direct.Failure?.Message); Assert.True(summary.Success, summary.Failure?.Message);
            Assert.True(HistoricalSolutionFindingOperation.TryPrepare(input, out var attacked, out _));
            var attackedDirect = await Client().AnalyzeAsync(attacked!.Direct.Value!.Payload);
            Assert.True(attackedDirect.Success, attackedDirect.Failure?.Message);
            string directory = Path.Combine(Root(), "artifacts/p5o2b25b/replay-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            string peer = Path.Combine(directory, "XMLDocNormalizer.HistoricalWorker.dll");
            string fixture = Path.Combine(Root(), "artifacts/p5o2b4/test-process/bin/Debug/net8.0/BoundaryTestProcess");
            File.Copy(fixture + ".dll", peer); File.Copy(fixture + ".runtimeconfig.json", Path.ChangeExtension(peer, ".runtimeconfig.json"));
            await File.WriteAllTextAsync(Path.Combine(directory, "b21-replayed-worker-response.json"), Json(new WorkerResponse(WorkerProtocol.Version,
                "analyze", true, summary.Identity, summary.Result, Provenance: summary.Provenance)));
            var replay = await new HistoricalWorkerClient("dotnet", peer, TimeSpan.FromSeconds(60)).AnalyzeAsync(attacked.Summary.Value!.Payload);
            Assert.False(attacked.TryComplete(attackedDirect, replay, out _, out _));
            Assert.False(HistoricalWorkerResponseValidation.TryGetFindingFacts(replay, out _));
            foreach (var call in new[] { direct, summary, attackedDirect, replay }) { HistoricalWorkerLifetimeAssertions.Stopped(call); }
            await Evidence("W6-wrong-physical-worker", new { direct, summary, attackedDirect, replay, PairRejected = true });
        }

        private static async Task<CustomFixture> Custom(string scenario, int count = 2)
        {
            string directory = Path.Combine(Root(), "artifacts/p5o2b25b/emit-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            var framework = (byte[][])Invoke(typeof(ExceptionFlowHistoricalReferenceIdentityTests), "FrameworkImages")!;
            byte[]? dependency = null; var inputs = new List<HistoricalWorkerPayloadProjectionInput>(); var jobs = new List<object>();
            for (int index = count - 1; index >= 0; index--)
            {
                var payload = (WorkerAnalysisInput)Invoke(typeof(ExceptionFlowHistoricalSolutionScopeTests), "Local", index, count, false, dependency == null ? framework : [.. framework, dependency])!;
                string source = payload.Source!;
                if (scenario == "direct-summary")
                {
                    if (index == 0) { source = source.Replace("P1.Fixture.Root();", "if (Branch) throw new System.InvalidOperationException(); P1.Fixture.Root();", StringComparison.Ordinal).Replace("public static class Fixture {", "public static class Fixture { public static bool Branch;", StringComparison.Ordinal); }
                    else { source = source.Replace("System.InvalidOperationException", "System.ArgumentException", StringComparison.Ordinal); }
                }
                if (scenario is "generic-catch" or "containing" && index == 1)
                {
                    source = scenario == "generic-catch"
                        ? "namespace P1 { public class Fault<T> : System.Exception {} public static class Fixture { public static System.InvalidOperationException Value = null!; public static void Root() { Invoke<int>(); } public static void Invoke<T>() { try { throw new Fault<T>(); } catch (Fault<T>) {} throw Value; } } }"
                        : "namespace P1 { public class Fault<T> : System.Exception {} public static class Api<T> { public static System.InvalidOperationException Value = null!; public static void Hit() { try { throw new Fault<T>(); } catch (Fault<T>) {} throw Value; } } public static class Fixture { public static void Root() { Api<int>.Hit(); } } }";
                }
                if (scenario == "generic-catch" && index == 0) { source = source.Replace("P1.Fixture.Root();", "P1.Fixture.Invoke<string>();", StringComparison.Ordinal); }
                if (scenario == "containing" && index == 0) { source = source.Replace("P1.Fixture.Root();", "P1.Api<string>.Hit();", StringComparison.Ordinal); }
                if (scenario == "growing" && index == 1) { source = source.Replace("throw Value;", "Recur<int>();", StringComparison.Ordinal).Replace("} } }", "} public static void Recur<T>() { Recur<System.Collections.Generic.List<T>>(); } } }", StringComparison.Ordinal); }
                if (scenario == "uncovered-call" && index == 1) { source = source.Replace("throw Value;", "System.GC.Collect();", StringComparison.Ordinal); }
                if (scenario == "generic-final" && index == 1) { source = "namespace P1 { public class Fault<T> : System.Exception {} public static class Fixture { public static void Root() { throw new Fault<int>(); } } }"; }
                if (scenario == "equal-graphs" && index == 1) { source = source.Replace("P2.Fixture.Root();", "throw Value;", StringComparison.Ordinal); }
                payload = payload with { Source = source };
                string output = Path.Combine(directory, "p" + index); Directory.CreateDirectory(output);
                await (Task<JsonElement>)Invoke(typeof(ExceptionFlowHistoricalReferenceIdentityTests), "Probe", payload, "--b14-emit", output)!;
                dependency = await File.ReadAllBytesAsync(Path.Combine(output, "input.dll"));
                var original = (HistoricalWorkerPayloadProjectionInput)Invoke(typeof(ExceptionFlowHistoricalSolutionScopeTests), "ReadOriginal", payload, dependency, await File.ReadAllBytesAsync(Path.Combine(output, "input.pdb")))!;
                inputs.Insert(0, original);
                string path = Path.Combine(output, "source.cs"); await File.WriteAllTextAsync(path, source, new UTF8Encoding(true));
                var references = new List<string>();
                for (int ordinal = 0; ordinal < payload.Context!.References!.Length; ordinal++)
                { string file = Path.Combine(output, "ref" + ordinal + ".dll"); await File.WriteAllBytesAsync(file, payload.Context.References[ordinal].Image); references.Add(file); }
                jobs.Insert(0, new { targetPe = Path.Combine(output, "input.dll"), portablePdb = Path.Combine(output, "input.pdb"),
                    sources = new[] { new { documentOrdinal = 0, path } }, references, root = new { typeMetadataName = payload.TypeMetadataName, methodName = "Root", documentOrdinal = 0 } });
            }
            var job = JsonSerializer.SerializeToNode(jobs[0])!.AsObject();
            job["version"] = 1; job["analysisMode"] = "solution-transitive";
            job["workerAssembly"] = Path.Combine(Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll");
            job["supportingProjects"] = JsonSerializer.SerializeToNode(jobs.Skip(1).ToArray());
            string jobPath = Path.Combine(directory, "job.json"); await File.WriteAllTextAsync(jobPath, job.ToJsonString());
            return new(inputs[0] with { SupportingProjects = inputs.Skip(1).ToImmutableArray() }, directory, jobPath);
        }

        private static void AssertReal(ExternalExceptionFindingAnalysisResult actual)
        {
            foreach (var stage in new[] { actual.Direct, actual.Summary! })
            {
                Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, stage.Dispatch!.Decision!.Selection);
                Assert.Null(stage.Dispatch.Routing!.CurrentAnalysis);
                var call = stage.Dispatch.Routing.HistoricalAnalysis!;
                Assert.True(call.Success); Assert.NotNull(call.Identity); Assert.NotNull(call.ProcessId);
                Assert.Equal("XMLDocNormalizer.HistoricalWorker", call.Identity.Worker.Name);
                Assert.All(call.Identity.LoadedRoslyn, image => Assert.Equal("5.0.0.0", image.AssemblyVersion));
                HistoricalWorkerLifetimeAssertions.Stopped(call);
            }
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        }

        internal static async Task<HistoricalWorkerPayloadProjectionInput> Fixture(int count, string scenario)
        {
            var task = (Task)Invoke(typeof(ExceptionFlowHistoricalSolutionFindingTests), "Create", count, scenario)!;
            await task;
            var fixture = task.GetType().GetProperty("Result")!.GetValue(task)!;
            return (HistoricalWorkerPayloadProjectionInput)fixture.GetType().GetProperty("Input")!.GetValue(fixture)!;
        }
        private static object? Invoke(Type type, string method, params object?[] arguments)
            => type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments);
        private static JsonElement Native(WorkerAnalysisInput input) => JsonSerializer.SerializeToElement(Invoke(typeof(ExceptionFlowHistoricalSolutionScopeTests), "Native", input, ExceptionAnalysisMode.SolutionTransitive), WorkerProtocol.JsonOptions);
        private static string[] Sorted(IEnumerable<string> values) => values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static Task<ExternalExceptionFindingAnalysisResult> Find(HistoricalWorkerPayloadProjectionInput input)
            => XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input, new(new(Client())));
        private static string Failure(ExternalExceptionFindingAnalysisResult result) => result.Failure + ": " + (result.Summary ?? result.Direct).Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message;
        private static async Task Evidence(string name, object value)
        { string directory = Path.Combine(Root(), "artifacts/p5o2b25b/adoption"); Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), Json(value)); }
    }
}
