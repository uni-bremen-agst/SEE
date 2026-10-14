# P5O2B21 — Historical documentation binding attestation

## Phase 1: pre-change trust-chain audit

Starting HEAD: `220ea85f2d9c9550f26e7c096417815736e9e1cc`, branch
`fix/documentation`; B20 is committed. Only protected root `.gitignore` WIP
exists. No repository instructions were found. Builds use a fresh HEAD archive.

1. **Actual binding origin.** `HistoricalDeclaredDocumentationFacts.Create`
   receives the original Historical `SemanticModel` and selected source-owned
   `MethodDeclarationSyntax` from `HistoricalAnalysis.Analyze` after the real
   analysis. It enumerates the existing ordered full exception tags.
2. **Semantic determinants.** The actual `XmlCrefAttributeSyntax.Cref` is bound
   with `model.GetSymbolInfo(cref.Cref).Symbol`, not candidate symbols or display
   string matching. The named-type symbol supplies canonical assembly/module/
   type identity, `InheritsFromOrEquals(System.Exception)` and display name.
   Null or other-symbol binding is explicitly distinguished. The declared root
   symbol and original document/member/tag spans determine target ownership.
3. **Neutral returned facts.** Existing B19 profile/mode, canonical callable,
   ordered tag syntax/kind/type/inheritance, displays, document/inheritdoc flags,
   Direct/Summary hashes; existing B17 declarations/ownership/Direct and canonical
   Summary plus input and actual loaded-runtime provenance accompany them.
4. **Old receipt mint.** `HistoricalWorkerResponseValidation.Validate` parses
   any caller-supplied stdout and, after structural/original-source/PE checks,
   inserts an immutable `DeclaredPolicyFacts` snapshot into its private
   original-call CWT. `DeclaredFindingDocumentationContract.Read` trusts it.
5. **Old receipt inputs.** Supplied JSON, exit code, diagnostics/PID, original
   projected payload object, syntax origin and retained PE symbol world.
   Source spans and flow hashes bind inputs; they do not prove the provenance
   of a cref-to-symbol association in that JSON.
6. **Main-controlled inputs.** Main controls validated request construction,
   fake DTO/JSON construction and the `Validate` arguments, including claimed
   PID/exit/identity. It can construct a structurally coherent false association
   without changing the original source, Direct or Summary.
7. **Exclusively executed evidence.** The real client owns an actual process,
   sends a frozen request through its private stdin and receives that child's
   stdout/stderr through private pipes after actual exit. The trusted configured
   Worker host's existing emitter derives its binding fields from Roslyn. These
   transport-origin facts were not a prerequisite for the old CWT mint.
8. **Exact bypass.** B20 turns a covered ArgumentException tag into Unresolved
   or OtherSymbol (removing type/flag), or redirects a String tag to the real
   Direct ArgumentException type/true flag, removing only its unused String
   display. Enum/type/display consistency remains valid. PE existence proves a
   type exists, not that this source cref bound to it. Each submitted modified
   response could therefore mint the old original-call receipt.
9. **Minimum additional evidence.** A fresh opaque, one-use capability branded
   only inside the completed real Worker-client exchange, retaining exact sent
   request, original payload object, received response/diagnostics, actual exit/
   PID and the metadata/hash identity of the configured executed Worker image.
   It is not a hash of a caller DTO. Typed validation consumes that capability
   once and compares the exact full exchange before admitting B19 facts.
10. **Trust boundary.** The existing trusted Main client, OS-private process
    pipes, configured trusted Worker image and reviewed binding emitter. Raw
    parsing/JSON re-admission is outside this boundary; a structural receipt
    cannot substitute for completed-execution authority. This is not remote
    cryptographic attestation or protection against compromised trusted code,
    reflection into private state, OS/runtime or Worker binary replacement.

## Chosen minimal correction

Reuse the project's private CWT/original-reference pattern at the execution
owner. An unregistered opaque object has no authority. Exact response equality
binds **all** existing worker-derived binding components and their root/source/
ownership context to the actual received message. Single-use consumption
prevents re-admission/replay; immutable Main facts remain the consumer boundary.
Verify reported Worker image name/version/MVID/hash against the configured
image read as metadata, without loading Historical assemblies into Main.

No new Worker DTO, digest, wire nonce, protocol version, binding resolver or
security service is necessary: B19 already emits the correct canonical facts.
Main does not reproduce Historical semantic binding or trust a self-reported
second checksum. Canonical B19 serialization remains deterministic; the
nonserializable execution capability is intentionally different per exchange.

