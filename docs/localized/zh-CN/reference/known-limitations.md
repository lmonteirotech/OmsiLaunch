# 已知限制

<!-- l10n: source=reference/known-limitations.md -->
> 本页是 OmsiLaunch 0.1.0-beta3 [英文原始页面](../../../reference/known-limitations.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页依据代码列出 OmsiLaunch 0.1.0-beta3 中所有 `UNAVAILABLE`、`PARTIAL` 或属于已接受的风险的内容，以免用户和集成方依赖产品并未提供的行为。每一行给出限制内容、其稳定性、存在的原因，以及详细说明所在的位置。英文文档具有规范性；`docs/localized/` 下的本地化副本的维护程度与英文不同，可能会滞后（见最后一节）。

<a id="compatibility"></a>
## 兼容性

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 仅支持 `Omsi23004_692EBFBF`（`692EBFBF...6243`）；Steam LAA 哈希 `7DAB063D...D759` 在允许列表中，其指纹和计划已通过受控副本验证，但游戏过程需要真正的 Steam 安装（该映像是受 DRM 保护的 Steam 可执行文件） | Steam LAA 为 `PARTIAL` | [兼容性](compatibility.md) |
| 仅支持 Windows 10+ x64；不支持 Windows 7/8、XP 和 ARM64 | `UNAVAILABLE` | [兼容性](compatibility.md) |
| 除控制器使用的 x64 运行时之外，插件还需要 **x86** .NET 6 运行时 | | [兼容性](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## 世界启动与启动选项

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| `LAST_MAP_STATE`（`/last`、`WorldMode.LastMapState`）未实现；绝不会以基于时间戳的 `.osn` 回退方案替代 | `UNAVAILABLE`（BI-006） | 计划诊断信息 `OL_E_CAPABILITY_UNAVAILABLE` |
| 显式或系统的日期、时间和年份（`/date`、`/time`、`/year`，配置档 `new.date`/`new.time`/`new.year`，`DateSpec`/`TimeSpec`/`YearSpec`）会被携带在规格中，但会使计划不可运行；插件拒绝非 `Unset` 的模式 | `UNAVAILABLE`（`STATICALLY_PARTIAL`） | [会话配置档](session-profiles.md)、[launchspec](launchspec.md) |
| 启动时的天气预设、ICAO 天气和实时天气（`/weather*`、`new.weather`） | `UNAVAILABLE`（`STATICALLY_PARTIAL`，BI-003） | 同上 |
| 启动时的玩家车辆型号、涂装、HOF、车队编号、车牌号（`/vehicle` 系列、`PlayerVehicleSpec`）会依据内容目录进行解析，但不会应用；请求这些内容会使计划不可运行；确定性的无界面 PlayerVehicle 分配属于未来的扩展 | `UNAVAILABLE`（BI-007） | [能力](capabilities.md) 中的 `player.assign-headless` |
| 按标识指定入口点（`/entrypoint:<identity>`）未与 OMSI 展示的列表建立对应关系；请使用 `/entrypoint-index` | `PARTIAL`（BI-001） | 计划能力 `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| 键盘和控制器文档覆盖层（`InputSpec`、`Environment.Keyboard`、`Environment.Controllers`）会被解析，但任何会话都不会应用 | `UNAVAILABLE`（BI-005） | `input.*` 能力 |
| `LaunchBehaviorSpec.RestoreConfiguration` 和 `InstallationSpec.ExpectedExecutableSha256` 已声明但从不读取 | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds`（`/shutdown-timeout`，配置档 `shutdown-timeout`）会被接受和携带，但监管器不会使用 | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [会话生命周期](../concepts/session-lifecycle.md) |
| `/quiet` 和 `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| 诊断参数（`/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process`、`/trace-plugin`、`/trace-native`）会填充 `DiagnosticsSpec`；可见效果仅限于 `.omsilaunch\diagnostics` 下的宿主跟踪 | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`、`/runtime-write-batch`、`/d3d-batch` 是验证测试工具 | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## 会话结束与进程控制

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 会话停止是强制终止：`session.stop`、托盘的“End session”菜单项（结束会话）、Ctrl+C 和 `CloseAsync` 最终都会调用 `TerminateProcess`。OMSI 的关闭例程不会运行，OMSI 退出时不会重写 `options.cfg` 或其日志文件，所有未保存的 OMSI 状态都会丢失。这是有意为之：可以防止 OMSI 覆盖已还原的文件。 | 设计如此 | [会话生命周期](../concepts/session-lifecycle.md) |
| 带超时和终止回退的协作式 WM_CLOSE 关闭未实现 | `UNAVAILABLE`（产品决定，S-11；在运行时收尾轮次中，OMSI 忽略了发送到其主窗口的 `WM_CLOSE`） | [运行时验证状态](../status/runtime-validation-status.md) |
| 控制台关闭或注销时，所有者有 4 s 的时间预算来停止并还原；未完成的部分会在下一次启动时通过事务日志恢复 | 控制台关闭已经过运行时验证；注销未测试 | [事务与恢复](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## 事务、恢复与租约

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 安装租约是一个 `Local\` 信号量：每个安装实例**在每个登录会话中**只有一个所有者；不跨用户强制执行；只要有其他进程持有句柄就不会释放；同一用户的任何进程都可以持有该名称 | 已接受的风险（S-18） | [事务与恢复](../concepts/transactions-and-recovery.md) |
| 当事务日志中记录的 OMSI 进程仍在运行时（对于没有 PID 的事务日志，则是该根目录下的任何 `Omsi.exe` 在运行时），恢复会被拒绝（`OL_E_INSTALLATION_BUSY`） | 设计如此 | 同上 |
| 原本不存在的覆盖层路径若在会话期间内容发生变化，会阻止还原（`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`），直到经过检查为止 | 设计如此 | 同上 |
| 所有权指纹引入之前的事务日志只能由计划字节完全相同的会话关闭（`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`） | `PARTIAL` | 同上 |
| 只还原会话所属的路径。OMSI 自身在会话期间的写入（当没有设置项覆盖 `options.cfg` 时的 `options.cfg` `[last_map]`、`Texture\standard.ipr`、缓存、`laststn.osn`、司机档案、日志文件）会保留下来，与直接启动 OMSI 后的情况相同 | 设计如此 | [事务与恢复](../concepts/transactions-and-recovery.md) |
| 当 `SuppressStaleClosecheckWarning` 为 true 时，会话之前对失效 `closecheck` 的移除是永久性的（会被记录，但不会还原） | 设计如此 | 同上 |

<a id="runtime-control"></a>
## 运行时控制

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| `weather.set` 被拒绝（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`）：OMSI 会在下一次天气更新时覆盖两个经过配置分析的风力候选值 | `UNAVAILABLE` | [能力](capabilities.md) |
| 日历写入（`SetActualDateTime`） | `UNAVAILABLE`（BI-002） | `calendar.set-actual-date-time` |
| 字符串变量写入、命名触发器、声音触发器（Delphi 托管字符串的所有权问题） | `UNAVAILABLE`（BI-004） | `scripts.string.read` 为只读 |
| 不支持车辆重新定位、跨地块空间重新绑定，也没有 ODE 安全的变换控制权；位置字段为只读 | `UNAVAILABLE`（BI-008） | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` 需要 PlayerVehicle；无界面的 NEW_MAP 启动没有 PlayerVehicle（已保存情景会提供一个） | 设计如此（BI-007） | RV-004 |
| 运行时变更（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、生成、随机放置、D3D 纹理）不会记入事务日志，也不会被还原 | 设计如此 | [运行时控制](runtime-control.md) |
| 句柄指纹盲区：若同一类别、同一定义的对象在两次列表读取之间于同一地址重新创建，则不会被检测为失效；自然移除的生命周期（RV-002）没有安全的运行时制造方式，仍仅离线 | `PARTIAL` | [运行时控制](runtime-control.md) |
| 结果受 64 KiB 邮箱限制：长列表会被截断（`truncated=true`）；每次 `d3d.texture.update` 的像素负载限制为 48 KiB | 设计如此 | [能力](capabilities.md) |
| 单飞行通道：每个会话同一时间只处理一个请求；槽忙时返回 `OL_E_RUNTIME_CHANNEL_BUSY`；请求 ID 不得重复使用 | 设计如此 | [运行时控制](runtime-control.md) |
| 遥测是最新值槽：快于宿主 100 ms 采样间隔的突发事件可能会丢失中间事件（序列号使相同的连续事件保持可区分；撕裂的样本会被跳过） | `PARTIAL` | [永久插件](../concepts/permanent-plugin.md) |
| 已在运行时观察到 D3D 设备重置（`resetting`、`restored`、代次失效）；由于 OMSI 的设备直接进入 `DEVICENOTRESET`，未产生单独的 `lost` 状态转换 | `PARTIAL`（RV-007） | [运行时验证状态](../status/runtime-validation-status.md) |
| 有界列表结果（`timetable.*.list` 操作、`vehicle.variables.list`、`vehicle.string-variables.list`）最多返回能容纳在 64 KiB 运行时槽中的行；其余行会被省略，并返回 `truncated=true` 和较小的 `returned_count`（文档审计 BUG-05）。此版本不提供分页 | 设计如此 | [能力](capabilities.md) |
| `timetable.logs.read`、`road-vehicles.list`、`humans.list`、`vehicle.constants.list` 和 `vehicle.curves.list` 不是有界的：大于槽的结果会以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失败（在已测试的地图上，这些操作均未出现此情况） | `PARTIAL` | [能力](capabilities.md) |
| 自报告的证据字符串（`PublicCapabilityRegistry` `RuntimeValidation`、`GetCapabilitiesAsync` `EvidenceState`）在运行时收尾轮次之后未更新：`camera.lock` 仍显示 `STATICALLY_VALIDATED`，`runtime.d3d.lifecycle.reset` 仍显示 `IMPLEMENTED_NOT_RUNTIME_VALIDATED`。以[运行时验证状态](../status/runtime-validation-status.md)页面为准 | 文档滞后，并非行为差异 | [能力](capabilities.md) |
| 部分高级的地图/地块/路径/对象图字段未公开；运行时读取器是受构建配置限制的类型化快照，绝不提供任意内存访问 | 设计如此 | [能力](capabilities.md) |
| 进程内内存读取针对正在运行的 OMSI 采用“先检查后使用”的方式；在检查与读取之间，OMSI 的并发修改可能导致快照不一致（`OL_E_RUNTIME_OPERATION_FAILED`） | 已接受的风险（S-33） | |

<a id="local-control-plane-and-trust-model"></a>
## 本地控制平面与信任模型

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 同一用户信任模型：命名管道（`CurrentUserOnly`）、交接/遥测/运行时内存映射以及租约信号量均可被同一 Windows 用户的任何进程访问。此类进程一旦读取到 `session_id`，就可以读取状态、停止会话或执行运行时操作。 | 已接受的风险（S-06、S-30） | [本地控制](local-control.md) |
| 控制端点仅在所有者处于 `Running` 状态时存在；在启动期间和会话结束之后，客户端会看到 `OL_E_NO_ACTIVE_SESSION`（退出码 4） | 设计如此 | [本地控制](local-control.md) |
| 如果另一个进程已占用该管道名称，所有者会在没有端点的情况下继续运行（`ListenFault`），而第二次启动可能会错误地报告 `OL_E_SESSION_ALREADY_ACTIVE` | 已接受的风险 | [本地控制](local-control.md) |
| `.omsilaunch\` 继承 OMSI 根目录的 ACL；不会应用显式的访问控制 | 已接受的风险（S-31） | [事务与恢复](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## 诊断信息与输出

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 诊断信息仅为本地文件（`.omsilaunch\diagnostics`）；不会上传任何内容，也没有远程报告 | 设计如此 | [事务与恢复](../concepts/transactions-and-recovery.md) |
| 保留策略保留最新的 50 个会话；新会话启动时，会删除较旧的、以会话为前缀的诊断信息 | 设计如此 | 同上 |
| JSON 输出和诊断信息包含安装路径（`RootPath`、资源目录、`.itx` 路径） | 设计如此（本地数据） | |
| `OmsiLaunchW.exe` 的会话失败对话框将插件的失败负载（例如 `{"name":"world.failed",...}`）作为消息显示，而不是一句说明文字；`Code:` 行是正确的 | 外观问题 | [Windows 托盘](windows-tray.md) |
| 在任何 Direct3D 调用之前就被原生桥拒绝的 D3D 请求会正确报告 `native_status`，但其 `detail` 文本显示 `HRESULT 0x00000000` | 外观问题 | [能力](capabilities.md) |
| 托盘状态窗口是打开时对已计划会话拍摄的快照；它不会刷新，也不显示 OMSI 的实时值 | 设计如此 | [Windows 托盘](windows-tray.md) |

<a id="documentation"></a>
## 文档

`docs/` 下的英文页面是此版本的规范性文档。`docs/localized/<locale>/` 包含相同 `0.1.0-beta3` 页面的翻译（参见 [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)）；译文与英文文本不一致时，以英文文本和代码为准。其中列出的历史页面和旧版页面仅提供英文版本。

相关页面：[能力](capabilities.md)、[运行时验证状态](../status/runtime-validation-status.md)、[错误](errors.md)。
