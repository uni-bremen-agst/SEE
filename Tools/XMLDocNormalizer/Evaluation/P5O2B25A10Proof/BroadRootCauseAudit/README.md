# Broad root-cause partition:179 unique cases; STOP, no repair batch

This is an architecture/integration diagnosis of the ORIGINAL export13 Broad
attempt, not a new test result. Its3334 executed cases remain3155pass/179fail/
0skip. Closed R2 remains unchanged; prior isolated2/2 and targeted149/149 stand.

## Primary partition (sum179, no overlap or unassigned case)

| ID | Common cause | Cases | Test methods | Decision |
|---|---|---:|---:|---|
| H1 | Historical PDB emitter not executed in fresh export |45|28|DIAGNOSTIC/UNRELATED|
| H2 | BoundaryTestProcess missing:5 read failures +16 wrong-peer outcomes |21|9|DIAGNOSTIC/UNRELATED|
| A | New graph/result references expose high-level admission/Analyzer ownership |2|2|PRODUCTION FIX|
| B | Raw callable substitution assumes a unique containing-type member |1|1|PRODUCTION FIX, exact Broad trigger still unproved|
| C | Incomplete class recovery symbol canonicalized before eligibility |1|1|PRODUCTION FIX|
| M | Two manifest counts121 rather than intended125 |1|1|ALREADY APPROVED CHARACTERIZATION, applied|
| D | Positive normal Findings/flow after actual coverage-OUTSIDE |91|91|ALREADY APPROVED CHARACTERIZATION: R7=32/R8=32/R9=27|
| W | Genuine Worker returns classified UnsupportedContext before old AnalysisFailure |13|13|ALREADY APPROVED CHARACTERIZATION: R10|
| E | Summary external-documentation evidence moved to independent diagnostic side |2|2|NEW APPROVAL REQUIRED|
| N | Existing DOC631 diagnostic branch reached by new outside/unknown residual |1|1|NEW APPROVAL REQUIRED / explicit visibility decision|
| T | CLI worker-error diagnostic expects old incomplete wording |1|1|NEW APPROVAL REQUIRED|
| | **Total** |**179**| | |

Decision totals: DIAGNOSTIC/UNRELATED66; PRODUCTION FIX4; ALREADY APPROVED
CHARACTERIZATION105; NEW APPROVAL REQUIRED4. Cases, not methods, are disjoint;
a method can have different parameter cases in different groups. Secondary
dependencies are not counted as extra primary failures.

`partition.json` binds every full original xUnit case name, error/stack, primary
group, exact grant row, observed fixture source hash/mode and live certificate.
`groups.json` documents ALL nine requested dimensions for EVERY group: count
(computed in partition), representatives, first common location, A10 origin,
A9 contract relationship, production/characterization distinction, permission,
minimal proposed production fix, and cross-group effects.

## Corrections to the initial coarse inventory

The previous103 method-name matches were not the whole permission proof. This
audit verifies the91 R7/R8/R9 exact Fact#0 rows and their actual assertion texts
against unchanged source, plus coverage-OUTSIDE in each reconstructed fixture.
`assertion-bindings.json` additionally binds all104 reached approved failures to
actual stack/statement sequence points or the exact async InlineData producer
and unchanged consumer assertion. Variable-declaration sequence points one line
before Assert are resolved explicitly, not treated as new assertion conflicts.
Copied draft grant rows retain historical Approved flags; the later explicit
user R1-R10 grants, not those frozen draft flags, are the current authority.
The13 R10 cases match the exact reached typed-code expectations; parameter
restrictions remain in the saved grant rows. R10's14th row (async Dispatch case)
is blocked by H1 before its code assertion, hence secondary only.

One of the17 earlier process-harness candidates is actually
`HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess`
with the async source. It is a GENUINE Worker UnsupportedContext case in W,
not a missing peer. Thus setup-related failures are66, not67. H2 contains the
remaining16 candidates plus5 direct missing-peer reads. No PID flakiness was
proved. A peer scenario expecting NonZeroExit may even pass accidentally when
dotnet cannot find the peer DLL; such a pass is not genuine scenario evidence.

H1/H2 are my earlier preparation omission, not a new analyzer bug: building
HistoricalPdbFixture did not emit `historical-input.dll`, and the peer was never
built. The genuine prerequisites already exist in Verify-DualVersionBuild.ps1
(`boundary-test-process-build`, `projection-pdb-fixture-emit`). Neither fixture
was repaired or any test rerun in this read-only audit.

