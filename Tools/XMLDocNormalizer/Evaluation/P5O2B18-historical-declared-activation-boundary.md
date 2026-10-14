# P5O2B18 - Historical Declared activation boundary

## Before implementation: reconstructed boundary and decision

Starting HEAD: faed564f92c6cf4fefed02666b71082e64084e0d (B17 committed).
Initial tree: protected root .gitignore only. No production change is made under
this stop decision; no commit, push, index or stash operation is planned.

HistoricalWorkerScopeProjection.TryProject omits Declared from its mode switch.
HistoricalWorkerPayloadProjection.Project additionally requires ProjectTransitive
for a B17 opt-in. WorkerInputValidation.HasSupportedScope accepts only the three
existing engine modes; WorkerDeclaredPolicyContract.HasSupportedRequest requires
project-transitive. These are intentional, mutually consistent admission checks,
not the sole cause of the missing end-to-end capability.

The private productive AnalyzeConfiguredExceptionFlow returns a native
ExceptionFlowAnalysisResult. It creates CurrentCompilation dispatch input and
requires Routing.CurrentAnalysis. AnalyzeExternalExceptionFlowAsync reuses the
B9 seam with validated PortablePdb/Worker input, but returns canonical analysis
and the original validated Historical call, not Findings. B5 deliberately sets
CurrentAnalysis to null for Historical success. No Cross-Version symbol import
exists or should be fabricated.

Reusable Current rules: unrestricted Direct DOC610; complete reporting exception
inventory as the summary guard; source-owned Summary relevance excluding Direct
types; source-owned documented relevance for DOC631/632. B17 already implements
these eligibility predicates on Main with an immutable original-call receipt.
It does NOT implement the rest of the Current detector: semantic cref validity,
exception-type validation, exact-symbol document coverage, inheritdoc suppression, document/member
anchors and context, snippets and final Finding construction. Canonical type
identity does not contain resolved documentation facts. The Current coverage
helpers use exact SymbolEqualityComparer equality, NOT base-type coverage. This
fresh source audit corrects the inherited B17 report's "inheritance coverage"
wording; inheritance is used for exception-type validation, not tag coverage.
The native Add*Finding methods require INamedTypeSymbol, native flow results and
the exact semantic reporting context. Supplying PE symbols as if they were source
symbols would also lose DeclaringSyntaxReferences ownership.

Intended future feed: original validated Historical call -> existing B17 Main
eligibility -> existing Main reporting policy through a separately validated
document/coverage/member-context bridge. Do not re-run the Current analyzer, resolve
Historical symbols with Current, filter raw flows or fabricate Findings from
eligibility alone. Full Finding comparison must precede productive admission.

There is NO minimal admission-only activation using the existing contracts.
Removing the mode checks cannot connect canonical results to native Findings.
Even identical Direct/Summary/declaration facts can yield different Findings
when documentation coverage, invalid tags or inheritdoc change. An unattested
external documented base type is intentionally rejected by B17. The no-local-type
guard also cannot be imitated by executing unguarded ProjectTransitive and discarding
its result, particularly when that execution fails for uncertainty.

Decision: use the explicit B18 independent-contract-gap stop rule. Historical
Declared remains unsupported, with no alias, partial activation or fallback.
General Historical Finding/reporting import is explicitly outside this task.
Tests and evidence will close the root-cause/readiness audit only, not mode support.

## Executable root-cause evidence

Seven new tests in ExceptionFlowHistoricalDeclaredReadinessTests reuse unchanged
B14 genuine PE/PDB/source/reference handoffs and B17 native fixture observations.
Each Historical call uses the REAL external detector entry -> B9 configured seam
-> B8 dispatch -> B7 projection -> B6 Historical selection -> B5 router -> isolated
Historical Worker -> validated B17 original-call receipt -> existing Main policy.
It stops exactly where the existing production contract stops: canonical analysis
and policy eligibility. No test-only mode enablement or new renderer is introduced.

Four fixed-width documentation cases keep executable spans/lines unchanged. They
compare whole native Direct/Summary canonical results and full Historical results,
all declaration/ownership/independent Direct evidence and deterministic repeat.
Source hashes/provenance appropriately differ between different documentation
inputs; each successful call validates its own original input world. Provenance
is NOT falsely compared as equal across changed source text.

| Documentation change | Full native Declared Findings | B17 flow/policy facts |
| --- | --- | --- |
| Exact System.ArgumentException tag | DOC610 removed; local Summary DOC611 retained | Identical; Direct eligibility still true |
| System.Exception base tag | Findings unchanged; DOC610 NOT covered | Identical; unattested documented base query rejects |
| inheritdoc | Missing exception Findings suppressed | Identical; Direct eligibility still true |
| System.String exception tag | DOC670 added; DOC610/DOC611 retained | Identical; no tag-validity Finding contract |

