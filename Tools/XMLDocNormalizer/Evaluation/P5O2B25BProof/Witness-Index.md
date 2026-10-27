# B25B witnesses and preserved characterization scope

Small paths below remain in `Results/<phase>-evidence/` (and the isolated
export's `artifacts/p5o2b25b`). The large W4 JSON is a **virtual original path**:
resolve its gate through [the storage manifest](evidence-storage-manifest.json)
to one of four lossless canonical gzip payloads under `Canonical/W4/`.
Every snapshot also has its own `capture-manifest.json`; Boundary and the
isolated harness snapshot do not claim a fresh W4 execution.
See [verification and exact raw restoration](EvidenceClosure/README.md).
Numeric positive filenames encode project count, not the conceptual W-number.

| Contract | Runtime witness | Independent expectation |
|---|---|---|
| W1 | `adoption/W2-chain.json` | two projects, one DOC611 at P0, P1 explicit throw path |
| W2 | `adoption/W3-chain.json` | three projects, one DOC611 at P0, complete P2 throw provenance |
| W3 | `adoption/W2-same-name-types.json`, `b25/ownership-*` | exactly P0/P1 canonical owners and two DOC611, full-identity P1 selection, duplicates preserved |
| W4 same graph / different run | `adoption/W4-original-stages.json` | other-operation Direct/Summary, clones, raw revalidation and replay rejected; original pair accepted; second invocation of the same prepared Direct request has no operation ticket |
| W4 different genuine graphs / equal results | virtual `adoption/W4-equal-results-different-graphs.json` → gate-specific `capture-manifest.json` → canonical gzip / raw SHA-256 | same target/source/native result, two versus three original projects; cross-pair rejected, both original pairs accepted; distinct PID/Execution IDs retained |
| W4 post-registration mutation | `W4_MutatedGraphAfterRegistrationCannotMintReceipt` TRX | snapshot mutation rejects Direct admission before Summary/policy |
| W5 A9 OUTSIDE | `adoption/W5-growing.json`, `W5-uncovered-call.json` | actual UnsupportedContext with A9 certificate, no Summary result or Findings; native Current diagnostics stay separate |
| W5 narrower Finding profile | `adoption/W5-generic-final.json` | real A9-exact complete flow but unsupported generic final type; no Findings, not falsely relabelled A9 OUTSIDE |
| W6 real execution / isolation | all positive stage records | actual Worker image/Analyzer/pinned Roslyn identities, owned process exit, no Current fallback or Historical assembly in Main |
| W6 physical replay | `adoption/W6-wrong-physical-worker.json` | replaying genuine JSON from a different executable cannot acquire Summary receipt / complete the pair |
| W6 actual output adoption | six `adoption/W6-cli-<2/3>-<console/json/sarif>.json` | exit1, exactly one DOC611/RunResult count, P0 report path; no supporting reporting root |
| Independent Direct + Summary | `adoption/direct-summary.json` | nonempty Direct InvalidOperationException plus distinct supporting ArgumentException, native and Historical two Findings |
| Finite method generic / exact catch | `adoption/generic-catch.json` | actual cross-project `Invoke<string>`, substituted generic Fault caught, nongeneric surviving exception only |
| Finite containing generic / exact catch | `adoption/containing.json` | actual cross-project `Api<string>.Hit`, exact constructed catch, nongeneric surviving exception only |
| Same-type multiple paths | `adoption/W3-multiple.json`, existing C1 multiple/supporting-sources | same canonical type once, preserving supporting throw paths; owner-distinct names not folded |

C1 is exactly the 15 existing parameter cases listed in the approved batch and
the P0/P1 Fact. Their first rejection-only helper half is now the bounded
positive operation with full sorted native Finding multiset equality. The
existing wrong-mode second helper half remains unchanged and negative.
No other B25/B24/A-series assertion is changed. C2/C3 replace only the two
approved source guards each with genuine two-positive/four-negative execution.

The original preflight, historical B25/A reports/audits/witnesses and old Full
failure remain unchanged. Development red runs are retained separately; final
runtime gates are not obtained by retry logic, skips or loosened assertions.
