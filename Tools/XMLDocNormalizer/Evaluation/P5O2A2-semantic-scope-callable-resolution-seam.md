# P5O2A2 Semantic Scope and Callable Resolution Seam

P5O2A2 separates the Roslyn-bound exception-flow analyzer from Main-owned
project-closure and P5/P6 acquisition infrastructure without changing the
active Roslyn type universe, graph identity, source ownership, or finding
semantics. The neutral core remains a source/build seam; no runtime core
`ProjectReference`, worker, historical Roslyn build, IPC, or source fork was
introduced.

## Repository gate and baseline

- Starting HEAD: `40160bc411d353fe72d18e1eaf44aa6168600ee7`.
- Starting commit: `40160bc411 Decompose exception flow analyzer dependencies.`
- P5O2A was committed before this package began.
- The pre-existing `../../.gitignore` modification was not touched.
- The protected stashes were not applied or changed.
- P5O2A baseline: 55 analyzer partials, 30,124 nonblank analyzer
  lines, four extracted responsibility classes with 763 nonblank lines,
  2,496 tests, 108 focused tests, and self-analysis `16` findings
  (`DOC611=1`, `DOC631=15`).

P5O2A's rejected runtime-core experiments remain authoritative. Moving the
neutral sources behind an ordinary runtime assembly boundary changed
self-analysis from 16 to 70 findings; general referenced-project rebinding
changed it to 129. Both converted same-compilation source calls into
metadata/referenced-project calls and lost value-fact precision. P5O2A2 does
not repeat either experiment.

## Focused dependency audit

Counts below are the pre-P5O2A2 direct analyzer-source counts. A “call” is an
actual member or resolver invocation; type occurrences include parameter,
field, local, and generic occurrences.

| Current dependency | Analyzer files / occurrences | Used members or operations | Semantic responsibility | Roslyn-bound | Main-only | Historical build needed | State / lifetime | P5O2A2 owner |
|---|---:|---|---|---|---|---|---|---|
| `ProjectClosureSemanticContext` | 26 / 112 | `TryGetSemanticModel` (16 calls), `GetAnalysisCompilationScopes` (1 call) | syntax-tree semantic model and enumeration of source-backed analysis compilations | Yes | Concrete type and acquisition state: yes | Capabilities: yes | context-local semantic-model and combined-scope caches; per analysis run | `ExceptionFlowSemanticEnvironment`; its Main partial implementation retains the context |
| `SemanticCompilationScope` | 3 / 15 | `Compilation`, `SourceTypes` (10 member reads); `Kind` and `ProjectId` were not used by the analyzer | one compilation universe and its deterministic source-type set | Yes | Workspace role and `ProjectId`: yes | Compilation/source types: yes | per compilation; lazy source-type cache | `ExceptionFlowSemanticScope`; Main scope owns role and project identity |
| `SupportingSourceSymbolResolver` | 3 / 6 | exact registered resolution, binding-compilation-aware exact resolution, external supporting-scope lookup | demand-driven exact source body selection | Yes | Acquisition/catalog implementation: yes | Capability: yes | no own cache; delegates to context-local P6 catalogs | Main adapter capability implementation |
| `CrossCompilationSymbolResolver` | 3 / 7 | `ResolveMethod`, `ResolveNamedType`, `ResolveMethodOnContainingType`, `ResolveStableMember` | Roslyn-local exact rebinding inside an explicitly supplied destination compilation | Yes | No; its old placement was incidental | Yes | stateless | `ExceptionFlowCrossCompilationResolver` in analyzer ownership |

The analyzer did not use registration, project identity, reporting roles,
binary discovery, Portable PDB/source acquisition, remote policy,
reconstruction plans, or catalog mutation directly. Those remain Main-owned.
The only indirect route to P3-P6 infrastructure was binding-compilation-aware
supporting-source lookup.

## Callable-resolution responsibility matrix

| Capability | Input | Output | Exactness and fail-closed behavior | State owner |
|---|---|---|---|---|
| Semantic-model lookup | `SyntaxTree` | same-universe `SemanticModel` | unknown trees return `false`; no fallback compilation | wrapped project-closure context |
| Analysis-scope enumeration | analysis environment | compilation-bound analyzer scopes | preserves the context's deterministic snapshot and supporting-source invalidation | Main context; no projection cache |
| Registered supporting-source resolution | metadata `IMethodSymbol` | source method plus owning scope | full assembly identity, documentation identity, unique declaration, source ownership | Main supporting catalog; no analyzer cache |
| External supporting-source resolution | method, binding `Compilation` | source method plus owning scope | exact P3 binary identity and demand-driven P6 attempt; missing/mismatch returns `false` | Main P5/P6 context |
| Cross-compilation method/type/member rebinding | symbol, explicit destination `Compilation` or containing type | destination symbol or `null` | documentation identity, full assembly identity, exact original definition, unique result | stateless analyzer component |
| Target registration | requested target/context plus environment | graph key | source target is used only after exact resolution and ownership validation | stateless registrar; graph owns nodes/context |
| Runtime dispatch expansion | callable and receiver constraints plus scopes | exact known source targets and structured uncertainty | scope-local rebinding; incomplete target sets remain uncertain | existing analyzer dispatch cluster |

