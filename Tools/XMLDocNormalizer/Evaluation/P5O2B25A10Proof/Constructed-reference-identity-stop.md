# A10 STOP: exact constructed identity is not CLR object interning

The latest B25 Finding grant is implemented and its named Fact passed fully in
export08. The Finding count is tied to the already verified two complete P0/P1
identities of the same Native/Flow snapshot; both IDs must be DOC611, without
positional selection. The existing Finding DTO has no per-Finding assembly
field, so no stronger per-output-owner binding is invented. Message/location/
path/owner assertions were not changed. B25B remains closed.

## Combined regression actually executed

Export09 combined B25/A suite: **143 passed / 3 failed / 0 skipped / 146 total**.
Builds of TestProject, Historical Worker and fixture: zero warnings/errors.
Included: B25 **27/27**, A2 **20/20**, A4 **15/15**, unchanged A5 **33/33**,
A6 **8/8**, initial A10 **12/12**. A3 is **15/17**, A ownership **13/14**.
The three failures are described below; the aggregate is NOT green.

The prior P11–P14, R5 and P16 changes were applied within their existing exact
grants. Their new witness outputs are under A10, not historical B25–A9.

## 1. Protected K17 — additional assertion approval required

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs`.
Fact: `ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught`.
No parameter cases. Existing helper `Candidate` is unchanged.

The combined run first fails on the new P12 replacement assertion at current
line 56, which I wrote as `Assert.Same(caught, resolver.ResolveType(...))`.
That newly added reference requirement is unnecessarily strong: the approved
P12 contract is unique exact rebinding, not CLR object interning. It must be
corrected within P12 to non-null/full-identity/symbol-equivalence validation.
This error in the new assertion is not attributed to the old test.

However, an independent A10 diagnostic then ran the UNCHANGED Candidate helper
for both the source and already-destination symbols. It proves that the original
protected K17 assertion (old line 58, current line 60) would also fail:

```csharp
Assert.Same(caught, normalizedCaught);
```

Actual facts for `Shared.Outer<int>.Fault`:

| Observation | Actual |
|---|---|
| Complete canonical identities: expected/rebound/normalized source/normalized catch | Equal, including B owner, module, nested chain and Int32 construction |
| Roslyn `SymbolEqualityComparer.Default` for destination symbols | True |
| Resolver result is the exact same CLR instance as `caught` | False |
| `Candidate(thrown, A)` is the same CLR instance as `caught` | False |
| `Candidate(caught, A)` is the same CLR instance as `caught` | False |

Before A10 the resolver could not reconstruct this nested type, so Candidate's
fallback returned its input object. The successful exact resolver now constructs
an equivalent destination symbol. The old no-op/fallback reference equality is
not a guarantee supplied by Roslyn construction or A9-FiniteOrbit-v1.

Classification: **EXACT primitive rebinding**, not OUTSIDE PROFILE. There is no
lost owner/type argument, ambiguous match, name-only resolution, or missing
source/result projection in this witness. No interning/cache workaround or
Candidate-helper rewrite has been implemented to force reference equality.

### Exact decision requested — not implemented

For ONLY this named Fact:

- P12/R5: clarify the already-approved corrections as non-null, complete
  canonical identity AND Roslyn symbol equality with the unique destination
  `caught` symbol, not a CLR instance-equality requirement introduced by the
  attempted new assertions.
- **K17 additional grant:** replace only the original
  `Assert.Same(caught, normalizedCaught)` with complete canonical-identity and
  Roslyn symbol-equality checks against `caught`.

Do not relax assembly/module/arity/nested/generic identity, forge rejection,
other object-identity tests, or any other Fact. The unchanged K17 assertion has
NOT been edited. This is the new additional-characterization STOP.

## 2. R2: genuine failure identity is validated and then discarded

File: `ExceptionFlowConstructedInvocationContextTests.cs`.
Method: `GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness`,
both existing containing=false/true cases.

Both fail at current line 227, the preserved genuine-Roslyn check:

```csharp
Assert.All(historical.Identity!.LoadedRoslyn,
    image => Assert.StartsWith("5.0.0-2.25451.107+", image.InformationalVersion));
```

Before that line, both cases already passed: Current OUTSIDE classification,
PositiveConstructorCycle witness, no normal Current entries, Historical
UnsupportedContext/no result, exact certificate-details parity, and the owned
Worker lifetime check. The later Main-isolation assertion was not reached in
these two executions; no pass is claimed for it.

Two NEW diagnostic executions using the same source inputs inspected the
genuine failure wire envelope and the unchanged Main validator. They are
separate owned diagnostic processes, not retroactively captured original R2
executions. Both prove:

- Failure envelope contains the genuine loaded 5.0 Roslyn identity.
- Exit=1, UnsupportedContext, Result=null, matching Current certificate.
- `HistoricalWorkerResponseValidation.Validate` checks `ValidIdentity` at
  lines 74–76, then returns `Fail` at 78–80.
- The `Fail` factory at 30–31 deliberately constructs `(Result:null,
  Identity:null, Failure:...)`. Thus the already-validated identity is lost.

No new wire DTO is required to retain this diagnostic metadata: both the wire
envelope and Main-local `HistoricalWorkerCallResult` already have Identity.
The minimal proposed general fix is to retain ONLY the already-validated
response Identity on this structured-failure path. Result, Provenance, Finding
receipts and Success must remain absent/false. Invalid or unvalidated identities
must still be rejected. Routing, versions, lifetime and all existing genuine
version/isolation assertions need not change or weaken.

This production fix is **not yet implemented**, because the K17 STOP was
established first. No transport/security behavior was opportunistically changed.
The new `Diagnosis_*` observations describe the existing loss, not a normative
requirement to preserve that defect after a future authorized fix.

## Evidence and remaining validation

New evidence only under A10:

- `IdentityDiagnostics/canonical-object-identity.json`.
- `IdentityDiagnostics/failure-identity-False.json`, `failure-identity-True.json`.
- `IdentityDiagnostics/ownership-finding-closure.json`: the passing B25 Fact's
  post-assertion full witness, including preserved lifetime/isolation/admission.
- `identity-stop-verification.json`: hashes, complete-identity comparisons,
  exact class counts, original K17 assertion/helper retention.
- Runs: `artifacts/p5o2b25a10/runs/development-08-b25-ownership.*`,
  `development-09-a-series.*`, `development-10-readonly-diagnostics.*`.

Diagnostic tests: **3/3**, zero skips; export10 TestProject/Worker builds zero
warnings/errors. They are read-only observations of the existing production
and helper behavior, not a replacement for the failed regressions or a new
production entry. No unchanged green retry.

Full/Broad/Boundary/Dual-Version, WAE11, Current baselines8 and A6–A10 gates are
still pending. No full A10 success/soundness claim. A10 implementation WIP is
preserved. No production source changed during this continuation; no commit,
push, staging, stash, restore or reset. Earlier STOP records remain intact.
