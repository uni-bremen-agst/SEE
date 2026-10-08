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
- Status: Partially Resolved (P5O2B5 explicit Current/Historical selection and central Main routing complete; automatic policy/full external routing open)
- First Observed: P5O1
- Current Root Cause: P5O2B4 supplies the productive validated HistoricalWorkerClient. P5O2B5 adds a central Main-owned ExceptionFlowAnalysisRouter and explicit strongly typed Current/Historical choice. Current reuses its caller-owned SummarySession; Historical exclusively delegates to the unchanged client. Routed real E2E, parity, fail-closed selection and runtime isolation are proven. Automatic policy, full external inputs and CLI/reporting adoption remain separate work.
- Current Fail-Closed Behavior: ordinary ToolRunner analysis continues in Current; explicit Historical requests accept only the bounded profile and fail without results on unsupported/incomplete input or process/protocol/identity failures. No external compilation is approximated by the smoke profile.
- Affected Evaluation Cases: artifacts requiring an unavailable historical compiler execution context
- Scientific Relevance: process isolation and compiler provenance are required before historical Roslyn can be trusted.
- Resolution Package: P5O2B4 Main boundary and P5O2B5 explicit selection/routing complete; next separately authorized validated external input projection/routing or automatic selection policy, reusing the proven router/client/build/worker
- Last Verified: 2026-10-08
- Evidence / Report: `Evaluation/P5O1-canonical-exception-flow-ir.md`, `Evaluation/P5O2B3-historical-worker-host.md`, `Evaluation/P5O2B3-historical-worker-host-audit.json`, `Evaluation/P5O2B4-main-worker-analysis-boundary.md`, `Evaluation/P5O2B4-main-worker-analysis-boundary-audit.json`, `Evaluation/P5O2B5-explicit-analyzer-routing.md`, `Evaluation/P5O2B5-explicit-analyzer-routing-audit.json`

## BND-P6-003 — Historical analyzer dependency separation

