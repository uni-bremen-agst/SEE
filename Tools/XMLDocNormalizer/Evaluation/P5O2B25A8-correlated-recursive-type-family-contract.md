# P5O2B25A8 - correlated recursive type families, catch remainders and SCC projection

Decision: **STOP - no production implementation GO**.

New result beyond A7: exact correlated families have a finite trace/equation
description, and exact catch remainders have a finite guarded description.
However, a total exact semantic decision procedure for this full class cannot
be supplied: correlated parametric catches can encode a Post Correspondence
Problem (PCP) instance. Even deciding whether a single *closed, non-generic*
exception Witness reaches the result would decide that instance. Finite syntax,
a larger canonical DTO, and A5's finite concrete SCC cache cannot remove this
obstruction. The reduction and its scope limitations are given below.

This does NOT prove that sound conservative analysis is impossible. A restricted
decidable profile plus explicit incomplete residuals/fail-closed projection is
possible in principle. This package specifies the necessary fallback obligations
but does not disguise an unspecified fragment/certificate checker as a full GO.
No production file or existing test is changed. B25B stays closed.

## 1. Scope, actual starting state and root-cause chain

HEAD c777f9642c145a57c112c28216c3996128c6234b, branch fix/documentation. A5's
uncommitted evaluator repair, the root ignore WIP, all 23 Core files, six stashes,
the semantic/physical index, and frozen B25--A7 evidence are inherited inputs.
The A8 start capture fingerprints 1073 inherited files, including every A6/A7
source/report/audit/proof file and the A6 test. There is no A8 source correction
to any product or existing test. Only new A8 report/audit/proof artifacts are owned.

B25 found wrong assembly ownership; A exposed nested/constructed problems; A2
located loss before type resolution; A3 isolated OriginalDefinition context loss
and structural growth; A4 found the independent finite recursive-cache defect;
A5 repaired that finite cache; A6 proved build-before-SCC exact-materialization
divergence; A7 narrowed the remaining obligations to correlation, catch residuals
and SCC/projection. A8 investigates exactly these three, not a new A/B/C survey.

The actual product still drains the graph queue before evaluator/SCC execution,
normalizes call contexts/keys and catches through definitions, and performs the
unsafe final declaration-ID lookup. Its unchanged tests can therefore pass while
characterizing a known defect. Fresh green regression evidence is not evidence
that this proposed contract is implemented.

## 2. Common finite type/substitution vocabulary

An owner is assembly identity, module identity, declaration identity and nested
containing chain, with an existing source-bound identity where metadata IDs do
not suffice. Var(v) identifies (owner, Method/Type scope, ordinal), not a name.
Terms are finite nominal constructor trees with containing terms and ordered
argument terms, scoped Vars, or existing exact ground canonical types. Arity,
constraints and owner consistency are part of validity. All constructor labels
are exact nominal owners; A<int> and a same-named type from another assembly
are distinct. No string reconstruction or OriginalDefinition semantic quotient.

A simultaneous edge substitution sigma_AB maps B's binders to terms in A's
scope. Root rho_A maps A's binders to exact ground terms. Define structurally:

    apply(id,t) = t
    apply(sigma,Var(v)) = sigma(v) if supplied, else the same declared free Var(v)
    apply(sigma,C(t1,...,tn)) = C(apply(sigma,t1),...,apply(sigma,tn))
    (tau after sigma)(v) = apply(tau,sigma(v))
    rho_B(v) = apply(rho_A,sigma_AB(v))
    rho_C = (rho_A after sigma_AB) after sigma_BC

Missing binding in a required closed environment is invalid, not a free-variable
identity case. Method and every containing/nested scope have disjoint binder
domains. Conflicting assignments to one binder are rejected, not combined.
Composition is associative with id as unit by induction on finite term structure;
it is not commutative. Apply does not recursively chase T->List<T> inside its
own replacement. Such recursion belongs to edge iteration, not term normalization.

Normalization: retain exact owners; normalize already supported runtime-equivalent
language forms using the established canonical rules; alpha-identify binders by
scoped IDs; sort maps by binder ID; intern equal finite term DAGs; remove explicit
identity assignments. Do not inline recursive substitutions. Term equality is
structural nominal equality. Equality of finite normalized equation syntax is a
safe sufficient cache-equality test, not a general semantic family-equivalence
procedure. Semantic inequality must not be inferred from different syntax.

