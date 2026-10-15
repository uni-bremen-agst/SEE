# P5O2B24 — bounded Direct / ProjectTransitive Finding contracts

## Preimplementation audit

Starting HEAD: `4b6890b9b4977024cae5288efe84babd736a48a6` (B23).
Only pre-existing change: root `.gitignore` (protected). Initial protection snapshot:
`artifacts/p5o2b24/protected-before.json`: 23 physical Core files, 6 stashes,
index and root-ignore SHA256. No original-tree build is permitted to churn Core outputs.
No network, commit, push, index or stash mutation is part of this work.

This matrix was written **before production changes**. Source owners inspected:
`XmlDocExceptionSemanticDetector.cs`, `.Findings.cs`, `.ExternalFindings.cs`,
`HistoricalWorkerPayloadProjection`, `HistoricalWorkerScopeProjection`,
`HistoricalAnalysis`, `HistoricalDeclaredPolicyFacts`,
`HistoricalDeclaredDocumentationFacts`, documentation projection, response validation
and the B19/B21/B22/B23 tests.

| Information | Direct Current | Direct Historical before B24 | Project Current | Project Historical before B24 | Required action |
| --- | --- | --- | --- | --- | --- |
| Exception Type Identity | native exact symbol equality; display separately | canonical flow type + exact reference world | same, including transitive types | B12/B14 canonical equivalent | validate bound type/display facts, not string identity |
| Target / Root Identity | actual documented source declaration | selector + provenance, not a Finding receipt | same declaration | B14 source-owned root | original PE-root + source-syntax receipt, actual worker root equality |
| Method Ownership | native declared symbol | worker requires unique static nongeneric parameterless body in root tree | same | same B14 restriction | retain bounded profile; no arbitrary signature/multi-root |
| Source Ownership | original tree and reporting path | canonical paths in submitted sources | complete same-project semantic environment | B14 complete ordered P5J set | preserve original ownership receipt and exact paths |
| Flow Source Kind | local collector Proven/external-document facts | complete Proven canonical flows only | Proven plus possible external-document evidence | complete Proven canonical flows only | keep complete/proven admission, do not pretend general uncertainty/XML support |
| Direct Evidence | `AnalyzeDirectlyThrownExceptions`; used directly as flow | actual Direct worker collector exists | independent local collector produces DOC610 | B17 independently produces Direct, but tied to Declared inventory | reuse collector, no replacement of Current; neutral independent Direct facts |
| Summary Evidence | none | none needed | productive same-project summary session; DOC611 excludes all Direct types | B14 flow/graph parity exists | explicit Project summary execution, independently bind both results |
| Documentation Binding | actual `GetSymbolInfo(cref)`, exact type, inheritance | absent in ordinary Direct response | same | B19 supplies facts only with Declared opt-in | reuse XML/binding helper and B21, not Declared relevance/inventory |
| Provenance | existing compiler-local semantic environment | B7 selection and B12/B14 reference/source hashes | same | same | no force-Historical or fallback; request/response receipt |
| Uncertainty | Direct policy still DOC630 for absent documented type | incomplete response rejected | DOC631 rather than DOC632; missing proven types still reported | worker rejects incomplete/truncated analysis | explicit bounded complete/proven subset; negative controls, no widening |
| Finding Context | original member/tag anchors, snippet, source context | no public Finding adapter | same | B19 context facts + B22 shared renderer exist | immutable original context, shared Main policy |
| Analysis Mode | Direct: DOC610/630 (+660/670) | flow mode Direct exists, Finding admission rejects | Project: DOC610/611/631/632 (+660/670) | flow Project exists, Finding admission rejects | explicit independent mode labels, not Declared aliases |
| Graph / Summary relevance | no summary session | not needed | summary required for documented root, irrespective of local exception declarations | B14 graph parity; B22 guard is Declared-specific | no local-definition guard or inventory for either new mode |

Current first skips undocumented members, builds tag facts, computes independent Direct,
then chooses Direct or the shared productive summary session. Both modes enter the existing
generic Main `AddExceptionFindings` policy. Exact exception equality is used (no base-type
coverage). Inheritdoc suppresses missing tags only. Invalid/nonexception crefs remain
DOC660/DOC670. Member and tag anchors supply the normal Finding context/target/identity;
the exception display becomes the flow target. Project DOC610 remains unrestricted and
DOC611 excludes independently Direct types; **neither new mode filters to local definitions**.
There is no need to transport the B17 complete exception-declaration inventory.

Before B24 the missing contract is not a new detector policy: it is an independently
mode-bound, execution-attested root/document/Direct receipt (plus Summary for Project).
B19's existing XML helper is reusable; B17's eligibility and inventory are not.
B21's exact completed physical-worker exchange must cover every new fact, not just DTO
self-consistency. B22/B23 Declared admission must retain its existing wire defaults and guard.

