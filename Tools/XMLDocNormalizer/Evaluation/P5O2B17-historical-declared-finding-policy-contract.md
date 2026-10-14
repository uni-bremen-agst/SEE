# P5O2B17 - Historical Declared-Exception Finding Policy Contract

Starting HEAD `839fcb03a6a313e031a859811d1f7faeaf333686`: B16 separately committed.
Initial tree: protected root .gitignore only; no applicable AGENTS.md. Protection
snapshot covers all tracked tool files, 23 Core files, six stashes and empty index.
Builds/tests will use a clean HEAD archive; original Core bin/obj stay untouched.
No commit, push, index or stash operation.

## Phase 1 - written dataflow BEFORE implementation

Current CLI enum -> reporting-project context -> ToolRunner session guard ->
detector AnalyzeConfiguredExceptionFlow -> Direct result OR caller-owned shared
summary session -> independent detector predicates -> Finding construction.

HasDeclaredExceptionTypesInReportingScope scans AnalysisTarget.SourceTypes for a
System.Exception subtype (source type definitions, including unused ones), not
XML exception annotations or observed throws. It caches that boolean. No local
subtype means no productive summary session in ToolRunner and the existing Direct
result in the detector. Otherwise ProjectTransitive and Declared use the same raw
session/resolver/graph. Project context contains reporting projects, not recursive
ProjectReference closure (only SolutionTransitive adds that closure). Registered
supporting scopes are not reporting scopes. B17 admits ONE complete target project.

IsDeclaredInReportingScope checks a type symbol's DeclaringSyntaxReferences using
exact source-tree ownership and AnalysisTarget role, not a path/name/assembly-only
heuristic. Assembly/module identity disambiguates neutral type identity but is not
alone proof of a declaration. The existing detector first verifies exception
inheritance and valid tags. IsRelevantThrownException and
IsRelevantDocumentedException then restrict Declared transitive checks to source
exception types in that reporting scope.

DOC610 uses the independent Direct collector, all valid documented types and
ordinary inheritance coverage; there is NO local-type restriction. DOC611 first
excludes each exception TYPE already present in Direct. It then requires inferred
transitive presence and local reporting declaration (and lack of covering relevant
documentation). Thus several direct/summary paths for one type do not generate
both DOC610 and DOC611. Direct membership must be collected independently: path
step kind or the existence of a summary throw is not a substitute.

DOC631/DOC632 apply local-type relevance to documented types. DOC631 additionally
requires uncertainty and absence of proven covering throws; external documentation
is supplemental evidence only. DOC632 is suppressed by uncertainty and by known
covering throws. No local XML annotation creates a proven throw. Invalid cref,
non-exception tags, inheritdoc suppression, inheritance coverage, anchors, snippets
and Finding rendering remain existing detector responsibilities, not Worker policy.

Decision granularity is exception type and Finding purpose at a source-owned
member. All its flow paths/sources remain intact. ProvenException versus external
XML evidence and Direct versus Summary membership are distinct axes. The currently
admitted Historical success domain is Proven, complete, untruncated flows; uncertain
or external-contract cases still fail closed, not a partial reporting decision.

Historical already supplies canonical exception/assembly/module identities, full
flow paths/source positions, complete B14 source ownership, B15 project bindings,
source-owned root selector and response/input/runtime provenance. It does NOT
supply a complete source-defined exception inventory or the independent Direct
result alongside ProjectTransitive. Result-only localness inference would omit
unused local definitions and cannot implement the Current guard. Assembly name
guessing cannot prove source declaration or distinguish a malformed type.

## B17 submatrix BEFORE implementation

