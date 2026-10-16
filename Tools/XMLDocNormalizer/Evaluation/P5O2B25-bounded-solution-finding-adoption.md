# P5O2B25 — Bounded Historical SolutionTransitive Finding Adoption

Outcome: **stop-rule boundary audit; no SolutionTransitive Finding/CLI activation**.
All production files are unchanged from HEAD. Validation closure is recorded below.

## Phase 1: Current contract (recorded before production edits)

Entry HEAD: `f93d2e7316fe2f4bcfe4b4bf4d81b2e8e41ba17e`. B24 and PID-G1
are committed. Only protected root `.gitignore` is dirty; index is empty.
`artifacts/p5o2b25/protected-before.json` fingerprints the index, ignore,
23 physical Core files and all six stash reflog entries. No network, commit,
push, stash or index operation is authorized.

Current's final Finding is created in `XmlDocExceptionSemanticDetector.Findings.cs`
by `AddExceptionFindings` / `AddMissingExceptionFindings`, called from the native
`FindExceptionSmells` member loop. Only documented members in the reporting
target's enumerated source tree enter this loop. The `Finding` anchors either
the root member (missing documentation) or its exception XML tag (invalid or
unjustified documentation). A throw in B/C does **not** move that anchor to B/C.
`ProjectClosureSemanticContext` associates exact trees with compilation scopes;
supporting projects contribute callable bodies, not reporting targets. For this
one-root external contract, A alone is the reporting project. No path heuristic
creates ownership, nor is a separate Finding emitted per canonical flow path.

`ExceptionFlowSemanticEnvironment`, the shared cross-compilation resolver and
summary session follow exact reference-bound owned scopes. B15 already projects
original PE/PDB/source/reference receipts to that graph and reconstructs it in
the actual Historical worker. Its complete canonical entries, paths, summaries,
graph count, owning source scopes and deterministic repetition match Current for
A→B, A→B→C, and a source-defined exception in B. This is analysis evidence,
not an existing final Finding import contract.

Current independently collects Direct evidence. A documented SolutionTransitive
root always requests Summary (unlike the Declared inventory-dependent guard).
The shared Main policy emits missing Direct types first, then transitive types
excluding Direct and exactly documented identities; hash-set type identity
deduplicates paths/callees. No new Historical deduplication is needed. Ordinary
Current uncertainty can yield DOC608; bounded external admission instead already
requires complete, proven, untruncated results and rejects incomplete execution.
No documentation skips the summary only **after** trusted Direct/doc admission.

The public Finding has root/tag path, location, smell, arguments and context;
it has no SourceProject field. Source method/project and exception-type owner
remain in canonical flow, graph and exact project/reference provenance, and must
be compared alongside the full Finding, not fabricated into the reporting DTO.

## Contract matrix

