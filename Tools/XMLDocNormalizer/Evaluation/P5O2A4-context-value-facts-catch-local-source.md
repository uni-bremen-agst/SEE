# P5O2A4 Context, Value Facts, Catch, and Local Source Decomposition

## Outcome and scope decision

- Starting HEAD: `70c233f56c42e0e99778b19b1b950ef20cd17ef2`.
- Initial working tree: only the foreign `../../.gitignore` modification.
- Starting analyzer: 54 `ExceptionFlowAnalyzer` partial files and 28,841
  nonblank lines.
- Baseline: 2,508 tests, 564 focused characterization tests, a zero-warning
  build, and 16 self-analysis findings.
- P5O2A4 performed the requested complete responsibility and dependency audit.
  The audit proved that call-context construction and value-fact discovery are
  one densely mutually recursive Roslyn semantic cluster. Splitting that
  cluster into nominal context and fact services would introduce the explicitly
  forbidden cyclic architecture or require an unreviewable big-bang rewrite.
- The retained smallest safe coherent slice therefore extracts catch semantics
  and direct local source/body analysis. Summary-local traversal was trialed,
  but its dispatcher and specialized edge producers are mutually recursive; it
  remains in `ExceptionFlowAnalyzer` rather than retaining a new dependency
  cycle.
- Call context and value facts are fully classified below, but remain in their
  authoritative implementation. Their two caches were not moved because cache
  access is part of the same recursive cluster and no semantic-neutral acyclic
  owner boundary was demonstrated.

This is a natural P5O2A4 split under the package STOP rule, not a declaration
that the original four-domain package is fully complete. The retained code is
the safe Catch/Direct-Local slice; the exact remaining blocker is recorded in
the P5O2B readiness section.

## Resume audit and phase status

At the continuation audit, the working tree already contained the first
Catch/Direct-Local/Summary-Local extraction attempt, the slim analyzer entry
facade, and build churn. Call Context and Value Facts had been measured but not
moved; neither fact cache had moved. The build and 564 focused tests were green,
and the first post-extraction self analysis had already reproduced all 16
findings with zero canonical diff. No P5O2A4 test file or architecture report
yet existed.

The continuation removed the forwarding facade, removed every reverse edge
from remaining analyzer partials to Direct Local, moved recursive direct
dispatch to the direct owner, restored the unsafe Summary-Local trial to the
existing analyzer partial cluster, added five architecture guards, created this
report, and updated the boundary registry. No safe retained Catch/Direct-Local
work was discarded.

| Phase | Final status | Evidence |
|---|---|---|
| A. Responsibility/state audit | Complete | method-family matrix, call graph, fields and caches classified |
| B. Call Context | Partial by STOP rule | 27 methods classified; no semantic move because of measured fact cycle |
| C. Value Facts | Partial by STOP rule | 225 methods and two caches classified; representation unchanged |
| D. Catch Semantics | Complete | leaf owner, no mutable state, characterization and canonical diff green |
| E. Local Source Analysis | Complete for direct local sources | acyclic owner; summary-local recursive cluster intentionally retained |
| F. Dependency guards/integration | Complete for retained slice | five new guards plus prior semantic/summary guards |
| G. Final regression | Complete | focused, modes, P5/P6, self, full suite, E1, build and format below |

## Responsibility audit

All listed methods are static. Unless stated otherwise, they own no analyzer
instance state; semantic state arrives through `SemanticModel`,
`ExceptionFlowSemanticEnvironment`, `ExceptionFlowCallContext`, traversal
state, graph, fragment, or result parameters.

