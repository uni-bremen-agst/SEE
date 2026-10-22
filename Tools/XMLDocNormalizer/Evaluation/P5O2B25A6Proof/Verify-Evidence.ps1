$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a6-r2'
$validation = (Get-Content (Join-Path $records 'export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Json([string]$path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Hash([string]$path) { [ordered]@{ Path = [IO.Path]::GetFullPath($path); Sha256 = (Get-FileHash -LiteralPath $path).Hash } }
$failures = [Collections.Generic.List[object]]::new()
function Trx([string]$name, [string]$path, [int]$expected, [string]$runName) {
    Require (Test-Path -LiteralPath $path) ('Required fresh TRX missing: ' + $name)
    [xml]$xml = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $counter = $xml.SelectSingleNode('//*[local-name()="Counters"]')
    $times = $xml.SelectSingleNode('//*[local-name()="Times"]')
    $run = Json (Join-Path $records ('runs/' + $runName + '.json'))
    Require ($null -ne $run.ExitCode) ('Unfinished attempt: ' + $runName)
    Require ([DateTimeOffset]::Parse($times.start).UtcDateTime -ge [DateTimeOffset]::Parse($run.StartedUtc).UtcDateTime) ('Stale TRX: ' + $name)
    Require ([DateTimeOffset]::Parse($times.finish).UtcDateTime -le [DateTimeOffset]::Parse($run.FinishedUtc).UtcDateTime) ('TRX outside attempt: ' + $name)
    Require ([int]$counter.total -eq $expected) ('Inventory changed: ' + $name + ' actual ' + $counter.total + ' expected ' + $expected)
    $errors = @($xml.SelectNodes('//*[local-name()="UnitTestResult" and @outcome!="Passed"]') | ForEach-Object { [ordered]@{ Test = $_.testName; Outcome = $_.outcome; Message = $_.Output.ErrorInfo.Message; Stack = $_.Output.ErrorInfo.StackTrace; Duration = $_.duration } })
    if ([int]$counter.passed -ne $expected -or [int]$counter.failed -ne 0 -or [int]$counter.notExecuted -ne 0) { $failures.Add([ordered]@{ Gate = $name; Failures = $errors }) }
    [ordered]@{ Name = $name; Total = [int]$counter.total; Passed = [int]$counter.passed; Failed = [int]$counter.failed; Skipped = [int]$counter.notExecuted; Started = $times.start; Finished = $times.finish; Artifact = (Hash $path); Failures = $errors }
}
foreach ($script in @('Verify-Protection.ps1', 'Verify-Revision.ps1', 'Trace-Lifecycle.ps1', 'Verify-CompiledSources.ps1', 'Verify-Static.ps1')) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $script)
    Require ($LASTEXITCODE -eq 0) ('Consistency verifier failed: ' + $script)
}
$inputs = Json (Join-Path $records 'validation-inputs.json')
Require (@($inputs.SharedSources).Count -eq 121 -and @($inputs.Sources).Count -eq 1 -and @($inputs.Images).Count -eq 5) 'Compiled input inventory mismatch.'
$start = Json (Join-Path $records 'start.json')
foreach ($source in $inputs.SharedSources) {
    Require (($start.OriginalFiles | Where-Object Path -ceq $source.Path).Sha256 -ceq $source.WorkspaceSha256) ('A6 production change: ' + $source.Path)
}
foreach ($image in $inputs.Images) { Require ((Get-FileHash (Join-Path $validation $image.Path)).Hash -ceq $image.Sha256) ('Executed image changed: ' + $image.Path) }
$runs = @(foreach ($name in @('build', 'dual', 'witnesses', 'contexts', 'broad', 'full', 'boundary')) {
    $path = Join-Path $records ('runs/' + $name + '.json'); $run = Json $path
    Require ($null -ne $run.ExitCode) ('Unfinished required attempt: ' + $name)
    if ($run.ExitCode -ne 0) { $failures.Add([ordered]@{ Gate = $name; ExitCode = $run.ExitCode }) }
    [ordered]@{ Name = $name; ExitCode = $run.ExitCode; Artifact = (Hash $path); Log = (Hash (Join-Path $records ('runs/' + $name + '.log'))) }
})
$tests = [Collections.Generic.List[object]]::new()
foreach ($row in @(@('witnesses', 8), @('contexts', 134), @('broad', 3319), @('full', 3410), @('boundary', 596))) { $tests.Add((Trx $row[0] (Join-Path $validation ('artifacts/p5o2b25a6/tests/' + $row[0] + '.trx')) $row[1] $row[0])) }
$dualPath = Join-Path $validation 'artifacts/dual-version-build/audit.json'
$dual = Json $dualPath
if (!$dual.Passed) { $failures.Add([ordered]@{ Gate = 'complete-dual'; Passed = $dual.Passed; State = $dual.State }) }
foreach ($row in @(@('main-boundary', 596), @('external-target', 36), @('bounded-findings', 42), @('solution-findings', 27), @('assembly-qualified-ownership', 14), @('canonical-exception-identity', 20), @('constructed-invocation-context', 17), @('bounded-generic-context', 15), @('recursive-summary-closure', 33))) { $tests.Add((Trx ('dual-' + $row[0]) (Join-Path $validation ('artifacts/dual-version-build/' + $row[0] + '.trx')) $row[1] 'dual')) }
$dualRun = Json (Join-Path $records 'runs/dual.json')
$wae = @(foreach ($name in @('0-current-build', '1-historical-build', '2-current-build', '3-historical-build', '4-historical-build', '5-current-build', 'worker-0-build', 'worker-1-build', 'boundary-test-process-build', 'projection-pdb-fixture-build', 'main-boundary-tests-build')) {
    $path = Join-Path $validation ('artifacts/dual-version-build/' + $name + '.log')
    $log = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    Require ((Get-Item $path).LastWriteTimeUtc -ge [DateTimeOffset]::Parse($dualRun.StartedUtc).UtcDateTime) ('Stale WAE: ' + $name)
    Require ($log -match '(?m)^\s+0 (Warnung\(en\)|Warning\(s\))\s*$' -and $log -match '(?m)^\s+0 (Fehler|Error\(s\))\s*$') ('WAE not clean: ' + $name)
    Hash $path
})
$baselines = @(foreach ($before in Get-ChildItem (Join-Path $records 'baseline/current') -File -Filter '*.json') {
    $after = Join-Path $validation ('artifacts/p5o2b9/current/' + $before.Name)
    Require ((Get-FileHash $before.FullName).Hash -ceq (Get-FileHash $after).Hash) ('Current baseline changed: ' + $before.Name)
    [ordered]@{ Name = $before.Name; Before = (Hash $before.FullName); After = (Hash $after); ByteEqual = $true; Reference = 'Frozen inherited A5 baseline bytes; fresh A6 output' }
})
Require ($baselines.Count -eq 8) 'Exactly eight baselines required.'
$witnesses = @(Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a6/analysis') -File -Filter '*.json' | Sort-Object Name | ForEach-Object {
    $w = Json $_.FullName
    Require (!$w.ProductionChanged) 'Witness must not claim production correction.'
    if ($_.Name -like 'growth-*') { Require ($w.ExactCandidateNodes -eq 25 -and $w.WitnessSteps -eq 24 -and @($w.Observations).Count -eq 24 -and $w.FinitePrefixIsNotTerminationProof) 'Growth witness contract mismatch.' }
    Hash $_.FullName
})
Require ($witnesses.Count -eq 8) 'Eight A6 witnesses required.'
$sourceBindings = Json (Join-Path $records 'compiled-source-verification.json')
$full = @($tests.ToArray() | Where-Object Name -ceq 'full')[0]
$fixtureCandidates = @(Get-ChildItem (Join-Path $tool 'Tests/XMLDocNormalizerTests') -Recurse -File -Filter '*MetadataReferenceFactoryTests.cs')
Require ($fixtureCandidates.Count -eq 1) 'One historical metadata fixture source expected.'
[xml]$fullXml = Get-Content -LiteralPath (Join-Path $validation 'artifacts/p5o2b25a6/tests/full.trx') -Raw -Encoding UTF8
$observations = @($fullXml.SelectNodes('//*[local-name()="UnitTestResult"]') | Where-Object { $_.testName -like '*MultiModuleAssembly_FailsClosed*' -or $_.testName -like '*ConcurrentReconstruction_PerformsOneRequestAndOneAttempt*' } | ForEach-Object { [ordered]@{ Test = $_.testName; Outcome = $_.outcome; Duration = $_.duration; Message = $_.Output.ErrorInfo.Message } })
Require ($observations.Count -eq 2) 'Historical fixture/acquisition observations must remain in Full.'
$foreignObservation = [ordered]@{ FullFailedTests = $full.Failures; Observations = $observations; FixtureSource = (Hash $fixtureCandidates[0].FullName); IsolatedOrFullRetry = $false; TestOrProductionRepaired = $false; A5HistoricalDiagnosis = (Hash (Join-Path $tool 'artifacts/p5o2b25a5/foreign-full-failure.json')); FreshRootCauseExperiment = $false }
$owned = @(Get-ChildItem $PSScriptRoot -File -Recurse | Sort-Object FullName | ForEach-Object { Hash $_.FullName })
$owned += Hash (Join-Path $tool 'Evaluation/P5O2B25A6-bounded-generic-context-expansion-after-scc.md')
$owned += Hash (Join-Path $tool 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowGenericExpansionBoundaryTests.cs')
$audit = [ordered]@{
    Package = 'P5O2B25A6'; GeneratedUtc = [DateTime]::UtcNow.ToString('o'); Head = $start.Head
    Outcome = 'BoundaryAudit'; BoundaryClosedAsAudit = $true; GenericProductionCorrected = $false; ReadyForB25B = $false; SolutionFindingAdmitted = $false
    ProductionFilesChanged = 0; ExistingTestsChanged = 0; NewWitnessTests = 8; ConditionalPermissionsExercised = 0; CandidateImplementedOrWithdrawn = $false
    ValidationScope = 'Final corrected source revision r2; initial failed serialization run is retained separately, not hidden or retried unchanged'; SourceCorrectionRevision = (Hash (Join-Path $records 'source-correction-revision.json')); InitialWitnessPassed = 6; InitialWitnessFailed = 2
    Passed = ($failures.Count -eq 0); ValidationPassed = ($failures.Count -eq 0); EvidenceConsistent = $true; GateFailures = @($failures.ToArray())
    Boundary = 'Graph construction drains pending exact contexts before SCC discovery/evaluation; constructor-height growth has no repeated exact state; independent finite family/catch/completeness contract required'
    Lifecycle = (Hash (Join-Path $records 'lifecycle.json'))
    Tests = @($tests.ToArray()); Runs = $runs; Dual = (Hash $dualPath); FinalDualPassed = [bool]$dual.Passed; WaeBuilds = $wae; CurrentBaselines = $baselines; Witnesses = $witnesses
    SourceBindings = (Hash (Join-Path $records 'compiled-source-verification.json')); PdbDocuments = @($sourceBindings.ImageBindings | ForEach-Object Documents).Count; RawLineEndingDifferences = $sourceBindings.RawDifferences
    CompiledInputs = (Hash (Join-Path $records 'validation-inputs.json')); Protection = (Hash (Join-Path $records 'protection.json')); Static = (Hash (Join-Path $records 'static.json'))
    HistoricalEvidenceRewritten = $false; NoSkipsOrGreenRetries = $true; ActualLinuxRun = $false; NetworkForBuilds = $false; ForeignFullObservation = $foreignObservation
    SelfAnalysis = [ordered]@{ Fresh = $false; InheritedCount = 16 }; CanonicalFindingDiff = [ordered]@{ Fresh = $false; InheritedAdded = 0; InheritedRemoved = 0; InheritedChanged = 0 }
    OwnedFiles = $owned; FinalGitStatus = @(& git --no-optional-locks -C (Join-Path $tool '../..') status --short)
}
$auditPath = Join-Path $tool 'Evaluation/P5O2B25A6-bounded-generic-context-expansion-after-scc-audit.json'
[IO.File]::WriteAllText($auditPath, (($audit | ConvertTo-Json -Depth 100) + "`r`n"), [Text.UTF8Encoding]::new($false))
if ($failures.Count -ne 0) { Write-Host 'FAIL: actual validation failures retained; boundary evidence consistent, no waiver/retry.'; exit 1 }
Write-Host 'PASS: A6 boundary evidence and fresh validation consistent; NO production generic correction/admission.'
