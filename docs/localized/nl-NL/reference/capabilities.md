# Capabilities

<!-- l10n: source=reference/capabilities.md -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/capabilities.md) voor OmsiLaunch 0.1.0-beta3. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Dit is de canonieke inventaris van wat OmsiLaunch 0.1.0-beta3 kan. Deze is afgeleid van `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), de runtime-implementatie in de plugin (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` en `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`) en `OmsiLaunchService.GetCapabilitiesAsync`. Per capability worden vermeld: de classificatie, het stabiliteitsniveau dat in deze hele documentatie wordt gebruikt, of een sessie in de toestand `Running` vereist is, of OMSI erdoor wordt gewijzigd, de argumenten en resultaatsleutels, de fouten en het bewijs van runtimevalidatie. Hoe je een operatie aanroept (CLI-routes, API-envelope, timeouts, handles) staat in [runtime control](runtime-control.md); de bewijstabel staat in [status van runtimevalidatie](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classificatie en stabiliteit

`PublicCapabilityClassification` heeft vier waarden. Ze worden als volgt op de stabiliteitsterminologie afgebeeld, met afwaarderingen waar het bewijs onvolledig is:

| Classificatie | Betekenis | Stabiliteit |
| --- | --- | --- |
| `PublicStableBeta` | Openbaar, onderdeel van het bètacontract, runtime-gevalideerd | `STABLE_BETA` (afgewaardeerd tot `PARTIAL` waar vermeld) |
| `PublicExperimental` | Openbaar, kan veranderen, runtime-gevalideerd of statisch gevalideerd | `EXPERIMENTAL` (afgewaardeerd tot `PARTIAL` waar vermeld) |
| `InternalOnly` | Onderzoeksprimitief; niet bereikbaar via de openbare API of de CLI | `INTERNAL` |
| `Unsupported` | Herkend voor diagnose, geweigerd of afwezig | `UNAVAILABLE` |

`PublicCapabilityKind` onderscheidt `Read`, `Write`, `Action` en `Event`. Elke runtime-capability vereist een sessie in de toestand `Running` en exact het profiel `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` in het register); sessie-capabilities die een sessie aanmaken vereisen het exacte profiel, maar geen sessie.

