param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $toolRoot
try {
    function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
    function Equal($Left,$Right,[string]$Message) { Require ((Json $Left) -ceq (Json $Right)) $Message }
    function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function FindingSet($Values) { @($Values | ForEach-Object { Json $_ } | Sort-Object -CaseSensitive) }
    function Tests([string]$Name,[string]$Path,[int]$Expected,[bool]$Fresh=$true) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Incomplete: $Name"
        Require ($counts.total -eq "$Expected" -and $counts.passed -eq "$Expected" -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Census/failure/skips: $Name"
        if($Fresh) { Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale: $Name" }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b22/protected-before.json'; $export=ReadJson 'artifacts/p5o2b22/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -ceq $export.ToolRoot -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Fresh HEAD archive required.'
    Require ((Get-FileHash artifacts/p5o2b22/head.tar).Hash -ceq $export.ArchiveSha256) 'Archive changed.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'External HEAD movement.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected root ignore changed.'
    Equal @(git stash list --format='%H %s') $initial.Stashes 'Stashes changed.'
    Equal @(git diff --cached --name-only) $initial.Index 'Index changed.'
    Require ($initial.TrackedFiles.Count -eq 932 -and $initial.CoreFiles.Count -eq 23 -and $initial.Stashes.Count -eq 6) 'Initial inventory changed.'
    [string[]]$built=ReadJson 'artifacts/p5o2b22/owned-build-inputs.json'
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})+@([IO.Path]::GetFullPath('Evaluation/OPEN-PIPELINE-BOUNDARIES.md'))
    foreach($file in $initial.TrackedFiles) {
        if($allowed -cnotcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/B5-B21/shared source changed: $($file.Path)" }
    }
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core inventory changed.'
    foreach($file in $initial.CoreFiles) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'Core bytes changed.' }
    Require ($built.Count -eq 11) 'Seven Main, two Worker, one test and one gate input required.'
    foreach($file in $built) { Require ((Get-FileHash $file).Hash -ceq (Get-FileHash (Join-Path $validation $file)).Hash) "Built overlay mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json'); $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Analyzer boundary changed.'
    Equal $gate.SharedSourceFingerprints $parent.SharedSourceFingerprints 'Shared Analyzer changed.'
    $images=foreach($image in $gate.BuildImages) {
        $before=@($parent.BuildImages | Where-Object {$_.Step -ceq $image.Step})
        Require ($before.Count -eq 1) 'Matching parent image required.'
        $dll=$image.Image.Sha256 -ceq $before[0].Image.Sha256; $pdb=$image.Image.PdbSha256 -ceq $before[0].Image.PdbSha256
        if($image.Owner -eq 'historical') { Require ($dll -and $pdb) 'Historical Analyzer DLL/PDB changed.' }
        else { Require (-not $dll -and -not $pdb) 'Main integration DLL/PDB should change.' }
        [ordered]@{Step=$image.Step;Owner=$image.Owner;Before=$before[0].Image;After=$image.Image;DllByteEqual=$dll;PdbByteEqual=$pdb}
    }
    for($index=0;$index -lt $gate.WorkerBuildImages.Count;$index++) {
        $before=$parent.WorkerBuildImages[$index]; $after=$gate.WorkerBuildImages[$index]
        Require ($before.Sha256 -cne $after.Sha256 -and $before.PdbSha256 -cne $after.PdbSha256) 'Worker execution-only host DLL/PDB should change.'
        $images+=@([ordered]@{Step=('worker-'+$index);Owner='worker';Before=$before;After=$after;DllByteEqual=$false;PdbByteEqual=$false})
    }
    $logs=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed step: $($step.Name)"
        if($step.Name.EndsWith('-build')) {
            $path=Join-Path $gateRoot ($step.Name+'.log'); $log=Get-Content $path -Raw -Encoding UTF8
            Require ($log -match '0 Warnung\(en\)' -and $log -match '0 Fehler') "Not clean WAE: $($step.Name)"
            Fingerprint $path
        }
    }
    Require (@($logs).Count -eq 11) 'Eleven WAE builds required.'
    $expected=[ordered]@{b22=46;b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=788;broad=3089;full=3180}
    $census=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b22/tests/'+$name+'.trx')) $expected[$name] }
    $census+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 578)
    $census+=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 532 $false)
    $baselines=foreach($file in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $file.FullName).Hash -ceq (Get-FileHash $after).Hash) "Current baseline changed: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight native Current baselines required.'
    $positives=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b22') -File -Filter 'positive-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.FullFindingSetParity -and $value.GuardBeforeSummary -and $value.B21Required -and $value.RepeatEqual -and $value.NoFallback -and $value.ProcessesStopped) 'Finding/guard/attestation boundary failed.'
        $declared=@($value.Production | Where-Object mode -eq 'projectTransitiveDeclaredExceptions')[0]
        Equal (FindingSet $declared.findings) (FindingSet $value.Historical.findings) 'Full Finding set differs.'
        Equal $value.Historical.findings $value.Repeated.findings 'Finding repeat differs.'
        Equal $value.Native.direct $value.Historical.direct.declaredPolicy.directResult 'Independent Direct differs.'
        Equal $value.Native.declarations $value.Historical.direct.declaredPolicy.declarations 'Full declarations/ownership differ.'
        Require ($value.Facts.B21OriginalExecutionReceipt -and $value.DirectFacts.B21OriginalExecutionReceipt -and $value.Historical.GuardReached) 'Missing immutable attested receipt.'
        $selected=$value.Historical.Direct
        if($value.Historical.SummaryRequested) {
            $selected=$value.Historical.Summary; Require ($declared.graphCount -gt 0) 'Selected graph absent.'
            Equal $value.Native.summary $selected.result 'Full selected Summary differs.'
        } else { Require ($declared.graphCount -eq 0) 'Skip guard populated graph.'; Equal $value.Native.direct $selected.result 'Guard-selected Direct differs.' }
        Equal $value.NativeDocument $selected.declaredDocumentation 'Full documentation/type/coverage evidence differs.'
        Equal $value.Facts.Documentation.typeDisplays $selected.declaredDocumentation.typeDisplays 'Immutable type displays differ.'
        Require ($selected.provenance.analysisMode -ceq 'project-transitive-declared-exceptions') 'Logical mode aliased.'
        if($value.Scenario -ne 'guard-direct') {
            $project=@($value.Production | Where-Object mode -eq 'projectTransitive')[0]
            Equal (FindingSet $project.findings) (FindingSet $value.HistoricalProjectPolicyFindings) 'Historical Project policy control differs.'
        } else {
            Require ($value.Historical.findings.Count -eq 1 -and $value.Historical.findings[0].smell.id -ceq 'DOC610' -and $null -eq $value.Historical.Summary) 'Guard did not preserve final Direct Finding.'
            Require ($value.ProjectControl.failure.workerFailure.code -ceq 'analysisFailure') 'Unguarded execution should fail.'
        }
        if($value.Scenario -in @('local','external')) { Require ((Json (FindingSet $value.HistoricalProjectPolicyFindings)) -cne (Json (FindingSet $value.Historical.Findings))) 'Project/Declared difference absent.' }
        if($value.Scenario -in @('local','combined','chain')) {
            Equal $value.Native.summaries $value.HistoricalAuxiliaryGraphProbe.summaries 'Actual beta auxiliary graph summaries differ.'
            Equal $value.HistoricalAuxiliaryGraphProbe.summaries $value.HistoricalAuxiliaryGraphProbe.repeatedSummaries 'Beta graph repeat differs.'
            Require ($value.HistoricalAuxiliaryGraphProbe.graphCount -eq $value.Native.graphCount -and $value.HistoricalAuxiliaryGraphProbe.repeatedResultEqual) 'Graph count/repeat differs.'
        }
        if($value.Scenario -eq 'chain') {
            Require ($value.Native.graphCount -eq 3) 'Root/A/B graph not proved.'
            $steps=$value.Historical.Summary.result.entries[0].paths[0].steps
            Require ($steps.Count -eq 3 -and $steps[0].symbolName -ceq 'Helper.A()' -and $steps[1].symbolName -ceq 'Helper.B()' -and
                $steps[2].symbolName -ceq 'LocalException' -and $steps[2].kind -ceq 'explicitThrow') 'Exact Root/A/B/LocalException flow missing.'
        }
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    }
    Require (@($positives).Count -eq 18) 'Eighteen complete positive root cases required.'
    $negative=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b22') -File -Filter 'negative-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.NoGuardAdmission -and $value.NoFindingPolicy -and $value.NoFallback -and $value.ProcessesStopped -and $null -ne $value.Rejection) 'Negative admitted before guard.'
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    }
    Require (@($negative).Count -eq 17) 'Seventeen contract/replay/substitution negatives required.'
    $scopes=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b22') -File -Filter 'scope-*.json') {
        $value=ReadJson $file.FullName
        Require (-not $value.GuardReached -and -not $value.SummaryRequested -and $value.NoDispatch -and @($value.Findings).Count -eq 0) 'Unsupported scope reached dispatch.'
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    }
    Require (@($scopes).Count -eq 7) 'Seven explicit unsupported purposes/scopes required.'
    $physical=ReadJson (Join-Path $validation 'artifacts/p5o2b22/physical-worker.json')
    Require ($physical.NoGuardAdmission -and $physical.NoFindingPolicy -and $physical.ExactGenuineProductiveResponse -and $physical.NoFallback -and $physical.ProcessesStopped -and $physical.Rejection.Code -ceq 'malformedResponse') 'Physical Worker substitution admitted.'
    $workerError=ReadJson (Join-Path $validation 'artifacts/p5o2b22/worker-error.json')
    Require ($workerError.NoPartialFindings -and $workerError.NoFallback -and $workerError.ProcessesStopped -and @($workerError.Historical.Findings).Count -eq 0) 'Partial Finding on selected Worker failure.'
    $purpose=ReadJson (Join-Path $validation 'artifacts/p5o2b22/purpose-rejection.json')
    Require ($purpose.NoGuardAdmission -and $purpose.NoFindingPolicy -and $purpose.NoFallback -and $purpose.ProcessesStopped) 'Legacy or wrong-purpose receipt admitted.'
    $attacks=foreach($folder in @('p5o2b20','p5o2b21')) {
        foreach($file in Get-ChildItem (Join-Path $validation ('artifacts/'+$folder)) -File -Filter '*.json') {
            $value=ReadJson $file.FullName
            if($value.NoFallback -and ($value.NoDocumentationReceipt -or $value.NoEvidenceReceiptWithoutExecution)) { [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value} }
        }
    }
    Require (@($attacks).Count -eq 21) 'Original three B20 and eighteen B21 attack artifacts required.'
    $owned=@($built)+@('Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B22-historical-declared-production-finding-integration.md',
        'Evaluation/P5O2B22Proof/Run-Regressions.ps1','Evaluation/P5O2B22Proof/Write-Verify-ProductionFindings.ps1')
    $encoding=foreach($file in $owned) {
        $bytes=[IO.File]::ReadAllBytes([IO.Path]::GetFullPath($file)); $content=[Text.UTF8Encoding]::new($false,$true).GetString($bytes)
        Require ($content.Length -gt 0 -and $content[0] -ne [char]0xFEFF -and $content.EndsWith("`r`n") -and $content -notmatch '(?<!\r)\n') "Not strict UTF8/noBOM/CRLF: $file"
        if($file.EndsWith('.ps1')) { $tokens=$null; $errors=$null; [Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath($file),[ref]$tokens,[ref]$errors) | Out-Null; Require ($errors.Count -eq 0) "Invalid PS syntax: $file" }
        Fingerprint $file
    }
    $paths=@(git -C D:/Repository/SEE ls-files); $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $collisions=@($paths | Where-Object { -not $seen.Add([string]$_) }); Require ($collisions.Count -eq 0) 'Git path case collision.'
    [string[]]$cs=@($built | Where-Object {$_ -like '*.cs'})
    & dotnet format whitespace . --folder --include $cs --verify-no-changes; Require ($LASTEXITCODE -eq 0) 'Owned format failed.'
    & git diff --check; Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $live=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('dotnet.exe','testhost.exe') -and $_.CommandLine -and $_.CommandLine.IndexOf($validation,[StringComparison]::OrdinalIgnoreCase) -ge 0 })
    Require ($live.Count -eq 0) 'Owned validation child still running.'
    $attempts=ReadJson 'artifacts/p5o2b22/attempts.json'
    [xml]$failed=Get-Content (Join-Path $validation 'artifacts/p5o2b22/tests/b22-first.trx') -Raw -Encoding UTF8
    Require ($failed.SelectSingleNode('//*[local-name()="Counters"]').failed -eq '25') 'First failed B22 attempt not retained.'
    $output='Evaluation/P5O2B22-historical-declared-production-finding-integration-audit.json'
    if(-not (Test-Path $output)) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),"{}`r`n",[Text.UTF8Encoding]::new($false)) }
    $audit=[ordered]@{
        Package='P5O2B22';VerifiedUtc=[DateTime]::UtcNow.ToString('o');EvidenceVerifierPassed=$true
        HistoricalDeclaredActivated=$true;ProductiveSummaryGuardClosed=$true;FinalFindingIntegrationImplemented=$true;FullBoundedFindingSetParityProven=$true
        Scope='Existing v1 exact complete single project/root, B11-B14 inputs and B19/B21; legacy flow-only entry remains unsupported; no general reporting/CLI adoption'
        MainPolicyShared=$true;GuardBeforeSelectedSummary=$true;B21BeforeGuard=$true;NoAlias=$true;NoCurrentFallback=$true;NoWorkerGuardOrFindingPolicy=$true
        WorkerChange='Execution-only DirectEvidence/SummaryEvidence purpose; optional envelope field, protocol still 2; legacy request bytes unchanged'
        AnalyzerSemanticsChanged=$false;RoslynVersionsChanged=$false;HistoricalAnalyzerDllPdbByteEqual=$true;WorkerDllPdbChanged=$true;MainDllPdbChanged=$true
        ProjectFindingCounterprobe='Same private productive Main renderer over genuine B21 Project evidence; not a new Project reporting API'
        GraphEvidence='Actual beta auxiliary B14 graph probe over the exact source/reference world; productive Worker still returns canonical flows, not graph DTOs'
        TrustBoundary='Unchanged B21 trusted client/configured image/private pipes/reviewed emitter; not remote cryptographic or compromised-runtime/OS protection'
        Head=$initial.Head;FinalHead=(git rev-parse HEAD);ExternalHeadMovement=$false;Branch=$initial.Branch
        Protection=[ordered]@{TrackedFiles=932;CoreFiles=23;Stashes=6;RootIgnoreSha256=$initial.RootIgnore;IndexUnchanged=$true;UnrelatedBytesUnchanged=$true;B5B21TestsUnchanged=$true}
        Export=$export;OwnedFiles=@($encoding);Tests=@($census);WaeBuilds=@($logs);DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'))
        CurrentBaselines=@($baselines);BinaryComparisons=@($images);SharedAnalyzerSources=121
        PositiveCases=@($positives);NegativeCases=@($negative);UnsupportedScopes=@($scopes);PhysicalWorker=$physical;WorkerError=$workerError;PurposeRejection=$purpose;OriginalAttacks=@($attacks)
        Isolation=[ordered]@{OwnLiveChildren=0;NoHistoricalLoadedInMain=$true;ActualWindowsExecution=$true;LinuxRunnerExecuted=$false;TrackedGitCaseCollisions=0;TrackedGitPathCount=$paths.Count}
        Attempts=$attempts
        SelfAnalysis=[ordered]@{Findings=16;Fresh=$false;Origin='Inherited B21, not rerun'}
        CanonicalFindingDiff=[ordered]@{Added=0;Removed=0;ChangedEvidence=0;Fresh=$false;Origin='Inherited B21, not rerun'}
        FinalGitStatus=@(git status --short);NoCommit=$true;NoPush=$true
    }
    $json=(ConvertTo-Json -InputObject $audit -Depth 100) -replace "`r?`n","`r`n"
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),($json+"`r`n"),[Text.UTF8Encoding]::new($false))
    $parsed=ReadJson $output
    Require ($parsed.EvidenceVerifierPassed -and $parsed.HistoricalDeclaredActivated -and $parsed.FullBoundedFindingSetParityProven) 'Closure JSON inconsistent.'
    Write-Host 'PASS: B22 bounded genuine attested production Findings; shared Main guard/policy, exact scope, Current and all prior regressions, isolation and protection.'
} finally { Pop-Location }
