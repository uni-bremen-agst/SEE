# P5O2B25A6 - bounded generic context expansion after SCC: boundary closure

Outcome: **BoundaryAudit; no generic production correction**. The inherited A5
finite recursive-cache repair is the authoritative baseline. B25B and external
SolutionTransitive Finding Admission remain closed. No commit, push or stash
operation. Validation results are recorded independently in the accompanying
audit JSON; a green witness is not a green overall validation gate.

## Starting state and protection

HEAD `c777f9642c145a57c112c28216c3996128c6234b`, branch `fix/documentation`.
`artifacts/p5o2b25a6-r2/start.json` captures 1054 inherited files, the semantic/physical
index, root ignore, 23 Core files and six stashes before validation. All inherited
files, including A5's production change and gate/register WIP, must remain byte
identical. `P5O2B25A6Proof/Verify-Protection.ps1` also verifies 566 older evidence
fingerprints and the fingerprint-bearing artifacts of the frozen A5 audit.
Final-revision builds/tests run only in `artifacts/b25a6-c777-r2/Tools/XMLDocNormalizer`.
The first revision/export remains untouched; see the revision record below.

## Root-cause chain and A5's actual accomplishment

B25 found wrong ownership through declaration-ID/name-first normalization.
B25A showed that assembly qualification alone does not close nested/constructed
identity and catches. A2 located loss before final resolution. A3 isolated
OriginalDefinition context folding and strictly growing substitutions. A4 then
found an independent finite SCC cache defect: B evaluated under active A was
cached as globally complete, losing a reachable Fault. A5 fixes that finite
cache defect using `(key, relevant SCC ancestor exclusions)`; partial results
are not reused as unrestricted summaries. A5 does NOT implement generic body
substitution, exact constructed context registration or recursive type families.

## Lifecycle trace: graph construction precedes all SCC evaluation

Read these files under `src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/`:

| Stage | Exact source location | Effect |
|---|---|---|
| Root | `ExceptionFlowSummaryAnalysisSession.cs`, Analyze | Registers root in the session graph |
| Build | same file, BuildPendingSummaryNodes before evaluator.Evaluate | Must return before evaluation begins |
| Work queue | `ExceptionFlowSummaryAnalysisSession.GraphConstruction.cs`, BuildPendingSummaryNodes | Repeatedly dequeues until empty; analyzes key.Symbol declarations and merges the fragment |
| Registration | `ExceptionFlowSummaryTargetRegistrar.cs`, RegisterMethodTarget | Resolves supporting source, rebinds call facts, registers target/context |
| New work | `ExceptionFlowSummaryGraph.cs`, GetOrAdd | A previously absent key creates a summary and is enqueued once |
| SCC discovery | `ExceptionFlowSummaryGraphEvaluator.cs`, Evaluate/FindSummaryComponents | Reads the already built graph; creates finite component/member IDs |
| Cache/evaluation | same file, activeKeys/completedFrames | Cycle-erased closure with SCC exclusion-sensitive reuse, per Evaluate call |

TryBuildTransitiveSummaryGraph similarly drains pending work before returning.
There is no interleaved SCC solver or constructor-growth guard in the builder.
The source-binding verifier ties these source bytes to the executed Current and
Historical images; this table does not claim a newly executed infinite build.

## Context identity and the actual losses

`ExceptionFlowCallContext` normalizes CallableSymbol through OriginalDefinition
in its constructor. Its legacy Key contains parameter value/member facts, not
the concrete generic environment. Its canonical callable is definition-normalized.
RebindCallable transfers those facts but does not preserve a separate scoped
method/containing/nested substitution environment.

`ExceptionFlowCallableKey` stores symbol.OriginalDefinition. Equality uses that
symbol plus ContextKey, so absent a new generic environment M<int> and M<string>
collapse. Merely changing its canonical display does not change equality.

Real source/semantic-model call sites retain M<int>, C<int>.M(),
C<int>.M<string>() and Outer<int>.Inner<string>.M<bool>(). OriginalDefinition
loses both method construction and constructed containing chain. ConstructedFrom
can retain the constructed container while exposing the method definition; it
is not interchangeable with OriginalDefinition. Method/containing parameter
ordinals must be scoped by declaration, module and assembly, not their names.
These compiler facts are characterized by the unchanged A2/A3 tests, rerun in
the isolated export; their parity does not prove correctness of the old result.

