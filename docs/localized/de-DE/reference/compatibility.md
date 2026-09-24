# Kompatibilität

<!-- l10n: source=reference/compatibility.md -->
> Übersetzung der [englischen Originalseite](../../../reference/compatibility.md) für OmsiLaunch 0.1.0-beta3. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

OmsiLaunch steuert OMSI, indem es profilierte Adressen innerhalb eines exakt festgelegten Builds der Programmdatei patcht. Diese Seite gibt an, welche OMSI-Builds unterstützt werden, was mit jedem anderen Build geschieht und welche Betriebssystem- und Runtime-Anforderungen für den Host und das Plugin gelten. Quellen: `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`, `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs` sowie die Projektdateien.

<a id="supported-omsi-builds"></a>
## Unterstützte OMSI-Builds

Es gibt genau ein Build-Profil, `Omsi23004_692EBFBF` (Familie `OMSI_2_3_004_COMMON`). Es akzeptiert zwei Programmdateien anhand ihres exakten SHA-256:

| Variante | `Omsi.exe` SHA-256 | Größe | PE-Dateiversion / Produkt | Status |
| --- | --- | --- | --- | --- |
| Profilierte Programmdatei (`ALTERNATE_LAA`) | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8.503.440 Byte | 2.2.032 / 2.3.004 | `STABLE_BETA`; jede Runtime-Validierung in der Matrix lief mit dieser Datei |
| Steam LAA (`STEAM_LAA`) | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | nicht geprüft | | Per Allowlist akzeptiert, weil sie das profilierte native Layout teilt und sich nur in den Headern der Programmdatei unterscheidet; **nicht zur Laufzeit validiert** (`profiles` meldet `runtime_validated=false`, `validation_status=pending_beta_field_validation`). `PARTIAL`. |

`OmsiLaunch.exe profiles` gibt diese Tabelle als JSON aus. Versionsnummern werden für die Akzeptanz nicht verwendet: Es zählen nur der SHA-256 (und, bei der primären Programmdatei, die exakte Größe). Kein anderer OMSI-2-Build, keine gepatchte Programmdatei und keine mit dem 4-GB-Patch versehene Kopie mit abweichendem Hash wird unterstützt.

<a id="what-happens-with-an-unknown-build"></a>
## Was bei einem unbekannten Build geschieht

| Phase | Prüfung | Ergebnis |
| --- | --- | --- |
| Planung (`PlanSessionAsync`, `/plan`, `/validate`) | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | Die erforderliche Capability `omsi.profile.OMSI23004` ist `UNAVAILABLE`; Diagnose `OL_E_UNSUPPORTED_BUILD`; `SessionPlan.IsRunnable=false`. CLI-Exit 1 bei einem Start oder 3 (`UnsupportedProfile`), wenn der Fehler als Ausnahme durchschlägt. |
| Start (`StartSessionAsync`) | Die Spezifikation wird neu geplant und der Hash von `Omsi.exe` neu berechnet | Ein Plan, der nicht mehr ausführbar ist (zum Beispiel weil sich die Programmdatei nach der Planung geändert hat oder ein Aufrufer `IsRunnable` bearbeitet hat), wird mit `OL_E_PLAN_NOT_RUNNABLE` abgelehnt; es wird keine Transaktion geöffnet und kein Prozess gestartet. |
| Im Prozess (`PluginRuntime.Start`) | `NativeServices.ValidateBuild` verlangt, dass die `BuildProfileId` der Übergabe `Omsi23004_692EBFBF` ist **und** `NativeValidateBuild()` gegen das laufende Image erfolgreich ist | Telemetrie `plugin.build.invalid`; der Host lässt die Sitzung mit `OL_E_BUILD_VALIDATION_FAILED` fehlschlagen; kein nativer Hook wird aktiviert; OMSI wird beendet und die Transaktion wiederhergestellt. |

