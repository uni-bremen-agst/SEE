# P5O2 Historical Compiler Worker Readiness Audit

P5O2 stopped at the mandatory readiness gate. The existing exception-flow
analyzer cannot currently be reused by a historical worker without a material
dependency-separation refactor. No worker, protocol, manifest, process host, or
historical execution path was added.

## Repository gate

- HEAD: `dbdecdbaf8e2e29da87764449d8d311ba2f60205`
- Commit: `dbdecdb Add canonical exception flow representation.`
- P5O1 is committed.
- The only pre-existing working-tree change was `../../.gitignore`; it was not
  modified by P5O2.
- No stash was applied or changed.

## Readiness answers

### 1. Canonical types directly transportable

The canonical identity graph is Roslyn-independent and already has
deterministic JSON round-trip coverage. This includes assembly, module,
namespace, type, callable, stable-member, and source-location identities.

The canonical exception-flow graph is also free of `Microsoft.CodeAnalysis`
references and has deterministic JSON round-trip coverage. It cannot yet be
referenced by an independent worker, however, because all canonical types are
internal to the main executable and some of them depend on other types in that
executable.

### 2. Types requiring a neutral contract boundary

No second canonical identity model is required. A Roslyn-neutral contract
assembly must own or reference the existing canonical sources plus the small
non-Roslyn value types on which they depend:

- `ExceptionFlowValueFacts`
- `ExceptionFlowPathStep`
- `ExceptionFlowPathStepKind`
- `ExceptionFlowSourceKind`

The canonical summary, call-context, and analysis-result types currently use
these values directly. Extracting them together preserves the P5O1 model and
avoids parallel worker-specific string identities or duplicate DTO semantics.
Protocol envelopes, compiler manifests, blob descriptors, limits, statuses,
diagnostics, receipts, and metrics remain separate serialization DTOs because
they are not exception-flow IR.

### 3. Existing P5 types that contain Roslyn objects

The reconstruction handoff is not transportable as-is:

- `ExternalCSharpCompilationConfiguration` stores `CSharpParseOptions` and
  `CSharpCompilationOptions`.
- `ExternalCSharpSyntaxTree` and `ExternalCSharpSyntaxTreeSet` store syntax
  trees.
- `ExternalMetadataReferenceSet` stores Roslyn metadata references.
- `ExternalSupportingSourceCompilation` stores a `CSharpCompilation`.
- `SemanticCompilationScope`, `SupportingSourceCatalog`, and
  `ProjectClosureSemanticContext` store compilations, syntax trees, semantic
  models, project identities, and symbols.
- The active exception-flow result and summary graph store Roslyn symbols.

The P5 provenance descriptors for validated PE/PDB/source/reference material
are the correct evidence source, but transport must project them to immutable
hash-, identity-, ordinal-, option-, and content-addressed DTOs. It must not
serialize the reconstructed active-Roslyn objects.

### 4. Required transport adapters

The parent needs adapters from validated P3-P5 provenance to a neutral worker
request. The worker needs historical-Roslyn adapters that reconstruct parse and
compilation options, syntax trees, references, signing identity, callable
identity, and call context from that request. The response adapter must emit
only the existing canonical exception-flow IR plus a neutral provenance
receipt. The main process must validate the receipt before accepting a result.

### 5. Analyzer logic that must run in the worker

Historical Roslyn must own parsing, binding, exact callable/type/member
rebinding, body discovery, semantic-model access, summary-graph construction,
catch hierarchy, context-sensitive flow, and canonical result creation. None
of those operations can safely use active-Roslyn objects.

### 6. Logic that can remain in the main process

Compiler acquisition, P3-P5 artifact validation, policy and demand decisions,
content-addressed workspace preparation, process supervision, timeout and
output enforcement, response validation, receipt validation, and integration
with the active finding pipeline can remain in the main process. The active
in-process analyzer and its Roslyn-based graph equality remain unchanged.

### 7. Analyzer reuse without a fork

The analyzer is not presently a reusable library boundary. The flow directory
contains 59 `ExceptionFlowAnalyzer` partial files with 30,859 lines, 11
additional active graph/support files with 1,516 lines, and 15 canonical files
with 4,086 lines. The analyzer also directly consumes:

- `ProjectClosureSemanticContext` and `SemanticCompilationScope`;
- `SupportingSourceSymbolResolver` and `CrossCompilationSymbolResolver`;
- active result/path models;
- known-framework and external-documentation models; and
- `SyntaxUtils`.

