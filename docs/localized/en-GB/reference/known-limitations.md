# Known Limitations

<!-- l10n: source=reference/known-limitations.md -->
> British English edition of the [canonical page](../../../reference/known-limitations.md) for OmsiLaunch 0.1.0-beta3. The US English page is normative: where the two differ, it and the code take precedence.

This page lists, from the code, everything in OmsiLaunch 0.1.0-beta3 that is `UNAVAILABLE`, `PARTIAL`, or an accepted risk, so that users and integrators do not build on behaviour the product does not provide. Each row names the limitation, its stability, why it exists, and where it is documented in detail. The English documentation is normative; localised copies under `docs/localized/` are not maintained at the same level and may lag behind (see the last section).

## Compatibility

| Limitation | Stability | Detail |
| --- | --- | --- |
| Only `Omsi23004_692EBFBF` is supported (`692EBFBF...6243`); the Steam LAA hash `7DAB063D...D759` is allow-listed; its fingerprint and plan were validated with a controlled copy, but gameplay needs a genuine Steam installation (the image is the DRM-protected Steam executable) | `PARTIAL` for Steam LAA | [compatibility](compatibility.md) |
| Windows 10+ x64 only; no Windows 7/8, no XP, no ARM64 | `UNAVAILABLE` | [compatibility](compatibility.md) |
| The plugin needs the **x86** .NET 6 runtime in addition to the x64 runtime used by the controller | | [compatibility](compatibility.md) |

## World start and launch options

