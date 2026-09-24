# ja-JP 用語集（OmsiLaunch 0.1.0-beta3 ドキュメント）

## 文体と表記

- 本文は「です・ます」調の技術文書とし、読者への呼びかけは原則として省略します（必要な場合のみ「利用者」「インテグレーター」と三人称で書き、「あなた」は使いません）。命令・手順は「〜してください」「〜します」で統一します。
- 句読点は全角「、」「。」を使い、括弧は日本語文中では全角（）、コード・英数字のみを囲む場合は半角 () でも可ですが、1 ページ内で統一します。
- 英数字・インラインコード・`STABLE_BETA` などのトークンと和文の間には半角スペースを入れます（例: 「`Running` セッション」「64 KiB の上限」）。
- 数値と単位は英語原文のまま（`64 KiB`、`250 ms`、`2 s`）。全角数字は使いません。
- 長音記号: 語末長音は JIS/Microsoft 方式で付けます（サーバー、ユーザー、フォルダー、インテグレーター、コントローラー、パラメーター、エラー、ヘルパー）。
- カタカナ複合語は中黒なしで続けるか半角スペースで区切ります（例: 「ランタイム操作」「ネイティブ ブリッジ」は「ネイティブブリッジ」に統一）。本用語集では区切りなしを標準とします。
- UI 文字列（`End session`、`Session is running` など）は製品が ja-JP に翻訳していないため英語のインラインコードのまま残し、意味を周囲の日本語で説明します。訳語を製品表示であるかのように「」やコードで示してはいけません（例:「`End session`（セッションを終了する項目）」は可、「`セッションの終了`」は不可）。
- 強さの修飾語（`UNAVAILABLE`、`PARTIAL`、「ランタイム検証されていない」「オフラインのみ」「未実装」）は弱めず強めず訳します。

## 用語表

