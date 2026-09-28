# P5O2A4C — Residual Call Context and Value Fact SCC Decomposition

## Outcome

P5O2A4C completed the requested method-level audit of the residual 63-method
strongly connected component (SCC), but deliberately made no production-code
change. The mandatory STOP rule applies.

The common responsibility is **recursive contextual fact evaluation**: a fact
query can inspect a same-compilation callee under a newly projected call
context, while constructing that context must evaluate the caller's argument
and receiver facts. The recursion is legitimate and should ultimately be owned
inside one concrete component.

Moving only the 63 methods does not produce that architecture. It creates a
component cycle because the SCC has both incoming calls from Analyzer-owned
upper methods and outgoing calls to Analyzer-owned lower fact methods. Removing
that cycle requires moving an additional complete side of the dependency
boundary. The smaller proven side is the downstream closure: 163 methods in the
original 252-node graph, of which 124 still belong to
`ExceptionFlowAnalyzer` and 39 already belong to the P5O2A4B components. The
SCC also directly calls two Analyzer summary helpers outside the 252-node
inventory. Extracting the remaining downstream closure together with the SCC
would be a non-reviewable fact-system rewrite in this package; moving the upper
side would spread into summary construction. Both outcomes are explicitly
forbidden by the package rules.

The safe result is therefore an exact architecture finding, not a cosmetic
class move. P5O2B is not ready.

## Repository gate and scope

- Initial HEAD: `c26fb517d33557cf7559d9879a130351e04ccd84`
  (`Reduce exception flow context and fact coupling.`).
- Initial status: only the protected foreign `M ../../.gitignore`.
- P5O2A4B was committed, so the P5O2A4C repository gate passed.
- No commit, push, checkout, restore, reset, or stash operation was performed.
- The protected `WIP full position provenance before P1 extraction` and
  `WIP known-framework position provenance` stashes remain unchanged.
- The P5O2A4B graph was used as the authoritative input. The other 189 methods
  were traversed only to determine the SCC's entry, exit, upstream, and
  downstream closures.
- `Program.Main`, `IOException`, canonical identity, graph keying, context
  keying, catch semantics, local traversal, summary evaluation, historical
  compiler execution, and worker construction were not changed.

## Authoritative audit artifact

[`P5O2A4C-residual-scc-audit.json`](P5O2A4C-residual-scc-audit.json) contains
all 63 methods. Every record contains:

- file, line, and complete overload-resolving signature;
- primary responsibility and semantic role;
- all callers and callees inside the SCC;
- incoming and outgoing graph edges outside the SCC;
- direct callers and callees outside the 252-method inventory;
- context read and context creation, fact read and fact computation;
- recursive entry, recursive exit, mutual recursion, and self recursion;
- cache, `SemanticModel`, symbol, and query-mode dependencies; and
- the candidate ownership layer.

The artifact is a focused extension of
`P5O2A4B-call-graph-after.json`; it does not duplicate the other 189 nodes.
There is no separate “after” graph because production calls did not change.

## Exact SCC baseline

| Metric | P5O2A4B baseline | P5O2A4C final |
|---|---:|---:|
| SCC methods | 63 | 63 |
| internal edges | 112 | 112 |
| graph-tagged Context methods | 4 | 4 |
| graph-tagged Fact methods | 59 | 59 |
| semantic context-construction methods | 2 | 2 |
| semantic value-fact methods | 61 | 61 |
| Context-to-Fact internal edges | 6 | 6 |
| Fact-to-Context internal edges | 6 | 6 |
| entry nodes | 4 | 4 |
| exit nodes | 53 | 53 |
| self-recursive methods | 8 | 8 |
| method SCCs represented | 1 | 1 |

The graph tags count both `AreSequenceElementsProvenNonNull` overloads as
Context because their source file owns context projection. Semantically they
are value-fact queries, which is why the original package description correctly
states two context-construction and 61 value-fact methods.

The four entry nodes are:

1. `CreateCallContext` (guard-preserving overload);
2. `AddExplicitArgumentFacts`;
3. `IsDefinitelyNonNull` (guard-preserving overload); and
4. `GetExpressionValueFacts` (public-to-cluster overload).

There are 12 incoming edges from the remaining 252-node graph and four direct
incoming calls from summary-construction sources outside that inventory. There
are 138 outgoing edges to the remaining graph and 39 direct outgoing calls to
sources outside that inventory.

The eight self-recursive methods are:

- `GetDefinedEnumValueFacts`;
- `IsSequenceExpressionProvenToContainOnlyDefinedEnumValues`;
- `TryGetStablePropertyFactsFromLocalObjectSource`;
- `IsDefinitelyNonNull`;
- `IsSequenceExpressionProvenToExcludeNullElements`;
- `GetSourceReturnExpressionValueFacts`;
- `IsGroupingSequenceProvenToContainNonNullElements`; and
- `GetExpressionValueFacts` (guard-preserving overload).

The SCC has one undirected articulation point:
`GetExpressionValueFacts(ExpressionSyntax, SemanticModel,
ExceptionFlowCallContext, HashSet<ISymbol>)`. Forty-six nodes are strong
articulation points. The largest internal hubs are that four-argument
`GetExpressionValueFacts` overload, the null-sequence query, the defined-enum
sequence query, `IsDefinitelyNonNull`, `GetDefinedEnumValueFacts`, and the
guard-preserving sequence-context query. This reinforces that the SCC is not a
pair of context methods connected by one accidental back edge.

## Responsibility classification

| Role | Methods | Meaning in this SCC |
|---|---:|---|
| A. Context Construction | 1 | materialize a callee context from evaluated caller facts |
| B. Fact Query | 2 | stable entry queries into contextual evaluation |
| C. Fact Computation | 46 | scalar, member, collection, dictionary, return, and source derivation |
| D. Recursive Interprocedural Evaluation | 6 | create a nested callee context from source-return/sequence discovery |
| E. Representation/Projection | 1 | project explicit argument facts by parameter ordinal |
| F. Cache Lookup | 1 | query the existing weak CWT invariant cache |
| G. Stable/Scalar Primitive Facts | 6 | immutable-member computations participating in contextual recursion |

All 63 methods participate in the same mutual recursion. “Primitive” in the
table describes the local computation performed by six methods; it does not
mean they can be extracted independently from the SCC.

The fact families are: CWT non-null (3), dictionary (4), defined enum (7),
immutable member (6), local stable property (4), nullability (5), return
nullability (2), return value (3), sequence context/query (2), collection (3),
sequence/dictionary elements (7), range/alias/storage (12), central value-fact
dispatch (3), and context construction/projection (2).

## Actual recursion semantics

The cycle is:

```text
upper context/fact query
  -> evaluate argument or receiver under current ExceptionFlowCallContext
  -> inspect same-compilation source return / sequence / dictionary behavior
  -> construct the callee ExceptionFlowCallContext
  -> evaluate the callee's arguments and receiver facts
  -> inspect nested source behavior
  -> return a conservative fact or terminate through the existing guards
```

`ExceptionFlowCallContext` is sufficient as the immutable query input. Its
effective read surface remains callable identity, parameter-ordinal value facts,
and parameter-ordinal stable non-null members. A second query-context type would
copy that complete surface and would not remove any recursive
`CreateCallContext` edge, so the rejected P5O2A4B query-context design remains
rejected.

Call-context construction and recursive evaluation are distinguishable roles,
but not separable components under the current method placement. Fact query and
fact computation are likewise distinguishable, but the computation recursively
re-enters context construction.

## Dependency closure and STOP proof

Within the authoritative 252-node graph, the SCC partitions the remaining
methods exactly as follows:

```text
17 upstream methods
  -> 63-method recursive contextual fact SCC
       -> 163 downstream methods
            -> 124 methods still in ExceptionFlowAnalyzer
            -> 39 methods in P5O2A4B extracted components

9 remaining methods are unrelated to this reachability partition.
```

The SCC has 71 unique direct downstream targets: 62 Analyzer methods and nine
already extracted component methods. Its source also directly reaches two
Analyzer summary helpers outside the 252-node inventory,
`RequiresSummaryRuntimeDispatch` and `TryResolveDelegateTarget`.

The proof that a smaller SCC move is unsafe is structural:

1. Any proper non-empty subset of a strongly connected directed graph has at
   least one incoming and one outgoing edge across the cut. Splitting the 63
   methods between context and fact components therefore creates a component
   cycle.
2. Moving all 63 together still leaves `ExceptionFlowAnalyzer -> evaluator`
   through the 17 upper methods and summary callers, and
   `evaluator -> ExceptionFlowAnalyzer` through the 124-method downstream
   closure and two summary helpers.
3. Moving only the 62 direct Analyzer targets does not help: those methods
   transitively depend on the rest of the 124-method downstream closure and
   would reproduce the same cycle one layer lower.
4. The acyclic target shape is valid only after the lower closure has cohesive
   owners:

```text
upper queries / call-context entry points
  -> contextual fact evaluator (internal 63-method recursion)
       -> scalar/member/collection/return primitive providers
       -> existing P5O2A4B helpers
```

That target contains no context/fact component cycle, but reaching it in this
package requires a broad 124-method family decomposition before the SCC can
move. This meets the explicit STOP conditions “necessary fact-system rewrite”
and “non-reviewable big-bang.” No callback network, service locator, interface
layer, virtual dispatch, duplicate cache, or temporary component cycle was
introduced.

## Ownership, state, lifetime, and caches

No new production component was created. Current ownership remains:

| Concern | Current owner | State / lifetime / thread safety |
|---|---|---|
| call-context construction | residual `ExceptionFlowAnalyzer` cluster | call-local immutable contexts and recursion guards |
| fact query and contextual computation | residual `ExceptionFlowAnalyzer` cluster | call-local guards; active `SemanticModel` and context inputs |
| recursive interprocedural evaluation | implicit inside the residual Analyzer SCC | call-local recursion; no global session |
| argument mapping | `ExceptionFlowArgumentMapper` | stateless, safe for concurrent calls |
| symbol usage | `ExceptionFlowSymbolUsageFacts` | stateless; consumes the DataFlow provider |
| stable member classification | `ExceptionFlowStableMemberFacts` | stateless |
| dereference discovery | `ExceptionFlowDereferenceFactDiscovery` | one weak per-`SemanticModel` cache with existing partition locks |
| data flow | `ExceptionFlowDataFlowFactsProvider` | one weak per-`SemanticModel` cache with existing partition locks |
| CWT invariant facts | residual `ExceptionFlowAnalyzer` cluster | one weak per-`SemanticModel` cache with existing partition lock |

The CWT cache did not receive a natural independent owner because one of its
lookup methods is in the 63-method SCC. Moving it alone would split cache and
computation ownership. Its key, value, weak partition, lock, lifetime,
invalidation, and compilation isolation remain unchanged. The dereference cache
remains solely owned by `ExceptionFlowDereferenceFactDiscovery`; its syntax,
normalized-symbol, and query-mode key is unchanged. No new cache exists.

`ExceptionFlowCallContext`, `ExceptionFlowValueFacts`, call-context equality,
hashing, normalization, canonical projection, summary-cache keying, and
canonical transport remain unchanged.

## Origin semantics

- **SameCompilation:** source body, semantic model, argument, receiver, stable
  member, sequence, dictionary, return, dereference, and recursion behavior are
  unchanged.
- **ReferencedProject:** existing source/scope and fact boundaries are
  unchanged.
- **SupportingSource:** exact semantic scope and source ownership are
  unchanged.
- **MetadataOnly:** no facts are synthesized; conservative fail-closed behavior
  is unchanged.

## Characterization and architecture guards

No new behavior test was added because the STOP decision retained no production
change. The required pre-structural characterization already exists and passed:

| Required behavior | Test ownership |
|---|---|
| nested A→B→C context | `DOC611_CallContextValueFactDependencyTests` |
| recursive A→B→A termination/fact preservation | `DOC611_CallContextValueFactDependencyTests` |
| same method, distinct call-site contexts | `DOC611_CallContextValueFactDependencyTests` and call-site tests |
| argument fact → callee context | `DOC611_CallSiteValueFactsTests` |
| receiver fact → callee context | `DOC611_ReducedExtensionCallContextTests` |
| stable member over context boundary | `DOC611_ParameterMemberCallContextTests` |
| return fact → outer query | `DOC611_SourceReturnCallContextTests`, `ExceptionFlowReturnValueFactsTests` |
| sequence fact over callee | `DOC611_SequenceElementCallContextTests` and source/range suites |
| dictionary fact over callee | `DOC611_DictionarySequenceRangeFactsTests` |
| cache and compilation isolation | `ExceptionFlowSuccessfulDereferenceCacheTests` and DataFlow cache tests |

The existing P5O2A, P5O2A2, P5O2A3, P5O2A4, and P5O2A4B dependency guards
remain green. Direct Analyzer references to
`ProjectClosureSemanticContext`, `SemanticCompilationScope`,
`SupportingSourceSymbolResolver`, and `CrossCompilationSymbolResolver` remain
zero. Neutral Core and Canonical Core remain Roslyn-free at their established
boundaries.

## Structural measurements

- Analyzer partials: 47 before, 47 after.
- Analyzer nonblank SLOC: 24,225 before, 24,225 after.
- Production components added: zero.
- Production SLOC extracted: zero.
- New tests: zero; baseline and final suite count remain 2,520.
- New audit artifacts: this report and one focused 63-node JSON audit.

