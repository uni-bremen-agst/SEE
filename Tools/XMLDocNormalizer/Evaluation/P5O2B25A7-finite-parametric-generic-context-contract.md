# P5O2B25A7 - finite parametric generic context abstraction contract

Decision: **STOP - no production implementation GO**.

This is a contract/architecture investigation, not an analyzer implementation,
not new Finding Admission and not a successful generic repair. B25B stays closed.
The recommended research direction is **A + C**, with B useful only as a proved
observation quotient/catch oracle. A finite declaration graph is necessary but
does not by itself give finite, executable closure of its type-family equations.
No existing test, production file, routing policy or Roslyn version is changed.

The precise new boundary is **correlated recursive exception-family closure,
catch residuals, and completeness-aware result projection**. The current A5
cycle-erasure proof cannot be transferred to type-transforming definition cycles.
An implementation of only finite examples would not close this boundary.

## 1. Inputs, decision standard and evidence classes

Authoritative HEAD is c777f9642c145a57c112c28216c3996128c6234b. A5's uncommitted
production fix is inherited, not a proposed A7 change. A6 now records fresh
boundary witnesses and validation in its own audit; A7 does not relabel those
tests as tests of a hypothetical implementation. The characterization inventory
below comes from repository-wide source searches and direct assertion inspection.

The GO standard is an executable, sound, terminating contract for all required
obligations, including representation and output completeness. A finite equation
notation without a terminating solver is not GO. Nor does STOP prove that all
sound finite abstractions are mathematically impossible. In particular, coarse
overapproximation with uncertainty is possible; adopting its precision/output
policy is a substantive contract choice, not a small identity fix.

## 2. Compare A, B, C before selecting a direction

### Candidate A: parametric bodies and substitution edges

A node denotes an owner-qualified callable declaration plus the existing relevant
value-fact partition. Its formal generic environment includes method and every
containing type's parameters. A source is an expression such as Fault<Var(T)>.
An edge to B carries a simultaneous map from B's formal binders to expressions
over A's binders. Body construction schedules each declaration/fact node once;
it does not schedule A<int>, A<List<int>>, ... as separate nodes.

Strengths: finite body graph, compositional ownership and ordinary concrete
instantiation, no need to copy compiler state. F1--F8 and cross-project chains
can be explained directly. Repeated identical environments have deterministic
reuse. Two root environments remain different when the source depends on them.

Failure of A alone: S_A(T) = {Fault<T>} union S_A(List<T>) is a finite equation,
but iterative substitution/materialization still grows without bound. Dropping
the recurrence as a repeated definition loses Fault<List<T>> and all later types.
An open Fault<T> is not an exact concrete exception returned for A<int>.
An equation graph is a useful IR, not an automatic solver or result type.

### Candidate B: finite observational abstraction

For a fixed finite set O of closed catch/type predicates, classify a concrete
environment by its observation vector. The powerset of those vectors is finite;
monotone union iteration over this finite lattice terminates. Sound abstract
transfer requires overapproximation of every concrete successor. Removing an
exception under a catch requires a universal proof over its concretization,
not merely a matching representative. This is a genuine candidate, not a depth
cutoff, and can supply a finite conservative catch oracle in a restricted scope.

But O cannot just be the finite text of catch declarations. catch(Fault<T>) in
a generic body ranges over constructed catches after substitution. Repeated
List<T> can generate unbounded closed predicates from that finite syntax.
Backward closure of observations under all substitutions is not proved finite.
Moreover, membership in finitely many catches does not identify every exact
exception construction: infinitely many different Fault<List^n<int>> can share
the same observation vector. The existing result enumerates exact identities,
not observation classes. A class label cannot be silently emitted as Proven
Fault<>, Fault<object>, one representative or an empty complete result.

One can deliberately choose a coarser finite abstraction with Unknown residuals.
It can be sound if unknown catches never suppress possible sources. It loses
precision, possibly leaving a residual where a correlated catch actually catches
everything. The required relational observations, class concretization and result
projection must then be explicit. This package does not pretend that an exact
finite, substitution-congruent quotient has been proved for the full scope.

### Candidate C: regular/recursive type families

