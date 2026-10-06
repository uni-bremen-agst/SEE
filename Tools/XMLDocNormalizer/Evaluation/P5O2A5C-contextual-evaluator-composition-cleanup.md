# P5O2A5C - Contextual Evaluator Composition / Facade Cleanup

Completed. **P5O2A6 Architecture / Readiness Closure ready: yes. Minimal blocker: none.** A6 itself has not begun; this is not worker/dual-Roslyn readiness.

Starting HEAD: `4ddd4113d753c04edb9cce7cc570b896ea83ab6d` (`Extract contextual fact evaluator.`), branch `fix/documentation`, 2026-10-06. Before production changes: status/stat/full diff/HEAD/log read, existing A5B evidence reviewed and fresh graph/composition measured. Only root .gitignore and seven Core bin/obj paths were initially dirty; preserved as foreign WIP/build output.

## Recovery after laptop restart

The recovered HEAD and complete WIP were audited before any further edit. Both seed proxies were already removed, all fourteen sites redirected, the empty Nullability partial deleted, and the architecture contract updated. Production/test diffs were complete and correct; no truncated or inconsistent production/test file was found. Nothing was reset, reimplemented or repeated at the production level.

All six previous TRX files had Completed outcomes, complete start/finish timestamps and passing counters; both graph snapshots and the earlier 16-finding Self Analysis were complete and parseable. No interrupted test/build artifact is counted as passing. Only final evidence was unfinished: Markdown contained its heading, JSON contained {}, and the renderer had not completed. Its scalar PSCustomObject Count assertion had already been corrected before interruption; the underlying original/rebound hashes were equal, not a production mismatch.

After recovery only evidence/tool-report work was finished. Solution and audit builds, focused, architecture, relevant, broad and full tests, Self Analysis, scoped formatting and quality gates were revalidated for current WIP. The new bound graph is byte-identical to both completed pre-restart post snapshots; source SHA256 values are captured in JSON. Old completed results are retained as history, recovery-suffixed runs are current validation. No production/test source was changed after recovery.

A final evidence review found that historical filename/whole-file SLOC metrics in the in-memory baseline still read the current filesystem. The audit now measures these from the rebound syntax trees, restoring 29/12,977 before versus 28/12,898 after, with exact equality to the original pre-production snapshot. Semantic ownership, hashes and SCC measures were already correct. This correction affects audit code only; current post graph output remains byte-identical.

## 1. Complete pre-boundary review

All 226 actual Analyzer method declarations were inspected structurally and with bound symbols. Fifteen methods directly called the evaluator. Exactly two guard-seed-only candidates and no additional return-only proxy were found. Small orchestrators were explicitly reviewed rather than removed by name or statement count. The JSON retains the entire pre/post method census, bodies, visibility, callers, source locations, exact bound targets and classification.

| Pre-boundary method | Decision | Semantic reason |
|---|---|---|
| CreateCallContext | RemoveAnalyzerFacade | Guard allocation only; remove Analyzer owner. |
| CreateAccessorCallContext | RetainOrchestration | Accessor/setter argument projection, defaults and context construction. |
| IsThrowExpressionInExhaustiveDefinedEnumFallback | RetainOrchestration | Discard/when checks and complete declared enum-constant coverage. |
| CreateInvocationCallContext | RetainOrchestration | Reduced-extension receiver, ordinal remapping and compile-time target binding. |
| CreateKnownFrameworkContractArguments | RetainOrchestration | Framework argument projection, parameter indexes and out-parameter exclusion. |
| IsDefinitelyNonNull | RemoveAnalyzerFacade | Guard allocation only; remove Analyzer owner. |
| EvaluatePositiveInt32Comparison | RetainOrchestration | Operator/constant-side recognition and positive-int fact integration. |
| CreateSummaryCollectionInitializerCallContext | RetainOrchestration | Extension receiver offsets, params-array shape and default projection. |
| GetSummaryCollectionArgumentFacts | RetainOrchestration | Explicit user-defined-conversion rejection is a genuine fail-closed boundary. |
| CreateSummaryImplicitCallContext | RetainOrchestration | Implicit receiver and extension/default-parameter composition. |
| CreateSummaryOperationCallContext | RetainOrchestration | Operation operand-to-parameter mapping and unknown operand exclusion. |
| IsThrowExpressionProvenUnreachable | RetainOrchestration | Coalesce/conditional/switch traversal and proven branch decisions. |
| EvaluateNullComparison | RetainOrchestration | Null operand/operator shape interpreted as a branch condition. |
| EvaluateNullPattern | RetainOrchestration | Pattern shape plus fact-to-condition integration. |
| EvaluateStringPredicate | RetainOrchestration | Exact framework predicate binding and predicate truth evaluation. |