| Information | Current available | Historical available | Needed for Finding policy | B17 measure |
| --- | --- | --- | --- | --- |
| Exception Type Identity | Bound exception symbols | Full canonical type identity | Exact per-type membership, never display names | Reuse canonical identities; reject missing/unknown facts |
| Local/external type definition | DeclaringSyntaxReferences and reporting role | Not a complete declaration inventory | Guard and transitive/documented relevance | Complete bounded source-exception declaration facts |
| Assembly Identity | Full bound symbol identity | B12 target/reference identities | Disambiguation and source-owner validation | Reuse exact identities; no simple-name classification |
| Module Identity | Bound source/reference module | Canonical module plus B12 PE MVID provenance | Prevent foreign type ownership | Existing context/response checks, source module ordinal 0 |
| Source Ownership | Exact trees in AnalysisTarget | Full ordered B14 single-project scope | Prove every reported type declaration is owned | Exact ordered scope and declaration-path membership |
| Method Ownership | Bound root/member with tree | B14 exact root selector/tree plus provenance | Bind direct and summary facts to the same member | Reuse existing root/postconditions; no duplicate root DTO |
| Flow Source Kind | Proven versus external documentation | Canonical evidence kind and complete paths | Accept only the admitted proven success domain | Validate source purpose separately; no XML-contract widening |
| Direct/Summary provenance | Independent collector and summary result | One selected result | DOC610; type-level DOC611 exclusion for combined sources | Opt-in independent Direct result; retain raw summary result |
| Uncertainty | Raw uncertainty plus DOC631/632 handling | Incomplete analysis rejects | Avoid fabricated definite decisions | Retain fail-closed Worker boundary; explicit open uncertain reporting |
| Target/Finding context | Docs, cref resolution, coverage, anchors, inheritance | Root/source provenance, not native Finding metadata | Eligibility versus actual emission | Return eligibility only; no Finding/import payload |
| Analysis Mode | Explicit enum plus reporting guard | Existing admitted engine modes; Declared rejects | No alias/default/fallback | Versioned explicit policy-facts request, not mode enablement |

## Contract decision BEFORE implementation

Direction: Historical binding/unchanged analyzers -> neutral facts -> Main-side
declared Finding eligibility. Worker must not decide Finding relevance or emit
Findings. Current detector/shared 121 analyzer files and B5/B6 routing stay unchanged.

An additive opt-in version-1 single-project policy-facts request accompanies an
existing ProjectTransitive engine request. It is NOT a mapping of Declared to that
mode: the returned raw analysis is labelled ProjectTransitive and no productive
Declared route is enabled. Other modes, legacy requests and B15 DAGs retain their
exact published behavior. Explicit complete B12/B13/B14 inputs are required.

Smallest missing facts: complete bounded local Exception-subtype declarations
(canonical type plus exact declaration source paths, explicit inventory count),
their reporting source manifest and an independent full Direct collector result.
Existing response provenance binds request, root, source/parse/reference world and
policy opt-in. All policy paths/types/direct data are validated before a sealed
Main receipt can be consumed. Missing/conflicting inventory, mode, ownership or
unknown type/source-purpose facts yield typed failure, never unknown -> relevant.

Main eligibility is scoped to known exception types in that declaration inventory
or either proven result: Direct type membership is unrestricted; Summary needs the
actual local-type guard, localness, summary membership and no Direct membership;
documented-type relevance needs source declaration. This closes those predicates,
not document coverage or general Finding rendering. External documented types
absent from both results/inventory have no attested type fact and reject rather
than invent metadata/type evidence. Generic declaration shapes are explicitly
outside this bounded v1 slice; no lossy generic identity normalization is planned.

Historical Declared stays unsupported. B18 needs explicit guarded execution and
complete native Finding comparison/remaining document context, separately authorized.

Completeness refinement before final transport implementation: a response count
alone cannot reject a removed UNUSED local type if a forged response also edits
that count. The existing retained P4-bound Root.ContainingAssembly therefore supplies
the exact original PE exception-definition identities to the opt-in request. No
binary is reopened, no compilation/reference/source validation is repeated. Worker
binds those exact definitions to actual owned source declarations; response validation
requires set equality, including unused types. Main projection rejects caller-supplied
expected declarations. This is necessary completeness binding, not a type-name
heuristic or a second analyzer. Generic/ambiguous-base/over-budget forms reject.

## Execution and closure

Implemented the bounded contract, not productive Declared mode. The facts request
contains Profile, explicit nullable policy Mode and expected declaration identities
derived from the existing retained PE symbol world. Caller-supplied inventories,
copied/missing original P5J receipts, other modes and solution graphs reject before
dispatch. Source/parse/reference validation and B5/B6 selection/routing are reused.

Worker facts use existing ExceptionFlowSemanticScope.SourceTypes, SymbolUtils
inheritance and RoslynCanonicalIdentityFactory. This is symbol fact projection,
not a second analyzer or a relevance policy. Exact nongeneric exception definitions
must match the original PE inventory; every declaration belongs to actual owned
trees. Partial declarations retain manifest-ordered paths, nested declarations
retain structured containing-type identity. Unused declarations are NOT omitted.
Direct is independently collected by the unchanged shared Direct collector.
The top result remains unfiltered, genuinely labelled ProjectTransitive.

