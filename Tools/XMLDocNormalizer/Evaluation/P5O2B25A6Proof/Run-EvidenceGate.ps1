$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$destination = Join-Path $tool 'artifacts/p5o2b25a6-r2/evidence-gate-process.json'
if (Test-Path -LiteralPath $destination) { throw 'Retain actual gate attempt; no overwrite.' }
$info = [Diagnostics.ProcessStartInfo]::new('powershell', ('-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $PSScriptRoot 'Verify-Evidence.ps1') + '"'))
$info.WorkingDirectory = $tool; $info.UseShellExecute = $false; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
$started = [DateTime]::UtcNow.ToString('o')
$process = [Diagnostics.Process]::Start($info)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit(); $code = $process.ExitCode
    $audit = Join-Path $tool 'Evaluation/P5O2B25A6-bounded-generic-context-expansion-after-scc-audit.json'
    $parsed = if (Test-Path $audit) { Get-Content $audit -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
    $result = [ordered]@{ ExitCode = $code; StartedUtc = $started; FinishedUtc = [DateTime]::UtcNow.ToString('o'); AuditPassed = $parsed.Passed; EvidenceConsistent = $parsed.EvidenceConsistent; AuditSha256 = $(if (Test-Path $audit) { (Get-FileHash $audit).Hash } else { $null }); Stdout = $stdout.Result; Stderr = $stderr.Result; ActualProcessExit = $true }
    [IO.File]::WriteAllText($destination, (($result | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
    Write-Host $stdout.Result
    if ($stderr.Result) { Write-Host $stderr.Result }
    exit $code
} finally { $process.Dispose() }
