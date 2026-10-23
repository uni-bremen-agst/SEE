$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Json([string]$path) { Get-Content -LiteralPath (Join-Path $tool $path) -Raw -Encoding UTF8 | ConvertFrom-Json }
$resultPath = Join-Path $PSScriptRoot 'full-fixture-preflight-validation.json'
Require (!(Test-Path -LiteralPath $resultPath)) 'Preserve first completed verification record.'
$protection = (& (Join-Path $PSScriptRoot 'Verify-Phase0.ps1') | Out-String | ConvertFrom-Json)
Require ($protection.Status -ceq 'PASS - protection only, not implementation closure') 'Protection verification did not pass.'
$manifest = Json 'Evaluation/P5O2B25A10Proof/Complete-fixture-preflight.json'
$original = Json 'artifacts/p5o2b25a10/production-finding-probe.json'
$inventory = Json 'artifacts/p5o2b25a10/test-path-inventory-r1.json'
$audit = Json 'Evaluation/P5O2B25A10-finite-orbit-admission-and-projection-audit.json'
Require ($manifest.Revision -eq 4 -and $manifest.OriginalExcludedMethods -eq 202) 'Unexpected manifest revision/count.'
Require ($manifest.UnreconstructedOriginalFixtures -eq 0 -and $manifest.OriginalExcludedCases -eq 203 -and $manifest.OriginalExcludedCalls -eq 228) 'Original reconstruction not complete.'
Require (!$manifest.FullFixturePreflightCompleted -and !$manifest.ImplementationClearance) 'Ambiguous extended bindings must not imply implementation clearance.'
Require ($manifest.SupplementalAmbiguousMethods -eq 85 -and $manifest.SupplementalAmbiguousParameterCases -eq 429 -and $manifest.SupplementalAmbiguousAssertionObservations -eq 3202) 'Ambiguity inventory changed.'
Require (@($inventory.Files).Count -eq 340 -and @($inventory.Rows).Count -eq 2194) 'Source inventory counts changed.'
foreach ($file in $inventory.Files) {
    Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path) -Algorithm SHA256).Hash -ceq $file.Sha256) ('Source inventory changed: ' + $file.Path)
}
$originalNames = @($original.SkippedFixtures.Test)
$cases = @($manifest.SemanticCases | Where-Object { $_.Fixture.Test -cin $originalNames })
Require (@($cases.Fixture.Test | Sort-Object -Unique).Count -eq 202 -and $cases.Count -eq 203) 'Original methods/cases lost.'
$counts = [ordered]@{ Exact = 0; Outside = 0; Diagnostic = 0; Unrelated = 0; Ambiguous = 0 }
foreach ($case in $cases) {
    $classes = @($case.Classification.Classification)
    Require ($classes.Count -gt 0 -and @($case.Calls).Count -gt 0) ('Original fixture not bound: ' + $case.Fixture.Id)
    if ($classes -contains 'AMBIGUOUS') { $counts.Ambiguous++ }
    elseif ($classes -contains 'OUTSIDE PROFILE') { $counts.Outside++ }
    elseif ($classes -contains 'EXACT / admitted') { $counts.Exact++ }
    elseif ($classes -contains 'diagnostic / low-level') { $counts.Diagnostic++ }
    else { Require (@($classes | Where-Object { $_ -cne 'unrelated' }).Count -eq 0) 'Unknown fixture category.'; $counts.Unrelated++ }
}
Require ($counts.Exact -eq 81 -and $counts.Outside -eq 96 -and $counts.Diagnostic -eq 1 -and $counts.Unrelated -eq 25 -and $counts.Ambiguous -eq 0) 'Original case classification sums differ.'
foreach ($group in @(@('R7',32,$true), @('R8',32,$false), @('R9',27,$false), @('R10',14,$false))) {
    $rows = @($manifest.Changes | Where-Object Group -ceq $group[0])
    Require ($rows.Count -eq $group[1] -and @($rows | Where-Object Approved -ne $group[2]).Count -eq 0) ('Wrong approval coverage: ' + $group[0])
}
# The same source line contains several assertions in these two cases: capture the exact code comparison, NOT Assert.False.
foreach ($row in @($manifest.Changes | Where-Object { $_.Group -ceq 'R10' -and ($_.Case -like '*ProductiveDetector_Genuine*' -or $_.Case -like '*LocalModes_DoNotWiden*') })) {
    Require ($row.Assertions[0].Current -cmatch '^Assert\.Equal\(WorkerFailureCode\.AnalysisFailure,') 'Wrong same-line R10 assertion.'
}
Require (!$audit.ProductionImplementationStarted -and !$audit.B25BActivated -and !$audit.SolutionTransitiveFindingAdmission) 'Scope changed.'
$proofFiles = @(Get-ChildItem -LiteralPath $PSScriptRoot -File | Where-Object { $_.Name -ne 'full-fixture-preflight-validation.json' } | Sort-Object Name | ForEach-Object {
    [ordered]@{ Name = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$record = [ordered]@{
    Status = 'PASS - original reconstruction, evidence consistency and protection ONLY; extended AMBIGUOUS bindings remain'
    Protection = $protection; OriginalMethods = 202; OriginalCases = 203; OriginalCalls = 228; OriginalCaseCounts = $counts
    SourceInventoryFilesVerified = 340; AttributedMethodsIndexed = 2194
    ExpandedMethods = 438; ExpandedCases = 464; ExpandedCalls = 527
    R1ToR7Approved = $true; ConditionalPendingChanges = [ordered]@{ R8 = 32; R9 = 27; R10 = 14 }
    SupplementalAmbiguousMethods = 85; SupplementalAmbiguousCases = 429; SupplementalAmbiguousAssertionObservations = 3202
    DefinitiveFullPreflightClosure = $false; ImplementationClearance = $false
    ProductOrTestBuildRun = $false; ExistingTestsExecuted = $false; HistoricalWorkerExecuted = $false
    BroadFullBoundaryDualWaeRun = $false; SelfAnalysisRun = $false; CanonicalFindingDiffRun = $false
    ProductionFilesChanged = 0; TestFilesChanged = 0; ExistingGateFilesChanged = 0
    IndexOperations = $false; StashOperations = $false; Commit = $false; Push = $false; B25BActivated = $false
    NewOsCrashConfirmedByToolOutput = $false; CrashDumpOrRegistryChanges = $false; ForeignDotnetProcessChanges = $false
    ProofFiles = $proofFiles
}
[IO.File]::WriteAllText($resultPath, ($record | ConvertTo-Json -Depth 10) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
$record | ConvertTo-Json -Depth 10
