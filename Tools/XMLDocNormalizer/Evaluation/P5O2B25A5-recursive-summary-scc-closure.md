# P5O2B25A5 — Recursive summary SCC closure and sound cache reuse

Bounded finite recursive-cache repair implemented and verified by independent
Current tests and genuine Historical execution. **Overall validation is RED**:
Full is 3401/3402 due to the unchanged, historically documented multi-module
fixture/lifetime failure below. No retry, waiver or foreign repair. B25B remains
closed; this is not completion of the original generic-expansion package.

## Scope, inherited state and Phase 0

Starting HEAD: `c777f9642c145a57c112c28216c3996128c6234b`, branch
`fix/documentation`. Root `.gitignore` is protected foreign WIP. All pre-existing
A3/A4 reports, audits, proof files, tests and gate/register deltas are inherited
uncommitted WIP, not new A5 work. The semantic index is empty. No commit, push,
stash mutation, reset/restore or authoritative Core build is authorized.

The repository-wide characterization preflight and pre-production lifecycle/
domain trace are recorded in `P5O2B25A5Proof/Lifecycle-and-contract.md`.
The first turn stopped before changes for explicit permission. The user then
authorized exactly TWO existing A4 assertions, each in the generic=false/true,
uncaughtFirst=false cases:

- `FiniteMutualCycle_CachedPartialResultDependsOnRootEdgeOrder`: empty thrown
  exceptions becomes exactly one named `Fault`.
- `GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness`: empty Current
  entries becomes exactly one entry with MetadataName `Fault`.

All uncaughtFirst=true assertions, uncertainty assertions, parity, lifetime,
isolation and all other existing tests remain unchanged. Only associated new
witness metadata/output is redirected to A5 for these four corrected cases.
The uncaught-first metadata values remain unchanged. The exact source/test/gate
delta is checked against the captured inherited A4 WIP, not merely HEAD.
Historical A4 reports/audits/witnesses are not rewritten; the old failure is
still reproduced by the retained original witness bytes and compared directly
with the new same-source results. Any further required characterization change
would require another STOP; none is currently needed by the candidate.

## Root-cause chain and actual loss

B25 demonstrated root declaration-ID first-match normalization replacing the
actual exception's assembly owner. B25A's top-level candidate was withdrawn on
constructed nested rebinding and catch mismatch. B25A2 located generic argument
loss earlier in summary context/key creation. B25A3 showed that exact contexts
can grow without bound under T -> List<T>; the builder drains pending work before
any evaluator guard. B25A4 then established a separate finite-cache defect.

The A4 graph contains Root, A and B, with A -> B -> A. A has a Fault source
behind an unknown mutable bool guard. Root calls A inside catch(Fault), then B
without a catching handler (two explicit try fragments establish real edge order).

1. A enters with local Fault.
2. A enters B. B skips its edge to active A.
3. B is popped and unconditionally published as completed under key B, empty.
4. A completes with Fault; Root's first edge catches it.
5. Root's uncaught B edge reuses empty B: zero exceptions and zero uncertainty.

Independent evaluation of B, or reversed root edge order, finds Fault. This is
not a PID reuse, shared-concurrent cache, source acquisition or Roslyn-version
problem. Current/Historical parity alone previously reproduced the same defect.

## Lifecycle, states and ownership

Registration creates a mutable empty Summary and queues a new key once.
CallableKey stores OriginalDefinition plus the existing normalized value-fact
ContextKey. Sources, edges and unresolved targets are collected by body analysis
and completed fragment transfers. Local catch suppression/removal happens here;
HasExecutableBody records body availability, NOT evaluated dependency closure.
The sequential session drains pending construction before evaluation and reuses
only the graph across roots in one semantic environment.

`Evaluate` creates fresh dictionaries and explicit stacks for each root. No
global, per-project or cross-root result cache exists. The evaluator remains
stateless and has only graph-level dependencies. Missing summary/body preserves
existing unresolved-symbol uncertainty. Results are mutable DTOs, paths are
immutable, and merges copy/prefix/filter rather than exposing a mutable callee.

Before A5, active-key recursion prevention existed, but neither SCC discovery,
dependency closure, invalidation nor fixed-point propagation existed. Frame-pop
was wrongly treated as unconditional global-within-root completion.

The corrected states are structural, not a new externally supplied enum:

| State | Representation / permitted reuse |
| --- | --- |
| Not started | No frame or completed entry for the exact node/exclusion context |
| Evaluating | Private frame on the explicit stack |
| Provisional/incomplete | That frame's local accumulated DTO, with pending permitted children; never read from completed cache |
| Closed | Every permitted child context completed; only now inserted under its exact node/exclusion key |

Malformed externally supplied SCC/cache-state payloads do not exist in this API:
membership is calculated from the actual graph, caches are invocation-local and
private, and canonical transport occurs only after Evaluate returns. Tests cover
absent, unbuilt and malformed unbuilt-cyclic dependencies retaining uncertainty.
No incomplete result is silently relabelled complete/proven.

## Closure model, monotonicity and termination

Tarjan SCC discovery uses an explicit stack, preserving real graph edge-list
order; component/member IDs do not depend on dictionary/hash iteration. Cache
identity becomes `(callable key, sorted excluded ancestors in that same SCC)`.
Thus B under active A is not reusable as unrestricted B. Component exits have
no relevant ancestor exclusion: otherwise the exited component could return and
would be part of the same SCC. Acyclic nodes keep empty-exclusion/key-only reuse.
Identical recursive contexts remain cacheable; there is no blanket no-cache rule.

This is a finite dependency-DAG expansion equivalent to reachable-fact closure,
not a naive loop over ever-longer prefixed recursive paths. For frozen graph,
root compilation and fixed edge catch filters, define F(v,S) from v's local
sources/uncertainty and each permitted child F(w,S union {v}); ancestors already
in S are excluded. Every recursive internal dependency grows the excluded set;
component exits progress in the condensation DAG. A closed F(B,{A}) is valid
only in that exclusion context, not as F(B,empty). Full unrestricted results are
published only under an empty exclusion key after their own dependencies finish.
The whole SCC need not be materialized under every possible entry in advance.

Any escaping abstract walk with repeated keys has a cycle-erased escaping walk:
removing a cycle removes catch filters, never adds one. Thus finite simple-key
paths preserve reachable exception types, evidence kinds and uncertainty under
the EXISTING static summary contract. This does not prove concrete program
termination or repair known type-identity/context-substitution defects.

Direct/proven sources and external-documentation sources remain distinct.
Summary sources/target facts are fixed after construction. Type normalization
and ownership/catch predicates are unchanged. During evaluation, exception and
uncertainty sets only grow; no removal/replacement occurs. Pre-evaluation fragment
suppression is not a non-monotone SCC update. Prefix/path insertion is additive.
The existing 64-path cap explicitly marks omitted representative paths; it is
neither a new iteration bound nor an unrestricted-path least-fixed-point proof.
Existing finite-cycle and path-cap tests remain unchanged and pass in the
targeted candidate regression run. No unlimited cycle unrolling is introduced.

Tarjan costs O(V+E). An SCC with s members has at most s*2^(s-1) possible expanded
contexts; the active evaluation stack has at most V distinct keys. Dense SCCs
can still be expensive; no polynomial all-path-performance claim is made.
Acyclic components retain one evaluation context per node and the existing
24-level shared-diamond reuse. No magic iteration/retry/depth budget or sleep.

## Positive, negative, order and Current/Historical evidence

The new suite contains 33 cases: 25 independently checked Current/model cases
then eight genuine Worker cases. Real productive compiler/graph fixtures cover:

| Case | Required observation |
| --- | --- |
| R1 self recursion | Fault retained, no silent loss |
| R2 A -> B -> A | Caught-first and uncaught paths cannot share an invalid partial cache entry |
| R3 A -> B -> C -> A | Each entry and both tested root orders retain Fault |
| R4 SCC plus outgoing C | C's Fault reaches incoming SCC callers |
| R5 incoming X | X cannot obtain a prematurely completed SCC result |
| R6 repeated B / repeated Evaluate | Exact context reuse; deterministic result and no mutable-result poisoning between evaluations |
| R7 independent SCCs | Distinct SCC identities and both Fault/Other results retained |
| R8 acyclic chain | Existing ordered propagation and memoization remain valid |

All eight productive fixtures have non-generic and stable-generic variants.
Additional deterministic entry/order checks cover two- and three-node SCCs.
Absent, unbuilt and unbuilt-cyclic dependencies retain uncertainty on repeated
evaluation. A supplemental independent per-type filtered BFS checks all 512
three-node directed graphs, three source placements and three roots: 4608
comparisons. It does not replace the real compiler/Current/Worker witnesses.

