# P5O2B1A - Bounded RuntimeAwait Capability and Same-Source Compatibility Proof

Date: 2026-10-08. HEAD: `d3140d7aa444809fa7dad7ad8ec4fa5b228040b3` (`Audit historical Roslyn build feasibility.`).

## Decision: NOT READY for P5O2B2

B1A is complete, without productive Analyzer changes. The compile blocker is bridgeable, but **a historical method-or-null fallback is not semantically safe**. The historical compiler already supports runtime async and can bind an AsyncHelpers call while reporting a complete normal awaiter pattern. Its public AwaitExpressionInfo drops that helper target. Returning null would then bypass the RuntimeAwait branch without triggering the existing incomplete-pattern uncertainty.

The isolated light-up experiment therefore rejects absent API access with NotSupportedException; it does not implement an unsafe null fallback. This is a fail-fast compile experiment, not a productive historical analysis fallback or an integration proof that the pipeline handles that exception. Successful projected compilation is explicitly not historical semantic readiness.

Smallest next package: **P5O2B1A2 - Missing RuntimeAwait Information / Fail-Closed Call-Site Contract Proof**. Prove a bounded NativeValue-versus-Unavailable result and two uncertainty guards, or establish exact historical helper reconstruction. Do not begin B2 before that semantic gate passes. No productive helper, call-site change, final historical project, worker, IPC or historical runtime execution was introduced.

Evidence: `P5O2B1A-runtime-await-compatibility-proof-audit.json`, `P5O2B1AProof/`, and ignored `artifacts/p5o2b1a/` build logs, proof results, and exact compiler source.

## Starting-state and complete B1 recheck

B1 was separately committed, including its previously ignored project files. Read its report, complete JSON/matrix data, all probe files and the boundary register. Verified all 361 productive source byte fingerprints, the 120-file manifest, active project hashes and all eight protected foreign file hashes against B1/A6F; all agree. All six stash hashes/descriptions agree. Initial and final HEAD are unchanged.

The original unchanged B1 historical build was rerun: 0 warnings / exactly four CS1061 errors, exclusively RuntimeAwaitMethod, with a fresh complete log. The B1A metadata pass checks all 430 used member identities, all 210 used types and all 84 used enum constants against both exact package graphs: the same one missing getter, no additional used API drift. B1's inspected 429 matching member contracts remain authoritative because both reference binaries and productive source are unchanged; the full projected historical compile also checks every productive API expression for additional compiler errors.

## Four productive accesses and their decisions

All paths are under `src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/`.

| File / line,column | Method | Purpose and dependent decision |
| --- | --- | --- |
| ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs / 218,27 | AddSummaryExplicitAwaitEdges | Tests whether the native runtime helper replaces the normal awaiter-chain route |
| ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs / 221,31 | AddSummaryExplicitAwaitEdges | Passes that exact helper symbol to AddSummaryImplicitMethodEdge with RuntimeAwaitCall, then returns |
| ExceptionFlowAnalyzer.SummaryGraphImplicitDispatch.cs / 275,27 | AddSummaryExplicitAwaitDispatchEdges | Selects the runtime-helper route before ordinary receiver/awaiter dispatch analysis |
| ExceptionFlowAnalyzer.SummaryGraphImplicitDispatch.cs / 278,31 | AddSummaryExplicitAwaitDispatchEdges | Adds the exact runtime helper as an implicit method edge with RuntimeAwaitCall, then returns |

Both complete methods were inspected, not just the four tokens. IsDynamic precedes them and already records uncertainty and returns. A non-null runtime method is **not merely an extra optional target**: its branch adds the helper edge and exits, replacing the ordinary chain in this path. A native null continues to GetAwaiter/IsCompleted/GetResult. An incomplete pattern records uncertainty; otherwise the method adds the three normal targets. The dispatch variant additionally resolves awaitable receiver types and dispatch targets for those normal calls. Returning a fake null can therefore change graph edges, exception targets and uncertainty, not merely remove an optimization.

Existing fallback safety differs by case:

- Dynamic binding: existing uncertainty is preserved; the property is not read.
- Runtime AsyncHelpers.Await: the three normal fields can be null, so pattern uncertainty remains conservative, but the exact helper target is still unavailable.
- Runtime AwaitAwaiter/UnsafeAwaitAwaiter: normal pattern fields can all be non-null. Existing complete-pattern logic does not establish that the runtime helper is absent and does not add missing-capability uncertainty. This is the blocking case.
- Ordinary non-runtime await: a native null is meaningful; an absent API is not equivalent to that value.

## Actual API and compiler semantics

