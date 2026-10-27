# Analysis Coverage / Completion – read-only preflight

Baseline: `aef86f86559fbcc7c355389a407857ed67e5bcfa`,
`Document post-B25B closure review`, branch `fix/documentation`.
Eingang: ausschließlich geschützter Root-Ignore-WIP, Index semantisch leer.
Vor diesem Dokument wurden keine Produktions-/Testdateien geändert und keine
Tests gestartet. Historische Closure-Zahlen sind keine neuen Validationsergebnisse.

## Vorhandene Informationswege

| Information | Entstehung / Typ | Lebensdauer / finaler Pfad | Aktuelle Ausgabe / Verlust |
|---|---|---|---|
| EXACT / v1 | `FiniteOrbitAdmission.Discover`, `Certificate.Complete`; nach Evaluation `ExceptionFlowAnalysisResult.ProfileOutcome` | Current behält nativen Result bis Detector; Session setzt erst nach exakter Evaluation/Projection Complete | Detector gibt nur Findings zurück, RunResult verliert Status |
| OUTSIDE | Dasselbe Certificate, `Reasons(Category,Witness)` | Current nativer Result; Historical `WorkerFailure(UnsupportedContext, Details)` | Current nur ggf. DOC631; Historical typisierter Fehler bis ExternalTargetExecution, dort Message-Flattening |
| TargetOrAnalysisCoverageIncomplete | Admission/konkrete Evaluation, Certificate-Reason | Bis nativer Result bzw. Worker-Details | Kein globaler Runstatus |
| UnsupportedContext | WorkerFailureCode; valide Failure-Envelope in Main geprüft | `HistoricalWorkerCallResult.Failure.WorkerFailure` mit tatsächlicher validierter Identity | Kein erfolgreicher Result/Receipt; CLI Exit4, Fehlertext |
| Unknown Residual | Fragment-/Result-Uncertainty, `UncertainTargets` / `HasUncertainPaths`; canonical `Uncertainties` | Native Result bzw. kanonisches Result; Historical lehnt nichtleere Uncertainties ab | DOC631 nur für passende Tags; Worker strukturiert failure |
| External XML | `ExternalDocumentationEvidenceExceptions`, canonical EvidenceKind.ExternalDocumentation, dokumentierte Attestation | Getrennte Evidence, kein Body-Throw-Beweis; gebundene Dokumentationsfacts bis Finding-Policy | Dokumentationshinweis ggf. DOC631; keine separate Coverage im RunResult |
| Worker technical failure | ClientFailureCode StartFailure/WorkerCrash/NonZeroExit/TransportFailure/Timeout/Cancelled etc. | Vollständiger Main-lokaler CallResult, kein erfolgreicher Analyse-Result | ExternalTargetExecution reduziert auf Exception-Message; Exit4 |
| Validation failure | ProtocolMismatch/MalformedResponse/IdentityMismatch/IncompleteResult, Main-Response-Validation | Failure statt Result/Receipt | CLI Fehler, kein normaler Report |
| Fehlende Source/Metadata | Source-/Reference-/Scope-/Payloadprojektion; fehlender SemanticModel/System.Exception; unresolved body/target | Staged failure oder Certificate/Uncertainty; einige Workspace-null-Pfade überspringen | Warnings/Fehler oder verloren; keine sichere globale Complete-Aussage |
| Incomplete syntax/binding | Parse-/Compilationdiagnostics; Admission prüft Compilation-Errors außer CS5001 | Im Summary-Certificate; Direct besitzt kein A9-Certificate | Current Summary fail closed, aber RunResult verliert Grund; Direct muss vorhandene Diagnostics auf Produktebene beachten |
| Result completion | Native ProfileOutcome; canonical Uncertainties/PathsTruncated | Canonical DTO hat kein zusätzliches Profile-Certificate; Worker lehnt vorher OUTSIDE und danach incomplete ab | Valider erfolgreicher Worker-Result ist bereits geprüft; neues Wire-Feld nicht notwendig |
| Diagnostic findings | DOC631/640 und normale cref-Diagnostik | Finding-Listen/RunResult/Reporter | Kein Ersatz für Completion; Null Findings ist kein Coverage-Beweis |
| Workspace skips | ToolRunner und ProjectClosureSemanticContextBuilder: fehlendes Projekt/Compilation/FilePath/Tree; WorkspaceFailed | Aktuell teilweise nur Warnung/continue | Scope-Verlust muss am vorhandenen Branch gemeldet werden, ohne erneuten Graphlauf |
| Datei/Ordner | ToolRunner.RunCheck/RunFix ohne semantisches Workspace | Nur Syntax/XML, obwohl Exception-Modus konfiguriert | Darf nicht als vollständig semantisch untersucht dargestellt werden |
| Console | `RunResultConsoleReporter` | Hat RunResult, bisher nur Finding-Zähler/Statistik | `Check succeeded` allein unterscheidet Coverage nicht |
| JSON | JsonReport + IResultAwareFindingsReporter.Complete(RunResult) | Metrics/Findings vorhanden | Additives eigenes `Analysis`-Objekt möglich |
| SARIF | SarifRun.Properties (bereits metrics) | Run-Propertybag vorhanden | `properties.analysis` möglich; keine neue Rule/Result erforderlich |
| Comparison | ModeResult.Clone, Child-JsonReport, DTO/RepresentativeResult | Findings/Metrics bleiben, neues Feld muss Clone und Modeausgaben überleben | Kein Coverage-Nachweis aus Timing oder Findingcount ableiten |