## 3. PO1 - correlated recursive families

### Finite synchronized trace representation

Recommended analytical IR: a finite parameterized equation graph, whose node is
(owner-qualified callable definition, existing relevant finite value-fact state).
Each node binds its own whole formal environment tuple. An edge has stable
call-site/target identity, one simultaneous substitution tuple, source/evidence
provenance, and caller-scoped catch predicates. A source has a nominal type
expression, evidence/location identity and local reachability guards, including
positive handler-entry predicates for sources thrown inside catch bodies.
Recursive references point to equation
IDs; they do not embed another concrete environment in the pending queue.

For a root (d,rho) and a finite call trace pi=e1...ek to a local source expression
E, compose the maps in that trace and instantiate E once. ALL occurrences and
ALL coordinates use this same trace/environment. Intermediate environments are
lexically bound per invocation, not one globally overwritten Var(T). This defines
a set of (exact type, evidence, source, trace) witnesses over all finite traces.
The escaping set additionally satisfies every applicable catch predicate in the
appropriate invocation frame (section 4). Infinite runtime calls need not execute.

This preserves Pair<T,List<T>>, Pair<T,T>, and tuples of method/containing
parameters. There is no independent choice of trace for each tuple component.
Recursively referring to the same body does not mean instantiating its sources
with the same ground T on every iteration. Projection binds only the entry
environment; the recursive formal environment is still transformed on each edge.

Operations on this finite description:

| Operation | Finite construction / meaning | Decision limitation |
|---|---|---|
| Constructor | Add an exact owner-labeled term over existing Vars | Validity checked with real destination Roslyn types |
| Union | New entry with alternatives to finite equation blocks | Union of witness sets; do not split correlated coordinates |
| Substitution | Compose an entry/edge environment, preserving shared binders | Finite map application, not recursive unfolding |
| Composition | Scoped simultaneous tuple composition as above | AST may grow if inlined; keep DAG/composition references |
| Normalization | Canonical local term/map/block syntax and stable owner-ordered IDs | Not an oracle for denotational equivalence |
| Equality | Structural normalized graph/entry equality implies same denotation | General semantic equivalence blocked by section 5 |
| Recursive reference | Equation ID with formal arguments | Finite syntax; all finite traversals give the meaning |
| Ground membership | Match a ground term against generated witnesses | Decidable in some productive restricted profiles, not assumed generally |
| Matching/emptiness | Ask whether any witness satisfies a predicate | Correlated parametric catch case can encode PCP |
| Owner / containing chain | Constructor labels and separate outer-to-inner argument maps | No name-only or first-match fallback |

### Required recurrence cases

| Case | Exact finite description | What must not be concluded |
|---|---|---|
| T->T | Identity edge/environment | Recursion alone is not uncertainty |
| T->int | Constant substitution, finite exact states | Do not keep an open T at projection |
| T->List<T> | One recursive rule Q(T)->Q(List<T>) | No list-depth budget; no concrete node per depth |
| T->Dictionary<string,T> | One owner-qualified constructor context | Still a term transformation, not a definition self-loop |
| T->Pair<T,List<T>> | One shared binder occurs twice in the expression | Do not replace by independent X,Y languages |
| T->F<G<T>> | Composed finite constructor context | No text replacement or arbitrary F/G owner matching |
| Several parameters | One tuple transformed by the same chosen edge | No cartesian product of coordinate histories |
| Method + containing | Separate scoped binders, one correlated tuple | Ordinal zero in two owners is not one variable |
| Nested owners | Preserve all constructed containing scopes | Inner construction does not erase Outer<int> |
| Mutual recursion | Finite mutually recursive equation block | Body SCC finiteness is not solved semantic closure |

