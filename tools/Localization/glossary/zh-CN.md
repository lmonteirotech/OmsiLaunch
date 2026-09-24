# zh-CN 术语表（OmsiLaunch 0.1.0-beta3 文档）

本表适用于 `docs/localized/zh-CN/` 下的所有译文。所有译者必须一致使用下列译法。没有旧版 zh-CN 译文，本表即为唯一术语来源。

## 称呼与语体

- 采用正式、客观的简体中文技术文档语体（中国大陆用语）。优先使用无主语或被动/客观句式（“需要……”“可以……”）；必须称呼读者时用“您”，全文一致，不用“你”。
- 用语以中国大陆 IT 习惯为准：文件、进程、线程、默认、设置、用户、程序集、内存、代码、目录、配置、日志。不要使用台湾用语（檔案、處理程序、執行緒、預設、使用者、組件、記憶體、程式碼）。
- 限定词强度必须与英文一致：`UNAVAILABLE`、`PARTIAL`、`EXPERIMENTAL` 等状态词原样保留；“not runtime validated”译为“未经运行时验证”，“offline only”译为“仅离线”，“not implemented”译为“未实现”，“by design”译为“设计如此”，“accepted risk”译为“已接受的风险”。不得弱化为“暂不支持”或强化为“损坏/缺陷”。

## 标点与排版

- 中文句子使用全角标点：，。；：？！（）“”、《》。句中列举用顿号“、”。
- 行内代码、英文专有名词、数字与单位保持原样；不在代码两侧加全角括号以外的额外符号。代码或英文术语紧邻中文时，可在两侧加一个半角空格（例如“调用 `StopAsync` 后”），全文保持一致：**加空格**。
- 括号内若全部是英文/代码（例如 `(exit 2)` 类说明），仍使用全角括号“（）”，括号内的代码保持原样。
- 数字与单位原样保留（`64 KiB`、`250 ms`、`2 s`、100 ms），数字与中文之间加半角空格（“最多 50 个会话”）。
- 引用英文 UI 字面量（如 `End session`、`Session is running`）必须保持反引号原样，并在正文中用中文解释其含义；产品界面没有简体中文本地化，**不得**把中文译名当作产品显示的文字，也不得在括号中加入虚构的中文界面字符串。
- 英文中的双引号引语用中文引号“”；破折号用“——”；省略号用“……”（代码中的 `...` 不改）。
- 标题不加句号；表格单元格若为完整句子，句末加“。”，若为短语则不加。

## 术语表

