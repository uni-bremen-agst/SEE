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

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Bounded source-origin equivalence and explicit witnesses of independent deferred input families.</summary>
    public sealed class ExceptionFlowHistoricalInputEquivalenceTests
    {
        private const string Source = "public static class Fixture { public static void Root() { throw null; } }";

        [Fact]
        public async Task RealExternalProjection_FullFlowSourceParityGraphRepetitionAndIsolation()
        {
            var input = (HistoricalWorkerPayloadProjectionInput)typeof(ExceptionFlowHistoricalPayloadProjectionTests)
                .GetMethod("Historical", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
            var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.True(first.Succeeded, first.Dispatch?.Routing?.HistoricalFailure?.Message);
            Assert.True(second.Succeeded);
            Assert.Equal(input.Source.Document.Name, first.PayloadProjection.Value!.Payload.LogicalPath);
            Assert.Equal(input.Source.Tree.FilePath, first.PayloadProjection.Value.Payload.LogicalPath);
            Assert.Equal(input.Source.Document.Name, first.Dispatch!.Routing!.HistoricalAnalysis!.Provenance!.LogicalPath);
            var native = Native(input.Source.Text.ToString(), input.Source.Document.Name);
            Assert.Equal(native.Result, first.Result);
            Assert.Equal(first.Result, second.Result);
            Assert.Empty(first.Result!.Uncertainties);
            Assert.Null(first.Dispatch.Routing.CurrentAnalysis);
            Assert.NotEqual(first.Dispatch.Routing.HistoricalAnalysis.ProcessId, second.Dispatch!.Routing!.HistoricalAnalysis!.ProcessId);
            AssertStopped(first.Dispatch.Routing.HistoricalAnalysis.ProcessId!.Value);
            AssertStopped(second.Dispatch.Routing.HistoricalAnalysis.ProcessId!.Value);
            AssertNoHistoricalLoaded();

            var graph = await HistoricalGraph(Source, input.Source.Document.Name);
            Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32());
            Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32());
            Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean());
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())));
            Assert.True(native.RepeatedFindingsEqual);
            await WriteEvidence("source-path-production", new
            {
                AutomaticProjection = true,
                OriginalPath = input.Source.Document.Name,
                FullCanonicalFlowEqual = true,
                NoNormalizationForEquality = true,
                Graph = graph,
                Native = native,
                Historical = first.Result,
                RepeatedHistorical = second.Result,
                FirstProvenance = first.Dispatch.Routing.HistoricalAnalysis.Provenance,
                FirstIdentity = first.Dispatch.Routing.HistoricalAnalysis.Identity,
                FirstProcess = first.Dispatch.Routing.HistoricalAnalysis.ProcessId,
                SecondProcess = second.Dispatch.Routing.HistoricalAnalysis.ProcessId,
                ProcessesStopped = true,
                HistoricalLoadedInMain = false,
                NoCurrentFallback = true,
                FindingsScope = "complete Current findings repeat identically; cross-engine findings import remains out of scope"
            });
        }

        [Theory]
        [InlineData("/src/original.cs")]
        [InlineData("D:\\logical only\\Grüße.cs")]
        [InlineData("../logical/源.cs")]
        [InlineData("not-a-real-file:identity.cs")]
        public async Task LogicalPath_IsExactDataNotFileIoOrNormalization(string path)
        {
            const string source = "public static class Fixture { public static void Root() { Thrower(); }\r\n#line 999 \"mapped.cs\"\r\n static void Thrower() { throw null; } }";
            var native = Native(source, path);
            var first = await Client().AnalyzeAsync(new(source, "Fixture", "Root", LogicalPath: path));
            var second = await Client().AnalyzeAsync(new(source, "Fixture", "Root", LogicalPath: path));
            Assert.True(first.Success, first.Failure?.Message);
            Assert.True(second.Success);
            Assert.Equal(path, first.Provenance!.LogicalPath);
            Assert.Equal(native.Result, first.Result);
            Assert.Equal(first.Result, second.Result);
            Assert.All(first.Result!.Entries.SelectMany(entry => entry.Paths).SelectMany(flow => flow.Steps), step => Assert.Equal(path, step.FilePath));
            AssertStopped(first.ProcessId!.Value);
            AssertStopped(second.ProcessId!.Value);
            AssertNoHistoricalLoaded();
        }

        [Fact]
        public async Task LegacyAbsentPath_RetainsExistingExplicitBoundedPolicy()
        {
            var result = await Client().AnalyzeAsync(new(Source, "Fixture", "Root"));
            Assert.True(result.Success);
            Assert.Equal("worker-input.cs", result.Provenance!.LogicalPath);
            Assert.Equal(Native(Source, "worker-input.cs").Result, result.Result);
            AssertStopped(result.ProcessId!.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("source\nforged.cs")]
        [InlineData("source\0forged.cs")]
        [InlineData("oversize")]
        public async Task InvalidPath_IsTypedBeforeProcessStartAndNeverNormalized(string path)
        {
            if (path == "oversize") { path = new string('x', WorkerProtocol.MaximumLogicalPathCharacters + 1); }
            var result = await Client().AnalyzeAsync(new(Source, "Fixture", "Root", LogicalPath: path));
            Assert.False(result.Success);
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code);
            Assert.Null(result.Result);
            Assert.Null(result.ProcessId);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConflictingReturnedPath_IsRejectedInProvenanceAndEveryFlowStep(bool step)
        {
            var input = new WorkerAnalysisInput(Source, "Fixture", "Root", LogicalPath: "/original.cs");
            var real = await Client().AnalyzeAsync(input);
            Assert.True(real.Success);
            var response = new WorkerResponse(WorkerProtocol.Version, "analyze", true, real.Identity, real.Result, Provenance: real.Provenance);
            var node = JsonNode.Parse(JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions))!;
            if (step) { node["result"]!["entries"]![0]!["paths"]![0]!["steps"]![0]!["filePath"] = "worker-input.cs"; }
            else { node["provenance"]!["logicalPath"] = "worker-input.cs"; }
            var rejected = HistoricalWorkerResponseValidation.Validate(node.ToJsonString(), "", 0, input);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, rejected.Failure!.Code);
            Assert.Null(rejected.Result);
            AssertStopped(real.ProcessId!.Value);
        }

        [Fact]
        public void UnknownProtocolField_IsNotSilentlyIgnored()
            => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkerAnalysisInput>("{\"source\":\"x\",\"logicalPath\":\"/x.cs\",\"pathPolicy\":\"guess\"}", WorkerProtocol.JsonOptions));

        [Fact]
        public async Task MissingSourceAndUnsupportedOptions_RetainTypedFailureWithoutFallback()
        {
            foreach (var input in new[] { new WorkerAnalysisInput(null, "Fixture", "Root", LogicalPath: "/real.cs"),
                new WorkerAnalysisInput(Source, "Fixture", "Root", new(LanguageVersion: "preview"), "/real.cs") })
            {
                var result = await Client().AnalyzeAsync(input);
                Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code);
                Assert.Null(result.ProcessId);
                Assert.Null(result.Result);
            }
        }

        [Fact]
        public async Task Inventory_DifferentialWitnessesSeparateParseReferencesSourcesAndNullable()
        {
            const string conditional = "public static class Fixture { public static void Root() {\n#if THROW\nthrow null;\n#endif\n} }";
            var defined = Native(conditional, "worker-input.cs", new(LanguageVersion.CSharp12, preprocessorSymbols: ["THROW"]));
            var noDefines = Native(conditional, "worker-input.cs");
            Assert.Single(defined.Result.Entries);
            Assert.Empty(noDefines.Result.Entries);
            var oldWorker = await Client().AnalyzeAsync(new(conditional, "Fixture", "Root"));
            Assert.True(oldWorker.Success);
            Assert.Equal(noDefines.Result, oldWorker.Result);
            Assert.NotEqual(defined.Result, oldWorker.Result);
            AssertStopped(oldWorker.ProcessId!.Value);

            const string language = "public static class Fixture { public static void Root() { var value = \"\"\"text\"\"\"; throw null; } }";
            var oldLanguage = Native(language, "worker-input.cs", new(LanguageVersion.CSharp10));
            var newLanguage = Native(language, "worker-input.cs");
            Assert.Contains(oldLanguage.Errors, error => error.Contains("CS8936", StringComparison.Ordinal));
            Assert.Empty(newLanguage.Errors);

            const string nullable = "public static class Fixture { static string Echo(string value) => value; public static void Root() { throw null; } }";
            var disabled = Native(nullable, "worker-input.cs", nullable: NullableContextOptions.Disable);
            var enabled = Native(nullable, "worker-input.cs");
            Assert.NotEqual(disabled.ParameterAnnotation, enabled.ParameterAnnotation);
            Assert.Equal(disabled.Result, enabled.Result);

            const string dependency = "public sealed class ExternalException : System.Exception { }";
            var dependencyCompilation = CSharpCompilation.Create("ExactDependency", [CSharpSyntaxTree.ParseText(dependency)], Framework(), new(OutputKind.DynamicallyLinkedLibrary));
            using MemoryStream image = new();
            Assert.True(dependencyCompilation.Emit(image).Success);
            const string external = "public static class Fixture { public static void Root() { throw new ExternalException(); } }";
            var withReference = Native(external, "worker-input.cs", references: Framework().Append(MetadataReference.CreateFromImage(image.ToArray())).ToArray());
            Assert.Equal("ExternalException", withReference.Result.Entries.Single().ExceptionType.MetadataName);
            var withoutReference = await Client().AnalyzeAsync(new(external, "Fixture", "Root"));
            Assert.Equal(WorkerFailureCode.CompilationFailure, withoutReference.Failure!.WorkerFailure!.Code);
            Assert.Null(withoutReference.Result);
            AssertStopped(withoutReference.ProcessId!.Value);

            const string rootSource = "public static class Fixture { public static void Root() { Extra.Thrower(); } }";
            var multiple = Native(rootSource, "worker-input.cs", additional: "public static class Extra { public static void Thrower() { throw null; } }");
            Assert.Single(multiple.Result.Entries);
            var missingSource = await Client().AnalyzeAsync(new(rootSource, "Fixture", "Root"));
            Assert.Equal(WorkerFailureCode.CompilationFailure, missingSource.Failure!.WorkerFailure!.Code);
            Assert.Null(missingSource.Result);
            AssertStopped(missingSource.ProcessId!.Value);
            await WriteEvidence("inventory-differentials", new
            {
                Defines = new { Native = defined, EmptyDefines = noDefines, Worker = oldWorker.Result, Gap = true },
                Language = new { CSharp10 = oldLanguage.Errors, CSharp12 = newLanguage.Errors, Gap = "admissibility" },
                Nullable = new { Disabled = disabled, Enabled = enabled, AnnotationsDiffer = true, FlowEqual = true },
                References = new { Native = withReference, WorkerFailure = withoutReference.Failure, Gap = true },
                Sources = new { Native = multiple, WorkerFailure = missingSource.Failure, Gap = true },
                Deferred = true
            });
        }

        private sealed record NativeSnapshot(CanonicalExceptionFlowAnalysisResult Result, int GraphCount,
            object Findings, bool RepeatedFindingsEqual, string[] Errors, string? ParameterAnnotation);

        private static NativeSnapshot Native(string source, string path, CSharpParseOptions? parse = null,
            NullableContextOptions nullable = NullableContextOptions.Enable, MetadataReference[]? references = null, string? additional = null)
        {
            var tree = CSharpSyntaxTree.ParseText(source, parse ?? new(LanguageVersion.CSharp12), path);
            var trees = additional == null ? new[] { tree } : new[] { tree, CSharpSyntaxTree.ParseText(additional, tree.Options as CSharpParseOptions, "/extra.cs") };
            var compilation = CSharpCompilation.Create("HistoricalWorkerInput", trees, references ?? Framework(), new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullable));
            var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(new(context));
            var member = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var result = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member));
            var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
            int count = graph.Count;
            Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)));
            Assert.Equal(count, graph.Count);
            var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, path, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session);
            string json = JsonSerializer.Serialize(findings);
            Assert.Equal(json, JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, path, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session)));
            var echo = compilation.GetTypeByMetadataName("Fixture")!.GetMembers("Echo").OfType<IMethodSymbol>().SingleOrDefault();
            return new(result, count, findings, true, compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToArray(), echo?.Parameters[0].NullableAnnotation.ToString());
        }

        private static MetadataReference[] Framework()
        {
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            return ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        }

        private static async Task<JsonElement> HistoricalGraph(string source, string path)
        {
            string fixture = Path.Combine(Root(), "artifacts", "exception-flow-historical", "HistoricalPdbFixture", "bin", "Debug", "net8.0", "HistoricalPdbFixture.dll");
            ProcessStartInfo start = new("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add(fixture);
            start.ArgumentList.Add("--b11-graph");
            start.ArgumentList.Add(path);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.StandardInput.WriteAsync(source);
            process.StandardInput.Close();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
            Assert.Equal(0, process.ExitCode);
            Assert.True(string.IsNullOrWhiteSpace(await error), await error);
            using var json = JsonDocument.Parse(await output);
            return json.RootElement.Clone();
        }

        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(30));
        private static async Task WriteEvidence(string name, object value)
        {
            string path = Path.Combine(Root(), "artifacts", "p5o2b11", name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
        private static void AssertStopped(int pid)
        {
            Assert.NotEqual(Environment.ProcessId, pid);
            try { using var process = Process.GetProcessById(pid); Assert.True(process.HasExited); }
            catch (ArgumentException) { }
        }
        private static void AssertNoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
            assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
            || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
    }
}
