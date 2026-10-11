# P5O2B15 - Historical Cross-Project Solution Scope Equivalence

Starting HEAD `d7251cf4e450aab2458f8f91b12b411fa6a39a4d`, separately committed
B14. Initial WIP: protected root `.gitignore` only; empty index, six stashes,
23 Core files fingerprinted before edits. No applicable AGENTS.md found.

## Actual Current chain and B15 matrix BEFORE implementation

ProjectClosureSemanticContextBuilder identifies native projects by opaque
ProjectId, recursively walks explicit ProjectReference.ProjectId for
SolutionTransitive only, constructs project compilations and associates each
exact SyntaxTree instance with its owning SemanticCompilationScope. Reporting
projects are AnalysisTargets; referenced projects have a separate role. No
filename/directory determines membership. Scope enumeration merges the explicit
project closure with the supporting catalog, ordered by assembly name/role.

Source-backed CompilationReference calls already have declaring syntax in the
other owned compilation. PE-backed external calls use SupportingSourceCatalog /
SupportingSourceSymbolResolver, which selects an exact assembly/binary scope and
the already shared ExceptionFlowCrossCompilationResolver.ResolveMethod. That
resolver preserves full assembly identity, exact declaration/signature and
constructed types; no method-name guessing. SummaryTargetRegistrar transfers
call-site context to the resolved source key. Summary graph dispatch/completeness
enumerates GetAnalysisScopes; graph construction requests the exact tree's stable
model. Main's external reconstruction catalog can acquire/reconstruct missing
sources, but that orchestration is not an IPC/compiler input to serialize.

| Information | Current | Historical before B15 | Analysis relevance | Neutral transport | B15 measure | Trust / reconstruction |
| --- | --- | --- | --- | --- | --- | --- |
| Project identity | Native ProjectId; external original full P3 assembly/modules | One B14 assembly-bound project | Select exact compilation, no same-name aliases | Yes, typed original assembly/module/MVID | Explicit bounded binary-project identity, not Workspace IDs | Existing original P4/P5A receipt; source assembly identity postcondition |
| Source ownership per project | Exact tree -> scope map | Stable models within one original P5J set | Foreign tree must not enlarge another project's scope | B14 source/parse/manifest model | Reuse B14 unchanged per node; global unique logical paths for unambiguous returned provenance | Original P5G/P5I/P5J; no new source validation |
| Project dependencies | Explicit native ProjectReference or exact registered external references | No dependency graph | A -> B -> C follows different scopes | Yes, explicit typed edges | Derive edges from exact existing B12 identity/module/MVID references to supplied validated projects | No path/name inference; missing/conflicting endpoint rejected |
| Cross-project metadata identity | CompilationReference or P5C/P5E exact PE world | B12 images only in target | Binding/friend/types/signatures | Existing B12 images/properties | Reuse original reference projection, preserve each project's ordered world | Original successful P5C/P5E and unsigned P4 identities |
| Target project | Reporting target plus exact source/PE root | B14 root project/tree/selector | Select only target root | Typed graph target plus B14 root | Require exact outer target membership, no selecting a supporting root | Existing PE-bound selector and exact root-tree check |
| Supporting projects | Closure scopes/supporting catalog | Reserved supporting envelopes rejected | Missing helper body yields incomplete/failed flow | Validated B14 node inputs | Bounded explicit reachable DAG, no acquisition or workspace loader | Every node must independently pass original B10-B14 handoffs |
| Cross-project method binding | Existing shared CrossCompilationResolver | Local source only | B-bound metadata must map to B source | Reconstruct actual compiler objects in child | Reuse shared resolver in Historical host adapter after exact reference binding | Actual Historical binding and full source ownership, no second parser |
| Cross-project source/method mapping | Exact resolver destination scope and stable model | Foreign compilation rejected | Correct helper locations and context transfer | Full node source description | Stable model map over all exact owned trees, scoped binary-reference lookup | No source/tree renaming; globally conflicting paths rejected in bounded profile |
| Cross-project summary scope | SummaryTargetRegistrar + builder/evaluator/dispatch/completeness | One scope | Source summaries/dispatch candidates across closure | Reconstruct scopes, not serialize graph cache | Unchanged shared 121-source analyzer over sorted owned node scopes | Full result/summary/edge/repeat differential, no analyzer edits |
| Solution reachability | Explicit recursive project closure, per-mode sessions | Local single-project SolutionTransitive only | Extra or absent projects change flows | Directed graph plus target | Require every supplied project reachable, cycles rejected, no smaller fallback | Current builder is cycle-visited, native Workspace cannot admit circular ProjectReference; bounded worker DAG only |
| Mode separation | Direct collector; ProjectTransitive reporting projects; Solution closure | B14 explicit local mode | B15 must not widen Direct/ProjectTransitive | Existing mode string with graph policy | Solution graph opt-in only for SolutionTransitive; other modes explicitly reject graph | B5/B6 and native detector unchanged |
| Findings/reporting policy | Native detector captures full Findings | Canonical worker only | Reporting downstream of flow, not scope input | Separate import contract | Preserve eight full Current baselines and full native Findings; no Historical Finding import | Explicitly excluded; do not claim cross-engine Finding import |

