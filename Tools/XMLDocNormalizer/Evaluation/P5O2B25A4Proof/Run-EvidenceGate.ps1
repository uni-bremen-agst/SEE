param([string]$OutputPath='artifacts/p5o2b25a4/evidence-gate-process.json')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$destination=Join-Path $tool $OutputPath
if(Test-Path -LiteralPath $destination) { throw 'Preserve actual process records; do not overwrite.' }
$info=[Diagnostics.ProcessStartInfo]::new('powershell',('-NoProfile -ExecutionPolicy Bypass -File "'+(Join-Path $PSScriptRoot 'Write-Verify-Evidence.ps1')+'"'))
$info.WorkingDirectory=$tool; $info.UseShellExecute=$false; $info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
$process=[Diagnostics.Process]::Start($info)
try {
    $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $audit=Join-Path $tool 'Evaluation/P5O2B25A4-bounded-generic-context-expansion-audit.json'
    $parsed=if(Test-Path $audit) { Get-Content $audit -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
    $result=[ordered]@{Package='P5O2B25A4';ExitCode=$process.ExitCode;AuditPassed=$parsed.Passed;EvidenceConsistent=$parsed.EvidenceConsistent;AuditSha256=$(if(Test-Path $audit){(Get-FileHash $audit).Hash}else{$null});FinishedUtc=[DateTime]::UtcNow.ToString('o');Stdout=$stdout.Result;Stderr=$stderr.Result;Capture='Actual child Process.ExitCode; stdout/stderr retained separately'}
    [IO.File]::WriteAllText($destination,(($result | ConvertTo-Json -Depth 8)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Write-Host $stdout.Result
    if($stderr.Result) { Write-Host ('Retained stderr: '+$stderr.Result) }
    if($process.ExitCode -ne 0 -or -not $parsed.Passed -or -not $parsed.EvidenceConsistent) { exit 1 }
} finally { $process.Dispose() }
