# LaunchSpec-Referenz

<!-- l10n: source=reference/launchspec.md -->
> Übersetzung der [englischen Originalseite](../../../reference/launchspec.md) für OmsiLaunch 0.1.0-beta3. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite ist die normative Referenz für `LaunchSpec`, den Anforderungsdatensatz, der eine OmsiLaunch-Sitzung beschreibt: seine C#-Form (`OmsiLaunch.Api`), seine JSON-Dateiform, wie sie von der CLI geladen wird (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), jede Eigenschaft mit Typ, Standardwert, Validierungsregel und aktueller Wirkung, die Validierungsregeln, die einen Plan nicht ausführbar machen, sowie die Rangfolge zwischen CLI-Flags, Spec-Dateien und Sitzungsprofilen. Dokumentiert wird nur, was der aktuelle Code tut.

Verwandte Seiten: [öffentliche API](public-api.md), [CLI-Referenz](cli.md), [Sitzungsprofile](session-profiles.md), [Fehlercodes](errors.md), [Sitzungslebenszyklus](../concepts/session-lifecycle.md), [Capabilities](capabilities.md).

<a id="where-a-launchspec-comes-from"></a>
## Woher eine LaunchSpec stammt

| Quelle | Wie daraus eine `LaunchSpec` wird |
| --- | --- |
| API | Der Integrator erstellt den Datensatz und übergibt ihn an `PlanSessionAsync`. |
| CLI-Flags | `CliInput.BuildSpecAsync` geht von eingebauten Standardwerten aus (NEW_MAP, alles nicht gesetzt, `Behavior`-Standardwerte) und wendet die Flags an. |
| `/spec:<path>`-JSON-Datei | Wird von `LaunchSpecJson.LoadAsync` geladen und dann als Ausgangsbasis verwendet, die CLI-Flags überschreiben (siehe [Rangfolge](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Sitzungsprofil (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` schreibt Welt, Einstellungen, Darstellung, Internettexturen und Verhalten des Profils in die Ausgangsbasis und hält die `SessionProfile`-Metadaten fest. |

Das [vollständige Beispiel](#complete-example) unten ist gegen die Form des Datensatzes geprüft. Eine Kopie des minimalen Beispiels wird als `examples/release-session.example.json` ausgeliefert (und im Release-Paket als `.omsilaunch\examples\release-session.example.json`).

## JSON-Form

| Regel | Detail |
| --- | --- |
| Serializer | `System.Text.Json` mit `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; es sind keine Konverter registriert. |
| Eigenschaftsnamen | Die C#-Eigenschaftsnamen (`Installation`, `RootPath`, ...). Beim Laden wird ohne Beachtung der Groß-/Kleinschreibung verglichen; die CLI schreibt sie in PascalCase. |
| Enums | Ganzzahlen (es gibt keinen String-Enum-Konverter). `"Mode": 0` ist gültig; `"Mode": "NewMap"` wird als fehlerhaftes JSON abgelehnt. Die Werte sind unter [Aufzählungen](#enumerations) aufgeführt. |
| `OptionalValue<T>` | Ein Objekt `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (der Wert wird ignoriert), 1 = `Set` (der Wert muss vorhanden und ungleich null sein; ein `Set` mit null-Wert wird nicht validiert und verhält sich wie ein ungültiger Wert). Ein ausgelassenes `OptionalValue`-Member ist `Unset`. Das schreibgeschützte Member `IsSet` erscheint in der von der CLI geschriebenen Ausgabe und wird beim Laden akzeptiert und ignoriert. |
| Optionale Datensätze | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile` dürfen `null` sein oder fehlen; die `Effective*`-Accessoren setzen Standardwerte ein. |
| Erforderliche Datensätze | `Installation`, `World`, `Date`, `Time`, `Environment` (mit allen acht Dictionaries, verwenden Sie `{}`), `Behavior` müssen als Objekte vorhanden sein. Sie werden nicht validiert: Ein `null`-Wert oder ein fehlender Datensatz scheitert später mit einer Nullreferenz, die die CLI als `OL_E_INTERNAL` (Exitcode 10) oder `OL_E_INVALID_ARGUMENT` (Exitcode 2) meldet. |
| Unbekannte Eigenschaften | Werden vor der Bindung abgelehnt: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (der Pfad verwendet die Member-Namen so, wie sie in der Datei stehen). Dictionary-Inhalte (`Environment.*`) werden nicht als Eigenschaften geprüft. |
| Wurzel | Muss ein JSON-Objekt sein: `OL_E_SPEC_INVALID`. Maximale Verschachtelungstiefe 32. |
| Dateigröße | Höchstens 1 MiB (1 048 576 Bytes): `OL_E_SPEC_TOO_LARGE`. Fehlende Datei: `OL_E_SPEC_NOT_FOUND`. |
| Kommentare und nachgestellte Kommas | `//`- und `/* */`-Kommentare sowie nachgestellte Kommas werden akzeptiert. |
| Fehlerhaftes JSON | Die Parser-Exception wird nicht übersetzt: Die CLI meldet `OL_E_INTERNAL` mit Exitcode 10. |
| Codierung | UTF-8 (ein BOM wird vom Leser toleriert). Backslashes in Identitäten müssen maskiert werden (`"maps\\Grundorf\\global.cfg"`); für Karten-, Situations-, Fahrzeug- und HOF-Identitäten werden Schrägstriche akzeptiert. |

<a id="complete-example"></a>
## Vollständiges Beispiel

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

Ein explizites Datum wird, sofern der Build es unterstützt, als `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` geschrieben und eine Uhrzeit als `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. In diesem Build machen beide den Plan nicht ausführbar (siehe unten).

<a id="property-reference"></a>
## Eigenschaftsreferenz

Die Spalte „Verwendet“ gibt an, was der aktuelle Code mit dem Wert tut. Die Stabilitätsangaben verwenden das Vokabular der [Seite zur öffentlichen API](public-api.md#stability-vocabulary).

<a id="launchspec-root"></a>
### `LaunchSpec` (Wurzel)

| Eigenschaft | JSON-Typ | Erforderlich | Standardwert bei Auslassung | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `Installation` | `InstallationSpec`-Objekt | ja | keiner | ja | `STABLE_BETA` |
| `World` | `WorldSpec`-Objekt | ja | keiner | ja | `STABLE_BETA` |
| `Date` | `DateSpec`-Objekt | ja | keiner | validiert; jeder Modus außer `Unset` ist nicht ausführbar | `PARTIAL` |
| `Time` | `TimeSpec`-Objekt | ja | keiner | validiert; jeder Modus außer `Unset` ist nicht ausführbar | `PARTIAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | nein | `Unset` | für Diagnosen aufgelöst; jedes gesetzte Feld ist nicht ausführbar | `PARTIAL` |
| `Environment` | `EnvironmentSpec`-Objekt | ja | keiner | ja (semantisches `options.cfg`-Overlay) | `STABLE_BETA` |
| `Behavior` | `LaunchBehaviorSpec`-Objekt | ja | keiner | teilweise (siehe Datensatz) | `STABLE_BETA` / `PARTIAL` |
| `Year` | `YearSpec`-Objekt oder null | nein | `null` → `EffectiveYear` = Modus `Unset` | jeder Modus außer `Unset` ist nicht ausführbar | `PARTIAL` |
| `Weather` | `WeatherSpec`-Objekt oder null | nein | `null` → `EffectiveWeather` = Modus `Unset` | jeder Modus außer `Unset` ist nicht ausführbar | `PARTIAL` |
| `Input` | `InputSpec`-Objekt oder null | nein | `null` → `EffectiveInput` = beide nicht gesetzt | jedes gesetzte Dokument ist nicht ausführbar | `PARTIAL` |
| `Diagnostics` | `DiagnosticsSpec`-Objekt oder null | nein | `null` → `EffectiveDiagnostics` = Standardwerte | nur mitgeführt | `PARTIAL` |
| `Presentation` | `SessionPresentationSpec`-Objekt oder null | nein | `null` → `EffectivePresentation` = verwaltetes Startbild, keine Sprache, kein benutzerdefiniertes Verzeichnis, Tray-Symbol angezeigt | ja | `STABLE_BETA` |
| `InternetTextures` | `InternetTexturesSpec`-Objekt oder null | nein | `null` → `EffectiveInternetTextures` = `Native` | ja | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | `SessionProfileMetadata`-Objekt oder null | nein | `null` | nur Herkunftsnachweis (Plandiagnose `session_profile.selected`) | `STABLE_BETA` |

Schreibgeschützte Accessoren (in der JSON-Ausgabe der CLI vorhanden, beim Laden ignoriert): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`.

### `InstallationSpec`

| Eigenschaft | Typ | Standardwert | Gültige Werte | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | erforderlich | Verzeichnis, das `Omsi.exe` und `plugins\` enthält. Siehe [Pfadregeln](#path-rules). Leer/nur Leerraum → `OL_E_INSTALLATION_NOT_FOUND`. | ja | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string oder null | `null` | Beliebiger String. | Im aktuellen Code gibt es keinen Verbraucher: Der Host bildet immer den Hash von `Omsi.exe` und vergleicht ihn mit dem Build-Profil, niemals mit diesem Wert. | `PARTIAL` (mitgeführt, derzeit ohne Wirkung) |

### `WorldSpec`

| Eigenschaft | Typ | Standardwert | Gültige Werte | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | erforderlich | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` ist ein veralteter Alias mit demselben Wert 2). | ja; `LastMapState` → `OL_E_CAPABILITY_UNAVAILABLE` | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | Für `NewMap`: erforderlich, in der Form `maps\<dir>\global.cfg` (ohne Beachtung der Groß-/Kleinschreibung, `/` wird akzeptiert, kein `..`) und installiert. Wird für `SavedSituation` ignoriert (die `.osn` liefert die Karte). | ja (Übergabe) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | Für `SavedSituation`: erforderlich, eine installierte `situations\...\<file>.osn`-Identität (wie von `DiscoverAsync(Situations)` / `/list:situations` zurückgegeben). | ja (Übergabe) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | Für `NewMap` ohne `EntrypointIdentity`: erforderlich, `>= 0`, ein Index in die von OMSI angezeigte Einstiegspunktliste der Karte. Wird an das Plugin als `-1` gesendet, wenn nicht gesetzt. | ja (Übergabe) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | Eine rohe Einstiegspunktbezeichnung oder Discovery-Identität. Wird sie gesetzt, ist der Plan nicht ausführbar (`world.entrypoint-identity`, `RUNTIME_PARTIAL`, `OL_E_CAPABILITY_UNAVAILABLE`). | mitgeführt | `PARTIAL` |
| `Entrypoint` | `EntrypointSpec` (schreibgeschützt) | berechnet | `Mode` = `Identity`, wenn `EntrypointIdentity` gesetzt ist, sonst `PresentedIndex`, wenn der Index gesetzt ist, sonst `Unset`; `PresentedIndex`, `Identity` spiegeln die Eingaben. | abgeleitet | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Eigenschaft | Typ | Standardwert | Gültige Werte | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | erforderlich (`Year`: `0`, wenn der Datensatz null ist) | `0` `Unset`, `1` `Explicit`, `2` `System`. | `Explicit`/`System` → `unsupported`-Eintrag (`world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `STATICALLY_PARTIAL`) und `OL_E_CAPABILITY_UNAVAILABLE`. Die Modi werden außerdem in die Start-Übergabe kopiert, die das Plugin ablehnt, wenn sie nicht `Unset` sind (wird nie erreicht, weil der Plan nicht ausführbar ist). | `PARTIAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: `Year`, `Month` 1..12, `Day` 1..31; `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59. Muss gesetzt sein, wenn `Mode` `Explicit` ist (andernfalls `OL_E_DATE_TIME_APPLY_FAILED`), und darf nicht gesetzt sein, wenn `Mode` nicht `Explicit` ist (`OL_E_INVALID_ARGUMENT`). `YearSpec.Value` wird nicht validiert. | nur validiert | `PARTIAL` |

### `WeatherSpec`

| Eigenschaft | Typ | Standardwert | Gültige Werte | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0`, wenn der Datensatz null ist | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Jeder Modus außer `Unset` → nicht unterstützter `weather`-Eintrag und `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Name der Voreinstellung (nicht validiert). | mitgeführt | `PARTIAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | ICAO-Code (nicht validiert). | mitgeführt | `PARTIAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec` (innerhalb von `PlayerVehicle`)

| Eigenschaft | Typ | Standardwert | Gültige Werte | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Installierte `Vehicles\...\<file>.bus`-Identität, sonst `OL_E_VEHICLE_NOT_FOUND`. | in `ResolvedContent` aufgelöst; danach `player-vehicle.model` nicht unterstützt → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | Eine Lackierungsidentität von `Model` (`<cti>#item:<n>`), sonst `OL_E_REPAINT_NOT_FOUND`; wird nur geprüft, wenn `Model` gesetzt ist. | ebenso | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | Installierte `Vehicles\...\<file>.hof`, sonst `OL_E_HOF_NOT_FOUND`. | ebenso | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Beliebiger String. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Beliebiger String. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (schreibgeschützt) | berechnet | `true`, wenn `Model` gesetzt ist. `PlayerVehicle.IsSet` ist das, was die Übergabe als `PlayerVehicleEnabled` transportiert. | abgeleitet | `PARTIAL` |

Ein `PlayerVehicle` mit `Presence` 1 und lauter nicht gesetzten Feldern wird akzeptiert und hat keine Wirkung. Jedes gesetzte Feld macht den Plan in diesem Build nicht ausführbar (`STATICALLY_PARTIAL`).

### `EnvironmentSpec`

| Eigenschaft | Typ | Standardwert | Verwendet | Stabilität |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | jeweils `IReadOnlyDictionary<string, OptionalValue<string>>`, erforderlich (`{}`, wenn leer) | keiner | ja | `STABLE_BETA` |

Die acht Gruppen werden zusammengefügt; in welcher Gruppe ein Schlüssel steht, hat keine Wirkung. Jeder Eintrag mit `Presence` 1 ist eine semantische Einstellung aus `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); der Schlüssel bestimmt die Zieldatei (`options.cfg` für jeden aktuellen Schlüssel) und das Token. Die Planung prüft, dass der Schlüssel existiert (`OL_E_UNKNOWN_SETTING`) und beschreibbar ist (`OL_E_SETTING_NOT_WRITABLE`); der Wert wird erst beim Start validiert (`OL_E_INVALID_SETTING_VALUE`, gemeldet als `Failed`-Sitzung mit `OL_E_START_SESSION`). Bei Schlüsseln wird Groß-/Kleinschreibung nicht beachtet. Nicht gesetzte Einträge werden ignoriert. Das CLI-Flag `/set:<key>=<value>` schreibt in `General`; die `settings` von Sitzungsprofilen werden ebenfalls in `General` zusammengeführt.

| Schlüssel | Wert | Hinweise |
| --- | --- | --- |
| `general.language` | string | `[language]`-Token |
| `general.radio` | string | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | Präsenz-Tokens (`autoSave` ist die Umkehrung von `noAutoSave`) |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | Ganzzahl 10..200 | |
| `graphics.tileDistance` | Ganzzahl 1..20 | |
| `graphics.maxObjectDistanceMeters` | Zahl 20..5000 | |
| `graphics.minObjectScreenPercent` | Zahl 0..10 | durch 100 geteilt gespeichert |
| `graphics.minReflectionObjectScreenPercent` | Zahl 0..50 | durch 100 geteilt gespeichert |
| `graphics.maxObjectComplexity` | Ganzzahl 0..3 | |
| `graphics.maxMapComplexity` | Ganzzahl 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | Präsenz-Tokens |
| `graphics.stencilShadows` | `true` / `false` | geschrieben als `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | ein `smokesystems`-Block |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | Präsenz-Tokens |
| `simulation.ticketSelling` | Ganzzahl 0..2 | |
| `simulation.maintenance` | Ganzzahl 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | zwei OMSI-Tokens gleichzeitig (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | Präsenz-Tokens |
| `traffic.randomVehicles` | Ganzzahl 0..1000 | Komponente 0 des mehrzeiligen `AIMaxCountRandom`-Blocks (zur Laufzeit validiert, Matrix RV-005) |
| `traffic.humans` | Ganzzahl 0..1000 | Komponente 1 von `AIMaxCountRandom` |
| `traffic.factorPercent` | Zahl 1..300 | |
| `traffic.parkedVehiclesPercent` | Zahl 0..100 | |
| `traffic.scheduledVehicles` | Zahl 0..1000 | |
| `traffic.scheduledLinePriority` | Zahl 1..4 | |
| `traffic.passengerFactorPercent` | Zahl 0..200 | |
| `sound.stereo` | Zahl 0..100 | |
| `sound.maxSimultaneousSounds` | Zahl 5..1000 | |
| `sound.masterVolume` | Zahl 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | abgelehnt | bekannt, aber nicht beschreibbar → `OL_E_SETTING_NOT_WRITABLE` |

Gepatchte Dateien behalten ihre Codierung (Windows-1252-Bytes bleiben erhalten; mit BOM gekennzeichnetes UTF-8/UTF-16 wird berücksichtigt) und ihre Zeilenenden.

### `LaunchBehaviorSpec`

| Eigenschaft | Typ | Standardwert | Gültige Werte | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | beliebig | Kein Verbraucher: Dateien, die der Sitzung gehören, werden immer exakt wiederhergestellt. | `PARTIAL` (mitgeführt, derzeit ohne Wirkung) |
| `SuppressStaleClosecheckWarning` | bool | `true` | beliebig | `true`: Eine `closecheck`-Datei, die vor der Sitzung existiert, wird beim Start dauerhaft entfernt (Diagnose `closecheck.stale-removed` mit ihrem SHA-256; Fehler `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: Eine vorhandene `closecheck` bleibt unangetastet und ist keine Sitzungslöschung. Die `closecheck`, die OMSI während der Sitzung schreibt, wird bei der Wiederherstellung immer entfernt. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (andernfalls `ArgumentOutOfRangeException` aus `StartSessionAsync`; der CLI-Parameter `/startup-timeout` erzwingt 1..600; Profile verlangen > 0). Zeitbudget vom Start des Supervisors bis `Running`; bei Ablauf scheitert die Sitzung mit `OL_E_STARTUP_TIMEOUT` (Plugin gestartet) oder `OL_E_PLUGIN_NOT_LOADED`. | ja | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | beliebiger int (CLI `/shutdown-timeout` 1..600) | Kein Verbraucher: Der Supervisor beendet OMSI sofort mit `TerminateProcess`; es gibt kein kooperatives Warten auf das Herunterfahren. | `PARTIAL` (mitgeführt, derzeit ohne Wirkung) |

### `InputSpec`

| Eigenschaft | Typ | Standardwert | Verwendet | Stabilität |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Gesetzt → `input.keyboard` nicht unterstützt (`STATICALLY_PARTIAL`) und `OL_E_CAPABILITY_UNAVAILABLE`. Die Ausführung von Tastatur-PATCH/REPLACE ist nicht implementiert. | `PARTIAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Gesetzt → `input.controller` nicht unterstützt und `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |

### `DiagnosticsSpec`

| Eigenschaft | Typ | Standardwert | Verwendet | Stabilität |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | Kein Verbraucher in `src/`. Der Host-Trace `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` wird immer geschrieben. | `PARTIAL` (mitgeführt, derzeit ohne Wirkung) |
| `Verbose` | bool | `false` | Kein Verbraucher. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | Kein Verbraucher. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | Kein Verbraucher. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Kein Verbraucher. | `PARTIAL` |
| `NativeTrace` | bool | `false` | Kein Verbraucher. | `PARTIAL` |

Die CLI-Flags `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` befüllen diese booleschen Werte (`/logall` setzt `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`); sie werden mit den Spec-Werten ODER-verknüpft.

### `SessionPresentationSpec`

| Eigenschaft | Typ | Standardwert | Gültige Werte | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (Alias `Native`): Die eigenen Startbilddateien von OMSI bleiben unangetastet. `1` `Managed`: OmsiLaunch legt für die Sitzung `GUI\NewSplashscreen_ENG.bmp` und `GUI\NewSplashscreen_<LANG>.bmp` als Overlay an (anschließend exakt wiederhergestellt). | ja | `STABLE_BETA` (Matrix RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (ohne Beachtung der Groß-/Kleinschreibung); jeder andere Wert wird zu `ENG` normalisiert. Wenn nicht gesetzt, wird der `[language]`-Wert aus `options.cfg` gelesen und auf dieselbe Weise normalisiert. | ja (nur verwaltetes Startbild) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Verzeichnis, das `ENG.bmp` und `<LANG>.bmp` enthält (640×480, 24-Bit-BMP). Siehe [Pfadregeln](#path-rules). Fehlendes Verzeichnis: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; fehlende Datei: `OL_E_SPLASH_ASSET_MISSING`; falsches Format: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. Wenn nicht gesetzt, wird `<root>\.omsilaunch\assets\splash` verwendet (einmalig aus dem Paket befüllt), andernfalls das mitgelieferte `assets\splash`. | ja (nur verwaltetes Startbild) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` unterdrückt die eigenständige Windows-Tray-Anzeige des CLI-Eigentümers. | Nur CLI-Eigentümer; die API hat kein Tray-Symbol. Es gibt kein CLI-Flag; der Wert kann nur aus einer Spec-Datei stammen. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Eigenschaft | Typ | Standardwert | Gültige Werte | Verwendet | Stabilität |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native`: Es ändert sich nichts. `1` `Disabled`: Das Plugin unterdrückt den prozessinternen Downloader von OMSI (Telemetrie `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: Das `.itx`-Profil wird als Overlay `Texture\standard.itx` angelegt; seine Zieldateien und `Texture\standard.ipr` werden zu Sitzungslöschungen. | ja | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Erforderlich für `Override` (`OL_E_ITX_PROFILE_REQUIRED`). Eine Textdatei mit Zeilenpaaren: absolute `http`/`https`-URL, danach ein Zielpfad relativ zum Installationsstammverzeichnis, der eine `Texture\`-Komponente enthält, nicht absolut ist, kein `..` enthält, nicht mit `\` beginnt und keine Junction/keinen Symlink durchläuft (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Siehe [Pfadregeln](#path-rules). | ja | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Eigenschaft | Typ | Verwendet | Stabilität |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | Strings / int | Werden in der Plandiagnose `session_profile.selected` festgehalten (`Data["session_profile.*"]`). Ansonsten nicht verwendet; normalerweise vom Sitzungsprofil-Compiler befüllt, nicht von Hand. | `STABLE_BETA` |

<a id="enumerations"></a>
## Aufzählungen

| Enum | Werte (JSON-Ganzzahl) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (veralteter Alias; es ist der native Last-Map-State-Zweig von OMSI, niemals die neueste `.osn`) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (Alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (schreibgeschütztes `Entrypoint.Mode`) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Ganzzahlen außerhalb des deklarierten Bereichs werden vom Serializer unverändert gespeichert und verhalten sich wie unbekannte Werte (ein unbekannter `WorldMode` ist z. B. weder NEW_MAP noch SAVED_SITUATION und ergibt einen Plan ohne Welt-Capability; das Plugin würde ihn ablehnen, die CLI ersetzt den Modus jedoch ohnehin, siehe Rangfolge).

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## Validierungsregeln und Diagnosen für nicht ausführbare Pläne

`PlanSessionAsync` führt `LaunchValidation.Validate` und anschließend `SessionPlanner.PlanAsync` aus. Ein Plan ist genau dann ausführbar, wenn kein Diagnosecode mit `OL_E_` beginnt. Die vollständige Menge:

| Diagnose | Bedingung | Quelle |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` leer oder nur Leerraum | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` ohne gesetzten Wert oder mit Monat/Tag außerhalb des Bereichs; `Time.Mode` = `Explicit` ohne gesetzten Wert oder mit Stunde/Minute/Sekunde außerhalb des Bereichs | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` oder `Time.Value` gesetzt, während der Modus nicht `Explicit` ist | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` mit nicht gesetzter `MapIdentity` oder nicht in der Form `maps\...\global.cfg` (Validierung); `NewMap` mit einer Identität, die nicht installiert ist (Planer) | beide |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` ohne `EntrypointIdentity` und mit nicht gesetztem oder negativem `PresentedEntrypointIndex` | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, Karte installiert, keine `EntrypointIdentity`, `PresentedEntrypointIndex` nicht gesetzt (`world.presented-entrypoint` nicht verfügbar) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` ohne `SituationIdentity` (Validierung) oder mit einer Identität, die nicht installiert ist (Planer) | beide |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: Die in der `.osn` genannte Karte ist nicht installiert | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | nicht Windows 10+ auf einem x64-Betriebssystem mit einem x64-Hostprozess (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | Stammverzeichnis fehlt, Schreibschutzattribut gesetzt oder kein Unterverzeichnis `plugins\` (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | `Omsi.exe` fehlt, oder ihre Größe/ihr SHA-256 entspricht weder dem Profil-Fingerabdruck (`692EBFBF...`, 8 503 440 Bytes) noch einem zugelassenen Hash (`omsi.profile.OMSI23004`) | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `World.Mode` = `LastMapState`; `EntrypointIdentity` gesetzt; Modus von `Date`/`Time`/`Year` nicht `Unset`; Modus von `Weather` nicht `Unset`; irgendein `PlayerVehicle`-Feld gesetzt; `Input.KeyboardDocument` oder `Input.ControllerDocument` gesetzt | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` nicht installiert (zusätzlich zu `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | ein `Environment`-Schlüssel, der nicht im Katalog steht / nicht beschreibbar ist | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | beim Erstellen des Startbild-/ITX-Plans wurde eine Exception ausgelöst; die Meldung enthält `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` oder `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | die Referenz der Plugin-Gesamtheit (Closure) (`OmsiLaunchRuntimePaths`) oder das Release-Manifest kann nicht geladen werden (die Meldung kann `OL_E_RELEASE_MANIFEST_INVALID` enthalten) | `OmsiLaunchService.PlanSessionAsync` |

Nicht zur Planungszeit validiert (scheitert beim Start als `Failed`-Sitzung mit `OL_E_START_SESSION`): Einstellungswerte (`OL_E_INVALID_SETTING_VALUE`), Integrität des permanenten Plugins (`OL_E_PERMANENT_PLUGIN_*`), Verfügbarkeit der Lease (`OL_E_INSTALLATION_BUSY`), Bereich von `StartupTimeoutSeconds` (ausgelöst von `StartSessionAsync`).

Informative Plandiagnosen: `plugin.integrity.reference` (Meldung `manifest` oder `self`), `session_profile.selected`.

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## Rangfolge: CLI-Flags vs. Spec-Datei vs. Sitzungsprofil

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) erstellt die wirksame Spec in dieser Reihenfolge:

1. Ausgangsbasis = eingebaute Standardwerte oder, falls angegeben, die `/spec`-Datei.
2. Installationsstammverzeichnis = das explizite Installationsargument, falls angegeben, sonst der `RootPath` der Ausgangsbasis; danach `.`/leer → Verzeichnis der ausführbaren Datei, `Path.GetFullPath`. Ein explizites Installationsargument hat immer Vorrang vor dem `RootPath` der Spec.
3. Sitzungsprofil (`/predefined-profile` + `/predefined-profile-index`): Explizite CLI-Argumente, die ein profileigenes Feld betreffen, werden mit `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` abgelehnt (Weltfelder nur im NEW_MAP-Modus; `/set`-Schlüssel, die in der Voreinstellung vorhanden sind; Startbild-Flags, wenn die Voreinstellung `presentation` hat; Internettextur-Flags, wenn sie `internet-textures` hat; Timeouts, wenn sie `behavior` hat). Die `World` der Ausgangsbasis wird durch eine neue ersetzt (nur der Weltmodus der CLI bleibt erhalten), dann werden der `new:`-Block des Profils (nur NEW_MAP), `settings` (in `General`), `presentation`, `internet-textures`, `behavior` und die `SessionProfile`-Metadaten angewendet. `compatibility.maps` wird für NEW_MAP und SAVED_SITUATION erzwungen.
4. Welt: Der Weltmodus der CLI hat immer Vorrang (`/new` Standard, `/saved:<osn>`, `/last`); der `World.Mode` der Spec-Datei wird ersetzt. Um eine gespeicherte Situation aus einer Spec auszuführen, übergeben Sie `/saved:`. `/map` und `/entrypoint`/`/entrypoint-index` überschreiben die Ausgangsbasis; eine CLI-Identität per `/entrypoint` löscht den Index; `/saved` zusammen mit `/map` oder Einstiegspunkt-Flags ergibt `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` überschreiben die Ausgangsbasis, wenn angegeben (`system` wählt `DateTimeMode.System`).
6. `/no-vehicle` löscht `PlayerVehicle`; die einzelnen Flags `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` überschreiben einzelne Felder des Spielerfahrzeugs der Ausgangsbasis.
7. `/set:<key>=<value>`-Einträge werden zu `Environment.General` hinzugefügt (Schlüssel geprüft, Wert nicht); die anderen sieben Gruppen stammen unverändert aus der Ausgangsbasis.
8. `/startup-timeout` und `/shutdown-timeout` überschreiben die Ausgangsbasis nur, wenn angegeben; andernfalls gelten Spec, dann Profil, dann die Standardwerte 180 s / 30 s. `ShutdownTimeoutSeconds` ist für den Supervisor `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`.
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` überschreiben die Ausgangsbasis, wenn angegeben; `SuppressTrayIcon` stammt ausschließlich aus der Ausgangsbasis.
10. Diagnose-Flags werden mit der Ausgangsbasis ODER-verknüpft.

Ergebnis: explizites CLI-Flag > Sitzungsprofil > Spec-Datei > eingebauter Standardwert, mit der Ausnahme, dass ein CLI-Flag, das mit einem profileigenen Feld kollidiert, ein Fehler ist und kein Überschreiben.

<a id="path-rules"></a>
## Pfadregeln

| Pfad | Verhalten der API | Verhalten der CLI |
| --- | --- | --- |
| `Installation.RootPath` | Wird wie angegeben verwendet: Relative Pfade werden bei Dateioperationen gegen das Arbeitsverzeichnis des Prozesses aufgelöst. Übergeben Sie einen absoluten Pfad. Lease, Journal und Inhaltskatalog normalisieren ihn mit `Path.GetFullPath`. | `.` oder leer = das Verzeichnis, das `OmsiLaunch.exe` enthält, niemals der Arbeitsordner des Aufrufers; ein explizites Installationsargument hat Vorrang vor der Spec; das Ergebnis wird absolut gemacht. |
| `Presentation.CustomAssetDirectory` | Absolut oder relativ zu `Installation.RootPath`. Muss existieren. | Ebenso (`/splash-assets`). Ein `assets`-Pfad eines Sitzungsprofils ist auf das Profilpaket beschränkt und wird absolut gespeichert. |
| `InternetTextures.OverrideProfilePath` | Wird mit `Path.GetFullPath` aufgelöst, d. h. relativ zum Arbeitsverzeichnis des Prozesses, nicht zum Installationsstammverzeichnis. Muss existieren. | Ebenso (`/internet-textures-profile`). Ein `profile`-Pfad eines Sitzungsprofils ist auf das Paket beschränkt und wird absolut gespeichert. |
| ITX-Zielzeilen | Relativ zum Installationsstammverzeichnis; müssen eine `Texture\`-Komponente enthalten; keine Wurzel, kein `..`, kein führendes `\`, keine Junction-/Symlink-Komponente. | Ebenso. |
| Inhaltsidentitäten (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Relativ zur Installation, ohne Beachtung der Groß-/Kleinschreibung, `/` wird akzeptiert; niemals absolut. | Ebenso. |

<a id="carried-but-not-applied"></a>
## Mitgeführt, aber nicht angewendet

| Feld | Aktuelle Wirkung | Stabilität |
| --- | --- | --- |
| `Installation.ExpectedExecutableSha256` | keine (der Host bildet den Hash von `Omsi.exe` und prüft ihn gegen das Build-Profil) | `PARTIAL` |
| `Behavior.RestoreConfiguration` | keine (die Wiederherstellung läuft immer) | `PARTIAL` |
| `Behavior.ShutdownTimeoutSeconds` | keine (erzwungene Beendigung; `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`) | `PARTIAL` |
| `Diagnostics.*` | keine (Host-Trace wird immer geschrieben) | `PARTIAL` |
| `Input.KeyboardDocument`, `Input.ControllerDocument` | Plan nicht ausführbar, wenn gesetzt | `PARTIAL` |
| `Date`, `Time`, `Year` (Modus ungleich `Unset`) | Plan nicht ausführbar (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `Weather` (Modus ungleich `Unset`) | Plan nicht ausführbar (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `PlayerVehicle.*` (jedes gesetzte Feld) | Inhalt für Diagnosen aufgelöst, Plan nicht ausführbar (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `World.EntrypointIdentity` | Plan nicht ausführbar (`RUNTIME_PARTIAL`) | `PARTIAL` |
| `World.Mode` = `LastMapState` / `LastSituation` | Plan nicht ausführbar (`UNSUPPORTED_FOR_CURRENT_PROFILE`) | `UNAVAILABLE` |
| `SessionProfile` | nur Herkunftsdiagnose | `STABLE_BETA` |
