# P5O2B7 - Trusted Selection Context Projection

Date: 2026-10-09. Starting HEAD `35dbd5b2150e4f4591b1eead23511cd6f8444108`
(`Add deterministic analyzer selection policy`). B6 separately committed.
Initial mandatory status/stat/full diff/HEAD/log audit: only protected root
` M ../../.gitignore`; no Core WIP at B7 start. Fresh 22-file Core, six-stash,
Main/Worker/source/project/manifest snapshots precede production changes.

## Investigation and design recorded before implementation

Current project/session hosts already own genuine Current CSharpCompilation
instances in ProjectClosureSemanticContext/SemanticCompilationScope. Such an
instance belongs to the Main's statically referenced Current Roslyn universe.
Its already loaded runtime type's AssemblyInformationalVersionAttribute provides
the actual compiler build; project/assembly names or language/framework versions
are not compiler identity. Projection will read this metadata only, never create
a Compilation, inspect diagnostics, analyze source, discover/load another Roslyn,
or infer the original compiler of a reconstructed external compilation.

External input already passes through ExternalAssemblyReferenceDescriptorFactory
and ExternalPeDebugDirectoryDescriptorFactory (P4A target PE), then
ExternalCompilationProvenanceDescriptorFactory (P4B target-bound PDB validation
and P5A CDI parsing from the same immutable image). The existing options supply
schema, language and exact compiler-version. They contain all selector values
B6 needs. Missing options/compiler-version remain missing, not Current defaults.
PE/PDB matching/checksums, CDI uniqueness/UTF8 parsing and options extraction must
remain owned by these existing validators; no parallel parser/reader is needed.

The missing trust fact is **who created a descriptor**, not another version field.
B6's public positional context construction/init properties accept arbitrary
strings, and an internal manually constructed descriptor currently resembles a
factory-validated one. B7 will close both routes:

- Context construction becomes private with get-only evidence properties. Its
  nested, Main-local Projector is the sole successful construction owner.
- Existing PDB factory records its successfully validated descriptor by reference
  in a private ConditionalWeakTable. A read-only receipt predicate identifies only
  actual successful validator outputs, without re-reading/re-validating inputs or
  retaining descriptors strongly. Manually assembled/copied descriptors get no
  receipt. Receipt production stays inside the existing factory's success path.
- Projector accepts typed CSharpCompilation or existing receipt-bearing PDB
  provenance, never a compiler-version string. Exactly one source is required;
  two sources fail as conflicting/ambiguous even when versions agree, rather
  than silently preferring an origin. No source/default means typed failure.
- Current projects actual loaded compiler metadata with CurrentCompilation origin.
  PDB projects the exact stored version with ValidatedPortablePdb origin, requiring
  existing schema 2 / language C#. No version allowlist or engine selection lives
  in projection; valid provenance with an unsupported version still projects, then
  B6 rejects it. Whitespace/casing/suffixes are not normalized.

The trust root remains the caller's already validated expected target PE debug
descriptor, as in P4A/P4B. A receipt proves this existing validation ran for that
descriptor; it does not add artifact discovery, prove every external compilation
input/equivalence, or sandbox hostile reflection inside Main. Neither enum labels
nor a plain compiler string authenticate external data.

B6's exact SelectionPolicy and B5's Router remain byte-identical. The old B6
FromValidatedPortablePdb convenience entry will delegate to the same projector,
returning only an uninitialized/non-executable context on failure, never bypassing
the receipt check. Existing B6 policy-only fuzz fixtures will use an explicitly
test-local reflection helper because arbitrary production context construction is
being intentionally closed; this is not a new production raw-string backdoor.

Architecture: typed input/provenance -> projection -> immutable context -> existing
B6 policy -> existing B5 router -> existing Current session / B4 isolated Worker.
No production call-site migration, CLI/reporting/Dapper integration or new Worker
contract is authorized. Completion evidence will follow actual validation.

## Closure: implemented abstraction, input and mapping

