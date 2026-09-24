# Bekannte Einschränkungen

<!-- l10n: source=reference/known-limitations.md -->
> Übersetzung der [englischen Originalseite](../../../reference/known-limitations.md) für OmsiLaunch 0.1.0-beta3. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite führt, abgeleitet aus dem Code, alles in OmsiLaunch 0.1.0-beta3 auf, was `UNAVAILABLE`, `PARTIAL` oder ein akzeptiertes Risiko ist, damit Anwender und Integratoren nicht auf Verhalten aufbauen, das das Produkt nicht bietet. Jede Zeile nennt die Einschränkung, ihre Stabilität, den Grund für ihr Bestehen und die Stelle, an der sie ausführlich dokumentiert ist. Die englische Dokumentation ist normativ; lokalisierte Fassungen unter `docs/localized/` werden nicht auf demselben Stand gepflegt und können hinterherhinken (siehe den letzten Abschnitt).

<a id="compatibility"></a>
## Kompatibilität

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Nur `Omsi23004_692EBFBF` wird unterstützt (`692EBFBF...6243`); der Steam-LAA-Hash `7DAB063D...D759` steht auf der Zulassungsliste; sein Fingerprint und sein Plan wurden mit einer kontrollierten Kopie validiert, der Spielbetrieb erfordert jedoch eine echte Steam-Installation (das Image ist die DRM-geschützte Steam-Executable) | `PARTIAL` für Steam LAA | [Kompatibilität](compatibility.md) |
| Nur Windows 10+ x64; kein Windows 7/8, kein XP, kein ARM64 | `UNAVAILABLE` | [Kompatibilität](compatibility.md) |
| Das Plugin benötigt zusätzlich zur vom Controller verwendeten x64-Runtime die **x86**-.NET-6-Runtime | | [Kompatibilität](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Weltstart und Startoptionen

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last`, `WorldMode.LastMapState`) ist nicht implementiert; es wird niemals ersatzweise auf eine `.osn`-Datei anhand von Zeitstempeln zurückgegriffen | `UNAVAILABLE` (BI-006) | Plandiagnose `OL_E_CAPABILITY_UNAVAILABLE` |
| Explizites Datum, explizite Uhrzeit und explizites Jahr bzw. Systemwerte dafür (`/date`, `/time`, `/year`, Profil `new.date`/`new.time`/`new.year`, `DateSpec`/`TimeSpec`/`YearSpec`) werden in der Spezifikation mitgeführt, machen den Plan aber nicht ausführbar; das Plugin lehnt Modi ungleich `Unset` ab | `UNAVAILABLE` (`STATICALLY_PARTIAL`) | [Sitzungsprofile](session-profiles.md), [launchspec](launchspec.md) |
| Wettervoreinstellung, ICAO und aktuelles reales Wetter beim Start (`/weather*`, `new.weather`) | `UNAVAILABLE` (`STATICALLY_PARTIAL`, BI-003) | wie oben |
| Modell, Lackierung, HOF, Fuhrparknummer und Kennzeichen des Spielerfahrzeugs beim Start (`/vehicle`-Familie, `PlayerVehicleSpec`) werden gegen den Inhaltskatalog aufgelöst, aber nicht angewendet; werden sie angefordert, ist der Plan nicht ausführbar; eine deterministische Headless-Zuweisung des PlayerVehicle ist eine künftige Erweiterung | `UNAVAILABLE` (BI-007) | `player.assign-headless` in [Capabilities](capabilities.md) |
| Ein Einstiegspunkt per Identität (`/entrypoint:<identity>`) wird nicht mit der von OMSI angezeigten Liste abgeglichen; verwenden Sie `/entrypoint-index` | `PARTIAL` (BI-001) | Plan-Capability `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| Dokument-Overlays für Tastatur und Controller (`InputSpec`, `Environment.Keyboard`, `Environment.Controllers`) werden geparst, aber von keiner Sitzung jemals angewendet | `UNAVAILABLE` (BI-005) | `input.*`-Capabilities |
| `LaunchBehaviorSpec.RestoreConfiguration` und `InstallationSpec.ExpectedExecutableSha256` sind deklariert, werden aber nie gelesen | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds` (`/shutdown-timeout`, Profil `shutdown-timeout`) wird akzeptiert und mitgeführt, vom Supervisor aber nicht ausgewertet | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [Sitzungslebenszyklus](../concepts/session-lifecycle.md) |
| `/quiet` und `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| Diagnose-Flags (`/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native`) füllen `DiagnosticsSpec`; die sichtbare Wirkung beschränkt sich auf den Host-Trace unter `.omsilaunch\diagnostics` | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` sind Validierungs-Harnesses | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Sitzungsende und Prozesssteuerung

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Das Beenden der Sitzung ist eine erzwungene Beendigung: `session.stop`, „End session“ im Tray, Ctrl+C und `CloseAsync` führen alle zu `TerminateProcess`. Die Beendigungsroutine von OMSI wird nicht ausgeführt, OMSI schreibt `options.cfg` und seine Logs beim Beenden nicht neu, und jeder nicht gespeicherte OMSI-Zustand geht verloren. Das ist Absicht: Es verhindert, dass OMSI wiederhergestellte Dateien überschreibt. | beabsichtigt | [Sitzungslebenszyklus](../concepts/session-lifecycle.md) |
| Ein kooperatives Herunterfahren per WM_CLOSE mit Timeout und Rückfall auf erzwungene Beendigung ist nicht implementiert | `UNAVAILABLE` (Produktentscheidung, S-11; OMSI ignorierte in der Runtime-Closure-Runde `WM_CLOSE` an sein Hauptfenster) | [Status der Runtime-Validierung](../status/runtime-validation-status.md) |
| Beim Schließen der Konsole oder bei der Abmeldung hat der Eigentümer ein Budget von 4 s zum Beenden und Wiederherstellen; alles Verbleibende wird beim nächsten Start über das Journal wiederhergestellt | Schließen der Konsole zur Laufzeit validiert; Abmeldung nicht erprobt | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transaktion, Recovery und Lease

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Die Installations-Lease ist ein `Local\`-Semaphor: ein Eigentümer pro Installation **pro Anmeldesitzung**; nicht benutzerübergreifend durchgesetzt; nicht freigegeben, solange ein anderer Prozess ein Handle hält; jeder Prozess desselben Benutzers kann den Namen belegen | akzeptiertes Risiko (S-18) | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |
| Die Recovery (Absturzwiederherstellung) wird abgelehnt (`OL_E_INSTALLATION_BUSY`), solange der im Journal erfasste OMSI-Prozess – oder bei einem Journal ohne PID irgendein `Omsi.exe` aus diesem Stammverzeichnis – läuft | beabsichtigt | wie oben |
| Ein ursprünglich nicht vorhandener Overlay-Pfad, dessen Inhalt sich während der Sitzung geändert hat, blockiert die Wiederherstellung (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`), bis er geprüft wurde | beabsichtigt | wie oben |
| Journale aus der Zeit vor den Eigentums-Fingerprints können nur von einer Sitzung mit identischen geplanten Bytes abgeschlossen werden (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | wie oben |
| Nur sitzungseigene Pfade werden wiederhergestellt. Eigene Schreibvorgänge von OMSI während einer Sitzung (`options.cfg` `[last_map]`, wenn keine Einstellung `options.cfg` überlagert, `Texture\standard.ipr`, Caches, `laststn.osn`, Fahrerprofil, Logs) bleiben bestehen, wie nach einem direkten OMSI-Start | beabsichtigt | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |
| Das Entfernen eines veralteten `closecheck` vor einer Sitzung ist dauerhaft (protokolliert, nicht wiederhergestellt), wenn `SuppressStaleClosecheckWarning` true ist | beabsichtigt | wie oben |

<a id="runtime-control"></a>
## Runtime-Steuerung

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| `weather.set` wird abgelehnt (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`): OMSI überschreibt beide profilierten Wind-Kandidaten bei seinem nächsten Wetter-Tick | `UNAVAILABLE` | [Capabilities](capabilities.md) |
| Schreiben des Kalenders (`SetActualDateTime`) | `UNAVAILABLE` (BI-002) | `calendar.set-actual-date-time` |
| Schreiben von String-Variablen, benannte Trigger, Sound-Trigger (Eigentum an verwalteten Delphi-Strings) | `UNAVAILABLE` (BI-004) | `scripts.string.read` ist nur lesend |
| Keine Fahrzeugverlagerung, keine kachelübergreifende räumliche Neubindung, keine ODE-sichere Transformationshoheit; Positionsfelder sind nur lesbar | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` benötigen ein PlayerVehicle; der Headless-Start mit NEW_MAP hat keines (eine gespeicherte Situation liefert eines) | beabsichtigt (BI-007) | RV-004 |
| Runtime-Änderungen (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, Spawn, Place-Random, D3D-Texturen) werden nicht im Journal erfasst und nicht wiederhergestellt | beabsichtigt | [Runtime-Steuerung](runtime-control.md) |
| Blinder Fleck beim Handle-Fingerprint: Ein Objekt derselben Klasse und Definition, das zwischen zwei Listenlesevorgängen an derselben Adresse neu erstellt wird, wird nicht als veraltet erkannt; für die Lebensdauer bei natürlichem Entfernen (RV-002) gibt es keinen sicheren Runtime-Erzeuger, sie bleibt offline | `PARTIAL` | [Runtime-Steuerung](runtime-control.md) |
| Ergebnisse sind durch die Mailbox von 64 KiB begrenzt: Lange Listen werden gekürzt (`truncated=true`); Pixel-Nutzdaten sind auf 48 KiB pro `d3d.texture.update` begrenzt | beabsichtigt | [Capabilities](capabilities.md) |
| Single-Flight-Kanal: eine Anforderung gleichzeitig pro Sitzung; ein belegter Slot ergibt `OL_E_RUNTIME_CHANNEL_BUSY`; Anforderungs-IDs dürfen nicht wiederverwendet werden | beabsichtigt | [Runtime-Steuerung](runtime-control.md) |
| Die Telemetrie ist ein Slot mit dem jeweils neuesten Wert: Ereignisfolgen, die schneller sind als das Sampling des Hosts von 100 ms, können Zwischenereignisse verlieren (Sequenznummern halten identische aufeinanderfolgende Ereignisse unterscheidbar; zerrissene Samples werden übersprungen) | `PARTIAL` | [Permanentes Plugin](../concepts/permanent-plugin.md) |
| Ein D3D-Geräte-Reset wurde zur Laufzeit beobachtet (`resetting`, `restored`, Invalidierung der Generation); ein eigenständiger `lost`-Übergang wurde nicht erzeugt, weil das Device von OMSI direkt zu `DEVICENOTRESET` überging | `PARTIAL` (RV-007) | [Status der Runtime-Validierung](../status/runtime-validation-status.md) |
| Ergebnisse begrenzter Listen (die `timetable.*.list`-Operationen, `vehicle.variables.list`, `vehicle.string-variables.list`) liefern höchstens die Zeilen, die in den Runtime-Slot von 64 KiB passen; die übrigen werden mit `truncated=true` und einem kleineren `returned_count` ausgelassen (Dokumentationsaudit BUG-05). In diesem Release gibt es kein Paging | beabsichtigt | [Capabilities](capabilities.md) |
| `timetable.logs.read`, `road-vehicles.list`, `humans.list`, `vehicle.constants.list` und `vehicle.curves.list` sind nicht begrenzt: Ein Ergebnis, das größer als der Slot ist, schlägt mit `OL_E_RUNTIME_RESPONSE_TOO_LARGE` fehl (auf den getesteten Karten bei keiner von ihnen beobachtet) | `PARTIAL` | [Capabilities](capabilities.md) |
| Die selbst gemeldeten Nachweis-Zeichenfolgen (`PublicCapabilityRegistry` `RuntimeValidation`, `GetCapabilitiesAsync` `EvidenceState`) wurden nach der Runtime-Closure-Runde nicht aktualisiert: `camera.lock` meldet weiterhin `STATICALLY_VALIDATED` und `runtime.d3d.lifecycle.reset` `IMPLEMENTED_NOT_RUNTIME_VALIDATED`. Maßgeblich ist die Seite [Status der Runtime-Validierung](../status/runtime-validation-status.md) | Dokumentationsrückstand, kein Verhaltensunterschied | [Capabilities](capabilities.md) |
| Einige erweiterte Felder des Karten-/Kachel-/Pfad-/Objektgraphen werden nicht bereitgestellt; Runtime-Reader sind profilgebundene typisierte Snapshots, niemals beliebiger Speicherzugriff | beabsichtigt | [Capabilities](capabilities.md) |
| In-Process-Speicherlesevorgänge folgen dem Muster „erst prüfen, dann verwenden“ gegen ein laufendes OMSI; eine gleichzeitige Änderung durch OMSI zwischen Prüfung und Lesevorgang kann einen inkonsistenten Snapshot ergeben (`OL_E_RUNTIME_OPERATION_FAILED`) | akzeptiertes Risiko (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Lokale Steuerungsebene und Vertrauensmodell

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Vertrauensmodell „gleicher Benutzer“: Die Named Pipe (`CurrentUserOnly`), die Speicherzuordnungen für Handoff, Telemetrie und Runtime sowie das Lease-Semaphor sind für jeden Prozess desselben Windows-Benutzers zugänglich. Ein solcher Prozess kann den Status lesen, die Sitzung beenden oder Runtime-Operationen ausführen, sobald er die `session_id` gelesen hat. | akzeptiertes Risiko (S-06, S-30) | [Lokale Steuerung](local-control.md) |
| Der Steuerungsendpunkt existiert nur, solange der Eigentümer `Running` ist; ein Client erhält während des Starts und nach dem Ende der Sitzung `OL_E_NO_ACTIVE_SESSION` (Exitcode 4) | beabsichtigt | [Lokale Steuerung](local-control.md) |
| Wenn bereits ein anderer Prozess den Pipe-Namen besitzt, läuft der Eigentümer ohne Endpunkt weiter (`ListenFault`), und ein zweiter Start kann fälschlicherweise `OL_E_SESSION_ALREADY_ACTIVE` melden | akzeptiertes Risiko | [Lokale Steuerung](local-control.md) |
| `.omsilaunch\` erbt die ACL des OMSI-Stammverzeichnisses; es wird keine explizite Zugriffssteuerung angewendet | akzeptiertes Risiko (S-31) | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnose und Ausgabe

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Diagnosedaten sind ausschließlich lokale Dateien (`.omsilaunch\diagnostics`); nichts wird hochgeladen, und es gibt keine Fernmeldung | beabsichtigt | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |
| Die Aufbewahrung behält die 50 neuesten Sitzungen; ältere Diagnosedaten mit Sitzungspräfix werden gelöscht, wenn eine neue Sitzung startet | beabsichtigt | wie oben |
| JSON-Ausgabe und Diagnosedaten enthalten Installationspfade (`RootPath`, Asset-Verzeichnisse, `.itx`-Pfade) | beabsichtigt (lokale Daten) | |
| Der Sitzungsfehlerdialog von `OmsiLaunchW.exe` zeigt als Meldung die Fehlernutzdaten des Plugins (z. B. `{"name":"world.failed",...}`) statt eines Satzes; die `Code:`-Zeile ist korrekt | kosmetisch | [Windows-Tray](windows-tray.md) |
| Eine D3D-Anforderung, die von der nativen Bridge vor jedem Direct3D-Aufruf abgelehnt wird, meldet `native_status` korrekt, ihr `detail`-Text lautet jedoch `HRESULT 0x00000000` | kosmetisch | [Capabilities](capabilities.md) |
| Das Tray-Statusfenster ist ein beim Öffnen erstellter Snapshot der geplanten Sitzung; es wird nicht aktualisiert und zeigt keine Live-Werte von OMSI | beabsichtigt | [Windows-Tray](windows-tray.md) |

<a id="documentation"></a>
## Dokumentation

Die englischen Seiten unter `docs/` sind die normative Dokumentation für dieses Release. `docs/localized/<locale>/` enthält Übersetzungen derselben `0.1.0-beta3`-Seiten (siehe [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); wo eine Übersetzung vom englischen Text abweicht, sind der englische Text und der Code maßgeblich. Die dort aufgeführten historischen und Legacy-Seiten sind nur auf Englisch verfügbar.

Siehe auch: [Capabilities](capabilities.md), [Status der Runtime-Validierung](../status/runtime-validation-status.md), [Fehler](errors.md).
