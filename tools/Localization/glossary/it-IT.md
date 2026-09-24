# Glossario it-IT — documentazione OmsiLaunch 0.1.0-beta3

Nessuna traduzione precedente it-IT esiste: questo glossario è la base terminologica unica per tutte le pagine italiane.

## Registro e forma di cortesia

- Registro: documentazione tecnica italiana professionale, sobria e precisa, in forma impersonale. Non rivolgersi mai al lettore con "tu", "voi" o "Lei": usare costruzioni impersonali o passive ("si avvia", "viene ripristinato", "è necessario") e, per le istruzioni, l'infinito ("Eseguire `session stop`", "Vedere [riferimento CLI](...)"). Descrizioni al presente indicativo.
- Qualificatori di stato e di validazione mantengono la stessa forza dell'inglese: "non validato a runtime", "solo offline", "non implementato", "non disponibile", "parziale". Mai attenuare ("potrebbe non…") né rafforzare.

## Convenzioni di punteggiatura e tipografia

- Virgolette in prosa: caporali « » per citazioni e termini evidenziati in italiano; le etichette UI inglesi restano sempre in backtick (mai tradotte, mai tra virgolette al posto dei backtick).
- Nessuno spazio prima di `:` `;` `?` `!`. Apostrofo tipografico o dritto indifferentemente, ma coerente nella pagina (preferire `'` dritto).
- Separatore decimale: nei numeri in prosa resta quello dell'originale (i valori sono letterali: `64 KiB`, `250 ms`, `2 s`, `0.1`); non convertire punti in virgole né aggiungere separatori delle migliaia.
- Titoli: maiuscola solo sulla prima parola e sui nomi propri ("Ciclo di vita della sessione", non "Ciclo Di Vita Della Sessione").
- Elenchi: iniziale maiuscola per voci che sono frasi complete; punto finale se l'originale lo ha.
- Genere dei prestiti inglesi: il runtime, il plugin, l'handle, lo snapshot, l'overlay, il backup, il thread, il timeout, la pipe (named pipe = la named pipe), il frame, l'envelope (m.), il journal, il manifest, il tooltip, il flag, il build, lo splash screen, il repaint, il mailbox → **la mailbox**, lo slot, il gate. Plurale invariato (gli handle, gli snapshot).
- Nomi di stato del prodotto (`Running`, `Failed` …), token maiuscoli (`STABLE_BETA`, `PARTIAL` …) e id di evidenza (`T01`, `BUG-05` …) invariati.

## Terminologia

