# P5O2B8 - Integrated Analyzer Dispatch Boundary

Date: 2026-10-09. Starting HEAD: `2f4690398142cc409039eff91f389fb51d94c6ae`.
Mandatory initial status/stat/full diff/HEAD/log audit found only protected root
`.gitignore` WIP and no staged files. Fresh snapshots precede implementation;
Core output must not be rebuilt even though the CI fix now tracks its real project.

## Investigation and design recorded before implementation

The smallest composition location is `Execution/Analysis`, beside B5 Router,
not ToolRunner, a detector, the semantic host or a new Worker endpoint. B5 accepts
an explicit enum and one Current or Worker payload, already preserving native
Current session lifetime, canonical conversion and unchanged B4 process cleanup.
B6 owns the exact version policy. B7 alone constructs trusted immutable contexts.

Current input needs a native Current CSharpCompilation as compiler evidence and
an existing CurrentExceptionFlowAnalysisInput (member + caller-owned sequential
summary session). External provenance needs an existing target-bound, factory-
receipted PDB descriptor. Historical execution needs only WorkerAnalysisInput,
not a Current member/session or Roslyn object. Its context/profile/source/selector
validation and complete-result import remain B4 responsibilities.

Use a closed typed request hierarchy: native Current compilation + Current input,
or PortablePdb provenance + a closed payload union (Current session / Worker).
Each constructor takes required typed values, not a selection enum, raw compiler
string or a collection of nullable/boolean switches. Payload describes available
execution input, never chooses an engine. Thus Current PDB can execute Current;
Historical PDB can execute Worker; contradictory engine/payload combinations
reach the unchanged router's InvalidInput check and never fall back or convert.
The type system cannot represent both provenance sources or both payloads. Null
values forced by invalid internal callers remain fail closed at existing layers.

Dispatch invokes B7 Project once, checks success; invokes B6 Select once, checks
success; invokes B5 AnalyzeAsync once with B6's enum and the supplied typed payload.
Staged result retains original projection, decision and routing contracts, with
later stages absent when an earlier stage fails. Dispatch compares no versions,
validates no raw metadata, creates no compilation/session, runs no Analyzer,
starts no Worker and performs no artifact discovery/reporting. No lower-layer
change, cycle, call-site migration or new execution profile is required.

Trust precondition: caller supplies the described existing compilation/member/
session or already prepared external analysis payload. Compiler provenance does
not itself prove whole-source/reference/options equivalence with the PDB's target.
B8 must not claim or reconstruct full external input equivalence. Real controlled
Historical integration reuses B7's actual Historical-runtime-emitted PE/PDB and
its known fixed source, never the SDK-build PDB of the Analyzer assembly.

Architecture: trusted typed request -> Dispatch -> B7 Projection -> B6 Selection
-> B5 Router -> existing Current session / existing B4 isolated Historical client.
The lower owners remain byte-identical; tests and the permanent gate will prove
both successful chains, staged early failures, contradictory payload rejection,
parity, deterministic repetition, runtime isolation and failure process cleanup.

## Implemented entry and typed input

`Execution/Analysis/ExceptionFlowAnalysisDispatch.cs` introduces
`ExceptionFlowAnalysisDispatch.AnalyzeAsync(request, cancellationToken)`, configured
with the existing concrete B5 router. No new lifetime, delegate-plugin interface,
mock execution stack or public engine-choice parameter was introduced.

`ExceptionFlowAnalysisDispatchRequest.cs` contains two sealed request forms below
a private-constructor base:

- `CurrentCompilation(CSharpCompilation, CurrentExceptionFlowAnalysisInput)`.
- `PortablePdb(ExternalCompilationProvenanceDescriptor, ExecutionPayload)`.

The closed payload forms are `CurrentSession(CurrentExceptionFlowAnalysisInput)`
and `Worker(WorkerAnalysisInput)`. Constructors require two typed values (or one
payload value), not boolean/nullable parameter combinations. Neither both sources
nor both payloads can be represented. The native form supplies only Current data,
but does not select Current: its actual B7-projected identity is still decided by
B6. The PDB form supports both recorded Current and Historical identity without
relabeling PDB origin or guessing an engine from payload type.

The immutable staged `ExceptionFlowAnalysisDispatchResult` exposes original
`Projection`, optional `Decision`, optional `Routing` and a canonical `Result`
only when every stage succeeded. Null later-stage fields mean **not reached**,
not fabricated successful empty output. It retains the original typed errors,
native Current result, Worker diagnostics/identity/provenance and process ID.

## Stage contracts and direction

