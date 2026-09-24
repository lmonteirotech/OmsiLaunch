# LaunchSpec 参考

<!-- l10n: source=reference/launchspec.md -->
> 本页是 OmsiLaunch 0.1.0-beta3 [英文原始页面](../../../reference/launchspec.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页是 `LaunchSpec` 的规范性参考。`LaunchSpec` 是描述单个 OmsiLaunch 会话的请求记录。本页说明：它的 C# 结构（`OmsiLaunch.Api`）；CLI 加载的 JSON 文件形式（`/spec:<path>`，`tools/OmsiLaunch.Cli/LaunchSpecJson.cs`）；每个属性的类型、默认值、校验规则和当前效果；使计划不可运行的校验规则；以及 CLI 参数、spec 文件和会话配置档之间的优先级。本页只记录当前代码实际执行的行为。

相关页面：[公共 API](public-api.md)、[CLI 参考](cli.md)、[会话配置档](session-profiles.md)、[错误码](errors.md)、[会话生命周期](../concepts/session-lifecycle.md)、[能力](capabilities.md)。

<a id="where-a-launchspec-comes-from"></a>
## LaunchSpec 的来源

| 来源 | 如何成为 `LaunchSpec` |
| --- | --- |
| API | 集成方构造该记录并将其传给 `PlanSessionAsync`。 |
| CLI 参数 | `CliInput.BuildSpecAsync` 从内置默认值（NEW_MAP、所有项均未设置、`Behavior` 默认值）开始，再应用各参数。 |
| `/spec:<path>` JSON 文件 | 由 `LaunchSpecJson.LoadAsync` 加载，然后作为种子，由 CLI 参数覆盖（参见[优先级](#precedence-cli-flags-vs-spec-file-vs-session-profile)）。 |
| 会话配置档（`/predefined-profile:<id> /predefined-profile-index:<n>`） | `SessionProfileCompiler.Apply` 将配置档的世界、设置项、呈现、网络纹理和行为写入种子，并记录 `SessionProfile` 元数据。 |

下文的[完整示例](#complete-example)已针对记录结构进行核对。最小示例的副本以 `examples/release-session.example.json` 提供（在发行包中为 `.omsilaunch\examples\release-session.example.json`）。

<a id="json-form"></a>
## JSON 形式

| 规则 | 详情 |
| --- | --- |
| 序列化器 | `System.Text.Json`，使用 `PropertyNameCaseInsensitive = true`、`ReadCommentHandling = Skip`、`AllowTrailingCommas = true`；未注册任何转换器。 |
| 属性名 | 即 C# 属性名（`Installation`、`RootPath` 等）。加载时匹配不区分大小写；CLI 写出时使用 PascalCase。 |
| 枚举 | 整数（没有字符串枚举转换器）。`"Mode": 0` 有效；`"Mode": "NewMap"` 会被当作格式错误的 JSON 拒绝。取值列于[枚举](#enumerations)。 |
| `OptionalValue<T>` | 一个对象 `{ "Presence": 0 | 1, "Value": <T or null> }`。`Presence` 为 0 表示 `Unset`（值被忽略），为 1 表示 `Set`（值必须存在且非 null；`Set` 而值为 null 的情况不会被校验，其行为等同于无效值）。省略的 `OptionalValue` 成员即为 `Unset`。只读成员 `IsSet` 会出现在 CLI 写出的输出中，加载时被接受并忽略。 |
| 可选记录 | `Year`、`Weather`、`Input`、`Diagnostics`、`Presentation`、`InternetTextures`、`SessionProfile` 可以为 `null` 或省略；`Effective*` 访问器会以默认值替代。 |
| 必需记录 | `Installation`、`World`、`Date`、`Time`、`Environment`（须包含全部八个字典，空时使用 `{}`）、`Behavior` 必须是存在的对象。它们不会被校验：为 `null` 或缺失时，会在后续因空引用而失败，CLI 将其报告为 `OL_E_INTERNAL`（exit 10）或 `OL_E_INVALID_ARGUMENT`（exit 2）。 |
| 未知属性 | 在绑定之前即被拒绝：`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`（路径使用文件中所写的成员名）。字典内容（`Environment.*`）不作为属性检查。 |
| 根节点 | 必须是 JSON 对象，否则为 `OL_E_SPEC_INVALID`。最大嵌套深度为 32。 |
| 文件大小 | 最多 1 MiB（1 048 576 字节），否则为 `OL_E_SPEC_TOO_LARGE`。文件不存在：`OL_E_SPEC_NOT_FOUND`。 |
| 注释和尾随逗号 | 接受 `//` 和 `/* */` 注释以及尾随逗号。 |
| 格式错误的 JSON | 解析器异常不会被转换：CLI 报告 `OL_E_INTERNAL`，退出码为 10。 |
| 编码 | UTF-8（读取器可容忍 BOM）。标识中的反斜杠必须转义（`"maps\\Grundorf\\global.cfg"`）；地图、情景、车辆和 HOF 标识也接受正斜杠。 |

<a id="complete-example"></a>
## 完整示例

```jsonc
{
  // Comments and trailing commas are accepted. Enums are integers.
  "Installation": {
    "RootPath": ".",                          // "." = directory that contains OmsiLaunch.exe (CLI only)
    "ExpectedExecutableSha256": null          // carried, not consumed
  },
  "World": {
    "Mode": 0,                                // 0 NewMap, 1 SavedSituation, 2 LastMapState (unavailable)
    "MapIdentity": { "Presence": 1, "Value": "maps\\Grundorf\\global.cfg" },
    "SituationIdentity": { "Presence": 0, "Value": null },
    "PresentedEntrypointIndex": { "Presence": 1, "Value": 1 },
    "EntrypointIdentity": { "Presence": 0, "Value": null }
  },
  "Date": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Time": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Year": null,
  "Weather": null,
  "PlayerVehicle": { "Presence": 0, "Value": null },
  "Environment": {
    "General": {
      "traffic.randomVehicles": { "Presence": 1, "Value": "150" },
      "graphics.maxFPS": { "Presence": 1, "Value": "60" }
    },
    "Advanced": {}, "Graphics": {}, "AdvancedGraphics": {},
    "Sound": {}, "AiPassengers": {}, "Keyboard": {}, "Controllers": {}
  },
  "Behavior": {
    "RestoreConfiguration": true,             // carried, restore always happens
    "SuppressStaleClosecheckWarning": true,
    "StartupTimeoutSeconds": 180,             // 1..600
    "ShutdownTimeoutSeconds": 30              // carried, not consumed
  },
  "Input": null,
  "Diagnostics": null,
  "Presentation": {
    "Splash": 1,                              // 0 Unset/Native (keep OMSI files), 1 Managed
    "Language": { "Presence": 0, "Value": null },
    "CustomAssetDirectory": { "Presence": 0, "Value": null },
    "SuppressTrayIcon": false
  },
  "InternetTextures": {
    "Mode": 0,                                // 0 Native, 1 Disabled, 2 Override
    "OverrideProfilePath": { "Presence": 0, "Value": null }
  },
  "SessionProfile": null
}
```

在版本（build）支持的情况下，显式日期写作 `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }`，时间写作 `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`。在当前版本（build）上，两者都会使计划不可运行（见下文）。

<a id="property-reference"></a>
## 属性参考

“使用情况”列说明当前代码如何处理该值。稳定性采用[公共 API 页面](public-api.md#stability-vocabulary)的术语。

<a id="launchspec-root"></a>
### `LaunchSpec`（根）

| 属性 | JSON 类型 | 必需 | 省略时的默认值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `Installation` | `InstallationSpec` 对象 | 是 | 无 | 是 | `STABLE_BETA` |
| `World` | `WorldSpec` 对象 | 是 | 无 | 是 | `STABLE_BETA` |
| `Date` | `DateSpec` 对象 | 是 | 无 | 会被校验；除 `Unset` 外的任何模式均不可运行 | `PARTIAL` |
| `Time` | `TimeSpec` 对象 | 是 | 无 | 会被校验；除 `Unset` 外的任何模式均不可运行 | `PARTIAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | 否 | `Unset` | 为诊断信息而解析；设置任何字段均不可运行 | `PARTIAL` |
| `Environment` | `EnvironmentSpec` 对象 | 是 | 无 | 是（语义化的 `options.cfg` 覆盖层） | `STABLE_BETA` |
| `Behavior` | `LaunchBehaviorSpec` 对象 | 是 | 无 | 部分使用（参见该记录） | `STABLE_BETA` / `PARTIAL` |
| `Year` | `YearSpec` 对象或 null | 否 | `null` → `EffectiveYear` = 模式 `Unset` | 除 `Unset` 外的任何模式均不可运行 | `PARTIAL` |
| `Weather` | `WeatherSpec` 对象或 null | 否 | `null` → `EffectiveWeather` = 模式 `Unset` | 除 `Unset` 外的任何模式均不可运行 | `PARTIAL` |
| `Input` | `InputSpec` 对象或 null | 否 | `null` → `EffectiveInput` = 两者均未设置 | 设置任何文档均不可运行 | `PARTIAL` |
| `Diagnostics` | `DiagnosticsSpec` 对象或 null | 否 | `null` → `EffectiveDiagnostics` = 默认值 | 仅携带 | `PARTIAL` |
| `Presentation` | `SessionPresentationSpec` 对象或 null | 否 | `null` → `EffectivePresentation` = 托管启动画面、无语言、无自定义目录、显示托盘 | 是 | `STABLE_BETA` |
| `InternetTextures` | `InternetTexturesSpec` 对象或 null | 否 | `null` → `EffectiveInternetTextures` = `Native` | 是 | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | `SessionProfileMetadata` 对象或 null | 否 | `null` | 仅用于来源记录（`session_profile.selected` 计划诊断信息） | `STABLE_BETA` |

只读访问器（出现在 CLI 的 JSON 输出中，加载时被忽略）：`EffectiveYear`、`EffectiveWeather`、`EffectiveInput`、`EffectiveDiagnostics`、`EffectivePresentation`、`EffectiveInternetTextures`。

### `InstallationSpec`

| 属性 | 类型 | 默认值 | 有效值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | 必需 | 包含 `Omsi.exe` 和 `plugins\` 的目录。参见[路径规则](#path-rules)。为空或仅含空白 → `OL_E_INSTALLATION_NOT_FOUND`。 | 是 | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string 或 null | `null` | 任意字符串。 | 当前代码中没有使用方：主机总是对 `Omsi.exe` 计算哈希并与构建配置比较，从不与此值比较。 | `PARTIAL`（仅携带，当前无效果） |

### `WorldSpec`

| 属性 | 类型 | 默认值 | 有效值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | 必需 | `0` `NewMap`、`1` `SavedSituation`、`2` `LastMapState`（`LastSituation` 是值同为 2 的已过时别名）。 | 是；`LastMapState` → `OL_E_CAPABILITY_UNAVAILABLE` | `NewMap`、`SavedSituation`：`STABLE_BETA`；`LastMapState`：`UNAVAILABLE` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | 对于 `NewMap`：必需，形式为 `maps\<dir>\global.cfg`（不区分大小写，接受 `/`，不得含 `..`），且必须已安装。对于 `SavedSituation` 被忽略（地图由 `.osn` 提供）。 | 是（交接） | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | 对于 `SavedSituation`：必需，为已安装的 `situations\...\<file>.osn` 标识（即 `DiscoverAsync(Situations)` / `/list:situations` 返回的标识）。 | 是（交接） | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | 对于未设置 `EntrypointIdentity` 的 `NewMap`：必需，`>= 0`，是该地图在 OMSI 所呈现的入口点列表中的索引。未设置时以 `-1` 发送给插件。 | 是（交接） | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | 原始入口点标签或发现标识。设置它会使计划不可运行（`world.entrypoint-identity`、`RUNTIME_PARTIAL`、`OL_E_CAPABILITY_UNAVAILABLE`）。 | 仅携带 | `PARTIAL` |
| `Entrypoint` | `EntrypointSpec`（只读） | 计算得出 | 设置了 `EntrypointIdentity` 时 `Mode` = `Identity`，否则设置了索引时为 `PresentedIndex`，否则为 `Unset`；`PresentedIndex`、`Identity` 与输入一致。 | 派生 | `STABLE_BETA` |

<a id="datespec-timespec-yearspec"></a>
### `DateSpec`、`TimeSpec`、`YearSpec`

| 属性 | 类型 | 默认值 | 有效值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | 必需（`Year`：记录为 null 时为 `0`） | `0` `Unset`、`1` `Explicit`、`2` `System`。 | `Explicit`/`System` → `unsupported` 条目（`world.explicit-date`、`world.explicit-time`、`world.explicit-year`，`STATICALLY_PARTIAL`）以及 `OL_E_CAPABILITY_UNAVAILABLE`。这些模式也会被复制到启动交接中，模式不为 `Unset` 时插件会拒绝（由于计划不可运行，实际不会执行到这一步）。 | `PARTIAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`：`Year`、`Month` 1..12、`Day` 1..31；`SemanticTime`：`Hour` 0..23、`Minute` 0..59、`Second` 0..59。`Mode` 为 `Explicit` 时必须设置（否则为 `OL_E_DATE_TIME_APPLY_FAILED`），`Mode` 不为 `Explicit` 时必须不设置（否则为 `OL_E_INVALID_ARGUMENT`）。`YearSpec.Value` 不会被校验。 | 仅校验 | `PARTIAL` |

### `WeatherSpec`

| 属性 | 类型 | 默认值 | 有效值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | 记录为 null 时为 `0` | `0` `Unset`、`1` `Preset`、`2` `Icao`、`3` `RealCurrent`。 | 除 `Unset` 外的任何模式 → `weather` 不支持条目以及 `OL_E_CAPABILITY_UNAVAILABLE`。 | `PARTIAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | 预设名称（不校验）。 | 仅携带 | `PARTIAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | ICAO 代码（不校验）。 | 仅携带 | `PARTIAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec`（位于 `PlayerVehicle` 内）

| 属性 | 类型 | 默认值 | 有效值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | 已安装的 `Vehicles\...\<file>.bus` 标识，否则为 `OL_E_VEHICLE_NOT_FOUND`。 | 解析到 `ResolvedContent` 中；随后 `player-vehicle.model` 不支持 → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | `Model` 的涂装标识（`<cti>#item:<n>`），否则为 `OL_E_REPAINT_NOT_FOUND`；仅在设置了 `Model` 时检查。 | 同上 | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | 已安装的 `Vehicles\...\<file>.hof`，否则为 `OL_E_HOF_NOT_FOUND`。 | 同上 | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | 任意字符串。 | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | 任意字符串。 | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool（只读） | 计算得出 | 设置了 `Model` 时为 `true`。交接中作为 `PlayerVehicleEnabled` 携带的是 `PlayerVehicle.IsSet`。 | 派生 | `PARTIAL` |

`Presence` 为 1 但所有字段均未设置的 `PlayerVehicle` 会被接受，且不产生任何效果。在当前版本（build）上，设置任何字段都会使计划不可运行（`STATICALLY_PARTIAL`）。

### `EnvironmentSpec`

| 属性 | 类型 | 默认值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- |
| `General`、`Advanced`、`Graphics`、`AdvancedGraphics`、`Sound`、`AiPassengers`、`Keyboard`、`Controllers` | 各为 `IReadOnlyDictionary<string, OptionalValue<string>>`，必需（为空时用 `{}`） | 无 | 是 | `STABLE_BETA` |

八个分组会被合并；键放在哪个分组中没有影响。每个 `Presence` 为 1 的条目都是 `ConfigurationCatalog`（`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`）中的一个语义设置项；键决定目标文件（当前所有键均为 `options.cfg`）和令牌。规划时会检查键是否存在（`OL_E_UNKNOWN_SETTING`）以及是否可写（`OL_E_SETTING_NOT_WRITABLE`）；值仅在启动时校验（`OL_E_INVALID_SETTING_VALUE`，报告为带 `OL_E_START_SESSION` 的 `Failed` 会话）。键不区分大小写。未设置的条目被忽略。CLI 参数 `/set:<key>=<value>` 写入 `General`；会话配置档的 `settings` 也合并到 `General` 中。

| 键 | 值 | 说明 |
| --- | --- | --- |
| `general.language` | string | `[language]` 令牌 |
| `general.radio` | string | |
| `general.alternateView`、`general.showOwnDriver`、`general.showErrorMessages`、`general.autoSave`、`general.currentTime`、`general.currentDate`、`general.currentYear` | `true` / `false` | 存在性令牌（`autoSave` 是 `noAutoSave` 的反向） |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | 整数 10..200 | |
| `graphics.tileDistance` | 整数 1..20 | |
| `graphics.maxObjectDistanceMeters` | 数值 20..5000 | |
| `graphics.minObjectScreenPercent` | 数值 0..10 | 存储时除以 100 |
| `graphics.minReflectionObjectScreenPercent` | 数值 0..50 | 存储时除以 100 |
| `graphics.maxObjectComplexity` | 整数 0..3 | |
| `graphics.maxMapComplexity` | 整数 0..2 | |
| `graphics.sunGlow`、`graphics.loadAllTiles`、`graphics.stencilBuffer`、`graphics.rainReflections`、`graphics.humansInRainReflections` | `true` / `false` | 存在性令牌 |
| `graphics.stencilShadows` | `true` / `false` | 写为 `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections`（bool,int>=0,bool,bool） | 一个 `smokesystems` 块 |
| `simulation.collision`、`simulation.collisionTerrain`、`simulation.collisionVehicles`、`simulation.collisionPedestrians`、`simulation.disableAutomaticScheduleAnalysisPopup`、`simulation.ticketInfo`、`simulation.automaticClutch` | `true` / `false` | 存在性令牌 |
| `simulation.ticketSelling` | 整数 0..2 | |
| `simulation.maintenance` | 整数 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | 同时对应两个 OMSI 令牌（`RUNTIME_PROVEN`） |
| `view.driverSmooth`、`view.driverMoving`、`controls.autoCenter`、`controls.reducedSteeringSpeed` | `true` / `false` | 存在性令牌 |
| `traffic.randomVehicles` | 整数 0..1000 | 多行 `AIMaxCountRandom` 块的第 0 个分量（经运行时验证，矩阵 RV-005） |
| `traffic.humans` | 整数 0..1000 | `AIMaxCountRandom` 的第 1 个分量 |
| `traffic.factorPercent` | 数值 1..300 | |
| `traffic.parkedVehiclesPercent` | 数值 0..100 | |
| `traffic.scheduledVehicles` | 数值 0..1000 | |
| `traffic.scheduledLinePriority` | 数值 1..4 | |
| `traffic.passengerFactorPercent` | 数值 0..200 | |
| `sound.stereo` | 数值 0..100 | |
| `sound.maxSimultaneousSounds` | 数值 5..1000 | |
| `sound.masterVolume` | 数值 0..1 | |
| `advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`、`graphics.texture`、`graphics.textureFilter` | 拒绝 | 已知但不可写 → `OL_E_SETTING_NOT_WRITABLE` |

被修补的文件保留其编码（保留 Windows-1252 字节；遵循带 BOM 的 UTF-8/UTF-16）和换行符。

### `LaunchBehaviorSpec`

| 属性 | 类型 | 默认值 | 有效值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | 任意 | 没有使用方：会话拥有的文件总是被精确还原。 | `PARTIAL`（仅携带，当前无效果） |
| `SuppressStaleClosecheckWarning` | bool | `true` | 任意 | `true`：会话开始前已存在的 `closecheck` 文件会在启动时被永久删除（诊断信息 `closecheck.stale-removed` 附带其 SHA-256；失败时为 `OL_E_CLOSECHECK_REMOVE_FAILED`）。`false`：已存在的 `closecheck` 保持不动，且不算作会话删除项。OMSI 在会话期间写入的 `closecheck` 总是在还原时被删除。 | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600（否则 `StartSessionAsync` 抛出 `ArgumentOutOfRangeException`；CLI `/startup-timeout` 强制 1..600；配置档要求 > 0）。这是从监管器启动到 `Running` 的时间预算；超时后会话以 `OL_E_STARTUP_TIMEOUT`（插件已启动）或 `OL_E_PLUGIN_NOT_LOADED` 失败。 | 是 | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 任意 int（CLI `/shutdown-timeout` 为 1..600） | 没有使用方：监管器立即用 `TerminateProcess` 终止 OMSI；不存在协作式关闭的等待。 | `PARTIAL`（仅携带，当前无效果） |

### `InputSpec`

| 属性 | 类型 | 默认值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | 设置后 → `input.keyboard` 不支持（`STATICALLY_PARTIAL`）以及 `OL_E_CAPABILITY_UNAVAILABLE`。键盘 PATCH/REPLACE 执行未实现。 | `PARTIAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | 设置后 → `input.controller` 不支持以及 `OL_E_CAPABILITY_UNAVAILABLE`。 | `PARTIAL` |

### `DiagnosticsSpec`

| 属性 | 类型 | 默认值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | `src/` 中没有使用方。主机跟踪 `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` 总是会被写入。 | `PARTIAL`（仅携带，当前无效果） |
| `Verbose` | bool | `false` | 没有使用方。 | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | 没有使用方。 | `PARTIAL` |
| `ProcessTrace` | bool | `false` | 没有使用方。 | `PARTIAL` |
| `PluginTrace` | bool | `false` | 没有使用方。 | `PARTIAL` |
| `NativeTrace` | bool | `false` | 没有使用方。 | `PARTIAL` |

CLI 参数 `/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process`、`/trace-plugin`、`/trace-native` 会填充这些布尔值（`/logall` 设置 `Verbose`、`ProcessTrace`、`PluginTrace`、`NativeTrace`）；它们与 spec 中的值做逻辑或（OR）运算。

### `SessionPresentationSpec`

| 属性 | 类型 | 默认值 | 有效值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1`（`Managed`） | `0` `Unset`（别名 `Native`）：OMSI 自己的启动画面文件保持不变。`1` `Managed`：OmsiLaunch 在会话期间以覆盖层替换 `GUI\NewSplashscreen_ENG.bmp` 和 `GUI\NewSplashscreen_<LANG>.bmp`（之后精确还原）。 | 是 | `STABLE_BETA`（矩阵 RV-006） |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`、`ENG`/`EN`、`DEU`/`DE`、`FRA`/`FR`（不区分大小写）；其他任何值都会规范化为 `ENG`。未设置时，读取 `options.cfg` 中的 `[language]` 值并以同样方式规范化。 | 是（仅托管启动画面） | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | 包含 `ENG.bmp` 和 `<LANG>.bmp`（640×480，24 位 BMP）的目录。参见[路径规则](#path-rules)。目录不存在：`OL_E_SPLASH_ASSET_DIRECTORY_MISSING`；文件不存在：`OL_E_SPLASH_ASSET_MISSING`；格式错误：`OL_E_SPLASH_FORMAT_UNSUPPORTED`。未设置时使用 `<root>\.omsilaunch\assets\splash`（从包中一次性初始化），否则使用包内的 `assets\splash`。 | 是（仅托管启动画面） | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` 会隐藏 CLI 所有者的独立 Windows 托盘指示器。 | 仅 CLI 所有者；API 没有托盘。没有对应的 CLI 参数，只能来自 spec 文件。 | `STABLE_BETA` |

### `InternetTexturesSpec`

| 属性 | 类型 | 默认值 | 有效值 | 使用情况 | 稳定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0`（`Native`） | `0` `Native`：不做任何更改。`1` `Disabled`：插件抑制 OMSI 进程内的下载器（遥测 `internet-textures.suppressed` / `internet-textures.suppression.failed`）。`2` `Override`：`.itx` 配置文件以覆盖层形式替换为 `Texture\standard.itx`；其目标文件和 `Texture\standard.ipr` 成为会话删除项。 | 是 | `Native`：`STABLE_BETA`；`Disabled`、`Override`：`EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | `Override` 时必需（`OL_E_ITX_PROFILE_REQUIRED`）。一个由成对行组成的文本文件：先是绝对的 `http`/`https` URL，然后是相对于安装根目录的目标路径；该路径必须包含 `Texture\` 组成部分、不是根路径、不含 `..`、不以 `\` 开头，且不经过任何 junction/symlink（`OL_E_ITX_PROFILE_INVALID`、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`）。参见[路径规则](#path-rules)。 | 是 | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| 属性 | 类型 | 使用情况 | 稳定性 |
| --- | --- | --- | --- |
| `Id`、`Name`、`Version`、`Author`、`PresetId`、`PresetIndex`、`PresetName`、`PackagePath` | 字符串 / int | 记录在计划诊断信息 `session_profile.selected` 中（`Data["session_profile.*"]`）。此外不被使用；通常由会话配置档编译器填写，而非手工填写。 | `STABLE_BETA` |

<a id="enumerations"></a>
## 枚举

| 枚举 | 取值（JSON 整数） |
| --- | --- |
| `WorldMode` | `NewMap` = 0、`SavedSituation` = 1、`LastMapState` = 2、`LastSituation` = 2（已过时的别名；它是 OMSI 原生的“上次地图状态”分支，绝不是最新的 `.osn`） |
| `DateTimeMode` | `Unset` = 0、`Explicit` = 1、`System` = 2 |
| `WeatherMode` | `Unset` = 0、`Preset` = 1、`Icao` = 2、`RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0、`Native` = 0（别名）、`Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0、`Disabled` = 1、`Override` = 2 |
| `Presence` | `Unset` = 0、`Set` = 1 |
| `EntrypointMode`（只读 `Entrypoint.Mode`） | `Unset` = 0、`PresentedIndex` = 1、`Identity` = 2 |

超出声明范围的整数会被序列化器原样存储，并表现为未知值（例如，未知的 `WorldMode` 既不是 NEW_MAP 也不是 SAVED_SITUATION，得到的计划没有世界能力；插件会拒绝它，但 CLI 无论如何都会替换该模式，参见优先级）。

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## 校验规则与不可运行诊断信息

`PlanSessionAsync` 先运行 `LaunchValidation.Validate`，再运行 `SessionPlanner.PlanAsync`。当且仅当没有任何诊断代码以 `OL_E_` 开头时，计划才可运行。完整列表：

| 诊断信息 | 条件 | 来源 |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` 为空或仅含空白 | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` 但未设置值，或月/日超出范围；`Time.Mode` = `Explicit` 但未设置值，或时/分/秒超出范围 | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | 模式不为 `Explicit` 时设置了 `Date.Value` 或 `Time.Value` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` 的 `MapIdentity` 未设置或不符合 `maps\...\global.cfg` 形式（校验）；`NewMap` 的标识未安装（规划器） | 两者 |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` 未设置 `EntrypointIdentity`，且 `PresentedEntrypointIndex` 未设置或为负数 | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`，地图已安装，没有 `EntrypointIdentity`，`PresentedEntrypointIndex` 未设置（`world.presented-entrypoint` 不可用） | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` 没有 `SituationIdentity`（校验），或其标识未安装（规划器） | 两者 |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`：`.osn` 中指定的地图未安装 | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | 不是 x64 操作系统上以 x64 主机进程运行的 Windows 10+（`runtime.current-windows-x64`） | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | 根目录不存在、设置了只读属性，或没有 `plugins\` 子目录（`transaction.exact-restore`） | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | `Omsi.exe` 不存在，或其大小/SHA-256 既不是配置指纹（`692EBFBF...`，8 503 440 字节），也不是允许列表中的哈希（`omsi.profile.OMSI23004`） | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `World.Mode` = `LastMapState`；设置了 `EntrypointIdentity`；`Date`/`Time`/`Year` 模式不为 `Unset`；`Weather` 模式不为 `Unset`；设置了任何 `PlayerVehicle` 字段；设置了 `Input.KeyboardDocument` 或 `Input.ControllerDocument` | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`、`OL_E_REPAINT_NOT_FOUND`、`OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` 未安装（在 `OL_E_CAPABILITY_UNAVAILABLE` 之外额外报告） | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE` | `Environment` 中的某个键不在目录中 / 不可写 | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | 构建启动画面/ITX 计划时抛出异常；消息中携带 `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`、`OL_E_ITX_PROFILE_REQUIRED`、`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID` 或 `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | 无法加载插件闭包引用（`OmsiLaunchRuntimePaths`）或发行清单（消息中可能携带 `OL_E_RELEASE_MANIFEST_INVALID`） | `OmsiLaunchService.PlanSessionAsync` |

规划时不校验（在启动时作为带 `OL_E_START_SESSION` 的 `Failed` 会话失败）：设置项的值（`OL_E_INVALID_SETTING_VALUE`）、永久插件完整性（`OL_E_PERMANENT_PLUGIN_*`）、租约可用性（`OL_E_INSTALLATION_BUSY`）、`StartupTimeoutSeconds` 范围（由 `StartSessionAsync` 抛出）。

信息性计划诊断信息：`plugin.integrity.reference`（消息为 `manifest` 或 `self`）、`session_profile.selected`。

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## 优先级：CLI 参数、spec 文件与会话配置档

`CliInput.BuildSpecAsync`（`tools/OmsiLaunch.Cli/Program.cs`）按以下顺序构建生效的 spec：

1. 种子 = 内置默认值；如果提供了 `/spec` 文件，则为该文件。
2. 安装根目录 = 若提供了显式安装参数则使用它，否则使用种子的 `RootPath`；然后 `.`/空值 → 可执行文件所在目录，再经 `Path.GetFullPath` 处理。显式安装参数总是优先于 spec 的 `RootPath`。
3. 会话配置档（`/predefined-profile` + `/predefined-profile-index`）：涉及配置档所拥有字段的显式 CLI 参数会以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 被拒绝（世界字段仅在 NEW_MAP 模式下；预设中存在的 `/set` 键；预设含 `presentation` 时的启动画面参数；预设含 `internet-textures` 时的网络纹理参数；预设含 `behavior` 时的超时参数）。种子的 `World` 被替换为一个全新的对象（只保留 CLI 世界模式），然后依次应用配置档的 `new:` 块（仅 NEW_MAP）、`settings`（写入 `General`）、`presentation`、`internet-textures`、`behavior` 以及 `SessionProfile` 元数据。`compatibility.maps` 对 NEW_MAP 和 SAVED_SITUATION 强制执行。
4. 世界：CLI 世界模式总是优先（默认 `/new`、`/saved:<osn>`、`/last`）；spec 文件的 `World.Mode` 会被替换。要从 spec 运行已保存情景，请传入 `/saved:`。`/map` 和 `/entrypoint`/`/entrypoint-index` 覆盖种子；CLI 的 `/entrypoint` 标识会清除索引；`/saved` 与 `/map` 或入口点参数同时使用时为 `OL_E_INVALID_ARGUMENT`。
5. 提供了 `/date`、`/time`、`/year`、`/weather*` 时，它们覆盖种子（`system` 选择 `DateTimeMode.System`）。
6. `/no-vehicle` 清除 `PlayerVehicle`；单独的 `/vehicle`、`/repaint`、`/hof`、`/fleet`、`/registration` 覆盖种子中玩家车辆的对应字段。
7. `/set:<key>=<value>` 条目被加入 `Environment.General`（检查键，不检查值）；其他七个分组原样来自种子。
8. `/startup-timeout` 和 `/shutdown-timeout` 仅在提供时覆盖种子；否则依次采用 spec、配置档，最后是默认值 180 s / 30 s。`ShutdownTimeoutSeconds` 对监管器而言是 `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`。
9. 提供了 `/splash`、`/splash-language`、`/splash-assets`、`/internet-textures`、`/internet-textures-profile` 时，它们覆盖种子；`SuppressTrayIcon` 仅来自种子。
10. 诊断参数与种子做逻辑或（OR）运算。

结果：显式 CLI 参数 > 会话配置档 > spec 文件 > 内置默认值；例外是与配置档所拥有字段冲突的 CLI 参数会导致错误，而不是覆盖。

<a id="path-rules"></a>
## 路径规则

| 路径 | API 行为 | CLI 行为 |
| --- | --- | --- |
| `Installation.RootPath` | 按原样使用：在文件操作中，相对路径以进程工作目录为基准解析。请传入绝对路径。租约、事务日志和内容目录会用 `Path.GetFullPath` 将其规范化。 | `.` 或空值 = 包含 `OmsiLaunch.exe` 的目录，绝不是调用方的工作目录；显式安装参数优先于 spec；结果会被转换为绝对路径。 |
| `Presentation.CustomAssetDirectory` | 绝对路径，或相对于 `Installation.RootPath` 的路径。必须存在。 | 相同（`/splash-assets`）。会话配置档中的 `assets` 路径被限制在配置档包内，并以绝对路径存储。 |
| `InternetTextures.OverrideProfilePath` | 用 `Path.GetFullPath` 解析，即相对于进程工作目录，而不是安装根目录。必须存在。 | 相同（`/internet-textures-profile`）。会话配置档中的 `profile` 路径被限制在包内，并以绝对路径存储。 |
| ITX 目标行 | 相对于安装根目录；必须包含 `Texture\` 组成部分；不得有根、不得含 `..`、不得以 `\` 开头、不得包含 junction/symlink 组成部分。 | 相同。 |
| 内容标识（`MapIdentity`、`SituationIdentity`、`PlayerVehicle.*`） | 相对于安装实例，不区分大小写，接受 `/`；绝不是绝对路径。 | 相同。 |

<a id="carried-but-not-applied"></a>
## 仅携带而不应用的字段

| 字段 | 当前效果 | 稳定性 |
| --- | --- | --- |
| `Installation.ExpectedExecutableSha256` | 无（主机依据构建配置对 `Omsi.exe` 计算哈希） | `PARTIAL` |
| `Behavior.RestoreConfiguration` | 无（还原总是执行） | `PARTIAL` |
| `Behavior.ShutdownTimeoutSeconds` | 无（强制终止；`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`） | `PARTIAL` |
| `Diagnostics.*` | 无（主机跟踪总是写入） | `PARTIAL` |
| `Input.KeyboardDocument`、`Input.ControllerDocument` | 设置后计划不可运行 | `PARTIAL` |
| `Date`、`Time`、`Year`（模式不为 `Unset`） | 计划不可运行（`STATICALLY_PARTIAL`） | `PARTIAL` |
| `Weather`（模式不为 `Unset`） | 计划不可运行（`STATICALLY_PARTIAL`） | `PARTIAL` |
| `PlayerVehicle.*`（设置了任何字段） | 为诊断信息解析内容，计划不可运行（`STATICALLY_PARTIAL`） | `PARTIAL` |
| `World.EntrypointIdentity` | 计划不可运行（`RUNTIME_PARTIAL`） | `PARTIAL` |
| `World.Mode` = `LastMapState` / `LastSituation` | 计划不可运行（`UNSUPPORTED_FOR_CURRENT_PROFILE`） | `UNAVAILABLE` |
| `SessionProfile` | 仅来源诊断信息 | `STABLE_BETA` |
