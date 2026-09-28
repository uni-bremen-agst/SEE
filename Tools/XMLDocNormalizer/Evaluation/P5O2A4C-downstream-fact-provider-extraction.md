# P5O2A4C – Downstream Fact Provider Extraction

## Result

P5O2A4C extracted three closed downstream fact families without moving any
method from the known 63-method recursive context/value-fact SCC:

- five known-framework property methods now belong to
  `ExceptionFlowKnownPropertyValueFactsProvider`;
- twelve terminating/null-guard methods now belong to
  `ExceptionFlowGuardFactsProvider`, while the general
  `StatementWritesSymbol` query was assigned to the already established
  `ExceptionFlowSymbolUsageFacts` owner;
- four intrinsic constant/string methods now belong to
  `ExceptionFlowPrimitiveValueFactsProvider`.

That is 22 of the 124 audited Analyzer-owned downstream methods. The remaining
102 methods stay in `ExceptionFlowAnalyzer`. Every new dependency points from
the Analyzer/downstream closure to a provider; there is no provider-to-Analyzer
edge, callback, interface, virtual dispatch, service locator, duplicated cache,
or new mutable state.

The extraction stops at this natural boundary. The next most shared leaf,
`GetSemanticModelForSyntaxTree`, is used by the 63-method SCC and by several
downstream families. Giving it a correct owner would require a new semantic-model
resolver/provenance decision spanning the existing semantic environment and
scope abstractions. That is explicitly outside this bounded downstream fact
provider extraction.

## Authoritative starting point

- `git status --short`: only the protected foreign `../../.gitignore` was
  modified.
- HEAD: `d59e12aa0118b021eae39259e7d03a57250a84d7`.
- HEAD subject: `Document residual contextual fact dependency.`
- The preceding P5O2A4C audit was committed and therefore passed the required
  continuation gate.
- No reset, checkout, restore, commit, push, or stash operation was performed.
- The protected stashes were not read, applied, renamed, deleted, or changed.

The audit reused, rather than reconstructed, the committed
`P5O2A4B-call-graph-after.json` and
`P5O2A4C-residual-scc-audit.json`. The complete per-method classification is in
`P5O2A4C-downstream-fact-provider-audit.json`.

## Exact downstream set

The set is the transitive descendant closure of SCC 154 in the committed P5O2A4B
graph, excluding the 63 SCC nodes themselves, then filtered to methods declared
on `ExceptionFlowAnalyzer`.

| Measurement | Value |
| --- | ---: |
| all downstream nodes | 163 |
| already extracted downstream methods | 39 |
| Analyzer-owned downstream methods before this package | 124 |
| edges induced by those 124 methods | 131 |
| self-recursive induced edges | 10 |
| cross-family induced edges | 38 |
| SCC-to-124 edges | 106 |
| 124-to-SCC edges | 0 |
| method SCCs in the 124-node induced graph | 124 |
| nontrivial method SCCs | 0 |
| leaf methods in the induced graph | 58 |
| roots within the induced graph | 46 |
| bottom-up dependency layers | 9 |
| methods reading call context | 4 |
| methods creating call context | 0 |
| methods reading value facts | 18 |
| methods creating value facts | 13 |
| methods accessing Analyzer state | 0 |
| methods writing Analyzer state | 0 |
| methods accessing a cache | 0 |
| same-compilation-only methods | 0 |

The absence of a downstream-to-SCC edge is what makes lower extraction possible.
It does not make every file or every coarse family independently movable: the
families still have 38 cross-family edges, and several source files mix lower
helpers with methods that remain in SCC 154.

## Family and dependency matrix

`R/C` is the number of methods reading/creating call context. `FR/FC` is the
number reading/creating value facts. Cache and Analyzer-state counts are zero in
all 21 families. The JSON audit contains every method signature, its exact
caller/callee arrays, flags, recursion, candidate owner, risk, and final status.