For one unary linear context L(x)=List<x>, Q = int | List<Q> is a finite regular
tree grammar. Fault<Q> represents the whole growing exception family without
enumeration. Finite automaton states give a finite solving domain for that
restricted grammar. Finite closed catch patterns can be matched against such a
family by finite product constructions; residual family subtraction must still
be represented, not replaced by an arbitrary surviving representative.

Strengths: explains F9 and linear single-parameter F10 without a depth budget.
Owners are constructor labels, not metadata-name strings. A fixed linear context
Dictionary<string,List<x>> also fits. Equality of canonical finite graph syntax
is computable; language equivalence requires a separate algorithm, not reference
or serialized-text equality. Determinization/product operations can be expensive
(potentially exponential state growth); no polynomial performance promise.

Boundary: arbitrary scoped substitutions can duplicate and correlate parameters.
For example a valid source program may have

```csharp
public class Fault<T> : System.Exception { }
public class Pair<X, Y> { }
public static class Flags { public static bool Stop; }
public static class Api
{
    public static void Root() => A<int>();
    public static void A<T>()
    {
        if (Flags.Stop) throw new Fault<Pair<T, T>>();
        A<System.Collections.Generic.List<T>>();
    }
}
```

with an initial int. Its source family contains exactly

    Fault<Pair<List^n<int>, List^n<int>>>    for n >= 0.

The two branches share n. Independent regular grammar occurrences Q,Q would
also permit Pair<List^n<int>,List^m<int>> for n != m. That is an overapproximation,
NOT an exact encoding of correlation.

Elementary proof: in any finite bottom-up tree automaton, two distinct depths
n,m have the same state (or same set of possible states). Replacing just the
right branch of an accepted Pair(t_n,t_n) by t_m therefore preserves acceptance.
An exact diagonal language forbids that replacement. Thus ordinary independent
regular-tree states do not suffice for this correlated family. This is our own
finite-state argument, not an assertion that every recursive representation or
sound overapproximation is impossible.

A finite parameterized recursive system can express the correlation, but then
membership, catch entailment, residual difference, equality and termination of
the chosen solver have new proof obligations. Finite syntax alone proves none
of those. Constrained families are not ordinary canonical concrete type trees.

### Combinations

A+B: finite body graph plus finite observations can terminate, but must declare
which distinctions/possible exception families it loses and how they survive
as residual uncertainty. No complete finite observation/congruence basis is
established for all substituted generic catches and requested exact results.

A+C: recommended foundation for sources and substitution-bearing edges. Complete
for some regular fragments in principle; full correlated/multi-binder closure
and integration with A5 are NOT proved. B may be added as a proved three-valued
catch oracle, not as a replacement for actual owner-qualified source semantics.

## 3. Candidate decision matrix

P = explained for the stated restricted fragment; O = unresolved obligation;
N = insufficient alone. These are analysis judgments, not execution results.

| Criterion | A | B (finite closed observations) | C (regular fragment) | A+B | A+C / oracle B |
|---|---|---|---|---|---|
| Soundness | O: recursive result closure | P only with correct concretization/transfers | P for admitted regular grammar | O: precision/output contract | O: correlated closure |
| Precision | Exact finite terms | May overapproximate explicitly | Exact admitted languages | O | Exact finite + admitted families only |
| Termination | Bodies P, solving N | P for a fixed finite lattice | P for finite automata | O: observation generation | O: richer family solver |
| Catch correctness | Needs substituted relation | Universal/may/unknown required | Closed regular filters P | O: relational predicates | O: correlated/parametric residuals |
| Generic method | P | Needs binder observations | Needs binder frontend | P finite cases | P finite cases |
| Generic containing | P | Needs binder observations | Needs owner labels | P finite cases | P finite cases |
| Nested type | P with scoped chain | Needs relational scope | Needs owner labels | P finite cases | P finite cases |
| Structural recursion | N alone | Finite coarse states possible | P linear fragment, N full correlation | O | O full scope |
| A5 compatibility | Not cycle-erasure for changing substitution | Only after a valid finite semantic state quotient | Family closure changes meaning of cycles | O | O |
| Cross-project | P with exact rebinding maps | Requires owner-scoped predicates | Requires owner-scoped grammar | P finite chain | P finite chain |
| Historical | Shared neutral algebra possible | Same fixed domain required | Same grammar/solver required | O implementation | O implementation |
| Canonical representation | New substitution/expr IR | New abstract class/residual semantics | New family refs/grammar | Extension | Extension |
| Implementation complexity | Medium frontend, solver open | High congruence/precision work | High family operations | High | Highest general closure burden |
| Proof burden | Recursive closure | Congruence + correct approximation | Correlation + residual operations | Combined | Combined + SCC integration |

