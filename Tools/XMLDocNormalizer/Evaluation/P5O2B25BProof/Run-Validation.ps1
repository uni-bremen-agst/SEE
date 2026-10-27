param([Parameter(Mandatory=$true)][ValidateSet('Targeted','Broad','Boundary','Full','HarnessIsolated','BroadHarness','FullHarness','Dual')][string]$Phase)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation = (Get-Content (Join-Path $PSScriptRoot 'implementation-export.json') -Raw | ConvertFrom-Json).Export
$records = Join-Path $PSScriptRoot 'Results'
New-Item -ItemType Directory -Path $records -Force | Out-Null
$name = $Phase.ToLowerInvariant()
if ($Phase -eq 'FullHarness') { $name='full-harness' }
if ($Phase -eq 'HarnessIsolated') { $name='harness-isolated' }
if ($Phase -eq 'BroadHarness') { $name='broad-harness' }
$record = Join-Path $records ($name + '.json')
if (Test-Path -LiteralPath $record) { throw 'Preserve previous attempt; no green retries' }
$previous = @{ Broad='targeted'; Boundary='broad'; Full='boundary'; BroadHarness='harness-isolated'; FullHarness='broad-harness'; Dual='full-harness' }
if ($previous.ContainsKey($Phase)) {
    $prior = Get-Content (Join-Path $records ($previous[$Phase] + '.json')) -Raw | ConvertFrom-Json
    if ($prior.ExitCode -ne 0 -or $null -eq $prior.ExitCode) { throw 'Previous gate must pass' }
}
$arguments = @('test','Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj','--no-build','--no-restore')
if ($Phase -in @('HarnessIsolated','BroadHarness','FullHarness')) {
    [xml]$red=Get-Content (Join-Path $records 'full.trx') -Raw
    $failed=@($red.TestRun.Results.UnitTestResult | Where-Object outcome -ne 'Passed')
    if ($failed.Count -ne 1 -or $failed[0].testName -cne 'XMLDocNormalizerTests.Execution.Semantic.ExternalSourceLineEndingReconstructionTests.ConcurrentReconstruction_PerformsOneRequestAndOneAttempt') { throw 'Unexpected Full failure; no harness closure authorized' }
    $delta=Get-Content (Join-Path $records 'harness-coordination-delta.json') -Raw | ConvertFrom-Json
    if (!$delta.ExactCoordinationWrapperOnly -or !$delta.AllExistingAssertionsUnchanged -or !$delta.TimeoutUnchanged) { throw 'Not a local unchanged-contract scheduling correction' }
}
if ($Phase -eq 'HarnessIsolated') {
    $arguments += @('--filter','FullyQualifiedName~ExternalSourceLineEndingReconstructionTests.ConcurrentReconstruction_PerformsOneRequestAndOneAttempt')
}
if ($Phase -eq 'Targeted') {
    $arguments += @('--filter','FullyQualifiedName~ExceptionFlow|FullyQualifiedName~HistoricalWorker|FullyQualifiedName~HistoricalAnalyzer')
}
if ($Phase -in @('Broad','BroadHarness')) {
    $groups = Get-Content (Join-Path $tool 'Evaluation/P5O2B12Proof/RegressionFilters.json') -Raw | ConvertFrom-Json
    $filter = @($groups | Where-Object Group -ceq 'broad')[0].Filter + '|FullyQualifiedName~ExceptionFlowExternalTargetCliTests|FullyQualifiedName~ExceptionFlowHistoricalBoundedFindingTests'
    $names = @{22='DeclaredProductionFinding';21='DocumentationAttestation';20='ProductionAdmission';19='FindingContract';18='DeclaredReadiness';17='DeclaredPolicy';16='DeclaredBoundary';15='SolutionScope';14='Scope';13='SourceContext';12='ReferenceIdentity'}
    foreach ($number in 22..12) { $filter += '|FullyQualifiedName~ExceptionFlowHistorical' + $names[$number] + 'Tests' }
    foreach ($class in @('HistoricalWorkerLifetimeTests','ExceptionFlowHistoricalSolutionFindingTests','ExceptionFlowAssemblyQualifiedOwnershipTests','ExceptionFlowCanonicalExceptionIdentityTests','ExceptionFlowConstructedInvocationContextTests','ExceptionFlowBoundedGenericContextTests','ExceptionFlowRecursiveSummaryClosureTests','ExceptionFlowGenericExpansionBoundaryTests','ExceptionFlowFiniteOrbitAdmissionTests','ExceptionFlowHistoricalSolutionAdoptionTests')) { $filter += '|FullyQualifiedName~' + $class }
    $arguments += @('--filter',$filter)
}
if ($Phase -eq 'Boundary') {
    $line = Get-Content (Join-Path $validation 'build/Verify-DualVersionBuild.ps1') | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
    if ($line -notmatch "'--filter', '([^']+)'") { throw 'Missing existing Boundary filter' }
    $arguments += @('--filter',$Matches[1])
}
$arguments += @('--blame','--logger',('trx;LogFileName=' + $name + '.trx'),'--results-directory','artifacts/p5o2b25b/results','-v:minimal')
$exe = 'dotnet'
if ($Phase -eq 'Dual') {
    $exe = 'powershell'
    $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File','build/Verify-DualVersionBuild.ps1','-RunMainBoundaryTests','-BuildTestProjectReferences')
}
$state = [ordered]@{ Phase=$Phase; StartedUtc=[DateTime]::UtcNow.ToString('o'); Export=$validation; Executable=$exe; Arguments=$arguments; ExitCode=$null }
$state | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $record -Encoding UTF8
Push-Location $validation
try {
    $ErrorActionPreference = 'Continue'
    & $exe @arguments > (Join-Path $records ($name + '.log')) 2>&1
    $state.ExitCode = $LASTEXITCODE
} finally { $ErrorActionPreference='Stop'; Pop-Location }
$state.CompletedUtc = [DateTime]::UtcNow.ToString('o')
if ($Phase -ne 'Dual') {
    $trx = Join-Path $validation ('artifacts/p5o2b25b/results/' + $name + '.trx')
    Copy-Item -LiteralPath $trx -Destination (Join-Path $records ($name + '.trx'))
    [xml]$result = Get-Content -LiteralPath $trx -Raw
    $state.Counters = $result.TestRun.ResultSummary.Counters | Select-Object total,executed,passed,failed,notExecuted
    if ($state.Counters.failed -ne '0' -or $state.Counters.notExecuted -ne '0' -or $state.Counters.total -eq '0') { $state.ExitCode = 1 }
}
$state | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $record -Encoding UTF8
if ($Phase -ne 'Dual') {
    # Storage only: final gate record is unchanged; W4 is content-addressed, never bulk-copied per gate.
    Import-Module (Join-Path $PSScriptRoot 'EvidenceClosure/EvidenceStorage.psm1') -Force
    Save-B25BEvidence -ValidationRoot $validation -RecordsRoot $records -Gate $name
}
Get-Content (Join-Path $records ($name + '.log')) -Tail 12
exit $state.ExitCode
