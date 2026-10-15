param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$gate=Join-Path $ValidationRoot 'artifacts/dual-version-build'
$project=Join-Path $ValidationRoot 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
$runs=@(@{Name='main-external-target-tests';Class='ExceptionFlowExternalTargetCliTests';Trx='external-target.trx'},
    @{Name='main-bounded-finding-tests';Class='ExceptionFlowHistoricalBoundedFindingTests';Trx='bounded-findings.trx'})
$failed=[Collections.Generic.List[string]]::new()
Write-Host 'Explicit separate execution of the unchanged follow-on gate commands after the full gate failed fast; this does not turn the full script result into PASS.'
foreach($run in $runs) {
    $arguments=@('test',$project,'--no-build','--no-restore','-p:Configuration=Debug','--filter',('FullyQualifiedName~'+$run.Class),
        '--logger',('trx;LogFileName='+$run.Trx),'--results-directory',$gate)
    $previousPreference=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& dotnet @arguments 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previousPreference }
    [IO.File]::WriteAllText((Join-Path $gate ($run.Name+'.log')),$log,[Text.UTF8Encoding]::new($false))
    Write-Host $log
    if($code -ne 0) { $failed.Add($run.Name) }
}
if($failed.Count -ne 0) { throw ('Follow-on gate failures retained: '+($failed -join ', ')) }
