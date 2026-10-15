param([Parameter(Mandatory=$true)][string]$ValidationRoot, [switch]$AfterPidG1)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$output=Join-Path $toolRoot 'Evaluation/P5O2B24-bounded-finding-contracts-audit.json'
$validationFailures=[Collections.Generic.List[string]]::new()
function Require([bool]$Value,[string]$Message) { if(-not $Value) { throw $Message } }
function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function Attempt([string]$Path) {
    [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
    $failures=@($trx.SelectNodes('//*[local-name()="UnitTestResult" and @outcome="Failed"]') | ForEach-Object {
        [ordered]@{Test=$_.testName;Message=$_.SelectSingleNode('.//*[local-name()="Message"]').InnerText;Stack=$_.SelectSingleNode('.//*[local-name()="StackTrace"]').InnerText}
    })
    [ordered]@{Artifact=(Fingerprint $Path);Total=[int]$counts.total;Passed=[int]$counts.passed;Failed=[int]$counts.failed;Skipped=[int]$counts.notExecuted;Failures=$failures}
}
function Tests([string]$Name,[string]$Path,[int]$Expected) {
    [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $summary=$trx.SelectSingleNode('//*[local-name()="ResultSummary"]')
    $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($summary.outcome -in @('Completed','Failed') -and $counts.total -eq "$Expected" -and $counts.notExecuted -eq '0' -and
        ([int]$counts.passed+[int]$counts.failed) -eq $Expected) "Incomplete/skipped $Name"
    if($counts.failed -ne '0') { $validationFailures.Add($Name) }
    Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge $validationStart) "Stale $Name"
    [ordered]@{Name=$Name;Total=$Expected;Passed=[int]$counts.passed;Failed=[int]$counts.failed;Skipped=0;Artifact=(Fingerprint $Path);Failures=(Attempt $Path).Failures}
}
Push-Location $toolRoot
try {
    $pending=[ordered]@{Package='P5O2B24';EvidenceVerifierPassed=$false;StartedUtc=[DateTime]::UtcNow.ToString('o');State='Running or interrupted'}
    [IO.File]::WriteAllText($output,(($pending | ConvertTo-Json)+"`r`n"),[Text.UTF8Encoding]::new($false))
    $before=ReadJson 'artifacts/p5o2b24/protected-before.json'
    $validationStart=(Get-Item 'artifacts/p5o2b24/protected-before.json').LastWriteTimeUtc
    $initialInputs=ReadJson 'artifacts/p5o2b24/validation-inputs.json'
    if($AfterPidG1) {
        $scope=ReadJson 'artifacts/pid-g1/scope-check.json'
        Require ($scope.Passed -and $scope.B21AuthorityUnchanged -and $scope.B24SemanticSourcesUnchanged) 'PID-G1 scope was not verified.'
        foreach($file in $scope.VerifiedSources) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'PID-G1 scope input changed.' }
        $initialInputs=ReadJson 'artifacts/pid-g1/validation-inputs.json'
        $validationStart=(Get-Item 'artifacts/pid-g1/protected-before.json').LastWriteTimeUtc
    }
    foreach($source in $initialInputs.Sources | Where-Object { $_.Path -notlike 'Tests/*' }) {
        Require ((Get-FileHash (Join-Path $toolRoot $source.Path)).Hash -ceq $source.Sha256) "Production/fixture source changed since regression inputs: $($source.Path)"
    }
    foreach($image in $initialInputs.Images | Where-Object { $_.Path -notlike 'Tests/*' }) {
        Require ((Get-FileHash (Join-Path $ValidationRoot $image.Path)).Hash -ceq $image.Sha256) "Production image changed since regression inputs: $($image.Path)"
    }
    Require ((& git rev-parse HEAD) -ceq $before.Head) 'External HEAD movement.'
    Require ((Get-FileHash (Join-Path $repoRoot '.gitignore')).Hash -ceq $before.Ignore) 'Protected ignore changed.'
    Require ((Get-FileHash (Join-Path $repoRoot '.git/index')).Hash -ceq $before.Index) 'Index changed.'
    Require ((Json @(& git reflog show refs/stash --format='%H %gs')) -ceq (Json $before.Stashes)) 'Stashes changed.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq $before.Core.Count) 'Core inventory changed.'
    foreach($file in $before.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Core bytes changed: $($file.Path)" }
    $export=ReadJson 'artifacts/p5o2b24/export.json'
    Require ([IO.Path]::GetFullPath($ValidationRoot) -ceq $export.ToolRoot) 'Unexpected validation tree.'
    $archiveEntries=@(& tar -tf $export.Archive)
    Require ($LASTEXITCODE -eq 0 -and @($archiveEntries | Where-Object { $_ -match '/(bin|obj|artifacts)/' }).Count -eq 0) 'Export archive contained build artifacts or could not be read.'
    $gateRoot=Join-Path $ValidationRoot 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson 'artifacts/b23-a3d8/Tools/XMLDocNormalizer/artifacts/dual-version-build/audit.json'
    if($gate.Passed) {
        Require ($gate.RuntimeExecuted -and $gate.MainBoundaryExecuted -and $gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Incomplete successful dual audit.'
        Require ((Json $gate.SharedSourceFingerprints) -ceq (Json $parent.SharedSourceFingerprints)) 'Analyzer semantics changed.'
    } else {
        # The gate deliberately fails fast. Preserve its failure; do not reinterpret a pending audit as PASS.
        $validationFailures.Add('dual-version-build-script')
        Require (Test-Path (Join-Path $gateRoot 'main-boundary.trx')) 'Missing actual boundary result.'
    }
    Require (@($parent.SharedSourceFingerprints).Count -eq 121) 'Unexpected parent source inventory.'
    foreach($source in $parent.SharedSourceFingerprints) {
        Require ((Get-FileHash (Join-Path $ValidationRoot $source.Path)).Hash -ceq $source.Sha256) "Shared Analyzer bytes changed: $($source.Path)"
    }
    $builds=foreach($file in Get-ChildItem $gateRoot -Filter '*-build.log') {
        Require ($file.LastWriteTimeUtc -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale WAE log: $($file.Name)"
        $log=Get-Content $file.FullName -Raw -Encoding UTF8
        Require ($log -match '0 Warnung\(en\)' -and $log -match '0 Fehler') "Not clean WAE: $($file.Name)"
        Fingerprint $file.FullName
    }
    Require (@($builds).Count -eq 11) 'Eleven WAE builds required.'
    $runtime=foreach($name in @('worker-identity','worker-smoke-first','worker-smoke-repeat','worker-invalid-version')) {
        $path=Join-Path $gateRoot ($name+'.json')
        Require ((Get-Item $path).LastWriteTimeUtc -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale runtime proof: $name"
        Fingerprint $path
    }
    $identity=ReadJson (Join-Path $gateRoot 'worker-identity.json')
    Require ($identity.success -and -not $identity.identity.runtimeAwaitInformationAvailable -and
        (Json $identity.identity.loadedRoslyn) -ceq (Json $parent.WorkerRuntimeChecks[0].Response.Identity.LoadedRoslyn)) 'Actual Historical runtime differs.'
    $workerImage=Join-Path $ValidationRoot 'artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll'
    Require ((Get-FileHash $workerImage).Hash -ceq $identity.identity.worker.sha256) 'Executed Worker image differs.'
    $smoke=ReadJson (Join-Path $gateRoot 'worker-smoke-first.json')
    Require ($smoke.success -and $smoke.result.entries.Count -eq 1 -and $smoke.result.entries[0].exceptionType.metadataName -eq 'NullReferenceException' -and
        $smoke.result.uncertainties.Count -eq 0 -and $smoke.result.entries[0].paths[0].steps.Count -eq 2) 'Genuine flow smoke failed.'
    Require ((Get-FileHash (Join-Path $gateRoot 'worker-smoke-first.json')).Hash -ceq (Get-FileHash (Join-Path $gateRoot 'worker-smoke-repeat.json')).Hash) 'Worker repeat differs.'
    $invalid=ReadJson (Join-Path $gateRoot 'worker-invalid-version.json')
    Require (-not $invalid.success -and $invalid.failure.code -eq 'unsupportedProtocolVersion') 'Protocol failed open.'
    $expected=[ordered]@{b24=42;b23=36;b22=46;b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=759;focused=866;broad=3167;full=3258}
    if($AfterPidG1) { foreach($name in @('productive','focused','broad','full')) { $expected[$name]+=18 } }
    $tests=foreach($name in $expected.Keys) { Tests $name (Join-Path $ValidationRoot ('artifacts/p5o2b24/tests/'+$name+'.trx')) $expected[$name] }
    $tests+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') $(if($AfterPidG1){596}else{578}))
    if($AfterPidG1) { $tests+=@(Tests 'pid-g1' (Join-Path $ValidationRoot 'artifacts/pid-g1/tests/targeted.trx') 540) }
    $tests+=@(Tests 'boundary-b23' (Join-Path $gateRoot 'external-target.trx') 36)
    $tests+=@(Tests 'boundary-b24' (Join-Path $gateRoot 'bounded-findings.trx') 42)
    $testImage=Join-Path $ValidationRoot 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll'
    foreach($name in @('b24','full')) {
        [xml]$latest=Get-Content (Join-Path $ValidationRoot ('artifacts/p5o2b24/tests/'+$name+'.trx')) -Raw -Encoding UTF8
        Require (([DateTime]$latest.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge (Get-Item $testImage).LastWriteTimeUtc) "Latest strengthened test not rerun: $name"
    }
    $baselines=foreach($file in Get-ChildItem (Join-Path $ValidationRoot 'artifacts/p5o2b9/current') -Filter '*.json') {
        $old=Join-Path $toolRoot ('artifacts/b23-a3d8/Tools/XMLDocNormalizer/artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $old).Hash -ceq (Get-FileHash $file.FullName).Hash) "Current differs: $($file.Name)"
        [ordered]@{Name=$file.Name;ByteEqual=$true;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($baselines).Count -eq 8) 'Eight Current baselines required.'
    $cases=foreach($file in Get-ChildItem (Join-Path $ValidationRoot 'artifacts/p5o2b24/cases') -Filter '*.json') {
        Require ($file.LastWriteTimeUtc -ge $validationStart) "Stale case: $($file.Name)"
        $item=ReadJson $file.FullName
        if($file.Name.StartsWith('positive-')) { Require ($item.FullFindingParity -and $item.FullEvidenceParity -and $item.RepeatEqual -and $item.NoDeclaredInventory -and $item.NoFallback) 'Incomplete positive differential.' }
        if($file.Name.StartsWith('negative-')) {
            Require ($item.NoReceipt -and $item.NoFallback -and $item.Failure -and $item.Execution -eq $(if($item.Project){'summaryEvidence'}else{'directEvidence'})) 'Missing mode/purpose-specific negative proof.'
        }
        if($file.Name.StartsWith('cli-negative-')) {
            Require ($item.Run.ExitCode -eq 4 -and $item.NoFallback -and -not (Test-Path $item.Run.report)) 'CLI negative admitted.'
        } elseif($file.Name.StartsWith('cli-')) {
            Require ($item.Run.ExitCode -eq $(if($item.Empty){0}else{1}) -and -not $item.Run.Stderr) 'CLI failed.'
            Require ($item.Report.Findings.Count -eq $(if($item.Empty){0}else{1}) -and $item.Report.Metrics.FindingCount -eq $item.Report.Findings.Count -and $item.SingleCounting -and $item.SharedJsonReporter) 'CLI/report count differs.'
        }
        Fingerprint $file.FullName
    }
    Require (@($cases | Where-Object { [IO.Path]::GetFileName($_.Path).StartsWith('positive-') }).Count -eq 27) '27 full repeated differentials required.'
    Require (@($cases | Where-Object { [IO.Path]::GetFileName($_.Path).StartsWith('negative-') }).Count -eq 26) '26 per-mode/purpose malformed/replayed/contradictory response proofs required.'
    Require (@($cases | Where-Object { [IO.Path]::GetFileName($_.Path).StartsWith('cli-') }).Count -eq 8) 'Eight real CLI cases required.'
    $paths=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
    $owned=@($paths | Sort-Object -Unique | Where-Object { $_ -notlike '*-audit.json' })
    $sources=@($owned | Where-Object { $_.EndsWith('.cs') -or $_ -like '*/build/Verify-DualVersionBuild.ps1' })
    foreach($path in $sources) {
        $relative=$path.Substring('Tools/XMLDocNormalizer/'.Length)
        Require ((Get-FileHash (Join-Path $repoRoot $path)).Hash -ceq (Get-FileHash (Join-Path $ValidationRoot $relative)).Hash) "Built source mismatch: $path"
    }
    $offline=foreach($relative in @('NuGet.Config','src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config')) {
        $path=Join-Path $ValidationRoot $relative; [xml]$config=Get-Content $path -Raw -Encoding UTF8
        foreach($source in $config.configuration.packageSources.add) {
            if($source.value -match '^https?://') {
                Require (@($config.configuration.disabledPackageSources.add | Where-Object { $_.key -eq $source.key -and $_.value -eq 'true' }).Count -eq 1) "Enabled network source: $($source.key)"
            } else { Require (Test-Path -LiteralPath $source.value -PathType Container) "Missing local source: $($source.value)" }
        }
        Fingerprint $path
    }
    # The older B23 export can contain its own validation-only NuGet configuration.
    # Compare the protected production feed file to committed HEAD, not that export.
    & git diff --quiet HEAD -- src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config
    Require ($LASTEXITCODE -eq 0) 'Production Historical feed configuration changed.'
    & git diff --quiet HEAD -- build/ExceptionFlow.HistoricalSources.props
    Require ($LASTEXITCODE -eq 0) 'Historical source manifest changed.'
    $manifest='build/ExceptionFlow.HistoricalSources.props'
    $localManifest=[IO.File]::ReadAllText((Join-Path $toolRoot $manifest)).Replace("`r`n","`n")
    $parentManifest=[IO.File]::ReadAllText((Join-Path $toolRoot ('artifacts/b23-a3d8/Tools/XMLDocNormalizer/'+$manifest))).Replace("`r`n","`n")
    Require ($localManifest -ceq $parentManifest) 'Historical source manifest content differs (line endings excluded).'
    foreach($relative in @('src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs',
        'src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs',
        'src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.Findings.cs',
        'src/XMLDocNormalizer/XMLDocNormalizer.csproj','src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj',
        'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj')) {
        $old=Join-Path $toolRoot ('artifacts/b23-a3d8/Tools/XMLDocNormalizer/'+$relative)
        Require ((Get-FileHash (Join-Path $toolRoot $relative)).Hash -ceq (Get-FileHash $old).Hash) "Native policy/host/feed bytes changed: $relative"
    }
    foreach($path in $owned) {
        $full=Join-Path $repoRoot $path
        if($path.EndsWith('.json')) { $null=ReadJson $full }
        if($path.EndsWith('.ps1')) { $errors=$null; $tokens=$null; $null=[Management.Automation.Language.Parser]::ParseFile($full,[ref]$tokens,[ref]$errors); Require ($errors.Count -eq 0) "PS parse: $path" }
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Format-Owned.ps1 -Verify
    Require ($LASTEXITCODE -eq 0) 'Format/UTF8/CRLF failed.'
    & git diff --check; Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($path in @(& git -C $repoRoot ls-files)+@(& git -C $repoRoot ls-files --others --exclude-standard)) { Require ($seen.Add($path)) "Case collision: $path" }
    $workerChanges=foreach($file in Get-ChildItem src/XMLDocNormalizer.HistoricalWorker -File -Filter '*.cs') {
        $old=Join-Path $toolRoot ('artifacts/b23-a3d8/Tools/XMLDocNormalizer/src/XMLDocNormalizer.HistoricalWorker/'+$file.Name)
        [ordered]@{Path=$file.Name;Changed=((Get-FileHash $file.FullName).Hash -cne (Get-FileHash $old).Hash)}
    }
    $audit=[ordered]@{
        Package='P5O2B24';VerifiedUtc=[DateTime]::UtcNow.ToString('o');EvidenceVerifierPassed=($validationFailures.Count -eq 0)
        ArtifactConsistencyPassed=$true;AllRequiredValidationPassed=($validationFailures.Count -eq 0);FailedRequiredValidation=@($validationFailures.ToArray())
        Head=(& git rev-parse HEAD);ExternalHeadMovement=$false;Scope='Unsigned complete/proven v1 single-project selected-root Direct/Project/unchanged Declared; no Solution'
        Tests=$tests;WaeBuilds=$builds;CurrentBaselines=$baselines;Cases=$cases;DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'))
        DualGateScriptPassed=[bool]$gate.Passed;RuntimeProofs=$runtime;GateFollowOnStepsInvokedSeparately=(-not $gate.Passed)
        AnalyzerSemanticsChanged=$false;RoutingChanged=$false;RoslynVersionsChanged=$false;SharedAnalyzerSources=121;WorkerSourceChanges=$workerChanges
        BinaryChanges=$(if($AfterPidG1){'PID-G1 changes Main lifecycle diagnostics and tests only; Worker/fixture/B24 semantic sources and shared Analyzer remain unchanged from the captured B24 WIP. No cross-export DLL equality claim.'}else{'Main/Worker hosts and test fixture changed; shared Historical Analyzer sources/host unchanged. No cross-export absolute-root DLL/PDB equality claim.'})
        PidG1LifetimeRepair=[bool]$AfterPidG1
        PidG1PriorAudit=$(if($AfterPidG1){Fingerprint 'artifacts/pid-g1/before-wip/Tools/XMLDocNormalizer/Evaluation/P5O2B24-bounded-finding-contracts-audit.json'}else{$null})
        InitialValidationInputs=$(if($AfterPidG1){Fingerprint 'artifacts/pid-g1/validation-inputs.json'}else{Fingerprint 'artifacts/p5o2b24/validation-inputs.json'})
        TestSupplement=$(if($AfterPidG1){'Old PID-only helpers replaced by exact owned-process exit observations; 18 deterministic/real lifetime regressions added. All B24 semantic inputs unchanged except explicitly scope-verified client diagnostic wrappers. Full updated regression/build inputs rerun.'}else{'Project negatives strengthened to genuine SummaryEvidence; new B24 helper uses original completed B21 execution instead of recycled PID lookup. Latest B24 and full suite rerun. Production/fixture source and image hashes must remain identical; old tests untouched.'})
        Protection=[ordered]@{CoreFileCount=$before.Core.Count;CoreUnchanged=$true;StashCount=$before.Stashes.Count;StashesUnchanged=$true;RootIgnoreUnchanged=$true;IndexUnchanged=$true}
        SelfAnalysis=[ordered]@{Fresh=$false;PriorFindings=16;Origin='Inherited B22/B21; not a fresh analysis of B24'}
        CanonicalFindingDiff=[ordered]@{Fresh=$false;PriorAdded=0;PriorRemoved=0;PriorChangedEvidence=0;Origin='Inherited B22/B21, not rerun'}
        OwnedFiles=@($owned | ForEach-Object { Fingerprint (Join-Path $repoRoot $_) })
        Attempts=@((Attempt 'artifacts/p5o2b24/attempt-second-gate/main-boundary.trx'),
            (Attempt 'artifacts/p5o2b24/attempt-third-gate/main-boundary.trx'),
            (Attempt 'artifacts/p5o2b24/attempt-fourth-gate/main-boundary.trx'),
            (Attempt (Join-Path $ValidationRoot 'artifacts/p5o2b24/attempts/b24-contract-first.trx')),
            (Attempt (Join-Path $ValidationRoot 'artifacts/p5o2b24/attempts/b24-contract-second.trx')),
            (Attempt (Join-Path $ValidationRoot 'artifacts/p5o2b24/attempts/b24-cli-first.trx')),
            (Attempt 'artifacts/p5o2b24/attempt-initial-regression/tests/broad.trx'),
            (Attempt 'artifacts/p5o2b24/attempt-initial-regression/tests/full.trx'))
        OtherAttempts='See report: initial local cache long-path copy, WAE null preconditions, intermediate format/setup commands. No old flake fix, exclusions or suppressed diagnostics.'
        NetworkUsed=$false;OfflineValidationConfigs=$offline;FinalGitStatus=@(& git status --short);NoCommit=$true;NoPush=$true
    }
    [IO.File]::WriteAllText($output,(($audit | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
    if($validationFailures.Count -ne 0) {
        throw ('Evidence collected and artifact consistency verified, but required validation remains FAILED: '+($validationFailures -join ', ')+'. Audit records false, not PASS.')
    }
    Write-Host 'PASS: B24 Finding differentials, CLI/counting, unchanged regressions, full dual gate, protected state, format and evidence.'
} finally { Pop-Location }
