# A10 Phase 0 — supplementary binding closure

Outcome: **85/85 methods, 429/429 parameter cases, 3,202/3,202 original
ambiguous assertion observations are source-bound. No additional R11 is
necessary.** This is static Phase-0 evidence, not an implemented admission
certificate, a runtime test pass, or permission to implement in this run.

The original 202-fixture reconstruction remains unchanged: 203 cases,
81 EXACT / 96 OUTSIDE / 1 DIAGNOSTIC / 25 UNRELATED / 0 ambiguous.

## Reproducible evidence and meaning of “bound”

- [Supplementary-resolved-bindings-r2.json](Supplementary-resolved-bindings-r2.json)
  preserves every original observation one-to-one, with test, actual parameter
  names/values, source file/line/span, exact assertion, owning method, original
  if/else predicates, concrete input/mode/result origins, A9 category, and
  change/permission decision. It links 313 exact source-method definitions,
  including locals, assignments, calls, class constants and MemberData sources.
- [Supplementary-method-reviews.txt](Supplementary-method-reviews.txt) contains
  **85 explicit source reviews**, including each selected source mutation,
  valid baseline, rejected variant, guard, scope and helper chain. Method names
  are lookup keys for these reviewed paths, not classification heuristics.
- [Resolve-Supplementary.csx](Resolve-Supplementary.csx) checks all 340 frozen
  test/helper file hashes and resolves each original assertion to exactly one
  Roslyn syntax invocation at its actual line. It never calls an existing
  test, fixture factory, Native helper, analyzer, worker, writer, or build.
  Only the new A10 evidence file is written. The installed VS `csi.exe` runs
  this syntax/evidence script without project builds or obj generation.
- Original source/reference bytes and prior in-memory observations remain in
  `Complete-fixture-preflight.json` / `worker-fixture-families-r1.json`.
  The old revision-4 any-`with` flag and regex classifications are **not** the
  new classification authority. A documentation-only `with` is not a different
  executable program; a forged response is not a source-program profile.

The old catalog is a **helper-closure overapproximation**, not 3,202 distinct
assertions executed on every InlineData branch. The refinement explicitly marks
**607 observations inactive for their parameter branch**; it does not delete,
skip, relax or alter any assertion. Data-dependent/helper-loop predicates remain
verbatim with their concrete evidence origins, rather than being asserted to
execute unconditionally. Their profile is determined by the actual body/scope,
not by whether a loop happens to have an item at runtime.

One closure observation can represent several actual helper calls. These are
kept separate, e.g. Native(local) versus Native(uncertain), or Native(full
Solution) versus Native(local Project). Consequently the **3,252 classified
origin bindings** (1,487 EXACT, 55 OUTSIDE, 102 DIAGNOSTIC, 1,608 UNRELATED,
including inactive branches) are **not** additional tests/cases and must not
be added to the 3,202 observation count. No binding has an AMBIGUOUS category.

## Actual entry/layer distinctions

Production Current:

`FindExceptionSmells` → `AnalyzeConfiguredExceptionFlow[Async]` → configured
dispatch/router → `request.Current.Session.Analyze(request.Current.Member)`.
The private Native helpers explicitly call `session.Analyze(root)` or the
actual configured detector seam. `CurrentResult` calls
`AnalyzeSolutionTransitivelyThrownExceptions` → `Session.Analyze`.
Creating a session or compiling an input alone is not executing that entry.

Production Historical:

`Original/ValidatedInput` → `HistoricalWorkerPayloadProjection.Project` (or
`ProjectForFindings`) → `AnalyzeExternalExceptionFlowAsync` /
`FindExternalExceptionSmellsAsync` → Dispatch → Router →
`HistoricalWorkerClient.AnalyzeAsync` → `HistoricalAnalysis.Analyze` → shared
`AnalyzeSolutionTransitivelyThrownExceptions` → `Session.Analyze` when a summary
is actually requested. DirectEvidence and a guarded-off summary are distinct.
Original/ValidatedInput handoff/emit assertions are receipt setup, not admission.

Auxiliary Historical graph observations are **not raw-graph fallbacks**:
`HistoricalPdbFixture.Program` `--b14-graph` → `B12ReferenceProbe.Run` →
`B14ScopeProbe.Observe`; `--b15-graph` → `B15SolutionProbe.Run` → actual Worker
`HistoricalAnalysis.Reconstruct` → `B14ScopeProbe.Observe` → reflected
`Session.Analyze`. Its Direct branch instead calls the direct analyzer.
`--b12/--b13` go through `B11GraphProbe.Observe` and the shared session.
These test-only probes observe production session behavior, not a new Main
production entry. Reading a graph dictionary and `TryCreateSummary` is low-level
representation inspection, not a certificate or finding admission.

Independent paths: selector/input/projection rejection, compiler errors before
analysis, fake pipe peers, deadlines/cancellation, response validation,
attestation/purpose/receipt checks, source ownership metadata, PID/lifetime and
Roslyn isolation. A genuine finite baseline used to construct an adversarial
response remains EXACT; the adversarial validation assertion remains UNRELATED.

The accepted raw contract remains unchanged:
`ExceptionFlowSummaryGraphTestHelper.Build` → `BuildMember<T>` →
`TryBuildTransitiveSummaryGraph`. It does not certify an exact production result.
No new facade, admission bypass, fallback or cache/finding promotion is added.
The 340-file hash check includes the unchanged constrained-type-parameter test.

## Decisive mixed cases (not whole-method guesses)

