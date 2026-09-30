# Open Pipeline Boundaries

This register records current fail-closed boundaries. A listed boundary is not a
permanent rejection; status and resolution package identify the active path.

## BND-P5G-001 — Dapper `release-debug-plus`

- Pipeline Stage: P5G
- Status: Open
- First Observed: P5G / Dapper 2.1.35 evaluation
- Current Root Cause: the validated external compilation manifest requests the non-canonical optimization value `release-debug-plus`, which the exact compilation reconstruction path cannot currently reproduce.
- Current Fail-Closed Behavior: `ConfigurationUnsupported`; no approximate optimization mode is substituted.
- Affected Evaluation Cases: Dapper 2.1.35 / net7.0
- Scientific Relevance: accepting a different optimization configuration would invalidate source/PE equivalence.
- Resolution Package: planned compiler-provenance follow-up after P5O2
- Last Verified: 2026-09-24
- Evidence / Report: `Evaluation/G2-portable-pdb-acquisition.md`, `Evaluation/G5-exact-signed-compilation.md`

## BND-P5G-002 — Preview compiler mismatch

- Pipeline Stage: P5G
- Status: Under Investigation
- First Observed: P5G external compilation reconstruction
- Current Root Cause: an active compiler may not reproduce metadata emitted by a different preview compiler build even when public compilation options agree.
- Current Fail-Closed Behavior: exact reconstruction validation rejects non-equivalent output.
- Affected Evaluation Cases: external artifacts built with compiler behavior unavailable in the active Roslyn version
- Scientific Relevance: compiler-version drift can change binding, lowering, diagnostics, and emitted metadata.
- Resolution Package: P5O1 → P5O2
- Last Verified: 2026-09-24
- Evidence / Report: P5O architecture audit and P5O1 canonical-IR tests

## BND-P6-001 — Historical worker / canonical summary boundary

- Pipeline Stage: P6 / Worker Boundary
- Status: Resolved
- First Observed: P5O architecture audit
- Current Root Cause: resolved by P5O1; stored summary identity, context, catch information, call edges, uncertainty, paths, and analysis results now have Roslyn-independent canonical representations.
- Current Fail-Closed Behavior: canonical-to-Roslyn conversion requires one explicit compilation and rejects missing or ambiguous exact identities.
- Affected Evaluation Cases: all source-backed external exception-flow candidates
- Scientific Relevance: transport must preserve overload, assembly, context, catch-hierarchy, provenance, and uncertainty semantics.
- Resolution Package: P5O1
- Last Verified: 2026-09-24
- Evidence / Report: `Evaluation/P5O1-canonical-exception-flow-ir.md`

## BND-P6-002 — Historical worker integration

- Pipeline Stage: P6 / Worker Boundary
- Status: Planned
- First Observed: P5O1
- Current Root Cause: P5O1 provides transportable IR but deliberately does not start a historical compiler worker or define IPC.
- Current Fail-Closed Behavior: analysis continues in the active process and active Roslyn compilation only.
- Affected Evaluation Cases: artifacts requiring an unavailable historical compiler execution context
- Scientific Relevance: process isolation and compiler provenance are required before historical Roslyn can be trusted.
- Resolution Package: P5O2
- Last Verified: 2026-09-24
- Evidence / Report: `Evaluation/P5O1-canonical-exception-flow-ir.md`

## BND-P6-003 — Historical analyzer dependency separation

