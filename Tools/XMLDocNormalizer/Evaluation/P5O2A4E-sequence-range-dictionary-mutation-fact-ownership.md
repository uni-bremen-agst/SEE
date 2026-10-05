# P5O2A4E - Sequence Range / Dictionary Mutation Fact Ownership

## Result

The nine-method Sequence range / Dictionary mutation family no longer belongs
to 'ExceptionFlowAnalyzer'. Its existing implementations were split across four
existing stateless owners:

| Method | Final owner | Responsibility |
| --- | --- | --- |
| 'TryGetDictionaryReceiverFromTryGetValue' | 'ExceptionFlowSequenceCollectionFactsProvider' | framework dictionary receiver and 'TryGetValue' method classification |
| 'IsDictionaryOfListsType' | 'ExceptionFlowSequenceCollectionFactsProvider' | 'Dictionary<TKey, List<T>>' shape classification |
| 'IsListAddRangeSourceArgument' | 'ExceptionFlowSequenceCollectionFactsProvider' | supported framework 'List<T>.AddRange' source-argument classification |
| 'IsUseGuardedBySuccessfulTryGetValue' | 'ExceptionFlowGuardFactsProvider' | successful 'TryGetValue' true-branch proof |
| 'ConditionRequiresInvocationTrue' | 'ExceptionFlowGuardFactsProvider' | direct/logical-and condition implication |
| 'DoesOutSequenceRemainUnchangedBeforeUse' | 'ExceptionFlowSequenceContentPreservationFactsProvider' | out-sequence preservation between production and use |
| 'IsSupportedDictionarySequenceProperty' | 'ExceptionFlowSequenceContentPreservationFactsProvider' | inspectable, get-only, empty dictionary-of-lists property classification |
| 'GetOutArgumentSymbol' | 'ExceptionFlowSymbolUsageFacts' | generic out-argument symbol resolution |
| 'AssignmentTargetsDictionaryProperty' | 'ExceptionFlowSymbolUsageFacts' | symbol-equal dictionary property assignment-target classification |

No new provider, interface, callback, cache, virtual dispatch, service locator,
Analyzer back-reference, or Analyzer forwarding method was introduced. The
Successful sequence validation family and the 63-method recursive SCC were not
changed.

## Gate and starting state

- Initial HEAD:
  'beb5777d5381ee3807eda0ff5bd2f31a159bccec'
  ('Separate dictionary value fact ownership.').
- The immediate baseline was the committed P5O2A4D audit and its final
  self-analysis artifact.
- Initial status contained only the protected foreign '../../.gitignore'
  change and pre-existing tracked Core 'bin/obj' churn.
- Protected stashes were not changed.
- No commit, push, stash, reset, clean, checkout, or restore was performed.

The current source confirmed the A4D starting graph: 23 Analyzer-owned
downstream methods in two families, a 63-method SCC with 112 internal edges,
eight SCC-to-Analyzer edges, 85 SCC-to-component edges, 18 direct
SCC-to-'ExceptionFlowSemanticScope' edges, zero component-to-Analyzer edges,
and zero component cycles.

## Exact incoming edges and ownership

The seven direct SCC-to-family edges now terminate at existing components:

1. 'IsDictionaryTryGetValueOutSequenceProvenNonNullElements' ->
   'ExceptionFlowSequenceCollectionFactsProvider.TryGetDictionaryReceiverFromTryGetValue'.
2. 'IsDictionaryTryGetValueOutSequenceProvenNonNullElements' ->
   'ExceptionFlowGuardFactsProvider.IsUseGuardedBySuccessfulTryGetValue'.
3. 'IsDictionaryTryGetValueOutSequenceProvenNonNullElements' ->
   'ExceptionFlowSequenceContentPreservationFactsProvider.DoesOutSequenceRemainUnchangedBeforeUse'.
4. 'IsDictionaryOfSequencesProvenToExcludeNullElements' ->
   'ExceptionFlowSequenceContentPreservationFactsProvider.IsSupportedDictionarySequenceProperty'.
5. 'DoesDictionaryTryGetValueAliasPreserveNonNullElements' ->
   'ExceptionFlowSymbolUsageFacts.GetOutArgumentSymbol'.
6. 'IsDictionarySequenceAliasReferenceSafe' ->
   'ExceptionFlowSymbolUsageFacts.AssignmentTargetsDictionaryProperty'.
7. 'IsDictionarySequenceAliasReferenceSafe' ->
   'ExceptionFlowSequenceCollectionFactsProvider.IsListAddRangeSourceArgument'.

The two internal family edges also remain direct lower-layer calls:

- 'IsUseGuardedBySuccessfulTryGetValue' ->
  'ConditionRequiresInvocationTrue' within the guard owner.
- 'IsSupportedDictionarySequenceProperty' ->
  'IsDictionaryOfListsType' from preservation to collection facts.