Maßgebliche Quellen sind die gleichnamigen Dateien unter `src/XMLDocNormalizer`,
`src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs`, `WorkerProtocol.cs`.
Der productive Entry bleibt Session.Analyze; weder neues Graphwalking noch neue
Admission-/Catch-/Finding-Regeln sind erforderlich.

## Normativer minimaler Produktvertrag

Zwei unabhängige Achsen, getrennt von Findings:

- Execution: `NotRun`, `Succeeded`, `Failed`.
- Completion: `NotAssessed`, `Complete`, `Unsupported`, `Incomplete`.

`Succeeded` bedeutet abgeschlossene kontrollierte Ausführung, nicht exakte
Vollständigkeit. OUTSIDE/unsupported kann daher Execution=Succeeded haben.
`NotAssessed` ist der sichere Default eines bloßen `new RunResult()` ohne
Analysebeleg. Ein technischer Fehler hat Execution=Failed und niemals Complete.
`Unsupported` bezeichnet ein explizit abgelehntes Profil; fehlende Coverage/
Unknown/fehlende Inputs innerhalb des gewählten Scopes sind Incomplete.
Bei Kombination beider gilt Incomplete mit beiden Reason-Code-Klassen.

`Complete` gilt nur für den gewählten Reporting-/Supporting-Scope, konfigurierte
Regeln/Filter und den jeweiligen Modus. Direct behauptet keine transitiven
Body-Effekte. Declared nutzt unverändert seinen Reporting-Type-Filter und den
vorhandenen No-inventory-/No-summary-Guard. Member ohne XML-Dokumentation
erhalten keine neue Flow-Analyse; Complete ist kein unaufgeforderter Beweis
über bisher nicht analysierte Member. Der externe No-doc-Guard zertifiziert
die ausgeführte Dokumentationspolicy, keine nicht ausgeführte Summary.

Aggregation: monotone Vereinigung stabiler Reason Codes und Zustandsflags;
Failed dominiert Execution, Incomplete dominiert Unsupported, Unsupported
dominiert Complete. Nicht bewertete Komponenten dürfen nicht als vollständige
Belege eingesetzt werden. Reihenfolge und Finding-Kardinalität sind irrelevant.
Keine optionalen Target-Zahlen erfinden: bestehende Pfade zählen unterschiedliche
Einheiten. Scope und Modus werden stattdessen ausdrücklich ausgegeben.

Native Summary-Certificates sind maßgeblich. Native Direct-Ergebnisse werden
aus ihren bestehenden Uncertainties/Truncation und vorhandenen Source-/Binding-
Diagnostics beurteilt; keine neue A9-Zulassung für Direct. Externe erfolgreiche
Findings dürfen nur den unveränderten bisherigen Admission-Pfad passieren.
Main-lokale Produktabbildung beobachtet bestehende Outcomes, besitzt keinerlei
Authority zur Mintung von Receipts oder Normalisierung von Findings.

## Ausgabe und Kompatibilität

JSON: zusätzlich `Analysis` mit Execution, Completion, Scope, Mode, ReasonCodes.
Alle bisherigen Felder und Findings bleiben erhalten. Keine Certificates,
Roslyn-Objekte, Graphen oder Worker-Interna veröffentlichen. Fehlendes Analysis
in altem JSON bedeutet nicht nachträglich Complete.

SARIF: dasselbe Modell unter `runs[].properties.analysis`; Serialization folgt
dem bestehenden camelCase-Writer. Keine neue Finding-ID und keine Umdeutung
der `results`. Console: zusätzliche kurze menschenverständliche Coverage-Zeile;
bei incomplete/unsupported kein uneingeschränktes `Check succeeded`.

Exit-Vertrag unverändert: 0 keine Findings, 1 Findings, 2 ungültige CLI,
3 Projekt nicht gefunden, 4 externe Ablehnung/Fehler. Incomplete allein ändert
0/1 nicht; Automation muss Analysis auswerten. Unbehandelte technische Fehler
behalten ihren bisherigen Exception-/Exit-Pfad, zusätzlich Coverage-Fehlerhinweis.

