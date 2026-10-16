param([string]$NewAttempt='boundary-clean')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$output=Join-Path $tool 'Evaluation/P5O2B25-bounded-solution-finding-adoption-audit.json'
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function Tests([string]$Name,[string]$Path,[int]$Expected,[DateTime]$After) {
    [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($counts.total -eq "$Expected" -and $counts.executed -eq "$Expected" -and $counts.passed -eq "$Expected" -and
        $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Incomplete/failed/skipped $Name"
    Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge $After) "Stale $Name"
    [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
}
Push-Location $tool
try {
    [IO.File]::WriteAllText($output,('{"Package":"P5O2B25","Passed":false,"State":"Verifier running or interrupted"}'+"`r`n"),[Text.UTF8Encoding]::new($false))
    $before=ReadJson 'artifacts/p5o2b25/protected-before.json'
    $export=ReadJson 'artifacts/p5o2b25/export.json'; $validation=$export.ToolRoot
    $baseline=Join-Path $tool 'artifacts/p5o2b25/baseline'
    $parent=ReadJson (Join-Path $baseline 'dual-version-build/audit.json')
    $inputs=ReadJson 'artifacts/p5o2b25/validation-inputs.json'
    $since=@($inputs.Images | ForEach-Object { ([DateTime]$_.LastWriteUtc).ToUniversalTime() } | Sort-Object)[-1]
    Require ((& git rev-parse HEAD) -ceq $before.Head -and $before.Head -ceq $export.Head) 'External HEAD movement.'
    Require ((Get-FileHash (Join-Path $repo '.git/index')).Hash -ceq $before.Index) 'Index changed.'
    Require ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -ceq $before.Ignore) 'Protected ignore changed.'
    Require ((Json @(& git reflog show refs/stash --format='%H %gs')) -ceq (Json $before.Stashes)) 'Stashes changed.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq $before.Core.Count) 'Core inventory changed.'
    foreach($file in $before.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Core changed: $($file.Path)" }
    foreach($source in $inputs.Sources) {
        Require ((Get-FileHash (Join-Path $tool $source.Path)).Hash -ceq $source.Sha256 -and
            (Get-FileHash (Join-Path $validation $source.Path)).Hash -ceq $source.Sha256) "Compiled source changed: $($source.Path)"
    }
    foreach($image in $inputs.Images) { Require ((Get-FileHash (Join-Path $validation $image.Path)).Hash -ceq $image.Sha256) "Tested image changed: $($image.Path)" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    Require ($gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted -and $gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Dual-version build incomplete.'
    Require ((Json $gate.SharedSourceFingerprints) -ceq (Json $parent.SharedSourceFingerprints)) 'Shared Analyzer semantics changed.'
    foreach($source in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash (Join-Path $validation $source.Path)).Hash -ceq $source.Sha256) "Shared Analyzer changed: $($source.Path)"
        $localText=[IO.File]::ReadAllText((Join-Path $tool $source.Path)).Replace("`r`n","`n")
        $builtText=[IO.File]::ReadAllText((Join-Path $validation $source.Path)).Replace("`r`n","`n")
        Require ($localText -ceq $builtText) "Local shared Analyzer content differs: $($source.Path)"
    }
    $unchanged=@('src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs',
        'src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.Findings.cs',
        'src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs',
        'src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerCallResult.cs',
        'src/XMLDocNormalizer/Execution/Historical/OwnedProcessExit.cs',
        'src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs',
        'build/ExceptionFlow.HistoricalSources.props')
    foreach($path in $unchanged) { & git diff --quiet HEAD -- $path; Require ($LASTEXITCODE -eq 0) "Protected architecture changed: $path" }
    & git diff --quiet HEAD -- src
    Require ($LASTEXITCODE -eq 0) 'Stop rule requires every production source to remain unchanged.'
    $changed=@(& git diff --name-only HEAD -- '*.csproj' '*.props' '*/NuGet.Config' '*ExceptionFlowAnalysisRouter.cs' '*ExceptionFlowAnalysisDispatch.cs' '*ExceptionFlowAnalyzerSelection*.cs' 'Tests/*')
    Require (@($changed | Where-Object { $_ -notlike '*ExceptionFlowHistoricalSolutionFindingTests.cs' }).Count -eq 0) 'Old tests/compiler/routing configuration changed.'
    $builds=foreach($file in Get-ChildItem $gateRoot -Filter '*-build.log') {
        Require ($file.LastWriteTimeUtc -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale WAE log: $($file.Name)"
        $log=Get-Content $file.FullName -Raw -Encoding UTF8
        Require ($log -match '0 Warnung\(en\)' -and $log -match '0 Fehler') "WAE not clean: $($file.Name)"
        Fingerprint $file.FullName
    }
    Require (@($builds).Count -eq 11) 'All eleven WAE steps required.'
    $actualIdentity=ReadJson (Join-Path $gateRoot 'worker-identity.json')
    Require ((Json $actualIdentity.identity.loadedRoslyn) -ceq (Json $parent.WorkerRuntimeChecks[0].Response.Identity.LoadedRoslyn)) 'Executed compiler images changed.'
    $oldAnalyzer=@($parent.BuildImages | Where-Object Owner -eq 'historical')[-1].Image
    $newAnalyzer=@($gate.BuildImages | Where-Object Owner -eq 'historical')[-1].Image
    Require ($oldAnalyzer.Sha256 -ceq $newAnalyzer.Sha256) 'Historical Analyzer binary changed.'
    $expected=[ordered]@{b24=42;b23=36;b22=46;b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=804;focused=911;broad=3212;full=3303}
    $tests=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b24/tests/'+$name+'.trx')) $expected[$name] $since }
    $tests+=@(Tests 'b25' (Join-Path $validation ('artifacts/p5o2b25/tests/b25-'+$NewAttempt+'.trx')) 27 $since)
    $tests+=@(Tests 'pid-g1' (Join-Path $validation 'artifacts/p5o2b25/tests/pid-g1.trx') 18 $since)
    $tests+=@(Tests 'boundary' (Join-Path $validation 'artifacts/p5o2b25/tests/boundary.trx') 596 $since)
    $tests+=@(Tests 'dual-boundary' (Join-Path $gateRoot 'main-boundary.trx') 596 $since)
    $tests+=@(Tests 'dual-b23' (Join-Path $gateRoot 'external-target.trx') 36 $since)
    $tests+=@(Tests 'dual-b24' (Join-Path $gateRoot 'bounded-findings.trx') 42 $since)
    $tests+=@(Tests 'dual-b25' (Join-Path $gateRoot 'solution-findings.trx') 27 $since)
    $baselines=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b9/current') -Filter '*.json') {
        $old=Join-Path $baseline ('current/'+$file.Name)
        Require ((Get-FileHash $old).Hash -ceq (Get-FileHash $file.FullName).Hash) "Current Finding baseline changed: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $old);After=(Fingerprint $file.FullName);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight byteequal Current baselines required.'
    $cases=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b25/audit-cases') -Filter '*.json') {
        Require ($file.LastWriteTimeUtc -ge $since) "Stale B25 case: $($file.Name)"
        $case=ReadJson $file.FullName
        if($file.Name -like 'baseline-*') {
            Require ($case.FullFlowGraphParity -and $case.RepeatEqual -and $case.FinalFindingAdmission -eq $false) "Incomplete baseline $($file.Name)"
            Require ($case.Flow.uncertainties.Count -eq 0 -and @($case.Flow.entries | Where-Object pathsTruncated).Count -eq 0) 'Incomplete B15 flow.'
        }
        if($file.Name -like 'cli-*') { Require ($case.ExitCode -eq 4 -and $case.NoReport -and $case.NoEmptySuccess -and $case.ProcessExit.HasExited) 'Unsupported CLI failed open.' }
        if($file.Name -like 'graph-negative-*') { Require ($case.NoProcess -and $case.Failure) 'Graph dispatched.' }
        Fingerprint $file.FullName
    }
    Require (@($cases | Where-Object Path -like '*baseline-*').Count -eq 15) 'Fifteen complete repeated B15/Current baseline observations required.'
    Require (@($cases | Where-Object Path -like '*graph-negative-*').Count -eq 7) 'Seven graph attacks required.'
    Require (@($cases | Where-Object Path -like '*cli-*').Count -eq 3) 'Three genuine unsupported CLI executions required.'
    $witness=ReadJson (Join-Path $validation 'artifacts/p5o2b25/audit-cases/ownership-stop-witness.json')
    Require ($witness.TypeOwnershipLossObserved -and $witness.CurrentHistoricalParityDoesNotProveOwnership -and
        $witness.LocalATypeOwner -ceq 'Original.Solution.P0' -and $witness.LocalBTypeOwner -ceq 'Original.Solution.P1' -and
        $witness.EvaluatedTypeOwner -ceq $witness.LocalATypeOwner -and $witness.FinalFindingAdmission -eq $false) 'Missing actual ownership stop witness.'
    $rawCliPath=Join-Path $tool 'artifacts/p5o2b25/raw-graph-cli/audit.json'
    $rawCli=ReadJson $rawCliPath
    Require ($rawCli.Passed -and $rawCli.Runs.Count -eq 2 -and $rawCli.MainSha256 -ceq @($inputs.Images | Where-Object Path -like 'src/XMLDocNormalizer/bin/*')[0].Sha256) 'Complete raw graph CLI proof missing.'
    foreach($run in $rawCli.Runs) { Require ($run.ExitCode -eq 4 -and $run.NoReport -and $run.SchemaNotAdmitted -and (Get-FileHash $run.Job).Hash -ceq $run.JobSha256) 'Raw graph CLI admission changed.' }
    $runs=foreach($file in Get-ChildItem 'artifacts/p5o2b25/runs' -Filter '*.json' | Where-Object Name -notlike '*cases.json') {
        $run=ReadJson $file.FullName
        Require ($null -ne $run.ExitCode) "Unfinished execution: $($file.Name)"
        if($file.Name -in @('tests-wae-first.json','new-tests-expanded-dedup.json')) { Require ($run.ExitCode -eq 1) 'Expected historical failure changed.' }
        else { Require ($run.ExitCode -eq 0) "Failed execution: $($file.Name)" }
        [ordered]@{Artifact=(Fingerprint $file.FullName);ExitCode=$run.ExitCode}
    }
    $attemptHistory=foreach($past in @(@{File='b25-first.trx';Total=40;Passed=40;Failed=0},
        @{File='b25-expanded-dedup.trx';Total=42;Passed=41;Failed=1},@{File='same-name-inspection.trx';Total=1;Passed=0;Failed=1})) {
        $path=Join-Path $validation ('artifacts/p5o2b25/tests/'+$past.File)
        [xml]$trx=Get-Content $path -Raw -Encoding UTF8; $count=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($count.total -eq "$($past.Total)" -and $count.passed -eq "$($past.Passed)" -and $count.failed -eq "$($past.Failed)" -and $count.notExecuted -eq '0') 'Retained prototype attempt differs.'
        $failures=@($trx.SelectNodes('//*[local-name()="UnitTestResult" and @outcome="Failed"]'))
        foreach($failure in $failures) { Require ($failure.testName -like '*same-name-types*' -and $failure.OuterXml -match 'Expected: 2') 'Unexpected prototype failure.' }
        [ordered]@{Artifact=(Fingerprint $path);Total=$past.Total;Passed=$past.Passed;Failed=$past.Failed;FinalGate=$false}
    }
    $stale=ReadJson 'artifacts/p5o2b25/validation-inputs-stale-incremental.json'
    $staleMain=@($stale.Images | Where-Object Path -like 'src/XMLDocNormalizer/bin/*')[0]
    $finalMain=@($inputs.Images | Where-Object Path -like 'src/XMLDocNormalizer/bin/*')[0]
    Require ($staleMain.Sha256 -cne $finalMain.Sha256) 'Stale incremental attempt was not distinguished from the final clean images.'
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
    $binaryChanges=foreach($image in $inputs.Images) {
        $oldSha=$null
        if($image.Path -like 'src/XMLDocNormalizer/bin/*') { $oldSha=@($parent.BuildImages | Where-Object Owner -eq 'current')[-1].Image.Sha256 }
        if($image.Path -like '*XMLDocNormalizer.HistoricalWorker.dll') { $oldSha=@($parent.WorkerBuildImages)[-1].Sha256 }
        if($oldSha) { Require ($oldSha -ceq $image.Sha256) 'Withdrawn prototype binary survived the final clean build.' }
        [ordered]@{Path=$image.Path;BeforeSha256=$oldSha;FinalSha256=$image.Sha256;Changed=$(if($oldSha){$oldSha -cne $image.Sha256}else{$null})}
    }
    $audit=[ordered]@{
        Package='P5O2B25';Passed=$true;Outcome='BoundaryAudit_StopRule';SolutionFindingAdmitted=$false;GeneratedUtc=[DateTime]::UtcNow.ToString('o');Head=$before.Head
        Supported='Unchanged B24 Direct/Project/Declared. B15 flow/graph scope preserved. No final Solution Finding/CLI activation.'
        RootCause='Shared NormalizeSummaryExceptionType uses assembly-unqualified GetFirstSymbolForDeclarationId; distinct B type becomes root A type.'
        FollowUp='P5O2B25A - Assembly-preserving Exception Normalization and Cross-Project Type Ownership Contract (separate analyzer-semantics decision).'
        Attestation='Existing original B21 whole-request/response/physical-image capability; no new authority DTO'
        Tests=$tests;WaeBuilds=$builds;Dual=(Fingerprint (Join-Path $gateRoot 'audit.json'));Cases=$cases;Runs=$runs;RetainedAttempts=$attemptHistory;CompleteRawGraphCli=(Fingerprint $rawCliPath)
        CurrentBaselines=$baselines;CompiledInputs=(Fingerprint 'artifacts/p5o2b25/validation-inputs.json');BinaryImages=$binaryChanges
        HistoricalAnalyzer=[ordered]@{Before=$oldAnalyzer.Sha256;After=$newAnalyzer.Sha256;ByteEqual=$true}
        SharedAnalyzerFiles=121;AnalyzerSemanticsUnchanged=$true;RoutingVersionsAndOldTestsUnchanged=$true;PidG1Unchanged=$true
        EvidenceConsistent=$true;FormatJsonUtf8Crlf=$true;GitCaseAudit=$true;ActualLinuxRun=$false;Network=$false
        SelfAnalysis=[ordered]@{Fresh=$false;InheritedCount=16};CanonicalFindingDiff=[ordered]@{Fresh=$false;InheritedAdded=0;InheritedRemoved=0;InheritedChanged=0}
        Attempts=[ordered]@{Prototype40Passed=$true;Expanded42FailedOnOwnershipAssumption=$true;IsolatedInspectionFailedAsExpected=$true;
            StaleIncrementalAuditNotAccepted=$true;FinalGateRequiresCleanHeadProductionImages=$true;RetryUntilGreen=$false}
        Protection=[ordered]@{HeadUnchanged=$true;IndexUnchanged=$true;IgnoreUnchanged=$true;CoreCount=$before.Core.Count;CoreUnchanged=$true;StashCount=$before.Stashes.Count;StashesUnchanged=$true}
        OwnedFiles=@($owned | Where-Object { $_ -notlike '*-audit.json' } | ForEach-Object { Fingerprint (Join-Path $repo $_) })
        FinalGitStatus=@(& git status --short)
    }
    [IO.File]::WriteAllText($output,(($audit | ConvertTo-Json -Depth 30)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Write-Host 'PASS: B25 stop-rule boundary audit, ownership regression, unchanged admission, full validation and repository protection.'
} catch {
    [ordered]@{Package='P5O2B25';Passed=$false;Outcome='BoundaryAudit_StopRule';SolutionFindingAdmitted=$false;
        State='Evidence verification failed';Failure=$_.Exception.Message;GeneratedUtc=[DateTime]::UtcNow.ToString('o')} |
        ConvertTo-Json | ForEach-Object { [IO.File]::WriteAllText($output,($_+"`r`n"),[Text.UTF8Encoding]::new($false)) }
    throw
} finally { Pop-Location }
