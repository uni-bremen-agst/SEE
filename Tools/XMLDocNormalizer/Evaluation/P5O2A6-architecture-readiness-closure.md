# P5O2A6 - Architecture / Readiness Closure

## Decision

**NOT READY. P5O2A is not yet architecturally closed.** The required zero
inter-component-cycle gate fails in upper summary orchestration, not in the
extracted contextual facts. Do not start P5O2B in this package. A separate,
minimal **P5O2A6-Fix - Summary orchestration dependency closure** is recommended.
No production fix has been made.

Audited HEAD: `4ee61abee61b7677857100aa46173977d64283f0`
(`Clean up contextual evaluator composition.`), 2026-10-06. A5C is separately
committed. Initial status/diff/stat/HEAD/log were inspected before A6 changes:
only the protected root `.gitignore` and seven tracked Core `bin/obj` files
were modified. They remain foreign WIP. A6 changes only the audit tool and
Evaluation evidence/register; production and test source are unchanged.

Evidence: [bound audit JSON](P5O2A6-architecture-readiness-closure-audit.json),
[exact source/component/state matrix](P5O2A6-historical-core-readiness-matrix.md),
[reproducible audit tool](ContextualBoundaryAudit/ArchitectureClosureAudit.cs)
and [evidence renderer](ContextualBoundaryAudit/Write-ClosureEvidence.ps1).

## 1. Fresh graph and closure gates

The active production compilation, rather than historical report assertions,
is the measurement source. Binding covers method calls, constructors,
constructor initializers, properties, method-group/delegate references,
lambda bodies, field initializers, type signatures and expression types.
There are 2,451 source-declared callable nodes (1,569 method declarations),
11,487 distinct kind-labelled edges (4,702 source-target edges), 417 type
declarations and 358 hashed production source
files. A callable-pair/kind edge retains one representative site; it is not
a count of every repeated invocation. The narrower primary source-method
invocation graph has 2,660 edges. Auto-properties, positional record properties
and field state are inventoried separately. Local functions have their own
callable nodes. Compiler-generated method bodies and metadata implementation
bodies are not traversed. This is a static dependency audit, not runtime
points-to/dynamic-execution proof; inspected callback targets are recorded.

| Gate | Fresh result | Verdict |
| --- | ---: | --- |
| Analyzer-owned downstream fact/resolver methods | 0 | Pass |
| Contextual evaluator -> Analyzer, direct/indirect | 0 | Pass |
| Lower fact/resolver/classifier/scope -> Analyzer | 0 | Pass |
| Unjustified contextual-evaluator facades | 0 | Pass |
| Contextual SCC methods / internal edges | 63 / 112 | Pass |
| Evaluator-owned cache helpers / fields | 2 / 3 | Pass |
| SCC ingress edges / callers / targets | 16 / 15 / 4 | Unchanged |
| Whole evaluator ingress edges / callers / targets | 27 / 24 / 4 | Unchanged |
| SCC egress | 791 | Exact edge equality |
| SCC-downstream inter-component cycles | 0 | Pass in that scope |
| Whole-core orchestration component cycles | **1** | **Fail** |

All 27 lower owners, including RuntimeDispatchClassifier, CatchSemantics and
SemanticScope, have their complete bound type closure recorded. No direct or
indirect Analyzer dependency was found there. The SCC downstream member graph,
initializer dependencies, lambda sites and state accesses match the validated
post-A5C graph exactly. The evaluator/cache declarations are token-identical.
Weak SemanticModel keys, OriginalDefinition/SymbolEqualityComparer keys,
guard-before-cache checks, per-chain recursion guards, `finally` removal,
bool cache values and partition locks are unchanged. No internal SCC splitting,
new provider, cache redesign or algorithm change occurred.

### Cycle scopes: do not equate a downstream census with the whole architecture

The expanded provider graph still contains its known nested DataFlow/cache
implementation cycle. It is one provider-internal ownership unit, unchanged
and not an inter-component closure failure.

The fresh top-level whole-core type graph has **three** cyclic groups:

1. `CanonicalExceptionFlowCallContext <-> CanonicalIdentityKeyWriter`: neutral
   context/key identity domain; the key writer consumes that domain.
2. `CanonicalFunctionPointerParameter <-> CanonicalTypeIdentity`: recursive
   neutral type identity domain (a function-pointer parameter contains a type,
   and a function-pointer type contains parameters).
