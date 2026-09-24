# Referencia de LaunchSpec

<!-- l10n: source=reference/launchspec.md -->
> Traducción de la [página original en inglés](../../../reference/launchspec.md) de OmsiLaunch 0.1.0-beta3. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta página es la referencia normativa de `LaunchSpec`, el record de solicitud que describe una sesión de OmsiLaunch: su forma en C# (`OmsiLaunch.Api`), su forma de archivo JSON tal como la carga la CLI (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), cada propiedad con su tipo, valor predeterminado, regla de validación y efecto actual, las reglas de validación que hacen que un plan no sea ejecutable y la precedencia entre los flags de la CLI, los archivos de spec y los perfiles de sesión. Solo se documenta lo que hace el código actual.

Páginas relacionadas: [API pública](public-api.md), [referencia de la CLI](cli.md), [perfiles de sesión](session-profiles.md), [códigos de error](errors.md), [ciclo de vida de la sesión](../concepts/session-lifecycle.md), [capacidades](capabilities.md).

<a id="where-a-launchspec-comes-from"></a>
## De dónde proviene un LaunchSpec

| Origen | Cómo se convierte en un `LaunchSpec` |
| --- | --- |
| API | El integrador construye el record y lo pasa a `PlanSessionAsync`. |
| Flags de la CLI | `CliInput.BuildSpecAsync` parte de valores predeterminados integrados (NEW_MAP, todo sin establecer, valores predeterminados de `Behavior`) y aplica los flags. |
| Archivo JSON `/spec:<path>` | Se carga con `LaunchSpecJson.LoadAsync` y luego se usa como semilla que los flags de la CLI sobrescriben (consulte [precedencia](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Perfil de sesión (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` escribe en la semilla el mundo, los ajustes, la presentación, las texturas de Internet y el comportamiento del perfil, y registra los metadatos de `SessionProfile`. |

El [ejemplo completo](#complete-example) de abajo está verificado contra la forma del record. Una copia del ejemplo mínimo se distribuye como `examples/release-session.example.json` (y `.omsilaunch\examples\release-session.example.json` en el paquete de release).

<a id="json-form"></a>
## Forma JSON

| Regla | Detalle |
| --- | --- |
| Serializador | `System.Text.Json` con `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; no hay convertidores registrados. |
| Nombres de propiedades | Los nombres de propiedad de C# (`Installation`, `RootPath`, ...). Al cargar, la coincidencia no distingue mayúsculas de minúsculas; la CLI los escribe en PascalCase. |
| Enums | Enteros (no hay convertidor de enums a cadena). `"Mode": 0` es válido; `"Mode": "NewMap"` se rechaza como JSON mal formado. Los valores se enumeran en [Enumeraciones](#enumerations). |
| `OptionalValue<T>` | Un objeto `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (el valor se ignora), 1 = `Set` (el valor debe estar presente y no ser nulo; un `Set` con valor nulo no se valida y se comporta como un valor no válido). Un miembro `OptionalValue` omitido es `Unset`. El miembro de solo lectura `IsSet` aparece en la salida que escribe la CLI y al cargar se acepta y se ignora. |
| Records opcionales | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile` pueden ser `null` u omitirse; los accesores `Effective*` sustituyen valores predeterminados. |
| Records obligatorios | `Installation`, `World`, `Date`, `Time`, `Environment` (con los ocho diccionarios; use `{}`), `Behavior` deben ser objetos presentes. No se validan: uno `null` o ausente falla más adelante con una referencia nula, que la CLI informa como `OL_E_INTERNAL` (salida 10) o `OL_E_INVALID_ARGUMENT` (salida 2). |
| Propiedades desconocidas | Se rechazan antes del enlace: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (la ruta usa los nombres de miembro tal como están escritos en el archivo). El contenido de los diccionarios (`Environment.*`) no se comprueba como propiedades. |
| Raíz | Debe ser un objeto JSON: `OL_E_SPEC_INVALID`. Profundidad máxima de anidamiento: 32. |
| Tamaño del archivo | Como máximo 1 MiB (1 048 576 bytes): `OL_E_SPEC_TOO_LARGE`. Archivo inexistente: `OL_E_SPEC_NOT_FOUND`. |
| Comentarios y comas finales | Se aceptan comentarios `//` y `/* */` y comas finales. |
| JSON mal formado | La excepción del parser no se traduce: la CLI informa `OL_E_INTERNAL` con el código de salida 10. |
| Codificación | UTF-8 (el lector tolera un BOM). Las barras invertidas en las identidades deben escaparse (`"maps\\Grundorf\\global.cfg"`); se aceptan barras normales para las identidades de mapa, situación, vehículo y HOF. |

<a id="complete-example"></a>
## Ejemplo completo

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

Una fecha explícita, cuando el build la admite, se escribe como `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` y una hora como `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. En este build, ambas hacen que el plan no sea ejecutable (vea más abajo).

<a id="property-reference"></a>
## Referencia de propiedades

La columna "Consumida" indica qué hace el código actual con el valor. La estabilidad usa el vocabulario de la [página de la API pública](public-api.md#stability-vocabulary).

<a id="launchspec-root"></a>
### `LaunchSpec` (raíz)

| Propiedad | Tipo JSON | Obligatoria | Valor predeterminado si se omite | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `Installation` | objeto `InstallationSpec` | sí | ninguno | sí | `STABLE_BETA` |
| `World` | objeto `WorldSpec` | sí | ninguno | sí | `STABLE_BETA` |
| `Date` | objeto `DateSpec` | sí | ninguno | validada; cualquier modo distinto de `Unset` no es ejecutable | `PARTIAL` |
| `Time` | objeto `TimeSpec` | sí | ninguno | validada; cualquier modo distinto de `Unset` no es ejecutable | `PARTIAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | no | `Unset` | se resuelve para los diagnósticos; cualquier campo establecido no es ejecutable | `PARTIAL` |
| `Environment` | objeto `EnvironmentSpec` | sí | ninguno | sí (overlay semántico de `options.cfg`) | `STABLE_BETA` |
| `Behavior` | objeto `LaunchBehaviorSpec` | sí | ninguno | en parte (consulte el record) | `STABLE_BETA` / `PARTIAL` |
| `Year` | objeto `YearSpec` o null | no | `null` → `EffectiveYear` = modo `Unset` | cualquier modo distinto de `Unset` no es ejecutable | `PARTIAL` |
| `Weather` | objeto `WeatherSpec` o null | no | `null` → `EffectiveWeather` = modo `Unset` | cualquier modo distinto de `Unset` no es ejecutable | `PARTIAL` |
| `Input` | objeto `InputSpec` o null | no | `null` → `EffectiveInput` = ambos sin establecer | cualquier documento establecido no es ejecutable | `PARTIAL` |
| `Diagnostics` | objeto `DiagnosticsSpec` o null | no | `null` → `EffectiveDiagnostics` = valores predeterminados | solo transportada | `PARTIAL` |
| `Presentation` | objeto `SessionPresentationSpec` o null | no | `null` → `EffectivePresentation` = splash administrado, sin idioma, sin directorio personalizado, ícono de la bandeja visible | sí | `STABLE_BETA` |
| `InternetTextures` | objeto `InternetTexturesSpec` o null | no | `null` → `EffectiveInternetTextures` = `Native` | sí | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | objeto `SessionProfileMetadata` o null | no | `null` | solo procedencia (diagnóstico de plan `session_profile.selected`) | `STABLE_BETA` |

Accesores de solo lectura (presentes en la salida JSON de la CLI, ignorados al cargar): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`.

### `InstallationSpec`

| Propiedad | Tipo | Valor predeterminado | Valores válidos | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | obligatorio | Directorio que contiene `Omsi.exe` y `plugins\`. Consulte las [reglas de rutas](#path-rules). Vacío o solo espacios en blanco → `OL_E_INSTALLATION_NOT_FOUND`. | sí | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string o null | `null` | Cualquier cadena. | Ningún consumidor en el código actual: el host siempre calcula el hash de `Omsi.exe` y lo compara con el perfil de build, nunca con este valor. | `PARTIAL` (transportada, sin efecto actualmente) |

### `WorldSpec`

| Propiedad | Tipo | Valor predeterminado | Valores válidos | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | obligatorio | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` es un alias obsoleto con el mismo valor 2). | sí; `LastMapState` → `OL_E_CAPABILITY_UNAVAILABLE` | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | Para `NewMap`: obligatoria, con la forma `maps\<dir>\global.cfg` (sin distinguir mayúsculas de minúsculas, se acepta `/`, sin `..`) e instalada. Se ignora para `SavedSituation` (el `.osn` proporciona el mapa). | sí (handoff) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | Para `SavedSituation`: obligatoria, una identidad `situations\...\<file>.osn` instalada (tal como la devuelven `DiscoverAsync(Situations)` / `/list:situations`). | sí (handoff) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | Para `NewMap` sin `EntrypointIdentity`: obligatorio, `>= 0`, un índice en la lista de puntos de entrada que OMSI presenta para el mapa. Se envía al plugin como `-1` cuando no está establecido. | sí (handoff) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | Una etiqueta de punto de entrada sin procesar o una identidad de descubrimiento. Establecerla hace que el plan no sea ejecutable (`world.entrypoint-identity`, `RUNTIME_PARTIAL`, `OL_E_CAPABILITY_UNAVAILABLE`). | transportada | `PARTIAL` |
| `Entrypoint` | `EntrypointSpec` (solo lectura) | calculado | `Mode` = `Identity` cuando `EntrypointIdentity` está establecida; si no, `PresentedIndex` cuando el índice está establecido; si no, `Unset`; `PresentedIndex` e `Identity` reflejan las entradas. | derivada | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Propiedad | Tipo | Valor predeterminado | Valores válidos | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | obligatorio (`Year`: `0` cuando el record es null) | `0` `Unset`, `1` `Explicit`, `2` `System`. | `Explicit`/`System` → entrada `unsupported` (`world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `STATICALLY_PARTIAL`) y `OL_E_CAPABILITY_UNAVAILABLE`. Los modos también se copian en el handoff de arranque, que el plugin rechaza cuando no son `Unset` (nunca se llega a eso porque el plan no es ejecutable). | `PARTIAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: `Year`, `Month` 1..12, `Day` 1..31; `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59. Debe estar establecido cuando `Mode` es `Explicit` (de lo contrario, `OL_E_DATE_TIME_APPLY_FAILED`) y no debe estar establecido cuando `Mode` no es `Explicit` (`OL_E_INVALID_ARGUMENT`). `YearSpec.Value` no se valida. | solo validado | `PARTIAL` |

### `WeatherSpec`

| Propiedad | Tipo | Valor predeterminado | Valores válidos | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0` cuando el record es null | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Cualquier modo distinto de `Unset` → entrada no admitida `weather` y `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Nombre del preset (no se valida). | transportada | `PARTIAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | Código ICAO (no se valida). | transportada | `PARTIAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec` (dentro de `PlayerVehicle`)

| Propiedad | Tipo | Valor predeterminado | Valores válidos | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Identidad `Vehicles\...\<file>.bus` instalada; de lo contrario, `OL_E_VEHICLE_NOT_FOUND`. | se resuelve en `ResolvedContent`; luego `player-vehicle.model` no admitida → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | Una identidad de repaint de `Model` (`<cti>#item:<n>`); de lo contrario, `OL_E_REPAINT_NOT_FOUND`; solo se comprueba cuando `Model` está establecido. | igual | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | `Vehicles\...\<file>.hof` instalado; de lo contrario, `OL_E_HOF_NOT_FOUND`. | igual | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Cualquier cadena. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Cualquier cadena. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (solo lectura) | calculado | `true` cuando `Model` está establecido. `PlayerVehicle.IsSet` es lo que el handoff transporta como `PlayerVehicleEnabled`. | derivada | `PARTIAL` |

Un `PlayerVehicle` con `Presence` 1 y todos los campos sin establecer se acepta y no tiene efecto. Cualquier campo establecido hace que el plan no sea ejecutable en este build (`STATICALLY_PARTIAL`).

### `EnvironmentSpec`

| Propiedad | Tipo | Valor predeterminado | Consumida | Estabilidad |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | `IReadOnlyDictionary<string, OptionalValue<string>>` cada uno, obligatorio (`{}` cuando está vacío) | ninguno | sí | `STABLE_BETA` |

Los ocho grupos se concatenan; el grupo en el que se coloca una clave no tiene efecto. Cada entrada con `Presence` 1 es un ajuste semántico de `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); la clave selecciona el archivo de destino (`options.cfg` para todas las claves actuales) y el token. La planificación comprueba que la clave exista (`OL_E_UNKNOWN_SETTING`) y sea escribible (`OL_E_SETTING_NOT_WRITABLE`); el valor solo se valida al iniciar (`OL_E_INVALID_SETTING_VALUE`, informado como una sesión `Failed` con `OL_E_START_SESSION`). Las claves no distinguen mayúsculas de minúsculas. Las entradas sin establecer se ignoran. El flag `/set:<key>=<value>` de la CLI escribe en `General`; los `settings` de un perfil de sesión también se combinan en `General`.

| Clave | Valor | Notas |
| --- | --- | --- |
| `general.language` | string | token `[language]` |
| `general.radio` | string | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | tokens de presencia (`autoSave` es el inverso de `noAutoSave`) |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | entero 10..200 | |
| `graphics.tileDistance` | entero 1..20 | |
| `graphics.maxObjectDistanceMeters` | número 20..5000 | |
| `graphics.minObjectScreenPercent` | número 0..10 | se almacena dividido entre 100 |
| `graphics.minReflectionObjectScreenPercent` | número 0..50 | se almacena dividido entre 100 |
| `graphics.maxObjectComplexity` | entero 0..3 | |
| `graphics.maxMapComplexity` | entero 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | tokens de presencia |
| `graphics.stencilShadows` | `true` / `false` | se escribe como `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | un bloque `smokesystems` |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | tokens de presencia |
| `simulation.ticketSelling` | entero 0..2 | |
| `simulation.maintenance` | entero 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | dos tokens de OMSI a la vez (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | tokens de presencia |
| `traffic.randomVehicles` | entero 0..1000 | componente 0 del bloque multilínea `AIMaxCountRandom` (validado en runtime, matriz RV-005) |
| `traffic.humans` | entero 0..1000 | componente 1 de `AIMaxCountRandom` |
| `traffic.factorPercent` | número 1..300 | |
| `traffic.parkedVehiclesPercent` | número 0..100 | |
| `traffic.scheduledVehicles` | número 0..1000 | |
| `traffic.scheduledLinePriority` | número 1..4 | |
| `traffic.passengerFactorPercent` | número 0..200 | |
| `sound.stereo` | número 0..100 | |
| `sound.maxSimultaneousSounds` | número 5..1000 | |
| `sound.masterVolume` | número 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | rechazado | conocidas pero no escribibles → `OL_E_SETTING_NOT_WRITABLE` |

Los archivos modificados conservan su codificación (se preservan los bytes Windows-1252; se respetan UTF-8/UTF-16 marcados con BOM) y sus finales de línea.

### `LaunchBehaviorSpec`

| Propiedad | Tipo | Valor predeterminado | Valores válidos | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | cualquiera | Ningún consumidor: los archivos que pertenecen a la sesión siempre se restauran de forma exacta. | `PARTIAL` (transportada, sin efecto actualmente) |
| `SuppressStaleClosecheckWarning` | bool | `true` | cualquiera | `true`: un archivo `closecheck` que existe antes de la sesión se elimina permanentemente al iniciar (diagnóstico `closecheck.stale-removed` con su SHA-256; error `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: un `closecheck` existente se deja intacto y no es una eliminación de la sesión. El `closecheck` que OMSI escribe durante la sesión siempre se elimina en la restauración. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (de lo contrario, `ArgumentOutOfRangeException` desde `StartSessionAsync`; el flag `/startup-timeout` de la CLI exige 1..600; los perfiles exigen > 0). Presupuesto de tiempo desde el inicio del supervisor hasta `Running`; al vencer, la sesión falla con `OL_E_STARTUP_TIMEOUT` (plugin iniciado) u `OL_E_PLUGIN_NOT_LOADED`. | sí | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | cualquier int (`/shutdown-timeout` de la CLI: 1..600) | Ningún consumidor: el supervisor termina OMSI de inmediato con `TerminateProcess`; no hay espera de cierre cooperativo. | `PARTIAL` (transportada, sin efecto actualmente) |

### `InputSpec`

| Propiedad | Tipo | Valor predeterminado | Consumida | Estabilidad |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Establecido → `input.keyboard` no admitida (`STATICALLY_PARTIAL`) y `OL_E_CAPABILITY_UNAVAILABLE`. La ejecución de PATCH/REPLACE del teclado no está implementada. | `PARTIAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Establecido → `input.controller` no admitida y `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |

### `DiagnosticsSpec`

| Propiedad | Tipo | Valor predeterminado | Consumida | Estabilidad |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | Ningún consumidor en `src/`. La traza del host `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` siempre se escribe. | `PARTIAL` (transportada, sin efecto actualmente) |
| `Verbose` | bool | `false` | Ningún consumidor. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | Ningún consumidor. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | Ningún consumidor. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Ningún consumidor. | `PARTIAL` |
| `NativeTrace` | bool | `false` | Ningún consumidor. | `PARTIAL` |

Los flags de la CLI `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` rellenan estos booleanos (`/logall` establece `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`); se combinan con OR con los valores del spec.

### `SessionPresentationSpec`

| Propiedad | Tipo | Valor predeterminado | Valores válidos | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (alias `Native`): los archivos de splash propios de OMSI no se modifican. `1` `Managed`: OmsiLaunch aplica como overlay `GUI\NewSplashscreen_ENG.bmp` y `GUI\NewSplashscreen_<LANG>.bmp` durante la sesión (restaurados de forma exacta después). | sí | `STABLE_BETA` (matriz RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (sin distinguir mayúsculas de minúsculas); cualquier otro valor se normaliza a `ENG`. Si no está establecido, se lee el valor `[language]` de `options.cfg` y se normaliza de la misma manera. | sí (solo splash administrado) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Directorio que contiene `ENG.bmp` y `<LANG>.bmp` (640×480, BMP de 24 bits). Consulte las [reglas de rutas](#path-rules). Directorio inexistente: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; archivo inexistente: `OL_E_SPLASH_ASSET_MISSING`; formato incorrecto: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. Si no está establecido, se usa `<root>\.omsilaunch\assets\splash` (inicializado una vez a partir del paquete); si no, el `assets\splash` del paquete. | sí (solo splash administrado) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` suprime el indicador independiente de la bandeja de Windows del propietario de la CLI. | Solo el propietario de la CLI; la API no tiene bandeja. No hay flag de la CLI; solo puede provenir de un archivo de spec. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Propiedad | Tipo | Valor predeterminado | Valores válidos | Consumida | Estabilidad |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native`: no cambia nada. `1` `Disabled`: el plugin suprime el descargador interno del proceso de OMSI (telemetría `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: el perfil `.itx` se aplica como overlay en `Texture\standard.itx`; sus archivos de destino y `Texture\standard.ipr` pasan a ser eliminaciones de la sesión. | sí | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Obligatorio para `Override` (`OL_E_ITX_PROFILE_REQUIRED`). Un archivo de texto con pares de líneas: una URL `http`/`https` absoluta y luego una ruta de destino relativa a la raíz de la instalación que contiene un componente `Texture\`, no tiene raíz, no contiene `..`, no empieza con `\` y no atraviesa ninguna junction ni enlace simbólico (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Consulte las [reglas de rutas](#path-rules). | sí | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Propiedad | Tipo | Consumida | Estabilidad |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | strings / int | Se registran en el diagnóstico de plan `session_profile.selected` (`Data["session_profile.*"]`). No se consumen de otra manera; normalmente los rellena el compilador de perfiles de sesión, no se escriben a mano. | `STABLE_BETA` |

<a id="enumerations"></a>
## Enumeraciones

| Enum | Valores (entero JSON) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsoleto; es la rama nativa de OMSI del último estado del mapa, nunca el `.osn` más reciente) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (`Entrypoint.Mode`, de solo lectura) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Los enteros fuera del rango declarado se almacenan tal cual en el serializador y se comportan como valores desconocidos (por ejemplo, un `WorldMode` desconocido no es ni NEW_MAP ni SAVED_SITUATION y produce un plan sin capacidad de mundo; el plugin lo rechazaría, pero la CLI reemplaza el modo de todos modos; consulte la precedencia).

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## Reglas de validación y diagnósticos de plan no ejecutable

`PlanSessionAsync` ejecuta `LaunchValidation.Validate` y luego `SessionPlanner.PlanAsync`. Un plan es ejecutable exactamente cuando ningún código de diagnóstico empieza con `OL_E_`. El conjunto completo:

| Diagnóstico | Condición | Origen |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` vacío o solo con espacios en blanco | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` sin un valor establecido o con mes/día fuera de rango; `Time.Mode` = `Explicit` sin un valor establecido o con hora/minuto/segundo fuera de rango | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` o `Time.Value` establecido mientras el modo no es `Explicit` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` con `MapIdentity` sin establecer o sin la forma `maps\...\global.cfg` (validación); `NewMap` con una identidad que no está instalada (planificador) | ambos |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` sin `EntrypointIdentity` y con `PresentedEntrypointIndex` sin establecer o negativo | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, mapa instalado, sin `EntrypointIdentity`, `PresentedEntrypointIndex` sin establecer (`world.presented-entrypoint` no disponible) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` sin `SituationIdentity` (validación) o con una identidad que no está instalada (planificador) | ambos |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: el mapa indicado dentro del `.osn` no está instalado | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | no es Windows 10+ en un sistema operativo x64 con un proceso host x64 (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | directorio raíz inexistente, atributo de solo lectura establecido o sin subdirectorio `plugins\` (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | falta `Omsi.exe`, o su tamaño/SHA-256 no es ni la huella del perfil (`692EBFBF...`, 8 503 440 bytes) ni un hash de la lista de permitidos (`omsi.profile.OMSI23004`) | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `World.Mode` = `LastMapState`; `EntrypointIdentity` establecida; modo de `Date`/`Time`/`Year` distinto de `Unset`; modo de `Weather` distinto de `Unset`; cualquier campo de `PlayerVehicle` establecido; `Input.KeyboardDocument` o `Input.ControllerDocument` establecido | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` no instalado (además de `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | una clave de `Environment` que no está en el catálogo / no es escribible | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | la construcción del plan de splash/ITX lanzó una excepción; el mensaje incluye `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` u `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | no se puede cargar la referencia del cierre del plugin (`OmsiLaunchRuntimePaths`) o el manifiesto de la release (el mensaje puede incluir `OL_E_RELEASE_MANIFEST_INVALID`) | `OmsiLaunchService.PlanSessionAsync` |

No se valida en el momento de planificar (falla al iniciar como una sesión `Failed` con `OL_E_START_SESSION`): valores de los ajustes (`OL_E_INVALID_SETTING_VALUE`), integridad del plugin permanente (`OL_E_PERMANENT_PLUGIN_*`), disponibilidad del lease (`OL_E_INSTALLATION_BUSY`), rango de `StartupTimeoutSeconds` (lanzado por `StartSessionAsync`).

Diagnósticos de plan informativos: `plugin.integrity.reference` (mensaje `manifest` o `self`), `session_profile.selected`.

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## Precedencia: flags de la CLI frente a archivo de spec frente a perfil de sesión

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) construye el spec efectivo en este orden:

1. Semilla = valores predeterminados integrados, o el archivo `/spec` cuando se indica.
2. Raíz de la instalación = el argumento de instalación explícito si se indica; si no, el `RootPath` de la semilla; luego `.`/vacío → directorio del archivo ejecutable, `Path.GetFullPath`. Un argumento de instalación explícito siempre prevalece sobre el `RootPath` del spec.
3. Perfil de sesión (`/predefined-profile` + `/predefined-profile-index`): los argumentos explícitos de la CLI que afectan un campo que pertenece al perfil se rechazan con `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (campos de mundo solo en modo NEW_MAP; claves de `/set` presentes en el preset; flags de splash cuando el preset tiene `presentation`; flags de texturas de Internet cuando tiene `internet-textures`; timeouts cuando tiene `behavior`). El `World` de la semilla se reemplaza por uno nuevo (solo se conserva el modo de mundo de la CLI) y luego se aplican el bloque `new:` del perfil (solo NEW_MAP), `settings` (en `General`), `presentation`, `internet-textures`, `behavior` y los metadatos de `SessionProfile`. `compatibility.maps` se exige para NEW_MAP y SAVED_SITUATION.
4. Mundo: el modo de mundo de la CLI siempre prevalece (`/new` de forma predeterminada, `/saved:<osn>`, `/last`); el `World.Mode` del archivo de spec se reemplaza. Para ejecutar una situación guardada desde un spec, pase `/saved:`. `/map` y `/entrypoint`/`/entrypoint-index` sobrescriben la semilla; una identidad `/entrypoint` de la CLI borra el índice; `/saved` con `/map` o flags de punto de entrada es `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` sobrescriben la semilla cuando se indican (`system` selecciona `DateTimeMode.System`).
6. `/no-vehicle` borra `PlayerVehicle`; `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` individuales sobrescriben campos individuales del vehículo del jugador de la semilla.
7. Las entradas `/set:<key>=<value>` se agregan a `Environment.General` (se comprueba la clave, no el valor); los otros siete grupos provienen de la semilla sin cambios.
8. `/startup-timeout` y `/shutdown-timeout` sobrescriben la semilla solo cuando se indican; de lo contrario, se aplican el spec, luego el perfil y luego los valores predeterminados 180 s / 30 s. `ShutdownTimeoutSeconds` es `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` en el supervisor.
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` sobrescriben la semilla cuando se indican; `SuppressTrayIcon` proviene solo de la semilla.
10. Los flags de diagnóstico se combinan con OR con la semilla.

Resultado: flag explícito de la CLI > perfil de sesión > archivo de spec > valor predeterminado integrado, excepto que un flag de la CLI que entra en conflicto con un campo que pertenece al perfil es un error y no una sobrescritura.

<a id="path-rules"></a>
## Reglas de rutas

| Ruta | Comportamiento de la API | Comportamiento de la CLI |
| --- | --- | --- |
| `Installation.RootPath` | Se usa tal como se indica: en las operaciones de archivo, las rutas relativas se resuelven respecto del directorio de trabajo del proceso. Pase una ruta absoluta. El lease, el journal y el catálogo de contenido la normalizan con `Path.GetFullPath`. | `.` o vacío = el directorio que contiene `OmsiLaunch.exe`, nunca la carpeta de trabajo del llamador; un argumento de instalación explícito prevalece sobre el spec; el resultado se convierte en absoluto. |
| `Presentation.CustomAssetDirectory` | Absoluta, o relativa a `Installation.RootPath`. Debe existir. | Igual (`/splash-assets`). Una ruta `assets` de un perfil de sesión queda confinada al paquete del perfil y se almacena como absoluta. |
| `InternetTextures.OverrideProfilePath` | Se resuelve con `Path.GetFullPath`, es decir, relativa al directorio de trabajo del proceso, no a la raíz de la instalación. Debe existir. | Igual (`/internet-textures-profile`). Una ruta `profile` de un perfil de sesión queda confinada al paquete y se almacena como absoluta. |
| Líneas de destino ITX | Relativas a la raíz de la instalación; deben contener un componente `Texture\`; sin raíz, sin `..`, sin `\` inicial, sin componentes que sean junctions ni enlaces simbólicos. | Igual. |
| Identidades de contenido (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Relativas a la instalación, sin distinguir mayúsculas de minúsculas, se acepta `/`; nunca absolutas. | Igual. |

<a id="carried-but-not-applied"></a>
## Transportado pero no aplicado

| Campo | Efecto actual | Estabilidad |
| --- | --- | --- |
| `Installation.ExpectedExecutableSha256` | ninguno (el host calcula el hash de `Omsi.exe` y lo compara con el perfil de build) | `PARTIAL` |
| `Behavior.RestoreConfiguration` | ninguno (la restauración siempre se ejecuta) | `PARTIAL` |
| `Behavior.ShutdownTimeoutSeconds` | ninguno (terminación forzada; `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`) | `PARTIAL` |
| `Diagnostics.*` | ninguno (la traza del host siempre se escribe) | `PARTIAL` |
| `Input.KeyboardDocument`, `Input.ControllerDocument` | plan no ejecutable cuando están establecidos | `PARTIAL` |
| `Date`, `Time`, `Year` (modo distinto de `Unset`) | plan no ejecutable (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `Weather` (modo distinto de `Unset`) | plan no ejecutable (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `PlayerVehicle.*` (cualquier campo establecido) | contenido resuelto para los diagnósticos, plan no ejecutable (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `World.EntrypointIdentity` | plan no ejecutable (`RUNTIME_PARTIAL`) | `PARTIAL` |
| `World.Mode` = `LastMapState` / `LastSituation` | plan no ejecutable (`UNSUPPORTED_FOR_CURRENT_PROFILE`) | `UNAVAILABLE` |
| `SessionProfile` | solo diagnóstico de procedencia | `STABLE_BETA` |
