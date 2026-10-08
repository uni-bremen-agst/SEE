# Permanent dual-version compile gate

Run from `Tools/XMLDocNormalizer` with .NET SDK 8:

```powershell
dotnet build src/XMLDocNormalizer/XMLDocNormalizer.csproj -warnaserror
dotnet build src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj -warnaserror
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1
```

On Linux/macOS use `pwsh -File build/Verify-DualVersionBuild.ps1`. The gate supports
`-Configuration Release`. It restores independent graphs and forces six clean/build
steps (Current, Historical, Current, Historical, Historical, Current). It checks all
121 physical shared files, actual package versions, separate assembly/assets/bin,
unchanged opposite output trees and deterministic DLL/PDB hashes. Evidence goes to
`artifacts/dual-version-build/`. GitHub's dedicated dual-build workflow runs this gate
once per change, not once per unit test. Normal solution tests remain Current-only.

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

This is a **build target, not a runnable historical analyzer**. The compile-local
`BuildOnlySemanticEnvironment` completes the existing five-member host contract,
rejects construction/execution and contains no analyzer/source-resolution policy.
No historical assembly is loaded (the gate reads assembly names as file metadata).
Per the A2 recommendation, the next isolated-runtime/worker package must supply a
real semantic host and prove historical execution before runtime integration.
Nothing here modifies the RuntimeAwait availability contract, scopes or caches.

The older B1/B1A/B1A2 Evaluation compile probes are retained historical evidence;
their compile-regression role is superseded by this permanent target. No probe
project, source projection or proof-only RuntimeAwait helper is a build dependency.