Implementation and validation results follow below; this preimplementation section is retained.

## Implemented contracts and trust

`WorkerFindingRequest` adds one independent opt-in to protocol 2, with the explicit
Direct or Project mode, an execution purpose and the existing local-document syntax
shape. Its profile is `single-project-root-findings-v1`. The historical names of the
reused XML DTOs / mode-label / purpose enums do **not** activate B17 eligibility:
`DeclaredPolicy` and `DeclaredExecution` must be absent. Direct and Project labels
are distinct; no mode substitution occurs. Old optional-null serialization and
Declared defaults stay unchanged.

The Worker uses the **unchanged** compiler-local Direct collector and productive
summary engine. It independently emits Direct plus the selected result and the
existing actual Historical XML bindings/root/type displays and hashes. No declaration
inventory, guard decision, eligibility, coverage or Finding is computed by the Worker.
The same XML emitter is source-linked against each compiler, not loaded across versions.

Main retains the original validated P5J syntax member and PE-bound root/reference
world. It checks exact method/syntax/path/type/display/classification/result bindings,
complete tags and ordered displays. Every flow type must resolve to an actual exception
in that original world. The new immutable original-call receipt is minted only after
B21 checks the exact sent request, original payload instance, completed response,
diagnostics, exit/PID and the physical executed Worker image. A caller-authored DTO,
clone, changed binding, consumed receipt or replay process cannot become authority.
This remains B21 local trusted-runtime attestation, **not remote cryptographic attestation**.

Direct enters Main's shared Finding policy after its original Direct/document receipt
and never requests Summary. Project first admits Direct/documentation, skips an
undocumented root, otherwise always requests separately attested Summary. Independent
Direct, root, member/tag context and complete source ownership must agree across both
executions. Summary failure yields no partial Direct Findings. No local-definition guard
or local-type filtering is applied. A fail-fast invariant ensures the common policy
never asks Declared relevance for either new mode.

The existing B22 renderer is only factored into a common evidence-to-policy adapter;
native `AddExceptionFindings`, the Current collector/session and Declared eligibility
are unchanged. Full serialized Finding-set equality covers every ordinary Finding field:
rule, path, tag, location, formatted message, snippet and context including target. There
is no separate Finding identity object; equality of all identity/context inputs also
covers the normal canonical identity. Flows additionally compare canonical exact types,
evidence/source kinds, paths, uncertainty and relevant graph snapshots, not merely counts.

## Bounded CLI adoption and compatibility

Use the existing command and job, adding exactly one optional field:

```json
"analysisMode": "direct"
```

or `project-transitive`. Missing/null mode retains B23 Declared; the explicit
`project-transitive-declared-exceptions` spelling does too. Unknown spellings and
Solution are rejected. There is no alias, automatic discovery, force-Historical,
network acquisition or Current fallback. Selection remains exclusively original
validated compiler provenance through B7/B6/B8/B5/B4.

Compatibility decision: B23 has permanent unchanged real-CLI tests requiring that the
legacy `--exception-analysis-mode direct|project-transitive|solution-transitive` override
be rejected for its old jobs. Accordingly the new opt-in is **job.analysisMode**, not a
relaxed legacy switch. The existing Declared CLI assertion is retained, but an explicit
assertion conflicting with the job is rejected, never silently aliased/ignored. No new
CLI input form, reporter, counting path or engine-selection option is introduced.

The shared ToolRunner branch accumulates once and reports each Finding once via the
unchanged ordinary RunResult/JSON path. B23's unchanged Console/SARIF controls cover the
same engine-neutral adoption; no mode-specific reporter behavior was added. Four fresh
GUID-directory CLI tests cover Direct/Project, each with one Finding and with none;
four additional real CLI negatives cover explicit mode conflicts and unsupported modes.
No pre-existing test-artifact directory or output report is required.

## Scope and B25

Only existing unsigned debug/AnyCPU/nullable-enabled exact-reference/regular-parse,
complete-source single-project v1 inputs and one source-owned static parameterless
nongeneric ordinary method with body are admitted. Flow and documented named types
must be nongeneric and resolvable in the original PE/reference world. Complete/proven,
untruncated results only: this does not claim the full Current uncertainty domain.
The Direct/GC.Collect witness succeeds without Summary, while Project Summary fails
closed without partial output. No general external XML, arbitrary compiler version,
signed target, Dapper, broad source loading, arbitrary root or multi-root adoption.

SolutionTransitive final Findings / cross-project Finding ownership remain **B25**.
Existing B15 flow/graph parity alone is not sufficient and no Solution mode is enabled.

## Validation chronology (retained attempts)