Runtimeresultaten zijn dictionaries van strings. Sleutels die beginnen met `internal_` of eindigen op `_address`, `_pointer` of `_vmt` worden aan de API-grens verwijderd (`ScrubInternalValues`) en worden hier niet vermeld. De resultaatsleutels in de onderstaande operatietabellen zijn exact de sleutelverzamelingen die het product retourneert: ze zijn vastgelegd door elke openbare operatie in een echte sessie uit te voeren (documentatieopname van de runtime closure, sessie `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; lijstrijen worden geschreven als `<row>.<n>.<field>`).

Hoe het mislukken van een operatie wordt gerapporteerd (`CurrentRuntimeControl.Execute`):

| Fout | `ErrorCode` | `Values` |
| --- | --- | --- |
| Weigering door het register (onbekende of `internal.*`-operatie, ontbrekend verplicht argument) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | geen |
| Een bewuste weigering door het product (`weather.set`) | de specifieke code (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (zin) |
| Een D3D-fout | de specifieke `OL_E_D3D_*`-code | `detail`, `native_status` |
| Elke andere fout aan de pluginzijde (verouderde handle, onbekende naam, waarde buiten bereik, geen spelersvoertuig, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (begint met de specifieke code, bijvoorbeeld `OL_E_RUNTIME_OBJECT_HANDLE_STALE` of `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (.NET-typenaam) |
| Een resultaat dat niet in de mailbox van 64 KiB past | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (begrensde lijsten worden in plaats daarvan ingekort, zie [Dienstregeling, chauffeurs, kaartjes](#timetable-drivers-tickets)) | geen |

De kolom `Errors` (Fouten) van elke tabel noemt de specifieke codes; lees ze uit `ErrorCode` of uit het eerste token van `Values.detail`, zoals hierboven beschreven.

<a id="session-capabilities"></a>
## Sessie-capabilities

| Capability | Classificatie | Stabiliteit | Soort | API-route | CLI-route | Wijzigt | Validatie |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | nee | RUNTIME_PASS (elke gevalideerde sessie begint met een plan) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | startvlaggen | bestandssysteem (transactie), proces | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | nee | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, systeemvak, Ctrl+C | proces (`TerminateProcess`), bestandssysteem (herstel) | RUNTIME_PASS; coöperatief afsluiten niet geïmplementeerd |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | bestandssysteem (herstel) | RUNTIME_PASS: recovery na vroegtijdig afsluiten (RV-008), eigenaar beëindigd plus vroege recovery bij de volgende start (`S05`), mislukt herstel en daarna `/recover` (`F01`), weigering onder de lease, met een verweesd OMSI-proces en in het venster vóór de PID (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, control `session.events` | `events read`, `events watch` | nee | RUNTIME_PASS; het telemetrieslot met alleen de laatste waarde kan pieken verliezen |

<a id="runtime-capabilities-registry-entries"></a>
## Runtime-capabilities (registervermeldingen)

| Capability | Classificatie | Stabiliteit | Soort | Operaties | Handles | Validatie |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (sessie `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (schrijven, `SetTime`, teruglezen) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: altijd `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (sessie `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: opnieuw gevalideerd op het gecorrigeerde kaartslot (sessie `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) en gelezen op Berlin-Spandau in de documentatieopname |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (FOV schrijven en teruglezen) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS met een PlayerVehicle uit een opgeslagen situatie (runtime closure `CAM01`, families 0, 2 en 1 met teruglezen, daarna ontgrendelen). De eigen `RuntimeValidation`-string van het register luidt nog steeds `STATICALLY_VALIDATED` (zelfrapportage van het product, in deze release niet bijgewerkt) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS; detectie van verouderde handles na natuurlijke verwijdering (RV-002) heeft geen veilige runtimeproducent en is offline gevalideerd |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (sessie `5f641c8d`, `2 -> 3`, handle `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (collectie `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: `present=false` bij een headless start, de volledige snapshot bij een opgeslagen situatie |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` en de familie `timetable.*.list` / `timetable.logs.read` | | RUNTIME_PASS (trackitems: 91 records op Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (alleen lezen; schrijven BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS voor create/describe/update/release, weigering van vrijgegeven en verouderde handles over hernieuwd aanmaken en sessies heen (`H02`), en apparaatreset met invalidatie van de generatie (RV-007, `D01`; `lost` zelf is niet opgetreden) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | Niet openbaar. `ExecuteRuntimeAsync` weigert deze met `OL_E_RUNTIME_OPERATION_UNKNOWN` vóór het opzoeken van een sessie; de CLI heeft er geen route voor. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | geen | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | geen | | UNSUPPORTED (BI-007) |

`internal.road-vehicles.make-basic` is `INTERNAL`: de operatie bestaat in de plugin voor onderzoek (ze retourneert een native adres) en wordt geweigerd aan de API-grens en door het lokale control plane, die beide de operatienaam eerst tegen `PublicRuntimeOperationIds` valideren.

<a id="public-runtime-operations"></a>
## Openbare runtime-operaties

Dit is de exacte lijst van operatie-id's die een openbare frontend mag doorsturen (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Elke operatie vereist `SessionState.Running`; geen enkele wordt in het journal vastgelegd of hersteld. Verplichte argumenten worden door het register afgedwongen voordat de mailbox wordt gebruikt (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); optionele argumenten worden door de plugin gevalideerd. Algemene foutcodes voor alle operaties: `OL_E_RUNTIME_OPERATION_UNKNOWN` (niet in deze lijst), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (de plugin heeft geen implementatie), `OL_E_RUNTIME_OPERATION_FAILED` (onverwachte exceptie binnen het proces; waarden `detail` en `exception`).

<a id="time"></a>
### Tijd

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `time.read` | Read | geen | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | optioneel `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimaal); minstens één | de sleutels van `time.read` na de geprofileerde `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Weer

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | geen | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | willekeurig | geen | Altijd `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (lege argumenten: `OL_E_RUNTIME_ARGUMENT_REQUIRED`). OMSI overschrijft beide geprofileerde windkandidaten bij zijn volgende weertick. |
| `weather.actual.read` | Read | geen | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Kaart

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `map.read` | Read | geen | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

### Camera

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | geen | `family` (0 chauffeur, 1 passagier, 2 extern, 3 kaart), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | optioneel `family` (0..3), `field_of_view` (10..170); minstens één | de sleutels van `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | verplicht `family` (0..3); optioneel `preset` (0..255, alleen met familie 0 of 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. Het beleid wordt elke 100 ms opnieuw toegepast tot `camera.unlock`; een mislukte hernieuwde toepassing geeft eenmaal per afzonderlijke fout de gebeurtenis `camera.lock.degraded`. |
| `camera.unlock` | Action | geen | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Wegvoertuigen en speler

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | geen | `count`, `ai_collection`, `player_index` (de ruwe spelersvoertuigindex van OMSI, gerapporteerd zoals gelezen; gebruik `present` van `player-vehicle.read` om te weten of er een spelersvoertuig bestaat) | |
| `road-vehicles.list` | Read | geen | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | verplicht `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | geen | `present` (`false` als OMSI geen spelersvoertuig heeft); bij `true`: `player_index` plus elke sleutel van `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | verplicht `model` (canoniek `Vehicles\...\*.bus`, maximaal 240 tekens, geen `..`, moet onder de installatie bestaan) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Wijst het PlayerVehicle niet toe. Duurt enkele seconden; gebruik de clienttimeout van 30 s. |
| `road-vehicles.place-random` | Action | optioneel `ai_type` (0..255, standaard 0), `group` (0..65535, standaard 1), `type` (-1..65535, standaard -1), `scheduled` (0..1, standaard 0), `tour` (0..65535, standaard 0), `line` (0..65535, standaard 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Mensen

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | geen | `count` | |
| `humans.list` | Read | geen | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | verplicht `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Dienstregeling, chauffeurs, kaartjes

Resultaten van begrensde lijsten bevatten `count` (alle records in OMSI), `returned_count` (rijen in dit antwoord) en `truncated` (`true` als er rijen zijn weggelaten). De rijen zijn altijd de eerste `returned_count` records, genummerd vanaf `0`. De rijlimieten zijn 128 voor tracks, ritten, lijnen, rv-bestanden, haltes, stationsverbindingen, omlopen en profielen, 256 voor omloopitems en 512 voor trackitems. Als de rijen die deze limiet toestaat nog steeds groter zijn dan de mailbox van 64 KiB (bijvoorbeeld trackitems en omloopitems op Berlin-Spandau), laat de plugin de laatste rijen weg tot het antwoord past en rapporteert de kleinere `returned_count` met `truncated=true` (documentatie-audit BUG-05; vóór de correctie mislukten deze twee lijsten met `OL_E_RUNTIME_RESPONSE_TOO_LARGE`). `timetable.logs.read` is geen begrensde lijst: de operatie retourneert elk logitem, en een log van een paar honderd items overschrijdt de mailbox en mislukt met `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | geen | `invalid` (`true`/`false`), en de recordaantallen `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | geen | `count`, `returned_count`, `truncated`; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | geen | `count`, `returned_count`, `truncated`; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | geen | `count`, `returned_count`, `truncated`; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | geen | `count`, `returned_count`, `truncated`; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | geen | `count`, `returned_count`, `truncated`; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | geen | `count`, `returned_count`, `truncated`; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | geen | `count`, `returned_count`, `truncated`; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | geen | `count`, `returned_count`, `truncated`; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | geen | `count`, `returned_count`, `truncated`; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | geen | `count`, `returned_count`, `truncated`; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | geen | `count`; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (alle items; niet begrensd) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` bij een lang log |
| `drivers.read` | Read | geen | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | geen | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Voertuigscripts, constanten, curves, HOF (per handle)

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | verplicht `handle` | `handle`, `count`, `returned_count`, `truncated` (limiet 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | verplicht `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | verplicht `handle`, `name`, `value` (eindige float) | `handle`, `name`, `requested_value`, `value` (teruggelezen) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | verplicht `handle` | `handle`, `count`, `returned_count`, `truncated` (limiet 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | verplicht `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | verplicht `handle` | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | verplicht `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | verplicht `handle` | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | verplicht `handle`, `name`, `x` (eindige float; een ontbrekende of niet-numerieke `x` geeft `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (lineaire interpolatie) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | verplicht `handle` | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D-operaties worden via de native brug uitgevoerd op de hoofd-/renderthread van OMSI. Texturehandles hebben de vorm `d3dtex-<sessionId N>-<16 hex digits>`.

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | geen | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | verplicht `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); optioneel `levels` (0..16, standaard 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | verplicht `handle`; optioneel `level` (0..15, standaard 0) | de 13 sleutels van `d3d.texture.create`, voor het gevraagde niveau | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, plus de fouten van create |
| `d3d.texture.update` | Action | verplicht `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (maximaal 48 KiB na decodering); optioneel `level` (0..15, standaard 0), `x` (0..4095, standaard 0), `y` (0..4095, standaard 0) | de 13 sleutels van `d3d.texture.create` na de update | `OL_E_D3D_INVALID_PIXEL_BUFFER`, plus de fouten van describe |
| `d3d.texture.release` | Action | verplicht `handle` | de 13 sleutels van `d3d.texture.create` met `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` bij herhaald vrijgeven, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Mislukte D3D-operaties retourneren de waarden `detail` en `native_status`. De extensiemethoden van `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) omhullen deze operaties voor API-gebruikers.

<a id="getcapabilitiesasync-names"></a>
## Namen van `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` retourneert een statische lijst van `Capability(Name, Available, EvidenceState, Reason)`-records die de kijk van de host op de installatie beschrijven. Deze namen vormen een afzonderlijke, grovere terminologie dan de register-id's hierboven: het register is het contract voor operaties, de capabilitylijst is een machineleesbare samenvatting voor integrators. Het verband staat in de laatste kolom.

| Naam | Beschikbaar | Bewijs | Verband |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | afhankelijk van het platform | STATICALLY_VALIDATED | [compatibiliteit](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | runtime-mailbox |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (geweigerd) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. De lijst is een vaste, door het product zelf gerapporteerde inventaris: deze vermelding is niet bijgewerkt nadat de runtime-closureronde de overgangen resetting/restored had waargenomen (`D01`); zie [status van runtimevalidatie](../status/runtime-validation-status.md) voor het bewijs |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | wereldmodi van `session.start` |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year`; wie ze aanvraagt, maakt het plan niet uitvoerbaar |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`; wie ze aanvraagt, maakt het plan niet uitvoerbaar |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` en verwante vlaggen; wie ze aanvraagt, maakt het plan niet uitvoerbaar |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, profiel-`settings` (RV-005 runtime) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | documentparsers bestaan; `InputSpec` wordt niet door een sessie toegepast (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

De planner rapporteert daarnaast capabilities per plan in `SessionPlan.RequiredCapabilities` en `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

<a id="known-limitations"></a>
## Bekende beperkingen

- Handles gelden per sessie; hergebruik van adressen wordt gedetecteerd aan de hand van een objectvingerafdruk (VMT plus definitiepointer voor voertuigen, VMT plus mensindex voor mensen) en gerapporteerd als `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Resterende blinde vlek: een object van dezelfde klasse en met dezelfde definitie dat tussen twee lijstleesacties op hetzelfde adres opnieuw is aangemaakt, is niet van het origineel te onderscheiden.
- Resultaten zijn begrensd tot de mailbox van 64 KiB; begrensde lijsten worden afgekapt met `truncated=true`; `timetable.logs.read` is niet begrensd en kan mislukken met `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.
- Geen verplaatsing van voertuigen tussen tiles, geen schrijven van stringvariabelen, geen schrijven naar de kalender, geen schrijven van het weer, geen headless toewijzing van het PlayerVehicle. Zie [bekende beperkingen](known-limitations.md).
