# P5O2B16 - Historical ProjectTransitiveDeclaredExceptions boundary

Starting HEAD: `eb7fb592dd7612c78a73b742c31b1e7c6668f162`, separately committed B15.
Initial Working Tree: protected root `.gitignore` only; empty index. All 896
tracked tool files, 23 Core files and six stashes fingerprinted before edits.
No applicable AGENTS.md. Builds/tests use a fresh Git archive, never original Core
bin/obj. No commit, push, staging or stash operation.

## Phase 1: source-backed decision BEFORE implementation

B14's executed `DeclaredMode_IsGenuineDetectorPolicyNotInventedWorkerTraversal`
is the starting witness: no reporting-scope exception type, normal transitive
helper throw/Finding, declared mode Direct with neither helper flow nor Finding.
Its original test and B15's declared-mode rejection remain unchanged.

`ExceptionAnalysisMode.cs` defines Declared as direct exceptions unrestricted,
transitive documentation requirements restricted to exception **type definitions
in reporting source**. This is not a list of exceptions declared on methods.
CLI `ArgsParsing` selects the enum; ToolRunner/comparison options pass it to
`ProjectClosureSemanticContextBuilder` and `XmlDocExceptionSemanticDetector`.
The builder only adds recursive referenced projects for SolutionTransitive;
Declared and ProjectTransitive have the same reporting-project scope. Existing
supporting-source resolution is not newly disabled/enabled by Declared. The B16
scope is exactly one complete target project, not a new cross-project policy.

`ProjectClosureSemanticContext.HasDeclaredExceptionTypesInReportingScope`
examines SourceTypes of AnalysisTarget compilations, resolves System.Exception
and tests inheritance. It caches a boolean. `IsDeclaredInReportingScope` checks
the exception symbol's DeclaringSyntaxReferences against exact source-tree
ownership and the AnalysisTarget role; name/path equality is not declaration
trust. ReferencedProject/SupportingSourceDependency declarations do not qualify.
There is no exception-contract annotation read at a callee for this mode.

`ToolRunner.CreateExceptionFlowSummaryAnalysisSession` returns null for Direct
or Declared with no qualifying type. The productive detector's
`AnalyzeConfiguredExceptionFlow` then returns its existing Direct result in
those cases. Otherwise both ProjectTransitive and Declared use the SAME existing
summary session and native B9 dispatch. The convenience detector overload may
allocate a session even when guarded, but does not populate it. No new flow,
summary kind, graph edge or declaration provenance is introduced by Declared.

Reporting is separately mode-sensitive: IsRelevantDocumentedException and
IsRelevantThrownException restrict Declared transitive checks to types declared
in reporting scope. These feed DOC611 (missing transitive), DOC631 (uncertain
documented flow) and DOC632 (documented but not transitively thrown). Direct
DOC610 still uses ALL valid documented types and all direct throws. DOC660/670
remain independent. DOC632 is suppressed on uncertainty; DOC631 retains the
existing unknown-target/evidence handling. The raw graph/flows are NOT filtered
when a qualifying source type exists. A mere unused local exception declaration
can enable the graph while framework exceptions remain in the raw flow but do
not generate declared-mode transitive Findings.

Local XML tags are consumed by the ordinary Finding detector (cref resolution,
coverage/relevance, inheritdoc suppression), not to manufacture proven throws.
The shared ExternalDocumentationExceptionModel / ExternalDocumentationEvidence
handles external metadata documentation for all modes, preserving uncertainty;
that independent validated XML-provider gap is NOT what the word Declared means.
Existing source kinds are ProvenException / ExternalDocumentationEvidence;
canonical evidence kinds are Proven / ExternalDocumentation. There is no
Declared flow-source kind. Source/provenance remain executable throw/call paths.

