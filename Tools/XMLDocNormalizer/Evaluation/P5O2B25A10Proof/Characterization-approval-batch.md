# A10 Phase 0: consolidated characterization approval batch

Status: **AWAITING USER APPROVAL. No production or test file changed.**

Subsequent decision: the user APPROVED exactly R1--R6, conditional on a full
sound A9-FiniteOrbit-v1 implementation. The mapping below is retained as the
precise approved batch. A later read-only lifecycle audit found an uncovered
diagnostic-graph conflict; see `Admission-boundary-stop.md`. That conflict is
NOT covered by this approval. No assertion has been edited.

Contract: `A9-FiniteOrbit-v1`. This document requests conditional permission,
not permission to weaken an assertion until a test passes. Every correction
requires a complete sound implementation and new A10 evidence. B25B stays closed.

All source locations refer to HEAD
`9b76e179a568c64ae972cc3a45980a094e44245e`, not the older HEAD in A9.
Paths below are relative to `Tests/XMLDocNormalizerTests/`.

## Reading the mapping

`Prior` means an explicit earlier user permission, not an inferred permission
from the implementation request. `Required` concerns an assertion change for
A10, not whether its test must run. Preserved rows still require regression
validation. An approval for one assertion never authorizes adjacent assertions.

The complete A9 preflight is mapped below, including its invariant/control rows
and its final witness-writer paragraph. Fresh identical search: 71 files,
513 literal matches; all 71 file hashes match A9's inventory.

### 1. Worker/ExceptionFlowConstructedInvocationContextTests.cs

| ID | Method / exact cases | Existing assertion (line) | A10 expectation | Prior | Required |
|---|---|---|---|---|---|
| P01 | `ActualCallSiteRetainsBothDimensions_ButActualContextUsesDefinition`; method, containing, both, nested | 39: canonical callable equals definition; 41: canonical method arguments empty | Canonical **semantic context** retains exactly the call-site method/containing/nested construction; method arguments only where present | Yes, the two explicitly named assertions | Yes |
| K01 | Same; non-generic control, and all cases' other assertions | 37: exact vs definition; 38: normalized CallableSymbol; 40: empty value-fact Key; 41 first clause: selected arguments present; 42: selected containing construction differs | Preserve. A definition handle and value-fact Key are not the full semantic context key | No | No |
| P02 | `DifferentConstructionsShareActualNode_WhileExactCanonicalIdentitiesDistinguishThem`; containing=false/true | 66: int and string targets equal | Relevant int/string contexts have distinct target keys | Yes, exactly this inequality correction | Yes |
| K02 | Same, both cases | 65: three edges; 67: repeated int target equal; 73--74: independent canonical identities | Preserve same-construction reuse and independent identity observations | No additional permission | No |
| P03 | `ExactConstructedKeys_ChangingTypeRecursionHasNoFiniteReuse_FiniteHarnessOnly`; growing=false, containing=false/true | 94: source argument is ITypeParameterSymbol | Argument is System.Int32 | Yes, A6 conflict 1 | Yes |
| K03 | Same, growing=false, both containing cases | 91: single Recur node; 92: self-edge; 93: single source | Preserve finite identity-cycle structure | No | No |
| R1 | Same; growing=true, containing=false/true | 90 Build assumes completed graph; 91 folded single node; 92 self-edge; 93 single completed source; 94 open parameter | Early OUTSIDE/GrowthCycle certificate; no completed folded summary or exact-complete result; no further constructed queue expansion | **No** | **Yes: batch R1** |
| K04 | Same; all four cases, independent candidate experiment | 118: fresh visited identity; 126--128: 32 growing / one identity contexts, successor count and pending status | Preserve exactly, using compilation obtained independently of successful product graph construction | No | No |
| P04 | `GenericBodyCatchSymbolsAlsoNeedInvocationSubstitution`; containing=false/true | 181: catch argument is open parameter | Fault<int> catch argument | Yes, A6 conflict 2 | Yes |
| K05 | Same | 180: exactly one catch; compilation and source observations | Preserve | No | No |
| P05 | `SupportingSourceResolverPreservesContainingConstruction_ButNotMethodArguments`; containing=false only | 164: expected invocation identity differs from rebound | Full constructed identity equal after exact rebind | Yes, A6 conflict 3 | Yes |
| K06 | Same; containing=true, plus both cases 160--161,165 | Non-null B owner; containing identity equality; normalized context CallableSymbol | Preserve. New method arguments belong to semantic context, not declaration handle | No broader permission | No |
| R2 | `GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness`; containing=false/true | 195--196 assumes successful Current canonical result; 201 historical.Success; 202 successful-result equality; 203 single entry; 204 Fault metadata; 205 empty arguments | Current/Historical same classified OUTSIDE/GrowthCycle rejection, no success result, nonempty structured certificate details; compare failure classification rather than two success results | **No** | **Yes: batch R2** |
| K07 | Same | 206 stopped owned worker; 207 genuine historical Roslyn identity; 208 no historical assembly in Main | Preserve without weaker lifetime/PID/isolation checks. Parity remains mandatory, applied to classification | No | No |

