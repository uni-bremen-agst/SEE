# CI fix: missing tracked ExceptionFlow.Core project

Date: 2026-10-09. Branch: `fix/documentation`.
Reported failing revision: `6fc6f19a4f` (B5). Initial local audit found B6 HEAD
`35dbd5b215`; B7 was externally committed as `eed44bc56080bda516be2a2cf424d0db43ab74f2`
during read-only investigation, before fix changes. This fix is based on B7;
neither B6 nor B7 was reset, rewritten or included as a new fix change.

## Cause and architecture decision

The test project and solution reference the genuine
`src/XMLDocNormalizer.ExceptionFlow.Core/XMLDocNormalizer.ExceptionFlow.Core.csproj`.
That existing local file was absent from Git because the root Unity ignore rules
exclude `*.csproj`. Git tracked only 19 generated Core `bin/obj` files. A clean
CI checkout therefore cannot resolve the project: MSB9008 is the primary error;
CS0430 (missing `ExceptionFlowCore` alias) and CS0234 are downstream effects.
Pre-existing local outputs and earlier `BuildProjectReferences=false` runs could
conceal the missing tracked input.

Core is **needed as an independent neutral validation assembly**, not as a runtime
dependency of Main or Historical. The A6 architecture report explicitly retains
this boundary. Its real existing project compiles **18 existing physical shared
sources**: canonical identities/context/summary/result and neutral facts/path
models. Sources are linked from Main, not copied or stubbed; Core has no package,
Roslyn, MSBuild or project references. `ExceptionFlowCoreDependencyTests` inspects
the actual independently compiled assembly's references and rejects compiler,
workspace, build and Main dependencies. Removing the project/test would remove
this meaningful neutrality proof. Main and Historical continue to source-compile
their own universes and do not reference this validation assembly at runtime.

## Changes

- Track the **unchanged original** Core `.csproj`; no dummy project, added runtime
  dependency, Analyzer edits or new source copies. All 18 linked sources already
  exist in Git and were verified.
- Add Core-local `.gitignore`: only the exact project exception plus `bin/` and
  `obj/`. Root `.gitignore` and its existing local changes remain byte-identical.
- Remove exactly 19 generated files from the index with `git rm --cached`.
  This does not delete local files. The requested subsequent clean build rebuilds
  generated outputs normally; generated content is no longer versioned.
- Retain the original real-assembly dependency test and add two architecture
  guards for linked real source inputs/no package or project dependencies and
  narrow project/output ignore rules. No test or reference was removed.
- Add a read-only Linux input proof and LF attribute in `CI-CoreProjectFixProof/`.
  This report and JSON record the fix evidence. No routing, Roslyn version,
  Analyzer semantics or existing CI script change is part of the fix.

## Fresh results

| Validation | Result |
| --- | --- |
| `dotnet clean` then `dotnet build` test project, WAE, references enabled | 0 warnings / 0 errors |
| Core dependency/project guards + Historical project contract tests | 11/11 |
| `Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences` | PASS, 175/175 |
| Complete suite with `-p:IncludeSelfAnalysis=true` | 2778/2778, 0 failed, 0 skipped |
| Test project built from Git-index-only archive, no previous bin/obj | 0 warnings / 0 errors |
| Same 11 architecture cases in that clean exported tree | 11/11 |
| Clean archive on case-sensitive WSL Ubuntu filesystem | 703 source/project inputs present, no bin/obj |
| Scoped whitespace check and staged diff check | PASS |

The exact CI-equivalent main-boundary build restored/built real project references;
it did not pass `BuildProjectReferences=false`. The permanent gate also retained
six Current/Historical forced build images, two Worker builds, original runtime
identities, deterministic output, isolated Historical execution and fail-closed
protocol tests. All gate build steps report zero warnings/errors.

Linux proof inventories actual Compile/ProjectReference items for Core, Main,
Tests, Evaluation, Historical and Worker. Explicit source/project links must match
Git spelling ordinally; case-colliding Git paths are rejected. Eight Windows SDK
default-glob paths report physical `Models/DTO` while Git spells `Models/Dto`;
these are filesystem glob results, not explicit links. They resolve using tracked
spelling in the clean checkout. No case error is hidden in an explicit manifest.
All 703 clean-checkout inputs were checked on a genuinely case-sensitive Linux
filesystem. WSL has **no .NET SDK**, so a Linux build is **not claimed**; the clean
Git-only build was performed with Windows .NET 8. Existing Git-only source content,
not ignored local project files or binary outputs, supplies that build.

Commands from `Tools/XMLDocNormalizer`:

```powershell
dotnet clean Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj -warnaserror
dotnet build Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj -warnaserror
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore -p:IncludeSelfAnalysis=true
```

No test settings or error-suppression flags were changed. The process-only script
policy switch does not change machine policy. Logs/TRX, protected snapshot, source
inventory, Git archive/tree and Linux audit are under `artifacts/ci-core-fix/`;
the existing permanent gate audit is under `artifacts/dual-version-build/`.
The committed `CI-CoreProjectFix-audit.json` records counts and fingerprints.

## Repository protection and handoff

Only fix files and the 19 index removals are committed. Root `.gitignore` is not
staged or committed; its captured SHA256 and all six stash identities remain
unchanged. Pre-existing B7 is retained as the parent. No stash operations, push,
reset, restore or checkout of user changes were performed.
After the fix commit the expected and verified `git status --short` is only:

```text
 M ../../.gitignore
```
