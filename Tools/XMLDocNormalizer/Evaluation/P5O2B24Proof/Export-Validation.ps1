param([string]$Destination='artifacts/b24-4b68')
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$exportRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot $Destination))
if(-not $exportRoot.StartsWith((Join-Path $toolRoot 'artifacts/'),[StringComparison]::OrdinalIgnoreCase) -or (Test-Path $exportRoot)) { throw 'Use a fresh artifacts destination.' }
[IO.Directory]::CreateDirectory($exportRoot) | Out-Null
$archive=Join-Path $toolRoot 'artifacts/p5o2b24/head.tar'
& git -C $repoRoot archive --format=tar --output=$archive HEAD Tools/XMLDocNormalizer
if($LASTEXITCODE -ne 0) { throw 'Archive failed.' }
& tar -xf $archive -C $exportRoot
if($LASTEXITCODE -ne 0) { throw 'Extract failed.' }
$validation=Join-Path $exportRoot 'Tools/XMLDocNormalizer'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $toolRoot 'Evaluation/P5O2B23Proof/Sync-Validation.ps1') -ValidationRoot $validation
if($LASTEXITCODE -ne 0) { throw 'Overlay failed.' }
$state=[ordered]@{Head=(& git -C $repoRoot rev-parse HEAD);ToolRoot=$validation;Archive=$archive}
[IO.File]::WriteAllText((Join-Path $toolRoot 'artifacts/p5o2b24/export.json'),($state | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Host $validation
