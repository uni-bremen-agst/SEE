using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Real execution origin, exact binding admission and single-use replay tests; no Finding/guard implementation.</summary>
    public sealed class ExceptionFlowHistoricalDocumentationAttestationTests
    {
        [Theory]
        [InlineData("exact")]
        [InlineData("alias")]
        [InlineData("base")]
        [InlineData("invalid")]
        [InlineData("method")]
        [InlineData("duplicate")]
        [InlineData("empty-tag")]
        [InlineData("no-doc")]
        public async Task RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority(string scenario)
        {
            var payload = Payload(scenario); var prepared = await Prepared(payload);
            var first = await Client().AnalyzeAsync(prepared); var repeat = await Client().AnalyzeAsync(prepared);
            Assert.True(first.Success, first.Failure?.Message); Assert.True(repeat.Success, repeat.Failure?.Message);
            Assert.NotEqual(first.ProcessId, repeat.ProcessId);
            Assert.Equal(Json(first.DeclaredDocumentation), Json(repeat.DeclaredDocumentation));
            Assert.Equal(Json(first.Result), Json(repeat.Result)); Assert.Equal(Json(first.DeclaredPolicy), Json(repeat.DeclaredPolicy));
            Assert.Equal(WorkerProtocol.Provenance(prepared), first.Provenance);
            Assert.True(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(first, out var firstFacts));
            Assert.True(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(repeat, out var repeatFacts));
            Assert.NotNull(firstFacts!.DocumentationExecution); Assert.NotNull(repeatFacts!.DocumentationExecution);
            Assert.NotSame(firstFacts.DocumentationExecution, repeatFacts.DocumentationExecution);
            var read = DeclaredFindingDocumentationContract.Read(first, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions);
            Assert.True(read.Succeeded); Assert.Equal(Json(read.Facts), Json(DeclaredFindingDocumentationContract.Read(repeat, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Facts));
            var native = Native(payload);
            Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result));
            Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult));
            var nativeDocument = typeof(ExceptionFlowHistoricalFindingContractTests).GetMethod("NativeDocument", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload, native]);
            Assert.Equal(Json(nativeDocument), Json(first.DeclaredDocumentation));
            string response = Response(first).ToJsonString();
            Assert.DoesNotContain("attestation", response, StringComparison.OrdinalIgnoreCase); // No new wire field or nondeterministic nonce.
            var replay = HistoricalWorkerResponseValidation.Validate(response, first.Diagnostics, 0, prepared, first.ProcessId, firstFacts.DocumentationExecution);
            Rejected(replay);
            Assert.Contains("execution attestation", replay.Failure!.Message, StringComparison.Ordinal);
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence,
                DeclaredFindingDocumentationContract.Read(first with { }, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Failure!.Code);
            // Mutable transport DTO arrays cannot replace the immutable admitted association.
            string immutable = Json(read.Facts);
            if (first.DeclaredDocumentation!.Bindings!.Length != 0) { first.DeclaredDocumentation.Bindings[0] = new(null, null, null, null); }
            Assert.Equal(immutable, Json(DeclaredFindingDocumentationContract.Read(first, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Facts));
            Stopped(first); Stopped(repeat); NoHistoricalLoaded();
            await Evidence("positive-" + scenario, new
            {
                Scenario = scenario,
                Native = native,
                Document = repeat.DeclaredDocumentation,
                NativeDocument = nativeDocument,
                Direct = repeat.DeclaredPolicy!.DirectResult,
                Summary = repeat.Result,
                Declarations = repeat.DeclaredPolicy.Declarations,
                Contract = read.Facts,
                repeat.Provenance,
                repeat.Identity,
                FirstProcess = first.ProcessId,
                SecondProcess = repeat.ProcessId,
                GenuineExecutionAttested = true,
                CanonicalRepeatEqual = true,
                FreshCapabilities = true,
                ConsumedReplayRejected = true,
                ImmutableReceipt = true,
                NoWireChange = true,
                NoFallback = true,
                DeclaredStillUnsupported = true,
                ProcessesStopped = true,
                NoHistoricalLoadedInMain = true
            });
        }

        [Theory]
        [InlineData("missing-attestation")]
        [InlineData("unregistered-attestation")]
        [InlineData("receipt-replay")]
        [InlineData("type")]
        [InlineData("coherent-type")]
        [InlineData("member")]
        [InlineData("source")]
        [InlineData("ownership")]
        [InlineData("default-kind")]
        [InlineData("missing-type")]
        [InlineData("missing-bindings")]
        [InlineData("unknown-protocol")]
        [InlineData("unknown-wire-attestation")]
        [InlineData("cross-binding")]
        [InlineData("cross-target")]
        [InlineData("cross-source")]
        [InlineData("cloned-input")]
        public async Task MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt(string scenario)
        {
            var prepared = await Prepared(Payload("exact")); var genuine = await Client().AnalyzeAsync(prepared);
            Assert.True(genuine.Success, genuine.Failure?.Message);
            Assert.True(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(genuine, out var facts));
            var response = Response(genuine); var document = response["declaredDocumentation"]!;
            HistoricalWorkerCallResult? other = null;
            if (scenario is "cross-binding" or "cross-target" or "cross-source")
            {
                var payload = Payload("base");
                if (scenario == "cross-target")
                {
                    payload = payload with
                    {
                        Source = payload.Source!.Replace("void Root()", "void Other()", StringComparison.Ordinal)
                        .Replace("public static class Fixture {", "public static class Fixture { public static void Root() { }", StringComparison.Ordinal)
                    };
                }
                if (scenario == "cross-source") { payload = payload with { Source = "\r\n" + payload.Source }; }
                other = await Client().AnalyzeAsync(await Prepared(payload, scenario == "cross-target" ? "Other" : "Root")); Assert.True(other.Success, other.Failure?.Message);
                response["declaredDocumentation"] = JsonSerializer.SerializeToNode(other.DeclaredDocumentation, WorkerProtocol.JsonOptions);
            }
            if (scenario == "type") { document["bindings"]![0]!["type"]!["assembly"]!["versionMajor"] = 123; }
            if (scenario == "coherent-type")
            {
                var local = genuine.DeclaredPolicy!.Declarations![0].Type;
                document["bindings"]![0]!["type"] = JsonSerializer.SerializeToNode(local, WorkerProtocol.JsonOptions);
                document["bindings"]![0]!["isException"] = true; // A real valid local type, already present in Summary/displays.
            }
            if (scenario == "member") { document["method"]!["metadataName"] = "Other"; }
            if (scenario == "source") { document["syntax"]!["logicalPath"] = "/foreign/Root.cs"; }
            if (scenario == "ownership") { response["declaredPolicy"]!["declarations"]![0]!["declaringSourcePaths"]![0] = "/foreign/Helper.cs"; }
            if (scenario == "default-kind") { document["bindings"]![0]!["kind"] = "unspecified"; }
            if (scenario == "missing-type") { document["bindings"]![0]!.AsObject().Remove("type"); }
            if (scenario == "missing-bindings") { document.AsObject().Remove("bindings"); }
            if (scenario == "unknown-protocol") { response["protocolVersion"] = 999; }
            if (scenario == "unknown-wire-attestation") { response["documentationAttestation"] = new JsonObject { ["trusted"] = true }; }
            var execution = scenario == "missing-attestation" ? null : scenario == "unregistered-attestation"
                ? new HistoricalWorkerClient.DocumentationExecution() : facts!.DocumentationExecution;
            var supplied = scenario == "cloned-input" ? prepared with { } : prepared;
            var rejected = HistoricalWorkerResponseValidation.Validate(response.ToJsonString(), genuine.Diagnostics, 0, supplied, genuine.ProcessId, execution);
            Rejected(rejected); Stopped(genuine); if (other != null) { Stopped(other); }
            NoHistoricalLoaded();
            await Evidence("negative-" + scenario, new
            {
                Scenario = scenario,
                Rejection = rejected.Failure,
                NoDocumentationReceipt = true,
                NoResult = true,
                OtherDocument = other?.DeclaredDocumentation,
                GenuineDocument = genuine.DeclaredDocumentation,
                ActualWorker = true,
                NoFallback = true,
                DeclaredStillUnsupported = true,
                ProcessesStopped = true,
                NoHistoricalLoadedInMain = true
            });
        }

        [Fact]
        public async Task ActualReplayProcess_CannotAttestCopiedGenuineBindingsOrWorkerIdentity_EvenWithWorkerFilename()
        {
            var prepared = await Prepared(Payload("exact")); var genuine = await Client().AnalyzeAsync(prepared);
            Assert.True(genuine.Success, genuine.Failure?.Message);
            string directory = Path.Combine(Root(), "artifacts", "p5o2b21", "replay-peer-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string fixture = Path.Combine(Root(), "artifacts", "p5o2b4", "test-process", "bin", "Debug", "net8.0", "BoundaryTestProcess");
            string peer = Path.Combine(directory, "XMLDocNormalizer.HistoricalWorker.dll");
            File.Copy(fixture + ".dll", peer);
            File.Copy(fixture + ".runtimeconfig.json", Path.Combine(directory, "XMLDocNormalizer.HistoricalWorker.runtimeconfig.json"));
            await File.WriteAllTextAsync(Path.Combine(directory, "b21-replayed-worker-response.json"), Response(genuine).ToJsonString());
            var copied = await new HistoricalWorkerClient("dotnet", peer, TimeSpan.FromSeconds(60)).AnalyzeAsync(prepared);
            Rejected(copied); Assert.NotNull(copied.ProcessId);
            Assert.Contains("execution attestation", copied.Failure!.Message, StringComparison.Ordinal);
            Stopped(genuine); Stopped(copied); NoHistoricalLoaded();
            await Evidence("physical-worker", new
            {
                GenuineWorker = genuine.Identity,
                FakeProcess = copied.ProcessId,
                Rejection = copied.Failure,
                ExactGenuineResponseCopied = true,
                ActualPipePeerRan = true,
                ClaimedWorkerFilename = true,
                PhysicalImageAssociationRequired = true,
                NoDocumentationReceipt = true,
                NoFallback = true,
                ProcessesStopped = true,
                NoHistoricalLoadedInMain = true
            });
        }

        private static void Rejected(HistoricalWorkerCallResult call)
        {
            Assert.False(call.Success); Assert.Null(call.Result); Assert.Null(call.DeclaredPolicy); Assert.Null(call.DeclaredDocumentation);
            Assert.False(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(call, out _));
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence,
                DeclaredFindingDocumentationContract.Read(call, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Failure!.Code);
        }
        private static JsonNode Response(HistoricalWorkerCallResult call) => JsonSerializer.SerializeToNode(new WorkerResponse(WorkerProtocol.Version,
            "analyze", true, call.Identity, call.Result, Provenance: call.Provenance, DeclaredPolicy: call.DeclaredPolicy,
            DeclaredDocumentation: call.DeclaredDocumentation), WorkerProtocol.JsonOptions)!;
        private static WorkerAnalysisInput Payload(string scenario) => (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalFindingContractTests)
            .GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [scenario])!;
        private static JsonElement Native(WorkerAnalysisInput payload) => JsonSerializer.SerializeToElement(typeof(ExceptionFlowHistoricalDeclaredPolicyTests)
            .GetMethod("Native", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload]), WorkerProtocol.JsonOptions);
        private static async Task<WorkerAnalysisInput> Prepared(WorkerAnalysisInput payload, string method = "Root")
        {
            var original = await (Task<HistoricalWorkerPayloadProjectionInput>)typeof(ExceptionFlowHistoricalScopeTests)
                .GetMethod("ValidatedInput", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload])!;
            if (method != "Root") { original = original with { Root = original.Root.ContainingType.GetMembers(method).OfType<IMethodSymbol>().Single() }; }
            var projection = HistoricalWorkerPayloadProjection.Project(original with
            {
                DeclaredPolicy = new(WorkerDeclaredPolicyContract.Profile,
                WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, Documentation: new(WorkerDeclaredDocumentationContract.Profile,
                    WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions))
            });
            Assert.True(projection.Succeeded, projection.Failure?.Message); return projection.Value!.Payload;
        }
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical",
            "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        private static void Stopped(HistoricalWorkerCallResult call) { Assert.NotNull(call.ProcessId); try { using var process = Process.GetProcessById(call.ProcessId.Value); Assert.True(process.HasExited); } catch (ArgumentException) { } }
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical");
        private static async Task Evidence(string name, object value)
        {
            string path = Path.Combine(Root(), "artifacts", "p5o2b21", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, Json(value));
        }
    }
}