Historical's shared analyzer has no ExceptionAnalysisMode parameter or declared
reporting policy. Its host intentionally has no ProjectClosureSemanticContext
or detector policy. The Worker calls Direct or the unchanged summary engine;
its response carries canonical flow/uncertainty and request/runtime provenance,
NOT Findings or a reporting decision. B11-B15 already carry exact complete
single-project source/parse/reference ownership; source text is not the missing
data. Adding an enum/boolean alone cannot execute the missing detector policy.

## Focused submatrix BEFORE implementation

| Information / behavior | ProjectTransitive | ProjectTransitiveDeclaredExceptions | Historical before B16 | B16 measure |
| --- | --- | --- | --- | --- |
| Explicit exception declaration | No type-declaration guard | Reporting source type deriving from System.Exception gates summaries | No reporting-type guard | Executed guard boundary witness, no alias |
| XML/source declaration information | Ordinary docs compare with inferred throws | Same docs; declared means source type, not XML throws | Source text/parse present; validated external XML snapshots open | Separate local-doc and external-contract boundaries |
| Method/symbol mapping | Existing bound root/callee resolver | Same resolver; type symbol syntax references used for relevance | Exact B11-B15 ownership already present | Reuse original B14 factories/probes unchanged |
| Declared versus inferred flow | Executable direct/transitive flow | No added declared flow; guarded Direct or identical raw summary flow | No Declared analyzer mode | Differential controls expose false mode interpretation |
| Sources/provenance | Existing throw/call/direct/summary sources | Same raw sources; reporting relevance is separate | Full canonical flow + bound IPC provenance | Full snapshots; never synthesize declared evidence |
| Summary representation | Existing shared session | Same session, or unpopulated graph when guarded | Shared unchanged analyzer, mode blind | Actual productive private seam observation in tests |
| Graph edges | Existing project edges | Same edges when gate true; no helper edges when false | Canonical summaries already supported | Compare full summaries, not counts alone |
| Uncertainty | Existing DOC631/632 policy | Same uncertainty, relevance restricted to declared types | Incomplete analysis fails closed | Current uncertain witness; retained Worker failure controls |
| Project scope | Selected reporting projects, not dependency closure | Same; exact AnalysisTarget determines declarations | Complete bounded single target project | No new DAG or cross-project Declared policy |
| Transitive interaction | All inferred types available for reporting | Raw types unchanged when enabled, transitive Findings filtered; Direct unrestricted | No reporting/filter contract or Findings response | Stop at detector/reporting boundary, not XML mega-change |

## B16 boundary decision

This is a **root-cause / boundary closure**, not Historical mode enablement.
Full equivalence requires a shared executable detector/reporting-policy boundary
and its reporting-scope/type membership result contract, followed by genuine
Historical Finding/reporting comparison. That infrastructure is absent and
Historical Findings import/reporting is expressly outside B16. Merely moving
the guard into the host, aliasing ProjectTransitive, filtering raw flows or
adding XML exceptions would duplicate or change Current semantics and omit the
second half of the mode. A smaller canonical guard-only slice is technically
possible after a shared policy extraction, but cannot honestly be called the
requested mode equivalence. No such partial success is presented here.

The requested "exclusively declared exception creates an additional flow" case
does not exist in Current for XML/type declaration alone. Boundary tests instead
prove actual differences (guard or Findings relevance), including mixed throws
and a same-project transitive chain. Positive Historical Declared differentials
are therefore **not available**, rather than fabricated by a test adapter.

No production payload/protocol/host/analyzer/routing/version change is planned.
The permanent boundary gate will include the new root-cause tests. B10-B15 tests
remain byte-identical. OPEN-PIPELINE-BOUNDARIES changes only to record this proven
cause and follow-up, never marking the mode resolved.

## Executed semantic witnesses and limitations

New tests invoke the ACTUAL private productive AnalyzeConfiguredExceptionFlow
seam, then the public detector; they do not copy the mode guard into a Native
test adapter. All root/helper compilations have no compiler errors. Each snapshot
retains complete native Findings, canonical flows/paths/sources/uncertainties,
every canonical summary and graph count, stable exact-tree ownership and repeat.
The isolated reporting-membership test also requires error-free compilations.

