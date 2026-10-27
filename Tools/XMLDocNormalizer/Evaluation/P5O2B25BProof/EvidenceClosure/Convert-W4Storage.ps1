param([Parameter(Mandatory=$true)][ValidateSet('Pack','RetireRaw')][string]$Phase)
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'EvidenceStorage.psm1') -Force
$proof=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$start=Get-Content (Join-Path $PSScriptRoot 'start.json') -Raw | ConvertFrom-Json
$catalogPath=Join-Path $proof 'evidence-storage-manifest.json'
if($Phase -eq 'Pack') {
    if(Test-Path -LiteralPath $catalogPath){throw 'Preserve existing canonical catalog'}
    $blobs=@(foreach($group in $start.W4 | Group-Object Sha256) {
        $source=Resolve-EvidencePath $proof $group.Group[0].Path
        if((Get-EvidenceHash $source) -cne $group.Name){throw 'Raw witness changed since audit'}
        $blob=Add-CanonicalW4 $proof $source
        # Confirm deterministic bytes on this documented encoder, without adding a second repository copy.
        $scratch=Join-Path ([IO.Path]::GetTempPath()) ('b25b-evidence-repeat-'+[Guid]::NewGuid().ToString('N')+'.gz')
        [B25BEvidenceStorage.ArchiveCodec]::Compress($source,$scratch)
        if((Get-EvidenceHash $scratch) -cne $blob.ArchiveSha256){throw 'Encoder did not reproduce identical bytes'}
        [B25BEvidenceStorage.ArchiveCodec]::Verify($scratch,$blob.RawSha256,$blob.RawBytes)
        if(![IO.Path]::GetFullPath($scratch).StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe scratch target'}
        Remove-Item -LiteralPath $scratch
        $blob | Add-Member -NotePropertyName RepeatArchiveSha256 -NotePropertyValue $blob.ArchiveSha256
        $blob
    })
    $maps=@(foreach($row in $start.W4) {
        $blob=@($blobs | Where-Object RawSha256 -CEQ $row.Sha256)[0]
        [pscustomobject][ordered]@{OriginalPath=$row.Path;CapturedByGate=$row.CapturedByGate;ProducerGate=$row.ProducerGate;ProducedInCapturedGate=$row.ProducedInCapturedGate;
            GateRecord=$row.GateRecord;GateRecordSha256=$row.GateRecordSha256;ProducerRecord=$row.ProducerRecord;RawBytes=$row.Bytes;RawSha256=$row.Sha256;
            CanonicalArchive=$blob.ArchivePath;ArchiveSha256=$blob.ArchiveSha256;DuplicateGroup=$row.Sha256;Role=$row.Role}
    })
    [ordered]@{Schema='B25B-W4-Storage-v1';ImmutableCatalog=$true;Package='P5O2B25B';CreatedUtc=[DateTime]::UtcNow.ToString('o');
        Format='Single-member RFC1952 gzip. Fixed header MTIME0/OS255/no filename; DeflateStream Optimal; explicit CRC32 and ISIZE. Encoder runtime recorded; cross-runtime compressed bytes are not promised, RAW SHA256 is normative.';
        RawJsonPermanentlyRequiredInRepository=$false;RuntimeRawProducerAndGateUnchanged=$true;SemanticDeduplicationPerformed=$false;
        OriginalPaths=$maps;Blobs=$blobs;OriginalPhysicalCopies=$maps.Count;CanonicalPayloads=$blobs.Count;TrueDuplicateExtraCopies=$maps.Count-$blobs.Count;
        SourceAudit='EvidenceClosure/start.json';UncompressedContentNeverRewritten=$true} |
        ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $catalogPath -Encoding UTF8
    foreach($map in $maps) {
        $snapshot=Resolve-EvidencePath $proof ('Results/'+$map.CapturedByGate+'-evidence')
        $target=Join-Path $snapshot 'capture-manifest.json'
        if(Test-Path -LiteralPath $target){throw 'Existing gate mapping must not be overwritten'}
        [ordered]@{Schema='B25B-Capture-v1';Migration='Lossless relocation of original snapshot, not a new runtime gate';Gate=$map.CapturedByGate;
            GateRecord=$map.GateRecord;GateRecordSha256=$map.GateRecordSha256;ProducerGate=$map.ProducerGate;ProducedInCapturedGate=$map.ProducedInCapturedGate;
            Files=@([ordered]@{OriginalRelativePath='adoption/W4-equal-results-different-graphs.json';RawSha256=$map.RawSha256;RawBytes=$map.RawBytes;
                CanonicalArchive=$map.CanonicalArchive;ArchiveSha256=$map.ArchiveSha256;Storage='CanonicalGzip'})} |
            ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $target -Encoding UTF8
    }
    Write-Output ('Packed '+$blobs.Count+' distinct contents; all six RAW paths still exist until separate verified retirement')
    exit 0
}
$retirement=Join-Path $PSScriptRoot 'raw-retirement.json'
if(Test-Path -LiteralPath $retirement){throw 'Preserve retirement receipt'}
$catalog=Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
foreach($blob in $catalog.Blobs) {
    $archive=Resolve-EvidencePath $proof $blob.ArchivePath
    if((Get-EvidenceHash $archive) -cne $blob.ArchiveSha256){throw 'Archive hash mismatch: no raw removal'}
    [B25BEvidenceStorage.ArchiveCodec]::Verify($archive,$blob.RawSha256,$blob.RawBytes)
}
$targets=@(foreach($map in $catalog.OriginalPaths) {
    if($map.OriginalPath -cne ('Results/'+$map.CapturedByGate+'-evidence/adoption/W4-equal-results-different-graphs.json')) {throw 'Unexpected raw target'}
    $path=Resolve-EvidencePath $proof $map.OriginalPath
    if((Get-EvidenceHash $path) -cne $map.RawSha256 -or (Get-Item -LiteralPath $path).Length -ne $map.RawBytes){throw 'Raw target changed: no removal'}
    $mapping=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($path))) 'capture-manifest.json'
    if(!(Test-Path -LiteralPath $mapping)){throw 'Missing gate mapping: no removal'}
    [pscustomobject]@{Path=$path;OriginalPath=$map.OriginalPath;RawSha256=$map.RawSha256;RecoverableFrom=$map.CanonicalArchive}
})
# No recursive/glob deletion. Every exact owned target was checked against the package boundary,
# original hash, mapping and independently decoded canonical archive before reaching this loop.
foreach($target in $targets) { Remove-Item -LiteralPath $target.Path }
[ordered]@{RetiredUtc=[DateTime]::UtcNow.ToString('o');RemovedRawPhysicalFiles=$targets.Count;TrueDuplicateExtraCopies=$catalog.TrueDuplicateExtraCopies;
    RecoverableByteForByte=$true;Targets=$targets;CatalogSha256=(Get-EvidenceHash $catalogPath);HistoricalB25ToA10FilesRemoved=0;TrxOrLogsRemoved=0} |
    ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $retirement -Encoding UTF8
Write-Output ('Retired '+$targets.Count+' recoverable raw representations; '+$catalog.TrueDuplicateExtraCopies+' true duplicate extras; no TRX/log/historical evidence deleted')
