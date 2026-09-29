# P5O2A4C – Residual Downstream Fact Family Extraction

## Result

This continuation reproduced the authoritative 92-method, 16-family residual
closure and extracted 28 methods from seven dependency-safe families. The
63-method recursive contextual-fact SCC remains intact.

| Measurement | Before | After |
| --- | ---: | ---: |
| Analyzer-owned downstream methods | 92 | 64 |
| residual fact families | 16 | 9 |
| SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC → residual Analyzer edges | 68 | 38 |
| SCC → provider edges | 20 | 50 |
| SCC → SemanticScope edges | 18 | 18 |
| provider → Analyzer edges | 0 | 0 |
| residual Analyzer → provider edges | 6 | 11 |
| residual Analyzer → SemanticScope edges | 9 | 4 |

The primary indicator therefore improves by 30 edges, and the secondary
indicator improves by 28 methods. No method of the recursive SCC moved.

The package stops at a natural family boundary. The five-method stable
source-member family still calls the Analyzer-owned
`RequiresSummaryRuntimeDispatch` helper outside the 252-method inventory.
Moving it would create the forbidden provider-to-Analyzer back edge, and the two
call-context projection methods depend on that family. The other residual
families are either the heterogeneous return/condition family or are attached
to the sequence context/element/source condensation cycle.

## Repository gate and scope

- Initial HEAD: `e0f362f958180f06fbed4a2a3f7a8c643eeaa9fb`
  (`Separate semantic model and value fact resolution.`).
- Initial status: only the protected foreign `../../.gitignore` modification.
- The previous semantic-model/fact slice was committed, so the continuation
  gate passed.
- Scope: dependency-depth analysis of the committed 92-method residual and
  staged extraction of closed fact families only.
- No historical build, worker, IPC, source fork, compiler `#if`, reflection
  bridge, precision feature, summary redesign, catch redesign, local-source
  redesign, commit, push, reset, or stash mutation was performed.
- The protected stashes were not read or changed.

## Complete 16-family classification

The machine-readable details, including source files, exact SCC callers,
incoming/outgoing family dependencies, provider and SemanticScope dependencies,
context/cache/recursion flags, candidate owners, risks, and decisions, are in
`P5O2A4C-residual-fact-family-audit.json`.

| Family | Methods | SCC edges | Classification | Final owner / decision |
| --- | ---: | ---: | --- | --- |
| Call-context projection/defaults | 2 | 2 | intermediate, SCC-bound | Analyzer; depends on stable source-member facts |
| Stable source-member facts | 5 | 0 | leaf-shaped but externally blocked | Analyzer; calls `RequiresSummaryRuntimeDispatch` |
| ConditionalWeakTable value facts | 7 | 4 | freed intermediate | `ExceptionFlowConditionalWeakTableValueFactsProvider` |
| Dictionary value facts | 4 | 4 | intermediate, SCC-bound | Analyzer; feeds sequence cycle |
| Enum value facts | 6 | 7 | leaf | `ExceptionFlowEnumValueFactsProvider` |
| Local-initializer currency | 2 | 3 | leaf | `ExceptionFlowLocalInitializerFactsProvider` |
| Numeric constant facts | 1 | 0 | leaf | existing `ExceptionFlowPrimitiveValueFactsProvider` |
| Known-framework return nullability | 1 | 1 | leaf | existing `ExceptionFlowNullabilityFactsProvider` |
| Return/condition value facts | 8 | 4 | leaf-shaped, heterogeneous | Analyzer; no mixed catch-all provider created |
| Sequence call-context observation | 5 | 4 | cycle member / hub | Analyzer |
| Sequence collection shape | 4 | 11 | shared leaf | `ExceptionFlowSequenceCollectionFactsProvider` |
| Sequence element facts | 12 | 15 | cycle member / hub | Analyzer |
| Sequence range/dictionary mutation | 9 | 7 | intermediate, SCC-bound | Analyzer |
| Sequence source preservation | 5 | 1 | cycle member / hub | Analyzer |
| Source-position value facts | 7 | 4 | intermediate freed by two leaves | `ExceptionFlowSourcePositionValueFactsProvider` |
| Successful sequence validation | 14 | 1 | hub, SCC-bound | Analyzer |

