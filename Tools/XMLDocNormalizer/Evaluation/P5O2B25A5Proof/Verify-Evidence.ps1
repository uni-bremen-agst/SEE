$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a5'
$validation = (Get-Content (Join-Path $records 'export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$failures = [Collections.Generic.List[object]]::new()
function Require([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Json([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Hash([string]$Path) { [ordered]@{ Path = [IO.Path]::GetFullPath($Path); Sha256 = (Get-FileHash -LiteralPath $Path).Hash } }
function Trx([string]$Name, [string]$Path, [int]$Expected, [string]$RunName) {
    Require (Test-Path -LiteralPath $Path) ('Fresh required TRX missing: ' + $Name)
    [xml]$xml = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $counter = $xml.SelectSingleNode('//*[local-name()="Counters"]')
    $times = $xml.SelectSingleNode('//*[local-name()="Times"]')
    $run = Json (Join-Path $records ('runs/' + $RunName + '.json'))
    Require ([DateTimeOffset]::Parse($times.start).UtcDateTime -ge [DateTimeOffset]::Parse($run.StartedUtc).UtcDateTime) ('Stale TRX start: ' + $Name)
    Require ([DateTimeOffset]::Parse($times.finish).UtcDateTime -le [DateTimeOffset]::Parse($run.FinishedUtc).UtcDateTime) ('TRX beyond completed attempt: ' + $Name)
    Require ([int]$counter.total -eq $Expected) ('Test inventory mismatch ' + $Name + ': ' + $counter.total + ' vs ' + $Expected)
    $errors = @($xml.SelectNodes('//*[local-name()="UnitTestResult" and @outcome!="Passed"]') | ForEach-Object { [ordered]@{ Test = $_.testName; Outcome = $_.outcome; Message = $_.Output.ErrorInfo.Message; Stack = $_.Output.ErrorInfo.StackTrace; Duration = $_.duration } })
    if ([int]$counter.passed -ne $Expected -or [int]$counter.failed -ne 0 -or [int]$counter.notExecuted -ne 0) { $failures.Add([ordered]@{ Gate = $Name; Failures = $errors }) }
    [ordered]@{ Name = $Name; Total = [int]$counter.total; Passed = [int]$counter.passed; Failed = [int]$counter.failed; Skipped = [int]$counter.notExecuted; Started = $times.start; Finished = $times.finish; Artifact = (Hash $Path); Failures = $errors }
}
Push-Location $tool
try {
    foreach ($script in @('Verify-ChangesAndHistory.ps1', 'Verify-CompiledSources.ps1', 'Verify-Static.ps1')) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $script)
        Require ($LASTEXITCODE -eq 0) ('Evidence consistency verifier failed: ' + $script)
    }
    $inputs = Json (Join-Path $records 'validation-inputs.json')
    Require ((Get-FileHash -LiteralPath (Join-Path $records 'source-inputs.json')).Hash -ceq $inputs.SourceCaptureSha256) 'Original pre-Dual source capture changed.'
    Require (@($inputs.Sources).Count -eq 2 -and @($inputs.SharedSources).Count -eq 121 -and @($inputs.Images).Count -eq 5) 'Exact compiled source/image inventory mismatch.'
    $start = Json (Join-Path $records 'start.json')
    $changedShared = @($inputs.SharedSources | Where-Object { $s = $_; ($start.OriginalFiles | Where-Object Path -ceq $s.Path).Sha256 -cne $s.WorkspaceSha256 })
    Require ($changedShared.Count -eq 1 -and $changedShared[0].Path -ceq 'src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs') 'Unexpected shared production source changes.'
    foreach ($image in $inputs.Images) { Require ((Get-FileHash -LiteralPath (Join-Path $validation $image.Path)).Hash -ceq $image.Sha256) ('Executed image changed after capture: ' + $image.Path) }
    $runs = @(foreach ($name in @('baseline-build', 'baseline-current', 'candidate-build', 'candidate-current', 'candidate-regressions', 'dual', 'contexts', 'pid-g1', 'pid-g1-proof', 'regressions', 'boundary')) {
        $path = Join-Path $records ('runs/' + $name + '.json'); $run = Json $path
        Require ($null -ne $run.ExitCode) ('Unfinished required attempt: ' + $name)
        if ($run.ExitCode -ne 0) { $failures.Add([ordered]@{ Gate = $name; ExitCode = $run.ExitCode; Artifact = (Hash $path) }) }
        [ordered]@{ Name = $name; ExitCode = $run.ExitCode; Artifact = (Hash $path); Log = (Hash (Join-Path $records ('runs/' + $name + '.log'))) }
    })
    $tests = [Collections.Generic.List[object]]::new()
    foreach ($row in @(@('baseline-current', 8), @('candidate-current', 25), @('candidate-regressions', 25), @('contexts', 126), @('pid-g1', 18), @('boundary', 596))) {
        $tests.Add((Trx $row[0] (Join-Path $validation ('artifacts/p5o2b25a5/tests/' + $row[0] + '.trx')) $row[1] $row[0]))
    }
    $counts = [ordered]@{ b24 = 42; b23 = 36; b22 = 46; b21 = 26; b20 = 10; b19 = 39; b18 = 7; b17 = 25; b16 = 18; b15 = 28; b14 = 31; b13 = 34; b12 = 29; b11 = 16; b10 = 34; 'b5-b9' = 156; productive = 903; focused = 1010; broad = 3311; full = 3402 }
    foreach ($name in $counts.Keys) { $tests.Add((Trx $name (Join-Path $validation ('artifacts/p5o2b24/tests/' + $name + '.trx')) $counts[$name] 'regressions')) }
    $acquisition = @(foreach ($name in @('broad', 'full')) {
        $path = Join-Path $validation ('artifacts/p5o2b24/tests/' + $name + '.trx')
        [xml]$xml = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $rows = @($xml.SelectNodes('//*[local-name()="UnitTestResult"]') | Where-Object testName -like '*ConcurrentReconstruction_PerformsOneRequestAndOneAttempt*')
        Require ($rows.Count -eq 1) 'Acquisition case must remain in Broad and Full.'
        [ordered]@{ Group = $name; Test = $rows[0].testName; Outcome = $rows[0].outcome; Duration = $rows[0].duration; Message = $rows[0].Output.ErrorInfo.Message; RootCauseProven = $false; TestOrTimeoutRepaired = $false; Retry = $false }
    })
    $pidProof = Json (Join-Path $records 'runs/pid-g1-proof-cases.json')
    Require (@($pidProof).Count -eq 9 -and @($pidProof | Where-Object { !$_.Passed }).Count -eq 0) 'Nine deterministic lifetime cases must pass.'
    $dualRoot = Join-Path $validation 'artifacts/dual-version-build'
    $dualPath = Join-Path $dualRoot 'audit.json'; $dual = Json $dualPath
    if (!$dual.Passed) { $failures.Add([ordered]@{ Gate = 'complete-dual'; State = $dual.State; Passed = $dual.Passed }) }
    foreach ($row in @(@('main-boundary', 596), @('external-target', 36), @('bounded-findings', 42), @('solution-findings', 27), @('assembly-qualified-ownership', 14), @('canonical-exception-identity', 20), @('constructed-invocation-context', 17), @('bounded-generic-context', 15), @('recursive-summary-closure', 33))) {
        $tests.Add((Trx ('dual-' + $row[0]) (Join-Path $dualRoot ($row[0] + '.trx')) $row[1] 'dual'))
    }
    $wae = @(foreach ($name in @('0-current-build', '1-historical-build', '2-current-build', '3-historical-build', '4-historical-build', '5-current-build', 'worker-0-build', 'worker-1-build', 'boundary-test-process-build', 'projection-pdb-fixture-build', 'main-boundary-tests-build')) {
        $path = Join-Path $dualRoot ($name + '.log'); $log = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ((Get-Item -LiteralPath $path).LastWriteTimeUtc -ge [DateTimeOffset]::Parse((Json (Join-Path $records 'runs/dual.json')).StartedUtc).UtcDateTime) ('Stale WAE log: ' + $name)
        Require ($log -match '(?m)^\s+0 (Warnung\(en\)|Warning\(s\))\s*$' -and $log -match '(?m)^\s+0 (Fehler|Error\(s\))\s*$') ('WAE log not clean: ' + $name)
        Hash $path
    })
    $current = @(foreach ($before in Get-ChildItem (Join-Path $records 'baseline/current') -Filter '*.json' -File) {
        $after = Join-Path $validation ('artifacts/p5o2b9/current/' + $before.Name)
        Require ((Get-FileHash -LiteralPath $before.FullName).Hash -ceq (Get-FileHash -LiteralPath $after).Hash) ('Current baseline changed: ' + $before.Name)
        [ordered]@{ Name = $before.Name; Before = (Hash $before.FullName); After = (Hash $after); ByteEqual = $true }
    })
    Require ($current.Count -eq 8) 'Exactly eight unchanged Current baselines required.'
    $witnesses = @(Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a5/analysis') -File -Filter '*.json' | Sort-Object Name | ForEach-Object {
        $w = Json $_.FullName
        if ($_.Name -match '^finite-scc-.*-False.json$') { Require (!$w.LostReachableExceptionWithoutUncertainty -and $w.ExistingEvaluatorCorrected -and @($w.Root.Entries).Count -eq 1 -and $w.Root.Entries[0].ExceptionType.MetadataName -ceq 'Fault') 'Corrected A4 Current witness missing.' }
        if ($_.Name -match '^worker-scc-.*-False.json$') { Require ($w.CorrectnessProven -and @($w.Current.Entries).Count -eq 1 -and $w.Current.Entries[0].ExceptionType.MetadataName -ceq 'Fault') 'Corrected A4 Worker witness missing.' }
        Hash $_.FullName
    })
    Require ($witnesses.Count -eq 36) ('Expected 36 A5 JSON witnesses; found ' + $witnesses.Count)
    $a4 = Json (Join-Path $tool 'Evaluation/P5O2B25A4-bounded-generic-context-expansion-audit.json')
    $beforeAfter = @(foreach ($prefix in @('finite-scc-', 'worker-scc-')) {
        foreach ($generic in @('False', 'True')) {
            $name = $prefix + $generic + '-False.json'
            $before = @($a4.Witnesses | Where-Object { [IO.Path]::GetFileName($_.Path) -ceq $name })
            Require ($before.Count -eq 1 -and (Get-FileHash -LiteralPath $before[0].Path).Hash -ceq $before[0].Sha256) ('Frozen original A4 witness unavailable: ' + $name)
            $afterPath = Join-Path $validation ('artifacts/p5o2b25a5/analysis/' + $name)
            $b = Json $before[0].Path; $a = Json $afterPath
            Require ($b.Source -ceq $a.Source) ('Changed A4 source fixture: ' + $name)
            $oldResult = if ($prefix -ceq 'finite-scc-') { $b.Root } else { $b.Current }
            $newResult = if ($prefix -ceq 'finite-scc-') { $a.Root } else { $a.Current }
            Require (@($oldResult.Entries).Count -eq 0 -and @($newResult.Entries).Count -eq 1 -and $newResult.Entries[0].ExceptionType.MetadataName -ceq 'Fault') ('A4 before/after flow contract mismatch: ' + $name)
            Require (@($oldResult.Uncertainties).Count -eq 0 -and @($newResult.Uncertainties).Count -eq 0) 'Do not replace precise A4 correction with uncertainty.'
            [ordered]@{ Name = $name; Before = $before[0]; After = (Hash $afterPath); SourceByteTextEqual = $true; BeforeFaultEntries = 0; AfterFaultEntries = 1; BeforeUncertainties = 0; AfterUncertainties = 0 }
        }
    })
    $owned = @(Get-ChildItem $PSScriptRoot -Recurse -File | Sort-Object FullName | ForEach-Object { Hash $_.FullName })
    foreach ($relative in @('Evaluation/P5O2B25A5-recursive-summary-scc-closure.md', 'src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs', 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs', 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs', 'build/Verify-DualVersionBuild.ps1', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) { $owned += Hash (Join-Path $tool $relative) }
    $bindings = Json (Join-Path $records 'compiled-source-verification.json')
    $history = Json (Join-Path $records 'changes-and-history.json')
    $foreignPath = Join-Path $records 'foreign-full-failure.json'
    $foreign = Json $foreignPath
    Require ($foreign.DiagnosticOnly -and $foreign.UnchangedSource -and !$foreign.TestOrProductionRepaired -and !$foreign.IsolatedOrFullRetry -and !$foreign.FactoryCallReached) 'Do not silently repair/retry or misclassify the foreign Full fixture failure.'
    $out = [ordered]@{
        Package = 'P5O2B25A5'; GeneratedUtc = [DateTime]::UtcNow.ToString('o'); Head = $start.Head
        Outcome = 'BoundedFiniteRecursiveCacheRepair'; RecursiveCacheFixed = $true; GenericContextFixed = $false; ReadyForB25B = $false; SolutionFindingAdmitted = $false
        Passed = ($failures.Count -eq 0); ValidationPassed = ($failures.Count -eq 0); EvidenceConsistent = $true; GateFailures = @($failures.ToArray())
        ClosureModel = 'Finite SCC ancestor-subset dynamic programming; equivalent reachable type/source/uncertainty closure with cycle-erased bounded path representatives, not unrestricted unrolled-path fixed point'
        CacheScope = 'Per Evaluate invocation; graph alone shared between sequential session roots'; CandidateWithdrawn = $false; ProductionFilesChanged = 1
        NewTests = 33; FiniteOracleChecks = 4608; AuthorizedCharacterizationMethodsChanged = 2; AuthorizedCasesChanged = 4; OtherExistingTestsUnchanged = $true
        HistoricalReportsAuditsWitnessesRewritten = $false; OriginalA4FiniteCacheBoundaryClosed = $true; OriginalA4GenericExpansionStillOpen = $true
        Tests = @($tests.ToArray()); Runs = $runs; Dual = (Hash $dualPath); FinalDualPassed = [bool]$dual.Passed; WaeBuilds = $wae; CurrentBaselines = $current; Witnesses = $witnesses; A4BeforeAfter = $beforeAfter
        AcquisitionObservations = $acquisition; ForeignFullFailure = (Hash $foreignPath); PidG1 = [ordered]@{ Unchanged = $true; RuntimeTests = 18; DeterministicCases = 9 }
        SourceCapture = (Hash (Join-Path $records 'source-inputs.json')); CompiledInputs = (Hash (Join-Path $records 'validation-inputs.json')); CompiledSourceVerification = (Hash (Join-Path $records 'compiled-source-verification.json'))
        PdbDocumentBindings = @($bindings.ImageBindings | ForEach-Object Documents).Count; SourceLineEndingOnlyDifferences = $bindings.RawDifferences
        ChangesAndHistoricalEvidenceVerifier = (Hash (Join-Path $records 'changes-and-history.json')); HistoricalFingerprints = $history.HistoricalFingerprints.Count; StaticVerifier = (Hash (Join-Path $records 'static-verification.json'))
        NoRoslynVersionRoutingAcquisitionOrPidRepair = $true; NoSkipsOrGreenRetries = $true; ActualLinuxRun = $false; Network = $false
        SelfAnalysis = [ordered]@{ Fresh = $false; InheritedCount = 16 }; CanonicalFindingDiff = [ordered]@{ Fresh = $false; InheritedAdded = 0; InheritedRemoved = 0; InheritedChanged = 0 }
        Protection = [ordered]@{ HeadUnchanged = $true; SemanticIndexClean = $true; PhysicalIndexUnchanged = $true; IndexSha256 = $start.IndexSha256; IgnoreUnchanged = $true; CoreUnchanged = $true; CoreCount = $start.Core.Count; StashesUnchanged = $true; StashCount = $start.Stashes.Count }
        OwnedFiles = $owned; FinalGitStatus = @(& git --no-optional-locks status --short)
    }
    $path = Join-Path $tool 'Evaluation/P5O2B25A5-recursive-summary-scc-closure-audit.json'
    [IO.File]::WriteAllText($path, (($out | ConvertTo-Json -Depth 100) + "`r`n"), [Text.UTF8Encoding]::new($false))
    $out.FinalGitStatus = @(& git --no-optional-locks status --short)
    [IO.File]::WriteAllText($path, (($out | ConvertTo-Json -Depth 100) + "`r`n"), [Text.UTF8Encoding]::new($false))
    if ($failures.Count -ne 0) { Write-Host 'FAIL: actual validation failures retained; evidence consistent, no waiver/retry.'; exit 1 }
    Write-Host 'PASS: bounded finite recursive cache repair and actual validation evidence consistent; B25B remains closed.'
} finally { Pop-Location }