| Stage | Input -> output | Trust boundary | Failure contract |
| --- | --- | --- | --- |
| B8 request mapping | One typed request -> existing B7 ProjectionInput | Caller-owned compilation/member/session association or prepared external payload; not external discovery | Forced null source goes to existing typed B7 failure; dual sources/payloads excluded structurally |
| B7 projection | Existing typed evidence -> ProjectionResult / private immutable Context | Native loaded Current universe or actual original target-bound PDB validator receipt | Original projection failure; context, decision and routing absent |
| B6 selection | Successfully projected Context -> SelectionDecision | Unchanged exact compiler build plus origin rules | Original typed failure and Unspecified; router not invoked |
| B5 routing | B6 enum + one supplied payload + same token -> RoutingResult | Existing branch/payload checks; original Current session or original B4 client | Original routing failure or complete original Worker failure; no result or fallback |
| Existing execution | Current member/session or bounded neutral WorkerAnalysisInput -> original analysis/canonical result | Current semantic environment/session; B4 profile, exact isolated identity and canonical import validation | Existing Current uncertainty semantics, or original fail-closed Worker errors and process cleanup |

The dependency edges are Dispatch -> Projection, Dispatch -> Selection,
Dispatch -> Router, Router -> HistoricalClient. Projection and Selection do not
depend on Router, and Router does not depend on SelectionPolicy. None depends back
on Dispatch. This is a directed composition, not a cyclic protocol or Analyzer.
There is no version constant, version comparison, provenance validator, process
start, dynamic load, source parser, Analyzer call, discovery or reporting in either
new production file. Selection/execution each have one original owner.

## Current / Historical execution and fail-closed evidence

Current uses `request.Current.Session.Analyze(request.Current.Member)` only inside
unchanged B5. Sequential repetition keeps the same existing graph/session; direct
Current results and uncertainty match. Native Current and real Current PE/PDB
requests succeed with an absent or deliberately unstartable Worker endpoint,
proving that no Historical deployment/process is required for Current.

Historical uses the real **runtime-emitted** B7 Historical PE/PDB, validated through
existing assembly binding, target PE debug directory and PDB identity/checksum
factories. The original receipt is preserved. Its known fixed source is dispatched
via unchanged B5/B4, with exact original emitting and Worker engine identities.
Two separate Worker calls have canonical equality with direct Current and with
each other, separate process IDs, process exit, stable Current MVIDs, and no
Historical Roslyn/Worker assembly loaded in the caller. Fixture emission accepts
no submitted source and is not part of production dispatch.

Projection negatives include missing/null/raw/copied evidence. Tests use a null
router sentinel for these and for unsupported validated compiler identities:
projection failures have no decision/routing; selection failures have no routing
and cannot dereference that sentinel. Source architecture guards additionally
prove each downstream invocation occurs only after the preceding success check.
Synthetic unknown-version options are existing-validator unit fixtures with a
supplied expected root, not claimed real PE equivalence proofs.

Conflicting provenance **sources** cannot be represented by the new hierarchy;
the unchanged B7 suite still independently tests its dual-source rejection.
Conflicting selected-engine/payload pairs are tested in both directions: historical
PDB + Current payload and Current PDB + Worker payload fail with original B5
InvalidInput, never conversion, repair, fallback or Worker start.

Existing Current cancellation/execution errors, unavailable Historical endpoint,
unsupported Worker profile, startup error, real structured compilation/analysis
failures, adversarial crash/malformed/protocol/nonzero/flood/UTF8 failures, timeout
and active cancellation retain original typed failures and no canonical/Current
result. Started processes terminate on success and error. The adversarial process
is the existing B4 test-only pipe peer, not a new Worker implementation or proof
of source/PDB equivalence for intentionally failing inputs.

## Tests, builds and quality

| Fresh B8 validation | Passed |
| --- | --- |
| New Dispatch cases | 37/37 |
| Independent B5 / B6 / B7 cases | 26/26 / 40/40 / 30/30 |
| Combined independent B5-B8 boundaries | 133/133 |
| Worker / runtime capability / architecture / dependency focused regressions | 278/278 |
| Broad Check / Execution / Evaluation / Worker modules | 2724/2724 |
| Complete configured normal suite | 2814/2814 |
| Expanded permanent Main boundary gate | 212/212 |

These overlapping suites are not additive unique-test counts. Final suites have
zero failed/not-executed tests. The complete normal suite retains the existing
default SelfAnalysis exclusion; no runsettings changed and no fresh B8 self-analysis
is claimed. The initial narrower semantic/external/evaluation/Worker sweep also
passed 1112/1112 and is retained as `artifacts/p5o2b8/tests/broad.trx`.
The full suite passed its first B8 full attempt; there was no full-suite retry.

