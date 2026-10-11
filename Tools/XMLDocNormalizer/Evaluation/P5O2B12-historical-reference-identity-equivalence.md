# P5O2B12 - Historical Reference and Identity Equivalence

Starting HEAD: `bb204a89466f56d730167ffbb9b1e75faeb84415` (separately committed B11).
Initial working tree: protected root `.gitignore` only, empty index, six stashes,
23 protected Core files. This submatrix and decision precede B12 code changes.

## Reference/identity submatrix extracted from the B11 36-row matrix

These are the reference/identity rows of B11, not a replacement whole-input matrix.

| B11 input / identity information | Current representation | Historical before B12 | Semantic relevance | Trust boundary | Neutral transport | Concrete B12 action | Equivalence status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Assembly/project name and module name | P4 target / P5K original identity and source module | Synthetic HistoricalWorkerInput | Source exception canonical identity; friend binding | Original P4 receipt | Exact assembly name; reconstructed source identity postcondition | Bind original unsigned name on explicit validated-reference profile | Closed bounded unsigned profile; unsupported module-name shapes rejected |
| Source assembly version/culture/attributes | Exact source attributes / P4 full identity | Attributes parsed, name substituted | Exception assembly identity and binding | Same P4/P5I receipts | Existing canonical identity DTO | Compare reconstructed complete identity with expected target, no attribute rewriting | Closed bounded unsigned single-module profile |
| Signing/public key, friend access | P4 signing / P5G CryptoPublicKey | Unsigned only | Friend binding and canonical identities | P4/P5G | Public identity is neutral, private key unnecessary | Keep target signing restriction: depends on B13 compilation options; do not enable signing | Open for signed targets |
| Ordered PE metadata reference images, MVID/hash | P5C immutable material / P5E ordered set / P5K | Seven Worker runtime images instead | Entire binding world and canonical module provenance | Original successful P5C and P5E association with same P5A | Exact bytes, SHA256, module name/MVID, assembly identity | Add explicit exact-image profile, preserve all ordinals, no ambient references | Closed within count/byte/properties bounds |
| Reference properties: assembly/module kind, aliases, EmbedInteropTypes | P5A properties / P5E references | Default assembly properties | Alias binding, embedded interop / target modules | Same ordered P5A association | Strings/boolean/kind | Preserve aliases; fail closed on modules/embedded interop, no B13 module expansion | Assembly aliases closed; other shapes open |
| Referenced assembly type/member/inheritance/constant/attribute metadata | PE-backed semantic symbols | Framework only | External exception types, inheritance, constants and identity | P5C exact retained images | Exact PE bytes, not Roslyn objects | Reconstruct only Historical MetadataReference in child | Closed for accepted PE reference profile, not source-less call completeness |
| DocumentationProvider / XML sidecar exception contracts | Main external documentation model/provider and optional sidecar IO | No external host registry | Source-less call contracts and uncertainty | No existing validated XML receipt associated with P5E | XML content could be neutral but lacks present handoff | Leave open: images do not attest XML providers; supporting source scopes separately need B13; no guessed sidecar reads | Open independent XML contract handoff; no external call success claim |
| Framework/CoreLib identity and runtime API surface | Original ordered references | net8-runtime-bounded-v1 ambient seven | Well-known type binding and exception identity | P5C/P5E original images | Exact images, same as other references | New profile never appends Worker runtime references; legacy policy unchanged | Closed exact-image profile within bounds, not legacy original-target claim |
| AssemblyIdentityComparer / portability policy | P5G default comparer; rejects unsupported policy | Default comparer | Identity unification | Existing P5G validation | Already same supported policy | No option or comparer change | Equivalent accepted profile only |
| PE/PDB target/compiler provenance and trust receipts | P4/P5A target / P5C material / P5E set | Original target retained Main-local, references absent | Trust and engine selection | Original factory receipts, not copied descriptors | Main validates association; DTO conveys binding information only | Retain successful associations without another parser or validator | Closed original reference handoff; selection unchanged |

## Pre-implementation scope decision

