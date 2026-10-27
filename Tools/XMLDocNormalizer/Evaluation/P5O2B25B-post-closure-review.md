# Post-B25B Closure Review

Stand: 27.10.2026. Ausschließlich statische Analyse und Planung.

**Hauptempfehlung: PRODUCTIZATION/DOCUMENTATION NEXT.** Der ursprüngliche
bounded B25-Vertrag ist erreicht. Weitere allgemeine Historical-/Generic-
Aktivierung ist dafür nicht erforderlich. Die wichtigste verbleibende
Produktlücke ist die Sichtbarkeit von Analyseabdeckung und Unvollständigkeit
im normalen CLI-Ergebnis. Danach hat die bereits vorgemerkte
ProjectTransitive-Performance Vorrang vor weiterer semantischer Ausweitung.

## 1. Baseline, Methode und Aussagegrenzen

| Gegenstand | Geprüfter Stand |
|---|---|
| HEAD | `789a924275db0281e107118c8ca5a79f3580956a` |
| Commit | `Enable bounded solution transitive finding adoption` |
| Parent | `1ef19a37bbecc2ba1f9f2b9e2be27f7cc092c65e` |
| Branch | `fix/documentation` |
| Lokal gespeicherter Upstream | `origin/fix/documentation`, identischer HEAD |
| Eingang vor neuen Review-Dateien | Ausschließlich geschützter Root-`.gitignore`-WIP; semantischer Index leer |
| Prüfung | Code-/Test-/Berichtslektüre, Call-Site-Suche, statische Größen, SHA256-Schutzvergleich |
| Nicht ausgeführt | Build, Tests, Produkt-/Evidence-Gates, Benchmarks, Crash-Reproduktion |

Der Push ist durch den Auftrag und den identischen lokalen Tracking-Ref
belegt; in diesem Review fand keine zusätzliche Remote-Git-Abfrage statt.
Der Start-Capture enthält bereits das neu angelegte Capture-Skript als
untracked Review-Datei. Er ist kein unveränderter Snapshot vor dieser
ersten zulässigen Review-Datei.

Nachweise: [Start und statische Größen](P5O2B25BPostClosureReview/start.json),
[Capture-/Vergleichsskript](P5O2B25BPostClosureReview/Capture-StaticAudit.ps1),
[abschließender Schutzvergleich](P5O2B25BPostClosureReview/verification.json).
Die statische Prüfung beweist weder Vollständigkeit für ganz C# noch neue
Laufzeitstabilität. „CLOSED“ unten bezeichnet den jeweils genannten Vertrag,
nicht einen universell vollständigen Exception-Analyzer.

## 2. Rekonstruktion B25 → A1–A10 → B25B

**Ursprüngliches Ziel:** Die bereits vorhandene originale externe
Zwei-/Dreiprojekt-Solution-Evidence sollte für einen ausgewählten Reporting-
Root produktive Historical-SolutionTransitive-Findings erzeugen. P0 meldet,
P1/P2 liefern Supporting-Evidence. Direct- und Summary-Beobachtung müssen
getrennt erhalten bleiben; die bestehende Main-Finding-Policy soll normale
Console-/JSON-/SARIF-Ergebnisse liefern. Beliebige Solutions, generische
finale Finding-Typen oder ein universeller rekursiver Solver waren kein
notwendiger Bestandteil dieses bounded Aktivierungsziels.

**B25-STOP:** Gleichnamige `Shared.Fault` aus P0 und P1 wurden bei der
Normalisierung zusammengefaltet. Die lokalen Summaries unterschieden die
Owner korrekt; erst der ausgewertete Root verlor sie. Der Pfad enthielt
weiter den Throw aus P1, während der Type Owner auf P0 zeigte. Ursache waren
`OriginalDefinition` und `GetFirstSymbolForDeclarationId` gegen die Root-
Compilation: Declaration-ID/Name allein trägt keine Assembly-Konstruktion.
Current/Historical-Parität bestand auch für den Fehler und war deshalb kein
hinreichender Korrektheitsbeweis. B25 nahm die Aktivierung wieder zurück.

| Schritt | Tatsächlicher Beitrag und neue Erkenntnis |
|---|---|
| B25A/A1 | Assembly-/Module-/Owner-Identität inventarisiert; ein isolierter Top-Level-Fix reichte für constructed/nested Typen und Catches nicht. Kein damaliges allgemeines Produktions-GO. |
| A2 | Verlust konstruierter Typen in Normalisierung, Catch-Vergleich und Nested-Rebinding aufgezeigt. Vollständige kanonische Identität statt Name/Definition erforderlich. |
| A3 | Der reale Call-Site hatte Method-/Containing-Argumente; der Summary-Kontext verlor sie. Ein konkreter Key allein substituiert zudem keinen offenen Quellkörper. Exakte wachsende Keys bedrohen die Terminierung vor der Evaluation. |
| A4 | Context-Repräsentation und weitere Grenzen untersucht; zusätzlich endlichen rekursiven Cache-Fehler entdeckt: ein unter aktiver Ancestor-Sperre leeres Teilresultat wurde später als uneingeschränkt vollständig wiederverwendet. |
| A5 | Endliche SCC-/Cache-Soundness korrigiert: SCC-interne relevante Ancestor-Ausschlüsse gehören zum Evaluation-Kontext; aktive Teilberechnungen werden nicht als allgemeine Ergebnisse publiziert. Azyklische Wiederverwendung bleibt erhalten. |
| A6 | Finite versus strukturell wachsende Konstruktionen getrennt. Ein endlicher Trace beweist keine Terminierung. SCC-Soundness allein beendet keine unendliche konkrete Context-Expansion. |
| A7 | Finite parametrische Gleichungen/Definition-Folding liefern nicht automatisch einen endlichen exakten konkreten Ergebnisraum oder eine catch-erhaltende Quotientenbildung. Kein pauschales Verbot konservativer Abstraktion. |
| A8 | Repository-interne konstruktive PCP-Reduktion für den allgemeinen korrelierten rekursiven Type-Family-/Catch-Vertrag. Eine totale allgemeine exakte Lösung ist für diesen Vertrag nicht verfügbar. Nicht jede Rekursion und nicht jedes OUTSIDE-Beispiel ist damit individuell unentscheidbar. |
| A9 | Positiven entscheidbaren Vertrag `A9-FiniteOrbit-v1` festgelegt: geschlossene Owner-/Binder-Inventare, totale simultane Substitution, keine constructor-positive zyklische Slot-Abhängigkeit, endlicher konkreter Output; klassifizierte Ablehnung sonst. |
| A10 | Diesen Vertrag produktiv vor Root- und Context-Registrierung implementiert; konkrete Identität, Substitution, Catch und Projection im admitted Bereich korrigiert; gleiche Shared-Semantik in Current/Historical. Raw-Graph-Diagnostik bleibt unterhalb Admission. |
| B25B | Die verbleibende bounded Historical-Finding-Lücke geschlossen: originale graph-/request-/executiongebundene Direct-/Summary-Receipts, validiertes Paar, PE-gebundene Typdisplays und bestehende Main-Policy. Keine neue Analyzer-Engine und kein neues Wire DTO. |

Die Identity-/Substitution-/SCC-/Admission-Arbeit waren notwendige Grundlagen,
die B25 erst sichtbar machte. Die spätere Wahl eines entscheidbaren Profils
ist eine begründete semantische Grenze. Die konkrete Beschränkung des
Historical-Finding-Adapters auf eine Zwei-/Dreierkette ist dagegen eine
Engineering-/Scope-Entscheidung, kein mathematisches Resultat.

Historische Quellen:
[B25](P5O2B25-bounded-solution-finding-adoption.md),
[A1](P5O2B25A-assembly-qualified-type-ownership.md),
[A2](P5O2B25A2-canonical-exception-type-identity.md),
[A3](P5O2B25A3-constructed-generic-summary-context.md),
[A4](P5O2B25A4-bounded-generic-context-expansion.md),
[A5](P5O2B25A5-recursive-summary-scc-closure.md),
[A6](P5O2B25A6-bounded-generic-context-expansion-after-scc.md),
[A7](P5O2B25A7-finite-parametric-generic-context-contract.md),
[A8](P5O2B25A8-correlated-recursive-type-family-contract.md),
[A9](P5O2B25A9-decidable-query-profile.md),
[A10](P5O2B25A10-finite-orbit-admission-and-projection.md),
[B25B](P5O2B25B-bounded-solution-finding-adoption.md).
Frühere STOPs bleiben historische Befunde; spätere Closure löscht sie nicht.

