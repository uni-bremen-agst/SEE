# P5O2B20 — Historical Declared production boundary

## Pre-change production data-flow audit

Starting HEAD: `6e080166f01e57a862fe2d1b1386e2355fa3e0e6` on
`fix/documentation`. Only the protected root `.gitignore` was dirty.
The recorded protection census is 922 tracked tool files, 23 physical Core
files and six stashes. Builds use a clean HEAD archive, not the protected tree.

1. **Entry.** `XmlDocExceptionSemanticDetector.FindExceptionSmells` skips a
   member without an attached documentation comment. Otherwise it collects
   compiler-bound tags and independently computes Direct evidence, then calls
   `AnalyzeConfiguredExceptionFlow`. `ToolRunner` also avoids allocating its
   shared summary session when Declared has no local exception definitions.
2. **Guard inputs.** The mode and
   `ProjectClosureSemanticContext.HasDeclaredExceptionTypesInReportingScope()`:
   the complete inventory of reporting-role source types inheriting
   `System.Exception`, including unused definitions. This is NOT a check for
   local exceptions observed in the result, documentation, or Direct evidence.
   Direct/Summary/documentation/uncertainty affect later decisions, not this
   inventory predicate. Root documentation presence is the earlier entry guard.
3. **Guard output.** Direct returns Direct. Declared without any local
   definition returns Direct without executing Summary. Otherwise the existing
   session runs the native Current summary analyzer through the B9/B8 dispatch.
4. **Relevance handoff.** Direct types are unrestricted. Declared Summary and
   documented types are relevant only when declared in the reporting scope.
   Direct types are excluded from DOC611. Evidence class matters: proven throws
   are not interchangeable with supplementary external documentation evidence.
5. **Final Findings.** Exact symbol equality provides coverage, not base-type
   inheritance. Invalid/unresolved cref yields DOC660; a bound non-Exception
   named type yields DOC670; a bound other symbol yields neither. Uncertainty
   selects DOC631 and suppresses DOC632. With complete flow, relevant unthrown
   documentation yields DOC632. Missing Direct/Summary documentation yields
   DOC610/DOC611 unless top-level `inheritdoc` suppresses those missing tags.
   `FindingFactory` supplies the existing smell/message plus original member
   or cref location, context, target name and snippet.
6. **B19 inputs.** B17 declaration inventory/ownership and independent Direct,
   canonical Summary, B19 ordered bindings/type displays/inheritance facts,
   documentation/`inheritdoc` flags and immutable original Main anchors cover
   the neutral decision inputs for its bounded selected member. Actual guard
   execution and final Finding generation are not already implemented by B19.
   Production integration must also check validation, not merely DTO shape.
7. **Unsupported barriers.** `HistoricalWorkerPayloadProjection.Project`
   permits the opt-in facts request only with ProjectTransitive;
   `HistoricalWorkerScopeProjection.TryProject` omits Declared;
   `WorkerInputValidation.HasSupportedScope` and
   `WorkerDeclaredPolicyContract.HasSupportedRequest` reject its engine mode.
   None can be removed before production parity and negative-case admission
   are established. The old evidence-only entry must not become a mode alias.

## Integration admission investigation

Before exposing these facts to the final renderer, B20 checks whether a
coherently modified response can obtain the same validation-owned receipt as
genuine compiler binding facts. This is distinct from B18's absent fields:
B19 now supplies the fields; the question is the strength of their admission.
No production activation or second renderer is introduced for this audit.

## New independent admission gap — reproduced

Three differential witnesses retain the **original** prepared B19 payload and
its original source/context receipt. They first execute the genuine isolated
Worker twice and verify native Direct/Summary and complete document repeat
parity. They then modify only a response's binding association:

- `System.ArgumentException`: `NamedType` becomes `Unresolved`, removing its
  type/inheritance fields. Current has resolved this tag and suppressed DOC610;
  these admitted facts instead describe an invalid cref and no exact coverage.
