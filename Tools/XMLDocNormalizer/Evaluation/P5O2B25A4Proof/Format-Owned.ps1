param([switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$files=@(Get-ChildItem (Join-Path $tool 'Evaluation/P5O2B25A4Proof') -Recurse -File)
$files+=@(Get-ChildItem (Join-Path $tool 'Evaluation') -File -Filter 'P5O2B25A4-*')
$files+=Get-Item (Join-Path $tool 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs')
$files+=Get-Item (Join-Path $tool 'build/Verify-DualVersionBuild.ps1')
$files+=Get-Item (Join-Path $tool 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md')
$encoding=[Text.UTF8Encoding]::new($false,$true)
foreach($file in $files) {
    $text=$encoding.GetString([IO.File]::ReadAllBytes($file.FullName))
    if($text.Length -gt 0 -and $text[0] -eq [char]0xfeff) { $text=$text.Substring(1) }
    $normalized=($text -replace "`r?`n","`r`n").TrimEnd()+"`r`n"
    if($VerifyOnly) {
        if([Convert]::ToBase64String([IO.File]::ReadAllBytes($file.FullName)) -cne [Convert]::ToBase64String($encoding.GetBytes($normalized))) { throw ('UTF8/no-BOM/CRLF mismatch: '+$file.FullName) }
    } else { [IO.File]::WriteAllText($file.FullName,$normalized,$encoding) }
}
Write-Host ('A4-owned files plus its two explicit gate/register edits: '+$files.Count+'; frozen B25/B25A/B25A2/B25A3 evidence untouched.')
