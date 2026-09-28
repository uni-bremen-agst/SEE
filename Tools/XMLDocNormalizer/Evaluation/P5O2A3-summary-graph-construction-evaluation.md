# P5O2A3 Summary Graph Construction and Evaluation Decomposition

P5O2A3 separates graph construction, graph evaluation, and session ownership
without changing exception-flow semantics. It does not add a historical
compiler, worker, IPC protocol, source fork, alternate graph representation,
or canonical-key migration.

## Baseline and scope

- Starting HEAD: `1a84e1469cb756e6e8a021c6bb6fd6cc56f7b0ef`.
- Starting analyzer: 55 partial files and 30,092 nonblank lines.
- Audited summary slice: 21 partial files, 12,096 nonblank lines, and 179
  methods under the audit's declaration-counting convention.
- Baseline validation: 2,504 tests; 279 summary-graph tests; build with zero
  warnings and errors; self-analysis 16 findings (`DOC611=1`, `DOC631=15`).
- `ExceptionFlowCallableKey`, the existing graph model, source kinds, catch
  filters, paths, and canonical projection remain authoritative.

The audit classified 172 methods as construction-side work and seven as
evaluation-side work. The 172 construction methods consist of nine root,
queue, and body-discovery methods; 150 specialized edge/dispatch/resolution
methods; five syntax-traversal and catch-fragment methods; and eight local
source methods. Only the cohesive nine-method coordinator moved to the
builder. Moving the other 163 methods would have mixed the P5O2A4
context/value-fact/catch/local-source boundary into this package or created a
10,000-line construction class.

## Summary dependency matrix

`G-R` means graph read, `G-W` graph write, `SE` semantic environment, `CR`
callable resolution, `RD` runtime dispatch, `CV` context/value facts, `C`
catch semantics, `P` path/provenance, and `CP` canonical projection. Analyzer
state is explicit method input unless the row says otherwise; none of these
partials owns mutable analyzer instance state.

| Audited file / method family | Count | Responsibility | Reads/writes | Important dependencies | Final owner |
|---|---:|---|---|---|---|
| `SummaryGraph`: `TryBuildTransitiveSummaryGraph`, root registration, pending queue, declaration/body discovery | 9 | Construction coordination | G-R/G-W | SE, CR, CV | `ExceptionFlowSummaryGraphBuilder` |
| `SummaryGraphAccessorDispatch`: accessor target/dispatch helpers | 5 | Specialized edges | G-W | SE, CR, RD, CV, P | Analyzer construction collaborator |
| `SummaryGraphAccessors`: property/indexer/event helpers | 8 | Specialized edges | G-W | SE, CR, CV, P | Analyzer construction collaborator |
| `SummaryGraphAwaits`: await/awaiter helpers | 2 | Specialized edges | G-W | SE, CR, RD, P | Analyzer construction collaborator |
| `SummaryGraphCalls`: invocation/delegate/external-evidence helpers | 12 | Specialized edges | G-W | SE, CR, RD, CV, P | Analyzer construction collaborator |
| `SummaryGraphCollectionInitializers`: constructor/Add/accessor helpers | 7 | Specialized edges | G-W | SE, CR, CV, P | Analyzer construction collaborator |
| `SummaryGraphConstructors`: explicit/implicit constructor and initializer helpers | 8 | Specialized edges | G-W | SE, CR, CV, P | Analyzer construction collaborator |
| `SummaryGraphDeconstruction`: deconstruction helpers | 6 | Specialized edges | G-W | SE, CR, RD, CV, P | Analyzer construction collaborator |
| `SummaryGraphDispatch`: dispatch plan and runtime-target helpers | 19 | Dispatch integration | G-R/G-W | SE, CR, RD, CV, P | Analyzer construction collaborator |
| `SummaryGraphDispatchCompleteness`: closed-target-set proofs | 13 | Dispatch completeness | G-W | SE, CR, RD | Analyzer construction collaborator |
| `SummaryGraphDisposalResolution`: disposal resolution helpers | 6 | Specialized resolution | none/G-W at caller | SE, CR, CV | Analyzer construction collaborator |
| `SummaryGraphDisposals`: using/foreach disposal helpers | 17 | Specialized edges | G-W | SE, CR, RD, CV, C, P | Analyzer construction collaborator |
| `SummaryGraphDynamicBindings`: dynamic-operation helpers | 9 | Uncertainty construction | G-W | RD, P | Analyzer construction collaborator |
| `SummaryGraphEvaluation`: evaluate, enter, merge, catch, normalize | 7 | Graph evaluation | G-R | C, P, CP; no SE/body scan | `ExceptionFlowSummaryGraphEvaluator` |
| `SummaryGraphForEach`: enumerator/current/disposal helpers | 6 | Specialized edges | G-W | SE, CR, RD, CV, P | Analyzer construction collaborator |
| `SummaryGraphImplicitCalls`: implicit call helpers | 7 | Specialized edges | G-W | SE, CR, RD, CV, P | Analyzer construction collaborator |
| `SummaryGraphImplicitDispatch`: implicit runtime dispatch helpers | 4 | Dispatch integration | G-W | SE, CR, RD, CV, P | Analyzer construction collaborator |
| `SummaryGraphImplicitObjectCreations`: target-typed creation helpers | 2 | Specialized edges | G-W | SE, CR, CV, P | Analyzer construction collaborator |
| `SummaryGraphOperators`: operator/conversion helpers | 19 | Specialized edges | G-W | SE, CR, RD, CV, P | Analyzer construction collaborator |
| `SummaryGraphThrows`: explicit/rethrow/nullability source helpers | 8 | Local source analysis | G-W | CV, C, P | Analyzer, deferred to P5O2A4 |
| `SummaryGraphTraversal`: node/try/catch/descendant traversal helpers | 5 | Construction traversal | G-W | SE, CV, C | Analyzer, deferred to P5O2A4 |
| `ExceptionFlowSummaryTargetRegistrar.Register` | 1 | Exact target registration and context rebinding | G-W | SE, CR | Existing stateless registrar |

