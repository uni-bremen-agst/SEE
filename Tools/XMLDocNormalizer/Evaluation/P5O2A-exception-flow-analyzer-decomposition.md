# P5O2A Exception-Flow Analyzer Decomposition

P5O2A establishes the first reviewable responsibility split of the exception-
flow analyzer and a Roslyn-neutral dependency direction for its durable value
model. It deliberately does not introduce a worker, historical Roslyn build,
IPC, compiler manifest, or version-specific source path.

## Baseline and scope

- Starting HEAD: `cbb99f21f22a1785abedef7e101e597ae4646e62`
- Starting analyzer: 59 partial files and 30,859 audited lines.
- Starting support graph: 11 files and 1,516 audited lines.
- Starting canonical area: 15 files and 4,086 audited lines, including three
  Roslyn adapters.
- The active `ExceptionFlowCallableKey` and summary-graph equality remain
  Roslyn-based and authoritative.

This package extracts four low-risk, representative responsibilities and moves
the existing canonical/value/path sources to a small neutral build boundary.
The remaining highly coupled clusters are recorded rather than redesigned in
one step.

## Audit notation

The matrix uses these dependency abbreviations:

- **R**: Roslyn syntax, symbol, semantic-model, operation, or compilation API.
- **C**: `ProjectClosureSemanticContext` or `SemanticCompilationScope`.
- **G**: summary graph, fragment, source, edge, or graph-session state.
- **S**: supporting-source or cross-compilation resolver.
- **K**: known-framework or external-documentation contract.
- **N**: canonical IR.

All analyzer partials are static. Unless a row explicitly names a field, state
is received through method parameters and local recursion guards. “Key
collaborators” records the important calls or state used across former partial
boundaries; it is not inferred from the old filename alone.

## Responsibility matrix for the 59 starting partials