B7 is complete at the selection-context projection boundary only. The Main-local
`ExceptionFlowAnalyzerSelectionContext.Projector.Project` accepts
`ExceptionFlowAnalyzerSelectionProjectionInput`: an existing Current
`CSharpCompilation` **or** an existing `ExternalCompilationProvenanceDescriptor`.
Neither the input nor any successful public/internal context constructor accepts
a raw compiler-version string. The context is a sealed partial record with a
private constructor and get-only `Provenance` / `CompilerVersion` properties.
Copies cannot change those properties. The result carries either a context or a
typed projection failure, not a partial context or executable selection.

| Existing source | Required trust | Preserved mapping | Actual integrated B6 result |
| --- | --- | --- | --- |
| Native Main Current compilation | Actual typed instance in the already loaded Current universe | Loaded type assembly informational version; `CurrentCompilation` origin | Current |
| Current-emitted PE/PDB | Existing target-bound validation and original factory receipt | Exact schema-2 C# `compiler-version`; `ValidatedPortablePdb` origin | Current |
| Historical-runtime-emitted PE/PDB | Same existing target-bound validation and receipt | Exact schema-2 C# `compiler-version`; `ValidatedPortablePdb` origin | Historical |

Current is `5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09`.
Historical is `5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0`.
These are observed identities / unchanged B6 policy values, **not projector
allowlists**. Projection preserves unsupported nonempty validated versions too;
B6 alone rejects them by its unchanged exact ordinal rules. There is no range,
trim, case conversion, fallback or origin relabelling.

The only existing provenance-factory extension is nine added lines: import,
private weak receipt table/query, and recording the newly created descriptor on
the original successful validation path. An exact source comparison in the
proof verifier checks that existing PE/PDB/CDI validation/parser text is unchanged
apart from this insertion. No second parser, PDB discovery or new version field
was needed. Receipt ownership is by object reference, not record equality;
hand-built, copied or deserialized values cannot inherit it. The table does not
strongly retain descriptors. The P4A expected target debug descriptor remains
the caller's existing trust precondition; this does not authenticate arbitrary
caller-minted expected PE roots or defend against hostile in-process reflection.

The original `FromValidatedPortablePdb` convenience method delegates to this same
projector. Its compatibility failure value is `Unspecified` / null, which B6
rejects; it never bypasses validation or supplies a Current/Historical default.
Consumers needing the projection error use the typed `Project` result directly.

## Missing, invalid and conflicting inputs

- Null/no source: `MissingInput`.
- Both sources: `ConflictingInputs`, even with identical values; no silent preference.
- Descriptor without the original factory's receipt: `UnvalidatedProvenance`.
- Receipted descriptor with no compilation options: `MissingCompilationOptions`.
- Missing/unsupported schema or language: `UnsupportedCompilationOptions`.
- Missing/empty/whitespace PDB compiler-version: `MissingCompilerVersion`.
- Missing Current runtime identity metadata: `MissingCompilerIdentity`.

All direct projection failures have null context and a typed failure. Corrupted
or mismatched PDB images fail in the existing validators before projection.
Genuinely validated but unsupported versions project unchanged, then B6 returns
`Unspecified` and its own typed failure; the unchanged B5 router cannot execute
that selection. The Current metadata-absence branch is defensive: the genuine
installed Current runtime has identity metadata; no fake production compiler
object or metadata mutation was introduced just to force this branch.

## Separation from selection, routing and execution

The dependency direction remains:

```text
existing typed input / validated provenance
  -> trusted projection -> immutable context
  -> unchanged B6 SelectionPolicy -> unchanged B5 Router -> original execution
```

Projector never calls policy/router/client, reads files/PDBs, creates a compilation,
checks source diagnostics, analyzes, starts a process or loads another compiler.
The native Current test intentionally supplies invalid source: this boundary
projects compiler identity, not analysis readiness or semantic correctness.
Current-native reconstruction does not discover an external target's original
compiler. No Current/Historical defaults are introduced.

