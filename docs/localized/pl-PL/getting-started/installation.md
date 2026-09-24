# Instalacja

<!-- l10n: source=getting-started/installation.md -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../getting-started/installation.md) dla OmsiLaunch 0.1.0-beta3. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona wyjaśnia, czego wymaga OmsiLaunch `0.1.0-beta3`, który build OMSI jest obsługiwany, jak zainstalować pakiet wydania w katalogu głównym instalacji OMSI oraz jak zweryfikować instalację za pomocą `/version` i `/plan` przed uruchomieniem sesji. Zawartość pakietu jest określona na stronie [pakowanie](../reference/packaging.md); pierwsze uruchomienie opisano na stronie [pierwsza sesja](first-session.md).

<a id="requirements"></a>
## Wymagania

| Wymaganie | Szczegóły | Błąd w razie braku |
|---|---|---|
| Windows 10 lub nowszy, 64-bitowy | Kontroler sprawdza `Environment.OSVersion.Version.Major >= 10`, 64-bitowy (x64) system operacyjny i proces x64. | Plan niemożliwy do uruchomienia z `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (lub `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), wyjście `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` wymaga `Microsoft.NETCore.App` 6.0 i `Microsoft.WindowsDesktop.App` 6.0 (Windows Forms jest używany przez wskaźnik w obszarze powiadomień). Shimy lokalizują go za pomocą `nethost.dll`. | `OmsiLaunch.exe` kończy działanie z kodem shimu `102`..`106` przed wypisaniem czegokolwiek; `OmsiLaunchW.exe` wyświetla `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` wymaga `Microsoft.NETCore.App` 6.0 dla x86, ponieważ wtyczka działa wewnątrz 32-bitowego `Omsi.exe`. Wymaganie spełnia także pakiet .NET 6 Desktop Runtime x86. | Wtyczka nie uruchamia się wewnątrz OMSI; sesja nie osiąga stanu `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), wyjście `1`, pliki przywrócone. |
| Obsługiwany build OMSI 2 | `Omsi.exe` z SHA-256 `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` (8,503,440 bajtów), profil `Omsi23004_692EBFBF`, zweryfikowany w runtime. Plik wykonywalny Steam LAA `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` jest akceptowany, ale jego stan weryfikacji to `pending_beta_field_validation`. Hash jest ponownie sprawdzany przy każdym planowaniu i każdym uruchomieniu. | `OL_E_UNSUPPORTED_BUILD`; plan niemożliwy do uruchomienia, wyjście `1`. Zob. [zgodność](../reference/compatibility.md). |
| Katalog główny instalacji z prawem zapisu | Transakcja zapisuje `.omsilaunch\`, nakładki w `GUI\`, `Texture\` i `options.cfg`, a następnie je przywraca; bieżący użytkownik musi mieć prawo zapisu w katalogu głównym (należy unikać `Program Files` bez odpowiednich uprawnień). | `OL_E_INSTALLATION_NOT_WRITABLE`, wyjście `1`. |
| Jeden użytkownik, jeden właściciel na instalację | Dzierżawa instalacji `Local\OmsiLaunch.Installation.<sha256(root)>` i potok sterowania są przypisane do sesji logowania. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, wyjście `7`. |

Oba środowiska runtime są osobnymi plikami do pobrania od firmy Microsoft; należy zainstalować .NET 6 Desktop Runtime x64 oraz Runtime x86 (lub Desktop Runtime x86). Żaden inny składnik nie jest wymagany. Żadne dane nie opuszczają komputera.

<a id="confirm-the-omsi-build"></a>
## Potwierdzenie buildu OMSI

Należy zastąpić `<OMSI_PATH>` katalogiem OMSI 2 (na przykład `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

Hash musi być jednym z dwóch podanych powyżej. Po instalacji `OmsiLaunch.exe profiles` wypisuje tę samą listę wraz ze stanem weryfikacji.

<a id="install-the-package"></a>
## Instalacja pakietu

