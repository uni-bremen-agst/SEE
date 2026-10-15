# PID-G1 — Process identity-safe lifetime gates

## Before-code audit and decision matrix

Audited HEAD: `4b6890b9b4977024cae5288efe84babd736a48a6`, branch
`fix/documentation`. Existing B24 WIP is authoritative. Protection and complete
owned-WIP copies/hashes are captured before changes under `artifacts/pid-g1/`.
Actual protected counts: 23 Core files, six stashes; index has no staged diff.
Root `.gitignore` is unrelated protected WIP. No repository AGENTS.md applies.

Repository-wide source/PowerShell search covers `GetProcessById`, `Get-Process
-Id`, `Win32_Process`, `ProcessId`/saved IDs, `HasExited`, `WaitForExit[Async]`
and process polling. Matches outside XMLDocNormalizer are unrelated Unity/plugin
process owners and are not changed. The matrix precedes product/test edits.

| Location | Purpose and owner | Current assumption / disposal boundary | PID reuse? / common cause | Planned correction |
| --- | --- | --- | --- | --- |
| HistoricalWorkerClientTests; AnalysisRouter/Dispatch; AnalyzerSelectionPolicy; ProductionPipeline; PayloadProjection; InputEquivalence | Actual Main client child cleanup / isolation | Client owns Process, awaits exit/EOF, disposes before returning; test later reopens saved PID | Yes: PID-only reacquisition after disposal | Require exact original-call owned-exit observation, not global lookup |
| B12 ReferenceIdentity; B13 SourceContext; B14 Scope; B15 SolutionScope | Exact flow/source/reference/scope parity plus cleanup | Same client; `Stopped(int pid)` after other Worker/graph executions | Yes, same identity loss | Pass original call, require its exact owned-exit observation |
| B16 DeclaredBoundary; B17 DeclaredPolicy; B18 DeclaredReadiness | Old contract/guard witnesses plus cleanup | Same client; saved PID or call, then global lookup | Yes, same cause | Same shared lifetime assertion; leave all semantic assertions |
| B19 FindingContract; B20 ProductionAdmission; B21 DocumentationAttestation; B22 DeclaredProductionFinding | Genuine/replayed/failed child cleanup | Same client; successful and failed calls followed by `GetProcessById` | Yes, including real adversarial peers | Same exact-call check for every actually started child, independent of success/B21 |
| Client/Process/InputEquivalence/Dispatch/B21 repeated-worker tests | Distinct actual process executions | Compare sequential numeric PIDs for inequality | Yes: a later genuine fresh execution may reuse the ID | Compare per-execution identity, keep concurrent caller-vs-child PID checks |
| HistoricalWorkerProcessTests direct Run helper | Direct Worker host lifetime | Own Process held across WaitForExit/EOF, disposed only after return expression | Existing exit wait is safe; repeated PID-inequality is not | Capture exit on owned Process before disposal; distinct execution observation |
| B24 new AssertCompletedExchange | Actual completed B21 opt-in execution | Exact original-call private receipt, no PID lookup | No PID-reuse dependence | Preserve B24 file and all B24 behavior |
| ExternalTargetCliTests, real graph/fixture helpers, SelfAnalysis, mode comparison, Evaluation runner, ContextualBoundaryAudit, build/Verify-DualVersionBuild | Own direct CLI/fixture/helper process completion | Hold actual Process across exit and output; no deferred PID identity | No | Retain owner-based waits/cleanup; add new regression class to permanent gate only |
| B4/B8 Proof fresh-worker checks | Distinct repeated execution | Persisted PID inequality | Yes | Persist and compare owned-execution identifiers |
| B8/B9/B10/B11/B12/B13 Proof cleanup | Later evidence verification | `Get-Process -Id`, missing treated as exit; B13 requires missing | Yes; absent PID alone also insufficient as lifetime evidence | Require serialized original owned-exit facts from fresh completed tests |
| B14/B15 Proof cleanup | Later evidence verification | PID lookup plus command-line/path heuristic to label reused process | Yes; same executable can be a different execution | Replace with exact owned-exit facts; no name/path heuristic |
| B16–B22 Proof final live scans | Supplementary validation-directory orphan census | Enumerate current command lines, no saved-PID identity assertion | No saved-PID reuse assumption; Windows-only census is not proof of individual exit | Preserve as supplementary census, never substitute for identity-bound lifetime checks |
| B23/B24 final verifier, permanent Boundary/Dual gate | Aggregate actual TRX/source/runtime validation | Execute above tests; no independent saved-PID lookup | Inherits old test defects, otherwise owner-based | Include PID-G1 regressions, retain all old gates and actual failure history |
| Existing PID audit standalone allocation probe | Demonstrate old defect | Intentionally looks up reused ID to show a false negative | Intentionally yes, diagnostic only | Preserve historical experiment, not a lifetime PASS gate |

