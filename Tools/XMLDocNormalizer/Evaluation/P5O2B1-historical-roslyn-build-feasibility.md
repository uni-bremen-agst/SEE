# P5O2B1 - Historical Roslyn Build Feasibility and Exact Build Boundary

Date: 2026-10-08. Starting and final HEAD: `a4048c91899115190992ceb3c86da29ba34d0825` (`Close summary orchestration cycle.`).

## Decision: NOT READY for P5O2B2

B1 is complete. The exact historical package version and source boundary are established, restore succeeds, and the unchanged productive source produces exactly four reproducible CS1061 errors for one absent Roslyn API: `AwaitExpressionInfo.RuntimeAwaitMethod`. Do not start B2 yet.

Smallest sensible intermediate package: **P5O2B1A - Bounded RuntimeAwait Capability and Same-Source Compatibility Proof**. Prove the historical binder's await capabilities, then establish a small typed compile-time accessor seam for the four property uses in two methods. Preserve the current RuntimeAwait behavior and historical fail-closed behavior. A historical `null` result is a hypothesis requiring proof, not a fix authorized or implemented by B1. No broad compatibility layer, source fork, worker or IPC is needed to investigate this blocker.

Production sources, tests, active projects, solution and package versions are unchanged. The probe is experimental, source-linked and not part of the normal solution. No compatibility fix, B2 scaffold or historical runtime host was implemented.

Evidence:

- `P5O2B1-historical-roslyn-build-feasibility-audit.json`: exact package/archive/reference hashes, project and resolved dependency graphs, all source/owner dependencies, metadata contracts, compiler errors and protected-state checks.
- `P5O2B1-roslyn-api-drift-matrix.md`: all 430 member contracts and their caller/file sites, 210 types, 84 enum constants, and the exact 120 productive files with required purpose.
- `P5O2B1CompileProbe/`: explicitly isolated compile experiment and evidence tools, not a final historical owner.
- `artifacts/p5o2b1/`: ignored restore assets, full diagnostic logs/binlogs and metadata inspection output; their hashes are retained in the audit JSON.

## Readiness evidence and HEAD verification

Read both A6 and A6F reports, full audit data and matrices, and `OPEN-PIPELINE-BOUNDARIES.md` before the probe changes. The A6F matrix is authoritative: its upper Summary cycle closure supersedes A6's old NOT READY result.

All **361 current Main source byte fingerprints** match the committed A6F evidence. Active project/solution/test sources have no diff. MSBuild evaluates exactly the A6F 120-file manifest plus the one explicitly identified compile-only host. No file was copied, removed, substituted or added to the productive boundary for convenience.

Consequently the A6F architecture remains verified by unchanged source:

- 100 owners: 47 A algorithms, 14 B shared infrastructure, 39 D neutral owners; 120 physical files including the exact namespace-documentation files.
- Contextual Fact SCC: 63 methods / 112 internal edges; egress 791; two cache helpers / three fields unchanged.
- Evaluator and lower Fact/Resolver owners to Analyzer: zero; Summary Builder to Analyzer: zero; Analyzer to Builder/Session: zero.
- Upper Summary orchestration cycles: zero. Two unchanged neutral canonical raw type cycles and one neutral method-owner cycle remain; there is no claim that every raw graph is acyclic. The provider-internal DataFlow/cache cycle is unchanged.
- Current Main-only API uses inside the candidate: zero. The active semantic host remains an explicit cut, not silently pulled into the probe.

The full JSON contains per-file category, purpose, owners, source dependencies and Roslyn type usage, and all 100 owner dependency records. A files provide the actual algorithm owners; B files close their Roslyn-sensitive helper dependencies in the same compiler universe; D files close the required neutral canonical/value/path contracts without an active Main assembly reference. Namespace documentation is retained exactly as inventoried. Each file has zero direct ProjectReferences and no Main-only infrastructure beyond the explicitly cut host capability surface.

## Exact historical compiler and packages

The target is **`5.0.0-2.25451.107`**, including its preview suffix. It is not stable `5.0.0` and was not inferred from the shared public assembly version.

Provenance chain:

1. Retained P5N evidence records the required compiler informational version `5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0`.
2. `artifacts/p5n-diagnostic/Program.cs` records the full requested package version and the Microsoft dnceng public dotnet-tools feed.
3. `artifacts/p5n-analysis/p5n-report.json` records all three original package URLs, SHA256/SHA512, bytes, repository commit and validated library pairs.
4. The retained `.nupkg` archives and extracted nuspecs identify the exact version and `dotnet/dotnet` commit `2db1f5ee2bdda2e8d873769325fabede32e420e0`. B1 rehashed all three archives and the two newly restored net8.0 references against that evidence; all match.

| Package | Active version / role | Exact historical requirement |
| --- | --- | --- |
| Microsoft.CodeAnalysis.Common | 5.0.0, transitive from the active compiler/workspace packages | `[5.0.0-2.25451.107]`, direct in the probe |
| Microsoft.CodeAnalysis.CSharp | 5.0.0, direct Main reference | `[5.0.0-2.25451.107]`, direct in the probe |
| Microsoft.CodeAnalysis | 5.0.0 umbrella, direct Main reference | Not needed in the historical boundary; do not import umbrella dependencies |
| Microsoft.CodeAnalysis.CSharp.Workspaces | 5.0.0, direct Main/audit reference | Not needed; no historical Workspace API is pulled in |
| Microsoft.CodeAnalysis.Workspaces.MSBuild | 5.0.0, direct Main/audit reference | Not needed; active project closure is behind the host cut |
| Microsoft.CodeAnalysis.Workspaces.Common | 5.0.0, transitive | Not needed |
| Microsoft.CodeAnalysis.VisualBasic / VisualBasic.Workspaces | 5.0.0, transitive through the active umbrella | Not needed |
| Microsoft.CodeAnalysis.Analyzers | 3.11.0, build-time analyzer tooling | `[3.11.0]`, unchanged tooling dependency; private assets, not the compiler API version |
| Microsoft.Net.Compilers.Toolset | Not a current PackageReference; the installed SDK drives compilation | Provenance archive `5.0.0-2.25451.107` validated; deliberately not installed as the probe compiler driver |

The historical CSharp nuspec requires Common with the **exact same version interval** `[5.0.0-2.25451.107, 5.0.0-2.25451.107]`. Both are strictly pinned; no floating, stable fallback, or suffix trimming. Historical Workspace versions are neither guessed nor required by this boundary.

Archive SHA256:

- Toolset: `2B1B4939BD3ABB9F4CB040BF1439EF8A59868918054DFB160FB89A84E59CE3BE` (23,344,285 bytes).
- Common: `01166A92FF3B0F0C1938685B20EE0A6CBED5534E46A3675AD8FD0E2D8946A665` (7,314,314 bytes).
- CSharp: `383991E8B4FFE9CCA8A91C57389E3DF4BE66D5219C11C58739D2192FD1FE9506` (18,332,392 bytes).

Restored net8.0 pair:

| Reference | Public assembly version | MVID | SHA256 |
| --- | --- | --- | --- |
| Microsoft.CodeAnalysis.dll | 5.0.0.0 | dc7738cc-6dca-4d34-9c44-29b53a7caa93 | 660C3D626C4B8F4CF8C231FBEF0FB6B4DB4FFFCC89EF5B31AAECA1CF4D7F66A1 |
| Microsoft.CodeAnalysis.CSharp.dll | 5.0.0.0 | 0f9c1dcf-4eb1-47f8-81b2-733db5887be7 | B0EC1DDCA4C97DCF15845FF4BCEB5499C3989025197D5D65E04310E29C09217D |

Both have the recorded historical informational version. The active stable package instead contains build `5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09`. Identical public `AssemblyVersion=5.0.0.0` does not establish identical API capabilities or runtime substitutability.

## Actual current project/package graph

All relevant projects target `net8.0`. Package versions are defined in individual `.csproj` files; no applicable ancestor `Directory.Packages.props`, `Directory.Build.props/targets`, `global.json` or NuGet.Config overrides were found. The probe's own Directory.Build.props is local to its experiment subtree.

