# A10 Broad closure continuation: STOP at the newly reached Await-details contract

This supersedes the earlier pending BRC1-BRC4 status for the current WIP, not
the historical root-cause audit. BRC1-BRC4 are now explicitly approved/applied.
HEAD remains `9b76e179a568c64ae972cc3a45980a094e44245e`. No commit/push/stash
operation, baseline replacement or B25B activation.

## Actual validation

Fresh isolated export: `artifacts/b25a10-development-16`; original Core outputs
are not build targets. Test/Main, Historical Worker, Historical PDB emitter
and BoundaryTestProcess builds all pass with zero warnings/errors. The real
Historical emitter was EXECUTED, not merely built; no copied/fake PDB or peer.

| Run | Passed | Failed | Skipped | Total |
|---|---:|---:|---:|---:|
| Targeted closure tests | 57 | 1 | 0 | 58 |
| Complete planned Broad | 3337 | 1 | 0 | 3338 |

Broad subgroups, not separate repeated gates: new A10 19/19; A6 8/8;
A5 33/33; all eight previously combined B25/A test classes 153/153;
unchanged summary orchestration architecture guards 4/4. BRC1/BRC2,
renamed BRC3 and BRC4 worker-error pass. The original raw supporting-source
reduced-extension test and the malformed-input permanent-boundary test pass.

Broad is NOT green. No Full, Boundary, Dual-Version, eleven WAE steps, eight
Current baseline checks, A6/A7/A8/A9 gates, final A10 evidence gate, Self Analysis
or Canonical Finding Diff was started after this STOP. Their earlier results
are not fresh closure evidence. No green retry or skipped test.

## Implemented production corrections

1. **Architecture, two original failures:** discovery/body execution stays in
   `ExceptionFlowFiniteOrbitAdmission` at Session level. The graph receives a
   copied `ExceptionFlowFiniteOrbitSeal` containing declaration arities,
   transition identities, assembly identities and immutable compilation handles.
   It has no discovery/Analyzer callback, lazy supplier or discovery-owned result
   type. The live environment is used only for the existing scope-set check.
   Independent result diagnostics moved to neutral `ExceptionFlowProfileDiagnostics`.
   All four existing signature/storage/IL/delegate architecture checks pass
   unchanged. The existing shared-source manifest remains125; no new entry.
2. **Rebinding:** `ExceptionFlowInvocationSubstitution.ApplyCallable` first
   recovers the static extension declaration and all receiver-inferred/explicit
   generic coordinates from a reduced handle, then requires a UNIQUE full
   canonical declaration match on the constructed containing type. No name-only
   or first-symbol fallback. Existing receiver/value facts remain untouched.
   New reduced/static controls check full canonical equality, explicit Roslyn
   equality and concrete generic arguments. The original Broad scheduling/world
   trigger remains UNPROVED; this is not a flakiness or complete-reproduction claim.
3. **Incomplete syntax:** `Discover` checks root syntax diagnostics and an empty
   recovery-symbol name BEFORE `AddDefinition`/canonical identity construction.
   Invalid input receives `RepresentationOutsideGrammar` with finite raw
   path/span/kind evidence, no fabricated callable or exact-empty success.

P1 remains in `Session.Analyze` before root registration. P2 still checks every
production context/transition before `GetOrAdd` and enqueue. Scope change rejects
registration; failed construction rolls back newly added nodes. A5 cache/SCC
behavior remains green. Session still obtains fresh discovery directly; neither
raw graphs nor caller-supplied certificates enter its private production graph.
The existing raw diagnostic entry is unchanged and is not a production fallback.
Current/Historical use the same shared source. No new Wire DTO, compiler version,
routing, attestation or R2 failure-identity change.

## Applied characterization scope

`approved-assertion-delta.json` records every existing R7/R8/R9/R10 row with
original assertions, case restrictions and the applied contract:91 coverage
cases and14 narrowly named expected Worker codes. The14th code belongs to the
previously PDB-blocked async Dispatch case; the original partition had only13
reached code failures. The two manifest counters were already changed before
this continuation and are not a new change here.

Only named callers use outside variants of shared fixture/assertion helpers;
the original helpers and all their other callers retain their prior assertions.
Direct/recursive controls are retained, including inside both-mode witnesses.
Source programs are not repaired to force admission. New coverage assertions
require the actual productive typed certificate as well as absence of normal
exact DOC610/DOC611/DOC632 output, not merely an empty list.

- **BRC1:** only IOException solutionRun normal path expectations become
  coverage-outside plus separate full canonical diagnostic identity. Proven-path
  emptiness remains; original projectRun/helper unchanged.
- **BRC2:** only the two named solutionRun contracts change; both distinct full
  canonical identities remain separately diagnostic, unique and order-independent.
  No preserved normal path/location is invented.
- **BRC3:** only the final empty-list contract becomes exactly one DOC631 for the
  documented ArgumentNullException, plus actual coverage certificate/unknown
  residual. Name becomes
  `ForeachElementFromOfTypeHelper_CoverageOutsideShowsUncertainty_NotExactFinding`.
  OfType/nullability/guard facts, source and owner/location semantics unchanged.
