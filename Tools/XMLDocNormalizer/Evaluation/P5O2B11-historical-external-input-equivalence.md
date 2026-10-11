# P5O2B11 - Historical External Input Equivalence

Starting HEAD: `37c7b286562ffea384f1c845f54290a0c9917005` (separately committed B10).
Initial working tree: protected root `.gitignore` only; empty index, six stashes,
23 Core files verified. No AGENTS.md applies. This inventory/decision was recorded
before production or test code changes.

## Phase 1: code-derived inventory and gap matrix

Current means the real detector's compilation-bound SummarySession and Main
ProjectClosureSemanticContext, not merely a source string. External structural
inputs come from P4/P5A/P5G/P5H/P5I/P5J/P5E/P5K; external source registration is a
supporting dependency and is not yet an automatically selected Historical target.
Historical below means the existing B3/B4 bounded host with B10 automatic payload.
An option's presence is not proof it changes exception-flow semantics.

Classification: E = equivalent inside the stated profile; M = missing/restricted;
V = deliberately version-specific, reconstruct in the Worker; I = no independent
flow input (but possibly reporting, diagnostics, acquisition or cache policy).

| Input / effect | Current / code authority | Historical before B11 | B11 measure / classification | Equivalent |
| --- | --- | --- | --- | --- |
| Exact decoded source text, trivia, directives, member bodies | P5I.Text / Tree; native Compilation.SyntaxTrees | One exact text from B10, parsed in Worker | E; retain decoded text without normalization | Yes, bounded single tree |
| Original source bytes, encoding/BOM/checksum | P5H material, P5I checksum-checked decoding | Main retains original objects; IPC binds UTF8 of decoded text | I for parsing after exact decoding; keep original provenance Main-local | Text yes; original-byte attestation not IPC |
| Logical document path | P5I document name/Tree.FilePath; ExceptionFlowPathFactory.GetLineSpan | Replaced by worker-input.cs | M closed by B11 source-provenance slice: transport exact logical path, no file access | Yes after B11, bounded profile |
| Line/column, line endings, #line/mapped directives | Actual syntax locations; path factory uses unmapped GetLineSpan | Same text/positions, substituted physical logical path | E positions; B11 fixes path, no #line reinterpretation | Yes after B11, bounded profile |
| Complete ordered source/tree set, partial types, generated output | P5J set/P5K; native project compilation | Host requires exactly one tree; B10 rejects multiple documents | M; independent B13 source/scope family | No |
| Concrete language version | P5G stable numeric LanguageVersion, native parse options | C#12 only | M; differential compile/admissibility probe; defer parse-context expansion | Only C#12 |
| Preprocessor symbols and order | P5G defines / CSharpParseOptions; controls active bodies | Empty only, B10 rejects defines | M; differential #if flow probe; B13 parse/source family | Only empty |
| DocumentationMode | P5G explicit Parse analysis policy; native may vary | Default Parse | E in external P5G profile; Diagnose/None affect docs/diagnostics, not assumed irrelevant globally | Profile yes |
| SourceCodeKind | P5G Regular policy; native parse options | Regular | E in supported external profile; Script not admitted | Profile yes |
| Parse feature flags / preview defaults | Native CSharpParseOptions.Features and compiler API | No feature envelope; C#12 only | V/M; no default/latest inference or arbitrary Roslyn serialization | Not generally |
| Nullable modes / annotations / flow-state API | P5G NullableContextOptions; semantic type facts | Enable only | M restriction; test annotations changed vs flow evidence; analyzer does not trust annotations alone | Enable only |
| checked overflow and unsafe | P5G compilation options; affects diagnostics/binding/constant folding | False only | M; compiler-context family, not copied without differential proof | False only |
| Output kind, platform | P5G options / P5K assembly composition | DLL / AnyCPU only | M; affects entry point/native width/module shape; keep explicit restriction | Bounded profile only |
| Optimization | P5G Debug/Release; current semantic reconstruction does not emit | Debug only | M admission restriction; no demonstrated direct flow gap; release-debug-plus fails upstream P5G | Debug profile only |
| Assembly/project name and module name | P5K target name/module identity; native Compilation.Assembly | HistoricalWorkerInput / synthetic module | M; identity family B12 (source exception identities, friend binding) | No |
| Source assembly version/culture/attributes | Source attributes and P3/P5K identity | Source attributes parsed, but synthetic name | E attributes; M full original identity | Not full identity |
| Signing/public key, friend access | P4 signing / P5G CryptoPublicKey / P5K full identity | Unsigned only | M; B12 identity/reference closure, no private key transport | No for signed targets |
| Ordered PE metadata reference images, MVID/hash | P5E validated references/P5K exact ordered instances | Seven runtime framework images discovered in Worker | M; B12 exact reference family; fixed framework policy is not original-reference equivalence | No generally |
| Reference properties: assembly/module kind, aliases, EmbedInteropTypes | P5A metadata ordinals/P5E MetadataReferenceProperties | Default assembly properties only | M; B12 with image provenance | No generally |
| Referenced assembly type/member/inheritance/constant/attribute metadata | Current semantic binding through references | Fixed framework binding only | M; explicit external-exception differential probe | No for arbitrary references |
| DocumentationProvider / XML sidecar exception contracts | ExternalDocumentationExceptionModel.GetDocumentationCommentXml / sidecar lookup | No provider/sidecar input; source-less calls may be incomplete and fail | M; B12 with references, not Reporting Import | No generally |
| Framework/CoreLib identity and runtime API surface | Original reference set; Compilation.GetTypeByMetadataName | net8-runtime-bounded-v1 fixed runtime images | M/V; reference profile must be explicit, no original-target claim | Controlled fixture only |
| AssemblyIdentityComparer / portability policy | P5G default comparer; unsupported portability policies fail | Default comparer | E in accepted profile; nondefault policy unsupported | Profile yes |
| Resolvers, key file/provider, warning/diagnostic maps, concurrent/deterministic emit flags | Native Compilation.Options; P5G supports a restricted structural subset | Default options; Worker rejects compile errors | I/V/M: no separate resolver/file transport; warnings can affect admissibility, not proven irrelevant globally | Not general compiler equivalence |
| Generator/analyzer/additional-file/config effects | Current sees generated final trees; project build may supply inputs | Does not run generators/analyzers | I if already materialized into complete source; M completeness handled with B13 | No general output completeness |
| PE/PDB target/compiler provenance and trust receipts | P4/P5A original descriptors; P5G/P5I associations | Shared exact descriptor for B7/B10, original objects Main-local | E trust/selection; no second provenance parser/validator | Yes at Main boundary |
| Root method binding, signature, generic/call context | Current exact source symbol; external PE symbol mapped through supporting resolver | Exact type/name; parameterless static nongeneric source method only | M full canonical root identity/signatures; same bounded selector only | Bounded selector only |
| Declared types/dispatch candidates, referenced projects | Main host enumerates deterministic project/closure scopes | One compilation scope | M; B13 scope/host family | No generally |
| External supporting source registry and exact method resolution | Main SupportingSourceCatalog / SupportingSourceSymbolResolver | No external registry, exact local source only | M; B13 dependency graph/source family | No generally |
| Analysis mode | XmlDocOptions + session factory; four Current modes/guards | SolutionTransitive only | E for supported mode; others typed rejected | Supported mode only |
| Summary-session lifetime/cache/graph reuse | ToolRunner owns one sequential session; graph grows across roots | One request/root, fresh process/environment | V operational lifetime; repeated logical input must be deterministic; no cache serialization | Results for one root, not multi-root state |
| RuntimeAwait capability / compiler operation/symbol APIs | Current semantic host/compiler supplies actual API info | Historical RuntimeAwait unavailable, affected await cases fail closed | V deliberate compiler capability, not missing raw input; no invented Current facts | No for affected await cases |
| Known framework-contract rules, budgets, semantic guards | Shared Analyzer/fact source set | Same 121 source files; Worker adds transport/completeness budgets | E algorithms, V admission; do not change analyzer semantics | Within supported domain |
| Native compiler error/uncertainty behavior | Structural P5K may retain diagnostics; Current may return uncertainty | Worker rejects compile errors/uncertainties/truncated paths | V explicit completeness contract, not equivalent unrestricted success domain | Successful bounded complete cases only |
| Documentation smell policy, finding target/file context, filtering | Detector + XmlDocOptions/ToolRunner reporting | Canonical analysis only; no Historical finding import | I to flow, M reporting boundary explicitly out of B11 | Findings not globally equivalent |
| Cancellation, process deadlines, input/output/node budgets | Main execution/session vs Worker client constraints | B4 typed failures, cleanup and bounds | I to successful flow semantics; preserve failure contract | Operationally explicit, not copied |