- Pipeline Stage: P6 / Worker Analyzer Host
- Status: Resolved (P5O2A architecture closure, P5O2B2 permanent build and P5O2B3 bounded executable isolated host complete)
- First Observed: P5O2 readiness audit
- Current Root Cause: Architectural dependency separation remains resolved by P5O2A6F; the explicit semantic-host cut and existing fact/evaluator SCC/cache/guard behavior are retained. B1A2 closed the RuntimeAwaitMethod API gap and B2 supplied the permanent source-linked isolated build. B3 now supplies a genuine single-compilation semantic host and executable one-request HistoricalWorker. Actual historical runtime identity and canonical-only transitive analysis are proven in separate processes, without changing the 121 shared Analyzer sources. Six library/two Worker build pairs pass 0/0; fresh full tests 2636/2636. A2 self 16/canonical 0/0/0 are explicitly inherited after all 362 Current source hashes were reverified. Main/full external-input integration remains open, not Analyzer dependency separation.
- Current Fail-Closed Behavior: Main does not automatically select the Worker; compiler-mismatched external reconstruction remains fail closed. P5O2B4 permits explicit bounded Main-client requests only, with version/identity/completeness/source-provenance validation and no partial result on failure. No Analyzer dependency redesign.
- Affected Evaluation Cases: source-backed external analysis that requires an exact historical compiler, including the planned S1 probes
- Scientific Relevance: historical parsing and analysis must use one internally consistent Roslyn type universe while returning only canonical IR.
- Resolution Progress: P5O2A2 through the Return/condition slice reduced the residual to 49 Analyzer methods in six families, with SCC-to-Analyzer 32, SCC-to-components 61, SCC-to-SemanticScope 18, and provider-to-Analyzer zero. The Sequence Context / Element / Source slice then reduced the residual to 27 Analyzer methods in three families, with SCC-to-Analyzer 12 and SCC-to-components 81. P5O2A4D reduced the residual to 23 Analyzer methods in two families, with SCC-to-Analyzer eight and SCC-to-components 85. P5O2A4E reduced it to 14 Analyzer methods in the single Successful sequence validation family, with SCC-to-Analyzer one and SCC-to-components 92. P5O2A4F moved 13 methods to ExceptionFlowSuccessfulSequenceValidationFactsProvider and the statement-preservation method to ExceptionFlowSequenceContentPreservationFactsProvider. Analyzer-owned downstream methods and residual fact families are now zero; SCC-to-Analyzer is zero, SCC-to-components is 93, SCC-to-SemanticScope is 18, provider-to-Analyzer is zero, and component cycles are zero. Analyzer partials fall to 40 and nonblank Analyzer SLOC to 18,631. The 63-method SCC and 112 internal edges are unchanged. Self analysis remains 16 with zero finding/evidence diff. P5O2A4G moved the five delegate-target-resolution methods into ExceptionFlowDelegateTargetResolver. The SCC remains 63/112, SCC-to-Analyzer fact dependencies remain zero, the former SCC-to-Analyzer delegate-resolution edge is zero, SCC-to-components is 94, SCC-to-SemanticScope is 18, component-to-Analyzer and component cycles remain zero, and Analyzer SLOC falls to 18,316 across the same 40 partials. Self analysis remains 16 with zero finding/evidence diff.
- Boundary Audit (P5O2A5A): completed without production/test-source changes at HEAD 31e8f7751cafac955b0a7df3c13e7197ee7b5b21. Fresh whole-compilation binding confirms exactly the same 63 SCC members and 112 internal edges, including exact historical signature/edge equality. All 16 ingress edges are classified (14 orchestration edges from 13 callers, two thin guard-seed facades), with four distinct SCC entry targets. The complete direct egress census is 791 kind-labelled edges: 126 calls to 19 fact/resolver owners, 19 SemanticScope calls (including the Compilation overload), 18 CallContext edges, 23 value-fact extension calls, three cache-partition support edges and 602 metadata API edges. Historical 94/18 counts above remain historical projections, not the complete current census. No downstream fact/resolver-to-Analyzer return path was found. The only Analyzer-owned non-SCC support is the invariant-cache partition's two methods; its complete type and three fields must move with the SCC in A5B. Expanded composition reveals one provider-internal DataFlow provider/cache type cycle; inter-component cycles remain zero after explicitly collapsing nested implementation owners. Static weak-cache/model/symbol identities and per-chain guards are documented. Atomic A5B extraction is ready with 63+2 method declarations and no preparatory package; A5B has not begun. Architecture gates pass 23/23, final full retry passes 2547/2547, audit-tool warning-as-error build is 0/0. The known MultiModule flake occurred in the first full and isolated runs and was not repaired. Self analysis/canonical diff retain the verified A4G baseline, not a new run.
- Atomic Extraction (P5O2A5B): completed at starting HEAD 7362e7e22b9bf9fa14390b598ee6a2bbae33b0bb. Exact A5A membership/edge equality was verified before changes. Post-extraction SCC remains 63/112, ingress 16 edges from 15 callers to four entries (17 concrete invocation sites), full egress 791 including 126 fact/resolver calls and 19 SemanticScope calls. All 65 moved method bodies/parameters and all three field declarations are token-identical; 226 retained Analyzer method bodies are identical modulo evaluator qualification. Static weak SemanticModel partitions, OriginalDefinition/SymbolEqualityComparer.Default keys, bool results, locking and guard-before-cache/finally semantics are unchanged. Foreign provider caches and the provider-internal DataFlow type cycle remain untouched. The historical Analyzer-prefix file/whole-file nonblank-line measure falls from 40/18,316 to 29/12,977; actual Analyzer partial declarations are 28 because the retained CWT filename is now provider-only. New tests pass 12/12, focused 44/44, relevant 250/250, architecture 26/26, broad 1767/1767, unchanged full retry 2559/2559 without skips; warning-as-error builds are 0/0. The first full run hit only the known MultiModule flake, isolated passed, no foreign fix. Fresh Self Analysis remains 16 (DOC610=0, DOC611=1, DOC631=15, DOC632=0), complete raw/normalized arrays and evidence equal baseline, canonical diff 0/0/0. P5O2A5C is architecturally ready with no minimal blocker; A5C has not begun. Evidence: Evaluation/P5O2A5B-contextual-fact-evaluator-extraction.md and Evaluation/P5O2A5B-contextual-fact-evaluator-extraction-audit.json.
- Composition Cleanup (P5O2A5C): complete at starting HEAD 4ddd4113d753c04edb9cce7cc570b896ea83ab6d. All 226 Analyzer methods and 15 direct boundary methods were reviewed; exactly two unnecessary seed proxies were removed. Their token-identical fresh-guard declarations now live in existing evaluator partials, with all 14 call sites redirected (three LocalSourceAnalyzer users included). All 63 original SCC declarations, two cache helpers and three fields are unchanged; SCC/egress remains exactly 63/112/791. SCC ingress remains 16/15/4, while whole evaluator external ingress is now 27 edges / 24 callers / 4 targets / 29 sites through direct composition. No unjustified thin facade or empty Analyzer declaration remains. Analyzer-prefix files/nonblank SLOC are 28/12,898; actual declarations/owned nonblank lines are 27/12,412. Evaluator/lower components to Analyzer and inter-component cycles remain zero; the provider-internal DataFlow cycle is untouched. Tests: focused 45, relevant 269, architecture 27, broad 1768, full/final 2560, all passing without skips; warning-as-error builds 0/0. Self Analysis remains 16 (DOC610=0, DOC611=1, DOC631=15, DOC632=0), full raw/normalized finding arrays and evidence exactly equal Post-A5B, canonical diff 0/0/0. P5O2A6 Architecture / Readiness Closure is ready with no minimal blocker; A6 has not begun and no historical-worker/dual-version readiness is claimed. Evidence: Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup.md and Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup-audit.json.
- Architecture Closure (P5O2A6): audit complete, NOT READY; no production/test changes. All 27 lower fact/resolver/classifier/scope owners have no direct/indirect Analyzer type path; evaluator SCC/cache/guard/egress and the validated A5C bound graph are unchanged. Whole-core raw top-level type cycles are three: two internal neutral canonical domains and the real Analyzer/Session/Builder cycle (12 distinct cross-owner method edges). The provider-internal DataFlow cycle is unchanged. Exact candidate source/category/dependency, all state/record-backed properties, 11 nonconstant static fields, 210 Roslyn types and 430 unique Roslyn member signatures are inventoried. Fresh architecture tests 27/27, audit warning-as-error build 0/0, repeated measurements byte-identical. Unchanged committed source/declaration/graph/hash evidence justifies inheriting A5C full 2560, solution build 0/0, self 16 and canonical diff 0/0/0; these were not rerun. Separate minimal A6-Fix should relocate upper session-entry composition and share the exact summary-body predicate below both Analyzer and Builder, preserving source coverage/session reuse/nonvirtual host semantics. Do not start P5O2B before authorization and gate revalidation. Evidence: Evaluation/P5O2A6-architecture-readiness-closure.md, Evaluation/P5O2A6-architecture-readiness-closure-audit.json, Evaluation/P5O2A6-historical-core-readiness-matrix.md.
- Summary Cycle Closure (P5O2A6F): complete; ten bounded method-owner moves, no new component/storage or algorithm. Whole-core raw type cycles 3 -> 2 and raw method-owner cycles 2 -> 1 leave only unchanged neutral canonical domains. All 2451 production callables and 334 fields verified against starting HEAD modulo explicit owner/accessibility/receiver edits; state and lifetimes unchanged. Focused 16, Summary 302, Local/Callback/SummaryGraph 292, dependency gates 33, broad P5/P6/G 1784, full/final 2576, all passing without skips. All solution-project and audit warning-as-error builds 0/0; Core output isolated to preserve protected bin/obj. Fresh Self Analysis 16 (DOC610=0, DOC611=1, DOC631=15, DOC632=0); full raw/normalized finding/evidence arrays equal Post-A5C; canonical diff 0/0/0. Repeated full audits byte-identical. Protected ignore/Core files and six stashes unchanged. Evidence: Evaluation/P5O2A6F-summary-orchestration-cycle-closure.md, Evaluation/P5O2A6F-summary-orchestration-cycle-closure-audit.json, Evaluation/P5O2A6F-historical-core-readiness-matrix.md.
- Historical Build Feasibility (P5O2B1): complete, NOT READY for B2. Exact Common/CSharp 5.0.0-2.25451.107 references match validated P5N archive/DLL hashes; native net8.0 restore succeeds and repeats byte-identically. All 361 current Main source byte hashes equal A6F. The 120 productive files are linked unchanged plus five always-throwing host members, with zero Main/Core ProjectReferences. Both unchanged-source builds yield 0 warnings / four identical errors: missing AwaitExpressionInfo.RuntimeAwaitMethod in AddSummaryExplicitAwaitEdges (Calls 218/221) and AddSummaryExplicitAwaitDispatchEdges (Dispatch 275/278). Metadata comparison finds 429/430 identical used member contracts, 210/210 historical types and 84/84 equal used enum values; historical Roslyn DLLs were never runtime-loaded. Current build is 0/0. A6F full 2576, self 16 and canonical 0/0/0 are inherited, not rerun. Protected ignore/Core files and six stashes unchanged. Evidence: Evaluation/P5O2B1-historical-roslyn-build-feasibility.md, Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json, Evaluation/P5O2B1-roslyn-api-drift-matrix.md.
- RuntimeAwait Capability Proof (P5O2B1A): complete at HEAD d3140d7aa444809fa7dad7ad8ec4fa5b228040b3, NOT READY for B2. Identical bounded light-up source compiles against current 5.0.0 and exact historical 5.0.0-2.25451.107; current-only native-value/structural-non-null/concurrency/allocation checks pass. Exactly four in-memory expression substitutions make the 120-file historical boundary compile twice with 0 warnings/errors and equal images, without executing either projected Analyzer. Historical binder already selects AsyncHelpers.Await/AwaitAwaiter/UnsafeAwaitAwaiter, but public AwaitExpressionInfo omits the helper; a complete normal pattern can conceal that target and would not trigger existing incomplete-pattern uncertainty. Eight exact compiler source/PDB checksum comparisons and actual binary IL assertions confirm this information loss. Null fallback was rejected, not implemented; the experiment explicitly throws on absent API, which is compile proof only, not a historical analysis fallback. Full used API recheck adds no drift; current Main build is 0/0. Production and committed B1 sources, protected foreign state and stashes unchanged; A6F semantics inherited. Evidence: Evaluation/P5O2B1A-runtime-await-compatibility-proof.md, Evaluation/P5O2B1A-runtime-await-compatibility-proof-audit.json, Evaluation/P5O2B1AProof.
- RuntimeAwait Call-Site Contract (P5O2B1A2): complete at starting HEAD 622388dcd2faa9af0ea1ecf61d52396ef1179a9c. One stateless cached native getter distinguishes available-symbol, native-null and unavailable. Four accesses in two existing bodies are replaced; both unavailable guards record uncertainty and return before pattern/dispatch selection. The dispatch body is the active path; the retained non-dispatch body is directly tested. Fragment/summary/transitive result uncertainty suppresses DOC632 and retains DOC631 unless independent proof or a genuine catch-all applies. Current native equivalence and final-decision tests 24/24; relevant 420/420; full/final 2600/2600; all current solution-project and historical WAE builds 0/0. Original 120 files plus one helper, no omissions/projection; repeat images equal. Eight source/PDB checksums and exact historical binary hashes reverified; 430 original contracts, 210 types, 84 enum values, zero new drift. Fresh self 16 (DOC611=1, DOC631=15) and complete raw/normalized finding/evidence arrays exactly equal A6F, canonical 0/0/0. Protected ignore/Core bytes and six stashes unchanged. Historical runtime and B2 remain outside this package. Evidence: Evaluation/P5O2B1A2-runtime-await-call-site-contract.md and Evaluation/P5O2B1A2-runtime-await-call-site-contract-audit.json.
- Permanent Dual Build (P5O2B2): complete at starting HEAD 40d59416489584567a3e3cce5770402881383f37. Current/Historical/Current/Historical/Historical/Current forced clean/build order passes 0/0; each clean removes its own image, opposite bin/assets remain byte-identical. Permanent net8.0 owner is deliberately outside the normal solution, no runtime ProjectReferences or Evaluation dependencies. Same 121 physical productive files plus a rejecting five-member build-only host; no source fork or RuntimeAwait redesign. Exact preview Common/CSharp and stable dependencies restore from scoped source mappings to an isolated cache. New project and three build files are versionable through narrow local ignore exceptions, protected root ignore untouched. New project/architecture regressions 72/72, full 2608/2608; protected 21 Core outputs and six stashes unchanged. Evidence: Evaluation/P5O2B2-dual-version-build-scaffold.md and Evaluation/P5O2B2-dual-version-build-scaffold-audit.json.
- Executable Isolated Host (P5O2B3): complete at starting HEAD a4a533c823ee909aadb25cf6660c688101f90618. Worker references only the permanent Historical library and imports B2's isolated build configuration. The real compile-local host owns one exact source Compilation/tree/model/scope and rejects foreign bindings/external approximations. One UTF-8 JSON request/process invokes the unchanged productive SummarySession and existing canonical-result adapter. Root -> Thrower -> throw null proves one transitive NullReferenceException call/throw path; two fresh full responses are identical, catch transfer works, real historical await information is unavailable and fails closed with no result. Actual loaded Historical native InformationalVersion 5.0.0-2.25451.107+2db1... and exact MVID/hashes are confirmed; Current caller remains 5.0.0-2.25567.12+6c4... with no historical assembly loaded. Six library plus two Worker clean/build pairs pass 0/0; tests 100/100 relevant, 1844/1844 broad and 2636/2636 full, no skips. All 121 shared Analyzer sources/capability and 362 Current productive sources unchanged; self 16/canonical 0/0/0 explicitly inherited. Protected 21 Core files, root ignore and six stashes unchanged. No Main routing/pool/ALC or external pipeline advancement. Evidence: Evaluation/P5O2B3-historical-worker-host.md and Evaluation/P5O2B3-historical-worker-host-audit.json.
- Resolution Package: P5O2A6F/B1/B1A/B1A2/B2/B3 complete; next proposed P5O2B4 Main-Process / Worker Integration and Final Roslyn-free Analysis Boundary
- Last Verified: 2026-10-08
- Boundary Audit Evidence: Evaluation/P5O2A5A-contextual-evaluator-boundary-audit.md, Evaluation/P5O2A5A-contextual-evaluator-boundary-audit.json, Evaluation/P5O2A5A-contextual-evaluator-boundary-matrices.md, Evaluation/ContextualBoundaryAudit
- Evidence / Report: Evaluation/P5O2-historical-compiler-worker-readiness.md, Evaluation/P5O2A-exception-flow-analyzer-decomposition.md, Evaluation/P5O2A2-semantic-scope-callable-resolution-seam.md, Evaluation/P5O2A3-summary-graph-construction-evaluation.md, Evaluation/P5O2A4-context-value-facts-catch-local-source.md, Evaluation/P5O2A4B-call-context-value-fact-dependency-decomposition.md, Evaluation/P5O2A4C-residual-context-value-fact-scc.md, Evaluation/P5O2A4C-residual-scc-audit.json, Evaluation/P5O2A4C-semantic-model-resolution-and-fact-extraction.md, Evaluation/P5O2A4C-semantic-model-resolution-audit.json, Evaluation/P5O2A4C-residual-fact-family-extraction.md, Evaluation/P5O2A4C-residual-fact-family-audit.json, Evaluation/P5O2A4C-runtime-dispatch-stable-source-member-extraction.md, Evaluation/P5O2A4C-runtime-dispatch-stable-source-member-audit.json, Evaluation/P5O2A4C-return-condition-ownership.md, Evaluation/P5O2A4C-return-condition-ownership-audit.json, Evaluation/P5O2A4C-sequence-context-element-source-ownership.md, Evaluation/P5O2A4C-sequence-context-element-source-audit.json, Evaluation/P5O2A4D-dictionary-value-fact-ownership.md, Evaluation/P5O2A4D-dictionary-value-fact-ownership-audit.json, Evaluation/P5O2A4E-sequence-range-dictionary-mutation-fact-ownership.md, Evaluation/P5O2A4E-sequence-range-dictionary-mutation-fact-ownership-audit.json, Evaluation/P5O2A4F-successful-sequence-validation-fact-ownership.md, Evaluation/P5O2A4F-successful-sequence-validation-fact-ownership-audit.json, Evaluation/P5O2A4G-delegate-target-resolver-ownership.md, Evaluation/P5O2A4G-delegate-target-resolver-ownership-audit.json

