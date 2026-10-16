# B25A stop-rule evidence

This is a boundary audit, not a shipped ownership repair. Production and all
existing B25 tests/report/audit/proof remain unchanged. No commit, push or stash
operation is part of these scripts. Run builds only in a fresh disposable Git
export; never in the original protected Core inventory.

## Recorded execution

1. Capture protection with the unchanged B23 helper into
   `artifacts/p5o2b25a/protected-before.json`.
2. `Prepare-Validation.ps1` creates a new HEAD export, using unchanged B24 offline
   cache configuration. No package download/network fallback.
3. Run the unmodified exported `build/Verify-DualVersionBuild.ps1` with
   `-RunMainBoundaryTests -BuildTestProjectReferences`; `Run-Validation.ps1
   -Phase BaselineClose` archives its audit, eight Current baselines and B25
   witnesses before any overlay.
4. The now-withdrawn candidate and its two authorized B25 characterization
   changes are retained as `withdrawn-candidate.patch.txt`; its 25 tests as
   `candidate-tests.cs.txt` (intentionally not compiled in the final tree).
   The actual first build and 49/52 run, failed isolated nested test and binary
   input hashes are preserved under `artifacts/p5o2b25a/candidate/` by
   `Preserve-Candidate.ps1`. That script refuses overwrites.
5. Two graph comparisons initially used an old copied probe dependency. A fresh
   probe build and two genuine tests established correct candidate Current/
   Worker/Graph B/C owners, without changing any test/production source.
   `Close-CandidateDiagnostics.ps1` records both DLL hashes and archives this
   diagnostic. The independent failing nested-catch proof remains retained.
6. Own candidate edits were withdrawn with a targeted patch. The final
   `Run-Validation.ps1 -Phase Build -Attempt boundary-audit -WithdrawCandidate`
   restores only those two files **inside the export**, overlays final audit
   tests, clean-builds Main/Historical, and builds Tests, Worker and the Graph
   probe. It checks that authoritative workspace files already equal HEAD.
7. Capture final compiled inputs with the unchanged B24 capture helper.
   Execute `New`, `Regressions`, `Boundary`, `Dual` phases (attempt
   `boundary-audit`). The regression runner remains unchanged; B25, PID-G1 and
   B25A are additive filters for productive/focused/broad runs. There is no
   automatic retry, exclusion or sleep workaround.
8. Format owned files with the unchanged B24 helper, then run
   `Write-Verify-Evidence.ps1`. It checks actual TRX counts/no skips/freshness,
   all eleven WAE logs, exact source/images, eight byte-equal baselines, all
   production and old tests unchanged, candidate failure history, repository
   protection, JSON/PS/UTF8/CRLF/whitespace and Git case collisions.

`Passed=true` in the generated audit means **the boundary audit and final
validation passed**, never that ownership was fixed. `OwnershipFixed=false`,
`ReadyForB25B=false` and `SolutionFindingAdmitted=false` are mandatory.
If any required test/run fails, the verifier retains its actual counts and
diagnostics, writes `Passed=false`/`ValidationPassed=false`, and exits 1 after
checking the remaining evidence. It does not waive a known failure or label a
different green group as its replacement.

## Evidence ownership

Only the candidate's first authorized characterization changed the generated
`ownership-stop-witness.json`. The second method generated nothing. The archived
candidate witness is kept separately from the old B25 witness and the final B25
executions. Historical committed B25 evidence is never regenerated in place.
The new `ownership-before-after.json` labels the corrected after-state as
**candidate only, withdrawn**. Three final audit witnesses independently capture
the original name-only substitution, nested source/PE rebind gap and generic
catch construction erasure. Green characterizations are not correctness claims.

Absolute artifact locations/hashes reflect this actual Windows/offline run.
The case audit is a static Linux-compatible Git inventory check, not a Linux
execution. Self Analysis and Canonical Finding Diff are inherited, not rerun.
