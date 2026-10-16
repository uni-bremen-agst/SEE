$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$output=Join-Path $tool 'Evaluation/P5O2B25A-assembly-qualified-type-ownership-audit.json'
$gateFailures=[Collections.Generic.List[object]]::new()
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function Tests([string]$Name,[string]$Path,[int]$Total,[int]$Passed,[int]$Failed,[DateTime]$After) {
    [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($counts.total -eq "$Total" -and $counts.executed -eq "$Total" -and $counts.notExecuted -eq '0' -and
        ([int]$counts.passed+[int]$counts.failed) -eq $Total) "Unexpected/incomplete/skipped $Name"
    if($Failed -eq 0 -and [int]$counts.failed -ne 0) {
        $gateFailures.Add([ordered]@{Gate=$Name;ExpectedFailed=0;ActualFailed=[int]$counts.failed;
            Failures=@($trx.SelectNodes('//*[local-name()="UnitTestResult" and @outcome="Failed"]') | ForEach-Object {
                [ordered]@{Test=$_.testName;Message=$_.Output.ErrorInfo.Message;Stack=$_.Output.ErrorInfo.StackTrace;Duration=$_.duration}
            })})
    } else {
        Require ($counts.passed -eq "$Passed" -and $counts.failed -eq "$Failed") "Retained attempt differs: $Name"
    }
    Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge $After) "Stale $Name"
    [ordered]@{Name=$Name;Total=$Total;Passed=[int]$counts.passed;Failed=[int]$counts.failed;ExpectedFailed=$Failed;Skipped=0;Artifact=(Fingerprint $Path)}
}
Push-Location $tool
try {
    [IO.File]::WriteAllText($output,('{"Package":"P5O2B25A","Passed":false,"State":"Verifier running or interrupted"}'+"`r`n"),[Text.UTF8Encoding]::new($false))
    $before=ReadJson 'artifacts/p5o2b25a/protected-before.json'
    $export=ReadJson 'artifacts/p5o2b25a/export.json'; $validation=$export.ToolRoot
    $baseline=Join-Path $tool 'artifacts/p5o2b25a/baseline'
    $parent=ReadJson (Join-Path $baseline 'dual-version-build/audit.json')
    $inputs=ReadJson 'artifacts/p5o2b25a/validation-inputs.json'
    $since=@($inputs.Images | ForEach-Object { ([DateTime]$_.LastWriteUtc).ToUniversalTime() } | Sort-Object)[-1]
    Require ((& git rev-parse HEAD) -ceq $before.Head -and $before.Head -ceq $export.Head) 'External HEAD movement.'
    Require ((Get-FileHash (Join-Path $repo '.git/index')).Hash -ceq $before.Index) 'Index changed.'
    Require ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -ceq $before.Ignore) 'Protected ignore changed.'
    Require ((Json @(& git reflog show refs/stash --format='%H %gs')) -ceq (Json $before.Stashes)) 'Stashes changed.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq $before.Core.Count) 'Core inventory changed.'
    foreach($file in $before.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Core changed: $($file.Path)" }
    & git diff --quiet HEAD -- src Tests Evaluation/P5O2B25-bounded-solution-finding-adoption.md Evaluation/P5O2B25-bounded-solution-finding-adoption-audit.json Evaluation/P5O2B25Proof
    Require ($LASTEXITCODE -eq 0) 'Production, existing tests or historical B25 evidence changed after withdrawal.'
    foreach($source in $inputs.Sources) {
        Require ((Get-FileHash (Join-Path $tool $source.Path)).Hash -ceq $source.Sha256 -and
            (Get-FileHash (Join-Path $validation $source.Path)).Hash -ceq $source.Sha256) "Compiled source changed: $($source.Path)"
    }
    Require (@($inputs.Sources).Count -eq 1 -and @($inputs.Sources)[0].Path -like '*ExceptionFlowAssemblyQualifiedOwnershipTests.cs') 'Only the new audit test may be a changed C# input.'
    foreach($image in $inputs.Images) { Require ((Get-FileHash (Join-Path $validation $image.Path)).Hash -ceq $image.Sha256) "Tested image changed: $($image.Path)" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    Require ($parent.Passed -and $gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted -and $gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Dual-version build incomplete.'
    Require ((Json $gate.SharedSourceFingerprints) -ceq (Json $parent.SharedSourceFingerprints)) 'Stop rule requires all 121 shared files unchanged.'
    foreach($source in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash (Join-Path $validation $source.Path)).Hash -ceq $source.Sha256) "Shared source differs: $($source.Path)"
        Require ([IO.File]::ReadAllText((Join-Path $tool $source.Path)).Replace("`r`n","`n") -ceq
            [IO.File]::ReadAllText((Join-Path $validation $source.Path)).Replace("`r`n","`n")) "Workspace shared content differs: $($source.Path)"
    }
    foreach($property in 'HistoricalPackages','CurrentPackages','WorkerPackages','HistoricalVersion') {
        Require ((Json $gate.$property) -ceq (Json $parent.$property)) "Dependency/version drift: $property"
    }
    $builds=foreach($file in Get-ChildItem $gateRoot -Filter '*-build.log') {
        Require ($file.LastWriteTimeUtc -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale WAE log: $($file.Name)"
        $log=Get-Content $file.FullName -Raw -Encoding UTF8
        Require ($log -match '0 Warnung\(en\)' -and $log -match '0 Fehler') "WAE not clean: $($file.Name)"
        Fingerprint $file.FullName
    }
    Require (@($builds).Count -eq 11) 'All eleven WAE steps required.'
    $actualIdentity=ReadJson (Join-Path $gateRoot 'worker-identity.json')
    Require ((Json $actualIdentity.identity.loadedRoslyn) -ceq (Json $parent.WorkerRuntimeChecks[0].Response.Identity.LoadedRoslyn)) 'Executed compiler images changed.'
    $expected=[ordered]@{b24=42;b23=36;b22=46;b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=818;focused=925;broad=3226;full=3317}
    $tests=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b24/tests/'+$name+'.trx')) $expected[$name] $expected[$name] 0 $since }
    $tests+=@(Tests 'ownership-audit-and-b25' (Join-Path $validation 'artifacts/p5o2b25a/tests/ownership-boundary-audit.trx') 41 41 0 $since)
    $tests+=@(Tests 'pid-g1' (Join-Path $validation 'artifacts/p5o2b25a/tests/pid-g1.trx') 18 18 0 $since)
    $tests+=@(Tests 'boundary' (Join-Path $validation 'artifacts/p5o2b25a/tests/boundary.trx') 596 596 0 $since)
    $tests+=@(Tests 'dual-boundary' (Join-Path $gateRoot 'main-boundary.trx') 596 596 0 $since)
    $tests+=@(Tests 'dual-b23' (Join-Path $gateRoot 'external-target.trx') 36 36 0 $since)
    $tests+=@(Tests 'dual-b24' (Join-Path $gateRoot 'bounded-findings.trx') 42 42 0 $since)
    $tests+=@(Tests 'dual-b25' (Join-Path $gateRoot 'solution-findings.trx') 27 27 0 $since)
    $tests+=@(Tests 'dual-b25a-audit' (Join-Path $gateRoot 'assembly-qualified-ownership.trx') 14 14 0 $since)
    $timingObservations=foreach($group in 'broad','full') {
        $path=Join-Path $validation "artifacts/p5o2b24/tests/$group.trx"
        [xml]$trx=Get-Content $path -Raw -Encoding UTF8
        $caseMatches=@($trx.SelectNodes('//*[local-name()="UnitTestResult"]') | Where-Object testName -like '*ExternalSourceLineEndingReconstructionTests.ConcurrentReconstruction_PerformsOneRequestAndOneAttempt')
        Require ($caseMatches.Count -eq 1) 'Concurrency case observation missing.'
        [ordered]@{Group=$group;Test=$caseMatches[0].testName;Outcome=$caseMatches[0].outcome;Duration=$caseMatches[0].duration;Artifact=(Fingerprint $path)}
    }
    $baselines=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b9/current') -Filter '*.json') {
        $old=Join-Path $baseline ('current/'+$file.Name)
        Require ((Get-FileHash $old).Hash -ceq (Get-FileHash $file.FullName).Hash) "STOP: Current baseline changed: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $old);After=(Fingerprint $file.FullName);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight unchanged Current baselines required.'
    $candidate=Join-Path $tool 'artifacts/p5o2b25a/candidate'
    $candidateInputs=ReadJson (Join-Path $candidate 'validation-inputs.json')
    $candidateSince=@($candidateInputs.Images | ForEach-Object { ([DateTime]$_.LastWriteUtc).ToUniversalTime() } | Sort-Object)[-1]
    $retained=@(Tests 'candidate-initial-not-accepted' (Join-Path $candidate 'artifacts/p5o2b25a/tests/ownership-first.trx') 52 49 3 $candidateSince)
    $retained+=@(Tests 'nested-catch-diagnostic-confirmed-failure' (Join-Path $candidate 'artifacts/p5o2b25a/tests/nested-catch-diagnostic.trx') 1 0 1 $candidateSince)
    $retained+=@(Tests 'fresh-probe-genuine-two-three-project-owner-parity' (Join-Path $candidate 'artifacts/p5o2b25a/tests/candidate-fresh-probe.trx') 2 2 0 $candidateSince)
    $manifest=ReadJson (Join-Path $candidate 'manifest.json')
    foreach($file in $manifest) { Require ((Get-FileHash (Join-Path $candidate $file.Path)).Hash -ceq $file.Sha256) "Candidate archive changed: $($file.Path)" }
    $diagnostics=ReadJson (Join-Path $candidate 'diagnostics.json')
    Require ($diagnostics.OldProbeHistoricalSha256 -cne $diagnostics.RebuiltProbeHistoricalSha256 -and
        $diagnostics.RebuiltProbeHistoricalSha256 -ceq $diagnostics.CandidateHistoricalSha256 -and $diagnostics.FreshProbeDifferentialsPassed -eq 2) 'Stale-probe root cause not established.'
    $oldWitnessPath=Join-Path $baseline 'b25-cases/ownership-stop-witness.json'
    $candidateWitnessPath=Join-Path $candidate 'artifacts/p5o2b25/audit-cases/ownership-stop-witness.json'
    $oldWitness=ReadJson $oldWitnessPath; $candidateWitness=ReadJson $candidateWitnessPath
    Require ($oldWitness.TypeOwnershipLossObserved -and $oldWitness.EvaluatedTypeOwner -ceq 'Original.Solution.P0') 'Actual old wrong-owner witness missing.'
    Require (-not $candidateWitness.TypeOwnershipLossObserved -and $candidateWitness.EvaluatedTypeOwner -ceq 'Original.Solution.P1' -and
        $candidateWitness.FinalFindingAdmission -eq $false) 'Withdrawn candidate correct-owner witness missing.'
    $cases=foreach($count in 2,3) {
        $path=Join-Path $candidate "artifacts/p5o2b25a/cases/collision-$count.json"
        $case=ReadJson $path
        Require ($case.ActualOwner -ceq ('Original.Solution.P'+($count-1)) -and $case.FindingAdmission -eq $false -and
            $case.ProcessExit.HasExited -and (Json $case.Current.result) -ceq (Json $case.Historical) -and
            (Json $case.Graph.result) -ceq (Json $case.Historical)) 'Candidate correct-owner full Current/Historical/graph parity missing.'
        Fingerprint $path
    }
    $defectDetails=[ordered]@{}
    $auditCases=foreach($name in 'root-cause','nested-rebind','generic-catch') {
        $path=Join-Path $validation "artifacts/p5o2b25a/audit-cases/$name.json"
        Require ((Get-Item $path).LastWriteTimeUtc -ge $since) 'Stale final defect witness.'
        $case=ReadJson $path
        $defectDetails[$name]=$case
        if($name -eq 'root-cause') { Require ($case.BeforeWrongOwner.assembly.name -ceq 'A' -and $case.ActualThrownType.assembly.name -ceq 'B' -and $case.CandidateCorrectOwner.assembly.name -ceq 'B' -and $case.CandidateAdopted -eq $false) 'Wrong root-cause proof.' }
        if($name -eq 'nested-rebind') { Require ($case.CompleteIdentitiesEqual -and $case.CanonicalRebindAvailable -eq $false -and $case.SameTypeCatchWorks -eq $false) 'Nested stop-rule witness missing.' }
        if($name -eq 'generic-catch') { Require ($case.IncorrectlySuppressed -and $case.ProductionRepaired -eq $false) 'Independent construction-erasure witness missing.' }
        Fingerprint $path
    }
    $witness=[ordered]@{Package='P5O2B25A';Outcome='BoundaryAudit_StopRule';CandidateAdopted=$false;OwnershipFixed=$false;
        Before=[ordered]@{ActualWitness=(Fingerprint $oldWitnessPath);WrongOwner=$oldWitness.EvaluatedTypeOwner;ActualLocalOwner=$oldWitness.LocalBTypeOwner;UnsafeLookup='GetFirstSymbolForDeclarationId'};
        CandidateOnly=[ordered]@{ActualWitness=(Fingerprint $candidateWitnessPath);Owner=$candidateWitness.EvaluatedTypeOwner;Type=$candidateWitness.CorrectedAssemblyQualifiedType;FindingAdmission=$false;
            EmbeddedGraphIsInitialStaleProbe=$true;GraphCorrectnessProof='Use the separately archived fresh-probe GenuineCandidateCases, not the initial witness embedded graph'};
        AuthorizedB25WitnessDelta='Only ownership-stop-witness.json was emitted differently by the two authorized candidate characterization methods; the architecture method emits no witness. Both methods and all B25 report/audit/proof files are unchanged in the final tree.';
        GenuineCandidateCases=$cases;OpenDefectWitnesses=$auditCases;OpenDefectDetails=$defectDetails;RetainedAttempts=$retained}
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'ownership-before-after.json'),(($witness | ConvertTo-Json -Depth 45)+"`r`n"),[Text.UTF8Encoding]::new($false))
    $lifetimeProof=ReadJson 'artifacts/p5o2b25a/runs/pid-g1-proof-cases.json'
    Require ($lifetimeProof.Count -eq 9 -and @($lifetimeProof | Where-Object { -not $_.Passed }).Count -eq 0) 'PID-G1 proof failed.'
    $runs=foreach($file in Get-ChildItem 'artifacts/p5o2b25a/runs' -Filter '*.json' | Where-Object Name -notlike '*cases.json') {
        $run=ReadJson $file.FullName
        Require ($null -ne $run.ExitCode) "Incomplete run: $($file.Name)"
        if($file.Name -eq 'new-tests-first.json') { Require ($run.ExitCode -eq 1) 'Retained candidate failure changed.' }
        elseif($run.ExitCode -ne 0) { $gateFailures.Add([ordered]@{Gate=$file.Name;ExpectedExitCode=0;ActualExitCode=$run.ExitCode}) }
        [ordered]@{Artifact=(Fingerprint $file.FullName);ExitCode=$run.ExitCode;FinalGate=($file.Name -like '*boundary-audit.json')}
    }
    $paths=@(& git -C $repo diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repo ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
    $owned=@($paths | Sort-Object -Unique)
    foreach($path in $owned) {
        $full=Join-Path $repo $path
        if($path.EndsWith('.json')) { $null=ReadJson $full }
        if($path.EndsWith('.ps1')) { $errors=$null; $tokens=$null; $null=[Management.Automation.Language.Parser]::ParseFile($full,[ref]$tokens,[ref]$errors); Require ($errors.Count -eq 0) "PS syntax: $path" }
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Format-Owned.ps1 -Verify
    Require ($LASTEXITCODE -eq 0) 'Format/UTF8/CRLF failed.'
    & git diff --check; Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($path in @(& git -C $repo ls-files)+@(& git -C $repo ls-files --others --exclude-standard)) { Require ($seen.Add($path)) "Git case collision: $path" }
    $binaryChanges=foreach($owner in 'current','historical') {
        $old=@($parent.BuildImages | Where-Object Owner -eq $owner)[-1].Image
        $new=@($gate.BuildImages | Where-Object Owner -eq $owner)[-1].Image
        Require ($old.Sha256 -ceq $new.Sha256) "Withdrawn candidate survived in $owner binary."
        [ordered]@{Owner=$owner;Before=$old.Sha256;After=$new.Sha256;ByteEqual=$true}
    }
    Require (@($parent.WorkerBuildImages)[-1].Sha256 -ceq @($gate.WorkerBuildImages)[-1].Sha256) 'Withdrawn candidate worker survived.'
    $binaryChanges+=@([ordered]@{Owner='worker-host';Before=@($parent.WorkerBuildImages)[-1].Sha256;After=@($gate.WorkerBuildImages)[-1].Sha256;ByteEqual=$true})
    $validationPassed=$gateFailures.Count -eq 0
    $audit=[ordered]@{Package='P5O2B25A';Passed=$validationPassed;ValidationPassed=$validationPassed;GateFailures=@($gateFailures.ToArray());Outcome='BoundaryAudit_StopRule';OwnershipFixed=$false;ReadyForB25B=$false;SolutionFindingAdmitted=$false;
        GeneratedUtc=[DateTime]::UtcNow.ToString('o');Head=$before.Head;SharedSourcesUnchanged=121;AllProductionAndExistingTestsUnchanged=$true;
        RootCause='OriginalDefinition + assembly-unqualified first declaration-ID match substitutes A for B';
        IndependentBoundary='Constructed containing-type canonical rebinding is unavailable; source/PE catch identity fails. Existing catch matching also erases different generic constructions.';
        FollowUp='P5O2B25A2 - Exact constructed/nested type rebinding and catch-identity contract, then complete common normalization before B25B';
        Tests=$tests;AcquisitionConcurrencyObservations=$timingObservations;AcquisitionTimeoutCauseConfirmed=$false;NoAcquisitionOrPidRepair=$true;WaeBuilds=$builds;Dual=(Fingerprint (Join-Path $gateRoot 'audit.json'));Runs=$runs;CurrentBaselines=$baselines;
        BeforeAfterWitness=(Fingerprint (Join-Path $PSScriptRoot 'ownership-before-after.json'));RetainedCandidateAttempts=$retained;
        CandidateDiagnostics=(Fingerprint (Join-Path $candidate 'diagnostics.json'));CandidateManifest=(Fingerprint (Join-Path $candidate 'manifest.json'));CandidateCases=$cases;OpenDefectWitnesses=$auditCases;
        BaselineDual=(Fingerprint (Join-Path $baseline 'dual-version-build/audit.json'));ProtectionSnapshot=(Fingerprint 'artifacts/p5o2b25a/protected-before.json');PidG1Proof=(Fingerprint 'artifacts/p5o2b25a/runs/pid-g1-proof-cases.json');
        CompiledInputs=(Fingerprint 'artifacts/p5o2b25a/validation-inputs.json');BinaryImages=$binaryChanges;
        HistoricalB25TestsReportAuditAndProofUnchanged=$true;PidG1Unchanged=$true;RoutingAndVersionsUnchanged=$true;
        FormatJsonUtf8Crlf=$true;GitCaseAudit=$true;ActualLinuxRun=$false;Network=$false;EvidenceConsistent=$true;
        SelfAnalysis=[ordered]@{Fresh=$false;InheritedCount=16};CanonicalFindingDiff=[ordered]@{Fresh=$false;InheritedAdded=0;InheritedRemoved=0;InheritedChanged=0};
        Protection=[ordered]@{HeadUnchanged=$true;IndexUnchanged=$true;IgnoreUnchanged=$true;CoreCount=$before.Core.Count;CoreUnchanged=$true;StashCount=$before.Stashes.Count;StashesUnchanged=$true};
        OwnedFiles=@($owned | Where-Object { $_ -notlike '*-audit.json' } | ForEach-Object { Fingerprint (Join-Path $repo $_) });FinalGitStatus=@(& git status --short)}
    [IO.File]::WriteAllText($output,(($audit | ConvertTo-Json -Depth 50)+"`r`n"),[Text.UTF8Encoding]::new($false))
    if(-not $validationPassed) {
        Write-Host 'FAIL: required validation gate(s) failed. Full evidence retained; no waiver, no green ownership/readiness claim.'
        exit 1
    }
    Write-Host 'PASS: B25A stop-rule audit validated; ownership NOT fixed, failed candidate preserved, all production and B25 tests unchanged.'
} catch {
    [ordered]@{Package='P5O2B25A';Passed=$false;Outcome='BoundaryAudit_StopRule';OwnershipFixed=$false;State='Evidence verification failed';Failure=$_.Exception.Message;GeneratedUtc=[DateTime]::UtcNow.ToString('o')} |
        ConvertTo-Json | ForEach-Object { [IO.File]::WriteAllText($output,($_+"`r`n"),[Text.UTF8Encoding]::new($false)) }
    throw
} finally { Pop-Location }
