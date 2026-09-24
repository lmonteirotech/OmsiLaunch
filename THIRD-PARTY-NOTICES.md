# Third-Party Notices

## OmsiHook

- Repository: `https://github.com/space928/Omsi-Extensions`
- Commit: `7687b6623f5f74b4419695257bd2a4eef54dd93e`
- License: `LGPL-3.0-only`
- Retrieval: 2026-09-07

The pinned checkout is reference-only until each adapted file receives a
provenance header and review. See the repository-level reuse matrix.

## YamlDotNet

- Package: `YamlDotNet` 15.1.2
- License: MIT
- Purpose: strict, non-executable YAML parsing for public Session Profiles.

The package is distributed as a managed dependency of the controller. Its
assembly identity remains that of its upstream authors and is intentionally not
rewritten as an OmsiLaunch binary.

## DNNE

- Package: `DNNE` 2.0.6
- License: MIT
- Purpose: build-time generator for the native export shim
  (`plugins\OmsiLaunch.PluginNE.dll`) that lets OMSI load the managed
  `OmsiLaunch.Plugin` assembly through the standard `.opl` plugin interface.

DNNE is consumed only by `src\OmsiLaunch.Plugin\OmsiLaunch.Plugin.csproj`. The
generated shim is part of the permanent plugin closure and is listed in
`release-manifest.json`; DNNE itself is not a runtime dependency of the
controller.

## nethost (.NET runtime host)

- Component: `nethost.dll` from the Microsoft .NET app-host pack
- License: MIT (part of the .NET runtime distribution)
- Purpose: locates `hostfxr` for the native bootstrappers `OmsiLaunch.exe` and
  `OmsiLaunchW.exe` (`tools\OmsiLaunch.Bootstrapper\*.cpp`), which then load
  the managed `OmsiLaunch.Controller.dll`.

`nethost.dll` is shipped unmodified next to the bootstrappers by
`tools\New-ReleasePackage.ps1`. It is not an OmsiLaunch binary and is excluded
from the OmsiLaunch PE identity audit.
