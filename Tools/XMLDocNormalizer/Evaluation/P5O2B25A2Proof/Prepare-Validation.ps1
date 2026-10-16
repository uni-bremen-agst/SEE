param([string]$Destination='artifacts/b25a2-4d90')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$destinationPath=[IO.Path]::GetFullPath((Join-Path $tool $Destination))
if(-not $destinationPath.StartsWith((Join-Path $tool 'artifacts/'),[StringComparison]::OrdinalIgnoreCase) -or (Test-Path $destinationPath)) { throw 'Fresh artifacts destination required.' }
$start=Get-Content (Join-Path $tool 'artifacts/p5o2b25a2/start/audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
[IO.Directory]::CreateDirectory($destinationPath) | Out-Null
$archive=Join-Path $tool 'artifacts/p5o2b25a2/head.tar'
& git --no-optional-locks -C $repo archive --format=tar --output=$archive $start.Head Tools/XMLDocNormalizer
if($LASTEXITCODE -ne 0) { throw 'Archive failed.' }
& tar -xf $archive -C $destinationPath
if($LASTEXITCODE -ne 0) { throw 'Extraction failed.' }
$validation=Join-Path $destinationPath 'Tools/XMLDocNormalizer'
foreach($file in $start.B25AWip) {
    $target=Join-Path $validation $file.Path
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    Copy-Item -LiteralPath (Join-Path $tool ('artifacts/p5o2b25a2/start/wip/'+$file.Path)) -Destination $target
    if((Get-FileHash $target).Hash -cne $file.Sha256) { throw 'Baseline overlay differs.' }
}
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -ValidationRoot $validation
if($LASTEXITCODE -ne 0) { throw 'Offline configuration failed.' }
[ordered]@{Head=$start.Head;ToolRoot=$validation;Archive=$archive;Baseline='HEAD plus exact captured B25A WIP, not bare HEAD';Network=$false} |
    ConvertTo-Json | Set-Content (Join-Path $tool 'artifacts/p5o2b25a2/export.json') -Encoding UTF8
Write-Host $validation
