# 兼容性

<!-- l10n: source=reference/compatibility.md -->
> 本页是 OmsiLaunch 0.1.0-beta3 [英文原始页面](../../../reference/compatibility.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

OmsiLaunch 通过修补某个确切可执行文件版本（build）内部经过剖析的地址来驱动 OMSI。本页说明支持哪些 OMSI 版本、使用任何其他版本时会发生什么，以及宿主和插件的操作系统与运行时要求。来源：`src/OmsiLaunch.Builds.Omsi23004/Profile.cs`、`src/OmsiLaunch.Core/SessionPlanner.cs`、`src/OmsiLaunch.Process/RuntimePlatform.cs`、`src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs` 以及项目文件。

<a id="supported-omsi-builds"></a>
## 受支持的 OMSI 版本

构建配置只有一个：`Omsi23004_692EBFBF`（系列 `OMSI_2_3_004_COMMON`）。它按确切的 SHA-256 接受两个可执行文件：

| 变体 | `Omsi.exe` SHA-256 | 大小 | PE 文件版本 / 产品版本 | 状态 |
| --- | --- | --- | --- | --- |
| 已剖析的可执行文件（`ALTERNATE_LAA`） | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 字节 | 2.2.032 / 2.3.004 | `STABLE_BETA`；矩阵中的每一项运行时验证都是在此文件上执行的 |
| Steam LAA（`STEAM_LAA`） | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | 不检查 | | 因与已剖析的原生布局相同、仅在可执行文件头上有差异而被允许列表接受；**未经运行时验证**（`profiles` 报告 `runtime_validated=false`、`validation_status=pending_beta_field_validation`）。`PARTIAL`。 |

`OmsiLaunch.exe profiles` 以 JSON 形式打印此表。版本号不用于判定是否接受：只有 SHA-256（对于主可执行文件，还有确切的大小）才起作用。不支持任何其他 OMSI 2 版本、任何经过修补的可执行文件，以及任何哈希不同的 4 GB 补丁副本。

<a id="what-happens-with-an-unknown-build"></a>
## 使用未知版本时会发生什么

| 阶段 | 检查 | 结果 |
| --- | --- | --- |
| 规划（`PlanSessionAsync`、`/plan`、`/validate`） | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | 必需能力 `omsi.profile.OMSI23004` 为 `UNAVAILABLE`；诊断信息 `OL_E_UNSUPPORTED_BUILD`；`SessionPlan.IsRunnable=false`。启动时 CLI 退出码为 1；错误以异常形式逃逸时为 3（`UnsupportedProfile`）。 |
| 启动（`StartSessionAsync`） | 重新规划 spec 并重新计算 `Omsi.exe` 的哈希 | 不再可运行的计划（例如可执行文件在规划后被更改，或调用方修改了 `IsRunnable`）会以 `OL_E_PLAN_NOT_RUNNABLE` 被拒绝；不会打开事务，也不会启动进程。 |
| 进程内（`PluginRuntime.Start`） | `NativeServices.ValidateBuild` 要求交接数据中的 `BuildProfileId` 为 `Omsi23004_692EBFBF`，**并且** `NativeValidateBuild()` 针对正在运行的映像成功 | 遥测 `plugin.build.invalid`；宿主以 `OL_E_BUILD_VALIDATION_FAILED` 使会话失败；不会启用任何原生钩子；OMSI 被终止，事务被还原。 |

由于会将可执行文件哈希与已剖析全局变量的大小和字节进行比较，进程内检查是防御以下副本的最后一道防线：该副本通过了哈希检查，但其映像在加载时有所不同。不存在回退配置，也不存在启发式匹配。

<a id="operating-system-and-architecture"></a>
## 操作系统与架构

`CurrentWindowsX64Platform.Detect` 计算 `RuntimePlatformInfo`。只有在以下条件全部满足时，当前平台才受支持：

| 要求 | 检查 | 不满足时的错误 |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 或更高版本 | `Environment.OSVersion.Version.Major >= 10`（Windows 10、Windows 11、Server 2016+） | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64 位 Windows 和 64 位宿主进程 | `OSArchitecture == X64` 且 `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| 可写的安装实例 | 根目录存在、不是只读，并且包含 `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` 还将 `OmsiArchitecture` 和 `PluginArchitecture` 报告为 `X86`（OMSI 是 32 位进程；插件闭包为 x86，在 WOW64 下运行），并报告 `LegacyPlatform=false` 和 `Wow64Available`。即使存在 x64 仿真，也不支持 ARM64 Windows，因为宿主进程本身必须是 x64。

<a id="net-requirements"></a>
## .NET 要求

| 组件 | 运行时 | 说明 |
| --- | --- | --- |
| 控制器（`OmsiLaunch.exe`、`OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`） | .NET 6，x64 | 原生引导程序通过随包提供的 `nethost.dll` 借助 `hostfxr` 定位运行时。缺少运行时的情况由 shim 报告（退出码 100-106；见 [CLI](cli.md) 和[退出码](exit-codes.md)）。 |
| 插件闭包（通过 `OmsiLaunch.PluginNE.dll` 加载的 `plugins\OmsiLaunch.Plugin.dll`） | .NET 6，**x86**（`net6.0-windows`、`win-x86`），由 DNNE 2.0.6 在 `Omsi.exe` 内部托管 | 需要在计算机上安装 x86 .NET 6 Desktop/Core 运行时；仅有 64 位运行时不足以运行插件。 |
| 原生桥（`plugins\OmsiLaunch.Native.x86.dll`） | 原生 x86 | 仅从 `plugins\` 加载（见[永久插件](../concepts/permanent-plugin.md)）。 |

<a id="legacy-platforms"></a>
## 旧版平台

Windows 7、Windows 8.x、Windows XP 以及其他 NT 6 及更早的系统不在当前支持范围内。`RuntimePlatformInfo.LegacyPlatform` 始终为 `false`，也不存在旧版适配器；该字段和 `IPluginNativeServices` 接缝的存在，只是为了将来可以在不更改公共 API 的情况下添加旧版适配器（见 `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`）。本发行版中没有任何内容能在这些系统上运行。

<a id="steam-and-large-address-aware-notes"></a>
## Steam 与 Large Address Aware 说明

- 带有 LAA 文件头的 OMSI 2.3.004 Steam 发行版（`7DAB063D...`）被列入允许列表，因为其经过剖析的地址与主可执行文件相同。在矩阵中记录现场验证会话之前，请将该文件上的每项能力都视为 `PARTIAL`。
- Steam 会自行启动 OMSI；会话必须通过 `OmsiLaunch.exe` 启动，才会存在交接数据。从 Steam 启动时，永久插件保持非活动状态（没有交接，没有钩子）。
- 对 `Omsi.exe` 应用其他 LAA 修补工具会改变其哈希，使其成为未知版本。

<a id="related-pages"></a>
## 相关页面

- [已知限制](known-limitations.md)
- [运行时验证状态](../status/runtime-validation-status.md)
- [安装](../getting-started/installation.md)
