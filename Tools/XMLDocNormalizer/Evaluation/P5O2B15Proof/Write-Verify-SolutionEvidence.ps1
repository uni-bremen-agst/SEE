param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
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
            Require ($start -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale run: $Name"
        }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b15/protected-before.json'
    $export=ReadJson 'artifacts/p5o2b15/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -eq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Recorded clean Git export required.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'HEAD/branch moved; explicit audit required.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected root ignore changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -ceq ($initial.Stashes -join "`n")) 'Stashes changed.'
    Require ((@(git diff --cached --name-only) -join "`n") -ceq ($initial.Index -join "`n")) 'Index changed.'
    $built=ReadJson 'artifacts/p5o2b15/owned-build-inputs.json'
    Require ($built.Count -eq 12) 'Twelve exact owned build inputs required.'
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})
    foreach($file in @($initial.TrackedSources)+@($initial.CoreFiles)) {
        if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/protected source changed: $($file.Path)" }
    }
    Require ($initial.CoreFiles.Count -eq 23 -and @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core file set changed.'
    Require ($initial.Stashes.Count -eq 6) 'Six protected stashes required.'
    foreach($file in $built) { Require ((Get-FileHash -LiteralPath $file).Hash -ceq (Get-FileHash -LiteralPath (Join-Path $validation $file)).Hash) "Built input mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Analyzer/host/build-image boundary changed.'
    foreach($file in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash -LiteralPath (Join-Path $validation $file.Path)).Hash -ceq $file.Sha256) 'Built shared analyzer changed.'
        Require ([IO.File]::ReadAllText((Join-Path $validation $file.Path)).Replace("`r`n","`n") -ceq [IO.File]::ReadAllText((Join-Path $toolRoot $file.Path)).Replace("`r`n","`n")) 'Shared analyzer export differs.'
        $before=@($parent.SharedSourceFingerprints | Where-Object {$_.Path -ceq $file.Path})
        Require ($before.Count -eq 1 -and $before[0].Sha256 -ceq $file.Sha256) 'Shared analyzer differs from B14 HEAD.'
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
    $tests=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 379)
    $census=[ordered]@{b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=617;broad=2918;full=3009}
    foreach($name in $census.Keys) { $tests+=@(Tests $name (Join-Path $validation ('artifacts/p5o2b15/tests/'+$name+'.trx')) $census[$name]) }
    $tests+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 407)
    $oldExport=ReadJson 'artifacts/p5o2b14/export.json'
    $coverage=foreach($group in @('productive','focused','broad','full')) {
        $oldPath=Join-Path $oldExport.ToolRoot ('artifacts/p5o2b14/tests/'+$group+'.trx')
        $newPath=Join-Path $validation ('artifacts/p5o2b15/tests/'+$group+'.trx')
        [xml]$old=Get-Content -LiteralPath $oldPath -Raw -Encoding UTF8; [xml]$new=Get-Content -LiteralPath $newPath -Raw -Encoding UTF8
        $oldNames=@($old.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object testName)
        $newNames=@($new.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object testName)
        $missing=@($oldNames | Where-Object {$newNames -cnotcontains $_})
        Require ($missing.Count -eq 0) "B14 regression omitted: $group"
        [ordered]@{Group=$group;OriginalCount=$oldNames.Count;CurrentCount=$newNames.Count;MissingCases=@($missing);OriginalTrx=(Fingerprint $oldPath)}
    }
    $parity=foreach($before in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$before.Name)
        Require ((Get-FileHash -LiteralPath $before.FullName).Hash -ceq (Get-FileHash -LiteralPath $after).Hash) "Complete Current JSON byte drift: $($before.Name)"
        [ordered]@{Case=$before.Name;CompleteJsonEqual=$true;Before=(Fingerprint $before.FullName);After=(Fingerprint $after)}
    }
    Require (@($parity).Count -eq 8) 'Eight complete Current baselines required.'
    $before=ReadJson 'artifacts/p5o2b15/before-cross-project-gap.json'
    Require ($before.ActualSourceRegistry -and $before.RepeatedEqual -and $before.ExplicitUnsupported -and (Json $before.ProjectFlow) -cne (Json $before.SolutionFlow)) 'Fresh parent cross-project witness missing.'
    $workerPath=[IO.Path]::GetFullPath((Join-Path $validation 'artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll'))
    $reusedProcessIds=New-Object 'System.Collections.Generic.List[object]'
    $runtime=foreach($path in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b15') -File -Filter 'solution-*.json') {
        $value=ReadJson $path.FullName
        Require ((Json $value.Native.Result) -ceq (Json $value.Historical) -and (Json $value.Historical) -ceq (Json $value.RepeatedHistorical)) 'Complete canonical flow/source/uncertainty drift.'
        Require ($value.Native.RepeatedFindingsEqual -and @($value.Native.Findings).Count -gt 0 -and $value.ProcessesStopped -and $value.NoCurrentFallback -and $value.OriginalReceipts -and -not $value.HistoricalLoadedInMain -and -not $value.HistoricalFindingsImport) 'Finding/handoff/isolation proof missing.'
        Require ($value.Graph.ActualHistoricalHost -and $value.Graph.RepeatedResultEqual -and $value.Graph.ForeignTreeRejected -and $value.Graph.ForeignBindingRejected -and -not $value.Graph.SubmittedSourceExecuted) 'Actual Historical ownership proof missing.'
        Require ((Json $value.Native.Summaries) -ceq (Json $value.Graph.Summaries) -and (Json $value.Graph.Summaries) -ceq (Json $value.Graph.RepeatedSummaries)) 'Full canonical graph summary drift.'
        Require ((Json $value.Graph.Result) -ceq (Json $value.Historical) -and $value.Native.GraphCount -eq $value.Graph.GraphCount -and $value.Graph.GraphCount -eq $value.Graph.RepeatedGraphCount) 'Graph/result/repetition drift.'
        Require ((Json $value.Native.Ownership) -ceq (Json $value.Graph.Ownership) -and @($value.Graph.Ownership).Count -eq $value.ProjectCount) 'Source/project ownership drift.'
        $nodes=@($value.Payload.Context.Solution.Projects)
        Require ($nodes.Count -eq $value.ProjectCount -and (Json $value.Payload.Context.Solution.Target) -ceq (Json $nodes[0].Identity)) 'Exact typed target/project graph missing.'
        $edges=0
        foreach($node in $nodes) {
            Require ($node.Input.Context.Scope.AssemblyName -ceq $node.Input.Context.AssemblyName -and $node.Input.Context.Scope.RootLogicalPath -ceq $node.Input.LogicalPath) 'Local B14 ownership lost.'
            foreach($dependency in $node.Dependencies) {
                $match=@($node.Input.Context.References | Where-Object {(Json $_.Identity) -ceq (Json $dependency.Assembly) -and $_.ModuleName -ceq $dependency.ModuleName -and $_.ModuleVersionId -ceq $dependency.ModuleVersionId})
                Require ($match.Count -eq 1 -and @($nodes | Where-Object {(Json $_.Identity) -ceq (Json $dependency)}).Count -eq 1) 'Dependency not bound to original exact reference and project.'
                $edges++
            }
        }
        Require ($edges -eq $value.ProjectCount-1 -and $value.Provenance.ReferenceWorldSha256.Length -eq 64 -and $value.Provenance.AnalysisMode -ceq 'solution-transitive') 'Edges/context provenance missing.'
        foreach($engine in $value.Identity.LoadedRoslyn) { Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256 -and $value.Graph.CompilerMvids -contains $engine.Mvid) 'Historical runtime differs.' }
        foreach($processId in @($value.FirstProcess,$value.SecondProcess)) {
            Require ($null -ne $processId) 'Actual Worker PID missing.'
            $live=Get-CimInstance Win32_Process -Filter "ProcessId = $processId"
            if($live) {
                Require ($null -ne $live.CommandLine -and $live.CommandLine.Replace('/','\').IndexOf($workerPath,[StringComparison]::OrdinalIgnoreCase) -lt 0) 'Own Worker still alive or identity unavailable.'
                $reusedProcessIds.Add([ordered]@{RecordedWorkerPid=$processId;CurrentProcessName=$live.Name;NotRecordedWorker=$true})
            }
        }
        [ordered]@{Value=$value;Artifact=(Fingerprint $path.FullName)}
    }
    Require (@($runtime).Count -eq 3) 'Three full genuine differentials required.'
    $controls=foreach($mode in @('Direct','ProjectTransitive')) {
        $value=ReadJson (Join-Path $validation ('artifacts/p5o2b15/local-'+$mode+'.json'))
        Require ($value.GraphRefusedBeforeDispatch -and $value.NoScopeWidening -and $value.ProcessesStopped -and (Json $value.Native.Result) -cne (Json $value.Solution.Result)) 'Scope counterproof missing.'
        $value
    }
    $negatives=foreach($path in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b15') -File -Filter 'negative-*.json') {
        $value=ReadJson $path.FullName
        Require ($value.Failure.Code -ceq 'unsupportedInput' -and $value.NoWorker -and $value.NoFallback) 'Graph negative not typed fail closed.'
        [ordered]@{Value=$value;Artifact=(Fingerprint $path.FullName)}
    }
    Require (@($negatives).Count -eq 19) 'Nineteen structural negative cases required.'
    $receipts=ReadJson (Join-Path $validation 'artifacts/p5o2b15/receipts.json')
    Require ($receipts.CopiedRejected -and $receipts.UnvalidatedProvenanceRejected -and $receipts.MissingReferencesRejected -and $receipts.MissingSourcesRejected -and $receipts.NestedRejected -and $receipts.EmptyRejected -and $receipts.NoSecondValidator) 'Original receipt rejection missing.'
    $missing=ReadJson (Join-Path $validation 'artifacts/p5o2b15/missing-reachable-source.json')
    Require ($missing.ActualReachableSourceMissing -and $missing.NoPartialResult -and $missing.NoLocalOrCurrentFallback -and $missing.HistoricalFailure.WorkerFailure.Code -ceq 'analysisFailure') 'Actual absent reachable source was approximated.'
    $unsupported=ReadJson (Join-Path $validation 'artifacts/p5o2b15/compiler-response.json')
    Require ($unsupported.Failure.WorkerFailure.Code -ceq 'unsupportedContext' -and $unsupported.ActualCompilerRejection -and $unsupported.ForgedGraphWorldRejected -and $unsupported.ForeignPathRejected -and $unsupported.NoFallback -and $unsupported.ProcessesStopped) 'Child capability/forgery proof missing.'
    $report='Evaluation/P5O2B15-historical-cross-project-solution-scope-equivalence.md'
    $matrix=foreach($line in Get-Content $report -Encoding UTF8) {
        if($line.StartsWith('| ') -and -not $line.StartsWith('| Information') -and -not $line.StartsWith('| ---')) {
            $parts=$line.Split('|')
            if($parts.Length -eq 9) { [ordered]@{Information=$parts[1].Trim();Current=$parts[2].Trim();HistoricalBeforeB15=$parts[3].Trim();AnalysisRelevance=$parts[4].Trim();NeutralTransport=$parts[5].Trim();Measure=$parts[6].Trim();Trust=$parts[7].Trim()} }
        }
    }
    Require (@($matrix).Count -eq 12) 'Twelve-row preimplementation matrix missing.'
    $owned=$built+@($report,'Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B15Proof/Run-Regressions.ps1','Evaluation/P5O2B15Proof/Write-Verify-SolutionEvidence.ps1')
    $utf8=New-Object Text.UTF8Encoding($false,$true)
    foreach($path in $owned) {
        $bytes=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path); $text=$utf8.GetString($bytes)
        Require (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191)) "BOM: $path"
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF8/CRLF failed: $path"
        if($path -like '*.ps1') { $errors=$null; [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$errors) | Out-Null; Require ($errors.Count -eq 0) "PowerShell syntax: $path" }
    }
    $sources=@($built | Where-Object {$_ -like '*.cs'}); $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $format=dotnet format whitespace . --folder --include $sources --verify-no-changes 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    $format | Set-Content artifacts/p5o2b15/format-final.log -Encoding UTF8
    Require ($code -eq 0) 'Folder-only format failed.'
    git diff --check; Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $gitPaths=@(git ls-files); $caseCollisions=@($gitPaths | Group-Object { $_.ToLowerInvariant() } | Where-Object Count -gt 1)
    Require ($caseCollisions.Count -eq 0) 'Tracked path case collisions.'
    $attempts=ReadJson 'artifacts/p5o2b15/attempts.json'
    $crashPath=Join-Path $export.ExportRoot 'attempt-final-full-aborted/full.trx'
    [xml]$crash=Get-Content -LiteralPath $crashPath -Raw -Encoding UTF8
    $crashCounts=$crash.SelectSingleNode('//*[local-name()="Counters"]')
    $crashText=($crash.SelectNodes('//*[local-name()="RunInfo"]') | ForEach-Object Text) -join "`n"
    Require ($crash.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Failed' -and $crashCounts.passed -eq '277' -and $crashCounts.failed -eq '0') 'Retained aborted run census missing.'
    Require ($crashText -match 'AccessViolationException' -and $crashText -match 'ExternalMetadataReferenceFactoryTests.*MultiModuleAssembly_FailsClosed') 'Known unchanged native reader-lifetime crash witness missing.'
    $cleanup=ReadJson 'artifacts/p5o2b15/aborted-run-cleanup.json'
    Require ($cleanup.IdentityVerified -and $cleanup.ParentGone -and $cleanup.Stopped -and $cleanup.VerifiedGone -and $cleanup.NoOtherProcessTouched) 'Aborted own fixture cleanup not verified.'
    Require (@($attempts | Where-Object {$_.Name -ceq 'full-final-1' -and $_.NoCodeChangeForRetry -and $_.NoExclusions}).Count -eq 1) 'Explicit unchanged repeat record missing.'
    Require (@($attempts | Where-Object {$_.Name -ceq 'full-final-2' -and $_.Passed -eq 3009 -and $_.Failed -eq 0 -and $_.Skipped -eq 0}).Count -eq 1) 'Final complete unchanged repeat missing.'
    $audit=[ordered]@{
        Schema='P5O2B15-cross-project-solution-scope-equivalence-v1';DateUtc=[DateTime]::UtcNow.ToString('o');Head=$initial.Head;Matrix=@($matrix)
        Closed='Explicit original validated external project DAG; exact reference-bound edges; source/project/root ownership; actual PE-to-source rebinding; complete tested two/three-project flow/graph equivalence'
        Open=@('native Workspace/MSBuild/CLI acquisition and automatic source discovery','larger graphs, shared/linked logical paths and broader roots/options/reference forms','ProjectTransitiveDeclaredExceptions','signed targets and XML contracts','Historical Reporting Import')
        Protocol=[ordered]@{Version=2;MaximumProjects=4;TotalSourceCharacters=65536;AggregateSyntaxNodes=16384;TotalReferenceBytes=67108864;RequestCharacters=100663296;LocalB14LimitsUnchanged=$true;LegacyUnchanged=$true;ExplicitSolutionOnly=$true;FullContextBoundByHash=$true;NoPathInferredEdges=$true;NoScopeSubstitution=$true}
        BeforeGap=$before;Runtime=@($runtime);Controls=@($controls);Negatives=@($negatives);Receipts=$receipts;MissingReachableSource=$missing;UnsupportedCompilerResponse=$unsupported;ReusedProcessIds=@($reusedProcessIds.ToArray())
        CurrentParity=@($parity);Tests=$tests;Coverage=@($coverage);BuildLogs=@($logs);BuildAudit=(Fingerprint (Join-Path $gateRoot 'audit.json'));Attempts=$attempts
        RetainedNativeCrash=[ordered]@{Trx=(Fingerprint $crashPath);Completed=277;FailedAssertions=0;Cause='Unchanged ReadAssemblyFiles returns reader-backed AssemblyFile after PEReader.Dispose';Log=(Fingerprint (Join-Path $export.ExportRoot 'attempt-final-full-aborted/full.log'));Cleanup=$cleanup;UnchangedFullRepeatPassed=3009;NoTestsExcluded=$true;NoOldCodeRepaired=$true}
        Architecture=[ordered]@{SharedAnalyzerSources=121;HistoricalHostSources=1;SharedAnalyzerUnchanged=$true;B5B6RoutingSelectionUnchanged=$true;B10ThroughB14TestsUnchanged=$true;NoSecondValidator=$true;NoMainHistoricalLoad=$true;NoSourceExecution=$true;NeutralProtocol=$true;NoProductionReportingImport=$true}
        FindingsScope='Full native Findings and deterministic repetition recorded; all eight full Current baselines byteequal. Historical canonical flows and every tested graph summary fully equal. No cross-engine Finding import/parity claimed.'
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;InheritedFrom='B14/B13/B11/B10';FreshSelfAnalysis=$false;FreshCanonicalDiff=$false}
        Quality=[ordered]@{Format=$true;Json=$true;Utf8CrLf=$true;PowerShellSyntax=$true;GitDiffCheck=$true;PermanentArchitectureIsolationGate=$true;EvidenceVerifier=$true;TrackedCaseCollisions=0;LinuxExecution=$false}
        Protection=[ordered]@{RootIgnoreUnchanged=$true;CoreFiles=23;Stashes=6;IndexUnchanged=$true;NoCommit=$true;NoPush=$true;ExternalHeadMovement='B14 separately committed before B15: d7251cf4e4 audited';HeadMovedDuringB15=$false}
        OwnedFiles=@($owned | ForEach-Object {Fingerprint $_});GitStatus=@(git status --short);Validation='Windows clean HEAD Git export plus exact owned overlay; original Core outputs untouched; no Linux/remote run claimed.'
    }
    $path='Evaluation/P5O2B15-historical-cross-project-solution-scope-equivalence-audit.json'
    [IO.File]::WriteAllText((Join-Path $toolRoot $path),(($audit | ConvertTo-Json -Depth 95) -replace "`r?`n","`r`n")+"`r`n",(New-Object Text.UTF8Encoding($false)))
    ReadJson $path | Out-Null
    Write-Host 'PASS: B15 genuine cross-project differential, Current parity, regressions, dual builds, isolation and protection.'
} finally { Pop-Location }
