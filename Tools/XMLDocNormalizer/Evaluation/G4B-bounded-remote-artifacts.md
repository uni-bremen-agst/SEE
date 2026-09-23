# G4B bounded remote artifact candidate discovery

## Invariant

Remote locations, package identities, versions, dependency ranges, TFMs, and
archive paths are discovery hints only. They never establish metadata-reference
identity. A candidate is `RemoteExact` only after the unchanged P5 image-kind,
COFF timestamp, `SizeOfImage`, and MVID validation accepts the exact immutable
bytes. G4B has no likely/compatible/best-match state and does not modify binary
images.

## Missing-reference inventory

The G4 LocalOnly baseline contains 175 unavailable references:

| Primary artifact class | Count | Evidence |
| --- | ---: | --- |
| A. .NET reference/targeting pack | 159 | Newtonsoft.Json `net6.0` reference closure; the bounded historical probe identified `Microsoft.NETCore.App.Ref` 6.0.14 through P5 |
| B. runtime/shared framework | 0 | No remaining implementation-image provenance required this provider |
| C. known package dependency | 16 | Scrutor/Serilog NuSpecs, their official transitive NuGet catalog metadata, and Scrutor's NuSpec-pinned repository revision |
| D. third-party package candidate | 0 | The initially heuristic JetBrains candidate became an exact PackageReference hint from the pinned Scrutor revision |
| E. symbol/image server | 0 | PE keys could form a bounded image-store lookup, but no row needed it after stronger package/pack provenance resolved the matrix |
| F. known release artifact | 0 | No row carried an independent official release-archive coordinate |
| G. unknown | 0 | Every row has a bounded package or pack container hint |

The generated `missing-reference-matrix.json` and Markdown report retain each
library, ordinal, name/simple name, unavailable assembly-version/public-key-token
fields, metadata image kind, MVID, timestamp, image size, aliases,
`EmbedInteropTypes`, local outcome, primary class, discovery evidence, possible
provider, final outcome, and RemoteExact provenance.

## Provider investigation and implementation

NuGet V3 is implemented for exact package/version hints and bounded package
search. The service index supplies Package Base Address, Registration Base URL,
and Search Query Service endpoints. Registration leaves identify package
content and catalog entries; catalog SHA-512 and package size establish
transport integrity. Search ranks an exact package-ID match first, but every
contained binary still goes through P5.

Historical .NET reference/targeting packs are implemented as a distinct
provider kind over official pack package artifacts. TFM narrows candidates but
does not select a current pack as historical truth. A publication-era probe of
6.0.14, 6.0.15, 6.0.16, and 6.0.18 found that only 6.0.14 passes P5 for the
Newtonsoft.Json reference images.

Runtime/shared-framework artifacts were investigated but not implemented:
none of the 175 primary classifications requires an implementation image.
Microsoft symbol stores have documented image lookup keys, but a provider would
add no candidate coverage for this matrix and no request was made. Explicit
release archives were excluded because no missing reference has a separate
release-asset coordinate. General web, repository, DLL-download, and unknown
mirror searches remain out of scope.

## Bounds and safety

Remote acquisition is context-local and disabled by default. The production
defaults are three search results per reference, three versions per package,
four package downloads per reference, 12 requests per reference, 128 requests
per run, 32 MiB downloaded per reference, 128 MiB per run, 16 MiB per artifact,
16 MiB per archive entry, 256 binary entries per artifact, three redirects,
and 15 seconds per request chain. A reached limit returns `Unavailable`; limits
are never increased automatically.

The existing P7B transport enforces HTTPS, rejects URI credentials, localhost,
loopback, private/link-local addresses, revalidates redirects and DNS results,
pins socket connections to validated public addresses, disables proxies and
cookies, and bounds decompressed responses. ZIP handling rejects traversal,
absolute/drive paths, duplicate case-insensitive entries, malformed archives,
oversized entries, excessive expanded size, and excessive candidate counts.
Only `ref`, `lib`, and `runtimes` PE entries are streamed in memory; nothing is
installed or executed.

Service metadata, searches, artifact bytes, positive validations, and negative
reference results are cached inside one acquisition context. One context gate
provides exactly-once download behavior under concurrent requests. Cache keys
include provider kind, package ID/version, and complete P5 binary provenance.

## Canonical evaluation result

Both profiles use `VerifiedLineEndings`. LocalOnly reproduces the G4 baseline.
`BoundedRemoteArtifacts` changes only reference acquisition:

| Library | Before | After | Requests | Artifact requests | Final boundary |
| --- | --- | --- | ---: | ---: | --- |
| Scrutor | 113 LocalExact, 11 unavailable | 113 LocalExact, 11 RemoteExact | 34 | 11 | P5K signed-target rejection |
| Serilog | 113 LocalExact, 5 unavailable | 113 LocalExact, 5 RemoteExact | 16 | 5 | P5K signed-target rejection |
| Newtonsoft.Json | 0 LocalExact, 159 unavailable | 159 RemoteExact | 4 | 1 | P5K signed-target rejection |

The control cases make zero G4B requests: Dapper stops at unsupported
`release-debug-plus`, OneOf lacks an exact PDB, Polly has 113 LocalExact
references and stops at signing, and Semver has 113 LocalExact references and
continues to its existing successful source-backed analysis. G4B does not fix
any of those independent boundaries.