Outer protocol v2 is unchanged; optional null fields are omitted exactly as before.
The opt-in profile is versioned single-project-declared-policy-v1, at most 128
exception definitions, with existing source/reference/syntax/request/1-MiB response
budgets unchanged. Missing/default/unknown policy mode or incomplete inventory
cannot downgrade. Old strict peers reject new fields, not a legacy fallback.
The existing ReferenceWorldSha256 now also binds the opt-in and original definitions;
ordinary no-opt-in hashes/serialization are unchanged.

Response validation reuses canonical identity/source/reference checks for the
independent Direct result, requires root-only Direct locations, complete ordered
reporting manifest, canonical declaration order, exact target assembly/module,
unique declarations, valid owner paths and equality with original PE definitions.
Every source-assembly exception in either result must have a declared-type fact.
Generic/incomplete/uncertain/external-contract evidence is not a successful policy
result. Failure envelopes cannot carry policy data; unsolicited/missing policy
responses reject. No second PE/PDB/ref/source validator or compiler reconstruction.

Only the original successfully validated call obtains a weak validation receipt.
It snapshots canonical type sets immutably. Record clones, failure/legacy calls and
raw DTO fabrication cannot mint it; mutation of returned DTO arrays cannot mutate
its decision facts. Main DeclaredExceptionFindingPolicy takes explicit policy mode,
known canonical type and Direct/Summary/Documented purpose; no Roslyn objects.

| Purpose | Main eligibility over validated facts | Existing Current responsibility not imported |
| --- | --- | --- |
| Direct / DOC610 | Type in independent Direct set, local OR external | Valid XML coverage, anchor/context/rendering |
| Summary / DOC611 | Complete local inventory nonempty; type in Summary, in local inventory, NOT in Direct | Relevant XML coverage, anchor/context/rendering |
| Documented / DOC631-632 relevance | Known exception type in local inventory | Valid cref, uncertainty/coverage rules, actual document and positions |

There is no raw-flow filter or inferred guard. A type with both Direct and Summary
paths remains in full flows, but only Direct documentation eligibility applies.
Unknown type/provenance/mode returns typed failure, not false-success or relevant.

## Executed differential design

Seven cases: local exception plus framework counterflow; external-only with no
local guard; mixed direct framework plus summary local/framework; one local type
with both Direct and Summary sources plus framework summary; unused local type
with actual DOC632 relevance; two-tree partial declaration; nested local exception.
Every case has a real Current finding difference, not only a finding count.

Native snapshots retain whole Current exception-detector Findings, full direct and
summary canonical flows/sources/uncertainties, every summary, graph state and repeat.
The exact existing private IsRelevantThrownException predicate is invoked; Current
actual DOC610/DOC611 presence is compared with every corresponding Main decision.
The unused-declaration case also asserts actual DOC632. Documented eligibility is
type relevance after valid-exception prerequisites, not an assertion that a tag or
full Finding exists for each query. Neither general Finding import nor coverage is
implemented or claimed. Whole native Findings/repeat are retained separately.

Genuine original B14 PE/PDB/P5G/P5I/P5J/P5C/P5E handoffs automatically project the
opt-in through unchanged production B9/B8/B7/B6/B5/B4 dispatch. Two real Historical
processes per case return equal full facts/flows and pinned runtime identities;
the unchanged isolated B14 observer compares all graphs and stable/foreign-rejected
source ownership. Exact declaration identities from the original PE, child source
binding, actual Direct result, per-purpose local/external decision and repeat match.
Native Current baselines additionally protect all four existing modes and session
lifetime/no-Worker behavior. Productive Declared remains explicitly rejected.

Sixteen response negatives cover absent policy/type/count/source/manifest/direct,
conflicting full assembly/module, foreign source/direct paths, duplicate types,
unknown evidence strength, missing/default mode and profile. Removing an UNUSED
definition and recomputing its count still fails the original PE identity binding.
Query controls reject missing type, null/default/invalid mode, unknown purpose/type,
legacy call, copied receipt. Request controls reject missing metadata/scope, caller
inventories, solution/other mode, copied receipts and generic definitions. Actual
child inventory mismatch is UnsupportedContext; unknown GC.Collect is AnalysisFailure
with native uncertainty retained and NO partial result/receipt. No Current fallback.

