# P5O2A5A - Contextual Evaluator Boundary Audit

Date: 2026-10-06. Audited HEAD: **31e8f7751cafac955b0a7df3c13e7197ee7b5b21**
(`31e8f7751c Separate delegate target resolution ownership.`).

**A5B is architecturally ready.** Move the intact 63-method SCC together with its
weak invariant-cache field and complete partition implementation. The partition
adds two method declarations, so the concrete move set is **65 method declarations**,
not a claim that the SCC contains 65 methods. No preparatory extraction is needed.
This package performs the audit only; A5B has not started.

## 1. Scope, baseline and reproducibility

The initial gate checked status, diff/stat/full diff, HEAD and last commit before
task edits. A4G is committed. The initial worktree contained only the protected
`../../.gitignore` WIP and seven tracked Core `bin/obj` churn files. They were not
edited, restored, staged or repaired. All six existing stashes were only read;
no commit, push or stash operation was performed.

No production or test C# implementation, project reference, solution composition,
provider, interface, facade, cache or SCC method was changed. The one new C# project
is a standalone **audit executable**, not an evaluator and not a solution member.
Its own `.gitignore` excludes its build outputs and explicitly exposes its project
file; it does not modify the protected repository ignore rules.

A small persisted tool was necessary: the earlier temporary audit used a fixed
historical graph/type list and invocation-only counting. That is insufficient for
this package's constructor, property, state, initializer and delegate questions.
The tool binds the current executable compilation with the same pinned Roslyn 5.0.0
and MSBuild package versions, collects normalized source symbols and computes Tarjan
SCCs. A separate small PowerShell script adds reviewed role/ownership annotations
and renders complete matrices. Neither participates in production or test compilation.

Run from `Tools/XMLDocNormalizer`:

~~~powershell
dotnet build Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -warnaserror
dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a5a/measured-boundary.json
dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a5a/measured-boundary-repeat.json
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/ContextualBoundaryAudit/Write-Evidence.ps1
dotnet format Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj --no-restore --verify-no-changes
~~~

The execution-policy switch is process-local; no machine/user policy is changed.
Raw comprehensive measurements are ignored under `artifacts/p5o2a5a`. Two independent
final measurement runs are byte-identical (SHA-256 recorded in the JSON validation).
The renderer imports local TRX results when present; graph-only reproduction does
not manufacture a new test measurement. The report retains the actual runs below.

Evidence:

- [Machine-readable audit](P5O2A5A-contextual-evaluator-boundary-audit.json):
  fully qualified method signatures, return types, internal callers/callees,
  all ingress/egress, full graph SCCs, source dependency graph, state readers,
  writers, mutations, initializers, local structures, parameters and ownership.
- [Complete boundary matrices](P5O2A5A-contextual-evaluator-boundary-matrices.md):
  every SCC method, all 16 ingress edges, all 81 direct egress owners with actual
  API signatures/counts, field ownership and all expanded source type edges.
- [Audit implementation](ContextualBoundaryAudit/Program.cs) and
  [evidence renderer](ContextualBoundaryAudit/Write-Evidence.ps1).

The JSON is authoritative for overload-distinct signatures and per-method external
dependencies; the matrices use unambiguous shortened namespace notation. M01..M63
identify this exact sorted method set, not the old report's N01..N63 numbering.

## 2. Actual SCC and graph definitions

| Measurement | Current result |
|---|---:|
| Source method declarations in active executable compilation | 1,569 |
| Unique directed bound method-invocation edges between those declarations | 2,660 |
| Contextual SCC methods / internal directed invocation edges | **63 / 112** |
| Expanded source-callable SCC, including constructors/accessors/method references | **63** |
| Added / removed historical member signatures | **0 / 0** |
| Added / removed historical internal edges | **0 / 0** |
| Ingress edges / distinct callers / distinct SCC entry targets | **16 / 15 / 4** |
| Full direct egress edges | **791** |
| Reachable explicit source callables / source types | **284 / 33** |
| Analyzer partial files / nonblank SLOC | **40 / 18,316** |

The primary graph is all actual `MethodDeclarationSyntax` nodes and uniquely bound,
normalized `InvocationExpressionSyntax` caller/callee pairs; reduced extension
methods and original definitions are normalized. It is not limited to the old
252-node graph. Its twelve nontrivial SCC sizes are 63, 11, 10, 3, 3 and seven 2s.
The other SCCs are outside the contextual extraction set; all memberships are
recorded, not silently included in A5B.