| Information | Current SolutionTransitive | Historical B15 | For Finding? | B25 measure |
| --- | --- | --- | --- | --- |
| Root project identity | Reporting scope/compilation | Original assembly/module/MVID graph target | Yes | Reuse exact graph target |
| Root member identity | Exact declared member | Exact selected PE/source method | Yes | Bind B24 root documentation to graph target |
| Reporting target | Root member/XML tag in A | Selected root, no Finding import | Yes | Same Main root/tag anchor; never supporting target |
| Source project identity | Exact tree-owning scope | Explicit node source manifest | Evidence | Reuse; globally unique logical paths required |
| Supporting project identity | Explicit reference closure | Independently validated P4/P5 receipts | Evidence | Reuse; chain of 2 or 3 only |
| Method ownership | Shared exact cross-compilation resolver | Same shared resolver/graph | Evidence | Reuse, compare summaries and scopes |
| Type ownership | Exact symbol assembly/module | Canonical type plus original reference world | Yes | Reuse; resolve displays against retained validated PE worlds |
| Exception type identity | Symbol identity | Canonical full identity | Yes | Existing Main canonical policy adapter |
| Reference identity | Exact compilation reference | Ordered images/full identity/MVID/checksum | Yes | Existing per-node B12 projection and validators |
| Source LogicalPath | Exact owned tree | Per-node complete P5J manifest | Yes | Reuse, no duplicate cross-project paths |
| Direct evidence | Independent local collector | Local-mode evidence available | Yes | B24 independent Direct contract on graph root |
| Summary evidence | Shared summary session | Complete canonical root result | Yes | Reuse actual B15 execution, not a new graph evaluator |
| Canonical flow | Full paths/evidence/uncertainty | Full differential established | Yes | Existing validation plus B21 receipt |
| Graph edge / ownership | Shared summary graph | Exact reference-derived DAG and summaries | Evidence | Reuse; restrict Finding admission to proven chains |
| Documentation binding | Root syntax/cref semantics | No final Finding receipt | Yes | Reuse B24 local root facts, explicit Solution profile |
| Provenance | Owning source/reference worlds | Full graph ReferenceWorldSha256 | Yes | Preserve graph in receipt and Direct/Summary agreement |
| Uncertainty | Native policy handles uncertain paths | Worker rejects incomplete results | Yes | Preserve stricter bounded fail-closed boundary |
| Finding context | Root/tag declaration context | Not imported | Yes | Existing Main syntax-derived context adapter |
| Analysis mode | SolutionTransitive | Explicit solution-transitive | Yes | Distinct mode/profile, no aliases |

## Trust decision before implementation

B21 already brands the **entire serialized** request and exact response with
original payload reference, completed owned-process exchange, EOF, diagnostics,
exit and actual configured Worker image before/after execution. The serialized
request includes every B15 node, ordered sources/references, graph target and
dependencies. `ReferenceWorldSha256` additionally covers that graph. Therefore
there is no new independent attestation mechanism to invent. The existing
original-origin registry must register the final graph payload, not a local
intermediate or caller copy. Direct/Summary agreement must compare full graph
ownership, not merely A's local scope. Structural coherence alone is never
authority. Existing PID-G1 observation remains untouched.

Minimal planned additions: explicit Solution Finding profile/mode; reuse of
B24 root facts on the existing B15 graph; graph-bound immutable ownership receipt;
raw local supporting-project materials in the existing v1 job; existing P4/P5
validation for each node; final shared Main policy. No worker Finding policy,
new analyzer/graph engine, routing layer, or compiler-version change. If an
independent trust/policy gap appears during proof, leave Solution unsupported
and close as a boundary audit instead of partial activation.

## Actual independent ownership gap and stop decision

The proposed additive adapter passed its first 40 tests, including full final
Finding-set, canonical flow/graph, root-target, repeated execution, genuine CLI
and wrong-physical-worker replay checks. Those experimental results are **not**
a final supported-scope claim. A subsequent proof-strengthening run added two
deduplication cases and explicit native tree/project comparison. It passed
41/42. The same-declaration-name test expected two Findings but observed one,
identically in Current and Historical. The initial count expectation was an
assumption, not evidence of Current semantics. An explicit isolated inspection
kept that assertion failing and recorded the actual summaries and result.

The independent issue is not the count itself. Both A and B define their own
`Shared.Fault : System.Exception`. The local canonical summary in A identifies
`Original.Solution.P0`; the local summary in B correctly identifies
`Original.Solution.P1`. The evaluated root result contains one type owned by A,
yet its paths include the explicit throw in B. Thus exact local ownership is
available **before** evaluation but is lost during root normalization. Reporting
still anchors A correctly. Source-path ownership and exception-type ownership
are different facts: correct A reporting and B source paths do not prove correct
B exception-type identity.

Root cause: `ExceptionFlowSummaryGraphEvaluator.NormalizeSummaryExceptionType`
creates the original type's DocumentationCommentId and calls
`DocumentationCommentId.GetFirstSymbolForDeclarationId(id, rootCompilation)`.
It accepts that result without checking assembly identity. Both types have
`T:Shared.Fault`, so root A's declaration is substituted for B's declaration.
`AddSummaryLocalSourcesToResult` applies this normalization to each local source
before graph merging. The separate `ExceptionFlowCrossCompilationResolver`
already filters assembly identity, but this evaluator normalization does not use
that resolver. The canonical adapter faithfully exports the already-normalized
result; JSON, response validation, B21 and the CLI are not the loss point.

