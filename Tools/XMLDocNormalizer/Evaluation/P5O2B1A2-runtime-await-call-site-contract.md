# P5O2B1A2 - Missing RuntimeAwait Information / Fail-Closed Call-Site Contract

Date: 2026-10-08. Starting/final HEAD: `622388dcd2faa9af0ea1ecf61d52396ef1179a9c`
(`Prove runtime await compatibility boundary.`). B1A is separately committed.

## Decision: READY for P5O2B2

The local unavailable-information contract is proven and the minimal productive
same-source capability is implemented. Current behavior retains native symbol/null
values. Missing API information records existing flow uncertainty and returns
before choosing an unjustified normal awaiter route. No private compiler
reconstruction or new Analyzer architecture is needed.

The genuine historical boundary builds twice with **0 warnings / 0 errors**,
without expression projection, source copies or omissions: all original **120**
productive files plus **one** new capability file. The existing compile-only
semantic host is the 122nd explicit Compile item, not productive Analyzer logic.
Current builds and full regressions pass; Self Analysis remains 16 and the complete
raw/normalized finding/evidence arrays match A6F exactly: canonical **0/0/0**.

B2 has **not** begun. This is compile compatibility and a local fail-closed
contract proof, not execution or behavioral equivalence of a historical Analyzer.
No worker, IPC, AssemblyLoadContext, serialization or transport was implemented.
No Dapper/external reconstruction stage advancement is claimed.

Evidence: `P5O2B1A2-runtime-await-call-site-contract-audit.json`,
`P5O2B1A2Proof/`, ignored `artifacts/p5o2b1a2/` logs/TRX/source-surface proof.

## Evidence-first audit and historical semantic basis

Before edits, read the B1/B1A reports, complete JSON/API matrix and boundary
register. Required git status/diff/HEAD audit showed only the eight protected
foreign paths. All 361 original Main byte fingerprints matched B1; the six stash
hashes/descriptions and all eight protected hashes matched. Exactly four native
RuntimeAwaitMethod expressions remained, at the original locations below.

The original exact package provenance remains authoritative:
`5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0`.
The new isolated restore's net8.0 Common/CSharp binaries match B1/P5N hashes and
MVIDs, not merely the shared AssemblyVersion 5.0.0.0. Eight retained historical
compiler C# sources were rehashed and their PDB document checksums reverified by
reproducing checkout CRLF in memory while preserving UTF8 BOMs. Raw Git source
hashes are not conflated with PDB checkout hashes. B1A's decoded IL remains valid
because the exact historical PE bytes are unchanged; the selected method evidence
and binary assertions are retained in A2's JSON. No historical DLL was executed.

Historical Binder.GetAwaitableExpressionInfo tests runtime support, containing
method/async return shape and runtime-async setting. It can select a direct
AsyncHelpers.Await call instead of normal pattern binding; alternatively it can
bind the complete GetAwaiter/IsCompleted/GetResult pattern and additionally choose
AwaitAwaiter or UnsafeAwaitAwaiter. BoundAwaitableInfo retains RuntimeAsyncAwaitCall,
but MemberSemanticModel constructs the public AwaitExpressionInfo with only four
fields, omitting that helper. The public operation/symbol routes do not recover it.

Therefore complete pattern information cannot prove that no runtime helper runs.
The Analyzer host's net8.0 TFM also cannot constrain all analyzed target runtimes.
Unavailable is not known-null, even for an apparently normal, complete await.
Exact reconstruction would need more compiler-internal information; it is neither
necessary for this conservative contract nor implemented.

Verified primary source identifiers are retained in B1A, including:

- `dotnet/dotnet` commit `2db1f5ee2bdda2e8d873769325fabede32e420e0`,
  `src/roslyn/src/Compilers/CSharp/Portable/Binder/Binder_Await.cs`.
- The same commit's `Compilation/MemberSemanticModel.cs`,
  `Compilation/AwaitExpressionInfo.cs`, `Compilation/CSharpCompilation.cs`,
  semantic model and operation/lowering sources.
