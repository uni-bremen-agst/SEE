$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
    function Fingerprint([string]$Path) { [ordered]@{ Path=$Path; Sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash } }
    $initial = Get-Content artifacts/p5o2b4/protected-before.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $b3 = Get-Content Evaluation/P5O2B3-historical-worker-host-audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $gate = Get-Content artifacts/dual-version-build/audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $runtime = Get-Content artifacts/p5o2b4/main-runtime-parity.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $finalHead = git rev-parse HEAD
    # Preserve/audit the observed external case-only commit; never rewrite the original snapshot.
    Require ($finalHead -eq '3962615c84b14ca7ac9d2ed1f1afe099abb61c14') 'Unreviewed HEAD change.'
    Require ((git rev-parse "$finalHead^") -eq $initial.Head) 'Unexpected external commit parent.'
    $externalPath = 'Tools/XMLDocNormalizer/build/ExceptionFlow.HistoricalSources.props'
    Require ((@(git diff-tree --no-commit-id --name-only -r $finalHead) -join ';') -eq $externalPath) 'External commit touches other paths.'
    $beforeManifest = (git show "$($initial.Head):$externalPath") -join "`n"
    $afterManifest = (git show "$($finalHead):$externalPath") -join "`n"
    Require ($afterManifest -eq $beforeManifest.Replace('Models/DTO/ExceptionFlowAnalysisResult.cs', 'Models/Dto/ExceptionFlowAnalysisResult.cs')) 'External manifest edit is not the reviewed case-only fix.'
    Require ((Get-FileHash ../../.gitignore).Hash -eq $initial.RootIgnore) 'Root ignore changed.'
    Require ((Get-FileHash XMLDocNormalizer.sln).Hash -eq $initial.Solution) 'Normal solution changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -eq ($initial.Stashes -join "`n")) 'Stashes changed.'
    $coreFiles = @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -File -Recurse | Where-Object { $_.FullName -match '[\\/](bin|obj)[\\/]' } | Sort-Object FullName)
    Require ($coreFiles.Count -eq $initial.CoreFiles.Count) 'Core output file set changed.'
    foreach ($file in $initial.CoreFiles) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Core changed: $($file.Path)" }
    Require (-not (git diff --name-only -- src/XMLDocNormalizer.ExceptionFlow.Core)) 'Protected tracked Core changed.'
    Require ($initial.OriginalSources.Count -eq 362) 'Wrong original source baseline.'
    foreach ($file in $initial.OriginalSources) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Original Main source changed: $($file.Path)" }
    Require ($gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Expanded build/Main boundary gate incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Historical source boundary changed.'
    foreach ($file in $b3.DualBuildAndWorkerGate.SharedSourceFingerprints) {
        Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Historical source changed: $($file.Path)"
    }
    Require ($runtime.CanonicalParity -and $runtime.Deterministic -and $runtime.ProcessesStopped -and -not $runtime.HistoricalLoadedInCaller) 'Missing actual Main E2E proof.'
    Require ($runtime.ProcessId -ne $runtime.RepeatedProcessId) 'Repeated requests reused a process.'
    Require ($runtime.Historical.ProtocolVersion -eq 2) 'Wrong final protocol version.'
    foreach ($engine in $runtime.Current) {
        Require ($engine.InformationalVersion.StartsWith('5.0.0-2.25567.12+')) 'Wrong actual Current engine.'
        Require ($gate.CurrentCompilerReferences.Sha256 -contains $engine.Sha256) 'Caller image differs from compile references.'
    }
    foreach ($engine in $runtime.Historical.LoadedRoslyn) {
        Require ($engine.InformationalVersion.StartsWith('5.0.0-2.25451.107+')) 'Wrong actual Historical engine.'
        Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256) 'Worker image differs from exact compile references.'
    }
    $tests = @('worker-architecture', 'broad', 'full') | ForEach-Object {
        $path = "artifacts/p5o2b4/tests/$_.trx"
        [xml]$trx = Get-Content -LiteralPath $path -Raw
        $counts = $trx.TestRun.ResultSummary.Counters
        Require ([int]$counts.total -eq [int]$counts.passed -and [int]$counts.failed -eq 0) "Tests failed/skipped: $path"
        [ordered]@{ Name=$_; Total=[int]$counts.total; Passed=[int]$counts.passed; Failed=[int]$counts.failed; Artifact=$path; Sha256=(Get-FileHash $path).Hash }
    }
    Require ($tests[0].Total -eq 143 -and $tests[1].Total -eq 1887 -and $tests[2].Total -eq 2679) 'Unexpected final regression census.'
    [xml]$gateTrx = Get-Content artifacts/dual-version-build/main-boundary.trx -Raw
    Require ([int]$gateTrx.TestRun.ResultSummary.Counters.total -eq 79 -and [int]$gateTrx.TestRun.ResultSummary.Counters.passed -eq 79) 'Expanded gate Main tests missing.'
    $logs = @($gate.Steps | ForEach-Object {
        $path = 'artifacts/dual-version-build/' + $_.Name + '.log'
        $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        Require ($_.ExitCode -eq 0) "Failed build step: $($_.Name)"
        if ($_.Name.EndsWith('-build')) {
            Require ($content -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $content -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Nonzero build warnings/errors: $path"
        }
        Fingerprint $path
    })
    $owned = @('src/XMLDocNormalizer/XMLDocNormalizer.csproj', 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj',
        'Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/HistoricalAnalyzerBuildProjectTests.cs',
        'build/Verify-DualVersionBuild.ps1', 'build/README.md', '../../.github/workflows/xml-doc-normalizer-dual-build.yml',
        'Evaluation/P5O2B4-main-worker-analysis-boundary.md', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md',
        'Evaluation/P5O2B4Proof/Write-Verify-BoundaryEvidence.ps1')
    $owned += @(Get-ChildItem src/XMLDocNormalizer/Execution/Historical, src/XMLDocNormalizer.HistoricalWorker, Tests/XMLDocNormalizerTests/Worker, Evaluation/P5O2B4Proof/TestProcess -File | ForEach-Object { $_.FullName })
    $utf8 = New-Object Text.UTF8Encoding($false, $true)
    foreach ($path in $owned) {
        $content = $utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        Require ($content -notmatch '(?<!\r)\n' -and $content.EndsWith("`r`n")) "UTF8/CRLF/final newline failed: $path"
        if ($path.EndsWith('.csproj') -or $path.EndsWith('.props')) { [xml](Get-Content -LiteralPath $path -Raw -Encoding UTF8) | Out-Null }
    }
    foreach ($path in @('build/Verify-DualVersionBuild.ps1', 'Evaluation/P5O2B4Proof/Write-Verify-BoundaryEvidence.ps1')) {
        $parseErrors = $null
        [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path, [ref]$null, [ref]$parseErrors) | Out-Null
        Require ($parseErrors.Count -eq 0) "PowerShell parse failed: $path"
    }
    $format = & dotnet format whitespace . --folder --include src/XMLDocNormalizer/Execution/Historical src/XMLDocNormalizer.HistoricalWorker Tests/XMLDocNormalizerTests/Worker Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/HistoricalAnalyzerBuildProjectTests.cs Evaluation/P5O2B4Proof/TestProcess --verify-no-changes 2>&1 | Out-String
    Require ($LASTEXITCODE -eq 0) 'Folder-only format failed.'
    $format | Set-Content artifacts/p5o2b4/format.log -Encoding UTF8
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'git diff --check failed.'
    $canonical = $b3.CurrentSemantics.Canonical
    Require ($canonical.Inherited -and $canonical.Added -eq 0 -and $canonical.Removed -eq 0 -and $canonical.ChangedEvidence -eq 0) 'Inherited canonical evidence missing.'
    $audit = [ordered]@{
        Schema='P5O2B4-main-worker-analysis-boundary-v1'; Date='2026-10-08'; StartingHead=$initial.Head; Head=$finalHead; Decision='Complete'
        ExternalHeadChange=[ordered]@{ Commit=$finalHead; Parent=$initial.Head; Path=$externalPath; CaseOnly=$true; AnalyzerSourceBytesChanged=$false; AgentCommitted=$false; FinalRuntimeInformationalVersion=$runtime.Historical.Worker.InformationalVersion }
        MainOwner='XMLDocNormalizer.Execution.Historical.HistoricalWorkerClient'; AutomaticRouting=$false
        Protocol=[ordered]@{ Version=2; B3Version1Rejected=$true; WorkerVersion='1.0'; Transport='strict UTF8 JSON, stdin EOF, one request/process'; ContractSource='src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs'; MainHistoricalProjectReferences=0; TransitivelyRoslynFree=$true; ResultOwner='existing CanonicalExceptionFlowAnalysisResult'; Provenance='UTF8 source SHA256, root selector, logical document, mode/reference/language/nullable profile'; RequestCharacters=65536; SourceCharacters=32768; SyntaxNodes=8192; ResponseCharacters=1048576; DiagnosticCharacters=16384 }
        Input=[ordered]@{ Supported='one CSharp12 source tree, fixed seven-image net8 runtime profile, nullable enable, HistoricalWorkerInput, static parameterless nongeneric source root, solution-transitive'; TypedButRejected=@('additional source documents/original bytes/checksums', 'metadata reference images/SHA256', 'supporting compilations/dependency IDs/provenance', 'canonical root identities', 'compiler option entries/preprocessor symbols/nondefault assembly'); Future='explicit selection and validated external input projection; acquire/validate artifacts in Main, not paths or object bags across the process boundary' }
        FailureContract=@('UnsupportedInput', 'StartFailure', 'ProtocolMismatch', 'MalformedResponse', 'IdentityMismatch', 'WorkerCrash', 'NonZeroExit', 'StructuredFailure', 'IncompleteResult', 'TransportFailure', 'OutputLimit', 'Timeout', 'Cancelled')
        MainEndToEnd=$runtime; ExpandedExistingGate=$gate; BuildLogFingerprints=$logs; Tests=$tests
        IntegrationGateTests=[ordered]@{Passed=79; Artifact='artifacts/dual-version-build/main-boundary.trx'; Sha256=(Get-FileHash artifacts/dual-version-build/main-boundary.trx).Hash}
        BoundarySourceFingerprints=@($owned | ForEach-Object { Fingerprint $_ })
        Quality=[ordered]@{ StrictUtf8CrLf=$true; XmlAndPowerShellParse=$true; JsonParsed=$true; FolderOnlyFormat=$true; GitDiffCheck=$true }
        Semantics=[ordered]@{ OriginalCurrentSourcesUnchanged=362; SharedHistoricalSourcesUnchanged=121; AnalyzerSemanticsChanged=$false; SelfAnalysis=$b3.CurrentSemantics.SelfAnalysis; Canonical=$canonical; InheritedReason='B4 section 18: only host/boundary/integration sources changed; all original 362 Current and 121 shared source hashes reverified'; SourceEvidence='Evaluation/P5O2B3-historical-worker-host-audit.json'; SourceEvidenceSha256=(Get-FileHash Evaluation/P5O2B3-historical-worker-host-audit.json).Hash }
        LocalCorrections=@('nullable PathStep line/column wiring corrected after failed Main build; subsequent stale binary test run excluded', 'retained B1 probe Models/DTO vs active Git Models/Dto spelling compared case-insensitively consistently with existing manifest identity tests', 'PS5 native stderr captured before checking failed test exit code', 'closed DTO type allowlist explicitly admits existing neutral Guid scalar; final gate/regression series rerun')
        RemainingBoundary='No automatic Current/Historical selection, CLI/full reporting route, full external manifest/reference/PE reconstruction, or supporting source import yet. No Dapper pipeline advancement.'
        NextStep='Explicit routing/selection integration: when Current and when Historical; reuse this productive client and exact worker.'
        Protected=[ordered]@{ Unchanged=$true; RootIgnore=$initial.RootIgnore; CoreFiles=$initial.CoreFiles; Stashes=$initial.Stashes; Solution=$initial.Solution; StartingSnapshot= (Fingerprint 'artifacts/p5o2b4/protected-before.json') }
        Git=[ordered]@{ Status=@(git status --short); AgentCommit=$false; AgentPush=$false; AgentStashChanged=$false; AgentIndexChanged=$false; ExternalCommit=$finalHead }
    }
    $audit | ConvertTo-Json -Depth 30 | Set-Content artifacts/p5o2b4/main-worker-boundary-audit.json -Encoding UTF8
    Write-Host 'PASS: B4 Main E2E/parity/isolation/fail-closed/lifecycle, all regression/build/quality gates and protected state.'
} finally { Pop-Location }