| Starting partial | Lines | Primary / secondary responsibility | State and key collaborators | Dependencies | Candidate / order |
|---|---:|---|---|---|---|
| `CallContext` | 624 | Build root, call, accessor, and dispatch contexts / map arguments | `GetExpressionValueFacts`, parameter/member fact dictionaries and recursion guards | R | Context builder; later context package |
| `CallContext.MemberSourceFacts` | 311 | Derive stable-member facts from returned object sources / initializers | Call-context creation, source-return and initializer facts | R | Context/stable-member component |
| `ConditionalWeakTableValueFacts` | 630 | Prove `ConditionalWeakTable` value facts / callback returns | Own static weak cache and per-partition lock | R | Dedicated cache owner; next value-facts package |
| `ExceptionFlowAnalyzer` | 463 | Public direct/transitive entry points and local traversal orchestration / explicit throws | Semantic-model lookup, `AnalyzeNode`, invocation and symbol traversal | R,C | Retained orchestrator |
| `DataFlowFacts` | 486 | Roslyn data-flow snapshots / weak semantic-model cache | Own static `DataFlowFactCache`, partitions, locks, region keys | R | Extracted first as `ExceptionFlowDataFlowFactsProvider` |
| `DictionaryValueFacts` | 607 | Dictionary non-null-value proofs / insertion and wrapper reasoning | Data-flow provider, source/value recursion guards | R | Value-facts cluster |
| `EnumSwitchReachability` | 151 | Exhaustive enum fallback reachability / constant collection | Semantic constants and switch arms | R | Condition/reachability component |
| `EnumValueFacts` | 847 | Defined-enum and sequence-enum proofs / list/source propagation | Value-fact recursion guards and source return analysis | R | Value-facts cluster |
| `FrameworkContracts` | 107 | External XML-documentation evidence projection / direct and summary output | `ExternalDocumentationExceptionModel`, path creation | R,G,K | Extracted first as `ExceptionFlowExternalDocumentationEvidence` |
| `ImmutableMembers` | 958 | Immutable field/property value facts / constructor and assignment validation | Value-fact recursion guards, semantic symbol analysis | R | Stable-member/value-facts component |
| `InvocationCallContext` | 268 | Select invocation analysis target and construct context / reduced extensions | Supporting-source resolver and semantic scope | R,C,S | Callable-resolution seam package |
| `Invocations` | 597 | Direct/transitive invocation analysis / delegate factories and known helpers | Context building, recursive dispatch, contracts, path factory | R,C,K | Local source/call expansion component |
| `KnownFrameworkContracts` | 164 | Match/evaluate curated contracts / project positive sources | Known contract model, expression value facts, path factory | R,G,K | Framework-contract evaluator |
| `KnownPropertyValueFacts` | 176 | Curated Roslyn property facts / symbol classification | Symbol identity checks | R | Framework value-facts component |
| `LocalCallables` | 200 | Local function and delegate body traversal / target resolution | `AnalyzeNode`, semantic context, traversal state | R,C | Local source analyzer |
| `LocalInitializerFacts` | 209 | Straight-line local initializer validity / write invalidation | Data-flow provider | R | Value-facts cluster |
| `LocalStablePropertyFacts` | 397 | Stable local object property facts / assignment and initializer analysis | Value-fact recursion guards | R | Stable-member/value-facts component |
| `Nullability` | 849 | Non-null and sequence-element proofs / foreach and framework cases | Return/value/sequence facts and semantic-model lookup | R | Nullability/value-facts component |
| `NullGuards` | 872 | Branch and preceding-guard facts / termination and writes | Data-flow provider, known guard model, condition helpers | R,K | Condition/value-facts component |
| `NumericConditions` | 203 | Positive-int comparison proofs / constant extraction | Expression value facts and condition result enum | R | Condition evaluator |
| `Paths` | 78 | Symbol/location to path-step projection / terminal paths | No owned state | R | Extracted first as `ExceptionFlowPathFactory` |
| `RecursiveDispatch` | 129 | Legacy transitive runtime-dispatch expansion / uncertainty | Dispatch resolution, call contexts, symbol traversal | R,C | Runtime-dispatch package |
| `ReturnNullability` | 309 | Source return non-null proof / framework factory facts | Value facts and inspected-return recursion guard | R | Return/value-facts component |
| `ReturnValueFacts` | 780 | Source return value facts / stable branch facts and framework methods | Value/condition helpers and recursion guards | R | Return/value-facts component |
| `SequenceCallContext` | 403 | Sequence facts carried through calls / mutation invalidation | Call context and source-preservation checks | R | Context/value-facts component |
| `SequenceCollectionFacts` | 473 | List/grouping sequence facts / source projection | Sequence recursion guards | R | Sequence value-facts component |
| `SequenceElementFacts` | 1220 | Element-preserving sequence and dictionary proofs / mutation tracking | Data-flow provider and multiple source fact helpers | R | Sequence value-facts component |
| `SequenceRangeFacts` | 1184 | Range/index sequence safety / count and dictionary proofs | Value/source recursion guards | R | Sequence value-facts component |
| `SequenceSourceHelpers` | 270 | Recognize supported sequence source helpers / argument mapping | Symbol matching | R | Sequence value-facts component |
| `SourcePositionValueFacts` | 503 | One-based and valid source-position facts / source propagation | Value-fact recursion guards and Roslyn source APIs | R | Source-position facts component |
| `SuccessfulCalleeDereferences` | 970 | Derive caller facts from callee successful execution / body traversal | Data-flow facts and inspected-method recursion guard | R | Dereference facts component |
| `SuccessfulDereferences` | 1546 | Successful receiver/member dereference facts / syntax-flow analysis | Own static weak cache, partition lock, cache keys | R | Dedicated cache-backed dereference component |
| `SuccessfulSequenceElements` | 825 | Sequence-element facts from successful operations / foreach conditions | Data-flow provider and sequence helpers | R | Sequence value-facts component |
| `SummaryGraph` | 812 | Graph root creation and callable fragment construction / body discovery | Semantic context, graph, context, all graph analyzers | R,C,G | Summary construction coordinator |
| `SummaryGraphAccessorDispatch` | 355 | Runtime-dispatched accessor edges / exact receiver handling | Dispatch resolver, target registrar, path factory | R,C,G | Accessor/dispatch component |
| `SummaryGraphAccessors` | 704 | Property/indexer/event edges / read-write classification | Context building, target registrar, path factory | R,C,G | Accessor construction component |
| `SummaryGraphAwaits` | 165 | Explicit and await-using awaiter chains / disposal integration | Await dispatch and disposal resolution | R,C,G | Implicit-call construction component |
| `SummaryGraphCalls` | 904 | Invocation and delegate call edges / external evidence | Context, contracts, dispatch, target registrar, path factory | R,C,G,K | Call-edge construction component |
| `SummaryGraphCollectionInitializers` | 778 | Collection initializer constructor/add/accessor edges / context facts | Target registrar and path factory | R,C,G | Collection construction component |
| `SummaryGraphConstructors` | 567 | Constructor and initializer sequencing / base and field initialization | Target registrar and path factory | R,C,G | Constructor construction component |
| `SummaryGraphDeconstruction` | 559 | Deconstruct edges / nested receiver tracking | Target registrar, dispatch, path factory | R,C,G | Deconstruction construction component |
| `SummaryGraphDispatch` | 1488 | Runtime target discovery and dispatch plans / cross-compilation mapping | Semantic scopes, supporting/cross resolver, graph target registrar | R,C,G,S | Runtime-dispatch package; high coupling |
| `SummaryGraphDispatchCompleteness` | 845 | Prove closed runtime target sets / structured uncertainty | Analysis scopes, accessibility, constraints, cross resolver | R,C,G,S | Runtime-dispatch completeness component |
| `SummaryGraphDisposalResolution` | 454 | Resolve interface/pattern disposal / nullable resources | Symbol/type hierarchy and local facts | R | Disposal resolver |
| `SummaryGraphDisposals` | 1132 | Discover resources and emit disposal edges / null and lifetime facts | Disposal resolver, target registrar, path factory | R,C,G | Disposal construction component |
| `SummaryGraphDynamicBindings` | 304 | Detect dynamic binding / structured uncertainty | Operation traversal and summary fragment | R,G | Dynamic-binding component |
| `SummaryGraphEvaluation` | 607 | Evaluate graph transitively / catch suppression and result merging | Own per-root `SummaryAnalysisSession`, graph and evaluation frames | R,C,G | Summary evaluator; separate from construction |
| `SummaryGraphForEach` | 465 | Enumerator/current/disposal edges / conversions | Implicit dispatch and semantic context | R,C,G | Foreach construction component |
| `SummaryGraphImplicitCalls` | 733 | Awaiter and implicit getter edges / speculative binding | Context, target registrar, path factory | R,C,G | Implicit-call construction component |
| `SummaryGraphImplicitDispatch` | 588 | Runtime dispatch for implicit calls / receiver typing | Dispatch resolution, target registrar, path factory | R,C,G | Runtime-dispatch package |
| `SummaryGraphImplicitObjectCreations` | 110 | Target-typed constructor edges / direct-throw exclusion | Context, target registrar, path factory | R,C,G | Object-creation construction component |
| `SummaryGraphOperators` | 1218 | Operator and conversion call edges / operation traversal | Context, target registrar, path factory | R,C,G | Operation-call construction component |
| `SummaryGraphTargets` | 134 | Canonical graph target registration / supporting-source context rebinding | Supporting and cross resolver, semantic scope | R,C,G,S | Extracted first as `ExceptionFlowSummaryTargetRegistrar` |
| `SummaryGraphThrows` | 509 | Summary throw sources and nullability / explicit throw paths | Value/nullability helpers and path factory | R,G | Local summary source analyzer |
| `SummaryGraphTraversal` | 448 | Summary syntax traversal and try/catch suppression / execution boundaries | All graph construction subcomponents | R,C,G | Summary construction coordinator |
| `SymbolTraversal` | 617 | Legacy symbol/object/property traversal / uncertainty | Semantic context, body discovery, invocation analysis | R,C | Local source analyzer |
| `ThrowReachability` | 583 | Boolean/null/string condition reachability / enum fallback | Guard, numeric, value, and context facts | R | Condition/reachability component |
| `TryCatch` | 642 | Direct-analysis catch/filter/rethrow semantics / alias writes | `AnalyzeNode`, result merge, type hierarchy | R,C | Catch semantics component; high criticality |
| `ValueFacts` | 556 | Central expression value-fact dispatch / constants and strings | Immutable/return/sequence/property fact helpers | R | Value-facts coordinator |