- **BRC4:** only worker-error's stderr substring becomes
  `outside A9-FiniteOrbit-v1`. Exit4, failure prefix, no findings/report/success,
  cleanup, attestation and every other parameter case remain unchanged.

`current-certificates.json` is a fresh read-only observation against export16:
133 calls, zero observation errors. It reconstructs the saved ORIGINAL fixtures,
not edited test programs; old BRC3 fixture identifiers intentionally map to the
renamed Fact. It contains full source hashes, certificates and canonical
diagnostic type identities. This observer executes no test methods/source
programs and is not a test gate.

## New protected assertion: decision required, not changed

File: `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerProcessTests.cs`.
Fact: `HistoricalAwait_ActuallyPropagatesUnavailableInformationAsFailure`;
no parameters. Its R10-approved code expectation now passes. The next existing
assertion, lines148-149, remains EXACTLY unchanged:

```csharp
Assert.Contains(json.RootElement.GetProperty("failure").GetProperty("details").EnumerateArray(),
    detail => detail.GetString() == "Await expression runtime-await information is unavailable.");
```

**Before:** failure details contain that exact raw-analysis uncertainty sentence.
**Actual A10:** genuine Worker exit1, success=false, no result,
`UnsupportedContext`, message `Exception-flow request is outside A9-FiniteOrbit-v1.`;
details contain Profile, Query, full Root, Scope, Inventory, Complete=false,
NormalFindingAdmissionBlocked=true and classified coverage reasons, including
`... / await requires unavailable shared RuntimeAwait capability`.
The exact legacy sentence is absent. Complete raw wire evidence, actual Worker
identity and image hash are in `await-details-diagnostic.json`. That is one
diagnostic Worker invocation, not a rerun of the failed test or a green gate.

**Call chain:** existing test Run/RunBytes -> genuine Historical Worker ->
`HistoricalAnalysis.Analyze` -> shared production summary Session -> finite
definition admission -> coverage-outside -> existing `UnsupportedContext`
branch with `outside.Details`. The later generic AnalysisFailure branch that
used raw uncertainties is no longer reached.

**Classification:** TargetOrAnalysisCoverageIncomplete, not exact/admitted and
not raw diagnostic analysis. Missing shared RuntimeAwait information and the
unavailable CompletedTask body are explicit classified reasons.

**Why not already granted:** R10 explicitly allowed only this Fact's expected
failure CODE, not its following exact Details assertion. BRC1-BRC4 do not name
this assertion. No R1-R9/P* grant authorizes changing it. The original Broad
failed at the earlier code check; its next assertion was not exercised there.

**Decision requested:** either narrowly authorize replacing ONLY the exact
raw-sentence assertion with the actual classified certificate contract (profile,
Complete=false, NormalFindingAdmissionBlocked=true and the specific unavailable
RuntimeAwait coverage reason), preserving all existing failure/no-result/identity
assertions; OR decide separately that the legacy raw detail is a normative
production diagnostic compatibility requirement. The latter needs a deliberate
diagnostic-projection decision, not a test-only message insertion. Neither
option is implemented; no Failure-envelope/DTO/version workaround.

## Separate targeted-run process anomaly

Targeted CLI `scenario="missing-source"` returned -1073741819 (`0xC0000005`)
instead of4 at the unchanged exit assertion. The same case passed in the
separately planned Broad run. Both results remain evidence; no isolated retry,
assertion change, timeout relaxation or flakiness/infrastructure-defect claim.
No usable crash stack/dump was captured; a read-only Application-event query
returned no usable event (exit1). Root cause remains unproved. This cannot be
silently erased by the later pass or counted as a fully green targeted gate.

## Protection and development disclosure

`verification-r16-final.json` binds current/tested source hashes, complete TRX
counters, command records, protection and final status. Core23 has zero NEW
drift relative to the accepted explained S1 state; its historical frozen-check
exit1 is not rewritten. All332 historical Evaluation files, root ignore bytes,
physical/semantic index and six stashes remain unchanged. Results/logs are
retained under `Results/`. `git diff --check` passes.

Development-only errors are retained, not gate retries: the read-only patch
planner initially failed on a non-variable invocation; its owned process was
terminated, and an overlapping utility rebuild hit the resulting DLL lock.
The planner was corrected and emits plans only; all actual edits use apply_patch.
One patch batch needed separate per-method application because hunks were out
of source order. Export14 test build failed CS0841 (check before declaration);
export15 failed CS0121 (ambiguous target-typed test construction). Both were
fixed in the new test code before fresh export16. Two initial runner invocations
failed shell argument/execution-policy binding before any recorded build ran.
The first protection capture stopped on the KNOWN offline NuGet.Config
transformation, not a new source/protection drift. Its later first record
serialized XML counters as empty arrays; raw TRX remains authoritative and the
separate final record materializes attributes explicitly. No record is erased.

STOP with the one newly reached protected Details contract and the separate
unexplained targeted process anomaly. A10 is not complete. B25B remains closed.
