# Compatibility

<!-- l10n: source=reference/compatibility.md -->
> British English edition of the [canonical page](../../../reference/compatibility.md) for OmsiLaunch 0.1.0-beta3. The US English page is normative: where the two differ, it and the code take precedence.

OmsiLaunch drives OMSI by patching profiled addresses inside one exact executable build. This page states which OMSI builds are supported, what happens with any other build, and the operating-system and runtime requirements of the host and the plugin. Sources: `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`, `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, and the project files.

## Supported OMSI builds

There is exactly one build profile, `Omsi23004_692EBFBF` (family `OMSI_2_3_004_COMMON`). It accepts two executables by exact SHA-256:

| Variant | `Omsi.exe` SHA-256 | Size | PE file version / product | Status |
| --- | --- | --- | --- | --- |
| Profiled executable (`ALTERNATE_LAA`) | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 bytes | 2.2.032 / 2.3.004 | `STABLE_BETA`; every runtime validation in the matrix ran on this file |
| Steam LAA (`STEAM_LAA`) | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | not checked | | Accepted by allow-list because it shares the profiled native layout and differs only in executable headers; **not runtime validated** (`profiles` reports `runtime_validated=false`, `validation_status=pending_beta_field_validation`). `PARTIAL`. |

`OmsiLaunch.exe profiles` prints this table as JSON. Version numbers are not used for acceptance: only the SHA-256 (and, for the primary executable, the exact size) count. No other OMSI 2 build, no patched executable, and no 4 GB-patched copy with a different hash is supported.

## What happens with an unknown build

| Stage | Check | Outcome |
| --- | --- | --- |
| Planning (`PlanSessionAsync`, `/plan`, `/validate`) | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | Required capability `omsi.profile.OMSI23004` is `UNAVAILABLE`; diagnostic `OL_E_UNSUPPORTED_BUILD`; `SessionPlan.IsRunnable=false`. CLI exit 1 for a launch, or 3 (`UnsupportedProfile`) when the error escapes as an exception. |
| Start (`StartSessionAsync`) | The spec is re-planned and `Omsi.exe` re-hashed | A plan that is no longer runnable (for example the executable changed after planning, or a caller edited `IsRunnable`) is rejected with `OL_E_PLAN_NOT_RUNNABLE`; no transaction is opened, no process is started. |
| In process (`PluginRuntime.Start`) | `NativeServices.ValidateBuild` requires the handoff's `BuildProfileId` to be `Omsi23004_692EBFBF` **and** `NativeValidateBuild()` to succeed against the running image | Telemetry `plugin.build.invalid`; the host fails the session with `OL_E_BUILD_VALIDATION_FAILED`; no native hook is armed; OMSI is terminated and the transaction restored. |

Because the executable hash is compared with the sizes and bytes of the profiled globals, the in-process check is the last line of defence against a copy that passed the hash check but whose image differs at load time. There is no fallback profile and no heuristic matching.

## Operating system and architecture

`CurrentWindowsX64Platform.Detect` computes `RuntimePlatformInfo`. The current platform is supported only when all of the following hold:

| Requirement | Check | Error when violated |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 or later | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64-bit Windows and a 64-bit host process | `OSArchitecture == X64` and `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Writable installation | Root directory exists, is not read-only, and contains `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` also reports `OmsiArchitecture` and `PluginArchitecture` as `X86` (OMSI is a 32-bit process; the plugin closure is x86 and runs under WOW64), `LegacyPlatform=false`, and `Wow64Available`. ARM64 Windows is not supported even where x64 emulation exists, because the host process must itself be x64.

## .NET requirements

| Component | Runtime | Notes |
| --- | --- | --- |
| Controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | The native bootstrapper locates the runtime through `hostfxr` via the packaged `nethost.dll`. A missing runtime is reported by the shim (exit codes 100-106; see [CLI](cli.md) and [exit codes](exit-codes.md)). |
| Plugin closure (`plugins\OmsiLaunch.Plugin.dll` via `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), hosted by DNNE 2.0.6 inside `Omsi.exe` | Requires the x86 .NET 6 Desktop/Core runtime to be installed on the machine; the 64-bit runtime alone is not sufficient for the plugin. |
| Native bridge (`plugins\OmsiLaunch.Native.x86.dll`) | native x86 | Loaded only from `plugins\` (see [permanent plugin](../concepts/permanent-plugin.md)). |

## Legacy platforms

Windows 7, Windows 8.x, Windows XP and other NT 6 and earlier systems are outside the current support boundary. `RuntimePlatformInfo.LegacyPlatform` is always `false` and no legacy adapter exists; the field and the `IPluginNativeServices` seam exist only so that a future legacy adapter could be added without changing the public API (see `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nothing in this release runs on those systems.

## Steam and Large Address Aware notes

- The Steam distribution of OMSI 2.3.004 with the LAA header (`7DAB063D...`) is allow-listed because its profiled addresses are identical to the primary executable. Until a field validation session is recorded in the matrix, treat every capability on that file as `PARTIAL`.
- Steam launches OMSI itself; a session must be started through `OmsiLaunch.exe` so that the handoff exists. Started from Steam, the permanent plugin stays inert (no handoff, no hooks).
- Applying a different LAA patcher to `Omsi.exe` changes its hash and makes it an unknown build.

## Related pages

- [Known limitations](known-limitations.md)
- [Runtime validation status](../status/runtime-validation-status.md)
- [Installation](../getting-started/installation.md)