| Project | Direct graph and role |
| --- | --- |
| XMLDocNormalizer | Owns and compiles the complete productive Roslyn-facing Analyzer source locally; no ProjectReference to Core. Direct current Roslyn packages are in the table above; additionally Build.Framework 17.11.31 (exclude runtime/private all), Build.Locator 1.11.2, NuGet.Frameworks 6.5.0 (exclude runtime/private all). |
| XMLDocNormalizer.ExceptionFlow.Core | No Roslyn PackageReference or ProjectReference. Compiles only linked neutral Canonical/value/path sources, not the full Roslyn-facing Analyzer. Protected outputs were not rebuilt. |
| XMLDocNormalizerTests | References Main, neutral Core using alias ExceptionFlowCore, and Evaluation. No direct Roslyn packages; current packages flow from Main. xUnit 2.5.3, runner 2.5.3, Test SDK 17.11.1, coverlet 6.0.0, NuGet.Frameworks 6.5.0. |
| XMLDocNormalizer.Evaluation | References Main and inherits its active Roslyn graph. |
| ContextualBoundaryAudit | Standalone current-version evidence tool, outside the solution; direct CSharp.Workspaces/Workspaces.MSBuild 5.0.0, Build.Framework/Locator. |
| HistoricalCompileProbe | No ProjectReferences. Strict historical Common/CSharp plus private Analyzers 3.11.0, Immutable/Reflection.Metadata 9.0.0. Exact 120 linked productive files and the throwing compile-only host. |
| MetadataAudit | No ProjectReferences; current Common/CSharp strictly pinned to 5.0.0. Reads historical files as PE metadata references only. Not a second runtime Roslyn world. |

The active Main resolved graph has all nine Roslyn package identities listed above (including Analyzers), Immutable 9.0.0 and Reflection.Metadata 9.0.0. Main, Tests, Evaluation and audit resolved package/project edges and selected compile/runtime assets are fully inventoried in the JSON, not merely inferred from the direct references.

The historical graph resolves exactly five packages: Common and CSharp at the full preview version, Analyzers 3.11.0, Immutable 9.0.0, Reflection.Metadata 9.0.0. Metadata depends on Immutable; both Roslyn packages depend on the same tooling/BCL packages; CSharp depends on the exact Common version. No workspace, Main/Core runtime dependency, downgrade or package-version-conflict diagnostic occurs. The two graphs coexist in the isolated package cache without replacing active references.

## Probe and host boundary

`HistoricalCompileProbe.csproj` disables default Compile items, imports `HistoricalSources.props` with 120 explicit links to the genuine production files, and supplies `CompileOnlySemanticEnvironment.cs`. MSBuild evaluated 121 explicit items; normal generated global-usings/assembly metadata files are SDK infrastructure, not extra Analyzer logic.

The active host cut is `src/XMLDocNormalizer/Execution/Semantic/ProjectClosureExceptionFlowSemanticEnvironment.cs`, which depends on Main project closure, workspace and supporting-source acquisition. It must not enter the historical algorithm assembly. The experimental local partial supplies only the already consumed type signatures: semantic-model lookup; analysis scopes; supporting method resolution (two overloads); external supporting scope resolution. All five members throw `NotSupportedException` unconditionally. It contains no state, constructor logic, scopes, source policy or fake body-resolution algorithm and is never executed. It proves compile-boundary feasibility only; a real nonvirtual capability host remains later work.

The regular solution and productive project files do not reference or discover the experiment. It is reversible by removing only the B1-owned experimental files, but no removal/reset was performed.

## Complete compile result and API drift

Initial restore: success. Initial unchanged-source compile and the repeated forced unchanged-source compile both fail with **0 warnings / 4 errors**, all CS1061:

| Productive file | Line,column | Caller | Missing current API / historical counterpart |
| --- | --- | --- | --- |
| ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs | 218,27 | AddSummaryExplicitAwaitEdges | AwaitExpressionInfo.RuntimeAwaitMethod / none |
| ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs | 221,31 | AddSummaryExplicitAwaitEdges | AwaitExpressionInfo.RuntimeAwaitMethod / none |
| ExceptionFlowAnalyzer.SummaryGraphImplicitDispatch.cs | 275,27 | AddSummaryExplicitAwaitDispatchEdges | AwaitExpressionInfo.RuntimeAwaitMethod / none |
| ExceptionFlowAnalyzer.SummaryGraphImplicitDispatch.cs | 278,31 | AddSummaryExplicitAwaitDispatchEdges | AwaitExpressionInfo.RuntimeAwaitMethod / none |

