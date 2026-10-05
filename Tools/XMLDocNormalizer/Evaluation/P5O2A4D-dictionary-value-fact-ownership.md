# P5O2A4D - Dictionary Value-Fact Ownership

## Result

The four-method Dictionary value-fact condensation family has been removed from
'ExceptionFlowAnalyzer' without changing the 63-method recursive contextual-fact
SCC or any value-fact semantics. The family was split by responsibility instead
of creating a catch-all dictionary provider:

| Method | Final owner | Responsibility |
| --- | --- | --- |
| 'GetDictionaryEntryValueFacts' | 'ExceptionFlowCallContextFactProjector' | projects 'NonNullDictionaryValues' from a source parameter's call context to 'KeyValuePair<TKey,TValue>.Value' |
| 'IsKeyValuePairValueProperty' | 'ExceptionFlowCallContextFactProjector' | classifies the framework property shape used by that projection |
| 'IsReadOnlyDictionaryWrapperConstruction' | 'ExceptionFlowSequenceCollectionFactsProvider' | classifies the wrapped source argument of the framework 'ReadOnlyDictionary<TKey,TValue>' constructor |
| 'TryGetPrecedingSimpleLocalAssignment' | 'ExceptionFlowSymbolUsageFacts' | finds the nearest safe straight-line simple assignment to any local symbol |

No new provider, interface, callback, cache, virtual dispatch, service locator,
semantic-scope owner, runtime-dispatch owner, or Analyzer forwarding wrapper was
introduced.

## Gate and starting state

- Initial HEAD: '8b1ef706b5f96541e5f69f9387334fe14c5f7391' ('Separate sequence fact ownership.').
- Initial status: protected foreign 'M ../../.gitignore' plus two pre-existing
  tracked Core 'obj/Debug/net8.0' build-churn files.
- Protected stashes were not changed.
- No commit, push, stash, reset, clean, checkout, or restore was performed.
- The 252-method graph was not reconstructed. The audit reused the established
  63-node SCC and inspected only the four target methods, their direct callers,
  callees, and the resulting two residual families.

## Exact incoming edges and ownership

The four direct SCC-to-group edges were confirmed in the resulting source:

1. 'GetExpressionValueFacts' ->
   'ExceptionFlowCallContextFactProjector.GetDictionaryEntryValueFacts'.
2. 'IsPrivateDictionaryFieldReferenceSafeForNonNullValues' ->
   'ExceptionFlowSequenceCollectionFactsProvider.IsReadOnlyDictionaryWrapperConstruction'.
3. 'IsDictionaryInsertionValueProvenNonNull' ->
   'ExceptionFlowSymbolUsageFacts.TryGetPrecedingSimpleLocalAssignment'.
4. 'IsStoredSequenceExpressionProvenNonNullElements' ->
   'ExceptionFlowSymbolUsageFacts.TryGetPrecedingSimpleLocalAssignment'.

'IsKeyValuePairValueProperty' remains the direct internal helper of
'GetDictionaryEntryValueFacts'.

The projection still delegates sequence-parameter currentness to
'ExceptionFlowSequenceContentPreservationFactsProvider'. The wrapper classifier
still delegates named/positional argument mapping to
'ExceptionFlowArgumentMapper'. The assignment query still uses
'ExceptionFlowDataFlowFactsProvider', symbol equality, and
'ExpressionReferencesSymbol'.

The resulting dependency direction is acyclic:

    63-method SCC
      -> ExceptionFlowCallContextFactProjector
           -> ExceptionFlowSequenceContentPreservationFactsProvider
           -> ExceptionFlowSymbolUsageFacts
      -> ExceptionFlowSequenceCollectionFactsProvider
           -> ExceptionFlowArgumentMapper
      -> ExceptionFlowSymbolUsageFacts
           -> ExceptionFlowDataFlowFactsProvider

All three final owners have zero source references back to
'ExceptionFlowAnalyzer'; the architecture guard verifies this lower-layer
invariant.

## Why the family was split three ways

The four methods formed one condensation family because of their callers, not
because they represented one architectural responsibility.

- The two 'KeyValuePair.Value' methods consume a call-context parameter fact and
  project it onto a foreach iteration value. They extend the existing
  call-context projector.
- 'IsReadOnlyDictionaryWrapperConstruction' is a stateless framework
  collection/source classifier. It extends the existing collection facts
  provider.