## Responsibility clusters and dependency direction

The audit produced 15 cohesive clusters:

1. orchestration and legacy traversal;
2. local body/source analysis;
3. call-context construction and propagation;
4. scalar, member, return, and collection value facts;
5. condition and throw reachability;
6. catch, filter, and rethrow semantics;
7. path and source provenance construction;
8. known-framework contract evaluation;
9. external-documentation evidence projection;
10. summary-graph construction coordination;
11. specialized summary edge construction;
12. callable target and supporting-source resolution;
13. runtime dispatch and completeness;
14. summary-graph evaluation and transitive expansion;
15. canonical/result projection.

The strongest coupling is between clusters 3–5, between summary construction
and specialized edge builders, and between target resolution, runtime dispatch,
and the concrete P6 semantic context. Summary construction and evaluation are
distinct: construction owns nodes, sources, edges, and uncertainty; evaluation
owns traversal frames, catch application, recursion state, and result merging.

The selected order starts with stateless projection, then one evidence
producer, one explicit cache owner, and finally the resolver-heavy graph target
boundary. Catch semantics, runtime dispatch, and the central value-fact web are
deferred because they are both semantically critical and highly coupled.

## Analyzer state and cache matrix

| State | Classification and lifetime | Starting owner | Owner after this slice | Thread safety |
|---|---|---|---|---|
| Data-flow weak cache | Cache; process-wide keys, semantic-model lifetime | Analyzer partial | `ExceptionFlowDataFlowFactsProvider` | Weak partitions with per-partition lock |
| ConditionalWeakTable value cache | Cache; process-wide keys, semantic-model lifetime | Analyzer partial | Unchanged pending value-facts extraction | Weak partitions with per-partition lock |
| Successful-dereference cache | Cache; process-wide keys, semantic-model lifetime | Analyzer partial | Unchanged pending dereference extraction | Weak partitions with per-partition lock |
| `ExceptionFlowTraversalState` | Per-analysis recursion state | Explicit entry-point parameter | Unchanged | One analysis traversal; not shared |
| `ExceptionFlowCallContext` | Immutable per-call/per-root facts | Explicit value object | Neutral core ownership for its value-fact enum; active Roslyn context remains local | Immutable snapshots |
| `ExceptionFlowSummaryGraph` | Per-root graph state and summary cache | Build/session local | Unchanged support owner | One analysis session |
| `SummaryAnalysisSession` | Per-root evaluation state | Nested in analyzer | Unchanged pending evaluator extraction | One session; no global sharing |
| Path construction | Helper-only, stateless | Analyzer partial | `ExceptionFlowPathFactory` | Stateless |
| External evidence projection | Helper-only, stateless | Analyzer partial | `ExceptionFlowExternalDocumentationEvidence` | Stateless |
| Target registration | Resolver state supplied explicitly | Analyzer partial | `ExceptionFlowSummaryTargetRegistrar` | Stateless; context/graph supplied explicitly |

