$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$export = Join-Path $tool 'artifacts/b25a10-development-07'
$source = Join-Path $export 'artifacts/p5o2b25a10/analysis/b25/ownership-cardinality.json'
$destination = Join-Path $PSScriptRoot 'ownership-cardinality-followup.json'
$verification = Join-Path $PSScriptRoot 'ownership-followup-verification.json'
if ((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath $verification)) { throw 'Preserve existing followup evidence; no overwrite.' }
if ((Get-FileHash -LiteralPath $source).Hash -cne 'CFAAC64408F0783BB5813E0A44C57DF4B37DF7B476B01A730B10B1156F6657FC') { throw 'Unexpected captured witness bytes.' }
$relative = 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs'
$current = Get-Content -LiteralPath (Join-Path $tool $relative) -Raw -Encoding UTF8
$original = (& git --no-optional-locks -C $tool show ('HEAD:Tools/XMLDocNormalizer/' + $relative)) -join "`n"
$assertions = @(
    'Assert.Single(native.GetProperty("findings").EnumerateArray());',
    'Assert.Equal("DOC611", native.GetProperty("findings")[0].GetProperty("smell").GetProperty("id").GetString());'
)
$protected = @(foreach ($assertion in $assertions) {
    [ordered]@{ Assertion=$assertion; InHead=$original.Contains($assertion); InCurrent=$current.Contains($assertion) }
})
if (@($protected | Where-Object { !$_.InHead -or !$_.InCurrent }).Count) { throw 'Protected Finding assertion changed.' }
$productionDrift = @(foreach ($file in Get-ChildItem (Join-Path $tool 'src') -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '\\(?:bin|obj)\\' }) {
    $path = $file.FullName.Substring($tool.Length + 1)
    $old = Join-Path $tool ('artifacts/b25a10-development-05/' + $path)
    if (!(Test-Path -LiteralPath $old) -or (Get-FileHash $old).Hash -cne (Get-FileHash $file.FullName).Hash) { $path }
})
[IO.File]::Copy($source, $destination, $false)
$witness = Get-Content -LiteralPath $destination -Raw -Encoding UTF8 | ConvertFrom-Json
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Status='STOP_FindingAssertionNotApproved'
    Source=$source; Witness='Evaluation/P5O2B25A10Proof/ownership-cardinality-followup.json'
    WitnessSha256=(Get-FileHash -LiteralPath $destination).Hash
    Owners=@($witness.Flow.entries | ForEach-Object { $_.exceptionType.assembly.name })
    SelectedOwner=$witness.SelectedOwner.name; Selection=$witness.Selection
    FindingCount=@($witness.Native.findings).Count
    FindingIds=@($witness.Native.findings | ForEach-Object { $_.smell.id })
    ProtectedFindingAssertions=$protected
    ProductionSourceDifferencesSinceExport05=$productionDrift
    SingleAffectedTestRun='artifacts/p5o2b25a10/runs/development-07-b25-ownership.json'
    TestExitCode=1; TestCount=1; Failed=1; Passed=0; Skipped=0
    OwnerAndP1PathAssertionsPassed=$true
    FindingCountAssertionPassed=$false; SubsequentFindingIdAssertionExecuted=$false
    A10Complete=$false; B25BActivated=$false
}
[IO.File]::WriteAllText($verification, (($record|ConvertTo-Json -Depth 10)+"`r`n"), [Text.UTF8Encoding]::new($false))
if ($productionDrift.Count -ne 0) { throw 'Unexpected production-source change during the narrow followup.' }
Write-Host 'Captured A10-only runtime witness: two owners, P1 identity selection, two DOC611 Findings; protected Finding assertions retained.'