## Bounded decision

Close a connected **original validated external binary-project DAG** for
SolutionTransitive: exact local B14 models per project, explicit identity-bound
edges, genuine PE-to-source rebinding and unchanged summary engine. Support the
simple and multi-stage chains if real Current differential proves them. A native
Workspace ProjectId/CompilationReference solution is not itself an original
P5A/P5J/P5E trust receipt and will not be serialized or inferred from paths.

Protocol graph budgets are explicit and opt-in: at most four projects, original
B14 source budgets per project plus 65,536 total UTF-16 source characters and
16,384 syntax nodes,
64 MiB total repeated reference bytes and 96 MiB request characters. Existing
legacy/B12/B14 limits remain unchanged. Reference images remain the existing
B12 form, not a new image/reference validator; shared-image compression and
larger closures are not necessary to prove this bounded chain and stay separate.

ProjectTransitiveDeclaredExceptions, signing, XML contracts, unsupported
reference forms, Historical Reporting Import, CLI/MSBuild/loading, new Roslyn
versions and analyzer/selection/routing changes remain explicitly out of scope.

## Implemented connected slice

The Main-only opt-in `SupportingProjects` composes independently original B14
handoffs. Each node is still projected by the existing B10-B14 path. Its typed
identity is the original canonical assembly plus original manifest module name
and MVID from the existing P4 receipt. Outgoing dependencies are explicit values
in original reference order: a candidate simple name is only an ambiguity filter;
the edge requires full B12 assembly identity, module name and MVID equality.
Paths never identify a project or an edge. All supplied projects must be reachable
from the exact target; duplicate identities/simple names, conflicting logical
paths, inconsistent outer/target source worlds, dangling edges, nested graphs and
cycles are rejected. Existing B14 local manifests and all B12/B13 limits/policies
still apply independently. Protocol v2 remains additive; absent Solution preserves
the original null-omitted envelope and limits.

The child runs the existing B12/B13/B14 reconstruction helper independently for
each project: exact submitted PE references/aliases, real compiler capability,
ordered source trees and actual assembly/module identity postconditions. There is
no recompilation/emit of dependencies, no replacement of PE images with guessed
CompilationReferences, and no duplicate PE/PDB/source validator. The Historical
host alone owns all reconstructed compilations and stable exact-tree models,
ordered by assembly name. Calls must bind to an actual reference of an owned
binding compilation before the unchanged shared CrossCompilationResolver maps
them to the exact supporting source scope. Foreign trees/binding compilations
remain rejected. The shared SummarySession/registrar/dispatch/graph evaluator
are unchanged.

The single-compilation host retains B14's source-only, explicit-declaration
resolver behavior. PE-to-source rebinding and implicit supporting members are
enabled only when the explicit accepted graph supplies multiple owned scopes;
the closure is not an incidental widening of the legacy local profile.

Response provenance binds the entire graph, ownership, ordered references,
identity, sources and parse contexts in ReferenceWorldSha256. Descriptive reference
names remain excluded as in B12; images are represented by their existing hashes.
Returned paths are checked against the particular owning node's source lines;
exception identity must belong to a supplied project or its exact reference world.
Canonical IR still intentionally has no emitted source-module MVID: original PE
MVIDs are bound in graph/reference provenance, not invented for source symbols.

## Actual differential and failure witnesses

