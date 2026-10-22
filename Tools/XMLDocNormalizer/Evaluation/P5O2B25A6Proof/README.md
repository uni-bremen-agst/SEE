# A6 boundary proof and fresh validation

No production implementation and no existing test changes. Preserve the A5 WIP.

Final source-correction revision uses `artifacts/p5o2b25a6-r2` and the fresh export
`artifacts/b25a6-c777-r2/Tools/XMLDocNormalizer`. The original A6 attempt is retained
under the same names without `-r2`: initial new-witness serialization failed 2/8
after its semantic assertions. Only new witness observation JSON now uses flat,
complete canonical keys; no assertions/depth/production code changed. Do not
delete or rerun the old attempt; the final audit must retain it explicitly.

From the authoritative tool root, in order:

1. Prepare-Validation.ps1: captures inherited bytes/protection and creates a NEW
   HEAD archive plus WIP overlay, with isolated local package caches. Refuses an
   existing A6 records/export directory. Never remove old records to obtain green.
2. Run-Validation.ps1 -Phase Build: new witness compilation, WAE.
3. Run-Validation.ps1 -Phase Dual: unchanged full Dual script, all eleven WAE builds.
4. Format-Owned.ps1: UTF8/no-BOM/CRLF for ONLY new A6/A7 files. If the export's
   earlier compiled test bytes were LF, preserve them and verify the exact
   line-ending-only difference, rather than rebuilding to conceal provenance.
5. Run-Validation.ps1 -Phase Capture: captures final five executed images and
   exact workspace/export checksums of 121 shared sources and the new test.
6. Run-Validation.ps1 -Phase Tests: witnesses and combined suite.
7. Run-Validation.ps1 -Phase Broad, then -Phase Full: inherited unchanged Broad
   selection plus the new A6 class; Full unfiltered. Older individual regression
   groups are covered in these required runs, not additionally repeated.
8. Run-Validation.ps1 -Phase Boundary: independent unchanged Boundary filter.
9. Run-EvidenceGate.ps1: protection/history, compiled-source/PDB bindings, static
   checks, all fresh test/build evidence, eight Current baseline byte comparisons.
   A real failing validation remains exit 1 even when evidence is consistent.

Recorded child processes retain stdout/stderr, start/finish, arguments and actual
exit status. A named attempt cannot be overwritten. Independent required phases
may continue after a failure, but the failed attempt remains part of the gate.

Verify-Protection.ps1 reads frozen historical evidence; it never invokes old
write-producing evidence scripts. Verify-CompiledSources.ps1 executes the frozen
A5 metadata-only algorithm with just its package/output paths redirected in
memory to A6. It does not load a Roslyn/Analyzer image, compile or change a source.

All actual test artifacts, including ordinary old-suite outputs, stay inside
the fresh export. Their historical counterparts in older exports/workspace are
not overwritten. Static analytical A7 files are not executed product behavior.
