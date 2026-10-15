param([string]$Destination = 'artifacts/b23-a3d8')
$ErrorActionPreference = 'Stop'
$toolRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$exportRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot $Destination))
if (-not $exportRoot.StartsWith((Join-Path $toolRoot 'artifacts/'), [StringComparison]::OrdinalIgnoreCase)) { throw 'Export must stay in tool artifacts.' }
if (Test-Path -LiteralPath $exportRoot) { throw 'Use a fresh export destination.' }
[IO.Directory]::CreateDirectory($exportRoot) | Out-Null
$archive = Join-Path $toolRoot 'artifacts/p5o2b23/head.tar'
& git -C $repoRoot archive --format=tar --output=$archive HEAD Tools/XMLDocNormalizer
if ($LASTEXITCODE -ne 0) { throw 'Archive failed.' }
& tar -xf $archive -C $exportRoot
if ($LASTEXITCODE -ne 0) { throw 'Extract failed.' }
$changed = @(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)
$added = @(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
$overlay = @($changed + $added | Sort-Object -Unique)
foreach ($relative in $overlay) {
    $source = Join-Path $repoRoot $relative
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing overlay: $relative" }
    $target = Join-Path $exportRoot $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    [IO.File]::Copy($source, $target, $true)
}
$evidence = [ordered]@{Head=(& git -C $repoRoot rev-parse HEAD);ExportRoot=$exportRoot;ToolRoot=(Join-Path $exportRoot 'Tools/XMLDocNormalizer');Overlay=$overlay;NoInitialBuildArtifacts=$true}
[IO.File]::WriteAllText((Join-Path $toolRoot 'artifacts/p5o2b23/export.json'), ($evidence | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
Write-Host $evidence.ToolRoot