Current, Historical, Worker, test processes, real PDB fixture and test assembly
WAE builds all pass **0 warnings / 0 errors**. Permanent gate retains six forced
Current/Historical images, two Worker images, 121 shared Analyzer sources and one
Historical host, exact package/runtime references, deterministic images, isolated
execution and original fail-closed protocol checks. It adds only B8's test filter;
no old test/filter/build/reference gate is removed. Core is not rebuilt: the
default `BuildTestProjectReferences=false` path is intentional repository protection,
not an omitted product build. The preceding separate CI fix already proved the
real tracked Core project and reference-enabled clean build.

The first new-test build had six syntax diagnostics from one extra closing
parenthesis in an evidence expression; the second had one wrong existing enum
name. Both were test-only corrections, with logs retained. The first focused
run had 28 passed / 9 failed: a wrong B4 adversarial fixture output path and a
reflection guard omitting public primary constructors of internal classes.
Correcting these test assumptions yielded 37/37, without a production workaround
or relaxation. Formatter-requested new initializer whitespace was corrected.
Initial build/TRX logs remain under `artifacts/p5o2b8/`.

Architecture tests verify the exact directed calls/short circuits, no backlinks,
no copied policy/validation/Worker logic, closed required typed inputs, original
session behavior and no Historical references in Main. Fresh fingerprint checks
prove **all 370 pre-existing Main C# sources**, 121 shared Analyzer files, 13
existing Worker/Historical physical files and six existing Worker test files
unchanged. All B5-B7 suites remain independent; no existing test was replaced.
All 23 current Core files (including all 22 historically protected files and the
CI fix's local ignore file), root ignore WIP and six stashes remain unchanged.

`P5O2B8Proof/Write-Verify-DispatchEvidence.ps1` checks actual TRX/build/provenance/
runtime evidence, SHA256s, protected bytes/file sets, PowerShell/XML/JSON parse,
strict UTF8/CRLF/final newline, folder-only whitespace and git diff checks.
Folder-only formatting never opens a Core MSBuild workspace. Machine-readable
audit mirrors generated verified output; it excludes its own fingerprint to
avoid a self-hash cycle. Existing root ignore LF/CRLF warning is not altered.

## Self Analysis / Canonical Finding Diff

Both are **inherited, not newly executed in B8**. The committed B7 baseline and
its evidence lineage supply Self Analysis **16** findings: DOC611 1, DOC631 15,
DOC610/DOC632 0. Canonical added/removed/changed-evidence remains inherited
**0/0/0**, with equal raw and normalized arrays. B8 only adds orchestration; all
existing Analyzer, session, projection, selection, routing and client bytes are
unchanged. This is not a claim of fresh self-analysis of new B8 documentation.
The prior CI-fix full-suite SelfAnalysis test is a separate historical run, not
relabeled B8 evidence or a newly measured canonical finding comparison.

## Changed files, reproduction and remaining scope

New production files: `Execution/Analysis/ExceptionFlowAnalysisDispatch.cs` and
`ExceptionFlowAnalysisDispatchRequest.cs`. New tests:
`Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs`.
Existing edits: one B8 filter addition in `build/Verify-DualVersionBuild.ps1`,
and only achieved integrated-entry progress in OPEN's BND-P6-002/BND-P5O2B-003.
Evidence: this report, `P5O2B8-integrated-analyzer-dispatch-audit.json` and
`P5O2B8Proof/Write-Verify-DispatchEvidence.ps1`. Root ignore WIP is not a B8 change.

From the tool directory, preserving protected Core:

```powershell
dotnet build src/XMLDocNormalizer/XMLDocNormalizer.csproj --no-restore -warnaserror
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B8Proof/Write-Verify-DispatchEvidence.ps1
```

The verifier checks this concrete run's fresh snapshot and saved artifacts; do
not recreate/replace the initial snapshot to hide a changed protected file.
The process-only script switch does not change persistent machine policy.

CLI/reporting/Dapper call-site adoption, full external execution-input/source/
reference/options equivalence, automatic project/PDB discovery, larger Worker
profiles, additional Historical versions and dynamic loading remain out of scope
and open. Compiler selection is not proof of full artifact reconstruction.
No repository-wide migration, Dapper advancement or new Analyzer finding behavior
is claimed. Stop here after B8's integrated Main boundary.

## Final repository audit

HEAD stays `2f4690398142cc409039eff91f389fb51d94c6ae` throughout B8: no external
HEAD movement. No commit, push, index, stash, reset, restore or checkout operation
was performed. Root ignore bytes, all Core/source/project/boundary fingerprints
and six stash hashes/messages match the pre-change snapshot.

Final `git status --short` (also captured in machine-readable evidence):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B8-integrated-analyzer-dispatch-audit.json
?? Evaluation/P5O2B8-integrated-analyzer-dispatch.md
?? Evaluation/P5O2B8Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisDispatch.cs
?? src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisDispatchRequest.cs
```
