# P5O2A4C - Sequence Context / Element / Source Ownership Decomposition

## Result

The complete 22-method Sequence Context / Element / Source condensation group has been removed from ExceptionFlowAnalyzer without moving the 63-method recursive contextual-fact SCC or changing fact semantics. Eight collection-shape/source-projection methods extend the existing ExceptionFlowSequenceCollectionFactsProvider; thirteen currentness and source-observation methods belong to the new ExceptionFlowSequenceContentPreservationFactsProvider; the single call-context-to-foreach projection extends the existing ExceptionFlowCallContextFactProjector.

| Measurement | Before | After |
| --- | ---: | ---: |
| Analyzer-owned downstream methods | 49 | 27 |
| residual fact families | 6 | 3 |
| SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC -> Analyzer | 32 | 12 |
| SCC -> providers/components | 61 | 81 |
| SCC -> SemanticScope | 18 | 18 |
| provider/component -> Analyzer | 0 | 0 |
| Analyzer partials | 42 | 41 |
| Analyzer nonblank SLOC | 20,975 | 19,948 |

The previously approximate Sequence edge count is exact: 20 SCC-to-group edges. The group has zero edges back to the SCC, 22 internal edges, zero edges to currently Analyzer-owned methods, nine edges to existing providers/components, and one edge to ExceptionFlowSemanticScope. All 22 induced method SCCs are singletons.

## Gate, scope, and repository state

- Initial HEAD: cc621e85d350457816493b69cc53d65bca19a01a (Separate return condition fact ownership.).
- Initial status: protected foreign M ../../.gitignore only.
- Initial tracked Core bin/obj churn: none.
- The protected foreign file and both protected stashes were not changed.
- No commit, push, stash operation, reset, clean, checkout, or restore was performed.
- The audit reused the 252-node graph and existing 63-node SCC audit; only the 22-method Sequence group and immediate dependencies were reconstructed.

## Exact method inventory

