$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    $initial = ReadJson 'artifacts/p5o2b8/protected-before.json'
    $baseline = ReadJson 'Evaluation/P5O2B7-trusted-selection-context-projection-audit.json'
    $gate = ReadJson 'artifacts/dual-version-build/audit.json'
    $boundary = ReadJson 'artifacts/p5o2b8/dispatch-boundary.json'
    $emission = ReadJson 'artifacts/p5o2b7/historical-pdb/emission-evidence.json'
    $head = git rev-parse HEAD
    Require ($head -eq $initial.Head) 'External HEAD movement requires a new explicit audit.'
    Require ((Get-FileHash ../../.gitignore).Hash -eq $initial.RootIgnore) 'Root ignore WIP changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -ceq ($initial.Stashes -join "`n")) 'Protected stashes changed.'
    Require ((@(git diff --cached --name-only) -join "`n") -ceq ($initial.Index -join "`n")) 'Index changed.'
    foreach ($file in @($initial.MainSources) + @($initial.CoreFiles) + @($initial.BoundaryFiles) + @($initial.ExistingWorkerTests) + @($initial.Projects)) {
        Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Existing/protected file changed: $($file.Path)"
    }
    Require ($initial.MainSources.Count -eq 370 -and $initial.CoreFiles.Count -eq 23) 'Wrong starting source/Core census.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -File -Recurse).Count -eq 23) 'Core file set changed.'
    Require ($gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted) 'Permanent gate incomplete.'
    Require ($gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2 -and $gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Build/source boundary changed.'
    foreach ($file in $gate.SharedSourceFingerprints) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) 'Shared Analyzer source changed.' }
    Require ($boundary.CurrentSelection -eq 'Current' -and $boundary.HistoricalSelection -eq 'Historical') 'Wrong integrated decisions.'
    Require ($boundary.CurrentContext.Provenance -eq 'currentCompilation' -and $boundary.HistoricalContext.Provenance -eq 'validatedPortablePdb') 'Origin lost.'
    Require ($boundary.CanonicalParity -and $boundary.HistoricalDeterministic -and $boundary.ProcessesStopped -and ($boundary.HistoricalLoadedInCaller -eq $false)) 'Runtime/parity/isolation proof incomplete.'
    Require ($boundary.RealPdbValidation -eq 'IdentityAndChecksum' -and $boundary.ActualFactoryReceipt) 'Real target-bound PDB trust proof missing.'
    Require ($boundary.HistoricalPeSha256 -eq $emission.PeSha256 -and $boundary.HistoricalPdbSha256 -eq $emission.PdbSha256) 'Historical emission does not match actual dispatch provenance.'
    $historicalPath = 'artifacts/p5o2b7/historical-pdb/historical-input.dll'
    Require ((Get-FileHash $historicalPath).Hash -eq $emission.PeSha256 -and (Get-FileHash ([IO.Path]::ChangeExtension($historicalPath,'.pdb'))).Hash -eq $emission.PdbSha256) 'Historical input changed.'
    foreach ($engine in $emission.CompilerRuntime) {
        Require ($engine.InformationalVersion -ceq $boundary.HistoricalContext.CompilerVersion) 'Wrong original emitting engine.'
        Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256) 'Fixture is outside proven Historical universe.'
    }
    foreach ($engine in $boundary.HistoricalIdentity.LoadedRoslyn) {
        Require ($engine.InformationalVersion -ceq $boundary.HistoricalContext.CompilerVersion) 'Worker differs from projected compiler.'
        Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256) 'Worker loaded wrong engine image.'
    }
    Require ($boundary.CurrentContext.CompilerVersion -ceq $baseline.ProjectionBoundary.CurrentRuntimeInformationalVersion) 'Current native identity changed.'
    Require ($boundary.CurrentMvids -contains $baseline.ProjectionBoundary.CurrentRuntimeMvid) 'Current CSharp runtime image changed.'
    Require (@($boundary.HistoricalProcessIds).Count -eq 2 -and $boundary.HistoricalProcessIds[0] -ne $boundary.HistoricalProcessIds[1]) 'Repeated historical execution not proven.'
    foreach ($pidValue in $boundary.HistoricalProcessIds) { $process = Get-Process -Id $pidValue -ErrorAction SilentlyContinue; Require ($null -eq $process -or $process.HasExited) 'Historical process remains alive.' }
    $tests = @('dispatch-final','b5-b8-boundaries','focused','broad-expanded','full') | ForEach-Object {
        $path = "artifacts/p5o2b8/tests/$_.trx"
        [xml]$trx = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $counts = $trx.TestRun.ResultSummary.Counters
        Require ([int]$counts.total -eq [int]$counts.passed -and [int]$counts.failed -eq 0 -and [int]$counts.notExecuted -eq 0) "Failed/skipped tests: $path"
        [ordered]@{Name=$_;Total=[int]$counts.total;Passed=[int]$counts.passed;Failed=[int]$counts.failed;Artifact=$path;Sha256=(Get-FileHash $path).Hash}
    }
    Require ($tests[0].Total -eq 37 -and $tests[1].Total -eq 133 -and $tests[2].Total -eq 278 -and $tests[3].Total -eq 2724 -and $tests[4].Total -eq 2814) 'Unexpected final test census.'
    [xml]$separate = Get-Content artifacts/p5o2b8/tests/b5-b8-boundaries.trx -Raw -Encoding UTF8
    # Enumerate each retained owner explicitly (no test replacement, no shared count assumption).
    $independent = foreach ($owner in @('ExceptionFlowAnalysisRouterTests','ExceptionFlowAnalyzerSelectionPolicyTests','ExceptionFlowAnalyzerSelectionProjectionTests','ExceptionFlowAnalysisDispatchTests')) {
        [ordered]@{Owner=$owner;Passed=@($separate.TestRun.Results.UnitTestResult | Where-Object { $_.testName.Contains($owner) -and $_.outcome -eq 'Passed' }).Count}
    }
    Require ($independent[0].Passed -eq 26 -and $independent[1].Passed -eq 40 -and $independent[2].Passed -eq 30 -and $independent[3].Passed -eq 37) 'Independent B5-B8 tests missing.'
    [xml]$gateTrx = Get-Content artifacts/dual-version-build/main-boundary.trx -Raw -Encoding UTF8
    Require ([int]$gateTrx.TestRun.ResultSummary.Counters.total -eq 212 -and [int]$gateTrx.TestRun.ResultSummary.Counters.passed -eq 212) 'Permanent gate missing B8.'
    [xml]$first = Get-Content artifacts/p5o2b8/tests/dispatch-first.trx -Raw -Encoding UTF8
    Require ([int]$first.TestRun.ResultSummary.Counters.failed -eq 9 -and [int]$first.TestRun.ResultSummary.Counters.total -eq 37) 'Initial fixture-correction evidence missing.'
    $logs = @($gate.Steps | ForEach-Object {
        Require ($_.ExitCode -eq 0) "Failed gate step: $($_.Name)"
        $path = 'artifacts/dual-version-build/' + $_.Name + '.log'
        $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        if ($_.Name.EndsWith('-build')) {
            Require ($content -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $content -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Build warnings/errors: $path"
        }
        Fingerprint $path
    })
    $standalone = @('current-first-build','tests-regression-build') | ForEach-Object {
        $path = "artifacts/p5o2b8/$_.log"
        $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ($content -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $content -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Standalone build warnings/errors: $path"
        Fingerprint $path
    }
    $owned = @('src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisDispatch.cs','src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisDispatchRequest.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs','build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md',
        'Evaluation/P5O2B8-integrated-analyzer-dispatch.md','Evaluation/P5O2B8Proof/Write-Verify-DispatchEvidence.ps1')
    $utf8 = New-Object Text.UTF8Encoding($false,$true)
    foreach ($path in $owned) {
        $content = $utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path $path).Path))
        Require ($content -notmatch '(?<!\r)\n' -and $content.EndsWith("`r`n")) "UTF8/CRLF failed: $path"
    }
    foreach ($path in @('build/Verify-DualVersionBuild.ps1','Evaluation/P5O2B8Proof/Write-Verify-DispatchEvidence.ps1')) {
        $errors = $null
        [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$errors) | Out-Null
        Require ($errors.Count -eq 0) "PowerShell parse failed: $path"
    }
    foreach ($file in $initial.Projects) { if ($file.Path.EndsWith('.csproj') -or $file.Path.EndsWith('.props')) { [xml](Get-Content -LiteralPath $file.Path -Raw -Encoding UTF8) | Out-Null } }
    $prior = $ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $format = dotnet format whitespace . --folder --include src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisDispatch.cs src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisDispatchRequest.cs Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs --verify-no-changes 2>&1 | Out-String; $formatCode=$LASTEXITCODE }
    finally { $ErrorActionPreference=$prior }
    $format | Set-Content artifacts/p5o2b8/format.log -Encoding UTF8
    Require ($formatCode -eq 0) 'Format failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git diff check failed.'
    Require ($baseline.Semantics.SelfAnalysis.Inherited -and $baseline.Semantics.SelfAnalysis.Findings -eq 16) 'Inherited SelfAnalysis missing.'
    Require ($baseline.Semantics.Canonical.Inherited -and $baseline.Semantics.Canonical.Added -eq 0 -and $baseline.Semantics.Canonical.Removed -eq 0 -and $baseline.Semantics.Canonical.ChangedEvidence -eq 0) 'Inherited canonical baseline missing.'
    $audit = [ordered]@{
        Schema='P5O2B8-integrated-analyzer-dispatch-v1';Date='2026-10-09';StartingHead=$initial.Head;Head=$head;Decision='Complete';ExternalHeadMovement=$false
        Owner='XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalysisDispatch';Entry='AnalyzeAsync(ExceptionFlowAnalysisDispatchRequest, CancellationToken)'
        Request=[ordered]@{Native='CurrentCompilation(CSharpCompilation, CurrentExceptionFlowAnalysisInput)';External='PortablePdb(ExternalCompilationProvenanceDescriptor, ExecutionPayload)';Payloads=@('CurrentSession(CurrentExceptionFlowAnalysisInput)','Worker(WorkerAnalysisInput)');ClosedPrivateBaseConstructors=$true;NullableBooleanGodApi=$false;RawVersionInput=$false;ExplicitEngineInput=$false;BothSourcesOrBothPayloadsRepresentable=$false;NullInvalidInputs='fail closed in unchanged existing layers'}
        Chain='trusted typed input -> B8 dispatch -> B7 projection -> B6 selection -> B5 router -> original Current session / B4 isolated Historical client'
        Stages=@(
            [ordered]@{Name='Projection';Input='existing B7 ProjectionInput from one request source';Output='original ProjectionResult / Context';Trust='native Current instance or actual existing target-bound PDB factory receipt';Error='original typed failure, context/decision/routing absent'},
            [ordered]@{Name='Selection';Input='successfully projected immutable context';Output='original SelectionDecision';Trust='unchanged exact B6 compiler/origin rule';Error='Unspecified + original typed failure, router not called'},
            [ordered]@{Name='Routing';Input='B6 enum + one supplied typed payload + same cancellation token';Output='original RoutingResult';Trust='unchanged B5 branch/payload checks and B4 Worker validation/import';Error='original routing/Worker failures; no result, repair or fallback'})
        CallerTrustPreconditions='Existing Current compilation/member/session association and prepared external payload remain caller responsibilities; compiler provenance is not full PDB target source/reference/options equivalence.'
        Current='original caller-owned sequential session, graph lifetime and uncertainty unchanged';Historical='existing B4 client only, exact proven isolated engine, original profile and process cleanup';NoCurrentFallback=$true
        ProductionChanges='two new orchestration/request files only; no existing Main source modified';LowerBoundariesUnchanged=$true;AnalyzerSemanticsChanged=$false;VersionsChanged=$false;RouterChanged=$false;ProductionCallSiteMigration=$false
        Boundary=$boundary;ActualHistoricalEmission=$emission;Tests=$tests;IndependentBoundaryTests=$independent;PermanentGate=[ordered]@{Passed=$true;Tests=212;Audit=(Fingerprint 'artifacts/dual-version-build/audit.json');BuildImages=6;WorkerBuildImages=2;SharedSources=121;HostSources=1;RuntimeExecuted=$true;BuildLogFingerprints=$logs};StandaloneBuildLogs=@($standalone)
        Attempts=[ordered]@{FirstTestBuildErrors=6;SecondTestBuildErrors=1;FirstFocusedTotal=37;FirstFocusedPassed=28;FirstFocusedFailed=9;FirstFocused=(Fingerprint 'artifacts/p5o2b8/tests/dispatch-first.trx');Corrections=@('new test evidence expression extra parenthesis','new test referenced wrong existing validation enum name','new adversarial fixture path corrected to existing B4 test-process output','new reflection guard includes public primary constructors of internal types','new test initializer whitespace corrected');ProductionWorkaround=$false;FinalFocusedPassed=37}
        Quality=[ordered]@{FolderOnlyFormat=$true;StrictUtf8CrLf=$true;PowerShellXmlAndJsonParsed=$true;GitDiffCheck=$true};OwnedFileFingerprints=@($owned | ForEach-Object { Fingerprint $_ })
        Semantics=[ordered]@{ExistingMainSourcesUnchanged=370;ExistingCoreFilesUnchanged=23;IncludesAllHistoricallyProtected22CoreFiles=$true;ExistingBoundaryFilesUnchanged=$initial.BoundaryFiles.Count;ExistingWorkerTestFilesUnchanged=$initial.ExistingWorkerTests.Count;SharedAnalyzerSourcesUnchanged=121;SelfAnalysis=$baseline.Semantics.SelfAnalysis;Canonical=$baseline.Semantics.Canonical;FreshSelfAnalysisRun=$false;FreshCanonicalFindingDiffRun=$false;BaselineSource=(Fingerprint 'Evaluation/P5O2B7-trusted-selection-context-projection-audit.json');InheritedReason='Only new orchestration; all existing Analyzer/session/policy/projection/router/client and Core bytes unchanged. No fresh analysis of B8 documentation claimed.'}
        Open='CLI/reporting/Dapper adoption, full external execution-input/equivalence projection, discovery and additional Worker profiles/Historical versions remain open; no pipeline migration claimed.'
        Protected=[ordered]@{RootIgnore=$initial.RootIgnore;CoreFiles=$initial.CoreFiles;Stashes=$initial.Stashes;Projects=$initial.Projects;StartingSnapshot=(Fingerprint 'artifacts/p5o2b8/protected-before.json')}
        Git=[ordered]@{InitialStatus=$initial.InitialStatus;Status=@(git status --short);AgentCommit=$false;AgentPush=$false;AgentIndexChanged=$false;AgentStashChanged=$false}
    }
    $audit | ConvertTo-Json -Depth 30 | Set-Content artifacts/p5o2b8/dispatch-audit.json -Encoding UTF8
    Write-Host 'PASS: integrated B7/B6/B5 dispatch, real Current/Historical parity, failure cleanup, regressions/build/isolation/quality and protected state.'
} finally { Pop-Location }
