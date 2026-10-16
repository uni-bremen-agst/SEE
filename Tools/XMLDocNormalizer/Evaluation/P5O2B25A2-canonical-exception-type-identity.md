# P5O2B25A2 — Canonical exception type identity

## Starting contract and protection

HEAD `4d90ce25ae143f90e968afdc8e5b6f5a7a07d82f`; B25A is existing uncommitted
audit WIP, not an A2 change. The root ignore is foreign protected WIP. Initial
capture preserves each existing B25A file before edits, original source/test/
build hashes, actual Core inventory, stashes and both semantic/physical index
state. The semantic index is clean. Its physical hash differs from B25A's old
snapshot; the user explicitly accepted documentation only, no index repair.

Permission covers exactly four B25A defect characterizations and their directly
emitted witnesses, plus the previously authorized two B25 characterizations.
Other assertions remain unchanged. B25/B25A reports and JSON audits are frozen.
Historical evidence verification is read-only and writes only A2 output. Diffs
to overlapping test/build/register files must be relative to captured B25A WIP,
not presented as changes newly introduced relative to HEAD.

No commit/push/stash operation, original Core build, Acquisition repair,
compiler/routing change, Finding activation or B25B work is authorized.

## Phase 1 — existing path, before any production edit

The semantic model supplies the actual thrown ITypeSymbol. The shared
`TryAddSummaryThrownTypeSource` accepts named exception types or bounded named
constraints for a thrown type parameter. `AddSummaryThrownSource` deduplicates
per operation by OriginalDefinition but stores the original INamedTypeSymbol
in `ExceptionFlowSummarySource`. Construction is not erased in stored sources;
the operation-local definition-only deduplication must also be checked.

Across B15 projects, the graph stores these symbols from exact owning
compilations reconstructed from validated source/reference worlds. B12/B15 bind
PE bytes, full assembly identity, module/MVID, aliases, ordered reference world
and source ownership; B21 attests the completed exchange. These facts are not
replaced by a freely supplied identity.

The evaluator's source and edge-catch paths both call
`NormalizeSummaryExceptionType`. It takes OriginalDefinition, creates a
declaration Documentation ID and accepts GetFirstSymbolForDeclarationId in the
root compilation. This erases construction and can substitute A for B because
the ID lacks assembly identity. The canonical adapter faithfully exports this
already wrong symbol; neither JSON nor Worker transport caused the loss.

The summary catch filter additionally stores caughtType.OriginalDefinition and
compares OriginalDefinition while traversing base classes. Thus it forgets
constructed arguments twice. Local Summary sources and call edges share this
filter. Direct catch handling already uses `SymbolUtils.InheritsFromOrEquals`,
which traverses actual BaseType and SymbolEqualityComparer.Default without
definition erasure. Reuse that established catch rule; do not invent a second
cross-compilation identity comparer. Normalize both summary operands into the
same root symbol universe when exact rebinding exists.

Existing `RoslynCanonicalIdentityFactory` / `CanonicalTypeIdentity.Equals`
already represent full assembly, module name/ordinal, namespace, metadata name
including arity, containing-type chain, and each constructed type argument
recursively. Module MVID is intentionally absent for symbol identities and is
separately bound by validated PE provenance. Tuple names are normalized through
the underlying tuple type; nullability is not included. No new global key or
DTO/schema is justified. Type parameters, arrays, pointers, dynamic and native
integer flags have existing explicit canonical representations; unsupported
rebinding must return null, never choose a namesake.

`RoslynCanonicalIdentityResolver` currently looks up a nested definition by full
metadata name and constructs only its own arguments, not its containing type.
It also returns an exact compilation candidate before checking the rest of the
assembly universe. A coherent resolver should resolve the containing identity
first, enumerate its exact nested metadata-name members, reconstruct arguments,
verify complete canonical equality, and accept exactly one distinct symbol.
Top-level types should use the same complete-match/unique-result rule over the
explicit root/reference assemblies, not a first candidate fast path.

