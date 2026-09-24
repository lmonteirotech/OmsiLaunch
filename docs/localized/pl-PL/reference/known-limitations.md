# Znane ograniczenia

<!-- l10n: source=reference/known-limitations.md -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/known-limitations.md) dla OmsiLaunch 0.1.0-beta3. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona wymienia, na podstawie kodu, wszystko w OmsiLaunch 0.1.0-beta3, co jest `UNAVAILABLE`, `PARTIAL` lub stanowi zaakceptowane ryzyko, tak aby użytkownicy i integratorzy nie opierali się na zachowaniu, którego produkt nie zapewnia. Każdy wiersz podaje ograniczenie, jego stabilność, przyczynę oraz miejsce, w którym zostało szczegółowo udokumentowane. Dokumentacja angielska jest normatywna; zlokalizowane kopie w `docs/localized/` nie są utrzymywane na tym samym poziomie i mogą być nieaktualne (zob. ostatnią sekcję).

<a id="compatibility"></a>
## Zgodność

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Obsługiwany jest wyłącznie `Omsi23004_692EBFBF` (`692EBFBF...6243`); skrót Steam LAA `7DAB063D...D759` znajduje się na liście dozwolonych; jego odcisk i plan zweryfikowano na kontrolowanej kopii, ale rozgrywka wymaga autentycznej instalacji Steam (obraz jest plikiem wykonywalnym Steam chronionym DRM) | `PARTIAL` dla Steam LAA | [zgodność](compatibility.md) |
| Tylko Windows 10+ x64; brak Windows 7/8, brak XP, brak ARM64 | `UNAVAILABLE` | [zgodność](compatibility.md) |
| Wtyczka wymaga runtime .NET 6 w wersji **x86** oprócz runtime x64 używanego przez kontroler | | [zgodność](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Uruchamianie świata i opcje uruchomienia

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last`, `WorldMode.LastMapState`) nie jest zaimplementowany; nigdy nie jest podstawiany zastępczy plik `.osn` wybrany na podstawie znacznika czasu | `UNAVAILABLE` (BI-006) | Komunikat diagnostyczny planu `OL_E_CAPABILITY_UNAVAILABLE` |
| Jawna lub systemowa data, godzina i rok (`/date`, `/time`, `/year`, profil `new.date`/`new.time`/`new.year`, `DateSpec`/`TimeSpec`/`YearSpec`) są przenoszone w specyfikacji, ale sprawiają, że plan jest niemożliwy do uruchomienia; wtyczka odrzuca tryby inne niż `Unset` | `UNAVAILABLE` (`STATICALLY_PARTIAL`) | [profile sesji](session-profiles.md), [launchspec](launchspec.md) |
| Ustawienie wstępne pogody, ICAO i bieżąca rzeczywista pogoda przy starcie (`/weather*`, `new.weather`) | `UNAVAILABLE` (`STATICALLY_PARTIAL`, BI-003) | jak wyżej |
| Model pojazdu gracza, malowanie, HOF, numer taborowy i numer rejestracyjny przy starcie (rodzina `/vehicle`, `PlayerVehicleSpec`) są rozwiązywane względem katalogu zawartości, ale nie są stosowane; ich żądanie sprawia, że plan jest niemożliwy do uruchomienia; deterministyczne przypisanie PlayerVehicle bez interakcji (headless) jest przyszłym rozszerzeniem | `UNAVAILABLE` (BI-007) | `player.assign-headless` w sekcji [możliwości](capabilities.md) |
| Punkt wejścia według tożsamości (`/entrypoint:<identity>`) nie jest korelowany z listą prezentowaną przez OMSI; należy używać `/entrypoint-index` | `PARTIAL` (BI-001) | możliwość planu `world.entrypoint-identity` = `RUNTIME_PARTIAL` |
| Nakładki (overlay) dokumentów klawiatury i kontrolerów (`InputSpec`, `Environment.Keyboard`, `Environment.Controllers`) są parsowane, ale nigdy nie są stosowane przez sesję | `UNAVAILABLE` (BI-005) | możliwości `input.*` |
| `LaunchBehaviorSpec.RestoreConfiguration` i `InstallationSpec.ExpectedExecutableSha256` są zadeklarowane, ale nigdy nie są odczytywane | `UNAVAILABLE` | [launchspec](launchspec.md) |
| `ShutdownTimeoutSeconds` (`/shutdown-timeout`, profil `shutdown-timeout`) jest akceptowany i przenoszony, ale nie jest wykorzystywany przez nadzorcę | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [cykl życia sesji](../concepts/session-lifecycle.md) |
| `/quiet` i `/serve` | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT | [CLI](cli.md) |
| Flagi diagnostyki (`/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native`) wypełniają `DiagnosticsSpec`; widoczny efekt ogranicza się do śladu hosta w `.omsilaunch\diagnostics` | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` są harnessami walidacyjnymi | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Zakończenie sesji i sterowanie procesem

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Zatrzymanie sesji jest wymuszonym zakończeniem: `session.stop`, „End session” w obszarze powiadomień, Ctrl+C i `CloseAsync` prowadzą do `TerminateProcess`. Procedura zamykania OMSI nie jest wykonywana, OMSI przy wyjściu nie zapisuje ponownie `options.cfg` ani swoich logów, a każdy niezapisany stan OMSI zostaje utracony. Jest to celowe: zapobiega nadpisaniu przywróconych plików przez OMSI. | zgodnie z projektem | [cykl życia sesji](../concepts/session-lifecycle.md) |
| Kooperacyjne zamykanie przez WM_CLOSE z limitem czasu i awaryjnym wymuszonym zakończeniem nie jest zaimplementowane | `UNAVAILABLE` (decyzja produktowa, S-11; w rundzie domknięcia runtime OMSI zignorował `WM_CLOSE` wysłane do jego głównego okna) | [stan weryfikacji w runtime](../status/runtime-validation-status.md) |
| Przy zamknięciu konsoli lub wylogowaniu właściciel ma budżet 4 s na zatrzymanie i przywrócenie; wszystko, co pozostanie, jest odzyskiwane z dziennika przy następnym uruchomieniu | zamknięcie konsoli zweryfikowane w runtime; wylogowania nie sprawdzano | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transakcja, odzyskiwanie i dzierżawa

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Dzierżawa instalacji jest semaforem `Local\`: jeden właściciel na instalację **w obrębie sesji logowania**; nie jest egzekwowana między użytkownikami; nie jest zwalniana, dopóki inny proces trzyma uchwyt; dowolny proces tego samego użytkownika może zająć tę nazwę | zaakceptowane ryzyko (S-18) | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |
| Odzyskiwanie jest odrzucane (`OL_E_INSTALLATION_BUSY`), dopóki działa proces OMSI zapisany w dzienniku lub – w przypadku dziennika bez PID – jakikolwiek `Omsi.exe` z tego katalogu głównego | zgodnie z projektem | jak wyżej |
| Pierwotnie nieobecna ścieżka nakładki, której zawartość zmieniła się podczas sesji, blokuje przywracanie (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) do czasu jej sprawdzenia | zgodnie z projektem | jak wyżej |
| Dzienniki sprzed wprowadzenia odcisków własności może zamknąć tylko sesja z identycznymi zaplanowanymi bajtami (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | jak wyżej |
| Przywracane są tylko ścieżki należące do sesji. Własne zapisy OMSI podczas sesji (`options.cfg` `[last_map]`, gdy żadne ustawienie nie nakłada zmian na `options.cfg`, `Texture\standard.ipr`, pamięci podręczne, `laststn.osn`, profil kierowcy, logi) pozostają, tak jak po bezpośrednim uruchomieniu OMSI | zgodnie z projektem | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |
| Usunięcie nieaktualnego `closecheck` przed sesją jest trwałe (zapisywane, nieprzywracane), gdy `SuppressStaleClosecheckWarning` ma wartość true | zgodnie z projektem | jak wyżej |

<a id="runtime-control"></a>
## Sterowanie runtime

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| `weather.set` jest odrzucane (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`): OMSI nadpisuje obu profilowanych kandydatów wiatru przy następnym takcie pogody | `UNAVAILABLE` | [możliwości](capabilities.md) |
| Zapisy kalendarza (`SetActualDateTime`) | `UNAVAILABLE` (BI-002) | `calendar.set-actual-date-time` |
| Zapisy zmiennych tekstowych, nazwane wyzwalacze, wyzwalacze dźwięku (własność zarządzanych ciągów Delphi) | `UNAVAILABLE` (BI-004) | `scripts.string.read` jest tylko do odczytu |
| Brak przenoszenia pojazdów, brak ponownego wiązania przestrzennego między kafelkami, brak bezpiecznego względem ODE uprawnienia do transformacji; pola pozycji są tylko do odczytu | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` wymagają PlayerVehicle; uruchomienie NEW_MAP bez interakcji go nie zapewnia (zapewnia go zapisana sytuacja) | zgodnie z projektem (BI-007) | RV-004 |
| Mutacje runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, tekstury D3D) nie są zapisywane w dzienniku ani przywracane | zgodnie z projektem | [sterowanie runtime](runtime-control.md) |
| Martwy punkt odcisku uchwytu: obiekt tej samej klasy i definicji utworzony ponownie pod tym samym adresem między dwoma odczytami listy nie jest wykrywany jako nieaktualny; czas życia przy naturalnym usunięciu (RV-002) nie ma bezpiecznego źródła w runtime i pozostaje tylko offline | `PARTIAL` | [sterowanie runtime](runtime-control.md) |
| Wyniki są ograniczone skrzynką 64 KiB: długie listy są obcinane (`truncated=true`); dane pikseli są ograniczone do 48 KiB na `d3d.texture.update` | zgodnie z projektem | [możliwości](capabilities.md) |
| Kanał jednożądaniowy: jedno żądanie naraz na sesję; zajęty slot daje `OL_E_RUNTIME_CHANNEL_BUSY`; identyfikatorów żądań nie wolno używać ponownie | zgodnie z projektem | [sterowanie runtime](runtime-control.md) |
| Telemetria jest slotem ostatniej wartości: serie szybsze niż próbkowanie hosta co 100 ms mogą gubić zdarzenia pośrednie (numery sekwencji odróżniają identyczne kolejne zdarzenia; rozerwane próbki są pomijane) | `PARTIAL` | [stała wtyczka](../concepts/permanent-plugin.md) |
| Reset urządzenia D3D zaobserwowano w runtime (`resetting`, `restored`, unieważnienie generacji); odrębne przejście `lost` nie wystąpiło, ponieważ urządzenie OMSI przeszło bezpośrednio do `DEVICENOTRESET` | `PARTIAL` (RV-007) | [stan weryfikacji w runtime](../status/runtime-validation-status.md) |
| Wyniki list ograniczonych (operacje `timetable.*.list`, `vehicle.variables.list`, `vehicle.string-variables.list`) zwracają co najwyżej tyle wierszy, ile mieści się w slocie runtime 64 KiB; pozostałe są pomijane z `truncated=true` i mniejszą wartością `returned_count` (audyt dokumentacji BUG-05). W tym wydaniu nie ma stronicowania | zgodnie z projektem | [możliwości](capabilities.md) |
| `timetable.logs.read`, `road-vehicles.list`, `humans.list`, `vehicle.constants.list` i `vehicle.curves.list` nie są ograniczone: wynik większy niż slot kończy się błędem `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (na testowanych mapach nie zaobserwowano tego dla żadnej z nich) | `PARTIAL` | [możliwości](capabilities.md) |
| Samodzielnie raportowane ciągi dowodów (`PublicCapabilityRegistry` `RuntimeValidation`, `GetCapabilitiesAsync` `EvidenceState`) nie zostały zaktualizowane po rundzie domknięcia runtime: `camera.lock` nadal podaje `STATICALLY_VALIDATED`, a `runtime.d3d.lifecycle.reset` – `IMPLEMENTED_NOT_RUNTIME_VALIDATED`. Rozstrzygająca jest strona [stan weryfikacji w runtime](../status/runtime-validation-status.md) | opóźnienie dokumentacji, nie różnica w zachowaniu | [możliwości](capabilities.md) |
| Niektóre zaawansowane pola grafu map/kafelków/ścieżek/obiektów nie są udostępniane; czytniki runtime to typowane migawki zależne od profilu, nigdy dowolny dostęp do pamięci | zgodnie z projektem | [możliwości](capabilities.md) |
| Odczyty pamięci w procesie działają na zasadzie „sprawdź, potem użyj” wobec działającego OMSI; równoczesna zmiana w OMSI między sprawdzeniem a odczytem może dać niespójną migawkę (`OL_E_RUNTIME_OPERATION_FAILED`) | zaakceptowane ryzyko (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Lokalna płaszczyzna sterowania i model zaufania

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Model zaufania tego samego użytkownika: potok nazwany (`CurrentUserOnly`), mapowania pamięci handoff/telemetrii/runtime oraz semafor dzierżawy są dostępne dla każdego procesu tego samego użytkownika Windows. Taki proces może odczytać stan, zatrzymać sesję lub wykonywać operacje runtime, gdy tylko odczyta `session_id`. | zaakceptowane ryzyko (S-06, S-30) | [lokalne sterowanie](local-control.md) |
| Punkt końcowy sterowania istnieje tylko wtedy, gdy właściciel jest w stanie `Running`; klient otrzymuje `OL_E_NO_ACTIVE_SESSION` (kod wyjścia 4) podczas uruchamiania i po zakończeniu sesji | zgodnie z projektem | [lokalne sterowanie](local-control.md) |
| Jeśli inny proces jest już właścicielem nazwy potoku, właściciel sesji działa dalej bez punktu końcowego (`ListenFault`), a drugie uruchomienie może błędnie zgłosić `OL_E_SESSION_ALREADY_ACTIVE` | zaakceptowane ryzyko | [lokalne sterowanie](local-control.md) |
| `.omsilaunch\` dziedziczy listę ACL katalogu głównego OMSI; nie jest stosowana żadna jawna kontrola dostępu | zaakceptowane ryzyko (S-31) | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnostyka i dane wyjściowe

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Diagnostyka to wyłącznie pliki lokalne (`.omsilaunch\diagnostics`); nic nie jest wysyłane i nie ma zdalnego raportowania | zgodnie z projektem | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |
| Retencja obejmuje 50 najnowszych sesji; starsza diagnostyka z prefiksem sesji jest usuwana przy starcie nowej sesji | zgodnie z projektem | jak wyżej |
| Dane wyjściowe JSON i diagnostyka zawierają ścieżki instalacji (`RootPath`, katalogi zasobów, ścieżki `.itx`) | zgodnie z projektem (dane lokalne) | |
| Okno dialogowe błędu sesji w `OmsiLaunchW.exe` pokazuje jako komunikat ładunek błędu wtyczki (na przykład `{"name":"world.failed",...}`) zamiast zdania; wiersz `Code:` jest poprawny | kosmetyczne | [obszar powiadomień Windows](windows-tray.md) |
| Żądanie D3D odrzucone przez mostek natywny przed jakimkolwiek wywołaniem Direct3D poprawnie raportuje `native_status`, ale jego tekst `detail` podaje `HRESULT 0x00000000` | kosmetyczne | [możliwości](capabilities.md) |
| Okno stanu w obszarze powiadomień jest migawką zaplanowanej sesji wykonaną w chwili jego otwarcia; nie odświeża się i nie pokazuje bieżących wartości OMSI | zgodnie z projektem | [obszar powiadomień Windows](windows-tray.md) |

<a id="documentation"></a>
## Dokumentacja

Angielskie strony w `docs/` są dokumentacją normatywną tego wydania. `docs/localized/<locale>/` zawiera tłumaczenia tych samych stron `0.1.0-beta3` (zob. [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); tam, gdzie tłumaczenie różni się od tekstu angielskiego, rozstrzygające są tekst angielski i kod. Wymienione tam strony historyczne i starsze (legacy) są dostępne tylko po angielsku.

Powiązane: [możliwości](capabilities.md), [stan weryfikacji w runtime](../status/runtime-validation-status.md), [błędy](errors.md).