This is shared Current/Historical analyzer behavior, not a new worker transport
defect. Full Current/Historical parity alone therefore cannot establish the
requested exception-type ownership invariant. B21 correctly attests execution;
it does not certify the semantic correctness of that execution. No independent
B21 graph-binding gap was found and no replacement attestation was built.

The prompt's stop rule applies: do not introduce an ownership heuristic, a
Historical-only deduplication policy, an analyzer fix, or partial activation.
The eight own production edits were preserved with hashes under
`artifacts/p5o2b25/activation-attempt`, then reversed with explicit patches.
No reset/restore/checkout/index/stash operation was used. `git diff --quiet HEAD
-- src` succeeds. Only the new audit tests and their additive gate remain.

Required follow-up: **P5O2B25A — Assembly-preserving Exception Normalization and
Cross-Project Type Ownership Contract**. It needs a separate shared-analyzer
semantics decision, assembly-aware root symbol normalization (or retained
original type when no exact root-world symbol exists), collision/alias/version
and typed-catch/deduplication differentials, and complete Current regression.
Only afterwards should the B25 Finding/CLI adapter be reconsidered. This report
does not prescribe an unverified one-line fix or automatically authorize it.

## Final contract and scope decision

- Root project/reporting target: A; missing tags anchor A's selected member,
  XML problems anchor A's XML tag. B/C never become automatic reporting targets.
- Source projects/methods: exact owned source scopes and complete canonical
  steps, not inferred from path spelling. Globally duplicate logical paths remain
  outside the B15 profile.
- Exception owner: local summaries distinguish assembly identity, but the
  observed shared normalization can lose it. This is the unresolved final
  ownership/deduplication boundary, not a reason to relabel B's type as A's.
- Existing B15 A→B / A→B→C flow/graph support is unchanged. New audit cases
  reuse original P4/P5/P5J/reference factories, actual pinned worker execution,
  native Current summary sessions and the existing B15 graph observer.
- Fifteen final baseline scenarios cover both chains, source-defined B exception,
  root and cross-project throw paths, repeated supporting methods/call paths, throws in both
  B/C, exact/base/invalid docs, inheritdoc, no docs and empty flow. They compare
  complete flow/graph/repetition and native Findings, then prove that the final
  Historical Finding operation still rejects before dispatch.
- S1/S2 experimental full final-Finding parity existed, but is withdrawn as an
  admission result because the overall ownership invariant was not proved.
  S3 is not claimed closed: the exploratory field-guarded mixed fixture did not
  establish nonempty independent Direct evidence (the same-name inspection's
  Direct result was empty). No additional activation proof is pursued after
  the stop rule. S4's repeated-call/source observations demonstrate why
  path/type deduplication must use the shared policy, not one Finding per flow.
  The new B/C same-framework-type case
  explicitly checks throw steps in both supporting projects and one native Finding.
- S5: supported B15 canonical empty analysis succeeds and Current has zero
  Findings. Final Solution Finding/CLI remains visibly unsupported (exit 4),
  never a fabricated successful empty report.
- No new evidence/profile/enum/job field remains in production. The proposed raw
  `supportingProjects` materials and graph-bound receipt are archived experiments,
  **not** an accepted job schema or supported CLI example.
- Three real CLI runs (two/three-node fixtures and empty input) verify exit 4
  and no JSON report. Two further executions replay the complete raw A→B and
  A→B→C jobs that succeeded in the withdrawn prototype, against HEAD-identical
  Main: their `supportingProjects` schema is rejected, exit 4, no report. Thus
  the final check is not limited to jobs omitting their graph materials.
  There is no final positive CLI-E2E claim. Existing B23/B24 positive
  Console/JSON/SARIF and single-counting coverage remains the reporting regression.
