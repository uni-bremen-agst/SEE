# P5O2A4B — Call Context and Value Fact Dependency Decomposition

## Result

P5O2A4B completed the requested method-level audit and retained the smallest
safe production slice. It did **not** claim that the complete context/fact
blocker is solved.

The 252-method baseline is exact: 27 call-context methods and 225 value-fact
methods, connected by 549 direct source-method edges. Tarjan analysis found 178
SCCs, four nontrivial SCCs, and one mixed Context/Fact SCC of 63 methods. That
mixed SCC is a real recursive evaluation algorithm, not merely accidental file
placement.

The safe lower layer extracted by this package is:

- `ExceptionFlowArgumentMapper`: parameter-ordinal mapping;
- `ExceptionFlowSymbolUsageFacts`: parenthesis, symbol-reference, and
  expression-write queries;
- `ExceptionFlowStableMemberFacts`: stable auto-property classification;
- `ExceptionFlowDereferenceFactDiscovery`: all 31 successful-dereference
  discovery methods and the one successful-dereference cache.

These components form one-way dependencies and do not reference
`ExceptionFlowAnalyzer`. They are static, sealed by the CLR type shape,
nonvirtual, interface-free, and callback-free. No service locator, DI framework,
mutable global session, second fact representation, or new value-fact feature
was introduced.

The mixed 63-method SCC remains in `ExceptionFlowAnalyzer`. Extracting it in
this package would require one of the explicitly forbidden outcomes: a
CallContextBuilder-to-FactDiscovery-to-CallContextBuilder component cycle, a
delegate/service-location substitute for that cycle, a combined 63-method
contextual evaluator without independently reviewable intermediate seams, or a
rewrite of the recursive fact algorithm. The STOP rule therefore applies at
this measured boundary.

## Repository gate and scope

- Initial HEAD: `40551c87b7ebcfdf213456fb8d44a74f424d23c1`
  (`Separate exception flow catch and local source analysis.`).
- Initial status: only the pre-existing foreign `M ../../.gitignore`.
- P5O2A4 was committed, so the P5O2A4B gate passed.
- No commit, push, checkout, restore, reset, or stash operation was performed.
- The protected `WIP full position provenance before P1 extraction` and
  `WIP known-framework position provenance` stashes were not changed.
- Scope stayed within call-context construction, value-fact discovery, their
  direct shared helpers, the successful-dereference cache, characterization,
  guards, and documentation.
- `Program.Main`, the known `IOException` boundary, known-framework contract
  semantics, canonical projection, summary evaluation, catch semantics, local
  source traversal, and historical-worker construction were not changed.

## Exact 252-method baseline

The prior 225 count excludes six methods belonging to nested cache helper
types (`TryGetValue`/`Store` and comparer methods), because those are cache
implementation methods rather than discovery methods. The previously implicit
225th discovery method is `CreateKnownFrameworkContractArguments` in
`ExceptionFlowAnalyzer.KnownFrameworkContracts.cs`. With that explicit rule,
the semantic inventory is reproducible as 27 + 225 = 252.

