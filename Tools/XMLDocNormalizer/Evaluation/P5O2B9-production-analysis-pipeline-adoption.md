# P5O2B9 - Production Analysis Pipeline Adoption

Starting HEAD: `0e5342de107ef585afd89f1186e5053b3ec690c5` (fix/documentation).
Initial status: only protected root `.gitignore` WIP, empty index, six stashes.
The protection snapshot is `artifacts/p5o2b9/protected-before.json`.

## Investigation recorded before production implementation

The real reporting entry is `XmlDocExceptionSemanticDetector.FindExceptionSmells`.
Both ordinary ToolRunner documents and prepared comparison-mode documents call it.
It computes direct throws with `ExceptionFlowLocalSourceAnalyzer`, then calls the
private `AnalyzeConfiguredExceptionFlow` execution seam. Direct mode returns the
already computed direct result. Declared-exceptions mode does the same when the
reporting scope declares no exception types. Otherwise the seam directly calls
the caller-owned `ExceptionFlowSummaryAnalysisSession.Analyze(member)`.

This is the smallest central adoption point: one detector seam, shared by both
ToolRunner reporting paths, rather than migrating each document/mode/CLI caller.
Current CSharpCompilation is available from the document's semantic model;
the root member, semantic environment and existing summary session are present.
ToolRunner creates one sequential session per analysis run/mode and reuses it
across documents; the detector's existing convenience/fallback session ownership
must also remain unchanged. Analysis mode is not compiler-engine selection.

No original external PE/PDB provenance or prepared Worker input reaches this
detector call today. ExternalSupportingSourceCompilation is a supporting semantic
dependency, explicitly not a reporting target; reconstruction retains existing
exact compiler/configuration checks. Analyzer-internal graph construction and
direct-throw helpers are not independent pipeline adoption points.

The selected seam will forward existing B8 typed requests through
`AnalyzeConfiguredExceptionFlowAsync` to Dispatch, and only the existing Current
mode adapter will consume the original native Current result for findings.
Native requests retain the exact compilation/member/session; prepared external
requests retain PortablePdb + CurrentSession/Worker payloads. No raw validation,
version comparison, Worker invocation, engine selection or fallback is added.
The synchronous detector will wait only on its native Current request, which the
existing router executes synchronously. Historical callers use the async typed
seam and receive the original staged canonical/Worker result, not guessed Current
symbols or findings. Any failed dispatch aborts native reporting, never returns
an empty successful result. No automatic external acquisition/adoption is claimed.

Before: ToolRunner -> detector mode guards -> existing SummarySession.Analyze.
After: ToolRunner -> same detector guards -> typed seam -> B8 -> B7 -> B6 -> B5
-> same existing Current session; prepared Historical input -> same typed seam
-> B8/B7/B6/B5 -> original B4 isolated Worker.

## Validation and closure

Implemented exactly one real execution seam in the existing detector. No separate
pipeline facade, engine policy, worker host or provenance implementation was added.
`CurrentCompilation(compilation, new(member, session))` reaches the async typed
entry; the existing synchronous reporting adapter consumes only the successful
original `Routing.CurrentAnalysis`, without converting canonical data back to
Current symbols. Native Current completes synchronously in B5, so the adapter's
wait does not start or synchronously wait on a Historical process. The prepared
PDB entry stays asynchronous and forwards the cancellation token unchanged.

The original Direct and declared-empty branches still return before session
execution. ToolRunner still owns one session per run/mode; detector convenience
overloads and the original absent-session fallback create it at the same points.
The shared stateless dispatcher owns no summary cache or semantic environment.
Current uncertainty remains a successful native result, not a Worker-style
completeness requirement. Failed native dispatch now throws a contextual
InvalidOperationException and aborts reporting; it never substitutes direct flow,
empty findings, another engine or a new session for failed execution.

Prepared Historical input is supported at this same production execution seam,
not yet discovered or constructed by normal CLI calls. The real integration uses
the existing B7 Historical-runtime-emitted PE/PDB and its known fixed source.
Existing P3/PE-debug-directory/PDB identity-and-checksum factories bind the original
target; B7 preserves the actual factory receipt, B6 selects, B5 invokes B4, and
only the validated canonical result/identity crosses back. No Historical Roslyn
is loaded into Main and the started Worker has exited when the call completes.
This controlled positive proof is not proof of arbitrary external input equivalence.

Projection errors expose no decision/routing/result. Unknown validated compiler
identities expose the original selection failure and no execution. Contradictory
payloads retain B5 InvalidInput. Startup and genuine structured Worker failures
retain the original B4 failure without Current results or fallback. Pre-cancellation
retains the selected branch's existing error. Null-router sentinels prove that
early projection/selection failures cannot reach execution. Independent B5-B8
tests remain unchanged; the permanent gate additionally includes B9's two classes.

Fresh final results (overlapping suites, not additive unique-test counts):

| Validation | Passed |
| --- | --- |
| Unmodified parent detector baseline | 8/8 |
| New B9 production integration cases | 23/23 |
| Independent B5-B8 cases | 133/133 |
| Productive ToolRunner/closure/supporting-source/DOC610-632 regressions | 446/446 |
| Worker/RuntimeAwait/dependency/Historical-build/canonical focused regressions | 320/320 |
| Broad Check/Execution/Evaluation/Worker regressions | 2746/2746 |
| Complete configured suite | 2837/2837 |
| Permanent Main boundary gate | 235/235 |