The analyzer uses only `TryGetSemanticModel` and
`GetAnalysisCompilationScopes` directly on the context, but supporting-source
resolution reaches the P3-P6 acquisition and registration pipeline through
that context. Referencing the main executable therefore imports both this
infrastructure and its active Roslyn package graph.

The existing P5N diagnostic does exactly that: its project references
`XMLDocNormalizer.csproj`, while historical Roslyn is operated through
reflection in a collectible load context. It proves isolation and type
incompatibility, but it is not a type-safe production analyzer boundary.

## Dependency graph

Current graph:

```text
XMLDocNormalizer (net8 executable)
  -> Microsoft.CodeAnalysis 5.0.0
  -> Microsoft.CodeAnalysis.CSharp 5.0.0
  -> Workspaces/MSBuild packages
  -> P3-P6 acquisition and semantic context
  -> ExceptionFlowAnalyzer
  -> canonical IR (internal)

P5NDiagnostic
  -> ProjectReference XMLDocNormalizer
  -> active Roslyn in default context
  -> historical Common/CSharp through reflection in a collectible ALC
```

A worker project reference to `XMLDocNormalizer` would violate the no-active-
Roslyn invariant. Copying or independently adapting the 59 analyzer partials
would create the prohibited analyzer fork. Reflection would violate the typed
analyzer requirement.

## Strategy comparison

- Strategy A, a neutral worker plus reflection-loaded Roslyn, is suitable only
  for provenance/bootstrap diagnostics. It cannot host the typed analyzer.
- Strategy B, a version-bound worker compiled against an exact historical
  Roslyn set, is the correct execution model, but it first needs a shared
  analyzer source boundary and neutral protocol assembly.
- Strategy C, compiling the same analyzer sources into active and historical
  Roslyn-bound assemblies, avoids a semantic fork. It is the preferred general
  architecture, provided the context and resolver dependencies are extracted
  behind one small version-bound host abstraction.

## Historical compiler evidence retained from P5N

The validated net8 compiler pair is:

- `Microsoft.CodeAnalysis.dll`: informational version
  `5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0`, MVID
  `dc7738cc-6dca-4d34-9c44-29b53a7caa93`, SHA-256
  `660C3D626C4B8F4CF8C231FBEF0FB6B4DB4FFFCC89EF5B31AAECA1CF4D7F66A1`.
- `Microsoft.CodeAnalysis.CSharp.dll`: the same informational version, MVID
  `0f9c1dcf-4eb1-47f8-81b2-733db5887be7`, SHA-256
  `B0EC1DDCA4C97DCF15845FF4BCEB5499C3989025197D5D65E04310E29C09217D`.

P5N also observed runtime dependencies on `System.Collections.Immutable` and
`System.Reflection.Metadata` and a larger .NET runtime assembly closure. That
closure was supplied by the diagnostic host, not captured as a production
worker manifest. P5N therefore does not yet satisfy P5O2 dependency-closure,
mixed-set, or runtime-receipt requirements.

## Minimal follow-up architecture

The smallest safe sequence is:

1. **P5O2A — neutral contracts and analyzer seam**
   - Extract the existing canonical sources and their four non-Roslyn value
     dependencies into a Roslyn-free contract assembly.
   - Add protocol, manifest, request/response, status, blob, limit, and receipt
     DTOs there.
   - Introduce a narrow Roslyn-bound semantic-context/resolution abstraction
     covering semantic-model lookup, analysis scopes, supporting-source method
     resolution, and stable-member resolution.
   - Keep active summary-graph equality unchanged.
2. **P5O2B — single-source, dual-version analyzer build**
   - Move the existing analyzer implementation to one shared source ownership
     boundary.
   - Compile that same source once against active Roslyn and once inside a
     version-pinned worker against the exact manifest-selected historical
     Roslyn dependency closure.
   - The worker must not reference the main executable.
3. **P5O2C — safe host and semantic equivalence**
   - Add offline content-addressed IPC, path and size enforcement, process and
     crash supervision, provenance receipts, and exact response validation.
   - Prove the eight active-versus-worker fixtures before attempting S1.
   - Only then enable the opt-in demand-driven historical path and S1 probes.

This is a material refactor because it changes project ownership and the
analyzer's semantic-context dependency, even though it need not change analyzer
semantics. It should be reviewed and validated independently before worker
execution is introduced.

## Result

P5O2 is **not implemented**. No compiler manifest, protocol version, worker
process, policy, workspace, receipt, historical callable rebinding, equivalence
test, or S1 probe exists. `BND-P6-002` remains planned. The newly identified
dependency-separation prerequisite is tracked as `BND-P6-003`.