`ExceptionFlowCrossCompilationResolver` already uses unique reference-ID matches
filtered by Roslyn AssemblyIdentity; it handles some constructed containing
types but assembly filtering alone does not prove the owner of every argument.
Documentation declaration ID + assembly is insufficient for constructed types.
No SymbolKey/global serialization layer is needed for the existing canonical
contract. Unrelated callable/member definition normalization is not changed.

## Phase 2/3 — identity and compiler proof

Before production changes, validate real compilable throw/catch forms for F1
assembly collisions, F2 complete nested owners, F3 generic definitions/arity,
F4 constructed and generic containing types. Also distinguish runtime type
identity from compiler representation (nullable, tuple names, aliases and
relevant argument sugar). Forwarding and module/reference expansion are not
silently added; any independent transport/representation gap triggers Stop.

Assembly/module/type/containing/argument facts required by the existing
canonical model must match together. Missing, contradictory or unavailable
identity yields no rebind; retain the authoritative source symbol under the
existing fallback contract. Multiple distinct exact candidates yield null.
Never select an assembly/source/declaration by order. Catch matching is a
separate inheritance operation on exact types, not equality of definitions.

The initial compiler-model run passed 17/17, with no compilation errors in
any fixture. Generic exception classes, generic arity and nested exceptions
inside constructed containing classes are legal in these real throw/catch
forms; they are not synthetic invalid-symbol cases. `Fault<int>` differs from
`Fault<string>`, and `Outer<int>.Leaf` differs from `Outer<string>.Leaf` even
though the existing catch filter reports a match after definition erasure.
Different containing declarations and arities remain distinct canonically.

Identity conversion is true for nullable annotation differences, tuple element
names, dynamic/object and nint/IntPtr in these compiler probes. Canonical
identity erases nullable and tuple-name sugar. Native integer symbols and
canonical identities compare equal in this actual probe. Dynamic/object remain
distinct in the existing canonical model and SymbolEqualityComparer.Default;
tuple-name variants also differ under that comparer despite identity conversion.
Thus simply replacing the summary catch filter with the existing Direct helper
would not itself prove complete runtime catch semantics. A future contract must
explicitly distinguish faithful compiler representation from runtime-equivalent
exception constructions, without silently changing global canonical equality.

Assembly identity (full version/culture/key), module ownership, namespace,
metadata names including arity, all containing owners and recursively
constructed arguments are identity-bearing. OriginalDefinition alone is not
the thrown runtime type. Nullability, tuple names and aliases do not make new
runtime exception types; the alias controls binding to an already exact owner.
MVID/reference bytes are bound by B12/B15, not inferred from names. No new
forwarding or multi-module contract is claimed: no forwarding fixture is part
of the demonstrated defect, and unsupported reference forms remain closed.
The unrelated XML-documentation cref resolver is not changed.

## Stop decision — information is lost before type resolution

The actual summary-graph observations expose a separate contract boundary:

1. A nongeneric helper whose body throws `new Outer<int>.Leaf()` stores the
   complete closed source symbol. Its later declaration-ID normalization loses
   construction, independently of cross-assembly collisions.
2. A nongeneric static root calling `GenericThrower<int>.Throw()` reaches a body
   that throws `new Outer<T>.Leaf()`. The summary already stores that open `T`,
   not the call site's `int`, **before the normalizer runs**.
3. Calling both `GenericThrower<int>.Throw()` and
   `GenericThrower<string>.Throw()` creates two root edges to the **same**
   summary key and only one open source. There are no uncertain targets.

`ExceptionFlowCallableKey` assigns `symbol.OriginalDefinition`.
`ExceptionFlowCallContext.NormalizeSymbol` also normalizes to the definition;
its canonical context builds a definition-normalized callable plus value/member
facts, not the generic call-site substitution. The canonical graph therefore
does not retain the needed per-edge/call-context construction. Although the
standalone CanonicalTypeIdentity can express `Outer<int>.Leaf`, it cannot
recover a substitution that was never retained in the canonical summary
context. Rebinding an open source cannot uniquely infer `int` versus `string`.