Decision does not depend on which implementation is easiest. None currently
satisfies every requested obligation within the existing exact-type graph/result
and A5 cache semantics. Therefore STOP, not a speculative implementation GO.

## 4. Minimal common algebra (required, not implemented)

### Nominal owners, binders, expressions

Owner = assembly identity + module identity + declaration identity, with each
nested containing declaration retained. A type constructor carries this Owner,
metadata arity and the containing construction. Names alone never identify it.

Var = (owner declaration, scope Method/Type, ordinal). Distinct binders called T
are distinct variables. An expression E is Var(v), exact ground canonical type,
or Constructor(owner, containingExpression, argumentExpressions); array/pointer
forms require their existing exact structural kind. No text replacement.

Constructor arity, owner, valid containing chain and type constraints must be
validated in the destination compiler environment. Missing/ambiguous/contradictory
facts yield an unsupported result, never invented Object or a first match.

### Substitution laws

A simultaneous substitution sigma maps the callee's formal variables to caller
expressions, with identity on unmapped *declared* free variables in a symbolic
scope. A supposedly closed invocation missing a required binding is invalid,
not this symbolic identity case. Apply does one structural, capture-free pass:

    apply(id,E) = E
    apply(sigma,Var(v)) = sigma(v), if bound; otherwise Var(v)
    apply(sigma,C(E1,...,Ek)) = C(apply(sigma,E1),...,apply(sigma,Ek))
    (tau after sigma)(v) = apply(tau,sigma(v))
    apply(tau after sigma,E) = apply(tau,apply(sigma,E))

Thus T -> List<U>, U -> int composes to T -> List<int>. Method and containing
substitutions are combined simultaneously over disjoint scoped domains. Nested
owners retain every outer mapping. Renaming bound variables is alpha-renaming
by scoped identity, never by display text. Duplicate contradictory assignments
to one variable are invalid; assignments to unrelated same-named binders do not
conflict. Constructor/owner substitution preserves nominal labels.

For an edge A->B, sigma_AB maps B binders to A expressions. Given root rho_A,
the actual B environment is rho_B(v)=apply(rho_A,sigma_AB(v)). For A->B->C it is
rho_C= (rho_A after sigma_AB) after sigma_BC. This is the edgeInst operation used
below; it is not application in the reverse mapping direction.

Composition is associative by structural induction; id is a left/right unit.
It is generally NOT commutative. A recurrence T -> List<T> is an edge transformer,
not an instruction to recursively chase a self-map while applying it once.
Its repeated compositions are distinct; never eagerly normalize them to a
closed concrete tree or collapse the cycle to identity.

Concrete expression equality is structural nominal equality after documented
existing canonical language normalizations. Symbolic equality is alpha-aware
structural equality. Family semantic equivalence must not be inferred from either
syntactic inequality or a matching outer definition. A family equation requires
its own equivalence/concretization contract.

### Required recursive source denotation

For a parametric node d and environment rho, the mathematical intended result is

    S_d(rho) = LocalSources_d(rho)
               union over edges e: Residual_e(rho, S_target(edgeInst_e(rho)))

Residual removes only exception instances actually caught along that edge.
This is a monotone set equation in the family unknowns when catches are fixed
for rho; use its least solution for escaping may-exceptions. This defines meaning,
NOT a terminating solving algorithm. Generic dispatch/value-fact dependencies
must also preserve their existing conservative semantics; they cannot be assumed
to be independent of type parameters just because the graph uses declarations.

Required source forms are Exact(E), Parametric(E,binders), or a family reference
plus environment and constraints. Proven concrete evidence cannot be manufactured
by picking one arbitrary member of an abstract family. Source location, evidence
kind and owner remain attached throughout mapping, union and residual filtering.