- Seven additional raw graph substitutions reject root/source/dependency
  project, reference MVID, edge, conflicting source ownership and root member
  before process start. Original B15 structural/missing-source/completeness
  negatives and unchanged B21/B24 trust/replay/worker/budget failures are rerun.
- Unsupported: all final external Solution Findings; cross-project Declared;
  branching/diamond/cyclic/larger/linked-path graphs; multi-root/general roots;
  signing, extra reference/compiler/options/XML profiles; discovery/network/
  MSBuild or general Solution loading. No new scope is supported by B25.

## Validation attempts and binary provenance

The unmodified HEAD was first exported without bin/obj/artifacts and built using
existing explicitly mapped local package caches only. Baseline Dual, its eleven
WAE steps, 596 Boundary, 36 B23 and 42 B24 cases passed. Eight complete Current
JSON baselines were captured before any overlay. Protected original Core outputs
were never built. All builds/tests run in the disposable export, not the original
workspace.

Retained attempts: new-test WAE initially failed on three nullable dereferences;
only the new test annotations/assertion were corrected. WAE then passed. The
40-case prototype passed; the expanded 42-case run and one-case inspection failed
on the explicit two-Findings assumption above. There was no PID flake, retry
policy, test exclusion, skip or random rerun-until-green. The final audit test
characterizes the **observed ownership loss and closed admission**, not a reduced
positive-activation assertion. Both attempted activation and failures remain
available; they are not counted as successful final adoption.

An initial incremental audit build was also invalidated: restoring source bytes
from an archive restored older mtimes, so MSBuild retained the prototype Main/
Worker DLLs. Their hashes exposed this before accepting a final gate. Those 27
passing tests are not used for closure. The runner now explicitly cleans the
export's Current and Worker projects before rebuilding. Fresh rebuilt hashes
match the unmodified HEAD baseline exactly: Main
`B73EC07AD8DF0F5526C3BF6FB97CCFF3F927C2D093EDDDF2149A720272EEE278`, Worker
`9DEA0D7047B7D8B4CA4C0139E69A9BE7791B404BA8D5E84198C31C46B8D35D3E`.
The final compiled-input snapshot records the new test image and exact source
hashes before the final audit/regression runs.

No fresh Self Analysis or standalone Canonical Finding Diff was executed in B25.
Self Analysis **16** and Canonical Finding Diff **0 added / 0 removed / 0 changed**
are inherited from the earlier closure evidence, not new B25 results. The eight
Current baseline comparisons below are fresh regression checks, not a renamed
Self Analysis or full standalone Canonical Finding Diff.

An intermediate format check found LF in the new raw-graph Proof script; owned
text formatting was normalized and verification then passed. The compiled C#
source hash remained identical. A read-only optional process-command-line census
was denied by the sandbox; no global "no processes remain" assertion is derived
from it. The actual held-process/exit observations in tests and PID-G1 proof
remain the lifetime evidence.

## Fresh final regression census

All completed rows have zero failed/skipped tests. The expected prototype
failures above remain separately retained, not relabelled as passes.

| Group | Passed / total |
| --- | --- |
| New B25 audit | 27 / 27 |
| Unchanged PID-G1 | 18 / 18 |
| Deterministic PID-G1 Proof | 9 / 9 |
| Unchanged B24 | 42 / 42 |
| Unchanged B23 | 36 / 36 |
| Unchanged B22 | 46 / 46 |
| Unchanged B21 | 26 / 26 |
| B20 / B19 / B18 / B17 / B16 | 10 / 10; 39 / 39; 7 / 7; 25 / 25; 18 / 18 |
| Unchanged B15 | 28 / 28 |
| B14 / B13 / B12 / B11 / B10 | 31 / 31; 34 / 34; 29 / 29; 16 / 16; 34 / 34 |
| B5–B9 | 156 / 156 |
| Productive | 804 / 804 |
| Focused | 911 / 911 |
| Broad | 3212 / 3212 |
| Full suite | 3303 / 3303 |
| Standalone Boundary | 596 / 596 |
| Full Dual Boundary / B23 / B24 / B25 | 596 / 596; 36 / 36; 42 / 42; 27 / 27 |

