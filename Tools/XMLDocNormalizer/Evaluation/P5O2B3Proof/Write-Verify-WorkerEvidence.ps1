$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition, [string]$Message) {
        if (-not $Condition) { throw $Message }
    }
    $initial = Get-Content 'artifacts/p5o2b3/protected-before.json' -Raw -Encoding UTF8 | ConvertFrom-Json
    $gate = Get-Content 'artifacts/dual-version-build/audit.json' -Raw -Encoding UTF8 | ConvertFrom-Json
    $isolation = Get-Content 'artifacts/p5o2b3/current-runtime-isolation.json' -Raw -Encoding UTF8 | ConvertFrom-Json
    $a2Path = 'Evaluation/P5O2B1A2-runtime-await-call-site-contract-audit.json'
    $a2 = Get-Content $a2Path -Raw -Encoding UTF8 | ConvertFrom-Json
    Require ($gate.Passed -and $gate.RuntimeExecuted -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Incomplete B3 build/runtime gate.'
    Require ((git rev-parse HEAD) -eq $initial.Head) 'HEAD changed during B3.'
    Require ((Get-FileHash '../../.gitignore').Hash -eq $initial.IgnoreSha256) 'Protected root ignore changed.'
    Require ((Get-FileHash 'src/XMLDocNormalizer/XMLDocNormalizer.csproj').Hash -eq $initial.MainProjectSha256) 'Current project changed.'
    Require ((Get-FileHash 'XMLDocNormalizer.sln').Hash -eq $initial.SolutionSha256) 'Normal solution changed.'
    Require ((@(git stash list --format='%H %gs') -join "`n") -eq ($initial.Stashes -join "`n")) 'Stashes changed.'
    $coreFiles = @(Get-ChildItem 'src/XMLDocNormalizer.ExceptionFlow.Core' -File -Recurse | Where-Object {$_.FullName -match '[\\/](bin|obj)[\\/]'})
    Require ($coreFiles.Count -eq $initial.CoreFiles.Count) 'Core output file set changed.'
    foreach ($file in $initial.CoreFiles) {
        Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Protected Core file changed: $($file.Path)"
    }
    $productive = @($a2.OriginalMainSourceFingerprints) + @($a2.ApiSurface.ProductiveFiles)
    foreach ($file in $productive) {
        Require ((Get-FileHash -LiteralPath $file.File).Hash -eq $file.Sha256) "Shared productive source changed: $($file.File)"
    }
    Require (@($productive.File | Sort-Object -Unique).Count -eq 362) 'Expected 362 unchanged productive Current sources.'
    Require ((($gate.SharedSourceFingerprints.Path | Sort-Object) -join ';') -eq (($a2.ApiSurface.ProductiveFiles.File | Sort-Object) -join ';')) 'Historical shared boundary changed.'
    Require ($isolation.SeparateProcesses -and -not $isolation.HistoricalLoadedInCaller -and $isolation.CallerProcessId -ne $isolation.WorkerProcessId) 'Process isolation not established.'
    foreach ($name in @('CurrentCommon', 'CurrentCSharp')) {
        $engine = $isolation.$name
        Require ($engine.BeforeMvid -eq $engine.AfterMvid -and $engine.InformationalVersion.StartsWith('5.0.0-2.25567.12+')) 'Current runtime changed/mismatched.'
        Require ($gate.CurrentCompilerReferences.Sha256 -contains $engine.Sha256) 'Actual Current runtime differs from Current compile references.'
    }
    foreach ($engine in $isolation.WorkerIdentity.LoadedRoslyn) {
        Require ($engine.InformationalVersion.StartsWith('5.0.0-2.25451.107+')) 'Actual Worker runtime is not exact Historical.'
        Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256) 'Actual Worker native image differs from Historical references.'
    }
    $tests = @('worker-architecture', 'broad', 'full') | ForEach-Object {
        $path = "artifacts/p5o2b3/tests/$_.trx"
        [xml]$trx = Get-Content -LiteralPath $path -Raw
        $counters = $trx.TestRun.ResultSummary.Counters
        Require ([int]$counters.failed -eq 0 -and [int]$counters.total -eq [int]$counters.passed) "Test gate failed: $path"
        [ordered]@{ Name=$_; Total=[int]$counters.total; Passed=[int]$counters.passed; Failed=[int]$counters.failed; Artifact=$path; Sha256=(Get-FileHash $path).Hash }
    }
    Require ($tests[0].Total -eq 100 -and $tests[1].Total -eq 1844 -and $tests[2].Total -eq 2636) 'Unexpected test totals.'
    $logs = @($gate.Steps | ForEach-Object {
        $path = 'artifacts/dual-version-build/' + $_.Name + '.log'
        $text = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ($_.ExitCode -eq 0) "Failed step: $($_.Name)"
        if ($_.Name.EndsWith('-build')) {
            Require ($text -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $text -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Nonzero warnings/errors: $path"
        }
        [ordered]@{ Name=$_.Name; Artifact=$path; Sha256=(Get-FileHash $path).Hash }
    })
    $sources = @('src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs') + @(Get-ChildItem 'src/XMLDocNormalizer.HistoricalWorker' -Filter '*.cs' -File | ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1).Replace('\','/') })
    $fingerprints = @($sources | Sort-Object | ForEach-Object { [ordered]@{ Path=$_; Sha256=(Get-FileHash -LiteralPath $_).Hash } })
    $ownedText = @('src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs', 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj', 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj', 'Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/HistoricalAnalyzerBuildProjectTests.cs', 'build/Verify-DualVersionBuild.ps1', 'build/README.md', '../../.github/workflows/xml-doc-normalizer-dual-build.yml', 'Evaluation/P5O2B3Proof/Write-Verify-WorkerEvidence.ps1', 'Evaluation/P5O2B3-historical-worker-host.md', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md') + @(Get-ChildItem 'src/XMLDocNormalizer.HistoricalWorker', 'Tests/XMLDocNormalizerTests/Worker' -File | ForEach-Object {$_.FullName})
    $utf8 = New-Object Text.UTF8Encoding($false,$true)
    foreach ($path in $ownedText) {
        $text = $utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF-8/CRLF/final newline failed: $path"
    }
    foreach ($path in @('build/Verify-DualVersionBuild.ps1', 'Evaluation/P5O2B3Proof/Write-Verify-WorkerEvidence.ps1')) {
        $parseErrors = $null
        [Management.Automation.Language.Parser]::ParseFile((Resolve-Path -LiteralPath $path).Path,[ref]$null,[ref]$parseErrors) | Out-Null
        Require ($parseErrors.Count -eq 0) "PowerShell parse failed: $path"
    }
    foreach ($path in @('src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj', 'src/XMLDocNormalizer.HistoricalWorker/Directory.Build.props', 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj', 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj', 'build/ExceptionFlow.HistoricalSources.props')) {
        [xml](Get-Content -LiteralPath $path -Raw) | Out-Null
    }
    $format = & dotnet format whitespace . --folder --include src/XMLDocNormalizer.HistoricalWorker src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs Tests/XMLDocNormalizerTests/Worker Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/HistoricalAnalyzerBuildProjectTests.cs --verify-no-changes -v:minimal 2>&1 | Out-String
    Require ($LASTEXITCODE -eq 0) 'Scoped folder format failed.'
    $format | Set-Content 'artifacts/p5o2b3/format.log' -Encoding UTF8
    & git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace check failed.'
    $result = [ordered]@{
        Schema='P5O2B3-worker-host-audit-v1'; Date='2026-10-08'; Head=$initial.Head
        Decision='COMPLETE: Historical Analyzer executes reproducibly in its own process; Current/Historical Roslyn runtime universes isolated'
        WorkerProject='src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj'
        HistoricalProject='src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj'
        Integration=[ordered]@{ ReusesB2BuildConfiguration=$true; WorkerInNormalSolution=$false; MainRoutingImplemented=$false; HistoricalReferenceInCurrentProjects=$false; ProtocolSourceLinkedByTestsOnly=$true; WorkerRuntimeReference='Historical library only'; Ci='../../.github/workflows/xml-doc-normalizer-dual-build.yml'; RemoteCiExecuted=$false }
        Protocol=[ordered]@{ Version=1; WorkerVersion='1.0'; Transport='one UTF-8 JSON object until stdin EOF; one stdout JSON response; diagnostics on stderr'; Operations=@('identity','analyze'); ExitCodes=[ordered]@{Success=0;Failure=1}; Payload=@('source','typeMetadataName','methodName'); ResultOwner='existing CanonicalExceptionFlowAnalysisResult (same-source P5O1 domain)'; RoslynFreeContractGraphTestPassed=$true; FailureCodes=@('invalidRequest','unsupportedProtocolVersion','malformedInput','compilationFailure','analysisFailure','unexpectedWorkerFailure'); MaximumRequestCharacters=65536; MaximumSourceCharacters=32768; MaximumSyntaxNodes=8192; Root='one body-bearing ordinary static parameterless non-generic method in a source-owned non-generic type'; InputProfile='CSharp12 / net8-runtime-bounded-v1; seven framework-only references'; AbsolutePathsInStableContract=$false; RoslynObjectsInContract=$false }
        Smoke=[ordered]@{ Entry='ExceptionFlowSummaryAnalysisSession.AnalyzeSolutionTransitivelyThrownExceptions'; Adapter='RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult'; Fixture='Root -> Thrower -> throw null'; ProvenException='System.NullReferenceException'; PathKinds=@('MethodCall','ExplicitThrow'); FreshProcesses=2; FullStdoutEqual=$true; SourceCodeEmittedOrExecuted=$false; ActualAwaitUnavailableFailureTestPassed=$true; CatchTransferTestPassed=$true }
        DualBuildAndWorkerGate=$gate; ActualCallerAndChildRuntimeEvidence=$isolation; BuildLogFingerprints=$logs; Tests=@($tests); NewHostAndWorkerSourceFingerprints=$fingerprints
        QualityGates=[ordered]@{ StrictUtf8CrLfFinalNewline=$true; ScopedFolderFormat=$true; JsonAndXmlParse=$true; PowerShellParse=$true; GitDiffCheck=$true; FormatLog='artifacts/p5o2b3/format.log' }
        StateLifetime=[ordered]@{ Model='one owned Compilation/tree/SemanticModel/scope and SummarySession per request/process'; WeakCaches=@('ContextualFactEvaluator conditional-weak-table value facts by SemanticModel','DereferenceFactDiscovery successful dereference facts by SemanticModel','DataFlowFactsProvider static cache with weak SemanticModel partitions and exact region keys'); OtherStatics='unchanged stateless RuntimeAwait cached getter, neutral comparers/immutable sentinels'; AlgorithmCachesRedesigned=$false; SessionGraphAndTraversal='same existing graph/SCC/cache/guard behavior; new process lifecycle only'; CrossRequestState='none: one request until EOF, then exit; two JSON frames rejected'; SymbolOrAnalyzerStateSerialized=$false }
        CurrentSemantics=[ordered]@{ ProductiveCurrentSourcesReverified=362; SharedHistoricalSourcesUnchanged=121; RuntimeAwaitCapabilityUnchanged=$true; A2Evidence=$a2Path; A2EvidenceSha256=(Get-FileHash $a2Path).Hash; SelfAnalysis=[ordered]@{Inherited=$true;Findings=16;DOC610=0;DOC611=1;DOC631=15;DOC632=0;Evidence=$a2.Validation.SelfAnalysis}; Canonical=[ordered]@{Inherited=$true;Added=0;Removed=0;ChangedEvidence=0;FullRawArraysEqual=$a2.Validation.Canonical.FullRawArraysEqual;FullNormalizedArraysEqual=$a2.Validation.Canonical.FullNormalizedArraysEqual;NormalizedHash=$a2.Validation.Canonical.AfterNormalizedHash}; InheritanceReason='B3 section 17: no productive shared Analyzer source change; Current Main source/project/solution unchanged. Historical host/protocol/orchestration are the authorized B3 additions, not an algorithm fork.' }
        LocalCorrections=@('Repeated identical TPA corelib path canonicalized/deduplicated; distinct ambiguous profile files still fail closed','Initial await test expected property name rather than the actual unchanged existing uncertainty text; assertion corrected, no Analyzer change','Original B2 rejecting host retained uncompiled as evidence; explicit Compile now selects the real Historical host','UTF-8 decoding and response serialization failures receive structured fail-closed responses; diagnostics stderr only')
        RemainingBoundary='No Main-process worker selection/routing; no external artifact/manifest acquisition, exact external reference reconstruction, full reporting/check routing, canonical import policy or worker pool. Bounded one-source endpoint is not a general external compilation workflow.'
        NextStep='P5O2B4 - Main-Process/Worker Integration and Final Roslyn-free Analysis Boundary, reusing proven permanent build/runtime isolation and existing canonical domain; no repeated compile/isolation proof package'
        Protected=[ordered]@{ Unchanged=$true; RootIgnore=$initial.IgnoreSha256; CoreFiles=$initial.CoreFiles; Stashes=$initial.Stashes; MainProject=$initial.MainProjectSha256; Solution=$initial.SolutionSha256 }
        Git=[ordered]@{ Status=@(git status --short); Commit=$false;Push=$false;IndexChanged=$false;StashChanged=$false }
    }
    $result | ConvertTo-Json -Depth 30 | Set-Content 'artifacts/p5o2b3/worker-host-audit.json' -Encoding UTF8
    Write-Host 'PASS: 362 unchanged productive sources; exact separate runtime worlds; eight build pairs; tests 100/1844/2636; protected state unchanged.'
} finally {
    Pop-Location
}
