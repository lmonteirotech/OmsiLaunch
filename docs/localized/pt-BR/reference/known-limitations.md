# Limitações conhecidas

<!-- l10n: source=reference/known-limitations.md -->
> Tradução da [página original em inglês](../../../reference/known-limitations.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página lista, a partir do código, tudo o que no OmsiLaunch 0.1.0-beta3 é `UNAVAILABLE`, `PARTIAL` ou um risco aceito, para que usuários e integradores não construam sobre um comportamento que o produto não oferece. Cada linha indica a limitação, sua estabilidade, o motivo de existir e onde ela está documentada em detalhes. A documentação em inglês é normativa; as cópias localizadas em `docs/localized/` não são mantidas no mesmo nível e podem ficar defasadas (veja a última seção).

<a id="compatibility"></a>
## Compatibilidade

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Somente `Omsi23004_692EBFBF` é suportado (`692EBFBF...6243`); o hash Steam LAA `7DAB063D...D759` está na lista de permissões; sua impressão digital e seu plano foram validados com uma cópia controlada, mas o gameplay exige uma instalação Steam genuína (a imagem é o executável Steam protegido por DRM) | `PARTIAL` para Steam LAA | [compatibilidade](compatibility.md) |
| Somente Windows 10+ x64; sem Windows 7/8, sem XP, sem ARM64 | `UNAVAILABLE` | [compatibilidade](compatibility.md) |
| O plugin precisa do runtime .NET 6 **x86**, além do runtime x64 usado pelo controlador | | [compatibilidade](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Início do mundo e opções de inicialização

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last`, `WorldMode.LastMapState`) não está implementado; nenhum fallback de `.osn` baseado em data e hora é usado como substituto | `UNAVAILABLE` (BI-006) | Diagnóstico do plano `OL_E_CAPABILITY_UNAVAILABLE` |
| Data, hora e ano explícitos ou do sistema (`/date`, `/time`, `/year`, perfil `new.date`/`new.time`/`new.year`, `DateSpec`/`TimeSpec`/`YearSpec`) são transportados na spec, mas tornam o plano não apto para execução; o plugin rejeita modos diferentes de `Unset` | `UNAVAILABLE` (`STATICALLY_PARTIAL`) | [perfis de sessão](session-profiles.md), [launchspec](launchspec.md) |
| Predefinição de clima, ICAO e clima real atual no início (`/weather*`, `new.weather`) | `UNAVAILABLE` (`STATICALLY_PARTIAL`, BI-003) | como acima |
| Modelo do veículo do jogador, pintura (repaint), HOF, número de frota e matrícula no início (família `/vehicle`, `PlayerVehicleSpec`) são resolvidos contra o catálogo de conteúdo, mas não aplicados; solicitá-los torna o plano não apto para execução; a atribuição determinística do PlayerVehicle sem interface (headless) é uma extensão futura | `UNAVAILABLE` (BI-007) | `player.assign-headless` em [capacidades](capabilities.md) |
| Ponto de entrada por identidade (`/entrypoint:<identity>`) não é correlacionado com a lista apresentada pelo OMSI; use `/entrypoint-index` | `PARTIAL` (BI-001) | capacidade do plano `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| Overlays dos documentos de teclado e de controles (`InputSpec`, `Environment.Keyboard`, `Environment.Controllers`) são interpretados, mas nunca aplicados por uma sessão | `UNAVAILABLE` (BI-005) | capacidades `input.*` |
| `LaunchBehaviorSpec.RestoreConfiguration` e `InstallationSpec.ExpectedExecutableSha256` são declarados, mas nunca lidos | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds` (`/shutdown-timeout`, perfil `shutdown-timeout`) é aceito e transportado, mas não é consumido pelo supervisor | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [ciclo de vida da sessão](../concepts/session-lifecycle.md) |
| `/quiet` e `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| As flags de diagnóstico (`/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native`) preenchem `DiagnosticsSpec`; o efeito visível se limita ao trace do host em `.omsilaunch\diagnostics` | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` são harnesses de validação | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fim da sessão e controle do processo

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| A parada da sessão é um encerramento forçado: `session.stop`, "End session" na bandeja, Ctrl+C e `CloseAsync` levam todos a `TerminateProcess`. A rotina de encerramento do OMSI não é executada, o OMSI não regrava `options.cfg` nem seus logs ao sair, e qualquer estado não salvo do OMSI é perdido. Isso é deliberado: impede que o OMSI grave por cima dos arquivos restaurados. | por design | [ciclo de vida da sessão](../concepts/session-lifecycle.md) |
| O encerramento cooperativo via WM_CLOSE, com timeout e fallback para encerramento forçado, não está implementado | `UNAVAILABLE` (decisão de produto, S-11; o OMSI ignorou `WM_CLOSE` enviado à sua janela principal na rodada de fechamento em runtime) | [status de validação em runtime](../status/runtime-validation-status.md) |
| No fechamento do console ou no logoff, o proprietário tem um orçamento de 4 s para parar e restaurar; o que restar é recuperado pelo journal no próximo início | fechamento do console validado em runtime; logoff não exercitado | [transações e recuperação](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transação, recuperação e lease

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| O lease da instalação é um semáforo `Local\`: um proprietário por instalação **por sessão de logon**; não é imposto entre usuários diferentes; não é liberado enquanto outro processo mantém um handle; qualquer processo do mesmo usuário pode reter o nome | risco aceito (S-18) | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A recuperação é recusada (`OL_E_INSTALLATION_BUSY`) enquanto o processo do OMSI registrado no journal estiver em execução, ou, para um journal sem PID, enquanto qualquer `Omsi.exe` daquela raiz estiver em execução | por design | como acima |
| Um caminho de overlay originalmente ausente cujo conteúdo mudou durante a sessão bloqueia a restauração (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) até ser inspecionado | por design | como acima |
| Journals anteriores às impressões digitais de propriedade só podem ser fechados por uma sessão com bytes planejados idênticos (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | como acima |
| Somente os caminhos pertencentes à sessão são restaurados. As gravações do próprio OMSI durante uma sessão (`options.cfg` `[last_map]` quando nenhuma configuração sobrepõe `options.cfg`, `Texture\standard.ipr`, caches, `laststn.osn`, perfil de motorista, logs) persistem, como depois de um início direto do OMSI | por design | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A remoção de um `closecheck` obsoleto antes de uma sessão é permanente (registrada, não restaurada) quando `SuppressStaleClosecheckWarning` é true | por design | como acima |

<a id="runtime-control"></a>
## Controle de runtime

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| `weather.set` é rejeitado (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`): o OMSI sobrescreve os dois candidatos de vento perfilados no próximo tick de clima | `UNAVAILABLE` | [capacidades](capabilities.md) |
| Gravações no calendário (`SetActualDateTime`) | `UNAVAILABLE` (BI-002) | `calendar.set-actual-date-time` |
| Gravações de variáveis string, triggers nomeados, triggers de som (propriedade de strings gerenciadas do Delphi) | `UNAVAILABLE` (BI-004) | `scripts.string.read` é somente leitura |
| Sem realocação de veículos, sem religação espacial entre tiles, sem autoridade de transformação segura para o ODE; os campos de posição são somente leitura | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` precisam de um PlayerVehicle; o início NEW_MAP headless não tem nenhum (uma situação salva fornece um) | por design (BI-007) | RV-004 |
| As mutações de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, texturas D3D) não são registradas no journal nem restauradas | por design | [controle de runtime](runtime-control.md) |
| Ponto cego da impressão digital dos handles: um objeto da mesma classe e definição recriado no mesmo endereço entre duas leituras de lista não é detectado como obsoleto; o ciclo de vida da remoção natural (RV-002) não tem um produtor seguro em runtime e continua coberto somente offline | `PARTIAL` | [controle de runtime](runtime-control.md) |
| Os resultados são limitados pelo mailbox de 64 KiB: listas longas são truncadas (`truncated=true`); os payloads de pixels são limitados a 48 KiB por `d3d.texture.update` | por design | [capacidades](capabilities.md) |
| Canal de requisição única: uma requisição por vez por sessão; um slot ocupado resulta em `OL_E_RUNTIME_CHANNEL_BUSY`; ids de requisição não podem ser reutilizados | por design | [controle de runtime](runtime-control.md) |
| A telemetria é um slot de último valor: rajadas mais rápidas que a amostragem de 100 ms do host podem perder eventos intermediários (números de sequência mantêm distintos eventos consecutivos idênticos; amostras inconsistentes são descartadas) | `PARTIAL` | [plugin permanente](../concepts/permanent-plugin.md) |
| O reset do dispositivo D3D foi observado em runtime (`resetting`, `restored`, invalidação de geração); uma transição `lost` distinta não foi produzida porque o dispositivo do OMSI foi diretamente para `DEVICENOTRESET` | `PARTIAL` (RV-007) | [status de validação em runtime](../status/runtime-validation-status.md) |
| Os resultados de listas limitadas (as operações `timetable.*.list`, `vehicle.variables.list`, `vehicle.string-variables.list`) retornam no máximo as linhas que cabem no slot de runtime de 64 KiB; o restante é omitido com `truncated=true` e um `returned_count` menor (auditoria de documentação BUG-05). Não há paginação nesta release | por design | [capacidades](capabilities.md) |
| `timetable.logs.read`, `road-vehicles.list`, `humans.list`, `vehicle.constants.list` e `vehicle.curves.list` não são limitadas: um resultado maior que o slot falha com `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (não observado para nenhuma delas nos mapas testados) | `PARTIAL` | [capacidades](capabilities.md) |
| As strings de evidência autodeclaradas (`PublicCapabilityRegistry` `RuntimeValidation`, `GetCapabilitiesAsync` `EvidenceState`) não foram atualizadas depois da rodada de fechamento em runtime: `camera.lock` ainda indica `STATICALLY_VALIDATED` e `runtime.d3d.lifecycle.reset` indica `IMPLEMENTED_NOT_RUNTIME_VALIDATED`. A página [status de validação em runtime](../status/runtime-validation-status.md) é a referência oficial | defasagem da documentação, não uma diferença de comportamento | [capacidades](capabilities.md) |
| Alguns campos avançados do grafo de mapas/tiles/caminhos/objetos não são expostos; os leitores de runtime são snapshots tipados controlados pelo perfil, nunca acesso arbitrário à memória | por design | [capacidades](capabilities.md) |
| As leituras de memória dentro do processo seguem o padrão verificar-e-depois-usar contra um OMSI ativo; uma mutação concorrente do OMSI entre a verificação e a leitura pode gerar um snapshot inconsistente (`OL_E_RUNTIME_OPERATION_FAILED`) | risco aceito (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Plano de controle local e modelo de confiança

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Modelo de confiança do mesmo usuário: o named pipe (`CurrentUserOnly`), os mapeamentos de memória de handoff/telemetria/runtime e o semáforo do lease são acessíveis a qualquer processo do mesmo usuário do Windows. Esse processo pode ler o status, parar a sessão ou executar operações de runtime depois de ler o `session_id`. | risco aceito (S-06, S-30) | [controle local](local-control.md) |
| O endpoint de controle só existe enquanto o proprietário está em `Running`; um cliente recebe `OL_E_NO_ACTIVE_SESSION` (saída 4) durante a inicialização e depois do fim da sessão | por design | [controle local](local-control.md) |
| Se outro processo já possuir o nome do pipe, o proprietário continua em execução sem endpoint (`ListenFault`), e uma segunda inicialização pode informar incorretamente `OL_E_SESSION_ALREADY_ACTIVE` | risco aceito | [controle local](local-control.md) |
| `.omsilaunch\` herda a ACL da raiz do OMSI; nenhum controle de acesso explícito é aplicado | risco aceito (S-31) | [transações e recuperação](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnósticos e saída

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Os diagnósticos são apenas arquivos locais (`.omsilaunch\diagnostics`); nada é enviado e não há relatórios remotos | por design | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A retenção mantém as 50 sessões mais recentes; diagnósticos mais antigos prefixados por sessão são excluídos quando uma nova sessão inicia | por design | como acima |
| A saída JSON e os diagnósticos incluem caminhos da instalação (`RootPath`, diretórios de recursos, caminhos `.itx`) | por design (dados locais) | |
| A caixa de diálogo de falha de sessão do `OmsiLaunchW.exe` mostra o payload de falha do plugin (por exemplo `{"name":"world.failed",...}`) como mensagem, em vez de uma frase; a linha `Code:` está correta | cosmético | [bandeja do Windows](windows-tray.md) |
| Uma requisição D3D rejeitada pela ponte nativa antes de qualquer chamada Direct3D informa `native_status` corretamente, mas seu texto `detail` diz `HRESULT 0x00000000` | cosmético | [capacidades](capabilities.md) |
| A janela de status da bandeja é um snapshot da sessão planejada, tirado quando ela é aberta; ela não é atualizada e não mostra valores do OMSI em tempo real | por design | [bandeja do Windows](windows-tray.md) |

<a id="documentation"></a>
## Documentação

As páginas em inglês em `docs/` são a documentação normativa desta release. `docs/localized/<locale>/` contém traduções das mesmas páginas `0.1.0-beta3` (veja [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); onde uma tradução diferir do texto em inglês, o texto em inglês e o código prevalecem. As páginas históricas e legadas listadas ali estão disponíveis somente em inglês.

Relacionadas: [capacidades](capabilities.md), [status de validação em runtime](../status/runtime-validation-status.md), [erros](errors.md).
