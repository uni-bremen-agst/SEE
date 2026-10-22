param([switch]$VerifyOnly)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$files = @(Get-ChildItem (Join-Path $tool 'Evaluation/P5O2B25A6Proof') -File -Recurse)
if (Test-Path (Join-Path $tool 'Evaluation/P5O2B25A7Proof')) { $files += @(Get-ChildItem (Join-Path $tool 'Evaluation/P5O2B25A7Proof') -File -Recurse) }
foreach ($pattern in @('P5O2B25A6-*', 'P5O2B25A7-*')) { $files += @(Get-ChildItem (Join-Path $tool 'Evaluation') -File -Filter $pattern) }
$files += Get-Item (Join-Path $tool 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowGenericExpansionBoundaryTests.cs')
$encoding = [Text.UTF8Encoding]::new($false, $true)
foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $text = $encoding.GetString($bytes)
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xfeff) { throw ('Unexpected BOM: ' + $file.FullName) }
    $normalized = [regex]::Replace($text, "`r?`n", "`r`n")
    if (!$normalized.EndsWith("`r`n")) { $normalized += "`r`n" }
    if ($VerifyOnly) {
        if ([Convert]::ToBase64String($bytes) -cne [Convert]::ToBase64String($encoding.GetBytes($normalized))) { throw ('CRLF mismatch: ' + $file.FullName) }
    } else { [IO.File]::WriteAllText($file.FullName, $normalized, $encoding) }
}
Write-Host ('New A6/A7 owned files only, UTF8/no-BOM/CRLF: ' + $files.Count)
