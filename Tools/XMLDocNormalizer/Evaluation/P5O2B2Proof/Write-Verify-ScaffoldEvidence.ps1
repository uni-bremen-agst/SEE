$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition, [string]$Message) {
        if (-not $Condition) { throw $Message }
    }
    $initial = Get-Content 'artifacts/p5o2b2/protected-before.json' -Raw -Encoding UTF8 | ConvertFrom-Json
    $gate = Get-Content 'artifacts/dual-version-build/audit.json' -Raw -Encoding UTF8 | ConvertFrom-Json
    $a2Path = 'Evaluation/P5O2B1A2-runtime-await-call-site-contract-audit.json'
    $a2 = Get-Content $a2Path -Raw -Encoding UTF8 | ConvertFrom-Json
    Require ($gate.Passed -and $gate.BuildImages.Count -eq 6) 'Incomplete dual-build evidence.'
    Require ((git rev-parse HEAD) -eq $initial.Head) 'HEAD changed during B2.'
    Require ((Get-FileHash '../../.gitignore' -Algorithm SHA256).Hash -eq $initial.IgnoreSha256) 'Protected root ignore changed.'
    Require ((Get-FileHash 'src/XMLDocNormalizer/XMLDocNormalizer.csproj').Hash -eq $initial.MainProjectSha256) 'Current project changed.'
    Require ((Get-FileHash 'XMLDocNormalizer.sln').Hash -eq $initial.SolutionSha256) 'Normal solution changed.'
    Require ((@(git stash list --format='%H %gs') -join "`n") -eq ($initial.Stashes -join "`n")) 'Stashes changed.'
    $coreFiles = @(Get-ChildItem 'src/XMLDocNormalizer.ExceptionFlow.Core' -File -Recurse | Where-Object { $_.FullName -match '[\\/](bin|obj)[\\/]' })
    Require ($coreFiles.Count -eq $initial.CoreFiles.Count) 'Core output file set changed.'
    foreach ($file in $initial.CoreFiles) {
        Require ((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -eq $file.Sha256) "Protected Core file changed: $($file.Path)"
    }
    $productive = @($a2.OriginalMainSourceFingerprints) + @($a2.ApiSurface.ProductiveFiles)
    foreach ($file in $productive) {
        Require ((Get-FileHash -LiteralPath $file.File -Algorithm SHA256).Hash -eq $file.Sha256) "A2 productive source changed: $($file.File)"
    }
    $productiveCount = @($productive.File | Sort-Object -Unique).Count
    Require ($productiveCount -eq 362) 'Expected all 362 unchanged Current productive files.'
    $manifestSources = @($gate.SharedSourceFingerprints.Path | Sort-Object)
    Require (($manifestSources -join ';') -eq (($a2.ApiSurface.ProductiveFiles.File | Sort-Object) -join ';')) 'Historical boundary differs from A2.'
    $tests = @('build-architecture', 'full') | ForEach-Object {
        $path = "artifacts/p5o2b2/tests/$_.trx"
        [xml]$trx = Get-Content -LiteralPath $path -Raw
        $counters = $trx.TestRun.ResultSummary.Counters
        Require ([int]$counters.failed -eq 0 -and [int]$counters.total -eq [int]$counters.passed) "Test gate failed: $path"
        [ordered]@{ Name = $_; Total = [int]$counters.total; Passed = [int]$counters.passed; Failed = [int]$counters.failed; Artifact = $path; Sha256 = (Get-FileHash $path).Hash }
    }
    Require ($tests[0].Total -eq 72 -and $tests[1].Total -eq 2608) 'Unexpected test counts.'
    $logs = @($gate.Steps | ForEach-Object {
        $path = 'artifacts/dual-version-build/' + $_.Name + '.log'
        $text = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ($_.ExitCode -eq 0) "Failed build step: $($_.Name)"
        if ($_.Name.EndsWith('-build')) {
            Require ($text -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $text -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Build is not warning/error free: $path"
        }
        [ordered]@{ Name = $_.Name; Artifact = $path; Sha256 = (Get-FileHash $path).Hash }
    })
    $result = [ordered]@{
        Schema = 'P5O2B2-scaffold-audit-v1'; Date = '2026-10-08'; Head = $initial.Head
        Decision = 'COMPLETE: permanent reproducible Current/Historical dual-version build available; no historical runtime claim'
        Scope = 'Build infrastructure only; all 362 Current productive C# files and all 121 Historical productive files unchanged from A2'
        Projects = [ordered]@{
            Current = 'src/XMLDocNormalizer/XMLDocNormalizer.csproj'
            Historical = 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj'
            Manifest = 'build/ExceptionFlow.HistoricalSources.props'
            HistoricalInSolution = $false; RuntimeProjectReferences = @()
            BuildOnlyHost = 'src/XMLDocNormalizer.ExceptionFlow.Historical/BuildOnlySemanticEnvironment.cs'
            BuildOnlyHostExplanation = 'Permanent compile-local five-member host contract plus rejecting constructor, not an executable semantic host. A2 explicitly defers the real host to later authorized runtime work.'
        }
        DualBuildGate = $gate; BuildLogFingerprints = $logs; Tests = @($tests)
        CurrentSemantics = [ordered]@{
            A2Evidence = $a2Path; A2EvidenceSha256 = (Get-FileHash $a2Path).Hash
            ProductiveFilesReverified = $productiveCount; ProductiveFilesUnchanged = $true
            FullSuite = 'Fresh 2608/2608 = previous 2600 + eight project-contract tests'
            SelfAnalysis = [ordered]@{ Inherited = $true; Findings = 16; DOC610 = 0; DOC611 = 1; DOC631 = 15; DOC632 = 0; Evidence = $a2.Validation.SelfAnalysis }
            Canonical = [ordered]@{ Inherited = $true; Added = $a2.Validation.Canonical.Added; Removed = $a2.Validation.Canonical.Removed; ChangedEvidence = $a2.Validation.Canonical.ChangedEvidence; FullRawArraysEqual = $a2.Validation.Canonical.FullRawArraysEqual; FullNormalizedArraysEqual = $a2.Validation.Canonical.FullNormalizedArraysEqual; BeforeNormalizedHash = $a2.Validation.Canonical.BeforeNormalizedHash; AfterNormalizedHash = $a2.Validation.Canonical.AfterNormalizedHash }
            ReasonForInheritance = 'No productive Current analyzer source/project/solution change; build-only host is outside Current and normal solution, non-executable and contains no analyzer semantics. B2 prompt section 17 permits inheritance.'
        }
        Integration = [ordered]@{
            RegressionGate = 'build/Verify-DualVersionBuild.ps1'
            CiWorkflow = '../../.github/workflows/xml-doc-normalizer-dual-build.yml'
            LocalGateExecuted = $true; RemoteCiExecuted = $false
            CurrentProjectAndSolutionUnchanged = $true
            IgnoreExceptions = @('src/XMLDocNormalizer.ExceptionFlow.Historical/.gitignore: one project only', '.gitignore: three named build files only')
            PreviousProbes = 'Retained unchanged; compile regression superseded, provenance/API/call-site evidence still authoritative'
        }
        LocalWiringCorrections = @('Resolve Current default package cache from assets instead of empty RestorePackagesPath', 'Canonicalize HistoricalArtifactsRoot before SDK props: unnormalized RAR cache path 260 characters, canonical 208; serial repeated WAE builds confirm fix', 'Add narrow tool-local exceptions for three files in globally ignored build directory; protected root ignore unchanged')
        FailedAttempts = 'Initial audit-path assumption failed, then MSB3101 persisted in unnormalized Windows cache path. A concurrent diagnostic was also attempted and excluded. No failed series is counted; final serial six-step series passes, including clean-image removal.'
        RemainingBoundary = 'No historical execution, no real semantic host, no worker/IPC/ALC/Main runtime integration. Existing external fail-closed pipeline unchanged.'
        NextStep = 'P5O2B3 - isolated historical runtime/worker host boundary: supply real compile-local semantic capabilities in one historical Roslyn universe and prove bounded execution/canonical-only handoff; do not repeat compile feasibility analysis'
        Protected = [ordered]@{ RootIgnore = $initial.IgnoreSha256; CoreFileCount = $initial.CoreFiles.Count; CoreFiles = $initial.CoreFiles; Stashes = $initial.Stashes; Unchanged = $true }
        Git = [ordered]@{ Status = @(git status --short); Commit = $false; Push = $false; IndexChanged = $false; StashChanged = $false }
    }
    $result | ConvertTo-Json -Depth 20 | Set-Content 'artifacts/p5o2b2/scaffold-audit.json' -Encoding UTF8
    Write-Host "PASS: source fingerprints $productiveCount; dual-build steps 6; tests 72/2608; protected Core files $($initial.CoreFiles.Count); A2 self/canonical explicitly inherited."
} finally {
    Pop-Location
}
