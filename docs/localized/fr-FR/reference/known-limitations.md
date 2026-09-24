# Limitations connues

<!-- l10n: source=reference/known-limitations.md -->
> Traduction de la [page originale en anglais](../../../reference/known-limitations.md) d’OmsiLaunch 0.1.0-beta3. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page recense, à partir du code, tout ce qui, dans OmsiLaunch 0.1.0-beta3, est `UNAVAILABLE`, `PARTIAL` ou constitue un risque accepté, afin que les utilisateurs et les intégrateurs ne s'appuient pas sur un comportement que le produit ne fournit pas. Chaque ligne nomme la limitation, sa stabilité, sa raison d'être et l'endroit où elle est documentée en détail. La documentation anglaise est normative ; les copies localisées sous `docs/localized/` ne sont pas maintenues au même niveau et peuvent être en retard (voir la dernière section).

<a id="compatibility"></a>
## Compatibilité

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| Seul `Omsi23004_692EBFBF` est pris en charge (`692EBFBF...6243`) ; le hash Steam LAA `7DAB063D...D759` figure dans la liste autorisée ; son empreinte et son plan ont été validés avec une copie contrôlée, mais la phase de jeu nécessite une véritable installation Steam (l'image est l'exécutable Steam protégé par DRM) | `PARTIAL` pour Steam LAA | [compatibilité](compatibility.md) |
| Windows 10+ x64 uniquement ; pas de Windows 7/8, pas de XP, pas d'ARM64 | `UNAVAILABLE` | [compatibilité](compatibility.md) |
| Le plugin nécessite le runtime .NET 6 **x86** en plus du runtime x64 utilisé par le contrôleur | | [compatibilité](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Démarrage du monde et options de lancement

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last`, `WorldMode.LastMapState`) n'est pas implémenté ; aucun repli sur un `.osn` choisi d'après l'horodatage n'est jamais substitué | `UNAVAILABLE` (BI-006) | Diagnostic de plan `OL_E_CAPABILITY_UNAVAILABLE` |
| La date, l'heure et l'année explicites ou système (`/date`, `/time`, `/year`, `new.date`/`new.time`/`new.year` du profil, `DateSpec`/`TimeSpec`/`YearSpec`) sont transportées dans la spécification mais rendent le plan non exécutable ; le plugin rejette les modes autres que `Unset` | `UNAVAILABLE` (`STATICALLY_PARTIAL`) | [profils de session](session-profiles.md), [launchspec](launchspec.md) |
| Préréglage météo, ICAO et météo réelle actuelle au démarrage (`/weather*`, `new.weather`) | `UNAVAILABLE` (`STATICALLY_PARTIAL`, BI-003) | idem |
| Le modèle, la livrée, le HOF, le numéro de flotte et l'immatriculation du véhicule du joueur au démarrage (famille `/vehicle`, `PlayerVehicleSpec`) sont résolus par rapport au catalogue de contenu mais non appliqués ; les demander rend le plan non exécutable ; l'attribution headless déterministe du PlayerVehicle est une extension future | `UNAVAILABLE` (BI-007) | `player.assign-headless` dans [capacités](capabilities.md) |
| Le point d'entrée par identité (`/entrypoint:<identity>`) n'est pas mis en correspondance avec la liste présentée par OMSI ; utilisez `/entrypoint-index` | `PARTIAL` (BI-001) | capacité de plan `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| Les overlays de documents clavier et manette (`InputSpec`, `Environment.Keyboard`, `Environment.Controllers`) sont analysés mais jamais appliqués par une session | `UNAVAILABLE` (BI-005) | Capacités `input.*` |
| `LaunchBehaviorSpec.RestoreConfiguration` et `InstallationSpec.ExpectedExecutableSha256` sont déclarés mais jamais lus | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds` (`/shutdown-timeout`, `shutdown-timeout` du profil) est accepté et transporté mais n'est pas utilisé par le superviseur | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [cycle de vie de la session](../concepts/session-lifecycle.md) |
| `/quiet` et `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| Les options de diagnostic (`/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native`) alimentent `DiagnosticsSpec` ; l'effet visible se limite à la trace de l'hôte sous `.omsilaunch\diagnostics` | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` sont des harnais de validation | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fin de session et contrôle des processus

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| L'arrêt de session est un arrêt forcé : `session.stop`, « End session » (`Terminer la session`) dans la zone de notification, Ctrl+C et `CloseAsync` aboutissent tous à `TerminateProcess`. La routine d'arrêt d'OMSI ne s'exécute pas, OMSI ne réécrit ni `options.cfg` ni ses journaux à la sortie, et tout état OMSI non enregistré est perdu. C'est délibéré : cela empêche OMSI d'écraser les fichiers restaurés. | par conception | [cycle de vie de la session](../concepts/session-lifecycle.md) |
| L'arrêt coopératif par WM_CLOSE avec un timeout et un repli sur la terminaison n'est pas implémenté | `UNAVAILABLE` (décision produit, S-11 ; OMSI a ignoré `WM_CLOSE` envoyé à sa fenêtre principale lors de la campagne de clôture runtime) | [état de la validation à l'exécution](../status/runtime-validation-status.md) |
| À la fermeture de la console ou à la déconnexion, le propriétaire dispose d'un budget de 4 s pour arrêter et restaurer ; ce qui reste est récupéré grâce au journal au démarrage suivant | fermeture de la console validée à l'exécution ; déconnexion non testée | [transactions et récupération](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transaction, récupération et bail

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| Le bail d'installation est un sémaphore `Local\` : un propriétaire par installation **par session de connexion Windows** ; non appliqué entre utilisateurs ; non libéré tant qu'un autre processus détient un handle ; tout processus du même utilisateur peut détenir le nom | risque accepté (S-18) | [transactions et récupération](../concepts/transactions-and-recovery.md) |
| La récupération est refusée (`OL_E_INSTALLATION_BUSY`) tant que le processus OMSI journalisé ou, pour un journal sans PID, tout `Omsi.exe` lancé depuis cette racine est en cours d'exécution | par conception | idem |
| Un chemin d'overlay initialement absent dont le contenu a changé pendant la session bloque la restauration (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) jusqu'à inspection | par conception | idem |
| Les journaux antérieurs aux empreintes de propriété ne peuvent être clos que par une session aux octets planifiés identiques (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | idem |
| Seuls les chemins appartenant à la session sont restaurés. Les écritures propres d'OMSI pendant une session (`options.cfg` `[last_map]` lorsqu'aucun paramètre ne superpose d'overlay à `options.cfg`, `Texture\standard.ipr`, caches, `laststn.osn`, profil de conducteur, journaux) persistent, comme après un démarrage direct d'OMSI | par conception | [transactions et récupération](../concepts/transactions-and-recovery.md) |
| La suppression d'un `closecheck` obsolète avant une session est définitive (enregistrée, non restaurée) lorsque `SuppressStaleClosecheckWarning` vaut true | par conception | idem |

<a id="runtime-control"></a>
## Contrôle runtime

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| `weather.set` est rejeté (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) : OMSI écrase les deux candidats de vent profilés à son tick météo suivant | `UNAVAILABLE` | [capacités](capabilities.md) |
| Écritures du calendrier (`SetActualDateTime`) | `UNAVAILABLE` (BI-002) | `calendar.set-actual-date-time` |
| Écritures de variables chaîne, déclencheurs nommés, déclencheurs sonores (propriété des chaînes gérées Delphi) | `UNAVAILABLE` (BI-004) | `scripts.string.read` est en lecture seule |
| Pas de relocalisation de véhicule, pas de réassociation spatiale entre tuiles, pas d'autorité de transformation sûre vis-à-vis d'ODE ; les champs de position sont en lecture seule | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` nécessitent un PlayerVehicle ; le démarrage headless NEW_MAP n'en a pas (une situation enregistrée en fournit un) | par conception (BI-007) | RV-004 |
| Les mutations runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, textures D3D) ne sont ni journalisées ni restaurées | par conception | [contrôle runtime](runtime-control.md) |
| Angle mort de l'empreinte des handles : un objet de même classe et de même définition recréé à la même adresse entre deux lectures de liste n'est pas détecté comme obsolète ; la durée de vie en cas de suppression naturelle (RV-002) n'a pas de producteur runtime sûr et reste hors ligne | `PARTIAL` | [contrôle runtime](runtime-control.md) |
| Les résultats sont bornés par la boîte aux lettres de 64 KiB : les longues listes sont tronquées (`truncated=true`) ; les données de pixels sont limitées à 48 KiB par `d3d.texture.update` | par conception | [capacités](capabilities.md) |
| Canal à requête unique : une requête à la fois par session ; un slot occupé donne `OL_E_RUNTIME_CHANNEL_BUSY` ; les identifiants de requête ne doivent pas être réutilisés | par conception | [contrôle runtime](runtime-control.md) |
| La télémétrie est un slot à dernière valeur : des rafales plus rapides que l'échantillonnage de 100 ms de l'hôte peuvent perdre des événements intermédiaires (les numéros de séquence maintiennent distincts les événements consécutifs identiques ; les échantillons déchirés sont ignorés) | `PARTIAL` | [plugin permanent](../concepts/permanent-plugin.md) |
| La réinitialisation du périphérique D3D a été observée à l'exécution (`resetting`, `restored`, invalidation de génération) ; aucune transition `lost` distincte n'a été produite, car le périphérique d'OMSI est passé directement à `DEVICENOTRESET` | `PARTIAL` (RV-007) | [état de la validation à l'exécution](../status/runtime-validation-status.md) |
| Les résultats de liste bornée (les opérations `timetable.*.list`, `vehicle.variables.list`, `vehicle.string-variables.list`) renvoient au plus les lignes qui tiennent dans le slot runtime de 64 KiB ; les autres sont omises avec `truncated=true` et un `returned_count` plus petit (audit de documentation BUG-05). Il n'y a pas de pagination dans cette version | par conception | [capacités](capabilities.md) |
| `timetable.logs.read`, `road-vehicles.list`, `humans.list`, `vehicle.constants.list` et `vehicle.curves.list` ne sont pas bornés : un résultat plus grand que le slot échoue avec `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (observé pour aucun d'entre eux sur les cartes testées) | `PARTIAL` | [capacités](capabilities.md) |
| Les chaînes de preuves auto-déclarées (`RuntimeValidation` de `PublicCapabilityRegistry`, `EvidenceState` de `GetCapabilitiesAsync`) n'ont pas été mises à jour après la campagne de clôture runtime : `camera.lock` indique toujours `STATICALLY_VALIDATED` et `runtime.d3d.lifecycle.reset` `IMPLEMENTED_NOT_RUNTIME_VALIDATED`. La page [état de la validation à l'exécution](../status/runtime-validation-status.md) fait foi | retard de documentation, pas une différence de comportement | [capacités](capabilities.md) |
| Certains champs avancés du graphe de carte/tuiles/chemins/objets ne sont pas exposés ; les lecteurs runtime sont des instantanés typés conditionnés par le profil, jamais un accès mémoire arbitraire | par conception | [capacités](capabilities.md) |
| Les lectures mémoire dans le processus suivent le schéma vérification puis utilisation sur un OMSI actif ; une mutation concurrente d'OMSI entre la vérification et la lecture peut produire un instantané incohérent (`OL_E_RUNTIME_OPERATION_FAILED`) | risque accepté (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Plan de contrôle local et modèle de confiance

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| Modèle de confiance « même utilisateur » : le canal nommé (named pipe, `CurrentUserOnly`), les mappages mémoire de handoff, de télémétrie et runtime, ainsi que le sémaphore du bail sont accessibles à tout processus du même utilisateur Windows. Un tel processus peut lire l'état, arrêter la session ou exécuter des opérations runtime dès qu'il a lu le `session_id`. | risque accepté (S-06, S-30) | [contrôle local](local-control.md) |
| Le point de terminaison de contrôle n'existe que tant que le propriétaire est `Running` ; un client obtient `OL_E_NO_ACTIVE_SESSION` (sortie 4) pendant le démarrage et après la fin de la session | par conception | [contrôle local](local-control.md) |
| Si un autre processus possède déjà le nom du pipe, le propriétaire continue de s'exécuter sans point de terminaison (`ListenFault`), et un second lancement peut signaler à tort `OL_E_SESSION_ALREADY_ACTIVE` | risque accepté | [contrôle local](local-control.md) |
| `.omsilaunch\` hérite de l'ACL de la racine OMSI ; aucun contrôle d'accès explicite n'est appliqué | risque accepté (S-31) | [transactions et récupération](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnostics et sortie

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| Les diagnostics sont uniquement des fichiers locaux (`.omsilaunch\diagnostics`) ; rien n'est téléversé et il n'existe aucun signalement à distance | par conception | [transactions et récupération](../concepts/transactions-and-recovery.md) |
| La rétention conserve les 50 sessions les plus récentes ; les diagnostics plus anciens préfixés par session sont supprimés au démarrage d'une nouvelle session | par conception | idem |
| La sortie JSON et les diagnostics incluent les chemins d'installation (`RootPath`, répertoires de ressources, chemins `.itx`) | par conception (données locales) | |
| La boîte de dialogue d'échec de session de `OmsiLaunchW.exe` affiche comme message la charge utile d'échec du plugin (par exemple `{"name":"world.failed",...}`) plutôt qu'une phrase ; la ligne `Code:` est correcte | cosmétique | [zone de notification Windows](windows-tray.md) |
| Une requête D3D rejetée par le pont natif avant tout appel Direct3D signale `native_status` correctement, mais son texte `detail` indique `HRESULT 0x00000000` | cosmétique | [capacités](capabilities.md) |
| La fenêtre d'état de la zone de notification est un instantané de la session planifiée pris à son ouverture ; elle ne se rafraîchit pas et n'affiche aucune valeur OMSI en direct | par conception | [zone de notification Windows](windows-tray.md) |

## Documentation

Les pages anglaises sous `docs/` constituent la documentation normative de cette version. `docs/localized/<locale>/` contient des traductions des mêmes pages `0.1.0-beta3` (voir [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)) ; lorsqu'une traduction diffère du texte anglais, le texte anglais et le code font foi. Les pages historiques et héritées qui y sont listées ne sont disponibles qu'en anglais.

Voir aussi : [capacités](capabilities.md), [état de la validation à l'exécution](../status/runtime-validation-status.md), [erreurs](errors.md).