This is the Stop-rule boundary: **generic callable instantiation/substitution
across summary keys, contexts and edges**, in addition to the known type resolver
and catch-comparison defects. Fixing only top-level ownership and nested lookup
would leave this valid source path semantically wrong. No production candidate
was applied in A2; no withdrawal/rewrite of existing production was necessary.
The next package must specify the substitution contract and runtime catch
equivalence, then address exact unique rebinding using existing type identities.
No separate transport DTO or Historical compensation was introduced here.

This is a boundary audit, not a successful type-identity repair. B25B remains
blocked; SolutionTransitive final Finding/CLI admission remains closed. The
four newly authorized B25A methods and two B25 methods need no adaptation when
production remains unchanged; all remain byte-for-byte preserved.

Final regression/build/evidence results follow below. Inherited B25A Broad is
red (3225/3226) while Full/Dual passed; that history remains frozen. No fresh
Self Analysis/standalone Canonical Diff has been run in A2.

## Historical cause and candidate (report items 1–6)

B25's actual local project summaries retain different A/B owners for the same
declaration ID. Shared evaluation substitutes A, merges B's source into that
wrong result and makes both engines wrong in the same way. Its committed
report/audit are evidence of that old defect, not expected new correctness.

B25A's test-only candidate created the existing full canonical identity,
resolved it in the root and retained the authoritative original if no exact
rebind existed. It repaired the top-level collision in the preserved candidate
witness. It was withdrawn in full because constructed containing-type rebinding
returned null and equivalent source/PE catch symbols then failed comparison.
Separately, definition-only catches incorrectly suppress different generic
constructions. The candidate's initial graph observations also contained a
stale copied Historical probe dependency; separately refreshed candidate graph
probes passed for the two-/three-node collision cases. None of those historical
candidate outcomes is rerun, overwritten or labelled as an adopted A2 fix.

## Contract coverage and limits (report items 7–18)

The existing type infrastructure is sufficient to *represent* the complete F1
owner, F2 containing chain, F3 definition/arity and F4 recursively closed type
arguments. It is not sufficient to recover lost call-site substitutions from
definition-only summary contexts. These are separate observations, not an
assertion that CanonicalTypeIdentity itself cannot represent generic types.

| Concern | A2 finding and evidence |
| --- | --- |
| Assembly collision | B25/B25A wrong A owner remains characterized; canonical A/B identities differ. No repaired production owner is claimed. |
| Nested collision / owner | Nested chains and different containing declarations have distinct canonical identities; inherited source/PE constructed-parent rebind/catch defect remains. |
| Generic definition / arity | Real compilable generic exception definitions and arity-one/arity-two constructions differ. |
| Constructed types | Real identical constructions have identity conversion; different int/string constructions do not. Definition erasure conflates them. |
| Catch | Stored catches lose construction; compiler probes expose both false generic matches and sugar-sensitive symbol comparisons. No catch repair was adopted. |
| Summary | Closed local source survives collection; generic callee source is open before normalization; two call-site constructions share one node. |
| Exact resolution | Proposed complete identity / exactly-one result contract is documented above, not implemented after Stop. |
| Negative / ambiguity | Existing nine missing/forged/contradictory owner/module/type cases remain unchanged and green. Different owner/containing/arity/construction identities are independently distinct. No complete new ambiguity or forwarding closure is claimed. |
| Canonical flow | Full type structure is available, but actual canonical callable contexts normalize away the instantiation. No new DTO/schema/equality rule was introduced. |
| Current correctness | Compiler and pre-normalization observations independently prove the defect; semantic correction is **not achieved**. |
| Historical correctness | Three genuine Worker construction-loss witnesses equal Current and stop their processes correctly. This proves the same defect, not successful type/catch correctness. |

The 20 final A2 tests comprise 13 valid compiler throw/catch cases, one structural
four-family identity case, three actual graph-origin/context assertions and
three genuine Current/Worker witnesses. The initial 17-test observation run was
green before these explicit stop assertions and Worker cases were added; the
final C# source was then formatted, clean-built and fingerprinted. This is
development after new observations, not a retry of a failed correctness gate.