## 3. Closure-Matrix

Statusvokabular: `CLOSED`, `BOUNDED-CLOSED`, `INTENTIONAL-LIMIT`, `OPEN`,
`OBSOLETE`. „Dauerhaft?“ unterscheidet eine Invariante vom heutigen Scope.
Die Quellanker S1–S9 werden unmittelbar unter der Matrix aufgelöst.

| Thema | Ursprung | Lösung / heutiger Vertrag | Aktueller Status | Dauerhafte Grenze? |
|---|---|---|---|---|
| Assembly-qualified Exception Identity | B25/A1 | Vollständige kanonische Assembly-/Typidentität, kein Name-only-Rebinding; S2 | CLOSED | Ja: Owner-Trennung |
| Module Identity | A1, B12/B15 | Symbolische Module-Identität plus getrennte physische PE-/MVID-Provenienz; kanonischer Symbol-MVID ist absichtlich kein physischer Image-Beweis; S2/S6 | BOUNDED-CLOSED | Ja: Trennung; Multimodule außerhalb externem Profil |
| Project Ownership | B25 | Assembly-/Project-Welt validiert, originale Scope-Zuordnung; S5/S6 | BOUNDED-CLOSED | Ja: Identitätsbindung |
| Reporting vs Supporting | B25/B25B | P0 meldet, P1/P2 liefern Belege ohne Reporting-Aufwertung; S5/S8 | BOUNDED-CLOSED | Ja: Rollentrennung; Ein-Root-Scope erweiterbar |
| Gleichnamige Typen verschiedener Assemblies | B25 | Owner-distinkte Summary-/Finding-Identitäten, gezielte P1-Auswahl; S2/S8 | CLOSED | Ja |
| Constructed generic identity | A2/A3 | Konkrete kanonische Konstruktion statt OriginalDefinition; S2/S3 | BOUNDED-CLOSED | Exact innerhalb v1 |
| Method Type Arguments | A3/A6 | InvocationSubstitution und Context-Key erhalten konkrete Argumente; S2/S3 | BOUNDED-CLOSED | Exact innerhalb v1 |
| Containing Type Arguments | A2/A3 | Vollständige constructed Containing-Kette; S2/S3 | BOUNDED-CLOSED | Exact innerhalb v1 |
| Nested Owner | A2/A3 | Nested-Typ bleibt an constructed Owner gebunden; S2 | BOUNDED-CLOSED | Exact innerhalb v1 |
| Cross-compilation rebinding | A2/A6 | Eindeutige vollständige Canonical-/Roslyn-Identität; CLR-Referenzgleichheit nicht erforderlich; S2 | BOUNDED-CLOSED | Fehlende/mehrdeutige Bindung bleibt Ablehnung |
| Rekursive Summary-Graphen | A4/A5 | Endliche admitted Context-Graphen, explizite Workqueues; S1/S4 | BOUNDED-CLOSED | Unendliche konkrete Graphen nicht admitted |
| SCC closure | A5 | SCC- und Ancestor-kontextbewusste endliche Evaluation; S4 | CLOSED | Ja: endlicher Graph als Voraussetzung |
| partial-as-complete | A4/A5/A10 | Kein uneingeschränktes Cachen aktiver Teilresultate; fehlgeschlagene Graphtransaktion entfernt neue Nodes; S1/S4 | CLOSED | Ja |
| Cache-/Context-Identity | A3–A5/A10 | Konstruktion und relevante Facts im Key, SCC-interne Sperren im Evaluationskontext; S2/S4 | BOUNDED-CLOSED | Kein globaler unbegrenzter Resultcache-Vertrag |
| Edge-order independence | A4/A5 | Erreichbare Exception nicht von Root-Kantenreihenfolge abhängig; A5-Witnesses | CLOSED | Ja: semantischer Vertrag, nicht Reihenfolge aller Trace-Ausgaben |
| Graphgebundenes Ergebnis/Evidence | B25B | Originale Scope-/Graph-Snapshots und echte Execution statt Gleichheit bloßer Resultwerte; S5/S6 | BOUNDED-CLOSED | Ja |
| Direct-/Summary-Receipts | B25B | Getrennte einmalige Capture-Tickets; Mint erst nach Main-Validation; genau ein gebundenes Paar konsumierbar; S5/S6 | CLOSED | Ja |
| Finite Generic Contexts | A3/A6/A9/A10 | Copy/Permutation/Reset und azyklisches Wrapping bei geschlossenem endlichem Orbit; S1/S3 | BOUNDED-CLOSED | v1 ist bewusster Scope |
| Structurally growing Contexts | A3/A6 | Constructor-positive Zyklen vor konkreter Expansion klassifiziert; kein Folding/Depth-Limit; S3 | INTENTIONAL-LIMIT | Ablehnung für v1; einzelne Spezialprofile erforschbar |
| Constructor cycles | A6/A9 | Positive Gewichte im selben Binder-SCC verboten; bloße Constant-/Copy-Zyklen nicht pauschal verboten; S3 | INTENTIONAL-LIMIT | v1-Regel |
| Allgemeine korrelierte rekursive Type Families | A7/A8 | Kein allgemeiner exakter Family-/Catch-Solver; S3 und A8-Reduktion | INTENTIONAL-LIMIT | Ja für beanspruchten allgemeinen exakten Vertrag |
| A7/A8-Abstraktionsgrenze | A7/A8 | Endliche Darstellung ist kein Beweis exakter Catch-/Projection-Semantik; konservative oder engere Erweiterungen bleiben denkbar | INTENTIONAL-LIMIT | Allgemein ja; nicht jede Spezialerweiterung ausgeschlossen |
| A9-FiniteOrbit-v1 | A9/A10 | Aus endlichem Declaration-/Slot-Inventar geprüft und produktiv versiegelt; S1/S3 | BOUNDED-CLOSED | Versionierter positiver Vertrag |
| P1 Root Admission | A10 | Discover/Complete/Seal vor TryRegisterSummaryGraphRoot; S1 | CLOSED | Ja |
| P2 Discovered Context Admission | A10 | Versiegelte Transition vor RegisterTarget/GetOrAdd, Context-/Scope-Prüfung vor Einfügen/Enqueue; S1 | CLOSED | Ja |
| Substitution | A2/A3/A6/A10 | Totale simultane Binder-Substitution für Quellen, Catches und Callees; S2/S3 | BOUNDED-CLOSED | Nicht repräsentierbar → OUTSIDE |
| Catch semantics | A2/A6/A10 | Konkrete Typen und tatsächliche Basistypen, keine Definition-only-Unterdrückung; S2/S4 | BOUNDED-CLOSED | Abdeckung/Bindung erforderlich |
| Result projection | A2/A10 | Exakte Owner-/Construction-Auflösung aller ausgegebenen Typen; S1/S2/S6 | BOUNDED-CLOSED | Generische finale Historical-Findings enger beschränkt |
| Source projection | B11–B15/A10 | Originale validierte Parse-/Source-/Reference-/Scope-Handoffs; S3/S6 | BOUNDED-CLOSED | Kein allgemeiner Build-Rekonstruktor |
| Incomplete syntax / Binding | A9/A10 | Relevante Compilation-Fehler führen zu klassifiziertem Nicht-Complete; CS5001 allein als Packaging-Fehler ausgenommen; S3 | CLOSED | Ja: kein fehlerhafter Body als exact complete |
| External documentation | B21/B22/B25B | XML/cref mit originaler Main-/PE-/Worker-Bindung; XML ist kein bewiesener Body-Throw; S6/S8 | BOUNDED-CLOSED | Ja: Informationsebenen getrennt |
| Raw diagnostic graph | A10-Layerentscheidung | Unveränderter interner Build-Pfad unter Admission, keine neue Fassade und kein Production-Caller; S1/S9 | CLOSED | Ja: nicht als Production-Zertifikat nutzbar |
| Worker isolation | B3–B5/B21 | Echter eigener Prozess, owned lifetime/exchange/EOF; S6 | CLOSED | Ja |
| Historical Roslyn isolation | B2–B5 | Historical im Child; Main verwendet Current, Shared-Quellen gleich; S6/S7 | CLOSED | Ja |
| Worker Identity | B6/B21/R2 | Präzise Version-/Hash-/MVID-/Execution-Bindung, auch Failure nicht in Current-Erfolg umdeutbar; S6 | CLOSED | Ja |
| Attestation | B21 | Capability aus echter validierter Execution statt frei erzeugtem JSON; S6 | CLOSED | Ja |
| Main validation | B21/B25B | Result, Proven-/Uncertainty-/Truncation, Referenzen/Quellen/Pfade/Identität vor Receipt; S6 | CLOSED | Ja |
| Historical projection | B10–B15/A10 | Originale Quellen, Binder-/Owner-/Reference-Welt, begrenztes Eingabeprofil; S2/S6 | BOUNDED-CLOSED | Aktuelle Profile erweiterbar |
| Historical Finding Adoption | B25/B25B | Bounded Direct-/Summary-Paar vor gemeinsamer Main-Policy; S5/S8 | BOUNDED-CLOSED | Keine allgemeine Solution-Adoption |
| Current/Historical Semantic Contract | A10 | Gemeinsame A9/A10-/A5-Semantik, unabhängige Soll-Witnesses zusätzlich zur Parität; S7 | BOUNDED-CLOSED | API-/Coverage-Verfügbarkeit bleibt geprüft |
| Wire DTO | A9/B25B | Vorhandene Requests/Results genügen; Receipts Main-lokal | CLOSED | Keine neue DTO-Schicht derzeit nötig |
| SolutionTransitive bounded adoption | B25/B25B | Exact+complete+gebundene Receipts+zulässige finale Typen; S5/S8 | BOUNDED-CLOSED | Allgemeine Ausweitung offen, nicht implizit aktiviert |
| Reporting Root | B25B | Genau ein ausgewählter externer Root; Supporting nicht Reporting; S5 | BOUNDED-CLOSED | Adapter-Scope, kein Limit normaler Solution-CLI |
| Zweiprojektkette | B25/B25B | Echte Current-/Historical-/CLI-Witnesses W1–W6; S7 | BOUNDED-CLOSED | Genau belegtes Profil |
| Dreiprojektkette | B25/B25B | Echte Kette, eindeutige Root-Referenzwelt für finale Typen; S5–S7 | BOUNDED-CLOSED | P2-Body erreichbar ≠ jeder P2-Typ projizierbar |
| Exact/complete gate | A9/A10/B25B | Admission, Evaluation, Projection und Receipts müssen gemeinsam passen; S1/S5/S6 | CLOSED | Ja |
| OUTSIDE handling | A10/B25B | Klassifiziert fail closed, kein normales exact Finding-Output aus Partial; S1/S8 | CLOSED | Ja |
| Incomplete handling | A10/B25B | Unknown/fehlende Coverage/Truncation nicht als vollständig admitted; S1/S6 | CLOSED | Ja |
| Uncertainty-/Diagnostic-Findings | A9/A10 | DOC631/diagnostische Untergrenzen getrennt von exaktem DOC611/632; S8 | BOUNDED-CLOSED | Kein completeness claim |
| Abdeckung im normalen CLI-Gesamtergebnis | A10/Produktreview | RunResult enthält Findings/Zeiten, keinen aggregierten ProfileOutcome; DOC631 hängt von passenden Tags ab; S8 | OPEN | Nein: echte UX-/Ergebnisvertragslücke |
| Finding cardinality | B25/A10/B25B | Distinkte Exception-Identitäten bleiben getrennt; nicht ein Finding pro Throw-Pfad; S2/S8 | CLOSED | Ja |
| Finding ownership | B25/B25B | Owner-qualified Typen, Supporting als Evidence; nur Reporting-Root als Meldestelle; S5/S8 | CLOSED | Ja |
| Finding location | B25/B25B | Root-Member-/Tag-Anchor, Throw-Pfad als Evidence; Console/JSON/SARIF bleiben Main-Policy; S8 | BOUNDED-CLOSED | Kein neues SourceProject-Feld versprochen |
| Deduplication | B25/A10/B25B | Vollständige Identität statt Name/Collection-Position; owner-distinkte Findings bleiben erhalten; S2/S8 | CLOSED | Ja |
| Historischer globaler „Solution Findings geschlossen“-Guard | B25/A10 | Durch gezielte bounded B25B-Aktivierung ersetzt; alte Berichte bleiben historisch | OBSOLETE | Nicht mehr heutige allgemeine Statusaussage |
| Historisches „GraphKey definition-only genügt“ | A3 | Durch exakte admitted Context-Identität ersetzt | OBSOLETE | Fehlercharakterisierung bleibt historische Evidence |