The specialized rows are cohesive as graph edge production but not as one
independent class today: their methods depend extensively on analyzer-owned
context/value-fact, catch, and local-source helpers. They remain stateless
construction collaborators until P5O2A4 rather than being copied one-for-one
from partial files into artificial classes. The target registrar remains only
a registrar.

## Graph data model and ownership

| Concern | Existing representation | Owner / key / lifetime | Thread safety and invalidation |
|---|---|---|---|
| Graph nodes and summary cache | `ExceptionFlowSummaryGraph` dictionary of `ExceptionFlowSummary` | Session; keyed by `ExceptionFlowCallableKey`; one sequential detector/tool run | Not shared; grows monotonically; discarded with session |
| Pending construction | Graph pending queue | Builder consumes, graph owns; session lifetime | Sequential; no explicit invalidation |
| Callable context | Graph call-context map of `ExceptionFlowCallContext` | Graph; same callable key and session lifetime | Immutable context values; sequential map mutation |
| Direct sources | `ExceptionFlowSummary.Sources` / `ExceptionFlowSummaryFragment` | Summary/fragment; graph lifetime | Merged during construction only |
| Call edges | `ExceptionFlowSummaryCallEdge` | Summary; target key, call-site path step, caught types | Immutable edge data after construction |
| Uncertainty | Summary/fragment unresolved-target sets; result uncertainty set | Construction records; evaluation unions and adds missing-body/summary evidence | Per graph/result; no global collection |
| Catch filters | Edge `CaughtExceptionTypes`; fragment filtering during body traversal | Constructed by existing catch traversal; consumed by evaluator | Existing type-matching semantics unchanged |
| Evaluation memoization | `completedFrames` keyed by `ExceptionFlowCallableKey` | Evaluator-local, per root | Discarded after one `Evaluate` call |
| Recursion state | `activeKeys` and explicit frame stack | Evaluator-local, per root | Sequential and nonrecursive CLR traversal |
| Final result | `ExceptionFlowAnalysisResult` | One result per evaluated root | Produced only by evaluator |