3. `ExceptionFlowAnalyzer -> ExceptionFlowSummaryAnalysisSession ->
   ExceptionFlowSummaryGraphBuilder -> ExceptionFlowAnalyzer`: a genuine cycle
   between distinct upper orchestration components.

The method-owner graph has two cyclic groups: the context/key domain and the
three orchestration owners. Both raw domain cycles are retained in evidence;
they are not hidden by reporting a raw type-cycle count of zero. The requested
inter-component gate fails on the third group independently of domain grouping.
A5A-C's zero-cycle statements measured the SCC-downstream closure. They do not
prove absence of upper summary orchestration cycles. A6 expands that scope;
this is an uncovered dependency, not an A5C semantic regression.

## 2. Minimal blocker and proposed separate fix

The cycle has two independent upper seams:

| Caller | Callee | Source site |
| --- | --- | --- |
| Analyzer.CreateSummaryAnalysisSession | Session constructor | Analyzer.SummaryGraphEvaluation.cs:23 |
| Analyzer.AnalyzeSolutionTransitivelyThrownExceptions | Session.Analyze | Analyzer.SummaryGraphEvaluation.cs:43 |
| Analyzer.GetSummaryInvocationSourceCoverage | Builder.HasAnalyzableSummaryInvocationBody | Analyzer.SummaryGraphDispatch.cs:421 |
| Session constructor | Builder constructor | SummaryAnalysisSession.cs:51 |
| Session.Analyze | Builder.TryRegisterSummaryGraphRoot / BuildPendingSummaryNodes | SummaryAnalysisSession.cs:71 / 80 |
| Builder.AnalyzeSummaryAccessor | Analyzer.AnalyzeSummaryNode | SummaryGraphBuilder.cs:790 |
| Builder.AnalyzeSummaryAnonymousFunction | Analyzer.AnalyzeSummaryNode | SummaryGraphBuilder.cs:740 |
| Builder.AnalyzeSummaryLocalFunction | Analyzer.AnalyzeSummaryNode | SummaryGraphBuilder.cs:602 |
| Builder.AnalyzeSummarySymbolDeclarations | Analyzer.AnalyzeSummaryNode / AnalyzeSummaryImplicitConstructor / AnalyzeSummaryInstanceConstructor | SummaryGraphBuilder.cs:243 / 210 / 297 |

These are 12 distinct cross-owner method-pair/kind edges, with bound source
signatures in JSON. Repeated Builder calls to the same Analyzer entry exist
at additional sites; deduplication does not change reachability.

Root cause: the Analyzer is simultaneously the lower traversal implementation
used by Builder and an upper owner of the session that creates Builder. It
also calls a Builder-owned body-availability query while Builder uses Analyzer
traversal. Consequently **moving only the session factory is insufficient**:
Analyzer -> Builder -> Analyzer would remain.

Smallest sensible separate package:

- Move ownership of the session factory/one-shot entry to existing outer
  composition or Session, updating the Main callers in ToolRunner and
  XmlDocExceptionSemanticDetector. Preserve reusable sequential sessions and
  every entry user's behavior; do not replace the edge with an Analyzer callback.
- Make `HasAnalyzableSummaryInvocationBody` and its exact
  `TryGetSummaryInvocationBody` predicate available below both Analyzer and
  Builder. Choose an existing semantic/body-discovery owner after reviewing its
  responsibility; do not automatically introduce a new provider. Keep source
  coverage policy in the Analyzer and graph construction in Builder.
- Preserve exact tree/semantic-model ownership and the currently supported
  method/local-function body shapes, including fail-closed missing-body cases.

This is locally bounded ownership work, but touches multiple components and a
precision-sensitive supporting-source/source-coverage seam. Risk: low-to-medium
structural change, potentially significant semantic impact if the predicate or
host dispatch changes. It merits its own authorized package with dependency
guards, session/local/summary/runtime/supporting-source regression, warning-as-
error solution build, full suite, self analysis and complete canonical/evidence
comparison. A6 does not perform it.

A cohesive source set might technically compile despite this cycle. That is
not evidence that the user's stronger component-direction gate is met; no
historical compilation failure is claimed as an experimentally proven result.

## 3. Remaining Analyzer responsibilities

