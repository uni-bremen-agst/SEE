param([string]$Destination='artifacts/b25-f93d')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$destinationPath=[IO.Path]::GetFullPath((Join-Path $tool $Destination))
if(-not $destinationPath.StartsWith((Join-Path $tool 'artifacts/'),[StringComparison]::OrdinalIgnoreCase) -or (Test-Path $destinationPath)) { throw 'Fresh artifacts destination required.' }
[IO.Directory]::CreateDirectory($destinationPath) | Out-Null
$archive=Join-Path $tool 'artifacts/p5o2b25/head.tar'
& git -C $repo archive --format=tar --output=$archive HEAD Tools/XMLDocNormalizer
if($LASTEXITCODE -ne 0) { throw 'Git archive failed.' }
& tar -xf $archive -C $destinationPath
if($LASTEXITCODE -ne 0) { throw 'Extraction failed.' }
$validation=Join-Path $destinationPath 'Tools/XMLDocNormalizer'
$churn=@(Get-ChildItem $validation -Recurse -Directory | Where-Object Name -in @('bin','obj','artifacts'))
if($churn.Count) { throw 'HEAD archive unexpectedly contains generated output.' }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -ValidationRoot $validation
if($LASTEXITCODE -ne 0) { throw 'Offline configuration failed.' }
[ordered]@{Head=(& git -C $repo rev-parse HEAD);ToolRoot=$validation;Archive=$archive;CleanHeadExport=$true;Network=$false} |
    ConvertTo-Json | Set-Content (Join-Path $tool 'artifacts/p5o2b25/export.json') -Encoding UTF8
Write-Host $validation
