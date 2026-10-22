# P5O2B25A3 — Constructed generic summary invocation context

## Phase 1: trace and proposed contract, before production edits

Starting HEAD: c777f9642c145a57c112c28216c3996128c6234b. Only the protected
root ignore was dirty at the initial audit; semantic index clean, six stashes,
23 actual Core files unchanged. B25/B25A/B25A2 are committed history. No commit,
push, stash operation or original Core build is authorized. Builds use a fresh
disposable offline export. Existing report/audit/Proof bytes remain historical.

### Exact information-loss chain (Phase 1 items 1–15)

1. `ExceptionFlowAnalyzer.SummaryGraphCalls.AnalyzeSummaryInvocations` reads
   `SemanticModel.GetSymbolInfo(invocation)` (line 48). The selected IMethodSymbol
   contains actual method TypeArguments and the complete constructed containing
   chain; its OriginalDefinition instead contains the declared TypeParameters.
2. `CreateSummaryDirectInvocationTargetPlan` resolves source coverage and the
   original supporting source scope, retaining requested and analysis targets.
   Ordinary calls use `CreateInvocationCallContext`, then the contextual fact
   evaluator's `CreateCallContext`. Reduced extensions additionally map receiver
   to parameter zero; this is value-fact mapping, not generic substitution.
3. `ExceptionFlowCallContext` line 88 assigns NormalizeSymbol(callableSymbol).
   NormalizeSymbol at line 431 returns OriginalDefinition: both method and all
   containing-type constructions are discarded here. The context stores only
   ordinal value/member facts; its Key is that finite fact description.
4. `ExceptionFlowSummaryTargetRegistrar` registers local targets directly or
   rebinds a validated supporting target through RebindCallable. That method
   transfers ordinal parameter/member facts, not a generic substitution. The
   existing cross-compilation method resolver reconstructs a containing member
   but does not preserve constructed method arguments by calling Construct.
5. `ExceptionFlowCallableKey` line 38 independently stores OriginalDefinition.
   Equality/hash use that symbol plus the fact ContextKey. Different argument
   constructions with identical value facts therefore share one node. Canonical
   key/context projection again explicitly requests definition normalization.
6. The session work queue builds each registered key once, visiting declaration
   syntax with its owning source SemanticModel. This model correctly exposes T
   in an open generic body; using a constructed caller symbol alone cannot make
   GetTypeInfo on that declaration report a closed type.
7. Summary throw collection takes the semantic/operation type directly. A named
   Fault<T> is stored as such. A thrown type parameter uses exception constraints
   plus explicit uncertainty, not a T-to-concrete mapping. No general body-type
   substitution engine was found repository-wide. Catch symbols and subsequent
   callees likewise require contextual projection, not just a longer cache key.
8. The later evaluator declaration-ID normalizer and definition-only catch
   filter retain the independent B25/B25A downstream defects. A3 must not guess
   arguments there or expand into Finding activation. Canonical result export
   faithfully reflects the symbols it receives; it cannot recreate missing data.
9. Current and Historical compile this same source set. The operation is compiler
   local, not a reason to add Roslyn objects or a new DTO to the Worker wire
   contract. Canonical graph/context evidence does cross the neutral seam.

### Existing reusable identity/construction infrastructure

`RoslynCanonicalIdentityFactory.CreateCallableIdentity(method, false)` already
represents constructed method arguments, complete containing identities and
owner assembly/module; definition-normalized calls intentionally omit them.
Canonical types represent nested containing chains and recursive arguments.
The resolver and CrossCompilationResolver use real Roslyn Construct/member
resolution for particular rebinding tasks. They are not a general substitution
of arbitrary body types under an invocation's type-parameter environment.
No SymbolKey/flattened-name/new global identity DTO is justified.

