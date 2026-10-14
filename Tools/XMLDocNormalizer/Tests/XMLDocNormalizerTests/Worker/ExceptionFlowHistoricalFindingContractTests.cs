using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Genuine bounded B19 document/finding inputs, unchanged Main policy and explicit deferred activation.</summary>
    public sealed class ExceptionFlowHistoricalFindingContractTests
    {
        [Theory]
        [InlineData("local")]
        [InlineData("external")]
        [InlineData("mixed")]
        [InlineData("combined")]
        [InlineData("unused")]
        [InlineData("partial")]
        [InlineData("nested")]
        [InlineData("exact")]
        [InlineData("base")]
        [InlineData("inheritdoc")]
        [InlineData("invalid")]
        [InlineData("no-doc")]
        [InlineData("alias")]
        [InlineData("method")]
        [InlineData("duplicate")]
        [InlineData("empty-tag")]
        public async Task CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat(string scenario)
        {
            var payload = Payload(scenario); var native = Native(payload); var document = NativeDocument(payload, native);
            var original = (await Original(payload)) with { DeclaredPolicy = OptIn() };
            var first = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(original, new(new(Client())));
            var second = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(original, new(new(Client())));
            Assert.True(first.Succeeded, first.PayloadProjection.Failure?.Message ?? first.Dispatch?.Routing?.HistoricalFailure?.Message);
            Assert.True(second.Succeeded, second.Dispatch?.Routing?.HistoricalFailure?.Message);
            var call = first.Dispatch!.Routing!.HistoricalAnalysis!; var repeat = second.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, first.Dispatch.Decision!.Selection); Assert.Null(first.Dispatch.Routing.CurrentAnalysis);
            Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result));
            Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult));
            Assert.Equal(Json(document), Json(call.DeclaredDocumentation)); Assert.Equal(Json(call.DeclaredDocumentation), Json(repeat.DeclaredDocumentation));
            Assert.Equal(Json(call.Result), Json(repeat.Result)); Assert.Equal(Json(call.DeclaredPolicy), Json(repeat.DeclaredPolicy));
            var prepared = first.PayloadProjection.Value!.Payload;
            Assert.Equal(WorkerProtocol.Provenance(prepared), call.Provenance); Assert.Equal("project-transitive", call.Provenance!.AnalysisMode);
            var read = DeclaredFindingDocumentationContract.Read(call, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions);
            var reread = DeclaredFindingDocumentationContract.Read(repeat, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions);
            Assert.True(read.Succeeded); Assert.Equal(Json(read.Facts), Json(reread.Facts)); var facts = read.Facts!;
            Assert.Equal(Json(document.TypeDisplays), Json(facts.TypeDisplays));
            Assert.Equal(document.Syntax!.HasDocumentation, facts.HasDocumentation); Assert.Equal(document.Syntax.HasInheritdoc, facts.HasInheritdoc);
            Assert.Equal(document.Syntax.Anchor, facts.Member.Position);
            var compilation = Compilation(payload); var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == payload.LogicalPath);
            var member = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context);
            Assert.Equal((tree.GetLineSpan(member.Identifier.Span).StartLinePosition.Line + 1), facts.Member.Line);
            Assert.Equal((tree.GetLineSpan(member.Identifier.Span).StartLinePosition.Character + 1), facts.Member.Column);
            var tags = HistoricalDeclaredDocumentationFacts.Tags(member);
            for (int index = 0; index < tags.Count; index++)
            {
                var tag = facts.Tags[index]; Assert.Equal(document.Bindings![index].Syntax, tag.Syntax);
                Assert.Equal(document.Bindings[index].Kind, tag.Kind); Assert.Equal(document.Bindings[index].Type, tag.Type);
                Assert.Equal(document.Bindings[index].IsException, tag.IsException); Assert.Equal(SyntaxUtils.GetSnippet(tags[index].Element), tag.Snippet);
                var raw = tag.Syntax.RawCref;
                Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionTag", targetName: string.IsNullOrWhiteSpace(raw) ? null : "cref:" + raw, filePath: payload.LogicalPath), tag.Anchor.Context);
                if (tag.IsException == true)
                {
                    var relevant = DeclaredExceptionFindingPolicy.Evaluate(call, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, tag.Type, DeclaredFindingSourceKind.Documented));
                    Assert.True(relevant.Succeeded); Assert.Equal(call.DeclaredPolicy.Declarations!.Any(item => item.Type!.Equals(tag.Type)), relevant.Decision!.Relevant);
                }
            }
            Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), call.DeclaredPolicy.Declarations!.Length != 0);
            var decisions = new List<object>();
            foreach (var expected in native.GetProperty("expected").EnumerateArray())
            {
                var type = expected.GetProperty("type").Deserialize<CanonicalTypeIdentity>(WorkerProtocol.JsonOptions)!;
                var kind = expected.GetProperty("kind").Deserialize<DeclaredFindingSourceKind>(WorkerProtocol.JsonOptions);
                var decision = DeclaredExceptionFindingPolicy.Evaluate(call, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, type, kind));
                Assert.True(decision.Succeeded); Assert.Equal(expected.GetProperty("relevant").GetBoolean(), decision.Decision!.Relevant);
                Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled);
                decisions.Add(new { Expected = expected, Decision = decision });
            }
            // All actual final native Finding fields are accounted for by the contract, not only counts.
            foreach (var finding in native.GetProperty("declaredFindings").EnumerateArray())
            {
                string id = finding.GetProperty("smell").GetProperty("id").GetString()!;
                if (id is "DOC610" or "DOC611")
                {
                    Assert.Equal(facts.Member.Line, finding.GetProperty("line").GetInt32()); Assert.Equal(facts.Member.Column, finding.GetProperty("column").GetInt32());
                    Assert.Contains(facts.TypeDisplays, item => finding.GetProperty("message").GetString()!.Contains(item.DisplayName!, StringComparison.Ordinal));
                    var context = finding.GetProperty("context"); string name = context.GetProperty("targetName").GetString()!;
                    Assert.Equal(Json(facts.Member.Context with { TargetName = name }), context.GetRawText());
                }
                else
                {
                    Assert.Contains(facts.Tags, tag => tag.Anchor.Line == finding.GetProperty("line").GetInt32()
                        && tag.Anchor.Column == finding.GetProperty("column").GetInt32() && tag.Snippet == finding.GetProperty("snippet").GetString()
                        && Json(tag.Anchor.Context) == finding.GetProperty("context").GetRawText());
                }
            }
            if (scenario == "exact" || scenario == "alias") { Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610"); }
            if (scenario == "base") { Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610"); }
            if (scenario == "inheritdoc" || scenario == "no-doc") { Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray()); }
            if (scenario == "invalid") { Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660"); Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC670"); }
            if (scenario == "external") { Assert.False(native.GetProperty("hasDeclaredTypes").GetBoolean()); Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray()); }
            if (scenario == "method") { Assert.Contains(facts.Tags, tag => tag.Kind == WorkerCrefBindingKind.OtherSymbol); }
            if (scenario == "empty-tag") { Assert.All(facts.Tags, tag => Assert.Null(tag.Syntax.RawCref)); }
            if (scenario == "duplicate") { Assert.Equal(2, facts.Tags.Length); }
            foreach (var mode in new WorkerDeclaredPolicyMode?[] { null, WorkerDeclaredPolicyMode.Unspecified, (WorkerDeclaredPolicyMode)999 })
            { Assert.Equal(DeclaredFindingPolicyFailureCode.InvalidMode, DeclaredFindingDocumentationContract.Read(call, mode).Failure!.Code); }
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredFindingDocumentationContract.Read(call with { }, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Failure!.Code);
            string beforeMutation = Json(facts); string rawDocument = Json(call.DeclaredDocumentation);
            if (call.DeclaredDocumentation!.Bindings!.Length != 0) { call.DeclaredDocumentation.Bindings[0] = new(null, null, null, null); }
            if (call.DeclaredDocumentation.TypeDisplays!.Length != 0) { call.DeclaredDocumentation.TypeDisplays[0] = new(null, null); }
            Assert.Equal(beforeMutation, Json(DeclaredFindingDocumentationContract.Read(call, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Facts));
            var disabled = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(original with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions }, new(new(Client())));
            Assert.False(disabled.Succeeded); Assert.Null(disabled.Dispatch);
            Stopped(call); Stopped(repeat); NoHistoricalLoaded();
            await Evidence("case-" + scenario, new
            {
                Scenario = scenario,
                Native = native,
                NativeDocument = document,
                HistoricalDocument = JsonSerializer.Deserialize<JsonElement>(rawDocument),
                RepeatedDocument = repeat.DeclaredDocumentation,
                HistoricalSummary = call.Result,
                HistoricalPolicy = call.DeclaredPolicy,
                Contract = facts,
                Decisions = decisions,
                call.Provenance,
                call.Identity,
                FirstProcess = call.ProcessId,
                SecondProcess = repeat.ProcessId,
                GuardInputEquivalent = true,
                GuardExecutionDeferred = true,
                DeclaredStillUnsupported = true,
                CompleteMemberContractParity = true,
                NoFindingImport = true,
                CloneRejected = true,
                ImmutableReceipt = true,
                NoFallback = true,
                ProcessesStopped = true,
                NoHistoricalLoadedInMain = true
            });
        }

        [Theory]
        [InlineData("missing-document")]
        [InlineData("missing-bindings")]
        [InlineData("omission")]
        [InlineData("owner")]
        [InlineData("member-span")]
        [InlineData("method")]
        [InlineData("cref")]
        [InlineData("inheritdoc")]
        [InlineData("has-doc")]
        [InlineData("default-mode")]
        [InlineData("missing-mode")]
        [InlineData("profile")]
        [InlineData("type-identity")]
        [InlineData("exception-classification")]
        [InlineData("display")]
        [InlineData("missing-displays")]
        [InlineData("duplicate-displays")]
        [InlineData("unknown-kind")]
        [InlineData("missing-type")]
        [InlineData("direct-conflict")]
        [InlineData("missing-summary")]
        [InlineData("missing-direct")]
        public async Task MissingConflictingOrForgedDocumentContract_FailsClosed(string scenario)
        {
            var input = (await Original(Payload("exact"))) with { DeclaredPolicy = OptIn() };
            var projection = HistoricalWorkerPayloadProjection.Project(input); Assert.True(projection.Succeeded);
            var payload = projection.Value!.Payload; var call = await Client().AnalyzeAsync(payload); Assert.True(call.Success, call.Failure?.Message);
            var response = JsonSerializer.SerializeToNode(new WorkerResponse(WorkerProtocol.Version, "analyze", true, call.Identity, call.Result,
                Provenance: call.Provenance, DeclaredPolicy: call.DeclaredPolicy, DeclaredDocumentation: call.DeclaredDocumentation), WorkerProtocol.JsonOptions)!;
            var doc = response["declaredDocumentation"]!;
            if (scenario == "missing-document") { response.AsObject().Remove("declaredDocumentation"); }
            if (scenario == "missing-bindings") { doc.AsObject().Remove("bindings"); }
            if (scenario == "omission") { doc["bindings"] = new JsonArray(); doc["syntax"]!["tags"] = new JsonArray(); }
            if (scenario == "owner") { doc["syntax"]!["logicalPath"] = "/foreign/Root.cs"; }
            if (scenario == "member-span") { doc["syntax"]!["memberStart"] = 0; }
            if (scenario == "method") { doc["method"]!["metadataName"] = "Other"; }
            if (scenario == "cref") { doc["bindings"]![0]!["syntax"]!["rawCref"] = "LocalException"; }
            if (scenario == "inheritdoc") { doc["syntax"]!["hasInheritdoc"] = true; }
            if (scenario == "has-doc") { doc["syntax"]!["hasDocumentation"] = false; }
            if (scenario == "default-mode") { doc["mode"] = "unspecified"; }
            if (scenario == "missing-mode") { doc.AsObject().Remove("mode"); }
            if (scenario == "profile") { doc["profile"] = "general-report-v0"; }
            if (scenario == "type-identity") { doc["bindings"]![0]!["type"]!["assembly"]!["versionMajor"] = 321; }
            if (scenario == "exception-classification") { doc["bindings"]![0]!["isException"] = false; }
            if (scenario == "display") { doc["typeDisplays"]![0]!["displayName"] = "FabricatedException"; }
            if (scenario == "missing-displays") { doc.AsObject().Remove("typeDisplays"); }
            if (scenario == "duplicate-displays") { doc["typeDisplays"]!.AsArray().Add(doc["typeDisplays"]![0]!.DeepClone()); }
            if (scenario == "unknown-kind") { doc["bindings"]![0]!["kind"] = "unspecified"; }
            if (scenario == "missing-type") { doc["bindings"]![0]!.AsObject().Remove("type"); }
            if (scenario == "direct-conflict") { response["declaredPolicy"]!["directResult"]!["entries"] = new JsonArray(); }
            if (scenario == "missing-summary") { response.AsObject().Remove("result"); }
            if (scenario == "missing-direct") { response["declaredPolicy"]!.AsObject().Remove("directResult"); }
            var rejected = HistoricalWorkerResponseValidation.Validate(response.ToJsonString(), "", 0, payload);
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, rejected.Failure!.Code); Assert.Null(rejected.Result);
            Assert.Null(rejected.DeclaredPolicy); Assert.Null(rejected.DeclaredDocumentation);
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredFindingDocumentationContract.Read(rejected, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Failure!.Code);
            Stopped(call); NoHistoricalLoaded();
            await Evidence("negative-" + scenario, new { Scenario = scenario, rejected.Failure, NoContractReceipt = true, NoResult = true, NoFallback = true, ProcessesStopped = true });
        }

        [Fact]
        public async Task OriginalContextReceipt_RequestAdmissionLegacyAndUncertainGuard_AreNotBypassed()
        {
            var original = await Original(Payload("mixed"));
            var request = OptIn();
            foreach (var invalid in new[] { request.Documentation! with { Profile = null }, request.Documentation! with { Mode = null },
                request.Documentation! with { Mode = WorkerDeclaredPolicyMode.Unspecified }, request.Documentation! with { Syntax = new(null, null, null, null, null, null, null) } })
            { Assert.False(HistoricalWorkerPayloadProjection.Project(original with { DeclaredPolicy = request with { Documentation = invalid } }).Succeeded); }
            var projected = HistoricalWorkerPayloadProjection.Project(original with { DeclaredPolicy = request }); Assert.True(projected.Succeeded);
            var clone = await Client().AnalyzeAsync(projected.Value!.Payload with { });
            Assert.Equal(HistoricalWorkerClientFailureCode.MalformedResponse, clone.Failure!.Code); Assert.Null(clone.Result); Stopped(clone);
            var legacy = await Client().AnalyzeAsync(Payload("mixed")); Assert.True(legacy.Success); Assert.Null(legacy.DeclaredDocumentation);
            Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredFindingDocumentationContract.Read(legacy, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions).Failure!.Code); Stopped(legacy);
            var external = Payload("external"); var uncertain = external with { Source = external.Source!.Replace("Helper.A();", "System.GC.Collect();", StringComparison.Ordinal) };
            var native = Native(uncertain); Assert.False(native.GetProperty("hasDeclaredTypes").GetBoolean()); Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray());
            var failed = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync((await Original(uncertain)) with { DeclaredPolicy = request }, new(new(Client())));
            var call = failed.Dispatch!.Routing!.HistoricalAnalysis!; Assert.False(failed.Succeeded); Assert.Null(call.DeclaredDocumentation);
            Assert.Equal(WorkerFailureCode.AnalysisFailure, call.Failure!.WorkerFailure!.Code); Assert.Null(failed.Result); Assert.Null(failed.Dispatch.Routing.CurrentAnalysis); Stopped(call);
            NoHistoricalLoaded();
            await Evidence("admission", new
            {
                CallerFactsRejected = true,
                InvalidDefaultModeRejected = true,
                ClonedPayloadRejected = true,
                LegacyCannotMintContract = true,
                GuardNative = native,
                GuardFailure = call.Failure,
                GuardExecutionDeferred = true,
                DeclaredStillUnsupported = true,
                NoFallback = true,
                ProcessesStopped = true
            });
        }

        private static string Id(JsonElement finding) => finding.GetProperty("smell").GetProperty("id").GetString()!;
        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        private static WorkerDeclaredPolicyRequest OptIn() => new(WorkerDeclaredPolicyContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions,
            Documentation: new(WorkerDeclaredDocumentationContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions));
        private static WorkerAnalysisInput Payload(string scenario)
        {
            string parent = scenario is "local" or "external" or "mixed" or "combined" or "unused" or "partial" or "nested" ? scenario : "mixed";
            var input = (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalDeclaredPolicyTests).GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [parent])!;
            string extra = scenario switch
            {
                "exact" => "<exception cref=\"System.ArgumentException\">Direct covered.</exception>",
                "base" => "<exception cref=\"System.Exception\">Base is not exact coverage.</exception>",
                "inheritdoc" => "<inheritdoc/>",
                "invalid" => "<exception cref=\"MissingException\">Unresolved.</exception><exception cref=\"System.String\">Not an exception.</exception><exception>Missing cref.</exception>",
                "alias" => "<exception cref=\"Alias\">Alias exact coverage.</exception>",
                "method" => "<exception cref=\"Fixture.Root\">Resolved method, not type.</exception>",
                "duplicate" => "<exception cref=\"LocalException\">One.</exception><exception cref=\"LocalException\">Two.</exception>",
                "empty-tag" => "<exception>Missing cref.</exception><exception cref=\"System.String\"/>",
                _ => ""
            };
            const string summary = "<summary>Observe policy eligibility.</summary>";
            input = input with { Source = input.Source!.Replace(summary, summary + extra, StringComparison.Ordinal) };
            if (scenario == "no-doc") { input = input with { Source = input.Source!.Replace("/// " + summary + "\r\n", "", StringComparison.Ordinal) }; }
            if (scenario == "alias") { input = input with { Source = "using Alias = System.ArgumentException;\r\n" + input.Source }; }
            return input;
        }
        private static JsonElement Native(WorkerAnalysisInput payload) => JsonSerializer.SerializeToElement(typeof(ExceptionFlowHistoricalDeclaredPolicyTests)
            .GetMethod("Native", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload]), WorkerProtocol.JsonOptions);
        private static CSharpCompilation Compilation(WorkerAnalysisInput payload)
        {
            var context = payload.Context!;
            var documents = new[] { new WorkerSourceDocument(payload.LogicalPath!, payload.Source!, "", ParseContext: context.ParseContext) }.Concat(context.AdditionalSources!);
            var trees = context.Scope!.OrderedSourcePaths!.Select(path => CSharpSyntaxTree.ParseText(documents.Single(item => item.LogicalPath == path).Text,
                new CSharpParseOptions(LanguageVersion.CSharp12, DocumentationMode.Parse, preprocessorSymbols: context.ParseContext!.PreprocessorSymbols), path));
            return CSharpCompilation.Create(context.AssemblyName, trees, context.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image)),
                new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        }
        private static WorkerDeclaredDocumentationEvidence NativeDocument(WorkerAnalysisInput payload, JsonElement native)
        {
            var compilation = Compilation(payload); var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == payload.LogicalPath);
            var member = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            return HistoricalDeclaredDocumentationFacts.Create(compilation.GetSemanticModel(tree), member,
                native.GetProperty("direct").Deserialize<CanonicalExceptionFlowAnalysisResult>(WorkerProtocol.JsonOptions)!,
                native.GetProperty("summary").Deserialize<CanonicalExceptionFlowAnalysisResult>(WorkerProtocol.JsonOptions)!);
        }
        private static Task<HistoricalWorkerPayloadProjectionInput> Original(WorkerAnalysisInput payload) => (Task<HistoricalWorkerPayloadProjectionInput>)typeof(ExceptionFlowHistoricalScopeTests)
            .GetMethod("ValidatedInput", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload])!;
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static void Stopped(HistoricalWorkerCallResult call) { Assert.NotNull(call.ProcessId); try { using var process = Process.GetProcessById(call.ProcessId.Value); Assert.True(process.HasExited); } catch (ArgumentException) { } }
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        {
            string path = Path.Combine(Root(), "artifacts", "p5o2b19", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, Json(value));
        }
    }
}