Regular tree-language independence is not assumed. Repeated variables and shared
choice histories can impose equality/correlation constraints outside ordinary
independent regular-tree grammars (A7's diagonal argument still applies).
The finite IR represents these constraints; it does not decide them by magic.

PO1 result: **finite exact denotational representation defined; full required
effective semantic equivalence/matching not closed**. This is not a production GO.

## 4. PO2 - catch split, residuals and closure

For a ground source t and catch c in one exact environment, catches(t,c) iff t
equals c or one of t's correctly substituted class bases equals c. Generic class
arguments, nested containing terms and assembly/module identity participate.
Do not compare OriginalDefinitions. A catch-all removes every matching exception;
an unresolved catch filter cannot justify definite suppression.
The language premises are the official C# rules for
[constructed types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/types)
and [constructed base classes](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes);
the family/guard semantics below are the proposed analyzer contract, not a claim
that the C# specification defines this analysis.

For a synchronized family of *witnesses* F and a catch predicate P in the correct
frame environment, define:

    caught(F,P) = { w in F | P(w) }
    remainder(F,P) = { w in F | not P(w) }
    F = caught(F,P) union remainder(F,P)
    caught(F,P) intersection remainder(F,P) = empty

Use witnesses until after filtering. The same concrete type can arrive through
different frames/traces with different catches. Prematurely unioning ground type
names and dropping environments loses the necessary distinction.

Represent Select(P,F) and Select(not P,F) as finite guarded references to the
same family. Adding a guard is finite and preserves the correlation. It is closed
as denotational syntax under intersection/difference. It does NOT follow that
emptiness, equivalence, or conversion to an exact list of result facts is decidable.
Finite guarded syntax and a usable decision procedure are separate obligations.

Concrete partial-cover example: F={Fault<List^n<int>,List^n<int>> | n>=0};
catch Fault<int,int> removes n=0 only. The exact residual has n>=1. Catching
Exception removes the whole family. Catching Fault<string,string> removes none.
An inherited Derived<int,string> is caught by Fault<int,string>, but
Fault<int,string> is not caught by Fault<int,int>. The finite harness checks the
closed examples without pretending that six observations prove an infinite law.

For parametric catches, the predicate may compare coordinates of the SAME
environment, e.g. Fault<X,Y> against Fault<X,X>. Replacing it by a constant catch
or by a Boolean on the outer Fault definition destroys precisely the relation
needed by the residual. Section 5 proves a general decision obstruction here.

No approximation is silently adopted. A conservative alternative would need
(L,U,complete,reasons), where L contains only proved exact facts and U denotes
all possible witnesses, retaining correlations/guards. If a decision is Unknown,
both possible effects must remain represented; never remove a possibly escaping
source or call it a proved exact result. Any coarsening must prove
actual escaped witnesses subset concretization(U), identify its precision loss,
and make incompleteness visible at projection/finding admission. Retaining a
guarded residual is preferable to silently propagating an unfiltered family.

PO2 result: **exact residual syntax and partition law defined; general required
semantic decision/projection for that syntax is not closed**. No production GO.

## 5. Precise decision obstruction: legal generic catches encode PCP

### External premise and confidence scope

PCP asks whether a finite list of word pairs has a nonempty index sequence with
equal concatenated words; no total algorithm decides all instances. The primary
research source used for this premise is Vesa Halava's proof,
[Another proof of undecidability for the correspondence decision problem](https://arxiv.org/html/1411.5197v1),
Problem 1 and its reduction. The original Post PDF was inaccessible and is not
claimed as inspected. The following reduction to generic source/catch semantics
is our analysis, not a statement quoted from that paper.

### Encoding and reduction

Use real nominal C# classes End, A<T>, B<T>, and invariant Fault<X,Y>:Exception.
For a binary word w, Enc(epsilon,t)=t and Enc(a.w,t)=A<Enc(w,t)> (similarly B).
By nominal constructed equality, Enc(u,End)=Enc(v,End) iff u=v.

For every tile (u_i,v_i), give the finite generic method Search<X,Y> an edge

    Search<X,Y> -> Search<Enc(u_i,X), Enc(v_i,Y)>.

A root chooses and applies ONE tile before entering Search, excluding the empty
trace. A finite input sequence selects further tiles or selects Stop. Prepended
constructor encoding composes tiles in reverse call order; reversing the entire
index sequence is a bijection over all nonempty sequences, so existence of an
equal pair is exactly PCP existence (do not reverse letters within a tile).

At Stop<X,Y>, the legal C# shape is:

```csharp
try { Throw<X,Y>(); }                 // throws new Fault<X,Y>()
catch (Fault<X,X>) { throw new Witness(); }
catch (System.Exception) { }          // non-diagonal Fault is swallowed
```

Input selection can be a Func<int> reading any finite tile/stop sequence; the
catch/type-identity part does not depend on exact modeling of the input mechanism.
All source is finite. For any fixed finite PCP instance, replacing each tile by
its finite generic constructor expression gives legal C# of the same shape.
Empty tile words are identity argument expressions, also legal. Fixed compiled
owner labels ensure that unrelated constructors cannot accidentally compare equal.

For every finite trace pi, Stop throws Witness iff its two constructed arguments
are equal. Hence:

    Witness in the exact projected escaping result
    iff some nonempty shared tile trace is diagonal
    iff the PCP instance has a solution.

Therefore a total exact operation deciding this projected membership, or whether
the diagonal caught family is empty, would decide PCP. General semantic family
equivalence to Empty also entails that impossible decision. This is stronger
than saying that some sources are nonregular: even the final observable result
can be just the one CLOSED non-generic type Witness.

### What this proves, and what it does not

This is conditional on admitting the full family/parametric-catch class described
above with exact decision requirements. It does not prove that the current
definition-folding analyzer already faithfully implements these programs, that
every generic loop is hard, that every ground query is undecidable, or that a
sound overapproximating analyzer cannot terminate. A conservative analyzer may
reject a syntactically unsupported profile or return explicit incomplete evidence.
Such a policy is a changed admission/completeness contract, not an exact solver.

The family/catch-emptiness obstruction uses only nominal constructor tuples,
shared regular tile traces and a diagonal predicate, not arbitrary C# arithmetic,
heap computation or general Turing-machine reachability. The catch-body Witness
example additionally requires precise positive handler-entry guards. An analyzer
that already overapproximates handler reachability can avoid that exact query,
but then must explicitly classify its precision/completeness policy. The primary
obstruction is the correlated family predicate itself, not a claim that every
existing conservative exception analyzer is impossible.

Lazy Select/Except syntax can construct the answer symbolically without deciding
its emptiness. Safe structural cache equality can also remain decidable. These
are real partial solutions, but do not satisfy a requirement for total exact
semantic query/equivalence and concrete complete projection for the full class.
No finite timeout/search depth proves that an unmatched instance has no solution.
An indefinitely growing search would violate A8's termination requirement.

### Executable finite cross-check (not the undecidability proof)

FamilyProof.cs under A8Proof compiles actual valid C# and runs only finite traces.
It checks 40 Roslyn/runtime assertions: all requested construction forms, shared
parameters, nested method/container construction, mutual generic calls, closed
catch partitions/inheritance, and diagonal catch behavior. Two tiles (a,aa) and
(aa,a) give a positive two-tile trace; one tile alone is non-diagonal. The emitted
assembly really throws Witness for choices 1,2,0 and returns without Witness for
1,0. Fourteen finite shared traces illustrate correlation, not a solver bound.
No infinite product analysis or unbounded C# recursion is run. No Historical
compiler is loaded. A finite example does not prove the general undecidability
theorem; the theorem plus explicit reduction does.

## 6. PO3 - SCC meaning, completion and caller projection

### Nodes, edges and finite construction

Choose parameterized BODY nodes, not unbounded A<int>/A<List<int>> body nodes.
Node identity is owner-qualified definition plus relevant finite value-fact
partition and input/scope profile. An edge identifies call-site, target, simultaneous
map and caller-scoped catch obligations. Every source is an expression with
provenance. SCC identity is the finite body/fact topology. A recursive substitution
cycle is a path in that topology with a nontrivial composed map, not an identity
semantic revisit. Type-dependent dispatch/facts must be preserved conservatively;
their independence from generic arguments is not assumed.

With N body/fact states, E edges and S finite term/guard input syntax, registering
the equation graph needs O(N+E+S) shared syntax, plus existing fact-domain costs.
Stable IDs come from owner/call-site ordering, with scoped recursive references;
canonicalization must not recursively hash/unfold a cycle. Union and residual
wrappers add finite references. Inlining substitutions across cycles reintroduces
A6's problem and is forbidden. Term-DAG sharing avoids immediate copying but is
not a theorem about all semantic query costs.

### Completion has two meanings; do not confuse them

Structural closure: every reachable definition/fact state has its body, sources,
maps, guards and references registered. No new syntactic rule remains. That is a
finite equation-block completion certificate.

Semantic closure: a result/query operator has a proved complete solution in its
admitted domain. No semantically new escaping fact remains unresolved. In the
general correlated guarded class, a total exact certificate/decision procedure
cannot be supplied for section 5. Merely setting a structural-complete flag must
not publish a complete concrete exception summary.

Identity/constant/permutation-only finite substitutions give a useful decidable
subcase. If m live slots range only over k closed seed/program-constant values,
there are at most k^m ground environments (per body/fact state); repeated exact
states bound the queue mathematically, not by a magic iteration count. A5 applies
to that actual finite ground graph after correct construction/catch substitution.
Constructor wrapping of live recursive variables falls outside this simple
finite-domain certificate. Rejecting that larger class conservatively is not a
proof that all its members diverge or are undecidable.

### A5 integration and cache

Reuse one graph/session orchestration and SCC discovery; do not bolt on a second
independent recursion engine. Preserve A5's prohibition on unrestricted reuse of
partial results and per-evaluation/root isolation. A5's current cycle-erasure and
(key, excluded members) cache proof applies to the frozen finite *semantic-state*
graph. A definition revisit with changed substitutions is not such a repeat.

Cache separately: (1) structurally closed parametric equation blocks; (2) complete
projected results with root environment, fact/scope/filter/query profile and
closure identity. A sound structural cache key includes exact owner/input profile,
canonical block ID, whole correlated seed map, query kind, relevant caller catches
and A5 recursion binding when the finite solver is used. Incomplete/unknown results
must carry their status/reasons and cannot satisfy a complete-result cache lookup.
No semantic-family-equivalence oracle may be hidden in cache-key normalization.

For M<int>, M<string>, M<int> again: the body block is shared, closed projections
differ when relevant, and an identical complete projection may be reused. Do not
globally replace all formal T in a recursive block by int when binding its entry.

### Projection and catches

For the finite example S_A(T)={Fault<List<T>>}, entry A<int> gives exactly
Fault<List<int>> by scoped substitution. For S_A(T,U)={Fault<Pair<T,U>>},
A<int,string> gives Fault<Pair<int,string>> with the same tuple assignment.
Nested owners are substituted outer-to-inner, retaining their own binder domains.

Filtering commutes with projection only when BOTH source AND catch terms and
frame environments are substituted capture-free together:

    project_rho(Select(P,F)) = Select(P_rho, project_rho(F)).

It is unsound to filter by open OriginalDefinitions and substitute the source
later. An infinite symbolic family can be instantiated as an entry/reference
without enumerating it, but converting it into a complete exact finite list is
a separate operation. Section 5 shows that this is not just a serialization-size
problem: even a singleton possible output type can require an undecidable query.

PO3 result: **finite structural construction and finite concrete subcase defined;
full semantic SCC/query completion and exact result projection remain OPEN**.

## 7. Cross-project and compiler-owner contracts

A->B: resolve B's unique assembly/module/declaration identity in its source scope;
reconstruct its containing chain outer-to-inner; retain separate method arguments
on ConstructedFrom. sigma_AB maps B's formal owners to A terms. Rebind ground
leaves exactly, not by name or first declaration match. Validate arity/constraints.

A->B->C: compose sigma_BC through sigma_AB and rho_A, not independently normalized
declaration IDs. Example B.P<T,U> passes C.Q<List<T>,U>; A calls B.P<int,string>:
C receives (List<int>,string), and Fault<Pair<C.X,C.Y>> projects to the correct
owner-qualified Fault<Pair<List<int>,string>>. Repeated occurrences of T use the
same assignment. For Outer<int>.Inner<string>.M<bool>, preserve all three scopes.
Missing/ambiguous owner or malformed nesting is an explicit unsupported relation,
not a best-effort reconstruction. Cross-project cyclic SCCs are not newly admitted
unless the existing semantic scope actually supports them.

Current/Historical use the same neutral owners, map algebra, admitted profile,
normalization, completeness and reason IDs. Roslyn symbols remain process-local.
Different compiler object identities may not decide semantic family equivalence.

## 8. Canonical capability, uncertainty and the explicit wire decision

| Item | Existing capability | Minimal proposed contract / gap |
|---|---|---|
| One ground concrete type | Existing canonical nominal type tree | Reuse exact owners/arguments; current resolver still needs correct nested rebind |
| Parametric term | Scoped TypeParameter identities exist | Explicit binder environment, simultaneous substitution and closed/open validation |
| Correlated recursive family | Not one concrete type identity | Finite parameterized block + shared trace/environment + recursive IDs |
| Catch remainder | Current Boolean/definition filter cannot express it | Finite guarded family reference, frame-aware predicate, explicit completeness |
| SCC/projection fact | Existing result enumerates exact types | Distinguish structural equation closure, complete exact projection and incomplete residual |

Wire decision for the recommended next-step profile: **do not transmit families
to Main; keep the current wire shape**. Worker-local projection may return the
existing success/result DTO ONLY when exact finite completeness is proved. If it
cannot prove projection, return Success=false, Result=null, Failure.Code=
UnsupportedContext with deterministic semantic reason details; no partial successful
FindingDirect. Current must use the equivalent fail-closed outcome. This decision
does not claim that a full family-consuming Main could use the same wire; exporting
families later would require a separate version-neutral DTO/protocol contract.

Existing WorkerResponse/WorkerFailureCode already have this failure shape, so no
new wire field is inherently required by the rejection policy. It does NOT solve
the decision of which general families are fully projectable. A decidable admitted
profile/certificate checker must be fixed before this becomes an implementation GO.

| Semantic uncertainty / unsupported reason | Trigger | Propagation and meaning | Current/Historical |
|---|---|---|---|
| OwnerBindingUnavailable | Missing/conflicting/ambiguous owner or required map | Preserve blocked source relation internally; projection cannot claim completion | Same canonical owner check/reason |
| UnsupportedCorrelatedCatchQuery | Needed equality/difference query outside a proved decision profile | Keep guarded residual, not Boolean suppression; no exact finding from it | Same syntactic profile/certificate test |
| FamilyProjectionIncomplete | Family cannot be completely represented as finite exact result under selected profile | Whole semantic request fails closed at current wire boundary | Same failure/no partial success |
| CatchRelationUnknown | Inheritance/filter relation cannot be proved in the exact scoped environment | Both possible outcomes retained as constrained possibilities; incomplete | Same relation/status, no backend special case |
| StructuralClosureUnsupported | Live type-transforming cycle outside the admitted closure algorithm | Not "recursion = uncertain"; identity/constant finite cases are not rejected for being recursive | Same graph/map property |

No reason is "depth N", elapsed time or retry count. Soundness, precision and
uncertainty are distinct: an overapproximate upper set may be sound but imprecise;
such facts are not Proven exact sources. Any coarsening must keep the whole tuple
relation or explicitly prove/label its loss. A8 adopts no production approximation.

## 9. Effective operations and complexity boundary

| Operation | What terminates / bound | Full-class status |
|---|---|---|
| Finite term equality/substitution | Structural DAG walk; sharing avoids immediate duplication | Defined |
| Finite equation graph construction | Finite source/fact states and input syntax | Defined, subject to explicit fact profile |
| Syntax equality/canonical block IDs | Stable finite labeled graph comparison/numbering | Safe cache relation, not semantic equivalence |
| Union/Select/remainder syntax | Finite references and predicate nodes | Denotational closure defined |
| Finite-domain concrete SCC | At most N*k^m states; A5 exclusion contexts may be exponential in SCC size | Defined for the admitted finite-domain premise |
| Regular restricted algorithms | Potential determinization/product exponential growth | Do not generalize to correlated guarded class |
| Exact guarded emptiness/equivalence/projected Witness membership | Would decide PCP | No total algorithm for full admitted class |
| General projection to exact concrete result | Even finite result membership can carry same obstruction | OPEN / no full GO |

Predicate/path condition normalization to arbitrary equivalent forms is not
required for safe syntax reuse, but exact semantic queries cannot be replaced
by syntax equality. Keeping a finite input graph as an opaque answer is not
proof that all required analyzer operations are practicable or decidable.

## 10. Characterization preflight and architecture impact

The current repository-wide capture and exact assertion/case table are in
`P5O2B25A8Proof/Characterization-preflight.md` and the A8 inventory JSON. No test
is changed. Earlier A6 permissions remain conditional and exact, not automatic
authority for body-vs-projection test redesign, uncertainty assertions, new
structural-family results or a later package. See that table for additional
permission requirements; the A6 witness itself is now frozen evidence.

| Component | Change necessary for later profile? / why | New invariant | Risk | Required later tests |
|---|---|---|---|---|
| ExceptionFlowCallContext | Yes: whole scoped generic environment | Preserve correlated method/container bindings | Capture/normalization loss | method+containing+nested, same-name binders |
| ExceptionFlowCallableKey | Yes: distinguish finite body key and projection key | Identical closed projections reuse, relevant different roots do not collapse | Infinite node expansion or aliasing | int/string/int, body sharing |
| Graph Builder | Yes: register finite equations | No concrete-depth materialization | Type-dependent dispatch omitted | all constructor recurrences |
| Pending Queue | Yes: body/fact schedule | Schedule each admitted finite state once | Concrete env accidentally queued | identity/constant/growing witnesses |
| Node representation | Yes: binders, sources, completeness | Structural complete != semantic complete | Premature publication | recursive block completeness |
| Edge representation | Yes: simultaneous map + frame catch | All coordinates share one trace | Cartesian history mixing | multi-parameter/correlated tiles |
| Source representation | Yes: term/family + provenance | Exact owner/location/evidence preserved | Open term mislabeled concrete | nested/constructed source projection |
| Summary Evaluator | Yes: admitted query/closure contract | No unsolved family reported complete | PCP-like query diverges | profile boundaries and fail-closed |
| A5 recursion context | Preserve finite rules, generalize only with proof | No provisional result reuse | Definition cycle erased despite changed map | all A5 33 plus changing-cycle cases |
| SCC detection | Reuse finite topology algorithm | Topology identity not semantic-state identity | Unsupported cycle conflation | finite/mutual/equation SCC |
| Cache | Yes: block vs complete projection/status | Incomplete cannot satisfy complete lookup | Root/filter/profile aliasing | root order/repeat/mutation isolation |
| ExceptionFlowCaughtTypeFilter | Yes: scoped relation/residual | Universal suppression only when proved | Fault<int>/Fault<string> false catch | All/None/Mixed/Unknown, inheritance |
| Supporting-source resolver | Yes: transfer whole environment | Destination scope owns mapped terms | Method map dropped | A->B generic method/container |
| Cross-compilation rebinding | Yes: exact owner chain and map | No first-match fallback | Same-named wrong assembly | A->B->C, owner/arity/ambiguity negatives |
| Canonical type representation | Reuse ground type; new internal family IR | No family disguised as one type | Fake Fault<>/Object | syntax/owner/binder normalization |
| Uncertainty | Yes: typed deterministic incomplete outcome | Not an exact Proven result | Silent precision loss | unsupported profile reasons |
| Historical Worker | Shared algorithm/profile; reject incomplete projection | Historical Roslyn stays isolated | Backend drift | genuine worker parity/lifetime |
| Wire Contract | No extension for selected concrete-success/failure-only profile | No symbolic family or Roslyn handle crosses | Partial success leaks | Result=null on unsupported |
| Main Process | Yes: honor unsupported semantic outcome | No optimistic empty success | Wrong report/finding admission | route+failure integration |
| Reporter/finding boundary | No activation; later completeness guard needed | No incomplete family admitted as exact finding | False positive/negative claims | unchanged B25B closure assertions |

## 11. B25B readiness matrix

CLOSED means proved in the stated existing production scope. CONTRACT-DEFINED
is a specified finite algebra/invariant, NOT an implemented end-to-end repair.
OPEN includes the full generic path where any required decision/projection is
missing. A green old characterization suite does not upgrade a row.

| Requirement | Status | Scope / reason |
|---|---|---|
| Assembly ownership | OPEN | Exact owner contract defined; unsafe product final normalizer still present |
| Constructed identity | OPEN | Individual canonical identity exists; end-to-end source/catch flow still loses it |
| Method arguments | CONTRACT-DEFINED | Scoped finite bindings; no production adoption |
| Containing arguments | CONTRACT-DEFINED | Separate containing environments |
| Nested owner | CONTRACT-DEFINED | Outer-to-inner exact nominal chain |
| Substitution composition | CONTRACT-DEFINED | Capture-free simultaneous algebra/orientation specified |
| Structural recursion | OPEN | Finite IR not a full terminating semantic solver |
| Correlation | OPEN | Exact synchronized denotation defined; full query algebra blocked |
| Catch remainder | OPEN | Guarded syntax closed, exact general query undecidable |
| SCC closure | OPEN | General semantic closure; A5 finite concrete SCC repair remains CLOSED |
| Result projection | OPEN | General closed Witness membership already embeds PCP |
| Cache soundness | OPEN | A5 finite cache CLOSED, new family/projection cache not implemented/proved |
| Cross-project | CONTRACT-DEFINED | Exact owner-scoped map composition; no new implementation |
| Current correctness | OPEN | New generic contract not implemented |
| Historical parity | OPEN | Existing parity passes; no new family contract parity proof |

B25B remains closed. A8 introduces no new finding admission in any mode;
the existing bounded Direct/ProjectTransitive contracts remain unchanged.

## 12. Smallest next step, not another unrestricted solver search

**P5O2B25A9 - Decidable Query Profile and Incomplete Projection Certificate
(contract/proof only).** Fix ONE syntactically decidable admission profile for
correlated recursion/catch queries and ONE fail-closed projection policy. Do not
try to decide arbitrary PCP-encoding parametric predicates.

Minimal deliverables: (a) explicit profile grammar for maps/sources/catches and a
terminating admission checker; (b) exact operations/certificates for that profile,
starting with finite-domain substitutions and clearly delimited structural cases;
(c) unsupported predicates retained as relational residuals, or whole-request
UnsupportedContext without partial results; (d) proof that every exact-success
projection is complete and Current/Historical rejection is identical. If no
structural correlated case can be supported with those guarantees, report that
limitation, do not turn it into a hidden depth heuristic.

All future implementation steps remain gated: first define/prove the profile;
then explicit characterization permission; then modify context/body/edge/source
IR; generalize the ONE SCC/evaluator pipeline; implement complete projection and
exact rebinding; integrate the existing success/failure wire; finally run the
full regression/baseline/evidence matrix. Stop/rollback points: any unproved
map/catch operation, any baseline change, premature completion, unsupported-result
success, unforeseen old assertion conflict, or Current/Historical semantic drift.
No production implementation package is authorized by A8.

## 13. Reproduction and actual validation

See `P5O2B25A8Proof/README.md` and the accompanying audit. A new isolated A8 export
builds the unchanged A5/A6 code and runs the required witness/combined/Broad/Full/
Boundary/Dual suites. Every attempt retains actual exit, times, log and fresh TRX.
The original A6/A7 evidence is never overwritten. Their verifier algorithms are
replayed with only output/record roots redirected; A6's original serialization
revision remains historical, not an A8 source correction.

The separate finite proof project has no product project reference or package
version change. It references the actual freshly built Current compiler binaries;
its source/project, input DLLs, emitted image and result are fingerprinted.
One initial launcher command marshaled the argument array as a comma-joined
single argument, so dotnet rejected the command BEFORE compilation. That exit/log
and source copy are retained. A corrected script invocation (not changed C# or
assertions) builds in a separate finite-proof-r2 directory with zero warnings/
errors and passes all 40 checks. No existing regression gate is retried.

The A8 gate separates fresh runtime ValidationPassed, finite-proof checks,
evidence consistency and Decision=STOP. Static consistency is not an automatic
mathematical theorem prover. No new Self Analysis or Canonical Finding Diff;
historical values remain historical. Final HEAD/status/index/Core/stash/history
fingerprints are bound by the audit. No actual Linux execution is claimed.

### Fresh unchanged-source A8 results

| Validation | Actual result |
|---|---|
| Initial test-project build | PASS; zero warnings/errors |
| Full Dual-Version | PASS; all eleven WAE builds clean |
| A6 witnesses | 8/8 passed |
| A5 separate Dual gate | 33/33 passed |
| Combined historical/context suite | 134/134 passed |
| Broad | 3319/3319 passed |
| Full | 3410/3410 passed |
| Boundary | 596/596 passed |
| Current baselines | Eight outputs byte-identical to frozen reference bytes |
| Separate analytical harness | 40/40 finite Roslyn/runtime checks passed |
| Compiled source bindings | 243 PDB documents; eight pure line-ending differences retained |
| Fresh A6 Evidence Gate | PASS; actual child-process exit 0 |
| Fresh A7 Consistency Gate | PASS; actual child-process exit 0 |

All listed test runs have zero failures and zero skips. Build, Dual, witness,
combined, Broad, Full and Boundary each have one fresh recorded attempt and actual
exit 0. No unchanged test gate was retried. Full also passed the unchanged
MultiModuleAssembly_FailsClosed fixture (0.0485208 s) and concurrent reconstruction
fixture (2.9573246 s); no opportunistic repair or separate retry was performed.

Fresh A6 regression and A7 consistency outputs are separate mirrors under
artifacts/p5o2b25a8, not rewritten historical audits. Actual child-process exits
for those gates and the final A8 proof/evidence gate are recorded in
runs/a6-gate.json, runs/a7-gate.json and runs/a8-gate.json respectively. The A8
audit binds their completed inputs/results; a green consistency gate never
changes the scientific STOP decision into an implementation GO.
