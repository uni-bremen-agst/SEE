# P5O2A4C – Return/Condition Ownership Slice

## Result

The heterogeneous eight-method Return/condition family has been split without
changing fact semantics. Five condition-derived string-fact methods now belong
to the existing `ExceptionFlowGuardFactsProvider`; three symbol-only
known-framework return classifiers now belong to the existing
`ExceptionFlowNullabilityFactsProvider`. No mixed return-facts provider, new
interface, callback, cache, virtual boundary, or Analyzer forwarding facade was
introduced.

| Measurement | Before | After |
| --- | ---: | ---: |
| Analyzer-owned downstream methods | 57 | 49 |
| residual fact families | 7 | 6 |
| SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC → Analyzer | 36 | 32 |
| SCC → providers/components | 57 | 61 |
| SCC → SemanticScope | 18 | 18 |
| provider/component → Analyzer | 0 | 0 |
| Analyzer partials | 42 | 42 |
| Analyzer nonblank SLOC | 21,343 | 20,975 |

The targeted graph delta is exact. `GetSourceReturnExpressionValueFacts` now
calls the guard provider instead of the Analyzer-owned condition entry point;
`GetKnownFrameworkInvocationValueFacts` now has three direct calls to the
nullability provider instead of three Analyzer-owned classifiers. These are the
four SCC edges identified by the preceding audit. No SCC method moved.

## Crash and WIP reconstruction

The continuation started at HEAD
`e9a33e8f6ad96ee412f56367842e2cd2f21b3ae1`
(`Separate runtime dispatch and stable source facts.`). The initial status after
the reported host freeze contained:

- the protected foreign `../../.gitignore` modification;
- three complete production-file changes for this slice;
- three complete test-file changes containing five new tests;
- seven tracked `ExceptionFlow.Core/bin` and `obj` build outputs;
- no new, deleted, truncated, or syntactically incomplete source file;
- no ownership Markdown or JSON artifact.

The full diff showed that all eight methods had already moved, all four SCC
call sites had already been migrated, and no old implementation or trivial
Analyzer forwarding method remained. Braces, XML documentation, and method
bodies were complete. The only active `dotnet` processes were the VS Code
project-system host and reusable MSBuild node hosts; no test, build, or Analyzer
run was left active.

Before the freeze, recorded tool results had already established a zero-warning
build, 42 focused tests, 35 four-mode tests, 524 broad P5/P6/G tests, a
2,529-test full suite, and two 16-finding intermediate self-analysis runs. The
freeze occurred while starting the final self-analysis command, before its JSON
file existed. The continuation therefore retained all code and tests, reran the
final build and self analysis, ran E1, and created the missing documentation.
No file required repair. The host freeze is not treated as an Analyzer crash;
there was no dotnet exception, native crash, managed crash, or test-host failure
in the available output.

The protected stashes `WIP full position provenance before P1 extraction` and
`WIP known-framework position provenance` remain unchanged. No commit, push,
reset, clean, restore, checkout, or stash operation was performed.

## Eight-method inventory

All methods were originally in
`ExceptionFlowAnalyzer.ReturnValueFacts.cs`. “SCC caller” below means a direct
caller in the unchanged 63-method recursive contextual-fact SCC.