There is no mutable analyzer configuration field. Most apparent “shared state”
was method visibility supplied by the partial class rather than stored state.
The extraction makes four such dependencies explicit without introducing a
general-purpose context object, service locator, dependency-injection framework,
or constructor cycle.

## Extracted components

| Component | Responsibility | Inputs / outputs | Owned state | Dependencies / lifetime |
|---|---|---|---|---|
| `ExceptionFlowPathFactory` | Project Roslyn symbols and syntax locations to neutral path values | symbol, node, step kind → step/path | None | Roslyn-bound, stateless, concurrent |
| `ExceptionFlowExternalDocumentationEvidence` | Add external XML evidence to direct results or summary fragments | invocation, method, semantic model, destination | None | Roslyn-bound contract binding, stateless |
| `ExceptionFlowDataFlowFactsProvider` | Compute and cache exact data-flow snapshots | region and semantic model → immutable facts | Weak cache partitions | Roslyn-bound, concurrent cache owner |
| `ExceptionFlowSummaryTargetRegistrar` | Register exact graph target and rebind supporting-source call context | method, context, semantic environment, graph | None | Roslyn/P6-bound, stateless; explicit remaining seam |

No forwarding wrappers remain in `ExceptionFlowAnalyzer`; callers address the
new owners directly. No dependency cycles or public APIs were introduced.

