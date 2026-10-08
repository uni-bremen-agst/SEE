# P5O2B3 - Executable Isolated Historical Worker Host

Date: 2026-10-08. Starting/unchanged HEAD:
`a4a533c823ee909aadb25cf6660c688101f90618` (`Add dual-version analyzer build.`).
B2 is committed; initial Working Tree contained only protected ` M ../../.gitignore`.

**COMPLETE: the Historical Analyzer executes reproducibly in a separate process;
Current and Historical Roslyn are isolated at runtime.**

This is a bounded executable endpoint, not Main routing or a general external
compilation-reconstruction workflow. No Dapper/external pipeline-stage advancement
is claimed. Self Analysis/canonical findings are explicitly inherited, not fresh.

## Evidence-first plan verification

Read the complete B2 report/audit, permanent projects/manifest/build/CI, boundary
register and relevant B1/B1A/A2 runtime-compatibility evidence. The existing B2 gate
was executed successfully at the new HEAD before modifying the historical host;
its starting audit is retained at `artifacts/p5o2b3/b2-at-start.json`.
All 362 Current productive C# files and the 121 shared Historical productive files
match A2 fingerprints. Main project, solution and shared source manifest remain
unchanged. No Analyzer algorithm, RuntimeAwait contract or cache refactoring.

## Permanent project and ownership

- Worker: `src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj`,
  net8.0 executable, AssemblyName `XMLDocNormalizer.HistoricalWorker`.
- Sole runtime ProjectReference: the existing permanent
  `XMLDocNormalizer.ExceptionFlow.Historical` library. No Current Main/Core,
  Workspace/MSBuild, Evaluation or worker-side source copies.
- Worker Directory.Build.props imports B2's Historical Directory.Build.props.
  There is one build configuration and one historical package universe, not a
  second build mechanism. Project-named outputs keep library and executable apart.
- Worker assets/generated sources/output:
  `artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/{obj,bin}/`;
  Historical library keeps its own existing sibling project directory.
- Same isolated package cache/source mapping as B2. Worker is deliberately outside
  the normal solution, with a one-project local ignore exception for versionability.
- Main has no Worker/Historical reference or launcher/routing change. Tests link
  **only** the neutral WorkerProtocol source, resolved against their existing Current
  canonical domain, and launch owned child processes. They do not reference or load
  either historical runtime assembly.

Historical still compiles all **121 original shared productive files plus one host**.
The selected host is now `HistoricalSemanticEnvironment.cs`, with an explicit Worker
friend assembly and an isolated-process execution-contract assembly attribute.
B2's original rejecting BuildOnlySemanticEnvironment is retained unchanged as
uncompiled evidence. Default Compile items remain disabled; it is not a runtime
fallback or a second active host. B1/B1A/A2 reports/probes are also retained.

### Real bounded semantic host

The compile-local, nonvirtual host owns exactly one Compilation/tree, one stable
SemanticModel and one deterministic scope snapshot for the request. Model lookup
requires exact tree-object ownership. Supporting-source resolution can return
only genuine source symbols belonging to that compilation and exact tree; a
binding-compilation overload additionally requires ReferenceEquals ownership.
No metadata-name rebinding or look-alike compilation is accepted. No external
supporting-source registry is installed: external lookup returns false, allowing
the existing Analyzer uncertainty behavior to fail closed.

Host additions are scope/capability orchestration, not copied Analyzer logic.
The ordinary production SummarySession, graph builder/evaluator, existing fact
providers, guards and canonical adapter perform the analysis unchanged.

## Versioned Roslyn-free protocol

Protocol version **1**, Worker version **1.0**. One UTF-8 JSON object on stdin until
EOF, one JSON object on stdout, then exit. No daemon/pool, line-loop, network server,
RPC framework, ALC or persistent cross-request state. A second JSON frame is rejected.
Exit 0 means success; exit 1 means structured failure. Diagnostics/stack traces, if
needed, go only to stderr; they are never the sole machine-readable failure.

Request fields: `protocolVersion`, `operation`, optional `payload`.
Operations: `identity` (no payload) and `analyze`.
Analyze payload: `source`, `typeMetadataName`, `methodName`.

```json
{
  "protocolVersion": 1,
  "operation": "analyze",
  "payload": {
    "source": "public static class Fixture { public static void Root() { Thrower(); } static void Thrower() { throw null; } }",
    "typeMetadataName": "Fixture",
    "methodName": "Root"
  }
}
```