Dependency direction remains acyclic:

    63-method SCC
      -> ExceptionFlowSequenceCollectionFactsProvider
           -> ExceptionFlowArgumentMapper
           -> ExceptionFlowSymbolUsageFacts
      -> ExceptionFlowGuardFactsProvider
           -> ExceptionFlowSymbolUsageFacts
      -> ExceptionFlowSequenceContentPreservationFactsProvider
           -> ExceptionFlowSequenceCollectionFactsProvider
           -> ExceptionFlowStableMemberFacts
           -> ExceptionFlowSemanticScope
           -> ExceptionFlowSymbolUsageFacts
      -> ExceptionFlowSymbolUsageFacts
           -> ExceptionFlowDataFlowFactsProvider

All four owners are already included in the source-level no-Analyzer-back-
reference guard. The new reflection guard verifies the exact nine owner
assignments and absence of Analyzer methods with those names.

## Why the family was split four ways

The nine methods were connected by range/dictionary call sites rather than one
architectural responsibility.

- Receiver, generic collection shape, and framework 'AddRange' argument
  classification extend the existing collection-facts owner.
- Branch implication and successful-guard reachability extend the existing
  guard-facts owner.
- Fact currentness and inspectable property state extend the existing sequence
  content-preservation owner.
- Out-symbol and assignment-target queries are generic syntax/symbol facts and
  extend the existing symbol-usage owner.

This is a general responsibility split. It is not tied to Dapper, a particular
fixture, or a special analyzer entry point.

## Measurements

| Measurement | P5O2A4D | P5O2A4E |
| --- | ---: | ---: |
| Analyzer-owned downstream methods | 23 | 14 |
| residual fact families | 2 | 1 |
| SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC -> Analyzer | 8 | 1 |
| SCC -> providers/components | 85 | 92 |
| SCC -> 'ExceptionFlowSemanticScope' | 18 | 18 |
| provider/component -> Analyzer | 0 | 0 |
| component cycles | 0 | 0 |
| Analyzer partial files | 41 | 41 |
| Analyzer nonblank SLOC | 19,693 | 19,376 |

All expected architecture values were reached without artificial edges or code.
The SCC is unchanged. The only remaining SCC-to-Analyzer fact edge is:

'AreSequenceElementsProvenNonNull' ->
'IsSequenceSymbolProvenToContainNonNullElementsBySuccessfulHelper'.

The only remaining Analyzer-owned downstream fact family is Successful sequence
validation: 14 methods with one SCC incoming edge.

## Tests

The five existing 'DOC611_DictionarySequenceRangeFactsTests' continue to protect
the end-to-end safe storage/retrieval/range path, cross-method propagation,
grouped append, possible-null rejection, and mutation invalidation.

Three new semantic tests directly protect:

- framework 'TryGetValue' receiver recognition, lookalike rejection,
  dictionary-of-lists classification, and supported versus unrelated
  'AddRange' arguments;
- positive and negated guard branches plus unchanged versus interveningly
  referenced out sequences;
- supported versus mutable/wrong-value dictionary properties, declaration and
  existing-local out symbols, and same-property versus different-property
  assignment targets.

One new architecture test protects all nine exact owners and prevents Analyzer
forwarders. The existing component test continues to reject every source-level
component-to-Analyzer dependency.

## Validation

- Focused new/architecture/end-to-end slice: 18/18.
- Relevant Dictionary/Sequence/Guard/Symbol/Content-Preservation slice: 80/80.
- Broad 'Check.Semantic' / 'Execution.Semantic' / 'Evaluation' P5/P6/G slice:
  1,748/1,748.
- Full suite: 2,540/2,540; zero failed; zero skipped.
- Warning-as-error solution build: zero warnings; zero errors.
- Final self analysis: 16 findings in 61,118 ms; 'DOC610=0', 'DOC611=1',
  'DOC631=15', 'DOC632=0'.
- Canonical finding/evidence diff against the immediate P5O2A4D
  'final-self-analysis.json': zero added, zero removed, zero changed;
  normalized finding arrays exactly equal.
- Retained canonical baseline hash:
  '15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B'.
- No crash or flake was observed. No performance conclusion is drawn from one
  self-analysis duration.

The machine-readable companion audit is
'Evaluation/P5O2A4E-sequence-range-dictionary-mutation-fact-ownership-audit.json'.
The final self-analysis output is
'artifacts/p5o2a4e/final-self-analysis.json'.

## Readiness

P5O2A4F is now architecturally ready: Successful sequence validation is the
only remaining Analyzer-owned fact family and the source of the only remaining
SCC-to-Analyzer fact edge.

The intact SCC is not yet ready to move, and P5O2B is not ready. P5O2A4F must
first extract that 14-method family without changing the SCC, cache ownership,
or behavior.
