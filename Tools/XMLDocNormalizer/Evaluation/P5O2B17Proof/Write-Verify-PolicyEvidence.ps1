param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $toolRoot
try {
    function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 90 -Compress }
    function Equal($Left,$Right,[string]$Message) { Require ((Json $Left) -ceq (Json $Right)) $Message }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function Tests([string]$Name,[string]$Path,[int]$Expected,[bool]$Fresh=$true) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Incomplete/failed: $Name"
        Require ($counts.total -eq "$Expected" -and $counts.passed -eq "$Expected" -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Census/failure/skips: $Name"
        if($Fresh) { Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale: $Name" }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b17/protected-before.json'; $export=ReadJson 'artifacts/p5o2b17/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -ceq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Clean recorded HEAD archive required.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'External HEAD movement requires explicit audit.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected ignore changed.'
    Equal @(git stash list --format='%H %s') $initial.Stashes 'Stashes changed.'
    Equal @(git diff --cached --name-only) $initial.Index 'Index changed.'
    [string[]]$built=ReadJson 'artifacts/p5o2b17/owned-build-inputs.json'
    Require ($built.Count -eq 10) 'Ten exact owned build inputs required.'
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})+@([IO.Path]::GetFullPath('Evaluation/OPEN-PIPELINE-BOUNDARIES.md'))
    foreach($file in $initial.TrackedFiles) { if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/shared/Current/test source changed: $($file.Path)" } }
    Require ($initial.TrackedFiles.Count -eq 901 -and $initial.CoreFiles.Count -eq 23 -and $initial.Stashes.Count -eq 6) 'Unexpected protected inventory.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core inventory changed.'
    foreach($file in $initial.CoreFiles) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'Core byte change.' }
    foreach($file in $built) { Require ((Get-FileHash $file).Hash -ceq (Get-FileHash (Join-Path $validation $file)).Hash) "Final built overlay mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'; $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Shared/host/version boundary changed.'
    Equal $gate.SharedSourceFingerprints $parent.SharedSourceFingerprints 'Shared analyzer source bytes changed.'
    foreach($file in $gate.SharedSourceFingerprints) { Require ((Get-FileHash (Join-Path $validation $file.Path)).Hash -ceq $file.Sha256) 'Built shared source changed.' }
    foreach($image in @($gate.BuildImages | Where-Object Owner -eq 'historical')) {
        $before=@($parent.BuildImages | Where-Object {$_.Step -eq $image.Step})
        Require ($image.Image.Sha256 -ceq $before[0].Image.Sha256 -and $image.Image.PdbSha256 -ceq $before[0].Image.PdbSha256) 'Historical analyzer DLL/PDB changed.'
    }
    $logs=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed gate step: $($step.Name)"
        if($step.Name.EndsWith('-build')) { $path=Join-Path $gateRoot ($step.Name+'.log'); $text=Get-Content $path -Raw -Encoding UTF8
            Require ($text -match '0 Warnung\(en\)' -and $text -match '0 Fehler') "Not clean WAE: $($step.Name)"; Fingerprint $path }
    }
    Require (@($logs).Count -eq 11) 'Eleven WAE builds required.'
    $expected=[ordered]@{b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=660;broad=2961;full=3052}
    $census=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b17/tests/'+$name+'.trx')) $expected[$name] }
    $census+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 450)
    $census+=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 425 $false)
    $baselines=foreach($file in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $file.FullName).Hash -ceq (Get-FileHash $after).Hash) "Current byte baseline changed: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight complete native Current baselines required.'
    $root=Join-Path $validation 'artifacts/p5o2b17'
    $cases=foreach($file in Get-ChildItem $root -File -Filter 'case-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.OriginalValidatedProjection -and $value.NoFallback -and $value.ProcessesStopped -and $value.NoHistoricalLoadedInMain -and $value.DeclaredStillUnsupported -and $value.NoFindingImport -and $value.CloneRejected -and $value.ImmutableReceipt) 'Contract/isolation/activation boundary wrong.'
        Equal $value.Native.Summary $value.Historical 'Entire canonical summary/flows/sources differ.'
        Equal $value.Native.Direct $value.PolicyEvidence.DirectResult 'Independent Direct result differs.'
        Equal $value.Native.Declarations $value.PolicyEvidence.Declarations 'Full type/declaration ownership differs.'
        Equal $value.PolicyEvidence $value.RepeatedPolicyEvidence 'Facts serialization repeat differs.'
        Equal $value.Native.Summaries $value.Graph.Summaries 'All canonical summaries differ.'
        Equal $value.Graph.Summaries $value.Graph.RepeatedSummaries 'Graph repeat differs.'
        Require ($value.Native.GraphCount -eq $value.Graph.GraphCount -and $value.Graph.GraphCount -eq $value.Graph.RepeatedGraphCount -and $value.Graph.RepeatedResultEqual -and $value.Graph.ForeignTreeRejected -and $value.Graph.ForeignBindingRejected) 'Graph state/ownership difference.'
        Require ((Json $value.Native.ProjectFindings) -cne (Json $value.Native.DeclaredFindings)) 'No real Current mode finding difference.'
        Require ($value.PolicyEvidence.Profile -ceq 'single-project-declared-policy-v1' -and $value.PolicyEvidence.Mode -ceq 'projectTransitiveDeclaredExceptions' -and $value.Provenance.AnalysisMode -ceq 'project-transitive' -and $value.Provenance.ReferenceWorldSha256.Length -eq 64) 'Opt-in/provenance incorrectly aliases engine mode.'
        Require ($value.PolicyEvidence.DeclarationCount -eq $value.PolicyEvidence.Declarations.Count) 'Incomplete declaration inventory.'
        Require (@($value.Decisions).Count -gt 0) 'No policy decision differential.'
        foreach($decision in $value.Decisions) {
            Require ($null -eq $decision.Historical.Failure -and $decision.Native.Relevant -eq $decision.Historical.Decision.Relevant -and $decision.Native.Local -eq $decision.Historical.Decision.LocallyDeclared -and $value.Native.HasDeclaredTypes -eq $decision.Historical.Decision.SummaryEnabled) 'Actual Current/Historical Finding relevance differs.'
            Equal $decision.Historical $decision.RepeatedHistorical 'Policy decision repeat differs.'
            if($decision.Query.SourceKind -in @('direct','summary')) {
                $rule=if($decision.Query.SourceKind -ceq 'direct') {'DOC610'} else {'DOC611'}
                $emitted=@($value.Native.DeclaredFindings | Where-Object {$_.Smell.Id -ceq $rule -and $_.Message.Contains($decision.Native.DisplayName)}).Count -gt 0
                Require ($emitted -eq $decision.Historical.Decision.Relevant) 'Resulting actual Finding applicability differs.'
            }
        }
        foreach($owner in $value.Graph.Ownership) { Require ($owner.StableModel -and $owner.Owned) 'Historical ownership is not stable/exact.' }
        foreach($compiler in $value.Identity.LoadedRoslyn) { Require ($gate.HistoricalCompilerReferences.Sha256 -contains $compiler.Sha256 -and $value.Graph.CompilerMvids -contains $compiler.Mvid) 'Not genuine pinned isolated Historical runtime.' }
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($cases).Count -eq 7) 'Seven complete Finding-policy differentials required.'
    $negatives=foreach($file in Get-ChildItem $root -File -Filter 'negative-response-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.Failure.Code -ceq 'malformedResponse' -and $value.NoPolicyReceipt -and $value.NoResult -and $value.NoFallback -and $value.ProcessesStopped) 'Negative response not fail closed.'
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($negatives).Count -eq 16) 'Sixteen forged/missing response negatives required.'
    $query=ReadJson (Join-Path $root 'query-negative.json')
    foreach($property in $query.PSObject.Properties) { Require ($property.Value -eq $true) 'Query admission or legacy receipt failure.' }
    $request=ReadJson (Join-Path $root 'request-negative.json')
    foreach($name in @('RequiredModeProfileAndInventory','OtherModesRefused','CallerInventoryRefused','MissingCopiedScopeRefused','SolutionScopeRefused','GenericRefused','NoPartialPolicy','NoFallback','ProcessesStopped')) { Require ($request.$name -eq $true) "Missing request control: $name" }
    Require ($request.ActualInventoryMismatch.WorkerFailure.Code -ceq 'unsupportedContext' -and $request.ActualUncertainWorker.WorkerFailure.Code -ceq 'analysisFailure' -and $request.Uncertainty.Count -gt 0) 'Actual binding/uncertainty errors lost.'
    $live=@(Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($validation,[StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.CommandLine -match 'HistoricalWorker.dll|HistoricalPdbFixture.dll' })
    Require ($live.Count -eq 0) 'Own Historical child remains alive.'
    $owned=@($built)+@('Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B17-historical-declared-finding-policy-contract.md','Evaluation/P5O2B17Proof/Run-Regressions.ps1','Evaluation/P5O2B17Proof/Write-Verify-PolicyEvidence.ps1')
    $utf8=New-Object Text.UTF8Encoding($false,$true)
    foreach($path in $owned) {
        $bytes=[IO.File]::ReadAllBytes((Resolve-Path $path).Path); $text=$utf8.GetString($bytes)
        Require (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191)) "BOM: $path"
        Require ($text -notmatch '(?<!\r)\n' -and $text.EndsWith("`r`n")) "UTF8/CRLF: $path"
        if($path -like '*.ps1') { $parseErrors=$null; [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path).Path,[ref]$null,[ref]$parseErrors) | Out-Null; Require ($parseErrors.Count -eq 0) "PS syntax: $path" }
    }
    # Plain strings only: PS5 Get-Content note properties include cyclic provider objects.
    $matrix=@(Get-Content Evaluation/P5O2B17-historical-declared-finding-policy-contract.md -Encoding UTF8 | Where-Object { $_.StartsWith('| ') -and $_.Split('|').Count -eq 7 -and -not $_.StartsWith('| Information') -and -not $_.StartsWith('| ---') } | ForEach-Object {[string]$_})
    Require ($matrix.Count -eq 11) 'Eleven pre-implementation matrix rows required.'
    $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $sources=@($built | Where-Object {$_ -like '*.cs'}); $format=dotnet format whitespace . --folder --include $sources --verify-no-changes 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    $format | Set-Content artifacts/p5o2b17/format-final.log -Encoding UTF8; Require ($code -eq 0) 'Owned folder-only format failed.'
    git diff --check; Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $collisions=@(git ls-files | Group-Object {$_.ToLowerInvariant()} | Where-Object Count -gt 1); Require ($collisions.Count -eq 0) 'Tracked path case collision.'
    $attempts=ReadJson 'artifacts/p5o2b17/attempts.json'
    $audit=[ordered]@{
        Schema='P5O2B17-declared-finding-policy-contract-v1';DateUtc=[DateTime]::UtcNow.ToString('o');Head=$initial.Head;Matrix=$matrix
        Outcome='Bounded neutral Finding-eligibility contract closed. Productive Historical Declared activation and general Finding import remain unsupported.'
        Protocol=[ordered]@{OuterVersion=2;AdditiveOptIn='single-project-declared-policy-v1';EngineMode='project-transitive';PolicyMode='ProjectTransitiveDeclaredExceptions';MaximumDeclarations=128;OriginalPeInventoryBound=$true;NoAlias=$true;NoLegacyPayloadChange=$true;ImmutableOriginalCallReceipt=$true}
        Differentials=$cases;Negatives=$negatives;QueryFailures=$query;RequestFailures=$request;CurrentBaselines=$baselines;Tests=$census;WaeBuilds=$logs;DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'));Attempts=$attempts
        Architecture=[ordered]@{SharedSources=121;SharedAnalyzerAndHistoricalHostUnchanged=$true;HistoricalAnalyzerDllPdbUnchanged=$true;CurrentDetectorUnchanged=$true;B5B6Unchanged=$true;B10ThroughB16TestsUnchanged=$true;RoslynVersionsUnchanged=$true;NoSecondPePdbRefSourceValidator=$true;WorkerOnlyProjectsFacts=$true;MainOnlyDecidesRelevance=$true;NoRoslynIpc=$true;NoHistoricalLoadedInMain=$true;NoOwnWorkerRemaining=$true;NoFallback=$true;NoFindingImport=$true}
        Open=@('B18 guarded productive Declared execution and complete Finding parity','external documented exception types absent from attested flows/source inventory','uncertain/external XML evidence and coverage/anchors/reporting context','generic exception declarations and broader scopes','CLI, signed targets, unsupported references/XML contracts')
        SemanticsBaseline=[ordered]@{SelfAnalysisFindings=16;CanonicalAdded=0;CanonicalRemoved=0;CanonicalChangedEvidence=0;InheritedFrom='B16/B15/B14/B13/B11/B10';FreshSelfAnalysis=$false;FreshCanonicalDiff=$false}
        Quality=[ordered]@{Format=$true;Json=$true;Utf8CrLf=$true;PowerShellSyntax=$true;GitDiffCheck=$true;ArchitectureIsolationGate=$true;EvidenceVerifier=$true;TrackedCaseCollisions=0;LinuxExecution=$false}
        Protection=[ordered]@{RootIgnoreUnchanged=$true;CoreFiles=23;Stashes=6;IndexUnchanged=$true;NoCommit=$true;NoPush=$true;ExternalHeadBeforeB17='B16 separately committed: 839fcb03a6';HeadMovedDuringB17=$false}
        OwnedFiles=@($owned | ForEach-Object {Fingerprint $_});GitStatus=@(git status --short);Validation='Windows clean HEAD archive plus exact final owned overlay. No configured tests skipped; Self Analysis/canonical diff inherited, not fresh.'
    }
    $path=Join-Path $toolRoot 'Evaluation/P5O2B17-historical-declared-finding-policy-contract-audit.json'
    [IO.File]::WriteAllText($path,(($audit | ConvertTo-Json -Depth 95) -replace "`r?`n","`r`n")+"`r`n",(New-Object Text.UTF8Encoding($false)))
    ReadJson $path | Out-Null
    $bytes=[IO.File]::ReadAllBytes($path); $text=$utf8.GetString($bytes); Require ($bytes[0] -ne 239 -and $text -notmatch '(?<!\r)\n') 'Generated JSON encoding failed.'
    Write-Host 'PASS: B17 full facts/Finding relevance, unchanged Current/Analyzer/routes, complete regressions/builds, isolation, protection and bounded activation status.'
} finally { Pop-Location }
