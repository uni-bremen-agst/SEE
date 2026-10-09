$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
    function Fingerprint([string]$Path) { [ordered]@{ Path=$Path; Sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash } }
    $initial = Get-Content artifacts/p5o2b7/protected-before.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $b6 = Get-Content Evaluation/P5O2B6-deterministic-analyzer-selection-audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $gate = Get-Content artifacts/dual-version-build/audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $projection = Get-Content artifacts/p5o2b7/projection-boundary.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $emission = Get-Content artifacts/p5o2b7/historical-pdb/emission-evidence.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $head = git rev-parse HEAD
    Require ($head -eq $initial.Head) 'Unreviewed external HEAD change.'
    Require ((Get-FileHash ../../.gitignore).Hash -eq $initial.RootIgnore) 'Protected root ignore changed.'
    Require ((Get-FileHash XMLDocNormalizer.sln).Hash -eq $initial.Solution) 'Solution changed.'
    Require ((Get-FileHash src/XMLDocNormalizer/XMLDocNormalizer.csproj).Hash -eq $initial.MainProject) 'Main project changed.'
    Require ((Get-FileHash build/ExceptionFlow.HistoricalSources.props).Hash -eq $initial.Manifest) 'Shared source manifest changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -eq ($initial.Stashes -join "`n")) 'Protected stashes changed.'
    $core = @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -File -Recurse | Sort-Object FullName)
    Require ($core.Count -eq $initial.CoreFiles.Count) 'Core file set changed.'
    Require ($initial.MainSources.Count -eq 369 -and $initial.BoundaryFiles.Count -eq 13) 'Wrong original source census.'
    $approvedChanges = @((Join-Path $repoRoot 'src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionContext.cs'),
        (Join-Path $repoRoot 'src/XMLDocNormalizer/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactory.cs'))
    foreach ($file in @($initial.CoreFiles) + @($initial.MainSources) + @($initial.BoundaryFiles)) {
        if ($approvedChanges -notcontains $file.Path) {
            Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Protected/existing file changed: $($file.Path)"
        }
    }
    Require ((Get-FileHash -LiteralPath $initial.SelectionPolicy.Path).Hash -eq $initial.SelectionPolicy.Sha256) 'Exact B6 policy changed.'
    Require ((Get-FileHash Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj).Hash -eq $initial.TestProject) 'Test project changed.'
    Require ((Get-FileHash -LiteralPath $initial.ExistingRoutingTests.Path).Hash -eq $initial.ExistingRoutingTests.Sha256) 'Existing B5 routing tests changed.'
    # Prove the existing validator/parser text is unchanged apart from the receipt insertion.
    $factoryPath = 'Tools/XMLDocNormalizer/src/XMLDocNormalizer/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactory.cs'
    $originalFactory = @(git show "${head}:$factoryPath") -join "`n"
    $receiptBlock = @(
        '        /// <summary>Weak, Main-local receipts for actual successful validation outputs; never serialized or caller-minted.</summary>',
        '        private static readonly ConditionalWeakTable<ExternalCompilationProvenanceDescriptor, object> validationReceipts = new();',
        '', '        /// <summary>Checks validation ownership by reference without repeating PE/PDB/CDI validation.</summary>',
        '        internal static bool IsValidated(ExternalCompilationProvenanceDescriptor? descriptor)',
        '            => descriptor != null && validationReceipts.TryGetValue(descriptor, out _);', '') -join "`n"
    $expectedFactory = $originalFactory.Replace('using System.Runtime.InteropServices;', "using System.Runtime.CompilerServices;`nusing System.Runtime.InteropServices;")
    $factoryHeader = "    internal static class ExternalCompilationProvenanceDescriptorFactory`n    {`n"
    $expectedFactory = $expectedFactory.Replace($factoryHeader, $factoryHeader + $receiptBlock + "`n")
    $expectedFactory = $expectedFactory.Replace("                    metadataReferences);`n                return true;", "                    metadataReferences);`n                validationReceipts.Add(descriptor, new object());`n                return true;")
    $actualFactory = (Get-Content src/XMLDocNormalizer/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactory.cs -Raw -Encoding UTF8).Replace("`r`n", "`n")
    Require ($actualFactory.TrimEnd() -ceq $expectedFactory.TrimEnd()) 'Existing validator/parser changed beyond the reviewed receipt insertion.'
    Require ($gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Expanded permanent gate incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Historical source boundary changed.'
    foreach ($file in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Shared source changed: $($file.Path)"
    }
    Require ($projection.CurrentDecision -eq 'Current' -and $projection.CurrentPdbDecision -eq 'Current' -and $projection.HistoricalDecision -eq 'Historical') 'Wrong actual projected selections.'
    Require ($projection.CurrentCompilation.Provenance -eq 1 -and $projection.CurrentPdb.Provenance -eq 2 -and $projection.HistoricalPdb.Provenance -eq 2) 'Projection loses source origin.'
    Require ($projection.ExistingFactoryReceiptVerified -and $projection.HistoricalValidationKind -eq 'IdentityAndChecksum') 'Missing actual target-bound PDB validation.'
    Require (($projection.ProjectorSelects -eq $false) -and ($projection.ProjectorExecutes -eq $false) -and ($projection.ProjectorStartsWorker -eq $false) -and ($projection.PolicyExecutes -eq $false) -and ($projection.HistoricalLoadedInCaller -eq $false)) 'Projection/selection executed or loaded Historical.'
    Require ($projection.CurrentCompilation.CompilerVersion -eq $projection.CurrentRuntimeInformationalVersion -and $projection.CurrentPdb.CompilerVersion -eq $projection.CurrentRuntimeInformationalVersion) 'Current identity mapping changed.'
    Require ($emission.TestOnly -and ($emission.SubmittedSourceAccepted -eq $false) -and ($emission.EmittedSourceExecuted -eq $false)) 'Historical PDB fixture is not isolated test-only emission.'
    Require ($emission.CompilerRuntime.Count -eq 2) 'Historical fixture engine evidence missing.'
    foreach ($engine in $emission.CompilerRuntime) {
        Require ($engine.InformationalVersion -eq $projection.HistoricalPdb.CompilerVersion) 'PDB recorded build differs from actual emitting compiler.'
        Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256) 'Fixture runtime differs from proven Historical image.'
    }
    Require ((Get-FileHash -LiteralPath $projection.HistoricalImagePath).Hash -eq $projection.HistoricalPeSha256 -and $emission.PeSha256 -eq $projection.HistoricalPeSha256) 'Historical PE fixture changed.'
    Require ((Get-FileHash -LiteralPath ([IO.Path]::ChangeExtension($projection.HistoricalImagePath, '.pdb'))).Hash -eq $projection.HistoricalPdbSha256 -and $emission.PdbSha256 -eq $projection.HistoricalPdbSha256) 'Historical PDB fixture changed.'
    $tests = @('projection-final', 'worker-architecture', 'broad', 'full-final', 'provenance-regressions') | ForEach-Object {
        $path = "artifacts/p5o2b7/tests/$_.trx"
        [xml]$trx = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $counts = $trx.TestRun.ResultSummary.Counters
        Require ([int]$counts.total -eq [int]$counts.passed -and [int]$counts.failed -eq 0) "Tests failed/skipped: $path"
        [ordered]@{ Name=$_; Total=[int]$counts.total; Passed=[int]$counts.passed; Failed=[int]$counts.failed; Artifact=$path; Sha256=(Get-FileHash $path).Hash }
    }
    Require ($tests[0].Total -eq 30 -and $tests[1].Total -eq 239 -and $tests[2].Total -eq 1983 -and $tests[3].Total -eq 2775 -and $tests[4].Total -eq 224) 'Unexpected final regression census.'
    [xml]$initialFocused = Get-Content artifacts/p5o2b7/tests/projection-first.trx -Raw -Encoding UTF8
    Require ([int]$initialFocused.TestRun.ResultSummary.Counters.total -eq 95 -and [int]$initialFocused.TestRun.ResultSummary.Counters.failed -eq 2) 'Initial test fixture correction evidence missing.'
    [xml]$gateTrx = Get-Content artifacts/dual-version-build/main-boundary.trx -Raw -Encoding UTF8
    Require ([int]$gateTrx.TestRun.ResultSummary.Counters.total -eq 175 -and [int]$gateTrx.TestRun.ResultSummary.Counters.passed -eq 175) 'Selection/routing tests missing from permanent gate.'
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
        $path = "artifacts/p5o2b7/$_.log"
        $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ($content -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $content -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Initial standalone build warnings/errors: $path"
        Fingerprint $path
    }
    $owned = @('src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionContext.cs',
        'src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionContext.Projector.cs',
        'src/XMLDocNormalizer/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactory.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionProjectionTests.cs',
        'build/Verify-DualVersionBuild.ps1', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md',
        'Evaluation/P5O2B7-trusted-selection-context-projection.md', 'Evaluation/P5O2B7Proof/Write-Verify-ProjectionEvidence.ps1')
    $owned += @(Get-ChildItem Evaluation/P5O2B7Proof/HistoricalPdbFixture -File | ForEach-Object FullName)
    $utf8 = New-Object Text.UTF8Encoding($false, $true)
    foreach ($path in $owned) {
        $content = $utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        if ($path.EndsWith('.csproj') -or $path.EndsWith('.props')) { [xml](Get-Content -LiteralPath $path -Raw -Encoding UTF8) | Out-Null }
        Require ($content -notmatch '(?<!\r)\n' -and $content.EndsWith("`r`n")) "UTF8/CRLF/final newline failed: $path"
    }
    foreach ($path in @('build/Verify-DualVersionBuild.ps1', 'Evaluation/P5O2B7Proof/Write-Verify-ProjectionEvidence.ps1')) {
        $parseErrors = $null
        [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path, [ref]$null, [ref]$parseErrors) | Out-Null
        Require ($parseErrors.Count -eq 0) "PowerShell parse failed: $path"
    }
    $priorErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $format = & dotnet format whitespace . --folder --include src/XMLDocNormalizer/Execution/Analysis src/XMLDocNormalizer/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactory.cs Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionProjectionTests.cs Evaluation/P5O2B7Proof/HistoricalPdbFixture --verify-no-changes 2>&1 | Out-String
        $formatExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $priorErrorAction }
    $format | Set-Content artifacts/p5o2b7/format.log -Encoding UTF8
    Require ($formatExit -eq 0) 'Folder-only format failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'git diff --check failed.'
    Require ($b6.Semantics.SelfAnalysis.Inherited -and $b6.Semantics.SelfAnalysis.Findings -eq 16) 'Inherited self-analysis evidence missing.'
    Require ($b6.Semantics.Canonical.Inherited -and $b6.Semantics.Canonical.Added -eq 0 -and $b6.Semantics.Canonical.Removed -eq 0 -and $b6.Semantics.Canonical.ChangedEvidence -eq 0) 'Inherited canonical evidence missing.'
    $audit = [ordered]@{
        Schema='P5O2B7-trusted-selection-context-projection-v1'; Date='2026-10-09'; StartingHead=$initial.Head; Head=$head; Decision='Complete'; ExternalHeadChange=$false
        ProjectionOwner='XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalyzerSelectionContext.Projector'; Entry='Project(ExceptionFlowAnalyzerSelectionProjectionInput)'
        Input=[ordered]@{TypedCurrent='existing Main Current CSharpCompilation'; TypedExternal='existing ExternalCompilationProvenanceDescriptor bearing actual factory receipt'; RawStringInput=$false; OneSourceRequired=$true; BothSourcesRejected=$true}
        Trust=[ordered]@{ContextConstructor='private'; EvidenceProperties='get-only'; PdbReceipt='private ConditionalWeakTable in existing validator, successful output reference only'; RepeatedValidation=$false; NewParser=$false; ExistingValidatorParserTextUnchangedBeyondReceipt=$true; StrongDescriptorRetention=$false; ExpectedPeRoot='existing P4A validated expected debug descriptor remains caller trust precondition'; ReceiptSerialized=$false; ReflectionSandbox=$false}
        Mapping=[ordered]@{Current='existing loaded runtime type informational version -> CurrentCompilation'; Pdb='exact stored schema-2 CSharp compiler-version -> ValidatedPortablePdb'; SelectionDoneByProjector=$false; VersionNormalization=$false; UnsupportedVersion='projects unchanged if actual validated provenance; unchanged B6 policy rejects it'; MultipleSources='typed failure without preference even when values agree'; FailureCodes=@('MissingInput','ConflictingInputs','UnvalidatedProvenance','MissingCompilationOptions','UnsupportedCompilationOptions','MissingCompilerVersion','MissingCompilerIdentity'); FailureContext=$null}
        Direction='typed input/provenance -> projection -> immutable B6 context -> unchanged B6 selection policy -> unchanged B5 router -> original execution'
        ExactB6RulesUnchanged=$true; RouterUnchanged=$true; AnalyzerSemanticsChanged=$false; ProductionCallSitesMigrated=$false
        ProjectionBoundary=$projection; HistoricalPdbEmission=$emission; ExpandedExistingGate=$gate; BuildLogFingerprints=$logs; InitialStandaloneBuildLogFingerprints=@($standaloneBuilds); Tests=$tests
        IntegrationGateTests=[ordered]@{Passed=175; Artifact='artifacts/dual-version-build/main-boundary.trx'; Sha256=(Get-FileHash artifacts/dual-version-build/main-boundary.trx).Hash}
        LocalAttempts=[ordered]@{FirstFocused=[ordered]@{Total=95;Passed=93;Failed=2;Evidence=(Fingerprint 'artifacts/p5o2b7/tests/projection-first.trx')}; CorrectedFocused=[ordered]@{Total=96;Passed=96;Evidence=(Fingerprint 'artifacts/p5o2b7/tests/projection-corrected.trx')}; Corrections=@('identical source can produce the same PDB despite distinct assembly names; mismatch fixture now differs in source', 'Historical Analyzer build PDB records SDK compiler 4.11, not loaded Historical runtime 5.0; actual native Historical-emitted PE/PDB fixture added in isolated test-only process; no production workaround')}
        OwnedFileFingerprints=@($owned | ForEach-Object { Fingerprint $_ })
        Quality=[ordered]@{StrictUtf8CrLf=$true; PowerShellAndXmlParse=$true; JsonParsed=$true; FolderOnlyFormat=$true; GitDiffCheck=$true}
        Semantics=[ordered]@{ExistingMainSources=369; ExistingMainSourcesUnchanged=367; ApprovedTrustBoundaryChanges=@($approvedChanges | ForEach-Object { Fingerprint $_ }); SharedAnalyzerSourcesUnchanged=121; ExistingWorkerFilesUnchanged=13; B6SelectionPolicyUnchanged=$true; SelfAnalysis=$b6.Semantics.SelfAnalysis; Canonical=$b6.Semantics.Canonical; FreshSelfAnalysisRun=$false; FreshCanonicalFindingDiffRun=$false; InheritedReason='Only the context trust constructor and validator output receipt change; all Analyzer algorithms and exact B6 policy/B5 router remain byte-identical. No fresh self-analysis of new projection documentation is claimed.'; SourceEvidence='Evaluation/P5O2B6-deterministic-analyzer-selection-audit.json'; SourceEvidenceSha256=(Get-FileHash Evaluation/P5O2B6-deterministic-analyzer-selection-audit.json).Hash}
        RemainingBoundary='CLI/reporting/Dapper adoption, automatic artifact/Project discovery and validated full external input/equivalence/reporting projection remain open. Current native Compilation identity is not discovery of the original compiler of an external reconstruction.'
        Protected=[ordered]@{UnchangedFromB7Start=$true; RootIgnore=$initial.RootIgnore; CoreFiles=$initial.CoreFiles; Stashes=$initial.Stashes; MainProject=$initial.MainProject; TestProject=$initial.TestProject; Solution=$initial.Solution; Manifest=$initial.Manifest; ExistingRoutingTests=$initial.ExistingRoutingTests; ExactSelectionPolicy=$initial.SelectionPolicy; StartingSnapshot=(Fingerprint 'artifacts/p5o2b7/protected-before.json')}
        Git=[ordered]@{InitialStatus=$initial.InitialStatus; Status=@(git status --short); AgentCommit=$false; AgentPush=$false; AgentStashChanged=$false; AgentIndexChanged=$false; ExternalCommitDuringB7=$false; B6SeparatelyCommitted=$initial.Head}
    }
    $audit | ConvertTo-Json -Depth 30 | Set-Content artifacts/p5o2b7/trusted-projection-audit.json -Encoding UTF8
    Write-Host 'PASS: trusted projection, real validated Current/Historical PDBs, unchanged selection/routing, regression/build/quality gates and protected B7 state.'
} finally { Pop-Location }
