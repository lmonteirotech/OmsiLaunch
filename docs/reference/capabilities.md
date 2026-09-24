# Capabilities

This is the canonical inventory of what OmsiLaunch 0.1.0-beta3 can do, derived from `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), the runtime implementation in the plugin (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` and `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`), and `OmsiLaunchService.GetCapabilitiesAsync`. For every capability it states the classification, the stability level used throughout this documentation, whether a `Running` session is required, whether it mutates OMSI, the arguments and result keys, the errors, and the runtime-validation evidence. How to invoke an operation (CLI routes, API envelope, timeouts, handles) is in [runtime control](runtime-control.md); the evidence table is in [runtime validation status](../status/runtime-validation-status.md).

## Classification and stability

`PublicCapabilityClassification` has four values. They map to the stability vocabulary as follows, with downgrades where evidence is incomplete:

| Classification | Meaning | Stability |
| --- | --- | --- |
| `PublicStableBeta` | Public, in the Beta contract, runtime validated | `STABLE_BETA` (downgraded to `PARTIAL` where noted) |
| `PublicExperimental` | Public, may change, runtime validated or statically validated | `EXPERIMENTAL` (downgraded to `PARTIAL` where noted) |
| `InternalOnly` | Research primitive; not reachable through the public API or CLI | `INTERNAL` |
| `Unsupported` | Recognised for diagnostics, rejected or absent | `UNAVAILABLE` |

`PublicCapabilityKind` distinguishes `Read`, `Write`, `Action` and `Event`. Every runtime capability requires a `Running` session and the exact profile `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` in the registry); session capabilities that create a session require the exact profile but no session.