| English | zh-CN | note |
| --- | --- | --- |
| session | 会话 | 全文统一。 |
| session owner / owner | 会话所有者 / 所有者 | 指负责规划、启动、监管和还原会话的进程；“owner mode”译“所有者模式”。 |
| client (mode) | 客户端（模式） | 通过本地控制管道转发命令的调用。 |
| owner process | 所有者进程 | |
| launch | 启动（名词/动词） | “launch flag”译“启动参数”；“launcher”译“启动器”。与 start 同译“启动”，上下文需区分时可写“发起启动”。 |
| plan (noun/verb) | 计划（名词）/ 规划（动词） | “session plan”译“会话计划”；“re-plan”译“重新规划”。 |
| runnable / not runnable | 可运行 / 不可运行 | “non-runnable plan”译“不可运行的计划”。 |
| start | 启动 | “start phase”译“启动阶段”；“world start”译“世界启动”。 |
| stop | 停止 | “stop request”译“停止请求”。 |
| end session | 结束会话 | UI 字面量 `End session` 保持英文原样，正文解释为“结束会话”菜单项/按钮。 |
| canonical stop | 规范停止（路径） | “canonical stop path”译“规范停止路径”。 |
| restore | 还原 | “exact restore”译“精确还原”；“restore verification”译“还原校验”。 |
| recovery | 恢复 | “crash recovery”译“崩溃恢复”；区别于 restore（还原）。 |
| pending (journal) | 待处理（的日志） | “a transaction is pending”译“存在待处理的事务”。 |
| journal | 日志（事务日志） | 首次出现可写“事务日志（journal）”；指 `journal.json`。与普通 log 区分：log 译“日志文件/记录”，需要区分时 journal 写“事务日志”。 |
| transaction | 事务 | “configuration transaction”译“配置事务”。 |
| overlay | 覆盖层（overlay） | 首次出现附英文；“temporary overlays”译“临时覆盖层”。 |
| backup | 备份 | |
| snapshot | 快照 | |
| installation | 安装（实例）/ OMSI 安装 | 指某个 OMSI 安装目录实例时译“安装实例”；“installation lease”译“安装租约”。 |
| installation root | 安装根目录 | |
| package | 包 | “session-profile package”译“会话配置档包”。 |
| release package | 发行包 | |
| manifest | 清单（manifest） | `release-manifest.json` 保持原样；“release manifest”译“发行清单”。 |
| permanent plugin | 永久插件 | “plugin closure”译“插件闭包”。 |
| native bridge | 原生桥 | |
| runtime | 运行时 | 保留中文惯用“运行时”；“.NET runtime”译“.NET 运行时”。 |
| runtime operation | 运行时操作 | |
| runtime command | 运行时命令 | “runtime command channel”译“运行时命令通道”。 |
| runtime slot / mailbox | 运行时槽 / 邮箱（mailbox） | “telemetry slot”译“遥测槽”；“runtime mailbox”译“运行时邮箱”。 |
| control plane | 控制平面 | “local control plane”译“本地控制平面”。 |
| local control | 本地控制 | “local control endpoint”译“本地控制端点”。 |
| named pipe | 命名管道 | “pipe”单独出现译“管道”。 |
| frame (protocol) | 帧 | 协议中的数据帧。 |
| envelope (JSON) | 信封（envelope） | “output envelope”译“输出信封”；首次出现附英文。 |
| handle | 句柄 | 运行时对象句柄与进程句柄都译“句柄”。 |
| stale handle | 失效句柄 | |
| capability | 能力 | “capability id”译“能力 ID”。 |
| capability registry | 能力注册表 | |
| bounded list | 有界列表 | BUG-05：结果超出 64 KiB 时返回可容纳的最长前缀，`truncated=true`，属成功回复，不是失败。 |
| truncated | 截断 | “was truncated”译“被截断”。 |
| row | 行 | 列表结果中的一行（记录）。 |
| evidence | 证据 | “runtime evidence”译“运行时证据”；“evidence id”译“证据编号”。 |
| runtime-validated | 经运行时验证 | 否定式“未经运行时验证”。`RUNTIME_VALIDATED` 字面量保持原样。 |
| statically validated | 经静态验证 | `STATICALLY_VALIDATED` 字面量保持原样。 |
| offline test | 离线测试 | “offline only”译“仅离线”。 |
| gate (documentation gate) | 门禁（文档门禁） | “the gate fails the build”译“门禁会使构建失败”。 |
| tray icon | 托盘图标 | “tray”单独出现译“托盘”。 |
| notification area | 通知区域 | Windows 官方简体中文用语。 |
| status window | 状态窗口 | |
| confirmation dialog | 确认对话框 | |
| message box | 消息框 | |
| failure dialog | 失败对话框 | |
| tooltip | 工具提示 | |
| context menu | 上下文菜单 | 也可用“右键菜单”，全文统一用“上下文菜单”。 |
| Explorer restart | 资源管理器重启 | Explorer 指 Windows 资源管理器（外壳）；`explorer.exe` 原样。 |
| entry point | 入口点 | OMSI 地图入口点；“entrypoint index”译“入口点索引”。 |
| new map | 新地图 | NEW_MAP 字面量保持原样；UI 值 `New session` 保持原样。 |
| saved situation | 已保存情景 | `.osn` 情景文件；SAVED_SITUATION 保持原样；“situation”单独出现译“情景”。 |
| map | 地图 | |
| splash screen | 启动画面 | “managed splash”译“托管启动画面”。 |
| Internet Textures | 网络纹理（Internet Textures） | OMSI 功能名，首次出现附英文。 |
| session profile | 会话配置档 | 与 build profile（构建配置 `Omsi23004_692EBFBF`）区分：后者译“构建配置”。 |
| preset | 预设 | |
| setting | 设置项 | “semantic setting”译“语义设置项”。 |
| player vehicle | 玩家车辆 | `PlayerVehicle` 原样。 |
| road vehicle | 道路车辆 | `RoadVehicle` 原样。 |
| human (pedestrian/passenger object) | 人物对象（行人/乘客） | `Human` 原样。 |
| timetable | 时刻表 | |
| track entry | 线路轨迹条目 | OMSI 时刻表中的 track 条目。 |
| tour entry | 班次条目 | OMSI 时刻表中的 tour（班次/排班）条目。 |
| ticket | 车票 | |
| driver | 司机 | “driver profile”译“司机档案”。 |
| fleet number | 车队编号 | UI 字面量 `Fleet number` 原样。 |
| registration (plate) | 车牌号 | |
| repaint | 涂装 | |
| spawn | 生成 | “spawned vehicles”译“生成的车辆”。 |
| camera lock | 摄像机锁定 | `camera.lock` 原样。 |
| device reset | 设备重置 | 指 D3D 设备重置。 |
| render thread | 渲染线程 | |
| texture | 纹理 | |
| exit code | 退出码 | |
| error code | 错误码 | |
| diagnostic | 诊断信息 | “plan diagnostic”译“计划诊断信息”。 |
| diagnostics directory | 诊断目录 | 指 `.omsilaunch\diagnostics`。 |
| timeout | 超时 | “startup timeout”译“启动超时”。 |
| placeholder | 占位符 | |
| flag | 参数（命令行参数） | 如 `/set`；避免译“标志”。 |
| route | 路由 | “hierarchical route”译“层级路由”。 |
| command word | 命令词 | |
| integrator | 集成方 | 指调用 `OmsiLaunch.Api` 的开发者。 |
| caller | 调用方 | |
| known limitation | 已知限制 | |
| accepted risk | 已接受的风险 | |
| stable beta | 稳定 Beta | `STABLE_BETA` 字面量原样。 |
| experimental | 实验性 | `EXPERIMENTAL` 原样。 |
| partial | 部分（实现/支持） | `PARTIAL` 原样；不得译为“不支持”。 |
| unavailable | 不可用 | `UNAVAILABLE` 原样。 |
| deprecated / legacy | 已弃用 / 旧版 | |
| not normative | 非规范性 | “normative”译“规范性”。 |
| source of truth | 唯一权威来源 | 英文页面是唯一权威来源。 |

## 其他常用固定译法

| English | zh-CN | note |
| --- | --- | --- |
| telemetry | 遥测 | |
| handoff | 交接（handoff） | 启动交接共享内存。 |
| lease | 租约 | |
| supervisor | 监管器 | “supervision”译“监管”。 |
| forced termination | 强制终止 | |
| cooperative shutdown | 协作式关闭 | |
| gameplay | 游戏过程 | “reach gameplay”译“进入游戏”。 |
| shim | shim（引导垫片） | 保留英文 shim。 |
| bootstrapper | 引导程序 | |
| hash | 哈希 | |
| API / CLI / IPC / plugin / build | API / CLI / IPC / 插件 / 构建 | plugin 译“插件”；build 指 OMSI 版本时译“版本（build）”。 |
