# P5O2B19 - Historical Declared Finding Contract Completion

## Phase 1 - source reconstruction BEFORE production changes

Starting HEAD 7fab8a8c626953390ac45b7b5c176970851e8d7d, fix/documentation.
B18 separately committed. Initial tree: protected root .gitignore only. No
applicable AGENTS.md. No commit, push, index or stash operation. Builds use a
clean HEAD export, preserving all 23 physical Core files and six stashes.

Current FindExceptionSmells first gets System.Exception; absent base means no
Findings. It enumerates MemberDeclarationSyntax in the reporting tree, skipping
members without an attached documentation comment. XmlDocUtils.TryGetDocComment
selects the first leading documentation trivia. XmlDocTagExtraction.ExtractTags
uses XmlDocElementQuery.DescendantNodes/XmlElementSyntax and exact local name
exception: nested full elements count, self-closing exception tags do not. Raw
cref is CrefSyntax.ToString(), not literal attribute bytes. BuildTagInfos binds
GetSymbolInfo(cref).Symbol, not candidates, and casts named types separately.

The unchanged Direct collector supplies actual direct exceptions and full paths.
AnalyzeConfiguredExceptionFlow returns Direct for Direct mode, and for Declared
with no reporting-source Exception subtype. Otherwise it uses the caller-owned
summary session and genuine Current dispatch. The graph/session and raw Summary
result are unfiltered. Direct membership is independently retained per TYPE.

ToolRunner.CreateExceptionFlowSummaryAnalysisSession applies the same guard BEFORE
session creation. ProjectClosureSemanticContext.HasDeclaredExceptionTypesInReportingScope
caches whether ANY AnalysisTarget.SourceTypes inherits from System.Exception;
unused declarations count, supporting analysis-only scopes do not. It is a
reporting-scope execution guard, not a per-flow/type/method/Finding filter. The
detector applies that scope-level condition for each analyzed member. Exact
IsDeclaredInReportingScope is syntax-tree ownership/role, not a name or assembly
heuristic. B17 supplies a complete original-PE-bound exception declaration inventory
and exact owned source paths, but only AFTER the Worker has run unguarded Summary.

DOC660: nonblank raw cref, actual cref syntax and no resolved Symbol -> invalid
cref Finding. DOC670: same syntax prerequisites, resolved named type and not an
Exception subtype -> non-exception Finding. A resolved NON-type symbol is neither
DOC660 nor DOC670. Missing cref does not manufacture either semantic Finding.
These checks precede transitive checks and are not suppressed by inheritdoc.

Declared is transitive for reporting. DOC631 requires uncertainty and a locally
declared valid documented exception not exactly covered by proven throws. It
uses sorted first-three uncertain target display texts; external XML evidence is
supplementary text, never proven coverage. DOC632 is suppressed by uncertainty;
otherwise relevant documented local exception types absent from proven throws
produce Findings. Both use exact SymbolEqualityComparer equality, NOT inheritance
coverage. Inheritance is used only to validate Exception types.

DOC610 uses independent Direct, ALL valid documented exception types (external
tags included), exact equality coverage and no local restriction. DOC611 excludes
every type already in Direct, then requires local reporting declaration and exact
coverage by relevant LOCAL documented exception tags. HasInheritdoc recognizes
top-level empty or full inheritdoc elements and suppresses DOC610/611, NOT invalid
cref/non-exception/DOC631/632. Missing documentation skips this detector entirely.

FindingFactory.AtPosition creates line/column from the tree's GetLineSpan at a
one-character absolute span. Missing-tag anchors use MemberAnchorResolver; tag
Findings use the cref attribute SpanStart. FindingContextBuilder.ForDeclaration
is syntax-based (owner/accessibility/name/containing type/nearest namespace/file
classification), not semantic binding. ExceptionFlow and ExceptionTag subjects
and target names are Main policy. Tag snippets use existing SyntaxUtils.GetSnippet;
messages use raw cref or actual thrownType.ToDisplayString. Existing smell definitions,
ordering, formatting and final Finding objects remain Main-only.

B18's "Documentation Contract" therefore means LOCAL reporting-member XML syntax
and its actual bound cref TYPE facts plus source/member reporting context. It is
NOT acquisition/import of external XML documentation-provider contracts. The
latter and uncertain/external-contract Worker success remain unsupported.

## Focused B19 matrix BEFORE code changes

