# P5O2A4F - Successful Sequence Validation Fact Ownership

## Result

The last 14 Analyzer-owned downstream fact methods have been removed from
'ExceptionFlowAnalyzer'. Thirteen cohesive Successful sequence validation
methods now belong to the new stateless
'ExceptionFlowSuccessfulSequenceValidationFactsProvider'.
'DoesStatementPreserveSequenceSymbolContents' now belongs to the existing
'ExceptionFlowSequenceContentPreservationFactsProvider'.

No Analyzer forwarder, callback, delegate, interface, dependency bag, service
locator, duplicate implementation, or new cache was introduced. The 63-method
contextual-fact SCC and its 112 internal edges were not extracted or changed.

## Gate and starting state

- Initial HEAD:
  '2ed48624923179dfdd96775d5e037b2d7b4c1fce'
  ('Separate sequence range fact ownership.').
- The immediate baseline was the committed P5O2A4E audit and its final
  self-analysis artifact.
- Initial status contained only the protected foreign '../../.gitignore'
  change and pre-existing tracked Core 'bin/obj' churn.
- All six stashes remained unchanged.
- No commit, push, stash, reset, clean, checkout, or restore was performed.

The current source confirmed the complete P5O2A4E starting graph: 14
Analyzer-owned downstream methods in one family, a 63-method SCC with 112
internal edges, one SCC-to-Analyzer edge, 92 SCC-to-component edges, 18 direct
SCC-to-'ExceptionFlowSemanticScope' edges, zero component-to-Analyzer edges,
and zero component cycles.

The single incoming edge was:

'AreSequenceElementsProvenNonNull' ->
'IsSequenceSymbolProvenToContainNonNullElementsBySuccessfulHelper'.

## Exact ownership

| Method | Final owner |
| --- | --- |
| 'IsSequenceSymbolProvenToContainNonNullElementsBySuccessfulHelper' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'StatementSuccessfulCompletionProvesSequenceElementsNonNull' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'InvocationSuccessfulCompletionProvesSequenceElementsNonNull' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'MethodSuccessfulCompletionProvesParameterElementsNonNull' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'ForeachDirectlyEnumeratesParameter' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'AllPrecedingReturnsAreVacuousSequenceGuards' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'IsSingleVoidReturn' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'ConditionTrueImpliesSequenceHasNoElements' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'IsSequenceCountComparedEqualToZero' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'ForeachBodyNecessarilyDereferencesEveryIteration' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'ForeachBodyCanExitBeforeRemainingElementsAreValidated' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'BreakTargetsForeach' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'ContinueTargetsForeach' | 'ExceptionFlowSuccessfulSequenceValidationFactsProvider' |
| 'DoesStatementPreserveSequenceSymbolContents' | 'ExceptionFlowSequenceContentPreservationFactsProvider' |

The former Analyzer partial
'ExceptionFlowAnalyzer.SuccessfulSequenceElements.cs' was replaced by
'ExceptionFlowSuccessfulSequenceValidationFactsProvider.cs'. The SCC call site
now directly invokes the new provider.

## Content-preservation split

'DoesStatementPreserveSequenceSymbolContents' is not a successful-validation
decision. It answers whether an already established sequence-content fact
survives one intervening statement. It therefore extends the existing content-
preservation owner and continues to use:

- 'ExceptionFlowDataFlowFactsProvider' for failed data flow and writes;
- 'ExceptionFlowSymbolUsageFacts' for symbol references;
- existing read-only, null-observation, and source-helper preservation facts.

The successful-validation provider depends on the content-preservation owner,
but the preservation owner has no reverse reference. This maintains an acyclic
dependency direction.

## Provider dependencies

The new provider directly uses only existing lower-level components:

- 'ExceptionFlowSequenceContentPreservationFactsProvider';
- 'ExceptionFlowRuntimeDispatchClassifier';
- 'ExceptionFlowArgumentMapper';
- 'ExceptionFlowDereferenceFactDiscovery';
- 'ExceptionFlowSemanticScope';
- 'ExceptionFlowGuardFactsProvider';
- 'ExceptionFlowSequenceCollectionFactsProvider';
- 'ExceptionFlowSymbolUsageFacts'.

