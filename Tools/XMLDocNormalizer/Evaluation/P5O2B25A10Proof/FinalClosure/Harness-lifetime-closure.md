# A10: approved MultiModule harness lifetime closure

## Outcome

The isolated `MultiModuleAssembly_FailsClosed` passes 1/1. Exactly one new Full
execution after the repair passes **3429/3429**, zero failed/skipped tests.
The A10-owned A6/A8/A9/A10 closure adapters pass with exit 0; A7 static contract
consistency also passes with exit 0. No historical gate script/audit was edited.
The earlier Full **3428/3429** and all its failure evidence remain unchanged.

Authoritative new capture: [lifetime-closure.json](lifetime-closure.json).
New logs, TRXs, source/input bindings, gate results and A6 witnesses are retained
under [LifetimeResults](LifetimeResults). The old
[independent-validation.json](independent-validation.json) and
[README.md](README.md) remain unchanged historical snapshots, not current status.

## Exact source change and resource ownership

Only the body of `ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed`
gains a local, static `IEnumerable<AssemblyFile> ReadAssemblyFiles(byte[])`
iterator. It opens the original bytes with a `MemoryStream` and `PEReader`, and
keeps both alive while yielding reader-backed `AssemblyFile` rows to the existing
assertion. Iterator disposal releases both resources after consumption, including
the predicate's early-success path. There is no eager materialization or fixture
copy, fabricated metadata, exception suppression, retry or skip.

Both existing assertions are **verbatim unchanged**:

```csharp
Assert.Contains(ReadAssemblyFiles(image), file => file.ContainsMetadata);
Assert.False(ExternalMetadataReferenceFactory.TryCreate(material, out _));
```

The fixture emitter, original PE bytes, expected reference, validated material
and Factory invocation are unchanged. The shared class-level helper and its
other caller `ResourceOnlyAssemblyFile_DoesNotTriggerMultiModuleRejection` are
not changed by this narrowly authorized repair. That caller's pre-existing
ownership hazard remains outside this repair; no general harness repair is claimed.

Original failing path:

```text
EmitMultiModuleAssembly
-> class helper creates stream/PEReader
-> returns reader-backed rows, disposes reader/stream
-> assertion dereferences ContainsMetadata after disposal
-> assertion fails before the Factory invocation
```

Corrected test path:

```text
EmitMultiModuleAssembly
-> local iterator owns stream/PEReader throughout assertion enumeration
-> assertion reads ContainsMetadata while backing image is alive
-> enumeration disposes resources
-> Factory.TryCreate independently reads validated immutable image
   -> ContainsAdditionalManagedModule owns its own PEReader throughout access
   -> false (unchanged fail-closed contract)
```

Production does not borrow the fixture reader. Its resource access is already
inside its own `using PEReader` scope. No production lifetime violation was
found; no production/Factory/failure/Analyzer/routing/version change was made.
The original violation is proved by ownership scope; exact freed-memory reuse
in the earlier failing invocation is still not claimed to have been observed.

## Validation and explicitly retained evidence

| Check | Actual result | Execution in this continuation |
|---|---|---|
| Main/test, Historical Worker, PDB fixture, peer builds | 0 warnings / 0 errors each | Fresh short-path export |
| Genuine Historical PDB emitter | Exit 0 | Fresh |
| MultiModule isolated | 1/1, no skips | Once, before Full |
| Full | 3429/3429, no skips | Exactly once after repair |
| A10 / A6 / A5 within Full | 19/19; 8/8; 33/33 | Fresh Full |
| Metadata-reference Factory class within Full | 18/18 | Fresh Full |
| Compiled source bindings | 3 PE/PDB pairs; 254 documents; 0 differences | Fresh, includes repaired harness |
| A6 / A8 / A9 / A10 evidence adapters | All exit 0 | Fresh against new Full |
| A7 contract consistency | Exit 0 | Fresh static confirmation |
| Broad / Boundary | 3338/3338; 596/596 | Export17 evidence reused, not rerun |
| Dual-Version | 800/800 in nine suites | Export17 evidence reused, not rerun |
| WAE / Current baselines | 11 clean steps / 8 byte-equal baselines | Export17 evidence reused, not rerun |
| A8 / A9 independent proofs | 40/40; 37/37 | Export17 evidence reused, not rerun |
| Additional actual A10 `Session.Analyze` queries | 30/30 | Export17 evidence reused, not rerun |

`lifetime-input-delta.json` binds 760 src/Tests/build inputs to export17 and the
actual executed short-path export: exactly one changed file, and its only change
is the local iterator insertion. Both assertions and all fixture generation are
unchanged; all other inputs are byte-identical. The adapters recheck these
bindings, old proof source/image/result hashes, reused runtime records, existing
WAE logs, baseline bytes and frozen protections. The repaired harness gets new
test-image/PDB source bindings; old Full is not reused as a green prerequisite.
No independent unchanged suite is represented as a fresh execution.

A7's general parametric-scope STOP and F1-F14 obligations are unchanged.
Green A10-owned adapters do not rewrite historical audits or prove support
outside `A9-FiniteOrbit-v1`. Self Analysis and Canonical Finding Diff were not
freshly run and are not claimed as new evidence here. B25B remains closed.

## Environment-only preparation failures, all retained

The first preparation of `artifacts/b25a10-development-18` failed while copying
because D: had approximately 16 MB free. No build or test ran there; the partial
export was preserved. C: had sufficient space, so validation moved to a fresh
temporary export. Its initial long root produced a 265-character Historical
AssemblyReference cache path and MSB3101 during fixture build, after clean
Main/test and Worker builds. No test ran in that export either; it and its logs
were retained without overwrite. These are preparation/build failures, not green
test retries. A fresh shorter root fixed the path constraint without changing
source, project configuration or suppressing a build error:

`C:\Users\Krause\AppData\Local\Temp\a10-277b50ca`

The successful export uses only the existing offline validation feed adaptation;
the real repository's NuGet configuration and Roslyn versions are unchanged.

## Protection and remaining independent observations

Core: all 23 protected files retain the accepted frozen/S1 bytes; no new drift.
All 332 frozen historical Evaluation files retain their hashes. Root `.gitignore`,
physical index hash, clean semantic index and all six stashes are unchanged.
The historical S1 protection exit 1 is retained, not rebased/restored/repaired.
Old Full, native-crash records and prior gate results remain historical evidence.

The previous native CLI exit `0xC0000005` remains **unattributed**. It has not
recurred in this continuation; it is not labelled flakiness and is not presented
as repaired by this lifetime fix or by the subsequent green run.

No commit, push, stash/index operation or B25B activation.