- The same tag becomes `OtherSymbol`. This describes a different Current
  decision again, without violating the response's structural consistency.
- A genuine `System.String` tag is redirected to the real, independently proven
  Direct `System.ArgumentException` identity. Its inheritance flag and display
  are correct for that substituted identity. The now-unused String display is
  removed. Current emits DOC670 and DOC610 for the actual String binding; the
  admitted association instead describes an Exception tag covering Direct.

All source/cref spans, raw cref, method/owner, declaration inventory, canonical
Direct/Summary, their hashes and provenance remain unchanged. The substitutions
are accepted by `HistoricalWorkerResponseValidation.Validate`. It mints the
same CWT-owned documentation receipt even without a transport-owned successful
execution (`ProcessId` absent). `DeclaredFindingDocumentationContract.Read`
accepts that original newly minted call; cloning it is correctly rejected.
Repeated admission yields the same complete immutable facts.

Root cause: `HistoricalWorkerDeclaredDocumentationProjection.TryValidate`
checks original syntax, valid enums, real PE identity/inheritance/display and
Direct/Summary consistency, but does not attest the association between the
original cref and that compiler result. `Unresolved`/`OtherSymbol` need no type
proof. A named type's PE identity proves that the type exists, not that this
cref bound to it. The response validator owns the only consumer receipt; a
receipt means structurally validated response, not exclusively genuine
execution-bound binding evidence. Self-reported flow hashes do not bind the
documentation association to an actual Worker execution.

This is **not** a claim that input source is executed, that the actual Worker
misbinds these crefs, or that an external attacker can mutate the present IPC
channel. The real Worker returns the correct facts. The demonstrated gap is
specifically the B20 requirement to reject manipulated evidence before
productive Finding admission. JSON re-admission and genuine execution are
currently indistinguishable to that Main consumer.

### Stop-rule decision

Do not expose this receipt to the final production renderer. Keep all existing
unsupported checks, not a partial activation, alias, heuristic or Current
fallback. No production sources, routing or Roslyn versions are changed.
The new tests are explicit **gap witnesses**, not claims that the coherently
modified evidence is fail closed. The unsupported production entry still
rejects every witness before dispatch and therefore cannot produce a false
Historical Declared Finding.

### Precise follow-up proposal

**P5O2B21 — Execution-bound Documentation Binding Admission**, before returning
to B20's guard/renderer integration:

1. Separate a structurally parsed response from execution-attested productive
   evidence using the existing Main-owned Worker client/validation boundary.
2. Only a completed genuine selected Worker execution over the original
   prepared request and exact received response may mint the productive
   documentation receipt. Caller-supplied JSON, a claimed PID/identity/hash,
   cloned calls/payloads and altered associations must not mint it.
3. Retain the immutable snapshot, exact original PE/source/owner checks and all
   current negative validation; do not rebind Historical source with Current,
   resolve crefs by string heuristics, duplicate policy in Worker or introduce
   cryptographic claims merely from self-reported hashes.
4. Turn these three gap witnesses into rejection tests at that final admission
   boundary while retaining genuine v1 B19 binding/flow/ownership parity.
5. Only then integrate the common Main guard and final Finding decision, with
   full A–F production parity and explicit pre-summary execution planning.

The follow-up design must state its trusted execution boundary. An execution
receipt is not a proof against a compromised Worker binary and must not be
presented as one. No such new mechanism is partially implemented in B20.

## A–F coverage and exact comparison status

