using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
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
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Observe the real detector policy; never implement a substitute Historical declared mode.</summary>
    public sealed class ExceptionFlowHistoricalDeclaredBoundaryTests
    {
        private const string Declared = "project-transitive-declared-exceptions";
        private const string LocalType = "public class LocalException : System.Exception { }";
        private const string Values = "public static System.InvalidOperationException Framework = null!; public static LocalException Local = null!; public static bool Flag;";

        [Fact]
        public void ReportingTypeMembership_UsesOwnedSourceNotSupportingTypesNamesOrXmlTags()
        {
            var payload = Payload("no-local-type");
            var references = payload.Context!.References!.Select(image => MetadataReference.CreateFromImage(image.Image)).ToArray();
            var rootTree = CSharpSyntaxTree.ParseText("public static class Fixture { public static void Root() { } }", path: payload.LogicalPath!);
            var compilation = CSharpCompilation.Create("Reporting", [rootTree], references, new(OutputKind.DynamicallyLinkedLibrary));
            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(rootTree, compilation);
            var supportingTree = CSharpSyntaxTree.ParseText(LocalType, path: payload.LogicalPath!); // Same path is not ownership.
            var supporting = CSharpCompilation.Create("Supporting", [supportingTree], references, new(OutputKind.DynamicallyLinkedLibrary));
            Assert.Empty(supporting.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            context.RegisterSupportingSource(supporting);
            Assert.False(context.HasDeclaredExceptionTypesInReportingScope());
            Assert.False(context.IsInReportingScope(supportingTree)); Assert.True(context.IsInAnalysisScope(supportingTree));
            Assert.False(context.IsDeclaredInReportingScope(supporting.GetTypeByMetadataName("LocalException")!));
            Assert.False(context.IsDeclaredInReportingScope(compilation.GetTypeByMetadataName("System.InvalidOperationException")!));
            var localTree = CSharpSyntaxTree.ParseText(LocalType, path: "/project-a/Local.cs");
            var local = compilation.AddSyntaxTrees(localTree);
            Assert.Empty(local.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var localContext = ProjectClosureSemanticContext.CreateSingleCompilationContext(rootTree, local);
            Assert.True(localContext.HasDeclaredExceptionTypesInReportingScope());
            Assert.True(localContext.IsDeclaredInReportingScope(local.GetTypeByMetadataName("LocalException")!));
            string output = Path.Combine(Root(), "artifacts", "p5o2b16", "reporting-ownership.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                SupportingSourceIsAnalysisOnly = true,
                SamePathNotOwnership = true,
                FrameworkTypeNotDeclared = true,
                LocalSourceTypeQualifies = true,
                NoXmlOrMethodDeclarationGuard = true
            }));
        }

        [Theory]
        [InlineData("no-local-type")]
        [InlineData("xml-only")]
        [InlineData("unused-local-type")]
        [InlineData("mixed-direct-transitive")]
        [InlineData("local-chain")]
        public async Task ActualCurrentPolicyDifferential_DeclaredIsNotAnAnalyzerModeOrXmlFlow(string scenario)
        {
            var payload = Payload(scenario);
            var project = Native(payload, ExceptionAnalysisMode.ProjectTransitive);
            var declared = Native(payload, ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions);
            var direct = Native(payload, ExceptionAnalysisMode.Direct);
            var solution = Native(payload, ExceptionAnalysisMode.SolutionTransitive);
            Assert.NotEqual(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(declared.Findings));
            Assert.Equal(project.Result, solution.Result); Assert.Equal(project.Summaries, solution.Summaries);
            Assert.Equal(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(solution.Findings));
            if (scenario is "no-local-type" or "xml-only")
            {
                Assert.False(declared.HasDeclaredTypes); Assert.Equal(direct.Result, declared.Result);
                Assert.Equal(0, declared.GraphCount); Assert.Empty(declared.Summaries);
                Assert.Empty(declared.Findings);
            }
            else
            {
                Assert.True(declared.HasDeclaredTypes); Assert.Equal(project.Result, declared.Result);
                Assert.Equal(project.Summaries, declared.Summaries); Assert.Equal(project.GraphCount, declared.GraphCount);
                Assert.DoesNotContain(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("System.InvalidOperationException", StringComparison.Ordinal));
            }
            if (scenario == "xml-only")
            {
                Assert.Empty(project.Result.Entries); Assert.Contains(project.Findings, finding => finding.Smell.ID == "DOC632");
                Assert.Contains(direct.Findings, finding => finding.Smell.ID == "DOC630");
            }
            if (scenario == "mixed-direct-transitive")
            {
                Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC610" && finding.Message.Contains("System.ArgumentException", StringComparison.Ordinal));
                Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("LocalException", StringComparison.Ordinal));
                Assert.Equal(3, declared.Result.Entries.Length); // No duplicate XML/type-declaration entry.
            }
            if (scenario == "local-chain")
            {
                Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC632" && finding.Message.Contains("LocalException", StringComparison.Ordinal));
                Assert.Contains(project.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == "/project-a/Helper.cs");
            }
            Assert.All(project.Result.Entries, entry => Assert.Equal(CanonicalExceptionFlowEvidenceKind.Proven, entry.EvidenceKind));
            Assert.Empty(project.Result.Uncertainties); Assert.Empty(declared.Result.Uncertainties);

            var original = await Original(payload);
            var rejected = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(
                original with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions }, new(new(Client())));
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, rejected.PayloadProjection.Failure!.Code);
            Assert.Null(rejected.Dispatch); Assert.Null(rejected.Result);
            var raw = await Client().AnalyzeAsync(payload with { Context = payload.Context! with { AnalysisMode = Declared } });
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, raw.Failure!.Code);
            Assert.Null(raw.ProcessId); Assert.Null(raw.Result);

            var controls = new List<object>();
            foreach (var (mode, name, native) in new[]
            {
                (ExceptionAnalysisMode.Direct, "direct", direct),
                (ExceptionAnalysisMode.ProjectTransitive, "project-transitive", project),
                (ExceptionAnalysisMode.SolutionTransitive, "solution-transitive", solution)
            })
            {
                var input = original with { AnalysisMode = mode };
                var projected = HistoricalWorkerPayloadProjection.Project(input); Assert.True(projected.Succeeded);
                var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
                var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
                Assert.True(first.Succeeded, first.Dispatch?.Routing?.HistoricalFailure?.Message); Assert.True(second.Succeeded);
                Assert.Equal(native.Result, first.Result); Assert.Equal(first.Result, second.Result);
                Assert.Null(first.Dispatch!.Routing!.CurrentAnalysis); Assert.Null(second.Dispatch!.Routing!.CurrentAnalysis);
                var worker = first.Dispatch.Routing.HistoricalAnalysis!;
                Assert.Equal(WorkerProtocol.Provenance(projected.Value!.Payload), worker.Provenance);
                var graph = await Graph(payload with { Context = payload.Context! with { AnalysisMode = name } });
                Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32());
                Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray());
                Assert.Equal(native.Summaries, graph.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()).ToArray());
                Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean());
                Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32());
                Assert.All(graph.GetProperty("ownership").EnumerateArray(), item => Assert.True(item.GetProperty("stableModel").GetBoolean()));
                Stopped(worker); Stopped(second.Dispatch.Routing.HistoricalAnalysis!);
                controls.Add(new
                {
                    Mode = name,
                    Native = native,
                    Historical = first.Result,
                    RepeatedHistorical = second.Result,
                    Graph = graph,
                    worker.Provenance,
                    worker.Identity,
                    FirstProcess = worker.ProcessId,
                    FirstProcessExit = worker.ProcessExit,
                    SecondProcess = second.Dispatch.Routing.HistoricalAnalysis!.ProcessId,
                    SecondProcessExit = second.Dispatch.Routing.HistoricalAnalysis.ProcessExit,
                    NoCurrentFallback = true,
                    ProcessesStopped = true
                });
            }
            NoHistoricalLoaded();
            await Evidence("case-" + scenario, new
            {
                Scenario = scenario,
                Project = project,
                Declared = declared,
                Direct = direct,
                Solution = solution,
                OriginalFailure = rejected.PayloadProjection.Failure,
                RawFailure = raw.Failure,
                Controls = controls,
                GenuineCurrentDetector = true,
                NoDeclaredHistoricalParityClaim = true,
                NoFindingImport = true,
                NoHistoricalLoadedInMain = true
            });
        }

        [Fact]
        public async Task Uncertainty_RelevanceIsDetectorPolicy_NotFilteredRawAnalysisOrXmlProof()
        {
            var payload = Payload("uncertain");
            var project = Native(payload, ExceptionAnalysisMode.ProjectTransitive);
            var declared = Native(payload, ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions);
            Assert.True(declared.HasDeclaredTypes); Assert.NotEmpty(project.Result.Uncertainties);
            Assert.Equal(project.Result, declared.Result); Assert.Equal(project.Summaries, declared.Summaries);
            Assert.Equal(2, project.Findings.Count(finding => finding.Smell.ID == "DOC631"));
            Assert.Single(declared.Findings, finding => finding.Smell.ID == "DOC631");
            Assert.DoesNotContain(project.Findings.Concat(declared.Findings), finding => finding.Smell.ID == "DOC632");
            var worker = await Client().AnalyzeAsync(payload);
            Assert.Equal(WorkerFailureCode.AnalysisFailure, worker.Failure!.WorkerFailure!.Code); Assert.Null(worker.Result);
            Stopped(worker); NoHistoricalLoaded();
            await Evidence("uncertainty", new { Project = project, Declared = declared, worker.Failure, ProcessesStopped = true, NoPartialResult = true });
        }

        [Theory]
        [InlineData("missing-mode")]
        [InlineData("default-mode")]
        [InlineData("unknown-mode")]
        [InlineData("declared-mode")]
        [InlineData("declared-missing-scope")]
        [InlineData("declared-missing-source")]
        [InlineData("declared-foreign-owner")]
        [InlineData("declared-missing-parse")]
        [InlineData("declared-invalid-parse")]
        [InlineData("declared-conflicting-scope")]
        public async Task UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine(string scenario)
        {
            var payload = Payload("local-chain"); var context = payload.Context!;
            context = context with { AnalysisMode = scenario == "missing-mode" ? null! : scenario == "default-mode" ? "" : scenario == "unknown-mode" ? "default" : Declared };
            if (scenario == "declared-missing-scope") { context = context with { Scope = null }; }
            if (scenario == "declared-missing-source") { context = context with { AdditionalSources = [] }; }
            if (scenario == "declared-foreign-owner") { context = context with { AdditionalSources = [context.AdditionalSources![0] with { AssemblyName = "Foreign" }] }; }
            if (scenario == "declared-missing-parse") { context = context with { ParseContext = null }; }
            if (scenario == "declared-invalid-parse") { context = context with { ParseContext = context.ParseContext! with { LanguageVersion = "latest" } }; }
            if (scenario == "declared-conflicting-scope") { context = context with { Scope = context.Scope! with { Kind = "solution-v1" } }; }
            var result = await Client().AnalyzeAsync(payload with { Context = context });
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code); Assert.Null(result.Result); Assert.Null(result.ProcessId);
            await Evidence("negative-" + scenario, new { Scenario = scenario, result.Failure, NoWorker = true, NoResult = true, NoFallback = true });
        }

        [Fact]
        public async Task ExistingSupportedWorkerErrors_PropagateWithoutCompilerDowngradeOrModeFallback()
        {
            var payload = Payload("no-local-type");
            var source = payload.Context!.AdditionalSources![0];
            var unavailable = await Client().AnalyzeAsync(payload with
            {
                Context = payload.Context with
                { AdditionalSources = [source with { ParseContext = source.ParseContext! with { LanguageVersion = "999" } }] }
            });
            Assert.Equal(WorkerFailureCode.UnsupportedContext, unavailable.Failure!.WorkerFailure!.Code);
            var broken = await Client().AnalyzeAsync(payload with { Source = "public static class Fixture { public static void Root() { Missing(); } }" });
            Assert.Equal(WorkerFailureCode.CompilationFailure, broken.Failure!.WorkerFailure!.Code);
            Assert.Null(unavailable.Result); Assert.Null(broken.Result);
            Stopped(unavailable); Stopped(broken); NoHistoricalLoaded();
            await Evidence("worker-errors", new { ParseFailure = unavailable.Failure, CompilationFailure = broken.Failure, ProcessesStopped = true, NoFallback = true, DeclaredNeverStarted = true });
        }

        private sealed record Snapshot(CanonicalExceptionFlowAnalysisResult Result, int GraphCount, string[] Summaries, List<Finding> Findings,
            bool HasDeclaredTypes, object[] Ownership, bool RepeatedFlowGraphAndFindingsEqual);

        private static Snapshot Native(WorkerAnalysisInput input, ExceptionAnalysisMode mode)
        {
            var context = input.Context!;
            var documents = new[] { new WorkerSourceDocument(input.LogicalPath!, input.Source!, WorkerProtocol.SourceHash(input.Source!), ParseContext: context.ParseContext) }.Concat(context.AdditionalSources!).ToArray();
            var trees = context.Scope!.OrderedSourcePaths!.Select(path =>
            {
                var document = documents.Single(source => source.LogicalPath == path);
                Assert.True(LanguageVersionFacts.TryParse(document.ParseContext!.LanguageVersion, out var language));
                return CSharpSyntaxTree.ParseText(document.Text, new CSharpParseOptions(language, DocumentationMode.Parse, preprocessorSymbols: document.ParseContext.PreprocessorSymbols), path);
            }).ToArray();
            var compilation = CSharpCompilation.Create(context.AssemblyName, trees, context.References!.Select(image => MetadataReference.CreateFromImage(image.Image)), new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var tree = trees.Single(source => source.FilePath == input.LogicalPath);
            var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var semantic = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            var environment = new ExceptionFlowSemanticEnvironment(semantic); var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(environment);
            var options = new XmlDocOptions { ExceptionAnalysisMode = mode };
            // Invoke the ACTUAL productive mode seam; do not copy its guard into this test.
            var analyze = typeof(XmlDocExceptionSemanticDetector).GetMethod("AnalyzeConfiguredExceptionFlow", BindingFlags.Static | BindingFlags.NonPublic)!;
            CanonicalExceptionFlowAnalysisResult Analyze() => RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult((ExceptionFlowAnalysisResult)analyze.Invoke(null,
                [root, compilation, semantic, environment, options, ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions(root, environment), session])!);
            var result = Analyze();
            var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
            string[] Summaries() => (string[])typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Summaries", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [graph])!;
            var summaries = Summaries(); int count = graph.Count;
            List<Finding> Findings() => XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session);
            var findings = Findings();
            Assert.Equal(result, Analyze()); Assert.Equal(count, graph.Count); Assert.Equal(summaries, Summaries());
            Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(Findings()));
            var ownership = trees.Select(owned =>
            {
                Assert.True(environment.TryGetSemanticModel(owned, out var first)); Assert.True(environment.TryGetSemanticModel(owned, out var second));
                Assert.Same(first, second); Assert.Same(compilation, first.Compilation);
                return (object)new { LogicalPath = owned.FilePath, AssemblyName = compilation.AssemblyName, StableModel = true, Reporting = semantic.IsInReportingScope(owned) };
            }).ToArray();
            return new(result, count, summaries, findings, semantic.HasDeclaredExceptionTypesInReportingScope(), ownership, true);
        }

        private static WorkerAnalysisInput Payload(string scenario)
        {
            var payload = (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, ["project-transitive", "12", 2, true, false])!;
            string docs = "/// <summary>Observe real detector policy.</summary>\r\n";
            string body = "Helper.A();";
            string helper = "public static class Helper { public static System.InvalidOperationException Framework = null!; public static void A() { throw Framework; } }";
            if (scenario == "xml-only") { docs += "/// <exception cref=\"System.InvalidOperationException\">Only documentation.</exception>\r\n"; helper = "public static class Helper { public static void A() { } }"; }
            if (scenario == "unused-local-type") { helper += "\r\n" + LocalType; }
            if (scenario == "mixed-direct-transitive")
            {
                body = "if (Helper.Flag) throw new System.ArgumentException(); else Helper.A();";
                helper = "public static class Helper { " + Values + " public static void A() { if (Flag) throw Framework; else throw Local; } }\r\n" + LocalType;
            }
            if (scenario == "local-chain")
            {
                docs += "/// <exception cref=\"LocalException\">Declared, not thrown.</exception>\r\n";
                helper = "public static class Helper { " + Values + " public static void A() { B(); } public static void B() { throw Framework; } }\r\n" + LocalType;
            }
            if (scenario == "uncertain")
            {
                docs += "/// <exception cref=\"LocalException\">Uncertain.</exception>\r\n/// <exception cref=\"System.InvalidOperationException\">Uncertain.</exception>\r\n";
                body = "System.GC.Collect();"; helper = LocalType;
            }
            string target = "public static class Fixture { public static System.ArgumentException DirectValue = null!;\r\n" + docs + "public static void Root() { " + body + " } }";
            var source = payload.Context!.AdditionalSources![0];
            return payload with { Source = target, Context = payload.Context with { AdditionalSources = [source with { Text = helper, Sha256 = WorkerProtocol.SourceHash(helper) }] } };
        }

        // Reuse B14's genuine P5A/P5G/P5I/P5J/P5C/P5E factory handoff and isolated graph observer unchanged.
        private static Task<HistoricalWorkerPayloadProjectionInput> Original(WorkerAnalysisInput payload) => (Task<HistoricalWorkerPayloadProjectionInput>)typeof(ExceptionFlowHistoricalScopeTests).GetMethod("ValidatedInput", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload])!;
        private static Task<JsonElement> Graph(WorkerAnalysisInput payload) => (Task<JsonElement>)typeof(ExceptionFlowHistoricalScopeTests).GetMethod("Probe", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload, "--b14-graph", null])!;
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static void Stopped(HistoricalWorkerCallResult call)
            => HistoricalWorkerLifetimeAssertions.Stopped(call);
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        {
            string path = Path.Combine(Root(), "artifacts", "p5o2b16", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