- Current `dotnet/roslyn` commit `6c4a46a31302167b425d5e0a31ea83c9a9aa1d09`,
  `src/Compilers/CSharp/Portable/Compilation/AwaitExpressionInfo.cs`.

## Four individual access contracts

Paths below share `src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/`.

| Original occurrence | Role | Concrete native symbol | Known native null | Unavailable information |
| --- | --- | --- | --- | --- |
| SummaryGraphImplicitCalls.cs 218,27 | AddSummaryExplicitAwaitEdges route predicate | Choose runtime-helper route, never normal chain | Test normal-pattern completeness, then collect selected pattern edges | Record uncertainty and return before this predicate |
| SummaryGraphImplicitCalls.cs 221,31 | Same method's edge argument | Register that exact helper/context and RuntimeAwaitCall edge; return | This argument is not evaluated; normal GetAwaiter/get_IsCompleted/GetResult edges apply | Argument is not evaluated; no invented helper or pattern edge |
| SummaryGraphImplicitDispatch.cs 275,27 | AddSummaryExplicitAwaitDispatchEdges route predicate | Choose helper before receiver/dispatch expansion | Resolve normal awaitable/awaiter receiver types and runtime dispatch targets, retaining completeness checks | Record uncertainty and return before dispatch/normal-pattern resolution |
| SummaryGraphImplicitDispatch.cs 278,31 | Same method's edge argument | Register exact helper/context with RuntimeAwaitCall; return | Not evaluated; selected/virtual/interface pattern targets and dispatch uncertainty apply | Not evaluated; missing helper is represented by unknown flow, not silently erased |

Both methods first retain the existing IsDynamic uncertainty/return branch.
They next check availability, then retain the previous non-null and normal-null
branches, path kinds, registration, receiver/context calculation and return order.
Exactly four native expressions are replaced by Information.Method reads; there
are two new unavailable guards. One existing orchestration call acquires the
information explicitly and passes the immutable value alongside AwaitExpressionInfo.

Important active-path qualification: AnalyzeSummaryAwaitOperations currently
calls the **dispatch** body. The non-dispatch body has no current production
caller; it is retained rather than cleaned up and its contract is tested directly.
There are four original source accesses in two bodies, not four active pipeline
entry points. No implicit-await/foreach/disposal API or algorithm was adapted.

## Complete propagation and final exception-flow decision

Native symbol route: AddSummaryImplicitMethodEdge normalizes ReducedFrom, builds
the existing implicit-call context, registers the exact method target and adds
RuntimeAwaitCall. Registration/source-body handling remains unchanged: analyzable
targets get their real summaries, unavailable target bodies retain normal
uncertainty/documentation evidence. Graph evaluation prepends the runtime-helper
path step and preserves exception kind/identity and caller-side catch filters.

Native null route: the previous pattern-completeness test still records uncertainty
if any required member is unavailable. Selected normal calls or dispatch-expanded
targets are registered exactly as before; none is replaced by a helper edge.
Current native availability never sets the new missing-information uncertainty.

Unavailable route: AddUncertainTarget records an unresolved operation in the local
fragment, followed by return. We do not speculate that normal calls execute: the
historical helper may replace them, and assuming that route could both miss helper
exceptions and falsely prove pattern exceptions. Unknown flow represents the
unidentified executable helper without claiming an exhaustive exception set.

The full existing propagation chain is:

`AnalyzeSummaryAwaitOperations -> explicit-await body -> fragment uncertainty ->
AnalyzeSummaryNode / try-fragment merge -> Session.BuildPendingSummaryNodes ->
Summary.Merge -> GraphEvaluator.EnterSummaryEvaluationFrame ->
AnalysisResult.UncertainTargets -> MergeWithPrefixExcluding / caller union ->
HasUncertainPaths -> final detector decisions`.