The broader audit includes source constructors/initializers, accessors, local
functions, bound method references and referenced field initializer targets.
Lambda dependencies are attributed to the enclosing callable; local function
bodies have their own nodes. This broader method graph still does not enlarge
the contextual SCC. The exact signatures AND 112 edge pairs agree with the
historical residual audit: no membership is retained merely because an old report
listed it.

All 63 methods remain declared by `ExceptionFlowAnalyzer`, across these partials:

| Partial suffix | SCC methods |
|---|---:|
| CallContext | 2 |
| ConditionalWeakTableValueFacts | 3 |
| ReturnNullability | 2 |
| DictionaryValueFacts | 4 |
| EnumValueFacts | 7 |
| SequenceCallContext | 2 |
| SequenceRangeFacts | 12 |
| SequenceElementFacts | 7 |
| ValueFacts | 3 |
| LocalStablePropertyFacts | 4 |
| ImmutableMembers | 6 |
| ReturnValueFacts | 3 |
| Nullability | 5 |
| SequenceCollectionFacts | 3 |

The complete membership/caller/callee matrix is the companion appendix; exact
A5B membership is `A5BProposal.ExactSccMethodSet` in JSON.

## 3. Ingress and facades

Every direct production ingress caller is currently Analyzer-owned. There are
no direct external-provider ingress calls. Full signatures, declaration locations,
call sites and roles appear in the complete 16-row matrix.

**A - Orchestration: 14 edges from 13 methods.** These retain their orchestration
and only redirect the measured fact call to the evaluator:

- `CreateAccessorCallContext`: two edges, to `AddExplicitArgumentFacts` and
  `GetExpressionValueFacts`; combines accessor/setter arguments.
- `CreateInvocationCallContext`: invocation/runtime-target context assembly.
- `CreateKnownFrameworkContractArguments`: framework argument-contract assembly.
- `CreateSummaryCollectionInitializerCallContext`,
  `CreateSummaryImplicitCallContext`, `CreateSummaryOperationCallContext`:
  corresponding Summary-edge context creation.
- `EvaluateNullComparison`, `EvaluateNullPattern`,
  `EvaluatePositiveInt32Comparison`, `EvaluateStringPredicate`:
  condition/reachability reasoning that consumes expression facts.
- `GetSummaryCollectionArgumentFacts`: collection argument/params/default adapter.
- `IsThrowExpressionInExhaustiveDefinedEnumFallback` and
  `IsThrowExpressionProvenUnreachable`: exception/branch reachability.

**B - Thin seed facades: 2 edges from 2 methods.**

- `CreateCallContext(IMethodSymbol, SeparatedSyntaxList<ArgumentSyntax>,
  SemanticModel, ExceptionFlowCallContext)` creates a fresh symbol guard and
  forwards to the five-argument SCC overload.
- `IsDefinitelyNonNull(ExpressionSyntax, SemanticModel,
  ExceptionFlowCallContext)` creates a fresh return-symbol guard and forwards
  to the four-argument SCC overload.

These are not literally zero-logic forwarders: each owns one guard allocation.
They fit the thin evaluation-seed category, not exception orchestration. A5B can
retain them as temporary Analyzer facades; A5C can remove them after moving the
same seed allocation into evaluator convenience overloads. Do not silently omit,
share or change the guard. **C - Other ingress: none.**

The four production entry targets needing internal visibility in a minimal
evaluator API are exactly:

~~~csharp
CreateCallContext(IMethodSymbol, SeparatedSyntaxList<ArgumentSyntax>,
    SemanticModel, ExceptionFlowCallContext, HashSet<ISymbol>)
AddExplicitArgumentFacts(IMethodSymbol, SeparatedSyntaxList<ArgumentSyntax>,
    SemanticModel, ExceptionFlowCallContext,
    Dictionary<int, ExceptionFlowValueFacts>, HashSet<int>, HashSet<ISymbol>? = null)
GetExpressionValueFacts(ExpressionSyntax, SemanticModel, ExceptionFlowCallContext)
IsDefinitelyNonNull(ExpressionSyntax, SemanticModel,
    ExceptionFlowCallContext, HashSet<ISymbol>)
~~~

No public service API is required; 59 other SCC implementation methods can be
private. Existing test/reflection visibility expectations must be reviewed during
A5B without enlarging the production dependency boundary.

