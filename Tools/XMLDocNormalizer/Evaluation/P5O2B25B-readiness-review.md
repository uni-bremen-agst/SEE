# P5O2B25B – Repository-Hygiene und Readiness-Review nach A10

Stand: 24.10.2026, Branch `fix/documentation`.

## Entscheidung

**GO B25B WITH EXPLICIT BOUNDS – für das nächste, zusammenhängende
Implementierungs-/Validierungspaket, nicht für eine Aktivierung in diesem Lauf.**

Die ursprünglich blockierende gemeinsame Type-Owner-Normalisierung und die
danach aufgedeckten Identity-/Substitution-/SCC-Probleme sind für den tatsächlich
implementierten `A9-FiniteOrbit-v1`-Bereich geschlossen. Daraus folgt kein
allgemeines Solution-GO und noch kein positiver Historical-Finding-E2E-Beweis.
Die finale Historical-Solution-Finding-Adoption ist heute absichtlich geschlossen.

B25B darf die weiter unten definierten Receipt-/Dokument-/Finding-/CLI-Lücken
als **ein** enges Aktivierungspaket schließen. Es darf nicht lediglich die
Mode-Guards entfernen. Seine positiven und negativen Witnesses sind vor einer
Abschluss-/Aktivierungsbehauptung zwingend zu liefern. Für Generic-Finding-Typen,
unvollständige Coverage oder allgemeine rekursive Familien wird kein GO erteilt.
Neue Analyzer-Semantik oder ein ungeklärter Identitätsverlust würde diese
Entscheidung in STOP ändern, nicht zu einer opportunistischen Erweiterung führen.

Dieser Review ändert ausschließlich neue Review-/Inventar-Dateien unter
`Evaluation`. Keine Produktions-/Teständerung, kein Cleanup, kein Build oder
Testlauf, keine Stash-/Indexoperation, kein Commit/Push.

## 1. Tatsächlicher Repository-Zustand (H1)

| Eigenschaft | Direkt geprüfter Stand |
|---|---|
| HEAD / A10-Commit | `a2fae5fc29f682461b38385990568fc5da494ae7`, `Implement finite-orbit exception flow admission` |
| Parent | `9b76e179a568c64ae972cc3a45980a094e44245e` |
| Branch / Upstream | `fix/documentation` / `origin/fix/documentation` |
| Lokal gespeicherter Upstream | Derselbe Commit, ahead/behind `0/0` |
| Direktes Remote | `git ls-remote origin refs/heads/fix/documentation` liefert denselben vollständigen HEAD; damit Push tatsächlich bestätigt, nicht nur aus dem lokalen Tracking-Ref abgeleitet |
| Anfangsstatus | Ausschließlich ` M ../../.gitignore` |
| Staged | Leer |
| Unstaged | Nur geschützter Root-Ignore-WIP, 4 Einfügungen / 1 Löschung |
| Physischer Index SHA256 | `13032C5672F8A79C0F4B923BCF47E51F8A0D5E4115A712BBC9B195C18A526106` |
| Root-Ignore SHA256 | `7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5` |
| Stashes | Genau sechs; vollständige IDs/Texte im Start-/Final-Capture |
| Ignoriertes `artifacts` | Am Anfang nicht vorhanden; nicht rekonstruiert, wiederhergestellt oder erneut gelöscht |

Die erste Remote-Abfrage war durch die Sandbox auf Port22 blockiert. Die
anschließend autorisierte read-only Abfrage war erfolgreich. Kein Fetch,
Remote-Ref-Update oder anderer schreibender Git-Befehl wurde verwendet.

### Geschützte Core-Dateien

Alle 23 physischen Dateien wurden am Anfang gelesen/gehasht und am Ende gegen
genau diese Anfangsbytes verglichen. **Kein Drift in diesem Review.**
Gegenüber dem historischen A10-Abschluss bestehen schon am Anfang zwei
Abweichungen, nicht 23 unverändert zum alten Commit:

| Datei unter `src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0` | Historischer A10-Abschluss SHA256 | Aktueller Anfangs-SHA256 |
|---|---|---|
| `XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfo.cs` | `A6B602AD3DABBB3B7AD41B26C8B73CE2C65E8EE2D09DB60DBFF811440F1647E8` | `387EC80969538596DEA6E579EBDE6D60D24DC32015C57CB70C1D4160DB0A7A61` |
| `XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfoInputs.cache` | `1284572B6EEED1C6B6668BC02AF762B4DE5040B2D9F343AC90E54F4529B32BCD` | `9EEE97147077E45BEA6A73748F261D8D3F916A742A626A0E0862E5626E3D600D` |

Die AssemblyInformationalVersion enthält jetzt den tatsächlich committed A10-HEAD
statt dessen Parent. Beide alten/neuen Info-/Cache-Hashes wurden aus genau
dieser Commit-Ersetzung und dem bestehenden MSBuild-Attribut-Digest bytegenau
reproduziert. Der schreibende Prozess ist nicht bewiesen. Es erfolgt keine
Rücksetzung, Manifeständerung, neue Schutzbaseline oder rückwirkende
Umschreibung der historischen S1-/Exit1-Evidence. Die übrigen 21 Dateien
entsprechen auch den historischen Abschlussbytes.

Nachweise:
[repository-start.json](P5O2B25BReadiness/repository-start.json),
[repository-final.json](P5O2B25BReadiness/repository-final.json).
Das Final-Capture prüft außerdem 507 bereits getrackte historische
B25–A10-Evaluation-Dateien auf unveränderte Bytes.

## 2. A10-Commit-Inventar und Hygieneentscheidung (H2/H3)

Untersucht wurde ausschließlich der Inhalt des gepushten A10-Commits unter
`Tools/XMLDocNormalizer/Evaluation/P5O2B25A10Proof/`.

| Gruppe | Dateien | Git-Blob-Bytes | Entscheidung |
|---|---:|---:|---|
| Gesamter Proof-Bereich | 375, alle durch A10 hinzugefügt | 151.218.571 (144,21 MiB) | Einzeln im Inventar erfasst |
| Getrackte `bin/` / `obj/` | 0 | 0 | Kein H-CLEAN-Bestand |
| Getrackte Package-/Cache-/Temp-Verzeichnisse | 0 | 0 | Kein H-CLEAN-Bestand |
| DLL / PDB / EXE / NUPKG / Cache-Binärprodukte | 0 | 0 | Kein H-CLEAN-Bestand |
| TRX | 25 | 41.738.770 | H-REVIEW, erhalten |
| Logs | 112 | 395.024 | H-REVIEW, erhalten |
| H-KEEP-Dateien | 230 | siehe Einzelinventar | Berichte, Skripte, Proof-Quellen/csproj, kleinere Audit-/Contract-/Final-Evidence |
| H-REVIEW-Dateien | 145 | siehe Einzelinventar | TRX/Logs sowie große JSON-Zwischen-/Witness-Dateien; kein pauschaler Löschentscheid |

Größen sind Git-Blob-Größen am Commit. CRLF-Working-Tree-Größen und SHA256
stehen separat im Inventar; diese werden nicht mit historischen Hashes vermischt.
Die gesamte Extension-Liste enthält nur `.gitignore`, Markdown, PowerShell,
C#/csx/csproj, JSON, TRX, Logs und eine TXT-Methodenliste; auch außerhalb von
`bin/obj` wurde kein anderes offensichtliches Binär-/Buildformat gefunden.
Physisch vorhandene ignorierte Proof-`bin/obj` sind **nicht getrackt** und werden
hier weder als Commit-Schaden gezählt noch zur unnötigen lokalen Bereinigung angefasst.

### Größte Dateien

