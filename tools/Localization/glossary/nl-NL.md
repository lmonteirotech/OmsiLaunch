# OmsiLaunch 0.1.0-beta3 — terminologielijst nl-NL

Er bestaat geen eerdere nl-NL-vertaling; deze lijst is de enige terminologiebron voor alle nl-NL-pagina's.

## Aanspreekvorm en register

- Spreek de lezer aan met **"je"** (niet "u"), consequent op alle pagina's; gebruik "jouw" alleen voor nadruk, anders "je". Waar het natuurlijk kan, liever een onpersoonlijke of passieve constructie ("De sessie wordt beëindigd", "Voer `/plan` uit").
- Register: zakelijke, precieze Nederlandse technische documentatie in de stijl van Microsoft/.NET-documentatie in het Nederlands. Korte zinnen, geen spreektaal, geen uitroeptekens. Gangbare Engelse IT-termen blijven Engels (zie tabel); ze krijgen Nederlandse lidwoorden en worden als Nederlandse woorden verbogen/samengesteld.
- Imperatief in instructies: "Start", "Voer … uit", "Kies" (de je-vorm zonder "je").

## Interpunctie en typografie

- Nederlandse spelling volgens het Groene Boekje (2015). Samenstellingen schrijf je aaneen (runtimeopdracht, runtimebewijs, sessieplan); bij een samenstelling met een afkorting of hoofdletterwoord gebruik je een koppelteken (OMSI-proces, API-route, CLI-vlag, JSON-envelope, D3D-fout, Steam-installatie). Plak nooit Nederlandse tekst aan een inline-codespan; schrijf "de route `session stop`" in plaats van een samenstelling met de code.
- Decimaalteken in lopende tekst: komma; getallen en eenheden die in het Engels staan (`64 KiB`, `250 ms`, `2 s`, 607 ms) blijven letterlijk ongewijzigd. Spatie tussen getal en eenheid zoals in de bron.
- Aanhalingstekens in lopende tekst: rechte dubbele aanhalingstekens "…", zoals in de bron. Engelse UI-teksten staan altijd als inline code, niet tussen aanhalingstekens, tenzij de bron ze zo schrijft.
- Titels en koppen: alleen het eerste woord en eigennamen met hoofdletter ("Bekende beperkingen", niet "Bekende Beperkingen").
- Geen spatie vóór `:` `;` `?` `!`. Pijlen (`->`, `→`) en gedachtestreepjes zoals in de bron.
- Beletselteken: "..." zoals in de bron.
- Windows-UI: Nederlandse Windows toont de Engelse OmsiLaunch-teksten (het product heeft geen nl-vertaling). Presenteer dus nooit een Nederlandse vertaling als door het product getoonde tekst; leg de betekenis uit in de omringende tekst, bijvoorbeeld: het menu-item `End session` (sessie beëindigen).

## Termen

