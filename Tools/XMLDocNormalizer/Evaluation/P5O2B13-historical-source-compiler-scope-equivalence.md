# P5O2B13 - Historical Source, Compiler and Scope Context Equivalence

Starting HEAD: `35d69be2a4f50c2f1ae6cde7a7f2452212a6b274`, separately committed B12.
Initial tree: protected root `.gitignore` only. Six stashes, empty index and all
23 Core files snapshotted before changes. No applicable AGENTS.md was found.

## B11-derived submatrix and decision BEFORE implementation

This is a extraction of the existing 36-row B11 inventory, not a replacement
inventory. Related rows are grouped without dropping their distinct limits.
Trust column states original authority, validation and Worker reconstruction.
E = equivalent within admitted profile; O = open; V = deliberate version policy;
I = no independently demonstrated exception-flow input in the tested profile.

| Input (B11 row family) | Current | Historical before B13 | Analysis relevance | Neutral transport | B13 action | Status | Trust / reconstruction / version policy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Exact decoded text / trivia / directives | P5I text/tree | Exact single decoded text | Bodies and active program | Yes | Retain exact text | E | Original P5H checksum/P5I decoding; reparse in child, not new validation |
| Original bytes / encoding / BOM / checksum | P5H and P5I | Main-local original objects; decoded UTF8 hash | After exact decoding no independent parsing effect; original attestation distinct | Yes but unnecessary here | No byte/encoding extension | I | Keep original receipt Main-local; not original-byte IPC attestation |
| Logical path / positions / line endings / mapped directives | Actual tree locations | B11 exact path/unmapped positions | Full source evidence | Yes | Preserve B11 | E | P5I document name; never normalize/open; no compiler emulation |
| Complete ordered trees / partial types / generated output | P5J/P5K exact ordinal sequence | Exactly one tree | Other tree can supply throwing callee; B11 differential witness | Yes, but host lacks multi-tree ownership | Separate source/host package | O | P5J explicit ordinals, not every PDB document is a tree; generators not executed |
| Concrete language version | P5G stable numeric version | C#12 only | Raw strings C#10 CS8936 vs C#12 valid (B11 witness) | Yes | Explicit opt-in validated parse context | E bounded after B13 | Original P5G/P5I same object; child checks actual enum support; no latest/default/downgrade |
| Defines and order | P5G parse symbols | Empty only | #if selects different throwing body (B11 witness) | Yes | Exact ordered symbols in explicit parse envelope | E bounded after B13 | Same original config; reparse with actual Historical compiler |
| DocumentationMode / SourceCodeKind | P5G Parse / Regular | Parse / Regular | Diagnose affects diagnostics; Script changes program model | Yes | Require explicit Parse/Regular; other forms unsupported | E external policy | P5G analysis policy, not arbitrary native option equivalence |
| Parse features / preview | Native feature map | No arbitrary features | Version-specific capabilities | Not generic serialization | Reject; no feature emulation | O/V | Original stable P5G only; Historical APIs stay child-local |
| Nullable annotations / flow state | P5G nullable mode | Enable only | B11 witness changes annotations but not full explicit-throw flow | Yes, not proved independent gap | Retain Enable restriction | O/I | Never turn annotations into invented value facts; general binding limits remain |
| Unsafe / overflow | P5G options | False only | Can affect diagnostics/constant folding; no closed flow witness here | Yes | Retain explicit false policy | O | No blind option transport |
| Output kind / platform / optimization | P5G options/P5K | DLL/AnyCPU/Debug | Admissibility/module shape; no demonstrated Debug-vs-Release flow gap | Yes | Retain existing restrictions | O/I | release-debug-plus still fails upstream; no emit reconstruction |
| Assembly/module/attributes | P3/P4/P5K source identity | B12 original unsigned identity postcondition | Exception identity and binding | Yes | Preserve B12 exact reference profile | E bounded | Original target receipt; actual Historical identity checked, signed targets OUT |
| Comparer / portability / resolvers / warning maps / emit flags | P5G restricted subset / native options | Default bounded policies | Some affect diagnostics; not independent proven flow transport gap | Partly | No resolver/file/diagnostic expansion | E profile / O general | Different compiler diagnostics remain deliberate; no defaults for new envelope |
| PE/PDB compiler provenance | Original P4/P5A/P5G | B7/B10 original Main-local receipts | Trust and engine selection | Existing | Preserve selection / receipts | E | No second PE/PDB parser, no B6 constants duplicated |
| Root selector / signature / generic call context | Exact Current source symbol | Static parameterless nongeneric method only | Target ownership and graph root | Bounded selector yes | Retain exact original PE binding and source ownership | E bounded / O general | P4 target module/MVID; Worker resolves exact owned method; no overload guessing |
| Declared types / project closures / supporting source registry | Main project+solution scopes and exact supporting resolver | One scope, local methods only | Cross-project/source callees and dispatch candidates | Requires dependency graph/receipt model | Separate scope package | O | Missing scopes cannot be filled with ambient references or Current results |
| Analysis mode | Four Current modes and guards | SolutionTransitive single scope only | Controls summary traversal / declared contracts | Name yes, full scopes not yet | Preserve explicit supported mode; reject others | E bounded / O other modes | No new mode semantics; missing/contradictory new context rejected |
| Session lifetime / cache / graph | Sequential Current session across roots | Fresh process, one root | Repeat and relevant summaries must match; no multi-root claim | Operational, not cache serialization | Full per-root graph/repeat proof | V | Unchanged shared analyzer/session; no session state transport |
| RuntimeAwait / error symbols / completeness / budgets | Actual compiler host and shared guards | Historical capability; errors/uncertainty fail closed | Deliberate different success domain | Not invented Current facts | Preserve typed failures | V | Same logical input is not promise of same compiler behavior |
| Reporting finding policy / target/file context | Detector and ToolRunner | Canonical result only | Reporting downstream, not compiler input | Separate import contract | Full Current findings retained; no Historical import | O | Cross-engine full Findings cannot be claimed without excluded Reporting Import |

