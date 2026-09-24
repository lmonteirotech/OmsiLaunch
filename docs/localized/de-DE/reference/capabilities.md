# Capabilities

<!-- l10n: source=reference/capabilities.md -->
> Übersetzung der [englischen Originalseite](../../../reference/capabilities.md) für OmsiLaunch 0.1.0-beta3. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Dies ist das maßgebliche Verzeichnis dessen, was OmsiLaunch 0.1.0-beta3 kann. Es ist abgeleitet aus `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), der Runtime-Implementierung im Plugin (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` und `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`) sowie `OmsiLaunchService.GetCapabilitiesAsync`. Für jede Capability werden angegeben: die Klassifizierung, die in dieser gesamten Dokumentation verwendete Stabilitätsstufe, ob eine Sitzung im Zustand `Running` erforderlich ist, ob sie OMSI verändert, die Argumente und Ergebnisschlüssel, die Fehler sowie der Nachweis der Validierung zur Laufzeit. Wie eine Operation aufgerufen wird (CLI-Routen, API-Envelope, Timeouts, Handles), steht unter [Runtime-Steuerung](runtime-control.md); die Nachweistabelle steht unter [Status der Runtime-Validierung](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Klassifizierung und Stabilität

`PublicCapabilityClassification` hat vier Werte. Sie werden wie folgt auf das Stabilitätsvokabular abgebildet, mit Herabstufungen, wo der Nachweis unvollständig ist:

| Klassifizierung | Bedeutung | Stabilität |
| --- | --- | --- |
| `PublicStableBeta` | Öffentlich, Teil des Beta-Vertrags, zur Laufzeit validiert | `STABLE_BETA` (herabgestuft auf `PARTIAL`, wo vermerkt) |
| `PublicExperimental` | Öffentlich, kann sich ändern, zur Laufzeit validiert oder statisch validiert | `EXPERIMENTAL` (herabgestuft auf `PARTIAL`, wo vermerkt) |
| `InternalOnly` | Forschungsprimitiv; nicht über die öffentliche API oder CLI erreichbar | `INTERNAL` |
| `Unsupported` | Für Diagnosezwecke erkannt, abgelehnt oder nicht vorhanden | `UNAVAILABLE` |

`PublicCapabilityKind` unterscheidet `Read`, `Write`, `Action` und `Event`. Jede Runtime-Capability erfordert eine Sitzung im Zustand `Running` und exakt das Profil `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` in der Registry); Sitzungs-Capabilities, die eine Sitzung erzeugen, erfordern das exakte Profil, aber keine Sitzung.

