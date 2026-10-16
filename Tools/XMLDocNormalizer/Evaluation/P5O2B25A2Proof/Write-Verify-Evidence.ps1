$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$output=Join-Path $tool 'Evaluation/P5O2B25A2-canonical-exception-type-identity-audit.json'
$failures=[Collections.Generic.List[object]]::new()
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function Tests([string]$Name,[string]$Path,[int]$Total,[DateTime]$After) {
    [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($counts.total -eq "$Total" -and $counts.executed -eq "$Total" -and $counts.notExecuted -eq '0' -and
        ([int]$counts.passed+[int]$counts.failed) -eq $Total) "Unexpected/incomplete/skipped $Name"
    Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge $After) "Stale $Name"
    $errors=@($trx.SelectNodes('//*[local-name()="UnitTestResult" and @outcome="Failed"]') | ForEach-Object {
        [ordered]@{Test=$_.testName;Message=$_.Output.ErrorInfo.Message;Stack=$_.Output.ErrorInfo.StackTrace;Duration=$_.duration}
    })
    if([int]$counts.failed) { $failures.Add([ordered]@{Gate=$Name;Failed=[int]$counts.failed;Failures=$errors}) }
    [ordered]@{Name=$Name;Total=$Total;Passed=[int]$counts.passed;Failed=[int]$counts.failed;Skipped=0;Failures=$errors;Artifact=(Fingerprint $Path)}
}
Push-Location $tool
try {
    [IO.File]::WriteAllText($output,('{"Package":"P5O2B25A2","Passed":false,"State":"Verifier running or interrupted"}'+"`r`n"),[Text.UTF8Encoding]::new($false))
    $start=ReadJson 'artifacts/p5o2b25a2/start/audit.json'
    $export=ReadJson 'artifacts/p5o2b25a2/export.json'; $validation=$export.ToolRoot
    $parent=ReadJson 'artifacts/p5o2b25a2/baseline/dual-version-build/audit.json'
    $inputs=ReadJson 'artifacts/p5o2b25a2/validation-inputs.json'
    $since=@($inputs.Images | ForEach-Object { ([DateTime]$_.LastWriteUtc).ToUniversalTime() } | Sort-Object)[-1]
    Require ((& git --no-optional-locks rev-parse HEAD) -ceq $start.Head -and $start.Head -ceq $export.Head) 'External HEAD movement.'
    & git --no-optional-locks diff --cached --quiet
    Require ($LASTEXITCODE -eq 0 -and @(& git --no-optional-locks diff --cached --name-only).Count -eq 0) 'Semantic index changed.'
    Require ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -ceq $start.IgnoreSha256) 'Protected root ignore changed.'
    Require ((Json @(& git --no-optional-locks reflog show refs/stash --format='%H %gs')) -ceq (Json $start.Stashes)) 'Stashes changed.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq $start.Core.Count) 'Actual Core inventory changed.'
    foreach($file in $start.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Protected Core changed: $($file.Path)" }
    foreach($file in $start.OriginalInputs | Where-Object Path -ne 'build/Verify-DualVersionBuild.ps1') {
        Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) "Original production/test/build input changed: $($file.Path)"
    }
    & git --no-optional-locks diff --quiet HEAD -- src
    Require ($LASTEXITCODE -eq 0) 'Stop audit must have no production changes.'
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25A2Proof/Capture-WipDelta.ps1
    Require ($LASTEXITCODE -eq 0) 'Inherited-WIP delta check failed.'
    $deltas=ReadJson 'artifacts/p5o2b25a2/wip-deltas.json'
    $history=ReadJson 'artifacts/p5o2b25a2/historical-evidence-verification.json'
    Require ($history.Passed -and $history.ReadOnlyHistoricalVerification -and -not $history.FrozenHistoryRewritten) 'Read-only historical verifier missing.'
    $static=ReadJson 'artifacts/p5o2b25a2/static-verification.json'
    Require ($static.Passed -and $static.DotnetWhitespaceExitCode -eq 0 -and $static.CaseCollisions -eq 0 -and -not $static.ActualLinuxExecution) 'Static verification missing or overstated.'
    foreach($source in $inputs.Sources) {
        Require ((Get-FileHash (Join-Path $tool $source.Path)).Hash -ceq $source.Sha256 -and
            (Get-FileHash (Join-Path $validation $source.Path)).Hash -ceq $source.Sha256) "Compiled source changed: $($source.Path)"
    }
    Require (@($inputs.Sources).Count -eq 2) 'Only inherited B25A and new A2 C# tests should be dirty.'
    foreach($image in $inputs.Images) { Require ((Get-FileHash (Join-Path $validation $image.Path)).Hash -ceq $image.Sha256) "Tested image changed: $($image.Path)" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    Require ((Get-FileHash (Join-Path $validation 'build/Verify-DualVersionBuild.ps1')).Hash -ceq
        (Get-FileHash (Join-Path $tool 'build/Verify-DualVersionBuild.ps1')).Hash) 'Final Dual did not use the exact A2 gate.'
    Require ($parent.Passed -and $gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted -and $gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Dual-version build incomplete.'
    Require ((Json $gate.SharedSourceFingerprints) -ceq (Json $parent.SharedSourceFingerprints)) 'Shared Analyzer sources changed.'
    foreach($source in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash (Join-Path $validation $source.Path)).Hash -ceq $source.Sha256) "Shared export source differs: $($source.Path)"
        Require ([IO.File]::ReadAllText((Join-Path $tool $source.Path)).Replace("`r`n","`n") -ceq
            [IO.File]::ReadAllText((Join-Path $validation $source.Path)).Replace("`r`n","`n")) "Shared workspace content differs: $($source.Path)"
    }
    foreach($property in 'HistoricalPackages','CurrentPackages','WorkerPackages','HistoricalVersion') {
        Require ((Json $gate.$property) -ceq (Json $parent.$property)) "Dependency/version drift: $property"
    }
    Require (@($gate.Steps | Where-Object ExitCode -ne 0).Count -eq 0) 'Dual step failure.'
    $builds=foreach($file in Get-ChildItem $gateRoot -Filter '*-build.log') {
        Require ($file.LastWriteTimeUtc -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale WAE log: $($file.Name)"
        $log=Get-Content $file.FullName -Raw -Encoding UTF8
        Require ($log -match '0 Warnung\(en\)' -and $log -match '0 Fehler') "WAE not clean: $($file.Name)"
        Fingerprint $file.FullName
    }
    Require (@($builds).Count -eq 11) 'All eleven WAE steps required.'
    $expected=[ordered]@{b24=42;b23=36;b22=46;b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=838;focused=945;broad=3246;full=3337}
    $tests=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b24/tests/'+$name+'.trx')) $expected[$name] $since }
    $tests+=@(Tests 'new-a2-and-unchanged-b25a-b25' (Join-Path $validation 'artifacts/p5o2b25a2/tests/identity.trx') 61 $since)
    $tests+=@(Tests 'pid-g1' (Join-Path $validation 'artifacts/p5o2b25a2/tests/pid-g1.trx') 18 $since)
    $tests+=@(Tests 'boundary' (Join-Path $validation 'artifacts/p5o2b25a2/tests/boundary.trx') 596 $since)
    foreach($item in @(@('main-boundary',596),@('external-target',36),@('bounded-findings',42),@('solution-findings',27),@('assembly-qualified-ownership',14),@('canonical-exception-identity',20))) {
        $tests+=@(Tests ('dual-'+$item[0]) (Join-Path $gateRoot ($item[0]+'.trx')) $item[1] $since)
    }
    $timing=foreach($group in 'broad','full') {
        [xml]$trx=Get-Content (Join-Path $validation ('artifacts/p5o2b24/tests/'+$group+'.trx')) -Raw -Encoding UTF8
        $test=@($trx.SelectNodes('//*[local-name()="UnitTestResult"]') | Where-Object testName -like '*ExternalSourceLineEndingReconstructionTests.ConcurrentReconstruction_PerformsOneRequestAndOneAttempt*')
        Require ($test.Count -eq 1) 'Unmodified Acquisition concurrency test missing.'
        [ordered]@{Group=$group;Name=$test[0].testName;Outcome=$test[0].outcome;Duration=$test[0].duration;Message=$test[0].Output.ErrorInfo.Message}
    }
    $baselines=foreach($file in Get-ChildItem 'artifacts/p5o2b25a2/baseline/current' -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $after).Hash -ceq (Get-FileHash $file.FullName).Hash) "Current baseline changed; STOP: $($file.Name)"
        Require ((Get-Item $after).LastWriteTimeUtc -ge $since) 'Current baseline not freshly executed.'
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight Current baselines required.'
    $cases=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25a2/analysis') -Filter '*.json') {
        $null=ReadJson $file.FullName
        Require ($file.LastWriteTimeUtc -ge $since) 'Stale identity witness.'
        Fingerprint $file.FullName
    }
    Require (@($cases).Count -eq 19) 'Thirteen compiler, three source-origin and three genuine Worker witnesses required.'
    $origin=ReadJson (Join-Path $validation 'artifacts/p5o2b25a2/analysis/summary-origin-two-constructed-callees.json')
    Require (-not $origin.ExactExpectedSourcePresent -and $origin.SourceTypes.Count -eq 1 -and $origin.UncertainTargets.Count -eq 0) 'Generic call-context boundary witness missing.'
    foreach($name in 'closed-source','constructed-callee','two-constructed-callees') {
        $case=ReadJson (Join-Path $validation ('artifacts/p5o2b25a2/analysis/worker-construction-loss-'+$name+'.json'))
        Require ($case.Parity -and -not $case.CorrectConstructedType -and -not $case.ProductionRepaired -and -not $case.SolutionFindingAdmission) 'Do not turn shared defect parity into correctness.'
    }
    $pidProof=ReadJson 'artifacts/p5o2b25a2/runs/pid-g1-proof-cases.json'
    Require ($pidProof.Count -eq 9 -and @($pidProof | Where-Object { -not $_.Passed }).Count -eq 0) 'PID-G1 deterministic proof failed.'
    $runs=foreach($file in Get-ChildItem 'artifacts/p5o2b25a2/runs' -Filter '*.json' | Where-Object Name -notlike '*cases.json') {
        $run=ReadJson $file.FullName
        Require ($null -ne $run.ExitCode) "Incomplete run: $($file.Name)"
        if($run.ExitCode -ne 0) { $failures.Add([ordered]@{Gate=$file.Name;ExpectedExitCode=0;ActualExitCode=$run.ExitCode}) }
        [ordered]@{Artifact=(Fingerprint $file.FullName);ExitCode=$run.ExitCode;InitialObservation=($file.Name -like '*model*')}
    }
    $binaryChanges=foreach($owner in 'current','historical') {
        $old=@($parent.BuildImages | Where-Object Owner -eq $owner)[-1].Image
        $new=@($gate.BuildImages | Where-Object Owner -eq $owner)[-1].Image
        Require ($old.Sha256 -ceq $new.Sha256) "Production binary changed: $owner"
        [ordered]@{Owner=$owner;Before=$old.Sha256;After=$new.Sha256;ByteEqual=$true}
    }
    Require (@($parent.WorkerBuildImages)[-1].Sha256 -ceq @($gate.WorkerBuildImages)[-1].Sha256) 'Worker host binary changed.'
    $binaryChanges+=@([ordered]@{Owner='worker-host';Before=@($parent.WorkerBuildImages)[-1].Sha256;After=@($gate.WorkerBuildImages)[-1].Sha256;ByteEqual=$true})
    $owned=@('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs')
    $owned+=@(Get-ChildItem Evaluation/P5O2B25A2Proof -Recurse -File | ForEach-Object { $_.FullName.Substring($tool.Length+1).Replace('\','/') })
    $owned+=@(Get-ChildItem Evaluation -Filter 'P5O2B25A2-*' -File | ForEach-Object { $_.FullName.Substring($tool.Length+1).Replace('\','/') })
    foreach($path in $owned) {
        if($path.EndsWith('.json')) { $null=ReadJson $path }
        if($path.EndsWith('.ps1')) { $errors=$null; $tokens=$null; $null=[Management.Automation.Language.Parser]::ParseFile((Join-Path $tool $path),[ref]$tokens,[ref]$errors); Require ($errors.Count -eq 0) "PS syntax: $path" }
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25A2Proof/Format-Owned.ps1 -VerifyOnly
    Require ($LASTEXITCODE -eq 0) 'A2 UTF8/CRLF check failed.'
    & git --no-optional-locks diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace check failed.'
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($path in @(& git --no-optional-locks -C $repo ls-files)+@(& git --no-optional-locks -C $repo ls-files --others --exclude-standard)) { Require ($seen.Add($path)) "Git case collision: $path" }
    $passed=$failures.Count -eq 0
    $audit=[ordered]@{Package='P5O2B25A2';Passed=$passed;ValidationPassed=$passed;EvidenceConsistent=$true;GateFailures=@($failures.ToArray());Outcome='BoundaryAudit_StopRule';OwnershipFixed=$false;ReadyForB25B=$false;SolutionFindingAdmitted=$false;
        GeneratedUtc=[DateTime]::UtcNow.ToString('o');Head=$start.Head;AllProductionAndExistingTestsUnchanged=$true;SharedSourcesUnchanged=121;
        RootCause='Declaration-ID definition normalization and OriginalDefinition catch comparison lose complete type identity';
        IndependentBoundary='Generic callable definition normalization discards call-site substitutions before exception normalization; canonical summary contexts/edges do not retain instantiation';
        FollowUp='Explicit generic callable-instantiation/substitution contract and runtime catch equivalence, then exact unique canonical rebinding; no B25B activation';
        Tests=$tests;CompilerModelInitialTests=17;NewFinalTests=20;IdentityWitnesses=$cases;CurrentBaselines=$baselines;AcquisitionConcurrencyObservations=$timing;NoAcquisitionOrPidRepair=$true;
        WaeBuilds=$builds;Dual=(Fingerprint (Join-Path $gateRoot 'audit.json'));BaselineDual=(Fingerprint 'artifacts/p5o2b25a2/baseline/dual-version-build/audit.json');Runs=$runs;BinaryImages=$binaryChanges;
        ProtectionSnapshot=(Fingerprint 'artifacts/p5o2b25a2/start/audit.json');CompiledInputs=(Fingerprint 'artifacts/p5o2b25a2/validation-inputs.json');PidG1Proof=(Fingerprint 'artifacts/p5o2b25a2/runs/pid-g1-proof-cases.json');
        HistoricalEvidenceVerifier=(Fingerprint 'artifacts/p5o2b25a2/historical-evidence-verification.json');StaticVerification=(Fingerprint 'artifacts/p5o2b25a2/static-verification.json');HistoricalB25AndB25AFrozen=$true;InheritedB25AWipCount=$start.B25AWip.Count;OverlappingWipDeltas=$deltas;AuthorizedCharacterizationMethodsChanged=0;HistoricalWitnessFilesUpdated=0;
        FormatJsonUtf8Crlf=$true;GitCaseAudit=$true;ActualLinuxRun=$false;Network=$false;SelfAnalysis=[ordered]@{Fresh=$false;InheritedCount=16};CanonicalFindingDiff=[ordered]@{Fresh=$false;InheritedAdded=0;InheritedRemoved=0;InheritedChanged=0};
        Protection=[ordered]@{HeadUnchanged=$true;SemanticIndexClean=$true;PhysicalIndexAtStart=$start.IndexSha256;PhysicalIndexAtEnd=(Get-FileHash (Join-Path $repo '.git/index')).Hash;OldB25AIndexHash=$start.PriorB25AIndexSha256;PhysicalIndexRepairPerformed=$false;IgnoreUnchanged=$true;CoreCount=$start.Core.Count;CoreUnchanged=$true;StashCount=$start.Stashes.Count;StashesUnchanged=$true};
        OwnedFiles=@($owned | Sort-Object -Unique | Where-Object { $_ -notlike '*-audit.json' } | ForEach-Object { Fingerprint (Join-Path $tool $_) });FinalGitStatus=@(& git --no-optional-locks status --short)}
    [IO.File]::WriteAllText($output,(($audit | ConvertTo-Json -Depth 60)+"`r`n"),[Text.UTF8Encoding]::new($false))
    if(-not $passed) { Write-Host 'FAIL: required validation failure retained, no waiver/retry/green readiness claim.'; exit 1 }
    Write-Host 'PASS: B25A2 boundary audit evidence and validation; production NOT repaired and B25B NOT ready.'
} catch {
    [ordered]@{Package='P5O2B25A2';Passed=$false;EvidenceConsistent=$false;Outcome='BoundaryAudit_StopRule';OwnershipFixed=$false;State='Evidence verification failed';Failure=$_.Exception.Message;GeneratedUtc=[DateTime]::UtcNow.ToString('o')} |
        ConvertTo-Json | ForEach-Object { [IO.File]::WriteAllText($output,($_+"`r`n"),[Text.UTF8Encoding]::new($false)) }
    throw
} finally { Pop-Location }
