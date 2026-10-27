$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records=Join-Path $PSScriptRoot 'Results'
$output=Join-Path $PSScriptRoot 'implementation-audit.json'
if(Test-Path -LiteralPath $output) { throw 'Preserve existing closure record' }
$gate=Get-Content (Join-Path $records 'b25b-gates.json') -Raw | ConvertFrom-Json
$protection=Get-Content (Join-Path $PSScriptRoot 'final-protection.json') -Raw | ConvertFrom-Json
if(!$gate.Passed -or !$protection.Passed) { throw 'All gates must pass' }
$location=Get-Content (Join-Path $PSScriptRoot 'implementation-export.json') -Raw | ConvertFrom-Json
$validation=$location.Export
$runs=@(foreach($name in @('targeted','broad','boundary','full','harness-isolated','broad-harness','full-harness','dual')) {
    Get-Content (Join-Path $records ($name+'.json')) -Raw | ConvertFrom-Json
})
$dual=@(foreach($file in Get-ChildItem (Join-Path $records 'Dual') -File -Filter '*.trx') {
    [xml]$trx=Get-Content $file.FullName -Raw
    [ordered]@{Suite=$file.Name;Counters=($trx.TestRun.ResultSummary.Counters | Select-Object total,passed,failed,notExecuted)}
})
$a6=Join-Path $records 'A6'
New-Item -ItemType Directory -Path $a6 -Force | Out-Null
Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a10/analysis/a6') -File -Filter '*.json' | Copy-Item -Destination $a6
$paths=@('src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs','src/XMLDocNormalizer/Execution/Analysis/HistoricalSolutionFindingOperation.cs','src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerDeclaredDocumentationProjection.cs','src/XMLDocNormalizer/Execution/ExternalTargetExecution.cs','src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs','src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs','Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs','Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs','Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionAdoptionTests.cs','Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalSourceLineEndingReconstructionTests.cs')
$sources=@(foreach($path in $paths) {
    $workspace=(Get-FileHash (Join-Path $tool $path)).Hash; $export=(Get-FileHash (Join-Path $validation $path)).Hash
    if($workspace -cne $export) { throw ('Final source drift: '+$path) }
    [ordered]@{Path=$path;WorkspaceSha256=$workspace;CompiledExportSha256=$export}
})
$status=@(& git.exe --no-optional-locks -C $tool status --short)
& git.exe --no-optional-locks -C $tool diff --cached --quiet
if($LASTEXITCODE -ne 0) { throw 'Semantic index drift' }
[ordered]@{
    Package='P5O2B25B';Passed=$true;CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=$location.Head;Export=$validation
    BoundedFindingAdoption=$true;GeneralSolutionSupport=$false;ReportingRoots=1;ProjectChainSizes=@(2,3)
    NewWitnessCases=20;CharacterizationBatch=@('C1:15 theory cases and one P0/P1 Fact','C2:two source guards to real positive/negative boundary','C3:same two source guards');AdditionalCharacterizationChanges=0
    AdditionalHarnessCorrection='Original prompt section12; Task.Run coordination wrapper only; all four assertions and five-second timeout unchanged'
    HistoricalRedFullPreserved=$true;FullSchedulingTelemetryCaptured=$false;ControlledSchedulerCounterexampleBeforeAfter=$true
    Runtime=$runs;DualSuites=$dual;GateChecks=$gate.Checks.Count;ProtectionChecks=$protection.Checks.Count;WaeSteps=11;CurrentBaselinesByteEqual=8;ActualAdditionalSessionQueries=30;A8Checks=40;A9Checks=37
    FinalSources=$sources;NoNewWireDto=$true;NoAnalyzerSemanticsChange=$true;NoVersionChange=$true;NoRoutingChange=$true;NoRawGraphBypass=$true
    NativeCliCrash='Historical 0xC0000005 remains unattributed; not observed in these B25B gates, not labelled flaky or fixed'
    IndexClean=$true;RootIgnoreForeignWipUnchanged=$true;CoreFilesUnchanged=23;StashesUnchanged=6;HistoricalEvaluationFilesUnchanged=735
    GitStatusShort=$status;Commit=$false;Push=$false;HistoryRewrite=$false
} | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $output -Encoding UTF8
Write-Output 'B25B closure audit written; no Git mutation.'