The base-tag control is deliberately NOT a positive difference claim. It confirms
the actual exact-equality coverage rule and preserves the failed first test attempt
that incorrectly assumed base-type coverage. Unknown documented base evidence
returns UnknownExceptionType, not guessed relevance or a complete document policy.

Another genuine input has no local exception declaration and calls GC.Collect.
Actual native Declared Findings and Direct result are empty; the diagnostic raw
Current summary is uncertain. The Historical ProjectTransitive/B17 facts request
selects Historical but returns the original structured AnalysisFailure and no
result/policy. This shows why executing the wrong engine then dropping/filtering
its output cannot implement the productive guard. Diagnostic graph observations
are not claimed to be the productive no-summary session. ToolRunner's existing
guard still creates no productive summary session when the declaration set is empty.

The same-project Root -> Helper.A -> Helper.B case gives full canonical flow parity,
validated local Summary eligibility, deterministic repeat and a real native
ProjectTransitive-versus-Declared Finding difference. The productive Historical
Declared request still rejects before dispatch, including B17 opt-in. Final
Historical Finding parity is not asserted or manufactured from native snapshots.
The seventh test checks the actual native/canonical production contract shapes.

B17's seven original cases separately retain local, framework/external, mixed,
combined Direct/Summary, unused, partial and nested type facts, complete native
Findings, canonical graph states, ownership, provenance and repeat. Its sixteen
response negatives plus query/request controls remain unchanged and run freshly.
They reject missing declarations/ownership/Direct, contradictory assembly/module/
owner paths, incomplete response, invalid/default mode, forged/cloned/unvalidated
evidence and real Worker failures without results, policy receipts or fallback.

## Required 21-point closure

1. Previous unsupported boundary: Main scope-mode admission, Main B17 opt-in
   admission, Worker scope admission and B17 profile admission all intentionally
   reject productive Declared. The separate native/canonical reporting mismatch
   remains after those checks, not merely an unsupported string in one switch.
2. Exact activation change: NONE. B18 stops under its explicit independent-gap
   rule. No production, shared Analyzer, Current detector, Worker, B5/B6 or version
   file changed. The audit/root cause is closed; requested activation is NOT achieved.
3. Production dataflow: genuine original validated handoffs traverse the existing
   B9/B8/B7/B6/B5 Historical route. Final native Finding generation is not connected
   to its canonical output. The private native productive entry uses CurrentCompilation
   and requires CurrentAnalysis; Historical success intentionally has that field null.
4. B17 contract: reused without changing its opt-in ProjectTransitive engine label,
   complete PE-bound inventory, owned declaration paths, independent Direct and
   original-call immutable validation receipt. No Declared-to-Project alias added.
5. Main Finding policy: existing neutral eligibility runs on validated facts.
   Existing native document validity, exact coverage, inheritdoc, positions/context
   and rendering remain Main responsibilities. Eligibility is necessary, not a Finding.
6. Worker duplication: NONE. Worker/source bytes and DLL/PDB are identical to the
   fresh parent. It projects facts through unchanged shared analyzers, not relevance
   or final Findings; Historical Roslyn remains exclusively isolated.
7. Current/Historical final Finding parity: NOT proven and NOT claimed. Full flow,
   fact and bounded relevance parity succeeds, but equal complete evidence can yield
   different actual native Findings. This is the decisive independent contract gap.
8. Difference from ProjectTransitive: original seven B17 scenarios and the new
   Root/A/B chain retain complete differing native Finding sets for identical inputs.
   Historical eligibility reproduces relevance, not the complete final Finding set.
9. Direct control: existing bounded Historical Direct analysis remains unchanged;
   Current Direct behavior and byte baselines remain unchanged. No mode fallback.
10. ProjectTransitive control: original supported single-project analysis and B17
    raw, explicitly labelled ProjectTransitive facts requests remain unchanged.
11. SolutionTransitive control: original supported bounded B15 cross-project DAG
    remains unchanged; Declared is not enabled for solutions or graph inputs.
12. Fail closed: original B17/B16 admission and failure tests are unchanged. New
    documented-world and chain Declared inputs reject before dispatch. No uncertain
    partial policy, unknown-type relevance, mode default, alias or Current fallback.
13. Isolation/cleanup: new successful calls compare pinned compiler identities;
    success/error children are stopped, Main asserts no Historical assembly loaded.
    Final verifier checks no live own Worker/fixture/process. Source is compiled for
    genuine handoffs, not executed as application code. Worker errors are retained.
