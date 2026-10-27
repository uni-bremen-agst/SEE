# W4 reference/producer audit before storage changes

Audit entry: HEAD `1ef19a37bbecc2ba1f9f2b9e2be27f7cc092c65e`, parent
`a2fae5fc29f682461b38385990568fc5da494ae7`, branch `fix/documentation`,
upstream `origin/fix/documentation`. Empty staged index; all B25B changes still
uncommitted. Actual initial inventory: **439 files, 620,258,962 bytes / 591.525 MiB**
(the user's earlier438-file count is not substituted for the measured439-file entry).
All original file hashes are in `start.json`.

## All six original paths

Prefix: `Results/`; suffix: `-evidence/adoption/W4-equal-results-different-graphs.json`.
Each row is retained in the canonical catalog and in its gate capture manifest.

| Capture gate | Actual W4 producer | Raw bytes | Raw SHA-256 / duplicate group | Storage decision |
|---|---|---:|---|---|
| broad | broad | 94643221 | `C52E0C241DB44C11E007A3ADC81063E9B8CD7710289EBC2171CB9798CEC83C38` | canonical gzip C52E… |
| boundary | broad; captured old artifact, NOT a new W4 run | 94643221 | `C52E0C241DB44C11E007A3ADC81063E9B8CD7710289EBC2171CB9798CEC83C38` | same C52E… archive, separate gate context |
| full | full, including its preserved unrelated red outcome | 94643225 | `3B628C50E9FFA20BD3A30C2CBB1AE15AB52CF9504703AB022290DFB48DCE13D7` | canonical gzip 3B62… |
| harness-isolated | full; captured old artifact, NOT a new W4 run | 94643225 | `3B628C50E9FFA20BD3A30C2CBB1AE15AB52CF9504703AB022290DFB48DCE13D7` | same 3B62… archive, separate gate context |
| broad-harness | broad-harness | 94643225 | `015400A51BCCF584E994652A08CCE89F03C7BE4C08D382F29966E571C60000A1` | separate canonical gzip 0154… |
| full-harness | full-harness | 94643223 | `0223BE1CF879303D8B3EBDC585F6EB98229A1A372D3F206CD85EA92119A2A0EB` | separate canonical gzip 0223… |

Role of every row: genuine two/three-project graphs, equal correct canonical
results, rejection of crossed receipts, successful original pairs and owned
process exits. Four calls (`ad`, `summaryTwo`, `bd`, `summaryThree`) carry their
actual identities/provenance/PIDs and execution-ID-bound lifetime evidence.

## Semantic distinctions: preserve, do not normalize away

All four distinct contents have the same graph strings and flow/identity/
provenance facts. Removing **only** each call's `processId` and its
`processExit.processId` / `processExit.executionId` in a read-only comparison
produced the same comparison digest
`BB51399F4D208879697D144F1BDD596F8E114C5056A9554FCD37D5212E2337AA`.
That comparison is NOT a replacement witness or semantic deduplication rule.
Actual PID/Execution IDs matter to the genuine execution evidence and remain
byte-for-byte in four separate payloads. Only the two exact SHA duplicates
represent physical duplication with no additional payload information.

## Producer/capture cause

The unchanged new B25B test
`ExceptionFlowHistoricalSolutionAdoptionTests.W4_EqualResultsFromDifferentGenuineGraphsCannotBeCrossAdopted`
(lines159–175) writes the raw JSON using its Evidence helper (lines312–313).
It emits two full graph snapshots including PE images; this explains the90MiB.
No production or test source changes are needed or performed in this closure.

Old `Run-Validation.ps1` indiscriminately copied every file in the `adoption`
and `b25` runtime directories after each non-Dual phase. It copied W4 even if
that phase did not execute the W4 test. Hence Boundary copied Broad's bytes,
and the isolated source-reconstruction test copied Full's bytes. Gate TRX and
run records prove these associations; storage verification checks them.

## Consumers, exact paths and stored hashes

- Completion report: the general `<phase>-evidence` snapshot description;
  no pre-existing raw SHA or direct link to one large physical JSON.
- `Witness-Index.md`: the virtual relative W4 witness path, now documented as
  gate → manifest → compressed canonical payload. Other witness paths unchanged.
- `Run-Validation.ps1`: generic directory-copy convention was the storage
  consumer. Only its storage block is replaced. Arguments, filters, counters,
  gate acceptance, exit checks and product commands are unchanged.
- `Verify-Gates.ps1`: expects `W4-equal-results-different-graphs.json` in the
  **live validation export's runtime output**, not one of these repository
  snapshots. Unchanged tests still create that live raw file. The existing gate
  is not modified or rerun. Its historical result `Results/b25b-gates.json`
  contains a successful name/existence check, not a raw-file checksum.
- `Finalize-Evidence.ps1`, implementation audit, original manifests and logs:
  preserve the gate records/results and their hashes, not an exact large-copy
  dependency. No stored old raw SHA reference needed rewriting. Raw/archive
  hashes are now explicitly added to the new catalog.
- Repository text/reference search, excluding the raw embedded PE payloads,
  found no further physical-copy consumer. All439 original file hashes are
  frozen so unexpected edits cannot be hidden by this conclusion.
- Reproduction may require raw JSON as an **output**, not permanently as a
  repository input. `Expand-W4Evidence.ps1` rehydrates any of the six exact
  original gate payloads to a fresh explicit destination, with full SHA checks.
  The storage-only selfcheck proves this without starting tests or dotnet.

Thus no STOP condition applies: information, execution distinctions, original
path context, producer attribution and exact raw bytes remain recoverable.

## TRX/log decision

Keep **all** existing TRX/logs, not just the green finals. In particular keep
`Results/full.trx` and its log/record (3448/3449), `full-harness`, both Broad
records, Targeted, Boundary, isolated harness, all nine Dual TRX and development
failure evidence. Their original hashes are verified unchanged. No speculative
pruning is part of this closure.

## Pre-existing protection observation

735 historical Evaluation files and all production/test/build source bytes
matched the completed B25B state at entry. Index/root-ignore/stashes matched.
21/23 Core bytes matched the24October protection data. Two generated Core-obj
files were already changed at entry (27October08:52:41 UTC): AssemblyInfo now
contains HEAD1ef19… rather than a2fae5…, and its generated inputs cache matches.
Both old/new AssemblyInfo SHA and MSBuild attribute-cache hashes were exactly
reproduced read-only (see `start.json.MetadataReconstructions`). Writer unknown.
No Core file, historical manifest, old Exit1 evidence or old763-check result is
restored/rewritten/rebased. Closure checks23 exact **entry** bytes for no further
mutation and explicitly reports the inherited two-file discrepancy; it does not
falsely assert that today's files still match24October's historical manifest.