Fresh inventory: **224 methods**, 27 actual Analyzer partial declarations,
12,412 owned nonblank lines. The historical filename-prefix measure is 28
files / 12,898 nonblank lines; one legacy filename contains provider code only.
These are observations, not size targets.

The matrix assigns every method ID to its physical partial/responsibility,
with current callers and declaration hashes retained in JSON. The groups are:

- Root/accessor/invocation contexts: argument projection, reduced-extension
  receiver and ordinal mapping, defaults/setter composition. They integrate
  evaluator facts instead of owning downstream fact algorithms.
- Enum/numeric/throw reachability: syntax/operator/pattern interpretation and
  branch truth decisions. Provider/evaluator fact retrieval remains below them.
- Framework contracts and LocalSourceFacts: exception-source/path policy.
  The latter's six methods detect thrown exception-factory results, recognize
  exception types/creation shapes and avoid duplicate direct-throw evidence.
  Callers are local-source and summary analyzers. Despite the filename, this is
  exception-source policy, not the extracted generic delegate-target resolver
  or a general downstream value-fact owner.
- Accessor/explicit/implicit/dynamic/operator/constructor/await/foreach/
  deconstruction/collection/disposal traversal: source/operation handling,
  target registration, exception-path/call-edge and uncertainty composition.
- Runtime dispatch/completeness: runtime target-set, source-coverage and
  dispatch-context coordination. The stateless method-shape classifier already
  belongs to RuntimeDispatchClassifier; it has not returned to the Analyzer.
- Summary traversal and graph coordination: productive analysis-core duties.
  The two SummaryGraphEvaluation entry-composition methods are the exception:
  upper session composition, identified as part of the blocker above.

No remaining method was assigned to an already extracted downstream fact/
resolver owner merely because it forwards a fact call or contains “Facts” in
its filename. Conversely, session composition is not excused as a lower fact
responsibility. No new seed proxy or empty Analyzer partial exists.

## 4. Concrete historical-source/readiness boundary

The source closure starts at all **83 Flow top-level owners** (including
canonical adapters/domain, local/summary/runtime components), follows all
bound source type dependencies and adds 17 shared non-Flow owners: **100 owners
/ 117 physical source files**. The matrix lists every owner, dependency,
category, physical file and direct Roslyn usage; JSON includes file hashes.
It also lists all excluded Main declarations.
Owner counts are A=44, B=14, D=39, E=3; C is the excluded host/Main inventory.

| Category | Boundary and treatment |
| --- | --- |
| A: Historical analyzer core | Evaluator and all fact/resolver/classifier/catch/local/summary algorithms, runtime/dispatch/traversal, Roslyn canonical adapters; compile against the historical compiler universe. |
| B: Shared infrastructure | SemanticScope/environment capability declaration, value facts/extensions, SymbolUtils/SyntaxUtils, KnownFramework contract/model infrastructure, ExternalDocumentation model and local Roslyn-bound analysis result. Source-sharing may be Roslyn-bound; it is not a reference to the current Main assembly. |
| C: Main-only | CLI/ToolRunner/detectors/reporting/options, Workspace/MSBuild loading, ProjectClosure semantic context, supporting-source acquisition/catalog/reconstruction, active ProjectClosure environment host partial. |
| D: Boundary/result domain | Canonical identity/context/summary/result/path/uncertainty/evidence values and neutral path/dedup/source-kind/details types. Existing source-shared neutral validation project remains unchanged. |
| E: Current blocker | Analyzer, SummaryAnalysisSession, SummaryGraphBuilder; intended core A once the two upper seams are closed. Not an unknown source set. |

**Explicit host cut:**
`Execution/Semantic/ProjectClosureExceptionFlowSemanticEnvironment.cs` is not
in the historical source manifest. Its sealed partial capability declaration
remains in Flow. A historical same-source, ordinary nonvirtual implementation
must supply exact semantic-model lookup, analysis scopes, both supporting-
source-method resolution overloads and external supporting-source scope lookup.
This preserves the A2 SameCompilation boundary; the active host implementation
itself still depends on Main services. No claim of a standalone historical
compilation without a host is made. All other source edges close inside the
candidate set; no Workspace/MSBuild type use remains after this explicit cut.

Do not reintroduce the rejected runtime-ProjectReference shortcut. The earlier
neutral runtime assembly experiment changed self findings 16 -> 70; the broad
rebind experiment produced 129. Source-sharing into each Roslyn universe is
the established candidate direction, not an instruction to implement B here.
Mixed declaration files (for example KnownFramework contract types) must be
included as whole physical files; a neutral enum alone does not make such a
file Roslyn-independent.

