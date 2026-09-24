# Glossário pt-PT — documentação OmsiLaunch 0.1.0-beta3

Glossário de terminologia obrigatório para todos os tradutores de `pt-PT` (português europeu). A página inglesa é a fonte de verdade; este glossário só fixa a terminologia.

## Registo e forma de tratamento

- Registo técnico, neutro e formal. Preferir construções impessoais ("é possível", "deve-se", "o comando devolve") ou "o utilizador" / "o integrador". Nunca usar "você"; evitar "tu". Instruções em listas podem usar o infinitivo ("Executar `session stop`.") ou o imperativo formal de 3.ª pessoa ("Execute ..."), mas de forma consistente dentro da página — preferir o infinitivo.
- Português europeu genuíno (norma do Acordo Ortográfico de 1990, como é usual em documentação técnica portuguesa atual): ficheiro, utilizador, ecrã, registo, guardar, predefinição/predefinido, transferir, ligação, aceder, gerir, equipa, contacto, facto, receção, ação, ótimo. Progressivo com "estar a" + infinitivo ("a sessão está a executar", "o OMSI está a carregar"), nunca gerúndio brasileiro ("está executando").
- Proibido (formas brasileiras): você, arquivo, usuário, tela, registro, gerenciar/gerenciamento, salvar, padrão (no sentido de "default"), baixar, deletar, time (equipa), contato, fato, acessar, ônibus, trem, "está executando".
- Colocação pronominal europeia: ênclise em frases afirmativas ("o plano torna-se não executável", "devolve-se"), próclise após negação e certos advérbios/conjunções ("não se aplica", "que se refere").

## Pontuação e tipografia

- Aspas: usar aspas latinas « » para citações em prosa; aspas retas "..." são aceitáveis se o texto inglês usar aspas para termos — manter a mesma escolha em toda a página. Nunca colocar aspas à volta de spans de código.
- Separador decimal: vírgula em números em prosa ("2,5 segundos"), MAS números, unidades e valores literais que o brief manda manter (`64 KiB`, `250 ms`, `2 s`, versões, hashes, ids) ficam exatamente como em inglês.
- Espaço entre número e unidade (já presente no inglês). Não inserir espaço antes de `:` `;` `?` `!`.
- Maiúsculas: títulos e cabeçalhos em maiúscula só na primeira palavra e em nomes próprios ("Ciclo de vida da sessão", não "Ciclo De Vida Da Sessão"). Nomes de produto (OmsiLaunch, OMSI, Windows, Steam, .NET) mantêm-se.
- Termos ingleses mantidos (runtime, plugin, handle, overlay, snapshot, timeout, pipe, hash, thread, build, CLI, API, IPC, JSON) escrevem-se sem itálico e sem aspas; género gramatical indicado na tabela. Plural: "os handles", "os overlays", "os snapshots", "os plugins".
- Travessão (—) para incisos, como no inglês; hífen para intervalos só quando está no original.

## Terminologia

