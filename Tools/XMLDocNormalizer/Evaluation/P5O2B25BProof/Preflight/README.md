# B25B – vollständiger Produktions-/Receipt-/Characterization-Preflight

Status: **Preflight abgeschlossen; einmaliger STOP für den konsolidierten
Characterization-Batch. Keine Produktions-/Testimplementierung begonnen.**

HEAD `1ef19a37bbecc2ba1f9f2b9e2be27f7cc092c65e`, Parent
`a2fae5fc29f682461b38385990568fc5da494ae7`. Der neue HEAD committtet den
Readiness-Review, nicht neue Analyzer-/Testsemantik. Branch `fix/documentation`.
Anfangsstatus nur geschützter Root-`.gitignore`-WIP; Index leer; sechs Stashes.

Die Quelle des Auftrags ist der neue B25B-Implementierungsauftrag. Dessen
Abschnitt11 verlangt vor Änderungen bestehender Assertions genau einen
vollständigen Approval-STOP. Der frühere GO WITH EXPLICIT BOUNDS ist keine
pauschale Characterization-Freigabe.

## 1. Auditumfang und tatsächliche Arbeit dieses Laufs

- 347 bestehende Testdateien inventarisiert/gehasht; alle 34 Worker-Dateien
  im Census; direkt passende Produktions-/Source-/Receipt-Bindungen erfasst.
- B24/B25/Solution-/Historical-/Declared-/Attestation-/External-Target-Fälle
  anhand ihrer tatsächlichen Inputs, Aufrufer und Assertions rekonstruiert.
- 761 getrackte Source-/Test-/Build-Dateien und 735 getrackte historische
  Evaluation-Dateien geschützt; 23 physische Core-Dateien gegen die
  tatsächlichen Anfangsbytes dieses Laufs geschützt. Diese sind gegenüber dem
  Readiness-Start unverändert, keine neue Schutzbaseline oder Manifeständerung.
- Drei Änderungskategorien mit 18 vorhandenen Testfällen in zwei Dateien
  identifiziert; alle in EINEM [Approval-Batch](Characterization-Approval-Batch.md).
- Keine Test-/Source-/Worker-/Wire-/Routing-/Profileänderung, kein Testlauf,
  Build, Cache-/Artifact-Rekonstruktion, Commit, Push oder Stash-Eingriff.

Nachweise: [start.json](start.json),
[characterization-inventory.json](characterization-inventory.json),
[verification.json](verification.json). Der Census ist ein Source-/Call-/
Assertion-Preflight, kein behaupteter neuer Runtime-Replay aller Full-Fixtures.
Die erhaltene grüne A10-Baseline wird nicht als neuer B25B-Lauf ausgegeben.

## 2. Identitäten und vorhandene Träger

| Kennung | Identität / Bindung | Heute vorhandene Daten und Authority |
|---|---|---|
| R | Reporting Root | P5-gebundener `IMethodSymbol` und Original-Provenance; `CanonicalCallableIdentity` mit Assembly/Module/Containing/Signature; Worker TypeMetadataName/MethodName und RootLogicalPath |
| S | Reporting-/Project-Scope | Original `ExternalCSharpSyntaxTreeSet`, geordnete Source-Membership/Checksums/LogicalPaths, Parse-Kontext, Root-Source; `WorkerCompilationScope` / AdditionalSources |
| G | Gesamter Solution-Graph | Original Root-/Supporting-P4/P5/B12/B14-Handoffs; `WorkerSolutionGraph` mit Target, ProjectIdentity, ModuleName/MVID, Dependencies und kompletten Project Inputs |
| V | Exakte Referenz-/Typwelt | Validierte PE-Images, geordnete Aliases/Properties/Identities/Checksums/MVID; `WorkerProtocol.ReferenceWorldHash` und Result-Provenance; vollständige Canonical-Type-/Member-Identity |
| E | Reale Execution | B21s privater single-use `DocumentationExecution` / abgeschlossener Request-Response-Exchange, EOF, Exit, PID, tatsächliches Worker-Image vorher/nachher und validierte echte WorkerIdentity |
| Q | Exactness / Vollständigkeit | Native `ProfileOutcome` unter Session; Worker gibt bei OUTSIDE vor Canonical-Erfolg `UnsupportedContext` zurück; zusätzlich keine Uncertainties/Truncation und nur Proven-Entries bei Main-Validation |
| D | Dokument-/Reporting-Fakten | Originale Main Syntax/Anchors/Context/Snippet, Worker-cref-Bindungen, unabhängiges Direct-Ergebnis; `DeclaredFindingDocumentationFacts` / `HistoricalFindingFacts`, aus Original-Input registriert und realem Run validiert |

