# P5O2B10 - Historical Worker Payload Projection

Starting HEAD: `a3194cf086280b6da5b0798e9357bb630f383c38` (committed B9).
Only protected root `.gitignore` WIP was present; index empty, six stashes.

## Investigation and mapping recorded before implementation

B9's real detector execution seam already dispatches Current and prepared PDB
requests. Its Historical tests manually construct WorkerAnalysisInput from a
known source string, "Fixture", "Root" and a default WorkerCompilationContext.
The existing bounded protocol accepts only one source compilation, a non-generic
parameterless static ordinary method, solution-transitive analysis, C# 12,
nullable enable and the fixed net8-runtime-bounded-v1 reference profile.
Additional documents, reference images, supporting compilations, compiler option
envelopes, preprocessor symbols and RootIdentity are not supported by the Worker.

Existing external acquisition retains P3 target identity, P4 PE debug provenance,
P5A target-bound PDB/options, P5H byte-exact source material and P5I decoded text/tree.
P5G reconstructs parse/compilation options without selecting a compiler. P5I works
with historical provenance too: its Current parser does not claim original compiler
equivalence. These objects suffice for bounded payload mapping, not full external
compilation equivalence. Normal ToolRunner still supplies only Current inputs;
supporting-source reconstruction is not a Historical reporting target.

Use a focused typed input: the existing P5A provenance, existing P5I source tree,
the PE-bound IMethodSymbol and explicit ExceptionAnalysisMode. No raw source or
selector string is accepted. Successful P4/P5G/P5I factories will retain weak
reference-origin associations with their already validated target/provenance/
configuration. This is a receipt, not another validator/parser. B10 can reuse the
exact original objects and reject caller-minted copies without rereading files.
P5I already validates the source checksum; no new P5H validation is needed.

| Worker field | Existing typed input / policy | Boundaries |
| --- | --- | --- |
| Source | P5I.Text.ToString(), already decoded from P5H bytes | Same receipted PDB document/configuration; no decode, parse or newline normalization |
| TypeMetadataName | PE method's containing type metadata/namespace/nesting | Exact module MVID/name and assembly identity from P4's existing target receipt; no raw selector |
| MethodName | PE ordinary method MetadataName | Parameterless/static/non-generic/body-capable bounded selector |
| Context.AnalysisMode | Explicit ExceptionAnalysisMode.SolutionTransitive | Other modes rejected, not defaulted |
| Context.LanguageVersion / NullableContext | P5G's recorded CSharp12 / Enable | Other concrete semantics rejected, not normalized |
| Context.ReferenceProfile / AssemblyName | Published Worker analysis policy | net8-runtime-bounded-v1 / HistoricalWorkerInput are Worker policy, not claimed original references/assembly identity |
| AdditionalSources / References / SupportingCompilations / RootIdentity / CompilerOptions / PreprocessorSymbols | Absent in the existing bounded contract | No invented completeness or silent dropping; unsupported source count/options fail closed |
| Original PE/PDB/material provenance | Retained by the Main-local projection result alongside the Worker payload | Existing IPC cannot carry original provenance; Worker response binds transported text, not original-byte checksum |

B10 will not compare compiler versions or choose engines. A projectable PDB from
another compiler may produce the same available Worker data; B6 still decides and
B5 rejects an incompatible selected-engine/payload pair. Native Current never
enters B10. Unsupported source-count, defines, signing or concrete compilation
options are rejected rather than replaced with Worker defaults. Reference/runtime
equivalence and original target identity in Worker results remain explicitly open.

The smallest adoption is one additional external-input entry on the existing B9
detector seam: B10 projection -> existing typed PortablePdb + Worker payload ->
B9 async seam -> B8/B7/B6/B5/B4. Payload failure returns a typed outcome and never
reaches dispatch. No Worker/engine selection, reporting import or CLI rewrite.

## Validation and closure

The bounded automatic payload boundary is implemented. The investigation above
was recorded before implementation; the following is the executed closure, not
a claim of full external input equivalence or whole-pipeline migration.