## Finite structural witness and general argument

New `ExceptionFlowGenericExpansionBoundaryTests` has eight cases:

- Four method/containing x List/Dictionary<string,List<...>> growth cases.
- Four method/containing x identity/constant-closing finite controls.

The selected initial argument in this witness is string (the same argument
applies to int). A hypothetical exact key is constructed using the existing
length-delimited canonical identity as ContextKey. This is test-local, not a
changed production registration path. At every one of 24 observation steps,
the successor is fresh, one more summary is registered, and a further pending
successor remains. The unchanged real key still folds the two constructions;
the real graph in this test remains finite for that WRONG reason.

The general, non-numerical argument is: let t0=int and t(n+1)=List<t(n)>.
The number of nested List constructors is n. For n != m, structural canonical
identity differs, with the same List declaration/owner and a different argument
tree. Consequently all exact A<t(n)> keys differ. GetOrAdd must enqueue each
fresh successor, so a builder faithfully expanding that recurrence cannot empty
its queue. Dictionary<string,List<t>> has the same strictly increasing height
argument. This proves the exact-materialization boundary; a finite test prefix
alone is NOT asserted to prove divergence or to specify a production depth.

Identity substitution repeats the same key. Closing A<string> -> A<int> reaches
a repeated closed state after two distinct constructions. These controls prevent
confusing finite recurrence with structural growth. No nonterminating real
analysis, timeout, production depth limit or retry was introduced.

## Why the tempting shortcuts are insufficient

Definition-folding discards Fault<int>/Fault<string> distinctions; finite graph
size alone is not correctness. A depth cutoff drops potentially reachable
Fault<List^n<int>> without defining a conservative result/completeness contract.
Moving the cutoff to A5 is too late: its first SCC scan needs graph construction
to have completed. Its excluded-member key does not make an infinite collection
of distinct concrete graph keys finite.

Existing UncertainTargets can flag incomplete work, and canonical uncertainty
has AnalysisUnsupported. Neither defines a constructor-growth detector, sound
family abstraction, substitution closure or which exceptions remain possible.
Path truncation (64 paths per concrete exception) is a different operation and
does not bound the number of types or graph states.

## Catch, rebinding and output boundaries

The current caught-type filter stores OriginalDefinition and compares base-type
OriginalDefinitions. Fault<int> and Fault<string> can therefore suppress one
another incorrectly. Body catch(Fault<T>) also needs the invocation substitution.
The evaluator's NormalizeSummaryExceptionType uses a declaration ID of the
OriginalDefinition and GetFirstSymbolForDeclarationId in the root compilation;
this can lose construction and owner independently of graph-key correction.

CrossCompilationResolver resolves the unique assembly-matched declaration and
constructed containing type, but does not reapply concrete METHOD arguments.
Supporting-source RebindCallable then normalizes again. A -> B -> C needs composed
owner-scoped substitutions at both boundaries, not only a better final lookup.
No cyclic project-reference scope is newly claimed.

Existing canonical callable/type objects represent finite concrete construction,
containing chains and assembly/module owners. A recursive family is not itself
one CanonicalTypeIdentity. An internal parametric representation does not by
itself require a wire change: fully evaluated finite exact results could retain
the current wire. Exporting a non-singleton residual family would require an
explicit result/projection decision. A6 proves neither that a wire extension is
necessary for every solution nor that the current wire can express every family.

## Phase-0 permissions and scope decision

Earlier permissions and the five newly conditional A6 permissions are recorded
in A7's characterization table. None was exercised: all existing tests remain
byte-identical to start.json. Structurally growing characterizations remain
unchanged; historical B25--A5 reports/audits/witnesses remain frozen. All fresh
A6 witness output is under the isolated export's artifacts/p5o2b25a6/analysis.

No generic candidate was implemented, so no candidate was withdrawn and no
finite-only partial repair is left behind. A5's existing production diff is not
an A6 production change. No routing, compiler version, acquisition, PID or
Finding-admission change. The next necessary boundary is a finite parametric
summary/family contract including substitution cycles and catch residuals; A7
examines it separately. B25B remains closed regardless of validation status.

## Reproduction and validation provenance