Quellanker (aktuelle Produktion, nicht nur Tests):

- **S1:** [Session.Analyze](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryAnalysisSession.cs), [Graph-Transaktion, RegisterTarget, GetOrAdd](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraph.cs), [Raw-Build](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryAnalysisSession.GraphConstruction.cs).
- **S2:** [kanonische Identitäten](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/Canonical/), [InvocationSubstitution](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowInvocationSubstitution.cs), [CallContext](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.CallContext.cs).
- **S3:** [FiniteOrbitAdmission](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowFiniteOrbitAdmission.cs), [FiniteOrbitProfile](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowFiniteOrbitProfile.cs).
- **S4:** [SummaryGraphEvaluator](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs), [A5-Vertrag/Beweise](P5O2B25A5-recursive-summary-scc-closure.md).
- **S5:** [HistoricalSolutionFindingOperation](../src/XMLDocNormalizer/Execution/Analysis/HistoricalSolutionFindingOperation.cs), [SolutionProjection](../src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerSolutionProjection.cs), [Receipt-Vertrag](P5O2B25BProof/Receipt-Contract.md).
- **S6:** [WorkerClient](../src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs), [ResponseValidation](../src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs), [PayloadProjection](../src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs), [DocumentationProjection](../src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerDeclaredDocumentationProjection.cs).
- **S7:** [B25B Witness-Index](P5O2B25BProof/Witness-Index.md), [echte Adoption-Tests](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionAdoptionTests.cs), [Dual-Audit](P5O2B25BProof/Results/Dual/audit.json).
- **S8:** [normaler Detector](../src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs), [gemeinsame Finding-Policy](../src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.Findings.cs), [externe Finding-Adoption](../src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs), [RunResult](../src/XMLDocNormalizer/Models/RunResult.cs), [Program.Main](../src/XMLDocNormalizer/Program.cs).
- **S9:** Suchaudit zu `TryBuildTransitiveSummaryGraph`: außerhalb seiner Deklaration nur Testhelper `ExceptionFlowSummaryGraphTestHelper`, `ExceptionFlowSummaryGraphProjectTestHelper`, `ExternalSupportingSourceExceptionFlowTestHelper`; kein anderer Produktionsaufruf unter `src`. Historical-Summary verwendet Session.Analyze; die echte Receipt-Paarprüfung akzeptiert keinen Raw Graph.

## 4. Der tatsächlich produktive B25B-Vertrag

Diese Grenzen gelten für **externe Historical-Solution-Finding-Adoption**.
Sie sind nicht pauschal Grenzen einer normalen lokalen `.sln`-Analyse.

### Admitted

1. Expliziter SolutionTransitive-Auftrag mit original validierten P4/P5-
   Provenienz-, Source-, Parse-, Reference- und vollständigen lokalen
   Scope-Handoffs für jedes Projekt. Kein aus Ergebniswerten nachgebauter
   Vertrauenskontext und keine implizite Projektentdeckung.
2. Ein ausgewählter Reporting-Root in P0 und genau ein oder zwei Supporting-
   Projekte: eine eindeutige, erreichbare, azyklische Kette P0→P1 bzw.
   P0→P1→P2. `TryPrepare` prüft zusätzlich zum allgemeineren Solution-
   Projektor höchstens eine Abhängigkeit je Knoten, einen Leaf und keine
   mehrfach verwendete Dependency. Keine Branches/Diamonds.
3. Der bestehende externe Selector verlangt eine definierte gewöhnliche
   statische parameterlose nongenerische Methode in einem nongenerischen
   Containing-Typ. Das ist enger als die intern analysierbaren generischen
   Callees und enger als normale lokale Workspace-Analyse.
4. Vorhandenes Historical-Eingabeprofil bleibt erhalten: validierte originale
   Quell-/Referenzwelt, unterstützte Parse-Konfiguration, ein PE-Modul, kein
   Reference-Assembly-Target; derzeit unter anderem unsigned, Debug,
   AnyCpu/DLL, Nullable enable, kein unsafe/overflow-check. Die tatsächlichen
   Guards in Payload-/Parse-/Scope-Projektion sind verbindlich; B25B erweitert
   sie nicht zu beliebigen Buildoptionen oder Compiler-Versionen.
