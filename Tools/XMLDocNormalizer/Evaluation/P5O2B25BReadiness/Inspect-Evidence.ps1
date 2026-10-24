$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$closure = Join-Path $tool 'Evaluation/P5O2B25A10Proof/FinalClosure'
$life = Get-Content (Join-Path $closure 'lifetime-closure.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$independent = Get-Content (Join-Path $closure 'independent-validation.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$destination = Join-Path $PSScriptRoot 'a10-evidence-verification.json'
if (Test-Path -LiteralPath $destination) { throw 'Never overwrite a completed review capture.' }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
$checks = [Collections.Generic.List[object]]::new()
function Bound([string]$id,[string]$path,[string]$expected) {
    $actual = Hash $path
    $checks.Add([ordered]@{Id=$id;Path=$path;ExpectedSha256=$expected;ActualSha256=$actual;Passed=$actual -ceq $expected})
}
Bound 'retained-independent-snapshot' (Join-Path $closure 'independent-validation.json') $life.ReusedIndependentValidationSha256
$suites = @(foreach ($pair in @(
    @('full-green','LifetimeResults/full.trx',3429,3429,0),
    @('multimodule-isolated','LifetimeResults/multimodule-first.trx',1,1,0),
    @('full-historical-red','../BroadClosure/Results-r17-independent/full.trx',3429,3428,1),
    @('broad','../BroadClosure/Results-r17-independent/broad.trx',3338,3338,0),
    @('boundary','../BroadClosure/Results-r17-independent/boundary.trx',596,596,0))) {
    $path = [IO.Path]::GetFullPath((Join-Path $closure $pair[1]))
    [xml]$trx = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $c=$trx.TestRun.ResultSummary.Counters
    $pass=[int]$c.total -eq $pair[2] -and [int]$c.passed -eq $pair[3] -and [int]$c.failed -eq $pair[4] -and [int]$c.notExecuted -eq 0
    $checks.Add([ordered]@{Id=('counters:'+$pair[0]);Passed=$pass})
    [ordered]@{Id=$pair[0];Path=$path;Sha256=(Hash $path);Total=[int]$c.total;Passed=[int]$c.passed;Failed=[int]$c.failed;Skipped=[int]$c.notExecuted}
})
Bound 'retained-full-green' (Join-Path $closure 'LifetimeResults/full.trx') $life.Full.Sha256
Bound 'retained-isolated' (Join-Path $closure 'LifetimeResults/multimodule-first.trx') $life.Isolated.Sha256
foreach ($gate in $life.Gates) {
    $path=Join-Path $closure ('LifetimeResults/'+$gate.Gate+'-gate.json')
    Bound ('gate:'+ $gate.Gate) $path $gate.ResultSha256
    $result=Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json
    $checks.Add([ordered]@{Id=('green-gate:'+$gate.Gate);Passed=$result.Passed -and @($result.Failures).Count -eq 0 -and $gate.ExitCode -eq 0})
}
foreach ($proof in $life.ReusedProofs) { Bound ('proof:'+$proof.Name) (Join-Path $closure ('IndependentResults/'+$proof.Name+'-proof.json')) $proof.ResultHash }
foreach ($step in $life.ReusedWae) { Bound ('WAE:'+$step.Name) (Join-Path $closure ('IndependentResults/dual-'+$step.Name+'.log')) $step.Sha256 }
$dual = Get-Content (Join-Path $closure 'IndependentResults/dual-audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checks.Add([ordered]@{Id='retained-Dual-PASS';Passed=[bool]$dual.Passed -and $independent.DualExitCode -eq 0})
foreach ($suite in $independent.DualSuites) { Bound ('dual-suite:'+$suite.Name) (Join-Path $closure ('IndependentResults/dual-'+$suite.Name)) $suite.Sha256 }
$inputs = Get-Content (Join-Path $closure 'LifetimeResults/lifetime-input-delta.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$sourceMismatches = @(foreach ($row in $inputs.Rows) { if ((Hash (Join-Path $tool $row.Path)) -cne $row.WorkspaceSha256) { $row.Path } })
$checks.Add([ordered]@{Id='current-source-inputs-equal-tested-A10';Passed=$sourceMismatches.Count -eq 0})
$fileRefs = @(foreach ($binding in $life.ReusedCurrentBaselineBindings) {
    [ordered]@{Name=$binding.Name;BeforeHash=$binding.BeforeHash;AfterHash=$binding.AfterHash;ByteEqual=$binding.ByteEqual;Assessment='Historical hash-comparison record retained, not re-executed; original ignored D: artifacts were deleted by user.'}
})
$record=[ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o');ReadOnly=$true;NoBuildOrTestExecuted=$true;A10HistoricalResultsNotRelabelledAsFresh=$true
    Passed=@($checks | Where-Object {!$_.Passed}).Count -eq 0;Checks=@($checks.ToArray());Suites=$suites
    SourceInputsChecked=$inputs.Rows.Count;CurrentSourceMismatches=$sourceMismatches;CurrentBaselines=$fileRefs
    RawArtifactPathsNotSilentlyRestored=$true;DeletedArtifactsDirectoryRecreated=$false
    ExistingGateScriptsAreNotReplayed='They bind old HEAD/index/absolute artifacts paths. Fresh reproduction needs a deliberate fresh export/cache setup, not rewritten historical snapshots.'
    A10CompletedByUserCommit=$true;B25BActivated=$false
}
[IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 14)+"`r`n"),[Text.UTF8Encoding]::new($false))
if (!$record.Passed) { throw 'A retained evidence check failed; preserve result and inspect before any readiness claim.' }
Write-Host ('PASS: '+$checks.Count+' static evidence/hash checks; '+$inputs.Rows.Count+' current inputs equal tested A10; no builds/tests.')