Ein kanonischer Type enthält bewusst keinen erfundenen PE-MVID. Physical PE-
MVID/Checksum bleiben in originaler Referenzwelt und Graph-Provenance gebunden;
Canonical Assembly/Module/Construction bleiben trotzdem vollständig. Niemals
MVID allein oder Displayname als Type-Identity-Ersatz verwenden.

## 3. Vollständiger tatsächlicher Pfad und minimale Einfügestellen

„Geplant“ bezeichnet ausschließlich die nach C1–C3-Freigabe erforderliche lokale
Integration. Es ist kein bereits implementierter positiver B25B-Pfad.

| Stufe / konkreter Typ und Methode | Relevanter Input → Output | Validation-/Trust-/Exactness-Grenze und Bindungen |
|---|---|---|
| 1. `ExternalTargetExecution.AnalyzeAsync`, `ExternalTargetJob` | Untrusted lokale PE/PDB/Source/Reference-Pfade + ein Root → Original-Handoffs | Heute Solution-Job sichtbar abgewiesen. Geplant: explizite Supporting-Materialien für zwei/drei Knoten; jede Datei durch dieselben P4/P5-Factories, kein Authority-Claim aus JSON. R/S/G/V nur nach Validierung |
| 2. `ExternalCompilationProvenanceDescriptorFactory`, `ValidatedExternalSourceMaterialFactory`, `ExternalCSharpSyntaxTreeFactory/SetFactory`, `ExternalMetadataReferenceSetFactory` | PE/PDB/Source/Referenzen → originale validierte Objekte | Vollständigkeit/Checksums/Parse/PE-Bindung erhalten; keine zweite Validator-Schicht, keine Acquisition. R/S/V und Originalität |
| 3. `HistoricalWorkerPayloadProjection.Project`, `HistoricalWorkerSolutionProjection.Project` | Root + original SupportingProjects → `HistoricalWorkerPayloadProjectionValue` / finaler `WorkerAnalysisInput` | Bestehende Flow-Projektion validiert Node-/Edge-/Target-/Source-World. Heute keine Finding-Adoption. Geplant: zusätzlicher MAIN-lokaler Finding-Shape-Guard genau zwei/drei Ketten, kein Branch/Link/Mehrroot. R/S/G/V |
| 4. `ExceptionFlowAnalyzerSelectionContext.Projector.Project` | Originales Root-PDB → trusted Compiler-Evidence | Kein freier Compilerstring, Auswahl keine Authority über Source-Äquivalenz; tatsächliche Provenance muss passen. R/V |
| 5. `ExceptionFlowAnalyzerSelectionPolicy.Select` | Trusted Evidence → exakte Current/Historical-Selection | Exakte Build-Identitäten, kein Force/Fallback. Ein Historical-Job, der Current wählt, wird nicht mit anderer Engine ausgeführt |
| 6. `ExceptionFlowAnalysisDispatch.AnalyzeAsync` / Router `AnalyzeAsync` | Vorbereitetes Worker-Payload → tatsächlicher Call | Reihenfolge Projection→Selection→Route bleibt. Geplant keine neue Route/Engine. Originaler Request, keine Quelle neu kompilieren im Main. R/S/G/V und angeforderter Execution-Zweck |
| 7. `HistoricalWorkerClient.AnalyzeAsync` | Supported Input → echter Prozess / Response / Lifetime-Observation | Bounded I/O, echte Worker-Image-/Exchange-Bindung für Finding-relevante Calls, nach Exit/EOF. Heute B21-Image-Witness für Doc-Opt-ins; für Solution-Summary fehlt der gezielte Witness-Mint noch. E, exakte gesendete Bytes R/S/G/V |
| 8. `HistoricalAnalysis.Reconstruct` | Graph.Nodes / originale Referenzen und Source-Strings → separate Historical Compilations | Exakte Assemblies/Module/References, vollständige Source-Scope-/Parse-Eignung, keine Source-Ausführung. R/S/G/V; noch keine Finding-Berechtigung |
| 9. `HistoricalAnalysis.Analyze` | Root, Root-Compilation + Supporting-Compilations → Direct oder Session | Root source-owned/parameterlos/static/nongeneric, kein Metadata-Fremdroot; identifizierter Root muss im eigenen Tree/Model sein. R/S/G/V |
| 10. `ExceptionFlowSummaryAnalysisSession.Analyze` → `ExceptionFlowFiniteOrbitAdmission.Discover` | Gewählter Source-Root/Environment → Certificate/Seal oder OUTSIDE | P1 vor Root-Registrar; endliches Definition-/Binder-Inventar und positiver Constructor-Zyklus-Check, keine Concrete-Depth-Heuristik. Q, R/S/G/V |
| 11. `ExceptionFlowSummaryGraph.RegisterTarget` / `GetOrAdd` | Späterer constructed Context → Graph-Key/Scheduling | P2 Transition-/Scope-/Ground-Context-Check vor Registrierung/Enqueue. Kein Raw-Graph-Fallback. Q/G/S/V |
| 12. Session `BuildPendingSummaryNodes` / `ExceptionFlowSummaryGraphEvaluator.Evaluate` | Endlicher versiegelter Graph → SCC-/Cache-sicheres Ergebnis | A5-Completion/Ancestor-Exclusions, exakte Substitution/Catches/Owners; keine aktive Partial-Summary als Complete. Q/G/V |
| 13. Session Projection + `RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult` | Native Result → Canonical Flow | Eindeutiger Destination-Roundtrip, keine Uncertainty/Truncation als Complete. Raw Diagnostic Entry ist nicht dieser Pfad. Q/R/V |
| 14. `HistoricalAnalysis.Analyze` Response | Native Profile/Flow → `WorkerResponse` | OUTSIDE ergibt Fehler ohne Result, echte Identity bleibt; Erfolg nur nach shared Contract, Uncertainty/Truncation zusätzlich abgewiesen. Kein neuer Q-Wire-Claim nötig. Q/R/S/G/V/E |
| 15. `HistoricalWorkerResponseValidation.Validate` | Echte Response + exakter Request + private Execution-Capability → `HistoricalWorkerCallResult` | Protokoll/Identity/Provenance/Type-/Source-/Path-/Reference-World, Complete/Proven und physischer Worker geprüft. Raw Revalidation ist keine Execution-Authority. R/S/G/V/Q/E |
| 16. Vorhandenes `findingReceipts` / `HistoricalFindingFacts`, `TryGetFindingFacts`; geplanter lokaler Solution-Receipt-Builder | Validierte Calls + originale Operation → Direct-/Summary-Receipts und validiertes Paar | Heute nur Single-Project-Facts; lokaler `OwnershipSnapshot` genügt NICHT für G. Geplant: Input-/Execution-Objektbindung + vollständiger G/R/S/V-Vergleich; keine Receipt aus Caller-DTOs oder falschem Run |
| 17. `FindExternalExceptionSmellsAsync` / `RenderBoundedFindingEvidence` / `AddExceptionFindingsCore` | Nur validiertes Receipt-Paar + Root-Docs → Findings | Heute globale Solution-Rejection vor Dispatch. Geplant bestehende Policy nach Pair-Gate; keine Worker-Finding-Policy, keine neue Symbol-/Summary-Engine. R bestimmt Owner/Anchor, Q ist Voraussetzung |
| 18. `ToolRunner.RunExternalTarget`, `RunResult.AccumulateFindings`, Console/JSON/SARIF | Erfolgreiche bounded Operation → einmaliges Reporting | Kein Reporter-/Erfolgsresult vor gesamter Operation. P0 bleibt Reporting, Supporting-Nodes nie zusätzliche Roots. Vollständiger Finding-Multiset/Count/Location/Context, keine Name-/Location-Deduplizierung |