| Information | Current | B17 Historical | Required for final Finding | B19 decision |
| --- | --- | --- | --- | --- |
| Exception type identity | Bound named symbols | Complete canonical flow/declaration identity | Exact equality coverage/relevance | Reuse; add bound documented named-type identity |
| Declaration facts | SourceTypes including unused exceptions | Complete PE-bound owned inventory | Guard and local relevance | Reuse unchanged, no new guard boolean |
| Ownership | Exact reporting tree role | Ordered reporting manifest/declaration paths | Localness and target ownership | Reuse; bind document to original root span/source |
| Direct result | Independent collector | Independent canonical Direct | Unrestricted DOC610 and DOC611 exclusion | Reuse, immutable original-call snapshot |
| Summary result | Raw session/graph result | Existing ProjectTransitive result | Transitive throws/coverage | Reuse, no raw-flow filter or mode alias |
| Direct/Summary provenance | Separate results, same member | Bound request/root/source/reference world | No source-kind guessing | Reuse full canonical results and input provenance |
| Guard input | Any reporting Exception subtype | Complete declarations, returned too late | Choose Direct BEFORE Summary | Evidence already complete; execution remains B20 |
| Local documentation syntax | Attached doc, exact tag extraction, inheritdoc | Not imported | Skip/no-doc, suppression, exact tag cardinality/positions | Add bounded syntax manifest; reuse existing syntax utilities |
| Local documentation binding | Symbol/type binding and exception inheritance | Missing for unobserved documented types | Valid/nonexception/unresolved/other and exact coverage | Add named-type identity/classification, no Finding decision |
| Finding target/context | Original member/tree, syntax builders | Root provenance only | Anchors, snippets, contexts, names/line-column | Derive and retain Main-local from original P5J tree, no general reporting DTO |
| Mode | Explicit Declared reporting mode | Explicit B17 policy mode; engine ProjectTransitive | No default/alias | Add versioned nested opt-in; Declared engine stays rejected |
| Uncertainty | Native result, DOC631/632 policy | Incomplete Worker analysis rejects | No definite result from unknown flows | Reuse failure boundary; do not admit uncertain/external XML reporting |
| Method/source identity | Exact owned member/tree | Existing selector/source provenance | Document belongs to selected member | Bind canonical original root plus exact member/document spans |
| Assembly/module identity | Bound source/reference symbols | Exact B12 context | Validate documented type definition/classification | Reuse retained original symbol world, no PE reopen/ref/source validator |
| Type display text | NamedType.ToDisplayString | Not a general Finding name contract | ExceptionFlow context/messages | Derive exact bounded type names from original bound PE symbol, compare Historical/native |

## Minimal contract decision BEFORE implementation

Add ONLY an optional version-1 documentation facts request nested inside the
existing B17 request. It requires the original complete single project, explicit
Declared POLICY mode and genuinely ProjectTransitive ENGINE mode. The caller
may request the profile but cannot supply a precomputed syntax/type/guard inventory.
Main projects exact syntax from the retained original P5J tree; Worker projects
the same syntax and actual cref binding with its own Historical SemanticModel.
Use unchanged syntax helper source compiled in Worker, not duplicate extraction.

Main validates exact syntax cardinality/order/spans against the original request,
canonical original root identity, existing reference identities and B17 ownership,
and named-type classification against the ALREADY retained original PE symbol world.
No Historical-to-Current symbol binding, source compilation, new metadata reader,
second acquisition/validation pipeline or Current analyzer execution is introduced.
The pinned Worker remains the semantic binding authority, just as for raw flows;
structural/provenance checks do not pretend to independently re-execute its compiler.

Original projected payloads carry a Main-only weak origin receipt. Serialized,
cloned or caller-fabricated payloads cannot mint a Main reporting-context receipt.
Only successful complete original validated calls expose immutable neutral document
facts, source anchors/context and exact type display names. B17 Main eligibility
also recognizes attested documented Exception types absent from throws (previously
UnknownExceptionType); its local/direct/summary rules themselves remain unchanged.
No Findings, smell IDs, relevance/coverage decisions or guard-result boolean go
through IPC. No general Reporting payload or productive detector adoption is planned.

Guard decision: CASE B. B19 supplies complete decision inputs; the existing Worker
executes Summary before creating B17 facts and rejects uncertainty before docs.
Changing that execution ordering/guarded productive entry is an independent B20
boundary, not necessary to close the reporting information contract for the
already admitted complete/proven single-project root-method domain. Existing B18
GC.Collect/no-local rejection remains unchanged. Declared activation remains off.

