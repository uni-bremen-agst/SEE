# A10 lifecycle audit: uncovered diagnostic-graph boundary

Status: **STOP before production changes. R1--R6 ARE approved.**

This is not a rejection of the finite-orbit theorem and not evidence that a
production fix is impossible. It is an uncovered existing-contract conflict.
The previous A9/Phase-0 mapping was incomplete here. Do not call these tests
newly introduced, defective, or now authorized merely because the old search
missed them. No product/test assertion has been changed.

## Exact contradiction at the selected admission seam

A9 sections 4(4) and 5 exclude open/unbound roots: a full-profile root seed must
be closed. Section 10 requires the side-effect-free profile barrier before
root registration at the session entry, not only before recursive successors.
A10's Phase-1 design applies this to `Analyze` AND
`TryBuildTransitiveSummaryGraph`.

The current `ExceptionFlowSummaryGraphTestHelper.BuildMember` calls exactly
`TryBuildTransitiveSummaryGraph`. It throws if that call fails or has no root
key. It is NOT an independent syntax-only or raw-fragment test helper.

An existing test passes an open `M<TException>` declaration directly to that
helper, with no closed invocation environment. It requires a diagnostic graph
containing the Exception constraint and uncertainty. A strict profile rejection
at this same entry cannot also return that old successfully built graph. No
additional assertion permission or diagnostic/production seam decision for this
case exists in R1--R6 or the earlier explicitly named permissions.

## Precise newly uncovered examples (not edit authorizations)

Paths below are under `Tests/XMLDocNormalizerTests/`.

| ID | File / method | Existing required behavior | A9 full-profile classification / issue | Existing permission |
|---|---|---|---|---|
| N1 | `Check/Semantic/Exceptions/ExceptionFlowSummaryGraphThrowTests.cs`, `ThrowConstrainedTypeParameter_AddsConstraintAndUncertainty`, one Fact | 623--625: open M<TException> where TException:Exception; 632--635 successful Build; 637--642 one Exception source; 644--649 uncertainty naming TException | No closed root environment. RepresentationOutsideGrammar / non-closed binding, before root registration. Returning the existing diagnostic graph through this entry is not the specified admitted full-projection outcome | NONE; not R1--R6 or prior finite-source corrections |
| N2 | `Check/Semantic/Exceptions/ExceptionFlowSummaryGraphDispatchCompletenessTests.cs`, `PublicInterface_RetainsKnownTargetAndAddsUncertainty`, one Fact | 45--48 successful Build; 50--56 known Service dispatch edge; 58 open-dispatch uncertainty | Unclosed public-interface target set. TargetOrAnalysisCoverageIncomplete; a partial diagnostic graph is deliberately different from a certified complete graph | NONE; old K28/invariant row is not permission |
| N3 | `Check/Semantic/Exceptions/ExceptionFlowSummaryGraphLocalCallableTests.cs`, `ReassignedDelegate_IsMarkedUncertain`, one Fact | 493--496 successful Build; 498--500 one root node; 502--506 no edges/sources; 508--510 Delegate invocation uncertainty | Unresolved reassigned delegate target. TargetOrAnalysisCoverageIncomplete. Legacy diagnostic graph construction still succeeds | NONE |

N1 alone triggers the user's stop rule. N2/N3 show this is a boundary-category
issue, not another int/string substitution assertion to rewrite opportunistically.
They are concrete examples, NOT a claimed exhaustive new approval batch.
Related dispatch/coverage/open-root tests require a separate complete boundary
inventory once the intended diagnostic seam has been decided.

## Important distinction: routing success is not exactness

Three Current tests were also inspected/replayed:

- `ExceptionFlowAnalysisRouterTests.CurrentUncertainty_IsPreservedRatherThanCollapsedIntoWorkerCompletenessPolicy`
- `ExceptionFlowAnalysisDispatchTests.CurrentUncertainty_IsNotReplacedByWorkerCompletenessPolicy`
- `ExceptionFlowProductionPipelineTests.Current_UncertaintyIsRetainedRatherThanHistoricalCompleteness`

They require execution success with explicit uncertainty. The existing routing
result's own remarks distinguish execution success from complete analysis.
Therefore `Succeeded == true` ALONE is NOT evidence of an exact-complete claim
or justification to change these tests. No permission to change them is requested
or exercised here. Their raw diagnostic contract must be distinguished from the
new classified production consumer boundary in any proposed design.

Likewise existing Historical `AnalysisFailure` assertions for old incomplete
analyses cannot automatically be rewritten to `UnsupportedContext`. A9 explicitly
preserves other old failure contracts while specifying classified new profile
rejections. The seam and reason mapping must be resolved, not inferred from a
blanket grep or changed just to make a broad gate pass.

## Why this escaped the earlier preflight

The 71-file/513-hit A9 characterization query focused on constructed identities
and generic recursion. It did not include the N1 Throw or N3 LocalCallable test
files. The N2 DispatchCompleteness file was in the hit inventory but was not
mapped individually: it fell under the broad 'other invariant' classification.
Five of the six replayed test files were absent from that inventory (all except
DispatchCompleteness). This is an audit coverage gap, not a new user request.
The previously claimed row mapping did not prove exhaustive behavioral coverage.
Frozen A9 records remain unchanged; this correction is recorded only under A10.

## Fresh diagnostic execution, actual scope

`Audit-AdmissionBoundary.ps1` ran exactly the six named unchanged tests once:
**6/6 PASS, zero failures, zero skips, actual process exit 0.**

This used the existing A9 validation-export test binary, with no build and no
changes to product/test files. The nine inspected workspace/export source files
were checked byte-equal before execution. The test binary's hash is captured;
it was NOT freshly compiled or freshly PDB-verified in this replay. Consequently
this confirms active legacy expectations, not A10 implementation correctness.

Result/log/TRX are exclusively under:
`artifacts/p5o2b25a10/admission-boundary-audit/`.

The selected tests write no legacy witness files and start no Historical worker.
No historical output, frozen evidence, Core artifact, index, ignore WIP or stash
is modified by this execution. No build/Dual/WAE/Broad/Full/Boundary gate was
run; there is no implemented candidate to validate. No Self Analysis or Canonical
Finding Diff is claimed. All those A10 closure requirements remain open.

## Decision required before implementation

Do not amend the three graph assertions now, do not inject dummy graphs into the
test helper, do not loosen uncertainty or introduce a test-only path that merely
hides the admission barrier. Do not silently exempt nongeneric/diagnostic inputs
from the normative profile or force Historical completeness policy onto Current.

The user needs to decide the intended relationship between existing diagnostic
graph queries and the certified production query:

1. Must the existing diagnostic graph contracts remain unchanged behind an
   explicitly separated, non-admitting diagnostic seam? If so, the boundary and
   its guard obligations need a justified architecture decision, not an implicit
   exemption from A9.
2. Or should the legacy successful-build expectations at the now-certified entry
   be adapted? Then a new COMPLETE, assertion-level inventory/approval batch for
   this diagnostic-graph category is needed BEFORE implementation, rather than
   granting only N1 and discovering the rest sequentially.

R1--R6 remain conditionally approved. Neither option is authorized by this report.
B25B, wire, compiler versions, routing choice and Historical isolation remain
unchanged. Production implementation remains unstarted; no partial fix exists.