| Relativer Pfad im A10-Proof-Bereich | Git-Blob-Bytes | Bewertung |
|---|---:|---|
| `Complete-fixture-preflight.json` | 21.282.704 | H-REVIEW behalten: vollständiger damaliger Assertion-/Case-Preflight |
| `BroadClosure/current-certificates.json` | 19.458.933 | H-REVIEW behalten: beobachtete klassifizierte Fälle, im Broad-Abschluss erläutert |
| `BroadRootCauseAudit/broad-rc-current.json` | 19.238.299 | H-REVIEW behalten: tatsächliche diagnostische Beobachtungen, keine Build-Ausgabe |
| `BroadRootCauseAudit/partition.json` | 14.932.220 | H-REVIEW behalten: vollständige 179-Fall-Partition und Freigabezuordnung |
| `Supplementary-resolved-bindings-r2.json` | 11.046.060 | H-REVIEW behalten: finaler Supplementary-Binding-Nachweis |
| `Supplementary-resolved-bindings.json` | 9.300.260 | H-REVIEW behalten: vorheriger, explizit abgelöster Auditstand |
| `BroadFollowup/broad.trx` | 5.949.332 | H-REVIEW behalten: früherer Broad-Lauf, nicht final grün umetikettieren |
| `BroadClosure/Results-r17-independent/full.trx` | 5.572.653 | H-REVIEW behalten: historisches Full3428/3429 |
| `BroadClosure/Results-r17/full.trx` | 5.572.653 | H-REVIEW behalten: gleiche Bytes in anderem Capture-Kontext |
| `FinalClosure/LifetimeResults/full.trx` | 5.399.884 | H-KEEP als finale Result-Evidence; im konservativen Dateifilter H-REVIEW, nach Prüfung ausdrücklich behalten |

Die Dateifilter-Kategorie H-REVIEW ist eine Prüfungspflicht, kein Urteil
„Müll“. Nach Inhalts-/Referenzprüfung werden finale grüne sowie historisch
rote Ergebnisse bewusst erhalten. Konkret lesen `Capture-BindingClosure.ps1`
und `Final-consolidated-approval-batch.md` die Preflight-/r2-Dateien;
`BroadRootCauseAudit/README.md` und `BroadClosure/Capture-Closure.ps1` benötigen
`partition.json`; `BroadClosure/README.md` erläutert `current-certificates.json`.
`FinalClosure/lifetime-closure.json` bindet den grünen Full-TRX-Hash, fünf
Gate-Hashes, wiederverwendete Proof-/WAE-Hashes und den alten unabhängigen
Snapshot. Die früheren Capture-JSONs referenzieren ihre jeweils eigenen
TRX-/Log-Sätze. Eine Extension allein erlaubt keine Löschung.

Es gibt 23 gleiche Git-Blob-Gruppen mit 37 zusätzlichen Kopien und insgesamt
11.071.757 zusätzlichen Bytes in der aktuellen Baumansicht. Fast alles entfällt
auf den doppelten r17-Full-/Broad-Satz. Gleiche Bytes bedeuten nicht identische
Referenzrollen. Auch hier keine Löschung, Umbenennung oder neue Referenzadresse.

**Cleanup-Ergebnis:** kein eindeutiger getrackter H-CLEAN-Fall, daher kein
Cleanup-Diff und aktuell kein nötiger Cleanup-Commit. Bestehende lokale
Proof-Ignore-Regeln `bin/`, `obj/` samt gezielten csproj-Ausnahmen reichen für
den beobachteten Bestand aus; auch sie bleiben unverändert. Ein späterer normaler
Dedup-/Archiv-Commit wäre nur mit vollständiger Referenz-/Hash-Migrationsprüfung
sinnvoll. Kein History-Rewrite würde die aktuelle Baumhygiene rechtfertigen.

Das vollständige Einzelinventar, Gruppen, Größen, Top-Dateien und alle
Dublettenpfade stehen in
[hygiene-inventory.json](P5O2B25BReadiness/hygiene-inventory.json).

## 3. Verifizierte A10-Baseline, keine neuen Gate-Läufe

| Vorhandene Evidence | Geprüfter Stand |
|---|---|
| Broad | 3338/3338, keine Skips |
| Full nach freigegebenem Harness-Fix | 3429/3429, keine Skips; genau ein neuer Full nach Fix |
| Historischer Full davor | 3428/3429; bleibt rot und unverändert erhalten |
| Isolierter MultiModule-Test | 1/1; originale fachliche Assertions unverändert |
| Boundary | 596/596 |
| Dual-Version | PASS, neun Testsätze zusammen 800/800 |
| WAE | 11/11 saubere Build-Schritte |
| Current-Baselines | Acht historisch protokollierte bytegleiche Vergleiche |
| A6/A7/A8/A9/A10-Abschlussadapter | Exit0, 397/374/393/396/437 Checks |
| A10 / A6 / A5 im grünen Full | 19/19; 8/8; 33/33 |
| Unabhängige A8-/A9-Programme | 40/40; 37/37 |
| Zusätzlicher realer Current-Production-Pfad | 30/30 `Session.Analyze`-Checks, keine Raw-Graph-Zertifizierung |
| Kompilierte Input-Bindung im Abschluss | Drei PE/PDB-Paare, 254 Source-Dokumente, keine Differenz |

Neu ausgeführt wurden **nur 43 statische Hash-/Counter-/Record-Prüfungen**,
keine Tests. Alle 760 im Abschluss erfassten src/Tests/build-Inputs stimmen
mit dem getesteten A10-Stand überein. Finale TRX-/Gate-/Proof-/Dual-/WAE-Dateien
in den getrackten Capture-Verzeichnissen stimmen mit ihren erhaltenen Hashes
überein. Acht Current-Baseline-Vergleiche bleiben historische Vergleichsevidence;
die gelöschten ignorierten Originaldateien werden nicht als heute erneut
verfügbar oder neu verglichen dargestellt.

Nachweis: [a10-evidence-verification.json](P5O2B25BReadiness/a10-evidence-verification.json).
Die historischen Gate-Dateien enthalten noch damalige Zwischenstandsflags,
z.B. `A10Complete=false`, absolute Exportpfade und alte Index-/HEAD-Bindungen.
Das widerspricht nicht dem anschließend committed Abschluss, ist aber kein
Anlass, diese Flags oder Dateien rückwirkend umzuschreiben. Die Scripts können
nach dem `artifacts`-Löschen nicht blind gegen ihre alten absoluten Inputs
replayed werden; eine spätere Reproduktion braucht einen frischen, kontrollierten
Export-/Offline-Cache-Aufbau. Self Analysis und Canonical Finding Diff sind
auch hier keine frisch ausgeführten Prüfungen.

Dokumentarischer Altstand: `OPEN-PIPELINE-BOUNDARIES.md` nennt am Anfang noch
unreparierte A-/A2-Owner-Probleme und führt die spätere Kette nur bis A5 fort.
Das ist nach dem geprüften A10-Code/Evidence-Stand veraltet, kein neuer
Produktblocker und keine Widerlegung der Closure-Matrix. Das Register bleibt
hier unverändert; das nächste B25B-Paket sollte seinen aktuellen Status mit
dem dann tatsächlich bewiesenen Aktivierungsumfang aktualisieren und die
historischen Meilensteine als solche behalten.

## 4. Ursprünglicher B25-Blocker (R1)

B25 sollte die bestehende B15-Solution-Analyse für A→B und A→B→C mit der
bereits vorhandenen Main-Finding-Policy verbinden: unabhängig erhobene Direct-
Evidenz, ggf. separat attestierte Summary, rootgebundene XML-Fakten und am Ende
normale Findings/CLI/Reporter. B und C liefern Bodies, werden nicht automatisch
Reporting-Ziele. B21 attestiert den ganzen ausgeführten Payload; es entscheidet
nicht die Korrektheit der Analyzer-Semantik.

