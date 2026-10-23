$ErrorActionPreference = 'Stop'
$validation = (Get-Content (Join-Path $PSScriptRoot 'lifetime-validation-location-short.json') -Raw | ConvertFrom-Json).ValidationRoot
$runner = Join-Path $validation 'Evaluation/P5O2B25A10Proof/Run-Recorded.ps1'
$full = Get-Content (Join-Path $validation 'artifacts/p5o2b25a10/runs/development-18-full.json') -Raw | ConvertFrom-Json
if ($null -eq $full.ExitCode -or $full.ExitCode -ne 0) { throw 'Do not run closure gates before successful Full.' }
foreach ($gate in @('A6','A7','A8','A9','A10')) {
    $name = 'development-18-' + $gate.ToLowerInvariant() + '-gate'
    $script = Join-Path $PSScriptRoot 'Verify-LifetimeGate.ps1'
    $command = "& '$runner' -Name '$name' -Executable 'powershell' -WorkingDirectory '$validation' -Arguments @('-NoProfile','-ExecutionPolicy','Bypass','-File','$script','-Gate','$gate')"
    & powershell -NoProfile -ExecutionPolicy Bypass -Command $command
    if ($LASTEXITCODE -ne 0) { throw ('Gate failed; preserve result and STOP: ' + $gate) }
}
