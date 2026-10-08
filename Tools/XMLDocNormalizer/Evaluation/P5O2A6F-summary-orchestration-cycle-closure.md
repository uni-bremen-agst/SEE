# P5O2A6F - Summary Orchestration Cycle Closure

Starting HEAD: `87f07fc5801bd2dd3948a475e7fa05dddd10a936`
(`Audit P5O2A architecture readiness.`), 2026-10-08.

**READY: P5O2A6's architecture closure blocker is resolved. P5O2B may be
the next separately authorized package; no dual-version build, historical
host, package change or worker/IPC was started here.**

## Pre-implementation edge / responsibility matrix

The A6 report, complete JSON and source/state matrix were read and evaluated
before production changes. All 358 A6 production source hashes still match.
A fresh whole-compilation measurement is stored in
`artifacts/p5o2a6f/before.json`; A6's 63/112 SCC and upper cycle reproduce.
Initial Working Tree contains only the protected root `.gitignore` and seven
tracked Core build outputs. Stash identities and protected bytes were captured.

| Area | Concrete dependency | Actual responsibility | Chosen owner |
| --- | --- | --- | --- |
| A: Analyzer -> Session | CreateSummaryAnalysisSession constructs Session; AnalyzeSolutionTransitivelyThrownExceptions constructs a fresh session and calls Analyze | Session construction/lifetime and one-shot orchestration, no value facts | Existing SummaryAnalysisSession |
| B: Session -> Builder | Constructor creates one builder bound to the same environment; Analyze registers the root and calls BuildPendingSummaryNodes | Root registration is construction; draining pending graph keys and invoking analysis is session orchestration | Keep Builder constructor/root registration; move queue orchestration to Session |
| C: Builder -> Analyzer | AnalyzeSummarySymbolDeclarations calls AnalyzeSummaryImplicitConstructor, AnalyzeSummaryInstanceConstructor and AnalyzeSummaryNode; AnalyzeSummaryLocalFunction, AnalyzeSummaryAnonymousFunction and AnalyzeSummaryAccessor call AnalyzeSummaryNode | Execute declaration-specific exception-source/summary traversal, not an independent Builder query | Move these four unchanged analysis operations to existing Analyzer |
| D: Analyzer -> Builder | GetSummaryInvocationSourceCoverage calls HasAnalyzableSummaryInvocationBody(IMethodSymbol, Environment): bool; it calls TryGetSummaryInvocationBody(SyntaxNode, out SyntaxNode?): bool | Exact target declaration/body availability, semantic-tree ownership and method/local-function body shape; no session/cache/graph state | Existing SummaryTargetRegistrar owns the real two-method target/body predicate |

All 12 distinct cross-owner method edges and additional invocation sites are
preserved in the before audit. Builder reads only its readonly semanticContext
reference; it owns no Analyzer state, body catalog, cache or mutable graph.
The four declaration operations require actual Analyzer traversal, not a
smaller fact query. Keeping them under Builder would require an Analyzer
callback or moving the much larger recursive traversal closure. Relocating
these four existing operations is the smaller responsibility correction.

## Selected directed architecture

Session orchestrates graph construction and evaluation. Builder performs real
root symbol/context/key registration. Analyzer executes declaration/body
traversal. TargetRegistrar owns the exact executable method/local-function
target-body predicate. Session -> Builder and Session -> Analyzer are allowed;
Analyzer -> Session, Analyzer -> Builder and Builder -> Analyzer must be zero,
including their source type closures. No new component, delegate, provider,
service locator or forwarded Builder query is introduced.

TryBuildTransitiveSummaryGraph moves with queue orchestration to Session: it
creates a fresh graph, registers its root and fully drains pending keys. It is
not a pass-through to a Builder analysis method. Existing test graph consumers
will use this genuine session construction operation.

All four Session fields and Builder's environment field remain at their current
owners, with identical declarations and constructor assignments. Session graph
reuse, fresh-graph construction, pending-key order, fragment merge/mark order,
body shapes, symbols, contexts, recursion guards and cache lifetimes remain
unchanged. File/partial renaming is not a goal; retained legacy filenames are
classified by declared source owner, not filename.