Der damalige Witness hatte `Shared.Fault` in P0 und P1. Beide erzeugen dieselbe
Declaration-ID `T:Shared.Fault`. Die lokalen Summaries hatten bereits die richtigen
Owner. `ExceptionFlowSummaryGraphEvaluator.NormalizeSummaryExceptionType` nahm
jedoch `GetFirstSymbolForDeclarationId(id, rootCompilation)` ohne vollständigen
Assembly-Vergleich. Beim Merge wurde P1s Throw zu P0s Typ normalisiert. Dadurch
blieb ein statt zwei Typ-/Finding-Einträgen übrig. Reporting-Position in P0 und
Throw-Pfad in P1 waren richtig, der Exception-Type-Owner trotzdem falsch.

Current und Historical führten denselben fehlerhaften Code aus. Ihre Parität
belegte deshalb nur denselben Fehler, nicht richtige Owner-Semantik. B25 nahm
die prototypische Aktivierung zurück; geschlossen blieben alle finalen externen
SolutionTransitive-Findings und Solution-CLI-Jobs. B15-Flow/Graph blieb davon getrennt.

Heute bindet der Normalizer über `RoslynCanonicalIdentityResolver` eindeutig
assembly-/module-/constructed-qualified. `Session.Analyze` fordert zusätzlich
einen eindeutigen Destination-Roundtrip. Im bestehenden B25-Fact
`AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted`
stehen inzwischen ausdrücklich zwei kanonische Owner P0/P1 und zwei DOC611.
P1 wird über vollständige `CanonicalAssemblyIdentity` ausgewählt, nicht über
Collection-Position. Der Name des Facts ist historisch; seine Assertions
belegen nicht mehr den alten Fehler. Seine finale Historical-Finding-Rejection
ist dagegen weiterhin absichtlich aktiv.

Originalbegründung unverändert:
[B25-Bericht](P5O2B25-bounded-solution-finding-adoption.md).

## 5. Closure-Matrix A–A10 (R2)

„CLOSED“ gilt für den genannten bestehenden Vertrag; „BOUNDED-CLOSED“ für
den endlichen admitted Bereich, nie für jede C#-Lösung. „OPEN“ heißt nicht
heimlich implementiert. Code-/Testdateien sind im Quellenregister unten verlinkt.

| Contract / Kette | Status | Konkreter Code / unabhängige Prüfung |
|---|---|---|
| Assembly-qualified Exception Owner (A/A2/A10) | BOUNDED-CLOSED | C4 Resolver + C3 Normalizer + C1 Roundtrip; T1 P0/P1, T2 `OriginalNameOnlySubstitution...` mit explizit ungleichen/faktisch richtigen Ownern |
| Module / Nested Owner | BOUNDED-CLOSED | C4 vollständige Identity-Prüfung und verschachteltes Owner-Rebinding; T2 `ConstructedContainingType...`, nicht Name-only |
| Method Type Arguments (A3/A10) | BOUNDED-CLOSED | C5 `Bindings`, `ApplyCallable`; C6 constructed ContextKey; T3 `ActualCallSiteRetainsBothDimensions...`, T5 finite production method-Fälle |
| Containing Type Arguments | BOUNDED-CLOSED | C5 outer-to-inner Binder; C6 InvocationSymbol statt allein Definition; T3 containing/both, T4 konstruiertes `Outer<int>.Leaf` |
| Nested Generic Construction | BOUNDED-CLOSED | C4 ResolveContainingType + Construct; C5 simultane Substitution; T2 K17/P12/R5 canonical + explizite Roslyn-Gleichheit; T4 nested identity families |
| Identische Construction wiederverwendbar, verschiedene getrennt | BOUNDED-CLOSED | C6 `ConstructionKey`, Equality/Hash; T3 `DifferentConstructionsShareActualNode...` jetzt getrennte Keys für verschiedene Construction |
| Cross-compilation Rebinding | BOUNDED-CLOSED | C4/C7 volle Declaration-/Assembly-/Containing-/Method-Konstruktion; T3 Resolver-Fälle, T5 `Production_SupportingSourceTransfersCallerScopedTemplateThenConcreteInvocation` |
| Endliche rekursive SCC Closure (A4/A5) | BOUNDED-CLOSED | C3 `(Key, SCC-Ancestor-Exclusions)` und nur fertige Cache-Einträge; T6 `EverySccEntryAndRootOrder...`, `AllThreeNodeGraphs...` mit unabhängigem Reachability-Oracle |
| Keine aktive Partial-Summary als Complete | BOUNDED-CLOSED | C3 aktive Keys werden nicht als fertige Cachewerte gelesen; T6 incomplete dependency, genuine Worker finite closure |
| Edge-order Independence / Cache Context | BOUNDED-CLOSED | C3 SCC-spezifischer Ausschluss im Cachekey; T6 Root-Reihenfolgen und mehrere unabhängige SCCs, A5 33/33 |
| A6 wachsender exakter Orbit | CLOSED als Grenze | T7 finite Prefix-Witness ist keine Terminierungsannahme; C8 positiver Constructor-Zyklus vor konkreter Produktionsgraph-Erweiterung |
| Allgemeine korrelierte Familien / unbekannter Residual (A7/A8) | OPEN, ausdrücklich außerhalb GO | A7/A8 begründen die allgemeine Solver-/Catch-/Projection-Grenze; kein endlicher Witness wird zum allgemeinen Entscheidbarkeitsbeweis erklärt |
| A9-FiniteOrbit-v1-Admission | BOUNDED-CLOSED | C8 endliche ownerqualifizierte Definition-/Binder-Inventare, gewichtete Slot-SCCs; Nullgewichtzyklen/Reset erlaubt, positive Rückkehrzyklen OUTSIDE |
| Kein Definition-/Depth-/Retry-Folding | CLOSED innerhalb Profil-Vertrag | C8 entscheidet auf endlichem Inventar, nicht per Context-Depth; C1/C2 exakte Expansion nur unter Seal; T5 growing vs finite/orbit/reset |
| P1 Root Admission (A10) | BOUNDED-CLOSED | C1 `Discover`/Certificate vor `BeginProduction` und `TryRegisterSummaryGraphRoot`; T5 `Production_GrowingRecurrenceIsRejectedBeforeRootMaterialization` |
| P2 Discovered Context Admission | BOUNDED-CLOSED | C2 `RegisterTarget` prüft Transition, `GetOrAdd` Scope/geschlossenen Context vor Add/Enqueue; T5 `ProductionSeal_RejectsAnUndiscoveredSuccessorBeforeGetOrAdd` |
| Konkrete Sources / Substitution / Result Projection | BOUNDED-CLOSED | C5 simultane owner-scoped Substitution; C1 Destination-Bindung/Truncation-Check; echte Current A→B→C-Kompositionschecks im 30er Zusatzproof |
| Exakte Catch-Semantik | BOUNDED-CLOSED | C9 volle canonical Identity entlang tatsächlicher BaseTypes; T2 `Fault<int>` ≠ `Fault<string>`, T5 finite production catches, T4 genuine Historical constructions |
| Incomplete Syntax / Coverage | BOUNDED-CLOSED | C8 incomplete Root vor Canonicalisierung; Compilation-Bindingfehler außerhalb, allein CS5001 als fehlende ausführbare Verpackung ausgenommen; T5 `Production_IncompleteRecoveryRootIsClassifiedBeforeCanonicalizationOrRegistration` |
| External Documentation | BOUNDED-CLOSED als Diagnostik/Ablehnung | C8 beobachtet Coverage, C1 gibt Certificate; C10 normale Missing-/Extra-Findings gesperrt; T8 ExternalDocumentationOutside, keine Aufwertung von XML zu Proven |
| Worker Identity auf klassifiziertem Fehler | CLOSED | C12 prüft reale Identity vor Failure-Projektion; T5 `Diagnosis_ClassifiedFailurePreservesActualValidatedIdentity`, kein Receipt/Result auf Fehler |
| Current/Historical gemeinsame Admission-Semantik | BOUNDED-CLOSED | Dieselben 125 Shared Sources; C1 läuft in beiden Hosts; T3/T4/T6 echte Worker-Result-/Failure-Prüfungen plus Identity/Isolation, nicht nur Parität |
| Diagnostic Raw Graph ohne Production Bypass | CLOSED im heutigen Callgraph | C1 allein ruft den Evaluator produktiv; Raw `TryBuildTransitiveSummaryGraph` hat keinen Produktionscaller; Historical convenience Entry führt ebenfalls `Session.Analyze` aus |
| Finales Historical Solution-Finding / CLI | OPEN – exakt die B25B-Arbeit | C11/C13/C14 weisen Solution-Finding heute zurück; T1 positive Flow-/Current-Finding-, aber negative Historical-Finding-/CLI-Evidence |