## 4. Complete egress and the historical counter discrepancy

A5A's complete direct egress comprises **397 property getters + 366 invocations +
28 constructions = 791 kind-labelled caller/callee edges**, across 81 owners.
There are 189 source-target edges and 602 metadata-target edges. This includes
Roslyn/BCL APIs rather than counting only selected extracted providers.

The source partition is exact:

- **126 invocations** to 19 named fact/resolver helper owners below.
- **19 invocations** to the two `ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree`
  overloads: 18 with a `SemanticModel`, one with a `Compilation`.
- **18 edges** to `ExceptionFlowCallContext`: eight invocations and ten constructor
  edges (three constructor overloads).
- **23 invocations** to `ExceptionFlowValueFactsExtensions.Normalize/ContainsAll`.
- **3 edges** to the current Analyzer-nested invariant-cache partition:
  two invocations (`TryGetValue`, `Store`) and its implicit constructor.

| Fact/resolver owner (all prefixed ExceptionFlow) | SCC invocation edges |
|---|---:|
| ArgumentMapper | 6 |
| CallContextFactProjector | 4 |
| ConditionalWeakTableValueFactsProvider | 4 |
| DelegateTargetResolver | 1 |
| DereferenceFactDiscovery | 5 |
| EnumValueFactsProvider | 7 |
| GuardFactsProvider | 6 |
| ImmutableMemberValueFactsProvider | 7 |
| KnownPropertyValueFactsProvider | 1 |
| LocalInitializerFactsProvider | 3 |
| NullabilityFactsProvider | 8 |
| PrimitiveValueFactsProvider | 4 |
| RuntimeDispatchClassifier | 5 |
| SequenceCollectionFactsProvider | 24 |
| SequenceContentPreservationFactsProvider | 11 |
| SourcePositionValueFactsProvider | 4 |
| StableMemberFacts | 2 |
| SuccessfulSequenceValidationFactsProvider | 1 |
| SymbolUsageFacts | 23 |

The complete owner matrix gives every used overload, exact edge count, field state,
acquisition route, next source owners, Analyzer-reference finding and historical
label, including the remaining 58 metadata owners. The JSON gives each edge's
source method and site. The downstream graph is explicit, not a type-name inventory.

**Do not repeat 94/18 as the complete current census.** They are the historical
A4G report projection. Fresh bound-code measurement includes technical helpers,
all overloads and state support. In particular, the Compilation scope overload is
a concrete nineteenth edge. The old component-total report does not supply a
complete matching edge-level census from which 94 can be reproduced as a total;
A5A supersedes that claim with 126/19 under a stated definition. No code changed
to obtain these numbers, and the exact SCC graph is unchanged. Mixed measurement
scopes must not be presented as a semantic regression or new production dependency.

## 5. Downstream composition, callbacks and cycles

There is **no direct or indirect downstream fact/resolver call back to Analyzer
or its orchestration helpers** in the expanded source closure. The only reachable
Analyzer-owned non-SCC implementations are the cache partition's `TryGetValue`
and `Store`; they are support state, not a new fact family. Keeping that partition
in Analyzer would violate the future evaluator-to-Analyzer rule, so it must move
together with the SCC, including its implicit constructor and initializers.

The SCC has 17 lambda sites (recorded with source text) and no direct outward
method-group edge. They filter syntax/search references or create the invariant
partition; none transfers Analyzer orchestration into a lower component.
`IsCallbackReturnDefinitelyNonNull` resolves an analyzed **source program's**
callback with the A4G resolver; this is not an Analyzer callback dependency.

The transitive DataFlow cache does use delegates. Production composition was
checked concretely:

1. `dataFlowFactCache = new()` selects its parameterless constructor.
2. That constructor delegates to `this(ComputeDataFlowFacts)`, binding the
   provider's own static Roslyn calculator.
3. `partitionFactory = CreatePartition` binds the cache's own factory.
4. Partition computation invokes that immutable calculator; no Analyzer target
   or contextual evaluator is supplied.

Its injection-capable internal testing constructor is not used to inject an
Analyzer in production. This is not a reason to introduce evaluator callbacks.

