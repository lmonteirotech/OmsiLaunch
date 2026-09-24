# Capability

<!-- l10n: source=reference/capabilities.md -->
> Traduzione della [pagina originale in inglese](../../../reference/capabilities.md) di OmsiLaunch 0.1.0-beta3. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questo è l'inventario canonico di ciò che OmsiLaunch 0.1.0-beta3 è in grado di fare, derivato da `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), dall'implementazione runtime nel plugin (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` e `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`) e da `OmsiLaunchService.GetCapabilitiesAsync`. Per ogni capability (funzionalità) indica la classificazione, il livello di stabilità usato in tutta questa documentazione, se è richiesta una sessione `Running`, se modifica OMSI, gli argomenti e le chiavi del risultato, gli errori e l'evidenza della validazione a runtime. Come invocare un'operazione (route CLI, envelope API, timeout, handle) è descritto in [controllo runtime](runtime-control.md); la tabella delle evidenze si trova in [stato della validazione a runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classificazione e stabilità

`PublicCapabilityClassification` ha quattro valori. Corrispondono al vocabolario di stabilità come segue, con declassamenti dove l'evidenza è incompleta:

| Classificazione | Significato | Stabilità |
| --- | --- | --- |
| `PublicStableBeta` | Pubblica, parte del contratto Beta, validata a runtime | `STABLE_BETA` (declassata a `PARTIAL` dove indicato) |
| `PublicExperimental` | Pubblica, può cambiare, validata a runtime o validata staticamente | `EXPERIMENTAL` (declassata a `PARTIAL` dove indicato) |
| `InternalOnly` | Primitiva di ricerca; non raggiungibile tramite l'API pubblica o la CLI | `INTERNAL` |
| `Unsupported` | Riconosciuta ai fini della diagnostica, rifiutata o assente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` ed `Event`. Ogni capability runtime richiede una sessione `Running` e il profilo esatto `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` nel registro); le capability di sessione che creano una sessione richiedono il profilo esatto ma nessuna sessione.

I risultati runtime sono dizionari di stringhe. Le chiavi che iniziano con `internal_` o terminano con `_address`, `_pointer` o `_vmt` vengono rimosse al confine dell'API (`ScrubInternalValues`) e non sono elencate qui. Le chiavi del risultato nelle tabelle delle operazioni che seguono sono gli insiemi esatti di chiavi restituiti dal prodotto: sono stati acquisiti eseguendo ogni operazione pubblica in una sessione reale (acquisizione per la documentazione della chiusura runtime, sessione `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; le righe degli elenchi sono scritte come `<row>.<n>.<field>`).

Come viene segnalato il fallimento di un'operazione (`CurrentRuntimeControl.Execute`):