Future crossing candidates are canonical values and neutral evidence/path/
source-position data. `Compilation`, SemanticModel, SyntaxNode/Tree, symbols,
operations, callable keys/contexts containing symbols and the local
`ExceptionFlowAnalysisResult` DTO stay in their owning compiler universe.
No worker, transport protocol, serializer or process manager is designed.

## 5. Roslyn/API/version inventory

The candidate uses 210 distinct directly bound Roslyn top-level types and 430
unique Roslyn metadata member signatures in five
namespaces: `Microsoft.CodeAnalysis`, `.CSharp`, `.CSharp.Syntax`, `.Operations`
and `.Text`. JSON records all unique metadata member signatures and source
sites, not just this selection:

- Compilation/SemanticModel, SyntaxNode/Tree/Reference/Token/TextSpan,
  ISymbol/IMethodSymbol/INamedTypeSymbol/ITypeSymbol/IParameterSymbol;
  SymbolEqualityComparer and SymbolDisplayFormat.
- Invocation/conversion/assignment/property/argument operations; semantic
  GetOperation/GetEnclosingSymbol; ModelExtensions GetSymbolInfo/GetTypeInfo/
  GetDeclaredSymbol/AnalyzeDataFlow and their actual typed overloads.
- CSharp await, foreach, deconstruction and conversion extension APIs;
  ListPatternSyntax, RecursivePatternSyntax, UnaryPatternSyntax and the used
  constant/declaration/discard/is-pattern syntax families.
- IFunctionPointerTypeSymbol and RefKind; DocumentationCommentId creation and
  exact symbol resolution; canonical symbol identity/rebinding APIs.

Main targets net8.0 and pins Microsoft.CodeAnalysis, CSharp, CSharp.Workspaces
and Workspaces.MSBuild at **5.0.0**. The audit project pins the 5.0.0 workspace
packages separately; it is not a proposed historical core dependency. Main
also pins MSBuild.Framework 17.11.31, Locator 1.11.2, NuGet.Frameworks 6.5.0
and CodeAnalysis.Analyzers 3.11.0. Tests reference Main, the neutral Core and
Evaluation projects. The neutral Core has no Roslyn/package/project reference
and explicitly links shared source; it is a neutral validation boundary, not
the complete Roslyn-bound historical analyzer. Projects/links are inventoried
without changing them or building the protected Core outputs.

Current Main-bin Roslyn DLLs have product version
`5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09`.
The P5N validated net8 historical pair has product version
`5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0`,
with exact MVID/SHA evidence in the original
[P5O2 readiness report](P5O2-historical-compiler-worker-readiness.md).
Matching public assembly version `5.0.0.0` does not make those builds identical.
Do not invent a historical “Roslyn 4.x” NuGet target.

Potential risks, **not proven incompatibilities**: availability/overload binding
of function-pointer/pattern/operation APIs and typed CSharp extensions;
language-version and operation/lowering differences; diagnostic or symbol-
display/DocumentationCommentId behavior; framework/API surface and exact
Immutable/Reflection.Metadata/runtime dependency closure. B must pin the
complete binary/runtime manifest and compile this exact source candidate plus
its host against both compiler pairs. A6 performs no historical installation,
compatibility adaptation or dual-version build.

## 6. State, lifetime and callback closure

The matrix/JSON enumerate every core-relevant field, auto-property and positional
record property, including nested state and the excluded host's ProjectClosure
field. `readonly` describes
the reference, not deep immutability. Main global configuration is not imported
into the candidate; environment-specific artifact access remains a host/input
requirement.
There are 236 candidate-local state declarations plus the one explicitly
excluded Main host field (237 inventory entries).

There are **11 nonconstant static fields** in the candidate. Five cache owners
are materially mutable: contextual evaluator, DataFlow provider, successful-
dereference discovery, ExternalDocumentation model and neutral path-step memo.
The other static fields are empty/failed snapshots, comparers or the private
curated framework contract registry.

