# P5O2B5 - Explicit Current/Historical Selection and Main Routing

Date: 2026-10-08. Starting/final HEAD:
`c57203e64ab4a9902b4c9a11b22b250ff5ae10a8`
(`Add historical analysis worker boundary`). B4 separately committed.
Initial audit ran git status, diff stat, full diff, HEAD and log before edits;
only the protected root ` M ../../.gitignore` was dirty.

P5O2B5 closes explicit selection and central Main routing. No automatic selection,
CLI/Detector/reporting adoption or Dapper pipeline extension is implemented.

## Smallest seam and productive ownership

Inspected the committed B4 client, request/result/validation contracts, real Worker,
Current detector and existing SummarySession lifetime before choosing the seam.
The smallest safe cut is above the existing caller-owned SummarySession and B4
client, not inside either Analyzer or a duplicated IPC mechanism.

Common Main entry: `XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalysisRouter`
in `src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisRouter.cs`, method
`AnalyzeAsync(ExceptionFlowAnalysisRoutingRequest, CancellationToken)`.

`ExceptionFlowAnalyzerSelection` is an explicit enum: Unspecified=0, Current=1,
Historical=2. Default, null request and unknown numeric values fail InvalidSelection;
none silently defaults to Current. Exactly the selected branch input must be present.
Both inputs, the wrong input, missing input/member/session fail InvalidInput before
either branch executes. Historical without a configured endpoint fails
HistoricalUnavailable. There is no fallback, metadata query or version heuristic.

The request/result are **Main-local composition types, not IPC DTOs**. The Current
input contains a member and its existing sequential SummarySession. Its syntax,
semantic objects, graph and caches are never serialized. The Historical input is
the already validated B4 WorkerAnalysisInput, not a second transport schema.

## Current route

Current calls exactly `request.Current.Session.Analyze(request.Current.Member)`,
the existing in-process entry used by the detector, then the existing canonical
adapter. The caller retains the session, graph, model and cache lifetime. No new
session per request, thread pool dispatch, concurrency policy or Analyzer algorithm
is introduced. Current requires no Worker deployment, even if the configured Worker
host cannot start. Tests exercise Root/Second/Root on the same session and verify
the same graph and results as direct existing productive execution.

The native Current result is retained alongside its canonical projection. Existing
uncertainties remain visible and do not become Historical's completeness failure:
Succeeded means the execution completed, not proof of a complete exception set.
Unexpected execution exceptions produce CurrentExecutionFailure with no result.
Cancellation is checked before the existing synchronous analysis only; there is
no new mid-call preemption or claim that the sequential session is thread-safe.
The existing ordinary ToolRunner/Detector route is untouched.

## Historical route and unchanged isolation

Historical exclusively awaits the configured **unchanged**
`HistoricalWorkerClient.AnalyzeAsync(input, cancellationToken)`.
The full original HistoricalWorkerCallResult is retained: identity, input provenance,
PID, diagnostics and the very same B4 failure object. The common canonical result
is exposed only when B4 Success is true. No wrapper replaces worker failure codes,
no failed analysis becomes an empty success, and no fallback executes Current.

The dependency direction is Main router -> existing Current session / Main B4 client
-> neutral protocol -> isolated Worker -> Historical library. There is no reverse
dependency and no Main runtime reference to Worker/Historical assemblies. The router
holds only the optional client; it contains no process launch, JSON implementation,
compiler reconstruction, dynamic load, version detection or analysis copy.
Worker protocol 2, exact identity/provenance/import/completeness checks, resource
limits and process cleanup remain wholly owned by B4. All 13 original physical
Worker/Historical project/host/contract/config files are unchanged.

## Fresh routed execution proof

One router explicitly executes both engines for identical C#12 source, nullable
enable, HistoricalWorkerInput assembly, worker-input.cs path and the existing seven
net8 framework references. Root -> Thrower -> throw null returns the same complete
canonical result in both routes: one Proven NullReferenceException and two call/throw
path steps. The Historical result is the original B4 result object. Its real PID
differs from the caller and has stopped when the result is returned.

Current Common/CSharp informational version remains
`5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09`.
Worker Common/CSharp is actually
`5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0`.
Current before/after MVIDs are unchanged; no Historical assembly or Roslyn MVID is
loaded in the caller. Actual runtime hashes match the permanent build references.
Complete actual identity/MVID/SHA256 and provenance are in the companion JSON.
Equal public AssemblyVersion 5.0.0.0 is not used to infer selection or isolation.

Fresh route tests additionally cover real compilation and unavailable RuntimeAwait
failures, unsupported profile, process start failure, cancellation, invalid/ambiguous
inputs, unknown/default selection, Current execution failure, unchanged uncertainties
and dependency/source guards. B4's adversarial lifecycle/import regressions remain
in the expanded gate and full suite. Controlled parity is not a claim of equivalence
for arbitrary external inputs or deliberate compiler differences.

## Verification

