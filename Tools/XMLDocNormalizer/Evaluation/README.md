# E1 real-world end-to-end evaluation

This opt-in harness evaluates unchanged published DLL, Portable PDB, and Source Link artifacts through the existing XMLDocNormalizer P3-P7 factories. It is separate from the user-facing CLI and from the normal test suite.

## Reproducible workflow

From the XMLDocNormalizer repository directory, prepare all pinned inputs once:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Evaluation\prepare-real-world-evaluation.ps1
```

The preparation command downloads only the exact HTTPS artifacts listed in `real-world-evaluation.manifest.json`, verifies every archive SHA-256, and extracts files as data. It does not restore packages, execute package build logic, invoke foreign MSBuild targets, clone repositories, or alter global machine configuration.

Run the complete offline-policy and real Source Link comparison with one command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Evaluation\run-real-world-evaluation.ps1
```

The default run writes:

```text
artifacts/real-world-evaluation/reports/source-link-disabled/real-world-evaluation.json
artifacts/real-world-evaluation/reports/source-link-disabled/real-world-evaluation.md
artifacts/real-world-evaluation/reports/source-link-enabled/real-world-evaluation.json
artifacts/real-world-evaluation/reports/source-link-enabled/real-world-evaluation.md
```

Use `-SourceLink enabled` or `-SourceLink disabled` to execute one policy only. The network-enabled run uses only the production P7B HTTPS client and the URLs embedded in validated Source Link provenance. The disabled run makes no Source Link request.

## Workspace and repository policy

All downloaded archives, extracted binaries, PDBs, optional manual-verification sources, and generated reports stay below `artifacts/real-world-evaluation`. The repository-level `.gitignore` excludes the complete `artifacts` directory. No third-party binary or source tree is versioned; only coordinates, hashes, relative paths, selection reasons, and concise flow notes are committed.

The harness never treats foreign package content as executable build logic. XMLDocNormalizer runtime reconstruction receives only already prepared local DLL/PDB data and explicit opt-in Source Link. It never performs NuGet restore/download, Git operations, external builds, generators, or MSBuild execution.

## Candidate matrix

| Package | Version | TFM | Evaluation role |
| --- | --- | --- | --- |
| Dapper | 2.1.35 | net7.0 | Embedded exact Portable PDB and next-boundary case |
| Newtonsoft.Json | 13.0.3 | net6.0 | Large multi-source Portable-PDB/Source-Link case |
| OneOf | 3.0.263 | netstandard2.0 | Small generic library; no published matching PDB |
| Polly | 7.2.4 | netstandard2.0 | Async/generic Portable-PDB/Source-Link case |
| Scrutor | 4.2.2 | netstandard2.0 | External-dependency and exact-reference case |
| Semver | 2.3.0 | netstandard2.0 | Unsigned complete reconstruction, embedded + Source Link, and exception probe |
| Serilog | 3.1.1 | netstandard2.0 | Interface-heavy Portable-PDB/Source-Link case |

Versions are immutable manifest entries. Testing a different release requires a new entry rather than silently changing an existing coordinate.

## Evaluation semantics

For each candidate the harness records pinned hashes, PDB/source provenance, every reached reconstruction gate, P7A discovery, P7B origin counts, P5 source/reference counts, compiler diagnostics, fallback classification, duration, and canonical finding sets. It invokes the existing exception detector in `SolutionTransitive` mode; the harness does not implement exception semantics of its own.

The Semver probe compares metadata-only analysis against registered P6A/P6B supporting source. Its manual note refers only to method/type/flow structure at the exact Source Link commit and does not reproduce third-party source text.

`Direct` and `ProjectTransitiveDeclaredExceptions` remain covered by the existing P7B demand-driven regression tests and are not made to reconstruct external bodies by this eager diagnostic harness. Performance interpretation, persistent source caching, authenticated Source Link, signed-target reconstruction, source-package discovery, generators, and new analysis modes are out of scope for E1.

## Offline unit tests

Manifest validation, workspace containment, stable ordering, JSON enum output, and Markdown generation are covered by local tests:

```powershell
dotnet test .\Tests\XMLDocNormalizerTests\XMLDocNormalizerTests.csproj `
    --filter "FullyQualifiedName~XMLDocNormalizerTests.Evaluation"
```

They use no package host, GitHub, or Source Link network access. The normal unfiltered test suite likewise remains network-independent.

## G1 standard local reference candidates

The evaluation harness opts P7A into the same context-local G1 sources that are
available to production orchestration: loaded file-backed references, installed
.NET reference packs and shared frameworks, and the NuGet global-packages
folder resolved from `NUGET_PACKAGES`, the applicable `NuGet.Config` hierarchy,
or the platform default. These locations provide candidate paths only; every
candidate still passes unchanged P5B/P5C validation.

Search is fixed-depth and expected-filename-directed. The NuGet fallback lazily
indexes only known package asset directories and never recursively enumerates
all files. JSON candidate entries record each P5A ordinal, exact provenance,
selected artifact source, and bounded discovery counters. The observed Scrutor
and Serilog root cause is documented in `G1-reference-acquisition.md`.

## G2 exact Portable PDB candidates

The evaluation harness also opts missing manifest PDB paths into the bounded,
local G2 candidate pipeline. It checks an embedded Portable PDB first, then
explicitly configured local paths, safe sibling/root probes, and explicitly
configured `.nupkg`/`.snupkg` archives. Candidate location never establishes
identity: every image must pass the unchanged P4B identity and checksum
validation before P5A metadata is consumed. Explicit manifest PDB paths remain
authoritative and never fall back after a mismatch.

G2 performs no network request, restore, build, global filesystem scan, or
persistent caching. The Dapper and OneOf artifact analysis, source order,
archive safeguards, cache boundary, and E1 outcome are documented in
`G2-portable-pdb-acquisition.md`.
