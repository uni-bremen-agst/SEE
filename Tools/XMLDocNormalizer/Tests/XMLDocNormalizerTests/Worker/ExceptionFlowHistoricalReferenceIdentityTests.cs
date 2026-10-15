using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
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
    /// <summary>Exact ordered reference/identity handoff through the genuine isolated Historical engine.</summary>
    public sealed class ExceptionFlowHistoricalReferenceIdentityTests
    {
        private const string PathName = "/original/reference-root.cs";
        private const string ExternalSource = "public static class Fixture { public static ExternalException Value = null!; public static void Root() { throw Value; } }";
        private const string LocalSource = "[assembly:System.Reflection.AssemblyVersion(\"3.4.5.6\")] public sealed class LocalException : System.Exception {} public static class Fixture { public static LocalException Value = null!; public static void Root() { Thrower(); } static void Thrower() { throw Value; } }";

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task RealExternalMetadata_FullCanonicalIdentitySourcesUncertaintyGraphRepeatAndIsolation(int version)
        {
            byte[][] images = [.. FrameworkImages(), Dependency(version)];
            var payload = Payload(ExternalSource, images, "OriginalUnsignedTarget");
            var native = Native(payload);
            Assert.Empty(native.Errors);
            Assert.Contains(native.Result.Entries, entry => entry.ExceptionType.MetadataName == "ExternalException"
                && entry.ExceptionType.Assembly!.VersionMajor == version);
            Assert.Empty(native.Result.Uncertainties);
            var before = await Client().AnalyzeAsync(new(ExternalSource, "Fixture", "Root", LogicalPath: PathName));
            Assert.Equal(WorkerFailureCode.CompilationFailure, before.Failure!.WorkerFailure!.Code);
            Assert.Null(before.Result);
            Stopped(before);

            // Genuine Historical PE/PDB -> unchanged P4/P5A/P5G/P5H/P5I -> original P5C/P5E -> production dispatch.
            var input = await ValidatedInput(payload);
            var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.True(first.Succeeded, first.Dispatch?.Routing?.HistoricalFailure?.Message ?? first.PayloadProjection.Failure?.Message);
            Assert.True(second.Succeeded);
            Assert.Equal(native.Result, first.Result);
            Assert.Equal(first.Result, second.Result);
            Assert.Null(first.Dispatch!.Routing!.CurrentAnalysis);
            Assert.Equal(WorkerProtocol.ExactReferenceProfile, first.PayloadProjection.Value!.Payload.Context!.ReferenceProfile);
            Assert.Equal("OriginalUnsignedTarget", first.PayloadProjection.Value.Payload.Context.AssemblyName);
            Assert.Equal(WorkerProtocol.ReferenceWorldHash(first.PayloadProjection.Value.Payload), first.Dispatch.Routing.HistoricalAnalysis!.Provenance!.ReferenceWorldSha256);
            var graph = await Probe(payload, "--b12-graph");
            AssertGraph(native, first.Result!, graph);
            Stopped(first.Dispatch.Routing.HistoricalAnalysis);
            Stopped(second.Dispatch!.Routing!.HistoricalAnalysis!);
            NoHistoricalLoaded();
            await Evidence("external-v" + version, new
            {
                Native = native,
                Historical = first.Result,
                RepeatedHistorical = second.Result,
                Graph = graph,
                Provenance = first.Dispatch.Routing.HistoricalAnalysis.Provenance,
                Identity = first.Dispatch.Routing.HistoricalAnalysis.Identity,
                OriginalP5CAndP5E = true,
                AutomaticProjection = true,
                ProcessesStopped = true,
                NoCurrentFallback = true,
                HistoricalLoadedInMain = false,
                Before = before.Failure,
                FirstProcess = first.Dispatch.Routing.HistoricalAnalysis.ProcessId,
                FirstProcessExit = first.Dispatch.Routing.HistoricalAnalysis.ProcessExit,
                SecondProcess = second.Dispatch.Routing.HistoricalAnalysis!.ProcessId,
                SecondProcessExit = second.Dispatch.Routing.HistoricalAnalysis!.ProcessExit,
                ReferenceWorld = payload.Context!.References!.Select(reference => reference with { Image = [] }).ToArray()
            });
        }

        [Fact]
        public async Task OriginalTargetNameVersionAndModule_FullIdentityParityWithoutCompilerOptionExpansion()
        {
            var input = Payload(LocalSource, FrameworkImages(), "Original.Local.Target");
            var native = Native(input);
            var first = await Client().AnalyzeAsync(input);
            var second = await Client().AnalyzeAsync(input);
            Assert.True(first.Success, first.Failure?.Message);
            Assert.True(second.Success);
            Assert.Equal(native.Result, first.Result);
            Assert.Equal(first.Result, second.Result);
            var local = first.Result!.Entries.Single(entry => entry.ExceptionType.MetadataName == "LocalException");
            Assert.Equal("Original.Local.Target", local.ExceptionType.Assembly!.Name);
            Assert.Equal(3, local.ExceptionType.Assembly.VersionMajor);
            Assert.Equal("Original.Local.Target.dll", local.ExceptionType.Module!.Name);
            var graph = await Probe(input, "--b12-graph");
            AssertGraph(native, first.Result, graph);
            Stopped(first); Stopped(second); NoHistoricalLoaded();
            await Evidence("target-identity", new { Native = native, Historical = first.Result, RepeatedHistorical = second.Result, Graph = graph, first.Provenance, ProcessesStopped = true });
        }

        [Fact]
        public async Task SameLookupNameDifferentVersions_BindsPeIdentityNotPath()
        {
            var first = Payload(ExternalSource, [.. FrameworkImages(), Dependency(1)], "Target");
            var second = Payload(ExternalSource, [.. FrameworkImages(), Dependency(2)], "Target");
            Assert.Equal(first.Context!.References!.Last().LogicalName, second.Context!.References!.Last().LogicalName);
            Assert.NotEqual(first.Context.References!.Last().Identity, second.Context.References!.Last().Identity);
            var left = await Client().AnalyzeAsync(first); var right = await Client().AnalyzeAsync(second);
            Assert.True(left.Success, left.Failure?.Message); Assert.True(right.Success, right.Failure?.Message);
            Assert.NotEqual(left.Result, right.Result);
            Assert.Equal(Native(first).Result, left.Result); Assert.Equal(Native(second).Result, right.Result);
            Stopped(left); Stopped(right);
            var renamed = first with { Context = first.Context with { References = first.Context.References!.Select(reference => reference with { LogicalName = "same/nonexistent/path.dll" }).ToArray() } };
            var again = await Client().AnalyzeAsync(renamed);
            Assert.True(again.Success); Assert.Equal(left.Result, again.Result); Assert.Equal(left.Provenance, again.Provenance);
            Stopped(again);
        }

        [Fact]
        public async Task AliasesAndOrder_ArePreservedNotSortedOrDiscarded()
        {
            const string source = "extern alias dep; public static class Fixture { public static dep::ExternalException Value = null!; public static void Root() { throw Value; } }";
            var original = Payload(source, [.. FrameworkImages(), Dependency(1)], "AliasedTarget");
            var refs = original.Context!.References!;
            refs[^1] = refs[^1] with { Aliases = ["dep"] };
            var native = Native(original);
            var first = await Client().AnalyzeAsync(original);
            Assert.True(first.Success, first.Failure?.Message); Assert.Equal(native.Result, first.Result);
            var reordered = original with { Context = original.Context with { References = refs.Reverse().ToArray() } };
            var second = await Client().AnalyzeAsync(reordered);
            Assert.True(second.Success); Assert.Equal(Native(reordered).Result, second.Result); Assert.Equal(first.Result, second.Result);
            Assert.NotEqual(first.Provenance!.ReferenceWorldSha256, second.Provenance!.ReferenceWorldSha256);
            Stopped(first); Stopped(second);
            await Evidence("aliases-order", new
            {
                Native = native,
                Historical = first.Result,
                ReorderedHistorical = second.Result,
                OriginalProvenance = first.Provenance,
                ReorderedProvenance = second.Provenance,
                OrderRetained = true,
                AliasRetained = true
            });
        }

        [Fact]
        public async Task MissingExternalReference_HasSameCompilerErrorAndNoSuccessfulOrFallbackResult()
        {
            var input = Payload(ExternalSource, FrameworkImages(), "Target");
            var native = Native(input);
            Assert.Contains(native.Errors, error => error.StartsWith("CS0246", StringComparison.Ordinal));
            var result = await Client().AnalyzeAsync(input);
            Assert.False(result.Success); Assert.Null(result.Result);
            Assert.Equal(WorkerFailureCode.CompilationFailure, result.Failure!.WorkerFailure!.Code);
            Assert.Contains(result.Failure.WorkerFailure.Details, error => error.StartsWith("CS0246", StringComparison.Ordinal));
            Stopped(result);
            await Evidence("missing-reference", new { CurrentErrors = native.Errors, HistoricalFailure = result.Failure, ExistingCompletenessContract = true });
        }

        [Theory]
        [InlineData("identity-version")]
        [InlineData("identity-key")]
        [InlineData("module-mvid")]
        [InlineData("module-name")]
        [InlineData("target-culture")]
        [InlineData("target-version")]
        [InlineData("target-module")]
        public async Task ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata(string scenario)
        {
            var input = Payload(ExternalSource, [.. FrameworkImages(), Dependency(1)], "Target");
            var context = input.Context!;
            var reference = context.References![^1];
            if (scenario == "identity-version") { reference = reference with { Identity = CopyIdentity(reference.Identity!, version: 8) }; }
            if (scenario == "identity-key") { reference = reference with { Identity = CopyIdentity(reference.Identity!, token: "0102030405060708") }; }
            if (scenario == "module-mvid") { reference = reference with { ModuleVersionId = Guid.NewGuid() }; }
            if (scenario == "module-name") { reference = reference with { ModuleName = "wrong.dll" }; }
            context.References[^1] = reference;
            if (scenario == "target-culture") { context = context with { TargetIdentity = CopyIdentity(context.TargetIdentity!, culture: "en-US") }; }
            if (scenario == "target-version") { context = context with { TargetIdentity = CopyIdentity(context.TargetIdentity!, version: 9) }; }
            if (scenario == "target-module") { context = context with { TargetModuleName = "wrong.dll" }; }
            var result = await Client().AnalyzeAsync(input with { Context = context });
            Assert.Equal(WorkerFailureCode.CompilationFailure, result.Failure!.WorkerFailure!.Code);
            Assert.Null(result.Result); Stopped(result);
        }

        [Theory]
        [InlineData("hash")]
        [InlineData("duplicate")]
        [InlineData("conflicting-version")]
        [InlineData("module")]
        [InlineData("embed")]
        [InlineData("signed-target")]
        [InlineData("missing-images")]
        [InlineData("unknown-profile")]
        [InlineData("alias-duplicate")]
        [InlineData("count-budget")]
        [InlineData("byte-budget")]
        [InlineData("target-name")]
        public async Task UnsupportedReferenceStates_FailBeforeProcessStart(string scenario)
        {
            var input = Payload(ExternalSource, [.. FrameworkImages(), Dependency(1)], "Target");
            var context = input.Context!;
            var refs = context.References!;
            if (scenario == "hash") { refs[^1] = refs[^1] with { Sha256 = new string('A', 64) }; }
            if (scenario == "duplicate") { context = context with { References = [.. refs, refs[^1]] }; }
            if (scenario == "conflicting-version") { context = context with { References = [.. refs, Description(Dependency(2), "another-path.dll")] }; }
            if (scenario == "module") { refs[^1] = refs[^1] with { Kind = "module" }; }
            if (scenario == "embed") { refs[^1] = refs[^1] with { EmbedInteropTypes = true }; }
            if (scenario == "signed-target") { context = context with { TargetIdentity = CopyIdentity(context.TargetIdentity!, token: "0102030405060708") }; }
            if (scenario == "missing-images") { context = context with { References = null }; }
            if (scenario == "unknown-profile") { context = context with { ReferenceProfile = "guess" }; }
            if (scenario == "alias-duplicate") { refs[^1] = refs[^1] with { Aliases = ["dep", "dep"] }; }
            if (scenario == "count-budget") { context = context with { References = Enumerable.Repeat(refs[^1], WorkerProtocol.MaximumReferenceCount + 1).ToArray() }; }
            if (scenario == "byte-budget") { refs[0] = refs[0] with { Image = new byte[WorkerProtocol.MaximumReferenceBytes + 1] }; }
            if (scenario == "target-name") { context = context with { AssemblyName = "ContradictsIdentity" }; }
            var result = await Client().AnalyzeAsync(input with { Context = context });
            Assert.Equal(HistoricalWorkerClientFailureCode.UnsupportedInput, result.Failure!.Code);
            Assert.Null(result.ProcessId); Assert.Null(result.Result);
        }

        [Fact]
        public async Task CopiedSetWrongProvenanceAndForgedMaterial_CannotMintOriginalReceipt()
        {
            var payload = Payload("public static class Fixture { public static void Root() { throw null; } }", FrameworkImages(), "Target");
            var input = await ValidatedInput(payload);
            var copied = new ExternalMetadataReferenceSet(input.ReferenceSet!.References);
            Assert.False(HistoricalWorkerPayloadProjection.Project(input with { ReferenceSet = copied }).Succeeded);
            Assert.True(ExternalMetadataReferenceSetFactory.TryGetValidatedMaterials(input.ReferenceSet, input.Provenance, out var originals));
            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(input.Provenance, originals.Take(originals.Length - 1).ToArray(), out _));
            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(input.Provenance, originals.Append(originals[0]).ToArray(), out _));
            var forged = originals.Select(material => new ValidatedExternalMetadataReferenceMaterial(material.Candidate, material.Image)).ToArray();
            Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(input.Provenance, forged, out var forgedSet));
            Assert.False(HistoricalWorkerPayloadProjection.Project(input with { ReferenceSet = forgedSet }).Succeeded);
            var another = (HistoricalWorkerPayloadProjectionInput)typeof(ExceptionFlowHistoricalPayloadProjectionTests).GetMethod("Historical", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
            Assert.False(HistoricalWorkerPayloadProjection.Project(another with { ReferenceSet = input.ReferenceSet }).Succeeded);
        }

        [Fact]
        public async Task ReturnedReferenceWorldHash_IsBoundAndUnknownDuplicateJsonFailsClosed()
        {
            var input = Payload("public static class Fixture { public static void Root() { throw null; } }", FrameworkImages(), "Target");
            var real = await Client().AnalyzeAsync(input);
            Assert.True(real.Success);
            var response = new WorkerResponse(WorkerProtocol.Version, "analyze", true, real.Identity, real.Result, Provenance: real.Provenance! with { ReferenceWorldSha256 = new string('F', 64) });
            var rejected = HistoricalWorkerResponseValidation.Validate(JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions), "", 0, input);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, rejected.Failure!.Code); Assert.Null(rejected.Result);
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkerReferenceImage>("{\"unexpected\":true}", WorkerProtocol.JsonOptions));
            using var duplicate = JsonDocument.Parse("{\"payload\":{\"context\":{\"references\":[],\"references\":[]}}}");
            Assert.Throws<JsonException>(() => WorkerProtocol.RejectDuplicateProperties(duplicate.RootElement));
            Stopped(real); NoHistoricalLoaded();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReturnedExceptionAssemblyOrMvid_CannotContradictRequestedReferenceWorld(bool mvid)
        {
            var input = Payload(ExternalSource, [.. FrameworkImages(), Dependency(1)], "Target");
            var real = await Client().AnalyzeAsync(input);
            Assert.True(real.Success, real.Failure?.Message);
            var response = new WorkerResponse(WorkerProtocol.Version, "analyze", true, real.Identity, real.Result, Provenance: real.Provenance);
            var node = JsonNode.Parse(JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions))!;
            var entry = node["result"]!["entries"]!.AsArray().Single(item => item!["exceptionType"]!["metadataName"]!.GetValue<string>() == "ExternalException")!;
            if (mvid) { entry["exceptionType"]!["module"]!["moduleVersionId"] = Guid.NewGuid().ToString(); }
            else
            {
                entry["exceptionType"]!["assembly"]!["versionMajor"] = 9;
                entry["exceptionType"]!["module"]!["assembly"]!["versionMajor"] = 9;
            }
            var rejected = HistoricalWorkerResponseValidation.Validate(node.ToJsonString(), "", 0, input);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, rejected.Failure!.Code); Assert.Null(rejected.Result);
            Stopped(real);
        }

        private sealed record Snapshot(CanonicalExceptionFlowAnalysisResult Result, int GraphCount, string[] GraphSummaries, object Findings, bool RepeatedFindingsEqual, string[] Errors);
        private static Snapshot Native(WorkerAnalysisInput input)
        {
            var tree = CSharpSyntaxTree.ParseText(input.Source!, new CSharpParseOptions(LanguageVersion.CSharp12), input.LogicalPath!);
            var refs = input.Context!.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image,
                new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases: [.. reference.Aliases!]))).ToArray();
            var compilation = CSharpCompilation.Create(input.Context.AssemblyName, [tree], refs, new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(new(context));
            var member = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var result = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member));
            var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
            int count = graph.Count;
            string[] summaries = Summaries(graph);
            Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member))); Assert.Equal(count, graph.Count);
            Assert.Equal(summaries, Summaries(graph));
            var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session);
            Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session)));
            return new(result, count, summaries, findings, true, compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).ToArray());
        }

        private static string[] Summaries(ExceptionFlowSummaryGraph graph)
        {
            var entries = (Dictionary<ExceptionFlowCallableKey, ExceptionFlowSummary>)typeof(ExceptionFlowSummaryGraph).GetField("summaries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(graph)!;
            return entries.Select(entry =>
            {
                Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary));
                return JsonSerializer.Serialize(summary, WorkerProtocol.JsonOptions);
            }).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static WorkerAnalysisInput Payload(string source, byte[][] images, string assemblyName)
        {
            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), PathName);
            var references = images.Select(image => MetadataReference.CreateFromImage(image)).ToArray();
            var compilation = CSharpCompilation.Create(assemblyName, [tree], references, new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            return new(source, "Fixture", "Root", new(ReferenceProfile: WorkerProtocol.ExactReferenceProfile,
                AssemblyName: assemblyName, References: images.Select((image, index) => Description(image, "lookup-" + index + ".dll")).ToArray(),
                TargetIdentity: RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(compilation.Assembly), TargetModuleName: compilation.SourceModule.Name), PathName);
        }

        private static WorkerReferenceImage Description(byte[] image, string logicalName)
        {
            var reference = MetadataReference.CreateFromImage(image);
            var consumer = CSharpCompilation.Create("Consumer", references: [reference]);
            var assembly = Assert.IsAssignableFrom<IAssemblySymbol>(consumer.GetAssemblyOrModuleSymbol(reference));
            var module = Assert.Single(assembly.Modules);
            return new(logicalName, image, Convert.ToHexString(SHA256.HashData(image)), RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(assembly),
                module.Name, module.GetMetadata()!.GetModuleVersionId(), []);
        }

        private static CanonicalAssemblyIdentity CopyIdentity(CanonicalAssemblyIdentity identity, string? name = null, int? version = null, string? token = null, string? culture = null)
            => new(name ?? identity.Name, version ?? identity.VersionMajor, identity.VersionMinor, identity.VersionBuild, identity.VersionRevision,
                culture ?? identity.CultureName, token ?? identity.PublicKeyOrToken, identity.HasPublicKey, identity.IsRetargetable, identity.ContentType);

        private static byte[] Dependency(int version)
        {
            var tree = CSharpSyntaxTree.ParseText("[assembly:System.Reflection.AssemblyVersion(\"" + version + ".2.3.4\")] public sealed class ExternalException : System.Exception {}", new CSharpParseOptions(LanguageVersion.CSharp12));
            var compilation = CSharpCompilation.Create("SameDependency", [tree], FrameworkImages().Select(image => MetadataReference.CreateFromImage(image)), new(OutputKind.DynamicallyLinkedLibrary, deterministic: true));
            using var stream = new MemoryStream(); Assert.True(compilation.Emit(stream).Success); return stream.ToArray();
        }
        private static byte[][] FrameworkImages()
        {
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            return ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .OrderBy(Path.GetFileName, StringComparer.Ordinal).Select(File.ReadAllBytes).ToArray();
        }

        private static async Task<HistoricalWorkerPayloadProjectionInput> ValidatedInput(WorkerAnalysisInput payload)
        {
            string output = Path.Combine(Root(), "artifacts", "p5o2b12", "emission-" + Guid.NewGuid().ToString("N"));
            await Probe(payload, "--b12-emit", output);
            var input = (HistoricalWorkerPayloadProjectionInput)typeof(ExceptionFlowHistoricalPayloadProjectionTests).GetMethod("Read", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [File.ReadAllBytes(Path.Combine(output, "input.dll")), File.ReadAllBytes(Path.Combine(output, "input.pdb")), payload.Source, "Fixture", false])!;
            var expected = input.Provenance.MetadataReferences!.References;
            Assert.Equal(payload.Context!.References!.Length, expected.Length);
            var materials = new List<ValidatedExternalMetadataReferenceMaterial>();
            for (int index = 0; index < expected.Length; index++)
            {
                using var stream = new MemoryStream(payload.Context.References[index].Image, writable: false);
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(expected[index], stream, out var material));
                Assert.Equal(payload.Context.References[index].Identity, RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(material.Candidate.AssemblyIdentity!));
                materials.Add(material);
            }
            Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(input.Provenance, materials, out var set));
            return input with { ReferenceSet = set };
        }

        private static async Task<JsonElement> Probe(WorkerAnalysisInput input, string operation, string? outputDirectory = null)
        {
            string fixture = Path.Combine(Root(), "artifacts", "exception-flow-historical", "HistoricalPdbFixture", "bin", "Debug", "net8.0", "HistoricalPdbFixture.dll");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add(fixture); start.ArgumentList.Add(operation);
            if (outputDirectory != null) { start.ArgumentList.Add(outputDirectory); }
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            try
            {
                await process.StandardInput.WriteAsync(JsonSerializer.Serialize(input, WorkerProtocol.JsonOptions)); process.StandardInput.Close();
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
            }
            finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
            Assert.True(process.ExitCode == 0, await error); Assert.True(string.IsNullOrWhiteSpace(await error), await error);
            using var json = JsonDocument.Parse(await output); return json.RootElement.Clone();
        }
        private static void AssertGraph(Snapshot native, CanonicalExceptionFlowAnalysisResult historical, JsonElement graph)
        {
            Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32());
            Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32());
            Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean());
            Assert.Equal(native.GraphSummaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()!).ToArray());
            Assert.Equal(native.GraphSummaries, graph.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()!).ToArray());
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(historical, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())));
            Assert.True(native.RepeatedFindingsEqual);
        }
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static async Task Evidence(string name, object value)
        {
            string output = Path.Combine(Root(), "artifacts", "p5o2b12", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
        private static void Stopped(HistoricalWorkerCallResult call)
            => HistoricalWorkerLifetimeAssertions.Stopped(call);
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
            || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
    }
}
