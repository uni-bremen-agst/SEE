# P5O2A4C – Runtime Dispatch and Stable Source Member Extraction

## Result

The Analyzer-owned `RequiresSummaryRuntimeDispatch` method was a stateless
method-shape classification located in a summary-accessor partial, not summary
graph construction. Its unchanged rule now belongs to the closed
`ExceptionFlowRuntimeDispatchClassifier`. This releases the five-method stable
source-member family into `ExceptionFlowStableSourceMemberFactsProvider` and
the two dependent call-context projections into
`ExceptionFlowCallContextFactProjector`.

| Measurement | Before | After |
| --- | ---: | ---: |
| Analyzer-owned downstream methods | 64 | 57 |
| residual fact families | 9 | 7 |
| SCC methods / internal edges | 63 / 112 | 63 / 112 |
| SCC → Analyzer | 38 | 36 |
| SCC → providers/components | 50 | 57 |
| SCC → SemanticScope | 18 | 18 |
| provider/component → Analyzer | 0 | 0 |

The five new SCC-to-component edges are calls from SCC methods to the dispatch
classifier. The other two replace SCC calls to the two extracted call-context
projection methods. No method of the recursive SCC moved.

## Repository gate and scope

- Initial HEAD: `48ba04dbcca750afdf9b0d4fe557005e3ef29227`
  (`Extract residual exception flow fact providers.`).
- Initial status: only the protected foreign `../../.gitignore` modification.
- The preceding residual-family slice was committed, so the continuation gate
  passed.
- The protected foreign file and both protected stashes were not changed.
- No commit, push, reset, checkout, restore, stash operation, historical build,
  worker, IPC, source fork, reflection bridge, compiler `#if`, precision
  feature, or cache redesign was performed.

## Runtime-dispatch classification audit

The former method accepted one `IMethodSymbol` and returned one Boolean. It
read only `IsStatic`, `IsSealed`, `ContainingType.TypeKind`, `IsAbstract`,
`IsVirtual`, and `IsOverride`. It had no callees and read or wrote no Analyzer
state. It had no semantic environment, semantic scope, summary graph, runtime
target set, callable resolution, or cache dependency.

The exact rule remains:

- static or sealed method: no runtime expansion;
- interface method: runtime expansion;
- abstract, virtual, or non-sealed override method: runtime expansion;
- otherwise: direct target.

Its 14 direct callers are the stable source-member source invocation, four
other recursive source-return/value paths, one dictionary source-helper path,
two residual sequence paths, and six summary edge/completeness paths. The
indirect callers are their existing fact, context, and summary consumers; no
new call path was introduced.

Similar responsibilities remain separate:

- `IsSummaryDispatchTargetSetComplete` proves whether a discovered target set
  is closed;
- `ResolveSummaryRuntimeTargets` and candidate helpers discover targets;
- summary call/accessor/collection/deconstruction/implicit/disposal methods
  construct graph edges;
- callable resolution maps source-backed symbols across exact scopes.

The selected owner is therefore a small stateless runtime-dispatch
classification component, not `ExceptionFlowSummaryGraphBuilder`, a fact
provider, or callable resolution. The class is static (sealed/nonvirtual at
runtime), has no interface, callback, service locator, state, cache, lifecycle,
or Analyzer reference. The old Analyzer method was removed rather than kept as
a forwarding facade.

## Stable source-member facts

The extracted five methods are:

1. `GetStableNonNullMemberFactsFromGuardedLocalSourceInvocation`;
2. `GetStableNonNullMemberFactsFromDirectSourceInvocation`;
3. `TryGetDirectObjectCreationInitializer`;
4. `GetStableNonNullMembersFromObjectInitializer`;
5. `IsObjectInitializerValueProvenNonNull`.

Their responsibility is source- and `SemanticModel`-dependent discovery of
stable non-null auto-property facts from a guarded local's current source
invocation and from every supported non-null returned object initializer. This
is not the pure property-shape classification already owned by
`ExceptionFlowStableMemberFacts`, so that component remains small and
stateless. It is also not immutable-member value lookup, known-property
classification, general nullability, or primitive-value classification.

`ExceptionFlowStableSourceMemberFactsProvider` consumes the established
dispatch classifier, stable-member shape classifier, symbol-usage, guard,
local-initializer, enum-return-expression, and compilation-local semantic
scope owners. It owns no cache, creates no callee context, consumes no
`ExceptionFlowCallContext`, and has no Analyzer reference.

## Call-context fact projection

The two extracted methods are:

1. `AddExplicitArgumentNonNullMemberFacts`;
2. `AddDefaultParameterFacts`.

They project already established argument/member facts and omitted/default
parameter facts into the collections from which an `ExceptionFlowCallContext`
is constructed. `ExceptionFlowCallContextFactProjector` is the narrow owner.
It consumes the argument mapper, dereference discovery, stable source-member
provider, primitive facts, symbol usage, and the immutable caller context.

`ExceptionFlowArgumentMapper` remains limited to argument-to-parameter ordinal
mapping. `ExceptionFlowStableMemberFacts` remains property-shape
classification. The source-member provider remains source fact discovery, and
the primitive provider remains constant classification. Putting both
projections into any of those owners would mix responsibilities.

`ExceptionFlowCallContext` representation, equality, hashing, normalization,
canonical projection, receiver mapping, named-argument mapping, and default
value semantics are unchanged.

