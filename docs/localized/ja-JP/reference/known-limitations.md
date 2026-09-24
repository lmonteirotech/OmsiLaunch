# 既知の制限事項

<!-- l10n: source=reference/known-limitations.md -->
> このページは OmsiLaunch 0.1.0-beta3 の[英語版の原文ページ](../../../reference/known-limitations.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページでは、OmsiLaunch 0.1.0-beta3 において `UNAVAILABLE`、`PARTIAL`、または受容済みリスクであるものをすべて、コードに基づいて列挙します。これは、利用者やインテグレーターが、製品が提供しない動作を前提に構築してしまうことを防ぐためです。各行には、制限事項、その安定性、それが存在する理由、および詳細が記載されている場所を示します。英語のドキュメントが規範です。`docs/localized/` 配下のローカライズ版は同じ水準では保守されておらず、内容が遅れている場合があります（最後のセクションを参照）。

<a id="compatibility"></a>
## 互換性

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| サポートされるのは `Omsi23004_692EBFBF`（`692EBFBF...6243`）のみです。Steam LAA のハッシュ `7DAB063D...D759` は許可リストに含まれています。そのフィンガープリントとプランは管理されたコピーで検証済みですが、ゲームプレイには正規の Steam インストール環境が必要です（イメージは DRM で保護された Steam の実行ファイルです） | Steam LAA については `PARTIAL` | [互換性](compatibility.md) |
| Windows 10 以降の x64 のみ。Windows 7/8、XP、ARM64 には対応していません | `UNAVAILABLE` | [互換性](compatibility.md) |
| プラグインには、コントローラーが使用する x64 ランタイムに加えて **x86** の .NET 6 ランタイムが必要です | | [互換性](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## ワールドの開始と起動オプション

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| `LAST_MAP_STATE`（`/last`、`WorldMode.LastMapState`）は未実装です。タイムスタンプに基づく `.osn` へのフォールバックで代替されることは一切ありません | `UNAVAILABLE`（BI-006） | プラン診断 `OL_E_CAPABILITY_UNAVAILABLE` |
| 明示的な日付・時刻・年、またはシステムの日付・時刻・年（`/date`、`/time`、`/year`、プロファイルの `new.date`/`new.time`/`new.year`、`DateSpec`/`TimeSpec`/`YearSpec`）は spec に保持されますが、プランを実行不可にします。プラグインは `Unset` 以外のモードを拒否します | `UNAVAILABLE`（`STATICALLY_PARTIAL`） | [セッションプロファイル](session-profiles.md)、[launchspec](launchspec.md) |
| 開始時の天候プリセット、ICAO、現在の実天候（`/weather*`、`new.weather`） | `UNAVAILABLE`（`STATICALLY_PARTIAL`、BI-003） | 同上 |
| 開始時のプレイヤー車両のモデル、リペイント、HOF、車両番号、ナンバープレート（`/vehicle` 系、`PlayerVehicleSpec`）はコンテンツカタログに照らして解決されますが、適用はされません。これらを要求するとプランは実行不可になります。決定論的なヘッドレスでの PlayerVehicle 割り当ては将来の拡張です | `UNAVAILABLE`（BI-007） | [ケイパビリティ](capabilities.md) の `player.assign-headless` |
| ID によるエントリポイント指定（`/entrypoint:<identity>`）は、OMSI が表示する一覧と対応付けられません。`/entrypoint-index` を使用してください | `PARTIAL`（BI-001） | プランのケイパビリティ `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| キーボードおよびコントローラーのドキュメントオーバーレイ（`InputSpec`、`Environment.Keyboard`、`Environment.Controllers`）は解析されますが、セッションによって適用されることはありません | `UNAVAILABLE`（BI-005） | `input.*` ケイパビリティ |
| `LaunchBehaviorSpec.RestoreConfiguration` と `InstallationSpec.ExpectedExecutableSha256` は宣言されていますが、読み取られることはありません | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds`（`/shutdown-timeout`、プロファイルの `shutdown-timeout`）は受け付けられて保持されますが、スーパーバイザーでは使用されません | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [セッションライフサイクル](../concepts/session-lifecycle.md) |
| `/quiet` と `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| 診断フラグ（`/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process`、`/trace-plugin`、`/trace-native`）は `DiagnosticsSpec` に値を設定します。目に見える効果は `.omsilaunch\diagnostics` 配下のホストトレースに限られます | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`、`/runtime-write-batch`、`/d3d-batch` は検証ハーネスです | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## セッションの終了とプロセス制御

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| セッションの停止は強制終了です。`session.stop`、トレイの "End session"、Ctrl+C、`CloseAsync` はいずれも `TerminateProcess` に至ります。OMSI のシャットダウン処理は実行されず、OMSI は終了時に `options.cfg` やログを書き直さず、OMSI の未保存の状態はすべて失われます。これは意図的なもので、復元されたファイルを OMSI が上書きすることを防ぎます。 | 設計上の仕様 | [セッションライフサイクル](../concepts/session-lifecycle.md) |
| タイムアウトと強制終了へのフォールバックを備えた、WM_CLOSE による協調的なシャットダウンは未実装です | `UNAVAILABLE`（製品としての判断、S-11。ランタイムクロージャーラウンドでは、OMSI はメインウィンドウへの `WM_CLOSE` を無視しました） | [ランタイム検証ステータス](../status/runtime-validation-status.md) |
| コンソールのクローズまたはログオフ時、オーナーには停止と復元のために 4 s の猶予があります。残ったものは次回の開始時にジャーナルによってリカバリされます | コンソールのクローズはランタイム検証済み。ログオフは未実施 | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## トランザクション、リカバリ、リース

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| インストールリースは `Local\` セマフォです。オーナーはインストール環境ごと、**ログオンセッションごと**に 1 つです。ユーザーをまたいでは強制されず、別のプロセスがハンドルを保持している間は解放されず、同じユーザーの任意のプロセスがその名前を保持できます | 受容済みリスク（S-18） | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |
| ジャーナルに記録された OMSI プロセス、または PID のないジャーナルの場合はそのルートの任意の `Omsi.exe` が実行中の間、リカバリは拒否されます（`OL_E_INSTALLATION_BUSY`） | 設計上の仕様 | 同上 |
| 元々存在しなかったオーバーレイパスの内容がセッション中に変更された場合、確認されるまで復元はブロックされます（`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`） | 設計上の仕様 | 同上 |
| 所有権フィンガープリント導入以前のジャーナルは、計画されたバイト列が同一のセッションによってのみクローズできます（`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`） | `PARTIAL` | 同上 |
| 復元されるのはセッションが所有するパスのみです。セッション中の OMSI 自身の書き込み（`options.cfg` をオーバーレイする設定がない場合の `options.cfg` の `[last_map]`、`Texture\standard.ipr`、キャッシュ、`laststn.osn`、運転士プロファイル、ログ）は、OMSI を直接起動した場合と同様に残ります | 設計上の仕様 | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |
| `SuppressStaleClosecheckWarning` が true の場合、セッション前に行われる古い `closecheck` の削除は恒久的です（記録はされますが、復元されません） | 設計上の仕様 | 同上 |

<a id="runtime-control"></a>
## ランタイム制御

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| `weather.set` は拒否されます（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`）。OMSI は次の天候ティックで、プロファイル化された両方の風の候補を上書きします | `UNAVAILABLE` | [ケイパビリティ](capabilities.md) |
| カレンダーの書き込み（`SetActualDateTime`） | `UNAVAILABLE`（BI-002） | `calendar.set-actual-date-time` |
| 文字列変数の書き込み、名前付きトリガー、サウンドトリガー（Delphi のマネージド文字列の所有権） | `UNAVAILABLE`（BI-004） | `scripts.string.read` は読み取り専用です |
| 車両の再配置、タイルをまたいだ空間的な再バインド、ODE に対して安全な変換権限はいずれもありません。位置フィールドは読み取り専用です | `UNAVAILABLE`（BI-008） | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` には PlayerVehicle が必要です。ヘッドレスの NEW_MAP 開始には PlayerVehicle がありません（保存済みシチュエーションでは提供されます） | 設計上の仕様（BI-007） | RV-004 |
| ランタイムでの変更（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、スポーン、ランダム配置、D3D テクスチャ）はジャーナルに記録されず、復元もされません | 設計上の仕様 | [ランタイム制御](runtime-control.md) |
| ハンドルのフィンガープリントの死角: 2 回のリスト読み取りの間に、同じクラスと定義のオブジェクトが同じアドレスに再作成された場合、失効したハンドルとして検出されません。自然な削除によるライフタイム（RV-002）には安全なランタイムでの発生手段がなく、オフラインのままです | `PARTIAL` | [ランタイム制御](runtime-control.md) |
| 結果は 64 KiB のメールボックスによって上限が設けられます。長いリストは切り詰められます（`truncated=true`）。ピクセルペイロードは `d3d.texture.update` 1 回あたり 48 KiB に制限されます | 設計上の仕様 | [ケイパビリティ](capabilities.md) |
| シングルフライトのチャネル: セッションごとに一度に 1 リクエストのみです。スロットがビジーの場合は `OL_E_RUNTIME_CHANNEL_BUSY` になります。リクエスト ID を再利用してはなりません | 設計上の仕様 | [ランタイム制御](runtime-control.md) |
| テレメトリは最新値のみを保持するスロットです。ホストの 100 ms のサンプリングより速いバーストでは、途中のイベントが失われることがあります（シーケンス番号により同一の連続イベントは区別され、不完全なサンプルはスキップされます） | `PARTIAL` | [常駐プラグイン](../concepts/permanent-plugin.md) |
| D3D のデバイスリセットはランタイムで観測されました（`resetting`、`restored`、世代の無効化）。OMSI のデバイスが直接 `DEVICENOTRESET` に移行したため、独立した `lost` 遷移は発生しませんでした | `PARTIAL`（RV-007） | [ランタイム検証ステータス](../status/runtime-validation-status.md) |
| 上限付きリストの結果（`timetable.*.list` 操作、`vehicle.variables.list`、`vehicle.string-variables.list`）は、64 KiB のランタイムスロットに収まる行までを返します。残りの行は省略され、`truncated=true` とより小さい `returned_count` が返されます（ドキュメント監査 BUG-05）。このリリースにはページングはありません | 設計上の仕様 | [ケイパビリティ](capabilities.md) |
| `timetable.logs.read`、`road-vehicles.list`、`humans.list`、`vehicle.constants.list` および `vehicle.curves.list` は上限付きではありません。スロットより大きい結果は `OL_E_RUNTIME_RESPONSE_TOO_LARGE` で失敗します（テストしたマップでは、いずれについても観測されていません） | `PARTIAL` | [ケイパビリティ](capabilities.md) |
| 自己申告のエビデンス文字列（`PublicCapabilityRegistry` の `RuntimeValidation`、`GetCapabilitiesAsync` の `EvidenceState`）は、ランタイムクロージャーラウンド後に更新されていません。`camera.lock` は依然として `STATICALLY_VALIDATED`、`runtime.d3d.lifecycle.reset` は `IMPLEMENTED_NOT_RUNTIME_VALIDATED` と表示されます。[ランタイム検証ステータス](../status/runtime-validation-status.md) ページが正式な情報です | ドキュメントの遅れであり、動作の違いではありません | [ケイパビリティ](capabilities.md) |
| 高度なマップ/タイル/パス/オブジェクトグラフのフィールドの一部は公開されていません。ランタイムリーダーはプロファイルで制限された型付きスナップショットであり、任意のメモリアクセスではありません | 設計上の仕様 | [ケイパビリティ](capabilities.md) |
| プロセス内のメモリ読み取りは、稼働中の OMSI に対してチェックしてから使用する方式です。チェックと読み取りの間に OMSI が並行して変更を行うと、一貫性のないスナップショットになる可能性があります（`OL_E_RUNTIME_OPERATION_FAILED`） | 受容済みリスク（S-33） | |

<a id="local-control-plane-and-trust-model"></a>
## ローカルコントロールプレーンと信頼モデル

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| 同一ユーザー信頼モデル: 名前付きパイプ（`CurrentUserOnly`）、ハンドオフ/テレメトリ/ランタイムのメモリマッピング、およびリースのセマフォには、同じ Windows ユーザーの任意のプロセスがアクセスできます。そのようなプロセスは、`session_id` を読み取ると、ステータスの読み取り、セッションの停止、ランタイム操作の実行が可能になります。 | 受容済みリスク（S-06、S-30） | [ローカル制御](local-control.md) |
| コントロールエンドポイントはオーナーが `Running` の間のみ存在します。起動中およびセッション終了後、クライアントには `OL_E_NO_ACTIVE_SESSION`（終了コード 4）が返されます | 設計上の仕様 | [ローカル制御](local-control.md) |
| 別のプロセスがすでにパイプ名を所有している場合、オーナーはエンドポイントなしで実行を続け（`ListenFault`）、2 回目の起動で `OL_E_SESSION_ALREADY_ACTIVE` が誤って報告される可能性があります | 受容済みリスク | [ローカル制御](local-control.md) |
| `.omsilaunch\` は OMSI ルートの ACL を継承します。明示的なアクセス制御は適用されません | 受容済みリスク（S-31） | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## 診断と出力

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| 診断情報はローカルファイル（`.omsilaunch\diagnostics`）のみです。何もアップロードされず、リモートへのレポート送信もありません | 設計上の仕様 | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |
| 保持されるのは最新 50 セッション分です。新しいセッションの開始時に、それより古いセッション接頭辞付きの診断情報は削除されます | 設計上の仕様 | 同上 |
| JSON 出力と診断情報にはインストールパス（`RootPath`、アセットディレクトリ、`.itx` パス）が含まれます | 設計上の仕様（ローカルデータ） | |
| `OmsiLaunchW.exe` のセッション失敗ダイアログは、メッセージとして文章ではなくプラグインの失敗ペイロード（たとえば `{"name":"world.failed",...}`）を表示します。`Code:` 行は正しい内容です | 表示上の問題 | [Windows トレイ](windows-tray.md) |
| Direct3D の呼び出しより前にネイティブブリッジによって拒否された D3D リクエストは、`native_status` を正しく報告しますが、その `detail` テキストは `HRESULT 0x00000000` となります | 表示上の問題 | [ケイパビリティ](capabilities.md) |
| トレイのステータスウィンドウは、開いた時点で取得された、プランニングされたセッションのスナップショットです。更新されず、OMSI のライブ値は表示されません | 設計上の仕様 | [Windows トレイ](windows-tray.md) |

<a id="documentation"></a>
## ドキュメント

`docs/` 配下の英語ページが、このリリースの規範となるドキュメントです。`docs/localized/<locale>/` には同じ `0.1.0-beta3` ページの翻訳が含まれています（[`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md) を参照）。翻訳と英語のテキストが異なる場合は、英語のテキストとコードが正となります。そこに記載されている履歴ページおよびレガシーページは英語でのみ提供されています。

関連項目: [ケイパビリティ](capabilities.md)、[ランタイム検証ステータス](../status/runtime-validation-status.md)、[エラー](errors.md)。
