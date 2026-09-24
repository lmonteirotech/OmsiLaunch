# Capacidades

<!-- l10n: source=reference/capabilities.md -->
> Traducción de la [página original en inglés](../../../reference/capabilities.md) de OmsiLaunch 0.1.0-beta3. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Este es el inventario canónico de lo que OmsiLaunch 0.1.0-beta3 puede hacer, derivado de `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), de la implementación de runtime del plugin (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` y `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`) y de `OmsiLaunchService.GetCapabilitiesAsync`. Para cada capacidad se indica la clasificación, el nivel de estabilidad que se usa en toda esta documentación, si requiere una sesión en estado `Running`, si modifica OMSI, los argumentos y las claves del resultado, los errores y la evidencia de validación en runtime. Cómo invocar una operación (rutas de la CLI, envelope de la API, timeouts, handles) se explica en [control de runtime](runtime-control.md); la tabla de evidencias está en [estado de la validación en runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Clasificación y estabilidad

`PublicCapabilityClassification` tiene cuatro valores. Se corresponden con el vocabulario de estabilidad de la siguiente manera, con rebajas de nivel donde la evidencia es incompleta:

| Clasificación | Significado | Estabilidad |
| --- | --- | --- |
| `PublicStableBeta` | Pública, dentro del contrato de la beta, validada en runtime | `STABLE_BETA` (rebajada a `PARTIAL` donde se indica) |
| `PublicExperimental` | Pública, puede cambiar, validada en runtime o validada estáticamente | `EXPERIMENTAL` (rebajada a `PARTIAL` donde se indica) |
| `InternalOnly` | Primitiva de investigación; no accesible a través de la API pública ni de la CLI | `INTERNAL` |
| `Unsupported` | Reconocida para diagnósticos, rechazada o ausente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` y `Event`. Toda capacidad de runtime requiere una sesión en estado `Running` y el perfil exacto `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` en el registro); las capacidades de sesión que crean una sesión requieren el perfil exacto, pero no una sesión.

Los resultados de runtime son diccionarios de cadenas. Las claves que empiezan por `internal_` o terminan en `_address`, `_pointer` o `_vmt` se eliminan en el límite de la API (`ScrubInternalValues`) y no se enumeran aquí. Las claves de resultado de las tablas de operaciones siguientes son los conjuntos exactos de claves que devuelve el producto: se capturaron ejecutando cada operación pública en una sesión real (captura de documentación del cierre de runtime, sesión `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; las filas de las listas se escriben como `<row>.<n>.<field>`).

Cómo se notifica el fallo de una operación (`CurrentRuntimeControl.Execute`):