A10 macht A7/A8 nicht rückwirkend zu allgemeinen GO-Paketen. Es implementiert
die spätere A9-Einschränkung, innerhalb der eine endliche exakte Antwort möglich
ist. Diese Unterscheidung bleibt für B25B normativ.

## 6. Tatsächliche Pipeline, Current und Historical getrennt (R3/R5)

### Current, bereits produktiv

```text
ArgParsing / ToolRunner.Run -> RunProjectOrSolution
-> ProjectClosureSemanticContextBuilder.Build, Reporting-/Supporting-Scopes
-> XmlDocExceptionSemanticDetector.FindExceptionSmells pro Reporting-Member
-> unabhängige Direct-Erhebung; Mode/RequiresSummary
-> AnalyzeConfiguredExceptionFlow[Async]
-> Dispatch: trusted CurrentCompilation -> SelectionPolicy -> Router(Current)
-> vorhandene Session.Analyze
-> Discover / FiniteOrbit-v1 P1 -> versiegelter Graph / P2
-> endliche konkrete Construction -> SCC/cache-sichere Evaluation
-> eindeutige Typ-Projektion + Complete/Outside-Outcome
-> nativer Detector prüft ProfileOutcome vor Finding-Policy
-> AddExceptionFindingsCore / AddMissingExceptionFindings
-> Finding(root member / XML tag), RunResult, bestehende Reporter
```

Current hat hier den nativen `ProfileOutcome`; ein bloß erfolgreicher Dispatch
oder leeres kanonisches Result wäre **kein** Complete-Beweis. B25B darf diesen
Status nicht durch Übernahme eines statuslosen kanonischen Raw-/Current-Ergebnisses
verlieren. Der externe Worker-Payload ist bei einer Current-Selection kein
Ersatz für eine vorhandene Current-Session; der Router lehnt unpassende Payloads
ab. Kein impliziter Current-Rebuild als Historical-Fallback.

Normale Current-Solution-Ausführung kann mehrere explizit ausgewählte Projekte
als eigene Reporting-Projekte analysieren. Davon zu unterscheiden ist B25B:
ein externer ausgewählter Root in A, B/C nur Supporting-Scopes.

### Historical, was heute wirklich erreicht wird

```text
AnalyzeExternalExceptionFlowAsync [flow-only, nicht Finding-Adoption]
-> HistoricalWorkerPayloadProjection.Project
-> HistoricalWorkerSolutionProjection.Project mit Original-P4/P5/B12/B14-Inputs
-> finaler WorkerAnalysisInput mit Solution-Graph
-> Dispatch: validiertes PDB -> exakte Selection -> Router(Historical)
-> HistoricalWorkerClient -> eigener Prozess / pinning / bounded exchange
-> HistoricalAnalysis.Reconstruct je Projekt, exakte Source-/PE-/Scope-Bindung
-> HistoricalAnalysis.Analyze -> dieselbe Session.Analyze (P1/P2/SCC/Projection)
-> OUTSIDE: UnsupportedContext + tatsächliche WorkerIdentity, kein Result
-> sonst kanonisches vollständiges Proven-Result, kein ungewisser/trunkierter Erfolg
-> Main ResponseValidation: Identity, Referenzwelt, Sources, Pfade, Provenance
-> kanonischer Flow/Graph verfügbar; KEINE finale Solution-Finding-Receipt
```

Der tatsächliche Finding-Pfad endet früher:

```text
ExternalTargetExecution.AnalyzeAsync: job.analysisMode=solution-transitive
-> sichtbar 'Solution is not admitted', kein Reporter/Success

oder interner FindExternalExceptionSmellsAsync(Solution-Input)
-> ProjectForFindings
-> UnsupportedProfile VOR Dispatch; keine Findings / kein Summary-Call
```

C11 erlaubt Independent-Finding nur Direct/Project und keine SupportingProjects;
C13 `WorkerFindingContract` verlangt `context.Solution == null` und kennt noch
keinen Solution-Finding-Mode. Auch Declared-Finding ist keine Solution-Aliasroute.
Es existiert folglich **kein heute schon erfolgreicher Historical-Solution-
Finding-E2E-Pfad**, den man allein über einen CLI-Schalter freischalten könnte.

### Wiederverwendbare, aber ausdrücklich zu erweiternde Grenze

B24/B22 verbinden bereits echte Direct-/Summary-Executions mit der gemeinsamen
Main-Policy. Wiederverwendbar sind neutrale Canonical-/Graph-/Dokument-/Direct-
Shapes sowie B21s private, single-use, nicht serialisierbare Execution-Autorität.
Nicht wiederverwendbar *ohne Anpassung* ist ihre Single-Project-Zulassungsregel.

Besonders konkret: C12 mintet `HistoricalFindingFacts.OwnershipSnapshot` heute
aus `input.Context.Scope`, also **nur dem lokalen Scope**. Für B25B muss die
Zusammengehörigkeit von Direct und Summary die vollständige Solution-Referenz- /
Source-/Owner-Welt binden. Der B21-Token bindet bereits die exakte ganze Request-
Serialisierung und reale Response, EOF, Exit, PID und ausgeführtes Worker-Image;
`ReferenceWorldHash` umfasst den Solution-Graph. Kein neuer Attestierungsmechanismus
ist nötig. Eine lokale Snapshot-Gleichheit darf aber nicht als graphübergreifende
Stage-Gleichheit ausgegeben werden.

Ebenso muss die Original-Dokument-Authority erst am **final komponierten**
Graph-Payload registriert werden. Eine lokale Zwischenkopie erbt sie nicht.
Root und Graph-Target-Node müssen denselben gültigen Zweck/Mode/Doc-Kontext
tragen; die bestehende Root-vs-Target-`ReferenceWorldHash`-Kohärenz darf nicht
umgangen werden. Direct-/Summary-Zwecke unterscheiden sich legitim: nicht
unbesehen zwei komplette, zweckabhängige Hashes auf Gleichheit setzen, sondern
den gemeinsamen immutable Graph-/Root-/Doc-Vertrag explizit vergleichen.

### Getrennte Vertrauensbewertung

| Eigenschaft | Current | Historical |
|---|---|---|
| Eigene Korrektheits-Witnesses | Explizite erwartete Faults/Construction/Catch/Owner, unabhängige SCC-Reachability | Echte Worker-Prüfungen mit denselben vorher begründeten Soll-Identitäten/Faults sowie eigenen Failure-/Identity-Negativen; Parität nur zusätzlich |
| Admission | Native Session P1/P2 | Physisch dieselben Source-Dateien in isoliertem Analyzer |
| Canonical Identity | Voller Root-World-Roundtrip und native Symbolgleichheit | Eigene Rekonstruktion/Canonical-Produktion, danach Main Owner-/Reference-/Location-Validierung |
| Finding-Policy | Gemeinsame Main-Policy aktiv | B22/B24 dieselbe Main-Policy aktiv; Solution-Facts/Receipt/Mode noch nicht admitted |
| Isolation | Kein Worker nötig für Current | `dotnet`-Child; Main lädt Worker/Roslyn nicht zur Analyse; Image-Pinning per PE/Hash, nicht Assembly.Load |
| Auswahl | Exakte validierte Current-Version | Exakte validierte Historical-PDB-Version, keine Präfix-/Range-Heuristik |
| Fehler | Kein künstlicher leerer Complete-Result | Failure ohne Result/Receipt, echte validierte Identity bleibt erhalten |
| Fallback / Force | Kein Current-Fallback für externe Historical-Jobs | Kein Force-Historical; `--force-historical` bleibt ungültig |