| English | nl-NL | note |
|---|---|---|
| session | sessie | de sessie |
| session owner / owner | sessie-eigenaar / eigenaar | het eigenaarproces van de sessie; "owner" = "de eigenaar" |
| client (mode) | client(modus) | Engels blijft; "clientmodus", "als client" |
| owner process | eigenaarproces | |
| launch (noun) | start, opstart | "launch plan" = startplan; "launch flags" = startvlaggen |
| launch (verb) | starten | "the launched `Omsi.exe`" = de gestarte `Omsi.exe` |
| plan (noun) | plan | het plan; "session plan" = sessieplan |
| plan (verb) | plannen | "planned mutations" = geplande wijzigingen; "planned values" = geplande waarden |
| runnable / not runnable | uitvoerbaar / niet uitvoerbaar | "makes the plan not runnable" = maakt het plan niet uitvoerbaar |
| start | starten / start | |
| stop | stoppen / stop | "stop path" = stoppad |
| end session | sessie beëindigen | UI-literal `End session` blijft Engels in code |
| canonical stop | canonieke stop | "canonical owner stop path" = canoniek stoppad van de eigenaar |
| restore | herstellen / herstel | bestanden terugzetten; "exact restore" = exact herstel |
| recovery | herstel na crash, recovery | gebruik "recovery" voor de functie/het mechanisme (`/recover`), "herstellen" voor het terugzetten van bestanden; "crash recovery" = crashrecovery |
| pending (journal) | openstaand (journal) | "a transaction is pending" = er staat een transactie open |
| journal | journal | Engels blijft (het journal, `journal.json`); niet "logboek" (dat is log) |
| transaction | transactie | |
| overlay | overlay | Engels blijft; de overlay, overlays; "temporary overlays" = tijdelijke overlays |
| backup | back-up | met koppelteken; "backup copy" = back-upkopie |
| snapshot | snapshot | Engels blijft; de snapshot; in `Status`-venster: momentopname mag als uitleg |
| installation | installatie | |
| installation root | installatiemap (root) | "de installatiemap"; `<root>` blijft code; "OMSI root" = OMSI-hoofdmap |
| package | pakket | "session-profile packages" = sessieprofielpakketten |
| release package | releasepakket | |
| manifest | manifest | het manifest |
| permanent plugin | permanente plugin | "plugin closure" = plugin-closure (set plugin-bestanden) |
| native bridge | native brug | "native" blijft Engels als bijvoeglijk naamwoord |
| runtime | runtime | Engels blijft; de runtime |
| runtime operation | runtime-operatie | "operation id" = operatie-id |
| runtime command | runtimeopdracht | "runtime command channel" = runtimeopdrachtkanaal |
| runtime slot / mailbox | runtime-slot / mailbox | Engels blijft; "telemetry slot" = telemetrieslot |
| control plane | control plane | Engels blijft; "local control plane" = lokaal control plane |
| local control | local control (lokale besturing) | Engels blijft als naam van het kanaal; uitleg "lokale besturing" bij eerste gebruik |
| named pipe | named pipe | Engels blijft; de named pipe |
| frame (protocol) | frame | het frame |
| envelope (JSON) | envelope | Engels blijft; de JSON-envelope |
| handle | handle | Engels blijft; de handle |
| stale handle | verouderde handle | "stale detection" = detectie van verouderde handles |
| capability | capability | Engels blijft; de capability, capabilities (gangbaar in NL-ontwikkelaarsdocs) |
| capability registry | capabilityregister | |
| bounded list | begrensde lijst | |
| truncated | afgekapt | "long lists are truncated" = lange lijsten worden afgekapt; `truncated=true` blijft code |
| row | rij | "list rows" = lijstrijen |
| evidence | bewijs | "runtime evidence" = runtimebewijs; "evidence id" = bewijs-id |
| runtime-validated | runtime-gevalideerd | "not runtime validated" = niet runtime-gevalideerd (zelfde sterkte) |
| statically validated | statisch gevalideerd | |
| offline test | offline test | "offline only" = alleen offline; "offline-validated" = offline gevalideerd |
| gate (documentation gate) | gate (documentatiegate) | Engels blijft; "controlepoort" alleen als uitleg |
| tray icon | systeemvakpictogram | "tray" = systeemvak; paginatitel "Windows Tray" = Windows-systeemvak |
| notification area | systeemvak (meldingsgebied) | Windows-NL gebruikt "systeemvak"; "meldingsgebied" als synoniem bij eerste gebruik |
| status window | statusvenster | |
| confirmation dialog | bevestigingsdialoogvenster | |
| message box | berichtvenster | |
| failure dialog | foutdialoogvenster | |
| tooltip | knopinfo | Windows-NL-term; "tooltip" niet gebruiken |
| context menu | contextmenu | |
| Explorer restart | herstart van Verkenner (Explorer) | Windows-NL: Verkenner; `explorer.exe` blijft code |
| entry point | instappunt | ook voor "entrypoint"; `/entrypoint-index` blijft code |
| new map | nieuwe kaart | NEW_MAP blijft token; "new session" (modus) = nieuwe sessie |
| saved situation | opgeslagen situatie | SAVED_SITUATION blijft token |
| map | kaart | "map directory" = kaartmap |
| splash screen | opstartscherm (splash screen) | "managed splash" = beheerd opstartscherm |
| Internet Textures | internettextures | "Internet textures `Override`" = internettextures in modus `Override` |
| session profile | sessieprofiel | |
| preset | preset | Engels blijft; de preset; "weather preset" = weerpreset |
| setting | instelling | "semantic setting" = semantische instelling |
| player vehicle | spelersvoertuig | `PlayerVehicle` blijft code |
| road vehicle | wegvoertuig | `RoadVehicle` blijft code |
| human (pedestrian/passenger object) | mens (voetganger/passagier) | object `Human` blijft code; in prose "mens-object" of "persoon (voetganger/passagier)" |
| timetable | dienstregeling | |
| track entry | trackitem (track entry) | "track entries" = trackitems |
| tour entry | omloopitem (tour entry) | "tour" = omloop |
| ticket | kaartje (ticket) | "ticket" mag in tabellen; kies één per pagina: "kaartje" |
| driver | chauffeur | "driver profile" = chauffeursprofiel (`Drivers\`) |
| fleet number | wagennummer | |
| registration (plate) | kenteken | |
| repaint | repaint | Engels blijft (gangbaar in OMSI-community) |
| spawn | spawnen / spawn | Engels blijft; "spawnen" als werkwoord |
| camera lock | cameravergrendeling | `camera.lock` blijft code |
| device reset | apparaatreset | D3D device reset = D3D-apparaatreset |
| render thread | renderthread | |
| texture | texture | Engels blijft; de texture, textures |
| exit code | exitcode | |
| error code | foutcode | |
| diagnostic | diagnose(melding) | "plan diagnostic" = plandiagnose |
| diagnostics directory | diagnostiekmap | `.omsilaunch\diagnostics` blijft code |
| timeout | timeout | Engels blijft; de timeout; "startup timeout" = opstart-timeout |
| placeholder | tijdelijke aanduiding | `<root>` enz. blijven code |
| flag | vlag | "CLI flag" = CLI-vlag |
| route | route | "hierarchical route" = hiërarchische route |
| command word | opdrachtwoord | |
| integrator | integrator | ontwikkelaar die OmsiLaunch inbouwt |
| caller | aanroeper | "caller thread" = aanroepende thread |
| known limitation | bekende beperking | |
| accepted risk | geaccepteerd risico | |
| stable beta | stabiele bèta | token `STABLE_BETA` blijft; "Beta contract" = bètacontract |
| experimental | experimenteel | token `EXPERIMENTAL` blijft |
| partial | gedeeltelijk | token `PARTIAL` blijft |
| unavailable | niet beschikbaar | token `UNAVAILABLE` blijft; niet verzachten |
| deprecated/legacy | verouderd / legacy | "legacy" blijft Engels voor oude compatibiliteitspaden |
| not normative | niet normatief | |
| source of truth | gezaghebbende bron | "the code wins" = de code is leidend |

## Aanvullende vaste vertalingen

| English | nl-NL | note |
|---|---|---|
| by design | bewust zo ontworpen | tabelcel "by design" |
| not implemented | niet geïmplementeerd | |
| lease / installation lease | lease / installatielease | Engels blijft |
| handoff (startup handoff) | handoff (opstart-handoff) | Engels blijft |
| telemetry | telemetrie | |
| session deletion | sessieverwijdering | |
| forced termination | geforceerde beëindiging | |
| cooperative shutdown | coöperatief afsluiten | |
| headless start | headless start | Engels blijft |
| read-back | terugleesactie / teruglezen | |
| owned file / session-owned | eigen bestand / bestand in bezit van de sessie | |
| fingerprint (ownership) | vingerafdruk (eigendoms-) | |
| logon session | aanmeldsessie | |
| shim | shim | Engels blijft |
