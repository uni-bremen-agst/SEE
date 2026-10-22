# A5 pre-implementation lifecycle and soundness analysis

## Phase 0 and authorization

Repository test searches included recursion/cycle/SCC/fixpoint, cache/completed/
active/visited/provisional/re-entry, uncertainty, and actual evaluator/graph API
references. Two A4 methods explicitly require lost Fault for caught-first roots:
FiniteMutualCycle_CachedPartialResultDependsOnRootEdgeOrder and
GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness. The user authorized
only their generic=false/true, uncaughtFirst=false empty-result assertions to
become exactly one Fault after a proven repair. All other assertions, including
uncaughtFirst=true, uncertainty, parity/lifetime/isolation, remain unchanged.
Only directly related new witness metadata/output may go to A5. No retrospective
A4 report/audit/witness edits. Any further necessary characterization change
requires another STOP and explicit permission.

Other inspected potential conflicts:

| Test / group | Existing assertion / contract | Classification and intended treatment |
| --- | --- | --- |
| RecursiveCycle_ProducesFinitePathSet | Single path, three steps | Preserve finite cycle-erased witness, not all unrolled recursive walks |
| SharedDiamondGraph_ReusesCompletedNodeResults | 64 retained paths plus truncation for 24-level diamond | Preserve acyclic memoization and existing path cap |
| MoreThanMaximumPaths_AreTruncated | 64 paths and explicit truncation | Preserve; not an SCC iteration budget |
| Graph_GetOrAdd_QueuesCallableExactlyOnce | Same node, one queue entry | Correct construction invariant; preserve |
| RecursiveCallGraph_TerminatesAndRetainsCycle / RecursiveLocalFunction_TerminatesAndRetainsCycle | Actual cyclic graph edges retained | Correct; no graph construction changes |
| RecursiveInvocation_MetadataAndSourcePaths_DoNotCreateDuplicateNodes / Recursion_UsesCanonicalSourceKeysWithoutLooping | Source/metadata canonical key reuse | Correct; preserve |
| A3 ExactConstructedKeys_ChangingTypeRecursionHasNoFiniteReuse_FiniteHarnessOnly | Exact stable reuse vs 32 strictly growing contexts and pending work | Independent unresolved expansion boundary; preserve |
| A3 GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness | Open Fault identity and parity/isolation | Generic identity boundary, not finite cache completion; preserve |
| A2 CompilerModel_ObserveActualSummarySourcesBeforeNormalization | Open source T and collapsed constructed targets | Independent graph identity boundary; preserve all cases |
| A4 RealRecurrences / RoslynComposition | Definition graph/open sources vs exact compiler constructions | Preserve; no generic substitution repair |
| RecursiveCall_PreservesFactsAndTerminates / recursive return-fact tests | Correct known facts, fail-closed unknown return facts | Different analysis domain; preserve |
| ContextualFactEvaluator / DataFlowFactCache / SuccessfulDereferenceCache tests | Guard restoration, positive/negative reuse, model partitions, weak lifetime | Different cache; preserve |
| Historical SolutionScope InvalidGraph (cycle) | Typed rejection before worker start | Project-reference graph validation, not callable SCC; preserve |
| SummaryComponentDependency / SummaryOrchestrationDependency tests | Stateless evaluator, four session fields, graph-only dependencies, no component backlink | Correct architecture; preserve |
| B25/A identity characterizations | Existing name-only normalization/catch behavior | Outside A5 repair; preserve |

## Actual lifecycle (before any production edit)

1. SummaryAnalysisSession.Analyze obtains a semantic model and asks the builder
   to register a root. TargetRegistrar registers source/rebound methods.
2. CallableKey normalizes Symbol to OriginalDefinition and includes ContextKey
   (value facts). This is not an exact generic-instantiation repair.
3. Graph.GetOrAdd creates a mutable empty Summary and queues its key once.
   Registration is a construction placeholder, NOT an evaluated result.
4. BuildPendingSummaryNodes drains the queue, analyzes bodies, marks body
   availability, and transfers finished fragments into summaries. Local typed
   catches remove local sources / annotate edges; catch-all removes protected
   fragments before evaluation. Sources, edges and uncertainty are then fixed
   during the sequential evaluation. HasExecutableBody is not closure state.
5. Evaluate creates completedFrames, activeKeys and an explicit stack afresh
   for EACH ROOT. Neither evaluator nor session has a cross-root result cache.
   Session graph reuse is per semantic environment, sequential/not thread-safe.
6. EnterSummaryEvaluationFrame adds local normalized source paths and copies
   uncertainty. Missing summary/body records unresolved-symbol uncertainty.
7. Active-target edges are skipped, with no dependency/fixpoint bookkeeping.
8. Popping a frame unconditionally inserts that SAME mutable frame in the
   completed dictionary, then copies/prefixes/filters its result into its
   caller. Paths are immutable; result sets and frames are mutable, never
   exposed concurrently by this sequential evaluator. No invalidation, SCC
   detection, update propagation or fixed point exists.

Fault loss: Root enters caught A; A has Fault and enters B; B skips active A;
B is cached empty under key B; A closes with Fault; Root's first edge catches
it; Root's uncaught B edge reuses that empty B, so Root is empty without
uncertainty. A separate Evaluate(B) uses fresh state and finds Fault. This is
same-call sibling cache poisoning, not shared-concurrent/global cache state.

## Domain and candidate contract

Direct/proven and documented sources remain distinct. With the graph frozen,
type normalization and edge catch predicates are fixed. Unions of types and
uncertain-target sets are monotone. Catch exclusion is a fixed edge transfer,
not a later destructive update of an evaluated callee. Local source removal
occurs BEFORE evaluation. Merge copies paths; it does not mutate the callee.

Path concatenation around a cycle creates infinitely many structural paths;
the existing 64-path cap and first-retained representation are NOT a proof of
an unrestricted least fixed point and must not become an iteration budget.
Existing tests require finite cycle-erased witnesses. A proposed equivalent
finite closure must therefore account explicitly for blocked ancestors:

For a frozen graph, let F(v,S) include v's local sources/uncertainty and every
filtered/prefixed F(w,S union {v}) where w is not in S union {v}. Only ancestors
in v's actual SCC can be relevant to this exclusion; a component exit cannot
return (otherwise it would belong to that SCC). Acyclic summaries use S=empty.
For each exception type and fixed edge filter, any escaping walk with a repeated
key has a cycle-erased escaping walk: deleting a cycle removes filters, never
introduces a new filter. Hence finite simple-key paths preserve reachability of
the existing abstract type/uncertainty domain. This is not a proof of concrete
program termination or of the already-known type-identity/generic defects.

not-started = no frame/cache entry for (v,S); evaluating = frame on stack;
provisional = its private accumulated result with remaining dependencies;
closed = every permitted dependency of (v,S) has closed. Only then publish
under (v,S). An ancestor-blocked F(B,{A}) is never reusable as F(B,empty).
No active frame's partial result is returned/read by a recursive edge.

Finite SCC membership and strictly increasing excluded-member sets give a
well-founded expanded dependency DAG (finite subsets, no arbitrary count/depth).
All finished contexts may be cached, including recursive ones. Acyclic shared
diamonds retain key-only-equivalent reuse. State remains per Evaluate call;
separate roots re-evaluate rather than sharing environment-dependent results.
No global cache, generic budget, Roslyn/routing/version change or B25B activation.

Candidate remains conditional on implementation/tests, deterministic behavior,
missing-body uncertainty, unchanged existing characterizations and all eight
Current baseline bytes. No success is claimed by this pre-implementation trace.