The graph is per session, not per evaluator. Multiple roots reuse completed
constructed summaries through the session graph. Evaluation frames and the
completed-frame memo are deliberately per root, so caller-side catch filters
and path prefixes cannot leak between roots.

## Extracted components

### `ExceptionFlowSummaryGraphBuilder`

The sealed concrete builder owns construction coordination for one
`ExceptionFlowSemanticEnvironment`: root registration, pending-node draining,
declaration/body discovery, fragment merging, and delegation to the existing
specialized edge producers. It owns only the immutable environment reference;
the session owns the graph. Its lifetime is one session and it is intentionally
sequential/non-thread-safe. It uses only the P5O2A2 semantic seam.

### `ExceptionFlowSummaryGraphEvaluator`

The sealed concrete evaluator interprets an already completed graph. It owns
no fields and cannot reach an `ExceptionFlowSemanticEnvironment`, syntax node,
semantic model, member declaration, or builder through its API. Per-call state
contains the explicit stack, active recursion keys, and completed-frame memo.
It preserves path prefixing, cross-compilation type normalization, catch-edge
filtering, missing-body uncertainty, and result accumulation.

### `ExceptionFlowSummaryAnalysisSession`

The sealed concrete session composes exactly the semantic environment, one
graph, one builder, and one evaluator. It registers/builds/evaluates each root
and reuses only graph construction state across roots. One session belongs to
one sequential detector or tool run and is not thread-safe.

### `ExceptionFlowUncertaintyRecorder`

This stateless helper owns the existing symbol-to-stable-display-name rule.
Both legacy traversal and graph evaluation now call the same nonvirtual method;
the uncertainty representation and messages are unchanged.

No interface, virtual member, service locator, constructor guard, global
mutable collection, or new graph type was introduced. The analyzer retains two
orchestration methods rather than forwarding the extracted internal method
surface.

## Dependency direction

Before:

```text
detector/tool
  -> ExceptionFlowAnalyzer partial host
       -> semantic resolution
       -> graph construction + specialized edges + local traversal
       -> session + graph evaluation
       -> final result
```

After:

```text
detector/tool
  -> ExceptionFlowAnalyzer orchestration
       -> ExceptionFlowSummaryAnalysisSession
            -> ExceptionFlowSummaryGraphBuilder -> existing specialized
               construction collaborators -> target registrar
            -> ExceptionFlowSummaryGraph
            -> ExceptionFlowSummaryGraphEvaluator -> analysis result
```

There is no dependency from evaluator to builder, session, syntax scanning, or
semantic resolution, and no dependency from builder to evaluation result or
evaluator state. The retained specialized construction collaborators still
call analyzer-owned context/value-fact/catch/local-source helpers; that is the
explicit P5O2A4 boundary, not hidden session state.

## Characterization and semantic preservation

The pre-refactor 279 summary-graph tests already characterize direct and
transitive calls, A-to-B-to-A recursion, constructors, accessors, operators,
await, foreach and disposal, collection initializers, deconstruction, implicit
calls and object creation, dynamic and runtime dispatch, unresolved targets,
catch propagation, context-sensitive keys, truncation, and diamond
memoization. They passed before and after extraction. Four reflection-based
tests additionally guard the new dependency direction, sealed/non-interface
shape, graph-only evaluator, construction/evaluation separation, and exact
session-owned state.

Same-compilation, referenced-project, supporting-source, and metadata-only
resolution continue to use `ExceptionFlowSemanticEnvironment` and
`ExceptionFlowSummaryTargetRegistrar`. No graph key, cache key, dispatch plan,
catch test, path factory, source-kind projection, or canonical operation was
changed.

## Rejected variants

- A single manager containing all 12,000 summary-slice lines was rejected as a
  God class.
- One class per current partial file was rejected because filenames do not
  define responsibilities and this would preserve coupling under new names.
- Moving the 150 specialized-edge methods now was rejected as an unreviewable
  P5O2A4-crossing migration.