All eight fresh complete Current baseline files are byteequal to the captured
unmodified HEAD: Direct, ProjectTransitive, ProjectTransitiveDeclaredExceptions
and SolutionTransitive, each with both fixture variants. Ordinary Current
project/solution execution and shared Main Finding policy are unchanged.

The actual full Dual command with `-RunMainBoundaryTests
-BuildTestProjectReferences` passed as one execution, not a stitched follow-on.
All eleven fresh WAE build logs have zero warnings/errors. Runtime compiler/image
identity, deterministic genuine isolated worker analysis, architecture and
isolation gates pass. No PID flake, native abort or failed final regression was
observed; no retry/exclusion/skip was required. A read-only freshness spot-check
initially mixed local and UTC DateTime kinds; the corrected explicit UTC check
confirmed all eleven logs fresh. No build failed in that spot-check.

The B25 Evidence verifier passed: actual TRX counters/freshness, exact compiled
source and DLL hashes, retained failing attempts, ownership witness, rejected
complete raw graph CLI jobs, eight Current baselines, 121 shared analyzer source
fingerprints, unchanged production/routing/version/old-test inputs, JSON,
PowerShell syntax, format, UTF8/no-BOM/CRLF, `git diff --check`, Git path case
collisions and repository protection. The final documentary refresh is verified
again after this closure text is written. This is Windows validation with SDK
8.0.418, **not** a claimed Linux CI execution; the Git inventory/casing check is
the Linux-compatible static check. Old package-specific audit files were not
rewritten to imply new results.

## Binary changes

No production DLL change remains. Fresh Main and Worker hashes equal the HEAD
baseline shown above. Historical Analyzer is also byteequal:
`196E0C50B405CA70E46512DDDBC0B9A59F86A39D5D42A1F70934EAADDEB50FF8`.
All 121 shared analyzer inputs and actual pinned Roslyn image identities are
unchanged. The new audit tests produce test image
`F5DAC67C202C628EF1DA8983AE7AE9B39D391677713B41644BC3ED587EBA3EDC`;
the baseline test DLL hash was not captured, so no invented before/after test
hash comparison is claimed. All binary changes/build output stay in the export;
the original protected Core inventory remains unchanged.

## Repository / boundary closure

BND-P5O2B-005 remains partial. Its register now names the proven shared
normalization/type-ownership gap and B25A, rather than claiming all four external
Finding modes or general Historical solution readiness. Existing B15 analysis
and B24/PID-G1 closures are preserved.

Final owned changes: the 27-case audit test, two additive build-gate lines,
this report/audit, five scripts in `Evaluation/P5O2B25Proof/`, and the narrow
boundary-register update. No production source, old test, project/package/version,
analyzer, routing, selection or attestation/lifetime code remains modified.

Entry/final HEAD: `f93d2e7316fe2f4bcfe4b4bf4d81b2e8e41ba17e`; no external HEAD
movement. Index hash is unchanged and nothing is staged. Root `.gitignore`
hash/WIP is unchanged, all **23** protected Core files and all **six** stash
reflog entries are unchanged, including the two explicitly protected provenance
stashes. No commit, push, stash application/deletion/rename or unrelated edit.
The own withdrawn activation files remain recoverable in the preserved attempt;
no user WIP was discarded.

## Final git status --short

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B25-bounded-solution-finding-adoption-audit.json
?? Evaluation/P5O2B25-bounded-solution-finding-adoption.md
?? Evaluation/P5O2B25Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs
```

Decision: **B25 boundary audit complete under the prescribed stop rule.
SolutionTransitive final Findings/CLI are still unsupported.** Resolve the
independent B25A shared ownership contract before reconsidering adoption.
