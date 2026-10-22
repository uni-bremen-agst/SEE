$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$start=Get-Content (Join-Path $tool 'artifacts/p5o2b25a3/start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checked=[Collections.Generic.List[object]]::new()
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function SameHash([string]$Path,[string]$Hash) { (Test-Path -LiteralPath $Path -PathType Leaf) -and (Get-FileHash -LiteralPath $Path).Hash -ceq $Hash }
function CheckFingerprint($Value) {
    if($null -eq $Value -or $Value -is [string] -or $Value.GetType().IsValueType) { return }
    if($Value -is [System.Array]) { foreach($item in $Value) { CheckFingerprint $item }; return }
    if($Value.Path -and $Value.Sha256) {
        $actual=$Value.Path
        $relative=if($actual.StartsWith($tool+'\',[StringComparison]::OrdinalIgnoreCase)) { $actual.Substring($tool.Length+1).Replace('\','/') } else { '' }
        $basis='Unchanged actual historical file'
        if(-not (SameHash $actual $Value.Sha256) -and $relative) {
            foreach($candidate in @((Join-Path $script:historicalRoot $relative),(Join-Path $tool ('artifacts/p5o2b25a2/start/wip/'+$relative)))) {
                if(SameHash $candidate $Value.Sha256) { $actual=$candidate; $basis='Exact retained historical export or captured B25A WIP'; break }
            }
        }
        $verified=SameHash $actual $Value.Sha256
        if(-not $verified -and $relative -in @('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) {
            foreach($revision in @($start.Head,'4d90ce25ae143f90e968afdc8e5b6f5a7a07d82f')) {
                $info=[Diagnostics.ProcessStartInfo]::new('git',('show '+$revision+':Tools/XMLDocNormalizer/'+$relative))
                $info.WorkingDirectory=$tool; $info.RedirectStandardOutput=$true; $info.UseShellExecute=$false; $info.StandardOutputEncoding=[Text.UTF8Encoding]::new($false)
                $process=[Diagnostics.Process]::Start($info)
                $text=$process.StandardOutput.ReadToEnd(); $process.WaitForExit()
                Require ($process.ExitCode -eq 0) 'Historical committed blob unavailable.'
                $sha=[Security.Cryptography.SHA256]::Create()
                try { $digest=[BitConverter]::ToString($sha.ComputeHash([Text.UTF8Encoding]::new($false).GetBytes(($text -replace "`r?`n","`r`n")))).Replace('-','') }
                finally { $sha.Dispose(); $process.Dispose() }
                if($digest -ceq $Value.Sha256) { $verified=$true; $actual=$revision+':Tools/XMLDocNormalizer/'+$relative; $basis='Read-only committed blob; exact recorded UTF8/CRLF worktree bytes verified in memory'; break }
            }
        }
        Require $verified ('Historical fingerprint differs: '+$Value.Path)
        $checked.Add([ordered]@{HistoricalPath=$Value.Path;CheckedPath=$actual;Sha256=$Value.Sha256;Basis=$basis})
    }
    foreach($property in $Value.psobject.Properties) { CheckFingerprint $property.Value }
}
foreach($file in $start.OriginalFiles | Where-Object { $_.Path -match '^Evaluation/P5O2B25(A2|A)?[-P]' -or $_.Path -match 'ExceptionFlow(CanonicalExceptionIdentity|AssemblyQualifiedOwnership|HistoricalSolutionFinding)Tests.cs$' }) {
    Require (SameHash (Join-Path $tool $file.Path) $file.Sha256) ('Committed historical evidence or characterization changed: '+$file.Path)
}
$history=foreach($name in 'P5O2B25-bounded-solution-finding-adoption-audit.json','P5O2B25A-assembly-qualified-type-ownership-audit.json','P5O2B25A2-canonical-exception-type-identity-audit.json') {
    $audit=Get-Content (Join-Path $tool ('Evaluation/'+$name)) -Raw -Encoding UTF8 | ConvertFrom-Json
    $script:historicalRoot=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($audit.Dual.Path)) '../..'))
    CheckFingerprint $audit
    [ordered]@{File=$name;HistoricalPassed=$audit.Passed;HistoricalValidationPassed=$audit.ValidationPassed;HistoricalEvidenceConsistent=$audit.EvidenceConsistent;Outcome=$audit.Outcome}
}
Require (-not $history[1].HistoricalPassed -and $history[1].HistoricalEvidenceConsistent -and $history[2].HistoricalPassed) 'Do not change the historical red B25A / green B25A2 outcome.'
$result=[ordered]@{Package='P5O2B25A3';Passed=$true;ReadOnlyHistoricalVerification=$true;GeneratedUtc=[DateTime]::UtcNow.ToString('o');HistoricalAudits=$history;Fingerprints=@($checked.ToArray());FrozenHistoryRewritten=$false}
[IO.File]::WriteAllText((Join-Path $tool 'artifacts/p5o2b25a3/historical-evidence-verification.json'),(($result | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host ('PASS: read-only B25/B25A/B25A2 historical evidence; '+$checked.Count+' exact fingerprints.')
