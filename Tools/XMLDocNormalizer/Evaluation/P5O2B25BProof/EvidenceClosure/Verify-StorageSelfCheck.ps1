$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'EvidenceStorage.psm1') -Force
$proof=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$destination=Join-Path $PSScriptRoot 'storage-selfcheck.json'
if(Test-Path -LiteralPath $destination){throw 'Preserve previous storage selfcheck'}
$catalog=Get-Content (Join-Path $proof 'evidence-storage-manifest.json') -Raw | ConvertFrom-Json
$map=@($catalog.OriginalPaths|Where-Object CapturedByGate -CEQ 'full-harness')[0]
$scratch=Join-Path ([IO.Path]::GetTempPath()) ('b25b-evidence-io-'+[Guid]::NewGuid().ToString('N'))
$validation=Join-Path $scratch 'validation'
$raw=Join-Path $validation 'artifacts/p5o2b25b/adoption/W4-equal-results-different-graphs.json'
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($raw)) -Force | Out-Null
& (Join-Path $PSScriptRoot 'Expand-W4Evidence.ps1') -Gate full-harness -OutputPath $raw
if($LASTEXITCODE -and $LASTEXITCODE -ne 0){throw 'Expansion failed'}
$fresh=Join-Path $scratch 'fresh-package'
$records=Join-Path $fresh 'Results'
New-Item -ItemType Directory -Path $records -Force | Out-Null
foreach($gate in @('storage-probe-a','storage-probe-b')) {
    [ordered]@{Meaning='Storage I/O selfcheck only; NOT an actual product/test gate';StartedUtc=[DateTime]::UtcNow.ToString('o')} |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $records ($gate+'.json')) -Encoding UTF8
    Save-B25BEvidence -ValidationRoot $validation -RecordsRoot $records -Gate $gate
}
$blobs=@(Get-ChildItem -LiteralPath (Join-Path $fresh 'Canonical/W4') -File -Filter '*.json.gz')
if($blobs.Count -ne 1){throw 'Capture duplicated an identical payload'}
$a=Get-Content (Join-Path $records 'storage-probe-a-evidence/capture-manifest.json') -Raw | ConvertFrom-Json
$b=Get-Content (Join-Path $records 'storage-probe-b-evidence/capture-manifest.json') -Raw | ConvertFrom-Json
if($a.Files[0].CanonicalArchive -cne $b.Files[0].CanonicalArchive -or $a.Files[0].RawSha256 -cne $map.RawSha256){throw 'Capture identity mismatch'}
foreach($gate in @('storage-probe-a','storage-probe-b')) {
    if(Test-Path (Join-Path $records ($gate+'-evidence/adoption/W4-equal-results-different-graphs.json'))){throw 'Bulk raw copy regression'}
}
$rejected=$false
try { [B25BEvidenceStorage.ArchiveCodec]::Verify($blobs[0].FullName,('0'*64),$map.RawBytes) } catch { $rejected=$true }
if(!$rejected){throw 'Incorrect raw checksum was accepted'}
if((Get-EvidenceHash $raw) -cne $map.RawSha256){throw 'Restoration did not preserve raw bytes'}
[ordered]@{Passed=$true;Scope='Storage I/O only, no product/test execution';Scratch=$scratch;RawRestoreSha256=$map.RawSha256;
    GateSnapshots=2;PhysicalCanonicalPayloads=1;RawCopiesInSnapshots=0;IncorrectRawSha256Rejected=$rejected;
    ArchiveSha256=(Get-EvidenceHash $blobs[0].FullName);OriginalArchiveSha256=$map.ArchiveSha256;
    SameEncoderArchiveByteIdentical=(Get-EvidenceHash $blobs[0].FullName) -ceq $map.ArchiveSha256;ProductTestsStarted=0} |
    ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $destination -Encoding UTF8
Write-Output 'PASS: exact raw restore; two capture contexts share one archive; wrong hash rejected; zero product tests'