5. Summary über `Session.Analyze`: A9/A10 EXACT, finite konkrete Contexts,
   vollständige Coverage, exakte Substitution/Catches/Source-/Resultprojection,
   kein relevanter Unknown Residual, keine Uncertainty/Truncation. A5-safe
   SCC-/Cache-Auswertung bleibt Voraussetzung.
6. Finale Exception-/Doc-Typen müssen vollständig canonical und nongenerisch
   in der eindeutigen originalen Reporting-PE-Typwelt auflösbar sein.
   Validierter Supporting-PE-Owner hilft nur bei exakter originaler
   Root-Referenz-/Graphbindung. Eine erreichbare P2-Exception ist nicht allein
   wegen der Kette automatisch im Root projizierbar. Endliche generische
   Zwischenaufrufe/Catches sind möglich; generische finale Findings dadurch
   nicht freigegeben.
7. Separater echter Direct-/Dokument-Request und echter Summary-Request,
   unveränderte Original-Snapshots, derselbe validierte physische Worker,
   einmalige Tickets, passende Root-/Scope-/Graph-/Request-/Execution-Bindung.
   Main validiert, mintet Receipts und konsumiert das Paar genau einmal.
8. Erst dann ruft `FindSolutionExternalExceptionSmellsAsync` die gemeinsame
   Main-Finding-Policy auf. Korrekt dokumentierte/empty/inheritdoc-Fälle
   müssen nicht positiv melden. P0 meldet; P1/P2 bleiben Supporting.

Ein trusted No-doc-Guard darf nach Input-/Direct-/Dokumentvalidierung leer
zurückgeben, ohne Summary auszuführen. Das ist keine Aussage „Summary exact
complete“. Externe CLI-Ausführung ist ein expliziter selected-root-
Exception-Auftrag, kein Ersatz für sämtliche DOC-Regeln eines Workspace-Laufs.

### Rejected / fail closed

OUTSIDE, Compilation-/Coverage-Fehler, fehlende RuntimeAwait-Information,
Unknown Residual, incomplete/truncated Results, offene oder nicht exakt
auflösbare finale Typen, unvalidierte oder manipulierte XML-/cref-Binding-Evidence, fehlende originale
Materialien, unstimmige Scope-/Reference-/Graph-/Execution-Bindung und
Worker-/Versions-/Attestation-Fehler berechtigen nicht zu normaler exakter
Solution-Finding-Adoption. Ein Clone, frei deserialisiertes JSON oder ein
gleiches Ergebnis aus einer anderen echten Execution besitzt keine Receipt-
Authority. Alte unvollständige CLI-Jobs bleiben negativ. Kein Current-Fallback,
kein Force-Historical und kein Definition Folding bei wachsender Rekursion.

Ein fachlich ungültiger `cref` im originalen dokumentierten Input ist davon
zu unterscheiden: Die normale Policy kann dafür DOC660 und gegebenenfalls
weitere zulässige Findings erzeugen. B25B verlangt eine gültige Bindung der
Beobachtung an ihren Ursprung, nicht ausschließlich fehlerfreie Dokumentation.

Unabhängige Syntaxdiagnostik, DOC660/670 oder zulässige diagnostische Hinweise
sind damit nicht pauschal verboten. Sie sind aber kein exact-complete
Summary-/Finding-Zertifikat. Externe XML-Dokumentation ist eine gebundene
Dokumentationsbehauptung; sie darf unbekannte Body-Semantik nicht in einen
bewiesenen Throw verwandeln.

## 5. Bounds: Produktlücke, Erweiterung oder Grenze?

Kategorien: **A** realistische Produktlücke, **B** optionale Forschungs-/
Engineering-Erweiterung, **C** bewusste mathematische/semantische Grenze,
**D** künstliche Implementierungs-/Test-Scope-Grenze. Eine Zeile nennt einen
primären Typ; der Zusatz erklärt ihre mögliche praktische Bedeutung.

| Grenze | Kategorie | Bewertung / Konsequenz |
|---|---|---|
| Mehr als drei externe Source-Projekte | D | Nicht mathematisch begründet. Praktisch A bei realem Historical-Solution-Einsatz; erst Corpus-Nachfrage und generalisierte Receipt-/Scope-Bindung belegen. Normaler Current-Workspace hat diesen Cap nicht. |
| Mehrere externe Reporting Roots | D | Adapter-/Operation-Scope. Ein Root pro Operation ist sinnvoll, eine sichere Batch-Orchestrierung wäre ein eigener Vertrag. Normale lokale `--full`-Analyse ist nicht dadurch verboten. |
| Allgemeine generische finale Historical-Findings | A | Reale Bibliotheken können constructed/nested Exceptions besitzen. A10 kann endliche konkrete Semantik intern, der externe PE-/Dokumentadapter ist enger. Sinnvolle spätere finite Erweiterung, kein Anlass für einen allgemeinen Family-Solver. |
| Jeder OUTSIDE-Fall als exact zu behandeln | C | Fail closed bleibt zwingend; allgemeine korrelierte Exactness nicht versprechbar. Kein Produktbug allein durch Ablehnung. |
| Häufige unterstützbare OUTSIDE-Unterfälle, etwa fehlende RuntimeAwait-Coverage | A | Coverage-Lücke für reale C#-Nutzung. Ein einzelner einfacher Fall kann entscheidbar sein; v1-Ablehnung ist keine individuelle Unentscheidbarkeitsbehauptung. Zuerst Häufigkeit sichtbar machen, dann begrenzten Capability-Vertrag planen. |
| Größere externe Solution-DAGs/diamonds | D | Kettenbeschränkung bewusst implementiert; Multi-Path-Ownership und graphgebundene Receipts benötigen zusätzliche Beweise. Keine pauschale Empfehlung zum Entfernen. |
| Mehrere externe Assemblies | A | Nicht generell verboten: originale Referenzsets und PE-Dependencies existieren bereits. Die Lücke ist umfassende Source-Rekonstruktion/Typ-Projektion mehrerer relevanter externer Assemblies außerhalb der belegten Welten. Nicht auf eine erfundene Ein-Assembly-Grenze reduzieren. |
| Weitere Historical Compiler-Versionen | B | Exakte gepinnte Version statt beliebiger Compiler ist ein sinnvoller Trust-Vertrag. Weitere Version nur für reale Artefakte nötig; kein Versions-Fallback. |
| Weitere Historical Build-/Signing-/Selector-Profile | D | Bestehende unsigned/Debug/static/parameterless-Grenzen sind Profile, keine allgemeine Roslyn-Unmöglichkeit. Praktischer Nutzen erst durch reale Job-Coverage quantifizieren. |
| Weitere Workspace-/Solution-Inputformen | A | `.cs`/Ordner haben keine volle Semantik; `.csproj`/`.sln` besitzen sie. Allgemeine Repository-/MSBuild-/Generator-/linked-source-Discovery und andere Solution-Formate nicht aus B25B ableiten. Bedienung und Scope-Dokumentation zuerst. |
| Mehrere Module / unvollständige Provenienz | C | Unter heutigem Profil Ablehnung korrekt; Multimodule selbst ist kein allgemeines mathematisches Hindernis. Unterstützung wäre optionale Engineering-Arbeit, fehlende Identität darf nie geraten werden. |
| Allgemeine korrelierte rekursive Familien | C | A8-Grenze für totale exakte Semantik; Forschungsprofile/konservative Residuen B, nicht zwingende Produktvoraussetzung. |

Es gibt somit echte Coverage-Lücken, aber keinen Nachweis, dass das
ursprüngliche bounded B25-Ziel noch eine weitere semantische Closure benötigt.
Größere externe DAGs oder generische finale Typen sind mögliche spätere
Produkterweiterungen, keine automatisch nächste Paketnummer.

## 6. CLI-/Benutzerperspektive

Quelle: [ToolRunner](../src/XMLDocNormalizer/Execution/ToolRunner.cs),
[ProjectClosureSemanticContextBuilder](../src/XMLDocNormalizer/Execution/Semantic/ProjectClosureSemanticContextBuilder.cs),
[Context](../src/XMLDocNormalizer/Execution/Semantic/ProjectClosureSemanticContext.cs),
[Benutzer-README](../README.md).
Die vier Modi ändern Exception-Semantik, nicht pauschal alle DOC-Regeln.

