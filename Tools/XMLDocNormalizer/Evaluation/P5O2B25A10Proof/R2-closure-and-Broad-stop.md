# R2 closed; A10 Broad reveals additional blockers

## Authorized production correction and actual proof

Pre-change trace: `R2-lifecycle-and-propagation.md`.
The only additional production delta is the structured-failure return in
`HistoricalWorkerResponseValidation.Validate`: after the unchanged envelope,
exit-code and `ValidIdentity` checks, pass the same deserialized
`response.Identity` to the existing `HistoricalWorkerCallResult`. The generic
rejection factory still discards invalid/unvalidated identity. Missing identity
remains null, failure remains unsuccessful, no Result/Provenance/receipts.
No synthesis, Current change, wire/selection/version/attestation change.

Existing R2 source/assertions and R5/K17/P12 are byte-unchanged relative to
export12. Only the two new A10 diagnostic cases were updated from observing the
old loss to requiring actual wire/projected identity equality, plus negative
validation checks. Historical diagnostic witnesses remain unchanged.

Fresh isolated export13 builds TestProject, Worker and fixture: zero warnings /
errors. First run: ONLY the two unchanged R2 cases, **2/2**. Then combined
targeted B25/A **149/149, zero failures/skips**, including ownership14/14,
A10 15/15, A5 33/33, A6 8/8 and the full R5/K17/P12 Fact. Existing lifetime,
genuine-version and later isolation assertions now execute and pass in both R2
cases. `R2Closure/` retains the full real identities before/after validation,
successful original R2 witnesses and run/TRX evidence. Five invalid identity
mutations are rejected per diagnostic case; missing identity stays fail closed.

`R2Closure/verification.json` proves the exact production-only projection delta
and 754 other unchanged source/test files. Its XML counter nodes serialized as
empty arrays: original record and TRX are retained, with explicit attribute
counts in `R2Closure/trx-counter-addendum.json`. This is an evidence serialization
addendum, not another execution or a changed result.

## First Broad attempt: actual red result, no retry

Broad on export13: **3,155 passed / 179 failed / 0 skipped / 3,334 executed**,
exit1, no abort. No fatal/CoreCLR signature was found in the retained log.
This is NOT a CLR-crash explanation for the red result.

I started Broad before building BoundaryTestProcess and executing the PDB
fixture emitter. Merely building HistoricalPdbFixture was insufficient. This
was a preparation error in this continuation, not a product regression or proof
of flakiness. `Verify-DualVersionBuild.ps1` has the genuine prerequisite steps
`boundary-test-process-build` and `projection-pdb-fixture-emit` (lines238/246).
No synthetic fixtures, copied old witnesses or assertion changes were used.

Read-only inventory of all179 failures is in `BroadFollowup/verification.json`:

| Failure group | Count | Interpretation |
|---|---:|---|
| Explicit missing artifact exceptions | 50 | 45 historical-input.dll, 5 BoundaryTestProcess.dll; incomplete isolated setup |
| Process harness assertions | 17 | Nine Client / eight Lifetime; depend on absent TestProcess. Not independently reproduced, NOT proved flaky |
| Method name occurs in R1-R10 batch | 103 | Candidate coverage only: exact parameter/fixture/certificate and assertion match still required; no blanket permission inferred |
| Additional unlisted semantic expectations | 4 | Read-only contract review required; no change authorized/inferred |
| Analyzer-backlink architecture tests | 2 | Actual new architectural regressions; do NOT weaken guards |
| Incomplete-syntax productive detector | 1 | Actual production exception, not an assertion waiver |
| Manifest test | 1 | Two count assertions121 vs125, outside enumerated test grants |
| Raw reduced-extension graph | 1 | Actual rebinding exception; do NOT rewrite expectation to failure |

The original capture grouped OrderedDictionary keys incorrectly; failure rows
remain classified. `BroadFollowup/categories-addendum.json` regroups the saved
JSON rows, retaining the original record. Full native log and TRX are copied.

## Concrete additional blockers: no opportunistic repair

