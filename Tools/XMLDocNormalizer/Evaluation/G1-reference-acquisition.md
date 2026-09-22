# G1 exact local reference acquisition

G1 extends the existing P7A candidate search. It does not define another
binary identity and does not weaken P5B/P5C. Every path from every source is
only a candidate until the recorded metadata image kind, MVID, PE timestamp,
and `SizeOfImage` validate exactly.

## E1 root cause

Before G1, both candidates searched only the prepared
`NETStandard.Library/2.0.3` reference directory. Its 113 top-level reference
assemblies all validate for the corresponding P5A ordinals. The remaining
dependency binaries are not in that directory. Existing P7A consequently
tested the expected-name path and its bounded top-directory fallback but
could not produce a complete ordinal sequence.

All entries below have `MetadataImageKind=Assembly`, empty aliases, and
`EmbedInteropTypes=false`.

### Scrutor 4.2.2 (`113/124`, 11 missing)

| Ordinal | Expected name | MVID | Timestamp | SizeOfImage |
| ---: | --- | --- | ---: | ---: |
| 0 | JetBrains.Annotations.dll | `f4fafbf9-d9c5-437a-8784-595cb75bf5ad` | -2142421064 | 122880 |
| 1 | Microsoft.Bcl.AsyncInterfaces.dll | `dbf6bd22-a3d3-4914-97ef-338bbbd08ac8` | -1715040096 | 32768 |
| 2 | Microsoft.Extensions.DependencyInjection.Abstractions.dll | `9ae75e73-bb6c-463d-b402-9a2512b65bd5` | -1806920108 | 57344 |
| 3 | Microsoft.Extensions.DependencyModel.dll | `2357f136-e271-46bd-906f-7e42fca2b020` | -429251696 | 81920 |
| 8 | System.Buffers.dll | `03321e3a-bb6d-4fe5-9065-a8364bf8e636` | 1582131356 | 32768 |
| 54 | System.Memory.dll | `364de6aa-3638-4f0c-b478-eff7d6f86309` | 1582131525 | 155648 |
| 68 | System.Numerics.Vectors.dll | `fd80350a-03fb-4f81-873a-acb5119f4935` | 1500482223 | 49152 |
| 76 | System.Runtime.CompilerServices.Unsafe.dll | `71b3a0f0-c0fb-4cde-9f07-04ea18373868` | -127126 | 32768 |
| 100 | System.Text.Encodings.Web.dll | `5feadb31-149e-48b1-8265-5fa31bb85715` | -2001848599 | 73728 |
| 101 | System.Text.Json.dll | `815ff95f-4d6a-463f-a6a3-be490e0514aa` | -1799726202 | 311296 |
| 106 | System.Threading.Tasks.Extensions.dll | `619062a8-972f-4ae5-bbee-e36ac541d14f` | 1582131540 | 40960 |

### Serilog 3.1.1 (`113/118`, 5 missing)

| Ordinal | Expected name | MVID | Timestamp | SizeOfImage |
| ---: | --- | --- | ---: | ---: |
| 4 | System.Buffers.dll | `03321e3a-bb6d-4fe5-9065-a8364bf8e636` | 1582131356 | 32768 |
| 20 | System.Diagnostics.DiagnosticSource.dll | `81fc8a4c-93ac-48de-af31-7729737a5f95` | -109624376 | 172032 |
| 51 | System.Memory.dll | `00be0fb5-88be-459b-8356-ff9591cac0f7` | 1651979883 | 155648 |
| 65 | System.Numerics.Vectors.dll | `fd80350a-03fb-4f81-873a-acb5119f4935` | 1500482223 | 49152 |
| 73 | System.Runtime.CompilerServices.Unsafe.dll | `a8391708-f361-428d-a4a8-45fb3e6b1001` | 1634946047 | 32768 |

## Local-source result

The G1 rerun checked these bounded local sources:

1. the existing explicit E1 reference root;
2. already loaded file-backed Roslyn references;
3. installed `.NET` `packs/*/*/ref/*` assets;
4. installed `.NET` `shared/*/*` assets;
5. the resolved NuGet global-packages folder, limited to `lib`, `ref`, and
   `runtimes/*/lib` asset directories.

The local NuGet cache contains some same-name candidates, including newer
`Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Extensions.DependencyInjection.Abstractions`,
`System.Buffers`, `System.Memory`, `System.Numerics.Vectors`,
`System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`,
and `System.Diagnostics.DiagnosticSource` package assets. Installed .NET 8.0.24
reference/runtime assets and NETStandard 2.1 reference assets supply further
same-name candidates. P5 rejects all of them for the missing ordinals. The
required historical builds are not locally present. No candidate was accepted
by filename, package ID, version, TFM, pack location, or runtime location.

The per-ordinal same-name candidate audit was:

| Candidate / ordinal | Expected reference | Bounded local same-name source | P5 exact match |
| --- | --- | --- | --- |
| Scrutor / 0 | JetBrains.Annotations.dll | None | No |
| Scrutor / 1 | Microsoft.Bcl.AsyncInterfaces.dll | NuGet global packages | No |
| Scrutor / 2 | Microsoft.Extensions.DependencyInjection.Abstractions.dll | NuGet global packages, .NET packs/shared framework | No |
| Scrutor / 3 | Microsoft.Extensions.DependencyModel.dll | None | No |
| Scrutor / 8 | System.Buffers.dll | NuGet global packages, .NET packs/shared framework | No |
| Scrutor / 54 | System.Memory.dll | NuGet global packages, .NET packs/shared framework | No |
| Scrutor / 68 | System.Numerics.Vectors.dll | NuGet global packages, .NET packs/shared framework | No |
| Scrutor / 76 | System.Runtime.CompilerServices.Unsafe.dll | NuGet global packages, .NET packs/shared framework | No |
| Scrutor / 100 | System.Text.Encodings.Web.dll | .NET packs/shared framework | No |
| Scrutor / 101 | System.Text.Json.dll | .NET packs/shared framework | No |
| Scrutor / 106 | System.Threading.Tasks.Extensions.dll | NuGet global packages, .NET packs/shared framework | No |
| Serilog / 4 | System.Buffers.dll | NuGet global packages, .NET packs/shared framework | No |
| Serilog / 20 | System.Diagnostics.DiagnosticSource.dll | NuGet global packages, .NET packs/shared framework | No |
| Serilog / 51 | System.Memory.dll | NuGet global packages, .NET packs/shared framework | No |
| Serilog / 65 | System.Numerics.Vectors.dll | NuGet global packages, .NET packs/shared framework | No |
| Serilog / 73 | System.Runtime.CompilerServices.Unsafe.dll | NuGet global packages, .NET packs/shared framework | No |

Therefore the post-G1 real-world result remains deliberately fail closed:

| Candidate | Before G1 | After G1 | Exact local additions |
| --- | ---: | ---: | ---: |
| Scrutor 4.2.2 | 113/124 | 113/124 | 0 |
| Serilog 3.1.1 | 113/118 | 113/118 | 0 |
| Semver 2.3.0 | 113/113 | 113/113 | 0 required |

## Search bounds and cost model

- Loaded references: `O(known file-backed references)` with an expected-name
  fast path.
- Explicit roots: unchanged P7A top-directory-only behavior.
- .NET packs/shared frameworks: fixed-depth directory traversal followed by
  direct expected-filename probes; no recursive file scan.
- NuGet: an expected-package-name fast path followed, only when necessary, by
  one context-local lazy index of known package asset directories. Each lookup
  probes only the expected filename inside those directories.
- Candidate images are not retained by discovery. P5C owns the immutable image
  used for validation/materialization.

Positive, negative, and ambiguous results remain context-local in the existing
P7A cache. Explicit P6C candidate sequences bypass standard sources, including
when an explicit candidate is wrong. Configuration is immutable after the first
lookup. No network request, package download, restore, external build, global
filesystem scan, or persistent index is performed.
