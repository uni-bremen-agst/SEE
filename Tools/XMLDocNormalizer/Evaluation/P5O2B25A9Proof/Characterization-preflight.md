# A9 characterization preflight - no old file changed

All paths are under Tests/XMLDocNormalizerTests. Fresh repository-wide inventory:
71 files, 513 literal matches; query implementation inventory: 68 files, 251 hits.
Line numbers are observations of the unchanged source, not edit authorizations.
EXACT means A9 finite-orbit/finite primitive admission, subject to full owner and
coverage checks. OUTSIDE means certificate before constructed graph expansion.

Prior conditional permissions remain conditional on a complete sound fix and
apply ONLY to their named assertions/cases. None is exercised by A9. A future
implementation prompt must approve all additional changes it will actually need.

| File | Test / precise cases | Existing assertion | Profile relevance | Required future semantics | Prior explicit permission? | Additional permission? |
|---|---|---|---|---|---|---|
| Worker/ExceptionFlowConstructedInvocationContextTests.cs | ActualCallSiteRetainsBothDimensions_ButActualContextUsesDefinition: method/containing/both/nested | 39 canonical definition; 41 empty method args | EXACT, nonrecursive nominal calls | Keep exactly the method/containing/nested construction present at call-site | Yes, precisely those two reported assertions in four cases | No within that correction; YES for any other assertion |
| same | same: non-generic; 38 declaration symbol, 40 empty value-fact Key, exact-vs-definition observations | Existing correct control/declaration/fact observations | EXACT invariant | Keep normalized declaration handle separate from concrete semantic environment | NO permission to change | Preserve; STOP if implementation needs a change |
| same | DifferentConstructionsShareActualNode_WhileExactCanonicalIdentitiesDistinguishThem: containing=false/true | 66 target equality for int/string; 67 equal repeated int | EXACT | First two semantic context keys differ; repeated identical int reuses | Yes ONLY differing-construction assertion for the two cases | No within that change; repeated-int/cache assertion MUST remain |
| same | ExactConstructedKeys_ChangingTypeRecursionHasNoFiniteReuse_FiniteHarnessOnly: growing=false, containing=false/true | 94 open ITypeParameterSymbol argument | EXACT zero-weight self cycle | Source argument System.Int32, i.e. Fault<int> | Yes, named A6 conflict 1 | No within exact correction; single context/self-edge unchanged |
| same | same: growing=true, containing=false/true | 91 folded actual node; 92 self-edge; 94 open T | OUTSIDE positive cycle | Explicit early certificate, not one completed open summary | NO structural-case permission | YES for every changed actual-graph/source/status assertion |
| same | same, independent exact candidate expansion | 118,126--128 finite prefix counts/unique contexts/pending work | Independent compiler fact, NOT product admission | Keep correct strict-growth proof; no production budget | No change needed | No edit |
| same | GenericBodyCatchSymbolsAlsoNeedInvocationSubstitution: both containing cases | 181 caught type argument remains open | EXACT | Correctly instantiated Fault<int> catch argument | Yes, named A6 conflict 2 | No within this exact argument assertion; other assertions unchanged |
| same | SupportingSourceResolverPreservesContainingConstruction_ButNotMethodArguments: containing=false | 164 unequal invocation identities | EXACT if unique scope mapping exists | Full constructed method identity survives rebind | Yes, named A6 conflict 3 | No within precisely that correction |
| same | same: containing=true; both cases 160--161,165 | Correct containing owner/construction and normalized handle | EXACT invariant | Preserve full owner/container and legitimate declaration handle | No broader permission | YES if anything else must change |
| same | GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness: both cases | 203 single entry; 205 empty args | OUTSIDE for its live List-growth summary | Current and Historical same classified failure/certificate, not empty-args success | NO | YES for result/status expectations; lifetime/isolation/parity invariants must stay |
| Worker/ExceptionFlowBoundedGenericContextTests.cs | RealRecurrences_DistinguishFiniteClosureFromIncreasingExactArguments: identity/constant | 107 open source argument | EXACT identity/closing | First source Fault<string>, not open T | Yes, named A6 conflict 4 | No within these source corrections |
| same | same: constant | 105 graph count two | EXACT reset | Root, A<string>, A<int>: three concrete nodes | Yes ONLY 2->3 in constant case | No within that count; no body-graph redesign implied |
| same | same: growing/mutual/containing | 105 folded actual counts; 107 open source | OUTSIDE positive constructor dependency cycles | Early certificate; no fake complete folded node | NO | YES for actual graph/source/status changes |
| same | same: all independent Roslyn identity counts | 104 exact construction count | Independent correct compiler observation | Preserve identity=1, constant=2, growing examples=3 finite observations | No changes needed | No edit |
| same | FiniteMutualCycle_CachedPartialResultDependsOnRootEdgeOrder | Correct A5 Fault reachability; no uncertainty | EXACT finite semantic SCC | Preserve all orders and generic/non-generic cases | Historical A5 correction already exercised, not renewed blanket permission | No further change |
| same | GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness | Correct Fault entry; parity/lifetime/isolation/no uncertainty | EXACT finite cycle | Preserve all existing assertions | Historical A5 correction only | No further change |
| Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs | CompilerModel_ObserveActualSummarySourcesBeforeNormalization: constructed-callee/two-constructed-callees | 97,106--115 open T/folded source and targets | EXACT finite contexts | Concrete substituted sources and distinct int/string contexts | Yes ONLY those two case corrections previously granted | No for exact previously named assertions; YES for any additional assertion |
| same | same: closed-source/other cases | Existing closed source observations | EXACT invariant | Preserve | NOT granted | No edit |
| same | StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness: all three cases | 148 empty containing args | EXACT finite closed calls | Actual concrete containing arguments | Yes, named A6 conflict 5 | No within that correction; missing uncertainty/parity/lifetime/isolation unchanged |
| same | same: ONLY two-constructed-callees | 145 folded single type | EXACT finite pair | Outer<int>.Leaf and Outer<string>.Leaf as two distinct identities | Yes, conflict 5 only this case | No within that correction; no extra-case edits |
| Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs | OriginalNameOnlySubstitution_IsProven_ExactCandidateRepairsOnlyTheTopLevelCase | 27--28 wrong A owner | EXACT if exact B owner resolves | Correct owner of actually thrown type | Earlier precisely named characterization permission | Reaffirm applicability; no broader file grant |
| same | ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught | 54,59--60 failed nested bind/uncaught same type | EXACT nested owner | Unique reconstructed type and correct catch | Earlier precisely named characterization permission | Only that correction; other assertions unchanged |
| same | GenericCatchComparison_ErasesDifferentConstruction_IndependentOfNameOnlyNormalizer | 86 false equality expressed as true catch | EXACT concrete catch | Fault<int> != Fault<string>; not caught | Earlier precisely named permission | No additional within exact assertion |
| same | StopRule_LeavesOriginalProductionAndClosedFindingAdmission | 104--105 unsafe lookup fingerprint | Exact finite owner normalization | Remove dependency on unsafe first-match lookup | Earlier precisely named permission | Finding-closure assertions NOT granted |
| Worker/ExceptionFlowHistoricalSolutionFindingTests.cs | AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted | about 105 wrong type owner A | EXACT finite owner | Actual assembly-qualified thrown owner | One of exactly TWO original B25 grants | No other B25 change; historical report/audit frozen |
| same | StopRule_DoesNotIntroducePolicyRoutingAttestationOrPidWorkarounds | about 195 unsafe lookup characterization | Exact unique owner resolution | Correct unique type/assembly bind, no first match | Second precise B25 grant | No other B25 assertion change |
| Worker/ExceptionFlowGenericExpansionBoundaryTests.cs | ExactMaterialization_HasFreshSuccessorAtEveryWitnessStep: all four containing/dictionary combinations | Bare old key equality; actual run.Graph.Count==2; Build expects success | OUTSIDE positive cycle | Admission failure/certificate before queue; retain independent 24-step compiler growth proof | NO A6 witness edits authorized | YES for actual Build/result/folding assertions and affected output metadata; do not change prefix proof |
| same | IdentityAndClosingSubstitution_RepeatAnExactState: all four cases | Exact candidate successor/repeat identities | EXACT identity/constant | Existing assertions remain correct; Build succeeds | No change necessary | No assertion edit; YES if touching legacy output metadata/path |
| Worker/ExceptionFlowRecursiveSummaryClosureTests.cs | all 33 cases | Finite closure/root order/complete-only cache/isolation | EXACT A5 invariants | Preserve; add NEW profile/context tests separately | None for additional edits | STOP if an old assertion needs adjustment |
| Check/Semantic/Exceptions/CanonicalIdentityTests.cs | constructed/nested/scoped identity tests | Exact structured type/owner equality and inequality | EXACT canonical invariants | Preserve; unsupported kind is not fake nominal equality | No changes authorized | Any required change needs permission |
| Check/Semantic/Exceptions/ExceptionFlowSummaryGraphTests.cs | CallableKey_NormalizesSymbolAndIncludesContext | 45--51 normalized declaration/key equality/hash | EXACT legitimate empty-environment/declaration behavior | Keep declaration handle separate from actual constructed environment; don't demand unequal hash codes | No necessary change | YES if contract of this old test must change |
| Check/Semantic/Exceptions/ExceptionFlowCrossCompilationResolverTests.cs | ResolveMethod_ConstructedGenericContainingType_PreservesSubstitution | Preserved string result/parameter/container and nondefinition method | EXACT correct current containing behavior | Preserve while adding method binding | No changes needed | No edit |
| Worker current/historical finding/response validation tests | Existing rejection of uncertainty/truncation and receipt/policy contracts | Complete success vs original validated failure | EXACT vs certificate boundary invariants | Keep rejection, mode, owner, attestation, PID/lifetime, isolation and routing checks | No blanket permission | Add new rejection cases; do not weaken old ones |

Other ordinary nongeneric, contextual-value-fact, dispatch, framework, reporting
and compiler hits are invariants or unrelated matches. They do not authorize
opportunistic assertions changes. Future typed-classifier tests must also exercise
coverage false negatives, malformed/ambiguous scope mappings, source-vs-PE owner
receipts, shared Current/Historical rejection, exact repeat/cache isolation and
the early registrar barrier.

Existing test writers can emit old ProductionCorrected/ProductionChanged=false
metadata and old package-relative paths. No old witness output has been changed
here. A later implementation must explicitly inventory and obtain permission for
any necessary writer/metadata correction, generate NEW evidence under its own
package, and preserve all frozen B25--A9 artifacts. An old report is never silently
updated to look as though it already described the corrected implementation.
