$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
    function Fingerprint([string]$Path) { [ordered]@{ Path=$Path; Sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash } }
    $initial = Get-Content artifacts/p5o2b6/protected-before.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $b5 = Get-Content Evaluation/P5O2B5-explicit-analyzer-routing-audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $gate = Get-Content artifacts/dual-version-build/audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $runtime = Get-Content artifacts/p5o2b6/selection-runtime-parity.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $head = git rev-parse HEAD
    Require ($head -eq $initial.Head) 'Unreviewed external HEAD change.'
    Require ((Get-FileHash ../../.gitignore).Hash -eq $initial.RootIgnore) 'Protected root ignore changed.'
    Require ((Get-FileHash XMLDocNormalizer.sln).Hash -eq $initial.Solution) 'Solution changed.'
    Require ((Get-FileHash src/XMLDocNormalizer/XMLDocNormalizer.csproj).Hash -eq $initial.MainProject) 'Main project changed.'
    Require ((Get-FileHash build/ExceptionFlow.HistoricalSources.props).Hash -eq $initial.Manifest) 'Shared source manifest changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -eq ($initial.Stashes -join "`n")) 'Protected stashes changed.'
    $core = @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -File -Recurse | Sort-Object FullName)
    Require ($core.Count -eq $initial.CoreFiles.Count) 'Core file set changed.'
    Require ($initial.MainSources.Count -eq 367 -and $initial.BoundaryFiles.Count -eq 13) 'Wrong original source census.'
    foreach ($file in @($initial.CoreFiles) + @($initial.MainSources) + @($initial.BoundaryFiles)) {
        Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Protected/existing file changed: $($file.Path)"
    }
    Require ((Get-FileHash -LiteralPath $initial.ExistingRoutingTests.Path).Hash -eq $initial.ExistingRoutingTests.Sha256) 'Existing B5 routing tests changed.'
    Require ($gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Expanded permanent gate incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Historical source boundary changed.'
    foreach ($file in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Shared source changed: $($file.Path)"
    }
    Require ($runtime.canonicalParity -and $runtime.executedByExistingRouter -and ($runtime.policyExecutedAnalysis -eq $false) -and ($runtime.policyStartedWorker -eq $false) -and $runtime.processStopped -and ($runtime.historicalLoadedInCaller -eq $false)) 'Missing selected runtime proof.'
    Require ($runtime.currentDecision.selection -eq 'current' -and $runtime.historicalDecision.selection -eq 'historical') 'Wrong actual policy choices.'
    Require ($runtime.historical.protocolVersion -eq 2) 'Worker protocol changed.'
    foreach ($engine in $runtime.current) {
        Require ($engine.informationalVersion.StartsWith('5.0.0-2.25567.12+')) 'Wrong Current runtime.'
        Require ($gate.CurrentCompilerReferences.Sha256 -contains $engine.sha256) 'Current runtime differs from compile references.'
    }
    foreach ($engine in $runtime.historical.loadedRoslyn) {
        Require ($engine.informationalVersion.StartsWith('5.0.0-2.25451.107+')) 'Wrong Historical runtime.'
        Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.sha256) 'Historical runtime differs from compile references.'
    }
    $tests = @('selection-final', 'worker-architecture', 'broad', 'full-final') | ForEach-Object {
        $path = "artifacts/p5o2b6/tests/$_.trx"
        [xml]$trx = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $counts = $trx.TestRun.ResultSummary.Counters
        Require ([int]$counts.total -eq [int]$counts.passed -and [int]$counts.failed -eq 0) "Tests failed/skipped: $path"
        [ordered]@{ Name=$_; Total=[int]$counts.total; Passed=[int]$counts.passed; Failed=[int]$counts.failed; Artifact=$path; Sha256=(Get-FileHash $path).Hash }
    }
    Require ($tests[0].Total -eq 40 -and $tests[1].Total -eq 209 -and $tests[2].Total -eq 1953 -and $tests[3].Total -eq 2745) 'Unexpected final regression census.'
    [xml]$isolated = Get-Content artifacts/p5o2b6/tests/multimodule-isolated.trx -Raw -Encoding UTF8
    Require ([int]$isolated.TestRun.ResultSummary.Counters.total -eq 1 -and [int]$isolated.TestRun.ResultSummary.Counters.passed -eq 1) 'Known-flake isolated retry failed.'
    $initialFullAttempt = $null
    if (Test-Path artifacts/p5o2b6/tests/full.trx) {
        [xml]$firstFull = Get-Content artifacts/p5o2b6/tests/full.trx -Raw -Encoding UTF8
        $firstCounts = $firstFull.TestRun.ResultSummary.Counters
        $failures = @($firstFull.TestRun.Results.UnitTestResult | Where-Object outcome -eq 'Failed')
        Require ([int]$firstCounts.total -eq 2745 -and [int]$firstCounts.failed -eq 1 -and $failures.Count -eq 1 -and $failures[0].testName.EndsWith('.ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed')) 'Unreviewed initial full-suite failure.'
        $initialFullAttempt = [ordered]@{Total=2745; Passed=2744; Failed=1; Test=$failures[0].testName; Evidence=(Fingerprint 'artifacts/p5o2b6/tests/full.trx'); ForeignFixApplied=$false}
    }
    [xml]$gateTrx = Get-Content artifacts/dual-version-build/main-boundary.trx -Raw -Encoding UTF8
    Require ([int]$gateTrx.TestRun.ResultSummary.Counters.total -eq 145 -and [int]$gateTrx.TestRun.ResultSummary.Counters.passed -eq 145) 'Selection/routing tests missing from permanent gate.'
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
        $path = "artifacts/p5o2b6/$_.log"
        $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ($content -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $content -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Initial standalone build warnings/errors: $path"
        Fingerprint $path
    }
    $owned = @('src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionContext.cs',
        'src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionPolicy.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs',
        'build/Verify-DualVersionBuild.ps1', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md',
        'Evaluation/P5O2B6-deterministic-analyzer-selection.md', 'Evaluation/P5O2B6Proof/Write-Verify-SelectionEvidence.ps1')
    $utf8 = New-Object Text.UTF8Encoding($false, $true)
    foreach ($path in $owned) {
        $content = $utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        Require ($content -notmatch '(?<!\r)\n' -and $content.EndsWith("`r`n")) "UTF8/CRLF/final newline failed: $path"
    }
    foreach ($path in @('build/Verify-DualVersionBuild.ps1', 'Evaluation/P5O2B6Proof/Write-Verify-SelectionEvidence.ps1')) {
        $parseErrors = $null
        [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path, [ref]$null, [ref]$parseErrors) | Out-Null
        Require ($parseErrors.Count -eq 0) "PowerShell parse failed: $path"
    }
    $priorErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $format = & dotnet format whitespace . --folder --include src/XMLDocNormalizer/Execution/Analysis Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs --verify-no-changes 2>&1 | Out-String
        $formatExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $priorErrorAction }
    $format | Set-Content artifacts/p5o2b6/format.log -Encoding UTF8
    Require ($formatExit -eq 0) 'Folder-only format failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'git diff --check failed.'
    Require ($b5.Semantics.SelfAnalysis.Inherited -and $b5.Semantics.SelfAnalysis.Findings -eq 16) 'Inherited self-analysis evidence missing.'
    Require ($b5.Semantics.Canonical.Inherited -and $b5.Semantics.Canonical.Added -eq 0 -and $b5.Semantics.Canonical.Removed -eq 0 -and $b5.Semantics.Canonical.ChangedEvidence -eq 0) 'Inherited canonical evidence missing.'
    $audit = [ordered]@{
        Schema='P5O2B6-deterministic-analyzer-selection-v1'; Date='2026-10-09'; StartingHead=$initial.Head; Head=$head; Decision='Complete'; ExternalHeadChange=$false
        PolicyOwner='XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalyzerSelectionPolicy'; Entry='Select(ExceptionFlowAnalyzerSelectionContext)'; Context='typed trusted Main C# provenance kind and exact required compiler-version'
        PreImplementationRuleArtifact='Evaluation/P5O2B6-deterministic-analyzer-selection.md'
        Rule=[ordered]@{Comparison='StringComparison.Ordinal exact equality, no trimming/prefix/range/default/fallback'; CurrentCompiler='5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09'; HistoricalCompiler='5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0'; Current='CurrentCompilation or ValidatedPortablePdb plus exact Current compiler'; Historical='ValidatedPortablePdb plus exact Historical compiler'; Conflict='CurrentCompilation plus exact Historical compiler'; FailureSelection='Unspecified'; FailureCodes=@('MissingContext','UnsupportedProvenance','MissingCompilerVersion','UnsupportedCompilerVersion','ConflictingEvidence')}
        ContextProjection=[ordered]@{Entry='FromValidatedPortablePdb(existing ExternalCompilationProvenanceDescriptor)'; Requires='existing target-bound validated descriptor, compilation-options schema 2 and language C#'; PreservesExactCompilerVersion=$true; AcquiresArtifacts=$false; ReconstructsCompilation=$false; TrustBoundary='typed Main provenance is a caller precondition, not authentication of untrusted enum/string input'; ExternalEquivalenceProven=$false}
        Composition=[ordered]@{Direction='trusted evidence -> pure policy -> existing B5 enum -> unchanged B5 router -> existing Current session or unchanged B4 client'; PolicyStartsWorker=$false; PolicyAnalyzes=$false; PolicyLoadsRoslyn=$false; PolicyOwnsSessions=$false; ExistingRouterUnchanged=$true; ExistingB5TestsUnchanged=$true; ProductionCallSitesMigrated=$false; NoHistoricalDependencyInMain=$true; NoFallback=$true}
        SelectedEndToEnd=$runtime; ExpandedExistingGate=$gate; BuildLogFingerprints=$logs; InitialStandaloneBuildLogFingerprints=@($standaloneBuilds); Tests=$tests
        IntegrationGateTests=[ordered]@{Passed=145; Artifact='artifacts/dual-version-build/main-boundary.trx'; Sha256=(Get-FileHash artifacts/dual-version-build/main-boundary.trx).Hash}
        LocalAttempts=[ordered]@{InitialFull=$initialFullAttempt; KnownFlakeIsolated=(Fingerprint 'artifacts/p5o2b6/tests/multimodule-isolated.trx'); IsolatedPassed=1; UnchangedFullRetryPassed=2745; InitialVerifierParseFailure='PS5 does not continue an expression whose next line starts with -and; corrected to one expression, with no production change'; NativeStderr='Initial full test raised PS5 NativeCommandError after its real failure; retry captures stderr before checking the exit code'}
        OwnedFileFingerprints=@($owned | ForEach-Object { Fingerprint $_ })
        Quality=[ordered]@{StrictUtf8CrLf=$true; PowerShellParse=$true; JsonParsed=$true; FolderOnlyFormat=$true; GitDiffCheck=$true}
        Semantics=[ordered]@{OriginalMainSourcesUnchanged=367; SharedHistoricalSourcesUnchanged=121; ExistingBoundaryFilesUnchanged=13; AnalyzerSemanticsChanged=$false; SelfAnalysis=$b5.Semantics.SelfAnalysis; Canonical=$b5.Semantics.Canonical; FreshSelfAnalysisRun=$false; FreshCanonicalFindingDiffRun=$false; InheritedReason='B6 only adds pure selection/context projection/tests; all 367 pre-existing Main sources and 121 shared Analyzer sources remain byte-identical. No fresh self-analysis of new selection documentation is claimed.'; SourceEvidence='Evaluation/P5O2B5-explicit-analyzer-routing-audit.json'; SourceEvidenceSha256=(Get-FileHash Evaluation/P5O2B5-explicit-analyzer-routing-audit.json).Hash}
        RemainingBoundary='CLI/autoselection/reporting/Dapper adoption, automatic external Assembly/Project discovery and validated full external documents/references/options/supporting-source projection remain open. No additional Historical versions or external pipeline advancement.'
        Protected=[ordered]@{UnchangedFromB6Start=$true; RootIgnore=$initial.RootIgnore; CoreFiles=$initial.CoreFiles; CorePreexistingWip=@('Core obj AssemblyInfo.cs records committed B5 HEAD','Core obj AssemblyInfoInputs.cache changed before B6'); Stashes=$initial.Stashes; MainProject=$initial.MainProject; Solution=$initial.Solution; Manifest=$initial.Manifest; ExistingRoutingTests=$initial.ExistingRoutingTests; StartingSnapshot=(Fingerprint 'artifacts/p5o2b6/protected-before.json')}
        Git=[ordered]@{InitialStatus=$initial.InitialStatus; Status=@(git status --short); AgentCommit=$false; AgentPush=$false; AgentStashChanged=$false; AgentIndexChanged=$false; ExternalCommitDuringB6=$false; B5SeparatelyCommitted=$initial.Head}
    }
    $audit | ConvertTo-Json -Depth 30 | Set-Content artifacts/p5o2b6/deterministic-selection-audit.json -Encoding UTF8
    Write-Host 'PASS: deterministic selection, unchanged routing/parity/isolation, regression/build/quality gates and protected B6 starting state.'
} finally { Pop-Location }