## Roslyn-neutral core ownership

`XMLDocNormalizer.ExceptionFlow.Core` is a new, narrowly named `net8.0` class
library. It independently compiles the existing single source set for:

- all canonical files whose names begin with `Canonical`;
- canonical namespace documentation;
- `ExceptionFlowValueFacts` and its normalization operations;
- `ExceptionFlowPath`, its persistent deduplication key, and path step;
- `ExceptionFlowPathStepKind`; and
- `ExceptionFlowSourceKind`.

There is no copied canonical IR and no worker DTO identity. The assembly has no
package or project references. An automated test references it through a
test-only assembly alias and asserts that the emitted assembly references only
framework assemblies, not XMLDocNormalizer, Roslyn, Workspaces, or
Microsoft.Build.

The active executable deliberately continues to compile the same source files
in its own compilation; it does not yet reference the neutral assembly at
runtime. A trial runtime `ProjectReference` changed project-scoped self-analysis
from 16 to 70 findings because the current analyzer sees the internal neutral
calls as metadata calls and loses same-compilation value-fact propagation.
General referenced-project rebinding increased the divergence to 129 findings
and was rejected because it would be a precision change rather than a safe
refactor. Keeping one source set compiled at both build boundaries therefore
preserves active semantics without a source copy or conditional compilation.
P5O2B must choose the final assembly composition together with the isolated
analyzer build, rather than silently changing active finding semantics here.

The four P5O2-identified neutral types therefore have explicit ownership:

- value facts: neutral analyzer semantics;
- path step and kind: neutral evidence/provenance values;
- source kind: neutral evidence strength.

`ExceptionFlowPath` and its structural key accompany the path step because the
active result model consumes their internal structural equality. Active result
objects containing Roslyn symbols remain in the main executable.

## Dependency graphs

Before:

```text
XMLDocNormalizer executable
  -> active Roslyn + Workspaces/MSBuild
  -> P3–P6 acquisition and ProjectClosureSemanticContext
  -> 59-partial ExceptionFlowAnalyzer
  -> canonical IR and neutral path/value models (same executable)
```

After this slice:

```text
one neutral source set
  -> compiled by XMLDocNormalizer.ExceptionFlow.Core
  -> compiled by the active XMLDocNormalizer analyzer

XMLDocNormalizer.ExceptionFlow.Core
  -> .NET framework assemblies only
  -> canonical IR + neutral path/value semantics

XMLDocNormalizer executable
  -> active Roslyn + Workspaces/MSBuild
  -> P3–P6 acquisition and ProjectClosureSemanticContext
  -> 55-partial ExceptionFlowAnalyzer
  -> four explicit Roslyn-bound responsibility components
```

