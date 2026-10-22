$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records=Join-Path $tool 'artifacts/p5o2b25a4'
$start=Get-Content (Join-Path $records 'start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$validation=(Get-Content (Join-Path $records 'export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$failures=[Collections.Generic.List[object]]::new()
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Hash([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function Json([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Trx([string]$Name,[string]$Path,[int]$Expected,[bool]$Required=$true) {
    if(-not (Test-Path -LiteralPath $Path)) {
        if($Required) { throw ('Required TRX missing: '+$Path) }
        $failures.Add([ordered]@{Gate=$Name;State='Not reached in failed Dual; no stale artifact counted'})
        return [ordered]@{Name=$Name;State='NotReached';Expected=$Expected}
    }
    [xml]$xml=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $c=$xml.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($null -ne $c) ('No counters: '+$Path)
    $runName=if($Name.StartsWith('dual-')) { 'dual' } elseif($Name -eq 'refined-current-model-before-worker-cases') { 'refined-model' } elseif($Name -eq 'combined-a4-a3-a2-a-b25') { 'contexts' } elseif($Name -in @('pid-g1','boundary','baseline-current')) { $Name } else { 'regressions' }
    $run=Json (Join-Path $records ('runs/'+$runName+'.json'))
    $times=$xml.SelectSingleNode('//*[local-name()="Times"]')
    Require ([DateTimeOffset]::Parse($times.start).UtcDateTime -ge [DateTimeOffset]::Parse($run.StartedUtc).UtcDateTime) ('Stale TRX start: '+$Name)
    Require ([DateTimeOffset]::Parse($times.finish).UtcDateTime -le [DateTimeOffset]::Parse($run.FinishedUtc).UtcDateTime) ('TRX outside completed attempt: '+$Name)
    $total=[int]$c.total; $passed=[int]$c.passed; $failed=[int]$c.failed; $skipped=[int]$c.notExecuted
    Require ($total -eq $Expected) ('Unexpected test count '+$Name+': '+$total+' vs '+$Expected)
    $errors=@($xml.SelectNodes('//*[local-name()="UnitTestResult" and @outcome!="Passed"]') | ForEach-Object { [ordered]@{Test=$_.testName;Outcome=$_.outcome;Message=$_.Output.ErrorInfo.Message;Stack=$_.Output.ErrorInfo.StackTrace;Duration=$_.duration} })
    if($passed -ne $total -or $failed -ne 0 -or $skipped -ne 0) { $failures.Add([ordered]@{Gate=$Name;Failures=$errors}) }
    [ordered]@{Name=$Name;Total=$total;Passed=$passed;Failed=$failed;Skipped=$skipped;Started=$times.start;Finished=$times.finish;FreshForRecordedAttempt=$true;Artifact=(Hash $Path);Failures=$errors}
}
Push-Location $tool
try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25A4Proof/Verify-Changes.ps1
    Require ($LASTEXITCODE -eq 0) 'Inherited changes gate failed.'
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25A4Proof/Format-Owned.ps1 -VerifyOnly
    Require ($LASTEXITCODE -eq 0) 'Owned text gate failed.'
    $head=& git --no-optional-locks -C $repo rev-parse HEAD
    Require ($head -ceq $start.Head) 'External HEAD movement.'
    & git --no-optional-locks -C $repo diff --cached --quiet
    Require ($LASTEXITCODE -eq 0) 'Semantic index changed.'
    $stashes=@(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs')
    Require (($stashes -join "`n") -ceq ($start.Stashes -join "`n")) 'Stash inventory changed.'
    Require ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -ceq $start.IgnoreSha256) 'Protected ignore changed.'
    $core=@(Get-ChildItem (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -Recurse -Force -File)
    Require ($core.Count -eq $start.Core.Count) 'Actual Core inventory changed.'
    foreach($file in $start.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) ('Protected Core file changed: '+$file.Path) }
    $staticPath=Join-Path $records 'static-verification.json'; $historyPath=Join-Path $records 'historical-evidence-verification.json'
    Require ((Json $staticPath).Passed -and (Json $historyPath).Passed) 'Static/historical verifier failed.'
    $inputs=Json (Join-Path $records 'validation-inputs.json')
    Require (@($inputs.Sources).Count -eq 1 -and @($inputs.Images).Count -eq 5 -and @($inputs.SharedSources).Count -eq 121) 'Compiled inventory mismatch.'
    $compiledSourcePath=Join-Path $records 'compiled-source-verification.json'
    $compiledSources=Json $compiledSourcePath
    Require ($compiledSources.Passed -and @($compiledSources.Sources).Count -eq 122 -and @($compiledSources.ImageBindings).Count -eq 3) 'Exact PE/PDB/document source binding failed.'
    foreach($s in @($inputs.Sources)+@($inputs.SharedSources)) {
        Require ((Get-FileHash (Join-Path $tool $s.Path)).Hash -ceq $s.WorkspaceSha256) ('Workspace input changed: '+$s.Path)
        Require ((Get-FileHash (Join-Path $validation $s.Path)).Hash -ceq $s.ExportSha256) ('Export input changed: '+$s.Path)
        $proof=@($compiledSources.Sources | Where-Object Path -ceq $s.Path)
        Require ($proof.Count -eq 1 -and $proof[0].WorkspaceSha256 -ceq $s.WorkspaceSha256 -and $proof[0].ExportSha256 -ceq $s.ExportSha256 -and $proof[0].ExactAfterCrlfToLfOnly) ('Source format/PDB binding record differs: '+$s.Path)
    }
    foreach($i in $inputs.Images) { Require ((Get-FileHash (Join-Path $validation $i.Path)).Hash -ceq $i.Sha256) ('Executed image changed after capture: '+$i.Path) }
    $baseline=Json (Join-Path $records 'baseline/state.json')
    foreach($i in $baseline.Images) { Require ((Get-FileHash (Join-Path $validation $i.Path)).Hash -ceq $i.Sha256) ('Production baseline image changed: '+$i.Path) }
    $runs=@(foreach($name in @('baseline-build','baseline-current','model-build','model','refined-model-build','refined-model','dual','contexts','pid-g1','pid-g1-proof','regressions','boundary')) {
        $path=Join-Path $records ('runs/'+$name+'.json'); $run=Json $path
        Require ($null -ne $run.ExitCode) ('Attempt did not complete: '+$name)
        if($run.ExitCode -ne 0 -and $name -ne 'model') { $failures.Add([ordered]@{Gate=$name;ExitCode=$run.ExitCode;Artifact=(Hash $path)}) }
        [ordered]@{Name=$name;ExitCode=$run.ExitCode;Artifact=(Hash $path);Log=(Hash (Join-Path $records ('runs/'+$name+'.log')))}
    })
    Require ((Json (Join-Path $records 'runs/model.json')).ExitCode -eq 1) 'Preserve actual failed fixture-development attempt.'
    [xml]$initial=Get-Content (Join-Path $validation 'artifacts/p5o2b25a4/tests/model.trx') -Raw -Encoding UTF8
    $initialCounters=$initial.SelectSingleNode('//*[local-name()="Counters"]')
    Require ([int]$initialCounters.total -eq 11 -and [int]$initialCounters.failed -eq 2) 'Initial 9/11 development evidence changed.'
    $development=[ordered]@{Passed=9;Total=11;Failed=2;Source=(Hash (Join-Path $records 'model-prototype/ExceptionFlowBoundedGenericContextTests.cs.txt'));Trx=(Hash (Join-Path $validation 'artifacts/p5o2b25a4/tests/model.trx'));Reason='Incorrect NEW fixture assumed lexical edge order; changed to two try fragments with explicit edge order assertions. No production or old test changed.';FinalValidationRetry=$false}
    Require ($development.Source.Sha256 -cne @($inputs.Sources)[0].WorkspaceSha256) 'Development refinement must not be an unchanged-source green retry.'
    $tests=[Collections.Generic.List[object]]::new()
    $counts=[ordered]@{b24=42;b23=36;b22=46;b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=870;focused=977;broad=3278;full=3369}
    foreach($name in $counts.Keys) { $tests.Add((Trx $name (Join-Path $validation ('artifacts/p5o2b24/tests/'+$name+'.trx')) $counts[$name])) }
    $acquisitionObservations=@(foreach($name in @('broad','full')) {
        $path=Join-Path $validation ('artifacts/p5o2b24/tests/'+$name+'.trx')
        [xml]$xml=Get-Content $path -Raw -Encoding UTF8
        $rows=@($xml.SelectNodes('//*[local-name()="UnitTestResult"]') | Where-Object testName -like '*ConcurrentReconstruction_PerformsOneRequestAndOneAttempt*')
        Require ($rows.Count -eq 1) 'Acquisition case must remain in both broad and full.'
        [ordered]@{Group=$name;Test=$rows[0].testName;Outcome=$rows[0].outcome;Duration=$rows[0].duration;Message=$rows[0].Output.ErrorInfo.Message;Artifact=(Hash $path);RootCauseProven=$false;TestOrTimeoutRepaired=$false;IsolatedRetry=$false}
    })
    $tests.Add((Trx 'refined-current-model-before-worker-cases' (Join-Path $validation 'artifacts/p5o2b25a4/tests/refined-model.trx') 11))
    $tests.Add((Trx 'baseline-current' (Join-Path $validation 'artifacts/p5o2b25a4/tests/baseline-current.trx') 8))
    $tests.Add((Trx 'combined-a4-a3-a2-a-b25' (Join-Path $validation 'artifacts/p5o2b25a4/tests/contexts.trx') 93))
    $tests.Add((Trx 'pid-g1' (Join-Path $validation 'artifacts/p5o2b25a4/tests/pid-g1.trx') 18))
    $tests.Add((Trx 'boundary' (Join-Path $validation 'artifacts/p5o2b25a4/tests/boundary.trx') 596))
    $pidProof=Json (Join-Path $records 'runs/pid-g1-proof-cases.json')
    Require (@($pidProof).Count -eq 9 -and @($pidProof | Where-Object { -not $_.Passed }).Count -eq 0) 'Nine deterministic lifetime decisions must pass.'
    $dualRoot=Join-Path $validation 'artifacts/dual-version-build'
    $dualPath=Join-Path $dualRoot 'audit.json'; $dual=Json $dualPath
    if(-not $dual.Passed) { $failures.Add([ordered]@{Gate='complete-dual';State=$dual.State;Passed=$dual.Passed}) }
    $dualCases=@(@('main-boundary','main-boundary.trx',596),@('external-target','external-target.trx',36),@('bounded-findings','bounded-findings.trx',42),@('solution-findings','solution-findings.trx',27),@('assembly-qualified-ownership','assembly-qualified-ownership.trx',14),@('canonical-exception-identity','canonical-exception-identity.trx',20),@('constructed-invocation-context','constructed-invocation-context.trx',17),@('bounded-generic-context','bounded-generic-context.trx',15))
    foreach($c in $dualCases) { $tests.Add((Trx ('dual-'+$c[0]) (Join-Path $dualRoot $c[1]) $c[2] $false)) }
    $formerProbeCases=@(foreach($c in @(@('external-target.trx','*GenuineCli_RejectsWithoutFindingsFallbackOrSuccess*missing-pe*'),@('bounded-findings.trx','*GenuineModeContract_FullNativeFindingEvidenceAndRepeat*True*unused*'))) {
        $path=Join-Path $dualRoot $c[0]
        if(Test-Path $path) {
            [xml]$xml=Get-Content $path -Raw -Encoding UTF8
            $matches=@($xml.SelectNodes('//*[local-name()="UnitTestResult"]') | Where-Object testName -like $c[1])
            Require ($matches.Count -eq 1) 'Former crash case must be individually identifiable.'
            [ordered]@{Test=$matches[0].testName;Outcome=$matches[0].outcome;Duration=$matches[0].duration;Artifact=(Hash $path);HistoricalCrashExplainedOrRepaired=$false}
        } else { [ordered]@{Pattern=$c[1];State='NotReached';HistoricalCrashExplainedOrRepaired=$false} }
    })
    $wae=@(foreach($name in @('0-current-build','1-historical-build','2-current-build','3-historical-build','4-historical-build','5-current-build','worker-0-build','worker-1-build','boundary-test-process-build','projection-pdb-fixture-build','main-boundary-tests-build')) {
        $path=Join-Path $dualRoot ($name+'.log'); $log=Get-Content $path -Raw -Encoding UTF8
        Require ((Get-Item $path).LastWriteTimeUtc -ge [DateTimeOffset]::Parse((Json (Join-Path $records 'runs/dual.json')).StartedUtc).UtcDateTime) ('Stale WAE log: '+$name)
        Require ($log -match '(?m)^\s+0 (Warnung\(en\)|Warning\(s\))\s*$' -and $log -match '(?m)^\s+0 (Fehler|Error\(s\))\s*$') ('WAE log not clean: '+$name)
        Hash $path
    })
    $current=@(foreach($before in Get-ChildItem (Join-Path $records 'baseline/current') -Filter '*.json' -File) {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$before.Name)
        Require ((Get-FileHash $before.FullName).Hash -ceq (Get-FileHash $after).Hash) ('Current baseline changed: '+$before.Name)
        [ordered]@{Name=$before.Name;Before=(Hash $before.FullName);After=(Hash $after);ByteEqual=$true}
    })
    Require ($current.Count -eq 8) 'Eight Current baselines required.'
    $witnesses=@(Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a4/analysis') -Filter '*.json' -File | Sort-Object Name | ForEach-Object {
        $w=Json $_.FullName
        if($_.Name -match '^finite-scc-.*-False.json$') { Require ($w.LostReachableExceptionWithoutUncertainty) 'Finite cache-loss witness missing.' }
        if($_.Name -match '^worker-scc-') { Require (-not $w.CorrectnessProven) 'Parity is not correctness.' }
        Hash $_.FullName
    })
    Require ($witnesses.Count -eq 15) 'Exactly fifteen final A4 witness files required.'
    $owned=@(Get-ChildItem (Join-Path $tool 'Evaluation/P5O2B25A4Proof') -Recurse -File | Sort-Object FullName | ForEach-Object { Hash $_.FullName })
    $owned+=@(foreach($relative in @('Evaluation/P5O2B25A4-bounded-generic-context-expansion.md','Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs','build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md')) { Hash (Join-Path $tool $relative) })
    $out=[ordered]@{Package='P5O2B25A4';GeneratedUtc=[DateTime]::UtcNow.ToString('o');Head=$head;Outcome='BoundaryAudit_StopRule';GenericContextFixed=$false;RecursiveCacheFixed=$false;ReadyForB25B=$false;SolutionFindingAdmitted=$false;
        Passed=($failures.Count -eq 0);ValidationPassed=($failures.Count -eq 0);EvidenceConsistent=$true;GateFailures=@($failures.ToArray());
        RootCause='Generic context is definition-normalized; independently, an active-back-edge-dependent partial frame is cached as complete and reused on an uncaught path.';
        FollowUp='Recursive Summary SCC Closure and Sound Cache Reuse, then re-evaluate bounded generic substitution';
        NoProductionCandidate=$true;AllInheritedProductionAndExistingTestsUnchanged=$true;OriginalFileCount=$start.OriginalFiles.Count;CharacterizationMethodsChanged=0;HistoricalEvidenceChanged=$false;PidG1Unchanged=$true;
        NewTests=15;DevelopmentAttempt=$development;Tests=@($tests.ToArray());Runs=$runs;Dual=(Hash $dualPath);FinalDualPassed=[bool]$dual.Passed;FormerA3NativeProbeCases=$formerProbeCases;AcquisitionObservations=$acquisitionObservations;WaeBuilds=$wae;CurrentBaselines=$current;Witnesses=$witnesses;CompiledInputs=(Hash (Join-Path $records 'validation-inputs.json'));
        SharedSourcesUnchanged=121;MainAndCoreByteEqualToFreshBaseline=$true;HistoricalWorkerImagesBeforeA4NotCaptured=$true;
        HistoricalEvidenceVerifier=(Hash $historyPath);StaticVerification=(Hash $staticPath);CompiledSourceVerification=(Hash $compiledSourcePath);SourceLineEndingOnlyDifferences=$compiledSources.RawDifferences;Changes=(Hash (Join-Path $records 'changes.json'));NoInfrastructureOrRoslynRepair=$true;NoGreenRetries=$true;ActualLinuxRun=$false;Network=$false;
        SelfAnalysis=[ordered]@{Fresh=$false;InheritedCount=16};CanonicalFindingDiff=[ordered]@{Fresh=$false;InheritedAdded=0;InheritedRemoved=0;InheritedChanged=0};
        Protection=[ordered]@{HeadUnchanged=$true;SemanticIndexClean=$true;IndexAtStart=$start.IndexSha256;IndexAtEnd=(Get-FileHash (Join-Path $repo '.git/index')).Hash;IndexRepair=$false;IgnoreUnchanged=$true;CoreCount=$core.Count;CoreUnchanged=$true;StashCount=$stashes.Count;StashesUnchanged=$true};
        OwnedFiles=$owned;FinalGitStatus=@(& git --no-optional-locks status --short)}
    $path=Join-Path $tool 'Evaluation/P5O2B25A4-bounded-generic-context-expansion-audit.json'
    [IO.File]::WriteAllText($path,(($out | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
    $out.FinalGitStatus=@(& git --no-optional-locks status --short)
    [IO.File]::WriteAllText($path,(($out | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
    if($failures.Count -ne 0) { Write-Host 'FAIL: actual final validation failures retained; evidence consistent, no waiver or retry.'; exit 1 }
    Write-Host 'PASS: final validation/evidence consistent; boundary audit only, no generic or recursive-cache repair.'
} finally { Pop-Location }