Contract scope is the existing source-owned parameterless static nongeneric root
method, bounded complete project/reference/parse world and complete proven results;
no solution Declared, generic documented type shapes or uncertain/external XML
success widening. Full member-contract parity must be demonstrated before claiming
this slice complete; general all-member reporting/CLI remains a separate boundary.

## Implemented bounded contract

Outer protocol stays v2. The new single-root-declared-documentation-v1 opt-in is
nested in WorkerDeclaredPolicyRequest.Documentation, with explicit nullable policy
mode, canonical original root and complete ordered syntax manifest. Maximum 128
full exception elements; existing source/reference/syntax/request and 1-MiB response
budgets stay unchanged. Legacy optional null fields are omitted as before.

The WorkerDeclaredPolicyEvidence DTO and HistoricalDeclaredPolicyFacts emitter are
byte-identical to B18. Documentation is a SEPARATE optional response/call field,
DeclaredDocumentation, not a general Documentation/Findings field added to B17.
HistoricalAnalysis creates these additional facts only after unchanged complete
analysis and B17 facts succeed. It still has no declaration/no-doc execution guard.
Failure envelopes cannot contain either B17 facts or B19 documentation facts.

HistoricalDeclaredDocumentationFacts is the SAME fact-projection source compiled
against each owner's own compiler: Worker uses genuine Historical binding; Main
uses its version for native differential observations/syntax projection, NEVER to
rebind Historical source. Six unchanged syntax helper sources are linked into
Worker: XmlDocUtils, XmlDocElementQuery, XmlSyntaxExtensions, MemberAnchorResolver,
ExtractedXmlDocTag, XmlDocTagExtraction. No detector, FindingContext, FindingFactory,
smell definition or relevance/coverage policy is compiled in Worker. The unchanged
Historical analyzer project remains 121 shared sources plus its one semantic host.

Facts added: exact logical root/member span and anchor; documentation presence and
inheritdoc SYNTAX presence; every selected full exception element's ordered span,
actual cref attribute span and raw CrefSyntax text; actual unresolved/named-type/
other-symbol binding category; canonical named-type identity and Exception-subtype
fact; canonical root identity; exact bounded type display names; hashes binding
the already-existing Direct and Summary results to this document evidence.
No new flow result copy, declared inventory copy, XML-provider contract, Finding,
smell/relevance/coverage decision or summary-guard-result boolean enters IPC.

Main projects from the already retained original P5J tree, selecting exactly one
source member for the existing bound PE root, and stores a weak original-payload
origin. It retains original member syntax and bound PE symbols, not a new compiler
or reopened metadata. Syntax/body/reference/root provenance is frozen at registration.
Request mutation, copied/serialized/fabricated payloads cannot acquire the receipt.

Response validation first requires ALL unchanged canonical/B12/B17 checks. B19
then requires original-payload origin, exact source/reference world, canonical
original root, exact complete syntax manifest and ordered bindings, explicit mode/
profile, complete type displays and independent result hashes. Direct types must
be represented in the complete Summary. Named-type identity/Exception-subtype/
display information is checked against the original bound PE symbol world; local
Exception types must exist in B17's complete owned inventory. Missing/contradictory
facts return MalformedResponse with no result, B17 or B19 receipt. This is fact
validation/projection over retained inputs, not another PE/PDB/ref/source validator.

Only the original successfully validated call receives an immutable neutral Main
documentation snapshot. Main derives actual line/column, member/tag contexts and
snippets using the unchanged syntax builders and original source tree; these
reporting objects never go through Worker IPC. Raw response-array mutation cannot
alter the snapshot. Call clones cannot consume it. Legacy calls without the opt-in
cannot create a B19 contract. Missing/default/invalid query mode is a typed failure.

DeclaredExceptionFindingPolicy's existing rules are unchanged. Its known-type
admission additionally recognizes attested documented Exception types in a B19
receipt, even when absent from throws/declarations. Thus a documented external
System.Exception base now has a validated false local-relevance decision instead
of an unknown-type failure on B19 calls; old B17/no-doc calls retain their exact
unknown-type behavior. Direct remains unrestricted; Summary still requires actual
local declaration and excludes Direct types; documented relevance remains local.
No final Historical Finding renderer or productive native-detector adoption is added.

## Differential and negative evidence