Current correctness assertions precede genuine Historical parity. Worker tests
also retain actual lifetime termination, exact Historical Roslyn identity and
absence of Historical analyzer assemblies in Main. There is no Historical
special case, Current fallback, routing change or process-boundary weakening.

The four expressly authorized A4 witnesses have directly compared retained
before/new after results, with EXACTLY the same C# source input in each pair:

| Witness | Before entries | After entries | Uncertainty before/after |
| --- | --- | --- | --- |
| Current, non-generic, caught-first | 0 | 1 Fault | 0/0 |
| Current, stable-generic, caught-first | 0 | 1 Fault | 0/0 |
| Genuine Worker, non-generic, caught-first | 0 | 1 Fault | 0/0 |
| Genuine Worker, stable-generic, caught-first | 0 | 1 Fault | 0/0 |

The original A4 witnesses remain at their original paths/hashes. Corrected
witnesses are only in the new export's `artifacts/p5o2b25a5/analysis`. Other
unchanged old cases can emit ordinary outputs within this NEW export; no output
is copied back over a historical workspace or old-export witness.

## Validation record

The first unchanged-source baseline build passed with zero warnings/errors and
all eight Current cases passed. The candidate build also passed with zero
warnings/errors. Candidate Current: 25/25; targeted existing regressions: 25/25.
All eight fresh Current JSON baselines were byte-equal before the Dual run.

The full Dual run completed PASS on 2026-10-21, 15:54:31--16:03:18 Europe/Berlin.
All eleven WAE builds report zero warnings/errors. All nine gates pass:
Boundary 596, External Target 36, B24 42, B25 27, A 14, A2 20, A3 17, A4 15,
and A5 33. Runtime identities, deterministic isolated analysis and fail-closed
protocol checks pass. Compiler versions and routing are unchanged.

Combined A5/A4/A3/A2/A/B25: 126/126. PID-G1: 18/18 runtime tests and 9/9
deterministic process-lifetime decisions. Separate historical regression groups:

| Group | Passed / total | Group | Passed / total |
| --- | --- | --- | --- |
| B24 | 42/42 | B23 | 36/36 |
| B22 | 46/46 | B21 | 26/26 |
| B20 | 10/10 | B19 | 39/39 |
| B18 | 7/7 | B17 | 25/25 |
| B16 | 18/18 | B15 | 28/28 |
| B14 | 31/31 | B13 | 34/34 |
| B12 | 29/29 | B11 | 16/16 |
| B10 | 34/34 | B5--B9 | 156/156 |

| Final group | Passed / total | Failed | Skipped |
| --- | --- | --- | --- |
| Productive | 903/903 | 0 | 0 |
| Focused | 1010/1010 | 0 | 0 |
| Broad | 3311/3311 | 0 | 0 |
| Full | 3401/3402 | 1 | 0 |
| Separate Boundary | 596/596 | 0 | 0 |

No skips or test retries. The regression-script process correctly exits 1 for
Full; the separate Boundary process exits 0. The overall evidence gate must
retain this failure and exit 1 rather than use Broad/Dual to replace Full.

Full's sole failure is the unchanged
`ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed`, line
446, duration 0.0629336s: `Assert.Contains` finds no AssemblyFile with
ContainsMetadata=true. This happens BEFORE the factory call at line 451 and
outside any SummaryGraphEvaluator call path. Its source in the initial snapshot,
workspace and compiled export has identical SHA256
`4D959975EEFF758C31517E1D2C0C0EAAA2C805E5E9621B7E581FBB2A7A8420BC`.

Read-only trace: EmitMultiModuleAssembly uses the unchanged P5B CSharpCompilation
emitter; ReadAssemblyFiles creates a using stream/PEReader, obtains reader-backed
AssemblyFile structs, returns their array, and disposes the reader before the
caller reads ContainsMetadata. Metadata-only inspection of the ACTUAL net8
System.Reflection.Metadata.dll used by the tests confirms its `_reader` field
and records ContainsMetadata getter IL. This is an existing reader-lifetime
hazard, also documented in the retained B13, B15 and B20 reports (B20 records
the same source hash and line-446 managed assertion; B15 documents a native
failure at this helper). The failing invocation's PE/freed memory was not
retained; no claim of independently proven exact memory reuse at that instant.
No isolated retry, Full retry, emitter invocation or harness repair was performed.
The diagnostic's first metadata-only inspection used a C# extension as a
PowerShell instance method and failed before output; the static extension call
corrected ONLY this new inspection script, not a product/test/validation gate.
See `artifacts/p5o2b25a5/foreign-full-failure.json` and its A5 proof script.

