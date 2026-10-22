$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$destination = Join-Path $tool 'artifacts/p5o2b25a7/contract-gate-process.json'
if (Test-Path -LiteralPath $destination) { throw 'Retain actual A7 contract gate process record.' }
$info = [Diagnostics.ProcessStartInfo]::new('powershell', ('-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $PSScriptRoot 'Verify-Contract.ps1') + '"'))
$info.WorkingDirectory = $tool; $info.UseShellExecute = $false; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
$started = [DateTime]::UtcNow.ToString('o')
$process = [Diagnostics.Process]::Start($info)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit(); $code = $process.ExitCode
    $audit = Join-Path $tool 'Evaluation/P5O2B25A7-finite-parametric-generic-context-contract-audit.json'
    $parsed = Get-Content -LiteralPath $audit -Raw -Encoding UTF8 | ConvertFrom-Json
    $result = [ordered]@{ ExitCode = $code; StartedUtc = $started; FinishedUtc = [DateTime]::UtcNow.ToString('o'); StaticConsistencyPassed = $parsed.StaticConsistencyPassed; ContractDecision = $parsed.Decision; ImplementationGo = $parsed.ImplementationGo; AuditSha256 = (Get-FileHash -LiteralPath $audit).Hash; ActualProcessExit = $true; Stdout = $stdout.Result; Stderr = $stderr.Result }
    [IO.File]::WriteAllText($destination, (($result | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
    Write-Host $stdout.Result
    if ($stderr.Result) { Write-Host $stderr.Result }
    exit $code
} finally { $process.Dispose() }
