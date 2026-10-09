# P5O2B6 - Deterministic Current/Historical Analyzer Selection

Date: 2026-10-09. Starting HEAD: `6fc6f19a4f4b732326bddafd210dc2623fe35067`
(`Add explicit current historical analysis routing`). B5 separately committed.

## Investigation and rule recorded before implementation

The existing Main holds reliable target-bound Portable PDB provenance in
ExternalCompilationProvenanceDescriptor. Its factory validates the PDB against
the PE debug descriptor, reads unique compilation-options CDI from the same
immutable image, and rejects duplicate or malformed options. TryGetValue retains
the exact compiler-version string. ExternalCSharpCompilationConfiguration also
retains this value, but accepting/reconstructing configuration does not prove
compiler compatibility or exact external-input equivalence.

B4 validates the actual Historical Common/CSharp native identity including full
informational version, MVID and SHA256 at execution. B5/gate runtime evidence
proves the exact Current identities. Both public assembly versions are 5.0.0.0;
package names, framework versions, language versions and ordering of version
strings are insufficient selection evidence. No existing unified Main abstraction
binds every local/external analysis to a validated required compiler. Therefore
B6 introduces a small explicit, Main-local selection context; no discovery or
production call-site migration is inferred.

The rule below was documented before editing production code:

| Trusted context provenance | Exact required compiler informational version | Decision |
| --- | --- | --- |
| CurrentCompilation or ValidatedPortablePdb | `5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09` | Current |
| ValidatedPortablePdb | `5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0` | Historical |
| CurrentCompilation | exact Historical build above | conflicting evidence, failure |
| missing/default/unknown provenance or compiler evidence | any | failure |
| recognized provenance | any other compiler string | unsupported compiler, failure |

Comparison is exact ordinal equality: no trimming, prefix matching, ranges,
fallback, semantic-version comparison or acceptance by AssemblyVersion. Current
means the proven Current build, not the nearest/available compiler. Historical
means only the one proven B4 build. A compiler-version declaration selects the
required execution engine; it does not authenticate an image or waive B4's runtime
identity/input/result gates, reconstruction equivalence or completeness checks.

Context provenance is a typed **trusted Main input precondition**, not a claim that
an enum authenticates arbitrary CLI strings. CurrentCompilation evidence must come
from the Current-owned compilation context; ValidatedPortablePdb evidence must come
from the existing target-bound validated descriptor. B6 supplies a narrow descriptor
projection for the latter, preserving exact bytes of the version string. Unvalidated
external metadata must never be relabelled as validated. Caller/CLI/Dapper/reporting
projection/adoption remains separately authorized future work.
The PDB context adapter requires existing compilation-options schema `2` and exact
language `C#`; absent options/other schemas/languages produce an uninitialized
context, not a guessed C# compiler. Missing compiler-version then fails separately.

Selection is a pure decision returning the existing B5 enum or a typed failure.
The B5 router alone executes a successful decision. The policy must not start a
Worker, load Roslyn, create/manage sessions, analyze code or report findings.
No Analyzer, router, protocol, client, historical source or existing B5 test changes
are planned.

## Starting protected state

The mandatory initial status/stat/full diff/HEAD/log audit found the protected root
.gitignore WIP plus two **pre-existing** Core obj modifications. The B5 commit
contains only its eight owned files, not these Core edits. Compared with the B5
protected snapshot, exactly Core.AssemblyInfo.cs and AssemblyInfoInputs.cache
changed; the visible generated informational version now records the committed B5
HEAD. This was stopped for read-only investigation before any build/edit. No repair
or reset is attempted. The current 22-file Core state is snapshotted as the B6
authoritative protected baseline, with all hashes rechecked after validation.

## Implementation and composition

Owner: `XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalyzerSelectionPolicy`,
entry `Select(ExceptionFlowAnalyzerSelectionContext)`. The immutable context holds
ExceptionFlowCompilerProvenance (Unspecified, CurrentCompilation, ValidatedPortablePdb)
and the exact required compiler-version string. Its narrow
`FromValidatedPortablePdb(ExternalCompilationProvenanceDescriptor)` projection reads
the already validated options only, without artifact acquisition, compilation or
new provenance validation. Local Current evidence remains supplied by its existing
trusted compilation owner. The context is Main-local, not a new Worker DTO.

Select returns ExceptionFlowAnalyzerSelectionDecision. Success contains only the
existing B5 Current/Historical enum; failure always contains Unspecified and one
typed code: MissingContext, UnsupportedProvenance, MissingCompilerVersion,
UnsupportedCompilerVersion or ConflictingEvidence. Null/default, unknown enum,
empty compiler evidence, partial/malformed/unsupported versions, whitespace/case
variants and neighboring builds never silently choose Current.