Da der Hash der Programmdatei mit den Größen und Bytes der profilierten globalen Variablen abgeglichen wird, ist die Prüfung im Prozess die letzte Verteidigungslinie gegen eine Kopie, die die Hash-Prüfung bestanden hat, deren Image sich zur Ladezeit aber unterscheidet. Es gibt kein Ersatzprofil und keinen heuristischen Abgleich.

<a id="operating-system-and-architecture"></a>
## Betriebssystem und Architektur

`CurrentWindowsX64Platform.Detect` berechnet `RuntimePlatformInfo`. Die aktuelle Plattform wird nur unterstützt, wenn alle folgenden Bedingungen erfüllt sind:

| Voraussetzung | Prüfung | Fehler bei Verletzung |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 oder neuer | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64-Bit-Windows und ein 64-Bit-Hostprozess | `OSArchitecture == X64` und `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Beschreibbare Installation | Das Stammverzeichnis existiert, ist nicht schreibgeschützt und enthält `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` meldet außerdem `OmsiArchitecture` und `PluginArchitecture` als `X86` (OMSI ist ein 32-Bit-Prozess; die Plugin-Gesamtheit (Closure) ist x86 und läuft unter WOW64), `LegacyPlatform=false` sowie `Wow64Available`. ARM64-Windows wird nicht unterstützt, selbst wenn eine x64-Emulation vorhanden ist, da der Hostprozess selbst x64 sein muss.

<a id="net-requirements"></a>
## .NET-Anforderungen

| Komponente | Runtime | Hinweise |
| --- | --- | --- |
| Controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | Der native Bootstrapper findet die Runtime über `hostfxr` mithilfe der mitgelieferten `nethost.dll`. Eine fehlende Runtime wird vom Shim gemeldet (Exitcodes 100-106; siehe [CLI](cli.md) und [Exitcodes](exit-codes.md)). |
| Plugin-Closure (`plugins\OmsiLaunch.Plugin.dll` über `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), gehostet von DNNE 2.0.6 innerhalb von `Omsi.exe` | Erfordert, dass die x86-.NET-6-Desktop-/Core-Runtime auf dem Rechner installiert ist; die 64-Bit-Runtime allein reicht für das Plugin nicht aus. |
| Native Bridge (`plugins\OmsiLaunch.Native.x86.dll`) | natives x86 | Wird nur aus `plugins\` geladen (siehe [permanentes Plugin](../concepts/permanent-plugin.md)). |

<a id="legacy-platforms"></a>
## Legacy-Plattformen

Windows 7, Windows 8.x, Windows XP und andere Systeme mit NT 6 und älter liegen außerhalb der aktuellen Supportgrenze. `RuntimePlatformInfo.LegacyPlatform` ist immer `false`, und es existiert kein Legacy-Adapter; das Feld und die `IPluginNativeServices`-Schnittstelle existieren nur, damit ein künftiger Legacy-Adapter ohne Änderung der öffentlichen API hinzugefügt werden könnte (siehe `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nichts in diesem Release läuft auf diesen Systemen.

<a id="steam-and-large-address-aware-notes"></a>
## Hinweise zu Steam und Large Address Aware

- Die Steam-Distribution von OMSI 2.3.004 mit dem LAA-Header (`7DAB063D...`) steht auf der Allowlist, weil ihre profilierten Adressen mit denen der primären Programmdatei identisch sind. Bis eine Feldvalidierungssitzung in der Matrix erfasst ist, behandeln Sie jede Capability mit dieser Datei als `PARTIAL`.
- Steam startet OMSI selbst; eine Sitzung muss über `OmsiLaunch.exe` gestartet werden, damit die Übergabe existiert. Beim Start über Steam bleibt das permanente Plugin inaktiv (keine Übergabe, keine Hooks).
- Wird ein anderer LAA-Patcher auf `Omsi.exe` angewendet, ändert sich deren Hash, und sie wird zu einem unbekannten Build.

<a id="related-pages"></a>
## Verwandte Seiten

- [Bekannte Einschränkungen](known-limitations.md)
- [Status der Runtime-Validierung](../status/runtime-validation-status.md)
- [Installation](../getting-started/installation.md)