## BND-P5O2B-001 — Historical RuntimeAwait API capability

- Pipeline Stage: P5O2B / Historical Analyzer Build
- Status: Resolved (P5O2B1A2 contract/P5O2B2 build; P5O2B3 real historical unavailable-await failure demonstrated)
- First Observed: P5O2B1 at HEAD a4048c91899115190992ceb3c86da29ba34d0825
- Current Root Cause: the exact historical Common/CSharp packages 5.0.0-2.25451.107 lack AwaitExpressionInfo.RuntimeAwaitMethod although runtime-async binding already exists. The internal BoundAwaitableInfo.RuntimeAsyncAwaitCall target is omitted from the historical public info/operation API; complete GetAwaiter/IsCompleted/GetResult can coexist with a hidden AwaitAwaiter/UnsafeAwaitAwaiter call. Consequently API absence cannot safely be replaced by null, and existing incomplete-pattern uncertainty does not cover all cases. Actual binary IL and PDB-checksum-verified compiler sources establish this loss. Equal public AssemblyVersion 5.0.0.0 is not a capability guarantee. Provenance, restore, net8.0 and the exact 120-file source boundary remain validated.
- Current Fail-Closed Behavior: the unchanged productive shared capability returns explicit unavailable information when the public API is absent, never known-null. Both explicit-await bodies record existing summary uncertainty and return before assuming pattern targets; existing evaluation/decision propagation preserves uncertainty, DOC631/DOC632 conservatism and independent proven sources/catch semantics. B3's real historical await fixture now executes this path in a separate Worker and returns structured analysisFailure with the actual unavailable-information diagnostic and no result. Current native symbol/null behavior remains unchanged; Main routing is not implemented.
- Affected Evaluation Cases: all planned same-source historical Analyzer builds against the exact preview package; no external pipeline-stage advancement is claimed.
- Scientific Relevance: runtime-async await calls differ from the normal awaiter pattern; removing or misrepresenting the capability could change call edges, uncertainty and exception paths.
- Resolution Package: P5O2B1A2 contract, P5O2B2 permanent compile and P5O2B3 bounded historical runtime execution complete. Three-state Information/getter lifetime, two guards and four replacements are unchanged; actual unavailable information is observed and fail closed in the real historical Worker. Fresh tests 100/1844/2636, WAE builds 0/0; A2 self 16/canonical 0/0/0 inherited with unchanged productive hashes. No private reconstruction or new compatibility layer. Next proposed P5O2B4 addresses Main integration and the validated neutral analysis boundary, not compile/runtime-isolation feasibility. Evidence includes Evaluation/P5O2B3-historical-worker-host.md and Evaluation/P5O2B3-historical-worker-host-audit.json.
- Last Verified: 2026-10-08
- Evidence / Report: Evaluation/P5O2B1-historical-roslyn-build-feasibility.md, Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json, Evaluation/P5O2B1-roslyn-api-drift-matrix.md, Evaluation/P5O2B1CompileProbe, Evaluation/P5O2B1A-runtime-await-compatibility-proof.md, Evaluation/P5O2B1A-runtime-await-compatibility-proof-audit.json, Evaluation/P5O2B1AProof, Evaluation/P5O2B1A2-runtime-await-call-site-contract.md, Evaluation/P5O2B1A2-runtime-await-call-site-contract-audit.json, Evaluation/P5O2B1A2Proof

