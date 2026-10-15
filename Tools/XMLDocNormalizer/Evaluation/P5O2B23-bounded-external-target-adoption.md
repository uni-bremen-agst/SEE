# P5O2B23 — Bounded External Target Execution & Output Adoption

## Pre-implementation decision

Audited HEAD: a3d811c68c72fbd6f911bf51e82cd95ff60970f6 (B22).
The only existing working-tree change is the protected root `.gitignore`.
No applicable AGENTS.md was found. No delegation is used.

The existing Program parses ToolOptions, invokes ToolRunner and derives exit
0/1 from RunResult. ToolRunner normally loads Current projects/solutions, then
accumulates Findings and calls the existing reporter. B22's internal external
Finding operation has no normal CLI caller. Reporters need no new schema.

Chosen vertical slice, recorded before production changes:

* `--check --external-target job.json --format console|json|sarif [--output path]`.
* The version-1 local job names target PE, matching PDB, ordered source inputs
  (PDB document ordinal plus explicit local path), ordered reference paths,
  root type metadata name/method name/source ordinal, worker assembly and
  optional dotnet host. Paths resolve relative to the job file, not PDB names.
* The job is untrusted raw input, strictly parsed; no receipt/Worker DTO input.
* Bind the exact PE as metadata in a non-executing Main metadata context, use
  the existing assembly descriptor/P4/P5A factories, then P5G, P5H/P5I/P5J and
  P5C/P5E. Retain their original objects. Do NOT require active-compiler P5K
  source recompilation as a condition for Historical execution.
* Root selection uses the actual bound target assembly/type and exactly one
  named method. Existing projection validates the supported signature and
  original module/MVID; source ordinal explicitly identifies its source.
* ToolRunner configures the existing HistoricalWorkerClient with explicit local
  worker path, dotnet host and a fixed bounded deadline. B7/B6 still exclusively
  select from the original PDB compiler provenance. Non-Historical inputs fail
  closed; the existing Current project/solution path is unchanged.
* Invoke B22 with its existing v1 Declared documentation opt-in. Only successful
  attested Findings enter RunResult.AccumulateFindings once, ReportFile once
  per Finding file, and existing CompleteReporting. No duplicate lists/importer.
* Dedicated external-execution errors produce stderr and a non-success exit,
  without reporting an empty success. No reporters run before successful B22.
* Explicitly reject rewrite, comparison, statistics and project/solution scope
  flags for external jobs. Default this input kind to Declared; explicitly
  supplied other modes reject. No change to normal default SolutionTransitive.

Protected Core files, root ignore, stash identities and index are snapshotted
before implementation. Builds/tests run in an exported validation tree to
avoid protected Core bin/obj churn. No commit, push or stash operation.

## Scope

Only B22's unsigned, complete/proven single-project, one supported static
parameterless nongeneric root, existing parse/reference/compilation profile.
No discovery/download, extra compiler, Dapper/P5G changes, new reference/XML
forms, Current fallback, other Historical modes, cross-project Declared,
general solution loading or Worker performance changes.

The admitted existing profile is an unsigned single-module implementation DLL,
AnyCPU, Debug, Nullable Enable, Checked/Unsafe false; Parse/Regular with concrete
supported language version and exact ordered defines. The target uses complete
P5J membership (1–16 sources), bounded exact assembly PE references (up to 64,
32 MiB total) and the existing per-source/request/result budgets. Other reference
forms, generic declaration/documentation types and incomplete/truncated analysis
remain rejected by their existing owners. Root is one unambiguously bound ordinary
static parameterless nongeneric nonabstract/nonextern metadata method in a
nongeneric containing type, in the explicitly selected source. No visibility or
overload guessing is added.

## User command and raw local input

```powershell
dotnet XMLDocNormalizer.dll --check --external-target job.json --format json --output findings.json
```

Use `--format console` or `--format sarif` for the other existing reporters.
Explicit `--exception-analysis-mode project-transitive-declared-exceptions`
is accepted but not required for this input kind. The normal project/solution
default remains SolutionTransitive. Other external modes and comparison,
rewrite, cleanup, project/solution selection, statistics and positional input
are rejected rather than ignored.