| Audited file or method family | Methods | Primary responsibility | Secondary responsibility and state | Important dependencies | Final owner |
|---|---:|---|---|---|---|
| `ExceptionFlowAnalyzer.CallContext.cs` | 10 | create root, invocation, member, and dispatch contexts | transfer parameter, receiver, optional/default, and stable-member facts; no static state | semantic model, scalar/member/return facts | `ExceptionFlowAnalyzer` pending context/fact cluster redesign |
| `ExceptionFlowAnalyzer.CallContext.MemberSourceFacts.cs` | 5 | discover source-member facts for contexts | stable member identity and source lookup | semantic environment, value facts | `ExceptionFlowAnalyzer` |
| `ExceptionFlowAnalyzer.InvocationCallContext.cs` | 5 | bind invocation target and supporting-source scope | preserve target provenance | semantic environment/scope and supporting-source method | `ExceptionFlowAnalyzer` |
| `ExceptionFlowAnalyzer.SequenceCallContext.cs` | 7 | propagate sequence receiver/argument facts | collection-element context | sequence and scalar facts | `ExceptionFlowAnalyzer` |
| scalar/value partials (`Dictionary`, `Enum`, `Immutable`, `KnownProperty`, `LocalInitializer`, `LocalStable`, `Nullability`, `NullGuards`, `NumericConditions`, `ReturnNullability`, `ReturnValueFacts`, `SourcePosition`, `ThrowReachability`, `ValueFacts`) | 110 | discover scalar, constant, nullability, initializer, stable-member, return, and reachability facts | recursive inspection guards are call-local | call context, semantic model, data-flow facts | `ExceptionFlowAnalyzer` |
| collection partials (`SequenceCollection`, `SequenceElement`, `SequenceRange`, `SequenceSourceHelpers`, `SuccessfulSequenceElements`) | 66 | discover sequence and collection value facts | successful-element reasoning | scalar/member facts, semantic model | `ExceptionFlowAnalyzer` |
| dereference partials (`SuccessfulCalleeDereferences`, `SuccessfulDereferences`) | 32 | infer facts from successful caller/callee dereferences | owns one weak semantic-model-partitioned cache | data-flow provider, call context, scalar/member facts | `ExceptionFlowAnalyzer` |
| `ExceptionFlowAnalyzer.ConditionalWeakTableValueFacts.cs` | 10 | infer non-null CWT return facts | owns one weak semantic-model-partitioned invariant cache | semantic model and source/value facts | `ExceptionFlowAnalyzer` |
| remaining value-fact support | 7 | combine/normalize discovered facts | representation remains `ExceptionFlowValueFacts` | neutral fact representation | `ExceptionFlowAnalyzer` |
| former `ExceptionFlowAnalyzer.TryCatch.cs` | 12 | typed catch, catch-all, filter conservatism, catch aliases, rethrow, local-symbol matching | mutates only supplied results/fragments; no fields | Roslyn type hierarchy and symbol equality | `ExceptionFlowCatchSemantics` |
| former analyzer entry/body traversal | 10 | direct/transitive member entry, node and try traversal, direct throw statements/expressions, result merge | one call owns result and traversal state | `SyntaxUtils`, call context/value facts, catch semantics | `ExceptionFlowLocalSourceAnalyzer` |
| former `Invocations` local-source family | 3 | invocation sources, known-framework contracts, delegate exception factories | consumes shared syntax/value helpers | semantic environment, external documentation, path factory | `ExceptionFlowLocalSourceAnalyzer` |
| former `LocalCallables` family | 2 | local function and delegate invocation traversal | call-local recursion state | semantic environment and call context | `ExceptionFlowLocalSourceAnalyzer` |
| former recursive-dispatch family | 1 | direct-transitive runtime target traversal | preserves dispatch completeness uncertainty | existing summary dispatch resolution and context facts | `ExceptionFlowLocalSourceAnalyzer` |
| former `SymbolTraversal` family | 4 | object creation, property/indexer and symbol body traversal | direct implicit local sources | `SyntaxUtils`, semantic environment, path factory | `ExceptionFlowLocalSourceAnalyzer` |
| summary traversal and summary throws | 14 | populate local summary fragment and apply catch/rethrow semantics | graph supplied by builder; no graph lifecycle ownership | specialized summary producers, catch semantics | `ExceptionFlowAnalyzer`; retained after cycle audit |
| specialized summary edge/source producers | 28 | await, invocation, construction, access, collection, disposal, dynamic, foreach, conversion, and dispatch edges | consume context/value facts and recurse through summary dispatcher | builder-supplied graph/fragment, context/value facts | `ExceptionFlowAnalyzer` |

Audit method counts are 27 call-context methods, 225 value-fact methods, 12
catch methods after ownership cleanup, and 20 methods in the retained direct
local-source component. The summary-local 14-method family is explicitly
classified but not extracted.