Response fields: `protocolVersion`, `operation`, `success`, optional `identity`,
`result`, `failure`. Result is the **existing P5O1
CanonicalExceptionFlowAnalysisResult** with canonical exception identities, evidence
kind, paths/steps/truncation and uncertainty. No parallel exception-result domain
was introduced. Runtime-identity DTOs are diagnostics, not replacement symbol identities.
The recursive contract-graph test rejects any Microsoft.CodeAnalysis or object-typed
transport member, including the complete reachable canonical-result property graph.
No syntax/semantic/compilation/symbol/operation object, graph or cache crosses stdout.

Failure is `{code,message,details}`. Codes:
`invalidRequest`, `unsupportedProtocolVersion`, `malformedInput`,
`compilationFailure`, `analysisFailure`, `unexpectedWorkerFailure`.
Malformed JSON/UTF-8, unknown fields, unsupported versions/operations, missing
selectors/payloads and oversized frames are tested. Compilation errors return
deterministic invariant-culture diagnostics. Missing/unsupported roots are errors,
not empty-success shortcuts. Any result uncertainty or path truncation produces
analysisFailure with **no successful or partial result payload**. Unexpected runtime
or serialization errors also yield a structured failure rather than corrupt stdout.

### Deliberately bounded input profile

Frame limit: 65,536 characters; source limit: 32,768 characters / 8,192 syntax nodes.
Exactly one C#12 source tree, fixed virtual path `worker-input.cs` and assembly name
`HistoricalWorkerInput`. The root must be a body-bearing, ordinary, static,
parameterless, non-generic method on a source-owned non-generic type. The declared
root symbol/model is verified before entering the session, covering its early
unsupported-root/empty-result conditions without duplicating Analyzer semantics.

Reference profile `net8-runtime-bounded-v1` selects seven framework-only images from
the host's trusted platform list: Private.CoreLib, Runtime, Threading.Tasks,
Collections, Console, Threading and Runtime.Extensions. Exact duplicate canonical
file paths are deduplicated; missing or distinct ambiguous candidates fail closed.
Worker/Analyzer/Roslyn assemblies are not input references. Requests cannot inject
metadata paths, options or acquisition instructions. Nothing is emitted and the
submitted source code is never executed.

This profile proves a controlled semantic path. It does **not** reproduce an
external artifact's original TFM/reference/options/compiler manifest. That larger
input/import policy belongs to subsequent Main/Worker integration, not an expansion
of this smoke fixture into a speculative full RPC system.

## Actual runtime identity and process isolation

AssemblyVersion alone cannot distinguish these engines; both Common/CSharp pairs
report `5.0.0.0`. The handshake reads **already loaded** typeof(Compilation) /
typeof(CSharpCompilation) assemblies, actual module MVID, InformationalVersion and
their image SHA256. It validates the exact Historical hashes before analysis and
rejects a Current Analyzer or additional Roslyn engine in the Worker. No stable
protocol field contains an absolute developer path, PID or timing value.

| Runtime | Actual Common/CSharp InformationalVersion |
| --- | --- |
| Current test caller | `5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09` |
| Historical child | `5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0` |

Historical runtime Common:
MVID `dc7738cc-6dca-4d34-9c44-29b53a7caa93`,
SHA256 `660C3D626C4B8F4CF8C231FBEF0FB6B4DB4FFFCC89EF5B31AAECA1CF4D7F66A1`.
Historical runtime CSharp:
MVID `0f9c1dcf-4eb1-47f8-81b2-733db5887be7`,
SHA256 `B0EC1DDCA4C97DCF15845FF4BCEB5499C3989025197D5D65E04310E29C09217D`.
These match the validated B1/A2/B2 images, not merely package filenames.

The Current integration test starts a real different PID, inspects its JSON identity,
then verifies Current Common/CSharp MVID/informational identity unchanged and no
historical MVID/Analyzer assembly in the caller's loaded assemblies. Actual caller
image hashes also match the Current compile references. Diagnostic PIDs/identities
are retained separately in `artifacts/p5o2b3/current-runtime-isolation.json`, not
inserted into semantic responses. Main has no historical reference; the process
test proves the actual Current caller isolation without pretending Main routing exists.

## Genuine analysis and deterministic failure

The smoke uses:
`historical ParseText/CreateCompilation/GetSemanticModel -> owned host ->
ExceptionFlowSummaryAnalysisSession.AnalyzeSolutionTransitivelyThrownExceptions ->
productive summary graph/SCC/evaluator -> RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult`.

`Root()` calls source `Thrower()`, whose `throw null` proves
System.NullReferenceException. Response contains one Proven entry and the real
MethodCall / ExplicitThrow path with source positions. This proves traversal and
transitive evaluation, not just startup/ping. Two fresh workers return identical
**complete stdout responses**, including identical relevant canonical data.

