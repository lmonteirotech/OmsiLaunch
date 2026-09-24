# OmsiLaunch 0.1.0-beta3 — glosariusz pl-PL

## Forma zwracania się i rejestr

Profesjonalna polska dokumentacja techniczna w rejestrze neutralnym. Preferowane są formy bezosobowe („należy uruchomić”, „można sprawdzić”, „sesja jest zatrzymywana”); tryb rozkazujący 2. osoby liczby pojedynczej („Uruchom…”, „Sprawdź…”) dopuszczalny jest tylko w instrukcjach krok po kroku, konsekwentnie w obrębie strony. Nie używać „Pan/Pani”.

## Typografia i interpunkcja

- Cudzysłowy w prozie: „…” (wewnętrzne: ‚…’ lub »…«). Nigdy nie zmieniać znaków wewnątrz spanów `` `...` `` ani bloków kodu.
- Myślnik: półpauza ze spacjami ( – ) w prozie; łącznik bez spacji w złożeniach („D3D-owy” unikać — pisać „tekstura D3D”).
- Liczby, jednostki, wersje, identyfikatory dowodów i tokeny statusu pozostają dokładnie jak w oryginale (`64 KiB`, `250 ms`, `2 s`, `0.1.0-beta3`, `STABLE_BETA`, `RV-002`) — także separator dziesiętny nie jest zmieniany na przecinek w takich literałach.
- Nagłówki: wielka litera tylko w pierwszym słowie i nazwach własnych („Cykl życia sesji”, nie „Cykl Życia Sesji”).
- Przecinek przed „że”, „który”, „gdy”, „aby” zgodnie z polską interpunkcją; brak przecinka szeregowego (oxford comma) przed „i”/„oraz”/„lub”.
- Nazwy produktów i komponentów nie są odmieniane przez przypadki, jeśli zawierają rozszerzenie lub są w backtickach (`OmsiLaunch.exe`); „OmsiLaunch” i „OMSI” można odmieniać tylko opisowo („w OmsiLaunch”, „procesu OMSI”) — bez apostrofów i końcówek typu „OMSI'ego”.
- „runtime” pozostaje po angielsku i nie jest odmieniany; stosować jako przydawkę („operacja runtime”, „slot runtime”, „w warstwie runtime”). Przysłówkowe „at runtime” = „w czasie działania”.
- Nazwy elementów interfejsu Windows (angielskie literały UI) zawsze w backtickach bez zmian; opcjonalnie tuż po nich polski napis produktu w nawiasie i backtickach, wyłącznie z tabeli „Product UI strings (exact)” poniżej, np. `End session` (`Zakończ sesję`).

## Terminologia