| Method signature | Direct caller(s) | Direct project callees | SCC caller | Final owner |
| --- | --- | --- | --- | --- |
| `ExceptionFlowValueFacts GetFactsProvenByDirectContainingReturnBranch(ExpressionSyntax expression, SemanticModel semanticModel)` | `GetSourceReturnExpressionValueFacts` | `TryGetDirectContainingIfBranch`, `GetStringFactsProvenForStableExpressionByCondition` | `GetSourceReturnExpressionValueFacts` | `ExceptionFlowGuardFactsProvider` |
| `bool TryGetDirectContainingIfBranch(ReturnStatementSyntax returnStatement, out IfStatementSyntax? ifStatement, out bool branchConditionValue)` | condition entry point | none | indirect | `ExceptionFlowGuardFactsProvider` |
| `ExceptionFlowValueFacts GetStringFactsProvenForStableExpressionByCondition(ExpressionSyntax condition, ExpressionSyntax valueExpression, bool conditionValue, SemanticModel semanticModel)` | condition entry point; itself | itself, `UnwrapParenthesizedExpression`, `AreEquivalentStableValueExpressions`, `Normalize` | indirect | `ExceptionFlowGuardFactsProvider` |
| `bool AreEquivalentStableValueExpressions(ExpressionSyntax left, ExpressionSyntax right, SemanticModel semanticModel)` | condition evaluator | `UnwrapParenthesizedExpression`, `SyntaxFactory.AreEquivalent`, `IsStableGuardValueExpression` | indirect | `ExceptionFlowGuardFactsProvider` |
| `bool IsStableGuardValueExpression(ExpressionSyntax expression, SemanticModel semanticModel)` | stable-expression comparison | `SemanticModel.GetSymbolInfo` | indirect | `ExceptionFlowGuardFactsProvider` |
| `bool IsRoslynCompilationUnitRootMethod(IMethodSymbol methodSymbol)` | `GetKnownFrameworkInvocationValueFacts` | symbol/string comparisons only | `GetKnownFrameworkInvocationValueFacts` | `ExceptionFlowNullabilityFactsProvider` |
| `bool IsRoslynCSharpSyntaxTreeParseTextMethod(IMethodSymbol methodSymbol)` | `GetKnownFrameworkInvocationValueFacts` | symbol/string comparisons only | `GetKnownFrameworkInvocationValueFacts` | `ExceptionFlowNullabilityFactsProvider` |
| `bool IsSystemEnumToStringMethod(IMethodSymbol methodSymbol)` | `GetKnownFrameworkInvocationValueFacts` | symbol/string comparisons only | `GetKnownFrameworkInvocationValueFacts` | `ExceptionFlowNullabilityFactsProvider` |

## Responsibility A: condition-derived string facts

The five methods inspect only a return expression's directly controlling
`if`/`else` branch. They unwrap parentheses and logical negation, combine the
true side of `&&` and the false side of `||`, and recognize the false result of
the exact static `System.String.IsNullOrEmpty` and
`System.String.IsNullOrWhiteSpace` methods. The guarded and returned syntax
must be equivalent and resolve to a stable local, non-ref parameter, or
single-declaration get-only auto-property.

The produced facts are unchanged:

- false `IsNullOrEmpty`: `NonNull | NonEmptyString`;
- false `IsNullOrWhiteSpace`: `NonNull | NonEmptyString |
  NonWhiteSpaceString`;
- unsupported condition, branch, symbol, or unstable value: `None`.

The group needs `SemanticModel` for exact invocation and value-symbol
resolution. It does not consume `ExceptionFlowCallContext`, resolve a semantic
scope, construct a callee context, recursively analyze a callee, read or write
Analyzer state, or use a cache. It reuses the existing symbol-usage
parenthesis helper and value-fact normalization. The existing guard provider is
therefore the cohesive owner: it already derives facts from control-flow guard
conditions and remains a static, stateless, nonvirtual component with no
Analyzer back reference.

Characterization covers a true branch, a false `else` branch, logical
negation, nested `&&`, `IsNullOrEmpty`, `IsNullOrWhiteSpace`, stable get-only
properties, and the fail-closed mutable-property case. String equality and
inequality are not part of this five-method implementation and were not added.

## Responsibility B: known-framework return classification

The three methods classify successful invocation results as non-null. They
recognize:

- Roslyn C# `GetCompilationUnitRoot`, with the expected C# assembly,
  namespace, and `CompilationUnitSyntax` return type;
- static `Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText`, with the
  expected C# assembly, containing type, and `SyntaxTree` return type;
- parameterless `System.Enum.ToString()` returning `System.String`.

They accept only `IMethodSymbol`, require no `SemanticModel`, semantic scope,
call context, Analyzer state, or cache, and define classification predicates
rather than consuming or redesigning `KnownFrameworkExceptionModel` contracts.
`ExceptionFlowNullabilityFactsProvider` is the existing cohesive owner for
stateless type/declaration/framework non-null classification. Direct tests
cover all three positive cases, a formatted enum overload, and foreign methods
named `ParseText` and `GetCompilationUnitRoot`; behavior tests also retain the
unrelated-parser false-positive guard.

## Dependency and semantic invariants