## Dependency finding and split rationale

The measured call graph between the prospective clusters was:

| Caller | Context | Scalar facts | Member facts | Collection facts | Dereference facts |
|---|---:|---:|---:|---:|---:|
| Context | - | 9 | 2 | 4 | 3 |
| Scalar facts | 4 | - | 7 | 7 | 5 |
| Member facts | 2 | 8 | - | 0 | 0 |
| Collection facts | 4 | 6 | 1 | - | 0 |
| Dereference facts | 2 | 6 | 2 | 4 | - |

These are semantic calls, not incidental file references. Context construction
discovers argument/receiver facts, while fact discovery consults the current
call context and recursively derives member, sequence, return, and successful-
dereference facts. Creating separate context and fact services without changing
the algorithms therefore creates bidirectional services. Combining all 252
methods would create the forbidden local-analysis god class. P5O2A4 keeps the
authoritative static cluster until a dedicated package can redesign this seam
with characterization at every intermediate step.

The first direct-local extraction also exposed potential reverse dependencies.
The retained final graph removes them:

```text
XmlDocExceptionSemanticDetector / tests
  -> ExceptionFlowLocalSourceAnalyzer
       -> ExceptionFlowAnalyzer context/value/dispatch facts
       -> ExceptionFlowCatchSemantics
       -> PathFactory / ExternalDocumentationEvidence / framework contracts

ExceptionFlowSummaryGraphBuilder
  -> ExceptionFlowAnalyzer summary-local dispatcher and specialized producers
       -> ExceptionFlowCatchSemantics

ExceptionFlowCatchSemantics
  -> Roslyn symbols/syntax and supplied result/fragment only
```

No `ExceptionFlowAnalyzer*.cs` source references
`ExceptionFlowLocalSourceAnalyzer`; Catch Semantics does not reference
`ExceptionFlowAnalyzer`. Dependency guards enforce both directions. The former
public analyzer forwarding methods were removed rather than preserving a
cycle.

## Call context ownership and semantics

`ExceptionFlowCallContext` remains the concrete per-call representation. It
owns normalized parameter facts, non-null stable parameter-member symbols, the
callable symbol, its deterministic legacy key, and the canonical transport
projection. Context instances live for the relevant traversal/graph work item;
they are not global state. Context creation remains in the analyzer fact
cluster because it calls value-fact discovery and value-fact discovery calls
back into context lookup.

- Same compilation: source bodies and the complete currently supported
  argument, receiver, sequence, return, and stable-member facts are retained.
- Referenced project: the existing semantic scope and graph policy are retained;
  it is not promoted to same-compilation facts.
- Supporting source: `ExceptionFlowSupportingSourceMethod` and its exact
  `ExceptionFlowSemanticScope` remain authoritative; no facts are invented.
- Metadata only: no source body facts are synthesized; existing conservative
  uncertainty/fail-closed behavior remains.
- Context reduction, keying, dispatch remapping, named arguments, optional
  defaults, and canonical context projection are unchanged.

`ExceptionFlowValueFacts` remains the single Roslyn-neutral representation.
All listed analyzer partials perform Roslyn-bound discovery only; no competing
fact type was introduced.

## Value-fact and cache ownership

- Nullability and null guards: analyzer scalar/value cluster.
- Sequence/collection facts: analyzer collection cluster.
- Dictionary facts: analyzer scalar/value cluster.
- Successful local and callee dereference facts: analyzer dereference cluster.
- Return and initializer facts: analyzer scalar/value cluster.
- Stable member facts: analyzer context/member cluster.
- `ExceptionFlowDataFlowFactsProvider` remains the sole data-flow-cache owner.
- ConditionalWeakTable value-fact cache: exactly one static
  `ConditionalWeakTable<SemanticModel, ConditionalWeakTableValueFactCachePartition>`
  in `ExceptionFlowAnalyzer.ConditionalWeakTableValueFacts.cs`. The semantic
  model is the weak lifetime boundary; the partition serializes its mutable
  dictionary with a private lock; collection of the model invalidates the
  partition.
