# P5O2B2 - Historical Analyzer Build Project / Dual-Version Build Scaffold

Date: 2026-10-08. Starting/unchanged HEAD:
`40d59416489584567a3e3cce5770402881383f37` (`Add runtime await compatibility boundary.`).
Initial Working Tree: only the protected ` M ../../.gitignore`.

**COMPLETE: permanent Current/Historical dual-version build is reproducibly available.**
This closes build ownership and compile regression, not historical execution.
No historical image was loaded or executed; external analysis/Dapper advances to
no new pipeline stage in this build-only package.

## Plan verification and final ownership

The complete B1/B1A/B1A2 reports, JSON audits, API matrix and boundary register were
read before implementation. The A2 source-linked owner/package/isolation plan is
implemented without productive Analyzer changes. All 361 original Current files
and the added capability (362 unique files), including the 121-file Historical
boundary, match the A2 SHA256 fingerprints. The Current project and solution are
byte-identical to the starting state. No additional Roslyn drift or compatibility
layer was introduced.

| Owner | Current | Historical |
| --- | --- | --- |
| Project | `src/XMLDocNormalizer/XMLDocNormalizer.csproj` | `src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj` |
| TFM | net8.0 | net8.0 |
| Assembly | XMLDocNormalizer | XMLDocNormalizer.ExceptionFlow.Historical |
| Source ownership | unchanged Main source-local glob | `build/ExceptionFlow.HistoricalSources.props`, physical linked Main files |
| Solution membership | unchanged normal solution | deliberately separate target, not in solution |
| Runtime ProjectReferences | none | none; no Main or neutral Core reference |
| Assets | `src/XMLDocNormalizer/obj/project.assets.json` | `artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/obj/project.assets.json` |
| bin/obj | unchanged Main paths | same isolated artifacts project root, separate bin/obj |
| Package cache | normal NuGet global cache | `artifacts/exception-flow-historical/packages/` |

The manifest retains **all original 120 files plus the one productive RuntimeAwait
capability**. The 120-file list is exactly the proven B1/A2 boundary, not a subset
chosen for build convenience. It includes canonical/model/support files required
by the algorithm but excludes Main CLI/composition/project-closure owners. Every
Historical shared Compile item is the same absolute physical file also compiled
by Current; 121 unique items, no copies, projections or generated source fork.
Historical has one additional build-contract host item, for 122 explicit items;
ordinary SDK-generated assembly/global-using sources are not productive boundary
items. The complete shared-path/hash census is in the audit JSON.

### Explicit host qualification

A2's audit explicitly defers replacing the five throwing compile-host members with
a real compile-local semantic host to later authorized runtime work. B2 introduces
a permanent `BuildOnlySemanticEnvironment`, not a dependency on the Evaluation
probe. It implements only those existing five signatures, throws rather than
substituting semantic models/resolutions/scopes, also rejects construction, and
marks the assembly's ExecutionContract as BuildOnly. There is no Analyzer policy,
state, symbol cache, new lifetime or source-resolution algorithm in it.

This is a deliberate build-only contract, **not an executable historical host**.
The historical Analyzer implementation and RuntimeAwait capability are the genuine
productive shared sources; the missing host is not disguised as a working runtime
adapter. This qualification follows the A2 JSON recommendation's explicit later-host
clause and the B2 prohibition on runtime integration. The next runtime package must
replace this host contract with real capabilities and validate execution.

## Exact package graphs and restore provenance

Historical direct strict pins, also the **entire five-package resolved graph**:

| Package | Requested / resolved |
| --- | --- |
| Microsoft.CodeAnalysis.Common | [5.0.0-2.25451.107] / 5.0.0-2.25451.107 |
| Microsoft.CodeAnalysis.CSharp | [5.0.0-2.25451.107] / 5.0.0-2.25451.107 |
| Microsoft.CodeAnalysis.Analyzers | [3.11.0] / 3.11.0, PrivateAssets=all |
| System.Collections.Immutable | [9.0.0] / 9.0.0 |
| System.Reflection.Metadata | [9.0.0] / 9.0.0 |

No umbrella/Workspace/MSBuild/Toolset package, stable fallback or version-suffix
truncation. Compiler API packages resolve exclusively through the recorded public
dnceng feed `d1622942-d16f-48e5-bc83-96f4539e7601`; other dependencies exclusively
through nuget.org. Project-local `NuGet.Config`, explicit RestoreConfigFile and
source mapping prevent accidental use of the Current package graph. Actual restored
`.nupkg.metadata` source URLs/content hashes and assets SHA512 values are audited.
First restore acquired into the new isolated cache; no Evaluation/offline package
directory is a permanent dependency. NuGet audit is not disabled.

Actual `ResolveReferences` items select the historical net8.0 Common/CSharp DLLs
from that isolated cache. Their SHA256 hashes match B1/A2's compiler evidence:

- Common: `660C3D626C4B8F4CF8C231FBEF0FB6B4DB4FFFCC89EF5B31AAECA1CF4D7F66A1`
- CSharp: `B0EC1DDCA4C97DCF15845FF4BCEB5499C3989025197D5D65E04310E29C09217D`

Current retains its unchanged direct packages: Build.Framework 17.11.31,
Build.Locator 1.11.2, NuGet.Frameworks 6.5.0, CodeAnalysis umbrella/CSharp/
CSharp.Workspaces/Workspaces.MSBuild 5.0.0 and Analyzers 3.11.0.
All resolved CodeAnalysis compiler/workspace packages remain stable 5.0.0;
Analyzers remains 3.11.0. Relevant transitives remain Immutable/Metadata,
Microsoft.Extensions.*, System.Composition.*, Bcl.AsyncInterfaces, Pipelines,
DiagnosticSource and Channels 9.0.0; Humanizer.Core 2.14.1, Newtonsoft.Json 13.0.3,
SolutionPersistence 1.0.52, Encoding.CodePages 8.0.0, Unsafe 6.1.0 and the ordinary
4.6.0/4.6.0/4.6.0/4.6.0 support packages (Buffers/Memory/Numerics.Vectors/
Tasks.Extensions). The complete 39-package Current graph is recorded, not filtered
to only direct packages. No historical preview package occurs in Current.

Local SDK: 8.0.418. This builds an Analyzer library against the exact historical
**API** packages; it does not claim to run the historical Toolset compiler driver
or reconstruct an external PE in B2. Both public Roslyn AssemblyVersions being
5.0.0.0 still says nothing about runtime coexistence or RuntimeAwait availability.

## Commands and automatic regression boundary

From `Tools/XMLDocNormalizer`, without editing configuration between targets:

```powershell
dotnet build src/XMLDocNormalizer/XMLDocNormalizer.csproj -warnaserror
dotnet build src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj -warnaserror
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1
```

The gate works with PowerShell 5 or `pwsh`, also accepts `-Configuration Release`,
and emits logs plus audit JSON to `artifacts/dual-version-build/`. Early
Directory.Build.props establishes canonical isolated BaseIntermediateOutputPath,
MSBuildProjectExtensionsPath, BaseOutputPath and RestorePackagesPath before SDK
imports. The source manifest is explicit and Main's normal glob remains unchanged.

Automatic compile regression is at the **build/CI level**, not per-unit-test:
`.github/workflows/xml-doc-normalizer-dual-build.yml` runs the same gate for changes
under this tool, plus workflow changes, and allows manual dispatch. It restores and
compiles genuine shared files against the strict preview pins. A future Current-only
API therefore fails Historical compilation directly. The gate also rejects package,
source-count, capability, runtime-reference and output-isolation drift and stale
success audits after a failed rerun. CI uses .NET 8 and pwsh on Ubuntu; the identical
script was executed locally under Windows PowerShell. Remote CI was not run or
claimed; no push occurred.

Normal Main/Evaluation/Core/Tests have no Historical references; normal solution
build/test does not discover this target or load its assembly. Metadata identity
checks use AssemblyName.GetAssemblyName on the file, not Assembly.Load/LoadFrom.
No ALC, worker, IPC, result transport or Main runtime loading was implemented.

### Narrow versionability exceptions and local wiring corrections

The root `*.csproj` rule required a project-local `.gitignore` exception for exactly
`XMLDocNormalizer.ExceptionFlow.Historical.csproj`. The existing global `build/`
rule also hid the new manifest/script/docs; the tool-local `.gitignore` reopens only
those three named files and keeps other build-directory contents ignored. Both
exceptions are Git-visible without force-add or index mutation. Protected
`../../.gitignore` was never edited.

Two small audit/MSBuild corrections were made in scope:

1. Current's default RestorePackagesPath is empty in project evaluation; its real
   cache is obtained from assets.packageFolders rather than passing an empty path
   to GetFullPath.
2. The unnormalized Historical RAR-cache path containing `../..` reaches 260 Windows
   characters (canonical path 208). MSB3101 persisted even in a serial diagnostic;
   canonicalizing the artifacts root before SDK imports fixed repeated clean/build.
   An additional concurrent reference diagnostic had also been attempted; none of
   the failed/interrupted series counts as successful validation. Failed-series
   logs are retained under `artifacts/p5o2b2/interrupted-gate-logs/`.

No API, host-semantics or package blocker remained. These are local wiring changes,
not a deviation to a different architecture or broader Analyzer refactoring.

## Fresh validation and reproducibility

The final gate forces **Current clean/build, Historical clean/build, Current
clean/build, Historical clean/build, Historical clean/build, Current clean/build**.
Thus the required forward order and the reverse Historical-then-Current pair are
covered. After each clean it verifies that its own image no longer exists. After
each clean/build it compares the entire opposite bin tree and opposite assets
hash. All six pairs pass with 0 warnings / 0 errors, no opposite artifact mutation.
Both targets' three DLLs and PDBs are separately identical within this checkout.

