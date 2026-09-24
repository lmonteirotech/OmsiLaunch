# Limitazioni note

<!-- l10n: source=reference/known-limitations.md -->
> Traduzione della [pagina originale in inglese](../../../reference/known-limitations.md) di OmsiLaunch 0.1.0-beta3. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina elenca, a partire dal codice, tutto ciò che in OmsiLaunch 0.1.0-beta3 è `UNAVAILABLE`, `PARTIAL` o un rischio accettato, affinché utenti e integratori non facciano affidamento su comportamenti che il prodotto non fornisce. Ogni riga indica la limitazione, la sua stabilità, il motivo per cui esiste e dove è documentata in dettaglio. La documentazione inglese è normativa; le copie localizzate in `docs/localized/` non sono mantenute allo stesso livello e possono essere in ritardo (vedere l'ultima sezione).

<a id="compatibility"></a>
## Compatibilità

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| È supportato solo `Omsi23004_692EBFBF` (`692EBFBF...6243`); l'hash Steam LAA `7DAB063D...D759` è nell'elenco consentito; il suo fingerprint e il piano sono stati validati con una copia controllata, ma il gameplay richiede un'installazione Steam autentica (l'immagine è l'eseguibile Steam protetto da DRM) | `PARTIAL` per Steam LAA | [compatibilità](compatibility.md) |
| Solo Windows 10+ x64; nessun supporto per Windows 7/8, XP o ARM64 | `UNAVAILABLE` | [compatibilità](compatibility.md) |
| Il plugin richiede il runtime .NET 6 **x86** oltre al runtime x64 usato dal controller | | [compatibilità](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Avvio del mondo e opzioni di avvio

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last`, `WorldMode.LastMapState`) non è implementato; non viene mai sostituito con un fallback `.osn` basato sui timestamp | `UNAVAILABLE` (BI-006) | Voce di diagnostica del piano `OL_E_CAPABILITY_UNAVAILABLE` |
| Data, ora e anno espliciti o di sistema (`/date`, `/time`, `/year`, `new.date`/`new.time`/`new.year` del profilo, `DateSpec`/`TimeSpec`/`YearSpec`) sono trasportati nella specifica ma rendono il piano non eseguibile; il plugin rifiuta le modalità diverse da `Unset` | `UNAVAILABLE` (`STATICALLY_PARTIAL`) | [profili di sessione](session-profiles.md), [launchspec](launchspec.md) |
| Preset meteo, ICAO e meteo reale attuale all'avvio (`/weather*`, `new.weather`) | `UNAVAILABLE` (`STATICALLY_PARTIAL`, BI-003) | come sopra |
| Modello del veicolo del giocatore, repaint (livrea), HOF, numero di flotta e targa all'avvio (famiglia `/vehicle`, `PlayerVehicleSpec`) vengono risolti rispetto al catalogo dei contenuti ma non applicati; richiederli rende il piano non eseguibile; l'assegnazione headless deterministica del PlayerVehicle è un'estensione futura | `UNAVAILABLE` (BI-007) | `player.assign-headless` in [capability](capabilities.md) |
| Il punto di ingresso per identità (`/entrypoint:<identity>`) non è correlato con l'elenco presentato da OMSI; usare `/entrypoint-index` | `PARTIAL` (BI-001) | capability del piano `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| Gli overlay dei documenti di tastiera e controller (`InputSpec`, `Environment.Keyboard`, `Environment.Controllers`) vengono analizzati ma mai applicati da una sessione | `UNAVAILABLE` (BI-005) | capability `input.*` |
| `LaunchBehaviorSpec.RestoreConfiguration` e `InstallationSpec.ExpectedExecutableSha256` sono dichiarati ma mai letti | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds` (`/shutdown-timeout`, `shutdown-timeout` del profilo) viene accettato e trasportato ma non utilizzato dal supervisore | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [ciclo di vita della sessione](../concepts/session-lifecycle.md) |
| `/quiet` e `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| I flag di diagnostica (`/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native`) popolano `DiagnosticsSpec`; l'effetto visibile è limitato alla traccia dell'host in `.omsilaunch\diagnostics` | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` sono harness di validazione | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fine della sessione e controllo dei processi

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| L'arresto della sessione è una terminazione forzata: `session.stop`, «End session» nell'area di notifica, Ctrl+C e `CloseAsync` portano tutti a `TerminateProcess`. La routine di chiusura di OMSI non viene eseguita, OMSI non riscrive `options.cfg` né i propri log all'uscita, e qualsiasi stato di OMSI non salvato va perso. È una scelta deliberata: impedisce a OMSI di sovrascrivere i file ripristinati. | per scelta progettuale | [ciclo di vita della sessione](../concepts/session-lifecycle.md) |
| L'arresto cooperativo tramite WM_CLOSE con un timeout e una terminazione di riserva non è implementato | `UNAVAILABLE` (decisione di prodotto, S-11; nel ciclo di chiusura a runtime OMSI ha ignorato `WM_CLOSE` inviato alla sua finestra principale) | [stato della validazione a runtime](../status/runtime-validation-status.md) |
| Alla chiusura della console o al logoff l'owner dispone di un budget di 4 s per arrestare e ripristinare; ciò che resta viene recuperato dal journal all'avvio successivo | chiusura della console validata a runtime; logoff non verificato | [transazioni e recupero](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transazione, recupero e lease

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| Il lease dell'installazione (blocco esclusivo) è un semaforo `Local\`: un solo owner per installazione **per sessione di accesso**; non è imposto tra utenti diversi; non viene rilasciato finché un altro processo detiene un handle; qualsiasi processo dello stesso utente può detenere il nome | rischio accettato (S-18) | [transazioni e recupero](../concepts/transactions-and-recovery.md) |
| Il recupero viene rifiutato (`OL_E_INSTALLATION_BUSY`) mentre è in esecuzione il processo OMSI registrato nel journal o, per un journal senza PID, un qualsiasi `Omsi.exe` di quella radice | per scelta progettuale | come sopra |
| Un percorso di overlay originariamente assente il cui contenuto è cambiato durante la sessione blocca il ripristino (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) finché non viene ispezionato | per scelta progettuale | come sopra |
| I journal precedenti ai fingerprint di proprietà possono essere chiusi solo da una sessione con byte pianificati identici (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | come sopra |
| Vengono ripristinati solo i percorsi di proprietà della sessione. Le scritture proprie di OMSI durante una sessione (`options.cfg` `[last_map]` quando nessuna impostazione applica un overlay a `options.cfg`, `Texture\standard.ipr`, cache, `laststn.osn`, profilo del conducente, log) persistono, come dopo un avvio diretto di OMSI | per scelta progettuale | [transazioni e recupero](../concepts/transactions-and-recovery.md) |
| La rimozione di un `closecheck` obsoleto prima di una sessione è permanente (registrata, non ripristinata) quando `SuppressStaleClosecheckWarning` è true | per scelta progettuale | come sopra |

<a id="runtime-control"></a>
## Controllo runtime

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| `weather.set` viene rifiutato (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`): OMSI sovrascrive entrambi i candidati del vento profilati al successivo tick del meteo | `UNAVAILABLE` | [capability](capabilities.md) |
| Scritture del calendario (`SetActualDateTime`) | `UNAVAILABLE` (BI-002) | `calendar.set-actual-date-time` |
| Scritture delle variabili stringa, trigger con nome, trigger sonori (proprietà delle stringhe gestite da Delphi) | `UNAVAILABLE` (BI-004) | `scripts.string.read` è di sola lettura |
| Nessun riposizionamento dei veicoli, nessuna riassociazione spaziale tra tile, nessuna autorità di trasformazione sicura per ODE; i campi di posizione sono di sola lettura | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` richiedono un PlayerVehicle; l'avvio headless NEW_MAP non ne ha uno (una situazione salvata lo fornisce) | per scelta progettuale (BI-007) | RV-004 |
| Le modifiche a runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, texture D3D) non sono registrate nel journal e non vengono ripristinate | per scelta progettuale | [controllo runtime](runtime-control.md) |
| Punto cieco del fingerprint degli handle: un oggetto della stessa classe e definizione ricreato allo stesso indirizzo tra due letture dell'elenco non viene rilevato come obsoleto; la durata in caso di rimozione naturale (RV-002) non ha un produttore runtime sicuro e resta solo offline | `PARTIAL` | [controllo runtime](runtime-control.md) |
| I risultati sono limitati dalla mailbox da 64 KiB: gli elenchi lunghi vengono troncati (`truncated=true`); i payload di pixel sono limitati a 48 KiB per `d3d.texture.update` | per scelta progettuale | [capability](capabilities.md) |
| Canale a richiesta singola: una richiesta alla volta per sessione; uno slot occupato restituisce `OL_E_RUNTIME_CHANNEL_BUSY`; gli id di richiesta non devono essere riutilizzati | per scelta progettuale | [controllo runtime](runtime-control.md) |
| La telemetria è uno slot con l'ultimo valore: raffiche più rapide del campionamento di 100 ms dell'host possono perdere eventi intermedi (i numeri di sequenza mantengono distinti eventi consecutivi identici; i campioni incoerenti vengono saltati) | `PARTIAL` | [plugin permanente](../concepts/permanent-plugin.md) |
| Il reset del dispositivo D3D è stato osservato a runtime (`resetting`, `restored`, invalidazione della generazione); una transizione `lost` distinta non è stata prodotta perché il dispositivo di OMSI è passato direttamente a `DEVICENOTRESET` | `PARTIAL` (RV-007) | [stato della validazione a runtime](../status/runtime-validation-status.md) |
| I risultati degli elenchi limitati (le operazioni `timetable.*.list`, `vehicle.variables.list`, `vehicle.string-variables.list`) restituiscono al massimo le righe che rientrano nello slot runtime da 64 KiB; le restanti vengono escluse con `truncated=true` e un `returned_count` più piccolo (audit della documentazione BUG-05). In questa release non è prevista la paginazione | per scelta progettuale | [capability](capabilities.md) |
| `timetable.logs.read`, `road-vehicles.list`, `humans.list`, `vehicle.constants.list` e `vehicle.curves.list` non sono limitati: un risultato più grande dello slot fallisce con `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (non osservato per nessuno di essi sulle mappe testate) | `PARTIAL` | [capability](capabilities.md) |
| Le stringhe di evidenza autodichiarate (`PublicCapabilityRegistry` `RuntimeValidation`, `GetCapabilitiesAsync` `EvidenceState`) non sono state aggiornate dopo il ciclo di chiusura a runtime: `camera.lock` riporta ancora `STATICALLY_VALIDATED` e `runtime.d3d.lifecycle.reset` `IMPLEMENTED_NOT_RUNTIME_VALIDATED`. Fa fede la pagina [stato della validazione a runtime](../status/runtime-validation-status.md) | ritardo della documentazione, non una differenza di comportamento | [capability](capabilities.md) |
| Alcuni campi avanzati del grafo di mappe/tile/percorsi/oggetti non sono esposti; i lettori runtime sono snapshot tipizzati vincolati al profilo, mai accessi arbitrari alla memoria | per scelta progettuale | [capability](capabilities.md) |
| Le letture della memoria in-process seguono lo schema verifica-poi-uso su un OMSI attivo; una modifica concorrente di OMSI tra la verifica e la lettura può produrre uno snapshot incoerente (`OL_E_RUNTIME_OPERATION_FAILED`) | rischio accettato (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Piano di controllo locale e modello di fiducia

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| Modello di fiducia basato sullo stesso utente: la named pipe (`CurrentUserOnly`), i mapping di memoria di handoff/telemetria/runtime e il semaforo del lease sono accessibili a qualsiasi processo dello stesso utente Windows. Un tale processo può leggere lo stato, arrestare la sessione o eseguire operazioni runtime dopo aver letto il `session_id`. | rischio accettato (S-06, S-30) | [controllo locale](local-control.md) |
| L'endpoint di controllo esiste solo mentre l'owner è `Running`; un client riceve `OL_E_NO_ACTIVE_SESSION` (uscita 4) durante l'avvio e dopo la fine della sessione | per scelta progettuale | [controllo locale](local-control.md) |
| Se un altro processo possiede già il nome della pipe, l'owner continua a essere eseguito senza endpoint (`ListenFault`), e un secondo avvio può segnalare erroneamente `OL_E_SESSION_ALREADY_ACTIVE` | rischio accettato | [controllo locale](local-control.md) |
| `.omsilaunch\` eredita l'ACL della radice di OMSI; non viene applicato alcun controllo di accesso esplicito | rischio accettato (S-31) | [transazioni e recupero](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnostica e output

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| La diagnostica è costituita solo da file locali (`.omsilaunch\diagnostics`); nulla viene caricato e non esiste alcuna segnalazione remota | per scelta progettuale | [transazioni e recupero](../concepts/transactions-and-recovery.md) |
| La conservazione mantiene le 50 sessioni più recenti; la diagnostica più vecchia con prefisso di sessione viene eliminata all'avvio di una nuova sessione | per scelta progettuale | come sopra |
| L'output JSON e la diagnostica includono i percorsi dell'installazione (`RootPath`, directory degli asset, percorsi `.itx`) | per scelta progettuale (dati locali) | |
| La finestra di dialogo di errore della sessione di `OmsiLaunchW.exe` mostra come messaggio il payload di errore del plugin (per esempio `{"name":"world.failed",...}`) anziché una frase; la riga `Code:` è corretta | estetico | [area di notifica di Windows](windows-tray.md) |
| Una richiesta D3D rifiutata dal bridge nativo prima di qualsiasi chiamata Direct3D riporta correttamente `native_status`, ma il suo testo `detail` indica `HRESULT 0x00000000` | estetico | [capability](capabilities.md) |
| La finestra di stato dell'area di notifica è uno snapshot (istantanea) della sessione pianificata acquisito alla sua apertura; non si aggiorna e non mostra valori di OMSI in tempo reale | per scelta progettuale | [area di notifica di Windows](windows-tray.md) |

<a id="documentation"></a>
## Documentazione

Le pagine inglesi in `docs/` costituiscono la documentazione normativa di questa release. `docs/localized/<locale>/` contiene le traduzioni delle stesse pagine `0.1.0-beta3` (vedere [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); dove una traduzione differisce dal testo inglese, fanno fede il testo inglese e il codice. Le pagine storiche e legacy elencate lì sono disponibili solo in inglese.

Correlati: [capability](capabilities.md), [stato della validazione a runtime](../status/runtime-validation-status.md), [errori](errors.md).
