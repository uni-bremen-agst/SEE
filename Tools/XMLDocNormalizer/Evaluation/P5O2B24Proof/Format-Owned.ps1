param([switch]$Verify)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$paths=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
$owned=@($paths | Sort-Object -Unique)
$utf8=[Text.UTF8Encoding]::new($false,$true)
foreach($relative in $owned) {
    $path=Join-Path $repoRoot $relative
    $text=$utf8.GetString([IO.File]::ReadAllBytes($path))
    if($Verify) {
        if($text.Length -eq 0 -or $text[0] -eq [char]0xFEFF -or -not $text.EndsWith("`r`n") -or $text -match '(?<!\r)\n') { throw "UTF8/noBOM/CRLF: $relative" }
    } else {
        $normalized=$text.Replace("`r`n","`n").Replace("`r","`n").TrimEnd("`n")+"`n"
        [IO.File]::WriteAllText($path,$normalized.Replace("`n","`r`n"),[Text.UTF8Encoding]::new($false))
    }
}
$cs=@($owned | Where-Object { $_.EndsWith('.cs') } | ForEach-Object { $_.Substring('Tools/XMLDocNormalizer/'.Length) })
Push-Location $toolRoot
try {
    $arguments=@('format','whitespace','.','--folder','--include')+$cs
    if($Verify) { $arguments+='--verify-no-changes' }
    & dotnet @arguments
    if($LASTEXITCODE -ne 0) { throw 'Owned C# whitespace failed.' }
} finally { Pop-Location }
