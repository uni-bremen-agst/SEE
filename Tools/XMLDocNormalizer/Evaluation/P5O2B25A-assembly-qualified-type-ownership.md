# P5O2B25A — Assembly-qualified exception type ownership

## Phase 1: reconstructed before implementation

Starting HEAD: `4d90ce25ae143f90e968afdc8e5b6f5a7a07d82f` (B25 is now committed).
The only initial WIP is the protected root `.gitignore`. Protection capture:
`artifacts/p5o2b25a/protected-before.json`: 23 Core files, six stashes, index hash.
No commit, push, stash mutation, or original Core build is authorized.

### Exact substitution path

1. `ExceptionFlowAnalyzer.SummaryGraphThrows.TryAddSummaryThrownTypeSource`
   obtains the real `INamedTypeSymbol` from the owning compilation. The
   exception-inheritance check precedes `AddSummaryThrownSource`.
2. `ExceptionFlowSummarySource.ExceptionType` retains that actual symbol,
   including its assembly, module, containing types and generic construction.
   B25's local A/B summaries already prove the correct distinct owners.
3. `ExceptionFlowSummaryGraphEvaluator.AddSummaryLocalSourcesToResult` invokes
   `NormalizeSummaryExceptionType` before adding either a proven exception or
   external documentation source to the result. Catch-filter evaluation calls
   the same normalizer.
4. The old normalizer takes `exceptionType.OriginalDefinition`, creates a
   declaration Documentation ID and calls `GetFirstSymbolForDeclarationId` in
   the root compilation. `T:Shared.Fault` contains no assembly identity. Root
   A's colliding type is returned even for B's actual exception. Generic
   construction is also erased by taking `OriginalDefinition`.
5. Result dictionaries use `SymbolEqualityComparer.Default`; the canonical
   adapter faithfully serializes their symbols. Neither causes this loss.
   No name, MetadataName, display-string or namespace comparison can repair
   an already substituted symbol downstream.

The smallest correction point is this one shared normalizer, for both sources
and catches. The Analyzer's collection semantics need no change.

### Existing identity infrastructure and intended correction

`RoslynCanonicalIdentityFactory.CreateTypeIdentity` already includes full
assembly identity (name, version, culture, public key/token, retargetability,
content type), module name/ordinal, namespace, metadata name/arity, containing
type and constructed type arguments recursively. `CanonicalTypeIdentity.Equals`
is structural. Canonical source-symbol modules intentionally have no MVID;
exact PE/MVID/reference provenance remains independently validated by B12/B15.

`RoslynCanonicalIdentityResolver.ResolveType` checks this complete identity,
searches exact assemblies rather than accepting a name collision, and returns
null for unsupported, missing or non-unique resolution. Reuse it; do not add a
second identity representation. `ExceptionFlowCrossCompilationResolver` also
filters documentation-reference candidates by Roslyn AssemblyIdentity, but its
assembly-only test does not by itself prove generic-argument owner equality.

Rebinding is permitted only to a symbol with the complete original canonical
identity. If the destination cannot represent/resolve it, retain the original
authoritative collected symbol, not a guessed type. This is the existing
normalizer's fallback contract, not acceptance of an untrusted identity DTO.
Negative identity DTOs must fail closed in the existing canonical resolver.
Tests must distinguish unsuccessful rebinding from dropping an actual known
exception. Constructed/nested shapes must never be reduced to a definition.

### Shared modes and trust

Current ProjectTransitive, SolutionTransitive and the admitted declared-summary
path use the shared evaluator/session. Direct bypasses it. Historical logical
summary modes use the same shared source, compiled against Historical Roslyn;
direct/direct-evidence collection bypasses it. B15's PE-backed owned project
scopes already supply the real type symbols. No new IPC or Main-supplied trust
fact, compiler version, scope reconstruction or Historical compensation is
needed. B21 attestation proves binding, not the old normalizer's correctness.

