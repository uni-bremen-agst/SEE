# B25B Main-local receipt contract

The outer operation is SolutionTransitive; its two actual subqueries are not
aliases of that mode. Direct uses the existing single-project Direct Finding
profile, Summary uses the existing Solution flow profile without any
Finding/Declared/Direct opt-in. No Worker protocol, engine profile or version changes.

`HistoricalSolutionFindingOperation.TryPrepare` first projects original P4/P5
handoffs using the existing solution projector and validator. It further admits
only two/three-node chains, one target, no branch/diamond/cycle/linked-source or
implicit project discovery. Its Direct input must equal that original target
after removal of precisely the Direct subquery mode/document opt-in.

The operation retains exact serialized Direct and full Solution snapshots,
including source/parse/reference-image and full assembly/module/MVID/dependency
worlds. It registers two distinct neutral single-use capture tickets for the
exact two prepared request objects. A ticket is not execution authority.

`HistoricalWorkerClient.AnalyzeAsync` consumes that request's capture registration.
Its existing private B21 exchange capability is minted only after real request
exchange, exit and both bounded pipe EOFs, and matching physical Worker image
before/after execution. Cleanup still records exit from the owned handle, never
numeric-PID lookup. A second invocation of the same request cannot reacquire its
operation ticket. Legacy unregistered flow calls retain their old transport behavior.

`HistoricalWorkerResponseValidation.Validate` is the only receipt mint boundary.
All existing pinned Worker/Analyzer/Roslyn identity, canonical result, Proven-only,
uncertainty/truncation, full reference/source/path/provenance checks precede mint.
An additional private CWT binds the original returned call to its exact input
object, serialized executed input, immutable result/identity/provenance snapshot
and original capture ticket. Record clones and raw response revalidation mint none.
B24's local Finding facts and OwnershipSnapshot remain unchanged.

`TryAdmitDirect` checks the registered Direct call and original validated
Direct/document facts. `TryComplete` also checks the separately registered
Summary call against the unchanged complete Solution snapshot, same actual
validated Worker image/identity and exact original-PE type world. Only a valid
pair is consumed; replay is rejected. Equal roots/results from different real
graphs or operations do not suffice.

The Summary wire request necessarily executes `Session.Analyze`, not local
Direct or raw graph construction. The unchanged A10 Worker rejects OUTSIDE
before canonical success and rejects incomplete/uncertain/truncated results.
Thus no extra caller-supplied Complete flag or new Wire DTO is needed.

Worker XML/cref bindings and root anchors come solely from the Direct document
receipt. New Summary exception displays are resolved with the existing exact
canonical original-PE resolver. For an originally metadata-bound selector whose
dependency symbol was unresolved, the already validated supporting PE owner may
be used only when it is in the reporting target's exact reference world; the
original graph already checked its complete Assembly/Module/MVID edge. No new
compilation, name-only resolution, metadata lookup heuristic or source rebind is
introduced. Generic final exception/doc identities remain outside this narrower
Finding profile even if their internal analysis is A9-exact.

Only then does `FindSolutionExternalExceptionSmellsAsync` call the existing Main
`RenderBoundedFindingEvidence` / `AddExceptionFindings` policy. Supporting projects
never become reporting roots. Canonical owner-distinct entries remain distinct;
no name/location/collection-position deduplication is added. The trusted no-doc
guard may return no Findings after validated input/Direct/doc admission without
running Summary; it does not claim an unexecuted Summary complete.

Current selection, Historical selection, isolation, original Worker failure
envelope, A9/A10 admission, exact substitution/catches and A5 SCC/cache are unchanged.
There is no raw-graph bypass or alternative test renderer. Native Current
Session/policy witnesses are independent oracles; parity is additional evidence,
not their only correctness assertion.
