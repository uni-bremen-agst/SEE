# P5O2A4C Semantic-model resolution and fact extraction

## 1-3. Repository gate and scope

1. **HEAD:** `882d906f01264b0425974f2788ac0a8d5299874e`
   (`882d906f01 Extract downstream exception flow fact providers.`).
2. **Initial status:** the foreign `../../.gitignore` modification and two
   tracked `XMLDocNormalizer.ExceptionFlow.Core/obj` build outputs were already
   present. The preceding downstream slice was committed. No stash was applied
   or changed.
3. **Scope:** assign exact `SyntaxTree -> SemanticModel` resolution to an
   existing semantic owner, remove the Analyzer helper, and extract only the
   downstream fact families newly released by that seam. No historical build,
   worker, IPC, precision work, canonical migration, or SCC migration was
   performed.

## 4-15. `GetSemanticModelForSyntaxTree` audit

4. **Definition:** the former private Analyzer helper accepted an existing
   `SemanticModel` and a target `SyntaxTree`. It returned the supplied model for
   its own tree, called `semanticModel.Compilation.GetSemanticModel(tree)` only
   when that compilation contained the exact tree, and otherwise returned
   `null`.
5. **Direct callers:** 27 Analyzer methods: 18 in SCC 154 and nine in the
   audited downstream closure.
   - SCC callers: `IsCallbackReturnDefinitelyNonNull`,
     `IsPrivateReadonlyDictionaryFieldProvenToExcludeNullValues`,
     `TryProveSourceInvocationDefinedEnumElements`,
     `GetStaticReadonlyFieldValueFacts`,
     `TryGetInstanceFieldInitializerFacts`,
     `TryGetGetOnlyPropertyInitializerFacts`,
     `TryGetStablePropertyDeclarationInitializerFacts`,
     `IsLocalGuaranteedNonNull`, `IsForeachLocalProvenNonNull`,
     `IsSequenceExpressionProvenToExcludeNullElements`,
     `IsInvocationResultDefinitelyNonNull`,
     `TryGetSourceInvocationReturnValueFacts`,
     `IsGroupingSequenceProvenToContainNonNullElements`,
     `IsLocalSequenceExpressionProvenToExcludeNullElements`,
     `IsLocalDictionaryProvenToExcludeNullValues`,
     `DoesSourceDictionaryParameterPreserveNonNullValues`,
     `IsLocalListWithRangeAddsProvenToExcludeNullElements`, and
     `IsDictionarySequencePropertyInvariantPreserved`.
   - Downstream callers: `GetStableNonNullMemberFactsFromDirectSourceInvocation`,
     `IsConditionalWeakTableFieldInitializedEmpty`,
     `HasConstructorAssignmentToMember`,
     `HasFieldAssignmentOutsideInitializer`,
     `TryGetDirectConstructorAssignment`,
     `IsPatternLocalGuaranteedNonNull`,
     `IsSupportedDictionarySequenceProperty`,
     `DoesSourceParameterPreserveSequenceContents`, and
     `MethodSuccessfulCompletionProvesParameterElementsNonNull`.
6. **Indirectly relevant callers:** all recursive users of the 18 SCC callers
   and all users of the nine lower callers. The authoritative 63/124 audits
   were reused; the original 252 methods were not re-audited.
7. **Analyzer state:** none read or written. The helper had no context, facts,
   cache, mutable state, recursion, callback, or analyzer-instance dependency.
8. **Compilation resolution:** the existing semantic model selected the exact
   compilation. No compilation search or fallback occurred.
9. **Semantic-model resolution:** exact current-model reuse followed by an
   exact compilation containment check and Roslyn model creation.
10. **SameCompilation:** unchanged; the current model object is retained for
    its own tree, and another tree is bound only by the same compilation.
11. **ReferencedProject:** the environment maps the exact tree to the referenced
    project's owning scope, whose compilation creates the model.
12. **SupportingSource:** the supporting catalog maps the exact registered tree
    to its supporting scope, whose compilation creates the model.