B20's three exact manipulation fixtures must become rejection assertions;
their payload/JSON transformations remain unchanged. B19–B5 tests remain
byte-unchanged. Productive Declared remains unsupported; known guard/final
Finding integration is reserved for B22, not implemented here.

## Implemented attestation and exact admission

`HistoricalWorkerClient` brands a fresh `DocumentationExecution` object in a
private CWT only after its actual stdin exchange, process exit and bounded
stdout/stderr EOF. Merely constructing that opaque object gives no authority.
The registered completed exchange contains the original payload reference,
exact frozen serialized request, exact received response/diagnostics, actual
exit/PID, and copied physical Worker name/version/MVID/SHA256. The configured
image is read before launch and checked again after completion. PE metadata is
read without loading an assembly; no reader-backed value escapes its lifetime.

`HistoricalWorkerResponseValidation.Validate` consumes the registered authority
exactly once. It requires reference-equal input, unchanged sent request and
ordinal equality of received response/diagnostics, exit and PID. It then runs
**all existing** protocol/runtime/flow/ownership/source/PE/B19 structural checks.
If documentation facts exist, actual-execution authority and the reported
Worker's exact physical image identity are mandatory before the CWT consumer
receipt is inserted. Missing/unregistered/consumed authority fails closed;
claimed PID, known real PE type, self-consistent JSON and a copied identity do
not mint a receipt. The client removes authority even if parsing/validation
fails. No callback, test-only mint or public registration method was added.

`DeclaredPolicyFacts` retains only the consumed opaque capability alongside
the existing immutable facts, not the large exchange or a mutable transport
DTO. Replaying that consumed object cannot obtain a second import receipt.
The existing original-call CWT prevents receipt transfer by cloning, and the
immutable document snapshot resists later mutation of raw DTO arrays.

The canonical binding facts still originate exclusively in the unchanged
Worker's actual `GetSymbolInfo`/type/root projector. The new proof is their
**execution-channel association**, not a second canonical string derived from
an attacker-controlled DTO. SHA256 here identifies the executed file, not a
new semantic-proof digest. The existing validated P4/P5 source/PE origin and
B19 syntax/root/context checks remain mandatory; attestation bypasses none.

The trusted configured image/runtime/OS/client are assumed uncompromised,
as in the existing isolated execution model. A malicious replacement of the
trusted host/runtime or reflection rewriting private CWT state is outside that
model. This implementation does not claim arbitrary untrusted-process semantic
proof, source recompilation in Main, remote signing, or cryptographic proof
against a compromised Worker.

## Original B20 fixtures — now rejected

| Unchanged transformation | Why B20 accepted it | B21 rejection |
| --- | --- | --- |
| ArgumentException → Unresolved; remove type/flag | Valid enum and internally consistent absence, original spans/flows intact | Re-submitted JSON has no exact completed-execution authority; no document receipt |
| ArgumentException → OtherSymbol; remove type/flag | Same structural consistency, original source cref retained | Same mandatory actual-execution association; no document receipt |
| String → real Direct ArgumentException/true; remove unused String display | PE existence/inheritance and display checks were all true for the substituted type | A coherent different binding cannot borrow the genuine execution receipt |

Only the three old gap-witness **expectations** and evidence flags changed to
rejection; their attack payloads, JSON transformations, original payload/source
receipt, unchanged Direct/Summary hashes and native Finding observations remain.
All seven B20 production/guard controls remain unchanged.

## New tests and negative boundaries

- Eight positive shapes run the real Worker twice: exact, alias, base type,
  invalid/unresolved/non-Exception tags, other-symbol method cref, duplicate,
  empty tag and no-doc. Full canonical document/Direct/Summary/declaration/
  source/provenance/context parity is compared, not just counts. Each execution
  gets fresh authority; wire facts repeat identically. Correct B19 acceptance,
  consumed-receipt rejection, clone rejection and immutable snapshot are proved.
- Seventeen negative cases reuse real emitted/bound input and a real successful
  Worker response: missing/unregistered/replayed authority, single type-component
  change, coherent real local-type substitution, member, source, ownership,
  default kind, missing type/bindings, unknown protocol/wire state, cross-binding,
  another actual bound target, different source bytes and cloned input. No result
  or document/policy receipt is returned; no Current fallback exists.
