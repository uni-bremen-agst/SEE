# P5O2A5B - Contextual Fact Evaluator Extraction

**Completed. P5O2A5C is architecturally ready; minimal blocker: none. A5C has not begun.**

Starting HEAD: `7362e7e22b9bf9fa14390b598ee6a2bbae33b0bb` (`Audit contextual evaluator boundary.`), branch `fix/documentation`. Date: 2026-10-06.
The initial status/diff/stat/HEAD/log gate was read before changes. Only the protected root ignore WIP and seven tracked Core build artifacts were dirty. A5A production/test paths were unchanged since the A4G baseline. All A5A evidence and its measured graph/state boundary were reviewed before the production move.

## 1. Boundary and outcome

| Contract | Before | After |
|---|---:|---:|
| SCC methods / internal directed edges | 63 / 112 | 63 / 112 |
| Expanded SCC | 63 | 63 |
| Ingress edges / callers / entries | 16 / 15 / 4 | 16 / 15 / 4 |
| Full direct egress | 791 | 791 |
| Fact/resolver calls / SemanticScope calls | 126 / 19 | 126 / 19 |
| Reachable explicit source callables | 284 | 284 |
| A5A Analyzer-prefix files / whole-file nonblank lines | 40 / 18,316 | 29 / 12,977 |
| Actual Analyzer declarations / owned nonblank lines | 43 / 17746 | 28 / 12483 |
| Evaluator / downstream components -> Analyzer | not extracted / 0 | 0 / 0 |
| Inter-component cycles | 0 | 0 |

Pre-A5B member signatures and 112 edge pairs equal A5A exactly. Post-A5B member, internal-edge, ingress and full kind-labelled egress sets equal the pre-set after normalizing only the new owner name back to the old name. No graph metric was forced. The older 94/18 counts remain historical partial projections, not the complete 791-edge census.
The historical file/SLOC metric counts the complete ExceptionFlowAnalyzer*.cs files, including co-located providers. There were 43 actual class declarations in the 40 original prefixed files because several files contained multiple partial declarations. After A5B, the old CWT file is provider-only but retains its filename; therefore there are 29 prefixed files but 28 actual Analyzer partial declarations. The separate owned-line count uses only each Analyzer class FullSpan (including documentation/nested state, excluding imports/namespace/other owners). No provider file was renamed merely to improve the metric.

The owner is `internal static partial ExceptionFlowContextualFactEvaluator`, in 14 family partials. It evaluates contextual values, symbols, sequence/dictionary content, calls and semantic contexts. Traversal, exception paths, Summary graphs, runtime-target orchestration and analysis output remain outside. No injection, interfaces, dependency bags, service locators, general-purpose traversal or worker/IPC changes were introduced.

## 2. Exact move set

Exactly 63 SCC method declarations moved. The following overload-distinct names/parameter counts identify this exact current set; the companion JSON `MovedSccMethods` gives every fully qualified signature, return type, visibility, source location, internal callers/callees, dependencies and state references.

