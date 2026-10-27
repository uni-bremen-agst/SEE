# Coverage / Completion: kompakter Abschlussnachweis

Basis: `aef86f86559fbcc7c355389a407857ed67e5bcfa` plus erhaltener Coverage-WIP.
Die geprüfte Source-Identität steht in `Results/source-identity.json`; ein
Commit allein beschreibt diesen uncommitted Zwischenstand nicht vollständig.
SDK 8.0.418, Debug/net8.0, Current Roslyn 5.0.0, Historical
5.0.0-2.25451.107. Keine Roslyn-Version wurde geändert.

## Ergebnis und Zuordnung

- Vorhanden: Broad 3450/3450, Boundary 596/596, Full 3541/3541, jeweils null Skips.
- Neu abgeschlossen: ursprünglicher Dual-Version-Lauf 800/800 in neun Suiten,
  WAE 11/11, Current-Baselines 8/8 bytegleich, A8 40/40, A9 37/37,
  A10 Production Entry 30/30, Abschlussadapter 1449/1449.
- Nach der abschließenden Typkorrektur wurden Broad, Boundary und Full gemäß
  der unten dokumentierten Input-Matrix jeweils einmal neu ausgeführt. Der
  Dual-Lauf führt außerdem seine ursprünglichen integrierten Tests selbst aus.
- `Results/closure-summary.json` enthält tatsächliche Commands, Parameter,
  Zeitpunkte, Zähler und bekannte rote Zwischenstände mit ihrer Ursache.
  Zwischenrevisionen werden nicht als Prüfung der finalen Source ausgegeben.
- `witnesses/four-mode-cli.json` reduziert die 16 realen CLI-Ausgaben aus Full
  auf Status, Modus, Szenario, Exitcode und Finding-IDs/-Zahl. Kein neuer Lauf,
  kein rekonstruierter Erfolg, keine gespeicherte komplette Reportkopie.
- `Results/{a8,a9,production}-proof.json` bewahren kleine Einzelresultate.
  A8/A9 prüfen unveränderte unabhängige mathematische Inputs und wurden für den
  Stilabschluss nicht erneut ausgeführt; A10 wurde mit den neuen Binaries am
  echten `Session.Analyze`-Entry ausgeführt. Die generische Konsolen-Schlusszeile
  des übernommenen Proofwrappers ersetzt diese tatsächliche Aufrufzuordnung nicht.
- `Results/protection-summary.json`: Index, Root-Ignore, sechs Stashes,
  1203 historische Dateien und 23 Core-Dateien unverändert.

## Reproduktion

Dies ist eine Anleitung für eine spätere Reproduktion, kein Auftrag, die
abgeschlossenen Gates jetzt erneut zu starten. In einem frischen Checkout mit
denselben Sourcebytes und demselben SDK einen isolierten Export verwenden.
Nicht den geschützten Workspace-Core bauen. Normale Restore-Feeds sind im
Repository versioniert; der vorhandene Lauf nutzte ausschließlich vorhandene
exakte Paket-Caches. Offline-Feeds wurden nur im Export umgebunden.

`Prepare-Validation.ps1 -HistoricalCache <absoluter-Paketcache>` erzeugt einen
frischen Export einschließlich der bisherigen Offline-Vorbereitung. Der
optionale Parameter ist eine Reproduktionshilfe für neue Maschinen, nicht Teil
der Produktlogik. Ohne Parameter bleibt der bisherige lokale B25B-Cachepfad
erhalten. Alle gepinnten Pakete müssen vorhanden sein. Keine Version ersetzen.
Temporäre `export.json`-Dateien gehören zur jeweiligen frischen Untersuchung.
Vorhandene Resultate werden durch die Skripte absichtlich nicht überschrieben.

