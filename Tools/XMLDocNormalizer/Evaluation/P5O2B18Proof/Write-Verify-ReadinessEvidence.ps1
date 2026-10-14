param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $toolRoot
try {
    function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
    function Equal($Left,$Right,[string]$Message) { Require ((Json $Left) -ceq (Json $Right)) $Message }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function Tests([string]$Name,[string]$Path,[int]$Expected,[bool]$Fresh=$true) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Incomplete/failed: $Name"
        Require ($counts.total -eq "$Expected" -and $counts.passed -eq "$Expected" -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Census/failure/skips: $Name"
        if($Fresh) { Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale: $Name" }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b18/protected-before.json'; $export=ReadJson 'artifacts/p5o2b18/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -ceq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Clean recorded HEAD archive required.'
    Require ((Get-FileHash artifacts/p5o2b18/head.tar).Hash -ceq $export.ArchiveSha256) 'Archive changed.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'External HEAD movement: stop and audit.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected ignore changed.'
    Equal @(git stash list --format='%H %s') $initial.Stashes 'Stashes changed.'
    Equal @(git diff --cached --name-only) $initial.Index 'Index changed.'
    [string[]]$built=ReadJson 'artifacts/p5o2b18/owned-build-inputs.json'
    Require ($built.Count -eq 2) 'Exactly test and gate build inputs required: no production activation.'
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})+@([IO.Path]::GetFullPath('Evaluation/OPEN-PIPELINE-BOUNDARIES.md'))
    foreach($file in $initial.TrackedFiles) {
        if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/production/Current/B5-B17 source changed: $($file.Path)" }
    }
    Require ($initial.TrackedFiles.Count -eq 909 -and $initial.CoreFiles.Count -eq 23 -and $initial.Stashes.Count -eq 6) 'Unexpected protected inventory.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core inventory changed.'
    foreach($file in $initial.CoreFiles) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'Core byte change.' }
    foreach($file in $built) { Require ((Get-FileHash $file).Hash -ceq (Get-FileHash (Join-Path $validation $file)).Hash) "Built overlay mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'; $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Analyzer build boundary changed.'
    Equal $gate.SharedSourceFingerprints $parent.SharedSourceFingerprints 'Shared sources changed.'
    foreach($image in $gate.BuildImages) {
        $before=@($parent.BuildImages | Where-Object {$_.Step -ceq $image.Step})
        Require ($before.Count -eq 1 -and $image.Image.Sha256 -ceq $before[0].Image.Sha256 -and $image.Image.PdbSha256 -ceq $before[0].Image.PdbSha256) 'Production DLL/PDB changed.'
    }
    for($index=0;$index -lt 2;$index++) {
        Require ($gate.WorkerBuildImages[$index].Sha256 -ceq $parent.WorkerBuildImages[$index].Sha256 -and
            $gate.WorkerBuildImages[$index].PdbSha256 -ceq $parent.WorkerBuildImages[$index].PdbSha256) 'Worker DLL/PDB changed.'
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
    $expected=[ordered]@{b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=667;broad=2968;full=3059}
    $census=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b18/tests/'+$name+'.trx')) $expected[$name] }
    $census+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 457)
    $census+=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 450 $false)
    $baselines=foreach($file in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $file.FullName).Hash -ceq (Get-FileHash $after).Hash) "Current baseline changed: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight native Current baselines required.'
    $cases=foreach($name in @('cover-exact','cover-base','inheritdoc','non-exception','guard','chain')) {
        $path=Join-Path $validation ('artifacts/p5o2b18/'+$name+'.json'); $value=ReadJson $path
        Require ($value.DeclaredStillUnsupported -and $value.ProcessesStopped) 'Partial activation/cleanup claim wrong.'
        if($name -in @('cover-exact','cover-base','inheritdoc','non-exception')) {
            Require ($value.EqualFullCanonicalFlowsAndPolicyFacts -and $value.NoHistoricalFindingImport) 'Wrong finding boundary.'
            Equal $value.First.Result $value.Changed.Result 'Canonical flows differ.'
            Equal $value.First.DeclaredPolicy $value.Changed.DeclaredPolicy 'Full policy facts differ.'
            Equal $value.Changed.Result $value.Repeated.Result 'Flow repeat differs.'
            Equal $value.Changed.DeclaredPolicy $value.Repeated.DeclaredPolicy 'Policy repeat differs.'
            Equal $value.Before.Summary $value.After.Summary 'Native summaries differ.'
            Equal $value.Before.Direct $value.After.Direct 'Native direct result differs.'
            Require ($value.Eligibility.Decision.Relevant -and $null -eq $value.Eligibility.Failure) 'No validated Direct eligibility.'
            if($name -ceq 'cover-base') {
                Equal $value.Before.DeclaredFindings $value.After.DeclaredFindings 'Base-type control changed Current semantics.'
                Require (-not $value.DifferentFullNativeFindings) 'False finding-difference claim.'
            } else {
                Require ($value.DifferentFullNativeFindings -and (Json $value.Before.DeclaredFindings) -cne (Json $value.After.DeclaredFindings)) 'No genuine full Finding difference.'
            }
            $calls=@($value.First,$value.Changed,$value.Repeated)
        } elseif($name -ceq 'guard') {
            Require (-not $value.Native.HasDeclaredTypes -and $value.Native.Direct.Entries.Count -eq 0 -and $value.Native.DeclaredFindings.Count -eq 0) 'Wrong native guard witness.'
            Require ($value.HistoricalFailure.Failure.WorkerFailure.Code -ceq 'analysisFailure' -and $null -eq $value.HistoricalFailure.Result -and $null -eq $value.HistoricalFailure.DeclaredPolicy -and $value.NoFallback) 'Lost Worker failure or partial policy.'
            $calls=@($value.HistoricalFailure)
        } else {
            Equal $value.Native.Summary $value.Historical.Result 'Full chain flow parity differs.'
            Equal $value.Historical.Result $value.Repeated.Result 'Chain repeat differs.'
            Require ($value.Decision.Decision.Relevant -and $value.NoHistoricalFindingImport) 'Wrong chain policy boundary.'
            Require ((Json $value.Native.ProjectFindings) -cne (Json $value.Native.DeclaredFindings)) 'No Current mode difference.'
            $calls=@($value.Historical,$value.Repeated)
        }
        foreach($call in $calls) {
            Require ($call.ProcessId -gt 0) 'Missing actual process evidence.'
            if($null -ne $call.Result) {
                Require ($null -ne $call.Identity -and $call.Identity.LoadedRoslyn.Count -eq 2) 'Missing success runtime evidence.'
                foreach($compiler in $call.Identity.LoadedRoslyn) {
                    Require ($gate.HistoricalCompilerReferences.Sha256 -contains $compiler.Sha256) 'Not pinned Historical compiler.'
                }
            } else {
                Require ($name -ceq 'guard' -and $call.Failure.WorkerFailure.Code -ceq 'analysisFailure') 'Missing structured failure.'
            }
        }
        [ordered]@{Scenario=$name;Value=$value;Artifact=(Fingerprint $path)}
    }
    $b17Cases=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b17') -File -Filter 'case-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.OriginalValidatedProjection -and $value.NoFallback -and $value.ProcessesStopped -and $value.NoHistoricalLoadedInMain -and $value.DeclaredStillUnsupported -and $value.NoFindingImport) 'B17 boundary changed.'
        Equal $value.Native.Summary $value.Historical 'B17 full canonical parity changed.'
        Equal $value.Native.Direct $value.PolicyEvidence.DirectResult 'B17 Direct changed.'
        Equal $value.Native.Declarations $value.PolicyEvidence.Declarations 'B17 complete ownership changed.'
        Equal $value.PolicyEvidence $value.RepeatedPolicyEvidence 'B17 repeat changed.'
        foreach($decision in $value.Decisions) { Require ($decision.Native.Relevant -eq $decision.Historical.Decision.Relevant -and $null -eq $decision.Historical.Failure) 'B17 eligibility differs.' }
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($b17Cases).Count -eq 7) 'B17 seven differentials absent.'
    $negatives=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b17') -File -Filter 'negative-response-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.Failure.Code -ceq 'malformedResponse' -and $value.NoPolicyReceipt -and $value.NoResult -and $value.NoFallback -and $value.ProcessesStopped) 'Missing/forged evidence not fail closed.'
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($negatives).Count -eq 16) 'Sixteen B17 response negatives absent.'
    $queryNegative=ReadJson (Join-Path $validation 'artifacts/p5o2b17/query-negative.json')
    Require ($queryNegative.MissingType -and $queryNegative.MissingDefaultInvalidMode -and $queryNegative.UnknownSourceKind -and
        $queryNegative.UnknownTypeRejected -and $queryNegative.LegacyCannotProducePolicyDecision -and $queryNegative.NoFallback -and $queryNegative.ProcessesStopped) 'B17 query admission weakened.'
    $requestNegative=ReadJson (Join-Path $validation 'artifacts/p5o2b17/request-negative.json')
    Require ($requestNegative.RequiredModeProfileAndInventory -and $requestNegative.OtherModesRefused -and
        $requestNegative.CallerInventoryRefused -and $requestNegative.MissingCopiedScopeRefused -and
        $requestNegative.SolutionScopeRefused -and $requestNegative.GenericRefused -and $requestNegative.NoPartialPolicy -and
        $requestNegative.NoFallback -and $requestNegative.ProcessesStopped) 'B17 request admission weakened.'
    $processes=@(Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and $_.CommandLine.Contains($validation) -and $_.CommandLine -match '(XMLDocNormalizer.HistoricalWorker.dll|HistoricalPdbFixture.dll|BoundaryTestProcess.dll)'
    })
    Require ($processes.Count -eq 0) 'Own isolated child still alive.'
    $owned=@('Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs','build/Verify-DualVersionBuild.ps1',
        'Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B18-historical-declared-activation-boundary.md',
        'Evaluation/P5O2B18Proof/Run-Regressions.ps1','Evaluation/P5O2B18Proof/Write-Verify-ReadinessEvidence.ps1')
    $encoding=foreach($file in $owned) {
        $bytes=[IO.File]::ReadAllBytes([IO.Path]::GetFullPath($file)); $content=[Text.UTF8Encoding]::new($false,$true).GetString($bytes)
        Require ($content.Length -gt 0 -and $content[0] -ne [char]0xFEFF -and $content.EndsWith("`r`n") -and $content -notmatch '(?<!\r)\n') "Not UTF8/noBOM/CRLF: $file"
        if($file.EndsWith('.ps1')) {
            $tokens=$null; $errors=$null; [Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath($file),[ref]$tokens,[ref]$errors) | Out-Null
            Require ($errors.Count -eq 0) "Invalid PS syntax: $file"
        }
        Fingerprint $file
    }
    $collisions=@(git -C D:/Repository/SEE ls-files | Group-Object {([string]$_).ToLowerInvariant()} | Where-Object Count -gt 1)
    Require ($collisions.Count -eq 0) 'Git path case collisions.'
    $gitReferences=ReadJson 'artifacts/p5o2b18/git-project-reference-census.json'
    Require (@($gitReferences).Count -eq 6 -and @($gitReferences | Where-Object {-not $_.ExactGitCase}).Count -eq 0) 'ProjectReference Git census failed.'
    & dotnet format whitespace . --folder --include $owned[0] --verify-no-changes
    Require ($LASTEXITCODE -eq 0) 'Owned C# format failed.'
    & git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $output='Evaluation/P5O2B18-historical-declared-activation-boundary-audit.json'
    if(-not (Test-Path $output)) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),"{}`r`n",[Text.UTF8Encoding]::new($false)) }
    $attemptFiles=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b18/attempts') -Recurse -File) { Fingerprint $file.FullName }
    $audit=[ordered]@{
        Package='P5O2B18';VerifiedUtc=[DateTime]::UtcNow.ToString('o');EvidenceVerifierPassed=$true
        Closure='Independent contract-gap root-cause audit; requested activation NOT achieved'
        HistoricalDeclaredActivated=$false;FullHistoricalFindingParityProven=$false;ProductionChanges=0
        NoAlias=$true;NoPartialActivation=$true;NoCurrentFallback=$true;NoWorkerPolicyDuplication=$true
        FindingGap='Existing B17 eligibility does not provide semantic documentation/exact coverage/inheritdoc/member Finding context. Native productive detector cannot consume canonical Historical results.'
        GuardGap='No-local Current Declared returns independent Direct; unguarded ProjectTransitive can fail with uncertainty.'
        CurrentCoverage='Exact symbol equality, NOT base-type coverage; fresh source audit corrects inherited B17 report wording.'
        Head=$initial.Head;FinalHead=(git rev-parse HEAD);ExternalHeadMovement=$false;Branch=$initial.Branch
        Protection=[ordered]@{TrackedFiles=909;CoreFiles=23;Stashes=6;AllBytesUnchangedExceptGateAndBoundaryNote=$true;RootIgnoreSha256=$initial.RootIgnore;IndexUnchanged=$true}
        Export=$export;ProtectedSnapshot=(Fingerprint 'artifacts/p5o2b18/protected-before.json')
        Tests=@($census);WaeBuilds=@($logs);DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'));DualGatePassed=$gate.Passed
        CurrentBaselines=@($baselines);AnalyzerAndWorkerDllPdbByteEqual=$true;SharedSources=121
        BoundaryCases=@($cases);B17Cases=@($b17Cases);B17ResponseNegatives=@($negatives)
        B17QueryNegative=$queryNegative;B17RequestNegative=$requestNegative
        ModeAssessment=@(
            [ordered]@{Mode='Direct';Historical='Existing bounded supported scope unchanged';FinalHistoricalFindings='Not imported'},
            [ordered]@{Mode='ProjectTransitive';Historical='Existing bounded single project unchanged';FinalHistoricalFindings='Not imported'},
            [ordered]@{Mode='SolutionTransitive';Historical='Existing bounded B15 DAG unchanged';FinalHistoricalFindings='Not imported'},
            [ordered]@{Mode='ProjectTransitiveDeclaredExceptions';Historical='Unsupported: independent reporting contract gap';FinalHistoricalFindings='Not imported'}
        )
        GitProjectReferences=$gitReferences;GitProjectReferenceCensus=(Fingerprint 'artifacts/p5o2b18/git-project-reference-census.json')
        Isolation=[ordered]@{OwnLiveChildren=0;ActualWindowsExecution=$true;LinuxRemoteExecution=$false;PinnedHistoricalWorkerOnly=$true;NoCrossVersionSymbolImport=$true}
        OwnedFiles=@($encoding);Attempts=(ReadJson 'artifacts/p5o2b18/attempts.json');AttemptFiles=@($attemptFiles)
        SelfAnalysis=[ordered]@{Findings=16;Fresh=$false;Origin='Inherited B17/B16 baseline'}
        CanonicalFindingDiff=[ordered]@{Added=0;Removed=0;ChangedEvidence=0;Fresh=$false;Origin='Inherited baseline'}
        FinalGitStatus=@(git status --short);NoCommit=$true;NoPush=$true
    }
    $json=(ConvertTo-Json -InputObject $audit -Depth 100) -replace "`r?`n","`r`n"
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),($json+"`r`n"),[Text.UTF8Encoding]::new($false))
    $parsed=ReadJson $output
    Require ($parsed.EvidenceVerifierPassed -and -not $parsed.HistoricalDeclaredActivated -and -not $parsed.FullHistoricalFindingParityProven) 'Invalid closure JSON.'
    Write-Host 'PASS: B18 root-cause stop, no activation; all regressions, exact byte baselines, production/protection/isolation and evidence verified.'
} finally { Pop-Location }