The minimal intended compiler-local contract is a validated source definition
plus exact method and complete containing-chain substitutions, keyed by scoped
type-parameter identity/ordinal and retaining actual owner symbols. Compose it
at the call site and across the already validated supporting-source boundary;
apply it before collecting throw/catch types and before creating later callee
contexts. Use the existing canonical constructed callable/type representation
for neutral identity. Missing, contradictory or unavailable mappings require
explicit failure/uncertainty, never a guessed namesake, object or silent open T.
Value facts continue to be scoped by the definition's parameter ordinals.

### Cache, recursion and scope checks

The graph dictionaries, pending queue, evaluator activeKeys and completedFrames
all use ExceptionFlowCallableKey. BuildPendingSummaryNodes drains the entire
queue before evaluation; no generic context/node/depth budget or SCC/fixpoint
construction policy exists in this orchestration. The evaluator skips active
keys only after graph construction. Preserving ever-growing generic arguments
removes the accidental finite bound supplied by definition folding.

Before selecting a production correction, test real valid method and containing
recursion with changing type arguments, finite repeated contexts, nested cases,
and the actual cross-project resolver. Any independent bounded-recursion policy
boundary triggers the requested Stop rule; do not install an arbitrary limit.

Permission is exactly the two A2 cases in
CompilerModel_ObserveActualSummarySourcesBeforeNormalization. closed-source,
all other assertions and historical witnesses remain unchanged unless their
already explicit permission applies. New witnesses are written only under A3.

## Executable result and Stop decision

The initial A3 model tests pass 17/17, including both genuine Historical Worker
cases. All source fixtures compile as real C#12 programs without errors. The
method and containing recursion fixtures use an unknown mutable bool field:
`if (Flags.Stop) throw new Fault<T>(); Recur<List<T>>();` (or the corresponding
`Api<List<T>>.Recur()`). This is neither invalid generic exception syntax nor a
recursive generic inheritance error. The concrete argument affects the possible
exception type and cannot simply be dropped as irrelevant compiler metadata.

The current product builds a two-node graph (root plus definition-normalized
Recur), with a self edge and the open Fault<T> source. The bounded test-only
experiment uses the existing CanonicalCallableIdentity and the existing graph
queue, retaining a complete constructed identity in its explicit key. Roslyn
Construct composes the real generic recurrence; no product substitution engine
or replacement analyzer is installed by this experiment.

| Dimension | Argument recurrence | Expanded | Registered | Pending |
| --- | --- | --- | --- | --- |
| Generic method | T -> T | 1 | 1 | no |
| Generic method | T -> List<T> | 32 | 33 | yes |
| Containing type | T -> T | 1 | 1 | no |
| Containing type | T -> List<T> | 32 | 33 | yes |

The 32-step cutoff belongs only to the finite proof harness. No unbounded
product run was attempted and no product budget was added. Strictly increasing
List nesting proves why exact-key repetition cannot terminate this recurrence;
nontermination of a naive full-key product change is an inference from that
recurrence and the actual drain-until-empty builder, not a fabricated timed-out
execution. Canonical identity distinguishes the instances correctly; the new
boundary is finite graph construction/recursive substitution, not an inability
to represent one constructed callable.

Keeping definition folding without retaining substitution is the proven wrong
behavior. Simply retaining every constructed key removes termination. Retaining
parametric summaries and composing substitutions on edges instead would require
defined recursive/SCC/fixpoint and escaping-type uncertainty semantics. None is
provided by the existing evaluator's active-key skip, which is reached only
after construction. An arbitrary numeric limit or first-compatible construction
would introduce an unjustified policy. The required follow-up is **Bounded
Generic Context Expansion and Recursive Substitution Contract**: specify finite
reuse/typed fail-closed behavior for type-changing recursive contexts, then
implement exact contextual projection/rebinding without reopening B25B.

Accordingly A3 is a BoundaryAudit_StopRule, not a context fix. No production
candidate was applied, so none needed withdrawal. The newly authorized two A2
cases were not changed: their assertions still describe the unchanged product.
closed-source and all other B25/B25A/B25A2 assertions and witness files remain
unchanged. New tests/evidence live only under A3. Solution Finding/CLI admission
stays closed, and B25B is not ready.

## Independent unchanged-baseline failure