The evaluator unions local uncertainty before executable-body evaluation;
transitive result merges preserve it even with typed exception filters. Thus an
executable summary is **not** treated as complete merely because it has a body or
no selected edges. The final AddDocumentedExceptionWithoutTransitiveThrowFindings
returns on HasUncertainPaths, suppressing optimistic DOC632. The complementary
AddExceptionFlowNotDecidableFindings emits DOC631 for relevant documented types
not covered by independently proven throws. Existing positive exception paths,
including DOC610/DOC611 evidence from other sources, are not deleted or fabricated.

Typed catches keep uncertainty; they cannot prove all unidentified exceptions are
handled. The unchanged SuppressCaughtSummaryFlow may call SuppressAll only through
its existing unfiltered/suppress-original/catch-all checks. A genuine catch-all can
remove protected uncertainty because no such exception escapes. Rethrow and filter
guards remain unchanged; catch/finally bodies are separately analyzed as before.
No new suppression or special exception type was introduced.

## Bounded productive owner and Current precision

`ExceptionFlowRuntimeAwaitCapability` depends only on public AwaitExpressionInfo,
IMethodSymbol and one narrowly validated public-property lookup. A cached open
ref-receiver delegate invokes the actual getter, avoiding per-read reflection,
boxing, four separate implementations or a version-number heuristic. CLR static
initialization publishes it thread-safely. Unexpected present API shape rejects
explicitly; absence returns unavailable. Native getter behavior is not wrapped or
reconstructed. No private Roslyn reflection, analyzer hooks or compiler substitution.

Information is readonly, per-call data:

- IsAvailable=true, Method=symbol: exact native helper reference.
- IsAvailable=true, Method=null: genuine known-null, normal route retained.
- Default/IsAvailable=false: unavailable, irrespective of Method being null.

IsAvailable is a readonly flag, not a computed getter; Method is readonly native
value storage. The sole cached object is the stateless delegate, not a result,
Compilation, SemanticModel, graph or symbol. Existing weak caches/guards are untouched.

Both existing private algorithm bodies take this explicit Information value.
Tests supply it per invocation and reflect only those **own** private entry points,
not private compiler semantics. There is no global mutable availability switch,
test-only productive overload or historical runtime loaded in the product process.
Current native null precision is not conservatively degraded by the historical
fallback. Historical missing capability deliberately marks all affected static
explicit awaits unknown rather than inferring feature absence from target/host TFM.

Current native tests cover Task/Task<T>, ValueTask/ValueTask<T>, custom and extension
awaiters, dynamic, incomplete binding and the default snapshot. Two current-only
structural non-null cases use the real current internal constructor in **test code**
to exercise native storage (helper-only and helper+complete pattern), including
1,000 concurrent reads. They are not claimed as real runtime-async binder fixtures;
the installed net8.0 fixture corelib does not provide RuntimeAsyncMethods. B1A's
native accessor/concurrency/allocation proof is retained; A2 reruns identity and
concurrency on the actual productive owner. No historical runtime test is claimed.

Six direct tests run both real bodies with all three states on a complete pattern,
through actual source-built target summaries, a transitive caller, evaluator and
the real final DOC631/DOC632 methods. Four catch-transfer cases and two independent
proven-source cases pin preservation/suppression behavior. Missing and known-null
produce demonstrably different final decisions: DOC631 versus DOC632.

## Validation results and verification issues

| Fresh gate | Result |
| --- | --- |
| New capability/call-site tests | 24/24, no failures/skips |
| Await/Async/Summary/DOC631/DOC632 regression | 420/420, no failures/skips |
| Full suite and final full suite | 2600/2600 each, no failures/skips (2576 baseline + 24) |
| Main, Core, Evaluation, Tests WAE builds | 0 warnings / 0 errors each |
| Current metadata/source proof WAE build | 0/0 |
| Real historical source build / forced repeat | 0/0 each, deterministic images identical |
| Used API recheck | 430 original contracts: 429 equal, one known absent getter; 210 types, 84 equal used enum constants; zero additional drift |
| Self Analysis | 16; DOC610=0, DOC611=1, DOC631=15, DOC632=0; CLI exit 1 expected |
| A6F Canonical Finding Diff | 0 added / 0 removed / 0 evidence changes; complete raw and normalized arrays equal |
| Scoped format, UTF8/CRLF, JSON/XML/PS parse, diff-check | PASS |
| Protected foreign file/stash identities | Eight hashes and six stashes unchanged |

