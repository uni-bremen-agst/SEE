$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
    function Fingerprint([string]$Path) { [ordered]@{ Path=$Path; Sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash } }
    $initial = Get-Content artifacts/p5o2b5/protected-before.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $b4 = Get-Content Evaluation/P5O2B4-main-worker-analysis-boundary-audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $gate = Get-Content artifacts/dual-version-build/audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $runtime = Get-Content artifacts/p5o2b5/routing-runtime-parity.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $head = git rev-parse HEAD
    Require ($head -eq $initial.Head) 'Unreviewed external HEAD change.'
    Require ((Get-FileHash ../../.gitignore).Hash -eq $initial.RootIgnore) 'Protected root ignore changed.'
    Require ((Get-FileHash XMLDocNormalizer.sln).Hash -eq $initial.Solution) 'Solution changed.'
    Require ((Get-FileHash src/XMLDocNormalizer/XMLDocNormalizer.csproj).Hash -eq $initial.MainProject) 'Main project changed.'
    Require ((Get-FileHash build/ExceptionFlow.HistoricalSources.props).Hash -eq $initial.Manifest) 'Shared source manifest changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -eq ($initial.Stashes -join "`n")) 'Protected stashes changed.'
    $core = @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -File -Recurse | Sort-Object FullName)
    Require ($core.Count -eq $initial.CoreFiles.Count) 'Core file set changed.'
    Require ($initial.MainSources.Count -eq 365 -and $initial.BoundaryFiles.Count -eq 13) 'Wrong original source census.'
    foreach ($file in @($initial.CoreFiles) + @($initial.MainSources) + @($initial.BoundaryFiles)) {
        Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Protected/existing file changed: $($file.Path)"
    }
    Require ($gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Expanded permanent gate incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Historical source boundary changed.'
    foreach ($file in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Shared source changed: $($file.Path)"
    }
    Require ($runtime.canonicalParity -and $runtime.currentUsesExistingSession -and $runtime.historicalUsesB4Client -and $runtime.processStopped -and -not $runtime.historicalLoadedInCaller) 'Missing routed runtime proof.'
    Require ($runtime.historical.protocolVersion -eq 2) 'Worker protocol changed.'
    foreach ($engine in $runtime.current) {
        Require ($engine.informationalVersion.StartsWith('5.0.0-2.25567.12+')) 'Wrong Current runtime.'
        Require ($gate.CurrentCompilerReferences.Sha256 -contains $engine.sha256) 'Current runtime differs from compile references.'
    }
    foreach ($engine in $runtime.historical.loadedRoslyn) {
        Require ($engine.informationalVersion.StartsWith('5.0.0-2.25451.107+')) 'Wrong Historical runtime.'
        Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.sha256) 'Historical runtime differs from compile references.'
    }
    $tests = @('router-final', 'worker-architecture', 'broad', 'full') | ForEach-Object {
        $path = "artifacts/p5o2b5/tests/$_.trx"
        [xml]$trx = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $counts = $trx.TestRun.ResultSummary.Counters
        Require ([int]$counts.total -eq [int]$counts.passed -and [int]$counts.failed -eq 0) "Tests failed/skipped: $path"
        [ordered]@{ Name=$_; Total=[int]$counts.total; Passed=[int]$counts.passed; Failed=[int]$counts.failed; Artifact=$path; Sha256=(Get-FileHash $path).Hash }
    }
    Require ($tests[0].Total -eq 26 -and $tests[1].Total -eq 169 -and $tests[2].Total -eq 1913 -and $tests[3].Total -eq 2705) 'Unexpected final regression census.'
    [xml]$gateTrx = Get-Content artifacts/dual-version-build/main-boundary.trx -Raw -Encoding UTF8
    Require ([int]$gateTrx.TestRun.ResultSummary.Counters.total -eq 105 -and [int]$gateTrx.TestRun.ResultSummary.Counters.passed -eq 105) 'Routing tests missing from permanent gate.'
    $logs = @($gate.Steps | ForEach-Object {
        $path = 'artifacts/dual-version-build/' + $_.Name + '.log'
        $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ($_.ExitCode -eq 0) "Failed gate step: $($_.Name)"
        if ($_.Name.EndsWith('-build')) {
            Require ($content -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $content -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Build warnings/errors: $path"
        }
        Fingerprint $path
    })
    $standaloneBuilds = @('current-first-build', 'tests-first-build') | ForEach-Object {
        $path = "artifacts/p5o2b5/$_.log"
        $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ($content -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $content -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Initial standalone build warnings/errors: $path"
        Fingerprint $path
    }
    $owned = @('src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisRouting.cs',
        'src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisRouter.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs',
        'build/Verify-DualVersionBuild.ps1', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md',
        'Evaluation/P5O2B5-explicit-analyzer-routing.md', 'Evaluation/P5O2B5Proof/Write-Verify-RoutingEvidence.ps1')
    $utf8 = New-Object Text.UTF8Encoding($false, $true)
    foreach ($path in $owned) {
        $content = $utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        Require ($content -notmatch '(?<!\r)\n' -and $content.EndsWith("`r`n")) "UTF8/CRLF/final newline failed: $path"
    }
    foreach ($path in @('build/Verify-DualVersionBuild.ps1', 'Evaluation/P5O2B5Proof/Write-Verify-RoutingEvidence.ps1')) {
        $parseErrors = $null
        [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path, [ref]$null, [ref]$parseErrors) | Out-Null
        Require ($parseErrors.Count -eq 0) "PowerShell parse failed: $path"
    }
    $priorErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $format = & dotnet format whitespace . --folder --include src/XMLDocNormalizer/Execution/Analysis Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs --verify-no-changes 2>&1 | Out-String
        $formatExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $priorErrorAction }
    $format | Set-Content artifacts/p5o2b5/format.log -Encoding UTF8
    Require ($formatExit -eq 0) 'Folder-only format failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'git diff --check failed.'
    Require ($b4.Semantics.SelfAnalysis.Inherited -and $b4.Semantics.SelfAnalysis.Findings -eq 16) 'Inherited self-analysis evidence missing.'
    Require ($b4.Semantics.Canonical.Inherited -and $b4.Semantics.Canonical.Added -eq 0 -and $b4.Semantics.Canonical.Removed -eq 0 -and $b4.Semantics.Canonical.ChangedEvidence -eq 0) 'Inherited canonical evidence missing.'
    $audit = [ordered]@{
        Schema='P5O2B5-explicit-analyzer-routing-v1'; Date='2026-10-08'; StartingHead=$initial.Head; Head=$head; Decision='Complete'; ExternalHeadChange=$false
        MainOwner='XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalysisRouter'; Entry='AnalyzeAsync(ExceptionFlowAnalysisRoutingRequest, CancellationToken)'
        Selection=[ordered]@{ Type='ExceptionFlowAnalyzerSelection'; Unspecified=0; Current=1; Historical=2; DefaultIsFailure=$true; AutomaticSelection=$false; InconsistentInputsRejected=$true; Fallback=$false }
        Current=[ordered]@{ Entry='existing caller-owned ExceptionFlowSummaryAnalysisSession.Analyze(member)'; Adapter='existing RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult'; LifetimeUnchanged=$true; UsesWorker=$false; UncertaintiesPreserved=$true; LocalRoslynInputNeverSerialized=$true; Cancellation='checked before the existing synchronous sequential analysis, not mid-call preemption' }
        Historical=[ordered]@{ Entry='unchanged HistoricalWorkerClient.AnalyzeAsync'; Outcome='original HistoricalWorkerCallResult and failure objects retained'; Protocol=2; NewIpcImplementation=$false; LoadsHistoricalInMain=$false }
        RoutedEndToEnd=$runtime; ExpandedExistingGate=$gate; BuildLogFingerprints=$logs; InitialStandaloneBuildLogFingerprints=@($standaloneBuilds); Tests=$tests
        IntegrationGateTests=[ordered]@{Passed=105; Artifact='artifacts/dual-version-build/main-boundary.trx'; Sha256=(Get-FileHash artifacts/dual-version-build/main-boundary.trx).Hash}
        OwnedFileFingerprints=@($owned | ForEach-Object { Fingerprint $_ })
        Quality=[ordered]@{StrictUtf8CrLf=$true; PowerShellParse=$true; JsonParsed=$true; FolderOnlyFormat=$true; GitDiffCheck=$true}
        LocalCorrections=@('First format gate rejected one new test initializer line; mechanical formatter corrected it. Final permanent gate and regression series rerun; no Analyzer or routing semantics changed.')
        Semantics=[ordered]@{OriginalMainSourcesUnchanged=365; SharedHistoricalSourcesUnchanged=121; ExistingBoundaryFilesUnchanged=13; AnalyzerSemanticsChanged=$false; SelfAnalysis=$b4.Semantics.SelfAnalysis; Canonical=$b4.Semantics.Canonical; FreshSelfAnalysisRun=$false; FreshCanonicalFindingDiffRun=$false; InheritedReason='B5 only adds explicit routing/composition/tests; all 365 pre-existing Main sources and 121 shared Analyzer sources remain byte-identical. No fresh self-analysis of new routing documentation is claimed.'; SourceEvidence='Evaluation/P5O2B4-main-worker-analysis-boundary-audit.json'; SourceEvidenceSha256=(Get-FileHash Evaluation/P5O2B4-main-worker-analysis-boundary-audit.json).Hash}
        RemainingBoundary='Automatic policy/version selection, CLI/Detector/reporting adoption, validated full external documents/references/options/supporting-source projection remain open. No Dapper/S1 pipeline advancement.'
        Protected=[ordered]@{Unchanged=$true; RootIgnore=$initial.RootIgnore; CoreFiles=$initial.CoreFiles; Stashes=$initial.Stashes; MainProject=$initial.MainProject; Solution=$initial.Solution; Manifest=$initial.Manifest; StartingSnapshot=(Fingerprint 'artifacts/p5o2b5/protected-before.json')}
        Git=[ordered]@{InitialStatus=$initial.InitialStatus; Status=@(git status --short); AgentCommit=$false; AgentPush=$false; AgentStashChanged=$false; AgentIndexChanged=$false; ExternalCommit=$false}
    }
    $audit | ConvertTo-Json -Depth 30 | Set-Content artifacts/p5o2b5/explicit-routing-audit.json -Encoding UTF8
    Write-Host 'PASS: explicit routing/parity/isolation/fail-closed, regression/build/quality gates and protected state.'
} finally { Pop-Location }