All builds/tests run in `artifacts/b24-4b68/Tools/XMLDocNormalizer`, exported from the
audited HEAD with owned-file overlays. The original protected Core outputs are not built.
Only local caches are used: Current has a validation-only local-source NuGet config;
Historical remote feeds are explicitly disabled in its validation-only config and an
explicitly mapped local B23 package cache is added. Production feed/version files are
unchanged; there is no audit/error suppression or network authorization/use.

1. PowerShell Copy-Item encountered long-path failures while copying the isolated package
   cache. A bounded non-deleting `robocopy /E /R:0 /W:0` completed that local copy.
2. Initial WAE attempt stopped on four CS8602 diagnoses in the newly factored binding
   helper. Explicit null preconditions fixed these; diagnostics were not disabled.
   Retained: `artifacts/p5o2b24/attempt-first-build`.
3. Next dual gate built cleanly but boundary was 575/578. One failure exposed the initial
   validation-only feed-value replacement (the test requires the original recorded feed);
   configuration now retains recorded feed values/mappings, explicitly disables remote
   sources and adds the local cache. Two unchanged old tests failed at Stopped(pid):
   B14 Direct/two sources and B13 C#12/no defines, after their semantic assertions.
   No old test/client fix. PID reuse is plausible, **not established**.
   Retained: `artifacts/p5o2b24/attempt-second-gate`.
4. First new contract run: 27/29. Both failures were the newly added physical-peer
   assertion: the old replay test-process recognized only DeclaredPolicy, so it did not
   replay the genuine new response. Its test-only request recognition was extended
   additively; the production trust boundary was not weakened. TRX retained.
5. Second new non-CLI contract run: **34/34**, no skips, including 27 full repeated
   differentials, both uncertainty/Direct isolation controls, per-mode malformed/replay
   controls and actual wrong-physical-worker/failure controls. This proof preceded CLI
   adoption.
6. First new real-CLI run after adoption: **8/8**, no skips. No reporter changes.
7. Next full dual attempt: all eleven WAE builds again 0 warnings / 0 errors; old
   boundary **574/578**, no skips. All four failures are unchanged Stopped(pid)
   assertions: B18 SameProjectChain, B21 method binding, B15 three-project/framework
   exception, B22 chain. No semantic/assertion/client change or excluded test.
   Retained: `artifacts/p5o2b24/attempt-third-gate`. The feed-shape test passed.
8. An intermediate format verification caught the newly added LF-only example JSON;
   owned-file UTF8/noBOM/CRLF normalization corrected it before final verification.
9. Initial individual B24/B23/B22/B21–B10/B5–B9 groups all passed unchanged, as did
   productive **759/759** and focused **866/866**, all with no skips. Broad was
   **3164/3167**: old B14 PID lookup raised AccessDenied, old B22 base failed its
   Stopped assertion, and the new B24 combined case failed the copied PID assertion.
   Full was **3257/3258**, with only that new B24 helper failing. All functional
   comparisons precede that helper. Initial TRX/logs are preserved in
   `artifacts/p5o2b24/attempt-initial-regression`.
10. Corrected **only the newly written B24 helper**: successful executions must have the
    original private B21 completed-exchange receipt, not an unrelated later PID lookup.
    The new permanent test also verifies the existing owned-process exit/pipe-EOF-before-
    receipt sequencing in the Client. No production/client lifetime or old test changes.
    The Project response negatives were additionally strengthened to use genuine
    SummaryEvidence purpose, including missing/contradictory Summary. These are test-only
    supplements. Initial compiled source/image fingerprints are retained in
    `artifacts/p5o2b24/validation-inputs.json`; production/fixture bytes must remain identical.
11. A non-mutating one-line PowerShell parser-check command initially used an unsupported
    foreach-to-pipeline form. The corrected command parsed all Proof scripts with zero
    parser errors. No file or test was changed by that setup failure.

Final complete validation results and protection/status audit are recorded below and
in the machine-readable audit produced by `P5O2B24Proof/Write-Verify-Findings.ps1`.

12. Final full dual attempt: all eleven WAE builds again clean; existing boundary
    **576/578**, zero skips. The unchanged B22 `mixed` and `base` cases fail only
    in `Stopped` at line 397, after their semantic comparisons. The full script
    exits 1 before its B23/B24 follow-on steps. Complete logs/TRX are retained in
    `artifacts/p5o2b24/attempt-fourth-gate` and the export's dual-build directory.
13. Explicit manual repeats of already executed B24/broad/full groups, with the
    corrected new helper and strengthened Summary-purpose negatives: **42/42**,
    **3166/3167**, **3258/3258**, all zero skips. Broad's only failure is unchanged
    B18 `IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings("cover-exact")`,
    `Stopped` line 207. No old test/production fix and no test exclusion.