## Validation

- Pre-change recursive/context/fact characterization: 51/51.
- Final recursive/context/fact characterization: 51/51.
- Four-mode-focused slice: 20/20, covering Direct,
  ProjectTransitiveDeclaredExceptions, ProjectTransitive, and
  SolutionTransitive test classes.
- Broad External/Canonical/Dependency slice: 856/856. This covers the existing
  P5G/P5I/P5J/P5K/P5L, P6A/P6B/P6C, G3A/G4B/G5, canonical, semantic seam,
  neutral-core, summary, local-analysis, and fact-component regressions under
  their production test classes.
- Full suite: 2,520/2,520, zero failed, zero skipped.
- Build: `dotnet build .\XMLDocNormalizer.sln -warnaserror --no-restore` passed
  with zero warnings and zero errors.
- Self Analysis before: 16 findings (`DOC610=0`, `DOC611=1`, `DOC631=15`,
  `DOC632=0`).
- Self Analysis after: 16 findings in 54,956 ms with the same distribution.
- Canonical Finding Diff: zero added, zero removed, zero changed evidence. The
  final normalized finding/evidence SHA-256 is
  `15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B`.
- E1 canonical profile (`SourceLink=enabled`, `VerifiedLineEndings`,
  `BoundedRemoteArtifacts`): seven candidates, five complete reconstructions,
  two expected fail-closed cases, zero unexpected failures, zero potential
  bugs. Dapper remains `ConfigurationReconstructed /
  ConfigurationUnsupported` for `optimization=release-debug-plus`; OneOf
  remains `PdbValidated / MissingArtifact`. Semver retains its one expected
  source-backed difference.
- The first sandboxed E1 run produced five environmental `SourceUnavailable`
  outcomes with zero downloaded bytes. The approved network-enabled rerun
  produced the canonical 5/2/0/0 result.
- `Program.Main` remains the single DOC611 finding. The known `IOException`
  case remains DOC631 and was not changed.
- `git diff --check` passes apart from the expected line-ending warning for the
  protected foreign `../../.gitignore` modification.
- No C# file changed, so the C# CRLF/BOM/bare-LF/bare-CR/trailing-whitespace and
  `var` gates are vacuously unchanged.
- No native crash, managed crash, modal dialog, or test flake was observed.
  Historical P5O1/P5N observations remain unrelated and unattributed.

## Completion-criteria disposition

The audit criteria are complete: all 63 methods, 112 internal edges, entries,
exits, roles, state, caches, recursion semantics, and the exact dependency
closure are recorded. Semantic preservation and every requested validation are
green.

The implementation criteria are intentionally **not** claimed:

- call-context construction, fact query, fact computation, and recursive
  evaluation do not yet have four acyclic concrete owners;
- the recursive owner is known conceptually but was not created;
- the 63-method SCC remains inside `ExceptionFlowAnalyzer`; and
- Analyzer dependency separation is therefore not complete.

Creating a nominal evaluator now would violate the no-component-cycle rule.
Moving the SCC plus its required downstream closure would violate the no-big-
bang rule. The STOP rule takes precedence over the nominal completion list.

## P5O2B readiness and next step

`BND-P6-003` remains **Under Investigation** and P5O2B is **not ready**. The
exact single blocker is now sharper than “the 63-method SCC”:

> `ExceptionFlowAnalyzer` co-locates the SCC's 17-method upper dependency side
> with its 124-method lower dependency closure, so moving the SCC to its correct
> recursive owner necessarily creates a component cycle.

Do not create an artificial P5O2A4D or P5O2A5. If this work is authorized to
continue, extend P5O2A4C itself with staged, family-by-family extraction of the
124 downstream Analyzer methods into cohesive primitive fact providers. Each
slice must build, run focused tests, remeasure the SCC/component graph, and keep
Self Analysis and canonical evidence unchanged. Once the downstream closure no
longer points back to `ExceptionFlowAnalyzer`, move the intact 63-method
recursive algorithm to one sealed/nonvirtual contextual fact evaluator. Only
then should P5O2B perform assembly composition and the dual-version historical
Roslyn build.

## Requested close-out matrix