- A separate **actual process** uses the existing test-only B4 pipe peer and
  outputs a copied genuine complete Worker response. Its file is even named
  `XMLDocNormalizer.HistoricalWorker.dll`. Runtime claim, source/flow hashes and
  document payload remain correct, but its real assembly metadata/hash differs:
  the completed pipe exchange cannot attest a different Worker image. It is
  rejected with no receipt and checked stopped. This is not just a hand-built
  DTO test. The B4 peer's old scenarios remain unchanged.

Missing/conflicting/malformed B19 evidence continues to fail at existing
validation before the new final admission check. Ordinary Historical requests
without B19 documentation opt-in retain the previous validation path. Source/
target associations are exact original objects and bytes, not path-only or
fuzzy equality. Receipt replay is one-use at import; consumers may read the
already admitted immutable facts repeatedly, as intended.

## Closure report — requested 23 points

1. **B20 trust gap:** structurally consistent false cref association could be
   re-admitted from arbitrary JSON into the original-call CWT.
2. **Three attacks:** all original transformations above are retained and now
   reject; genuine/native Finding observations remain available in evidence.
3. **Previous receipt:** structural/input/PE completeness snapshot, not exclusive
   proof of the real received binding association.
4. **Bypass cause:** original syntax and existing PE type identity did not prove
   the origin of the supplied association; `Validate` could mint it without
   completed execution.
5. **New semantics:** exact actual completed-exchange association plus physical
   Worker identity and one-use authority before document receipt mint.
6. **Trust boundary:** trusted real client/private pipes/configured Worker and
   reviewed binding emitter; limits stated above, not remote cryptography.
7. **Worker facts:** unchanged real semantic binding kind/type/inheritance,
   canonical root/source spans/displays and accompanying existing evidence.
8. **Main validation:** typed, exact, one-use, original-reference/request/response
   equality, no normalization, all old structural checks retained.
9. **Protocol:** outer v2 and B19 v1 DTO/bytes unchanged; no wire attestation,
   nonce, digest or general reporting field. Authority is Main-local only.
10. **Manipulation/replay:** original three B20 attacks, seventeen new negative
    cases and a real forged pipe peer; missing/claimed/default authority does
    not suffice.
11. **Positive real execution:** eight dual-run shapes, full native/canonical
    comparisons, fresh authority and deterministic serialized facts.
12. **B19 impact:** conceptually unchanged immutable finding inputs, now admitted
    exclusively through completed execution. All 39 B19 tests are untouched.
13. **Declared status:** still unsupported. No guard/final renderer/CLI adoption,
    mode alias or fallback is added. Activation is B22.
14. **Binaries:** Main DLL/PDB change for three Main trust files. Historical
    Analyzer DLL/PDB and Worker DLL/PDB remain byte-identical to the fresh
    same-HEAD parent. Test DLL/PDB and the test-only replay-peer image are rebuilt
    for new tests/peer behavior; not production Analyzer changes.
15. **Current parity:** all eight complete Current mode/bool baselines compared
    byte-for-byte; native detector/session/flow/source semantics unchanged.
16. **Historical modes:** Direct/Project/Solution, complete B14/B15 scope/DAG,
    B5–B19 routing/selection/reference/source/compiler controls unchanged.
17. **Isolation/cleanup:** no Historical assemblies or cross-version objects in
    Main; copied PE primitives only. Owned real and forged peers checked exited;
    no submitted target code executed. Final verifier checks no own live child.
18. **Tests/builds:** fresh TRX and eleven exact WAE build logs, not counts inferred
    from earlier packages. Final census below is completed only after actual runs.
19. **Gates:** permanent Boundary includes all B21 tests, old B20 attacks and
    unchanged B5–B19; source preservation, isolation, format/encoding/JSON/PS
    syntax, archive/overlay, baseline/binary and evidence/protection gates.
20. **Self/diff:** inherited B20 Self Analysis 16 and canonical 0/0/0; NOT rerun
    and not represented as fresh analysis of the modified Main source.
21. **Remaining limits:** B22 existing Main pre-Summary guard and final Finding
    integration/full A–F parity; all other reporting/CLI/loading/reference/
    compiler/Dapper limits unchanged.
22. **Git:** protected root ignore/23 Core files/six stashes/index preserved;
    final status is recorded verbatim in this report and audit. No commit/push.
23. **HEAD movement:** starting HEAD `220ea85f2d9c9550f26e7c096417815736e9e1cc`;
    repeated verifier checks reject unaccounted movement. Final HEAD is identical;
    no external movement occurred during B21.