It does not reference 'ExceptionFlowAnalyzer'. It introduces no state, cache,
interface, or runtime dispatch of its own.

The final relevant direction is:

    63-method contextual SCC
      -> ExceptionFlowSuccessfulSequenceValidationFactsProvider
           -> ExceptionFlowSequenceContentPreservationFactsProvider
           -> ExceptionFlowRuntimeDispatchClassifier
           -> ExceptionFlowArgumentMapper
           -> ExceptionFlowDereferenceFactDiscovery
           -> ExceptionFlowSemanticScope
           -> ExceptionFlowGuardFactsProvider
           -> ExceptionFlowSequenceCollectionFactsProvider
           -> ExceptionFlowSymbolUsageFacts

## Measurements

| Measurement | P5O2A4E | P5O2A4F |
| --- | ---: | ---: |
| Analyzer-owned downstream methods | 14 | 0 |
| residual fact families | 1 | 0 |
| SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC -> Analyzer | 1 | 0 |
| SCC -> providers/components | 92 | 93 |
| SCC -> 'ExceptionFlowSemanticScope' | 18 | 18 |
| provider/component -> Analyzer | 0 | 0 |
| component cycles | 0 | 0 |
| Analyzer partial files | 41 | 40 |
| Analyzer nonblank SLOC | 19,376 | 18,631 |

The SCC-to-component value is exactly 93: the previous single SCC-to-Analyzer
edge now terminates at the new provider. No synthetic edge was added.

There are no remaining Analyzer-owned downstream fact methods, residual fact
families, or SCC-to-Analyzer fact edges.

## Tests

The five pre-existing Successful sequence validation tests continue to protect:

- complete successful validation;
- rejection of a validating loop with an early 'break';
- invalidation after inserting a null element;
- rejection of a non-vacuous conditional early return;
- acceptance of switch-local 'break' statements after dereference.

Four new behavior tests protect:

- rejection when a different sequence symbol was validated;
- rejection of a helper that enumerates without dereferencing;
- rejection of a runtime-dispatched validation receiver;
- preservation across a supported read-only 'Count' observation.

Together with the existing mutation test, these cover positive and negative
content-preservation decisions without testing private implementation names.

The architecture test now verifies:

- the exact 13 methods on the new provider;
- the preservation method on the existing preservation owner;
- absence of all 14 methods from the Analyzer;
- a static interface-free provider;
- no provider-to-Analyzer source reference;
- no preservation-to-successful-validation back edge.

## Validation

- Focused Successful-sequence and architecture slice: 20/20.
- Relevant Sequence/Guard/Preservation/Symbol slice: 85/85.
- Broad 'Check.Semantic' / 'Execution.Semantic' / 'Evaluation' P5/P6/G slice:
  1,753/1,753.
- Full suite: 2,545/2,545; zero failed; zero skipped.
- Warning-as-error solution build: zero warnings; zero errors.
- Final self analysis: 16 findings in 55,986 ms; 'DOC610=0', 'DOC611=1',
  'DOC631=15', 'DOC632=0'.
- Canonical finding/evidence diff against the immediate P5O2A4E
  'final-self-analysis.json': zero added, zero removed, zero changed;
  normalized finding arrays exactly equal.
- Retained canonical baseline hash:
  '15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B'.

The first final self-analysis attempt exited without completion output and
without creating its JSON artifact. An isolated identical retry completed
normally and produced the exact canonical baseline. This is recorded as one
transient artifactless run; no semantic or code change was made between runs,
and no performance conclusion is drawn.

The machine-readable companion audit is
'Evaluation/P5O2A4F-successful-sequence-validation-fact-ownership-audit.json'.
The final self-analysis output is
'artifacts/p5o2a4f/final-self-analysis.json'.

## Readiness

P5O2A4G is architecturally ready: the downstream fact layer is fully separated
from the Analyzer, and all SCC fact dependencies now point directly to
components.

The intact SCC is deliberately still owned by 'ExceptionFlowAnalyzer'.
P5O2A5 boundary work remains required before moving it, and P5O2B remains
not ready.