SolutionTransitive final Finding admission remains closed. B25B is not part of
this work. B25's committed report/audit remain historical evidence unchanged.

## Explicitly authorized B25 characterization changes

Only `AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted`
and the normalizer-path assertion in
`StopRule_DoesNotIntroducePolicyRoutingAttestationOrPidWorkarounds` may change.
The former emits `ownership-stop-witness.json`; the latter emits no witness.
All other B25 assertions/helpers and the committed B25 report/audit remain
unchanged. New B25A evidence will retain the old wrong A owner/name-only lookup
and compare it with the actual assembly-qualified B owner after correction.

## Outcome: stop-rule boundary audit, not an ownership fix

The intended correction above was tested, then withdrawn. **Ownership is not
fixed in the final production tree. B25B is not ready and is not activated.**
The following independent constructed-type/catch boundary prevents declaring a
top-level assembly-binding repair complete. No resolver or catch-policy repair
was added after this discovery. All production sources and all existing B25
tests, including both temporarily authorized characterizations, are again
unchanged. Only new audit tests, a separate permanent audit gate and evidence
remain. Green final audit tests characterize open defects; they do not turn the
failed correctness test into a successful repair.

### Independent boundary and exact root causes

For source B and a real emitted B reference in A, construct
`Shared.Outer<int>.Fault` on both sides. Complete canonical identities are equal,
including the constructed containing type, assembly and module. The root owns
the exact PE type. Nevertheless `RoslynCanonicalIdentityResolver.ResolveType`
returns null: it obtains a nested definition by metadata name and constructs
only the nested type's own arguments, not its containing generic type.
The full-identity equality correctly refuses that incomplete candidate.

Keeping the original source symbol avoids substitution but does not make it
equal to the PE catch symbol. The unchanged catch filter therefore leaves the
same exception uncaught. This was an actual failed shared-evaluator test, not
only a code-inspection inference: `Assert.Empty` found
`Shared.Outer<int>.Fault`. An isolated execution independently confirmed it.

The existing cross-compilation resolver can resolve this particular nested
example (recorded in `nested-rebind.json`). It is a reusable lead, not a proof
that every collision/construction is resolved or that the whole contract is
closed. More importantly, an independent direct test proves that
`ExceptionFlowCaughtTypeFilter.IsSameOrDerivedFrom` compares
`currentType.OriginalDefinition` with `baseType.OriginalDefinition`. It therefore
incorrectly suppresses `Fault<int>` for a `Fault<string>` catch even within one
compilation. This does not depend on the proposed normalizer or on Historical
dispatch. Correct canonical representation alone cannot repair this consumer.

No module-identity loss or inability to represent assemblies was found. The new
boundary is exact rebinding/consumer matching, not a need for a new identity DTO,
compiler version, trust layer or broad solution reconstruction.

Nine negative canonical-resolution cases reject missing/unknown assembly,
version/culture/token manipulation, contradictory module owner/name,
reference-owner/type contradiction and absent metadata name. The candidate also
retained an actual unresolved-owner source rather than substituting A, and did
not name-rebind a symbol without an assembly. This is not a completed proof for
every ambiguous reference universe: unique complete binding across all relevant
constructed shapes remains an explicit A2 acceptance criterion. No heuristic
first match or new untrusted authority was accepted.

### Required follow-up before B25B

Proposed separate package: **P5O2B25A2 — Exact constructed/nested type rebinding
and catch-identity contract**. Specify and test source/PE rebinding for constructed
containing types and arguments with assembly collisions; reuse existing resolvers
only behind complete identity equality and unique-candidate proof. Specify exact
constructed exception/catch equality and legitimate base-type matching without
erasing construction. Cover same-assembly and cross-assembly catches, wrong-owner
and wrong-argument catches, nested/generic shapes, unresolved/ambiguous identities,
and Current/Historical differentials. Then complete the common normalizer and
repeat the B25A regression/baseline gates. No Finding admission belongs in A2;
reconsider bounded SolutionTransitive Finding adoption separately in B25B.