| Limitation | Stability | Detail |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last`, `WorldMode.LastMapState`) is not implemented; no timestamp-based `.osn` fallback is ever substituted | `UNAVAILABLE` (BI-006) | Plan diagnostic `OL_E_CAPABILITY_UNAVAILABLE` |
| Explicit or system date, time and year (`/date`, `/time`, `/year`, profile `new.date`/`new.time`/`new.year`, `DateSpec`/`TimeSpec`/`YearSpec`) are carried in the spec but make the plan not runnable; the plugin rejects non-`Unset` modes | `UNAVAILABLE` (`STATICALLY_PARTIAL`) | [session profiles](session-profiles.md), [launchspec](launchspec.md) |
| Weather preset, ICAO and real-current at start (`/weather*`, `new.weather`) | `UNAVAILABLE` (`STATICALLY_PARTIAL`, BI-003) | as above |
| Player vehicle model, repaint, HOF, fleet number, registration at start (`/vehicle` family, `PlayerVehicleSpec`) are resolved against the content catalogue but not applied; requesting them makes the plan not runnable; deterministic headless PlayerVehicle assignment is a future extension | `UNAVAILABLE` (BI-007) | `player.assign-headless` in [capabilities](capabilities.md) |
| Entrypoint by identity (`/entrypoint:<identity>`) is not correlated with OMSI's presented list; use `/entrypoint-index` | `PARTIAL` (BI-001) | plan capability `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| Keyboard and controller document overlays (`InputSpec`, `Environment.Keyboard`, `Environment.Controllers`) are parsed but never applied by a session | `UNAVAILABLE` (BI-005) | `input.*` capabilities |
| `LaunchBehaviorSpec.RestoreConfiguration` and `InstallationSpec.ExpectedExecutableSha256` are declared but never read | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds` (`/shutdown-timeout`, profile `shutdown-timeout`) is accepted and carried but not consumed by the supervisor | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [session lifecycle](../concepts/session-lifecycle.md) |
| `/quiet` and `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| Diagnostics flags (`/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native`) populate `DiagnosticsSpec`; the visible effect is limited to the host trace under `.omsilaunch\diagnostics` | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` are validation harnesses | `INTERNAL` | [CLI](cli.md) |

## Session end and process control

| Limitation | Stability | Detail |
| --- | --- | --- |
| Session stop is a forced termination: `session.stop`, tray "End session", Ctrl+C and `CloseAsync` all lead to `TerminateProcess`. OMSI's shutdown routine does not run, OMSI does not rewrite `options.cfg` or its logs on exit, and any unsaved OMSI state is lost. This is deliberate: it keeps OMSI from writing over restored files. | by design | [session lifecycle](../concepts/session-lifecycle.md) |
| Cooperative WM_CLOSE shutdown with a timeout and terminate fallback is not implemented | `UNAVAILABLE` (product decision, S-11; OMSI ignored `WM_CLOSE` to its main window in the runtime closure round) | [runtime validation status](../status/runtime-validation-status.md) |
| On console close or logoff the owner has a 4 s budget to stop and restore; anything left is recovered by the journal on the next start | console close runtime validated; logoff not exercised | [transactions and recovery](../concepts/transactions-and-recovery.md) |

## Transaction, recovery and lease

| Limitation | Stability | Detail |
| --- | --- | --- |
| Installation lease is a `Local\` semaphore: one owner per installation **per logon session**; not enforced across users; not released while another process holds a handle; any same-user process can hold the name | accepted risk (S-18) | [transactions and recovery](../concepts/transactions-and-recovery.md) |
| Recovery is refused (`OL_E_INSTALLATION_BUSY`) while the journalled OMSI process, or for a journal without PID any `Omsi.exe` from that root, is running | by design | as above |
| An originally-absent overlay path whose content changed during the session blocks restore (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) until inspected | by design | as above |
| Journals from before ownership fingerprints can only be closed by a session with identical planned bytes (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | as above |
| Only session-owned paths are restored. OMSI's own writes during a session (`options.cfg` `[last_map]` when no setting overlays `options.cfg`, `Texture\standard.ipr`, caches, `laststn.osn`, driver profile, logs) persist, as after a direct OMSI start | by design | [transactions and recovery](../concepts/transactions-and-recovery.md) |
| Stale `closecheck` removal before a session is permanent (recorded, not restored) when `SuppressStaleClosecheckWarning` is true | by design | as above |

## Runtime control

| Limitation | Stability | Detail |
| --- | --- | --- |
| `weather.set` is rejected (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`): OMSI overwrites both profiled wind candidates on its next weather tick | `UNAVAILABLE` | [capabilities](capabilities.md) |
| Calendar writes (`SetActualDateTime`) | `UNAVAILABLE` (BI-002) | `calendar.set-actual-date-time` |
| String-variable writes, named triggers, sound triggers (Delphi managed-string ownership) | `UNAVAILABLE` (BI-004) | `scripts.string.read` is read-only |
| No vehicle relocation, no cross-tile spatial rebinding, no ODE-safe transform authority; position fields are read-only | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` need a PlayerVehicle; the headless NEW_MAP start has none (a saved situation provides one) | by design (BI-007) | RV-004 |
| Runtime mutations (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, D3D textures) are not journalled and not restored | by design | [runtime control](runtime-control.md) |
| Handle fingerprint blind spot: an object of the same class and definition recreated at the same address between two list reads is not detected as stale; natural-removal lifetime (RV-002) has no safe runtime producer and stays offline | `PARTIAL` | [runtime control](runtime-control.md) |
| Results are bounded by the 64 KiB mailbox: long lists are truncated (`truncated=true`); pixel payloads are limited to 48 KiB per `d3d.texture.update` | by design | [capabilities](capabilities.md) |
| Single-flight channel: one request at a time per session; a busy slot is `OL_E_RUNTIME_CHANNEL_BUSY`; request ids must not be reused | by design | [runtime control](runtime-control.md) |
| Telemetry is a latest-value slot: bursts faster than the host's 100 ms sampling can lose intermediate events (sequence numbers keep identical consecutive events distinct; torn samples are skipped) | `PARTIAL` | [permanent plugin](../concepts/permanent-plugin.md) |
| D3D device reset was observed at runtime (`resetting`, `restored`, generation invalidation); a distinct `lost` transition was not produced because OMSI's device went straight to `DEVICENOTRESET` | `PARTIAL` (RV-007) | [runtime validation status](../status/runtime-validation-status.md) |
| Bounded list results (the `timetable.*.list` operations, `vehicle.variables.list`, `vehicle.string-variables.list`) return at most the rows that fit the 64 KiB runtime slot; the rest are left out with `truncated=true` and a smaller `returned_count` (documentation audit BUG-05). There is no paging in this release | by design | [capabilities](capabilities.md) |
| `timetable.logs.read`, `road-vehicles.list`, `humans.list`, `vehicle.constants.list` and `vehicle.curves.list` are not bounded: a result larger than the slot fails with `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (observed for none of them on the tested maps) | `PARTIAL` | [capabilities](capabilities.md) |
| The self-reported evidence strings (`PublicCapabilityRegistry` `RuntimeValidation`, `GetCapabilitiesAsync` `EvidenceState`) were not updated after the runtime closure round: `camera.lock` still reads `STATICALLY_VALIDATED` and `runtime.d3d.lifecycle.reset` `IMPLEMENTED_NOT_RUNTIME_VALIDATED`. The [runtime validation status](../status/runtime-validation-status.md) page is authoritative | documentation lag, not a behaviour difference | [capabilities](capabilities.md) |
| Some advanced map/tile/path/object graph fields are not exposed; runtime readers are profile-gated typed snapshots, never arbitrary memory access | by design | [capabilities](capabilities.md) |
| In-process memory reads are check-then-use against a live OMSI; a concurrent OMSI mutation between the check and the read can yield an inconsistent snapshot (`OL_E_RUNTIME_OPERATION_FAILED`) | accepted risk (S-33) | |

