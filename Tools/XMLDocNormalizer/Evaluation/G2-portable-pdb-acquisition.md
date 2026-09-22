# G2 exact Portable PDB acquisition

G2 extends the demand-driven P6C boundary with bounded local Portable PDB
candidate sources. Candidate location never establishes identity. Every image
is passed unchanged to the existing P4B validation in
`ExternalPortablePdbDescriptorFactory` before P5A provenance is read.

## Existing P4A/P4B trust boundary

P4A first revalidates the exact target assembly identity and manifest MVID,
then records all CodeView entries, embedded Portable PDB identifiers, Portable
PDB checksums, and the reproducible marker. For Portable CodeView entries the
expected content identifier is the CodeView GUID plus debug-directory stamp.

P4B accepts only a Portable PDB whose 20-byte `#Pdb` identifier equals a
Portable CodeView or embedded identifier recorded by P4A. When the PE contains
a supported PDB checksum entry, P4B additionally hashes the unchanged PDB
image with its 20-byte ID zeroed and compares the digest in fixed time. Source
Link, Embedded Source, compilation options, and compilation references are read
only after this validation succeeds. G2 does not introduce another validator.

## E1 root cause

| Property | Dapper | OneOf |
| --- | --- | --- |
| Package | 2.1.35 | 3.0.263 |
| Assembly / TFM | Dapper.dll / net7.0 | OneOf.dll / netstandard2.0 |
| CodeView path | `Dapper.pdb` | `C:\projects\oneof\OneOf\obj\Release\netstandard2.0\OneOf.pdb` |
| Portable CodeView GUID | `19c5ec6e-ed66-411b-95ee-5f4359ae20e8` | `4a1f0da9-8060-403f-b91d-a7978ea10064` |
| CodeView stamp | 2326393009 | 4162441531 |
| Age | 1 | 1 |
| PE PDB checksum | SHA256 `6EECC51966ED1BE1D5EE5F4359AE20E8B1F0A98A913CA5F081E3B11544C425E9` | SHA256 `A90D1F4A60803F10791DA7978EA100643BD11978880FAE0FE0D8AE89FA5AB0F4` |
| Reproducible entry | Yes | Yes |
| Embedded Portable PDB | Yes | No |
| PDB in nupkg | No standalone PDB | No |
| Public snupkg | No; official endpoint returned 404 | No; official endpoint returned 404 |
| Exact NuGet symbol-key lookup | Not needed because the PDB is embedded | 404 with exact key and checksum header |
| Sufficient PE identity | Yes | Yes |

Both PEs therefore contain enough provenance to validate an external Portable
PDB. Dapper additionally carries that exact PDB in its PE and requires no
external symbol source. OneOf's standard Portable PDB symbol-server path is:

```text
oneof.pdb/4A1F0DA98060403FB91DA7978EA10064FFFFFFFF/oneof.pdb
```

The CodeView path contributes only the safe filename `OneOf.pdb`; its build
machine directory is never opened. The Portable PDB symbol-key layout was
cross-checked against the .NET symbol-store convention. Remote lookup was not
implemented because the exact public lookup and symbol-package endpoint both
return 404, so remote infrastructure would not address the real candidate.

## Implemented local sources

The opt-in order is:

1. Embedded Portable PDB in the exact target PE.
2. Explicitly known local PDB candidate paths.
3. A sibling file using only a safe Portable CodeView filename.
4. Direct expected-filename probes in explicitly configured local roots.
5. Matching entries streamed from explicitly configured `.nupkg` or `.snupkg`
   archives.

The embedded check is the PE/P4B fast path: it records no discovered candidate
or PDB-file open. Only its unchanged P4B validation attempt is counted. G2
candidate discovery begins after that fast path has no usable embedded image.

Packages are candidate containers only. They are never executed or extracted.
Archive entry names are checked for rooting, drive prefixes, traversal, and
duplicates; entry count and uncompressed PDB size are bounded. Malformed or
oversized archives fail closed. The hard Portable PDB limit is 128 MiB.

Authoritative explicit PDB paths bypass G2 completely. A wrong explicit PDB
fails P4B without falling back to any configured local source. Plans without
explicit local-acquisition opt-in retain their previous behavior.

## Cache, memory, and I/O

Positive, negative, ambiguous, and in-progress results are context-local and
keyed by the target-specific sibling location plus exact manifest MVID, P4A
Portable PDB IDs, and checksums, never by filename alone. Concurrent identical
requests wait for the one in-progress lookup. Configuration is immutable after acquisition starts. Cache
entries retain only validated P4B/P5A descriptors; unvalidated response or
archive bytes are not retained. Embedded and archive images are bounded before
decompression or materialization.

G2 performs no network request, download, restore, build, repository lookup,
decompilation, global filesystem scan, or persistent symbol caching. A remote
configured-HTTPS symbol source remains a possible follow-up if a real exact
symbol is available and the existing P7B network policy is factored into a
general binary-download transport.

## Real-world result

| Candidate | Before G2 | After G2 | New boundary |
| --- | --- | --- | --- |
| Dapper 2.1.35 | PDB missing | Embedded PDB validated; 55 documents and 163 references read | P5G rejects exact `optimization=release-debug-plus` |
| OneOf 3.0.263 | PDB missing | PDB still unavailable | No exact local or public Portable PDB artifact |

Dapper's PDB validation kind is `IdentityAndChecksum`. The newly exposed P5G
boundary is outside G2 and was not relaxed. OneOf remains `MissingArtifact`.
The other five E1 candidates retain their previous result.