## Validation

All final groups were actually run AFTER the final exact source overlay and gate.
Zero failed/skipped configured tests. Groups overlap, not additive unique counts.

| Final validation | Passed |
| --- | --- |
| Fresh B16 parent boundary and eight Current byte baselines | 425/425 |
| B17 | 25/25 |
| B16 unchanged | 18/18 |
| B15 unchanged | 28/28 |
| B14 unchanged | 31/31 |
| B13 unchanged | 34/34 |
| B12 unchanged | 29/29 |
| B11 unchanged | 16/16 |
| B10 unchanged | 34/34 |
| B5-B9 unchanged | 156/156 |
| Productive | 681/681 |
| Focused | 660/660 |
| Broad, diagnostic Blame | 2961/2961 |
| Full configured suite, diagnostic Blame | 3052/3052 |
| Permanent boundary gate | 450/450 |

Dual-Version/runtime/isolation PASS. Eleven WAE build logs each have zero warnings
and errors: three Current, three Historical, two Worker, isolated test process,
genuine Historical PDB fixture, Main boundary tests. All 121 shared sources and
the Historical host are unchanged; all three Historical analyzer DLL/PDB pairs
are byteequal to parent. Main/Worker images legitimately change for the additive
contract, not shared Analyzer policy. No package/project/routing/version changes.
The Current detector is byte-identical. All eight whole Current baseline JSONs
(Findings, graph counts, repeat/session lifetime, all four modes) are byteequal.

Format, JSON, strict UTF8/no-BOM/CRLF, PS parsing, Git whitespace, exact source/export
hashes, architecture/isolation and strict Evidence-Verifier PASS. Final report,
attempt index and status are rebound by the final verifier repeat. Zero tracked
path case collisions; actual Windows clean-export execution, no Linux/remote run.
Actual children stopped and no own Historical Worker/fixture remains at verification.

Retained implementation attempts: first Main build CS1022 (owned extra brace),
then CS1061 (wrong namespace for existing SymbolUtils helper), corrected without
changing shared code. Builds thereafter 0/0. First B17 suite 24/24, strengthened
25/25 with nested type and actual Finding applicability checks. One PS5 JSON-array
wrapper caused an incomplete export overlay; a passing 450-test intermediate gate
is preserved separately and NOT final evidence. The exact ten final inputs were
re-overlaid/hash-verified before the final fresh complete gate/regressions. Owned
whitespace layout was corrected by folder-only formatter. No failed test assertion,
test exclusion, warning suppression or known-flake repair. The unchanged native
reader-lifetime crash did not recur in final broad/full runs. All logs/TRX/attempts
are retained and referenced, not hidden behind final totals.

Self Analysis 16 and Canonical Finding Diff 0 added / 0 removed / 0 changed-evidence
are **inherited from B16/B15/B14/B13/B11/B10, not newly run**. Default runsettings
remain Category!=SelfAnalysis; no configured test skipped. No fresh self/diff claim.

## Required 20-point closure

1. Current policy: valid exception prerequisites, unrestricted Direct DOC610,
   declared-type transitive/documented relevance and per-type Direct exclusion.
2. Guard: reporting inventory presence, independent of Result. Actual native guard
   stays unchanged; B17 only enables an equivalent decision from complete facts.
3. Local type: full canonical identity plus source declaration/target role,
   not XML annotation or assembly-name inference; exact original PE inventory bound.
4. Submatrix: eleven rows written before implementation and retained above.
5. Missing facts: complete source-defined exception inventory and separate Direct
   result; original root/source/parse/reference/provenance already present.
6. Added facts: original PE definition IDs in explicit request; owned declaration
   paths/count/ordered reporting scope and immutable canonical Direct result in response.
7. Dataflow: original validated handoffs -> explicit neutral opt-in -> unchanged
   Historical analyzers/binding -> structural validation/receipt -> Main eligibility.
8. Differentials: seven genuine cases, whole flows/types/provenance/graph/ownership,
   full native Findings, exact per-purpose decisions and actual DOC610/DOC611/DOC632.
