param(
    [Parameter(Mandatory=$true)][ValidateSet('Broad','Full','Boundary','Dual')][string]$Phase,
    [ValidatePattern('^[0-9]{2}$')][string]$Revision = '13'
)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation = Join-Path $tool ('artifacts/b25a10-development-' + $Revision)
$arguments = @('test','Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj','--no-build','--no-restore')
if ($Phase -eq 'Broad') {
    $groups = Get-Content (Join-Path $tool 'Evaluation/P5O2B12Proof/RegressionFilters.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $filter = @($groups | Where-Object Group -ceq 'broad')[0].Filter + '|FullyQualifiedName~ExceptionFlowExternalTargetCliTests|FullyQualifiedName~ExceptionFlowHistoricalBoundedFindingTests'
    $names = @{ 22='DeclaredProductionFinding';21='DocumentationAttestation';20='ProductionAdmission';19='FindingContract';18='DeclaredReadiness';17='DeclaredPolicy';16='DeclaredBoundary';15='SolutionScope';14='Scope';13='SourceContext';12='ReferenceIdentity' }
    foreach ($number in 22..12) { $filter += '|FullyQualifiedName~ExceptionFlowHistorical' + $names[$number] + 'Tests' }
    $filter += '|FullyQualifiedName~HistoricalWorkerLifetimeTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests|FullyQualifiedName~ExceptionFlowBoundedGenericContextTests|FullyQualifiedName~ExceptionFlowRecursiveSummaryClosureTests|FullyQualifiedName~ExceptionFlowGenericExpansionBoundaryTests|FullyQualifiedName~ExceptionFlowFiniteOrbitAdmissionTests'
    $arguments += @('--filter',$filter)
}
if ($Phase -eq 'Boundary') {
    $line = Get-Content (Join-Path $validation 'build/Verify-DualVersionBuild.ps1') -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
    if ($line -notmatch "'--filter', '([^']+)'") { throw 'Boundary filter unavailable.' }
    $arguments += @('--filter',$Matches[1])
}
$arguments += @('--blame','--logger',('trx;LogFileName=' + $Phase.ToLowerInvariant() + '.trx'),'--results-directory','artifacts/a10-closure-results','-v:minimal')
$executable = 'dotnet'
if ($Phase -eq 'Dual') {
    $executable = 'powershell'
    $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $validation 'build/Verify-DualVersionBuild.ps1'),'-RunMainBoundaryTests','-BuildTestProjectReferences')
}
& (Join-Path $PSScriptRoot 'Run-Recorded.ps1') -Name ('development-' + $Revision + '-' + $Phase.ToLowerInvariant()) -Executable $executable -WorkingDirectory $validation -Arguments $arguments
exit $LASTEXITCODE
