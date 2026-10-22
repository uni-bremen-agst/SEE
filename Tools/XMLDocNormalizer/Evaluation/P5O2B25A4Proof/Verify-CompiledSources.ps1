$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records=Join-Path $tool 'artifacts/p5o2b25a4'
$validation=(Get-Content (Join-Path $records 'export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$inputs=Get-Content (Join-Path $records 'validation-inputs.json') -Raw -Encoding UTF8 | ConvertFrom-Json
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function Hex($Bytes) { ($Bytes | ForEach-Object { $_.ToString('X2') }) -join '' }
# Metadata-only reader in this isolated PowerShell process. These installed
# net462 dependencies do not alter a project, compiler, Main or Worker image.
$assemblies=@{}
$dependencies=@(foreach($relative in @('system.runtime.compilerservices.unsafe/6.1.0/lib/net462/System.Runtime.CompilerServices.Unsafe.dll','system.buffers/4.6.0/lib/net462/System.Buffers.dll','system.numerics.vectors/4.6.0/lib/net462/System.Numerics.Vectors.dll','system.memory/4.6.0/lib/net462/System.Memory.dll','system.collections.immutable/9.0.0/lib/net462/System.Collections.Immutable.dll','system.reflection.metadata/9.0.0/lib/net462/System.Reflection.Metadata.dll')) {
    $path=Join-Path "$env:USERPROFILE/.nuget/packages" $relative
    $assembly=[Reflection.Assembly]::LoadFrom($path)
    $assemblies[$assembly.GetName().Name]=$assembly
    [ordered]@{Image=(Fingerprint $path);Identity=$assembly.FullName}
})
# The existing 4.6.0 Memory package is assembly-compatible with the metadata
# reader's older 4.5.5 reference; resolve only these explicit already-loaded
# metadata-tool dependencies, never a Roslyn/Analyzer assembly or network source.
$resolver=[ResolveEventHandler]{param($sender,$eventArgs) $name=[Reflection.AssemblyName]::new($eventArgs.Name).Name; if($assemblies.ContainsKey($name)) { return $assemblies[$name] }; return $null}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$bindings=[Collections.Generic.List[object]]::new()
$utf8=[Text.UTF8Encoding]::new($false,$true)
$sourceRows=@(foreach($s in @($inputs.Sources)+@($inputs.SharedSources)) {
    $workspace=Join-Path $tool $s.Path; $export=Join-Path $validation $s.Path
    Require ((Get-FileHash $workspace).Hash -ceq $s.WorkspaceSha256) ('Workspace changed since capture: '+$s.Path)
    Require ((Get-FileHash $export).Hash -ceq $s.ExportSha256) ('Compiled export changed since capture: '+$s.Path)
    $w=$utf8.GetString([IO.File]::ReadAllBytes($workspace)); $e=$utf8.GetString([IO.File]::ReadAllBytes($export))
    Require ($w.Replace("`r`n","`n") -ceq $e.Replace("`r`n","`n")) ('Non-line-ending source difference: '+$s.Path)
    if($s.WorkspaceSha256 -cne $s.ExportSha256) {
        Require (-not $w.Contains('"""') -and -not $w.Contains('@"') -and -not $w.Contains('@$"')) ('Do not assume line-ending-insensitive multiline string semantics: '+$s.Path)
    }
    [ordered]@{Path=$s.Path;WorkspaceSha256=$s.WorkspaceSha256;ExportSha256=$s.ExportSha256;RawByteEqual=($s.WorkspaceSha256 -ceq $s.ExportSha256);ExactAfterCrlfToLfOnly=$true;DifferentFileHasNoRawOrVerbatimStringLiteral=$true}
})
try {
    foreach($imagePath in @('src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll','artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll','Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll')) {
        $image=Join-Path $validation $imagePath; $pdb=[IO.Path]::ChangeExtension($image,'.pdb')
        $captured=@($inputs.Images | Where-Object Path -ceq $imagePath)[0]
        Require ((Get-FileHash $image).Hash -ceq $captured.Sha256) 'Actual executed image differs from capture.'
        $pdbStream=[IO.File]::OpenRead($pdb); $peStream=[IO.File]::OpenRead($image)
        $provider=[System.Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream,[System.Reflection.Metadata.MetadataStreamOptions]::Default,0)
        $pe=[System.Reflection.PortableExecutable.PEReader]::new($peStream)
        try {
            $reader=$provider.GetMetadataReader([System.Reflection.Metadata.MetadataReaderOptions]::Default,$null)
            $id=[System.Reflection.Metadata.BlobContentId]::new($reader.DebugMetadataHeader.Id)
            $bytes=[IO.File]::ReadAllBytes($pdb); $offset=$reader.DebugMetadataHeader.IdStartOffset
            Require ((Hex $bytes[$offset..($offset+19)]) -ceq (Hex $reader.DebugMetadataHeader.Id)) 'Portable PDB ID offset mismatch.'
            [Array]::Clear($bytes,$offset,20)
            $sha=[Security.Cryptography.SHA256]::Create()
            try { $checksum=[BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','') } finally { $sha.Dispose() }
            $codeView=@($pe.ReadDebugDirectory() | Where-Object Type -eq ([System.Reflection.PortableExecutable.DebugDirectoryEntryType]::CodeView))
            $checksumEntries=@($pe.ReadDebugDirectory() | Where-Object Type -eq ([System.Reflection.PortableExecutable.DebugDirectoryEntryType]::PdbChecksum))
            Require ($codeView.Count -eq 1 -and $checksumEntries.Count -eq 1) 'One exact CodeView and PDB checksum required.'
            $cv=$pe.ReadCodeViewDebugDirectoryData($codeView[0]); $cs=$pe.ReadPdbChecksumDebugDirectoryData($checksumEntries[0])
            Require ($cv.Guid -eq $id.Guid -and $codeView[0].Stamp -eq $id.Stamp -and $cv.Age -eq 1) 'PE/PDB ID mismatch.'
            Require ($cs.AlgorithmName -ceq 'SHA256' -and (Hex $cs.Checksum) -ceq $checksum) 'PE deterministic PDB checksum mismatch.'
            $documents=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
            foreach($h in $reader.Documents) {
                $d=$reader.GetDocument($h); $name=$reader.GetString($d.Name).Replace('\','/')
                Require (-not $documents.ContainsKey($name)) 'Duplicate PDB document path.'
                $documents.Add($name,[ordered]@{Algorithm=$reader.GetGuid($d.HashAlgorithm);Sha256=(Hex $reader.GetBlobBytes($d.Hash))})
            }
            $wanted=if($imagePath.StartsWith('Tests/')) { @($inputs.Sources) } else { @($inputs.SharedSources) }
            $checked=@(foreach($s in $wanted) {
                $full=[IO.Path]::GetFullPath((Join-Path $validation $s.Path)).Replace('\','/')
                Require ($documents.ContainsKey($full)) ('Exact PDB document missing: '+$full)
                $d=$documents[$full]
                Require ($d.Algorithm -eq [Guid]'8829d00f-11b8-4213-878b-770e8597ac16' -and $d.Sha256 -ceq $s.ExportSha256) ('Compiled document checksum mismatch: '+$full)
                [ordered]@{Path=$full;Algorithm=$d.Algorithm;Sha256=$d.Sha256;ExactCapturedExportBytes=$true}
            })
            $bindings.Add([ordered]@{Image=(Fingerprint $image);Pdb=(Fingerprint $pdb);PdbId=$id.Guid.ToString();PdbStamp=$id.Stamp;PePdbChecksum=$checksum;CodeViewPath=$cv.Path;PePdbIdentityAndChecksumMatch=$true;Documents=$checked})
        } finally { $pe.Dispose(); $provider.Dispose(); $peStream.Dispose(); $pdbStream.Dispose() }
    }
    $result=[ordered]@{Package='P5O2B25A4';Passed=$true;ReadOnly=$true;GeneratedUtc=[DateTime]::UtcNow.ToString('o');Comparison='Exact CRLF-to-LF replacement only; no trim, token rewrite, name substitution or source edit';Sources=$sourceRows;RawDifferences=@($sourceRows | Where-Object { -not $_.RawByteEqual }).Count;ImageBindings=@($bindings.ToArray());MetadataToolDependencies=$dependencies;ProductOrCompilerVersionChanged=$false;TestOrBuildRerun=$false;CapturedInputManifestRewritten=$false}
    [IO.File]::WriteAllText((Join-Path $records 'compiled-source-verification.json'),(($result | ConvertTo-Json -Depth 15)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Write-Host ('PASS: exact source checksums in Current/Historical/Test PDBs; PE/PDB identities and deterministic SHA256 checksums bound; '+$result.RawDifferences+' pure line-ending differences, raw captures unchanged.')
} finally { [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver) }