14. The exact unreached permanent B23/B24 test commands were explicitly invoked
    separately: **36/36** and **42/42**, zero skips. They are independently real
    executions, not a resumed successful full dual-build command. The pending
    false dual audit is not rewritten as a successful gate.
15. At final evidence collection, new Proof-script line continuations failed
    Windows PowerShell 5 parsing; moving boolean operators to the preceding line
    corrected the new script only. A literal wildcard `rg` invocation was also
    rejected by Windows; this read-only setup failure changed nothing.
    The verifier then caught two unsuitable comparisons to the prior export:
    B23's validation-only NuGet configuration differs from production, and its
    source manifest has CRLF whereas the untouched original manifest has LF.
    Production feeds/manifest are now checked against committed HEAD; manifest
    content additionally matches B23 after newline normalization. All 121 actual
    analyzer source fingerprints still require exact byte equality. No production
    file, old test, warning, failure flag or analysis assertion was changed.
16. The optional final local process-inventory query initially received WMI
    AccessDenied in the sandbox. The authorized read-only repeat completed:
    no dotnet/testhost process command line matching this B24 export remained
    (2026-10-15, checked before 18:27:49 UTC). No process was terminated and no
    network was accessed. This is an observational cleanup check, not a repair
    or proof of the root cause of the old PID-test failures.

## Final validation census and honest readiness status

| Fresh executed group | Passed / total | Failed / skipped |
| --- | ---: | ---: |
| B24, latest complete mode/CLI contract tests | 42 / 42 | 0 / 0 |
| Unchanged B23 | 36 / 36 | 0 / 0 |
| Unchanged B22 | 46 / 46 | 0 / 0 |
| Unchanged B21 | 26 / 26 | 0 / 0 |
| Unchanged B20 | 10 / 10 | 0 / 0 |
| Unchanged B19 | 39 / 39 | 0 / 0 |
| Unchanged B18 | 7 / 7 | 0 / 0 |
| Unchanged B17 | 25 / 25 | 0 / 0 |
| Unchanged B16 | 18 / 18 | 0 / 0 |
| Unchanged B15 | 28 / 28 | 0 / 0 |
| Unchanged B14 | 31 / 31 | 0 / 0 |
| Unchanged B13 | 34 / 34 | 0 / 0 |
| Unchanged B12 | 29 / 29 | 0 / 0 |
| Unchanged B11 | 16 / 16 | 0 / 0 |
| Unchanged B10 | 34 / 34 | 0 / 0 |
| Unchanged B5-B9 | 156 / 156 | 0 / 0 |
| Productive | 759 / 759 | 0 / 0 |
| Focused | 866 / 866 | 0 / 0 |
| Broad, latest explicit repeat | 3166 / 3167 | 1 / 0 |
| Full configured suite, latest explicit repeat | 3258 / 3258 | 0 / 0 |
| Existing permanent boundary, last full dual invocation | 576 / 578 | 2 / 0 |
| Permanent B23 follow-on, separately invoked | 36 / 36 | 0 / 0 |
| Permanent B24 follow-on, separately invoked | 42 / 42 | 0 / 0 |

**The full Dual-Version-Build and overall Evidence Gate are NOT PASS.** All
eleven actual WAE build steps have zero warnings/errors, and actual Historical
runtime identity, repeated canonical smoke, protocol rejection and isolation
checks pass. Nevertheless the complete script fails at the existing boundary;
its follow-on steps are reported separately, not spliced into a synthetic PASS.
The broad group also remains red. The verifier writes
`ArtifactConsistencyPassed=true`, `AllRequiredValidationPassed=false`,
`EvidenceVerifierPassed=false`, and exits nonzero for
`dual-version-build-script`, `broad`, `boundary`. Exact failed test names,
messages/stacks, TRX hashes and retained attempts are in the JSON audit.

These failures occur in unchanged later PID lookups, not the Finding assertions.
The production client still awaits its owned process and both pipe EOFs before
minting the private B21 completed-exchange receipt. PID reuse is a possible
explanation, not a demonstrated root cause of every old failure. This work does
not repair those old helpers or claim the remaining overall readiness condition
is satisfied. A separate approved test-lifecycle investigation is needed before
an all-gates-green release/readiness claim.

The final test-only supplements were rebuilt before the latest B24/full runs.
All production/fixture source and actual Main/Worker image hashes match the
initial complete-regression inputs, checked by the verifier. Earlier unchanged
cohort results therefore use identical production inputs; their timestamps and
hashes are retained, not represented as newly rerun after documentation edits.

Eight fresh complete Current baseline JSON files are byte-equal to B23: all
four native modes, both existing control variants. The ordinary Current detector,
collector, summary session, comparison/reporting logic, router/selection and
compiler versions are unchanged. Existing Current/comparison/reporter tests are
part of the executed configured suites; external comparison remains unsupported.