The job is strict camelCase JSON; see `P5O2B23Proof/job.example.json`.
Its `version: 1` describes the local job, not a new Worker protocol version.
Unknown/duplicate fields and absent required ordinals reject. Fields:

| Field | Meaning |
| --- | --- |
| targetPe / portablePdb | Explicit local PE and exact matching Portable PDB |
| sources | Complete original source-tree order; each entry names a zero-based validated PDB documentOrdinal and exact local source path |
| references | Exact original metadata-reference ordinal order, not lookup/search paths |
| root.typeMetadataName / methodName | Exact PE-bound metadata type and unique named method, not a query language |
| root.documentOrdinal | Explicit root source in sources; no filename inference |
| workerAssembly | Existing local deployable HistoricalWorker DLL with its runtime/dependency files |
| dotnetHost | Optional explicitly configured host; otherwise the normal dotnet executable |

All material/worker paths resolve relative to the job file. The optional host
is an executable configuration, not compiler selection. The example's reference
list is illustrative and must be replaced by the target's complete exact list.
No file is inferred from a PDB document name or reference name. No discovery,
network acquisition, embedded-source fallback or installation is performed.

Build/deploy the existing Worker normally with
`dotnet build src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj -warnaserror`.
The existing isolated Debug output is under
`artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/`.
Point workerAssembly at its DLL, retaining its runtimeconfig and dependency files.
This command is preparation, not an automatic action by the CLI. An explicit
dotnetHost should be an installed host executable (absolute path, or executable
name resolved by the normal process environment); it is not a raw Worker request.

Only selected-root exception findings are checked. Other documentation detectors,
whole-project coverage, SLOC statistics and comparison are not claimed for this
input kind. Console explicitly labels this scope. Normal reports retain their
existing schema and metrics; finding counts are real, not a full-project census.

## Production chain and trust

Program/ArgParsing → ToolOptions.ExternalTarget → ToolRunner.RunExternalTarget
→ ExternalTargetExecution raw local job composition
→ existing bound assembly descriptor/P4 PE/P5A PDB validation
→ P5G configuration → P5H checksum-validated source/P5I/P5J complete source set
→ P5C exact immutable reference snapshots/P5E reference set
→ exact PE metadata root → B22 FindExternalExceptionSmellsAsync
→ B10 projection/B7 provenance/B6 selection/B8 dispatch/B5 router/B4 client
→ actual isolated Historical Worker → response validation/B21 attestation
→ B22 Main guard/optional independently attested Summary/shared Main policy
→ Finding → RunResult.AccumulateFindings → unchanged ReportFile/CompleteReporting
→ normal Program summary and finding-based exit.

The temporary Current metadata context binds only submitted PE/reference images;
it does not parse/bind/recompile the submitted source to derive Historical flow.
No target code is executed. Reference bytes are read once and the same immutable
snapshots feed metadata binding and original P5C validation, avoiding a second
file-read TOCTOU world. All original receipts remain Main-local and unforgeable
through the job. No B7/B10/B19/B21 shortcut, extra selection rule, dispatch/router
layer, Analyzer change or Roslyn-version change was introduced.

Main configures the existing client with a 60-second bounded deadline and the
explicit worker. The original B6 policy makes the only engine choice. A validated
Current identity is visibly unsupported by this Historical-only input operation;
it never becomes a Current fallback. The existing Current project/solution path
and its shared sessions are untouched.

Successful B22 Findings are accumulated once and grouped by their existing file
for one ReportFile call per group. No separate Historical finding collection,
translation/import policy or reporter exists. Empty successful results still
complete normal reports. Reporter creation happens only after B22 success.

## Exit and error contract

* 0: supported completed selected-root check, no findings.
* 1: completed check with findings; ordinary Console/JSON/SARIF and RunResult.
* 2: rejected CLI syntax/input-kind/mode combination or missing job path.
* 4: rejected/invalid/incomplete external materials, unsupported profile/root/
  compiler context, Worker failure or B21/B22 admission failure. Stderr explains
  the failing boundary. No success summary, no report or partial Direct findings.

The normal Current exit codes remain unchanged. Output files are only written
by existing reporters, after the entire selected B22 operation succeeds.

## Tests and attempts