14. Changed files: one NEW test class; permanent gate filter only; one honest open
    boundary annotation; this report, generated audit JSON and two proof scripts.
    Root-ignore WIP is not part of B18. No production changes or bin/obj products.
15. Tests/builds: fresh census below; first owned compile/test failures retained,
    then corrected tests run through complete final validation. No test exclusions,
    warning/error suppression or opportunistic known-flake repair.
16. Gates: permanent gate now checks the retained readiness boundary, NOT an enabled
    Declared mode. Dual runtime/architecture/isolation, source/production image bytes,
    exact encodings/format/JSON, protection and evidence checks are performed freshly.
17. Eight Current baselines: all whole JSONs byteequal to fresh HEAD parent.
    They contain native Findings, graph counts, repeat and session-lifetime fields,
    not a separate complete flow/provenance dump. Unchanged detector/121 shared
    sources, byteequal production images and existing differentials protect those
    additional flow/source/provenance/uncertainty semantics. Native Current starts no Worker.
18. Self Analysis 16 and Canonical Finding Diff 0 added / 0 removed / 0 changed
    evidence are INHERITED, NOT freshly run. Default Category!=SelfAnalysis
    configuration is unchanged; no configured test is skipped.
19. Open boundaries: productive guarded Declared and bounded semantic document/
    coverage/member-context integration; general Historical Finding/reporting import
    and CLI; XML Documentation Contracts/uncertain external evidence; broader solution
    loading/scope/signatures, signed targets and unsupported refs/shapes/generics.
    None is opportunistically closed by this package or its boundary annotation.
20. Final status: listed below and independently recorded by the final verifier.
    Protected root ignore, all 23 Core files, six stashes and index remain unchanged.
    All builds use a clean HEAD archive plus exactly the new test and changed gate;
    original Core bin/obj are not rebuilt. No commit, push or stash/index manipulation.
21. External HEAD movements: none observed; starting/final faed564f92c6cf4fefed02666b71082e64084e0d
    on fix/documentation, checked repeatedly and by final verification.

## Four-mode assessment (bounded ANALYSIS support, not general Finding import)

| Mode | Historical status after B18 |
| --- | --- |
| Direct | Existing bounded analysis scope supported, unchanged |
| ProjectTransitive | Existing bounded single-project scope supported, unchanged |
| SolutionTransitive | Existing bounded B15 solution DAG supported, unchanged |
| ProjectTransitiveDeclaredExceptions | Unsupported; independent reporting/guard integration remains open |

## Required follow-up before activation

B17 had already flagged remaining document/reporting context. B18 confirms its
concrete consumer gap and counterexamples; it does not pretend this warning was
an already completed contract. Separately scope a bounded source-owned documentation/
member-context contract (valid cref and exact canonical coverage, invalid/non-exception
tags, inheritdoc and positions/context), consumed by the SAME Main Finding policy.
No Worker duplicate detector, name matching, Current symbol substitution, raw-flow
filtering or fallback. Establish validated owned declaration facts BEFORE summary
execution so the independent Direct guard is an execution choice, not recovery
after a failed ProjectTransitive run. Preserve full input/provenance binding and
fail-closed invalid contexts. Then require actual whole Historical Findings from
the real production entry to match all native Current cases before mode admission.
This follow-up must not silently expand into general reporting import/CLI/solution
loading. No partial admission change is included here.

## Fresh validation census

All final groups actually completed after the exact final test/gate overlay and
final Dual-Version build. Zero failed or skipped configured tests. Overlapping
groups must not be added as a unique total.

| Validation | Passed / total |
| --- | --- |
| Fresh unchanged HEAD parent boundary | 450 / 450 |
| B18 readiness witnesses | 7 / 7 |
| B17 unchanged | 25 / 25 |
| B16 unchanged | 18 / 18 |
| B15 unchanged | 28 / 28 |
| B14 unchanged | 31 / 31 |
| B13 unchanged | 34 / 34 |
| B12 unchanged | 29 / 29 |
| B11 unchanged | 16 / 16 |
| B10 unchanged | 34 / 34 |
| B5-B9 unchanged | 156 / 156 |
| Productive | 681 / 681 |
| Focused | 667 / 667 |
| Broad, diagnostic Blame | 2968 / 2968 |
| Full configured suite, diagnostic Blame | 3059 / 3059 |
| Final permanent boundary gate | 457 / 457 |

Dual-Version PASS; eleven WAE build logs each contain zero warnings and errors:
three Current, three Historical, two Worker, isolated boundary process, genuine
Historical PDB fixture and Main test project. All six Current/Historical and both
Worker DLL/PDB captures are byteequal to fresh HEAD parent, not merely source-equal.
The shared 121 source files and Historical host are unchanged. All eight whole
Current JSON baselines are byteequal. Native Current worker/session behavior is
covered by the unchanged production parity tests and those baselines.

