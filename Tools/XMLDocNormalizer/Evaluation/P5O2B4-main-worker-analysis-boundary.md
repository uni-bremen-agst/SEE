# P5O2B4 - Main/Worker Integration and Final Roslyn-Free Analysis Boundary

Date: 2026-10-08. Starting HEAD:
`fefd95c6f106a11af6b59bc83ba8140d8eb51630` (`Add isolated historical analyzer worker.`).
Initial audit: only protected ` M ../../.gitignore`; B3 separately committed.
Final HEAD: `3962615c84b14ca7ac9d2ed1f1afe099abb61c14`
(`Fix case-sensitive DTO source path.`), externally committed during B4, not by
this agent. Its sole delta corrects Models/DTO to the tracked Models/Dto spelling
in the permanent shared source manifest (Include and Link), with unchanged source
bytes. It is preserved, explicitly audited and included in the final fresh builds.

**P5O2B4 ist abgeschlossen: der Main Process kann eine echte Historical-Analyse
über eine validierte Roslyn-freie Worker-Grenze ausführen.**

This closes the explicitly callable bounded Main analysis boundary, not automatic
selection, CLI/reporting integration or exact external compilation reconstruction.
No Dapper/S1 external pipeline-stage advancement is claimed. The inherited Self
Analysis/canonical baseline is explicitly distinguished from fresh execution gates.

## Existing evidence and ownership

Read B3 report and complete JSON audit, relevant B2/A2 reports and the open boundary
register before implementation. Inspected the actual protocol, Worker entry/host,
runtime identities, canonical constructors and Main single-compilation semantic host.
Captured fresh protected file/stash/source fingerprints before builds or edits.
The starting Git status/diff/stat/HEAD/log audit is preserved as the starting state.

Reuse the permanent B2 projects, genuine B3 Worker and build/CI gate. There is no
second IPC service, Analyzer copy, process framework, runtime library reference or
new canonical findings model. All original **362 Main C# files and 121 shared
Historical Analyzer sources remain byte-identical**. Three new Main boundary files
are outside the shared Analyzer manifest; the real Historical semantic host and
RuntimeAwait capability/caches are unchanged.

Main owner: `XMLDocNormalizer.Execution.Historical.HistoricalWorkerClient`, under
`src/XMLDocNormalizer/Execution/Historical/`. It is explicitly callable internally
via `AnalyzeAsync(WorkerAnalysisInput, CancellationToken)` and takes trusted local
dotnet/Worker endpoint paths plus a bounded deadline. It launches a process, not an
Analyzer. `HistoricalWorkerResponseValidation` owns the strict import gate;
`HistoricalWorkerCallResult` owns the local success/failure/diagnostic outcome.

Main compiles the **same physical neutral WorkerProtocol.cs** as Worker, resolved
against its existing source-local canonical domain. This is a Compile link only:
zero Main ProjectReferences, zero historical package/runtime assembly references.
Tests no longer compile a separate contract copy; they consume the productive Main
contract through the existing friend-assembly boundary. Worker retains its sole
Historical-library ProjectReference. Neither algorithm/library depends back on Main.
No new inter-component cycle or lower-component/Analyzer dependency was introduced.

## Final versioned input contract

Final protocol version **2**, Worker application identity **1.0**. The required
successful-response input provenance makes this an incompatible protocol change
from B3 version 1; old v1 and unknown versions are structurally rejected. There is
no silent best-effort import or compatibility matrix. Identity and analyze remain
the two existing operations, with one strict UTF-8 JSON object until stdin EOF.

`WorkerRequest`: protocolVersion, operation, payload. `WorkerAnalysisInput`: source,
typeMetadataName, methodName, optional typed context. A null context selects the
same fixed bounded profile, not an unspecified external configuration. The Main
E2E requests send the context explicitly. Shared WorkerInputValidation is compiled
in both owners, so Main/Worker cannot silently disagree about supported inputs.

Supported context: solution-transitive; net8-runtime-bounded-v1; C#12; nullable
enable; assembly HistoricalWorkerInput. Exactly one source tree with virtual path
worker-input.cs, 32,768 source characters/8,192 syntax nodes, seven existing
framework-only references from the runtime TPA profile, and one ordinary static,
parameterless, non-generic source method on a non-generic source-owned type.
Worker verifies compilation diagnostics and exact root declaration/model ownership
before calling the unchanged productive SummarySession and canonical adapter.
No submitted source is emitted or executed.