The expanded **type-level** graph has one real two-type composition SCC:
`ExceptionFlowDataFlowFactsProvider ↔ DataFlowFactCache`. It is the provider's
own cache/calculator composition. Counting explicit method references reveals
it; an invocation-only metric misses it. When nested implementation types are
collapsed to their top-level semantic owners, **inter-component cycles are zero**.
Both graphs and the exact cycle membership remain in JSON; the cycle is not erased
or misreported as universally zero. It does not cross the proposed evaluator cut.

Static binding is not runtime points-to analysis. Metadata bodies and arbitrary
virtual/dynamic targets are not traversed. The two known cache comparers were
therefore also inspected directly: they are stateless singleton implementations,
with no Analyzer reference. Their `Equals/GetHashCode` use exact syntax reference
identity, query/region mode and, for dereference keys, Roslyn symbol equality.
Those Dictionary-dispatched comparer bodies are not inferred as ordinary bound
call edges. This explicit composition review closes the relevant hidden routes
without claiming a whole-program runtime proof.

## 6. State ownership and exact A5B state set

Every directly referenced field/constant, source-field access in the reachable
closure, mutable local structure and SCC parameter is inventoried. JSON separates
reference reads, assignment writes and mutation through readonly collection
receivers; a readonly cache field is not described as immutable merely because
the reference cannot be reassigned. Initializers and source sites are retained.

| Concrete element | Current owner | Recommended A5B owner | Category / reason |
|---|---|---|---|
| 63 exact SCC implementations | Analyzer | ContextualFactEvaluator | Fact evaluation / context projection |
| `conditionalWeakTableValueFactCaches` | Analyzer, static readonly | ContextualFactEvaluator, same static lifecycle | A: exclusively SCC-used memoization root |
| `ConditionalWeakTableValueFactCachePartition`, `gate`, `entries`, `TryGetValue`, `Store` | Analyzer nested type | ContextualFactEvaluator nested support | A: exclusively SCC/cache support, move whole type |
| `inspectedValueSources`, `inspectedReturnSymbols`, `inspectedImmutableMembers`, `inspectedSequenceSources`, `inspectedDictionarySources`, `inspectedDictionaries` | Seed/facade/recursive invocation | Evaluator evaluation chain; temporary facade may allocate | B: per-chain guards, not persistent shared state |
| `invariantInspectedValueSources` | SCC local | Evaluator local | B: independent field-seeded invariant evaluation |
| `knownParameterFacts`, `suppliedParameterIndexes` supplied to `AddExplicitArgumentFacts` | Accessor caller / SCC caller scratch | Typed caller scratch across cut | F: synchronous per-call shared output, no Analyzer object |
| `knownNonNullParameterMembers`, callback `parameterFacts`, returned-expression lists | SCC locals / stateless provider results | Evaluator invocation locals | C: temporary fact/context projection, not memoization |
| `callerContext`, `callContext`, constructor/initializer/return/callee/callback contexts | Domain object / SCC local | Explicit domain value, evaluator local as appropriate | C: context snapshot, no domain-type move |
| `ExceptionFlowCallContext.parameterFacts`, `nonNullParameterMembers` | CallContext | CallContext | C: constructor-copied and normalized; shared domain owner |
| CallContext `CallableSymbol` and `Key` auto-property state | CallContext | CallContext | C/D: constructor-derived original symbol / immutable legacy identity |
| SemanticModel, Compilation, syntax, symbols, SymbolEqualityComparer.Default, enum/flag constants | Roslyn / semantic / neutral domain owners | Explicit shared dependencies, same owners | D: no evaluator ownership or cache re-scope |
| `successfulDereferenceCaches` and partition `gate/entries` | DereferenceFactDiscovery | Same provider | F: shared mutable provider memoization, not SCC-exclusive |
| `dataFlowFactCache`, `partitions`, partition `gate/entries` | DataFlowFactsProvider | Same provider | F: shared mutable provider memoization, not SCC-exclusive |
| DataFlow `calculator`, `partitionFactory`, partition `semanticModel`; stateless comparer `Instance` properties | Respective provider/cache | Same owner | D: immutable production bindings / model dependency / comparers |
| Traversal, exception paths, Summary graph/session, runtime target orchestration | Analyzer / established orchestration components | Same owners | E: no direct SCC state intersection; never move |

The exact moved field list has **three fields**: the weak-table root plus the
partition's `gate` and `entries`. Two cache support method declarations move
in addition to 63 SCC bodies; no provider cache moves. The complete per-element
field matrix records direct readers/writers, other Analyzer accesses, exclusivity,
type, lifetime, initialization and invalidation. Parameter/local matrices record
their actual declarations and receiver operations, not hypothetical persistent fields.

