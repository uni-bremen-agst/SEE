# P5O2B25A4 — Bounded generic contexts and recursive substitution

## Phase 1 — read-only trace, before any production candidate

Actual starting HEAD: c777f9642c145a57c112c28216c3996128c6234b. The root ignore
is protected foreign WIP; A3 report/audit/Proof/test and its gate/register delta
are inherited uncommitted WIP, not new A4 edits. The semantic index is empty.
All builds use a new disposable offline export, never the protected Core tree.
No commit, push or stash mutation is authorized.

B25 lost the collected B exception owner during root declaration-ID first-match
normalization. A's top-level canonical candidate was withdrawn on constructed
nested rebinding and catch mismatch. A2 found earlier call-site substitution
loss. A3 established separate method/containing loss and unbounded exact-key
growth under T -> List<T>. None is an adopted production correction.

1. Call-site GetSymbolInfo selects the real constructed IMethodSymbol. Its
   method arguments and each containing-type argument are available there.
2. ExceptionFlowCallContext's constructor sets CallableSymbol to
   OriginalDefinition. Its ordinal value facts are normalized, None facts and
   negative ordinals removed, duplicates unioned, and stable member symbols
   definition-normalized/deduplicated. CreateLegacyKey sorts those facts.
3. TryCreateCanonicalContext sorts ordinal/member facts and exports a
   definition-normalized Callable. No generic substitution is retained.
4. RebindCallable transfers parameter/member facts to the supporting source
   symbol. CrossCompilationResolver can rebind constructed containing members;
   it does not reconstruct concrete method arguments. B12/B15 exact ownership
   receipts remain authoritative; no name-based recovery is permitted.
5. ExceptionFlowCallableKey separately stores Symbol.OriginalDefinition and a
   context string. Equality/hash use exact Roslyn symbol identity plus ordinal
   string equality. CanonicalIdentity separately normalizes to the definition.
6. ExceptionFlowSummaryGraph has two key-indexed dictionaries (summary/context)
   and one pending queue. GetOrAdd schedules once per key. It contains no SCC,
   dependency invalidation, context/depth/node budget or substitution policy.
7. A SummaryAnalysisSession reuses its graph across roots in one semantic
   environment. It is explicitly sequential/not thread-safe. Evaluation state
   is local to a root; no new global/inter-compilation cache is justified.
8. BuildPendingSummaryNodes drains the queue before evaluation. Each node visits
   its declaration's SemanticModel, which still correctly reports open T.
   Substitution must be composed before collecting relevant body/catch/callee
   types, or carried explicitly on parametric transfer functions.
9. Evaluate uses activeKeys, completedFrames and an explicit DFS stack, all
   keyed by ExceptionFlowCallableKey. Active target edges are skipped. Completed
   frames are reused. No SCC discovery or iterative fixpoint computation exists.
10. Thus there is no existing SCC/fixpoint identity to extend. Definition/fact
    reuse bounds current examples accidentally, not by a generic semantic
    expansion contract. Worker process/JSON/request budgets protect transport
    and lifetime, not Current graph construction or semantic convergence.
11. CanonicalCallableIdentity/CanonicalTypeIdentity already express exact
    assembly/module, nested containing chain and method/type arguments. Reuse
    them; no wire schema or second identity/substitution engine is justified
    merely to represent a finite construction.
12. Existing compiler Construct/member-resolution operations can recreate
    selected symbols. They do not substitute an entire source SemanticModel or
    establish recursive summary convergence. Type parameter scope includes its
    declaring owner and ordinal, not its spelling.

## Candidate contract and proof obligations

The minimal intended context consists of source definition, scoped concrete
method and complete containing-chain substitutions, existing normalized value
facts and exact owner identity. Exact constructions with identical facts reuse
one key. Different constructions that affect throw/catch/callee/ownership remain
distinct. No relevance-based merging is justified without a dependency proof.
The declaration symbol may remain definition-scoped for syntax/value facts;
canonical invocation evidence must retain its actual construction.

Compose body type expressions under the caller environment before rebinding a
callee: List<T> under T=int is List<int>. Replacing textual T is invalid: method
and containing T can have different owners. Nested owner chains participate in
composition. Repeated identical substitutions reuse; constant substitutions
can reach a finite stable context; structural wrapping can grow without bound.

