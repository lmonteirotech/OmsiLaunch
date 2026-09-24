# Glossaire fr-FR — documentation OmsiLaunch 0.1.0-beta3

Ce glossaire s'impose à tous les traducteurs fr-FR. En cas de doute, la page anglaise et le code font foi ; ce glossaire ne remplace jamais les règles de `BRIEF.md` (blocs de code, spans inline, liens, jetons littéraux inchangés).

## Registre et forme d'adresse

- Documentation technique professionnelle en français de France. On s'adresse au lecteur par « vous » (jamais « tu ») ; privilégier les tournures impersonnelles ou l'infinitif dans les consignes (« Exécutez… », « Il faut… »). Ton neutre, précis, sans familiarité ni marketing.
- Temps : présent de l'indicatif pour décrire le comportement du produit (« OmsiLaunch restaure… »). Ne pas ajouter de conditionnel ni d'atténuation absents de l'anglais, et inversement.
- Les qualificatifs de statut gardent leur force exacte : « non implémenté », « non validé à l'exécution », « hors ligne uniquement », « indisponible », « partiel ». Ne jamais rendre `PARTIAL` par « non pris en charge » ni RV-002 par « corrigé » ou « défaut ».

## Typographie et ponctuation

- **Espace avant la ponctuation double** : une espace (espace normale U+0020, comme dans les chaînes du produit, par ex. `Terminer la session ?`) avant `:` `;` `?` `!` dans la prose française. Appliquer systématiquement. Jamais d'espace ajoutée à l'intérieur d'un span de code, d'un bloc de code, d'une URL, d'une ancre ou d'un jeton comme `/key:value`, `OL_E_...`, `T01`.
- **Guillemets** : guillemets français « … » avec une espace intérieure pour citer de la prose. Un libellé d'interface anglais cité hors backticks dans l'anglais (par ex. tray "End session") s'écrit « End session » (libellé anglais conservé), éventuellement suivi de la chaîne produit française entre parenthèses et backticks : « End session » (`Terminer la session`).
- **Apostrophe** : apostrophe droite `'` (comme dans les chaînes du produit : `Point d'entrée`), de façon cohérente dans toute la page.
- **Majuscules** : titres et en-têtes de tableau en casse de phrase (seule la première lettre en majuscule : « Cycle de vie de la session », « Limitations connues »). Les noms propres (OMSI, OmsiLaunch, Windows, Steam, Direct3D, Explorer) gardent leur casse.
- **Nombres et unités** : laisser tels quels les nombres, versions, unités et jetons (`64 KiB`, `250 ms`, `2 s`, `4 s`, `0.1.0-beta3`), même hors backticks. Pas de conversion en « Kio ».
- **Listes** : conserver la ponctuation de fin de l'anglais (point final si l'anglais en a un). Pas de majuscule forcée après deux-points.
- **Tirets et flèches** : conserver `→`, `->`, `–` tels qu'ils apparaissent dans l'anglais.
- Pas de note du traducteur, pas de bandeau, pas de traduction entre crochets.

## Terminologie