The request frame has 65,536 characters, including escaped JSON. Main checks the
serialized frame too: a source within its character limit can still exceed the
transport budget. No caller-local reference/source paths or Roslyn objects are sent.

### Existing supporting/external input categories

The known Main reconstruction boundary contains source/PDB document ordinals,
explicit acquisition/provenance, ordered compiler-option entries, validated metadata
reference identities/images and source-backed dependency scopes. Do not confuse
those validated external inputs with the chosen runtime smoke reference profile.

The explicit typed context can represent, but **currently rejects**, these inputs:

- Additional WorkerSourceDocument records: logical path/text, SHA256, optional
  original bytes/checksum algorithm (original encoding/PDB checksums are not lost
  to a requirement to serialize syntax).
- WorkerReferenceImage: logical name, metadata image bytes, SHA256; no file handles
  or symbols. Image bytes serialize as JSON base64, not executable command fragments.
- WorkerSupportingCompilation: ID, sources, reference images, options, dependency
  IDs and provenance identity. Multiple projects/dependency relationships can be
  represented without transporting Workspace/Compilation objects.
- CanonicalCallableIdentity root selection, exact ordered WorkerCompilerOption
  key/value provenance, preprocessor symbols and non-default assembly names.

These are concrete neutral values, not reflection-based object bags. Even empty
reserved arrays are rejected, not ignored. The round-trip test proves transport
representability; it does NOT claim semantic support or external provenance validation.
Existing compiler options use the same explicit ordered key/value form in Main.
Future validated projection can extend these DTOs under deliberate versioning;
no exhaustive MSBuild/PDB/emit schema or supporting-source redesign was built here.
Artifact acquisition, checksum/reference/PE equivalence and import policy remain
Main-owned prerequisites, not instructions for a Worker to read arbitrary files.

## Canonical output and input binding

Result remains the **existing CanonicalExceptionFlowAnalysisResult**. Its exact
CanonicalTypeIdentity, evidence kind, ordered ExceptionFlowPathStep data, paths,
truncation and uncertainties round-trip into the Main's existing domain owner.
No parallel Findings model or Roslyn-symbol deserialization is introduced.

Successful WorkerResponse additionally contains WorkerAnalysisProvenance: UTF-8
source SHA256, exact type/method selector, logical document, mode, reference profile,
language and nullable configuration. Main computes/compares the requested binding.
This proves which bounded input was analyzed, not validation of an external PDB/PE.
Supporting/external documentation provenance is not fabricated: the profile has no
external supporting registry; unsupported/incomplete analysis fails with no result.

Main rejects duplicate/unknown fields, wrong versions/operations, invalid enum data,
missing/inconsistent envelopes and non-canonical constructor defaults. It verifies
typed deserialize/serialize structural equality, preventing missing collections
(e.g. result `{}`) from becoming a manufactured empty success. Collection order and
canonical identity normalization must survive unchanged; JSON property ordering is
not semantic. Main additionally checks required named exception/module/assembly
identity, Proven evidence, nonempty retained paths, fixed logical source locations
and valid source line/column bounds. Uncertainties and truncation reject completeness.
No uncertain or partial result is exposed through a failure outcome.

An actually completed catch/no-escaping-exception analysis IS allowed an explicit
empty canonical result only with successful exit, valid envelope, identity and
matching input provenance. This is tested against the Current canonical result.

The recursive reflection gate walks every reachable DTO/domain property, array,
nullable type and owned generic argument, including reserved supporting inputs and
the Main call outcome. It rejects Microsoft.CodeAnalysis, object bags, interfaces,
abstract payload types and opaque foreign types; Guid is the explicitly allowed
neutral scalar already used by CanonicalModuleIdentity. No session, syntax, model,
symbol, graph, cache, operation or Main semantic context crosses the boundary.

## Failure contract and process lifecycle

HistoricalWorkerClientFailureCode distinguishes UnsupportedInput, StartFailure,
ProtocolMismatch, MalformedResponse, IdentityMismatch, WorkerCrash, NonZeroExit,
StructuredFailure, IncompleteResult, TransportFailure, OutputLimit, Timeout and
Cancelled. StructuredFailure retains the original WorkerFailure code/message/details
(invalidRequest, malformedInput, compilationFailure, analysisFailure or unexpected
Worker failure). Unsupported protocol failures map to ProtocolMismatch. Failures
carry no canonical result; stderr diagnostics are separate, never the result.

