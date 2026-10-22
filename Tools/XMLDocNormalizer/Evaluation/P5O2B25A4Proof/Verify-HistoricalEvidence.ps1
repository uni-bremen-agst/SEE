$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$start=Get-Content (Join-Path $tool 'artifacts/p5o2b25a4/start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checks=[Collections.Generic.List[object]]::new()
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Same([string]$Path,[string]$Sha) { (Test-Path -LiteralPath $Path -PathType Leaf) -and (Get-FileHash -LiteralPath $Path).Hash -ceq $Sha }
function Fingerprints($Value) {
    if($null -eq $Value -or $Value -is [string] -or $Value.GetType().IsValueType) { return }
    if($Value -is [Array]) { foreach($v in $Value) { Fingerprints $v }; return }
    if($Value.Path -and $Value.Sha256) {
        $actual=$Value.Path; $basis='Unchanged historical file'
        $relative=if($actual.StartsWith($tool+'\',[StringComparison]::OrdinalIgnoreCase)) { $actual.Substring($tool.Length+1).Replace('\','/') } else { '' }
        if(-not (Same $actual $Value.Sha256) -and $relative) {
            foreach($candidate in @((Join-Path $tool ('artifacts/p5o2b25a4/inherited/'+$relative)),(Join-Path $script:historicalRoot $relative),(Join-Path $tool ('artifacts/p5o2b25a2/start/wip/'+$relative)))) {
                if(Same $candidate $Value.Sha256) { $actual=$candidate; $basis='Exact retained snapshot/export; no historical rewrite'; break }
            }
        }
        $verified=Same $actual $Value.Sha256
        if(-not $verified -and $relative -in @('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) {
            foreach($revision in @($start.Head,'4d90ce25ae143f90e968afdc8e5b6f5a7a07d82f')) {
                $info=[Diagnostics.ProcessStartInfo]::new('git',('show '+$revision+':Tools/XMLDocNormalizer/'+$relative))
                $info.WorkingDirectory=$tool; $info.RedirectStandardOutput=$true; $info.UseShellExecute=$false; $info.StandardOutputEncoding=[Text.UTF8Encoding]::new($false)
                $process=[Diagnostics.Process]::Start($info)
                $content=$process.StandardOutput.ReadToEnd(); $process.WaitForExit()
                Require ($process.ExitCode -eq 0) 'Historical committed blob unavailable.'
                $sha=[Security.Cryptography.SHA256]::Create()
                try { $digest=[BitConverter]::ToString($sha.ComputeHash([Text.UTF8Encoding]::new($false).GetBytes(($content -replace "`r?`n","`r`n")))).Replace('-','') }
                finally { $sha.Dispose(); $process.Dispose() }
                if($digest -ceq $Value.Sha256) { $verified=$true; $actual=$revision+':'+$relative; $basis='Exact committed historical CRLF bytes, read in memory'; break }
            }
        }
        Require $verified ('Historical fingerprint changed: '+$Value.Path)
        $checks.Add([ordered]@{HistoricalPath=$Value.Path;CheckedPath=$actual;Sha256=$Value.Sha256;Basis=$basis})
    }
    foreach($p in $Value.psobject.Properties) { Fingerprints $p.Value }
}
$history=@(foreach($name in @('P5O2B25-bounded-solution-finding-adoption-audit.json','P5O2B25A-assembly-qualified-type-ownership-audit.json','P5O2B25A2-canonical-exception-type-identity-audit.json','P5O2B25A3-constructed-generic-summary-context-audit.json')) {
    $path=Join-Path $tool ('Evaluation/'+$name)
    $audit=Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json
    $script:historicalRoot=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($audit.Dual.Path)) '../..'))
    Fingerprints $audit
    [ordered]@{File=$name;Sha256=(Get-FileHash $path).Hash;Passed=$audit.Passed;ValidationPassed=$audit.ValidationPassed;EvidenceConsistent=$audit.EvidenceConsistent;Outcome=$audit.Outcome}
})
Require (-not $history[1].Passed -and $history[2].Passed -and -not $history[3].Passed -and $history[3].EvidenceConsistent) 'Retain historical red A / green A2 / red A3.'
[IO.File]::WriteAllText((Join-Path $tool 'artifacts/p5o2b25a4/historical-evidence-verification.json'),(([ordered]@{Package='P5O2B25A4';Passed=$true;ReadOnlyHistoricalVerification=$true;GeneratedUtc=[DateTime]::UtcNow.ToString('o');HistoricalAudits=$history;Fingerprints=@($checks.ToArray());FrozenHistoryRewritten=$false} | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host ('PASS: read-only B25/A/A2/A3 historical evidence; '+$checks.Count+' exact fingerprints.')