- Pipeline Stage: P6 / Worker Analyzer Host
- Status: Under Investigation
- First Observed: P5O2 readiness audit
- Current Root Cause: P5O2A established an independently buildable Roslyn-neutral single-source core and separated semantic scope, callable resolution, summary construction/evaluation, catch/local-source behavior, and numerous leaf fact owners. The genuine recursive contextual-fact SCC remains 63 methods with 112 internal edges. Within the original 252-node graph, 17 Analyzer methods are upstream and 163 methods are downstream; after the Sequence ownership slice, 27 downstream methods remain Analyzer-owned and 136 have component/provider owners. Moving the SCC alone still creates Analyzer -> evaluator -> Analyzer because 12 direct SCC-to-Analyzer fact edges remain. The ConditionalWeakTable invariant cache remains with the residual SCC. The active executable still compiles the Roslyn-bound source set locally because a runtime project boundary loses SameCompilation value-fact precision.
- Current Fail-Closed Behavior: no historical worker is created or invoked; compiler-mismatched reconstruction continues to fail closed at the existing boundary.
- Affected Evaluation Cases: source-backed external analysis that requires an exact historical compiler, including the planned S1 probes
- Scientific Relevance: historical parsing and analysis must use one internally consistent Roslyn type universe while returning only canonical IR.
- Resolution Progress: P5O2A2 through the Return/condition slice reduced the residual to 49 Analyzer methods in six families, with SCC-to-Analyzer 32, SCC-to-components 61, SCC-to-SemanticScope 18, and provider-to-Analyzer zero. The Sequence Context / Element / Source slice then reconstructed the exact 22-method group: 20 SCC incoming edges, zero edges back to the SCC, 22 internal edges, and 22 singleton method SCCs. Eight collection-shape/source-projection methods now extend ExceptionFlowSequenceCollectionFactsProvider, thirteen currentness/source-observation methods belong to ExceptionFlowSequenceContentPreservationFactsProvider, and one call-context projection extends ExceptionFlowCallContextFactProjector. The residual is 27 Analyzer methods in three families; SCC-to-Analyzer is 12, SCC-to-components is 81, SCC-to-SemanticScope is 18, and provider-to-Analyzer remains zero. Analyzer partials fall to 41 and nonblank Analyzer SLOC to 19,948. The 63-method SCC and 112 internal edges are unchanged. Self analysis remains 16 with zero finding/evidence diff. The exact smallest next blocker is the four-method Dictionary value-facts family receiving four SCC edges.
- Resolution Package: extract the four-method Dictionary value-facts family, then the remaining Range/mutation and Successful-sequence families; move the intact recursive SCC only after its 12 residual Analyzer edges are eliminated; complete assembly composition; then perform the P5O2B dual-version build
- Last Verified: 2026-09-30
- Evidence / Report: Evaluation/P5O2-historical-compiler-worker-readiness.md, Evaluation/P5O2A-exception-flow-analyzer-decomposition.md, Evaluation/P5O2A2-semantic-scope-callable-resolution-seam.md, Evaluation/P5O2A3-summary-graph-construction-evaluation.md, Evaluation/P5O2A4-context-value-facts-catch-local-source.md, Evaluation/P5O2A4B-call-context-value-fact-dependency-decomposition.md, Evaluation/P5O2A4C-residual-context-value-fact-scc.md, Evaluation/P5O2A4C-residual-scc-audit.json, Evaluation/P5O2A4C-semantic-model-resolution-and-fact-extraction.md, Evaluation/P5O2A4C-semantic-model-resolution-audit.json, Evaluation/P5O2A4C-residual-fact-family-extraction.md, Evaluation/P5O2A4C-residual-fact-family-audit.json, Evaluation/P5O2A4C-runtime-dispatch-stable-source-member-extraction.md, Evaluation/P5O2A4C-runtime-dispatch-stable-source-member-audit.json, Evaluation/P5O2A4C-return-condition-ownership.md, Evaluation/P5O2A4C-return-condition-ownership-audit.json, Evaluation/P5O2A4C-sequence-context-element-source-ownership.md, Evaluation/P5O2A4C-sequence-context-element-source-audit.json

## BND-P4P7-001 — OneOf exact PDB

- Pipeline Stage: P4/P7
- Status: Open
- First Observed: G2 / OneOf 3.0.263 evaluation
- Current Root Cause: no exact local, embedded, package, or public symbol-server Portable PDB has been validated for the shipped assembly.
- Current Fail-Closed Behavior: `MissingArtifact`; no non-matching PDB is accepted.
- Affected Evaluation Cases: OneOf 3.0.263 / netstandard2.0
- Scientific Relevance: document and method provenance cannot be established without the exact PDB.
- Resolution Package: artifact acquisition follow-up
- Last Verified: 2026-09-24
- Evidence / Report: `Evaluation/G2-portable-pdb-acquisition.md`, `Evaluation/G4B-bounded-remote-artifacts.md`

## BND-EXC-001 — `Program.Main` relational postcondition

- Pipeline Stage: Exception Analysis
- Status: Planned
- First Observed: self-analysis evaluation
- Current Root Cause: bool/out/member relationships across a call are not represented as relational postconditions.
- Current Fail-Closed Behavior: the unresolved relationship retains the existing DOC611 finding.
- Affected Evaluation Cases: XMLDocNormalizer self analysis, `Program.Main`
- Scientific Relevance: suppressing the finding without a proven relation would be unsound.
- Resolution Package: future relational-value-facts package
- Last Verified: 2026-09-24
- Evidence / Report: self-analysis canonical finding baseline

## BND-EXC-002 — `IOException` / BCL uncertainty

- Pipeline Stage: Exception Analysis / BCL
- Status: Open
- First Observed: self-analysis evaluation
- Current Root Cause: the relevant BCL I/O behavior is not proven by source-backed or authoritative framework-contract evidence.
- Current Fail-Closed Behavior: the existing DOC631 finding is retained.
- Affected Evaluation Cases: XMLDocNormalizer self analysis
- Scientific Relevance: undocumented framework behavior must not be treated as proven absence of an exception.
- Resolution Package: future BCL-contract provenance package
- Last Verified: 2026-09-24
- Evidence / Report: self-analysis canonical finding baseline

## BND-G5-001 — DelaySign / PublicSign

- Pipeline Stage: P5K/G5 exact signed compilation
- Status: Currently Unsupported
- First Observed: G5
- Current Root Cause: delayed- and public-sign emit shapes are distinct from fully signed and unsigned output and are not reconstructed by the validated signing path.
- Current Fail-Closed Behavior: these signing modes are rejected instead of normalized to another signing mode.
- Affected Evaluation Cases: external assemblies emitted with `DelaySign` or `PublicSign`
- Scientific Relevance: changing the signing mode changes the emitted PE identity and invalidates exact reconstruction.
- Resolution Package: future exact signing-mode reconstruction
- Last Verified: 2026-09-24
- Evidence / Report: `Evaluation/G5-exact-signed-compilation.md`
