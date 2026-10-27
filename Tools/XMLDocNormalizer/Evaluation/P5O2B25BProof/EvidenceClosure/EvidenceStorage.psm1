$ErrorActionPreference='Stop'
if(-not ('B25BEvidenceStorage.ArchiveCodec' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'ArchiveCodec.cs') -ReferencedAssemblies 'System.dll','System.Core.dll','System.IO.Compression.dll'
}
function Get-EvidenceHash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
function Resolve-EvidencePath([string]$Root,[string]$Relative) {
    $base=[IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
    $target=[IO.Path]::GetFullPath((Join-Path $base $Relative))
    if([IO.Path]::IsPathRooted($Relative) -or !$target.StartsWith($base,[StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence path escapes its package' }
    $target
}
function Add-CanonicalW4([string]$ProofRoot,[string]$Source) {
    $hash=Get-EvidenceHash $Source; $size=(Get-Item -LiteralPath $Source).Length
    $relative='Canonical/W4/W4-equal-results-different-graphs.'+$hash+'.json.gz'
    $target=Resolve-EvidencePath $ProofRoot $relative
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
    if(!(Test-Path -LiteralPath $target)) { [B25BEvidenceStorage.ArchiveCodec]::Compress($Source,$target) }
    [B25BEvidenceStorage.ArchiveCodec]::Verify($target,$hash,$size)
    [pscustomobject][ordered]@{RawSha256=$hash;RawBytes=$size;ArchivePath=$relative;ArchiveSha256=(Get-EvidenceHash $target);ArchiveBytes=(Get-Item -LiteralPath $target).Length;
        Format='RFC1952 gzip / RFC1951 DeflateStream Optimal';HeaderHex='1F8B08000000000002FF';Mtime=0;FilenameStored=$false;
        EncoderClr=[Environment]::Version.ToString();EncoderAssembly=[IO.Compression.DeflateStream].Assembly.FullName;
        EncoderFileVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo([IO.Compression.DeflateStream].Assembly.Location).FileVersion}
}
function Save-B25BEvidence([string]$ValidationRoot,[string]$RecordsRoot,[string]$Gate) {
    $proof=[IO.Path]::GetFullPath((Join-Path $RecordsRoot '..'))
    if(Test-Path -LiteralPath (Join-Path $proof 'EvidenceClosure/closure-audit.json')) { throw 'Sealed evidence package: capture into a new package/revision' }
    $snapshot=Resolve-EvidencePath $RecordsRoot ($Gate+'-evidence')
    $manifestPath=Join-Path $snapshot 'capture-manifest.json'
    if(Test-Path -LiteralPath $manifestPath) { throw 'Preserve existing capture context' }
    $record=Resolve-EvidencePath $RecordsRoot ($Gate+'.json')
    $run=Get-Content -LiteralPath $record -Raw | ConvertFrom-Json
    $files=@(foreach($directory in @('adoption','b25')) {
        $source=Join-Path $ValidationRoot ('artifacts/p5o2b25b/'+$directory)
        if(!(Test-Path -LiteralPath $source)){continue}
        foreach($file in Get-ChildItem -LiteralPath $source -File) {
            $relative=$directory+'/'+$file.Name
            if($relative -ceq 'adoption/W4-equal-results-different-graphs.json') {
                $blob=Add-CanonicalW4 $proof $file.FullName
                [ordered]@{OriginalRelativePath=$relative;RawSha256=$blob.RawSha256;RawBytes=$blob.RawBytes;CanonicalArchive=$blob.ArchivePath;ArchiveSha256=$blob.ArchiveSha256;
                    Storage='CanonicalGzip';SourceLastWriteUtc=$file.LastWriteTimeUtc.ToString('o');FreshByTimestamp=$file.LastWriteTimeUtc -ge [DateTime]::Parse($run.StartedUtc).ToUniversalTime()}
            } else {
                $target=Resolve-EvidencePath $snapshot $relative
                New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
                if(Test-Path -LiteralPath $target){throw 'Do not overwrite captured witness'}
                Copy-Item -LiteralPath $file.FullName -Destination $target
                [ordered]@{OriginalRelativePath=$relative;RawSha256=(Get-EvidenceHash $target);RawBytes=$file.Length;Storage='Raw';SourceLastWriteUtc=$file.LastWriteTimeUtc.ToString('o')}
            }
        }
    })
    New-Item -ItemType Directory -Path $snapshot -Force | Out-Null
    [ordered]@{Schema='B25B-Capture-v1';Gate=$Gate;CapturedUtc=[DateTime]::UtcNow.ToString('o');GateRecord=$Gate+'.json';GateRecordSha256=(Get-EvidenceHash $record);
        Meaning='Artifact snapshot; presence does NOT assert that this gate executed every witness. Source timestamps and gate TRX retain that distinction.';Files=$files} |
        ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
}
Export-ModuleMember -Function Get-EvidenceHash,Resolve-EvidencePath,Add-CanonicalW4,Save-B25BEvidence