The policy is static and stateless, with only two constant strings. It has no
Worker client, router/session field, Roslyn API, reflection/runtime inspection,
filesystem read, process launch, async execution or reporting. Repeated parallel
selection yields identical decisions. It does not replace B4's stricter actual
runtime/image/provenance checks with a string match or reinterpret an uncertain
Current result as Historical completeness failure.

Dependency direction: trusted context -> pure decision -> existing enum ->
**unchanged ExceptionFlowAnalysisRouter.AnalyzeAsync** -> existing Current session
or unchanged HistoricalWorkerClient. Tests compose a successful decision directly
into the existing request, not a second production execution facade. The selection
failure's Unspecified remains non-executable even if mistakenly sent to the B5
router alongside a valid Current input. No existing productive call site is migrated.
All B5 router/request/outcome/client/validation files and its 26-test file are
byte-identical to B6 start; neither execution path nor IPC contract is duplicated.

## Real evidence and tests

40 new cases cover the three accepted context/build pairs, 16 non-matching version
strings (both adjacent exact builds, missing commits, changed commits, prefixes,
suffixes, AssemblyVersion, casing and whitespace), four unknown/default provenance
values, null/default context, three missing-version values, contradictory provenance,
pure parallel decisions/no execution dependency, genuine target-validated PDB
projection, seven incomplete/unsupported descriptor projections, selected Current,
selected Historical and non-executable failure at the unchanged router.

The real PDB test reuses the existing in-memory Roslyn PE/PDB emitter and productive
PE/PDB provenance factories. Its CDI contains exactly the documented full Current
compiler-version; projection preserves it unchanged and selects Current. Negative
projection variants are explicitly synthetic unit fixtures, not claimed to be newly
checksum-validated modified PDBs. The trusted Historical requirement is supplied
explicitly to the controlled fixture; no external historical PDB/assembly discovery
is claimed. Actual Worker historical runtime identity is validated independently
through B4 as always.

Selected Current uses an intentionally unstartable configured Worker endpoint and
still matches direct productive Current Session execution. Selected Historical
uses the real B5 router and real B4 Worker: the same bounded source/options/reference
profile matches Current's complete canonical result (one Proven NullReferenceException
with call/throw path). The actual Worker PID differs from the caller and is stopped
on return. The original B4 result object, identity and input provenance are retained.
Current before/after MVIDs match; no Historical library/Worker/Roslyn MVID is loaded
in Main. Runtime informational versions match the exact rule above and image SHA256
values match the permanent gate's compile references. The JSON records actual loaded
Common/CSharp and Worker/Analyzer MVIDs, hashes, provenance and canonical output.

Controlled parity proves the authorized bounded case, not parity for different
compiler behavior or full external input equivalence. Historical RuntimeAwait still
fails closed under unchanged B4 regressions; no missing capability is manufactured.

## Fresh verification and inherited semantic baseline

The only existing build-script change adds the SelectionPolicyTests class to the
existing B5 Main-boundary filter. Existing CI already invokes this same expanded
gate; no second build owner or new CI workflow is introduced. Local tests build with
BuildProjectReferences=false and do not rebuild Core. No remote CI run is claimed.

| Fresh gate | Result |
| --- | --- |
| New Selection tests | 40/40 |
| Initial Selection + unchanged B5 Routing | 66/66 |
| Expanded permanent Main-boundary gate | 145/145 |
| Worker/architecture/RuntimeAwait/dependency regressions | 209/209 |
| Broad semantic/external/transitive/Worker regressions | 1953/1953 |
| Complete Current suite, unchanged final retry | 2745/2745 |
| Known MultiModule flake, isolated retry | 1/1 |
| Forced Current/Historical WAE builds | three each, 0 warnings / 0 errors, equal DLL/PDB |
| Forced Worker WAE builds | two, 0/0, equal DLL/PDB and dependency images |
| Initial Current and test WAE builds | 0/0 |
| Actual selected execution/parity/isolation and original fail-closed lifecycle | PASS |
| Folder-only format, strict UTF8/CRLF, PS/JSON parsing, git diff check | PASS |

No failures or skipped tests in the final recorded suites. The first full attempt
completed 2744 passed / 1 failed: only the pre-existing
ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed flake,
already documented in A5A/A5B. Its isolated retry passed and the unchanged complete
retry passed 2745/2745; both the initial failure TRX and passing retries are retained.
No unrelated test/production fix was made. PS5 raised NativeCommandError on native
failure stderr; final retry capture checks the actual exit code after preserving
output. The initial verifier also had a PS5 expression-continuation parse error;
one-line correction fixed the evidence script only, with no production change.
The normal full-suite
runsettings exclude the separate Self Analysis category, unchanged from B5.
All 367 pre-existing Main sources, 121 shared Analyzer sources and 13 physical
Historical/Worker configuration/host/protocol files remain byte-identical. Current
Analyzer algorithms, semantic environment, RuntimeAwait information, graph/cache/
guard ownership and session lifetimes are unchanged.