### 2. Worker/ExceptionFlowBoundedGenericContextTests.cs

| ID | Method / exact cases | Existing assertion (line) | A10 expectation | Prior | Required |
|---|---|---|---|---|---|
| P06 | `RealRecurrences_DistinguishFiniteClosureFromIncreasingExactArguments`; identity, constant | 107: first source has open parameter | First source Fault<string> | Yes, A6 conflict 4 | Yes |
| P07 | Same; constant only | 105: two product nodes | Three: Root, A<string>, A<int> | Yes, constant-only count correction | Yes |
| K08 | Same; identity | 105: two nodes | Preserve Root + A<string> | No | No |
| R3 | Same; growing, mutual, containing | 88 completed Build; 105 folded node count (3 mutual, otherwise 2); 106 required completed target summary; 107 open source | OUTSIDE/GrowthCycle before exact materialization, certificate rather than completed folded graph | **No** | **Yes: batch R3** |
| K09 | Same; all five cases | 104: independent distinct Roslyn constructions (identity=1, constant=2, other=3) | Preserve independent compiler observations exactly | No | No |
| K10 | `FiniteMutualCycle_CachedPartialResultDependsOnRootEdgeOrder`; all four generic/order cases | 35--55: graph edges, Fault reachability, absence of uncertainty | Preserve all assertions, including already corrected caught-first A5 assertions | Earlier A5 permission already exercised | No |
| K11 | `GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness`; all four cases | 175--183: success, diagnostics, parity, absence of uncertainty, one Fault, lifetime/isolation | Preserve all assertions | Earlier A5 permission already exercised | No |
| K12 | `RoslynComposition_PreservesScopedMethodAndContainingArguments`; nested=false/true | 136--149: selected arguments, exact reconstruction, distinction from definition | Preserve; output-only false implementation flag is addressed in R6 | No | No |

### 3. Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs

| ID | Method / exact cases | Existing assertion (line) | A10 expectation | Prior | Required |
|---|---|---|---|---|---|
| P08 | `CompilerModel_ObserveActualSummarySourcesBeforeNormalization`; constructed-callee / two-constructed-callees only | 97 one source; 106 not expected closed type; 107 open containing parameter; 108 single folded Throw summary; 111 empty canonical context arguments; 115 equal targets | Concrete Outer<int>.Leaf; two-constructed-callees has two substituted sources/semantic contexts with int/string and distinct targets | Yes, exact two-case A3 correction permission | Yes, only affected source/context/count assertions |
| K13 | Same; constructed cases | 95 nonempty sources; 98 no uncertain targets; 109 normalized declaration Symbol; 110 definition CanonicalIdentity; 114 two call edges | Preserve. A canonical **definition identity** may stay normalized while CanonicalContext carries construction | No further permission | No |
| K14 | Same; closed-source | 97 single source; 98 no uncertainty; 101 correct closed source | Preserve all assertions; no semantic edit to this case | No | No |
| P09 | `StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness`; all three cases | 148: empty containing argument list | Actual concrete containing arguments | Yes, A6 conflict 5 | Yes |
| P10 | Same; two-constructed-callees only | 145: single collapsed entry | Exactly Outer<int>.Leaf and Outer<string>.Leaf entries; apply existing metadata assertions to each entry | Yes, A6 conflict 5, only this case | Yes |
| K15 | Same; other cases / all remaining assertions | 142--144 success/diagnostics/parity; 145 single in other cases; 146--147 Leaf/Outer metadata; 149--152 no uncertainty, lifetime, genuine version, isolation | Preserve semantic assertions (iterate unchanged metadata checks when P10 yields two entries) | No extra permission | No |
| K16 | `CompilerModel_ValidExceptionAndCatchForms`; all cases, and `CompilerModel_FourIdentityFamiliesRetainDifferentOwnersArityAndContainingChains` | Compiler conversion, owner/arity/containing distinctions | Preserve every assertion. Catch output in first method is an observation, not an assertion requiring relaxation | No | No |

