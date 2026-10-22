$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$start=Get-Content (Join-Path $tool 'artifacts/p5o2b25a4/start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$encoding=[Text.UTF8Encoding]::new($false)
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Digest([string]$Text) {
    $sha=[Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($sha.ComputeHash($encoding.GetBytes($Text))).Replace('-','') } finally { $sha.Dispose() }
}
foreach($file in $start.OriginalFiles | Where-Object Path -notin @('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) {
    Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) ('Inherited production/test/history changed: '+$file.Path)
}
$changes=@(foreach($relative in @('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) {
    $current=Join-Path $tool $relative
    $text=[IO.File]::ReadAllText($current).Replace("`r`n","`n")
    if($relative.StartsWith('build/')) {
        $restored=($text -split "`n" | Where-Object { $_ -notmatch '# B25A4 stop audit:' -and $_ -notmatch "Invoke-DotNet 'main-bounded-generic-context-tests' " }) -join "`n"
    } else { $restored=($text -split "`n" | Where-Object { $_ -notmatch '^- P5O2B25A4 Stop Rule:' }) -join "`n" }
    $old=@($start.OriginalFiles | Where-Object Path -ceq $relative)[0]
    Require ((Digest ($restored.Replace("`n","`r`n"))) -ceq $old.Sha256) ('A4 diff exceeds additive gate/register scope: '+$relative)
    $before=Join-Path $tool ('artifacts/p5o2b25a4/inherited/'+$relative)
    $info=[Diagnostics.ProcessStartInfo]::new('git',('diff --no-index --no-ext-diff -- "'+$before+'" "'+$current+'"'))
    $info.UseShellExecute=$false; $info.RedirectStandardOutput=$true; $info.StandardOutputEncoding=$encoding
    $process=[Diagnostics.Process]::Start($info)
    $patch=$process.StandardOutput.ReadToEnd(); $process.WaitForExit()
    Require ($process.ExitCode -eq 1) 'Expected an explicit no-index difference against inherited A3 WIP.'
    $process.Dispose()
    $name=if($relative.StartsWith('build/')) { 'gate-vs-inherited-a3-wip.patch.txt' } else { 'register-vs-inherited-a3-wip.patch.txt' }
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot $name),(($patch -replace "`r?`n","`r`n").TrimEnd()+"`r`n"),$encoding)
    [ordered]@{Path=$relative;BeforeSha256=$old.Sha256;AfterSha256=(Get-FileHash $current).Hash;ExactDelta=$name}
})
[IO.File]::WriteAllText((Join-Path $tool 'artifacts/p5o2b25a4/changes.json'),(($changes | ConvertTo-Json -Depth 6)+"`r`n"),$encoding)
Write-Host ('PASS: '+$start.OriginalFiles.Count+' inherited files unchanged except exactly two additive gate lines and one register paragraph; A3 WIP frozen.')