## Origin, scope, graph, and cache semantics

The dispatch rule is based solely on the same Roslyn method-symbol flags as
before and does not branch on source origin.

- SameCompilation retains the identical source-return and member projection.
- ReferencedProject retains the existing exact owning-scope boundary.
- SupportingSource retains exact registered-scope lookup and evidence rules.
- MetadataOnly gains no synthetic source or semantic model and remains fail
  closed.

`ExceptionFlowSemanticScope` remains compilation-local, and
`ExceptionFlowSemanticEnvironment` remains cross-scope. Summary graph
construction, target discovery, completeness, registration, and evaluation
owners are unchanged. The ConditionalWeakTable invariant cache remains with
the Analyzer-owned recursive evaluator; DataFlow remains with
`ExceptionFlowDataFlowFactsProvider`; successful dereference remains with
`ExceptionFlowDereferenceFactDiscovery`. Keys, locks, weak lifetimes,
invalidation, and compilation isolation did not change.

## Structural result and stopping point

- Analyzer partials: 43 → 42.
- File-based Analyzer nonblank SLOC: 21,775 → 21,343.
- New owner nonblank SLOC: 442 across the classifier, source-member provider,
  and projector.
- Extracted methods: seven downstream methods plus the formerly out-of-
  inventory dispatch classifier.
- Interfaces, virtual methods, callbacks, services, caches, or mutable fields
  added: zero.
- Direct references from providers/components to `ExceptionFlowAnalyzer`: zero.
- Direct Analyzer references to the four P5O2A2 blockers: zero.
- Neutral Core and Canonical Core remain Roslyn-free.

The seven remaining Analyzer-owned families total 57 methods: heterogeneous
return/condition facts; dictionary facts; sequence call-context observation;
sequence element facts; sequence range/dictionary mutation; sequence source
preservation; and successful sequence validation. No additional small leaf is
free as a result of this slice. The exact smallest next blocker is the
eight-method Return/condition downstream family: it mixes five
condition-derived string-fact helpers with three independent known-framework
return classifiers and owns four SCC-to-Analyzer edges. It needs a separate
ownership split rather than a mixed catch-all provider. After that independent
leaf, the three-family sequence call-context/element/source condensation cycle
still owns 20 SCC-to-Analyzer edges and cannot be split by moving only one
family without forming a component cycle.

The 63-method SCC and its 112 internal edges are unchanged. It is still not
ready to move because 36 SCC-to-Analyzer edges remain. P5O2B is therefore not
ready: the same analyzer source set still cannot be composed against active and
manifest-pinned historical Roslyn without an executable reference, active-
Roslyn leakage, source copy, reflection, compiler-version `#if`, or loss of
SameCompilation facts.

## Validation

- Pre-change dispatch/source-member/origin characterization: 81/81.
- Post-extraction focused dispatch/fact/context/architecture slice: 100/100.
- Architecture guards: 6/6; direct classifier characterization: 1/1.
- Explicit Direct, ProjectTransitiveDeclaredExceptions, ProjectTransitive, and
  SolutionTransitive slice: 15/15.
- Full suite: 2,524/2,524; zero failed; zero skipped.
- Warning-as-error build after extraction: zero warnings and zero errors.
- Final self analysis: 16 findings in 62,311 ms; `DOC610=0`, `DOC611=1`,
  `DOC631=15`, and `DOC632=0`.
- Canonical finding/evidence diff against the retained P5O2A4 baseline: zero
  added, zero removed, zero changed evidence; the complete finding arrays are
  equal.
- The first incorrectly broad `--full` self-analysis invocation included the
  Tests and Evaluation projects and produced 440 out-of-profile documentation
  findings. The correct `--project XMLDocNormalizer` repeat above is the
  canonical result; the broad result is not a product regression.
- E1 canonical profile: Source Link enabled, `verified-line-endings`, and
  `bounded-remote`; seven candidates, five complete, two expected fail closed,
  zero unexpected, and zero potential bugs.
- The first sandboxed E1 attempt reproduced the known environmental result:
  zero complete, two expected fail closed, and five unexpected
  `SourceUnavailable` outcomes. The network-enabled repeat produced the
  canonical 5/2/0/0 result.
- Dapper remains `ConfigurationReconstructed / ConfigurationUnsupported` for
  exact `optimization=release-debug-plus`. OneOf remains
  `PdbValidated / MissingArtifact`. Semver retains one expected added finding.
- `Program.Main` remains the sole DOC611 finding. The known `IOException` BCL
  uncertainty remains DOC631.
- No native crash, managed crash, modal dialog, or test flake was observed.
- The single 62,311 ms self-analysis duration is recorded without a performance
  conclusion.
- Final warning-as-error build: zero warnings and zero errors.
- `git diff --check -- .` passes; its only output is the expected line-ending
  warning for the protected foreign `../../.gitignore` change.
- All 20 changed task C# files are CRLF-only UTF-8 without BOM, bare LF, bare
  CR, trailing whitespace, or added `var` declarations. The JSON audit parses
  successfully.
- Validation regenerated seven tracked ExceptionFlow.Core `bin`/`obj` files.
  They are build churn and contain no production-source change.

The machine-readable ownership, caller list, dependency surfaces, measurements,
origin behavior, cache owners, and readiness result are in
`P5O2A4C-runtime-dispatch-stable-source-member-audit.json`.