Three positive original Historical emissions use the pinned compiler in an
isolated test process and pass unchanged P4/P5A/P5G/P5I/P5J/P5C/P5E factories:
two projects A -> B; three projects A -> B -> C; and two projects throwing a
source-declared LocalException in B. Actual automatic detector projection/B7/B6/
B5/B4 worker dispatch runs twice. Full canonical results (entries, path steps,
sources/provenance/evidence, uncertainties), every canonical summary and graph
count, stable source/project ownership and exact reference-bound edges match
native Current over the same external source registry, not merely finding counts.
The isolated graph probe invokes the actual Worker reconstruction and actual
Historical host, not an alternate reconstruction implementation.

Full native Current Findings and their deterministic repetition are recorded.
Historical Finding/reporting import is explicitly excluded: therefore no
cross-engine Finding import equivalence is claimed. The meaningful cross-engine
comparison here is the complete canonical flow/graph contract feeding future
reporting work, exactly as in B14.

For the same three-project source world, Direct sees no helper throw and its
unchanged local worker result matches Current. ProjectTransitive sees no B/C
source and differs from the full solution; its incomplete local worker result is
typed AnalysisFailure, not an empty successful solution. Neither mode accepts
the explicit solution graph. Native Workspace also rejects circular
ProjectReference construction (actual test), while this bounded graph rejects
cycles rather than inventing cycle semantics.

Nineteen structural counterexamples reject before process start: missing project,
unknown target/project, duplicate/conflicting identities, conflicting source
ownership, dangling/missing edges, missing cross-reference, wrong MVID or full
assembly version, unreachable node, cycle, nested graph, outer root mismatch,
missing node references, declared/project mode and project count budget.
Original-project tests also reject copied P5J sets, unvalidated P5A descriptors,
missing P5E/P5J and nested/empty Main composition. An unsupported language in B
reaches the actual pinned Historical compiler and returns UnsupportedContext,
without downgrade or Current fallback. Forged graph-world provenance and a
foreign helper path are MalformedResponse with no imported result.

A separate real missing-C witness retains C's validated PE reference in B but
omits C's source-project receipt. Projection treats it as external metadata,
not an inferred source project. The reachable C call then produces the existing
typed AnalysisFailure with no partial/smaller-scope result and no Current route.
Thus a binary reference is not a promise of available source; only explicit
graph source ownership is transported. The raw dangling-edge negative additionally
proves that an explicitly declared project dependency cannot lose its endpoint.

## Deliberately open B16+ families / readiness

Proven: bounded unsigned original external PE-backed project DAGs for the tested
two-/three-project SolutionTransitive chains. This is not a blanket equivalence
claim for every native Roslyn Solution shape. Larger/branching/cyclic closures,
shared logical paths across linked projects, richer root selectors, automatic
source acquisition, native Workspace/CompilationReference adoption and arbitrary
MSBuild solution loading require separate bounded follow-ups. The four-project
cap is a ceiling, not proof that every four-project reference world fits the
independent aggregate image/request budgets. Existing per-source B14 contexts
remain supported; B15 does not broaden parse/compiler policies.

ProjectTransitiveDeclaredExceptions remains unsupported, with B14's genuine
guard/reporting-scope difference preserved. Signing, XML contracts, additional
reference forms, Historical Reporting Import, CLI adoption, additional Roslyn
versions, plugins and Dapper redesign remain out of scope. No external Dapper
pipeline advancement is claimed. B5 routing, B6 selection and all 121 shared
Analyzer files remain unchanged.

## Validation and attempts

Fresh unmodified HEAD export had no artifacts/bin/obj and passed all 379 parent
Boundary tests and Dual-Version build; its eight complete Current JSON baselines
and actual B14 cross-project gap witness are retained. Builds run only in that
export with exact owned overlays: protected original Core bin/obj files are never
built or treated as product changes.

The first new gate failed at the new test project's public Theory parameter
(internal ExceptionAnalysisMode, CS0051). The next incremental compile exposed
new-test nullable annotations and attempting record-with on the existing immutable
CanonicalAssemblyIdentity (CS8858/CS0200). Only new test code was corrected, using
string Theory parameters and the existing identity constructor. The next WAE
compile had zero warnings/errors; the first runtime B15 attempt passed 27/27.
A real missing-reachable-source case was then added (28 total), structural
negatives strengthened, and copied P5A provenance rejection added to the existing
receipt test. The intermediate gate passed 407/407. These attempts are retained;
no old test/flake was repaired or excluded and no failed runtime case was retried
unchanged. Final regression/build/evidence census is recorded below after execution.

