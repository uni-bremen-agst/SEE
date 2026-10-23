# Await-details approval applied; Broad green; Full stops at foreign fixture lifetime

Latest: independent Boundary/Dual/proof/evidence checks have also completed.
The first Full-stop snapshot `await-approval-followup.json` is retained unchanged;
`independent-validation.json` supersedes its then-unexecuted-gate flags.

Current HEAD: `9b76e179a568c64ae972cc3a45980a094e44245e`, unchanged.
Fresh export17 starts from the authoritative existing A10 WIP. No commit, push,
stash/index operation, protected WIP repair or B25B activation.

## Exact authorized source delta

Only `HistoricalWorkerProcessTests.HistoricalAwait_ActuallyPropagatesUnavailableInformationAsFailure`
changes relative to export16. Its existing `UnsupportedContext` code check and
failure/no-result checks are unchanged. The one exact legacy Details sentence
is replaced by assertions requiring:

- `Profile=A9-FiniteOrbit-v1`;
- `Complete=false`, no conflicting `Complete=true`;
- `NormalFindingAdmissionBlocked=true`;
- a `TargetOrAnalysisCoverageIncomplete` reason specifically identifying
  unavailable RuntimeAwait information, without binding a whole prose sentence;
- actual Worker identity still reports `runtimeAwaitInformationAvailable=false`.

The Worker-only request fails without a result; no alternate Current fallback
is introduced. Production does not reconstruct the old sentence. No new wire
DTO, message, routing, version, attestation or other assertion change.

`await-approval-followup.json` verifies the complete source/test delta against
export16 is exactly this block (line-ending normalization only in the textual
comparison; raw hashes also retained). Production source changes this turn:0.
All current source/test files match tested export17.

## Actual execution, no unchanged green retry

| Run | Passed | Failed | Skipped | Total |
|---|---:|---:|---:|---:|
| Approved Await Fact, first run |1|0|0|1|
| Complete unchanged-filter Broad |3338|0|0|3338|
| Full |3428|1|0|3429|
| Boundary |596|0|0|596|
| Dual-Version, nine test groups combined |800|0|0|800|

All eleven Dual WAE steps have fresh zero-warning/error logs. All eight Current
baselines are byte-equal to frozen A9 baseline bytes, without a new baseline.
Unchanged A8/A9 finite contract harnesses pass40/40 and37/37 respectively;
the additional real A10 production-path harness passes30 checks. Exact source
checksums in three PE/PDB pairs bind125 shared sources in both Main/Historical
plus four test files (254 checked PDB documents), with zero byte differences.

The A10-owned A6/A7/A8/A9/A10 adapters execute once each, preserving the old
scripts/audits rather than running them with historical output paths:

| Adapter gate | Exit | Result |
|---|---:|---|
| A6 regression evidence |1|Only failed requirement: `required-runtime:full`|
| A7 static contract consistency |0|General-scope STOP remains; not production validation|
| A8 contract/regression evidence |1|Only failed requirement: `required-runtime:full`|
| A9 bounded-contract/regression evidence |1|Only failed requirement: `required-runtime:full`|
| A10 evidence |1|Only failed requirement: `required-runtime:full`|

These are explicitly described A10 adapters of applicable obligations, NOT new
claims that the frozen old-HEAD scripts passed against modified source. All
non-Full checks pass. No Full failure is waived or converted into completion.

Main/Test, Historical Worker, PDB emitter and BoundaryTestProcess builds all
complete with zero warnings/errors. The real Historical PDB emitter executes.
Await passes in all three test runs. No native crash was observed in this
continuation. The prior targeted CLI0xC0000005 remains UNATTRIBUTED and is NOT
declared repaired by these later passes. No relationship to the fixture below
is established. Historical crash/failure evidence remains untouched.

The Full-first controller stops at the nonzero Full result. The foreign repair
remains stopped, but independent Boundary/Dual/contract/evidence checks are
subsequently executed without retrying Full. Self Analysis and Canonical Finding
Diff are NOT fresh. A10 is not complete.

One new controller initially failed PowerShell parsing before any Full command
started; the line continuation was corrected. Exactly one actual Full attempt
is recorded. No isolated MultiModule test, Full retry, exclusion, assertion
relaxation or production workaround is performed.