| English | ja-JP | note |
| --- | --- | --- |
| session | セッション | |
| session owner / owner | セッションオーナー / オーナー | 所有プロセスの意。「所有者」は人を連想させるため使わない |
| client (mode) | クライアント（モード） | |
| owner process | オーナープロセス | |
| launch | 起動（名詞）/ 起動する | launch spec は `LaunchSpec` のまま。「ランチ」は不可 |
| plan (noun/verb) | プラン（名詞）/ プランを作成する・プランニングする（動詞） | 「計画」も可だが、`SessionPlan` との対応上「プラン」に統一 |
| runnable / not runnable | 実行可能 / 実行不可 | 「実行不可のプラン」 |
| start | 開始（セッション）/ 起動（OMSI・プロセス） | start session = セッションを開始する |
| stop | 停止 | |
| end session | セッションの終了 / セッションを終了する | UI 項目 `End session` はコードのまま |
| canonical stop | 正規の停止経路 / 正規停止 | canonical owner stop path = オーナーの正規停止経路 |
| restore | 復元 | |
| recovery | リカバリ | 「回復」は使わない。recover = リカバリする |
| pending (journal) | 保留中の（ジャーナル） | pending journal = 保留中のジャーナル |
| journal | ジャーナル | `journal.json` はコードのまま |
| transaction | トランザクション | |
| overlay | オーバーレイ | 英語由来の技術用語として定着 |
| backup | バックアップ | |
| snapshot | スナップショット | status window の snapshot も同じ |
| installation | インストール環境 / OMSI インストール | 名詞として OMSI のインストール先一式を指す。「インストール作業」と区別 |
| installation root | インストールルート | |
| package | パッケージ | |
| release package | リリースパッケージ | |
| manifest | マニフェスト | |
| permanent plugin | 常駐プラグイン | 「永続プラグイン」は使わない。常にインストールされたまま残るプラグインの意 |
| native bridge | ネイティブブリッジ | |
| runtime | ランタイム | |
| runtime operation | ランタイム操作 | operation id はコードのまま |
| runtime command | ランタイムコマンド | |
| runtime slot / mailbox | ランタイムスロット / メールボックス | 共有メモリ領域。64 KiB の上限 |
| control plane | コントロールプレーン | local control plane = ローカルコントロールプレーン |
| local control | ローカル制御 | ページ名 local control も「ローカル制御」 |
| named pipe | 名前付きパイプ | |
| frame (protocol) | フレーム | |
| envelope (JSON) | エンベロープ | JSON エンベロープ |
| handle | ハンドル | |
| stale handle | 失効したハンドル | 「古いハンドル」は不可 |
| capability | ケイパビリティ | 「機能」は feature と紛れるため使わない。capability id はコードのまま |
| capability registry | ケイパビリティレジストリ | |
| bounded list | 上限付きリスト | BUG-05 の意味を厳密に保持 |
| truncated | 切り詰められた | `truncated=true` はコードのまま。「行が省略された」と説明 |
| row | 行 | |
| evidence | エビデンス / 検証根拠 | evidence id（`T01` など）はそのまま |
| runtime-validated | ランタイム検証済み | not runtime validated = ランタイム検証されていない |
| statically validated | 静的検証済み | |
| offline test | オフラインテスト | offline only = オフラインのみ |
| gate (documentation gate) | ゲート（ドキュメントゲート） | |
| tray icon | トレイアイコン | |
| notification area | 通知領域 | Windows 公式用語 |
| status window | ステータスウィンドウ | |
| confirmation dialog | 確認ダイアログ | |
| message box | メッセージボックス | |
| failure dialog | エラーダイアログ | 失敗を通知するダイアログ |
| tooltip | ツールチップ | |
| context menu | コンテキストメニュー | |
| Explorer restart | エクスプローラーの再起動 | |
| entry point | エントリポイント | OMSI の開始地点。`/entrypoint` はコードのまま |
| new map | 新規マップ | NEW_MAP はトークンのまま |
| saved situation | 保存済みシチュエーション | SAVED_SITUATION はトークンのまま。`.osn` |
| map | マップ | |
| splash screen | スプラッシュ画面 | |
| Internet Textures | Internet Textures | OMSI の機能名として英語のまま |
| session profile | セッションプロファイル | |
| preset | プリセット | |
| setting | 設定 / 設定項目 | setting key = 設定キー |
| player vehicle | プレイヤー車両 | |
| road vehicle | 道路車両 | AI 交通車両を含む。`RoadVehicle` はコードのまま |
| human (pedestrian/passenger object) | 人物オブジェクト（歩行者・乗客） | `Human` はコードのまま |
| timetable | 時刻表 | |
| track entry | 路線エントリ（track entry） | 時刻表の track 項目。初出で英語併記 |
| tour entry | ツアーエントリ（tour entry） | 時刻表のツアー（運行）項目。初出で英語併記 |
| ticket | 乗車券 | |
| driver | 運転士 | OMSI のドライバー（運転手プロファイル）。デバイスドライバーとは区別 |
| fleet number | 車両番号 | |
| registration (plate) | ナンバープレート / 登録番号 | |
| repaint | リペイント | |
| spawn | スポーン / 出現させる | |
| camera lock | カメラロック | `camera.lock` はコードのまま |
| device reset | デバイスリセット | D3D デバイス |
| render thread | レンダースレッド | |
| texture | テクスチャ | |
| exit code | 終了コード | |
| error code | エラーコード | |
| diagnostic | 診断 / 診断情報 | plan diagnostic = プラン診断 |
| diagnostics directory | 診断ディレクトリ | |
| timeout | タイムアウト | |
| placeholder | プレースホルダー | |
| flag | フラグ | CLI のスイッチ（`/silent` など） |
| route | ルート | CLI/API のコマンドルート。バス路線の意味ではない |
| command word | コマンドワード | |
| integrator | インテグレーター | OmsiLaunch を組み込む開発者 |
| caller | 呼び出し元 | |
| known limitation | 既知の制限事項 | |
| accepted risk | 受容済みリスク | |
| stable beta | 安定ベータ | `STABLE_BETA` はトークンのまま |
| experimental | 試験的 | `EXPERIMENTAL` はトークンのまま |
| partial | 部分的 | `PARTIAL` はトークンのまま |
| unavailable | 利用不可 | `UNAVAILABLE` はトークンのまま |
| deprecated/legacy | 非推奨 / レガシー | |
| not normative | 規範的ではない / 参考情報 | 英語版が規範（normative） |
| source of truth | 唯一の正とする情報源 | 「英語版が唯一の正です」 |

## 補足訳語

| English | ja-JP | note |
| --- | --- | --- |
| lease (installation lease) | リース（インストールリース） | |
| handoff | ハンドオフ | |
| telemetry | テレメトリ | |
| supervisor | スーパーバイザー | |
| unsupported | 非対応 | 「壊れている」「不具合」と混同しない（Steam LAA は `PARTIAL`） |
| fixed / defect | 修正済み / 不具合 | RV-002 には使わない（オフラインでカバー） |
| content catalogue | コンテンツカタログ | |
| weather | 天候 | |
