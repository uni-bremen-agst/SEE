# A9 bounded contract/proof only

No production or existing test change, no commit/push/stash operation. Run from
the tool root. Historical A6/A7/A8 sources/audits/proofs are READ ONLY.

1. Invoke-Inherited.ps1 -Mode Prepare: verifies protected state and fingerprints
   all 1092 inherited files; makes a fresh offline artifacts/b25a9-c777 export.
2. Invoke-Inherited.ps1 -Mode Run -Phase Build, then -Phase Dual: unchanged full
   build and Dual, eleven WAE builds and the genuine existing test gates.
3. Format only new A9 files. Run-Harness.ps1 -Harness A9 builds/runs the BCL-only
   ProfileProof once. It has no product/Core/Roslyn/package/test dependency.
4. After Dual, -Mode Run -Phase Capture binds final executed images/sources.
   Run-Harness.ps1 -Harness A8 copies the frozen A8 source/project, builds against
   actual fresh Current compiler DLLs and runs its 40 checks in the NEW records.
5. -Mode Run -Phase Tests, then Broad, Full, Boundary, each once. No source edit or
   product rebuild after capture. No skipped tests or unchanged green retries.
6. Capture-Inventory.ps1: captures repository-wide old characterization search
   and actual query implementation files. Finalize report/table/owned scripts.
7. Run-Gate.ps1 -Gate A6, then A7, then A8, then A9. All actual child exits/logs
   are preserved under artifacts/p5o2b25a9/runs. A failed gate is not rerun to green.

Invoke-Inherited reads frozen verifier algorithms and retargets record/output
roots IN MEMORY. A6/A7/A8 fresh audits go to artifacts/p5o2b25a9, never to the
frozen Evaluation audit files. The fresh A8 gate still checks the frozen A8
report/preflight/proof source, plus the new unchanged-source runtime evidence.
Its initial launcher failure refers to the original A8 record/source copies;
that failure is NOT replayed or hidden. A8's legacy finite-proof-r2/proof-build-r2
names are compatibility input names, not new A9 retries.

The new proof source/project/input/image/result are fingerprinted separately.
Its finite enumeration is NOT a universal termination proof; the report proves
the height bound for the admitted finite-slot substitution grammar. The prototype
does not extract C# or implement product exception propagation, discovery, A5
closure, canonical production rebinding or Finding Admission.

The report's GO authorizes only a later explicitly scoped implementation package
after needed characterization approvals. It is not production adoption or B25B
readiness. All actual validation outcomes remain distinct from that decision.
