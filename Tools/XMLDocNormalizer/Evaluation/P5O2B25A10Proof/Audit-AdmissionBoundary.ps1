$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$export = Join-Path $tool 'artifacts/b25a9-c777/Tools/XMLDocNormalizer'
$records = Join-Path $tool 'artifacts/p5o2b25a10/admission-boundary-audit'
if (Test-Path -LiteralPath $records) { throw 'Preserve the first boundary-audit run; no overwrite or green retry.' }
$sources = @(
    'Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowSummaryGraphThrowTests.cs',
    'Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowSummaryGraphDispatchCompletenessTests.cs',
    'Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowSummaryGraphLocalCallableTests.cs',
    'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs',
    'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs',
    'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionPipelineTests.cs',
    'Tests/XMLDocNormalizerTests/Helpers/ExceptionFlowSummaryGraphTestHelper.cs',
    'src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryAnalysisSession.GraphConstruction.cs',
    'src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisRouting.cs'
)
$bindings = @(foreach ($relative in $sources) {
    $workspaceHash = (Get-FileHash -LiteralPath (Join-Path $tool $relative)).Hash
    $exportHash = (Get-FileHash -LiteralPath (Join-Path $export $relative)).Hash
    if ($workspaceHash -cne $exportHash) { throw ('Inherited export source differs: ' + $relative) }
    [ordered]@{ Path = $relative; WorkspaceSha256 = $workspaceHash; InheritedExportSha256 = $exportHash }
})
$tests = @(
    'XMLDocNormalizerTests.Check.Semantic.Exception.ExceptionFlowSummaryGraphThrowTests.ThrowConstrainedTypeParameter_AddsConstraintAndUncertainty',
    'XMLDocNormalizerTests.Check.Semantic.Exception.ExceptionFlowSummaryGraphDispatchCompletenessTests.PublicInterface_RetainsKnownTargetAndAddsUncertainty',
    'XMLDocNormalizerTests.Check.Semantic.Exception.ExceptionFlowSummaryGraphLocalCallableTests.ReassignedDelegate_IsMarkedUncertain',
    'XMLDocNormalizerTests.Worker.ExceptionFlowAnalysisRouterTests.CurrentUncertainty_IsPreservedRatherThanCollapsedIntoWorkerCompletenessPolicy',
    'XMLDocNormalizerTests.Worker.ExceptionFlowAnalysisDispatchTests.CurrentUncertainty_IsNotReplacedByWorkerCompletenessPolicy',
    'XMLDocNormalizerTests.Worker.ExceptionFlowProductionPipelineTests.Current_UncertaintyIsRetainedRatherThanHistoricalCompleteness'
)
$assembly = Join-Path $export 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll'
$binary = [ordered]@{ Path = $assembly; Sha256 = (Get-FileHash -LiteralPath $assembly).Hash }
$filter = ($tests | ForEach-Object { 'FullyQualifiedName=' + $_ }) -join '|'
$arguments = @('vstest', $assembly, ('--TestCaseFilter:' + $filter), ('--ResultsDirectory:' + $records), '--Logger:trx;LogFileName=unchanged-boundary-tests.trx')
[IO.Directory]::CreateDirectory($records) | Out-Null
$started = [DateTime]::UtcNow.ToString('o')
Push-Location $tool
try {
    & dotnet @arguments 2>&1 | Tee-Object -FilePath (Join-Path $records 'unchanged-boundary-tests.log') | Write-Host
    $exitCode = $LASTEXITCODE
    [xml]$trx = Get-Content -LiteralPath (Join-Path $records 'unchanged-boundary-tests.trx') -Raw
    $counters = $trx.TestRun.ResultSummary.Counters
    $result = [ordered]@{
        Package = 'P5O2B25A10'; Kind = 'Fresh execution of inherited unchanged binary; not an implementation regression or fresh build'
        StartedUtc = $started; FinishedUtc = [DateTime]::UtcNow.ToString('o'); ExitCode = $exitCode
        Head = (& git --no-optional-locks rev-parse HEAD); BuildRun = $false
        Executable = 'dotnet'; Arguments = $arguments; Binary = $binary; SourceBindings = $bindings
        TestBinaryFreshlyCompiledOrPdbReverified = $false
        RequestedTests = $tests; Total = [int]$counters.total; Passed = [int]$counters.passed
        Failed = [int]$counters.failed; NotExecuted = [int]$counters.notExecuted
        ChangedProduction = $false; ChangedTests = $false; OutsideProfileImplementationValidated = $false
        Interpretation = 'Active legacy graph/diagnostic expectations confirmed; a sound A10 barrier is not implemented or tested by this replay'
    }
    [IO.File]::WriteAllText((Join-Path $records 'result.json'), (($result | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
    if ($exitCode -ne 0 -or [int]$counters.total -ne 6 -or [int]$counters.passed -ne 6 -or [int]$counters.notExecuted -ne 0) {
        throw 'Unchanged boundary replay did not pass all six requested tests; retain this result without retry.'
    }
} finally { Pop-Location }
