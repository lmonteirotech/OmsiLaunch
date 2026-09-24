# OmsiLaunch 0.1.0-beta3 術語表：zh-TW（繁體中文，台灣）

## 稱呼與語體

- 使用台灣軟體技術文件常見的正式、中性語體。需要稱呼讀者時用「您」（不用「你」），能以無主詞或「使用者」「整合者」表達時優先如此；語氣客觀、精確，不口語化。
- 一律使用台灣 IT 用語，不可由簡體中文逐字轉換：檔案（非文件）、處理程序（非進程）、執行緒（非線程）、預設（非默認）、設定（非設置）、使用者（非用戶）、組件（assembly，非程序集）、記憶體（非內存）、程式碼（非代碼）、資料（非數據）、目錄／資料夾、伺服器、介面、參數、回傳、物件、字串、型別、屬性、建構函式、簽章、支援、執行、呼叫、載入、儲存、日誌／記錄檔、螢幕、視窗、選單、按鈕、捷徑、網路、硬碟、資訊。

## 標點與排版

- 中文句子使用全形標點：，。、；：？！「」『』（）。中文括號內若全為英文、程式碼或數字，可使用半形 `( )`，但同一頁需一致；預設用全形（）。
- 引號用「」，巢狀用『』；不用 “ ” 或 " "（程式碼內除外）。
- 中英文、中文與數字之間不強制加空格；行內程式碼（反引號）前後不加額外空格亦可，但同一頁保持一致。建議：不加空格。
- 數字、單位與識別碼維持英文原樣（`64 KiB`、`250 ms`、`2 s`、`T01`、`BUG-05`），不改為全形。
- 列表與表格儲存格中的完整句子以「。」結尾；僅為名詞片語時不加句號，與英文原文一致。
- 粗體（`**...**`）保留在對應的中文詞語上。
- 狀態／分類詞（`STABLE_BETA`、`PARTIAL`、`UNAVAILABLE` 等）、錯誤碼、旗標、路徑一律照英文原樣，不翻譯。
- 英文 UI 字串（例如 `End session`、`Session is running`）是產品實際顯示的文字，保留反引號原樣，並在前後文以中文說明其意義；產品未提供繁體中文 UI，**絕不可**把中文譯名寫成像是產品顯示的文字。

## 術語表