Die vorhandenen Wire-Records können Solution-Graph, unabhängiges Direct-Result,
Root-Dokumentfacts, Canonical-Types und echte Identity bereits ausdrücken.
Nicht vorhandene Zulassung ist nicht fehlende Datenrepräsentation. Für den
unten begrenzten Slice ist kein neuer Analyzer-/Result-Wire-DTO nachgewiesen
nötig; additive Profile-/Mode-Validierung und Main-lokale immutable Receipts
sind dagegen tatsächlich erforderlich. B21 bleibt lokale Trusted-Client-/
Worker-Image-Autorität, keine Behauptung über kompromittierte OS-/Runtime-Codepfade.

## 7. Finding-Policy EXACT / OUTSIDE / FAILURE (R4)

| Ergebnis | Current heute | Historical heute / verbindlich für B25B |
|---|---|---|
| EXACT / admitted / vollständig | Normale Main-Policy; Direct DOC610 vor Summary DOC611; dokumentierte exakte Typen und Direct-Typen nicht doppelt als transitive Missing-Typen ausgeben | Nur nach beiden erforderlichen echten Executions, kompletter Owner-/Doc-/Graph-Receipt und Proven/untruncated Result darf dieselbe Policy laufen |
| OUTSIDE PROFILE, positive Constructor-Zyklen | Certificate `Complete=false`, `NormalFindingAdmissionBlocked=true`; niemals DOC610/DOC611/DOC632 aus diesem transitive Ergebnis | Worker `UnsupportedContext`, kein Result/Receipt/Finding-Import; keine Partial-/Local-/Current-Ersatzantwort |
| Unvollständige Coverage, externe XML-Evidenz, Unknown Residual | Keine exact Missing-/Extra-Findings; nur erlaubte Diagnostik | Fail closed; kein generisches Residual-DTO und keine erfundene RuntimeAwait-/Complete-Information |
| Trunkierte Projektion | Explizit uncertified, selbst wenn Kandidaten/Pfade vorhanden sind | Kein Complete-Erfolg, ResponseValidation weist zusätzlich Unsicherheit/Truncation zurück |
| Transport-/Identity-/Scope-/Workerfehler | Keine Übernahme eines fremden/erfundenen Resultats | Keine Findings, keine Reporter-Ausgabe als erfolgreicher Check, keine Receipt |

Wichtig: Der Current-OUTSIDE-Zweig darf tatsächlich **DOC631** für passende
unentschiedene dokumentierte Typen erzeugen und unabhängig gebundene XML-
Diagnostik **DOC660/DOC670** erhalten. Er muss nicht für jeden OUTSIDE-Root ein
Finding erzeugen: bewiesene Ground-Lower-Witnesses können eine ungerechtfertigte
Uncertainty-Meldung verhindern. Diese Witnesses berechtigen aber nie zu DOC611
oder DOC632. „OUTSIDE heißt alle Findings weg“ wäre genauso falsch wie
„leere Findings heißen exact complete“.

Historical hat derzeit keinen Erfolgskanal für diese unvollständigen
diagnostischen Ergebnisse. B25B soll **keinen** neuen Diagnose-Import eröffnen,
sondern den bisherigen expliziten Fehlervertrag behalten. Das ist die engere
bestehende externe Finding-Admission, keine Historical-Sondersemantik des
shared Analyzers. Sie stimmt mit A10/R7–R10/BRC und RuntimeAwait-Certificate
überein. DOC630 bleibt Direct-spezifisch; cross-project Declared wird nicht
als Solution umbenannt.

Der bestehende, **nach** vertrauenswürdiger Direct-/Root-/Dokument-Admission
ausgewertete No-doc-Guard darf weiterhin Summary überspringen und keine
Findings zurückgeben. Das ist keine Aussage, dass eine nicht angeforderte
Solution-Summary exact complete wäre. B25B darf weder diese Guard-Semantik
ändern noch den Guard vor die notwendige Input-/Receipt-Zulassung verlegen.

### Owner, Kardinalität, Dedup und Location

- Reporting-Owner ist der ausgewählte Root A; Missing-Findings verankern A's
  Member, Tag-Probleme dessen XML-Tag. Ein Throw in B/C verschiebt keine Location.
- Exception-Type-Owner bleibt kanonische Assembly/Module/Containing/Arguments-
  Identity; Source-Projekt und Reporting-Projekt sind davon getrennte Fakten.
- Gleicher Typ über mehrere Calls/Pfade: shared Policy/HashSets deduplizieren
  Typen, nicht ein Finding pro Pfad. Zwei gleichnamige Typen aus P0/P1 bleiben
  dagegen zwei Identitäten. Der A10-B25-Witness prüft zwei DOC611.
- Der öffentliche `Finding` enthält **keinen Assembly-Owner-Key**. Zwei Owner
  können gleich aussehende Displaytexte, Locations und Finding-Inhalte haben.
  Daraus darf kein nachträgliches Display-/Name-/Location-Dedup entstehen.
  Vollständige Ownerbindung muss im zugehörigen Canonical-Receipt/Witness belegt
  werden, nicht durch eine erfundene Zusatzbehauptung über das Finding-DTO.
- `ToolRunner` gruppiert nach Reporting-Datei, zählt jeden Eintrag genau einmal;
  JSON fügt jeden Finding-Datensatz hinzu, SARIF projiziert jeden Eintrag.
  SARIFs `DistinctBy(smell.ID)` betrifft nur Regelmetadaten, nicht Findings.
  Positive B25B-CLI-Witnesses müssen diese Kardinalität trotzdem tatsächlich belegen.

## 8. Witness-/Coverage-Matrix und noch offene Aktivierungsbeweise (R6)

Dateien T1–T10 siehe Quellenregister. „Vorhanden“ bezeichnet statisch geprüfte
Tests und erhaltene A10-Ergebnisse, nicht einen neuen Lauf in diesem Review.