The fresh c777 HEAD Dual build reaches clean WAE builds, Worker runtime checks,
Boundary 596/596 and External Target 36/36, then B24 fails 41/42:
GenuineModeContract_FullNativeFindingEvidenceAndRepeat(project: True,
scenario: unused). Its separate HistoricalPdbFixture probe terminates with a
native System.AccessViolationException in Roslyn MethodCompiler..ctor while
GetDiagnostics is called by B14ScopeProbe.Observe/B12ReferenceProbe.Run.
The new A3 class was not in that archive/build and no production edit existed.
The full original log, failed TRX, false/incomplete Dual audit and unchanged
images are preserved before overlay. No baseline replay was performed.

This establishes a failure in existing unmodified probe execution, not its
memory-corruption root cause, reproducibility or a compiler-version diagnosis.
No Roslyn/runtime/harness/timeout/PID fix is in scope. Later required regressions
and the final post-change Dual are distinct configured validations, not retries
until green; they cannot erase this baseline failure. Overall validation must
retain a red gate for this attempt even if later invocations pass.

Final regression/build/evidence results are recorded below. No fresh Self
Analysis or standalone Canonical Finding Diff is claimed.

## Root-cause chain and coverage (closure items 1–20)

B25 proves wrong assembly ownership after evaluation: local B retains its own
source exception, but the root's declaration-ID first-match can substitute A.
B25A's canonical top-level rebinding candidate was withdrawn because constructed
nested source/PE rebinding and definition-erasing catches were not correct.
B25A2 then proves that closed containing construction is already absent in
summary contexts. None of those historical audits was a production fix.
A3 separates both generic dimensions and the queue termination consequence.

| Requirement | Actual new evidence and limitation |
| --- | --- |
| G1 method invocation | Real M<int> symbol retains Int32; its definition/context discards it and its body still produces Fault<T>. This characterizes the defect, not corrected propagation. |
| G2 multiple constructions | Three calls (int, string, int) have two exact canonical identities but one actual target key. Correct finite reuse would add one context, not one context per call. |
| G3 containing construction | Api<int>.M has no method type arguments but a constructed containing type. The actual context discards that dimension too. |
| G4 nested combination | Outer<int>.Inner<string>.M<bool> retains all three argument scopes at its call site; definition normalization removes method/containing construction. Existing canonical structure can express all scopes. |
| G5 A -> B | Real compiled/emitted B PE referenced by A, and actual source ResolveMethod in B. Containing construction survives this resolver while method construction does not; subsequent context normalization discards both. This is a compiler-local rebinding witness, not a completed new P5J/Worker generic solution-flow contract. |
| G6 A -> B -> C | No new corrected constructed two-hop implementation is claimed after Stop. Existing B15 complete/proven two-/three-project regressions are run unchanged. |
| G7 non-generic | The new nongeneric call-site identity equals its definition identity, and unchanged regression/baseline cases validate the existing product. |
| Catch | Real generic method/containing-body Fault<T> catches are collected as open symbols before comparison. Contextual projection must precede comparison; the old definition-only matcher is not repaired here. Existing concrete-type catches remain regression targets. |
| Negative mappings | No new mapping implementation/DTO is admitted, so missing/conflicting/ambiguous mapping acceptance is not claimed tested or closed. Existing forged/missing owner/module identity tests remain unchanged. |
| Canonical | Existing constructed callable/type identities distinguish exact argument scopes and repeated calls; no new neutral schema, global equality change or Roslyn IPC object is needed to express a finite instance. |
| Current | Independently compiled call-site, actual context, cache/rebind/catch and finite queue observations prove the loss and expansion boundary. No corrected Current engine is claimed. |
| Historical | Two genuine Worker witnesses terminate under the existing definition fold and equal Current's still-open result. Their Historical identities and lifetime/isolation assertions pass; parity is not constructed correctness. |