13. **MetadataOnly:** there is no source tree and no synthetic model. Resolution
    remains unavailable and analysis fails closed.
14. **Foreign or ambiguous tree:** exact reference-identity ownership is
    required. Unknown trees return failure; registration already rejects one
    exact tree being claimed by multiple scopes. No first-match or fuzzy path
    exists.
15. **Equivalent lookup paths:** closure-wide lookup in
    `ProjectClosureSemanticContext`, the CWT compilation scan, dereference
    callee discovery, and canonical identity resolution were inspected.
    Closure-wide lookup retains its map/cache and now delegates local creation
    to the scope. CWT and dereference discovery now use the scope operation.
    `RoslynCanonicalIdentityResolver` retains its private exact-compilation
    lookup because it owns a distinct canonical-resolution operation and only
    iterates its own compilation trees.

## 16-32. Ownership decision and characterization

16. **SemanticEnvironment responsibility:** choose an owning scope across the
    active project closure, including referenced projects and registered
    supporting source, and retain the existing context-local semantic-model
    cache.
17. **SemanticScope responsibility:** represent one exact compilation and
    create a semantic model only for an exact syntax-tree object owned by that
    compilation.
18. **Chosen owner:** `ExceptionFlowSemanticScope` for compilation-local model
    creation; `ExceptionFlowSemanticEnvironment` remains the cross-scope owner.
19. **Why correct:** it follows the existing P5O2A2 ownership split exactly:
    environment selects a source-backed scope, scope performs a compilation-
    local Roslyn operation.
20. **Rejected alternatives:** a new resolver would duplicate an existing
    scope responsibility; putting the operation in a fact provider would mix
    semantic ownership with fact discovery; threading a Main context or
    environment through the recursive fact SCC would be a broad redesign;
    Roslyn-neutral wrappers would not help the dual-version build.
21. **API:** nonvirtual `ExceptionFlowSemanticScope.TryGetSemanticModel`, two
    internal `GetSemanticModelForSyntaxTree` overloads for existing exact
    `SemanticModel` or `Compilation` inputs, and one private shared helper.
22. **Virtuality:** zero virtual resolution methods.
23. **Interfaces:** zero new interfaces or adapters.
24. **Main-only dependencies:** zero in the scope or extracted providers.
25. **Analyzer back references:** zero from the scope and all fact providers.
    The neutral ExceptionFlow.Core project has zero package/project dependency
    references, and its 11 `Canonical*.cs` inputs contain zero Roslyn
    references. Neutral and canonical core boundaries therefore remain clean.
26. **Cache:** no new cache. The closure-wide reference-identity dictionary
    remains per analysis context. Same-compilation fact lookup remains
    uncached as before.
27. **Thread safety:** the new resolution code is stateless and adds no
    synchronization. The existing environment/cache keeps its established
    per-analysis, sequential-session assumptions. Scope `SourceTypes` lazy
    caching is unchanged.
28. **SameCompilation characterization:** the same model instance is returned
    for its own tree; a second exact tree receives a model from the same
    compilation; symbol binding resolves the expected declaration.
29. **ReferencedProject characterization:** environment lookup returns a model
    whose compilation is the referenced project's exact scope and binds the
    referenced declaration.
30. **SupportingSource characterization:** environment lookup returns a model
    from the exact registered supporting compilation.
31. **Unknown-tree characterization:** both scope-local and environment lookup
    reject a foreign tree.
32. **Old helper:** removed completely; no forwarding wrapper remains.

## 33-45. Downstream remeasurement and caches

33. **Downstream baseline:** 102 Analyzer-owned methods after the preceding
    P5O2A4C slice.
34. **After semantic separation:** 97 remained after moving the semantic helper
    plus the four closed nullability methods.
35. **Freed families:** `Nullability helpers` was directly freed by the owner
    seam. Its removal made the closed `Immutable-member facts` helper family a
    lower leaf.
36. **Extracted families:** all five baseline nullability-family methods and
    all five immutable-member helper methods, ten audited methods total.