- Interfaces, virtual dispatch, delegate-based callback seams, and dependency
  injection were rejected because there is no polymorphic requirement and
  analyzer-visible runtime dispatch previously changed evidence.
- A combined build/evaluate session context was rejected because it would let
  evaluation reach semantic lookup and construction state.
- New graph, canonical key, fixpoint, cache, and catch implementations were
  rejected as semantic changes.

## Size and dependency result

- Analyzer partials: 55 before, 54 after.
- Analyzer nonblank lines: 30,092 before, 28,841 after.
- Extracted nonblank lines: builder 728, evaluator 434, session 80,
  uncertainty recorder 32 (1,274 total).
- Direct analyzer references to `ProjectClosureSemanticContext`,
  `SemanticCompilationScope`, `SupportingSourceSymbolResolver`, and
  `CrossCompilationSymbolResolver`: zero before and after.
- Evaluation has no main-project orchestration or acquisition dependency.

The line totals are architectural measurements, not optimization targets. The
important change is that graph interpretation is now independently owned and
cannot acquire Roslyn-body discovery dependencies through its type surface.

## P5O2A4 boundary

The following remain intentionally in the analyzer:

- context creation and propagation, including same-compilation value facts;
- scalar, member, sequence, dictionary, nullability, and dereference facts;
- try/catch/filter/rethrow discovery while constructing local fragments;
- explicit throw, implicit local source, and body traversal semantics;
- specialized edge producers that consume those facts;
- runtime-dispatch completeness logic coupled to those construction facts.

P5O2A4 should extract those responsibilities without changing the builder,
graph key, evaluator, or canonical result semantics. It should also decide the
final owner of specialized edge collaborators after their context and local
analysis dependencies are explicit.

## P5O2B readiness and boundary status

P5O2A3 improves P5O2B readiness by giving graph construction and graph
evaluation concrete owners and by making the evaluator independent of semantic
lookup and source scanning. P5O2B is not ready: P5O2A4 and final assembly
composition are still required, and active graph/result types remain
Roslyn-bound by design. BND-P6-003 therefore remains **Under Investigation**.

## Validation

- Focused summary slice: 285/285 including the new architecture guards; the
  exact pre-existing `ExceptionFlowSummaryGraph` slice remains 279/279.
- Self-analysis before: 16 findings in 86,863 ms.
- Self-analysis after extraction: 16 findings in 78,935 ms.
- Canonical finding comparison: zero added, zero removed, zero changed
  evidence (`DOC610=0`, `DOC611=1`, `DOC631=15`, `DOC632=0`).
- Construction/evaluation sub-durations are not available without invasive
  instrumentation; no performance conclusion is drawn from individual runs.

- Four-mode-focused regression slice: 72/72. Direct,
  ProjectTransitiveDeclaredExceptions, ProjectTransitive, and
  SolutionTransitive behavior is also covered by the full suite.
- Full suite: 2,508/2,508, zero failed, zero skipped (2,504 baseline plus four
  P5O2A3 architecture guards). This includes the P5G/P5I/P5J/P5K/P5L,
  P6A/P6B/P6C, G3A/G4B/G5, P5O1, P5O2A, and P5O2A2 regressions.
- E1 with Source Link, verified line endings, and bounded remote references:
  seven candidates, five full reconstructions, two expected fail-closed, zero
  unexpected failures, and zero potential bugs. Dapper remains
  `ConfigurationReconstructed / ConfigurationUnsupported` for
  `optimization=release-debug-plus`; OneOf remains
  `PdbValidated / MissingArtifact`.
- The first sandboxed E1 attempt could not download Source Link documents and
  produced five environmental `SourceUnavailable` outcomes with zero downloaded
  bytes. The identical network-enabled retry produced the expected result.
- Final build: zero warnings and zero errors. `git diff --check` passed. All
  changed production/test C# files are CRLF-only UTF-8 without BOM, contain no
  bare LF/CR or trailing whitespace, and introduce no `var` declaration.
- No new native or managed process crash or test flake was observed. Historical
  unattributed P5O1/P5N crash observations remain distinct; no shared cause is
  inferred.