## Chosen seam

`ExceptionFlowSemanticEnvironment` models four cohesive per-analysis
capabilities rather than mirroring four existing Main classes:

1. obtain a semantic model for an owned syntax tree;
2. enumerate analyzer-visible source compilation scopes;
3. resolve an exact supporting-source method, with or without an explicit
   binding compilation; and
4. obtain an exact external supporting scope for runtime-target discovery.

The analyzer-owned declaration is an empty sealed partial class. The active
Main build supplies the four ordinary, non-virtual operations in a same-type
partial implementation. A future historical host can supply the equivalent
implementation while compiling the unchanged analyzer source in its own
Roslyn universe. This is deliberately a compile-time host seam rather than an
interface/runtime-dispatch seam: the latter added seven self-analysis
uncertainties even though its behavior was otherwise correct. The class
contains no acquisition DTOs, catalogs, project identities, or general service
lookup. Roslyn types intentionally remain in the seam because each analyzer
build is compiled wholly within one Roslyn universe.

`ExceptionFlowSemanticScope` is a small Roslyn-bound scope containing only a
`Compilation` and its lazy deterministic source-type set. These values share
one responsibility, owner, and per-compilation lifetime. It is not a renamed
project-closure context. `ExceptionFlowSupportingSourceMethod` couples the two
outputs of one atomic exact resolution: the method and the scope that owns it.
It cannot represent metadata-only or ambiguous success.

`ProjectClosureExceptionFlowSemanticEnvironment.cs` contains the active Main
partial implementation. It retains all concrete
`ProjectClosureSemanticContext`, `SemanticCompilationScope`, and
`SupportingSourceSymbolResolver` knowledge. `SemanticCompilationScope` derives
from the analyzer-owned scope, so the existing deterministic context snapshot
is returned covariantly without projection, copying, or a second source-type
cache. Supporting-catalog version invalidation is unchanged. The adapter adds
no symbol-resolution cache and does not eagerly reconstruct source.

`ExceptionFlowSupportingSourceMethod` retains one exact method together with
its owning scope. Its scope accessor has an explicit trivial body: a bodyless
auto-property became one additional uncertainty in self-analysis, whereas the
explicit getter preserves the same immutable value and is analyzable. This is
the only correction required after the exact uncertainty-set comparison.

The old cross-compilation resolver was not abstracted. Its operations are
purely Roslyn-bound analyzer mechanics, so ownership moved to
`ExceptionFlowCrossCompilationResolver`. Resolution now requires one unique
assembly-identical result instead of accepting the first matching candidate.
No name-only, display-string, simple-assembly-name, fuzzy, or first-member
fallback exists.

## Semantic origins and same-compilation protection

The implementation preserves four behaviorally distinct outcomes without
inventing an origin enum that no analyzer decision consumes:

- **Same compilation:** a source-backed selected method remains the original
  Roslyn symbol. Supporting-source resolution is not attempted, so its source
  body, semantic model, call-site value facts, context, and graph identity are
  unchanged.
- **Referenced project:** a source symbol remains owned by its referenced
  project compilation. Semantic-model lookup follows the syntax tree to that
  exact compilation. No general cross-project rebinding was added.
- **Supporting source:** metadata is replaced only by a successful exact
  supporting-source result carrying the owning scope; call context is rebound
  by parameter ordinal and exact stable-member identity.
- **Metadata only:** unsuccessful or unavailable source lookup leaves the
  requested metadata target intact and body analysis unavailable. The
  existing XML evidence and uncertainty behavior remains authoritative.

The distinction is embodied by symbol ownership, syntax references, and the
explicit supporting-source result. A generic `ResolvedCallable` union was
rejected because it could collapse same-compilation and metadata behavior.

## Exact resolution audit

- Declaration/reference documentation IDs provide structured Roslyn identity;
  display strings are used only for deterministic ordering or diagnostics.
- Full `AssemblyIdentity`, not simple assembly name, filters candidates.
- Multiple distinct assembly-identical candidates now return `null`.
- Overloads remain separated by declaration identity.
- Constructed generic containing types are rebound and retain substitution.
- Instance constructors and operators use the same exact declaration path.
- Property getter/setter and event add/remove accessors resolve through their
  associated declaration and requested `MethodKind`.
