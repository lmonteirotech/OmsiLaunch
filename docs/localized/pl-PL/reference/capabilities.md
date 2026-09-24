# Możliwości

<!-- l10n: source=reference/capabilities.md -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/capabilities.md) dla OmsiLaunch 0.1.0-beta3. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Jest to kanoniczny spis tego, co potrafi OmsiLaunch 0.1.0-beta3, opracowany na podstawie `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), implementacji runtime we wtyczce (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` i `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`) oraz `OmsiLaunchService.GetCapabilitiesAsync`. Dla każdej możliwości (capability) podano klasyfikację, poziom stabilności stosowany w całej tej dokumentacji, informację, czy wymagana jest sesja w stanie `Running`, czy możliwość modyfikuje OMSI, argumenty i klucze wyniku, błędy oraz dowody weryfikacji w runtime. Sposób wywołania operacji (ścieżki poleceń CLI, koperta API, limity czasu, uchwyty) opisano w [sterowaniu runtime](runtime-control.md); tabela dowodów znajduje się w [stanie weryfikacji runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Klasyfikacja i stabilność

`PublicCapabilityClassification` ma cztery wartości. Odpowiadają one słownictwu stabilności w następujący sposób, z obniżeniem poziomu tam, gdzie dowody są niepełne:

| Klasyfikacja | Znaczenie | Stabilność |
| --- | --- | --- |
| `PublicStableBeta` | Publiczna, objęta kontraktem Beta, zweryfikowana w runtime | `STABLE_BETA` (obniżona do `PARTIAL` tam, gdzie to zaznaczono) |
| `PublicExperimental` | Publiczna, może się zmienić, zweryfikowana w runtime lub zweryfikowana statycznie | `EXPERIMENTAL` (obniżona do `PARTIAL` tam, gdzie to zaznaczono) |
| `InternalOnly` | Prymityw badawczy; niedostępny przez publiczne API ani CLI | `INTERNAL` |
| `Unsupported` | Rozpoznawana na potrzeby diagnostyki, odrzucana lub nieobecna | `UNAVAILABLE` |

`PublicCapabilityKind` rozróżnia `Read`, `Write`, `Action` i `Event`. Każda możliwość runtime wymaga sesji w stanie `Running` i dokładnie profilu `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` w rejestrze); możliwości sesji, które tworzą sesję, wymagają dokładnego profilu, ale nie wymagają sesji.