**Failure-Artefakte:** Bestehende externe Fehler erzeugen bewusst keinen normalen
Finding-Report. Das bleibt so: kein neuer success-looking JSON/SARIF-Result aus
ungültiger Worker-Evidence. Der bestehende Fehlerpfad erhält einen separaten
flachen Completion-Status im Fehlerkanal und eine Main-lokale typisierte
Produktinformation; vorhandene Fehlermeldung/Exit4 bleiben. Bei solchem Exit
ist ein eventuell älterer Report nicht Evidence dieses Laufs. Invalid CLI
bleibt vor Analyse und erzeugt ebenfalls keinen Report. Keine Breaking-
Schema-/Exit-/Wire-Änderung ist für den normalen Completion-Report nötig.

## Characterization-Preflight

Der Such-Census `characterization-files.txt` erfasst produktive CLI-/RunResult-/
Reporter-Tests. Geprüft wurden zusätzlich Current-/Historical-Summary-,
Diagnostic-/Uncertainty- und B25B-Adoption-Assertions sowie Source-Guards.

| Bestehender Vertrag | Entscheidung |
|---|---|
| JSON Metrics/Findings und SARIF rules/results/messages | Byte-/wertsemantisch unverändert; neues Feld separat, keine bestehende Assertion ändern |
| ConsoleFindingsReporter.Complete produziert nichts | Unverändert; globale Coverage gehört RunResultConsoleReporter |
| GenuineCli_NoFindingIsSuccess, console/json/sarif | Vollständiger Empty-Root bleibt Complete; `Check succeeded` bleibt dafür korrekt |
| GenuineCli_RejectsWithoutFindingsFallbackOrSuccess, 20 Fälle | Exit4, bisherige Fehlertexte, kein normaler Report/Success/Findingcount-Text bleiben erhalten |
| GenuineCli_RejectsOtherHistoricalFindingModes, 3 Fälle | CLI-Rejection/Exit2 bleibt unverändert |
| HistoricalBoundedFinding-/SolutionFinding negative CLI | Kein normaler Report, kein Current-Fallback; zusätzliche Completion im Fehlerkanal erlaubt bestehende Assertions unverändert |
| Owner-/Finding-/Current-Historical-Parität | Finding-Listen und Engine-Ergebnisse bleiben unverändert |
| Clone, Timing und Comparison | Bisherige Metrics unverändert; Coverage zusätzlich weiterreichen, neue Tests ergänzen |

**Kein bestehender normativer Assertion-Vertrag muss nach diesem Entwurf
geändert werden; kein Approval Batch beantragt.** Bestehende Tests bleiben
unangetastet. Falls die Umsetzung dem widerspricht, gesammelt stoppen statt
bestehende Assertions zu ändern. Keine historische Evidence aktualisieren.

## Geplante neue Characterization / Validation

Präzisierung der bestehenden Failure-Abbildung: `AnalysisFailure` ist im Worker
überladen. Die exakt vorhandenen Meldungen `Exception-flow analysis is incomplete.`
und `Independent Direct evidence is incomplete.` bezeichnen kontrolliert
unvollständige Resultate; `CompilationFailure` mit `Historical compilation failed.`
bezeichnet vorhandene Source-Diagnostics. Nach unveränderter Identity-/Envelope-
Validation werden sie als Succeeded/Incomplete projiziert. Andere technische
Fehler bleiben Failed. Diese vorhandenen Message-Discriminators werden nur für
die gepinnte validierte Version verwendet, nicht heuristisch aus beliebigem
Exceptiontext geraten. Kein neues Wire-Feld erforderlich; Version-/Failure-
Envelope bleibt unverändert. Unbekannte Failure-Formen bleiben konservativ failed.

Für jeden der vier Modi: Complete mit null/positiven Findings, incomplete ohne
normale Findings, incomplete mit Diagnose, bestehender technischer Failure.
Direct bleibt innerhalb seiner lokalen Semantik; A9-Unsupported ist kein
erzwungener Direct-Fall. Neue C1–C10-Tests für native/externe Outcomes,
Aggregation, Clone, Console, JSON, SARIF und normale CLI. Echte Historical-
Worker-/B25B-E2E zusätzlich zu unabhängigen Current-Solls. Danach gezielt,
Broad, Boundary, Full, Dual-Version/WAE/Current-Baselines/A9/A10/B25B.

Builds/Tests ausschließlich in einem frischen isolierten Export mit dem
aktuellen Delta; Sourcebytes und Gatebefehle separat festhalten. Alte riesige
W4-/historische Payloads nicht in neue Evidence kopieren. Keine Grün-Retries.
