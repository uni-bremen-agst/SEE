$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$before = Join-Path $tool 'artifacts/b25a10-development-11'
$tested = Join-Path $tool 'artifacts/b25a10-development-12'
$relative = 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs'
$original = (Get-Content (Join-Path $before $relative) -Raw -Encoding UTF8).Replace("`r`n", "`n")
$actual = (Get-Content (Join-Path $tool $relative) -Raw -Encoding UTF8).Replace("`r`n", "`n")
$old = '            Assert.Same(caught, normalizedThrown); // R5'
$replacement = @(
    '            Assert.Equal(Identity(caught), Identity(normalizedThrown)); // R5'
    '            Assert.True(SymbolEqualityComparer.Default.Equals(caught, normalizedThrown));'
) -join "`n"
if (!$original.Contains($old) -or $actual -cne $original.Replace($old, $replacement)) { throw 'Test diff exceeds the one approved R5 assertion.' }
if ((Get-FileHash (Join-Path $tool $relative)).Hash -cne (Get-FileHash (Join-Path $tested $relative)).Hash) { throw 'Tested source differs from WIP.' }
$offlineConfig = 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config'
& git --no-optional-locks -C $tool diff --quiet -- $offlineConfig
if ($LASTEXITCODE -ne 0) { throw 'Original Historical feed config changed.' }
# The offline preparer intentionally transforms only this config in the exports.
$paths = @(@(& git --no-optional-locks -C $tool ls-files -- src Tests) + @(& git --no-optional-locks -C $tool ls-files --others --exclude-standard -- src Tests) | Sort-Object -Unique | Where-Object { $_ -notmatch '(^|/)(bin|obj)/' -and $_ -ne $offlineConfig -and $_ -ne $relative })
foreach ($path in $paths) {
    if ((Get-FileHash (Join-Path $tool $path)).Hash -cne (Get-FileHash (Join-Path $before $path)).Hash) { throw ('Additional source/test change: ' + $path) }
}
[xml]$trx = Get-Content (Join-Path $tested 'artifacts/a10-development-results/a-series.trx') -Raw -Encoding UTF8
$results = @($trx.TestRun.Results.UnitTestResult)
$failures = @($results | Where-Object outcome -ne 'Passed')
$fact = @($results | Where-Object { $_.testName -like '*.ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught' })
$tracePath = Join-Path $tested 'artifacts/p5o2b25a10/diagnostics/canonical-object-identity.json'
$trace = Get-Content $tracePath -Raw -Encoding UTF8 | ConvertFrom-Json
$expected = $trace.expected | ConvertTo-Json -Depth 30 -Compress
$identitiesEqual = @('rebound','normalizedThrown','normalizedCaught' | Where-Object { ($trace.$_ | ConvertTo-Json -Depth 30 -Compress) -cne $expected }).Count -eq 0
$classes = @($results | Group-Object { [regex]::Match($_.testName, '^XMLDocNormalizerTests\.Worker\.([A-Za-z0-9_]+)\.').Groups[1].Value } | ForEach-Object {
    [ordered]@{ Class=$_.Name; Total=$_.Count; Passed=@($_.Group | Where-Object outcome -eq 'Passed').Count; Failed=@($_.Group | Where-Object outcome -eq 'Failed').Count }
})
$output = Join-Path $PSScriptRoot 'R5Closure'
if (Test-Path -LiteralPath $output) { throw 'Retain existing evidence; never overwrite.' }
[IO.Directory]::CreateDirectory($output) | Out-Null
$copies = @(
    @((Join-Path $tested 'artifacts/a10-development-results/a-series.trx'), 'a-series.trx'),
    @($tracePath, 'canonical-object-identity.json'),
    @((Join-Path $tested 'artifacts/p5o2b25a10/analysis/a/nested-rebind.json'), 'nested-rebind.json')
)
foreach ($run in @('development-12-test-build','development-12-worker-build','development-12-fixture-build','development-12-a-series')) {
    foreach ($suffix in @('.json','.log')) { $copies += ,@((Join-Path $tool ('artifacts/p5o2b25a10/runs/' + $run + $suffix)), ($run + $suffix)) }
}
$copied = @(foreach ($pair in $copies) {
    $target = Join-Path $output $pair[1]
    [IO.File]::Copy($pair[0], $target, $false)
    [ordered]@{ Path=$pair[1]; Sha256=(Get-FileHash $target).Hash }
})
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Status='R5_K17_P12_Closed_R2_RemainsOpen'
    PreviousSnapshot=$before; TestedSnapshot=$tested; ExactTestDiffOnlyApprovedR5=$true
    OtherSourceAndTestFilesCompared=$paths.Count; ProductionChangesThisFollowup=0
    CandidateHelperUnchanged=$true; R2FileUnchanged=$true; WorkspaceFeedConfigGitDiffClean=$true
    TestedSourceSha256=(Get-FileHash (Join-Path $tool $relative)).Hash
    CombinedCounters=$trx.TestRun.ResultSummary.Counters; Classes=$classes
    NamedFactCount=$fact.Count; NamedFactPassed=($fact.Count -eq 1 -and $fact[0].outcome -eq 'Passed')
    FullIdentitiesEqual=$identitiesEqual; DestinationRoslynSymbolsEqual=$trace.bothDestinationSymbolsEqual
    ClrReferenceObservations=@{ Rebound=$trace.reboundSameInstanceAsCaught; NormalizedThrown=$trace.normalizedThrownSameInstanceAsCaught; NormalizedCaught=$trace.normalizedCaughtSameInstanceAsCaught }
    Failures=@($failures | ForEach-Object { [ordered]@{ Test=$_.testName; Outcome=$_.outcome; Message=$_.Output.ErrorInfo.Message; Stack=$_.Output.ErrorInfo.StackTrace } })
    Evidence=$copied; R2FixImplemented=$false; FailureEnvelopeChanged=$false; WireDtoChanged=$false
    FullFinalGatesExecuted=$false; A10Complete=$false; B25BActivated=$false
}
[IO.File]::WriteAllText((Join-Path $output 'verification.json'), (($record | ConvertTo-Json -Depth 12) + "`r`n"), [Text.UTF8Encoding]::new($false))
if (!$record.NamedFactPassed -or !$identitiesEqual -or !$trace.bothDestinationSymbolsEqual) { throw 'Named closure not proved; actual evidence retained.' }
if ($failures.Count -ne 2 -or @($failures | Where-Object { $_.testName -notlike '*GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness*' -or $_.Output.ErrorInfo.Message -notmatch 'NullReferenceException' }).Count -ne 0) { throw 'Unexpected regression: STOP, actual evidence retained.' }
Write-Host 'R5/K17/P12 Fact passed; only two existing R2 failures remain; exact one-assertion diff verified, evidence retained.'
