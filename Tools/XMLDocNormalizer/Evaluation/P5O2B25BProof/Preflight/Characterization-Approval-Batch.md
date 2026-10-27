# B25B – ein konsolidierter Characterization-Freigabebatch

Status: **STOP vor Produktions-/Teständerungen; Freigabe für C1–C3 erbeten.**
Ausgangs-HEAD: `1ef19a37bbecc2ba1f9f2b9e2be27f7cc092c65e`.

Die Freigabe soll ausschließlich gelten, wenn die jeweiligen Fälle nach der
Implementierung nachweislich im bounded B25B-Profil liegen, vollständige
graph-/root-/scope-/executiongebundene Receipts besitzen und den eigenständig
richtigen Current- und Historical-Vertrag erfüllen. Sie ist kein Vorabbeweis
für einen möglichen Produktionsfix.

Es sind **drei Gruppen**, **zwei bestehende Dateien**, **18 bestehende Testfälle**:
C1 umfasst 15 Parameterfälle und den P0/P1-Fact; C2/C3 je einen Source-Guard-Fact.
Die B25-Klasse behält 27 Fälle. Keine Änderungen an bestehenden CLI-, B24-,
Worker-, Ownership-, Canonical-, Catch-, SCC- oder A10-Assertions außerhalb
der unten einzeln benannten Stellen.

Die alten Assertions samt eindeutiger Zeile/Span/Dateihash sind maschinenlesbar
in [characterization-inventory.json](characterization-inventory.json) erfasst.
Die vollständige Pipeline-/Receipt-/Unchanged-Matrix steht in
[README.md](README.md). Bisher wurde keine dieser Änderungen vorgenommen.

## C1 – positives Finding-Adoption-Ergebnis für die tatsächlich validierten B25-Inputs

