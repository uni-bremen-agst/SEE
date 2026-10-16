$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$destination=Join-Path $tool 'artifacts/p5o2b25/activation-attempt'
if(Test-Path $destination) { throw 'Activation attempt must not be overwritten.' }
[IO.Directory]::CreateDirectory($destination) | Out-Null
$paths=@(& git -C $repo diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repo ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
$files=foreach($path in $paths | Sort-Object -Unique) {
    $source=Join-Path $repo $path; $target=Join-Path $destination $path
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    Copy-Item -LiteralPath $source -Destination $target
    [ordered]@{Path=$path;Sha256=(Get-FileHash -LiteralPath $source).Hash}
}
[ordered]@{Head=(& git -C $repo rev-parse HEAD);CapturedUtc=[DateTime]::UtcNow.ToString('o');Files=$files;
    Reason='Stop rule: shared Summary evaluator normalization loses exception assembly ownership under equal declaration IDs; no partial activation.'} |
    ConvertTo-Json -Depth 8 | Set-Content (Join-Path $destination 'manifest.json') -Encoding UTF8
Write-Host "Preserved activation attempt ($($files.Count) files), original run records/TRXs and isolated inspection remain alongside it."