| Surface | Current package 5.0.0 | Historical package 5.0.0-2.25451.107 |
| --- | --- | --- |
| AwaitExpressionInfo | readonly struct | readonly struct |
| RuntimeAwaitMethod | Public instance IMethodSymbol? getter | Absent; no alternate property on this struct |
| GetAwaiterMethod / GetResultMethod | IMethodSymbol? | Same public types |
| IsCompletedProperty | IPropertySymbol? | Same public type |
| IsDynamic | bool | Same public type |
| Runtime-async binding | Exists | Also exists; API absence does not imply feature absence |
| BoundAwaitableInfo.RuntimeAsyncAwaitCall | Compiler-internal bound information | Compiler-internal bound information exists; public getter on an **internal type**, not an accessible substitute API |

Current documentation describes two runtime-helper families: direct AsyncHelpers.Await can leave all three ordinary members null; AwaitAwaiter/UnsafeAwaitAwaiter can coexist with them. The native current struct stores the helper separately, and the proof accessor returns that exact symbol reference. [Official API documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.awaitexpressioninfo.runtimeawaitmethod?view=roslyn-dotnet-5.0.0).

Historical evidence is stronger than a public-member absence check:

1. Exact restored historical Common/CSharp DLL hashes match B1/P5N. Only these files are used as historical metadata references.
2. Portable PDB SourceLink points to dotnet/dotnet commit `2db1f5ee2bdda2e8d873769325fabede32e420e0`.
3. Eight relevant compiler source documents match the actual PDB document checksums after reproducing the build checkout's CRLF representation and preserving each UTF8 BOM. Raw downloaded hashes, expected document checksums, normalized checksums and normalization flags are retained. No productive files were normalized or copied.
4. Independently decoded IL from the validated historical DLL confirms the critical binder calls, internal runtime-async feature check, four-field public info construction and operation-factory shape. Method hashes, dependency tokens and complete selected instruction streams are retained. No historical assembly was runtime-loaded.

The historical binder checks runtime support, async method return type, per-method runtime-async setting and feature setting. It first tries the direct helper; otherwise it binds GetAwaiter/IsCompleted/GetResult and, when enabled, selects AwaitAwaiter/UnsafeAwaitAwaiter. The hidden BoundCall is stored in RuntimeAsyncAwaitCall. The public info factory constructs only four values and omits that call. IAwaitOperation also contains the awaited operand and result type, not the hidden helper target. The historical GetSymbolInfo await case only handles the dynamic flag and delegates to the bound expression's ordinary symbol route; it does not select RuntimeAsyncAwaitCall. These public alternatives were inspected rather than presumed equivalent.

The net8.0 **Analyzer host TFM** does not restrict the analyzed compilation to a runtime lacking this feature. The safe-null conclusion cannot be inferred from the probe's TFM or from the current fixtures' net8.0 reference set; other target references and per-method/feature settings can enable the historical binder route.

Primary compiler references, verified against PDB/binary evidence:

- [Historical AwaitExpressionInfo](https://github.com/dotnet/dotnet/blob/2db1f5ee2bdda2e8d873769325fabede32e420e0/src/roslyn/src/Compilers/CSharp/Portable/Compilation/AwaitExpressionInfo.cs).
- [Historical binder](https://github.com/dotnet/dotnet/blob/2db1f5ee2bdda2e8d873769325fabede32e420e0/src/roslyn/src/Compilers/CSharp/Portable/Binder/Binder_Await.cs), especially the normal-pattern plus runtime-await-awaiter route.
- [Historical info factory](https://github.com/dotnet/dotnet/blob/2db1f5ee2bdda2e8d873769325fabede32e420e0/src/roslyn/src/Compilers/CSharp/Portable/Compilation/MemberSemanticModel.cs).
- [Historical operation factory](https://github.com/dotnet/dotnet/blob/2db1f5ee2bdda2e8d873769325fabede32e420e0/src/roslyn/src/Compilers/CSharp/Portable/Operations/CSharpOperationFactory.cs).
- [Current AwaitExpressionInfo](https://github.com/dotnet/roslyn/blob/6c4a46a31302167b425d5e0a31ea83c9a9aa1d09/src/Compilers/CSharp/Portable/Compilation/AwaitExpressionInfo.cs).

There is no exact inverse from the historical four-field snapshot alone: ordinary and runtime-awaiter cases may expose the same pattern symbols. Exact reconstruction would need the bound node and method/runtime context, not just AwaitExpressionInfo. The identified internal route is MemberSemanticModel.GetLowerBoundNode(await syntax) -> BoundAwaitExpression.AwaitableInfo.RuntimeAsyncAwaitCall.Method -> public symbol conversion. Accessing that route would require further compiler-internal APIs, SemanticModel/syntax inputs and new compatibility/provenance proof; it was not implemented as a single-property light-up extension.

## Bounded light-up experiment

`RuntimeAwaitCapability.cs` is the **identical physical file** compiled by CurrentCapabilityProof and HistoricalCapabilityProof. It has no native RuntimeAwaitMethod member reference, version conditional or copied Analyzer source. It looks up exactly one known property on its actual AwaitExpressionInfo type, verifies public instance getter/return type/no index parameters, and creates an open typed delegate with a ref struct receiver.

Current: native getter value, including native null, is returned unchanged. Historical: cached accessor absence causes explicit unsupported rejection. No version-number heuristic, substituted compiler, general reflection framework or internal historical reflection was added.

Performance/lifetime:

- One property lookup and accessor creation at CLR static initialization; thread-safe readonly publication.
- One open typed delegate call afterwards; no PropertyInfo.GetValue or per-call lookup.
- AwaitExpressionInfo is passed by value to the boundary and by ref to the native getter; no struct boxing in the accessor. Warmed 100,000 calls measured 0 allocated bytes; 10,000 concurrent calls preserved reference identity.
- One static delegate field; no Compilation, SemanticModel, symbol result, graph or Analyzer state cached. This does not alter existing weak-cache or guard lifetimes.
- Native getter exceptions propagate directly through the delegate, without reflection invocation wrapping. Initialization/unsupported-shape failures remain explicit. The current-only missing-accessor test uses reflection to call the private proof seam; its TargetInvocationException is a test artifact, not accessor behavior.
- Normal net8.0 builds are untrimmed; no trimming/AOT configuration is introduced or tested. Any later trimmed/AOT deployment must verify property preservation and delegate creation rather than infer support from this proof.
- typeof(AwaitExpressionInfo) binds to the actual compiler universe. Equal public AssemblyVersion 5.0.0.0 cannot select or isolate versions; only current Roslyn executes here. No AssemblyLoadContext or runtime-loader design was built.

## Strategy comparison

| Strategy | Same source / production scope | Semantic safety / drift / maintenance |
| --- | --- | --- |
| A. Bounded light-up | One helper, four property-use replacements; possibly two availability guards after separate proof | Preferred narrow accessor mechanism for native values. Missing information requires an explicit state, not null. One cached delegate, no duplicated algorithms. |
| B. Common denominator, delete property branch | Four removals/rewrites | Rejected: loses genuine current helpers and historical hidden helpers. Existing pattern completeness is not a safe replacement. |
| C. Compile-time shim | One source-local helper with build-owned implementation | Viable only with the same unavailable-state contract. Current-native/historical-null is unsafe. Package/config drift must be kept explicit. |
| D. #if at four sites | Four version branches | Adds no semantic information and duplicates policy; no evidence that cleaner strategies cannot work. Rejected. |
| E. Historical copy/fork | Duplicated Analyzer implementation | Does not recover hidden information by itself and violates source-sharing goal. Rejected. |

No strategy currently proves a safe method-or-null historical fallback. No unsafe fallback was implemented even experimentally.

## Current behavioral proof

Current-only assertions cover Task, Task<T>, ValueTask, custom awaiter, extension awaiter, dynamic await, incomplete binding and default struct. The helper returns the exact native symbol reference/value. Custom/extension normal pattern information is checked independently.

The test compilation reports RuntimeAsyncMethods capability unavailable for the installed net8.0 corelib. Thus no real non-null runtime-async binding fixture was generated under that target; fabricating a complete alternate corelib would be a separate fixture expansion. Two **structural**, explicitly current-only cases use the real current internal constructor: helper-only with null pattern fields, and helper plus complete pattern. Both preserve exact symbol identity. The latter is also the critical complete-pattern counterexample; it is not claimed as a historical runtime test or as real lowering reproduction. Missing accessor rejection, concurrency and allocation are separately checked.

## Same-source and 120-file compile proof

Both tiny capability projects independently restore and build against strictly pinned current 5.0.0 and historical 5.0.0-2.25451.107 packages. Both use the same full-path capability Compile item and its SHA256. Outputs/assets/packages are under `artifacts/p5o2b1a/`, with independent project names, no ProjectReferences and no solution membership. The historical assembly is compiled only, never loaded or run.

The current proof engine parses each of the exact 120 productive files and verifies its B1 byte hash. It rewrites only the four RuntimeAwaitMethod MemberAccess syntax nodes, **in memory**, to RuntimeAwaitCapability.GetMethod(awaitInfo). Original file paths and trivia are retained; no projected `.cs` files are emitted. The original B1 always-throwing host and generated global usings close the existing compile-only seam. The same capability source is included; no other algorithm or predicate is changed.

It emits the projected boundary against current references, then twice against exact historical references, treating compiler warnings as errors. Results: **0 compiler warnings/errors in all three emits**, identical deterministic historical image hashes within the repeated run, and no additional API failures. The in-memory assemblies are never loaded or executed. This uses the current Roslyn 5.0.0 compiler engine reading historical PE references, not historical compiler execution. SDK 8.0.418 drives the separate tiny project builds and normal Main build.

Crucial limitation: the historical projection uses an accessor that rejects unavailable information. This proves **compile completeness after bridging this single member reference**, not a viable historical Analyzer fallback. It does not justify silently changing the productive branch to null or claim B2 readiness.

## Reproducible commands

Run from Tools/XMLDocNormalizer; the offline feeds/cache and retained exact P5N/B1 evidence are prerequisites. Reacquire missing original package/source artifacts from their recorded exact URLs and verify hashes. No manual package/source swapping is required.

```powershell
dotnet restore Evaluation/P5O2B1AProof/CurrentCapabilityProof.csproj --source C:/Users/Krause/.nuget/packages --verbosity minimal
dotnet restore Evaluation/P5O2B1AProof/HistoricalCapabilityProof.csproj --source artifacts/p5n-acquisition/packages --source C:/Users/Krause/.nuget/packages --verbosity minimal
dotnet build Evaluation/P5O2B1AProof/CurrentCapabilityProof.csproj --no-restore -warnaserror --no-incremental --verbosity minimal
dotnet build Evaluation/P5O2B1AProof/HistoricalCapabilityProof.csproj --no-restore -warnaserror --no-incremental --verbosity minimal
dotnet build Evaluation/P5O2B1AProof/HistoricalCapabilityProof.csproj --no-restore -warnaserror --no-incremental --verbosity minimal
dotnet run --project Evaluation/P5O2B1AProof/CurrentCapabilityProof.csproj --no-build -- D:/Repository/SEE/Tools/XMLDocNormalizer
dotnet build src/XMLDocNormalizer/XMLDocNormalizer.csproj --no-restore -warnaserror --verbosity minimal
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B1AProof/Write-Verify-RuntimeAwaitEvidence.ps1
```

The current proof run performs all behavioral assertions and three projected emits; it writes proof-results.json. The final script verifies exact inputs, package graphs, repeated image equality, source/PDB and IL assertions, protected state, full scoped C# formatting, JSON/XML/PowerShell parsing, CRLF/UTF8/final-newline/whitespace, and git diff --check. It requires the recorded logs and compiler-source artifacts and does not itself rerun builds. Execution-policy override is process-local.

## Validation and semantics

Current Main warning-as-error build: **0 warnings / 0 errors**. Both same-source capability builds: **0 warnings / 0 errors**; historical build repeated. Projected Current/Historical/Historical-repeat: **0 warnings / 0 errors**, historical images equal. Current proof assertions: **19/19**, with exact names in the JSON. Full used API recheck has no additional drift. Eight compiler source/PDB checksums and selected binary IL assertions pass. Format, JSON, CRLF, UTF8, parse and git diff --check pass.

The original unprojected historical build still fails at the same four CS1061 sites; no productive compatibility fix was made. All 361 Main source byte hashes and committed B1 sources remain unchanged. No full suite, Self Analysis or canonical diff was unnecessarily rerun. Inherit the verified A6F semantic baseline: **2,576/2,576**, zero failures/skips; **16 findings** (DOC610=0, DOC611=1, DOC631=15, DOC632=0); canonical **0 added / 0 removed / 0 evidence changes**, exact full raw/normalized arrays retained.

## Minimal follow-up, not implemented

Potential productive owner: `ExceptionFlowRuntimeAwaitCapability`, below Analyzer, depending only on the two public Roslyn types and the narrow property accessor. It must not acquire Analyzer, SemanticModel, graph, context or cache ownership.

Four productive property occurrences would change in the two existing explicit-await methods. A method-or-null helper alone is rejected. The smallest conservative candidate is TryGetMethod returning **known-native-value versus unavailable**, including native null as a successful known value. Each of the two callers would explicitly record missing-capability uncertainty before normal pattern analysis; native current branches would remain unchanged. This adds two bounded availability guards, not a generic context or compatibility framework. Whether it satisfies the required fail-closed behavior must be proved in the next package, including incomplete, dynamic and complete custom awaiter paths and both Summary edge/dispatch decisions. It has not been substituted in this B1A projection or adopted as the final production design.

If exact target reconstruction is required instead, the identified compiler-internal bound-call route needs a separately scoped semantic/provenance proof; it cannot be reconstructed from this historical struct alone. Do not grow the single-property light-up into a private compiler reflection library in B1A.

B2 remains **NOT READY** because the required historical analysis fallback gate failed, despite complete compile proofs. The next package should prove the availability-aware fail-closed contract first; only then authorize the historical source-linked owner/scaffold. Future productive changes require the full regression suite, Self Analysis and canonical diff again.

All eight protected foreign files and six stashes remain byte-/identity-identical. No ignore edits, index changes, commit, push, stash mutation or reset. Both new B1A `.csproj` files are ignored by the existing root `*.csproj` rule but are present, restored and compiled; a later authorized commit must explicitly include them. No B1 evidence file or probe source was rewritten.