**Self Analysis 16** (DOC610=0, DOC611=1, DOC631=15, DOC632=0) and **Canonical Finding
Diff 0 added / 0 removed / 0 evidence changes** are explicitly inherited from the
documented B5/B4/B3/B1A2 baseline, not fresh B6 executions or finding/evidence equality.
The new selection documentation was not self-analyzed. Fresh source fingerprints
support unchanged Analyzer semantics, not a claim that the new host files were
included in a fresh self-analysis run.

## Evidence, reproduction and changed files

Report: `Evaluation/P5O2B6-deterministic-analyzer-selection.md`.
Permanent evidence: `Evaluation/P5O2B6-deterministic-analyzer-selection-audit.json`.
Verifier: `Evaluation/P5O2B6Proof/Write-Verify-SelectionEvidence.ps1` (adapted from B5's
unchanged evidence verifier). Fresh snapshot/logs/TRX/runtime proof/generated audit:
`artifacts/p5o2b6/`; permanent build/runtime gate: `artifacts/dual-version-build/`.
Verifier consumes actual tests, logs, runtime hashes and protected snapshots. The
adopted permanent JSON is separately checked for strict UTF8/CRLF, JSON parsing,
equality with fresh generated evidence and complete final status.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~ExceptionFlowAnalyzerSelectionPolicyTests' --logger 'trx;LogFileName=selection-final.trx' --results-directory artifacts/p5o2b6/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~HistoricalWorker|FullyQualifiedName~HistoricalAnalyzerBuildProjectTests|FullyQualifiedName~ExceptionFlowAnalysisRouterTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionPolicyTests|FullyQualifiedName~ExceptionFlowSemanticDependencyGuardTests|FullyQualifiedName~RuntimeAwait|FullyQualifiedName~Dependency' --logger 'trx;LogFileName=worker-architecture.trx' --results-directory artifacts/p5o2b6/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~Check.Semantic|FullyQualifiedName~Execution.Semantic|FullyQualifiedName~Evaluation|FullyQualifiedName~Worker' --logger 'trx;LogFileName=broad.trx' --results-directory artifacts/p5o2b6/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~MultiModuleAssembly_FailsClosed' --logger 'trx;LogFileName=multimodule-isolated.trx' --results-directory artifacts/p5o2b6/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --logger 'trx;LogFileName=full-final.trx' --results-directory artifacts/p5o2b6/tests
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B6Proof/Write-Verify-SelectionEvidence.ps1
```

B6-owned changes are exactly:

- src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionContext.cs
- src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionPolicy.cs
- Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs
- build/Verify-DualVersionBuild.ps1 (one filter extension)
- Evaluation/OPEN-PIPELINE-BOUNDARIES.md
- Evaluation/P5O2B6-deterministic-analyzer-selection.md
- Evaluation/P5O2B6-deterministic-analyzer-selection-audit.json
- Evaluation/P5O2B6Proof/Write-Verify-SelectionEvidence.ps1

## Remaining boundary and final audit

BND-P6-002 and BND-P5O2B-003 now record the deterministic decision sub-boundary as
complete, not production-wide automatic routing. BND-P5O2B-002's next-step text is
aligned. Still open: trusted context delivery/adoption by CLI/Dapper/reporting,
automatic external Assembly/Project discovery, full validated original documents/
references/options/supporting-source projection, original PDB/PE equivalence and
full reporting import. The fixed bounded Worker profile is not substituted for
external reconstruction. No Dapper/S1 external pipeline-stage advancement, extra
Historical build/version, plugin or dynamic Main assembly loading was added.
There is no blocker for the authorized deterministic decision layer. B6 stops here.

Starting/final HEAD is unchanged; the B5 commit predates B6. No external HEAD motion
or new foreign change was observed during B6. The initial root .gitignore WIP and
two pre-existing Core obj modifications are preserved byte-for-byte, not described
as newly introduced B6 changes. All 22 Core files/file set and six stash hashes/messages
match B6 start. Main/Tests project configuration, normal solution and shared manifest
are untouched. No commit, push, index/stash mutation, reset, checkout or restore
occurred. Only the permanent gate rebuilt its own disposable output trees.

Final git status --short:

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfo.cs
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfoInputs.cache
?? Evaluation/P5O2B6-deterministic-analyzer-selection-audit.json
?? Evaluation/P5O2B6-deterministic-analyzer-selection.md
?? Evaluation/P5O2B6Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionContext.cs
?? src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionPolicy.cs
```
