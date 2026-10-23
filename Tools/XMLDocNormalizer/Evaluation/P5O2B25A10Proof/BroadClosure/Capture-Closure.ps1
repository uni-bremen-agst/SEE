param([ValidatePattern('^[0-9]{2}$')][string]$Revision = '16', [switch]$Final,
    [ValidatePattern('^[a-z0-9-]*$')][string]$RecordTag = '')
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$previous = Get-Content (Join-Path $PSScriptRoot '../implementation-broad-root-cause-state.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$suffix = if ($Final) { '-final' } else { '' }
if ($RecordTag) { $suffix += '-' + $RecordTag }
$destination = Join-Path $PSScriptRoot ('verification-r' + $Revision + $suffix + '.json')
if (Test-Path -LiteralPath $destination) { throw 'Do not overwrite a completed capture.' }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
$core = @(foreach ($file in $previous.Core) {
    $actual = Hash $file.Path
    [ordered]@{ Path=$file.Path; Frozen=$file.Frozen; ExplainedS1OrFrozen=$file.ExplainedS1OrFrozen; Actual=$actual; NewDrift=$actual -cne $file.Actual }
})
$historical = @(foreach ($file in $previous.HistoricalEvidence) {
    $actual = Hash (Join-Path $tool $file.Path)
    [ordered]@{ Path=$file.Path; Frozen=$file.Frozen; Actual=$actual; Changed=$actual -cne $file.Frozen }
})
$stashes = @(& git --no-optional-locks -C $repo stash list --format='%H %gs')
& git --no-optional-locks -C $repo diff --cached --quiet
$indexClean = $LASTEXITCODE -eq 0
$index = Hash (Join-Path $repo '.git/index')
$ignore = Hash (Join-Path $repo '.gitignore')
$runs = @(Get-ChildItem (Join-Path $tool 'artifacts/p5o2b25a10/runs') -Filter ('development-' + $Revision + '-*.json') | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
})
if (@($runs | Where-Object { $null -eq $_.ExitCode }).Count) { throw 'A recorded command is still running; do not capture partial validation as complete.' }
$validation = Join-Path $tool ('artifacts/b25a10-development-' + $Revision)
$results = @(Get-ChildItem (Join-Path $validation 'artifacts/a10-closure-results') -Filter '*.trx' | ForEach-Object {
    $file = $_
    [xml]$trx = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    $counts = [ordered]@{}
    foreach ($attribute in $trx.TestRun.ResultSummary.Counters.Attributes) { $counts[$attribute.Name] = [int]$attribute.Value }
    $classes = @($trx.TestRun.Results.UnitTestResult | Group-Object { ($_.testName.Split('(')[0].Split('.'))[-2] } | ForEach-Object {
        [ordered]@{ Class=$_.Name; Total=$_.Count; Passed=@($_.Group | Where-Object outcome -eq 'Passed').Count; Failed=@($_.Group | Where-Object outcome -eq 'Failed').Count }
    })
    [ordered]@{ File=$file.Name; Sha256=(Hash $file.FullName); Counters=$counts; Classes=$classes
        Failures=@($trx.TestRun.Results.UnitTestResult | Where-Object outcome -eq 'Failed' | ForEach-Object {
            [ordered]@{ Test=$_.testName; Message=$_.Output.ErrorInfo.Message; Stack=$_.Output.ErrorInfo.StackTrace }
        }) }
})
$sourceChanges = @(foreach ($path in @(& git --no-optional-locks -C $tool ls-files -- src Tests) + @(& git --no-optional-locks -C $tool ls-files --others --exclude-standard -- src Tests) | Sort-Object -Unique) {
    if ($path -match '(^|/)(bin|obj)/') { continue }
    if ($path -ceq 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config') {
        & git --no-optional-locks -C $tool diff --quiet -- $path
        if ($LASTEXITCODE -ne 0) { throw 'Original feed configuration changed.' }
        continue # Explicit offline-export feed transformation, not compiled source.
    }
    $current = Hash (Join-Path $tool $path)
    $tested = Join-Path $validation $path
    if (!(Test-Path -LiteralPath $tested) -or (Hash $tested) -cne $current) { throw ('Current source differs from tested export: ' + $path) }
    $old = Join-Path $tool ('artifacts/b25a10-development-13/' + $path)
    $before = if (Test-Path -LiteralPath $old) { Hash $old } else { $null }
    if ($before -cne $current) { [ordered]@{ File=$path; BeforeExport13=$before; CurrentAndTested=$current } }
})
$newDrift = @($core | Where-Object NewDrift).Count
$historicalChanges = @($historical | Where-Object Changed).Count
$stashSame = ($stashes -join "`n") -ceq ($previous.Stashes -join "`n")
$original = Get-Content (Join-Path $PSScriptRoot '../BroadRootCauseAudit/partition.json') -Raw -Encoding UTF8 | ConvertFrom-Json
[xml]$broad = Get-Content (Join-Path $validation 'artifacts/a10-closure-results/broad.trx') -Raw -Encoding UTF8
$progress = @(foreach ($row in $original.Rows) {
    $name = $row.Test.Replace('ForeachElementFromOfTypeHelper_DoesNotProduceFinding', 'ForeachElementFromOfTypeHelper_CoverageOutsideShowsUncertainty_NotExactFinding')
    # These three exact async InlineData expected codes are explicitly R10-approved.
    if ($name -match '\.(ExceptionFlowAnalysisDispatchTests\.WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess|ExceptionFlowAnalysisRouterTests\.HistoricalAnalysisFailure_PreservesOriginalB4FailureContract|HistoricalWorkerClientTests\.RealWorkerFailure_IsStructuredAndNeverEmptySuccess)\(') {
        $name = $name.Replace(': "AnalysisFailure"', ': "UnsupportedContext"')
    }
    $match = @($broad.TestRun.Results.UnitTestResult | Where-Object testName -ceq $name)
    if ($match.Count -ne 1) { throw ('Original case not uniquely bound: ' + $name) }
    [ordered]@{ OriginalTest=$row.Test; CurrentTest=$name; OriginalPrimaryGroup=$row.PrimaryGroup; Outcome=[string]$match[0].outcome }
})
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Revision=$Revision
    Head=(& git --no-optional-locks -C $repo rev-parse HEAD); SemanticIndexClean=$indexClean
    IndexSha256=$index; IndexUnchanged=$index -ceq $previous.IndexSha256
    RootIgnoreSha256=$ignore; RootIgnoreUnchanged=$ignore -ceq $previous.RootIgnoreSha256
    Core=$core; NewCoreDrift=$newDrift; HistoricalEvidence=$historical; HistoricalChanges=$historicalChanges
    Stashes=$stashes; StashesUnchanged=$stashSame; SourceDeltaFromPriorBroad=$sourceChanges
    Runs=$runs; Results=$results; GitStatus=@(& git --no-optional-locks -C $tool status --short)
    Original179CaseProgress=$progress
    Original179Passed=@($progress | Where-Object Outcome -eq 'Passed').Count
    Original179Failed=@($progress | Where-Object Outcome -eq 'Failed').Count
    FullA10Closure=$false; B25BActivated=$false; Commit=$false; Push=$false; StashOperation=$false
    HistoricalS1ProtectionExit1Preserved=$true; FrozenManifestRewritten=$false
}
if ($Final) {
    $retained = Join-Path $PSScriptRoot ('Results-r' + $Revision + $(if ($RecordTag) { '-' + $RecordTag } else { '' }))
    [IO.Directory]::CreateDirectory($retained) | Out-Null
    foreach ($file in Get-ChildItem (Join-Path $validation 'artifacts/a10-closure-results') -Filter '*.trx') {
        [IO.File]::Copy($file.FullName, (Join-Path $retained $file.Name), $false)
    }
    foreach ($file in Get-ChildItem (Join-Path $tool 'artifacts/p5o2b25a10/runs') -Filter ('development-' + $Revision + '-*')) {
        [IO.File]::Copy($file.FullName, (Join-Path $retained $file.Name), $false)
    }
}
[IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 35)+"`r`n"),[Text.UTF8Encoding]::new($false))
foreach ($result in $results) { Write-Host ($result.File + ': ' + ($result.Counters | ConvertTo-Json -Compress)) }
if ($newDrift -or $historicalChanges -or !$stashSame -or !$indexClean -or !$record.IndexUnchanged -or !$record.RootIgnoreUnchanged) { throw 'STOP: protection drift. Record retained; no repair/baseline reset.' }
Write-Host 'Protection unchanged relative to explained S1 state; no historical rewrite, index or stash mutation.'
