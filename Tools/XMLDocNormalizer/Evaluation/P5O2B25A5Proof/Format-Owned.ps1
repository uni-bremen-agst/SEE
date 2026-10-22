param([switch]$VerifyOnly)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$files = @(Get-ChildItem (Join-Path $tool 'Evaluation/P5O2B25A5Proof') -Recurse -File)
$files += @(Get-ChildItem (Join-Path $tool 'Evaluation') -File -Filter 'P5O2B25A5-*')
foreach ($relative in @('src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs', 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs', 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs', 'build/Verify-DualVersionBuild.ps1', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) { $files += Get-Item (Join-Path $tool $relative) }
$encoding = [Text.UTF8Encoding]::new($false, $true)
foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $text = $encoding.GetString($bytes)
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xfeff) { throw ('Unexpected BOM: ' + $file.FullName) }
    $normalized = $text -replace "`r?`n", "`r`n"
    if (!$normalized.EndsWith("`r`n")) { $normalized += "`r`n" }
    if ($VerifyOnly) {
        if ([Convert]::ToBase64String($bytes) -cne [Convert]::ToBase64String($encoding.GetBytes($normalized))) { throw ('CRLF mismatch: ' + $file.FullName) }
    } else { [IO.File]::WriteAllText($file.FullName, $normalized, $encoding) }
}
Write-Host ('A5 plus its four explicit inherited source/test/gate/register edits: ' + $files.Count + ' files; other historical evidence untouched.')
