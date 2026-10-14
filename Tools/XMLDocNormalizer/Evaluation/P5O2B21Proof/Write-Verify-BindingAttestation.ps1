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
    function Tests([string]$Name,[string]$Path,[int]$Expected,[bool]$Fresh=$true) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Incomplete/failed: $Name"
        Require ($counts.total -eq "$Expected" -and $counts.passed -eq "$Expected" -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Census/failure/skips: $Name"
        if($Fresh) { Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale: $Name" }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b21/protected-before.json'; $export=ReadJson 'artifacts/p5o2b21/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -ceq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Clean recorded HEAD archive required.'
    Require ((Get-FileHash artifacts/p5o2b21/head.tar).Hash -ceq $export.ArchiveSha256) 'Archive changed.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'External HEAD movement: audit before continuing.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected ignore changed.'
    Equal @(git stash list --format='%H %s') $initial.Stashes 'Stashes changed.'
    Equal @(git diff --cached --name-only) $initial.Index 'Index changed.'
    Require ($initial.TrackedFiles.Count -eq 927 -and $initial.CoreFiles.Count -eq 23 -and $initial.Stashes.Count -eq 6) 'Unexpected protected inventory.'
    [string[]]$built=ReadJson 'artifacts/p5o2b21/owned-build-inputs.json'
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})+@([IO.Path]::GetFullPath('Evaluation/OPEN-PIPELINE-BOUNDARIES.md'))
    foreach($file in $initial.TrackedFiles) { if($allowed -cnotcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/production/B5-B19 source changed: $($file.Path)" } }
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core inventory changed.'
    foreach($file in $initial.CoreFiles) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'Core byte change.' }
    Require ($built.Count -eq 7) 'Three Main sources, B20/B21 tests, pipe peer and gate are build inputs.'
    foreach($file in $built) { Require ((Get-FileHash $file).Hash -ceq (Get-FileHash (Join-Path $validation $file)).Hash) "Final built overlay mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'; $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Analyzer build boundary changed.'
    Equal $gate.SharedSourceFingerprints $parent.SharedSourceFingerprints 'Shared analyzer sources changed.'
    $images=foreach($image in $gate.BuildImages) {
        $before=@($parent.BuildImages | Where-Object {$_.Step -ceq $image.Step})
        Require ($before.Count -eq 1) 'Matching parent image required.'
        $equal=$image.Image.Sha256 -ceq $before[0].Image.Sha256 -and $image.Image.PdbSha256 -ceq $before[0].Image.PdbSha256
        if($image.Owner -eq 'historical') { Require $equal 'Historical analyzer DLL/PDB changed.' }
        else { Require (-not $equal -and $image.Image.Sha256 -cne $before[0].Image.Sha256 -and $image.Image.PdbSha256 -cne $before[0].Image.PdbSha256) 'Main attestation-only binaries should change.' }
        [ordered]@{Step=$image.Step;Owner=$image.Owner;Before=$before[0].Image;After=$image.Image;DllPdbByteEqual=$equal}
    }
    for($index=0;$index -lt $gate.WorkerBuildImages.Count;$index++) {
        $before=$parent.WorkerBuildImages[$index]; $after=$gate.WorkerBuildImages[$index]
        Require ($before.Sha256 -ceq $after.Sha256 -and $before.PdbSha256 -ceq $after.PdbSha256) 'Worker DLL/PDB changed.'
        $images+=@([ordered]@{Step=('worker-'+$index);Owner='worker';Before=$before;After=$after;DllPdbByteEqual=$true})
    }
    $logs=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed gate step: $($step.Name)"
        if($step.Name.EndsWith('-build')) {
            $path=Join-Path $gateRoot ($step.Name+'.log'); $value=Get-Content $path -Raw -Encoding UTF8
            Require ($value -match '0 Warnung\(en\)' -and $value -match '0 Fehler') "Not clean WAE: $($step.Name)"
            Fingerprint $path
        }
    }
    Require (@($logs).Count -eq 11) 'Eleven WAE builds required.'
    $expected=[ordered]@{b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=742;broad=3043;full=3134}
    $census=foreach($name in $expected.Keys) {
        $artifact=$name+'.trx'
        Tests $name (Join-Path $validation ('artifacts/p5o2b21/tests/'+$artifact)) $expected[$name]
    }
    $census+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 532)
    $census+=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 506 $false)
    $baselines=foreach($file in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $file.FullName).Hash -ceq (Get-FileHash $after).Hash) "Current baseline changed: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight complete native Current baselines required.'
    $cases=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b20') -File -Filter 'admission-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.OriginalPayloadRetained -and $value.SourceAndSpansUnchanged -and $value.DirectSummaryHashesUnchanged -and
            -not $value.CoherentSubstitutionAccepted -and $value.NoEvidenceReceiptWithoutExecution -and $value.BindingAssociationAttestationRequired -and
            $value.ProductionStillUnsupported -and $value.NoFallback -and $value.ProcessesStopped -and $value.NoHistoricalLoadedInMain) 'Original B20 tampering not rejected.'
        Require ($value.Rejection.Code -ceq 'malformedResponse' -and $value.Rejection.Message.Contains('execution attestation')) 'B21 rejection boundary wrong.'
        Equal $value.Rejection $value.RepeatedRejection 'Repeated coherent tampering differs.'
        Equal $value.Native.Summary $value.Summary 'Genuine Summary differs.'
        Equal $value.Native.Direct $value.Direct 'Genuine Direct differs.'
        Equal $value.Native.Declarations $value.Declarations 'Genuine declarations/ownership differ.'
        Equal $value.GenuineDocument.Syntax $value.ModifiedDocument.Syntax 'B20 source/cref substitution changed.'
        Equal $value.GenuineDocument.Method $value.ModifiedDocument.Method 'B20 root changed.'
        Equal $value.GenuineDocument.DirectSha256 $value.ModifiedDocument.DirectSha256 'B20 Direct hash changed.'
        Equal $value.GenuineDocument.SummarySha256 $value.ModifiedDocument.SummarySha256 'B20 Summary hash changed.'
        Require ((Json $value.GenuineDocument.Bindings) -cne (Json $value.ModifiedDocument.Bindings)) 'Original coherent attack absent.'
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    }
    Require (@($cases).Count -eq 3) 'All three unchanged B20 transformations required.'
    $positives=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b21') -File -Filter 'positive-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.GenuineExecutionAttested -and $value.CanonicalRepeatEqual -and $value.FreshCapabilities -and $value.ConsumedReplayRejected -and
            $value.ImmutableReceipt -and $value.NoWireChange -and $value.NoFallback -and $value.DeclaredStillUnsupported -and
            $value.ProcessesStopped -and $value.NoHistoricalLoadedInMain) 'Genuine execution/one-use/immutability/wire boundary wrong.'
        Equal $value.Document $value.NativeDocument 'Full Current/Historical binding facts differ.'
        Equal $value.Native.Summary $value.Summary 'Full genuine flow/source/uncertainty differs.'
        Equal $value.Native.Direct $value.Direct 'Independent Direct differs.'
        Equal $value.Native.Declarations $value.Declarations 'Ownership inventory differs.'
        Require ($value.FirstProcess -ne $value.SecondProcess -and $value.Document.Profile -ceq 'single-root-declared-documentation-v1' -and
            $value.Provenance.AnalysisMode -ceq 'project-transitive') 'Fresh actual Worker or truthful mode missing.'
        Equal $value.Contract.TypeDisplays $value.Document.TypeDisplays 'Immutable type facts differ.'
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    }
    Require (@($positives).Count -eq 8) 'Eight genuine bound/unresolved/other/alias/inherit/doc shapes required.'
    $attestationNegatives=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b21') -File -Filter 'negative-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.NoDocumentationReceipt -and $value.NoResult -and $value.ActualWorker -and $value.NoFallback -and
            $value.DeclaredStillUnsupported -and $value.ProcessesStopped -and $value.NoHistoricalLoadedInMain) 'Attestation/replay rejection failed.'
        Require ($null -ne $value.Rejection) 'Negative needs actual rejection.'
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    }
    Require (@($attestationNegatives).Count -eq 17) 'Seventeen missing/default/tampering/target/source/replay tests required.'
    $physical=ReadJson (Join-Path $validation 'artifacts/p5o2b21/physical-worker.json')
    Require ($physical.ExactGenuineResponseCopied -and $physical.ActualPipePeerRan -and $physical.ClaimedWorkerFilename -and
        $physical.PhysicalImageAssociationRequired -and $physical.NoDocumentationReceipt -and $physical.NoFallback -and
        $physical.ProcessesStopped -and $physical.NoHistoricalLoadedInMain -and $physical.Rejection.Code -ceq 'malformedResponse' -and
        $physical.Rejection.Message.Contains('execution attestation')) 'Real forged peer identity/response admitted.'
    $production=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b20') -File -Filter 'production-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.GuardObservedThroughProduction -and $value.EvidenceOnly -and -not $value.HistoricalFinalFindingsAvailable -and
            $value.ProductionStillUnsupported -and $value.NoFallback -and $value.ProcessesStopped -and $value.NoHistoricalLoadedInMain) 'Partial activation/parity misclaim.'
        Require ($value.ActualProduction.Count -eq 4) 'All four native mode controls required.'
        $declared=@($value.ActualProduction | Where-Object Mode -eq 'projectTransitiveDeclaredExceptions')
        Require ($declared.Count -eq 1) 'Declared production snapshot missing.'
        Equal $value.Native.DeclaredFindings $declared[0].Findings 'Actual full productive Declared Findings differ.'
        $direct=@($value.ActualProduction | Where-Object Mode -eq 'direct')
        Require ($direct.Count -eq 1 -and $direct[0].GraphCount -eq 0) 'Direct populated a summary graph.'
        if($value.Scenario -in @('external','guard','no-doc')) { Require ($declared[0].GraphCount -eq 0) 'Actual productive skip guard not executed.' }
        else { Require ($declared[0].GraphCount -gt 0) 'Actual productive summary not executed.' }
        if($value.Scenario -eq 'guard') {
            Require ($value.Historical.Failure.WorkerFailure.Code -eq 'analysisFailure' -and $null -eq $value.Historical.Result) 'Existing incomplete Summary boundary changed.'
            Equal $value.Historical.Failure $value.RepeatedHistorical.Failure 'Worker failure repeat differs.'
        } else {
            Equal $value.Native.Summary $value.Historical.Result 'Genuine complete flows differ.'
            Equal $value.Native.Direct $value.Historical.DeclaredPolicy.DirectResult 'Genuine independent Direct differs.'
            Equal $value.Native.Declarations $value.Historical.DeclaredPolicy.Declarations 'Genuine ownership differs.'
            Equal $value.Historical.Result $value.RepeatedHistorical.Result 'Canonical repeat differs.'
            Equal $value.Historical.DeclaredDocumentation $value.RepeatedHistorical.DeclaredDocumentation 'Binding repeat differs.'
        }
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    }
    Require (@($production).Count -eq 7) 'Seven real production guard/control snapshots required.'
    $negative=foreach($folder in @('p5o2b17','p5o2b19')) { foreach($file in Get-ChildItem (Join-Path $validation ('artifacts/'+$folder)) -File -Filter 'negative-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.NoFallback -and $value.ProcessesStopped) 'Legacy negative fallback/cleanup drift.'
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    } }
    Require (@($negative).Count -eq 38) 'All 16 B17 and 22 B19 negative response artifacts required.'
    $owned=@($built)+@('Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B21-historical-documentation-binding-attestation.md',
        'Evaluation/P5O2B21Proof/Run-Regressions.ps1','Evaluation/P5O2B21Proof/Write-Verify-BindingAttestation.ps1')
    $encoding=foreach($file in $owned) {
        $bytes=[IO.File]::ReadAllBytes([IO.Path]::GetFullPath($file)); $content=[Text.UTF8Encoding]::new($false,$true).GetString($bytes)
        Require ($content.Length -gt 0 -and $content[0] -ne [char]0xFEFF -and $content.EndsWith("`r`n") -and $content -notmatch '(?<!\r)\n') "Not UTF8/noBOM/CRLF: $file"
        if($file.EndsWith('.ps1')) {
            $tokens=$null; $errors=$null; [Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath($file),[ref]$tokens,[ref]$errors) | Out-Null
            Require ($errors.Count -eq 0) "Invalid PS syntax: $file"
        }
        Fingerprint $file
    }
    $paths=@(git -C D:/Repository/SEE ls-files)
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $collisions=@($paths | Where-Object { -not $seen.Add([string]$_) })
    Require ($collisions.Count -eq 0) 'Git path case collisions.'
    [string[]]$cs=@($built | Where-Object {$_ -like '*.cs'})
    & dotnet format whitespace . --folder --include $cs --verify-no-changes
    Require ($LASTEXITCODE -eq 0) 'Owned C# format failed.'
    & git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $attempts=ReadJson 'artifacts/p5o2b21/attempts.json'
    $attemptFiles=foreach($file in $attempts.PreservedFiles) { Fingerprint $file }
    [xml]$firstTest=Get-Content (Join-Path $validation 'artifacts/p5o2b21/tests/first-attestation.trx') -Raw -Encoding UTF8
    $firstCounts=$firstTest.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($firstCounts.total -eq '36' -and $firstCounts.passed -eq '36' -and $firstCounts.failed -eq '0') 'Initial 36/36 real attestation+B20 run must be retained.'
    $live=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('dotnet.exe','testhost.exe') -and
        $_.CommandLine -and $_.CommandLine.IndexOf($validation,[StringComparison]::OrdinalIgnoreCase) -ge 0 })
    Require ($live.Count -eq 0) 'Owned validation child still running.'
    $output='Evaluation/P5O2B21-historical-documentation-binding-attestation-audit.json'
    if(-not (Test-Path $output)) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),"{}`r`n",[Text.UTF8Encoding]::new($false)) }
    $audit=[ordered]@{
        Package='P5O2B21';VerifiedUtc=[DateTime]::UtcNow.ToString('o');EvidenceVerifierPassed=$true
        Outcome='Bounded B20 binding-admission gap closed by exact actual single-use Worker-exchange authority'
        HistoricalDeclaredActivated=$false;ProductiveSummaryGuardClosed=$false;FinalFindingIntegrationImplemented=$false;FullProductionFindingParityProven=$false
        ProductionSourceChange='Three Main client/validator/receipt files only';NoAlias=$true;NoCurrentFallback=$true;NoWorkerFindingOrGuardPolicy=$true
        TrustBoundary='Trusted Main client, configured Worker image, OS private pipes and reviewed Worker binding emitter. No remote cryptographic attestation or compromised-code/runtime/OS/binary protection.'
        CoherentManipulationsFailClosedAtEvidenceAdmission=$true;ExecutionBindingAttestationClosed=$true;ProtocolVersion=2;WorkerProtocolChanged=$false;WorkerCodeChanged=$false;ProductionModeStillFailClosed=$true
        FollowUp='B22: existing Main pre-Summary guard and final Finding integration with full A-F production parity'
        Scope='Existing B19 evidence-only v1 single complete/proven project/root; no productive Historical Declared scope activated'
        Head=$initial.Head;FinalHead=(git rev-parse HEAD);ExternalHeadMovement=$false;Branch=$initial.Branch
        Protection=[ordered]@{TrackedFiles=927;CoreFiles=23;Stashes=6;RootIgnoreSha256=$initial.RootIgnore;IndexUnchanged=$true;AllUnrelatedBytesUnchanged=$true;HistoricalWorkerSharedAndB5B19TestsUnchanged=$true}
        Export=$export;ProtectedSnapshot=(Fingerprint 'artifacts/p5o2b21/protected-before.json');OwnedFiles=@($encoding)
        Tests=@($census);WaeBuilds=@($logs);DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'));DualGatePassed=$true
        CurrentBaselines=@($baselines);ProductionBinaryComparisons=@($images);HistoricalWorkerDllPdbByteEqual=$true;MainDllPdbChangedForAttestation=$true;SharedAnalyzerSources=121
        OriginalB20AttacksRejected=@($cases);GenuineAttestationCases=@($positives);AttestationNegatives=@($attestationNegatives);RealPhysicalReplayPeer=$physical;ActualProductionControls=@($production);UnchangedNegativeEvidence=@($negative)
        Isolation=[ordered]@{OwnLiveChildren=0;PinnedWorkerOnly=$true;ActualWindowsExecution=$true;LinuxRunnerExecuted=$false;GitCaseCollisionCheck=$true;NoCrossVersionRoslynInContract=$true}
        Attempts=$attempts;AttemptFiles=@($attemptFiles)
        SelfAnalysis=[ordered]@{Findings=16;Fresh=$false;Origin='Inherited B20 baseline, not rerun'}
        CanonicalFindingDiff=[ordered]@{Added=0;Removed=0;ChangedEvidence=0;Fresh=$false;Origin='Inherited B20 baseline, not rerun'}
        OpenPipelineBoundariesFileChanged=$true;OnlyBindingAdmissionGapClosed=$true;FinalGitStatus=@(git status --short);NoCommit=$true;NoPush=$true
    }
    $json=(ConvertTo-Json -InputObject $audit -Depth 100) -replace "`r?`n","`r`n"
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),($json+"`r`n"),[Text.UTF8Encoding]::new($false))
    $parsed=ReadJson $output
    Require ($parsed.EvidenceVerifierPassed -and -not $parsed.HistoricalDeclaredActivated -and -not $parsed.FullProductionFindingParityProven -and
        $parsed.CoherentManipulationsFailClosedAtEvidenceAdmission -and $parsed.ExecutionBindingAttestationClosed -and $parsed.ProductionModeStillFailClosed) 'Dishonest closure JSON.'
    Write-Host 'PASS: B21 exact actual single-use binding attestation; B20 attacks/replay rejected; B5-B19/current/Worker/Historical regression and protection gates preserved. Historical Declared remains unsupported.'
} finally { Pop-Location }
