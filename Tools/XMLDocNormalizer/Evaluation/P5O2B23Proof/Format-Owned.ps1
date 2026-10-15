$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$paths=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
foreach($relative in @($paths | Sort-Object -Unique)) {
    if([IO.Path]::GetExtension($relative) -notin @('.cs','.ps1','.md','.json')) { throw "Unexpected owned format target: $relative" }
    $path=Join-Path $repoRoot $relative
    $text=[Text.UTF8Encoding]::new($false,$true).GetString([IO.File]::ReadAllBytes($path))
    $text=$text.TrimStart([char]0xFEFF).Replace("`r`n","`n").Replace("`r","`n").TrimEnd("`n")+"`n"
    [IO.File]::WriteAllText($path,$text.Replace("`n","`r`n"),[Text.UTF8Encoding]::new($false))
}
