# Unity Input System migration

## Zielzustand

SEE soll seine eigenen Eingaben soweit möglich vollständig mit dem neuen Unity Input System verarbeiten. Dazu gehören Aktionsdefinitionen, Abfragen, Rebinding, Anzeigen und Speicherung von Belegungen. `KeyCode` und alte `KeyBindings.json`-Einträge dienen danach nur noch als Import- oder Kompatibilitätsschnittstelle, wo sie für bestehende Nutzer oder Erweiterungen gebraucht werden.

Ziel ist, alles auf dem neuen Unity Input System laufen zu lassen, was sich sinnvoll migrieren oder neu umsetzen lässt. Das gilt für SEE-eigenen Code ebenso wie für verwendete Plugins und Integrationen: Wo ein Update, eine Konfiguration oder eine eigene Umsetzung möglich ist, wird der neue Eingabepfad verwendet. `Both` darf vorerst als Kompatibilitätsmodus aktiv bleiben, solange konkrete Legacy-Abhängigkeiten bestehen. Diese Abhängigkeiten sind schrittweise zu beseitigen oder zu begrenzen; `Both` ist kein Grund, neue oder migrierbare Funktionen weiterhin auf der alten Input API aufzubauen.

## Stand

### Pushbarer Zwischenstand

Die Umstellung des SEE-eigenen Eingabesystems ist implementiert: Desktop-Aktionen, direkte Modifier- und Mausabfragen sowie Rebinding und Speicherung laufen über das neue Input System. Bestehende Tastaturbelegungen werden importiert. Der Stand ist noch nicht umfassend im Unity Editor oder in Laufzeitszenen getestet.

Für diesen Zwischenstand kann die Migration als im SEE-Code umgesetzt gelten. `Both` bleibt vorerst aktiv, weil einzelne Drittanbieter-Plugins noch die alte Input API verwenden. Plugin-Audit und ausführliche Funktionsprüfungen sind der nächste Schritt nach dem Push.

- `com.unity.inputsystem` ist bereits installiert (`Packages/manifest.json`); `ProjectSettings.asset` hat `activeInputHandler: 2` (`Both`).
- VR nutzt `InputActionReference` in `Assets/SEE/XR/XRSEEActions.cs` und `Assets/SEE/XR/RadialSelection.cs`.
- SEE-eigene Desktop-Abfragen für registrierte Tastenkürzel, Modifier, Maustasten, Mausrad und Cursorposition verwenden das neue Input System.
- Aktive SEE-C#-Aufrufe von `UnityEngine.Input` wurden migriert; auch die letzten auskommentierten Beispiele der alten Mausachsen-Abfrage wurden entfernt.
- Die Einstellungen verwenden Unitys interaktive Rebind-Operation, zeigen lesbare Input-System-Bezeichnungen und unterstützen Abbruch mit Escape.
- Die 59 registrierten SEE-Kürzel sowie feste Aktionen für Modifier, Escape und Ziffern sind zentral im `SEE Keyboard Actions.inputactions`-Asset definiert.
- Neue Belegungen werden mit Unitys `SaveBindingOverridesAsJson` in einer versionierten Datei unter `Application.persistentDataPath` gespeichert und mit `LoadBindingOverridesFromJson` geladen. Wenn noch keine native Override-Datei existiert, werden vorhandene `KeyBindings.json`-Einstellungen importiert und beim ersten neuen Speichern in das native Format übernommen.
- `KeyCode` wird im SEE-eigenen Runtime- und Rebind-Pfad nicht mehr verwendet. Es bleibt nur im Importadapter für bestehende `KeyBindings.json`-Dateien.
- `Both` wird aktuell noch für Drittanbieter-Eingaben benötigt: ZFBrowser liest Maus- und Touch-Eingaben über die alte API; SimpleFileBrowser wird in mehreren Dateiauswahldialogen verwendet und liest alte Tastenkürzel. Dissonance `VoiceBroadcastTrigger` verwendet eine benannte alte Input-Achse; die aktive Nutzung in SEE muss noch in Unity bestätigt werden.
- Weitere UI- und Panel-Plugins besitzen teils bedingte Input-System-Pfade. Ihre Nutzung muss in den tatsächlich geladenen Szenen geprüft werden.

## Noch erforderliche Arbeit

1. Im Unity Editor prüfen, ob das neue Asset importiert ist und Aktionsnamen sowie Control-Pfade den bisherigen Defaults entsprechen.
2. Fehlerfälle bei alten `KeyBindings.json`-Werten und versionierten Overrides praktisch prüfen. Unbekannte Aktionen und ungültige Legacy-Einträge werden übersprungen; Konflikte und beschädigte Override-Dateien sind noch nicht umfassend validiert.
3. Aktive Plugins und Szenen auf Eingabeabhängigkeiten prüfen. Wo sinnvoll Plugins aktualisieren oder ihre neue Input-System-Unterstützung aktivieren; verbleibende Legacy-Fälle dokumentieren und anschließend entscheiden, ob `Both` entfallen kann.
4. In Unity Desktop, VR, UI-Navigation, Abbruch, Konflikte, alte Einstellungen und fehlende/getrennte Geräte durchspielen.

## Abnahmekriterien

- SEE-eigener Laufzeitcode nutzt das neue Input System durchgängig; alte Unity-Input-Aufrufe bleiben nur in dokumentierten Adaptern oder in nicht migrierbaren Drittanbieterpaketen.
- Alle SEE-Aktionen sind zentral definiert und über Input System editierbar.
- Alte gespeicherte Tastenbelegungen werden beim Update übernommen und anschließend im neuen Format gespeichert.
- Die Belegungsoberfläche zeigt Controls direkt aus dem Input System und kann diese ohne `KeyCode`-Polling ändern.
- Alles sinnvoll Migrierbare läuft über das neue Input System, einschließlich eigener Implementierungen und aktualisierbarer Plugins.
- `Both` ist entweder abgeschaltet oder seine verbleibenden konkreten Legacy-Abhängigkeiten sind dokumentiert und bewusst unterstützt.

## Aktuelle Risiken

- Press-, Hold- und Release-Zeitpunkte müssen bei den migrierten Interaktionen gleich bleiben.
- Die jetzige Konfliktprüfung ist auf einzelne Tastaturtasten ausgelegt. Modifier-Kombinationen, mehrere Bindings pro Aktion, Gamepad und weitere Geräte erfordern ein eigenes Verhalten.
- Bestehende Nutzerbelegungen dürfen beim Wechsel auf das neue Speicherformat nicht zurückgesetzt werden.
- ZFBrowser, SimpleFileBrowser, Dissonance und verwendete UI-Plugins bestimmen, ob `Both` am Ende entfallen kann.
