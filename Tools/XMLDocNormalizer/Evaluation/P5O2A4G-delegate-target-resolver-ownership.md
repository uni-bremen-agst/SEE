# P5O2A4G - Delegate Target Resolver Ownership

## Result

The complete delegate-target-resolution seam now belongs to the stateless
`ExceptionFlowDelegateTargetResolver`. All three production users call the
resolver directly. The Analyzer contains no forwarding method or duplicate
implementation, and the resolver has no Analyzer reference, callback, field,
cache, interface, or service-locator dependency.

This is an ownership-only refactoring. The supported expression forms,
symbol resolution, stable-local and write checks, recursion guard, Roslyn
symbol identity, Summary behavior, callback facts, and analysis modes are
unchanged.

## Gate and starting state

- Starting HEAD: `7204c01c0d146c8c3863a14ec610f3bb7d8e04db`
  (`Separate successful sequence validation ownership.`).
- P5O2A4F was therefore committed separately before A4G.
- Initial status contained only the protected foreign `../../.gitignore`
  change and the pre-existing tracked Core `bin/obj` churn.
- All six stashes were left unchanged.
- No reset, checkout, restore, clean, commit, push, or stash operation was
  performed.

The starting architecture was the P5O2A4F baseline: zero Analyzer-owned
downstream fact methods, zero residual fact families, a 63-method / 112-edge
contextual-fact SCC, zero SCC-to-Analyzer fact dependencies, 93
SCC-to-component edges, 18 direct SCC-to-`ExceptionFlowSemanticScope` edges,
zero component-to-Analyzer edges, zero component cycles, 40 Analyzer partials,
and 18,631 nonblank Analyzer lines.

## Exact ownership move

The current source contained exactly the five methods predicted by the
architecture plan and no additional exclusive helper:

| Method | Overloads | Old owner | New owner |
| --- | ---: | --- | --- |
| `TryResolveDelegateTarget` | 2 | `ExceptionFlowAnalyzer` | `ExceptionFlowDelegateTargetResolver` |
| `UnwrapDelegateExpression` | 1 | `ExceptionFlowAnalyzer` | `ExceptionFlowDelegateTargetResolver` |
| `TryResolveStableDelegateLocal` | 1 | `ExceptionFlowAnalyzer` | `ExceptionFlowDelegateTargetResolver` |
| `HasDelegateLocalWrites` | 1 | `ExceptionFlowAnalyzer` | `ExceptionFlowDelegateTargetResolver` |

The implementation was moved without semantic edits. The public-internal
entry overload still creates a `HashSet<ISymbol>` with
`SymbolEqualityComparer.Default`; the recursive overload still recognizes
anonymous functions, explicit and implicit one-argument delegate creation,
non-`DelegateInvoke` method symbols, stable locals, and exactly one viable
method candidate. Unwrapping still covers parentheses, casts, checked
expressions, and nullable-warning suppression. Stable locals still require one
declaration and initializer, a semantic model from `ExceptionFlowSemanticScope`,
no assignment/increment/decrement/ref/out write found through
`ExceptionFlowCatchSemantics`, and the same symbol-cycle guard.

## Direct users and dependency direction

The audit found three rather than only the two callers anticipated by the
prompt. Every caller now directly invokes
`ExceptionFlowDelegateTargetResolver.TryResolveDelegateTarget`:

1. `ExceptionFlowAnalyzer.AnalyzeSummaryDelegateInvocation`;
2. `ExceptionFlowAnalyzer.IsCallbackReturnDefinitelyNonNull`;
3. `ExceptionFlowLocalSourceAnalyzer.AnalyzeDelegateInvocation`.

The resolver depends only on Roslyn syntax/symbol/operation types,
`ExceptionFlowSemanticScope`, and `ExceptionFlowCatchSemantics`. It declares
no fields. Neither lower dependency refers back to the resolver, and the
resolver does not refer to `ExceptionFlowAnalyzer`, `Func<>`, or `Action<>`.
The resulting direction is:

    Summary delegate invocation -----------\
    Callback / CWT contextual fact ----------> DelegateTargetResolver
    Local source delegate invocation -------/       |
                                                    +-> SemanticScope
                                                    +-> CatchSemantics

## Architecture measurements

| Measurement | P5O2A4F | P5O2A4G |
| --- | ---: | ---: |
| Analyzer-owned downstream methods | 0 | 0 |
| residual fact families | 0 | 0 |
| SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC -> Analyzer fact dependencies | 0 | 0 |
| SCC -> Analyzer delegate-resolution seam | 1 | 0 |
| SCC -> components | 93 | 94 |
| SCC -> `ExceptionFlowSemanticScope` | 18 | 18 |
| component -> Analyzer | 0 | 0 |
| resolver -> Analyzer | n/a | 0 |
| component cycles | 0 | 0 |
| Analyzer partial files | 40 | 40 |
| Analyzer nonblank SLOC | 18,631 | 18,316 |