1. HEAD: `c26fb517d33557cf7559d9879a130351e04ccd84`.
2. Initial status: protected `M ../../.gitignore` only.
3. Scope: residual 63-method SCC audit and safe-cut decision.
4. Baseline: 63 methods, one SCC.
5. Internal edges: 112.
6. Entry nodes: 4.
7. Exit nodes: 53.
8. Context methods: 2 construction; 4 graph-tagged with sequence queries.
9. Fact methods: 61 semantic; 59 graph-tagged.
10. Primitive methods: 6 stable/scalar-role methods, none independently
    extractable from the SCC.
11. Recursive fact methods: all 61 participate; 6 directly create/re-enter a
    callee context.
12. Context→Fact edges before/final: 6/6.
13. Fact→Context edges before/final: 6/6.
14. Root cause: same-compilation source fact evaluation recursively projects a
    callee context whose arguments require contextual fact evaluation.
15. Responsibility: recursive contextual fact evaluation.
16. Call-context owner: residual Analyzer, unchanged.
17. Fact-query owner: residual Analyzer, unchanged.
18. Fact-computation owner: residual Analyzer plus existing leaf components.
19. Recursive owner: concept identified; concrete extraction stopped.
20. New production components: none.
21. Component responsibilities: unchanged; table above.
22. State per component: unchanged; table above.
23. Lifetimes: call-local or weak `SemanticModel` partitions, unchanged.
24. Thread safety: existing stateless/partition-lock behavior unchanged.
25. Dependency before: upper Analyzer → SCC → lower Analyzer/components.
26. Dependency after: unchanged.
27. Component cycles: none introduced; the cyclic candidate was rejected.
28. SCC size before: 63.
29. SCC size after: 63.
30. Remaining nontrivial method SCCs: 63, 10, 3, and 2; the latter three
    remain internal to their established fact owners.
31. ArgumentMapper: separate, stateless owner retained.
32. SymbolUsageFacts: separate, stateless owner retained.
33. StableMemberFacts: separate, stateless owner retained.
34. DereferenceFactDiscovery: separate cache/computation owner retained.
35. DataFlowFactsProvider: separate cache owner retained.
36. CWT cache owner: residual Analyzer.
37. Dereference cache owner: DereferenceFactDiscovery.
38. Cache keys: unchanged.
39. Cache lifetimes: unchanged weak semantic-model partitions.
40. Cache locks: unchanged private partition locks.
41. SameCompilation: unchanged.
42. ReferencedProject: unchanged.
43. SupportingSource: unchanged.
44. MetadataOnly: unchanged/fail closed.
45. Nested context: green.
46. Recursion: green and terminating.
47. Multiple call sites: green and isolated.
48. Return-fact recursion: green.
49. Sequence-fact recursion: green.
50. Dictionary-fact recursion: green.
51. Stable-member propagation: green.
52. Cache isolation: green.
53. Architecture guards: green in the 856-test slice/full suite.
54. Old four blocker count: zero each.
55. Neutral Core guard: green.
56. Canonical Core guard: green.
57. Partials before/after: 47/47.
58. Analyzer SLOC before/after: 24,225/24,225.
59. Extracted SLOC: zero.
60. New tests: zero because no production slice survived the STOP gate.
61. Test count before/after: 2,520/2,520.
62. Focused tests: 51/51.
63. Direct: green.
64. ProjectTransitiveDeclaredExceptions: green.
65. ProjectTransitive: green.
66. SolutionTransitive: green.
67. P5/P6/G regression: 856/856 broad slice and full suite green.
68. E1: 7/5/2/0/0.
69. Dapper: expected `release-debug-plus` fail closed.
70. OneOf: expected exact-PDB fail closed.
71. Program.Main: unchanged DOC611.
72. IOException: unchanged DOC631.
73. Self Analysis before: 16.
74. Self Analysis after: 16.
75. Canonical diff: 0 added, 0 removed, 0 changed evidence.
76. Full Suite: 2,520/2,520.
77. Build: 0 warnings, 0 errors.
78. `git diff --check`: green except protected `.gitignore` warning.
79. Line endings: no C# changes.
80. Crashes/flakes: none.
81. Architecture documentation: this report plus focused JSON audit.
82. OPEN-PIPELINE-BOUNDARIES: updated.
83. BND-P6-003: Under Investigation.
84. Analyzer dependency separation: not complete.
85. Remaining assembly composition: not started; still follows dependency
    separation.
86. P5O2B readiness: not ready.
87. Exact remaining blocker: upper and lower Analyzer ownership around the SCC.
88. Recommended next step: staged downstream-provider extraction within
    P5O2A4C, then intact SCC ownership; no new package label.
89. Historical worker/build: not created.
90. Commit/push/stash changes: none.
91. Final status: reported separately at handoff after format checks.
