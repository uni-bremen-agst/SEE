# B25A2 boundary-audit proof

No production candidate is applied. New tests characterize the separate loss
of generic call-site construction in summary keys/contexts before type rebinding.
They must not be interpreted as successful repair or Finding admission.

`Capture-Start.ps1` captures the authoritative B25A WIP once. `Prepare-Validation.ps1`
exports HEAD plus that exact WIP, using existing offline caches. `Run-Validation.ps1`
records non-overwriting attempts: baseline close, initial compiler observations,
final build, new tests, regressions, Boundary and full Dual. Every original build
runs only in the disposable export, preserving actual Core bin/obj files.

The initial compiler-model source was subsequently strengthened with explicit
boundary assertions and genuine Worker witnesses, then formatted and frozen for
the final build. This is not a retry of a failed gate. Source/image fingerprints
bind final tests to final inputs; earlier observations retain their own TRX/log.

`Verify-HistoricalEvidence.ps1` reads frozen B25/B25A evidence without invoking
their mutating writers. It checks inherited gate/register fingerprints against
the captured WIP and preserves the old red Broad result. `Write-Verify-Evidence.ps1`
creates only the new A2 audit and validates protection, exact tests, builds,
Current baselines, binary consistency and overlapping WIP deltas.

During verifier development, two read-only invocations stopped on historical
gate/register fingerprint mismatches: the first compared B25's older gate hash
against B25A WIP; the next found that B25's final register postdated its own
build export. The completed verifier checks the appropriate exact historical
export, captured B25A WIP, or committed B25 blob with its recorded UTF8/CRLF bytes.
All 221 recorded fingerprints then pass. These were verifier provenance-mapping
errors, not modified historical evidence or failed analyzer tests; no historical
file was rewritten to make them pass.

The first completed A2 verifier wrote Passed=true, but an outer PowerShell
all-stream redirection returned 1 on Git's existing root-ignore line-ending
warning. Its audit/log are preserved in artifacts/p5o2b25a2/first-completed-*.
A direct Process invocation independently records the unchanged verifier's
actual exit code 0 and retains the warning in separate stderr. `Run-EvidenceGate.ps1`
uses that capture mechanism and binds its process result to the audit SHA256.
No warning, test, error check or protected ignore file is suppressed or changed.

`Format-Owned.ps1` touches only A2-owned files. It never formats old B25A evidence.
No test exclusions, automatic retries, timeout changes, Acquisition/PID repairs,
index/stash operations, commits or pushes are part of these scripts.
