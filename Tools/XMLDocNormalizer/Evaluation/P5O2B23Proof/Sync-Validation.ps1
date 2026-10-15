param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$paths=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
foreach($relative in @($paths | Sort-Object -Unique)) {
    $toolRelative=$relative.Substring('Tools/XMLDocNormalizer/'.Length)
    $source=Join-Path $toolRoot $toolRelative
    $target=Join-Path $ValidationRoot $toolRelative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    [IO.File]::Copy($source,$target,$true)
}