Format, Git whitespace, strict UTF8/no-BOM/CRLF, PS parsing, JSON, protected/source
hashes, final built-overlay hashes, runtime/architecture/isolation and Evidence
Verifier PASS. The final verifier repeat binds this completed report and final
status to the generated audit. No live own isolated Worker/fixture/process remains.
Zero tracked path case collisions; all six ProjectReferences resolve to exact-case
tracked Git targets. Actual execution is Windows SDK 8.0.418; NO Linux or remote
execution is claimed. There was no external HEAD movement.

## Attempts retained (not hidden by final totals)

The first new test-project build had 0 warnings and 3 errors: a nonexistent local
transport AnalysisFailure enum value and two missing native DTO namespace uses.
Only the NEW B18 test was corrected to check StructuredFailure plus the original
WorkerFailureCode.AnalysisFailure and import Models.DTO. The complete first console
diagnostics remain in the tool transcript (execution 9d102d); no first-build disk
log was captured, explicitly recorded in attempts.json. Second build: 0/0.

First B18 run: 5 passed / 1 failed / 0 skipped, retained b18-first.trx and log.
The new cover-base test incorrectly assumed inheritance coverage. A fresh source
inspection established exact SymbolEqualityComparer equality in both Current
coverage helpers. No production code was changed. The corrected exact-type case
and explicit unchanged base-type control expanded the class to seven tests.
Third build: 0/0; second B18 run: 7/7. Final gate: 457/457; all fourteen final
regression groups above freshly passed. Broad/full use --blame; the known unchanged
reader-lifetime native crash did NOT recur. No automatic retries, test exclusions,
warning/error suppressions, or opportunistic flaky-test changes were made.

## Owned files and reproduction

Seven owned files: Tests/XMLDocNormalizerTests/Worker/
ExceptionFlowHistoricalDeclaredReadinessTests.cs; build/Verify-DualVersionBuild.ps1
(filter only); Evaluation/OPEN-PIPELINE-BOUNDARIES.md (one audit annotation);
this report and P5O2B18-historical-declared-activation-boundary-audit.json;
P5O2B18Proof/Run-Regressions.ps1 and Write-Verify-ReadinessEvidence.ps1.
All pre-existing B5-B17 tests and all production files remain byte-identical.

Recorded local artifacts: artifacts/p5o2b18 contains protected-before.json
(909 tracked tool files, 23 complete physical Core files, six stash hashes,
original empty index/root-ignore hash), head.tar, export.json, exact two owned
build inputs, attempts.json and the six-reference Git census. Protection snapshot
was taken after writing the initial pre-implementation audit, before test/gate
changes; its status therefore also records that new report, not a foreign WIP.
Build export: artifacts/b18-faed564/Tools/XMLDocNormalizer. Parent-dual and eight
parent-current snapshots are retained alongside it. Final export holds fourteen
TRX/log groups, Dual audit/eleven build logs, all B17 and B18 full runtime witnesses,
negative evidence and initial failed/passing attempts. Machine-readable audit embeds
the six B18 witness cases, seven B17 cases and sixteen negative response witnesses,
whole native Findings, canonical flows/sources, graph data, declarations/ownership,
provenance, original call identities, repeat and policy decisions; it explicitly
sets HistoricalDeclaredActivated=false and FullHistoricalFindingParityProven=false.

From the recorded HEAD archive and exact two test/gate overlay inputs:

```powershell
# Run in the recorded clean export:
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
# Run the proof scripts in the authoritative workspace with its protection records:
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B18Proof/Run-Regressions.ps1 -ValidationRoot <export-tool-root>
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B18Proof/Write-Verify-ReadinessEvidence.ps1 -ValidationRoot <export-tool-root>
```

The verifier requires the recorded archive/protection/source hashes and fresh TRX,
not stale totals alone. It checks all unrelated tracked files, full protected Core
inventory, root ignore, stash/index/HEAD, eight Current baselines, exact production
images, full case data, negative admission and child cleanup. Its positive result
is an evidence/protection/readiness PASS, never a claim of Declared activation.

## Final git status --short

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B18-historical-declared-activation-boundary-audit.json
?? Evaluation/P5O2B18-historical-declared-activation-boundary.md
?? Evaluation/P5O2B18Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs
```

Root-ignore is preserved pre-existing WIP, unchanged by B18. All seven owned files
remain unstaged. Final index is empty. No commit, push, reset/restore/checkout or
stash mutation. Requested end-to-end activation is NOT complete; the explicitly
permitted independent-contract-gap audit is completed with a clean bounded stop.
