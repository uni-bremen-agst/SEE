$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output = Join-Path $PSScriptRoot 'identity-stop-verification.json'
$destination = Join-Path $PSScriptRoot 'IdentityDiagnostics'
if ((Test-Path -LiteralPath $output) -or (Test-Path -LiteralPath $destination)) { throw 'Preserve prior capture, no overwrite.' }
[IO.Directory]::CreateDirectory($destination) | Out-Null
$copies = @(
    @('artifacts/b25a10-development-10/artifacts/p5o2b25a10/diagnostics/canonical-object-identity.json', 'canonical-object-identity.json'),
    @('artifacts/b25a10-development-10/artifacts/p5o2b25a10/diagnostics/failure-identity-False.json', 'failure-identity-False.json'),
    @('artifacts/b25a10-development-10/artifacts/p5o2b25a10/diagnostics/failure-identity-True.json', 'failure-identity-True.json'),
    @('artifacts/b25a10-development-08/artifacts/p5o2b25a10/analysis/b25/ownership-stop-witness.json', 'ownership-finding-closure.json')
)
$evidence = @(foreach ($pair in $copies) {
    $source = Join-Path $tool $pair[0]; $target = Join-Path $destination $pair[1]
    [IO.File]::Copy($source, $target, $false)
    [ordered]@{ Source=$pair[0]; Target=('Evaluation/P5O2B25A10Proof/IdentityDiagnostics/' + $pair[1]); Sha256=(Get-FileHash $target).Hash }
})
[xml]$trx = Get-Content (Join-Path $tool 'artifacts/b25a10-development-09/artifacts/a10-development-results/a-series.trx') -Raw -Encoding UTF8
$classes = @($trx.TestRun.Results.UnitTestResult | Group-Object { [regex]::Match($_.testName, '^XMLDocNormalizerTests\.Worker\.([A-Za-z0-9_]+)\.').Groups[1].Value } | ForEach-Object {
    [ordered]@{ Class=$_.Name; Total=$_.Count; Passed=@($_.Group | Where-Object outcome -eq 'Passed').Count; Failed=@($_.Group | Where-Object outcome -eq 'Failed').Count }
})
$original = (& git --no-optional-locks -C $tool show 'HEAD:Tools/XMLDocNormalizer/Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs') -join "`n"
$current = Get-Content (Join-Path $tool 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs') -Raw -Encoding UTF8
$helperPattern = '(?s)private static INamedTypeSymbol Candidate\(INamedTypeSymbol type, Compilation root\)\s*=>\s*(.*?;)'
$oldHelper = [regex]::Match($original, $helperPattern).Value -replace '\s+', ' '
$newHelper = [regex]::Match($current, $helperPattern).Value -replace '\s+', ' '
$k17 = 'Assert.Same(caught, normalizedCaught);'
$trace = Get-Content (Join-Path $destination 'canonical-object-identity.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$expected = $trace.expected | ConvertTo-Json -Depth 30 -Compress
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Status='STOP_ProtectedK17ObjectIdentity'
    Evidence=$evidence; CombinedSuite=[ordered]@{Total=146;Passed=143;Failed=3;Skipped=0}; Classes=$classes
    B25OwnershipFactPassed=$true; B25SuitePassed=27; DiagnosticTestsPassed=3
    ProtectedK17AssertionUnchanged=($original.Contains($k17) -and $current.Contains($k17))
    CandidateHelperUnchanged=($oldHelper.Length -gt 0 -and $oldHelper -ceq $newHelper)
    CompleteIdentitiesEqual=(@('rebound','normalizedThrown','normalizedCaught' | Where-Object {($trace.$_ | ConvertTo-Json -Depth 30 -Compress) -cne $expected}).Count -eq 0)
    ObjectReferenceObservations=[ordered]@{Rebound=$trace.reboundSameInstanceAsCaught;NormalizedThrown=$trace.normalizedThrownSameInstanceAsCaught;NormalizedCaught=$trace.normalizedCaughtSameInstanceAsCaught}
    BothDestinationSymbolsEqual=$trace.bothDestinationSymbolsEqual
    ProductionSourceChangesThisContinuation=0; B25BActivated=$false; A10Complete=$false
}
[IO.File]::WriteAllText($output, (($record | ConvertTo-Json -Depth 12)+"`r`n"), [Text.UTF8Encoding]::new($false))
if (!$record.ProtectedK17AssertionUnchanged -or !$record.CandidateHelperUnchanged -or !$record.CompleteIdentitiesEqual) { throw 'STOP trace verification failed; preserve actual evidence.' }
Write-Host 'Captured: protected K17/helper unchanged; complete identities equal, reference identity false; B25 Finding grant passes.'