## Candidate history and before/after evidence

The candidate changed only the shared normalizer, using existing canonical
factory/resolver and retaining an original symbol if exact rebinding failed.
It did not change the Analyzer, routing, versions, Worker trust, CLI or PID-G1.
It was general type-identity infrastructure, with no package-specific condition.

The archived 25 candidate cases cover T1 (proven/documentation sources), T2,
T3, T4, T5, three generic/nested shapes, actual/wrong-owner catches, an unavailable
owner, a missing symbol owner, nine identity attacks, the constructed-parent
catch, and two genuine Current/Worker two-/three-project differentials.

Initial combined candidate/B25 run: **49/52 passed, three failed, zero skipped**.
The constructed-parent catch is the semantic stop witness. The other two
failures compared a new native/Worker result against a Graph probe which still
loaded the old copied Historical DLL. Native/Worker parity and correct B/C owner
assertions had already passed before those graph comparisons failed. Hashes:

- stale probe Historical DLL: `6E02C73866E59E3457A1736A4FA401A8993353C5CD3BD6A1C1878DC30F3D4FFB`;
- candidate Historical DLL: `529957E0115485C5FCA6FB580A800B7C1D927E0F300E77CD36F2270CF0950772`.

Rebuilding only the probe (0 warnings/errors), with no production/test source
change, synchronized its dependency. The two genuine differentials then passed
**2/2**, including complete graph/result parity and the actual B/C owners. This
is a corrected validation-input diagnostic, not an assertion relaxation or a
retry in product/test logic. The separate nested-catch diagnostic remained
**0/1**, the original 49/52 run is preserved, and no candidate success is claimed.

`artifacts/p5o2b25a/candidate/` preserves source, DLLs, exact input hashes,
initial failed TRX/log, isolated failing TRX, fresh-probe TRX, witnesses and
diagnostic hashes. Versionable copies of candidate tests and the withdrawn
two-file patch are in `P5O2B25AProof/`. The verifier checks the archived hashes.

Before: B's local source owns `Original.Solution.P1`, but evaluation reports
`Original.Solution.P0` through name-only declaration lookup. Candidate only:
evaluation retains the full actual B identity; in A→B→C it retains P2/C.
`P5O2B25AProof/ownership-before-after.json` explicitly distinguishes this
**withdrawn candidate after-state** from final, unchanged production.

### Exact B25 witness impact

Only `ownership-stop-witness.json` was technically changed by the first authorized
characterization during the candidate run: two distinct flow entries/owners and
two native DOC611 observations replace the old incorrectly coalesced one. Its
candidate copy is archived separately. The second authorized method emits no
witness. No other B25 assertions/helpers were adapted. The final B25 test file
is byte/content-unmodified under Git; its final executions again emit the
historical wrong-owner characterization. Neither the committed B25 report nor
its JSON audit nor its proof scripts was changed. The old error stays visible
in Git, the B25 report/audit and the fresh unmodified-HEAD baseline witness.
The initial candidate witness's embedded Graph result still belongs to the
stale probe and is not used as graph-correctness proof. Its native/Worker B
owner is valid; complete corrected graph parity is proved only by the separately
archived two fresh-probe B25A differential cases. The old attempt is not rewritten.

## Semantics and pipeline impact

Current and Historical both showed the wrong owner before; the shared candidate
showed the same correct B/C owner in both, without Historical compensation. It
was not accepted because parity and a top-level repair do not prove the complete
type/catch contract. Final production retains the original behavior. Direct,
ProjectTransitive and ProjectTransitiveDeclaredExceptions are not changed. B15
flow/graph scope, provenance and canonical representation are not extended.
Only the archived colliding-type candidate changes canonical evaluated owners
and paths; no canonical baseline was blindly regenerated.

No new productive pipeline stage is reached. Admitted bounded Direct/Project/
Declared Finding paths stay as in B24. SolutionTransitive final Findings and CLI
remain closed, including empty-flow jobs. No Dapper, signing, acquisition,
cross-project declared, multi-root or general solution-loading work was done.