- Reduced extension selection remains in invocation-context logic: the
  unreduced method is used only when its source declaration resolves exactly.
- Explicit-interface implementations resolve by their declaration identity;
  no metadata-name approximation was introduced.
- Stable call-context members remain restricted to fields and properties.
- Multi-module ambiguity is fail closed through unique-result enforcement;
  the active resolver does not invent a separate module-name fallback.

Static constructors are not independently exposed by the existing metadata
test compilation surface and therefore have no new special path. P5O2A2
preserves the resolver's supported constructor surface rather than adding a
name-based `.cctor` fallback.

## Dependency graph

Before:

```text
ExceptionFlowAnalyzer / ExceptionFlowSummaryTargetRegistrar
  -> ProjectClosureSemanticContext
  -> SemanticCompilationScope
  -> SupportingSourceSymbolResolver
  -> CrossCompilationSymbolResolver
  -> Main semantic, project, and P5/P6 infrastructure
```

After:

```text
Exception-flow analyzer components
  -> ExceptionFlowSemanticEnvironment (analyzer-owned partial declaration)
  -> ExceptionFlowSemanticScope
  -> ExceptionFlowSupportingSourceMethod
  -> ExceptionFlowCrossCompilationResolver

ExceptionFlowSemanticEnvironment (Main partial implementation)
  -> ProjectClosureSemanticContext
  -> SemanticCompilationScope
  -> SupportingSourceSymbolResolver
  -> P5/P6 acquisition and catalogs
```

No dependency cycle is introduced: Main semantic infrastructure already
hosts the active analyzer and now depends on the analyzer-owned capability
contract, while analyzer sources contain no `Execution.Semantic` dependency.

## Alternatives rejected

- Four one-to-one interfaces were rejected because they would preserve the
  accidental Main class decomposition rather than analyzer capabilities.
- A record containing all four old services was rejected as a god context.
- Delegates for every Roslyn operation were rejected as excessive and would
  hide ordinary analyzer-local Roslyn work.
- Generic symbol universes were rejected because active and historical builds
  compile the same source separately against compatible Roslyn API surfaces.
- Canonical callable identity was not substituted for active graph equality or
  local Roslyn binding; P5O1 proved that such a change can alter findings.
- Runtime core references, broad referenced-project rebinding, reflection,
  worker implementation, historical adapters, compiler-version `#if`, and
  source forks remain out of scope.

## State, lifetime, caching, and thread safety

- One active adapter belongs to one `ProjectClosureSemanticContext` and one
  analysis run/session.
- Semantic models and P5/P6 attempt/catalog state remain context-local and
  retain their existing cache keys and invalidation.
- `ExceptionFlowSemanticScope` is per compilation and owns the source-type
  cache formerly located in `SemanticCompilationScope`; the Main scope
  delegates to the same analyzer-scope instance, so no duplicate cache exists.
- Cross-compilation resolution and target registration remain stateless.
- No static mutable cache or global canonical-identity-to-symbol map was added.
- The existing summary session remains intentionally sequential. The adapter
  has the same effective thread-safety assumptions as its wrapped context.

## Structural result

- Analyzer partials: 55 -> 55.
- Nonblank analyzer-partial lines: 30,124 -> 30,092.
- Direct analyzer references:
  - `ProjectClosureSemanticContext`: 26 files / 112 occurrences -> 0 / 0.
  - `SemanticCompilationScope`: 3 files / 15 occurrences -> 0 / 0.
  - `SupportingSourceSymbolResolver`: 3 files / 6 calls -> 0 / 0.
  - `CrossCompilationSymbolResolver`: 3 files / 7 calls -> 0 / 0.
- New analyzer types: one compile-time capability host, one semantic scope, one
  exact supporting-source result, and one analyzer-owned cross-compilation
  resolver.
- New Main type: none; one active partial implementation completes the
  analyzer-owned host type.
- Nonblank lines: capability host 18, scope 98, supporting result 48,
  cross-compilation resolver 254, active Main partial implementation 138.
- No new project, package reference, project reference, source copy, source
  fork, compiler conditional, service locator, or DI framework.

## Characterization and dependency guards

The pre-change focused slice passed 57/57. P5O2A2 adds characterization for:

- exact overload and explicit-interface rebinding;
- ambiguous declaration rejection;
- analyzer-scope compilation/model ownership and unrelated-tree failure;
- exact supporting-source method plus owning scope;
- same-compilation registrar target/context preservation;
- unresolved metadata registrar target/context preservation; and
- a parsed-source dependency guard forbidding the four former Main blockers,
  Main semantic namespace imports, project identity, and supporting catalogs
  from the analyzer source boundary.