| # | Method | Parameters | New partial |
|---|---|---:|---|
| 1 | `AddExplicitArgumentFacts` | 7 | CallContext |
| 2 | `AreAllConditionalWeakTableFieldValuesDefinitelyNonNull` | 3 | ConditionalWeakTableValueFacts |
| 3 | `AreAllReturnValuesDefinitelyNonNull` | 4 | ReturnNullability |
| 4 | `AreDictionaryValuesProvenNonNull` | 3 | DictionaryValueFacts |
| 5 | `AreSequenceElementsProvenDefinedEnumValues` | 3 | EnumValueFacts |
| 6 | `AreSequenceElementsProvenNonNull` | 3 | SequenceCallContext |
| 7 | `AreSequenceElementsProvenNonNull` | 4 | SequenceCallContext |
| 8 | `CreateCallContext` | 5 | CallContext |
| 9 | `DoesDictionaryTryGetValueAliasPreserveNonNullElements` | 3 | SequenceRangeFacts |
| 10 | `DoesSourceDictionaryParameterPreserveNonNullValues` | 3 | SequenceElementFacts |
| 11 | `GetDefinedEnumValueFacts` | 4 | EnumValueFacts |
| 12 | `GetExpressionValueFacts` | 3 | ValueFacts |
| 13 | `GetExpressionValueFacts` | 4 | ValueFacts |
| 14 | `GetFactsProvenByCurrentLocalStablePropertyInitializer` | 5 | LocalStablePropertyFacts |
| 15 | `GetGetOnlyPropertyValueFacts` | 3 | ImmutableMembers |
| 16 | `GetImmutableMemberValueFacts` | 3 | ImmutableMembers |
| 17 | `GetInstanceReadonlyFieldValueFacts` | 3 | ImmutableMembers |
| 18 | `GetKnownFrameworkInvocationValueFacts` | 4 | ReturnValueFacts |
| 19 | `GetSourceReturnExpressionValueFacts` | 4 | ReturnValueFacts |
| 20 | `GetStaticReadonlyFieldValueFacts` | 3 | ImmutableMembers |
| 21 | `GetStringConcatenationValueFacts` | 4 | ValueFacts |
| 22 | `IsCallbackReturnDefinitelyNonNull` | 3 | ConditionalWeakTableValueFacts |
| 23 | `IsConditionalWeakTableGetValueResultDefinitelyNonNull` | 3 | ConditionalWeakTableValueFacts |
| 24 | `IsDefinitelyNonNull` | 4 | Nullability |
| 25 | `IsDictionaryInsertionValueProvenNonNull` | 2 | DictionaryValueFacts |
| 26 | `IsDictionaryMemberInvocationSafeForNonNullValues` | 2 | SequenceElementFacts |
| 27 | `IsDictionaryOfSequencesProvenToExcludeNullElements` | 2 | SequenceRangeFacts |
| 28 | `IsDictionaryReferenceSafeForNonNullValues` | 3 | SequenceElementFacts |
| 29 | `IsDictionarySequenceAliasReferenceSafe` | 4 | SequenceRangeFacts |
| 30 | `IsDictionarySequencePropertyInvariantPreserved` | 4 | SequenceRangeFacts |
| 31 | `IsDictionarySequencePropertyReferenceSafe` | 4 | SequenceRangeFacts |
| 32 | `IsDictionarySourceHelperArgumentSafeForNonNullValues` | 4 | SequenceElementFacts |
| 33 | `IsDictionaryTryGetValueOutSequenceProvenNonNullElements` | 3 | SequenceRangeFacts |
| 34 | `IsDictionaryValuesExpressionProvenToExcludeNullElements` | 3 | SequenceElementFacts |
| 35 | `IsForeachGroupingLocalProvenToExcludeNullElements` | 5 | SequenceCollectionFacts |
| 36 | `IsForeachIterationVariableProvenDefinedEnumValue` | 5 | EnumValueFacts |
| 37 | `IsForeachIterationVariableProvenNonNull` | 4 | Nullability |
| 38 | `IsForeachLocalProvenNonNull` | 3 | Nullability |
| 39 | `IsGroupingSequenceProvenToContainNonNullElements` | 4 | SequenceCollectionFacts |
| 40 | `IsInvocationResultDefinitelyNonNull` | 4 | ReturnNullability |
| 41 | `IsListAddOfDefinedEnumValue` | 4 | EnumValueFacts |
| 42 | `IsListAddRangeReferenceSafeForNonNullElements` | 4 | SequenceRangeFacts |
| 43 | `IsListAliasMemberInvocationSafe` | 2 | SequenceRangeFacts |
| 44 | `IsLocalDictionaryProvenToExcludeNullValues` | 4 | SequenceElementFacts |
| 45 | `IsLocalGuaranteedNonNull` | 5 | Nullability |
| 46 | `IsLocalListProvenToContainOnlyDefinedEnumValues` | 5 | EnumValueFacts |
| 47 | `IsLocalListReferenceSafeForNonNullElements` | 3 | SequenceCollectionFacts |
| 48 | `IsLocalListWithRangeAddsProvenToExcludeNullElements` | 5 | SequenceRangeFacts |
| 49 | `IsLocalSequenceExpressionProvenToExcludeNullElements` | 5 | SequenceElementFacts |
| 50 | `IsPrivateDictionaryFieldReferenceSafeForNonNullValues` | 3 | DictionaryValueFacts |
| 51 | `IsPrivateReadonlyDictionaryFieldProvenToExcludeNullValues` | 3 | DictionaryValueFacts |
| 52 | `IsRangeSourceProvenToExcludeNullElements` | 3 | SequenceRangeFacts |
| 53 | `IsRangeSourceProvenToExcludeNullElements` | 4 | SequenceRangeFacts |
| 54 | `IsSequenceExpressionProvenToContainOnlyDefinedEnumValues` | 4 | EnumValueFacts |
| 55 | `IsSequenceExpressionProvenToExcludeNullElements` | 4 | Nullability |
| 56 | `IsStoredSequenceExpressionProvenNonNullElements` | 2 | SequenceRangeFacts |
| 57 | `TryGetGetOnlyPropertyInitializerFacts` | 4 | ImmutableMembers |
| 58 | `TryGetInstanceFieldInitializerFacts` | 4 | ImmutableMembers |
| 59 | `TryGetPropertyAssignmentFacts` | 6 | LocalStablePropertyFacts |
| 60 | `TryGetSourceInvocationReturnValueFacts` | 5 | ReturnValueFacts |
| 61 | `TryGetStablePropertyDeclarationInitializerFacts` | 5 | LocalStablePropertyFacts |
| 62 | `TryGetStablePropertyFactsFromLocalObjectSource` | 7 | LocalStablePropertyFacts |
| 63 | `TryProveSourceInvocationDefinedEnumElements` | 4 | EnumValueFacts |

