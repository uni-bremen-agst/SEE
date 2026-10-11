param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $toolRoot
try {
    function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 90 -Compress }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function Equal($Left,$Right,[string]$Message) { Require ((Json $Left) -ceq (Json $Right)) $Message }
    function Tests([string]$Name,[string]$Path,[int]$Expected,[bool]$Fresh=$true) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Incomplete/failed run: $Name"
        Require ($counts.total -eq "$Expected" -and $counts.total -eq $counts.passed -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Failed/skipped census: $Name"
        if($Fresh) { Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale tests: $Name" }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b16/protected-before.json'
    $export=ReadJson 'artifacts/p5o2b16/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -ceq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Clean HEAD export required.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'External HEAD/branch movement requires explicit audit.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Root ignore changed.'
    Equal @(git stash list --format='%H %s') $initial.Stashes 'Stashes changed.'
    Equal @(git diff --cached --name-only) $initial.Index 'Index changed.'
    $buildInputs=ReadJson 'artifacts/p5o2b16/owned-build-inputs.json'
    Require ($buildInputs.Count -eq 2) 'Only new B16 tests and boundary filter are build inputs.'
    $allowed=@((Join-Path $toolRoot 'build/Verify-DualVersionBuild.ps1'),(Join-Path $toolRoot 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md'))
    foreach($file in $initial.TrackedFiles) {
        if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Tracked unrelated/production source changed: $($file.Path)" }
    }
    Require ($initial.TrackedFiles.Count -eq 896 -and $initial.CoreFiles.Count -eq 23 -and $initial.Stashes.Count -eq 6) 'Unexpected starting inventory.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core inventory changed.'
    foreach($file in $initial.CoreFiles) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'Protected Core byte change.' }
    foreach($file in $buildInputs) { Require ((Get-FileHash $file).Hash -ceq (Get-FileHash (Join-Path $validation $file)).Hash) "Export/workspace differ: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Shared/version boundary changed.'
    Equal $gate.SharedSourceFingerprints $parent.SharedSourceFingerprints 'Shared analyzer source bytes changed.'
    foreach($file in $gate.SharedSourceFingerprints) { Require ((Get-FileHash (Join-Path $validation $file.Path)).Hash -ceq $file.Sha256) 'Built shared source changed.' }
    foreach($image in $gate.BuildImages) {
        $before=@($parent.BuildImages | Where-Object {$_.Step -eq $image.Step -and $_.Owner -ceq $image.Owner})
        Require ($before.Count -eq 1 -and $image.Image.Sha256 -ceq $before[0].Image.Sha256 -and $image.Image.PdbSha256 -ceq $before[0].Image.PdbSha256) 'Production DLL/PDB differs from B15 baseline.'
    }
    Equal $gate.WorkerBuildImages $parent.WorkerBuildImages 'Worker DLL/PDB differs from B15 baseline.'
    $buildLogs=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed gate step: $($step.Name)"
        if($step.Name.EndsWith('-build')) {
            $path=Join-Path $gateRoot ($step.Name+'.log'); $text=Get-Content $path -Raw -Encoding UTF8
            Require ($text -match '0 Warnung\(en\)' -and $text -match '0 Fehler') "Not clean WAE: $($step.Name)"
            Fingerprint $path
        }
    }
    Require (@($buildLogs).Count -eq 11) 'Eleven WAE builds required.'
    $expected=[ordered]@{b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=635;broad=2936;full=3027}
    $census=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b16/tests/'+$name+'.trx')) $expected[$name] }
    $census+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 425)
    $census+=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 407 $false)
    $baselines=foreach($file in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $file.FullName).Hash -ceq (Get-FileHash $after).Hash) "Current baseline differs: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight complete Current baselines required.'
    $root=Join-Path $validation 'artifacts/p5o2b16'
    $cases=foreach($file in Get-ChildItem $root -File -Filter 'case-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.GenuineCurrentDetector -and $value.NoDeclaredHistoricalParityClaim -and $value.NoFindingImport -and $value.NoHistoricalLoadedInMain) 'Boundary claim/isolation incorrect.'
        Require ($value.OriginalFailure.Code -ceq 'unsupportedProfile' -and $value.RawFailure.Code -ceq 'unsupportedInput') 'Declared mode not fail closed.'
        Require ((Json $value.Project.Findings) -cne (Json $value.Declared.Findings)) 'No actual mode difference.'
        Equal $value.Project.Result $value.Solution.Result 'Project/solution control flow differs.'
        Equal $value.Project.Summaries $value.Solution.Summaries 'Project/solution summaries differ.'
        if($value.Scenario -in @('no-local-type','xml-only')) {
            Require (-not $value.Declared.HasDeclaredTypes -and $value.Declared.GraphCount -eq 0 -and $value.Declared.Findings.Count -eq 0) 'Guard witness incorrect.'
            Equal $value.Direct.Result $value.Declared.Result 'Guard is not actual Direct result.'
        } else {
            Require ($value.Declared.HasDeclaredTypes) 'Local type guard not activated.'
            Equal $value.Project.Result $value.Declared.Result 'Declared mode incorrectly filters raw flows.'
            Equal $value.Project.Summaries $value.Declared.Summaries 'Declared mode invented a distinct graph.'
        }
        Require ($value.Controls.Count -eq 3) 'Three unaffected mode controls required.'
        foreach($control in $value.Controls) {
            Require ($control.NoCurrentFallback -and $control.ProcessesStopped -and $control.Native.RepeatedFlowGraphAndFindingsEqual) 'Control repeat/cleanup missing.'
            Equal $control.Native.Result $control.Historical 'Full control flow/sources/uncertainties differ.'
            Equal $control.Historical $control.RepeatedHistorical 'Historical repeat differs.'
            Equal $control.Historical $control.Graph.Result 'Historical graph result differs.'
            Equal $control.Native.Summaries $control.Graph.Summaries 'Full graph summaries differ.'
            Equal $control.Graph.Summaries $control.Graph.RepeatedSummaries 'Graph repeat differs.'
            Equal @($control.Native.Ownership | ForEach-Object LogicalPath) @($control.Graph.Ownership | ForEach-Object LogicalPath) 'Owner paths differ.'
            Require ($control.Native.GraphCount -eq $control.Graph.GraphCount -and $control.Graph.GraphCount -eq $control.Graph.RepeatedGraphCount -and $control.Graph.RepeatedResultEqual -and $control.Graph.ForeignTreeRejected -and $control.Graph.ForeignBindingRejected) 'Graph state/foreign ownership mismatch.'
            Require ($control.Provenance.AnalysisMode -ceq $control.Mode -and $control.Provenance.ReferenceWorldSha256.Length -eq 64) 'Mode/world provenance missing.'
            foreach($owner in $control.Native.Ownership) { Require ($owner.StableModel -and $owner.Reporting) 'Native reporting ownership invalid.' }
            foreach($owner in $control.Graph.Ownership) { Require ($owner.StableModel -and $owner.Owned) 'Historical ownership invalid.' }
            foreach($compiler in $control.Identity.LoadedRoslyn) { Require ($gate.HistoricalCompilerReferences.Sha256 -contains $compiler.Sha256 -and $control.Graph.CompilerMvids -contains $compiler.Mvid) 'Not genuine pinned Historical runtime.' }
        }
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($cases).Count -eq 5) 'Five real Current differentials required.'
    $uncertainty=ReadJson (Join-Path $root 'uncertainty.json')
    Require ($uncertainty.Project.Result.Uncertainties.Count -gt 0 -and $uncertainty.NoPartialResult -and $uncertainty.ProcessesStopped -and $uncertainty.Failure.WorkerFailure.Code -ceq 'analysisFailure') 'Uncertainty/fail-closed witness missing.'
    Equal $uncertainty.Project.Result $uncertainty.Declared.Result 'Mode changed raw uncertainty.'
    Equal $uncertainty.Project.Summaries $uncertainty.Declared.Summaries 'Mode changed uncertain graph.'
    Require (@($uncertainty.Project.Findings | Where-Object {$_.Smell.ID -eq 'DOC631'}).Count -eq 2 -and @($uncertainty.Declared.Findings | Where-Object {$_.Smell.ID -eq 'DOC631'}).Count -eq 1) 'Uncertain reporting relevance witness missing.'
    $ownership=ReadJson (Join-Path $root 'reporting-ownership.json')
    foreach($property in $ownership.PSObject.Properties) { Require ($property.Value -eq $true) 'Reporting versus supporting source witness failed.' }
    $errors=ReadJson (Join-Path $root 'worker-errors.json')
    Require ($errors.ParseFailure.WorkerFailure.Code -ceq 'unsupportedContext' -and $errors.CompilationFailure.WorkerFailure.Code -ceq 'compilationFailure' -and $errors.ProcessesStopped -and $errors.NoFallback -and $errors.DeclaredNeverStarted) 'Real Worker errors missing.'
    $negatives=foreach($file in Get-ChildItem $root -File -Filter 'negative-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.Failure.Code -ceq 'unsupportedInput' -and $value.NoWorker -and $value.NoResult -and $value.NoFallback) 'Negative case not fail closed.'
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($negatives).Count -eq 10) 'Ten explicit structural/mode rejection cases required.'
    # Read process identity, not PID-only: completed Worker PIDs may be reused by unrelated processes.
    $live=@(Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($validation,[StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.CommandLine -match 'HistoricalWorker.dll|HistoricalPdbFixture.dll' })
    Require ($live.Count -eq 0) 'Own Historical child remains alive.'
    $owned=@($buildInputs)+@('Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B16-historical-declared-exceptions-boundary.md','Evaluation/P5O2B16Proof/Run-Regressions.ps1','Evaluation/P5O2B16Proof/Write-Verify-DeclaredBoundaryEvidence.ps1')
    $utf8=New-Object Text.UTF8Encoding($false,$true)
    foreach($path in $owned) {
        $bytes=[IO.File]::ReadAllBytes((Resolve-Path $path).Path); $text=$utf8.GetString($bytes)
        Require (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191)) "BOM: $path"
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF8/CRLF: $path"
        if($path -like '*.ps1') { $parseErrors=$null; [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$parseErrors) | Out-Null; Require ($parseErrors.Count -eq 0) "PS syntax: $path" }
    }
    # PS5 Get-Content decorates each string with PSDrive/PSProvider objects.
    # Strip that metadata before deep JSON serialization; retain only the ten text rows.
    $matrix=@(Get-Content Evaluation/P5O2B16-historical-declared-exceptions-boundary.md -Encoding UTF8 | Where-Object { $_.StartsWith('| ') -and $_.Split('|').Count -eq 7 -and -not $_.StartsWith('| Information') -and -not $_.StartsWith('| ---') } | ForEach-Object {[string]$_})
    Require ($matrix.Count -eq 10) 'Ten pre-implementation matrix rows required.'
    $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $format=dotnet format whitespace . --folder --include Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs --verify-no-changes 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    $format | Set-Content artifacts/p5o2b16/format-final.log -Encoding UTF8; Require ($code -eq 0) 'Folder-only format failed.'
    git diff --check; Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $collisions=@(git ls-files | Group-Object {$_.ToLowerInvariant()} | Where-Object Count -gt 1)
    Require ($collisions.Count -eq 0) 'Tracked path case collisions.'
    [xml]$first=Get-Content (Join-Path $root 'attempts/b16-first.trx') -Raw -Encoding UTF8
    $firstCounts=$first.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($firstCounts.total -eq '18' -and $firstCounts.passed -eq '17' -and $firstCounts.failed -eq '1' -and $firstCounts.notExecuted -eq '0') 'First test assumption failure not retained.'
    $attempts=ReadJson 'artifacts/p5o2b16/attempts.json'
    $audit=[ordered]@{
        Schema='P5O2B16-declared-detector-boundary-v1';DateUtc=[DateTime]::UtcNow.ToString('o');Head=$initial.Head;Matrix=$matrix
        Outcome='Analysis/boundary closure; Historical Declared remains unsupported. No equivalence success claim.'
        RootCause='Declared is Current detector guard plus reporting-type relevance policy, absent from the shared Historical analyzer and canonical Worker result contract; not XML-declared flow injection.'
        RequiredFollowUp='Shared executable detector/reporting policy, exact reporting/type-membership boundary and genuine Historical Finding differential; no guard-only alias or raw-flow filter.'
        ProductionChanges=0;ProtocolChanges=0;CanonicalGuardOnlySliceImplemented=$false
        CurrentDifferentials=$cases;Uncertainty=$uncertainty;ReportingOwnership=$ownership;Negatives=$negatives;WorkerErrors=$errors
        CurrentBaselines=$baselines;Tests=$census;DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'));WaeBuilds=$buildLogs;Attempts=$attempts
        FirstFailure=(Fingerprint (Join-Path $root 'attempts/b16-first.trx'))
        Architecture=[ordered]@{SharedAnalyzerCount=121;AllProductionFilesUnchanged=$true;AllProductionDllsAndPdbsByteEqual=$true;HostUnchanged=$true;B5B6Unchanged=$true;B10ThroughB15TestsUnchanged=$true;RoslynVersionsUnchanged=$true;NoSecondValidator=$true;NoRoslynIpc=$true;NoHistoricalLoadedInMain=$true;NoOwnWorkerRemaining=$true;NoFallback=$true;NoFindingImport=$true}
        Open=@('Historical ProjectTransitiveDeclaredExceptions detector/reporting policy','Historical Findings import and CLI adoption','validated external XML provider/sidecar contracts','signed targets and other unsupported references','broader scope/roots/compiler options')
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;InheritedFrom='B15/B14/B13/B11/B10';FreshSelfAnalysis=$false;FreshCanonicalDiff=$false}
        Quality=[ordered]@{Format=$true;Json=$true;Utf8CrLf=$true;PowerShellSyntax=$true;GitDiffCheck=$true;PermanentArchitectureIsolationGate=$true;EvidenceVerifier=$true;TrackedCaseCollisions=0;LinuxExecution=$false}
        Protection=[ordered]@{RootIgnoreUnchanged=$true;CoreFiles=23;Stashes=6;IndexUnchanged=$true;NoCommit=$true;NoPush=$true;ExternalHeadMovementBeforeB16='B15 separately committed: eb7fb592dd';HeadMovedDuringB16=$false}
        OwnedFiles=@($owned | ForEach-Object {Fingerprint $_});GitStatus=@(git status --short)
        Validation='Windows clean HEAD archive plus exact test/filter overlay. Existing native crash test not repaired/excluded; Self Analysis and Canonical Diff inherited, not fresh.'
    }
    $path=Join-Path $toolRoot 'Evaluation/P5O2B16-historical-declared-exceptions-boundary-audit.json'
    [IO.File]::WriteAllText($path,(($audit | ConvertTo-Json -Depth 95) -replace "`r?`n","`r`n")+"`r`n",(New-Object Text.UTF8Encoding($false)))
    ReadJson $path | Out-Null
    $jsonBytes=[IO.File]::ReadAllBytes($path); $jsonText=$utf8.GetString($jsonBytes)
    Require ($jsonText -notmatch '(?<!\r)\n' -and $jsonBytes[0] -ne 239) 'Generated JSON UTF8/CRLF invalid.'
    Write-Host 'PASS: B16 actual detector-policy boundary, unchanged Current/three Historical modes, complete regressions, builds, isolation and protection.'
} finally { Pop-Location }
