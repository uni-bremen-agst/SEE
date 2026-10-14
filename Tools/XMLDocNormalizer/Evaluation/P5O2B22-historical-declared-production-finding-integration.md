# P5O2B22 – Historical Declared Production Finding Integration

## Phase 1: audit before production edits

Starting HEAD: `6a603804108eee6a06803915332815b4df41e036`, branch
`fix/documentation`. Only pre-existing change: root `.gitignore`; protected.
The initial snapshot records 932 tracked tool files, 23 physical Core files,
six stashes, the index and HEAD. Validation uses a clean HEAD archive, not
workspace `bin/obj`.

1. **Rejections:** `HistoricalWorkerPayloadProjection.Project` rejects an
   evidence opt-in unless the engine mode is `ProjectTransitive` and there is
   an original complete P5J project without supporting projects.
   `HistoricalWorkerScopeProjection.TryProject` has no Declared mode mapping.
   `WorkerInputValidation.HasSupportedScope` and
   `WorkerDeclaredPolicyContract.HasSupportedRequest` also reject it. B9
   `AnalyzeExternalExceptionFlowAsync` returns flows, not Findings.
2. **Admission versus readiness:** These mode checks protect the still
   unimplemented productive Declared path; removing them alone is incorrect.
   Original P4/P5A/P5C/P5E/P5G/P5I/P5J receipts, exact references, selector,
   source/parse scope, complete B17 facts, B19 validation and B21 execution
   attestation are real admission requirements and must remain.
3. **Current guard:** `XmlDocExceptionSemanticDetector.FindExceptionSmells`
   skips undocumented members before analysis.
   `AnalyzeConfiguredExceptionFlow` returns Direct for Direct mode or for
   Declared without `HasDeclaredExceptionTypesInReportingScope()`. Otherwise
   it dispatches the existing summary session through the B9 seam.
4. **Inputs:** explicit mode, documentation presence, the complete reporting
   scope's declaration inventory, independent Direct, and (only when selected)
   Summary. The guard does not inspect observed Summary types, cref relevance,
   or worker-supplied relevance booleans. Uncertainty affects DOC631/DOC632
   downstream; bounded Historical incomplete analysis already fails closed.
5. **Evidence:** B19 provides immutable original-member context, tag syntax,
   bound identities, inheritance/display facts, complete local definitions and
   ownership, independent Direct and canonical Summary. B21 admits this only
   for the original successful call from its exact one-use request/response/
   physical-image exchange. No raw/cloned/replayed call is sufficient.
6. **Finding policy:** the detector's existing Add/Collect/IsRelevant helpers
   implement DOC660/670, exact cref coverage, DOC610 for all Direct exceptions,
   DOC611 for Summary-only locally defined exceptions in Declared, and
   DOC631/632 for relevant documentation. Inheritdoc suppresses missing tags
   only. External Summary exceptions are not Declared-relevant; Direct ones
   remain relevant. This policy and Finding construction belong to Main.
7. **Smallest intended path:** a bounded productive external Finding entry in
   this same detector, distinct from the legacy evidence-only B9 entry (whose
   old unsupported controls stay unchanged), and one shared Main rendering
   path. Genuine attested Direct/declaration/document evidence must precede
   the shared Main guard; only if selected may Main request genuine Summary.
   A narrowly explicit Worker execution request may be necessary because the
   current host always executes Summary before producing its evidence. That
   is an already-known ordering/integration gap, not a new Stop root cause.
   The Worker must not decide guard or relevance; no Project alias, native
   rebinding, Current fallback or shared-analyzer change is acceptable.

## Integration outcome (report items 1–7)

The bounded productive Declared Finding entry is implemented. All final
aggregate regressions and the complete dual-version gate passed. The final
census and machine-readable verifier result are recorded below.

The remaining boundary was **execution ordering plus Main Finding adoption**,
not a new analyzer algorithm. Merely admitting the enum would still execute
unguarded Summary, and merely accepting B21 facts would still return flows
without Findings. No further independent semantic/contract/trust gap was
found in the proved v1 scope; the Stop rule was not invoked.

