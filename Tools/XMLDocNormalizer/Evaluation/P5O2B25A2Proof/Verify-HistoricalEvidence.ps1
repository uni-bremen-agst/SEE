$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$start=Get-Content (Join-Path $tool 'artifacts/p5o2b25a2/start/audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checked=[Collections.Generic.List[object]]::new()
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function CheckFingerprint($Value) {
    if($null -eq $Value -or $Value -is [string] -or $Value.GetType().IsValueType) { return }
    if($Value -is [System.Array]) { foreach($item in $Value) { CheckFingerprint $item }; return }
    if($Value.Path -and $Value.Sha256) {
        $actual=$Value.Path
        $relative=if($actual.StartsWith($tool,[StringComparison]::OrdinalIgnoreCase)) { $actual.Substring($tool.Length+1).Replace('\','/') } else { '' }
        $snapshot=@($start.B25AWip | Where-Object Path -ceq $relative)
        $basis='Unmodified historical file'
        if($snapshot.Count -eq 1 -and $snapshot[0].Sha256 -ceq $Value.Sha256) {
            $actual=Join-Path $tool ('artifacts/p5o2b25a2/start/wip/'+$relative)
            $basis='Exact captured inherited B25A WIP (not later A2 gate/register content)'
        }
        elseif($relative -and $script:historicalRoot -and (Get-FileHash -LiteralPath $actual).Hash -cne $Value.Sha256) {
            $actual=Join-Path $script:historicalRoot $relative
            $basis='Exact historical disposable export; historical gate/register predate inherited B25A WIP'
        }
        $verified=(Test-Path -LiteralPath $actual -PathType Leaf) -and (Get-FileHash -LiteralPath $actual).Hash -ceq $Value.Sha256
        if(-not $verified -and $relative -in @('Evaluation/OPEN-PIPELINE-BOUNDARIES.md','build/Verify-DualVersionBuild.ps1')) {
            # Historical register updates may postdate its old build export.
            # Verify the committed blob with the recorded CRLF worktree bytes;
            # never overwrite a current file or historical evidence file.
            $info=[Diagnostics.ProcessStartInfo]::new('git',('show HEAD:Tools/XMLDocNormalizer/'+$relative))
            $info.WorkingDirectory=$tool; $info.RedirectStandardOutput=$true; $info.UseShellExecute=$false
            $info.StandardOutputEncoding=[Text.UTF8Encoding]::new($false)
            $process=[Diagnostics.Process]::Start($info)
            $text=$process.StandardOutput.ReadToEnd(); $process.WaitForExit()
            Require ($process.ExitCode -eq 0) 'Committed historical blob unavailable.'
            $sha=[Security.Cryptography.SHA256]::Create()
            try { $digest=[BitConverter]::ToString($sha.ComputeHash([Text.UTF8Encoding]::new($false).GetBytes(($text -replace "`r?`n","`r`n")))).Replace('-','') }
            finally { $sha.Dispose(); $process.Dispose() }
            $verified=$digest -ceq $Value.Sha256
            $actual='HEAD:Tools/XMLDocNormalizer/'+$relative
            $basis='Read-only committed historical blob; exact recorded UTF8/CRLF bytes verified in memory'
        }
        Require $verified ('Historical fingerprint differs: '+$Value.Path)
        $checked.Add([ordered]@{HistoricalPath=$Value.Path;CheckedPath=$actual;Sha256=$Value.Sha256;Basis=$basis})
    }
    foreach($property in $Value.psobject.Properties) { CheckFingerprint $property.Value }
}
foreach($file in $start.B25AWip | Where-Object { $_.Path -like 'Evaluation/P5O2B25A-*' -or $_.Path -like 'Evaluation/P5O2B25AProof/*' -or $_.Path -like '*ExceptionFlowAssemblyQualifiedOwnershipTests.cs' }) {
    Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) ('Frozen B25A WIP changed: '+$file.Path)
}
& git --no-optional-locks -C $tool diff --quiet HEAD -- Evaluation/P5O2B25-bounded-solution-finding-adoption.md Evaluation/P5O2B25-bounded-solution-finding-adoption-audit.json Evaluation/P5O2B25Proof Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs
Require ($LASTEXITCODE -eq 0) 'Frozen B25 history changed.'
$history=foreach($name in 'P5O2B25-bounded-solution-finding-adoption-audit.json','P5O2B25A-assembly-qualified-type-ownership-audit.json') {
    $audit=Get-Content (Join-Path $tool ('Evaluation/'+$name)) -Raw -Encoding UTF8 | ConvertFrom-Json
    $script:historicalRoot=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($audit.Dual.Path)) '../..'))
    CheckFingerprint $audit
    [ordered]@{File=$name;HistoricalPassed=$audit.Passed;HistoricalValidationPassed=$audit.ValidationPassed;HistoricalEvidenceConsistent=$audit.EvidenceConsistent;Outcome=$audit.Outcome}
}
$old=Get-Content (Join-Path $tool 'Evaluation/P5O2B25A-assembly-qualified-type-ownership-audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$broad=@($old.Tests | Where-Object Name -eq 'broad')[0]
Require (-not $old.Passed -and -not $old.ValidationPassed -and $old.EvidenceConsistent -and $broad.Failed -eq 1 -and $broad.Total -eq 3226) 'Do not rewrite the historical red Broad as green.'
$result=[ordered]@{Package='P5O2B25A2';Passed=$true;ReadOnlyHistoricalVerification=$true;GeneratedUtc=[DateTime]::UtcNow.ToString('o');HistoricalAudits=$history;Fingerprints=@($checked.ToArray());HistoricalBroad=$broad;FrozenHistoryRewritten=$false}
$output=Join-Path $tool 'artifacts/p5o2b25a2/historical-evidence-verification.json'
[IO.File]::WriteAllText($output,(($result | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host ('PASS: read-only historical evidence verification; '+$checked.Count+' fingerprints; historical B25A remains red.')
