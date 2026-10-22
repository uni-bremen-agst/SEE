param([Parameter(Mandatory = $true)][ValidateSet('Baseline', 'Candidate', 'Regressions', 'Boundary', 'Dual')][string]$Phase)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation = (Get-Content (Join-Path $tool 'artifacts/p5o2b25a5/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$records = Join-Path $tool 'artifacts/p5o2b25a5/runs'
[IO.Directory]::CreateDirectory($records) | Out-Null
function Recorded([string]$Name, [string]$Exe, [string[]]$Arguments) {
    $record = Join-Path $records ($Name + '.json')
    if (Test-Path $record) { throw ('Named attempt already exists; no retry: ' + $Name) }
    $started = [DateTime]::UtcNow.ToString('o')
    [IO.File]::WriteAllText($record, ([ordered]@{ Name = $Name; StartedUtc = $started; ExitCode = $null; State = 'Running or interrupted'; Arguments = $Arguments } | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $Exe; $info.WorkingDirectory = $validation; $info.UseShellExecute = $false
    $info.Arguments = (@($Arguments | ForEach-Object { '"' + ($_ -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"' }) -join ' ')
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($info)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit(); $code = $process.ExitCode
        $log = $stdout.Result + "`r`nSTDERR:`r`n" + $stderr.Result
        [IO.File]::WriteAllText((Join-Path $records ($Name + '.log')), $log, [Text.UTF8Encoding]::new($false))
        Write-Host $log
        [IO.File]::WriteAllText($record, (([ordered]@{ Name = $Name; StartedUtc = $started; FinishedUtc = [DateTime]::UtcNow.ToString('o'); ExitCode = $code; Executable = $Exe; Arguments = $Arguments; Stderr = $stderr.Result } | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
    } finally { $process.Dispose() }
    if ($code -ne 0) { throw ($Name + ' failed; original attempt retained, no automatic retry.') }
}
Push-Location $validation
try {
    $project = Join-Path $validation 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
    $tests = Join-Path $validation 'artifacts/p5o2b25a5/tests'
    if ($Phase -eq 'Baseline') {
        Recorded 'baseline-build' 'dotnet' @('build', $project, '-warnaserror')
        Recorded 'baseline-current' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~ExceptionFlowProductionCurrentParityTests', '--logger', 'trx;LogFileName=baseline-current.trx', '--results-directory', $tests)
        $baseline = Join-Path $tool 'artifacts/p5o2b25a5/baseline'
        if (Test-Path $baseline) { throw 'Baseline already captured.' }
        [IO.Directory]::CreateDirectory($baseline) | Out-Null
        Copy-Item 'artifacts/p5o2b9/current' (Join-Path $baseline 'current') -Recurse
    }
    if ($Phase -eq 'Candidate') {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B23Proof/Sync-Validation.ps1') -ValidationRoot $validation
        if ($LASTEXITCODE -ne 0) { throw 'Candidate overlay failed.' }
        Recorded 'candidate-build' 'dotnet' @('build', $project, '--no-restore', '-warnaserror')
        Recorded 'candidate-current' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~ExceptionFlowRecursiveSummaryClosureTests&FullyQualifiedName!~GenuineWorker', '--logger', 'trx;LogFileName=candidate-current.trx', '--results-directory', $tests)
        Recorded 'candidate-regressions' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~ExceptionFlowSummaryGraphEvaluationTests|FullyQualifiedName~ExceptionFlowProductionCurrentParityTests|FullyQualifiedName~ExceptionFlowSummaryComponentDependencyTests|FullyQualifiedName~ExceptionFlowBoundedGenericContextTests.FiniteMutualCycle', '--logger', 'trx;LogFileName=candidate-regressions.trx', '--results-directory', $tests)
    }
    if ($Phase -eq 'Regressions') {
        Recorded 'contexts' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~ExceptionFlowRecursiveSummaryClosureTests|FullyQualifiedName~ExceptionFlowBoundedGenericContextTests|FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests', '--logger', 'trx;LogFileName=contexts.trx', '--results-directory', $tests)
        Recorded 'pid-g1' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~HistoricalWorkerLifetimeTests', '--logger', 'trx;LogFileName=pid-g1.trx', '--results-directory', $tests)
        Recorded 'pid-g1-proof' 'powershell' @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $tool 'Evaluation/PIDG1Proof/Test-ProcessLifetime.ps1'), '-OutputPath', (Join-Path $records 'pid-g1-proof-cases.json'))
        Recorded 'regressions' 'powershell' @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $tool 'Evaluation/P5O2B24Proof/Run-Regressions.ps1'), '-ValidationRoot', $validation, '-AdditionalFilter', 'FullyQualifiedName~HistoricalWorkerLifetimeTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests|FullyQualifiedName~ExceptionFlowBoundedGenericContextTests|FullyQualifiedName~ExceptionFlowRecursiveSummaryClosureTests')
    }
    if ($Phase -eq 'Boundary') {
        $line = Get-Content 'build/Verify-DualVersionBuild.ps1' -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
        if ($line -notmatch "'--filter', '([^']+)'") { throw 'Boundary filter not found.' }
        Recorded 'boundary' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', $Matches[1], '--logger', 'trx;LogFileName=boundary.trx', '--results-directory', $tests)
    }
    if ($Phase -eq 'Dual') { Recorded 'dual' 'powershell' @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $validation 'build/Verify-DualVersionBuild.ps1'), '-RunMainBoundaryTests', '-BuildTestProjectReferences') }
} finally { Pop-Location }
