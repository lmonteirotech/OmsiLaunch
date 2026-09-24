# Referência do LaunchSpec

<!-- l10n: source=reference/launchspec.md -->
> Tradução da [página original em inglês](../../../reference/launchspec.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página é a referência normativa de `LaunchSpec`, o registro de requisição que descreve uma sessão do OmsiLaunch: seu formato em C# (`OmsiLaunch.Api`), sua forma de arquivo JSON carregada pela CLI (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), cada propriedade com seu tipo, valor padrão, regra de validação e efeito atual, as regras de validação que tornam um plano não apto para execução e a precedência entre flags da CLI, arquivos de especificação e perfis de sessão. Somente o que o código atual faz está documentado.

Páginas relacionadas: [API pública](public-api.md), [referência da CLI](cli.md), [perfis de sessão](session-profiles.md), [códigos de erro](errors.md), [ciclo de vida da sessão](../concepts/session-lifecycle.md), [capacidades](capabilities.md).

<a id="where-a-launchspec-comes-from"></a>
## De onde vem um LaunchSpec

| Origem | Como se torna um `LaunchSpec` |
| --- | --- |
| API | O integrador constrói o registro e o passa para `PlanSessionAsync`. |
| Flags da CLI | `CliInput.BuildSpecAsync` parte de padrões internos (NEW_MAP, tudo não definido, padrões de `Behavior`) e aplica as flags. |
| Arquivo JSON `/spec:<path>` | Carregado por `LaunchSpecJson.LoadAsync` e depois usado como base que as flags da CLI sobrescrevem (ver [precedência](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Perfil de sessão (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` grava na base o mundo, as configurações, a apresentação, as texturas da internet e o comportamento do perfil e registra os metadados de `SessionProfile`. |

O [exemplo completo](#complete-example) abaixo foi verificado contra o formato do registro. Uma cópia do exemplo mínimo é distribuída como `examples/release-session.example.json` (e `.omsilaunch\examples\release-session.example.json` no pacote de release).

<a id="json-form"></a>
## Forma JSON

| Regra | Detalhe |
| --- | --- |
| Serializador | `System.Text.Json` com `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; nenhum conversor é registrado. |
| Nomes de propriedades | Os nomes das propriedades em C# (`Installation`, `RootPath`, ...). A correspondência no carregamento não diferencia maiúsculas de minúsculas; a CLI os grava em PascalCase. |
| Enums | Inteiros (não há conversor de enum para string). `"Mode": 0` é válido; `"Mode": "NewMap"` é rejeitado como JSON malformado. Os valores estão listados em [Enumerações](#enumerations). |
| `OptionalValue<T>` | Um objeto `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (o valor é ignorado), 1 = `Set` (o valor deve estar presente e não ser nulo; um `Set` com valor nulo não é validado e se comporta como valor inválido). Um membro `OptionalValue` omitido é `Unset`. O membro somente leitura `IsSet` aparece na saída gravada pela CLI e é aceito e ignorado no carregamento. |
| Registros opcionais | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile` podem ser `null` ou omitidos; os acessores `Effective*` substituem por padrões. |
| Registros obrigatórios | `Installation`, `World`, `Date`, `Time`, `Environment` (com os oito dicionários, use `{}`), `Behavior` devem ser objetos presentes. Eles não são validados: um `null` ou ausente falha mais tarde com uma referência nula, que a CLI reporta como `OL_E_INTERNAL` (saída 10) ou `OL_E_INVALID_ARGUMENT` (saída 2). |
| Propriedades desconhecidas | Rejeitadas antes do binding: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (o caminho usa os nomes de membros como escritos no arquivo). O conteúdo dos dicionários (`Environment.*`) não é verificado como propriedades. |
| Raiz | Deve ser um objeto JSON: `OL_E_SPEC_INVALID`. Profundidade máxima de aninhamento 32. |
| Tamanho do arquivo | No máximo 1 MiB (1 048 576 bytes): `OL_E_SPEC_TOO_LARGE`. Arquivo ausente: `OL_E_SPEC_NOT_FOUND`. |
| Comentários e vírgulas finais | Comentários `//` e `/* */` e vírgulas finais são aceitos. |
| JSON malformado | A exceção do parser não é traduzida: a CLI reporta `OL_E_INTERNAL` com código de saída 10. |
| Codificação | UTF-8 (um BOM é tolerado pelo leitor). Barras invertidas em identidades devem ser escapadas (`"maps\\Grundorf\\global.cfg"`); barras normais são aceitas para identidades de mapa, situação, veículo e HOF. |

<a id="complete-example"></a>
## Exemplo completo

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

Uma data explícita, quando o build a suporta, é escrita como `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` e uma hora como `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. Neste build, ambas tornam o plano não apto para execução (ver abaixo).

<a id="property-reference"></a>
## Referência de propriedades

A coluna "Consumido" indica o que o código atual faz com o valor. A estabilidade usa o vocabulário da [página da API pública](public-api.md#stability-vocabulary).

<a id="launchspec-root"></a>
### `LaunchSpec` (raiz)

| Propriedade | Tipo JSON | Obrigatório | Padrão quando omitido | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `Installation` | objeto `InstallationSpec` | sim | nenhum | sim | `STABLE_BETA` |
| `World` | objeto `WorldSpec` | sim | nenhum | sim | `STABLE_BETA` |
| `Date` | objeto `DateSpec` | sim | nenhum | validado; qualquer modo exceto `Unset` é não apto para execução | `PARTIAL` |
| `Time` | objeto `TimeSpec` | sim | nenhum | validado; qualquer modo exceto `Unset` é não apto para execução | `PARTIAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | não | `Unset` | resolvido para diagnósticos; qualquer campo definido é não apto para execução | `PARTIAL` |
| `Environment` | objeto `EnvironmentSpec` | sim | nenhum | sim (overlay semântico de `options.cfg`) | `STABLE_BETA` |
| `Behavior` | objeto `LaunchBehaviorSpec` | sim | nenhum | em parte (ver registro) | `STABLE_BETA` / `PARTIAL` |
| `Year` | objeto `YearSpec` ou null | não | `null` → `EffectiveYear` = modo `Unset` | qualquer modo exceto `Unset` é não apto para execução | `PARTIAL` |
| `Weather` | objeto `WeatherSpec` ou null | não | `null` → `EffectiveWeather` = modo `Unset` | qualquer modo exceto `Unset` é não apto para execução | `PARTIAL` |
| `Input` | objeto `InputSpec` ou null | não | `null` → `EffectiveInput` = ambos não definidos | qualquer documento definido é não apto para execução | `PARTIAL` |
| `Diagnostics` | objeto `DiagnosticsSpec` ou null | não | `null` → `EffectiveDiagnostics` = padrões | apenas transportado | `PARTIAL` |
| `Presentation` | objeto `SessionPresentationSpec` ou null | não | `null` → `EffectivePresentation` = splash gerenciado, sem idioma, sem diretório personalizado, bandeja exibida | sim | `STABLE_BETA` |
| `InternetTextures` | objeto `InternetTexturesSpec` ou null | não | `null` → `EffectiveInternetTextures` = `Native` | sim | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | objeto `SessionProfileMetadata` ou null | não | `null` | apenas procedência (diagnóstico de plano `session_profile.selected`) | `STABLE_BETA` |

Acessores somente leitura (presentes na saída JSON da CLI, ignorados no carregamento): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`.

### `InstallationSpec`

| Propriedade | Tipo | Padrão | Valores válidos | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | obrigatório | Diretório que contém `Omsi.exe` e `plugins\`. Ver [regras de caminho](#path-rules). Vazio/só espaços → `OL_E_INSTALLATION_NOT_FOUND`. | sim | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string ou null | `null` | Qualquer string. | Nenhum consumidor no código atual: o host sempre calcula o hash do `Omsi.exe` e o compara com o perfil de build, nunca com este valor. | `PARTIAL` (transportado, atualmente sem efeito) |

### `WorldSpec`

| Propriedade | Tipo | Padrão | Valores válidos | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | obrigatório | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` é um alias obsoleto com o mesmo valor 2). | sim; `LastMapState` → `OL_E_CAPABILITY_UNAVAILABLE` | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | Para `NewMap`: obrigatório, no formato `maps\<dir>\global.cfg` (sem diferenciar maiúsculas de minúsculas, `/` aceito, sem `..`) e instalado. Ignorado para `SavedSituation` (o `.osn` fornece o mapa). | sim (handoff) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | Para `SavedSituation`: obrigatório, uma identidade `situations\...\<file>.osn` instalada (como retornada por `DiscoverAsync(Situations)` / `/list:situations`). | sim (handoff) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | Para `NewMap` sem `EntrypointIdentity`: obrigatório, `>= 0`, um índice na lista de pontos de entrada apresentada pelo OMSI para o mapa. Enviado ao plugin como `-1` quando não definido. | sim (handoff) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | Um rótulo bruto de ponto de entrada ou uma identidade de descoberta. Defini-lo torna o plano não apto para execução (`world.entrypoint-identity`, `RUNTIME_PARTIAL`, `OL_E_CAPABILITY_UNAVAILABLE`). | transportado | `PARTIAL` |
| `Entrypoint` | `EntrypointSpec` (somente leitura) | calculado | `Mode` = `Identity` quando `EntrypointIdentity` está definido, senão `PresentedIndex` quando o índice está definido, senão `Unset`; `PresentedIndex`, `Identity` espelham as entradas. | derivado | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Propriedade | Tipo | Padrão | Valores válidos | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | obrigatório (`Year`: `0` quando o registro é null) | `0` `Unset`, `1` `Explicit`, `2` `System`. | `Explicit`/`System` → entrada `unsupported` (`world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `STATICALLY_PARTIAL`) e `OL_E_CAPABILITY_UNAVAILABLE`. Os modos também são copiados para o handoff de inicialização, que o plugin rejeita quando não são `Unset` (nunca alcançado, pois o plano é não apto para execução). | `PARTIAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: `Year`, `Month` 1..12, `Day` 1..31; `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59. Deve estar definido quando `Mode` é `Explicit` (caso contrário, `OL_E_DATE_TIME_APPLY_FAILED`) e não deve estar definido quando `Mode` não é `Explicit` (`OL_E_INVALID_ARGUMENT`). `YearSpec.Value` não é validado. | apenas validado | `PARTIAL` |

### `WeatherSpec`

| Propriedade | Tipo | Padrão | Valores válidos | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0` quando o registro é null | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Qualquer modo exceto `Unset` → entrada não suportada `weather` e `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Nome da predefinição (não validado). | transportado | `PARTIAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | Código ICAO (não validado). | transportado | `PARTIAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec` (dentro de `PlayerVehicle`)

| Propriedade | Tipo | Padrão | Valores válidos | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Identidade `Vehicles\...\<file>.bus` instalada, senão `OL_E_VEHICLE_NOT_FOUND`. | resolvido em `ResolvedContent`; depois `player-vehicle.model` não suportado → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | Uma identidade de pintura (repaint) de `Model` (`<cti>#item:<n>`), senão `OL_E_REPAINT_NOT_FOUND`; verificada apenas quando `Model` está definido. | idem | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | `Vehicles\...\<file>.hof` instalado, senão `OL_E_HOF_NOT_FOUND`. | idem | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Qualquer string. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Qualquer string. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (somente leitura) | calculado | `true` quando `Model` está definido. `PlayerVehicle.IsSet` é o que o handoff transporta como `PlayerVehicleEnabled`. | derivado | `PARTIAL` |

Um `PlayerVehicle` com `Presence` 1 e todos os campos não definidos é aceito e não tem efeito. Qualquer campo definido torna o plano não apto para execução neste build (`STATICALLY_PARTIAL`).

### `EnvironmentSpec`

| Propriedade | Tipo | Padrão | Consumido | Estabilidade |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | `IReadOnlyDictionary<string, OptionalValue<string>>` cada um, obrigatório (`{}` quando vazio) | nenhum | sim | `STABLE_BETA` |

Os oito grupos são concatenados; o grupo em que uma chave é colocada não tem efeito. Cada entrada com `Presence` 1 é uma configuração semântica de `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); a chave seleciona o arquivo de destino (`options.cfg` para todas as chaves atuais) e o token. O planejamento verifica se a chave existe (`OL_E_UNKNOWN_SETTING`) e é gravável (`OL_E_SETTING_NOT_WRITABLE`); o valor é validado apenas no início (`OL_E_INVALID_SETTING_VALUE`, reportado como uma sessão `Failed` com `OL_E_START_SESSION`). As chaves não diferenciam maiúsculas de minúsculas. Entradas não definidas são ignoradas. A flag da CLI `/set:<key>=<value>` grava em `General`; as `settings` do perfil de sessão também são mescladas em `General`.

| Chave | Valor | Observações |
| --- | --- | --- |
| `general.language` | string | token `[language]` |
| `general.radio` | string | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | tokens de presença (`autoSave` é o inverso de `noAutoSave`) |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | inteiro 10..200 | |
| `graphics.tileDistance` | inteiro 1..20 | |
| `graphics.maxObjectDistanceMeters` | número 20..5000 | |
| `graphics.minObjectScreenPercent` | número 0..10 | armazenado dividido por 100 |
| `graphics.minReflectionObjectScreenPercent` | número 0..50 | armazenado dividido por 100 |
| `graphics.maxObjectComplexity` | inteiro 0..3 | |
| `graphics.maxMapComplexity` | inteiro 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | tokens de presença |
| `graphics.stencilShadows` | `true` / `false` | gravado como `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | um bloco `smokesystems` |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | tokens de presença |
| `simulation.ticketSelling` | inteiro 0..2 | |
| `simulation.maintenance` | inteiro 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | dois tokens do OMSI de uma vez (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | tokens de presença |
| `traffic.randomVehicles` | inteiro 0..1000 | componente 0 do bloco multilinha `AIMaxCountRandom` (validado em runtime, matriz RV-005) |
| `traffic.humans` | inteiro 0..1000 | componente 1 de `AIMaxCountRandom` |
| `traffic.factorPercent` | número 1..300 | |
| `traffic.parkedVehiclesPercent` | número 0..100 | |
| `traffic.scheduledVehicles` | número 0..1000 | |
| `traffic.scheduledLinePriority` | número 1..4 | |
| `traffic.passengerFactorPercent` | número 0..200 | |
| `sound.stereo` | número 0..100 | |
| `sound.maxSimultaneousSounds` | número 5..1000 | |
| `sound.masterVolume` | número 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | rejeitado | conhecido, mas não gravável → `OL_E_SETTING_NOT_WRITABLE` |

Os arquivos modificados mantêm sua codificação (bytes Windows-1252 preservados; UTF-8/UTF-16 marcados com BOM respeitados) e suas quebras de linha.

### `LaunchBehaviorSpec`

| Propriedade | Tipo | Padrão | Valores válidos | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | qualquer | Nenhum consumidor: os arquivos pertencentes à sessão são sempre restaurados exatamente. | `PARTIAL` (transportado, atualmente sem efeito) |
| `SuppressStaleClosecheckWarning` | bool | `true` | qualquer | `true`: um arquivo `closecheck` que existe antes da sessão é removido permanentemente no início (diagnóstico `closecheck.stale-removed` com seu SHA-256; falha `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: um `closecheck` existente é deixado intacto e não é uma exclusão da sessão. O `closecheck` que o OMSI grava durante a sessão é sempre removido na restauração. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (caso contrário, `ArgumentOutOfRangeException` vindo de `StartSessionAsync`; a CLI `/startup-timeout` impõe 1..600; perfis exigem > 0). Tempo disponível desde o início do supervisor até `Running`; ao expirar, a sessão falha com `OL_E_STARTUP_TIMEOUT` (plugin iniciado) ou `OL_E_PLUGIN_NOT_LOADED`. | sim | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | qualquer int (CLI `/shutdown-timeout` 1..600) | Nenhum consumidor: o supervisor encerra o OMSI imediatamente com `TerminateProcess`; não há espera por um encerramento cooperativo. | `PARTIAL` (transportado, atualmente sem efeito) |

### `InputSpec`

| Propriedade | Tipo | Padrão | Consumido | Estabilidade |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Definido → `input.keyboard` não suportado (`STATICALLY_PARTIAL`) e `OL_E_CAPABILITY_UNAVAILABLE`. A execução de PATCH/REPLACE do teclado não está implementada. | `PARTIAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Definido → `input.controller` não suportado e `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |

### `DiagnosticsSpec`

| Propriedade | Tipo | Padrão | Consumido | Estabilidade |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | Nenhum consumidor em `src/`. O trace do host `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` é sempre gravado. | `PARTIAL` (transportado, atualmente sem efeito) |
| `Verbose` | bool | `false` | Nenhum consumidor. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | Nenhum consumidor. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | Nenhum consumidor. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Nenhum consumidor. | `PARTIAL` |
| `NativeTrace` | bool | `false` | Nenhum consumidor. | `PARTIAL` |

As flags da CLI `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` preenchem esses booleanos (`/logall` define `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`); eles são combinados por OR com os valores da especificação.

### `SessionPresentationSpec`

| Propriedade | Tipo | Padrão | Valores válidos | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (alias `Native`): os arquivos de splash do próprio OMSI não são tocados. `1` `Managed`: o OmsiLaunch aplica overlays de `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<LANG>.bmp` durante a sessão (restaurados exatamente depois). | sim | `STABLE_BETA` (matriz RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (sem diferenciar maiúsculas de minúsculas); qualquer outro valor é normalizado para `ENG`. Quando não definido, o valor `[language]` de `options.cfg` é lido e normalizado da mesma forma. | sim (apenas splash gerenciado) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Diretório que contém `ENG.bmp` e `<LANG>.bmp` (640×480, BMP de 24 bits). Ver [regras de caminho](#path-rules). Diretório ausente: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; arquivo ausente: `OL_E_SPLASH_ASSET_MISSING`; formato incorreto: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. Quando não definido, é usado `<root>\.omsilaunch\assets\splash` (preenchido uma vez a partir do pacote); caso contrário, o `assets\splash` empacotado. | sim (apenas splash gerenciado) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` suprime o indicador autônomo na bandeja do Windows do proprietário CLI. | Apenas proprietário CLI; a API não tem bandeja. Não há flag da CLI; só pode vir de um arquivo de especificação. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Propriedade | Tipo | Padrão | Valores válidos | Consumido | Estabilidade |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native`: nada muda. `1` `Disabled`: o plugin suprime o downloader interno ao processo do OMSI (telemetria `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: o perfil `.itx` é aplicado como overlay em `Texture\standard.itx`; seus arquivos de destino e `Texture\standard.ipr` se tornam exclusões da sessão. | sim | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Obrigatório para `Override` (`OL_E_ITX_PROFILE_REQUIRED`). Um arquivo de texto com pares de linhas: URL absoluta `http`/`https` e, em seguida, um caminho de destino relativo à raiz da instalação que contém um componente `Texture\`, não é enraizado, não tem `..`, não começa com `\` e não atravessa nenhuma junction/symlink (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Ver [regras de caminho](#path-rules). | sim | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Propriedade | Tipo | Consumido | Estabilidade |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | strings / int | Registrados no diagnóstico de plano `session_profile.selected` (`Data["session_profile.*"]`). Não são consumidos de outra forma; normalmente preenchidos pelo compilador de perfis de sessão, não à mão. | `STABLE_BETA` |

<a id="enumerations"></a>
## Enumerações

| Enum | Valores (inteiro JSON) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsoleto; é o ramo nativo de último estado do mapa do OMSI, nunca o `.osn` mais recente) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (somente leitura `Entrypoint.Mode`) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Inteiros fora do intervalo declarado são armazenados como estão pelo serializador e se comportam como valores desconhecidos (por exemplo, um `WorldMode` desconhecido não é nem NEW_MAP nem SAVED_SITUATION e gera um plano sem capacidade de mundo; o plugin o rejeitaria, mas a CLI substitui o modo de qualquer forma, ver precedência).

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## Regras de validação e diagnósticos de não aptidão para execução

`PlanSessionAsync` executa `LaunchValidation.Validate` e depois `SessionPlanner.PlanAsync`. Um plano é apto para execução exatamente quando nenhum código de diagnóstico começa com `OL_E_`. O conjunto completo:

| Diagnóstico | Condição | Origem |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` vazio ou só com espaços | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` sem valor definido ou com mês/dia fora do intervalo; `Time.Mode` = `Explicit` sem valor definido ou com hora/minuto/segundo fora do intervalo | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` ou `Time.Value` definido enquanto o modo não é `Explicit` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` com `MapIdentity` não definido ou fora do formato `maps\...\global.cfg` (validação); `NewMap` com uma identidade que não está instalada (planejador) | ambos |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` sem `EntrypointIdentity` e com `PresentedEntrypointIndex` não definido ou negativo | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, mapa instalado, sem `EntrypointIdentity`, `PresentedEntrypointIndex` não definido (`world.presented-entrypoint` indisponível) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` sem `SituationIdentity` (validação) ou com uma identidade que não está instalada (planejador) | ambos |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: o mapa indicado dentro do `.osn` não está instalado | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | não é Windows 10+ em um SO x64 com um processo host x64 (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | diretório raiz ausente, atributo somente leitura definido ou nenhum subdiretório `plugins\` (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | `Omsi.exe` ausente, ou seu tamanho/SHA-256 não é nem a impressão digital do perfil (`692EBFBF...`, 8 503 440 bytes) nem um hash da lista de permitidos (`omsi.profile.OMSI23004`) | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `World.Mode` = `LastMapState`; `EntrypointIdentity` definido; modo de `Date`/`Time`/`Year` diferente de `Unset`; modo de `Weather` diferente de `Unset`; qualquer campo de `PlayerVehicle` definido; `Input.KeyboardDocument` ou `Input.ControllerDocument` definido | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` não instalado (além de `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | uma chave de `Environment` que não está no catálogo / não é gravável | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | a construção do plano de splash/ITX lançou uma exceção; a mensagem traz `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` ou `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | a referência do conjunto de arquivos do plugin (`OmsiLaunchRuntimePaths`) ou o manifesto de release não pode ser carregado (a mensagem pode trazer `OL_E_RELEASE_MANIFEST_INVALID`) | `OmsiLaunchService.PlanSessionAsync` |

Não validado no momento do planejamento (falha no início como uma sessão `Failed` com `OL_E_START_SESSION`): valores de configuração (`OL_E_INVALID_SETTING_VALUE`), integridade do plugin permanente (`OL_E_PERMANENT_PLUGIN_*`), disponibilidade do lease (`OL_E_INSTALLATION_BUSY`), intervalo de `StartupTimeoutSeconds` (lançado por `StartSessionAsync`).

Diagnósticos de plano informativos: `plugin.integrity.reference` (mensagem `manifest` ou `self`), `session_profile.selected`.

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## Precedência: flags da CLI vs arquivo de especificação vs perfil de sessão

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) constrói a especificação efetiva nesta ordem:

1. Base = padrões internos, ou o arquivo `/spec` quando informado.
2. Raiz da instalação = o argumento de instalação explícito, se informado, senão o `RootPath` da base; depois `.`/vazio → diretório do executável, `Path.GetFullPath`. Um argumento de instalação explícito sempre prevalece sobre o `RootPath` da especificação.
3. Perfil de sessão (`/predefined-profile` + `/predefined-profile-index`): argumentos explícitos da CLI que afetam um campo pertencente ao perfil são rejeitados com `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (campos de mundo apenas no modo NEW_MAP; chaves de `/set` presentes na predefinição; flags de splash quando a predefinição tem `presentation`; flags de texturas da internet quando tem `internet-textures`; timeouts quando tem `behavior`). O `World` da base é substituído por um novo (apenas o modo de mundo da CLI sobrevive); em seguida são aplicados o bloco `new:` do perfil (apenas NEW_MAP), `settings` (em `General`), `presentation`, `internet-textures`, `behavior` e os metadados de `SessionProfile`. `compatibility.maps` é imposto para NEW_MAP e SAVED_SITUATION.
4. Mundo: o modo de mundo da CLI sempre prevalece (`/new` padrão, `/saved:<osn>`, `/last`); o `World.Mode` do arquivo de especificação é substituído. Para executar uma situação salva a partir de uma especificação, passe `/saved:`. `/map` e `/entrypoint`/`/entrypoint-index` sobrescrevem a base; uma identidade `/entrypoint` da CLI limpa o índice; `/saved` com `/map` ou flags de ponto de entrada gera `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` sobrescrevem a base quando informados (`system` seleciona `DateTimeMode.System`).
6. `/no-vehicle` limpa `PlayerVehicle`; `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` individuais sobrescrevem campos individuais do veículo do jogador da base.
7. As entradas `/set:<key>=<value>` são adicionadas a `Environment.General` (chave verificada, valor não); os outros sete grupos vêm da base sem alteração.
8. `/startup-timeout` e `/shutdown-timeout` sobrescrevem a base apenas quando informados; caso contrário, aplicam-se a especificação, depois o perfil, depois os padrões 180 s / 30 s. `ShutdownTimeoutSeconds` é `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` no supervisor.
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` sobrescrevem a base quando informados; `SuppressTrayIcon` vem apenas da base.
10. As flags de diagnóstico são combinadas por OR com a base.

Resultado: flag explícita da CLI > perfil de sessão > arquivo de especificação > padrão interno, exceto que uma flag da CLI em conflito com um campo pertencente ao perfil é um erro, e não uma sobrescrita.

<a id="path-rules"></a>
## Regras de caminho

| Caminho | Comportamento na API | Comportamento na CLI |
| --- | --- | --- |
| `Installation.RootPath` | Usado como informado: caminhos relativos são resolvidos em relação ao diretório de trabalho do processo nas operações de arquivo. Passe um caminho absoluto. O lease, o journal e o catálogo de conteúdo o normalizam com `Path.GetFullPath`. | `.` ou vazio = o diretório que contém `OmsiLaunch.exe`, nunca a pasta de trabalho do chamador; um argumento de instalação explícito prevalece sobre a especificação; o resultado é tornado absoluto. |
| `Presentation.CustomAssetDirectory` | Absoluto, ou relativo a `Installation.RootPath`. Deve existir. | Igual (`/splash-assets`). Um caminho `assets` de perfil de sessão fica confinado ao pacote do perfil e é armazenado como absoluto. |
| `InternetTextures.OverrideProfilePath` | Resolvido com `Path.GetFullPath`, ou seja, relativo ao diretório de trabalho do processo, não à raiz da instalação. Deve existir. | Igual (`/internet-textures-profile`). Um caminho `profile` de perfil de sessão fica confinado ao pacote e é armazenado como absoluto. |
| Linhas de destino ITX | Relativas à raiz da instalação; devem conter um componente `Texture\`; sem raiz, sem `..`, sem `\` inicial, sem componente junction/symlink. | Igual. |
| Identidades de conteúdo (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Relativas à instalação, sem diferenciar maiúsculas de minúsculas, `/` aceito; nunca absolutas. | Igual. |

<a id="carried-but-not-applied"></a>
## Transportado, mas não aplicado

| Campo | Efeito atual | Estabilidade |
| --- | --- | --- |
| `Installation.ExpectedExecutableSha256` | nenhum (o host calcula o hash do `Omsi.exe` e o compara com o perfil de build) | `PARTIAL` |
| `Behavior.RestoreConfiguration` | nenhum (a restauração sempre é executada) | `PARTIAL` |
| `Behavior.ShutdownTimeoutSeconds` | nenhum (encerramento forçado; `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`) | `PARTIAL` |
| `Diagnostics.*` | nenhum (trace do host sempre gravado) | `PARTIAL` |
| `Input.KeyboardDocument`, `Input.ControllerDocument` | plano não apto para execução quando definidos | `PARTIAL` |
| `Date`, `Time`, `Year` (modo diferente de `Unset`) | plano não apto para execução (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `Weather` (modo diferente de `Unset`) | plano não apto para execução (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `PlayerVehicle.*` (qualquer campo definido) | conteúdo resolvido para diagnósticos, plano não apto para execução (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `World.EntrypointIdentity` | plano não apto para execução (`RUNTIME_PARTIAL`) | `PARTIAL` |
| `World.Mode` = `LastMapState` / `LastSituation` | plano não apto para execução (`UNSUPPORTED_FOR_CURRENT_PROFILE`) | `UNAVAILABLE` |
| `SessionProfile` | apenas diagnóstico de procedência | `STABLE_BETA` |
