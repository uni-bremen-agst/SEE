using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
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
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Original validated external project DAGs, genuine worker dispatch and complete flow/graph differentials.</summary>
    public sealed class ExceptionFlowHistoricalSolutionScopeTests
    {
        private static readonly Lazy<WorkerAnalysisInput> RawGraph = new(() => Raw());

        [Theory]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(2, true)]
        public async Task Solution_OriginalProjectsFullFlowGraphFindingsOwnershipEdgesRepeatAndIsolation(int count, bool sourceException)
        {
            var original = await Original(count, sourceException);
            var projection = HistoricalWorkerPayloadProjection.Project(original);
            Assert.True(projection.Succeeded, projection.Failure?.Message);
            var payload = projection.Value!.Payload;
            var native = Native(payload, ExceptionAnalysisMode.SolutionTransitive);
            Assert.NotEmpty(native.Result.Entries); Assert.Empty(native.Result.Uncertainties); Assert.NotEmpty(native.Findings);
            Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == PathFor(count - 1));
            var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(original, new(new(Client())));
            var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(original, new(new(Client())));
            Assert.True(first.Succeeded, first.PayloadProjection.Failure?.Message ?? first.Dispatch?.Routing?.HistoricalFailure?.Message);
            Assert.True(second.Succeeded); Assert.Equal(native.Result, first.Result); Assert.Equal(first.Result, second.Result);
            Assert.Null(first.Dispatch!.Routing!.CurrentAnalysis);
            var call = first.Dispatch.Routing.HistoricalAnalysis!;
            Assert.Equal(WorkerProtocol.Provenance(payload), call.Provenance);
            var graph = await Probe(payload, "--b15-graph");
            Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32());
            Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32());
            Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray());
            Assert.Equal(native.Summaries, graph.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()).ToArray());
            Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean());
            Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())));
            Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(native.Ownership, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("ownership").GetRawText())));
            Assert.Equal(count - 1, payload.Context!.Solution!.Projects!.Sum(node => node.Dependencies!.Length));
            foreach (var node in payload.Context.Solution.Projects!)
            {
                foreach (var dependency in node.Dependencies!)
                { Assert.Single(node.Input!.Context!.References!.Where(reference => WorkerInputValidation.ReferenceMatchesProject(reference, dependency))); }
            }
            Stopped(call.ProcessId!.Value); Stopped(second.Dispatch!.Routing!.HistoricalAnalysis!.ProcessId!.Value); NoHistoricalLoaded();
            await Evidence("solution-" + count + "-" + sourceException, new
            {
                ProjectCount = count,
                SourceException = sourceException,
                Native = native,
                Historical = first.Result,
                RepeatedHistorical = second.Result,
                Graph = graph,
                Payload = WithoutImages(payload),
                call.Provenance,
                call.Identity,
                FirstProcess = call.ProcessId,
                SecondProcess = second.Dispatch.Routing.HistoricalAnalysis.ProcessId,
                OriginalReceipts = true,
                NoCurrentFallback = true,
                ProcessesStopped = true,
                HistoricalLoadedInMain = false,
                HistoricalFindingsImport = false
            });
        }

        [Theory]
        [InlineData("Direct")]
        [InlineData("ProjectTransitive")]
        public async Task LocalModes_DoNotWidenToSupportingProjects(string modeName)
        {
            var mode = Enum.Parse<ExceptionAnalysisMode>(modeName);
            var original = await Original(3, false);
            var payload = HistoricalWorkerPayloadProjection.Project(original).Value!.Payload;
            var solution = Native(payload, ExceptionAnalysisMode.SolutionTransitive);
            var local = Native(payload, mode);
            Assert.NotEqual(solution.Result, local.Result);
            Assert.Single(local.Ownership);
            Assert.DoesNotContain(local.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath != PathFor(0));
            var refusal = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(original with { AnalysisMode = mode }, new(new(Client())));
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, refusal.PayloadProjection.Failure!.Code);
            Assert.Null(refusal.Dispatch); Assert.Null(refusal.Result);
            var rootOnly = payload with { Context = payload.Context! with { Solution = null, AnalysisMode = mode == ExceptionAnalysisMode.Direct ? "direct" : "project-transitive" } };
            var worker = await Client().AnalyzeAsync(rootOnly);
            if (mode == ExceptionAnalysisMode.Direct) { Assert.True(worker.Success, worker.Failure?.Message); Assert.Equal(local.Result, worker.Result); }
            else { Assert.False(worker.Success); Assert.Equal(WorkerFailureCode.AnalysisFailure, worker.Failure!.WorkerFailure!.Code); Assert.Null(worker.Result); }
            Stopped(worker.ProcessId!.Value); NoHistoricalLoaded();
            await Evidence("local-" + mode, new { Mode = mode, Native = local, Solution = solution, WorkerResult = worker.Result, worker.Failure, GraphRefusedBeforeDispatch = true, NoScopeWidening = true, ProcessesStopped = true });
        }

        [Theory]
        [InlineData("missing-project")]
        [InlineData("unknown-target")]
        [InlineData("duplicate-identity")]
        [InlineData("conflicting-identity")]
        [InlineData("conflicting-ownership")]
        [InlineData("dangling-edge")]
        [InlineData("missing-reference")]
        [InlineData("wrong-mvid")]
        [InlineData("wrong-assembly-version")]
        [InlineData("missing-edges")]
        [InlineData("unreachable")]
        [InlineData("cycle")]
        [InlineData("nested-graph")]
        [InlineData("root-mismatch")]
        [InlineData("unknown-project")]
        [InlineData("missing-project-references")]
        [InlineData("declared-mode")]
        [InlineData("project-mode")]
        [InlineData("project-budget")]
        public async Task InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback(string scenario)
        {
            var input = RawGraph.Value; var graph = input.Context!.Solution!; var nodes = graph.Projects!.ToArray();
            var unknown = nodes[1].Identity! with { ModuleVersionId = Guid.NewGuid() };
            if (scenario == "missing-project") { nodes = [nodes[0]]; }
            if (scenario == "unknown-target") { graph = graph with { Target = unknown }; }
            if (scenario == "duplicate-identity") { nodes = [nodes[0], nodes[1], nodes[1]]; }
            if (scenario == "conflicting-identity") { nodes[1] = nodes[1] with { Identity = nodes[0].Identity }; }
            if (scenario == "conflicting-ownership") { var other = nodes[1].Input!; nodes[1] = nodes[1] with { Input = other with { LogicalPath = input.LogicalPath, Context = other.Context! with { Scope = other.Context.Scope! with { OrderedSourcePaths = [input.LogicalPath!], RootLogicalPath = input.LogicalPath } } } }; }
            if (scenario == "dangling-edge") { nodes[0] = nodes[0] with { Dependencies = [unknown] }; }
            if (scenario == "missing-reference") { nodes[0] = nodes[0] with { Input = nodes[0].Input! with { Context = nodes[0].Input!.Context! with { References = nodes[1].Input!.Context!.References } } }; input = nodes[0].Input!; }
            if (scenario == "wrong-mvid") { nodes[1] = nodes[1] with { Identity = unknown }; nodes[0] = nodes[0] with { Dependencies = [unknown] }; }
            if (scenario == "wrong-assembly-version") { var assembly = nodes[1].Identity!.Assembly!; var changed = new CanonicalAssemblyIdentity(assembly.Name, 1, assembly.VersionMinor, assembly.VersionBuild, assembly.VersionRevision, assembly.CultureName, assembly.PublicKeyOrToken, assembly.HasPublicKey, assembly.IsRetargetable, assembly.ContentType); nodes[1] = nodes[1] with { Identity = nodes[1].Identity! with { Assembly = changed }, Input = nodes[1].Input! with { Context = nodes[1].Input!.Context! with { TargetIdentity = changed } } }; nodes[0] = nodes[0] with { Dependencies = [nodes[1].Identity!] }; }
            if (scenario == "missing-edges") { nodes[0] = nodes[0] with { Dependencies = [] }; }
            if (scenario == "unreachable") { nodes[0] = nodes[0] with { Dependencies = [], Input = nodes[0].Input! with { Context = nodes[0].Input!.Context! with { References = nodes[1].Input!.Context!.References } } }; input = nodes[0].Input!; }
            if (scenario == "cycle") { var identity = nodes[0].Identity!; var reference = nodes[0].Input!.Context!.References!.Last() with { Identity = identity.Assembly, ModuleName = identity.ModuleName, ModuleVersionId = identity.ModuleVersionId }; nodes[1] = nodes[1] with { Dependencies = [identity], Input = nodes[1].Input! with { Context = nodes[1].Input!.Context! with { References = [reference] } } }; }
            if (scenario == "nested-graph") { nodes[1] = nodes[1] with { Input = nodes[1].Input! with { Context = nodes[1].Input!.Context! with { Solution = graph } } }; }
            if (scenario == "root-mismatch") { input = input with { Source = input.Source + " " }; }
            if (scenario == "unknown-project") { nodes[1] = nodes[1] with { Identity = null }; }
            if (scenario == "missing-project-references") { nodes[1] = nodes[1] with { Input = nodes[1].Input! with { Context = nodes[1].Input!.Context! with { References = null } } }; }
            if (scenario == "declared-mode") { input = input with { Context = input.Context! with { AnalysisMode = "project-transitive-declared-exceptions" } }; }
            if (scenario == "project-mode") { input = input with { Context = input.Context! with { AnalysisMode = "project-transitive" } }; }
            if (scenario == "project-budget") { nodes = Enumerable.Repeat(nodes[1], 5).ToArray(); }
            input = input with { Context = input.Context! with { Solution = graph with { Projects = nodes } } };
            var result = await Client().AnalyzeAsync(input);
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code); Assert.Null(result.ProcessId); Assert.Null(result.Result);
            await Evidence("negative-" + scenario, new { Scenario = scenario, result.Failure, NoWorker = true, NoFallback = true });
        }

        [Fact]
        public async Task OriginalGraph_RejectsUnvalidatedCopiedIncompleteAndNestedProjects()
        {
            var original = await Original(2, false); var other = original.SupportingProjects[0];
            var copied = new ExternalCSharpSyntaxTreeSet(other.SourceSet!.Trees, other.SourceSet.SourceMaterials);
            var provenance = other.Provenance;
            var unvalidated = new ExternalCompilationProvenanceDescriptor(provenance.DebugDirectory, provenance.PortablePdb, provenance.CompilationOptions, provenance.MetadataReferences);
            foreach (var invalid in new[] { other with { SourceSet = copied }, other with { ReferenceSet = null }, other with { SourceSet = null }, other with { SupportingProjects = [other] }, other with { Provenance = unvalidated } })
            {
                var refusal = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(original with { SupportingProjects = [invalid] }, new(new(Client())));
                Assert.False(refusal.Succeeded); Assert.NotNull(refusal.PayloadProjection.Failure); Assert.Null(refusal.Dispatch); Assert.Null(refusal.Result);
            }
            var empty = HistoricalWorkerPayloadProjection.Project(original with { SupportingProjects = [] });
            Assert.False(empty.Succeeded);
            await Evidence("receipts", new { CopiedRejected = true, UnvalidatedProvenanceRejected = true, MissingReferencesRejected = true, MissingSourcesRejected = true, NestedRejected = true, EmptyRejected = true, NoSecondValidator = true });
        }

        [Fact]
        public async Task MissingReachableSourceProject_DoesNotReturnACompleteSmallerScope()
        {
            var original = await Original(3, false);
            var full = HistoricalWorkerPayloadProjection.Project(original).Value!.Payload;
            Assert.NotEmpty(Native(full, ExceptionAnalysisMode.SolutionTransitive).Result.Entries);
            // C's validated PE remains in B's exact references, but C's source-project receipt is absent.
            // Binary references without supplied source are not silently turned into source projects.
            var missing = original with { SupportingProjects = [original.SupportingProjects[0]] };
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(missing, new(new(Client())));
            Assert.True(result.PayloadProjection.Succeeded); Assert.False(result.Succeeded); Assert.Null(result.Result);
            var routing = result.Dispatch!.Routing!;
            Assert.Null(routing.CurrentAnalysis);
            Assert.Equal(WorkerFailureCode.AnalysisFailure, routing.HistoricalFailure!.WorkerFailure!.Code);
            Stopped(routing.HistoricalAnalysis!.ProcessId!.Value); NoHistoricalLoaded();
            await Evidence("missing-reachable-source", new { routing.HistoricalFailure, ActualReachableSourceMissing = true, NoPartialResult = true, NoLocalOrCurrentFallback = true, ProcessesStopped = true });
        }

        [Fact]
        public async Task SupportingProject_UnsupportedCompilerContextAndForgedWorldAreFailClosed()
        {
            var input = RawGraph.Value; var graph = input.Context!.Solution!; var nodes = graph.Projects!.ToArray();
            var supporting = nodes[1].Input!;
            nodes[1] = nodes[1] with { Input = supporting with { Context = supporting.Context! with { LanguageVersion = "999", ParseContext = supporting.Context.ParseContext! with { LanguageVersion = "999" } } } };
            var unavailable = input with { Context = input.Context with { Solution = graph with { Projects = nodes } } };
            Assert.NotEqual(WorkerProtocol.ReferenceWorldHash(input), WorkerProtocol.ReferenceWorldHash(unavailable));
            var result = await Client().AnalyzeAsync(unavailable);
            Assert.Equal(WorkerFailureCode.UnsupportedContext, result.Failure!.WorkerFailure!.Code); Assert.Null(result.Result); Stopped(result.ProcessId!.Value);
            var real = await Client().AnalyzeAsync(input); Assert.True(real.Success, real.Failure?.Message);
            var response = new WorkerResponse(2, "analyze", true, real.Identity, real.Result, Provenance: WorkerProtocol.Provenance(unavailable));
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, HistoricalWorkerResponseValidation.Validate(JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions), "", 0, input).Failure!.Code);
            response = response with { Provenance = WorkerProtocol.Provenance(input) };
            string forged = JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions).Replace(PathFor(1), "/foreign/Helper.cs", StringComparison.Ordinal);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, HistoricalWorkerResponseValidation.Validate(forged, "", 0, input).Failure!.Code);
            Stopped(real.ProcessId!.Value); NoHistoricalLoaded();
            await Evidence("compiler-response", new { result.Failure, ActualCompilerRejection = true, ForgedGraphWorldRejected = true, ForeignPathRejected = true, NoFallback = true, ProcessesStopped = true });
        }

        [Fact]
        public void NativeWorkspace_RejectsCircularProjectReferences()
        {
            using var workspace = new AdhocWorkspace();
            var a = ProjectId.CreateNewId(); var b = ProjectId.CreateNewId();
            var solution = workspace.CurrentSolution.AddProject(a, "A", "A", LanguageNames.CSharp).AddProject(b, "B", "B", LanguageNames.CSharp).AddProjectReference(a, new(b));
            Assert.Throws<InvalidOperationException>(() => solution.AddProjectReference(b, new(a)));
        }

        private sealed record Snapshot(CanonicalExceptionFlowAnalysisResult Result, int GraphCount, string[] Summaries, object[] Findings, bool RepeatedFindingsEqual, object[] Ownership);
        private static Snapshot Native(WorkerAnalysisInput input, ExceptionAnalysisMode mode)
        {
            var projects = input.Context!.Solution!.Projects!.Select(node => Compile(node.Input!)).OrderBy(project => project.AssemblyName, StringComparer.Ordinal).ToArray();
            var target = projects.Single(project => project.AssemblyName == input.Context.AssemblyName);
            var tree = target.SyntaxTrees.Single(source => source.FilePath == input.LogicalPath);
            var semantic = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, target);
            if (mode == ExceptionAnalysisMode.SolutionTransitive) { foreach (var other in projects.Where(project => !ReferenceEquals(project, target))) { semantic.RegisterSupportingSource(other); } }
            var environment = new ExceptionFlowSemanticEnvironment(semantic); var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(environment);
            var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            CanonicalExceptionFlowAnalysisResult Analyze() => RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(mode == ExceptionAnalysisMode.Direct
                ? ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions(root, environment) : session.Analyze(root));
            var result = Analyze(); Assert.Equal(result, Analyze());
            var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
            var entries = (Dictionary<ExceptionFlowCallableKey, ExceptionFlowSummary>)typeof(ExceptionFlowSummaryGraph).GetField("summaries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(graph)!;
            var summaries = entries.Select(entry => { Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary)); return JsonSerializer.Serialize(summary, WorkerProtocol.JsonOptions); }).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var options = new XmlDocOptions { ExceptionAnalysisMode = mode };
            var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, target.GetSemanticModel(tree), semantic, options, session);
            Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, target.GetSemanticModel(tree), semantic, options, session)));
            var ownership = environment.GetAnalysisScopes().SelectMany(scope => scope.Compilation.SyntaxTrees.Select(source =>
            {
                Assert.True(environment.TryGetSemanticModel(source, out var first));
                Assert.True(environment.TryGetSemanticModel(source, out var second));
                Assert.Same(first, second); Assert.Same(scope.Compilation, first.Compilation);
                return (object)new { LogicalPath = source.FilePath, AssemblyName = scope.Compilation.AssemblyName, Owned = true, StableModel = true };
            })).ToArray();
            return new(result, graph.Count, summaries, findings.Cast<object>().ToArray(), true, ownership);
        }

        private static CSharpCompilation Compile(WorkerAnalysisInput input)
        {
            var context = input.Context!; Assert.True(LanguageVersionFacts.TryParse(context.ParseContext!.LanguageVersion, out var language));
            var tree = CSharpSyntaxTree.ParseText(input.Source!, new CSharpParseOptions(language, DocumentationMode.Parse, SourceCodeKind.Regular, context.ParseContext.PreprocessorSymbols), input.LogicalPath!);
            var compilation = CSharpCompilation.Create(context.AssemblyName, [tree], context.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image,
                new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases: [.. reference.Aliases!]))), new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)); return compilation;
        }
        private static string PathFor(int index) => "/solution/p" + index + "/Source.cs";
        private static WorkerAnalysisInput Local(int index, int count, bool sourceException, byte[][] images)
        {
            string leaf = sourceException ? "public class LocalException : System.Exception {}" : "";
            string body = index + 1 < count ? "P" + (index + 1) + ".Fixture.Root();" : "throw Value;";
            string source = "namespace P" + index + " { " + (index + 1 == count ? leaf : "") + " public static class Fixture { public static "
                + (sourceException && index + 1 == count ? "LocalException" : "System.InvalidOperationException") + " Value = null!;\r\n/// <summary>Controlled cross-project root.</summary>\r\npublic static void Root() { " + body + " } } }";
            var payload = (WorkerAnalysisInput)B12("Payload", source, images, "Original.Solution.P" + index)!;
            var parse = new WorkerParseContext("12", [], "parse", "regular");
            return payload with
            {
                LogicalPath = PathFor(index),
                TypeMetadataName = "P" + index + ".Fixture",
                Context = payload.Context! with
                { ParseProfile = "validated-parse-v1", ParseContext = parse, Scope = new("single-project-v1", payload.Context.AssemblyName, [PathFor(index)], PathFor(index)), AdditionalSources = [] }
            };
        }
        private static WorkerAnalysisInput Raw()
        {
            var framework = (byte[][])B12("FrameworkImages")!;
            var b = Local(1, 2, false, framework); using var pe = new MemoryStream(); Assert.True(Compile(b).Emit(pe).Success);
            var a = Local(0, 2, false, [.. framework, pe.ToArray()]);
            var bReference = a.Context!.References!.Last();
            var bIdentity = new WorkerProjectIdentity(b.Context!.TargetIdentity, b.Context.TargetModuleName, bReference.ModuleVersionId);
            var aIdentity = new WorkerProjectIdentity(a.Context.TargetIdentity, a.Context.TargetModuleName, Guid.NewGuid());
            return a with { Context = a.Context with { Solution = new(aIdentity, [new(aIdentity, a, [bIdentity]), new(bIdentity, b, [])]) } };
        }
        private static async Task<HistoricalWorkerPayloadProjectionInput> Original(int count, bool sourceException)
        {
            var framework = (byte[][])B12("FrameworkImages")!; byte[]? dependency = null;
            var inputs = new List<HistoricalWorkerPayloadProjectionInput>();
            for (int index = count - 1; index >= 0; index--)
            {
                var payload = Local(index, count, sourceException, dependency == null ? framework : [.. framework, dependency]);
                string output = Path.Combine(Root(), "artifacts", "p5o2b15", "emit-" + Guid.NewGuid().ToString("N"));
                await Probe(payload, "--b14-emit", output);
                dependency = File.ReadAllBytes(Path.Combine(output, "input.dll"));
                inputs.Insert(0, ReadOriginal(payload, dependency, File.ReadAllBytes(Path.Combine(output, "input.pdb"))));
            }
            return inputs[0] with { SupportingProjects = inputs.Skip(1).ToImmutableArray() };
        }
        private static HistoricalWorkerPayloadProjectionInput ReadOriginal(WorkerAnalysisInput payload, byte[] binary, byte[] pdb)
        {
            var reference = MetadataReference.CreateFromImage(binary);
            var consumer = CSharpCompilation.Create("PayloadConsumer", references: ((byte[][])B12("FrameworkImages")!).Select(image => MetadataReference.CreateFromImage(image)).Append(reference), options: new(OutputKind.DynamicallyLinkedLibrary));
            var assembly = Assert.IsAssignableFrom<IAssemblySymbol>(consumer.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(consumer, assembly, out var expected));
            using var pe = new MemoryStream(binary, writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(expected, pe, out var debug));
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(debug, [.. pdb], out var provenance));
            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(provenance, out var configuration));
            var document = provenance.PortablePdb.Documents.Single();
            using var bytes = new MemoryStream([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(payload.Source!)], writable: false);
            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(document, bytes, out var material));
            Assert.True(ExternalCSharpSyntaxTreeFactory.TryCreate(material, configuration, out var tree));
            Assert.True(ExternalCSharpSyntaxTreeSetFactory.TryCreate(configuration, [document], [tree], out var set));
            var materials = new List<ValidatedExternalMetadataReferenceMaterial>();
            var references = provenance.MetadataReferences!.References;
            for (int index = 0; index < references.Length; index++)
            {
                using var image = new MemoryStream(payload.Context!.References![index].Image, writable: false);
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(references[index], image, out var metadata)); materials.Add(metadata);
            }
            Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(provenance, materials, out var referenceSet));
            var root = assembly.GetTypeByMetadataName(payload.TypeMetadataName!)!.GetMembers("Root").OfType<IMethodSymbol>().Single();
            return new(provenance, tree, root, ExceptionAnalysisMode.SolutionTransitive, referenceSet, configuration, set);
        }
        private static WorkerAnalysisInput WithoutImages(WorkerAnalysisInput input)
        {
            var context = input.Context! with { References = input.Context!.References!.Select(reference => reference with { Image = [] }).ToArray() };
            return input with { Context = context with { Solution = context.Solution! with { Projects = context.Solution!.Projects!.Select(node => node with { Input = WithoutLocalImages(node.Input!) }).ToArray() } } };
        }
        private static WorkerAnalysisInput WithoutLocalImages(WorkerAnalysisInput input) => input with { Context = input.Context! with { References = input.Context!.References!.Select(reference => reference with { Image = [] }).ToArray() } };
        private static object? B12(string method, params object?[] arguments) => typeof(ExceptionFlowHistoricalReferenceIdentityTests).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments);
        private static Task<JsonElement> Probe(WorkerAnalysisInput input, string operation, string? output = null) => (Task<JsonElement>)B12("Probe", input, operation, output)!;
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static void Stopped(int pid) { try { using var process = Process.GetProcessById(pid); Assert.True(process.HasExited); } catch (ArgumentException) { } }
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        {
            string output = Path.Combine(Root(), "artifacts", "p5o2b15", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
