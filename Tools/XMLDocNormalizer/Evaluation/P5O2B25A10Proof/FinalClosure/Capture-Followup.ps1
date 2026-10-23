$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$export = Join-Path $tool 'artifacts/b25a10-development-17'
$prior = Join-Path $tool 'artifacts/b25a10-development-16'
$destination = Join-Path $PSScriptRoot 'await-approval-followup.json'
if (Test-Path -LiteralPath $destination) { throw 'Preserve the existing followup capture.' }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
$delta = @(foreach ($relative in @(& git --no-optional-locks -C $tool ls-files -- src Tests) + @(& git --no-optional-locks -C $tool ls-files --others --exclude-standard -- src Tests) | Sort-Object -Unique) {
    if ($relative -match '(^|/)(bin|obj)/' -or $relative -ceq 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config') { continue }
    $current = Hash (Join-Path $tool $relative)
    if ($current -cne (Hash (Join-Path $export $relative))) { throw ('Current/tested source mismatch: ' + $relative) }
    $before = Hash (Join-Path $prior $relative)
    if ($before -cne $current) { [ordered]@{ Path=$relative; BeforeExport16=$before; CurrentAndExport17=$current } }
})
$test = 'Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerProcessTests.cs'
if ($delta.Count -ne 1 -or $delta[0].Path -cne $test) { throw 'Source changes exceed the single approved test file.' }
$old = (Get-Content (Join-Path $prior $test) -Raw -Encoding UTF8).Replace("`r`n","`n")
$current = (Get-Content (Join-Path $tool $test) -Raw -Encoding UTF8).Replace("`r`n","`n")
$before = @'
            Assert.Contains(json.RootElement.GetProperty("failure").GetProperty("details").EnumerateArray(),
                detail => detail.GetString() == "Await expression runtime-await information is unavailable.");
'@
$after = @'
            string[] details = json.RootElement.GetProperty("failure").GetProperty("details")
                .EnumerateArray().Select(detail => detail.GetString()!).ToArray();
            Assert.Contains("Profile=A9-FiniteOrbit-v1", details);
            Assert.Contains("Complete=false", details);
            Assert.Contains("NormalFindingAdmissionBlocked=true", details);
            Assert.DoesNotContain("Complete=true", details);
            Assert.Contains(details, detail =>
                detail.StartsWith("TargetOrAnalysisCoverageIncomplete: ", StringComparison.Ordinal)
                && detail.Contains("RuntimeAwait", StringComparison.Ordinal)
                && detail.Contains("unavailable", StringComparison.Ordinal));
            Assert.False(json.RootElement.GetProperty("identity")
                .GetProperty("runtimeAwaitInformationAvailable").GetBoolean());
'@
$before=$before.Replace("`r`n","`n"); $after=$after.Replace("`r`n","`n")
if (!$old.Contains($before) -or $current -cne $old.Replace($before,$after)) { throw 'Delta exceeds the exact approved Details block.' }
$results = @(foreach ($name in @('await-first','broad','full')) {
    $path = Join-Path $export ('artifacts/a10-closure-results/' + $name + '.trx')
    [xml]$trx = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $counts = [ordered]@{}
    foreach ($attribute in $trx.TestRun.ResultSummary.Counters.Attributes) { $counts[$attribute.Name] = [int]$attribute.Value }
    [ordered]@{ Phase=$name; Path=$path; Sha256=(Hash $path); Counters=$counts
        Failures=@($trx.TestRun.Results.UnitTestResult | Where-Object outcome -eq 'Failed' | ForEach-Object {
            [ordered]@{ Test=$_.testName; Message=$_.Output.ErrorInfo.Message; Stack=$_.Output.ErrorInfo.StackTrace }
        }); Await=@($trx.TestRun.Results.UnitTestResult | Where-Object testName -like '*HistoricalAwait_ActuallyPropagatesUnavailableInformationAsFailure' | Select-Object testName,outcome) }
})
$foreign = 'Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalMetadataReferenceFactoryTests.cs'
$historical = Get-Content (Join-Path $tool 'artifacts/p5o2b25a5/foreign-full-failure.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$sourceHash=Hash (Join-Path $tool $foreign)
$dependencyHash=Hash (Join-Path $export 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/System.Reflection.Metadata.dll')
if ($sourceHash -cne $historical.WorkspaceSource.Sha256 -or $dependencyHash -cne $historical.ActualExecutedDependency.Sha256) { throw 'Do not infer historical identity from a different source/dependency.' }
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Status='STOP_FullForeignFixtureReaderLifetime_NoRetry'
    SourceDeltaExactlyApprovedDetailsBlock=$true; SourceDelta=$delta; ProductionFilesChangedThisContinuation=0
    ExistingOtherAssertionsChanged=$false; ExistingFailureCodeUnchangedFromExport16=$true
    Results=$results; BroadGreen=$true; FullGreen=$false
    ForeignFailure=[ordered]@{ Test='ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed'; Line=446
        SourceSha256=$sourceHash; HistoricalSourceAndCurrentAndExportEqual=$true
        MetadataImageSha256=$dependencyHash; HistoricalAndCurrentDependencyEqual=$true
        HistoricalDiagnosis='artifacts/p5o2b25a5/foreign-full-failure.json'
        HistoricalDiagnosisSha256=(Hash (Join-Path $tool 'artifacts/p5o2b25a5/foreign-full-failure.json'))
        FactoryReached=$false; AnalyzerReachedOnFailingPath=$false; ReaderLifetimeHazardConfirmed=$true
        ExactFreedMemoryReuseProved=$false; FixImplemented=$false; TestRetried=$false }
    PreviousCliNativeCrashStillUnattributed=$true; NativeCrashObservedThisContinuation=$false
    BoundaryExecuted=$false; DualExecuted=$false; Wae11Verified=$false; Current8BaselinesVerified=$false
    A6A7A8A9GatesExecuted=$false; A10EvidenceGateExecuted=$false
    AdditionalProductionHarnessPreparedButNotBuiltOrExecuted=$true
    SelfAnalysisFresh=$false; CanonicalFindingDiffFresh=$false; B25BActivated=$false; A10Complete=$false
    Commit=$false; Push=$false; StashOperation=$false
}
[IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 25)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host 'Exact approved Details-only source delta; Await1/1, Broad3338/3338, Full3428/3429; foreign lifetime hazard unchanged, no retry.'
