# 相容性

<!-- l10n: source=reference/compatibility.md -->
> 本頁為 OmsiLaunch 0.1.0-beta3 [英文原始頁面](../../../reference/compatibility.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

OmsiLaunch 透過修補單一確切執行檔組建內的已建檔位址來驅動 OMSI。本頁說明支援哪些 OMSI 組建、使用其他組建時會發生什麼事，以及主機與外掛程式的作業系統與 runtime 需求。來源：`src/OmsiLaunch.Builds.Omsi23004/Profile.cs`、`src/OmsiLaunch.Core/SessionPlanner.cs`、`src/OmsiLaunch.Process/RuntimePlatform.cs`、`src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs` 以及專案檔。

<a id="supported-omsi-builds"></a>
## 支援的 OMSI 組建

組建設定檔只有一個：`Omsi23004_692EBFBF`（系列 `OMSI_2_3_004_COMMON`）。它以確切的 SHA-256 接受兩個執行檔：

| 變體 | `Omsi.exe` SHA-256 | 大小 | PE 檔案版本／產品版本 | 狀態 |
| --- | --- | --- | --- | --- |
| 已建檔的執行檔（`ALTERNATE_LAA`） | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 位元組 | 2.2.032 / 2.3.004 | `STABLE_BETA`；矩陣中的每一項執行階段驗證都是在此檔案上執行 |
| Steam LAA（`STEAM_LAA`） | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | 不檢查 | | 因與已建檔的原生配置相同、僅執行檔標頭不同，而列入允許清單；**未經執行階段驗證**（`profiles` 回報 `runtime_validated=false`、`validation_status=pending_beta_field_validation`）。`PARTIAL`。 |

`OmsiLaunch.exe profiles` 會以 JSON 印出此表。接受與否不依據版本號碼：只有 SHA-256（以及主要執行檔的確切大小）才算數。不支援其他任何 OMSI 2 組建、任何經修補的執行檔，也不支援雜湊不同的 4 GB 修補版本。

<a id="what-happens-with-an-unknown-build"></a>
## 使用未知組建時會發生什麼事

| 階段 | 檢查 | 結果 |
| --- | --- | --- |
| 規劃（`PlanSessionAsync`、`/plan`、`/validate`） | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | 必要功能 `omsi.profile.OMSI23004` 為 `UNAVAILABLE`；診斷訊息 `OL_E_UNSUPPORTED_BUILD`；`SessionPlan.IsRunnable=false`。啟動時 CLI 結束代碼為 1，錯誤以例外形式逸出時則為 3（`UnsupportedProfile`）。 |
| 啟動（`StartSessionAsync`） | 重新規劃 spec 並重新計算 `Omsi.exe` 的雜湊 | 已不再可執行的計畫（例如執行檔在規劃後遭變更，或呼叫端修改了 `IsRunnable`）會以 `OL_E_PLAN_NOT_RUNNABLE` 拒絕；不會開啟交易，也不會啟動處理程序。 |
| 處理程序內（`PluginRuntime.Start`） | `NativeServices.ValidateBuild` 要求交接資料的 `BuildProfileId` 為 `Omsi23004_692EBFBF`，**且** `NativeValidateBuild()` 對執行中的映像檢查成功 | 遙測 `plugin.build.invalid`；主機以 `OL_E_BUILD_VALIDATION_FAILED` 使工作階段失敗；不會啟用任何原生 hook；OMSI 會被終止，交易會還原。 |

由於執行檔雜湊會與已建檔全域變數的大小與位元組進行比對，處理程序內的檢查是防範「通過雜湊檢查但載入時映像不同」之複本的最後一道防線。沒有備援設定檔，也沒有啟發式比對。

<a id="operating-system-and-architecture"></a>
## 作業系統與架構

`CurrentWindowsX64Platform.Detect` 會計算 `RuntimePlatformInfo`。只有在下列條件全部成立時，目前平台才受支援：

| 需求 | 檢查 | 違反時的錯誤 |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 或更新版本 | `Environment.OSVersion.Version.Major >= 10`（Windows 10、Windows 11、Server 2016+） | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64 位元 Windows 與 64 位元主機處理程序 | `OSArchitecture == X64` 且 `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| 可寫入的安裝 | 根目錄存在、不是唯讀，且包含 `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` 也會回報 `OmsiArchitecture` 與 `PluginArchitecture` 為 `X86`（OMSI 是 32 位元處理程序；外掛程式檔案集為 x86，在 WOW64 下執行）、`LegacyPlatform=false` 以及 `Wow64Available`。ARM64 Windows 即使具備 x64 模擬也不受支援，因為主機處理程序本身必須是 x64。

<a id="net-requirements"></a>
## .NET 需求

| 元件 | Runtime | 備註 |
| --- | --- | --- |
| 控制器（`OmsiLaunch.exe`、`OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`） | .NET 6，x64 | 原生啟動載入器透過套件隨附的 `nethost.dll` 經由 `hostfxr` 尋找 runtime。缺少 runtime 時由 shim 回報（結束代碼 100-106；請參閱 [CLI](cli.md) 與[結束代碼](exit-codes.md)）。 |
| 外掛程式檔案集（`plugins\OmsiLaunch.Plugin.dll`，經由 `OmsiLaunch.PluginNE.dll`） | .NET 6，**x86**（`net6.0-windows`、`win-x86`），由 DNNE 2.0.6 在 `Omsi.exe` 內部裝載 | 電腦上必須安裝 x86 的 .NET 6 Desktop／Core runtime；僅有 64 位元 runtime 並不足以執行外掛程式。 |
| 原生橋接層（`plugins\OmsiLaunch.Native.x86.dll`） | 原生 x86 | 只會從 `plugins\` 載入（請參閱[永久外掛程式](../concepts/permanent-plugin.md)）。 |

<a id="legacy-platforms"></a>
## 舊版平台

Windows 7、Windows 8.x、Windows XP 以及其他 NT 6 及更早的系統不在目前的支援範圍內。`RuntimePlatformInfo.LegacyPlatform` 一律為 `false`，也不存在任何舊版轉接器；此欄位與 `IPluginNativeServices` 接縫之所以存在，只是為了讓未來能在不變更公開 API 的情況下加入舊版轉接器（請參閱 `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`）。此版本中沒有任何部分能在這些系統上執行。

<a id="steam-and-large-address-aware-notes"></a>
## Steam 與 Large Address Aware 注意事項

- 帶有 LAA 標頭的 OMSI 2.3.004 Steam 發行版（`7DAB063D...`）之所以列入允許清單，是因為其已建檔位址與主要執行檔完全相同。在矩陣中記錄到實地驗證工作階段之前，請將該檔案上的每項功能都視為 `PARTIAL`。
- Steam 會自行啟動 OMSI；工作階段必須透過 `OmsiLaunch.exe` 啟動，才會有交接資料。若從 Steam 啟動，永久外掛程式會保持不作用（沒有交接資料，也沒有 hook）。
- 對 `Omsi.exe` 套用其他 LAA 修補工具會改變其雜湊，使其成為未知組建。

<a id="related-pages"></a>
## 相關頁面

- [已知限制](known-limitations.md)
- [執行階段驗證狀態](../status/runtime-validation-status.md)
- [安裝](../getting-started/installation.md)