- Successful-dereference cache: exactly one static
  `ConditionalWeakTable<SemanticModel, SuccessfulDereferenceCachePartition>`
  in `ExceptionFlowAnalyzer.SuccessfulDereferences.cs`. Keys contain syntax,
  normalized symbol, and query mode; partition access is locked; semantic-model
  collection is invalidation.
- No cache was copied, no process-global symbol/fact dictionary was added, and
  the summary session remains the owner of its existing per-run graph state.

Moving only the cache containers would leave computation and recursive fact
ownership split across a bidirectional boundary. Moving the complete fact
algorithms would be the prohibited big bang; both caches therefore retain one
explicit owner in the pending cluster.

## Catch semantics

`ExceptionFlowCatchSemantics` is a concrete static leaf with no mutable fields,
interfaces, virtual calls, graph construction, or graph evaluation. It owns:

- exact and base-type matching through Roslyn symbols and base-type traversal;
- catch-all and `System.Exception` handling;
- conservative filters (a filtered catch never suppresses protected flow);
- caught-variable aliases, writes before throw, nested ownership, and rethrow;
- suppression/removal from supplied direct results and summary fragments;
- local-symbol-reference matching shared by catch alias and summary source
  recognition.

Catch-all clears caught direct exceptions, external documentation evidence,
and uncertainty exactly as before. Typed catches remove only assignable types.
Rethrow resolution remains Roslyn-authoritative. Paths are not rebuilt here;
`ExceptionFlowPathFactory` remains their owner. The evaluator continues to own
transitive graph interpretation.

## Direct local source and body ownership

`ExceptionFlowLocalSourceAnalyzer` is a concrete static partial component with
no mutable static fields. One invocation owns its `ExceptionFlowAnalysisResult`,
`ExceptionFlowTraversalState`, recursion guards, and call contexts. It owns:

- member-body discovery through the existing `SyntaxUtils.TryGetMemberBody`;
- direct and source-transitive syntax traversal;
- explicit throw statements, throw expressions, conditional reachability, and
  rethrow dispatch to Catch Semantics;
- constructor, accessor, expression-bodied, property/indexer, local-function,
  delegate, and runtime-dispatch source traversal;
- implicit object/member/invocation sources already modeled by the analyzer;
- consumption, but not ownership, of known-framework contracts, external XML
  documentation evidence, and path projection.

No `SyntaxUtils` function was copied or redesigned. No known-framework contract
was added. `ExceptionFlowExternalDocumentationEvidence` and
`ExceptionFlowPathFactory` remain separate. Summary graph construction stays
with `ExceptionFlowSummaryGraphBuilder`; graph evaluation stays with
`ExceptionFlowSummaryGraphEvaluator`; the direct component has neither type in
its method signatures.

## Components and state

| Component | Responsibility | Owned state / lifetime | Thread safety |
|---|---|---|---|
| `ExceptionFlowCatchSemantics` | catch/filter/rethrow and local-symbol matching | no fields; supplied result/fragment only | safe for concurrent calls with independent arguments |
| `ExceptionFlowLocalSourceAnalyzer` | direct/source-transitive body and local-source discovery | no fields; traversal/result/context are per call | safe for concurrent calls with independent arguments |

Both components are static (`abstract && sealed` in reflection), implement no
interfaces, introduce no virtual dispatch, and use no service locator. Five
dependency guards cover shape, mutable state, graph boundaries, the absence of
an Analyzer-to-Direct-Local reverse edge, and Catch's leaf direction.

## Source-set measurements

- Analyzer partials: 54 before, 49 after.
- Analyzer nonblank lines: 28,841 before, 26,731 after.
- Extracted classes: two concrete owners across six files.
- Extracted nonblank lines: 2,127 total: Catch 550; Direct Local 1,577.
- The top-level Flow directory contains 78 C# sources. Under the explicit
  counting convention “all top-level Flow sources except the neutral
  `ExceptionFlowValueFacts` plus the three Roslyn canonical adapters,” the
  Roslyn-bound analyzer source set contains 80 files.
- Main-only implementation sources directly required to bind the analyzer
  capability seam: one,
  `Execution/Semantic/ProjectClosureExceptionFlowSemanticEnvironment.cs`.
  CLI/workspace/artifact orchestration remains Main-owned but is not counted as
  analyzer-core source.