16 genuine positive cases: local, external-only, mixed, combined Direct/Summary,
unused local declaration, partial, nested, exact external documentation coverage,
external base counter-control, inheritdoc, unresolved/non-exception/missing-cref
tags, no documentation, cref alias, resolved method cref, duplicate full tags,
and missing-cref/self-closing-tag extraction control. Original B14 PE/PDB/P5
handoffs traverse the real external detector/B9 -> B8 -> B7 -> B6 -> B5 -> pinned
Worker, not a custom activation side channel. Two calls compare whole canonical
results, complete B17 declarations/ownership/Direct, full B19 document facts,
provenance, immutable Main facts and deterministic repeat. Raw engine is explicitly
ProjectTransitive, not a renamed Declared request.

Native Current observations use the actual unchanged detector and retain COMPLETE
Finding objects, graph count, canonical graph strings, all Direct/Summary flows and
uncertainties, reporting guard observation and per-type policy decisions. Native
and Historical document binding facts are compared in full, not counts. Every
actual native Finding's anchor, line/column, snippet, complete context and type
display is accounted for by B19 data; aliases and base-type noncoverage are explicit.
These are full member-CONTRACT differentials, NOT a claim that Historical already
emits production Findings. The identical fact projector is compiler-local in both
versions and reuses original syntax utilities; the independent actual native
detector/Finding checks prevent a flows-only or count-only parity claim.

Guard positive controls include observed and UNUSED local Exception declarations;
negative external-only control has zero source declarations, raw framework Summary
flow but no native Declared transitive Finding. Main's existing SummaryEnabled
decision matches the full declaration inventory on every query. No guard boolean
is invented or sent by Worker. B18's no-local GC.Collect case remains actual
Historical AnalysisFailure with no result/B17/B19 facts, whereas native Declared
Findings are empty. This production execution difference is explicitly retained.

22 forged response cases: missing docs/bindings, removed/recounted tags, foreign
owner/member span/root, altered cref/inheritdoc/doc-presence, default/missing mode,
wrong profile, conflicting type identity/classification/display, missing/duplicated
type displays, unknown binding category, missing type, contradictory independent
Direct, missing Summary and missing Direct. All fail closed with no contract receipt.
Additional request/admission test rejects caller facts, absent/default modes and
profile, cloned original-payload receipt, legacy evidence, and preserves the actual
uncertain Worker failure. Original B17/B18 negative tests run unchanged as well.

## Scope, trust and B20

Complete v1 contract means the already admitted complete/proven single-project
selected-root member, static/non-generic/parameterless selector, exact original
source/parse/reference receipts, and nongeneric named documentation types that can
be attested in the retained original PE symbol world. This is NOT a claim of
general all-member/solution/CLI reporting support. Generic named docs, types outside
that original bound world, incomplete/uncertain/external-XML-contract analysis and
unsupported references remain fail closed. No display-name matching or new PE
reader is used to widen that domain. A valid metadata image elsewhere in the
reference manifest is not substituted for unavailable bound type evidence.

The pinned Historical compiler is the semantic cref-binding authority, as it is
for existing analyzer facts. Receiver checks bind syntax, full identities, original
input world, classifications, completeness and result consistency. They do NOT
claim cryptographic proof of arbitrary compiler semantics or independently repeat
its analysis. Main never resolves Historical crefs with a Current SemanticModel or
uses Current analysis as recovery. Original bound PE type checks are factual
consistency checks, not source ownership inference or a cross-version symbol import.

B20 is a separate productive boundary: establish complete owned declarations and
documentation/no-doc skip facts BEFORE requesting Summary; select Direct versus
Summary according to the existing scope rule, not after a failed unguarded run.
Then adopt the validated neutral contract at the SAME Main Finding policy/rendering
boundary, preserve native Current session lifetime and all eight byte baselines,
and prove whole Historical production Findings equal Current before engine mode
admission. B19 supplies the reporting information; it neither duplicates the native
detector nor implements a parallel renderer. Remaining productive consumer wiring
and guard ordering are NOT hidden behind the word "contract complete".

## Required 21-point closure

1. Full Current Finding flow: documented in Phase 1, including analyzer, guard,
   doc extraction/binding, relevance/coverage, suppression and exact Finding creation.
2. B18 Documentation meaning: LOCAL selected-member tags/binding/context, not
   external XML-provider acquisition or a general reporting payload.
