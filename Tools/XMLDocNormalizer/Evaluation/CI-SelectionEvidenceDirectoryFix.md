# CI selection evidence directory fix

## Cause and scope

[Run 37954528078, job 113901549480](https://github.com/uni-bremen-agst/SEE/actions/runs/37954528078/job/113901549480)
failed on the Linux runner at the final evidence write in
`SelectedHistorical_ExecutesGenuineB5RouteWithParityAndIsolation` (previous line 193).
The functional assertions had passed; `File.WriteAllTextAsync` then threw
`DirectoryNotFoundException` because `artifacts/p5o2b6` did not exist.
An existing local evidence directory had hidden this test setup dependency.

The test now constructs its evidence path and explicitly calls
`Directory.CreateDirectory` on the parent before writing the unchanged JSON.
This also works when the parent already exists. No Analyzer semantics,
Current/Historical routing, Roslyn versions, assertions, or build gates changed.

The reported failing commit was `2f4690398142cc409039eff91f389fb51d94c6ae`.
During the initial read-only audit, the separate B8 work was committed externally
as `bdd913dee5ad30c2b2cebc2bed8915f30a5823a8`; validation and this fix use that
current parent, without reverting or including B8 work in the fix.

## Comparable test writes

Inspected direct text/byte/line writes, file creation, stream writers and file
streams across the test project. No additional comparable missing-parent write
was found. Other Worker evidence writers already create their directories.
Fixture writes use directory-creating temporary/workspace helpers or explicitly
create their project directories; temporary output files use the existing system
temporary directory. Missing-candidate tests intentionally read absent paths.

## Actual validation

Validation ran locally on Windows in a fresh export of the staged Git tree, not
against pre-existing workspace build outputs. Before execution, the export had
neither `artifacts` nor `bin/obj` directories.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter FullyQualifiedName~SelectedHistorical_ExecutesGenuineB5RouteWithParityAndIsolation
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore
```

- Full dual-version gate: PASS, including runtime/isolation checks and 212/212
  Main boundary cases. Test project references were built normally. All eleven
  build steps succeeded with warnings-as-errors, zero warnings and zero errors.
- Targeted test: 1/1 PASS. After the gate, only the export's newly generated
  `p5o2b6` directory was moved to a backup and its absence verified. The test
  recreated the directory and valid JSON: canonical parity true, process stopped
  true, Historical loaded in caller false.
- Full normally configured suite: 2814/2814 PASS, zero failed or skipped cases.
  Its existing default configuration excludes SelfAnalysis; no test settings
  were changed and no separate SelfAnalysis execution is claimed.
- Staged whitespace check and test-file whitespace formatter verification: PASS.

An initial deeper export hit Windows PowerShell 5 path-length limitations during
source fingerprinting, before builds/tests. The unchanged gate passed after
exporting the same staged tree to a shorter path. No Linux build or remote rerun
is claimed; the original Linux failure was verified in the GitHub job logs.

Local evidence is retained under `artifacts/ci-selection-directory-fix`:
`fresh-export.json`, `fresh-dual-build-final.log`, `target-no-directory.log`,
`no-directory-audit.json`, `no-directory-parity.json`, `fresh-full.log`, and
`tests/*.trx`. Gate audit and boundary TRX are under
`artifacts/sd-bd2ceac4/Tools/XMLDocNormalizer/artifacts/dual-version-build`.
The initial long-path attempt logs are retained as well.

Only this report and the test fix are included in the fix commit. The root
`.gitignore` WIP, all six stashes, and 395 snapshotted Main/Core files (including
the original Core build outputs) were verified unchanged. No push is performed.