CreateDispatchCallContext is additionally retained: a seed context is composed with runtime-target rebinding. AddSummaryConstructorCallEdge and other newly direct users bind/register targets and compose paths. The three-statement GetSummaryCollectionArgumentFacts explicitly rejects user-defined conversions; its remaining evaluator call is not a removable proxy. Existing Analyzer source bodies are unchanged modulo evaluator qualification.

## 2. Seed decisions and all direct users

Both old Analyzer entries were internal and consisted solely of a fresh HashSet<ISymbol>(SymbolEqualityComparer.Default) plus return of the guarded evaluator result. Neither had Analyzer domain state, fact transformation or an orchestration boundary. Both Analyzer proxies are removed. Their two declarations (including bodies, parameter tokens and guard allocation) move unchanged to the existing evaluator partials as convenience entry overloads. No new abstraction, callback, dependency bag, fact algorithm or additional fact ownership is introduced.

CreateCallContext/4 had 12 distinct source callers and 13 invocation sites (CreateInvocationCallContext twice), including three LocalSourceAnalyzer callers outside Analyzer. IsDefinitelyNonNull/3 had one source caller/site and no external user. All 14 sites now bind directly to evaluator entries with identical argument-token multisets. Dependency direction remains upper orchestration -> evaluator -> lower facts/resolvers. All existing static imports used for genuine orchestration are retained.

Seed: `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.CreateCallContext` -> `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowContextualFactEvaluator.CreateCallContext`.

- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.AddSummaryConstructorCallEdge` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.AddSummaryDisposalEdges` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.AddSummaryPropertyGetterEdge` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.AnalyzeSummaryDelegateInvocation` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.AnalyzeSummaryImplicitObjectCreations` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.AnalyzeSummaryObjectCreations` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.CreateDispatchCallContext` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.CreateInvocationCallContext` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.CreateSummaryImplicitCallContext` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowLocalSourceAnalyzer.AnalyzeDelegateInvocation` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowLocalSourceAnalyzer.AnalyzeObjectCreations` -> evaluator directly (formerly Analyzer seed).
- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowLocalSourceAnalyzer.AnalyzePropertyAndIndexerAccesses` -> evaluator directly (formerly Analyzer seed).

Seed: `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsDefinitelyNonNull` -> `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowContextualFactEvaluator.IsDefinitelyNonNull`.

- `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.AnalyzeSummaryThrownExpressionNullability` -> evaluator directly (formerly Analyzer seed).

The JSON includes complete post-boundary classifications and all 27 external caller/callee edges, not just the seed users listed above. Evaluator now declares 65 root methods (63 original plus two non-SCC seeds); its two nested cache helpers remain additional. The original 65 A5B moved implementations and three fields are unchanged. All 63 original root declarations and both moved seed declarations are token-identical. All 224 retained Analyzer bodies are token-identical modulo evaluator-call qualification.

## 3. Measured ownership and composition

| Metric | Before | After |
|---|---:|---:|
| AnalyzerPrefixFiles | 29 | 28 |
| AnalyzerWholeFileNonblankSloc | 12977 | 12898 |
| AnalyzerDeclarations | 28 | 27 |
| AnalyzerOwnedNonblankLines | 12483 | 12412 |
| AnalyzerMethods | 226 | 224 |
| AnalyzerBoundaryMethods | 15 | 21 |
| AnalyzerEvaluatorSites | 17 | 26 |
| EvaluatorExternalEdges | 16 | 27 |
| EvaluatorExternalCallers | 15 | 24 |
| EvaluatorExternalTargets | 4 | 4 |
| EvaluatorExternalSites | 17 | 29 |
| EvaluatorRootMethods | 63 | 65 |
| SccMethods | 63 | 63 |
| SccInternalEdges | 112 | 112 |
| SccIngressEdges | 16 | 16 |
| SccIngressCallers | 15 | 15 |
| SccEntryTargets | 4 | 4 |
| SccEgress | 791 | 791 |