All 16 families create zero callee contexts and require no new cache. Context is
read only by the call-context projection, dictionary, sequence-context, and
source-position families. The extracted source-position provider consumes
`ExceptionFlowCallContext` as data and does not orchestrate recursive
evaluation.

## Family dependency graph

The pre-extraction family graph is:

```text
Call-context projection/defaults
  -> Stable source-member
       -> Enum
       -> Local-initializer

ConditionalWeakTable -> Enum
Source-position -> Local-initializer
Source-position -> Numeric
Dictionary -> Sequence context
Sequence mutation -> Sequence element
Sequence mutation -> Sequence collection
Successful sequence -> Sequence context
Successful sequence -> Sequence element
Successful sequence -> Sequence source

Sequence context -> Sequence element
Sequence element -> Sequence source
Sequence source -> Sequence context
Sequence source -> Sequence element

Sequence element -> Sequence collection
Sequence source -> Sequence collection
```

The graph is not itself acyclic at the coarse family level. Its condensation
DAG contains one pre-existing three-family node:

```text
{Sequence context, Sequence element, Sequence source}
```

Enum, Local-initializer, Numeric, Known-framework return nullability, Return/
condition, and Sequence collection were initial leaves. Source-position became
a leaf after Local-initializer and Numeric moved. ConditionalWeakTable became a
leaf after Enum moved. Stable source-member became leaf-shaped after Enum and
Local-initializer moved, but its direct out-of-inventory Analyzer dependency
prevents component extraction.

## Extraction order and slices

1. **Known-framework return nullability.** One exact framework classification
   method moved to the existing nullability owner. It removes one SCC edge.
2. **Numeric constant facts.** `TryGetInt32Constant` moved to the existing
   primitive owner. It frees Source-position without adding a new class.
3. **Enum value facts.** Six stateless syntax/type/constant helpers moved to a
   cohesive enum provider. It removes seven SCC edges.
4. **Local-initializer currency.** Two stateless validity queries moved to a
   provider that consumes the established SymbolUsage, Dereference, and
   DataFlow owners. It removes three SCC edges.
5. **Sequence collection shape.** Four stateless type/invocation/source-shape
   helpers moved to a cohesive provider. It removes eleven SCC edges.
6. **Source-position value facts.** Seven mutually cohesive methods moved only
   after their Numeric and Local dependencies had provider owners. They consume
   context as input and remove four SCC edges.
7. **ConditionalWeakTable helpers.** Seven stateless contract/invocation/
   callback-syntax helpers moved after Enum. They remove four SCC edges. The
   recursive callback evaluation and cache remain Analyzer-owned.

Every slice built with warnings as errors, passed its focused characterization,
and retained the 16-finding self-analysis baseline. One intermediate CWT run
reported five DOC110 findings because five newly introduced partial
declarations lacked XML documentation. Those declarations were documented
within the same slice; the repeated build and self analysis returned to the
exact baseline. This was neither a semantic suppression nor a finding waiver.

## Production ownership

Existing providers reused:

- `ExceptionFlowNullabilityFactsProvider`;
- `ExceptionFlowPrimitiveValueFactsProvider`;
- `ExceptionFlowArgumentMapper`;
- `ExceptionFlowSymbolUsageFacts`;
- `ExceptionFlowDereferenceFactDiscovery`;
- `ExceptionFlowDataFlowFactsProvider`;
- `ExceptionFlowGuardFactsProvider`; and
- `ExceptionFlowSemanticScope`.

New provider owners:

- `ExceptionFlowEnumValueFactsProvider`;
- `ExceptionFlowLocalInitializerFactsProvider`;
- `ExceptionFlowSequenceCollectionFactsProvider`;
- `ExceptionFlowSourcePositionValueFactsProvider`; and
- `ExceptionFlowConditionalWeakTableValueFactsProvider`.

All are static, sealed by the runtime, nonvirtual, and implement no interfaces.
No delegate/callback injection, service locator, mutable provider state, new
cache, or Analyzer back reference was introduced. The CWT provider is split
across three documented partial declarations in the existing source file so
the unchanged Analyzer-owned recursive computation and cache stay physically
and semantically together; a Roslyn-based architecture guard inspects every
provider declaration for Analyzer references.

