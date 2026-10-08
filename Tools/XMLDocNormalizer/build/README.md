# Permanent dual-version compile gate

Run from `Tools/XMLDocNormalizer` with .NET SDK 8:

```powershell
dotnet build src/XMLDocNormalizer/XMLDocNormalizer.csproj -warnaserror
dotnet build src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj -warnaserror
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests
```

On Linux/macOS use `pwsh -File build/Verify-DualVersionBuild.ps1`. The gate supports
`-Configuration Release`. It restores independent graphs and forces six clean/build
steps (Current, Historical, Current, Historical, Historical, Current), then two
Worker-only clean/build pairs. It checks all
121 physical shared files, actual package versions, separate assembly/assets/bin,
unchanged opposite output trees and deterministic DLL/PDB hashes. Evidence goes to
`artifacts/dual-version-build/`. The same gate checks real Worker identity, two
fresh deterministic transitive analysis processes and a fail-closed protocol version.
GitHub's dedicated dual-build workflow runs this gate once per change and the
Current-host process/contract integration tests. No per-unit-test full compilation.

`ExceptionFlow.HistoricalSources.props` is the explicit original 120-file boundary
plus the productive RuntimeAwait capability. Main continues to compile those same
physical files through its unchanged source-local glob. Do not copy or project the
analyzer sources. Any deliberately authorized boundary extension must update the
manifest, source-count gate and architecture test together.

Historical is deliberately **not** in the normal solution and has no ProjectReferences.
Its five strict package pins restore into `artifacts/exception-flow-historical/packages`
using its own source-mapped NuGet.Config. Its unique assembly, generated sources,
assets and outputs reside beneath `artifacts/exception-flow-historical/`.
Only the two compiler API packages map to the recorded public dnceng feed; stable
dependencies map to nuget.org. No local Evaluation package directory is required.
The small project-local `.gitignore` exception makes just its project file versionable
without editing the protected repository-wide `*.csproj` rule. The tool-local
`.gitignore` exposes only the three named infrastructure files inside the otherwise
ignored `build/` directory.

P5O2B3 adds `src/XMLDocNormalizer.HistoricalWorker`, an executable net8.0 process
referencing only this Historical library. It reuses the same early B2 build
properties/package universe, with project-named separate bin/obj. Build/run:

```powershell
dotnet build src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj -warnaserror
'{"protocolVersion":2,"operation":"identity"}' | dotnet artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll
```

The real compile-local `HistoricalSemanticEnvironment` owns exactly one tree,
stable SemanticModel and compilation scope. No external supporting-source registry
or approximation. B2's old BuildOnly host is retained as uncompiled evidence;
EnableDefaultCompileItems=false and the exact Compile item prevent it executing.
The 121 productive sources, RuntimeAwait availability and existing caches are unchanged.

One UTF-8 JSON object through stdin until EOF produces one JSON stdout response
and process exit (0 success / 1 structured failure); diagnostics use stderr. Version 2
supports `identity` and `analyze`, with payload `{source,typeMetadataName,methodName}`.
Analysis is bounded to one source (32768 characters/8192 syntax nodes), a source-owned
non-generic type and a parameterless static non-generic method. Its fixed C#12/net8
runtime-reference profile cannot import metadata paths or Compiler/Analyzer assemblies.
The real summary graph returns the existing canonical exception-flow result owner,
including paths/identities; any uncertainty/truncation rejects successful completion.
No emitted source code is executed. The request frame limit is 65536 characters.

Main/normal tests never reference or load historical assemblies. P5O2B4 Main compiles
only the same neutral WorkerProtocol source (no runtime ProjectReference), with the
explicit HistoricalWorkerClient under Execution/Historical. Tests use that productive
client and its existing Current canonical domain. Successful responses require input
SHA256/selector/profile provenance; v1 is rejected rather than best-effort imported.
The optional typed context transports source/reference/supporting compilation data,
but the current bounded profile explicitly rejects these not-yet-supported inputs.
No routing, worker pool/daemon or ALC. The expanded gate builds the adversarial pipe
fixture and -RunMainBoundaryTests executes Main E2E/parity/identity/lifecycle tests.
Local tests use BuildProjectReferences=false to preserve Core WIP; on a clean CI
checkout, additionally use -BuildTestProjectReferences. CI enables both switches.
See `Evaluation/P5O2B3-historical-worker-host.md` for the runtime proof and limits.

The older B1/B1A/B1A2 Evaluation compile probes are retained historical evidence;
their compile-regression role is superseded by this permanent target. No probe
project, source projection or proof-only RuntimeAwait helper is a build dependency.
