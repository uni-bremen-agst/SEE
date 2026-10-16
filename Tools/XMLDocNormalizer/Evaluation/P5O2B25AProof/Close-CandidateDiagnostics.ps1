$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $tool 'artifacts/p5o2b25a/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$destination=Join-Path $tool 'artifacts/p5o2b25a/candidate'
$files=@('artifacts/p5o2b25a/tests/candidate-fresh-probe.trx','artifacts/p5o2b25a/cases/collision-2.json','artifacts/p5o2b25a/cases/collision-3.json')
foreach($relative in $files) {
    $to=Join-Path $destination $relative
    if(Test-Path $to) { throw 'Never overwrite diagnostic evidence.' }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($to)) | Out-Null
    Copy-Item (Join-Path $validation $relative) $to
}
$historical='artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll'
$probe='artifacts/exception-flow-historical/HistoricalPdbFixture/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll'
$old=(Get-FileHash (Join-Path $destination $probe)).Hash
$new=(Get-FileHash (Join-Path $validation $probe)).Hash
$analyzer=(Get-FileHash (Join-Path $validation $historical)).Hash
if($old -ceq $new -or $new -cne $analyzer) { throw 'Stale-probe diagnosis not established.' }
[ordered]@{OldProbeHistoricalSha256=$old;RebuiltProbeHistoricalSha256=$new;CandidateHistoricalSha256=$analyzer;
    RebuildLog=(Join-Path $tool 'artifacts/p5o2b25a/candidate-probe-build.log');
    InitialCandidateTotal=52;InitialPassed=49;InitialFailed=3;InitialSkipped=0;
    ConfirmedSemanticFailure='ConstructedContainingType_ExactMetadataCatchStillHandlesSourceException';
    StaleProbeFailures='Two native/graph comparisons, after correct-owner assertions and native/Worker parity already succeeded';
    FreshProbeDifferentialsPassed=2;NoProductionOrTestSourceChangedForDiagnostic=$true;CandidateWithdrawnForStopRule=$true} |
    ConvertTo-Json | Set-Content (Join-Path $destination 'diagnostics.json') -Encoding UTF8
Write-Host 'Preserved fresh two-/three-project differentials and proved obsolete probe dependency; independent nested-catch failure retained.'