9. Negatives: sixteen forged/missing responses including recounted unused omission;
   query/mode/kind/type/legacy/clone controls; original/request and actual child failures.
10. Protocol: outer v2 unchanged, additive opt-in v1, strict response validation,
    complete inventory binding, deterministic serialization/repeat, no legacy downgrade.
11. Activation: Historical ProjectTransitiveDeclaredExceptions remains unsupported,
    including opt-in requests trying to change the engine mode. B18, not a mode alias.
12. Current parity: eight byte baselines; existing detector/shared engine/host and
    B10-B16 sources unchanged. No native Worker adoption or session-lifetime change.
13. Historical parity: bounded known-type Finding ELIGIBILITY plus full raw Direct/
    ProjectTransitive analysis match. General full Historical Finding parity NOT claimed.
14. Isolation: genuine pinned child compiler, no Main Historical load or Roslyn IPC,
    no input execution, success/error cleanup and no Current fallback.
15. Tests/builds: fresh final census above, eleven 0/0 WAE builds, Dual PASS.
16. Gates: permanent new class filter, exact build/source/runtime identities,
    full artifact verifier, encodings/format, protected scope and typed negatives.
17. Self/diff: inherited 16 / 0/0/0, neither new.
18. Open: productive guarded Declared, general Finding/document context/coverage,
    unattested external documented types, uncertainty/external XML evidence, generics,
    broader scopes, CLI/signing/unsupported refs/contracts. No workarounds or mega-change.
19. Final Git status below: 15 owned files (eight production contract files, test,
    gate, boundary note, report, audit, two proof scripts); protected root-ignore excluded.
20. HEAD: B16 separately committed before B17 to 839fcb03a6; no movement during B17.
    Final branch/HEAD, root ignore, 23 Core hashes, six stash hashes and empty index
    verified. No commit, push, stash mutation or original Core build churn.

## Owned files / reproduction

Production: HistoricalWorkerPayloadProjection.cs, new
HistoricalWorkerDeclaredPolicyProjection.cs and DeclaredExceptionFindingPolicy.cs;
HistoricalWorkerCallResult.cs / HistoricalWorkerResponseValidation.cs; WorkerProtocol.cs,
HistoricalAnalysis.cs and new HistoricalDeclaredPolicyFacts.cs. No Current detector,
shared analyzer, Historical host or B5/B6 edits. Test:
ExceptionFlowHistoricalDeclaredPolicyTests.cs. Gate only appends its filter.
OPEN-PIPELINE-BOUNDARIES adds only the bounded contract, not Declared activation.
This report, audit JSON and two P5O2B17Proof scripts complete the owned set.

Protected/export/owned-input/attempt snapshots: artifacts/p5o2b17. Actual build:
artifacts/b17-839fcb/Tools/XMLDocNormalizer; retained parent-dual/parent-current and
attempt-overlay-incomplete alongside it. That export holds 13 final regression
TRX/log groups, final Dual audit/logs, exact runtime snapshots and original B14
emissions. Product JSON embeds full seven differentials, graph strings/Findings,
identity/ownership/provenance, decisions/repeats, negatives and source/artifact hashes.

Reproduce from the recorded clean archive plus ten exact owned build inputs:
Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences;
P5O2B17Proof/Run-Regressions.ps1 -ValidationRoot <export-tool-root>;
P5O2B17Proof/Write-Verify-PolicyEvidence.ps1 with the same root. Verifier requires
recorded parent/protection data, checks freshness and all source hashes, not totals
alone. Matrix lines are plain strings to avoid PS5 provider metadata serialization.

B18 must separately define guarded productive execution and the remaining document/
coverage/uncertain-type context necessary for FULL Finding parity before enabling
Declared. Do not feed the unguarded raw ProjectTransitive result to existing
reporting as a Declared result. B17 stops at the verified neutral contract.

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerCallResult.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
?? Evaluation/P5O2B17-historical-declared-finding-policy-contract-audit.json
?? Evaluation/P5O2B17-historical-declared-finding-policy-contract.md
?? Evaluation/P5O2B17Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs
?? src/XMLDocNormalizer.HistoricalWorker/HistoricalDeclaredPolicyFacts.cs
?? src/XMLDocNormalizer/Execution/Analysis/DeclaredExceptionFindingPolicy.cs
?? src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerDeclaredPolicyProjection.cs
```