This pre-implementation section records the decision made before code changes.
Post-change measurements and complete validation follow below.

## Exact reconstruction of the four problem areas

A: `ExceptionFlowAnalyzer.CreateSummaryAnalysisSession(Environment)` returned
the concrete Session. `AnalyzeSolutionTransitivelyThrownExceptions(member,
Environment)` called that factory and `Session.Analyze(member)`. Both are
construction/lifetime/run orchestration, not downstream Analyzer facts.

B: Session's constructor created one Builder with the identical Environment.
`Analyze` used `builder.TryRegisterSummaryGraphRoot` and
`builder.BuildPendingSummaryNodes`. Builder was session-owned, not static or
shared between sessions. Root registration binds the member symbol and semantic
scope, creates the entry context/key and registers the graph target. Queue
draining invokes real declaration analysis and merges its fragment.

C: Six distinct Builder-to-Analyzer method edges existed. Three came from
`AnalyzeSummarySymbolDeclarations`: `AnalyzeSummaryImplicitConstructor`,
`AnalyzeSummaryInstanceConstructor`, and `AnalyzeSummaryNode`. The other three
were `AnalyzeSummaryLocalFunction`, `AnalyzeSummaryAnonymousFunction`, and
`AnalyzeSummaryAccessor`, each invoking `AnalyzeSummaryNode`. Multiple syntax
branches/invocation sites belong to these edges. No Analyzer field was needed;
the dependency was executable traversal itself. Moving a factory alone would
leave Analyzer -> Builder -> Analyzer through the additional query below.

D: Analyzer's `GetSummaryInvocationSourceCoverage` directly called
`Builder.HasAnalyzableSummaryInvocationBody(IMethodSymbol, Environment): bool`.
That predicate enumerated `DeclaringSyntaxReferences`, required exact
Environment semantic-model ownership, and called
`TryGetSummaryInvocationBody(SyntaxNode, out SyntaxNode?): bool`. The latter
accepts exactly method or local-function block/expression bodies; it returns
false/null for unsupported or bodyless declarations. It was colocated with
Builder declaration traversal, not backed by a Builder collection/cache.
This is target/body-resolution responsibility: the existing TargetRegistrar
already owns target identity/scope registration and is the smallest suitable
neutral owner below traversal. The two real implementations move there; no
Session or Builder query forwarder substitutes for them.

The original three-owner graph has 12 distinct cross-owner method edges:
Analyzer -> Session 2, Session -> Builder 3, Builder -> Analyzer 6,
Analyzer -> Builder 1. The JSON additionally retains all bound upper-owner
sites/edges, including TargetRegistrar/Evaluator, so this projection is not
misrepresented as the entire graph.

## Implemented ownership and final dependency graph

Ten existing methods change owner; no new production method algorithm exists:

| From | To | Methods |
| --- | --- | --- |
| Analyzer | Session | CreateSummaryAnalysisSession; AnalyzeSolutionTransitivelyThrownExceptions |
| Builder | Session | TryBuildTransitiveSummaryGraph; BuildPendingSummaryNodes |
| Builder | Analyzer | AnalyzeSummarySymbolDeclarations; AnalyzeSummaryLocalFunction; AnalyzeSummaryAnonymousFunction; AnalyzeSummaryAccessor |
| Builder | TargetRegistrar | HasAnalyzableSummaryInvocationBody; TryGetSummaryInvocationBody |

The bound upper type graph is now:

```text
Main composition -> Session -> Builder -> Environment / graph / keys
                         |---> Analyzer -> TargetRegistrar -> Environment / scopes / resolution
                         `---> GraphEvaluator -> graph / catch / result domain