Konkrete aktuelle Quellen liegen unter
`src/XMLDocNormalizer/Execution/{Analysis,Historical,Semantic}`,
`src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector*.cs`,
`src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow` und
`src/XMLDocNormalizer.HistoricalWorker/{HistoricalAnalysis,WorkerProtocol}.cs`.
Die Source-Hashes sind in `start.json` fixiert. Im
[Readiness-Bericht](../../P5O2B25B-readiness-review.md#12-quellenregister-konkrete-dateien--entry-methoden)
sind alle konkreten Quell-/Testdateien verlinkt; dessen historische Aussagen
werden hier nicht überschrieben.

## 4. Minimaler Receipt-Ansatz ohne neues Wire-DTO/Worker-Profil

### 4.1 Bereits ausreichend vorhanden

- Das bestehende Wire-Input stellt Solution-Graph und kompletten Scope bereits
  dar; die bestehende Solution-Analyse braucht kein Finding-/Mode-Alias.
- Das bestehende lokale Direct-Finding-Profil kann tatsächlich unabhängige
  Direct-/Dokument-Evidence liefern. Direct betrachtet keine Supporting-Bodies.
  `ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions` benutzt
  Direct-Traversal; `LocalModes_DoNotWidenToSupportingProjects` zeigt auch den
  echten historischen Root-only-Direct-Run. Root-Referenzwelt und Original-
  Source-Scope müssen trotzdem identisch zum Graph-Target sein.
- B21 besitzt bereits den privaten Beweis über exakten gesendeten Request,
  echte Response, Diagnostics, Exit, PID und konfiguriertes ausgeführtes Image.
- `HistoricalWorkerDeclaredDocumentationProjection.TryType` und seine
  Binding-/Display-Prüfung kennen die ursprüngliche Root-PE-Typwelt. Für die
  hier erlaubten nongenerischen finalen Typen muss kein Source-Compiler im
  Main nachgebaut werden.
- Die gemeinsame Main-Policy nimmt Canonical-Types und Root-Document-Facts;
  der Worker muss keine Findings oder Policy-Entscheidungen transportieren.

### 4.2 Vorgesehene zwei Subqueries, keine neue Historical-Profilfamilie

Die **äußere** Finding-Operation ist immer SolutionTransitive. Sie besitzt einen
unveränderlich registrierten Operations-/Graphkontext. Darin:

1. **Direct-/Dokument-Subquery:** vorhandenes Single-Project-Direct-Profil mit
   `WorkerFindingRequest(DirectEvidence, vorhandener Direct-Dokument-Request)`.
   Root/Source/References sind exakt das Target des registrierten Gesamtgraphen.
   Die tatsächliche Subquery bleibt Mode Direct; kein Umbenennen als Solution,
   kein ProjectTransitive-Summary-Ersatz.
2. **Summary-Subquery:** vorhandenes vollständiges Solution-Flow-Payload ohne
   neuen Finding-Opt-in/Wire-Enum. Es läuft über denselben echten Dispatch und
   die A9/A10-Session. Für seinen realen Exchange wird gezielt dieselbe strenge
   B21-Ausführungsbeweis-Mechanik benutzt.

Die unterschiedliche Subquery-Scope ist explizit und sachlich: Direct benötigt
den exakten Root-Scope, nicht zusätzliche Source-Body-Traversierung. Das lokale
Direct-Result wird aber **nicht** allein wegen gleichen Contents/Roots zu einem
beliebigen Solution-Graph promoted. Die vorbereitete Operation registriert
seine konkrete Subquery als Direct-Rolle genau dieses G/R/S/V-Kontexts und
verifiziert am validierten Call die ursprüngliche Request-Objekt-/Snapshot-
und Execution-Bindung. Eine separat ausgeführte strukturell gleiche lokale
Direct-Evidence reicht nicht.

Damit bleiben `WorkerProtocol`, dessen Version/Enums, alle Worker-Profile,
Compiler-Versionen und Shared Analyzer unverändert. Insbesondere wird
`single-project-root-findings-v1` **nicht** auf Solution-Requests erweitert und
`WorkerDeclaredPolicyMode.ProjectTransitive` nicht als Solution-Alias verwendet.
Die im Readiness-Review genannte mögliche additive Finding-Profil-Erweiterung
ist hierfür nicht die bevorzugte Wahl: bestehende Subqueries plus lokale
Composition reichen aus. Ein neuer Wire-Contract wäre ein anderer STOP, nicht
ein heimlicher Teil von C1–C3.

### 4.3 Lokale Facts, Capability und Pair-Gate – getrennte Zuständigkeiten

| Eigentümer | Erzeugt / validiert | Darf nicht übernehmen |
|---|---|---|
| Main-Analysis / originale Projection-Factories | Registrierter unveränderlicher Operationskontext: kompletter Kettengraph, Root, Source-/Reference-World, tatsächliche Stage-Requests und frische Operationsbindung | Kein Prozessstart, keine Engine-Auswahl/Analyseroutine im reinen Payload-Projector; dessen normativer Architecture-Guard bleibt |
| Historical Client | Privater, gezielt angeforderter vollständiger Execution-Witness; unveränderte Image-/Request-/Response-/EOF-/Exit-Prüfung | Keine Graph-/Finding-Policy, keine Roslyn-/Session-Abhängigkeit, keine Authority allein durch angeforderte Witness-Erfassung |
| Main Response Validation | Originaler erfolgreich validierter Call plus seine tatsächliche Request-/Execution-Bindung; Direct-Facts bzw. complete Summary-Facts | Keine Caller-JSON-Re-Admission, kein Record-Clone-Receipt, keine Replays; Failure gibt keine Facts/Receipt frei |
| Main-lokaler Receipt-Builder | DirectReceipt und SummaryReceipt an denselben erwarteten G/R/S/V-/Operationskontext; geprüfte Stage-/Worker-Bindungen | Keine neue Symbol-/SCC-/Finding-Engine; kein Wissen über Reporter oder Summary-Graph-Implementierung erforderlich |
| Finding-Layer | Vorhandene Policy auf dem validierten Paar; Root-Anchor und unveränderte Fehlermenge | Keine Receipt selbst minten; keine Current-/kleinere-Scope-Fallbacks oder Nachnormalisierung |

Die alte B24-Receipt-/Scope-Snapshot-Semantik bleibt unberührt. Solution erhält
zusätzliche **kleine Main-lokale** Facts, nicht einen aufgeweichten lokalen
`OwnershipSnapshot`. Der Client kann eine neutral-typisierte Main-lokale
Capture-Anforderung für eine konkrete Request-Instanz verwenden; sie erzeugt
noch keine Admission. So muss weder der reine Payload-Projector Client-/JSON- /
Hash-/Execution-Code aufnehmen noch der Client eine Analysis-/Finding-Layer-
Rückabhängigkeit bekommen. Legacy-Flow-Calls werden nicht pauschal auf einen
anderen Failure-/Attestation-Vertrag umgestellt.

Die heutige `DocumentationExecution`-Prüfung exportiert bislang hauptsächlich
den Worker-Image-Matcher. Für das neue Paar muss die lokale, nach wirklicher
Validation entstandene Call-Bindung zusätzlich die **konkrete ursprüngliche
Request-Instanz und den tatsächlich ausgeführten immutable Snapshot** erhalten.
`call.Provenance == expected` oder Wertegleichheit allein ist nicht ausreichend:
Ein anderer echter Run mit gleichem Output darf nicht hineingepasst werden.

Der Summary-Worker liefert heute keine Dokument-Displays für seine zusätzlichen
Flow-Typen. Diese dürfen im Main nur mittels der **bereits vorhandenen** exakten
Original-PE-Typprüfung gewonnen werden. Root-XML/cref-Bindungen und Anchors
bleiben die tatsächlich attestierten Direct-/Dokument-Fakten; keine synthetisch
erfundene Worker-Dokument-Response und kein Source-Rebinding. Ein finaler Typ,
dessen volle Identity im retained Root-PE-World nicht eindeutig bindbar ist,
bleibt außerhalb des Finding-Slices. Generics im finalen Typ oder Containing-
Owner erweitern den bisherigen `TryType`-Vertrag nicht.

### 4.4 Pflichtfelder / Invarianten der Receipts

Beide logischen Receipts müssen lokal an folgende validierte Tatsachen gebunden
sein; die Struktur darf bestehende Facts/Capabilities referenzieren, statt sie
monolithisch neu zu erfinden:

- erwartete äußere AnalysisMode SolutionTransitive;
- volle Canonical Member Identity und originale Root-PE-/Module-/MVID-Bindung;
- kompletter eingefrorener Graph mit Target, geordneten Project-/Source-/
  Reference-/Parse-/Ownership-/Dependency-Identitäten und Checksum-Welt;
- unveränderlicher Reporting-Scope und RootLogicalPath; Original-Dokument/
  Member-Anchors, nicht Supporting-Project-Reporting;
- konkrete Subquery-/Execution-Rolle: Direct ist nicht Summary;
- genau registrierte Request-Instanz, Snapshot und tatsächlicher validierter
  physischer Worker-Run; frische private Operationsbindung;
- Direct: vollständige Proven-Direct-/Dokument-Facts, **kein erfundener A9-
  Summary-Admission-State**;
- Summary: tatsächlicher A10-Worker-Erfolg aus der Session, ohne Uncertainty/
  Truncation und mit vollständigem Canonical-/Reference-/Location-Check;
  dadurch aus dem ausgeführten A10-Contract abgeleitete admitted Summary,
  nicht ein aus fehlender `ProfileOutcome`-Wire-Property erfundenes Boolean;
- Der echte ausgeführte Summary-Snapshot hat dafür explizit
  `Context.AnalysisMode == "solution-transitive"`, den ursprünglichen
  `Context.Solution`-Graph und **keinen** `DeclaredExecution`-/DeclaredPolicy-/
  Finding-Direct-Opt-in. So kann ein erfolgreicher Direct-Run nicht als
  Session-/A9-Summary-Erfolg ausgegeben werden. Diese Eigenschaft gehört zur
  Request-/Execution-Bindung, nicht zu einem nachträglichen Caller-Label;
- nach Pair-Prüfung keine offenen Root-/Scope-/Graph-/Execution-/Type-Owner-
  Widersprüche. Ein wiederverwendeter/caller-kopierter Call mintet nichts.

Der No-doc-Guard darf nach validierter Graph-Input-/Direct-/Dokument-Admission
unverändert Summary überspringen und keine Findings liefern. Er behauptet
damit keine nicht ausgeführte Summary als exact complete. Bei dokumentiertem
Root ist Summary dagegen unabhängig von Declared-Type-Inventaren erforderlich.

## 5. Current zuerst, Historical separat; Witness- und Policy-Plan

Noch kein neuer Runtime-Beweis wurde ausgeführt. Nach Freigabe zuerst für die
neuen Source-/Graph-Fixtures über echte Current-Session und native Finding-
Policy die expliziten Soll-Owner/Typen/Counts/Anchors prüfen, dann die genuine
Historical-Operation mit denselben unabhängig richtigen Sollwerten. Parität
ist nur eine zusätzliche Differentialprüfung.

| Witness | Eigenständiger Current-Sollvertrag | Genuine Historical / Receipt-/Reporting-Vertrag |
|---|---|---|
| W1 zwei Projekte | ein korrekter Supporting-Throw-Typ, Source-Pfad P1, ein Missing-Finding am Root P0 | beide realen Stages, passende vollständige Receipts, ein Finding P0, keine Duplikate |
| W2 drei Projekte | P0→P1→P2 vollständige Path-/Source-Owner-Kette, exakte Kardinalität | vollständiger originaler Kettengraph und identische Source-/Type-Owner-Fakten; Reporting nur P0 |
| W3 gleiche Typnamen | zwei volle Canonical Owner P0/P1, nicht gleiche DeclarationId als Identity | zwei getrennte Entries und zwei erwartete Findings; Multiset, keine Display-/Location-Deduplizierung |
| W4 Root/Scope/Graph/Run mismatch | zugrunde liegende kontrollierte Ergebnisse jeweils korrekt | echte validierte Stages aus verschiedenen registrierten Kontexten können kein Paar/Finding erzeugen; cloned/raw/replayed Calls ebenfalls nicht |
| W5 OUTSIDE/incomplete | Session-Certificate vor exact Expansion, kein exact Missing/Extra; bestehende Diagnostik getrennt | UnsupportedContext oder Validation-Failure, keine Summary-Receipt und keine normalen Findings; No-fallback |
| W6 reale Execution | nicht als gleichwertiger Ersatz benutzt | echter getrennter Worker, tatsächliche Image-/Roslyn-Identity/Exit/EOF, Main-Validation vor Receipt/Policy; keine Historical-Assembly im Main |
| Direkte + transitive Mischung | **neuer** Witness mit tatsächlich nichtleerer unabhängiger Direct-Evidence, bekannte DOC610/DOC611-Deduplizierung | Direct aus Root und Summary aus Supporting-Scope werden unabhängig geprüft; alter B25-`mixed` ist kein Ersatzbeweis |
| Generic-Catch/Rebinding | finite interne Method-/Containing-/Nested-Construction und exakter Catch, vollständiger nongenerischer finaler Finding-Typ | echter Historical-Generic-Catch- und cross-project-Witness; generische finale Result-/Doc-Typen weiterhin sichtbar ausgeschlossen |
| Typ-/Pfad-Deduplizierung | gleicher voller Typ über mehrere Pfade einmal; verschiedene Owner getrennt | dieselbe Main-Policy, echte Console/JSON/SARIF-/RunResult-Kardinalität, keine zweite Worker-Policy |

Besonders aussagekräftiger W4-Fall: zwei echte, jeweils valide Graphen können
denselben Root und dasselbe Canonical-Ergebnis haben, z.B. zwei Knoten versus
drei mit einer zusätzlich vorhandenen, referenzierten aber im ausgewählten
Body nicht benutzten Supporting-Source. Gleiche Root-/Result-Werte beweisen
dann gerade keine gleiche Graph-/Execution-Bindung. Das Pair-Gate muss die
gekreuzten Stages vor Policy ablehnen. Ein nur grob kaputter Request, der schon
vor Worker-Start scheitert, ersetzt diesen Test nicht.

W4 darf echte Stages am **auch produktiv verwendeten** Receipt-/Adoption-Seam
gezielt kreuzen. Kein Test darf DTOs selbst als validiert branden, eine Fake-
Receipt-Fassade benutzen oder den normalen erfolgreichen W1/W2/W6-Pfad
durch einen Test-Renderer ersetzen.

Normale Findings nur aus admitted/complete und gültigem Paar. A9-OUTSIDE,
Unknown Residual, fehlende Coverage, Worker-/Identity-/Receipt-Failure ergeben
keine normale Historical-Finding-Adoption. Native Current-DOC631/gebundene
Tag-Diagnostik bleiben unverändert; kein neuer Historical-Diagnose-Import.
Ein innerlich A9-admitted generischer Result-Typ kann zusätzlich am äußeren
nongenerischen Finding-Vertrag scheitern: **nicht** rückwirkend A9-OUTSIDE
behaupten, um diesen Unterschied zu verstecken.

## 6. Vollständiger direkt betroffener Characterization-Abgleich

| Bestand / tatsächlicher Entry/Input | Entscheidung / Grund |
|---|---|
| B25 Theory, 15 bestehende originale zweier/dreier Inputs | C1: erste Rejection-Hälfte des Helpers wird bounded positiv; Flow/Graph/Owner/Repeat/Lifetime-Assertions unverändert |
| B25 Same-name-P0/P1-Fact | C1: dieselbe Rejection-Hälfte wird positiv; alle bereits richtigen zwei Owner/zwei DOC611/volle P1-Auswahl bleiben strikt erhalten |
| B25 Helper `directProfile` mit Project-Finding auf Solution-Input | Unverändert negativ; kein Alias und keine Erweiterung des Single-Project-Wire-Profils |
| B25 sieben Graph-Mutationsfälle | Unverändert negativ: falsches Target/Source/Dependency/MVID/Edge/LogicalPath/Member, keine neue Lockerung |
| B25 drei vorhandene CLI-Fälle | Unverändert negativ: ihre Jobs haben P0-Materialien, **keine** Supporting-Materialien. Auch leerer Root ist kein Ersatz für den erforderlichen expliziten Graph |
| B25 Source-Guard, genau zwei Assertions | C2: globale Rejection/fehlende Supporting-Komposition ist überholt; funktionaler bounded Positiv-/Vierer-Negativkontrollfall statt Source-String-Trick |
| B25A Source-Guard, genau zwei Assertions | C3: derselbe Fall; exakte Normalizer-/alle anderen Ownership-Assertions unverändert |
| B24, alle 42 Fälle | Unverändert: lokale Facts/OwnershipSnapshot, getrennte Direct-/Project-Summary, cloned/raw/replayed/unattested/wrong physical Worker bleiben abgewiesen; Single-Project-Profile werden nicht umgedeutet |
| B24 CLI unsupported Solution ohne Graph / Mode-Konflikte | Unverändert: `not admitted` bzw. `conflicts`, kein Erfolgsreport; neue vollständige Solution-Jobs als neue B25B-Tests |
| B23 Legacy-Mode-Overrides / `--force-historical` | Unverändert: keine neue Alias-/Force-Option, alte v1-Default-Declared-Jobs unverändert |
| B15 `LocalModes_DoNotWidenToSupportingProjects` | Unverändert: alter direkter Flow-Entry mit falsch gemodetem Graph lehnt ab; neu explizit vorbereitete lokale Direct-Subquery bleibt tatsächlich lokal |
| B15 invalid/missing/copy/foreign scope / supporting compiler negatives | Unverändert: keine fehlenden Sources/Scopes/Referenzen oder stärkere Engine durch B25B ergänzen |
| B22 `UnsupportedFindingScopes_AreRejectedBeforeDispatch`, auch solution/cross-project | Unverändert: Inputs besitzen DeclaredPolicy oder keine valide Kette; kein cross-project Declared und keine neue Interpretation derselben invaliden Requests |
| B17–B21 Mode-/Binding-/Replay-/Original-Origin-Negative | Unverändert: bestehenden Requests/Receipts nicht zusätzliche Authority verleihen; neues lokales Pair nur aus tatsächlichen realen Stages |
| A10/generic identity/Catch/SCC/cache | Unverändert: keine Analyzer-, Raw-Graph-, Definition-/Depth-/Retry-, Canonical- oder Failure-Erwartung wird wegen Finding-Adoption geändert |
| Manifest-/Worker-/Main-Architektur | Unverändert normativ; Details unten, kein zusätzlicher Approval-Antrag für Lockerungen |

Damit sind die vorhersehbaren fachlichen Testkonflikte **vollständig C1–C3**.
Die Scope-/Legacy-Negative werden gerade nicht pauschal mit aktiviert. Für neue
B25B-Witnesses/Harnesses und rein mechanische Lifetime-Korrekturen gelten die
vom Auftrag erteilten Befugnisse; daraus folgt keine zusätzliche Freigabe für
eine bestehende fachliche Assertion.

### Insbesondere geprüfte normative Architektur-Guards

- `ExceptionFlowHistoricalPayloadProjectionTests.Architecture_NoSelectionValidationParsingExecutionOrRawPayloadInput`:
  im reinen Projector weiterhin keine Client/Router/Selection-/Process-/File-/
  PEReader-/ParseText-/SHA256-/JsonSerializer-/AnalyzeAsync-Verantwortung.
  Darum liegen Operation-/Receipt-Snapshots nicht in dieser Datei.
- `HistoricalWorkerClientTests.Architecture_OnlyNeutralProtocolSourceAndNoAnalyzerDependencies`:
  keine Roslyn- oder Session-Abhängigkeit, keine neuen Analyzer-ProjectReferences,
  sichere ArgumentList, keine File.Write. Capture-Anforderung ist rein Main-lokal
  und neutral, keine Rückabhängigkeit auf Finding-/Analysis-/Graphklassen.
- `ExceptionFlowHistoricalDeclaredProductionFindingTests.PermanentBoundary_MainOwnsGuardAndPolicy_WorkerOnlyExecutesExplicitPurpose`:
  Worker bekommt keine Guard-/Finding-/Reporting-Relevanzentscheidung; External
  Finding-Adapter baut keine CSharpCompilation/GetSemanticModel/Assembly.Load.
- `ExceptionFlowHistoricalBoundedFindingTests.Boundary_NewModesNeverUseDeclaredEligibilityOrMutateCurrentPolicy`:
  bestehender lokaler unabhängiger Pfad behält seine RequiresSummary-/Render-
  Regeln und fragt kein Declared-Inventar; B21-Capture erst nach Exchange/Exit/EOF.
- `HistoricalWorkerLifetimeTests.Boundary_PublishesObservationOnlyAfterExistingOwnedExitAndPipeCleanup`:
  bestehende Handle-/Pipe-Reihenfolge, kein GetProcessById/StartTime-Polling.
- Shared manifest weiterhin 125 physische Sources; kein Finden/Receiptwissen
  in Session/Graph/Worker-Protokoll. Die beiden C2/C3-Source-Abwesenheitsguards
  sind die explizit beantragte Aktivierungs-Characterization, nicht eine Lizenz,
  irgendeine dieser Architekturregeln zu lockern.

## 7. Implementierungs-/Validierungsplan nach Freigabe

1. Kleine Main-lokale Operation-/Direct-/Summary-Bindings mit unveränderlichem
   Graph-/Root-/Scope-/Reference- und tatsächlichem Request-/Run-Bezug.
2. Gezielte neutrale Execution-Capture für die registrierte Summary; gleiche
   B21-Validierung und Fehlersemantik, keine Legacy-/Raw-Flow-Authority-Aufwertung.
3. Bestehende Main-Response-Validation als einzige Receipt-Mint-Grenze;
   Main-Receipt-Pair und Original-PE-Type-Displays vor Policy/Rendering prüfen.
4. Bounded Solution-Branch am bestehenden Finding-Entry; bisherige B22/B24-
   Branches/Calls und Current unverändert, No-doc-Guard nach Admission.
5. Explizite lokale Supporting-Materialien für neue gültige CLI-Jobs; bestehende
   ungültige Jobs weiter sichtbar ablehnen, keine Discovery/zweite P5-Validierung.
6. C1–C3 exakt im genehmigten Umfang; neue Evidence ausschließlich B25B.
7. Zuerst eigenständige Current-Sollwerte der neuen Witnesses, dann genuine
   Historical-Stages und W1–W6/Negativ-/Generic-Catch-/Direct-Mix-/Reporter-Beweise.
8. Gezielte Suite: neue Witnesses, B25 **27/27**, A10, Ownership/Construction/
   Rebinding/Catch, A5 **33/33**, A6 **8/8**, B24 **42/42**, Solution Scope/Finding,
   Worker/Attestation, External Target. Erst wenn komplett grün Broad, dann
   Boundary, dann Full/Dual/WAE11/Current8 und relevante A6–A10-/neuer B25B-Gate.

Historische A10-/A-/B25-Gates binden alte HEADs/Hashes/absolute inzwischen
gelöschte Artifacts-Pfade. Sie werden nicht überschrieben oder mit neuen
Inputs als angebliche historische Erfolge ausgeführt. Neue B25B-eigene Adapter
müssen die relevanten unveränderten Verträge gegen die tatsächlich frisch
kompilierten B25B-Inputs/-Resultate nachweisen. Export/Copy niemals rekursiv aus
`artifacts` und nicht mit alten, breit gestageten Proof-Builds aufblähen.

Ein auftretender bekannter Harness-/Fixture-Defekt kann im ausdrücklich
zugelassenen Umfang korrigiert und einmal erneut geprüft werden; kein Grün-
Retry und keine bestehende fachliche Assertion außerhalb C1–C3 ändern. Ein
wirklich neuer Wire-/Layer-/Production-Vertrag bleibt STOP-pflichtig.

Der frühere CLI-Absturz `0xC0000005` bleibt separat ungeklärt, nicht als Flakiness
oder behoben bezeichnet. In diesem reinen Preflight wurde kein Worker-/CLI-
Testprozess gestartet; daraus entsteht kein neuer Reproduktions-/Heilungsbeweis.

## 8. Ergebnis dieses Turns

**Einmaliger Approval-STOP mit vollständigem Batch C1–C3.** Kein Produktionsfix
begonnen, kein bestehender Test geändert, kein Runtime-Gate gestartet. Nach
gezielter Freigabe direkt mit dem beschriebenen begrenzten Paket fortsetzen;
nicht wegen derselben vorhersehbaren Assertions erneut einzeln anhalten.