| Family/file | Before | Responsibility | State/cache | Same-compilation / scope behavior | Candidate or final owner |
|---|---:|---|---|---|---|
| four CallContext partials | 27 | receiver, argument, default, member and sequence projection; context normalization | call-local dictionaries/guards | preserves callable identity and active caller facts | residual contextual SCC; construction owner not yet safely extractable |
| ConditionalWeakTable facts | 10 | callback and stored-value non-null proof | one weak `SemanticModel` cache, locked partition | semantic-model-bound | residual contextual fact SCC |
| dictionary facts | 8 | dictionary value and insertion invariants | call-local recursion guards | semantic-model-bound | residual contextual fact SCC |
| enum facts and switch reachability | 15 | defined enum value/element and reachability proof | call-local recursion guards | semantic-model-bound | residual contextual fact SCC |
| immutable/stable/known properties | 23 | immutable value discovery and stable-member facts | no long-lived state | source declaration evidence | discovery remains residual; classification now `ExceptionFlowStableMemberFacts` |
| local initializer/stable property facts | 6 | current initializer and property assignment facts | call-local guards | same semantic scope | residual contextual fact SCC |
| nullability/null guards/numeric conditions | 32 | scalar, guard, constant and write-sensitive facts | DataFlow provider only | active `SemanticModel` and call context | residual contextual fact SCC plus `ExceptionFlowSymbolUsageFacts` |
| return nullability/value facts | 14 | source and known-framework return facts | call-local inspected-symbol guards | origin-sensitive source resolution | residual contextual fact SCC |
| sequence/collection/range/source facts | 66 | sequence element, list range, dictionary sequence, helper and successful-element facts | call-local guards | active semantic scope | residual contextual fact SCC |
| source-position facts | 7 | one-based Roslyn position invariants | no long-lived state | semantic-model-bound | residual value facts |
| successful callee/dereference facts | 31 | preceding and callee-success dereference proof | one weak `SemanticModel` cache, locked partition | semantic-model-bound | `ExceptionFlowDereferenceFactDiscovery` |
| throw reachability | 9 | Boolean reachability under current facts | no long-lived state | active context | residual value facts |
| central value facts | 7 | normalize and combine scalar facts | no long-lived state | active context | residual contextual fact SCC |
| known-framework argument projection | 1 | project argument facts into a registered contract | no long-lived state | consumes existing contracts only | residual query adapter |
| **Total value facts** | **225** |  |  |  |  |
| **Total cluster** | **252** |  |  |  |  |

The complete per-method records are checked in as
[`P5O2A4B-call-graph-before.json`](P5O2A4B-call-graph-before.json) and
[`P5O2A4B-call-graph-after.json`](P5O2A4B-call-graph-after.json). Every node
contains file, line, complete method signature, domain, direct internal callers
and callees, external source callers and callees, context/fact read/creation
flags, cache access, `SemanticModel`, symbol, syntax/operation, summary
dependencies, and SCC identity/size. The family table above supplies the
responsibility, state, semantic-scope restriction, and candidate/final owner
for every node. Together they are the complete method-level classification,
not a text-search approximation.

## Call graph and SCCs before the slice

| Metric | Before |
|---|---:|
| nodes | 252 |
| context nodes | 27 |
| value-fact nodes | 225 |
| direct internal edges | 549 |
| Context-to-Fact edges | 27 |
| Fact-to-Context edges | 26 |
| SCCs | 178 |
| nontrivial SCCs | 4 |
| largest SCC | 63 |

The nontrivial SCC sizes were 63, 10, 3, and 2:

- 63: contextual scalar/member/return/collection discovery plus two context
  construction methods;
- 10: successful-callee completion/dereference recursion;
- 3: logical condition/AND/OR reachability recursion;
- 2: expression/invocation direct-dereference recursion.

The 10- and 2-method SCCs now live wholly inside
`ExceptionFlowDereferenceFactDiscovery`. They remain method-recursive by
design, but no component cycle surrounds them. The 3-method reachability SCC is
also wholly fact-internal. Only the 63-method SCC crosses the Context/Fact
responsibility boundary.

## Exact mixed 63-method SCC

The remaining mixed SCC consists of the following methods (overloads are
distinguished by the checked-in graph signatures and recorded lines):

- `CallContext.cs` (2): `CreateCallContext`, `AddExplicitArgumentFacts`.
- `ConditionalWeakTableValueFacts.cs` (3):
  `IsConditionalWeakTableGetValueResultDefinitelyNonNull`,
  `AreAllConditionalWeakTableFieldValuesDefinitelyNonNull`,
  `IsCallbackReturnDefinitelyNonNull`.
- `DictionaryValueFacts.cs` (4): `AreDictionaryValuesProvenNonNull`,
  `IsPrivateReadonlyDictionaryFieldProvenToExcludeNullValues`,
  `IsPrivateDictionaryFieldReferenceSafeForNonNullValues`,
  `IsDictionaryInsertionValueProvenNonNull`.