Format, Git whitespace, strict UTF8/no-BOM/CRLF for owned files, JSON parsing,
PowerShell parsing, exact built-source overlay, archive without bin/obj/artifacts,
shared-source architecture, runtime isolation, protected state and Git path case
collision checks pass during artifact verification. This is Windows execution
plus a static Linux-relevant Git path audit, **not a Linux runner execution**.
No network was used; only explicit local cache sources were enabled in the export.

Self Analysis **16** and Canonical Finding Diff **0/0/0** are **inherited B22/B21
evidence, not freshly executed** and not analysis of the newly added B24 code.
The eight fresh Current baseline comparisons are a distinct, actually run check.

## Per-mode outcome and proof inventory

Direct is functionally implemented and proved in its admitted complete/proven
scope: **13** differentials compare full Current/Historical Finding sets and
evidence twice, including actual Direct throw, empty/no Finding, framework type,
exact/base/alias/invalid/inheritdoc/no-doc shapes. It never creates Summary merely
for symmetry. Its genuine `GC.Collect` control proves that Direct remains usable
when an unrequested transitive Summary would fail.

ProjectTransitive is independently functionally implemented and proved in the
same bounded scope: **14** full repeated differentials include Target -> Helper
-> throw, Target -> A -> B -> throw, external framework and local exceptions,
Direct+Summary mixtures and a shared type on both paths without double counting.
Actual graph/summary states, exact types, paths, source/provenance/ownership,
documentation, uncertainty and full Finding context/target agree. It always
requests its genuinely separate Summary for a documented root; the incomplete
Summary control fails closed without partial Direct output. Neither mode asks
for a Declared inventory, declaration relevance or local-type filter.

Both use the same Main Finding policy and same B23 CLI-to-RunResult adoption,
with explicit job modes. Their **26** persisted response-negative proofs (13
per mode, Project using actual SummaryEvidence) cover missing/contradictory root,
ownership/type/Direct/effective result/document/binding/mode data, missing actual
authority, consumed receipt and cloned original input. Additional unsupported
input controls cover profile/root/source/source-set completeness. Coherent JSON
replay by an actually different physical process fails B21 even with the correct
copied payload. Genuine worker errors cannot create Findings or fallback.

The **8** real CLI proofs use fresh directories: one Finding and zero Findings
for each mode, two explicit legacy-flag/job conflicts and two unsupported job
modes. Normal JSON/RunResult counts equal the Finding collection exactly once;
no Historical output schema or duplicate counting exists. Existing B23 Console,
JSON, SARIF, empty, Declared and negative controls remain unchanged and pass.

No new independent trust/documentation/ownership/summary contract gap was found
inside the admitted complete/proven profile, so the per-mode Stop rule was not
triggered. This is not general Current-domain equivalence. SolutionTransitive
final Findings and cross-project Finding ownership/reporting target remain
**B25**, explicitly rejected in the new external job. No partial Solution or
cross-project Declared activation, signing, Dapper/release-debug-plus progress,
new compiler/reference/XML forms, multi-root or automatic loading is claimed.
Functional bounded adoption is proved for each mode; overall gate readiness
remains open as stated above.

## Changed inputs and binary impact

Main changes are confined to independent Finding receipt/projection/admission,
the shared existing policy adapter, the existing physical-worker opt-in trigger,
and existing external-job/CLI orchestration. Historical Worker changes are only
`HistoricalAnalysis.cs` and `WorkerProtocol.cs`: independent neutral facts and
explicit purpose plumbing into the unchanged analyzers. The original binding
emitter, one Historical semantic host and **121 shared Analyzer sources** are
unchanged, as are project/compiler-version declarations and production feeds.
Main and Worker DLL/PDB outputs and the test assembly/test-only replay fixture
are rebuilt for these changes. No cross-export raw DLL/PDB equality is claimed
across different absolute build roots; exact executed image and same-export
production-image hashes are verified.

Old B5-B23 test files are untouched. The new 42-case test file and additive
test-peer request recognition supply the new proof. The permanent build script
adds only a separate B24 test step; the old 578-test filter and eleven WAE build
steps are unchanged. Documentation, JSON audit and B24 Proof scripts are the
remaining owned changes. The root `.gitignore` is pre-existing protected WIP,
not part of B24; no generated bin/obj/artifacts files are included in the diff.

## Reproduction and final repository audit

All proof paths are workspace-local. Retained B23 comparison data and local
package caches are prerequisites, not downloaded by these scripts. For this
run the export tool root is:
`D:/Repository/SEE/Tools/XMLDocNormalizer/artifacts/b24-4b68/Tools/XMLDocNormalizer`.