## Implementation, trust and error contract

`src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs`
owns `HistoricalWorkerPayloadProjection.Project` and the focused typed input,
success value and failure records. Its input has only the existing provenance,
source tree, PE method symbol and explicit mode; no string-based source/selector
trust boundary. The success value retains those same original Main-local objects
alongside the unchanged `WorkerAnalysisInput`.

The existing P4 PE, P5G configuration and P5I syntax-tree factories now remember
their successful target/provenance/configuration associations in weak reference
tables. No factory validation branch, compiler policy or analyzer behavior was
changed. B10 checks the existing P5A receipt, these handoffs, the same original
PDB document and the method's existing PE module/assembly metadata. The latter
must match P4's retained manifest module name/MVID and target assembly identity.
Source-owned methods and copied records do not acquire authority. The tables
do not retain dead input graphs as permanent global caches.

Source text is transported from `P5I.Text.ToString()` without decoding, parsing,
checksum computation, string normalization or rereading files. Exact type
selectors use namespace/type metadata names and `+` for nested types, rather
than C# display strings (including escaped keywords). Existing successful P5I
already validates original source bytes/checksum. B7 and B10 receive the same
original P5A descriptor, not two independently parsed or validated contexts.
No all-knowing input abstraction was added.

The existing Worker contract remains unchanged. Single-source C#12/nullable-enable,
regular parse/ordinary static parameterless non-generic method, unsigned DLL,
AnyCPU, Debug, unchecked, no unsafe/defines and SolutionTransitive are required.
Unsupported recorded concrete options, multiple PDB documents/source files,
reference assemblies or multiple modules are rejected. The existing Worker
payload validator enforces selector/source bounds without truncation. The
existing client still owns transport budgets and process/protocol validation.

`net8-runtime-bounded-v1` and `HistoricalWorkerInput` are explicitly published
Worker analysis policies, not recovered original references or assembly identity.
No original-reference/runtime/generator equivalence is asserted. Original PDB
paths, original-byte checksum/material origin/exactness and target provenance
remain on the Main side; the Worker binds the decoded transported text under its
existing `worker-input.cs` logical path. That distinction is retained in JSON
evidence. It must not be mistaken for original external result identity.

Failures are typed: `MissingInput`, `UnvalidatedProvenance`, `MissingValidatedPe`,
`UnvalidatedSource`, `ConflictingInputs`, `UnsupportedProfile`,
`UnsupportedSelector`, `UnsupportedPayload`. Failure has no success value;
the external detector entry returns with absent dispatch/result. Missing PDB/
provenance cannot be defaulted; real PDB without an actual PE receipt is rejected.
Existing B7/B6/B5/B4 failures remain staged downstream failures, never an empty
successful result or Current fallback.

## Minimal adoption and execution behavior

Previously B9 tests manually prepared `WorkerAnalysisInput` and default context.
Now `XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync` accepts
the typed external handoffs and performs B10 mapping before entering the same
existing `AnalyzeConfiguredExceptionFlowAsync` execution seam:

```text
Existing P4/P5A/P5G/P5H/P5I handoffs -> B10 payload projection
 -> same B9 detector seam -> B8 -> B7 (same P5A) -> B6 -> B5 -> B4 Worker
```

Only a 14-line external entry was added to the detector; its Current code is
unchanged. B10 does not choose engines, compare compiler versions, dispatch,
start processes, load Historical assemblies or report findings. No B5/B6/B7/B8,
Worker protocol/host, Roslyn version, analyzer/shared source or project change.

Native Current still takes the B9 Current request through B8/B7/B6/B5 into the
same caller-owned SummarySession. Mode guards/session lifetimes, complete
findings/graph/flow/uncertainty behavior are unchanged, with no B10 payload or
Worker process. A separate negative test maps compatible data from a Current PDB:
mapping itself does not select an engine; B6 selects Current and B5 rejects its
incompatible Worker payload. It does not execute Current or fall back. This is
not the native productive Current route.

