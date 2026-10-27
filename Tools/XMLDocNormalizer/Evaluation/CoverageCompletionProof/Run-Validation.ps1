param([Parameter(Mandatory=$true)][ValidateSet('Targeted','TargetedPrepared','TargetedFinal','ProjectionFinal','Broad','Boundary','Full','Dual')][string]$Phase)
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$export=(Get-Content (Join-Path $PSScriptRoot 'export.json') -Raw|ConvertFrom-Json).Export
$results=Join-Path $PSScriptRoot 'Results';$null=New-Item -ItemType Directory -Path $results -Force
$name=$Phase.ToLowerInvariant();$record=Join-Path $results ($name+'.json')
if($Phase -eq 'TargetedPrepared'){$name='targeted-prepared';$record=Join-Path $results ($name+'.json')}
if($Phase -eq 'TargetedFinal'){$name='targeted-final';$record=Join-Path $results ($name+'.json')}
if($Phase -eq 'ProjectionFinal'){$name='projection-final';$record=Join-Path $results ($name+'.json')}
if(Test-Path $record){throw 'Preserve previous gate; no green retries'}
if($Phase -eq 'TargetedPrepared'){
 $cause=Get-Content (Join-Path $PSScriptRoot 'targeted-precondition-audit.json') -Raw|ConvertFrom-Json
 if(!$cause.AllFailuresMissingStaticFixture -or $cause.Failed -ne 45){throw 'Unexpected prior failure; do not rerun'}
 if(!(Test-Path (Join-Path $PSScriptRoot 'prepared-fixtures.json'))){throw 'Missing corrected-runtime-input evidence'}
}
foreach($required in @('artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll','artifacts/p5o2b4/test-process/bin/Debug/net8.0/BoundaryTestProcess.dll','artifacts/exception-flow-historical/HistoricalPdbFixture/bin/Debug/net8.0/HistoricalPdbFixture.dll','artifacts/p5o2b7/historical-pdb/historical-input.dll','artifacts/p5o2b7/historical-pdb/historical-input.pdb')){
 if(!(Test-Path (Join-Path $export $required))){throw ('Prepare fixture before running any gate: '+$required)}
}
$previous=@{TargetedFinal='targeted-prepared';ProjectionFinal='targeted-final';Broad='projection-final';Boundary='broad';Full='boundary';Dual='full'}
if($previous.ContainsKey($Phase)){
 $prior=Get-Content (Join-Path $results ($previous[$Phase]+'.json')) -Raw|ConvertFrom-Json
 if($null -eq $prior.ExitCode -or $prior.ExitCode -ne 0){throw 'Previous gate must pass'}
}
$arguments=@('test','Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj','--no-build','--no-restore')
if($Phase -in @('Targeted','TargetedPrepared')){
 $arguments+=@('--filter','FullyQualifiedName~CoverageCompletion|FullyQualifiedName~ExceptionFlow|FullyQualifiedName~HistoricalWorker|FullyQualifiedName~HistoricalAnalyzer|FullyQualifiedName~FindingsReporter|FullyQualifiedName~ToolRunnerComparison|FullyQualifiedName~RunResultClone|FullyQualifiedName~ModeComparisonReportWriter')
}
if($Phase -eq 'TargetedFinal'){
 $arguments+=@('--filter','FullyQualifiedName~CoverageCompletion|FullyQualifiedName~FindingsReporter|FullyQualifiedName~RunResultClone|FullyQualifiedName~ExceptionFlowExternalTargetCliTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionAdoptionTests|FullyQualifiedName~ExceptionFlowHistoricalBoundedFindingTests|FullyQualifiedName~DOC631|FullyQualifiedName~DOC632|FullyQualifiedName~Uncertainty')
}
if($Phase -eq 'ProjectionFinal'){
 $arguments+=@('--filter','FullyQualifiedName~CoverageCompletion|FullyQualifiedName~FindingsReporter|FullyQualifiedName~RunResultClone|FullyQualifiedName~ExceptionFlowExternalTargetCliTests')
}
if($Phase -eq 'Broad'){
 $groups=Get-Content (Join-Path $tool 'Evaluation/P5O2B12Proof/RegressionFilters.json') -Raw|ConvertFrom-Json
 $filter=@($groups|Where-Object Group -ceq 'broad')[0].Filter
 $filter+='|FullyQualifiedName~ExceptionFlow|FullyQualifiedName~HistoricalWorker|FullyQualifiedName~HistoricalAnalyzer|FullyQualifiedName~CoverageCompletion'
 $arguments+=@('--filter',$filter)
}
if($Phase -eq 'Boundary'){
 $line=Get-Content (Join-Path $export 'build/Verify-DualVersionBuild.ps1')|Where-Object{$_ -match "Invoke-DotNet 'main-boundary-tests' "}
 if($line -notmatch "'--filter', '([^']+)'"){throw 'Existing Boundary filter unavailable'}
 $arguments+=@('--filter',$Matches[1])
}
$arguments+=@('--blame','--logger',('trx;LogFileName='+$name+'.trx'),'--results-directory','artifacts/coverage/results','-v:minimal')
$exe='dotnet'
if($Phase -eq 'Dual'){$exe='powershell';$arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File','build/Verify-DualVersionBuild.ps1','-RunMainBoundaryTests','-BuildTestProjectReferences')}
$log=Join-Path $results ($name+'.log')
if($Phase -eq 'Dual'){
 $raw=Join-Path $tool 'artifacts/evaluation/CoverageCompletion/closure';$null=New-Item -ItemType Directory -Path $raw -Force
 $log=Join-Path $raw 'dual.log';if(Test-Path $log){throw 'Preserve previous raw attempt'}
}
$state=[ordered]@{Phase=$Phase;Export=$export;Executable=$exe;Arguments=$arguments;StartedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$null;CompletedUtc=$null;Counters=$null;TransientLog=$log}
$state|ConvertTo-Json -Depth 7|Set-Content $record -Encoding UTF8
Push-Location $export
try{$ErrorActionPreference='Continue';& $exe @arguments *> $log;$state.ExitCode=$LASTEXITCODE}
finally{$ErrorActionPreference='Stop';Pop-Location}
$state.CompletedUtc=[DateTime]::UtcNow.ToString('o')
if($Phase -ne 'Dual'){
 $trx=Join-Path $export ('artifacts/coverage/results/'+$name+'.trx')
 if(Test-Path $trx){
  Copy-Item -LiteralPath $trx -Destination (Join-Path $results ($name+'.trx'))
  [xml]$xml=Get-Content $trx -Raw;$state.Counters=$xml.TestRun.ResultSummary.Counters|Select-Object total,executed,passed,failed,notExecuted
  if($state.Counters.failed -ne '0' -or $state.Counters.notExecuted -ne '0' -or $state.Counters.total -eq '0'){$state.ExitCode=1}
 } else {$state.ExitCode=1}
}
$state|ConvertTo-Json -Depth 7|Set-Content $record -Encoding UTF8
Get-Content $log -Tail 13
exit $state.ExitCode