## Architecture A: concrete dependency paths and proposed ownership split

Read-only IL dependency extraction from the existing normatively unchanged test
code confirms these shortest paths in the actual compiled Main:

```
SummaryGraphBuilder -> SummaryGraph -> FiniteOrbitAdmission -> Analyzer
CatchSemantics -> AnalysisResult -> FiniteOrbitAdmission -> Analyzer
ContextualFactEvaluator -> DelegateTargetResolver -> CatchSemantics
  -> AnalysisResult -> FiniteOrbitAdmission -> Analyzer
```

The new graph field/BeginProduction parameter uses the concrete discovery
owner; its InspectDeclaration invokes Analyzer. The neutral result's new
ProfileDiagnostics type is nested under that same discovery owner. Nested-type
ownership is deliberately included by the IL guard. These are genuine new A10
dependencies, not mere test fingerprints or harmless object identity changes.

Proposed minimal split, **not implemented**:

1. Keep executable-body discovery/orchestration at Session/admission-discovery
   level. It may invoke Analyzer using the existing definition-only sink.
2. After successful discovery and profile checking, hand the graph immutable
   sealed INVENTORY/TRANSITION/SCOPE proof data with a pure domain validator.
   The graph must not hold the discovery object, Analyzer callback, delegate,
   lazy supplier, or an interface whose runtime implementation calls Analyzer.
3. Move independent diagnostic fact data out of the discovery owner's nested
   type into the neutral domain; no executable analysis in the result DTO.
4. Preserve issuance authority and the exact scope/root/map seal: a raw graph
   or a Complete boolean alone cannot mint a production proof. Session must
   still acquire it from fresh checked discovery, guard P1 and every P2 before
   registration/enqueue, invalidate changed worlds and roll back incomplete
   transactions. It must not accept caller-supplied or stale proof data.
5. Keep A5 cache/SCC semantics and Current/Historical shared-source equivalence.
   No Wire DTO, new diagnostic facade, version, routing, attestation or B25B.

This makes discovery/proof-data ownership explicit and therefore requires the
requested separate boundary/design decision before implementation. The exact
construction/mint API still needs design review; do NOT camouflage a backlink
behind an interface/delegate just to pass reflection. The existing dependency
guards remain normative, including their signature/IL/delegate traversal.

## Rebinding B: proven lookup issue versus unproved Broad precondition

The original Broad stack is retained: raw graph -> RegisterTarget ->
InvocationSubstitution.ApplyCallable line96, failed unique-member lookup.
The newly added composition runs on the raw graph as well as production; this
is not an A9 admission check and must not be fixed by changing the raw test to
OUTSIDE or dropping receiver facts.

Controlled symbol observation proves:

- reduced extension handle:0 containing-type OriginalDefinition matches,
  ApplyCallable throws the SAME uniqueness error;
- exact unreduced and source-declaration handles:1 match each, both succeed;
- source and unreduced full canonical identities are equal, including assembly/
  module/owner. This fixture has0 method args,0 containing args and no nested
  owner. It cannot prove a generic-argument loss; previous generic tests are not
  replaced by this observation.

Important limitation: the fresh EXISTING graph helper succeeds standalone,
and the independent reconstructed raw graph also succeeds (3nodes). Therefore
the actual Broad scheduling/reference-world state that sent an unacceptable
handle to lookup has NOT been isolated. No cache/race/flakiness claim. The
controlled reduced failure demonstrates a shape-sensitive hole, not a captured
actual Broad variable value. Do not call this complete live reproduction.

Minimal candidate to investigate before any patch: compose reduced/unreduced
selection with the exact destination source declaration and full constructed
identity; retain receiver-to-static-parameter facts and method/containing/nested
coordinates, require unique canonical roundtrip and correct compilation owner.
No name-only/MetadataName/OriginalDefinition-only fallback and no definition
folding. Root cause assignment is complete at the failed lookup invariant;
its precise Broad trigger remains an explicit investigation limit.

## Incomplete syntax C: recovery symbols are not complete semantic roots

The exact incomplete `class` fixture has CS1001/CS1514/CS1513 and a NON-NULL
Roslyn recovery symbol whose Name and MetadataName are empty; identifier is
missing. Discover currently accepts non-null GetDeclaredSymbol and calls
AddDefinition/BodyId before canonical root eligibility. CreateTypeDeclarationIdentity
then throws from CanonicalCallableIdentity, before a classified result exists.
The read-only observer reproduces the full stack.

