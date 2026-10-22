& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Invoke-Inherited.ps1') -Mode Revision
exit $LASTEXITCODE