Datei: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs`.

Betroffene Aufrufer:

- `ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission`,
  Aufruf um Zeile67, ausschließlich die folgenden 15 vorhandenen InlineData-Fälle;
- `AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted`,
  kein Parameter, Aufruf um Zeile131.

Gemeinsame geschützte Stelle: **nur die ersten vier Assertions** in
`AssertNoFindingAdmission`, um Zeile228:

```csharp
Assert.False(result.Succeeded);
Assert.Empty(result.Findings);
Assert.Null(result.Direct.Dispatch);
Assert.Null(result.Summary);
```

Die zweite Hälfte des Helpers (`directProfile` / `mismoded`) bleibt exakt
negativ: ein Single-Project-Project-Finding-Request ist kein gültiger
Solution-Finding-Opt-in. Seine drei Assertions sind **nicht** zur Änderung beantragt.

### Vollständige Fallliste und neue Erwartung

Alle Zeilen: Logical Mode SolutionTransitive, zwei-/dreiprojektige Kette,
vollständige ursprüngliche P4/P5/B12/B14-Inputs, nongenerische Finding-/Doc-Typen.
Erforderliche Summary wird über die echte A9/A10-Session admitted; keine normale
Finding-Adoption aus OUTSIDE/incomplete/receipt mismatch.

| Aufrufer / Parameterfall | Bisher | Vorgeschlagene neue Erwartung |
|---|---|---|
| Theory `(2, "chain")` | Pauschale Rejection / keine Dispatches | Erfolg, echte Direct- und Summary-Executions/Receipts; genau ein DOC611 am P0-Root, P1-Throw-Pfad intern |
| Theory `(3, "chain")` | Wie oben | Erfolg, beide Receipts; genau ein DOC611 am P0-Root, P0→P1→P2-Evidence |
| Theory `(2, "source-type")` | Wie oben | Erfolg, beide Receipts; genau ein DOC611, tatsächlicher Exception-Owner P1, Reporting P0 |
| Theory `(2, "mixed")` | Wie oben | Erfolg, beide Receipts; vollständiger Finding-Multisetvergleich gegen eigenständig geprüftes natives Current-Ergebnis; keine neue Direct-Evidence behaupten |
| Theory `(3, "mixed")` | Wie oben | Dasselbe für drei Projekte; dieser alte Fixture-Fall ersetzt nicht den neuen nichtleeren unabhängigen Direct-Witness |
| Theory `(2, "multiple")` | Wie oben | Erfolg, beide Receipts; ein kanonischer Exception-Typ trotz wiederholter Calls/Pfade, genau ein entsprechendes Missing-Finding |
| Theory `(3, "multiple")` | Wie oben | Dasselbe für drei Projekte |
| Theory `(3, "supporting-sources")` | Wie oben | Erfolg, beide Receipts; P1/P2-Throw-Evidence bleibt erhalten, weiterhin genau ein Finding desselben Typs |
| Theory `(2, "empty")` | Rejection; Findings leer | Erfolgreich validierte leere Summary, beide Receipts; Findings weiterhin exakt leer |
| Theory `(3, "empty")` | Wie oben | Dasselbe für drei Projekte |
| Theory `(2, "exact")` | Rejection; Findings leer | Erfolg mit beiden Receipts; exakte Dokumentdeckung, weiterhin keine Findings |
| Theory `(2, "base")` | Rejection | Erfolg mit beiden Receipts; voller Current-Multiset einschließlich exakt unterscheidbarer DOC611/DOC632; keine neue Base-Type-Dokumentdeckung |
| Theory `(2, "invalid")` | Rejection | Erfolg mit beiden Receipts; voller Current-Multiset einschließlich DOC660 und fehlender exakter Dokumentation; keine unterdrückte Tag-Diagnostik |
| Theory `(2, "inheritdoc")` | Rejection; Findings leer | Erfolg mit beiden Receipts; bestehende inheritdoc-Policy, weiterhin exakt keine Findings |
| Theory `(2, "no-doc")` | Rejection, kein Dispatch/Summary | Vertrauenswürdig validierter Direct-/Dokument-Run, legitimer No-doc-Guard; Findings weiterhin leer, **Summary bleibt null und wird nicht angefordert**; kein erfundener Summary-Complete-Claim |
| Owner-Fact, kein Parameter | Rejection trotz zwei richtigen Canonical-Ownern/Current-Findings | Erfolg mit beiden Receipts; genau zwei Findings dieses Witness, beide DOC611, getrennte P0/P1-Identitäten in zugehöriger Evidence; beide Findings am Reporting-Root P0 |

Die vorgeschlagene Änderung ist **keine** pauschale Null-/Empty-Lockerung:

```text
Succeeded == true
Direct.Dispatch != null und echte Historical-Selection / validiertes Receipt

wenn Root Dokumentation besitzt:
    Summary != null, SummaryRequested == true
    exakt validierte Summary-Execution, A9/A10-Complete, passende Pair-Receipt
sonst:
    Summary == null, SummaryRequested == false, Findings exakt leer
    No-doc-Guard nach Direct-/Dokument-/Graph-Input-Validierung

Findings == vollständiges erwartetes Current-Finding-Multiset
    inklusive File/Line/Column/Context/Snippet/Arguments/ID
    Reihenfolge darf normalisiert werden; Duplikate dürfen nicht verschwinden