All final suites have zero failed or skipped cases. No runsettings were changed.
The default full suite excludes the separate SelfAnalysis category as before.

The dual-version gate passes all eleven WAE build steps with zero warnings/errors:
six forced Current/Historical library builds, two Worker builds, the existing
adversarial process, Historical PDB fixture and test project. Test project references
are built normally. The gate also proves deterministic repeated images, exact
runtime identities, package/compiler separation and genuine isolated execution.
The 121 shared Analyzer sources and single Historical semantic host are unchanged.
Folder-only format, strict UTF8/CRLF, PowerShell parsing and whitespace checks pass.

All eight complete before/after finding arrays (including locations, messages,
contexts and evidence), graph counts and sequential repetition are equal. The
baseline was actually executed with the unmodified HEAD detector in an isolated
Git archive plus only the new parity test; then only B9-owned files were overlaid
and built. No workspace Core output was rebuilt. Evidence and TRX/logs live in
`artifacts/b9-8c09031a`; `artifacts/p5o2b9/export.json` records the exact location.
The export initially contained no artifacts or bin/obj. Validation is local Windows;
no Linux or remote run is claimed.

Retained initial attempts: the new parity fixture first expected DOC610 for
`throw null` (0/8), then expected DOC632 in declared-type-only mode outside that
scope (6/8). Correcting those test assumptions to the unchanged parent contract
gave 8/8 before any production overlay. The first B9 gate stopped at test compilation
because the new test used `StartupFailure` instead of the existing `StartFailure`
enum; final test and dual-gate builds pass. No production workaround or suppression
was applied for any of these test-only mistakes.

The first evidence verification compared gate-recorded Git-export LF source hashes
with Windows worktree CRLF files and rejected the mismatch. The verifier now checks
the actual recorded export bytes, the original worktree's pre-edit byte snapshots,
and newline-normalized source equality independently. No source was rewritten to
make those fingerprints match; the rejected verifier attempt is retained.

Self Analysis 16 and Canonical Diff 0/0/0 are explicitly inherited B8 baseline
values, not new execution evidence or a guarantee about B9's changed source and
documentation. No fresh self-analysis or full canonical finding diff was run.
The fresh eight-case fixture finding parity is a separate, narrower measurement.

## Remaining boundaries

The direct-throw local helper remains deliberate Direct/DOC610 machinery; Analyzer
graph operations remain internal to the engine. B5 is now the only pipeline caller
outside the Analyzer of SummarySession.Analyze. The unchanged Analyzer-internal
AnalyzeSolutionTransitivelyThrownExceptions convenience entry still creates and
calls a session directly (no current reporting call-site found). Ordinary and comparison ToolRunner paths reach
the adopted detector seam, but neither constructs external Worker payloads.
External reconstruction and supporting-source registration are unchanged; a
supporting dependency is not a reporting target. Dapper acquisition/reconstruction,
CLI external-input projection and Historical canonical-to-findings/reporting
import are still not migrated. Whole external source documents, references,
options, generator output, supporting compilations and canonical selectors must
be shown equivalent before extending the bounded single-source Worker profile.
Compiler provenance alone is insufficient. No external pipeline advancement or
repository-wide adoption is claimed.

## Changed files and repository protection

- `src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs`: one seam adoption.
- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs`:
  eight real detector mode/session baseline cases.
- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionPipelineTests.cs`:
  fifteen typed production-seam runtime/failure/isolation cases.
- `build/Verify-DualVersionBuild.ps1`: add both B9 classes to the existing gate only.
- `Evaluation/OPEN-PIPELINE-BOUNDARIES.md`: precisely bounded first adoption/open work.
- This report, `Evaluation/P5O2B9-production-analysis-pipeline-adoption-audit.json`,
  and `Evaluation/P5O2B9Proof/Write-Verify-ProductionEvidence.ps1`.

The proof script checks root ignore bytes, all 23 Core files (including outputs),
371 other Main source files, existing seven Worker test files, project/build
configuration hashes, six stashes and the untouched index against the pre-edit
snapshot. HEAD remains `0e5342de107ef585afd89f1186e5053b3ec690c5`; no external HEAD
movement, commit, push, stash change or staging was performed in B9.

The complete dual gate was run inside the isolated tool root with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
```

All regression sweeps used `dotnet test ... --no-build --no-restore`, explicit
fully-qualified-name filters for the named owner groups, and separate named TRX
files under the export's `artifacts/p5o2b9/tests`; the full sweep had no added
filter. The final reproducible evidence/protection check runs from this worktree:

```powershell
$export = Get-Content artifacts/p5o2b9/export.json -Raw | ConvertFrom-Json
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B9Proof/Write-Verify-ProductionEvidence.ps1 -ValidationRoot $export.ToolRoot
```

Final `git status --short` (the root ignore line is pre-existing protected WIP):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs
?? Evaluation/P5O2B9-production-analysis-pipeline-adoption-audit.json
?? Evaluation/P5O2B9-production-analysis-pipeline-adoption.md
?? Evaluation/P5O2B9Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionPipelineTests.cs
```
