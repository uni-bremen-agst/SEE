param([Parameter(Mandatory = $true)][ValidateSet('A6', 'A7', 'A8')][string]$Gate)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($Gate -eq 'A8') { $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot 'Verify-A8.ps1')) }
else { $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot 'Invoke-Inherited.ps1'), '-Mode', ($Gate + 'Gate')) }
& (Join-Path $PSScriptRoot 'Run-Recorded.ps1') -Name ($Gate.ToLowerInvariant() + '-gate') -Executable powershell -WorkingDirectory $tool -Arguments $arguments
exit $LASTEXITCODE
