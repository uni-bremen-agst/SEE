$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$output=Join-Path $tool 'Evaluation/P5O2B25A3-constructed-generic-summary-context-audit.json'
$failures=[Collections.Generic.List[object]]::new()
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function Tests([string]$Name,[string]$Path,[int]$Total,[DateTime]$After,[bool]$MayNotHaveBeenReached=$false) {
    $exists=Test-Path -LiteralPath $Path
    if(-not $exists -or ($MayNotHaveBeenReached -and (Get-Item -LiteralPath $Path).LastWriteTimeUtc -lt $After)) {
        Require $MayNotHaveBeenReached ('Missing required test artifact: '+$Name)
        $failures.Add([ordered]@{Gate=$Name;Failure='Not reached after fail-closed Dual driver failure; not an intentional test skip'})
        return [ordered]@{Name=$Name;Executed=$false;ExpectedTotal=$Total;OlderArtifactRetained=$exists;OlderArtifact=$(if($exists){Fingerprint $Path}else{$null})}
    }
    [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($counts.total -eq "$Total" -and $counts.executed -eq "$Total" -and $counts.notExecuted -eq '0' -and ([int]$counts.passed+[int]$counts.failed) -eq $Total) ('Incomplete/unexpected/skipped test group: '+$Name)
    Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge $After) ('Stale test result: '+$Name)
    $errors=@($trx.SelectNodes('//*[local-name()="UnitTestResult" and @outcome="Failed"]') | ForEach-Object { [ordered]@{Test=$_.testName;Message=$_.Output.ErrorInfo.Message;Stack=$_.Output.ErrorInfo.StackTrace;Duration=$_.duration} })
    if($errors.Count) { $failures.Add([ordered]@{Gate=$Name;Failures=$errors}) }
    [ordered]@{Name=$Name;Executed=$true;Total=$Total;Passed=[int]$counts.passed;Failed=[int]$counts.failed;Skipped=0;Failures=$errors;Artifact=(Fingerprint $Path)}
}
Push-Location $tool
try {
    [IO.File]::WriteAllText($output,('{"Package":"P5O2B25A3","Passed":false,"State":"Evidence verifier running or interrupted"}'+"`r`n"),[Text.UTF8Encoding]::new($false))
    $start=ReadJson 'artifacts/p5o2b25a3/start.json'
    $export=ReadJson 'artifacts/p5o2b25a3/export.json'; $validation=$export.ToolRoot
    $baseline=ReadJson 'artifacts/p5o2b25a3/baseline/state.json'
    $inputs=ReadJson 'artifacts/p5o2b25a3/validation-inputs.json'
    $since=@($inputs.Images | ForEach-Object { ([DateTime]$_.LastWriteUtc).ToUniversalTime() } | Sort-Object)[-1]
    Require ((& git --no-optional-locks rev-parse HEAD) -ceq $start.Head -and $start.Head -ceq $export.Head) 'External HEAD movement.'
    & git --no-optional-locks diff --cached --quiet
    Require ($LASTEXITCODE -eq 0 -and @(& git --no-optional-locks diff --cached --name-only).Count -eq 0) 'Semantic index changed.'
    Require ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -ceq $start.IgnoreSha256) 'Protected root ignore changed.'
    Require ((Json @(& git --no-optional-locks reflog show refs/stash --format='%H %gs')) -ceq (Json $start.Stashes)) 'Stashes changed.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -File -Recurse).Count -eq $start.Core.Count) 'Protected Core inventory changed.'
    foreach($file in $start.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) ('Protected Core changed: '+$file.Path) }
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25A3Proof/Verify-Changes.ps1
    Require ($LASTEXITCODE -eq 0) 'Original production/tests/history or exact additive audit scope changed.'
    $history=ReadJson 'artifacts/p5o2b25a3/historical-evidence-verification.json'
    $static=ReadJson 'artifacts/p5o2b25a3/static-verification.json'
    Require ($history.Passed -and $history.ReadOnlyHistoricalVerification -and -not $history.FrozenHistoryRewritten) 'Historical verifier not complete.'
    Require ($static.Passed -and $static.DotnetWhitespaceExitCode -eq 0 -and $static.CaseCollisions -eq 0) 'Static verification not complete.'
    Require (-not $baseline.Passed -and $baseline.ExitCode -eq 1 -and $baseline.NoOverlayYet -and $baseline.NoRetry) 'Retained unchanged baseline failure must not be rewritten.'
    Require ($baseline.Failures.Count -eq 1 -and $baseline.Failures[0].Message -match 'AccessViolationException' -and $baseline.Failures[0].Test -like '*scenario: "unused"*') 'Baseline native probe failure differs.'
    $failures.Add([ordered]@{Gate='unchanged-head-baseline-dual';ExitCode=1;Failures=$baseline.Failures;BeforeAnyProductionOrTestOverlay=$true})
    foreach($source in $inputs.Sources) {
        Require ((Get-FileHash (Join-Path $tool $source.Path)).Hash -ceq $source.Sha256 -and (Get-FileHash (Join-Path $validation $source.Path)).Hash -ceq $source.Sha256) ('Compiled source changed: '+$source.Path)
    }
    Require (@($inputs.Sources).Count -eq 1 -and @($inputs.Sources)[0].Path -like '*ExceptionFlowConstructedInvocationContextTests.cs') 'Only the new A3 C# test may be dirty.'
    foreach($image in $inputs.Images) { Require ((Get-FileHash (Join-Path $validation $image.Path)).Hash -ceq $image.Sha256) ('Compiled image changed: '+$image.Path) }
    Require ($baseline.SharedSourceFingerprints.Count -eq 121) 'Shared baseline inventory incomplete.'
    foreach($source in $baseline.SharedSourceFingerprints) {
        Require ((Get-FileHash (Join-Path $validation $source.Path)).Hash -ceq $source.Sha256) ('Shared export source differs: '+$source.Path)
        Require ([IO.File]::ReadAllText((Join-Path $tool $source.Path)).Replace("`r`n","`n") -ceq [IO.File]::ReadAllText((Join-Path $validation $source.Path)).Replace("`r`n","`n")) ('Workspace shared source differs: '+$source.Path)
    }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $dualRun=ReadJson 'artifacts/p5o2b25a3/runs/dual.json'
    Require ((Get-FileHash (Join-Path $validation 'build/Verify-DualVersionBuild.ps1')).Hash -ceq (Get-FileHash 'build/Verify-DualVersionBuild.ps1').Hash) 'Final Dual used a different gate.'
    if(-not $gate.Passed) { $failures.Add([ordered]@{Gate='final-post-change-dual';ExitCode=$dualRun.ExitCode;State=$gate.State}) }
    else {
        Require ($gate.MainBoundaryExecuted -and $gate.RuntimeExecuted -and $gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Successful Dual has incomplete checks.'
        Require ((Json $gate.SharedSourceFingerprints) -ceq (Json $baseline.SharedSourceFingerprints)) 'Shared sources changed.'
    }
    $builds=foreach($file in Get-ChildItem $gateRoot -Filter '*-build.log') {
        Require ($file.LastWriteTimeUtc -ge ([DateTime]$dualRun.StartedUtc).ToUniversalTime()) ('Stale WAE log: '+$file.Name)
        $log=Get-Content $file.FullName -Raw -Encoding UTF8
        Require ($log -match '0 Warnung\(en\)' -and $log -match '0 Fehler') ('Unclean WAE: '+$file.Name)
        Fingerprint $file.FullName
    }
    Require (@($builds).Count -eq 11) 'Eleven actual clean WAE build logs required.'
    $identity=ReadJson (Join-Path $gateRoot 'worker-identity.json')
    Require ($identity.success -and $identity.identity.loadedRoslyn.Count -eq 2) 'Genuine Worker identity missing.'
    foreach($engine in $identity.identity.loadedRoslyn) { Require ($engine.informationalVersion.StartsWith('5.0.0-2.25451.107+')) 'Historical compiler drift.' }
    $binaries=foreach($image in $baseline.Images) {
        $actual=(Get-FileHash (Join-Path $validation $image.Path)).Hash
        Require ($actual -ceq $image.Sha256) ('Unchanged production binary differs: '+$image.Owner)
        [ordered]@{Owner=$image.Owner;Before=$image.Sha256;After=$actual;ByteEqual=$true}
    }
    $expected=[ordered]@{b24=42;b23=36;b22=46;b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=855;focused=962;broad=3263;full=3354}
    $tests=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b24/tests/'+$name+'.trx')) $expected[$name] $since }
    $tests+=@(Tests 'a3-and-unchanged-b25a2-b25a-b25' (Join-Path $validation 'artifacts/p5o2b25a3/tests/contexts.trx') 78 $since)
    $tests+=@(Tests 'pid-g1' (Join-Path $validation 'artifacts/p5o2b25a3/tests/pid-g1.trx') 18 $since)
    $tests+=@(Tests 'boundary' (Join-Path $validation 'artifacts/p5o2b25a3/tests/boundary.trx') 596 $since)
    foreach($item in @(@('main-boundary',596),@('external-target',36),@('bounded-findings',42),@('solution-findings',27),@('assembly-qualified-ownership',14),@('canonical-exception-identity',20),@('constructed-invocation-context',17))) {
        $tests+=@(Tests ('dual-'+$item[0]) (Join-Path $gateRoot ($item[0]+'.trx')) $item[1] ([DateTime]$dualRun.StartedUtc).ToUniversalTime() $true)
    }
    $baselines=foreach($file in Get-ChildItem 'artifacts/p5o2b25a3/baseline/current' -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $after).Hash -ceq (Get-FileHash $file.FullName).Hash) ('Current baseline changed; STOP: '+$file.Name)
        Require ((Get-Item $after).LastWriteTimeUtc -ge $since) 'Current baseline not fresh.'
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight Current cases required.'
    $caseRoot=Join-Path $validation 'artifacts/p5o2b25a3/analysis'
    $cases=foreach($file in Get-ChildItem $caseRoot -Filter '*.json') { $null=ReadJson $file.FullName; Require ($file.LastWriteTimeUtc -ge $since) 'Stale A3 witness.'; Fingerprint $file.FullName }
    Require (@($cases).Count -eq 17) 'Seventeen fresh A3 witnesses required.'
    $recursion=foreach($containing in 'False','True') { foreach($growing in 'False','True') {
        $value=ReadJson (Join-Path $caseRoot ('recursion-'+$containing+'-'+$growing+'.json'))
        Require (-not $value.UnboundedProductRunExecuted -and -not $value.ProductionBudgetAdded -and -not $value.ProductionCorrected) 'Do not turn the finite experiment into a product correction.'
        Require ($value.ExactContextsRegistered -eq $(if($value.Growing){33}else{1}) -and $value.ExactContextsVisited -eq $(if($value.Growing){32}else{1}) -and $value.PendingWorkRemainsAtHarnessLimit -eq $value.Growing) 'Generic expansion boundary witness differs.'
        $value
    } }
    $pidProof=ReadJson 'artifacts/p5o2b25a3/runs/pid-g1-proof-cases.json'
    Require ($pidProof.Count -eq 9 -and @($pidProof | Where-Object { -not $_.Passed }).Count -eq 0) 'PID-G1 proof failed.'
    $runs=foreach($file in Get-ChildItem 'artifacts/p5o2b25a3/runs' -Filter '*.json' | Where-Object Name -notlike '*cases.json') {
        $run=ReadJson $file.FullName; Require ($null -ne $run.ExitCode) ('Incomplete run: '+$file.Name)
        if($run.ExitCode -ne 0) { $failures.Add([ordered]@{Gate=$file.Name;ExitCode=$run.ExitCode}) }
        [ordered]@{Artifact=(Fingerprint $file.FullName);ExitCode=$run.ExitCode}
    }
    $owned=@('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs')
    $owned+=@(Get-ChildItem Evaluation/P5O2B25A3Proof -Recurse -File | ForEach-Object { $_.FullName.Substring($tool.Length+1).Replace('\','/') })
    $owned+=@(Get-ChildItem Evaluation -File -Filter 'P5O2B25A3-*' | ForEach-Object { $_.FullName.Substring($tool.Length+1).Replace('\','/') })
    foreach($path in $owned) { if($path.EndsWith('.json')) { $null=ReadJson $path }; if($path.EndsWith('.ps1')) { $errors=$null; $tokens=$null; $null=[Management.Automation.Language.Parser]::ParseFile((Join-Path $tool $path),[ref]$tokens,[ref]$errors); Require ($errors.Count -eq 0) ('PowerShell syntax: '+$path) } }
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25A3Proof/Format-Owned.ps1 -VerifyOnly
    Require ($LASTEXITCODE -eq 0) 'A3 format/UTF8/CRLF failed.'
    $passed=$failures.Count -eq 0
    $audit=[ordered]@{Package='P5O2B25A3';Passed=$passed;ValidationPassed=$passed;EvidenceConsistent=$true;GateFailures=@($failures.ToArray());Outcome='BoundaryAudit_StopRule';ConstructedContextFixed=$false;ReadyForB25B=$false;SolutionFindingAdmitted=$false;
        GeneratedUtc=[DateTime]::UtcNow.ToString('o');Head=$start.Head;AllProductionAndExistingTestsUnchanged=$true;OriginalFileCount=$start.OriginalFiles.Count;SharedSourcesUnchanged=121;NewTests=17;
        RootCause='Method and containing substitutions are discarded by definition normalization; body semantic types remain open';IndependentBoundary='Exact type-growing generic contexts remove the finite queue bound; graph construction has no bounded recursive-substitution/SCC policy before evaluator active-key guards';
        FollowUp='Bounded Generic Context Expansion and Recursive Substitution Contract; no arbitrary cutoff or B25B activation';RecursionObservations=$recursion;
        BaselinePassed=$baseline.Passed;BaselineNativeFailure=$baseline.Failures;BaselineRootCauseKnown=$false;BaselineNoRetry=$true;BaselineSnapshot=(Fingerprint 'artifacts/p5o2b25a3/baseline/state.json');BaselineDual=(Fingerprint 'artifacts/p5o2b25a3/baseline/dual-version-build/audit.json');FinalDualPassed=$gate.Passed;Dual=(Fingerprint (Join-Path $gateRoot 'audit.json'));
        Tests=$tests;WaeBuilds=$builds;Runs=$runs;CurrentBaselines=$baselines;BinaryImages=$binaries;IdentityWitnesses=$cases;CompiledInputs=(Fingerprint 'artifacts/p5o2b25a3/validation-inputs.json');
        HistoricalEvidenceVerifier=(Fingerprint 'artifacts/p5o2b25a3/historical-evidence-verification.json');StaticVerification=(Fingerprint 'artifacts/p5o2b25a3/static-verification.json');Changes=ReadJson 'artifacts/p5o2b25a3/changes.json';CharacterizationMethodsChanged=0;HistoricalEvidenceChanged=$false;PidG1Unchanged=$true;NoInfrastructureOrRoslynRepair=$true;NoGreenRetries=$true;
        SelfAnalysis=[ordered]@{Fresh=$false;InheritedCount=16};CanonicalFindingDiff=[ordered]@{Fresh=$false;InheritedAdded=0;InheritedRemoved=0;InheritedChanged=0};ActualLinuxRun=$false;Network=$false;
        Protection=[ordered]@{HeadUnchanged=$true;SemanticIndexClean=$true;IndexAtStart=$start.IndexSha256;IndexAtEnd=(Get-FileHash (Join-Path $repo '.git/index')).Hash;IndexRepair=$false;IgnoreUnchanged=$true;CoreCount=$start.Core.Count;CoreUnchanged=$true;StashCount=$start.Stashes.Count;StashesUnchanged=$true};
        OwnedFiles=@($owned | Sort-Object -Unique | Where-Object { $_ -notlike '*-audit.json' } | ForEach-Object { Fingerprint (Join-Path $tool $_) });FinalGitStatus=@(& git --no-optional-locks status --short)}
    [IO.File]::WriteAllText($output,(($audit | ConvertTo-Json -Depth 65)+"`r`n"),[Text.UTF8Encoding]::new($false))
    if(-not $passed) { Write-Host 'FAIL: validation remains red, including retained unchanged-HEAD native probe failure; evidence consistent, no waiver or retry.'; exit 1 }
    Write-Host 'PASS: A3 evidence validated, not a production correction.'
} catch {
    [ordered]@{Package='P5O2B25A3';Passed=$false;EvidenceConsistent=$false;Outcome='BoundaryAudit_StopRule';ConstructedContextFixed=$false;State='Evidence verification failed';Failure=$_.Exception.Message;GeneratedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | ForEach-Object { [IO.File]::WriteAllText($output,($_+"`r`n"),[Text.UTF8Encoding]::new($false)) }
    throw
} finally { Pop-Location }