Code authorities inspected: ExternalCSharpCompilationConfigurationFactory,
ExternalCSharpSyntaxTreeFactory, ExternalCSharpCompilationFactory,
ProjectClosureSemanticContext/ProjectClosureExceptionFlowSemanticEnvironment,
HistoricalSemanticEnvironment, ExceptionFlowSummaryAnalysisSession and graph
construction, XmlDocExceptionSemanticDetector/ToolRunner/XmlDocOptions,
ExceptionFlowPathFactory, ExceptionFlowContextualFactEvaluator.Nullability,
ExternalMetadataReferenceFactory/ExternalDocumentationExceptionModel,
WorkerProtocol/HistoricalAnalysis/HistoricalWorkerResponseValidation.

## Scope decision before implementation

The gaps are several independent large families, not one small payload omission.
Use the prompt's bounded-slice rule. B11 closes only exact logical source-path
equivalence and its request/response/flow provenance binding. This is already an
observable mismatch inside B10's successful single-source profile: Current
records `/controlled-historical.cs`; the Worker records `worker-input.cs`.

An additive optional LogicalPath on the existing neutral source input preserves
old explicitly bounded requests (absent path retains the documented legacy
worker-input.cs policy). New production projections must send the real validated
document name; empty/oversized/control-character paths are rejected, never
normalized or opened. Both returned provenance and every canonical flow step
must match that exact request path. Unknown/duplicate fields remain fail closed.
No new source acquisition, decoder, PE/PDB validator, selection or engine policy.