Runtime-Ergebnisse sind Dictionaries aus Zeichenfolgen. Schlüssel, die mit `internal_` beginnen oder auf `_address`, `_pointer` oder `_vmt` enden, werden an der API-Grenze entfernt (`ScrubInternalValues`) und sind hier nicht aufgeführt. Die Ergebnisschlüssel in den folgenden Operationstabellen sind exakt die Schlüsselmengen, die das Produkt zurückgibt: Sie wurden durch Ausführen jeder öffentlichen Operation in einer echten Sitzung erfasst (Dokumentationserfassung des Runtime-Abschlusses, Sitzung `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; Listenzeilen werden als `<row>.<n>.<field>` geschrieben).

Wie ein Fehlschlag einer Operation gemeldet wird (`CurrentRuntimeControl.Execute`):

| Fehlschlag | `ErrorCode` | `Values` |
| --- | --- | --- |
| Ablehnung durch die Registry (unbekannte oder `internal.*`-Operation, fehlendes erforderliches Argument) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | keine |
| Eine bewusste Ablehnung durch das Produkt (`weather.set`) | der spezifische Code (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (Satz) |
| Ein D3D-Fehlschlag | der spezifische `OL_E_D3D_*`-Code | `detail`, `native_status` |
| Jeder andere Fehlschlag auf Plugin-Seite (veraltetes Handle, unbekannter Name, Wert außerhalb des Bereichs, kein Spielerfahrzeug, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (beginnt mit dem spezifischen Code, z. B. `OL_E_RUNTIME_OBJECT_HANDLE_STALE` oder `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (.NET-Typname) |
| Ein Ergebnis, das nicht in die Mailbox von 64 KiB passt | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (begrenzte Listen werden stattdessen gekürzt, siehe [Fahrplan, Fahrer, Fahrscheine](#timetable-drivers-tickets)) | keine |

Die Spalte `Errors` (Fehler) jeder Tabelle nennt die spezifischen Codes; lesen Sie sie wie oben beschrieben aus `ErrorCode` oder aus dem ersten Token von `Values.detail`.

<a id="session-capabilities"></a>
## Sitzungs-Capabilities

| Capability | Klassifizierung | Stabilität | Art | API-Route | CLI-Route | Verändert | Validierung |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | nein | RUNTIME_PASS (jede validierte Sitzung beginnt mit einem Plan) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | Startflags | Dateisystem (Transaktion), Prozess | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | nein | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, Tray, Ctrl+C | Prozess (`TerminateProcess`), Dateisystem (Wiederherstellung) | RUNTIME_PASS; kooperatives Herunterfahren nicht implementiert |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | Dateisystem (Wiederherstellung) | RUNTIME_PASS: Recovery (Absturzwiederherstellung) nach frühem Beenden (RV-008), beendeter Eigentümer plus frühe Recovery beim nächsten Start (`S05`), fehlgeschlagene Wiederherstellung und anschließend `/recover` (`F01`), Ablehnung unter der Lease, mit einem verwaisten OMSI und im Fenster vor der PID (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, Steuerung `session.events` | `events read`, `events watch` | nein | RUNTIME_PASS; der Telemetrie-Slot mit jeweils neuestem Wert kann Ereignisschübe verlieren |

<a id="runtime-capabilities-registry-entries"></a>
## Runtime-Capabilities (Registry-Einträge)

| Capability | Klassifizierung | Stabilität | Art | Operationen | Handles | Validierung |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (Sitzung `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (Schreiben, `SetTime`, Rücklesen) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: immer `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (Sitzung `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: auf dem korrigierten Karten-Slot erneut validiert (Sitzung `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) und in der Dokumentationserfassung auf Berlin-Spandau gelesen |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (FOV schreiben und zurücklesen) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS mit einem PlayerVehicle aus einer gespeicherten Situation (Runtime-Abschluss `CAM01`, Familien 0, 2 und 1 mit Rücklesen, danach Entsperren). Die eigene `RuntimeValidation`-Zeichenfolge der Registry lautet weiterhin `STATICALLY_VALIDATED` (Selbstauskunft des Produkts, in diesem Release nicht aktualisiert) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS; für die Erkennung veralteter Handles nach natürlichem Entfernen (RV-002) gibt es keinen sicheren Runtime-Erzeuger, sie ist offline validiert |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (Sitzung `5f641c8d`, `2 -> 3`, Handle `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (Sammlung `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: `present=false` bei einem Headless-Start, der vollständige Snapshot mit einer gespeicherten Situation |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` und die Familie `timetable.*.list` / `timetable.logs.read` | | RUNTIME_PASS (Fahrteinträge: 91 Datensätze auf Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (nur Lesen; Schreiben BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS für create/describe/update/release, Ablehnung freigegebener und veralteter Handles über Neuerstellung und Sitzungen hinweg (`H02`) sowie Geräte-Reset mit Invalidierung der Generation (RV-007, `D01`; `lost` selbst wurde nicht erzeugt) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | Nicht öffentlich. `ExecuteRuntimeAsync` lehnt sie vor jeder Sitzungssuche mit `OL_E_RUNTIME_OPERATION_UNKNOWN` ab; die CLI hat keine Route dafür. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | keine | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | keine | | UNSUPPORTED (BI-007) |

`internal.road-vehicles.make-basic` ist `INTERNAL`: Sie existiert im Plugin für Forschungszwecke (sie gibt eine native Adresse zurück) und wird an der API-Grenze und von der lokalen Steuerungsebene (Control Plane) abgelehnt, die beide den Operationsnamen zuerst gegen `PublicRuntimeOperationIds` prüfen.

<a id="public-runtime-operations"></a>
## Öffentliche Runtime-Operationen

Dies ist die exakte Liste der Operations-IDs, die ein öffentliches Frontend weiterleiten darf (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Jede davon erfordert `SessionState.Running`; keine wird im Journal erfasst oder wiederhergestellt. Erforderliche Argumente werden von der Registry erzwungen, bevor die Mailbox verwendet wird (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); optionale Argumente werden vom Plugin validiert. Gemeinsame Fehlercodes aller Operationen: `OL_E_RUNTIME_OPERATION_UNKNOWN` (nicht in dieser Liste), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (das Plugin hat keine Implementierung), `OL_E_RUNTIME_OPERATION_FAILED` (unerwartete Ausnahme im Prozess; Werte `detail` und `exception`).

<a id="time"></a>
### Zeit

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `time.read` | Read | keine | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | optional `hour` (0..23), `minute` (0..59), `second` (0..59.999, Dezimalzahl); mindestens eines | die `time.read`-Schlüssel nach dem profilierten `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Wetter

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | keine | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | beliebig | keine | Immer `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (leere Argumente: `OL_E_RUNTIME_ARGUMENT_REQUIRED`). OMSI überschreibt beide profilierten Windkandidaten bei seinem nächsten Wetter-Tick. |
| `weather.actual.read` | Read | keine | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Karte

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `map.read` | Read | keine | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Kamera

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | keine | `family` (0 Fahrer, 1 Fahrgast, 2 Außen, 3 Karte), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | optional `family` (0..3), `field_of_view` (10..170); mindestens eines | die `camera.read`-Schlüssel | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | erforderlich `family` (0..3); optional `preset` (0..255, nur mit Familie 0 oder 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. Die Richtlinie wird alle 100 ms erneut angewendet, bis `camera.unlock` erfolgt; eine fehlschlagende erneute Anwendung löst das Ereignis `camera.lock.degraded` einmal pro unterschiedlichem Fehler aus. |
| `camera.unlock` | Action | keine | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Straßenfahrzeuge und Spieler

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | keine | `count`, `ai_collection`, `player_index` (OMSIs roher Spielerfahrzeug-Index, so gemeldet wie gelesen; verwenden Sie `present` aus `player-vehicle.read`, um zu erfahren, ob ein Spielerfahrzeug existiert) | |
| `road-vehicles.list` | Read | keine | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | erforderlich `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | keine | `present` (`false`, wenn OMSI kein Spielerfahrzeug hat); bei `true`: `player_index` plus jeder `road-vehicle.read`-Schlüssel | |
| `road-vehicles.spawn` | Action | erforderlich `model` (kanonisch `Vehicles\...\*.bus`, höchstens 240 Zeichen, kein `..`, muss unterhalb der Installation existieren) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Weist das PlayerVehicle nicht zu. Dauert mehrere Sekunden; verwenden Sie das Client-Timeout von 30 s. |
| `road-vehicles.place-random` | Action | optional `ai_type` (0..255, Standard 0), `group` (0..65535, Standard 1), `type` (-1..65535, Standard -1), `scheduled` (0..1, Standard 0), `tour` (0..65535, Standard 0), `line` (0..65535, Standard 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Menschen

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | keine | `count` | |
| `humans.list` | Read | keine | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | erforderlich `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Fahrplan, Fahrer, Fahrscheine

Ergebnisse begrenzter Listen enthalten `count` (alle Datensätze in OMSI), `returned_count` (Zeilen in dieser Antwort) und `truncated` (`true`, wenn Zeilen ausgelassen wurden). Die Zeilen sind immer die ersten `returned_count` Datensätze, nummeriert ab `0`. Die Zeilenlimits betragen 128 für Tracks, Trips, Linien, RV-Dateien, Haltestellen, Stationsverbindungen, Umläufe und Profile, 256 für Umlaufeinträge und 512 für Fahrteinträge. Wenn die nach diesem Limit zulässigen Zeilen die Mailbox von 64 KiB dennoch überschreiten (z. B. Fahrteinträge und Umlaufeinträge auf Berlin-Spandau), verwirft das Plugin die letzten Zeilen, bis die Antwort passt, und meldet das kleinere `returned_count` mit `truncated=true` (Dokumentationsaudit BUG-05; vor der Korrektur schlugen diese beiden Listen mit `OL_E_RUNTIME_RESPONSE_TOO_LARGE` fehl). `timetable.logs.read` ist keine begrenzte Liste: Sie gibt jeden Log-Eintrag zurück, und ein Log mit einigen hundert Einträgen überschreitet die Mailbox und schlägt mit `OL_E_RUNTIME_RESPONSE_TOO_LARGE` fehl.

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | keine | `invalid` (`true`/`false`) sowie die Datensatzanzahlen `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | keine | `count`, `returned_count`, `truncated`; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | keine | `count`, `returned_count`, `truncated`; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | keine | `count`, `returned_count`, `truncated`; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | keine | `count`, `returned_count`, `truncated`; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | keine | `count`, `returned_count`, `truncated`; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | keine | `count`, `returned_count`, `truncated`; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | keine | `count`, `returned_count`, `truncated`; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | keine | `count`, `returned_count`, `truncated`; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | keine | `count`, `returned_count`, `truncated`; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | keine | `count`, `returned_count`, `truncated`; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | keine | `count`; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (alle Einträge; nicht begrenzt) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` bei einem langen Log |
| `drivers.read` | Read | keine | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | keine | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Fahrzeugskripte, Konstanten, Kurven, HOF (an ein Handle gebunden)

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | erforderlich `handle` | `handle`, `count`, `returned_count`, `truncated` (Limit 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | erforderlich `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | erforderlich `handle`, `name`, `value` (endlicher Float) | `handle`, `name`, `requested_value`, `value` (zurückgelesen) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | erforderlich `handle` | `handle`, `count`, `returned_count`, `truncated` (Limit 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | erforderlich `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | erforderlich `handle` | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | erforderlich `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | erforderlich `handle` | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | erforderlich `handle`, `name`, `x` (endlicher Float; ein fehlendes oder nicht numerisches `x` ergibt `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (lineare Interpolation) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | erforderlich `handle` | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D-Operationen werden über die native Bridge auf OMSIs Haupt-/Render-Thread ausgeführt. Textur-Handles haben die Form `d3dtex-<sessionId N>-<16 hex digits>`.

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | keine | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | erforderlich `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); optional `levels` (0..16, Standard 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | erforderlich `handle`; optional `level` (0..15, Standard 0) | die 13 `d3d.texture.create`-Schlüssel für die angeforderte Ebene | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED` sowie die Fehler von create |
| `d3d.texture.update` | Action | erforderlich `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (dekodiert höchstens 48 KiB); optional `level` (0..15, Standard 0), `x` (0..4095, Standard 0), `y` (0..4095, Standard 0) | die 13 `d3d.texture.create`-Schlüssel nach der Aktualisierung | `OL_E_D3D_INVALID_PIXEL_BUFFER` sowie die Fehler von describe |
| `d3d.texture.release` | Action | erforderlich `handle` | die 13 `d3d.texture.create`-Schlüssel mit `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` bei wiederholter Freigabe, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Fehlgeschlagene D3D-Operationen geben die Werte `detail` und `native_status` zurück. Die Erweiterungsmethoden von `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) kapseln diese Operationen für API-Nutzer.

<a id="getcapabilitiesasync-names"></a>
## `GetCapabilitiesAsync`-Namen

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` gibt eine statische Liste von `Capability(Name, Available, EvidenceState, Reason)`-Datensätzen zurück, die die Sicht des Hosts auf die Installation beschreiben. Diese Namen bilden ein eigenes, gröberes Vokabular als die obigen Registry-IDs: Die Registry ist der Vertrag für Operationen, die Capability-Liste ist eine maschinenlesbare Zusammenfassung für Integratoren. Die Beziehung ist in der letzten Spalte angegeben.

| Name | Verfügbar | Nachweis | Beziehung |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | plattformabhängig | STATICALLY_VALIDATED | [Kompatibilität](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | Runtime-Mailbox |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (abgelehnt) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. Die Liste ist ein festes, selbst gemeldetes Verzeichnis: Dieser Eintrag wurde nicht aktualisiert, nachdem die Runde des Runtime-Abschlusses die Übergänge resetting/restored beobachtet hatte (`D01`); den Nachweis finden Sie unter [Status der Runtime-Validierung](../status/runtime-validation-status.md) |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | Weltmodi von `session.start` |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year`; werden sie angefordert, ist der Plan nicht ausführbar |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`; werden sie angefordert, ist der Plan nicht ausführbar |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` und zugehörige Flags; werden sie angefordert, ist der Plan nicht ausführbar |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, Profil-`settings` (RV-005 Runtime) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | Dokumentparser existieren; `InputSpec` wird von einer Sitzung nicht angewendet (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

Der Planer meldet zusätzlich Capabilities pro Plan in `SessionPlan.RequiredCapabilities` und `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

<a id="known-limitations"></a>
## Bekannte Einschränkungen

- Handles sind an die Sitzung gebunden; die Wiederverwendung einer Adresse wird über einen Objekt-Fingerabdruck erkannt (VMT plus Definitionszeiger bei Fahrzeugen, VMT plus Human-Index bei Menschen) und als `OL_E_RUNTIME_OBJECT_HANDLE_STALE` gemeldet. Verbleibender blinder Fleck: Ein Objekt derselben Klasse und derselben Definition, das zwischen zwei Listenlesevorgängen an derselben Adresse neu erzeugt wird, ist vom ursprünglichen Objekt nicht zu unterscheiden.
- Ergebnisse sind auf die Mailbox von 64 KiB begrenzt; begrenzte Listen werden mit `truncated=true` gekürzt; `timetable.logs.read` ist nicht begrenzt und kann mit `OL_E_RUNTIME_RESPONSE_TOO_LARGE` fehlschlagen.
- Kein kachelübergreifendes Versetzen von Fahrzeugen, kein Schreiben von String-Variablen, kein Schreiben des Kalenders, kein Schreiben des Wetters, keine Headless-Zuweisung des PlayerVehicle. Siehe [bekannte Einschränkungen](known-limitations.md).