| Family | Methods | R/C | FR/FC | Principal incoming dependencies | Principal outgoing dependencies | Candidate owner | Disposition |
| --- | ---: | ---: | ---: | --- | --- | --- | --- |
| Call-context projection/defaults | 2 | 1/0 | 1/0 | SCC, extracted owners | stable-source, primitive, extracted | call-context factory | residual |
| Stable source-member facts | 5 | 0/0 | 2/0 | call-context projection | enum, local, nullability, guard, extracted | stable-member facts | residual |
| ConditionalWeakTable value facts | 7 | 0/0 | 0/0 | SCC | enum, nullability, extracted | dedicated CWT fact provider | residual; cache boundary retained |
| Dictionary value facts | 4 | 1/0 | 1/1 | SCC | sequence context, extracted | dictionary fact provider | residual |
| Enum value facts | 6 | 0/0 | 0/0 | SCC, CWT, stable-source | none | enum fact provider | residual leaf family |
| Immutable-member facts | 5 | 0/0 | 0/0 | SCC | nullability | immutable-member provider | residual |
| Known-framework property facts | 5 | 0/0 | 1/1 | SCC | none | known-property provider | extracted |
| Local-initializer currency | 2 | 0/0 | 0/0 | SCC, source-position, stable-source | guard/symbol usage, extracted | local-initializer provider | residual |
| Nullability helpers | 5 | 0/0 | 0/0 | SCC and seven lower families | none | nullability plus semantic environment | residual shared leaf |
| Terminating/null-guard facts | 13 | 0/0 | 7/6 | SCC and three lower families | extracted owners only | guard provider / symbol usage | extracted |
| Numeric constant facts | 1 | 0/0 | 0/0 | source-position, extracted | none | numeric fact provider | residual leaf helper |
| Known-framework return nullability | 1 | 0/0 | 0/0 | SCC | none | framework-return provider | residual leaf helper |
| Return/condition value facts | 8 | 0/0 | 2/2 | SCC | extracted owners | return-condition provider | residual mixed-file family |
| Sequence call-context observation | 5 | 1/0 | 1/0 | SCC and three lower families | sequence element, extracted | sequence-context provider | residual |
| Sequence collection shape | 4 | 0/0 | 0/0 | SCC and three lower families | none | sequence-collection provider | residual shared leaf |
| Sequence element facts | 12 | 0/0 | 0/0 | SCC and four lower families | collection, source, extracted | sequence-element provider | residual |
| Sequence range/dictionary mutation | 9 | 0/0 | 0/0 | SCC | nullability, collection, element, extracted | sequence-mutation provider | residual |
| Sequence source preservation | 5 | 0/0 | 0/0 | SCC, element, successful validation | nullability, context, collection, element, extracted | sequence-source provider | residual |
| Source-position value facts | 7 | 1/0 | 1/1 | SCC | local, numeric, extracted | source-position provider | residual recursive family |
| Successful sequence validation | 14 | 0/0 | 0/0 | SCC | guard and four sequence families, extracted | successful-sequence provider | residual |
| Primitive scalar value facts | 4 | 0/0 | 2/2 | SCC, call-context defaults | none | primitive-value provider | extracted |

The largest induced hubs were `GetSemanticModelForSyntaxTree` (nine incoming,
zero outgoing), `GetFactsProvenByPrecedingGuard` (three incoming, five outgoing),
`DoesSourceParameterPreserveSequenceContents` (two incoming, five outgoing),
and `MethodSuccessfulCompletionProvesParameterElementsNonNull` (one incoming,
five outgoing). The latter three remain within coherent families; the first is
the semantic ownership boundary that prevents another honest low-risk slice.

## Extraction order and why it is acyclic

1. **Known framework property facts.** This was a complete five-method leaf
   family with one Analyzer entry edge and no outgoing family edge. It neither
   accepts nor creates a call context.
2. **Terminating/null-guard facts.** The family had no Analyzer dependency; its
   only outgoing calls already targeted `ExceptionFlowArgumentMapper`,
   `ExceptionFlowSymbolUsageFacts`, `ExceptionFlowDereferenceFactDiscovery`,
   `ExceptionFlowDataFlowFactsProvider`, and the known framework model. The
   general statement-write predicate was moved to the existing symbol-usage
   owner instead of being mislabeled as a guard fact.
3. **Primitive scalar facts.** Four leaf methods for constants, interpolation,
   and built-in concatenation were separated from the recursive expression
   evaluator. The recursive evaluator calls the provider; the provider never
   calls the evaluator.

After all three stages:

- extracted methods: 22;
- residual Analyzer-owned downstream methods: 102;
- residual-to-provider edges: 6;
- provider-to-residual-Analyzer edges: 0;
- edges from SCC 154 to the new providers: 9, targeting eight methods;
- edges from SCC 154 to the residual lower closure: 97, targeting 54 methods;
- residual induced edges: 104;
- extracted-provider induced edges: 21.

The order therefore peels only the bottom of the dependency graph. It does not
move an SCC method early, does not add a callback to regain Analyzer behavior,
and does not create a component cycle.

## Production changes

- Added `ExceptionFlowKnownPropertyValueFactsProvider`, a concrete static
  provider for exact Roslyn property contracts.
- Added `ExceptionFlowGuardFactsProvider`, a concrete static provider for facts
  established by terminating guards and short-circuit control flow.
- Added `ExceptionFlowPrimitiveValueFactsProvider`, a concrete static provider
  for constants and intrinsic string expressions.
- Extended the existing `ExceptionFlowSymbolUsageFacts` owner with the statement
  overload of its write query.
- Replaced implicit same-partial calls with explicit one-way provider calls.

All providers are static (therefore sealed by the runtime), nonvirtual, and
stateless. No interface or configuration layer was introduced. The providers
may consume Roslyn syntax/symbol/semantic-model inputs but do not construct an
`ExceptionFlowCallContext`; none of the extracted methods creates one.

This is not a class-per-partial transformation. The source partial boundaries
were used only when they already represented complete closed families; the
primitive provider was selected from the method graph and intentionally leaves
the recursive string-concatenation evaluator in the SCC.

## Cache, identity, and semantic-boundary audit

The 124 audited methods have zero cache-access flags. The
`conditionalWeakTableValueFactCaches` field and its cache partition remain
owned by `ExceptionFlowAnalyzer` because the cached entry method belongs to the
63-method SCC. The seven downstream CWT helpers were not moved independently,
so there is no new cache owner and no duplicate cache.