Only the now-empty ExceptionFlowAnalyzer.Nullability.cs is deleted; recoverable from starting Git HEAD. No partial merging/renaming for metrics. No empty Analyzer declaration remains. Historical prefix/whole-file metrics include the provider-only old CWT filename; actual declaration/owner-line metrics intentionally do not.

SCC membership, all 112 internal kind-labelled edges and all 791 egress edges are exactly equal, not just count-equal. SCC ingress stays 16/15/4: the two seed caller identities change owner, while the underlying guarded targets stay unchanged. Whole evaluator external ingress grows 16 -> 27 edges, 15 -> 24 callers and 17 -> 29 sites because the formerly hidden seed users are now direct (three belong to LocalSourceAnalyzer). There are still four external overload targets; the two old guarded entries are replaced at the outside boundary by seed overloads. Distinguish whole evaluator ingress from SCC ingress.

Historical A5B prose incorrectly attributed the 17th invocation to EvaluateNullComparison. The fresh bound audit shows EvaluatePositiveInt32Comparison calls GetExpressionValueFacts on both alternative operand sides. The 17-site/16-edge number was correct; prior immutable evidence is not edited. Post-cleanup the additional repeated seed call is in CreateInvocationCallContext.

Evaluator -> Analyzer = 0; reachable lower fact/resolver components -> Analyzer = 0; logical inter-component cycles = 0. These gates cover the bound source closure plus initializer/delegate/compiled-IL checks, not a claim that upper LocalSourceAnalyzer orchestration never uses Analyzer. The one expanded DataFlow provider/cache type cycle remains internal to that provider; it is unchanged and collapses to zero inter-component cycles. No Analyzer delegate/callback/state capture is introduced.

Cache/guard ownership is unchanged: static readonly CWT weak SemanticModel identity, original partition first-use, OriginalDefinition/SymbolEqualityComparer.Default keys and bool positive/negative results. Guard-before-cache/finally and independent invariant seeds remain unchanged. Each new convenience call creates its own fresh guard; no shared guard/reset/lifetime redesign. Lookup/store locks, computation outside locks and first-result TryAdd are unchanged. All foreign provider state stays at its owner.

## 4. Tests and complete validation

A5B semantic/cache/lifetime tests are retained. The exact SCC/entry ownership contract now permits only the two explicit convenience overloads outside the original 63-method set and forbids old Analyzer duplicates. One additional structural architecture test rejects return-only/guard-only Analyzer proxies across all partials, rejects empty partial declarations and checks fresh default-comparer seed bodies. Its source check is complementary to the bound census and existing compiled-IL closure test (not an alias-proof standalone semantic binder). Existing call-context, nullability and all-mode regressions cover direct callers; no redundant semantic fixture is added.

| Run | Passed / total | Failed | Not executed |
|---|---:|---:|---:|
| focused | 45 / 45 | 0 | 0 |
| architecture | 27 / 27 | 0 | 0 |
| relevant | 269 / 269 | 0 | 0 |
| broad | 1768 / 1768 | 0 | 0 |
| full | 2560 / 2560 | 0 | 0 |
| full-final | 2560 / 2560 | 0 | 0 |
| focused-recovery | 45 / 45 | 0 | 0 |
| architecture-recovery | 27 / 27 | 0 | 0 |
| relevant-recovery | 269 / 269 | 0 | 0 |
| broad-recovery | 1768 / 1768 | 0 | 0 |
| full-recovery | 2560 / 2560 | 0 | 0 |

Solution and audit warning-as-error builds: 0 warnings / 0 errors. Full baseline 2559 becomes 2560 solely through one new architecture test. No MultiModule flake occurred in the recorded A5C full runs; no flake repair was attempted. Focused includes evaluator/cache/CallContext seams; relevant includes Sequence, Dictionary, Callback, Nullability, Guard, NonNull, Successful, ContentPreservation, Delegate and CallContext; architecture includes fact/scope/core/summary/local/evaluator dependency guards; broad includes Check.Semantic, Execution.Semantic and Evaluation.

