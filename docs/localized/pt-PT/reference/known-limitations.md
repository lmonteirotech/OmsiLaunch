# Limitações conhecidas

<!-- l10n: source=reference/known-limitations.md -->
> Tradução da [página original em inglês](../../../reference/known-limitations.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página enumera, a partir do código, tudo o que no OmsiLaunch 0.1.0-beta3 é `UNAVAILABLE`, `PARTIAL` ou um risco aceite, para que os utilizadores e integradores não construam sobre comportamentos que o produto não oferece. Cada linha indica a limitação, a sua estabilidade, a razão da sua existência e onde está documentada em detalhe. A documentação inglesa é normativa; as cópias localizadas em `docs/localized/` não são mantidas ao mesmo nível e podem estar desatualizadas (ver a última secção).

<a id="compatibility"></a>
## Compatibilidade

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Só é suportado `Omsi23004_692EBFBF` (`692EBFBF...6243`); o hash Steam LAA `7DAB063D...D759` está na lista de permissões; a sua impressão digital e o plano foram validados com uma cópia controlada, mas o jogo exige uma instalação Steam genuína (a imagem é o executável Steam protegido por DRM) | `PARTIAL` para Steam LAA | [compatibilidade](compatibility.md) |
| Apenas Windows 10+ x64; sem Windows 7/8, sem XP, sem ARM64 | `UNAVAILABLE` | [compatibilidade](compatibility.md) |
| O plugin precisa do runtime .NET 6 **x86**, além do runtime x64 usado pelo controlador | | [compatibilidade](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Início do mundo e opções de lançamento

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last`, `WorldMode.LastMapState`) não está implementado; nunca é usada em substituição uma alternativa `.osn` baseada em carimbos temporais | `UNAVAILABLE` (BI-006) | Diagnóstico do plano `OL_E_CAPABILITY_UNAVAILABLE` |
| Data, hora e ano explícitos ou do sistema (`/date`, `/time`, `/year`, `new.date`/`new.time`/`new.year` do perfil, `DateSpec`/`TimeSpec`/`YearSpec`) são transportados na especificação, mas tornam o plano não executável; o plugin rejeita modos diferentes de `Unset` | `UNAVAILABLE` (`STATICALLY_PARTIAL`) | [perfis de sessão](session-profiles.md), [launchspec](launchspec.md) |
| Predefinição de meteorologia, ICAO e meteorologia real atual no início (`/weather*`, `new.weather`) | `UNAVAILABLE` (`STATICALLY_PARTIAL`, BI-003) | como acima |
| Modelo, repaint, HOF, número de frota e matrícula do veículo do jogador no início (família `/vehicle`, `PlayerVehicleSpec`) são resolvidos face ao catálogo de conteúdos, mas não aplicados; pedi-los torna o plano não executável; a atribuição headless determinística do PlayerVehicle é uma extensão futura | `UNAVAILABLE` (BI-007) | `player.assign-headless` em [capacidades](capabilities.md) |
| O ponto de entrada por identidade (`/entrypoint:<identity>`) não é correlacionado com a lista apresentada pelo OMSI; usar `/entrypoint-index` | `PARTIAL` (BI-001) | capacidade do plano `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| Os overlays de documentos de teclado e controlador (`InputSpec`, `Environment.Keyboard`, `Environment.Controllers`) são analisados, mas nunca aplicados por uma sessão | `UNAVAILABLE` (BI-005) | Capacidades `input.*` |
| `LaunchBehaviorSpec.RestoreConfiguration` e `InstallationSpec.ExpectedExecutableSha256` estão declarados, mas nunca são lidos | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds` (`/shutdown-timeout`, `shutdown-timeout` do perfil) é aceite e transportado, mas não é consumido pelo supervisor | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [ciclo de vida da sessão](../concepts/session-lifecycle.md) |
| `/quiet` e `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| As flags de diagnóstico (`/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native`) preenchem `DiagnosticsSpec`; o efeito visível limita-se ao trace do host em `.omsilaunch\diagnostics` | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch` e `/d3d-batch` são harnesses de validação | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fim da sessão e controlo de processos

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| A paragem da sessão é uma terminação forçada: `session.stop`, "End session" na área de notificação, Ctrl+C e `CloseAsync` conduzem todos a `TerminateProcess`. A rotina de encerramento do OMSI não é executada, o OMSI não reescreve `options.cfg` nem os seus registos ao sair, e qualquer estado do OMSI não guardado perde-se. Isto é deliberado: impede que o OMSI escreva por cima dos ficheiros restaurados. | por conceção | [ciclo de vida da sessão](../concepts/session-lifecycle.md) |
| O encerramento cooperativo por WM_CLOSE com timeout e terminação como alternativa não está implementado | `UNAVAILABLE` (decisão de produto, S-11; o OMSI ignorou `WM_CLOSE` enviado à sua janela principal na ronda de fecho de runtime) | [estado da validação em runtime](../status/runtime-validation-status.md) |
| Ao fechar a consola ou ao terminar a sessão do Windows (logoff), o proprietário dispõe de um orçamento de 4 s para parar e restaurar; o que ficar por fazer é recuperado pelo journal no início seguinte | fecho da consola validado em runtime; logoff não exercitado | [transações e recuperação](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transação, recuperação e lease

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| O lease da instalação é um semáforo `Local\`: um proprietário por instalação **por sessão de início de sessão (logon)**; não é imposto entre utilizadores; não é libertado enquanto outro processo mantiver um handle; qualquer processo do mesmo utilizador pode reter o nome | risco aceite (S-18) | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A recuperação é recusada (`OL_E_INSTALLATION_BUSY`) enquanto o processo do OMSI registado no journal, ou, para um journal sem PID, qualquer `Omsi.exe` dessa raiz, estiver em execução | por conceção | como acima |
| Um caminho de overlay originalmente ausente cujo conteúdo mudou durante a sessão bloqueia o restauro (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) até ser inspecionado | por conceção | como acima |
| Os journals anteriores às impressões digitais de propriedade só podem ser fechados por uma sessão com bytes planeados idênticos (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | como acima |
| Só são restaurados os caminhos pertencentes à sessão. As escritas próprias do OMSI durante uma sessão (`[last_map]` de `options.cfg` quando nenhuma definição sobrepõe `options.cfg`, `Texture\standard.ipr`, caches, `laststn.osn`, perfil de motorista, registos) persistem, tal como após um início direto do OMSI | por conceção | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A remoção de um `closecheck` obsoleto antes de uma sessão é permanente (registada, não restaurada) quando `SuppressStaleClosecheckWarning` é true | por conceção | como acima |

<a id="runtime-control"></a>
## Controlo de runtime

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| `weather.set` é rejeitado (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`): o OMSI sobrescreve ambos os candidatos de vento do perfil no seu ciclo meteorológico seguinte | `UNAVAILABLE` | [capacidades](capabilities.md) |
| Escritas no calendário (`SetActualDateTime`) | `UNAVAILABLE` (BI-002) | `calendar.set-actual-date-time` |
| Escritas de variáveis de texto, triggers com nome, triggers de som (propriedade de strings geridas do Delphi) | `UNAVAILABLE` (BI-004) | `scripts.string.read` é só de leitura |
| Sem relocalização de veículos, sem reassociação espacial entre tiles, sem autoridade de transformação segura para o ODE; os campos de posição são só de leitura | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` precisam de um PlayerVehicle; o início headless de NEW_MAP não tem nenhum (uma situação guardada fornece um) | por conceção (BI-007) | RV-004 |
| As mutações de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, texturas D3D) não são registadas no journal nem restauradas | por conceção | [controlo de runtime](runtime-control.md) |
| Ponto cego da impressão digital dos handles: um objeto da mesma classe e definição recriado no mesmo endereço entre duas leituras de lista não é detetado como obsoleto; o tempo de vida com remoção natural (RV-002) não tem produtor seguro em runtime e permanece coberto offline | `PARTIAL` | [controlo de runtime](runtime-control.md) |
| Os resultados estão limitados pela mailbox de 64 KiB: as listas longas são truncadas (`truncated=true`); os dados de píxeis estão limitados a 48 KiB por `d3d.texture.update` | por conceção | [capacidades](capabilities.md) |
| Canal de pedido único: um pedido de cada vez por sessão; um slot ocupado resulta em `OL_E_RUNTIME_CHANNEL_BUSY`; os ids de pedido não podem ser reutilizados | por conceção | [controlo de runtime](runtime-control.md) |
| A telemetria é um slot com o valor mais recente: rajadas mais rápidas do que a amostragem de 100 ms do host podem perder eventos intermédios (os números de sequência mantêm distintos os eventos consecutivos idênticos; as amostras rasgadas são ignoradas) | `PARTIAL` | [plugin permanente](../concepts/permanent-plugin.md) |
| A reposição do dispositivo D3D foi observada em runtime (`resetting`, `restored`, invalidação por geração); não foi produzida uma transição `lost` distinta, porque o dispositivo do OMSI passou diretamente a `DEVICENOTRESET` | `PARTIAL` (RV-007) | [estado da validação em runtime](../status/runtime-validation-status.md) |
| Os resultados de listas limitadas (as operações `timetable.*.list`, `vehicle.variables.list`, `vehicle.string-variables.list`) devolvem no máximo as linhas que cabem no slot de runtime de 64 KiB; as restantes são omitidas, com `truncated=true` e um `returned_count` menor (auditoria de documentação BUG-05). Não há paginação nesta versão | por conceção | [capacidades](capabilities.md) |
| `timetable.logs.read`, `road-vehicles.list`, `humans.list`, `vehicle.constants.list` e `vehicle.curves.list` não são limitadas: um resultado maior do que o slot falha com `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (não observado em nenhuma delas nos mapas testados) | `PARTIAL` | [capacidades](capabilities.md) |
| As strings de evidência autodeclaradas (`RuntimeValidation` de `PublicCapabilityRegistry`, `EvidenceState` de `GetCapabilitiesAsync`) não foram atualizadas após a ronda de fecho de runtime: `camera.lock` ainda indica `STATICALLY_VALIDATED` e `runtime.d3d.lifecycle.reset` indica `IMPLEMENTED_NOT_RUNTIME_VALIDATED`. A página [estado da validação em runtime](../status/runtime-validation-status.md) é a referência autorizada | atraso da documentação, não uma diferença de comportamento | [capacidades](capabilities.md) |
| Alguns campos avançados do grafo de mapa/tile/caminho/objeto não são expostos; os leitores de runtime são snapshots tipados condicionados ao perfil, nunca acesso arbitrário à memória | por conceção | [capacidades](capabilities.md) |
| As leituras de memória dentro do processo seguem o padrão verificar-e-depois-usar contra um OMSI em execução; uma mutação concorrente do OMSI entre a verificação e a leitura pode produzir um snapshot inconsistente (`OL_E_RUNTIME_OPERATION_FAILED`) | risco aceite (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Plano de controlo local e modelo de confiança

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Modelo de confiança do mesmo utilizador: o named pipe (`CurrentUserOnly`), os mapeamentos de memória de handoff/telemetria/runtime e o semáforo do lease são acessíveis a qualquer processo do mesmo utilizador do Windows. Um processo desses pode ler o estado, parar a sessão ou executar operações de runtime depois de ler o `session_id`. | risco aceite (S-06, S-30) | [controlo local](local-control.md) |
| O endpoint de controlo só existe enquanto o proprietário está em `Running`; um cliente vê `OL_E_NO_ACTIVE_SESSION` (saída 4) durante o arranque e após o fim da sessão | por conceção | [controlo local](local-control.md) |
| Se outro processo já for dono do nome do pipe, o proprietário continua a executar sem endpoint (`ListenFault`), e um segundo lançamento pode comunicar erradamente `OL_E_SESSION_ALREADY_ACTIVE` | risco aceite | [controlo local](local-control.md) |
| `.omsilaunch\` herda a ACL da raiz do OMSI; não é aplicado nenhum controlo de acesso explícito | risco aceite (S-31) | [transações e recuperação](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnóstico e saída

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Os diagnósticos são apenas ficheiros locais (`.omsilaunch\diagnostics`); nada é enviado e não existe comunicação remota | por conceção | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A retenção mantém as 50 sessões mais recentes; os diagnósticos mais antigos com prefixo de sessão são eliminados quando uma nova sessão se inicia | por conceção | como acima |
| A saída JSON e os diagnósticos incluem caminhos da instalação (`RootPath`, diretórios de recursos, caminhos `.itx`) | por conceção (dados locais) | |
| A caixa de diálogo de falha da sessão de `OmsiLaunchW.exe` mostra como mensagem os dados de falha do plugin (por exemplo `{"name":"world.failed",...}`) em vez de uma frase; a linha `Code:` está correta | cosmético | [área de notificação do Windows](windows-tray.md) |
| Um pedido D3D rejeitado pela ponte nativa antes de qualquer chamada Direct3D comunica `native_status` corretamente, mas o seu texto `detail` indica `HRESULT 0x00000000` | cosmético | [capacidades](capabilities.md) |
| A janela de estado da área de notificação é um snapshot da sessão planeada tirado quando é aberta; não se atualiza e não mostra valores do OMSI em tempo real | por conceção | [área de notificação do Windows](windows-tray.md) |

<a id="documentation"></a>
## Documentação

As páginas inglesas em `docs/` são a documentação normativa desta versão. `docs/localized/<locale>/` contém traduções das mesmas páginas da `0.1.0-beta3` (ver [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); onde uma tradução diferir do texto inglês, o texto inglês e o código são a referência autorizada. As páginas históricas e legadas aí listadas estão disponíveis apenas em inglês.

Relacionado: [capacidades](capabilities.md), [estado da validação em runtime](../status/runtime-validation-status.md), [erros](errors.md).