Before considering a production correction, inspect R1 identity, R2 constant,
R3 growing, R4 mutual and R5 containing recurrences, plus the existing evaluator
on finite mutually recursive summaries. Terminating graph construction alone
does not prove a reusable cached recursive result. An arbitrary depth limit,
discarded back edge, silent truncation or guessed widened exception is not a
contract. A true cutoff must produce explicit propagated uncertainty and block
bounded external complete/proven admission, identically in both engines.

No production candidate has been applied. The two narrow A3 authorizations and
the earlier two A2 case authorizations are conditional on an actual correction;
no old assertion or historical witness has yet been changed. All later results
must distinguish boundary characterization from achieved correction.

## New independent boundary — finite recursive memoization

The first model run (9/11) disproved a fixture assumption, not a product invariant:
an outer B call is collected before a nested try A call by AnalyzeSummaryNode's
simple-node phase, regardless of text order. Both root order variants therefore
returned Fault. The exact failed source, log and TRX remain in the A4 export and
artifacts/p5o2b25a4/model-prototype. No production file changed. The corrected
new fixture places BOTH calls in separate try fragments, one with catch Fault,
the other with empty finally. Explicit assertions now verify actual edge order.
This is new-test development on changed input, not an unchanged-test green retry.

The refined 11-case Current model passes. In both generic-stable and nongeneric
forms, the finite graph contains Root, A and B, with A -> B -> A. A has one local
Fault source behind an unknown mutable bool guard. Both root orders have a real
escaping path Root -> B -> A -> throw. When the caught Root -> A edge is visited
first, evaluation enters A, then B, skips B -> active A and caches B as empty.
A's local Fault is caught on return to Root. Root's later uncaught B edge reuses
the empty completed frame: zero exceptions AND zero uncertainties. Evaluating
B independently in the exact same graph returns Fault. Visiting uncaught B first
also returns Fault from Root. This is a real actual-evaluator observation, not
a test-only context-expansion engine or a hypothetical infinite product run.

Here A and B are mutually recursive METHODS in one compilation, not cyclic
project dependencies. No B15 project graph or general solution-loading scope is
expanded by this witness.

Thus active-key skipping plus unconditional completed-frame reuse is not an
SCC/fixpoint closure contract, even before introducing expanding generic terms.
It cannot certify C3/C5 recursive summaries as complete independently of entry
order. New generic keys alone do not fix this shared evaluation boundary.
No broad evaluator rewrite, silent incompleteness or partial generic candidate
is adopted. Under the explicit A4 Stop rule, the outcome is a Boundary Audit.
Required follow-up: **Recursive Summary SCC Closure and Sound Cache Reuse**,
including edge-specific catch transfer, finite path evidence, dependency-aware
completion and uncertainty. Only then re-evaluate bounded generic substitution.

## Proposed boundedness and uncertainty contract — not implemented

R1 T -> T reuses an exact context. R2 T -> int from string has two total contexts
(initial string, one int); int -> int stabilizes. R3 method and R5 containing
T -> List<T> produce infinitely many exact terms. R4 A<T> -> B<List<T>> -> A<T>
adds one List constructor each cycle of A, even though the definition SCC is
finite. No equality/canonical representation can merge these different relevant
exception constructions without losing precision. The finite compiler probes
illustrate three exact A constructions only; three is NOT a production limit.

A minimum safe future rule is: no cached recursive result may be branded complete
until its relevant dependency/SCC transfer has closed. Any cutoff of reachable
unrepresented contexts must retain exact known evidence AND explicit propagated
uncertainty, never silently report complete empty flow. Existing bounded external
complete/proven admission must reject that incompleteness; Current may retain
known exceptions plus uncertainty. Both engines must share the same rule.
Numeric/global budgets, symbolic abstraction or widening are not selected here:
each needs a proved deterministic domain/order/termination and evidence policy.
The existing path cap/Worker timeout does not define such a graph construction
or SCC closure contract. Adding one while finite cache soundness is unresolved
would be an inconsistent partial implementation.

The new R1–R5 compiler probes, exact ConstructedFrom reconstruction and nested
containing scopes validate identity/composition facts, not a substituted body
engine. C1/C2/C6 remain characterized by unchanged A3 tests; no corrected C7 A->B
or C8 A->B->C transport is claimed after Stop. Existing A3 resolver and B15 graph
regressions stay unchanged. Negative mapping/arity/ambiguity/owner admission
cannot be claimed closed without a new implemented contract; old canonical
negative tests remain regression targets, not a new acceptance engine.