All files are under `src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/`. The JSON retains full localized diagnostics, codes, paths, positions and unchanged repeated messages. Full diagnostic logs and binlogs are under `artifacts/p5o2b1/historical-{first,repeat}.*`.

Classification: four absent-API errors; zero signature errors, absent types, changed used enum values, nullable/annotation/TFM problems, package/reference errors, incomplete-source errors or other MSBuild/compiler errors. This is not a missing package or missing source-boundary dependency. Adding current Workspace or Main references would neither supply the historical getter nor be an acceptable solution.

The metadata inspection uses **one current Roslyn engine** with independent current/historical PE reference compilations. It never Assembly.Loads a historical DLL. The process's actual loaded Roslyn informational versions and MVIDs are recorded, and historical runtime loading is checked and rejected. The same 120 files and host bind with zero errors against current API metadata.

Complete used surface:

- 429/430 member signatures have identical inspected metadata contracts, including return/parameter nullable types, ref kinds, optional/default/params information and generic constraints. The missing getter is the single difference.
- 210/210 used Roslyn types exist historically.
- 84/84 used enum constants have equal numeric values, including their file/caller sites; this closes a drift class a successful compile alone would not detect.

No adaptation is necessary for the 429 unchanged compile contracts or the unchanged used constants. This says nothing about behavioral equivalence of compiler versions, unused APIs, arbitrary metadata attributes, or exact emitted PE equality; those are not B1's scope.

The missing property has current semantic significance. When runtime async is enabled it identifies calls to AsyncHelpers.Await/AwaitAwaiter/UnsafeAwaitAwaiter; it is not synonymous with the normal GetAwaiter/IsCompleted/GetResult pattern. Existing code deliberately handles this branch before falling back to the normal await pattern. [Official RuntimeAwaitMethod API documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.awaitexpressioninfo.runtimeawaitmethod?view=roslyn-dotnet-5.0.0).

Minimal strategy classification: **small typed compile-time capability accessor / shim candidate**, conditional on proving historical runtime-async capability. Keep the current branch, uncertainty and path-step behavior; do not simply delete four reads, use dynamic/reflection, or pretend the preview has the stable getter. B1 performed no hypothesis edit to production or to the source-linked algorithms.

## TFM and build-driver distinction

Historical Common/CSharp library packages contain native `net8.0`, `net9.0`, and `netstandard2.0` assets. Restore chooses native net8.0 compile and runtime assets, with the same Immutable/Reflection.Metadata 9.0.0 dependencies as current packages. **No TFM change is necessary.** Netstandard fallback is available but unnecessary; its additional Memory/Unsafe/CodePages/Tasks dependencies do not enter the chosen net8.0 graph.

B1 uses installed SDK **8.0.418** and its **Roslyn 4.11.0-3.25569.22 (3fb752d4) compiler driver**, verified with `csc.dll -version`, to compile against the exact historical API references. This is an API-targeted build, not execution of the historical CSharp compiler. The historical Toolset driver/archive has separate .NET Framework/netcore runtime requirements and was not selected, loaded or substituted. Historical compiler execution, runtime hosting and exact signed PE reconstruction remain separate later validations.

## Isolation and B2 owner recommendation

| Variant | Source sharing / maintenance | Isolation, identity and outputs | Testing / worker suitability |
| --- | --- | --- | --- |
| Separate source-linked Historical Core project (recommended after B1A) | One productive source implementation; explicit shared item manifest; independent build owner | Strict Common/CSharp pins; independent obj/project.assets.json and bin; distinct Analyzer assembly name recommended; no Main runtime reference | Straightforward independently built fixtures; prepares an isolated later worker without implementing one |
| One parameterized project with Current/Historical flavor | Same source, but every caller and restore must pass a coherent flavor | Viable only with early flavor-specific intermediate/output/assets paths and references; default shared obj permits restore overwrite/races | More fragile for IDE, solution builds and parallel tests; later worker packaging less explicit |
| Multi-targeting alone / neutral Core reuse | Different TFMs do not select compiler versions; both variants need net8.0; existing neutral Core is not the algorithm owner | A TFM switch does not isolate same-identity Roslyn packages. Reusing neutral source item definitions is possible, importing Main is not | No demonstrated reason for a new TFM. Not an alternative to a distinct historical owner/package graph |

