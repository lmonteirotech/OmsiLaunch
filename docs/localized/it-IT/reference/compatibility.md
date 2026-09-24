# Compatibilità

<!-- l10n: source=reference/compatibility.md -->
> Traduzione della [pagina originale in inglese](../../../reference/compatibility.md) di OmsiLaunch 0.1.0-beta3. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

OmsiLaunch pilota OMSI applicando patch a indirizzi profilati all'interno di un build eseguibile esatto. Questa pagina indica quali build di OMSI sono supportati, che cosa accade con qualsiasi altro build e i requisiti di sistema operativo e di runtime dell'host e del plugin. Fonti: `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`, `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs` e i file di progetto.

<a id="supported-omsi-builds"></a>
## Build di OMSI supportati

Esiste esattamente un profilo di build, `Omsi23004_692EBFBF` (famiglia `OMSI_2_3_004_COMMON`). Accetta due eseguibili in base allo SHA-256 esatto:

| Variante | SHA-256 di `Omsi.exe` | Dimensione | Versione file PE / prodotto | Stato |
| --- | --- | --- | --- | --- |
| Eseguibile profilato (`ALTERNATE_LAA`) | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 byte | 2.2.032 / 2.3.004 | `STABLE_BETA`; ogni validazione a runtime della matrice è stata eseguita su questo file |
| Steam LAA (`STEAM_LAA`) | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | non verificata | | Accettato tramite allow-list perché condivide la struttura nativa profilata e differisce solo nelle intestazioni dell'eseguibile; **non validato a runtime** (`profiles` riporta `runtime_validated=false`, `validation_status=pending_beta_field_validation`). `PARTIAL`. |

`OmsiLaunch.exe profiles` stampa questa tabella come JSON. I numeri di versione non vengono usati per l'accettazione: contano solo lo SHA-256 (e, per l'eseguibile principale, la dimensione esatta). Non è supportato nessun altro build di OMSI 2, nessun eseguibile modificato con patch e nessuna copia con patch 4 GB con un hash diverso.

<a id="what-happens-with-an-unknown-build"></a>
## Che cosa accade con un build sconosciuto

| Fase | Verifica | Esito |
| --- | --- | --- |
| Pianificazione (`PlanSessionAsync`, `/plan`, `/validate`) | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | La capability richiesta `omsi.profile.OMSI23004` è `UNAVAILABLE`; diagnostica `OL_E_UNSUPPORTED_BUILD`; `SessionPlan.IsRunnable=false`. Uscita CLI 1 per un avvio, oppure 3 (`UnsupportedProfile`) quando l'errore sfugge come eccezione. |
| Avvio (`StartSessionAsync`) | La spec viene ripianificata e l'hash di `Omsi.exe` viene ricalcolato | Un piano non più eseguibile (per esempio perché l'eseguibile è cambiato dopo la pianificazione, o perché un chiamante ha modificato `IsRunnable`) viene rifiutato con `OL_E_PLAN_NOT_RUNNABLE`; non viene aperta alcuna transazione e non viene avviato alcun processo. |
| Nel processo (`PluginRuntime.Start`) | `NativeServices.ValidateBuild` richiede che il `BuildProfileId` dell'handoff sia `Omsi23004_692EBFBF` **e** che `NativeValidateBuild()` abbia esito positivo sull'immagine in esecuzione | Telemetria `plugin.build.invalid`; l'host fa fallire la sessione con `OL_E_BUILD_VALIDATION_FAILED`; nessun hook nativo viene armato; OMSI viene terminato e la transazione ripristinata. |

Poiché l'hash dell'eseguibile viene confrontato con le dimensioni e i byte delle variabili globali profilate, la verifica nel processo è l'ultima linea di difesa contro una copia che ha superato la verifica dell'hash ma la cui immagine differisce al momento del caricamento. Non esiste un profilo di ripiego né una corrispondenza euristica.

<a id="operating-system-and-architecture"></a>
## Sistema operativo e architettura

`CurrentWindowsX64Platform.Detect` calcola `RuntimePlatformInfo`. La piattaforma corrente è supportata solo quando sono soddisfatte tutte le condizioni seguenti:

| Requisito | Verifica | Errore in caso di violazione |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 o successivo | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows a 64 bit e processo host a 64 bit | `OSArchitecture == X64` e `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Installazione scrivibile | La directory radice esiste, non è di sola lettura e contiene `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` riporta inoltre `OmsiArchitecture` e `PluginArchitecture` come `X86` (OMSI è un processo a 32 bit; la chiusura del plugin è x86 e viene eseguita sotto WOW64), `LegacyPlatform=false` e `Wow64Available`. Windows ARM64 non è supportato, anche dove esiste l'emulazione x64, perché il processo host deve essere esso stesso x64.

<a id="net-requirements"></a>
## Requisiti .NET

| Componente | Runtime | Note |
| --- | --- | --- |
| Controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | Il bootstrapper nativo individua il runtime tramite `hostfxr` attraverso il `nethost.dll` incluso nel pacchetto. Un runtime mancante viene segnalato dallo shim (codici di uscita 100-106; vedere [CLI](cli.md) e [codici di uscita](exit-codes.md)). |
| Chiusura del plugin (`plugins\OmsiLaunch.Plugin.dll` tramite `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), ospitato da DNNE 2.0.6 all'interno di `Omsi.exe` | Richiede che sul computer sia installato il runtime .NET 6 Desktop/Core x86; il solo runtime a 64 bit non è sufficiente per il plugin. |
| Bridge nativo (`plugins\OmsiLaunch.Native.x86.dll`) | nativo x86 | Caricato solo da `plugins\` (vedere [plugin permanente](../concepts/permanent-plugin.md)). |

<a id="legacy-platforms"></a>
## Piattaforme legacy

Windows 7, Windows 8.x, Windows XP e gli altri sistemi NT 6 e precedenti sono al di fuori dell'attuale perimetro di supporto. `RuntimePlatformInfo.LegacyPlatform` è sempre `false` e non esiste alcun adattatore legacy; il campo e il punto di separazione `IPluginNativeServices` esistono solo affinché un futuro adattatore legacy possa essere aggiunto senza modificare l'API pubblica (vedere `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nulla in questa release viene eseguito su tali sistemi.

<a id="steam-and-large-address-aware-notes"></a>
## Note su Steam e Large Address Aware

- La distribuzione Steam di OMSI 2.3.004 con l'intestazione LAA (`7DAB063D...`) è inclusa nell'allow-list perché i suoi indirizzi profilati sono identici a quelli dell'eseguibile principale. Finché nella matrice non viene registrata una sessione di validazione sul campo, considerare ogni capability su quel file come `PARTIAL`.
- Steam avvia OMSI autonomamente; una sessione deve essere avviata tramite `OmsiLaunch.exe` affinché esista l'handoff. Se OMSI viene avviato da Steam, il plugin permanente resta inerte (nessun handoff, nessun hook).
- L'applicazione di un diverso patcher LAA a `Omsi.exe` ne modifica l'hash e lo rende un build sconosciuto.

<a id="related-pages"></a>
## Pagine correlate

- [Limitazioni note](known-limitations.md)
- [Stato della validazione a runtime](../status/runtime-validation-status.md)
- [Installazione](../getting-started/installation.md)