| Current witness | ProjectTransitive | Declared | What this proves |
| --- | --- | --- | --- |
| No local exception type; helper throws framework type | One helper flow, DOC611 | Direct: empty flow/graph, no Finding | B14's guard difference reproduced at actual productive seam |
| XML-only framework exception, no throw | Empty flow, DOC632 | Empty flow, no Finding | XML alone does not add a flow or activate summaries; Direct retains DOC630 |
| Unused local Exception subtype; framework helper throw | One flow, DOC611 | Identical full flow/graph, no Finding | Type declaration gates execution but does not filter raw flows |
| Direct framework throw plus inferred framework/local helper throws | Three flow types, DOC610 + two DOC611 | Same three flow types, DOC610 + local DOC611 | Direct remains unrestricted; reporting filtering is distinct; no duplicate local flow |
| Root -> A -> B with local XML tag but framework throw | Same-project call graph, DOC632(local) + DOC611(framework) | Same full graph/flow, DOC632(local) only | No XML-declared throw or special declared edge is invented |
| Unknown GC.Collect with local/framework XML tags | Unchanged raw uncertainty, two DOC631 | Same raw uncertainty/graph, local DOC631 only | Relevance is reporting policy; DOC632 stays suppressed |

For each of the five complete cases, the unchanged original B14 emitter/factories
produce genuine validated B11-B14 project inputs. Declared rejects automatically
at Main projection (UnsupportedProfile, no dispatch) and raw Client admission
(UnsupportedInput, no process/result). Direct/ProjectTransitive/SolutionTransitive
each run twice in genuine production Historical Workers: entire canonical result,
source/provenance, pinned runtime identity, independent isolated Historical graph
summaries/ownership and repetitions match native Current. Native full Findings
are retained for all modes; **no Historical Declared Findings parity** is claimed.

Ten explicit negatives cover missing/empty/unknown mode, Declared itself,
Declared with absent scope/source/parse, invalid parse, foreign ownership and
contradictory solution scope. Since Declared is unsupported, these combined
cases fail at admission; they are NOT evidence of a new Declared input validator.
Unchanged B14/B15 tests independently exercise source/ownership/parse validation
under supported modes. Actual supported Workers propagate UnsupportedContext
(language 999), CompilationFailure and AnalysisFailure (incomplete GC.Collect)
without downgrade/partial result/Current fallback. Children are checked exited.

The only missing logical-mode information is policy interpretation: the existing
string already has a spelling for Declared but rejects it. Complete single-project
sources/reference/parse ownership are already transported. There is no justified
new XML-exception list, boolean attestation, SourceSet, validation layer or DAG.
For this bounded target all its trees would be reporting source; general native
reporting-versus-supporting roles remain a separate policy contract. Executed
Current membership rejects supporting declarations even at the same logical path
and accepts a local source Exception subtype. Enum zero is legitimate Direct in
Current, not an invalid enum to redefine; the explicit unsupported/default-string
tests do not change that existing behavior.

## Validation and final closure

All final tests below were actually executed after the final test-source change
and final Dual gate. Groups overlap and are not additive unique counts.

| Actual final validation | Passed |
| --- | --- |
| Fresh unchanged B15 parent boundary and eight Current baselines | 407/407 |
| New B16 root-cause/boundary cases | 18/18 |
| B15 unchanged | 28/28 |
| B14 unchanged | 31/31 |
| B13 unchanged | 34/34 |
| B12 unchanged | 29/29 |
| B11 unchanged | 16/16 |
| B10 unchanged | 34/34 |
| B5-B9 unchanged | 156/156 |
| Productive | 681/681 |
| Focused | 635/635 |
| Broad, diagnostic Blame | 2936/2936 |
| Full configured suite, diagnostic Blame | 3027/3027 |
| Final permanent boundary gate | 425/425 |