Runtime results are dictionaries of strings. Keys that start with `internal_` or end with `_address`, `_pointer` or `_vmt` are removed at the API boundary (`ScrubInternalValues`) and are not listed here. The result keys in the operation tables below are the exact key sets returned by the product: they were captured by executing every public operation in a real session (runtime closure documentation capture, session `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; list rows are written `<row>.<n>.<field>`).

How an operation failure is reported (`CurrentRuntimeControl.Execute`):

| Failure | `ErrorCode` | `Values` |
| --- | --- | --- |
| Registry rejection (unknown or `internal.*` operation, missing required argument) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | none |
| A deliberate product rejection (`weather.set`) | the specific code (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (sentence) |
| A D3D failure | the specific `OL_E_D3D_*` code | `detail`, `native_status` |
| Any other plugin-side failure (stale handle, unknown name, value out of range, no player vehicle, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (starts with the specific code, for example `OL_E_RUNTIME_OBJECT_HANDLE_STALE` or `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (.NET type name) |
| A result that does not fit the 64 KiB mailbox | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (bounded lists are shortened instead, see [Timetable, drivers, tickets](#timetable-drivers-tickets)) | none |

The `Errors` column of each table names the specific codes; read them from `ErrorCode` or from the first token of `Values.detail` as above.

## Session capabilities

| Capability | Classification | Stability | Kind | API route | CLI route | Mutates | Validation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | no | RUNTIME_PASS (every validated session begins with a plan) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | launch flags | filesystem (transaction), process | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | no | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, tray, Ctrl+C | process (`TerminateProcess`), filesystem (restore) | RUNTIME_PASS; cooperative shutdown not implemented |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | filesystem (restore) | RUNTIME_PASS: early-exit recovery (RV-008), owner killed plus early recovery at the next start (`S05`), failed restore then `/recover` (`F01`), refusal under the lease, with an orphaned OMSI and in the pre-PID window (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, control `session.events` | `events read`, `events watch` | no | RUNTIME_PASS; latest-value telemetry slot can drop bursts |

## Runtime capabilities (registry entries)

| Capability | Classification | Stability | Kind | Operations | Handles | Validation |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (session `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (write, `SetTime`, read-back) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: always `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (session `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: revalidated on the corrected map slot (session `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) and read on Berlin-Spandau in the documentation capture |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (FOV write and read-back) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS with a PlayerVehicle from a saved situation (runtime closure `CAM01`, families 0, 2 and 1 with read-back, then unlock). The registry's own `RuntimeValidation` string still reads `STATICALLY_VALIDATED` (product self-report, not updated in this release) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS; stale detection after natural removal (RV-002) has no safe runtime producer and is offline-validated |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (session `5f641c8d`, `2 -> 3`, handle `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (collection `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: `present=false` in a headless start, the full snapshot with a saved situation |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` and the `timetable.*.list` / `timetable.logs.read` family | | RUNTIME_PASS (track entries: 91 records on Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (read only; writes BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS for create/describe/update/release, released and stale handle rejection across re-creation and sessions (`H02`), and device reset with generation invalidation (RV-007, `D01`; `lost` itself was not produced) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | Not public. `ExecuteRuntimeAsync` rejects it with `OL_E_RUNTIME_OPERATION_UNKNOWN` before any session lookup; the CLI has no route for it. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | none | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | none | | UNSUPPORTED (BI-007) |

`internal.road-vehicles.make-basic` is `INTERNAL`: it exists in the plugin for research (it returns a native address) and is rejected at the API boundary and by the local control plane, both of which validate the operation name against `PublicRuntimeOperationIds` first.

## Public runtime operations

This is the exact list of operation ids a public frontend may forward (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Every one requires `SessionState.Running`; none is journaled or restored. Required arguments are enforced by the registry before the mailbox is used (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); optional arguments are validated by the plugin. Common failure codes for all operations: `OL_E_RUNTIME_OPERATION_UNKNOWN` (not in this list), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (plugin has no implementation), `OL_E_RUNTIME_OPERATION_FAILED` (unexpected in-process exception; `detail` and `exception` values).

### Time

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `time.read` | Read | none | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | optional `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimal); at least one | the `time.read` keys after the profiled `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

### Weather

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | none | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | any | none | Always `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (empty arguments: `OL_E_RUNTIME_ARGUMENT_REQUIRED`). OMSI overwrites both profiled wind candidates on its next weather tick. |
| `weather.actual.read` | Read | none | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

### Map

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `map.read` | Read | none | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

### Camera

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | none | `family` (0 driver, 1 passenger, 2 external, 3 map), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | optional `family` (0..3), `field_of_view` (10..170); at least one | the `camera.read` keys | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | required `family` (0..3); optional `preset` (0..255, only with family 0 or 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. The policy is re-applied every 100 ms until `camera.unlock`; a failing re-application emits the event `camera.lock.degraded` once per distinct error. |
| `camera.unlock` | Action | none | `locked` (`false`) | |

### Road vehicles and player

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | none | `count`, `ai_collection`, `player_index` (OMSI's raw player-vehicle index, reported as read; use `player-vehicle.read` `present` to know whether a player vehicle exists) | |
| `road-vehicles.list` | Read | none | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | required `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | none | `present` (`false` when OMSI has no player vehicle); when `true`: `player_index` plus every `road-vehicle.read` key | |
| `road-vehicles.spawn` | Action | required `model` (canonical `Vehicles\...\*.bus`, at most 240 characters, no `..`, must exist below the installation) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Does not assign the PlayerVehicle. Takes several seconds; use the 30 s client timeout. |
| `road-vehicles.place-random` | Action | optional `ai_type` (0..255, default 0), `group` (0..65535, default 1), `type` (-1..65535, default -1), `scheduled` (0..1, default 0), `tour` (0..65535, default 0), `line` (0..65535, default 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

### Humans

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | none | `count` | |
| `humans.list` | Read | none | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | required `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

### Timetable, drivers, tickets

Bounded list results carry `count` (all records in OMSI), `returned_count` (rows in this reply) and `truncated` (`true` when rows were left out). Rows are always the first `returned_count` records, numbered from `0`. The row limits are 128 for tracks, trips, lines, rv-files, bus stops, station links, tours and profiles, 256 for tour entries and 512 for track entries; when the rows allowed by that limit still exceed the 64 KiB mailbox (for example track and tour entries on Berlin-Spandau), the plugin drops the last rows until the reply fits and reports the smaller `returned_count` with `truncated=true` (documentation audit BUG-05; before the fix these two lists failed with `OL_E_RUNTIME_RESPONSE_TOO_LARGE`). `timetable.logs.read` is not a bounded list: it returns every log entry, and a log of a few hundred entries exceeds the mailbox and fails with `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | none | `invalid` (`true`/`false`), and the record counts `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | none | `count`, `returned_count`, `truncated`; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | none | `count`, `returned_count`, `truncated`; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | none | `count`, `returned_count`, `truncated`; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | none | `count`, `returned_count`, `truncated`; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | none | `count`, `returned_count`, `truncated`; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | none | `count`, `returned_count`, `truncated`; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | none | `count`, `returned_count`, `truncated`; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | none | `count`, `returned_count`, `truncated`; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | none | `count`, `returned_count`, `truncated`; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | none | `count`, `returned_count`, `truncated`; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | none | `count`; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (all entries; not bounded) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` for a long log |
| `drivers.read` | Read | none | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | none | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

### Vehicle scripts, constants, curves, HOF (handle-scoped)

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | required `handle` | `handle`, `count`, `returned_count`, `truncated` (limit 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | required `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | required `handle`, `name`, `value` (finite float) | `handle`, `name`, `requested_value`, `value` (read back) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | required `handle` | `handle`, `count`, `returned_count`, `truncated` (limit 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | required `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | required `handle` | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | required `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | required `handle` | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | required `handle`, `name`, `x` (finite float; a missing or non-numeric `x` is `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (linear interpolation) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | required `handle` | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D operations execute on OMSI's main/render thread through the native bridge. Texture handles are `d3dtex-<sessionId N>-<16 hex digits>`.

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | none | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | required `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); optional `levels` (0..16, default 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | required `handle`; optional `level` (0..15, default 0) | the 13 `d3d.texture.create` keys, for the requested level | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, plus create errors |
| `d3d.texture.update` | Action | required `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (at most 48 KiB decoded); optional `level` (0..15, default 0), `x` (0..4095, default 0), `y` (0..4095, default 0) | the 13 `d3d.texture.create` keys after the update | `OL_E_D3D_INVALID_PIXEL_BUFFER`, plus describe errors |
| `d3d.texture.release` | Action | required `handle` | the 13 `d3d.texture.create` keys with `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` on repeated release, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Failed D3D operations return `detail` and `native_status` values. The `D3DRuntimeApi` extension methods (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) wrap these operations for API consumers.

## `GetCapabilitiesAsync` names

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` returns a static list of `Capability(Name, Available, EvidenceState, Reason)` records describing the host's view of the installation. These names are a separate, coarser vocabulary from the registry ids above; the registry is the contract for operations, the capability list is a machine-readable summary for integrators. The relation is given in the last column.

| Name | Available | Evidence | Relation |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | platform dependent | STATICALLY_VALIDATED | [compatibility](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | runtime mailbox |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (rejected) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. The list is a fixed, self-reported inventory: this entry was not updated after the runtime closure round observed resetting/restored transitions (`D01`); see [runtime validation status](../status/runtime-validation-status.md) for the evidence |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | `session.start` world modes |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year`; requesting them makes the plan not runnable |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`; requesting them makes the plan not runnable |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` and related flags; requesting them makes the plan not runnable |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, profile `settings` (RV-005 runtime) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | document parsers exist; `InputSpec` is not applied by a session (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

The planner additionally reports per-plan capabilities in `SessionPlan.RequiredCapabilities` and `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

## Known limitations

- Handles are session-scoped; address reuse is detected by an object fingerprint (VMT plus definition pointer for vehicles, VMT plus human index for humans) and reported as `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Residual blind spot: an object of the same class and the same definition recreated at the same address between two list reads is indistinguishable from the original.
- Results are bounded to the 64 KiB mailbox; bounded lists are truncated with `truncated=true`; `timetable.logs.read` is not bounded and can fail with `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.
- No cross-tile vehicle relocation, no string-variable writes, no calendar writes, no weather writes, no headless PlayerVehicle assignment. See [known limitations](known-limitations.md).