Exactly two additional helpers moved inside the complete private sealed `ConditionalWeakTableValueFactCachePartition`: `TryGetValue(ISymbol, out bool)` and `Store(ISymbol, bool)`. Their original accessibility and implicit constructor are preserved; no other Analyzer method was moved.

| Field | New owner | Storage / initialization |
|---|---|---|
| `conditionalWeakTableValueFactCaches` | Evaluator | private static readonly CWT<SemanticModel, Partition>, `new()` |
| `gate` | Evaluator nested partition | private instance readonly object, `new()` |
| `entries` | Evaluator nested partition | private instance readonly Dictionary<ISymbol,bool>, `new(SymbolEqualityComparer.Default)` |

All 65 method-body token hashes and parameter token hashes equal starting HEAD. All three complete field-declaration token hashes equal starting HEAD. All 226 retained Analyzer methods are identical modulo the new evaluator qualification. All three retained CWT provider partial bodies in the mixed source file are token-identical. The JSON contains before/after hashes and per-method results.

Four SCC entries are internal: `CreateCallContext/5`, `AddExplicitArgumentFacts/7`, `GetExpressionValueFacts/3`, `IsDefinitelyNonNull/4`. The other 59 SCC methods are private. Only visibility adjustments are the two formerly private ingress targets becoming internal and the now evaluator-only `IsDictionaryInsertionValueProvenNonNull/2` becoming private. No algorithm, parameter, default or result was changed.

Eleven old Analyzer-only partial files were removed as part of the move; their implementations are retained in the new evaluator partials and recoverable from starting Git HEAD. Provider-only content remains in the old CWT source file; it was not copied into the evaluator.

## 3. Ingress, composition and dependencies

Thirteen orchestration callers retain their behavior and now call the evaluator entries. Two thin seed facades remain Analyzer-owned per the A5A recommendation: `CreateCallContext/4` allocates the same fresh value-source guard and delegates to evaluator `/5`; `IsDefinitelyNonNull/3` allocates the same fresh return-symbol guard and delegates to evaluator `/4`. Neither is an SCC-method duplicate. Systematic facade/composition cleanup is reserved for A5C.

There are 16 unique caller/callee ingress edges but 17 concrete invocation sites: `EvaluateNullComparison` evaluates both operands on the same edge. This is a deduplication distinction, not an additional ingress edge or caller.

The full egress remains 791: 397 property accesses, 366 invocations and 28 constructions. The 126 direct fact/resolver calls still use the same 19 owners; SemanticScope remains 19 including the Compilation overload. CallContext, value-fact extensions, metadata APIs and cache support remain the same dependencies; the three nested cache-support edges merely change owner. No downstream owner was copied or centralized.

The bound source closure, field initializer/method-reference/delegate audit, exact implementation comparison and compiled-IL architecture test find no evaluator -> Analyzer path, no component -> Analyzer path, no Analyzer state/delegate/callback/factory capture and no inter-component cycle. The IL guard includes `ldftn`, calls, constructors, field/type tokens and nested compiler-generated implementation bodies. Static analysis does not infer dynamic runtime targets or traverse metadata bodies; the unchanged algorithm/provider graph and semantic regressions supply complementary coverage.

The one expanded type-level DataFlow provider/cache cycle remains provider-internal. Folding nested implementation types to their logical owner yields zero inter-component cycles. It was not refactored.

