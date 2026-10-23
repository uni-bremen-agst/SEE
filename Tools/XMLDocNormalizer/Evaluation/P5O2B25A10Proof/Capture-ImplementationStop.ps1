param(
    [ValidatePattern('^[a-z0-9-]+\.json$')][string]$RecordName = 'implementation-stop-state.json',
    [ValidateSet('STOP_UnapprovedB25Cardinality_NoCompletionClaim', 'STOP_UnapprovedB25FindingCardinality_NoCompletionClaim', 'STOP_ProtectedConstructedReferenceIdentity_NoCompletionClaim', 'STOP_SeparateR5ReferenceAssertionClarification_NoCompletionClaim', 'STOP_R2IdentityContractDecision_NoCompletionClaim', 'STOP_BroadAdditionalArchitectureAndCharacterizationReview_NoCompletionClaim', 'STOP_BroadRootCausePartition_NoProductionRepair_NoCompletionClaim')]
    [string]$StopStatus = 'STOP_UnapprovedB25Cardinality_NoCompletionClaim'
)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$output = Join-Path $PSScriptRoot $RecordName
if (Test-Path -LiteralPath $output) { throw 'Retain the original STOP capture; do not overwrite.' }
$frozen = Get-Content (Join-Path $tool 'artifacts/p5o2b25a9/start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$phase0 = Get-Content (Join-Path $PSScriptRoot 'phase0-binding-closure-validation.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$core = @(foreach ($file in $frozen.Core) {
    $expected = $file.Sha256
    $s1 = @($phase0.CoreRawFrozenHashDifferences | Where-Object { $_.Path -eq $file.Path })
    if ($s1.Count) { $expected = $s1[0].Actual }
    $actual = (Get-FileHash -LiteralPath $file.Path).Hash
    [ordered]@{ Path=$file.Path; Frozen=$file.Sha256; ExplainedS1OrFrozen=$expected; Actual=$actual; NewDrift=($actual -cne $expected) }
})
$historical = @(foreach ($file in $frozen.OriginalFiles | Where-Object { $_.Path -match '^Evaluation/' -and $_.Path -notmatch '^Evaluation/P5O2B25A10(?:Proof/|-)' }) {
    $actual = (Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash
    [ordered]@{ Path=$file.Path; Frozen=$file.Sha256; Actual=$actual; Changed=($actual -cne $file.Sha256) }
})
$stashes = @(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs')
& git --no-optional-locks -C $repo diff --cached --quiet
$semanticIndexClean = $LASTEXITCODE -eq 0
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Status=$StopStatus
    Head=(& git --no-optional-locks -C $repo rev-parse HEAD)
    IndexSha256=(Get-FileHash -LiteralPath (Join-Path $repo '.git/index')).Hash
    RootIgnoreSha256=(Get-FileHash -LiteralPath (Join-Path $repo '.gitignore')).Hash
    SemanticIndexClean=$semanticIndexClean; Core=$core
    NewCoreDrift=@($core|Where-Object NewDrift).Count
    HistoricalEvidence=$historical; HistoricalChanges=@($historical|Where-Object Changed).Count
    Stashes=$stashes; StashesUnchanged=(($stashes -join "`n") -ceq ($frozen.Stashes -join "`n"))
    ChangedPaths=@(& git --no-optional-locks -C $repo diff --name-only)
    GitStatus=@(& git --no-optional-locks -C $repo status --short)
    B25TestUnchanged=((& git --no-optional-locks -C $tool diff --name-only -- Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs) -eq $null)
    Commit=$false; Push=$false; StashOperation=$false; FrozenManifestRewritten=$false
}
[IO.File]::WriteAllText($output, (($record|ConvertTo-Json -Depth 12)+"`r`n"), [Text.UTF8Encoding]::new($false))
if ($record.NewCoreDrift -ne 0 -or $record.HistoricalChanges -ne 0 -or !$record.StashesUnchanged -or !$semanticIndexClean) { throw 'New protection mismatch: retained STOP capture, no repair.' }
Write-Host ('STOP capture: no new Core drift; historical files unchanged: ' + $historical.Count + '; six stashes unchanged; clean semantic index.')