| Eingabe | Direct | ProjectTransitive | SolutionTransitive | ProjectTransitiveDeclaredExceptions |
|---|---|---|---|---|
| Einzelne `.cs` | Syntax-/XML-Checks; keine volle semantische Direct-Throw-Prüfung | Keine Workspace-Summary; Modus macht Einzeldatei nicht zu Projekt | Keine Solution-Closure | Keine semantische Declared-Exception-Summary |
| Ordner | Syntax-/XML-Checks über gefundene Dateien | Kein implizites Projektladen | Keine implizite Solution-Discovery | Keine implizite Declared-Typwelt |
| `.csproj` | Direkte lokale Throw-Analyse dokumentierter Member; keine transitive Vollständigkeitsbehauptung | Context der ausgewählten Reporting-Projekte; Summary muss A9/A10 erfüllen | Source-ProjectReference-Closure im geladenen Workspace; Supporting kann beitragen | Summary im Projektscope, relevante Exception-Typen aus Reporting-Scope; kein allgemeiner Solution-Modus |
| `.sln` | Direct für gewählte Reporting-Projekte | Summary über den aufgebauten Reporting-Projektscope | Referenzclosure der Reporting-Projekte; kein B25B-3-Projekte-/Ein-Root-Cap | Reporting-Scope und Declared-Filter, nicht beliebige externe dokumentierte Exceptions |

Bei `.sln` analysiert die CLI standardmäßig das zum Solutionnamen passende
Projekt, alternativ `--project`, mit `--full` alle ausgewählten Projekte.
ProjectTransitive wird aus dem gewählten Reporting-Projektscope aufgebaut;
„Project“ sollte insbesondere bei `--full` nicht ohne Prüfung als völlig
unabhängige, jeweils einzeln isolierte Projektanalyse beschrieben werden.
Generierte Dateien/Testprojekte werden standardmäßig gefiltert. Vollständiger
Run bedeutet daher zunächst vollständiger gewählter Scope, nicht jede Datei
im Repository.

**Korrektheit:** A9/A10 verhindert exakte Behauptungen bei nicht zugelassener
Summary. Keine passende `<exception>`-Dokumentation heißt aber nicht automatisch
Summary-Coverage. Member ohne XML-Dokumentation werden im Exception-Detector
übersprungen; andere DOC-Regeln können fehlende Dokumentation separat melden.
Direct bleibt eine lokale Beobachtung. DOC630 ist entsprechend ein Hinweis
auf fehlenden direkt erkannten Throw, kein globaler Beweis unnötiger
Exception-Dokumentation. Declared-Modus filtert bewusst auf Reporting-Typen.

**CLI-Unterstützung:** Datei/Ordner sind unterstützte Syntaxeingaben, keine
vollwertigen semantischen Projekte. Externe Jobs sind ein separater expliziter
selected-root-Pfad mit eigenem validiertem Materialprofil. W6 belegt echte
Console/JSON/SARIF-Ausgabe dafür, keine universelle Historical-Solution-CLI.

**Diagnose/UX – wichtige OPEN-Lücke:** Im normalen Detector-Zweig
`flowResult.ProfileOutcome is { Complete: false }` gilt
`allowExactFindings:false`. DOC611/632 werden korrekt nicht als exact
ausgegeben. DOC631 entsteht jedoch beim Durchlaufen passender dokumentierter
Exception-Tags. Ohne solche Tags kann trotz OUTSIDE überhaupt kein
Exception-Finding entstehen. `RunResult` aggregiert keinen ProfileOutcome;
`Program.Main` entscheidet den normalen Exit anhand `FindingCount`.
Ein ansonsten regelkonformer Input kann deshalb Exit 0 haben, ohne dass die
transitive Exception-Analyse vollständig war. Das ist ein statisch belegter
Pfadbefund, kein in diesem Review ausgeführter neuer Witness und keine
Behauptung, jedes OUTSIDE erzeuge Exit 0.

Fehlende Compilation wird in ToolRunner teilweise mit Warnung übersprungen;
fehlende Dateien/Trees werden ebenfalls übersprungen. Auch diese Fälle sind
nicht als aggregierte Coverage im RunResult modelliert. Externe Jobs scheitern
deutlicher über ExternalTargetFailure. Für normale Runs fehlen dagegen eine
durchgängige Abdeckungsbilanz und eine klare maschinenlesbare Trennung
„keine Findings“/„vollständig analysiert“.

**Dokumentation:** Benutzer- und Test-README existieren bereits. Die
Benutzer-README erklärt Eingabearten, Modi und Projektauswahl, aber nicht
ausreichend A9/A10-Coverage, OUTSIDE, neue externe bounded Findings und
Receipt-/No-doc-Scope. Der historische
[Boundary-Tracker](OPEN-PIPELINE-BOUNDARIES.md) enthält inzwischen überholte
allgemeine STOP-/Readiness-Aussagen. Er ist hier unverändert; ein zukünftiger
aktueller Statusindex muss auf Closure verweisen statt Geschichte umschreiben.

**Performance:** Unterstützte Eingabe heißt nicht akzeptable Laufzeit.
Vergleichsmodus und `--full` haben andere Arbeitsmengen als selected-root
Historical-Jobs; Zeiten dürfen nicht zwischen diesen Pfaden übertragen werden.

## 7. DOC-Landschaft und Entwicklungsgewicht

[XmlDocSmells](../src/XMLDocNormalizer/Models/XmlDocSmells.cs) enthält statisch
78 definierte IDs, davon zwölf im DOC6xx-Bereich. Der tatsächliche Katalog
geht über die im Auftrag genannte Obergrenze DOC950 hinaus bis DOC981.

| Familie | Definierte IDs | Gegenstand / aktueller Reviewbefund |
|---|---:|---|
| DOC1xx | 7 | Namespace-/Member-Dokumentation, XML-/Tag-Syntax und Reihenfolge |
| DOC2xx | 5 | Summary/Remarks vorhanden, sinnvoller Inhalt, Duplikate |
| DOC3xx | 10 | Param-/paramref-Vollständigkeit, Namen, Attribute und Reihenfolge |
| DOC4xx | 10 | Typeparam-/typeparamref-Verträge |
| DOC5xx | 6 | Returns und passende Member-Art |
| DOC6xx | 12 | Exception-Dokumentation, lokale/transitive Beobachtung, Unsicherheit, cref und Duplikate |
| DOC7xx | 7 | Inheritdoc-Auflösung, Quelle, Konflikte und Redundanz |
| DOC8xx | 5 | Value-Dokumentation und Property-/Indexer-Scope |
| DOC9xx | 16 | See/seealso, Zielattribute, cref/href/langword, Inhalt/Nesting/Duplikate |

Methodisch ausreichend geschlossen sind jetzt die untersuchten
assembly-/construction-qualifizierten DOC610/611-/632-Entscheidungen im
admitted endlichen Bereich und ihre bounded Historical-Adoption. DOC631
bleibt ausdrücklich Unvollständigkeitsdiagnose; DOC630 ist Direct-begrenzt.
DOC600/620/650/660/670/680 und die anderen Dokumentationsregeln werden nicht
durch einen erfolgreichen Graphsolver ersetzt. Inheritdoc und gebundene
externe Dokumentation beeinflussen bewusst die Finding-Policy.

Bekannte verbleibende fachliche Coverage-Lücken betreffen insbesondere
fehlende RuntimeAwait-Coverage, allgemein nicht geschlossene Targets/Bodies,
nicht repräsentierbare Generic-Profile und engere externe finale Typ-/Input-
Profile. Bei ihnen ist fail closed korrekt; die fehlende Benutzersichtbarkeit
ist das eigenständige Produktproblem. Dieser Review hat **keinen neuen
konkreten Korrektheitsdefekt einer DOC1xx–5xx-/7xx–9xx-Regel nachgewiesen**.
Das ist kein pauschaler Closed-Beweis für diese Regeln: Es wurde weder ein
neues unabhängiges Label-Corpus noch ein vollständiger Rule-by-Rule-Oracle-
Audit durchgeführt. Eine erfundene wichtigere Regelbaustelle wäre unbegründet.

Statische Größen verdeutlichen die Konzentration: 391 Main-C#-Dateien,
davon 114 im Flow-Verzeichnis; 348 Test-C#-Dateien, davon 35 unter Worker.
Dateianzahlen sind kein Aufwand-/Qualitätsmaß. Trotzdem ist der lange
ExceptionFlow-/Historical-Entwicklungspfad gegenüber zwölf von 78 IDs
auffällig groß. Die bisherige Arbeit war wegen echter unsounder Owner-/
Cache-Befunde erforderlich. **Eine weitere automatische Scope-Erweiterung
wäre jetzt unverhältnismäßig**, solange Nutzerabdeckung, Laufzeit und
unabhängige Evaluation des ganzen Werkzeugs nicht vergleichbar sichtbar sind.
Außerhalb ExceptionFlow sind Eingabe-/Coverage-Kommunikation, Laufzeit,
Reproduktionsanleitung und Runtime-Stabilität die besser belegten Probleme.