Productive call chain:

1. `XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync` requests
   bounded `DirectEvidence` through `HistoricalWorkerPayloadProjection.ProjectForFindings`.
2. Original P4/P5A/P5C/P5E/P5G/P5I/P5J source/reference/selector checks remain
   mandatory. Scope is a complete single project and the original selected
   ordinary, static, nongeneric, parameterless root method, not a solution DAG.
3. `AnalyzeConfiguredExceptionFlowAsync` enters the existing B9/B8/B7/B6/B5
   dispatch/selection/router and B4 client. The logical mode really is
   `project-transitive-declared-exceptions`, not `project-transitive`.
4. The genuine Worker executes the unchanged Direct collector and unchanged
   declaration/document emitters. B21 validates its exact completed exchange
   and physical image before the validation-owned immutable receipt can be
   admitted by `TryAdmitFindingEvidence`.
5. Main applies the existing documentation skip and the **same**
   `RequiresSummary(mode, hasDeclaredTypes)` predicate used by Current.
   Declaration inventory is complete, including unused types, not inferred
   from the observed flow. No documentation skips Finding generation; no
   reporting-scope Exception definitions selects independent Direct.
6. Only when that Main guard selects Summary does the same chain execute
   `SummaryEvidence`. Its own B21 receipt is required. Direct result,
   definition/ownership snapshot, root, context and complete ordered tag
   bindings must agree across both executions before any Finding is rendered.
7. `RenderFindingEvidence` supplies trusted canonical identities to the same
   `AddExceptionFindings<T>` / `AddMissingExceptionFindings<T>` Main detector
   implementation used by native Current symbols. It constructs actual
   `Finding` objects with the retained original Main anchors/context/snippets.

This is an explicit **productive Finding operation**, not a test flag. It lives
in the existing detector and uses its existing production dispatch seam.
The legacy B9 **flow/evidence-only** entry intentionally still rejects Declared:
its old B18/B19/B20/B21 assertions and their emitted unsupported flags refer to
that entry. They are unchanged and do not describe the new typed Finding
operation. Ordinary/comparison CLI discovery/loading is deliberately not
extended by B22.

### Shared Main policy and exact guard inputs

Current retains native symbol equality, original enumeration order, actual
inheritance/type resolution and actual source ownership. Historical uses
validated canonical identity equality, PE-checked inheritance/displays, the
complete attested definition inventory and immutable original context. No
Historical Roslyn symbols, native reconstructed compilation or worker relevance
boolean enter Main rendering. Historical output uses stable canonical type
order; parity compares **complete Finding sets**, not a promised native hash-set
enumeration order. Repeated Historical emission is additionally byte-equal.

- DOC660: nonempty syntactic cref with no resolved symbol; DOC670: resolved
  named non-Exception type. A resolved non-type symbol produces neither.
- DOC610: all proven Direct Exception types, including framework/external
  types; exact valid documentation coverage, not base-type coverage.
- DOC611: Summary types not also Direct; in Declared only locally defined
  Exception types and relevant local documentation participate.
- DOC631/DOC632: the same documented-type relevance, proven-flow coverage,
  uncertainty suppression and external-XML suffix as before. Current native
  uncertainty behavior is preserved. Historical incomplete/truncated selected
  evidence remains outside B17/B19 completeness and fails closed.
- Inheritdoc suppresses missing DOC610/DOC611 only; other findings retain their
  existing policy. No documentation skips all findings for that member.
- Ownership/type information selects relevance; original member/tag context
  selects target, file, line, column, snippet and reporting metadata. The guard
  itself does not inspect cref coverage, observed Summary types or uncertainty.

The neutral per-tag record is a private representation adapter inside the same
detector, not another policy service or orchestration framework. Native private
signature seams used by unchanged B16–B18 controls remain present.

### Necessary Worker change

The old host always ran Summary before creating B17/B19 facts. B21 authority
therefore could not reach a pre-Summary Main guard on its own. A nullable
`DeclaredExecution` purpose (`DirectEvidence` / `SummaryEvidence`) now allows
Main to request these two executions explicitly. Worker only selects the
existing Direct collector versus existing Summary engine; **it does not decide
whether Main needs Summary or what is relevant**.