```powershell
# For a new run only: use a fresh artifacts destination, never overwrite this evidence.
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Export-Validation.ps1 -Destination artifacts/<fresh-export>
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1 -ValidationRoot <export-tool-root>
# In the export tool root (actual last full invocation exited 1):
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
# From the original tool root:
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Capture-ValidationInputs.ps1 -ValidationRoot <export-tool-root>
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Run-Regressions.ps1 -ValidationRoot <export-tool-root>
# Preserve complete prior TRX/logs before any explicit repeat; do not conceal failures.
# This run additionally repeated all B24/broad/full tests with -RepeatGroups b24,broad,full.
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Run-FollowOnBoundary.ps1 -ValidationRoot <export-tool-root>
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Format-Owned.ps1 -Verify
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Write-Verify-Findings.ps1 -ValidationRoot <export-tool-root>
```

The before snapshot is the original `artifacts/p5o2b24/protected-before.json`;
it was not replaced after changes. After the traffic-reset continuation, actual
HEAD/status/index/protected files/stashes and existing B23/B22/B21 reports were
re-audited; the existing B24 WIP was continued, not restarted. Only outstanding
follow-on collection, final verifier corrections and reporting remained.

Starting and final HEAD are `4b6890b9b4977024cae5288efe84babd736a48a6`, branch
`fix/documentation`; no external HEAD movement. All **23** actual physical Core
files retain their original SHA256, all **six** stash identities/reflog entries
are unchanged, root `.gitignore` and Git index retain their original SHA256.
No protected original Core build was run. No staging, commit, push, stash mutation
or WIP discard. The machine audit checks these facts again on each invocation.

Final `git status --short` (first entry is protected pre-existing WIP):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Evaluation/P5O2B4Proof/TestProcess/Program.cs
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs
 M src/XMLDocNormalizer/Cli/ArgsParsing.cs
 M src/XMLDocNormalizer/Cli/ToolOptions.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerDeclaredDocumentationProjection.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
 M src/XMLDocNormalizer/Execution/ExternalTargetExecution.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
 M src/XMLDocNormalizer/Execution/ToolRunner.cs