## Full failure: existing foreign reader-lifetime hazard

File: `Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalMetadataReferenceFactoryTests.cs`.
Fact: `MultiModuleAssembly_FailsClosed`, no parameters. Unchanged line446:

```csharp
Assert.Contains(ReadAssemblyFiles(image), file => file.ContainsMetadata);
```

Observed: `Assert.Contains() Failure: Filter not matched in collection`,
collection `[System.Reflection.Metadata.AssemblyFile]`. This occurs before
`ExternalMetadataReferenceFactory.TryCreate` at line451; the factory and A10
Analyzer are not on the failing invocation path.

Read-only source trace:

```text
EmitMultiModuleAssembly
→ ReadAssemblyFiles
  → using MemoryStream / using PEReader
  → reader.GetAssemblyFile(handle), store reader-backed structs
  → return ImmutableArray<AssemblyFile>
  → dispose PEReader/stream
→ caller dereferences file.ContainsMetadata
→ fixture precondition assertion fails
```

The helper is at line663. Both this Fact and
`ResourceOnlyAssemblyFile_DoesNotTriggerMultiModuleRejection` consume those
reader-backed values after the owning reader is disposed.

This exact lifetime hazard and failure location were already documented in
`artifacts/p5o2b25a5/foreign-full-failure.json`. Current workspace and export17
test source SHA256 are BOTH the historical value:
`4D959975EEFF758C31517E1D2C0C0EAAA2C805E5E9621B7E581FBB2A7A8420BC`.
The actual Metadata DLL SHA256 also matches the historical diagnosis:
`CD87C789B7FA364165590F841770CDD481ADF1478849B7F7FC6AD7972BEBAC3D`.

Confirmed: the lifetime hazard is pre-existing; the current failure occurs at
the same unchanged fixture assertion before production is reached. NOT proved:
the exact freed-memory contents/reuse or retained emitted image for this failed
invocation. No flakiness label, memory-reuse claim or new semantic A10 defect is
inferred from the result. The historical diagnosis is read, not rerun/rewritten.

## Separate decision needed

The new approval authorizes ONE Await Details assertion, not an unrelated
metadata-fixture repair. No opportunistic repair is made here.

Proposed separate bounded harness repair: materialize the actual metadata flags
while the PEReader is alive and return detached data (or explicitly keep the
reader lifetime through consumption). Only necessary helper/caller types and
lifetime handling should change. Preserve the managed-module rejection and
linked-resource acceptance assertions; no dummy fixture, fabricated flag,
factory/Analyzer change, skip, retry logic or weaker assertion.

## Additional closure tools and their limited evidence scope

`Program.cs`/`Validation.csproj` prepare additional real `Session.Analyze`
production-path checks, including finite permutation/constant orbits, nested
catch identities and A→B→C substitution/coverage rejection. It is built cleanly
and its30 checks pass through actual `Session.Analyze`, not a raw graph.
`Run-Proofs.ps1` stages unchanged A8/A9 contract harnesses under A10-owned paths.
It requires green Boundary/Dual and permits independent proof collection only
for the one precisely identified existing Full fixture failure, without waiving
it. Three proof-tool builds have zero warnings/errors. No new Production/
Diagnostic facade exists and no Raw Graph is certified by these tools.

The first proof staging invocation failed before any build because PowerShell
combined two computed filenames. Only empty directories existed; no file or
attempt record was overwritten. The launcher expression was corrected before
the first actual proof builds/executions. No unchanged test/gate retry.

## Protection and retained evidence

`../BroadClosure/verification-r17-final-independent.json` captures final Git status, current
source hashes, complete result counters and unchanged protections. Logs/TRX are
copied without overwriting previous attempts to `../BroadClosure/Results-r17-independent/`.
Independent gate/proof/dual records are also retained under `IndependentResults/`.
Core23: no new drift relative to explained S1; its historical frozen-check exit1
is preserved. All332 historical Evaluation files, root ignore, physical and
semantic index and six stashes remain unchanged. No new baseline or restore.
`git diff --check` passes. B25B remains closed.
