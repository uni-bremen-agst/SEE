# A10: additional production-finding characterization conflicts

Current decision: **R7 APPROVED**, exactly rows 01–32, conditional on actual
sound A9 OUTSIDE-PROFILE certification. Do not request R7 again. No production
or test edits have been made. The original partial preflight below is historical
A10 audit history: all 202 exclusions are now reconstructed. The current single
additional request is [R8–R10](Final-consolidated-approval-batch.md), with the
complete source/fixture map in [Complete-fixture-preflight.json](Complete-fixture-preflight.json).

The accepted decision A is binding. `TryBuildTransitiveSummaryGraph` remains
raw/diagnostic, with no new facade. The open-root throw test and the 27 simple
graph-helper test files are NOT reclassified as production queries. Their raw
contracts are not the reason for this STOP. R1--R6 remain approved.

## 1. Raw-graph production bypass audit — negative

Repository-wide C# search of `src`, excluding bin/obj, finds exactly one
`TryBuildTransitiveSummaryGraph` occurrence: its declaration in
`ExceptionFlowSummaryAnalysisSession.GraphConstruction.cs:27`. All its actual
call sites are in test helpers/tests (seven calls in six test files).

That method creates a LOCAL graph at line 32. It does not replace the private
production session graph in `ExceptionFlowSummaryAnalysisSession.cs:26`; it does
not evaluate or project a result. Production has one `evaluator.Evaluate` call,
at `ExceptionFlowSummaryAnalysisSession.Analyze:83`. No production raw-graph
adoption, raw fallback, raw-to-production cache import, or raw-to-finding route
was found. Historical calls the shared `AnalyzeSolutionTransitivelyThrownExceptions`
wrapper, which delegates to `Session.Analyze`, not the raw builder.

Actual Current chain:

    CheckAssert.FindSemanticExceptionFindingsForSource [test setup]
      -> XmlDocExceptionSemanticDetector.FindExceptionSmells
      -> AnalyzeConfiguredExceptionFlow
      -> AnalyzeConfiguredExceptionFlowAsync
      -> ExceptionFlowAnalysisDispatch.AnalyzeAsync
      -> ExceptionFlowAnalysisRouter.AnalyzeAsync
      -> Session.Analyze
      -> builder.TryRegisterSummaryGraphRoot
      -> BuildPendingSummaryNodes
      -> evaluator.Evaluate
      -> native result -> AddExceptionFindings -> normal DOC611

The upcoming P1 belongs before the root registration in this PRODUCTION entry.
P2 must guard every production registration/enqueue against the sealed finite
inventory. Preserving raw diagnostics does not waive either guard. No existing
bypass has been found that would require a separate fix before those guards.
This is a source-call-site audit, not a claim that unimplemented A10 guards
already exist or that the old engine already enforces the new contract.

## 2. Different, actual production conflict

`ExceptionFlowSummaryGraphProductionIntegrationTests.TransitiveModes_UseKnownVirtualDispatchTargets`
uses a public nonsealed BaseService with public virtual Execute and a
parameter-typed receiver. Existing dispatch completeness correctly reports the
possibility of additional external overrides. A known DerivedService throws
InvalidOperationException. The existing PRODUCTIVE detector nevertheless emits
normal DOC611 for that known path, alongside the incomplete analysis state.

Existing code explicitly permits this: `AddExceptionFindings` in
`XmlDocExceptionSemanticDetector.Findings.cs` calls
`AddMissingExceptionFindings(... transitive: true)` even when `uncertain` is true.
This is old behavior, not a raw-graph bypass and not an A10 implementation.

A9 sections 4,7,8 instead require TargetOrAnalysisCoverageIncomplete,
complete=false, NormalFindingAdmissionBlocked=true, and no normal exact Finding
from an outside request. Keeping a raw diagnostic entry therefore does NOT
resolve this PRODUCTIVE finding-presence expectation. The normal-finding
assertions below are not covered by R1--R6 or any earlier named permission.

This does not prove the old known path is fabricated. It proves the stricter A9
whole-request output policy differs from the old partial-analysis policy. Do not
call the old tests bogus, erase their value-fact intent, or relabel incomplete
output as exact in order to retain their normal findings.

## 3. Collected, precise R7 request (subsequently approved conditionally)

All files below are in `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/`;
the class name is the `.cs` filename. Each row covers ONLY the named existing
Fact and its existing mode calls, not all users of a shared assertion helper.

Common old expectation: normal DOC611 containing the named exception despite
the observed incomplete coverage. Common new production expectation, ONLY when
the unchanged input is proved outside A9: classified coverage certificate,
complete=false, no normal DOC610/DOC611 from the failed production request.
Diagnostic DOC631 and unrelated tag diagnostics are NOT automatically forbidden.
No depth bound, new framework assumptions, or manufactured admission is allowed.