## 8. Performance: alte Messwerte, neue Risiken, Priorität

| Historisch im Auftrag genannte Größenordnung | Einordnung am heutigen HEAD |
|---|---|
| Direct ca. 7,5 s | Kein aktueller Reproduktionslauf; lokale Semantik/Arbeitsmenge unterscheidet sich von Summary |
| SolutionTransitive ca. 108 s | Nicht als heutiger repräsentativer End-to-End-Wert belegt |
| ProjectTransitive ca. 420 s | Weiterhin starkes Prioritätssignal; kein Beweis, dass heutiger HEAD dieselben Hotspots hat |
| Declared ca. 430 s | Ebenfalls neu mit identischer Coverage zu messen, kein aktueller Preis der engeren Typfilterung bewiesen |

Diese Zahlen stammen aus dem übergebenen historischen Kontext. Die im Review
gesichtete Closure-Evidence liefert keinen passenden aktuellen Benchmark mit
gleichem Corpus, Kommando, Hardware, Cachezustand, Coverage und Wiederholungen.
Test-/Build-Gate-Dauern ersetzen ihn nicht. Deshalb weder „noch genau so
langsam“ noch „durch A10 schneller“ behaupten. Frühes OUTSIDE könnte Arbeit
sparen und gleichzeitig weniger analysieren; ein solcher Lauf wäre keine
semantisch gleichwertige Beschleunigung. Dass Solution historisch schneller
als Project war, beweist allein ebenfalls keine vertauschten Modi oder einen
heutigen Algorithmusfehler.

Statisch plausible aktuelle Kostenstellen:

- `FiniteOrbitAdmission.Discover` baut für jeden analysierten Root ein
  Declaration-Inventar auf; `InspectDeclaration` fragt Compilation-Diagnostics
  ab. Scope-/Assembly-Inventarisierung und symbolische Körperanalyse kommen
  vor dem konkreten Graph hinzu. Roslyn kann intern cachen; die tatsächliche
  Kostenverteilung wurde hier nicht gemessen.
- Exakte constructed Contexts können einen großen, aber endlichen Orbit
  haben. „Finite“ ist weder klein noch polynomial. A5 verhindert unsounde
  Wiederverwendung; relevante SCC-Ancestor-Kontexte können kombinatorisch
  wachsen. Ein Key-only-Cache wäre keine zulässige Performance-Abkürzung.
- Die Session teilt den Graph über Roots, evaluiert Ergebnisse aber pro
  Analyse neu. Source-/Scope-Rebinding, Canonical-Serialisierung und
  Scope-Prüfungen sind Kandidaten für Messung, nicht bewiesene Engpässe.
- B25B serialisiert komplette Direct-/Summary-/Graph-Snapshots mit PE-Images
  mehrfach und startet getrennte echte Worker-Subqueries. Das ist ein
  zusätzlicher Historical-Zeit-/Speicherkandidat, **nicht** automatisch die
  Erklärung langsamer normaler ProjectTransitive-Läufe.
- Evidence-Captures vervielfachten große W4-Payloads. Die abgeschlossene
  Storage-Closure reduziert Repository-Evidence von 591,525 auf 195,4182 MiB
  (620.258.962 → 204.910.822 Bytes), verändert aber keinen produktiven
  Analyzer-Hotspot. Nicht als Analyzer-Speedup ausgeben.

Nächste Performance-Arbeit: vorhandenen Modusvergleich mit festem Corpus,
Build-/Runtime-/Cacheangaben, Laufzeit/Peak-Memory, Reporting-/Supporting-
Scope sowie admitted/OUTSIDE/incomplete Rootzahlen verbinden. Phasen
Workspace/Discovery/Graph/Evaluation/Projection/Worker unterscheiden. Erst
dann Project-Hotspots optimieren; A5, P1/P2 und Receipt-Bindung beibehalten.
Das ist sinnvoller als neue Generic-/DAG-Semantik, aber nach einem kleinen
sichtbaren Coverage-Vertrag belastbarer als sofortiges Optimieren alter Zahlen.

## 9. Architektur und Wartbarkeit

Physische Zeilen inklusive Kommentare, zusätzlich Nonblank-Zeilen; keine
semantischen SLOC und kein Vergleich unterschiedlicher Zählmethoden:

| Komponente | Dateien | Zeilen | Nonblank |
|---|---:|---:|---:|
| `ExceptionFlowAnalyzer.*.cs` | 29 | 14.770 | 13.454 |
| `ExceptionFlowSummaryAnalysisSession*.cs` | 2 | 240 | 218 |
| `ExceptionFlowFiniteOrbit*.cs` | 2 | 631 | 589 |
| `ExceptionFlowSummaryGraphEvaluator.cs` | 1 | 586 | 525 |

Größte Analyzer-Partials: Dispatch 1.482, Operators 1.217, Disposals 1.131,
DispatchCompleteness 844 und CollectionInitializers 777 Zeilen. Frühere
P5O2A-Zahlen sind historische andere Stände, kein heutiges Größeninventar.

Verantwortlichkeiten sind teilweise bereits sinnvoll getrennt: Session
orchestriert, Graph speichert/schedult, Admission entdeckt/versiegelt,
Evaluator verarbeitet endliche SCC-/Catch-/Projection-Semantik, Canonical
trägt neutrale Identitäten. Main orchestriert Selection/Worker/Validation;
die neue Finding-Operation verbindet Receipts. Der Analyzer selbst besitzt
weiter zahlreiche Syntax-/Dispatch-/Accessor-/Operator-/Disposal-/Framework-
Transferregeln und Context-Fact-Zusammenstellung. Partial-Dateien verteilen
Text, schaffen aber allein keine unabhängigen Verantwortungsgrenzen.

**TODO „ExceptionFlowAnalyzer zerlegen“: sinnvoll, aber nach Coverage- und
Performance-Baseline; kein akuter Beweis eines neuen Architektur-STOPs.**
P5O2A hat bereits echte Abhängigkeitszyklen/Fact-Owner bearbeitet. Ein erneutes
großes Rename-/Move-Refactoring ohne konkreten Hotspot oder Owner-Vertrag
wäre derzeit riskanter als ein gezielter Schnitt. Nicht heutige Partialzahl
mit einem unbehobenen historischen Analyzer/Session/Builder-Zyklus gleichsetzen.

Ein späterer Schnitt sollte Transferregel-Familien und Adaptersysteme
abgrenzen, während Session als einziger zertifizierter Entry, Admission-Seal,
A5-Cache und Main-only-Receipt-Authority zusammenhängend prüfbar bleiben.
Keine zweite Engine, kein nebenläufiges Teilen der heute sequenziellen Session
und keine abstrahierende Schicht, die Raw Graph als admitted deklarieren kann.

Historical erhöht die Wartungskosten: gepinnte getrennte Buildwelt,
physische Image-/Provenienzprüfung, zwei Subqueries, private Authority und
Original-PE-Projektion. Das ist eine begründete Trust-Grenze, kein Anlass,
Main und Historical Roslyn wieder zusammenzuladen. Testharness und
Evidenceharness sind ebenfalls komplex: echte Child-Prozesse, PIDs/Lifetimes,
SynchronizationContext, PE-/Stream-Lifetimes, isolierte Exporte, mehrfach
gesnapshottete Dateien und generationenübergreifende Hash-Gates. Die bekannten
Harness-Defekte belegen Kosten dieser Infrastruktur, keinen Analyzer-Fixbedarf.
Die neue content-addressierte W4-Ablage verbessert diesen Teil bereits.

## 10. Projekt-TODOs und GXL/SEE