- `EnumValueFacts.cs` (7): `GetDefinedEnumValueFacts`,
  `IsForeachIterationVariableProvenDefinedEnumValue`,
  `IsSequenceExpressionProvenToContainOnlyDefinedEnumValues`,
  `TryProveSourceInvocationDefinedEnumElements`,
  `IsLocalListProvenToContainOnlyDefinedEnumValues`,
  `IsListAddOfDefinedEnumValue`,
  `AreSequenceElementsProvenDefinedEnumValues`.
- `ImmutableMembers.cs` (6): `GetImmutableMemberValueFacts`,
  `GetStaticReadonlyFieldValueFacts`, `GetInstanceReadonlyFieldValueFacts`,
  `TryGetInstanceFieldInitializerFacts`, `GetGetOnlyPropertyValueFacts`,
  `TryGetGetOnlyPropertyInitializerFacts`.
- `LocalStablePropertyFacts.cs` (4):
  `GetFactsProvenByCurrentLocalStablePropertyInitializer`,
  `TryGetStablePropertyFactsFromLocalObjectSource`,
  `TryGetPropertyAssignmentFacts`,
  `TryGetStablePropertyDeclarationInitializerFacts`.
- `Nullability.cs` (5): `IsDefinitelyNonNull`,
  `IsLocalGuaranteedNonNull`,
  `IsForeachIterationVariableProvenNonNull`, `IsForeachLocalProvenNonNull`,
  `IsSequenceExpressionProvenToExcludeNullElements`.
- `ReturnNullability.cs` (2): `IsInvocationResultDefinitelyNonNull`,
  `AreAllReturnValuesDefinitelyNonNull`.
- `ReturnValueFacts.cs` (3): `TryGetSourceInvocationReturnValueFacts`,
  `GetSourceReturnExpressionValueFacts`,
  `GetKnownFrameworkInvocationValueFacts`.
- `SequenceCallContext.cs` (2): both
  `AreSequenceElementsProvenNonNull` overloads.
- `SequenceCollectionFacts.cs` (3):
  `IsForeachGroupingLocalProvenToExcludeNullElements`,
  `IsGroupingSequenceProvenToContainNonNullElements`,
  `IsLocalListReferenceSafeForNonNullElements`.
- `SequenceElementFacts.cs` (7):
  `IsLocalSequenceExpressionProvenToExcludeNullElements`,
  `IsDictionaryValuesExpressionProvenToExcludeNullElements`,
  `IsLocalDictionaryProvenToExcludeNullValues`,
  `IsDictionaryReferenceSafeForNonNullValues`,
  `IsDictionaryMemberInvocationSafeForNonNullValues`,
  `IsDictionarySourceHelperArgumentSafeForNonNullValues`,
  `DoesSourceDictionaryParameterPreserveNonNullValues`.
- `SequenceRangeFacts.cs` (12):
  `IsLocalListWithRangeAddsProvenToExcludeNullElements`, both
  `IsRangeSourceProvenToExcludeNullElements` overloads,
  `IsListAddRangeReferenceSafeForNonNullElements`,
  `IsDictionaryTryGetValueOutSequenceProvenNonNullElements`,
  `IsDictionaryOfSequencesProvenToExcludeNullElements`,
  `IsDictionarySequencePropertyInvariantPreserved`,
  `IsDictionarySequencePropertyReferenceSafe`,
  `IsStoredSequenceExpressionProvenNonNullElements`,
  `DoesDictionaryTryGetValueAliasPreserveNonNullElements`,
  `IsDictionarySequenceAliasReferenceSafe`,
  `IsListAliasMemberInvocationSafe`.
- `ValueFacts.cs` (3): both `GetExpressionValueFacts` overloads and
  `GetStringConcatenationValueFacts`.

## Exact back edges inside the mixed SCC

Context-to-Fact:

1. `AddExplicitArgumentFacts` → `AreDictionaryValuesProvenNonNull`.
2. `AddExplicitArgumentFacts` →
   `AreSequenceElementsProvenDefinedEnumValues`.
3. `AddExplicitArgumentFacts` → `GetExpressionValueFacts`.
4. `AreSequenceElementsProvenNonNull` →
   `IsSequenceExpressionProvenToExcludeNullElements`.