The first full regression run also passed every group (including 3,009/3,009).
Final review strengthened native ownership observations to enumerate actual
GetAnalysisScopes and verify stable exact-tree semantic models; local modes now
assert exactly one owned project. The host's old one-compilation resolver domain
was explicitly preserved. Intermediate passed gates and the first complete
regression/evidence set are retained before the final build/regression repetition.
These are reviewed proof-strengthening/profile-preservation changes, not repairs
to old tests, routing or shared Analyzer semantics.

Self Analysis 16 and Canonical Finding Diff 0 added / 0 removed / 0 changed
evidence are inherited from B14/B13/B11/B10, not newly run in B15.

## Changed files and final closure

Production: Main payload input/projection plus the new solution composition;
neutral WorkerProtocol and request cap; Worker reconstruction; compile-local
Historical host; Main response validation. Test-only: new B15 scope tests and
isolated B15 probe, optional supporting-compilation observation in the existing
B14 probe, fixture entry point. Gate: additive permanent B15 test filter. Evidence:
this report, audit JSON, two B15 Proof scripts and a narrow BND-P5O2B-005 update.
Original B10-B14 test files and all routing/selection/version files are unchanged.

Final validation uses the exact final owned inputs. All final groups passed,
with zero skipped tests:

| Group | Passed / total | Failed / skipped |
| --- | --- | --- |
| New B15 | 28 / 28 | 0 / 0 |
| Unchanged B14 | 31 / 31 | 0 / 0 |
| Unchanged B13 | 34 / 34 | 0 / 0 |
| Unchanged B12 | 29 / 29 | 0 / 0 |
| Unchanged B11 | 16 / 16 | 0 / 0 |
| Unchanged B10 | 34 / 34 | 0 / 0 |
| Unchanged B5-B9 | 156 / 156 | 0 / 0 |
| Productive | 681 / 681 | 0 / 0 |
| Focused | 617 / 617 | 0 / 0 |
| Broad | 2,918 / 2,918 | 0 / 0 |
| Full final unchanged repetition | 3,009 / 3,009 | 0 / 0 |
| Permanent Boundary | 407 / 407 | 0 / 0 |

Final full attempt 1 aborted with 277 completed/passed tests and no failed
assertions: native AccessViolationException in unchanged
ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed.
ReadAssemblyFiles returns reader-backed AssemblyFile structs after disposing its
PEReader, then the caller reads ContainsMetadata. The crash stack points directly
to that old helper/caller; its initial fingerprint is unchanged. Broad and the
first full run had passed it. No old test or production fix was made, no cases
were excluded and no settings/build inputs changed for the explicit full repeat
(--no-build --no-restore --blame), which passed 3,009/3,009.
Aborted TRX, full log and blame Sequence XML are retained under
artifacts/b15-d7251/attempt-final-full-aborted. No crash dump was requested or
claimed: --blame retained the sequence. The hard test-host crash orphaned one
test-only --b13-emit fixture waiting for stdin, also holding the runner's pipe.
Its exact executable/operation/PID/creation time and missing parent were checked;
only that own orphan was stopped, with verified exit. A first Stop-Process
-InputObject attempt failed with NullReferenceException; rechecked -Id -Force
cleanup succeeded. No unrelated process/file was removed or changed.

Current/Historical/Worker Dual-Version and all eleven WAE build steps: PASS,
zero warnings/errors. Format, strict JSON, UTF8 without BOM / CRLF, PowerShell
syntax, git diff --check, unchanged 121 shared analyzer files, permanent
architecture/isolation gate and evidence verifier: PASS. All eight fresh complete
Current JSON baselines are byteequal. Prior B14 regression identities remain
present in every corresponding final group. Tracked paths have no case collisions;
the validation is a clean Git export on Windows, not a claimed Linux CI run.

## Requested 19-point closure index

1. B15 submatrix: the twelve-row matrix above was written before production edits.
2. Cross-project gaps: B14 lacked a typed target/project graph, exact edges and
   multi-compilation source scope, method rebinding and returned source-world validation.
3. Closed: original external bounded DAG transport, exact reference-bound edges,
   per-project reconstruction/ownership, PE-to-source rebinding and tested full flow/graph parity.