Changed production sources comprise the two extended providers, five new
provider owners, removal of the two now-empty Analyzer partial files, the CWT
ownership split, and direct qualification of all affected call sites. No
forwarding Analyzer wrapper remains.

## Cache and semantic boundaries

| Concern | Owner | Status |
| --- | --- | --- |
| ConditionalWeakTable invariant cache | `ExceptionFlowAnalyzer` | unchanged weak `SemanticModel` partition, normalized field-symbol key, private partition lock |
| Data-flow cache | `ExceptionFlowDataFlowFactsProvider` | unchanged |
| successful-dereference cache | `ExceptionFlowDereferenceFactDiscovery` | unchanged |

Cache keys, values, query modes, weak lifetimes, locks, invalidation, and
compilation isolation did not change.

`ExceptionFlowSemanticScope` remains the sole compilation-local
`SyntaxTree -> SemanticModel` owner. `ExceptionFlowSemanticEnvironment`
remains the cross-scope owner. No new resolver or scope search was added.
SameCompilation, ReferencedProject, SupportingSource, and MetadataOnly behavior
are unchanged. Direct Analyzer references to `ProjectClosureSemanticContext`,
`SemanticCompilationScope`, `SupportingSourceSymbolResolver`, and
`CrossCompilationSymbolResolver` remain zero. Neutral and canonical cores
remain Roslyn-free.

## Structural measurements

- Analyzer partial files: 45 -> 43.
- File-based nonblank SLOC across files containing Analyzer partials:
  22,799 -> 21,775.
- Owner-adjusted Analyzer nonblank SLOC excluding the 296 co-located CWT
  provider lines: 21,479.
- Provider-owned nonblank source represented by this continuation: 1,403 lines
  including documentation and the co-located CWT provider sections.
- Extracted methods in this continuation: 28.
- Cumulative methods extracted from the original 124-method Analyzer closure:
  60; with 39 pre-existing downstream component methods, 99 of 163 downstream
  nodes now have non-Analyzer owners.
- Final SCC direct residual targets: 27 methods through 38 edges.
- New production interfaces, virtual methods, callbacks, services, caches, or
  mutable provider fields: zero.
- New behavior tests: zero; existing characterization was sufficient.
- New architecture tests: one, bringing the normal suite from 2,522 to 2,523.

## Validation

| Validation | Result |
| --- | --- |
| pre-change focused baseline | 49/49 |
| final focused touched-family slice | 50/50 |
| architecture guards | 6/6 |
| explicit four-mode slice | 8/8 |
| P5/P6/G + Canonical + Semantic slice | 469/469 |
| full suite | 2,523/2,523; 0 failed; 0 skipped |
| warning-as-error build | 0 warnings; 0 errors |
| final self analysis | 16 findings in 60,622 ms |
| finding distribution | DOC610=0, DOC611=1, DOC631=15, DOC632=0 |
| canonical finding/evidence diff | 0 added, 0 removed, 0 changed |
| exact normalized finding arrays | equal to committed P5O2A4C semantic baseline |
| canonical baseline hash | `15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B` |
| E1 canonical profile | Source Link enabled; verified-line-endings; bounded-remote |
| E1 canonical result | 7 candidates; 5 complete; 2 expected fail closed; 0 unexpected; 0 potential bugs |

Direct, ProjectTransitiveDeclaredExceptions, ProjectTransitive, and
SolutionTransitive are green in the explicit slice and full suite. The broad
slice covers the requested P5G/P5I/P5J/P5K/P5L, P6A/P6B/P6C, G3A/G4B/G5,
Canonical, Dependency, Fact Provider, and Semantic Scope paths.

The first sandboxed E1 attempt reproduced the known environmental result:
0 complete, two expected fail-closed cases, and five unexpected
`SourceUnavailable` outcomes. The network-enabled canonical repeat produced
5/2/0/0.