Zero skipped/failed final tests. Eleven WAE build logs (three Current, three
Historical, two Worker, isolated boundary process, original Historical PDB
fixture, Main boundary tests) each have zero warnings/errors. Dual-Version,
exact runtime identities, deterministic build/runtime and isolation pass. All
six Current/Historical DLL+PDB images and both Worker build images are byteequal
to the fresh B15 parent, not merely source-equivalent. All 121 shared analyzer
sources, host, routing/selection, versions and B10-B15 test bytes are unchanged.
Eight whole Current baseline JSON files are byteequal, including Findings,
graph counts, repeat/session lifetime, all four modes with/without a local type.

Format is folder-only on the new test file (no original Core MSBuild); strict
UTF8/no-BOM/CRLF, JSON, PS syntax, Git whitespace, tracked-path case collision,
architecture/isolation and source/export fingerprints are checked by the evidence
script. The corrected strict evidence verifier passed; final report/status
fingerprints are rebound by a final repeat and recorded in the audit JSON.
Actual execution is Windows in a clean Git archive plus exactly two owned build
inputs (new tests and appended boundary filter). No Linux/remote CI run is claimed.
There are zero tracked path case collisions. No Historical assembly was loaded
in Main, no submitted source executed, no own Worker/fixture remained at audit.

The initial NU1900 restore failure and the permission-enabled repeat are retained
in separate attempt directories. A .NET-8 unsupported --no-http-cache attempt
was corrected to --force --no-cache -warnaserror; that restore succeeded with
security auditing enabled, no warning suppression or package/version change.
The first new B16 run passed 17/18: its field-throw DOC610 test assumption was
wrong. Only that new fixture was changed to an explicit ArgumentException
creation; targeted repeat passed, then the complete gate/regressions passed.
The new ownership test was subsequently made self-contained with explicit zero
compiler-error checks. The entire first passing regression/gate/evidence set was
copied before final revalidation. Both broad/full sets passed without the known
reader-lifetime native crash. No old test was repaired/excluded, no automatic
retry or fail-open result. All attempts and actual TRX/log paths are in the JSON.

The first evidence-write attempt exposed PS5 Get-Content string note properties
(PSDrive/PSProvider): deep JSON serialization started traversing provider metadata
with growing memory, before creating the audit file. Only the own verifier PID
32080, confirmed by its exact command line/export, was stopped. Plain string
casts on the ten admitted matrix rows remove those incidental properties; the
corrected verifier passed immediately. No test/build rerun or product/test-source
change was needed for this proof-only correction. The failed attempt and narrowly
verified cleanup are retained in the attempt index, not hidden as a first-time pass.

Self Analysis 16 and Canonical Finding Diff 0 added / 0 removed / 0 changed-evidence
are **inherited from B15/B14/B13/B11/B10, not newly executed**. The complete suite
uses its unchanged default Category!=SelfAnalysis runsettings; no configured test
was skipped. No fresh Self Analysis or canonical diff is asserted.

## Required 20-point closure

1. Exact Current semantics: reporting-source exception-type guard plus separate
   transitive Finding relevance, documented in Phase 1 and executed witnesses.
2. Difference to ProjectTransitive: guarded Direct when no qualifying type;
   otherwise identical raw summary analysis but different DOC611/631/632 relevance.
3. Submatrix: ten focused rows written before implementation, retained above.
4. Missing Historical information: executable detector/reporting-policy contract,
   not missing source text or a method-level XML-declared exception list.
5. Closed: semantic ambiguity/root cause and the unsupported boundary; genuine
   unchanged three-mode controls. **Historical Declared enablement is not closed.**
