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
    $initial=ReadJson 'artifacts/p5o2b20/protected-before.json'; $export=ReadJson 'artifacts/p5o2b20/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -ceq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Clean recorded HEAD archive required.'
    Require ((Get-FileHash artifacts/p5o2b20/head.tar).Hash -ceq $export.ArchiveSha256) 'Archive changed.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'External HEAD movement: audit before continuing.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected ignore changed.'
    Equal @(git stash list --format='%H %s') $initial.Stashes 'Stashes changed.'
    Equal @(git diff --cached --name-only) $initial.Index 'Index changed.'
    Require ($initial.TrackedFiles.Count -eq 922 -and $initial.CoreFiles.Count -eq 23 -and $initial.Stashes.Count -eq 6) 'Unexpected protected inventory.'
    $allowed=[IO.Path]::GetFullPath('build/Verify-DualVersionBuild.ps1')
    foreach($file in $initial.TrackedFiles) { if($file.Path -cne $allowed) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/production/B5-B19 source changed: $($file.Path)" } }
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core inventory changed.'
    foreach($file in $initial.CoreFiles) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'Core byte change.' }
    [string[]]$built=ReadJson 'artifacts/p5o2b20/owned-build-inputs.json'
    Require ($built.Count -eq 2) 'Exactly new tests and permanent gate are build changes.'
    foreach($file in $built) { Require ((Get-FileHash $file).Hash -ceq (Get-FileHash (Join-Path $validation $file)).Hash) "Final built overlay mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'; $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Analyzer build boundary changed.'
    Equal $gate.SharedSourceFingerprints $parent.SharedSourceFingerprints 'Shared analyzer sources changed.'
    $images=foreach($image in $gate.BuildImages) {
        $before=@($parent.BuildImages | Where-Object {$_.Step -ceq $image.Step})
        Require ($before.Count -eq 1 -and $image.Image.Sha256 -ceq $before[0].Image.Sha256 -and $image.Image.PdbSha256 -ceq $before[0].Image.PdbSha256) "Production/Worker DLL/PDB changed: $($image.Step)"
        [ordered]@{Step=$image.Step;Owner=$image.Owner;Before=$before[0].Image;After=$image.Image;DllPdbByteEqual=$true}
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
    $expected=[ordered]@{b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=716;broad=3017;full=3108}
    $census=foreach($name in $expected.Keys) {
        $artifact=if($name -eq 'full') { 'full-repeat.trx' } else { $name+'.trx' }
        Tests $name (Join-Path $validation ('artifacts/p5o2b20/tests/'+$artifact)) $expected[$name]
    }
    $census+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 506)
    $census+=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 496 $false)
    $baselines=foreach($file in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $file.FullName).Hash -ceq (Get-FileHash $after).Hash) "Current baseline changed: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight complete native Current baselines required.'
    $cases=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b20') -File -Filter 'admission-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.OriginalPayloadRetained -and $value.SourceAndSpansUnchanged -and $value.DirectSummaryHashesUnchanged -and
            $value.CoherentSubstitutionAccepted -and $value.EvidenceReceiptWithoutTransport -and $value.BindingAssociationNotAttested -and
            $value.CompleteFindingParityNotProved -and $value.ProductionStillUnsupported -and $value.NoFallback -and
            $value.ProcessesStopped -and $value.NoHistoricalLoadedInMain) 'Admission witness claims wrong.'
        Equal $value.Native.Summary $value.Summary 'Genuine Summary differs.'
        Equal $value.Native.Direct $value.Direct 'Genuine Direct differs.'
        Equal $value.Native.Declarations $value.Declarations 'Genuine declarations/ownership differ.'
        Equal $value.GenuineDocument.Syntax $value.ModifiedDocument.Syntax 'Original syntax changed.'
        Equal $value.GenuineDocument.Method $value.ModifiedDocument.Method 'Original root changed.'
        Equal $value.GenuineDocument.DirectSha256 $value.ModifiedDocument.DirectSha256 'Direct hash changed.'
        Equal $value.GenuineDocument.SummarySha256 $value.ModifiedDocument.SummarySha256 'Summary hash changed.'
        Equal $value.GenuineFacts.Member $value.AdmittedFacts.Member 'Original Main anchor changed.'
        Equal $value.AdmittedFacts $value.RepeatedAdmittedFacts 'Repeated admission differs.'
        Require ((Json $value.GenuineFacts.Tags) -cne (Json $value.AdmittedFacts.Tags)) 'Binding substitution did not reach Main receipt.'
        Require ($value.GenuineFacts.Tags.Count -eq 1 -and $value.AdmittedFacts.Tags.Count -eq 1) 'Incomplete witness.'
        if($value.Substitution -eq 'redirect-named') {
            Require ($value.GenuineFacts.Tags[0].IsException -eq $false -and $value.AdmittedFacts.Tags[0].IsException -eq $true) 'Named redirect absent.'
            Require ($value.Native.DeclaredFindings.Smell.Id -contains 'DOC670' -and $value.Native.DeclaredFindings.Smell.Id -contains 'DOC610') 'Complete actual native Finding effect absent.'
        } else {
            Require ($value.GenuineFacts.Tags[0].IsException -eq $true -and $null -eq $value.AdmittedFacts.Tags[0].Type) 'Binding kind erasure absent.'
            Require ($value.Native.DeclaredFindings.Smell.Id -notcontains 'DOC610' -and $value.Native.DeclaredFindings.Smell.Id -notcontains 'DOC660') 'Actual exact coverage not established.'
        }
        [ordered]@{Artifact=(Fingerprint $file.FullName);Evidence=$value}
    }
    Require (@($cases).Count -eq 3) 'Three coherent substitution witnesses required.'
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
    $owned=@($built)+@('Evaluation/P5O2B20-historical-declared-production-boundary.md',
        'Evaluation/P5O2B20Proof/Run-Regressions.ps1','Evaluation/P5O2B20Proof/Write-Verify-ProductionBoundary.ps1')
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
    & dotnet format whitespace . --folder --include Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs --verify-no-changes
    Require ($LASTEXITCODE -eq 0) 'Owned C# format failed.'
    & git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $attempts=ReadJson 'artifacts/p5o2b20/attempts.json'
    $attemptFiles=foreach($file in $attempts.PreservedFiles) { Fingerprint $file }
    [xml]$firstTest=Get-Content (Join-Path $validation 'artifacts/p5o2b20/tests/first-b20.trx') -Raw -Encoding UTF8
    $firstCounts=$firstTest.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($firstCounts.total -eq '10' -and $firstCounts.passed -eq '9' -and $firstCounts.failed -eq '1') 'Initial test failure was not preserved honestly.'
    [xml]$firstFull=Get-Content (Join-Path $validation 'artifacts/p5o2b20/attempts/full-first/full.trx') -Raw -Encoding UTF8
    $firstFullCounts=$firstFull.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($firstFullCounts.total -eq '3108' -and $firstFullCounts.passed -eq '3107' -and $firstFullCounts.failed -eq '1' -and $firstFullCounts.notExecuted -eq '0') 'Initial full-suite result not preserved.'
    $failed=@($firstFull.SelectNodes('//*[local-name()="UnitTestResult"]') | Where-Object outcome -eq 'Failed')
    Require ($failed.Count -eq 1 -and $failed[0].testName -ceq 'XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed') 'Unexpected initial full-suite failure.'
    $readerTest='Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalMetadataReferenceFactoryTests.cs'
    Require ((Get-FileHash $readerTest).Hash -ceq (Get-FileHash (Join-Path $validation $readerTest)).Hash) 'Known reader test not unchanged in export.'
    $live=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('dotnet.exe','testhost.exe') -and
        $_.CommandLine -and $_.CommandLine.IndexOf($validation,[StringComparison]::OrdinalIgnoreCase) -ge 0 })
    Require ($live.Count -eq 0) 'Owned validation child still running.'
    $output='Evaluation/P5O2B20-historical-declared-production-boundary-audit.json'
    if(-not (Test-Path $output)) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),"{}`r`n",[Text.UTF8Encoding]::new($false)) }
    $audit=[ordered]@{
        Package='P5O2B20';VerifiedUtc=[DateTime]::UtcNow.ToString('o');EvidenceVerifierPassed=$true
        Outcome='Stop-rule Boundary Audit: coherent documentation binding association substitution can mint a B19 evidence receipt'
        HistoricalDeclaredActivated=$false;ProductiveSummaryGuardClosed=$false;FinalFindingIntegrationImplemented=$false;FullProductionFindingParityProven=$false
        NoProductionSourceChange=$true;NoAlias=$true;NoCurrentFallback=$true;NoWorkerFindingOrGuardPolicy=$true
        Gap='Original source/context/hash/PE structural checks do not attest the cref-to-compiler-binding association or exclusively genuine Worker execution at final Main receipt admission.'
        CoherentManipulationsFailClosedAtEvidenceAdmission=$false;ProductionModeStillFailClosed=$true
        FollowUp='Proposed P5O2B21 Execution-bound Documentation Binding Admission, then common Main guard/final Finding integration and full A-F production parity'
        Scope='Existing B19 evidence-only v1 single complete/proven project/root; no productive Historical Declared scope activated'
        Head=$initial.Head;FinalHead=(git rev-parse HEAD);ExternalHeadMovement=$false;Branch=$initial.Branch
        Protection=[ordered]@{TrackedFiles=922;CoreFiles=23;Stashes=6;RootIgnoreSha256=$initial.RootIgnore;IndexUnchanged=$true;AllUnrelatedAndProductionBytesUnchanged=$true}
        Export=$export;ProtectedSnapshot=(Fingerprint 'artifacts/p5o2b20/protected-before.json');OwnedFiles=@($encoding)
        Tests=@($census);WaeBuilds=@($logs);DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'));DualGatePassed=$true
        CurrentBaselines=@($baselines);ProductionBinaryComparisons=@($images);AllMainHistoricalWorkerDllPdbByteEqual=$true;SharedAnalyzerSources=121
        AdmissionGapWitnesses=@($cases);ActualProductionControls=@($production);UnchangedNegativeEvidence=@($negative)
        Isolation=[ordered]@{OwnLiveChildren=0;PinnedWorkerOnly=$true;ActualWindowsExecution=$true;LinuxRunnerExecuted=$false;GitCaseCollisionCheck=$true;NoCrossVersionRoslynInContract=$true}
        Attempts=$attempts;AttemptFiles=@($attemptFiles)
        SelfAnalysis=[ordered]@{Findings=16;Fresh=$false;Origin='Inherited B19 baseline, not rerun'}
        CanonicalFindingDiff=[ordered]@{Added=0;Removed=0;ChangedEvidence=0;Fresh=$false;Origin='Inherited B19 baseline, not rerun'}
        OpenPipelineBoundariesFileChanged=$false;FinalGitStatus=@(git status --short);NoCommit=$true;NoPush=$true
    }
    $json=(ConvertTo-Json -InputObject $audit -Depth 100) -replace "`r?`n","`r`n"
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),($json+"`r`n"),[Text.UTF8Encoding]::new($false))
    $parsed=ReadJson $output
    Require ($parsed.EvidenceVerifierPassed -and -not $parsed.HistoricalDeclaredActivated -and -not $parsed.FullProductionFindingParityProven -and
        -not $parsed.CoherentManipulationsFailClosedAtEvidenceAdmission -and $parsed.ProductionModeStillFailClosed) 'Dishonest closure JSON.'
    Write-Host 'PASS: B20 stop-rule audit, three coherent admission-gap witnesses, real native production guards, unchanged B5-B19/full regressions/binaries/baselines/protection. Historical Declared remains unsupported.'
} finally { Pop-Location }
