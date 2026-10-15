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
        if(-not $Name.StartsWith('parent-')) {
            $testStart=([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime()
            Require ($testStart -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale pre-final-gate test evidence: $Name"
        }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b13/protected-before.json'
    $export=ReadJson 'artifacts/p5o2b13/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -eq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Not recorded clean Git export.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'HEAD/branch moved; audit required.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected root ignore changed.'
    Require ((@(git stash list --format='%H %s') -join "`n") -ceq ($initial.Stashes -join "`n")) 'Stashes changed.'
    Require ((@(git diff --cached --name-only) -join "`n") -ceq ($initial.Index -join "`n")) 'Index changed.'
    $built=ReadJson 'artifacts/p5o2b13/owned-build-inputs.json'
    Require ($built.Count -eq 8) 'Eight owned build inputs required.'
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})
    foreach($file in @($initial.TrackedSources)+@($initial.CoreFiles)) {
        if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/protected input changed: $($file.Path)" }
    }
    Require ($initial.CoreFiles.Count -eq 23 -and @(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core file set changed.'
    Require ($initial.Stashes.Count -eq 6) 'Expected six protected stashes.'
    foreach($file in $built) { Require ((Get-FileHash -LiteralPath $file).Hash -ceq (Get-FileHash -LiteralPath (Join-Path $validation $file)).Hash) "Built input mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Build/runtime/boundary gates incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Shared analyzer/host boundary changed.'
    foreach($file in $gate.SharedSourceFingerprints) {
        Require ((Get-FileHash -LiteralPath (Join-Path $validation $file.Path)).Hash -ceq $file.Sha256) 'Built shared source changed.'
        Require ([IO.File]::ReadAllText((Join-Path $validation $file.Path)).Replace("`r`n","`n") -ceq [IO.File]::ReadAllText((Join-Path $toolRoot $file.Path)).Replace("`r`n","`n")) 'Shared analyzer export differs.'
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
    $tests=@(Tests 'parent-current' (Join-Path $validation 'artifacts/p5o2b13/tests/parent-before.trx') 8)
    $tests+=@(Tests 'parent-witness' (Join-Path $validation 'artifacts/p5o2b13/tests/parent-witness.trx') 9)
    $tests+=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 314)
    $census=[ordered]@{b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=558;broad=2859;full=2950}
    foreach($name in $census.Keys) { $tests+=@(Tests $name (Join-Path $validation ('artifacts/p5o2b13/tests/'+$name+'.trx')) $census[$name]) }
    $tests+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 348)
    $oldExport=ReadJson 'artifacts/p5o2b12/export.json'
    $coverage=foreach($group in @('productive','focused','broad')) {
        $oldPath=Join-Path $oldExport.ToolRoot ('artifacts/p5o2b12/tests/'+$group+'.trx')
        $newPath=Join-Path $validation ('artifacts/p5o2b13/tests/'+$group+'.trx')
        [xml]$old=Get-Content -LiteralPath $oldPath -Raw -Encoding UTF8
        [xml]$new=Get-Content -LiteralPath $newPath -Raw -Encoding UTF8
        $oldNames=@($old.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object testName)
        $newNames=@($new.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object testName)
        $missing=@($oldNames | Where-Object {$newNames -cnotcontains $_})
        Require ($missing.Count -eq 0) "B12 regression omitted: $group"
        [ordered]@{Group=$group;OriginalCount=$oldNames.Count;CurrentCount=$newNames.Count;MissingCases=@($missing);OriginalTrx=(Fingerprint $oldPath)}
    }
    $parity=foreach($before in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$before.Name)
        Require ((Get-FileHash -LiteralPath $before.FullName).Hash -ceq (Get-FileHash -LiteralPath $after).Hash) "Full Current Finding/graph byte drift: $($before.Name)"
        [ordered]@{Case=$before.Name;CompleteJsonEqual=$true;Before=(Fingerprint $before.FullName);After=(Fingerprint $after)}
    }
    Require (@($parity).Count -eq 8) 'Eight Current baselines missing.'
    $before=ReadJson 'artifacts/p5o2b13/before-input-gaps.json'
    Require ($before.Defines.Gap -and $before.Sources.Gap -and $before.Language.CSharp10[0] -match 'CS8936' -and $before.Nullable.FlowEqual) 'Actual parent gap witnesses missing.'
    $runtime=foreach($language in @('10','12')) { foreach($defined in 0..2) {
        $name='parse-'+$language+'-'+$defined
        $path=Join-Path $validation ('artifacts/p5o2b13/'+$name+'.json')
        $value=ReadJson $path
        Require ((Json $value.Native.Result) -ceq (Json $value.Historical) -and (Json $value.Historical) -ceq (Json $value.RepeatedHistorical)) "Full canonical flow/source/uncertainty differs: $name"
        Require ($value.Native.RepeatedFindingsEqual -and $value.ProcessesStopped -and $value.NoCurrentFallback -and $value.OriginalP5GP5IP5CP5E -and -not $value.HistoricalLoadedInMain) "Handoff/findings/isolation missing: $name"
        if($defined -gt 0) { Require (@($value.Native.Findings).Count -gt 0) "Non-empty full Current Finding proof missing: $name" }
        Require ($value.Graph.ActualHistoricalHost -and $value.Graph.RepeatedResultEqual -and -not $value.Graph.SubmittedSourceExecuted) 'Not actual Historical host.'
        Require ((Json $value.Native.Summaries) -ceq (Json $value.Graph.Summaries) -and (Json $value.Graph.Summaries) -ceq (Json $value.Graph.RepeatedSummaries)) 'Full canonical graph summary drift.'
        Require ((Json $value.Graph.Result) -ceq (Json $value.Historical) -and $value.Native.GraphCount -eq $value.Graph.GraphCount -and $value.Graph.GraphCount -eq $value.Graph.RepeatedGraphCount) 'Graph/result ownership/repetition drift.'
        Require ($value.Provenance.LanguageVersion -ceq $language -and $value.Provenance.ReferenceWorldSha256.Length -eq 64) 'Parse context not bound.'
        foreach($engine in $value.Identity.LoadedRoslyn) { Require ($gate.HistoricalCompilerReferences.Sha256 -contains $engine.Sha256 -and $value.Graph.CompilerMvids -contains $engine.Mvid) 'Historical runtime identity mismatch.' }
        Assert-OwnedProcessPair $value.FirstProcessExit $value.SecondProcessExit $value.FirstProcess $value.SecondProcess
        [ordered]@{Name=$name;Value=$value;Artifact=(Fingerprint $path)}
    } }
    $language=ReadJson (Join-Path $validation 'artifacts/p5o2b13/language-admissibility.json')
    Require ($language.Current10.Errors[0] -match 'CS8936' -and ($language.Historical10.WorkerFailure.Details -join '') -match 'CS8936' -and $language.NoDowngrade) 'Language admissibility witness missing.'
    Require ((Json $language.Current12.Result) -ceq (Json $language.Historical12)) 'Language12 full flow differs.'
    $unsupported=ReadJson (Join-Path $validation 'artifacts/p5o2b13/unsupported-language.json')
    Require ($unsupported.Failure.WorkerFailure.Code -ceq 'unsupportedContext' -and $unsupported.NoFallback -and $unsupported.NoDowngrade -and $unsupported.ProcessStopped) 'Actual unsupported compiler capability missing.'
    $report='Evaluation/P5O2B13-historical-source-compiler-scope-equivalence.md'
    $matrix=foreach($line in Get-Content $report -Encoding UTF8) {
        if($line.StartsWith('| ') -and -not $line.StartsWith('| Input') -and -not $line.StartsWith('| ---')) {
            $parts=$line.Split('|')
            if($parts.Length -eq 10) { [ordered]@{Input=$parts[1].Trim();Current=$parts[2].Trim();HistoricalBeforeB13=$parts[3].Trim();SemanticRelevance=$parts[4].Trim();NeutralTransport=$parts[5].Trim();B13Action=$parts[6].Trim();Status=$parts[7].Trim();Trust=$parts[8].Trim()} }
        }
    }
    Require (@($matrix).Count -eq 20) 'B11-derived twenty-row submatrix missing.'
    $owned=$built+@($report,'Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B13Proof/Run-Regressions.ps1','Evaluation/P5O2B13Proof/Write-Verify-ContextEvidence.ps1')
    $utf8=New-Object Text.UTF8Encoding($false,$true)
    foreach($path in $owned) {
        $text=$utf8.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $path).Path))
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF8/CRLF failed: $path"
        if($path -like '*.ps1') { $errors=$null; [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$errors) | Out-Null; Require ($errors.Count -eq 0) "PowerShell syntax: $path" }
    }
    $sources=@($built | Where-Object {$_ -like '*.cs'})
    $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $format=dotnet format whitespace . --folder --include $sources --verify-no-changes 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    $format | Set-Content artifacts/p5o2b13/format-final.log -Encoding UTF8
    Require ($code -eq 0) 'Folder-only format failed.'
    git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace check failed.'
    $audit=[ordered]@{
        Schema='P5O2B13-source-compiler-scope-equivalence-v1';Date='2026-10-11';Head=$initial.Head;Matrix=@($matrix)
        Closed='Explicit original P5G/P5I single-source regular parse context: exact concrete language and ordered defines, within unchanged unsigned/exact-reference/compilation/scope profile'
        Open=@('complete ordered/generated multi-source ownership','project/solution/supporting-source dependency scopes and full root signatures','nullable/unsafe/checked/output/platform/optimization outside admitted policy','parse feature/preview configurations','signed targets and unsupported reference forms','XML contracts','Historical Reporting Import','CLI and call-site adoption')
        Protocol=[ordered]@{Version=2;Additive=$true;ParseProfile='validated-parse-v1';LegacyUnchanged=$true;OriginalParseReceiptRequired=$true;ActualCompilerCapabilityCheckedInChild=$true;UnknownDuplicateMissingContradictoryRejected=$true;NoDowngrade=$true;NoCurrentFallback=$true;SymbolCount=128;SymbolLength=256;ContextBoundByExistingReferenceWorldHash=$true}
        Scope=[ordered]@{Direct='unsupported';ProjectTransitive='unsupported';SolutionTransitive='equivalent only within complete single-tree local graph';ProjectTransitiveDeclaredExceptions='unsupported; XML excluded';FullProjectSolutionScope='separate architecture family'}
        BeforeGaps=$before;Runtime=@($runtime);Language=$language;UnsupportedLanguage=$unsupported;CurrentParity=@($parity);Tests=$tests;Coverage=@($coverage);BuildLogs=@($logs)
        Attempts=(ReadJson 'artifacts/p5o2b13/attempts.json')
        Architecture=[ordered]@{SharedAnalyzerSources=121;HistoricalHostSources=1;AnalyzerCurrentVersionsRoutingSelectionUnchanged=$true;B5ThroughB12TestsUnchanged=$true;NoSecondPePdbReferenceValidator=$true;NoMainHistoricalLoad=$true;HistoricalObjectsOnlyInChild=$true;NoSourceExecution=$true;NoProductionGraphReportingImport=$true}
        FindingsScope='Eight fresh full Current Finding/graph baseline comparisons and complete repeat findings in positive cases; cross-engine Historical Finding import explicitly excluded and not claimed.'
        GraphScope='Every tested canonical summary/body/context/source/edge/uncertainty equals Current and repeats in actual isolated Historical fixture; not full project/solution closure.'
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;InheritedFrom='B11/B10';FreshSelfAnalysis=$false;FreshCanonicalDiff=$false}
        Quality=[ordered]@{Format=$true;Json=$true;Utf8CrLf=$true;PowerShellSyntax=$true;GitDiffCheck=$true;PermanentArchitectureIsolationGate=$true;EvidenceVerifier=$true}
        Protection=[ordered]@{RootIgnoreUnchanged=$true;CoreFiles=23;Stashes=6;IndexUnchanged=$true;NoCommit=$true;NoPush=$true;ExternalHeadMovement='B12 separately committed before B13: 35d69be2a4 audited';HeadMovedDuringB13=$false}
        OwnedFiles=@($owned | ForEach-Object {Fingerprint $_});GitStatus=@(git status --short)
        Validation='Windows clean Git export with owned overlay; original workspace Core build outputs never used or changed. No remote/Linux run claimed.'
    }
    $path='Evaluation/P5O2B13-historical-source-compiler-scope-equivalence-audit.json'
    $text=($audit | ConvertTo-Json -Depth 95) -replace "`r?`n","`r`n"
    [IO.File]::WriteAllText((Join-Path $toolRoot $path),$text+"`r`n",(New-Object Text.UTF8Encoding($false)))
    ReadJson $path | Out-Null
    Write-Host 'PASS: bounded B13 parse context, full flow/graph differential, Current parity, regressions, builds, isolation and repository protection.'
} finally { Pop-Location }