Proposal only: validate root kind/syntax/binding eligibility before BodyId.
Represent invalid input through its raw finite path/span/kind/error witness in
an incomplete certificate, not a fabricated valid callable or exact empty set.
Preserve no-Finding/lazy-anchor behavior and existing uncertainty; do not swallow
all exceptions, skip assertions, or weaken normal complete-compilation checks.

## Characterization D/W versus new E/N/T

The observer reconstructs133 calls for94 semantically affected methods using
the actual export13 helpers/engine and frozen fixture source/reference data;
no test method or source program executes. Each method has an actual productive
coverage-OUTSIDE observation. No observation error. Comparing the same133 calls
against the inherited A9 binary shows29 Direct/Recursive native observations
unchanged (thrown/external/uncertain sets). Only productive summary behavior
is reclassified. All observed outside reasons are TargetOrAnalysisCoverageIncomplete,
not a fabricated growth classification for ordinary finite code.

D's91 changes are already granted, not requested again. W's13 real responses
actually report UnsupportedContext; no synthetic identity or envelope repair
is involved. A10 intentionally classifies unsupported bodies/await/missing scope
before the old generic AnalysisFailure branch. R2 stays closed.

E's two tests fail only at solutionRun: Recursive still has IO and
InvalidOperation documentation evidence; Summary has no normal evidence paths
but retains BOTH FULL CANONICAL type identities under independent ProfileDiagnostics,
unknown residual and NormalFindingAdmissionBlocked. Documentation never becomes
Proven exception flow. Existing normal path-step assertions are not silently
claimed preserved on that different side channel.

N's one root has parameters, so the intentionally bounded ground/parameterless
diagnostic subquery cannot certify its complete flow. Iterator/interface/disposal
coverage is missing; its certificate is outside, diagnostic lower set empty and
unknown residual true. The existing documented-type uncertainty branch emits
one DOC631, NOT a normal exact missing/extra-throw Finding. Baseline is empty.
This says nothing about a nullable OfType element: those lower facts remain
unchanged. The visible diagnostic expectation requires explicit approval; if
that visibility is rejected, decide it separately without restoring partial
exact output or changing OfType semantics.

T's worker-error source replaces Helper with GC.Collect. The classified failure
uses the existing `Exception-flow request is outside A9-FiniteOrbit-v1.` message;
the CLI preserves failure formatting. R10 grants expected codes, not this
additional `incomplete` substring assertion.

The SINGLE `Broad-Approval-Batch.md` names ONLY these4 additional Facts/cases,
including exact external-evidence path assertions and conditional diagnostic
visibility. No edit under that batch is applied here.

## Edits, evidence quality and STOP

Only the two approved Manifest assertions121->125 changed in any existing
test/production source this turn. All other755 source/test files match export13
byte hashes (offline transformed NuGet config separately checked against Git).
The source set difference is exactly the four intended A10 files, no removals.
No production change; R2 and R5/K17/P12 are untouched. Manifest test was not
executed; only its count/set/file conditions and exact two-line diff were checked.

Observer-only development attempts are retained: initial missing direct
Immutable/Metadata references caused MSB3277/CS1705; a later local-name collision
caused CS0136; changed diagnostic sources subsequently build clean. Additional
builds add structural state and diagnostic-side fields, not green retries of
unit gates. Earlier observer outputs remain intact, including the first
structural attempt that incorrectly inspected the Session production graph
instead of the raw graph OUT parameter; it is NOT rebinding evidence. The
subsequent raw/helper observations and shape controls above supersede that
instrumentation error without rewriting it.

The capture script had two pre-write PS5 array/scalar binding errors and then
a witness/run-record basename collision. Staged identical witnesses were
hash-verified, not overwritten; final records put run metadata under Runs/.
These are diagnostic-tool development errors, not new product/test failures.
The separate assertion-binding capture initially stopped at the two async cases
because permission concerns InlineData, not the unchanged consuming Assert.Equal;
the corrected binder records both exact producer/consumer locations. No test run
or source expectation was altered to resolve these diagnostic binding errors.

Full source-level fixture/grant/certificate bindings, original failure stacks,
observer outputs and run records are in partition.json and sibling files.
The final protection/status record is `../implementation-broad-root-cause-state.json`.
No further closure gate, Broad retry, fixture repair, architecture/rebinding fix
or unapproved assertion change was performed. Historical B25-A9 and previous
A10 runtime snapshots remain unchanged. No new protection baseline or repair;
Core S1 history preserved. No commit/push/stash operation; B25B closed.

STOP after this complete179-case primary partition. A10 is not complete.
