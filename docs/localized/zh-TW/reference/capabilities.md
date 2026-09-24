# 功能

<!-- l10n: source=reference/capabilities.md -->
> 本頁為 OmsiLaunch 0.1.0-beta3 [英文原始頁面](../../../reference/capabilities.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁是 OmsiLaunch 0.1.0-beta3 可執行之功能的標準清單，依據 `PublicCapabilityRegistry`（`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`）、外掛程式中的執行階段實作（`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` 與 `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`）以及 `OmsiLaunchService.GetCapabilitiesAsync` 整理而成。每一項功能（capability）皆列出其分類、本文件通用的穩定性等級、是否需要處於 `Running` 的工作階段、是否會變更 OMSI、參數與結果鍵、錯誤，以及執行階段驗證證據。如何呼叫操作（CLI 路由、API 封套、逾時、handle）請參閱[執行階段控制](runtime-control.md)；證據表請參閱[執行階段驗證狀態](../status/runtime-validation-status.md)。

<a id="classification-and-stability"></a>
## 分類與穩定性

`PublicCapabilityClassification` 有四個值。它們與穩定性用語的對應如下；證據不完整之處會降級：

| 分類 | 意義 | 穩定性 |
| --- | --- | --- |
| `PublicStableBeta` | 公開、屬於 Beta 合約、已於執行階段驗證 | `STABLE_BETA`（註明之處降級為 `PARTIAL`） |
| `PublicExperimental` | 公開、可能變更、已於執行階段驗證或已靜態驗證 | `EXPERIMENTAL`（註明之處降級為 `PARTIAL`） |
| `InternalOnly` | 研究用基本操作；無法透過公開 API 或 CLI 存取 | `INTERNAL` |
| `Unsupported` | 為診斷而辨識，但會被拒絕或不存在 | `UNAVAILABLE` |

`PublicCapabilityKind` 區分 `Read`、`Write`、`Action` 與 `Event`。每一項執行階段功能都需要處於 `Running` 的工作階段以及完全相符的設定檔 `Omsi23004_692EBFBF`（登錄中的 `RequiresSession`／`RequiresExactProfile`）；會建立工作階段的工作階段功能需要完全相符的設定檔，但不需要工作階段。

執行階段結果是字串字典。以 `internal_` 開頭或以 `_address`、`_pointer`、`_vmt` 結尾的鍵會在 API 邊界被移除（`ScrubInternalValues`），因此不列於此處。下列操作表中的結果鍵即為產品回傳的確切鍵集合：它們是在實際工作階段中執行每一項公開操作所擷取的（runtime closure 文件擷取，工作階段 `f51dcb59-a723-4570-b6c5-b61e628a7993`，`situations\Linie 5.osn`；清單資料列寫作 `<row>.<n>.<field>`）。

操作失敗的回報方式（`CurrentRuntimeControl.Execute`）：

| 失敗 | `ErrorCode` | `Values` |
| --- | --- | --- |
| 登錄拒絕（未知或 `internal.*` 操作、缺少必要參數） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | 無 |
| 產品刻意拒絕（`weather.set`） | 特定錯誤碼（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`） | `detail`（一句說明） |
| D3D 失敗 | 特定的 `OL_E_D3D_*` 錯誤碼 | `detail`、`native_status` |
| 其他任何外掛程式端失敗（失效的 handle、未知名稱、值超出範圍、沒有玩家車輛……） | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`（以特定錯誤碼開頭，例如 `OL_E_RUNTIME_OBJECT_HANDLE_STALE` 或 `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`）、`exception`（.NET 型別名稱） |
| 無法放入 64 KiB 信箱（mailbox）的結果 | `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（有界清單則改為縮短，請參閱[時刻表、駕駛員、車票](#timetable-drivers-tickets)） | 無 |

各表的 `Errors`（「錯誤」）欄列出特定錯誤碼；請依上述方式從 `ErrorCode` 或 `Values.detail` 的第一個語彙單元讀取。

<a id="session-capabilities"></a>
## 工作階段功能

| 功能 | 分類 | 穩定性 | 種類 | API 路由 | CLI 路由 | 變更 | 驗證 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`、`/validate` | 否 | RUNTIME_PASS（每個經驗證的工作階段都從計畫開始） |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | 啟動旗標 | 檔案系統（交易）、處理程序 | RUNTIME_PASS（NEW_MAP Grundorf、SAVED_SITUATION Berlin-Spandau） |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | 否 | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync`／`CloseAsync` | `session stop`、系統匣、Ctrl+C | 處理程序（`TerminateProcess`）、檔案系統（還原） | RUNTIME_PASS；協同式關閉未實作 |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`、`/recover` | 檔案系統（還原） | RUNTIME_PASS：提前結束後的復原（RV-008）、擁有者遭終止後於下次啟動時提前復原（`S05`）、還原失敗後執行 `/recover`（`F01`）、在租約下、存在孤立 OMSI 時以及在取得 PID 之前的時段中拒絕（`S04`、`S04b`） |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`、控制命令 `session.events` | `events read`、`events watch` | 否 | RUNTIME_PASS；僅保留最新值的遙測槽位可能遺失突發事件 |

<a id="runtime-capabilities-registry-entries"></a>
## 執行階段功能（登錄項目）

| 功能 | 分類 | 穩定性 | 種類 | 操作 | Handle | 驗證 |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS（工作階段 `e5454061`） |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS（寫入、`SetTime`、回讀） |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED：一律回傳 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（工作階段 `50a1f1ec`） |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS：已於修正後的地圖槽位重新驗證（工作階段 `9dd62626-94c6-4cd7-bb6f-0288327696f4`，Grundorf），並於文件擷取中在 Berlin-Spandau 上讀取 |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS（FOV 寫入與回讀） |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`、`camera.unlock` | | RUNTIME_PASS，使用來自已儲存情境的 PlayerVehicle（runtime closure `CAM01`，family 0、2 與 1 並回讀，之後解除鎖定）。登錄本身的 `RuntimeValidation` 字串仍為 `STATICALLY_VALIDATED`（產品自我回報，本版未更新） |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS；自然移除後的失效偵測（RV-002）沒有安全的執行階段產生方式，已以離線方式驗證 |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003（工作階段 `5f641c8d`，`2 -> 3`，handle `rv-000003`） |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS（集合 `2 -> 4`） |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS：無頭（headless）啟動時為 `present=false`，使用已儲存情境時為完整快照 |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` 以及 `timetable.*.list`／`timetable.logs.read` 系列 | | RUNTIME_PASS（路線軌跡項目（track entry）：Grundorf 上 91 筆記錄） |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`、`vehicle.variable.get`、`vehicle.variable.set` | RoadVehicle | RUNTIME_PASS（`Refresh_Strings` 0 -> 1 -> 0） |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`、`vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS（僅讀取；寫入見 BI-004） |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`、`vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`、`vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`、`d3d.texture.create`、`d3d.texture.describe`、`d3d.texture.update`、`d3d.texture.release` | D3DTexture | create／describe／update／release 為 RUNTIME_PASS；已釋放與失效 handle 在重新建立及跨工作階段時皆被拒絕（`H02`）；裝置重設並使世代失效（RV-007、`D01`；`lost` 狀態本身未被產生） |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | 不公開。`ExecuteRuntimeAsync` 會在任何工作階段查詢之前以 `OL_E_RUNTIME_OPERATION_UNKNOWN` 拒絕它；CLI 沒有對應的路由。 |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | 無 | | UNSUPPORTED（BI-002） |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | 無 | | UNSUPPORTED（BI-007） |

`internal.road-vehicles.make-basic` 為 `INTERNAL`：它存在於外掛程式中供研究使用（會回傳原生位址），並會在 API 邊界及本機控制平面被拒絕；兩者都會先以 `PublicRuntimeOperationIds` 驗證操作名稱。

<a id="public-runtime-operations"></a>
## 公開執行階段操作

以下是公開前端可轉送之操作 id 的確切清單（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）。每一項都需要 `SessionState.Running`；沒有任何一項會記入日誌或被還原。必要參數在使用信箱之前由登錄強制檢查（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）；選用參數由外掛程式驗證。所有操作的共通失敗錯誤碼：`OL_E_RUNTIME_OPERATION_UNKNOWN`（不在此清單中）、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_RUNTIME_CHANNEL_BUSY`、`OL_E_RUNTIME_CHANNEL_CLOSED`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_REQUEST_ID_REUSED`、`OL_E_RUNTIME_RESPONSE_INVALID`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`（外掛程式沒有實作）、`OL_E_RUNTIME_OPERATION_FAILED`（非預期的處理程序內例外；附 `detail` 與 `exception` 值）。

<a id="time"></a>
### 時間

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `time.read` | Read | 無 | `hour`、`minute`、`second`、`day`、`month`、`year` | |
| `time.set` | Write | 選用 `hour`（0..23）、`minute`（0..59）、`second`（0..59.999，小數）；至少一項 | 經設定檔定義之 `SetTime` 後的 `time.read` 鍵 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### 天氣

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | 無 | `fog_density`、`lightness`、`primary_light_factor`、`secondary_light_factor`、`ambient_light_factor`、`cloud_type`、`cloud_transparency`、`precipitation_set`、`wet_ground`、`wind_speed`、`wind_direction`、`relative_humidity`、`absolute_humidity`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate` | |
| `weather.set` | Write | 任意 | 無 | 一律為 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（參數為空時：`OL_E_RUNTIME_ARGUMENT_REQUIRED`）。OMSI 會在下一次天氣更新時覆寫兩個經設定檔定義的風力候選位置。 |
| `weather.actual.read` | Read | 無 | `active`、`icao`、`last_downloaded`、`invalid_icao`、`counter`、`process` | |

<a id="map"></a>
### 地圖

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `map.read` | Read | 無 | `loaded`、`loaded_tiles`、`left_hand_traffic`、`name`、`filename`、`friendly_name`、`description`、`max_speed`、`year_start`、`year_end` | |

<a id="camera"></a>
### 攝影機

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | 無 | `family`（0 駕駛員、1 乘客、2 外部、3 地圖）、`name`、`field_of_view`、`normal_field_of_view`、`distance` | |
| `camera.set` | Write | 選用 `family`（0..3）、`field_of_view`（10..170）；至少一項 | `camera.read` 的鍵 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | 必要 `family`（0..3）；選用 `preset`（0..255，僅限 family 0 或 1） | `locked`（`true`）、`family`、`preset`、`head_look`（`preserved`）、`field_of_view`（`preserved`） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`。此原則每 100 ms 重新套用一次，直到 `camera.unlock`；重新套用失敗時，每種不同錯誤只會發出一次 `camera.lock.degraded` 事件。 |
| `camera.unlock` | Action | 無 | `locked`（`false`） | |

<a id="road-vehicles-and-player"></a>
### 道路車輛與玩家

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | 無 | `count`、`ai_collection`、`player_index`（OMSI 原始的玩家車輛索引，照讀取值回報；要知道是否存在玩家車輛，請使用 `player-vehicle.read` 的 `present`） | |
| `road-vehicles.list` | Read | 無 | `count`、`vehicle.<n>.handle`（`rv-NNNNNN`） | |
| `road-vehicle.read` | Read | 必要 `handle` | `handle`、`runtime_index`、`tile`、`marked_for_killing`、`traffic_type`、`position_x`、`position_y`、`position_z`、`rotation_x`、`rotation_y`、`rotation_z`、`rotation_w`、`steering`、`tacho`、`ground_speed`、`kilometres`、`throttle`、`brake`、`clutch`、`ai_enabled`、`ai_mode`、`ai_light`、`ai_interior_light`、`ai_indicator_left`、`ai_indicator_right`、`ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | 無 | `present`（OMSI 沒有玩家車輛時為 `false`）；為 `true` 時：`player_index` 加上所有 `road-vehicle.read` 的鍵 | |
| `road-vehicles.spawn` | Action | 必要 `model`（標準形式 `Vehicles\...\*.bus`，最多 240 個字元，不可含 `..`，必須存在於安裝目錄之下） | `bus`、`created_handle`、`before_count`、`after_count`、`delta_count`、`raw_native_return`、`identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`。不會指派 PlayerVehicle。需要數秒；請使用 30 s 的用戶端逾時。 |
| `road-vehicles.place-random` | Action | 選用 `ai_type`（0..255，預設 0）、`group`（0..65535，預設 1）、`type`（-1..65535，預設 -1）、`scheduled`（0..1，預設 0）、`tour`（0..65535，預設 0）、`line`（0..65535，預設 0） | `raw_return`、`before_count`、`after_count`、`delta_count`、`identity_validation`（`native-placement-return-is-diagnostic-only`） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### 人物

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | 無 | `count` | |
| `humans.list` | Read | 無 | `count`、`human.<n>.handle`（`hb-NNNNNN`） | |
| `human.read` | Read | 必要 `handle` | `handle`、`runtime_index`、`human_index`、`tile`、`marked_for_killing`、`collision_type`、`position_x`、`position_y`、`position_z`、`target_x`、`target_y`、`target_z`、`target_station`、`pre_target_station`、`departure`、`enter_bus_at`、`seat_bus`、`seat_station`、`ticket_type`、`ticket_index`、`ticket_ready`、`state`、`speed`、`bus_index`、`station`、`ai_mode`、`ai_mode_ex`、`ai_sub_mode`、`collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### 時刻表、駕駛員、車票

有界清單結果包含 `count`（OMSI 中的所有記錄數）、`returned_count`（此回覆中的資料列數）與 `truncated`（有資料列被省略時為 `true`）。資料列一律是前 `returned_count` 筆記錄，從 `0` 開始編號。資料列上限為：tracks、trips、lines、rv-files、bus stops、station links、tours 與 profiles 為 128，班次項目（tour entry）為 256，路線軌跡項目為 512；若該上限允許的資料列仍超過 64 KiB 信箱（例如 Berlin-Spandau 上的路線軌跡項目與班次項目），外掛程式會捨棄最後的資料列直到回覆能夠容納，並以 `truncated=true` 回報較小的 `returned_count`（文件稽核 BUG-05；修正之前，這兩個清單會以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失敗）。`timetable.logs.read` 不是有界清單：它會回傳每一筆記錄項目，數百筆項目的記錄即會超過信箱並以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失敗。

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | 無 | `invalid`（`true`/`false`），以及記錄數 `tracks`、`trips`、`bus_stops`、`station_links`、`lines`、`rv_files` | |
| `timetable.tracks.list` | Read | 無 | `count`、`returned_count`、`truncated`；`track.<n>.filename`、`track.<n>.path`、`track.<n>.entries`、`track.<n>.length` | |
| `timetable.trips.list` | Read | 無 | `count`、`returned_count`、`truncated`；`trip.<n>.filename`、`trip.<n>.chrono_origin`、`trip.<n>.target`、`trip.<n>.line`、`trip.<n>.track_index`、`trip.<n>.track_name`、`trip.<n>.bus_stops`、`trip.<n>.profiles`、`trip.<n>.train_reverse`、`trip.<n>.invalid` | |
| `timetable.lines.list` | Read | 無 | `count`、`returned_count`、`truncated`；`line.<n>.name`、`line.<n>.chrono_origin`、`line.<n>.priority`、`line.<n>.tours`、`line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | 無 | `count`、`returned_count`、`truncated`；`rv_file.<n>.line`、`rv_file.<n>.number_tours`、`rv_file.<n>.start_date_rel_2000`、`rv_file.<n>.end_date_rel_2000`、`rv_file.<n>.type_line_files`、`rv_file.<n>.type_line_probability`、`rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | 無 | `count`、`returned_count`、`truncated`；`track_entry.<n>.track_index`、`track_entry.<n>.id`、`track_entry.<n>.path_index_on_object`、`track_entry.<n>.path_tile`、`track_entry.<n>.path_index`、`track_entry.<n>.relative_distance`、`track_entry.<n>.distance`、`track_entry.<n>.valid`、`track_entry.<n>.path_order_check`、`track_entry.<n>.allowed_fstrn`、`track_entry.<n>.chrono_origin`、`track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | 無 | `count`、`returned_count`、`truncated`；`bus_stop.<n>.name`、`bus_stop.<n>.supplement`、`bus_stop.<n>.tile`、`bus_stop.<n>.id`、`bus_stop.<n>.parent_id`、`bus_stop.<n>.preset_alighting`、`bus_stop.<n>.index`、`bus_stop.<n>.starting_links`、`bus_stop.<n>.ending_links`、`bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | 無 | `count`、`returned_count`、`truncated`；`station_link.<n>.length`、`station_link.<n>.start_bus_stop_id`、`station_link.<n>.end_bus_stop_id`、`station_link.<n>.start_bus_stop`、`station_link.<n>.end_bus_stop`、`station_link.<n>.chrono_origin`、`station_link.<n>.valid`、`station_link.<n>.visible`、`station_link.<n>.track_entries`、`station_link.<n>.start_track_entry`、`station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | 無 | `count`、`returned_count`、`truncated`；`tour.<n>.line_index`、`tour.<n>.name`、`tour.<n>.ai_group`、`tour.<n>.ai_group_index`、`tour.<n>.ai_type`、`tour.<n>.completed_day`、`tour.<n>.entries`、`tour.<n>.has_normal_vehicle`、`tour.<n>.invalid`、`tour.<n>.vehicle_indices`、`tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | 無 | `count`、`returned_count`、`truncated`；`profile.<n>.trip_index`、`profile.<n>.name`、`profile.<n>.service_trip`、`profile.<n>.stop_times`、`profile.<n>.total_time`、`profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | 無 | `count`、`returned_count`、`truncated`；`tour_entry.<n>.line_index`、`tour_entry.<n>.tour_index`、`tour_entry.<n>.trip`、`tour_entry.<n>.trip_index`、`tour_entry.<n>.profile_index`、`tour_entry.<n>.start_time`、`tour_entry.<n>.end_time`、`tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | 無 | `count`；`log.<n>.bus_stop`、`log.<n>.estimated_arrival`、`log.<n>.estimated_departure`、`log.<n>.actual_arrival`、`log.<n>.actual_departure`、`log.<n>.arrival_ok`、`log.<n>.departure_ok`（所有項目；非有界） | 記錄很長時為 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` |
| `drivers.read` | Read | 無 | `count`、`selected_index`；`driver.<n>.filename`、`driver.<n>.name`、`driver.<n>.gender`、`driver.<n>.bus_stops`、`driver.<n>.crashes`、`driver.<n>.passengers`、`driver.<n>.tickets`、`driver.<n>.cash` | |
| `tickets.read` | Read | 無 | `filename`、`voice_path`、`stamper_factor`、`buy_factor`、`chattiness`、`whinge_factor`、`count`；`ticket.<n>.name`、`ticket.<n>.display_name`、`ticket.<n>.value`、`ticket.<n>.maximum_stations`、`ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### 車輛腳本、常數、曲線、HOF（以 handle 為範圍）

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | 必要 `handle` | `handle`、`count`、`returned_count`、`truncated`（上限 512）、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | 必要 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | 必要 `handle`、`name`、`value`（有限浮點數） | `handle`、`name`、`requested_value`、`value`（回讀值） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | 必要 `handle` | `handle`、`count`、`returned_count`、`truncated`（上限 512）、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | 必要 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | 必要 `handle` | `handle`、`count`、`name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | 必要 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | 必要 `handle` | `handle`、`count`、`name.<n>` | |
| `vehicle.curve.evaluate` | Read | 必要 `handle`、`name`、`x`（有限浮點數；缺少 `x` 或非數值時為 `OL_E_RUNTIME_ARGUMENT_REQUIRED`） | `handle`、`name`、`x`、`value`（線性內插） | `OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | 必要 `handle` | `handle`、`count`、`hof.<n>.name`、`hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D 操作透過原生橋接層在 OMSI 的主執行緒／轉譯執行緒上執行。材質 handle 的格式為 `d3dtex-<sessionId N>-<16 hex digits>`。

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | 無 | `available`、`native_status`、`query_interface_hresult`、`cooperative_level_hresult`、`execution_thread_id`、`owned_device_references`、`state`（`NOT_READY`、`READY`、`LOST`、`RESETTING`、`STOPPING`、`STOPPED`）、`transition`、`generation`、`live_textures`、`reset_hook_installed`、`last_reset_thread_id`、`binding` | |
| `d3d.texture.create` | Action | 必要 `width`（1..4096）、`height`（1..4096）、`format`（`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）；選用 `levels`（0..16，預設 1） | `handle`、`state`（`LIVE`、`RELEASED`、`STALE`）、`device_state`、`generation`、`width`、`height`、`format`、`levels`、`level`、`level_width`、`level_height`、`hresult`、`execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`、`OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST`、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | 必要 `handle`；選用 `level`（0..15，預設 0） | 所要求層級的 13 個 `d3d.texture.create` 鍵 | `OL_E_D3D_STALE_RESOURCE_HANDLE`、`OL_E_D3D_RESOURCE_RELEASED`，以及 create 的錯誤 |
| `d3d.texture.update` | Action | 必要 `handle`、`width`（1..4096）、`height`（1..4096）、`pixels_base64`（解碼後最多 48 KiB）；選用 `level`（0..15，預設 0）、`x`（0..4095，預設 0）、`y`（0..4095，預設 0） | 更新後的 13 個 `d3d.texture.create` 鍵 | `OL_E_D3D_INVALID_PIXEL_BUFFER`，以及 describe 的錯誤 |
| `d3d.texture.release` | Action | 必要 `handle` | 13 個 `d3d.texture.create` 鍵，其中 `state` = `RELEASED` | 重複釋放時為 `OL_E_D3D_RESOURCE_RELEASED`、`OL_E_D3D_STALE_RESOURCE_HANDLE` |

D3D 操作失敗時會回傳 `detail` 與 `native_status` 值。`D3DRuntimeApi` 擴充方法（`GetD3DStatusAsync`、`CreateD3DTextureAsync`、`DescribeD3DTextureAsync`、`UpdateD3DTextureAsync`、`ReleaseD3DTextureAsync`）為 API 使用者包裝了這些操作。

<a id="getcapabilitiesasync-names"></a>
## `GetCapabilitiesAsync` 名稱

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` 會回傳由 `Capability(Name, Available, EvidenceState, Reason)` 記錄構成的靜態清單，描述主機對該 OMSI 安裝的看法。這些名稱是與上述登錄 id 分開、粒度較粗的另一套用語；登錄是操作的合約，功能清單則是提供給整合者的機器可讀摘要。兩者的關係列於最後一欄。

| 名稱 | 可用 | 證據 | 關係 |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | 視平台而定 | STATICALLY_VALIDATED | [相容性](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | 執行階段信箱 |
| `runtime.time.read`、`runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`、`time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`、`runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`、`weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set`（被拒絕） |
| `runtime.camera.read`、`runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`、`camera.set` |
| `runtime.d3d.status`、`runtime.d3d.texture.create`、`runtime.d3d.texture.update`、`runtime.d3d.texture.describe`、`runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007。此清單是固定的自我回報清單：在 runtime closure 輪次觀察到 resetting／restored 轉換（`D01`）之後，這個項目並未更新；證據請參閱[執行階段驗證狀態](../status/runtime-validation-status.md) |
| `runtime.road-vehicles.read`、`runtime.road-vehicles.spawn`、`runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`、`runtime.timetable.read`、`runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`、`timetable.*` |
| `world.new-map`、`world.presented-entrypoint`、`world.saved-situation` | true | RUNTIME_VALIDATED | `session.start` 世界模式 |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState`（`/last`） |
| `world.date.explicit`、`world.date.system`、`world.time.explicit`、`world.time.system` | false | STATICALLY_PARTIAL | `/date`、`/time`、`/year`；要求這些項目會使計畫不可執行 |
| `weather.preset`、`weather.icao`、`weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`；要求這些項目會使計畫不可執行 |
| `player-vehicle.model`、`player-vehicle.repaint`、`player-vehicle.hof`、`player-vehicle.fleet-number`、`player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` 及相關旗標；要求這些項目會使計畫不可執行 |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`、設定檔 `settings`（RV-005 執行階段） |
| `input.keyboard.patch`、`input.controller.active-ffscale` | true | STATICALLY_VALIDATED | 文件剖析器已存在；`InputSpec` 不會由工作階段套用（BI-005） |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`、`content.situations`、`content.vehicles`、`content.repaints`、`content.hofs`、`content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`、`/list` |

規劃器另外會在 `SessionPlan.RequiredCapabilities` 與 `SessionPlan.UnsupportedRequestedFeatures` 中回報各計畫的功能（`runtime.current-windows-x64`、`transaction.exact-restore`、`omsi.profile.OMSI23004`、`world.new-map`、`world.presented-entrypoint`、`world.entrypoint-identity`、`world.saved-situation`、`boot.headless-start`、`internet-textures.disabled`、`world.explicit-date`、`world.explicit-time`、`world.explicit-year`、`weather`、`player-vehicle.*`、`input.keyboard`、`input.controller`、`content.*`）。

<a id="known-limitations"></a>
## 已知限制

- Handle 以工作階段為範圍；位址重複使用會透過物件指紋偵測（車輛為 VMT 加上定義指標，人物為 VMT 加上人物索引），並回報為 `OL_E_RUNTIME_OBJECT_HANDLE_STALE`。殘留盲點：在兩次清單讀取之間，於同一位址重新建立之相同類別、相同定義的物件，無法與原物件區分。
- 結果受限於 64 KiB 信箱；有界清單會截斷並標示 `truncated=true`；`timetable.logs.read` 不是有界清單，可能以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失敗。
- 不支援跨圖塊（tile）移動車輛、字串變數寫入、行事曆寫入、天氣寫入，也不支援無頭模式下的 PlayerVehicle 指派。請參閱[已知限制](known-limitations.md)。
