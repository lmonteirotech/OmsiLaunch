# ケイパビリティ

<!-- l10n: source=reference/capabilities.md -->
> このページは OmsiLaunch 0.1.0-beta3 の[英語版の原文ページ](../../../reference/capabilities.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページは、OmsiLaunch 0.1.0-beta3 で何ができるかを示す規範的な一覧です。内容は `PublicCapabilityRegistry`（`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`）、プラグイン内のランタイム実装（`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` と `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`）、および `OmsiLaunchService.GetCapabilitiesAsync` から導いています。各ケイパビリティについて、分類、このドキュメント全体で使用する安定性レベル、`Running` セッションが必要かどうか、OMSI を変更するかどうか、引数と結果キー、エラー、ランタイム検証のエビデンスを示します。操作の呼び出し方法（CLI ルート、API エンベロープ、タイムアウト、ハンドル）は[ランタイム制御](runtime-control.md)に、エビデンスの表は[ランタイム検証の状況](../status/runtime-validation-status.md)に記載しています。

<a id="classification-and-stability"></a>
## 分類と安定性

`PublicCapabilityClassification` には 4 つの値があります。これらは次のように安定性の用語に対応し、エビデンスが不完全な箇所では格下げされます。

| 分類 | 意味 | 安定性 |
| --- | --- | --- |
| `PublicStableBeta` | 公開、Beta の契約に含まれる、ランタイム検証済み | `STABLE_BETA`（注記のある箇所では `PARTIAL` に格下げ） |
| `PublicExperimental` | 公開、変更される可能性あり、ランタイム検証済みまたは静的検証済み | `EXPERIMENTAL`（注記のある箇所では `PARTIAL` に格下げ） |
| `InternalOnly` | 調査用のプリミティブ。公開 API や CLI からは到達できません | `INTERNAL` |
| `Unsupported` | 診断のために認識されますが、拒否されるか存在しません | `UNAVAILABLE` |

`PublicCapabilityKind` は `Read`、`Write`、`Action`、`Event` を区別します。すべてのランタイムケイパビリティには、`Running` セッションと完全一致するプロファイル `Omsi23004_692EBFBF` が必要です（レジストリの `RequiresSession` / `RequiresExactProfile`）。セッションを作成するセッションケイパビリティは、完全一致するプロファイルを必要としますが、セッションは必要としません。

ランタイムの結果は文字列の辞書です。`internal_` で始まるキー、または `_address`、`_pointer`、`_vmt` で終わるキーは API 境界で削除され（`ScrubInternalValues`）、ここには記載していません。以下の操作表の結果キーは、製品が返す正確なキーの集合です。これらは実際のセッションですべての公開操作を実行して取得したものです（ランタイムクロージャーのドキュメント用キャプチャ、セッション `f51dcb59-a723-4570-b6c5-b61e628a7993`、`situations\Linie 5.osn`。リストの行は `<row>.<n>.<field>` と表記します）。

操作の失敗の報告方法（`CurrentRuntimeControl.Execute`）:

| 失敗 | `ErrorCode` | `Values` |
| --- | --- | --- |
| レジストリによる拒否（不明な操作または `internal.*` 操作、必須引数の欠落） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | なし |
| 製品による意図的な拒否（`weather.set`） | 固有のコード（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`） | `detail`（文） |
| D3D の失敗 | 固有の `OL_E_D3D_*` コード | `detail`、`native_status` |
| その他のプラグイン側の失敗（失効したハンドル、不明な名前、範囲外の値、プレイヤー車両がない、など） | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`（固有のコードで始まります。例: `OL_E_RUNTIME_OBJECT_HANDLE_STALE` または `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`）、`exception`（.NET の型名） |
| 64 KiB のメールボックスに収まらない結果 | `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（上限付きリストは代わりに短縮されます。[時刻表、運転士、乗車券](#timetable-drivers-tickets)を参照） | なし |

各表の「エラー」列（`Errors`）には固有のコードを記載しています。上記のとおり、`ErrorCode` または `Values.detail` の先頭トークンから読み取ってください。

<a id="session-capabilities"></a>
## セッションケイパビリティ

| ケイパビリティ | 分類 | 安定性 | 種類 | API ルート | CLI ルート | 変更対象 | 検証 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`、`/validate` | なし | RUNTIME_PASS（検証済みのすべてのセッションはプランから始まります） |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | 起動フラグ | ファイルシステム（トランザクション）、プロセス | RUNTIME_PASS（NEW_MAP Grundorf、SAVED_SITUATION Berlin-Spandau） |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | なし | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`、トレイ、Ctrl+C | プロセス（`TerminateProcess`）、ファイルシステム（復元） | RUNTIME_PASS。協調的なシャットダウンは未実装です |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`、`/recover` | ファイルシステム（復元） | RUNTIME_PASS: 早期終了時のリカバリ（RV-008）、オーナーの強制終了と次回開始時の早期リカバリ（`S05`）、復元失敗後の `/recover`（`F01`）、リース保持中・孤立した OMSI がある状態・PID 確定前の期間における拒否（`S04`、`S04b`） |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`、コントロールの `session.events` | `events read`、`events watch` | なし | RUNTIME_PASS。最新値のみを保持するテレメトリスロットのため、バーストを取りこぼすことがあります |

<a id="runtime-capabilities-registry-entries"></a>
## ランタイムケイパビリティ（レジストリエントリ）

| ケイパビリティ | 分類 | 安定性 | 種類 | 操作 | ハンドル | 検証 |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS（セッション `e5454061`） |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS（書き込み、`SetTime`、読み戻し） |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: 常に `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（セッション `50a1f1ec`） |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: 修正後のマップスロットで再検証済み（セッション `9dd62626-94c6-4cd7-bb6f-0288327696f4`、Grundorf）、ドキュメント用キャプチャで Berlin-Spandau でも読み取り |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS（FOV の書き込みと読み戻し） |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`、`camera.unlock` | | 保存済みシチュエーションの PlayerVehicle で RUNTIME_PASS（ランタイムクロージャー `CAM01`、ファミリー 0、2、1 を読み戻し付きで実行し、その後ロック解除）。レジストリ自身の `RuntimeValidation` 文字列は依然として `STATICALLY_VALIDATED` のままです（製品の自己申告であり、このリリースでは更新されていません） |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS。自然な削除後の失効検出（RV-002）には安全なランタイム上の発生手段がなく、オフラインで検証されています |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003（セッション `5f641c8d`、`2 -> 3`、ハンドル `rv-000003`） |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS（コレクション `2 -> 4`） |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: ヘッドレス開始では `present=false`、保存済みシチュエーションでは完全なスナップショット |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` と `timetable.*.list` / `timetable.logs.read` 系 | | RUNTIME_PASS（路線エントリ（track entry）: Grundorf で 91 レコード） |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`、`vehicle.variable.get`、`vehicle.variable.set` | RoadVehicle | RUNTIME_PASS（`Refresh_Strings` 0 -> 1 -> 0） |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`、`vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS（読み取りのみ。書き込みは BI-004） |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`、`vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`、`vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`、`d3d.texture.create`、`d3d.texture.describe`、`d3d.texture.update`、`d3d.texture.release` | D3DTexture | create/describe/update/release、再作成およびセッションをまたいだ解放済み・失効ハンドルの拒否（`H02`）、世代の無効化を伴うデバイスリセット（RV-007、`D01`。`lost` 自体は発生しませんでした）について RUNTIME_PASS |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | 非公開です。`ExecuteRuntimeAsync` はセッションを検索する前に `OL_E_RUNTIME_OPERATION_UNKNOWN` で拒否します。CLI にはこの操作のルートがありません。 |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | なし | | UNSUPPORTED（BI-002） |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | なし | | UNSUPPORTED（BI-007） |

`internal.road-vehicles.make-basic` は `INTERNAL` です。調査用にプラグイン内に存在し（ネイティブアドレスを返します）、API 境界とローカルコントロールプレーンで拒否されます。どちらも最初に操作名を `PublicRuntimeOperationIds` と照合して検証します。

<a id="public-runtime-operations"></a>
## 公開ランタイム操作

以下は、公開フロントエンドが転送してよい操作 id の正確な一覧です（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）。いずれも `SessionState.Running` を必要とし、ジャーナルへの記録も復元も行われません。必須引数はメールボックスを使用する前にレジストリによって検査されます（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）。省略可能な引数はプラグインが検証します。すべての操作に共通する失敗コード: `OL_E_RUNTIME_OPERATION_UNKNOWN`（この一覧にない）、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_RUNTIME_CHANNEL_BUSY`、`OL_E_RUNTIME_CHANNEL_CLOSED`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_REQUEST_ID_REUSED`、`OL_E_RUNTIME_RESPONSE_INVALID`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`（プラグインに実装がない）、`OL_E_RUNTIME_OPERATION_FAILED`（プロセス内の予期しない例外。`detail` と `exception` の値を伴います）。

<a id="time"></a>
### 時刻

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `time.read` | Read | なし | `hour`、`minute`、`second`、`day`、`month`、`year` | |
| `time.set` | Write | 省略可能な `hour`（0..23）、`minute`（0..59）、`second`（0..59.999、小数）。少なくとも 1 つ | プロファイル済みの `SetTime` 実行後の `time.read` のキー | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### 天候

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | なし | `fog_density`、`lightness`、`primary_light_factor`、`secondary_light_factor`、`ambient_light_factor`、`cloud_type`、`cloud_transparency`、`precipitation_set`、`wet_ground`、`wind_speed`、`wind_direction`、`relative_humidity`、`absolute_humidity`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate` | |
| `weather.set` | Write | 任意 | なし | 常に `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（引数が空の場合: `OL_E_RUNTIME_ARGUMENT_REQUIRED`）。OMSI は次の天候ティックで、プロファイル済みの風の候補 2 つの両方を上書きします。 |
| `weather.actual.read` | Read | なし | `active`、`icao`、`last_downloaded`、`invalid_icao`、`counter`、`process` | |

<a id="map"></a>
### マップ

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `map.read` | Read | なし | `loaded`、`loaded_tiles`、`left_hand_traffic`、`name`、`filename`、`friendly_name`、`description`、`max_speed`、`year_start`、`year_end` | |

<a id="camera"></a>
### カメラ

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | なし | `family`（0 運転士、1 乗客、2 外部、3 マップ）、`name`、`field_of_view`、`normal_field_of_view`、`distance` | |
| `camera.set` | Write | 省略可能な `family`（0..3）、`field_of_view`（10..170）。少なくとも 1 つ | `camera.read` のキー | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | 必須の `family`（0..3）。省略可能な `preset`（0..255、ファミリー 0 または 1 の場合のみ） | `locked`（`true`）、`family`、`preset`、`head_look`（`preserved`）、`field_of_view`（`preserved`） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`。このポリシーは `camera.unlock` まで 100 ms ごとに再適用されます。再適用が失敗すると、異なるエラーごとに 1 回ずつイベント `camera.lock.degraded` が発行されます。 |
| `camera.unlock` | Action | なし | `locked`（`false`） | |

<a id="road-vehicles-and-player"></a>
### 道路車両とプレイヤー

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | なし | `count`、`ai_collection`、`player_index`（OMSI の生のプレイヤー車両インデックスを読み取ったまま報告します。プレイヤー車両が存在するかどうかは `player-vehicle.read` の `present` で確認してください） | |
| `road-vehicles.list` | Read | なし | `count`、`vehicle.<n>.handle`（`rv-NNNNNN`） | |
| `road-vehicle.read` | Read | 必須の `handle` | `handle`、`runtime_index`、`tile`、`marked_for_killing`、`traffic_type`、`position_x`、`position_y`、`position_z`、`rotation_x`、`rotation_y`、`rotation_z`、`rotation_w`、`steering`、`tacho`、`ground_speed`、`kilometres`、`throttle`、`brake`、`clutch`、`ai_enabled`、`ai_mode`、`ai_light`、`ai_interior_light`、`ai_indicator_left`、`ai_indicator_right`、`ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | なし | `present`（OMSI にプレイヤー車両がない場合は `false`）。`true` の場合: `player_index` とすべての `road-vehicle.read` のキー | |
| `road-vehicles.spawn` | Action | 必須の `model`（正規形の `Vehicles\...\*.bus`、240 文字以内、`..` を含まない、インストール環境の配下に存在すること） | `bus`、`created_handle`、`before_count`、`after_count`、`delta_count`、`raw_native_return`、`identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`。PlayerVehicle は割り当てません。数秒かかるため、クライアントのタイムアウトは 30 s を使用してください。 |
| `road-vehicles.place-random` | Action | 省略可能な `ai_type`（0..255、既定値 0）、`group`（0..65535、既定値 1）、`type`（-1..65535、既定値 -1）、`scheduled`（0..1、既定値 0）、`tour`（0..65535、既定値 0）、`line`（0..65535、既定値 0） | `raw_return`、`before_count`、`after_count`、`delta_count`、`identity_validation`（`native-placement-return-is-diagnostic-only`） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### 人物オブジェクト

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | なし | `count` | |
| `humans.list` | Read | なし | `count`、`human.<n>.handle`（`hb-NNNNNN`） | |
| `human.read` | Read | 必須の `handle` | `handle`、`runtime_index`、`human_index`、`tile`、`marked_for_killing`、`collision_type`、`position_x`、`position_y`、`position_z`、`target_x`、`target_y`、`target_z`、`target_station`、`pre_target_station`、`departure`、`enter_bus_at`、`seat_bus`、`seat_station`、`ticket_type`、`ticket_index`、`ticket_ready`、`state`、`speed`、`bus_index`、`station`、`ai_mode`、`ai_mode_ex`、`ai_sub_mode`、`collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### 時刻表、運転士、乗車券

上限付きリストの結果には、`count`（OMSI 内の全レコード数）、`returned_count`（この応答に含まれる行数）、`truncated`（行が省略された場合は `true`）が含まれます。行は常に先頭から `returned_count` 件のレコードで、`0` から番号が振られます。行数の上限は、track、trip、line、rv-file、停留所、station link、ツアー、プロファイルが 128、ツアーエントリ（tour entry）が 256、路線エントリ（track entry）が 512 です。この上限で許される行数でも 64 KiB のメールボックスを超える場合（例: Berlin-Spandau の路線エントリとツアーエントリ）、プラグインは応答が収まるまで末尾の行を削除し、小さくなった `returned_count` を `truncated=true` とともに報告します（ドキュメント監査 BUG-05。修正前は、この 2 つのリストが `OL_E_RUNTIME_RESPONSE_TOO_LARGE` で失敗していました）。`timetable.logs.read` は上限付きリストではありません。すべてのログエントリを返すため、数百件のエントリを含むログはメールボックスを超え、`OL_E_RUNTIME_RESPONSE_TOO_LARGE` で失敗します。

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | なし | `invalid`（`true`/`false`）、およびレコード数 `tracks`、`trips`、`bus_stops`、`station_links`、`lines`、`rv_files` | |
| `timetable.tracks.list` | Read | なし | `count`、`returned_count`、`truncated`。`track.<n>.filename`、`track.<n>.path`、`track.<n>.entries`、`track.<n>.length` | |
| `timetable.trips.list` | Read | なし | `count`、`returned_count`、`truncated`。`trip.<n>.filename`、`trip.<n>.chrono_origin`、`trip.<n>.target`、`trip.<n>.line`、`trip.<n>.track_index`、`trip.<n>.track_name`、`trip.<n>.bus_stops`、`trip.<n>.profiles`、`trip.<n>.train_reverse`、`trip.<n>.invalid` | |
| `timetable.lines.list` | Read | なし | `count`、`returned_count`、`truncated`。`line.<n>.name`、`line.<n>.chrono_origin`、`line.<n>.priority`、`line.<n>.tours`、`line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | なし | `count`、`returned_count`、`truncated`。`rv_file.<n>.line`、`rv_file.<n>.number_tours`、`rv_file.<n>.start_date_rel_2000`、`rv_file.<n>.end_date_rel_2000`、`rv_file.<n>.type_line_files`、`rv_file.<n>.type_line_probability`、`rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | なし | `count`、`returned_count`、`truncated`。`track_entry.<n>.track_index`、`track_entry.<n>.id`、`track_entry.<n>.path_index_on_object`、`track_entry.<n>.path_tile`、`track_entry.<n>.path_index`、`track_entry.<n>.relative_distance`、`track_entry.<n>.distance`、`track_entry.<n>.valid`、`track_entry.<n>.path_order_check`、`track_entry.<n>.allowed_fstrn`、`track_entry.<n>.chrono_origin`、`track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | なし | `count`、`returned_count`、`truncated`。`bus_stop.<n>.name`、`bus_stop.<n>.supplement`、`bus_stop.<n>.tile`、`bus_stop.<n>.id`、`bus_stop.<n>.parent_id`、`bus_stop.<n>.preset_alighting`、`bus_stop.<n>.index`、`bus_stop.<n>.starting_links`、`bus_stop.<n>.ending_links`、`bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | なし | `count`、`returned_count`、`truncated`。`station_link.<n>.length`、`station_link.<n>.start_bus_stop_id`、`station_link.<n>.end_bus_stop_id`、`station_link.<n>.start_bus_stop`、`station_link.<n>.end_bus_stop`、`station_link.<n>.chrono_origin`、`station_link.<n>.valid`、`station_link.<n>.visible`、`station_link.<n>.track_entries`、`station_link.<n>.start_track_entry`、`station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | なし | `count`、`returned_count`、`truncated`。`tour.<n>.line_index`、`tour.<n>.name`、`tour.<n>.ai_group`、`tour.<n>.ai_group_index`、`tour.<n>.ai_type`、`tour.<n>.completed_day`、`tour.<n>.entries`、`tour.<n>.has_normal_vehicle`、`tour.<n>.invalid`、`tour.<n>.vehicle_indices`、`tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | なし | `count`、`returned_count`、`truncated`。`profile.<n>.trip_index`、`profile.<n>.name`、`profile.<n>.service_trip`、`profile.<n>.stop_times`、`profile.<n>.total_time`、`profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | なし | `count`、`returned_count`、`truncated`。`tour_entry.<n>.line_index`、`tour_entry.<n>.tour_index`、`tour_entry.<n>.trip`、`tour_entry.<n>.trip_index`、`tour_entry.<n>.profile_index`、`tour_entry.<n>.start_time`、`tour_entry.<n>.end_time`、`tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | なし | `count`。`log.<n>.bus_stop`、`log.<n>.estimated_arrival`、`log.<n>.estimated_departure`、`log.<n>.actual_arrival`、`log.<n>.actual_departure`、`log.<n>.arrival_ok`、`log.<n>.departure_ok`（全エントリ。上限なし） | 長いログでは `OL_E_RUNTIME_RESPONSE_TOO_LARGE` |
| `drivers.read` | Read | なし | `count`、`selected_index`。`driver.<n>.filename`、`driver.<n>.name`、`driver.<n>.gender`、`driver.<n>.bus_stops`、`driver.<n>.crashes`、`driver.<n>.passengers`、`driver.<n>.tickets`、`driver.<n>.cash` | |
| `tickets.read` | Read | なし | `filename`、`voice_path`、`stamper_factor`、`buy_factor`、`chattiness`、`whinge_factor`、`count`。`ticket.<n>.name`、`ticket.<n>.display_name`、`ticket.<n>.value`、`ticket.<n>.maximum_stations`、`ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### 車両スクリプト、定数、カーブ、HOF（ハンドル単位）

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | 必須の `handle` | `handle`、`count`、`returned_count`、`truncated`（上限 512）、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | 必須の `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | 必須の `handle`、`name`、`value`（有限の浮動小数点数） | `handle`、`name`、`requested_value`、`value`（読み戻し値） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | 必須の `handle` | `handle`、`count`、`returned_count`、`truncated`（上限 512）、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | 必須の `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | 必須の `handle` | `handle`、`count`、`name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | 必須の `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | 必須の `handle` | `handle`、`count`、`name.<n>` | |
| `vehicle.curve.evaluate` | Read | 必須の `handle`、`name`、`x`（有限の浮動小数点数。`x` が欠落しているか数値でない場合は `OL_E_RUNTIME_ARGUMENT_REQUIRED`） | `handle`、`name`、`x`、`value`（線形補間） | `OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | 必須の `handle` | `handle`、`count`、`hof.<n>.name`、`hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D 操作は、ネイティブブリッジを介して OMSI のメイン/レンダースレッド上で実行されます。テクスチャハンドルは `d3dtex-<sessionId N>-<16 hex digits>` の形式です。

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | なし | `available`、`native_status`、`query_interface_hresult`、`cooperative_level_hresult`、`execution_thread_id`、`owned_device_references`、`state`（`NOT_READY`、`READY`、`LOST`、`RESETTING`、`STOPPING`、`STOPPED`）、`transition`、`generation`、`live_textures`、`reset_hook_installed`、`last_reset_thread_id`、`binding` | |
| `d3d.texture.create` | Action | 必須の `width`（1..4096）、`height`（1..4096）、`format`（`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）。省略可能な `levels`（0..16、既定値 1） | `handle`、`state`（`LIVE`、`RELEASED`、`STALE`）、`device_state`、`generation`、`width`、`height`、`format`、`levels`、`level`、`level_width`、`level_height`、`hresult`、`execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`、`OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST`、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | 必須の `handle`。省略可能な `level`（0..15、既定値 0） | 要求したレベルについての `d3d.texture.create` の 13 個のキー | `OL_E_D3D_STALE_RESOURCE_HANDLE`、`OL_E_D3D_RESOURCE_RELEASED`、および create のエラー |
| `d3d.texture.update` | Action | 必須の `handle`、`width`（1..4096）、`height`（1..4096）、`pixels_base64`（デコード後 48 KiB 以内）。省略可能な `level`（0..15、既定値 0）、`x`（0..4095、既定値 0）、`y`（0..4095、既定値 0） | 更新後の `d3d.texture.create` の 13 個のキー | `OL_E_D3D_INVALID_PIXEL_BUFFER`、および describe のエラー |
| `d3d.texture.release` | Action | 必須の `handle` | `state` = `RELEASED` となった `d3d.texture.create` の 13 個のキー | 繰り返し解放した場合は `OL_E_D3D_RESOURCE_RELEASED`、`OL_E_D3D_STALE_RESOURCE_HANDLE` |

失敗した D3D 操作は `detail` と `native_status` の値を返します。`D3DRuntimeApi` 拡張メソッド（`GetD3DStatusAsync`、`CreateD3DTextureAsync`、`DescribeD3DTextureAsync`、`UpdateD3DTextureAsync`、`ReleaseD3DTextureAsync`）は、API 利用者向けにこれらの操作をラップしています。

<a id="getcapabilitiesasync-names"></a>
## `GetCapabilitiesAsync` の名前

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` は、ホストから見たインストール環境を記述する `Capability(Name, Available, EvidenceState, Reason)` レコードの静的な一覧を返します。これらの名前は、上記のレジストリ id とは別の、より粒度の粗い用語体系です。操作の契約はレジストリであり、ケイパビリティ一覧はインテグレーター向けの機械可読な要約です。両者の関係は最後の列に示します。

| 名前 | 利用可否 | エビデンス | 関係 |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | プラットフォームに依存 | STATICALLY_VALIDATED | [互換性](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | ランタイムメールボックス |
| `runtime.time.read`、`runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`、`time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`、`runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`、`weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set`（拒否されます） |
| `runtime.camera.read`、`runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`、`camera.set` |
| `runtime.d3d.status`、`runtime.d3d.texture.create`、`runtime.d3d.texture.update`、`runtime.d3d.texture.describe`、`runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007。この一覧は固定の自己申告による一覧です。このエントリは、ランタイムクロージャーのラウンドで resetting/restored の遷移が観測された（`D01`）後も更新されていません。エビデンスは[ランタイム検証の状況](../status/runtime-validation-status.md)を参照してください |
| `runtime.road-vehicles.read`、`runtime.road-vehicles.spawn`、`runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`、`runtime.timetable.read`、`runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`、`timetable.*` |
| `world.new-map`、`world.presented-entrypoint`、`world.saved-situation` | true | RUNTIME_VALIDATED | `session.start` のワールドモード |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState`（`/last`） |
| `world.date.explicit`、`world.date.system`、`world.time.explicit`、`world.time.system` | false | STATICALLY_PARTIAL | `/date`、`/time`、`/year`。これらを要求するとプランは実行不可になります |
| `weather.preset`、`weather.icao`、`weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`。これらを要求するとプランは実行不可になります |
| `player-vehicle.model`、`player-vehicle.repaint`、`player-vehicle.hof`、`player-vehicle.fleet-number`、`player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` と関連フラグ。これらを要求するとプランは実行不可になります |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`、プロファイルの `settings`（RV-005 ランタイム） |
| `input.keyboard.patch`、`input.controller.active-ffscale` | true | STATICALLY_VALIDATED | ドキュメントパーサーは存在しますが、`InputSpec` はセッションによって適用されません（BI-005） |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`、`content.situations`、`content.vehicles`、`content.repaints`、`content.hofs`、`content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`、`/list` |

プランナーは、さらにプランごとのケイパビリティを `SessionPlan.RequiredCapabilities` と `SessionPlan.UnsupportedRequestedFeatures` で報告します（`runtime.current-windows-x64`、`transaction.exact-restore`、`omsi.profile.OMSI23004`、`world.new-map`、`world.presented-entrypoint`、`world.entrypoint-identity`、`world.saved-situation`、`boot.headless-start`、`internet-textures.disabled`、`world.explicit-date`、`world.explicit-time`、`world.explicit-year`、`weather`、`player-vehicle.*`、`input.keyboard`、`input.controller`、`content.*`）。

<a id="known-limitations"></a>
## 既知の制限事項

- ハンドルはセッション単位です。アドレスの再利用はオブジェクトのフィンガープリント（車両では VMT と定義ポインター、人物オブジェクトでは VMT と human インデックス）によって検出され、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` として報告されます。残る死角: 2 回のリスト読み取りの間に、同じクラス・同じ定義のオブジェクトが同じアドレスに再作成された場合、元のオブジェクトと区別できません。
- 結果は 64 KiB のメールボックスに制限されます。上限付きリストは `truncated=true` を付けて切り詰められます。`timetable.logs.read` には上限がなく、`OL_E_RUNTIME_RESPONSE_TOO_LARGE` で失敗することがあります。
- タイルをまたいだ車両の移動、文字列変数の書き込み、カレンダーの書き込み、天候の書き込み、ヘッドレスでの PlayerVehicle の割り当てはいずれもできません。[既知の制限事項](known-limitations.md)を参照してください。
