param([Parameter(Mandatory=$true)][string]$ValidationRoot, [string]$RepeatGroups, [string]$AdditionalFilter)
$ErrorActionPreference='Stop'
$results=Join-Path $ValidationRoot 'artifacts/p5o2b24/tests'
[IO.Directory]::CreateDirectory($results) | Out-Null
$names=@{24='BoundedFinding';22='DeclaredProductionFinding';21='DocumentationAttestation';20='ProductionAdmission';19='FindingContract';18='DeclaredReadiness';17='DeclaredPolicy';16='DeclaredBoundary';15='SolutionScope';14='Scope';13='SourceContext';12='ReferenceIdentity';11='InputEquivalence';10='PayloadProjection'}
$runs=@([ordered]@{Name='b24';Filter='FullyQualifiedName~ExceptionFlowHistoricalBoundedFindingTests'},
    [ordered]@{Name='b23';Filter='FullyQualifiedName~ExceptionFlowExternalTargetCliTests'})
foreach($number in 22..10) { $runs+=@([ordered]@{Name=('b'+$number);Filter=('FullyQualifiedName~ExceptionFlowHistorical'+$names[$number]+'Tests')}) }
$runs+=@([ordered]@{Name='b5-b9';Filter='FullyQualifiedName~ExceptionFlowAnalysisRouterTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionPolicyTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionProjectionTests|FullyQualifiedName~ExceptionFlowAnalysisDispatchTests|FullyQualifiedName~ExceptionFlowProduction'})
$filters=Get-Content (Join-Path $PSScriptRoot '../P5O2B12Proof/RegressionFilters.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach($group in $filters) {
    $filter=$group.Filter+'|FullyQualifiedName~ExceptionFlowExternalTargetCliTests|FullyQualifiedName~ExceptionFlowHistoricalBoundedFindingTests'
    if($group.Group -ne 'productive') { foreach($number in 22..12) { $filter+='|FullyQualifiedName~ExceptionFlowHistorical'+$names[$number]+'Tests' } }
    $runs+=@([ordered]@{Name=$group.Group;Filter=$filter})
}
$runs+=@([ordered]@{Name='full';Filter=$null})
if($RepeatGroups) {
    # Explicit manual repeats only, after all configured groups have already run.
    $initial=Join-Path $PSScriptRoot '../../artifacts/p5o2b24/attempt-initial-regression/tests'
    foreach($run in $runs) { if(-not (Test-Path (Join-Path $initial ($run.Name+'.trx')))) { throw 'Run the complete initial regression set before selecting repeats.' } }
    $selected=$RepeatGroups.Split(',')
    foreach($name in $selected) { if(@($runs | Where-Object Name -eq $name).Count -ne 1) { throw "Unknown repeat group: $name" } }
    $runs=@($runs | Where-Object { $selected -ccontains $_.Name })
    Write-Host ('Explicit repeats of already executed groups (no test exclusions): '+$RepeatGroups)
}
$failed=[Collections.Generic.List[string]]::new()
foreach($run in $runs) {
    if($AdditionalFilter -and $run.Name -in @('productive','focused','broad')) { $run.Filter+='|'+$AdditionalFilter }
    $arguments=@('test',(Join-Path $ValidationRoot 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'),
        '--no-build','--no-restore','--logger',('trx;LogFileName='+$run.Name+'.trx'),'--results-directory',$results)
    if($run.Filter) { $arguments+=@('--filter',$run.Filter) }
    if($run.Name -in @('broad','full')) { $arguments+='--blame' }
    $previousPreference=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& dotnet @arguments 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previousPreference }
    [IO.File]::WriteAllText((Join-Path $ValidationRoot ('artifacts/p5o2b24/'+$run.Name+'.log')),$log,[Text.UTF8Encoding]::new($false))
    Write-Host $log
    if($code -ne 0) { $failed.Add($run.Name); Write-Warning ($run.Name+' failed; retained log/TRX. Continuing required groups, without retry or exclusion.') }
}
if($failed.Count -ne 0) { throw ('Required regression groups failed: '+($failed -join ', ')+'. All requested groups were executed; failures are not suppressed.') }