The sole SCC caller is `IsCallbackReturnDefinitelyNonNull`. Its former
Analyzer-owned external edge now terminates at the resolver, so the component
edge count increases from 93 to 94. No SCC member or SCC-internal call was
changed; 63 methods and 112 internal edges therefore remain exact. Direct
SCC-to-`ExceptionFlowSemanticScope` edges remain 18 because the resolver's
scope dependency is indirect.

The resolver itself has 327 nonblank lines, including its class shell and
documentation. The Analyzer loses the 315 nonblank lines of the moved method
block; its partial-file count stays 40 because `SummaryGraphCalls` retains its
Summary responsibilities.

## Tests and behavior coverage

Existing behavior tests covered nearly every required category. One focused
semantic test was added for the previously untested explicit and target-typed
delegate-object-creation branches, including nullable-suppression unwrapping.
The complete behavior coverage is:

- direct target, cast/parenthesis unwrapping, explicit and implicit delegate
  creation, and nullable suppression:
  `ImmediatelyInvokedLambda_CreatesDelegateEdge` and
  `DelegateObjectCreationAndSuppression_ResolveTargets`;
- stable lambda and method-group locals:
  `InvokedLocalLambda_CreatesDelegateEdge` and
  `LocalFunctionMethodGroup_ResolvesDelegateTarget`;
- anonymous methods: `InvokedAnonymousMethod_CreatesDelegateEdge`;
- a later local write: `ReassignedDelegate_IsMarkedUncertain` and the local
  source equivalent `ReassignedDelegate_RemainsUncertain`;
- unresolved callback: `UnknownCallback_PropagatesDownstreamGuard`;
- multiple possible callback targets:
  `MultipleCallbackTargets_PropagateDownstreamGuard`;
- unchanged Summary behavior: the complete
  `ExceptionFlowSummaryGraphLocalCallableTests` slice;
- unchanged callback/CWT behavior:
  `DOC611_ConditionalWeakTableReturnFactsTests`.

One architecture test was added. It guards exactly five declared resolver
methods with two `TryResolveDelegateTarget` overloads, zero fields, absence of
the methods from the Analyzer, all three direct users, static/interface-free
shape, no Analyzer/callback back-reference, and no reverse edge from either
resolver dependency.

## Validation

- Focused Delegate/Summary/Callback/architecture slice: 43/43.
- Relevant Summary/Callback/Contextual-Fact slice: 311/311.
- Broad `Check.Semantic` / `Execution.Semantic` / `Evaluation` P5/P6/G slice:
  1,755/1,755.
- Full suite: 2,547/2,547; zero failed; zero skipped.
- Warning-as-error solution build: zero warnings; zero errors.
- Final self analysis: 16 findings in 62,237 ms; `DOC610=0`, `DOC611=1`,
  `DOC631=15`, `DOC632=0`.
- Canonical diff against the immediate P5O2A4F artifact: zero added, zero
  removed, zero changed evidence; normalized finding arrays exactly equal.
- Retained canonical baseline hash:
  `15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B`.
- C# format gate over every A4G-changed source/test file: pass.
- The whole-solution format audit still reports seven pre-existing whitespace
  diagnostics in four unchanged files: `XmlDocSmells.cs`,
  `ArgParsing_InvalidExceptionAnalysisModeTests.cs`,
  `ArgsParsing_ExceptionAnalysisModeTests.cs`, and
  `ExternalRemoteReferenceAcquisitionTests.cs`. They were not modified.

`MultiModuleAssembly_FailsClosed` failed in two parallel aggregate
invocations. Both isolated verification runs passed immediately, and the
unchanged broad and full retries passed completely. The test is outside the
A4G files and behavior; no code change was made for this transient observation.

The machine-readable companion is
`Evaluation/P5O2A4G-delegate-target-resolver-ownership-audit.json`; the final
self-analysis artifact is
`artifacts/p5o2a4g/final-self-analysis.json`.

## Readiness

P5O2A5A - Contextual Evaluator Boundary Audit is now architecturally ready:
all downstream facts and the shared delegate-target-resolution seam have
independent owners, with no component-to-Analyzer return edge.

The intact 63-method SCC deliberately remains in `ExceptionFlowAnalyzer`.
This package does not make SCC extraction itself ready; P5O2A5A must audit the
remaining contextual evaluator boundary before any move.