The existing weak semantic-model partitions in
`ExceptionFlowDataFlowFactsProvider` and
`ExceptionFlowDereferenceFactDiscovery` are unchanged. Call-context equality,
canonical projection, symbol identity, summary keys, summary evaluation,
same-compilation lookup, referenced-project lookup, supporting-source lookup,
metadata-only behavior, and demand-driven P6 behavior are unchanged.

Direct Analyzer references to `ProjectClosureSemanticContext`,
`SemanticCompilationScope`, `SupportingSourceSymbolResolver`, and
`CrossCompilationSymbolResolver` remain zero. The neutral and canonical cores
remain at their established Roslyn boundaries.

## Residual SCC re-evaluation and STOP decision

SCC 154 remains exactly the audited 63 methods with 112 internal edges. No SCC
method was moved. Its recursive entry/exit, mutual recursion, self recursion,
context creation, and value-fact evaluation semantics are unchanged.

The lower extraction removes useful leaf behavior but does not by itself make
the SCC a safe component. Moving the SCC now would still require ownership of
the 102-method residual lower closure, and that closure still includes shared
semantic-model resolution plus intertwined enum, sequence, local, immutable,
dictionary, source-position, return, and CWT source reasoning. Extracting those
correctly is follow-up work; hiding them behind callbacks or an Analyzer
reference would violate the package constraints.

P5O2A4C therefore stops with the safe work retained. A follow-up should first
decide whether same-compilation semantic-model retrieval belongs in the
existing semantic environment/scope layer. Only then should it peel the enum,
nullability, sequence-collection, numeric, and return-nullability leaves and
recompute SCC 154.

## Structural measurements

- Analyzer partial files: 47 before, 45 after.
- Analyzer nonblank SLOC: 24,225 before, 23,291 after.
- Nonblank SLOC in the three provider files: 899.
- Analyzer methods extracted from the audited downstream closure: 22.
- Production provider types added: 3.
- Existing provider types extended: 1.
- New production interfaces, callbacks, virtual methods, service locators,
  caches, or mutable fields: 0.
- New tests: 0; existing characterization tests provide the behavioral lock.
- New audit artifacts: this report and one 124-method JSON matrix.

## Validation

| Validation | Result |
| --- | --- |
| pre-change focused characterization | 21/21 |
| known-property stage characterization | 8/8 |
| guard stage characterization | 13/13 |
| primitive stage characterization | 14/14 |
| per-stage self analysis | 16 findings after every stage; zero raw finding diff |
| four exception-analysis modes | 24/24 |
| broad P5/P6/G/canonical/dependency slice | 1,094/1,094 |
| full suite | 2,520/2,520; zero failed, zero skipped |
| warning-as-error build | zero warnings, zero errors |
| final self analysis | 16 in 53,973 ms (`DOC610=0`, `DOC611=1`, `DOC631=15`, `DOC632=0`) |
| canonical finding/evidence diff | zero added, removed, or changed; normalized baseline hash remains `15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B` |
| E1 canonical profile | 7 candidates, 5 complete, 2 expected fail closed, 0 unexpected, 0 potential bugs |
| C# format gates | 10 files; zero BOM, bare LF, bare CR, trailing whitespace, or `var` declarations |

The first sandboxed E1 run reproduced the known environmental result: five
`SourceUnavailable` outcomes and the two expected fail-closed cases. The
network-enabled canonical repeat produced 5/2/0/0. Dapper remains fail closed at
`ConfigurationReconstructed / ConfigurationUnsupported` for the exact recorded
`optimization=release-debug-plus` value. OneOf remains fail closed at
`PdbValidated / MissingArtifact`. Semver retains the one expected source-backed
finding difference.

The final self-analysis finding objects are byte-for-byte equivalent after JSON
normalization to the committed P5O2A4C baseline. `Program.Main` remains the sole
DOC611 finding, and the known `IOException` case remains DOC631. `git diff
--check -- .` passes. No native crash, managed crash, modal dialog, or test flake
was observed.

## Scope and repository safety

No historical worker/build/IPC work was introduced. No Dapper-, OneOf-, or
package-specific production branch was added. No commit or push was made. The
foreign `../../.gitignore` modification and both protected stashes remain
untouched.

Final `git status --short`:

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.CallContext.MemberSourceFacts.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.CallContext.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.KnownPropertyValueFacts.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.LocalInitializerFacts.cs
 D src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.NullGuards.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.Nullability.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SuccessfulSequenceElements.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.ValueFacts.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSymbolUsageFacts.cs
?? Evaluation/P5O2A4C-downstream-fact-provider-audit.json
?? Evaluation/P5O2A4C-downstream-fact-provider-extraction.md
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowGuardFactsProvider.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowKnownPropertyValueFactsProvider.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowPrimitiveValueFactsProvider.cs
```

The two deleted paths are the old partial-class filenames; their content is
represented by the new provider files plus the explicitly qualified call sites.
The protected foreign `.gitignore` remains the sole pre-existing modification.