| English | pl-PL | note |
| --- | --- | --- |
| session | sesja | |
| session owner / owner | właściciel sesji / właściciel | „owner mode” = tryb właściciela. |
| client (mode) | klient / tryb klienta | „client invocation” = wywołanie w trybie klienta. |
| owner process | proces właściciela | |
| launch | uruchomienie (rzecz.), uruchamiać / uruchomić (czas.) | „launch flag” = flaga uruchomienia; „launch-time” = etap uruchamiania. |
| plan (noun/verb) | plan / planować, zaplanować | „planning” = planowanie; typ `SessionPlan` bez zmian. |
| runnable / not runnable | możliwy do uruchomienia / niemożliwy do uruchomienia | Nie używać „wykonywalny” (koliduje z „plik wykonywalny”). |
| start | uruchomienie, start (sesji, OMSI) / uruchomić | „startup” = uruchamianie; „startup timeout” = limit czasu uruchamiania. |
| stop | zatrzymanie / zatrzymać | Przy stanach i operacjach: „żądanie zatrzymania”. |
| end session | zakończenie sesji / zakończyć sesję | Napis UI: `End session` (`Zakończ sesję`). |
| canonical stop | kanoniczne zatrzymanie (kanoniczna ścieżka zatrzymania) | Ta sama ścieżka co `session stop`. |
| restore | przywracanie / przywrócić | „exact restore” = dokładne przywrócenie. |
| recovery | odzyskiwanie | „recover” = odzyskać; nie mylić z „restore”. |
| pending (journal) | oczekujący (dziennik), oczekująca transakcja | Obecność dziennika = „transakcja oczekuje”. |
| journal | dziennik (transakcji) | Plik `journal.json` bez zmian; „journaled” = zapisywany w dzienniku. |
| transaction | transakcja | „configuration transaction” = transakcja konfiguracji. |
| overlay | nakładka | Przy pierwszym użyciu na stronie można dodać „(overlay)”; termin ustalony we wcześniejszym tłumaczeniu. |
| backup | kopia zapasowa | Katalog `backup\<session>` bez zmian. |
| snapshot | migawka | Przy pierwszym użyciu można dodać „(snapshot)”; „read-only snapshot” = migawka tylko do odczytu. |
| installation | instalacja | Instalacja OMSI. |
| installation root | katalog główny instalacji | Placeholder `<root>` bez zmian. |
| package | pakiet | |
| release package | pakiet wydania | |
| manifest | manifest | Zostaje po angielsku (termin przyjęty); `release-manifest.json` bez zmian. |
| permanent plugin | stała wtyczka | „plugin” = wtyczka; „plugin closure” = zestaw plików wtyczki (closure). |
| native bridge | mostek natywny | |
| runtime | runtime | Po angielsku, nieodmieniany (patrz typografia). |
| runtime operation | operacja runtime | Identyfikatory operacji w backtickach bez zmian. |
| runtime command | polecenie runtime | |
| runtime slot / mailbox | slot runtime / skrzynka runtime (mailbox) | Slot 64 KiB; „telemetry slot” = slot telemetrii. |
| control plane | płaszczyzna sterowania | „local control plane” = lokalna płaszczyzna sterowania. |
| local control | lokalne sterowanie | „control endpoint” = punkt końcowy sterowania. |
| named pipe | potok nazwany (named pipe) | „pipe” = potok. |
| frame (protocol) | ramka | „framed” = ramkowany. |
| envelope (JSON) | koperta JSON (envelope) | „output envelope” = koperta wyjściowa. |
| handle | uchwyt | Wartości typu `rv-000001` bez zmian. |
| stale handle | nieaktualny uchwyt | Nie „przestarzały” (to jest „deprecated”). |
| capability | możliwość | „capability id” = identyfikator możliwości; przy pierwszym użyciu można dodać „(capability)”. |
| capability registry | rejestr możliwości | `PublicCapabilityRegistry` bez zmian. |
| bounded list | lista ograniczona | Zwraca największy mieszczący się prefiks; to udana odpowiedź, nie błąd (BUG-05). |
| truncated | obcięta (lista) | `truncated=true` = część wierszy pominięto; nie „uszkodzona”. |
| row | wiersz | `returned_count` = liczba zwróconych wierszy. |
| evidence | dowody (dowód) | „runtime evidence” = dowody z runtime; „evidence id” = identyfikator dowodu (`T01`, `CAM01` bez zmian). |
| runtime-validated | zweryfikowany w runtime | Token `RUNTIME_VALIDATED` bez zmian; „not runtime validated” = niezweryfikowany w runtime (ta sama siła). |
| statically validated | zweryfikowany statycznie | Token `STATICALLY_VALIDATED` bez zmian. |
| offline test | test offline | „covered offline” = pokryty testami offline; „offline only” = tylko offline. |
| gate (documentation gate) | bramka (bramka dokumentacji) | |
| tray icon | ikona w obszarze powiadomień | Potocznie „ikona w zasobniku”; w dokumentacji preferować „obszar powiadomień”. |
| notification area | obszar powiadomień | Termin z polskiego Windows. |
| status window | okno stanu | Pozycja menu UI: `Status` (`Stan`). |
| confirmation dialog | okno dialogowe potwierdzenia | |
| message box | okno komunikatu | |
| failure dialog | okno dialogowe błędu | |
| tooltip | etykietka narzędzia | Termin z polskiego Windows; można dodać „(tooltip)”. |
| context menu | menu kontekstowe | |
| Explorer restart | ponowne uruchomienie Eksploratora (Explorer) | Eksplorator Windows / `explorer.exe`. |
| entry point | punkt wejścia | Napis UI `Entry point` (`Punkt wejścia`). |
| new map | nowa mapa | Token `NEW_MAP` bez zmian; UI trybu: `New session` (`Nowa sesja`). |
| saved situation | zapisana sytuacja | Token `SAVED_SITUATION` bez zmian; UI `Saved situation` (`Zapisana sytuacja`). |
| map | mapa | |
| splash screen | ekran startowy | UI `Splash` (`Ekran startowy`); „managed splash” = zarządzany ekran startowy. |
| Internet Textures | tekstury internetowe | UI `Internet textures` (`Tekstury internetowe`). |
| session profile | profil sesji | |
| preset | ustawienie wstępne (preset) | UI pokazuje `Preset` (`Ustawienie`); w prozie „ustawienie wstępne”, by odróżnić od „setting”. |
| setting | ustawienie | `/set:<key>=<value>` bez zmian. |
| player vehicle | pojazd gracza | Typ `PlayerVehicle` bez zmian. |
| road vehicle | pojazd drogowy | Typ `RoadVehicle` bez zmian (także poza backtickami jako nazwa typu). |
| human (pedestrian/passenger object) | postać (obiekt Human: pieszy lub pasażer) | Nazwa typu `Human` bez zmian. |
| timetable | rozkład jazdy | |
| track entry | wpis trasy (track entry) | Pozycja rozkładu dla trasy OMSI. |
| tour entry | wpis brygady (tour entry) | „tour” = brygada (termin komunikacji miejskiej i społeczności OMSI). |
| ticket | bilet | |
| driver | kierowca | „driver profile” = profil kierowcy. |
| fleet number | numer taborowy | UI `Fleet number` (`Numer floty`); w prozie termin branżowy „numer taborowy”. |
| registration (plate) | numer rejestracyjny (tablica rejestracyjna) | UI `Registration` (`Rejestracja`). |
| repaint | malowanie | UI `Repaint` (`Malowanie`). |
| spawn | utworzenie (dodanie) pojazdu / utworzyć, dodać | Trasa `vehicles spawn` bez zmian. |
| camera lock | blokada kamery | Identyfikator `camera.lock` bez zmian. |
| device reset | reset urządzenia (D3D) | „device lost” = utrata urządzenia. |
| render thread | wątek renderowania | „UI thread” = wątek interfejsu użytkownika. |
| texture | tekstura | |
| exit code | kod wyjścia | |
| error code | kod błędu | Kody `OL_E_*` bez zmian. |
| diagnostic | komunikat diagnostyczny (diagnostyka) | „diagnostics” w kopercie = komunikaty diagnostyczne. |
| diagnostics directory | katalog diagnostyki | `.omsilaunch\diagnostics` bez zmian. |
| timeout | limit czasu | Można dodać „(timeout)”; „times out” = przekracza limit czasu. |
| placeholder | symbol zastępczy | Np. `<root>`, `<id>` bez zmian. |
| flag | flaga | Flagi CLI (`/new`, `/json`) bez zmian. |
| route | ścieżka polecenia (hierarchiczna) | CLI route, np. `time get`; „path” (plik) = ścieżka — kontekst rozstrzyga. |
| command word | słowo polecenia | |
| integrator | integrator | Twórca aplikacji korzystającej z API/CLI. |
| caller | wywołujący (kod wywołujący) | |
| known limitation | znane ograniczenie | |
| accepted risk | zaakceptowane ryzyko | |
| stable beta | stabilna beta | Token `STABLE_BETA` bez zmian. |
| experimental | eksperymentalny | Token `EXPERIMENTAL` bez zmian. |
| partial | częściowy | Token `PARTIAL` bez zmian; nie „nieobsługiwany” ani „uszkodzony”. |
| unavailable | niedostępny | Token `UNAVAILABLE` bez zmian. |
| deprecated / legacy | przestarzały / starszy (legacy) | „legacy” = starszy mechanizm zachowany dla zgodności. |
| not normative | nienormatywny (informacyjny) | |
| source of truth | źródło prawdy | Angielska dokumentacja jest jedynym źródłem prawdy. |

