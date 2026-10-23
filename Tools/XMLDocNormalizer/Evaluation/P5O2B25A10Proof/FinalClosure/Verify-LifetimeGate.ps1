param([Parameter(Mandatory=$true)][ValidateSet('A6','A7','A8','A9','A10')][string]$Gate)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$validation = (Get-Content (Join-Path $PSScriptRoot 'lifetime-validation-location-short.json') -Raw | ConvertFrom-Json).ValidationRoot
$records = Join-Path $tool 'artifacts/p5o2b25a10/final-r18'
$previousRecords = Join-Path $tool 'artifacts/p5o2b25a10/final-r17'
$previousValidation = Join-Path $tool 'artifacts/b25a10-development-17'
$old = Get-Content (Join-Path $previousRecords ($Gate.ToLowerInvariant() + '-gate.json')) -Raw | ConvertFrom-Json
if (@($old.Failures | Where-Object Name -ne 'required-runtime:full').Count -ne 0) { throw 'Previous gate had an independent failure: STOP.' }
$deltaPath = Join-Path $records 'lifetime-input-delta.json'
$delta = Get-Content $deltaPath -Raw | ConvertFrom-Json
if (!$delta.ExactAuthorizedInsertionOnly -or !$delta.BothExistingAssertionsVerbatimUnchanged -or $delta.ProductionFilesChanged -ne 0) { throw 'Not an isolated lifetime-only delta.' }
foreach ($row in $delta.Rows) {
    if ((Get-FileHash (Join-Path $tool $row.Path)).Hash -cne $row.WorkspaceSha256 -or
        (Get-FileHash (Join-Path $validation $row.Path)).Hash -cne $row.ExecutedExportSha256 -or
        (Get-FileHash (Join-Path $previousValidation $row.Path)).Hash -cne $row.PreviousSha256) { throw ('Reuse binding changed: ' + $row.Path) }
}
foreach ($snapshot in $delta.PreservedSnapshots) {
    if ((Get-FileHash (Join-Path $tool $snapshot.Path)).Hash -cne $snapshot.Sha256) { throw ('Historical snapshot changed: ' + $snapshot.Path) }
}
foreach ($run in $delta.ReusedRuntimeEvidence) {
    if ((Get-FileHash $run.RunPath).Hash -cne $run.Sha256) { throw 'Reused runtime record changed.' }
}
[xml]$full = Get-Content (Join-Path $validation 'artifacts/a10-closure-results/full.trx') -Raw
if ($full.TestRun.ResultSummary.Counters.total -ne '3429' -or $full.TestRun.ResultSummary.Counters.passed -ne '3429' -or $full.TestRun.ResultSummary.Counters.notExecuted -ne '0') { throw 'Full must pass 3429/3429 without skips.' }
# Preserve and reuse the existing A10-owned adapter, not historical gates.
# Full, compiled inputs and A6 witnesses are fresh; unchanged independent
# proofs and runtime suites are explicitly reused from export17.
$source = Get-Content (Join-Path $PSScriptRoot 'Verify-Gate.ps1') -Raw
$source = $source.Replace("`$tool = [IO.Path]::GetFullPath((Join-Path `$PSScriptRoot '../../..'))", ("`$tool = '" + $tool + "'"))
$source = $source.Replace("`$validation = Join-Path `$tool 'artifacts/b25a10-development-17'", ("`$validation = '" + $validation + "'"))
$source = $source.Replace("`$records = Join-Path `$tool 'artifacts/p5o2b25a10/final-r17'", ("`$records = '" + $records + "'"))
$source = $source.Replace("Join-Path `$tool 'artifacts/p5o2b25a10/runs/development-17-full.json'", ("'" + (Join-Path $validation 'artifacts/p5o2b25a10/runs/development-18-full.json') + "'"))
$oldRun = "`$run = Json (Join-Path `$tool ('artifacts/p5o2b25a10/runs/development-17-' + `$phase + '.json'))"
$newRun = "`$runPath = if (`$phase -eq 'full') { '" + (Join-Path $validation 'artifacts/p5o2b25a10/runs/development-18-full.json') + "' } else { Join-Path `$tool ('artifacts/p5o2b25a10/runs/development-17-' + `$phase + '.json') }; `$run = Json `$runPath"
$source = $source.Replace($oldRun,$newRun)
$source = $source.Replace("Join-Path `$validation 'artifacts/dual-version-build/audit.json'", ("Join-Path '" + $previousValidation + "' 'artifacts/dual-version-build/audit.json'"))
$source = $source.Replace("Join-Path `$validation ('artifacts/dual-version-build/", ("Join-Path '" + $previousValidation + "' ('artifacts/dual-version-build/"))
$source = $source.Replace("Join-Path `$validation ('artifacts/p5o2b9/current/", ("Join-Path '" + $previousValidation + "' ('artifacts/p5o2b9/current/"))
foreach ($file in @('proof-inputs.json','a8-proof.json','a9-proof.json','production-proof.json')) {
    $source = $source.Replace(("Join-Path `$records '" + $file + "'"), ("Join-Path '" + $previousRecords + "' '" + $file + "'"))
}
$source = $source.Replace('FullRemainsFailed=$true','FullRemainsFailed=$false')
$source = $source.Replace('A10Complete=$false', 'A10Complete=$false; ClosureGatePassedOnly=$true; FreshFullRevision=18; ReusedIndependentEvidenceRevision=17; LifetimeOnlySourceDeltaVerified=$true')
$source = $source.Replace('Full failure is not waived.', 'Fresh Full3429/3429; unchanged independent export17 evidence is explicitly reused, not reexecuted.')
& ([scriptblock]::Create($source)) -Gate $Gate
exit $LASTEXITCODE
