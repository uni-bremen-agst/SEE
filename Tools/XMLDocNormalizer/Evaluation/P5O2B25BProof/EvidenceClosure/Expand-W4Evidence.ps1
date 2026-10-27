param([Parameter(Mandatory=$true)][ValidateSet('broad','boundary','full','harness-isolated','broad-harness','full-harness')][string]$Gate,
    [Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'EvidenceStorage.psm1') -Force
$proof=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$catalog=Get-Content (Join-Path $proof 'evidence-storage-manifest.json') -Raw | ConvertFrom-Json
$matches=@($catalog.OriginalPaths|Where-Object CapturedByGate -CEQ $Gate)
if($matches.Count -ne 1){throw 'Gate must map to exactly one canonical payload'}
$map=$matches[0]; $archive=Resolve-EvidencePath $proof $map.CanonicalArchive
if((Get-EvidenceHash $archive) -cne $map.ArchiveSha256){throw 'Archive hash mismatch'}
$output=[IO.Path]::GetFullPath($OutputPath)
if($output.StartsWith($proof+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Restore into an explicit scratch/output directory, never mutate the sealed package'}
if(Test-Path -LiteralPath $output){throw 'Never overwrite a restored witness'}
[B25BEvidenceStorage.ArchiveCodec]::Expand($archive,$output,$map.RawSha256,$map.RawBytes)
if((Get-EvidenceHash $output) -cne $map.RawSha256){throw 'Expanded bytes do not match'}
Write-Output ('Restored '+$Gate+' byte-for-byte: '+$map.RawSha256)
