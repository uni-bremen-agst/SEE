# K17/P12 grant applied; targeted validation STOP at R5

The latest grant is applied only in
`ExceptionFlowAssemblyQualifiedOwnershipTests.ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught`:

- P12: replace the newly introduced CLR `Same` requirement with non-null,
  complete `Identity(caught) == Identity(rebound)`, and explicit
  `SymbolEqualityComparer.Default.Equals(caught, rebound)`.
- K17: replace only `Assert.Same(caught, normalizedCaught)` with complete
  canonical identity equality and explicit Roslyn symbol equality.
- Different CLR instances are allowed, not required. No name-only or
  OriginalDefinition-only comparison; assembly/module/nested construction is
  still part of the existing full canonical identity.

No production file, Candidate helper, R2 assertion, Worker failure envelope,
version/identity validation, routing or wire DTO was changed in this followup.

## Actual validation: fresh WIP export11

Test-project build: passed, zero warnings/errors, with `-warnaserror`.
The targeted Fact: **0 passed / 1 failed / 0 skipped**.
It passed all new P12 checks, then failed at current line 62:

```csharp
Assert.Same(caught, normalizedThrown); // R5
```

Expected and actual both display `Shared.Outer<int>.Fault`, but are different
CLR instances. This is the R5 assertion, NOT the now-corrected K17 assertion.
K17 lies after R5 and was not reached; no executed K17 pass is claimed.

Existing A10 diagnostic evidence in
`IdentityDiagnostics/canonical-object-identity.json` separately proves complete
canonical equality and Roslyn symbol equality for this exact normalized source,
with CLR reference inequality. That prior diagnostic is not a passing rerun of
the current Fact. Classification: EXACT constructed rebinding, not OUTSIDE.

## Narrow approval clarification required before continuation

The original R5 batch authorized replacing source-instance reuse with the
uniquely rebound consumer symbol, but explicitly proposed
`Assert.Same(caught, normalizedThrown)`. This overstrict replacement is my new
assertion, not a newly discovered production identity defect. The previous STOP
requested clarification for P12/R5; the latest grant expressly names only
K17/P12 and excludes further reference-identity changes. I do not infer R5
permission from adjacent lines.

Requested change, ONLY this R5 assertion in this Fact:

```csharp
Assert.Equal(Identity(caught), Identity(normalizedThrown));
Assert.True(SymbolEqualityComparer.Default.Equals(caught, normalizedThrown));
```

This retains full canonical owner/assembly/module/nested/constructed identity
and explicit destination Roslyn equality; it removes only CLR object interning.
It is NOT implemented. No Candidate/interner workaround is introduced.
All other assertions remain unchanged except the exactly approved K17/P12 edits.

The two R2 failures remain separately open and unchanged. No identity-retention
fix or failure-contract change is inferred from the K17/P12 approval.
No further tests or full gates were run after the R5 STOP. No green retry.
Full/Broad/Boundary/Dual-Version and other final A10 gates remain pending.
B25B remains closed; no A10 completion claim, commit, push or stash operation.

## Evidence

Recorded runs: `artifacts/p5o2b25a10/runs/development-11-test-build.*` and
`development-11-k17-p12.*`. Durable copies of the TRX/run records and exact
scope verification are under `K17P12Followup/`.
Final protection/status capture: `implementation-k17-p12-r5-stop-state.json`.
Previous STOP snapshots and historical B25-A9 evidence are retained unchanged.