## 5. Catch contract and precision

For a closed thrown type t and closed catch c, catches(t,c) holds exactly when
the constructed type equals c or one of its substituted base classes equals c.
Apply the invocation substitution to both source and catch first. Do not compare
OriginalDefinitions. Nested containing constructions participate in equality.
Unknown catch filters retain the existing conservative filtering behavior.

For a family L and a fixed environment rho:

- AllCaught: every member satisfies catches; only then may the whole family vanish.
- NoneCaught: no member satisfies catches; retain the whole family.
- Mixed: represent L minus the caught subset, not all-or-nothing suppression.
- Unknown: retain a conservative residual and explicit deterministic uncertainty;
  never silently classify the source as completely analyzed or wholly caught.

Example: {Fault<List^n<int>> | n>=0} outside catch(Fault<int>) leaves all n>=1.
Cycle-erasing the type-transforming self-edge leaves just Fault<int>; the outer
catch then removes it and falsely reports no escaping exception. This shows why
correct root substitution plus the A5 definition-cycle skip is still unsound.

Soundness means every relevant reachable exception stays in the concretization.
Precision means unnecessary possibilities are avoided. Independent regular
approximation of a diagonal family may be sound but imprecise; false residuals
must not be exported as precise Proven findings. A future contract must classify
such overapproximation and its effect on catch suppression/output. No such policy
is silently adopted here. B25B remains a separate decision even after it exists.

## 6. Graph, SCC, cache and termination obligations

Recommended body nodes: (owner-qualified callable definition, existing relevant
finite value-fact partition). Invocation substitutions live on edges/root contexts,
not an unbounded work queue of concrete instantiations. This answers the graph
question: A<int>, A<List<int>>, ... must NOT each become a new body node.

SCC discovery can reuse the existing finite declaration/fact topology. It must
not equate that syntactic SCC with a set of identical semantic context states.
Changing substitution cycles denote transformations/families, not no-op revisits.
Reuse A5's nonrecursive graph ownership, component discovery, per-root isolation
and rule against publishing partial completion. Do NOT run a second competing
recursive engine alongside A5, or reuse its closed finite result for a symbolic
cycle whose closure was never solved.

Existing A5 cache `(key, excluded members)` is sound for the proved frozen finite
graph/filter model. On the same exact semantic state, cycle-erased exception
reachability remains appropriate. On a mere callable-definition repeat with a
different type transformer, the premise fails. Generalized completion must bind
definition/fact state, substitution/family environment, filter obligations,
component closure version and relevant ancestor context as required by a proof.
This is a contract requirement, not a proposed second string-key infrastructure.

Termination decomposition:

1. Finite declarations and finite existing relevant fact states can bound body
   construction. Root/type arguments must not enlarge this node space.
2. Identity/constant finite recurrences have repeated exact environments; ordinary
   A5 finite closure suffices after correct substitution.
3. Linear regular families have finite grammar/automaton state representations;
   operations must operate on those states, not enumerate their members.
4. Correlated families and substituted catch residuals are the remaining solver
   boundary. No finite-height domain or general terminating exact decision
   procedure is proved here. A finite equation does not resolve item 4.

Neither a timeout nor a maximum constructor height supplies the missing proof.
There is no production budget hidden in the A6 witness's 24 observations.

## 7. F1--F14 obligations

| Obligation | Required semantic explanation | Present decision |
|---|---|---|
| F1 M<int> | Apply method binder to exact source; owner preserved | Common finite algebra specified; not implemented |
| F2 int/string | Same parametric body, distinct root environments/results when relevant | No definition-only result/cache equality |
| F3 repeated int | Same closed environment/facts/filter context reuses a completed result | A5 completion invariant retained |
| F4 C<int>.M | Bind containing declaration's ordinal | Finite algebra specified |
| F5 C<int>.M<string> | Disjoint type/method binder domains | Finite algebra specified |
| F6 nested | Preserve every containing owner and scoped argument | Finite algebra specified, resolver work needed |
| F7 constant | A<string> reaches A<int>, then repeats int | Finite closure, not one folded open source |
| F8 identity | Same exact environment repeats | Existing finite SCC premise can hold |
| F9 List growth | One body with substitution edge; family Q=int|List<Q> | Regular fragment explained, existing result cannot export family |
| F10 mutual growth | Finite equation SCC with composed maps and correlations | Full correlated closure unresolved; STOP |
| F11 catch | Exact constructed bases; universal suppression, residual otherwise | Concrete specified; general family residual solver unresolved |
| F12 A->B | Exact owner rebind plus method/containing map | Finite composition specified, not implemented |
| F13 A->B->C | Compose maps in caller-to-callee order, preserving each binder owner | No name-only fallback |
| F14 A5 | No provisional reuse; changing substitution cycle is not erased as identity | Generalized closure/cache proof unresolved; STOP |