All 121 shared Analyzer sources, the B6 policy and the B5 router are byte-identical
to B7 start. Of 369 original physical Main sources, 367 are unchanged; the two
approved trust-boundary changes are context construction and factory receipt.
All 13 existing physical Worker/Historical boundary files remain unchanged.
No new Main Historical reference, protocol change, Analyzer copy or call-site
migration was made. The old 40 policy cases retain their expectations through a
private **test-only** reflection fixture helper, needed to keep invalid-context
fuzz coverage after closing raw production construction. The original 26 B5
routing cases are unchanged. This helper is not a production trust API.

## Real Historical PDB proof and corrected local attempts

The permanent dual-version gate now builds/runs a small isolated **test-only**
HistoricalPdbFixture before Main boundary tests. It uses the existing Historical
build configuration and reference, emits one fixed literal source via its actual
loaded Historical Roslyn engine, accepts no submitted source, and never executes
the emitted assembly. It is not a Worker endpoint, CLI integration or plugin.

Its Common/CSharp loaded informational identities equal the exact Historical
version above; their MVIDs are `dc7738cc-6dca-4d34-9c44-29b53a7caa93` and
`0f9c1dcf-4eb1-47f8-81b2-733db5887be7`. Their SHA256s match the already proven
Historical compiler images. The Main test reads the emitted PE only as metadata,
uses the existing assembly/PE/PDB factories, proves `IdentityAndChecksum`
validation and receipt, projects the recorded version, and observes B6 Historical.
Historical Roslyn is not CLR-loaded in the caller; Current MVID is unchanged.
Projection and selection do not execute analysis or start a Worker. Gate runtime
smoke tests separately exercise the existing execution boundary.

The first focused attempt was 93/95 with two incorrect **fixture assumptions**,
preserved in `artifacts/p5o2b7/tests/projection-first.trx`:

1. Different assembly names with identical source can produce identical PDB
   content. The mismatch fixture now uses different source, not a validator change.
2. The Historical Analyzer DLL's build PDB records its SDK compiler **4.11**, not
   the Historical **5.0** runtime the DLL subsequently loads. The real runtime
   emitter above replaced this invalid positive fixture. A separate regression
   preserves that SDK-build identity and proves B6 rejects it unchanged.

No production workaround or selection-rule relaxation addressed either failure.
The corrected focused run passed 96/96 (30 B7 + 40 B6 + 26 B5). The final full
suite passed on its first B7 full attempt; no retry/flake is concealed.
Synthetic schema/options negatives use validator-created PDB unit fixtures with
a supplied expected debug root; they are not presented as real PE checksum proofs.
The actual Current/Historical positive boundaries use real emitted PE/PDB images.

## Final validation results

| Fresh B7 validation | Result | Artifact |
| --- | --- | --- |
| New projection cases | 30/30 | `artifacts/p5o2b7/tests/projection-final.trx` |
| Existing PE/PDB/options/configuration regressions | 224/224 | `artifacts/p5o2b7/tests/provenance-regressions.trx` |
| Worker, architecture, RuntimeAwait, dependency cases | 239/239 | `artifacts/p5o2b7/tests/worker-architecture.trx` |
| Broad semantic/external/evaluation/Worker cases | 1983/1983 | `artifacts/p5o2b7/tests/broad.trx` |
| Full normal suite (configured SelfAnalysis excluded) | 2775/2775 | `artifacts/p5o2b7/tests/full-final.trx` |
| Expanded permanent Main boundary gate | 175/175 | `artifacts/dual-version-build/main-boundary.trx` |

Final suites have zero failed/skipped tests; these overlapping suites are not
additive unique-test counts. The original full-run TRX is retained as
`full-initial.trx`; `full-final.trx` is the same successful result, not another run.
Current/Historical six forced build images, two Worker builds, test processes,
test assembly and the new fixture all pass WAE with **0 warnings / 0 errors**.
The gate proves original package/reference universes, source boundary, isolated
runtime identity, process-only Historical execution and build determinism.
Standalone Current/test builds also passed 0/0. Core was not rebuilt.

