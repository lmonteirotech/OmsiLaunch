# Возможности

<!-- l10n: source=reference/capabilities.md -->
> Перевод [исходной страницы на английском языке](../../../reference/capabilities.md) для OmsiLaunch 0.1.0-beta3. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Это канонический перечень того, что умеет OmsiLaunch 0.1.0-beta3. Он составлен на основе `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), runtime-реализации в плагине (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` и `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`) и метода `OmsiLaunchService.GetCapabilitiesAsync`. Для каждой возможности (capability) указаны классификация, уровень стабильности, используемый во всей этой документации, требуется ли сеанс в состоянии `Running`, изменяет ли она состояние OMSI, аргументы и ключи результата, ошибки и подтверждения проверки в runtime. Как вызывать операцию (маршруты CLI, конверт API, тайм-ауты, handle), описано в разделе [runtime-управление](runtime-control.md); таблица подтверждений приведена на странице [статус проверки в runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Классификация и стабильность

У `PublicCapabilityClassification` четыре значения. Они соответствуют словарю стабильности следующим образом, с понижением уровня там, где подтверждения неполны:

| Классификация | Значение | Стабильность |
| --- | --- | --- |
| `PublicStableBeta` | Публичная, входит в контракт беты, проверена в runtime | `STABLE_BETA` (понижается до `PARTIAL`, где это отмечено) |
| `PublicExperimental` | Публичная, может измениться, проверена в runtime или проверена статически | `EXPERIMENTAL` (понижается до `PARTIAL`, где это отмечено) |
| `InternalOnly` | Исследовательский примитив; недоступен через публичный API или CLI | `INTERNAL` |
| `Unsupported` | Распознаётся для диагностики, отклоняется или отсутствует | `UNAVAILABLE` |

`PublicCapabilityKind` различает `Read`, `Write`, `Action` и `Event`. Каждой runtime-возможности требуется сеанс в состоянии `Running` и точный профиль `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` в реестре); возможностям сеанса, которые создают сеанс, требуется точный профиль, но не сеанс.