| TODO | Bewertung nach B25B |
|---|---|
| ProjectTransitive Performance | Weiter hoch; neue coveragegleiche Messung und Profiling vor Optimierung. Zweites priorisiertes Paket, keine Scope-Erweiterung als Ersatz. |
| Tool-Umbenennung | Produkt-/Release-/Artifact-Konsistenz, keine semantische Blockade. Namen und Migrationsstrategie vor finaler Publikation/GXL festlegen; nicht mit heißem Algorithmus-Refactoring vermischen. |
| Benutzer-/CLI-README | Existiert. Aktualisierung statt Neubau: Coverage, Inputs/Modes, externe Jobprofile, exact vs incomplete, Exit-/Reportaussage und Beispiele. Mit Produktvertrag koppeln. |
| Test-README | Existiert, erklärt Standard-/SelfAnalysis-Filter. Ergänzen um sauberen Build, Dual-/Historical-Voraussetzungen, Offline-/Fixture-Inputs, Evidence-Rehydration und echte versus wiederverwendete Gates. `--no-build` allein ist keine Fresh-Checkout-Anleitung. |
| ExceptionFlowAnalyzer zerlegen | Gezielter Folgeschritt nach Baselines, kein blindes großes Refactoring vor allen Produktfragen. |
| SEE-GXL / Issue #1000 | B25B verbessert fachliche Grundlage erheblich. Noch kein GO für Implementierung: Benennung, stabiler Domain-Exportvertrag, Provenienz-/Coverage-Darstellung und Nutzungsziel fehlen. |