The positive Historical test emits real PE/PDB using the actual Historical Roslyn
runtime, passes existing target/PDB/source-byte/configuration/tree validation,
then calls the detector without constructing any Worker payload/context in test
setup. B6 selects Historical from the actual PDB identity. A genuine separate
Worker returns canonical `NullReferenceException` explicit-throw evidence and no
uncertainties. Worker response SHA256 equals the projected decoded text hash;
PE/PDB hashes equal the genuine B7 emission. Historical Roslyn runtime identities,
MVIDs and hashes match that emission and the gate's exact compiler images.
The Worker has exited, no Historical Roslyn/Analyzer/Worker assembly is loaded in
Main and Main's Current Roslyn MVIDs remain unchanged. Start failure and
pre-cancellation expose the original typed Worker error, no result and no
Current analysis. Existing independent runtime/error/isolation tests also run.

## Actual validation

All final groups pass without failed or skipped cases. Suites overlap and must
not be added as unique-test counts. Default runsettings are unchanged and still
exclude the separately invoked SelfAnalysis category.

| Validation | Passed |
| --- | --- |
| Unchanged committed B9 parent Current baseline | 8/8 |
| New B10 payload/trust/profile/automatic-runtime tests | 34/34 |
| Unchanged independent B5-B9 tests | 156/156 |
| Productive regression groups plus all affected PE/configuration/source-material factories | 676/676 |
| Worker/RuntimeAwait/dependency/Historical-build/canonical focused group | 354/354 |
| Broad Check/Execution/Evaluation/Worker group, unchanged retry | 2780/2780 |
| Complete configured suite | 2871/2871 |
| Permanent Main boundary gate | 269/269 |
| Known MultiModule fixture, isolated retry | 1/1 |

The first broad run had 2779/2780 passing and one failure in the unchanged,
previously documented `ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed`
fixture assertion at line 446, before its factory assertion. Its log/TRX are
preserved as `broad-first.log` / `tests/broad-first.trx`. The isolated test and
identical full broad rerun passed without test/production edits, exclusions or
suppression. This failed attempt is explicitly part of machine evidence.

Both full dual-version gate runs pass: initially 266 boundary cases, then 269
after adding exact nested/keyword selectors, multiple sources and source-budget
coverage. All eleven warning-as-error build steps have zero warnings/errors:
six forced Current/Historical library builds, two Worker builds, adversarial
process, real Historical PDB fixture and test project with real project references.
Repeated images are deterministic and package/compiler/runtime separation and
genuine isolated execution gates pass. All 121 shared Analyzer sources and the
single Historical semantic host remain unchanged. No build failure occurred.

The eight unchanged B9 parity tests were actually executed against the unchanged
HEAD export before overlay, and rerun after B10. All eight complete finding-array
and graph-state JSON files are equal before/after, including full locations,
messages, context/evidence and sequential reuse checks. No abbreviated finding
comparison substitutes for that fresh parent baseline.

Validation uses isolated Git archive `artifacts/b10-d8fb9e6b`, initially without
artifacts or bin/obj, then an overlay of B10-owned source/gate files. Original
workspace Core outputs are not rebuilt. `artifacts/p5o2b10/export.json` records
the validation root; separate suite logs/TRX, actual emission and runtime evidence
are retained there. This is local Windows validation, not a Linux or remote run.

Folder-only formatting, strict UTF8/CRLF, PowerShell syntax, JSON parse,
`git diff --check`, architecture/isolation and protected-file checks are performed
by the reproducible proof verifier. The source set actually built is compared
with the worktree; Git-export LF is compared textually with Windows CRLF while
recorded export hashes and original protected byte hashes are checked separately.
The verifier uses Windows PowerShell 5-compatible hashing. A failed atomic edit
attempt while expanding tests was reapplied against formatter-split lines; it
did not mutate files or cause a test/build failure.

Self Analysis 16 and Canonical Diff added/removed/changed-evidence 0/0/0 remain
explicitly inherited B9 baseline values. Neither was newly executed in B10,
and neither is claimed as fresh evidence about B10's source/documentation.
The fresh eight-case complete Current parity comparison is separate evidence.

