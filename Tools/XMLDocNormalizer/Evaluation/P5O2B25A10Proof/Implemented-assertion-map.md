# Actual A10 assertion edits at the implementation STOP

## Broad root-cause audit: latest exact two-counter grant

Only `HistoricalAnalyzerBuildProjectTests.SharedManifest_MatchesProvenBoundaryAndCurrentPhysicalSources`:
`shared.Length`121->125 and ordinal-ignore-case distinct count121->125.
The exact four added manifest sources were verified; historical120 probe,
original-membership/runtime-await predicate/file assertions unchanged. Static
verification only; no test or closure gate executed this turn. Other755 source/
test files byte-equal to export13. R2 and R5/K17/P12 unchanged.
Root-cause partition179/179 and single not-applied additional4-case batch under
`BroadRootCauseAudit/`. Existing R7-R10 source texts/case scopes unchanged.

## R2 propagation followup

No existing R2, R5/K17/P12 or other protected test assertion changed. Two NEW
A10 diagnostics now require the genuine wire identity to survive validation
and projection, rather than observe its historical loss; five invalid identity
mutations per case and missing identity still fail closed. Old diagnostic
witnesses retained. First unchanged R2 2/2; combined149/149. Subsequent Broad red
3155/3334, no skips; no additional assertions modified. See
`R2-closure-and-Broad-stop.md` for the exact additional stop boundary.

## R5 grant closure (supersedes the previous R5 STOP)

Only the named R5 `Same(caught, normalizedThrown)` becomes full canonical
identity equality plus explicit Roslyn symbol equality, as newly authorized.
K17/P12 remain as previously granted; Candidate unchanged. EXACT constructed
rebinding, not OUTSIDE. Fresh export12 complete Fact passes; ownership14/14,
new A10 15/15. Combined147/149, zero skips, only the two separately open R2
null-Identity failures. No other existing reference assertion changed or failed.
Fresh witnesses: `R5Closure/canonical-object-identity.json` and
`R5Closure/nested-rebind.json`; scope/results: `R5Closure/verification.json`.
See `R5-semantic-identity-closure.md`. Earlier sections remain snapshot history.

## K17/P12 followup (supersedes the prior K17 STOP below)

Only the named ownership Fact changed: P12's new `Same` becomes non-null/full
canonical identity/explicit Roslyn equality; K17's existing `Same` becomes full
canonical identity/explicit Roslyn equality. Different CLR instances allowed.
Actual classification: EXACT constructed rebinding, with prior identity witness
`IdentityDiagnostics/canonical-object-identity.json`; no historical output edits.
Fresh export11 build passes, zero warnings/errors. Targeted Fact 0/1, no skips:
P12 passes; unchanged R5 `Same(caught, normalizedThrown)` at line62 fails before
K17 is reached. R5 is not edited under an inferred adjacent permission.
See `K17-P12-followup-R5-stop.md` and `K17P12Followup/verification.json`.
All text below describes prior snapshots, not current K17 edit status.

These are WIP changes, not completion claims. Pxx refers to the exact prior
grant mapping in `Characterization-approval-batch.md`. No R7–R10 assertion has
yet been edited. P11–P14/R5 and P16 are now applied. The B25 Finding-count/ID
grant is also applied for the one named Fact, which passes completely. A new
protected K17 reference-identity STOP is documented in
`Constructed-reference-identity-stop.md`; K17/helper remain unchanged.

## Latest exact edits and runtime status

- B25 named ownership Fact only, original Finding assertions 107–108: Single →
  count equal to its already verified two P0/P1 entries; first ID check → All
  Findings DOC611. No positional pairing or message/location/path/owner edits.
  The DTO has no per-Finding owner field; the same Native/Flow snapshot binds
  the verified two input identities and resulting count. Full Fact passes in
  export08 and in combined export09. New post-assertion witness:
  `IdentityDiagnostics/ownership-finding-closure.json`.
- P11: normalized owner A/unequal-to-B → unequal-to-A/equal-to-B. Passed,
  export09 `analysis/a/root-cause.json`; before-wrong-owner retained separately
  in new output and frozen historical evidence, not overwritten.
- P12: absent nested rebind/unequal normalized symbols/uncaught same type →
  unique exact rebound/equal destination symbols/same-type catch. The attempted
  new rebind assertion was written with overstrict CLR `Same` and failed at
  current line56. It is NOT proof that the old assertion failed. Independent
  new diagnostic proves exact canonical and Roslyn symbol equality.
- R5: source-object reuse → intended unique destination identity; attempted
  `Same(caught, normalizedThrown)` is also too strong, as diagnosed. P12/R5
  need clarification/correction as full canonical AND Roslyn equality; no
  generalized weakening of identity is proposed.
- K17 original `Same(caught, normalizedCaught)` is NOT edited. Independent trace
  proves equal canonical/symbol identities but distinct instances. Additional
  approval is required. Witness `IdentityDiagnostics/canonical-object-identity.json`.
- P13: Fault<int> caught by Fault<string> true → false. Passed, export09
  `analysis/a/generic-catch.json`.
- P14/P16: unsafe first-match fingerprint required → forbidden, adopted full
  canonical resolver required. Passed. All other scope/PID/admission assertions
  retained; B25 full suite 27/27.
- R2: classification, certificate parity and lifetime pass in both cases; the
  preserved genuine-version assertion then encounters null Identity because
  the existing validator discards it. No version/isolation assertion is changed.
  Independent raw/validated diagnostic witnesses confirm both cases; no fix yet.

Export09 exact suite results: 143/146, no skips; A2 20/20, A4 15/15, A5 33/33,
A6 8/8 and initial A10 12/12. Export10 diagnostics add 3/3; they do not replace
the failed regressions. Earlier snapshot descriptions below remain historical.

