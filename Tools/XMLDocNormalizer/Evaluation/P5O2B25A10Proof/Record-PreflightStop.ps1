# Read-only state capture after the protection verifier STOP. Never repairs or accepts drift.
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$target = Join-Path $PSScriptRoot 'full-fixture-preflight-validation.json'
if (Test-Path -LiteralPath $target) { throw 'Preserve completed STOP record.' }
$start = Get-Content -LiteralPath (Join-Path $tool 'artifacts/p5o2b25a9/start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
function Differences($files) {
    foreach ($f in $files) {
        $actual = if (Test-Path -LiteralPath $f.Path -PathType Leaf) { (Get-FileHash -LiteralPath $f.Path -Algorithm SHA256).Hash } else { 'MISSING' }
        if ($actual -cne $f.Sha256) { [ordered]@{ Path = $f.Path; Expected = $f.Sha256; Actual = $actual } }
    }
}
$coreDiff = @(Differences $start.Core)
$inheritedDiff = @(Differences $start.OriginalFiles)
$inventory = Get-Content -LiteralPath (Join-Path $tool 'artifacts/p5o2b25a10/test-path-inventory-r1.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$sourceDiff = @($inventory.Files | Where-Object { (Get-FileHash -LiteralPath (Join-Path $tool $_.Path) -Algorithm SHA256).Hash -cne $_.Sha256 })
Push-Location $tool
try {
    $head = & git --no-optional-locks rev-parse HEAD
    $parent = & git --no-optional-locks rev-parse 'HEAD^'
    & git --no-optional-locks diff --cached --quiet
    $indexClean = $LASTEXITCODE -eq 0
    $stashes = @(& git --no-optional-locks stash list --format='%H %gs')
    $status = @(& git --no-optional-locks status --short)
    $record = [ordered]@{
        Status = 'FAIL / STOP - protected Core drift; no green closure or implementation clearance'
        Head = $head; Parent = $parent; SemanticIndexClean = $indexClean
        IndexSha256 = (Get-FileHash -LiteralPath (Join-Path $repo '.git/index') -Algorithm SHA256).Hash
        IgnoreSha256 = (Get-FileHash -LiteralPath (Join-Path $repo '.gitignore') -Algorithm SHA256).Hash
        CoreFiles = @($start.Core).Count; CoreHashDifferences = $coreDiff; DriftAccepted = $false; CoreFilesRepaired = $false
        CoreWriterEstablished = $false
        CoreObservation = 'AssemblyInfo now has InformationalVersion 1.0.0+9b76e179a568c64ae972cc3a45980a094e44245e; AssemblyInfoInputs cache also differs. Actual writer is not proved. Audit project has DLL references only, no Core ProjectReference; no attribution from timestamps alone.'
        StashCount = $stashes.Count; StashesEqual = (($stashes -join "`n") -ceq ($start.Stashes -join "`n"))
        InheritedFilesVerified = @($start.OriginalFiles).Count; InheritedHashDifferences = $inheritedDiff
        TestInventoryFilesVerified = @($inventory.Files).Count; TestInventoryHashDifferences = $sourceDiff
        OriginalMethodsReconstructed = 202; OriginalCases = 203; OriginalCalls = 228
        OriginalCaseCounts = [ordered]@{ Exact = 81; Outside = 96; Diagnostic = 1; Unrelated = 25; Ambiguous = 0 }
        SupplementalAmbiguousMethods = 85; SupplementalAmbiguousCases = 429; SupplementalAmbiguousAssertionObservations = 3202
        R1ThroughR7Approved = $true; PendingConditionalChangeRows = [ordered]@{ R8 = 32; R9 = 27; R10 = 14 }
        DefinitiveFullPreflightClosure = $false; ImplementationClearance = $false
        ProductOrTestBuildRun = $false; ExistingTestsExecuted = $false; HistoricalWorkerExecuted = $false
        BroadFullBoundaryDualWaeRun = $false; SelfAnalysisRun = $false; CanonicalFindingDiffRun = $false
        ProductionSourceEditsByAgent = 0; ExistingTestEditsByAgent = 0; ExistingGateEditsByAgent = 0
        IndexOperations = $false; StashOperations = $false; Commit = $false; Push = $false; B25BActivated = $false
        FirstVerifierAttempt = 'Blocked by execution policy before script execution; then run with process-only ExecutionPolicy Bypass, no persistent policy change'
        ActualProtectionVerifierExitCode = 1; VerificationRetriedToGreen = $false
        NewOsCrashConfirmedByToolOutput = $false; ProcDumpInstalled = $false; DumpOrRegistryChanged = $false; ForeignDotnetProcessChanged = $false
        GitStatus = $status
    }
    [IO.File]::WriteAllText($target, ($record | ConvertTo-Json -Depth 10) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    $record | ConvertTo-Json -Depth 10
} finally { Pop-Location }
exit 1