5. `AreSequenceElementsProvenNonNull` →
   `IsDictionaryTryGetValueOutSequenceProvenNonNullElements`.
6. `AreSequenceElementsProvenNonNull` →
   `IsLocalListWithRangeAddsProvenToExcludeNullElements`.

Fact-to-Context:

1. `TryProveSourceInvocationDefinedEnumElements` → `CreateCallContext`.
2. `IsSequenceExpressionProvenToExcludeNullElements` →
   `CreateCallContext`.
3. `IsInvocationResultDefinitelyNonNull` → `CreateCallContext`.
4. `TryGetSourceInvocationReturnValueFacts` → `CreateCallContext`.
5. `IsRangeSourceProvenToExcludeNullElements` →
   `AreSequenceElementsProvenNonNull`.
6. `IsStoredSequenceExpressionProvenNonNullElements` →
   `AreSequenceElementsProvenNonNull`.

The other fourteen pre-slice Fact-to-Context edges targeted only
`GetParameterIndexForArgument`. They were not semantic context-construction
dependencies. `ExceptionFlowArgumentMapper.GetParameterIndex` now owns that
stateless query, reducing the measured Fact-to-Context count from 26 to 12.

## Cycle root cause and minimum surfaces

Fact discovery does not need an “everything context.” Its actual immutable
view is only:

- normalized current callable identity;
- parameter ordinal → normalized `ExceptionFlowValueFacts`;
- parameter ordinal → stable non-null member symbols.

It uses those data through `GetParameterFacts`, `IsParameterKnownNonNull`,
`IsParameterMemberKnownNonNull`, and
`GetKnownNonNullParameterMembers`. It does not need a builder, graph,
evaluator, cache, resolver, or analyzer reference in a query context.
Introducing a second view now would merely duplicate the complete read surface
of `ExceptionFlowCallContext` and would not remove the recursive
`CreateCallContext` calls, so no `ExceptionFlowFactQueryContext` was added.

Conversely, context construction needs these concrete fact queries:

- value of an argument or receiver expression;
- dictionary values exclude null;
- sequence elements exclude null;
- sequence elements are defined enum values;
- stable non-null members from source/initializer evidence and successful
  dereferences;
- constant/default parameter facts.

Return, scalar, stable-member, collection, and source invocation discovery are
not independent at this point. A source return can recursively evaluate a
callee under a newly projected context, while creating that context evaluates
the source arguments. That is the semantic recursion represented by the 63er
SCC.

## Representation, query, computation, cache, construction

| Concern | Owner after P5O2A4B |
|---|---|
| fact representation | unchanged `ExceptionFlowValueFacts` |
| context representation and key/canonical projection | unchanged `ExceptionFlowCallContext` |
| parameter mapping query | `ExceptionFlowArgumentMapper` |
| symbol-use query | `ExceptionFlowSymbolUsageFacts` |
| stable-member shape query | `ExceptionFlowStableMemberFacts` |
| successful-dereference computation | `ExceptionFlowDereferenceFactDiscovery` |
| contextual scalar/member/sequence/dictionary/return computation | residual 63-method SCC plus its acyclic callers in `ExceptionFlowAnalyzer` |
| call-context construction | residual `ExceptionFlowAnalyzer` owner; not falsely claimed as extracted |
| DataFlow cache | unchanged `ExceptionFlowDataFlowFactsProvider` |
| ConditionalWeakTable fact cache | unchanged residual Analyzer owner |
| successful-dereference cache | `ExceptionFlowDereferenceFactDiscovery` |

`ExceptionFlowCallContext.Key`, equality-relevant normalization, canonical
projection, receiver mapping, argument-to-parameter semantics, default facts,
and stable-member propagation were not altered. The new mapper contains the
same named-argument/fallback algorithm byte-for-byte at the semantic level.

## Components, lifetime, state, and thread safety

