using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Tests the common productive Main route owner with explicit choices and the genuine B4 client.</summary>
    public sealed class ExceptionFlowAnalysisRouterTests
    {
        private const string Source = "public static class Fixture { public static void Root() { Thrower(); } public static void Second() { Thrower(); } static void Thrower() { throw null; } }";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExplicitCurrent_UsesExistingSessionWithoutAnyWorker(bool configuredUnstartableWorker)
        {
            Fixture fixture = CreateFixture(Source);
            ExceptionFlowAnalysisRouter router = new(configuredUnstartableWorker
                ? new HistoricalWorkerClient(Path.Combine(Root(), "artifacts", "p5o2b5", "never-start-this-host"), WorkerPath(), TimeSpan.FromSeconds(30)) : null);
            object graph = typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Session)!;
            foreach (string method in new[] { "Root", "Second", "Root" })
            {
                ExceptionFlowAnalysisRoutingResult result = await router.AnalyzeAsync(new(ExceptionFlowAnalyzerSelection.Current,
                    Current: new(fixture.Method(method), fixture.Session)));
                Assert.True(result.Succeeded, result.RoutingFailure?.Message);
                Assert.Equal(ExceptionFlowAnalyzerSelection.Current, result.Selection);
                Assert.NotNull(result.CurrentAnalysis);
                Assert.Null(result.HistoricalAnalysis);
                Assert.Null(result.HistoricalFailure);
                Assert.Equal(DirectCurrent(fixture, method), result.Result);
                Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Session));
            }
        }

        [Fact]
        public async Task ExplicitHistorical_UsesRealB4ClientWithCurrentParityAndRuntimeIsolation()
        {
            Fixture fixture = CreateFixture(Source);
            Assembly common = typeof(Compilation).Assembly;
            Assembly csharp = typeof(CSharpCompilation).Assembly;
            Guid beforeCommon = common.ManifestModule.ModuleVersionId;
            Guid beforeCsharp = csharp.ManifestModule.ModuleVersionId;
            ExceptionFlowAnalysisRouter router = new(Client());
            ExceptionFlowAnalysisRoutingResult current = await router.AnalyzeAsync(new(ExceptionFlowAnalyzerSelection.Current,
                Current: new(fixture.Method("Root"), fixture.Session)));
            ExceptionFlowAnalysisRoutingResult historical = await router.AnalyzeAsync(new(ExceptionFlowAnalyzerSelection.Historical, Historical: Input(Source)));
            Assert.True(current.Succeeded);
            Assert.True(historical.Succeeded, historical.HistoricalFailure?.Message);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, historical.Selection);
            Assert.Null(historical.CurrentAnalysis);
            Assert.Null(historical.RoutingFailure);
            Assert.Same(historical.HistoricalAnalysis!.Result, historical.Result);
            Assert.Equal(current.Result, historical.Result);
            Assert.Equal(DirectCurrent(fixture, "Root"), current.Result);
            Assert.Equal("NullReferenceException", Assert.Single(historical.Result!.Entries).ExceptionType.MetadataName);
            Assert.Equal(2, historical.Result.Entries[0].Paths[0].Steps.Length);
            Assert.NotEqual(Environment.ProcessId, historical.HistoricalAnalysis.ProcessId);
            AssertStopped(historical.HistoricalAnalysis);
            Assert.Equal(beforeCommon, common.ManifestModule.ModuleVersionId);
            Assert.Equal(beforeCsharp, csharp.ManifestModule.ModuleVersionId);
            Assert.StartsWith("5.0.0-2.25567.12+", Info(common));
            Assert.StartsWith("5.0.0-2.25567.12+", Info(csharp));
            Assert.All(historical.HistoricalAnalysis.Identity!.LoadedRoslyn, engine => Assert.StartsWith("5.0.0-2.25451.107+", engine.InformationalVersion));
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
                assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
                || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
            string evidence = Path.Combine(Root(), "artifacts", "p5o2b5", "routing-runtime-parity.json");
            Directory.CreateDirectory(Path.GetDirectoryName(evidence)!);
            await File.WriteAllTextAsync(evidence, JsonSerializer.Serialize(new
            {
                Current = new[] { Identity(common), Identity(csharp) },
                Historical = historical.HistoricalAnalysis.Identity,
                historical.HistoricalAnalysis.ProcessId,
                historical.HistoricalAnalysis.Provenance,
                Result = historical.Result,
                CanonicalParity = true,
                CurrentUsesExistingSession = true,
                HistoricalUsesB4Client = true,
                HistoricalLoadedInCaller = false,
                ProcessStopped = true
            }, WorkerProtocol.JsonOptions));
        }

        [Fact]
        public async Task CurrentUncertainty_IsPreservedRatherThanCollapsedIntoWorkerCompletenessPolicy()
        {
            const string source = "public static class Fixture { public static void Root() { System.Console.WriteLine(); } }";
            Fixture fixture = CreateFixture(source);
            CanonicalExceptionFlowAnalysisResult expected = DirectCurrent(fixture, "Root");
            Assert.NotEmpty(expected.Uncertainties);
            ExceptionFlowAnalysisRoutingResult result = await new ExceptionFlowAnalysisRouter().AnalyzeAsync(new(
                ExceptionFlowAnalyzerSelection.Current, Current: new(fixture.Method("Root"), fixture.Session)));
            Assert.True(result.Succeeded);
            Assert.Equal(expected, result.Result);
            Assert.Null(result.HistoricalAnalysis);
            Assert.Null(result.RoutingFailure);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(int.MaxValue)]
        public async Task UnsupportedSelection_NeverFallsBackToCurrent(int choice)
        {
            Fixture fixture = CreateFixture(Source);
            ExceptionFlowAnalysisRoutingResult result = await new ExceptionFlowAnalysisRouter(Client()).AnalyzeAsync(new(
                (ExceptionFlowAnalyzerSelection)choice, Current: new(fixture.Method("Root"), fixture.Session), Historical: Input(Source)));
            AssertRoutingFailure(result, ExceptionFlowAnalysisRoutingFailureCode.InvalidSelection);
        }

        [Fact]
        public async Task NullRequest_IsNotAnImplicitCurrentSelection()
            => AssertRoutingFailure(await new ExceptionFlowAnalysisRouter(Client()).AnalyzeAsync(null), ExceptionFlowAnalysisRoutingFailureCode.InvalidSelection);

        [Theory]
        [InlineData("current-missing")]
        [InlineData("historical-missing")]
        [InlineData("current-wrong-input")]
        [InlineData("historical-wrong-input")]
        [InlineData("both-current")]
        [InlineData("both-historical")]
        [InlineData("null-member")]
        [InlineData("null-session")]
        public async Task AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute(string scenario)
        {
            Fixture fixture = CreateFixture(Source);
            CurrentExceptionFlowAnalysisInput current = new(fixture.Method("Root"), fixture.Session);
            WorkerAnalysisInput historical = Input(Source);
            ExceptionFlowAnalysisRoutingRequest request = scenario switch
            {
                "current-missing" => new(ExceptionFlowAnalyzerSelection.Current),
                "historical-missing" => new(ExceptionFlowAnalyzerSelection.Historical),
                "current-wrong-input" => new(ExceptionFlowAnalyzerSelection.Current, Historical: historical),
                "historical-wrong-input" => new(ExceptionFlowAnalyzerSelection.Historical, Current: current),
                "both-current" => new(ExceptionFlowAnalyzerSelection.Current, current, historical),
                "both-historical" => new(ExceptionFlowAnalyzerSelection.Historical, current, historical),
                "null-member" => new(ExceptionFlowAnalyzerSelection.Current, Current: current with { Member = null! }),
                _ => new(ExceptionFlowAnalyzerSelection.Current, Current: current with { Session = null! })
            };
            AssertRoutingFailure(await new ExceptionFlowAnalysisRouter(Client()).AnalyzeAsync(request), ExceptionFlowAnalysisRoutingFailureCode.InvalidInput);
        }

        [Fact]
        public async Task HistoricalWithoutConfiguredEndpoint_IsFailureNotCurrentFallback()
            => AssertRoutingFailure(await new ExceptionFlowAnalysisRouter().AnalyzeAsync(new(
                ExceptionFlowAnalyzerSelection.Historical, Historical: Input(Source))), ExceptionFlowAnalysisRoutingFailureCode.HistoricalUnavailable);

        [Theory]
        [InlineData("public static class Fixture { public static void Root() { Missing(); } }", "CompilationFailure")]
        [InlineData("public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure")]
        public async Task HistoricalAnalysisFailure_PreservesOriginalB4FailureContract(string source, string code)
        {
            ExceptionFlowAnalysisRoutingResult result = await new ExceptionFlowAnalysisRouter(Client()).AnalyzeAsync(new(
                ExceptionFlowAnalyzerSelection.Historical, Historical: Input(source)));
            Assert.False(result.Succeeded);
            Assert.Null(result.Result);
            Assert.Null(result.CurrentAnalysis);
            Assert.Null(result.RoutingFailure);
            Assert.Same(result.HistoricalAnalysis!.Failure, result.HistoricalFailure);
            Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, result.HistoricalFailure!.Code);
            Assert.Equal(code, result.HistoricalFailure.WorkerFailure!.Code.ToString());
            AssertStopped(result.HistoricalAnalysis);
        }

        [Fact]
        public async Task UnsupportedHistoricalProfile_RemainsB4UnsupportedInput()
        {
            WorkerAnalysisInput input = Input(Source) with { Context = new(LanguageVersion: "preview") };
            ExceptionFlowAnalysisRoutingResult result = await new ExceptionFlowAnalysisRouter(Client()).AnalyzeAsync(new(
                ExceptionFlowAnalyzerSelection.Historical, Historical: input));
            Assert.False(result.Succeeded);
            Assert.Null(result.CurrentAnalysis);
            Assert.Null(result.Result);
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.HistoricalFailure!.Code);
            Assert.Null(result.HistoricalAnalysis!.ProcessId);
        }

        [Fact]
        public async Task HistoricalStartFailure_RemainsB4StartFailure()
        {
            HistoricalWorkerClient client = new(Path.Combine(Root(), "artifacts", "p5o2b5", "never-start-this-host"), WorkerPath(), TimeSpan.FromSeconds(30));
            ExceptionFlowAnalysisRoutingResult result = await new ExceptionFlowAnalysisRouter(client).AnalyzeAsync(new(
                ExceptionFlowAnalyzerSelection.Historical, Historical: Input(Source)));
            Assert.False(result.Succeeded);
            Assert.Null(result.Result);
            Assert.Equal(HistoricalWorkerClientFailureCode.StartFailure, result.HistoricalFailure!.Code);
            Assert.Same(result.HistoricalAnalysis!.Failure, result.HistoricalFailure);
        }

        [Fact]
        public async Task PreCancelledCurrent_DoesNotRunOrUseWorker()
        {
            Fixture fixture = CreateFixture(Source);
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();
            AssertRoutingFailure(await new ExceptionFlowAnalysisRouter(Client()).AnalyzeAsync(new(
                ExceptionFlowAnalyzerSelection.Current, Current: new(fixture.Method("Root"), fixture.Session)), cancelled.Token),
                ExceptionFlowAnalysisRoutingFailureCode.Cancelled);
        }

        [Fact]
        public async Task PreCancelledHistorical_PreservesB4Cancellation()
        {
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();
            ExceptionFlowAnalysisRoutingResult result = await new ExceptionFlowAnalysisRouter(Client()).AnalyzeAsync(new(
                ExceptionFlowAnalyzerSelection.Historical, Historical: Input(Source)), cancelled.Token);
            Assert.False(result.Succeeded);
            Assert.Equal(HistoricalWorkerClientFailureCode.Cancelled, result.HistoricalFailure!.Code);
            Assert.Null(result.HistoricalAnalysis!.ProcessId);
        }

        [Fact]
        public async Task CurrentExecutionException_IsNotAnEmptySuccess()
        {
            Fixture fixture = CreateFixture(Source);
            ExceptionFlowSummaryAnalysisSession brokenSession = new(null!);
            AssertRoutingFailure(await new ExceptionFlowAnalysisRouter().AnalyzeAsync(new(
                ExceptionFlowAnalyzerSelection.Current, Current: new(fixture.Method("Root"), brokenSession))),
                ExceptionFlowAnalysisRoutingFailureCode.CurrentExecutionFailure);
        }

        [Fact]
        public void Architecture_RouterIsMainOnlyAndNeverSerializesTheLocalRequest()
        {
            Assert.Equal(typeof(ExceptionFlowAnalysisRouter).Assembly, typeof(HistoricalWorkerClient).Assembly);
            Assert.Equal(0, (int)ExceptionFlowAnalyzerSelection.Unspecified);
            Assert.NotEqual(default, ExceptionFlowAnalyzerSelection.Current);
            string source = File.ReadAllText(Path.Combine(Root(), "src", "XMLDocNormalizer", "Execution", "Analysis", "ExceptionFlowAnalysisRouter.cs"));
            Assert.Contains("request.Current.Session.Analyze(request.Current.Member)", source);
            Assert.Contains("historicalClient.AnalyzeAsync(request.Historical, cancellationToken)", source);
            foreach (string forbidden in new[] { "Process.Start", "ProcessStartInfo", "JsonSerializer", "Assembly.Load", "InformationalVersion", "GetTypeByMetadataName", "Dapper", "Task.Run" })
            {
                Assert.DoesNotContain(forbidden, source);
            }
            Assert.Single(typeof(ExceptionFlowAnalysisRouter).GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
                field => field.FieldType == typeof(HistoricalWorkerClient));
            Assert.DoesNotContain(typeof(ExceptionFlowAnalysisRouter).Assembly.GetReferencedAssemblies(), assembly =>
                assembly.Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical");
        }

        private static void AssertRoutingFailure(ExceptionFlowAnalysisRoutingResult result, ExceptionFlowAnalysisRoutingFailureCode code)
        {
            Assert.False(result.Succeeded);
            Assert.Null(result.Result);
            Assert.Null(result.CurrentAnalysis);
            Assert.Null(result.HistoricalAnalysis);
            Assert.Equal(code, result.RoutingFailure!.Code);
        }
        private static WorkerAnalysisInput Input(string source) => new(source, "Fixture", "Root", new());
        private static HistoricalWorkerClient Client() => new("dotnet", WorkerPath(), TimeSpan.FromSeconds(30));
        private static string WorkerPath() => Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin",
            new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "net8.0", "XMLDocNormalizer.HistoricalWorker.dll");
        private static string Root()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) { return directory.FullName; }
            }
            throw new InvalidOperationException("Cannot locate tool root.");
        }
        private static string Info(Assembly assembly) => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        private static WorkerAssemblyIdentity Identity(Assembly assembly) => new(assembly.GetName().Name!, assembly.GetName().Version!.ToString(), Info(assembly),
            assembly.ManifestModule.ModuleVersionId.ToString(), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))));
        private static void AssertStopped(HistoricalWorkerCallResult result)
        {
            Assert.NotNull(result.ProcessId);
            try { using Process process = Process.GetProcessById(result.ProcessId!.Value); Assert.True(process.HasExited); }
            catch (ArgumentException) { }
        }
        private sealed record Fixture(SyntaxTree Tree, ExceptionFlowSemanticEnvironment Environment, ExceptionFlowSummaryAnalysisSession Session)
        {
            internal MethodDeclarationSyntax Method(string name) => Tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == name);
        }
        private static Fixture CreateFixture(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "worker-input.cs");
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
            Assert.Equal(7, references.Length);
            CSharpCompilation compilation = CSharpCompilation.Create("HistoricalWorkerInput", [tree], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            ExceptionFlowSemanticEnvironment environment = new(ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation));
            return new(tree, environment, ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(environment));
        }
        private static CanonicalExceptionFlowAnalysisResult DirectCurrent(Fixture fixture, string method)
            => RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(
                ExceptionFlowSummaryAnalysisSession.AnalyzeSolutionTransitivelyThrownExceptions(fixture.Method(method), fixture.Environment));
    }
}
