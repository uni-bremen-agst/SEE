param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 40 -Compress }
    function ReadTests([string]$Name,[string]$Path) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Aborted/failed run summary: $Name"
        Require ($null -ne $counts -and [int]$counts.total -gt 0 -and $counts.total -eq $counts.passed -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Failed/skipped suite: $Name"
        [ordered]@{Name=$Name;Total=[int]$counts.total;Passed=[int]$counts.passed;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b11/protected-before.json'
    $export=ReadJson 'artifacts/p5o2b11/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -eq $export.ToolRoot -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Validation is not the recorded clean export.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'External HEAD/branch movement requires a new audit.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Root ignore WIP changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -ceq ($initial.Stashes -join "`n")) 'Stashes changed.'
    Require ((@(git diff --cached --name-only) -join "`n") -ceq ($initial.Index -join "`n")) 'Index changed.'
    $ownedSources=@('src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs','src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs',
        'src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs',
        'src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs',
        'Evaluation/P5O2B7Proof/HistoricalPdbFixture/Program.cs','Evaluation/P5O2B7Proof/HistoricalPdbFixture/B11GraphProbe.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs')
    $built=@($ownedSources)+@('build/Verify-DualVersionBuild.ps1')
    $allowed=@($built | ForEach-Object { [IO.Path]::GetFullPath($_) })
    foreach($file in @($initial.TrackedSources)+@($initial.CoreFiles)) {
        if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/protected source changed: $($file.Path)" }
    }
    Require ($initial.CoreFiles.Count -eq 23 -and @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Protected Core file set changed.'
    foreach($path in $built) { Require ((Get-FileHash -LiteralPath $path).Hash -ceq (Get-FileHash -LiteralPath (Join-Path $validation $path)).Hash) "Validated input differs: $path" }

    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parentGate=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($parentGate.Passed -and $gate.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual-version/runtime/boundary gate incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Shared-source/build boundary changed.'
    foreach($file in $gate.SharedSourceFingerprints) {
        $path=Join-Path $validation $file.Path
        Require ((Get-FileHash -LiteralPath $path).Hash -ceq $file.Sha256) 'Validated shared source changed.'
        Require ([IO.File]::ReadAllText($path).Replace("`r`n","`n") -ceq [IO.File]::ReadAllText((Join-Path $repoRoot $file.Path)).Replace("`r`n","`n")) 'Shared worktree/export source differs.'
    }
    $logs=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed build/gate step: $($step.Name)"
        $path=Join-Path $gateRoot ($step.Name+'.log')
        if($step.Name.EndsWith('-build')) {
            $text=Get-Content -LiteralPath $path -Raw -Encoding UTF8
            Require ($text -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $text -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Build warnings/errors: $path"
        }
        Fingerprint $path
    }
    $tests=@(
        ReadTests 'parent-before' (Join-Path $validation 'artifacts/p5o2b11/tests/parent-before.trx')
        ReadTests 'b11' (Join-Path $validation 'artifacts/p5o2b11/tests/b11.trx')
        ReadTests 'b10' (Join-Path $validation 'artifacts/p5o2b11/tests/b10.trx')
        ReadTests 'b5-b9' (Join-Path $validation 'artifacts/p5o2b11/tests/b5-b9.trx')
        ReadTests 'productive' (Join-Path $validation 'artifacts/p5o2b11/tests/productive.trx')
        ReadTests 'focused' (Join-Path $validation 'artifacts/p5o2b11/tests/focused.trx')
        ReadTests 'broad' (Join-Path $validation 'artifacts/p5o2b11/tests/broad.trx')
        ReadTests 'full' (Join-Path $validation 'artifacts/p5o2b11/tests/full.trx')
        ReadTests 'boundary' (Join-Path $gateRoot 'main-boundary.trx')
    )
    Require ($tests[0].Total -eq 9 -and $tests[1].Total -eq 16 -and $tests[2].Total -eq 34 -and $tests[3].Total -eq 156 -and $tests[7].Total -eq 2887 -and $tests[8].Total -eq 285) 'Unexpected B11 test census.'
    Require ($tests[4].Total -eq 676 -and $tests[5].Total -eq 370 -and $tests[6].Total -eq 2796) 'Unexpected regression group census.'
    $firstBroadPath=Join-Path $validation 'artifacts/p5o2b11/tests/broad-first.trx'
    [xml]$firstBroad=Get-Content -LiteralPath $firstBroadPath -Raw -Encoding UTF8
    $firstCounts=$firstBroad.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($firstBroad.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Failed' -and $firstCounts.total -eq '2782' -and $firstCounts.passed -eq '2782') 'Original aborted broad run evidence changed.'
    $crash=ReadJson 'artifacts/p5o2b11/testhost-crash.json'
    Require ($crash.EventId -eq 1000 -and $crash.ConfirmedModule -ceq 'clrjit.dll' -and $crash.ExceptionCode -ceq '0xc0000409' -and $crash.HelperStopped) 'Native testhost crash/owned helper cleanup evidence missing.'
    $firstBroadAttempt=[ordered]@{Outcome='Failed/aborted';RecordedTotal=2782;RecordedPassed=2782;Complete=$false;Artifact=(Fingerprint $firstBroadPath);Log=(Fingerprint (Join-Path $validation 'artifacts/p5o2b11/broad-first.log'));RuntimeEvent=$crash;UnchangedRetry=$tests[6];RetryDiagnostic='--blame only, identical filter/source/test configuration';RetryLog=(Fingerprint (Join-Path $validation 'artifacts/p5o2b11/broad-retry.log'))}
    $parity=foreach($before in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$before.Name)
        Require ((Get-Content -LiteralPath $before.FullName -Raw -Encoding UTF8) -ceq (Get-Content -LiteralPath $after -Raw -Encoding UTF8)) "Full Current finding/graph drift: $($before.Name)"
        [ordered]@{Case=$before.Name;Equal=$true;Before=(Fingerprint $before.FullName);After=(Fingerprint $after)}
    }
    Require (@($parity).Count -eq 8) 'Missing full Current parity cases.'
    $before=ReadJson (Join-Path $validation 'artifacts/p5o2b11/before-path-gap.json')
    $runtime=ReadJson (Join-Path $validation 'artifacts/p5o2b11/source-path-production.json')
    $gaps=ReadJson (Join-Path $validation 'artifacts/p5o2b11/inventory-differentials.json')
    $b10=ReadJson (Join-Path $validation 'artifacts/p5o2b10/payload-production-boundary.json')
    $emission=ReadJson (Join-Path $validation 'artifacts/p5o2b7/historical-pdb/emission-evidence.json')
    Require ($before.UnmodifiedB10 -and $before.AutomaticProjection -and -not $before.FullFlowEqual -and $before.OnlyLogicalPathDifferent) 'Actual before-gap execution missing.'
    Require ($runtime.AutomaticProjection -and $runtime.FullCanonicalFlowEqual -and $runtime.NoNormalizationForEquality -and $runtime.ProcessesStopped -and -not $runtime.HistoricalLoadedInMain -and $runtime.NoCurrentFallback) 'Automatic real Historical source-path proof incomplete.'
    Require ((Json $runtime.Native.Result) -ceq (Json $runtime.Historical) -and (Json $runtime.Historical) -ceq (Json $runtime.RepeatedHistorical)) 'Full canonical differential/repetition mismatch.'
    Require ($runtime.Graph.ActualHistoricalHost -and -not $runtime.Graph.SubmittedSourceExecuted -and $runtime.Graph.RepeatedResultEqual -and $runtime.Graph.GraphCount -eq $runtime.Native.GraphCount -and $runtime.Graph.RepeatedGraphCount -eq $runtime.Native.GraphCount) 'Actual Historical graph/repetition proof missing.'
    Require ((Json $runtime.Graph.Result) -ceq (Json $runtime.Historical)) 'Diagnostic graph probe does not agree with real Worker execution.'
    Require ($runtime.FirstProvenance.LogicalPath -ceq $runtime.OriginalPath -and $b10.Payload.LogicalPath -ceq $b10.OriginalDocumentName -and $b10.WorkerProvenance.LogicalPath -ceq $b10.OriginalDocumentName) 'Original path lost.'
    Require ($b10.PeSha256 -ceq $emission.PeSha256 -and $b10.PdbSha256 -ceq $emission.PdbSha256) 'Actual Historical PE/PDB emission differs.'
    foreach($engine in $runtime.FirstIdentity.LoadedRoslyn) { Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256 -and $runtime.Graph.CompilerMvids -contains $engine.Mvid) 'Historical runtime/compiler/graph host identity mismatch.' }
    foreach($processId in @($runtime.FirstProcess,$runtime.SecondProcess)) {
        $process=Get-Process -Id $processId -ErrorAction SilentlyContinue
        Require ($null -eq $process -or $process.HasExited) 'Historical Worker is still running.'
    }
    Require ($gaps.Defines.Gap -and $gaps.References.Gap -and $gaps.Sources.Gap -and $gaps.Nullable.AnnotationsDiffer -and $gaps.Nullable.FlowEqual -and $gaps.Deferred) 'Differential gap inventory incomplete.'
    $report='Evaluation/P5O2B11-historical-external-input-equivalence.md'
    $matrix=foreach($line in Get-Content $report -Encoding UTF8) {
        if($line.StartsWith('| ') -and -not $line.StartsWith('| Input') -and -not $line.StartsWith('| ---')) {
            $parts=$line.Split('|')
            if($parts.Length -eq 7) { [ordered]@{Input=$parts[1].Trim();Current=$parts[2].Trim();HistoricalBeforeB11=$parts[3].Trim();B11Measure=$parts[4].Trim();Equivalent=$parts[5].Trim()} }
        }
    }
    Require (@($matrix).Count -ge 30) 'Incomplete field-by-field inventory matrix.'
    $owned=@($built)+@('Evaluation/OPEN-PIPELINE-BOUNDARIES.md',$report,'Evaluation/P5O2B11Proof/Write-Verify-InputEvidence.ps1')
    $utf8=New-Object Text.UTF8Encoding($false,$true)
    foreach($path in $owned) {
        $text=$utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF8/CRLF failed: $path"
        if($path -like '*.ps1') {
            $errors=$null
            [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$errors) | Out-Null
            Require ($errors.Count -eq 0) "PowerShell parse failed: $path"
        }
    }
    $prior=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $format=dotnet format whitespace . --folder --include $ownedSources --verify-no-changes 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$prior }
    $format | Set-Content artifacts/p5o2b11/format-final.log -Encoding UTF8
    Require ($code -eq 0) 'Format verification failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace check failed.'
    $audit=[ordered]@{
        Schema='P5O2B11-external-input-equivalence-v1';Date='2026-10-11';Head=$initial.Head;ExternalHeadMovementDuringTask=$false
        InitialHeadChange='New task starts at separately committed B10 37c7b286, previous task ended at a3194cf0; audited before edits'
        Decision='Bounded B11 source-origin/path closure only; full external input equivalence remains open'
        Matrix=@($matrix);BeforeGap=$before;Closed='Exact logical source path in projection, Historical syntax reconstruction, response provenance and every full-flow source step'
        Protocol=[ordered]@{Version=2;AdditiveOptionalField='WorkerAnalysisInput.LogicalPath';MaximumPathCharacters=4096;Absent='existing legacy explicit bounded worker-input.cs policy';Production='required exact existing P5I.Document.Name, never absent';NoNormalization=$true;NoFileAccess=$true;UnknownFieldsRejected=$true;ResponsePathsBound=$true;NewRoslynObjectsInMain=$false}
        Runtime=$runtime;DifferentialGaps=$gaps;B10Runtime=$b10;Emission=$emission;CurrentParity=@($parity);Tests=$tests;BuildLogs=@($logs)
        ParentBuildAudit=(Fingerprint (Join-Path $export.ExportRoot 'parent-dual/audit.json'));FinalBuildAudit=(Fingerprint (Join-Path $gateRoot 'audit.json'))
        Unchanged=[ordered]@{AnalyzerSharedSources=121;HistoricalHostSources=1;AnalyzerSemantics=$true;B5B6B7B8B9=$true;Versions=$true;CurrentDetector=$true;ExistingTests=$true;ProvenanceFactories=$true;NoSecondValidatorParser=$true;NoCurrentFallback=$true}
        FindingComparison='eight fresh complete native Current before/after fixture comparisons and deterministic full native findings in new source-path test; no cross-engine Finding import/parity claim'
        GraphComparison='actual isolated Historical host count/repetition and full canonical output equals Current and genuine Worker; no full graph IPC/state migration'
        Remaining=[ordered]@{B12='exact reference images/order/properties/XML contracts, original assembly/signing identity and framework equivalence';B13='complete sources/parse/compiler context, generated output, project/supporting scopes and exact roots';Other='Historical reporting import, CLI/discovery/acquisition and other call-sites; compiler capability/completeness differences; no Dapper redesign'}
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;InheritedFrom='B10';FreshSelfAnalysis=$false;FreshCanonicalDiff=$false}
        Quality=[ordered]@{Format=$true;Utf8CrLf=$true;JsonParsed=$true;PowerShellParsed=$true;ArchitectureIsolationBoundary=$true;GitDiffCheck=$true}
        Attempts=[ordered]@{BuildFailures=0;AssertionFailures=0;AbortedRuns=1;UnchangedBroadRetries=1;FirstBroad=$firstBroadAttempt;SetupRetry=$export.SetupRetry;NoTestOrProductChangeForRetry=$true;Cause='Windows native clrjit.dll host fail-fast confirmed; causative test not established. No unrelated flake fix or exclusion.'}
        Protected=[ordered]@{CoreFiles=23;TrackedSources=$initial.TrackedSources.Count;RootIgnore=$initial.RootIgnore;Stashes=6;IndexUnchanged=$true}
        OwnedFiles=@($owned | ForEach-Object { Fingerprint $_ });Git=[ordered]@{Status=@(git status --short);Commit=$false;Push=$false;IndexChanged=$false;StashChanged=$false}
        Validation='local Windows clean Git export plus own file overlay; original Core outputs untouched; no Linux/remote execution claim'
    }
    $jsonPath=Join-Path $repoRoot 'Evaluation/P5O2B11-historical-external-input-equivalence-audit.json'
    $jsonText=($audit | ConvertTo-Json -Depth 45) -replace "`r?`n","`r`n"
    [IO.File]::WriteAllText($jsonPath,$jsonText+"`r`n",(New-Object Text.UTF8Encoding($false)))
    ReadJson $jsonPath | Out-Null
    Write-Host 'PASS: B11 source-origin closure, explicit deferred gap matrix, real differential/runtime/graph evidence, full regressions and repository protection.'
} finally { Pop-Location }
