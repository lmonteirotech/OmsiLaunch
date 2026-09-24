# Glossar de-DE – OmsiLaunch 0.1.0-beta3

Verbindlich für alle Übersetzungen nach `docs/localized/de-DE/`. Die englische Seite ist die einzige Quelle; dieses Glossar regelt nur Terminologie und Stil.

## Anrede, Register, Stil

- Anrede: durchgehend **„Sie“** (formell, großgeschrieben: Sie, Ihr, Ihnen). Wo das Englische unpersönlich ist, deutsch ebenfalls unpersönlich oder Passiv („wird … wiederhergestellt“). Keine „du“-Formen.
- Register: sachliche deutsche Fachdokumentation für Entwickler und Anwender, Präsens, Indikativ. Imperative als Sie-Imperativ („Führen Sie … aus“). Qualifikatoren exakt in gleicher Stärke übersetzen („not implemented“ = „nicht implementiert“, „not runtime validated“ = „nicht zur Laufzeit validiert“, „offline only“ = „nur offline“, „never“ = „nie/niemals“, „must“ = „muss“).
- Etablierte englische IT-Begriffe bleiben (Runtime, Plugin, Handle, Thread, Build, Timeout, Pipe, Overlay, Snapshot, Hash, API, CLI). Als deutsche Substantive großschreiben; Zusammensetzungen mit Bindestrich, wenn ein englischer Bestandteil vorkommt: „Runtime-Operation“, „Named-Pipe-Verbindung“, „Plugin-Verzeichnis“, „Overlay-Datei“. Zusammensetzungen mit Inline-Code immer mit Bindestrich an den Code angrenzend: `options.cfg`-Datei, `session stop`-Befehl (Code selbst unverändert).
- Genus der Lehnwörter: die Runtime, das Plugin, das Handle, der Thread, der Build, das Timeout, die Pipe, das Overlay, der Snapshot, der Hash, das Journal, die Capability, das Manifest, das Gate, das Flag, der Tooltip. „Session“ wird im Fließtext **nicht** verwendet, stattdessen „die Sitzung“.
- Typografie:
  - Deutsche Anführungszeichen „…“ (und ‚…‘ innen) in Fließtext; niemals Anführungszeichen um Inline-Code, Code bleibt in Backticks.
  - Dezimalkomma nur in reinem Fließtext ohne Code; Zahlen mit Einheiten wie `64 KiB`, `250 ms`, `2 s` bleiben exakt wie im Englischen (Leerzeichen, Punkt).
  - Gedankenstrich „–“ (Halbgeviertstrich mit Leerzeichen) statt englischem Doppelpunkt-/Semikolon-Stil, wo der Satz es erfordert; Pfeile `->` in Code/Listen unverändert.
  - Überschriften und Tabellenköpfe: Satzschreibung (nur erstes Wort und Substantive groß), keine englische Title Case.
  - Keine Leerzeichen vor `:` `;` `?` `!`.
  - Tastenkombinationen wie im Original: Ctrl+C (nicht in Strg+C ändern, nichts ergänzen).
  - „z. B.“, „d. h.“, „u. a.“ mit schmalem/normalem Leerzeichen; „bzw.“ sparsam.

## Terminologie

