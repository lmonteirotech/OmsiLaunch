# LaunchSpec reference

This page is the normative reference for `LaunchSpec`, the request record that describes one OmsiLaunch session: its C# shape (`OmsiLaunch.Api`), its JSON file form as loaded by the CLI (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), every property with its type, default, validation rule and current effect, the validation rules that make a plan non-runnable, and the precedence between CLI flags, spec files and session profiles. Only what the current code does is documented.

Related pages: [public API](public-api.md), [CLI reference](cli.md), [session profiles](session-profiles.md), [error codes](errors.md), [session lifecycle](../concepts/session-lifecycle.md), [capabilities](capabilities.md).

## Where a LaunchSpec comes from

| Source | How it becomes a `LaunchSpec` |
| --- | --- |
| API | The integrator constructs the record and passes it to `PlanSessionAsync`. |
| CLI flags | `CliInput.BuildSpecAsync` starts from built-in defaults (NEW_MAP, everything unset, `Behavior` defaults) and applies flags. |
| `/spec:<path>` JSON file | Loaded by `LaunchSpecJson.LoadAsync`, then used as the seed that CLI flags override (see [precedence](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Session profile (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` writes the profile's world, settings, presentation, internet-textures and behaviour into the seed and records `SessionProfile` metadata. |

The [complete example](#complete-example) below is verified against the record shape. A copy of the minimal example ships as `examples/release-session.example.json` (and `.omsilaunch\examples\release-session.example.json` in the release package).

## JSON form

| Rule | Detail |
| --- | --- |
| Serializer | `System.Text.Json` with `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; no converters are registered. |
| Property names | The C# property names (`Installation`, `RootPath`, ...). Matching is case-insensitive on load; the CLI writes them in PascalCase. |
| Enums | Integers (there is no string-enum converter). `"Mode": 0` is valid; `"Mode": "NewMap"` is rejected as malformed JSON. Values are listed in [Enumerations](#enumerations). |
| `OptionalValue<T>` | An object `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (the value is ignored), 1 = `Set` (the value must be present and non-null; a `Set` with a null value is not validated and behaves as an invalid value). An omitted `OptionalValue` member is `Unset`. The read-only `IsSet` member appears in output written by the CLI and is accepted and ignored on load. |
| Optional records | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile` may be `null` or omitted; the `Effective*` accessors substitute defaults. |
| Required records | `Installation`, `World`, `Date`, `Time`, `Environment` (with all eight dictionaries, use `{}`), `Behavior` must be present objects. They are not validated: a `null` or missing one fails later with a null reference, which the CLI reports as `OL_E_INTERNAL` (exit 10) or `OL_E_INVALID_ARGUMENT` (exit 2). |
| Unknown properties | Rejected before binding: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (the path uses the member names as written in the file). Dictionary contents (`Environment.*`) are not checked as properties. |
| Root | Must be a JSON object: `OL_E_SPEC_INVALID`. Maximum nesting depth 32. |
| File size | At most 1 MiB (1 048 576 bytes): `OL_E_SPEC_TOO_LARGE`. Missing file: `OL_E_SPEC_NOT_FOUND`. |
| Comments and trailing commas | `//` and `/* */` comments and trailing commas are accepted. |
| Malformed JSON | The parser exception is not translated: the CLI reports `OL_E_INTERNAL` with exit code 10. |
| Encoding | UTF-8 (a BOM is tolerated by the reader). Backslashes in identities must be escaped (`"maps\\Grundorf\\global.cfg"`); forward slashes are accepted for map, situation, vehicle and HOF identities. |

## Complete example

```jsonc
{
  // Comments and trailing commas are accepted. Enums are integers.
  "Installation": {
    "RootPath": ".",                          // "." = directory that contains OmsiLaunch.exe (CLI only)
    "ExpectedExecutableSha256": null          // carried, not consumed
  },
  "World": {
    "Mode": 0,                                // 0 NewMap, 1 SavedSituation, 2 LastMapState (unavailable)
    "MapIdentity": { "Presence": 1, "Value": "maps\\Grundorf\\global.cfg" },
    "SituationIdentity": { "Presence": 0, "Value": null },
    "PresentedEntrypointIndex": { "Presence": 1, "Value": 1 },
    "EntrypointIdentity": { "Presence": 0, "Value": null }
  },
  "Date": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Time": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Year": null,
  "Weather": null,
  "PlayerVehicle": { "Presence": 0, "Value": null },
  "Environment": {
    "General": {
      "traffic.randomVehicles": { "Presence": 1, "Value": "150" },
      "graphics.maxFPS": { "Presence": 1, "Value": "60" }
    },
    "Advanced": {}, "Graphics": {}, "AdvancedGraphics": {},
    "Sound": {}, "AiPassengers": {}, "Keyboard": {}, "Controllers": {}
  },
  "Behavior": {
    "RestoreConfiguration": true,             // carried, restore always happens
    "SuppressStaleClosecheckWarning": true,
    "StartupTimeoutSeconds": 180,             // 1..600
    "ShutdownTimeoutSeconds": 30              // carried, not consumed
  },
  "Input": null,
  "Diagnostics": null,
  "Presentation": {
    "Splash": 1,                              // 0 Unset/Native (keep OMSI files), 1 Managed
    "Language": { "Presence": 0, "Value": null },
    "CustomAssetDirectory": { "Presence": 0, "Value": null },
    "SuppressTrayIcon": false
  },
  "InternetTextures": {
    "Mode": 0,                                // 0 Native, 1 Disabled, 2 Override
    "OverrideProfilePath": { "Presence": 0, "Value": null }
  },
  "SessionProfile": null
}
```

An explicit date, when the build supports it, is written as `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` and a time as `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. On this build both make the plan non-runnable (see below).

## Property reference

Column "Consumed" states what the current code does with the value. Stability uses the vocabulary of the [public API page](public-api.md#stability-vocabulary).

### `LaunchSpec` (root)

| Property | JSON type | Required | Default when omitted | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Installation` | `InstallationSpec` object | yes | none | yes | `STABLE_BETA` |
| `World` | `WorldSpec` object | yes | none | yes | `STABLE_BETA` |
| `Date` | `DateSpec` object | yes | none | validated; any mode but `Unset` is non-runnable | `PARTIAL` |
| `Time` | `TimeSpec` object | yes | none | validated; any mode but `Unset` is non-runnable | `PARTIAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | no | `Unset` | resolved for diagnostics; any set field is non-runnable | `PARTIAL` |
| `Environment` | `EnvironmentSpec` object | yes | none | yes (semantic `options.cfg` overlay) | `STABLE_BETA` |
| `Behavior` | `LaunchBehaviorSpec` object | yes | none | partly (see record) | `STABLE_BETA` / `PARTIAL` |
| `Year` | `YearSpec` object or null | no | `null` → `EffectiveYear` = mode `Unset` | any mode but `Unset` is non-runnable | `PARTIAL` |
| `Weather` | `WeatherSpec` object or null | no | `null` → `EffectiveWeather` = mode `Unset` | any mode but `Unset` is non-runnable | `PARTIAL` |
| `Input` | `InputSpec` object or null | no | `null` → `EffectiveInput` = both unset | any set document is non-runnable | `PARTIAL` |
| `Diagnostics` | `DiagnosticsSpec` object or null | no | `null` → `EffectiveDiagnostics` = defaults | carried only | `PARTIAL` |
| `Presentation` | `SessionPresentationSpec` object or null | no | `null` → `EffectivePresentation` = managed splash, no language, no custom directory, tray shown | yes | `STABLE_BETA` |
| `InternetTextures` | `InternetTexturesSpec` object or null | no | `null` → `EffectiveInternetTextures` = `Native` | yes | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | `SessionProfileMetadata` object or null | no | `null` | provenance only (`session_profile.selected` plan diagnostic) | `STABLE_BETA` |

Read-only accessors (present in CLI JSON output, ignored on load): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`.

### `InstallationSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | required | Directory containing `Omsi.exe` and `plugins\`. See [path rules](#path-rules). Empty/whitespace → `OL_E_INSTALLATION_NOT_FOUND`. | yes | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string or null | `null` | Any string. | No consumer in the current code: the host always hashes `Omsi.exe` and compares it with the build profile, never with this value. | `PARTIAL` (carried, currently no effect) |

### `WorldSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | required | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` is an obsolete alias with the same value 2). | yes; `LastMapState` → `OL_E_CAPABILITY_UNAVAILABLE` | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | For `NewMap`: required, of the form `maps\<dir>\global.cfg` (case-insensitive, `/` accepted, no `..`) and installed. Ignored for `SavedSituation` (the `.osn` provides the map). | yes (handoff) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | For `SavedSituation`: required, an installed `situations\...\<file>.osn` identity (as returned by `DiscoverAsync(Situations)` / `/list:situations`). | yes (handoff) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | For `NewMap` without `EntrypointIdentity`: required, `>= 0`, an index into OMSI's presented entrypoint list for the map. Sent to the plugin as `-1` when unset. | yes (handoff) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | A raw entrypoint label or discovery identity. Setting it makes the plan non-runnable (`world.entrypoint-identity`, `RUNTIME_PARTIAL`, `OL_E_CAPABILITY_UNAVAILABLE`). | carried | `PARTIAL` |
| `Entrypoint` | `EntrypointSpec` (read-only) | computed | `Mode` = `Identity` when `EntrypointIdentity` is set, else `PresentedIndex` when the index is set, else `Unset`; `PresentedIndex`, `Identity` mirror the inputs. | derived | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | required (`Year`: `0` when the record is null) | `0` `Unset`, `1` `Explicit`, `2` `System`. | `Explicit`/`System` → `unsupported` entry (`world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `STATICALLY_PARTIAL`) and `OL_E_CAPABILITY_UNAVAILABLE`. The modes are also copied into the startup handoff, which the plugin rejects when not `Unset` (never reached because the plan is non-runnable). | `PARTIAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: `Year`, `Month` 1..12, `Day` 1..31; `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59. Must be set when `Mode` is `Explicit` (`OL_E_DATE_TIME_APPLY_FAILED` otherwise) and must be unset when `Mode` is not `Explicit` (`OL_E_INVALID_ARGUMENT`). `YearSpec.Value` is not validated. | validated only | `PARTIAL` |

### `WeatherSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0` when the record is null | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Any mode but `Unset` → `weather` unsupported entry and `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Preset name (not validated). | carried | `PARTIAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | ICAO code (not validated). | carried | `PARTIAL` |

### `PlayerVehicleSpec` (inside `PlayerVehicle`)

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Installed `Vehicles\...\<file>.bus` identity, else `OL_E_VEHICLE_NOT_FOUND`. | resolved into `ResolvedContent`; then `player-vehicle.model` unsupported → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | A repaint identity of `Model` (`<cti>#item:<n>`), else `OL_E_REPAINT_NOT_FOUND`; only checked when `Model` is set. | same | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | Installed `Vehicles\...\<file>.hof`, else `OL_E_HOF_NOT_FOUND`. | same | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Any string. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Any string. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (read-only) | computed | `true` when `Model` is set. `PlayerVehicle.IsSet` is what the handoff carries as `PlayerVehicleEnabled`. | derived | `PARTIAL` |

A `PlayerVehicle` with `Presence` 1 and all fields unset is accepted and has no effect. Any set field makes the plan non-runnable on this build (`STATICALLY_PARTIAL`).

### `EnvironmentSpec`

| Property | Type | Default | Consumed | Stability |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | `IReadOnlyDictionary<string, OptionalValue<string>>` each, required (`{}` when empty) | none | yes | `STABLE_BETA` |

The eight groups are concatenated; the group a key is placed in has no effect. Each entry with `Presence` 1 is a semantic setting from `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); the key selects the target file (`options.cfg` for every current key) and token. Planning checks that the key exists (`OL_E_UNKNOWN_SETTING`) and is writable (`OL_E_SETTING_NOT_WRITABLE`); the value is validated only at start (`OL_E_INVALID_SETTING_VALUE`, reported as a `Failed` session with `OL_E_START_SESSION`). Keys are case-insensitive. Unset entries are ignored. The CLI `/set:<key>=<value>` flag writes into `General`; session-profile `settings` are merged into `General` as well.

| Key | Value | Notes |
| --- | --- | --- |
| `general.language` | string | `[language]` token |
| `general.radio` | string | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | presence tokens (`autoSave` is the inverse of `noAutoSave`) |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | integer 10..200 | |
| `graphics.tileDistance` | integer 1..20 | |
| `graphics.maxObjectDistanceMeters` | number 20..5000 | |
| `graphics.minObjectScreenPercent` | number 0..10 | stored divided by 100 |
| `graphics.minReflectionObjectScreenPercent` | number 0..50 | stored divided by 100 |
| `graphics.maxObjectComplexity` | integer 0..3 | |
| `graphics.maxMapComplexity` | integer 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | presence tokens |
| `graphics.stencilShadows` | `true` / `false` | written as `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | one `smokesystems` block |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | presence tokens |
| `simulation.ticketSelling` | integer 0..2 | |
| `simulation.maintenance` | integer 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | two OMSI tokens at once (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | presence tokens |
| `traffic.randomVehicles` | integer 0..1000 | component 0 of the multiline `AIMaxCountRandom` block (runtime validated, matrix RV-005) |
| `traffic.humans` | integer 0..1000 | component 1 of `AIMaxCountRandom` |
| `traffic.factorPercent` | number 1..300 | |
| `traffic.parkedVehiclesPercent` | number 0..100 | |
| `traffic.scheduledVehicles` | number 0..1000 | |
| `traffic.scheduledLinePriority` | number 1..4 | |
| `traffic.passengerFactorPercent` | number 0..200 | |
| `sound.stereo` | number 0..100 | |
| `sound.maxSimultaneousSounds` | number 5..1000 | |
| `sound.masterVolume` | number 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | rejected | known but not writable → `OL_E_SETTING_NOT_WRITABLE` |

Patched files keep their encoding (Windows-1252 bytes preserved; BOM-marked UTF-8/UTF-16 respected) and line endings.

### `LaunchBehaviorSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | any | No consumer: session-owned files are always restored exactly. | `PARTIAL` (carried, currently no effect) |
| `SuppressStaleClosecheckWarning` | bool | `true` | any | `true`: a `closecheck` file that exists before the session is permanently removed at start (diagnostic `closecheck.stale-removed` with its SHA-256; failure `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: an existing `closecheck` is left alone and is not a session deletion. The `closecheck` OMSI writes during the session is always removed at restore. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (`ArgumentOutOfRangeException` from `StartSessionAsync` otherwise; CLI `/startup-timeout` enforces 1..600; profiles require > 0). Time budget from supervisor start to `Running`; on expiry the session fails with `OL_E_STARTUP_TIMEOUT` (plugin started) or `OL_E_PLUGIN_NOT_LOADED`. | yes | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | any int (CLI `/shutdown-timeout` 1..600) | No consumer: the supervisor terminates OMSI immediately with `TerminateProcess`; there is no cooperative shutdown wait. | `PARTIAL` (carried, currently no effect) |

### `InputSpec`

| Property | Type | Default | Consumed | Stability |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Set → `input.keyboard` unsupported (`STATICALLY_PARTIAL`) and `OL_E_CAPABILITY_UNAVAILABLE`. Keyboard PATCH/REPLACE execution is not implemented. | `PARTIAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Set → `input.controller` unsupported and `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |

### `DiagnosticsSpec`

| Property | Type | Default | Consumed | Stability |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | No consumer in `src/`. The host trace `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` is always written. | `PARTIAL` (carried, currently no effect) |
| `Verbose` | bool | `false` | No consumer. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | No consumer. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | No consumer. | `PARTIAL` |
| `PluginTrace` | bool | `false` | No consumer. | `PARTIAL` |
| `NativeTrace` | bool | `false` | No consumer. | `PARTIAL` |

The CLI flags `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` populate these booleans (`/logall` sets `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`); they are ORed with the spec values.

### `SessionPresentationSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (alias `Native`): OMSI's own splash files are untouched. `1` `Managed`: OmsiLaunch overlays `GUI\NewSplashscreen_ENG.bmp` and `GUI\NewSplashscreen_<LANG>.bmp` for the session (restored exactly afterwards). | yes | `STABLE_BETA` (matrix RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (case-insensitive); any other value normalizes to `ENG`. When unset, the `[language]` value of `options.cfg` is read and normalized the same way. | yes (managed splash only) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Directory containing `ENG.bmp` and `<LANG>.bmp` (640×480, 24-bit BMP). See [path rules](#path-rules). Missing directory: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; missing file: `OL_E_SPLASH_ASSET_MISSING`; wrong format: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. When unset, `<root>\.omsilaunch\assets\splash` is used (seeded once from the package), else the packaged `assets\splash`. | yes (managed splash only) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` suppresses the standalone Windows tray indicator of the CLI owner. | CLI owner only; the API has no tray. There is no CLI flag; it can only come from a spec file. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native`: nothing changes. `1` `Disabled`: the plugin suppresses OMSI's in-process downloader (telemetry `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: the `.itx` profile is overlaid as `Texture\standard.itx`; its target files and `Texture\standard.ipr` become session deletions. | yes | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Required for `Override` (`OL_E_ITX_PROFILE_REQUIRED`). A text file with pairs of lines: absolute `http`/`https` URL, then a target path relative to the installation root that contains a `Texture\` component, is not rooted, has no `..`, does not start with `\` and traverses no junction/symlink (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). See [path rules](#path-rules). | yes | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Property | Type | Consumed | Stability |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | strings / int | Recorded in the plan diagnostic `session_profile.selected` (`Data["session_profile.*"]`). Not otherwise consumed; normally filled by the session-profile compiler, not by hand. | `STABLE_BETA` |

## Enumerations

| Enum | Values (JSON integer) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (obsolete alias; it is OMSI's native last-map-state branch, never the newest `.osn`) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (read-only `Entrypoint.Mode`) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Integers outside the declared range are stored as-is by the serializer and behave as unknown values (for example an unknown `WorldMode` is neither NEW_MAP nor SAVED_SITUATION and yields a plan with no world capability; the plugin would reject it, but the CLI replaces the mode anyway, see precedence).

## Validation rules and non-runnable diagnostics

`PlanSessionAsync` runs `LaunchValidation.Validate` and then `SessionPlanner.PlanAsync`. A plan is runnable exactly when no diagnostic code starts with `OL_E_`. The complete set:

| Diagnostic | Condition | Source |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` empty or whitespace | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` without a set value or with month/day out of range; `Time.Mode` = `Explicit` without a set value or with hour/minute/second out of range | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` or `Time.Value` set while the mode is not `Explicit` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` with `MapIdentity` unset or not of the form `maps\...\global.cfg` (validation); `NewMap` with an identity that is not installed (planner) | both |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` without `EntrypointIdentity` and with `PresentedEntrypointIndex` unset or negative | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, map installed, no `EntrypointIdentity`, `PresentedEntrypointIndex` unset (`world.presented-entrypoint` unavailable) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` without `SituationIdentity` (validation) or with an identity that is not installed (planner) | both |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: the map named inside the `.osn` is not installed | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | not Windows 10+ on an x64 OS with an x64 host process (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | root directory missing, read-only attribute set, or no `plugins\` subdirectory (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | `Omsi.exe` missing, or its size/SHA-256 is neither the profile fingerprint (`692EBFBF...`, 8 503 440 bytes) nor an allow-listed hash (`omsi.profile.OMSI23004`) | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `World.Mode` = `LastMapState`; `EntrypointIdentity` set; `Date`/`Time`/`Year` mode not `Unset`; `Weather` mode not `Unset`; any `PlayerVehicle` field set; `Input.KeyboardDocument` or `Input.ControllerDocument` set | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` not installed (in addition to `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | an `Environment` key that is not in the catalog / not writable | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | building the splash/ITX plan threw; the message carries `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` or `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | the plugin closure reference (`OmsiLaunchRuntimePaths`) or the release manifest cannot be loaded (message may carry `OL_E_RELEASE_MANIFEST_INVALID`) | `OmsiLaunchService.PlanSessionAsync` |

Not validated at plan time (fails at start as a `Failed` session with `OL_E_START_SESSION`): setting values (`OL_E_INVALID_SETTING_VALUE`), permanent plugin integrity (`OL_E_PERMANENT_PLUGIN_*`), lease availability (`OL_E_INSTALLATION_BUSY`), `StartupTimeoutSeconds` range (thrown by `StartSessionAsync`).

Informational plan diagnostics: `plugin.integrity.reference` (message `manifest` or `self`), `session_profile.selected`.

## Precedence: CLI flags vs spec file vs session profile

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) builds the effective spec in this order:

1. Seed = built-in defaults, or the `/spec` file when given.
2. Installation root = the explicit installation argument if given, else the seed's `RootPath`; then `.`/empty → executable directory, `Path.GetFullPath`. An explicit installation argument always wins over the spec's `RootPath`.
3. Session profile (`/predefined-profile` + `/predefined-profile-index`): explicit CLI arguments that touch a profile-owned field are rejected with `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (world fields only in NEW_MAP mode; `/set` keys present in the preset; splash flags when the preset has `presentation`; internet-texture flags when it has `internet-textures`; timeouts when it has `behavior`). The seed's `World` is replaced by a fresh one (only the CLI world mode survives), then the profile's `new:` block (NEW_MAP only), `settings` (into `General`), `presentation`, `internet-textures`, `behavior` and `SessionProfile` metadata are applied. `compatibility.maps` is enforced for NEW_MAP and SAVED_SITUATION.
4. World: the CLI world mode always wins (`/new` default, `/saved:<osn>`, `/last`); the spec file's `World.Mode` is replaced. To run a saved situation from a spec, pass `/saved:`. `/map` and `/entrypoint`/`/entrypoint-index` override the seed; a CLI `/entrypoint` identity clears the index; `/saved` with `/map` or entrypoint flags is `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` override the seed when given (`system` selects `DateTimeMode.System`).
6. `/no-vehicle` clears `PlayerVehicle`; individual `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` override individual fields of the seed's player vehicle.
7. `/set:<key>=<value>` entries are added to `Environment.General` (key checked, value not); the other seven groups come from the seed unchanged.
8. `/startup-timeout` and `/shutdown-timeout` override the seed only when given; otherwise spec, then profile, then defaults 180 s / 30 s apply. `ShutdownTimeoutSeconds` is `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` on the supervisor.
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` override the seed when given; `SuppressTrayIcon` comes from the seed only.
10. Diagnostics flags are ORed with the seed.

Result: explicit CLI flag > session profile > spec file > built-in default, except that a CLI flag conflicting with a profile-owned field is an error rather than an override.

## Path rules

| Path | API behaviour | CLI behaviour |
| --- | --- | --- |
| `Installation.RootPath` | Used as given: relative paths resolve against the process working directory in file operations. Pass an absolute path. The lease, journal and content catalog normalize it with `Path.GetFullPath`. | `.` or empty = the directory that contains `OmsiLaunch.exe`, never the caller's working folder; an explicit installation argument wins over the spec; the result is made absolute. |
| `Presentation.CustomAssetDirectory` | Absolute, or relative to `Installation.RootPath`. Must exist. | Same (`/splash-assets`). A session-profile `assets` path is confined to the profile package and stored absolute. |
| `InternetTextures.OverrideProfilePath` | Resolved with `Path.GetFullPath`, i.e. relative to the process working directory, not to the installation root. Must exist. | Same (`/internet-textures-profile`). A session-profile `profile` path is confined to the package and stored absolute. |
| ITX target lines | Relative to the installation root; must contain a `Texture\` component; no root, no `..`, no leading `\`, no junction/symlink component. | Same. |
| Content identities (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Installation-relative, case-insensitive, `/` accepted; never absolute. | Same. |

## Carried but not applied

| Field | Current effect | Stability |
| --- | --- | --- |
| `Installation.ExpectedExecutableSha256` | none (the host hashes `Omsi.exe` against the build profile) | `PARTIAL` |
| `Behavior.RestoreConfiguration` | none (restore always runs) | `PARTIAL` |
| `Behavior.ShutdownTimeoutSeconds` | none (forced termination; `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`) | `PARTIAL` |
| `Diagnostics.*` | none (host trace always written) | `PARTIAL` |
| `Input.KeyboardDocument`, `Input.ControllerDocument` | plan non-runnable when set | `PARTIAL` |
| `Date`, `Time`, `Year` (mode other than `Unset`) | plan non-runnable (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `Weather` (mode other than `Unset`) | plan non-runnable (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `PlayerVehicle.*` (any set field) | content resolved for diagnostics, plan non-runnable (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `World.EntrypointIdentity` | plan non-runnable (`RUNTIME_PARTIAL`) | `PARTIAL` |
| `World.Mode` = `LastMapState` / `LastSituation` | plan non-runnable (`UNSUPPORTED_FOR_CURRENT_PROFILE`) | `UNAVAILABLE` |
| `SessionProfile` | provenance diagnostic only | `STABLE_BETA` |
