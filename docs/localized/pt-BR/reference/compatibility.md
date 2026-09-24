# Compatibilidade

<!-- l10n: source=reference/compatibility.md -->
> Tradução da [página original em inglês](../../../reference/compatibility.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

O OmsiLaunch controla o OMSI aplicando patches em endereços perfilados dentro de um único build exato do executável. Esta página indica quais builds do OMSI são suportados, o que acontece com qualquer outro build e os requisitos de sistema operacional e de runtime do host e do plugin. Fontes: `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`, `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs` e os arquivos de projeto.

<a id="supported-omsi-builds"></a>
## Builds do OMSI suportados

Existe exatamente um perfil de build, `Omsi23004_692EBFBF` (família `OMSI_2_3_004_COMMON`). Ele aceita dois executáveis por SHA-256 exato:

| Variante | SHA-256 de `Omsi.exe` | Tamanho | Versão do arquivo PE / produto | Status |
| --- | --- | --- | --- | --- |
| Executável perfilado (`ALTERNATE_LAA`) | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 bytes | 2.2.032 / 2.3.004 | `STABLE_BETA`; todas as validações em runtime da matriz foram executadas neste arquivo |
| Steam LAA (`STEAM_LAA`) | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | não verificado | | Aceito por lista de permissões porque compartilha o layout nativo perfilado e difere apenas nos cabeçalhos do executável; **não validado em runtime** (`profiles` informa `runtime_validated=false`, `validation_status=pending_beta_field_validation`). `PARTIAL`. |

`OmsiLaunch.exe profiles` imprime esta tabela como JSON. Os números de versão não são usados para a aceitação: só contam o SHA-256 (e, para o executável principal, o tamanho exato). Nenhum outro build do OMSI 2, nenhum executável com patch e nenhuma cópia com patch de 4 GB com um hash diferente é suportado.

<a id="what-happens-with-an-unknown-build"></a>
## O que acontece com um build desconhecido

| Etapa | Verificação | Resultado |
| --- | --- | --- |
| Planejamento (`PlanSessionAsync`, `/plan`, `/validate`) | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | A capacidade exigida `omsi.profile.OMSI23004` fica `UNAVAILABLE`; diagnóstico `OL_E_UNSUPPORTED_BUILD`; `SessionPlan.IsRunnable=false`. Saída 1 da CLI para uma inicialização, ou 3 (`UnsupportedProfile`) quando o erro escapa como exceção. |
| Início (`StartSessionAsync`) | A spec é replanejada e o hash de `Omsi.exe` é recalculado | Um plano que deixou de estar apto para execução (por exemplo, o executável mudou depois do planejamento, ou um chamador editou `IsRunnable`) é rejeitado com `OL_E_PLAN_NOT_RUNNABLE`; nenhuma transação é aberta, nenhum processo é iniciado. |
| No processo (`PluginRuntime.Start`) | `NativeServices.ValidateBuild` exige que o `BuildProfileId` do handoff seja `Omsi23004_692EBFBF` **e** que `NativeValidateBuild()` tenha sucesso contra a imagem em execução | Telemetria `plugin.build.invalid`; o host faz a sessão falhar com `OL_E_BUILD_VALIDATION_FAILED`; nenhum hook nativo é armado; o OMSI é encerrado e a transação é restaurada. |

Como o hash do executável é comparado com os tamanhos e os bytes das variáveis globais perfiladas, a verificação dentro do processo é a última linha de defesa contra uma cópia que passou pela verificação de hash, mas cuja imagem difere no momento do carregamento. Não existe perfil de fallback nem correspondência heurística.

<a id="operating-system-and-architecture"></a>
## Sistema operacional e arquitetura

`CurrentWindowsX64Platform.Detect` calcula `RuntimePlatformInfo`. A plataforma atual só é suportada quando todas as condições a seguir são atendidas:

| Requisito | Verificação | Erro quando violado |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 ou posterior | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows de 64 bits e um processo host de 64 bits | `OSArchitecture == X64` e `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Instalação gravável | O diretório raiz existe, não é somente leitura e contém `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` também informa `OmsiArchitecture` e `PluginArchitecture` como `X86` (o OMSI é um processo de 32 bits; o conjunto de arquivos do plugin é x86 e é executado sob WOW64), `LegacyPlatform=false` e `Wow64Available`. O Windows ARM64 não é suportado, mesmo onde existe emulação x64, porque o próprio processo host precisa ser x64.

<a id="net-requirements"></a>
## Requisitos do .NET

| Componente | Runtime | Observações |
| --- | --- | --- |
| Controlador (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | O bootstrapper nativo localiza o runtime por meio do `hostfxr`, usando o `nethost.dll` incluído no pacote. Um runtime ausente é informado pelo shim (códigos de saída 100-106; veja [CLI](cli.md) e [códigos de saída](exit-codes.md)). |
| Conjunto de arquivos do plugin (`plugins\OmsiLaunch.Plugin.dll` via `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), hospedado pelo DNNE 2.0.6 dentro do `Omsi.exe` | Exige que o runtime .NET 6 Desktop/Core x86 esteja instalado na máquina; o runtime de 64 bits sozinho não é suficiente para o plugin. |
| Ponte nativa (`plugins\OmsiLaunch.Native.x86.dll`) | x86 nativo | Carregada somente a partir de `plugins\` (veja [plugin permanente](../concepts/permanent-plugin.md)). |

<a id="legacy-platforms"></a>
## Plataformas legadas

Windows 7, Windows 8.x, Windows XP e outros sistemas NT 6 e anteriores estão fora do limite de suporte atual. `RuntimePlatformInfo.LegacyPlatform` é sempre `false` e não existe nenhum adaptador legado; o campo e a interface `IPluginNativeServices` existem apenas para que um futuro adaptador legado possa ser adicionado sem alterar a API pública (veja `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nada nesta release é executado nesses sistemas.

<a id="steam-and-large-address-aware-notes"></a>
## Observações sobre Steam e Large Address Aware

- A distribuição Steam do OMSI 2.3.004 com o cabeçalho LAA (`7DAB063D...`) está na lista de permissões porque seus endereços perfilados são idênticos aos do executável principal. Até que uma sessão de validação em campo seja registrada na matriz, trate todas as capacidades nesse arquivo como `PARTIAL`.
- A Steam inicia o próprio OMSI; uma sessão precisa ser iniciada pelo `OmsiLaunch.exe` para que o handoff exista. Quando o OMSI é iniciado pela Steam, o plugin permanente permanece inerte (sem handoff, sem hooks).
- Aplicar outro patcher LAA ao `Omsi.exe` altera seu hash e o torna um build desconhecido.

<a id="related-pages"></a>
## Páginas relacionadas

- [Limitações conhecidas](known-limitations.md)
- [Status de validação em runtime](../status/runtime-validation-status.md)
- [Instalação](../getting-started/installation.md)