CallContext constructors copy/normalize parameter dictionaries and member-symbol
sets; member symbol equality is `SymbolEqualityComparer.Default`. They derive
`CallableSymbol` from the original definition and `Key` via the existing legacy
key builder. Context query methods read these snapshots; SCC evaluation does not
mutate them. Existing read-only collection exposures must retain their behavior;
A5B is not permission to change cloning/identity semantics. Its `RebindCallable(Func<...>)`
API exists but is not on the SCC closure and is not required by this cut.

The shared scratch case (F) is narrow and explicit: accessor orchestration owns
the destination dictionary/set; `AddExplicitArgumentFacts` fills them, and the
existing projector/default mapping reads or supplements them before context
construction. Preserve the typed synchronous arguments; do not store an Analyzer,
invent a bag, share scratch across calls or turn scratch into a global cache.

## 7. Cache and recursion semantics

### Evaluator-owned CWT invariant cache

- Outer key: **SemanticModel reference identity**, weak `ConditionalWeakTable`.
  Value: `ConditionalWeakTableValueFactCachePartition`.
- Inner key: normalized field **OriginalDefinition**, `SymbolEqualityComparer.Default`.
  Value: **bool** (both successful and unsuccessful invariant results).
- It is already **static**, initialized once with the Analyzer type. It is not
  per Analyzer instance. Partitions are created on first model use; their retained
  symbols stay within that semantic world's weak ephemeron lifetime.
- No explicit reset/invalidation. A different semantic model/Compilation receives
  a distinct partition; model collection releases the partition. Do not replace
  this with a global symbol dictionary, instance cache or fresh-per-fact cache.
- No call-context key: only the context-neutral invariant of a private static
  readonly source CWT is memoized. The receiver shape, empty initialization and
  all source field uses/factory return expressions are checked. Cross-tree analysis
  uses the same Compilation through the concrete SemanticScope overload.
- Ambient guard `Add(normalizedField)` happens **before cache lookup** and a
  duplicate fails closed. Removal occurs in `finally`. Uncached invariant
  computation gets its own fresh symbol guard already seeded with that field,
  rather than inheriting ambient context-dependent facts.
- Table operations are concurrency-safe; per-partition locks protect lookup and
  `TryAdd` store, not Roslyn evaluation. Parallel immutable computations may
  duplicate work; the first stored answer is retained. There is no global
  in-progress marker and no evaluator recursion under a cache lock.

### Provider-owned caches (not moved)

| Cache | Complete key / value | Equality / lifecycle / concurrency |
|---|---|---|
| `successfulDereferenceCaches` | Model; (exact ExpressionSyntax, ISymbol, SuccessfulDereferenceQueryMode) → ExceptionFlowValueFacts | Weak model identity; syntax ReferenceEquals/RuntimeHelpers hash; SymbolEqualityComparer.Default; exact mode. Static table, first-use partition, no reset/context key. Lock lookup/store, compute outside. |
| `dataFlowFactCache` | Model; DataFlowRegionKey(exact StatementSyntax/ExpressionSyntax, DataFlowRegionKind) → (Succeeded, ImmutableArray<ISymbol> WrittenInside) | Weak model identity; syntax ReferenceEquals/RuntimeHelpers hash; exact overload kind. Static owner cache, first-use partition, no reset/context key. Partition lock includes first AnalyzeDataFlow computation. |

Compilation/model identity supplies the semantic scope of both providers' keys;
neither cache gets a new context dimension in A5B. DataFlow calculator/factory
bindings and retained partition semantic model remain under their provider's
weak-partition lifecycle. The comparer singleton instances are stateless, immutable
type-lifetime support and do not move.

### Guards and local memoization distinction

The six named symbol-set parameter families and their seed/copy locals use
`HashSet<ISymbol>(SymbolEqualityComparer.Default)`. Active symbol/method/field
normalization remains exactly as current code chooses. Guard mutation sites use
Add/Remove with `try/finally`; copied guards in expression/return analysis preserve
the inherited inspected set rather than sharing or clearing it accidentally.
Nullable `inspectedValueSources` in argument projection selects an existing guard
or creates a fresh one. The CWT invariant seed is deliberately independent.