## Final validation: executed; overall gate red

Fresh offline validation uses a clean Git archive of HEAD and an overlay of only
owned changes in `artifacts/b25a-4d90/Tools/XMLDocNormalizer`; original Core bin/obj
never receive build writes. The exact changed C# input and Main/Worker/Test DLL
hashes are in `artifacts/p5o2b25a/validation-inputs.json`. A clean restored-source
build, including a fresh probe build, prevents stale candidate images.

New final audit tests: **14/14**; combined with unchanged B25: **41/41**, no skips.
Executed regression results:

| Group | Passed / Total | Failed | Skipped |
| --- | --- | --- | --- |
| B24 / B23 / B22 / B21 | 42/42; 36/36; 46/46; 26/26 | 0 | 0 |
| B20 / B19 / B18 / B17 / B16 | 10/10; 39/39; 7/7; 25/25; 18/18 | 0 | 0 |
| B15 / B14 / B13 / B12 / B11 / B10 | 28/28; 31/31; 34/34; 29/29; 16/16; 34/34 | 0 | 0 |
| B5–B9 | 156/156 | 0 | 0 |
| PID-G1 | 18/18, plus nine deterministic proof decisions | 0 | 0 |
| Productive | 818/818 | 0 | 0 |
| Focused | 925/925 | 0 | 0 |
| Broad | **3225/3226** | **1** | 0 |
| Full (ordinary configured suite) | 3317/3317 | 0 | 0 |

### Broad remains red: no retry or test/harness repair

The unchanged
`ExternalSourceLineEndingReconstructionTests.ConcurrentReconstruction_PerformsOneRequestAndOneAttempt`
failed at line 341, `Assert.True(await first)`: the first Acquisition returned
false, after 5.2297342 seconds. Its fake HTTP handler signals entry and awaits a
release task; the client has a five-second cancellation deadline. A second
in-progress acquisition should return false immediately, then the test releases
the first. `ExternalSourceLinkClient.TryDownload` synchronously awaits the
download and maps cancellation to false. A delayed test continuation exceeding
the deadline is consistent with the observed failure. Scheduling/thread-pool
contention is an **inference**, not a proven captured cancellation cause: the
unchanged test does not record the underlying reason for the false result.

The same unchanged test passed in the already scheduled fresh Full run in
1.7351349 seconds, with the exact same final Main/Worker/Test image hashes; it
also passed in B25's earlier Broad in 1.3191047 seconds (inherited comparison,
not a B25A run). This proves differing outcomes without source repair, but does
not make Broad green or conclusively isolate the timing cause. Acquisition,
the old test and its timeout remain untouched. No isolated/full retry was run
to clear this failure. This is not a PID-G1 failure.

All configured regression groups were executed despite the Broad failure. The
regression phase correctly returned exit 1. Boundary and Dual are executed once
afterward as remaining required gates, not replacements for Broad. The final
evidence verifier must retain the failure, emit `Passed=false` and exit 1;
there is **no all-green validation/readiness closure** in this run. A separate
decision is needed for further Acquisition test-harness diagnosis/repair.

Standalone Boundary passed **596/596**. The complete Dual command with
`-RunMainBoundaryTests -BuildTestProjectReferences` passed as one execution
(2026-10-16 10:11:41–10:20:00 UTC), with fresh **596/596 Boundary, 36/36 B23,
42/42 B24, 27/27 unchanged B25, 14/14 B25A audit**. All eleven WAE build logs
are fresh and have **zero warnings / zero errors**. This includes Current,
Historical and Worker forced build/determinism/isolation checks; the separate
final clean/build and graph-probe builds also passed WAE. Architecture and
runtime compiler/image/isolation gates passed. No skipped tests.

