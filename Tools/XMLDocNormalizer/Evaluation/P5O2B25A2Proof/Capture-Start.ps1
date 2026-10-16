$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$destination=Join-Path $tool 'artifacts/p5o2b25a2/start'
if(Test-Path $destination) { throw 'Never overwrite the actual B25A WIP baseline.' }
[IO.Directory]::CreateDirectory($destination) | Out-Null
$paths=@(& git --no-optional-locks -C $repo diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git --no-optional-locks -C $repo ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
$wip=foreach($path in @($paths | Sort-Object -Unique | Where-Object { $_ -notlike '*P5O2B25A2*' })) {
    $relative=$path.Substring('Tools/XMLDocNormalizer/'.Length)
    $target=Join-Path $destination ('wip/'+$relative)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo $path) -Destination $target
    [ordered]@{Path=$relative;Sha256=(Get-FileHash -LiteralPath $target).Hash}
}
$originals=foreach($path in @('src','Tests','build')) {
    Get-ChildItem (Join-Path $tool $path) -File -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj|artifacts)\\' -and $_.Name -ne 'AGENTS.md' } | ForEach-Object {
        [ordered]@{Path=$_.FullName.Substring($tool.Length+1).Replace('\','/');Sha256=(Get-FileHash $_.FullName).Hash}
    }
}
$staged=@(& git --no-optional-locks -C $repo diff --cached --name-only)
& git --no-optional-locks -C $repo diff --cached --quiet
if($LASTEXITCODE -ne 0 -or $staged.Count) { throw 'Unexpected semantic index changes.' }
$prior=Get-Content (Join-Path $tool 'artifacts/p5o2b25a/protected-before.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$state=[ordered]@{Head=(& git --no-optional-locks -C $repo rev-parse HEAD);Status=@(& git --no-optional-locks -C $repo status --short);
    IndexSha256=(Get-FileHash (Join-Path $repo '.git/index')).Hash;PriorB25AIndexSha256=$prior.Index;SemanticIndexClean=$true;IgnoreSha256=(Get-FileHash (Join-Path $repo '.gitignore')).Hash;
    Stashes=@(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs');
    Core=@(Get-ChildItem (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -File -Recurse | Sort-Object FullName | ForEach-Object { [ordered]@{Path=$_.FullName;Sha256=(Get-FileHash $_.FullName).Hash} });
    B25AWip=$wip;OriginalInputs=@($originals);CapturedUtc=[DateTime]::UtcNow.ToString('o')}
[IO.File]::WriteAllText((Join-Path $destination 'audit.json'),(($state | ConvertTo-Json -Depth 9)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host "Captured authoritative B25A WIP ($($wip.Count) files), $($state.Core.Count) Core files, $($state.Stashes.Count) stashes; semantic index clean."