6. Open: shared declared-reporting policy and actual Historical Finding equivalence.
7. Protocol/payload: zero changes; new string alias/boolean cannot solve the policy.
8. Declared dataflow: CLI enum -> reporting project context -> type gate -> Direct
   or existing session -> existing Finding relevance; no added declared flow kind.
9. Differentials: five real Current policy differences, one raw-uncertainty/reporting
   difference; 15 unchanged-mode genuine Historical full flow/graph/repeat controls.
   No fabricated positive Historical Declared result or complete Finding parity.
10. Direct/Project/Solution controls remain exact; Direct retains framework DOC610,
    no newly globalized declaration filter or SourceSet/DAG policy.
11. Negatives: ten explicit admission failures, original validated automatic
    projection rejection for all five cases, actual supported-worker error controls.
12. XML: local docs affect existing reporting; they do not turn type declarations
    into throws. Validated external XML providers/sidecars remain independent.
13. Isolation: real pinned child compiler, Main never loads Historical, original
    neutral protocol, successful/error-child cleanup, no Current fallback.
14. Current parity: eight complete native byte baselines and all production
    source/DLL/PDB bytes unchanged; no session-lifetime or mode change.
15. Tests/builds: final census above, all after final source change, eleven 0/0 WAE
    steps, Dual-Version PASS; failed/earlier attempts retained separately.
16. Gates: permanent filter includes B16; exact build/runtime identities,
    architecture, source protection, encoding/format and strict evidence verifier.
17. Self Analysis / Canonical Diff: 16 / 0/0/0 inherited, neither newly executed.
18. Remaining boundaries: Historical reporting/CLI adoption, signed/other unsupported
    references, external XML contracts, broader scopes/roots/compiler options. No
    source discovery, routing/version expansion or Dapper redesign in this package.
19. Final Git status: below. Seven owned files; protected root ignore is not owned.
20. External HEAD: B15 committed separately before B16 to eb7fb592dd; no movement
    during B16. Initial and final HEAD/branch, empty index, root-ignore hash,
    23 Core fingerprints and six stash hashes checked. No commit/push/stash action.

## Owned files and reproduction

New test: Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs.
Build-only change: build/Verify-DualVersionBuild.ps1 appends that class's filter.
Documentation-only change: Evaluation/OPEN-PIPELINE-BOUNDARIES.md records the still
open mode with its proven detector/reporting cause. New report/audit JSON and two
Evaluation/P5O2B16Proof scripts. No production project/source changes.

Protected snapshot/export/attempt index: artifacts/p5o2b16. Clean build:
artifacts/b16-eb7fb/Tools/XMLDocNormalizer. That export holds the actual Dual
logs/audit/TRX, all twelve final regression groups, full runtime snapshots and
original emissions. Parent-dual/parent-current and both failed parent restores
are retained alongside attempt-before-ownership-hardening. Product evidence JSON
embeds full differentials, graph strings, ownership, uncertainties and tests,
plus artifact fingerprints and precise non-equivalence/remaining-boundary claims.

Reproduce: use a clean Git archive with the two recorded owned build inputs;
run Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences,
then P5O2B16Proof/Run-Regressions.ps1 -ValidationRoot <export-tool-root>, then
P5O2B16Proof/Write-Verify-DeclaredBoundaryEvidence.ps1 with that same root. The
verifier intentionally requires the recorded protected-before/parent baselines;
it checks freshness and exact built/source bytes, not prior totals alone.

Next bounded package must first define/extract ONE shared executable Current
guard/relevance policy and a truthful reporting-scope/type identity contract,
then establish genuine Historical reporting decisions/Findings comparison in an
explicitly authorized slice. Do not alias ProjectTransitive, filter raw flows,
transport guessed declaration booleans or close the broader XML/CLI family by
assertion. B16 stops here under its infrastructure boundary rule.

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B16-historical-declared-exceptions-boundary-audit.json
?? Evaluation/P5O2B16-historical-declared-exceptions-boundary.md
?? Evaluation/P5O2B16Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs
```