Differential tests are built before changing production. They first witness the
old path mismatch using actual Current analysis and a genuine Worker. Separate
probes expose active #if flows, language admissibility, nullable annotations vs
flow, external metadata binding and multiple source trees; those families stay
explicitly unsupported in B11 rather than being forced into a mega-change.
Graph-count/repetition proof may use a test-only diagnostic operation in the
existing isolated Historical fixture, never Historical loading into Main or a
production graph/Reporting protocol expansion.

B12: exact validated references/properties/XML contracts, original assembly/signing
identity and framework-reference equivalence. B13: complete source/parse/compiler
context plus project/supporting-source graph and exact root bindings, with proven
capability/admissibility limits. Historical Reporting Import, CLI discovery and
other call-site migration remain separate, unimplemented work. No full external
input equivalence or cross-engine full Finding import will be claimed here.

## Execution and closure

Completed as the explicitly permitted smaller B11 slice, not full external input
equivalence. B12/B13 are recommendations/remaining work, not implemented packages.

## Actual differential evidence

Before any production edit, the unchanged committed B10 sources were built in a
clean Git archive, and nine tests actually passed: the eight unchanged complete
Current baseline cases plus an export-only before-gap witness. That witness
uses the real B10 projection and genuine Historical PE/PDB/Worker. Current flow
has `/controlled-historical.cs`, Worker flow has `worker-input.cs`; full canonical
results differ. A separate diagnostic comparison showed that replacing that one
path makes them equal, proving the particular missing input rather than merely
comparing counts. The before witness source, TRX and JSON are retained; its
export-only test file was then removed, not shipped as a permanently false claim.

After B11, the same real validated external handoffs automatically project the
original P5I document name. Complete canonical results are equal without any
normalization: exception identities, evidence kinds, every flow step's symbol,
source path/line/column, truncation flags and uncertainties. Two fresh real Worker
processes return identical results and have exited; no Current result/fallback
or Historical assembly load in Main. The original PE/PDB hashes and actual
Historical compiler identities still match the genuine emission.

The new source-path tests additionally cover Unix, Windows, relative and Unicode
logical names, nonexistent file-like names, CRLF source, a local callee and #line.
All full flows match Current; unmapped physical positions remain the existing
Analyzer policy. Names are opaque input provenance, not paths to open, resolve,
canonicalize or sanitize into another identity.

The existing isolated HistoricalPdbFixture has one test-only diagnostic branch
and helper. It observes the actual unchanged Historical semantic host/summary
session, using the same seven references and logical input, through reflection
inside that child process. Its graph count, repeated count and full canonical
output equal Current and the genuine production Worker result. It neither
executes submitted code nor changes the production Worker/graph/Reporting
protocol. This proves relevant graph count/repetition, not full multi-root graph
state transfer or project-closure equality.