Existing coverage remains authoritative for generic containing-type
substitution, constructors, property/event accessors, operators, stable-member
context rebinding, reduced extensions, registered/missing/mismatched supporting
source, source-body use, metadata-only fallback, referenced projects, runtime
dispatch, and all four analysis modes.

The neutral-core assembly-reference guard and P5O1 canonical round-trip and
Roslyn-free guards remain unchanged.

## Finding-regression audit

The first post-seam self-analysis produced 108 findings (`DOC610=3`,
`DOC611=90`, `DOC631=14`). Root cause was three newly introduced internal
`ArgumentNullException.ThrowIfNull` calls in the scope, resolution-result, and
adapter constructors. Those new production throw edges propagated through the
transitive analyzer. The checks were removed because all construction is
internal, NRT-non-null, and already guarded by existing ownership invariants;
no finding suppression or resolver relaxation was used. The next run returned
the exception-flow counts to `DOC610=0`, `DOC611=1`, `DOC631=15`, `DOC632=0`.

The first capability shape was a four-operation interface. It preserved the
finding IDs but added seven runtime-dispatch uncertainties to the existing
`FindExceptionSmells` DOC631 evidence. A bodyless partial-method variant then
hid host method bodies from source analysis and was also rejected. The final
sealed partial host uses ordinary non-virtual methods supplied by Main, so no
runtime-dispatch edge is introduced.

An exact baseline/current dump of all uncertainties then isolated one remaining
delta: the new `ExceptionFlowSupportingSourceMethod.Scope` auto-property getter
had no source body for the analyzer to inspect. Giving that immutable accessor
an explicit trivial body removed precisely that additional uncertainty. The
final canonical comparison is `16 -> 16`, `0` added, `0` removed, and `0`
changed evidence; no finding suppression or precision relaxation was used.

This was a deterministic semantic regression and correction, not a process
crash. No new modal error, native crash, managed crash, or flake was observed.
Historical crash observations remain separate: four unattributed P5O1
`dotnet.exe` `0xE0434352` dialogs at `0x00007FF9E77B41CA`, one unreproduced
P5O1 `System.AccessViolationException` in
`WithUsingNamespacesAndTypesBinder.GetExtensionDeclarations`, the P5N
diagnostic `0xE0434352` observation, and an earlier separate write to address
zero. No common cause is asserted.

## Remaining Main dependencies and next boundary

The semantic/callable blockers are removed from analyzer sources, but P5O2B is
not ready. Active graph/result/body-discovery types and the large summary
construction/evaluation and context/value-fact/catch/local-source clusters
remain compiled in the executable. Final assembly composition must still
preserve same-compilation value facts.

The next package is P5O2A3: separate summary construction and evaluation while
retaining active Roslyn graph equality. P5O2A4 remains necessary for context,
value facts, catch semantics, and local source analysis before the same complete
analyzer source set can safely be built against historical Roslyn in P5O2B.

## Final validation

- Build: `dotnet build .\XMLDocNormalizer.sln -warnaserror --no-restore`
  completed with zero warnings and zero errors.
- Direct seam/resolver/registrar slice: 33/33.
- Broader semantic, supporting-source, same-compilation, and metadata-only
  slice: 165/165.
- Four-modes-oriented slice: 101/101. Earlier WIP gates also passed the
  dedicated 110/110 modes slice and the grouped P5/P6/G3A/G4B/G5 slice at
  806/806.
- Full suite: 2,504/2,504, zero failed, zero skipped (baseline 2,496 plus eight
  P5O2A2 tests).
- Final solution-transitive self-analysis: 16 findings (`DOC610=0`,
  `DOC611=1`, `DOC631=15`, `DOC632=0`) in 57,945 ms. The isolated HEAD
  baseline was 16 findings in 63,950 ms. Timing samples varied materially, so
  no performance conclusion is drawn.
- Canonical finding diff: zero added, zero removed, zero changed evidence.
- E1 (`SourceLink=enabled`, `VerifiedLineEndings`, `BoundedRemoteArtifacts`):
  seven candidates, five complete reconstructions, two expected fail-closed,
  zero unexpected, zero potential bugs. Dapper remains fail closed at
  `ConfigurationReconstructed / ConfigurationUnsupported` for
  `optimization=release-debug-plus`; OneOf remains fail closed at
  `PdbValidated / MissingArtifact`.
- `git diff --check` passed. Changed C# sources are CRLF-only UTF-8 without a
  BOM or trailing whitespace and contain no `var` declarations.
- No new native or managed process crash was observed. The first sandboxed E1
  attempt could not download Source Link documents and therefore reported five
  environmental `SourceUnavailable` outcomes; the identical network-enabled
  bounded run produced the expected 5/2/0/0 result.

BND-P6-003 remains **Under Investigation** because P5O2A3/P5O2A4 and final
assembly composition are still required before P5O2B.
