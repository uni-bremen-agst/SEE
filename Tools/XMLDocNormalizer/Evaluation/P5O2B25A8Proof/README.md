# A8 contract/proof only

No product or existing test edit, no commit/push/stash operation. Run from tool root.

1. Invoke-Inherited.ps1 -Mode Prepare: fresh artifacts/b25a8-c777 export plus
   inherited WIP; captures ALL A6/A7 files, not just HEAD; protected state checked.
2. Invoke-Inherited.ps1 -Mode Run -Phase Build, then -Phase Dual: unchanged full
   production/test build and Dual, eleven WAE steps and existing test gates.
3. Format only NEW A8 files; run Run-Harness.ps1 separately. No product project
   reference, no package/version change. Actual Current DLLs from fresh export
   are the only compiler references. Builds/run records and input/output hashes
   are under artifacts/p5o2b25a8. No unbounded analysis is ever invoked.
4. Invoke-Inherited.ps1 -Mode Run -Phase Capture: immutable final executed-image
   and source capture. Then -Phase Tests, -Phase Broad, -Phase Full, -Phase Boundary,
   each once; no source changes or rebuilds between them.
5. Capture-Inventory.ps1: fresh repository-wide old characterization search.
6. Finalize/format the A8 report, proof table, scripts and JSON placeholder.
7. Run-Gate.ps1 -Gate A6; then -Gate A7; then -Gate A8. A failed required test gate
   remains failed, including if a known historical fixture recurs; execute the
   other required phases but do not repeat to green.

Fresh runs/records are in artifacts/p5o2b25a8. Ordinary inherited test JSON/TRX
outputs remain inside the NEW export, so old B25--A7 witnesses are not overwritten.
Invoke-Inherited reads the frozen algorithms and redirects only paths, record
roots and the A8 owned-file exclusion/formatting scope IN MEMORY. It never edits
the historical scripts. A6's audit is written to artifacts/p5o2b25a8/a6-regression-
audit.json; A7's audit to a7-consistency-audit.json, not their frozen Evaluation
files. Their Package/Decision semantics are retained and clearly labeled as fresh
verification of historical algorithms. A8's report/audit are independent.

The initial A8 proof launcher failed before compilation because native powershell
-File marshaled a string array as one comma-joined argument. Retain runs/proof-
build.json/log and finite-proof/ source copy. The corrected typed script call
uses a fresh finite-proof-r2 directory and proof-build-r2 record; its C# source,
assertions and project are unchanged. The successful 40 checks are a finite
illustration, NOT an algorithm solving PCP or an automatic proof of undecidability.

The A8 gate binds report/preflight/owned scripts, all fresh required gate exits,
proof input DLL/source/project/output, original A6/A7 gate/audit fingerprints and
all inherited files. A Decision=STOP can have green validation/static consistency;
that never means production implementation GO or Finding Admission.
