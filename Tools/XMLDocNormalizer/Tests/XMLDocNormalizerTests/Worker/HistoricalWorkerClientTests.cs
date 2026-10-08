using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Exercises the productive Main owner, not B3's direct process helper.</summary>
    public sealed class HistoricalWorkerClientTests
    {
        private const string Source = "public static class Fixture { public static void Root() { Thrower(); } static void Thrower() { throw null; } }";

        [Fact]
        public async Task MainToHistorical_RealCanonicalParityIdentityAndFreshProcessDeterminism()
        {
            Assembly common = typeof(Compilation).Assembly;
            Assembly csharp = typeof(CSharpCompilation).Assembly;
            Guid commonMvid = common.ManifestModule.ModuleVersionId;
            Guid csharpMvid = csharp.ManifestModule.ModuleVersionId;
            HistoricalWorkerCallResult first = await Client().AnalyzeAsync(Input());
            HistoricalWorkerCallResult second = await Client().AnalyzeAsync(Input());
            AssertSuccess(first);
            AssertSuccess(second);
            Assert.NotEqual(first.ProcessId, second.ProcessId);
            Assert.NotEqual(Environment.ProcessId, first.ProcessId);
            Assert.Equal(first.Result, second.Result);
            Assert.Equal(JsonSerializer.Serialize(first.Identity, WorkerProtocol.JsonOptions),
                JsonSerializer.Serialize(second.Identity, WorkerProtocol.JsonOptions));
            Assert.Equal(CurrentResult(Source), first.Result);
            Assert.Equal("NullReferenceException", Assert.Single(first.Result!.Entries).ExceptionType.MetadataName);
            Assert.Equal(2, first.Result.Entries[0].Paths[0].Steps.Length);
            Assert.Contains("worker-input.cs", JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions));
            Assert.Equal(commonMvid, common.ManifestModule.ModuleVersionId);
            Assert.Equal(csharpMvid, csharp.ManifestModule.ModuleVersionId);
            Assert.StartsWith("5.0.0-2.25567.12+", Info(common));
            Assert.StartsWith("5.0.0-2.25567.12+", Info(csharp));
            Assert.All(first.Identity!.LoadedRoslyn, image => Assert.StartsWith("5.0.0-2.25451.107+", image.InformationalVersion));
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
                assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
                || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
            AssertStopped(first);
            AssertStopped(second);

            string evidence = Path.Combine(Root(), "artifacts", "p5o2b4", "main-runtime-parity.json");
            Directory.CreateDirectory(Path.GetDirectoryName(evidence)!);
            await File.WriteAllTextAsync(evidence, JsonSerializer.Serialize(new
            {
                Current = new[] { CurrentIdentity(common), CurrentIdentity(csharp) },
                first.ProcessId, RepeatedProcessId = second.ProcessId,
                Historical = first.Identity, Result = first.Result,
                first.Provenance,
                CanonicalParity = true, Deterministic = true, ProcessesStopped = true,
                HistoricalLoadedInCaller = false
            }, WorkerProtocol.JsonOptions));
        }

        [Fact]
        public async Task CompletedCatch_IsExplicitValidatedEmptySuccess()
        {
            const string source = "public static class Fixture { public static void Root() { try { Thrower(); } catch (System.NullReferenceException) {} } static void Thrower() { throw null; } }";
            HistoricalWorkerCallResult result = await Client().AnalyzeAsync(Input(source));
            AssertSuccess(result);
            Assert.Empty(result.Result!.Entries);
            Assert.Equal(CurrentResult(source), result.Result);
        }

        [Theory]
        [InlineData("public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure")]
        [InlineData("public static class Fixture { public static void Root() { missing(); } }", "CompilationFailure")]
        [InlineData("public static class Fixture { public static void Other() {} }", "InvalidRequest")]
        public async Task RealWorkerFailure_IsStructuredAndNeverEmptySuccess(string source, string code)
        {
            HistoricalWorkerCallResult result = await Client().AnalyzeAsync(Input(source));
            Assert.False(result.Success);
            Assert.Null(result.Result);
            Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, result.Failure!.Code);
            Assert.Equal(code, result.Failure.WorkerFailure!.Code.ToString());
            AssertStopped(result);
        }

        [Theory]
        [InlineData("missing-source")]
        [InlineData("mode")]
        [InlineData("language")]
        [InlineData("references")]
        [InlineData("additional-sources")]
        [InlineData("supporting")]
        [InlineData("compiler-options")]
        [InlineData("assembly-name")]
        public async Task UnsupportedInput_IsRejectedBeforeProcessStart(string scenario)
        {
            WorkerCompilationContext context = new();
            WorkerAnalysisInput input = Input();
            context = scenario switch
            {
                "mode" => context with { AnalysisMode = "local" },
                "language" => context with { LanguageVersion = "preview" },
                "references" => context with { References = [] },
                "additional-sources" => context with { AdditionalSources = [] },
                "supporting" => context with { SupportingCompilations = [] },
                "compiler-options" => context with { CompilerOptions = [new("optimization", "release-debug-plus")] },
                "assembly-name" => context with { AssemblyName = "ExternalArtifact" },
                _ => context
            };
            input = input with { Source = scenario == "missing-source" ? null : Source, Context = context };
            HistoricalWorkerCallResult result = await Client().AnalyzeAsync(input);
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code);
            Assert.False(result.Success);
            Assert.Null(result.Result);
            Assert.Null(result.ProcessId);
        }

        [Fact]
        public async Task MissingDotnetHost_IsStartFailure()
        {
            HistoricalWorkerCallResult result = await new HistoricalWorkerClient(
                Path.Combine(Root(), "artifacts", "p5o2b4", "does-not-exist"), WorkerPath(), TimeSpan.FromSeconds(10)).AnalyzeAsync(Input());
            Assert.Equal(HistoricalWorkerClientFailureCode.StartFailure, result.Failure!.Code);
            Assert.Null(result.ProcessId);
            Assert.Null(result.Result);
        }

        [Theory]
        [InlineData("crash", "WorkerCrash")]
        [InlineData("malformed", "MalformedResponse")]
        [InlineData("mismatch", "ProtocolMismatch")]
        [InlineData("nonzero", "NonZeroExit")]
        [InlineData("flood", "OutputLimit")]
        [InlineData("stderr-flood", "OutputLimit")]
        [InlineData("utf8", "TransportFailure")]
        public async Task AdversarialPipePeer_IsFailClosedAndCleanedUp(string scenario, string expected)
        {
            HistoricalWorkerCallResult result = await Client(fake: true).AnalyzeAsync(Input(scenario));
            Assert.False(result.Success);
            Assert.Null(result.Result);
            Assert.Equal(expected, result.Failure!.Code.ToString());
            if (scenario == "crash") { Assert.Equal("test crash diagnostic", result.Diagnostics); }
            AssertStopped(result);
        }

        [Fact]
        public async Task Deadline_KillsOwnedProcessAndCompletesPipes()
        {
            HistoricalWorkerCallResult result = await Client(fake: true, deadline: TimeSpan.FromSeconds(1)).AnalyzeAsync(Input("hang"));
            Assert.Equal(HistoricalWorkerClientFailureCode.Timeout, result.Failure!.Code);
            AssertStopped(result);
        }

        [Fact]
        public async Task Cancellation_KillsOwnedProcess()
        {
            using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(1));
            HistoricalWorkerCallResult result = await Client(fake: true).AnalyzeAsync(Input("hang"), cancellation.Token);
            Assert.Equal(HistoricalWorkerClientFailureCode.Cancelled, result.Failure!.Code);
            AssertStopped(result);
        }

        [Fact]
        public async Task PreCancellation_DoesNotStartAProcess()
        {
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            HistoricalWorkerCallResult result = await Client().AnalyzeAsync(Input(), cancellation.Token);
            Assert.Equal(HistoricalWorkerClientFailureCode.Cancelled, result.Failure!.Code);
            Assert.Null(result.ProcessId);
        }

        [Theory]
        [InlineData("missing-result")]
        [InlineData("empty-result-object")]
        [InlineData("missing-entries")]
        [InlineData("identity")]
        [InlineData("success-and-failure")]
        [InlineData("truncated")]
        [InlineData("enum")]
        [InlineData("version")]
        [InlineData("duplicate")]
        [InlineData("location")]
        [InlineData("source-checksum")]
        [InlineData("selector")]
        [InlineData("absolute-path")]
        [InlineData("invalid-line")]
        [InlineData("missing-type-name")]
        public async Task ImportValidation_RejectsTamperedOrIncompleteCanonicalEnvelope(string scenario)
        {
            HistoricalWorkerCallResult real = await Client().AnalyzeAsync(Input());
            AssertSuccess(real);
            JsonObject node = JsonNode.Parse(JsonSerializer.Serialize(new WorkerResponse(WorkerProtocol.Version, "analyze", true,
                real.Identity, real.Result, Provenance: real.Provenance), WorkerProtocol.JsonOptions))!.AsObject();
            switch (scenario)
            {
                case "missing-result": node.Remove("result"); break;
                case "empty-result-object": node["result"] = new JsonObject(); break;
                case "missing-entries": node["result"]!.AsObject().Remove("entries"); break;
                case "identity": node["identity"]!["loadedRoslyn"]![0]!["sha256"] = new string('0', 64); break;
                case "success-and-failure": node["failure"] = JsonNode.Parse("{\"code\":\"analysisFailure\",\"message\":\"failed\",\"details\":[]}"); break;
                case "truncated": node["result"]!["entries"]![0]!["pathsTruncated"] = true; break;
                case "enum": node["result"]!["entries"]![0]!["evidenceKind"] = 0; break;
                case "version": node["protocolVersion"] = 99; break;
                case "location": node["result"]!["entries"]![0]!["paths"]![0]!["steps"]![0]!.AsObject().Remove("kind"); break;
                case "source-checksum": node["provenance"]!["sourceSha256"] = new string('0', 64); break;
                case "selector": node["provenance"]!["methodName"] = "Other"; break;
                case "absolute-path": node["result"]!["entries"]![0]!["paths"]![0]!["steps"]![0]!["filePath"] = "/local/source.cs"; break;
                case "invalid-line": node["result"]!["entries"]![0]!["paths"]![0]!["steps"]![0]!["line"] = 999; break;
                case "missing-type-name": node["result"]!["entries"]![0]!["exceptionType"]!.AsObject().Remove("metadataName"); break;
            }
            string json = node.ToJsonString();
            if (scenario == "duplicate") { json = json.Replace("\"protocolVersion\":2", "\"protocolVersion\":2,\"protocolVersion\":2", StringComparison.Ordinal); }
            HistoricalWorkerCallResult rejected = HistoricalWorkerResponseValidation.Validate(json, "diagnostic-only", 0, Input());
            Assert.False(rejected.Success);
            Assert.Null(rejected.Result);
            Assert.NotNull(rejected.Failure);
            Assert.Equal("diagnostic-only", rejected.Diagnostics);
        }

        [Fact]
        public void Architecture_OnlyNeutralProtocolSourceAndNoAnalyzerDependencies()
        {
            string main = Path.Combine(Root(), "src", "XMLDocNormalizer", "XMLDocNormalizer.csproj");
            XDocument project = XDocument.Load(main);
            Assert.Empty(project.Descendants("ProjectReference"));
            Assert.Single(project.Descendants("Compile").Where(item => (string?)item.Attribute("Link") == "Execution/Historical/WorkerProtocol.cs"));
            Assert.All(typeof(HistoricalWorkerClient).GetFields(BindingFlags.NonPublic | BindingFlags.Instance), field =>
                Assert.False(field.FieldType.Namespace?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true));
            string clientSource = File.ReadAllText(Path.Combine(Root(), "src", "XMLDocNormalizer", "Execution", "Historical", "HistoricalWorkerClient.cs"));
            Assert.DoesNotContain("Microsoft.CodeAnalysis", clientSource);
            Assert.DoesNotContain("ExceptionFlowSummaryAnalysisSession", clientSource);
            Assert.Contains("start.ArgumentList.Add(workerAssemblyPath)", clientSource);
            Assert.DoesNotContain("start.Arguments", clientSource);
            Assert.DoesNotContain("File.Write", clientSource);
        }

        private static WorkerAnalysisInput Input(string source = Source) => new(source, "Fixture", "Root", new());
        private static HistoricalWorkerClient Client(bool fake = false, TimeSpan? deadline = null)
            => new("dotnet", fake ? Path.Combine(Root(), "artifacts", "p5o2b4", "test-process", "bin", Configuration(), "net8.0", "BoundaryTestProcess.dll")
                : WorkerPath(), deadline ?? TimeSpan.FromSeconds(30));
        private static string Configuration() => new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        private static string WorkerPath() => Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", Configuration(), "net8.0", "XMLDocNormalizer.HistoricalWorker.dll");
        private static string Root()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) { return directory.FullName; }
            }
            throw new InvalidOperationException("Cannot locate tool root.");
        }
        private static void AssertSuccess(HistoricalWorkerCallResult result)
        {
            Assert.True(result.Success, result.Failure?.Code + ": " + result.Failure?.Message);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Identity);
            Assert.Null(result.Failure);
            Assert.Empty(result.Diagnostics);
        }
        private static void AssertStopped(HistoricalWorkerCallResult result)
        {
            Assert.NotNull(result.ProcessId);
            try { using Process process = Process.GetProcessById(result.ProcessId!.Value); Assert.True(process.HasExited); }
            catch (ArgumentException) { /* Reaped owned child is absent from the process table. */ }
        }
        private static string Info(Assembly assembly) => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        private static WorkerAssemblyIdentity CurrentIdentity(Assembly assembly)
            => new(assembly.GetName().Name!, assembly.GetName().Version!.ToString(), Info(assembly),
                assembly.ManifestModule.ModuleVersionId.ToString(), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))));
        private static CanonicalExceptionFlowAnalysisResult CurrentResult(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "worker-input.cs");
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll",
                "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
            Assert.Equal(7, references.Length);
            CSharpCompilation compilation = CSharpCompilation.Create("HistoricalWorkerInput", [tree], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            MethodDeclarationSyntax root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            ExceptionFlowSemanticEnvironment environment = new(ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation));
            return RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(
                ExceptionFlowSummaryAnalysisSession.AnalyzeSolutionTransitivelyThrownExceptions(root, environment));
        }
    }
}