## 4. State, cache and lifetime

The outer cache remains static readonly, with weak SemanticModel reference-identity keys and first-use partitions. Type initialization naturally belongs to the new static owner; it does not create a per-call or per-Analyzer cache. The inner key remains field OriginalDefinition with SymbolEqualityComparer.Default, value bool, including negative results. Model/Compilation worlds cannot share partitions; collection releases model/partition/symbol state. There is no reset, invalidation redesign, context key or global strong model dictionary.

Ambient guard Add occurs before lookup; a duplicate fails closed even when a positive entry is warm. Finally removes only successfully-added field guards. Invariant computation uses the same fresh independently seeded guard; recursive return/value/sequence sets retain comparer, copies, Add/Remove and finally behavior. Per-partition locking still protects lookup/store only, computation is outside locks, and TryAdd preserves the first result. Sequential caller-owned scratch collections are not made thread-safe or persisted.

Analyzer orchestration state and Domain/CallContext constructor-copied normalized state remain at their owners. Typed argument-fact dictionary/index scratch is still synchronously filled through AddExplicitArgumentFacts; it is not turned into a persistent dependency bag. Successful-dereference and DataFlow provider caches, semantic scope, delegate resolution, successful sequence validation/content preservation and all four analysis modes are unchanged.

## 5. Tests and validation

Twelve new test cases (nine semantic/lifetime, three architecture) secure all four entries, positive/negative/unknown facts, optional default projection, per-context results, mutual recursion and guard cleanup, repeated positive/negative memoization, symbol comparer/original definitions, guard-before-cache, semantic-world partition isolation and bounded GC weak lifetime. Existing semantic suites remain intact. One A4G architecture source-path expectation follows its direct CWT user to the evaluator; the provider-independent check retains its old physical provider file.

| Run | Passed / total | Failed | Not executed |
|---|---:|---:|---:|
| p5o2a5b-new-tests-final | 12 / 12 | 0 | 0 |
| focused | 44 / 44 | 0 | 0 |
| relevant | 250 / 250 | 0 | 0 |
| architecture-final | 26 / 26 | 0 | 0 |
| broad | 1767 / 1767 | 0 | 0 |
| full | 2558 / 2559 | 1 | 0 |
| known-flake-isolated | 1 / 1 | 0 | 0 |
| full-retry | 2559 / 2559 | 0 | 0 |
| full-final | 2559 / 2559 | 0 | 0 |

The initial full run failed only the pre-existing MultiModuleAssembly_FailsClosed flake (2558 passed, 1 failed). The isolated test passed; the unchanged full retry passed 2559/2559. No foreign-flake fix was made. During new-test development, three default-argument assertions initially expected PositiveInt32; existing GetConstantValueFacts only supplies NonNull for that default. The assertions were corrected to the existing contract, with no production change.

Solution and audit-tool warning-as-error builds: 0 warnings / 0 errors. Changed/new production/test C# formatting and complete audit-tool formatting gates pass. Known unrelated global HEAD format deviations were neither reformatted nor treated as A5B work. Final JSON parse, CRLF (35 A5B text files), git diff --check and byte-identical repeated post-measurement gates pass; the renderer checks these again after generation. A final full run after CRLF normalization also passes 2559/2559 without skips.

Fresh solution-transitive Self Analysis: 16 findings, DOC610=0, DOC611=1, DOC631=15, DOC632=0. CLI exit 1 is expected because documentation findings remain. Compared against immediate Post-A5A/A4G semantic baseline: 0 added, 0 removed, 0 changed evidence; complete raw finding arrays are exactly equal and normalized arrays are exactly equal. This retains Message/Snippet and every finding/evidence property, not just smell counts. The JSON stores current normalization/hashes and the historical baseline reference hash distinctly.

## 6. Reproduction and evidence

Run from Tools/XMLDocNormalizer. The audit remains outside production/solution compilation. Extraction verification is opt-in with an explicit starting ref so later normal audits do not depend on uncommitted HEAD shape.