F9 is no longer the vague question "how many List levels?": enumerate none;
use the finite equation/family. The unresolved question is precise: which family
algebra/solver handles correlation and catch residuals with a proved finite
state space, and how is its completeness represented at the existing output seam?

## 8. Cross-compilation and Historical boundaries

Resolve exactly one destination declaration by assembly/module/declaration owner.
Resolve the containing construction from outermost inward, then bind the method's
own arguments on ConstructedFrom. Rebind each ground leaf and formal binder to
the matching owner domain; verify arity/constraints and canonical identity after
construction. Compose supporting-source maps at A->B and B->C. Missing owner,
ambiguous declaration, impossible construction or conflicting map is not repaired
by a first symbol, same name, OriginalDefinition or Object fallback.

Cross-project recursive SCCs are only in scope if the existing supporting-source
graph genuinely contains them. No cyclic project-reference support is promised.
Current and Historical must use the same source algebra, normal forms, result
classification and deterministic unsupported reasons. Roslyn handles remain local
to each process; the host must not load Historical compiler images.

Internal canonical needs, separated:

| Item | Existing support | Minimal new contract required |
|---|---|---|
| Callable identity | Finite constructed callable and source identity | Keep definition separate from environment; no lossful normalization |
| Owner | Assembly/module/containing identities exist | Reuse; exact destination mapping/validation |
| Type expression | Finite concrete trees and scoped TypeParameter identity exist | Explicit free/bound environment and validity rules |
| Substitution | Not an explicit simultaneous composition object | Sorted scoped binder->expression map with capture-free operations |
| Recursive family | No recursive language/set node | Finite refs/equations and a proved admitted fragment/solver |
| Result | Exact CanonicalTypeIdentity[] entries plus uncertainty | Explicit exact/family/residual completeness projection, not fake concrete type |

Worker->Main does NOT automatically need a new field just because the worker
uses parametric internal nodes. If every returned result is finite/exact after
internal evaluation, current transport may suffice. If a non-singleton family
must be retained in the returned semantic result, exact entries cannot carry it.
Choose a version-neutral family DTO or a formally specified unsupported-result
projection; do not silently discard it behind an otherwise complete-looking
response. That policy is part of the next contract boundary, not implemented here.

## 9. Uncertainty and unsupported cases

Deterministic semantic reasons: unresolved/contradictory owner, missing required
binding, arity/constraint mismatch, unsupported correlated family operation,
undecided family/catch entailment, or inability to project the residual faithfully.
Not a reason: "we reached depth N" or "the analysis took too long".

Existing AnalysisUnsupported can label a reason, but a label alone does not
preserve the possible source family or define which findings are complete.
The current adapter also maps legacy display uncertainty to LegacyUnclassified.
The follow-up must define typed completeness and a conservative residual before
using such a label as a sound total fallback. Do not infer that an enum addition
or a wire extension is necessarily the only solution.

## 10. Architecture impact matrix (future, not a diff)