For every Main-client row the intended assertion is **the actual original child
has exited**, not absence of an integer from the global process table. It is not
a generic descendant census. Client error/timeout paths already kill the owned
tree and await the original; direct fixtures retain their own Process objects.
No arbitrary unowned OS process may count either as a leak or as successful
cleanup. Start failure / pre-cancellation has no started process to observe.

### Chosen minimum ownership seam

Use the already-held actual Process (approach A) to record a completed owned exit
before disposal. For later callers retain a small immutable, original-call-bound
exit observation (approach C, existing private CWT pattern), not a re-query and
not a second security/attestation service. The client owns the only registration;
raw response validation and cloned/fabricated calls cannot acquire it. Capture
checks actual HasExited/ExitCode on the retained Process; running, missing or
disposed objects do not silently pass. The observer never changes wait/kill/
timeout/pipe behavior and never admits analysis facts. B21's request/response/
image authority, canonical validation and worker protocol stay unchanged.

Each actual exit observation has a unique execution identifier, independent of
the reusable PID. Keep the PID for diagnostics and matching the exact call.
Persist the observation in test/proof evidence where later verification needs
it; do not turn JSON self-consistency into B21 authority. Different sequential
executions may have the same PID but never share an owned observation.

Regression plans: live original is not stopped; completed original is stopped;
deterministic same-PID replacement cannot change the completed original's state;
other/cloned/missing observations cannot pass; disposed/unqueryable ownership
fails closed; real ordinary/error/cancellation/timeout cleanup remains required.
No random PID allocation is required by the permanent regression tests.

## Implementation / validation

### Implemented ownership semantics

`OwnedProcessExit.Capture` queries HasExited and ExitCode on the **actual owned
Process before disposal**, rejecting a still-running owner and propagating
missing/unstarted/disposed/unqueryable ownership errors, rather than silently
passing them. Previously captured immutable facts remain readable after disposal.
A small weak-key observation cache for the one-launch-per-owner architecture ensures repeat
capture of the same owner has the same execution identity. It retains only
immutable primitives, never an OS handle or Process. The execution ID identifies
the owned observation, not a globally recoverable OS descriptor or security nonce.

The client's separate private weak-key map brands the **original returned call**
only after its unchanged finally has awaited exit and pipe cleanup. Computed
`HistoricalWorkerCallResult.ProcessExit` exposes those local diagnostics to tests
and their evidence. No new field is added to the Worker request/response protocol.
Cloning or constructing a call with the same PID cannot transfer the observation.
It is not B21 authority and cannot admit any analysis/Finding facts.

All 18 deferred-PID test helper locations now pass the exact call to one shared
assertion. It requires an actual branded exit, matching diagnostic PID, nonempty
execution identity and ended state; missing observations do not pass. Safe
concurrent caller-versus-child PID checks remain. Five repeated-worker PID-
inequality assertions now compare the observations' execution identities.
The direct B3 Process test helper captures on its own retained Process as well.
No wait/kill/deadline/cancellation/pipe/error or Analyzer behavior changes.