?? Evaluation/P5O2B24-bounded-finding-contracts-audit.json
?? Evaluation/P5O2B24-bounded-finding-contracts.md
?? Evaluation/P5O2B24Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/HistoricalFindingFacts.cs
```

## Validation Closure / PID Gate Audit — 2026-10-15 addendum

**P5O2B24: functional scope complete; validation closure blocked by a
reproducible existing PID-gate defect.** No B24 functionality was developed in
this step. No production, test, build, routing, protocol, compiler-version or
assertion file changed. The three old failing cases passed in isolation, but
the one permitted unchanged Broad repeat failed in another old PID helper.
The Stop rule was applied: no Boundary, Dual-Version-Build or Evidence Gate was
then rerun. No B25 work, retries-until-green, skips, sleeps, assertion weakening,
test-order controls or cleanup/lifetime modifications were introduced.

### Historical errors and limits of the original observation

The original failed audit/report/TRX/logs and case evidence were copied before
any execution to `artifacts/p5o2b24/pid-gate-audit-20261015/before`; a SHA256
manifest is retained in `preserved-artifacts.json`. Nothing in that failed
snapshot was replaced with a successful result.

| Original group | Exact case | Failing helper | Recorded TRX timestamp (+02:00) |
| --- | --- | --- | --- |
| Broad 3166/3167 | B18 `IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings("cover-exact")` | `Stopped`, line 207 | 2026-10-15 17:57:49.5890394 |
| Boundary 576/578 | B22 `ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat("base")` | `Stopped`, line 397 | 2026-10-15 17:51:52.3618128 |
| Boundary 576/578 | Same B22 method, `"mixed"` | `Stopped`, line 397 | 2026-10-15 17:51:33.8391993 |

All three are `Assert.True(process.HasExited)` with expected true / actual
false, not flow/Finding mismatches. Expected/observed PID numbers and the looked-up
process creation identity were **not logged** by those failing assertions. Their
evidence write occurs after `Stopped`, so successful JSON from subsequent runs
cannot supply the missing historical PID. No historical orphan/cleanup state is
retroactively claimed. The adapter's reported start/end attributes are nearly
identical despite nonzero duration; they are retained as recorded, not treated
as precise OS start/exit events.

B23 already documents the same pattern in unchanged B16
`mixed-direct-transitive`: boundary 577/578, then isolated B16 18/18 and later
green gates, without client/helper changes. B24's earlier varying B13/B14/B15/
B18/B21/B22 PID failures remain in the preceding chronology. The three initially
selected cases are therefore not continuously failing functional cases.

### One isolated repeat, then one Broad repeat

No test assembly was rebuilt. A `--list-tests` check confirmed that the exact
FullyQualifiedName/DisplayName filter selected only the requested three cases;
the actual TRX independently confirms three executed, zero skipped cases.
Framework execution order was not configured or altered.

| Invocation | UTC start → finish | Result | Exit |
| --- | --- | --- | ---: |
| Exact isolated three cases | 20:17:47.1566857 → 20:17:59.3953967 | 3/3, zero skips | 0 |
| Unchanged Broad | 20:18:10.9375175 → 20:21:20.7351710 | 3166/3167, one old PID assertion, zero skips | 1 |

Fresh isolated evidence was copied before Broad could overwrite it. Observed
completed-call PID lists, in each JSON's original call order:

- B18 cover-exact: 36328, 15620, 23988 (first/changed/repeated).
- B22 base: 26296, 6264, 14752, 33308, 10704
  (first Direct/Summary, repeat Direct/Summary, Project control).
- B22 mixed: 9576, 26448, 17624, 24448, 16296 (same order).

All fresh isolated evidence has `ProcessesStopped=true`; all corresponding
calls have no failure. The tests do not serialize a separate raw process exit
code: successful client validation follows the actual owned exit/EOF path.
The VSTest invocation exit code is independently recorded as 0. Artifact hashes,
command arguments, group timestamps and each reported test duration are retained
under `isolated.json`, `isolated.trx` and `isolated-evidence/`.

The original three cases also **all passed in Broad**. Broad instead failed:
`ExceptionFlowHistoricalReferenceIdentityTests.RealExternalMetadata_FullCanonicalIdentitySourcesUncertaintyGraphRepeatAndIsolation(version: 1)`.
Its recorded timestamp is 22:18:34.6612575+02:00, duration 7.4052210 s. The stack
is `Stopped(int pid)` line 403, called at line 60, after canonical/identity/graph
assertions. Specifically line 60 checks the **first** successfully returned
Historical call, after the second call and auxiliary graph process. This helper
is already committed before B24 (`35d69be2a4`, reference-identity work); B18 and
B22 likewise predate B24. Its actual failing PID was not logged and its JSON
write was not reached. Older `external-v1.json` has an earlier timestamp and is
not attributed to this failure. The new Broad TRX/log/result is preserved,
including the exact failure, without another Broad attempt.

### Root cause: PID value is not retained process identity

Both helper forms discard the completed call's owned process identity and later
perform:

```csharp
using var process = Process.GetProcessById(savedPid);
Assert.True(process.HasExited);
```

They tolerate only the no-such-PID `ArgumentException`. If Windows has reused
that integer, lookup succeeds for a **different current process** and the
assertion legitimately sees false. It does not test whether the original Worker
exited. Extra worker/graph executions and parallel test classes widen the gap
between releasing the old handle and the later global lookup. No parallelism or
test order was changed to obtain a pass.

The unchanged client owns a `Process`, captures its ID, awaits the stdin exchange,
actual process exit and both pipe EOFs; its `finally` additionally awaits the
owned `WaitForExitAsync` before disposing the process and returning. B21's
private receipt is only minted after completion. No detached spawn or alternate
successful early-return path was found. The defect is the later test's loss of
identity, not evidence of a real Worker still running. Later PID lookups can
also explain why unrelated-process access restrictions are possible; the earlier
AccessDenied observation is not independently assigned to a particular process.

To separate this explanation from speculation, a **standalone .NET 8.0.24
diagnostic** was compiled directly with the installed SDK 8.0.418 compiler and
local framework reference pack. It is outside all production/test projects,
does not load any XMLDocNormalizer assembly, uses no network and never repeats
a gate. Source, runtimeconfig, compiler log, run record and JSON result are
retained as `PidIdentityProbe.*`, `probe-build.log`, `probe-run.json` and
`pid-identity-probe.json` under the PID audit directory.

Minimal experiment: start an owned stdin-gated `cmd` child, record ID/creation
time, release it by a line of input, wait for exit, record exit code and dispose
the handle. Retain only primitive identity. Allocate subsequent identical owned
children (bound: 4096 allocations or 90 s; **zero sleeps**). On a reused ID, use
the exact old `GetProcessById(id).HasExited` lookup while the new child still
awaits its input, then complete and clean up that child too. This is a bounded
process-identity experiment, not a test retry loop.

It reproduced on its **single invocation**, allocation 139, in 2489 ms:

| Identity observation | Value |
| --- | --- |
| Original PID | 22292 |
| Original creation UTC | 2026-10-15T20:23:06.1282194Z |
| Original completion recorded UTC | 2026-10-15T20:23:06.1434376Z |
| Original owned HasExited / exit code | true / 0 |
| Replacement PID | 22292 |
| Replacement creation UTC | 2026-10-15T20:23:07.6669407Z |
| Lookup creation UTC | exactly the replacement creation, not the original |
| PID-only lookup HasExited | false |
| Replacement final cleanup / exit code | completed / 0 |

This proves the existing assertion's false-negative mechanism under the same
installed .NET runtime. It is **not a captured process trace of the unlogged
Broad PID**; that event cannot be reconstructed exactly from the available TRX.
Combined with the unchanged client's owned-exit guarantee, the varying old
failing cases and the unchanged passing isolated cases, the supported diagnosis
is an existing test-harness identity/lifetime race, with directly reproduced
Windows PID reuse. No actual Worker-lifetime leak was observed. A final authorized
read-only process scan at 20:23:35.5332665Z found zero dotnet/testhost command lines
belonging to this export. No process was killed by the audit; diagnostic children
completed normally with exit code 0.

### Stop decision and separate follow-up

The PID-gate defect is reproducible; an unchanged all-green closure was **not**
obtained. Broad was not repeatedly rerun until random success. Boundary remains
the previous 576/578 (not rerun); Dual-Version-Build and Evidence-Gesamtgate remain
failed from the previous attempt (not rerun). The prior 3258/3258 full suite,
eleven clean WAE builds, eight byte-equal baselines and artifact-consistency PASS
are retained **previous** results, not freshly executed closure gates. The new
audit distinguishes that original verifier snapshot from this blocked step.
Self Analysis 16 / Canonical Diff 0/0/0 remain inherited, not fresh.

Proposed separate package: **PID-G1 — Identity-bound Worker Cleanup Test Harness**.
First capture original PID **and process creation identity / completed owned
exit** plus the later lookup identity without weakening any assertion. Then
decide a single identity-bound cleanup observation at the actual client/test
ownership seam, preserving failure on a genuinely live original child. Include
a deterministic replacement-identity negative control, real cancellation/error/
timeout cleanup and unchanged B12–B23 regression/gate validation. Do not assert
the state of an unrelated current owner of a recycled PID. No sleeps, retries,
skips or production-lifetime changes are proposed as a substitute. Implementation
requires a separate user decision; none is performed here. B25 has not begun.

### Files, protection and status after this step

Only this report, the B24 JSON audit and new audit-only Proof scripts are added
or updated by this step. Generated diagnostic source/output remains ignored in
`artifacts/`. All 750 captured original production/test/build/fixture file hashes,
the test DLL hash, protected root-ignore/index bytes, 23 Core files, six stashes
and HEAD remain identical to the before snapshot. No production/test file was
formatted, edited or rebuilt. Proof scripts are parsed and their own text is
UTF8 without BOM / CRLF. Documentary audit collection is not represented as a
successful rerun of the blocked existing Evidence Gate.

Final HEAD: `4b6890b9b4977024cae5288efe84babd736a48a6`, `fix/documentation`.
Final index: unchanged and no staged diff. Final short status remains the
verbatim status above (the new Proof scripts are inside its already untracked
`Evaluation/P5O2B24Proof/` entry). No commit, push or stash operation.

## Final validation closure after separate PID-G1 repair

The preceding failed attempts and PID audit remain historical evidence. The
separate PID-G1 package repaired only original-process lifetime observation;
B24 Finding/CLI/Worker/selection/routing/reporting semantics and B21 authority
are unchanged. Its exact pre-WIP client-delta proof and file separation are in
`PID-G1-process-identity-lifetime-gate.md` and the corresponding JSON audit.

Fresh post-fix results: PID-G1 18 tests plus nine Proof controls; exact old cases
4/4; affected lifetime classes 540/540; Broad **3185/3185**; standalone Boundary
**596/596**; complete Dual-Version-Build **PASS**, with eleven WAE steps each
zero warnings/errors and its own **596/596 + 36/36 + 42/42** permanent gates.
All B5-B24 cohorts pass individually; productive **777/777**, focused **884/884**,
full configured suite **3276/3276**, all zero skips. The increase is exactly the
18 new lifetime tests, with no old test removed or excluded. Eight fresh Current
baselines remain byte-equal. Main/Worker/test images match the inputs used for
the targeted run across the same-export clean builds.

Final existing Evidence-Verifier / artifact-consistency result: **PASS**, actually
executed after all required runs; the separate PID-G1 Evidence Verifier also
passes. **B24 is fully closed in its bounded scope.** Self Analysis 16 and Canonical
Diff 0/0/0 remain inherited, not freshly run. Final scope/protection/Git inventory
is recorded by the separate PID-G1 verifier. No commit/push/stash mutation or B25
work. Final verifier repeats after documentary status updates do not rerun or
repair any test.