| Fallimento | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rifiuto da parte del registro (operazione sconosciuta o `internal.*`, argomento obbligatorio mancante) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | nessuno |
| Un rifiuto deliberato del prodotto (`weather.set`) | il codice specifico (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (frase) |
| Un errore D3D | il codice `OL_E_D3D_*` specifico | `detail`, `native_status` |
| Qualsiasi altro errore lato plugin (handle obsoleto, nome sconosciuto, valore fuori intervallo, nessun veicolo del giocatore, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (inizia con il codice specifico, per esempio `OL_E_RUNTIME_OBJECT_HANDLE_STALE` oppure `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (nome del tipo .NET) |
| Un risultato che non entra nella mailbox da 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (gli elenchi limitati vengono invece accorciati, vedere [Orario, conducenti, biglietti](#timetable-drivers-tickets)) | nessuno |

La colonna `Errors` di ciascuna tabella indica i codici specifici; leggerli da `ErrorCode` o dal primo token di `Values.detail` come descritto sopra.

<a id="session-capabilities"></a>
## Capability di sessione

| Capability | Classificazione | Stabilità | Tipo | Route API | Route CLI | Modifica | Validazione |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | no | RUNTIME_PASS (ogni sessione validata inizia con un piano) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | flag di avvio | filesystem (transazione), processo | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | no | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, area di notifica, Ctrl+C | processo (`TerminateProcess`), filesystem (ripristino) | RUNTIME_PASS; arresto cooperativo non implementato |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | filesystem (ripristino) | RUNTIME_PASS: recupero dopo uscita anticipata (RV-008), owner terminato più recupero anticipato al successivo avvio (`S05`), ripristino fallito seguito da `/recover` (`F01`), rifiuto sotto il lease, con un OMSI orfano e nella finestra precedente al PID (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, controllo `session.events` | `events read`, `events watch` | no | RUNTIME_PASS; lo slot di telemetria a ultimo valore può perdere raffiche di eventi |

<a id="runtime-capabilities-registry-entries"></a>
## Capability runtime (voci del registro)

| Capability | Classificazione | Stabilità | Tipo | Operazioni | Handle | Validazione |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (sessione `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (scrittura, `SetTime`, rilettura) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: sempre `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (sessione `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: rivalidata sullo slot della mappa corretto (sessione `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) e letta su Berlin-Spandau nell'acquisizione per la documentazione |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (scrittura del FOV e rilettura) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS con un PlayerVehicle da una situazione salvata (chiusura runtime `CAM01`, famiglie 0, 2 e 1 con rilettura, poi sblocco). La stringa `RuntimeValidation` del registro riporta ancora `STATICALLY_VALIDATED` (autodichiarazione del prodotto, non aggiornata in questa release) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS; il rilevamento degli handle obsoleti dopo la rimozione naturale (RV-002) non ha un produttore runtime sicuro ed è validato offline |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (sessione `5f641c8d`, `2 -> 3`, handle `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (collezione `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: `present=false` in un avvio headless, lo snapshot completo con una situazione salvata |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` e la famiglia `timetable.*.list` / `timetable.logs.read` | | RUNTIME_PASS (voci di tracciato (track entry): 91 record su Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (solo lettura; scritture BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS per create/describe/update/release, rifiuto degli handle rilasciati e obsoleti attraverso ricreazioni e sessioni (`H02`), e reset del dispositivo con invalidazione della generazione (RV-007, `D01`; lo stato `lost` in sé non è stato prodotto) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | Non pubblica. `ExecuteRuntimeAsync` la rifiuta con `OL_E_RUNTIME_OPERATION_UNKNOWN` prima di qualsiasi ricerca della sessione; la CLI non ha alcuna route per essa. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | nessuna | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | nessuna | | UNSUPPORTED (BI-007) |

`internal.road-vehicles.make-basic` è `INTERNAL`: esiste nel plugin a scopo di ricerca (restituisce un indirizzo nativo) e viene rifiutata al confine dell'API e dal piano di controllo locale, che per primi validano entrambi il nome dell'operazione rispetto a `PublicRuntimeOperationIds`.

<a id="public-runtime-operations"></a>
## Operazioni runtime pubbliche

Questo è l'elenco esatto degli id di operazione che un frontend pubblico può inoltrare (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Ognuna richiede `SessionState.Running`; nessuna viene registrata nel journal né ripristinata. Gli argomenti obbligatori sono imposti dal registro prima che la mailbox venga usata (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); gli argomenti facoltativi sono validati dal plugin. Codici di errore comuni a tutte le operazioni: `OL_E_RUNTIME_OPERATION_UNKNOWN` (non presente in questo elenco), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (il plugin non ha un'implementazione), `OL_E_RUNTIME_OPERATION_FAILED` (eccezione imprevista nel processo; valori `detail` ed `exception`).

<a id="time"></a>
### Ora

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `time.read` | Read | nessuno | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | facoltativi `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimale); almeno uno | le chiavi di `time.read` dopo la chiamata profilata a `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Meteo

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | nessuno | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | qualsiasi | nessuna | Sempre `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (argomenti vuoti: `OL_E_RUNTIME_ARGUMENT_REQUIRED`). OMSI sovrascrive entrambi i candidati profilati per il vento al suo successivo tick meteo. |
| `weather.actual.read` | Read | nessuno | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mappa

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `map.read` | Read | nessuno | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Telecamera

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | nessuno | `family` (0 conducente, 1 passeggero, 2 esterna, 3 mappa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | facoltativi `family` (0..3), `field_of_view` (10..170); almeno uno | le chiavi di `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | obbligatorio `family` (0..3); facoltativo `preset` (0..255, solo con la famiglia 0 o 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. La policy viene riapplicata ogni 100 ms fino a `camera.unlock`; una riapplicazione fallita emette l'evento `camera.lock.degraded` una volta per ogni errore distinto. |
| `camera.unlock` | Action | nessuno | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Veicoli stradali e giocatore

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | nessuno | `count`, `ai_collection`, `player_index` (l'indice grezzo del veicolo del giocatore di OMSI, riportato così come letto; usare `present` di `player-vehicle.read` per sapere se esiste un veicolo del giocatore) | |
| `road-vehicles.list` | Read | nessuno | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | obbligatorio `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | nessuno | `present` (`false` quando OMSI non ha un veicolo del giocatore); quando è `true`: `player_index` più tutte le chiavi di `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | obbligatorio `model` (`Vehicles\...\*.bus` canonico, al massimo 240 caratteri, senza `..`, deve esistere all'interno dell'installazione) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Non assegna il PlayerVehicle. Richiede diversi secondi; usare il timeout client di 30 s. |
| `road-vehicles.place-random` | Action | facoltativi `ai_type` (0..255, predefinito 0), `group` (0..65535, predefinito 1), `type` (-1..65535, predefinito -1), `scheduled` (0..1, predefinito 0), `tour` (0..65535, predefinito 0), `line` (0..65535, predefinito 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Human

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | nessuno | `count` | |
| `humans.list` | Read | nessuno | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | obbligatorio `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Orario, conducenti, biglietti

I risultati degli elenchi limitati contengono `count` (tutti i record presenti in OMSI), `returned_count` (righe in questa risposta) e `truncated` (`true` quando alcune righe sono state escluse). Le righe sono sempre i primi `returned_count` record, numerati a partire da `0`. I limiti di righe sono 128 per tracciati, corse, linee, file rv, fermate, collegamenti tra stazioni, turni e profili, 256 per le voci di turno (tour entry) e 512 per le voci di tracciato; quando le righe consentite da tale limite superano comunque la mailbox da 64 KiB (per esempio le voci di tracciato e di turno su Berlin-Spandau), il plugin scarta le ultime righe finché la risposta non entra e riporta il `returned_count` ridotto con `truncated=true` (audit della documentazione BUG-05; prima della correzione questi due elenchi fallivano con `OL_E_RUNTIME_RESPONSE_TOO_LARGE`). `timetable.logs.read` non è un elenco limitato: restituisce ogni voce del log, e un log di qualche centinaio di voci supera la mailbox e fallisce con `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | nessuno | `invalid` (`true`/`false`) e i conteggi dei record `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | nessuno | `count`, `returned_count`, `truncated`; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | nessuno | `count`; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (tutte le voci; non limitato) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` per un log lungo |
| `drivers.read` | Read | nessuno | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | nessuno | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Script, costanti, curve e HOF del veicolo (legati a un handle)

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | obbligatorio `handle` | `handle`, `count`, `returned_count`, `truncated` (limite 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | obbligatori `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | obbligatori `handle`, `name`, `value` (float finito) | `handle`, `name`, `requested_value`, `value` (riletto) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | obbligatorio `handle` | `handle`, `count`, `returned_count`, `truncated` (limite 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | obbligatori `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | obbligatorio `handle` | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | obbligatori `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | obbligatorio `handle` | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | obbligatori `handle`, `name`, `x` (float finito; un `x` mancante o non numerico produce `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolazione lineare) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | obbligatorio `handle` | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Le operazioni D3D vengono eseguite sul thread principale/di rendering di OMSI tramite il bridge nativo. Gli handle delle texture hanno la forma `d3dtex-<sessionId N>-<16 hex digits>`.

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | nessuno | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | obbligatori `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); facoltativo `levels` (0..16, predefinito 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | obbligatorio `handle`; facoltativo `level` (0..15, predefinito 0) | le 13 chiavi di `d3d.texture.create`, per il livello richiesto | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, più gli errori di create |
| `d3d.texture.update` | Action | obbligatori `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (al massimo 48 KiB dopo la decodifica); facoltativi `level` (0..15, predefinito 0), `x` (0..4095, predefinito 0), `y` (0..4095, predefinito 0) | le 13 chiavi di `d3d.texture.create` dopo l'aggiornamento | `OL_E_D3D_INVALID_PIXEL_BUFFER`, più gli errori di describe |
| `d3d.texture.release` | Action | obbligatorio `handle` | le 13 chiavi di `d3d.texture.create` con `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` in caso di rilascio ripetuto, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Le operazioni D3D fallite restituiscono i valori `detail` e `native_status`. I metodi di estensione di `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) incapsulano queste operazioni per chi utilizza l'API.

<a id="getcapabilitiesasync-names"></a>
## Nomi di `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` restituisce un elenco statico di record `Capability(Name, Available, EvidenceState, Reason)` che descrivono la visione che l'host ha dell'installazione. Questi nomi costituiscono un vocabolario separato e meno granulare rispetto agli id del registro visti sopra; il registro è il contratto per le operazioni, mentre l'elenco delle capability è un riepilogo leggibile dalla macchina per gli integratori. La relazione è indicata nell'ultima colonna.

| Nome | Disponibile | Evidenza | Relazione |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | dipende dalla piattaforma | STATICALLY_VALIDATED | [compatibilità](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | mailbox runtime |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (rifiutata) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. L'elenco è un inventario fisso e autodichiarato: questa voce non è stata aggiornata dopo che il ciclo di chiusura runtime ha osservato le transizioni resetting/restored (`D01`); vedere [stato della validazione a runtime](../status/runtime-validation-status.md) per l'evidenza |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | modalità del mondo di `session.start` |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year`; richiederli rende il piano non eseguibile |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`; richiederli rende il piano non eseguibile |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` e i flag correlati; richiederli rende il piano non eseguibile |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, `settings` del profilo (RV-005 a runtime) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | i parser dei documenti esistono; `InputSpec` non viene applicato da una sessione (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

Il pianificatore riporta inoltre le capability per singolo piano in `SessionPlan.RequiredCapabilities` e `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

<a id="known-limitations"></a>
## Limitazioni note

- Gli handle sono legati alla sessione; il riutilizzo di un indirizzo viene rilevato tramite un'impronta dell'oggetto (VMT più puntatore alla definizione per i veicoli, VMT più indice del human per i human) e segnalato come `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Punto cieco residuo: un oggetto della stessa classe e con la stessa definizione ricreato allo stesso indirizzo tra due letture dell'elenco è indistinguibile dall'originale.
- I risultati sono limitati alla mailbox da 64 KiB; gli elenchi limitati vengono troncati con `truncated=true`; `timetable.logs.read` non è limitato e può fallire con `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.
- Nessuno spostamento di veicoli tra tile, nessuna scrittura di variabili stringa, nessuna scrittura del calendario, nessuna scrittura del meteo, nessuna assegnazione headless del PlayerVehicle. Vedere [limitazioni note](known-limitations.md).