A second real source fixture catches that NullReferenceException, producing no
escaping entries or uncertainty. A real historical Task-await fixture takes the
unchanged missing-RuntimeAwait information path: analysisFailure, with
`Await expression runtime-await information is unavailable.` and no result.
Thus executable-body/complete-looking await patterns are not optimistic successes.
Historical capability reports unavailable; its three-state contract/cache/getter
lifetime and the two guards/four substitutions remain byte-identical to A2.

## State/cache lifetime

Each fresh process initializes its CLR static owners on first use. The RuntimeAwait
cached public getter is stateless (absent for this exact historical image) and never
retains symbols/compilations. Existing static weak caches are unchanged:

- ContextualFactEvaluator: CWT value-fact partitions keyed by SemanticModel,
  OriginalDefinition/SymbolEqualityComparer invariant facts and existing guards.
- DereferenceFactDiscovery: successful-dereference CWT SemanticModel partitions.
- DataFlowFactsProvider: one static cache owner with weak SemanticModel partitions
  and exact region/overload keys, including unsuccessful snapshots.

Neutral comparer/sentinel singletons are unchanged. Within one request, the stable
model, scope and ordinary session graph retain their normal reuse, traversal and
SCC/locking/guard-before-cache/finally semantics. No cache reset/redesign or global
test switch. Request completion/process exit ends the whole Roslyn universe; one
request/one process cannot expose stale cross-request state. Only canonical value
copies leave the process, never session, weak-cache or symbol data.

## Build, package and CI validation

The existing `build/Verify-DualVersionBuild.ps1` now covers Worker, not a competing
pipeline. It retains the six forced Current/Historical pairs in both orders and
adds two Worker-only clean/build pairs with BuildProjectReferences=false after the
library is built. Each clean removes its own image; opposite Current/Historical
bins and corresponding library assets remain isolated. Worker dependency copies
are SHA-checked against the genuine Historical library/Common/CSharp images.
Two forced Worker DLL/PDB pairs match, as do each library's three repeated pairs.

Historical graph remains B2's five exact packages. Worker inherits four transitives:
Common/CSharp **5.0.0-2.25451.107**, Immutable/Metadata **9.0.0**, plus the one
Historical project dependency; Analyzers **3.11.0** remains library-private build
infrastructure. No Worker direct floating pins or Current/Workspace package.
Current retains its unchanged stable 5.0.0/39-package graph. All package graphs,
resolved references, copy hashes, runtime identities and build logs are audited.

| Target | Final repeat DLL SHA256 |
| --- | --- |
| Current | `097A822687E8FD1C93890D2522D57EDE81AD62F422DF85BFA0AC636213716EA7` |
| Historical library | `4CDBE1735AF7D0B98E5AA04DC4BD487BAE52C6283C58FE76E6C7D3333A65F058` |
| Historical Worker | `AA696D11C0754722978CBF75058D3C3FBA25CE91CD669DA9C99C4BFAB6248083` |

SDK 8.0.418 drives compilation, as in B2; runtime execution is now proven against
the exact historical API engine. Within-checkout Debug repeatability is asserted;
different commit/source-link/SDK/runtime profiles are not claimed byte-equivalent.
Current's new build hash reflects the new committed HEAD, not changed Analyzer logic.

The existing GitHub workflow runs this expanded gate and then the Current-host
protocol/process/project tests. It uses .NET 8/pwsh and retains build/runtime
artifacts. Local Windows PowerShell execution is proven; remote CI was not run and
no push was made. Per-unit-test compilation was not added. Integration tests require
the corresponding permanent Worker output to have been built first and use a
30-second owned-child deadline/cleanup, not a silent skip.

## Fresh regressions and explicit inherited semantics

| Gate | Result |
| --- | --- |
| Current/Historical forced clean/builds | 3 each, all 0 warnings / 0 errors |
| Worker forced clean/builds | 2, both 0/0, equal DLL/PDB |
| Tests warning-as-error build, references=false | 0/0 |
| Worker / protocol / architecture / RuntimeAwait / dependency | **100/100**, no skips |
| Broad Check.Semantic / Execution.Semantic / Evaluation / Worker (P5/P6/G) | **1844/1844**, no skips |
| Full Current suite | **2636/2636**, no skips (2608 + 28 new tests) |
| Exact child runtime identity and caller isolation | PASS, actual separate processes |
| Two fresh real analysis responses | identical; real call/throw path |
| Failure, actual unavailable await, catch-transfer and malformed UTF-8 | PASS |
| Scoped folder-only format, UTF-8/CRLF, JSON/XML/PS parse, git diff check | PASS |

