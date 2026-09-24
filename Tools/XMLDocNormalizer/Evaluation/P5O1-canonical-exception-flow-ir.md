# P5O1 Canonical Exception-Flow IR

P5O1 introduces a Roslyn-independent value model for identities and exception
flow summaries. Roslyn remains authoritative for local semantic analysis and
catch hierarchy. Conversion back to active symbols requires an explicit
compilation-bound resolver and fails closed on missing or ambiguous identity.

## Identity matrix

| Existing type or area | Stored Roslyn identity | Purpose and comparison | Lifetime / role | Canonical representation |
|---|---|---|---|---|
| `ExceptionFlowCallableKey` | `ISymbol` | graph-node equality and hashing | graph/cache key | on-demand `CanonicalCallableIdentity` + canonical context for transport |
| `ExceptionFlowCallContext` | callable and stable-member `ISymbol` | context-sensitive facts | graph state / summary input | `CanonicalExceptionFlowCallContext`, `CanonicalStableMemberIdentity` |
| `ExceptionFlowSummarySource` | `INamedTypeSymbol` | produced exception identity | summary field | `CanonicalExceptionFlowSummarySource` |
| `ExceptionFlowSummaryCallEdge` | target callable and caught `INamedTypeSymbol` values | graph edge and suppression | summary field | `CanonicalExceptionFlowCallEdge`, `CanonicalExceptionFlowCatch` |
| `ExceptionFlowCaughtTypeFilter` | set of `INamedTypeSymbol` | exact/base-type suppression | edge-local hierarchy query | canonical caught types rebound in an explicit compilation |
| `ExceptionFlowAnalysisResult` | four symbol-keyed sets/dictionaries | proven and external evidence paths | analysis result | `CanonicalExceptionFlowAnalysisResult` |
| Summary graph dictionaries/queue | `ExceptionFlowCallableKey` | node lookup and scheduling | graph key | existing Roslyn/context-key semantics remain authoritative; canonical values are attached for transport |
| `SupportingSourceCatalog` | `Compilation`, `SyntaxTree`, `AssemblyIdentity` | active source and artifact lookup | local P6 analysis | remains local; not a transported summary field |
| Semantic compilation scope | `Compilation`, source type symbols | active semantic resolution | local P6 analysis | remains local; explicit resolver context |
| `CrossCompilationSymbolResolver` | method-local symbols | active cross-compilation rebinding | local adapter | retained; canonical resolver adds exact transport rebinding |

The audit identified 20 durable symbol- or compilation-bearing storage/key
slots in the P6/exception-flow structures. The broader flow area contains 70
C# files, 32,254 lines, 66 files with direct Roslyn dependency, and 1,845
relevant Roslyn type/identity references. Those local Roslyn uses are not a
P5O1 migration target unless they store, compare, cache, transport, or perform
catch hierarchy over durable state.

## Canonical design

- Assembly identity is structured as name, four-part version, culture,
  public key or token, key/retargetable flags, and content type.
- Module identity carries assembly, metadata name, ordinal, and optional MVID.
  Active `IModuleSymbol` does not expose an MVID; therefore the active adapter
  records `null`, while the model can receive a validated MVID from artifact
  provenance later.
- Type identity is recursive and distinguishes named, nested, generic,
  constructed, array, pointer, type-parameter, dynamic, and function-pointer
  shapes. Display strings are not identity.
- Callable identity includes containing type, metadata name, kind, arity,
  return/ref shape, ordered parameters, substitutions, explicit-interface and
  reduced-extension identity, plus source identity for local/anonymous
  callables.
- Stable-member identity models only the productive field/property cases found
  by the audit.
- Context, source, catch, edge, uncertainty, summary, and result objects are
  immutable value objects with deterministic collection ordering.
- Existing free-form uncertainty is retained as display evidence under
  `LegacyUnclassified`; display text is not promoted to callable identity.

## Catch strategy and fail-closed resolution

P5O1 uses strategy A: canonical caught type identities are resolved in the
explicit destination compilation, after which the existing Roslyn base-type
walk remains authoritative. No name or namespace-prefix hierarchy is used.
Filtered catches remain conservative exactly as in the existing analyzer.

Canonical-to-existing conversion never selects the first same-name member.
Every callable, type, context member, source, edge, and structured target must
resolve exactly. Any unsupported, missing, or ambiguous identity rejects the
whole conversion.

The active summary graph deliberately retains its pre-P5O1 Roslyn symbol and
legacy context-key equality. An attempted direct switch to canonical key
equality changed the project self-analysis (`DOC631` 15 to 14) and was therefore
not retained. Canonical identity and context are created on demand by the
transport adapter. If either cannot be represented exactly, conversion returns
`false`; it never substitutes display-name identity or changes the active graph.

## Validation

- 37 focused canonical identity, summary, serialization, catch, result, and
  eight-fixture equivalence tests pass.
- The grouped exception-flow and P5K/P5L/P6A/P6B/P6C/G3A/G4B/G5 regression is
  799/799; the full suite is 2495/2495 with no skipped tests.
- Cross-compilation tests distinguish assemblies, overloads, generic methods,
  constructors, accessors, explicit interface implementations, nested and
  constructed types, and parameter ref kinds.
- Existing-to-canonical-to-existing conversion uses an explicit compilation and
  rejects unresolved or ambiguous identities.
- Project-scoped solution-transitive self-analysis remains exactly 16 findings:
  `DOC611=1`, `DOC631=15`, with 0 added and 0 removed canonical finding keys.
- E1 remains 7 candidates, 5 full reconstructions, 2 expected fail-closed
  cases, 0 unexpected failures, and 0 potential bugs.

## Crash audit

Four modal Windows errors were observed earlier in P5O1. All four named
`dotnet.exe`, exception code `0xE0434352`, and exception address
`0x00007FF9E77B41CA`. The managed exception type and the commands active at the
dialog times were not captured, so these observations remain **unattributed**.
Historical `P5NDiagnostic.exe` dialogs used the same visible code and address,
but that is not evidence of a shared root cause.

During continuation, one self-analysis process terminated with a native
`System.AccessViolationException` in Roslyn's
`WithUsingNamespacesAndTypesBinder.GetExtensionDeclarations`, reached through
`CastHelpers.IsInstanceOfClass`. Subsequent isolated self-analysis runs did not
reproduce it. Separate deterministic managed `NotSupportedException` failures
for type, field, and namespace summary roots were fixed by general canonical
root support. They were ordinary reported process failures, not correlated to
the four modal dialogs. All later build, test, self-analysis, and E1 commands
completed without another dialog or native crash.

## Boundary and non-goals

The complete exception summary and relevant analysis result can now be
represented without Roslyn objects. Callable queries, call context, caught
types, and uncertainty likewise have transportable forms. P5O1 does not define
worker IPC, start another process, load historical Roslyn, acquire compiler
manifests, or alter exception-finding semantics. Historical worker integration
is the planned P5O2 boundary.
