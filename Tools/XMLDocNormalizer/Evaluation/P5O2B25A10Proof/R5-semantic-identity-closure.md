# R5 semantic-identity closure; R2 remains separately open

## Exact authorized change

Only the named R5 assertion in
`ExceptionFlowAssemblyQualifiedOwnershipTests.ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught`
changed relative to export11:

```diff
- Assert.Same(caught, normalizedThrown); // R5
+ Assert.Equal(Identity(caught), Identity(normalizedThrown)); // R5
+ Assert.True(SymbolEqualityComparer.Default.Equals(caught, normalizedThrown));
```

`Identity` still calls `RoslynCanonicalIdentityFactory.CreateTypeIdentity`.
`CanonicalTypeIdentity.Equals` compares assembly, module, namespace, metadata
name, recursively constructed containing type and all type arguments (plus the
other kind-specific identity coordinates). This is neither name-only nor
OriginalDefinition-only equality. Explicit Roslyn symbol equality is additional,
not a replacement for full canonical equality. Different CLR instances are
allowed, not required. K17/P12 retain their previously approved semantic checks.
The Candidate helper is unchanged; no interning workaround was introduced.

`Capture-R5Closure.ps1` verifies the entire test-file delta against export11 is
exactly the one replacement above. It also verifies 755 other source/test files
are unchanged; the export-only offline NuGet configuration transformation is
explicitly excluded, with the workspace original separately checked clean
against Git. No production source, R2 assertion, version check, failure-envelope
semantics, routing or wire DTO changed in this followup.

## Actual validation: fresh isolated export12

Test-project, Historical Worker and Historical PDB fixture builds all passed
with `-warnaserror`, zero warnings/errors. No protected workspace Core outputs
were built or modified by these commands.

The combined targeted B25/A regression ran once on this changed snapshot:
**147 passed / 2 failed / 0 skipped / 149 total**. It is NOT a green aggregate.

| Suite | Passed / total |
|---|---|
| Assembly-qualified ownership | 14 / 14 |
| New A10 admission/identity diagnostics | 15 / 15 |
| A2 canonical identity | 20 / 20 |
| A3 constructed invocation | 15 / 17 |
| A4 bounded generic context | 15 / 15 |
| A5 SCC/cache closure | 33 / 33 |
| A6 boundary | 8 / 8 |
| B25 historical Solution findings | 27 / 27 |

The affected Fact now executes completely and passes, including R5, K17, P12
and the existing exact catch checks. No further protected reference-identity
assertion failed. The new export12 diagnostic independently records the full
expected/rebound/normalized-thrown/normalized-caught identities as equal and
destination Roslyn symbols equal, with all three CLR reference comparisons
false. Actual classification: EXACT constructed rebinding, not OUTSIDE PROFILE.

## Only the two existing R2 failures remain in this selection

`ExceptionFlowConstructedInvocationContextTests.GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness`,
both `containing=false` and `containing=true`, still fail at line227 with
`NullReferenceException` when reading `historical.Identity!.LoadedRoslyn`.
Classification/certificate parity and owned-worker lifetime checks precede the
failure; the subsequent isolation assertion is not reached in those two cases.
No pass is claimed for it. Other passing tests do not replace that assertion.

As already diagnosed, `HistoricalWorkerResponseValidation.Validate` validates
the optional genuine identity on a structured failure, then its unchanged
`Fail` factory returns a Main-local result with null Identity. The wire already
contains the genuine identity. The two existing A10 diagnostic cases observing
that behavior pass; they are not a fix or substitutes for the two failed R2
regressions. R2 is deliberately not repaired in this step. Any change to failure
identity retention requires a separate contract decision; no authorization is
inferred from the R5 grant. No version assertion is relaxed.

No unchanged green retry, skip or suppression. Full/Broad/Boundary/Dual-Version,
WAE11, eight Current baselines and final A6-A10 gates remain pending; the latest
request authorized targeted validation, not a failure-envelope change.
No full A10 completion/soundness claim. B25B remains closed.

## Durable evidence and protection

`R5Closure/verification.json` contains exact scope verification, full class
counts, both actual R2 failure stacks and hashes of the copied TRX, build/test
records, complete canonical identity trace and successful post-assertion
`nested-rebind.json` witness. Original export11 failures and historical
B25-A9 reports/audits/witnesses remain untouched.

Final protection/status: `implementation-r5-closure-r2-open-state.json`.
No new Core drift relative to the separately accepted S1 observations; the
historical raw-manifest exit1 remains preserved, not relabelled green. No
manifest/baseline replacement, index/stash mutation, commit or push.
