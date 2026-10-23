using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>B18 independent contract-gap witnesses, not a partial Declared activation.</summary>
    public sealed class ExceptionFlowHistoricalDeclaredReadinessTests
    {
        private const string Summary = "<summary>Observe policy eligibility.</summary>";

        [Theory]
        [InlineData("cover-exact")]
        [InlineData("cover-base")]
        [InlineData("inheritdoc")]
        [InlineData("non-exception")]
        public async Task IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings(string scenario)
        {
            var original = Payload("mixed");
            string extra = scenario switch
            {
                "cover-exact" => "<exception cref=\"System.ArgumentException\">Covers the exact direct type.</exception>",
                "cover-base" => "<exception cref=\"System.Exception\">Base type is not exact coverage.</exception>",
                "inheritdoc" => "<inheritdoc/>",
                _ => "<exception cref=\"System.String\">Not an exception.</exception>"
            };
            // Fixed-width documentation keeps all executable spans/lines unchanged.
            var plain = original with { Source = original.Source!.Replace(Summary, Summary.PadRight(240), StringComparison.Ordinal) };
            var documented = original with { Source = original.Source!.Replace(Summary, (Summary + extra).PadRight(240), StringComparison.Ordinal) };
            var before = Native(plain); var after = Native(documented);
            var first = await Analyze(plain); var second = await Analyze(documented);
            var call = first.Dispatch!.Routing!.HistoricalAnalysis!;
            var repeated = await Analyze(documented);
            var changed = second.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.Equal(Json(call.Result), Json(changed.Result));
            Assert.Equal(Json(call.DeclaredPolicy), Json(changed.DeclaredPolicy));
            Assert.Equal(Json(changed.Result), Json(repeated.Result));
            Assert.Equal(Json(changed.DeclaredPolicy), Json(repeated.Dispatch!.Routing!.HistoricalAnalysis!.DeclaredPolicy));
            Assert.Equal(before.GetProperty("summary").GetRawText(), after.GetProperty("summary").GetRawText());
            Assert.Equal(before.GetProperty("direct").GetRawText(), after.GetProperty("direct").GetRawText());
            if (scenario == "cover-base")
            { Assert.Equal(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText()); }
            else
            { Assert.NotEqual(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText()); }
            var directType = Assert.Single(call.DeclaredPolicy!.DirectResult!.Entries).ExceptionType;
            var query = new DeclaredFindingPolicyQuery(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, directType, DeclaredFindingSourceKind.Direct);
            var eligibility = DeclaredExceptionFindingPolicy.Evaluate(call, query);
            Assert.True(eligibility.Succeeded); Assert.True(eligibility.Decision!.Relevant);
            Assert.Equal(eligibility, DeclaredExceptionFindingPolicy.Evaluate(changed, query));
            Assert.True(Has(before, "DOC610"));
            if (scenario == "cover-exact")
            {
                Assert.False(Has(after, "DOC610")); Assert.True(Has(after, "DOC611"));
            }
            if (scenario == "cover-base")
            {
                Assert.True(Has(after, "DOC610")); Assert.True(Has(after, "DOC611"));
                var baseType = ExceptionBase(documented);
                Assert.Equal(DeclaredFindingPolicyFailureCode.UnknownExceptionType,
                    DeclaredExceptionFindingPolicy.Evaluate(changed, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions,
                        baseType, DeclaredFindingSourceKind.Documented)).Failure!.Code);
            }
            if (scenario == "inheritdoc") { Assert.Empty(after.GetProperty("declaredFindings").EnumerateArray()); }
            if (scenario == "non-exception") { Assert.True(Has(after, "DOC670")); Assert.True(Has(after, "DOC610")); }
            await RejectDeclared(documented);
            Stopped(call); Stopped(changed); Stopped(repeated.Dispatch.Routing.HistoricalAnalysis!); NoHistoricalLoaded();
            await Evidence(scenario, new
            {
                Before = before,
                After = after,
                First = call,
                Changed = changed,
                Repeated = repeated.Dispatch.Routing.HistoricalAnalysis,
                Eligibility = eligibility,
                EqualFullCanonicalFlowsAndPolicyFacts = true,
                DifferentFullNativeFindings = scenario != "cover-base",
                NoHistoricalFindingImport = true,
                DeclaredStillUnsupported = true,
                ProcessesStopped = true
            });
        }

        [Fact]
        public async Task NoLocalDeclarationGuard_CannotBeReplacedByUnguardedProjectExecution()
        {
            var original = Payload("external");
            var payload = original with { Source = original.Source!.Replace("Helper.A();", "System.GC.Collect();", StringComparison.Ordinal) };
            var native = Native(payload);
            Assert.False(native.GetProperty("hasDeclaredTypes").GetBoolean());
            Assert.Empty(native.GetProperty("direct").GetProperty("entries").EnumerateArray());
            Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray());
            var input = (await Original(payload)) with { DeclaredPolicy = OptIn() };
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.False(result.Succeeded); Assert.True(result.PayloadProjection.Succeeded);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, result.Dispatch!.Decision!.Selection);
            Assert.Null(result.Result); Assert.Null(result.Dispatch.Routing!.CurrentAnalysis);
            var call = result.Dispatch.Routing.HistoricalAnalysis!;
            Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, call.Failure!.Code);
            Assert.Equal(WorkerFailureCode.UnsupportedContext, call.Failure.WorkerFailure!.Code);
            Assert.Null(call.DeclaredPolicy); Stopped(call); NoHistoricalLoaded();
            await RejectDeclared(payload);
            await Evidence("guard", new
            {
                Native = native,
                HistoricalFailure = call,
                NoPartialEvidence = true,
                NoFallback = true,
                DeclaredStillUnsupported = true,
                ProcessesStopped = true
            });
        }

        [Fact]
        public async Task SameProjectChain_HasFullPolicyFactsButNoProductiveDeclaredFindingRoute()
        {
            var original = Payload("local"); var helper = original.Context!.AdditionalSources![0];
            string text = helper.Text!.Replace("public static void A() { if (Flag)",
                "public static void A() { B(); } public static void B() { if (Flag)", StringComparison.Ordinal);
            Assert.NotEqual(helper.Text, text);
            var payload = original with { Context = original.Context with { AdditionalSources = [helper with { Text = text, Sha256 = WorkerProtocol.SourceHash(text) }] } };
            var native = Native(payload); var result = await Analyze(payload); var repeat = await Analyze(payload);
            var call = result.Dispatch!.Routing!.HistoricalAnalysis!;
            Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result));
            Assert.Equal(Json(call.Result), Json(repeat.Result));
            Assert.True(Has(native, "DOC611"));
            Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText());
            var local = Assert.Single(call.DeclaredPolicy!.Declarations!).Type!;
            var query = new DeclaredFindingPolicyQuery(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, local, DeclaredFindingSourceKind.Summary);
            var decision = DeclaredExceptionFindingPolicy.Evaluate(call, query);
            Assert.True(decision.Succeeded); Assert.True(decision.Decision!.Relevant);
            Assert.Equal(decision, DeclaredExceptionFindingPolicy.Evaluate(repeat.Dispatch!.Routing!.HistoricalAnalysis, query));
            await RejectDeclared(payload); Stopped(call); Stopped(repeat.Dispatch.Routing.HistoricalAnalysis!); NoHistoricalLoaded();
            await Evidence("chain", new
            {
                Native = native,
                Historical = call,
                Repeated = repeat.Dispatch.Routing.HistoricalAnalysis,
                Decision = decision,
                DeclaredStillUnsupported = true,
                NoHistoricalFindingImport = true,
                ProcessesStopped = true
            });
        }

        [Fact]
        public void ExistingProductionFindingContract_RequiresNativeResultsAndSemanticContext()
        {
            var native = typeof(XmlDocExceptionSemanticDetector).GetMethod("AnalyzeConfiguredExceptionFlow", BindingFlags.Static | BindingFlags.NonPublic)!;
            Assert.Equal(typeof(ExceptionFlowAnalysisResult), native.ReturnType);
            Assert.Contains(native.GetParameters(), parameter => parameter.ParameterType == typeof(CSharpCompilation));
            var add = typeof(XmlDocExceptionSemanticDetector).GetMethod("AddMissingTransitiveExceptionTagFindings", BindingFlags.Static | BindingFlags.NonPublic)!;
            Assert.Contains(add.GetParameters(), parameter => parameter.ParameterType == typeof(INamedTypeSymbol));
            Assert.Contains(add.GetParameters(), parameter => parameter.ParameterType == typeof(ExceptionFlowAnalysisResult));
            Assert.DoesNotContain(add.GetParameters(), parameter => parameter.ParameterType == typeof(CanonicalExceptionFlowAnalysisResult));
            Assert.Null(typeof(CanonicalTypeIdentity).GetProperty("BaseType"));
            Assert.DoesNotContain(typeof(WorkerDeclaredPolicyEvidence).GetProperties(), property => property.Name is "Findings" or "Documentation" or "Coverage");
        }

        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        private static bool Has(JsonElement native, string id) => native.GetProperty("declaredFindings").EnumerateArray()
            .Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id);
        private static WorkerAnalysisInput Payload(string scenario) => (WorkerAnalysisInput)typeof(ExceptionFlowHistoricalDeclaredPolicyTests)
            .GetMethod("Payload", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [scenario])!;
        private static JsonElement Native(WorkerAnalysisInput payload) => JsonSerializer.SerializeToElement(typeof(ExceptionFlowHistoricalDeclaredPolicyTests)
            .GetMethod("Native", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload]), WorkerProtocol.JsonOptions);
        private static Task<HistoricalWorkerPayloadProjectionInput> Original(WorkerAnalysisInput payload) => (Task<HistoricalWorkerPayloadProjectionInput>)typeof(ExceptionFlowHistoricalScopeTests)
            .GetMethod("ValidatedInput", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [payload])!;
        private static WorkerDeclaredPolicyRequest OptIn() => new(WorkerDeclaredPolicyContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions);
        private static async Task<ExternalExceptionFlowAnalysisResult> Analyze(WorkerAnalysisInput payload)
        {
            var input = (await Original(payload)) with { DeclaredPolicy = OptIn() };
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.True(result.Succeeded, result.PayloadProjection.Failure?.Message ?? result.Dispatch?.Routing?.HistoricalFailure?.Message);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, result.Dispatch!.Decision!.Selection);
            Assert.Null(result.Dispatch.Routing!.CurrentAnalysis);
            return result;
        }
        private static async Task RejectDeclared(WorkerAnalysisInput payload)
        {
            var input = (await Original(payload)) with { AnalysisMode = ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions, DeclaredPolicy = OptIn() };
            var result = await XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync(input, new(new(Client())));
            Assert.False(result.Succeeded); Assert.Null(result.Dispatch); Assert.Null(result.Result);
            Assert.Equal(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, result.PayloadProjection.Failure!.Code);
        }
        private static CanonicalTypeIdentity ExceptionBase(WorkerAnalysisInput payload)
        {
            var compilation = CSharpCompilation.Create(payload.Context!.AssemblyName, references: payload.Context.References!
                .Select(reference => MetadataReference.CreateFromImage(reference.Image)));
            return RoslynCanonicalIdentityFactory.CreateTypeIdentity(compilation.GetTypeByMetadataName("System.Exception")!);
        }
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
        private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(Root(), "artifacts", "exception-flow-historical",
            "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));
        private static void Stopped(HistoricalWorkerCallResult call)
            => HistoricalWorkerLifetimeAssertions.Stopped(call);
        private static void NoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(),
            assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        private static async Task Evidence(string name, object value)
        {
            string path = Path.Combine(Root(), "artifacts", "p5o2b18", name + ".json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, Json(value));
        }
    }
}
