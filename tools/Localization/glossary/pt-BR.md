# Glossário pt-BR — documentação OmsiLaunch 0.1.0-beta3

Obrigatório para todas as páginas traduzidas em `docs/localized/pt-BR/`. A página em inglês é a única fonte da verdade; este glossário só fixa a terminologia.

## Tratamento, registro e tipografia

- **Tratamento e registro:** português brasileiro técnico, profissional e direto. Use "você" quando for preciso se dirigir ao leitor e prefira construções impessoais ("é necessário", "execute", "use") nas instruções; nunca "tu", nunca formas europeias (ficheiro, utilizador, ecrã, registo, guardar, "está a executar").
- **Vocabulário regional:** arquivo, usuário, tela, registro, salvar, padrão, configurar, gerenciar, diretório/pasta, excluir/remover.
- **Artigo com nomes de produto:** "o OMSI", "o OmsiLaunch", "o `Omsi.exe`", "o runtime", "o journal", "o handle", "o overlay", "o snapshot", "o backup", "o plugin", "o pipe", "o timeout", "o slot", "o mailbox", "o frame", "o envelope" (todos masculinos).
- **Maiúsculas:** títulos e cabeçalhos em caixa de frase (só a primeira palavra e nomes próprios em maiúscula): "Ciclo de vida da sessão", não "Ciclo de Vida da Sessão".
- **Aspas:** aspas retas duplas `"..."` na prosa (como no original); nunca transformar crases de código em aspas.
- **Números e unidades:** permanecem exatamente como no inglês (`64 KiB`, `250 ms`, `2 s`, `0.1.0-beta3`); não trocar ponto decimal por vírgula nem inserir separadores de milhar em valores literais. Em números escritos por extenso na prosa, siga a norma brasileira.
- **Listas e tabelas:** manter a pontuação final do original (se o item em inglês termina com ponto, o traduzido também termina).
- **Estrangeirismos** mantidos em inglês (runtime, handle, overlay, snapshot, backup, journal, pipe, frame, timeout, thread, build, hash, plugin, flag, slot, mailbox, gate, spawn) vão sem itálico e sem aspas, em minúsculas no meio da frase.
- **Tokens de status** (`STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `UNAVAILABLE`, `RUNTIME_VALIDATED`, `STATICALLY_VALIDATED` etc.), ids de evidência (`T01`, `CAM01`, `RV-002`, `BUG-05`, `DOC-01` …) e versões ficam inalterados. Na prosa, o conceito é traduzido (ver tabela); o token literal nunca.
- **Literais de UI em inglês** (`End session`, `Session is running` …) ficam em crase, sem tradução. Se útil, acrescente logo depois o texto pt-BR do produto entre parênteses e crases, copiado exatamente da tabela "Product UI strings (exact)" abaixo, por exemplo: `End session` (`Encerrar sessão`). Nunca invente texto de UI.

## Termos

| English | pt-BR | note |
| --- | --- | --- |
| session | sessão | |
| session owner / owner | proprietário da sessão / proprietário | O processo ou host que possui a sessão (OMSI, restauração, parada). "owner mode" = modo proprietário. Não usar "dono". |
| client (mode) | modo cliente; cliente | O processo CLI que encaminha comandos ao proprietário. |
| owner process | processo proprietário | |
| launch (noun / verb) | inicialização / iniciar | Evitar "lançamento/lançar". "launch spec" = especificação de inicialização (o tipo `LaunchSpec` fica em código). |
| plan (noun / verb) | plano / planejar | "planning" = planejamento; "re-plan" = replanejamento. |
| runnable / not runnable | apto para execução / não apto para execução | Não usar "executável" como adjetivo (colide com "executável" = arquivo .exe). "a non-runnable launch" = "uma inicialização não apta para execução". |
| start (noun / verb) | início, inicialização / iniciar | "startup" = inicialização; "startup timeout" = timeout de inicialização. |
| stop (noun / verb) | parada / parar | "stopping" = parada em andamento. |
| end session | encerrar a sessão | Coincide com o texto de UI pt-BR `Encerrar sessão`. |
| canonical stop | parada canônica | "canonical owner stop path" = caminho canônico de parada do proprietário. |
| restore (noun / verb) | restauração / restaurar | |
| recovery | recuperação | "recover" = recuperar. |
| pending (journal) | journal pendente | "a pending journal" = um journal pendente; "pending recovery" = recuperação pendente. |
| journal | journal | Mantido em inglês (termo usual de journaling). "journaled" = registrado no journal; "journal state" = estado do journal. Não usar "diário". |
| transaction | transação | "transaction participant" = participante da transação. |
| overlay | overlay | Mantido em inglês (o overlay; plural overlays). "temporary overlays" = overlays temporários. |
| backup | backup | "backed up" = copiado para backup / com backup feito. |
| snapshot | snapshot | Mantido em inglês (o snapshot). "Snapshotting" (estado) fica em código. |
| installation | instalação | "installation lease" = lease da instalação (mantém "lease" em inglês, explicar como bloqueio exclusivo se necessário). |
| installation root | raiz da instalação | |
| package | pacote | "session-profile package" = pacote de perfil de sessão. |
| release package | pacote de release | Não "pacote de lançamento". |
| manifest | manifesto | |
| permanent plugin | plugin permanente | "plugin closure" = conjunto de arquivos do plugin (closure do plugin). |
| native bridge | ponte nativa | |
| runtime | runtime | Mantido em inglês (o runtime). "at runtime" = em runtime / em tempo de execução. |
| runtime operation | operação de runtime | "operation id" = id da operação. |
| runtime command | comando de runtime | |
| runtime slot / mailbox | slot de runtime / mailbox | Mantidos em inglês; se precisar explicar: área de memória compartilhada de 64 KiB. |
| control plane | plano de controle | "local control plane" = plano de controle local. |
| local control | controle local | |
| named pipe | named pipe | Mantido em inglês (o named pipe); pode-se explicar como "pipe nomeado" na primeira ocorrência. |
| frame (protocol) | frame | Mantido em inglês (o frame). "control frame" = frame de controle; "request frame" = frame de requisição. |
| envelope (JSON) | envelope | "CLI envelope" = envelope da CLI; "response envelope" = envelope de resposta. |
| handle | handle | Mantido em inglês (o handle). |
| stale handle | handle obsoleto | |
| capability | capacidade | "capability id" = id da capacidade. |
| capability registry | registro de capacidades | |
| bounded list | lista limitada | Resultado com limite de linhas; ver BUG-05 no brief. |
| truncated | truncado(a) | "truncated=true" fica em código; na prosa: "a lista foi truncada" (linhas omitidas), nunca "falhou". |
| row | linha | "returned rows" = linhas retornadas. |
| evidence | evidência | "runtime evidence" = evidência de runtime. |
| runtime-validated | validado em runtime | "not runtime validated" = não validado em runtime (mesma força). |
| statically validated | validado estaticamente | |
| offline test | teste offline | "offline only" = somente offline. |
| gate (documentation gate) | gate de documentação | Mantém "gate" (verificação automatizada). |
| tray icon | ícone da bandeja | "tray" = bandeja do sistema; "tray indicator" = indicador na bandeja. |
| notification area | área de notificação | Termo oficial do Windows em pt-BR. |
| status window | janela de status | |
| confirmation dialog | caixa de diálogo de confirmação | |
| message box | caixa de mensagem | |
| failure dialog | caixa de diálogo de falha | |
| tooltip | dica de ferramenta | Termo do Windows pt-BR; "texto da dica" quando mais natural. |
| context menu | menu de contexto | |
| Explorer restart | reinício do Explorer | O processo `explorer.exe` (Explorador de Arquivos do Windows); manter "Explorer". |
| entry point | ponto de entrada | Igual à UI (`Ponto de entrada`). |
| new map | novo mapa | Modo `NEW_MAP` (a UI mostra `Nova sessão`). |
| saved situation | situação salva | Igual à UI (`Situação salva`). |
| map | mapa | |
| splash screen | tela de abertura (splash) | "splash" pode ficar sozinho depois da primeira ocorrência; "managed splash" = splash gerenciado. |
| Internet Textures | texturas da internet | Igual à UI (`Texturas da internet`). |
| session profile | perfil de sessão | |
| preset | predefinição | Igual à UI (`Predefinição`). |
| setting | configuração | "settings" = configurações; "setting key" = chave de configuração. |
| player vehicle | veículo do jogador | |
| road vehicle | veículo rodoviário | Qualquer veículo na via (inclui tráfego de IA); `RoadVehicle` fica em código. |
| human (pedestrian/passenger object) | humano (pedestre/passageiro) | Objeto humano do OMSI; `Human` fica em código. |
| timetable | tabela de horários | "timetable logs" = logs da tabela de horários. |
| track entry | entrada de trajeto | Plural: entradas de trajeto. |
| tour entry | entrada de tour | "tour" mantido (termo da comunidade OMSI para serviço/circulação). |
| ticket | bilhete | |
| driver | motorista | "driver profile" = perfil de motorista. |
| fleet number | número de frota | Igual à UI (`Número de frota`). |
| registration (plate) | matrícula (placa) | Usar "matrícula" (igual à UI `Matrícula`); "placa" só como esclarecimento. |
| repaint | pintura (repaint) | Igual à UI (`Pintura`); "repaint" entre parênteses na primeira ocorrência. |
| spawn | spawn / gerar | "spawn a vehicle" = gerar (fazer spawn de) um veículo. |
| camera lock | travamento de câmera | O comando `camera lock` fica em código. |
| device reset | reset do dispositivo | Dispositivo Direct3D. |
| render thread | thread de renderização | "thread" masculino (o thread). |
| texture | textura | |
| exit code | código de saída | |
| error code | código de erro | |
| diagnostic | diagnóstico | |
| diagnostics directory | diretório de diagnósticos | |
| timeout | timeout | Mantido em inglês (o timeout); "tempo limite" aceitável na prosa explicativa. |
| placeholder | placeholder | Mantido em inglês (ex.: `<root>`); pode explicar como "marcador". |
| flag | flag | Mantido em inglês (a flag, opção de linha de comando). |
| route | rota | "hierarchical route" = rota hierárquica. |
| command word | palavra de comando | |
| integrator | integrador | |
| caller | chamador | |
| known limitation | limitação conhecida | |
| accepted risk | risco aceito | |
| stable beta | beta estável | Token `STABLE_BETA` inalterado. |
| experimental | experimental | Token `EXPERIMENTAL` inalterado. |
| partial | parcial | Token `PARTIAL` inalterado; nunca "não suportado". |
| unavailable | indisponível | Token `UNAVAILABLE` inalterado. |
| deprecated / legacy | obsoleto / legado | |
| not normative | não normativo | |
| source of truth | fonte da verdade | |

## Product UI strings (exact)

Origem: `tools/OmsiLaunch.Cli/WindowsUiStrings.cs` (dicionários `English` e `PortugueseBrazil`). Nas páginas, o literal em inglês fica em crase; o texto pt-BR, se usado, vem entre parênteses e em crase, exatamente como abaixo.

| key | English | pt-BR (product) |
| --- | --- | --- |
| `Tray.Running` | `OmsiLaunch is running` | `OmsiLaunch em execução` |
| `Tray.Status` | `Status` | `Status` |
| `Tray.EndSession` | `End session` | `Encerrar sessão` |
| `Tray.EndSessionDescription` | `Ends OMSI 2 and the OmsiLaunch session` | `Encerra o OMSI 2 e a sessão do OmsiLaunch` |
| `Status.Title` | `OmsiLaunch session status` | `Status da sessão OmsiLaunch` |
| `Status.SessionRunning` | `Session is running` | `Sessão em execução` |
| `Status.Section.Session` | `Session` | `Sessão` |
| `Status.Section.Profile` | `Session profile` | `Perfil de sessão` |
| `Status.Section.Environment` | `Environment` | `Ambiente` |
| `Status.Section.Vehicle` | `Vehicle` | `Veículo` |
| `Status.Section.Configuration` | `Configuration` | `Configuração` |
| `Status.Section.Presentation` | `Presentation` | `Apresentação` |
| `Status.Field.Mode` | `Mode` | `Modo` |
| `Status.Field.Map` | `Map` | `Mapa` |
| `Status.Field.EntryPoint` | `Entry point` | `Ponto de entrada` |
| `Status.Field.Situation` | `Situation` | `Situação` |
| `Status.Field.SessionProfile` | `Profile` | `Perfil` |
| `Status.Field.Preset` | `Preset` | `Predefinição` |
| `Status.Field.Date` | `Date` | `Data` |
| `Status.Field.Time` | `Time` | `Hora` |
| `Status.Field.Weather` | `Weather` | `Clima` |
| `Status.Field.Vehicle` | `Vehicle` | `Veículo` |
| `Status.Field.Repaint` | `Repaint` | `Pintura` |
| `Status.Field.Hof` | `HOF` | `HOF` |
| `Status.Field.Fleet` | `Fleet number` | `Número de frota` |
| `Status.Field.Registration` | `Registration` | `Matrícula` |
| `Status.Field.Splash` | `Splash` | `Splash` |
| `Status.Field.InternetTextures` | `Internet textures` | `Texturas da internet` |
| `Status.Close` | `Close` | `Fechar` |
| `Mode.NewMap` | `New session` | `Nova sessão` |
| `Mode.SavedSituation` | `Saved situation` | `Situação salva` |
| `Mode.LastMapState` | `Last map state` | `Último estado do mapa` |
| `Presentation.Managed` | `Managed` | `Gerenciado` |
| `Presentation.Unset` | `Original OMSI` | `OMSI original` |
| `InternetTextures.Native` | `Original OMSI` | `OMSI original` |
| `InternetTextures.Disabled` | `Disabled` | `Desativadas` |
| `InternetTextures.Override` | `Override` | `Substituição` |
| `Stop.Title` | `End session?` | `Encerrar sessão?` |
| `Stop.Message` | `OMSI 2 will be closed and the OmsiLaunch managed session will end.` | `O OMSI 2 será encerrado e a sessão gerenciada pelo OmsiLaunch será finalizada.` |
| `Stop.Confirm` | `End session` | `Encerrar sessão` |
| `Stop.Cancel` | `Cancel` | `Cancelar` |
| `Stop.Failed` | `The session could not be ended. OMSI and its managed session remain active.` | `Não foi possível encerrar a sessão. O OMSI e sua sessão gerenciada continuam ativos.` |

Nota: a UI é localizada pelo próprio produto apenas para inglês, pt-BR, alemão, francês e polonês; `pt-PT` e demais idiomas do Windows exibem os textos em inglês.