The compositional requirement can be stated without choosing a new DTO or
algorithm: X(v) = Local(v) union the edge-transferred X(w) of every callee w.
Transfer includes exact substitution, catch suppression and path prefix. A
complete recursive component must satisfy its simultaneous equations, and a
memoized result must not depend on which ancestor happened to be active when
it was computed. In the three-node witness, B needs A's local Fault, but the
current cached empty B does not satisfy that equation. Changing the key cannot
make that cached empty result complete. Path evidence and exception-type facts
need separate closure policies: MaximumPathsPerException=64 already reports
path truncation, but does not bound the number or nesting of generic types,
pending nodes, substitution environments or SCC iterations.

The SCC witness deliberately uses one nongeneric Fault in one compilation, with
ordinary exact typed catches. No constructed exception, cross-assembly owner
collision or nested resolver mismatch is needed to produce the loss. Its generic
variant changes only A/B method declarations and preserves T across the cycle;
the five separate Fault<T> recurrence probes establish relevance of the generic
argument to possible exception types. No conflation of these two proofs is used.

This is a necessary future correctness contract, NOT a selected widening,
completed SCC implementation or proof that every possible generic algorithm
must diverge. Finite exact-key reuse can represent R1/R2; exact generic terms
represent R3/R4/R5 but alone do not provide convergence. The unresolved finite
cache closure is an independent prerequisite under the user's SCC Stop rule.
The follow-up does not mandate Tarjan, a particular fixpoint solver or a large
rewrite: a proved cycle-aware cache policy could be smaller. Its required result
is sound reusable completion, not the presence of a function named SCC.

The two recreation probes inspect already constructed real call-site symbols
and rebuild them with Roslyn ConstructedFrom, including the nested containing
chain. They are not execution of a general open-expression substitution engine.
Composition of scoped open body expressions is specified above as an obligation,
not claimed implemented or fully covered by those two representation probes.

## Scope, old tests and evidence preservation