The only permanent build-gate change adds ExceptionFlowAnalysisRouterTests to the
existing Main-boundary filter; existing CI already invokes this same gate. Local
test builds disable ProjectReference builds to preserve protected Core outputs.
Owned Current/Historical/Worker outputs are rebuilt by the existing gate only.
No remote CI execution or push is claimed.

| Fresh validation | Result |
| --- | --- |
| New routing tests | 26/26 |
| Expanded permanent Main-boundary gate | 105/105 |
| Worker/architecture/RuntimeAwait/dependency regressions | 169/169 |
| Broad semantic/external/transitive/Worker regressions | 1913/1913 |
| Complete Current suite | 2705/2705 |
| Forced Current/Historical WAE builds | three each, 0 warnings / 0 errors; DLL/PDB equality |
| Forced Worker WAE builds | two, 0/0; DLL/PDB and dependency image equality |
| Initial standalone Current and test WAE builds | 0/0 |
| Routed runtime isolation/parity/fail-closed and original lifecycle tests | PASS |
| Folder-only whitespace format, strict UTF8/CRLF, PS/JSON parsing, diff check | PASS |

No tests are skipped in the recorded suites. The normal full-suite runsettings
exclude the separate Self Analysis integration test, as in B4.
The first evidence verification found one whitespace-only initializer line in the
new routing test. The formatter corrected it; the final permanent build gate and
regression series were rerun. No production semantics or existing source changed.

**Self Analysis 16** (DOC610=0, DOC611=1, DOC631=15, DOC632=0) and **Canonical Finding
Diff 0 added / 0 removed / 0 evidence changes** remain the explicitly inherited
B4/B3/B1A2 baseline, not fresh B5 executions or fresh finding/evidence equality.
All **365 pre-existing Main sources**, including the B4 client/validator/outcome,
and **121 shared Analyzer sources** are freshly verified byte-identical. The new
Main routing documentation itself was not self-analyzed. No Analyzer semantics,
RuntimeAwait capability or semantic graph/cache/guard owner was changed.

## Evidence and reproduction

Permanent audit: `Evaluation/P5O2B5-explicit-analyzer-routing-audit.json`.
Verifier: `Evaluation/P5O2B5Proof/Write-Verify-RoutingEvidence.ps1`.
Fresh local artifacts: `artifacts/p5o2b5/` (snapshot, logs, TRX, actual routed runtime
proof, generated audit) and `artifacts/dual-version-build/` (permanent gate evidence).
The verifier checks actual TRX/build logs/runtime hashes and protected state, rather
than manufacturing pass flags from the design. The adopted permanent JSON is also
checked for strict UTF8, CRLF/final newline and JSON parsing after generation.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~ExceptionFlowAnalysisRouterTests' --logger 'trx;LogFileName=router-final.trx' --results-directory artifacts/p5o2b5/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~HistoricalWorker|FullyQualifiedName~HistoricalAnalyzerBuildProjectTests|FullyQualifiedName~ExceptionFlowAnalysisRouterTests|FullyQualifiedName~ExceptionFlowSemanticDependencyGuardTests|FullyQualifiedName~RuntimeAwait|FullyQualifiedName~Dependency' --logger 'trx;LogFileName=worker-architecture.trx' --results-directory artifacts/p5o2b5/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~Check.Semantic|FullyQualifiedName~Execution.Semantic|FullyQualifiedName~Evaluation|FullyQualifiedName~Worker' --logger 'trx;LogFileName=broad.trx' --results-directory artifacts/p5o2b5/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --logger 'trx;LogFileName=full.trx' --results-directory artifacts/p5o2b5/tests
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B5Proof/Write-Verify-RoutingEvidence.ps1
```

## Changed files, remaining limits and workspace audit

Two new productive files: ExceptionFlowAnalysisRouting.cs (choice/local input/outcome)
and ExceptionFlowAnalysisRouter.cs (Main route owner). One new 26-case routing test
file, the single existing build filter, this report, its JSON/verifier, and the
open-boundary register comprise the B5 changes. Existing Main/Tests project files,
normal solution, shared manifest, Analyzer and Worker sources remain untouched.

BND-P6-002 and BND-P5O2B-003 now record explicit selection/routing as complete,
not full historical external pipeline readiness. BND-P5O2B-002's next-step text is
aligned. Remaining: automatic policy/version/project/metadata selection;
CLI/Detector/full reporting adoption; validated external document/reference/option/
supporting-source projection and original PDB/PE equivalence. No Dapper/S1 progress
or external pipeline-stage advancement is claimed. B5 stops here, with no minimal
blocker for the authorized explicit integration.

HEAD did not move and no new foreign changes were observed. The pre-existing root
.gitignore WIP was preserved byte-for-byte. All 22 Core files (including bin/obj)
and their complete file set, six stash hashes/messages, Main project, solution and
shared manifest match the starting snapshot. No commit, push, stash or index mutation,
reset, checkout or restore occurred. Final status:

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B5-explicit-analyzer-routing-audit.json
?? Evaluation/P5O2B5-explicit-analyzer-routing.md
?? Evaluation/P5O2B5Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/
```
