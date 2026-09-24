# Capacités

<!-- l10n: source=reference/capabilities.md -->
> Traduction de la [page originale en anglais](../../../reference/capabilities.md) d’OmsiLaunch 0.1.0-beta3. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page constitue l'inventaire canonique de ce que OmsiLaunch 0.1.0-beta3 sait faire. Il est dérivé de `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), de l'implémentation runtime dans le plugin (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` et `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`) et de `OmsiLaunchService.GetCapabilitiesAsync`. Pour chaque capacité, il indique la classification, le niveau de stabilité employé dans toute cette documentation, si une session `Running` est requise, si la capacité modifie OMSI, les arguments et les clés de résultat, les erreurs et les preuves de validation à l'exécution. La manière d'invoquer une opération (routes CLI, enveloppe de l'API, timeouts, handles) est décrite dans [contrôle runtime](runtime-control.md) ; le tableau des preuves se trouve dans [état de la validation à l'exécution](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classification et stabilité

`PublicCapabilityClassification` possède quatre valeurs. Elles correspondent au vocabulaire de stabilité comme suit, avec des rétrogradations lorsque les preuves sont incomplètes :

| Classification | Signification | Stabilité |
| --- | --- | --- |
| `PublicStableBeta` | Publique, dans le contrat de la bêta, validée à l'exécution | `STABLE_BETA` (rétrogradée en `PARTIAL` là où c'est indiqué) |
| `PublicExperimental` | Publique, susceptible de changer, validée à l'exécution ou validée statiquement | `EXPERIMENTAL` (rétrogradée en `PARTIAL` là où c'est indiqué) |
| `InternalOnly` | Primitive de recherche ; inaccessible par l'API publique ou la CLI | `INTERNAL` |
| `Unsupported` | Reconnue à des fins de diagnostic, rejetée ou absente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` et `Event`. Chaque capacité runtime exige une session `Running` et le profil exact `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` dans le registre) ; les capacités de session qui créent une session exigent le profil exact mais aucune session.

Les résultats runtime sont des dictionnaires de chaînes. Les clés qui commencent par `internal_` ou se terminent par `_address`, `_pointer` ou `_vmt` sont supprimées à la frontière de l'API (`ScrubInternalValues`) et ne sont pas listées ici. Les clés de résultat des tableaux d'opérations ci-dessous sont les ensembles de clés exacts renvoyés par le produit : elles ont été capturées en exécutant chaque opération publique dans une session réelle (capture de documentation de la clôture runtime, session `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn` ; les lignes de liste sont notées `<row>.<n>.<field>`).

Manière dont l'échec d'une opération est signalé (`CurrentRuntimeControl.Execute`) :