The optional envelope field is bound by B21's exact request witness. Protocol
number remains 2; absence preserves legacy wire bytes and behavior. Unknown,
default, mismatched mode/purpose, absent document opt-in and unsupported scope
are rejected. A Direct-phase result must equal independently computed Direct.
The existing `SummarySha256` document slot binds the effective result; a
Direct-phase result is explicitly identified as Direct evidence, never
misrepresented as an executed Summary. Selected Summary remains genuinely
executed Summary. No new compiler provenance or reference/source acquisition
layer was introduced.

## Differential evidence (items 8–9)

Eighteen real prepared external inputs run twice through the productive entry.
They compare full Findings (rule/message/tag/file/line/column/snippet/context/
target), actual type identities, Direct and selected canonical flows with every
path/source/location, declarations and ownership, full documentation/binding/
display/hash evidence, B21 execution authority, truthful provenance, uncertainty
and deterministic repetition. All four native production modes are captured
for each input. The selected graph decision agrees with actual Current graph
population, not merely a proposed guard boolean.

| Required case | Actual witness |
| --- | --- |
| A – local | Local Summary exception produces the same complete DOC611. |
| B – external | Framework Summary exception is irrelevant in Declared; with no local declarations Main skips Summary and returns no Finding. |
| C – guard | `guard-direct`: proven direct `ArgumentException` plus an unavailable `GC.Collect` call, no local definitions. Current and guarded Historical return the same DOC610 with zero Summary graph. The real unguarded Project Worker fails incomplete analysis and cannot return that final Finding. This proves final-Finding availability, not a new relevance rule. |
| D – combined | `mixed` yields the same DOC610 plus local DOC611; `combined` retains Direct and Summary paths for the same local type but emits DOC610 only for it. |
| E – chain | Root → `Helper.A()` → `Helper.B()` → `LocalException`, three actual graph nodes, exact three-step cross-file flow and matching full DOC611. |
| F – non-alias | For identical local/external inputs Project and Declared have different complete Finding sets, reproduced over genuine Historical evidence by the same Main renderer. |

F is a **bounded policy differential** over the actual successfully attested
Project call and the actual productive Declared result. It does not activate a
general Historical Project reporting API. Exact Current Project Findings are
compared with the real common Main renderer's Historical Project Findings;
no Current result is copied into Historical output.

For local, combined and chain cases, the existing genuine B14 beta child graph
probe additionally compares full summary states, counts, repeat and canonical
result over the same source/reference world. This is explicitly an auxiliary
actual Historical graph probe; the productive Worker has not acquired a graph
reporting DTO or a second analyzer implementation.

Additional shapes: unused/partial/nested Exception definitions, exact versus
base coverage, alias, inheritdoc, unresolved/non-Exception/non-type crefs,
duplicate tags, empty tags and absent documentation.

## Mode controls (items 10–12)

Direct, ProjectTransitive and SolutionTransitive selection/routing/execution are
unchanged. Existing B5–B15 tests run unmodified, including the real B15
cross-project DAGs. Every positive B22 input also captures actual native Direct,
Project, Declared and Solution Findings/graph states; genuine Historical Project
canonical evidence remains unchanged. Eight full Current JSON baselines cover
all four modes with both existing control variants and are byte-identical to
the fresh unmodified-parent run.

## Admission, attacks and failure boundaries (items 13–14)

No raw DTO, clone or fabricated `DeclaredPolicyFacts` is a productive admission
argument. Only an original CWT-owned successful B21 execution receipt with the
correct explicit execution purpose can pass `TryAdmitFindingEvidence`, which
precedes the Main guard and rendering calls. The new purpose is an additional
operation boundary, not replacement authority for B21.

Seventeen new cases reject missing document/type/declaration/ownership/Direct/
effective-result data, conflicting flow, invalid context, default/wrong mode,
missing/malformed/consumed attestation, request/response/binding/target/source
substitution before guard/policy. Seven unsupported-purpose/scope cases reject
before dispatch. The real renamed B4 pipe peer copies the exact genuine
productive response yet fails physical-image association before guard. A
legacy B21 receipt and a correct receipt with the wrong/default/unknown purpose
are also rejected.

