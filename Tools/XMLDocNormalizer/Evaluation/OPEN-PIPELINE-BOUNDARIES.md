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
- Current Root Cause: P5O2A established an independently buildable Roslyn-neutral single-source core. P5O2A2 removed direct analyzer-source dependencies on `ProjectClosureSemanticContext`, `SemanticCompilationScope`, `SupportingSourceSymbolResolver`, and the Main-owned cross-compilation resolver. P5O2A3 separated environment-bound summary construction, graph-only summary evaluation, and per-run session/cache ownership. P5O2A4B reconstructed the complete 252-method context/fact graph and extracted parameter mapping, symbol-use classification, stable-member classification, and successful-dereference discovery. P5O2A4C isolated the remaining 63-method SCC exactly: it has 112 internal edges, four entry nodes, 53 exit nodes, six Context-to-Fact and six Fact-to-Context edges. Its responsibility is genuine recursive contextual fact evaluation: same-compilation source-return and sequence discovery constructs a callee context whose arguments must themselves be evaluated. The sharper blocker is ownership around that SCC. Within the 252-node graph, 17 Analyzer methods are upstream and 163 methods are downstream; 49 downstream methods still belong to `ExceptionFlowAnalyzer` and 114 have component or provider owners. Moving the SCC alone therefore creates `Analyzer -> evaluator -> Analyzer`. Any proper SCC subset also has edges in both directions. Acyclic extraction requires a staged decomposition of the remaining 49-method downstream Analyzer closure before the intact SCC can move. Performing both changes at once would be the prohibited non-reviewable fact-system rewrite. The ConditionalWeakTable invariant cache remains with the residual contextual fact SCC. The active executable also still compiles the Roslyn-bound source set locally because a runtime project boundary loses same-compilation value-fact precision.
- Current Fail-Closed Behavior: no historical worker is created or invoked; compiler-mismatched reconstruction continues to fail closed at the existing boundary.
- Affected Evaluation Cases: source-backed external analysis that requires an exact historical compiler, including the planned S1 probes
- Scientific Relevance: historical parsing and analysis must use one internally consistent Roslyn type universe while returning only canonical IR.
- Resolution Progress: P5O2A2 completed semantic-scope and callable-resolution separation. P5O2A3 gives summary construction and summary evaluation distinct owners. P5O2A4 gives catch/filter/rethrow semantics and direct local-source traversal concrete static owners. P5O2A4B adds `ExceptionFlowArgumentMapper`, `ExceptionFlowSymbolUsageFacts`, `ExceptionFlowStableMemberFacts`, and `ExceptionFlowDereferenceFactDiscovery` as closed static owners with one-way dependencies and no interface, callback, virtual, service-locator, summary-evaluator, or Main dependency. The first P5O2A4C pass added a complete machine-readable audit of the residual 63-method SCC. The first downstream continuation classified all 124 Analyzer-owned descendants and extracted 22 methods into `ExceptionFlowKnownPropertyValueFactsProvider`, `ExceptionFlowGuardFactsProvider`, `ExceptionFlowPrimitiveValueFactsProvider`, and the existing symbol-usage owner. The semantic-model continuation assigns exact compilation-local lookup to `ExceptionFlowSemanticScope` while `ExceptionFlowSemanticEnvironment` retains cross-scope selection and the existing context cache. It then extracts the four remaining nullability helpers and five immutable-member helpers into two closed providers. Cumulative extraction is 32 methods; 92 Analyzer-owned downstream methods in 16 families remain. The SCC is still 63 methods and 112 internal edges, with 68 outgoing edges to the residual Analyzer closure, 20 to providers, and 18 to the semantic scope. Analyzer partials remain 45 and Analyzer nonblank SLOC is now 22,799. Provider-to-Analyzer edges remain zero; no cache, context construction, mutable state, interface, or virtual dispatch moved. Same-compilation, referenced-project, supporting-source, metadata-only, exact identity, cache, and demand-driven P6 behavior remain unchanged; self-analysis remains 16 with zero finding/evidence diff. Direct references to all four P5O2A2 blockers remain zero. The residual-family continuation extracts another 28 methods from seven dependency-safe families into existing or cohesive new providers. The residual is now 64 Analyzer-owned methods in nine families. SCC-to-Analyzer edges fall from 68 to 38, SCC-to-provider edges rise from 20 to 50, and the 18 SCC-to-SemanticScope edges remain unchanged. Provider-to-Analyzer remains zero. The ConditionalWeakTable cache stays Analyzer-owned while seven stateless helpers move. The runtime-dispatch continuation assigns the former Analyzer helper to the stateless `ExceptionFlowRuntimeDispatchClassifier`, extracts five stable source-member methods and two call-context projections, and reduces the residual to 57 methods in seven families. SCC-to-Analyzer falls to 36, SCC-to-components rises to 57, SCC-to-SemanticScope remains 18, and provider-to-Analyzer remains zero. The Return/condition continuation assigns five condition-derived string-fact helpers to `ExceptionFlowGuardFactsProvider` and three known-framework non-null return classifiers to `ExceptionFlowNullabilityFactsProvider`. The residual is now 49 Analyzer methods in six families; SCC-to-Analyzer is 32, SCC-to-components is 61, SCC-to-SemanticScope remains 18, and provider-to-Analyzer remains zero. The SCC stays 63 methods and 112 internal edges. Self analysis remains 16 with zero finding/evidence diff. The exact next blocker is the Sequence Context / Element / Source condensation cycle, with approximately 20 relevant SCC edges.
- Resolution Package: decompose the Sequence Context / Element / Source ownership cycle, continue staged family-level extraction of the 49-method residual Analyzer downstream closure, move the intact recursive SCC only after its 32 residual Analyzer edges are eliminated, complete assembly composition, then perform the P5O2B dual-version build
- Last Verified: 2026-09-30
- Evidence / Report: `Evaluation/P5O2-historical-compiler-worker-readiness.md`, `Evaluation/P5O2A-exception-flow-analyzer-decomposition.md`, `Evaluation/P5O2A2-semantic-scope-callable-resolution-seam.md`, `Evaluation/P5O2A3-summary-graph-construction-evaluation.md`, `Evaluation/P5O2A4-context-value-facts-catch-local-source.md`, `Evaluation/P5O2A4B-call-context-value-fact-dependency-decomposition.md`, `Evaluation/P5O2A4C-residual-context-value-fact-scc.md`, `Evaluation/P5O2A4C-residual-scc-audit.json`, `Evaluation/P5O2A4C-semantic-model-resolution-and-fact-extraction.md`, `Evaluation/P5O2A4C-semantic-model-resolution-audit.json`, `Evaluation/P5O2A4C-residual-fact-family-extraction.md`, `Evaluation/P5O2A4C-residual-fact-family-audit.json`, `Evaluation/P5O2A4C-runtime-dispatch-stable-source-member-extraction.md`, `Evaluation/P5O2A4C-runtime-dispatch-stable-source-member-audit.json`, `Evaluation/P5O2A4C-return-condition-ownership.md`, `Evaluation/P5O2A4C-return-condition-ownership-audit.json`

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
