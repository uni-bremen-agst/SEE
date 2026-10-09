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
        Require ($null -ne $counts -and [int]$counts.total -gt 0 -and $counts.total -eq $counts.passed -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Failed/skipped tests: $Name"
        [ordered]@{Name=$Name;Total=[int]$counts.total;Passed=[int]$counts.passed;Failed=[int]$counts.failed;NotExecuted=[int]$counts.notExecuted;Artifact=(Fingerprint $Path)}
    }
    $initial = ReadJson 'artifacts/p5o2b9/protected-before.json'
    $export = ReadJson 'artifacts/p5o2b9/export.json'
    $validation = [IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -eq $export.ToolRoot) 'Validation must match the recorded isolated export.'
    $head = git rev-parse HEAD
    Require ($head -eq $initial.Head) 'External HEAD movement needs a new explicit audit.'
    Require ((git branch --show-current) -eq $initial.Branch) 'Branch changed.'
    Require ((Get-FileHash ../../.gitignore).Hash -eq $initial.RootIgnore) 'Root ignore WIP changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -ceq ($initial.Stashes -join "`n")) 'Stashes changed.'
    Require ((@(git diff --cached --name-only) -join "`n") -ceq ($initial.Index -join "`n")) 'Index changed.'
    $detector = Join-Path $repoRoot 'src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs'
    foreach ($file in @($initial.MainSources) + @($initial.CoreFiles) + @($initial.Projects) + @($initial.WorkerTests)) {
        if ($file.Path -ne $detector) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Sha256) "Unrelated/protected file changed: $($file.Path)" }
    }
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq $initial.CoreFiles.Count) 'Core file set changed.'
    Require ($export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Export contained old inputs.'
    $gateRoot = Join-Path $validation 'artifacts/dual-version-build'
    $gate = ReadJson (Join-Path $gateRoot 'audit.json')
    Require ($gate.Passed -and $gate.MainBoundaryExecuted -and $gate.RuntimeExecuted) 'Dual gate incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Shared/build boundary changed.'
    foreach ($file in $gate.SharedSourceFingerprints) {
        $exportPath = Join-Path $validation $file.Path
        Require ((Get-FileHash -LiteralPath $exportPath).Hash -eq $file.Sha256) "Validated shared Analyzer changed: $($file.Path)"
        # Git archive retains repository line endings, while the Windows worktree uses CRLF.
        # Original worktree bytes are independently checked against the pre-edit snapshot above.
        Require ([IO.File]::ReadAllText($exportPath).Replace("`r`n", "`n") -ceq [IO.File]::ReadAllText((Join-Path $repoRoot $file.Path)).Replace("`r`n", "`n")) "Shared Analyzer source differs from protected worktree: $($file.Path)"
    }
    $logs = foreach ($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Gate step failed: $($step.Name)"
        $path = Join-Path $gateRoot ($step.Name + '.log')
        if ($step.Name.EndsWith('-build')) {
            $text = Get-Content -LiteralPath $path -Raw -Encoding UTF8
            Require ($text -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $text -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Build warnings/errors: $path"
        }
        Fingerprint $path
    }
    $tests = @(
        ReadTests 'parent-current' (Join-Path $export.ExportRoot 'baseline-tests/parent-current.trx')
        ReadTests 'b9' (Join-Path $validation 'artifacts/p5o2b9/tests/b9.trx')
        ReadTests 'b5-b8-independent' (Join-Path $validation 'artifacts/p5o2b9/tests/b5-b8.trx')
        ReadTests 'productive' (Join-Path $validation 'artifacts/p5o2b9/tests/productive.trx')
        ReadTests 'focused' (Join-Path $validation 'artifacts/p5o2b9/tests/focused.trx')
        ReadTests 'broad' (Join-Path $validation 'artifacts/p5o2b9/tests/broad.trx')
        ReadTests 'full' (Join-Path $validation 'artifacts/p5o2b9/tests/full.trx')
        ReadTests 'permanent-boundary' (Join-Path $gateRoot 'main-boundary.trx')
    )
    Require ($tests[0].Total -eq 8 -and $tests[1].Total -eq 23 -and $tests[2].Total -eq 133 -and $tests[6].Total -eq 2837 -and $tests[7].Total -eq 235) 'Unexpected test census.'
    $parity = foreach ($before in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after = Join-Path $validation ('artifacts/p5o2b9/current/' + $before.Name)
        Require ((Get-Content -LiteralPath $before.FullName -Raw -Encoding UTF8) -ceq (Get-Content -LiteralPath $after -Raw -Encoding UTF8)) "Finding/session drift: $($before.Name)"
        [ordered]@{Case=$before.Name;Before=(Fingerprint $before.FullName);After=(Fingerprint $after);FullFindingsAndGraphEqual=$true}
    }
    Require (@($parity).Count -eq 8) 'Incomplete Current baseline.'
    $boundary = ReadJson (Join-Path $validation 'artifacts/p5o2b9/production-boundary.json')
    $emission = ReadJson (Join-Path $validation 'artifacts/p5o2b7/historical-pdb/emission-evidence.json')
    Require ($boundary.CanonicalParity -and $boundary.ProcessStopped -and -not $boundary.HistoricalLoadedInCaller) 'Historical runtime parity/isolation incomplete.'
    Require ($boundary.ActualFactoryReceipt -and $boundary.RealPdbValidation -eq 'IdentityAndChecksum') 'Historical provenance not genuinely validated.'
    Require ($boundary.HistoricalPeSha256 -eq $emission.PeSha256 -and $boundary.HistoricalPdbSha256 -eq $emission.PdbSha256) 'Positive input does not match real emission.'
    foreach ($engine in $boundary.HistoricalIdentity.LoadedRoslyn) {
        Require ($engine.InformationalVersion -ceq $boundary.HistoricalContext.CompilerVersion -and $gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256) 'Wrong isolated Historical engine.'
    }
    $process = Get-Process -Id $boundary.HistoricalProcessId -ErrorAction SilentlyContinue
    Require ($null -eq $process -or $process.HasExited) 'Historical process still alive.'
    $source = Get-Content -LiteralPath $detector -Raw -Encoding UTF8
    foreach ($forbidden in @('session.Analyze(', 'ExceptionFlowAnalyzerSelectionPolicy.Select', 'Projector.Project', 'HistoricalWorkerClient', 'Process.Start', 'Assembly.Load', '5.0.0-', 'IsValidated', 'new WorkerAnalysisInput')) {
        Require (-not $source.Contains($forbidden)) "New production seam owns forbidden lower-layer behavior: $forbidden"
    }
    $owned = @('src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs','Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionPipelineTests.cs','build/Verify-DualVersionBuild.ps1','Evaluation/OPEN-PIPELINE-BOUNDARIES.md',
        'Evaluation/P5O2B9-production-analysis-pipeline-adoption.md','Evaluation/P5O2B9Proof/Write-Verify-ProductionEvidence.ps1')
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
    try {
        $ErrorActionPreference = 'Continue'
        $format = dotnet format whitespace . --folder --include $owned[0] $owned[1] $owned[2] --verify-no-changes 2>&1 | Out-String
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $prior }
    $format | Set-Content artifacts/p5o2b9/format-final.log -Encoding UTF8
    Require ($code -eq 0) 'Format failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git diff check failed.'
    $audit = [ordered]@{
        Schema='P5O2B9-production-adoption-v1';StartingHead=$initial.Head;Head=$head;ExternalHeadMovement=$false;Decision='Complete'
        ProductionEntry='XmlDocExceptionSemanticDetector.AnalyzeConfiguredExceptionFlow -> AnalyzeConfiguredExceptionFlowAsync'
        Before='ToolRunner ordinary/comparison documents -> detector mode guards -> existing Current SummarySession.Analyze'
        After='same caller/guards -> typed B8 request -> Dispatch -> B7 Projection -> B6 Selection -> B5 Router -> original Current session / B4 isolated Worker'
        ProductionBoundariesMigrated=1;AnalyzerSemanticsChanged=$false;VersionChanges=$false;RawValidationAdded=$false;SelectionDuplicated=$false;CurrentFallback=$false
        Current='same compilation/member/caller-owned sequential session; native result retained, mode/declared-empty guards preserved; failed dispatch aborts reporting'
        Historical='prepared PortablePdb + Worker typed payload through same seam; canonical result and original failure/identity/process evidence, never Main Roslyn import'
        SessionLifetime='ToolRunner run/mode owner and detector convenience/fallback owner unchanged; no session constructed by typed seam or dispatch'
        Failure='original B7/B6/B5/B4 staged contracts; no downstream execution after projection/selection errors; no Current recovery after Worker errors'
        Tests=$tests;CurrentParentParity=@($parity);Boundary=$boundary;ActualHistoricalEmission=$emission;BuildLogFingerprints=@($logs)
        Gate=[ordered]@{Passed=$true;SharedSources=121;HostSources=1;BuildImages=6;WorkerBuildImages=2;ProjectReferencesEnabled=$true;Audit=(Fingerprint (Join-Path $gateRoot 'audit.json'))}
        Quality=[ordered]@{Format=$true;Utf8CrLf=$true;PowerShellParsed=$true;JsonParsed=$true;GitDiffCheck=$true;ArchitectureIsolationTests=$true}
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;Inherited=$true;FreshSelfAnalysis=$false;FreshCanonicalDiff=$false;Note='Inherited B8 values, not claimed revalidated after detector/documentation changes. Fresh fixture finding/session parity is separate.'}
        Attempts=[ordered]@{ParentFirstPassed=0;ParentFirstFailed=8;ParentSecondPassed=6;ParentSecondFailed=2;ParentFinalPassed=8;B9InitialTestCompileErrors=1;EvidenceFirstFailed=$true;Corrections='New fixture expected DOC610 for throw null and DOC632 outside declared-type scope; corrected to actual unchanged parent contracts. New B9 test used StartupFailure instead of existing StartFailure. Evidence verifier originally compared Git-archive LF hashes against Windows CRLF worktree; now verifies the gate-recorded export bytes and protected worktree snapshots independently plus newline-normalized source equality. No production workaround.'}
        Remaining='direct-throw local helper, Analyzer graph operations and internal AnalyzeSolutionTransitivelyThrownExceptions convenience session entry preserved; B5 alone calls Session.Analyze outside the Analyzer; ordinary external reconstruction/CLI does not prepare Worker input; no Historical canonical-to-findings/reporting import; full external documents/references/options/generator output/selectors equivalence and broader Worker profiles remain open'
        OwnedFileFingerprints=@($owned | ForEach-Object { Fingerprint $_ });Protected=[ordered]@{RootIgnore=$initial.RootIgnore;CoreFiles=$initial.CoreFiles.Count;MainSourcesExceptDetector=$initial.MainSources.Count-1;SixStashesUnchanged=$true;IndexUnchanged=$true}
        Git=[ordered]@{Status=@(git status --short);AgentCommit=$false;AgentPush=$false;AgentIndexChanged=$false;AgentStashChanged=$false}
        LocalValidation='Windows isolated Git export + only owned-file overlay; no Linux/remote execution claimed'
    }
    $jsonPath = Join-Path $repoRoot 'Evaluation/P5O2B9-production-analysis-pipeline-adoption-audit.json'
    $jsonText = ($audit | ConvertTo-Json -Depth 18) -replace "`r?`n", "`r`n"
    [IO.File]::WriteAllText($jsonPath, $jsonText + "`r`n", (New-Object Text.UTF8Encoding($false)))
    ReadJson $jsonPath | Out-Null
    Write-Host 'PASS: production adoption, full parent finding/session parity, Historical parity/isolation, regressions and protected state.'
} finally { Pop-Location }