Fresh solution-transitive Self Analysis: 16 findings, DOC610=0, DOC611=1, DOC631=15, DOC632=0; CLI exit 1 is expected. Against both immediate A5B raw output and committed normalized evidence: 0 added / 0 removed / 0 evidence changes. Full raw and normalized finding arrays (every property, including Message/Snippet) are exactly equal. Both full normalized arrays/hashes are retained in the JSON.

Changed/new production/test C# formatting and complete audit-project verify-no-changes gates pass. Known global format deviations are untouched. Scoped CRLF, JSON parse and git diff --check gates pass; repeat post-composition measurements are byte-identical. Cache helper/field token gates and 14 bound seed invocation/argument-token gates pass. A full run after final source formatting/CRLF also passes.

## 5. Evidence and reproduction

- [Machine-readable audit](P5O2A5C-contextual-evaluator-composition-cleanup-audit.json): full pre/post census, classifications, all callers/sites, metrics, exact SCC/egress comparisons, cache/implementation hashes, complete finding arrays and TRX counters.
- Evaluation/ContextualBoundaryAudit/CompositionAudit.cs: structural census plus bound callers/targets/argument tokens.
- Evaluation/ContextualBoundaryAudit/CompositionBaseline.cs: optional in-memory rebind of committed production sources, preserving active references/options/generated trees; no checkout or WIP change.
- Raw early pre-production measurement, enriched HEAD rebind, two post measurements, Self Analysis and TRX files: ignored artifacts/p5o2a5c. Early census declarations and SCC members/edges exactly match the enriched pre baseline.

During evidence-tool development the first in-memory baseline attempt used a cwd-relative Git tree prefix and failed before producing evidence. Using Git -C at the repository root fixed that read-only tool issue. A subsequent PowerShell scalar Count assertion was corrected without changing the equal declaration hashes. No production reconstruction or reset occurred.

~~~powershell
dotnet build XMLDocNormalizer.sln --no-restore -warnaserror
dotnet build Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj --no-restore -warnaserror
dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a5c/pre-composition-rebound.json --composition-baseline=4ddd4113d753c04edb9cce7cc570b896ea83ab6d
dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a5c/post-composition-final.json
dotnet test XMLDocNormalizer.sln --no-build --no-restore --logger "trx;LogFileName=full-final.trx" --results-directory artifacts/p5o2a5c/test-results
dotnet src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll --check --project XMLDocNormalizer --exception-analysis-mode solution-transitive --format json --output artifacts/p5o2a5c/recovery-self-analysis.json XMLDocNormalizer.sln
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/ContextualBoundaryAudit/Write-CompositionEvidence.ps1
git diff --check
~~~

## 6. Git protection and next package

Root .gitignore WIP, tracked Core build churn, global format deviations and all six stash identities/names are preserved. No reset, checkout, restore, commit, push or stash mutation. Only the authorized empty partial was removed. OPEN-PIPELINE-BOUNDARIES records A5C completion; historical A5B evidence is unchanged.

**P5O2A6 ready: yes; minimal blocker: none.** The evaluator composition seam is closed. A6 must make the broader architecture/readiness assessment itself; A5C does not pre-empt it, implement worker/IPC, run the dual-Roslyn experiment or promise historical compiler readiness.

Final status at evidence generation (also printed in the handoff):

~~~text
 M ../../.gitignore
 M Evaluation/ContextualBoundaryAudit/Program.cs
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowContextualFactEvaluatorDependencyTests.cs
 M src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfo.cs
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfoInputs.cache
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.sourcelink.json
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/ref/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/refint/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.CallContext.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.InvocationCallContext.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.Nullability.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphAccessors.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphCalls.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphConstructors.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphDisposals.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphImplicitObjectCreations.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphThrows.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.CallContext.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowContextualFactEvaluator.Nullability.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowLocalSourceAnalyzer.LocalCallables.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowLocalSourceAnalyzer.SymbolTraversal.cs
?? Evaluation/ContextualBoundaryAudit/CompositionAudit.cs
?? Evaluation/ContextualBoundaryAudit/CompositionBaseline.cs
?? Evaluation/ContextualBoundaryAudit/Write-CompositionEvidence.ps1
?? Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup-audit.json
?? Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup.md
~~~
