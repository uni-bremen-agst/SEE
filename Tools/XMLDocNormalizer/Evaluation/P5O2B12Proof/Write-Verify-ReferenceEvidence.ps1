param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $toolRoot
try {
    function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 80 -Compress }
    function ReadTests([string]$Name,[string]$Path,[int]$Expected) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Failed/aborted summary: $Name"
        Require ($counts.total -eq "$Expected" -and $counts.total -eq $counts.passed -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Failed/skipped or unexpected census: $Name"
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b12/protected-before.json'
    $export=ReadJson 'artifacts/p5o2b12/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -eq $export.ToolRoot -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Not the recorded clean Git export.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'HEAD/branch moved; audit required.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected root ignore changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -ceq ($initial.Stashes -join "`n")) 'Stashes changed.'
    Require ((@(git diff --cached --name-only) -join "`n") -ceq ($initial.Index -join "`n")) 'Index changed.'
    $sources=@('src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs','src/XMLDocNormalizer.HistoricalWorker/Program.cs',
        'src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs',
        'src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs',
        'src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerReferenceProjection.cs',
        'src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs',
        'src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs',
        'src/XMLDocNormalizer/Execution/Semantic/ExternalMetadataReferenceSetFactory.cs',
        'src/XMLDocNormalizer/Execution/Semantic/ValidatedExternalMetadataReferenceMaterialFactory.cs',
        'Evaluation/P5O2B7Proof/HistoricalPdbFixture/Program.cs','Evaluation/P5O2B7Proof/HistoricalPdbFixture/B11GraphProbe.cs',
        'Evaluation/P5O2B7Proof/HistoricalPdbFixture/B12ReferenceProbe.cs',
        'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs')
    $built=$sources+@('build/Verify-DualVersionBuild.ps1')
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})
    foreach($file in @($initial.TrackedSources)+@($initial.CoreFiles)) {
        if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/protected file changed: $($file.Path)" }
    }
    Require ($initial.CoreFiles.Count -eq 23 -and @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core file set changed.'
    foreach($file in $built) { Require ((Get-FileHash -LiteralPath $file).Hash -ceq (Get-FileHash -LiteralPath (Join-Path $validation $file)).Hash) "Built input differs: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parentGate=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parentGate.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual-version/runtime/boundary gate incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Analyzer source/build boundary changed.'
    foreach($file in $gate.SharedSourceFingerprints) {
        $path=Join-Path $validation $file.Path
        Require ((Get-FileHash -LiteralPath $path).Hash -ceq $file.Sha256) 'Built shared source changed.'
        Require ([IO.File]::ReadAllText($path).Replace("`r`n","`n") -ceq [IO.File]::ReadAllText((Join-Path $toolRoot $file.Path)).Replace("`r`n","`n")) 'Worktree/export shared source differs.'
    }
    $logs=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Gate step failed: $($step.Name)"
        $path=Join-Path $gateRoot ($step.Name+'.log')
        if($step.Name.EndsWith('-build')) {
            $text=Get-Content -LiteralPath $path -Raw -Encoding UTF8
            Require ($text -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $text -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Build warnings/errors: $path"
        }
        Fingerprint $path
    }
    $tests=@(ReadTests 'parent-before' (Join-Path $validation 'artifacts/p5o2b12/tests/parent-before.trx') 9)
    $census=[ordered]@{b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=524;broad=2825;full=2916}
    foreach($name in $census.Keys) { $tests+=@(ReadTests $name (Join-Path $validation ('artifacts/p5o2b12/tests/'+$name+'.trx')) $census[$name]) }
    $tests+=@(ReadTests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 314)
    $coverage=foreach($group in @('productive','focused','broad')) {
        $oldPath=Join-Path $toolRoot ('artifacts/b11-37c7/Tools/XMLDocNormalizer/artifacts/p5o2b11/tests/'+$group+'.trx')
        $newPath=Join-Path $validation ('artifacts/p5o2b12/tests/'+$group+'.trx')
        [xml]$old=Get-Content -LiteralPath $oldPath -Raw -Encoding UTF8
        [xml]$new=Get-Content -LiteralPath $newPath -Raw -Encoding UTF8
        $oldNames=@($old.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object testName)
        $newNames=@($new.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object testName)
        $missing=@($oldNames | Where-Object {$newNames -cnotcontains $_})
        Require ($missing.Count -eq 0) "Original B11 regression cases omitted: $group"
        [ordered]@{Group=$group;OriginalCount=$oldNames.Count;CurrentCount=$newNames.Count;MissingCases=@($missing);
            AdditionalCases=@($newNames | Where-Object {$oldNames -cnotcontains $_});OriginalTrx=(Fingerprint $oldPath)}
    }
    $parity=foreach($before in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$before.Name)
        Require ((Get-Content -LiteralPath $before.FullName -Raw -Encoding UTF8) -ceq (Get-Content -LiteralPath $after -Raw -Encoding UTF8)) "Complete Current Finding/graph drift: $($before.Name)"
        [ordered]@{Case=$before.Name;CompleteJsonEqual=$true;Before=(Fingerprint $before.FullName);After=(Fingerprint $after)}
    }
    Require (@($parity).Count -eq 8) 'Eight Current baselines missing.'
    $before=ReadJson 'artifacts/p5o2b12/before-reference-gap.json'
    Require ($before.References.Gap -and $before.References.WorkerFailure.WorkerFailure.Code -eq 'compilationFailure') 'Actual unchanged before-gap witness missing.'
    $runtime=foreach($name in @('external-v1','external-v2','target-identity')) {
        $path=Join-Path $validation ('artifacts/p5o2b12/'+$name+'.json')
        $value=ReadJson $path
        Require ((Json $value.Native.Result) -ceq (Json $value.Historical) -and (Json $value.Historical) -ceq (Json $value.RepeatedHistorical)) "Full canonical flow/repetition differs: $name"
        Require ($value.Native.RepeatedFindingsEqual -and $value.ProcessesStopped) "Current findings/cleanup incomplete: $name"
        Require ($value.Graph.ActualHistoricalHost -and $value.Graph.RepeatedResultEqual -and -not $value.Graph.SubmittedSourceExecuted) "Historical graph host incomplete: $name"
        Require ((Json $value.Native.GraphSummaries) -ceq (Json $value.Graph.Summaries) -and (Json $value.Graph.Summaries) -ceq (Json $value.Graph.RepeatedSummaries)) "Complete graph summaries differ: $name"
        Require ((Json $value.Graph.Result) -ceq (Json $value.Historical)) 'Diagnostic Historical result differs from genuine Worker.'
        if($name.StartsWith('external')) {
            Require ($value.OriginalP5CAndP5E -and $value.AutomaticProjection -and $value.NoCurrentFallback -and -not $value.HistoricalLoadedInMain) 'Original production/isolation handoff incomplete.'
            Require ($value.Identity.ReferenceProfile -ceq 'validated-pe-images-v1' -and $value.Provenance.ReferenceProfile -ceq 'validated-pe-images-v1' -and $value.Provenance.ReferenceWorldSha256.Length -eq 64) 'Exact reference world not bound.'
            foreach($engine in $value.Identity.LoadedRoslyn) { Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256 -and $value.Graph.CompilerMvids -contains $engine.Mvid) 'Historical runtime identity differs.' }
            foreach($processId in @($value.FirstProcess,$value.SecondProcess)) {
                Require ($null -ne $processId) 'Production process evidence missing.'
                $process=Get-Process -Id $processId -ErrorAction SilentlyContinue
                Require ($null -eq $process -or $process.HasExited) 'Historical Worker still running.'
            }
        }
        [ordered]@{Name=$name;Value=$value;Artifact=(Fingerprint $path)}
    }
    $aliases=ReadJson (Join-Path $validation 'artifacts/p5o2b12/aliases-order.json')
    Require ($aliases.OrderRetained -and $aliases.AliasRetained -and $aliases.OriginalProvenance.ReferenceWorldSha256 -cne $aliases.ReorderedProvenance.ReferenceWorldSha256) 'Alias/order binding missing.'
    Require ((Json $aliases.Native.Result) -ceq (Json $aliases.Historical) -and (Json $aliases.Historical) -ceq (Json $aliases.ReorderedHistorical)) 'Alias/order full flow differs.'
    $missing=ReadJson (Join-Path $validation 'artifacts/p5o2b12/missing-reference.json')
    Require ($missing.ExistingCompletenessContract -and ($missing.CurrentErrors -join '') -match 'CS0246' -and ($missing.HistoricalFailure.WorkerFailure.Details -join '') -match 'CS0246') 'Missing-reference compiler behavior not demonstrated.'
    $report='Evaluation/P5O2B12-historical-reference-identity-equivalence.md'
    $matrix=foreach($line in Get-Content $report -Encoding UTF8) {
        if($line.StartsWith('| ') -and -not $line.StartsWith('| B11') -and -not $line.StartsWith('| ---')) {
            $parts=$line.Split('|')
            if($parts.Length -eq 10) { [ordered]@{Input=$parts[1].Trim();Current=$parts[2].Trim();HistoricalBeforeB12=$parts[3].Trim();SemanticRelevance=$parts[4].Trim();TrustBoundary=$parts[5].Trim();NeutralTransport=$parts[6].Trim();B12Measure=$parts[7].Trim();Equivalence=$parts[8].Trim()} }
        }
    }
    Require (@($matrix).Count -eq 10) 'B11-derived ten-row Reference/Identity submatrix missing.'
    $owned=$built+@($report,'Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B12Proof/Run-Regressions.ps1','Evaluation/P5O2B12Proof/RegressionFilters.json','Evaluation/P5O2B12Proof/Write-Verify-ReferenceEvidence.ps1')
    $utf8=New-Object Text.UTF8Encoding($false,$true)
    foreach($path in $owned) {
        $text=$utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF8/CRLF failed: $path"
        if($path -like '*.json') { $text | ConvertFrom-Json | Out-Null }
        if($path -like '*.ps1') {
            $errors=$null
            [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$errors) | Out-Null
            Require ($errors.Count -eq 0) "PowerShell parse failed: $path"
        }
    }
    $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $format=dotnet format whitespace . --folder --include $sources --verify-no-changes 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    $format | Set-Content artifacts/p5o2b12/format-final.log -Encoding UTF8
    Require ($code -eq 0) 'Folder-only format failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace check failed.'
    $attempts=ReadJson 'artifacts/p5o2b12/attempts.json'
    $audit=[ordered]@{
        Schema='P5O2B12-reference-identity-equivalence-v1';Date='2026-10-11';Head=$initial.Head;Matrix=@($matrix)
        InitialHeadMovement='Separately committed B11: 37c7b286 -> bb204a8946; audited before B12';ExternalHeadMovementDuringTask=$false
        Closed='Original complete ordered validated single-module assembly PE images, aliases, referenced full identity/MVID and original unsigned target name/full identity/module postcondition, bounded exact profile only'
        Open=@('signed targets / B13 compilation options','netmodule/embedded interop and multi-module configuration','XML providers/sidecar contracts without original validated handoff','B13 complete source/compiler/project/supporting scope and root families','Historical Reporting Import and CLI/discovery/callsite adoption')
        Protocol=[ordered]@{Version=2;Additive=$true;Profile='validated-pe-images-v1';Legacy='net8-runtime-bounded-v1 unchanged';ReferenceCount=64;ReferenceBytes=33554432;ExactRequestCharacters=50331648;LegacyRequestCharacters=65536;NoAmbientReferencesInExactProfile=$true;HashIsTransportBindingNotBuildValidator=$true;ActualHistoricalMetadataIdentityPostconditions=$true;UnknownDuplicateContradictoryRejected=$true;NoPathIdentityHeuristic=$true}
        BeforeGap=$before.References;Runtime=@($runtime);AliasesOrder=$aliases;MissingReference=$missing;CurrentParity=@($parity);Tests=$tests;RegressionCoverage=@($coverage);BuildLogs=@($logs);Attempts=$attempts
        Architecture=[ordered]@{SharedAnalyzerSources=121;HistoricalHostSources=1;AnalyzerAndCurrentUnchanged=$true;B5ThroughB11TestsUnchanged=$true;VersionsSelectionRoutingUnchanged=$true;NoSecondPePdbParserValidator=$true;MainReceiptRetentionOnly=$true;HistoricalObjectsOnlyInChild=$true;NoCurrentFallback=$true;NoInputExecution=$true}
        FindingsScope='Eight fresh complete Current before/after Finding/graph JSON comparisons; positive cases retain full Current Findings. Cross-engine Finding import explicitly excluded and not claimed.'
        GraphScope='Full canonical summaries including body/source/edge/context/uncertainty for every tested node and repeat, inside existing isolated diagnostic fixture; no production graph import.'
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;InheritedFrom='B11/B10';FreshSelfAnalysis=$false;FreshCanonicalDiff=$false}
        Quality=[ordered]@{Format=$true;Json=$true;Utf8CrLf=$true;PowerShellSyntax=$true;GitDiffCheck=$true;PermanentArchitectureIsolationGate=$true;EvidenceVerifier=$true}
        Protection=[ordered]@{RootIgnoreUnchanged=$true;CoreFiles=23;TrackedSources=$initial.TrackedSources.Count;Stashes=6;IndexUnchanged=$true;NoCommit=$true;NoPush=$true}
        OwnedFiles=@($owned | ForEach-Object {Fingerprint $_});GitStatus=@(git status --short)
        Validation='Local Windows clean Git export plus only owned overlay, original Core outputs untouched. No Linux/remote run claimed.'
    }
    $jsonPath='Evaluation/P5O2B12-historical-reference-identity-equivalence-audit.json'
    $text=($audit | ConvertTo-Json -Depth 90) -replace "`r?`n","`r`n"
    [IO.File]::WriteAllText((Join-Path $toolRoot $jsonPath),$text+"`r`n",(New-Object Text.UTF8Encoding($false)))
    ReadJson $jsonPath | Out-Null
    Write-Host 'PASS: B12 exact bounded reference/identity handoff, full flow/graph differentials, native Current parity, all regressions, architecture/isolation and repository protection.'
} finally { Pop-Location }