4. Open: larger/general solution families, shared logical paths, richer roots/options,
   acquisition/loading, signing/XML/unsupported references and Historical reporting import.
5. SolutionTransitive: fully equal in the evidenced two-/three-project external slices;
   not a claim for all native Workspace solutions. Scope stops here.
6. ProjectTransitive: exactly one Current owned project, no B/C flow; graph rejected
   before dispatch, incomplete local Historical analysis retained as AnalysisFailure.
7. Direct: exactly one Current owned project, no helper throw; unchanged local result
   equal, explicit solution graph rejected before dispatch.
8. Protocol/payload/worker: additive protocol-v2 graph and Main-only original-project
   composition, existing reconstruction per node, compile-local multi-scope host,
   graph-bound provenance and per-owning-source/result identity validation.
9. Current parity: eight complete byteequal Finding/flow/graph fixtures, native full
   Finding repetition and source/model ownership assertions; no native Current Worker.
10. Historical parity: genuine repeated pinned worker route; every result entry,
    path/source/uncertainty, summary, graph count, owner and exact dependency compared.
    Historical Finding import explicitly not implemented/claimed.
11. Failure: nineteen pre-start typed structural negatives plus original receipt,
    actual unavailable language/missing source and forged response rejection;
    no local/Project/Direct/Current fallback or partial success.
12. Isolation/cleanup: Historical assemblies only in child; actual compiler MVID/SHA
    identities, no source execution, normal request processes exited. Native-crash
    test-fixture cleanup is separately retained and verified.
13. Changed files: seven production files, one gate, four test/fixture files and
    five evidence/boundary files listed below; no shared Analyzer or B5/B6/version edit.
14. Tests/builds: final census above; eleven WAE steps 0/0 and genuine Dual-Version
    PASS. Compile attempts/native abort/unchanged repeat are not hidden.
15. Architecture/isolation/evidence: permanent 407-test gate and evidence verifier
    PASS, all 121 shared files equal, exact export/workspace owned inputs equal.
16. Self Analysis / Canonical Diff: 16 and 0/0/0 inherited, not newly executed.
17. Next: independently bounded B16+ broader solution/adoption families; a separate
    ProjectTransitiveDeclaredExceptions detector/reporting contract. Old reader-lifetime
    reliability is separate work, not opportunistically repaired in B15.
18. Final git status: exact snapshot below and in the machine-readable audit. Root
    ignore WIP is preserved, original Core 23 and six stashes unchanged, index empty;
    no commit, push, stash operation or new bin/obj product change.
19. External HEAD: B14 separately committed before B15 to
    d7251cf4e450aab2458f8f91b12b411fa6a39a4d; audited at entry and final exit,
    no HEAD movement during B15.

## Exact owned file list (17)

- src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
- src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerSolutionProjection.cs
- src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
- src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
- src/XMLDocNormalizer.HistoricalWorker/Program.cs
- src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
- src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs
- build/Verify-DualVersionBuild.ps1
- Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs
- Evaluation/P5O2B7Proof/HistoricalPdbFixture/Program.cs
- Evaluation/P5O2B7Proof/HistoricalPdbFixture/B14ScopeProbe.cs
- Evaluation/P5O2B7Proof/HistoricalPdbFixture/B15SolutionProbe.cs
- Evaluation/OPEN-PIPELINE-BOUNDARIES.md (only the evidenced BND-P5O2B-005 slice)
- Evaluation/P5O2B15-historical-cross-project-solution-scope-equivalence.md
- Evaluation/P5O2B15-historical-cross-project-solution-scope-equivalence-audit.json
- Evaluation/P5O2B15Proof/Run-Regressions.ps1
- Evaluation/P5O2B15Proof/Write-Verify-SolutionEvidence.ps1

## Final git status --short

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Evaluation/P5O2B7Proof/HistoricalPdbFixture/B14ScopeProbe.cs
 M Evaluation/P5O2B7Proof/HistoricalPdbFixture/Program.cs
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/Program.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
?? Evaluation/P5O2B15-historical-cross-project-solution-scope-equivalence-audit.json
?? Evaluation/P5O2B15-historical-cross-project-solution-scope-equivalence.md
?? Evaluation/P5O2B15Proof/
?? Evaluation/P5O2B7Proof/HistoricalPdbFixture/B15SolutionProbe.cs
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerSolutionProjection.cs
```