3. Summary guard: scope-wide ANY owned Exception subtype, including unused;
   ToolRunner avoids session creation, detector selects independent Direct. Not per-flow.
4. Matrix: fifteen concrete information rows written before production changes.
5. Existing B17 facts: complete PE-bound inventory/ordered ownership, independent
   Direct, raw Summary and exact source/reference/runtime provenance reused unchanged.
6. Previously missing: local document syntax/bindings, unobserved documented types,
   complete selected-member target/context and actual type display information.
7. Added: versioned local syntax/binding/root/type/result-binding facts and immutable
   Main source/context receipt; no duplicate B17 data or new analysis semantics.
8. Responsibility: Worker binds/projects facts; Main validates, owns source context,
   guard/relevance/coverage/emission. Productive execution guard remains B20.
9. Protocol: outer v2 unchanged, nested explicit opt-in v1; separate optional B19
   response/call field; null-omitted legacy unchanged; full validation and same budgets.
10. Differential: sixteen full member-fact/native-Finding cases, whole flows/types/
    provenance/ownership/repeats and actual positive/negative declaration guard inputs.
11. Negatives: twenty-two malformed/missing/conflicting responses plus genuine
    original/request/clone/legacy/query controls and real AnalysisFailure; no fallback.
12. Activation: Historical Declared remains explicitly UNSUPPORTED, including
    B19 opt-in. Full Historical productive Finding parity is NOT claimed.
13. B20: early declaration/no-doc guard and existing Main consumer adoption;
    prove complete production Finding parity before mode admission. No mega-change here.
14. Current: eight whole native JSON baselines byteequal; actual detector/ToolRunner,
    121 shared analyzer sources, host, B5/B6 and B10-B18 tests unchanged. Native Current
    does not start Worker. Main/Worker binaries legitimately change for opt-in support.
15. Isolation/cleanup: pinned child runtime only, no Historical assembly in Main,
    no Roslyn types in protocol, success/error children stopped, errors retained;
    final verifier finds no own live Worker/fixture. Submitted source is not executed.
16. Tests/builds: fresh final census below, all configured groups passed; eleven
    WAE builds 0/0. All first failures retained, no assertion failures or skipped tests.
17. Gates: permanent gate checks EXACT six unchanged Worker syntax leaves and no
    detector/policy link, bounded fact projector, complete new tests and unchanged
    analyzer/version/runtime isolation. Format/JSON/UTF8/CRLF/PS/protection/verifier PASS.
18. Self Analysis 16 and Canonical Finding Diff 0/0/0 are INHERITED, NOT freshly run.
    Unchanged configured Category!=SelfAnalysis; no configured test is skipped.
19. Open: B20 productive guard/consumer/admission; general reporting/CLI/solution
    loading, uncertain/external XML, generic/out-of-world document types, signed/
    unsupported refs and wider scope/signatures. No Dapper or plugin/version work.
20. Final status below: 18 owned files, protected ignore excluded; all unstaged,
    empty index, 23 Core files/six stashes/root ignore unchanged; no commit/push.
21. HEAD: initial/final 7fab8a8c626953390ac45b7b5c176970851e8d7d on fix/documentation.
    No external movement observed; fresh verifier checks recorded HEAD/branch.

## Fresh validation census

Final runs are after the final exact 13 build-input overlay and Dual gate. Groups
overlap and must not be summed as unique totals. Zero failed/skipped configured tests.

| Group | Passed / total |
| --- | --- |
| Fresh B18 HEAD parent boundary | 457 / 457 |
| B19 | 39 / 39 |
| B18 unchanged | 7 / 7 |
| B17 unchanged | 25 / 25 |
| B16 unchanged | 18 / 18 |
| B15 unchanged | 28 / 28 |
| B14 unchanged | 31 / 31 |
| B13 unchanged | 34 / 34 |
| B12 unchanged | 29 / 29 |
| B11 unchanged | 16 / 16 |
| B10 unchanged | 34 / 34 |
| B5-B9 unchanged | 156 / 156 |
| Productive | 681 / 681 |
| Focused | 706 / 706 |
| Broad, diagnostic Blame | 3007 / 3007 |
| Full configured, diagnostic Blame | 3098 / 3098 |
| Final boundary | 496 / 496 |