1. Pobierz `OmsiLaunch-0.1.0-beta3.zip` i `OmsiLaunch-0.1.0-beta3.zip.sha256`; zweryfikuj sumę kontrolną (wynik `Get-FileHash` musi być równy wartości w pliku `.sha256`).
2. Rozpakuj archiwum **bezpośrednio do katalogu głównego instalacji OMSI** (folderu zawierającego `Omsi.exe`). Układ archiwum odpowiada temu katalogowi:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` i pozostałe zestawy kontrolera `OmsiLaunch.*.dll`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` w katalogu głównym;
   - zestaw plików stałej wtyczki `plugins\OmsiLaunch.*` (9 plików) obok istniejących wtyczek, które nigdy nie są modyfikowane;
   - `.omsilaunch\` z zasobami ekranu startowego, dokumentacją offline i przykładami.
3. Pozostaw `release-manifest.json` obok `OmsiLaunch.exe`. To on pozwala każdemu uruchomieniu zweryfikować zainstalowane pliki wtyczki za pomocą SHA-256 (`plugin.integrity.reference = manifest`); bez niego sprawdzana jest tylko obecność i wewnętrzna spójność (`plugin.integrity.reference = self`).
4. Nie przenoś ani nie zmieniaj nazw niczego w `plugins\OmsiLaunch.*` i nie umieszczaj plików binarnych wtyczki w `.omsilaunch\`.

Aktualizacja to ta sama operacja: nowy pakiet należy rozpakować na stare pliki, gdy żadna sesja nie działa i żadne odzyskiwanie nie oczekuje (`OmsiLaunch.exe /recovery-status`). Hashe wtyczki i manifest muszą zawsze pochodzić z tego samego pakietu (w przeciwnym razie `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Weryfikacja

Polecenia należy uruchamiać z katalogu głównego OMSI (argument instalacji domyślnie wskazuje katalog zawierający `OmsiLaunch.exe`):

```text
OmsiLaunch.exe /version
```
Oczekiwany wynik: `"version": "0.1.0-beta3"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2_3_004_COMMON"`; wyjście `0`. Wyjście `102`..`106` oznacza, że brakuje środowiska .NET 6 runtime x64 lub pakiet jest niekompletny.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Oczekiwany wynik: obsługiwane hashe, a następnie mapy wykryte w tej instalacji; wyjście `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Oczekiwany wynik: `Plan: READY profile=Omsi23004_692EBFBF` i wyjście `0` (można użyć dowolnej tożsamości mapy z `/list:Maps`; indeks punktu wejścia musi być prezentowanym indeksem tej mapy, zob. `/list:Entrypoints /map:<identity>`). Z `--json` plan wymienia `TouchedFiles` (nakładki ekranu startowego), `PlannedMutations`, `RequiredCapabilities` (wszystkie `STATICALLY_VALIDATED`), `Diagnostics` (w tym `plugin.integrity.reference`) oraz `IsRunnable`. `Plan: NOT RUNNABLE` z wyjściem `1` podaje przyczynę w `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). Planowanie nigdy nie uruchamia OMSI i nigdy nie zapisuje niczego w instalacji.

<a id="where-things-live-afterwards"></a>
## Gdzie co się znajduje po instalacji

| Ścieżka | Zawartość |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Ślad hosta każdej sesji (przechowywanych jest 50 najnowszych sesji) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Log wskaźnika w obszarze powiadomień |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Obecne tylko wtedy, gdy transakcja oczekuje; zob. [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Własne predefiniowane profile sesji; zob. [profile sesji](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Zasoby zarządzanego ekranu startowego |
| `<root>\.omsilaunch\docs\` | Ta dokumentacja w wersji offline (zacząć od `README.md`; dokumentacja CLI to `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | Przykładowy LaunchSpec i profil sesji |

<a id="uninstall"></a>
## Odinstalowanie

Należy zatrzymać każdą sesję, uruchomić `OmsiLaunch.exe /recovery-status` (oraz `/recover`, jeśli transakcja oczekuje), a następnie usunąć pliki produktu z katalogu głównego, `plugins\OmsiLaunch.*` i `.omsilaunch\`. Szczegóły na stronie [pakowanie](../reference/packaging.md).

<a id="next"></a>
## Dalej

[Pierwsza sesja](first-session.md) · [Dokumentacja CLI](../reference/cli.md) · [znane ograniczenia](../reference/known-limitations.md)
