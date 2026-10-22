# A5 recursive-summary closure proof

See Lifecycle-and-contract.md for the pre-production trace, Phase 0 conflict
inventory, exact narrow authorization and the finite-closure equivalence proof.

The repair keeps evaluated state local to Evaluate. Tarjan identifies actual
callable SCCs without CLR recursion. A cached result is keyed by callable plus
the sorted set of blocked ancestors in that SAME SCC. It is inserted only after
all permitted children have finished; mutable active/provisional frames are never
read as callee results. An SCC-context result cannot escape its context as an
unrestricted summary. Component exits have no relevant blocked ancestors, so
acyclic diamond reuse is preserved. No entire recursive cache is disabled.

This computes finite cycle-erased witnesses, NOT an unrestricted fixed point of
infinitely unrolled paths. For the existing static graph/type/filter/uncertainty
contract the result has the same reachable facts as least reachability closure.
The old 64-path cap remains a deterministic representative-path/truncation
contract, not a convergence budget. It is not changed or used to stop traversal.

Termination: Tarjan costs O(V+E). Every expanded (node, ancestor-subset) context
depends on a strictly larger subset in its SCC, or a later component in the
condensation DAG. At most s*2^(s-1) contexts are possible in an SCC of size s;
the evaluation stack has at most V distinct keys. Dense SCCs may therefore be
expensive; there is no claim of polynomial all-path closure or general performance
optimization. Acyclic components have one context per node. There is no retry,
sleep, magic iteration/depth bound, global cache, compiler change or generic
expansion engine. Graph immutability/sequential session use remains the existing
precondition; no claim of new concurrent cache support.

Positive cases use real compilable source and productive summary construction:
self, mutual, three-node SCC, outgoing dependency, incoming caller, repeated
closed-context reuse, independent SCCs, and acyclic paths, both non-generic and
stable generic call graphs. Repeated evaluation proves deterministic canonical
results and no mutable-result poisoning across roots. All SCC entry/root-order
cases and missing/unbuilt/cyclic-placeholder dependency cases are explicit.
A supplemental independent filtered BFS oracle covers all 512 three-node
directed graphs, three source placements and three roots (4608 checks). This
does not replace real compiler/Current tests or eight genuine Worker cases.

There is no public component-state DTO or caller-controlled cache-state enum to
fabricate. State safety is structural: fresh invocation-local dictionaries;
active frames absent from completed cache; exact exclusion keys; completion only
after permitted dependencies; SCC IDs computed from the actual frozen graph,
not accepted from a payload. Incomplete body/summary dependencies retain existing
uncertainty, including a malformed unbuilt cyclic placeholder. Canonical transport
occurs only after evaluation returns and never transports provisional cache state.

Prepare-Validation captures inherited tracked/untracked WIP, protected files,
index and stashes and exports HEAD+WIP to a NEW disposable tree. Only that tree is
built. Two preparation commands initially failed before extraction/build (native
argument expansion, then a manual command missing git -C); the initial capture
was retained and the empty export resumed. No test or build gate was retried.
Run-Validation refuses overwriting named attempts and retains actual exits/logs.
Baseline/candidate Current tests precede genuine Historical parity. Dual performs
all existing gates and the additive A5 gate, with unchanged compiler versions.
Regressions use the existing B24 regression script without repeat/exclusion flags.

The only old test edits are the two authorized caught-first A4 expectations
(four cases). Their corrected witnesses go to A5. The uncaught-first assertions
and emitted metadata values remain unchanged; so do all other existing tests.
Ordinary outputs from unchanged old cases remain confined to the NEW export,
not historical workspace/export witness files. No historical report is rewritten.

Verify-ChangesAndHistory reconstructs exact inherited A4 test bytes in memory by
reversing only authorized changes; it similarly removes only A5 gate/register
additions and verifies every other inherited file. It independently checks all
protected state and historical B25--A4 evidence against exact retained bytes.
Verify-CompiledSources binds pre-Dual immutable source hashes to Current,
Historical and Test PDB documents and exact PE/PDB identity/checksum. Any original
CRLF/LF differences require exact CRLF-to-LF-only equality and no multiline raw/
verbatim literal. The isolated metadata-reader dependencies do not change product
or compiler references. Verify-Evidence validates actual fresh TRXs, all eleven
WAE logs, eight Current byte baselines, witnesses and image/source stability.
Failures remain failures even if a different later group passes. No green retries.

A5 removes only the proven finite recursive-cache loss. Exact generic callable
identity, substitution, typed catch equality, assembly/nested rebinding and
unbounded constructed-context expansion from earlier audits remain separate.
B25B and SolutionTransitive Finding/CLI admission remain closed. Self Analysis
and Canonical Finding Diff are not claimed fresh unless actually rerun.

Final validation is NOT all-green: Broad passes 3311/3311, Full is 3401/3402,
separate Boundary passes 596/596, and Dual/eleven WAE builds pass. Full's sole
unchanged MultiModuleAssembly_FailsClosed fixture failure occurs before factory
or summary evaluation. Trace-ForeignFullFailure reads actual executed net8
metadata fields/getter IL without running any test/emitter. The escaped
reader-backed structs/disposing scope are an existing lifetime hazard documented
in B13/B15/B20; the exact freed-memory state of this failing call is not retained.
No test repair, isolated retry or Full retry. An initial metadata-only inspection
needed its PowerShell extension-method invocation corrected; this is recorded,
not presented as a product/test fix. Overall evidence retains the real Full and
regression-process failures and must exit 1 even when evidence is consistent.