Analyzer -> existing contextual evaluator / lower facts / syntax and symbol infrastructure
```

All other existing algorithm/domain edges are retained in the machine audit;
this is an ownership sketch, not an exhaustive graph. Main's three factory/
one-shot consumers and the test helper consumers use the actual Session owner.
The retained legacy `ExceptionFlowAnalyzer.SummaryGraphEvaluation.cs` filename
now declares a Session partial: measurement uses symbols, not filenames. No
unrelated partial cleanup or rename was done.

Session is genuine orchestration: fresh/shared graph selection, root registration,
pending-key draining, declaration traversal and fragment merge/mark order.
Builder still performs substantial root semantic/context/key registration.
TargetRegistrar executes the actual tree/body predicate. There is no new type,
interface, dependency bag, God context, retained callback, lambda into Analyzer,
service locator or metric-only return wrapper.

## State, semantics and token verification

`SummaryClosureVerification` rebinds all committed production sources in memory
against starting HEAD, using the active compilation's references/options. No
checkout/reset/restore occurs. All **2,451 source-declared callables** and
**334 explicit field declarations** are compared, including constructors,
accessors and local functions. No callable is added/deleted, no field changes,
and no unexpected declaration/body change is accepted.

The explicit allowlist is limited to the ten owner moves; three visibility
changes (`AnalyzeSummarySymbolDeclarations` and `TryGetSummaryInvocationBody`
private -> internal, queue drain internal -> private); queue/root/body-query
receiver qualification; and three Main factory/one-shot receiver changes.
The four declaration-analysis bodies, both target predicates and both Session
entry bodies are exact token matches. The queue's body differs only by its
Analyzer qualification; the fresh-graph body only by `builder.` root registration.

The four Session fields and Builder's readonly Environment field remain at the
same owners with identical declarations and constructor assignments. Shared
graph reuse, per-call fresh graphs, exact Compilation/tree scope, target order,
contexts/keys, recursion guards, mark-before-merge ordering, caches and per-call
evaluation traversal are unchanged. Session remains sequential/not thread-safe.
The full 237-entry historical state inventory (236 core plus one excluded active
host entry), including positional-record-backed properties, is declaration-equal;
all 11 nonconstant static core entries and their A6 lifetime/risk descriptions
are retained. The cache's two helpers and three fields are unchanged.

No change to any of the four analysis modes, runtime dispatch, callable/delegate
resolution, exception paths or fail-closed source/body decisions is introduced.

## Repeated architecture and historical-core readiness measurement

| Gate / metric | Before | After |
| --- | ---: | ---: |
| Contextual-Fact SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC kind-labelled egress | 791 | 791 |
| SCC ingress edges | 16 | 16 |
| Evaluator/lower Fact-Resolver -> Analyzer | 0 | 0 |
| Unjustified evaluator facades | 0 | 0 |
| Builder -> Analyzer, direct / indirect | nonzero | 0 / 0 |
| Analyzer -> Builder body query | 1 | 0 |
| Analyzer -> Session | nonzero | 0 |
| Upper summary orchestration cycles | 1 | 0 |
| Proposed core raw top-level type cycles | 3 | 2 |
| Whole-source raw method-owner cycles | 2 | 1 |
| Proposed core owners / source files | 100 / 117 | 100 / 120 |
| Proposed core type edges | 387 | 386 |
| Core direct Roslyn types / metadata member signatures | 210 / 430 | 210 / 430 |

SCC member/edge/egress equality is exact, not count-only. All 27 lower owners
have zero transitive Analyzer reachability; expanded DataFlow/cache cyclicity
remains internal to that same collapsed provider. Two unchanged raw neutral
canonical domains remain: CallContext/IdentityKeyWriter, and recursive
FunctionPointerParameter/TypeIdentity. The first also accounts for the single
remaining raw method-owner cycle. These were explicitly distinguished from the
genuine orchestration blocker in A6; no cycle is hidden to obtain READY.

The two final full-compilation measurements are byte-identical:
`43CD2662A18B0267C9763DDF09157869E891CC4D5A24955968F8C3ABEED2767F`.
Compilation errors are zero. Both additionally contain the complete starting-HEAD
token comparison. Raw before/after/repeat artifacts and fingerprints are retained
in the JSON, with explicit static-audit limits (no metadata implementation or
runtime points-to/dynamic proof).

All resulting owners belong to the same Historical-Core source candidate and
need only its existing source-shared/domain capabilities. No new Main-only
Composition, Workspace or MSBuild API is pulled into that candidate outside the
already explicit active semantic-host cut. Three former E blockers are now A.
SameCompilation precision remains source-local in the active executable.

READY is an **architecture-closure** judgment. The historical same-source,
nonvirtual capability host and the exact pinned compiler/runtime manifest remain
P5O2B work. A6's 210-type/430-member signature surface and compatibility risks
are unchanged, not claimed tested against historical binaries. No ordinary
Main/Core runtime reference, historical package/TFM experiment or IPC was added.

## Tests, build and all quality gates

| Fresh validation | Passed / total | Failed / skipped |
| --- | ---: | ---: |
| New focused ownership/body tests | 16 / 16 | 0 / 0 |
| Summary / Session / Builder regression | 302 / 302 | 0 / 0 |
| Local / Callback / SummaryGraph | 292 / 292 | 0 / 0 |
| All DependencyTests, including existing fact/evaluator guards | 33 / 33 | 0 / 0 |
| Broad Check.Semantic / Execution.Semantic / Evaluation (P5/P6/G) | 1,784 / 1,784 | 0 / 0 |
| Full suite | 2,576 / 2,576 | 0 / 0 |
| Full suite after final CRLF/build | 2,576 / 2,576 | 0 / 0 |

Four new architecture tests include fields/signatures/local storage and executable
IL, resolving constructor, method, delegate/`ldftn`, field and type tokens;
compiler-generated nested closures fold to their actual owner. They protect
Builder's entire transitive closure, Analyzer's absence of Builder/Session queries,
all five upper components against cycles and all 27 existing lower owners.
Existing semantic suites cover Session creation, recursive/transitive graphs,
constructors/accessors/operators, calls, local/anonymous callables, target identity,
context and fail-closed dispatch. Twelve new predicate cases pin the narrower
method/local-function shape boundary, bodyless/metadata rejection and rejection
of equal source text belonging to a foreign tree/Compilation. No redundant
algorithm-level expansion was added.

One initial new-test failure was a test inventory issue (26 root-namespace owners
instead of including the existing canonical resolver); the namespace guard was
corrected to match the audit's 27-owner scope. Audit tooling also needed fixes
for unqualified local-function IDs and Windows PowerShell array/newline handling.
These were verification-tool issues; no production algorithm was changed in
response. Only completed passing TRX files count as final evidence. No known
MultiModule flake occurred in either full run, and no flake repair was attempted.

Warning-as-error builds: Main, neutral Core, Tests, Evaluation and audit tool
all **0 warnings / 0 errors**. To honor protected Core `bin/obj`, Core was built
with isolated `IntermediateOutputPath`/`OutputPath` under
`artifacts/p5o2a6f/core-build`; dependent solution projects used
`BuildProjectReferences=false`. This covers every solution project but is not
misreported as an ordinary solution-wide rebuild. Existing Core output/reference
bytes used by consumers remain exactly unchanged; current Core sources also
compile independently in the isolated location.

Fresh solution-transitive Self Analysis: **16 findings**, DOC610=0, DOC611=1,
DOC631=15, DOC632=0; CLI exit 1 is the expected finding status. Against raw and
committed normalized Post-A5C evidence: **0 added / 0 removed / 0 evidence changes**.
Full raw and normalized arrays are exactly equal, retaining every property,
including paths, line/column, message/snippet and evidence. Only workspace prefix/
path separators and deterministic finding order are normalized. Both arrays and
their equal SHA256 `2335A309D71F94CC0D3DD7546B57E2D14A107806CD666DD4FEBED5F04AB5F8DF`
are preserved in the JSON.

Raw bound-audit `SourceHashes` use UTF8 decoded source text, excluding an
original encoding BOM. The final JSON additionally records separate file-byte
SHA256 values for all 361 production files. The 29 existing BOM-bearing namespace
documentation files are untouched; their byte/text hashes are deliberately not
conflated. All current source-text hashes match the final measured compilation.

Scoped production/test/audit formatting verifies with no changes. Known global
format deviations remain untouched. Final JSON parse/schema/canonical gates,
scoped CRLF gates and `git diff --check` pass. The protected root `.gitignore`,
seven tracked Core outputs and all six stash identities/bytes match the captured
pre-production fingerprints. No commit, push, stash operation or reset.

## Evidence and reproduction

- [Machine-readable closure audit](P5O2A6F-summary-orchestration-cycle-closure-audit.json)
- [Current historical source/category/dependency matrix](P5O2A6F-historical-core-readiness-matrix.md)
- [Current pipeline boundaries](OPEN-PIPELINE-BOUNDARIES.md)
- Raw bound graph/verification: `artifacts/p5o2a6f/before.json`, `after-verified.json`, `after-repeat.json`.
- Fresh TRX evidence and Self Analysis: `artifacts/p5o2a6f/tests/`, `artifacts/p5o2a6f/self-analysis.json`.

```powershell
dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a6f/after-verified.json --summary-closure=87f07fc5801bd2dd3948a475e7fa05dddd10a936
dotnet test XMLDocNormalizer.sln --no-build --no-restore --logger 'trx;LogFileName=full-final.trx' --results-directory artifacts/p5o2a6f/tests
dotnet src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll --check --project XMLDocNormalizer --exception-analysis-mode solution-transitive --format json --output artifacts/p5o2a6f/self-analysis.json XMLDocNormalizer.sln
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/ContextualBoundaryAudit/Write-SummaryClosureEvidence.ps1
```

**P5O2A6 now READY; remaining minimal architecture-closure blocker: none.**
P5O2B may begin as the next separately authorized step. P5O2B itself was not begun.

## Final git status --short

The root ignore file and seven Core output paths below are protected pre-existing
WIP; their bytes are exactly equal before/after. All other entries are A6F work.

```text
 M ../../.gitignore
 M Evaluation/ContextualBoundaryAudit/Program.cs
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Evaluation/P5O2A6-historical-core-readiness-matrix.md
 M Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExternalSupportingSourceAcquisitionTests.cs
 M Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExternalSupportingSourceBinaryDiscoveryTests.cs
 M Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalSupportingSourceReconstructionTests.cs
 M Tests/XMLDocNormalizerTests/Helpers/ExceptionFlowAnalyzerTestHelper.cs
 M Tests/XMLDocNormalizerTests/Helpers/ExceptionFlowSummaryGraphProjectTestHelper.cs
 M Tests/XMLDocNormalizerTests/Helpers/ExceptionFlowSummaryGraphTestHelper.cs
 M Tests/XMLDocNormalizerTests/Helpers/ExternalSupportingSourceExceptionFlowTestHelper.cs
 M src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfo.cs
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfoInputs.cache
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.sourcelink.json
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/ref/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/refint/XMLDocNormalizer.ExceptionFlow.Core.dll
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphDispatch.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphEvaluation.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryAnalysisSession.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphBuilder.cs
 M src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryTargetRegistrar.cs
 M src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs
 M src/XMLDocNormalizer/Execution/ToolRunner.cs
?? Evaluation/ContextualBoundaryAudit/SummaryClosureVerification.cs
?? Evaluation/ContextualBoundaryAudit/Write-SummaryClosureEvidence.ps1
?? Evaluation/P5O2A6F-historical-core-readiness-matrix.md
?? Evaluation/P5O2A6F-summary-orchestration-cycle-closure-audit.json
?? Evaluation/P5O2A6F-summary-orchestration-cycle-closure.md
?? Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowSummaryBodyResolutionTests.cs
?? Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowSummaryOrchestrationDependencyTests.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphDeclarations.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryAnalysisSession.GraphConstruction.cs
?? src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryTargetRegistrar.BodyResolution.cs
```