| Concrete origin | Classification / required treatment |
|---|---|
| B4/B5/Dispatch real await `Task.CompletedTask` input | OUTSIDE: missing RuntimeAwait information; R10 expected code only. Missing()/missing Root controls keep CompilationFailure/InvalidRequest. |
| A3 genuine growing Recur<T> → Recur<List<T>> or Api<T> → Api<List<T>> | OUTSIDE: positive constructor-weight self SCC; exact R2. Not a diagnostic helper. |
| A2 closed/constructed/two-constructed callees | EXACT finite closed int/string constructions; earlier P09/P10 exact corrections, no growth certificate. |
| A4/A5 finite mutual/self cycles, both edge orders | EXACT zero-weight/closed context SCC; retain Fault/Other correctness, absence of uncertainty, repeat/cache and genuine Worker parity. |
| B17 local Native then uncertain Native | Two bindings: finite EXACT; GC.Collect OUTSIDE. Malformed-request/inventory/receipt controls separate; final worker code R10 only. |
| B18/B19/B20 guard with external/no local declared type | Native unguarded Project may be OUTSIDE, actual Declared guard remains Direct/no graph/no normal summary. Unguarded Project worker code R10 only. |
| B22 `guard-direct` | first/repeat guarded Declared Direct DOC610 remains; only unguarded projectCall is OUTSIDE/R10. Native's unconditional Project analysis does not certify first.Findings. |
| B22 selected-summary failure | Direct ArgumentException facts stay; requested GC summary OUTSIDE; no final findings/partial fallback; R10 Summary code only. |
| B24 Direct versus Project/no-doc | All selected positive fixtures finite. Native helper's independent unconditional summary is not the selected reporting route. Direct/no-doc do not request admission. |
| B14 cross-project comparator | Project PE-only callee OUTSIDE; registered supporting source Solution EXACT. Existing inequality and positive solution paths unchanged. |
| B15 local modes | Full Solution EXACT; local Direct independent; local Project PE-only callee OUTSIDE. No scope widening; R10 Project worker code only. |
| B15 missing C source | Native(full) EXACT; separate missing request OUTSIDE, no complete smaller scope; R10 failure code only. |
| B12 dependency omitted | Current Native compilation-error program is not exact; repeat-equality assertions do not claim completeness. Worker still CompilationFailure before admission; preserve errors/details/no result. |
| B13 symbols/languages, B14 source order, B18 XML-only substitutions | Inspect actual active source/receipts: finite exact programs; documentation/reference order is not a growth cycle or missing callable. |
| B25 equal declaration names in distinct assemblies | Finite exact scope, actual thrown owner P1; prior P15 corrects only old P0 result owner. Per-node owners and B25B no-finding-admission remain. |

## Complete change/approval coverage

**All production-relevant characterization changes found in this complete
preflight are covered by R1–R10 or earlier explicit grants. No R11 is invented.**
This does not mean R8–R10 have been approved.

- R1–R7: already approved, conditional on the complete sound contract; never
  requested again. In the supplementary subset, the two A3 growing cases use R2.
- Earlier P09/P10: three A2 concrete containing-argument corrections; only
  two-constructed-callees splits the collapsed entry. Earlier P15: B25 evaluated
  owner P0 → actual P1. Their precise existing bounds remain normative.
- R8: **32 pending** normal-finding/cardinality rows, unchanged in the existing
  final batch. No additional positive finding conflict was found in the 85.
- R9: **27 pending** positive paths through the productive Solution helper,
  unchanged. No blanket conversion of raw helper tests.
- R10: **14 pending** exact Historical failure-code rows, unchanged. Every
  supplementary mixed source/mode/guard has been checked against its selector;
  other compiler/transport/error/detail/parity/no-result/lifetime assertions
  remain protected.
- R6/new A10-only witnesses: retain historical B25–A9 evidence; no existing
  writer/test was executed and no old evidence overwritten here.

Of the 3,202 previously ambiguous observations, **3,194 require no semantic
change**; eight map to an existing grant/request: four R2 observations, one
P10 observation, three R10 code observations. This count deliberately concerns
only the old ambiguous subset, **not all assertions** in those methods.
`CaseRecords.RequiredAssertionChanges` additionally indexes the non-ambiguous
existing P09/P15/R2 assertions: 18 assertion/case obligations in this subset
(10 R2, 3 P09, 1 P10, 1 P15, 3 R10). Remaining R10 obligations, including GC and
missing-scope cases whose code assertion was already mapped, stay exactly in
the existing 14-row final request. Nothing adjacent is implicitly approved.

## Validation actually performed

Static file/hash/AST binding checks and the separate byte-exact S1 drift
analysis only. No product/test build, unit test, Historical worker, full gate,
regression, Self Analysis, Canonical Finding Diff or gate retry was run.
The owned syntax tools had disclosed draft errors (ArrayList cast in the
inspector; nonexistent foreach syntax property, then struct/record owner lookup
in the resolver); these were corrected only in A10 proof scripts. Both completed
binding outputs are retained, r2 is authoritative; these are not test failures
or an attempt to retry a product gate to green.

Protection decision: [S1 content attribution](Protected-obj-drift-analysis.md),
actual process writer still unproved. Old protection exit 1 and historical
manifest remain unchanged, with two raw hash mismatches explicitly retained.

**STOP.** The Phase-0 audit/binding/drift tasks are complete, with R8–R10 finally
requested but pending. No A10 implementation in this run even if approved later.
B25B stays closed. No commit, push, index or stash operation.