Dapper remains `ConfigurationReconstructed / ConfigurationUnsupported` for
the exact manifest value `optimization=release-debug-plus`. OneOf remains
`PdbValidated / MissingArtifact`. Semver retains its one expected
source-backed added finding. `Program.Main` remains the sole DOC611 finding.
The known `IOException`/BCL uncertainty remains DOC631. No performance claim
is made from the individual self-analysis durations.

No native crash, managed crash, modal dialog, or test flake was observed.

`git diff --check -- .` passes. All 20 changed task C# files are CRLF-only
UTF-8 without BOM, bare LF, bare CR, trailing whitespace, or added `var`
declarations. The Markdown and JSON artifacts have no trailing whitespace and
the JSON parses successfully.

The initial status contained no Core build outputs. Validation regenerated
seven tracked files under `XMLDocNormalizer.ExceptionFlow.Core/bin` and `obj`.
An exact cleanup was attempted only for those seven paths, but repository index
write access was denied and the escalation was rejected because an earlier
explicit instruction forbids `git restore`. No workaround was used; the seven
build-churn paths therefore remain visible in the final status. They contain no
production-source change.

## SCC and P5O2B readiness

The 63-method SCC and its 112 internal edges are unchanged. Its outgoing shape
is now 38 edges to residual Analyzer methods, 50 to providers, and 18 to
SemanticScope. This is substantial separation but not an acyclic extraction
boundary.

P5O2B is **not ready**. The Roslyn-bound Analyzer source set still cannot be
composed against both active and manifest-pinned historical Roslyn without a
Main executable reference, source copy/fork, reflection bridge, compiler
version `#if`, or SameCompilation fact loss.

The single smallest next blocker is:

> `GetStableNonNullMemberFactsFromDirectSourceInvocation` still depends on
> Analyzer-owned `RequiresSummaryRuntimeDispatch` outside the 252-method
> inventory, preventing extraction of the five-method Stable source-member
> family and its two dependent call-context projection methods.

`BND-P6-003` therefore remains **Under Investigation**. The recommended next
step is to assign the exact runtime-dispatch classification to an acyclic
existing/new owner, then re-evaluate Stable source-member and Call-context
projection. The heterogeneous Return/condition family and the three-family
sequence condensation cycle must still be decomposed without a mixed provider
or callback. Only after the remaining 38 SCC-to-Analyzer edges are eliminated
should the intact SCC move to a contextual fact evaluator and P5O2B begin.

## Close-out matrix

1. HEAD: `e0f362f958180f06fbed4a2a3f7a8c643eeaa9fb`.
2. Initial status: protected foreign `.gitignore` only.
3. Scope: residual family DAG plus bounded leaf extraction.
4. Baseline/final downstream: 92/64.
5. Baseline/final families: 16/9.
6. Family classification: complete above and in JSON.
7. Family DAG and condensation cycle: complete above.
8. Leaf/intermediate/hub/SCC-bound families: recorded above.
9. Extraction order and rationale: recorded above.
10. Extracted methods/families: 28/seven.
11. Provider -> Analyzer: 0 -> 0.
12. Analyzer -> Provider: 6 -> 11.
13. SCC -> Analyzer: 68 -> 38.
14. SCC -> Provider: 20 -> 50.
15. SCC -> SemanticScope: 18 -> 18.
16. CWT/DataFlow/Dereference cache owners: unchanged.
17. Cache keys/lifetimes/locks/isolation: unchanged.
18. SemanticScope/SemanticEnvironment ownership: unchanged.
19. SameCompilation/ReferencedProject/SupportingSource/MetadataOnly: unchanged.
20. Component cycles introduced: zero.
21. Interfaces/callbacks/virtuality/service locators: zero.
22. Provider back references: zero.
23. Partials: 45 -> 43.
24. Analyzer SLOC: 22,799 -> 21,775 file-based; 21,479 owner-adjusted.
25. Tests: 2,522 -> 2,523.
26. Full suite/build/self analysis/E1: green as reported.
27. Canonical diff: zero; baseline hash unchanged.
28. Format: diff check and all C# line-ending/style gates pass; final status is
    reported at handoff.
29. BND-P6-003: Under Investigation.
30. P5O2B readiness: no.
31. Exact smallest blocker: runtime-dispatch classification ownership stated
    above.
32. Commit/push/stash/reset: none.