## Remaining boundaries and stopping point

Closed: automatic trustworthy Worker payload mapping from existing typed external
handoffs, plus minimal adoption at the existing B9 detector execution boundary,
for the existing bounded single-source profile only.

Still open: complete original external document/reference/runtime/options/generator/
supporting-compilation equivalence and original identity in Worker results;
Historical canonical-to-findings/reporting import; ordinary external CLI acquisition/
discovery, other call-site adoption and CLI/reporting boundaries. Normal and
comparison ToolRunner still supply Current requests. Supporting-source registration
is not a Historical reporting target. Direct throw helpers and Analyzer-internal
convenience/graph calls are unchanged. No Dapper redesign or full external pipeline
advancement is claimed. BND-P5O2B-004 is bounded-resolved; BND-P5O2B-003 stays partial.
Work stops here; no equivalence or reporting implementation was started.

## Changed files and protection

- `src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs`: new typed projection/result/error contract.
- `src/XMLDocNormalizer/Execution/Semantic/ExternalPeDebugDirectoryDescriptorFactory.cs`: retain original successful PE target.
- `src/XMLDocNormalizer/Execution/Semantic/ExternalCSharpCompilationConfigurationFactory.cs`: retain original successful P5G provenance.
- `src/XMLDocNormalizer/Execution/Semantic/ExternalCSharpSyntaxTreeFactory.cs`: retain original successful P5I configuration.
- `src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs`: one external-input entry on the existing seam.
- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs`: 34 focused cases; no manual Worker payload in setup.
- `build/Verify-DualVersionBuild.ps1`: add the new class to the existing permanent filter only.
- `Evaluation/OPEN-PIPELINE-BOUNDARIES.md`: bounded closure versus open equivalence/reporting.
- This report, `Evaluation/P5O2B10-historical-worker-payload-projection-audit.json`, and `Evaluation/P5O2B10Proof/Write-Verify-PayloadEvidence.ps1`.

All 23 protected Core files (including outputs), 368 unrelated Main sources,
22 existing project/build files and nine existing Worker test files retain their
pre-edit hashes. Root `.gitignore` WIP and all six stashes are unchanged;
the index remains empty. HEAD stays `a3194cf086280b6da5b0798e9357bb630f383c38`,
the separately committed B9 baseline. No external HEAD movement or other external
change was detected; no agent commit, push, staging or stash operation.

Inside the isolated tool root, full build command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
```

All regressions use `dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj
--no-build --no-restore --logger 'trx;LogFileName=<group>.trx' --results-directory
artifacts/p5o2b10/tests`, with explicit fully-qualified-name OR filters for the
named owner groups above. The productive group extends B9's ToolRunner,
ProjectClosure, ExternalSupportingSource and DOC610/611/631/632 filter with
ExternalPeDebugDirectoryDescriptorFactoryTests,
ExternalCSharpCompilationConfigurationFactoryTests,
ExternalCSharpSyntaxTreeFactoryTests and ValidatedExternalSourceMaterialFactoryTests.
The full suite has no additional filter and no skipped tests.

From this worktree, reproducible closure/evidence verification:

```powershell
$export = Get-Content artifacts/p5o2b10/export.json -Raw | ConvertFrom-Json
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B10Proof/Write-Verify-PayloadEvidence.ps1 -ValidationRoot $export.ToolRoot
```

Final `git status --short` (root ignore is protected pre-existing WIP):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs
 M src/XMLDocNormalizer/Execution/Semantic/ExternalCSharpCompilationConfigurationFactory.cs
 M src/XMLDocNormalizer/Execution/Semantic/ExternalCSharpSyntaxTreeFactory.cs
 M src/XMLDocNormalizer/Execution/Semantic/ExternalPeDebugDirectoryDescriptorFactory.cs
?? Evaluation/P5O2B10-historical-worker-payload-projection-audit.json
?? Evaluation/P5O2B10-historical-worker-payload-projection.md
?? Evaluation/P5O2B10Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
```
