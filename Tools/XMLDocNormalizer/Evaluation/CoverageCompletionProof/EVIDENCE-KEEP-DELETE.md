# Coverage-WIP: konkrete Keep/Delete-Empfehlung

Stand: Abschlussgates einschließlich finaler Stilkorrektur erfolgreich ausgewertet.
Die autorisierte paketbezogene Auswahl ist angewendet; entbehrliche untracked
Dateien wurden nach Einzelprüfung entfernt. Keine endgültige zentrale Policy.
Die finale konkrete Auswahl mit Bytes/Hashes steht in `storage-selection.json`.

## Behalten

- `Capture-CompiledInputs.ps1`, `Capture-Protection.ps1`, `Prepare-Validation.ps1`,
  `Run-Proofs.ps1`, `Run-Validation.ps1`, `Sync-Validation.ps1`, `Verify-Gates.ps1`:
  bestehender Reproduktions-/Prüfcode, zusammen mit einer kurzen Anleitung.
- `Run-ClosureVerification.ps1` und `Write-CompactClosure.ps1`: neue dünne
  Adapter für temporäre Detailausgabe und kompakte Ergebnisse.
- `Preflight.md` und `characterization-files.txt`: kleine Methodik-/Scope-Nachweise.
- Außerhalb dieses Ordners: `Evaluation/coverage-completion-contract.md`,
  Benutzer-README/-Dokumentation, autorisierter Source-/Test-WIP.
- `Results/closure-summary.json`, `Results/source-identity.json`,
  `Results/protection-summary.json`, `Results/closure-gate.json`:
  Commands, Source-Identität, SDK-/Compiler-Versionen, Zähler und Schutzresultat.
- `Results/a8-proof.json`, `Results/a9-proof.json`,
  `Results/production-proof.json`: kleine originale Einzelprüfresultate.
- `witnesses/four-mode-cli.json`: aus Full reduzierte reale 16-Fälle-Matrix.
- `evidence-classification.json`, diese Empfehlung, `storage-selection.json`
  und `README.md`: konkrete Auswahl und Reproduktionsanleitung.

Die ersten neun Dateien umfassten beim ersten Census 37483 Bytes. Die finale
Auswahl bleibt einschließlich Bericht und Entwurf unter 256 KiB; exakte
Bytelängen nennt `storage-selection.json`. Source-/Test-/Benutzerdokumentation
werden separat gezählt, nicht als generierte Evidence umetikettiert.

## Nach erfolgreicher Auswertung verwerfbar

- `build-*.log`, `emit-static-fixture.log`, `new-*.log`, `normal-cli-contract.log`.
- Sämtliche `Results/*.log` und `Results/*.trx`, einschließlich Broad, Boundary,
  Full und der roten Vorbereitungs-/Fixture-Läufe. Die Ursachen und tatsächlichen
  Zähler bleiben im Bericht/kompakten Resultat. Kein unbekannter nativer Crash
  wurde durch diese konkreten Dateien nachgewiesen.
- Die Roh-Daten im isolierten Export: große W4-/Canonical-/SARIF-/JSON-Dumps,
  vollständige Konsolenausgaben sowie normale Build-/Test-Artefakte.
- `artifacts/evaluation/CoverageCompletion/closure/`: Dual-/Proof-/Gate-Logs,
  ausführliche Proof-JSONs und der ausführliche Abschlusscheck. Die kompakten
  Ergebnisse wurden daraus abgeleitet. Kein neuer nativer Crash in diesen Läufen.

Die 32 vorhandenen Log-/TRX-Dateien umfassen 18583865 Bytes. Erfolgreiche
reproduzierbare Ausgabe wird nicht standardmäßig archiviert, auch nicht nur
unter einem anderen dauerhaften Verzeichnis.

## Erst Schlussfolgerungen ableiten, dann verwerfbar

- `start.json` und `protection-*.json`: während der Arbeit für den Schutzcheck
  erforderlich; anschließend genügen kompakte Vorher-/Nachher-Ergebnisse für
  HEAD, Index, Root-Ignore, Stashes, historische Dateien und Core.
- `export.json` und alle `*-inputs.json`: temporäre Exporte/Inventare; Source-
  Digest, relevante Inputs, Befehle und Versionen in der Abschlusszusammenfassung
  festhalten. Für neue Reproduktion frisch erzeugen.
- `Results/compiled-source-verification.json`, `Results/validation-inputs.json`,
  `Results/export.json` und dieselben drei Dateien unter `Results/FinalInputs/`:
  große Detailinventare bzw. ältere Captures. Anzahl, Source-/Image-Identität
  und erfolgreiches PE/PDB-Prüfergebnis kompakt erhalten.
- `prepared-fixtures.json`, `historical-precondition-failure.json`,
  `targeted-precondition-audit.json`: bekannte reproduzierbare Testvoraussetzungen
  und Ursachen; keine seltene Runtime-Primärevidence. Kleine Zusammenfassung genügt.
- `Results/targeted*.json`, `Results/projection-final.json`, `Results/broad.json`,
  `Results/boundary.json`, `Results/full.json`, `Results/dual.json`: in ein gemeinsames kompaktes
  Resultat mit unverfälschter Lauf-/Source-Zuordnung übernehmen. Insbesondere
  den langen Broad-Filter über Skript und gehaltene Filterdatei reproduzierbar machen.
- `Results/proof-inputs.json`: Quell-/Image-Verknüpfung ist geprüft; relevante
  originale Proofquellen und Commands sind in den gehaltenen Einzelresultaten
  und der Abschlusszusammenfassung enthalten.

Diese 25 Dateien umfassen 4127954 Bytes. Bis Auswertung und Schutzabschluss
blieben sie unverändert verfügbar. Die kompakten Ableitungen sind vorhanden;
die autorisierten temporären Dateien wurden anschließend entfernt. Die genaue
Operation wird in `storage-selection.json` mit Pfaden, Counts und Bytes erfasst.

## Ausnahmen

Ein tatsächlicher ungeklärter Prozessfehler, Crash-Dump oder zwingend benötigte
wissenschaftliche Primärdaten werden gezielt gehalten. Ein Hash allein ersetzt
fehlende Primärdaten nicht. Dafür wird keine pauschale große Logsammlung angelegt.
Gepushte B25/A1–B25B-Evidence und ihre History bleiben vollständig unangetastet.

`evidence-classification.json` enthält die konkrete dateiweise Zuordnung der
66 ursprünglich vorhandenen Dateien als ursprünglichen Census. Die tatsächlich
ausgeführte Auswahl steht separat in `storage-selection.json`; dieser initiale
Census wird nicht als neuer Gate-Lauf ausgegeben.