All three original B20 coherent manipulations and all B21 replay/substitution/
physical-worker tests remain unmodified. A selected Summary Worker error returns
failure and **no partial Direct Findings**, no optimistic empty success and no
Direct/Project/Solution/Current fallback.

## Binary and isolation results (items 15–16)

- Three Main DLL/PDB pairs changed: shared Main rendering and productive
  orchestration, receipt snapshots and bounded payload validation.
- Three Historical Analyzer DLL/PDB pairs remain byte-identical. All 121 shared
  analyzer sources and the one Historical semantic host are unchanged.
- Two Worker DLL/PDB pairs changed: explicit execution-purpose plumbing only;
  document/declaration emitters and B21 client attestation code are unchanged.
- Main test assembly is rebuilt; no new build project or twelfth WAE step.
- Only clean archive/export binaries were built. Original protected Core
  physical files remain unchanged. Each owned child is checked stopped; Main
  never loads Historical Analyzer/Worker/Roslyn assemblies. Final live-process
  scan is included in the verifier. No submitted input code is executed.

## Validation (items 17–20)

Final dual boundary passed 578/578, without skips, and all eleven WAE builds
passed with zero warnings/errors. Separate B22 is 46/46; B21 and all prior
packages ran unchanged. Final productive/focused/broad/full counts are
681/788/3089/3180, all passed, all zero skips. Full passed on its first actual
execution; the earlier aggregate attempt stopped at focused before broad/full.

Proof entry points:

```powershell
./Evaluation/P5O2B22Proof/Run-Regressions.ps1 -ValidationRoot <clean-export-tool-root>
./Evaluation/P5O2B22Proof/Write-Verify-ProductionFindings.ps1 -ValidationRoot <clean-export-tool-root>
```

Verifier checks actual TRX counts/freshness/skips, all build logs, source and
binary fingerprints, full positive/negative evidence, original attack evidence,
eight baselines, exact built overlay, unrelated source/test/Core/root-ignore/
index/stash preservation, strict UTF8 without BOM/CRLF, JSON, PowerShell syntax,
owned C# whitespace, architecture/source boundaries, Git case collisions and
owned live children. This is real Windows execution and static Git case audit,
not a claim that a Linux runner was executed.

Self Analysis **16** and Canonical Finding Diff **0/0/0** are inherited from B21,
**not newly executed**. The eight Current-baseline comparison is fresh and a
separate result.

### Attempts retained

The first new-test compile failed with four CS1061 errors (`Id` versus actual
`ID`). Original complete tool output is retained in the conversation; an
unchanged diagnostic reproduction is retained locally. No claim is made that
the first invocation itself had a local log. The corrected build passed.

First B22 run: **17/42 passed, 25 failed, zero skips**. Fifteen two-stage cases
used reference inequality on canonical callable objects; corrected to value
`Equals`. Ten other failures were the new exporter's attempted native Roslyn/
Encoding serialization, replaced with neutral snapshots (including rejected
scope snapshots). Full first log/TRX are retained. Second complete 42-case run
passed 42/42. The subsequent expanded gate passed 577/577; that intermediate
audit/log/TRX and second-run evidence are retained separately. Final expanded
46-case tests and 578-case gate include physical-worker admission and stronger
full document snapshots. No exclusions, automatic retry, old-test edits or
opportunistic flake fix were used. `artifacts/p5o2b22/attempts.json` records the
attempt sequence honestly.