| English | de-DE | Hinweis |
|---|---|---|
| session | Sitzung | Produkt-UI verwendet „Sitzung“. Nie „Session“ im Fließtext. |
| session owner / owner | Sitzungseigentümer / Eigentümer | „Owner“ nur in Code. „owner process“ siehe unten. |
| client (mode) | Client(-Modus) | Englisch üblich. „Client mode“ = „Client-Modus“. |
| owner process | Eigentümerprozess | |
| launch (noun/verb) | Start / starten; Launch-… in Zusammensetzungen mit Code | „launch flags“ = „Startflags“; „launch option“ = „Startoption“. |
| plan (noun/verb) | Plan / planen | „the plan is runnable“ = „der Plan ist ausführbar“. |
| runnable / not runnable | ausführbar / nicht ausführbar | |
| start | Start / starten | |
| stop | Stopp / stoppen, beenden | „session stop“ (Vorgang) = „Beenden der Sitzung“; Verb „stoppen“ für Prozess/Owner, „beenden“ für Sitzung. |
| end session | Sitzung beenden | Entspricht Produkt-UI `Sitzung beenden`. |
| canonical stop | kanonischer Stopp(pfad) | „canonical owner stop path“ = „kanonischer Stopppfad des Eigentümers“. |
| restore | Wiederherstellung / wiederherstellen | Dateiwiederherstellung aus Backup. |
| recovery | Recovery (Wiederherstellung nach Absturz) | Zur Unterscheidung von „restore“: **„Recovery“** verwenden (die Recovery), beim ersten Vorkommen pro Seite optional „Recovery (Absturzwiederherstellung)“. |
| pending (journal) | ausstehend (ausstehendes Journal) | „a transaction is pending“ = „eine Transaktion steht aus“. |
| journal | Journal | das Journal; Englisch üblich. |
| transaction | Transaktion | |
| overlay | Overlay | das Overlay; „temporary overlays“ = „temporäre Overlays“. |
| backup | Backup / Sicherungskopie | „backup copy“ = „Sicherungskopie“, sonst „Backup“ (das Backup). |
| snapshot | Snapshot | der Snapshot. |
| installation | Installation | |
| installation root | Installationsstammverzeichnis | Kurz „OMSI-Stammverzeichnis“ für „OMSI root“. |
| package | Paket | |
| release package | Release-Paket | |
| manifest | Manifest | das Manifest. |
| permanent plugin | permanentes Plugin | „plugin closure“ = „Plugin-Gesamtheit (Closure)“ – beim ersten Vorkommen so, danach „Plugin-Closure“. |
| native bridge | native Bridge | Englisch belassen, „die native Bridge“. |
| runtime | Runtime | Englisch belassen (die Runtime). Nicht „Laufzeit“ (alte Übersetzung). Ausnahme: „at runtime“ = „zur Laufzeit“; „.NET runtime“ = „.NET-Runtime“. |
| runtime operation | Runtime-Operation | |
| runtime command | Runtime-Befehl | „runtime command channel“ = „Runtime-Befehlskanal“. |
| runtime slot / mailbox | Runtime-Slot / Mailbox | Englisch belassen; „the 64 KiB mailbox“ = „die 64-KiB-Mailbox“ nur wenn nicht in Code – sonst `64 KiB` unverändert lassen: „die Mailbox von 64 KiB“. |
| control plane | Steuerungsebene (Control Plane) | Erstes Vorkommen mit englischem Begriff in Klammern. |
| local control | lokale Steuerung (Local Control) | Seitenname „Local control / IPC“ = „Lokale Steuerung / IPC“. |
| named pipe | Named Pipe | Englisch üblich; die Named Pipe. |
| frame (protocol) | Frame | der Frame; „length-prefixed frame“ = „Frame mit Längenpräfix“. |
| envelope (JSON) | Envelope | das Envelope (JSON-Hülle); englisch belassen, „output envelope“ = „Ausgabe-Envelope“. |
| handle | Handle | das Handle. |
| stale handle | veraltetes Handle | |
| capability | Capability | die Capability, Plural „Capabilities“. Englisch belassen (entspricht IDs und Registry). Nicht „Fähigkeit“. |
| capability registry | Capability-Registry | |
| bounded list | begrenzte Liste | „bounded list result“ = „Ergebnis einer begrenzten Liste“. |
| truncated | gekürzt | `truncated=true` bleibt Code; Prosa: „gekürzt“, nie „abgeschnitten/fehlerhaft“; ist eine erfolgreiche Antwort. |
| row | Zeile | Listenzeile/Tabellenzeile. |
| evidence | Nachweis(e) | „runtime evidence“ = „Runtime-Nachweis“; „evidence id“ = „Nachweis-ID“. |
| runtime-validated | zur Laufzeit validiert | Token `RUNTIME_VALIDATED` bleibt. |
| statically validated | statisch validiert | Token `STATICALLY_VALIDATED` bleibt. |
| offline test | Offline-Test | „offline-validated“ = „offline validiert“. |
| gate (documentation gate) | Gate (Dokumentations-Gate) | das Gate; englisch üblich in CI-Kontext. |
| tray icon | Tray-Symbol | |
| notification area | Infobereich (Benachrichtigungsbereich) | Windows-deutscher Begriff „Infobereich“; bei erstem Vorkommen Klammerzusatz erlaubt. |
| status window | Statusfenster | |
| confirmation dialog | Bestätigungsdialog | |
| message box | Meldungsfenster | |
| failure dialog | Fehlerdialog | |
| tooltip | Tooltip | der Tooltip (Windows-Deutsch: QuickInfo; hier „Tooltip“ verwenden). |
| context menu | Kontextmenü | |
| Explorer restart | Neustart des Explorers | „when Explorer restarts“ = „wenn der Explorer neu startet“. |
| entry point | Einstiegspunkt | Wie Produkt-UI `Einstiegspunkt`. |
| new map | neue Karte (NEW_MAP-Start) | Modusname im UI: `Neue Sitzung`; Token `NEW_MAP` bleibt. |
| saved situation | gespeicherte Situation | Wie Produkt-UI `Gespeicherte Situation`. |
| map | Karte | Wie Produkt-UI `Karte`. |
| splash screen | Startbild (Splash-Screen) | Produkt-UI `Startbild`; „managed splash“ = „verwaltetes Startbild“. |
| Internet Textures | Internettexturen | Wie Produkt-UI `Internettexturen`. |
| session profile | Sitzungsprofil | Wie Produkt-UI. |
| preset | Voreinstellung | Wie Produkt-UI `Voreinstellung`; „weather preset“ = „Wettervoreinstellung“. |
| setting | Einstellung | |
| player vehicle | Spielerfahrzeug | Code `PlayerVehicle` bleibt. |
| road vehicle | Straßenfahrzeug | Code `RoadVehicle` bleibt. |
| human (pedestrian/passenger object) | Mensch (Fußgänger-/Fahrgastobjekt) | Code `Human` bleibt; in Prosa „Human-Objekt“ zulässig. |
| timetable | Fahrplan | |
| track entry | Trip-Eintrag / Fahrteintrag | „Fahrteintrag“ verwenden. |
| tour entry | Umlaufeintrag | OMSI-Begriff „Umlauf“ für tour. |
| ticket | Fahrschein (Ticket) | „Fahrschein“; Ticket-Code-IDs bleiben. |
| driver | Fahrer | OMSI-Fahrerprofil = „Fahrerprofil“. |
| fleet number | Fuhrparknummer | Wie Produkt-UI `Fuhrparknummer`. |
| registration (plate) | Kennzeichen | Wie Produkt-UI `Kennzeichen`. |
| repaint | Lackierung (Repaint) | Wie Produkt-UI `Lackierung`. |
| spawn | spawnen / Erzeugen | Verb „erzeugen“, Substantiv „Spawn“ wo technisch (Operation `road-vehicles.spawn`). |
| camera lock | Kamerasperre | „camera lock“ als Capability-ID `camera.lock` bleibt Code. |
| device reset | Geräte-Reset (D3D-Device-Reset) | |
| render thread | Render-Thread | |
| texture | Textur | |
| exit code | Exitcode | |
| error code | Fehlercode | |
| diagnostic | Diagnose(meldung) | „plan diagnostic“ = „Plandiagnose“. |
| diagnostics directory | Diagnoseverzeichnis | |
| timeout | Timeout | das Timeout. |
| placeholder | Platzhalter | |
| flag | Flag | das Flag, Plural „Flags“. |
| route | Route | „hierarchical route“ = „hierarchische Route“. |
| command word | Befehlswort | |
| integrator | Integrator | Plural „Integratoren“. |
| caller | Aufrufer | |
| known limitation | bekannte Einschränkung | |
| accepted risk | akzeptiertes Risiko | |
| stable beta | stabile Beta | Token `STABLE_BETA` bleibt. |
| experimental | experimentell | Token `EXPERIMENTAL` bleibt. |
| partial | teilweise / partiell | Token `PARTIAL` bleibt; nie „unvollständig/kaputt“. |
| unavailable | nicht verfügbar | Token `UNAVAILABLE` bleibt; nie „nicht unterstützt“ verwechseln („unsupported“ = „nicht unterstützt“). |
| deprecated / legacy | veraltet / Legacy- | „legacy flag“ = „Legacy-Flag“. |
| not normative | nicht normativ | |
| source of truth | maßgebliche Quelle | |
| by design | beabsichtigt (by design) | In Tabellen: „beabsichtigt“. |
| supervisor | Supervisor | |
| lease (installation lease) | Lease (Installations-Lease) | die Lease. |
| handoff | Übergabe (Handoff) | „startup handoff“ = „Start-Übergabe“. |