All builds run only in `artifacts/b25a5-c777/Tools/XMLDocNormalizer`, an offline
HEAD+actual-WIP export. Authoritative Core bin/obj remain untouched. Two initial
archive PREPARATION commands failed before extraction/build, as recorded in
export.json; the captured state and empty destination were resumed. These were
not test retries. No other infrastructure/native/PID/acquisition repair occurs.

Sources were captured before Dual. Five built images were captured after the
final Dual test-project build, while Boundary could already be running. Exact
PE/PDB identities/checksums and all 244 Current/Historical/test source-document
bindings pass. Eight inherited workspace/export source differences are exactly
CRLF/LF only, with no multiline raw/verbatim literals; neither raw capture nor
compiled source is rewritten. See the source-input and compiled-source records.

The known Acquisition-Concurrency test is untouched and remains included in
Broad/Full. It PASSES in both: Broad 4.2762364s, Full 0.0863348s. No timeout,
assertion, retry or skip is changed. This is not an explanation/repair of the
historical A4 Broad failure: A4's red result remains frozen. PID-G1 is untouched.
Historical/protection verification passes with 566 exact fingerprints; the
overall evidence gate retains the Full failure rather than claiming all-green.
Static format/PowerShell/UTF8/CRLF/JSON/Git-casing/project-reference checks are
not a claim of an actual Linux execution.

## A4 reassessment, remaining boundaries and handoff

The retained repair addresses the independent finite recursive-cache boundary, not
the original whole A4 generic expansion/substitution package. OriginalDefinition
normalization, concrete method/containing substitutions, nested/assembly type
rebinding, generic catch equality and expanding T -> List<T> contexts remain
open. Existing A3/A2/generic characterization expectations remain intact.

Next work must return explicitly to bounded generic-context expansion and
recursive substitution, with a sound finite/uncertain contract for genuinely
unbounded constructed contexts, then exact identity/rebinding/catch matching.
No generic correctness is inferred from stable-generic Fault fixtures.
`SolutionTransitive` final Finding/CLI admission and B25B remain CLOSED.

Self Analysis is NOT newly run (historical count 16 only). Canonical Finding Diff
is NOT newly run (historical 0 added/0 removed/0 changed only). No fresh claim.
No candidate was withdrawn: no new independent summary-domain boundary was
found. The unrelated Full harness failure is retained, not opportunistically
fixed. Only one production file changes (the evaluator); the only old test
changes are the expressly authorized four A4 cases, plus associated witness
routing/metadata. The gate gains only two additive A5 lines; the boundary
register gains only one additive A5 paragraph. Exact test/gate patches against
the inherited A4 WIP are in the A5 Proof directory. New tests/proof/report/audit
are separate A5 files; pre-existing A3/A4 WIP is otherwise byte-preserved.

## Final repository/protection state

HEAD remains `c777f9642c145a57c112c28216c3996128c6234b`; no external HEAD movement.
Semantic index clean, no staged paths. Physical index SHA256 remains
`7E38AE2E60BE2F836FC08BD9305155A3D5A7D8323D33975C49CD46EB782B3F8D`.
Root ignore SHA256 remains
`7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5`.
All 23 actual Core files and all six stash entries remain unchanged, including
both named protected position-provenance stashes. No index repair, commit,
push, stash operation, reset, checkout or restore.

Final `git status --short` (the root ignore and A3/A4 entries are inherited):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs
?? Evaluation/P5O2B25A3-constructed-generic-summary-context-audit.json
?? Evaluation/P5O2B25A3-constructed-generic-summary-context.md
?? Evaluation/P5O2B25A3Proof/
?? Evaluation/P5O2B25A4-bounded-generic-context-expansion-audit.json
?? Evaluation/P5O2B25A4-bounded-generic-context-expansion.md
?? Evaluation/P5O2B25A4Proof/
?? Evaluation/P5O2B25A5-recursive-summary-scc-closure-audit.json
?? Evaluation/P5O2B25A5-recursive-summary-scc-closure.md
?? Evaluation/P5O2B25A5Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs
```