These guards are recursion/reentrancy state, not successful-result caches.
Do not merge guard families, change comparer, persist a guard between roots,
move it into static state, remove protective copies or change the fail-closed
duplicate result. Each evaluation chain assumes sequential use of its scratch
collections; cache synchronization does not make shared caller guards parallel-safe.

## 8. Context roles versus actual entry seeds

The historical four Context-labelled SCC methods are confirmed:

| SCC method | Current role | A5B ownership |
|---|---|---|
| `CreateCallContext(..., HashSet<ISymbol>)` (5 arguments) | Recursive context construction/projection | Evaluator |
| `AddExplicitArgumentFacts(...)` (7 arguments) | Expression/sequence fact projection into argument scratch | Evaluator |
| `AreSequenceElementsProvenNonNull(...)` (3 arguments) | Fresh-guard sequence evaluation seed | Evaluator |
| Same method (4 arguments) | Recursive contextual sequence proof | Evaluator |

None runs an exception-path/summary/traversal operation. They belong to the
connected evaluation component. **Four Context roles are not the four exported
entry targets.** The latter additionally include `GetExpressionValueFacts` and
`IsDefinitelyNonNull`, and do not include the sequence overloads (no current
outside-SCC caller).

Other genuine SCC seeds include the three-argument expression evaluator,
`AreDictionaryValuesProvenNonNull`, `AreSequenceElementsProvenDefinedEnumValues`,
`IsRangeSourceProvenToExcludeNullElements`, foreach non-null helpers and invariant
evaluation. Their local guard allocations are documented in the local-state
matrix. They stay in the evaluator even when only internal evaluation calls them.
Two outside-SCC guard-seed facades may temporarily remain Analyzer-owned.
The 13 orchestration callers stay outside, not in the evaluator.
Also outside the SCC are `CreateRootCallContext` (top-level callable context),
`CreateDispatchCallContext` and `CreateDispatchTargetContext` (compile-time/runtime
parameter-ordinal rebinding). They are Analyzer context orchestration, not direct
SCC ingress targets or thin A5C fact facades; keep them with their current owners.

## 9. Concrete A5B boundary and dependency rules

Recommended implementation shape: **internal static**
`ExceptionFlowContextualFactEvaluator`, retaining current static semantics.

There is no required service constructor or new registration. Composition is
explicit typed method input: SemanticModel and its Compilation, Roslyn syntax
and symbols, ExceptionFlowCallContext, exact recursion guards, and the typed
argument-projection scratch structures. Named static fact/resolver helpers are
direct dependencies, not hidden behind injected delegates or interfaces.
A SemanticScope *instance*, its lazy `sourceTypes`, Summary session, options bag,
AnalysisSession, runtime-target plan or Analyzer reference is not required.

Exact move plan:

1. All signatures in `A5BProposal.ExactSccMethodSet` (63) as one unit, retaining
   internal recursion and all local guard/context logic.
2. `conditionalWeakTableValueFactCaches` and the entire
   `ConditionalWeakTableValueFactCachePartition` nested type, including implicit
   constructor, initializers, synchronization fields and its two methods.
3. The four measured production entry signatures internal; remaining implementation
   methods private. Retain only temporary measured-signature facades if necessary
   for a mechanical call-site transition; no evaluation body/cache remains in Analyzer.
4. Keep both outside-SCC seed facades initially. A5C can move their unchanged
   seed allocation into evaluator convenience overloads and remove forwarding.
5. Keep all 13 orchestration callers and orchestration state in existing owners;
   redirect only their actual contextual-evaluation calls.

Allowed named dependencies are all 19 fact/resolver owners, SemanticScope's two
static resolution overloads, CallContext and ValueFacts/ValueFactsExtensions, plus
the concrete Roslyn/BCL APIs enumerated in the egress matrix. Indirect DataFlow
and dereference provider caches retain their existing owners/lifecycles.

Forbidden:

- Evaluator → Analyzer references/calls, including private helper leakage.
- Analyzer callbacks, captured Analyzer delegates or method groups to disguise
  a reverse dependency.
- Moving orchestration state, Summary/session/runtime-target ownership or shared
  Compilation into the evaluator.
- A service locator, generic dependency bag, new provider/interface abstraction
  merely to conceal these concrete dependencies.
- Provider-cache moves, cache/guard lifecycle/key/comparer changes, performance
  redesign or historical worker/compiler/IPC work.