## Local control plane and trust model

| Limitation | Stability | Detail |
| --- | --- | --- |
| Same-user trust model: the named pipe (`CurrentUserOnly`), the handoff/telemetry/runtime memory mappings and the lease semaphore are accessible to any process of the same Windows user. Such a process can read status, stop the session or execute runtime operations once it has read the `session_id`. | accepted risk (S-06, S-30) | [local control](local-control.md) |
| The control endpoint exists only while the owner is `Running`; a client sees `OL_E_NO_ACTIVE_SESSION` (exit 4) during startup and after the session ends | by design | [local control](local-control.md) |
| If another process already owns the pipe name, the owner keeps running without an endpoint (`ListenFault`), and a second launch may misreport `OL_E_SESSION_ALREADY_ACTIVE` | accepted risk | [local control](local-control.md) |
| `.omsilaunch\` inherits the ACL of the OMSI root; no explicit access control is applied | accepted risk (S-31) | [transactions and recovery](../concepts/transactions-and-recovery.md) |

## Diagnostics and output

| Limitation | Stability | Detail |
| --- | --- | --- |
| Diagnostics are local files only (`.omsilaunch\diagnostics`); nothing is uploaded, and there is no remote reporting | by design | [transactions and recovery](../concepts/transactions-and-recovery.md) |
| Retention keeps the 50 newest sessions; older session-prefixed diagnostics are deleted when a new session starts | by design | as above |
| JSON output and diagnostics include installation paths (`RootPath`, asset directories, `.itx` paths) | by design (local data) | |
| The session-failure dialogue of `OmsiLaunchW.exe` shows the plugin's failure payload (for example `{"name":"world.failed",...}`) as its message rather than a sentence; the `Code:` line is correct | cosmetic | [windows tray](windows-tray.md) |
| A D3D request rejected by the native bridge before any Direct3D call reports `native_status` correctly but its `detail` text says `HRESULT 0x00000000` | cosmetic | [capabilities](capabilities.md) |
| The tray status window is a snapshot of the planned session taken when it opens; it does not refresh and shows no live OMSI values | by design | [windows tray](windows-tray.md) |

## Documentation

The English pages under `docs/` are the normative documentation for this release. `docs/localized/<locale>/` contains translations of the same `0.1.0-beta3` pages (see [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); where a translation differs from the English text, the English text and the code are authoritative. The historical and legacy pages listed there are available in English only.

Related: [capabilities](capabilities.md), [runtime validation status](../status/runtime-validation-status.md), [errors](errors.md).