### 4. Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs

| ID | Method / exact cases | Existing assertion (line) | A10 expectation | Prior | Required |
|---|---|---|---|---|---|
| P11 | `OriginalNameOnlySubstitution_IsProven_ExactCandidateRepairsOnlyTheTopLevelCase` | 27--28: actual normalized owner equals local A, differs from thrown B | Actual assembly-qualified B owner, not local A | Yes, previously approved four A-characterization tests | Yes |
| P12 | `ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught` | 54: null exact rebind; 59 unequal normalized types; 60 same type not caught | Unique exact nested constructed rebind and same-type catch | Yes, previously named correction; A9 specifically lists 54,59--60 | Yes |
| R5 | Same, additional coupled assertion | 57: Assert.Same(thrown, normalizedThrown), where thrown belongs to source B compilation and Candidate resolves in consumer A | Assert.Same(caught, normalizedThrown), the uniquely resolved metadata symbol in A; identity still B. The old assertion becomes false precisely when the exact resolver starts working | **Not separately enumerated by A9; request explicitly rather than infer adjacent permission** | **Yes: batch R5** |
| K17 | Same | 52 exact identities equal; 53 constructed vs definition different; 58 Assert.Same(caught, normalizedCaught) | Preserve all three. In particular line 58 does NOT need changing | No | No |
| P13 | `GenericCatchComparison_ErasesDifferentConstruction_IndependentOfNameOnlyNormalizer` | 86: Fault<int> caught as Fault<string> | False | Yes, named prior correction | Yes |
| P14 | `StopRule_LeavesOriginalProductionAndClosedFindingAdmission` | 104 unsafe lookup required; 105 exact resolver forbidden | No unsafe first-match normalization; verify unique assembly/module/type binding in the implementation actually adopted (not an arbitrary class-name requirement) | Yes, named prior correction | Yes |
| K18 | Same, plus remaining methods | 107--108 Solution inadmission; same complete assembly/type exact resolver; all nine forged identity rejection cases | Preserve all assertions | No | No |

### 5. Worker/ExceptionFlowHistoricalSolutionFindingTests.cs

| ID | Method / exact cases | Existing assertion (line) | A10 expectation | Prior | Required |
|---|---|---|---|---|---|
| P15 | `AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted` | 105 evaluated owner Original.Solution.P0 | Actual thrown owner Original.Solution.P1 | Yes, first of exactly two B25 permissions | Yes |
| P16 | `StopRule_DoesNotIntroducePolicyRoutingAttestationOrPidWorkarounds` | 195 unsafe GetFirstSymbolForDeclarationId fingerprint | Correct unique owner/type resolution, no unsafe first match | Yes, second B25 permission | Yes |
| K19 | Both methods and all other B25 tests | Local source owners 103; graph/native parity; original finding policy; no final Solution admission; PID/lifetime/isolation, routing/attestation | Preserve. No other B25 assertion is requested | No | No |

### 6. Worker/ExceptionFlowGenericExpansionBoundaryTests.cs

| ID | Method / exact cases | Existing assertion (line) | A10 expectation | Prior | Required |
|---|---|---|---|---|---|
| R4 | `ExactMaterialization_HasFreshSuccessorAtEveryWitnessStep`; all four containing/dictionary combinations | 21 calls Build expecting completed product graph; 49 product graph count=2 | Early OUTSIDE/GrowthCycle, certificate, no exact-complete graph/result; independent compilation/selected method still available for the finite prefix | **No** | **Yes: batch R4** |
| K20 | Same | 29--39 exact candidate queue and unequal successor; 42,47--48 24-step counts and pending candidate | Preserve every independent finite compiler-harness assertion; never run the unbounded product experiment | No | No |
| K21 | Same | 41: equal bare `(symbol, string.Empty)` definition keys | Preserve as explicit low-level declaration-key observation, NOT as the real context identity. Correct misleading comment 40/output labels only under R6 | No assertion change needed | No |
| K22 | `IdentityAndClosingSubstitution_RepeatAnExactState`; all four cases | 63 repeated exact identity; 64 identity-vs-constant distinction; successful Build | Preserve every assertion and successful finite build. Shared fixture helper 75--85 may separate compilation from analysis only as needed for R4; do not turn finite controls into rejection tests | No | No |