No additional old characterization method, assertion, helper or emitted
historical witness was changed. All four authorized B25A methods and both
authorized B25 methods remain unchanged, as do all other B25/B25A tests.

## Changes and protection (report items 25, 28–30)

A2 adds its own test class, report/audit and Proof scripts. It changes no
production file, routing, policy, Roslyn package or version. Overlap with the
captured inherited WIP is exactly:

- `build/Verify-DualVersionBuild.ps1`: two additive lines (audit comment plus
  separate A2 test gate), leaving the B25A gate intact.
- `Evaluation/OPEN-PIPELINE-BOUNDARIES.md`: one additive A2 boundary paragraph
  and the Last Verified label changed from B25A to B25A2.

Exact patches relative to **captured B25A WIP**, not HEAD, are
`P5O2B25A2Proof/gate-vs-b25a-wip.patch.txt` and
`P5O2B25A2Proof/register-vs-b25a-wip.patch.txt`. The delta checker reconstructs
the inherited content by removing only these additions and restoring the old
verification label. Every other inherited B25A WIP file is hash-checked unchanged.
B25/B25A report, JSON audit and Proof files are not rewritten. No existing
B25/B25A witness file needed technical updating; fresh executions emit their
unchanged witnesses only inside the new disposable validation export.

All builds run under `artifacts/b25a2-4d90/Tools/XMLDocNormalizer`, never in the
authoritative Core tree. Final evidence checks all original source/test/build
inputs (except the explicit additive gate), 23 actual Core files, root ignore,
six stash entries and the semantic index. Physical index hashes are diagnostic;
the old B25A hash discrepancy is accepted and no repair/reset is performed.
The initial snapshot and final audit retain exact hashes. No commit, push,
stash operation or repository rollback is performed.

## Validation closure (report items 19–24, 26–27)

The single configured regression sequence passed without skips or retries:

| Gate | Passed / total |
| --- | --- |
| Final A2 + unchanged B25A + unchanged B25 | 61/61 (20 + 14 + 27) |
| B24 / B23 / B22 / B21 | 42/42, 36/36, 46/46, 26/26 |
| B20 / B19 / B18 / B17 / B16 | 10/10, 39/39, 7/7, 25/25, 18/18 |
| B15 / B14 / B13 / B12 | 28/28, 31/31, 34/34, 29/29 |
| B11 / B10 / B5–B9 | 16/16, 34/34, 156/156 |
| Productive / Focused | 838/838, 945/945 |
| Broad / Full | 3246/3246, 3337/3337 |
| PID-G1 | 18/18 plus 9/9 deterministic serialized lifetime decisions |

Counts increase by the 20 new audit tests in the broad groups; no old test was
removed or relaxed. The unchanged Acquisition concurrency test passes in Broad
(1.7867138 seconds) and Full (4.7534273 seconds). Its old B25A Broad failure
remains historical evidence; one fresh passing sequence does not establish a
root-cause repair or prove it can no longer be timing-sensitive. No timeout,
sleep, retry, skip, test-harness or Acquisition/PID change was introduced.

Separate Boundary passed 596/596 without skips. The complete final Dual also
passes: Boundary 596/596, External Target 36/36, B24 42/42, B25 27/27,
B25A 14/14 and A2 20/20. All eleven WAE build steps have zero warnings/errors;
real Worker identity, deterministic smoke and fail-closed protocol checks pass.
Initial captured-WIP Dual and final A2 Dual are separately preserved; they are
required before/after validations, not retries of a failure.

Eight fresh Current cases remain byte-identical: Direct, ProjectTransitive,
ProjectTransitiveDeclaredExceptions and SolutionTransitive, each False/True.
This is a same-export comparison against HEAD plus exact captured B25A WIP,
not a regenerated baseline. Current, Historical and Worker binaries also remain
byte-identical to that fresh baseline, respectively:

```text
Current    FA2BDAC96DADF860F0FE700E457DC25E869B972CF914D225FD82EB2C14DBC619
Historical 91D50D43EF03E367D2A5D50361EBC7F6094CA685707D726EC81623452F82D2AD
Worker     5937B6ACDD8D70FA8488DB1645DC79F50304A54A5CB2CC4CC65B2EF1CCCAF692
```

These are A2 export-path images, not the different-path historical B25A image
hashes. All 121 shared sources are unchanged. Final tests also retain exactly
the fingerprinted Main/Worker/Test DLLs and the two dirty C# inputs (one inherited
B25A class, one new A2 class). Three source-origin and three genuine Worker
witnesses plus thirteen compiler-case JSONs are fresh and tied to those tests.

Historical evidence verification is read-only: 221 exact fingerprint checks,
with old B25A Broad still red. Two early verifier-development invocations found
an incorrect provenance mapping for B25's older gate/register fingerprints;
the completed verifier checks their exact historical export/committed blob,
not later B25A WIP. No historical file was changed to satisfy it. Final A2
evidence verification checks the complete test/build/artifact/protection set
and passes: Passed=true, ValidationPassed=true, EvidenceConsistent=true, no gate
failures. Its actual child-process exit code is 0. The first outer PowerShell
all-stream redirection returned 1 because it reclassified Git's existing root
ignore CRLF warning as NativeCommandError; the already completed audit and log
are preserved. A direct Process capture proves the verifier exits 0 and retains
the warning separately. No warning/error was hidden and `.gitignore` was not
changed. The reproducible capture is `Run-EvidenceGate.ps1`; its process record
binds the exact final audit hash. These evidence-logging diagnostics did not
rerun any test or change any Analyzer/test semantics.

C# whitespace verification, PowerShell parsing, JSON parsing, UTF8/no-BOM/CRLF,
Git whitespace, case-collision inventory and exact-case tracked project-reference
checks pass. No actual Linux execution is claimed. Existing architecture and
runtime isolation assertions are included unchanged in Boundary and Dual.

`SolutionTransitive` final Finding/CLI admission remains closed. B25B is not
ready. The next independent work is an explicit generic callable-instantiation/
substitution contract plus runtime-equivalent catch identity, before exact
shared normalization and any later Finding activation. Existing bounded
Direct/ProjectTransitive/Declared contracts are regression targets, not expanded.

Self Analysis was not rerun: inherited count 16 only. Standalone Canonical
Finding Diff was not rerun: inherited 0 added / 0 removed / 0 changed only.
The separately fresh eight-Current-case comparison is not relabelled as either.

## Final repository state

HEAD remains `4d90ce25ae143f90e968afdc8e5b6f5a7a07d82f`; no external movement.
The semantic index is empty (`diff --cached --quiet` succeeds; cached names are
empty). The physical index hash at A2 start is
`6B1B533D60B826E7F3DEAF98C2C3BA894BAD6C35AD57B03828332FB05B5CB847`,
different from the old B25A snapshot
`4D34F17D8D1FAF58C7BC07834BB6E410D2D9A9CBA68A3FB8E3E30B010EA71EF9`.
The final audit records the end hash without enforcing a physical reset or
repair. Root ignore, all 23 actual Core files and six stash entries are checked
against the A2 starting snapshot, not an assumed inventory.

Final `git status --short` (inherited B25A rows remain inherited WIP):

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B25A-assembly-qualified-type-ownership-audit.json
?? Evaluation/P5O2B25A-assembly-qualified-type-ownership.md
?? Evaluation/P5O2B25A2-canonical-exception-type-identity-audit.json
?? Evaluation/P5O2B25A2-canonical-exception-type-identity.md
?? Evaluation/P5O2B25A2Proof/
?? Evaluation/P5O2B25AProof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs
```

No commit, push, stash operation, index repair, protected-WIP rewrite or B25B
activation. Outcome: **BoundaryAudit_StopRule; OwnershipFixed=false;
ReadyForB25B=false**, regardless of green regression/evidence gates.