The final evidence verifier completed all artifact/protection/format checks and
correctly returned **exit 1**, with `Passed=false`, `ValidationPassed=false`,
`EvidenceConsistent=true`. The only final validation defect is the recorded
Broad test failure (and its enclosing regression phase's exit 1), not a hidden
build, PID, format or artifact error. It is not waived. No all-green completion,
ownership repair or B25B readiness is claimed. The native Git LF/CRLF warning
about the protected `.gitignore` is not a build warning and was not "fixed" by
touching that WIP.

Eight fresh pre-change Current baselines and unmodified-HEAD Dual/Boundary
results are preserved in `artifacts/p5o2b25a/baseline/`. Final byte comparisons
confirmed **all eight byte-equal** (four modes × both fixture variants).
All **121 shared source fingerprints** are equal to the fresh HEAD control.
No baseline was regenerated to accept a changed result.

Final production DLLs equal the fresh control **within the same export path**:

| Image | Before = after SHA-256 |
| --- | --- |
| Main | `F9CE78C5A5DFFF894FAF609CD391206326EFB5DA9BCAC1C80797450F4A12EA8C` |
| Historical Analyzer | `6E02C73866E59E3457A1736A4FA401A8993353C5CD3BD6A1C1878DC30F3D4FFB` |
| Worker host | `34D463331C6B0CD7B518F894D4FF8297BA7B1E47007E8CF32AFC0C4DEBE183BB` |

The final test DLL was rebuilt for the 14 new audit cases; its exact executed
hash is in the compiled-input snapshot. No before-test-DLL hash was captured,
so no fabricated test-image before/after comparison is claimed. Runtime Roslyn
images and package/version graphs are unchanged. Source/compiled-image hashes,
TRX freshness, retained failed candidate attempts, JSON, PowerShell syntax,
owned C# whitespace, strict UTF8/no-BOM/CRLF, `git diff --check` and Git path case
collisions all passed consistency checks. This is actual Windows/offline
validation; the Git case/inventory audit is static, not a Linux execution.

No fresh Self Analysis or Canonical Finding Diff was run: inherited evidence is
16 self findings and canonical added/removed/changed = 0/0/0, not a B25A rerun.
The ordinary configured Full test run is not a fresh Self Analysis claim.

## Repository and boundary register

`BND-P5O2B-005` remains partially resolved; ownership is still open, now with exact
nested-rebind and construction-erasing catch witnesses. The proposed A2 contract
precedes B25B. Earlier milestone history is retained.

The subsequent traffic-reset continuation prompt was read against the existing
WIP, not used to restart implementation. HEAD/status/diff/index/stashes/Core and
the committed B25 report (plus relevant B24 closure evidence) were re-audited.
The already completed asynchronous Dual run was collected, not repeated.

No external HEAD movement: starting/final HEAD is
`4d90ce25ae143f90e968afdc8e5b6f5a7a07d82f`, branch `fix/documentation`.
The final verifier confirms **same index hash, no staged diff, same protected
root-ignore hash, 23 identical Core files, all six identical stash identities/
reflog entries**. Original Core build outputs were never built or cleaned.
No commit, push, stash operation, foreign WIP discard or unrelated repair.

Final owned changes are the new 14-case audit class, two additive build-gate
lines, this report/audit, B25A Proof scripts/archived candidate text/witness, and
the narrow boundary-register update. Production and all pre-existing test,
routing, selection, protocol, version, attestation and lifetime sources are
unchanged. Both authorized B25 characterizations remain original in the final
tree because no corrected production contract was accepted under the stop rule.

Final `git status --short` (also serialized verbatim in the audit):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B25A-assembly-qualified-type-ownership-audit.json
?? Evaluation/P5O2B25A-assembly-qualified-type-ownership.md
?? Evaluation/P5O2B25AProof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs
```

Decision: **B25A boundary investigation complete under the stop rule; semantic
repair and all-green validation closure not achieved.** Complete the separate
B25A2 type/catch contract before B25B, and obtain a separate decision for any
Acquisition test-harness investigation/repair. Do not infer readiness from the
green audit characterizations, Full or Dual while Broad is red.