### 7. Other A9 preflight rows / invariants

| ID | File / method / cases | Existing assertion | A10 expectation | Prior | Required |
|---|---|---|---|---|---|
| K23 | `Worker/ExceptionFlowRecursiveSummaryClosureTests.cs`; all 33 cases | Finite SCC closure, root order, no partial leakage, completed-cache reuse, uncertainty/isolation | Preserve ALL assertions. Add A10 tests separately; no second recursion engine | No new grant | No |
| K24 | `Check/Semantic/Exceptions/CanonicalIdentityTests.cs`; constructed/nested/scoped identities | Structured identity equalities/inequalities | Preserve | No | No |
| K25 | `Check/Semantic/Exceptions/ExceptionFlowSummaryGraphTests.cs`; `CallableKey_NormalizesSymbolAndIncludesContext` | 45--51 equal normalized declaration keys and hash for explicitly same fact string; 53+ different string differs | Preserve legacy explicit definition/fact-key constructor; production semantic key must separately encode the admitted environment. Do not demand unequal hash codes | No | No |
| K26 | `Check/Semantic/Exceptions/ExceptionFlowCrossCompilationResolverTests.cs`; `ResolveMethod_ConstructedGenericContainingType_PreservesSubstitution` | 254--263 source declaration, string result/parameter/container and nondefinition | Preserve, add method-argument/nested tests separately | No | No |
| K27 | Existing Worker Current/Historical response, finding, routing, attestation, receipt and lifetime tests | Validated success vs rejection; uncertainty/truncation cannot enter finding production | Preserve original assertions and mode/owner/PID/isolation requirements. Add new classified-rejection tests, no weaker old assertions | No blanket permission | No |
| K28 | Other ordinary/contextual-value-fact/dispatch/framework/reporting/compiler search hits | Correct existing invariants / unrelated observations | No opportunistic edits. A new necessary conflict still triggers STOP | No | No |

## Consolidated additional permission request (R1--R6)

Approve the following as ONE batch, conditional on a complete sound A10 fix:

1. **R1:** Only the two `growing=true` cases of A3
   `ExactConstructedKeys_ChangingTypeRecursionHasNoFiniteReuse_FiniteHarnessOnly`:
   replace completed folded-graph/source expectations at 90--94 with early
   `GrowthCycle` certificate/no exact-complete result/no further expansion.
   Separate compilation observations from successful Build. Keep the independent
   32-step experiment and every finite `growing=false` invariant unchanged
   except the previously approved concrete source argument.
2. **R2:** Both containing cases of A3
   `GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness`:
   change only Current-success observation 195--196 and success/entry assertions
   201--205 to equivalent Current/Historical classified rejection with retained
   certificate, absent successful result, and no fallback. Keep worker stopped,
   genuine-version and Main isolation assertions 206--208. Use an A10-specific
   failure observer; do not change the finite callers' `CurrentResult` helper.
3. **R3:** Only `growing`, `mutual`, `containing` cases of A4
   `RealRecurrences_DistinguishFiniteClosureFromIncreasingExactArguments`:
   change completed Build/graph/source assertions at 88,105--107 to early
   classified rejection. Separate compiler observations; line 104 stays unchanged.
   Finite identity/constant changes remain limited to their prior permissions.
4. **R4:** All four growing A6
   `ExactMaterialization_HasFreshSuccessorAtEveryWitnessStep` cases:
   replace the successful Build assumption at 21 and completed folded count at 49
   with early `GrowthCycle` rejection/certificate/no exact-complete result.
   Adapt only the fixture compilation/analysis split at 75--85 to support this.
   Preserve 24-step prefix assertions and the bare definition-key equality at 41;
   preserve every assertion of all four finite control cases.
5. **R5:** Only A-characterization
   `ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught`
   line 57: source-symbol object reuse becomes the uniquely rebound consumer
   metadata symbol (`caught`), retaining its complete B identity. This is a coupled
   consequence of the already approved 54/59/60 correction, not a separate feature.
   Line 58 and all other unapproved assertions remain unchanged.
6. **R6 (output only):** The exact writer/metadata changes in the next section,
   solely to emit truthful NEW A10 evidence. No retrospective historical edits.

