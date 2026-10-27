# P5O2B25B evidence closure (storage only)

No new product gate, test, analyzer, routing, compiler version, Worker profile
or test assertion. B25B's recorded functional results remain immutable. No
Post-B25B package is planned here.

## What is stored

[`../evidence-storage-manifest.json`](../evidence-storage-manifest.json) maps
all six original W4 paths and their **capture gate and actual producer** to
four content-addressed `../Canonical/W4/*.json.gz` files. Boundary reuses
Broad's exact payload; the isolated harness snapshot reuses Full's exact
payload. Other executions keep their distinct bytes, PIDs and execution IDs.
Each phase retains `Results/<phase>-evidence/capture-manifest.json`.

All original TRX/logs, including the red3448/3449 Full, remain byte-identical.
All other small witnesses remain at their original paths. No historical
B25/A1–A10 evidence is changed. Original input inventory and complete reference
analysis: [`start.json`](start.json), [`Reference-Analysis.md`](Reference-Analysis.md).

## Format / hash / reproducibility

- Each canonical file is one ordinary **RFC1952 gzip member**, containing the
  exact original UTF-8 JSON bytes. No JSON parse/reserialization/normalization.
- Header hex `1F8B08000000000002FF`: DEFLATE, flags0, MTIME0, XFL2, OS255;
  no filename, comment, permissions, current time or ZIP directory metadata.
- `ArchiveCodec.cs` writes raw RFC1951 `DeflateStream(Optimal)` plus explicit
  CRC32/ISIZE trailer. The runtime/encoder assembly and file version are in
  each manifest blob. The same encoder was executed twice per input and
  produced identical archive SHA-256 values.
- Deflate implementations can differ across CLR/platform versions. The
  **uncompressed SHA-256 and exact byte length are normative** for long-term
  reproduction; retained archive SHA-256 protects the immutable stored bytes.
  The documented encoder reproduces the current archive hashes; other valid
  gzip encoders may produce different compressed bytes for identical JSON.
- Verification checks compressed SHA-256, canonical header, full decompressed
  SHA-256/size and CRC32/ISIZE, without writing a90MiB temporary JSON.
- This uses built-in .NET compression, no ZIP timestamps or Git LFS service.
  The four files are each about35.82MiB and normal Git-visible binary blobs.

## Read-only verification (no product gates)

From `Tools/XMLDocNormalizer`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25BProof/EvidenceClosure/Verify-EvidenceClosure.ps1
```

Checks the six gate bindings, four archives, unchanged red/green records,
all protected source/test/build/historical bytes, index/root ignore/stashes/
Core entry bytes, report links, the reversible storage-only capture diff,
and the package seal if present. Verification does not rewrite a result or
invoke dotnet/test/build. A fresh checkout may use `-PortablePackageOnly`
to check archived/package evidence without claiming this machine's old
dirty-worktree/Core/index state (see verification scope output).

## Rehydrate any exact original W4 JSON

Create a fresh scratch directory, then select one of `broad`, `boundary`,
`full`, `harness-isolated`, `broad-harness`, `full-harness`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25BProof/EvidenceClosure/Expand-W4Evidence.ps1 -Gate full-harness -OutputPath C:/Temp/B25B-W4-full-harness.json
```

The parent output directory must exist and the file must not. The helper
checks archive and raw hashes, refuses overwrite and refuses writing inside
the sealed package. The restored filename can be placed at the original
virtual path in a separate export when a consumer needs raw JSON. Existing
`Verify-Gates.ps1` checks the **live export's** raw output, not the removed
repository copy, and has not been modified. No raw JSON is permanently needed
inside this repository package.

Storage-only selfcheck: [`storage-selfcheck.json`](storage-selfcheck.json)
proves restoration, two captured contexts sharing exactly one blob, no bulk
raw copies, and rejection of an incorrect raw digest. It is explicitly not
a fake product gate, compiler run or test facade.

## Future capture / immutability

`Run-Validation.ps1`'s arguments, filters, counters, acceptance predicates,
exit logic and test commands are unchanged. Only bulk directory storage is
replaced with `Save-B25BEvidence` from `EvidenceStorage.psm1`, after the final
run record is written. W4 is compressed once per distinct raw SHA; another
snapshot of identical bytes just points to that blob. All small evidence is
still captured. Manifests distinguish an artifact snapshot from fresh witness
execution; presence alone is never declared fresh production evidence.

Tests intentionally continue producing their original live raw JSON. This
closure changes capture/storage, not tests. Independently executed W4 payloads
with different execution identities are NOT collapsed to one normalized body.
Reversing only this storage transformation recovers the exact original gate
runner SHA `03DB6550B7691E80FFA2CD468200BFA5C805D3D9A409D3FCB89EAFE056296E3D`.

The final package is inventory/hash sealed. Capture refuses a sealed package;
future legitimate executions require a separately named package/revision,
not overwriting this completed evidence. Content-addressed blobs and capture
manifests are never overwritten. Repeat verification is read-only.

## Protection and runtime status

The two Core-obj commit-metadata differences already existed at closure entry.
Old/new AssemblyInfo/cache hashes were reproduced exactly; writer unknown.
They are not restored, repaired or adopted into a replacement historical Core
baseline. Entry-byte checks mean **unchanged by this closure**, not equality
with24October's original manifest. Its historical checks/Exit1 evidence remain.

**Runtime stability investigation: OPEN – recurrent, unattributed process failure.**
This updated user-provided status supersedes earlier "not observed in these
gates" observations for investigation status, without rewriting them. Not a
B25B semantic error, not labelled flaky, not resolved. No additional product
test run is started to investigate it and no LocalDumps/registry setup is done
by this closure.