- Direct references from the analyzer source boundary to
  `ProjectClosureSemanticContext`, `SemanticCompilationScope`,
  `SupportingSourceSymbolResolver`, and `CrossCompilationSymbolResolver`
  remain 0/0 for every type.
- The neutral and canonical cores remain Roslyn-free except for the three
  explicit canonical Roslyn adapters compiled outside the neutral core.

Line totals are architectural measurements, not optimization targets. The new
`ExceptionFlowAnalyzer.LocalSourceFacts.cs` keeps the small shared syntax/symbol
facts on the consumed side of the one-way dependency; it is not another cache
or service.

## Validation

- Focused context/value/dereference/catch/throw/summary/dependency slice:
  569/569 after adding five architecture guards; the pre-existing slice remains
  564/564.
- Self analysis before: 16 findings in 80,463 ms.
- Self analysis after final formatting/build: 16 findings in 61,536 ms.
- Canonical finding comparison: zero added, zero removed, zero changed
  evidence (`DOC610=0`, `DOC611=1`, `DOC631=15`, `DOC632=0`).
- The duration values are individual runs; no performance conclusion is drawn.
- Final four-mode, full-suite, E1, formatting, and build results are recorded
  below.
- Four-mode-focused regression slice: 137/137, covering Direct,
  ProjectTransitiveDeclaredExceptions, ProjectTransitive, and
  SolutionTransitive behavior. The full suite covers the complete mode matrix.
- P5/P6/canonical/dependency focused slice: 45/45. The full suite additionally
  covers P5G/P5I/P5J/P5K/P5L, P6A/P6B/P6C, G3A/G4B/G5, P5O1, P5O2A,
  P5O2A2, and P5O2A3 under their existing test names.
- Full suite: 2,513/2,513, zero failed, zero skipped (2,508 baseline plus five
  new P5O2A4 architecture guards).
- E1 with Source Link enabled, verified line endings, and bounded remote
  references: seven candidates, five complete reconstructions, two expected
  fail-closed cases, zero unexpected failures, and zero potential bugs. Dapper
  remains `ConfigurationReconstructed / ConfigurationUnsupported` for
  `optimization=release-debug-plus`; OneOf remains
  `PdbValidated / MissingArtifact`.
- Two preliminary E1 invocations used the script defaults (`strict` source
  reconstruction and `local` references), not the established baseline
  profile. The sandboxed run and its network-enabled repeat therefore reported
  environmental `SourceUnavailable`/`ReferenceUnsupported` results. No
  potential bug or finding/evidence change was reported. The exact established
  profile above passed.
- Final build: zero warnings and zero errors with `-warnaserror --no-restore`.
- `git diff --check` passed; its only output was the expected line-ending
  warning for the protected foreign `../../.gitignore` modification.
- All 30 changed C# files, including tracked build churn, are CRLF-only UTF-8
  without BOM and contain zero bare LF, bare CR, or trailing whitespace. The
  production/test diff introduces no `var` declaration.
- No new native or managed process crash or test flake has been observed.
  Historical P5O1/P5N crash observations remain unrelated and unattributed.

## P5O2B readiness and assembly composition

P5O2B is **not ready**. The exact next single blocker is the mutually recursive
call-context/value-fact discovery cluster: context construction requires scalar,
member, collection, return, and dereference discovery, while those discoveries
read the active call context. It currently remains 252 methods in the static
analyzer partial source set with the two fact caches. A safe follow-up must find
one acyclic ownership model without combining the cluster into a god class or
losing same-compilation facts.

After that dependency seam is proven, assembly composition still has to place
the one Roslyn-bound analyzer source set into both the active and manifest-
pinned historical build without a source copy, executable `ProjectReference`,
reflection, compiler-version `#if`, or active-Roslyn leakage. No historical
worker, dual-version build, source fork, or compiler conditional was introduced
here.

`BND-P6-003` therefore remains **Under Investigation**. The recommended next
step is a narrowly scoped Call Context/Value Facts seam package that starts from
the measured bidirectional call graph and cache ownership above; it must not
begin P5O2B until that package either proves an acyclic source boundary or
documents that the intact cluster itself is the only safe shared source unit.