Prior permission at the time of the original request: **NO**. Current user
decision for every exact R7 row: **YES, conditional**. Required: **YES if the A9 coverage
certificate applies**; an independently sound admitted case keeps its old
assertions. Do not deliberately reject an exact case merely to fit this table.

| R7 row | Class | Existing method | Normal-finding assertion / helper line |
|---|---|---|---|
| 01 | DOC611_CallContextValueFactDependencyTests | MultipleCallSites_KeepDistinctArgumentFacts | 145 Single; 146 DOC611 ID; 149 ArgumentNullException message |
| 02 | DOC611_DictionarySequenceRangeFactsTests | PossibleNullBeforeDictionaryStorage_StillProducesFinding | AssertTransitiveArgumentNullFinding(source) -> 415 Contains DOC611/ArgumentNullException |
| 03 | DOC611_DictionarySequenceRangeFactsTests | NullAddedThroughTryGetValueAlias_StillProducesFinding | Same helper/415, ONLY this caller |
| 04 | DOC611_ExtractedTagElementInvariantTests | GuardedStructElementWithPossibleDefault_StillProducesFinding | 135 Contains DOC611/ArgumentNullException |
| 05 | DOC611_GuardedSourceReturnMemberFactsTests | AlternateNonNullReturnWithoutMemberFact_StillProducesFinding | AssertArgumentNullFinding(source) -> 467 Contains ArgumentNullException |
| 06 | DOC611_GuardedSourceReturnMemberFactsTests | GuardedLocalReassignedBeforeCall_StillProducesFinding | Same helper/467, ONLY this caller |
| 07 | DOC611_IntrinsicNonNullValueFactsTests | UnrelatedNullableDeclarationProperty_StillProducesFinding | 176 Contains ArgumentNullException |
| 08 | DOC611_ParameterMemberCallContextTests | ReceiverReassignedBeforeCall_StillProducesFinding | AssertArgumentNullFinding(source) -> 318 Contains ArgumentNullException |
| 09 | DOC611_ParameterMemberCallContextTests | CalleeParameterReassignedBeforePropertyUse_StillProducesFinding | Same helper/318, ONLY this caller |
| 10 | DOC611_ParameterMemberCallContextTests | CalculatedPropertyAcrossCall_StillProducesFinding | Same helper/318, ONLY this caller |
| 11 | DOC611_PrecedingSuccessfulDereferenceTests | ConditionalAccessBeforeGuard_StillProducesFinding | 147 Contains ArgumentNullException |
| 12 | DOC611_PrecedingSuccessfulDereferenceTests | ShortCircuitedDereference_StillProducesFinding | 200 Contains ArgumentNullException |
| 13 | DOC611_PrecedingSuccessfulDereferenceTests | WriteAfterDereference_StillProducesFinding | 252 Contains ArgumentNullException |
| 14 | DOC611_PrecedingSuccessfulDereferenceTests | CoalesceAssignmentDereference_StillProducesFinding | 306 Contains ArgumentNullException |
| 15 | DOC611_PrecedingSuccessfulPropertyDereferenceTests | SkippablePropertyDereferenceInEnclosingCondition_StillProducesFinding | AssertArgumentNullFinding(findings) -> 461 Contains ArgumentNullException |
| 16 | DOC611_PrecedingSuccessfulPropertyDereferenceTests | OverridablePropertyDereferencedByEnclosingCondition_StillProducesFinding | Same helper/461, ONLY this caller (call at 211) |
| 17 | DOC611_PrecedingSuccessfulPropertyDereferenceTests | ReceiverReassignedAfterPropertyDereference_StillProducesFinding | Same helper/461, ONLY this caller |
| 18 | DOC611_PrecedingSuccessfulPropertyDereferenceTests | ReceiverReassignedInsideForEach_StillProducesFinding | Same helper/461, ONLY this caller |
| 19 | DOC611_PrecedingSuccessfulPropertyDereferenceTests | CalculatedGetOnlyProperty_StillProducesFinding | Same helper/461, ONLY this caller |
| 20 | DOC611_PrecedingSuccessfulPropertyDereferenceTests | DifferentReceiverWithSameStableProperty_StillProducesFinding | Same helper/461, ONLY this caller |
| 21 | DOC611_StatisticsProvenanceTests | StablePropertyInsidePositiveReceiverBranch_NullableValueStillProducesFinding | AssertTransitiveArgumentNullFinding(source) -> 856 Contains DOC611/ArgumentNullException |
| 22 | DOC611_StatisticsProvenanceTests | StablePropertyReceiverReassignment_StillProducesFinding | Same helper/856, ONLY this caller |
| 23 | DOC611_StatisticsProvenanceTests | MutablePropertyInitializer_StillProducesFinding | Same helper/856, ONLY this caller |
| 24 | DOC611_StatisticsProvenanceTests | PossibleNullDictionaryInsertion_StillProducesFinding | Same helper/856, ONLY this caller |
| 25 | DOC611_SuccessfulSequenceElementValidationTests | ValidationHelperWithBreak_StillProducesFinding | AssertArgumentNullFinding(source) -> 550 Contains ArgumentNullException |
| 26 | DOC611_SuccessfulSequenceElementValidationTests | NullInsertedAfterValidation_StillProducesFinding | Same helper/550, ONLY this caller |
| 27 | DOC611_SuccessfulSequenceElementValidationTests | ConditionalValidation_StillProducesFinding | Same helper/550, ONLY this caller |
| 28 | DOC611_SuccessfulSequenceElementValidationTests | ValidationOfDifferentSequence_StillProducesFinding | Same helper/550, ONLY this caller |
| 29 | DOC611_SuccessfulSequenceElementValidationTests | NonValidatingHelper_StillProducesFinding | Same helper/550, ONLY this caller |
| 30 | DOC611_SuccessfulSequenceElementValidationTests | RuntimeDispatchedValidationHelper_StillProducesFinding | Same helper/550, ONLY this caller (call at 479) |
| 31 | DOC611_WhereSequenceElementFactsTests | OfTypeElementsThroughNullableSelect_RemainPotentiallyNull | AssertArgumentNullExceptionPresent(projectFindings/solutionFindings) -> 124 Contains DOC611/ArgumentNullException target |
| 32 | ExceptionFlowSummaryGraphProductionIntegrationTests | TransitiveModes_UseKnownVirtualDispatchTargets | 60/79 Single DOC611, 68/87 InvalidOperationException message; exactly ProjectTransitive/SolutionTransitive |

