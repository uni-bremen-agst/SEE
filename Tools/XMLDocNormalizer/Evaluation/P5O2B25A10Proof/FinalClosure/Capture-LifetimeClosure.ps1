$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$validation = (Get-Content (Join-Path $PSScriptRoot 'lifetime-validation-location-short.json') -Raw | ConvertFrom-Json).ValidationRoot
$records = Join-Path $tool 'artifacts/p5o2b25a10/final-r18'
$destination = Join-Path $PSScriptRoot 'lifetime-closure.json'
$copies = Join-Path $PSScriptRoot 'LifetimeResults'
if ((Test-Path $destination) -or (Test-Path $copies)) { throw 'Never overwrite a completed capture.' }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
function Json([string]$path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
$protection = Json (Join-Path $tool 'Evaluation/P5O2B25A10Proof/implementation-broad-root-cause-state.json')
$core = @(foreach ($file in $protection.Core) {
    $actual = Hash $file.Path
    [ordered]@{Path=$file.Path;FrozenSha256=$file.Frozen;AcceptedS1OrFrozenSha256=$file.Actual;ActualSha256=$actual;NewDrift=$actual -cne $file.Actual}
})
$historical = @(foreach ($file in $protection.HistoricalEvidence) {
    $actual = Hash (Join-Path $tool $file.Path)
    [ordered]@{Path=$file.Path;FrozenSha256=$file.Frozen;ActualSha256=$actual;Changed=$actual -cne $file.Frozen}
})
$index = Hash (Join-Path $repo '.git/index')
$ignore = Hash (Join-Path $repo '.gitignore')
$stashes = @(& git --no-optional-locks -C $repo stash list --format='%H %gs')
& git --no-optional-locks -C $repo diff --cached --quiet
$semanticIndexClean = $LASTEXITCODE -eq 0
if (@($core | Where-Object NewDrift).Count -or @($historical | Where-Object Changed).Count -or $index -cne $protection.IndexSha256 -or $ignore -cne $protection.RootIgnoreSha256 -or !$semanticIndexClean -or ($stashes -join "`n") -cne ($protection.Stashes -join "`n")) { throw 'New protected drift: STOP, do not repair.' }
$gates = @(foreach ($gate in @('a6','a7','a8','a9','a10')) {
    $path = Join-Path $records ($gate + '-gate.json')
    $result = Json $path
    $run = Json (Join-Path $validation ('artifacts/p5o2b25a10/runs/development-18-' + $gate + '-gate.json'))
    if (!$result.Passed -or $null -eq $run.ExitCode -or $run.ExitCode -ne 0) { throw ('Gate not green: ' + $gate) }
    [ordered]@{Gate=$gate;Passed=$result.Passed;ExitCode=$run.ExitCode;Checks=$result.Checks.Count;ResultSha256=(Hash $path)}
})
$runs = @(Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a10/runs') -Filter 'development-18-*.json' | ForEach-Object {
    $run = Json $_.FullName
    if ($null -eq $run.ExitCode -or $run.ExitCode -ne 0) { throw ('Unfinished/failed recorded command: ' + $_.Name) }
    $run
})
$results = @(foreach ($name in @('multimodule-first','full')) {
    $file = Join-Path $validation ('artifacts/a10-closure-results/' + $name + '.trx')
    [xml]$trx = Get-Content $file -Raw -Encoding UTF8
    $c = $trx.TestRun.ResultSummary.Counters
    if ($c.failed -ne '0' -or $c.notExecuted -ne '0') { throw 'Non-green TRX: STOP.' }
    [ordered]@{Name=$name;Path=$file;Sha256=(Hash $file);Total=[int]$c.total;Passed=[int]$c.passed;Failed=[int]$c.failed;Skipped=[int]$c.notExecuted}
})
$compiled = Json (Join-Path $records 'compiled-source-verification.json')
$delta = Json (Join-Path $records 'lifetime-input-delta.json')
$previous = Json (Join-Path $PSScriptRoot 'independent-validation.json')
[xml]$fullTrx = Get-Content $results[1].Path -Raw -Encoding UTF8
$fullClasses = @(foreach ($name in @('ExceptionFlowGenericExpansionBoundaryTests','ExceptionFlowRecursiveSummaryClosureTests','ExceptionFlowFiniteOrbitAdmissionTests','ExternalMetadataReferenceFactoryTests')) {
    $members = @($fullTrx.TestRun.Results.UnitTestResult | Where-Object testName -like ('*.' + $name + '.*'))
    [ordered]@{Class=$name;Total=$members.Count;Passed=@($members | Where-Object outcome -eq 'Passed').Count}
})
[IO.Directory]::CreateDirectory($copies) | Out-Null
foreach ($file in Get-ChildItem $records -File) { [IO.File]::Copy($file.FullName,(Join-Path $copies $file.Name),$false) }
foreach ($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a10/runs') -File) { [IO.File]::Copy($file.FullName,(Join-Path $copies $file.Name),$false) }
foreach ($result in $results) { [IO.File]::Copy($result.Path,(Join-Path $copies ($result.Name + '.trx')),$false) }
$witnesses = @(Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a10/analysis/a6') -Filter '*.json')
foreach ($file in $witnesses) { [IO.File]::Copy($file.FullName,(Join-Path $copies ('a6-witness-' + $file.Name)),$false) }
$status = @(& git --no-optional-locks -C $tool status --short)
$record = [ordered]@{
    Package='P5O2B25A10';CapturedUtc=[DateTime]::UtcNow.ToString('o');Status='Approved isolated test-harness lifetime repair validated; Full and closure adapters green'
    Head=(& git --no-optional-locks -C $tool rev-parse HEAD);ValidationRoot=$validation
    Isolated=$results[0];Full=$results[1];FreshFullClasses=$fullClasses;FullExecutedExactlyOnceAfterRepair=$true;TestsRetriedUnchanged=$false
    PriorFull3428Of3429Preserved=$true;PriorGatesPreserved=$true;AssertionsVerbatimUnchanged=$delta.BothExistingAssertionsVerbatimUnchanged
    ProductionFilesChangedThisContinuation=$delta.ProductionFilesChanged;SourceDeltaCount=@($delta.Rows | Where-Object Changed).Count
    Gates=$gates;A7ConsistencyConfirmed=$true;AdapterScope='Same A10-owned obligations as export17; only Full and PE/PDB inputs refreshed, independent unchanged evidence explicitly reused'
    ReusedIndependentValidationPath='Evaluation/P5O2B25A10Proof/FinalClosure/independent-validation.json'
    ReusedIndependentValidationSha256=(Hash (Join-Path $PSScriptRoot 'independent-validation.json'))
    ReusedBroad='3338/3338';ReusedBoundary='596/596';ReusedDualSuites='800/800 across nine suites';ReusedWaeSteps=11;ReusedCurrentBaselines=8
    ReusedA8Proof='40/40';ReusedA9Proof='37/37';ReusedAdditionalActualProductionQueries='30/30'
    ReusedProofs=$previous.Proofs;ReusedWae=$previous.Wae;ReusedCurrentBaselineBindings=$previous.CurrentBaselines
    FreshCompiledBindingsPassed=$compiled.Passed;FreshPePdbPairs=$compiled.ImageBindings.Count;FreshBoundDocuments=@($compiled.ImageBindings | ForEach-Object Documents).Count
    Core=$core;FrozenHistoricalEvidence=$historical;PhysicalIndexSha256=$index;PhysicalIndexUnchanged=$true;SemanticIndexClean=$semanticIndexClean;RootIgnoreSha256=$ignore;RootIgnoreUnchanged=$true;Stashes=$stashes;StashesUnchanged=$true
    HistoricalS1Exit1Preserved=$true;ProtectionManifestOrBaselineChanged=$false;GitStatusShort=$status;RecordedCommands=$runs
    PreviousNativeCliCrash='0xC0000005, unattributed; no recurrence observed, not labelled flakiness or fixed'
    NativeCrashObservedThisContinuation=$false;SelfAnalysisFresh=$false;CanonicalFindingDiffFresh=$false
    B25BActivated=$false;Commit=$false;Push=$false;StashOperations=$false;IndexOperations=$false
}
[IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 18)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host 'Captured green Full, closure gates, explicit evidence reuse and unchanged protection state; historical red Full retained.'