| Requested case | Genuine Current production observation | Historical observation / limit |
| --- | --- | --- |
| A local | LocalException produces DOC611; Declared graph has two nodes | Complete B19 flows/Direct/bindings/declarations match; no final renderer activated |
| B external | Framework-only helper: Project has DOC611, Declared has none and graph remains empty | Genuine Project evidence matches; productive Declared remains rejected |
| C guard | No local type + GC.Collect: Declared returns Direct, no Finding and zero graph nodes | Unguarded evidence-only Project reports AnalysisFailure; no fallback or partial result |
| D mixed | Direct ArgumentException DOC610 + local Summary DOC611; external Summary not relevant | Genuine complete B19 inputs and repeated serialization match; the admission gap blocks final Finding adoption |
| E chain | Root → A → B: local DOC611, actual three-node productive graph | Complete canonical flows/ownership/Direct/B19 context match on two genuine executions |
| F same input | Local, mixed, chain and unused cases have different **full** Project/Declared Finding arrays | Same B19 evidence semantics established, but no Historical final Finding difference claimed |

The extra unused-declaration case produces DOC632 despite no locally thrown
exception. The no-doc case has zero Findings/zero graph in all four native
modes. These are actual `FindExceptionSmells` invocations, not the diagnostic
Summary analysis that B17's snapshot runs before the productive guard.

Every new production-control case records all four native modes, every final
Finding field, targets/type names/contexts/snippets, actual graph count and
canonical graph summaries, plus the independent native Direct/Summary and
ownership inventory. Every genuine Historical request is repeated, records
full results/source paths/provenance/identity and explicitly checks that no
native Current result is returned. Existing B17/B14 differential tests retain
their unchanged Historical graph/semantic-ownership comparisons.

No Historical final Finding array exists in this B20 outcome. Therefore
**full Current/Historical production Finding parity is NOT established**.
Contract/flow parity is not substituted for that acceptance criterion.

## Negative-case accounting

The unchanged B19 22 negative response cases check missing document/bindings,
omitted/foreign root and member/cref context, changed inheritdoc/documentation,
wrong/default/missing modes and profiles, conflicting type/assembly identity,
classification/display, missing/duplicate type information, unknown binding
kind, and missing/conflicting Direct/Summary hashes/results. B17's unchanged
16 response negatives check missing/incomplete declarations, inventory/count,
reporting/declaring ownership, type/assembly/module, Direct paths/evidence,
and mode/profile. Original-object receipts reject clones and raw legacy calls.
Existing admission tests reject caller-invented facts, copied scopes, generic
and solution opt-ins and propagate genuine incomplete-analysis errors.

Those established negatives remain fail closed, but they do **not** cover the
three coherent association substitutions above. B20 honestly records accepted
evidence admission, NOT successful fail-closed evidence validation for those
three cases. Productive Historical Declared still rejects before dispatch;
there is no Direct/Project/Solution/Current downgrade. The source-authoritative
Current guard/renderer is untouched.

## Repository and evidence scope

Changed files are only the new admission/production-guard test class, its
inclusion in `Verify-DualVersionBuild.ps1`, this report, the B20 audit JSON and
two B20 Proof scripts. There is **no production-source change**. All B5–B19
tests, 121 shared analyzer sources, Historical host, Worker, Main detector,
ToolRunner, selection/routing, contracts and Roslyn package versions are
byte-unchanged from starting HEAD.

`OPEN-PIPELINE-BOUNDARIES.md` is deliberately unchanged: no support, productive
guard or final Finding boundary has actually been closed. B20 adds evidence
for a more precise blocking admission requirement, not a resolved pipeline
status. General Historical reporting import, CLI/acquisition/adoption,
solution loading, signed/other unsupported reference forms, additional
compiler versions and Dapper work remain outside this package.

Both initial long-path export failures and the initial new-test/format failures
are preserved. The first isolated export made the MSBuild cache path **261
characters**; the unchanged repeat failed during reference resolution. A new
shorter clean archive of the same HEAD passed without changing product/build
settings. Initial B20 tests were 9/10: a comparison of two failure records used
array reference equality. Comparing their complete JSON fixes the test, not
the product. Whitespace verification also identified only new evidence-object
initializer formatting; the owned-file formatter fixed it. No exclusions,
skips, opportunistic native-reader fix or automatic retry is used.