The compile-time source sets are now:

- **Neutral core source set:** canonical identity/summary/result/context plus
  value facts, path values, path kinds, and source kind. The same sources are
  presently compiled by both the independent core validation project and the
  active analyzer; there is no copied or divergent source.
- **Roslyn-bound analyzer:** remaining analyzer partials, the four extracted
  components, active graph/result models, canonical Roslyn adapters, contract
  binding, syntax/body analysis, and symbol/type hierarchy logic.
- **Main-only:** CLI, MSBuild/workspace project orchestration, artifact and
  remote acquisition, P3–P5 orchestration, evaluation, and future worker host.

## Remaining seams and deferred clusters

The target registrar intentionally exposes rather than hides the next boundary:
it still accepts `ProjectClosureSemanticContext` and `SemanticCompilationScope`
and calls `SupportingSourceSymbolResolver` and
`CrossCompilationSymbolResolver`. Runtime dispatch also enumerates concrete
semantic scopes. Invocation context selection has the same coupling.

`SyntaxUtils` is used only for member/body discovery in the orchestrator,
summary construction, constructor handling, symbol traversal, and catch
traversal. It should be reduced to a Roslyn-bound body-discovery component in a
later package rather than copied wholesale.

Known-framework contract definitions and binding remain unchanged. Their
symbol matching is Roslyn-bound; no speculative neutral split was made.
External-documentation evidence projection is extracted, while the existing
documentation model and its Roslyn binding remain authoritative.

Recommended follow-up responsibility packages are:

1. **P5O2A2 — semantic scope and callable-resolution seam:** replace direct
   P6 context/scope and resolver use with a small Roslyn-bound analyzer
   environment whose historical implementation can be built without P3–P5
   acquisition code.
2. **P5O2A3 — summary construction and evaluation:** separate the construction
   coordinator, specialized edge builders, and evaluation session without
   changing active graph equality.
3. **P5O2A4 — context, value facts, catch, and local-source analysis:** move the
   remaining cache owners and highly connected semantic clusters only after
   characterization coverage is isolated.

These packages follow actual dependency boundaries, not file counts.

## Structural result

- Remaining analyzer partials: 55.
- Remaining analyzer-partial source lines: 30,124 by the post-change line
  counter.
- Extracted responsibility classes: 4, totaling 763 lines.
- Neutral core source set: 18 files and 3,211 lines.
- Remaining analyzer static caches: two; data-flow caching has a dedicated
  owner.
- Source forks or compiler-version conditionals: none.
- Service locators, DI frameworks, new component interfaces, or cycles: none.

The differing pre/post line counters reflect documentation and formatting in
the extracted files; total LOC reduction is not an objective.

## P5O2B readiness

P5O2B is **not yet ready**. A future isolated build can consume the validated
neutral source set without active Roslyn leakage, but the complete analyzer
source set still requires the main executable because of these exact
dependencies:

1. concrete `ProjectClosureSemanticContext` and `SemanticCompilationScope`;
2. `SupportingSourceSymbolResolver` and `CrossCompilationSymbolResolver` in
   target selection and runtime dispatch;
3. main-owned active result/graph and helper types, including body discovery;
4. source ownership of the remaining analyzer in the executable project; and
5. final neutral-assembly composition must preserve the active analyzer's
   current same-compilation value-fact semantics.

The next blocking dependency is the semantic scope/callable-resolution seam,
not canonical transport. Until that seam is extracted, compiling the same
complete analyzer source set against a manifest-pinned historical Roslyn
closure would still require a reference to the main executable.

## Non-goals retained

- No worker, IPC, process host, compiler manifest, or historical compiler load.
- No preview relaxation or Roslyn-version special case.
- No canonical identity redesign or active graph-equality change.
- No Dapper `release-debug-plus`, OneOf artifact, `Program.Main`, or BCL
  `IOException` work.
- No finding suppression, precision change, cache-policy change, or performance
  optimization.
