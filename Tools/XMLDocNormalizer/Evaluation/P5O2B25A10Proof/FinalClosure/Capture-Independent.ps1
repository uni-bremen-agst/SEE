$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$validation = Join-Path $tool 'artifacts/b25a10-development-17'
$records = Join-Path $tool 'artifacts/p5o2b25a10/final-r17'
$destination = Join-Path $PSScriptRoot 'independent-validation.json'
$copies = Join-Path $PSScriptRoot 'IndependentResults'
if ((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath $copies)) { throw 'Preserve completed evidence; no overwrite.' }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
function Json([string]$path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
$gates = @(foreach ($gate in @('a6','a7','a8','a9','a10')) {
    $path = Join-Path $records ($gate + '-gate.json')
    $result = Json $path
    $run = Json (Join-Path $tool ('artifacts/p5o2b25a10/runs/development-17-' + $gate + '-gate.json'))
    [ordered]@{ Gate=$gate; Passed=$result.Passed; ExitCode=$run.ExitCode; Failures=$result.Failures; ResultHash=(Hash $path) }
})
$dual = Json (Join-Path $validation 'artifacts/dual-version-build/audit.json')
$dualRun = Json (Join-Path $tool 'artifacts/p5o2b25a10/runs/development-17-dual.json')
$wae = @(foreach ($name in @('0-current-build','1-historical-build','2-current-build','3-historical-build','4-historical-build','5-current-build','worker-0-build','worker-1-build','boundary-test-process-build','projection-pdb-fixture-build','main-boundary-tests-build')) {
    $path = Join-Path $validation ('artifacts/dual-version-build/' + $name + '.log')
    $log = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $clean = $log -match '(?m)^\s+0 (Warnung\(en\)|Warning\(s\))\s*$' -and $log -match '(?m)^\s+0 (Fehler|Error\(s\))\s*$'
    $fresh = (Get-Item -LiteralPath $path).LastWriteTimeUtc -ge [DateTimeOffset]::Parse($dualRun.StartedUtc).UtcDateTime
    [ordered]@{ Name=$name; Clean=$clean; Fresh=$fresh; Path=$path; Sha256=(Hash $path) }
})
$baselines = @(Get-ChildItem (Join-Path $tool 'artifacts/p5o2b25a9/baseline/current') -Filter '*.json' | ForEach-Object {
    $after = Join-Path $validation ('artifacts/p5o2b9/current/' + $_.Name)
    $beforeHash = Hash $_.FullName; $afterHash=Hash $after
    [ordered]@{Name=$_.Name;BeforeHash=$beforeHash;AfterHash=$afterHash;ByteEqual=$beforeHash -ceq $afterHash}
})
$proofs = @(foreach ($name in @('a8','a9','production')) {
    $path = Join-Path $records ($name + '-proof.json'); $proof=Json $path
    [ordered]@{Name=$name;Checks=$proof.Checks.Count;Failed=@($proof.Checks | Where-Object { !$_.Passed }).Count;ResultHash=(Hash $path)}
})
$suites = @(foreach ($file in Get-ChildItem (Join-Path $validation 'artifacts/dual-version-build') -Filter '*.trx') {
    [xml]$trx=Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    [ordered]@{Name=$file.Name;Total=[int]$trx.TestRun.ResultSummary.Counters.total;Passed=[int]$trx.TestRun.ResultSummary.Counters.passed;Failed=[int]$trx.TestRun.ResultSummary.Counters.failed;Skipped=[int]$trx.TestRun.ResultSummary.Counters.notExecuted;Sha256=(Hash $file.FullName)}
})
$compiled = Json (Join-Path $records 'compiled-source-verification.json')
[IO.Directory]::CreateDirectory($copies) | Out-Null
foreach ($file in Get-ChildItem $records -File) { [IO.File]::Copy($file.FullName,(Join-Path $copies $file.Name),$false) }
foreach ($file in Get-ChildItem (Join-Path $validation 'artifacts/dual-version-build') -File) {
    [IO.File]::Copy($file.FullName,(Join-Path $copies ('dual-' + $file.Name)),$false)
}
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Status='Independent gates executed; Full foreign fixture remains red'
    DualPassed=[bool]$dual.Passed; DualExitCode=$dualRun.ExitCode; DualSuites=$suites
    Wae=$wae; CurrentBaselines=$baselines; Proofs=$proofs; Gates=$gates
    CompiledSourceBindingsPassed=$compiled.Passed; PePdbImageBindings=$compiled.ImageBindings.Count
    BoundPdbDocuments=@($compiled.ImageBindings | ForEach-Object Documents).Count
    HistoricalGateScriptsRewritten=$false; HistoricalGateAuditsRewritten=$false; A10OwnedAdaptersExplicitlyUsed=$true
    FullFailureWaived=$false; FullRetried=$false; OverallPassed=$false; A10Complete=$false
    PreviousNativeCrashStillUnattributed=$true; NativeCrashObservedThisContinuation=$false
    SourceOrExistingTestAssertionsChangedAfterAwaitApproval=$false; B25BActivated=$false; Commit=$false; Push=$false
    SelfAnalysisFresh=$false; CanonicalFindingDiffFresh=$false
}
[IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 25)+"`r`n"),[Text.UTF8Encoding]::new($false))
if (!$dual.Passed -or $wae.Count -ne 11 -or @($wae | Where-Object { !$_.Clean -or !$_.Fresh }).Count -or $baselines.Count -ne 8 -or @($baselines | Where-Object { !$_.ByteEqual }).Count) { throw 'Independent validation inconsistency; preserve actual evidence.' }
Write-Host 'Dual PASS; WAE11 clean/fresh; Current8 byte-equal; proofs40/37/30 pass. A6/A8/A9/A10 remain red only for Full; A7 static contract PASS. No Full waiver/retry.'