The first full-suite run completed **3,107/3,108**, zero skips, with the known
unchanged `ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed`
assertion failure at line 446 (`AssemblyFile.ContainsMetadata` false). Its
`ReadAssemblyFiles` helper (lines 663–675) returns reader-backed `AssemblyFile`
structs after disposing its `PEReader`; the caller subsequently reads that
disposed backing state. This is the already documented lifetime flake, here a
managed assertion failure rather than the earlier native access violation.
The initial snapshot, workspace and built copy all have SHA256
`4D959975EEFF758C31517E1D2C0C0EAAA2C805E5E9621B7E581FBB2A7A8420BC`.
Broad was 3,017/3,017 before it. The entire failed log/TRX is preserved at
`artifacts/p5o2b20/attempts/full-first`; no crash or forced cleanup occurred.
After inspecting and documenting this unchanged source, the complete suite was
explicitly repeated without rebuilding, filtering, exclusions or source fixes:
**3,108/3,108, zero failures and zero skips** (`full-repeat.trx`).
The failed first full run remains part of the final evidence.

## Closure checklist (the requested 23 points)

1. **B19 → production:** B19 supplies complete bounded neutral Finding inputs;
   B20 found a further admission-strength gap before a productive renderer may
   consume them. This is not the old absence of documentation fields.
2. **Summary guard:** The seven-step pre-change audit above records the actual
   no-doc and complete-local-inventory guards. Seven new real production
   snapshots prove the zero/populated graph decisions without a parallel guard.
3. **Finding flow:** Existing DOC660/670/631/632/610/611 decisions, exact
   coverage, relevance and inheritdoc suppression remain solely in Main.
4. **Production integration:** None; the stop rule prevents unsafe partial
   adoption. Only tests, gate inclusion and audit/proof files change.
5. **B19 data used:** Original prepared request/receipt, source/member/cref
   syntax, binding kind/type/inheritance/display, immutable anchors/context,
   B17 declarations/ownership, independent Direct, full canonical Summary,
   hashes, source paths and compiler/provenance identity.
6. **Unsupported barrier:** All three Main/Worker admission barriers remain.
   Coherent false binding associations can currently obtain an evidence receipt;
   the productive mode rejects before dispatch, not by falling back.
7. **Finding parity:** NOT established. Full actual native Findings and genuine
   B19 input parity are recorded; no fake Historical Finding renderer or
   count-only success claim is used.
8. **Project difference:** The same local/mixed/chain/unused inputs give the
   complete different Current arrays shown above. Historical final Finding
   mode-difference proof remains open, not renamed Project execution.
9. **Direct:** Unchanged; each new native control has zero Summary graph nodes.
   Existing genuine Historical Direct and eight Current baselines are retained.
10. **Project:** Unchanged genuine labelled evidence execution and all B14
    scope/Direct/flow/ownership controls remain in the regression set.
11. **Solution:** Unchanged native controls, B15 exact DAG/cross-project and
    supporting-source/ownership/error regressions remain included.
12. **Negatives:** All prior B17/B19 response/admission/clone/error checks are
    unchanged. The three new accepted coherent substitutions are a documented
    failure of evidence admission strength, not misreported as rejected.
13. **Supported Declared scope:** No Historical productive Declared scope has
    been activated. The existing bounded v1 B19 evidence-only project/root
    contract remains available through genuinely labelled Project requests.
14. **Unsupported Declared scope:** All productive Historical Declared requests,
    including this proposed root scope, remain rejected. Cross-project,
    other members/profiles/versions and general loading are not admitted.
15. **Isolation/cleanup:** Real pinned Historical Worker processes execute twice
    per new witness/control. Main loads no Historical Analyzer/Worker assembly;
    no input code is executed. Every child is checked stopped; final verifier
    checks no validation-owned dotnet/testhost remains. Actual execution is
    Windows; Git path-case checking is not represented as a Linux runner test.
16. **Files:** Six owned files: new test, gate, Markdown, JSON and two Proof
    scripts. Root ignore, 23 Core files, six stashes and the empty index remain
    protected. No existing B5–B19 test or production file changes.
