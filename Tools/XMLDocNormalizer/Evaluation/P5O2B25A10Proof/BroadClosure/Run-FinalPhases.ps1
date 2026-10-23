param([ValidatePattern('^[0-9]{2}$')][string]$Revision = '17')
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$validation = Join-Path $tool ('artifacts/b25a10-development-' + $Revision)
$broad = Get-Content (Join-Path $tool ('artifacts/p5o2b25a10/runs/development-' + $Revision + '-broad.json')) -Raw -Encoding UTF8 | ConvertFrom-Json
[xml]$trx = Get-Content (Join-Path $validation 'artifacts/a10-closure-results/broad.trx') -Raw -Encoding UTF8
if ($broad.ExitCode -ne 0 -or $trx.TestRun.ResultSummary.Counters.total -ne '3338' -or
    $trx.TestRun.ResultSummary.Counters.passed -ne '3338' -or $trx.TestRun.ResultSummary.Counters.notExecuted -ne '0') {
    throw 'Broad3338/3338 is required before final phases; no retry.'
}
foreach ($phase in @('Full','Boundary','Dual')) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot '../Run-Closure.ps1') -Phase $phase -Revision $Revision
    if ($LASTEXITCODE -ne 0) { throw ('STOP: ' + $phase + ' failed. Preserve process/run context; no blind retry or next phase.') }
}