| English | it-IT | note |
|---|---|---|
| session | sessione | f. |
| session owner / owner | proprietario della sessione / owner | Alla prima occorrenza "proprietario della sessione (owner)", poi "owner" o "proprietario". Il "processo owner" = processo proprietario. |
| client (mode) | client (modalità client) | Mantenere "client". |
| owner process | processo proprietario | Oppure "processo owner" se il contesto lo richiede; coerenza nella pagina. |
| launch (noun) | avvio | "launch flag" = flag di avvio; "launch plan" = piano di avvio. |
| launch (verb) | avviare | |
| plan (noun) | piano | "session plan" = piano di sessione. |
| plan (verb) | pianificare | "planned mutations" = modifiche pianificate. |
| runnable / not runnable | eseguibile / non eseguibile | Riferito al piano. "Plan: NOT RUNNABLE" resta letterale. |
| start | avvio / avviare | "startup" = avvio; "startup timeout" = timeout di avvio. |
| stop | arresto / arrestare | "stop path" = percorso di arresto. |
| end session | terminare la sessione | L'etichetta UI `End session` resta in inglese in backtick; in prosa "terminare la sessione". |
| canonical stop | arresto canonico | |
| restore | ripristino / ripristinare | "exact restore" = ripristino esatto. |
| recovery | recupero | "recover" = recuperare. Non usare "ripristino" (riservato a restore). |
| pending (journal) | (journal) in sospeso | "pending transaction" = transazione in sospeso; "left pending" = lasciata in sospeso. |
| journal | journal | Mantenere l'inglese (file `journal.json`); "journaled" = registrato nel journal. |
| transaction | transazione | |
| overlay | overlay | Mantenere; m. |
| backup | backup | Mantenere; m. |
| snapshot | snapshot | Mantenere; m. ("istantanea" solo se serve spiegare, es. finestra di stato "istantanea, non vista in tempo reale"). |
| installation | installazione | |
| installation root | radice dell'installazione | "root" nei percorsi `<root>` resta letterale. |
| package | pacchetto | |
| release package | pacchetto di rilascio | |
| manifest | manifest | Mantenere; m. |
| permanent plugin | plugin permanente | "plugin closure" = chiusura del plugin (insieme dei file del plugin). |
| native bridge | bridge nativo | |
| runtime | runtime | Mantenere; m. "at runtime" = a runtime. |
| runtime operation | operazione runtime | |
| runtime command | comando runtime | |
| runtime slot / mailbox | slot runtime / mailbox runtime | Mantenere "slot" (m.) e "mailbox" (f.). "telemetry slot" = slot di telemetria. |
| control plane | piano di controllo | "local control plane" = piano di controllo locale. |
| local control | controllo locale | |
| named pipe | named pipe | Mantenere; f. |
| frame (protocol) | frame | Mantenere; m. "control frame" = frame di controllo. |
| envelope (JSON) | envelope (JSON) | Mantenere; m. "Envelope di successo / di errore". |
| handle | handle | Mantenere; m., plurale invariato. |
| stale handle | handle obsoleto | |
| capability | capability | Mantenere (termine del prodotto: id `camera.lock` ecc.). Alla prima occorrenza si può glossare "capability (funzionalità)". |
| capability registry | registro delle capability | |
| bounded list | elenco limitato | Precisare: risultato con `returned_count` e `truncated=true`, risposta riuscita, non errore. |
| truncated | troncato | `truncated=true` resta letterale. |
| row | riga | |
| evidence | evidenza / prove | Preferire "evidenza" (evidenza di runtime); "evidence id" = id di evidenza. |
| runtime-validated | validato a runtime | Token `RUNTIME_VALIDATED` invariato. |
| statically validated | validato staticamente | Token `STATICALLY_VALIDATED` invariato. |
| offline test | test offline | "offline-validated" = validato offline; "offline only" = solo offline. |
| gate (documentation gate) | gate (gate della documentazione) | Mantenere "gate"; m. |
| tray icon | icona nell'area di notifica | "tray" da solo = area di notifica / tray; "Windows tray" = area di notifica di Windows. |
| notification area | area di notifica | Termine ufficiale Windows italiano. |
| status window | finestra di stato | |
| confirmation dialog | finestra di dialogo di conferma | |
| message box | finestra di messaggio | |
| failure dialog | finestra di dialogo di errore | |
| tooltip | tooltip | Mantenere; m. |
| context menu | menu contestuale | |
| Explorer restart | riavvio di Esplora risorse | "Explorer (the shell)" = Esplora risorse (la shell); `explorer.exe` letterale. |
| entry point | punto di ingresso | `Entry point` etichetta UI resta in inglese. |
| new map | nuova mappa | Token NEW_MAP invariato. |
| saved situation | situazione salvata | Token SAVED_SITUATION invariato. |
| map | mappa | |
| splash screen | splash screen | Mantenere; f. ("schermata iniziale" solo come glossa). |
| Internet Textures | Internet Textures (texture da Internet) | Nome della funzione di OMSI: mantenere, glossare alla prima occorrenza. |
| session profile | profilo di sessione | |
| preset | preset | Mantenere; m. |
| setting | impostazione | |
| player vehicle | veicolo del giocatore | `PlayerVehicle` in backtick o come nome di tipo resta invariato. |
| road vehicle | veicolo stradale | `RoadVehicle` invariato. |
| human (pedestrian/passenger object) | human (oggetto pedone/passeggero) | Mantenere "human" come termine OMSI (tipo `Human`); glossare. |
| timetable | orario | "timetable logs" = log dell'orario. |
| track entry | voce di tracciato (track entry) | Glossare alla prima occorrenza; poi "voce di tracciato". |
| tour entry | voce di turno (tour entry) | "tour" OMSI = turno di servizio; glossare alla prima occorrenza. |
| ticket | biglietto | |
| driver | conducente | "driver profile" = profilo del conducente. Non confondere con driver di dispositivo. |
| fleet number | numero di flotta | Etichetta UI `Fleet number` invariata. |
| registration (plate) | targa | |
| repaint | repaint (livrea) | Mantenere "repaint" (termine della community OMSI), glossare "livrea". |
| spawn | spawn / generare | Sostantivo "spawn"; verbo "generare" (es. "genera un veicolo"). |
| camera lock | blocco della telecamera | id `camera.lock` invariato. |
| device reset | reset del dispositivo | Riferito al dispositivo D3D. |
| render thread | thread di rendering | |
| texture | texture | Mantenere; f., plurale invariato. |
| exit code | codice di uscita | |
| error code | codice di errore | |
| diagnostic | diagnostica (voce di diagnostica) | "a diagnostic" = una voce di diagnostica / un messaggio diagnostico. |
| diagnostics directory | directory di diagnostica | |
| timeout | timeout | Mantenere; m. |
| placeholder | segnaposto | |
| flag | flag | Mantenere; m. |
| route | route | Mantenere "route" (f.) per le route CLI/API ("hierarchical route" = route gerarchica). |
| command word | parola di comando | |
| integrator | integratore | |
| caller | chiamante | |
| known limitation | limitazione nota | |
| accepted risk | rischio accettato | |
| stable beta | beta stabile | Token `STABLE_BETA` invariato. |
| experimental | sperimentale | Token `EXPERIMENTAL` invariato. |
| partial | parziale | Token `PARTIAL` invariato. Mai "non supportato" o "non funzionante". |
| unavailable | non disponibile | Token `UNAVAILABLE` invariato. |
| deprecated / legacy | deprecato / legacy | "legacy" mantenuto per il codice/percorsi legacy. |
| not normative | non normativo | "normative" = normativo (la documentazione inglese è normativa). |
| source of truth | fonte di riferimento | Oppure "unica fonte autorevole"; coerenza nella pagina. |

## Termini aggiuntivi ricorrenti

| English | it-IT | note |
|---|---|---|
| lease / installation lease | lease / lease dell'installazione | Mantenere "lease" (m.); glossare "blocco esclusivo" alla prima occorrenza. |
| handoff | handoff | Mantenere; m. ("startup handoff" = handoff di avvio). |
| telemetry | telemetria | |
| supervisor | supervisore | |
| forced termination | terminazione forzata | |
| host | host | Mantenere. |
| shim | shim | Mantenere. |
| capture (documentation capture) | acquisizione | |
| read-back | rilettura | |
| by design | per scelta progettuale | Nelle colonne di stabilità. |
| behaviour | comportamento | |