17. **Tests/builds:** See the fresh validation table below and exact TRX/hash
    evidence. Initial failures are separately retained; final successful
    results never overwrite their accounting.
18. **Gates:** Extended permanent Boundary gate, exact dual build/runtime
    isolation, all eleven WAE steps, owned format, JSON, strict UTF8/no-BOM/CRLF,
    PS syntax, source hashes, archive/overlay and evidence/protection verifier.
19. **Current/binaries:** All eight Current JSON baselines and all six scheduled
    Main/Historical DLL/PDB comparisons plus both Worker DLL/PDB comparisons
    are byte-identical to the fresh same-HEAD parent run.
20. **Self analysis/diff:** 16 Findings and canonical 0 added / 0 removed / 0
    changed-evidence are **inherited B19 results**, not fresh B20 executions.
21. **Remaining boundaries:** Execution-bound binding admission first; then the
    common Main pre-Summary guard and final Finding integration/full A–F parity.
    General reporting/CLI/loading/reference/compiler-version work remains open.
22. **Git status:** Recorded verbatim below and in the machine audit. No commit,
    push, staging or stash operation.
23. **External HEAD movement:** Starting/final HEAD is
    `6e080166f01e57a862fe2d1b1386e2355fa3e0e6`; verifier requires the same branch,
    HEAD, index and stash hashes. No external HEAD movement observed.

## Fresh validation results

| Group | Passed / total |
| --- | --- |
| B20 | 10 / 10 |
| B19 (unchanged) | 39 / 39 |
| B18 (unchanged) | 7 / 7 |
| B17 (unchanged) | 25 / 25 |
| B16 (unchanged) | 18 / 18 |
| B15 / B14 / B13 | 28 / 28; 31 / 31; 34 / 34 |
| B12 / B11 / B10 | 29 / 29; 16 / 16; 34 / 34 |
| B5–B9 | 156 / 156 |
| Productive | 681 / 681 |
| Focused | 716 / 716 |
| Broad | 3,017 / 3,017 |
| Full configured suite | 3,108 / 3,108 on explicit unchanged repeat; first 3,107 / 3,108 retained |
| Permanent Boundary | 506 / 506 (fresh parent: 496 / 496) |

All final successful groups: zero failures and zero skipped tests. Dual-version,
runtime compiler identity/isolation, all eleven WAE builds (zero warnings /
zero errors), eight Current baselines and production DLL/PDB comparisons PASS.
Full-suite attempts are reported separately above. Evidence verifier, format,
JSON, strict UTF8/no-BOM/CRLF, PS parsing, architecture/source preservation,
isolation/cleanup and repository protection: **PASS**. No case collisions in
40,818 tracked repository paths; no Linux runner execution is claimed.

## Final `git status --short`

```text
 M ../../.gitignore
 M build/Verify-DualVersionBuild.ps1
?? Evaluation/P5O2B20-historical-declared-production-boundary-audit.json
?? Evaluation/P5O2B20-historical-declared-production-boundary.md
?? Evaluation/P5O2B20Proof/
?? Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs
```

The `.gitignore` entry is exclusively the pre-existing protected WIP and has
the original SHA256 `7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5`.
The other entries are this bounded B20 audit. No Core/build-churn entry appears.
Index empty; six stash identities and all 23 Core hashes unchanged. No commit
or push. HEAD remains `6e080166f01e57a862fe2d1b1386e2355fa3e0e6`.

Proof entry points: `P5O2B20Proof/Run-Regressions.ps1` and
`P5O2B20Proof/Write-Verify-ProductionBoundary.ps1` with the recorded
`-ValidationRoot` from `artifacts/p5o2b20/export.json`. Machine evidence is
`P5O2B20-historical-declared-production-boundary-audit.json`; original
failure artifacts, successful TRXs, parent comparisons and protection snapshots
remain in the recorded isolated export/artifacts directories.
