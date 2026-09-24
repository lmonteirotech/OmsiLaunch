# Bekende beperkingen

<!-- l10n: source=reference/known-limitations.md -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/known-limitations.md) voor OmsiLaunch 0.1.0-beta3. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina somt, op basis van de code, alles in OmsiLaunch 0.1.0-beta3 op dat `UNAVAILABLE` of `PARTIAL` is of een geaccepteerd risico vormt, zodat gebruikers en integrators niet voortbouwen op gedrag dat het product niet biedt. Elke rij noemt de beperking, de stabiliteit ervan, waarom ze bestaat en waar ze in detail is gedocumenteerd. De Engelse documentatie is normatief; gelokaliseerde kopieën onder `docs/localized/` worden niet op hetzelfde niveau onderhouden en kunnen achterlopen (zie de laatste sectie).

<a id="compatibility"></a>
## Compatibiliteit

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| Alleen `Omsi23004_692EBFBF` wordt ondersteund (`692EBFBF...6243`); de Steam LAA-hash `7DAB063D...D759` staat op de toegestane lijst; de vingerafdruk en het plan ervan zijn gevalideerd met een gecontroleerde kopie, maar gameplay vereist een echte Steam-installatie (de image is het met DRM beveiligde uitvoerbare Steam-bestand) | `PARTIAL` voor Steam LAA | [compatibiliteit](compatibility.md) |
| Alleen Windows 10+ x64; geen Windows 7/8, geen XP, geen ARM64 | `UNAVAILABLE` | [compatibiliteit](compatibility.md) |
| De plugin heeft de **x86**-runtime van .NET 6 nodig, naast de x64-runtime die de controller gebruikt | | [compatibiliteit](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Wereldstart en startopties

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last`, `WorldMode.LastMapState`) is niet geïmplementeerd; er wordt nooit een terugval op een `.osn` op basis van tijdstempels gebruikt | `UNAVAILABLE` (BI-006) | Plandiagnose `OL_E_CAPABILITY_UNAVAILABLE` |
| Expliciete of systeemdatum, -tijd en -jaar (`/date`, `/time`, `/year`, profiel `new.date`/`new.time`/`new.year`, `DateSpec`/`TimeSpec`/`YearSpec`) worden in de spec meegegeven, maar maken het plan niet uitvoerbaar; de plugin weigert modi anders dan `Unset` | `UNAVAILABLE` (`STATICALLY_PARTIAL`) | [sessieprofielen](session-profiles.md), [launchspec](launchspec.md) |
| Weerpreset, ICAO en actueel weer bij de start (`/weather*`, `new.weather`) | `UNAVAILABLE` (`STATICALLY_PARTIAL`, BI-003) | zoals hierboven |
| Model, repaint, HOF, wagennummer en kenteken van het spelersvoertuig bij de start (familie `/vehicle`, `PlayerVehicleSpec`) worden tegen de contentcatalogus opgelost maar niet toegepast; wie ze aanvraagt, maakt het plan niet uitvoerbaar; deterministische headless toewijzing van een PlayerVehicle is een toekomstige uitbreiding | `UNAVAILABLE` (BI-007) | `player.assign-headless` in [capabilities](capabilities.md) |
| Een instappunt op identiteit (`/entrypoint:<identity>`) wordt niet gecorreleerd met de door OMSI getoonde lijst; gebruik `/entrypoint-index` | `PARTIAL` (BI-001) | plancapability `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| Documentoverlays voor toetsenbord en controller (`InputSpec`, `Environment.Keyboard`, `Environment.Controllers`) worden geparseerd maar nooit door een sessie toegepast | `UNAVAILABLE` (BI-005) | capabilities `input.*` |
| `LaunchBehaviorSpec.RestoreConfiguration` en `InstallationSpec.ExpectedExecutableSha256` zijn gedeclareerd maar worden nooit gelezen | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds` (`/shutdown-timeout`, profiel `shutdown-timeout`) wordt geaccepteerd en meegegeven, maar niet gebruikt door de supervisor | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [sessielevenscyclus](../concepts/session-lifecycle.md) |
| `/quiet` en `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| Diagnostiekvlaggen (`/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native`) vullen `DiagnosticsSpec`; het zichtbare effect beperkt zich tot de hosttrace onder `.omsilaunch\diagnostics` | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` zijn validatieharnassen | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Sessie-einde en procesbesturing

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| Het stoppen van een sessie is een geforceerde beëindiging: `session.stop`, "End session" in het systeemvak (sessie beëindigen), Ctrl+C en `CloseAsync` leiden allemaal tot `TerminateProcess`. De afsluitroutine van OMSI wordt niet uitgevoerd, OMSI herschrijft `options.cfg` en zijn logs niet bij het afsluiten, en niet-opgeslagen OMSI-toestand gaat verloren. Dit is bewust: zo wordt voorkomen dat OMSI herstelde bestanden overschrijft. | bewust zo ontworpen | [sessielevenscyclus](../concepts/session-lifecycle.md) |
| Coöperatief afsluiten via WM_CLOSE met een timeout en terugval op beëindiging is niet geïmplementeerd | `UNAVAILABLE` (productbeslissing, S-11; OMSI negeerde in de afsluitende runtimeronde `WM_CLOSE` naar zijn hoofdvenster) | [status van de runtimevalidatie](../status/runtime-validation-status.md) |
| Bij het sluiten van de console of bij afmelden heeft de eigenaar een budget van 4 s om te stoppen en te herstellen; wat overblijft, wordt bij de volgende start via het journal hersteld | sluiten van de console runtime-gevalideerd; afmelden niet getest | [transacties en recovery](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transactie, recovery en lease

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| De installatielease is een semafoor `Local\`: één eigenaar per installatie **per aanmeldsessie**; niet afgedwongen tussen gebruikers; niet vrijgegeven zolang een ander proces een handle vasthoudt; elk proces van dezelfde gebruiker kan de naam bezetten | geaccepteerd risico (S-18) | [transacties en recovery](../concepts/transactions-and-recovery.md) |
| Recovery wordt geweigerd (`OL_E_INSTALLATION_BUSY`) zolang het in het journal vastgelegde OMSI-proces draait, of, bij een journal zonder PID, zolang er een `Omsi.exe` uit die hoofdmap draait | bewust zo ontworpen | zoals hierboven |
| Een oorspronkelijk afwezig overlaypad waarvan de inhoud tijdens de sessie is gewijzigd, blokkeert het herstel (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) totdat het is geïnspecteerd | bewust zo ontworpen | zoals hierboven |
| Journals van vóór de eigendomsvingerafdrukken kunnen alleen worden afgesloten door een sessie met identieke geplande bytes (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | zoals hierboven |
| Alleen paden in bezit van de sessie worden hersteld. De eigen schrijfacties van OMSI tijdens een sessie (`options.cfg` `[last_map]` wanneer geen enkele instelling een overlay op `options.cfg` legt, `Texture\standard.ipr`, caches, `laststn.osn`, chauffeursprofiel, logs) blijven bestaan, net als na een directe OMSI-start | bewust zo ontworpen | [transacties en recovery](../concepts/transactions-and-recovery.md) |
| Het verwijderen van een verouderde `closecheck` vóór een sessie is permanent (vastgelegd, niet hersteld) wanneer `SuppressStaleClosecheckWarning` true is | bewust zo ontworpen | zoals hierboven |

<a id="runtime-control"></a>
## Runtimebesturing

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| `weather.set` wordt geweigerd (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`): OMSI overschrijft beide geprofileerde windkandidaten bij de volgende weertick | `UNAVAILABLE` | [capabilities](capabilities.md) |
| Schrijven naar de kalender (`SetActualDateTime`) | `UNAVAILABLE` (BI-002) | `calendar.set-actual-date-time` |
| Schrijven van stringvariabelen, benoemde triggers, geluidstriggers (eigendom van managed strings in Delphi) | `UNAVAILABLE` (BI-004) | `scripts.string.read` is alleen-lezen |
| Geen verplaatsing van voertuigen, geen ruimtelijke herkoppeling over tiles heen, geen ODE-veilige autoriteit over transformaties; positievelden zijn alleen-lezen | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` vereisen een PlayerVehicle; de headless NEW_MAP-start heeft er geen (een opgeslagen situatie levert er een) | bewust zo ontworpen (BI-007) | RV-004 |
| Runtimewijzigingen (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, D3D-textures) worden niet in het journal vastgelegd en niet hersteld | bewust zo ontworpen | [runtimebesturing](runtime-control.md) |
| Blinde vlek van de handlevingerafdruk: een object van dezelfde klasse en definitie dat tussen twee lijstleesacties op hetzelfde adres opnieuw is aangemaakt, wordt niet als verouderd gedetecteerd; de levensduur bij natuurlijke verwijdering (RV-002) heeft geen veilige runtimeproducent en blijft offline | `PARTIAL` | [runtimebesturing](runtime-control.md) |
| Resultaten zijn begrensd door de mailbox van 64 KiB: lange lijsten worden afgekapt (`truncated=true`); pixelpayloads zijn beperkt tot 48 KiB per `d3d.texture.update` | bewust zo ontworpen | [capabilities](capabilities.md) |
| Kanaal met één verzoek tegelijk: één verzoek per keer per sessie; een bezet slot geeft `OL_E_RUNTIME_CHANNEL_BUSY`; verzoek-id's mogen niet worden hergebruikt | bewust zo ontworpen | [runtimebesturing](runtime-control.md) |
| Telemetrie is een slot met de laatste waarde: uitbarstingen die sneller zijn dan de sampling van 100 ms door de host kunnen tussenliggende events verliezen (volgnummers houden identieke opeenvolgende events uit elkaar; gescheurde samples worden overgeslagen) | `PARTIAL` | [permanente plugin](../concepts/permanent-plugin.md) |
| De D3D-apparaatreset is tijdens runtime waargenomen (`resetting`, `restored`, invalidatie van de generatie); een afzonderlijke overgang `lost` werd niet geproduceerd, omdat het apparaat van OMSI rechtstreeks naar `DEVICENOTRESET` ging | `PARTIAL` (RV-007) | [status van de runtimevalidatie](../status/runtime-validation-status.md) |
| Resultaten van begrensde lijsten (de operaties `timetable.*.list`, `vehicle.variables.list`, `vehicle.string-variables.list`) retourneren hoogstens de rijen die in het runtime-slot van 64 KiB passen; de rest wordt weggelaten met `truncated=true` en een kleinere `returned_count` (documentatieaudit BUG-05). Deze release kent geen paginering | bewust zo ontworpen | [capabilities](capabilities.md) |
| `timetable.logs.read`, `road-vehicles.list`, `humans.list`, `vehicle.constants.list` en `vehicle.curves.list` zijn niet begrensd: een resultaat dat groter is dan het slot, mislukt met `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (bij geen van deze operaties waargenomen op de geteste kaarten) | `PARTIAL` | [capabilities](capabilities.md) |
| De zelfgerapporteerde bewijsstrings (`PublicCapabilityRegistry` `RuntimeValidation`, `GetCapabilitiesAsync` `EvidenceState`) zijn na de afsluitende runtimeronde niet bijgewerkt: `camera.lock` meldt nog steeds `STATICALLY_VALIDATED` en `runtime.d3d.lifecycle.reset` `IMPLEMENTED_NOT_RUNTIME_VALIDATED`. De pagina [status van de runtimevalidatie](../status/runtime-validation-status.md) is gezaghebbend | achterstand in de documentatie, geen verschil in gedrag | [capabilities](capabilities.md) |
| Sommige geavanceerde velden van de kaart-, tile-, pad- en objectgraaf worden niet beschikbaar gemaakt; runtimelezers zijn door het profiel afgeschermde, getypeerde snapshots en nooit willekeurige geheugentoegang | bewust zo ontworpen | [capabilities](capabilities.md) |
| Geheugenleesacties in het proces volgen het patroon eerst controleren, dan gebruiken tegen een levende OMSI; een gelijktijdige wijziging door OMSI tussen de controle en het lezen kan een inconsistente snapshot opleveren (`OL_E_RUNTIME_OPERATION_FAILED`) | geaccepteerd risico (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Lokaal control plane en vertrouwensmodel

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| Vertrouwensmodel op basis van dezelfde gebruiker: de named pipe (`CurrentUserOnly`), de geheugenmappings voor handoff, telemetrie en runtime en de semafoor van de lease zijn toegankelijk voor elk proces van dezelfde Windows-gebruiker. Zo'n proces kan de status lezen, de sessie stoppen of runtime-operaties uitvoeren zodra het de `session_id` heeft gelezen. | geaccepteerd risico (S-06, S-30) | [local control](local-control.md) |
| Het besturingseindpunt bestaat alleen zolang de eigenaar `Running` is; een client ziet `OL_E_NO_ACTIVE_SESSION` (exitcode 4) tijdens het opstarten en nadat de sessie is beëindigd | bewust zo ontworpen | [local control](local-control.md) |
| Als een ander proces de pipenaam al bezit, blijft de eigenaar draaien zonder eindpunt (`ListenFault`), en kan een tweede start ten onrechte `OL_E_SESSION_ALREADY_ACTIVE` melden | geaccepteerd risico | [local control](local-control.md) |
| `.omsilaunch\` erft de ACL van de OMSI-hoofdmap; er wordt geen expliciet toegangsbeheer toegepast | geaccepteerd risico (S-31) | [transacties en recovery](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnostiek en uitvoer

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| Diagnostiek bestaat alleen uit lokale bestanden (`.omsilaunch\diagnostics`); er wordt niets geüpload en er is geen rapportage op afstand | bewust zo ontworpen | [transacties en recovery](../concepts/transactions-and-recovery.md) |
| De bewaartermijn omvat de 50 nieuwste sessies; oudere diagnostiek met sessievoorvoegsel wordt verwijderd wanneer een nieuwe sessie start | bewust zo ontworpen | zoals hierboven |
| JSON-uitvoer en diagnostiek bevatten installatiepaden (`RootPath`, assetmappen, paden `.itx`) | bewust zo ontworpen (lokale gegevens) | |
| Het sessiefoutdialoogvenster van `OmsiLaunchW.exe` toont de foutpayload van de plugin (bijvoorbeeld `{"name":"world.failed",...}`) als bericht in plaats van een zin; de regel `Code:` is correct | cosmetisch | [Windows-systeemvak](windows-tray.md) |
| Een D3D-verzoek dat door de native brug wordt geweigerd vóór enige Direct3D-aanroep, meldt `native_status` correct, maar de tekst van `detail` zegt `HRESULT 0x00000000` | cosmetisch | [capabilities](capabilities.md) |
| Het statusvenster in het systeemvak is een snapshot van de geplande sessie, gemaakt wanneer het wordt geopend; het wordt niet vernieuwd en toont geen live OMSI-waarden | bewust zo ontworpen | [Windows-systeemvak](windows-tray.md) |

<a id="documentation"></a>
## Documentatie

De Engelse pagina's onder `docs/` vormen de normatieve documentatie voor deze release. `docs/localized/<locale>/` bevat vertalingen van dezelfde pagina's van `0.1.0-beta3` (zie [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); waar een vertaling afwijkt van de Engelse tekst, zijn de Engelse tekst en de code gezaghebbend. De historische en legacy pagina's die daar worden vermeld, zijn alleen in het Engels beschikbaar.

Zie ook: [capabilities](capabilities.md), [status van de runtimevalidatie](../status/runtime-validation-status.md), [fouten](errors.md).