| English | fr-FR | note |
| --- | --- | --- |
| session | session (f.) | |
| session owner / owner | propriétaire de la session / propriétaire (m.) | « owner mode » = mode propriétaire. Ne pas laisser « owner » en anglais dans la prose. |
| client (mode) | client / mode client | |
| owner process | processus propriétaire | |
| launch (noun) | lancement | verbe : lancer. « launch flags » = options de lancement. |
| plan (noun) | plan (m.) | « a runnable plan » = un plan exécutable. |
| plan (verb) | planifier | « planning » = planification ; « re-plan » = replanifier / nouvelle planification. |
| runnable / not runnable | exécutable / non exécutable | Pour un plan. « makes the plan not runnable » = rend le plan non exécutable. |
| start (noun / verb) | démarrage / démarrer | « startup » = démarrage ; « startup timeout » = timeout de démarrage. |
| stop (noun / verb) | arrêt / arrêter | « stop request » = demande d'arrêt. |
| end session | terminer la session | Aligné sur la chaîne produit `Terminer la session`. « session end » = fin de session. |
| canonical stop | arrêt canonique | « the same canonical owner stop path » = le même chemin d'arrêt canonique du propriétaire. |
| restore (noun / verb) | restauration / restaurer | |
| recovery | récupération | « recover » = récupérer ; « crash recovery » = récupération après plantage. Ne pas utiliser « reprise ». |
| pending (journal) | (journal) en attente | « left pending » = laissé en attente. |
| journal | journal (m.) | « journaled » = journalisé. |
| transaction | transaction (f.) | |
| overlay | overlay (m.) | Anglais conservé (terme courant). Ne pas traduire par « superposition ». Pluriel : overlays. |
| backup | sauvegarde (f.) | « backup directory » = répertoire de sauvegarde ; « backed up » = sauvegardé. |
| snapshot | instantané (m.) | « a snapshot, not a live view » = un instantané, pas une vue en direct. |
| installation | installation (f.) | |
| installation root | racine de l'installation | « OMSI root » = racine OMSI. « installation lease » = bail d'installation. |
| lease | bail (m.) | Verrou d'installation (`Local\` sémaphore). « releases the lease » = libère le bail. |
| package | paquet (m.) | « portable package » = paquet portable. |
| release package | paquet de version | |
| manifest | manifeste (m.) | Le nom de fichier `release-manifest.json` reste inchangé. |
| permanent plugin | plugin permanent | « plugin » reste en anglais. « plugin closure » = ensemble de fichiers du plugin (closure du plugin) ; utiliser « closure du plugin » de façon cohérente. |
| native bridge | pont natif | |
| runtime | runtime (m.) | Anglais conservé. « .NET runtime » = runtime .NET. |
| runtime operation | opération runtime | |
| runtime command | commande runtime | « runtime command channel » = canal de commandes runtime. |
| runtime slot / mailbox | slot runtime / boîte aux lettres runtime | « slot » conservé ; « mailbox » traduit par « boîte aux lettres ». « telemetry slot » = slot de télémétrie ; « latest-value slot » = slot à dernière valeur. |
| control plane | plan de contrôle | « local control plane » = plan de contrôle local. |
| local control | contrôle local | « control endpoint » = point de terminaison de contrôle. |
| named pipe | named pipe / canal nommé | Première occurrence : « canal nommé (named pipe) », ensuite « pipe » ou « canal nommé ». |
| frame (protocol) | trame (f.) | |
| envelope (JSON) | enveloppe (JSON) (f.) | |
| handle | handle (m.) | Anglais conservé. Ne pas traduire par « poignée ». « process handle » = handle de processus. |
| stale handle | handle obsolète | |
| capability | capacité (f.) | « capability id » = identifiant de capacité. |
| capability registry | registre des capacités | |
| bounded list | liste bornée | « bounded list result » = résultat de liste bornée. |
| truncated | tronqué(e) | `truncated=true` reste littéral. « the largest prefix that fits » = le plus grand préfixe qui tient. |
| row | ligne (f.) | Lignes d'une liste ou d'un tableau. |
| evidence | preuve(s) (f.) | « runtime evidence » = preuves d'exécution ; « evidence id » = identifiant de preuve. |
| runtime-validated | validé à l'exécution | « not runtime validated » = non validé à l'exécution. |
| statically validated | validé statiquement | |
| offline test | test hors ligne | « covered offline » = couvert hors ligne. |
| gate (documentation gate) | contrôle bloquant (de documentation) | Gate de validation qui bloque la publication. |
| tray icon | icône de la zone de notification | Forme courte admise : icône de notification. « tray » seul = zone de notification. |
| notification area | zone de notification | Terme Windows officiel. |
| status window | fenêtre d'état | Aligné sur `État` (produit). |
| confirmation dialog | boîte de dialogue de confirmation | |
| message box | boîte de message | |
| failure dialog | boîte de dialogue d'échec | |
| tooltip | info-bulle (f.) | |
| context menu | menu contextuel | |
| Explorer restart | redémarrage de l'Explorateur | Explorateur Windows (explorer.exe). |
| entry point | point d'entrée | Aligné sur `Point d'entrée` (produit). « entrypoint index » = index du point d'entrée. |
| new map | nouvelle carte | Mode NEW_MAP ; la chaîne produit du mode est `Nouvelle session`. |
| saved situation | situation enregistrée | Aligné sur `Situation enregistrée` (produit). |
| map | carte (f.) | Aligné sur `Carte` (produit). |
| splash screen | écran de démarrage | Aligné sur `Écran de démarrage` (produit). « splash » seul : écran de démarrage. |
| Internet Textures | textures Internet | Aligné sur `Textures Internet` (produit). |
| session profile | profil de session | |
| preset | préréglage (m.) | Aligné sur `Préréglage` (produit). « weather preset » = préréglage météo. |
| setting | paramètre (m.) | « semantic setting » = paramètre sémantique. |
| player vehicle | véhicule du joueur | `PlayerVehicle` reste littéral quand il est écrit ainsi. |
| road vehicle | véhicule routier | `RoadVehicle` littéral inchangé. |
| human (pedestrian/passenger object) | humain (objet piéton/passager) | `Human` littéral inchangé. |
| timetable | horaire (m.) / grille horaire | « timetable entries » = entrées d'horaire. |
| track entry | entrée de trajet | |
| tour entry | entrée de service | « tour » = service (tournée de conduite). |
| ticket | billet (m.) | |
| driver | conducteur (m.) | « driver profile » = profil de conducteur. |
| fleet number | numéro de flotte | Aligné sur `Numéro de flotte` (produit). |
| registration (plate) | immatriculation (f.) | Aligné sur `Immatriculation` (produit). |
| repaint | livrée (f.) | Aligné sur `Livrée` (produit). |
| spawn | faire apparaître / apparition | « spawned vehicles » = véhicules créés (apparus) en cours de session. |
| camera lock | verrouillage de la caméra | |
| device reset | réinitialisation du périphérique (D3D) | « device lost » = périphérique perdu. |
| render thread | thread de rendu | « thread » conservé. |
| texture | texture (f.) | |
| exit code | code de sortie | |
| error code | code d'erreur | |
| diagnostic | diagnostic (m.) | Pluriel : diagnostics. |
| diagnostics directory | répertoire de diagnostics | |
| timeout | timeout (m.) | Anglais conservé ; « délai d'expiration » admis une fois pour expliquer. « expiry » = expiration. |
| placeholder | espace réservé | Dans les exemples : valeur d'exemple / espace réservé. |
| flag | option (f.) | Option de ligne de commande (`/key:value`). |
| route | route (f.) | « hierarchical route » = route hiérarchique. |
| command word | mot de commande | |
| integrator | intégrateur (m.) | |
| caller | appelant (m.) | |
| known limitation | limitation connue | |
| accepted risk | risque accepté | |
| stable beta | bêta stable | Le jeton `STABLE_BETA` reste inchangé. |
| experimental | expérimental | Le jeton `EXPERIMENTAL` reste inchangé. |
| partial | partiel | Le jeton `PARTIAL` reste inchangé. |
| unavailable | indisponible | Le jeton `UNAVAILABLE` reste inchangé. Ne pas affaiblir en « limité ». |
| deprecated / legacy | obsolète (déprécié) / historique (hérité) | « legacy pages » = pages historiques ; « legacy journal » = journal hérité. |
| not normative | non normatif | « normative » = normatif. |
| source of truth | source de référence | « authoritative » = qui fait foi. |
| host trace | trace de l'hôte | |
| supervisor | superviseur (m.) | |
| handoff | handoff (m.) | Anglais conservé (transfert de démarrage) ; « startup handoff » = handoff de démarrage. |
| forced termination | arrêt forcé | |
| cooperative shutdown | arrêt coopératif | |
| fingerprint | empreinte (f.) | « ownership fingerprint » = empreinte de propriété. |
| hash | hash (m.) / hacher | Anglais conservé pour le nom ; verbe « calculer le hash ». |
| same-user trust model | modèle de confiance « même utilisateur » | |

## Product UI strings (exact)

Chaînes françaises du produit, reprises exactement de `tools/OmsiLaunch.Cli/WindowsUiStrings.cs` (tableau `French`, utilisé pour `fr` et `fr-FR`). Dans les pages, le libellé anglais reste le span de code ; la chaîne française peut être ajoutée entre parenthèses et backticks, exactement comme ci-dessous. N'inventez jamais d'autre libellé.

| Key | English | fr-FR (product) |
| --- | --- | --- |
| `Tray.Running` | `OmsiLaunch is running` | `OmsiLaunch est en cours d'exécution` |
| `Tray.Status` | `Status` | `État` |
| `Tray.EndSession` | `End session` | `Terminer la session` |
| `Tray.EndSessionDescription` | `Ends OMSI 2 and the OmsiLaunch session` | `Ferme OMSI 2 et la session OmsiLaunch` |
| `Status.Title` | `OmsiLaunch session status` | `État de la session OmsiLaunch` |
| `Status.SessionRunning` | `Session is running` | `La session est en cours` |
| `Status.Section.Session` | `Session` | `Session` |
| `Status.Section.Profile` | `Session profile` | `Profil de session` |
| `Status.Section.Environment` | `Environment` | `Environnement` |
| `Status.Section.Vehicle` | `Vehicle` | `Véhicule` |
| `Status.Section.Configuration` | `Configuration` | `Configuration` |
| `Status.Section.Presentation` | `Presentation` | `Présentation` |
| `Status.Field.Mode` | `Mode` | `Mode` |
| `Status.Field.Map` | `Map` | `Carte` |
| `Status.Field.EntryPoint` | `Entry point` | `Point d'entrée` |
| `Status.Field.Situation` | `Situation` | `Situation` |
| `Status.Field.SessionProfile` | `Profile` | `Profil` |
| `Status.Field.Preset` | `Preset` | `Préréglage` |
| `Status.Field.Date` | `Date` | `Date` |
| `Status.Field.Time` | `Time` | `Heure` |
| `Status.Field.Weather` | `Weather` | `Météo` |
| `Status.Field.Vehicle` | `Vehicle` | `Véhicule` |
| `Status.Field.Repaint` | `Repaint` | `Livrée` |
| `Status.Field.Hof` | `HOF` | `HOF` |
| `Status.Field.Fleet` | `Fleet number` | `Numéro de flotte` |
| `Status.Field.Registration` | `Registration` | `Immatriculation` |
| `Status.Field.Splash` | `Splash` | `Écran de démarrage` |
| `Status.Field.InternetTextures` | `Internet textures` | `Textures Internet` |
| `Status.Close` | `Close` | `Fermer` |
| `Mode.NewMap` | `New session` | `Nouvelle session` |
| `Mode.SavedSituation` | `Saved situation` | `Situation enregistrée` |
| `Mode.LastMapState` | `Last map state` | `Dernier état de la carte` |
| `Presentation.Managed` | `Managed` | `Gérée` |
| `Presentation.Unset` | `Original OMSI` | `OMSI d'origine` |
| `InternetTextures.Native` | `Original OMSI` | `OMSI d'origine` |
| `InternetTextures.Disabled` | `Disabled` | `Désactivées` |
| `InternetTextures.Override` | `Override` | `Remplacement` |
| `Stop.Title` | `End session?` | `Terminer la session ?` |
| `Stop.Message` | `OMSI 2 will be closed and the OmsiLaunch managed session will end.` | `OMSI 2 sera fermé et la session gérée par OmsiLaunch prendra fin.` |
| `Stop.Confirm` | `End session` | `Terminer la session` |
| `Stop.Cancel` | `Cancel` | `Annuler` |
| `Stop.Failed` | `The session could not be ended. OMSI and its managed session remain active.` | `La session n'a pas pu être terminée. OMSI et sa session gérée restent actifs.` |

Remarque : ces chaînes suivent la langue de l'interface Windows (`fr`, `fr-FR`), jamais la langue d'OMSI ni celle du profil de session. Les autres langues Windows (hors en, pt-BR, de, fr, pl) affichent les chaînes anglaises.