## Product UI strings (exact)

Aus `tools/OmsiLaunch.Cli/WindowsUiStrings.cs` (Wörterbuch `German`, gilt für `de` und `de-DE`). Englische UI-Literale bleiben in Backticks unverändert; die deutsche Produktzeichenfolge darf nur exakt so, in Klammern und Backticks, direkt danach ergänzt werden, z. B. `End session` (`Sitzung beenden`).

| Key | English | Produkt (de) |
|---|---|---|
| Tray.Running | OmsiLaunch is running | OmsiLaunch wird ausgeführt |
| Tray.Status | Status | Status |
| Tray.EndSession | End session | Sitzung beenden |
| Tray.EndSessionDescription | Ends OMSI 2 and the OmsiLaunch session | Beendet OMSI 2 und die OmsiLaunch-Sitzung |
| Status.Title | OmsiLaunch session status | OmsiLaunch-Sitzungsstatus |
| Status.SessionRunning | Session is running | Sitzung wird ausgeführt |
| Status.Section.Session | Session | Sitzung |
| Status.Section.Profile | Session profile | Sitzungsprofil |
| Status.Section.Environment | Environment | Umgebung |
| Status.Section.Vehicle | Vehicle | Fahrzeug |
| Status.Section.Configuration | Configuration | Konfiguration |
| Status.Section.Presentation | Presentation | Darstellung |
| Status.Field.Mode | Mode | Modus |
| Status.Field.Map | Map | Karte |
| Status.Field.EntryPoint | Entry point | Einstiegspunkt |
| Status.Field.Situation | Situation | Situation |
| Status.Field.SessionProfile | Profile | Profil |
| Status.Field.Preset | Preset | Voreinstellung |
| Status.Field.Date | Date | Datum |
| Status.Field.Time | Time | Zeit |
| Status.Field.Weather | Weather | Wetter |
| Status.Field.Vehicle | Vehicle | Fahrzeug |
| Status.Field.Repaint | Repaint | Lackierung |
| Status.Field.Hof | HOF | HOF |
| Status.Field.Fleet | Fleet number | Fuhrparknummer |
| Status.Field.Registration | Registration | Kennzeichen |
| Status.Field.Splash | Splash | Startbild |
| Status.Field.InternetTextures | Internet textures | Internettexturen |
| Status.Close | Close | Schließen |
| Mode.NewMap | New session | Neue Sitzung |
| Mode.SavedSituation | Saved situation | Gespeicherte Situation |
| Mode.LastMapState | Last map state | Letzter Kartenstatus |
| Presentation.Managed | Managed | Verwaltet |
| Presentation.Unset | Original OMSI | Originales OMSI |
| InternetTextures.Native | Original OMSI | Originales OMSI |
| InternetTextures.Disabled | Disabled | Deaktiviert |
| InternetTextures.Override | Override | Überschreiben |
| Stop.Title | End session? | Sitzung beenden? |
| Stop.Message | OMSI 2 will be closed and the OmsiLaunch managed session will end. | OMSI 2 wird geschlossen und die von OmsiLaunch verwaltete Sitzung wird beendet. |
| Stop.Confirm | End session | Sitzung beenden |
| Stop.Cancel | Cancel | Abbrechen |
| Stop.Failed | The session could not be ended. OMSI and its managed session remain active. | Die Sitzung konnte nicht beendet werden. OMSI und die verwaltete Sitzung bleiben aktiv. |

## Terminologie-Entscheidungen gegenüber der alten Übersetzung

- Alte de-DE-Übersetzung verwendete „Laufzeit…“ für runtime und „Fähigkeit“ für capability; jetzt verbindlich **Runtime** und **Capability**.
- Alte Übersetzung setzte „…“ um Code – verboten; Code bleibt in Backticks.
- „Benachrichtigungsbereich“ (alt) → **Infobereich**; „Windows-Sitzungsanzeige“ (alt) → **Tray-Anzeige / Tray-Symbol**.