Результаты runtime-операций — это словари строк. Ключи, которые начинаются с `internal_` или заканчиваются на `_address`, `_pointer` или `_vmt`, удаляются на границе API (`ScrubInternalValues`) и здесь не перечислены. Ключи результата в таблицах операций ниже — это точные наборы ключей, которые возвращает продукт: они были получены выполнением каждой публичной операции в реальном сеансе (снятие данных для документации в раунде runtime closure, сеанс `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; строки списков записываются как `<row>.<n>.<field>`).

Как сообщается об ошибке операции (`CurrentRuntimeControl.Execute`):

| Ошибка | `ErrorCode` | `Values` |
| --- | --- | --- |
| Отклонение реестром (неизвестная операция или операция `internal.*`, отсутствует обязательный аргумент) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | нет |
| Намеренное отклонение продуктом (`weather.set`) | конкретный код (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (предложение) |
| Сбой D3D | конкретный код `OL_E_D3D_*` | `detail`, `native_status` |
| Любой другой сбой на стороне плагина (устаревший handle, неизвестное имя, значение вне диапазона, нет транспортного средства игрока, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (начинается с конкретного кода, например `OL_E_RUNTIME_OBJECT_HANDLE_STALE` или `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (имя типа .NET) |
| Результат, который не помещается в почтовый ящик (mailbox) размером 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (ограниченные списки вместо этого укорачиваются, см. [Расписание, водители, билеты](#timetable-drivers-tickets)) | нет |

В столбце «Ошибки» (`Errors`) каждой таблицы названы конкретные коды; их следует читать из `ErrorCode` или из первого токена `Values.detail`, как описано выше.

<a id="session-capabilities"></a>
## Возможности сеанса

| Возможность | Классификация | Стабильность | Вид | Маршрут API | Маршрут CLI | Изменяет | Проверка |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | нет | RUNTIME_PASS (каждый проверенный сеанс начинается с плана) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | флаги запуска | файловая система (транзакция), процесс | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | нет | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, трей, Ctrl+C | процесс (`TerminateProcess`), файловая система (восстановление) | RUNTIME_PASS; кооперативное завершение не реализовано |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | файловая система (восстановление) | RUNTIME_PASS: восстановление после сбоя при раннем выходе (RV-008), принудительно завершённый владелец плюс раннее восстановление после сбоя при следующем запуске (`S05`), неудачное восстановление, затем `/recover` (`F01`), отказ под арендой установки (lease), при осиротевшем процессе OMSI и в окне до получения PID (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, команда управления `session.events` | `events read`, `events watch` | нет | RUNTIME_PASS; слот телеметрии, хранящий только последнее значение, может терять всплески событий |

<a id="runtime-capabilities-registry-entries"></a>
## Runtime-возможности (записи реестра)

| Возможность | Классификация | Стабильность | Вид | Операции | Handle | Проверка |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (сеанс `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (запись, `SetTime`, обратное чтение) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: всегда `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (сеанс `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: повторно проверено на исправленном слоте карты (сеанс `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) и прочитано на Berlin-Spandau при снятии данных для документации |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (запись FOV и обратное чтение) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS с PlayerVehicle из сохранённой ситуации (runtime closure `CAM01`, семейства 0, 2 и 1 с обратным чтением, затем снятие фиксации). Собственная строка `RuntimeValidation` в реестре по-прежнему гласит `STATICALLY_VALIDATED` (самоотчёт продукта, не обновлённый в этом выпуске) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS; для обнаружения устаревших handle после естественного удаления объекта (RV-002) нет безопасного способа воспроизвести ситуацию в runtime, оно проверено офлайн-тестами |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (сеанс `5f641c8d`, `2 -> 3`, handle `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (коллекция `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: `present=false` при headless-запуске, полный снимок (snapshot) с сохранённой ситуацией |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` и семейство `timetable.*.list` / `timetable.logs.read` | | RUNTIME_PASS (записи маршрута (track entries): 91 запись на Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (только чтение; запись — BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS для create/describe/update/release, отклонения освобождённых и устаревших handle при повторном создании и между сеансами (`H02`), а также сброса устройства (device reset) с инвалидацией поколения (RV-007, `D01`; само состояние `lost` получено не было) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | Не публичная. `ExecuteRuntimeAsync` отклоняет её с кодом `OL_E_RUNTIME_OPERATION_UNKNOWN` до любого поиска сеанса; в CLI для неё нет маршрута. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | нет | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | нет | | UNSUPPORTED (BI-007) |

Операция `internal.road-vehicles.make-basic` имеет статус `INTERNAL`: она существует в плагине для исследований (возвращает нативный адрес) и отклоняется на границе API и локальной плоскостью управления, которые обе сначала сверяют имя операции с `PublicRuntimeOperationIds`.

<a id="public-runtime-operations"></a>
## Публичные runtime-операции

Это точный список идентификаторов операций, которые публичный фронтенд может передавать дальше (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Каждой из них требуется `SessionState.Running`; ни одна не записывается в журнал транзакции и не восстанавливается. Обязательные аргументы проверяются реестром до использования почтового ящика (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); необязательные аргументы проверяет плагин. Общие коды ошибок для всех операций: `OL_E_RUNTIME_OPERATION_UNKNOWN` (операции нет в этом списке), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (в плагине нет реализации), `OL_E_RUNTIME_OPERATION_FAILED` (неожиданное исключение внутри процесса; значения `detail` и `exception`).

<a id="time"></a>
### Время

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `time.read` | Read | нет | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | необязательные `hour` (0..23), `minute` (0..59), `second` (0..59.999, десятичное); хотя бы один | ключи `time.read` после профилированного вызова `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Погода

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | нет | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | любые | нет | Всегда `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (при пустых аргументах: `OL_E_RUNTIME_ARGUMENT_REQUIRED`). OMSI перезаписывает оба профилированных кандидата для ветра на следующем такте обновления погоды. |
| `weather.actual.read` | Read | нет | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Карта

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `map.read` | Read | нет | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Камера

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | нет | `family` (0 — водитель, 1 — пассажир, 2 — внешняя, 3 — карта), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | необязательные `family` (0..3), `field_of_view` (10..170); хотя бы один | ключи `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | обязательный `family` (0..3); необязательный `preset` (0..255, только с семейством 0 или 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. Политика применяется повторно каждые 100 ms до вызова `camera.unlock`; неудачное повторное применение порождает событие `camera.lock.degraded` один раз для каждой отличающейся ошибки. |
| `camera.unlock` | Action | нет | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Дорожные транспортные средства и игрок

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | нет | `count`, `ai_collection`, `player_index` (сырой индекс транспортного средства игрока в OMSI, передаётся в том виде, в каком прочитан; чтобы узнать, существует ли ТС игрока, используйте `present` из `player-vehicle.read`) | |
| `road-vehicles.list` | Read | нет | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | обязательный `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | нет | `present` (`false`, когда у OMSI нет транспортного средства игрока); при `true`: `player_index` плюс все ключи `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | обязательный `model` (канонический путь `Vehicles\...\*.bus`, не более 240 символов, без `..`, должен существовать внутри установки) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Не назначает PlayerVehicle. Занимает несколько секунд; используйте клиентский тайм-аут 30 s. |
| `road-vehicles.place-random` | Action | необязательные `ai_type` (0..255, по умолчанию 0), `group` (0..65535, по умолчанию 1), `type` (-1..65535, по умолчанию -1), `scheduled` (0..1, по умолчанию 0), `tour` (0..65535, по умолчанию 0), `line` (0..65535, по умолчанию 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Люди

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | нет | `count` | |
| `humans.list` | Read | нет | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | обязательный `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Расписание, водители, билеты

Результаты ограниченных списков содержат `count` (все записи в OMSI), `returned_count` (строки в этом ответе) и `truncated` (`true`, когда часть строк не вошла в ответ). Строки — это всегда первые `returned_count` записей, пронумерованные с `0`. Лимиты строк: 128 для tracks, trips, lines, rv-files, bus stops, station links, tours и profiles, 256 для tour entries и 512 для track entries. Если строки, допускаемые этим лимитом, всё равно превышают почтовый ящик размером 64 KiB (например, записи маршрута и записи рейса на Berlin-Spandau), плагин отбрасывает последние строки, пока ответ не поместится, и сообщает меньшее значение `returned_count` с `truncated=true` (аудит документации BUG-05; до исправления эти два списка завершались ошибкой `OL_E_RUNTIME_RESPONSE_TOO_LARGE`). `timetable.logs.read` не является ограниченным списком: операция возвращает все записи лога, и лог из нескольких сотен записей превышает почтовый ящик и завершается ошибкой `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | нет | `invalid` (`true`/`false`) и счётчики записей `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | нет | `count`, `returned_count`, `truncated`; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | нет | `count`, `returned_count`, `truncated`; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | нет | `count`, `returned_count`, `truncated`; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | нет | `count`, `returned_count`, `truncated`; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | нет | `count`, `returned_count`, `truncated`; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | нет | `count`, `returned_count`, `truncated`; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | нет | `count`, `returned_count`, `truncated`; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | нет | `count`, `returned_count`, `truncated`; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | нет | `count`, `returned_count`, `truncated`; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | нет | `count`, `returned_count`, `truncated`; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | нет | `count`; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (все записи; не ограничен) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` для длинного лога |
| `drivers.read` | Read | нет | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | нет | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Скрипты, константы, кривые и HOF транспортного средства (в рамках handle)

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | обязательный `handle` | `handle`, `count`, `returned_count`, `truncated` (лимит 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | обязательные `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | обязательные `handle`, `name`, `value` (конечное число с плавающей точкой) | `handle`, `name`, `requested_value`, `value` (обратное чтение) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | обязательный `handle` | `handle`, `count`, `returned_count`, `truncated` (лимит 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | обязательные `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | обязательный `handle` | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | обязательные `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | обязательный `handle` | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | обязательные `handle`, `name`, `x` (конечное число с плавающей точкой; отсутствующий или нечисловой `x` даёт `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (линейная интерполяция) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | обязательный `handle` | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Операции D3D выполняются в главном потоке / потоке рендеринга OMSI через нативный мост. Handle текстур имеют вид `d3dtex-<sessionId N>-<16 hex digits>`.

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | нет | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | обязательные `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); необязательный `levels` (0..16, по умолчанию 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | обязательный `handle`; необязательный `level` (0..15, по умолчанию 0) | 13 ключей `d3d.texture.create` для запрошенного уровня | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, а также ошибки create |
| `d3d.texture.update` | Action | обязательные `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (не более 48 KiB после декодирования); необязательные `level` (0..15, по умолчанию 0), `x` (0..4095, по умолчанию 0), `y` (0..4095, по умолчанию 0) | 13 ключей `d3d.texture.create` после обновления | `OL_E_D3D_INVALID_PIXEL_BUFFER`, а также ошибки describe |
| `d3d.texture.release` | Action | обязательный `handle` | 13 ключей `d3d.texture.create` с `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` при повторном освобождении, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Неудачные операции D3D возвращают значения `detail` и `native_status`. Методы расширения `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) оборачивают эти операции для потребителей API.

<a id="getcapabilitiesasync-names"></a>
## Имена `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` возвращает статический список записей `Capability(Name, Available, EvidenceState, Reason)`, описывающих то, как хост видит установку. Эти имена образуют отдельный, более грубый словарь, чем идентификаторы реестра выше: реестр — это контракт для операций, а список возможностей — машиночитаемая сводка для интеграторов. Связь между ними указана в последнем столбце.

| Имя | Доступно | Подтверждение | Связь |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | зависит от платформы | STATICALLY_VALIDATED | [совместимость](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | runtime-почтовый ящик (mailbox) |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (отклоняется) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. Список — это фиксированный перечень-самоотчёт: эта запись не была обновлена после того, как в раунде runtime closure наблюдались переходы resetting/restored (`D01`); подтверждения см. на странице [статус проверки в runtime](../status/runtime-validation-status.md) |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | режимы мира `session.start` |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year`; их запрос делает план неисполнимым |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`; их запрос делает план неисполнимым |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` и связанные флаги; их запрос делает план неисполнимым |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, `settings` профиля (RV-005 в runtime) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | парсеры документов существуют; `InputSpec` сеансом не применяется (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

Кроме того, планировщик сообщает возможности для каждого плана в `SessionPlan.RequiredCapabilities` и `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

<a id="known-limitations"></a>
## Известные ограничения

- Handle действуют в рамках сеанса; повторное использование адреса обнаруживается по отпечатку объекта (VMT плюс указатель на определение для транспортных средств, VMT плюс индекс человека для объектов Human) и сообщается как `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Остаточная слепая зона: объект того же класса и с тем же определением, пересозданный по тому же адресу между двумя чтениями списка, неотличим от исходного.
- Результаты ограничены почтовым ящиком размером 64 KiB; ограниченные списки усекаются с `truncated=true`; `timetable.logs.read` не ограничен и может завершиться ошибкой `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.
- Нет перемещения транспортных средств между тайлами, нет записи строковых переменных, нет записи календаря, нет записи погоды, нет назначения PlayerVehicle при headless-запуске. См. [известные ограничения](known-limitations.md).