Gap witnesses actually executed:

- Preprocessor defines: same text with THROW produces an exception in Current;
  empty defines and existing fixed Worker yield no exception. Not equivalent.
- Language: C#10 reports CS8936 for the raw-string source while C#12 is valid.
  This proves an admissibility/parse-context gap, not a newly supported language.
- Nullable: Disable versus Enable changes parameter annotations, while full
  explicit-throw flows are identical in the tested case. No annotations-only
  non-null fact was invented; unrestricted nullable equivalence is not claimed.
- External metadata: a real emitted dependency supplies ExternalException to
  Current; the framework-only Worker returns CompilationFailure, not a result.
- Multiple trees: Current analyzes the local callee in the second source;
  the single-source Worker returns CompilationFailure. No tree is dropped and
  no host/SupportingSource redesign was smuggled into the path slice.

Full native Current findings are serialized and repeated in the new proof and
all eight existing before/after fixture files are compared completely. There
is no Historical finding representation/import at this boundary. Consequently
cross-engine complete Finding parity cannot honestly be claimed in this slice;
full canonical flow/source/uncertainty parity is the differential measurement,
and Historical Reporting Import remains explicitly separate work.

## Contract, projection and error changes

Four production files only:

- `WorkerProtocol.cs`: optional neutral `WorkerAnalysisInput.LogicalPath`, a
  4096-character path budget and validation; no Roslyn types or file access.
- `HistoricalWorkerPayloadProjection.cs`: pass the exact original immutable
  `input.Source.Document.Name`, retaining every existing validation receipt.
- `HistoricalAnalysis.cs`: use the effective exact path for the existing
  Historical syntax-tree reconstruction and returned source provenance.
- `HistoricalWorkerResponseValidation.cs`: bind the returned provenance and
  every canonical step to the exact requested logical path, alongside existing
  source hash, selector, runtime identity and position validation.

Protocol remains v2 with an additive optional field. Absent/null on a legacy
explicit bounded request preserves its documented worker-input.cs policy and
serializes without the new field; old request behavior is independently tested.
Production uses a required non-null immutable PDB document name; empty names
are rejected, not changed into the legacy default. An older strict peer receiving
the new field fails closed; no downgrade, fallback or cross-peer guarantee is
claimed. Unknown/duplicate fields and inconsistent envelopes retain existing
strict handling. No compiler selector/version contract changed.

Empty, whitespace, control-character and over-budget supplied paths fail with
UnsupportedInput before a process starts. Missing required source/unsupported
options retain the same typed failures. Forged paths in response provenance or
any flow step yield MalformedResponse and no result. B10's unchanged raw/copied,
missing, conflicting and unvalidated handoff tests retain their typed failures;
start/cancellation/structured/crash failures keep the existing B4/B5 behavior.
No silent normalization, second source decoder, PE/PDB parser/validator or
Current fallback. The native Current detector, session/mode behavior, shared
Analyzer algorithm, B5-B9 execution/selection and Roslyn versions are unchanged.

## Validation

| Actual final validation | Passed |
| --- | --- |
| Unmodified B10 production before witness + eight Current baseline cases | 9/9 |
| New B11 tests | 16/16 |
| Existing B10 payload tests, unchanged | 34/34 |
| Independent B5-B9 tests, unchanged | 156/156 |
| Productive ToolRunner/closure/supporting-source/DOC and factory regressions | 676/676 |
| Focused Worker/RuntimeAwait/dependency/Historical-build/canonical regressions | 370/370 |
| Broad Check/Execution/Evaluation/Worker group, unchanged retry | 2796/2796 |
| Full configured suite | 2887/2887 |
| Permanent boundary gate | 285/285 |

Groups overlap; they are not additive unique counts. Default runsettings remain
unchanged and exclude the separate SelfAnalysis category. Final runs have no
failed/skipped tests. Current/Historical/Worker warning-as-error builds and full
dual-version gate pass with zero warnings/errors: six forced library builds,
two Worker builds, adversarial process, real Historical PDB fixture and test
project with real references. Deterministic images, exact runtime/package
identity and process isolation pass; all 121 shared Analyzer sources and the
one Historical semantic-host source are unchanged.

