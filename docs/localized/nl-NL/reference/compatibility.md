# Compatibiliteit

<!-- l10n: source=reference/compatibility.md -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/compatibility.md) voor OmsiLaunch 0.1.0-beta3. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

OmsiLaunch stuurt OMSI aan door geprofileerde adressen binnen één exacte build van het uitvoerbare bestand te patchen. Deze pagina beschrijft welke OMSI-builds worden ondersteund, wat er met elke andere build gebeurt en welke eisen de host en de plugin stellen aan het besturingssysteem en de runtime. Bronnen: `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`, `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs` en de projectbestanden.

<a id="supported-omsi-builds"></a>
## Ondersteunde OMSI-builds

Er is precies één buildprofiel, `Omsi23004_692EBFBF` (familie `OMSI_2_3_004_COMMON`). Het accepteert twee uitvoerbare bestanden op basis van de exacte SHA-256:

| Variant | SHA-256 van `Omsi.exe` | Grootte | PE-bestandsversie / productversie | Status |
| --- | --- | --- | --- | --- |
| Geprofileerd uitvoerbaar bestand (`ALTERNATE_LAA`) | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 bytes | 2.2.032 / 2.3.004 | `STABLE_BETA`; elke runtimevalidatie in de matrix is op dit bestand uitgevoerd |
| Steam LAA (`STEAM_LAA`) | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | niet gecontroleerd | | Geaccepteerd via de allowlist omdat het dezelfde geprofileerde native indeling heeft en alleen in de headers van het uitvoerbare bestand verschilt; **niet runtime-gevalideerd** (`profiles` meldt `runtime_validated=false`, `validation_status=pending_beta_field_validation`). `PARTIAL`. |

`OmsiLaunch.exe profiles` drukt deze tabel af als JSON. Versienummers worden niet gebruikt voor acceptatie: alleen de SHA-256 (en, voor het primaire uitvoerbare bestand, de exacte grootte) telt. Geen enkele andere OMSI 2-build, geen gepatcht uitvoerbaar bestand en geen 4 GB-gepatchte kopie met een andere hash wordt ondersteund.

<a id="what-happens-with-an-unknown-build"></a>
## Wat er gebeurt met een onbekende build

| Fase | Controle | Resultaat |
| --- | --- | --- |
| Plannen (`PlanSessionAsync`, `/plan`, `/validate`) | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | De vereiste capability `omsi.profile.OMSI23004` is `UNAVAILABLE`; diagnose `OL_E_UNSUPPORTED_BUILD`; `SessionPlan.IsRunnable=false`. CLI-exit 1 voor een start, of 3 (`UnsupportedProfile`) wanneer de fout als exception ontsnapt. |
| Start (`StartSessionAsync`) | De spec wordt opnieuw gepland en de hash van `Omsi.exe` opnieuw berekend | Een plan dat niet langer uitvoerbaar is (bijvoorbeeld omdat het uitvoerbare bestand na het plannen is gewijzigd, of omdat een aanroeper `IsRunnable` heeft aangepast), wordt geweigerd met `OL_E_PLAN_NOT_RUNNABLE`; er wordt geen transactie geopend en geen proces gestart. |
| In het proces (`PluginRuntime.Start`) | `NativeServices.ValidateBuild` vereist dat de `BuildProfileId` van de handoff `Omsi23004_692EBFBF` is **en** dat `NativeValidateBuild()` slaagt tegen het draaiende image | Telemetrie `plugin.build.invalid`; de host laat de sessie mislukken met `OL_E_BUILD_VALIDATION_FAILED`; er wordt geen native hook geactiveerd; OMSI wordt beëindigd en de transactie hersteld. |

Omdat de hash van het uitvoerbare bestand wordt vergeleken met de groottes en bytes van de geprofileerde globals, is de controle in het proces de laatste verdedigingslinie tegen een kopie die de hashcontrole heeft doorstaan, maar waarvan het image bij het laden afwijkt. Er is geen terugvalprofiel en geen heuristische matching.

<a id="operating-system-and-architecture"></a>
## Besturingssysteem en architectuur

`CurrentWindowsX64Platform.Detect` berekent `RuntimePlatformInfo`. Het huidige platform wordt alleen ondersteund als aan alle volgende voorwaarden wordt voldaan:

| Vereiste | Controle | Fout bij overtreding |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 of later | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64-bits Windows en een 64-bits hostproces | `OSArchitecture == X64` en `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Beschrijfbare installatie | De hoofdmap bestaat, is niet alleen-lezen en bevat `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` meldt daarnaast `OmsiArchitecture` en `PluginArchitecture` als `X86` (OMSI is een 32-bits proces; de plugin-closure is x86 en draait onder WOW64), `LegacyPlatform=false` en `Wow64Available`. ARM64-Windows wordt niet ondersteund, ook niet waar x64-emulatie bestaat, omdat het hostproces zelf x64 moet zijn.

<a id="net-requirements"></a>
## .NET-vereisten

| Component | Runtime | Opmerkingen |
| --- | --- | --- |
| Controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | De native bootstrapper zoekt de runtime op via `hostfxr` met behulp van de meegeleverde `nethost.dll`. Een ontbrekende runtime wordt door de shim gemeld (exitcodes 100-106; zie [CLI](cli.md) en [exitcodes](exit-codes.md)). |
| Plugin-closure (`plugins\OmsiLaunch.Plugin.dll` via `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), gehost door DNNE 2.0.6 binnen `Omsi.exe` | Vereist dat de x86-versie van de .NET 6 Desktop/Core-runtime op de computer is geïnstalleerd; de 64-bits runtime alleen is niet voldoende voor de plugin. |
| Native brug (`plugins\OmsiLaunch.Native.x86.dll`) | native x86 | Wordt alleen geladen vanuit `plugins\` (zie [permanente plugin](../concepts/permanent-plugin.md)). |

## Legacy-platforms

Windows 7, Windows 8.x, Windows XP en andere systemen met NT 6 en ouder vallen buiten de huidige ondersteuningsgrens. `RuntimePlatformInfo.LegacyPlatform` is altijd `false` en er bestaat geen legacy-adapter; het veld en de naad `IPluginNativeServices` bestaan alleen zodat in de toekomst een legacy-adapter kan worden toegevoegd zonder de openbare API te wijzigen (zie `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Niets in deze release draait op die systemen.

<a id="steam-and-large-address-aware-notes"></a>
## Opmerkingen over Steam en Large Address Aware

- De Steam-distributie van OMSI 2.3.004 met de LAA-header (`7DAB063D...`) staat op de allowlist omdat de geprofileerde adressen identiek zijn aan die van het primaire uitvoerbare bestand. Zolang er geen veldvalidatiesessie in de matrix is vastgelegd, moet je elke capability op dat bestand als `PARTIAL` beschouwen.
- Steam start OMSI zelf; een sessie moet via `OmsiLaunch.exe` worden gestart, zodat de handoff bestaat. Bij een start vanuit Steam blijft de permanente plugin inactief (geen handoff, geen hooks).
- Het toepassen van een andere LAA-patcher op `Omsi.exe` wijzigt de hash en maakt er een onbekende build van.

<a id="related-pages"></a>
## Gerelateerde pagina's

- [Bekende beperkingen](known-limitations.md)
- [Status van de runtimevalidatie](../status/runtime-validation-status.md)
- [Installatie](../getting-started/installation.md)