## BND-P5O2B-002 — Executable isolated historical semantic host

- Pipeline Stage: P5O2B / Historical Runtime and Worker Host
- Status: Resolved (P5O2B3 bounded real semantic host and process isolation complete; Main/full external analysis remains separate)
- First Observed: P5O2B2 permanent build closure
- Current Root Cause: resolved for the authorized bounded path by P5O2B3. The permanent library now selects a real compile-local single-source-compilation host and the standalone HistoricalWorker executes its genuine productive SummarySession/canonical adapter. Actual loaded Common/CSharp version, MVID and image hashes prove exact historical runtime, while the Current caller's engine remains unchanged and separate. Full external input/acquisition/import/routing is not claimed.
- Current Fail-Closed Behavior: Worker stays outside the normal solution/Main runtime graph and accepts only its bounded protocol-2 source profile (P5O2B4). Invalid requests, build inputs, incomplete analysis and unexpected failures produce structured failure with no optimistic result. External supporting source is unavailable, never approximated. Main explicitly invokes it only through the validated B4 client, never loads Historical and does not automatically route to it; existing external compiler-mismatch boundaries remain fail closed.
- Affected Evaluation Cases: future exact-historical source-backed Analyzer execution and S1 probes
- Scientific Relevance: semantic/symbol/cache identities must stay within one Roslyn universe; only neutral canonical results may cross the later process boundary.
- Resolution Package: P5O2B3 host, P5O2B4 Main/input/result boundary and P5O2B5 explicit routing/selection complete. Validated full external input projection and automatic policy remain separate; do not repeat proven build/runtime isolation.
- Last Verified: 2026-10-08
- Evidence / Report: Evaluation/P5O2B1A2-runtime-await-call-site-contract.md, Evaluation/P5O2B2-dual-version-build-scaffold.md, Evaluation/P5O2B2-dual-version-build-scaffold-audit.json, Evaluation/P5O2B3-historical-worker-host.md, Evaluation/P5O2B3-historical-worker-host-audit.json