## Scope decision

Close only the proven single-source parse family (concrete language + defines),
using an explicit original P5G configuration opt-in alongside the B12 original
reference set. Legacy B10/B11/B12 requests remain their published bounded policy;
there is no implicit expansion or silent conversion of their reserved fields.
The new parse envelope must carry all required fields and match the outer logical
language. Compilation options remain the existing explicitly restricted policy.

| Mode | Actual Current scope | Transport / Historical | B13 decision |
| --- | --- | --- | --- |
| Direct | Root semantic model, direct collector; no transitive summary closure | Only mode name slot; Worker does not invoke this mode | Unsupported, no substitution |
| ProjectTransitive | Project compilation trees and same-project callable summaries | Single-tree local scope only; no project membership envelope | Unsupported; separate scope reconstruction |
| SolutionTransitive | Project closure, supporting sources, dispatch candidates across scopes | Existing one-compilation/local-method host | Equal only within single-source complete local graph; full solution scope open |
| ProjectTransitiveDeclaredExceptions | Project transitive scope plus declared XML exception contracts | No declared-contract/provider context | Unsupported; XML remains excluded |

The historical host explicitly requires one tree and has no external supporting
scope. Main enumerates project closure plus SupportingSources.GetAnalysisScopes
and resolves exact methods into those scopes. Full closure therefore needs a
trusted ordered multi-tree/supporting-compilation dependency model and host
ownership/resolution integration, not just another mode string. B11's executed
multi-tree missing-callee witness establishes relevance. Do not change the shared
121 analyzer sources or the Historical host in this bounded slice.

## Implemented bounded closure

Four production files: WorkerProtocol.cs, HistoricalAnalysis.cs,
HistoricalWorkerPayloadProjection.cs and new HistoricalWorkerParseProjection.cs.
The existing projection input gains an optional **original** P5G configuration,
not raw caller strings or arbitrary Current parse/compilation objects to serialize.
Reference identity is still the unchanged B12 original P5C/P5E path. The same P5I
configuration instance and P5A origin receipt are mandatory; copied or foreign
configurations/provenance cannot mint trust. The original PE method/module/MVID,
source document and bounded selector checks remain unchanged.