The previous B24 `AssertCompletedExchange` file is preserved byte-for-byte.
Main client overlap is explicitly bounded: `Verify-Scope.ps1` removes only the
new diagnostic fields/accessor, return-observer wrappers/local and final capture,
then requires **exact equality** with the captured pre-PID-G1 B24 client. This
proves B21 authority and lifetime control are unchanged, not merely selected
assertions. The call-result change is likewise restricted to its computed getter.
All other existing B24 semantic/Worker/fixture/build inputs retain their hashes.
The 121 actual exported Analyzer source hashes equal B23; local normalized source
content matches the export (some untouched local files use LF, export uses CRLF).

Proofs B3/B4/B8/B9/B10/B11/B12/B13/B14/B15/B21 now consume actual original-owned
exit metadata rather than later PID/path/name guesses or sequential PID
inequality. B16–B22 supplementary Windows command-line orphan censuses are
unchanged: they do not reacquire saved PIDs and do not replace per-child exit
proof. Historical milestone verifiers retain their other milestone-specific
counts/source assumptions; they are not misrepresented as current full gates.
PID-G1 checks their new lifetime predicates against fresh actual metadata in
addition to the current complete B24 and dual-version gates.

### Deterministic and actual regressions

New `HistoricalWorkerLifetimeTests`: **18 tests**. Live original cannot mint an
exit; completed original retains its observation after disposal; repeated capture
is stable; a simulated replacement call with the **same PID** cannot borrow the
original's authority or invalidate its stopped state; a different genuinely live
child does not become the exited original. Missing/invalid/disposed ownership,
record cloning and exit/observation races are explicit controls. The replacement
model is deterministic at the original-call boundary, not random Windows PID
recycling. Tests do not use sleeps or retry logic.

Ten real normal/failure outcomes cover genuine Worker success, compilation error,
crash, malformed/nonzero/invalid-UTF8 peers, stdout/stderr budget failures,
timeout and cancellation. Start failure and pre-cancellation cannot mint a
started-child exit observation. The existing real deadline/cancel/tree-kill
cleanup tests remain in the targeted/current gates. Nine deterministic
PowerShell controls additionally reject missing/wrong/live/invalid evidence and
duplicate execution identity, while accepting two completed distinct executions
with the same numeric PID.

Windows/.NET 8.0.24 is actually executed. The fix uses cross-platform retained
Process APIs and weak reference identity; no StartTime query, Windows PID table,
process-name/path heuristic or Windows-only new native API is introduced. No
Linux execution is claimed; existing Linux-relevant Git path/build assumptions
remain separately checked.

### Retained setup/build attempts

1. Two read-only patch-orchestration reads failed before editing: aggregate
   output truncation, then PowerShell extended-string JSON shape. Per-file plain
   text reads corrected the setup. Actual edits use apply_patch.
2. First test WAE build reported four CS8602 diagnoses on new evidence accesses
   whose previous helper-call nullable-flow suppression had changed. Explicit
   non-null annotations at already-proved original calls corrected the new
   accesses; no warning was disabled. Full first/second/formatted build logs stay
   under `artifacts/pid-g1/`. Second and formatted builds: zero warnings/errors.
3. First new regression execution: 18/18, no skips. Formatted final targeted
   execution subsequently includes all 18 again; no assertion fix or test retry.
4. Initial scope verifier compared untouched local LF text hashes to prior
   exported CRLF bytes. Corrected proof requires exact **built** source hashes
   and equal normalized local content; no source was changed for this check.
5. A manual metadata-probe one-liner could not dot-source under the shell's
   execution policy and printed unreliable trailing labels after nonterminating
   errors. Those labels are **not validation results**. The strict process-local
   `-ExecutionPolicy Bypass` repeat completed all five real metadata predicates.
   Permanent scripts already use Stop and process-local Bypass, not policy changes.

### Fresh validation census

Completed so far: new tests 18/18; exact B18/B22/B12 isolated cases 4/4; all
affected old lifetime classes plus new tests **540/540**; nine Proof controls;
Broad **3185/3185**, all no skips. The original B24/PID audit failed runs and
unchanged 3/3 isolated repeat are preserved, not replaced with these results.
Boundary, full dual-version build and remaining closure gates follow sequentially.
Final actual results and complete Git/status/separability inventory are recorded
by `PIDG1Proof/Write-Verify-Evidence.ps1`. No commit, push, stash mutation or B25.

