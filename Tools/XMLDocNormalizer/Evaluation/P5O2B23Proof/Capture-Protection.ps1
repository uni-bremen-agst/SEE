param([string]$OutputPath = 'artifacts/p5o2b23/protected-before.json')
$ErrorActionPreference = 'Stop'
$toolRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$coreRoot = Join-Path $toolRoot 'src/XMLDocNormalizer.ExceptionFlow.Core'
$state = [ordered]@{
    Head = (& git -C $repoRoot rev-parse HEAD)
    Status = @(& git -C $repoRoot status --short)
    Index = (Get-FileHash (Join-Path $repoRoot '.git/index')).Hash
    Ignore = (Get-FileHash (Join-Path $repoRoot '.gitignore')).Hash
    Stashes = @(& git -C $repoRoot reflog show refs/stash --format='%H %gs')
    Core = @(Get-ChildItem -LiteralPath $coreRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
        [ordered]@{Path=$_.FullName;Sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}
    })
}
$destination = [IO.Path]::GetFullPath((Join-Path $toolRoot $OutputPath))
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
[IO.File]::WriteAllText($destination, ($state | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
Write-Host "Captured $($state.Core.Count) Core files and $($state.Stashes.Count) stashes: $destination"
