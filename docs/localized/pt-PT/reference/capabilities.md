# Capacidades

<!-- l10n: source=reference/capabilities.md -->
> Tradução da [página original em inglês](../../../reference/capabilities.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Este é o inventário canónico daquilo que o OmsiLaunch 0.1.0-beta3 consegue fazer, derivado de `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`), da implementação de runtime no plugin (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs` e `src/OmsiLaunch.Interop/OmsiRuntimeReaders.cs`) e de `OmsiLaunchService.GetCapabilitiesAsync`. Para cada capacidade indica a classificação, o nível de estabilidade usado em toda esta documentação, se é necessária uma sessão `Running`, se altera o OMSI, os argumentos e as chaves de resultado, os erros e a evidência de validação em runtime. A forma de invocar uma operação (rotas da CLI, envelope da API, timeouts, handles) está em [controlo de runtime](runtime-control.md); a tabela de evidências está em [estado da validação em runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classificação e estabilidade

`PublicCapabilityClassification` tem quatro valores. Correspondem ao vocabulário de estabilidade da seguinte forma, com despromoções onde a evidência é incompleta:

| Classificação | Significado | Estabilidade |
| --- | --- | --- |
| `PublicStableBeta` | Pública, no contrato Beta, validada em runtime | `STABLE_BETA` (despromovida para `PARTIAL` onde indicado) |
| `PublicExperimental` | Pública, pode mudar, validada em runtime ou validada estaticamente | `EXPERIMENTAL` (despromovida para `PARTIAL` onde indicado) |
| `InternalOnly` | Primitiva de investigação; não acessível através da API pública nem da CLI | `INTERNAL` |
| `Unsupported` | Reconhecida para efeitos de diagnóstico, rejeitada ou ausente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` e `Event`. Todas as capacidades de runtime exigem uma sessão `Running` e o perfil exato `Omsi23004_692EBFBF` (`RequiresSession` / `RequiresExactProfile` no registo); as capacidades de sessão que criam uma sessão exigem o perfil exato, mas não uma sessão.

Os resultados de runtime são dicionários de strings. As chaves que começam por `internal_` ou terminam em `_address`, `_pointer` ou `_vmt` são removidas na fronteira da API (`ScrubInternalValues`) e não são listadas aqui. As chaves de resultado nas tabelas de operações abaixo são os conjuntos exatos de chaves devolvidos pelo produto: foram capturadas através da execução de todas as operações públicas numa sessão real (captura de documentação do fecho de runtime, sessão `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; as linhas das listas escrevem-se `<row>.<n>.<field>`).

Como é comunicada a falha de uma operação (`CurrentRuntimeControl.Execute`):

| Falha | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rejeição pelo registo (operação desconhecida ou `internal.*`, argumento obrigatório em falta) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | nenhum |
| Uma rejeição deliberada do produto (`weather.set`) | o código específico (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (frase) |
| Uma falha de D3D | o código `OL_E_D3D_*` específico | `detail`, `native_status` |
| Qualquer outra falha do lado do plugin (handle obsoleto, nome desconhecido, valor fora do intervalo, sem veículo do jogador, ...) | `OL_E_RUNTIME_OPERATION_FAILED` | `detail` (começa pelo código específico, por exemplo `OL_E_RUNTIME_OBJECT_HANDLE_STALE` ou `OL_E_RUNTIME_VALUE_OUT_OF_RANGE (Parameter 'minute')`), `exception` (nome do tipo .NET) |
| Um resultado que não cabe na mailbox de 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (as listas limitadas são encurtadas em vez disso, ver [Horário, motoristas, bilhetes](#timetable-drivers-tickets)) | nenhum |

A coluna `Errors` («Erros») de cada tabela indica os códigos específicos; devem ler-se a partir de `ErrorCode` ou do primeiro token de `Values.detail`, como acima.

<a id="session-capabilities"></a>
## Capacidades de sessão

| Capacidade | Classificação | Estabilidade | Tipo | Rota da API | Rota da CLI | Altera | Validação |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | `STABLE_BETA` | Action | `PlanSessionAsync` | `/plan`, `/validate` | não | RUNTIME_PASS (todas as sessões validadas começam com um plano) |
| `session.start` | `PublicStableBeta` | `STABLE_BETA` | Action | `StartSessionAsync` | flags de lançamento | sistema de ficheiros (transação), processo | RUNTIME_PASS (NEW_MAP Grundorf, SAVED_SITUATION Berlin-Spandau) |
| `session.status` | `PublicStableBeta` | `STABLE_BETA` | Read | `GetStatusAsync` | `session status` | não | RUNTIME_PASS |
| `session.stop` | `PublicStableBeta` | `STABLE_BETA` | Action | `StopAsync` / `CloseAsync` | `session stop`, área de notificação, Ctrl+C | processo (`TerminateProcess`), sistema de ficheiros (restauro) | RUNTIME_PASS; encerramento cooperativo não implementado |
| `session.recover` | `PublicStableBeta` | `STABLE_BETA` | Action | `RecoverPendingAsync` | `/recovery-status`, `/recover` | sistema de ficheiros (restauro) | RUNTIME_PASS: recuperação após saída prematura (RV-008), proprietário terminado mais recuperação antecipada no arranque seguinte (`S05`), restauro falhado seguido de `/recover` (`F01`), recusa sob a lease, com um OMSI órfão e na janela anterior ao PID (`S04`, `S04b`) |
| `events.read` | `PublicExperimental` | `EXPERIMENTAL` | Event | `GetStatusAsync().RuntimeEvents`, controlo `session.events` | `events read`, `events watch` | não | RUNTIME_PASS; o slot de telemetria de último valor pode perder rajadas |

<a id="runtime-capabilities-registry-entries"></a>
## Capacidades de runtime (entradas do registo)

| Capacidade | Classificação | Estabilidade | Tipo | Operações | Handles | Validação |
| --- | --- | --- | --- | --- | --- | --- |
| `time.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `time.read` | | RUNTIME_PASS (sessão `e5454061`) |
| `time.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `time.set` | | RUNTIME_PASS (escrita, `SetTime`, releitura) |
| `weather.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `weather.read` | | RUNTIME_PASS |
| `weather.set` | `Unsupported` | `UNAVAILABLE` | Write | `weather.set` | | RUNTIME_REJECTED: sempre `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (sessão `50a1f1ec`) |
| `weather.actual.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `weather.actual.read` | | RUNTIME_PASS |
| `map.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `map.read` | | RUNTIME_PASS: revalidada no slot de mapa corrigido (sessão `9dd62626-94c6-4cd7-bb6f-0288327696f4`, Grundorf) e lida em Berlin-Spandau na captura de documentação |
| `camera.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `camera.read` | | RUNTIME_PASS |
| `camera.set` | `PublicExperimental` | `EXPERIMENTAL` | Write | `camera.set` | | RUNTIME_PASS (escrita do FOV e releitura) |
| `camera.lock` | `PublicExperimental` | `EXPERIMENTAL` | Action | `camera.lock`, `camera.unlock` | | RUNTIME_PASS com um PlayerVehicle de uma situação guardada (fecho de runtime `CAM01`, famílias 0, 2 e 1 com releitura, seguidas de desbloqueio). A própria string `RuntimeValidation` do registo ainda indica `STATICALLY_VALIDATED` (autodeclaração do produto, não atualizada nesta versão) |
| `vehicles.list` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.list` | RoadVehicle | RUNTIME_PASS |
| `vehicles.get` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicle.read` | RoadVehicle | RUNTIME_PASS; a deteção de handles obsoletos após remoção natural (RV-002) não tem produtor seguro em runtime e está validada offline |
| `vehicles.summary` | `PublicStableBeta` | `STABLE_BETA` | Read | `road-vehicles.read` | | RUNTIME_PASS |
| `vehicles.spawn` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.spawn` | RoadVehicle | RUNTIME_PASS RV-003 (sessão `5f641c8d`, `2 -> 3`, handle `rv-000003`) |
| `vehicles.place-random` | `PublicExperimental` | `EXPERIMENTAL` | Action | `road-vehicles.place-random` | | RUNTIME_PASS (coleção `2 -> 4`) |
| `player.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `player-vehicle.read` | RoadVehicle | RUNTIME_PASS: `present=false` num arranque headless, o snapshot completo com uma situação guardada |
| `humans.list` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.list` | Human | RUNTIME_PASS |
| `humans.get` | `PublicExperimental` | `EXPERIMENTAL` | Read | `human.read` | Human | RUNTIME_PASS |
| `humans.summary` | `PublicExperimental` | `EXPERIMENTAL` | Read | `humans.read` | | RUNTIME_PASS |
| `timetable.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `timetable.read` e a família `timetable.*.list` / `timetable.logs.read` | | RUNTIME_PASS (entradas de trajeto: 91 registos em Grundorf) |
| `scripts.numeric` | `PublicExperimental` | `EXPERIMENTAL` | Write | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set` | RoadVehicle | RUNTIME_PASS (`Refresh_Strings` 0 -> 1 -> 0) |
| `scripts.string.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | RoadVehicle | RUNTIME_PASS (apenas leitura; escritas BI-004) |
| `constants` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.constants.list`, `vehicle.constant.get` | RoadVehicle | RUNTIME_PASS |
| `curves` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.curves.list`, `vehicle.curve.evaluate` | RoadVehicle | RUNTIME_PASS |
| `hof.read` | `PublicStableBeta` | `STABLE_BETA` | Read | `vehicle.hofs.read` | RoadVehicle | RUNTIME_PASS |
| `drivers.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `drivers.read` | | RUNTIME_PASS |
| `tickets.read` | `PublicExperimental` | `EXPERIMENTAL` | Read | `tickets.read` | | RUNTIME_PASS |
| `d3d.texture` | `PublicExperimental` | `EXPERIMENTAL` | Action | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | D3DTexture | RUNTIME_PASS para create/describe/update/release, rejeição de handles libertados e obsoletos através de recriações e de sessões (`H02`), e reset do dispositivo com invalidação da geração (RV-007, `D01`; o próprio estado `lost` não foi produzido) |
| `internal.make-basic` | `InternalOnly` | `INTERNAL` | Action | `internal.road-vehicles.make-basic` | | Não pública. `ExecuteRuntimeAsync` rejeita-a com `OL_E_RUNTIME_OPERATION_UNKNOWN` antes de qualquer pesquisa de sessão; a CLI não tem rota para ela. |
| `calendar.set-actual-date-time` | `Unsupported` | `UNAVAILABLE` | Write | nenhuma | | UNSUPPORTED (BI-002) |
| `player.assign-headless` | `Unsupported` | `UNAVAILABLE` | Action | nenhuma | | UNSUPPORTED (BI-007) |

`internal.road-vehicles.make-basic` é `INTERNAL`: existe no plugin para investigação (devolve um endereço nativo) e é rejeitada na fronteira da API e pelo plano de controlo local, que validam ambos primeiro o nome da operação contra `PublicRuntimeOperationIds`.

<a id="public-runtime-operations"></a>
## Operações de runtime públicas

Esta é a lista exata de ids de operação que um frontend público pode reencaminhar (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Todas exigem `SessionState.Running`; nenhuma é registada no journal nem restaurada. Os argumentos obrigatórios são impostos pelo registo antes de a mailbox ser usada (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); os argumentos opcionais são validados pelo plugin. Códigos de falha comuns a todas as operações: `OL_E_RUNTIME_OPERATION_UNKNOWN` (não consta desta lista), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (o plugin não tem implementação), `OL_E_RUNTIME_OPERATION_FAILED` (exceção inesperada dentro do processo; valores `detail` e `exception`).

<a id="time"></a>
### Hora

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `time.read` | Read | nenhum | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | opcionais `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimal); pelo menos um | as chaves de `time.read` após o `SetTime` perfilado | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Meteorologia

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | nenhum | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` | |
| `weather.set` | Write | quaisquer | nenhuma | Sempre `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (argumentos vazios: `OL_E_RUNTIME_ARGUMENT_REQUIRED`). O OMSI sobrescreve ambos os candidatos de vento perfilados no seu próximo ciclo de meteorologia. |
| `weather.actual.read` | Read | nenhum | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mapa

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `map.read` | Read | nenhum | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Câmara

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | nenhum | `family` (0 motorista, 1 passageiro, 2 exterior, 3 mapa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | opcionais `family` (0..3), `field_of_view` (10..170); pelo menos um | as chaves de `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | obrigatório `family` (0..3); opcional `preset` (0..255, apenas com a família 0 ou 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. A política é reaplicada a cada 100 ms até `camera.unlock`; uma reaplicação falhada emite o evento `camera.lock.degraded` uma vez por cada erro distinto. |
| `camera.unlock` | Action | nenhum | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Veículos rodoviários e jogador

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | nenhum | `count`, `ai_collection`, `player_index` (índice bruto do veículo do jogador no OMSI, comunicado tal como é lido; usar `present` de `player-vehicle.read` para saber se existe um veículo do jogador) | |
| `road-vehicles.list` | Read | nenhum | `count`, `vehicle.<n>.handle` (`rv-NNNNNN`) | |
| `road-vehicle.read` | Read | obrigatório `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | nenhum | `present` (`false` quando o OMSI não tem veículo do jogador); quando `true`: `player_index` mais todas as chaves de `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | obrigatório `model` (canónico `Vehicles\...\*.bus`, no máximo 240 caracteres, sem `..`, tem de existir abaixo da instalação) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Não atribui o PlayerVehicle. Demora vários segundos; usar o timeout de cliente de 30 s. |
| `road-vehicles.place-random` | Action | opcionais `ai_type` (0..255, predefinição 0), `group` (0..65535, predefinição 1), `type` (-1..65535, predefinição -1), `scheduled` (0..1, predefinição 0), `tour` (0..65535, predefinição 0), `line` (0..65535, predefinição 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Humanos

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | nenhum | `count` | |
| `humans.list` | Read | nenhum | `count`, `human.<n>.handle` (`hb-NNNNNN`) | |
| `human.read` | Read | obrigatório `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Horário, motoristas, bilhetes

Os resultados de listas limitadas incluem `count` (todos os registos no OMSI), `returned_count` (linhas nesta resposta) e `truncated` (`true` quando foram omitidas linhas). As linhas são sempre os primeiros `returned_count` registos, numerados a partir de `0`. Os limites de linhas são 128 para trajetos, viagens, linhas, ficheiros rv, paragens, ligações entre paragens, serviços (tours) e perfis, 256 para entradas de serviço e 512 para entradas de trajeto; quando as linhas permitidas por esse limite ainda excedem a mailbox de 64 KiB (por exemplo, as entradas de trajeto e de serviço em Berlin-Spandau), o plugin descarta as últimas linhas até a resposta caber e comunica o `returned_count` mais pequeno com `truncated=true` (auditoria de documentação BUG-05; antes da correção, estas duas listas falhavam com `OL_E_RUNTIME_RESPONSE_TOO_LARGE`). `timetable.logs.read` não é uma lista limitada: devolve todas as entradas do registo, e um registo de algumas centenas de entradas excede a mailbox e falha com `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | nenhum | `invalid` (`true`/`false`) e as contagens de registos `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | nenhum | `count`, `returned_count`, `truncated`; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | nenhum | `count`; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` (todas as entradas; não limitada) | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` para um registo longo |
| `drivers.read` | Read | nenhum | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | nenhum | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Scripts, constantes, curvas e HOF de veículos (por handle)

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | obrigatório `handle` | `handle`, `count`, `returned_count`, `truncated` (limite 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | obrigatórios `handle`, `name`, `value` (float finito) | `handle`, `name`, `requested_value`, `value` (relido) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | obrigatório `handle` | `handle`, `count`, `returned_count`, `truncated` (limite 512), `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.constants.list` | Read | obrigatório `handle` | `handle`, `count`, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | obrigatório `handle` | `handle`, `count`, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | obrigatórios `handle`, `name`, `x` (float finito; um `x` em falta ou não numérico dá `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolação linear) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | obrigatório `handle` | `handle`, `count`, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

As operações D3D são executadas na thread principal/de renderização do OMSI através da ponte nativa. Os handles de textura têm a forma `d3dtex-<sessionId N>-<16 hex digits>`.

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | nenhum | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | obrigatórios `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); opcional `levels` (0..16, predefinição 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | obrigatório `handle`; opcional `level` (0..15, predefinição 0) | as 13 chaves de `d3d.texture.create`, para o nível pedido | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, mais os erros de create |
| `d3d.texture.update` | Action | obrigatórios `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (no máximo 48 KiB descodificados); opcionais `level` (0..15, predefinição 0), `x` (0..4095, predefinição 0), `y` (0..4095, predefinição 0) | as 13 chaves de `d3d.texture.create` após a atualização | `OL_E_D3D_INVALID_PIXEL_BUFFER`, mais os erros de describe |
| `d3d.texture.release` | Action | obrigatório `handle` | as 13 chaves de `d3d.texture.create` com `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` numa libertação repetida, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

As operações D3D falhadas devolvem os valores `detail` e `native_status`. Os métodos de extensão de `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) encapsulam estas operações para os consumidores da API.

<a id="getcapabilitiesasync-names"></a>
## Nomes de `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` devolve uma lista estática de registos `Capability(Name, Available, EvidenceState, Reason)` que descrevem a visão que o anfitrião tem da instalação. Estes nomes constituem um vocabulário separado e menos granular do que os ids do registo acima; o registo é o contrato para as operações, e a lista de capacidades é um resumo legível por máquina para integradores. A relação é indicada na última coluna.

| Nome | Disponível | Evidência | Relação |
| --- | --- | --- | --- |
| `runtime.current-windows-x64` | depende da plataforma | STATICALLY_VALIDATED | [compatibilidade](compatibility.md) |
| `runtime.command-channel` | true | STATICALLY_VALIDATED | mailbox de runtime |
| `runtime.time.read`, `runtime.time.write` | true | RUNTIME_VALIDATED | `time.read`, `time.set` |
| `runtime.time.actual-date-time.write` | false | RELEASE_IF_CLOSED | `calendar.set-actual-date-time` |
| `runtime.map.read` | true | RUNTIME_VALIDATED | `map.read` |
| `runtime.weather.read`, `runtime.weather.actual.read` | true | RUNTIME_VALIDATED | `weather.read`, `weather.actual.read` |
| `runtime.weather.write` | false | RUNTIME_PARTIAL | `weather.set` (rejeitada) |
| `runtime.camera.read`, `runtime.camera.write` | true | RUNTIME_VALIDATED | `camera.read`, `camera.set` |
| `runtime.d3d.status`, `runtime.d3d.texture.create`, `runtime.d3d.texture.update`, `runtime.d3d.texture.describe`, `runtime.d3d.texture.release` | true | RUNTIME_VALIDATED | `d3d.*` |
| `runtime.d3d.lifecycle.reset` | true | IMPLEMENTED_NOT_RUNTIME_VALIDATED | RV-007. A lista é um inventário fixo e autodeclarado: esta entrada não foi atualizada depois de a ronda de fecho de runtime ter observado as transições resetting/restored (`D01`); ver [estado da validação em runtime](../status/runtime-validation-status.md) para a evidência |
| `runtime.road-vehicles.read`, `runtime.road-vehicles.spawn`, `runtime.road-vehicles.place-random` | true | RUNTIME_VALIDATED | `road-vehicles.*` |
| `runtime.vehicle.variable.write` | true | RUNTIME_VALIDATED | `vehicle.variable.set` |
| `runtime.humans.read`, `runtime.timetable.read`, `runtime.timetable.track-entries.read` | true | RUNTIME_VALIDATED | `humans.*`, `timetable.*` |
| `world.new-map`, `world.presented-entrypoint`, `world.saved-situation` | true | RUNTIME_VALIDATED | modos de mundo de `session.start` |
| `world.entrypoint-identity` | false | RUNTIME_PARTIAL | BI-001 |
| `world.last-map-state` | false | UNSUPPORTED_FOR_CURRENT_PROFILE | `LastMapState` (`/last`) |
| `world.date.explicit`, `world.date.system`, `world.time.explicit`, `world.time.system` | false | STATICALLY_PARTIAL | `/date`, `/time`, `/year`; pedi-las torna o plano não executável |
| `weather.preset`, `weather.icao`, `weather.real-current` | false | STATICALLY_PARTIAL | `/weather*`; pedi-las torna o plano não executável |
| `player-vehicle.model`, `player-vehicle.repaint`, `player-vehicle.hof`, `player-vehicle.fleet-number`, `player-vehicle.registration` | false | STATICALLY_PARTIAL | `/vehicle` e flags relacionadas; pedi-las torna o plano não executável |
| `configuration.options.semantic` | true | STATICALLY_VALIDATED | `/set`, `settings` do perfil (RV-005 em runtime) |
| `input.keyboard.patch`, `input.controller.active-ffscale` | true | STATICALLY_VALIDATED | existem parsers de documentos; `InputSpec` não é aplicado por uma sessão (BI-005) |
| `input.controller.axis-buttons` | false | STATICALLY_PARTIAL | BI-005 |
| `content.maps`, `content.situations`, `content.vehicles`, `content.repaints`, `content.hofs`, `content.fleet-registration-sources` | true | STATICALLY_VALIDATED | `DiscoverAsync`, `/list` |

O planeador comunica adicionalmente capacidades por plano em `SessionPlan.RequiredCapabilities` e `SessionPlan.UnsupportedRequestedFeatures` (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.profile.OMSI23004`, `world.new-map`, `world.presented-entrypoint`, `world.entrypoint-identity`, `world.saved-situation`, `boot.headless-start`, `internet-textures.disabled`, `world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `weather`, `player-vehicle.*`, `input.keyboard`, `input.controller`, `content.*`).

<a id="known-limitations"></a>
## Limitações conhecidas

- Os handles têm âmbito de sessão; a reutilização de endereços é detetada por uma impressão digital do objeto (VMT mais ponteiro de definição para veículos, VMT mais índice de humano para humanos) e comunicada como `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Ponto cego residual: um objeto da mesma classe e com a mesma definição, recriado no mesmo endereço entre duas leituras da lista, é indistinguível do original.
- Os resultados estão limitados à mailbox de 64 KiB; as listas limitadas são truncadas com `truncated=true`; `timetable.logs.read` não é limitada e pode falhar com `OL_E_RUNTIME_RESPONSE_TOO_LARGE`.
- Sem relocalização de veículos entre tiles, sem escrita de variáveis de string, sem escrita de calendário, sem escrita de meteorologia, sem atribuição headless do PlayerVehicle. Ver [limitações conhecidas](known-limitations.md).