All four solution projects were covered. To preserve protected Core bin/obj, Core
used isolated IntermediateOutputPath/OutputPath under artifacts/p5o2b1a2/core-build;
dependents used BuildProjectReferences=false. This is not described as an ordinary
solution-wide rebuild. Normal current package/project files are unchanged.

The historical Compile items link the real WIP sources. MSBuild evaluates all 120
original items, the same original throwing compile-only host, and the new capability.
Strict Common/CSharp pins retain the full preview suffix, native net8.0 assets and
independent assets/bin/obj/package paths. There are no Main/Core ProjectReferences.
No source rewrite, fork, #if, projected syntax tree or experimental accessor is
substituted. The installed SDK 8.0.418/csc 4.11.0 drives these reference-targeted
builds; the historical Toolset is not executed. Image hashes and input hashes are
recorded in the JSON. Successful compile is not historical runtime equivalence.

Initial verification issues are retained, not counted as passing gates: three test
inventory name errors and two constructor argument-order failures were corrected
against actual current contracts. An initial Self Analysis had two DOC360 param-doc
order findings; correcting docs restored 16. A subsequent full finding comparison
exposed one existing DOC631's uncertainty-count delta (1622 versus 1621 more targets).
A read-only HEAD/WIP current-source diagnostic identified two new carrier auto-getters
versus the removed native getter. Representing availability directly as a readonly
flag removed the unnecessary getter boundary; the final complete finding/evidence
arrays match exactly. The diagnostic's platform-reference/overload selection fixes
were tool-only. No Analyzer uncertainty policy, finding message, baseline or
normalization rule was altered to hide a delta. No independent flake was repaired.

The final protected-state gate also detected design-time generation of two Core
obj files during current-project evaluation: AssemblyInfo.cs (revision/culture
comment) and AssemblyInfoInputs.cache. Core DLL/ref bytes and the other protected
files did not change. The original AssemblyInfo text was resolved by its captured
SHA256; the exact original cache content was recovered from the retained A6F
isolated artifact with the matching SHA256. Only these task-induced edits were
reversed with apply_patch, not Git restore/reset. All eight final protected hashes
equal their initial values; no pre-existing WIP was discarded. This transient
churn is explicitly reported rather than described as never occurring.

## Reproduction

From Tools/XMLDocNormalizer, with retained validated P5N archives and current package
cache available (on a clean machine reacquire exact recorded URLs and verify hashes):

```powershell
dotnet restore Evaluation/P5O2B1A2Proof/HistoricalContractCompile.csproj --source artifacts/p5n-acquisition/packages --source C:/Users/Krause/.nuget/packages
dotnet build Evaluation/P5O2B1A2Proof/HistoricalContractCompile.csproj --no-restore -warnaserror --no-incremental
dotnet build Evaluation/P5O2B1A2Proof/HistoricalContractCompile.csproj --no-restore -warnaserror --no-incremental
dotnet restore Evaluation/P5O2B1A2Proof/CurrentSurfaceProof.csproj --source C:/Users/Krause/.nuget/packages
dotnet build Evaluation/P5O2B1A2Proof/CurrentSurfaceProof.csproj --no-restore -warnaserror
dotnet run --project Evaluation/P5O2B1A2Proof/CurrentSurfaceProof.csproj --no-build -- D:/Repository/SEE/Tools/XMLDocNormalizer
dotnet build src/XMLDocNormalizer/XMLDocNormalizer.csproj --no-restore -warnaserror
dotnet build src/XMLDocNormalizer.ExceptionFlow.Core/XMLDocNormalizer.ExceptionFlow.Core.csproj --no-restore -warnaserror -p:IntermediateOutputPath=D:/Repository/SEE/Tools/XMLDocNormalizer/artifacts/p5o2b1a2/core-build/obj/ -p:OutputPath=D:/Repository/SEE/Tools/XMLDocNormalizer/artifacts/p5o2b1a2/core-build/bin/
dotnet build Evaluation/XMLDocNormalizer.Evaluation/XMLDocNormalizer.Evaluation.csproj --no-restore -warnaserror -p:BuildProjectReferences=false
dotnet build Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-restore -warnaserror -p:BuildProjectReferences=false
dotnet test XMLDocNormalizer.sln --no-build --no-restore --logger 'trx;LogFileName=full-final.trx' --results-directory artifacts/p5o2b1a2/tests
dotnet src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll --check --project XMLDocNormalizer --exception-analysis-mode solution-transitive --format json --output artifacts/p5o2b1a2/self-analysis-final.json XMLDocNormalizer.sln
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B1A2Proof/Write-Verify-ContractEvidence.ps1
```