The new v2 additive `ParseProfile=validated-parse-v1` and `WorkerParseContext`
carry concrete numeric language, ordered symbol strings, DocumentationMode=parse
and SourceKind=regular. All fields are required in that explicit profile, as is
the logical path and the B12 exact-reference world. No new resolver, feature map,
byte decoder, PE/PDB parser, reference validator, engine selection or compiler
version constant. Old strict peers reject the additive fields; no downgrade or
automatic cross-peer compatibility is claimed. Old B10-B12 fixtures and their
reserved PreprocessorSymbols/CompilerOptions fields remain unchanged/rejected.

Roslyn's display spelling `12.0` maps losslessly to the same concrete version's
canonical major spelling `12` (also `10.0` to `10`). This is notation, not language
downgrade or compiler behavior normalization. Stable minor versions 7.1/7.2/7.3
keep their minor component. P5G does not admit default/latest/preview, and the
projection explicitly rejects these and feature flags. The neutral layer checks
numeric syntax only; the actual loaded Historical LanguageVersionFacts and enum
membership decide capability **inside the child**. Unavailable concrete version
999 is an executed synthetic capability-negative, not a claim that a known
Current-only feature/version has been emulated. Genuine C#10/C#12 are tested.

Symbols preserve order; at most 128, each at most 256 characters, unique and
within the explicit letter/digit/underscore identifier subset. `true`/`false`
are not symbols. Other valid native identifier spellings outside this subset,
duplicates, arbitrary feature flags, Script/Diagnose/None are typed unsupported,
not normalized. The single-source, unsigned, one-module DLL/AnyCPU/Debug,
Nullable Enable, unsafe=false, overflow=false compilation policy is unchanged.
Do not claim unrestricted nullable, native compilation-option or emit equivalence.

Historical creates its own parse options with the supplied concrete language and
defines, then invokes the unchanged same local Summary engine. Source text/path,
existing source hash, selector and original unsigned identity remain exact.
The existing reference-world hash now also binds the explicit parse envelope;
response validation already checks the entire expected hash and language. A
different symbol set cannot masquerade as the same active-program response.

## Differential and negative evidence

The unchanged archived HEAD ran its 314-test permanent boundary gate, the eight
complete Current baseline cases and the actual B11 input-gap witness. The first
baseline filter used a nonexistent witness name and correctly selected only the
eight cases; a separate corrected nine-case run executed the real witness. Both
TRX files are retained. Parent evidence actually demonstrates defines changing
active exception bodies, C#10 raw strings failing CS8936, and a throwing second
tree missing from the Worker. The nullable witness changes annotations but not
the complete tested explicit-throw flow. No B11 tests were rewritten to erase gaps.

Six positive cases use zero/one/two defines under C#10 and C#12. The two-symbol
input deliberately uses SECOND,FIRST order. Each emits genuine Historical PE/PDB,
passes the original P4/P5A/P5G/P5H/P5I and P5C/P5E handoffs, then invokes the real
detector/dispatch/selection/router/Worker path twice. Compare **entire** canonical
results: exception assembly/module identities, every source/flow step, positions,
evidence kinds, uncertainty/truncation, exact target and logical source ownership.
The active no-throw, helper-throw and direct-throw bodies are genuinely different.
Original validated parse display/defines/features are separately recorded.

The actual isolated Historical diagnostic fixture (not Main dynamic loading)
observes every canonical graph summary including callable/context/body/sources/
edges/catches/uncertainty and repeats. Every tested summary and full result match
Current without semantic normalization. The diagnostic operation neither executes
submitted code nor enlarges the production graph/Reporting protocol. This proves
the tested per-root local graph, not a complete multi-project/multi-root closure.

The raw-string differential gives the same relevant CS8936 diagnostic family on
C#10 and a complete equal flow on C#12; there is no language fallback. Missing
parse/profile/language/symbols/scope/path, invalid or conflicting language,
latest, duplicate/invalid/reserved/over-budget symbols, conflicting compiler
options, extra trees and other modes fail before Worker start. Copied/foreign
P5G and copied P5A receipts fail in projection; missing exact references fails
rather than substituting framework references. Actual Historical-unavailable
concrete language yields typed UnsupportedContext and no result. Forged parse
world response, unknown fields and duplicate JSON fields fail closed. Existing
B4-B12 deadline/cancellation/start/crash/structured failures remain regression
coverage; no Current fallback or altered completeness/error-symbol policy.

