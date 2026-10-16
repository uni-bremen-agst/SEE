$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$start=Get-Content (Join-Path $tool 'artifacts/p5o2b25a2/start/audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$encoding=[Text.UTF8Encoding]::new($false)
$deltas=foreach($relative in 'build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md') {
    $before=Join-Path $tool ('artifacts/p5o2b25a2/start/wip/'+$relative)
    $after=Join-Path $tool $relative
    $old=[IO.File]::ReadAllText($before).Replace("`r`n","`n")
    $new=[IO.File]::ReadAllText($after).Replace("`r`n","`n")
    if($relative.StartsWith('build/')) {
        $removed=$new -split "`n" | Where-Object { $_ -notmatch '# B25A2 boundary audit:' -and $_ -notmatch "Invoke-DotNet 'main-canonical-exception-identity-tests' " }
        if(($removed -join "`n") -cne $old) { throw 'The build gate has changes beyond its two additive A2 lines.' }
    } else {
        $removed=$new -split "`n" | Where-Object { $_ -notmatch '^- P5O2B25A2 Stop Rule:' }
        $restored=($removed -join "`n").Replace('- Last Verified: 2026-10-16 (B25A2 stop-rule summary-instantiation audit;','- Last Verified: 2026-10-16 (B25A stop-rule identity/catch audit;')
        if($restored -cne $old) { throw 'The boundary register has changes beyond its A2 entry and verification label.' }
    }
    $info=[Diagnostics.ProcessStartInfo]::new('git',('diff --no-index --no-ext-diff -- "'+$before+'" "'+$after+'"'))
    $info.WorkingDirectory=$tool; $info.RedirectStandardOutput=$true; $info.UseShellExecute=$false; $info.StandardOutputEncoding=$encoding
    $process=[Diagnostics.Process]::Start($info)
    $patch=$process.StandardOutput.ReadToEnd(); $process.WaitForExit()
    if($process.ExitCode -ne 1) { throw 'An exact nonempty overlap diff was expected.' }
    $process.Dispose()
    $name=if($relative.StartsWith('build/')) { 'gate-vs-b25a-wip.patch.txt' } else { 'register-vs-b25a-wip.patch.txt' }
    $path=Join-Path $PSScriptRoot $name
    [IO.File]::WriteAllText($path,(($patch -replace "`r?`n","`r`n").TrimEnd()+"`r`n"),$encoding)
    [ordered]@{Path=$relative;BeforeSha256=(Get-FileHash $before).Hash;AfterSha256=(Get-FileHash $after).Hash;ExactDelta=$name;InheritedContentOtherwiseUnchanged=$true}
}
foreach($file in $start.B25AWip | Where-Object Path -notin @('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) {
    if((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -cne $file.Sha256) { throw ('Inherited B25A WIP changed: '+$file.Path) }
}
$output=Join-Path $tool 'artifacts/p5o2b25a2/wip-deltas.json'
[IO.File]::WriteAllText($output,(($deltas | ConvertTo-Json -Depth 6)+"`r`n"),$encoding)
Write-Host 'PASS: exactly two additive gate lines, one boundary entry/label; all other inherited B25A WIP unchanged.'