| Component | Responsibility | State | Lifetime | Thread safety |
|---|---|---|---|---|
| `ExceptionFlowArgumentMapper` | named/positional argument → parameter ordinal | none | stateless | safe |
| `ExceptionFlowSymbolUsageFacts` | unwrap, symbol equality, expression writes | none; consumes DataFlow provider | stateless | safe |
| `ExceptionFlowStableMemberFacts` | get-only/init-only auto-property shape | none | stateless | safe |
| `ExceptionFlowDereferenceFactDiscovery` | preceding/callee successful dereference proof and invalidation | one `ConditionalWeakTable<SemanticModel, SuccessfulDereferenceCachePartition>` | process owner, weak per-`SemanticModel` partitions | existing per-partition lock retained |

The successful-dereference key is unchanged: syntax node, normalized symbol,
and query mode. Lookup/store behavior, lock extent, weak semantic-model
partitioning, invalidation-by-collection, and compilation isolation are
unchanged. There is one owner and no copied cache. The ConditionalWeakTable
value-fact cache likewise retains its original field, key, weak lifetime, and
lock. There is no global `ISymbol` or canonical-identity fact dictionary.

Dependency direction for the extracted slice is:

```text
residual contextual fact SCC
    -> ExceptionFlowDereferenceFactDiscovery
        -> ExceptionFlowStableMemberFacts
        -> ExceptionFlowSymbolUsageFacts
            -> ExceptionFlowDataFlowFactsProvider

residual context/fact callers
    -> ExceptionFlowArgumentMapper
```

There is no reverse edge from any new component to `ExceptionFlowAnalyzer`.
There are no new interfaces, delegates/callbacks, virtual methods, or mutable
session objects.

## SCC measurement after the slice

| Metric | Before | After |
|---|---:|---:|
| nodes under the original identity set | 252 | 252 |
| context nodes | 27 | 26 |
| value-fact nodes | 225 | 225 |
| neutral support nodes | 0 | 1 |
| edges | 549 | 549 |
| Context-to-Fact edges | 27 | 27 |
| Fact-to-Context edges | 26 | 12 |
| SCCs | 178 | 178 |
| nontrivial SCCs | 4 | 4 |
| largest SCC | 63 | 63 |

The unchanged SCC size is reported deliberately. The production change
establishes real owners for acyclic lower layers and contains the two
dereference SCCs; it does not pretend that moving files removed the remaining
method cycle.

## Characterization and origin semantics

Seven new tests were added before/with the structural slice:

- nested A→B→C context uses only the immediate parent facts;
- recursive A→B→A terminates and preserves the known fact;
- two call sites to the same method keep distinct argument facts;
- four dependency guards enforce static/sealed/interface-free components, no
  Analyzer reverse edge, sole successful-dereference cache ownership, and no
  Main/summary/historical-resolution dependencies.

Existing focused tests remain the characterization for the other measured
back edges:

| Semantic behavior | Characterization |
|---|---|
| argument fact → callee context | `DOC611_CallSiteValueFactsTests` and new multiple-site test |
| receiver fact → callee context | `DOC611_ReducedExtensionCallContextTests` |
| stable-member fact → callee context | `DOC611_ParameterMemberCallContextTests` |
| return fact propagation | `DOC611_SourceReturnCallContextTests`, `ExceptionFlowReturnValueFactsTests` |
| dereference under active context | preceding/property/callee dereference suites and cache suite |
| sequence under active context | `DOC611_SequenceElementCallContextTests` and source/range suites |
| dictionary under active context | dictionary sequence/range and source-helper suites |
| cache isolation | DataFlow and successful-dereference tests, including shared syntax tree in different compilations |
| determinism | cached/uncached equality and repeated-query tests |

The implementation neither detects nor branches on origin. Same Compilation
retains receiver, argument, stable-member, nullability, sequence, dictionary,
dereference, and return facts. Referenced Project retains its existing fact
boundary. Supporting Source retains its exact `ExceptionFlowSemanticScope` and
evidence requirements. Metadata Only remains conservative and fail closed. No
cross-compilation fact was invented, and no origin was unified for architectural
convenience.

## Architecture guards and source measurements

- Direct Analyzer references to `ProjectClosureSemanticContext`,
  `SemanticCompilationScope`, `SupportingSourceSymbolResolver`, and
  `CrossCompilationSymbolResolver`: 0 each.