| Owner/state | Lifetime and readiness condition |
| --- | --- |
| Evaluator CWT + gate/entries | Weak SemanticModel partition; OriginalDefinition symbol -> bool. Guard, locking and lifetime exactly unchanged from A5C. Local to each compiled core/semantic world. |
| DataFlow cache | Weak SemanticModel table, exact syntax/overload keys, immutable snapshots; lock covers first computation. Calculator/partition factory bind provider methods only. Known nested provider cycle unchanged. |
| Dereference cache | Weak SemanticModel partitions, exact syntax/symbol/query mode; locked lookup/store with computation outside lock. No Main/global state. |
| Path step dedup CWT | Weak neutral path-step identity memo; no Roslyn symbol retention. |
| XML sidecar cache | Strong process-local ConcurrentDictionary keyed by documentation path; string/array indexes only, no Roslyn objects. No invalidation: artifact/XML files must remain immutable at that path during process lifetime. Local historical artifact layout/file access must be supplied; no redesign here. |
| SemanticScope | One Compilation and sequential lazy deterministic source-type list; exact syntax-tree identity guard, not a global compilation cache. |
| Summary session/graph/evaluator | Sequential reusable environment/session lifetime; mutable nodes, pending work and result/traversal containers stay core-local. Session intentionally not thread-safe; no cross-world sharing. |
| AnalysisResult / call context / traversal | Per-analysis mutable result sets/maps, context snapshots and recursion guards; symbol-valued data stays local. |
| Canonical identity/result and path values | Owned neutral value graphs/snapshots. Recursive type-domain relationships are not shared semantic state. |

Callback inspection covers lambda bodies and static initializers, not only
explicit invocation names. DataFlow's injectable calculator is a provider
test seam, not a production Analyzer factory. KnownFramework match/evaluate
delegates bind within that model. CallContext.RebindCallable receives the
SummaryTargetRegistrar callback to CrossCompilationResolver, capturing only
the destination scope. Result merge/filter predicates are synchronous
SummaryGraphEvaluator catch-policy inputs, not retained Analyzer delegates.
Evaluator callback-return analysis inspects user syntax; it does not receive
an Analyzer-owned callback. No lower-owner callback/factory/state backchannel
to the Analyzer was found. The upper cycle is ordinary bound calls, not a
hidden delegate issue.

## 7. A4/A5 progress and scope reconciliation

| Package | Analyzer downstream methods | SCC -> Analyzer facts | Historical provider projection |
| --- | --- | --- | --- |
| A4C Semantic-model seam | 102 -> 92 | 68 after | 20 after; Scope 18 |
| A4C Residual families | 92 -> 64 | 68 -> 38 | 20 -> 50 |
| A4C Runtime/stable member | 64 -> 57 | 38 -> 36 | 50 -> 57 |
| A4C Return/condition | 57 -> 49 | 36 -> 32 | 57 -> 61 |
| A4C Sequence/context/element/source | 49 -> 27 | 32 -> 12 | 61 -> 81 |
| A4D | 27 -> 23 | 12 -> 8 | 81 -> 85 |
| A4E | 23 -> 14 | 8 -> 1 | 85 -> 92 |
| A4F | 14 -> 0 | 1 -> 0 | 92 -> 93 |
| A4G | 0 | 0; delegate seam 1 -> 0 | 93 -> 94 |
| A5A | 0 | 0 | Full census: 126 calls / 19 owners |
| A5B | 0 | 0 | 63 SCC + 2 helpers + 3 fields move to evaluator |
| A5C | 0 | 0 | 2 seed facades -> 0; 14 sites bind directly |
| A6 | 0 | 0 | Upper orchestration cycle found outside prior downstream scope |

The SCC is 63/112 throughout. Historical SemanticScope projection was 18;
the A5A full kind-labelled census counts 19 including the Compilation overload.
Provider/lower-to-Analyzer and downstream component cycles remain zero; these
are not whole upper-architecture cycle claims. SCC ingress remains 16/15/4;
A5C whole evaluator ingress becomes 27/24/4 after direct seed composition.

Size observations: A4D 41 files/19,948 -> 41/19,693; A4E -> 41/19,376;
A4F -> 40/18,631; A4G -> 40/18,316; A5B -> 29/12,977;
A5C -> 28/12,898. Actual A5B declarations were 28/12,483 owned lines;
A5C and A6 are 27/12,412. Source reports and precise ownership/graph records
are linked in the JSON progress inventory; no historical projected count is
presented as a fresh full census.

