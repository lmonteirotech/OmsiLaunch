# Compatibilidad

<!-- l10n: source=reference/compatibility.md -->
> Traducción de la [página original en inglés](../../../reference/compatibility.md) de OmsiLaunch 0.1.0-beta3. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

OmsiLaunch controla OMSI parcheando direcciones perfiladas dentro de una build exacta del ejecutable. Esta página indica qué builds de OMSI se admiten, qué ocurre con cualquier otra build y cuáles son los requisitos de sistema operativo y de runtime del host y del plugin. Fuentes: `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`, `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs` y los archivos de proyecto.

<a id="supported-omsi-builds"></a>
## Builds de OMSI admitidas

Existe exactamente un perfil de build, `Omsi23004_692EBFBF` (familia `OMSI_2_3_004_COMMON`). Acepta dos ejecutables por su SHA-256 exacto:

| Variante | SHA-256 de `Omsi.exe` | Tamaño | Versión de archivo PE / producto | Estado |
| --- | --- | --- | --- | --- |
| Ejecutable perfilado (`ALTERNATE_LAA`) | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 bytes | 2.2.032 / 2.3.004 | `STABLE_BETA`; todas las validaciones en runtime de la matriz se ejecutaron con este archivo |
| Steam LAA (`STEAM_LAA`) | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | no se comprueba | | Aceptado mediante lista de permitidos porque comparte la estructura nativa perfilada y solo difiere en las cabeceras del ejecutable; **no validado en runtime** (`profiles` notifica `runtime_validated=false`, `validation_status=pending_beta_field_validation`). `PARTIAL`. |

`OmsiLaunch.exe profiles` imprime esta tabla como JSON. Los números de versión no se usan para la aceptación: solo cuentan el SHA-256 (y, para el ejecutable principal, el tamaño exacto). No se admite ninguna otra build de OMSI 2, ningún ejecutable parcheado ni ninguna copia con el parche de 4 GB que tenga un hash distinto.

<a id="what-happens-with-an-unknown-build"></a>
## Qué ocurre con una build desconocida

| Etapa | Comprobación | Resultado |
| --- | --- | --- |
| Planificación (`PlanSessionAsync`, `/plan`, `/validate`) | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | La capacidad requerida `omsi.profile.OMSI23004` es `UNAVAILABLE`; diagnóstico `OL_E_UNSUPPORTED_BUILD`; `SessionPlan.IsRunnable=false`. Salida de la CLI 1 para un lanzamiento, o 3 (`UnsupportedProfile`) cuando el error escapa como excepción. |
| Inicio (`StartSessionAsync`) | La especificación se vuelve a planificar y se vuelve a calcular el hash de `Omsi.exe` | Un plan que ya no es ejecutable (por ejemplo, porque el ejecutable cambió después de la planificación o porque un llamador modificó `IsRunnable`) se rechaza con `OL_E_PLAN_NOT_RUNNABLE`; no se abre ninguna transacción ni se inicia ningún proceso. |
| Dentro del proceso (`PluginRuntime.Start`) | `NativeServices.ValidateBuild` exige que el `BuildProfileId` del handoff sea `Omsi23004_692EBFBF` **y** que `NativeValidateBuild()` tenga éxito contra la imagen en ejecución | Telemetría `plugin.build.invalid`; el host hace fallar la sesión con `OL_E_BUILD_VALIDATION_FAILED`; no se arma ningún hook nativo; se termina OMSI y se restaura la transacción. |

Como el hash del ejecutable se compara con los tamaños y bytes de las variables globales perfiladas, la comprobación dentro del proceso es la última línea de defensa frente a una copia que superó la comprobación del hash pero cuya imagen difiere en el momento de la carga. No hay perfil de reserva ni coincidencia heurística.

<a id="operating-system-and-architecture"></a>
## Sistema operativo y arquitectura

`CurrentWindowsX64Platform.Detect` calcula `RuntimePlatformInfo`. La plataforma actual solo se admite cuando se cumplen todas las condiciones siguientes:

| Requisito | Comprobación | Error si no se cumple |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 o posterior | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows de 64 bits y un proceso host de 64 bits | `OSArchitecture == X64` y `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Instalación con permiso de escritura | El directorio raíz existe, no es de solo lectura y contiene `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` también notifica `OmsiArchitecture` y `PluginArchitecture` como `X86` (OMSI es un proceso de 32 bits; el conjunto de archivos del plugin es x86 y se ejecuta bajo WOW64), `LegacyPlatform=false` y `Wow64Available`. Windows ARM64 no se admite, aunque exista emulación x64, porque el propio proceso host debe ser x64.

<a id="net-requirements"></a>
## Requisitos de .NET

| Componente | Runtime | Notas |
| --- | --- | --- |
| Controlador (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | El bootstrapper nativo localiza el runtime mediante `hostfxr` a través del `nethost.dll` incluido en el paquete. El shim notifica la falta del runtime (códigos de salida 100-106; consulta [CLI](cli.md) y [códigos de salida](exit-codes.md)). |
| Conjunto de archivos del plugin (`plugins\OmsiLaunch.Plugin.dll` mediante `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), alojado por DNNE 2.0.6 dentro de `Omsi.exe` | Requiere que el runtime Desktop/Core x86 de .NET 6 esté instalado en el ordenador; el runtime de 64 bits por sí solo no basta para el plugin. |
| Puente nativo (`plugins\OmsiLaunch.Native.x86.dll`) | x86 nativo | Solo se carga desde `plugins\` (consulta [plugin permanente](../concepts/permanent-plugin.md)). |

<a id="legacy-platforms"></a>
## Plataformas heredadas

Windows 7, Windows 8.x, Windows XP y otros sistemas NT 6 y anteriores quedan fuera del límite de compatibilidad actual. `RuntimePlatformInfo.LegacyPlatform` es siempre `false` y no existe ningún adaptador heredado; el campo y la interfaz de extensión `IPluginNativeServices` existen solo para que en el futuro pueda añadirse un adaptador heredado sin cambiar la API pública (consulta `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nada de esta versión se ejecuta en esos sistemas.

<a id="steam-and-large-address-aware-notes"></a>
## Notas sobre Steam y Large Address Aware

- La distribución de Steam de OMSI 2.3.004 con la cabecera LAA (`7DAB063D...`) figura en la lista de permitidos porque sus direcciones perfiladas son idénticas a las del ejecutable principal. Hasta que se registre en la matriz una sesión de validación de campo, trata cada capacidad sobre ese archivo como `PARTIAL`.
- Steam lanza OMSI por sí mismo; una sesión debe iniciarse mediante `OmsiLaunch.exe` para que exista el handoff. Si se inicia desde Steam, el plugin permanente permanece inactivo (sin handoff, sin hooks).
- Aplicar a `Omsi.exe` un parcheador LAA distinto cambia su hash y lo convierte en una build desconocida.

<a id="related-pages"></a>
## Páginas relacionadas

- [Limitaciones conocidas](known-limitations.md)
- [Estado de validación en runtime](../status/runtime-validation-status.md)
- [Instalación](../getting-started/installation.md)
