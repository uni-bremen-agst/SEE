using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Execution;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Fresh raw artifacts through the real executable, never direct B22 invocation.</summary>
    public sealed class ExceptionFlowExternalTargetCliTests
    {
        private const string Source = """
            public class LocalException : System.Exception { }
            public static class Fixture
            {
                /// <summary>Calls a throwing helper.</summary>
                public static void Root() { Helper(); }
                /// <summary>Throws from a distinct, non-selected member.</summary>
                public static void Helper() { throw new LocalException(); }
            }
            """;

        [Theory]
        [InlineData("console")]
        [InlineData("json")]
        [InlineData("sarif")]
        public async Task GenuineCli_ReportsExactlyOneHistoricalFinding(string format)
        {
            var fixture = await Create(Source);
            var run = await Run(fixture, format);
            Assert.Equal(1, run.ExitCode); Assert.Empty(run.Stderr);
            Assert.Contains("1 documentation issue(s)", run.Stdout);
            if (format == "console") { Assert.Equal(1, Occurrences(run.Stdout, "DOC611")); }
            else
            {
                using var report = JsonDocument.Parse(File.ReadAllText(run.Report));
                var findings = format == "json" ? report.RootElement.GetProperty("Findings")
                    : report.RootElement.GetProperty("runs")[0].GetProperty("results");
                var finding = Assert.Single(findings.EnumerateArray());
                Assert.Contains("DOC611", finding.GetRawText());
                Assert.DoesNotContain("historical", report.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
                if (format == "json") { Assert.Equal(1, report.RootElement.GetProperty("Metrics").GetProperty("FindingCount").GetInt32()); }
            }
            await Evidence("positive-" + format, fixture, run);
        }

        [Theory]
        [InlineData("console")]
        [InlineData("json")]
        [InlineData("sarif")]
        public async Task GenuineCli_NoFindingIsSuccess(string format)
        {
            var fixture = await Create(Source.Replace("Helper();", "", StringComparison.Ordinal));
            var run = await Run(fixture, format);
            Assert.Equal(0, run.ExitCode); Assert.Empty(run.Stderr);
            Assert.Contains("Check succeeded", run.Stdout); Assert.DoesNotContain("DOC611", run.Stdout);
            if (format != "console")
            {
                using var report = JsonDocument.Parse(File.ReadAllText(run.Report));
                Assert.Empty((format == "json" ? report.RootElement.GetProperty("Findings")
                    : report.RootElement.GetProperty("runs")[0].GetProperty("results")).EnumerateArray());
            }
            await Evidence("empty-" + format, fixture, run);
        }

        [Theory]
        [InlineData("missing-pe")]
        [InlineData("missing-pdb")]
        [InlineData("wrong-pdb")]
        [InlineData("invalid-pdb")]
        [InlineData("compiler")]
        [InlineData("root")]
        [InlineData("signature")]
        [InlineData("ambiguous-root")]
        [InlineData("missing-source")]
        [InlineData("checksum")]
        [InlineData("profile")]
        [InlineData("worker-missing")]
        [InlineData("worker-error")]
        [InlineData("attestation")]
        [InlineData("contract")]
        [InlineData("root-source")]
        [InlineData("signed")]
        [InlineData("reference")]
        [InlineData("unknown-property")]
        [InlineData("duplicate-property")]
        public async Task GenuineCli_RejectsWithoutFindingsFallbackOrSuccess(string scenario)
        {
            string source = scenario == "signature" ? Source.Replace("void Root()", "void Root(int value)", StringComparison.Ordinal)
                : scenario == "ambiguous-root" ? Source + "\npublic static partial class Other { }" : Source;
            if (scenario == "ambiguous-root") { source = source.Replace("public static void Helper()", "public static void Root(int value) { } public static void Helper()", StringComparison.Ordinal); }
            if (scenario == "worker-error") { source = source.Replace("Helper();", "System.GC.Collect();", StringComparison.Ordinal); }
            if (scenario == "contract") { source = source.Replace("class LocalException :", "class LocalException<T> :", StringComparison.Ordinal).Replace("new LocalException()", "new LocalException<int>()", StringComparison.Ordinal); }
            var fixture = await Create(source, current: scenario is "compiler" or "profile" or "signed", release: scenario == "profile", signed: scenario == "signed");
            var job = JsonNode.Parse(File.ReadAllText(fixture.Job))!;
            switch (scenario)
            {
                case "missing-pe": job["targetPe"] = "absent.dll"; break;
                case "missing-pdb": job["portablePdb"] = "absent.pdb"; break;
                case "wrong-pdb": job["portablePdb"] = (await Create(Source + "\n// other")).Pdb; break;
                case "invalid-pdb": job["portablePdb"] = fixture.Source; break;
                case "root": job["root"]!["methodName"] = "Absent"; break;
                case "missing-source": job["sources"]![0]!["path"] = "absent.cs"; break;
                case "checksum": await File.AppendAllTextAsync(fixture.Source, "\n// mutation"); break;
                case "worker-missing": job["workerAssembly"] = "absent-worker.dll"; break;
                case "attestation": job["workerAssembly"] = await ReplayWorker(source, fixture.Directory); break;
                case "root-source": job["root"]!["documentOrdinal"] = 999; break;
                case "reference": job["references"]![0] = fixture.Pe; break;
                case "unknown-property": job["trusted"] = true; break;
            }
            string text = job.ToJsonString();
            if (scenario == "duplicate-property") { text = text.Insert(1, "\"version\":1,"); }
            await File.WriteAllTextAsync(fixture.Job, text);
            var run = await Run(fixture, "json");
            Assert.Equal(4, run.ExitCode);
            Assert.Contains("External target failed:", run.Stderr);
            if (scenario == "attestation") { Assert.Contains("execution attestation", run.Stderr, StringComparison.Ordinal); }
            if (scenario == "worker-error") { Assert.Contains("outside A9-FiniteOrbit-v1", run.Stderr, StringComparison.OrdinalIgnoreCase); }
            if (scenario == "contract") { Assert.Contains("B21/B22", run.Stderr, StringComparison.Ordinal); }
            Assert.DoesNotContain("Check succeeded", run.Stdout); Assert.DoesNotContain("documentation issue(s)", run.Stdout);
            Assert.False(File.Exists(run.Report));
            await Evidence("negative-" + scenario, fixture, run);
        }

        [Theory]
        [InlineData("--fix")]
        [InlineData("--compare-exception-analysis-modes")]
        [InlineData("--full")]
        [InlineData("--enable-statistics")]
        [InlineData("--force-historical")]
        [InlineData("positional")]
        public void ExternalArguments_RejectUnsupportedOperations(string extra)
        {
            Assert.False(ArgParsing.TryParseOptions(["--check", "--external-target", "job.json", extra], out _));
        }

        [Theory]
        [InlineData("direct")]
        [InlineData("project-transitive")]
        [InlineData("solution-transitive")]
        public async Task GenuineCli_RejectsOtherHistoricalFindingModes(string mode)
        {
            var fixture = await Create(Source);
            var run = await Run(fixture, "json", "--exception-analysis-mode", mode);
            Assert.Equal(2, run.ExitCode); Assert.False(File.Exists(run.Report));
            Assert.DoesNotContain("Check succeeded", run.Stdout);
            await Evidence("mode-" + mode, fixture, run);
        }

        [Fact]
        public async Task ToolRunner_UsesOrdinaryRunResultExactlyOnceAndRetainsIsolation()
        {
            var fixture = await Create(Source);
            Assert.True(ArgParsing.TryParseOptions(["--check", "--external-target", fixture.Job, "--format", "json", "--output", Path.Combine(fixture.Directory, "runner.json")], out var options));
            Assert.Equal(1, ToolRunner.Run(options!).FindingCount);
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        }

        private sealed record Fixture(string Directory, string Job, string Pe, string Pdb, string Source);
        private sealed record RunResult(int ExitCode, string Stdout, string Stderr, string Report);
        private static async Task<Fixture> Create(string source, bool current = false, bool release = false, bool signed = false)
        {
            string directory = Path.Combine(Root(), "artifacts", "p5o2b23", "fresh-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var images = (byte[][])typeof(ExceptionFlowHistoricalReferenceIdentityTests).GetMethod("FrameworkImages", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
            var payload = (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, ["project-transitive", "12", 1, false, false])!;
            payload = payload with { Source = source };
            if (current)
            {
                var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), payload.LogicalPath!, Encoding.UTF8);
                var compilation = CSharpCompilation.Create("Current.Cli.Target", [tree], images.Select(image => MetadataReference.CreateFromImage(image)),
                    new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, optimizationLevel: release ? OptimizationLevel.Release : OptimizationLevel.Debug));
                if (signed) { compilation = compilation.WithOptions(compilation.Options.WithCryptoPublicKey([.. typeof(object).Assembly.GetName().GetPublicKey()!]).WithPublicSign(true)); }
                using var pe = File.Create(Path.Combine(directory, "input.dll")); using var pdb = File.Create(Path.Combine(directory, "input.pdb"));
                var emitted = compilation.Emit(pe, pdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
            }
            else
            {
                await (Task<JsonElement>)typeof(ExceptionFlowHistoricalReferenceIdentityTests).GetMethod("Probe", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, [payload, "--b14-emit", directory])!;
            }
            string sourcePath = Path.Combine(directory, "source.cs");
            await File.WriteAllTextAsync(sourcePath, source, new UTF8Encoding(true));
            var references = new List<string>();
            for (int index = 0; index < images.Length; index++)
            { string path = Path.Combine(directory, "reference-" + index + ".dll"); await File.WriteAllBytesAsync(path, images[index]); references.Add(Path.GetFileName(path)); }
            string jobPath = Path.Combine(directory, "job.json");
            var job = new ExternalTargetJob(1, "input.dll", "input.pdb", [new(0, "source.cs")], references.ToArray(),
                new("Fixture", "Root", 0), Worker());
            await File.WriteAllTextAsync(jobPath, JsonSerializer.Serialize(job, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            return new(directory, jobPath, Path.Combine(directory, "input.dll"), Path.Combine(directory, "input.pdb"), sourcePath);
        }
        private static async Task<RunResult> Run(Fixture fixture, string format, params string[] extra)
        {
            string report = Path.Combine(fixture.Directory, "report." + format);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetTempPath() };
            start.ArgumentList.Add(Path.Combine(Root(), "src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll"));
            foreach (string argument in new[] { "--check", "--external-target", fixture.Job, "--format", format, "--output", report }) { start.ArgumentList.Add(argument); }
            foreach (string argument in extra) { start.ArgumentList.Add(argument); }
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(90)); }
            finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
            return new(process.ExitCode, await stdout, await stderr, report);
        }
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static string Worker() => Path.Combine(Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll");
        private static async Task<string> ReplayWorker(string source, string directory)
        {
            var payload = (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, ["project-transitive", "12", 1, false, false])!;
            payload = payload with { Source = source };
            var original = await (Task<HistoricalWorkerPayloadProjectionInput>)typeof(ExceptionFlowHistoricalScopeTests)
                .GetMethod("ValidatedInput", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload])!;
            original = original with
            {
                AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions,
                DeclaredPolicy = new(WorkerDeclaredPolicyContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions,
                    Documentation: new(WorkerDeclaredDocumentationContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions))
            };
            var projected = HistoricalWorkerPayloadProjection.ProjectForFindings(original, WorkerDeclaredExecution.DirectEvidence);
            Assert.True(projected.Succeeded, projected.Failure?.Message);
            var genuine = await new HistoricalWorkerClient("dotnet", Worker(), TimeSpan.FromSeconds(60)).AnalyzeAsync(projected.Value!.Payload);
            Assert.True(genuine.Success, genuine.Failure?.Message);
            string peer = Path.Combine(directory, "XMLDocNormalizer.HistoricalWorker.dll");
            string fixture = Path.Combine(Root(), "artifacts/p5o2b4/test-process/bin/Debug/net8.0/BoundaryTestProcess");
            File.Copy(fixture + ".dll", peer);
            File.Copy(fixture + ".runtimeconfig.json", Path.ChangeExtension(peer, ".runtimeconfig.json"));
            await File.WriteAllTextAsync(Path.Combine(directory, "b21-replayed-worker-response.json"), JsonSerializer.Serialize(
                new WorkerResponse(2, "analyze", true, genuine.Identity, genuine.Result, Provenance: genuine.Provenance,
                    DeclaredPolicy: genuine.DeclaredPolicy, DeclaredDocumentation: genuine.DeclaredDocumentation), WorkerProtocol.JsonOptions));
            return peer;
        }
        private static int Occurrences(string text, string value) => (text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;
        private static Task Evidence(string name, Fixture fixture, RunResult result)
        {
            string directory = Path.Combine(Root(), "artifacts/p5o2b23/e2e"); Directory.CreateDirectory(directory);
            return File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { Fixture = fixture, Result = result }));
        }
    }
}
