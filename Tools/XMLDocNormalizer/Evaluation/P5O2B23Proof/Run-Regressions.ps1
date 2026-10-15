param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$results=Join-Path $ValidationRoot 'artifacts/p5o2b23/tests'
[IO.Directory]::CreateDirectory($results) | Out-Null
$runs=@([ordered]@{Name='b23';Filter='FullyQualifiedName~ExceptionFlowExternalTargetCliTests'})
foreach($number in 22..10) {
    $names=@{22='DeclaredProductionFinding';21='DocumentationAttestation';20='ProductionAdmission';19='FindingContract';18='DeclaredReadiness';17='DeclaredPolicy';16='DeclaredBoundary';15='SolutionScope';14='Scope';13='SourceContext';12='ReferenceIdentity';11='InputEquivalence';10='PayloadProjection'}
    $runs+=@([ordered]@{Name=('b'+$number);Filter=('FullyQualifiedName~ExceptionFlowHistorical'+$names[$number]+'Tests')})
}
$runs+=@([ordered]@{Name='b5-b9';Filter='FullyQualifiedName~ExceptionFlowAnalysisRouterTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionPolicyTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionProjectionTests|FullyQualifiedName~ExceptionFlowAnalysisDispatchTests|FullyQualifiedName~ExceptionFlowProduction'})
# Preserve the established B22 group filters without maintaining a parallel class inventory.
$filters=Get-Content (Join-Path $PSScriptRoot '../P5O2B12Proof/RegressionFilters.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach($group in $filters) {
    $filter=$group.Filter+'|FullyQualifiedName~ExceptionFlowExternalTargetCliTests'
    if($group.Group -ne 'productive') {
        foreach($number in 22..12) { $filter+='|FullyQualifiedName~ExceptionFlowHistorical'+$names[$number]+'Tests' }
    }
    $runs+=@([ordered]@{Name=$group.Group;Filter=$filter})
}
$runs+=@([ordered]@{Name='full';Filter=$null})
foreach($run in $runs) {
    $arguments=@('test',(Join-Path $ValidationRoot 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'),
        '--no-build','--no-restore','--logger',('trx;LogFileName='+$run.Name+'.trx'),'--results-directory',$results)
    if($run.Filter) { $arguments+=@('--filter',$run.Filter) }
    if($run.Name -in @('broad','full')) { $arguments+='--blame' }
    $previousPreference=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& dotnet @arguments 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previousPreference }
    [IO.File]::WriteAllText((Join-Path $ValidationRoot ('artifacts/p5o2b23/'+$run.Name+'.log')),$log,[Text.UTF8Encoding]::new($false))
    Write-Host $log
    if($code -ne 0) { throw ($run.Name+' failed; retained log/TRX. No automatic retry or exclusion.') }
}
