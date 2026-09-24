# Совместимость

<!-- l10n: source=reference/compatibility.md -->
> Перевод [исходной страницы на английском языке](../../../reference/compatibility.md) для OmsiLaunch 0.1.0-beta3. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

OmsiLaunch управляет OMSI, изменяя профилированные адреса внутри одной конкретной сборки исполняемого файла. На этой странице указано, какие сборки OMSI поддерживаются, что происходит с любой другой сборкой, а также требования к операционной системе и среде выполнения для хоста и плагина. Источники: `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`, `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs` и файлы проектов.

<a id="supported-omsi-builds"></a>
## Поддерживаемые сборки OMSI

Существует ровно один профиль сборки — `Omsi23004_692EBFBF` (семейство `OMSI_2_3_004_COMMON`). Он принимает два исполняемых файла по точному SHA-256:

| Вариант | SHA-256 файла `Omsi.exe` | Размер | Версия файла PE / продукта | Статус |
| --- | --- | --- | --- | --- |
| Профилированный исполняемый файл (`ALTERNATE_LAA`) | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8 503 440 байт | 2.2.032 / 2.3.004 | `STABLE_BETA`; все проверки в runtime из матрицы выполнялись на этом файле |
| Steam LAA (`STEAM_LAA`) | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | не проверяется | | Принимается по списку разрешённых, поскольку имеет ту же профилированную нативную структуру и отличается только заголовками исполняемого файла; **не проверено в runtime** (`profiles` сообщает `runtime_validated=false`, `validation_status=pending_beta_field_validation`). `PARTIAL`. |

`OmsiLaunch.exe profiles` выводит эту таблицу в виде JSON. Номера версий для приёма не используются: учитывается только SHA-256 (а для основного исполняемого файла — ещё и точный размер). Никакая другая сборка OMSI 2, никакой изменённый исполняемый файл и никакая копия с патчем 4 GB и другим хешем не поддерживаются.

<a id="what-happens-with-an-unknown-build"></a>
## Что происходит с неизвестной сборкой

| Этап | Проверка | Результат |
| --- | --- | --- |
| Планирование (`PlanSessionAsync`, `/plan`, `/validate`) | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | Обязательная возможность `omsi.profile.OMSI23004` имеет статус `UNAVAILABLE`; диагностическое сообщение `OL_E_UNSUPPORTED_BUILD`; `SessionPlan.IsRunnable=false`. Код выхода CLI 1 для запуска или 3 (`UnsupportedProfile`), если ошибка выходит наружу в виде исключения. |
| Запуск (`StartSessionAsync`) | Спецификация планируется повторно, хеш `Omsi.exe` вычисляется заново | План, который больше не исполним (например, исполняемый файл изменился после планирования или вызывающая сторона изменила `IsRunnable`), отклоняется с `OL_E_PLAN_NOT_RUNNABLE`; транзакция не открывается, процесс не запускается. |
| Внутри процесса (`PluginRuntime.Start`) | `NativeServices.ValidateBuild` требует, чтобы `BuildProfileId` из handoff был равен `Omsi23004_692EBFBF`, **и** чтобы `NativeValidateBuild()` успешно выполнилась для работающего образа | Телеметрия `plugin.build.invalid`; хост переводит сеанс в сбой с `OL_E_BUILD_VALIDATION_FAILED`; ни один нативный хук не активируется; OMSI завершается, транзакция восстанавливается. |

Поскольку хеш исполняемого файла сопоставляется с размерами и байтами профилированных глобальных переменных, проверка внутри процесса — последний рубеж защиты от копии, которая прошла проверку хеша, но чей образ при загрузке отличается. Запасного профиля и эвристического сопоставления нет.

<a id="operating-system-and-architecture"></a>
## Операционная система и архитектура

`CurrentWindowsX64Platform.Detect` вычисляет `RuntimePlatformInfo`. Текущая платформа поддерживается, только если выполняются все следующие условия:

| Требование | Проверка | Ошибка при нарушении |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 или новее | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64-разрядная Windows и 64-разрядный процесс хоста | `OSArchitecture == X64` и `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Установка, доступная для записи | Корневой каталог существует, не доступен только для чтения и содержит `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` также сообщает `OmsiArchitecture` и `PluginArchitecture` как `X86` (OMSI — 32-разрядный процесс; замыкание плагина (набор его файлов) имеет архитектуру x86 и работает под WOW64), `LegacyPlatform=false` и `Wow64Available`. Windows на ARM64 не поддерживается даже там, где есть эмуляция x64, потому что процесс хоста сам должен быть x64.

<a id="net-requirements"></a>
## Требования к .NET

| Компонент | Среда выполнения | Примечания |
| --- | --- | --- |
| Контроллер (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | Нативный загрузчик (bootstrapper) находит среду выполнения через `hostfxr` с помощью входящего в пакет `nethost.dll`. Об отсутствии среды выполнения сообщает shim (коды выхода 100-106; см. [CLI](cli.md) и [коды выхода](exit-codes.md)). |
| Замыкание плагина (`plugins\OmsiLaunch.Plugin.dll` через `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), размещается DNNE 2.0.6 внутри `Omsi.exe` | Требует, чтобы на компьютере была установлена среда выполнения .NET 6 Desktop/Core x86; одной 64-разрядной среды выполнения для плагина недостаточно. |
| Нативный мост (`plugins\OmsiLaunch.Native.x86.dll`) | нативный x86 | Загружается только из `plugins\` (см. [постоянный плагин](../concepts/permanent-plugin.md)). |

<a id="legacy-platforms"></a>
## Устаревшие платформы

Windows 7, Windows 8.x, Windows XP и другие системы NT 6 и более ранние находятся за пределами текущей границы поддержки. `RuntimePlatformInfo.LegacyPlatform` всегда равен `false`, и адаптера для устаревших платформ нет; поле и точка расширения `IPluginNativeServices` существуют только для того, чтобы в будущем можно было добавить такой адаптер без изменения публичного API (см. `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Ничто в этом выпуске на таких системах не работает.

<a id="steam-and-large-address-aware-notes"></a>
## Примечания о Steam и Large Address Aware

- Дистрибутив OMSI 2.3.004 из Steam с заголовком LAA (`7DAB063D...`) внесён в список разрешённых, поскольку его профилированные адреса идентичны адресам основного исполняемого файла. Пока в матрице не зафиксирован сеанс полевой проверки, считайте каждую возможность на этом файле `PARTIAL`.
- Steam запускает OMSI самостоятельно; сеанс должен запускаться через `OmsiLaunch.exe`, чтобы существовал handoff. При запуске из Steam постоянный плагин остаётся неактивным (нет handoff, нет хуков).
- Применение к `Omsi.exe` другого патчера LAA меняет его хеш и делает его неизвестной сборкой.

<a id="related-pages"></a>
## Связанные страницы

- [Известные ограничения](known-limitations.md)
- [Состояние проверки в runtime](../status/runtime-validation-status.md)
- [Установка](../getting-started/installation.md)
