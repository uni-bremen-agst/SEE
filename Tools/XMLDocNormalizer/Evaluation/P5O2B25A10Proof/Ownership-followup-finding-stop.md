# A10 followup: owner approval applied, STOP at protected Finding assertion

The latest user grant was applied only to the named B25 Fact's entry-count
characterization and necessary P1 selection. The prior P15 owner correction and
R6 A10-only witness redirection were also applied. No production source changed
in this followup. Historical B25–A9 evidence is unchanged; B25B remains closed.

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs`.
Method: `AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted`.
Case: existing Fact, `Create(2, "same-name-types")`; no additional parameter cases.

## What passed

The fresh isolated export-07 run passed the preceding Current/Historical success
and full-result/graph parity checks, both local source-owner checks, exactly two
complete assembly identities, and the P1 explicit-throw path check.

Selection uses full `CanonicalAssemblyIdentity.Equals`, with the expected P1
identity deserialized from the existing canonical P1 summary. It is NOT
`entries[1]`, sorted-position selection, or an unqualified type-name match.
Sorting is used only for equality of the two complete-owner sets.

Both Proven exception entries have one retained path:

- `Shared.Fault`, owner `Original.Solution.P0`;
- `Shared.Fault`, owner `Original.Solution.P1`.

Classification: EXACT/admitted in the tested Current/Historical production path.
This is a finite nongeneric ownership case, not an OUTSIDE-profile substitution
for a valid exact result. It is not a complete A10 validation claim.

## The next protected assertion fails

Original line 107, now line 124 (unchanged text):

```csharp
Assert.Single(native.GetProperty("findings").EnumerateArray());
```

Actual: **two** native Current Findings, both `DOC611`.
Both render target `Shared.Fault`, path `/solution/p0/Source.cs`, line 3.
The Finding DTO's text/context does not carry the exception's assembly owner,
so they look identical in this projection. The separate canonical flow entries
retain the two distinct complete owners; no assembly-unqualified merge remains.

The existing native Finding policy enumerates missing exceptions by Roslyn type
identity (`AddExceptionFindingsCore` → `AddMissingExceptionFindings`), then uses
the type display name for the existing Finding projection. With distinct P0/P1
types it now emits two DOC611 observations. No count-based workaround, name-only
deduplication, or new Finding DTO/policy has been implemented.

Original line 108, now line 125, is also unchanged:

```csharp
Assert.Equal("DOC611", native.GetProperty("findings")[0].GetProperty("smell").GetProperty("id").GetString());
```

This second assertion was NOT reached because the count assertion failed.
The runtime witness independently contains both IDs; no pass is attributed to
the unexecuted assertion. Remaining assertions (including final admission and
lifetime checks later in this Fact) were likewise not reached in this run.

These are **native Current Finding observations**, not final adoption of
Historical SolutionTransitive Findings. The external B25B admission guard is
unchanged and remains closed.

## Exact additional decision requested — NOT implemented

For this one existing Fact only:

1. Original line 107: expect exactly two native Findings instead of one.
2. Original line 108: require `DOC611` for BOTH Findings (instead of checking
   only the first). This preserves the rule ID and strengthens its coverage;
   it is not permission to change rule semantics or other cardinalities.

All other assertions/cases remain protected. No permission is inferred from the
entry-count grant. Further production/test work is stopped pending this decision.

## Evidence and actual validation

- New A10-only full runtime witness:
  `ownership-cardinality-followup.json`, SHA256
  `CFAAC64408F0783BB5813E0A44C57DF4B37DF7B476B01A730B10B1156F6657FC`.
  It was captured BEFORE the protected Finding assertion, without bypassing it.
- Source/witness verification: `ownership-followup-verification.json`.
- Export 06 test build failed (three diagnostics) because the new local
  `CanonicalAssemblyIdentity` helper lacked its namespace import; corrected.
  The failed attempt is retained, not hidden or retried unchanged.
- Export 07 test-project, Historical Worker and HistoricalPdbFixture builds:
  **zero warnings/errors**. Previously uncompiled R2/P08–P10 test WIP now
  compiles; its runtime regressions have not been rerun in this followup.
- Exactly one affected test execution: **0 passed, 1 failed, 0 skipped**;
  failure at the unchanged Finding-count assertion. Logs/record/TRX:
  `artifacts/p5o2b25a10/runs/development-07-b25-ownership.*` and
  `artifacts/b25a10-development-07/artifacts/a10-development-results/b25-ownership.trx`.
- No full regression, Broad/Full/Boundary, Dual-Version, WAE/baseline or
  evidence-gate completion is claimed. The previous 96/96 and A5 33/33 results
  remain results of export 05, not new current-tree runs.

No commit/push/staging/stash/reset/restore. New protection/status capture:
`implementation-finding-stop-state.json`; the earlier STOP capture is retained.