~~~powershell
dotnet build XMLDocNormalizer.sln --no-restore -warnaserror
dotnet build Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj --no-restore -warnaserror
dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a5b/post-boundary-final.json --verify-extraction=7362e7e22b9bf9fa14390b598ee6a2bbae33b0bb
dotnet test XMLDocNormalizer.sln --no-build --no-restore --logger "trx;LogFileName=full-retry.trx" --results-directory artifacts/p5o2a5b/test-results
dotnet src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll --check --project XMLDocNormalizer --exception-analysis-mode solution-transitive --format json --output artifacts/p5o2a5b/final-self-analysis.json XMLDocNormalizer.sln
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/ContextualBoundaryAudit/Write-ExtractionEvidence.ps1
git diff --check
~~~

The invocation filters actually used were: focused = ContextualFactEvaluator, DOC611_CallContextValueFactDependencyTests, ConditionalWeakTable, DataFlowFactCache, SuccessfulDereferenceCache; relevant = Sequence, Dictionary, Callback, Nullability, Guard, NonNull, Successful, ContentPreservation, Delegate; architecture = five existing fact/scope/core/summary/local dependency suites plus the new evaluator dependency suite; broad = Check.Semantic, Execution.Semantic, Evaluation. TRX files contain exact selected case identities and outcomes.

The optional --extraction-plan mode only generates apply_patch artifacts from bound syntax spans; it never writes production. Ambiguous initial one-line patch matching was detected and replaced with whole-file syntax-span hunks. Two evidence-helper issues (cache support public accessibility and multiple provider partial declarations) were corrected; both were audit-tool issues, not production semantic changes.

- [Machine-readable A5B audit](P5O2A5B-contextual-fact-evaluator-extraction-audit.json): exact signatures, edges, before/after owner/token/state evidence, tests and canonical arrays.
- [A5A authoritative audit](P5O2A5A-contextual-evaluator-boundary-audit.json) and [A5A matrices](P5O2A5A-contextual-evaluator-boundary-matrices.md): immutable pre-boundary evidence.
- Raw 19/20 MB bound measurements, self-analysis output and TRX results remain under ignored artifacts/p5o2a5b, not runtime dependencies.

## 7. Git protection and next boundary

Protected root .gitignore WIP was not edited/reset/staged. Tracked Core bin/obj churn remains foreign build output. All six stash hashes/names are unchanged, including both protected provenance stashes. No reset/checkout/restore, commit, push or stash mutation occurred. Source-file deletions are the authorized ownership move, not discarded WIP.

**A5C ready: yes. Minimal blocker: none.** The SCC/state ownership and downstream no-return/acyclic contracts are now closed. The two deliberate seed facades and systematic composition review remain A5C work. This is not a claim of historical-worker or dual-Roslyn readiness; no A5C/A6/worker work was started.

Final status is printed in the user handoff; the evidence JSON also captures generation-time status and verified stash identities.

~~~text
 M ../../.gitignore
 M Evaluation/ContextualBoundaryAudit/Program.cs
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowFactComponentDependencyTests.cs
 M src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfo.cs
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfoInputs.cache
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.sourcelink.json
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/ref/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/refint/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.CallContext.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.ConditionalWeakTableValueFacts.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.DictionaryValueFacts.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.EnumSwitchReachability.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.EnumValueFacts.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.ImmutableMembers.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.InvocationCallContext.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.KnownFrameworkContracts.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.LocalStablePropertyFacts.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.Nullability.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.NumericConditions.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.ReturnNullability.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.ReturnValueFacts.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SequenceCallContext.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SequenceCollectionFacts.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SequenceElementFacts.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SequenceRangeFacts.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphCollectionInitializers.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphOperators.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.ThrowReachability.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.ValueFacts.cs
?? Evaluation/ContextualBoundaryAudit/ExtractionPlan.cs
?? Evaluation/ContextualBoundaryAudit/ExtractionVerification.cs
?? Evaluation/ContextualBoundaryAudit/Write-ExtractionEvidence.ps1
?? Evaluation/P5O2A5B-contextual-fact-evaluator-extraction-audit.json
?? Evaluation/P5O2A5B-contextual-fact-evaluator-extraction.md
?? Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowContextualFactEvaluatorDependencyTests.cs
?? Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowContextualFactEvaluatorTests.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.CallContext.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.ConditionalWeakTableValueFacts.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.DictionaryValueFacts.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.EnumValueFacts.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.ImmutableMembers.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.LocalStablePropertyFacts.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.Nullability.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.ReturnNullability.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.ReturnValueFacts.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.SequenceCallContext.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.SequenceCollectionFacts.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.SequenceElementFacts.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.SequenceRangeFacts.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.ValueFacts.cs
~~~