Complete native Current Findings are retained and repeated for positive cases;
the throwing fixtures carry only a source `<summary>` and assert non-empty full
Current Findings under the explicit same SolutionTransitive mode. This strengthens
the test observation, not XML exception-contract transport or Reporting Import.
all eight existing full Current Finding/graph cases are byte-compared against the
fresh unchanged HEAD baseline. **Cross-engine complete Findings are not claimed:**
Historical Reporting Import is expressly excluded by this prompt and does not
exist at this boundary. The full differential here is canonical analysis plus
source/uncertainty/graph evidence, not a fabricated Historical Finding model.

## Scope follow-up / exact remaining limits

The four-mode table above is the explicit scope result. Only SolutionTransitive
inside the complete single-source local graph is admitted; Direct,
ProjectTransitive and ProjectTransitiveDeclaredExceptions are rejected in both
neutral input and real production projection, never substituted. Complete ordered
source/generated output, project membership, dependency topology, supporting
source receipts/ownership and exact full callable binding need a separate trusted
context/host package. The existing one-tree Historical host and 121 shared analyzer
sources are untouched. The actual second-tree witness and Main registry code
justify the architectural boundary, not merely an absence of a protocol field.

Other compiler policies, signed targets, unsupported reference forms and XML
contracts remain open. Reporting Import, CLI/acquisition/discovery and repository
call-site adoption remain separate; no Dapper pipeline redesign or new pipeline
success stage is claimed.

## Validation and closure

Final results were actually executed in a clean Git archive with only eight owned
build inputs overlaid. No tracked bin/obj/artifacts exist in the archived tool
tree; original workspace Core outputs were never built or used. No remote or
Linux execution is claimed.

| Final validation | Passed |
| --- | --- |
| Fresh unchanged parent boundary gate | 314/314 |
| Fresh parent full Current baselines | 8/8 |
| Corrected parent baselines + existing gap witness | 9/9 |
| New B13 | 34/34 |
| Unchanged B12 | 29/29 |
| Unchanged B11 | 16/16 |
| Unchanged B10 | 34/34 |
| Unchanged B5-B9 | 156/156 |
| Productive regressions | 681/681 |
| Focused regressions | 558/558 |
| Broad, consciously unchanged-code Blame diagnostic | 2859/2859 |
| Full final configured suite, Blame diagnostic | 2950/2950 |
| Final permanent boundary gate | 348/348 |
| Eight complete Current Finding/graph files | Byte-identical |
| Dual-Version / Current-Historical-Worker WAE / runtime isolation | PASS |
| Format / JSON / UTF8-CRLF / PS syntax / Git whitespace | PASS |
| Strict context evidence verifier / repository protection | PASS |

Groups overlap and are not summed. Existing configured runsettings are unchanged;
SelfAnalysis remains its separately excluded category. Final runs have zero
failed/skipped tests. Productive coverage is unchanged; focused/broad/full grow
by exactly 34 B13 cases. The verifier compares every B12 original regression
test name against the new groups and proves zero omissions. All 121 shared
analyzer sources and the one Historical host are unchanged. All eleven gate
build logs have zero warnings/errors; six library build images, two deterministic
Worker build images and exact loaded compiler/runtime identities are checked.

## Retained failures and unchanged repetitions

- First new build failed CS0051 because a public test signature exposed an
  internal enum. Only the new test signature was corrected.
- Second new boundary run: 339 passed / 6 failed / 345 total. New major-version
  display mapping rejected Roslyn's concrete 10.0/12.0 spelling. A diagnostic
  repeat (0/6) with unchanged behavior recorded the original parse values, then
  only the new mapper's lossless .0 notation conversion was corrected.
- Intermediate boundary 345/345 passed; the three required-path/reserved-symbol
  negatives gave 348/348. First complete regression sweep passed all groups,
  including broad 2859 and full 2950. That entire pass is retained separately.