| Fallo | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rechazo del registro (operación desconocida o `internal.*`, falta un argumento obligatorio) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | ninguno |
| Un rechazo deliberado del producto (`weather.set`) | el código específico (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (frase) |
| Un fallo de D3D | el código `OL_E_D3D_*` específico | `detail`, `native_status` |
| Cualquier otro fallo en el lado del plugin (handle obsoleto, nombre desconocido, valor fuera de rango, sin vehículo del jugador, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (empieza por el código específico, por ejemplo `OL_E_RUNTIME_OBJECT_HANDLE_STALE` o `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (nombre del tipo .NET) |
| Un resultado que no cabe en el buzón de 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (las listas acotadas se acortan en su lugar, consulta [Horario, conductores, billetes](#timetable-drivers-tickets)) | ninguno |

La columna `Errors` de cada tabla nombra los códigos específicos; se leen en `ErrorCode` o en el primer token de `Values.detail`, como se ha indicado.

<a id="session-capabilities"></a>
## Capacidades de sesión

| Capacidad | Clasificación | Estabilidad | Tipo | Ruta de la API | Ruta de la CLI | Modifica | Validación |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | no | RUNTIME_PASS (toda sesión validada empieza con un plan) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | flags de lanzamiento | sistema de archivos (transacción), proceso | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | no | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, bandeja, Ctrl+C | proceso (`TerminateProcess`), sistema de archivos (restauración) | RUNTIME_PASS; cierre cooperativo no implementado |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | sistema de archivos (restauración) | RUNTIME_PASS: recuperación tras salida temprana (RV-008), propietario terminado más recuperación temprana en el siguiente inicio (`S05`), restauración fallida y luego `/recover` (`F01`), rechazo bajo el lease, con un OMSI huérfano y en la ventana previa al PID (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, control `session.events` | `events read`, `events watch` | no | RUNTIME_PASS; el slot de telemetría de último valor puede perder ráfagas |

<a id="runtime-capabilities-registry-entries"></a>
## Capacidades de runtime (entradas del registro)

| Capacidad | Clasificación | Estabilidad | Tipo | Operaciones | Handles | Validación |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (sesión `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (escritura, `SetTime`, relectura) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: siempre `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (sesión `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: revalidada sobre el slot de mapa corregido (sesión `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) y leída en Berlin-Spandau durante la captura de documentación |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (escritura del FOV y relectura) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS con un PlayerVehicle procedente de una situación guardada (cierre de runtime `CAM01`, familias 0, 2 y 1 con relectura y después desbloqueo). La propia cadena `RuntimeValidation` del registro sigue indicando `STATICALLY_VALIDATED` (autoinforme del producto, no actualizado en esta versión) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS; la detección de handles obsoletos tras la eliminación natural (RV-002) no tiene un productor seguro en runtime y está validada offline |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (sesión `5f641c8d`, `2 -> 3`, handle `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (colección `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: `present=false` en un inicio headless, el snapshot completo con una situación guardada |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` y la familia `timetable.*.list` / `timetable.logs.read` | | RUNTIME_PASS (entradas de trayecto: 91 registros en Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (solo lectura; escrituras BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS para create/describe/update/release, rechazo de handles liberados y obsoletos a través de nuevas creaciones y de sesiones (`H02`), y reset del dispositivo con invalidación de la generación (RV-007, `D01`; el estado `lost` en sí no se produjo) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | No pública. `ExecuteRuntimeAsync` la rechaza con `OL_E_RUNTIME_OPERATION_UNKNOWN` antes de buscar ninguna sesión; la CLI no tiene ruta para ella. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | ninguna | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | ninguna | | UNSUPPORTED (BI-007) |

`internal.road-vehicles.make-basic` es `INTERNAL`: existe en el plugin con fines de investigación (devuelve una dirección nativa) y se rechaza en el límite de la API y en el plano de control local, que validan ambos primero el nombre de la operación contra `PublicRuntimeOperationIds`.

<a id="public-runtime-operations"></a>
## Operaciones de runtime públicas

Esta es la lista exacta de ids de operación que un frontend público puede reenviar (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Todas requieren `SessionState.Running`; ninguna se registra en el diario ni se restaura. El registro comprueba los argumentos obligatorios antes de usar el buzón (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); el plugin valida los argumentos opcionales. Códigos de error comunes a todas las operaciones: `OL_E_RUNTIME_OPERATION_UNKNOWN` (no está en esta lista), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (el plugin no tiene implementación), `OL_E_RUNTIME_OPERATION_FAILED` (excepción inesperada dentro del proceso; valores `detail` y `exception`).

<a id="time"></a>
### Hora

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `time.read` | Read | ninguno | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | opcionales `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimal); al menos uno | las claves de `time.read` después del `SetTime` perfilado | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Meteorología

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | ninguno | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | cualquiera | ninguna | Siempre `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (con argumentos vacíos: `OL_E_RUNTIME_ARGUMENT_REQUIRED`). OMSI sobrescribe los dos candidatos de viento perfilados en su siguiente tick meteorológico. |
| `weather.actual.read` | Read | ninguno | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mapa

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `map.read` | Read | ninguno | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Cámara

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | ninguno | `family` (0 conductor, 1 pasajero, 2 exterior, 3 mapa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | opcionales `family` (0..3), `field_of_view` (10..170); al menos uno | las claves de `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | obligatorio `family` (0..3); opcional `preset` (0..255, solo con la familia 0 o 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. La política se vuelve a aplicar cada 100 ms hasta `camera.unlock`; si una reaplicación falla, se emite el evento `camera.lock.degraded` una vez por cada error distinto. |
| `camera.unlock` | Action | ninguno | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Vehículos de carretera y jugador

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | ninguno | `count`, `ai_collection`, `player_index` (el índice bruto del vehículo del jugador de OMSI, tal como se lee; usa `present` de `player-vehicle.read` para saber si existe un vehículo del jugador) | |
| `road-vehicles.list` | Read | ninguno | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | obligatorio `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | ninguno | `present` (`false` cuando OMSI no tiene vehículo del jugador); cuando es `true`: `player_index` más todas las claves de `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | obligatorio `model` (`Vehicles\...\*.bus` canónico, 240 caracteres como máximo, sin `..`, debe existir bajo la instalación) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. No asigna el PlayerVehicle. Tarda varios segundos; usa el timeout de cliente de 30 s. |
| `road-vehicles.place-random` | Action | opcionales `ai_type` (0..255, predeterminado 0), `group` (0..65535, predeterminado 1), `type` (-1..65535, predeterminado -1), `scheduled` (0..1, predeterminado 0), `tour` (0..65535, predeterminado 0), `line` (0..65535, predeterminado 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Humanos

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | ninguno | `count` | |
| `humans.list` | Read | ninguno | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | obligatorio `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Horario, conductores, billetes

Los resultados de las listas acotadas incluyen `count` (todos los registros en OMSI), `returned_count` (filas en esta respuesta) y `truncated` (`true` cuando se omitieron filas). Las filas son siempre los primeros `returned_count` registros, numerados desde `0`. Los límites de filas son 128 para trayectos, viajes, líneas, archivos rv, paradas, enlaces entre estaciones, tours y perfiles, 256 para entradas de tour y 512 para entradas de trayecto; cuando las filas permitidas por ese límite siguen superando el buzón de 64 KiB (por ejemplo, las entradas de trayecto y de tour en Berlin-Spandau), el plugin descarta las últimas filas hasta que la respuesta cabe e informa del `returned_count` menor con `truncated=true` (auditoría de documentación BUG-05; antes de la corrección, estas dos listas fallaban con `OL_E_RUNTIME_RESPONSE_TOO_LARGE`). `timetable.logs.read` no es una lista acotada: devuelve todas las entradas del registro, y un registro de unos cientos de entradas supera el buzón y falla con `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | ninguno | `invalid` (`true`/`false`) y los recuentos de registros `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | ninguno | `count`, `returned_count`, `truncated`; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | ninguno | `count`; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (todas las entradas; no acotada) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` para un registro largo |
| `drivers.read` | Read | ninguno | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | ninguno | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Scripts, constantes, curvas y HOF del vehículo (ligados a un handle)

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | obligatorio `handle` | `handle`, `count`, `returned_count`, `truncated` (límite 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | obligatorios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | obligatorios `handle`, `name`, `value` (float finito) | `handle`, `name`, `requested_value`, `value` (releído) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | obligatorio `handle` | `handle`, `count`, `returned_count`, `truncated` (límite 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | obligatorios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | obligatorio `handle` | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | obligatorios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | obligatorio `handle` | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | obligatorios `handle`, `name`, `x` (float finito; un `x` ausente o no numérico da `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolación lineal) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | obligatorio `handle` | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Las operaciones D3D se ejecutan en el thread principal/de renderizado de OMSI a través del puente nativo. Los handles de textura tienen la forma `d3dtex-<sessionId N>-<16 hex digits>`.

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | ninguno | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | obligatorios `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); opcional `levels` (0..16, predeterminado 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | obligatorio `handle`; opcional `level` (0..15, predeterminado 0) | las 13 claves de `d3d.texture.create`, para el nivel solicitado | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, más los errores de create |
| `d3d.texture.update` | Action | obligatorios `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (48 KiB decodificados como máximo); opcionales `level` (0..15, predeterminado 0), `x` (0..4095, predeterminado 0), `y` (0..4095, predeterminado 0) | las 13 claves de `d3d.texture.create` después de la actualización | `OL_E_D3D_INVALID_PIXEL_BUFFER`, más los errores de describe |
| `d3d.texture.release` | Action | obligatorio `handle` | las 13 claves de `d3d.texture.create` con `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` si se libera repetidamente, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Las operaciones D3D fallidas devuelven los valores `detail` y `native_status`. Los métodos de extensión de `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) encapsulan estas operaciones para los consumidores de la API.

<a id="getcapabilitiesasync-names"></a>
## Nombres de `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` devuelve una lista estática de registros `Capability(Name, Available, EvidenceState, Reason)` que describen la visión que tiene el host de la instalación. Estos nombres son un vocabulario distinto y más general que los ids del registro anteriores; el registro es el contrato para las operaciones, y la lista de capacidades es un resumen legible por máquina para los integradores. La relación se indica en la última columna.

| Nombre | Disponible | Evidencia | Relación |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | depende de la plataforma | STATICALLY_VALIDATED | [compatibilidad](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | buzón de runtime |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (rechazada) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. La lista es un inventario fijo y autoinformado: esta entrada no se actualizó después de que la ronda de cierre de runtime observara las transiciones resetting/restored (`D01`); consulta [estado de la validación en runtime](../status/runtime-validation-status.md) para ver la evidencia |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | modos de mundo de `session.start` |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year`; solicitarlos hace que el plan no sea ejecutable |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`; solicitarlos hace que el plan no sea ejecutable |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` y flags relacionados; solicitarlos hace que el plan no sea ejecutable |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, `settings` del perfil (runtime RV-005) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | existen analizadores de documentos; una sesión no aplica `InputSpec` (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

El planificador informa además de las capacidades de cada plan en `SessionPlan.RequiredCapabilities` y `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

<a id="known-limitations"></a>
## Limitaciones conocidas

- Los handles están ligados a la sesión; la reutilización de direcciones se detecta mediante una huella del objeto (VMT más puntero de definición para los vehículos, VMT más índice de humano para los humanos) y se notifica como `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Punto ciego residual: un objeto de la misma clase y la misma definición recreado en la misma dirección entre dos lecturas de lista no se distingue del original.
- Los resultados están limitados al buzón de 64 KiB; las listas acotadas se truncan con `truncated=true`; `timetable.logs.read` no está acotada y puede fallar con `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.
- No hay reubicación de vehículos entre tiles, ni escritura de variables de cadena, ni escritura del calendario, ni escritura de la meteorología, ni asignación headless del PlayerVehicle. Consulta [limitaciones conocidas](known-limitations.md).