| Contract | Current Test / Witness | Historical Test / Witness | Production Path | Status für B25B |
|---|---|---|---|---|
| Assembly-qualified owner separation | T1 `AssemblyDistinctSameDeclarationId...` + T2 Normalizer-Witness | T1 echter `Flow` und Source-/Summary-Owner P0/P1 | Session + Worker, Current Finding-Policy | Analyzer BOUNDED-CLOSED; finale Historical-Adoption fehlt |
| P0/P1 / same-name types / zwei Findings | T1 genau zwei volle Owner und zwei DOC611, volle P1-Auswahl | T1 genau zwei Canonical Entries; finale Findings ausdrücklich abgewiesen | Echter Current Detector vs Historical Flow | OPEN positiver Historical-Finding-/Reporter-Witness |
| Generic method arguments | T3 method/both; T5 finite constructions | T4 `StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness` jetzt mit konkreten Erwartungen | Session/Worker, nicht nur Compiler-Model | BOUNDED-CLOSED Analyzer; B25B-integrierter Witness fehlt |
| Generic containing / nested owner | T2 K17/R5/P12; T3 containing/both/nested | T4 geschlossene `Outer<int>.Leaf` / `Outer<string>.Leaf` | Canonical resolver + echter Worker | BOUNDED-CLOSED; generic Result-/Doc-Typen außerhalb erstem Finding-Slice |
| Endliche gegenseitige Rekursion | T6 unabhängiger Reachability-Oracle / Entry-/Edge-order | T6 `GenuineWorker_UsesIndependentlyProvenFiniteClosure` | Echte Session und Worker | BOUNDED-CLOSED; activation case mit Root-XML ergänzen |
| Growing recursion → OUTSIDE | T5 `Production_GrowingRecurrenceIsRejectedBeforeRootMaterialization` | T3 growing Worker; T5 `Diagnosis_ClassifiedFailurePreservesActualValidatedIdentity` | Session P1 + Worker/ResponseValidation | CLOSED Ablehnung; integrierter Solution-Finding-Negativfall fehlt |
| Catch<int> vs Catch<string> | T2 generischer Vergleich; T5 konkrete Produktion; Zusatzproof BaseType-Catch | T4 belegt konkrete Typidentitäten; `HistoricalWorkerProcessTests.RealCatchTransfer_SuppressesOnlyTheCaughtProvenException` belegt einen echten nongenerischen Catch, NICHT das generische Paar | Filter + Evaluation | Shared primitive BOUNDED-CLOSED; dedizierter genuine-Historical-Generic-Catch- sowie cross-project Finding-catch-Witness OPEN |
| Cross-compilation Rebinding | T3 SupportingSourceResolver; T5 caller-scoped substitution; zusätzlicher echter A→B→C-Proof | T1/T9 echte nongeneric Source-Projekte + Canonical parity | PE/source environment / Session | BOUNDED-CLOSED in gezeigten Slices; kein behaupteter generischer Historical A→B→C-Finding-Beweis |
| Incomplete syntax | T5 `Production_IncompleteRecoveryRootIsClassifiedBeforeCanonicalizationOrRegistration` | `HistoricalWorkerProcessTests.CompilationErrors_AreStructuredAndNeverSuccessful` | Session bzw. Worker-Reconstruction | CLOSED fail-closed; keine neue positive Activation-Evidence |
| Externe Dokumentation | T8 `ExternalDocumentationContract_IsTransitiveOnly` / CoverageOutside | Historischer vollständiger Proven-Result-Vertrag weist unvollständige Flows zurück | Native Detector vs Worker-Failure | BOUNDED-CLOSED Ablehnung/Diagnostik, kein externer XML-Finding-Import |
| Multi-project cardinality / dedup | T1 chain/multiple/supporting-sources und Same-name-Fact | T1/T9 exakte Graph-/Source-/Result-Differentials | Current echte Findings; Historical Flow | OPEN finale Historical-Mengen-/Reporter-Kardinalität |
| Finding ownership / location | T1 Native-Findings alle am Root-Pfad; C10 Anchor-Code | T10 B24 volle lokale Finding-/Doc-Vergleiche | Shared Main-Policy lokal echt | OPEN volle Solution-Finding-Location/Context/Owner-Receipt |
| Diagnostic vs exact findings | T8 CoverageOutside und ProductionIntegration `OpenInterfaceDispatch_ProducesDoc631` | T5 klassifizierter Fehler ohne Result/Receipt | Native Outcome vor Policy / Worker vor Canonicalization | CLOSED aktuelle Trennung; integrierter Solution-CLI-Negativbeweis fehlt |
| Worker error / UnsupportedContext | Erwartete A9-Klassifikation im Current Oracle | T5 Failure-Identity-Negative; T10 falsches physisches Worker-Image/Workerfailure | Echter Client/Validator, isoliert | CLOSED bestehender Mechanismus; bei graphgebundener Receipt erneut prüfen |
| Finaler Solution-Finding-E2E | T1 echter Native Detector; keine generelle Runtime-Path-Owner-Deduktion | T1 `AssertNoFindingAdmission` und `RealCli_SolutionJobRemainsUnsupported_NoReportOrEmptySuccess` | Bewusst vor Dispatch/Reporter geschlossen | OPEN, nicht durch Parität oder Full ersetzt |
| Nichtleere unabhängige Direct- plus Supporting-Summary-Evidence | Lokale T10 mixed/combined-Witnesses | T10 B24 lokale echte zweistufige Findings | Single-project-Finding-Pfad aktiv | OPEN Solution-S3; B25s alter mixed-Witness bewies gerade keine nichtleere Direct-Evidence |
| Graphgebundene B21-Receipt über beide Stages | B25 historische Trust-Analyse, C12 Request/Response-Bindung | T10 lokale Replay-/Copy-/Wrong-Worker-Negative | Whole-request Token vorhanden, Solution-Finding-Receipt fehlt | OPEN gezielter Adapter-/Stage-Kohärenzbeweis |

Die OPEN-Einträge sind konkret die zu liefernden Aktivierungs-Witnesses des
nächsten Pakets, kein behaupteter zusätzlicher A10-Analyzer-Defekt. Daher
GO zur begrenzten Implementierung, **kein GO zu unbewiesener Aktivierung**.
Wenn das nächste Paket sie ohne neue Analyzer-/Identity-/Trust-Semantik nicht
liefern kann, muss es geschlossen bleiben und gesammelt STOP berichten.

## 9. Versteckte Characterization / notwendige nächste Freigaben (R7)

1. **T1 B25:** Die 15 Fälle `ExistingB15FlowGraphAndCurrentFindings...` haben
   echte Current-Findings/Session- und Historical-Flow-/Graph-Prüfungen, aber
   `AssertNoFindingAdmission` verlangt weiterhin vor Dispatch Rejection. Derselbe
   Helper wird im Same-name-Owner-Fact verwendet. Das sind reale closed-boundary
   Assertions, keine positiven Historical-Finding-Tests.
2. **T1 CLI:** Drei Fälle `RealCli_SolutionJobRemainsUnsupported_NoReportOrEmptySuccess`
   verlangen Exit4, keinen Report und `not admitted`. Sie müssen für tatsächlich
   neu admitted Jobs später gezielt neu beschlossen werden. Heute enthalten
   diese Jobs noch keine vollständigen Supporting-Materialien; bloß die gleiche
   alte Datei als positiven Job zu deklarieren wäre falsch.
3. **T1/T2 Source Guards:** `StopRule_DoesNotIntroducePolicyRoutingAttestationOrPidWorkarounds`
   und `StopRule_LeavesOriginalProductionAndClosedFindingAdmission` prüfen
   weiterhin `Solution is not admitted` / fehlende `SupportingProjects` im
   ExternalTarget-Code. Diese beiden konkreten Source-Assertions brauchen beim
   zulässigen Adapter eine bewusste Architektur-Neufassung. Ihre PID-/Isolation-
   und exakten Normalizer-Assertions bleiben relevant und sind nicht pauschal frei.
4. **T10 B24:** `RealCli_ConflictingOrUnsupportedJobMode_FailsClosed`
   (`solution-transitive`, kein CLI-Konflikt) lehnt einen unvollständigen lokalen
   Job ohne Solution-Materialien ab. Das soll weiter scheitern. Nicht jede
   Solution-String-Rejection muss zu einem positiven Test geändert werden.
5. **B23:** `GenuineCli_RejectsOtherHistoricalFindingModes` testet Legacy-CLI-
   Mode-Overrides, nicht den künftig expliziten Solution-Job. Unveränderte
   Legacy-/Conflict-/Force-Historical-Rejections bleiben gültig.
6. **A-/A2-/A3-Namen:** Einige Methoden-/XML-Kommentare nennen noch „Loss“,
   „DefinitionFold“ oder „NotCorrectness“, obwohl die genehmigten Assertions
   inzwischen exakte Construction bzw. klassifiziertes OUTSIDE verlangen.
   Ausschlaggebend sind Assertions/Entry-Pfad, nicht der historische Name.
   Compiler-/Raw-Graph-Tests bleiben Low-Level-Tests, keine Production Admission.

Hier wurde keine dieser Assertions umgeschrieben und keine frühere A10-Freigabe
auf B25B übertragen. Das nächste Paket muss seinen engen Characterization-
Änderungsumfang ausdrücklich festlegen; keine opportunistischen Kardinalitäts-,
Message-,Location-,Identity- oder Worker-Failure-Anpassungen.