Im frischen Export lautet das originale vollständige Build-/Isolationsgate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Verify-DualVersionBuild.ps1 -RunMainBoundaryTests -BuildTestProjectReferences
```

Dieses Gate baut auch die realen PDB-/Worker-/Boundary-Fixtures und emittiert
die B7-Inputs. Fehlende Fixtures waren die dokumentierte Ursache der frühen
roten Vorbereitungsläufe. Erst nach erfüllten Voraussetzungen Tests starten.
Die finalen Testbefehle stehen als Argumentarrays in `closure-summary.json`,
`Runs[].Executable`/`Command`; relative Pfade beziehen sich auf den Export.
Broad verwendet den committed Filter aus
`Evaluation/P5O2B12Proof/RegressionFilters.json` plus die in
`Run-Validation.ps1` sichtbaren ExceptionFlow-/Historical-/Coverage-Ergänzungen.
Boundary übernimmt den Filter aus dem unveränderten Dual-Version-Skript.
Full verwendet den dort protokollierten ungefilterten `dotnet test`-Aufruf.
Das Originaltestprojekt legt seine eigenen Self-Analysis-Ausnahmen fest.
Für spätere Reproduktion neue eindeutige Outputpfade unter `artifacts/` setzen.

Die Source-Digest-Regel ist in `Results/source-identity.json` vollständig
angegeben: tatsächliche Compile-Items der Main-, Test- und Historical-Projekte
aus `dotnet msbuild -getItem:Compile`, generierte bin/obj/artifacts-Dateien
ausschließen, relative Pfade mit `/` vereinheitlichen, nach Pfad deduplizieren
und sortieren, dann SHA256 über UTF8 `Pfad|SHA256\n`. Originale
`Capture-CompiledInputs.ps1` und der historische metadatenbasierte
`P5O2B25A5Proof/Verify-CompiledSources.ps1` binden Sourcechecksums an PE/PDB.
Die erzeugten vollständigen Inventare sind temporär; kompakte Identität und
erfolgreiche Bindungszahlen bleiben erhalten.

`Run-Proofs.ps1` kopiert die unveränderten historischen A8-/A9-/A10-Proofquellen
in frische Export-Unterordner und prüft sie gegen die finalen Analyzer-Binaries.
`Run-ClosureVerification.ps1` bindet nur den Detail-Ausgabepfad des neuen
Abschlussadapters nach artifacts um. Keine Akzeptanzbedingung wird geändert.
`Write-CompactClosure.ps1` reduziert vorhandene Resultate ohne Testneuausführung.
Die operativen Wrapper brauchen ihre frischen Input-/Laufrecords; die kompakte
Git-Auswahl ist kein Archiv dieser temporären Zustandsverzeichnisse. Bei einer
späteren vollständigen Reproduktion diese Records neu erzeugen und alte
kompakte Ergebnisdateien niemals als neuen Lauf umetikettieren.

## Dauerhafte Auswahl

`storage-selection.json` dokumentiert die angewendete paketbezogene Auswahl.
Nur die dort mit `Keep` benannten Dateien plus die separat
benannten Source-/Test-/Benutzerdokumentationsänderungen auswählen.
Kein pauschales `git add Evaluation/` oder `git add .`.

Logs/TRX, große Inventare und ausgewertete vollständige JSON-/SARIF-/W4-Ausgaben
wurden aus der Paket-Evidence entfernt. Kein großes Rohdatenarchiv wurde als
Ersatz angelegt. Ungeklärte nicht reproduzierbare Fehler würden gezielt
Primär-Evidence erfordern; ein solcher neuer Crash trat in diesen Abschlussgates
nicht auf. Der Runtime-Status bleibt `OPEN - recurrent, unattributed process failure`.
Historische B25/A-/B25B-Evidence bleibt unverändert. Die zentrale
Storage-Entscheidungsvorlage bleibt ausdrücklich ein Entwurf.

## Finale Stilkorrektur: statische Typen und Gate-Inputs

Die folgende Korrektur betrifft genau zwei Dateien mit insgesamt 63 `var`-Tokens,
nicht nur zwei einzelne Deklarationen. Nur Typ-Syntax wurde ersetzt; Initializer,
Kontrollfluss, Fixtures und Assertions blieben unverändert. Roslyn bestätigt
dieselben 112 lokalen Symbole einschließlich Nullable-Annotationen. Rückersetzung
der Typ-Tokens rekonstruiert bytegenau die vorher gehashten Source-Dateien.
Der neue Build und die Revalidierung gehören zu einer neuen Source-Revision.

| Gate | Geänderter validierter Input | Erneuter Lauf |
|---|---|---|
| Broad | Main- und Test-Compile-Inputs, tatsächliche Test-/Main-Images | Ja |
| Boundary | Dasselbe Test-Assembly und Main-Image, auch ohne Coverage-Testfilter | Ja |
| Full | Main- und Test-Compile-Inputs | Ja |
| Dual-Version | Current-Image, Test-Image und deren PDB-/Source-Identitäten | Ja, unverändertes vollständiges Gate |
| WAE | Drei Current-Builds und Main-Testbuild direkt betroffen; Historical drei, Worker zwei und die zwei Fixture-Builds haben keine der geänderten Quellen | Vollständige elf Schritte als unteilbarer Bestandteil von Dual-Version |
| Current-Baselines | Werden durch das neue Main-/Test-Image im Dual-Gate erzeugt | Ja, acht neue Outputs gegen committed B25B-Referenzen |
| A8 | Nur unveränderte FamilyProof.cs/.csproj, keine Produktabhängigkeit | Nein; Ergebnis 40/40 bleibt an diese Inputs gebunden |
| A9 | Nur unveränderte ProfileProof.cs/.csproj, keine Produktabhängigkeit | Nein; Ergebnis 37/37 bleibt an diese Inputs gebunden |
| A10 | Unverändertes Proofprogramm, aber neue tatsächliche Analyzer-/Test-Binaries | Ja, echter Session-Entry |
| Abschlussgate | Aktuelle Source-/PE-/PDB-Hashes und neue Gate-Records | Ja |

Beleg: vorherige tatsächliche Compile-Inventare enthalten Observer einmal unter
Main395 und CoverageCompletionTests einmal unter Tests344; keine der beiden
Dateien ist in Historical126/Shared125 enthalten. A8/A9-Metadaten nennen ihre
eigenen unveränderten Proofquellen und ausdrücklich keine Produktanalyse.
Die Matrix wurde vor dem Start der Revalidierung aus diesen Inputs bestimmt.
Die quellunabhängigen WAE-Schritte werden nicht zusätzlich separat gestartet.

Die folgenden Zeilen beziehen sich auf den unverändert gebliebenen Zeilenstand.
`?` erhält die von `var` tatsächlich inferierte Nullable-Annotation.

### src/XMLDocNormalizer/Execution/Analysis/AnalysisCoverageObserver.cs

| Zeile(n) | Expliziter statischer Typ |
|---|---|
| 28 | `XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile.Reason?` |
| 56 | `ExternalExceptionFlowAnalysisResult?` |

### Tests/XMLDocNormalizerTests/Execution/CoverageCompletionTests.cs

| Zeile(n) | Expliziter statischer Typ |
|---|---|
| 37, 74, 103, 230 | `ExceptionAnalysisMode` |
| 48, 51 | `(int Exit, string Output, string Error)` |
| 54, 206, 209, 338 | `JsonDocument?` |
| 55, 59, 210 | `JsonElement` |
| 77, 104, 114, 177 | `RunResult?` |
| 127 | `IEnumerable<AnalysisCoverage>?` |
| 128, 140, 158, 159 | `AnalysisCoverage?` |
| 191, 232, 355 | `TextWriter?` |
| 245, 309 | `(HistoricalWorkerPayloadProjectionInput Input, string Job)` |
| 246, 288, 297, 310 | `ExternalExceptionFindingAnalysisResult?` |
| 252, 287, 296 | `ExternalExceptionFlowAnalysisResult?` |
| 255, 259, 262, 270, 284, 315 | `HistoricalWorkerCallResult?` |
| 260, 282 | `WorkerResponse?` |
| 268 | `HistoricalWorkerClient?` |
| 275 | `(WorkerFailureCode code, string? message, string? reason)` |
| 289, 298, 312 | `AnalysisCoverageReport?` |
| 319 | `ExternalTargetExecutionException?` |
| 353 | `FileStream?` |
| 354, 371 | `MethodInfo?` |
| 359 | `TargetInvocationException?` |
| 372 | `Task?` |
| 373 | `object?` |
| 385 | `Process?` |
| 386, 386 | `Task<string>?` |
| 398 | `Project?` |
| 405 | `Compilation?` |
| 406 | `SyntaxTree?` |
| 407 | `ProjectClosureSemanticContext?` |
