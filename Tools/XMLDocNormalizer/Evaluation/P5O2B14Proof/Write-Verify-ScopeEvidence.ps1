param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../PIDG1Proof/ProcessLifetime.ps1')
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $toolRoot
try {
    function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 90 -Compress }
    function Tests([string]$Name,[string]$Path,[int]$Expected) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Aborted/failed run: $Name"
        Require ($counts.total -eq "$Expected" -and $counts.total -eq $counts.passed -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Unexpected/failed/skipped census: $Name"
        if($Name -ne 'parent-boundary') {
            $start=([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime()
            Require ($start -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale pre-final-build run: $Name"
        }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b14/protected-before.json'
    $export=ReadJson 'artifacts/p5o2b14/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -eq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Recorded clean Git export required.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'HEAD/branch moved; explicit audit required.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected root ignore changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -ceq ($initial.Stashes -join "`n")) 'Stashes changed.'
    Require ((@(git diff --cached --name-only) -join "`n") -ceq ($initial.Index -join "`n")) 'Index changed.'
    $built=ReadJson 'artifacts/p5o2b14/owned-build-inputs.json'
    Require ($built.Count -eq 13) 'Thirteen exact owned build inputs required.'
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})
    foreach($file in @($initial.TrackedSources)+@($initial.CoreFiles)) {
        if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/protected source changed: $($file.Path)" }
    }
    Require ($initial.CoreFiles.Count -eq 23 -and @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core file set changed.'
    Require ($initial.Stashes.Count -eq 6) 'Expected six protected stashes.'
    foreach($file in $built) { Require ((Get-FileHash -LiteralPath $file).Hash -ceq (Get-FileHash -LiteralPath (Join-Path $validation $file)).Hash) "Built input mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary gates incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Analyzer/host/build-image boundary changed.'
    foreach($file in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash -LiteralPath (Join-Path $validation $file.Path)).Hash -ceq $file.Sha256) 'Built shared analyzer changed.'
        Require ([IO.File]::ReadAllText((Join-Path $validation $file.Path)).Replace("`r`n","`n") -ceq [IO.File]::ReadAllText((Join-Path $toolRoot $file.Path)).Replace("`r`n","`n")) 'Shared analyzer export differs.'
        $before=@($parent.SharedSourceFingerprints | Where-Object {$_.Path -ceq $file.Path})
        Require ($before.Count -eq 1 -and $before[0].Sha256 -ceq $file.Sha256) 'Shared analyzer differs from B13 HEAD.'
    }
    $logs=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed gate step: $($step.Name)"
        $path=Join-Path $gateRoot ($step.Name+'.log')
        if($step.Name.EndsWith('-build')) {
            $text=Get-Content -LiteralPath $path -Raw -Encoding UTF8
            Require ($text -match '(?m)^\s*0 (Warnung\(en\)|Warning\(s\))\s*$' -and $text -match '(?m)^\s*0 (Fehler|Error\(s\))\s*$') "Warnings/errors: $path"
        }
        Fingerprint $path
    }
    $tests=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 348)
    $census=[ordered]@{b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=589;broad=2890;full=2981}
    foreach($name in $census.Keys) { $tests+=@(Tests $name (Join-Path $validation ('artifacts/p5o2b14/tests/'+$name+'.trx')) $census[$name]) }
    $tests+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 379)
    $oldExport=ReadJson 'artifacts/p5o2b13/export.json'
    $coverage=foreach($group in @('productive','focused','broad','full')) {
        $oldPath=Join-Path $oldExport.ToolRoot ('artifacts/p5o2b13/tests/'+$group+'.trx')
        $newPath=Join-Path $validation ('artifacts/p5o2b14/tests/'+$group+'.trx')
        [xml]$old=Get-Content -LiteralPath $oldPath -Raw -Encoding UTF8; [xml]$new=Get-Content -LiteralPath $newPath -Raw -Encoding UTF8
        $oldNames=@($old.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object testName)
        $newNames=@($new.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object testName)
        $missing=@($oldNames | Where-Object {$newNames -cnotcontains $_})
        Require ($missing.Count -eq 0) "B13 regression omitted: $group"
        [ordered]@{Group=$group;OriginalCount=$oldNames.Count;CurrentCount=$newNames.Count;MissingCases=@($missing);OriginalTrx=(Fingerprint $oldPath)}
    }
    $parity=foreach($before in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$before.Name)
        Require ((Get-FileHash -LiteralPath $before.FullName).Hash -ceq (Get-FileHash -LiteralPath $after).Hash) "Full Current Finding/graph byte drift: $($before.Name)"
        [ordered]@{Case=$before.Name;CompleteJsonEqual=$true;Before=(Fingerprint $before.FullName);After=(Fingerprint $after)}
    }
    Require (@($parity).Count -eq 8) 'Eight complete Current baselines required.'
    $before=ReadJson 'artifacts/p5o2b14/before-input-gaps.json'
    Require ($before.Sources.Gap -and @($before.Sources.Native.Result.Entries).Count -gt 0 -and $before.Sources.WorkerFailure.WorkerFailure.Code -ceq 'compilationFailure') 'Executed parent supporting-source gap missing.'
    $runtime=foreach($path in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b14') -File -Filter 'scope-*.json') {
        $value=ReadJson $path.FullName
        Require ((Json $value.Native.Result) -ceq (Json $value.Historical) -and (Json $value.Historical) -ceq (Json $value.RepeatedHistorical)) 'Full canonical flow/source/uncertainty drift.'
        Require ($value.Native.RepeatedFindingsEqual -and $value.ProcessesStopped -and $value.NoCurrentFallback -and $value.OriginalP5GP5IP5JP5CP5E -and -not $value.HistoricalLoadedInMain) 'Findings/handoff/isolation proof missing.'
        if($value.Throwing -and ($value.Mode -ne 'direct' -or $value.SourceCount -eq 1)) { Require (@($value.Native.Findings).Count -gt 0) 'Nonempty Current Finding proof required.' }
        Require ($value.Graph.ActualHistoricalHost -and $value.Graph.RepeatedResultEqual -and $value.Graph.ForeignTreeRejected -and $value.Graph.ForeignBindingRejected -and -not $value.Graph.SubmittedSourceExecuted) 'Actual stable Historical ownership proof missing.'
        Require ((Json $value.Native.Summaries) -ceq (Json $value.Graph.Summaries) -and (Json $value.Graph.Summaries) -ceq (Json $value.Graph.RepeatedSummaries)) 'Full canonical graph summary drift.'
        Require ((Json $value.Graph.Result) -ceq (Json $value.Historical) -and $value.Native.GraphCount -eq $value.Graph.GraphCount -and $value.Graph.GraphCount -eq $value.Graph.RepeatedGraphCount) 'Graph/result/repetition drift.'
        Require ((Json $value.Native.Ownership) -ceq (Json $value.Graph.Ownership) -and @($value.Graph.Ownership).Count -eq $value.SourceCount) 'Source/project ownership drift.'
        Require ($value.Payload.Context.Scope.Kind -ceq 'single-project-v1' -and $value.Payload.Context.Scope.AssemblyName -ceq $value.Payload.Context.AssemblyName -and $value.Payload.Context.Scope.RootLogicalPath -ceq $value.Payload.LogicalPath) 'Explicit original project/root binding missing.'
        Require ($value.Provenance.AnalysisMode -ceq $value.Mode -and $value.Provenance.ReferenceWorldSha256.Length -eq 64) 'Scope not bound to response.'
        foreach($engine in $value.Identity.LoadedRoslyn) { Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256 -and $value.Graph.CompilerMvids -contains $engine.Mvid) 'Historical runtime differs.' }
        Assert-OwnedProcessPair $value.FirstProcessExit $value.SecondProcessExit $value.FirstProcess $value.SecondProcess
        [ordered]@{Value=$value;Artifact=(Fingerprint $path.FullName)}
    }
    Require (@($runtime).Count -eq 6) 'Six complete positive differentials required.'
    $declared=ReadJson (Join-Path $validation 'artifacts/p5o2b14/declared-mode.json')
    Require ($declared.ActualDetectorGuardDifference -and $declared.HistoricalExplicitlyUnsupported -and @($declared.Normal.Result.Entries).Count -gt 0 -and @($declared.Declared.Result.Entries).Count -eq 0 -and @($declared.Normal.Findings).Count -gt 0 -and @($declared.Declared.Findings).Count -eq 0) 'Actual declared-mode difference missing.'
    $cross=ReadJson (Join-Path $validation 'artifacts/p5o2b14/cross-project-gap.json')
    Require ($cross.ActualSourceRegistry -and $cross.RepeatedEqual -and $cross.ExplicitUnsupported -and (Json $cross.ProjectFlow) -cne (Json $cross.SolutionFlow) -and @($cross.SolutionFlow.Entries).Count -gt 0) 'Actual cross-project closure gap missing.'
    $unsupported=ReadJson (Join-Path $validation 'artifacts/p5o2b14/unsupported-source-language.json')
    Require ($unsupported.Failure.WorkerFailure.Code -ceq 'unsupportedContext' -and $unsupported.NoDowngrade -and $unsupported.NoFallback -and $unsupported.ProcessStopped) 'Actual child language rejection missing.'
    $receipts=ReadJson (Join-Path $validation 'artifacts/p5o2b14/original-receipts.json')
    Require ($receipts.CopiedRejected -and $receipts.IncompleteRejected -and $receipts.ForeignRejected -and $receipts.UnvalidatedP5IRejected) 'Original project receipts missing.'
    $response=ReadJson (Join-Path $validation 'artifacts/p5o2b14/response-and-root.json')
    Require ($response.ForeignRootRejected -and $response.ForgedWorldRejected -and $response.ForgedPathRejected -and $response.StrictUnknownDuplicates -and $response.ProcessesStopped) 'Response/root fail-closed evidence missing.'
    $report='Evaluation/P5O2B14-historical-supporting-source-scope-equivalence.md'
    $matrix=foreach($line in Get-Content $report -Encoding UTF8) {
        if($line.StartsWith('| ') -and -not $line.StartsWith('| Input') -and -not $line.StartsWith('| ---')) {
            $parts=$line.Split('|')
            if($parts.Length -eq 10) { [ordered]@{Input=$parts[1].Trim();Current=$parts[2].Trim();HistoricalBeforeB14=$parts[3].Trim();SemanticRelevance=$parts[4].Trim();Measure=$parts[5].Trim();InitialStatus=$parts[6].Trim();TrustBoundary=$parts[7].Trim();NeutralReconstruction=$parts[8].Trim()} }
        }
    }
    Require (@($matrix).Count -eq 12) 'Twelve-row initial scope matrix missing.'
    $owned=$built+@($report,'Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B14Proof/Run-Regressions.ps1','Evaluation/P5O2B14Proof/Write-Verify-ScopeEvidence.ps1')
    $utf8=New-Object Text.UTF8Encoding($false,$true)
    foreach($path in $owned) {
        $text=$utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF8/CRLF failed: $path"
        if($path -like '*.ps1') { $errors=$null; [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$errors) | Out-Null; Require ($errors.Count -eq 0) "PowerShell syntax: $path" }
    }
    $sources=@($built | Where-Object {$_ -like '*.cs'}); $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $format=dotnet format whitespace . --folder --include $sources --verify-no-changes 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    $format | Set-Content artifacts/p5o2b14/format-final.log -Encoding UTF8
    Require ($code -eq 0) 'Folder-only format failed.'
    git diff --check; Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $gitPaths=@(git ls-files); $caseCollisions=@($gitPaths | Group-Object { $_.ToLowerInvariant() } | Where-Object Count -gt 1)
    Require ($caseCollisions.Count -eq 0) 'Tracked path case collisions.'
    $attempts=ReadJson 'artifacts/p5o2b14/attempts.json'
    Require ($attempts.Count -eq 11) 'Retained attempt census malformed or incomplete.'
    Require (@($attempts | Where-Object {$_.Name -ceq 'dual-1' -and $_.Total -eq 378 -and $_.Failed -eq 2}).Count -eq 1) 'Retained first failed gate missing.'
    Require (@($attempts | Where-Object {$_.Name -ceq 'dual-final' -and $_.Passed -eq 379 -and $_.Failed -eq 0}).Count -eq 1) 'Final passed gate missing.'
    $audit=[ordered]@{
        Schema='P5O2B14-supporting-source-scope-equivalence-v1';DateUtc=[DateTime]::UtcNow.ToString('o');Head=$initial.Head;Matrix=@($matrix)
        Closed='Original complete P5J/P5I bounded single-project source membership, source/project/root ownership, per-source B13 parse and B12 reference identity, Direct/ProjectTransitive local scope'
        Modes=[ordered]@{Direct='equivalent tested local control';ProjectTransitive='equivalent complete local single-project graph';SolutionTransitive='equivalent only explicit complete single-project/local graph; cross-project open B15';ProjectTransitiveDeclaredExceptions='explicit unsupported; actual Current detector guard/filter difference evidenced'}
        Open=@('B15 exact dependency/source/assembly closure and supporting registry','declared-mode detector/reporting contract','arbitrary root signatures','compiler/parse options outside bounded B13 policy','signed targets and unsupported references','XML documentation contracts','Historical Reporting Import','CLI adoption')
        Protocol=[ordered]@{Version=2;ScopeKind='single-project-v1';MaximumSources=16;TotalSourceCharacters=32768;AggregateSyntaxNodes=8192;LegacyUnchanged=$true;ExplicitOptIn=$true;ExactOrderedManifest=$true;OriginalReceiptRequired=$true;NoScopeSubstitution=$true;ContextBoundByReferenceWorldHash=$true}
        BeforeGaps=$before.Sources;Runtime=@($runtime);LifetimeVerification='original owned exit observations, not PID/path heuristics';DeclaredMode=$declared;CrossProject=$cross;UnsupportedLanguage=$unsupported;Receipts=$receipts;Response=$response
        CurrentParity=@($parity);Tests=$tests;Coverage=@($coverage);BuildLogs=@($logs);BuildAudit=(Fingerprint (Join-Path $gateRoot 'audit.json'));Attempts=$attempts
        Architecture=[ordered]@{SharedAnalyzerSources=121;HistoricalHostSources=1;HistoricalHostChange='stable exact-tree model map in one compilation';SharedAnalyzerUnchanged=$true;B5B6RoutingSelectionUnchanged=$true;B10ThroughB13TestsUnchanged=$true;OneB2OwnershipAssertionUpdated=$true;NoSecondPePdbReferenceSourceValidator=$true;NoMainHistoricalLoad=$true;NoSourceExecution=$true;NoProductionGraphReportingImport=$true}
        FindingsScope='All eight native Current Finding/graph baselines byteequal; full native repeat Findings in positives and declared differential. Historical canonical flow/graph is fully compared; Historical Finding import remains explicitly excluded, so cross-engine Finding parity is not claimed.'
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;InheritedFrom='B13/B11/B10';FreshSelfAnalysis=$false;FreshCanonicalDiff=$false}
        Quality=[ordered]@{Format=$true;Json=$true;Utf8CrLf=$true;PowerShellSyntax=$true;GitDiffCheck=$true;PermanentArchitectureIsolationGate=$true;EvidenceVerifier=$true;TrackedCaseCollisions=0;LinuxExecution=$false}
        Protection=[ordered]@{RootIgnoreUnchanged=$true;CoreFiles=23;Stashes=6;IndexUnchanged=$true;NoCommit=$true;NoPush=$true;ExternalHeadMovement='B13 separately committed before B14: a8f65d39f2 audited';HeadMovedDuringB14=$false}
        OwnedFiles=@($owned | ForEach-Object {Fingerprint $_});GitStatus=@(git status --short);Validation='Windows clean Git export with exact owned overlay. Original Core outputs untouched; no remote/Linux execution claimed.'
    }
    $path='Evaluation/P5O2B14-historical-supporting-source-scope-equivalence-audit.json'
    [IO.File]::WriteAllText((Join-Path $toolRoot $path),(($audit | ConvertTo-Json -Depth 95) -replace "`r?`n","`r`n")+"`r`n",(New-Object Text.UTF8Encoding($false)))
    ReadJson $path | Out-Null
    Write-Host 'PASS: B14 complete bounded project scope, full flow/graph differential, Current parity, regressions, dual builds, isolation and protection.'
} finally { Pop-Location }