These are **five assertion/scaffolding groups plus one output-only group**.
R1/R2/R3/R4 cover 2+2+3+4=11 parameterized growing-case executions; R5 is
one additional coupled assertion in one previously named test. No approval is
requested for weakening parity, PID, isolation, no-uncertainty, or cache reuse.

### R6: exact witness-writer and metadata inventory

All new outputs must live below `artifacts/p5o2b25a10/` (and durable selected
witnesses below `Evaluation/P5O2B25A10Proof/`). Preserve every existing B25--A9
report, audit, script and witness file. Distinguish a test writer's future output
path from the bytes of an existing artifact. Do not describe a compiler-only
observation as a production correctness proof.

| File / writer | Permitted output-only changes / exact callers |
|---|---|
| A3 ConstructedInvocation, `Evidence` 234--239, old A3 analysis path | Redirect NEW `callsite-*`, `cache-*`, `recursion-*`, `cross-project-rebind-*`, `catch-*`, `worker-recursion-*` outputs under A10, with original test identity. Correct false `ProductionCorrected` flags at 52,75,142,166,182,209 only after proven; actual-node/open-source labels 133--134 become concrete/admission observations; `SharedDefinitionFold` at 209 becomes classified rejection, not success. Non-generic assertions unchanged |
| A4 BoundedGenericContext, `Evidence` 187--192, old A4/A5 analysis paths | Redirect new `finite-scc-*`, `recurrence-*`, `composition-*`, `worker-scc-*` under A10 without editing any SCC assertion. Correct `ActualDefinitionGraphCount`/`CorrectedSummary` 114,118 for changed recurrence cases; `BodyProjectionImplemented=false` at 151 becomes truthful implementation/observation metadata. Preserve root-order/A5 facts and no-budget statements |
| A2 CanonicalExceptionIdentity, `Evidence` 193--198, old A2 analysis path | Redirect new `compiler-*`, `summary-origin-*`, `worker-construction-loss-*` under A10. Correct `CorrectConstructedType=false` / `ProductionRepaired=false` 161--162 only with validated concrete identities; `Parity=true` and `SolutionFindingAdmission=false` retained. Compiler conversions and closed-source assertions untouched |
| A AssemblyQualifiedOwnership, `Evidence` 162--167, old A audit-cases path | Redirect new `root-cause`, `nested-rebind`, `generic-catch` under A10. Correct 34--37 before/candidate labels and adopted status; 67--72 rebind/catch/adoption claims; 87 incorrect suppression/repaired flags. Keep explicit old-vs-new comparison referencing frozen historical witness, not claim the current result still has the old defect |
| B25 HistoricalSolutionFinding, `Evidence` 292--293 | New `ownership-stop-witness` output under A10; change `TypeOwnershipLossObserved=true` at 119 to truthful current outcome and retain documented historical loss separately. Output path only for other calls (`baseline-{count}-{scenario}`, `graph-negative-*`, `cli-unsupported-*`) may be redirected under A10 to prevent historical overwrite. NO other B25 assertion/fixture/policy changes |
| A6 GenericExpansionBoundary, `Evidence` 88--93, old A6 analysis path | Redirect all four `growth-*` and all four `finite-*` outputs under A10. Correct `ProductionChanged=false` 50,65 and actual-product labels; retain independent `WitnessSteps=24`, prefix observations, and `FinitePrefixIsNotTerminationProof=true`. Correct misleading comment 40 to distinguish bare declaration keys from actual admitted context keys |

No source edit is requested for A5 `ExceptionFlowRecursiveSummaryClosureTests`.
Run its unchanged writer in a fresh isolated A10 validation export, as previous
packages already did, and capture its new outputs within that export under A10.
The same isolation applies to unaffected earlier suites. Do not run old gates
against their frozen old-HEAD/index assertions and then weaken those checks.
New A10-owned gate adapters must distinguish historical byte verification from
fresh implementation regressions; historical proof scripts remain frozen.

## Limits of this batch

No edits have been made under this permission request. Prior permissions remain
conditional. If the user approves, implementation must still prove membership,
coverage, owner binding, substitution, queue termination and A5 integration.
This batch does not approve a widening/depth budget, a first-match fallback,
new wire DTO, general Solution scope, B25B, test skipping, retries, or changes to
any other existing assertion. A genuinely new necessary conflict requires STOP.