- The new test fixture was then strengthened with summary-only source trivia and
  non-empty Current Finding assertions; no production change or XML contract.
  Boundary 348/348 passed again, and all regression groups were consciously
  repeated against the final test source.
- Final broad sweep: 2858 passed / 1 failed / 2859 total in unchanged
  ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed, line
  446 (ContainsMetadata). Separate unchanged method diagnostic passed 1/1.
  First conscious broad retry: 2858/2859, instead failing unchanged
  ResourceOnlyAssemblyFile_DoesNotTriggerMultiModuleRejection, line 464.
  Both use ReadAssemblyFiles, which returns reader-backed AssemblyFile values
  after its using PEReader/stream are disposed (lines 663-676). This is a plausible
  existing lifetime explanation, not independently proven causality and NOT a
  proven cause of B11's clrjit.dll 0xc0000409 native crash. Test source fingerprint
  is unchanged. No repair, exclusion or error suppression. Both failed complete
  TRX/logs are retained. Later consciously unchanged-code --blame broad run passed
  2859/2859; full final --blame suite passed 2950/2950. No native testhost abort in
  B13. The inherited native-crash boundary remains unresolved, not fixed.
- Two new Proof verifier failures were tooling-only: slash/backslash comparison
  of the recorded export location, then PowerShell 5's nested JSON-array wrapper
  in the owned-input allowlist. Corrected exact filesystem-path comparison and
  required eight-element allowlist; no protection check was removed. Third proof
  run passed. Initial wrong witness filter/artifact filename, Windows rg wildcard
  operands, two new-test whitespace diagnostics and one artifact-metadata PS
  hash-literal typo are separately recorded in attempts.json; no foreign edits.

Retained local roots: artifacts/p5o2b13 (initial snapshots/before witness/attempt
index/verifier logs), artifacts/b13-35d69/parent-dual and parent-current,
attempt-1-dual through attempt-4-dual, first-regression-pass, failed-final-broad,
failed-manual-broad, and the export's artifacts/p5o2b13 tests/logs/full runtime
JSON. These are ignored runtime artifacts, not product bin/obj changes. The
machine-readable audit includes exact artifact hashes and final counter checks;
the verifier rejects stale pre-final-gate TRX data.

## Changed files / final repository audit

Production: WorkerProtocol.cs, HistoricalAnalysis.cs,
HistoricalWorkerPayloadProjection.cs, new HistoricalWorkerParseProjection.cs.
Test-only fixture: HistoricalPdbFixture/Program.cs and B12ReferenceProbe.cs (old
branches retain their default behavior); new ExceptionFlowHistoricalSourceContextTests.cs.
Gate: Verify-DualVersionBuild.ps1 adds only the new B13 class.
Evidence: this report, its -audit.json, P5O2B13Proof/Run-Regressions.ps1,
P5O2B13Proof/Write-Verify-ContextEvidence.ps1 and OPEN-PIPELINE-BOUNDARIES.md.

Starting and final HEAD remain `35d69be2a4f50c2f1ae6cde7a7f2452212a6b274` on
fix/documentation. The external B12 commit was audited before B13; there was no
HEAD movement during B13. Root ignore bytes, all 23 Core files, all six stash
IDs/messages and the empty staged index match the initial snapshot. All other
tracked code/project/config/test inputs match their initial fingerprints. No
commit, push, reset, checkout/restore, stash or staging operation.

Final git status --short (root ignore is pre-existing protected WIP):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Evaluation/P5O2B7Proof/HistoricalPdbFixture/B12ReferenceProbe.cs
 M Evaluation/P5O2B7Proof/HistoricalPdbFixture/Program.cs
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
?? Evaluation/P5O2B13-historical-source-compiler-scope-equivalence-audit.json
?? Evaluation/P5O2B13-historical-source-compiler-scope-equivalence.md
?? Evaluation/P5O2B13Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerParseProjection.cs
```

Self Analysis 16 and Canonical Diff 0/0/0 are inherited from B11/B10, not newly
executed. This bounded B13 slice stops without implementing the larger scope,
signing or Reporting families.