Wyniki runtime są słownikami ciągów znaków. Klucze zaczynające się od `internal_` lub kończące się na `_address`, `_pointer` albo `_vmt` są usuwane na granicy API (`ScrubInternalValues`) i nie zostały tu wymienione. Klucze wyniku w poniższych tabelach operacji są dokładnymi zestawami kluczy zwracanymi przez produkt: zostały przechwycone przez wykonanie każdej publicznej operacji w rzeczywistej sesji (przechwycenie dokumentacji w rundzie domknięcia runtime, sesja `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; wiersze list zapisywane są jako `<row>.<n>.<field>`).

Sposób zgłaszania niepowodzenia operacji (`CurrentRuntimeControl.Execute`):

| Niepowodzenie | `ErrorCode` | `Values` |
| --- | --- | --- |
| Odrzucenie przez rejestr (nieznana operacja lub operacja `internal.*`, brak wymaganego argumentu) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | brak |
| Celowe odrzucenie przez produkt (`weather.set`) | konkretny kod (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (zdanie) |
| Niepowodzenie D3D | konkretny kod `OL_E_D3D_*` | `detail`, `native_status` |
| Każde inne niepowodzenie po stronie wtyczki (nieaktualny uchwyt, nieznana nazwa, wartość spoza zakresu, brak pojazdu gracza, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (zaczyna się od konkretnego kodu, na przykład `OL_E_RUNTIME_OBJECT_HANDLE_STALE` lub `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (nazwa typu .NET) |
| Wynik, który nie mieści się w skrzynce 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (listy ograniczone są zamiast tego skracane, patrz [Rozkład jazdy, kierowcy, bilety](#timetable-drivers-tickets)) | brak |

Kolumna „Błędy” (`Errors`) każdej tabeli wymienia konkretne kody; należy je odczytywać z `ErrorCode` lub z pierwszego tokenu `Values.detail`, jak opisano powyżej.

<a id="session-capabilities"></a>
## Możliwości sesji

| Możliwość | Klasyfikacja | Stabilność | Rodzaj | Ścieżka API | Ścieżka CLI | Modyfikuje | Weryfikacja |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | nie | RUNTIME_PASS (każda zweryfikowana sesja zaczyna się od planu) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | flagi uruchomienia | system plików (transakcja), proces | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | nie | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, obszar powiadomień, Ctrl+C | proces (`TerminateProcess`), system plików (przywracanie) | RUNTIME_PASS; kooperacyjne zamykanie nie jest zaimplementowane |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | system plików (przywracanie) | RUNTIME_PASS: odzyskiwanie po wczesnym zakończeniu (RV-008), zabity właściciel oraz wczesne odzyskiwanie przy następnym uruchomieniu (`S05`), nieudane przywracanie, a następnie `/recover` (`F01`), odmowa pod dzierżawą, przy osieroconym OMSI i w oknie przed PID (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, sterowanie `session.events` | `events read`, `events watch` | nie | RUNTIME_PASS; slot telemetrii przechowujący tylko najnowszą wartość może gubić serie zdarzeń |

<a id="runtime-capabilities-registry-entries"></a>
## Możliwości runtime (wpisy rejestru)

| Możliwość | Klasyfikacja | Stabilność | Rodzaj | Operacje | Uchwyty | Weryfikacja |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (sesja `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (zapis, `SetTime`, odczyt kontrolny) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: zawsze `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (sesja `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: ponownie zweryfikowana na poprawionym slocie mapy (sesja `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) i odczytana na Berlin-Spandau podczas przechwytywania dokumentacji |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (zapis FOV i odczyt kontrolny) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS z pojazdem PlayerVehicle z zapisanej sytuacji (domknięcie runtime `CAM01`, rodziny 0, 2 i 1 z odczytem kontrolnym, a następnie odblokowanie). Własny ciąg `RuntimeValidation` w rejestrze nadal brzmi `STATICALLY_VALIDATED` (samoopis produktu, niezaktualizowany w tym wydaniu) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS; wykrywanie nieaktualności po naturalnym usunięciu (RV-002) nie ma bezpiecznego źródła w runtime i jest zweryfikowane offline |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (sesja `5f641c8d`, `2 -> 3`, uchwyt `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (kolekcja `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: `present=false` przy uruchomieniu bez pojazdu gracza (headless), pełna migawka przy zapisanej sytuacji |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` oraz rodzina `timetable.*.list` / `timetable.logs.read` | | RUNTIME_PASS (wpisy trasy: 91 rekordów na Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (tylko odczyt; zapisy BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS dla create/describe/update/release, odrzucania zwolnionych i nieaktualnych uchwytów po ponownym utworzeniu i między sesjami (`H02`) oraz resetu urządzenia z unieważnieniem generacji (RV-007, `D01`; sam stan `lost` nie został wywołany) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | Niepubliczna. `ExecuteRuntimeAsync` odrzuca ją z `OL_E_RUNTIME_OPERATION_UNKNOWN` przed jakimkolwiek wyszukaniem sesji; CLI nie ma dla niej ścieżki polecenia. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | brak | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | brak | | UNSUPPORTED (BI-007) |

`internal.road-vehicles.make-basic` ma status `INTERNAL`: istnieje we wtyczce na potrzeby badań (zwraca adres natywny) i jest odrzucana na granicy API oraz przez lokalną płaszczyznę sterowania, które najpierw sprawdzają nazwę operacji względem `PublicRuntimeOperationIds`.

<a id="public-runtime-operations"></a>
## Publiczne operacje runtime

Jest to dokładna lista identyfikatorów operacji, które publiczny frontend może przekazywać dalej (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Każda z nich wymaga `SessionState.Running`; żadna nie jest zapisywana w dzienniku ani przywracana. Wymagane argumenty są egzekwowane przez rejestr, zanim zostanie użyta skrzynka runtime (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); argumenty opcjonalne weryfikuje wtyczka. Wspólne kody błędów dla wszystkich operacji: `OL_E_RUNTIME_OPERATION_UNKNOWN` (brak na tej liście), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (wtyczka nie ma implementacji), `OL_E_RUNTIME_OPERATION_FAILED` (nieoczekiwany wyjątek w procesie; wartości `detail` i `exception`).

<a id="time"></a>
### Czas

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `time.read` | Read | brak | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | opcjonalne `hour` (0..23), `minute` (0..59), `second` (0..59.999, dziesiętne); co najmniej jeden | klucze `time.read` po profilowanym `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Pogoda

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | brak | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | dowolne | brak | Zawsze `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (puste argumenty: `OL_E_RUNTIME_ARGUMENT_REQUIRED`). OMSI nadpisuje obu profilowanych kandydatów wiatru przy następnym takcie pogody. |
| `weather.actual.read` | Read | brak | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mapa

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `map.read` | Read | brak | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Kamera

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | brak | `family` (0 kierowca, 1 pasażer, 2 zewnętrzna, 3 mapa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | opcjonalne `family` (0..3), `field_of_view` (10..170); co najmniej jeden | klucze `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | wymagany `family` (0..3); opcjonalny `preset` (0..255, tylko z rodziną 0 lub 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. Polityka jest ponownie stosowana co 100 ms aż do `camera.unlock`; nieudane ponowne zastosowanie emituje zdarzenie `camera.lock.degraded` raz dla każdego odrębnego błędu. |
| `camera.unlock` | Action | brak | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Pojazdy drogowe i gracz

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | brak | `count`, `ai_collection`, `player_index` (surowy indeks pojazdu gracza w OMSI, zgłaszany tak, jak został odczytany; aby ustalić, czy pojazd gracza istnieje, należy użyć `present` z `player-vehicle.read`) | |
| `road-vehicles.list` | Read | brak | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | wymagany `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | brak | `present` (`false`, gdy OMSI nie ma pojazdu gracza); gdy `true`: `player_index` oraz każdy klucz `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | wymagany `model` (kanoniczna ścieżka `Vehicles\...\*.bus`, najwyżej 240 znaków, bez `..`, musi istnieć w obrębie instalacji) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Nie przypisuje pojazdu PlayerVehicle. Trwa kilka sekund; należy użyć limitu czasu klienta 30 s. |
| `road-vehicles.place-random` | Action | opcjonalne `ai_type` (0..255, domyślnie 0), `group` (0..65535, domyślnie 1), `type` (-1..65535, domyślnie -1), `scheduled` (0..1, domyślnie 0), `tour` (0..65535, domyślnie 0), `line` (0..65535, domyślnie 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Postacie

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | brak | `count` | |
| `humans.list` | Read | brak | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | wymagany `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Rozkład jazdy, kierowcy, bilety

Wyniki list ograniczonych zawierają `count` (wszystkie rekordy w OMSI), `returned_count` (wiersze w tej odpowiedzi) i `truncated` (`true`, gdy część wierszy pominięto). Wiersze to zawsze pierwsze `returned_count` rekordów, numerowane od `0`. Limity wierszy wynoszą 128 dla tras, kursów, linii, plików rv, przystanków, połączeń między przystankami, brygad i profili, 256 dla wpisów brygady i 512 dla wpisów trasy; gdy wiersze dopuszczone przez ten limit nadal przekraczają skrzynkę 64 KiB (na przykład wpisy trasy i wpisy brygady na Berlin-Spandau), wtyczka odrzuca ostatnie wiersze, aż odpowiedź się zmieści, i zgłasza mniejszą wartość `returned_count` z `truncated=true` (audyt dokumentacji BUG-05; przed poprawką te dwie listy kończyły się niepowodzeniem z `OL_E_RUNTIME_RESPONSE_TOO_LARGE`). `timetable.logs.read` nie jest listą ograniczoną: zwraca każdy wpis dziennika, a dziennik liczący kilkaset wpisów przekracza skrzynkę i kończy się niepowodzeniem z `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | brak | `invalid` (`true`/`false`) oraz liczby rekordów `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | brak | `count`, `returned_count`, `truncated`; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | brak | `count`, `returned_count`, `truncated`; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | brak | `count`, `returned_count`, `truncated`; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | brak | `count`, `returned_count`, `truncated`; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | brak | `count`, `returned_count`, `truncated`; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | brak | `count`, `returned_count`, `truncated`; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | brak | `count`, `returned_count`, `truncated`; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | brak | `count`, `returned_count`, `truncated`; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | brak | `count`, `returned_count`, `truncated`; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | brak | `count`, `returned_count`, `truncated`; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | brak | `count`; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (wszystkie wpisy; lista nieograniczona) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` dla długiego dziennika |
| `drivers.read` | Read | brak | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | brak | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Skrypty pojazdu, stałe, krzywe, HOF (w zakresie uchwytu)

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | wymagany `handle` | `handle`, `count`, `returned_count`, `truncated` (limit 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | wymagane `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | wymagane `handle`, `name`, `value` (skończona liczba zmiennoprzecinkowa) | `handle`, `name`, `requested_value`, `value` (odczyt kontrolny) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | wymagany `handle` | `handle`, `count`, `returned_count`, `truncated` (limit 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | wymagane `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | wymagany `handle` | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | wymagane `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | wymagany `handle` | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | wymagane `handle`, `name`, `x` (skończona liczba zmiennoprzecinkowa; brakujący lub nienumeryczny `x` daje `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolacja liniowa) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | wymagany `handle` | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Operacje D3D są wykonywane w wątku głównym/wątku renderowania OMSI za pośrednictwem mostka natywnego. Uchwyty tekstur mają postać `d3dtex-<sessionId N>-<16 hex digits>`.

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | brak | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | wymagane `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); opcjonalny `levels` (0..16, domyślnie 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | wymagany `handle`; opcjonalny `level` (0..15, domyślnie 0) | 13 kluczy `d3d.texture.create` dla żądanego poziomu | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED` oraz błędy create |
| `d3d.texture.update` | Action | wymagane `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (najwyżej 48 KiB po zdekodowaniu); opcjonalne `level` (0..15, domyślnie 0), `x` (0..4095, domyślnie 0), `y` (0..4095, domyślnie 0) | 13 kluczy `d3d.texture.create` po aktualizacji | `OL_E_D3D_INVALID_PIXEL_BUFFER` oraz błędy describe |
| `d3d.texture.release` | Action | wymagany `handle` | 13 kluczy `d3d.texture.create` z `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` przy powtórnym zwolnieniu, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Nieudane operacje D3D zwracają wartości `detail` i `native_status`. Metody rozszerzające `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) opakowują te operacje dla użytkowników API.

<a id="getcapabilitiesasync-names"></a>
## Nazwy `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` zwraca statyczną listę rekordów `Capability(Name, Available, EvidenceState, Reason)`, opisujących instalację z perspektywy hosta. Te nazwy tworzą odrębne, mniej szczegółowe słownictwo niż powyższe identyfikatory rejestru; rejestr jest kontraktem dla operacji, a lista możliwości – odczytywalnym maszynowo podsumowaniem dla integratorów. Zależność między nimi podano w ostatniej kolumnie.

| Nazwa | Dostępna | Dowody | Zależność |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | zależnie od platformy | STATICALLY_VALIDATED | [zgodność](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | skrzynka runtime |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (odrzucana) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. Lista jest stałym spisem zgłaszanym przez sam produkt: ten wpis nie został zaktualizowany po tym, jak runda domknięcia runtime zaobserwowała przejścia resetting/restored (`D01`); dowody opisano w [stanie weryfikacji runtime](../status/runtime-validation-status.md) |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | tryby świata `session.start` |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year`; zażądanie ich sprawia, że plan staje się niemożliwy do uruchomienia |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`; zażądanie ich sprawia, że plan staje się niemożliwy do uruchomienia |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` i powiązane flagi; zażądanie ich sprawia, że plan staje się niemożliwy do uruchomienia |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, `settings` profilu (RV-005 w runtime) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | parsery dokumentów istnieją; `InputSpec` nie jest stosowany przez sesję (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

Planista dodatkowo zgłasza możliwości dla poszczególnych planów w `SessionPlan.RequiredCapabilities` i `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

<a id="known-limitations"></a>
## Znane ograniczenia

- Uchwyty mają zakres sesji; ponowne użycie adresu jest wykrywane na podstawie odcisku obiektu (VMT plus wskaźnik definicji dla pojazdów, VMT plus indeks postaci dla postaci) i zgłaszane jako `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Pozostały martwy punkt: obiektu tej samej klasy i tej samej definicji, ponownie utworzonego pod tym samym adresem między dwoma odczytami listy, nie da się odróżnić od oryginału.
- Wyniki są ograniczone do skrzynki 64 KiB; listy ograniczone są obcinane z `truncated=true`; `timetable.logs.read` nie jest ograniczona i może zakończyć się niepowodzeniem z `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.
- Brak przenoszenia pojazdów między kafelkami, brak zapisu zmiennych tekstowych, brak zapisu kalendarza, brak zapisu pogody, brak przypisywania pojazdu PlayerVehicle w trybie headless. Patrz [znane ograniczenia](known-limitations.md).