## Product UI strings (exact)

Źródło: `tools/OmsiLaunch.Cli/WindowsUiStrings.cs` (słownik `Polish`, używany dla `pl` i `pl-PL`). Kolumny „English” i „Product (pl)” są dokładnymi literałami produktu — nie zmieniać.

| Key | English | Product (pl) |
| --- | --- | --- |
| `Tray.Running` | `OmsiLaunch is running` | `OmsiLaunch jest uruchomiony` |
| `Tray.Status` | `Status` | `Stan` |
| `Tray.EndSession` | `End session` | `Zakończ sesję` |
| `Tray.EndSessionDescription` | `Ends OMSI 2 and the OmsiLaunch session` | `Kończy OMSI 2 i sesję OmsiLaunch` |
| `Status.Title` | `OmsiLaunch session status` | `Stan sesji OmsiLaunch` |
| `Status.SessionRunning` | `Session is running` | `Sesja jest uruchomiona` |
| `Status.Section.Session` | `Session` | `Sesja` |
| `Status.Section.Profile` | `Session profile` | `Profil sesji` |
| `Status.Section.Environment` | `Environment` | `Środowisko` |
| `Status.Section.Vehicle` | `Vehicle` | `Pojazd` |
| `Status.Section.Configuration` | `Configuration` | `Konfiguracja` |
| `Status.Section.Presentation` | `Presentation` | `Prezentacja` |
| `Status.Field.Mode` | `Mode` | `Tryb` |
| `Status.Field.Map` | `Map` | `Mapa` |
| `Status.Field.EntryPoint` | `Entry point` | `Punkt wejścia` |
| `Status.Field.Situation` | `Situation` | `Sytuacja` |
| `Status.Field.SessionProfile` | `Profile` | `Profil` |
| `Status.Field.Preset` | `Preset` | `Ustawienie` |
| `Status.Field.Date` | `Date` | `Data` |
| `Status.Field.Time` | `Time` | `Czas` |
| `Status.Field.Weather` | `Weather` | `Pogoda` |
| `Status.Field.Vehicle` | `Vehicle` | `Pojazd` |
| `Status.Field.Repaint` | `Repaint` | `Malowanie` |
| `Status.Field.Hof` | `HOF` | `HOF` |
| `Status.Field.Fleet` | `Fleet number` | `Numer floty` |
| `Status.Field.Registration` | `Registration` | `Rejestracja` |
| `Status.Field.Splash` | `Splash` | `Ekran startowy` |
| `Status.Field.InternetTextures` | `Internet textures` | `Tekstury internetowe` |
| `Status.Close` | `Close` | `Zamknij` |
| `Mode.NewMap` | `New session` | `Nowa sesja` |
| `Mode.SavedSituation` | `Saved situation` | `Zapisana sytuacja` |
| `Mode.LastMapState` | `Last map state` | `Ostatni stan mapy` |
| `Presentation.Managed` | `Managed` | `Zarządzany` |
| `Presentation.Unset` | `Original OMSI` | `Oryginalny OMSI` |
| `InternetTextures.Native` | `Original OMSI` | `Oryginalny OMSI` |
| `InternetTextures.Disabled` | `Disabled` | `Wyłączone` |
| `InternetTextures.Override` | `Override` | `Zastąpienie` |
| `Stop.Title` | `End session?` | `Zakończyć sesję?` |
| `Stop.Message` | `OMSI 2 will be closed and the OmsiLaunch managed session will end.` | `OMSI 2 zostanie zamknięty, a zarządzana sesja OmsiLaunch zostanie zakończona.` |
| `Stop.Confirm` | `End session` | `Zakończ sesję` |
| `Stop.Cancel` | `Cancel` | `Anuluj` |
| `Stop.Failed` | `The session could not be ended. OMSI and its managed session remain active.` | `Nie można było zakończyć sesji. OMSI i zarządzana sesja pozostają aktywne.` |