## 10. Explizite B25B-Grenzen (R8)

Das GO ist die Schnittmenge aus ursprünglichem B25-Slice, vorhandener äußerer
Finding-Admission und `A9-FiniteOrbit-v1`, **nicht** die Vereinigung aller jemals
geprüften Optionen:

- Ein explizit ausgewählter statischer, parameterloser, nongenerischer Ordinary-
  Root mit Body in nongenerischem Owner; ein Reporting-Projekt A.
- Original validierte PE/PDB/Source/Reference/Parse-Handoffs pro Projekt;
  vollständige Source-Membership; unsigned/exakte Referenzwelt und weiterhin
  unterstützte Compiler-/Options-/Language-Profile, keine Discovery-Rekonstruktion.
- Zunächst A→B und A→B→C als zwei-/dreiprojektige Ketten mit global eindeutigen
  LogicalPaths. Der vorhandene Worker-Validator kann bis vier DAG-Knoten
  darstellen; das ist **kein** pauschales GO für vier Knoten, Branches/Diamonds
  oder allgemeine Solution-Graphen. Projektzyklen und linked/shared Pfade bleiben
  ausgeschlossen. Endliche **Callable**-Rekursion ist davon zu unterscheiden.
- Finite exakte generische interne Calls dürfen nur unter derselben P1/P2-
  Admission laufen. Method-/Containing-/Nested-Arguments bleiben vollständig
  erhalten, auch wenn der finale Finding-Typ nongenerisch ist. Keine Abkürzung
  über Definitions-Keys und keine pauschale „alle Generics sind unsupported“-Änderung
  der gemeinsamen Analyzer-Semantik.
- Die erste Finding-Adoption bleibt für vollständig nominale **nongenerische
  Flow-/Doc-Typen samt nongenerischen Containing-Ownern**, die im behaltenen
  ursprünglichen Root-PE-Symbolraum eindeutig bindbar sind. C15 `TryType` akzeptiert
  heute keine Generic-Argumentlisten/Arity-Metadata und prüft am Ende die volle
  Identity. A10s weitergehende Generic-Flow-Fähigkeit öffnet diese äußere
  Finding-Typgrenze nicht automatisch. Erweiterung wäre ein anderer Scope.
- Result-Typ-Owner muss im Destination-/Original-PE-World eindeutig vorhanden
  sein; ein fehlender transitive Type-Owner wird nicht aus Displayname/Pfad
  ergänzt. Catch in Consumer und Supporting-Body verwendet unveränderte exakte
  Construction-/BaseType-Semantik.
- Nur vollständige, Proven, nicht trunkierte Ergebnisse; OUTSIDE/Unknown/
  fehlende Scope-/Syntax-/RuntimeAwait-/Dokument-Coverage bleibt sichtbarer
  Failure ohne normale Findings. Keine neue Historical-Diagnose-Adoption.
- Normale Solution-Main-Policy DOC610/DOC611/DOC632 und unabhängig validierte
  Tag-Diagnostik DOC660/DOC670; `inheritdoc`, genaue Dokumenttypen und Direct-
  Deduplizierung unverändert. Keine cross-project Declared-Aktivierung.
- Keine zweite Analyzer-Engine/Type-Normalisierung/Finding-Policy, keine
  Routing-/Version-/B21-/PID-Umgehung, kein Current-Fallback, kein Force-Historical,
  kein allgemeiner Workspace-/MSBuild-/NuGet-/Source-Discovery-Aufbau.

## 11. Genaues nächstes Paket und Abschlusskriterien

**B25B – Bounded Solution Root Finding Adoption**, ein zusammenhängendes Paket:

1. Additiven, eindeutig benannten Solution-Finding-Profile-/Mode-Opt-in mit
   den obigen Grenzen definieren; alte Single-Project-/Declared-Profile und
   Legacy-Jobs nicht still umdeuten. Bestehende Graph/Doc/Direct/Identity-Shapes
   nutzen; kein neuer Result-Wire-DTO ohne tatsächlich nachgewiesene Lücke.
2. Originale lokale P4/P5/B12/B14-Inputs zum finalen zweckgebundenen Graph
   komponieren, dessen Root-/Target-Node-Kohärenz erhalten, erst dann die
   Dokument-Origin registrieren. B21s ganze tatsächliche Execution verwenden.
3. Immutable Main-Finding-Receipt um volle Graph-/Source-/Reference-/Owner-
   Stage-Bindung ergänzen. Direct/Summary getrennt ausführen, denselben Root-/
   Doc-/Direct-Vertrag nachweisen. Lokales `Scope` allein genügt nicht.
4. Ohne Declared-Inventarfilter in dieselbe Main-Finding-Policy eintreten;
   P0/P1 getrennt erhalten, supporting source nicht zum Reporting-Ziel machen,
   keine neue Display-/Location-Dedup-Phase. OUTSIDE/Failure vor Rendering abweisen.
5. Explizite lokale CLI-Materialien für alle Kettenknoten über bestehende P4/P5-
   Eigentümer aufnehmen; keine automatische Akquisition. Erst nach erfolgreicher
   kompletter Operation den bestehenden RunResult/Console/JSON/SARIF-Pfad verwenden.
6. Alle OPEN-Witnesses der Matrix liefern: echte positive Historical-Solution-
   Findings gegen unabhängig richtige Current-Owner/Typen, nicht nur Parität;
   P0/P1 zwei Findings durch alle Reporter, same-type mehrere Pfade nur einmal,
   volle Location/Context/Tag-Anchor, A→B/A→B→C, Supporting-Source-Catch und
   Consumer-Catch, finite interne Construction, nichtleere unabhängige Direct-
   plus transitive Summary, exact/base/invalid/inheritdoc/no-doc/empty.
7. Negative Integrated-Witnesses: growing/unknown/external-doc/incomplete syntax,
   fehlende Scope/PE/Type-Owner, Stage-Graph-/Reference-/Root-/Doc-Substitution,
   copied/replayed Receipt, falsches physisches Worker-Image, Worker-Fehler und
   UnsupportedContext; kein Result/Finding/Report als Erfolg. Root-Finding-
   No-doc-Guard darf keinen unvalidierten Job oder erforderliche Execution umgehen.
8. Vor diesem Paket gezielte Freigabe für die unter Abschnitt9 konkret
   betroffenen Closed-Admission-Assertions einholen/festlegen; alle übrigen
   Assertions unverändert. Danach die vorgesehenen bestehenden Regressionen,
   Dual/WAE/Current-Baselines und einen neuen B25B-Evidence-Gate gegen den
   tatsächlichen Implementierungsstand ausführen. Kein Grün-Retry, kein
   Schema-/Identity-/Failure-Weichzeichnen als Ersatz für den Beweis.

Das ist keine automatische Freigabe zum Commit/Push und keine B25B-Implementierung
dieses Reviews. Schlägt der volle Vertrag fehl, bleibt Finding-Admission
geschlossen; ein lokaler Teil-Erfolg ist keine B25B-Abschlussmeldung.

## 12. Quellenregister: konkrete Dateien / Entry-Methoden

Code:

- C1 [ExceptionFlowSummaryAnalysisSession.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryAnalysisSession.cs), `Analyze`, `Outside`; Root Admission vor Registrar, Ergebnis-/Projection-Grenze.
- C2 [ExceptionFlowSummaryGraph.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraph.cs), `BeginProduction`, `RegisterTarget`, `GetOrAdd`, `EndProduction`; P2 und Rollback neuer Contexts.
- C3 [ExceptionFlowSummaryGraphEvaluator.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs), `Evaluate`, `FindSummaryComponents`, `CompleteSummaryEvaluationFrame`, `NormalizeSummaryExceptionType`.
- C4 [RoslynCanonicalIdentityResolver.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/Canonical/RoslynCanonicalIdentityResolver.cs), `ResolveNamedType`, `TryMatchNamedType`, `ResolveCallable`, `TryConstructMethod`.
- C5 [ExceptionFlowInvocationSubstitution.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowInvocationSubstitution.cs), `Bindings`, `Binder`, `Apply`, `ApplyCallable`.
- C6 [ExceptionFlowCallableKey.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowCallableKey.cs), `Equals`, `GetHashCode`, `ConstructionKey`; [ExceptionFlowCallContext.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowCallContext.cs), `InvocationSymbol` / CanonicalContext.
- C7 [ExceptionFlowCrossCompilationResolver.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowCrossCompilationResolver.cs), `ResolveMethod`, `ResolveInvocationType`, `ResolveNamedType`.
- C8 [ExceptionFlowFiniteOrbitAdmission.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowFiniteOrbitAdmission.cs), `Discover`, `InspectDeclaration`, `DiagnoseGroundFacts`, Seal `ContainsTransition` / `ContainsContext`; [ExceptionFlowFiniteOrbitProfile.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowFiniteOrbitProfile.cs), `Check`, `Certificate`.
- C9 [ExceptionFlowCaughtTypeFilter.cs](../src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowCaughtTypeFilter.cs), `Suppresses`, `IsSameOrDerivedFrom`.
- C10 [XmlDocExceptionSemanticDetector.cs](../src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.cs), `FindExceptionSmells`, `AnalyzeConfiguredExceptionFlow`; [Findings.cs](../src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.Findings.cs), `AddExceptionFindingsCore`, `AddDocumentedTransitiveFindings`, `AddMissingExceptionFindings`.
- C11 [HistoricalWorkerPayloadProjection.cs](../src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs), `ProjectForFindings` / `Project`; [HistoricalWorkerSolutionProjection.cs](../src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerSolutionProjection.cs), `Project`.
- C12 [HistoricalWorkerResponseValidation.cs](../src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs), `Validate`, `TryGetFindingFacts`, `ValidCanonicalResult`, `ValidIdentity`; [HistoricalWorkerClient.cs](../src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs), `AnalyzeAsync`, `TryConsumeDocumentationExecution`, `ReadWorkerImage`.
- C13 [WorkerProtocol.cs](../src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs), `ReferenceWorldHash`, `WorkerFindingContract.HasSupportedRequest`, `WorkerInputValidation.HasSupportedSolution`; [HistoricalAnalysis.cs](../src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs), `Analyze` / `Reconstruct`.
- C14 [ExternalTargetExecution.cs](../src/XMLDocNormalizer/Execution/ExternalTargetExecution.cs), `ExternalTargetJob`, `AnalyzeAsync`; [ExternalFindings.cs](../src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs), `FindExternalExceptionSmellsAsync`, `FindIndependentExternalExceptionSmellsAsync`, `RenderBoundedFindingEvidence`.
- C15 [HistoricalWorkerDeclaredDocumentationProjection.cs](../src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerDeclaredDocumentationProjection.cs), `TryProject`, `Register`, `TryValidateFinding`, `TryValidateBindings`, `TryType`.
- Dispatch/Reporting: [ExceptionFlowAnalysisDispatch.cs](../src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisDispatch.cs), [SelectionPolicy.cs](../src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalyzerSelectionPolicy.cs), [Router.cs](../src/XMLDocNormalizer/Execution/Analysis/ExceptionFlowAnalysisRouter.cs), [ToolRunner.cs](../src/XMLDocNormalizer/Execution/ToolRunner.cs), [JsonFindingsReporter.cs](../src/XMLDocNormalizer/Reporting/Json/JsonFindingsReporter.cs), [SarifLogBuilder.cs](../src/XMLDocNormalizer/Reporting/Sarif/SarifLogBuilder.cs).

Tests / Proofs:

- T1 [ExceptionFlowHistoricalSolutionFindingTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs).
- T2 [ExceptionFlowAssemblyQualifiedOwnershipTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs).
- T3 [ExceptionFlowConstructedInvocationContextTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs).
- T4 [ExceptionFlowCanonicalExceptionIdentityTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs).
- T5 [ExceptionFlowFiniteOrbitAdmissionTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowFiniteOrbitAdmissionTests.cs).
- T6 [ExceptionFlowRecursiveSummaryClosureTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs).
- T7 [ExceptionFlowGenericExpansionBoundaryTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowGenericExpansionBoundaryTests.cs).
- T8 [ExceptionFlowExternalDocumentationTests.cs](../Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowExternalDocumentationTests.cs), [ProductionIntegrationTests.cs](../Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowSummaryGraphProductionIntegrationTests.cs), [FiniteOrbitAssertions.cs](../Tests/XMLDocNormalizerTests/Helpers/ExceptionFlowFiniteOrbitAssertions.cs).
- T9 [ExceptionFlowHistoricalSolutionScopeTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs), `Native` ruft Session und echten Current-Detector; [B15SolutionProbe.cs](P5O2B7Proof/HistoricalPdbFixture/B15SolutionProbe.cs) / [B14ScopeProbe.cs](P5O2B7Proof/HistoricalPdbFixture/B14ScopeProbe.cs) laufen im isolierten Testprozess und rufen `Session.Analyze`, nicht den Raw-Builder.
- T10 [ExceptionFlowHistoricalBoundedFindingTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs), `GenuineModeContract_FullNativeFindingEvidenceAndRepeat`, Trust-/Replay-/CLI-Negative.
- Worker/Dispatch: [HistoricalWorkerProcessTests.cs](../Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerProcessTests.cs), [ExceptionFlowAnalysisDispatchTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs), [ExceptionFlowExternalTargetCliTests.cs](../Tests/XMLDocNormalizerTests/Worker/ExceptionFlowExternalTargetCliTests.cs).
- Zusätzlicher realer Current-Proof: [FinalClosure/Program.cs](P5O2B25A10Proof/FinalClosure/Program.cs), Ergebnisse [production-proof.json](P5O2B25A10Proof/FinalClosure/IndependentResults/production-proof.json).
- Geteilte physische Sources: [ExceptionFlow.HistoricalSources.props](../build/ExceptionFlow.HistoricalSources.props), 125 Include-Einträge; kein kopierter zweiter Analyzer.

## 13. Abschluss / Schutzstatus

Keine H-CLEAN-Datei musste entfernt werden; historische A10-Evidence ist
vollständig erhalten. Ein Cleanup-Commit ist derzeit nicht nötig. Die
optionale Dubletten-/Archivdiskussion ist von B25B-Korrektheit getrennt.

Der nächste konkrete Schritt ist der begrenzte B25B-Auftrag aus Abschnitt11
einschließlich expliziter Characterization-Freigaben und der fehlenden
positiven/negativen Aktivierungs-Witnesses. In diesem Lauf bleibt B25B geschlossen.

Der einmal beobachtete native CLI-Absturz `0xC0000005` bleibt separat ungeklärt.
Keine neue Evidence verknüpft ihn mit dem Produktpfad; daher hier kein A10-/B25B-
Blocker. Nicht als Flakiness oder behoben bezeichnet; keine neue Prozessausführung
in diesem Review als angeblicher Reproduktions-/Heilungsbeweis.

Finaler erwarteter und per Final-Capture geprüfter Status:

```text
 M ../../.gitignore
?? Evaluation/P5O2B25B-readiness-review.md
?? Evaluation/P5O2B25BReadiness/
```

HEAD, Root-Ignore, physischer/semantischer Index, 23 Core-Anfangsdateien,
sechs Stashes und 507 historische B25–A10-Evaluation-Dateien unverändert.
Keine historischen Reports/Gates/Hashes umgeschrieben. Keine Builds, Full/Broad/
Boundary-/Dual-Retries oder neuen Laufresultate. Kein Commit, Push, Force-Push,
Rebase, History-Rewrite, Restore oder Stash-Eingriff. Danach STOP.