| Target | Repeated DLL SHA256 |
| --- | --- |
| Current | `28072C6FC2B1ECF543D605B68B21FC78240A0833EB9FE3473F9BF36B6C232EAF` |
| Historical | `866CFBC5EECD7D69FF87BAF47F92E32DEBB9881F6841919262564E3EF1E10519` |

These are new permanent-scaffold outputs, not substituted B1/A2 probe-image hashes.
Repeatability is measured within the same source checkout/SDK/configuration; source
link/commit/path effects across commits or SDKs are not asserted away.

Fresh test build with `-warnaserror -p:BuildProjectReferences=false`: 0/0, avoiding
protected Core regeneration. Fresh relevant project/architecture/dependency/
RuntimeAwait tests: **72/72**, no skips. Includes eight new inexpensive project
contract tests for source boundary, pins, permanent wiring, early output isolation,
normal graph separation, rejecting host, source mapping and narrow project ignore.
Additional fresh full Current suite: **2608/2608**, no skips (A2's 2600 plus eight).
TRX and build logs are hashed in the audit JSON.

```powershell
dotnet build Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-restore -warnaserror -p:BuildProjectReferences=false
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~HistoricalAnalyzerBuildProjectTests|FullyQualifiedName~ExceptionFlowSemanticDependencyGuardTests|FullyQualifiedName~RuntimeAwait|FullyQualifiedName~Dependency' --logger 'trx;LogFileName=build-architecture.trx' --results-directory artifacts/p5o2b2/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --logger 'trx;LogFileName=full.trx' --results-directory artifacts/p5o2b2/tests
dotnet format whitespace . --folder --include src/XMLDocNormalizer.ExceptionFlow.Historical/BuildOnlySemanticEnvironment.cs Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/HistoricalAnalyzerBuildProjectTests.cs --verify-no-changes
```

Folder-only format verification passes without opening a Core MSBuild workspace.
XML/JSON/PowerShell parsing and scoped Git whitespace checks pass. New build files,
tests and evidence are versionable; no existing Probe project was altered.

### Explicit inherited semantic evidence

Per B2 section 17, Self Analysis and Canonical Diff inherit the completed A2
baseline, with the 362 productive fingerprints reverified; **not rerun in B2**:

- Self Analysis: 16 findings, DOC610=0, DOC611=1, DOC631=15, DOC632=0.
- Canonical Diff: 0 added / 0 removed / 0 evidence changes; A2 complete raw and
  normalized Finding/Evidence arrays identical.

The shared capability, its cached public getter lifetime and three states
(available symbol / native null / unavailable) are unchanged. Both unavailable
guards and four Call-Site substitutions are unchanged; no null collapse, scope/cache
redesign or semantics adjustment. The new rejecting build-contract host is outside
Current and the normal solution, has no Analyzer policy and is never executable.
Consequently there is no new Current productive C# semantics requiring fresh
Self Analysis/Canonical Diff. Full tests were nevertheless rerun as noted above.

## Retained evidence, remaining boundary and next step

B1/B1A/B1A2 Evaluation compile targets are now **redundant as ongoing compile
regression owners**, superseded by the permanent target/gate. Their original
provenance, compiler-source/PDB checksums, metadata/IL drift, capability and
Call-Site evidence remains useful and retained byte-for-byte. No probe project,
archive, report or audit history was deleted.

Next sensible package: **P5O2B3 - isolated historical runtime/worker host boundary**.
Implement a real compile-local semantic host in one isolated historical Roslyn
universe and validate bounded execution/canonical-only handoff before any Main
integration. This is the next authorized-work proposal, not runtime code added
here. Do not repeat proven compile-compatibility analysis. The build-only host is
not sufficient for execution; runtime isolation/host correctness remains an explicit
open boundary. Existing fail-closed external behavior remains unchanged.

## Protected state / handoff

Initial/final root ignore SHA256 matches; all **21 existing Core bin/obj files**,
their file set and SHA256s match; all six stash identities/messages match, including
both protected position-provenance stashes. Main project and solution hashes match.
No commit, push, reset, checkout, restore-to-HEAD, stash/index operation or foreign
change. Regular target `dotnet clean` only removed/recreated its own disposable
build outputs; protected Core outputs were not touched.

The companion audit records the full final `git status --short`, initial protected
snapshot, package graphs, reference hashes, all 121 source fingerprints, six build
images/log hashes, test counters and explicit inherited semantic provenance.
Revalidate with `Evaluation/P5O2B2Proof/Write-Verify-ScaffoldEvidence.ps1` after the
build/test commands; it emits `artifacts/p5o2b2/scaffold-audit.json` for this run.