B11 already executed an external-exception witness: genuine emitted dependency
binds in Current, framework-only Worker returns CompilationFailure. Existing P5C
has exact immutable bytes and P5B identity/MVID; P5E retains only Roslyn references.
Retain successful P5C/P5E associations for a new optional typed reference-set
handoff. Copies/raw objects cannot mint a receipt. No second PE/PDB parser.

Preserve legacy B10/B11 explicitly bounded requests unchanged. Only the explicit
validated-reference handoff selects the new reference profile; this is payload
representation, not Current/Historical engine selection. Transport the complete
ordered set, not an external-image append to guessed framework references. Bound
total bytes/count and explicitly extend request budget for this profile. Worker
reconstructs metadata, checks actual bound full identities and module names/MVIDs
against descriptions, and retains existing compilation-error/incomplete-result
fail-closed policy. SHA256 binds IPC bytes; it is not a new original-build validator.

Target name/full unsigned identity accompanies this profile. Unsupported signing,
netmodules, embedded interop, XML contracts and supporting/project source scopes
remain explicit boundaries; they must not pull B13 into B12. Complete Historical
Finding import is excluded by the prompt: compare full canonical flows, sources,
uncertainties/provenance and isolated graph observation; separately preserve the
eight complete Current Finding/graph fixtures. Do not claim cross-engine Finding
import or full external-input equivalence.

## Implemented reference/identity slice

Protocol remains v2. The existing reserved reference-image DTO is extended
additively with the existing neutral canonical assembly identity, module name/MVID,
ordered aliases, explicit assembly kind and EmbedInteropTypes flag. Context adds
the expected target identity/module name. New `validated-pe-images-v1` is an
explicit bounded representation, not a compiler version or engine selection rule.
Legacy `net8-runtime-bounded-v1` inputs still require null references/target fields
and the original synthetic name; all B10/B11 tests stay unchanged.

The production input has one optional typed `ExternalMetadataReferenceSet`.
P5C retains original successful-material receipts; P5E retains the exact materials
actually used at each successful reconstruction ordinal, associated with the
same P5A descriptor. P5E acceptance/validation algorithms remain unchanged:
caller-created material copies may still pass existing structural P5E tests but
cannot acquire the new transport receipt. Copied reference sets, wrong provenance,
missing/extra materials and forged materials cannot mint an original handoff.
No second PE/PDB parser, source decoder, validator or IO is introduced in Main.

The reference projector reads those immutable bytes and already validated P5B
assembly/module identities. P4 supplies the original target identity/module.
SHA256 covers exact IPC image bytes. Ordered descriptors plus target identity,
module and fixed policies are bound by `ReferenceWorldSha256` in returned
provenance. Lookup names are descriptive provenance only and are deliberately
excluded from that semantic reference-world hash. No file path is transported
as a path to open; no filename-to-assembly binding heuristic is used.

The Worker uses only the submitted complete reference sequence for the exact
profile. It does not append its seven runtime images, Analyzer, Worker or Roslyn
metadata. MetadataReference.CreateFromImage belongs exclusively to the child;
actual Historical assembly symbol identities and metadata module names/MVIDs
must equal the descriptions at every ordinal. Reconstructed source assembly full
identity/module name must equal the expected target. Nothing rewrites source
attributes, injects public keys or changes compilation/parse options.

The existing canonical adapter intentionally reports null module MVIDs even for
PE-backed flow symbols. B12 does not change this Analyzer/IR semantics. Actual
reference MVIDs are checked during Historical binding and included in the
reference-world hash; returned canonical exception assembly/module identity must
belong to the requested world and retain the existing null-MVID representation.
Forged assembly versions or non-null MVIDs in response flows are rejected.

Bounds: at most 64 references and 32 MiB total image bytes; exact-profile JSON at
most 48 MiB characters. Legacy request budget remains 65,536 characters; existing
source/path/node/response/deadline budgets remain unchanged. Source/selector,
supporting compilations, parse/compiler options and scope profiles do not expand.
Strict unknown/duplicate fields and canonical exact-profile roundtrip checks
reject incomplete/contradictory input rather than downgrade to a legacy request.