| Component | Change needed? / why | Invariant | Risk |
|---|---|---|---|
| ExceptionFlowCallContext | Yes: explicit scoped invocation environment | Retain relevant construction independently of declaration handle | Binder capture/lost value facts |
| ExceptionFlowCallableKey | Yes: separate finite body identity from instantiated result identity | Repeated same environment reuses; distinct relevant roots differ | Infinite node registration or accidental collapse |
| Graph builder | Yes: build parametric body once | Preserve all sources/edges/fact dependencies | Type-dependent dispatch/body facts |
| Pending queue | Yes semantics of scheduled state, not a depth budget | Finite declared state inventory | Exact successor sneaks back into queue |
| Source representation | Yes: expression/family + provenance | No owner/source/evidence loss | Fake Proven representative |
| Summary evaluator | Yes: solve admitted substitution-cycle equations | Completion only after sound closure | Reusing A5 proof outside its premises |
| Caught type filter | Yes: substituted equality/base chain and family residual | Never suppress merely by definition | False negative exception flow |
| Supporting-source resolver | Yes: carry exact environment through source mapping | Target scope owns rebound body | Metadata/source binder mismatch |
| Cross-compilation rebinding | Yes: method/containing/nested composition | Unique owner-qualified match | Name-only fallback |
| Cache | Yes: body vs instantiated/family completed result separation | No provisional/unrestricted reuse | Context aliasing/root order dependence |
| SCC | Reuse discovery; extend closure semantics only with proof | Exact semantic cycles vs changing maps distinguished | Cycle erasure loses new types |
| Canonical identity | Reuse nominal concrete model; add internal map/family contract | Stable owner-aware normalization | Conflating syntax/equivalence |
| Historical worker | Shared algorithm and validation required | Historical Roslyn isolated | Current/Historical semantic drift |
| Wire | Conditional on residual export decision | Version-neutral, complete classification | Family silently disappears |
| Main | Conditional projection/adoption; no Historical symbols | Fail closed on incomplete residuals | False successful findings |
| Tests | New semantic/proof tests + explicit approved characterizations only | Same-construction reuse and A5 correctness persist | Updating defects without permission |
| Evidence | New package outputs only | Frozen B25--A6 history, bound actual images | Retroactive rewrite / false green |

## 11. Characterization preflight for a later implementation

Paths below are under Tests/XMLDocNormalizerTests. Line numbers are the unchanged
current source, not a requested edit range. "Yes" means ONLY the precise prior
permission and ONLY after a complete sound fix. A7 exercises none. Permissions
for A6 are NOT automatically transferred to a later differently scoped A8;
the implementation prompt must explicitly reaffirm applicable authorizations.