The 28 new tests cover serialization/failure categories, complete neutral contract
graph, exact runtime identity, real transitive deterministic paths/catches, unavailable
await, invalid versions/JSON/UTF-8/commands/root/compilation/size and worker project
ownership. B2's eight existing project tests remain green with their active-host
expectation updated for the explicitly authorized B3 executable transition.

Per B3 section 17, **Self Analysis 16 (DOC610=0, DOC611=1, DOC631=15, DOC632=0)
and Canonical Diff 0/0/0 are inherited from A2, not rerun**. A2's complete raw and
normalized Finding/Evidence array equality is retained as baseline evidence.
All shared productive Analyzer source fingerprints, capability, Main project and
normal solution are unchanged. New Historical host/protocol/orchestration does not
fork or modify the Analyzer algorithm. This is not presented as a fresh B3 finding
comparison. Full Current tests were nevertheless rerun as above.

Initial issues were local and resolved in this package: a duplicate identical
Private.CoreLib TPA entry failed the initial profile-count check; canonical-path
deduplication solved it without accepting ambiguous distinct images. The first
process test run had 34/35 passing because its await assertion expected an API
property name rather than the unchanged real uncertainty message; the assertion,
not the algorithm/diagnostic, was corrected. Earlier failed runs are not counted
as passing evidence; their first TRX/build logs are retained.

## Reproduction

From Tools/XMLDocNormalizer (use pwsh instead of powershell on Linux/macOS):

```powershell
dotnet build src/XMLDocNormalizer/XMLDocNormalizer.csproj -warnaserror
dotnet build src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj -warnaserror
dotnet build src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj -warnaserror
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1
'{"protocolVersion":1,"operation":"identity"}' | dotnet artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll
dotnet build Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-restore -warnaserror -p:BuildProjectReferences=false
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~HistoricalWorker|FullyQualifiedName~HistoricalAnalyzerBuildProjectTests|FullyQualifiedName~ExceptionFlowSemanticDependencyGuardTests|FullyQualifiedName~RuntimeAwait|FullyQualifiedName~Dependency' --logger 'trx;LogFileName=worker-architecture.trx' --results-directory artifacts/p5o2b3/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~Check.Semantic|FullyQualifiedName~Execution.Semantic|FullyQualifiedName~Evaluation|FullyQualifiedName~Worker' --logger 'trx;LogFileName=broad.trx' --results-directory artifacts/p5o2b3/tests
dotnet test Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj --no-build --no-restore --logger 'trx;LogFileName=full.trx' --results-directory artifacts/p5o2b3/tests
dotnet format whitespace . --folder --include src/XMLDocNormalizer.HistoricalWorker src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs Tests/XMLDocNormalizerTests/Worker Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/HistoricalAnalyzerBuildProjectTests.cs --verify-no-changes
powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B3Proof/Write-Verify-WorkerEvidence.ps1
```

The evidence verifier checks real logs/TRX/runtime/source/protected snapshots and
emits `artifacts/p5o2b3/worker-host-audit.json`, adopted into the companion report
audit. It does not fabricate runs or replace the permanent build mechanism.
The Core outputs were not rebuilt: current dependent test builds explicitly use
BuildProjectReferences=false; folder-only formatting opens no Core MSBuild workspace.

## Remaining integration boundary and next step

Next: **P5O2B4 - Main-Process / Worker Integration and Final Roslyn-free Analysis Boundary**.
Define the explicit validated input/reference/options/provenance and canonical-result
import boundary, then integrate authorized Main worker selection/routing using the
proven process endpoint. Do not repeat a compile-feasibility or runtime-isolation
analysis package. Full external compilation inputs/reporting and larger batches
remain deliberately outside this bounded endpoint. No Worker pool/daemon, automatic
Current/Historical routing or large canonical/RPC redesign was implemented.

Minimal B3 blocker: **none**. The remaining Main routing/full-analysis boundary is
future authorized work, not a failure to execute the bounded real Analyzer path.

## Protected state and handoff

Protected root ignore bytes, all **21 existing Core bin/obj files/file set**, six
stash identities/messages, Main project and normal solution hashes match the
initial audit. Both named position-provenance stashes are untouched. No foreign
changes, commit, push, index/stash operation, reset/checkout/restore-to-HEAD.
Only owned disposable target outputs were cleaned/rebuilt. Final complete
`git status --short` and protected/runtime/build/test/source hashes are recorded
in the companion audit and handoff. All B1/B1A/B1A2/B2 evidence remains intact.