ProcessStartInfo has UseShellExecute=false/CreateNoWindow=true and typed ArgumentList
with only the configured Worker DLL. Source is sent on stdin, never interpolated
into shell arguments. No temporary files, daemon, pool, persistent manager, ALC,
network or automatic routing. One request starts one fresh process and ends it.

Input writing, concurrent stdout/stderr reads, process exit AND pipe EOF are within
one linked deadline/cancellation scope. Faulted output reads are observed promptly,
including a full pipe. Stdout is limited to 1,048,576 characters; stderr to 16,384.
Strict UTF-8 rejects invalid bytes. On timeout/cancellation/transport failure, the
owned process tree is killed if still running, exit is awaited and pipe tasks are
observed/disposed. The client has no cross-request Analyzer state or cache reset.
The trusted endpoint/deadline is configured explicitly, not discovered by source
version heuristics; deadlines must be positive and at most five minutes.

The adversarial TestProcess is a tiny Evaluation-owned BCL-only pipe fixture. It
does not reference Main/Historical, copy the Worker protocol or execute input code;
it supplies malformed/version/crash/nonzero/flood/invalid-UTF8/hang scenarios to the
PRODUCTIVE client. Real E2E/parity/failure tests always use the genuine Worker.

## Actual E2E, controlled parity and identities

Current-host integration test calls the productive client twice with the same
source/options/profile. Each request gets a different real Worker PID. Historical
parse/compile/host -> genuine SummarySession -> canonical adapter -> stdout -> Main
validation yields one Proven NullReferenceException with MethodCall/ExplicitThrow
steps and source positions. Input provenance binds the source and Root selector.
Canonical results and stable identity/provenance serialization are identical.
Process IDs are local diagnostics only, not canonical results/protocol identities.

Current parity executes the genuine Current SummarySession with the existing
ProjectClosureSemanticContext single-compilation factory, identical source,
assembly name, virtual path, C#12, nullable enable and seven-reference profile.
The complete canonical results are equal, including identities/evidence/paths.
A second controlled catch fixture also matches, with no escaping exceptions.
This does not assert parity for deliberate compiler differences or unavailable
Historical RuntimeAwait information: the real historical await still returns
structured analysisFailure with no result, propagated by the productive Main client.

Actual Current Common/CSharp informational version:
`5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09`.
Actual Worker Common/CSharp informational version:
`5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0`.
Both public AssemblyVersions are 5.0.0.0; version alone is not the identity gate.
Main validates exact native informational version, MVID and SHA256 for both engines,
the Worker/Analyzer identity envelope, protocol/profile and unavailable capability.
Runtime images in fresh E2E evidence match the permanent gate's compile references.
Current before/after MVIDs match, with no Historical Analyzer/Worker/MVID loaded in
the caller. No assembly is runtime-loaded to bridge the process boundary.

Historical Common MVID `dc7738cc-6dca-4d34-9c44-29b53a7caa93`, SHA256
`660C3D626C4B8F4CF8C231FBEF0FB6B4DB4FFFCC89EF5B31AAECA1CF4D7F66A1`;
CSharp MVID `0f9c1dcf-4eb1-47f8-81b2-733db5887be7`, SHA256
`B0EC1DDCA4C97DCF15845FF4BCEB5499C3989025197D5D65E04310E29C09217D`.
Actual Worker/Analyzer and Current image hashes are retained in the JSON audit.

## Fresh verification and explicit inherited semantic baseline

The existing dual-build gate retains six forced library build pairs (both orders),
two Worker-only pairs, output/asset isolation, exact packages and DLL/PDB equality.
It now builds the adversarial fixture and optionally runs Main integration tests
via -RunMainBoundaryTests; clean CI also enables -BuildTestProjectReferences. Local
test builds use BuildProjectReferences=false, preserving protected Core outputs.
The existing CI workflow invokes that same expanded gate, not a second mechanism.
Remote CI was not executed and no push was made.