| Original family | Full signature | Final owner | Responsibility |
| --- | --- | --- | --- |
| Sequence Context | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsForeachIterationVariableProvenNonNullByCallContext(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax, Microsoft.CodeAnalysis.ILocalSymbol, Microsoft.CodeAnalysis.SemanticModel, XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowCallContext)` | `ExceptionFlowCallContextFactProjector` | call-context non-null-element projection to foreach iteration variable |
| Sequence Context | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsSequenceParameterFactStillCurrent(Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax, Microsoft.CodeAnalysis.IParameterSymbol, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | foreach-site parameter fact currentness |
| Sequence Context | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsSequenceParameterFactStillCurrentAtUse(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax, Microsoft.CodeAnalysis.IParameterSymbol, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | top-level parameter fact currentness |
| Sequence Context | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.DoesStatementPreserveSequenceParameterContents(Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax, Microsoft.CodeAnalysis.IParameterSymbol, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | parameter statement preservation |
| Sequence Context | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsSupportedSequenceNullObservation(Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | null-comparison observation classification |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsLocalSequenceInitializerStillCurrent(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax, Microsoft.CodeAnalysis.ILocalSymbol, Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | local initializer currentness |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.DoesContainingStatementEntryPreserveLocalSequenceContents(Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax, Microsoft.CodeAnalysis.ILocalSymbol, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | nested statement-entry preservation |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.DoesStatementPreserveLocalSequenceContents(Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax, Microsoft.CodeAnalysis.ILocalSymbol, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | local statement preservation |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.DoesSyntaxPreserveLocalSequenceContents(Microsoft.CodeAnalysis.SyntaxNode, Microsoft.CodeAnalysis.ILocalSymbol, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | local syntax preservation |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsSupportedReadOnlySequenceObservation(Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | read-only Count/Length observation classification |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsFrameworkCollectionCountProperty(Microsoft.CodeAnalysis.IPropertySymbol)` | `ExceptionFlowSequenceCollectionFactsProvider` | supported framework Count-property shape classification |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.TryGetElementPreservingSequenceSource(Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax, Microsoft.CodeAnalysis.IMethodSymbol, Microsoft.CodeAnalysis.IMethodSymbol, out Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax?)` | `ExceptionFlowSequenceCollectionFactsProvider` | element-preserving LINQ source projection |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsElementPreservingSequenceMethod(Microsoft.CodeAnalysis.IMethodSymbol)` | `ExceptionFlowSequenceCollectionFactsProvider` | element-preserving LINQ method classification |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsDictionaryValuesProperty(Microsoft.CodeAnalysis.IPropertySymbol)` | `ExceptionFlowSequenceCollectionFactsProvider` | framework dictionary Values-property classification |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsKnownEmptyDictionaryCreation(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceCollectionFactsProvider` | empty framework dictionary creation classification |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsDictionaryType(Microsoft.CodeAnalysis.ITypeSymbol)` | `ExceptionFlowSequenceCollectionFactsProvider` | framework dictionary type classification |
| Sequence Element Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsEqualityComparerType(Microsoft.CodeAnalysis.ITypeSymbol)` | `ExceptionFlowSequenceCollectionFactsProvider` | framework equality-comparer type classification |
| Sequence Source Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsSourceHelperArgumentProvenToPreserveSequenceContents(Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | source-helper argument-to-parameter preservation proof |
| Sequence Source Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsKnownMaterializedSequenceType(Microsoft.CodeAnalysis.ITypeSymbol)` | `ExceptionFlowSequenceCollectionFactsProvider` | materialized array/list type classification |
| Sequence Source Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.DoesSourceParameterPreserveSequenceContents(Microsoft.CodeAnalysis.IParameterSymbol, Microsoft.CodeAnalysis.SemanticModel)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | source helper parameter-use preservation proof |
| Sequence Source Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsInsideNestedCallable(Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax, Microsoft.CodeAnalysis.SyntaxNode)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | captured/nested callable observation rejection |
| Sequence Source Facts | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer.IsDirectForeachSequenceObservation(Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax)` | `ExceptionFlowSequenceContentPreservationFactsProvider` | direct foreach observation classification |

The JSON audit records original/current file and line, direct callers/callees, SCC/group/other-Analyzer callers, inputs, output, SemanticModel/SemanticScope/CallContext/return/cache dependencies, and recursive-callee status for every method.

## Family classification and dependency graph

The original buckets were 5 Context methods (4 SCC edges), 12 Element methods (15 SCC edges), and 5 Source methods (1 SCC edge). Actual responsibilities are:

1. Sequence collection shape and source projection (8, leaf): framework Count/Values/type/constructor classifiers and element-preserving source projection in the existing collection provider.
2. Sequence content preservation (13, intermediate): parameter/local currentness, read-only/null/foreach observations, and bounded source-helper body inspection. Stateless; no cache or call context; SemanticScope only for exact local source lookup.
3. Sequence call-context projection (1, hub-facing): projects an existing NonNullElements parameter fact to a foreach iteration variable after currentness is proven.

Dependency direction is acyclic: Call-context projection -> Content preservation -> Collection shape/source projection. Content preservation also points one-way to symbol usage, data-flow facts, dereference discovery, argument mapping, runtime-dispatch classification, and SemanticScope. No component points to ExceptionFlowAnalyzer.

Range facts are a real but unmodified 9-method residual family. Successful sequence validation is a real but unmodified 14-method discovery family. The existing collection family is real and now owns the eight compatible methods. Return-derived Sequence Facts are not an independent downstream family. Sequence projection exists as the one-method call-context projection; no separate position/index family or Sequence cache exists.

## Extraction order

1. Eight leaf collection classifiers/projections moved into the existing collection provider.
2. Thirteen content-preservation methods moved as one closed responsibility.
3. The one-method call-context projection moved into the existing projector.

Each stage built with zero warnings/errors, ran the focused slice, and reproduced the 16-finding self-analysis baseline before the next stage. No Analyzer forwarding method remains. The old SequenceSourceHelpers Analyzer partial became unnecessary and was replaced by the concrete provider file.

## Ownership and invariant status

- New type: ExceptionFlowSequenceContentPreservationFactsProvider; static, sealed-at-runtime, nonvirtual, stateless, no interface, callback, service locator, Analyzer field, or cache.
- Reused owners: ExceptionFlowSequenceCollectionFactsProvider and ExceptionFlowCallContextFactProjector.
- Provider-to-Analyzer remains zero by source guard and direct ownership test.
- SemanticScope remains compilation-local; SemanticEnvironment remains cross-scope.
- Runtime dispatch, argument mapping, symbol usage, stable-member facts, nullability, guards, primitive/source-position facts, data flow, and dereference discovery remain authoritative.
- ConditionalWeakTable cache remains with the residual Analyzer SCC; DataFlow cache with ExceptionFlowDataFlowFactsProvider; dereference cache with ExceptionFlowDereferenceFactDiscovery.
- SameCompilation, ReferencedProject, SupportingSource, and conservative MetadataOnly behavior are unchanged. No precision, context-key, cache, summary, source-fork, compiler-conditional, reflection, historical-build, worker, or IPC change occurred.
- All four old P5O2A2 direct-reference blockers remain zero. Neutral and Canonical Core remain Roslyn-free.

## Residual graph and readiness

The residual Analyzer-owned closure contains 27 methods: Dictionary value facts (4 methods / 4 SCC edges), Sequence range and dictionary mutation (9 / 7), and Successful sequence validation (14 / 1). The 63-method SCC and 112 internal edges are unchanged.

SCC extraction and P5O2B are not ready because 12 direct SCC-to-Analyzer fact edges remain. The single smallest next blocker is the four-method Dictionary value-facts family receiving four direct SCC edges.

## Validation

- Focused Sequence/Source/Collection/Return interaction/provider/SemanticScope slice: 63/63.
- Broad P5/P6/G plus Canonical/Semantic/Dependency/Provider/Runtime Dispatch/Sequence slice: 966/966.
- Full suite: 2,530/2,530, zero failed/skipped (2,529 baseline plus one ownership guard).
- Build: zero warnings and errors.
- Final Self Analysis: 16 in 58,778 ms; DOC610=0, DOC611=1, DOC631=15, DOC632=0.
- Canonical diff: zero added/removed/changed evidence; exact finding arrays equal; baseline hash 15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B.
- E1 canonical profile: 7 candidates, 5 complete, 2 expected fail closed, 0 unexpected, 0 potential bugs.
- Dapper: ConfigurationReconstructed / ConfigurationUnsupported for exact optimization=release-debug-plus.
- OneOf: PdbValidated / MissingArtifact. Semver: complete source-backed result.
- Four modes are covered by focused/broad/full suites. Program.Main and IOException boundaries are unchanged.
- No crash or flake; no performance conclusion from individual durations.

## Files and format

Production changes extend two existing owners, add the preservation provider, remove the obsolete SourceHelpers partial, and redirect callers. One architecture test was added; existing behavior characterization was sufficient.

All changed C# is CRLF-only, UTF-8 without BOM, with no bare CR/LF, trailing whitespace, or new var. git diff --check is clean for task files. Seven tracked Core bin/obj files are build churn.

## Requested close-out matrix

1. HEAD: cc621e85d350457816493b69cc53d65bca19a01a.
2. Initial status: protected M ../../.gitignore only.
3. Scope: current 22-method Sequence group and immediate dependencies.
4. Sequence-group method count: 22.
5. Complete method list: inventory table and JSON.
6. SCC-to-Sequence edges: 20.
7. Sequence-to-SCC edges: 0.
8. Internal Sequence edges: 22.
9. Classification: 8 collection, 13 preservation, 1 projection.
10. Context methods: five original; four preservation/currentness plus one projection.
11. Element methods: twelve original; seven collection and five preservation.
12. Source methods: five original; one collection and four preservation.
13. Range methods: nine residual, unchanged.
14. Successful Element methods: fourteen residual.
15. Collection methods: eight extracted; provider now has twelve methods.
16. Return-derived Sequence methods: no independent downstream family.
17. Further actual family: four Dictionary value facts.
18. Graph: projection -> preservation -> collection plus established leaf owners.
19. Leaf: collection shape/source projection.
20. Intermediate: content preservation.
21. Hub: call-context projection.
22. SCC-bound: three residual families / 12 edges.
23. Order: collection, preservation, projection.
24. Rationale: leaves first, closed preservation second, context consumer last.
25. Extracted families: detailed above; no cache/state.
26. Reused providers: collection and call-context projector.
27. New provider: content preservation.
28. Provider-to-Analyzer before: 0.
29. Provider-to-Analyzer after: 0.
30. Downstream before: 49.
31. Downstream after: 27.
32. Families before: 6.
33. Families after: 3.
34. SCC-to-Analyzer before: 32.
35. SCC-to-Analyzer after: 12.
36. SCC-to-provider before: 61.
37. SCC-to-provider after: 81.
38. SCC-to-SemanticScope before: 18.
39. SCC-to-SemanticScope after: 18.
40. SCC size: 63.
41. SCC internal edges: 112.
42. SCC changed: no.
43. SCC readiness: not ready.
44. Residual families: Dictionary, Range/mutation, Successful validation.
45. Next blocker: four Dictionary methods / four SCC edges.
46. CWT cache owner: residual Analyzer SCC.
47. DataFlow cache owner: DataFlowFactsProvider.
48. Dereference cache owner: DereferenceFactDiscovery.
49. SemanticScope: unchanged authoritative owner.
50. SemanticEnvironment: unchanged.
51. RuntimeDispatchClassifier: unchanged.
52. ArgumentMapper: unchanged.
53. SymbolUsageFacts: reused.
54. Component cycles: zero.
55. Interfaces: none added.
56. Callbacks: none added.
57. Virtuality: none added.
58. Analyzer back references: zero.
59. SameCompilation regression: none.
60. ReferencedProject regression: none.
61. SupportingSource regression: none.
62. MetadataOnly regression: none; fail closed.
63. Partials before: 42.
64. Partials after: 41.
65. Analyzer SLOC before: 20,975.
66. Analyzer SLOC after: 19,948.
67. Extracted SLOC: 1,027.
68. Production files: final diff/status.
69. Tests: one ownership guard; behavior tests reused.
70. Tests before: 2,529.
71. Tests after: 2,530.
72. Focused: 63/63.
73. Architecture guards: green.
74. Direct: green.
75. ProjectTransitiveDeclaredExceptions: green.
76. ProjectTransitive: green.
77. SolutionTransitive: green.
78. P5/P6/G: 966/966 and full suite green.
79. Self Analysis before: 16.
80. Self Analysis after: 16.
81. DOC610: 0.
82. DOC611: 1.
83. DOC631: 15.
84. DOC632: 0.
85. Canonical diff: 0 / 0 / 0.
86. Baseline hash unchanged.
87. Performance: 58,778 ms; no conclusion.
88. E1: 7 / 5 / 2 / 0 / 0.
89. Dapper: expected release-debug-plus fail closed.
90. OneOf: expected exact-PDB fail closed.
91. Semver: expected source-backed result.
92. Program.Main: sole DOC611.
93. IOException: unchanged DOC631 boundary.
94. Full suite: 2,530/2,530.
95. Build: 0 warnings / 0 errors.
96. diff check: clean for task files.
97. Line endings: CRLF-only, no BOM/trailing whitespace.
98. Build churn: seven tracked Core bin/obj paths.
99. Crashes/flakes: none.
100. Architecture documentation: this report.
101. JSON audit: accompanying audit file.
102. OPEN boundaries: updated for progress.
103. BND-P6-003: Under Investigation.
104. Analyzer separation complete: no.
105. P5O2B readiness: not ready.
106. Exact next blocker: Dictionary value facts.
107. Next step: extract four Dictionary methods, remeasure.
108. Final status: handoff; no commit/push/stash/reset/clean.