```

Leere Erwartungsmengen bleiben strikt leer. Nichtleere Erwartungen werden
vollständig geprüft, nicht durch `NotEmpty`, Name-only oder bloße ID-Zählung
ersetzt. Normative Owner-/Kardinalitäts-Erwartungen werden zusätzlich unabhängig
vom Current/Historical-Gleichheitsvergleich geprüft. Insbesondere bleibt beim
Owner-Fact unverändert: genau P0/P1, volle Assembly-Identity-Auswahl von P1,
korrekter P1-Throw-Pfad, zwei native DOC611, kein positionsabhängiger Zugriff.

Production Path:

`FindExternalExceptionSmellsAsync` → bounded Solution-Operation aus originalen
Graph-Inputs → vorhandene Direct-/Dokument- und Solution-Summary-Requests →
Selection/Dispatch/echte Worker → Main-Validation → graph-/executiongebundene
Receipt-Paarprüfung → dieselbe Main-Finding-Policy.

B25B-Contract: nur der tatsächlich belegte admitted Ketten-Slice wird aktiviert.
Der alte Vertrag weist **alle** solchen Inputs vor Dispatch zurück. Diese
Pauschalrejection ist genau die nun beauftragte zu öffnende Grenze. Es wird keine
Identity-/Catch-/SCC-/Failure-Semantik dafür geändert. Frühere A10-Freigaben
erlaubten Owner-/Finding-Korrektheit bei weiterhin geschlossener finaler Adoption;
sie genehmigen nicht diese C1-Erfolgs-/Dispatch-Änderung.

### Unmittelbar notwendige Helper-/Evidence-Anpassungen unter C1

- Die beiden Aufrufe und der private Helpername dürfen den positiven bounded
  Vertrag ausdrücken; ggf. Rückgabe des tatsächlichen Finding-Ergebnisses für
  Witness-Metadaten. Keine zusätzliche Caller-/Parameter-Freigabe.
- Drei Stellen `FinalFindingAdmission = false` (um Zeilen84/122/143) dürfen
  ausschließlich in den zugehörigen **neuen B25B-Ausgaben** durch tatsächlich
  gemessene Operation-/Receipt-/Finding-/SummaryRequested-Information ersetzt
  werden. Kein statisches `Complete=true`, insbesondere nicht im No-doc-Fall.
- Der bisherige Writer dieses Tests zeigt auf
  `artifacts/p5o2b25a10/analysis/b25`. Neue Resultate dieses aktualisierten Tests
  dürfen ausschließlich unter `artifacts/p5o2b25b/...` geschrieben/archiviert
  werden. Die unveränderten zehn negativen B25-Fälle können dort als frische
  B25B-Regression mitgeführt werden; ihre Assertions ändern sich dadurch nicht.
- Historische B25/A–A10-/Readiness-Berichte, Audits, TRX, Logs und Witnesses
  bleiben unverändert. Keine rückwirkende Anpassung von `FinalFindingAdmission`
  in einem alten Capture.

## C2 – B25 Source-Guard: keine pauschale Ablehnung aller Solution-Jobs mehr

Datei: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs`.

Methode: `StopRule_DoesNotIntroducePolicyRoutingAttestationOrPidWorkarounds`.
Parameterfall: keiner. Geschützte Stelle um Zeile220, ausschließlich:

```csharp
Assert.Contains("Solution is not admitted", external);
Assert.DoesNotContain("SupportingProjects", external);
```

Bisherige Erwartung: ExternalTarget enthält die globale Solution-Rejection und
keine Supporting-Project-Komposition.

Neue Erwartung: funktionale Prüfung der tatsächlichen bounded Production-Grenze:

- Eine vollständige, gültige Zwei-Projekt-Kette mit einem nongenerischen
  Supporting-Throw wird über die echte Produktionsoperation zugelassen;
  ein erwartetes DOC611 gehört P0, Receipts sind validiert.
- Eine ansonsten vollständig valide Vier-Projekt-Kette wird in dieser
  Finding-Operation **vor Dispatch** abgewiesen: `Succeeded=false`, Findings
  leer, Direct.Dispatch null, Summary null. Kein allgemeines Solution-GO.
- Der Test kann dafür einen neuen Testhelper benutzen und mechanisch async
  werden; der Helper muss vorhandene P4/P5-Factories und den echten
  `FindExternalExceptionSmellsAsync`-Pfad benutzen, nicht eine Test-Policy.

Production Path / Contract: dieselbe operationelle Ketten-/Root-/Receipt-Grenze
wie C1; die globale Source-String-Abwesenheit kann nach einer legitimen
Supporting-Material-Komposition nicht mehr die richtige Architekturregel sein.
Der Ersatz prüft sowohl einen echten positiven bounded Kontrollfall als auch
die weiterhin geschlossene Größen-Grenze.