The initial A4C SCC audit found 124 Analyzer-owned downstream methods in its
252-node inventory (39 already extracted owners, 163 total downstream nodes)
and two summary helper dependencies outside that inventory. The subsequent
semantic-model slice starts at 102 remaining methods after intervening slices;
these are distinct historical stages, not directly interchangeable censuses.

## 8. Validation and reproducibility

Fresh A6 architecture tests: **27/27, no failures/skips**. Fresh audittool
warning-as-error build: **0 warnings / 0 errors**. Active source binding has
zero compilation errors. The full A6 measurements are byte-identical on repeat;
raw measurement hashes and TRX are recorded in the paired JSON.

The post-A5C semantic baseline is inherited, **not newly run in A6**:
full recovery suite **2,560/2,560, no skips**, solution warning-as-error build
**0/0**, self analysis **16** (DOC610=0, DOC611=1, DOC631=15, DOC632=0),
canonical diff **0 added / 0 removed / 0 evidence changes**. Full raw and
normalized finding arrays were equal in A5C. The normalized finding hash is
`2335A309D71F94CC0D3DD7546B57E2D14A107806CD666DD4FEBED5F04AB5F8DF`.

Reuse is justified by zero production/test diff or untracked source at the
committed A5C HEAD, all 14 A5C recovery source/test hashes still matching,
and exact equality of fresh SCC, composition/declaration hashes, ingress,
egress, every callable node, state access, initializer and lambda evidence to
the validated post-A5C raw graph. The raw A5C artifact hash is checked before
comparison. Main/Core solution build and self/full suite were deliberately not
rerun, preserving protected Core build churn. The known MultiModule parallel
flake and global format differences were not touched.

Reproduction from this HEAD/workspace:

```powershell
dotnet build Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj --no-restore -warnaserror
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter "FullyQualifiedName~ExceptionFlowFactComponentDependencyTests|FullyQualifiedName~ExceptionFlowSemanticDependencyGuardTests|FullyQualifiedName~ExceptionFlowCoreDependencyTests|FullyQualifiedName~ExceptionFlowSummaryComponentDependencyTests|FullyQualifiedName~ExceptionFlowLocalAnalysisComponentDependencyTests|FullyQualifiedName~ExceptionFlowContextualFactEvaluatorDependencyTests" --logger "trx;LogFileName=architecture.trx" --results-directory artifacts/p5o2a6/test-results
dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a6/architecture-final.json --architecture-closure
dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a6/architecture-repeat.json --architecture-closure
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/ContextualBoundaryAudit/Write-ClosureEvidence.ps1
git diff --check
```

The audit's legacy console `inter-component cycles 0` line is explicitly the
SCC-downstream counter; use `ArchitectureClosure` and this report for the
broader A6 gate. Report/matrix/audit JSON use scoped CRLF/UTF-8 formatting;
unrelated global formatting and protected files are untouched.

## 9. Closure handoff

Historical source candidates, shared infrastructure, capability-host
requirements, Roslyn surface, state ownership and canonical-only result boundary
are now concretely inventoried. The minimal remaining blocker is the pair of
upper summary orchestration seams above. **P5O2B cannot yet be authorized by a
READY verdict under the specified hard gates.** Obtain authorization for the
small A6-Fix, verify the two seams close without semantic drift, then rerun A6
readiness. No B, historical package installation, worker/IPC, SCC refactoring,
provider extraction or performance work started. No commit/push/stash/reset.

Final `git status --short` is recorded below and verbatim in the handoff. The protected
`.gitignore`, all Core `bin/obj` WIP paths and all six stash identities/names
remain unchanged; protected file SHA-256 values and complete stash listings
were compared before/after and are identical. JSON parsing, scoped CRLF checks,
PowerShell parsing and `git diff --check` pass.

```text
 M ../../.gitignore
 M Evaluation/ContextualBoundaryAudit/Program.cs
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfo.cs
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfoInputs.cache
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.sourcelink.json
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/ref/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/refint/XMLDocNormalizer.ExceptionFlow.Core.dll
?? Evaluation/ContextualBoundaryAudit/ArchitectureClosureAudit.cs
?? Evaluation/ContextualBoundaryAudit/Write-ClosureEvidence.ps1
?? Evaluation/P5O2A6-architecture-readiness-closure-audit.json
?? Evaluation/P5O2A6-architecture-readiness-closure.md
?? Evaluation/P5O2A6-historical-core-readiness-matrix.md
```
