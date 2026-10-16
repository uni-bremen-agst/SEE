$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $tool 'artifacts/p5o2b25a/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$attempt=Get-Content (Join-Path $tool 'artifacts/p5o2b25a/runs/new-tests-first.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($null -eq $attempt.ExitCode -or $attempt.ExitCode -eq 0) { throw 'The completed failed candidate attempt must be retained before withdrawal.' }
$destination=Join-Path $tool 'artifacts/p5o2b25a/candidate'
if(Test-Path $destination) { throw 'Candidate archive already exists; never overwrite actual attempts.' }
[IO.Directory]::CreateDirectory($destination) | Out-Null
$files=@('src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs',
    'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs',
    'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs',
    'src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll',
    'artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll',
    'artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll',
    'artifacts/exception-flow-historical/HistoricalPdbFixture/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll',
    'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll',
    'artifacts/p5o2b25a/tests/ownership-first.trx', 'artifacts/p5o2b25a/tests/nested-catch-diagnostic.trx',
    'artifacts/p5o2b25/audit-cases/ownership-stop-witness.json')
$manifest=foreach($relative in $files) {
    $from=Join-Path $validation $relative
    $to=Join-Path $destination $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($to)) | Out-Null
    Copy-Item -LiteralPath $from -Destination $to
    [ordered]@{Path=$relative;Sha256=(Get-FileHash $to).Hash}
}
Copy-Item (Join-Path $tool 'artifacts/p5o2b25a/validation-inputs.json') (Join-Path $destination 'validation-inputs.json')
Copy-Item (Join-Path $tool 'artifacts/p5o2b25a/runs/new-tests-first.json') (Join-Path $destination 'run.json')
Copy-Item (Join-Path $tool 'artifacts/p5o2b25a/runs/new-tests-first.log') (Join-Path $destination 'run.log')
$proof=Join-Path $PSScriptRoot 'candidate-tests.cs.txt'
Copy-Item (Join-Path $validation 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs') $proof
Push-Location $tool
try {
    $patch=(& git diff HEAD -- src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs) -join "`r`n"
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'withdrawn-candidate.patch.txt'),($patch+"`r`n"),[Text.UTF8Encoding]::new($false))
} finally { Pop-Location }
[IO.File]::WriteAllText((Join-Path $destination 'manifest.json'),(($manifest | ConvertTo-Json -Depth 6)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host 'Candidate source, images, failed checks and actual correct top-level ownership witnesses preserved; no production/index mutation.'