These constraints are reachable with the measured graph. The current nested
invariant-cache support is part of the A5B move, **not** a missing prepackage.
There is no unresolved mixed-ownership long-lived mutable Analyzer field
requiring another preparation slice.

## 10. Atomic extraction assessment

**Yes, retain the atomic SCC decision.** A nonempty proper subset of a strongly
connected component cannot be dependency-closed: a partition has paths, and hence
crossing edges, in both directions. Moving such a subset to a separate owner
while the rest remains Analyzer-owned introduces a backward path/type-owner
cycle unless evaluation semantics is redesigned or callbacks are introduced.

No proper subset of these 63 methods is independently extractable under the
zero-Analyzer-return rule. The two cache support methods are not SCC members;
their complete state ownership still makes moving them with the SCC the clean
cut. Provider-local DataFlow composition is independent of this decision.

No code evidence calls for breaking the SCC internally before its move.
A future internal factoring would be a separate architectural/semantic change,
not a prerequisite or permission in A5A.

## 11. Historical-build relevance labels only

| Dependency family | Label / later concern |
|---|---|
| SCC plus invariant partition, guards | Analyzer-Core/future evaluator; current source uses Roslyn symbols/syntax, so not automatically portable |
| SemanticModel, Compilation, IOperation/symbol/syntax/semantic APIs | Current Roslyn API: bind/compile in one selected type universe during later P5O2B |
| Named fact/resolver providers, SemanticScope, CallContext | Own semantic/domain components, many explicitly Roslyn-bound; build closure must include exact APIs, not only class names |
| ValueFacts flags/extensions and already canonical own representations | Own Roslyn-neutral single-source/domain pieces where actually neutral; do not equate Roslyn-bound CallContext with its canonical representation |
| CWT/Dictionary/HashSet/LINQ/locks/comparers | BCL runtime infrastructure; preserve semantic world and identity/lifecycle |
| Root exception traversal, Summary/session/runtime-target orchestration, solution/project/CLI composition | Main-process/orchestration infrastructure outside this evaluator cut; separate A6/P5O2B questions |

The executable still compiles the active Roslyn-bound source set locally. No
Historical-Roslyn API compatibility claim, compiler layer, worker boundary or IPC
design was introduced. A5B readiness does not claim P5O2B build/runtime readiness.

## 12. Validation and final readiness

- No production/test-source, production project-reference or solution change.
  Remaining tracked C# diff is only pre-existing generated Core AssemblyInfo churn.
- Current semantic compilation: **0 errors**, workspace diagnostics empty.
- Existing architecture/ownership/dependency guard slice: **23/23**, no skips.
  Includes FactComponentDependency, SemanticDependencyGuard, CoreDependency,
  SummaryComponentDependency and LocalAnalysisComponentDependency tests.
- Full existing suite, rerun because standalone C# audit infrastructure was added:
  **final retry 2,547/2,547**, no failures/skips. Initial full run: 2,546 passed,
  one known `MultiModuleAssembly_FailsClosed` failure. Its intervening isolated run
  also failed (0/1), then the unchanged full retry passed. This is observed known
  foreign baseline behavior, not a repaired issue or evidence that isolation passed.
- Standalone audit project warning-as-error build: **0 warnings / 0 errors**.
- Audit-project format gate: pass. No global formatting cleanup.
- Two independent final graph measurements: byte-identical SHA-256.
- Evidence JSON syntax, method/edge counts, state categories, exact historical
  member/edge equality and generated matrix consistency verified.
- No fresh production solution build: standalone tool is outside the solution;
  immediate A4G warning-as-error production build remains **0/0**. Avoided unrelated
  tracked Core output churn.
- **Self analysis / canonical diff not rerun**, exactly as requested for no
  productive/test-compiled behavioral change. Retained, not newly measured:
  A4G **16 findings** (DOC610=0, DOC611=1, DOC631=15, DOC632=0);
  canonical **0 added / 0 removed / 0 changed evidence**, normalized arrays exactly
  equal, retained canonical hash
  `15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B`.
- Protected repository ignore WIP, tracked Core churn, six stashes, known global
  format deviations and the independent MultiModule flake were not repaired.

**P5O2A5B is architecturally ready. Minimal prerequisite: none.**
A5B must move the complete 63+2 implementation/state set, preserve the static
weak-cache and per-chain guard semantics, keep orchestration outside and validate
the zero-return-edge/facade and behavior gates. No A5B implementation, commit,
push or stash change was made here.

