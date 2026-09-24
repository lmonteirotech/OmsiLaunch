# 互換性

<!-- l10n: source=reference/compatibility.md -->
> このページは OmsiLaunch 0.1.0-beta3 の[英語版の原文ページ](../../../reference/compatibility.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

OmsiLaunch は、ただ 1 つの厳密な実行ファイルビルド内のプロファイル済みアドレスにパッチを適用することで OMSI を制御します。このページでは、どの OMSI ビルドに対応しているか、それ以外のビルドではどうなるか、そしてホストとプラグインのオペレーティングシステムおよびランタイムの要件を示します。ソース: `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`、`src/OmsiLaunch.Core/SessionPlanner.cs`、`src/OmsiLaunch.Process/RuntimePlatform.cs`、`src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`、およびプロジェクトファイル。

<a id="supported-omsi-builds"></a>
## 対応する OMSI ビルド

ビルドプロファイルはただ 1 つ、`Omsi23004_692EBFBF`（ファミリー `OMSI_2_3_004_COMMON`）です。このプロファイルは、厳密な SHA-256 によって 2 つの実行ファイルを受け入れます。

| バリアント | `Omsi.exe` SHA-256 | サイズ | PE ファイルバージョン / 製品バージョン | ステータス |
| --- | --- | --- | --- | --- |
| プロファイル済み実行ファイル（`ALTERNATE_LAA`） | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 バイト | 2.2.032 / 2.3.004 | `STABLE_BETA`。マトリックス内のすべてのランタイム検証はこのファイルで実行されました |
| Steam LAA（`STEAM_LAA`） | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | 検査なし | | プロファイル済みのネイティブレイアウトを共有し、実行ファイルのヘッダーのみが異なるため、許可リストにより受け入れられます。**ランタイム検証されていません**（`profiles` は `runtime_validated=false`、`validation_status=pending_beta_field_validation` を報告します）。`PARTIAL`。 |

`OmsiLaunch.exe profiles` はこの表を JSON として出力します。受け入れの判定にバージョン番号は使用されません。考慮されるのは SHA-256（および、主実行ファイルの場合は厳密なサイズ）のみです。これ以外の OMSI 2 ビルド、パッチを適用した実行ファイル、ハッシュの異なる 4 GB パッチ適用済みのコピーには対応していません。

<a id="what-happens-with-an-unknown-build"></a>
## 不明なビルドの場合の動作

| 段階 | 検査 | 結果 |
| --- | --- | --- |
| プランの作成（`PlanSessionAsync`、`/plan`、`/validate`） | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | 必須ケイパビリティ `omsi.profile.OMSI23004` が `UNAVAILABLE` になります。診断は `OL_E_UNSUPPORTED_BUILD`、`SessionPlan.IsRunnable=false` です。CLI の終了コードは、起動の場合は 1、エラーが例外として伝播した場合は 3（`UnsupportedProfile`）です。 |
| 開始（`StartSessionAsync`） | spec のプランが再作成され、`Omsi.exe` のハッシュが再計算されます | もはや実行可能でなくなったプラン（例: プラン作成後に実行ファイルが変更された、または呼び出し元が `IsRunnable` を編集した）は `OL_E_PLAN_NOT_RUNNABLE` で拒否されます。トランザクションは開始されず、プロセスも起動されません。 |
| プロセス内（`PluginRuntime.Start`） | `NativeServices.ValidateBuild` は、ハンドオフの `BuildProfileId` が `Omsi23004_692EBFBF` であること、**かつ** 実行中のイメージに対して `NativeValidateBuild()` が成功することを要求します | テレメトリ `plugin.build.invalid`。ホストはセッションを `OL_E_BUILD_VALIDATION_FAILED` で失敗させます。ネイティブフックは有効化されず、OMSI は終了させられ、トランザクションは復元されます。 |

実行ファイルのハッシュはプロファイル済みグローバル変数のサイズおよびバイト列と照合されるため、プロセス内の検査は、ハッシュ検査を通過したものの読み込み時のイメージが異なるコピーに対する最後の防御線となります。フォールバックのプロファイルやヒューリスティックな照合はありません。

<a id="operating-system-and-architecture"></a>
## オペレーティングシステムとアーキテクチャ

`CurrentWindowsX64Platform.Detect` が `RuntimePlatformInfo` を算出します。現在のプラットフォームは、次のすべてが満たされる場合にのみ対応しています。

| 要件 | 検査 | 違反時のエラー |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 以降 | `Environment.OSVersion.Version.Major >= 10`（Windows 10、Windows 11、Server 2016 以降） | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64 ビット Windows かつ 64 ビットのホストプロセス | `OSArchitecture == X64` かつ `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| 書き込み可能なインストール環境 | ルートディレクトリが存在し、読み取り専用ではなく、`plugins\` を含むこと | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` はさらに、`OmsiArchitecture` と `PluginArchitecture` を `X86`（OMSI は 32 ビットプロセスであり、プラグインのクロージャは x86 で WOW64 上で実行されます）、`LegacyPlatform=false`、および `Wow64Available` として報告します。ARM64 版 Windows は、x64 エミュレーションが存在する場合でも非対応です。ホストプロセス自体が x64 でなければならないためです。

<a id="net-requirements"></a>
## .NET の要件

| コンポーネント | ランタイム | 注記 |
| --- | --- | --- |
| コントローラー（`OmsiLaunch.exe`、`OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`） | .NET 6、x64 | ネイティブブートストラッパーは、パッケージに含まれる `nethost.dll` を介して `hostfxr` 経由でランタイムを探します。ランタイムがない場合はシムが報告します（終了コード 100-106。[CLI](cli.md)と[終了コード](exit-codes.md)を参照）。 |
| プラグインのクロージャ（`OmsiLaunch.PluginNE.dll` 経由の `plugins\OmsiLaunch.Plugin.dll`） | .NET 6、**x86**（`net6.0-windows`、`win-x86`）、`Omsi.exe` 内で DNNE 2.0.6 によりホスト | マシンに x86 の .NET 6 Desktop/Core ランタイムがインストールされている必要があります。プラグインには 64 ビットのランタイムだけでは不十分です。 |
| ネイティブブリッジ（`plugins\OmsiLaunch.Native.x86.dll`） | ネイティブ x86 | `plugins\` からのみ読み込まれます（[常駐プラグイン](../concepts/permanent-plugin.md)を参照）。 |

<a id="legacy-platforms"></a>
## レガシープラットフォーム

Windows 7、Windows 8.x、Windows XP、およびその他の NT 6 以前のシステムは、現在のサポート範囲外です。`RuntimePlatformInfo.LegacyPlatform` は常に `false` であり、レガシーアダプターは存在しません。このフィールドと `IPluginNativeServices` の接合点は、将来レガシーアダプターを公開 API を変更せずに追加できるようにするためだけに存在します（`docs/adr/ADR-0010-Legacy-Portability-Boundary.md` を参照）。このリリースには、これらのシステムで動作するものは何もありません。

<a id="steam-and-large-address-aware-notes"></a>
## Steam と Large Address Aware に関する注記

- LAA ヘッダー付きの Steam 版 OMSI 2.3.004（`7DAB063D...`）は、プロファイル済みアドレスが主実行ファイルと同一であるため、許可リストに登録されています。フィールド検証セッションがマトリックスに記録されるまでは、そのファイル上のすべてのケイパビリティを `PARTIAL` として扱ってください。
- Steam は OMSI を自ら起動します。ハンドオフが存在するように、セッションは `OmsiLaunch.exe` を通じて開始する必要があります。Steam から起動された場合、常駐プラグインは何も動作しません（ハンドオフなし、フックなし）。
- 別の LAA パッチャーを `Omsi.exe` に適用するとハッシュが変わり、不明なビルドになります。

<a id="related-pages"></a>
## 関連ページ

- [既知の制限事項](known-limitations.md)
- [ランタイム検証ステータス](../status/runtime-validation-status.md)
- [インストール](../getting-started/installation.md)
