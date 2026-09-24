# LaunchSpec 參考

<!-- l10n: source=reference/launchspec.md -->
> 本頁為 OmsiLaunch 0.1.0-beta3 [英文原始頁面](../../../reference/launchspec.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁是 `LaunchSpec` 的規範性參考。`LaunchSpec` 是描述單一 OmsiLaunch 工作階段的請求記錄，本頁涵蓋：其 C# 結構（`OmsiLaunch.Api`）、由 CLI 載入的 JSON 檔案形式（`/spec:<path>`、`tools/OmsiLaunch.Cli/LaunchSpecJson.cs`）、每個屬性的型別、預設值、驗證規則與目前效果、使計畫不可執行的驗證規則，以及 CLI 旗標、spec 檔案與工作階段設定檔之間的優先順序。本頁只記載目前程式碼實際的行為。

相關頁面：[公開 API](public-api.md)、[CLI 參考](cli.md)、[工作階段設定檔](session-profiles.md)、[錯誤碼](errors.md)、[工作階段生命週期](../concepts/session-lifecycle.md)、[功能](capabilities.md)。

<a id="where-a-launchspec-comes-from"></a>
## LaunchSpec 的來源

| 來源 | 如何成為 `LaunchSpec` |
| --- | --- |
| API | 整合者建構此記錄並將其傳給 `PlanSessionAsync`。 |
| CLI 旗標 | `CliInput.BuildSpecAsync` 從內建預設值（NEW_MAP、所有項目皆未設定、`Behavior` 預設值）開始，再套用旗標。 |
| `/spec:<path>` JSON 檔案 | 由 `LaunchSpecJson.LoadAsync` 載入，接著作為種子，由 CLI 旗標覆寫（請見[優先順序](#precedence-cli-flags-vs-spec-file-vs-session-profile)）。 |
| 工作階段設定檔（`/predefined-profile:<id> /predefined-profile-index:<n>`） | `SessionProfileCompiler.Apply` 將設定檔的世界、設定、呈現、Internet Textures（網路材質）與行為寫入種子，並記錄 `SessionProfile` 中繼資料。 |

下方的[完整範例](#complete-example)已對照記錄結構驗證。最小範例的副本以 `examples/release-session.example.json` 隨附（在發行套件中為 `.omsilaunch\examples\release-session.example.json`）。

<a id="json-form"></a>
## JSON 形式

| 規則 | 詳細說明 |
| --- | --- |
| 序列化程式 | `System.Text.Json`，搭配 `PropertyNameCaseInsensitive = true`、`ReadCommentHandling = Skip`、`AllowTrailingCommas = true`；未註冊任何轉換器。 |
| 屬性名稱 | 即 C# 屬性名稱（`Installation`、`RootPath`……）。載入時比對不區分大小寫；CLI 以 PascalCase 寫出。 |
| 列舉 | 整數（沒有字串列舉轉換器）。`"Mode": 0` 有效；`"Mode": "NewMap"` 會被視為格式錯誤的 JSON 而遭拒絕。各值列於[列舉](#enumerations)。 |
| `OptionalValue<T>` | 一個物件 `{ "Presence": 0 | 1, "Value": <T or null> }`。`Presence` 0 = `Unset`（值會被忽略），1 = `Set`（值必須存在且不可為 null；值為 null 的 `Set` 不會被驗證，其行為等同無效值）。省略的 `OptionalValue` 成員即為 `Unset`。唯讀成員 `IsSet` 會出現在 CLI 寫出的輸出中，載入時接受但忽略。 |
| 選用記錄 | `Year`、`Weather`、`Input`、`Diagnostics`、`Presentation`、`InternetTextures`、`SessionProfile` 可為 `null` 或省略；`Effective*` 存取子會代入預設值。 |
| 必要記錄 | `Installation`、`World`、`Date`、`Time`、`Environment`（須含全部八個字典，空的請用 `{}`）、`Behavior` 必須是存在的物件。它們不會被驗證：若為 `null` 或缺少，稍後會以 null 參考失敗，CLI 會回報為 `OL_E_INTERNAL`（結束代碼 10）或 `OL_E_INVALID_ARGUMENT`（結束代碼 2）。 |
| 未知屬性 | 在繫結前即遭拒絕：`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`（路徑使用檔案中所寫的成員名稱）。字典內容（`Environment.*`）不會被當作屬性檢查。 |
| 根節點 | 必須是 JSON 物件：`OL_E_SPEC_INVALID`。最大巢狀深度為 32。 |
| 檔案大小 | 最多 1 MiB（1 048 576 位元組）：`OL_E_SPEC_TOO_LARGE`。檔案不存在：`OL_E_SPEC_NOT_FOUND`。 |
| 註解與結尾逗號 | 接受 `//` 與 `/* */` 註解，以及結尾逗號。 |
| 格式錯誤的 JSON | 剖析器例外不會被轉譯：CLI 回報 `OL_E_INTERNAL`，結束代碼為 10。 |
| 編碼 | UTF-8（讀取器可容許 BOM）。識別碼中的反斜線必須跳脫（`"maps\\Grundorf\\global.cfg"`）；地圖、情境、車輛與 HOF 識別碼可接受正斜線。 |

<a id="complete-example"></a>
## 完整範例

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

明確日期（在組建支援時）寫成 `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }`，時間則寫成 `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`。在此組建中，兩者都會使計畫不可執行（見下文）。

<a id="property-reference"></a>
## 屬性參考

「使用情形」欄說明目前程式碼如何處理該值。穩定性使用[公開 API 頁面](public-api.md#stability-vocabulary)的用語。

<a id="launchspec-root"></a>
### `LaunchSpec`（根）

| 屬性 | JSON 型別 | 必要 | 省略時的預設值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Installation` | `InstallationSpec` 物件 | 是 | 無 | 是 | `STABLE_BETA` |
| `World` | `WorldSpec` 物件 | 是 | 無 | 是 | `STABLE_BETA` |
| `Date` | `DateSpec` 物件 | 是 | 無 | 會驗證；`Unset` 以外的任何模式皆不可執行 | `PARTIAL` |
| `Time` | `TimeSpec` 物件 | 是 | 無 | 會驗證；`Unset` 以外的任何模式皆不可執行 | `PARTIAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | 否 | `Unset` | 為診斷訊息而解析；設定任何欄位皆不可執行 | `PARTIAL` |
| `Environment` | `EnvironmentSpec` 物件 | 是 | 無 | 是（語意 `options.cfg` overlay（暫時覆蓋）） | `STABLE_BETA` |
| `Behavior` | `LaunchBehaviorSpec` 物件 | 是 | 無 | 部分（見該記錄） | `STABLE_BETA` / `PARTIAL` |
| `Year` | `YearSpec` 物件或 null | 否 | `null` → `EffectiveYear` = 模式 `Unset` | `Unset` 以外的任何模式皆不可執行 | `PARTIAL` |
| `Weather` | `WeatherSpec` 物件或 null | 否 | `null` → `EffectiveWeather` = 模式 `Unset` | `Unset` 以外的任何模式皆不可執行 | `PARTIAL` |
| `Input` | `InputSpec` 物件或 null | 否 | `null` → `EffectiveInput` = 兩者皆未設定 | 設定任何文件皆不可執行 | `PARTIAL` |
| `Diagnostics` | `DiagnosticsSpec` 物件或 null | 否 | `null` → `EffectiveDiagnostics` = 預設值 | 僅攜帶 | `PARTIAL` |
| `Presentation` | `SessionPresentationSpec` 物件或 null | 否 | `null` → `EffectivePresentation` = 受控啟動畫面、無語言、無自訂目錄、顯示系統匣 | 是 | `STABLE_BETA` |
| `InternetTextures` | `InternetTexturesSpec` 物件或 null | 否 | `null` → `EffectiveInternetTextures` = `Native` | 是 | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | `SessionProfileMetadata` 物件或 null | 否 | `null` | 僅供來源追溯（`session_profile.selected` 計畫診斷訊息） | `STABLE_BETA` |

唯讀存取子（出現在 CLI JSON 輸出中，載入時忽略）：`EffectiveYear`、`EffectiveWeather`、`EffectiveInput`、`EffectiveDiagnostics`、`EffectivePresentation`、`EffectiveInternetTextures`。

### `InstallationSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | 字串 | 必要 | 包含 `Omsi.exe` 與 `plugins\` 的目錄。請見[路徑規則](#path-rules)。空白或僅含空白字元 → `OL_E_INSTALLATION_NOT_FOUND`。 | 是 | `STABLE_BETA` |
| `ExpectedExecutableSha256` | 字串或 null | `null` | 任何字串。 | 目前程式碼中沒有取用端：主機一律計算 `Omsi.exe` 的雜湊並與組建設定檔比對，從不與此值比對。 | `PARTIAL`（會攜帶，目前無效果） |

### `WorldSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` 整數 | 必要 | `0` `NewMap`、`1` `SavedSituation`、`2` `LastMapState`（`LastSituation` 是值同為 2 的已淘汰別名）。 | 是；`LastMapState` → `OL_E_CAPABILITY_UNAVAILABLE` | `NewMap`、`SavedSituation`：`STABLE_BETA`；`LastMapState`：`UNAVAILABLE` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | 用於 `NewMap`：必要，形式為 `maps\<dir>\global.cfg`（不區分大小寫、接受 `/`、不可含 `..`）且必須已安裝。用於 `SavedSituation` 時會被忽略（地圖由 `.osn` 提供）。 | 是（交接） | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | 用於 `SavedSituation`：必要，為已安裝的 `situations\...\<file>.osn` 識別碼（如 `DiscoverAsync(Situations)` / `/list:situations` 所回傳）。 | 是（交接） | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | 用於未設定 `EntrypointIdentity` 的 `NewMap`：必要，`>= 0`，為該地圖在 OMSI 所呈現之進入點清單中的索引。未設定時以 `-1` 傳給外掛程式。 | 是（交接） | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | 原始進入點標籤或探索識別碼。設定此值會使計畫不可執行（`world.entrypoint-identity`、`RUNTIME_PARTIAL`、`OL_E_CAPABILITY_UNAVAILABLE`）。 | 會攜帶 | `PARTIAL` |
| `Entrypoint` | `EntrypointSpec`（唯讀） | 計算得出 | 設定 `EntrypointIdentity` 時 `Mode` = `Identity`；否則設定索引時為 `PresentedIndex`；否則為 `Unset`。`PresentedIndex`、`Identity` 反映輸入值。 | 衍生 | `STABLE_BETA` |

<a id="datespec-timespec-yearspec"></a>
### `DateSpec`、`TimeSpec`、`YearSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` 整數 | 必要（`Year`：記錄為 null 時為 `0`） | `0` `Unset`、`1` `Explicit`、`2` `System`。 | `Explicit`/`System` → `unsupported` 項目（`world.explicit-date`、`world.explicit-time`、`world.explicit-year`、`STATICALLY_PARTIAL`）與 `OL_E_CAPABILITY_UNAVAILABLE`。這些模式也會被複製到啟動交接資料中，外掛程式在其不為 `Unset` 時會拒絕（由於計畫不可執行，實際上永遠不會走到這一步）。 | `PARTIAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`：`Year`、`Month` 1..12、`Day` 1..31；`SemanticTime`：`Hour` 0..23、`Minute` 0..59、`Second` 0..59。`Mode` 為 `Explicit` 時必須設定（否則為 `OL_E_DATE_TIME_APPLY_FAILED`），`Mode` 不為 `Explicit` 時必須未設定（`OL_E_INVALID_ARGUMENT`）。`YearSpec.Value` 不會被驗證。 | 僅驗證 | `PARTIAL` |

### `WeatherSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` 整數 | 記錄為 null 時為 `0` | `0` `Unset`、`1` `Preset`、`2` `Icao`、`3` `RealCurrent`。 | `Unset` 以外的任何模式 → `weather` 不支援項目與 `OL_E_CAPABILITY_UNAVAILABLE`。 | `PARTIAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | 預設組合（preset）名稱（不驗證）。 | 會攜帶 | `PARTIAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | ICAO 代碼（不驗證）。 | 會攜帶 | `PARTIAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec`（位於 `PlayerVehicle` 內）

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | 已安裝的 `Vehicles\...\<file>.bus` 識別碼，否則為 `OL_E_VEHICLE_NOT_FOUND`。 | 解析至 `ResolvedContent`；接著 `player-vehicle.model` 不支援 → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | `Model` 的塗裝（repaint）識別碼（`<cti>#item:<n>`），否則為 `OL_E_REPAINT_NOT_FOUND`；僅在設定 `Model` 時檢查。 | 同上 | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | 已安裝的 `Vehicles\...\<file>.hof`，否則為 `OL_E_HOF_NOT_FOUND`。 | 同上 | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | 任何字串。 | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | 任何字串。 | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool（唯讀） | 計算得出 | 設定 `Model` 時為 `true`。交接資料以 `PlayerVehicleEnabled` 攜帶的是 `PlayerVehicle.IsSet`。 | 衍生 | `PARTIAL` |

`Presence` 為 1 且所有欄位皆未設定的 `PlayerVehicle` 會被接受，且沒有任何效果。在此組建中，設定任何欄位都會使計畫不可執行（`STATICALLY_PARTIAL`）。

### `EnvironmentSpec`

| 屬性 | 型別 | 預設值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | 各為 `IReadOnlyDictionary<string, OptionalValue<string>>`，必要（空的時候為 `{}`） | 無 | 是 | `STABLE_BETA` |

這八個群組會被串接在一起；鍵放在哪個群組中並無影響。每個 `Presence` 為 1 的項目都是來自 `ConfigurationCatalog`（`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`）的語意設定；鍵決定目標檔案（目前所有鍵皆為 `options.cfg`）與 token。規劃時會檢查鍵是否存在（`OL_E_UNKNOWN_SETTING`）以及是否可寫入（`OL_E_SETTING_NOT_WRITABLE`）；值只在啟動時驗證（`OL_E_INVALID_SETTING_VALUE`，回報為帶有 `OL_E_START_SESSION` 的 `Failed` 工作階段）。鍵不區分大小寫。未設定的項目會被忽略。CLI 的 `/set:<key>=<value>` 旗標會寫入 `General`；工作階段設定檔的 `settings` 也會合併到 `General`。

| 鍵 | 值 | 備註 |
| --- | --- | --- |
| `general.language` | 字串 | `[language]` token |
| `general.radio` | 字串 | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | 存在型 token（`autoSave` 是 `noAutoSave` 的反向） |
| `graphics.screenRatio` | 字串 | |
| `graphics.maxFPS` | 整數 10..200 | |
| `graphics.tileDistance` | 整數 1..20 | |
| `graphics.maxObjectDistanceMeters` | 數值 20..5000 | |
| `graphics.minObjectScreenPercent` | 數值 0..10 | 除以 100 後儲存 |
| `graphics.minReflectionObjectScreenPercent` | 數值 0..50 | 除以 100 後儲存 |
| `graphics.maxObjectComplexity` | 整數 0..3 | |
| `graphics.maxMapComplexity` | 整數 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | 存在型 token |
| `graphics.stencilShadows` | `true` / `false` | 寫成 `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | 一個 `smokesystems` 區塊 |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | 存在型 token |
| `simulation.ticketSelling` | 整數 0..2 | |
| `simulation.maintenance` | 整數 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | 同時寫入兩個 OMSI token（`RUNTIME_PROVEN`） |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | 存在型 token |
| `traffic.randomVehicles` | 整數 0..1000 | 多行 `AIMaxCountRandom` 區塊的第 0 個分量（已於執行階段驗證，矩陣 RV-005） |
| `traffic.humans` | 整數 0..1000 | `AIMaxCountRandom` 的第 1 個分量 |
| `traffic.factorPercent` | 數值 1..300 | |
| `traffic.parkedVehiclesPercent` | 數值 0..100 | |
| `traffic.scheduledVehicles` | 數值 0..1000 | |
| `traffic.scheduledLinePriority` | 數值 1..4 | |
| `traffic.passengerFactorPercent` | 數值 0..200 | |
| `sound.stereo` | 數值 0..100 | |
| `sound.maxSimultaneousSounds` | 數值 5..1000 | |
| `sound.masterVolume` | 數值 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | 拒絕 | 已知但不可寫入 → `OL_E_SETTING_NOT_WRITABLE` |

修補後的檔案會保留其編碼（保留 Windows-1252 位元組；遵循帶 BOM 的 UTF-8/UTF-16）與行尾字元。

### `LaunchBehaviorSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | 任意 | 沒有取用端：工作階段擁有的檔案一律精確還原。 | `PARTIAL`（會攜帶，目前無效果） |
| `SuppressStaleClosecheckWarning` | bool | `true` | 任意 | `true`：工作階段開始前已存在的 `closecheck` 檔案會在啟動時被永久移除（診斷訊息 `closecheck.stale-removed` 附其 SHA-256；失敗時為 `OL_E_CLOSECHECK_REMOVE_FAILED`）。`false`：現有的 `closecheck` 保持不動，且不屬於工作階段刪除項目。OMSI 在工作階段期間寫入的 `closecheck` 一律在還原時移除。 | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600（否則 `StartSessionAsync` 會擲回 `ArgumentOutOfRangeException`；CLI `/startup-timeout` 強制 1..600；設定檔要求 > 0）。從監督程式啟動到 `Running` 的時間預算；逾時則工作階段以 `OL_E_STARTUP_TIMEOUT`（外掛程式已啟動）或 `OL_E_PLUGIN_NOT_LOADED` 失敗。 | 是 | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 任何整數（CLI `/shutdown-timeout` 1..600） | 沒有取用端：監督程式以 `TerminateProcess` 立即終止 OMSI；沒有協同式關閉等待。 | `PARTIAL`（會攜帶，目前無效果） |

### `InputSpec`

| 屬性 | 型別 | 預設值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | 設定時 → `input.keyboard` 不支援（`STATICALLY_PARTIAL`）與 `OL_E_CAPABILITY_UNAVAILABLE`。鍵盤 PATCH/REPLACE 執行尚未實作。 | `PARTIAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | 設定時 → `input.controller` 不支援與 `OL_E_CAPABILITY_UNAVAILABLE`。 | `PARTIAL` |

### `DiagnosticsSpec`

| 屬性 | 型別 | 預設值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | `src/` 中沒有取用端。主機追蹤記錄 `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` 一律會寫入。 | `PARTIAL`（會攜帶，目前無效果） |
| `Verbose` | bool | `false` | 沒有取用端。 | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | 沒有取用端。 | `PARTIAL` |
| `ProcessTrace` | bool | `false` | 沒有取用端。 | `PARTIAL` |
| `PluginTrace` | bool | `false` | 沒有取用端。 | `PARTIAL` |
| `NativeTrace` | bool | `false` | 沒有取用端。 | `PARTIAL` |

CLI 旗標 `/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process`、`/trace-plugin`、`/trace-native` 會填入這些布林值（`/logall` 會設定 `Verbose`、`ProcessTrace`、`PluginTrace`、`NativeTrace`）；它們會與 spec 中的值做 OR 運算。

### `SessionPresentationSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` 整數 | `1`（`Managed`） | `0` `Unset`（別名 `Native`）：不改動 OMSI 自己的啟動畫面檔案。`1` `Managed`：OmsiLaunch 在工作階段期間以 overlay 覆蓋 `GUI\NewSplashscreen_ENG.bmp` 與 `GUI\NewSplashscreen_<LANG>.bmp`（事後精確還原）。 | 是 | `STABLE_BETA`（矩陣 RV-006） |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`、`ENG`/`EN`、`DEU`/`DE`、`FRA`/`FR`（不區分大小寫）；其他任何值都會正規化為 `ENG`。未設定時，會讀取 `options.cfg` 的 `[language]` 值並以相同方式正規化。 | 是（僅受控啟動畫面） | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | 包含 `ENG.bmp` 與 `<LANG>.bmp`（640×480、24 位元 BMP）的目錄。請見[路徑規則](#path-rules)。目錄不存在：`OL_E_SPLASH_ASSET_DIRECTORY_MISSING`；檔案不存在：`OL_E_SPLASH_ASSET_MISSING`；格式錯誤：`OL_E_SPLASH_FORMAT_UNSUPPORTED`。未設定時使用 `<root>\.omsilaunch\assets\splash`（由套件初始化一次），否則使用套件隨附的 `assets\splash`。 | 是（僅受控啟動畫面） | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` 會隱藏 CLI 擁有者獨立的 Windows 系統匣指示器。 | 僅 CLI 擁有者；API 沒有系統匣。沒有對應的 CLI 旗標；只能來自 spec 檔案。 | `STABLE_BETA` |

### `InternetTexturesSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` 整數 | `0`（`Native`） | `0` `Native`：不做任何變更。`1` `Disabled`：外掛程式抑制 OMSI 處理程序內的下載器（遙測 `internet-textures.suppressed` / `internet-textures.suppression.failed`）。`2` `Override`：`.itx` 設定檔以 overlay 覆蓋為 `Texture\standard.itx`；其目標檔案與 `Texture\standard.ipr` 成為工作階段刪除項目。 | 是 | `Native`：`STABLE_BETA`；`Disabled`、`Override`：`EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | `Override` 時為必要（`OL_E_ITX_PROFILE_REQUIRED`）。一個由成對行組成的文字檔：先是絕對 `http`/`https` URL，接著是相對於安裝根目錄的目標路徑；該路徑必須包含 `Texture\` 元件、不是根路徑、不含 `..`、不以 `\` 開頭，且不經過任何 junction/symlink（`OL_E_ITX_PROFILE_INVALID`、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`）。請見[路徑規則](#path-rules)。 | 是 | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| 屬性 | 型別 | 使用情形 | 穩定性 |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | 字串／整數 | 記錄在計畫診斷訊息 `session_profile.selected`（`Data["session_profile.*"]`）中。除此之外不會被使用；通常由工作階段設定檔編譯器填入，而非手動填寫。 | `STABLE_BETA` |

<a id="enumerations"></a>
## 列舉

| 列舉 | 值（JSON 整數） |
| --- | --- |
| `WorldMode` | `NewMap` = 0、`SavedSituation` = 1、`LastMapState` = 2、`LastSituation` = 2（已淘汰的別名；它是 OMSI 原生的最後地圖狀態分支，絕不是最新的 `.osn`） |
| `DateTimeMode` | `Unset` = 0、`Explicit` = 1、`System` = 2 |
| `WeatherMode` | `Unset` = 0、`Preset` = 1、`Icao` = 2、`RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0、`Native` = 0（別名）、`Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0、`Disabled` = 1、`Override` = 2 |
| `Presence` | `Unset` = 0、`Set` = 1 |
| `EntrypointMode`（唯讀 `Entrypoint.Mode`） | `Unset` = 0、`PresentedIndex` = 1、`Identity` = 2 |

超出宣告範圍的整數會由序列化程式原樣儲存，其行為等同未知值（例如未知的 `WorldMode` 既不是 NEW_MAP 也不是 SAVED_SITUATION，會產生沒有世界功能的計畫；外掛程式會拒絕它，但 CLI 無論如何都會取代該模式，請見優先順序）。

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## 驗證規則與不可執行的診斷訊息

`PlanSessionAsync` 先執行 `LaunchValidation.Validate`，再執行 `SessionPlanner.PlanAsync`。若且唯若沒有任何診斷代碼以 `OL_E_` 開頭，計畫才可執行。完整清單如下：

| 診斷訊息 | 條件 | 來源 |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` 為空或僅含空白字元 | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` 但未設定值，或月／日超出範圍；`Time.Mode` = `Explicit` 但未設定值，或時／分／秒超出範圍 | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | 模式不是 `Explicit` 時卻設定了 `Date.Value` 或 `Time.Value` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` 且 `MapIdentity` 未設定或不是 `maps\...\global.cfg` 形式（驗證）；`NewMap` 且識別碼未安裝（規劃器） | 兩者 |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` 未設定 `EntrypointIdentity`，且 `PresentedEntrypointIndex` 未設定或為負數 | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`、地圖已安裝、沒有 `EntrypointIdentity`、`PresentedEntrypointIndex` 未設定（`world.presented-entrypoint` 無法使用） | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` 沒有 `SituationIdentity`（驗證），或識別碼未安裝（規劃器） | 兩者 |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`：`.osn` 內指定的地圖未安裝 | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | 不是在 x64 作業系統上以 x64 主機處理程序執行的 Windows 10 以上版本（`runtime.current-windows-x64`） | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | 根目錄不存在、設有唯讀屬性，或沒有 `plugins\` 子目錄（`transaction.exact-restore`） | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | `Omsi.exe` 不存在，或其大小／SHA-256 既不符合設定檔指紋（`692EBFBF...`，8 503 440 位元組），也不是允許清單中的雜湊（`omsi.profile.OMSI23004`） | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `World.Mode` = `LastMapState`；設定了 `EntrypointIdentity`；`Date`/`Time`/`Year` 模式不是 `Unset`；`Weather` 模式不是 `Unset`；設定了任何 `PlayerVehicle` 欄位；設定了 `Input.KeyboardDocument` 或 `Input.ControllerDocument` | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` 未安裝（另外也會有 `OL_E_CAPABILITY_UNAVAILABLE`） | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | `Environment` 鍵不在目錄中／不可寫入 | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | 建立啟動畫面／ITX 計畫時擲回例外；訊息中帶有 `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`、`OL_E_ITX_PROFILE_REQUIRED`、`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID` 或 `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | 無法載入外掛程式封閉集合參考（`OmsiLaunchRuntimePaths`）或發行資訊清單（manifest）（訊息可能帶有 `OL_E_RELEASE_MANIFEST_INVALID`） | `OmsiLaunchService.PlanSessionAsync` |

規劃時不會驗證（在啟動時以帶有 `OL_E_START_SESSION` 的 `Failed` 工作階段失敗）：設定值（`OL_E_INVALID_SETTING_VALUE`）、永久外掛程式完整性（`OL_E_PERMANENT_PLUGIN_*`）、租用可用性（`OL_E_INSTALLATION_BUSY`）、`StartupTimeoutSeconds` 範圍（由 `StartSessionAsync` 擲回）。

資訊性計畫診斷訊息：`plugin.integrity.reference`（訊息為 `manifest` 或 `self`）、`session_profile.selected`。

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## 優先順序：CLI 旗標、spec 檔案與工作階段設定檔

`CliInput.BuildSpecAsync`（`tools/OmsiLaunch.Cli/Program.cs`）依下列順序建立有效的 spec：

1. 種子 = 內建預設值；若有指定 `/spec` 檔案則為該檔案。
2. 安裝根目錄 = 若有指定明確的安裝引數則使用之，否則使用種子的 `RootPath`；接著 `.`／空白 → 可執行檔所在目錄，再套用 `Path.GetFullPath`。明確的安裝引數一律優先於 spec 的 `RootPath`。
3. 工作階段設定檔（`/predefined-profile` + `/predefined-profile-index`）：觸及設定檔所擁有欄位的明確 CLI 引數會以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 拒絕（世界欄位僅限 NEW_MAP 模式；預設組合中存在的 `/set` 鍵；預設組合含 `presentation` 時的啟動畫面旗標；含 `internet-textures` 時的 Internet Textures 旗標；含 `behavior` 時的逾時旗標）。種子的 `World` 會被全新的一個取代（只保留 CLI 世界模式），接著套用設定檔的 `new:` 區塊（僅 NEW_MAP）、`settings`（併入 `General`）、`presentation`、`internet-textures`、`behavior` 以及 `SessionProfile` 中繼資料。`compatibility.maps` 會對 NEW_MAP 與 SAVED_SITUATION 強制執行。
4. 世界：CLI 世界模式一律優先（`/new` 為預設、`/saved:<osn>`、`/last`）；spec 檔案的 `World.Mode` 會被取代。若要從 spec 執行已儲存情境，請傳入 `/saved:`。`/map` 與 `/entrypoint`/`/entrypoint-index` 會覆寫種子；CLI 的 `/entrypoint` 識別碼會清除索引；`/saved` 搭配 `/map` 或進入點旗標為 `OL_E_INVALID_ARGUMENT`。
5. 有指定 `/date`、`/time`、`/year`、`/weather*` 時會覆寫種子（`system` 選取 `DateTimeMode.System`）。
6. `/no-vehicle` 會清除 `PlayerVehicle`；個別的 `/vehicle`、`/repaint`、`/hof`、`/fleet`、`/registration` 會覆寫種子玩家車輛的個別欄位。
7. `/set:<key>=<value>` 項目會加入 `Environment.General`（會檢查鍵，不檢查值）；其他七個群組原封不動地來自種子。
8. `/startup-timeout` 與 `/shutdown-timeout` 只在有指定時覆寫種子；否則依序套用 spec、設定檔，最後是預設值 180 s / 30 s。`ShutdownTimeoutSeconds` 對監督程式而言為 `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`。
9. 有指定 `/splash`、`/splash-language`、`/splash-assets`、`/internet-textures`、`/internet-textures-profile` 時會覆寫種子；`SuppressTrayIcon` 只來自種子。
10. 診斷旗標會與種子做 OR 運算。

結果：明確的 CLI 旗標 > 工作階段設定檔 > spec 檔案 > 內建預設值；但與設定檔所擁有欄位衝突的 CLI 旗標會被視為錯誤，而不是覆寫。

<a id="path-rules"></a>
## 路徑規則

| 路徑 | API 行為 | CLI 行為 |
| --- | --- | --- |
| `Installation.RootPath` | 依原樣使用：在檔案操作中，相對路徑會以處理程序的工作目錄為基準解析。請傳入絕對路徑。租用、日誌與內容目錄會以 `Path.GetFullPath` 將其正規化。 | `.` 或空白 = 包含 `OmsiLaunch.exe` 的目錄，絕不是呼叫端的工作資料夾；明確的安裝引數優先於 spec；結果會轉為絕對路徑。 |
| `Presentation.CustomAssetDirectory` | 絕對路徑，或相對於 `Installation.RootPath`。必須存在。 | 相同（`/splash-assets`）。工作階段設定檔的 `assets` 路徑受限於設定檔套件內，並以絕對路徑儲存。 |
| `InternetTextures.OverrideProfilePath` | 以 `Path.GetFullPath` 解析，亦即相對於處理程序的工作目錄，而非安裝根目錄。必須存在。 | 相同（`/internet-textures-profile`）。工作階段設定檔的 `profile` 路徑受限於套件內，並以絕對路徑儲存。 |
| ITX 目標行 | 相對於安裝根目錄；必須包含 `Texture\` 元件；不可為根路徑、不可含 `..`、不可以 `\` 開頭、不可含 junction/symlink 元件。 | 相同。 |
| 內容識別碼（`MapIdentity`、`SituationIdentity`、`PlayerVehicle.*`） | 相對於安裝、不區分大小寫、接受 `/`；絕不是絕對路徑。 | 相同。 |

<a id="carried-but-not-applied"></a>
## 會攜帶但不套用

| 欄位 | 目前效果 | 穩定性 |
| --- | --- | --- |
| `Installation.ExpectedExecutableSha256` | 無（主機以組建設定檔比對 `Omsi.exe` 的雜湊） | `PARTIAL` |
| `Behavior.RestoreConfiguration` | 無（還原一律會執行） | `PARTIAL` |
| `Behavior.ShutdownTimeoutSeconds` | 無（強制終止；`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`） | `PARTIAL` |
| `Diagnostics.*` | 無（主機追蹤記錄一律寫入） | `PARTIAL` |
| `Input.KeyboardDocument`, `Input.ControllerDocument` | 設定時計畫不可執行 | `PARTIAL` |
| `Date`, `Time`, `Year`（模式不是 `Unset`） | 計畫不可執行（`STATICALLY_PARTIAL`） | `PARTIAL` |
| `Weather`（模式不是 `Unset`） | 計畫不可執行（`STATICALLY_PARTIAL`） | `PARTIAL` |
| `PlayerVehicle.*`（設定任何欄位） | 為診斷訊息解析內容，計畫不可執行（`STATICALLY_PARTIAL`） | `PARTIAL` |
| `World.EntrypointIdentity` | 計畫不可執行（`RUNTIME_PARTIAL`） | `PARTIAL` |
| `World.Mode` = `LastMapState` / `LastSituation` | 計畫不可執行（`UNSUPPORTED_FOR_CURRENT_PROFILE`） | `UNAVAILABLE` |
| `SessionProfile` | 僅供來源追溯的診斷訊息 | `STABLE_BETA` |