Owned build inputs are three Main files, modified B20 expectations, new B21
tests, the narrow test-peer branch and Boundary filter. Additional owned files
are this report/audit, two B21 Proof scripts and the single truthful B21 entry
in `OPEN-PIPELINE-BOUNDARIES.md`. Worker/Protocol/shared Analyzer/Current detector/
ToolRunner/routing/version files and all B5–B19 test files are untouched.

## Validation attempts

Fresh parent: 506/506 and all eleven WAE steps 0 warnings/0 errors.
First Main/test WAE build: PASS; first combined B21+B20 run: **36/36**, no skips.
Final permanent gate: **532/532**, all eleven WAE steps 0 warnings/0 errors.

The initial owned-file whitespace verification found new anonymous-object
initializer formatting only; formatter changes and initial log are retained.
An initial mechanically adapted evidence-verifier draft had a CRLF-sensitive
rewrite truncation, caught by parser checking **before execution**. Its source
was retained; the full read B20 verifier was adapted with explicit newline
normalization and checked anchors, then parser-verified. No production/test
semantics were changed for that draft issue. Regression outcomes and any known
reader-lifetime flake/repeat are reported separately when completed.

## Fresh final validation census

| Group | Passed / total |
| --- | --- |
| B21 | 26 / 26 |
| B20, original three attacks now rejected | 10 / 10 |
| B19 / B18 / B17, byte-unchanged tests | 39 / 39; 7 / 7; 25 / 25 |
| B16 / B15 / B14 | 18 / 18; 28 / 28; 31 / 31 |
| B13 / B12 / B11 / B10 | 34 / 34; 29 / 29; 16 / 16; 34 / 34 |
| B5–B9 | 156 / 156 |
| Productive | 681 / 681 |
| Focused | 742 / 742 |
| Broad | 3,043 / 3,043 |
| Full configured suite, first run | 3,134 / 3,134 |
| Permanent Boundary | 532 / 532; fresh parent 506 / 506 |

All test/build attempts passed: zero test failures/skips, zero build warnings/
errors. No reader-lifetime flake or test retry occurred in B21; the unchanged
flaky helper remains untouched. Initial whitespace and verifier-draft parser
issues are preserved separately above, not counted as passed validation.

All eleven WAE steps and full dual-version/runtime isolation PASS. Eight
Current baselines are byte-identical. All three scheduled Historical Analyzer
DLL/PDB pairs and both Worker DLL/PDB pairs are byte-identical to the fresh
parent; all three Main DLL/PDB pairs change consistently for the trust fix.
No shared analyzer, Worker-host, Protocol, routing or Roslyn-version source
changed. Actual execution is Windows; Git case checking is not a Linux run.
Final evidence/protection verifier, architecture/isolation, JSON, strict
UTF8/no-BOM/CRLF, PS syntax and owned C# formatting: **PASS**. The verifier was
run against all final actual TRXs/artifacts and source/binary/baseline hashes.
Git case collisions: zero across 40,823 tracked repository paths. No Linux
runner execution is claimed. No own live Worker/testhost remains.

## Final `git status --short`

```text
 M ../../.gitignore
 M Evaluation/OPEN-PIPELINE-BOUNDARIES.md
 M Evaluation/P5O2B4Proof/TestProcess/Program.cs
 M Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs
 M build/Verify-DualVersionBuild.ps1
 M src/XMLDocNormalizer/Execution/Analysis/DeclaredExceptionFindingPolicy.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs
 M src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs
?? Evaluation/P5O2B21-historical-documentation-binding-attestation-audit.json
?? Evaluation/P5O2B21-historical-documentation-binding-attestation.md
?? Evaluation/P5O2B21Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs
```

The `.gitignore` entry is exclusively the original protected WIP, SHA256
`7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5`.
All other entries belong to B21. All 23 Core files and six stash identities
are unchanged; index remains empty. No commit/push. Starting/final HEAD:
`220ea85f2d9c9550f26e7c096417815736e9e1cc`.

Proof entry points: `P5O2B21Proof/Run-Regressions.ps1` and
`P5O2B21Proof/Write-Verify-BindingAttestation.ps1`, both with the recorded
`-ValidationRoot` from `artifacts/p5o2b21/export.json`. Machine evidence:
`P5O2B21-historical-documentation-binding-attestation-audit.json`. The isolated
export retains original source archive, successful parent/final gate logs,
first 36/36 TRX, all regression TRXs, attack/positive/replay JSON, protection
snapshots and the initial format/verifier-draft artifacts. No failures hidden.
