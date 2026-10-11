param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$proofRoot=$PSScriptRoot
$results=Join-Path $ValidationRoot 'artifacts/p5o2b12/tests'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$b5b9='FullyQualifiedName~ExceptionFlowAnalysisRouterTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionPolicyTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionProjectionTests|FullyQualifiedName~ExceptionFlowAnalysisDispatchTests|FullyQualifiedName~ExceptionFlowProduction'
$filters=Get-Content (Join-Path $proofRoot 'RegressionFilters.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$runs=@(
    [ordered]@{Name='b12';Filter='FullyQualifiedName~ExceptionFlowHistoricalReferenceIdentityTests'},
    [ordered]@{Name='b11';Filter='FullyQualifiedName~ExceptionFlowHistoricalInputEquivalenceTests'},
    [ordered]@{Name='b10';Filter='FullyQualifiedName~ExceptionFlowHistoricalPayloadProjectionTests'},
    [ordered]@{Name='b5-b9';Filter=$b5b9}
)
foreach($group in $filters) {
    $filter=$group.Filter
    if($group.Group -ne 'productive') { $filter+='|FullyQualifiedName~ExceptionFlowHistoricalReferenceIdentityTests' }
    $runs+=@([ordered]@{Name=$group.Group;Filter=$filter})
}
$runs+=@([ordered]@{Name='full';Filter=$null})
foreach($run in $runs) {
    $arguments=@('test',(Join-Path $ValidationRoot 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'),
        '--no-build','--no-restore','--logger',('trx;LogFileName='+$run.Name+'.trx'),'--results-directory',$results)
    if($run.Filter) { $arguments+=@('--filter',$run.Filter) }
    $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& dotnet @arguments 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    $log | Set-Content (Join-Path $ValidationRoot ('artifacts/p5o2b12/'+$run.Name+'.log')) -Encoding UTF8
    Write-Host $log
    if($code -ne 0) { throw ($run.Name+' failed; retained log/TRX. No automatic retry or exclusion.') }
}
