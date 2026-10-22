param([Parameter(Mandatory = $true)][ValidateSet('Build', 'Dual', 'Capture', 'Tests', 'Broad', 'Full', 'Boundary')][string]$Phase)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a6-r2'
$validation = (Get-Content (Join-Path $records 'export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$runs = Join-Path $records 'runs'
[IO.Directory]::CreateDirectory($runs) | Out-Null
function Recorded([string]$name, [string]$exe, [string[]]$arguments) {
    $record = Join-Path $runs ($name + '.json')
    if (Test-Path -LiteralPath $record) { throw ('Attempt exists, no retry: ' + $name) }
    $started = [DateTime]::UtcNow.ToString('o')
    [IO.File]::WriteAllText($record, ([ordered]@{ Name = $name; StartedUtc = $started; ExitCode = $null; Arguments = $arguments } | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $exe; $info.WorkingDirectory = $validation; $info.UseShellExecute = $false
    $info.Arguments = (@($arguments | ForEach-Object { '"' + ($_ -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"' }) -join ' ')
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($info)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit(); $code = $process.ExitCode
        $log = $stdout.Result + "`r`nSTDERR:`r`n" + $stderr.Result
        [IO.File]::WriteAllText((Join-Path $runs ($name + '.log')), $log, [Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText($record, (([ordered]@{ Name = $name; StartedUtc = $started; FinishedUtc = [DateTime]::UtcNow.ToString('o'); ExitCode = $code; Executable = $exe; Arguments = $arguments } | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
        Write-Host $log
    } finally { $process.Dispose() }
    if ($code -ne 0) { throw ($name + ' failed, retained without retry.') }
}
Push-Location $validation
try {
    $project = Join-Path $validation 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
    $tests = Join-Path $validation 'artifacts/p5o2b25a6/tests'
    if ($Phase -eq 'Build') { Recorded 'build' 'dotnet' @('build', $project, '-warnaserror') }
    if ($Phase -eq 'Dual') { Recorded 'dual' 'powershell' @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $validation 'build/Verify-DualVersionBuild.ps1'), '-RunMainBoundaryTests', '-BuildTestProjectReferences') }
    if ($Phase -eq 'Capture') {
        $path = Join-Path $records 'validation-inputs.json'
        if (Test-Path $path) { throw 'Capture once, never rewrite compiled inputs.' }
        $shared = @([xml](Get-Content build/ExceptionFlow.HistoricalSources.props -Raw -Encoding UTF8)).Project.ItemGroup.Compile.Include
        $sources = @(foreach ($relative in @('Tests/XMLDocNormalizerTests/Worker/ExceptionFlowGenericExpansionBoundaryTests.cs')) { [ordered]@{ Path = $relative; WorkspaceSha256 = (Get-FileHash (Join-Path $tool $relative)).Hash; ExportSha256 = (Get-FileHash (Join-Path $validation $relative)).Hash } })
        $sharedSources = @(foreach ($include in $shared) {
            $relative = $include.Replace('$(MSBuildThisFileDirectory)', 'build/').Replace('\', '/')
            $full = [IO.Path]::GetFullPath((Join-Path $validation $relative))
            $relative = $full.Substring($validation.Length + 1).Replace('\', '/')
            [ordered]@{ Path = $relative; WorkspaceSha256 = (Get-FileHash (Join-Path $tool $relative)).Hash; ExportSha256 = (Get-FileHash $full).Hash }
        })
        $images = @(foreach ($relative in @('src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll', 'src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll', 'artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll', 'artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll', 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll')) { [ordered]@{ Path = $relative; Sha256 = (Get-FileHash (Join-Path $validation $relative)).Hash } })
        [IO.File]::WriteAllText($path, (([ordered]@{ CapturedUtc = [DateTime]::UtcNow.ToString('o'); Sources = $sources; SharedSources = $sharedSources; Images = $images } | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
    }
    if ($Phase -eq 'Tests') {
        Recorded 'witnesses' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~ExceptionFlowGenericExpansionBoundaryTests', '--logger', 'trx;LogFileName=witnesses.trx', '--results-directory', $tests)
        Recorded 'contexts' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~ExceptionFlowGenericExpansionBoundaryTests|FullyQualifiedName~ExceptionFlowRecursiveSummaryClosureTests|FullyQualifiedName~ExceptionFlowBoundedGenericContextTests|FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests', '--logger', 'trx;LogFileName=contexts.trx', '--results-directory', $tests)
    }
    if ($Phase -eq 'Broad') {
        $groups = Get-Content (Join-Path $tool 'Evaluation/P5O2B12Proof/RegressionFilters.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        $filter = @($groups | Where-Object Group -ceq 'broad')[0].Filter + '|FullyQualifiedName~ExceptionFlowExternalTargetCliTests|FullyQualifiedName~ExceptionFlowHistoricalBoundedFindingTests'
        $names = @{ 22 = 'DeclaredProductionFinding'; 21 = 'DocumentationAttestation'; 20 = 'ProductionAdmission'; 19 = 'FindingContract'; 18 = 'DeclaredReadiness'; 17 = 'DeclaredPolicy'; 16 = 'DeclaredBoundary'; 15 = 'SolutionScope'; 14 = 'Scope'; 13 = 'SourceContext'; 12 = 'ReferenceIdentity' }
        foreach ($number in 22..12) { $filter += '|FullyQualifiedName~ExceptionFlowHistorical' + $names[$number] + 'Tests' }
        $filter += '|FullyQualifiedName~HistoricalWorkerLifetimeTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests|FullyQualifiedName~ExceptionFlowBoundedGenericContextTests|FullyQualifiedName~ExceptionFlowRecursiveSummaryClosureTests|FullyQualifiedName~ExceptionFlowGenericExpansionBoundaryTests'
        Recorded 'broad' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', $filter, '--blame', '--logger', 'trx;LogFileName=broad.trx', '--results-directory', $tests)
    }
    if ($Phase -eq 'Full') { Recorded 'full' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--blame', '--logger', 'trx;LogFileName=full.trx', '--results-directory', $tests) }
    if ($Phase -eq 'Boundary') {
        $line = Get-Content 'build/Verify-DualVersionBuild.ps1' -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
        if ($line -notmatch "'--filter', '([^']+)'") { throw 'Boundary filter unavailable.' }
        Recorded 'boundary' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', $Matches[1], '--logger', 'trx;LogFileName=boundary.trx', '--results-directory', $tests)
    }
} finally { Pop-Location }