Recommended B2 structure, **not implemented**: retain Main's current source-local Analyzer compile; add a dedicated historical source-linked owner with this exact productive boundary, bounded proven capability seam, exact package pins and its eventual real compile-local host. Share source/item definitions, not compiled Roslyn-facing Main types. Keep neutral canonical/value source reuse consistent with A6F's identity constraints.

Distinct output/intermediate paths are mandatory even if builds are sequential: restore assets and reference/runtime copies must never overwrite each other. A distinct historical Analyzer assembly name is strongly recommended for test/package identity; it does **not** isolate Microsoft.CodeAnalysis DLLs, whose public identities are the same. Later runtime work must keep compiler universes apart; B1 does not choose an AssemblyLoadContext or worker implementation.

Actual B1 paths: `artifacts/p5o2b1/HistoricalCompileProbe/{obj,bin}` and `artifacts/p5o2b1/MetadataAudit/{obj,bin}`, with isolated `artifacts/p5o2b1/packages`. The probe assembly name is `XMLDocNormalizer.B1.HistoricalCompileProbe`. There are zero ProjectReferences, no solution membership and no historical dependency copied into a normal Main output. Repeated restore resolved the exact graph; a further forced restore produced byte-identical assets (SHA256 `8CD89CC596C8B098BC96628473EC32C1C27BF2775A3CE312A633F4C7C67DD0CD`). No historical Analyzer DLL is emitted because compilation correctly fails.

## Reproducible commands

Run from `Tools/XMLDocNormalizer`. The tested historical restore uses retained validated P5N archives as an offline feed and the existing user package cache for tooling/BCL dependencies. This is not a fallback to another compiler version. Those archives/build logs are ignored local evidence; a clean machine must reacquire the original ArtifactUrls recorded in the JSON and verify their hashes, or receive that retained artifact set. Restore sources are explicit and machine-specific, not hidden prerequisites.

Current build, independently executable with its existing restored current graph:

```powershell
dotnet build src/XMLDocNormalizer/XMLDocNormalizer.csproj --no-restore -warnaserror --verbosity minimal
```

If that current graph has not been restored, first run normal `dotnet restore src/XMLDocNormalizer/XMLDocNormalizer.csproj`; this prerequisite command is documented, not claimed as an additional B1 run.

Historical probe, without touching active PackageReferences:

```powershell
dotnet restore Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj --source artifacts/p5n-acquisition/packages --source C:/Users/Krause/.nuget/packages --verbosity minimal
dotnet build Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj --no-restore -warnaserror --no-incremental --verbosity minimal -fl '-flp:logfile=artifacts/p5o2b1/historical-first.log;verbosity=diagnostic' -bl:artifacts/p5o2b1/historical-first.binlog
dotnet restore Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj --source artifacts/p5n-acquisition/packages --source C:/Users/Krause/.nuget/packages --force-evaluate --verbosity minimal -fl '-flp:logfile=artifacts/p5o2b1/restore-repeat.log;verbosity=normal'
dotnet build Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj --no-restore -warnaserror --no-incremental --verbosity minimal -fl '-flp:logfile=artifacts/p5o2b1/historical-repeat.log;verbosity=diagnostic' -bl:artifacts/p5o2b1/historical-repeat.binlog
```

Restore exits 0; **each unchanged-source historical build must exit 1 with the same four errors**, not be interpreted as a successful dual-version build. Full error collection preceded metadata classification; no fixes were interposed between the two builds.

Metadata inventory and evidence regeneration:

```powershell
dotnet restore Evaluation/P5O2B1CompileProbe/MetadataAudit/MetadataAudit.csproj --source C:/Users/Krause/.nuget/packages --verbosity minimal
dotnet build Evaluation/P5O2B1CompileProbe/MetadataAudit/MetadataAudit.csproj --no-restore -warnaserror --verbosity minimal
dotnet run --project Evaluation/P5O2B1CompileProbe/MetadataAudit/MetadataAudit.csproj --no-build -- D:/Repository/SEE/Tools/XMLDocNormalizer
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B1CompileProbe/Write-BuildFeasibilityEvidence.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B1CompileProbe/Test-BuildFeasibilityEvidence.ps1
```

The execution-policy override is process-local, not a persistent system change. The generator asserts unchanged HEAD, productive source, exact evaluated boundary, archive/reference hashes, repeated diagnostics and protected foreign state before writing evidence. It requires the recorded build logs and P5N/A6F artifacts; it does not pretend to run validations itself.

## Validation and inherited semantics

| Validation | Result |
| --- | --- |
| Current Main warning-as-error build | PASS, 0 warnings / 0 errors; repeated and logged |
| Historical restore / repeat / further forced restore | PASS, exact graph; final forced restore assets byte-identical |
| Initial / repeated historical compilation | Expected failure, 0 warnings / 4 identical CS1061 errors, complete logs and binlogs |
| Metadata tool warning-as-error build / source binding | PASS, 0 warnings / 0 errors; no historical runtime DLL loaded |
| Project/package/TFM/source boundary | PASS, exact 120 productive files; no extra Main reference; native net8.0 assets |
| B1-owned full C# format verification | PASS, only the host and MetadataAudit Program included; linked production files excluded |
| B1 JSON/XML/PowerShell parse and CRLF gates | PASS, 12 task-owned/modified files; strict UTF8, final newline and no trailing whitespace; no foreign-file normalization |
| git diff --check | PASS |
| Foreign state | All eight original foreign file hashes and all six stash identities/descriptions unchanged |

No productive C# or semantics changed; therefore B1 deliberately did not rerun the full suite, Self Analysis or canonical finding comparison. Inherited from verified unchanged A6F:

- Full and full-final: **2,576/2,576**, zero failures/skips; original TRX hashes retained in the B1 JSON.
- Self Analysis: **16 findings**, DOC610=0, DOC611=1, DOC631=15, DOC632=0.
- Canonical diff: **0 added / 0 removed / 0 evidence changes**. Full raw and normalized finding/evidence arrays identical; normalized SHA256 `2335A309D71F94CC0D3DD7546B57E2D14A107806CD666DD4FEBED5F04AB5F8DF`.

These are inherited semantic results, not new historical-runtime test results. No new regression test was necessary for a compile-only, nonproductive change. Existing independent flakes and foreign formatting deviations were untouched.

The quality gate passed, its results were incorporated in the JSON, and the regenerated evidence passed again. Git reports the pre-existing LF-to-CRLF warning for protected `.gitignore`; `git diff --check` exits 0 and its byte hash is unchanged. That warning was not repaired or normalized.

## Remaining risk and bounded next step

The only demonstrated compile blocker is RuntimeAwaitMethod's absence; the smaller package must prove what the historical binder can produce before deciding a historical accessor result. Compilation alone cannot prove await lowering, dispatch, exception behavior, compiler provenance, a real external semantic host or PE reconstruction. Current API metadata equality outside this one property does not remove those later runtime obligations.

B1A acceptance should cover normal and runtime-async await capability, both explicit Summary paths and their uncertainty/path behavior, and a same-source historical compile with no new omitted dependencies. If it changes productive code, rerun the full suite, Self Analysis and canonical diff as required. Then authorize B2's separate historical source-linked owner. **No B2, runtime host, worker, IPC, serialization, canonical transport or process lifecycle work was begun in B1.**

No commit, push, stash mutation or reset. `.gitignore`, seven protected Core bin/obj files and six existing stashes remain untouched. The complete final `git status --short` is supplied with the handoff.

Git handoff caveat: the existing root `.gitignore:41:*.csproj` rule ignores `Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj` and `Evaluation/P5O2B1CompileProbe/MetadataAudit/MetadataAudit.csproj`. Both files exist in this workspace, have recorded hashes and were used by the builds; they do not appear in untracked status. No ignore exception or staging was performed. A later authorized commit must explicitly include these two project files (for example by deliberate force-add), rather than relying on a plain add of only visible untracked files.