Dual-Version/runtime/isolation PASS. Eleven WAE builds each 0 warnings/0 errors:
three Current, three Historical, two Worker, isolated boundary process, genuine
Historical fixture and Main test project. All three Historical analyzer DLL/PDB
pairs byteequal to freshly built HEAD parent; Main/Worker images intentionally
change for protocol/context support. All eight whole Current baseline JSONs byteequal.
No analyzer/version or native policy/session behavior change. Windows SDK 8.0.418
executed; no Linux/remote execution claim. Tracked path case-collision check PASS.
Final report/status rebound by final verifier repeat; source/export/protection,
format/PS/JSON/UTF8-noBOM-CRLF, architecture and own-child cleanup PASS.

## Retained attempts

First Main WAE build failed with two CS8602 null-dereference diagnostics in NEW
documentation request/type checks, 0 warnings/2 errors. Explicit syntax and metadata
name null checks corrected the implementation; main-second.log is 0/0. Both Worker
attempts and first test-project build are 0/0. First B19 test run 39/39. During
implementation response layout was kept separate from B17: its evidence DTO and
Worker fact emitter remain byte-identical, not rewritten. No failed test assertion,
unchanged retry, exclusion or suppression. Final boundary 496/496 and all fifteen
regression groups passed. Broad/full --blame did not encounter the known unchanged
native metadata reader-lifetime flake; no opportunistic fix. All logs/TRX/attempts
remain under the recorded export and attempts.json, not overwritten as final evidence.

## Owned files and reproduction

Production (11): WorkerProtocol.cs, HistoricalAnalysis.cs, NEW
HistoricalDeclaredDocumentationFacts.cs, Worker csproj; Main csproj,
HistoricalWorkerPayloadProjection.cs, DeclaredExceptionFindingPolicy.cs,
HistoricalWorkerCallResult.cs, HistoricalWorkerResponseValidation.cs, NEW
HistoricalWorkerDeclaredDocumentationProjection.cs and DeclaredFindingDocumentationContract.cs.
Test (1): NEW ExceptionFlowHistoricalFindingContractTests.cs. Gate (1):
Verify-DualVersionBuild.ps1. Evidence (5): one OPEN-PIPELINE annotation, this report,
generated audit JSON, Run-Regressions.ps1 and Write-Verify-FindingContract.ps1.
No bin/obj product changes. Root ignore is excluded pre-existing WIP.

artifacts/p5o2b19 stores initial 914 tracked-file/23-Core/six-stash/index/root-ignore
protection snapshot (after written pre-implementation report), clean head.tar,
export.json, exact thirteen owned build inputs and attempts.json. Export is
artifacts/b19-7fab8a/Tools/XMLDocNormalizer, with parent-dual/parent-current alongside.
It retains fifteen final TRX/log groups, final Dual audit/build logs, sixteen full
member witnesses, twenty-two negatives/admission and all initial attempts. Audit
JSON embeds whole native Findings/flows/graphs, full document facts and immutable
Main context, actual runtime/provenance/repeat/decisions and source/artifact hashes.
It marks bounded contract complete, productive guard/activation FALSE.

```powershell
# In the recorded clean export plus exact thirteen source/test/gate overlays:
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
# In the authoritative workspace with its recorded protection/export artifacts:
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B19Proof/Run-Regressions.ps1 -ValidationRoot <export-tool-root>
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B19Proof/Write-Verify-FindingContract.ps1 -ValidationRoot <export-tool-root>
```

Verifier checks freshness and complete case data, not totals alone; all unrelated
tracked source bytes, 23 complete physical Core files, six stashes, root-ignore,
empty index and HEAD; exact built overlays, original analyzer images/versions,
eight byte baselines, own-child cleanup, format/encoding and final owned-file hashes.

## Final git status --short

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj
 M src/XMLDocNormalizer/Execution/Analysis/DeclaredExceptionFindingPolicy.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerCallResult.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
 M src/XMLDocNormalizer/XMLDocNormalizer.csproj
?? Evaluation/P5O2B19-historical-declared-finding-contract-audit.json
?? Evaluation/P5O2B19-historical-declared-finding-contract.md
?? Evaluation/P5O2B19Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs
?? src/XMLDocNormalizer.HistoricalWorker/HistoricalDeclaredDocumentationFacts.cs
?? src/XMLDocNormalizer/Execution/Analysis/DeclaredFindingDocumentationContract.cs
?? src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerDeclaredDocumentationProjection.cs
```

No commit/push/stash/index change; protected WIP/Core bin/obj unchanged. B19 ends
at the verified bounded member Finding contract, NOT productive Declared support.