1. `ExceptionFlowSummaryOrchestrationDependencyTests.BuilderClosure_HasNoAnalyzerOrSessionBacklink`
   and `LowerFactResolverClosure_RemainsIndependentOfAnalyzer` fail on Analyzer
   reachability. Actual source paths:
   - Builder -> SummaryGraph -> its `ExceptionFlowFiniteOrbitAdmission` field ->
     `InspectDeclaration` -> `ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations`.
   - Lower facts/CatchSemantics -> `ExceptionFlowAnalysisResult.ProfileDiagnostics`
     typed as nested `ExceptionFlowFiniteOrbitAdmission.DiagnosticFacts` ->
     admission owner -> Analyzer. Existing IL dependency guard includes nested
     declaring types. This is a dependency-layer regression, not permission to
     loosen reflection/IL reachability checks. The R2 projection does not cause
     these edges; they pre-exist in the A10 implementation snapshot.
2. `ExceptionFlowSupportingSourceBindingTests.ReducedExtension_RegisteredSupportingSource_UsesUnreducedSourceAndReceiverFact`:
   diagnostic/raw helper -> `TryBuildTransitiveSummaryGraph` ->
   `SummaryGraph.RegisterTarget` -> `InvocationSubstitution.ApplyCallable` line96:
   `ExceptionFlowProfileViolation: Invocation member declaration is not unique.`
   Do not convert this supported raw-graph contract into production OUTSIDE.
3. `ExceptionFlowHistoricalDeclaredProductionFindingTests.PermanentBoundary_MainOwnsGuardAndPolicy_WorkerOnlyExecutesExplicitPurpose`:
   its incomplete `class` fixture reaches a productive detector exception:
   `A callable without a metadata name requires source-bound identity`, rather
   than preserving its existing empty/no-anchor behavior. No assertion change.
4. `HistoricalAnalyzerBuildProjectTests.SharedManifest_MatchesProvenBoundaryAndCurrentPhysicalSources`:
   line22 expects121 shared sources, actual125. The distinct-count assertion at
   line23 also expects121. Genuine additions are the four A10 source files:
   `ExceptionFlowFiniteOrbitAdmission.cs`, `ExceptionFlowFiniteOrbitProfile.cs`,
   `ExceptionFlowInvocationSubstitution.cs`, `IExceptionFlowSummaryTargetSink.cs`.
   Request a narrow count correction121->125 for ONLY those two assertions.
   The120-source historical probe, original membership, one runtime-await
   predicate match, real-file checks and all other tests remain unchanged.
   No dummy file/manifest removal to force121. This grant is NOT inferred.
5. Additional batch-unlisted semantic cases, not changed:
   - `ExceptionFlowExternalDocumentationTests.ExternalDocumentationContract_IsTransitiveOnly`:
     transitive external exception list is now empty, fails NotEmpty.
   - Same class `MultipleExternalContracts_ArePropagated`: same first failure.
   - `DOC611_CallSiteNullabilityTests.ForeachElementFromOfTypeHelper_DoesNotProduceFinding`:
     expected empty, actual DOC631 with classified-incomplete diagnostics.
   - `ExceptionFlowExternalTargetCliTests.GenuineCli_RejectsWithoutFindingsFallbackOrSuccess`,
     ONLY `scenario="worker-error"`: expected diagnostic substring `incomplete`
     is absent. No string relaxation. These require exact live classification /
     regression analysis before any further assertion request, not an assumed
     waiver based on missing normal findings.

## Stop boundary / remaining work

R2 is closed; the narrow authorized identity propagation is not reverted or
broadened. Existing guards are not rewritten to accommodate the above failures.
Additional architecture/rebinding/incomplete-input corrections are outside the
requested R2-only production change and need a separate followup decision;
the two manifest count assertions need their precise grant. The four unlisted
semantic cases are not pre-approved by method-name matching.

No further production/test changes were made after Broad. No fixture repair /
green retry. Full, Boundary, full Dual-Version, WAE11, Current-baselines8 and
A6/A7/A8/A9/A10 gates have NOT run after the additional STOP. B24/PID-G1/B23-B5,
generic/catch/cross-project cases included in Broad inherit its red result;
do not claim them as independent green gates. No full A10 completion/soundness
claim and no B25B activation.

Final protection/status capture: `implementation-r2-broad-stop-state.json`.
No new Core drift beyond the previously accepted S1 observations; historical
raw-manifest exit1 remains preserved. Root ignore/index/stashes and historical
B25-A9 evidence remain unchanged. No commit or push.