New permanent tests call the actual executable with fresh unique raw artifact
directories and a different working directory, covering all three reporters and
zero findings. A documented throwing non-selected helper verifies selected-root
scope: it must not produce its own extra Finding. A direct ToolRunner test also
checks the ordinary RunResult count and absence of loaded Historical assemblies.

Twenty negative CLI cases cover missing PE/PDB/source, mismatched/invalid PDB,
source checksum mutation, Current compiler identity, invalid/ambiguous/unsupported
root, wrong root source, Release and signed target profiles, invalid references,
missing Worker, genuine incomplete Worker analysis, generic B22 policy-contract
rejection, unknown/duplicate job fields and a physical adversarial process
replaying a genuine matching Worker response. The replay must specifically fail
B21 execution attestation. Three more CLI cases reject other Historical modes;
six parsing cases reject unsupported operations. No E2E test invokes the internal
B22 Finding API instead of CLI/ToolRunner. Replay setup obtains neutral evidence
from the real Worker only; admission under test remains the real CLI path.

Retained attempts, without suppression or opportunistic foreign fixes:

1. Initial PowerShell launch was blocked by Windows execution policy; rerun
   using process-local ExecutionPolicy Bypass, no machine policy change.
2. Initial sandboxed restore failed NU1900 because the NuGet security service
   was inaccessible. Unchanged authorized network restore passed; audit was not
   disabled and warnings were not suppressed.
3. Relative export-cwd setup mistakes were visible in tool output; corrected
   original-root/absolute invocations. They are not successful validation claims.
4. First new test run: 31/33 passed, two JSON checks used camelCase rather than
   the existing PascalCase report. Only test assertions changed; reporter/schema
   stayed unchanged. With three added mode cases, second run passed 36/36.
5. Initial dual gate passed 578/578. Second gate passed 577/578: unchanged B16
   mixed-direct-transitive reached its final Stopped(pid) assertion after the
   semantic/parity checks, but that PID appeared live. An unchanged isolated
   B16 repeat passed 18/18. Client explicitly awaits process exit; PID reuse is
   plausible but not established by the retained evidence. No test, client or
   process logic was repaired, skipped or weakened. Both gate directories remain
   under artifacts/p5o2b23; final logs/TRX are separately authoritative.

## Validation status

Final completed results (all with zero failures/skips):

| Group | Passed / total |
| --- | ---: |
| B23 | 36 / 36 |
| Unchanged B22 | 46 / 46 |
| Unchanged B21 | 26 / 26 |
| Unchanged B20 | 10 / 10 |
| Unchanged B19 | 39 / 39 |
| Unchanged B18 | 7 / 7 |
| Unchanged B17 | 25 / 25 |
| Unchanged B16 | 18 / 18 |
| Unchanged B15 | 28 / 28 |
| Unchanged B14 | 31 / 31 |
| Unchanged B13 | 34 / 34 |
| Unchanged B12 | 29 / 29 |
| Unchanged B11 | 16 / 16 |
| Unchanged B10 | 34 / 34 |
| Unchanged B5–B9 | 156 / 156 |
| Productive (including B23) | 717 / 717 |
| Focused (including B23) | 824 / 824 |
| Broad | 3,125 / 3,125 |
| Full configured suite | 3,216 / 3,216 |
| Permanent existing boundary | 578 / 578 |
| Permanent added external CLI boundary | 36 / 36 |

The complete final Verify-DualVersionBuild.ps1 run used
`-RunMainBoundaryTests -BuildTestProjectReferences`. All eleven WAE build steps
passed with zero warnings/errors, including the Current/Historical alternating
clean builds, Worker builds, fixture builds and test-project build. The test
project was initially built from a fresh HEAD export with no bin/obj/artifacts;
all subsequent source changes were rebuilt and matched by the verifier.

All eight full Current differential JSON baselines are byte-equal. Existing
architecture, canonical, factory, RuntimeAwait, Worker/runtime identity and
isolation gates ran as part of the named groups and full suite. No old B5–B22
test was edited. The final full suite succeeded on its first full execution;
the earlier boundary-only PID assertion failure is retained above, not hidden.

