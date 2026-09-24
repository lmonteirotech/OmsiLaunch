# 能力

<!-- l10n: source=reference/capabilities.md -->
> 本页是 OmsiLaunch 0.1.0-beta3 [英文原始页面](../../../reference/capabilities.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页是 OmsiLaunch 0.1.0-beta3 功能的规范清单，依据 `PublicCapabilityRegistry`（`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`）、插件中的运行时实现（`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` 和 `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`）以及 `OmsiLaunchService.GetCapabilitiesAsync` 整理而成。对每项能力，本页列出其分类、本文档统一使用的稳定性级别、是否需要处于 `Running` 状态的会话、是否会修改 OMSI、参数与结果键、错误以及运行时验证证据。如何调用操作（CLI 路由、API 信封（envelope）、超时、句柄）见[运行时控制](runtime-control.md)；证据表见[运行时验证状态](../status/runtime-validation-status.md)。

<a id="classification-and-stability"></a>
## 分类与稳定性

`PublicCapabilityClassification` 有四个取值。它们与稳定性术语的对应关系如下，证据不完整之处会降级：

| 分类 | 含义 | 稳定性 |
| --- | --- | --- |
| `PublicStableBeta` | 公开，属于 Beta 契约，经运行时验证 | `STABLE_BETA`（注明之处降级为 `PARTIAL`） |
| `PublicExperimental` | 公开，可能变更，经运行时验证或经静态验证 | `EXPERIMENTAL`（注明之处降级为 `PARTIAL`） |
| `InternalOnly` | 研究用原语；无法通过公共 API 或 CLI 访问 | `INTERNAL` |
| `Unsupported` | 可被识别以用于诊断，但会被拒绝或不存在 | `UNAVAILABLE` |

`PublicCapabilityKind` 区分 `Read`、`Write`、`Action` 和 `Event`。每项运行时能力都需要处于 `Running` 状态的会话以及精确匹配的配置 `Omsi23004_692EBFBF`（注册表中的 `RequiresSession` / `RequiresExactProfile`）；创建会话的会话能力需要精确匹配的配置，但不需要会话。

运行时结果是字符串字典。以 `internal_` 开头或以 `_address`、`_pointer`、`_vmt` 结尾的键会在 API 边界处被移除（`ScrubInternalValues`），本页不予列出。下文各操作表中的结果键是产品实际返回的精确键集合：它们是在真实会话中执行每个公开操作后采集的（运行时收尾文档采集，会话 `f51dcb59-a723-4570-b6c5-b61e628a7993`，`situations\Linie 5.osn`；列表行写作 `<row>.<n>.<field>`）。

操作失败的报告方式（`CurrentRuntimeControl.Execute`）：

| 失败 | `ErrorCode` | `Values` |
| --- | --- | --- |
| 注册表拒绝（未知操作或 `internal.*` 操作、缺少必需参数） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | 无 |
| 产品有意的拒绝（`weather.set`） | 具体错误码（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`） | `detail`（一句说明） |
| D3D 失败 | 具体的 `OL_E_D3D_*` 错误码 | `detail`、`native_status` |
| 任何其他插件端失败（失效句柄、未知名称、值超出范围、无玩家车辆……） | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`（以具体错误码开头，例如 `OL_E_RUNTIME_OBJECT_HANDLE_STALE` 或 `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`）、`exception`（.NET 类型名） |
| 结果无法放入 64 KiB 邮箱 | `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（有界列表则改为缩短，见[时刻表、司机、车票](#timetable-drivers-tickets)） | 无 |

各表的 `Errors` 列列出具体错误码；按上文所述，从 `ErrorCode` 或 `Values.detail` 的第一个标记读取。

<a id="session-capabilities"></a>
## 会话能力

| 能力 | 分类 | 稳定性 | 类型 | API 路由 | CLI 路由 | 修改 | 验证 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`、`/validate` | 否 | RUNTIME_PASS（每个经验证的会话都以规划开始） |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | 启动参数 | 文件系统（事务）、进程 | RUNTIME_PASS（NEW_MAP Grundorf、SAVED_SITUATION Berlin-Spandau） |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | 否 | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`、托盘、Ctrl+C | 进程（`TerminateProcess`）、文件系统（还原） | RUNTIME_PASS；协作式关闭未实现 |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`、`/recover` | 文件系统（还原） | RUNTIME_PASS：提前退出恢复（RV-008）、所有者被终止后在下次启动时提前恢复（`S05`）、还原失败后执行 `/recover`（`F01`）、在租约下、存在孤立 OMSI 时以及在 PID 之前的时间窗口内拒绝（`S04`、`S04b`） |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`、控制命令 `session.events` | `events read`、`events watch` | 否 | RUNTIME_PASS；仅保存最新值的遥测槽可能丢失突发事件 |

<a id="runtime-capabilities-registry-entries"></a>
## 运行时能力（注册表条目）

| 能力 | 分类 | 稳定性 | 类型 | 操作 | 句柄 | 验证 |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS（会话 `e5454061`） |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS（写入、`SetTime`、回读） |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED：始终返回 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（会话 `50a1f1ec`） |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS：已在修正后的地图槽上重新验证（会话 `9dd62626-94c6-4cd7-bb6f-0288327696f4`，Grundorf），并在文档采集中于 Berlin-Spandau 上读取 |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS（FOV 写入与回读） |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`、`camera.unlock` | | 使用来自已保存情景的 PlayerVehicle 时 RUNTIME_PASS（运行时收尾 `CAM01`，family 0、2 和 1 并回读，随后解锁）。注册表自身的 `RuntimeValidation` 字符串仍为 `STATICALLY_VALIDATED`（产品自报信息，本版本未更新） |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS；自然移除后的失效检测（RV-002）没有安全的运行时触发方式，已通过离线验证 |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003（会话 `5f641c8d`，`2 -> 3`，句柄 `rv-000003`） |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS（集合 `2 -> 4`） |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS：headless 启动时为 `present=false`，使用已保存情景时为完整快照 |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` 以及 `timetable.*.list` / `timetable.logs.read` 系列 | | RUNTIME_PASS（线路轨迹条目：Grundorf 上 91 条记录） |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`、`vehicle.variable.get`、`vehicle.variable.set` | RoadVehicle | RUNTIME_PASS（`Refresh_Strings` 0 -> 1 -> 0） |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`、`vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS（仅读取；写入见 BI-004） |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`、`vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`、`vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`、`d3d.texture.create`、`d3d.texture.describe`、`d3d.texture.update`、`d3d.texture.release` | D3DTexture | create/describe/update/release 均为 RUNTIME_PASS；已释放句柄与失效句柄在重新创建后及跨会话时均被拒绝（`H02`）；设备重置时代次失效（RV-007、`D01`；`lost` 状态本身未被触发） |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | 非公开。`ExecuteRuntimeAsync` 在查找任何会话之前即以 `OL_E_RUNTIME_OPERATION_UNKNOWN` 拒绝它；CLI 没有对应路由。 |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | 无 | | UNSUPPORTED（BI-002） |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | 无 | | UNSUPPORTED（BI-007） |

`internal.road-vehicles.make-basic` 属于 `INTERNAL`：它存在于插件中供研究使用（会返回一个原生地址），在 API 边界处以及本地控制平面中都会被拒绝，二者都会先依据 `PublicRuntimeOperationIds` 校验操作名称。

<a id="public-runtime-operations"></a>
## 公开运行时操作

以下是公开前端可以转发的操作 ID 的精确列表（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）。每个操作都要求 `SessionState.Running`；均不记入事务日志，也不会被还原。必需参数由注册表在使用邮箱之前强制检查（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）；可选参数由插件校验。所有操作通用的失败码：`OL_E_RUNTIME_OPERATION_UNKNOWN`（不在此列表中）、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_RUNTIME_CHANNEL_BUSY`、`OL_E_RUNTIME_CHANNEL_CLOSED`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_REQUEST_ID_REUSED`、`OL_E_RUNTIME_RESPONSE_INVALID`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`（插件没有实现）、`OL_E_RUNTIME_OPERATION_FAILED`（进程内意外异常；带有 `detail` 和 `exception` 值）。

<a id="time"></a>
### 时间

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `time.read` | Read | 无 | `hour`、`minute`、`second`、`day`、`month`、`year` | |
| `time.set` | Write | 可选 `hour`（0..23）、`minute`（0..59）、`second`（0..59.999，小数）；至少一个 | 经过配置化的 `SetTime` 之后的 `time.read` 键 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### 天气

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | 无 | `fog_density`、`lightness`、`primary_light_factor`、`secondary_light_factor`、`ambient_light_factor`、`cloud_type`、`cloud_transparency`、`precipitation_set`、`wet_ground`、`wind_speed`、`wind_direction`、`relative_humidity`、`absolute_humidity`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate` | |
| `weather.set` | Write | 任意 | 无 | 始终返回 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（参数为空时：`OL_E_RUNTIME_ARGUMENT_REQUIRED`）。OMSI 会在下一次天气更新时覆盖两个已配置化的风力候选值。 |
| `weather.actual.read` | Read | 无 | `active`、`icao`、`last_downloaded`、`invalid_icao`、`counter`、`process` | |

<a id="map"></a>
### 地图

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `map.read` | Read | 无 | `loaded`、`loaded_tiles`、`left_hand_traffic`、`name`、`filename`、`friendly_name`、`description`、`max_speed`、`year_start`、`year_end` | |

<a id="camera"></a>
### 摄像机

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | 无 | `family`（0 司机、1 乘客、2 外部、3 地图）、`name`、`field_of_view`、`normal_field_of_view`、`distance` | |
| `camera.set` | Write | 可选 `family`（0..3）、`field_of_view`（10..170）；至少一个 | `camera.read` 的键 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | 必需 `family`（0..3）；可选 `preset`（0..255，仅用于 family 0 或 1） | `locked`（`true`）、`family`、`preset`、`head_look`（`preserved`）、`field_of_view`（`preserved`） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`。该策略每 100 ms 重新应用一次，直至 `camera.unlock`；重新应用失败时，每种不同的错误发出一次 `camera.lock.degraded` 事件。 |
| `camera.unlock` | Action | 无 | `locked`（`false`） | |

<a id="road-vehicles-and-player"></a>
### 道路车辆与玩家

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | 无 | `count`、`ai_collection`、`player_index`（OMSI 原始的玩家车辆索引，按读取值报告；要判断是否存在玩家车辆，请使用 `player-vehicle.read` 的 `present`） | |
| `road-vehicles.list` | Read | 无 | `count`、`vehicle.<n>.handle`（`rv-NNNNNN`） | |
| `road-vehicle.read` | Read | 必需 `handle` | `handle`、`runtime_index`、`tile`、`marked_for_killing`、`traffic_type`、`position_x`、`position_y`、`position_z`、`rotation_x`、`rotation_y`、`rotation_z`、`rotation_w`、`steering`、`tacho`、`ground_speed`、`kilometres`、`throttle`、`brake`、`clutch`、`ai_enabled`、`ai_mode`、`ai_light`、`ai_interior_light`、`ai_indicator_left`、`ai_indicator_right`、`ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | 无 | `present`（OMSI 没有玩家车辆时为 `false`）；为 `true` 时：`player_index` 加上 `road-vehicle.read` 的全部键 | |
| `road-vehicles.spawn` | Action | 必需 `model`（规范形式 `Vehicles\...\*.bus`，最多 240 个字符，不含 `..`，必须存在于安装目录之下） | `bus`、`created_handle`、`before_count`、`after_count`、`delta_count`、`raw_native_return`、`identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`。不会分配 PlayerVehicle。耗时数秒；请使用 30 s 的客户端超时。 |
| `road-vehicles.place-random` | Action | 可选 `ai_type`（0..255，默认 0）、`group`（0..65535，默认 1）、`type`（-1..65535，默认 -1）、`scheduled`（0..1，默认 0）、`tour`（0..65535，默认 0）、`line`（0..65535，默认 0） | `raw_return`、`before_count`、`after_count`、`delta_count`、`identity_validation`（`native-placement-return-is-diagnostic-only`） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### 人物对象

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | 无 | `count` | |
| `humans.list` | Read | 无 | `count`、`human.<n>.handle`（`hb-NNNNNN`） | |
| `human.read` | Read | 必需 `handle` | `handle`、`runtime_index`、`human_index`、`tile`、`marked_for_killing`、`collision_type`、`position_x`、`position_y`、`position_z`、`target_x`、`target_y`、`target_z`、`target_station`、`pre_target_station`、`departure`、`enter_bus_at`、`seat_bus`、`seat_station`、`ticket_type`、`ticket_index`、`ticket_ready`、`state`、`speed`、`bus_index`、`station`、`ai_mode`、`ai_mode_ex`、`ai_sub_mode`、`collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### 时刻表、司机、车票

有界列表结果带有 `count`（OMSI 中的全部记录数）、`returned_count`（本次回复中的行数）和 `truncated`（有行被省略时为 `true`）。返回的行始终是前 `returned_count` 条记录，从 `0` 开始编号。行数上限为：tracks、trips、lines、rv-files、bus stops、station links、tours 和 profiles 为 128，tour entries 为 256，track entries 为 512；当按该上限允许的行数仍超出 64 KiB 邮箱时（例如 Berlin-Spandau 上的线路轨迹条目和班次条目），插件会丢弃末尾的行直至回复能够放入，并报告较小的 `returned_count` 及 `truncated=true`（文档审计 BUG-05；修复之前，这两个列表会以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失败）。`timetable.logs.read` 不是有界列表：它返回每一条日志条目，包含数百条记录的日志会超出邮箱，并以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失败。

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | 无 | `invalid`（`true`/`false`），以及记录数 `tracks`、`trips`、`bus_stops`、`station_links`、`lines`、`rv_files` | |
| `timetable.tracks.list` | Read | 无 | `count`、`returned_count`、`truncated`；`track.<n>.filename`、`track.<n>.path`、`track.<n>.entries`、`track.<n>.length` | |
| `timetable.trips.list` | Read | 无 | `count`、`returned_count`、`truncated`；`trip.<n>.filename`、`trip.<n>.chrono_origin`、`trip.<n>.target`、`trip.<n>.line`、`trip.<n>.track_index`、`trip.<n>.track_name`、`trip.<n>.bus_stops`、`trip.<n>.profiles`、`trip.<n>.train_reverse`、`trip.<n>.invalid` | |
| `timetable.lines.list` | Read | 无 | `count`、`returned_count`、`truncated`；`line.<n>.name`、`line.<n>.chrono_origin`、`line.<n>.priority`、`line.<n>.tours`、`line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | 无 | `count`、`returned_count`、`truncated`；`rv_file.<n>.line`、`rv_file.<n>.number_tours`、`rv_file.<n>.start_date_rel_2000`、`rv_file.<n>.end_date_rel_2000`、`rv_file.<n>.type_line_files`、`rv_file.<n>.type_line_probability`、`rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | 无 | `count`、`returned_count`、`truncated`；`track_entry.<n>.track_index`、`track_entry.<n>.id`、`track_entry.<n>.path_index_on_object`、`track_entry.<n>.path_tile`、`track_entry.<n>.path_index`、`track_entry.<n>.relative_distance`、`track_entry.<n>.distance`、`track_entry.<n>.valid`、`track_entry.<n>.path_order_check`、`track_entry.<n>.allowed_fstrn`、`track_entry.<n>.chrono_origin`、`track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | 无 | `count`、`returned_count`、`truncated`；`bus_stop.<n>.name`、`bus_stop.<n>.supplement`、`bus_stop.<n>.tile`、`bus_stop.<n>.id`、`bus_stop.<n>.parent_id`、`bus_stop.<n>.preset_alighting`、`bus_stop.<n>.index`、`bus_stop.<n>.starting_links`、`bus_stop.<n>.ending_links`、`bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | 无 | `count`、`returned_count`、`truncated`；`station_link.<n>.length`、`station_link.<n>.start_bus_stop_id`、`station_link.<n>.end_bus_stop_id`、`station_link.<n>.start_bus_stop`、`station_link.<n>.end_bus_stop`、`station_link.<n>.chrono_origin`、`station_link.<n>.valid`、`station_link.<n>.visible`、`station_link.<n>.track_entries`、`station_link.<n>.start_track_entry`、`station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | 无 | `count`、`returned_count`、`truncated`；`tour.<n>.line_index`、`tour.<n>.name`、`tour.<n>.ai_group`、`tour.<n>.ai_group_index`、`tour.<n>.ai_type`、`tour.<n>.completed_day`、`tour.<n>.entries`、`tour.<n>.has_normal_vehicle`、`tour.<n>.invalid`、`tour.<n>.vehicle_indices`、`tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | 无 | `count`、`returned_count`、`truncated`；`profile.<n>.trip_index`、`profile.<n>.name`、`profile.<n>.service_trip`、`profile.<n>.stop_times`、`profile.<n>.total_time`、`profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | 无 | `count`、`returned_count`、`truncated`；`tour_entry.<n>.line_index`、`tour_entry.<n>.tour_index`、`tour_entry.<n>.trip`、`tour_entry.<n>.trip_index`、`tour_entry.<n>.profile_index`、`tour_entry.<n>.start_time`、`tour_entry.<n>.end_time`、`tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | 无 | `count`；`log.<n>.bus_stop`、`log.<n>.estimated_arrival`、`log.<n>.estimated_departure`、`log.<n>.actual_arrival`、`log.<n>.actual_departure`、`log.<n>.arrival_ok`、`log.<n>.departure_ok`（全部条目；无上限） | 日志较长时为 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` |
| `drivers.read` | Read | 无 | `count`、`selected_index`；`driver.<n>.filename`、`driver.<n>.name`、`driver.<n>.gender`、`driver.<n>.bus_stops`、`driver.<n>.crashes`、`driver.<n>.passengers`、`driver.<n>.tickets`、`driver.<n>.cash` | |
| `tickets.read` | Read | 无 | `filename`、`voice_path`、`stamper_factor`、`buy_factor`、`chattiness`、`whinge_factor`、`count`；`ticket.<n>.name`、`ticket.<n>.display_name`、`ticket.<n>.value`、`ticket.<n>.maximum_stations`、`ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### 车辆脚本、常量、曲线、HOF（按句柄）

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | 必需 `handle` | `handle`、`count`、`returned_count`、`truncated`（上限 512）、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | 必需 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | 必需 `handle`、`name`、`value`（有限浮点数） | `handle`、`name`、`requested_value`、`value`（回读值） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | 必需 `handle` | `handle`、`count`、`returned_count`、`truncated`（上限 512）、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | 必需 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | 必需 `handle` | `handle`、`count`、`name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | 必需 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | 必需 `handle` | `handle`、`count`、`name.<n>` | |
| `vehicle.curve.evaluate` | Read | 必需 `handle`、`name`、`x`（有限浮点数；缺少 `x` 或 `x` 非数值时为 `OL_E_RUNTIME_ARGUMENT_REQUIRED`） | `handle`、`name`、`x`、`value`（线性插值） | `OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | 必需 `handle` | `handle`、`count`、`hof.<n>.name`、`hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D 操作通过原生桥在 OMSI 的主线程/渲染线程上执行。纹理句柄的格式为 `d3dtex-<sessionId N>-<16 hex digits>`。

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | 无 | `available`、`native_status`、`query_interface_hresult`、`cooperative_level_hresult`、`execution_thread_id`、`owned_device_references`、`state`（`NOT_READY`、`READY`、`LOST`、`RESETTING`、`STOPPING`、`STOPPED`）、`transition`、`generation`、`live_textures`、`reset_hook_installed`、`last_reset_thread_id`、`binding` | |
| `d3d.texture.create` | Action | 必需 `width`（1..4096）、`height`（1..4096）、`format`（`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）；可选 `levels`（0..16，默认 1） | `handle`、`state`（`LIVE`、`RELEASED`、`STALE`）、`device_state`、`generation`、`width`、`height`、`format`、`levels`、`level`、`level_width`、`level_height`、`hresult`、`execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`、`OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST`、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | 必需 `handle`；可选 `level`（0..15，默认 0） | `d3d.texture.create` 的 13 个键，对应所请求的 level | `OL_E_D3D_STALE_RESOURCE_HANDLE`、`OL_E_D3D_RESOURCE_RELEASED`，以及 create 的错误 |
| `d3d.texture.update` | Action | 必需 `handle`、`width`（1..4096）、`height`（1..4096）、`pixels_base64`（解码后最多 48 KiB）；可选 `level`（0..15，默认 0）、`x`（0..4095，默认 0）、`y`（0..4095，默认 0） | 更新之后的 `d3d.texture.create` 的 13 个键 | `OL_E_D3D_INVALID_PIXEL_BUFFER`，以及 describe 的错误 |
| `d3d.texture.release` | Action | 必需 `handle` | `d3d.texture.create` 的 13 个键，其中 `state` = `RELEASED` | 重复释放时为 `OL_E_D3D_RESOURCE_RELEASED`，`OL_E_D3D_STALE_RESOURCE_HANDLE` |

失败的 D3D 操作会返回 `detail` 和 `native_status` 值。`D3DRuntimeApi` 扩展方法（`GetD3DStatusAsync`、`CreateD3DTextureAsync`、`DescribeD3DTextureAsync`、`UpdateD3DTextureAsync`、`ReleaseD3DTextureAsync`）为 API 使用方封装了这些操作。

<a id="getcapabilitiesasync-names"></a>
## `GetCapabilitiesAsync` 名称

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` 返回一个由 `Capability(Name, Available, EvidenceState, Reason)` 记录组成的静态列表，描述宿主对该安装实例的看法。这些名称是一套独立于上文注册表 ID 的、粒度更粗的术语；注册表是操作的契约，能力列表则是面向集成方的机器可读摘要。二者的关系见最后一列。

| 名称 | 可用 | 证据 | 关系 |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | 取决于平台 | STATICALLY_VALIDATED | [兼容性](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | 运行时邮箱 |
| `runtime.time.read`、`runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`、`time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`、`runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`、`weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set`（被拒绝） |
| `runtime.camera.read`、`runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`、`camera.set` |
| `runtime.d3d.status`、`runtime.d3d.texture.create`、`runtime.d3d.texture.update`、`runtime.d3d.texture.describe`、`runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007。该列表是固定的自报清单：在运行时收尾轮次观察到 resetting/restored 状态转换（`D01`）之后，此条目并未更新；证据见[运行时验证状态](../status/runtime-validation-status.md) |
| `runtime.road-vehicles.read`、`runtime.road-vehicles.spawn`、`runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`、`runtime.timetable.read`、`runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`、`timetable.*` |
| `world.new-map`、`world.presented-entrypoint`、`world.saved-situation` | true | RUNTIME_VALIDATED | `session.start` 的世界模式 |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState`（`/last`） |
| `world.date.explicit`、`world.date.system`、`world.time.explicit`、`world.time.system` | false | STATICALLY_PARTIAL | `/date`、`/time`、`/year`；请求它们会使计划不可运行 |
| `weather.preset`、`weather.icao`、`weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`；请求它们会使计划不可运行 |
| `player-vehicle.model`、`player-vehicle.repaint`、`player-vehicle.hof`、`player-vehicle.fleet-number`、`player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` 及相关参数；请求它们会使计划不可运行 |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`、配置档 `settings`（RV-005 运行时） |
| `input.keyboard.patch`、`input.controller.active-ffscale` | true | STATICALLY_VALIDATED | 已有文档解析器；`InputSpec` 不会被会话应用（BI-005） |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`、`content.situations`、`content.vehicles`、`content.repaints`、`content.hofs`、`content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`、`/list` |

此外，规划器还会在 `SessionPlan.RequiredCapabilities` 和 `SessionPlan.UnsupportedRequestedFeatures` 中报告每个计划的能力（`runtime.current-windows-x64`、`transaction.exact-restore`、`omsi.profile.OMSI23004`、`world.new-map`、`world.presented-entrypoint`、`world.entrypoint-identity`、`world.saved-situation`、`boot.headless-start`、`internet-textures.disabled`、`world.explicit-date`、`world.explicit-time`、`world.explicit-year`、`weather`、`player-vehicle.*`、`input.keyboard`、`input.controller`、`content.*`）。

<a id="known-limitations"></a>
## 已知限制

- 句柄的作用域为会话；地址复用通过对象指纹检测（车辆为 VMT 加定义指针，人物对象为 VMT 加 human 索引），并报告为 `OL_E_RUNTIME_OBJECT_HANDLE_STALE`。残余盲区：在两次列表读取之间，于同一地址重新创建的同类、同定义对象与原对象无法区分。
- 结果受 64 KiB 邮箱限制；有界列表会被截断并带有 `truncated=true`；`timetable.logs.read` 不是有界列表，可能以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失败。
- 不支持跨图块的车辆重新定位，不支持字符串变量写入，不支持日历写入，不支持天气写入，不支持 headless 模式下的 PlayerVehicle 分配。见[已知限制](known-limitations.md)。