- 'TryGetPrecedingSimpleLocalAssignment' is dictionary-independent
  syntax/symbol/data-flow discovery. It extends the existing shared symbol-usage
  owner and remains reusable by dictionary insertion and sequence-range logic.

This split avoids a misleading 'ExceptionFlowDictionaryValueFactsProvider' and
keeps dependency direction one-way.

## Measurements

| Measurement | Before | After |
| --- | ---: | ---: |
| Analyzer-owned downstream methods | 27 | 23 |
| residual fact families | 3 | 2 |
| SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC -> Analyzer | 12 | 8 |
| SCC -> providers/components | 81 | 85 |
| SCC -> 'ExceptionFlowSemanticScope' | 18 | 18 |
| provider/component -> Analyzer | 0 | 0 |
| component cycles | 0 | 0 |
| Analyzer partial files | 41 | 41 |
| Analyzer nonblank SLOC | 19,948 | 19,693 |

The measurements match the expected result. The SCC itself, the
ConditionalWeakTable-backed invariant/cache ownership, compilation modes,
external-source paths, exact-identity behavior, and demand-driven P6 behavior
are unchanged.

## Remaining Analyzer-owned downstream families

### Sequence range and dictionary mutation - 9 methods / 7 SCC edges

- 'TryGetDictionaryReceiverFromTryGetValue'
- 'IsUseGuardedBySuccessfulTryGetValue'
- 'ConditionRequiresInvocationTrue'
- 'DoesOutSequenceRemainUnchangedBeforeUse'
- 'IsSupportedDictionarySequenceProperty'
- 'IsDictionaryOfListsType'
- 'GetOutArgumentSymbol'
- 'IsListAddRangeSourceArgument'
- 'AssignmentTargetsDictionaryProperty'

### Successful sequence validation - 14 methods / 1 SCC edge

- 'IsSequenceSymbolProvenToContainNonNullElementsBySuccessfulHelper'
- 'DoesStatementPreserveSequenceSymbolContents'
- 'StatementSuccessfulCompletionProvesSequenceElementsNonNull'
- 'InvocationSuccessfulCompletionProvesSequenceElementsNonNull'
- 'MethodSuccessfulCompletionProvesParameterElementsNonNull'
- 'ForeachDirectlyEnumeratesParameter'
- 'AllPrecedingReturnsAreVacuousSequenceGuards'
- 'IsSingleVoidReturn'
- 'ConditionTrueImpliesSequenceHasNoElements'
- 'IsSequenceCountComparedEqualToZero'
- 'ForeachBodyNecessarilyDereferencesEveryIteration'
- 'ForeachBodyCanExitBeforeRemainingElementsAreValidated'
- 'BreakTargetsForeach'
- 'ContinueTargetsForeach'

SCC extraction is not ready. P5O2B is not ready. The remaining eight direct
SCC-to-Analyzer edges must be removed before moving the intact SCC or attempting
the dual-version build.

## Tests

'ExceptionFlowDictionaryValueFactOwnershipTests' directly protects:

- call-context 'NonNullDictionaryValues' projection to framework
  'KeyValuePair.Value';
- rejection of unrelated 'Value' properties;
- framework 'ReadOnlyDictionary<TKey,TValue>' wrapped-source recognition;
- rejection of unrelated constructors and arguments;
- nearest preceding simple local assignment selection;
- rejection of conditional and compound preceding writes.

'ExceptionFlowFactComponentDependencyTests' now verifies the exact three owners,
absence of the four methods from 'ExceptionFlowAnalyzer', absence of owner
back-references, and the existing acyclic static component layer.

## Validation

- Initial new ownership/semantic slice: 14/14.
- Focused Dictionary/Sequence/Return/provider slice: 67/67.
- Broad P5/P6/G-relevant Semantic/Evaluation/Exception-Flow slice: 1,694/1,694.
- Full suite: 2,536/2,536; zero failed; zero skipped.
- Warning-as-error solution build: zero warnings; zero errors.
- Final self analysis: 16 findings in 62,103 ms; 'DOC610=0', 'DOC611=1',
  'DOC631=15', 'DOC632=0'.
- Canonical finding/evidence diff: zero added, zero removed, zero changed;
  normalized finding arrays exactly equal to the P5O2A4C Sequence baseline.
- Retained canonical baseline hash:
  '15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B'.
- 'git diff --check': clean for the completed task patch.
- No crash or flake was observed; no performance conclusion is drawn from one
  self-analysis duration.

The machine-readable companion audit is
'Evaluation/P5O2A4D-dictionary-value-fact-ownership-audit.json'.