## Differential, failure and isolation evidence

The unmodified committed B11 export genuinely executed nine baseline cases:
eight complete Current Finding/graph fixtures and B11's real external-reference
loss witness. A Current compilation with an emitted ExternalException dependency
binds that type while the old framework-only Worker yields CompilationFailure.
The actual before JSON, TRX and parent build audit are retained.

New positive cases use real PE images, not mock identities:

- Two different versions of the same dependency, under identical lookup names,
  produce distinct expected canonical exception identities. Each whole result
  equals Current. Renaming every lookup to the same nonexistent path changes
  neither the result nor reference-world hash. Identity comes from PE metadata.
- The existing isolated Historical fixture emits genuine source/PE/PDB through
  the exact Historical compiler. Original P4/P5A/P5G/P5H/P5I and P5C/P5E factories
  validate these inputs. The actual production detector/dispatch selects and runs
  the genuine Historical Worker twice with the complete original references.
- A local target exception retains `Original.Local.Target`, version `3.4.5.6`
  and source module `Original.Local.Target.dll`; no synthetic-name substitution.
- An extern alias is retained. Reversing the reference sequence preserves the
  tested full flow but changes the ordered reference-world hash. B12 transports
  the order, not an assertion that every future reordering is harmless.

Full equality covers every exception identity, evidence kind, path/source step,
truncation flag and uncertainty. Every positive production result repeats across
fresh exited processes. The isolated diagnostic fixture additionally observes
every actual graph node through the existing TryCreateSummary adapter: full
callable/context, executable-body status, local sources, outgoing edges, caught
types and uncertainties. All summaries equal Current and repeat unchanged,
not just graph counts. No graph or Findings import is added to production IPC.

Missing ExternalException gives the same CS0246 compiler-error family in Current
and Historical. Current's structural reconstruction may retain diagnostics while
Worker still rejects compilation errors under its pre-existing completeness
contract; this is not a claim of equal unrestricted success domains. No successful
or empty result and no Current fallback is manufactured.

Contradictory full assembly version/key, target version/culture/module, reference
module name/MVID fail against actual Historical metadata with CompilationFailure.
Missing images, hash mismatch, duplicate/conflicting simple assembly identities,
bad aliases, unknown profile, inconsistent target name, signing, module/embedded
interop shapes and count/byte overflow fail UnsupportedInput before process start.
Valid Current duplicate/unification configurations are not silently approximated:
this narrower profile rejects repeated case-insensitive simple identities,
including different-version aliases. Unknown/incomplete exact fields are rejected.
Forged response reference-world hash/assembly/MVID is MalformedResponse, no result.

Loaded Historical compiler hash/MVID matches the dual-version runtime audit.
No Historical engine assembly enters Main; native Current never starts Worker.
Processes are awaited/reaped, with no fallback and unchanged propagation of
deadline/start/cancellation/crash/structured failures through the existing layers.

## Deliberately open and handoff to B13

B12 is a bounded Reference/Identity closure, not full external input equivalence.
Legacy automatic input with no reference-set handoff retains its original
published Worker policy; it does not now claim original-reference fidelity.
Callers supplying the original P5E set obtain the exact profile. CLI/acquisition
and repository-wide call-site adoption are explicitly not performed.

Signed targets remain rejected by the unchanged B10 profile: semantic public-key
reconstruction requires the P5G CryptoPublicKey compilation-option family, which
B13 must handle with original signing provenance and no private-key transfer.
Module targets, embedded interop and multi-module/source configuration remain
unsupported and are not flattened into assemblies. Nondefault identity comparer
and portability policies remain rejected upstream by existing P5G.

XML DocumentationProvider/sidecar contracts are an independent open reference
handoff: P5E has no validated XML snapshot/provider association to transport.
B12 does not guess/read sidecars or invent trust receipts. Metadata alone cannot
prove complete source-less external calls; existing uncertainty still fails closed.
External supporting source/project scopes separately require B13. XML closure is
not claimed or mislabeled as solved merely because PE bytes are transported.

