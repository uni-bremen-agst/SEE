param([string]$OutputPath='artifacts/p5o2b25a3/evidence-gate-process.json')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$destination=Join-Path $tool $OutputPath
if(Test-Path -LiteralPath $destination) { throw 'Preserve the actual process record; do not overwrite an attempt.' }
$info=[Diagnostics.ProcessStartInfo]::new('powershell',('-NoProfile -ExecutionPolicy Bypass -File "'+(Join-Path $PSScriptRoot 'Write-Verify-Evidence.ps1')+'"'))
$info.WorkingDirectory=$tool; $info.UseShellExecute=$false
$info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
$process=[Diagnostics.Process]::Start($info)
try {
    $stdout=$process.StandardOutput.ReadToEndAsync()
    $stderr=$process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $audit=Join-Path $tool 'Evaluation/P5O2B25A3-constructed-generic-summary-context-audit.json'
    $parsed=Get-Content $audit -Raw -Encoding UTF8 | ConvertFrom-Json
    $result=[ordered]@{Package='P5O2B25A3';ExitCode=$process.ExitCode;AuditPassed=$parsed.Passed;EvidenceConsistent=$parsed.EvidenceConsistent;
        AuditSha256=(Get-FileHash $audit).Hash;FinishedUtc=[DateTime]::UtcNow.ToString('o');Stdout=$stdout.Result;Stderr=$stderr.Result;
        Capture='Actual child Process.ExitCode; native stderr retained separately, not converted into a parent PowerShell NativeCommandError'}
    [IO.File]::WriteAllText($destination,(($result | ConvertTo-Json -Depth 8)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Write-Host $stdout.Result
    if($stderr.Result) { Write-Host ('Retained stderr (see process record): '+$stderr.Result) }
    if($process.ExitCode -ne 0 -or -not $parsed.Passed -or -not $parsed.EvidenceConsistent) { exit 1 }
} finally { $process.Dispose() }
