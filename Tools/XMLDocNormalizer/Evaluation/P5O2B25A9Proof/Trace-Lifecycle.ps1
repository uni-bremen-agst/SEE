& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Invoke-Inherited.ps1') -Mode Trace
exit $LASTEXITCODE