B13 must address the already inventoried complete ordered source set, parse/
language/defines/nullable/compiler options, generated output, project/solution/
supporting scopes and exact root bindings. RuntimeAwait/compiler capability and
completeness limitations remain real. Historical Reporting Import, full
cross-engine Findings, CLI/discovery and other call-sites remain separate work.

## Validation and reproducibility

Final validation census is recorded by the verifier, including TRX ResultSummary
being Completed (counters alone cannot hide a native host abort). Groups overlap.
All builds/tests run in `artifacts/b12-bb204a/Tools/XMLDocNormalizer`, a clean Git
archive plus only owned source overlay, not the original protected Core outputs.
The fresh parent export starts without artifacts or bin/obj. Validation is local
Windows; no Linux/remote execution is claimed.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
# Run the build above in the recorded isolated export, not the protected original workspace.
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B12Proof/Run-Regressions.ps1 -ValidationRoot <isolated-tool-root>
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B12Proof/Write-Verify-ReferenceEvidence.ps1 -ValidationRoot <isolated-tool-root>
```

RegressionFilters.json retains B11's executed test classes, derived from the
actual B11 TRX. Class-level selection is broader than B11's partially method-level
filters: productive also runs five further existing graph-integration cases;
focused also runs 125 further existing cases, plus B12's 29 cases. Focused/broad
add B12. The verifier checks every original B11 test-case name is still present
and records all added cases, rather than pretending these are identical filters.
No skipped test, error suppression, settings change or semantic fallback.
The verifier binds built source fingerprints, all 121 shared Analyzer sources,
one unchanged Historical host, parent/final audits, full Current/canonical/graph
JSON, format, strict UTF8/CRLF, JSON and PowerShell syntax, git diff --check,
23 Core files, root ignore bytes, six stash identities and unchanged index.

Self Analysis 16 and Canonical Finding Diff 0/0/0 are inherited B11/B10 values,
not newly executed B12 self-analysis or whole-tool canonical finding comparisons.
The fresh complete Current baseline comparisons and B12 full-flow differentials
are explicitly narrower new measurements.

## Retained development failures and retries

No failure is relabeled as a PASS:

1. First final-gate attempt failed Worker compilation with four CS1503/CS1061/
   CS0021 errors in new B12 code (ImmutableArray aliases; enumerable modules).
   Corrected only new reconstruction code; retained first log and gate directory.
2. Second attempt failed the new test-project build with four missing Detector
   namespace references and two nullable warnings promoted to errors. Corrected
   new test imports/nullability; retained second log/gate directory. No suppression.
3. Third attempt built successfully but boundary failed 17/314 (297 passed):
   own fixture's TPA list duplicated one physical runtime image; original PDB
   recorded seven references but the submitted fixture had eight. Existing
   duplicate-simple-identity rejection correctly rejected the request. Corrected
   only new fixture acquisition to use the existing explicit framework profile's
   distinct-path rule; never deduplicated original P5E references in production.
4. Fourth attempt failed 7/314 (307 passed) because new response validation
   incorrectly demanded emitted MVIDs in canonical flow IR. The unchanged
   canonical factory deliberately produces null MVIDs. Adjusted the new
   validation to that existing representation; actual Worker PE MVID checks and
   reference-world hash binding remain mandatory. No Analyzer/IR change.
5. Fifth complete dual-version/boundary run passed 314/314. Four earlier attempts
   remain in `attempt-1-dual` through `attempt-4-dual` and distinct build logs;
   logs/TRX are not overwritten or concealed by the successful final evidence.

Two non-executing tool attempts (JavaScript syntax and a formatted patch context)
were retried before they could mutate files. No recovery/reset/index/stash action.
Known B11 clrjit.dll 0xc0000409 evidence and unresolved cause remain unchanged;
no opportunistic flake repair. Actual B12 sweep failures/retries, if any, are
separately recorded by the final verifier rather than inherited as new events.

## Changed files and protection

Production changes are confined to:

- WorkerProtocol.cs: additive exact-reference DTO/profile/bounds/hash/strict input.
- Historical Worker Program.cs and HistoricalAnalysis.cs: bounded strict parsing,
  submitted reference reconstruction and actual identity postconditions.
- HistoricalWorkerPayloadProjection.cs and new HistoricalWorkerReferenceProjection.cs:
  optional typed original P5E handoff, not raw input or engine choice.
- HistoricalWorkerClient.cs and HistoricalWorkerResponseValidation.cs: profile
  request budget, runtime/provenance and canonical reference-world binding.
- ValidatedExternalMetadataReferenceMaterialFactory.cs and
  ExternalMetadataReferenceSetFactory.cs: original successful receipt retention only.

Test/evidence changes: new ExceptionFlowHistoricalReferenceIdentityTests.cs;
existing isolated HistoricalPdbFixture Program.cs/B11GraphProbe.cs plus new
B12ReferenceProbe.cs; permanent Verify-DualVersionBuild.ps1 filter addition;
this report/JSON, P5O2B12Proof and BND-P5O2B-005's actual bounded closure.
No B10/B11 test, project, version, native Current detector, routing, Analyzer
shared source, Historical semantic host, CLI, Reporting or Dapper file is changed.

No commit, push, staging or stash action. Root `.gitignore` is inherited WIP, not
a B12 change. Original Core's 23 files, six stashes and empty index are protected
by byte/identity checks. The separately committed B11 HEAD movement is audited;
no further HEAD movement occurred during B12. Generated bin/obj/artifacts are
not product changes. Stop after this reference/unsigned-identity slice.

## Actual final results

| Validation | Passed |
| --- | --- |
| Unmodified B11 parent: eight complete Current cases + actual reference-gap witness | 9/9 |
| New B12 | 29/29 |
| B11 unchanged | 16/16 |
| B10 unchanged | 34/34 |
| B5-B9 unchanged | 156/156 |
| Productive class superset, no original case omitted | 681/681 |
| Focused class superset + B12, no original case omitted | 524/524 |
| Broad + B12, no original case omitted | 2825/2825 |
| Full configured suite | 2916/2916 |
| Permanent boundary gate | 314/314 |

Final sweeps have Completed summaries, zero failed/skipped tests and no native
host abort or test retry. The four failed development gates above are retained
separately and corrected by only owned B12 code/fixture changes. Current/Historical/
Worker WAE and full dual-version gate PASS, zero warnings/errors: six forced
library builds, two Worker builds, adversarial process, real Historical fixture
and test project with actual project references. Determinism and exact package/
runtime identity are unchanged. All eight full Current JSON baselines are equal.

Format, JSON, strict UTF8/CRLF, PowerShell syntax, architecture/isolation, evidence
and protection verifier PASS. The harmless Git LF-to-CRLF advisory for inherited
root `.gitignore` is retained in the verifier log; the file's bytes are unchanged.
No actual self-analysis or whole-tool canonical finding diff was run (inherited
16 and 0/0/0 only). No extra HEAD movement, external edit, index or stash change.

Final `git status --short` (root ignore is inherited protected WIP):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Evaluation/P5O2B7Proof/HistoricalPdbFixture/B11GraphProbe.cs
 M Evaluation/P5O2B7Proof/HistoricalPdbFixture/Program.cs
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs
 M src/XMLDocNormalizer.HistoricalWorker/Program.cs
 M src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs
 M src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
 M src/XMLDocNormalizer/Execution/Semantic/ExternalMetadataReferenceSetFactory.cs
 M src/XMLDocNormalizer/Execution/Semantic/ValidatedExternalMetadataReferenceMaterialFactory.cs
?? Evaluation/P5O2B12-historical-reference-identity-equivalence-audit.json
?? Evaluation/P5O2B12-historical-reference-identity-equivalence.md
?? Evaluation/P5O2B12Proof/
?? Evaluation/P5O2B7Proof/HistoricalPdbFixture/B12ReferenceProbe.cs
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerReferenceProjection.cs
```