37. **Owners:** `GetSemanticModelForSyntaxTree` belongs to
    `ExceptionFlowSemanticScope`; four nullability methods belong to
    `ExceptionFlowNullabilityFactsProvider`; five immutable-member methods
    belong to `ExceptionFlowImmutableMemberValueFactsProvider`.
38. **Provider -> Analyzer:** 0 edges.
39. **Residual Analyzer -> Provider:** 6 audited downstream edges, unchanged
    from the preceding slice. Newly extracted methods are reached directly by
    the SCC, while the nine lower semantic callers target the scope.
40. **SCC -> residual Analyzer:** 68 downstream edges remain.
41. **SCC -> Provider:** 20 edges; a further 18 SCC edges now target the
    semantic scope.
42. **Residual methods:** 92 Analyzer-owned downstream methods.
43. **Residual families:** 16 of the established 21 families. The next families
    still depend on enum, sequence, local-initializer, return, dictionary, CWT,
    context, or other residual behavior; none was newly released solely by this
    seam.
44. **ConditionalWeakTable cache owner:** remains `ExceptionFlowAnalyzer`.
45. **Cache keys/lifetime/locks:** unchanged. The CWT invariant cache remains a
    weak `SemanticModel` partition keyed by normalized field symbol with its
    existing partition lock. Syntax/symbol/query-mode keys, data-flow cache,
    lifetime, locks, and compilation isolation were not changed.

## 46-60. Behavioral and structural result

46. **SameCompilation facts:** source body, argument, receiver, stable-member,
    scalar, nullability, sequence, dictionary, return, and dereference facts
    remain unchanged.
47. **ReferencedProject:** exact owning-compilation lookup is unchanged.
48. **SupportingSource:** exact registered-scope lookup and demand-driven P6
    behavior are unchanged.
49. **MetadataOnly:** remains conservative and fail closed.
50. **SCC:** unchanged at 63 methods and 112 internal edges; no node moved.
51. **SCC extraction readiness:** not safe. Its 68 outgoing edges to the
    residual Analyzer closure would still create an Analyzer/component cycle.
52. **Analyzer partials before:** 45.
53. **Analyzer partials after:** 45.
54. **Analyzer nonblank SLOC before:** 23,291.
55. **Analyzer nonblank SLOC after:** 22,799.
56. **Analyzer SLOC removed:** 492; the two new provider files contain 473
    nonblank lines.
57. **Production files:** `ExceptionFlowSemanticScope.cs`,
    `ProjectClosureSemanticContext.cs`, the two new provider files,
    `ExceptionFlowAnalyzer.Nullability.cs`,
    `ExceptionFlowAnalyzer.ImmutableMembers.cs`,
    `ExceptionFlowAnalyzer.KnownFrameworkContracts.cs`, CWT and dereference
    discovery, plus the remaining exact Analyzer call sites now qualified to
    the scope owner.
58. **Tests:** `SemanticCompilationScopeTests.cs` adds referenced-project,
    supporting-source, same-compilation, same-model, symbol-binding, and foreign
    tree locks. `ExceptionFlowFactComponentDependencyTests.cs` adds the two
    providers to closed-component guards and adds a nonvirtual scope-resolution
    guard.
59. **Test count before:** 2,520.
60. **Test count after:** 2,522.

## 61-82. Validation

61. **Focused tests:** pre-change scope/environment characterization 9/9;
    post-seam nullability/sequence/scope slice 32/32; final touched-family slice
    49/49.
62. **Architecture guards:** 6/6 for fact-component direction, forbidden
    dependencies, cache ownership, and concrete/nonvirtual resolution. All
    prior guards also pass in the full suite.
63. **Direct:** green in the explicit four-mode slice and full suite.
64. **ProjectTransitiveDeclaredExceptions:** green.
65. **ProjectTransitive:** green.
66. **SolutionTransitive:** green. The explicit four-mode slice is 22/22.
67. **P5/P6/G:** the broad semantic/evaluation/supporting-source slice is
    807/807 and covers P5G, P5I, P5J, P5K, P5L, P6A, P6B, P6C, G3A, G4B, and
    G5; the full suite independently covers the same tests.
