param([switch]$VerifyOnly)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$files = @(Get-ChildItem $PSScriptRoot -File -Recurse) + @(Get-ChildItem (Join-Path $tool 'Evaluation') -File -Filter 'P5O2B25A9-*')
$encoding = [Text.UTF8Encoding]::new($false, $true)
foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName); $value = $encoding.GetString($bytes)
    if ($value.Length -gt 0 -and $value[0] -eq [char]0xfeff) { throw 'Unexpected BOM.' }
    $normal = [regex]::Replace($value, "`r?`n", "`r`n")
    if (!$normal.EndsWith("`r`n")) { $normal += "`r`n" }
    if ($VerifyOnly) { if ([Convert]::ToBase64String($bytes) -cne [Convert]::ToBase64String($encoding.GetBytes($normal))) { throw ('CRLF mismatch: ' + $file.FullName) } }
    else { [IO.File]::WriteAllText($file.FullName, $normal, $encoding) }
}
Write-Host ('New A9 files only, UTF8/no-BOM/CRLF: ' + $files.Count)