The 17 new cases are five call-site dimension cases, two repeated-construction
cache cases, four finite recursion experiments, two A/B source-rebinding cases,
two body-catch cases and two genuine Worker cases. Seventeen fresh JSON witnesses
are emitted only under A3. The finite exact-key experiment counts Recur contexts
only, excluding the caller root. The unchanged actual product has two total
nodes (root + Recur). Growing experiments add 32 Recur contexts relative to its
one definition slot; same-type recursion adds zero. There is no existing generic
expansion budget to validate against, so no arbitrary number is invented as a
new product policy. Worker deadlines are process lifetime bounds, not a safe
Current compiler-local graph termination contract.

## Production, tests and historical protection (items 21, 27–32)

No production candidate, partial generic engine, resolver/catch correction,
scope loading or policy/CLI/reporting change was applied. No production file
needs rollback. The newly authorized A2 method's two cases stay byte-identical
because their characterized error has not been corrected. No further permission
is used or inferred. All other existing tests and original tracked files remain
unchanged except the explicitly additive permanent gate and boundary register:

- build/Verify-DualVersionBuild.ps1: two A3 lines, a stop-audit comment plus a
  separate constructed-invocation audit test gate. Existing gates untouched.
- Evaluation/OPEN-PIPELINE-BOUNDARIES.md: one A3 boundary paragraph and the Last
  Verified label. It does not rewrite B25/B25A/B25A2 historical report/audit data.

Verify-Changes checks all 1009 captured original files (except those exact two
additive deltas), reconstructs their previous bytes, and writes HEAD-relative
patches under A3Proof. B25, B25A, B25A2 reports/audits/Proof witnesses and all
three old characterization classes remain exact bytes. Their ordinary fresh
test outputs occur only inside this new validation export, never in previous
historical exports. Read-only historical verification checks 333 recorded
fingerprints, retaining B25A's old red Broad and B25A2's old green audit.

The root ignore and actual 23 Core files are never built/formatted/modified in
the authoritative workspace. Six stash entries and the clean semantic index
are checked against the starting snapshot. Physical index hashes are diagnostic
only; no repair/reset is performed. HEAD remains c777 unless an explicit final
check detects an external movement. No commit, push or stash operation occurs.

SolutionTransitive final Finding/CLI remains closed. B25B is not ready. The next
work is the bounded generic-context/recursive-substitution contract, followed
by contextual projection and the still-open exact type-normalization/catch
contract; a successful identity representation alone is not Finding admission.

## Validation closure (items 22–26, 29)

The new model tests pass 17/17; the combined new A3 plus unchanged A2/A/B25
tests pass 78/78 (17 + 20 + 14 + 27), without skips. The final clean build of
Main, Historical, Worker, Tests and refreshed graph fixture is WAE-clean.
PID-G1 passes 18/18 plus 9/9 deterministic serialized lifetime decisions.
Format/PowerShell/UTF8-no-BOM/CRLF, Git whitespace/case and exact tracked
project-reference inventory checks pass. This is not an actual Linux run.

The one-pass regression sequence completed with no failed or skipped tests:

| Group | Passed / total |
| --- | --- |
| A3 plus unchanged A2/A/B25 | 78/78 (17 + 20 + 14 + 27) |
| B24 / B23 / B22 / B21 | 42/42, 36/36, 46/46, 26/26 |
| B20 / B19 / B18 / B17 / B16 | 10/10, 39/39, 7/7, 25/25, 18/18 |
| B15 / B14 / B13 / B12 | 28/28, 31/31, 34/34, 29/29 |
| B11 / B10 / B5–B9 | 16/16, 34/34, 156/156 |
| Productive / Focused | 855/855, 962/962 |
| Broad / Full | 3263/3263, 3354/3354 |
| PID-G1 | 18/18 plus 9/9 deterministic lifetime decisions |

The unchanged Acquisition concurrency test passes in Broad (2.6299237 seconds)
and Full (1.2718974 seconds). It was not repaired, skipped or retried. All 17
new tests are additive; existing counts/assertions were not relaxed. Eight fresh
Current cases are byte-identical after Broad; the final evidence gate also
checks their final outputs. Existing generic and concrete Catch regressions
remain in the unchanged Focused/Broad/Full filters.

