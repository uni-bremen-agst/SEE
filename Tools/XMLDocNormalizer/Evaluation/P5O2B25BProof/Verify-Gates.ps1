$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $PSScriptRoot 'implementation-export.json') -Raw | ConvertFrom-Json).Export
$records=Join-Path $PSScriptRoot 'Results'
$output=Join-Path $records 'b25b-gates.json'
if(Test-Path -LiteralPath $output) { throw 'Preserve previous gate attempt' }
$checks=[Collections.Generic.List[object]]::new()
function Check([string]$name,[bool]$passed) { $checks.Add([ordered]@{Name=$name;Passed=$passed}) }
function Json([string]$path) { Get-Content -LiteralPath $path -Raw | ConvertFrom-Json }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
try {
    $protection=Json (Join-Path $PSScriptRoot 'final-protection.json')
    Check 'protection-all-763' ($protection.Passed -and $protection.Checks.Count -eq 763)
    foreach($name in @('targeted','broad','broad-harness','boundary','harness-isolated','full-harness','dual')) {
        $run=Json (Join-Path $records ($name + '.json'))
        Check ('runtime:'+$name) ($null -ne $run.ExitCode -and $run.ExitCode -eq 0)
        if($name -ne 'dual') { Check ('no-skips:'+$name) ($run.Counters.notExecuted -eq '0' -and $run.Counters.failed -eq '0') }
    }
    [xml]$full=Get-Content (Join-Path $records 'full-harness.trx') -Raw
    $delta=Json (Join-Path $records 'harness-coordination-delta.json')
    Check 'local-harness-only-delta' ($delta.ExactCoordinationWrapperOnly -and $delta.AllExistingAssertionsUnchanged -and $delta.TimeoutUnchanged -and $delta.ProductionChanges -eq 0 -and (Hash (Join-Path $tool $delta.Path)) -ceq $delta.AfterSha256)
    $before=Json (Join-Path $records 'scheduler-before.json'); $after=Json (Join-Path $records 'scheduler-after.json')
    Check 'controlled-context-counterexample-before-after' ($before.CapturedCallerContext -and !$before.Passed -and $after.CapturedCallerContext -and $after.Passed -and !$before.OfficialGate -and !$after.OfficialGate)
    [xml]$redFull=Get-Content (Join-Path $records 'full.trx') -Raw
    Check 'historical-red-Full-preserved' ($redFull.TestRun.ResultSummary.Counters.total -eq '3449' -and $redFull.TestRun.ResultSummary.Counters.passed -eq '3448' -and $redFull.TestRun.ResultSummary.Counters.failed -eq '1')
    $all=@($full.TestRun.Results.UnitTestResult)
    foreach($entry in @(@('ExceptionFlowGenericExpansionBoundaryTests',8),@('ExceptionFlowRecursiveSummaryClosureTests',33),@('ExceptionFlowFiniteOrbitAdmissionTests',19),@('ExceptionFlowHistoricalSolutionFindingTests',27),@('ExceptionFlowHistoricalBoundedFindingTests',42),@('ExceptionFlowHistoricalSolutionAdoptionTests',20))) {
        $members=@($all | Where-Object testName -like ('*.'+$entry[0]+'.*'))
        Check ('class:'+ $entry[0]) ($members.Count -eq $entry[1] -and @($members | Where-Object outcome -ne 'Passed').Count -eq 0)
    }
    $binding=Json (Join-Path $records 'compiled-source-verification.json')
    Check 'all-actual-Main-Historical-Test-PDB-source-bindings' ($binding.Passed -and $binding.ImageBindings.Count -eq 3 -and $binding.RawDifferences -eq 0)
    $dual=Json (Join-Path $validation 'artifacts/dual-version-build/audit.json')
    Check 'unchanged-Dual-Version-gate' ([bool]$dual.Passed)
    foreach($name in @('0-current-build','1-historical-build','2-current-build','3-historical-build','4-historical-build','5-current-build','worker-0-build','worker-1-build','boundary-test-process-build','projection-pdb-fixture-build','main-boundary-tests-build')) {
        $log=Get-Content (Join-Path $validation ('artifacts/dual-version-build/'+$name+'.log')) -Raw
        Check ('WAE:'+ $name) ($log -match '(?m)^\s+0 (Warnung\(en\)|Warning\(s\))\s*$' -and $log -match '(?m)^\s+0 (Fehler|Error\(s\))\s*$')
    }
    $baseline=Join-Path $env:TEMP 'a10-277b50ca/artifacts/p5o2b9/current'
    $files=@(Get-ChildItem -LiteralPath $baseline -Filter '*.json' -File)
    Check 'eight-A10-Current-baselines-retained' ($files.Count -eq 8)
    foreach($file in $files) { Check ('Current-byte-equal:'+ $file.Name) ((Hash $file.FullName) -ceq (Hash (Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)))) }
    $a6=@(Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a10/analysis/a6') -Filter '*.json' -File)
    Check 'A6-eight-fresh-witnesses' ($a6.Count -eq 8)
    foreach($file in $a6 | Where-Object Name -like 'growth-*') {
        $w=Json $file.FullName
        Check ('A6-before-root:'+ $file.Name) (!$w.Profile.Complete -and $w.ProductionGraphCount -eq 0 -and @($w.Profile.Reasons | Where-Object Category -eq 'PositiveConstructorCycle').Count -gt 0)
        Check ('A6-independent-prefix-not-limit:'+ $file.Name) ($w.WitnessSteps -eq 24 -and $w.ExactCandidateNodes -eq 25 -and $w.FinitePrefixIsNotTerminationProof)
    }
    $report=Get-Content (Join-Path $tool 'Evaluation/P5O2B25A7-finite-parametric-generic-context-contract.md') -Raw
    Check 'A7-general-scope-still-STOP' ($report.Contains('Decision: **STOP - no production implementation GO**.'))
    foreach($number in 1..14) { Check ('A7-obligation:F'+$number) ($report -match ('(?m)^\| F'+$number+' ')) }
    $inputs=Json (Join-Path $records 'proof-inputs.json')
    foreach($input in $inputs) {
        foreach($file in $input.SourceFiles) { Check ('proof-source:'+ $input.Id+':'+[IO.Path]::GetFileName($file.Original)) ((Hash $file.Original) -ceq $file.Sha256 -and (Hash $file.Copied) -ceq $file.Sha256) }
        Check ('proof-image:'+ $input.Id) ((Hash $input.Image) -ceq $input.ImageSha256)
        Check ('proof-output:'+ $input.Id) ((Hash $input.Result) -ceq $input.ResultSha256)
    }
    $a8=Json (Join-Path $records 'a8-proof.json')
    Check 'A8-fresh-40-checks' ($a8.Checks.Count -eq 40 -and @($a8.Checks | Where-Object { !$_.Passed }).Count -eq 0)
    Check 'A8-not-general-product-proof' (!$a8.ProductAnalyzerExecuted -and !$a8.GeneralUndecidabilityProvedByFiniteHarness -and !$a8.InfiniteRuntimeAnalysisExecuted)
    $a9=Json (Join-Path $records 'a9-proof.json')
    Check 'A9-fresh-37-checks' ($a9.Checks.Count -eq 37 -and @($a9.Checks | Where-Object { !$_.Passed }).Count -eq 0)
    Check 'A9-bounded-profile-not-general-proof' ($a9.Profile -ceq 'A9-FiniteOrbit-v1' -and $a9.NoProductDependency -and !$a9.ProductAnalyzerExecuted -and !$a9.TerminationProofByFiniteEnumeration)
    Check 'A9-no-unbounded-materialization' (!$a9.Growth.Exact -and $a9.Growth.ConcreteContextsMaterialized -eq 0 -and !$a9.HardPattern.Exact -and $a9.HardPattern.ConcreteContextsMaterialized -eq 0 -and $a9.Reset.Exact)
    $production=Json (Join-Path $records 'production-proof.json')
    Check 'A10-real-Session-entry-queries' ($production.Passed -and @($production.Checks | Where-Object { !$_.Passed }).Count -eq 0 -and $production.Entry -ceq 'ExceptionFlowSummaryAnalysisSession.Analyze')
    Check 'A10-no-RawGraph-or-new-facade' (!$production.RawGraphsUsed -and !$production.ExistingTestMethodsExecuted -and !$production.SubmittedSourceExecuted -and !$production.NewProductionFacade)
    foreach($name in @('production-A-B-C-admitted','production-A-B-C-composed-substitution','production-A-B-C-missing-scope-fail-closed','production-cross-scope-growth-outside-before-root')) {
        Check ('A10-query:'+ $name) (@($production.Checks | Where-Object { $_.Name -ceq $name -and $_.Passed }).Count -eq 1)
    }
    $start=Json (Join-Path $PSScriptRoot 'Preflight/start.json')
    $repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
    foreach($file in $start.CodeAndTestFiles | Where-Object { $_.Path -match '/Tests/' -and $_.Path -notmatch '(ExceptionFlowHistoricalSolutionFindingTests|ExceptionFlowAssemblyQualifiedOwnershipTests|ExternalSourceLineEndingReconstructionTests)\.cs$' }) {
        Check ('other-existing-test-unchanged:'+ $file.Path) ((Hash (Join-Path $repo $file.Path)) -ceq $file.Sha256)
    }
    $relative='Tools/XMLDocNormalizer/src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs'
    $before=@(& git.exe --no-optional-locks -C $repo show ('HEAD:'+ $relative)) -join "`n"
    $after=(Get-Content (Join-Path $repo $relative) -Raw).Replace("`r`n","`n")
    foreach($bounds in @(@('                if (!response.Success)','                if (exitCode != 0)'),@('        private static bool ValidIdentity(','        private static void RejectDuplicateProperties('))) {
        $oldStart=$before.IndexOf($bounds[0],[StringComparison]::Ordinal); $newStart=$after.IndexOf($bounds[0],[StringComparison]::Ordinal)
        $oldEnd=$before.IndexOf($bounds[1],$oldStart,[StringComparison]::Ordinal); $newEnd=$after.IndexOf($bounds[1],$newStart,[StringComparison]::Ordinal)
        Check ('unchanged-failure-version-contract:'+ $bounds[0].Trim()) ($before.Substring($oldStart,$oldEnd-$oldStart) -ceq $after.Substring($newStart,$newEnd-$newStart))
    }
    # Shared analyzer, Worker/protocol/selection policy and independent B24 tests must remain byte-identical.
    foreach($file in $start.CodeAndTestFiles | Where-Object { $_.Path -match 'ExceptionFlow\.Historical/|XMLDocNormalizer\.HistoricalWorker/|ExceptionFlowFiniteOrbit|ExceptionFlowSummary|ExceptionFlowAnalyzerSelection|HistoricalBoundedFindingTests\.cs|WorkerProtocol\.cs|HistoricalSources\.props' }) {
        Check ('unchanged-semantic-boundary:'+ $file.Path) ((Hash (Join-Path ([IO.Path]::GetFullPath((Join-Path $tool '../..'))) $file.Path)) -ceq $file.Sha256)
    }
    $witness=Join-Path $validation 'artifacts/p5o2b25b/adoption'
    foreach($name in @('W2-chain','W3-chain','W2-same-name-types','W4-original-stages','W4-equal-results-different-graphs','W5-growing','W5-uncovered-call','W5-generic-final','W6-wrong-physical-worker','direct-summary','generic-catch','containing')) { Check ('B25B-witness:'+ $name) (Test-Path -LiteralPath (Join-Path $witness ($name+'.json'))) }
    foreach($count in 2..3) { foreach($format in @('console','json','sarif')) { Check ("B25B-CLI:$count-$format") (Test-Path -LiteralPath (Join-Path $witness ("W6-cli-$count-$format.json"))) } }
} catch { $errorDetail=$_.Exception.ToString(); Check 'complete-gate-execution' $false }
$failed=@($checks | Where-Object { !$_.Passed })
[ordered]@{Package='P5O2B25B';Passed=$failed.Count -eq 0;CapturedUtc=[DateTime]::UtcNow.ToString('o');Checks=@($checks.ToArray());Failures=$failed;Error=$errorDetail;
    ContractAdapters=@('A6','A7','A8','A9','A10','B25B');HistoricalEvidenceRewritten=$false;S1HistoricalExit1Preserved=$true;GeneralSolutionSupport=$false;NewWireDto=$false} |
    ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $output -Encoding UTF8
if($failed.Count) { Write-Host ($failed.Name -join ', '); exit 1 }
Write-Host ('PASS: '+$checks.Count+' checks; B25B-owned A6-A10 and B25B closure adapters')
