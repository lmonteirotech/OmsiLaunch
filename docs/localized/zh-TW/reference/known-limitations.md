# 已知限制

<!-- l10n: source=reference/known-limitations.md -->
> 本頁為 OmsiLaunch 0.1.0-beta3 [英文原始頁面](../../../reference/known-limitations.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁根據程式碼，列出 OmsiLaunch 0.1.0-beta3 中所有屬於 `UNAVAILABLE`、`PARTIAL` 或已接受風險的項目，讓使用者與整合者不會建立在產品並未提供的行為之上。每一列說明該限制、其穩定性、存在原因，以及詳細說明所在的位置。英文文件為規範版本；`docs/localized/` 底下的在地化副本並未以相同程度維護，內容可能落後（請見最後一節）。

<a id="compatibility"></a>
## 相容性

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 僅支援 `Omsi23004_692EBFBF`（`692EBFBF...6243`）；Steam LAA 雜湊 `7DAB063D...D759` 列於允許清單中；其指紋與計畫已透過受控副本驗證，但遊戲進行需要真正的 Steam 安裝（該映像是受 DRM 保護的 Steam 執行檔） | Steam LAA 為 `PARTIAL` | [相容性](compatibility.md) |
| 僅支援 Windows 10+ x64；不支援 Windows 7/8、XP 或 ARM64 | `UNAVAILABLE` | [相容性](compatibility.md) |
| 除了控制器使用的 x64 runtime 之外，外掛程式還需要 **x86** .NET 6 runtime | | [相容性](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## 世界啟動與啟動選項

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| `LAST_MAP_STATE`（`/last`、`WorldMode.LastMapState`）未實作；絕不會以依時間戳記挑選的 `.osn` 作為替代 | `UNAVAILABLE`（BI-006） | 計畫診斷訊息 `OL_E_CAPABILITY_UNAVAILABLE` |
| 明確指定或系統的日期、時間與年份（`/date`、`/time`、`/year`、設定檔 `new.date`/`new.time`/`new.year`、`DateSpec`/`TimeSpec`/`YearSpec`）會攜帶於規格中，但會使計畫不可執行；外掛程式拒絕非 `Unset` 的模式 | `UNAVAILABLE`（`STATICALLY_PARTIAL`） | [工作階段設定檔](session-profiles.md)、[launchspec](launchspec.md) |
| 啟動時的天氣預設組合（preset）、ICAO 與即時實際天氣（`/weather*`、`new.weather`） | `UNAVAILABLE`（`STATICALLY_PARTIAL`、BI-003） | 同上 |
| 啟動時的玩家車輛型號、塗裝（repaint）、HOF、車隊編號、車牌號碼（`/vehicle` 系列、`PlayerVehicleSpec`）會對照內容目錄解析，但不會套用；要求這些項目會使計畫不可執行；具確定性的無頭 PlayerVehicle 指派屬於未來的擴充 | `UNAVAILABLE`（BI-007） | [功能](capabilities.md)中的 `player.assign-headless` |
| 依識別指定進入點（`/entrypoint:<identity>`）不會與 OMSI 所呈現的清單相對應；請使用 `/entrypoint-index` | `PARTIAL`（BI-001） | 計畫功能 `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| 鍵盤與控制器文件 overlay（暫時覆蓋）（`InputSpec`、`Environment.Keyboard`、`Environment.Controllers`）會被剖析，但工作階段絕不會套用 | `UNAVAILABLE`（BI-005） | `input.*` 功能 |
| `LaunchBehaviorSpec.RestoreConfiguration` 與 `InstallationSpec.ExpectedExecutableSha256` 已宣告但從未被讀取 | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds`（`/shutdown-timeout`、設定檔 `shutdown-timeout`）會被接受並攜帶，但監督程式不會使用 | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [工作階段生命週期](../concepts/session-lifecycle.md) |
| `/quiet` 與 `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| 診斷旗標（`/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process`、`/trace-plugin`、`/trace-native`）會填入 `DiagnosticsSpec`；可見的效果僅限於 `.omsilaunch\diagnostics` 底下的主機追蹤記錄 | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`、`/runtime-write-batch`、`/d3d-batch` 是驗證測試工具 | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## 工作階段結束與處理程序控制

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 工作階段停止是強制終止：`session.stop`、系統匣的「End session」、Ctrl+C 與 `CloseAsync` 最終都會呼叫 `TerminateProcess`。OMSI 的關閉程序不會執行，OMSI 在結束時不會重寫 `options.cfg` 或其記錄檔，任何未儲存的 OMSI 狀態都會遺失。這是刻意的設計：可避免 OMSI 覆寫已還原的檔案。 | 依設計 | [工作階段生命週期](../concepts/session-lifecycle.md) |
| 具逾時與終止備援機制的協同式 WM_CLOSE 關閉未實作 | `UNAVAILABLE`（產品決策，S-11；在執行階段收尾回合中，OMSI 忽略了送往其主視窗的 `WM_CLOSE`） | [執行階段驗證狀態](../status/runtime-validation-status.md) |
| 主控台關閉或登出時，擁有者有 4 s 的時間上限來停止並還原；未完成的部分會在下一次啟動時由日誌復原 | 主控台關閉已於執行階段驗證；登出未經實測 | [交易與復原](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## 交易、復原與租約

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 安裝租約是一個 `Local\` 號誌：**每個登入工作階段**中每個安裝只能有一個擁有者；不跨使用者強制執行；在其他處理程序持有 handle（控制代碼）時不會釋放；任何同一使用者的處理程序都能持有該名稱 | 已接受的風險（S-18） | [交易與復原](../concepts/transactions-and-recovery.md) |
| 當日誌記錄的 OMSI 處理程序（或對於沒有 PID 的日誌，該根目錄中的任何 `Omsi.exe`）正在執行時，復原會被拒絕（`OL_E_INSTALLATION_BUSY`） | 依設計 | 同上 |
| 原本不存在、但內容在工作階段期間被變更的 overlay 路徑，會阻擋還原（`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`），直到經過檢查為止 | 依設計 | 同上 |
| 擁有權指紋出現之前的日誌，只能由規劃位元組完全相同的工作階段結清（`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`） | `PARTIAL` | 同上 |
| 只會還原工作階段擁有的路徑。OMSI 在工作階段期間的自身寫入（未有設定 overlay `options.cfg` 時的 `options.cfg` `[last_map]`、`Texture\standard.ipr`、快取、`laststn.osn`、駕駛員設定檔、記錄檔）會保留下來，與直接啟動 OMSI 後的情況相同 | 依設計 | [交易與復原](../concepts/transactions-and-recovery.md) |
| 當 `SuppressStaleClosecheckWarning` 為 true 時，工作階段開始前對過時 `closecheck` 的移除是永久的（會記錄，但不還原） | 依設計 | 同上 |

<a id="runtime-control"></a>
## 執行階段控制

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| `weather.set` 會被拒絕（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`）：OMSI 會在下一次天氣更新時覆寫兩個經設定檔分析的風力候選位置 | `UNAVAILABLE` | [功能](capabilities.md) |
| 日曆寫入（`SetActualDateTime`） | `UNAVAILABLE`（BI-002） | `calendar.set-actual-date-time` |
| 字串變數寫入、具名觸發器、音效觸發器（Delphi managed 字串的擁有權問題） | `UNAVAILABLE`（BI-004） | `scripts.string.read` 為唯讀 |
| 無車輛重新定位、無跨圖塊空間重新繫結、無 ODE 安全的變換權限；位置欄位為唯讀 | `UNAVAILABLE`（BI-008） | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` 需要 PlayerVehicle；無頭 NEW_MAP 啟動沒有 PlayerVehicle（已儲存情境會提供一個） | 依設計（BI-007） | RV-004 |
| 執行階段變更（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、生成、隨機放置、D3D 材質）不會記入日誌，也不會還原 | 依設計 | [執行階段控制](runtime-control.md) |
| Handle 指紋盲點：在兩次清單讀取之間，於相同位址重新建立的相同類別與定義的物件，不會被偵測為失效；自然移除的存續期間（RV-002）沒有安全的執行階段產生方式，仍以離線方式涵蓋 | `PARTIAL` | [執行階段控制](runtime-control.md) |
| 結果受限於 64 KiB 信箱（mailbox）：長清單會被截斷（`truncated=true`）；每次 `d3d.texture.update` 的像素酬載上限為 48 KiB | 依設計 | [功能](capabilities.md) |
| 單一飛行（single-flight）通道：每個工作階段一次只能有一個要求；槽位忙碌時為 `OL_E_RUNTIME_CHANNEL_BUSY`；要求 id 不得重複使用 | 依設計 | [執行階段控制](runtime-control.md) |
| 遙測是最新值槽位：比主機 100 ms 取樣更快的突發事件可能遺失中間事件（序號讓相同的連續事件得以區分；撕裂的取樣會被略過） | `PARTIAL` | [永久外掛程式](../concepts/permanent-plugin.md) |
| 已在執行階段觀察到 D3D 裝置重設（`resetting`、`restored`、generation 失效）；由於 OMSI 的裝置直接進入 `DEVICENOTRESET`，未產生獨立的 `lost` 轉換 | `PARTIAL`（RV-007） | [執行階段驗證狀態](../status/runtime-validation-status.md) |
| 有界清單結果（`timetable.*.list` 操作、`vehicle.variables.list`、`vehicle.string-variables.list`）最多回傳可容納於 64 KiB 執行階段槽位的資料列；其餘資料列會被省略，並以 `truncated=true` 及較小的 `returned_count` 表示（文件稽核 BUG-05）。此版本不提供分頁 | 依設計 | [功能](capabilities.md) |
| `timetable.logs.read`、`road-vehicles.list`、`humans.list`、`vehicle.constants.list` 與 `vehicle.curves.list` 不是有界的：大於槽位的結果會以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失敗（在已測試的地圖上，這些操作都未曾觀察到此情況） | `PARTIAL` | [功能](capabilities.md) |
| 自我回報的證據字串（`PublicCapabilityRegistry` `RuntimeValidation`、`GetCapabilitiesAsync` `EvidenceState`）在執行階段收尾回合之後並未更新：`camera.lock` 仍顯示 `STATICALLY_VALIDATED`，`runtime.d3d.lifecycle.reset` 仍顯示 `IMPLEMENTED_NOT_RUNTIME_VALIDATED`。以[執行階段驗證狀態](../status/runtime-validation-status.md)頁面為準 | 文件落後，並非行為差異 | [功能](capabilities.md) |
| 部分進階的地圖／圖塊／路徑／物件圖欄位未公開；執行階段讀取器是受設定檔限制的具型別快照，絕非任意記憶體存取 | 依設計 | [功能](capabilities.md) |
| 處理程序內記憶體讀取是對執行中 OMSI 的「先檢查後使用」；在檢查與讀取之間若 OMSI 同時發生變更，可能產生不一致的快照（`OL_E_RUNTIME_OPERATION_FAILED`） | 已接受的風險（S-33） | |

<a id="local-control-plane-and-trust-model"></a>
## 本機控制平面與信任模型

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 同一使用者信任模型：具名管道（named pipe）（`CurrentUserOnly`）、handoff／遙測／執行階段記憶體對應以及租約號誌，同一 Windows 使用者的任何處理程序都能存取。這類處理程序一旦讀取到 `session_id`，就能讀取狀態、停止工作階段或執行執行階段操作。 | 已接受的風險（S-06、S-30） | [本機控制](local-control.md) |
| 控制端點僅在擁有者處於 `Running` 時存在；用戶端在啟動期間及工作階段結束後會看到 `OL_E_NO_ACTIVE_SESSION`（結束代碼 4） | 依設計 | [本機控制](local-control.md) |
| 若另一個處理程序已擁有該管道名稱，擁有者會在沒有端點的情況下繼續執行（`ListenFault`），而第二次啟動可能誤報 `OL_E_SESSION_ALREADY_ACTIVE` | 已接受的風險 | [本機控制](local-control.md) |
| `.omsilaunch\` 繼承 OMSI 根目錄的 ACL；未套用明確的存取控制 | 已接受的風險（S-31） | [交易與復原](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## 診斷與輸出

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 診斷資料僅為本機檔案（`.omsilaunch\diagnostics`）；不會上傳任何內容，也沒有遠端回報 | 依設計 | [交易與復原](../concepts/transactions-and-recovery.md) |
| 保留最新的 50 個工作階段；新工作階段啟動時，會刪除較舊的、以工作階段為前綴的診斷資料 | 依設計 | 同上 |
| JSON 輸出與診斷資料包含安裝路徑（`RootPath`、素材目錄、`.itx` 路徑） | 依設計（本機資料） | |
| `OmsiLaunchW.exe` 的工作階段失敗對話方塊會以外掛程式的失敗酬載（例如 `{"name":"world.failed",...}`）作為訊息，而不是一個句子；`Code:` 那一行是正確的 | 外觀問題 | [Windows 系統匣](windows-tray.md) |
| 在任何 Direct3D 呼叫之前就被原生橋接層拒絕的 D3D 要求，會正確回報 `native_status`，但其 `detail` 文字卻顯示 `HRESULT 0x00000000` | 外觀問題 | [功能](capabilities.md) |
| 系統匣狀態視窗是開啟時所擷取的已規劃工作階段快照；它不會重新整理，也不顯示即時的 OMSI 數值 | 依設計 | [Windows 系統匣](windows-tray.md) |

<a id="documentation"></a>
## 文件

`docs/` 底下的英文頁面是此版本的規範文件。`docs/localized/<locale>/` 包含相同 `0.1.0-beta3` 頁面的翻譯（請見 [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)）；若翻譯與英文內容有出入，以英文內容與程式碼為準。其中列出的歷史與舊版頁面僅提供英文版。

相關頁面：[功能](capabilities.md)、[執行階段驗證狀態](../status/runtime-validation-status.md)、[錯誤](errors.md)。
