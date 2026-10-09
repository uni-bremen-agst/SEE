param([Parameter(Mandatory = $true)][string]$ValidationRoot)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function ReadTests([string]$Name, [string]$Path) {
        [xml]$trx = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts = $trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($null -ne $counts -and [int]$counts.total -gt 0 -and $counts.total -eq $counts.passed -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Failed/skipped suite: $Name"
        [ordered]@{Name=$Name;Total=[int]$counts.total;Passed=[int]$counts.passed;Failed=[int]$counts.failed;NotExecuted=[int]$counts.notExecuted;Artifact=(Fingerprint $Path)}
    }
    $initial = ReadJson 'artifacts/p5o2b10/protected-before.json'
    $export = ReadJson 'artifacts/p5o2b10/export.json'
    $validation = [IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -eq $export.ToolRoot) 'Validation location differs from recorded export.'
    $head = git rev-parse HEAD
    Require ($head -eq $initial.Head -and (git branch --show-current) -eq $initial.Branch) 'External HEAD/branch movement requires an explicit new audit.'
    Require ((Get-FileHash ../../.gitignore).Hash -eq $initial.RootIgnore) 'Root ignore WIP changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -ceq ($initial.Stashes -join "`n")) 'Stashes changed.'
    Require ((@(git diff --cached --name-only) -join "`n") -ceq ($initial.Index -join "`n")) 'Index changed.'
    $ownedSources = @('src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs',
        'src/XMLDocNormalizer/Execution/Semantic/ExternalPeDebugDirectoryDescriptorFactory.cs',
        'src/XMLDocNormalizer/Execution/Semantic/ExternalCSharpCompilationConfigurationFactory.cs',
        'src/XMLDocNormalizer/Execution/Semantic/ExternalCSharpSyntaxTreeFactory.cs',
        'src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs')
    $allowed = @($ownedSources | ForEach-Object { [IO.Path]::GetFullPath($_) })
    foreach ($file in @($initial.MainSources) + @($initial.CoreFiles) + @($initial.Projects) + @($initial.WorkerTests)) {
        if ($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Protected/unrelated file changed: $($file.Path)" }
    }
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq $initial.CoreFiles.Count) 'Core file set changed.'
    foreach ($path in @($ownedSources) + @('build/Verify-DualVersionBuild.ps1')) {
        Require ((Get-FileHash -LiteralPath $path).Hash -eq (Get-FileHash -LiteralPath (Join-Path $validation $path)).Hash) "Validated owned source differs: $path"
    }
    Require ($export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Export was not clean.'
    $gateRoot = Join-Path $validation 'artifacts/dual-version-build'
    $gate = ReadJson (Join-Path $gateRoot 'audit.json')
    Require ($gate.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Full dual gate incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Build/source boundary changed.'
    foreach ($file in $gate.SharedSourceFingerprints) {
        $exportPath = Join-Path $validation $file.Path
        Require ((Get-FileHash -LiteralPath $exportPath).Hash -eq $file.Sha256) "Validated shared source changed: $($file.Path)"
        Require ([IO.File]::ReadAllText($exportPath).Replace("`r`n", "`n") -ceq [IO.File]::ReadAllText((Join-Path $repoRoot $file.Path)).Replace("`r`n", "`n")) "Shared source differs from protected worktree: $($file.Path)"
    }
    $logs = foreach ($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed build/gate step: $($step.Name)"
        $path = Join-Path $gateRoot ($step.Name + '.log')
        if ($step.Name.EndsWith('-build')) {
            $text = Get-Content -LiteralPath $path -Raw -Encoding UTF8
            Require ($text -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $text -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Build warnings/errors: $path"
        }
        Fingerprint $path
    }
    $tests = @(
        ReadTests 'parent-current' (Join-Path $export.ExportRoot 'parent-tests/parent-current.trx')
        ReadTests 'b10' (Join-Path $validation 'artifacts/p5o2b10/tests/b10.trx')
        ReadTests 'b5-b9-independent' (Join-Path $validation 'artifacts/p5o2b10/tests/b5-b9.trx')
        ReadTests 'productive' (Join-Path $validation 'artifacts/p5o2b10/tests/productive.trx')
        ReadTests 'focused' (Join-Path $validation 'artifacts/p5o2b10/tests/focused.trx')
        ReadTests 'broad' (Join-Path $validation 'artifacts/p5o2b10/tests/broad.trx')
        ReadTests 'full' (Join-Path $validation 'artifacts/p5o2b10/tests/full.trx')
        ReadTests 'permanent-boundary' (Join-Path $gateRoot 'main-boundary.trx')
        ReadTests 'multimodule-isolated' (Join-Path $validation 'artifacts/p5o2b10/tests/multimodule-isolated.trx')
    )
    Require ($tests[0].Total -eq 8 -and $tests[1].Total -eq 34 -and $tests[2].Total -eq 156 -and $tests[6].Total -eq 2871 -and $tests[7].Total -eq 269) 'Unexpected final test census.'
    $firstBroadPath = Join-Path $validation 'artifacts/p5o2b10/tests/broad-first.trx'
    [xml]$firstBroad = Get-Content -LiteralPath $firstBroadPath -Raw -Encoding UTF8
    $firstCounts = $firstBroad.SelectSingleNode('//*[local-name()="Counters"]')
    $failedCases = $firstBroad.SelectNodes('//*[local-name()="UnitTestResult" and @outcome="Failed"]')
    Require ($firstCounts.total -eq '2780' -and $firstCounts.passed -eq '2779' -and $firstCounts.failed -eq '1' -and $firstCounts.notExecuted -eq '0') 'First broad attempt census changed.'
    Require ($failedCases.Count -eq 1 -and $failedCases[0].testName.EndsWith('ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed')) 'First failure is not the documented unchanged MultiModule fixture.'
    $firstBroadAttempt = [ordered]@{Total=2780;Passed=2779;Failed=1;NotExecuted=0;TestName=$failedCases[0].testName;Artifact=(Fingerprint $firstBroadPath);Log=(Fingerprint (Join-Path $validation 'artifacts/p5o2b10/broad-first.log'))}
    $parity = foreach ($before in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after = Join-Path $validation ('artifacts/p5o2b9/current/' + $before.Name)
        Require ((Get-Content -LiteralPath $before.FullName -Raw -Encoding UTF8) -ceq (Get-Content -LiteralPath $after -Raw -Encoding UTF8)) "Current finding/graph drift: $($before.Name)"
        [ordered]@{Case=$before.Name;Before=(Fingerprint $before.FullName);After=(Fingerprint $after);Equal=$true}
    }
    Require (@($parity).Count -eq 8) 'Missing complete Current parity cases.'
    $runtime = ReadJson (Join-Path $validation 'artifacts/p5o2b10/payload-production-boundary.json')
    $emission = ReadJson (Join-Path $validation 'artifacts/p5o2b7/historical-pdb/emission-evidence.json')
    Require ($runtime.AutomaticPayload -and -not $runtime.RawPayloadInPositiveSetup -and $runtime.RetainsOriginalObjects -and $runtime.ActualFactoryReceipt) 'Automatic trusted payload proof incomplete.'
    Require ($runtime.ProcessStopped -and -not $runtime.HistoricalLoadedInCaller -and $runtime.RealPdbValidation -eq 'IdentityAndChecksum') 'Historical process/provenance isolation missing.'
    Require ($runtime.PeSha256 -eq $emission.PeSha256 -and $runtime.PdbSha256 -eq $emission.PdbSha256) 'Positive input is not the actual Historical emission.'
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $textHash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($runtime.Payload.Source))).Replace('-', '') }
    finally { $sha.Dispose() }
    Require ($runtime.WorkerProvenance.SourceSha256 -eq $textHash) 'Worker response is not bound to projected text.'
    foreach ($engine in $runtime.HistoricalIdentity.LoadedRoslyn) {
        Require ($engine.InformationalVersion -ceq $runtime.HistoricalContext.CompilerVersion -and $gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256) 'Worker runs a different compiler from original provenance.'
    }
    $process = Get-Process -Id $runtime.ProcessId -ErrorAction SilentlyContinue
    Require ($null -eq $process -or $process.HasExited) 'Historical process remains alive.'
    $projection = Get-Content -LiteralPath $ownedSources[4] -Raw -Encoding UTF8
    foreach ($forbidden in @('ExceptionFlowAnalyzerSelectionPolicy','ExceptionFlowAnalysisRouter','HistoricalWorkerClient','Process.Start','Assembly.Load','5.0.0-','File.','Stream','MetadataReader','PEReader','ParseText','SourceText.From','TryCreate(','GetChecksum','SHA256','JsonSerializer','AnalyzeAsync')) {
        Require (-not $projection.Contains($forbidden)) "Projection owns forbidden selection/parser/execution: $forbidden"
    }
    $testSource = Get-Content -LiteralPath $ownedSources[5] -Raw -Encoding UTF8
    Require ($testSource -notmatch 'new\s+WorkerAnalysisInput\s*\(' -and $testSource -notmatch 'new\s+WorkerCompilationContext\s*\(') 'New test setup manually manufactures Worker payload/context.'
    $owned = @($ownedSources) + @('build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md',
        'Evaluation/P5O2B10-historical-worker-payload-projection.md','Evaluation/P5O2B10Proof/Write-Verify-PayloadEvidence.ps1')
    $utf8 = New-Object Text.UTF8Encoding($false,$true)
    foreach ($path in $owned) {
        $text = $utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path $path).Path))
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF8/CRLF failed: $path"
        if ($path -like '*.ps1') {
            $errors = $null
            [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$errors) | Out-Null
            Require ($errors.Count -eq 0) "PowerShell parse failed: $path"
        }
    }
    $prior = $ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $format=dotnet format whitespace . --folder --include $ownedSources --verify-no-changes 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$prior }
    $format | Set-Content artifacts/p5o2b10/format-final.log -Encoding UTF8
    Require ($code -eq 0) 'Format verification failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace check failed.'
    $audit = [ordered]@{
        Schema='P5O2B10-worker-payload-projection-v1';StartingHead=$initial.Head;Head=$head;ExternalHeadMovement=$false;Decision='Complete for bounded profile'
        Projection='Execution/Analysis/HistoricalWorkerPayloadProjection.Project';Adoption='XmlDocExceptionSemanticDetector.AnalyzeExternalExceptionFlowAsync'
        Input='existing P5A provenance + original P5I source/text + PE-bound IMethodSymbol + explicit ExceptionAnalysisMode'
        ManualBefore='B9 prepared WorkerAnalysisInput(source, type name, method name, default context) in tests'
        Chain='existing P4/P5A/P5G/P5H/P5I handoffs -> B10 -> same B9 async seam -> B8/B7/B6/B5 -> B4 Worker'
        Trust='Reference receipts on successful existing P4/P5G/P5I factories; same original P5A descriptor and document; existing PE method module/assembly binding; no second validator/parser or file I/O'
        FieldMapping=[ordered]@{Source='retained P5I.Text.ToString, no decode/parse/normalization';TypeMetadataName='exact namespace/nested metadata names of PE-bound method type';MethodName='PE ordinary method MetadataName';AnalysisMode='explicit SolutionTransitive only';LanguageNullable='recorded CSharp12 / Enable only';ReferenceProfile='published net8-runtime-bounded-v1 policy, not original-reference equivalence';AssemblyName='published HistoricalWorkerInput analysis identity, not original target identity';UnsupportedExtras='source count/options/signing/defines mismatches rejected; no additional sources/reference/supporting/root/compiler envelope invented';OriginalProvenance='same original P5A/P5I/P5H objects retained Main-locally alongside unchanged IPC contract'}
        Failure='typed missing/unvalidated/mixed/profile/selector/payload errors with no Value; no dispatch after failed projection; original downstream staged/Worker failures without fallback'
        Current='B9 native Current route unchanged; no B10 payload construction or Worker; all eight complete parent finding/graph cases rerun unchanged'
        NoCurrentFallback=$true;B5B6B7B8Unchanged=$true;AnalyzerSemanticsChanged=$false;VersionsChanged=$false;WorkerContractChanged=$false;NewParserValidator=$false;AllKnowingInputContext=$false
        Tests=$tests;CurrentParity=@($parity);Runtime=$runtime;ActualEmission=$emission;BuildLogs=@($logs)
        DualGate=[ordered]@{Passed=$true;SharedSources=121;HostSources=1;BuildImages=6;WorkerBuildImages=2;TestProjectReferencesEnabled=$true;Audit=(Fingerprint (Join-Path $gateRoot 'audit.json'))}
        Quality=[ordered]@{Format=$true;Utf8CrLf=$true;JsonParsed=$true;PowerShellParsed=$true;ArchitectureIsolationTests=$true;GitDiffCheck=$true;NoManualPayloadInNewSetup=$true}
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;Inherited=$true;FreshSelfAnalysis=$false;FreshCanonicalDiff=$false;Note='B9 baseline values, not newly revalidated for changed source/documentation. Fresh Current fixture finding/graph parity is separate.'}
        Attempts=[ordered]@{FirstDualGatePassed=$true;FirstBoundaryCases=266;FinalBoundaryCases=269;TestFailures=1;BuildFailures=0;FirstBroad=$firstBroadAttempt;IsolatedRetry=$tests[8];BroadRetry=$tests[5];NoTestOrProductionChangeForRetry=$true;Note='Previously documented MultiModule fixture assertion flaked in first broad run; preserved failure, isolated retry and identical broad rerun. No exclusions or suppression.';Expansion='exact nested/keyword metadata names, multiple sources, source budget';EditorPatchRetry='Formatter split a test initializer; failed atomic patch was reapplied against actual lines; no source/build failure.'}
        Remaining='full external reference/runtime/documents/options/generator/supporting-compilation equivalence and original identity in Worker results; Historical reporting import; CLI/acquisition/discovery and other call-sites; no Dapper redesign'
        Protected=[ordered]@{RootIgnore=$initial.RootIgnore;CoreFiles=$initial.CoreFiles.Count;MainSourcesExceptFourChanged=$initial.MainSources.Count-4;ExistingWorkerTests=$initial.WorkerTests.Count;Stashes=6;IndexUnchanged=$true}
        OwnedFileFingerprints=@($owned | ForEach-Object { Fingerprint $_ });Git=[ordered]@{Status=@(git status --short);AgentCommit=$false;AgentPush=$false;AgentIndexChanged=$false;AgentStashChanged=$false}
        LocalValidation='Windows isolated Git archive + only owned source overlay; original Core outputs preserved; no Linux/remote run claimed'
    }
    $jsonPath = Join-Path $repoRoot 'Evaluation/P5O2B10-historical-worker-payload-projection-audit.json'
    $jsonText = ($audit | ConvertTo-Json -Depth 18) -replace "`r?`n", "`r`n"
    [IO.File]::WriteAllText($jsonPath, $jsonText + "`r`n", (New-Object Text.UTF8Encoding($false)))
    ReadJson $jsonPath | Out-Null
    Write-Host 'PASS: automatic bounded payload, genuine Historical production flow, Current parity, complete regressions and repository protection.'
} finally { Pop-Location }