[Issue #1000](https://github.com/uni-bremen-agst/SEE/issues/1000) wurde im Review
über den GitHub-Connector read-only gelesen. Es ist offen und bezeichnet die
Integration als FUTURE / DESIGN ONLY; Analyzer-Stabilität und Umbenennung
sind Voraussetzungen. Es verlangt einen stabilen Domain-Export, nicht einen
Dump von Summary-/Cache-/Worker-DTOs: Project→Namespace→Type→Member,
Contains/Calls/Overrides-Implements/Throws/Propagates, normale CodeCity-
Nutzung auch ohne Exception-Overlay, kompakte relevante externe Satelliten,
keine Unity-Abhängigkeit im Analyzer. Die dort erwähnte DataModel-Auslagerung
#998 ist vor Umsetzung neu zu bewerten; ihr heutiger Abschluss wird hier
nicht behauptet.

B25B liefert korrekte Owner und gebundene Flows als gute Grundlage. Ein GXL-
Schema muss aber Diagnostik/incomplete von exact trennen, Reporting/Supporting
erhalten und Root-/Source-/Location-/Provenienzbeziehungen stabil ausdrücken.
GUI/Export darf Admission nicht neu implementieren. Designvorbereitung ist
möglich; **READY FOR GXL INTEGRATION PREPARATION ist noch nicht die
Hauptempfehlung** und kein stillschweigender Implementierungsauftrag.

## 11. Runtime-Stabilität: eigenständiges offenes Engineering

**OPEN – recurrent, unattributed process failure.**

Frühere `0xe0434352`-/`0xC0000005`-Beobachtungen und der weitere
dotnet-/Runtime-Prozessfehler während B25B bleiben dokumentiert. Aus Codes
allein folgt keine konkrete Ursache: `0xe0434352` ist ein CLR-Exception-Code,
nicht automatisch derselbe Fehler wie eine Access Violation. Keine kausale
Zuordnung zu B25B, kein Flakiness-Etikett und kein „behoben“ aufgrund später
grüner Tests. Auch PID-G1 und die getrennten Lifetime-/Scheduling-Harness-
Fixes erklären diese Prozessfehler nicht.

Eigenständige Untersuchung mit Prozess-/Runtime-/SDK-/Command-/Parent-/
Exit-/Zeitkontext und separat autorisierter Dump-Erfassung für dotnet.exe,
testhost.exe, vstest.console.exe, MSBuild.exe, VBCSCompiler.exe ist sinnvoll.
In diesem Review wurde nichts konfiguriert oder reproduziert.

Kein globaler Blocker für statische Produktarbeit, Dokumentation oder
isoliert geplante Entwicklung. Er blockiert aber belastbare Aussagen über
unbeaufsichtigte Stabilität und kann empirische Laufzeit-/Recall-Vergleiche
verzerren. Ein erneuter Prozessfehler in künftigen entscheidenden Gates ist
als fehlgeschlagener/unvollständiger Lauf zu erhalten und zu untersuchen,
nicht durch Grün-Retry wegzuwerten. Dumpanalyse kann parallel zur Produkt-
arbeit laufen; ihre Baselines/Artefakte müssen getrennt bleiben.

## 12. Wissenschaftliche Reife

| Ziel | Bereits tragfähig | Vor belastbarer Nutzung noch nötig |
|---|---|---|
| Methodikbeschreibung | Root/Supporting-Ownership, kanonische Konstruktion, SCC-Soundness, decidable Admission, klassifiziertes OUTSIDE, Execution-Receipts | Modell/Query-Scope ausdrücklich nennen; „exact“ nicht zu ganz C# oder jeder externen Bibliothek verallgemeinern |
| Reproduktion | Gepinnte Versionen, Original-Inputs/Hashes, echte Worker-Witnesses, rote/anschließend grüne Evidence, portable Evidence-Storage-Prüfung | Eine zusammenhängende Fresh-Checkout-Anleitung; Umgebung/Feeds/Fixtures, SelfAnalysis-Ausschluss und historische versus frische Gates kennzeichnen |
| Evaluation | Viele gezielte unabhängige Soll-Witnesses und echte Historical-E2E, negative Bindungs-/Replay-Fälle | Repräsentatives unabhängiges Corpus, manuelle Labels, Definitionsentscheidungen pro Finding-Familie, Coverage-Denominator, Zeit/Memory und Fehlerquote |
| LLM-Agentenvergleich | Reproduzierbare konkrete Findings/Flows, Scope und Provenienz als maschinenlesbare Grundlage | Gleiches Input-/Build-/Dokumentwissen und Budget; Precision/Recall plus Abstention/Coverage getrennt; kein „kein Finding“ als True Negative bei OUTSIDE; Agent-/Modell-/Prompt-Versionen fixieren |
| GXL/SEE | Owner-/Flow-Identitäten und Reporting/Supporting stabiler als vor B25 | Domain-Exportdesign, Coverage-/Uncertainty-Visualisierung, Rename und Issue-Voraussetzungen; Usability-/Erklärungsstudie separat vom Analyzer-Recall |

Zwingend vor **belastbaren empirischen Leistungs-/Qualitätsbehauptungen**:
Coverage und Ausschlüsse zählen; fehlgeschlagene/abgebrochene Prozesse
transparent erfassen; deterministische Inputs/Versionen/Commands dokumentieren;
unabhängige Ground Truth und Modusvergleich festlegen. Runtime-Ursache muss
für jede Vorstudie nicht zwingend schon vollständig gelöst sein, aber eine
bestätigende Stabilitäts-/Performance-Studie braucht kontrollierte Fehlerdaten
und darf solche Läufe nicht still aus der Auswertung entfernen.

Als Scope/Threat deklarierbar: v1 statt allgemeiner Rekursion, Ketten-
Historical-Profile, nongenerische finale externe Typen, ein externer Root,
gepinntes Compilerpaar, ausgewählte Inputs und keine vollständige externe
Buildrekonstruktion. Nicht vorher alle DAGs/Compiler/Generic-Familien lösen.
Current/Historical-Parität ist wertvolle zusätzliche Evidence, aber die
gemeinsame B25-Fehlergeschichte verbietet sie als einziges Oracle.
A8 enthält einen Repository-internen Reduktionsbeweis mit separaten endlichen
Checks, keine extern formal verifizierte universelle Analyzer-Korrektheit.

## 13. Vorhandene Validierung: geerbt, nicht erneut ausgeführt

| Abschluss-Evidence | Dokumentierter Stand |
|---|---|
| Neue B25B-Witnesses | 20/20; W1–W6, unabhängige Current-Solls plus echte Historical-/CLI-Parität |
| Gezielte Gesamtsuite | 1.428/1.428 |
| Broad | 3.358/3.358; nach autorisiertem Scheduling-Harness-Fix erneut grün |
| Boundary | 596/596 |
| Full | Erst 3.448/3.449 erhalten; nach isoliertem Harness-Fix 3.449/3.449 |
| Dual-Version | 800/800, genaue Current-/Historical-Identitäten und Shared-Quellen geprüft |
| Warning-as-error | Elf Schritte grün |
| Current-Baselines | Acht grün |
| A6–A10/B25B-Gate | 528 Checks grün, einschließlich A6-/A5-/Generic-/SCC-/Catch-/Projektklassen |
| Implementierungsschutz | 763 Checks grün am damaligen Abschlussstand |
| Evidence-Storage-Closure | Gespeicherter finaler Audit: 2.046 Prüfungen grün; portable Verifikation separat vorgesehen, keine Produkt-Neuausführung |

Quellen: [B25B-Bericht](P5O2B25B-bounded-solution-finding-adoption.md),
[Gate-Audit](P5O2B25BProof/Results/b25b-gates.json),
[Implementierungs-/Laufaudit](P5O2B25BProof/implementation-audit.json),
[Storage-Closure](P5O2B25BProof/EvidenceClosure/README.md),
[gespeicherter Storage-Schutzcheck](P5O2B25BProof/EvidenceClosure/verification-final.json).
Die hier aufgeführten Zahlen sind historische Nachweise am abgeschlossenen
Code, keine heute gestarteten Gates. Der frühere A10-Full 3.428/3.429 und
sein späterer 3.429/3.429 bleiben eigene historische Harness-Lifetime-Evidence.

Die acht Current-Baselines sind **kein neuer vollständiger SelfAnalysis-Lauf**
und **kein neuer globaler Canonical Finding Diff**. Frühere SelfAnalysis16/
Canonical0/0/0-Angaben gehören zu damaligen Ständen. Full schließt laut
Test-README die gesondert markierte SelfAnalysis standardmäßig aus. Für eine
aktuelle reale Produktevaluation darf keine dieser Zahlen umetikettiert werden.

## 14. Priorität: maximal fünf nächste Arbeitspakete

Keine dieser Empfehlungen wird in diesem Review implementiert.

| Rang / Paket | Ziel und Warum jetzt? | Risiko / Umfang | Voraussetzung | Codex-heavy? | Parallel/unabhängig? |
|---|---|---|---|---|---|
| 1 – Sichtbare Analyseabdeckung und CLI-Vertrag | „Keine Findings“ von „Exception-Analyse vollständig“ unterscheidbar machen; OUTSIDE/incomplete/skipped und gewählten Scope konsistent in Result/Report/CLI dokumentieren. Benutzer-/Testanleitung und aktueller Statusindex dazu konsolidieren. Schließt die konkret belegte Produktlücke ohne neue Flow-Semantik. | Mittel: Report-/Exit-Kompatibilität und neue Resultanforderungen müssen bewusst entschieden werden. Umfang mittel. | Kleiner expliziter Produktvertrag vor Umsetzung; keine vorschnelle neue Finding-ID oder Worker-DTO-Annahme. | Ja, bei abgestimmtem Vertrag; Benutzerentscheidung über Kompatibilität nötig. | Dokumentationsinventar unabhängig; Result-/Reporteränderungen mit Performance-Instrumentierung koordinieren. |
| 2 – Coveragegleiche ProjectTransitive-Performance | Aktuelle vier Modi auf festem realistischem Corpus messen, Phasen/Memory erklären, nachgewiesene Project-Hotspots optimieren. Höherer Nutzen als zusätzliche Historical-Scope-Aktivierung. | Mittel bis hoch: unsounder Cache oder weniger Coverage als falscher Speedup. Umfang mittel für Messung, Optimierung gegebenenfalls groß. | Gleiche Coverage/Inputs und Phase-1-Vertrag oder äquivalentes explizites Messinventar; Runtime-Fehler sauber erfassen. | Ja für Instrumentierung/Analyse; Profilinterpretation gemeinsam prüfen. | Messdesign parallel zu 1; keine gleichzeitige Veränderung der gemessenen Analyzer-Baseline. |
| 3 – Runtime-Prozessfehler untersuchen | Unattribuierte wiederkehrende Abbrüche mit Prozesskontext/Dumps eingrenzen; Studien-/CI-Zuverlässigkeit absichern. | Ursachen-/Reproduktionsunsicherheit; Dumps können sensible Daten enthalten. Umfang mittel, ergebnisoffen. | Separater autorisierter Erfassungsplan und kontrollierte Umgebung; keine Crash-Provozierung in Produktgates. | Teilweise; menschliche Debugger-/Umgebungsarbeit wahrscheinlich. | Weitgehend unabhängig von 1/4/5; entscheidende Messläufe aus 2 nicht durch Experimente kontaminieren. |
| 4 – Gezielte Analyzer-/Harness-Verantwortlichkeiten trennen | Nach Profiling ein bis zwei große Transferregel-Familien mit klaren Dependencies aus Analyzer herauslösen; Session/Seal/A5/Receipt-Grenzen erhalten. Evidence-Capture-Verträge dokumentieren statt neue Gatekopien wachsen lassen. | Hoch bei zu großem Schnitt; Umfang mittel bis groß. Kein pauschaler Neuaufbau. | Stabile Vergleichs-/Coverage-Baseline und konkreter Dependency-/Hotspotbefund; bestehende historische Evidence bleibt unverändert. | Ja. | Plan unabhängig; produktiver Diff erst nach 2 oder auf klar abgegrenztem Bereich. |
| 5 – Unabhängiges Evaluations-/LLM-Vergleichsprotokoll | Repräsentatives Corpus für das gesamte DOC-Spektrum, manuelle Labels, Scope/Abstention, Laufzeit und Agent-Budgets festlegen. Entwicklungsaufwand an realem Nutzen statt Paketnummern ausrichten. | Labelbias, Kosten, Daten-/Versionsdrift; Umfang mittel für Pilot, spätere Studie groß. | Ergebnis-/Coverage-Vertrag, reproduzierbare Anleitung; kontrollierter Umgang mit offenen Prozessfehlern. Rename vor finalen öffentlichen Artefakten festlegen. | Gemischt: Harness Codex-heavy, Ground Truth/Studienentscheidung menschlich. | Corpus-/Protokolldesign parallel; bestätigende Messung nach 1/2 und geeigneter Stabilitätsklärung. |

Umbenennung und GXL sind weiterhin reale TODOs, aber kein sechstes heimliches
Prioritätspaket: Rename rechtzeitig als begrenzten Release-Schritt entscheiden;
GXL-Implementierung nach Issue-Voraussetzungen und Evaluationsnutzen planen.
Weitere semantische Extension nur, wenn Coverage-Daten einen konkreten
Nutzerfall und einen sounden zusätzlichen Vertrag begründen.

## 15. Entscheidung und Schutzabschluss

**Genau eine Hauptempfehlung: PRODUCTIZATION/DOCUMENTATION NEXT.**
Gemeint ist zuerst ein sichtbarer, dokumentierter Coverage-/Completion-
Vertrag im Produkt, nicht nur Textpflege. Ein Benutzer muss beurteilen können,
ob Exit0/kein DOC611 auf vollständiger admitted Analyse oder auf unterdrückter
exakter Aussage beruht. Dieser Vertrag macht anschließend die hoch priorisierte
Performance-Arbeit und eine wissenschaftliche Evaluation erst vergleichbar.
Keine weitere semantische Closure ist für den ursprünglichen bounded B25-
Vertrag als notwendig nachgewiesen. Kein automatisch benanntes „B25C“.

**Gesamturteil:** Identity-/Ownership-/Finite-Graph-/SCC-/Admission-Grundlagen
sind für ihren Vertrag CLOSED, produktive Historical-Solution-Findings
BOUNDED-CLOSED. Allgemeine korrelierte exakte Rekursion bleibt
INTENTIONAL-LIMIT. OPEN sind vor allem Produktsichtbarkeit der Abdeckung,
aktuelle Performance-Einordnung und die separate Runtime-Untersuchung.
Der größte bewusste Grenzvertrag ist exact nur nach A9-FiniteOrbit-v1 und
vollständiger Bindung/Coverage; die Kettenbeschränkung ist engerer
Engineering-Scope, keine Universalgrenze.

Schutz: Gegen den Eintritt in **diesen** Review werden 764 Source-/Test-/
Build-/README-Dateien, 1.199 getrackte historische Evaluation-Dateien und
23 physische Core-Dateien byteweise verglichen; außerdem HEAD, physischer
Index, leerer semantischer Index, Root-Ignore und alle sechs Stashes.
Die alten S1-Core-obj-Abweichungen bleiben historische Evidence. Dieser
Vergleich repariert sie nicht, ändert kein Manifest und ersetzt keine alte
normative Baseline. Ein anderer Indexhash nach dem inzwischen erfolgten
Commit ist kein Anlass für eine Rücksetzung.

Finaler Status relativ zum Toolverzeichnis, durch
`verification.json` überprüft:

```text
 M ../../.gitignore
?? Evaluation/P5O2B25B-post-closure-review.md
?? Evaluation/P5O2B25BPostClosureReview/
```

Nur neuer Reviewbericht und statische Review-Evidence. Keine Produktions-/
Teständerung, keine historischen Evidenceänderungen, kein Build/Test/Gate/
Benchmark, keine Crash-Reproduktion, kein Commit/Push/Rebase/Stash-Eingriff.
Danach STOP; kein Folgepaket begonnen.
