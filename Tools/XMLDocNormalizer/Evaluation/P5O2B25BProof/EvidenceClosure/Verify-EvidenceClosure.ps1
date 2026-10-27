param([switch]$Prepared,[switch]$PortablePackageOnly,[string]$Record)
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'EvidenceStorage.psm1') -Force
$proof=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$tool=[IO.Path]::GetFullPath((Join-Path $proof '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$start=Get-Content (Join-Path $PSScriptRoot 'start.json') -Raw | ConvertFrom-Json
$catalog=Get-Content (Join-Path $proof 'evidence-storage-manifest.json') -Raw | ConvertFrom-Json
$checks=[Collections.Generic.List[object]]::new()
function Check([string]$name,[bool]$passed) {$checks.Add([pscustomobject]@{Name=$name;Passed=$passed})}
function GitRead([string[]]$arguments) { $out=@(& git.exe --no-optional-locks -C $repo @arguments); if($LASTEXITCODE -ne 0){throw 'Read-only Git failed'}; $out }
try {
    Check 'six-gates-four-raw-contents-two-duplicate-extras' ($catalog.OriginalPaths.Count -eq 6 -and $catalog.Blobs.Count -eq 4 -and $catalog.TrueDuplicateExtraCopies -eq 2)
    foreach($blob in $catalog.Blobs) {
        $path=Resolve-EvidencePath $proof $blob.ArchivePath
        Check ('archive-sha256:'+$blob.RawSha256) ((Get-EvidenceHash $path) -ceq $blob.ArchiveSha256)
        [B25BEvidenceStorage.ArchiveCodec]::Verify($path,$blob.RawSha256,$blob.RawBytes)
        Check ('decoded-sha-size-crc-isize:'+$blob.RawSha256) $true
        Check ('repeat-encoder-byte-identical:'+$blob.RawSha256) ($blob.RepeatArchiveSha256 -ceq $blob.ArchiveSha256)
    }
    foreach($original in $start.W4) {
        $map=@($catalog.OriginalPaths|Where-Object OriginalPath -CEQ $original.Path)
        Check ('unique-gate-binding:'+$original.CapturedByGate) ($map.Count -eq 1)
        $map=$map[0]
        Check ('original-raw-identity-preserved:'+$original.CapturedByGate) ($map.RawSha256 -ceq $original.Sha256 -and $map.RawBytes -eq $original.Bytes)
        Check ('producer-context-preserved:'+$original.CapturedByGate) ($map.ProducerGate -ceq $original.ProducerGate -and $map.ProducedInCapturedGate -eq $original.ProducedInCapturedGate)
        Check ('gate-record-unchanged:'+$original.CapturedByGate) ((Get-EvidenceHash (Resolve-EvidencePath $proof $map.GateRecord)) -ceq $original.GateRecordSha256)
        $blob=@($catalog.Blobs|Where-Object RawSha256 -CEQ $map.RawSha256)
        Check ('gate-to-canonical-archive:'+$original.CapturedByGate) ($blob.Count -eq 1 -and $blob[0].ArchivePath -ceq $map.CanonicalArchive -and $blob[0].ArchiveSha256 -ceq $map.ArchiveSha256)
        $capture=Get-Content (Join-Path $proof ('Results/'+$map.CapturedByGate+'-evidence/capture-manifest.json')) -Raw | ConvertFrom-Json
        Check ('explicit-gate-capture-manifest:'+$original.CapturedByGate) ($capture.Files.Count -eq 1 -and $capture.Files[0].RawSha256 -ceq $map.RawSha256 -and $capture.Files[0].CanonicalArchive -ceq $map.CanonicalArchive -and $capture.ProducerGate -ceq $map.ProducerGate)
        $raw=Resolve-EvidencePath $proof $original.Path
        if($Prepared) {Check ('raw-still-safe-before-retirement:'+$original.CapturedByGate) ((Get-EvidenceHash $raw) -ceq $original.Sha256)}
        else {Check ('no-extra-raw-copy:'+$original.CapturedByGate) (!(Test-Path -LiteralPath $raw))}
        [xml]$trx=Get-Content (Join-Path $proof ('Results/'+$map.CapturedByGate+'.trx')) -Raw
        $executed=@($trx.TestRun.Results.UnitTestResult | Where-Object testName -like '*.W4_EqualResultsFromDifferentGenuineGraphsCannotBeCrossAdopted')
        Check ('producer-vs-snapshot-not-confused:'+$original.CapturedByGate) (($executed.Count -eq 1) -eq [bool]$map.ProducedInCapturedGate)
    }
    foreach($group in $start.W4|Group-Object Sha256) {
        $targets=@($catalog.OriginalPaths|Where-Object RawSha256 -CEQ $group.Name|Select-Object -ExpandProperty CanonicalArchive -Unique)
        Check ('one-physical-content-per-raw-hash:'+$group.Name) ($targets.Count -eq 1)
    }
    $rawPaths=@($start.W4|ForEach-Object {$_.Path})
    foreach($file in $start.OriginalFiles) {
        if($rawPaths -contains $file.Path){continue}
        if($file.Path -in @('Run-Validation.ps1','Witness-Index.md')){continue}
        Check ('original-evidence-byte-unchanged:'+$file.Path) ((Get-EvidenceHash (Resolve-EvidencePath $proof $file.Path)) -ceq $file.Sha256)
    }
    # Prove the complete gate runner is unchanged except the reversible storage block, not merely selected filters.
    $runner=[IO.File]::ReadAllText((Join-Path $proof 'Run-Validation.ps1')).Replace("`r`n","`n")
    $added=@'
if ($Phase -ne 'Dual') {
    # Storage only: final gate record is unchanged; W4 is content-addressed, never bulk-copied per gate.
    Import-Module (Join-Path $PSScriptRoot 'EvidenceClosure/EvidenceStorage.psm1') -Force
    Save-B25BEvidence -ValidationRoot $validation -RecordsRoot $records -Gate $name
}
'@
    $old=@'
    foreach ($directory in @('adoption','b25')) {
        $source=Join-Path $validation ('artifacts/p5o2b25b/' + $directory)
        if (Test-Path -LiteralPath $source) {
            $destination=Join-Path $records ($name + '-evidence/' + $directory)
            New-Item -ItemType Directory -Path $destination -Force | Out-Null
            Get-ChildItem -LiteralPath $source -File | Copy-Item -Destination $destination
        }
    }
'@
    $reverse=$runner.Replace($added.Replace("`r`n","`n")+"`n",'')
    $needle="    if (`$state.Counters.failed -ne '0' -or `$state.Counters.notExecuted -ne '0' -or `$state.Counters.total -eq '0') { `$state.ExitCode = 1 }`n"
    $reverse=$reverse.Replace($needle,$needle+$old.Replace("`r`n","`n")+"`n")
    $sha=[Security.Cryptography.SHA256]::Create()
    try { $hash=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($reverse))).Replace('-','') } finally {$sha.Dispose()}
    $expected=@($start.OriginalFiles|Where-Object Path -CEQ 'Run-Validation.ps1')[0].Sha256
    Check 'exact-original-gate-script-recovered-by-reversing-storage-only-diff' ($hash -ceq $expected)
    $selfcheckPath=Join-Path $PSScriptRoot 'storage-selfcheck.json'
    if(Test-Path -LiteralPath $selfcheckPath) {
        $io=Get-Content -LiteralPath $selfcheckPath -Raw|ConvertFrom-Json
        Check 'raw-restore-and-future-capture-no-duplicate-payloads' ($io.Passed -and $io.GateSnapshots -eq 2 -and $io.PhysicalCanonicalPayloads -eq 1 -and $io.RawCopiesInSnapshots -eq 0 -and $io.IncorrectRawSha256Rejected -and $io.SameEncoderArchiveByteIdentical -and $io.ProductTestsStarted -eq 0)
    } elseif(!$Prepared) {Check 'storage-selfcheck-present' $false}
    [xml]$red=Get-Content (Join-Path $proof 'Results/full.trx') -Raw
    Check 'red-historical-full-3448-of-3449-retained' ($red.TestRun.ResultSummary.Counters.total -eq '3449' -and $red.TestRun.ResultSummary.Counters.passed -eq '3448' -and $red.TestRun.ResultSummary.Counters.failed -eq '1')
    if(!$PortablePackageOnly) {
        foreach($file in $start.Sources){Check ('no-source-test-build-drift:'+$file.Path) ((Get-EvidenceHash (Join-Path $repo $file.Path)) -ceq $file.Sha256)}
        foreach($file in $start.HistoricalFiles){Check ('historical-B25-A1-A10-unchanged:'+$file.Path) ((Get-EvidenceHash (Join-Path $repo $file.Path)) -ceq $file.Sha256)}
        foreach($file in $start.Core){Check ('core-entry-bytes-unchanged:'+$file.Path) ((Get-EvidenceHash $file.Path) -ceq $file.ClosureEntrySha256)}
    }
    Check 'preexisting-core-metadata-drift-explicit-not-rebased' ($start.PreexistingCoreDifferences.Count -eq 2 -and $start.MetadataReconstructions[0].AssemblyInfoSha256 -ceq $start.PreexistingCoreDifferences[0].HistoricalSha256 -and $start.MetadataReconstructions[1].AssemblyInfoSha256 -ceq $start.PreexistingCoreDifferences[0].ClosureEntrySha256 -and $start.MetadataReconstructions[0].CacheSha256 -ceq $start.PreexistingCoreDifferences[1].HistoricalSha256 -and $start.MetadataReconstructions[1].CacheSha256 -ceq $start.PreexistingCoreDifferences[1].ClosureEntrySha256)
    if(!$PortablePackageOnly) {
        Check 'head-unchanged' (@(GitRead @('rev-parse','HEAD'))[0] -ceq $start.Head)
        Check 'physical-index-unchanged' ((Get-EvidenceHash (Join-Path $repo '.git/index')) -ceq $start.IndexSha256)
        Check 'semantic-index-empty' (@(GitRead @('diff','--cached','--name-only')).Count -eq 0)
        Check 'foreign-root-ignore-wip-unchanged' ((Get-EvidenceHash (Join-Path $repo '.gitignore')) -ceq $start.RootIgnoreSha256)
        Check 'six-stashes-unchanged' ((@(GitRead @('stash','list','--format=%H %gs')) -join "`n") -ceq ($start.Stashes -join "`n"))
    }
    $report=Join-Path $tool 'Evaluation/P5O2B25B-bounded-solution-finding-adoption.md'
    foreach($match in [regex]::Matches([IO.File]::ReadAllText($report),'\]\(([^)]+)\)')) {
        $link=$match.Groups[1].Value
        if($link -match '^https?://|^#'){continue}
        $path=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($report)) ($link.Split('#')[0])))
        Check ('report-link-resolves:'+$link) (Test-Path -LiteralPath $path)
    }
    $sealPath=Join-Path $PSScriptRoot 'package-seal.json'
    if(Test-Path -LiteralPath $sealPath) {
        $seal=Get-Content -LiteralPath $sealPath -Raw | ConvertFrom-Json
        foreach($file in $seal.Files){Check ('sealed-package-file:'+$file.Path) ((Get-EvidenceHash (Resolve-EvidencePath $proof $file.Path)) -ceq $file.Sha256)}
        $actual=@(Get-ChildItem -LiteralPath $proof -File -Recurse | Where-Object FullName -NE $sealPath)
        Check 'no-unsealed-additional-files' ($actual.Count -eq $seal.Files.Count)
        Check 'sealed-report-unchanged' ((Get-EvidenceHash $report) -ceq $seal.ReportSha256)
    }
} catch {$errorDetail=$_.Exception.ToString();Check 'complete-verification' $false}
$failed=@($checks|Where-Object {!$_.Passed})
$result=[ordered]@{Scope='Evidence storage only; no product gates rerun';PortablePackageOnly=[bool]$PortablePackageOnly;
    PortableExcludesLiveWorkspaceProtections=[bool]$PortablePackageOnly;CapturedUtc=[DateTime]::UtcNow.ToString('o');Prepared=[bool]$Prepared;Passed=$failed.Count -eq 0;
    Checks=@($checks.ToArray());Failures=$failed;Error=$errorDetail;ProductTestsStarted=0;HistoricalCoreBaselineReplaced=$false;
    RuntimeStatus='Runtime stability investigation: OPEN - recurrent, unattributed process failure'}
if($Record) {
    if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'package-seal.json')){throw 'Sealed package: verification is read-only; record outside this package if needed'}
    if(Test-Path -LiteralPath $Record){throw 'Preserve prior verification record'}
    $result|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $Record -Encoding UTF8
}
if($failed.Count){$failed|Format-Table; if($errorDetail){Write-Output $errorDetail};exit 1}
Write-Output ('PASS: '+$checks.Count+' storage/protection/reference checks; zero product/test gate executions')