No production candidate was applied or withdrawn in A4. No generic budget,
engine, resolver/catch repair, CLI, reporter, routing, version, IPC, PID or native
probe/harness change was made. The only shared-file deltas against captured A3
WIP are two additive permanent A4 gate lines and one additive boundary paragraph.
The A3 register paragraph/Last Verified label are preserved byte-for-byte.
Exact patches are in P5O2B25A4Proof/*-vs-inherited-a3-wip.patch.txt.

All 1023 inherited files (including untracked A3 WIP) are hash-checked, with only
those two shared-file additive exceptions. The old A3 class is unchanged in
full. Neither newly authorized A3 method/assertion is adapted because no corrected
context implementation is adopted. Both previously authorized A2 cases, all
other A2/A/B25 assertions and historical witnesses also remain unchanged. No
additional old characterization needed modification for this boundary audit.

Fifteen additive A4 cases cover four finite SCC order witnesses (nongeneric and
stable generic), five real R1–R5 recurrence observations, two method/nested Roslyn
composition cases, and four genuine Current/Worker SCC witnesses. They are
boundary regressions, not positive claims of corrected generic/SCC semantics.
Historical parity must never be reported as correctness: the Worker witnesses
explicitly retain the observed wrong result and actual lifetime/isolation checks.

Read-only historical verification checks 438 exact fingerprints across committed
B25/A/A2 and inherited uncommitted A3 evidence. The old A Broad failure, old A2
green result and old A3 native failures remain unchanged. Where shared gate bytes
have evolved, the verifier checks their exact old export/committed/snapshot bytes,
not today's gate and not a rewritten historical audit. Old tests emit ordinary
fresh outputs only inside the NEW A4 export, never over old witness evidence.

The actual protected inventory is 23 Core files and six stash entries. Root
ignore, semantic index and stash/Core identities are compared with the actual A4
start snapshot, not inferred from prompt history. Physical index hashes are
diagnostic only; no repair is performed. Original Core bin/obj never receive
build/clean writes. All restore/build/test activity is confined to the new
artifacts/b25a4-c777/Tools/XMLDocNormalizer export with mapped local caches and
remote feeds disabled. No Linux execution is claimed; Git casing/reference
inventory is only a static Linux-compatibility check.

## Validation closure

The regular configured groups completed once each, with the actual counts below.
The initial 9/11 changed-fixture development attempt is retained separately, not
relabelled as a successful run. Failed required final gates remain red even when
other scheduled groups pass. No skip, unchanged-test green retry, sleep or
workaround was introduced.

| Group | Passed / total | Failed / skipped |
| --- | --- | --- |
| A4 plus unchanged A3/A2/A/B25 | 93/93 (15 + 17 + 20 + 14 + 27) | 0 / 0 |
| B24 / B23 / B22 / B21 | 42/42, 36/36, 46/46, 26/26 | 0 / 0 |
| B20 / B19 / B18 / B17 / B16 | 10/10, 39/39, 7/7, 25/25, 18/18 | 0 / 0 |
| B15 / B14 / B13 / B12 | 28/28, 31/31, 34/34, 29/29 | 0 / 0 |
| B11 / B10 / B5–B9 | 16/16, 34/34, 156/156 | 0 / 0 |
| PID-G1 | 18/18 plus 9/9 deterministic decisions | 0 / 0 |
| Productive / Focused | 870/870, 977/977 | 0 / 0 |
| Broad | 3277/3278 | 1 / 0 |
| Full | 3369/3369 | 0 / 0 |
| Separate Boundary | 596/596 | 0 / 0 |

Existing generic recursion, finite cycle, contextual-fact and catch regressions
remain unmodified within the normal Focused/Broad/Full filters. All fifteen new
A4 tests pass, including the four genuine Worker cases. No old test or assertion
was removed, relaxed or replaced. Separate Boundary closure is recorded below.

Separate Boundary completed 596/596 after the complete regression sequence,
without skips. Both evidence checks complete: 438 read-only historical
fingerprints pass; the overall verifier binds actual TRX counters/time windows,
all fifteen new witnesses, source/DLL fingerprints, old development failure,
eight byte-identical baselines and repository protections. It correctly records
Passed=false, ValidationPassed=false, EvidenceConsistent=true and actual child
exit 1 for the retained Broad failure and its enclosing regression phase. No
validation failure is waived or replaced by the green Full/Boundary/Dual gates.
The process record binds the exact final audit SHA256. Git's existing protected
root-ignore LF/CRLF warning is retained; it is not a WAE build warning and was
not repaired by modifying the protected WIP.

The first overall-verifier attempt stops before creating an audit: its raw
workspace=export byte-equality assumption was too strong. The immutable pre-test
capture already records CRLF workspace versus LF export for the new A4 test and
eight shared files. Both recorded raw hashes remain exact and unchanged. The
completed verifier instead binds the exact compiled export bytes to portable
PDB document SHA256 records, and binds those PDBs to their unchanged executed PE
CodeView GUID/stamp and deterministic PDB checksum. All 121 shared documents are
checked in BOTH Current and Historical, plus the new test document (243 document
bindings). Workspace/export text is equal under CRLF-to-LF replacement ONLY,
without trimming or any other rewrite; the nine differing files contain no raw
or verbatim string literals whose values could change with line endings.

The read-only compiled-source proof preserves both raw fingerprints and the
original input capture; it does not normalize an export, recompile, rerun a test
or rewrite a DLL/PDB. The failed verifier process record is retained. The final
closure process has its own record and still exits 1 for Broad. This helper
correction does not waive a validation failure or claim raw-byte equality that
did not exist. Metadata-only dependencies are loaded in a separate PowerShell
process; no Analyzer/Roslyn package graph or runtime image changes.

### Broad is red — unrelated acquisition assertion retained

The regular Broad run finished 3277/3278, one failure and zero skips. The unchanged
ExternalSourceLineEndingReconstructionTests.ConcurrentReconstruction_PerformsOneRequestAndOneAttempt
fails at line 341, Assert.True(await first), after 7.6955551 seconds. This is the
same existing assertion recorded in historical B25A's red Broad, not a PID or
native Roslyn crash and not a new A4 assertion. Both its source and the production
ExternalSourceLinkClient source are byte-identical to the actual A4 start.

Read-only trace: the fake handler signals entered then waits on release with a
cancellation token. The test awaits entered, checks that concurrent acquisition
is rejected, releases the first request and expects it to succeed. Its configured
total timeout is five seconds. TryDownload waits synchronously on its async
operation and maps OperationCanceledException to false. Delayed continuation/
thread-pool scheduling exceeding that timeout is consistent with the observed
false result and duration, but cancellation/scheduling root cause is NOT proved:
the unmodified test records neither the cancellation cause nor the release times.
No infrastructure repair, timeout change, scheduling workaround or isolated retry
is authorized or performed. All later required groups continue as configured;
their success cannot replace this failed Broad attempt. The final overall audit
must remain Passed=false/ValidationPassed=false and exit 1 while allowing
EvidenceConsistent=true for correctly retained evidence.

The already configured Full run then passes 3369/3369 with that same acquisition
test passing in 3.0149524 seconds and unchanged Main/Core/Historical/Worker/Test
image hashes. The two observations establish differing outcomes without a repair,
not a diagnosed scheduling cause. The enclosing complete regression phase returns
exit 1 as required; neither Full nor the preceding green Dual clears Broad.

The complete final Dual command with -RunMainBoundaryTests and
-BuildTestProjectReferences passed in one execution, 2026-10-21
14:35:42–14:44:34 Europe/Berlin (UTC+02): Boundary 596/596, External Target 36/36, B24 42/42,
B25 27/27, A 14/14, A2 20/20, A3 17/17 and A4 15/15. All eleven WAE build
steps have zero warnings/errors, including Current, Historical, Worker and test
fixtures. Genuine Worker image/version, deterministic smoke, invalid-protocol,
architecture and isolation checks pass. No package/compiler version changes.

The exact two former A3 crash cases pass in that regular Dual run:
GenuineCli_RejectsWithoutFindingsFallbackOrSuccess(missing-pe), 0.7770204 seconds,
and GenuineModeContract_FullNativeFindingEvidenceAndRepeat(project: True,
scenario: unused), 4.9668915 seconds. Neither native signature recurs in this
execution. This does not prove a root cause, a repair, general absence of flakes
or a causal relation to A4. Product/probe sources are unchanged, and historical
A3's AccessViolation/Internal CLR failures remain red and frozen. No diagnostic
retry, runtime/Roslyn/harness change or native-crash suppression was performed.

Fresh same-export Main/Core production DLLs remain byte-identical to the initial
baseline: A69D288E37587EF55043F3B14A2EB1F1E42835891DA9D36F5E1F122C73EC6A45
and 73D2C605D9A756E49A52C4EAFB82A06BB003B7149EDA9734D02497F6534C6F7B.
No before-A4 Historical/Worker baseline image was captured in this new export;
no invented image before/after comparison is claimed for them. All 121 shared
sources are unchanged, and the five exact Main/Core/Historical/Worker/Test images
captured after the final Dual test build remain unchanged at the final audit.
Boundary had already started when that snapshot was taken; it is not claimed
as a pre-Boundary image capture. The source/image snapshot precedes the A4 Dual
gate and subsequent combined/regression tests, and records its actual timing.

Self Analysis is NOT fresh (inherited 16 only). Standalone Canonical Finding Diff
is NOT fresh (inherited 0 added / 0 removed / 0 changed only). Eight fresh Current
comparisons are separate evidence, not either of those runs. B25B and final
SolutionTransitive Finding/CLI admission stay CLOSED regardless of test color.
Those admission references concern the bounded external Historical operation;
ordinary existing Current project/solution paths are not disabled or changed.

## Current baselines and final repository state

All eight fresh Current JSON baselines are byte-identical: Direct,
ProjectTransitive, ProjectTransitiveDeclaredExceptions and SolutionTransitive,
each False/True. No regeneration or exception for an altered generic case is
used. Existing modes are not expanded or given new Finding admission.

Starting/final HEAD remains c777f9642c145a57c112c28216c3996128c6234b on
fix/documentation, with no external HEAD movement. The semantic index remains
empty. Its initial/end physical SHA256 is
7E38AE2E60BE2F836FC08BD9305155A3D5A7D8323D33975C49CD46EB782B3F8D;
root ignore SHA256 is
7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5.
All 23 actual Core files and six stash reflog entries remain unchanged, including
both protected position-provenance stashes. No commit, push, stash operation,
index repair, foreign-WIP discard or original Core build/clean was performed.
The inherited A3 files below remain inherited WIP, not A4-owned modifications.

Final git status --short, including the newly generated A4 audit:

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B25A3-constructed-generic-summary-context-audit.json
?? Evaluation/P5O2B25A3-constructed-generic-summary-context.md
?? Evaluation/P5O2B25A3Proof/
?? Evaluation/P5O2B25A4-bounded-generic-context-expansion-audit.json
?? Evaluation/P5O2B25A4-bounded-generic-context-expansion.md
?? Evaluation/P5O2B25A4Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs
```

Outcome: BoundaryAudit_StopRule; GenericContextFixed=false;
RecursiveCacheFixed=false; ReadyForB25B=false; SolutionFindingAdmitted=false.
Overall validation remains red due to the unchanged acquisition Broad failure.
Next: Recursive Summary SCC Closure and Sound Cache Reuse, then bounded generic
context/substitution and the still-open exact nested type/catch/owner contracts.
None of those closures alone authorizes B25B activation.
