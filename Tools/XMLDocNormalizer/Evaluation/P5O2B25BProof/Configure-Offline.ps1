param([Parameter(Mandatory=$true)][string]$ValidationRoot,
    [Parameter(Mandatory=$true)][string]$HistoricalCache)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation = [IO.Path]::GetFullPath($ValidationRoot)
$record = Get-Content (Join-Path $PSScriptRoot 'implementation-export.json') -Raw | ConvertFrom-Json
if ($validation -cne $record.Export -or $validation -eq $tool) { throw 'Only the recorded isolated export is writable' }
$cache = [IO.Path]::GetFullPath($HistoricalCache)
if (-not (Test-Path -LiteralPath $cache -PathType Container)) { throw 'Pinned existing cache required' }
# Reuse the unchanged offline-preparation algorithm; only bind its isolated export guard and cache location.
# Original feed names, values and version mapping remain present for all architecture checks.
$source = Get-Content (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -Raw
$source = $source.Replace("`$toolRoot=[IO.Path]::GetFullPath((Join-Path `$PSScriptRoot '../..'))", "`$toolRoot='$tool'")
$source = $source.Replace("if(-not `$validation.StartsWith((Join-Path `$toolRoot 'artifacts/'),[StringComparison]::OrdinalIgnoreCase))", "if(`$validation -cne '$validation')")
$source = $source.Replace("`$cache=[IO.Path]::GetFullPath((Join-Path `$toolRoot `$HistoricalCache))", "`$cache='$cache'")
& ([scriptblock]::Create($source)) -ValidationRoot $validation
if ($LASTEXITCODE -ge 8) { throw 'Offline preparation failed' }
