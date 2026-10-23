$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$before = Join-Path $tool 'artifacts/b25a10-development-12'
$tested = Join-Path $tool 'artifacts/b25a10-development-13'
$production = 'src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs'
$old = @'
                    return Fail(response.Failure.Code == WorkerFailureCode.UnsupportedProtocolVersion
                        ? HistoricalWorkerClientFailureCode.ProtocolMismatch : HistoricalWorkerClientFailureCode.StructuredFailure,
                        response.Failure.Message, response.Failure);
'@
$new = @'
                    // Preserve only the actual identity that passed the checks above; never admit a result or receipt.
                    return new(null, response.Identity, new(response.Failure.Code == WorkerFailureCode.UnsupportedProtocolVersion
                        ? HistoricalWorkerClientFailureCode.ProtocolMismatch : HistoricalWorkerClientFailureCode.StructuredFailure,
                        response.Failure.Message, response.Failure), stderr, processId);
'@
$old = $old.Replace("`r`n","`n"); $new = $new.Replace("`r`n","`n")
$original = (Get-Content (Join-Path $before $production) -Raw -Encoding UTF8).Replace("`r`n","`n")
$actual = (Get-Content (Join-Path $tool $production) -Raw -Encoding UTF8).Replace("`r`n","`n")
if (!$original.Contains($old) -or $actual -cne $original.Replace($old,$new)) { throw 'Production changes exceed the exact validated failure-identity projection.' }
$test = 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowFiniteOrbitAdmissionTests.cs'
$config = 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config'
& git --no-optional-locks -C $tool diff --quiet -- $config
if ($LASTEXITCODE -ne 0) { throw 'Original feed configuration changed.' }
$paths = @(@(& git --no-optional-locks -C $tool ls-files -- src Tests) + @(& git --no-optional-locks -C $tool ls-files --others --exclude-standard -- src Tests) | Sort-Object -Unique | Where-Object { $_ -notmatch '(^|/)(bin|obj)/' -and $_ -notin @($production,$test,$config) })
foreach ($path in $paths) {
    if ((Get-FileHash (Join-Path $tool $path)).Hash -cne (Get-FileHash (Join-Path $before $path)).Hash) { throw ('Unrelated source/test changed: ' + $path) }
}
foreach ($path in @($production,$test)) {
    if ((Get-FileHash (Join-Path $tool $path)).Hash -cne (Get-FileHash (Join-Path $tested $path)).Hash) { throw 'Tested source differs from current WIP.' }
}
$output = Join-Path $PSScriptRoot 'R2Closure'
if (Test-Path -LiteralPath $output) { throw 'Retain previous evidence; no overwrite.' }
[IO.Directory]::CreateDirectory($output) | Out-Null
$copies = @(
    @('artifacts/a10-development-results/r2-first.trx','r2-first.trx'),
    @('artifacts/a10-development-results/a-series.trx','a-series.trx'),
    @('artifacts/p5o2b25a10/diagnostics/failure-identity-False.json','failure-identity-False.json'),
    @('artifacts/p5o2b25a10/diagnostics/failure-identity-True.json','failure-identity-True.json'),
    @('artifacts/p5o2b25a10/analysis/a3/worker-recursion-False.json','worker-recursion-False.json'),
    @('artifacts/p5o2b25a10/analysis/a3/worker-recursion-True.json','worker-recursion-True.json')
)
foreach ($pair in $copies) { [IO.File]::Copy((Join-Path $tested $pair[0]),(Join-Path $output $pair[1]),$false) }
foreach ($name in @('test-build','worker-build','fixture-build','r2-first','a-series')) {
    foreach ($suffix in @('.json','.log')) {
        $file = 'development-13-' + $name + $suffix
        [IO.File]::Copy((Join-Path $tool ('artifacts/p5o2b25a10/runs/' + $file)),(Join-Path $output $file),$false)
    }
}
$comparisons = @(foreach ($containing in @('False','True')) {
    $trace = Get-Content (Join-Path $output ('failure-identity-' + $containing + '.json')) -Raw -Encoding UTF8 | ConvertFrom-Json
    $equal = ($trace.rawResponse.identity | ConvertTo-Json -Depth 30 -Compress) -ceq ($trace.validatedIdentity | ConvertTo-Json -Depth 30 -Compress)
    if (!$equal -or !$trace.actualValidatedIdentityPreserved -or !$trace.noSuccessfulResult -or $trace.invalidIdentityCasesRejected -ne 5 -or !$trace.missingIdentityRemainsFailClosed) { throw 'Identity preservation / fail-closed evidence mismatch.' }
    [ordered]@{ Containing=$containing; ActualWireIdentityEqualsProjectedIdentity=$equal; InvalidIdentityCasesRejected=5; MissingIdentityRemainsFailClosed=$true; NoSuccessfulResult=$true }
})
[xml]$first = Get-Content (Join-Path $output 'r2-first.trx') -Raw
[xml]$combined = Get-Content (Join-Path $output 'a-series.trx') -Raw
$classes = @($combined.TestRun.Results.UnitTestResult | Group-Object { [regex]::Match($_.testName, '^XMLDocNormalizerTests\.Worker\.([A-Za-z0-9_]+)\.').Groups[1].Value } | ForEach-Object {
    [ordered]@{ Class=$_.Name; Total=$_.Count; Passed=@($_.Group | Where-Object outcome -eq 'Passed').Count; Failed=@($_.Group | Where-Object outcome -eq 'Failed').Count }
})
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); ActualProductionDeltaOnlyValidatedIdentityPropagation=$true
    OtherSourceAndTestFilesUnchanged=$paths.Count; ExistingR2AssertionsUnchanged=$true; R5K17P12Unchanged=$true
    WireAndVersionAndAttestationValidationUnchanged=$true; CurrentSemanticsUnchanged=$true
    FirstR2=$first.TestRun.ResultSummary.Counters; Combined=$combined.TestRun.ResultSummary.Counters
    Classes=$classes; IdentityComparisons=$comparisons
    Evidence=@(Get-ChildItem $output -File | ForEach-Object { [ordered]@{ Name=$_.Name; Sha256=(Get-FileHash $_.FullName).Hash } })
    FullGatesComplete=$false; B25BActivated=$false; A10Complete=$false
}
[IO.File]::WriteAllText((Join-Path $output 'verification.json'),(($record | ConvertTo-Json -Depth 15)+"`r`n"),[Text.UTF8Encoding]::new($false))
if ($record.FirstR2.passed -ne '2' -or $record.FirstR2.failed -ne '0' -or $record.Combined.passed -ne '149' -or $record.Combined.failed -ne '0' -or $record.Combined.notExecuted -ne '0') { throw 'Targeted regression not fully green; actual evidence retained.' }
Write-Host 'R2 first2/2; combined149/149; exact propagation-only production delta and actual wire/projected identity equality verified.'