| File | Test / case | Assertion | Old characterized state -> necessary semantics | Permission |
|---|---|---|---|---|
| Worker/ExceptionFlowConstructedInvocationContextTests.cs | ActualCallSiteRetainsBothDimensions_ButActualContextUsesDefinition: method/containing/both/nested | 39 definition canonical callable; 41 missing method args | Canonical environment retains exactly dimensions present at call | Yes, earlier precise two assertions; non-generic/other assertions no |
| same | DifferentConstructionsShareActualNode_WhileExactCanonicalIdentitiesDistinguishThem: both containing cases | 66 target equality for int/string | Relevant instantiated result contexts differ; a parametric BODY node may still be shared, so test must distinguish body key vs invocation environment | Prior permission for separated semantic target; new body/environment split needs explicit review; 67 identical reuse unchanged |
| same | ExactConstructedKeys_ChangingTypeRecursionHasNoFiniteReuse_FiniteHarnessOnly: growing=false, both containing cases | 94 open source T | Closed Fault<int> at invocation boundary; parametric body source itself may remain formal | Conditional A6 permission for concrete source only; body-vs-result assertion redesign NOT covered |
| same | same: growing=true, both containing cases | 91 single actual node; 92 self-edge; 94 open source | Parametric body may stay single/open; edge must carry real List transformation; closed result is family, not fake Fault<> | No changes authorized; actual new assertion choice depends on next family contract |
| same | same, independent exact-key harness | 118,126--128 finite prefix uniqueness/counts/pending | Still correct as boundary counterexample, NOT production bound | No change needed/authorized |
| same | SupportingSourceResolverPreservesContainingConstruction_ButNotMethodArguments: containing=false | 164 expected identity inequality | Preserve full constructed method identity after rebind | Conditional A6 permission for this assertion only; containing=true remains |
| same | GenericBodyCatchSymbolsAlsoNeedInvocationSubstitution: both containing cases | 181 open collected T | Invocation catch Fault<int>; stored parametric catch may remain Fault<T> if evaluated with environment | Conditional A6 concrete catch permission only; observation-layer redesign needs review |
| same | GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness: both cases | 203 one entry; 205 empty args | Non-singleton growth family/residual, not exact open entry | NO; family/output contract still unresolved |
| same | ActualCallSiteRetainsBothDimensions..., generic cases | 38 definition CallableSymbol; 40 empty value-fact Key | May remain correct if declaration handle/value facts are separate from generic environment | NOT authorized; no necessary change established |
| same | SupportingSourceResolver..., both cases | 165 context CallableSymbol equals OriginalDefinition | Declaration handle may remain normalized with separate environment | NOT authorized; can remain invariant |
| Worker/ExceptionFlowBoundedGenericContextTests.cs | RealRecurrences_DistinguishFiniteClosureFromIncreasingExactArguments: identity/constant | 107 open body source | Concrete invocation source Fault<string> | Conditional A6 permission only; parametric observation-layer redesign not automatic |
| same | same: constant | 105 graph count 2 | Exact concrete materialization would have 3; recommended parametric BODY graph need not | Permission allows 2->3 ONLY for concrete nodes; NOT authority to invent a different body/result assertion |
| same | same: growing/mutual/containing | 105 fixed folded graph count; 107 open source | Finite body graph may keep count/open expressions; substitution edges and family result must carry growth | NO; do not pre-approve a family outcome |
| same | same, all cases | 104 independent Roslyn exact identity counts | Still correct and unchanged | No change needed |
| same | FiniteMutualCycle_CachedPartialResultDependsOnRootEdgeOrder; GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness | A5 Fault reachability, no uncertainty, parity/lifetime | Preserve finite A5 correction in every order | No change needed; A5 permissions do not authorize new edits |
| Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs | CompilerModel_ObserveActualSummarySourcesBeforeNormalization: constructed-callee / two-constructed-callees | 97,106--115 single/open sources, definition callee, folded targets | Concrete instantiated results retain int and int/string separately; body graph may be shared | Earlier precise two-case permission; moving observation layer/body assertions requires review; closed-source unchanged |
| same | StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness: all three cases | 148 empty containing args; 145 single entry for two callees | Exact Outer<int>.Leaf and, in two-case, Outer<string>.Leaf | Conditional A6 permission; all uncertainty/parity/lifetime/isolation and other assertions unchanged |
| Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs | OriginalNameOnlySubstitution_IsProven_ExactCandidateRepairsOnlyTheTopLevelCase | 27--28 wrong owner | Keep actual exact owner | Earlier A2 authorization for precisely reported ownership characterization; must reaffirm scope |
| same | ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught | 54,59--60 null resolve / same-type uncaught | Exact constructed nested rebind and matching catch | Earlier four-characterization permission, not blanket file permission |
| same | GenericCatchComparison_ErasesDifferentConstruction_IndependentOfNameOnlyNormalizer | 86 incorrect catch true | Fault<int> not caught by Fault<string> | Earlier four-characterization permission; no broader assertions |
| same | StopRule_LeavesOriginalProductionAndClosedFindingAdmission | 104--105 unsafe lookup required / exact resolver forbidden | Remove obsolete implementation fingerprint only after correct type normalization | Earlier four-characterization permission; admission assertions unchanged |
| Worker/ExceptionFlowHistoricalSolutionFindingTests.cs | AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted; StopRule_DoesNotIntroducePolicyRoutingAttestationOrPidWorkarounds | old owner/first-match assertions 105/195 | Actual assembly-qualified owner | Earlier two B25 characterizations only; historical report/audit frozen |
| Check/Semantic/Exceptions/ExceptionFlowSummaryGraphTests.cs | CallableKey_NormalizesSymbolAndIncludesContext | 45--51 definition/constructed key/hash equal | Generic<T> body is empty: construction irrelevant, body sharing can remain; do NOT demand unequal hashes | No necessary change; no permission |
| Check/Semantic/Exceptions/ExceptionFlowCrossCompilationResolverTests.cs | ResolveMethod_ConstructedGenericContainingType_PreservesSubstitution | concrete string return/parameter/container and nondefinition symbol | Correct preservation already required | No change needed |
| Check/Semantic/Exceptions/CanonicalIdentityTests.cs | constructed callable/type/scoped parameter identity tests | exact argument/owner inequality/equality | Correct finite identity invariants | No change needed |
| Worker/ExceptionFlowRecursiveSummaryClosureTests.cs | all 33 A5 cases | reachable Fault sets, stable reuse, uncertainty, components, parity/isolation | Preserve proved finite behavior; no type-changing family is tested here | No change needed; no permission to rewrite |
| Worker/ExceptionFlowGenericExpansionBoundaryTests.cs | new A6 boundary/control witnesses | real definition folding, hypothetical exact growth | Boundary facts remain valid; future production correction changes "current defect" assertions | NEW future permission needed after A6 freeze; not to be opportunistically edited in A7 |