Both owners are existing static classes, which are sealed and nonvirtual at
runtime and implement no interface. They add no callbacks, delegates, service
registry, mutable field, or cache. A source scan and architecture test both
show provider/component → `ExceptionFlowAnalyzer` = 0. The production caller
uses the owners directly; there is no Analyzer facade.

SameCompilation source bodies, semantic models, receiver/argument/member
facts, nullability, return facts, sequence/dictionary facts, and recursion are
unchanged. ReferencedProject and SupportingSource scope selection are
unchanged; MetadataOnly remains conservative and fail closed.
`ExceptionFlowSemanticScope`, `ExceptionFlowSemanticEnvironment`,
`ExceptionFlowRuntimeDispatchClassifier`, argument mapping, symbol usage,
summary construction/evaluation/session ownership, catch/local-source
semantics, and all cache owners are unchanged. Direct Analyzer references to
`ProjectClosureSemanticContext`, `SemanticCompilationScope`,
`SupportingSourceSymbolResolver`, and `CrossCompilationSymbolResolver` remain
zero. Neutral Core and Canonical Core remain Roslyn-free.

## Residual graph and P5O2B readiness

The residual 49 Analyzer-owned downstream methods form six families:
dictionary value facts, sequence call-context observation, sequence element
facts, sequence range/dictionary mutation, sequence source preservation, and
successful sequence validation. The sequence call-context/element/source
condensation cycle remains the exact smallest next blocker and owns about 20
of the remaining SCC-to-Analyzer edges. Its dependent dictionary, mutation,
and successful-validation families cannot be moved first without introducing
a component cycle.

The 63-method SCC and its 112 internal edges are unchanged. It is not ready to
move while 32 SCC-to-Analyzer edges remain. P5O2B is therefore not ready: the
same Roslyn-bound Analyzer source set still cannot be built against active and
manifest-pinned historical Roslyn without the executable dependency,
active-Roslyn leakage, a source fork, reflection, compiler-version `#if`, or a
loss of SameCompilation facts. The recommended next package is the bounded
Sequence Context / Element / Source ownership decomposition, not a historical
build.

## Tests and validation

- New behavior tests: false guarded `else` return; nested guarded return;
  mutable-property fail closed; exact known-framework signature classifier.
- New architecture test: the five and three method groups have their dedicated
  owners and no Analyzer forwarding methods.
- Focused condition/framework/context/provider/runtime-dispatch slice: 42/42.
- Explicit four-mode slice: 35/35, covering Direct,
  ProjectTransitiveDeclaredExceptions, ProjectTransitive, and
  SolutionTransitive paths.
- P5/P6/G plus Canonical/Semantic/Dependency/Provider/Runtime Dispatch/Known
  Framework slice: 524/524.
- Full suite: 2,529/2,529, zero failed, zero skipped (baseline 2,524 plus five
  tests).
- Warning-as-error build: zero warnings and zero errors.
- Self analysis after each extraction group: 16 findings and exact baseline
  finding arrays.
- Final self analysis: 16 findings in 59,624 ms; `DOC610=0`, `DOC611=1`,
  `DOC631=15`, `DOC632=0`.
- Canonical finding/evidence diff: zero added, zero removed, zero changed;
  exact finding arrays equal. The retained normalized baseline hash remains
  `15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B`.
- E1 canonical profile: Source Link enabled, `verified-line-endings`, and
  `bounded-remote`; seven candidates, five complete, two expected fail closed,
  zero unexpected, zero potential bugs.
- Dapper remains `ConfigurationReconstructed / ConfigurationUnsupported` for
  exact `optimization=release-debug-plus`. OneOf remains
  `PdbValidated / MissingArtifact`. Semver retains its expected source-backed
  result. `Program.Main` remains the sole DOC611 finding; the known
  `IOException` uncertainty remains DOC631.
- No performance conclusion is drawn from individual self-analysis durations.
- No native or managed Analyzer crash and no test flake was observed. The
  separately reported host freeze has no evidence tying it to the Analyzer.

The initial status had no task documentation and already contained the seven
Core output changes. Subsequent build validation rewrote the same seven tracked
paths. They are build churn, not production progress, and were not restored.
The foreign `../../.gitignore` change was not touched.

The machine-readable method inventory, ownership, dependency surfaces, graph
measurements, validation, and readiness decision are in
`P5O2A4C-return-condition-ownership-audit.json`.
