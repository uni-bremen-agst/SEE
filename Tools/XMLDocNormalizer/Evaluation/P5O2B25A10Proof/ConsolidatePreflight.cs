using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class ConsolidatePreflight
{
    private sealed record Change(string Group, string Case, string File, string[] Parameters, string[] Modes,
        object[] Assertions, string[] Reasons, string NewSemantics, bool Approved);
    private static string S(JsonElement row, string name) => row.GetProperty(name).GetString()!;
    private static JsonElement[] A(JsonElement row, string name) => row.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array ? a.EnumerateArray().ToArray() : [];
    private static string Method(string id) => Regex.Replace(id, @"#\d+$", "");
    private static bool Product(JsonElement row) => A(row, "Entries").Any(e => Regex.IsMatch(e.GetString()!, "AnalyzeSolutionTransitively|SummaryAnalysisSession.Analyze|FindExceptionSmells|AnalyzeExternalExceptionFlow|FindExternalExceptionSmells|AnalysisRouter.Analyze|AnalysisDispatch.Analyze|HistoricalWorkerClient.Analyze"));
    internal static void Run(string tool)
    {
        string root = Path.Combine(tool, "Evaluation/P5O2B25A10Proof");
        string output = Path.Combine(root, "Complete-fixture-preflight.json");
        string batchPath = Path.Combine(root, "Final-consolidated-approval-batch.md");
        if (File.Exists(output) || File.Exists(batchPath))
        {
            // Explicit owned-A10 draft revision, NOT historical B25-A9 evidence or a gate retry.
            string archive = Path.Combine(tool, "artifacts/p5o2b25a10/closure-draft-r3.json");
            if (File.Exists(archive)) throw new InvalidOperationException("Final consolidation already revised; preserve records.");
            File.Copy(output, archive);
            File.Copy(batchPath, Path.Combine(tool, "artifacts/p5o2b25a10/approval-draft-r3.md"));
        }
        JsonDocument Load(string name) => JsonDocument.Parse(File.ReadAllText(Path.Combine(tool, "artifacts/p5o2b25a10/" + name)));
        using var old = Load("production-finding-probe.json");
        using var fixtures = Load("expanded-semantic-fixtures-r3.json");
        using var classified = Load("expanded-semantic-classification-r2.json");
        using var index = Load("test-path-inventory-r1.json");
        using var workers = Load("worker-fixture-families-r1.json");
        using var originalAll = Load("all-finding-fixtures-r1.json");
        using var originalClassified = Load("all-finding-classification-r1.json");
        using var r7 = Load("production-finding-assertion-map.json");
        var expandedCases = A(fixtures.RootElement, "Cases");
        var absent = A(originalAll.RootElement, "Cases").Where(c => !expandedCases.Any(e => S(e, "Id") == S(c, "Id"))).Select(c => S(c, "Id")).ToHashSet();
        var cases = expandedCases.Concat(A(originalAll.RootElement, "Cases").Where(c => absent.Contains(S(c, "Id")))).ToArray();
        var calls = A(fixtures.RootElement, "Captures").Concat(A(originalAll.RootElement, "Captures").Where(c => absent.Contains(S(c, "Case")))).ToArray();
        var assertions = A(fixtures.RootElement, "Assertions").Concat(A(originalAll.RootElement, "Assertions").Where(c => absent.Contains(S(c, "Case")))).ToArray();
        var rows = A(classified.RootElement, "Rows").Concat(A(originalClassified.RootElement, "Rows").Where(c => absent.Contains(S(c, "Case")))).ToArray();
        var tests = A(index.RootElement, "Rows");
        var approvedR7 = r7.RootElement.EnumerateArray().Select(r => S(r, "Test")).ToHashSet(StringComparer.Ordinal);
        var changes = new List<Change>();
        string[] Reasons(IEnumerable<JsonElement> rs) => rs.SelectMany(r => A(r, "Roots").SelectMany(n => A(n, "Evidence")))
            .Select(e => e.GetString()!).Distinct().Order(StringComparer.Ordinal).ToArray();
        object[] Assertions(IEnumerable<JsonElement> source) => source.GroupBy(a => S(a, "File") + ":" + a.GetProperty("Line").GetInt32() + ":" + S(a, "Assertion"))
            .Select(g => (object)new { File = S(g.First(), "File"), Line = g.First().GetProperty("Line").GetInt32(), Current = S(g.First(), "Assertion") }).ToArray();
        foreach (var group in rows.Where(r => S(r, "Classification") == "OUTSIDE PROFILE"
            && A(r, "Findings").Any(f => Regex.IsMatch(S(f, "Id"), "^DOC(610|611|630|632)$"))).GroupBy(r => S(r, "Case")))
        {
            string name = Method(group.Key);
            // Follow the actual assertion predicate, not the test name: these assert DOC631 only.
            string[] actualAssertions = assertions.Where(a => S(a, "Case") == group.Key).Select(a => Regex.Replace(S(a, "Assertion"), @"\s+", "")).ToArray();
            if (actualAssertions.Any(s => s.Contains("ExceptionFlowNotDecidable", StringComparison.Ordinal))
                && !actualAssertions.Any(s => s.Contains("MissingTransitiveExceptionDocumentation", StringComparison.Ordinal) || s.Contains("MissingExceptionTag", StringComparison.Ordinal))) continue;
            var c = cases.Single(c => S(c, "Id") == group.Key);
            bool approved = approvedR7.Contains(name);
            changes.Add(new(approved ? "R7" : "R8", group.Key, S(c, "File"), A(c, "Parameters").Select(p => p.ToString()).ToArray(),
                group.Select(r => S(r, "Mode")).Distinct().Order().ToArray(), Assertions(assertions.Where(a => S(a, "Case") == group.Key)), Reasons(group),
                "ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.", approved));
        }
        foreach (var group in rows.Where(r => S(r, "Classification") == "OUTSIDE PROFILE" && S(r, "Mode") == "SolutionTransitiveNative").GroupBy(r => S(r, "Case")))
        {
            string name = Method(group.Key);
            // XML evidence is explicitly non-Proven and may be retained as diagnostic inventory facts.
            // Mere presence of uncertainty is compatible and must NOT be weakened.
            var positive = assertions.Where(a => S(a, "Case") == group.Key
                && ((Regex.IsMatch(S(a, "Assertion"), @"Assert\.(NotEmpty|Single)\(") && S(a, "Assertion").Contains("GetExceptionPaths", StringComparison.Ordinal))
                    || Regex.IsMatch(S(a, "Assertion"), @"Assert\.Equal\(\s*64,"))).ToArray();
            if (positive.Length == 0) continue;
            var c = cases.Single(c => S(c, "Id") == group.Key);
            bool truncation = Reasons(group).Any(r => r.Contains("required result paths are truncated", StringComparison.Ordinal));
            // These two already assert truncation, NOT complete success. Their finite admitted graph
            // may retain 64 internal diagnostic paths while the classified projection blocks normal output.
            if (truncation) continue;
            var selected = truncation ? assertions.Where(a => S(a, "Case") == group.Key).ToArray() : positive;
            changes.Add(new("R9", group.Key, S(c, "File"), A(c, "Parameters").Select(p => p.ToString()).ToArray(), ["SolutionTransitiveNative ONLY"],
                Assertions(selected), Reasons(group),
                "ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.", false));
        }
        var failureCases = new (string Class, string Method, string Case, int Line, string Witness)[]
        {
            ("ExceptionFlowAnalysisDispatchTests", "WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess", "source=async Task Root await Task.CompletedTask ONLY; not Missing()", 189, "Runtime-await required capability unavailable in historical adapter"),
            ("ExceptionFlowAnalysisRouterTests", "HistoricalAnalysisFailure_PreservesOriginalB4FailureContract", "source=async Task Root await Task.CompletedTask ONLY; not Missing()", 161, "Runtime-await required capability unavailable in historical adapter"),
            ("HistoricalWorkerClientTests", "RealWorkerFailure_IsStructuredAndNeverEmptySuccess", "source=async Task Root await Task.CompletedTask ONLY; other InlineData unchanged", 84, "Runtime-await required capability unavailable in historical adapter"),
            ("HistoricalWorkerProcessTests", "HistoricalAwait_ActuallyPropagatesUnavailableInformationAsFailure", "Fact", 146, "Runtime-await required capability unavailable; retain original information-unavailable detail assertion"),
            ("ExceptionFlowHistoricalDeclaredBoundaryTests", "Uncertainty_RelevanceIsDetectorPolicy_NotFilteredRawAnalysisOrXmlProof", "Payload(uncertain), worker code ONLY", 190, "GC.Collect metadata target has no modeled executable body; preserve DOC631 relevance differential"),
            ("ExceptionFlowHistoricalDeclaredPolicyTests", "IncompleteRequestOriginalReceiptsAndUnsupportedShapes_DoNotActivateOrDowngrade", "Payload(uncertain), final worker failure ONLY", 236, "GC.Collect metadata body unavailable; unrelated negative requests remain unchanged"),
            ("ExceptionFlowHistoricalDeclaredReadinessTests", "NoLocalDeclarationGuard_CannotBeReplacedByUnguardedProjectExecution", "unguarded Project worker ONLY; original Declared guard unchanged", 109, "GC.Collect target/body unavailable; do not widen original guard"),
            ("ExceptionFlowHistoricalFindingContractTests", "OriginalContextReceipt_RequestAdmissionLegacyAndUncertainGuard_AreNotBypassed", "uncertain external payload final worker ONLY", 234, "GC.Collect target/body unavailable; receipts/replay assertions unchanged"),
            ("ExceptionFlowHistoricalProductionAdmissionTests", "ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation", "scenario=guard ONLY", 153, "GC.Collect target/body unavailable; no local Declared summary request"),
            ("ExceptionFlowHistoricalDeclaredProductionFindingTests", "ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat", "scenario=guard-direct, unguarded projectCall ONLY", 86, "GC.Collect unknown residual; guarded Direct DOC610 remains unchanged"),
            ("ExceptionFlowHistoricalDeclaredProductionFindingTests", "SelectedSummaryWorkerFailure_DoesNotPublishPartialDirectFindings", "Fact, selected Summary code ONLY", 256, "GC.Collect target/body unavailable; selected Direct facts are separate and not fallback output"),
            ("ExceptionFlowHistoricalBoundedFindingTests", "UncertainSummary_IsNotRequestedByDirect_AndFailsClosedForProject", "project=true ONLY; project=false unchanged", 140, "guard-direct GC.Collect unavailable; no partial finding receipt"),
            ("ExceptionFlowHistoricalSolutionScopeTests", "LocalModes_DoNotWidenToSupportingProjects", "modeName=ProjectTransitive ONLY; Direct unchanged", 104, "Reachable PE-only supporting target outside original local source scope"),
            ("ExceptionFlowHistoricalSolutionScopeTests", "MissingReachableSourceProject_DoesNotReturnACompleteSmallerScope", "Fact, missing C source ONLY; full graph baseline unchanged", 188, "C PE retained but reachable source receipt absent; no smaller exact graph"),
        };
        foreach (var f in failureCases)
        {
            string name = f.Class + "." + f.Method; var test = tests.Single(t => S(t, "Test") == name);
            string relative = S(test, "File"); string[] lines = File.ReadAllLines(Path.Combine(tool, relative));
            var match = A(test, "Assertions").FirstOrDefault(a => a.GetProperty("Line").GetInt32() == f.Line && S(a, "Expression").Contains("AnalysisFailure", StringComparison.Ordinal));
            string current = match.ValueKind == JsonValueKind.Undefined ? lines[f.Line - 1].Trim() : S(match, "Expression");
            changes.Add(new("R10", name + " [" + f.Case + "]", relative, [f.Case], ["Historical selected Summary/profile query ONLY"],
                [new { File = relative, Line = f.Line, Current = current }], ["TargetOrAnalysisCoverageIncomplete: " + f.Witness],
                "ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.", false));
        }
        var manual = new List<object>();
        foreach (var c in cases.Where(c => c.GetProperty("CapturedCalls").GetInt32() == 0))
        {
            string name = S(c, "Test"); string[] parameters = A(c, "Parameters").Select(p => p.ToString()).ToArray();
            string mode = parameters.FirstOrDefault() ?? "SolutionTransitive";
            bool noSummary = mode is "Direct" or "ProjectTransitiveDeclaredExceptions";
            bool modes = name.Contains("AnalysisModes_", StringComparison.Ordinal);
            bool fallback = Regex.IsMatch(name, "Invalid|Disabled|WrongHash");
            string[] classification = noSummary ? ["unrelated"] : modes ? ["EXACT / admitted", "OUTSIDE PROFILE"]
                : fallback ? ["OUTSIDE PROFILE"] : name.Contains("ControlledFindingDiff", StringComparison.Ordinal) ? ["EXACT / admitted", "OUTSIDE PROFILE"] : ["EXACT / admitted"];
            manual.Add(new { Case = S(c, "Id"), File = S(c, "File"), Parameters = parameters, Classification = classification,
                Entry = "Existing Find -> FindExceptionSmells -> configured mode -> Session.Analyze; static source/helper reconstruction, NOT original test execution",
                Source = "Consumer.M -> External.Api.A; actual Prepare source is closed nongeneric A { throw new InvalidOperationException(); }; P6B variant uses its original wrong ArgumentException documentation, not as Proven flow",
                Reason = noSummary ? "Direct or original Declared guard: no summary; mode/configuration assertions unaffected"
                    : "Original validated source registration/acquisition success provides unique closed owner and complete two-body finite transfer; absent/invalid/disabled registration leaves PE-only target and TargetOrAnalysisCoverageIncomplete. Admission must try the existing authorized resolver before deciding source unavailable. No substituted source receipt or extra acquisition permitted.",
                AssertionChanges = 0, Assertions = Assertions(assertions.Where(a => S(a, "Case") == S(c, "Id"))) });
        }
        // These external execution tests use the same sources but are outside the semantic-test directory.
        foreach (var test in tests.Where(t => S(t, "Test").StartsWith("ExternalSupportingSourceReconstructionTests.AnalysisModes_", StringComparison.Ordinal)
            || S(t, "Test") == "ExternalSupportingSourceReconstructionTests.ControlledFindingDiff_ValidPlanAloneAddsPreciseSourceFlow"
            || S(t, "Test") == "ExternalSupportingSourceRegistrationTests.Registration_IsFindingNeutralAndDoesNotActivateTraversal"))
            manual.Add(new { Test = S(test, "Test"), File = S(test, "File"), InlineCases = A(test, "InlineCases"),
                Classification = S(test, "Test").Contains("Registration_IsFindingNeutral", StringComparison.Ordinal) ? new[] { "unrelated" } : new[] { "EXACT / admitted", "OUTSIDE PROFILE", "unrelated" },
                Reason = "Original CreateCallingConsumer/Prepare finite External.Api.A throw source; valid reconstruction admitted, missing/invalid outside, guard/Direct no summary. Registration-neutral test has no documented executable root and keeps empty before/after findings. Source/PE/checksum/ordinal/reference assertions unchanged.", AssertionChanges = 0 });
        var workerFamilies = A(workers.RootElement, "Rows").Select(r =>
        {
            JsonElement native = r.GetProperty("Native");
            JsonElement flow = native.TryGetProperty("summary", out var summary) ? summary : native.GetProperty("result");
            var summaries = A(native, "summaries").Select(s => JsonDocument.Parse(s.GetString()!).RootElement.Clone()).ToArray();
            bool outside = A(flow, "uncertainties").Length > 0 || summaries.Any(s => s.TryGetProperty("hasExecutableBody", out var body) && !body.GetBoolean());
            return new { Family = S(r, "Family"), Scenario = S(r, "Scenario"), Classification = outside ? "OUTSIDE PROFILE" : "EXACT / admitted",
                Reason = outside ? "Actual GC.Collect missing-body/uncertainty witness (TargetOrAnalysisCoverageIncomplete)"
                    : "Actual closed nongeneric reachable root/callees, finite two/three-body graph, no missing body, no uncertainty; no open/growing context. Unused generic exception declaration is not a reachable generic invocation.",
                OriginalSources = new { Root = S(r, "Source"), Additional = A(r, "AdditionalSources") }, ReferenceIdentities = A(r, "References"),
                GraphCount = native.GetProperty("graphCount").GetInt32(), Witness = flow };
        }).ToArray();
        var inherited202 = A(old.RootElement, "SkippedFixtures").Select(f => S(f, "Test")).ToHashSet(StringComparer.Ordinal);
        if (inherited202.Count != 202 || inherited202.Any(t => !cases.Any(c => S(c, "Test") == t)) || A(fixtures.RootElement, "Unresolved").Length != 0)
            throw new InvalidOperationException("Incomplete original fixture reconstruction.");
        var caseManifest = cases.Select(c => new { Fixture = c, Calls = calls.Where(call => S(call, "Case") == S(c, "Id")).ToArray(), Classification = rows.Where(r => S(r, "Case") == S(c, "Id")).ToArray(),
            Assertions = Assertions(assertions.Where(a => S(a, "Case") == S(c, "Id"))),
            AssertionChanges = changes.Where(ch => ch.Case == S(c, "Id")).ToArray(),
            ClosedByStaticExternalReview = c.GetProperty("CapturedCalls").GetInt32() == 0 }).ToArray();
        var workerReview = tests.Where(t => S(t, "File").Contains("\\Worker\\", StringComparison.Ordinal)).Select(t => new {
            Test = S(t, "Test"), File = S(t, "File"), InlineCases = A(t, "InlineCases"), MemberData = A(t, "MemberData"),
            Entries = A(t, "Entries"), Helpers = A(t, "Helpers"), SourceLiterals = A(t, "SourceLiterals"),
            EntryLayers = A(t, "Entries"),
            AssertionPolicy = "Exact finite positive source/finding/Current-Historical parity assertions preserved. Invalid input, attestation/replay, transport/process, selector, compiler/version, PID/isolation are independent boundaries. Only named R1-R7/prior grants and R10 listed code assertions may change after approval. No partial output/fallback or loosened lifetime check.",
            Assertions = A(t, "Assertions"), AdditionalChanges = changes.Where(ch => Method(ch.Case).StartsWith(S(t, "Test"), StringComparison.Ordinal)).ToArray()
            , Cases = WorkerCaseAssertions(tool, t, changes, workerFamilies.Select(f => JsonSerializer.SerializeToElement(f)).ToArray())
        }).ToArray();
        var ambiguous = workerReview.SelectMany(review => review.Cases.Select(c => new { review.Test, review.File, Value = JsonSerializer.SerializeToElement(c) }))
            .Where(c => A(c.Value, "Assertions").Any(a => S(a, "Classification") == "AMBIGUOUS")).ToArray();
        File.WriteAllText(output, JsonSerializer.Serialize(new {
            Kind = "Completed source/fixture/assertion preflight, NOT an implemented A9 admission certificate or implementation validation",
            Revision = 4,
            Scope = "All 202 exclusions reconstructed; 437 semantic/helper methods plus one primitive-only original fixture and all 237 Worker methods reviewed; repository-wide attributed-test/helper index checked for other production seams",
            OriginalExcludedMethods = 202, OriginalExcludedCases = cases.Count(c => inherited202.Contains(S(c, "Test"))),
            OriginalExcludedCalls = calls.Count(c => inherited202.Contains(S(c, "Test"))), UnreconstructedOriginalFixtures = 0,
            SourceInventoryFiles = A(index.RootElement, "Files").Length, SourceInventoryTestMethods = tests.Length,
            ExpandedMethods = cases.Select(c => S(c, "Test")).Distinct().Count(), ExpandedCases = cases.Length, ExpandedAnalysisCalls = calls.Length, UnresolvedExtractionExpressions = 0,
            NativeWorkerFixtureFamilies = workerFamilies, SemanticCases = caseManifest, StaticExternalCases = manual,
            WorkerMethods = workerReview,
            OtherTestLayers = tests.Where(t => !S(t, "File").Contains("\\Worker\\", StringComparison.Ordinal) && !cases.Any(c => S(c, "Test") == S(t, "Test")))
                .Select(t => new { Test = S(t, "Test"), File = S(t, "File"), Entries = A(t, "Entries"),
                    Classification = OtherClasses(t), Reason = "Source/helper call-path review; raw graph/primitive/DTO compiler assertions stay diagnostic, parser/report serializer assertions do not execute certified analysis; external production and CLI cases are separately described below. Not a claim of unit execution.",
                    Assertions = A(t, "Assertions") }),
            Changes = changes, R1ThroughR6Approved = true, R7Approved = true, NewApprovalPending = true,
            ExactScopeRules = new[] {
                "Diagnostic/raw graphs remain below admission; no new facade or production adoption",
                "Current router Succeeded is execution success, explicitly not exact completeness: preserve the three Current uncertainty tests; attach classified quality and block normal finding consumers, never reinterpret Succeeded as exact",
                "Native negative path assertions do not assert complete success; preserve them and independently require certificate quality. Positive ordinary escaping paths from rejected Session requests are R9, recursive counterparts unchanged",
                "Both path-cap tests already assert incomplete truncation, not complete success: keep 64 paths and ArePathsTruncated assertions as internal diagnostic data after admitted finite graph evaluation; the resulting incomplete projection cannot enter normal finding/Worker success. No R9 permission requested for them",
                "XML-only external documentation evidence is not Proven: preserve its independent metadata evidence and uncertainty tests; no normal finding receipt",
                "DOC631-only tests never acquire permission to remove coincidentally observed DOC611/DOC632; catch-all diagnostic suppression remains independent of full graph admission",
                "B11-B15 finite source/reference/parse/order/scope/solution factories: source calls/throw values are closed, finite, source-owned; missing/invalid owner/reference/parse cases fail their original earlier boundary; only missing reachable source/local-Project code R10 changes",
                "B23 CLI positives and B24 four CLI mode/empty combinations are finite: complete flow/findings/exit metrics unchanged. Worker-error message must retain incomplete meaning; no fallback/report on failure",
                "ProductionCurrentParity all eight modes remain finite; preserve byte-stable finding expectation, existing guard and caller-owned graph reuse",
                "A5 raw SCC/control cases (16 CurrentCases), eight WorkerCases and all other A5 assertions preserved; no second recursion/cache engine",
                "SelfAnalysis test asserts no crash, report existence and ordinary non-exception findings exit, not fixed exception-flow counts: unchanged. Comparison timing tests only inspect timing/repetition and use finite sample throw: unchanged",
                "Original invalid GuardedDictionaryValues fixture (CS0136) retained as invalid input; no repair or skip, classified outside, original absence assertions unchanged",
                "No product/test files, historical evidence, index, stashes, versions, routing or B25B changed"
            }, ProductionOrTestChanges = 0, ExistingTestsExecuted = false, HistoricalWorkerExecuted = false,
            RemainingUnclassifiedOriginalFixtures = 0, Original202ReconstructionCompleted = true,
            FullFixturePreflightCompleted = false, DefinitiveExtendedAssertionMappingCompleted = false,
            SupplementalAmbiguousMethods = ambiguous.Select(c => c.Test).Distinct().Count(), SupplementalAmbiguousParameterCases = ambiguous.Length,
            SupplementalAmbiguousAssertionObservations = ambiguous.Sum(c => A(c.Value, "Assertions").Count(a => S(a, "Classification") == "AMBIGUOUS")),
            ImplementationClearance = false, AmbiguityReason = "Source/helper inventory is broader than the 202 reconstructed fixtures. Uniquely binding each supplemental assertion to its concrete selected/mutated request and actual scope is not proved by that catalog. Do not infer classifications from test names or grant changes from these rows."
        }, new JsonSerializerOptions { WriteIndented = true }));
        WriteBatch(batchPath, changes);
        var ambiguousDoc = new StringBuilder("# Supplementary Worker assertion-binding ambiguities\n\nThese are source-inventory binding gaps, NOT proved semantic conflicts and NOT test-change permissions. Original 202 methods / 203 cases / 228 calls are reconstructed. No production clearance or definitive full-preflight closure is claimed. This exact list belongs to the same consolidated batch; do not turn it into serial implementation STOPs.\n\n");
        foreach (var group in ambiguous.GroupBy(c => c.Test))
        {
            ambiguousDoc.AppendLine($"## `{group.Key}`\n\nFile: `{group.First().File.Replace('\\','/')}`\n");
            foreach (var c in group)
            {
                ambiguousDoc.AppendLine($"Case `{S(c.Value, "Case")}`, parameters: {string.Join(", ", A(c.Value,"Parameters").Select(p => p.ToString()))}\n");
                foreach (var a in A(c.Value, "Assertions").Where(a => S(a,"Classification") == "AMBIGUOUS").GroupBy(a => S(a,"File") + ":" + a.GetProperty("Line").ToString()).Select(g => g.First()))
                    ambiguousDoc.AppendLine($"- `{S(a,"File").Replace('\\','/')}:{a.GetProperty("Line")}`: `{Regex.Replace(S(a,"Current"), @"\s+", " ").Replace("`","'")}`\n");
            }
        }
        File.WriteAllText(Path.Combine(root, "Supplementary-assertion-binding-ambiguities.md"), ambiguousDoc.ToString());
        File.AppendAllText(batchPath, $"\n## E — supplementary AMBIGUOUS bindings, no change permission\n\n{ambiguous.Select(c=>c.Test).Distinct().Count()} Worker methods / {ambiguous.Length} parameter cases retain source-inventory assertion-to-request/scope binding gaps. These are NOT additional proved conflicts. The exact complete list (file, method, parameters and assertion) is [Supplementary-assertion-binding-ambiguities.md](Supplementary-assertion-binding-ambiguities.md). R8-R10 are the 73 evidenced conditional change rows above; approval of those rows alone must NOT be treated as definitive full-preflight closure or clearance to implement while these supplemental bindings remain unproved. No test name is used as a membership proof.\n");
        Console.WriteLine($"Closed 202/202 exclusions; 203 original cases / 228 original calls; expanded {cases.Length} cases/{calls.Length} calls; {workerReview.Length} Worker methods; R8={changes.Count(c => c.Group == "R8")}, R9={changes.Count(c => c.Group == "R9")}, R10={changes.Count(c => c.Group == "R10")}. {output}");
    }
    private static object[] WorkerCaseAssertions(string tool, JsonElement test, List<Change> changes, JsonElement[] nativeFamilies)
    {
        string name = S(test, "Test");
        var inline = A(test, "InlineCases").Select(c => c.EnumerateArray().Select(p => p.ToString()).ToArray()).ToArray();
        string[] scenarios = ["self", "mutual", "three", "outgoing", "incoming", "repeat", "independent", "acyclic"];
        if (name.EndsWith(".GenuineWorker_UsesIndependentlyProvenFiniteClosure", StringComparison.Ordinal)) inline = scenarios.Select(s => new[] { s }).ToArray();
        if (name.EndsWith(".Current_ClosesFiniteRecursiveDependencies", StringComparison.Ordinal)) inline = scenarios.SelectMany(s => new[] { new[] { s, "false" }, new[] { s, "true" } }).ToArray();
        if (inline.Length == 0) inline = [[]];
        var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(tool, S(test, "File")))).GetRoot();
        var method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == name[(name.LastIndexOf('.') + 1)..]);
        var owner = method.Ancestors().OfType<ClassDeclarationSyntax>().First();
        bool productive = Product(test);
        bool raw = !productive && (A(test, "Helpers").Any(h => h.GetString()!.StartsWith("ExceptionFlowSummaryGraphTestHelper.", StringComparison.Ordinal))
            || A(test, "Entries").Any(e => e.GetString()!.Contains("TryBuildTransitiveSummaryGraph", StringComparison.Ordinal)));
        return inline.Select((parameters, index) =>
        {
            var env = new Dictionary<string, string>(StringComparer.Ordinal);
            string? Value(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax? expression)
            {
                if (expression is LiteralExpressionSyntax literal) return literal.Token.Value?.ToString();
                if (expression is IdentifierNameSyntax id) return env.GetValueOrDefault(id.Identifier.ValueText);
                if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression) && Value(binary.Left) is string l && Value(binary.Right) is string r) return l + r;
                return null;
            }
            foreach (var field in owner.Members.OfType<FieldDeclarationSyntax>()) foreach (var v in field.Declaration.Variables)
                if (Value(v.Initializer?.Value) is string text) env[v.Identifier.ValueText] = text;
            for (int i = 0; i < parameters.Length && i < method.ParameterList.Parameters.Count; i++)
                env[method.ParameterList.Parameters[i].Identifier.ValueText] = Value(SyntaxFactory.ParseExpression(parameters[i])) ?? parameters[i];
            foreach (var local in method.DescendantNodes().OfType<VariableDeclaratorSyntax>()) if (Value(local.Initializer?.Value) is string text) env[local.Identifier.ValueText] = text;
            var selected = new List<JsonElement>(); var sourceInputs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var call in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string called = call.Expression switch { IdentifierNameSyntax id => id.Identifier.ValueText, MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText, _ => "" };
                var arguments = call.ArgumentList.Arguments;
                if (called == "Payload" && arguments.Count > 0 && Value(arguments[0].Expression) is string selector)
                {
                    var family = nativeFamilies.Where(f => S(f, "Family") == owner.Identifier.ValueText && S(f, "Scenario") == selector).ToArray();
                    if (family.Length == 0)
                        family = nativeFamilies.Where(f => S(f, "Scenario") == selector && A(test, "Helpers").Any(h => h.GetString() == S(f, "Family") + ".Payload")).ToArray();
                    selected.AddRange(family);
                }
                foreach (var argument in arguments) if (Value(argument.Expression) is string source && source.Contains("class ", StringComparison.Ordinal) && source.Contains("Root", StringComparison.Ordinal)) sourceInputs.Add(source);
                if (arguments.Count == 0)
                {
                    var helper = owner.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == called && m.ParameterList.Parameters.Count > 0);
                    if (Value(helper?.ParameterList.Parameters[0].Default?.Value) is string source && source.Contains("class ", StringComparison.Ordinal) && source.Contains("Root", StringComparison.Ordinal)) sourceInputs.Add(source);
                }
            }
            bool sourceMutated = method.DescendantNodes().OfType<WithExpressionSyntax>().Any();
            string[] proved = selected.Select(s => S(s, "Classification")).Concat(sourceInputs.Select(SmallSourceClassification).Where(s => s != "AMBIGUOUS")).Distinct().ToArray();
            bool allSourcesProved = sourceInputs.All(s => SmallSourceClassification(s) != "AMBIGUOUS");
            string bound = !sourceMutated && allSourcesProved && proved.Length == 1 ? proved[0] : "AMBIGUOUS";
            return (object)new { Case = name + "#" + index, Parameters = parameters,
            ActualFixtureLinks = selected.Select(s => new { Family = S(s, "Family"), Scenario = S(s, "Scenario"), Classification = S(s, "Classification") }).ToArray(),
            ActualSourceArguments = sourceInputs.ToArray(), SourceMutatedAfterFactory = sourceMutated,
            Assertions = A(test, "Assertions").Select(a =>
            {
                string expression = S(a, "Expression"); int line = a.GetProperty("Line").GetInt32();
                string category = "unrelated";
                string basis = "Original transport/input/receipt/selection/provenance/owned-process or source-architecture assertion, not an ordinary exact finding/flow admission";
                bool flow = Regex.IsMatch(expression, "Result|result|Entries|entries|Thrown|Paths|paths|Findings|findings|Flow|flow|Summary|summary|graphCount|GraphCount|summaries|GetRequiredType");
                bool independent = Regex.IsMatch(expression, "ProcessId|ProcessExit|HistoricalLoaded|CurrentAnalysis|Selection|Provenance|execution attestation|MalformedResponse|UnsupportedInput|CompilationFailure|RoutingFailure|ReferenceWorld|PayloadProjection|TryGetFindingFacts|TryGetDeclaredPolicyFacts|DocumentationExecution");
                if (raw) { category = "diagnostic / low-level"; basis = "Existing raw graph/compiler/primitive/evaluator contract below production admission; prior exact named characterization permissions remain separate"; }
                else if (flow && !independent)
                {
                    category = productive ? bound : "diagnostic / low-level";
                    basis = category == "AMBIGUOUS" ? "Supplementary inventory: actual entry is productive, but the precise assertion-to-selected/mutated payload or scope branch is not uniquely bound here. NO inference from test name and NO assertion-change permission. Explicit clarification item; no production clearance from this row."
                        : "Actual source argument / linked original in-memory fixture observation, not test-name inference. This does not certify a different scope, forged response or a mutated payload.";
                }
                var change = changes.FirstOrDefault(c => c.Group == "R10" && c.Case.StartsWith(name + " [", StringComparison.Ordinal)
                    && c.Assertions.Any(item => JsonSerializer.SerializeToElement(item).GetProperty("Line").GetInt32() == line));
                if (change != null) { category = "OUTSIDE PROFILE"; basis = string.Join("; ", change.Reasons) + " — ONLY the named R10 parameter selector, not all cases of this method"; }
                return new { File = S(a, "File"), Line = line, Current = expression, Classification = category, Reason = basis,
                    ChangePermission = change == null ? "Preserve unless covered by existing exact prior/R1-R7 grant" : "R10 pending: " + change.Parameters[0],
                    ParameterReachability = "All source assertions retained with original branch predicates; parameter selector on a change is normative, never a global method/helper grant" };
            }).ToArray() };
        }).ToArray();
    }
    private static string SmallSourceClassification(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source); var syntax = tree.GetRoot();
        if (tree.GetDiagnostics().Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)) return "AMBIGUOUS";
        var methods = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        if (!methods.Any(m => m.Identifier.ValueText == "Root") || syntax.DescendantNodes().OfType<TypeParameterListSyntax>().Any()) return "AMBIGUOUS";
        if (syntax.DescendantNodes().OfType<AwaitExpressionSyntax>().Any()) return "AMBIGUOUS"; // Current/Historical capabilities require their actual adapter.
        if (syntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => i.Expression.ToString() == "System.Console.WriteLine")) return "OUTSIDE PROFILE";
        if (syntax.DescendantNodes().Any(n => n is ForEachStatementSyntax or UsingStatementSyntax or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or PropertyDeclarationSyntax)) return "AMBIGUOUS";
        if (methods.Any(m => !m.Modifiers.Any(SyntaxKind.StaticKeyword))) return "AMBIGUOUS";
        if (syntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => i.Expression is not IdentifierNameSyntax id || !methods.Any(m => m.Identifier.ValueText == id.Identifier.ValueText))) return "AMBIGUOUS";
        // Restrict the independent simple source proof to actual null throws / empty bodies / local-static calls.
        if (syntax.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Any() || syntax.DescendantNodes().OfType<ThrowStatementSyntax>().Any(t => t.Expression is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })) return "AMBIGUOUS";
        return "EXACT / admitted";
    }
    private static string[] OtherClasses(JsonElement test)
    {
        string name = S(test, "Test");
        if (name.StartsWith("ExternalSupportingSourceReconstructionTests.", StringComparison.Ordinal) || name.StartsWith("ExternalSupportingSourceRegistrationTests.", StringComparison.Ordinal))
            return ["diagnostic / low-level (scope/registration/raw graph)", "EXACT / admitted (valid requested source)", "OUTSIDE PROFILE (missing source)", "unrelated (no root/guard)"];
        if (name.StartsWith("SolutionTransitiveSelfAnalysisTests.", StringComparison.Ordinal)) return ["unrelated (crash/report/overall exit assertions, not exact per-root flow)"];
        if (name.StartsWith("ToolRunnerComparisonTimingRegressionTests.", StringComparison.Ordinal)) return ["EXACT / admitted (finite sample)", "unrelated (timing assertions)"];
        return Product(test) ? ["diagnostic / low-level (helper overload/direct/syntax observation; no ordinary certified finding assertion)"] : ["diagnostic / low-level", "unrelated"];
    }
    private static void WriteBatch(string path, List<Change> changes)
    {
        var text = new StringBuilder("# A10 final consolidated fixture-preflight approval batch\n\nStatus: STOP BEFORE IMPLEMENTATION. R1-R7 APPROVED, not requested again.\n\n");
        text.AppendLine("All 202 original exclusions are reconstructed (203 parameter cases, 228 calls). Expanded source review: 438 semantic/helper/primitive methods, 464 cases, 527 calls (including the original primitive-only case); 340 current test/helper files and 2,194 attributed test methods indexed; all 237 Worker methods reviewed. The JSON manifest retains exact source inputs, references, helper paths, per-root witnesses and original assertions. Runtime observations are inherited Current analysis, NOT an implemented A9 certificate, a genuine Historical execution, or fresh unit gates.\n");
        text.AppendLine("These are requests, NOT permissions. Each row is conditional on a sound implemented A9-FiniteOrbit-v1 certificate for that exact input; if admitted, KEEP its assertion. No extra parameter cases, blanket helper weakening, raw graph facade/fallback, new wire DTO, compiler/routing/PID/isolation changes, or B25B. Historical B25-A9 evidence stays frozen. Only new A10 witnesses may record corrections.\n");
        foreach (string group in new[] { "R8", "R9", "R10" })
        {
            text.AppendLine($"## {group} ({changes.Count(c => c.Group == group)} exact rows)\n");
            text.AppendLine(group == "R8" ? "Normal finding/cardinality and dependent finding dereference/context assertions ONLY. Outside requests get classified incomplete output and no normal exact Finding; no partial Direct fallback.\n"
                : group == "R9" ? "Positive normal escaping-path projection from the existing productive SolutionTransitive helper ONLY, where unavailable target/body coverage rejects production graph registration. Its same test's recursive Project/Direct/raw observations are NOT authorized for change. Empty-path, uncertainty-only, XML-only evidence and the two already-incomplete path-truncation tests are NOT included. The latter retain diagnostic 64-path/truncation assertions but may not produce a complete normal result.\n"
                : "Only listed actual profile-rejection code expectations: AnalysisFailure -> existing UnsupportedContext plus deterministic profile/reason/context Details. All failure/null-result/no-receipt/no-fallback, compiler/transport, parity, PID and isolation assertions stay. This is NOT an enum/protocol change or a blanket rewrite of AnalysisFailure.\n");
            int index = 0;
            foreach (var c in changes.Where(c => c.Group == group))
            {
                text.AppendLine($"### {group}-{++index:D2}: `{c.Case}`\n");
                text.AppendLine($"- File: `{c.File.Replace('\\', '/')}`\n- Parameter case: {string.Join(", ", c.Parameters.Length == 0 ? ["existing Fact only"] : c.Parameters)}\n- Entry/modes: {string.Join(", ", c.Modes)}\n- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.\n- Witness: {string.Join("; ", c.Reasons)}\n- New semantics: {c.NewSemantics}\n");
                foreach (var a in c.Assertions)
                {
                    var row = JsonSerializer.SerializeToElement(a);
                    text.AppendLine($"Current line {row.GetProperty("Line").GetInt32()}:\n\n```csharp\n{S(row, "Current")}\n```\n");
                }
            }
        }
        text.AppendLine("## Explicit exclusions and preserved contracts\n\nR1-R6 keep their existing exact case lists. All 32 R7 methods are APPROVED conditionally and are marked approved in the manifest; no re-request. No DOC631-only assertion is inferred from incidental normal output. The two catch-all DOC631 absence tests, exact-coverage DOC631 test, XML-only evidence, invalid cref and non-exception diagnostics remain protected. The three Current uncertainty route tests assert execution success, not exact completeness; no need to edit them or reroute an engine. Positive finite Current/Historical finding families remain unchanged. Acquisition/reconstruction must use existing authorized source resolution before rejecting a missing body, preserving acquisition counts and scope/receipt checks. No self-analysis, Dual/Broad/Full/Boundary/WAE or test gate was run by this source audit.\n");
        text.AppendLine("The complete machine-readable assertion/case inventory is `Complete-fixture-preflight.json`. Approval of this ONE additional batch is requested before any production/test change. It cannot authorize falsely rejecting an exact fixture, silently deleting assertions, changing any unlisted assertion, or rewriting historical witness files.\n");
        File.WriteAllText(path, text.ToString());
    }
}