Repository-wide searches additionally cover Generic Summary/recursion/cache,
canonical callable/type, supporting-source and contextual-fact suites. Ordinary
nongeneric normalization assertions, correct identical-construction reuse,
Roslyn model facts and lifecycle/isolation checks are invariants, not obstacles
to be rewritten. No assertion is changed in this package. A STOP here is a contract
decision, not a request to expand current test permissions.

## 12. Exact next package and acceptance conditions

There is NO A8 production implementation GO. Next package:

**P5O2B25A8 - Correlated Recursive Type-Family Closure and Completeness Contract
(contract/proof only).** It must either select and prove a sufficiently expressive
decidable family fragment plus an explicit sound unsupported projection, or
demonstrate why a different architecture is needed. No new Finding Admission.

Required deliverables before implementation:

1. A formal admitted family grammar including parameter correlation, scope and
   owner substitution, or an explicit conservative approximation policy.
2. Total terminating algorithms for representation, substitution composition,
   union, catch residual, classification and stable equality/canonicalization.
   If equivalence is not decidable, define the weaker safe cache relation.
3. A finite-state/finite-equation solving argument for mutual substitutions,
   not just body scheduling. State bounds and complexity must identify the actual
   admitted fragment. Unsupported recognition must itself terminate semantically.
4. A generalized SCC completion proof that retains A5's partial-result protection
   without discarding transformation cycles. No parallel competing recursion engine.
5. A completeness-aware internal result and Worker/Main projection decision.
   Decide exactly when current transport suffices and when family/residual evidence
   would cross it; no hypothetical blanket protocol expansion.
6. Exact validation/permission plan against the table above, including body-vs-
   invocation observation changes that prior concrete-node permissions do not cover.

Later IMPLEMENTATION components are the affected rows of section 10, not just
CallContext/CallableKey. Required new tests: F1--F14, scoped same-named binders,
method/containing/nested composition order, identity/constant/mutual growth,
diagonal/correlated sources, family catch All/None/Mixed/Unknown, inheritance,
owner/arity/constraint/missing/ambiguous negatives, deterministic unsupported
projection, exact identical reuse, A5 root-order/SCC/cache regressions, genuine
Historical parity/lifetime/isolation, cross-project A->B->C and no admission.

Validation matrix: independent algebra/family proof tests BEFORE production
integration; all approved characterizations only after full correctness; then
A6/A5/A4/A3/A2/A/B25, B24/PID/B23--B5, generic/catch/cache/architecture,
Broad/Full/Boundary, complete Dual eleven WAE, eight Current baseline bytes,
source/PDB/image bindings, both evidence gates and frozen historical fingerprints.
Do not regenerate baselines, waive failures, retry to green or rewrite history.

## 13. Sources and confidence boundaries

The C# specification defines constructed generic types by their declarations and
arguments, and substituted base classes for constructed classes. Those are the
language premises, not a proof of our analyzer contract. See
[C# types, constructed types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/types#84-constructed-types)
and [C# classes, base classes](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes#15242-base-classes).
All family counterexamples, proposed algebra and architecture judgments above
are our analysis of the local code, explicitly NOT existing product capability.
No external theorem of general impossibility/undecidability is asserted. A sought
external tree-automata PDF was inaccessible; it is not used as evidence.

No production implementation, no existing test change, no A6 permission exercised,
no compiler/routing/PID change, no commit/push/stash operation. No new Self Analysis
or Canonical Finding Diff. The accompanying A7 audit separates static contract
consistency from A6's actual runtime validation and from implementation readiness.