The evidence verifier expects the logged builds/TRX plus historical-first-image.json,
captured from Get-FileHash between the two forced builds. It does not invent test
results or rerun builds. The optional SurfaceProof `--self-uncertainty` diagnostic
compares HEAD/WIP source in one current compilation and reflects only our current
Analyzer entry; it never loads historical runtime code. ExecutionPolicy override is
process-local. Formatting is scoped to the four productive files, one new test and
the new evidence program, with no normalization of protected files. Design-time
workspace evaluation can regenerate Core AssemblyInfo even with build-output
isolation; snapshot and compare protected bytes around such operations. The final
verifier rejects any remaining difference.

## Exact B2 project/package recommendation - not implemented

Use a dedicated source-linked `XMLDocNormalizer.ExceptionFlow.Historical` build
owner, with a distinct Analyzer AssemblyName. Keep Main's current source-local
implementation and stable 5.0.0 package graph unchanged. An explicit shared source
item definition must retain all original 120 files plus the one shared capability;
do not reference Main's compiled Roslyn-facing types or turn the neutral Core into
the algorithm owner. The semantic host is compile-local/nonvirtual; the five
throwing probe members are not a final executable host.

TargetFramework remains net8.0. Direct exact PackageReferences:

| Package | B2 requirement |
| --- | --- |
| Microsoft.CodeAnalysis.Common | [5.0.0-2.25451.107] |
| Microsoft.CodeAnalysis.CSharp | [5.0.0-2.25451.107] |
| Microsoft.CodeAnalysis.Analyzers | [3.11.0], PrivateAssets=all |
| System.Collections.Immutable | [9.0.0] |
| System.Reflection.Metadata | [9.0.0] |

No CodeAnalysis umbrella, Workspace/MSBuild package, historical Toolset driver,
TFM switch or ordinary Main/Core runtime ProjectReference is required by this
proven algorithm boundary. Configure BaseIntermediateOutputPath and
MSBuildProjectExtensionsPath before SDK imports, plus distinct BaseOutputPath and
restore graph. For example, future historical-owner obj/bin beneath its own
artifacts/exception-flow-historical/<project>/ directory; never Main/Core's existing
obj/assets or current runtime dependency copies. Independent restore/build/test
owners should validate pins and all shared Compile items. This recommendation is
not a created B2 scaffold or a choice of historical runtime isolation implementation.
The same public Roslyn identities still require a later isolated execution design.

Minimal remaining local A2 blocker: **none**. Proceed directly to separately
authorized P5O2B2; no further analysis intermediary is proposed.

## Protected state and handoff

No commit, push, index changes, reset/checkout/restore, stash application or mutation.
Root .gitignore and all seven protected Core bin/obj hashes match the initial audit.
All six stashes, including both explicitly protected provenance stashes, are intact.
Existing ignore rules are unchanged. Both new proof projects are ignored by the
existing root .gitignore:41 `*.csproj` rule; they exist, were restored/built and have
recorded inputs. A later authorized commit must explicitly include them.

Final `git status --short` is recorded in the audit JSON and final handoff. Entries
for .gitignore/Core are protected pre-existing WIP; the three changed Analyzer
files, new capability/test, A2 evidence/proof files and boundary register are A2 work.