The first aggregate run additionally exposed **12 failed / 776 passed of 788
focused tests**, zero skips. Unchanged RuntimeAwait tests reflect the private
DOC631/DOC632 helper signatures; the refactor had removed those entry names.
They are restored as thin native adapters to the **same shared** documented-flow
implementation, not another policy. All 24 unchanged RuntimeAwait capability
tests then passed in a targeted diagnostic run. The entire first aggregate
evidence/log/TRX set and prior dual audit are retained under
`artifacts/b22-6a60/before-native-seam-fix*`; the final dual and complete aggregate
runs are repeated after this source correction. This is a refactoring
compatibility defect, not a new semantic/contract/trust boundary or a known-flake
repair. Broad/full runs had not started when the first aggregate script stopped.
Review also retains the old lazy evaluation of missing-Finding member anchors;
the new permanent B22 control exercises an incomplete member at EOF with no
Finding, so extraction cannot eagerly resolve an otherwise unused anchor.
The first verifier draft's exact-chain check failed PowerShell 5 syntax parsing
before execution (continuation operator on the next line). The draft is
retained and the corrected script parses cleanly; no product/test workaround.

## Remaining scope and repository state (items 21–23)

Only the bounded v1 complete/proven, single-project, original root-method scope
is activated at the new typed Finding entry. Existing exact reference/source/
parse/signing/selector/budget restrictions from B11–B14 and B17/B19 remain.
Cross-project Declared, arbitrary non-root signatures, new contract versions,
signed reference targets, generic/out-of-world document types, incomplete
evidence, broader solution/source acquisition, external XML contracts, general
Historical reporting/CLI adoption, additional Roslyn versions, plugins and
Dapper redesign are not closed. The legacy evidence-only mode rejection stays
explicit; no globally renamed mode or fallback is introduced.

Starting and final HEAD are audited independently. Starting HEAD is
`6a603804108eee6a06803915332815b4df41e036`; no external movement has been
observed. No commit or push. Root `.gitignore` SHA256 remains
`7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5`.
All 23 protected physical Core files, six stashes and the index are checked
against the initial snapshot. Final `git status --short` is included in the
machine-readable audit and the final census below. `OPEN-PIPELINE-BOUNDARIES.md`
updates only this bounded guard/Finding closure and retains broader open scope.

## Final validation census and Git status

| Fresh run | Passed / total | Failed / skipped |
| --- | ---: | ---: |
| b22 | 46 / 46 | 0 / 0 |
| b21 | 26 / 26 | 0 / 0 |
| b20 | 10 / 10 | 0 / 0 |
| b19 | 39 / 39 | 0 / 0 |
| b18 | 7 / 7 | 0 / 0 |
| b17 | 25 / 25 | 0 / 0 |
| b16 | 18 / 18 | 0 / 0 |
| b15 | 28 / 28 | 0 / 0 |
| b14 | 31 / 31 | 0 / 0 |
| b13 | 34 / 34 | 0 / 0 |
| b12 | 29 / 29 | 0 / 0 |
| b11 | 16 / 16 | 0 / 0 |
| b10 | 34 / 34 | 0 / 0 |
| b5-b9 | 156 / 156 | 0 / 0 |
| productive | 681 / 681 | 0 / 0 |
| focused | 788 / 788 | 0 / 0 |
| broad | 3089 / 3089 | 0 / 0 |
| full | 3180 / 3180 | 0 / 0 |
| final boundary | 578 / 578 | 0 / 0 |
| unmodified-parent boundary | 532 / 532 | 0 / 0 |

Eleven final WAE build steps: **0 warnings / 0 errors each**. Format, strict
encoding/JSON/PowerShell parsing, architecture, isolation, source/binary/
Current-baseline/protection checks are enforced by the final machine-readable
Evidence Verifier, which passed on its first executed run (exit 0). Its draft
parser failure described above was caught before execution. See `P5O2B22-historical-declared-production-finding-integration-audit.json`
for its result and exact underlying artifacts/fingerprints.

Final status (root ignore is pre-existing protected WIP; all other paths are B22):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs
 M src/XMLDocNormalizer/Execution/Analysis/DeclaredExceptionFindingPolicy.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerScopeProjection.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
?? Evaluation/P5O2B22-historical-declared-production-finding-integration-audit.json
?? Evaluation/P5O2B22-historical-declared-production-finding-integration.md
?? Evaluation/P5O2B22Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs
?? src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs
?? src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.Findings.cs
```

No external HEAD movement, no commit, no push, no staging or stash operation.
