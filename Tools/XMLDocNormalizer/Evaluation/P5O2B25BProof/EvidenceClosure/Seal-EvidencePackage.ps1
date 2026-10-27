$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'EvidenceStorage.psm1') -Force
$proof=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$tool=[IO.Path]::GetFullPath((Join-Path $proof '../..'))
$sealPath=Join-Path $PSScriptRoot 'package-seal.json'
$auditPath=Join-Path $PSScriptRoot 'closure-audit.json'
if((Test-Path -LiteralPath $sealPath) -or (Test-Path -LiteralPath $auditPath)){throw 'Completed package must not be resealed/rewritten'}
$start=Get-Content (Join-Path $PSScriptRoot 'start.json') -Raw | ConvertFrom-Json
$verified=Get-Content (Join-Path $PSScriptRoot 'verification-final.json') -Raw | ConvertFrom-Json
$catalog=Get-Content (Join-Path $proof 'evidence-storage-manifest.json') -Raw | ConvertFrom-Json
if(!$verified.Passed -or $verified.PortablePackageOnly){throw 'Full current-workspace protection verification required before sealing'}
$gateResult=Get-Content (Join-Path $proof 'Results/b25b-gates.json') -Raw|ConvertFrom-Json
if(!$gateResult.Passed -or $gateResult.Checks.Count -ne 528){throw 'Original completed B25B gate evidence changed'}
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$status=@(& git.exe --no-optional-locks -C $repo status --short)
[ordered]@{Package='P5O2B25B Evidence Closure / Repository Hygiene';ClosedUtc=[DateTime]::UtcNow.ToString('o');Passed=$true;
    OriginalFiles=$start.OriginalFileCount;OriginalBytes=$start.OriginalBytes;FinalSizeSource='package-seal.json.PackageBytes (includes that seal)';
    OriginalW4PhysicalCopies=6;CanonicalW4Payloads=4;TrueDuplicateExtrasRemoved=2;RawRepresentationsReplaced=6;
    RawAndArchiveSha256InCatalog=$true;AllSixGateContextsRetained=$true;InformationLoss=$false;AllOriginalTrxRetained=20;AllOriginalLogsRetained=77;
    ProductTestsOrGatesRerun=0;ProductionOrTestFilesChanged=0;HistoricalEvaluationFilesUnchanged=735;SourceTestBuildFilesUnchanged=$start.Sources.Count;
    IndexUnchanged=$true;RootIgnoreUnchanged=$true;SixStashesUnchanged=$true;CoreUnchangedDuringClosure=23;PreexistingGeneratedCoreMetadataDifferences=2;
    HistoricalCoreManifestReplaced=$false;HistoricalExit1EvidencePreserved=$true;WriterProcessProved=$false;
    VerificationChecks=$verified.Checks.Count;OriginalB25BChecks=528;OriginalB25BResultsReinterpreted=$false;
    RuntimeStatus='Runtime stability investigation: OPEN - recurrent, unattributed process failure';RuntimeClassifiedAsB25BError=$false;RuntimeCalledFlaky=$false;RuntimeCalledResolved=$false;
    CommitReady='YES: scoped B25B code/tests and sealed evidence; exclude protected root-ignore WIP and Core-obj churn; runtime investigation remains separately OPEN';
    GitStatus=$status;Commit=$false;Push=$false;Rebase=$false;StashMutation=$false} |
    ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $auditPath -Encoding UTF8
$files=@(Get-ChildItem -LiteralPath $proof -File -Recurse | Sort-Object FullName | ForEach-Object {
    [pscustomobject][ordered]@{Path=$_.FullName.Substring($proof.Length+1).Replace('\','/');Bytes=$_.Length;Sha256=(Get-EvidenceHash $_.FullName)}
})
$size=[long](($files|Measure-Object Bytes -Sum).Sum)
$report=Join-Path $tool 'Evaluation/P5O2B25B-bounded-solution-finding-adoption.md'
$seal=[ordered]@{Schema='B25B-Evidence-Seal-v1';ImmutableAfterClosure=$true;CreatedUtc=[DateTime]::UtcNow.ToString('o');
    Meaning='Hash inventory of this completed package, not a replacement historical Core baseline';PackageFiles=$files.Count+1;PackageBytes=0;
    Report='Evaluation/P5O2B25B-bounded-solution-finding-adoption.md';ReportSha256=(Get-EvidenceHash $report);Files=$files}
# Fixed point accounts for the seal's own byte length without a self-referential digest.
# All OTHER files plus report are hash-bound; Git subsequently binds the seal itself.
do {
    $oldSize=$seal.PackageBytes
    $json=$seal|ConvertTo-Json -Depth 10
    $seal.PackageBytes=$size+[Text.Encoding]::UTF8.GetPreamble().Length+[Text.Encoding]::UTF8.GetByteCount($json+"`r`n")
} while($seal.PackageBytes -ne $oldSize)
$seal|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $sealPath -Encoding UTF8
$actual=@(Get-ChildItem -LiteralPath $proof -File -Recurse)
$actualBytes=[long](($actual|Measure-Object Length -Sum).Sum)
if($actualBytes -ne $seal.PackageBytes -or $actual.Count -ne $seal.PackageFiles){throw 'Package size/count does not match seal'}
Write-Output ('SEALED: '+$actual.Count+' files; '+$actualBytes+' bytes; '+[math]::Round($actualBytes/1MB,4)+' MiB; no commit/push')
