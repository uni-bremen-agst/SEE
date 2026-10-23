# A10 implementation STOP: an unapproved B25 cardinality characterization

A10 is **not complete**. R1–R10 were approved before this implementation.
The user-required additional-characterization STOP now applies. No assertion
of `ExceptionFlowHistoricalSolutionFindingTests.cs` has been changed.

## Observed counterexample to the Phase-0 completeness claim

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs`.
Method: `AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted`.
Case: existing Fact, `Create(2, "same-name-types")`.

The unchanged test was run once in `artifacts/b25a10-development-05` and failed
at **line 104**, not the already approved owner assertion at line 105:

```csharp
var entry = Assert.Single(historical.Result!.Entries);
```

Actual collection: **two Proven entries**, one owned by
`Original.Solution.P0` and one by `Original.Solution.P1`.
The preceding success, full Current/Historical result equality, graph equality,
and per-summary source-owner assertions all passed. The test stops at line 104;
assertions below it have NOT been claimed as executed.

The root fixture actually contains:

```csharp
if (Value != null) throw Value; // Shared.Fault from P0
P1.Fixture.Root();             // throws Shared.Fault from P1
```

Both source owners are independently asserted at line 103. There is no catch
between these sources and the root result. The old first-match normalization
collapsed both owners to P0. Preserving exact assembly identity must preserve
both reachable types, not merely relabel a single merged entry as P1.

Classification: **EXACT/admitted finite nongeneric case in the tested WIP**.
The actual Historical success is downstream of the new shared admission check;
its full result equals Current. This is not an OUTSIDE case, growth certificate,
or permission to remove a valid P0 source. It is not a full A10 soundness proof.

The Phase-0 supplementary review for this method incorrectly concluded that
changing only the owner at line 105 sufficed. That review is retained as history;
this new runtime evidence explicitly corrects its completeness claim.

## Narrow additional approval needed

Not covered by P15/P16 or R1–R10:

1. Line 104: replace the one-entry characterization by exactly **two** entries
   with the distinct complete owners P0 and P1. Select the P1 entry by its exact
   assembly-qualified identity for the previously approved line-105 correction
   and existing P1 explicit-throw path assertion. Preserve the valid P0 entry.
2. Audit the directly dependent native Finding assertions at lines 107–108
   (`Assert.Single(native.findings)` and `findings[0].smell.id == DOC611`) before
   editing them. Their post-line-104 execution/result has not yet been observed.
   There is **no inferred permission** to change their count, IDs, or policy.
   Approval of item 1 alone is not approval of these Finding changes.
3. New witness metadata for this exact Fact may reflect two distinct owners
   only under A10. Historical B25 reports/audit/witnesses remain untouched.

No B25B activation, normal Finding-policy change, removed source, name-only
merge, or broad assertion relaxation is proposed. Further production/test
implementation is stopped pending the user's decision.

## Actual validation, including failures

- Initial Main development builds: successful; one changed-code build failed
  on `Exception` namespace shadowing, corrected to `System.Exception`; later
  Main builds passed with zero warnings/errors.
- Development export 01: test-project build failed on the NEW test's unqualified
  `Check` method name shadowed by the existing test namespace. Corrected.
- Export 02: initial new A10 tests **6/6**, no skips.
- Export 03: expanded new A10 tests **9/9**, no skips. Separate existing
  identity/catch/DOC631 selection: **49 pass, 1 fail**. New admission had treated
  missing executable Main (CS5001) as a member binding failure.
- Export 04: new Workspace reproducer **1 fail / 9 pass**, retaining the full
  classified rejection. Production now distinguishes only CS5001 packaging
  from member-binding errors; binding/reference/constraint errors still reject.
- Export 05: contextual/identity/catch/DOC631 selection **96/96**, zero skips,
  including **12 new A10 cases** and **A6 8/8**. TRX/log retained.
- Unchanged A5 SCC/cache suite: **33/33**, zero skips, including genuine Worker
  parity and the 4,608 finite-oracle comparisons.
- Historical Worker and HistoricalPdbFixture builds: **0 warnings / 0 errors**.
  Shared A10 sources compile against both existing Roslyn versions.
- Unchanged B25 ownership Fact: **0 pass / 1 fail**, exactly the two-entry
  failure above; no retry or assertion edit.
- Auxiliary standalone evidence observer: first launch interrupted, no usable
  witness; isolated launch failed with `Exception.ToString() failed`, no usable
  witness. Neither attempt is an analyzer/test pass or used as proof. Both
  attempts remain recorded, including the interrupted null-exit record. The
  diagnostic observer is not a production entry or test replacement.

The current workspace also contains the subsequently authorized R2 and P08–P10
test edits, which were made AFTER export 05. Those latest edits/helper additions
have **not yet been compiled or run**. The 96/33 results apply to the captured
export-05 revision, not to an invented all-green current-tree completion.

Not run as final A10 gates: combined B25/A, B24, PID-G1, B23–B5, Broad, Full,
Boundary, complete Dual-Version script, 11 WAE steps, 8 Current baselines,
A6/A7/A8/A9 gates, A10 evidence gate. No gate is marked green by inheritance.
The STOP prevents claiming or proceeding with the requested full closure.

## Implementation WIP and safety

P1 discovery precedes root registration in `Session.Analyze`. P2 graph guards
check the sealed declaration/map before composition and registration, and check
scope/closed context before `GetOrAdd`/enqueue. Failed construction rolls back
new nodes. A new neutral slot-SCC checker, simultaneous invocation substitution,
exact nested/assembly resolution and catch matching are present but not yet
fully validated. The A5 evaluator closure algorithm was not replaced.

Outside results use an internal typed certificate and existing Worker
`UnsupportedContext` details; normal exact Finding output is blocked. A separate
finite ground lower-witness subquery preserves diagnostic-only DOC631 contracts
without building a raw graph or promoting a partial result to exact.

Production source search finds no call to `TryBuildTransitiveSummaryGraph`
outside its declaration. Current uses `Session.Analyze`; Historical invokes the
same shared session and rejects outside before success projection. No new raw
facade, raw fallback, wire DTO, Roslyn version, or routing policy is introduced.
Full bypass/soundness/regression certification remains pending.

No commit, push, staging, stash, restore, or reset. Historical S1 drift and frozen
manifests are not rewritten. Final protection/status are captured separately in
`implementation-stop-state.json`.