Request includes only the immediately necessary call-site/assertion scaffolding
to observe the typed production failure for these rows and new A10-only witness
metadata. A shared helper must NOT be globally weakened for its other callers.
Underlying independent value-fact/raw-graph contracts remain unchanged; new A10
witnesses should distinguish their diagnostic facts from certified production
output. Every other existing assertion/case stays protected. No blanket file,
class, Current, B25B, or finding-policy change permission is requested.

## 4. Actual probe coverage and limits

Read-only `ProductionFindingProbe` uses the inherited A9 Current/test assemblies
and existing CheckAssert/AnalyzerTestHelper methods. Their helper source bytes
match the current workspace. It extracts CURRENT constant source fixtures and
analyzes them; it does NOT execute the compiled existing test methods or the
source programs. Test-source raw/CRLF-normalized equality and hashes are recorded,
not confused with a fresh test-binary/source/PDB binding.

- 56 files containing the normal detector helper were inspected.
- 97 constant source fixtures were extracted; 90 analyzed.
- 64 mode observations (ProjectTransitive/SolutionTransitive), grouped into the
  32 methods above, have both uncertainty and normal DOC611 output.
- 202 delegated/parameterized/nonconstant or unavailable-root fixtures are
  explicitly listed as not reconstructed. They are NOT skipped unit tests.
- The two exploratory modes do NOT grant extra cases beyond the modes already
  used by each named existing test. Original assertions/helper chains are in
  `artifacts/p5o2b25a10/production-finding-assertion-map.json`.
- This is NOT exhaustive behavioral coverage, an A10 exact-profile classifier,
  a proof that all observed metadata uncertainties are irreparable, or an
  authorization to change 32 tests without approval.

`production-finding-probe.json` records actual uncertainty reasons, output types,
source hashes and excluded fixtures. In particular the open BaseService/Validator/
Holder cases provide concrete target-closure failures, not just a broad search
hit. Any genuinely new unapproved assertion still requires STOP.

## 5. Honest execution history / validation status

Only the new A10 audit executable was built, never the product/test project.
Its first build exited 0 with two MSB3277 dependency warnings. Adding explicit
references to the SAME inherited Immutable/Metadata DLLs (as A8 tooling already
does) produced a clean build, without changing any compiler/package version.
The first probe execution aborted at an overstrict test-file byte comparison:
that file differed only by CRLF/LF. The owned execution session was stopped
(exit 1); it generated no completed probe record. The revised tool distinguishes
current fixture text from invoked unchanged helper code. That revised probe ran
once to completion, exit 0; its separate static assertion-map mode also exited 0.
Both revised builds were clean. These are disclosed audit-tool revisions, NOT
retries of an unchanged test or production gate to obtain green.

No production implementation, unit-test replay, Broad/Full/Boundary/Dual/WAE,
Current-baseline comparison, A6--A10 implementation gate, Self Analysis or
Canonical Finding Diff was run in this continuation. Earlier A10 diagnostic
6/6 results remain earlier results, not new validation here.

Only A10-owned proof/documentation was added/updated. Historical B25--A9
evidence, root ignore WIP, Core23, index and six stashes remain protected. No
commit, push, stash operation, new wire DTO or B25B admission.

R7 decision received and bounded as stated above. Next decision: the one final
R8–R10 additional batch after the completed fixture preflight. Until that decision,
no production/test fix will be started.