The first broad run was NOT successful: its TRX summary is Failed/aborted even
though its counters record 2782 passed results and zero assertion failures.
Windows Application event 1000 records testhost.exe fail-fast in clrjit.dll
(8.0.24), exception 0xc0000409, PID 23900. The causative test/root cause was not
established. An existing metadata fixture's reader lifetime was inspected but
is not proven responsible and was not altered. No known flake was repaired.
A BoundaryTestProcess child, PID 28836, survived that native crash; after verifying
its exact export command line and crashed parent PID, only that own orphan was
stopped so the aborted runner could finish. Failed TRX/log and Windows event /
cleanup evidence are retained. Full suite then passed; the identical broad
filter was rerun without source/test changes or exclusions, with only --blame
diagnostic sequence collection. The successful rerun does not erase the abort.

One export setup retry is also retained: git archive initially used a root-relative
pathspec from tool cwd and rejected it. The newly created export was verified
empty, then archive was rerun from the actual repository root. No worktree/index
recovery, stash operation or build/test workaround was involved. No build or
assertion failure occurred in B11's new tests/gates.

Folder-only format, strict UTF8/CRLF, JSON, PowerShell syntax, git diff --check,
architecture/isolation and evidence/protection checks are covered by the proof
verifier. It checks TRX run summary as well as counters, so a host-aborted run
cannot masquerade as PASS. Test execution and parent/final source fingerprints
come from `artifacts/b11-37c7`, a clean Git export plus only owned file overlay;
original workspace Core outputs were never built. Local Windows only; no remote
or Linux execution is claimed.

Self Analysis 16 and Canonical Diff 0/0/0 are inherited B10 values, not freshly
executed B11 results. The new full-flow differentials and eight full Current
finding/graph fixtures are narrower fresh evidence, not a new whole-tool self
analysis or canonical finding diff.

## Changed files, repository protection and reproduction

In addition to the four production files above:

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs`.
- `Evaluation/P5O2B7Proof/HistoricalPdbFixture/Program.cs` and new `B11GraphProbe.cs`:
  existing fixture's isolated test-only graph diagnostics; default emission unchanged.
- `build/Verify-DualVersionBuild.ps1`: include B11 class in existing permanent filter.
- `Evaluation/OPEN-PIPELINE-BOUNDARIES.md`: BND-P5O2B-005 bounded closure and B12/B13 gaps.
- This report, `Evaluation/P5O2B11-historical-external-input-equivalence-audit.json`
  and `Evaluation/P5O2B11Proof/Write-Verify-InputEvidence.ps1`.

The pre-edit snapshot checks 768 tracked source/build/configuration files;
only six existing owned files changed, all 762 others remain byte-identical.
All 23 protected Core files including bin/obj, root .gitignore bytes, six stashes
and empty index are unchanged. All existing tests, Current detector, factory
validators, project files, shared engine and Historical semantic host are intact.
No commit, push, staging or stash change. Initial HEAD advanced between tasks
from a3194cf0 to the separately committed B10 37c7b286; no HEAD movement or other
external change was detected during B11.

Isolated tool-root build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
```

Regression sweeps use `dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj
--no-build --no-restore --logger 'trx;LogFileName=<group>.trx' --results-directory
artifacts/p5o2b11/tests`, explicit OR filters for the named groups (the same as
B10 plus B11) and no added filter for the complete configured suite.

Worktree evidence/protection verifier:

```powershell
$export = Get-Content artifacts/p5o2b11/export.json -Raw | ConvertFrom-Json
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B11Proof/Write-Verify-InputEvidence.ps1 -ValidationRoot $export.ToolRoot
```

The matrix and JSON explicitly leave B12 references/identity, B13 source/context/
scopes, Historical Reporting Import, CLI/acquisition/discovery and other call-sites
open. No Dapper architecture change or full external pipeline advancement. Stop
at the audited source-origin B11 slice; no further family was implemented.

Final `git status --short` (root ignore is pre-existing protected WIP):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Evaluation/P5O2B7Proof/HistoricalPdbFixture/Program.cs
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
?? Evaluation/P5O2B11-historical-external-input-equivalence-audit.json
?? Evaluation/P5O2B11-historical-external-input-equivalence.md
?? Evaluation/P5O2B11Proof/
?? Evaluation/P5O2B7Proof/HistoricalPdbFixture/B11GraphProbe.cs
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs
```
