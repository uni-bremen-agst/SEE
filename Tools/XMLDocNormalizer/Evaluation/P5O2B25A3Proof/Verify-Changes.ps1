$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$start=Get-Content (Join-Path $tool 'artifacts/p5o2b25a3/start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$encoding=[Text.UTF8Encoding]::new($false)
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Digest([string]$Text) {
    $sha=[Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($sha.ComputeHash($encoding.GetBytes($Text))).Replace('-','') } finally { $sha.Dispose() }
}
foreach($file in $start.OriginalFiles | Where-Object Path -notin @('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) {
    Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) ('Original production, test or history changed: '+$file.Path)
}
$deltas=foreach($relative in 'build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md') {
    $text=[IO.File]::ReadAllText((Join-Path $tool $relative)).Replace("`r`n","`n")
    if($relative.StartsWith('build/')) {
        $restored=($text -split "`n" | Where-Object { $_ -notmatch '# B25A3 stop audit:' -and $_ -notmatch "Invoke-DotNet 'main-constructed-invocation-context-tests' " }) -join "`n"
    } else {
        $restored=($text -split "`n" | Where-Object { $_ -notmatch '^- P5O2B25A3 Stop Rule:' }) -join "`n"
        $restored=$restored.Replace('- Last Verified: 2026-10-16 (B25A3 stop-rule generic-context expansion audit; no production repair or activation; initial B24 native probe failure retained, no waiver)',
            '- Last Verified: 2026-10-16 (B25A2 stop-rule summary-instantiation audit; no production repair or activation; B24/PID-G1 and earlier bounded closures preserved)')
    }
    $old=@($start.OriginalFiles | Where-Object Path -ceq $relative)[0]
    Require ((Digest ($restored.Replace("`n","`r`n"))) -ceq $old.Sha256) ('Change exceeds its exact additive A3 gate/register scope: '+$relative)
    $info=[Diagnostics.ProcessStartInfo]::new('git',('diff --no-ext-diff -- "'+$relative+'"'))
    $info.WorkingDirectory=$tool; $info.UseShellExecute=$false; $info.RedirectStandardOutput=$true; $info.StandardOutputEncoding=$encoding
    $process=[Diagnostics.Process]::Start($info)
    $patch=$process.StandardOutput.ReadToEnd(); $process.WaitForExit()
    Require ($process.ExitCode -eq 0) 'Could not capture the exact HEAD-relative delta.'
    $process.Dispose()
    $name=if($relative.StartsWith('build/')) { 'gate-vs-c777.patch.txt' } else { 'register-vs-c777.patch.txt' }
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot $name),(($patch -replace "`r?`n","`r`n").TrimEnd()+"`r`n"),$encoding)
    [ordered]@{Path=$relative;BeforeSha256=$old.Sha256;AfterSha256=(Get-FileHash (Join-Path $tool $relative)).Hash;ExactDelta=$name}
}
[IO.File]::WriteAllText((Join-Path $tool 'artifacts/p5o2b25a3/changes.json'),(($deltas | ConvertTo-Json -Depth 6)+"`r`n"),$encoding)
Write-Host 'PASS: all original production/tests/history unchanged; exactly two additive gate lines and one boundary entry/label.'