68. **Self analysis before:** 16 (`DOC610=0`, `DOC611=1`, `DOC631=15`,
    `DOC632=0`).
69. **Self analysis after:** 16 in 62,995 ms with the same distribution.
70. **Canonical finding diff:** zero added, zero removed, zero changed
    evidence. The normalized finding arrays are exactly equal to the committed
    P5O2A4C downstream baseline.
71. **E1 profile:** Source Link enabled, source reconstruction
    `verified-line-endings`, references `bounded-remote`.
72. **E1 result:** 7 candidates, 5 complete, 2 expected fail closed,
    0 unexpected, 0 potential bugs. Semver retains its one expected finding
    difference.
73. **Dapper:** unchanged at `ConfigurationReconstructed /
    ConfigurationUnsupported`; exact `optimization=release-debug-plus` remains
    unsupported and no approximation was introduced.
74. **OneOf:** unchanged at `PdbValidated / MissingArtifact`.
75. **Program.Main:** production code and the sole DOC611 self-analysis finding
    are unchanged.
76. **IOException:** production behavior and the known DOC631/BCL uncertainty
    are unchanged.
77. **Full suite:** 2,522/2,522, zero failed, zero skipped.
78. **Build:** `dotnet build .\XMLDocNormalizer.sln -warnaserror --no-restore`
    completed with zero warnings and zero errors.
79. **Diff check:** `git diff --check -- .` passes.
80. **Line endings:** all changed C# files are CRLF-only UTF-8 without BOM,
    bare LF, bare CR, trailing whitespace, or `var` declarations.
81. **Build churn:** the two initial tracked `obj` changes were preserved.
    Validation also regenerated tracked ExceptionFlow.Core binary/`obj`
    outputs; they are build products, not production changes.
82. **Crashes/flakes:** none. The sandboxed E1 attempt produced the known
    environmental 5 `SourceUnavailable` outcomes and two expected fail-closed
    cases; the network-enabled repeat produced the canonical 5/2/0/0 result.

## 83-92. Documentation, boundary, and P5O2B readiness

83. **Architecture documentation:** this report records ownership, extraction,
    graph measurements, validation, and the STOP decision.
84. **JSON artifacts:**
    `P5O2A4C-semantic-model-resolution-audit.json` records the semantic paths,
    ten extracted methods, 92-method residual, and updated SCC edges.
85. **OPEN-PIPELINE-BOUNDARIES:** BND-P6-003 progress and evidence were updated.
86. **BND-P6-003:** remains **Under Investigation**.
87. **Analyzer dependency separation complete:** no. The four former direct
    Main semantic blockers remain at zero, but the recursive evaluator is not
    yet independently owned.
88. **Single smallest blocker:** the 92-method residual Analyzer-owned
    downstream fact closure leaves 68 SCC -> Analyzer edges and prevents an
    acyclic owner for the intact recursive contextual fact evaluator.
89. **Assembly composition:** the Roslyn-neutral/canonical core remains clean,
    but the complete Roslyn-bound analyzer source set is still compiled as part
    of the executable; no active/historical dual composition exists yet.
90. **P5O2B readiness:** **No.** The same analyzer source cannot yet be composed
    as both active- and manifest-pinned historical-Roslyn assemblies without a
    Main executable reference while preserving the current recursive fact
    evaluator. No source fork, reflection analyzer, compiler `#if`, or
    SameCompilation fact loss was accepted.
91. **Recommended next step:** continue the existing 21-family plan against the
    92-method residual, one closed leaf family at a time, then remeasure the 68
    SCC edges. Move the SCC only when it no longer needs an Analyzer callback;
    perform assembly composition before P5O2B.
92. **Repository safety:** no commit, push, reset, checkout, restore, or stash
    mutation was performed. The foreign `.gitignore` and both protected
    stashes remain untouched. The final `git status --short` is reported in the
    completion response after final format/status verification.