| Fresh final gate | Result |
| --- | --- |
| Current/Historical forced WAE builds | three each, 0 warnings / 0 errors; equal DLL/PDB |
| Worker forced WAE builds | two, 0/0; equal DLL/PDB; dependency-copy hashes match |
| Main integration in expanded permanent gate | 79/79, no failures/skips |
| Worker/architecture/RuntimeAwait/dependency regressions | 143/143, no failures/skips |
| Broad P5/P6/G semantic/external/transitive/Worker regressions | 1887/1887, no failures/skips |
| Full Current suite | 2679/2679, no failures/skips (2636 + 43 new cases) |
| Main runtime isolation, controlled complete parity, repeat determinism | PASS |
| Process start/crash/nonzero/malformed/version/flood/timeout/cancellation/cleanup | PASS |
| Folder-only format, strict UTF8/CRLF, XML/PS/JSON, git diff check | PASS |

Per B4 section 18, **Self Analysis 16** (DOC610=0, DOC611=1, DOC631=15, DOC632=0)
and **Canonical Finding Diff 0 added / 0 removed / 0 evidence changes** are inherited
from B1A2/B3, NOT new runs. Complete A2 raw/normalized finding/evidence equality and
its audit/hash are retained through B3. All 121 shared sources and original 362 Main
sources match fresh fingerprints; only host/boundary/integration code changed.
This is not a claim that new host documentation was freshly self-analyzed.

Local corrections and failed initial attempts are retained, not counted as passes:
nullable PathStep location wiring caused one Main compile failure; a chained initial
test invocation then ran stale binaries and failed imports. Corrected the nullable
wiring and made subsequent command stages stop on failed builds. The first expanded
gate also exposed the unchanged B1 probe Models/DTO vs active Git Models/Dto path;
the test now compares consistently with its existing case-insensitive manifest set
operations. The concurrent external commit corrected the active manifest's casing;
the agent changed neither manifest nor any Analyzer source. PS5 native stderr is captured
before checking failed exit codes so failure logs survive. A stricter closed-DTO test
initially rejected the existing Guid scalar; explicit neutral-scalar allowance fixed
the test without a production change. Final gates were rerun with the final code.

## Reproduction and evidence

From Tools/XMLDocNormalizer (pwsh on Linux/macOS):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~HistoricalWorker|FullyQualifiedName~HistoricalAnalyzerBuildProjectTests|FullyQualifiedName~ExceptionFlowSemanticDependencyGuardTests|FullyQualifiedName~RuntimeAwait|FullyQualifiedName~Dependency' --logger 'trx;LogFileName=worker-architecture.trx' --results-directory artifacts/p5o2b4/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~Check.Semantic|FullyQualifiedName~Execution.Semantic|FullyQualifiedName~Evaluation|FullyQualifiedName~Worker' --logger 'trx;LogFileName=broad.trx' --results-directory artifacts/p5o2b4/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --logger 'trx;LogFileName=full.trx' --results-directory artifacts/p5o2b4/tests
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B4Proof/Write-Verify-BoundaryEvidence.ps1
```

The verifier consumes real gate logs/TRX/runtime evidence, rechecks unchanged
source/protected state and format/encoding/parsing, and emits the ignored artifact
audit adopted into the permanent companion JSON. The permanent JSON also records
the complete final git status. It is evidence verification, not another build owner.

## Remaining boundary and next step

Minimal B4 blocker: **none**. The Main can explicitly execute/validate a genuine
bounded Historical analysis without loading Historical Roslyn. Automatic selection,
CLI/full checking/report import, exact external input reconstruction and supporting
registry integration remain deliberately open. In particular the smoke runtime
profile is never substituted for Dapper's original compiler/reference/options/PE
provenance. Existing external fail-closed boundaries remain unchanged.

Next: **explicit routing/selection integration - when Current and when Historical**,
using this productive client and validated input profiles. Do not repeat B2/B3
build/runtime-isolation feasibility or introduce a second Worker/IPC mechanism.

Protected root .gitignore, all 21 Core bin/obj files/file set, other tracked Core
files, normal solution and all six stash hashes/messages remain unchanged. The agent
performed no commit, push, index/stash mutation, reset, checkout or restore-to-HEAD.
The separate external case-only commit is recorded, not described as unchanged HEAD.
Only owned disposable build outputs were cleaned/rebuilt. The companion audit
records all protected hashes and the complete final `git status --short`.