### First-revision witness serialization defect (retained, not retried unchanged)

The first revision's WAE build and complete Dual pass remain under
`artifacts/p5o2b25a6/runs` and `artifacts/b25a6-c777/Tools/XMLDocNormalizer`.
Its FIRST new witness run is 6/8: both dictionary-growth cases pass their semantic
assertions, then fail in Evidence serialization at System.Text.Json's default
depth 64. They serialize the whole deeply nested canonical object graph. This
is a deterministic defect in our NEW test evidence output, not a PID/Full flake
or an existing analyzer assertion. Combined/Broad/Full had not yet started.

Only that new output projection changes: each observation now emits complete
length-delimited canonical ExactKey/SuccessorKey strings. No assertion, witness
step count, generic program, source/target type, serializer depth limit or
production behavior changes. The original source, compiled images, PDB bindings,
TRX and actual process exit remain in the first export/records; nothing is
overwritten. A fresh r2 export recompiles/revalidates the changed source,
including a new full Dual. This is a source-correction revision, NOT an unchanged
retry to turn a flaky gate green. The audit lists both revisions and the exact
one-line source diff. No overall initial failure is concealed.

See `P5O2B25A6Proof/README.md`. Build, Dual, witness/combined suites, unchanged
Broad/Full selections plus the A6 witnesses and a separate Boundary run are new
attempts, not retries of A5. The Dual script itself is unchanged. Its nine test
gates include A5 33 and the other historical suites; the eight new A6 cases are
run separately and included in combined/Broad/Full. The Broad filter is assembled
identically to the inherited A5/B24 runner (same base JSON and historical additions),
with only the new A6 class added. Full has no filter. B24/B23--B5/PID and the other
regressions remain in those required suites; they are not skipped. Redundant
individual productive/focused/older-group runs are not claimed as fresh A6 runs.

Audit `Tests`, `Runs`, `WaeBuilds`, `CurrentBaselines`, `GateFailures`,
`SourceBindings` and `Protection` are authoritative actual results. A failing
Full fixture is retained without repair/retry and makes the overall validation
gate red. Evidence consistency and successful production correction are separate
properties. Source/image hashes, fresh TRX time intervals and actual process
exit codes are checked. Self Analysis and Canonical Finding Diff are not newly
executed; inherited 16 findings and inherited 0/0/0 diff are historical only.

Final HEAD/status/protection are captured in the audit. An actual Linux runner
is not claimed. Static Git case/path/project checks are distinguished from it.

## Actual final-source validation (r2)

| Check | Actual result |
|---|---|
| New witness suite | 8/8, zero skips |
| A5 finite SCC suite in full Dual | 33/33, zero skips |
| Combined A6/A5/A4/A3/A2/A/B25 | 134/134, zero skips |
| Broad, inherited selection + eight new cases | 3319/3319, zero skips |
| Full, unfiltered, first and only r2 attempt | 3410/3410, zero skips |
| Separate Boundary | 596/596, zero skips |
| Complete Dual | PASS; all nine existing test gates and eleven WAE steps |
| New test-project WAE build | PASS, zero warnings/errors |
| Eight Current baselines | Byte-identical to frozen reference bytes |
| Source/image binding | 243 PDB document bindings; eight pure inherited LF/CRLF differences |
| Production / existing test changes from inherited A5 WIP | 0 / 0 |

The final Dual ran 2026-10-21 18:09:54--18:18:50 UTC. Full ran
18:25:36--18:29:11 UTC; separate Boundary ran 18:29:39--18:32:14 UTC.
Actual process start/finish timestamps and complete logs are retained in r2/runs.
The evidence gate's actual exit/audit digest is retained separately, not inferred
from this table; the audit checks all rows, freshness, unchanged inputs and hashes.

The unchanged MultiModuleAssembly_FailsClosed case PASSED in this Full run
(0.0343540s). The unchanged acquisition-concurrency case also PASSED (5.8033174s).
No fixture/acquisition repair or isolated/Full retry was performed. This pass is
an observation, not a claim that the historical fixture-lifetime problem was
fixed. Frozen A5 Full 3401/3402 and its red evidence gate remain unchanged.
The initial A6 witness serialization failure (6/8) is likewise retained explicitly;
it is not included as a successful execution of the final corrected source.