## Ownership followup grant

Only `ExceptionFlowHistoricalSolutionFindingTests.AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted`,
existing Fact `Create(2, "same-name-types")`:

- Original line 104: one merged entry → exactly two complete canonical assembly
  identities P0/P1. Full identity set equality passed.
- P1 selection: exactly one match by complete `CanonicalAssemblyIdentity.Equals`
  against the existing canonical P1 source summary. No positional selection.
- Original line 105: P0 → P1, already authorized by P15. The unchanged P1
  explicit-throw path assertion passed for that selected P1 entry.
- R6 output-only: future B25 writer under A10, false ownership-loss flag
  corrected, new pre-Finding-assertion cardinality witness under A10 only.
- Actual classification: EXACT/admitted; export07 Owner/path checks passed.
  Witness: `ownership-cardinality-followup.json` (full Native/Flow/Graph).
- Original Finding lines 107–108 remain unchanged. Count assertion fails
  (two DOC611, not one); subsequent ID assertion is not reached. No new
  authority is inferred. See `Ownership-followup-finding-stop.md`.

Export07 also compiles the prior R2/P08–P10 test WIP; their runtime regressions
remain pending. The tables below retain their export05 runtime snapshot.

Witness prefix below is the isolated export
`artifacts/b25a10-development-05/artifacts/p5o2b25a10/analysis/`.
All runtime source/certificate evidence is NEW A10 output; no historical witness
was updated. R6 authorizes the A3/A4/A6 future writer redirection to A10.

| Test / exact cases | Grant | Old assertion | New assertion | Actual classification / witness |
|---|---|---|---|---|
| A3 `ActualCallSiteRetainsBothDimensions_ButActualContextUsesDefinition`: method/containing/both/nested ONLY | P01 | context callable = definition; present method arguments missing | context callable = exact selected construction; preserve present method arguments | DIAGNOSTIC raw graph; passed, `a3/callsite-{case}.json`. Non-generic branch and all other assertions preserved |
| A3 `DifferentConstructionsShareActualNode_WhileExactCanonicalIdentitiesDistinguishThem`: containing false/true | P02 | int target = string target | different targets; repeated int still same target | DIAGNOSTIC raw graph; passed, `a3/cache-{False,True}.json` |
| A3 `ExactConstructedKeys_ChangingTypeRecursionHasNoFiniteReuse_FiniteHarnessOnly`: growing=false, containing false/true | P03 | source argument open type parameter | source argument Int32; single Recur/self-edge/single source unchanged | EXACT production certificate plus diagnostic graph; passed, `a3/recursion-{False,True}-False.json` |
| Same: growing=true, containing false/true | R1 | completed folded graph/node/source | OUTSIDE PositiveConstructorCycle certificate, zero production nodes | OUTSIDE proven by certificate; passed, `a3/recursion-{False,True}-True.json`. All independent 32-step assertions retained |
| A3 `SupportingSourceResolverPreservesContainingConstruction_ButNotMethodArguments`: containing=false ONLY | P05 | selected identity differs from rebound | exact full identity equal | exact primitive rebinding; passed, `a3/cross-project-rebind-False.json`. Containing=true assertion preserved |
| A3 `GenericBodyCatchSymbolsAlsoNeedInvocationSubstitution`: containing false/true | P04 | catch argument open type parameter | catch argument Int32 | DIAGNOSTIC substituted graph; passed, `a3/catch-{False,True}.json` |
| A3 `GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness`: containing false/true | R2 | success, one folded Fault entry, empty arguments | Current outside certificate; Historical UnsupportedContext/no result; exact certificate-details parity | Actual runtime pending: edits made after export05. No witness or green result claimed. Existing lifetime/version/isolation assertions unchanged |
| A4 `RealRecurrences_DistinguishFiniteClosureFromIncreasingExactArguments`: identity/constant | P06/P07 | open source; constant graph count 2 | Fault<string>; constant count 3; identity count remains 2 | EXACT; passed, `a4/recurrence-{identity,constant}.json` |
| Same: growing/mutual/containing | R3 | completed folded graph/count/open source | OUTSIDE PositiveConstructorCycle; no production nodes | OUTSIDE proven; passed, `a4/recurrence-{growing,mutual,containing}.json`. Independent Roslyn identity counts unchanged |
| A6 `ExactMaterialization_HasFreshSuccessorAtEveryWitnessStep`: all four existing cases | R4 | successful raw Build / folded production count 2 | production OUTSIDE certificate/count 0; compilation observed separately | OUTSIDE proven; passed, `a6/growth-{False,True}-{False,True}.json`. All 24-step prefix and bare declaration-key assertions unchanged |
| A2 `CompilerModel_ObserveActualSummarySourcesBeforeNormalization`: constructed-callee/two-constructed-callees ONLY | P08 | open source, folded Throw context, two targets same | concrete source/context; two-source/two-callee count only for two-constructed-callees; distinct targets | Actual runtime pending after export05. Closed-source semantics and normalized declaration-handle assertions unchanged |
| A2 `StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness`: all three cases, count change ONLY two-constructed-callees | P09/P10 | empty containing arguments, one merged entry | concrete containing arguments; int/string entries only for two-constructed-callees | Actual runtime pending after export05. Parity, no uncertainty, lifetime and isolation preserved, not yet rerun |

Unchanged A5 test source: 33/33 in export05. Its writer executed only in the
isolated export, so workspace historical A5 output remains untouched.

The original B25 line-104 STOP and its evidence remain in
`Implementation-stop-ownership-cardinality.md`; the newer grant/finding STOP
above supersedes that approval gap, without rewriting its historical record.