## Separability from the pre-existing B24 WIP

The before snapshot contains 28 owned B24 working-tree files, separately from
protected root `.gitignore`. It is not inferred from HEAD; original file bytes
are retained under `artifacts/pid-g1/before-wip/`. Final JSON inventories every
current owned file with `AlreadyB24Wip` and `ChangedByPidG1` flags.

| File group | PID-G1 delta / ownership |
| --- | --- |
| `src/XMLDocNormalizer/Execution/Historical/OwnedProcessExit.cs` | New PID-G1-only primitive observation; no policy/analysis dependency |
| `HistoricalWorkerCallResult.cs` in the same directory | PID-G1-only computed local diagnostic getter |
| `HistoricalWorkerClient.cs` | **Mixed**: preserve B24's existing Finding opt-in line; PID-G1 adds only the registry, return observers and final owned capture. Exact pre-WIP restoration comparison proves that boundary |
| B24 Finding/CLI/projection/validation/ToolRunner/Worker sources, `HistoricalFindingFacts.cs` | Existing B24 WIP, unchanged by PID-G1 |
| `ExceptionFlowHistoricalBoundedFindingTests.cs`, B4 `TestProcess/Program.cs`, permanent `build/Verify-DualVersionBuild.ps1` | Existing B24 WIP, byte-unchanged. New test class automatically matches the existing permanent `HistoricalWorker` filter |
| B24 Proof `Capture-ValidationInputs.ps1`, `Run-Regressions.ps1`, `Write-Verify-Findings.ps1` | **Mixed**: additive separate input snapshot, additive aggregate test filter, explicitly scope-verified PID-G1 census/input support; original default behavior and historical attempts retained |
| B24 report/audit and boundary register | Existing B24 evidence; final closure status only, no rewritten failure history |
| PID-G1 report/audit and `Evaluation/PIDG1Proof/*.ps1` | New PID-G1-only audit/regression/verification package |

PID-G1-only modified test files, all under `Tests/XMLDocNormalizerTests/Worker/`:

```text
HistoricalWorkerClientTests.cs
HistoricalWorkerProcessTests.cs
ExceptionFlowAnalysisRouterTests.cs
ExceptionFlowAnalysisDispatchTests.cs
ExceptionFlowAnalyzerSelectionPolicyTests.cs
ExceptionFlowProductionPipelineTests.cs
ExceptionFlowHistoricalPayloadProjectionTests.cs
ExceptionFlowHistoricalInputEquivalenceTests.cs
ExceptionFlowHistoricalReferenceIdentityTests.cs
ExceptionFlowHistoricalSourceContextTests.cs
ExceptionFlowHistoricalScopeTests.cs
ExceptionFlowHistoricalSolutionScopeTests.cs
ExceptionFlowHistoricalDeclaredBoundaryTests.cs
ExceptionFlowHistoricalDeclaredPolicyTests.cs
ExceptionFlowHistoricalDeclaredReadinessTests.cs
ExceptionFlowHistoricalFindingContractTests.cs
ExceptionFlowHistoricalProductionAdmissionTests.cs
ExceptionFlowHistoricalDocumentationAttestationTests.cs
ExceptionFlowHistoricalDeclaredProductionFindingTests.cs
```

New PID-G1-only test files in that directory:
`HistoricalWorkerLifetimeAssertions.cs` and `HistoricalWorkerLifetimeTests.cs`.
Changes in old tests are only lifetime assertions, stable fresh-execution checks,
their necessary non-null flow annotations and lifecycle evidence fields. Original
Finding/flow/type/provenance/document/graph/attestation assertions remain.

PID-G1-only modified historical Proof files:

```text
Evaluation/P5O2B3Proof/Write-Verify-WorkerEvidence.ps1
Evaluation/P5O2B4Proof/Write-Verify-BoundaryEvidence.ps1
Evaluation/P5O2B8Proof/Write-Verify-DispatchEvidence.ps1
Evaluation/P5O2B9Proof/Write-Verify-ProductionEvidence.ps1
Evaluation/P5O2B10Proof/Write-Verify-PayloadEvidence.ps1
Evaluation/P5O2B11Proof/Write-Verify-InputEvidence.ps1
Evaluation/P5O2B12Proof/Write-Verify-ReferenceEvidence.ps1
Evaluation/P5O2B13Proof/Write-Verify-ContextEvidence.ps1
Evaluation/P5O2B14Proof/Write-Verify-ScopeEvidence.ps1
Evaluation/P5O2B15Proof/Write-Verify-SolutionEvidence.ps1
Evaluation/P5O2B21Proof/Write-Verify-BindingAttestation.ps1
```

No staging/commit is planned automatically: mixed files require hunk-aware review
if a later separate commit is requested. Root ignore, Core outputs and all stash
identities are explicitly outside the change set.

## Final executed validation and repository audit

| Fresh execution | Passed / total or result | Failed / skipped |
| --- | ---: | ---: |
| New lifetime tests, initial run | 18 / 18 | 0 / 0 |
| Deterministic Proof predicate controls | 9 / 9 | 0 / 0 |
| Exact previous B18/B22/B12 cases | 4 / 4 | 0 / 0 |
| All affected lifetime classes, including new tests | 540 / 540 | 0 / 0 |
| B24 | 42 / 42 | 0 / 0 |
| B23 | 36 / 36 | 0 / 0 |
| B22 / B21 / B20 | 46 / 46; 26 / 26; 10 / 10 | 0 / 0 |
| B19 / B18 / B17 / B16 | 39 / 39; 7 / 7; 25 / 25; 18 / 18 | 0 / 0 |
| B15 / B14 / B13 / B12 | 28 / 28; 31 / 31; 34 / 34; 29 / 29 | 0 / 0 |
| B11 / B10 / B5-B9 | 16 / 16; 34 / 34; 156 / 156 | 0 / 0 |
| Productive, old scope plus 18 new lifetime tests | 777 / 777 | 0 / 0 |
| Focused, same additive extension | 884 / 884 | 0 / 0 |
| Broad, same additive extension | 3185 / 3185 | 0 / 0 |
| Standalone permanent Boundary | 596 / 596 | 0 / 0 |
| Full Dual-Version-Build | PASS, eleven clean WAE build steps | 0 warnings / 0 errors each |
| Boundary inside that full script | 596 / 596 | 0 / 0 |
| B23 / B24 inside that full script | 36 / 36; 42 / 42 | 0 / 0 |
| Full configured suite | 3276 / 3276 | 0 / 0 |
| Existing B24 Evidence-Gesamtgate / verifier | PASS, actual complete invocation | 0 failures |
| PID-G1 Evidence Verifier | PASS, actual complete invocation | 0 failures |
| Artifact consistency / source / format / protection gates | PASS | No suppressed errors |

All required post-fix test invocations passed without a failing-test retry. The
standalone Boundary and later Boundary inside the full build are separate
explicitly required workflow steps, not a retry loop. Source and Main/Worker/test
DLL hashes remain identical across targeted, broad and same-export clean-build
inputs; the entire full suite is fresh after the final build.

Eight full Current JSON baselines are freshly byte-equal to B23. Source/physical
Worker runtime/loaded Roslyn identities and canonical repeat/protocol rejection
are checked by the actual full gate. Source scope proves no Analyzer, selection,
dispatch, routing, Worker protocol/version or Finding-policy change. Main DLL/PDB
and tests change for lifecycle diagnostics; Worker/fixture semantic sources are
unchanged relative to the captured B24 WIP. No cross-export DLL equality claim.

Self Analysis **16** and Canonical Finding Diff **0/0/0** are **inherited**, not
freshly executed. No Linux runner execution is claimed. The actual Windows test
and runtime results, Git case/path checks and platform-independent ownership
design are distinguished. Final read-only export-process census at
2026-10-15T21:54:56.9602310Z found zero live dotnet/testhost command lines belonging
to this validation tree; no process was killed by that census.

Evidence entry points, from the original tool root:

```powershell
# Existing B24 export/local offline caches and preserved before snapshots are prerequisites.
# Run records reject overwriting attempts; a new approved rerun needs fresh record paths.
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/PIDG1Proof/Run-Validation.ps1 -Phase Targeted
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/PIDG1Proof/Run-Validation.ps1 -Phase Broad
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/PIDG1Proof/Run-Validation.ps1 -Phase Boundary
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/PIDG1Proof/Run-Validation.ps1 -Phase Dual
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/PIDG1Proof/Run-Validation.ps1 -Phase Regressions
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/PIDG1Proof/Run-Validation.ps1 -Phase Full
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/PIDG1Proof/Write-Verify-Evidence.ps1
```

The last command independently checks actual TRX counts/freshness/no-skips,
real lifecycle outcomes, legacy Proof exit facts, complete gate/audit links,
exact source/image overlay and original protected state; it runs the existing
B24 verifier with explicit `-AfterPidG1` input/census handling. The old B24 input
snapshot and failed audits are not overwritten or relabelled as successful runs.
Its output stays false if interrupted or unsuccessful.

HEAD stays `4b6890b9b4977024cae5288efe84babd736a48a6` on `fix/documentation`.
Index bytes are unchanged and staged diff is empty. All 23 original Core files,
root `.gitignore` and six stash identities retain their before hashes/reflog.
No original-tree Core build, commit, push or stash mutation; no B25 was begun.

**PID-G1 is complete; B24 validation closure is fully PASS in its existing
bounded scope.** Both final Evidence Verifiers passed on their first actual
complete invocation. The subsequent final repeat only refreshes hashes after
this documentary PASS/status update; no product/test change or test retry.
The old blocked state remains in the unchanged historical B24 sections and
archived audits. B25 has not been planned or begun.

Final short status (existing B24 WIP and PID-G1 are distinguished above and in
the machine inventory; the first entry is protected unrelated WIP):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Evaluation/P5O2B10Proof/Write-Verify-PayloadEvidence.ps1
 M Evaluation/P5O2B11Proof/Write-Verify-InputEvidence.ps1
 M Evaluation/P5O2B12Proof/Write-Verify-ReferenceEvidence.ps1
 M Evaluation/P5O2B13Proof/Write-Verify-ContextEvidence.ps1
 M Evaluation/P5O2B14Proof/Write-Verify-ScopeEvidence.ps1
 M Evaluation/P5O2B15Proof/Write-Verify-SolutionEvidence.ps1
 M Evaluation/P5O2B21Proof/Write-Verify-BindingAttestation.ps1
 M Evaluation/P5O2B3Proof/Write-Verify-WorkerEvidence.ps1
 M Evaluation/P5O2B4Proof/TestProcess/Program.cs
 M Evaluation/P5O2B4Proof/Write-Verify-BoundaryEvidence.ps1
 M Evaluation/P5O2B8Proof/Write-Verify-DispatchEvidence.ps1
 M Evaluation/P5O2B9Proof/Write-Verify-ProductionEvidence.ps1
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionPipelineTests.cs
 M Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs
 M Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerProcessTests.cs
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs
 M src/XMLDocNormalizer/Cli/ArgsParsing.cs
 M src/XMLDocNormalizer/Cli/ToolOptions.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerDeclaredDocumentationProjection.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
 M src/XMLDocNormalizer/Execution/ExternalTargetExecution.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerCallResult.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
 M src/XMLDocNormalizer/Execution/ToolRunner.cs
?? Evaluation/P5O2B24-bounded-finding-contracts-audit.json
?? Evaluation/P5O2B24-bounded-finding-contracts.md
?? Evaluation/P5O2B24Proof/
?? Evaluation/PID-G1-process-identity-lifetime-gate-audit.json
?? Evaluation/PID-G1-process-identity-lifetime-gate.md
?? Evaluation/PIDG1Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs
?? Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerLifetimeAssertions.cs
?? Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerLifetimeTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/HistoricalFindingFacts.cs
?? src/XMLDocNormalizer/Execution/Historical/OwnedProcessExit.cs
```
