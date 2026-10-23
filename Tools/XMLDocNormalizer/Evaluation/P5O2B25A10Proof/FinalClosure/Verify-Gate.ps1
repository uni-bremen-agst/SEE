param([Parameter(Mandatory=$true)][ValidateSet('A6','A7','A8','A9','A10')][string]$Gate)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$validation = Join-Path $tool 'artifacts/b25a10-development-17'
$records = Join-Path $tool 'artifacts/p5o2b25a10/final-r17'
$destination = Join-Path $records ($Gate.ToLowerInvariant() + '-gate.json')
if (Test-Path -LiteralPath $destination) { throw 'Preserve previous gate attempt; no overwrite/retry.' }
$checks = [Collections.Generic.List[object]]::new()
function Check([string]$name,[bool]$passed) { $checks.Add([ordered]@{Name=$name;Passed=$passed}) }
function Json([string]$path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
$errorDetail = $null
try {
    $protection = Json (Join-Path $tool 'Evaluation/P5O2B25A10Proof/implementation-broad-root-cause-state.json')
    foreach ($file in $protection.HistoricalEvidence) { Check ('frozen:' + $file.Path) ((Hash (Join-Path $tool $file.Path)) -ceq $file.Frozen) }
    foreach ($file in $protection.Core) { Check ('protected:' + $file.Path) ((Hash $file.Path) -ceq $file.Actual) }
    Check 'root-ignore' ((Hash (Join-Path $tool '../../.gitignore')) -ceq $protection.RootIgnoreSha256)
    Check 'physical-index' ((Hash (Join-Path $tool '../../.git/index')) -ceq $protection.IndexSha256)
    $stashes = @(& git --no-optional-locks -C $tool stash list --format='%H %gs')
    Check 'stashes' (($stashes -join "`n") -ceq ($protection.Stashes -join "`n"))
    $fullRun = Json (Join-Path $tool 'artifacts/p5o2b25a10/runs/development-17-full.json')
    [xml]$full = Get-Content (Join-Path $validation 'artifacts/a10-closure-results/full.trx') -Raw -Encoding UTF8
    $all = @($full.TestRun.Results.UnitTestResult)
    Check 'no-Full-skips' ($full.TestRun.ResultSummary.Counters.notExecuted -eq '0')
    if ($Gate -ne 'A7') {
        foreach ($phase in @('broad','full','boundary','dual')) {
            $run = Json (Join-Path $tool ('artifacts/p5o2b25a10/runs/development-17-' + $phase + '.json'))
            Check ('required-runtime:' + $phase) ($null -ne $run.ExitCode -and $run.ExitCode -eq 0)
        }
        $binding = Json (Join-Path $records 'compiled-source-verification.json')
        Check 'exact-PDB-source-and-PE-bindings' ([bool]$binding.Passed -and $binding.ImageBindings.Count -eq 3 -and $binding.RawDifferences -eq 0)
    }
    if ($Gate -in @('A6','A10')) {
        foreach ($entry in @(@('ExceptionFlowGenericExpansionBoundaryTests',8),@('ExceptionFlowRecursiveSummaryClosureTests',33),@('ExceptionFlowFiniteOrbitAdmissionTests',19))) {
            $members = @($all | Where-Object testName -like ('*.' + $entry[0] + '.*'))
            Check ('fresh-class:' + $entry[0]) ($members.Count -eq $entry[1] -and @($members | Where-Object outcome -ne 'Passed').Count -eq 0)
        }
        $witnesses = @(Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a10/analysis/a6') -Filter '*.json')
        Check 'A6-eight-fresh-witnesses' ($witnesses.Count -eq 8)
        foreach ($file in $witnesses | Where-Object Name -like 'growth-*') {
            $w = Json $file.FullName
            Check ('A6-production-reject-before-root:' + $file.Name) (!$w.Profile.Complete -and $w.ProductionGraphCount -eq 0 -and @($w.Profile.Reasons | Where-Object Category -eq 'PositiveConstructorCycle').Count -gt 0)
            Check ('A6-independent-prefix-not-bound:' + $file.Name) ($w.WitnessSteps -eq 24 -and $w.ExactCandidateNodes -eq 25 -and $w.FinitePrefixIsNotTerminationProof)
        }
        $dual = Json (Join-Path $validation 'artifacts/dual-version-build/audit.json')
        Check 'complete-Dual' ([bool]$dual.Passed)
        foreach ($name in @('0-current-build','1-historical-build','2-current-build','3-historical-build','4-historical-build','5-current-build','worker-0-build','worker-1-build','boundary-test-process-build','projection-pdb-fixture-build','main-boundary-tests-build')) {
            $log = Get-Content (Join-Path $validation ('artifacts/dual-version-build/' + $name + '.log')) -Raw -Encoding UTF8
            Check ('WAE:' + $name) ($log -match '(?m)^\s+0 (Warnung\(en\)|Warning\(s\))\s*$' -and $log -match '(?m)^\s+0 (Fehler|Error\(s\))\s*$')
        }
        $baselines = @(Get-ChildItem (Join-Path $tool 'artifacts/p5o2b25a9/baseline/current') -Filter '*.json')
        Check 'eight-frozen-Current-baselines' ($baselines.Count -eq 8)
        foreach ($before in $baselines) { Check ('Current-byte-equal:' + $before.Name) ((Hash $before.FullName) -ceq (Hash (Join-Path $validation ('artifacts/p5o2b9/current/' + $before.Name)))) }
    }
    if ($Gate -in @('A7','A8','A9','A10')) {
        $report = Get-Content (Join-Path $tool 'Evaluation/P5O2B25A7-finite-parametric-generic-context-contract.md') -Raw -Encoding UTF8
        Check 'A7-general-scope-still-STOP' ($report.Contains('Decision: **STOP - no production implementation GO**.'))
        foreach ($number in 1..14) { Check ('A7-obligation:F' + $number) ($report -match ('(?m)^\| F' + $number + ' ')) }
    }
    if ($Gate -in @('A8','A9','A10')) {
        $inputs = Json (Join-Path $records 'proof-inputs.json')
        foreach ($input in $inputs) {
            foreach ($file in $input.SourceFiles) {
                Check ('proof-source-binding:' + $input.Id + ':' + [IO.Path]::GetFileName($file.Original)) ((Hash $file.Original) -ceq $file.Sha256 -and (Hash $file.Copied) -ceq $file.Sha256)
            }
            Check ('proof-image-binding:' + $input.Id) ((Hash $input.Image) -ceq $input.ImageSha256)
            Check ('proof-result-binding:' + $input.Id) ((Hash $input.Result) -ceq $input.ResultSha256)
        }
        $a8 = Json (Join-Path $records 'a8-proof.json')
        Check 'A8-fresh-40-checks' ($a8.Checks.Count -eq 40 -and @($a8.Checks | Where-Object { !$_.Passed }).Count -eq 0)
        Check 'A8-not-general-or-product-proof' (!$a8.ProductAnalyzerExecuted -and !$a8.GeneralUndecidabilityProvedByFiniteHarness -and !$a8.InfiniteRuntimeAnalysisExecuted)
    }
    if ($Gate -in @('A9','A10')) {
        $a9 = Json (Join-Path $records 'a9-proof.json')
        Check 'A9-fresh-37-checks' ($a9.Checks.Count -eq 37 -and @($a9.Checks | Where-Object { !$_.Passed }).Count -eq 0)
        Check 'A9-bounded-profile-not-general-proof' ($a9.Profile -ceq 'A9-FiniteOrbit-v1' -and $a9.NoProductDependency -and !$a9.ProductAnalyzerExecuted -and !$a9.TerminationProofByFiniteEnumeration)
        Check 'A9-outside-without-context-materialization' (!$a9.Growth.Exact -and $a9.Growth.ConcreteContextsMaterialized -eq 0 -and !$a9.HardPattern.Exact -and $a9.HardPattern.ConcreteContextsMaterialized -eq 0 -and $a9.Reset.Exact)
    }
    if ($Gate -eq 'A10') {
        $production = Json (Join-Path $records 'production-proof.json')
        Check 'additional-actual-production-queries' ($production.Passed -and @($production.Checks | Where-Object { !$_.Passed }).Count -eq 0 -and $production.Entry -ceq 'ExceptionFlowSummaryAnalysisSession.Analyze')
        Check 'no-RawGraph-or-new-facade-certification' (!$production.RawGraphsUsed -and !$production.ExistingTestMethodsExecuted -and !$production.SubmittedSourceExecuted -and !$production.NewProductionFacade)
        foreach ($name in @('production-A-B-C-admitted','production-A-B-C-composed-substitution','production-A-B-C-missing-scope-fail-closed','production-cross-scope-growth-outside-before-root')) {
            Check ('required-query:' + $name) (@($production.Checks | Where-Object { $_.Name -ceq $name -and $_.Passed }).Count -eq 1)
        }
        foreach ($file in @('HistoricalWorkerResponseValidation.cs','HistoricalAnalysis.cs')) {
            $relative = if ($file -eq 'HistoricalAnalysis.cs') { 'src/XMLDocNormalizer.HistoricalWorker/' + $file } else { 'src/XMLDocNormalizer/Execution/Historical/' + $file }
            Check ('no-new-envelope-change:' + $file) ((Hash (Join-Path $tool $relative)) -ceq (Hash (Join-Path $tool ('artifacts/b25a10-development-16/' + $relative))))
        }
    }
} catch { $errorDetail = $_.Exception.ToString(); Check 'complete-gate-execution' $false }
$failed = @($checks | Where-Object { !$_.Passed })
$record = [ordered]@{
    Gate=$Gate; Package='P5O2B25A10'; Adapter='A10-owned regression/contract adapter, NOT a rewrite or replay of historical audit output'
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Passed=$failed.Count -eq 0; Checks=@($checks.ToArray()); Failures=$failed; Error=$errorDetail
    FullRemainsFailed=$true; HistoricalContractChecksAreNotA10ProductionProof=$true
    HistoricalS1Exit1Preserved=$true; HistoricalEvidenceRewritten=$false; B25BActivated=$false; A10Complete=$false
}
[IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 15)+"`r`n"),[Text.UTF8Encoding]::new($false))
if ($failed.Count) { Write-Host ($Gate + ' FAIL: ' + (($failed | ForEach-Object Name) -join ', ')); exit 1 }
Write-Host ($Gate + ' PASS for this explicitly described adapter scope; Full failure is not waived.')