Evidence verifier passes on the actual logs/TRX, 29 persisted real CLI outcomes,
all eight built inputs, original protected state and actual source fingerprints.
Owned C# whitespace, Git whitespace, JSON parsing, PowerShell syntax, strict
UTF8/no-BOM/CRLF and tracked/new path case checks pass. This is a static
cross-platform path check, not a claim that a Linux runner was executed locally.
The audit starts with Passed=false on each invocation so an interrupted verifier
cannot leave a stale success. Full artifact paths and hashes are in the audit.

Reproduction in this workspace (retained B22 comparison artifacts are required):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B23Proof/Export-Validation.ps1 -Destination artifacts/<fresh-b23-export>
# In that export's Tools/XMLDocNormalizer:
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
# From the original tool root:
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B23Proof/Run-Regressions.ps1 -ValidationRoot <export-tool-root>
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B23Proof/Write-Verify-ExternalTarget.ps1 -ValidationRoot <export-tool-root>
```

Use the existing captured protection snapshot for this run; recapturing it after
changes would not be a valid before/after proof. No validation build ran against
the protected original Core output directory.

## Changed files

Production changes are limited to ArgsParsing.cs, ToolOptions.cs, Program.cs,
ToolRunner.cs, ToolExitCodes.cs and the new Execution/ExternalTargetExecution.cs.
No Detector/B21/B22/Analyzer/Worker/Roslyn-version source is changed.
ExceptionFlowExternalTargetCliTests.cs is new; old B5–B22 tests are unchanged.
Verify-DualVersionBuild.ps1 adds a separate permanent B23 test step, retaining the
578-test existing boundary step and all eleven WAE builds.
OPEN-PIPELINE-BOUNDARIES.md, this report, the audit JSON and P5O2B23Proof's
capture/export/sync/format/regression/verifier scripts plus raw-job example
provide documentation and reproducible evidence. Generated bin/obj and raw test
artifacts stay ignored. The root .gitignore WIP is not an owned B23 change.

## Boundaries and handoff

B23 closes the explicit local raw-input/CLI/output adoption gap for the original
B22 scope only. BND-P6-003's old selection statement, BND-P5O2B-003/004/005's
outdated single-source/no-Findings/no-CLI generalizations are qualified by the
current adoption note; chronological milestone evidence is retained.

Still open: general Historical Direct/Project/Solution final-Finding contracts,
cross-project Declared, arbitrary roots, signed Historical targets, richer
compiler/reference/XML/source profiles, automatic discovery/acquisition/loading,
and supporting-dependency import into consumer summaries. Dapper still fails
upstream at P5G release-debug-plus; no Dapper progress or broader CLI support is
claimed. External comparison is explicitly unsupported, not a new comparison
platform. No new independent contract/trust gap required invoking the Stop rule.

Self Analysis 16 and Canonical Diff 0/0/0 are previous B22/B21 evidence, not fresh
analysis of added B23 source. The eight unchanged Current differential fixtures
are checked separately by fresh regression output. Historical Analyzer and
Worker source hashes remain unchanged. Cross-export raw DLL/PDB equality is not
claimed: exports have different absolute build roots; within-export repeatable
builds, exact runtime images and source equality are the relevant gates here.

No commit/push/stash operation. Final protection/HEAD/status checks are enforced
by Write-Verify-ExternalTarget.ps1 and recorded in the audit JSON.

## Final repository audit

HEAD stayed a3d811c68c72fbd6f911bf51e82cd95ff60970f6 on fix/documentation;
no external HEAD movement was observed. Root .gitignore and index bytes are
unchanged. All 23 protected physical Core files and all six stash identities
are unchanged. No generated bin/obj files are included as product changes.

Final `git status --short` (original protected WIP remains the first entry):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer/Cli/ArgsParsing.cs
 M src/XMLDocNormalizer/Cli/ToolOptions.cs
 M src/XMLDocNormalizer/Execution/ToolExitCodes.cs
 M src/XMLDocNormalizer/Execution/ToolRunner.cs
 M src/XMLDocNormalizer/Program.cs
?? Evaluation/P5O2B23-bounded-external-target-adoption-audit.json
?? Evaluation/P5O2B23-bounded-external-target-adoption.md
?? Evaluation/P5O2B23Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowExternalTargetCliTests.cs
?? src/XMLDocNormalizer/Execution/ExternalTargetExecution.cs
```