| Échec | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rejet par le registre (opération inconnue ou `internal.*`, argument requis manquant) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | aucune |
| Rejet délibéré par le produit (`weather.set`) | le code spécifique (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (phrase) |
| Échec D3D | le code `OL_E_D3D_*` spécifique | `detail`, `native_status` |
| Tout autre échec côté plugin (handle obsolète, nom inconnu, valeur hors plage, aucun véhicule du joueur, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (commence par le code spécifique, par exemple `OL_E_RUNTIME_OBJECT_HANDLE_STALE` ou `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (nom de type .NET) |
| Résultat qui ne tient pas dans la boîte aux lettres de 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (les listes bornées sont raccourcies à la place, voir [Horaires, conducteurs, billets](#timetable-drivers-tickets)) | aucune |

La colonne `Errors` de chaque tableau nomme les codes spécifiques ; lisez-les dans `ErrorCode` ou dans le premier jeton de `Values.detail`, comme ci-dessus.

<a id="session-capabilities"></a>
## Capacités de session

| Capacité | Classification | Stabilité | Type | Route API | Route CLI | Modifie | Validation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | non | RUNTIME_PASS (chaque session validée commence par un plan) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | options de lancement | système de fichiers (transaction), processus | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | non | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, zone de notification, Ctrl+C | processus (`TerminateProcess`), système de fichiers (restauration) | RUNTIME_PASS ; arrêt coopératif non implémenté |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | système de fichiers (restauration) | RUNTIME_PASS : récupération après sortie précoce (RV-008), propriétaire tué puis récupération précoce au démarrage suivant (`S05`), restauration échouée puis `/recover` (`F01`), refus sous le bail, avec un OMSI orphelin et dans la fenêtre précédant le PID (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, contrôle `session.events` | `events read`, `events watch` | non | RUNTIME_PASS ; le slot de télémétrie à dernière valeur peut perdre des rafales |

<a id="runtime-capabilities-registry-entries"></a>
## Capacités runtime (entrées du registre)

| Capacité | Classification | Stabilité | Type | Opérations | Handles | Validation |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (session `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (écriture, `SetTime`, relecture) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED : toujours `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (session `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS : revalidée sur le slot de carte corrigé (session `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) et lue sur Berlin-Spandau lors de la capture de documentation |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (écriture du FOV et relecture) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS avec un PlayerVehicle issu d'une situation enregistrée (clôture runtime `CAM01`, familles 0, 2 et 1 avec relecture, puis déverrouillage). La chaîne `RuntimeValidation` du registre lui-même indique toujours `STATICALLY_VALIDATED` (auto-déclaration du produit, non mise à jour dans cette version) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS ; la détection des handles obsolètes après suppression naturelle (RV-002) n'a pas de producteur runtime sûr et est validée hors ligne |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (session `5f641c8d`, `2 -> 3`, handle `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (collection `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS : `present=false` lors d'un démarrage headless, l'instantané complet avec une situation enregistrée |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` et la famille `timetable.*.list` / `timetable.logs.read` | | RUNTIME_PASS (entrées de trajet : 91 enregistrements sur Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (lecture seule ; écritures BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS pour create/describe/update/release, rejet des handles libérés et obsolètes à travers les recréations et les sessions (`H02`), et réinitialisation du périphérique avec invalidation de génération (RV-007, `D01` ; l'état `lost` lui-même n'a pas été produit) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | Non publique. `ExecuteRuntimeAsync` la rejette avec `OL_E_RUNTIME_OPERATION_UNKNOWN` avant toute recherche de session ; la CLI n'a aucune route pour elle. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | aucune | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | aucune | | UNSUPPORTED (BI-007) |

`internal.road-vehicles.make-basic` est `INTERNAL` : elle existe dans le plugin à des fins de recherche (elle renvoie une adresse native) et elle est rejetée à la frontière de l'API ainsi que par le plan de contrôle local, qui valident tous deux d'abord le nom de l'opération par rapport à `PublicRuntimeOperationIds`.

<a id="public-runtime-operations"></a>
## Opérations runtime publiques

Voici la liste exacte des identifiants d'opération qu'un frontend public peut transmettre (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Chacune exige `SessionState.Running` ; aucune n'est journalisée ni restaurée. Les arguments requis sont contrôlés par le registre avant l'utilisation de la boîte aux lettres (`OL_E_RUNTIME_ARGUMENT_REQUIRED`) ; les arguments facultatifs sont validés par le plugin. Codes d'échec communs à toutes les opérations : `OL_E_RUNTIME_OPERATION_UNKNOWN` (absente de cette liste), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (le plugin n'a pas d'implémentation), `OL_E_RUNTIME_OPERATION_FAILED` (exception inattendue dans le processus ; valeurs `detail` et `exception`).

<a id="time"></a>
### Heure

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `time.read` | Read | aucun | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | `hour` (0..23), `minute` (0..59), `second` (0..59.999, décimal) facultatifs ; au moins un | les clés de `time.read` après le `SetTime` profilé | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Météo

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | aucun | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | quelconques | aucune | Toujours `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (arguments vides : `OL_E_RUNTIME_ARGUMENT_REQUIRED`). OMSI écrase les deux candidats de vent profilés lors de son prochain tick météo. |
| `weather.actual.read` | Read | aucun | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Carte

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `map.read` | Read | aucun | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Caméra

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | aucun | `family` (0 conducteur, 1 passager, 2 extérieure, 3 carte), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | `family` (0..3), `field_of_view` (10..170) facultatifs ; au moins un | les clés de `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | `family` (0..3) requis ; `preset` (0..255, uniquement avec la famille 0 ou 1) facultatif | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. La politique est réappliquée toutes les 100 ms jusqu'à `camera.unlock` ; une réapplication en échec émet l'événement `camera.lock.degraded` une fois par erreur distincte. |
| `camera.unlock` | Action | aucun | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Véhicules routiers et joueur

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | aucun | `count`, `ai_collection`, `player_index` (l'index brut du véhicule du joueur dans OMSI, rapporté tel que lu ; utilisez `present` de `player-vehicle.read` pour savoir si un véhicule du joueur existe) | |
| `road-vehicles.list` | Read | aucun | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | `handle` requis | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | aucun | `present` (`false` lorsque OMSI n'a pas de véhicule du joueur) ; lorsque `true` : `player_index` plus chaque clé de `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | `model` requis (`Vehicles\...\*.bus` canonique, au plus 240 caractères, sans `..`, doit exister sous l'installation) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. N'attribue pas le PlayerVehicle. Prend plusieurs secondes ; utilisez le timeout client de 30 s. |
| `road-vehicles.place-random` | Action | `ai_type` (0..255, par défaut 0), `group` (0..65535, par défaut 1), `type` (-1..65535, par défaut -1), `scheduled` (0..1, par défaut 0), `tour` (0..65535, par défaut 0), `line` (0..65535, par défaut 0) facultatifs | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Humains

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | aucun | `count` | |
| `humans.list` | Read | aucun | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | `handle` requis | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Horaires, conducteurs, billets

Les résultats de liste bornée comportent `count` (tous les enregistrements dans OMSI), `returned_count` (lignes de cette réponse) et `truncated` (`true` lorsque des lignes ont été omises). Les lignes sont toujours les `returned_count` premiers enregistrements, numérotés à partir de `0`. Les limites de lignes sont de 128 pour les trajets (tracks), les courses (trips), les lignes de transport, les fichiers rv, les arrêts de bus, les liaisons entre arrêts, les services et les profils, de 256 pour les entrées de service et de 512 pour les entrées de trajet ; lorsque les lignes autorisées par cette limite dépassent encore la boîte aux lettres de 64 KiB (par exemple les entrées de trajet et de service sur Berlin-Spandau), le plugin supprime les dernières lignes jusqu'à ce que la réponse tienne et rapporte le `returned_count` réduit avec `truncated=true` (audit de documentation BUG-05 ; avant la correction, ces deux listes échouaient avec `OL_E_RUNTIME_RESPONSE_TOO_LARGE`). `timetable.logs.read` n'est pas une liste bornée : elle renvoie toutes les entrées du journal, et un journal de quelques centaines d'entrées dépasse la boîte aux lettres et échoue avec `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | aucun | `invalid` (`true`/`false`), ainsi que les nombres d'enregistrements `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | aucun | `count` ; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (toutes les entrées ; non bornée) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` pour un journal long |
| `drivers.read` | Read | aucun | `count`, `selected_index` ; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | aucun | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count` ; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Scripts, constantes, courbes et HOF du véhicule (portée d'un handle)

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | `handle` requis | `handle`, `count`, `returned_count`, `truncated` (limite 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | `handle`, `name` requis | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | `handle`, `name`, `value` (flottant fini) requis | `handle`, `name`, `requested_value`, `value` (relu) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | `handle` requis | `handle`, `count`, `returned_count`, `truncated` (limite 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | `handle`, `name` requis | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | `handle` requis | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | `handle`, `name` requis | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | `handle` requis | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | `handle`, `name`, `x` (flottant fini ; un `x` manquant ou non numérique donne `OL_E_RUNTIME_ARGUMENT_REQUIRED`) requis | `handle`, `name`, `x`, `value` (interpolation linéaire) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | `handle` requis | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Les opérations D3D s'exécutent sur le thread principal/de rendu d'OMSI par l'intermédiaire du pont natif. Les handles de texture ont la forme `d3dtex-<sessionId N>-<16 hex digits>`.

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | aucun | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`) requis ; `levels` (0..16, par défaut 1) facultatif | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | `handle` requis ; `level` (0..15, par défaut 0) facultatif | les 13 clés de `d3d.texture.create`, pour le niveau demandé | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, plus les erreurs de create |
| `d3d.texture.update` | Action | `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (au plus 48 KiB une fois décodé) requis ; `level` (0..15, par défaut 0), `x` (0..4095, par défaut 0), `y` (0..4095, par défaut 0) facultatifs | les 13 clés de `d3d.texture.create` après la mise à jour | `OL_E_D3D_INVALID_PIXEL_BUFFER`, plus les erreurs de describe |
| `d3d.texture.release` | Action | `handle` requis | les 13 clés de `d3d.texture.create` avec `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` en cas de libération répétée, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Les opérations D3D en échec renvoient les valeurs `detail` et `native_status`. Les méthodes d'extension de `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) encapsulent ces opérations pour les consommateurs de l'API.

<a id="getcapabilitiesasync-names"></a>
## Noms de `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` renvoie une liste statique d'enregistrements `Capability(Name, Available, EvidenceState, Reason)` qui décrivent la vision que l'hôte a de l'installation. Ces noms forment un vocabulaire distinct et plus grossier que les identifiants du registre ci-dessus ; le registre est le contrat des opérations, la liste des capacités est un résumé lisible par machine destiné aux intégrateurs. La correspondance est donnée dans la dernière colonne.

| Nom | Disponible | Preuves | Correspondance |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | dépend de la plateforme | STATICALLY_VALIDATED | [compatibilité](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | boîte aux lettres runtime |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (rejetée) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. La liste est un inventaire fixe et auto-déclaré : cette entrée n'a pas été mise à jour après que la série de clôture runtime a observé les transitions resetting/restored (`D01`) ; voir [état de la validation à l'exécution](../status/runtime-validation-status.md) pour les preuves |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | modes de monde de `session.start` |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year` ; les demander rend le plan non exécutable |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*` ; les demander rend le plan non exécutable |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` et les options associées ; les demander rend le plan non exécutable |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, `settings` du profil (RV-005 à l'exécution) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | les analyseurs de documents existent ; `InputSpec` n'est pas appliqué par une session (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

Le planificateur rapporte en outre des capacités propres à chaque plan dans `SessionPlan.RequiredCapabilities` et `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

<a id="known-limitations"></a>
## Limitations connues

- Les handles ont la portée d'une session ; la réutilisation d'adresse est détectée par une empreinte d'objet (VMT plus pointeur de définition pour les véhicules, VMT plus index d'humain pour les humains) et signalée par `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Angle mort résiduel : un objet de la même classe et de la même définition recréé à la même adresse entre deux lectures de liste est impossible à distinguer de l'original.
- Les résultats sont bornés à la boîte aux lettres de 64 KiB ; les listes bornées sont tronquées avec `truncated=true` ; `timetable.logs.read` n'est pas bornée et peut échouer avec `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.
- Pas de déplacement de véhicule d'une tuile à une autre, pas d'écriture de variables chaîne, pas d'écriture du calendrier, pas d'écriture de la météo, pas d'attribution headless du PlayerVehicle. Voir [limitations connues](known-limitations.md).
