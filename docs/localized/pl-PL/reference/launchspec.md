# Dokumentacja LaunchSpec

<!-- l10n: source=reference/launchspec.md -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/launchspec.md) dla OmsiLaunch 0.1.0-beta3. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona jest normatywną dokumentacją referencyjną rekordu `LaunchSpec` – rekordu żądania opisującego jedną sesję OmsiLaunch. Opisuje jego kształt w C# (`OmsiLaunch.Api`), postać pliku JSON wczytywanego przez CLI (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), każdą właściwość wraz z typem, wartością domyślną, regułą walidacji i bieżącym działaniem, reguły walidacji, które czynią plan niemożliwym do uruchomienia, oraz pierwszeństwo między flagami CLI, plikami specyfikacji i profilami sesji. Udokumentowano wyłącznie to, co robi bieżący kod.

Powiązane strony: [publiczne API](public-api.md), [dokumentacja CLI](cli.md), [profile sesji](session-profiles.md), [kody błędów](errors.md), [cykl życia sesji](../concepts/session-lifecycle.md), [możliwości](capabilities.md).

<a id="where-a-launchspec-comes-from"></a>
## Skąd pochodzi LaunchSpec

| Źródło | Jak powstaje `LaunchSpec` |
| --- | --- |
| API | Integrator tworzy rekord i przekazuje go do `PlanSessionAsync`. |
| Flagi CLI | `CliInput.BuildSpecAsync` zaczyna od wbudowanych wartości domyślnych (NEW_MAP, wszystko nieustawione, wartości domyślne `Behavior`) i stosuje flagi. |
| Plik JSON `/spec:<path>` | Wczytywany przez `LaunchSpecJson.LoadAsync`, a następnie używany jako punkt wyjścia, który flagi CLI nadpisują (zob. [pierwszeństwo](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Profil sesji (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` zapisuje w punkcie wyjścia świat, ustawienia, prezentację, tekstury internetowe i zachowanie z profilu oraz rejestruje metadane `SessionProfile`. |

Poniższy [kompletny przykład](#complete-example) jest zweryfikowany względem kształtu rekordu. Kopia minimalnego przykładu jest dostarczana jako `examples/release-session.example.json` (oraz `.omsilaunch\examples\release-session.example.json` w pakiecie wydania).

<a id="json-form"></a>
## Postać JSON

| Reguła | Szczegóły |
| --- | --- |
| Serializator | `System.Text.Json` z `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; nie są zarejestrowane żadne konwertery. |
| Nazwy właściwości | Nazwy właściwości C# (`Installation`, `RootPath`, ...). Przy wczytywaniu dopasowanie nie uwzględnia wielkości liter; CLI zapisuje je w notacji PascalCase. |
| Wyliczenia | Liczby całkowite (brak konwertera wyliczeń na ciągi znaków). `"Mode": 0` jest poprawne; `"Mode": "NewMap"` jest odrzucane jako źle sformowany JSON. Wartości wymieniono w sekcji [Wyliczenia](#enumerations). |
| `OptionalValue<T>` | Obiekt `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (wartość jest ignorowana), 1 = `Set` (wartość musi być obecna i różna od null; `Set` z wartością null nie jest walidowane i zachowuje się jak nieprawidłowa wartość). Pominięty element `OptionalValue` oznacza `Unset`. Element tylko do odczytu `IsSet` pojawia się w danych wyjściowych zapisywanych przez CLI i jest przy wczytywaniu akceptowany oraz ignorowany. |
| Rekordy opcjonalne | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile` mogą mieć wartość `null` lub zostać pominięte; akcesory `Effective*` podstawiają wartości domyślne. |
| Rekordy wymagane | `Installation`, `World`, `Date`, `Time`, `Environment` (ze wszystkimi ośmioma słownikami, należy użyć `{}`), `Behavior` muszą być obecnymi obiektami. Nie są walidowane: wartość `null` lub brak rekordu powoduje później błąd odwołania do null, który CLI zgłasza jako `OL_E_INTERNAL` (kod wyjścia 10) lub `OL_E_INVALID_ARGUMENT` (kod wyjścia 2). |
| Nieznane właściwości | Odrzucane przed powiązaniem: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (ścieżka używa nazw elementów w postaci zapisanej w pliku). Zawartość słowników (`Environment.*`) nie jest sprawdzana jako właściwości. |
| Korzeń | Musi być obiektem JSON: `OL_E_SPEC_INVALID`. Maksymalna głębokość zagnieżdżenia 32. |
| Rozmiar pliku | Najwyżej 1 MiB (1 048 576 bajtów): `OL_E_SPEC_TOO_LARGE`. Brak pliku: `OL_E_SPEC_NOT_FOUND`. |
| Komentarze i końcowe przecinki | Komentarze `//` i `/* */` oraz końcowe przecinki są akceptowane. |
| Źle sformowany JSON | Wyjątek parsera nie jest tłumaczony: CLI zgłasza `OL_E_INTERNAL` z kodem wyjścia 10. |
| Kodowanie | UTF-8 (czytnik toleruje BOM). Ukośniki odwrotne w identyfikatorach muszą być poprzedzone znakiem ucieczki (`"maps\\Grundorf\\global.cfg"`); w identyfikatorach map, sytuacji, pojazdów i plików HOF akceptowane są ukośniki zwykłe. |

<a id="complete-example"></a>
## Kompletny przykład

```jsonc
{
  // Comments and trailing commas are accepted. Enums are integers.
  "Installation": {
    "RootPath": ".",                          // "." = directory that contains OmsiLaunch.exe (CLI only)
    "ExpectedExecutableSha256": null          // carried, not consumed
  },
  "World": {
    "Mode": 0,                                // 0 NewMap, 1 SavedSituation, 2 LastMapState (unavailable)
    "MapIdentity": { "Presence": 1, "Value": "maps\\Grundorf\\global.cfg" },
    "SituationIdentity": { "Presence": 0, "Value": null },
    "PresentedEntrypointIndex": { "Presence": 1, "Value": 1 },
    "EntrypointIdentity": { "Presence": 0, "Value": null }
  },
  "Date": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Time": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Year": null,
  "Weather": null,
  "PlayerVehicle": { "Presence": 0, "Value": null },
  "Environment": {
    "General": {
      "traffic.randomVehicles": { "Presence": 1, "Value": "150" },
      "graphics.maxFPS": { "Presence": 1, "Value": "60" }
    },
    "Advanced": {}, "Graphics": {}, "AdvancedGraphics": {},
    "Sound": {}, "AiPassengers": {}, "Keyboard": {}, "Controllers": {}
  },
  "Behavior": {
    "RestoreConfiguration": true,             // carried, restore always happens
    "SuppressStaleClosecheckWarning": true,
    "StartupTimeoutSeconds": 180,             // 1..600
    "ShutdownTimeoutSeconds": 30              // carried, not consumed
  },
  "Input": null,
  "Diagnostics": null,
  "Presentation": {
    "Splash": 1,                              // 0 Unset/Native (keep OMSI files), 1 Managed
    "Language": { "Presence": 0, "Value": null },
    "CustomAssetDirectory": { "Presence": 0, "Value": null },
    "SuppressTrayIcon": false
  },
  "InternetTextures": {
    "Mode": 0,                                // 0 Native, 1 Disabled, 2 Override
    "OverrideProfilePath": { "Presence": 0, "Value": null }
  },
  "SessionProfile": null
}
```

Jawną datę, gdy kompilacja (build) ją obsługuje, zapisuje się jako `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }`, a czas jako `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. W tym buildzie obie czynią plan niemożliwym do uruchomienia (zob. niżej).

<a id="property-reference"></a>
## Dokumentacja właściwości

Kolumna „Wykorzystanie” określa, co bieżący kod robi z wartością. Stabilność jest opisana słownictwem ze [strony publicznego API](public-api.md#stability-vocabulary).

<a id="launchspec-root"></a>
### `LaunchSpec` (element główny)

| Właściwość | Typ JSON | Wymagana | Wartość domyślna przy pominięciu | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `Installation` | obiekt `InstallationSpec` | tak | brak | tak | `STABLE_BETA` |
| `World` | obiekt `WorldSpec` | tak | brak | tak | `STABLE_BETA` |
| `Date` | obiekt `DateSpec` | tak | brak | walidowana; każdy tryb oprócz `Unset` jest niemożliwy do uruchomienia | `PARTIAL` |
| `Time` | obiekt `TimeSpec` | tak | brak | walidowana; każdy tryb oprócz `Unset` jest niemożliwy do uruchomienia | `PARTIAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | nie | `Unset` | rozwiązywana na potrzeby komunikatów diagnostycznych; każde ustawione pole jest niemożliwe do uruchomienia | `PARTIAL` |
| `Environment` | obiekt `EnvironmentSpec` | tak | brak | tak (semantyczna nakładka (overlay) na `options.cfg`) | `STABLE_BETA` |
| `Behavior` | obiekt `LaunchBehaviorSpec` | tak | brak | częściowo (zob. rekord) | `STABLE_BETA` / `PARTIAL` |
| `Year` | obiekt `YearSpec` lub null | nie | `null` → `EffectiveYear` = tryb `Unset` | każdy tryb oprócz `Unset` jest niemożliwy do uruchomienia | `PARTIAL` |
| `Weather` | obiekt `WeatherSpec` lub null | nie | `null` → `EffectiveWeather` = tryb `Unset` | każdy tryb oprócz `Unset` jest niemożliwy do uruchomienia | `PARTIAL` |
| `Input` | obiekt `InputSpec` lub null | nie | `null` → `EffectiveInput` = oba nieustawione | każdy ustawiony dokument jest niemożliwy do uruchomienia | `PARTIAL` |
| `Diagnostics` | obiekt `DiagnosticsSpec` lub null | nie | `null` → `EffectiveDiagnostics` = wartości domyślne | tylko przenoszona | `PARTIAL` |
| `Presentation` | obiekt `SessionPresentationSpec` lub null | nie | `null` → `EffectivePresentation` = zarządzany ekran startowy, brak języka, brak katalogu niestandardowego, ikona w obszarze powiadomień wyświetlana | tak | `STABLE_BETA` |
| `InternetTextures` | obiekt `InternetTexturesSpec` lub null | nie | `null` → `EffectiveInternetTextures` = `Native` | tak | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | obiekt `SessionProfileMetadata` lub null | nie | `null` | tylko informacja o pochodzeniu (komunikat diagnostyczny planu `session_profile.selected`) | `STABLE_BETA` |

Akcesory tylko do odczytu (obecne w danych wyjściowych JSON CLI, ignorowane przy wczytywaniu): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`.

### `InstallationSpec`

| Właściwość | Typ | Wartość domyślna | Prawidłowe wartości | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | wymagana | Katalog zawierający `Omsi.exe` i `plugins\`. Zob. [reguły ścieżek](#path-rules). Pusta wartość/same białe znaki → `OL_E_INSTALLATION_NOT_FOUND`. | tak | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string lub null | `null` | Dowolny ciąg znaków. | W bieżącym kodzie brak odbiorcy: host zawsze oblicza skrót (hash) `Omsi.exe` i porównuje go z profilem buildu, nigdy z tą wartością. | `PARTIAL` (przenoszona, obecnie bez efektu) |

### `WorldSpec`

| Właściwość | Typ | Wartość domyślna | Prawidłowe wartości | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | wymagana | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` to przestarzały alias o tej samej wartości 2). | tak; `LastMapState` → `OL_E_CAPABILITY_UNAVAILABLE` | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | Dla `NewMap`: wymagana, w postaci `maps\<dir>\global.cfg` (bez rozróżniania wielkości liter, `/` akceptowany, bez `..`) i zainstalowana. Ignorowana dla `SavedSituation` (mapę dostarcza plik `.osn`). | tak (przekazanie) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | Dla `SavedSituation`: wymagana, identyfikator zainstalowanego pliku `situations\...\<file>.osn` (w postaci zwracanej przez `DiscoverAsync(Situations)` / `/list:situations`). | tak (przekazanie) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | Dla `NewMap` bez `EntrypointIdentity`: wymagana, `>= 0`, indeks na prezentowanej przez OMSI liście punktów wejścia mapy. Gdy nieustawiona, jest wysyłana do wtyczki jako `-1`. | tak (przekazanie) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | Surowa etykieta punktu wejścia lub identyfikator z wykrywania. Jej ustawienie czyni plan niemożliwym do uruchomienia (`world.entrypoint-identity`, `RUNTIME_PARTIAL`, `OL_E_CAPABILITY_UNAVAILABLE`). | przenoszona | `PARTIAL` |
| `Entrypoint` | `EntrypointSpec` (tylko do odczytu) | obliczana | `Mode` = `Identity`, gdy ustawiono `EntrypointIdentity`, w przeciwnym razie `PresentedIndex`, gdy ustawiono indeks, w przeciwnym razie `Unset`; `PresentedIndex`, `Identity` odzwierciedlają dane wejściowe. | pochodna | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Właściwość | Typ | Wartość domyślna | Prawidłowe wartości | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | wymagana (`Year`: `0`, gdy rekord ma wartość null) | `0` `Unset`, `1` `Explicit`, `2` `System`. | `Explicit`/`System` → wpis `unsupported` (`world.explicit-date`, `world.explicit-time`, `world.explicit-year`, `STATICALLY_PARTIAL`) i `OL_E_CAPABILITY_UNAVAILABLE`. Tryby są też kopiowane do przekazania startowego, które wtyczka odrzuca, gdy nie mają wartości `Unset` (do czego nigdy nie dochodzi, bo plan jest niemożliwy do uruchomienia). | `PARTIAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: `Year`, `Month` 1..12, `Day` 1..31; `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59. Musi być ustawiona, gdy `Mode` ma wartość `Explicit` (w przeciwnym razie `OL_E_DATE_TIME_APPLY_FAILED`), i musi być nieustawiona, gdy `Mode` nie ma wartości `Explicit` (`OL_E_INVALID_ARGUMENT`). `YearSpec.Value` nie jest walidowana. | tylko walidowana | `PARTIAL` |

### `WeatherSpec`

| Właściwość | Typ | Wartość domyślna | Prawidłowe wartości | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0`, gdy rekord ma wartość null | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Każdy tryb oprócz `Unset` → wpis nieobsługiwanej możliwości `weather` i `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Nazwa ustawienia wstępnego (preset) (niewalidowana). | przenoszona | `PARTIAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | Kod ICAO (niewalidowany). | przenoszona | `PARTIAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec` (wewnątrz `PlayerVehicle`)

| Właściwość | Typ | Wartość domyślna | Prawidłowe wartości | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Identyfikator zainstalowanego pliku `Vehicles\...\<file>.bus`, w przeciwnym razie `OL_E_VEHICLE_NOT_FOUND`. | rozwiązywana do `ResolvedContent`; następnie nieobsługiwana możliwość `player-vehicle.model` → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | Identyfikator malowania dla `Model` (`<cti>#item:<n>`), w przeciwnym razie `OL_E_REPAINT_NOT_FOUND`; sprawdzana tylko, gdy ustawiono `Model`. | jak wyżej | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | Zainstalowany plik `Vehicles\...\<file>.hof`, w przeciwnym razie `OL_E_HOF_NOT_FOUND`. | jak wyżej | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Dowolny ciąg znaków. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Dowolny ciąg znaków. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (tylko do odczytu) | obliczana | `true`, gdy ustawiono `Model`. Przekazanie przenosi jako `PlayerVehicleEnabled` wartość `PlayerVehicle.IsSet`. | pochodna | `PARTIAL` |

`PlayerVehicle` z `Presence` 1 i wszystkimi polami nieustawionymi jest akceptowany i nie ma żadnego efektu. Każde ustawione pole czyni plan w tym buildzie niemożliwym do uruchomienia (`STATICALLY_PARTIAL`).

### `EnvironmentSpec`

| Właściwość | Typ | Wartość domyślna | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | każda `IReadOnlyDictionary<string, OptionalValue<string>>`, wymagana (`{}`, gdy pusta) | brak | tak | `STABLE_BETA` |

Osiem grup jest łączonych; to, w której grupie umieszczono klucz, nie ma znaczenia. Każdy wpis z `Presence` 1 jest ustawieniem semantycznym z `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); klucz wybiera plik docelowy (`options.cfg` dla każdego obecnego klucza) i token. Planowanie sprawdza, czy klucz istnieje (`OL_E_UNKNOWN_SETTING`) i czy jest zapisywalny (`OL_E_SETTING_NOT_WRITABLE`); wartość jest walidowana dopiero przy uruchomieniu (`OL_E_INVALID_SETTING_VALUE`, zgłaszane jako sesja `Failed` z `OL_E_START_SESSION`). Klucze nie rozróżniają wielkości liter. Nieustawione wpisy są ignorowane. Flaga CLI `/set:<key>=<value>` zapisuje do `General`; ustawienia `settings` z profilu sesji również są scalane z `General`.

| Klucz | Wartość | Uwagi |
| --- | --- | --- |
| `general.language` | string | token `[language]` |
| `general.radio` | string | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | tokeny obecności (`autoSave` jest odwrotnością `noAutoSave`) |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | liczba całkowita 10..200 | |
| `graphics.tileDistance` | liczba całkowita 1..20 | |
| `graphics.maxObjectDistanceMeters` | liczba 20..5000 | |
| `graphics.minObjectScreenPercent` | liczba 0..10 | zapisywana po podzieleniu przez 100 |
| `graphics.minReflectionObjectScreenPercent` | liczba 0..50 | zapisywana po podzieleniu przez 100 |
| `graphics.maxObjectComplexity` | liczba całkowita 0..3 | |
| `graphics.maxMapComplexity` | liczba całkowita 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | tokeny obecności |
| `graphics.stencilShadows` | `true` / `false` | zapisywane jako `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | jeden blok `smokesystems` |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | tokeny obecności |
| `simulation.ticketSelling` | liczba całkowita 0..2 | |
| `simulation.maintenance` | liczba całkowita 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | dwa tokeny OMSI jednocześnie (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | tokeny obecności |
| `traffic.randomVehicles` | liczba całkowita 0..1000 | składowa 0 wielowierszowego bloku `AIMaxCountRandom` (zweryfikowana w runtime, macierz RV-005) |
| `traffic.humans` | liczba całkowita 0..1000 | składowa 1 bloku `AIMaxCountRandom` |
| `traffic.factorPercent` | liczba 1..300 | |
| `traffic.parkedVehiclesPercent` | liczba 0..100 | |
| `traffic.scheduledVehicles` | liczba 0..1000 | |
| `traffic.scheduledLinePriority` | liczba 1..4 | |
| `traffic.passengerFactorPercent` | liczba 0..200 | |
| `sound.stereo` | liczba 0..100 | |
| `sound.maxSimultaneousSounds` | liczba 5..1000 | |
| `sound.masterVolume` | liczba 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | odrzucane | znane, ale niezapisywalne → `OL_E_SETTING_NOT_WRITABLE` |

Modyfikowane pliki zachowują swoje kodowanie (bajty Windows-1252 zachowane; UTF-8/UTF-16 oznaczone BOM respektowane) i znaki końca wiersza.

### `LaunchBehaviorSpec`

| Właściwość | Typ | Wartość domyślna | Prawidłowe wartości | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | dowolna | Brak odbiorcy: pliki należące do sesji są zawsze dokładnie przywracane. | `PARTIAL` (przenoszona, obecnie bez efektu) |
| `SuppressStaleClosecheckWarning` | bool | `true` | dowolna | `true`: plik `closecheck` istniejący przed sesją jest przy uruchomieniu trwale usuwany (komunikat diagnostyczny `closecheck.stale-removed` z jego SHA-256; błąd `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: istniejący `closecheck` pozostaje nietknięty i nie jest traktowany jako usunięcie w ramach sesji. Plik `closecheck` zapisany przez OMSI w trakcie sesji jest zawsze usuwany podczas przywracania. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (w przeciwnym razie `ArgumentOutOfRangeException` z `StartSessionAsync`; flaga CLI `/startup-timeout` wymusza 1..600; profile wymagają > 0). Budżet czasu od uruchomienia nadzorcy do stanu `Running`; po jego wyczerpaniu sesja kończy się błędem `OL_E_STARTUP_TIMEOUT` (wtyczka uruchomiona) lub `OL_E_PLUGIN_NOT_LOADED`. | tak | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | dowolna wartość int (CLI `/shutdown-timeout` 1..600) | Brak odbiorcy: nadzorca natychmiast kończy OMSI za pomocą `TerminateProcess`; nie ma kooperacyjnego oczekiwania na zamknięcie. | `PARTIAL` (przenoszona, obecnie bez efektu) |

### `InputSpec`

| Właściwość | Typ | Wartość domyślna | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Ustawiona → nieobsługiwana możliwość `input.keyboard` (`STATICALLY_PARTIAL`) i `OL_E_CAPABILITY_UNAVAILABLE`. Wykonywanie PATCH/REPLACE dla klawiatury nie jest zaimplementowane. | `PARTIAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Ustawiona → nieobsługiwana możliwość `input.controller` i `OL_E_CAPABILITY_UNAVAILABLE`. | `PARTIAL` |

### `DiagnosticsSpec`

| Właściwość | Typ | Wartość domyślna | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | Brak odbiorcy w `src/`. Ślad hosta `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` jest zapisywany zawsze. | `PARTIAL` (przenoszona, obecnie bez efektu) |
| `Verbose` | bool | `false` | Brak odbiorcy. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | Brak odbiorcy. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | Brak odbiorcy. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Brak odbiorcy. | `PARTIAL` |
| `NativeTrace` | bool | `false` | Brak odbiorcy. | `PARTIAL` |

Flagi CLI `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` ustawiają te wartości logiczne (`/logall` ustawia `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`); są one łączone operacją OR z wartościami ze specyfikacji.

### `SessionPresentationSpec`

| Właściwość | Typ | Wartość domyślna | Prawidłowe wartości | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (alias `Native`): własne pliki ekranu startowego OMSI pozostają nietknięte. `1` `Managed`: OmsiLaunch nakłada na czas sesji pliki `GUI\NewSplashscreen_ENG.bmp` i `GUI\NewSplashscreen_<LANG>.bmp` (a następnie dokładnie je przywraca). | tak | `STABLE_BETA` (macierz RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (bez rozróżniania wielkości liter); każda inna wartość jest normalizowana do `ENG`. Gdy nieustawiona, odczytywana jest wartość `[language]` z `options.cfg` i normalizowana w ten sam sposób. | tak (tylko zarządzany ekran startowy) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Katalog zawierający `ENG.bmp` i `<LANG>.bmp` (640×480, 24-bitowy BMP). Zob. [reguły ścieżek](#path-rules). Brak katalogu: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; brak pliku: `OL_E_SPLASH_ASSET_MISSING`; nieprawidłowy format: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. Gdy nieustawiona, używany jest `<root>\.omsilaunch\assets\splash` (jednorazowo zasilany z pakietu), a w przeciwnym razie `assets\splash` z pakietu. | tak (tylko zarządzany ekran startowy) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` wyłącza samodzielny wskaźnik właściciela CLI w obszarze powiadomień Windows. | Tylko właściciel CLI; API nie ma ikony w obszarze powiadomień. Nie ma flagi CLI; wartość może pochodzić wyłącznie z pliku specyfikacji. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Właściwość | Typ | Wartość domyślna | Prawidłowe wartości | Wykorzystanie | Stabilność |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native`: nic się nie zmienia. `1` `Disabled`: wtyczka blokuje działający w procesie OMSI mechanizm pobierania (telemetria `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: profil `.itx` jest nakładany jako `Texture\standard.itx`; jego pliki docelowe i `Texture\standard.ipr` stają się usunięciami w ramach sesji. | tak | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Wymagana dla `Override` (`OL_E_ITX_PROFILE_REQUIRED`). Plik tekstowy z parami wierszy: bezwzględny adres URL `http`/`https`, a następnie ścieżka docelowa względna wobec katalogu głównego instalacji, która zawiera składową `Texture\`, nie jest zakorzeniona, nie zawiera `..`, nie zaczyna się od `\` i nie przechodzi przez żadne połączenie (junction) ani dowiązanie symboliczne (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Zob. [reguły ścieżek](#path-rules). | tak | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Właściwość | Typ | Wykorzystanie | Stabilność |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | ciągi znaków / int | Rejestrowane w komunikacie diagnostycznym planu `session_profile.selected` (`Data["session_profile.*"]`). Poza tym niewykorzystywane; zwykle wypełniane przez kompilator profili sesji, a nie ręcznie. | `STABLE_BETA` |

<a id="enumerations"></a>
## Wyliczenia

| Wyliczenie | Wartości (liczba całkowita JSON) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (przestarzały alias; jest to natywna gałąź ostatniego stanu mapy OMSI, nigdy najnowszy plik `.osn`) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (tylko do odczytu `Entrypoint.Mode`) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Liczby całkowite spoza zadeklarowanego zakresu są zapisywane przez serializator bez zmian i zachowują się jak nieznane wartości (na przykład nieznany `WorldMode` nie jest ani NEW_MAP, ani SAVED_SITUATION i daje plan bez możliwości świata; wtyczka by go odrzuciła, ale CLI i tak zastępuje tryb – zob. pierwszeństwo).

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## Reguły walidacji i komunikaty diagnostyczne braku możliwości uruchomienia

`PlanSessionAsync` wykonuje `LaunchValidation.Validate`, a następnie `SessionPlanner.PlanAsync`. Plan jest możliwy do uruchomienia dokładnie wtedy, gdy żaden kod komunikatu diagnostycznego nie zaczyna się od `OL_E_`. Pełny zestaw:

| Komunikat diagnostyczny | Warunek | Źródło |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` puste lub zawiera same białe znaki | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` bez ustawionej wartości lub z miesiącem/dniem spoza zakresu; `Time.Mode` = `Explicit` bez ustawionej wartości lub z godziną/minutą/sekundą spoza zakresu | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` lub `Time.Value` ustawione, gdy tryb nie jest `Explicit` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` z nieustawionym `MapIdentity` lub niemającym postaci `maps\...\global.cfg` (walidacja); `NewMap` z identyfikatorem, który nie jest zainstalowany (planista) | oba |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` bez `EntrypointIdentity` i z nieustawionym lub ujemnym `PresentedEntrypointIndex` | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, mapa zainstalowana, brak `EntrypointIdentity`, `PresentedEntrypointIndex` nieustawiony (`world.presented-entrypoint` niedostępna) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` bez `SituationIdentity` (walidacja) lub z identyfikatorem, który nie jest zainstalowany (planista) | oba |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: mapa wskazana w pliku `.osn` nie jest zainstalowana | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | środowisko inne niż Windows 10+ w systemie x64 z procesem hosta x64 (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | brak katalogu głównego, ustawiony atrybut tylko do odczytu lub brak podkatalogu `plugins\` (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | brak `Omsi.exe` lub jego rozmiar/SHA-256 nie odpowiada ani odciskowi profilu (`692EBFBF...`, 8 503 440 bajtów), ani skrótowi z listy dozwolonych (`omsi.profile.OMSI23004`) | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `World.Mode` = `LastMapState`; ustawione `EntrypointIdentity`; tryb `Date`/`Time`/`Year` inny niż `Unset`; tryb `Weather` inny niż `Unset`; ustawione dowolne pole `PlayerVehicle`; ustawione `Input.KeyboardDocument` lub `Input.ControllerDocument` | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` nie jest zainstalowany (oprócz `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | klucz `Environment`, którego nie ma w katalogu / który nie jest zapisywalny | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | budowanie planu ekranu startowego/ITX zgłosiło wyjątek; komunikat zawiera `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` lub `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | nie można wczytać odwołania do zestawu plików wtyczki (closure) (`OmsiLaunchRuntimePaths`) ani manifestu wydania (komunikat może zawierać `OL_E_RELEASE_MANIFEST_INVALID`) | `OmsiLaunchService.PlanSessionAsync` |

Niewalidowane na etapie planowania (błąd występuje przy uruchomieniu jako sesja `Failed` z `OL_E_START_SESSION`): wartości ustawień (`OL_E_INVALID_SETTING_VALUE`), integralność stałej wtyczki (`OL_E_PERMANENT_PLUGIN_*`), dostępność dzierżawy instalacji (`OL_E_INSTALLATION_BUSY`), zakres `StartupTimeoutSeconds` (wyjątek zgłaszany przez `StartSessionAsync`).

Informacyjne komunikaty diagnostyczne planu: `plugin.integrity.reference` (komunikat `manifest` lub `self`), `session_profile.selected`.

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## Pierwszeństwo: flagi CLI, plik specyfikacji i profil sesji

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) buduje efektywną specyfikację w następującej kolejności:

1. Punkt wyjścia = wbudowane wartości domyślne lub plik `/spec`, jeśli go podano.
2. Katalog główny instalacji = jawny argument instalacji, jeśli podano, w przeciwnym razie `RootPath` z punktu wyjścia; następnie `.`/pusta wartość → katalog pliku wykonywalnego, `Path.GetFullPath`. Jawny argument instalacji zawsze ma pierwszeństwo przed `RootPath` ze specyfikacji.
3. Profil sesji (`/predefined-profile` + `/predefined-profile-index`): jawne argumenty CLI dotykające pola należącego do profilu są odrzucane z `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (pola świata tylko w trybie NEW_MAP; klucze `/set` obecne w ustawieniu wstępnym; flagi ekranu startowego, gdy ustawienie wstępne ma `presentation`; flagi tekstur internetowych, gdy ma `internet-textures`; limity czasu, gdy ma `behavior`). `World` z punktu wyjścia jest zastępowany nowym (zachowuje się tylko tryb świata z CLI), po czym stosowane są blok `new:` profilu (tylko NEW_MAP), `settings` (do `General`), `presentation`, `internet-textures`, `behavior` oraz metadane `SessionProfile`. `compatibility.maps` jest egzekwowane dla NEW_MAP i SAVED_SITUATION.
4. Świat: tryb świata z CLI zawsze wygrywa (`/new` domyślnie, `/saved:<osn>`, `/last`); `World.Mode` z pliku specyfikacji jest zastępowany. Aby uruchomić zapisaną sytuację ze specyfikacji, należy przekazać `/saved:`. `/map` oraz `/entrypoint`/`/entrypoint-index` nadpisują punkt wyjścia; identyfikator `/entrypoint` z CLI czyści indeks; `/saved` razem z `/map` lub flagami punktu wejścia daje `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` nadpisują punkt wyjścia, gdy zostały podane (`system` wybiera `DateTimeMode.System`).
6. `/no-vehicle` czyści `PlayerVehicle`; poszczególne flagi `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` nadpisują poszczególne pola pojazdu gracza z punktu wyjścia.
7. Wpisy `/set:<key>=<value>` są dodawane do `Environment.General` (klucz jest sprawdzany, wartość nie); pozostałe siedem grup pochodzi bez zmian z punktu wyjścia.
8. `/startup-timeout` i `/shutdown-timeout` nadpisują punkt wyjścia tylko wtedy, gdy zostały podane; w przeciwnym razie obowiązuje specyfikacja, potem profil, a potem wartości domyślne 180 s / 30 s. `ShutdownTimeoutSeconds` ma w nadzorcy status `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`.
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` nadpisują punkt wyjścia, gdy zostały podane; `SuppressTrayIcon` pochodzi wyłącznie z punktu wyjścia.
10. Flagi diagnostyczne są łączone operacją OR z punktem wyjścia.

Wynik: jawna flaga CLI > profil sesji > plik specyfikacji > wbudowana wartość domyślna, z tym że flaga CLI kolidująca z polem należącym do profilu jest błędem, a nie nadpisaniem.

<a id="path-rules"></a>
## Reguły ścieżek

| Ścieżka | Zachowanie API | Zachowanie CLI |
| --- | --- | --- |
| `Installation.RootPath` | Używana w podanej postaci: w operacjach na plikach ścieżki względne są rozwiązywane względem katalogu roboczego procesu. Należy przekazywać ścieżkę bezwzględną. Dzierżawa instalacji, dziennik i katalog zawartości normalizują ją za pomocą `Path.GetFullPath`. | `.` lub pusta wartość = katalog zawierający `OmsiLaunch.exe`, nigdy folder roboczy wywołującego; jawny argument instalacji wygrywa ze specyfikacją; wynik jest przekształcany w ścieżkę bezwzględną. |
| `Presentation.CustomAssetDirectory` | Bezwzględna lub względna wobec `Installation.RootPath`. Musi istnieć. | Tak samo (`/splash-assets`). Ścieżka `assets` z profilu sesji jest ograniczona do pakietu profilu i zapisywana jako bezwzględna. |
| `InternetTextures.OverrideProfilePath` | Rozwiązywana za pomocą `Path.GetFullPath`, czyli względem katalogu roboczego procesu, a nie katalogu głównego instalacji. Musi istnieć. | Tak samo (`/internet-textures-profile`). Ścieżka `profile` z profilu sesji jest ograniczona do pakietu i zapisywana jako bezwzględna. |
| Wiersze docelowe ITX | Względne wobec katalogu głównego instalacji; muszą zawierać składową `Texture\`; bez korzenia, bez `..`, bez początkowego `\`, bez składowej będącej połączeniem (junction) lub dowiązaniem symbolicznym. | Tak samo. |
| Identyfikatory zawartości (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Względne wobec instalacji, bez rozróżniania wielkości liter, `/` akceptowany; nigdy bezwzględne. | Tak samo. |

<a id="carried-but-not-applied"></a>
## Przenoszone, ale niestosowane

| Pole | Bieżące działanie | Stabilność |
| --- | --- | --- |
| `Installation.ExpectedExecutableSha256` | brak (host porównuje skrót `Omsi.exe` z profilem buildu) | `PARTIAL` |
| `Behavior.RestoreConfiguration` | brak (przywracanie jest zawsze wykonywane) | `PARTIAL` |
| `Behavior.ShutdownTimeoutSeconds` | brak (wymuszone zakończenie; `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`) | `PARTIAL` |
| `Diagnostics.*` | brak (ślad hosta jest zawsze zapisywany) | `PARTIAL` |
| `Input.KeyboardDocument`, `Input.ControllerDocument` | plan niemożliwy do uruchomienia, gdy ustawione | `PARTIAL` |
| `Date`, `Time`, `Year` (tryb inny niż `Unset`) | plan niemożliwy do uruchomienia (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `Weather` (tryb inny niż `Unset`) | plan niemożliwy do uruchomienia (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `PlayerVehicle.*` (dowolne ustawione pole) | zawartość rozwiązywana na potrzeby komunikatów diagnostycznych, plan niemożliwy do uruchomienia (`STATICALLY_PARTIAL`) | `PARTIAL` |
| `World.EntrypointIdentity` | plan niemożliwy do uruchomienia (`RUNTIME_PARTIAL`) | `PARTIAL` |
| `World.Mode` = `LastMapState` / `LastSituation` | plan niemożliwy do uruchomienia (`UNSUPPORTED_FOR_CURRENT_PROFILE`) | `UNAVAILABLE` |
| `SessionProfile` | tylko komunikat diagnostyczny o pochodzeniu | `STABLE_BETA` |