Separate Boundary passes 596/596. The final post-change Dual executes all eleven
WAE build steps with zero warnings/errors and passes Boundary 596/596, then
fails External Target 35/36. GenuineCli_RejectsWithoutFindingsFallbackOrSuccess
(scenario: missing-pe) fails during its fixture preparation, before CLI rejection
is exercised: the separate HistoricalPdbFixture process reports Fatal error,
Internal CLR error (0x80131506), in Roslyn MergedNamespaceSymbol..ctor while
B12ReferenceProbe.Run calls Emit. Its raw complete stack and failed TRX are
retained. The unchanged fail-closed driver stops, so B24/B25/B25A/B25A2/A3 Dual
gates are **not reached**. The older B24 TRX left in that directory is explicitly
stale for this final run and is not counted as fresh validation. Those classes
are covered by the separate completed regression/combined/full runs, not by a
fictitious completed Dual. No further Dual run or failed-test replay is done.

This second native failure shares the existing fixture/probe execution family
with the initial baseline failure but has a different test, phase and crash
signature. A common Roslyn/runtime/environment memory-corruption cause is not
established; neither crash is attributed to generic semantics or a diagnosed
compiler-version bug. Both are independent retained validation failures; no
probe/harness/compiler/runtime/timeout/PID repair is included. The later regular
B24 and B23 groups pass, which does not repair or explain these native crashes.

Same-export Current, Historical and Worker DLLs remain byte-identical before/
after all work, respectively:

```text
Current    ACCECCFFB3DFAEBA7EAC7CF54BB3F74E3589202BC66979FAFAA5CD6D6D38195A
Historical F470A1B21A0C87C47265A93BD6F0CB31F698A354E1FBE8FD028E2F4581EFBA9A
Worker     C1C9BD1310C2918C9963C7C70838BEF64B11BEE8F8A4AE244C5FE773E9B68295
```

These are the c777 A3 export images, not hashes from earlier export paths.
All 121 shared sources and all original production/test files remain unchanged.
The exact Main/Worker/Test images captured before final tests still match after
the final Dual build. Eight fresh Current baselines are byte-identical; no
baseline regeneration or exception for changed generic cases is used.

Read-only historical evidence verification passes all 333 fingerprints. The A3
overall evidence gate must return nonzero with Passed=false and
ValidationPassed=false while distinguishing EvidenceConsistent=true when all
real attempts, including both driver failures and not-reached final gates, are
correctly bound and recorded. This is not a green closure or a waived failure.

Self Analysis is not fresh: inherited count 16 only. Standalone Canonical Finding
Diff is not fresh: inherited 0 added / 0 removed / 0 changed only. The eight fresh
Current comparison cases are separate evidence, not renamed as either run.

## Final repository state

The traffic-reset continuation was audited against the actual existing A3 WIP,
not restarted from the prompt's earlier ignore-only snapshot. HEAD, diff, clean
semantic index, committed B25/B25A/B25A2 reports and completed evidence were
rechecked. No test, build or failed probe was replayed for this documentary
closure; only this report and its evidence binding were refreshed.

HEAD remains c777f9642c145a57c112c28216c3996128c6234b, with no external movement.
The semantic index remains empty. Its captured physical hash is
7E38AE2E60BE2F836FC08BD9305155A3D5A7D8323D33975C49CD46EB782B3F8D;
the final audit records the end hash without requiring physical repair. Root
ignore SHA256 is 7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5.
All 23 actual Core files and six stash entries are verified against the actual
start inventory. No index repair, commit, push, stash operation or protected
WIP rewrite occurs.

Final git status --short:

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B25A3-constructed-generic-summary-context-audit.json
?? Evaluation/P5O2B25A3-constructed-generic-summary-context.md
?? Evaluation/P5O2B25A3Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs
```

Outcome: BoundaryAudit_StopRule, ConstructedContextFixed=false,
ReadyForB25B=false, SolutionFindingAdmitted=false. Overall validation is red;
the useful completed regression results do not turn this into a production fix.