The proof verifier checks build/TRX hashes, actual emission/projection identities,
source/protected fingerprints, PowerShell/XML/JSON syntax, strict UTF8/CRLF/final
newlines, folder-only whitespace verification and `git diff --check`. It avoids
MSBuild workspace formatting and protected Core build churn. The existing root
ignore LF/CRLF warning is not a B7 formatting change.

Self Analysis is **inherited, not newly executed**: 16 findings (DOC611: 1,
DOC631: 15; DOC610/DOC632: 0). Canonical Finding Diff is **inherited, not newly
executed**: added/removed/changed evidence **0/0/0**, raw and normalized arrays
equal. Source is the committed B6 audit, with its evidence lineage and SHA256
included in the B7 JSON. Analyzer/policy/router identity supports retaining this
baseline; B7 does not claim fresh self-analysis of new projector documentation.

## Changed files and reproducible evidence

Production: context and new `.Projector.cs` under `Execution/Analysis`, plus the
nine-line receipt extension in `Execution/Semantic/ExternalCompilationProvenanceDescriptorFactory.cs`.
Tests: existing `ExceptionFlowAnalyzerSelectionPolicyTests.cs` fixture construction
only, new `ExceptionFlowAnalyzerSelectionProjectionTests.cs` (30 cases).
Gate: `build/Verify-DualVersionBuild.ps1` adds fixture preparation and B7 tests.
Evidence: this report, `P5O2B7-trusted-selection-context-projection-audit.json`,
`OPEN-PIPELINE-BOUNDARIES.md`, and `P5O2B7Proof/Write-Verify-ProjectionEvidence.ps1`.
Test fixture: `P5O2B7Proof/HistoricalPdbFixture/{.gitignore,Directory.Build.props,HistoricalPdbFixture.csproj,Program.cs}`.
Root `.gitignore` is pre-existing protected WIP, **not an owned B7 change**.

From this tool directory, reproduce the permanent build/runtime/test gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter FullyQualifiedName~ExceptionFlowAnalyzerSelectionProjectionTests
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B7Proof/Write-Verify-ProjectionEvidence.ps1
```

The default gate leaves test ProjectReferences disabled, protecting Core. The
process-only ExecutionPolicy switch is needed by this machine's script policy;
no persistent policy setting is changed. The final evidence verifier passed.
The closure verifier requires the fresh starting snapshot and saved final test logs
under `artifacts/p5o2b7`; it checks this concrete run, not a recreated baseline.
The machine-readable permanent audit mirrors its generated output. All owned
files except the audit itself are fingerprinted (avoiding a self-hash cycle).

## Remaining boundaries and final repository audit

OPEN changes only the achieved trusted selection-context subboundary in
BND-P6-002 / BND-P5O2B-003 and the aligned BND-P5O2B-002 next-step wording.
Full validated external execution input/equivalence, more documents/references/
options/selectors, automatic acquisition/discovery, CLI/reporting/Dapper adoption,
and additional Historical versions remain open and fail closed as before.
No Dapper pipeline advancement is claimed. Stop here at B7 scope.

Final HEAD remains `35dbd5b2150e4f4591b1eead23511cd6f8444108`; **no external
HEAD movement** during B7. Root ignore hash, all 22 Core files (including bin/obj),
six stash hashes/messages, solution, Main/test projects and source manifest match
the pre-change snapshot. No commit, push, index or stash operation was performed.
No foreign changes were overwritten; initial root ignore WIP is retained.

Final `git status --short` (audit records the same status):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionContext.cs
 M src/XMLDocNormalizer/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactory.cs
?? Evaluation/P5O2B7-trusted-selection-context-projection-audit.json
?? Evaluation/P5O2B7-trusted-selection-context-projection.md
?? Evaluation/P5O2B7Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionProjectionTests.cs
?? src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionContext.Projector.cs
```