- Neutral Core remains Roslyn-free.
- Canonical Core remains Roslyn-free outside the existing explicit Roslyn
  adapters.
- P5O2A3 construction/evaluation guards, P5O2A4 catch/local-source guards, and
  the new P5O2A4B guards pass.
- Analyzer partials: 49 before, 47 after.
- Analyzer nonblank SLOC: 26,731 before, 24,225 after.
- New/extracted component nonblank SLOC: 2,556 total. The authoritative
  per-file counts are Argument Mapper 48, Dereference Discovery 2,320, Stable
  Member Facts 110, and Symbol Usage Facts 78. The Analyzer reduction is 2,506
  lines because the new mapper and component documentation add explicit owner
  surface rather than merely moving the old text.
- No empty or forwarding Analyzer partial was retained for the two moved
  dereference files.

## Validation

- New characterization tests before production extraction: 3/3.
- Focused context/dereference/dependency slice: 30/30.
- New dependency/cache/context slice: 14/14.
- Four-mode-focused slice: 62/62, covering Direct,
  ProjectTransitiveDeclaredExceptions, ProjectTransitive, and
  SolutionTransitive.
- P5/P6/G/Semantic/canonical/dependency slice: 767/767, including the existing
  P5G/P5I/P5J/P5K/P5L, P6A/P6B/P6C, G3A/G4B/G5 paths through their production
  test classes.
- Full Suite: 2,520/2,520, zero failed, zero skipped (2,513 baseline + seven
  new tests).
- Self Analysis before: 16 findings (`DOC610=0`, `DOC611=1`, `DOC631=15`,
  `DOC632=0`).
- Self Analysis after: 16 findings in 54,026 ms with the same distribution.
- Canonical finding diff: zero added, zero removed, zero changed evidence.
- Performance: only the single 54,026 ms duration is recorded; no performance
  conclusion is drawn.
- E1 correct profile: Source Link enabled, verified line endings, bounded
  remote references. Seven candidates, five complete reconstructions, two
  expected fail-closed cases, zero unexpected failures, zero potential bugs.
  Dapper remains `ConfigurationReconstructed / ConfigurationUnsupported` for
  `optimization=release-debug-plus`; OneOf remains
  `PdbValidated / MissingArtifact`. Semver retains its expected one added
  source-backed finding and no potential bug.
- A preliminary sandboxed E1 run with the same profile produced five
  environmental `SourceUnavailable` outcomes because network access was
  unavailable. Its network-enabled repeat above is the canonical result.
- Final build: zero warnings and zero errors with
  `dotnet build .\XMLDocNormalizer.sln -warnaserror --no-restore`.
- `git diff --check` passes; its only output is the expected line-ending
  warning for the protected foreign `../../.gitignore` change.
- All changed C# sources are CRLF-only UTF-8 without BOM and have zero bare LF,
  bare CR, or trailing whitespace. New production/test code contains no `var`
  declaration.
- Generated Core `bin`/`obj` changes were treated as build churn and removed
  from the handoff diff without touching source WIP.
- No new crash or flake was observed. The historical P5O1 and P5N observations
  remain separate and unattributed.

## P5O2B readiness gate

P5O2B is **not ready**. The same complete Roslyn-bound analyzer source basis
cannot yet be built against both active and manifest-pinned historical Roslyn
without an executable `ProjectReference`, active-Roslyn leakage, source copy,
reflection analyzer, compiler-version `#if`, or loss of Same Compilation facts.

The exact next single blocker is the 63-method contextual fact SCC listed
above. The next package must split context projection from recursive contextual
fact evaluation through a reviewable concrete ownership model while preserving
the three-field immutable context read surface. It must not introduce a
builder/fact component cycle or callback indirection. Only after that seam is
proven should assembly composition and the P5O2B dual-version build begin.

`BND-P6-003` therefore remains **Under Investigation**. The dependency-
separation part made measurable progress, but it is not complete; dual-version
assembly composition was not attempted, and no historical build or worker was
created.
