$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$before = Join-Path $tool 'artifacts/b25a10-development-10'
$tested = Join-Path $tool 'artifacts/b25a10-development-11'
$relative = 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs'
$original = (Get-Content (Join-Path $before $relative) -Raw -Encoding UTF8).Replace("`r`n", "`n")
$actual = (Get-Content (Join-Path $tool $relative) -Raw -Encoding UTF8).Replace("`r`n", "`n")
$oldP12 = '            Assert.Same(caught, new RoslynCanonicalIdentityResolver(a).ResolveType(Identity(thrown))); // P12'
$newP12 = @(
    '            var rebound = new RoslynCanonicalIdentityResolver(a).ResolveType(Identity(thrown)); // P12'
    '            Assert.NotNull(rebound);'
    '            Assert.Equal(Identity(caught), Identity(rebound));'
    '            Assert.True(SymbolEqualityComparer.Default.Equals(caught, rebound));'
) -join "`n"
$oldK17 = '            Assert.Same(caught, normalizedCaught);'
$newK17 = @(
    '            Assert.Equal(Identity(caught), Identity(normalizedCaught)); // K17'
    '            Assert.True(SymbolEqualityComparer.Default.Equals(caught, normalizedCaught));'
) -join "`n"
if (!$original.Contains($oldP12) -or !$original.Contains($oldK17)) { throw 'Previous snapshot does not match the exact approved assertions.' }
if ($actual -cne $original.Replace($oldP12, $newP12).Replace($oldK17, $newK17)) { throw 'Test edits exceed exactly K17/P12.' }
if ((Get-FileHash (Join-Path $tool $relative)).Hash -cne (Get-FileHash (Join-Path $tested $relative)).Hash) { throw 'Tested source differs from WIP.' }
$offlineConfig = 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config'
# Prepare-OfflineValidation deliberately rewrites this file only in each export.
# Check the workspace original against Git rather than the transformed export.
& git --no-optional-locks -C $tool diff --quiet -- $offlineConfig
if ($LASTEXITCODE -ne 0) { throw 'Workspace Historical feed configuration changed.' }
$paths = @(@(& git --no-optional-locks -C $tool ls-files -- src) + @(& git --no-optional-locks -C $tool ls-files --others --exclude-standard -- src) | Sort-Object -Unique | Where-Object { $_ -notmatch '(^|/)(bin|obj)/' -and $_ -ne $offlineConfig })
foreach ($path in $paths) {
    if ((Get-FileHash (Join-Path $tool $path)).Hash -cne (Get-FileHash (Join-Path $before $path)).Hash) { throw ('Production source changed: ' + $path) }
}
$r2 = 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs'
if ((Get-FileHash (Join-Path $tool $r2)).Hash -cne (Get-FileHash (Join-Path $before $r2)).Hash) { throw 'R2 file changed.' }
[xml]$trx = Get-Content (Join-Path $tested 'artifacts/a10-development-results/k17-p12.trx') -Raw
$counters = $trx.TestRun.ResultSummary.Counters
if ($counters.total -ne '1' -or $counters.failed -ne '1' -or $counters.passed -ne '0' -or $counters.notExecuted -ne '0') { throw 'Unexpected targeted results.' }
$failure = $trx.TestRun.Results.UnitTestResult.Output.ErrorInfo
if ($failure.StackTrace -notmatch ':line 62' -or $failure.Message -notmatch 'Assert.Same') { throw 'Unexpected failure location/type.' }
$output = Join-Path $PSScriptRoot 'K17P12Followup'
if (Test-Path -LiteralPath $output) { throw 'Retain existing evidence; never overwrite.' }
[IO.Directory]::CreateDirectory($output) | Out-Null
foreach ($run in @('development-11-test-build', 'development-11-k17-p12')) {
    foreach ($suffix in @('.json', '.log')) {
        [IO.File]::Copy((Join-Path $tool ('artifacts/p5o2b25a10/runs/' + $run + $suffix)), (Join-Path $output ($run + $suffix)), $false)
    }
}
[IO.File]::Copy((Join-Path $tested 'artifacts/a10-development-results/k17-p12.trx'), (Join-Path $output 'k17-p12.trx'), $false)
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Status='STOP_SeparateR5ReferenceAssertionClarification'
    PreviousSnapshot=$before; TestedSnapshot=$tested
    ExactTestDiffOnlyApprovedK17AndP12=$true; CandidateHelperUnchanged=$true
    FullCanonicalIdentityAndExplicitRoslynEquality=$true; ReferenceEqualityNotRequired=$true
    TestedSourceSha256=(Get-FileHash (Join-Path $tool $relative)).Hash
    ProductionFilesCompared=$paths.Count; ProductionChangesThisFollowup=0; R2FileUnchanged=$true
    ExportOnlyTransformedConfig=$offlineConfig; WorkspaceConfigGitDiffClean=$true
    InitialCaptureAttempt='Stopped before writing evidence: offline-transformed export NuGet.Config cannot equal original workspace config; corrected comparison explicitly checks unchanged original against Git.'
    TestBuild=(Get-Content (Join-Path $output 'development-11-test-build.json') -Raw | ConvertFrom-Json)
    TargetedFact=@{ Passed=0; Failed=1; Skipped=0; P12Passed=$true; K17Reached=$false }
    FailureMessage=$failure.Message; FailureStack=$failure.StackTrace
    R5Assertion='Assert.Same(caught, normalizedThrown); // R5'; R5Changed=$false
    PriorSemanticWitness='IdentityDiagnostics/canonical-object-identity.json'
    FullFinalGatesExecuted=$false; B25BActivated=$false; A10Complete=$false
}
[IO.File]::WriteAllText((Join-Path $output 'verification.json'), (($record | ConvertTo-Json -Depth 10) + "`r`n"), [Text.UTF8Encoding]::new($false))
Write-Host ('Only K17/P12 test edits; unchanged production files: ' + $paths.Count + '; R2 unchanged; targeted failure at R5 line62; evidence retained.')