**Alle übrigen bestehenden Assertions dieses Facts bleiben unverändert**:
kein `GetFirstSymbolForDeclarationId` im Evaluator, vorhandener exakter Resolver,
`OwnedProcessExit.Capture(process)`, kein `GetProcessById`, Main-Isolation.
Keine Lockerung einer Abhängigkeits-/PID-/Attestation-Regel. Dafür gilt keine
frühere A10-Freigabe; A10 ließ diese finale Adoption ausdrücklich geschlossen.

## C3 – derselbe historische All-Solution-Source-Guard im Ownership-Test

Datei: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs`.

Methode: `StopRule_LeavesOriginalProductionAndClosedFindingAdmission`.
Parameterfall: keiner. Ausschließlich die Assertions um Zeilen114–115:

```csharp
Assert.Contains("Solution is not admitted", external);
Assert.DoesNotContain("SupportingProjects", external);
```

Alter Vertrag, neuer funktionaler bounded Kontroll-/Rejection-Vertrag,
Production Path und semantische Begründung sind exakt C2. Derselbe neue
testseitige Helper darf verwendet werden; kein zweiter Analyzer oder alternative
Receipt-Fassade. Mechanische async-Anpassung nur für diese Ausführung.

Unverändert bleiben insbesondere die beiden vorangehenden Evaluator-Assertions
und **alle anderen 13 Ownership-Testfälle**, einschließlich K17/P12/R5,
voller kanonischer Identity, expliziter Roslyn-Symbolgleichheit, Catch- und
gefälschter Assembly-/Module-/Version-/Reference-Negativen. Keine frühere
Freigabe gestattet, die finale All-Solution-Rejection durch Aktivierung zu ersetzen.

## Ausdrücklich NICHT beantragt / unverändert

- `AssertNoFindingAdmission`: alle drei Assertions des zweiten `mismoded`-Calls.
- B25: alle übrigen Flow-/Graph-/Owner-/Finding-Inhalt-/Repeat-/Isolation-/
  Lifetime-Assertions; sieben Graph-Mutations-Negative.
- B25-CLI-Fact `RealCli_SolutionJobRemainsUnsupported_NoReportOrEmptySuccess`:
  `(2,false)`, `(3,false)`, `(2,true)`. Seine aktuellen Jobs enthalten **keine**
  Supporting-Materialien. Exit4, kein Report, kein Erfolgsclaim und `not admitted`
  bleiben richtig. Keine Fixture wird dafür künstlich in Scope gebracht.
- B24: alle 42 Fälle, einschließlich lokaler OwnershipSnapshot, Direct ohne
  Summary, uncertain Project fail closed, falsches Worker-Image/Replays und
  unvollständiger Solution-Job. Keine lokale Scope-Assertion wird auf Graph
  umgedeutet; Solution bekommt getrennte lokale Receipts.
- B22/Declared: alle inkompatiblen Modi, cross-project-Rejections, Guard-/
  Attestation-/Original-Receipt-Negativen und Main/Worker-Architektur.
- B23: Legacy-CLI-Mode-Overrides/Force-Historical-Rejections; neue positive
  vollständige Solution-CLI-Witnesses entstehen separat unter B25B.
- A10/ProfileOutcome, incomplete/OUTSIDE, generische Identity/Catch/SCC,
  Worker-Identity-/Failure-/Version-/Isolation-Assertions.
- Bestehende Architecture-Guards und 125 physisch geteilte Analyzer-Sources.

Nach Freigabe nur C1–C3 umsetzen, dann die im Auftrag verlangte Validierung.
Bei einem tatsächlich unerwarteten weiteren geschützten Vertrag oder neuem
Wire-/Architekturcontract weiterhin STOP – aber nicht für die hier bereits
vollständig aufgeführten, genehmigten Konflikte erneut einzeln anhalten.