| English | zh-TW | note |
| --- | --- | --- |
| session | 工作階段 | 台灣 Windows／微軟慣用譯法；不用「會話」。 |
| session owner / owner | 工作階段擁有者／擁有者 | 指擁有並監督 OMSI 與交易的 OmsiLaunch 處理程序。 |
| client (mode) | 用戶端（模式） | 不用「客戶端」。 |
| owner process | 擁有者處理程序 | |
| launch | 啟動 | 名詞可作「啟動」；launch plan 譯「啟動計畫」。 |
| plan (noun/verb) | 計畫（名詞）／規劃（動詞） | session plan =「工作階段計畫」；`/plan` 保留原樣。 |
| runnable / not runnable | 可執行／不可執行 | 「計畫不可執行」表示不會啟動 OMSI。 |
| start | 啟動 | 與 launch 同譯；語境需區分時 start 可作「開始」。 |
| stop | 停止 | |
| end session | 結束工作階段 | 產品 UI 字串 `End session` 保留英文。 |
| canonical stop | 標準停止路徑／標準停止 | 指擁有者唯一的正式停止流程。 |
| restore | 還原 | |
| recovery | 復原 | 與 restore（還原）區分。 |
| pending (journal) | 待處理（日誌） | 「a transaction is pending」=「有交易待處理」。 |
| journal | 日誌 | 指交易日誌 `journal.json`；與一般 log（記錄檔）區分。 |
| transaction | 交易 | 資料庫意義的交易。 |
| overlay | overlay（暫時覆蓋） | 保留英文 overlay；首次出現可加註「暫時覆蓋」。 |
| backup | 備份 | |
| snapshot | 快照 | 台灣常用；不保留英文。 |
| installation | 安裝（OMSI 安裝） | 指一個 OMSI 安裝目錄實體，可譯「OMSI 安裝」。 |
| installation root | 安裝根目錄 | |
| package | 套件 | |
| release package | 發行套件 | |
| manifest | 資訊清單（manifest） | 微軟台灣譯法；首次出現附英文。 |
| permanent plugin | 永久外掛程式 | plugin =「外掛程式」，短稱「外掛」亦可，但全頁一致用「外掛程式」。 |
| native bridge | 原生橋接層 | |
| runtime | runtime／執行階段 | 指 .NET runtime 時保留 runtime；指 OMSI 執行中的狀態或 runtime 控制時可譯「執行階段」。runtime-validated 見下。 |
| runtime operation | 執行階段操作 | operation id 保留原樣。 |
| runtime command | 執行階段命令 | |
| runtime slot / mailbox | 執行階段槽位／信箱（mailbox） | 64 KiB 共用記憶體區塊；mailbox 首次附英文。 |
| control plane | 控制平面 | local control plane =「本機控制平面」。 |
| local control | 本機控制 | |
| named pipe | 具名管道（named pipe） | 微軟台灣譯法。 |
| frame (protocol) | 訊框（frame） | 協定中的長度前綴訊息單位。 |
| envelope (JSON) | JSON 封套（envelope） | |
| handle | handle（控制代碼） | 保留英文 handle；首次可附「控制代碼」。 |
| stale handle | 失效的 handle | 物件已不存在的 handle。 |
| capability | 功能（capability） | 文件中指可查詢的功能項目；capability id 保留原樣。 |
| capability registry | 功能登錄（capability registry） | `PublicCapabilityRegistry` 保留原樣。 |
| bounded list | 有界清單 | BUG-05：不符 64 KiB 槽位時回傳可容納的最大前綴，`truncated=true`，屬成功回應而非失敗。 |
| truncated | 已截斷 | `truncated=true` 保留原樣。 |
| row | 資料列 | |
| evidence | 證據 | runtime evidence =「執行階段證據」。 |
| runtime-validated | 已於執行階段驗證 | `RUNTIME_VALIDATED` 保留原樣；「not runtime validated」=「未經執行階段驗證」。 |
| statically validated | 已靜態驗證 | `STATICALLY_VALIDATED` 保留原樣。 |
| offline test | 離線測試 | 「offline only」=「僅離線」。 |
| gate (documentation gate) | 關卡（文件檢查關卡） | 指 CI／文件檢查門檻。 |
| tray icon | 系統匣圖示 | |
| notification area | 通知區域 | Windows 台灣官方用語。 |
| status window | 狀態視窗 | |
| confirmation dialog | 確認對話方塊 | |
| message box | 訊息方塊 | |
| failure dialog | 失敗對話方塊 | |
| tooltip | 工具提示 | |
| context menu | 右鍵選單（內容功能表） | 建議用「右鍵選單」。 |
| Explorer restart | 檔案總管重新啟動 | Explorer（shell）= 檔案總管；`explorer.exe` 保留原樣。 |
| entry point | 進入點 | OMSI 地圖的起始位置；`/entrypoint` 保留原樣。 |
| new map | 新地圖 | 模式 `NEW_MAP` 保留原樣。 |
| saved situation | 已儲存情境 | `.osn` 檔；`SAVED_SITUATION` 保留原樣。 |
| map | 地圖 | |
| splash screen | 啟動畫面（splash） | UI 值 `Managed`／`Original OMSI` 保留英文。 |
| Internet Textures | Internet Textures（網路材質） | OMSI 功能名，保留英文並附中文。 |
| session profile | 工作階段設定檔 | |
| preset | 預設組合（preset） | 不可譯「預設」，以免與 default 混淆；天氣 preset 可作「天氣預設組合」。 |
| setting | 設定 | semantic setting =「語意設定」。 |
| player vehicle | 玩家車輛 | |
| road vehicle | 道路車輛 | `RoadVehicle` 保留原樣。 |
| human (pedestrian/passenger object) | 人物（行人／乘客物件） | `Human` 保留原樣。 |
| timetable | 時刻表 | |
| track entry | 路線軌跡項目（track entry） | 時刻表中的 track 項目；首次附英文。 |
| tour entry | 班次項目（tour entry） | 首次附英文。 |
| ticket | 車票 | |
| driver | 駕駛員 | OMSI 駕駛員設定檔；`Drivers\` 保留原樣。 |
| fleet number | 車隊編號 | |
| registration (plate) | 車牌號碼 | |
| repaint | 塗裝（repaint） | |
| spawn | 生成 | |
| camera lock | 攝影機鎖定 | `camera.lock` 保留原樣。 |
| device reset | 裝置重設 | D3D device reset；`runtime.d3d.lifecycle.reset` 保留原樣。 |
| render thread | 轉譯執行緒 | |
| texture | 材質 | D3D texture；技術語境亦可稱「紋理」，全頁統一用「材質」。 |
| exit code | 結束代碼 | |
| error code | 錯誤碼 | `OL_E_*` 保留原樣。 |
| diagnostic | 診斷訊息 | 計畫中的單筆診斷。 |
| diagnostics directory | 診斷目錄 | `.omsilaunch\diagnostics` 保留原樣。 |
| timeout | 逾時 | 「timeout budget」=「逾時預算／時間上限」。 |
| placeholder | 預留位置 | 例如 `<root>`。 |
| flag | 旗標 | CLI 旗標如 `/set`。 |
| route | 路由 | CLI／API route。 |
| command word | 命令字 | 例如 `session`、`events`。 |
| integrator | 整合者 | 以 API／CLI 整合 OmsiLaunch 的開發者。 |
| caller | 呼叫端 | |
| known limitation | 已知限制 | |
| accepted risk | 已接受的風險 | |
| stable beta | 穩定 Beta | 狀態詞 `STABLE_BETA` 保留原樣。 |
| experimental | 實驗性 | `EXPERIMENTAL` 保留原樣。 |
| partial | 部分支援／部分 | `PARTIAL` 保留原樣；Steam LAA 為 `PARTIAL`（未經執行階段驗證），不可譯成「不支援」或「損壞」。 |
| unavailable | 無法使用 | `UNAVAILABLE` 保留原樣；「not implemented」=「未實作」。 |
| deprecated/legacy | 已淘汰／舊版 | |
| not normative | 非規範性 | 英文文件為規範版本（normative）。 |
| source of truth | 唯一可信來源 | |

## 其他固定用法

- process = 處理程序；thread = 執行緒；assembly = 組件；hash = 雜湊（hash 值可保留英文 hash）；build = 組建（建置）；pipe = 管道；API、CLI、DLL、D3D、PID、GUID、UTC 保留英文。
- restore（還原）與 recovery（復原）必須區分；「exact restore」=「精確還原」。
- DOC-01：OmsiLaunch 只還原交易中由它擁有的檔案，不承諾還原 OMSI 本身的正常寫入（如 `options.cfg` 的 `[last_map]`、快取、記錄檔）。
- RV-002：「沒有安全的執行階段產生方式，以離線測試涵蓋」，不可譯為「已修正」或「缺陷」。