## BND-P5O2B-003 — Main routing / validated external Worker analysis boundary

- Pipeline Stage: P5O2B / Main-Process Integration
- Status: Partially Resolved (P5O2B5 explicit Current/Historical Main routing complete; automatic selection/full external routing open)
- First Observed: P5O2B3 executable isolated host closure
- Current Root Cause: P5O2B5 provides one Main entry for explicit Current/Historical selection, above the existing session/client. Default/unknown selections, absent or inconsistent branch inputs and unavailable Historical endpoints fail without fallback. P5O2B4 protocol-2 provenance/completeness/import validation is unchanged. One controlled C#12/net8 compilation is supported. Additional documents/reference images/supporting compilations/dependency provenance/canonical selectors/compiler option entries remain explicitly rejected. External acquisition/equivalence projection, automatic policy and full reporting route remain open.
- Current Fail-Closed Behavior: explicit routes only; invalid selection never means Current. Historical errors preserve the original B4 failure and expose no result. Current retains its existing uncertainties and sequential session lifetime, not the Worker's stronger completeness policy. Ordinary ToolRunner remains Current; no automatic routing and no larger external artifact is approximated by the smoke profile. Existing reconstruction/provenance failures remain intact.
- Affected Evaluation Cases: future historical external analysis integration, including S1 and compiler-mismatched artifacts
- Scientific Relevance: exact compiler identity alone cannot prove exact external input binding or canonical-result provenance. Only Roslyn-free values may cross the eventual process boundary.
- Resolution Package: P5O2B4 Main boundary and P5O2B5 explicit selection/routing complete; next separately authorized validated full external input/reporting integration or automatic policy, not a repeat of selection/build/runtime isolation
- Last Verified: 2026-10-08
- Evidence / Report: Evaluation/P5O2B3-historical-worker-host.md, Evaluation/P5O2B3-historical-worker-host-audit.json, Evaluation/P5O2B4-main-worker-analysis-boundary.md, Evaluation/P5O2B4-main-worker-analysis-boundary-audit.json, Evaluation/P5O2B5-explicit-analyzer-routing.md, Evaluation/P5O2B5-explicit-analyzer-routing-audit.json

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
