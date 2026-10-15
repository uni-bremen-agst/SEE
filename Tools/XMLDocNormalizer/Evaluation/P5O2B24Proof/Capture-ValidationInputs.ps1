param([Parameter(Mandatory=$true)][string]$ValidationRoot, [string]$OutputPath='artifacts/p5o2b24/validation-inputs.json')
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$paths=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
$sources=foreach($path in @($paths | Sort-Object -Unique | Where-Object { $_.EndsWith('.cs') })) {
    $relative=$path.Substring('Tools/XMLDocNormalizer/'.Length)
    $original=(Get-FileHash (Join-Path $repoRoot $path)).Hash
    $built=(Get-FileHash (Join-Path $ValidationRoot $relative)).Hash
    if($original -cne $built) { throw "Source overlay differs: $relative" }
    [ordered]@{Path=$relative;Sha256=$built}
}
$images=foreach($relative in @('src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll',
    'artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll',
    'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll')) {
    $path=Join-Path $ValidationRoot $relative
    [ordered]@{Path=$relative;Sha256=(Get-FileHash $path).Hash;LastWriteUtc=(Get-Item $path).LastWriteTimeUtc.ToString('o')}
}
$state=[ordered]@{CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=(& git -C $repoRoot rev-parse HEAD);Sources=$sources;Images=$images}
[IO.File]::WriteAllText((Join-Path $toolRoot $OutputPath),(($state | ConvertTo-Json -Depth 8)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host 'Captured the exact compiled production/test images and their source overlay; no build or mutation of binaries.'