| English | pt-PT | note |
| --- | --- | --- |
| session | sessão | f. |
| session owner / owner | proprietário da sessão / proprietário | m. Processo (CLI) que é dono da sessão. Não usar "dono" em texto formal. |
| client (mode) | (modo) cliente | "modo cliente"; "um cliente" = processo que fala com o proprietário. |
| owner process | processo proprietário | |
| launch | lançamento (s.); lançar / iniciar (v.) | "launch spec" em prosa = especificação de lançamento; `LaunchSpec` fica em código. |
| plan (noun/verb) | plano (s.); planear (v.) | Europeu: "planear", nunca "planejar". "Planeamento" = planning. |
| runnable / not runnable | executável / não executável | Referido a um plano: "o plano é executável" / "torna o plano não executável". |
| start | iniciar (v.); início / arranque (s.) | "startup" = arranque; "startup timeout" = timeout de arranque. |
| stop | parar (v.); paragem (s.) | Europeu: "paragem", nunca "parada". Não confundir com "terminação forçada" (forced termination). |
| end session | terminar a sessão | O literal de UI `End session` fica em inglês (pt-PT não é localizado pelo produto). |
| canonical stop | paragem canónica | "caminho de paragem canónico do proprietário" para "canonical owner stop path". Grafia europeia "canónica". |
| restore | restauro (s.); restaurar (v.) | Europeu: "restauro", não "restauração". |
| recovery | recuperação | |
| pending (journal) | (journal) pendente | |
| journal | journal | m. Manter em inglês (ficheiro `journal.json`); na primeira ocorrência pode explicar-se "journal (registo de transação)". Não usar "diário". |
| transaction | transação | f. |
| overlay | overlay | m. Manter em inglês ("overlay temporário"). |
| backup | cópia de segurança | "backup" (m.) aceitável em tabelas estreitas; preferir "cópia de segurança" em prosa. Verbo: "fazer cópia de segurança de". |
| snapshot | snapshot | m. Manter em inglês. "Status window (read-only snapshot)" = janela de estado (snapshot só de leitura). |
| installation | instalação | |
| installation root | raiz da instalação | "a raiz do OMSI" para "OMSI root". |
| package | pacote | |
| release package | pacote de lançamento | "release" isolado = versão / lançamento; "release notes" = notas de versão. |
| manifest | manifesto | |
| permanent plugin | plugin permanente | m. |
| native bridge | ponte nativa | |
| runtime | runtime | m. Manter em inglês ("o runtime", "controlo de runtime"). ".NET runtime" = runtime .NET. |
| runtime operation | operação de runtime | |
| runtime command | comando de runtime | |
| runtime slot / mailbox | slot de runtime / mailbox de runtime | m./f. Manter em inglês; pode explicar-se "(caixa de correio)" na primeira ocorrência. "telemetry slot" = slot de telemetria. |
| control plane | plano de controlo | Europeu: "controlo", nunca "controle". |
| local control | controlo local | |
| named pipe | named pipe | m. Manter em inglês; "pipe" (m.) isolado. |
| frame (protocol) | frame | m. Manter em inglês ("frame com prefixo de comprimento"). |
| envelope (JSON) | envelope (JSON) | m. |
| handle | handle | m. Manter em inglês. |
| stale handle | handle obsoleto | |
| capability | capacidade | f. Ids (`camera.lock`) ficam em código. |
| capability registry | registo de capacidades | Europeu: "registo", nunca "registro". |
| bounded list | lista limitada | "lista com limite" aceitável; manter consistente "lista limitada". |
| truncated | truncado/a | "`truncated=true` indica que foram omitidas linhas". |
| row | linha | De uma tabela ou lista de resultados. |
| evidence | evidência | f. "evidência de runtime"; ids (`T01`, `CAM01`) inalterados. |
| runtime-validated | validado em runtime | Não suavizar. `RUNTIME_VALIDATED` literal inalterado. |
| statically validated | validado estaticamente | `STATICALLY_VALIDATED` literal inalterado. |
| offline test | teste offline | "coberto apenas offline" para "covered offline only". |
| gate (documentation gate) | gate (gate de documentação) | m. Manter "gate"; pode explicar-se "(verificação obrigatória)". |
| tray icon | ícone da área de notificação | "tray" em títulos: "Indicador na área de notificação do Windows". Não usar "bandeja". |
| notification area | área de notificação | Termo oficial do Windows em pt-PT. |
| status window | janela de estado | Europeu: "estado", não "status". |
| confirmation dialog | caixa de diálogo de confirmação | |
| message box | caixa de mensagem | |
| failure dialog | caixa de diálogo de falha | |
| tooltip | descrição (tooltip) | Primeira ocorrência "descrição (tooltip)"; depois "tooltip" (f.) aceitável. |
| context menu | menu de contexto | |
| Explorer restart | reinício do Explorador | "Explorador de Ficheiros" (Windows pt-PT); `explorer.exe` em código. |
| entry point | ponto de entrada | `/entrypoint` em código. |
| new map | novo mapa | `NEW_MAP` literal inalterado. |
| saved situation | situação guardada | Europeu: "guardada", nunca "salva". `SAVED_SITUATION` inalterado. |
| map | mapa | |
| splash screen | ecrã de abertura (splash) | Primeira ocorrência "ecrã de abertura (splash screen)"; depois "splash" (m.) aceitável. |
| Internet Textures | Internet Textures | Nome de funcionalidade do OMSI; manter em inglês ("texturas da Internet" só como explicação). |
| session profile | perfil de sessão | |
| preset | predefinição (preset) | "preset de meteorologia" aceitável; preferir "predefinição" em prosa geral. |
| setting | definição | Europeu: "definição"/"definições", não "configuração" para "setting"; "configuration" = configuração. |
| player vehicle | veículo do jogador | |
| road vehicle | veículo rodoviário | `RoadVehicle` em código inalterado. |
| human (pedestrian/passenger object) | humano (objeto peão/passageiro) | `Human` em código. Europeu: "peão", não "pedestre". |
| timetable | horário | |
| track entry | entrada de trajeto | Termo do horário do OMSI. |
| tour entry | entrada de serviço (tour) | "tour" = serviço/turno de horário do OMSI; manter "(tour)" na primeira ocorrência. |
| ticket | bilhete | Europeu: "bilhete", não "passagem". |
| driver | motorista | Perfil de condutor do OMSI (`Drivers\`): "perfil de motorista". Nunca "driver" salvo drivers de dispositivo (controlador). |
| fleet number | número de frota | |
| registration (plate) | matrícula | Europeu: "matrícula", nunca "placa". |
| repaint | repaint (pintura) | m. Termo da comunidade OMSI; "repaint" com "(pintura alternativa)" na primeira ocorrência. |
| spawn | criar / fazer aparecer (spawn) | Verbo: "criar"; "spawn" entre parênteses se necessário. |
| camera lock | bloqueio da câmara | Europeu: "câmara", não "câmera". `camera.lock` em código. |
| device reset | reposição do dispositivo (reset) | D3D device reset; "reset do dispositivo D3D" aceitável. |
| render thread | thread de renderização | f. ("a thread"). |
| texture | textura | |
| exit code | código de saída | |
| error code | código de erro | |
| diagnostic | diagnóstico | "plan diagnostic" = diagnóstico do plano. |
| diagnostics directory | diretório de diagnóstico | "pasta" aceitável em texto para utilizadores; preferir "diretório". |
| timeout | timeout | m. Manter em inglês ("tempo limite" como explicação). |
| placeholder | marcador de posição | "placeholder" entre parênteses se útil. |
| flag | flag | f. ("a flag `/json`"). Manter em inglês. |
| route | rota | "rota hierárquica". |
| command word | palavra de comando | |
| integrator | integrador | |
| caller | chamador | "o código chamador". |
| known limitation | limitação conhecida | |
| accepted risk | risco aceite | Europeu: "aceite", não "aceito". |
| stable beta | beta estável | `STABLE_BETA` literal inalterado. |
| experimental | experimental | `EXPERIMENTAL` inalterado. |
| partial | parcial | `PARTIAL` inalterado. |
| unavailable | indisponível | `UNAVAILABLE` inalterado; não suavizar para "limitado". |
| deprecated/legacy | obsoleto / legado | "deprecated" = obsoleto (desaconselhado); "legacy" = legado. |
| not normative | não normativo | |
| source of truth | fonte de verdade | "a página inglesa é a única fonte de verdade". |

## Outros termos recorrentes

| English | pt-PT | note |
| --- | --- | --- |
| file | ficheiro | Nunca "arquivo". |
| user | utilizador | Nunca "usuário". |
| screen | ecrã | Nunca "tela". |
| save | guardar | Nunca "salvar". |
| default | predefinição / predefinido | "por predefinição". |
| download | transferir / transferência | |
| log | registo (log) | "ficheiro de registo"; nomes `*.log` em código. |
| supervisor | supervisor | |
| forced termination | terminação forçada | |
| weather | meteorologia / tempo | "definição meteorológica". |
| catalogue (content) | catálogo de conteúdos | |
| shortcut | atalho | |

## Strings da UI do Windows

O produto não localiza a UI para `pt-PT`: o Windows em português europeu mostra as strings inglesas. Os literais de UI (`End session`, `Session is running`, `Status`, `Close`, `OmsiLaunch is running`, ...) ficam em inglês dentro de backticks; explica-se o seu significado em português na prosa. Nunca apresentar uma tradução pt-PT (nem as strings pt-BR do produto) como se fosse texto mostrado pelo produto.
