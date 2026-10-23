$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$directory = Join-Path $PSScriptRoot 'BroadRootCauseAudit'
$destination = Join-Path $directory 'partition.json'
if (Test-Path -LiteralPath $destination) { throw 'Preserve the existing partition; no overwrite.' }
function Json([string]$path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
function CopyEvidence([string]$source,[string]$target) {
    if (Test-Path -LiteralPath $target) {
        if ((Get-FileHash $source).Hash -cne (Get-FileHash $target).Hash) { throw ('Never overwrite different evidence: ' + $target) }
        return # Preserve identical files staged before the capture's filename-collision failure.
    }
    [IO.File]::Copy($source,$target,$false)
}
$broad = Json (Join-Path $PSScriptRoot 'BroadFollowup/verification.json')
$fixtures = Json (Join-Path $PSScriptRoot 'Complete-fixture-preflight.json')
$current = Json (Join-Path $tool 'artifacts/p5o2b25a10/broad-rc-current.json')
$baseline = Json (Join-Path $tool 'artifacts/p5o2b25a10/broad-rc-baseline.json')
$structural = Json (Join-Path $tool 'artifacts/p5o2b25a10/broad-rc-structural-r3.json')
$side = Json (Join-Path $tool 'artifacts/p5o2b25a10/broad-rc-diagnostic-side.json')
$definitions = Json (Join-Path $directory 'groups.json')
$rows = @(foreach ($failure in $broad.Failures) {
    $short = [regex]::Match($failure.Test, '([^.]+Tests)\.(\w+)(?=\(|$)').Value
    if (!$short) { throw ('Unbound test name: ' + $failure.Test) }
    $group = if ($failure.Message -match 'DirectoryNotFoundException' -and $failure.Message -match 'p5o2b7') { 'H1' }
        elseif ($failure.Message -match 'DirectoryNotFoundException' -and $failure.Message -match 'p5o2b4') { 'H2' }
        elseif ($failure.Category -eq 'ProcessHarnessAssertion_NotProvedFlaky' -and $failure.Method -ne 'RealWorkerFailure_IsStructuredAndNeverEmptySuccess') { 'H2' }
        elseif ($failure.Category -eq 'ArchitectureRegression_DoNotRelaxAssertions') { 'A' }
        elseif ($failure.Category -eq 'RawGraphRebindingRegression_DoNotRelaxAssertions') { 'B' }
        elseif ($failure.Category -eq 'IncompleteSyntaxProductionRegression_DoNotRelaxAssertions') { 'C' }
        elseif ($failure.Category -eq 'AdditionalProtectedManifestExpectation') { 'M' }
        elseif ($short -like 'ExceptionFlowExternalDocumentationTests.*') { 'E' }
        elseif ($failure.Method -eq 'ForeachElementFromOfTypeHelper_DoesNotProduceFinding') { 'N' }
        elseif ($failure.Method -eq 'GenuineCli_RejectsWithoutFindingsFallbackOrSuccess') { 'T' }
        elseif ($failure.Test -like 'XMLDocNormalizerTests.Worker.*') { 'W' }
        else { 'D' }
    $definition = @($definitions | Where-Object Id -eq $group)
    if ($definition.Count -ne 1) { throw ('Every failure must have one primary group: ' + $short + ' -> ' + $group) }
    $grant = @(if ($group -eq 'D') { @($fixtures.Changes | Where-Object { $_.Case -ceq ($short + '#0') -and $_.Group -in @('R7','R8','R9') }) }
        elseif ($group -eq 'W') { @($fixtures.Changes | Where-Object { $_.Group -eq 'R10' -and $_.Case.StartsWith($short + ' ',[StringComparison]::Ordinal) }) }
        else { @() })
    $boundObservation = @($current.Results | Where-Object Test -ceq $short)
    $outside = @($boundObservation.Production | Where-Object { $_.Result.Outcome.Complete -eq $false })
    $grantSourceTextPresent = $null
    if ($group -in @('D','W')) {
        if ($grant.Count -ne 1) { throw ('Expected exact single approved case row: ' + $short) }
        $grantSourceTextPresent = $true
        foreach ($assertion in $grant[0].Assertions) {
            $source = Get-Content (Join-Path $tool $assertion.File) -Raw -Encoding UTF8
            if (($source -replace '\s+','').IndexOf(($assertion.Current -replace '\s+',''),[StringComparison]::Ordinal) -lt 0) { $grantSourceTextPresent=$false }
        }
        if (!$grantSourceTextPresent) { throw ('Granted assertion no longer matches actual source: ' + $short) }
    }
    if ($group -eq 'D' -and ($outside.Count -eq 0 -or $failure.Test.Contains('('))) { throw ('Fact#0/outside binding missing: ' + $short) }
    if ($group -eq 'W' -and $failure.Message -notmatch 'UnsupportedContext|unsupportedContext') { throw ('Expected actual classified Worker failure: ' + $short) }
    [pscustomobject][ordered]@{
        Test=$failure.Test; Method=$short; PrimaryGroup=$group; Decision=$definition[0].Decision
        OriginalCoarseCategory=$failure.Category; Message=$failure.Message; Stack=$failure.Stack
        ExistingGrant=$(if ($grant.Count) { $grant[0] } else { $null })
        GrantedAssertionTextsPresentInCurrentSource=$grantSourceTextPresent
        FixtureCaseIds=@($boundObservation.Case | Sort-Object -Unique)
        ActualObservedCalls=@($boundObservation | Select-Object CallIndex,Entry,Mode,SourceSha256,Error)
        OutsideObservedRoots=@($outside | ForEach-Object { [pscustomobject]@{ Member=$_.Member; Certificate=$_.Result.Outcome } })
        Secondary=$(if ($group -eq 'H1' -and $short -eq 'ExceptionFlowAnalysisDispatchTests.WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess') { 'R10 async expected-code row blocked by missing fixture first, not another primary failure.' } else { $null })
    }
})
if ($rows.Count -ne 179 -or @($rows.Test | Sort-Object -Unique).Count -ne 179) { throw 'Must bind179 unique failures without duplicate primary causes.' }
$groups = @(foreach ($definition in $definitions) {
    $members = @($rows | Where-Object PrimaryGroup -eq $definition.Id)
    if ($members.Count -ne $definition.ExpectedCases) { throw ('Group count mismatch: ' + $definition.Id + ' actual=' + $members.Count) }
    [pscustomobject][ordered]@{ Id=$definition.Id; Cases=$members.Count; Methods=@($members.Method | Sort-Object -Unique).Count; Analysis=$definition }
})
$recursive = @(foreach ($entry in $current.Results | Where-Object { $_.Entry -in @('AnalyzeDirectly','AnalyzeTransitively') }) {
    $previous = @($baseline.Results | Where-Object { $_.Test -ceq $entry.Test -and $_.CallIndex -eq $entry.CallIndex })
    if ($previous.Count -ne 1) { throw 'Missing exact baseline call.' }
    $equal = $true
    foreach ($field in @('Thrown','External','Uncertain')) {
        if ((@($entry.Native.$field | Sort-Object) -join "`n") -cne (@($previous[0].Native.$field | Sort-Object) -join "`n")) { $equal=$false }
    }
    if (!$equal) { throw ('Unexpected Direct/Recursive semantic difference: ' + $entry.Test) }
    [pscustomobject]@{ Test=$entry.Test; CallIndex=$entry.CallIndex; Entry=$entry.Entry; Equal=$equal }
})
$snapshot = Join-Path $tool 'artifacts/b25a10-development-13'
$manifestTest = 'Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/HistoricalAnalyzerBuildProjectTests.cs'
$old = (Get-Content (Join-Path $snapshot $manifestTest) -Raw -Encoding UTF8).Replace("`r`n","`n")
$now = (Get-Content (Join-Path $tool $manifestTest) -Raw -Encoding UTF8).Replace("`r`n","`n")
$expected = $old.Replace('Assert.Equal(121, shared.Length);','Assert.Equal(125, shared.Length);').Replace('Assert.Equal(121, shared.Distinct(StringComparer.OrdinalIgnoreCase).Count());','Assert.Equal(125, shared.Distinct(StringComparer.OrdinalIgnoreCase).Count());')
if ($expected -ceq $old -or $now -cne $expected) { throw 'Only the two expressly approved manifest counters may change.' }
$config = 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config'
& git --no-optional-locks -C $tool diff --quiet -- $config
if ($LASTEXITCODE -ne 0) { throw 'Protected original config changed.' }
$paths = @(@(& git --no-optional-locks -C $tool ls-files -- src Tests) + @(& git --no-optional-locks -C $tool ls-files --others --exclude-standard -- src Tests) | Sort-Object -Unique | Where-Object { $_ -notmatch '(^|/)(bin|obj)/' -and $_ -notin @($manifestTest,$config) })
foreach ($path in $paths) {
    if ((Get-FileHash (Join-Path $tool $path)).Hash -cne (Get-FileHash (Join-Path $snapshot $path)).Hash) { throw ('Unauthorized production/test change: ' + $path) }
}
$copies = @('broad-rc-current.json','broad-rc-baseline.json','broad-rc-structural.json','broad-rc-structural-r2.json','broad-rc-structural-r3.json','broad-rc-diagnostic-side.json')
$evidence = @(foreach ($name in $copies) {
    $target = Join-Path $directory $name
    CopyEvidence (Join-Path $tool ('artifacts/p5o2b25a10/' + $name)) $target
    [pscustomobject]@{ Name=$name; Sha256=(Get-FileHash $target).Hash }
})
$runDirectory=Join-Path $directory 'Runs'
[IO.Directory]::CreateDirectory($runDirectory) | Out-Null
$runs = @(Get-ChildItem (Join-Path $tool 'artifacts/p5o2b25a10/runs') -Filter 'broad-rc-*.json' | ForEach-Object {
    $run = Json $_.FullName
    foreach ($extension in @('.json','.log')) { CopyEvidence ([IO.Path]::ChangeExtension($_.FullName,$extension)) (Join-Path $runDirectory ($_.BaseName+$extension)) }
    [pscustomobject]@{ Name=$run.Name; ExitCode=$run.ExitCode; SourcePath=$_.FullName; Sha256=(Get-FileHash $_.FullName).Hash }
})
[xml]$manifest = Get-Content (Join-Path $tool 'build/ExceptionFlow.HistoricalSources.props') -Raw
$shared = @($manifest.Project.ItemGroup.Compile.Include)
$headManifest = [xml]((& git --no-optional-locks -C $tool show 'HEAD:Tools/XMLDocNormalizer/build/ExceptionFlow.HistoricalSources.props') -join "`n")
$delta = @(Compare-Object @($headManifest.Project.ItemGroup.Compile.Include) $shared)
if ($shared.Count -ne 125 -or @($shared | Sort-Object -Unique).Count -ne 125 -or $delta.Count -ne 4 -or @($delta | Where-Object SideIndicator -ne '=>').Count) { throw 'Unexpected manifest set change.' }
$record = [ordered]@{
    Kind='Complete primary partition of179 original Broad failures; read-only diagnosis, not final validation'
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Groups=$groups; Rows=$rows
    Total=179; UniquePrimaryAssignments=179; Overlaps=0; Unassigned=0
    Decisions=@($rows | Group-Object Decision | ForEach-Object { [pscustomobject]@{ Decision=$_.Name; Cases=$_.Count } })
    ActualCurrentSemanticCalls=$current.Results.Count; ActualBaselineSemanticCalls=$baseline.Results.Count
    NativeObservationErrors=@($current.Results | Where-Object Error).Count
    DirectRecursivePreserved=$recursive; DirectRecursiveCount=$recursive.Count
    ManifestGrant=@{ OnlyTwoCountersChanged=$true; SharedCount=125; DistinctCount=125; AddedEntries=@($delta.InputObject); ExistingTestExecuted=$false }
    OtherSourceAndTestFilesVerifiedUnchanged=$paths.Count; ProductionChangesThisRun=0; OtherTestChangesThisRun=0
    NewArchitectureBoundaryProposedNotImplemented=$true; NewApprovalBatch='Broad-Approval-Batch.md'
    RebindingEvidenceLimit='Original Broad failure retained; fresh existing helper does not reproduce. Controlled reduced-vs-unreduced lookup isolates the shape-sensitive invariant. Actual Broad state trigger/cache/race is not proved.'
    Evidence=$evidence; DiagnosticRuns=$runs; ClosedR2Unchanged=$true
    ClosureGatesRunThisTurn=0; BroadRetried=$false; FrozenHistoricalEvidenceRewritten=$false; B25BActivated=$false
    CaptureDevelopment='Two pre-write PS5 array/scalar binding failures; then run-record/witness filename collision after staging six unchanged witnesses. Resumed by hash-verifying existing staged files and putting run records under Runs; no evidence overwritten or tests retried.'
}
[IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 70)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host ($groups | Select-Object Id,Cases,Methods | ConvertTo-Json -Compress)
Write-Host '179 unique cases, no overlap/unassigned; only two manifest counters changed; no further closure gate.'
