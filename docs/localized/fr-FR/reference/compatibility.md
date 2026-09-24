# Compatibilité

<!-- l10n: source=reference/compatibility.md -->
> Traduction de la [page originale en anglais](../../../reference/compatibility.md) d’OmsiLaunch 0.1.0-beta3. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

OmsiLaunch pilote OMSI en modifiant des adresses profilées à l'intérieur d'un build d'exécutable précis. Cette page indique quels builds d'OMSI sont pris en charge, ce qui se passe avec tout autre build, ainsi que les exigences en matière de système d'exploitation et de runtime de l'hôte et du plugin. Sources : `src/OmsiLaunch.Builds.Omsi23004/Profile.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`, `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, et les fichiers de projet.

<a id="supported-omsi-builds"></a>
## Builds d'OMSI pris en charge

Il existe exactement un profil de build, `Omsi23004_692EBFBF` (famille `OMSI_2_3_004_COMMON`). Il accepte deux exécutables par SHA-256 exact :

| Variante | SHA-256 de `Omsi.exe` | Taille | Version de fichier PE / produit | Statut |
| --- | --- | --- | --- | --- |
| Exécutable profilé (`ALTERNATE_LAA`) | `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` | 8,503,440 octets | 2.2.032 / 2.3.004 | `STABLE_BETA` ; toutes les validations à l'exécution de la matrice ont été effectuées sur ce fichier |
| Steam LAA (`STEAM_LAA`) | `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` | non vérifiée | | Accepté par liste d'autorisation parce qu'il partage la disposition native profilée et ne diffère que par les en-têtes de l'exécutable ; **non validé à l'exécution** (`profiles` indique `runtime_validated=false`, `validation_status=pending_beta_field_validation`). `PARTIAL`. |

`OmsiLaunch.exe profiles` affiche ce tableau au format JSON. Les numéros de version ne servent pas à l'acceptation : seuls le SHA-256 (et, pour l'exécutable principal, la taille exacte) comptent. Aucun autre build d'OMSI 2, aucun exécutable modifié et aucune copie patchée 4 GB ayant un hash différent n'est pris en charge.

<a id="what-happens-with-an-unknown-build"></a>
## Ce qui se passe avec un build inconnu

| Étape | Vérification | Résultat |
| --- | --- | --- |
| Planification (`PlanSessionAsync`, `/plan`, `/validate`) | `Omsi23004.Profile.MatchesExecutable(<root>\Omsi.exe)` | La capacité requise `omsi.profile.OMSI23004` est `UNAVAILABLE` ; diagnostic `OL_E_UNSUPPORTED_BUILD` ; `SessionPlan.IsRunnable=false`. Sortie CLI 1 pour un lancement, ou 3 (`UnsupportedProfile`) lorsque l'erreur remonte sous forme d'exception. |
| Démarrage (`StartSessionAsync`) | La spec est replanifiée et le hash de `Omsi.exe` recalculé | Un plan qui n'est plus exécutable (par exemple si l'exécutable a changé après la planification, ou si un appelant a modifié `IsRunnable`) est rejeté avec `OL_E_PLAN_NOT_RUNNABLE` ; aucune transaction n'est ouverte, aucun processus n'est démarré. |
| Dans le processus (`PluginRuntime.Start`) | `NativeServices.ValidateBuild` exige que le `BuildProfileId` du handoff soit `Omsi23004_692EBFBF` **et** que `NativeValidateBuild()` réussisse sur l'image en cours d'exécution | Télémétrie `plugin.build.invalid` ; l'hôte fait échouer la session avec `OL_E_BUILD_VALIDATION_FAILED` ; aucun hook natif n'est armé ; OMSI est arrêté et la transaction restaurée. |

Comme le hash de l'exécutable est comparé aux tailles et aux octets des globales profilées, la vérification dans le processus constitue la dernière ligne de défense contre une copie qui a passé la vérification du hash mais dont l'image diffère au chargement. Il n'existe ni profil de repli ni correspondance heuristique.

<a id="operating-system-and-architecture"></a>
## Système d'exploitation et architecture

`CurrentWindowsX64Platform.Detect` calcule `RuntimePlatformInfo`. La plateforme courante n'est prise en charge que si toutes les conditions suivantes sont remplies :

| Exigence | Vérification | Erreur en cas de non-respect |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 ou ultérieur | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows 64 bits et processus hôte 64 bits | `OSArchitecture == X64` et `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Installation accessible en écriture | Le répertoire racine existe, n'est pas en lecture seule et contient `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` indique également `OmsiArchitecture` et `PluginArchitecture` comme `X86` (OMSI est un processus 32 bits ; la closure du plugin est x86 et s'exécute sous WOW64), `LegacyPlatform=false`, et `Wow64Available`. Windows ARM64 n'est pas pris en charge, même là où une émulation x64 existe, car le processus hôte doit lui-même être x64.

<a id="net-requirements"></a>
## Exigences .NET

| Composant | Runtime | Remarques |
| --- | --- | --- |
| Contrôleur (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | Le bootstrapper natif localise le runtime par `hostfxr` via le `nethost.dll` fourni dans le paquet. Un runtime absent est signalé par le shim (codes de sortie 100-106 ; voir [CLI](cli.md) et [codes de sortie](exit-codes.md)). |
| Closure du plugin (`plugins\OmsiLaunch.Plugin.dll` via `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), hébergé par DNNE 2.0.6 dans `Omsi.exe` | Nécessite que le runtime .NET 6 Desktop/Core x86 soit installé sur la machine ; le runtime 64 bits seul ne suffit pas pour le plugin. |
| Pont natif (`plugins\OmsiLaunch.Native.x86.dll`) | x86 natif | Chargé uniquement depuis `plugins\` (voir [plugin permanent](../concepts/permanent-plugin.md)). |

<a id="legacy-platforms"></a>
## Plateformes historiques

Windows 7, Windows 8.x, Windows XP et les autres systèmes NT 6 et antérieurs sont en dehors du périmètre de prise en charge actuel. `RuntimePlatformInfo.LegacyPlatform` vaut toujours `false` et il n'existe aucun adaptateur historique ; le champ et l'interface de séparation `IPluginNativeServices` n'existent que pour qu'un futur adaptateur historique puisse être ajouté sans modifier l'API publique (voir `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Rien dans cette version ne s'exécute sur ces systèmes.

<a id="steam-and-large-address-aware-notes"></a>
## Remarques sur Steam et Large Address Aware

- La distribution Steam d'OMSI 2.3.004 avec l'en-tête LAA (`7DAB063D...`) figure sur la liste d'autorisation parce que ses adresses profilées sont identiques à celles de l'exécutable principal. Tant qu'aucune session de validation sur le terrain n'est consignée dans la matrice, considérez chaque capacité sur ce fichier comme `PARTIAL`.
- Steam lance OMSI lui-même ; une session doit être démarrée via `OmsiLaunch.exe` pour que le handoff existe. Démarré depuis Steam, le plugin permanent reste inerte (pas de handoff, pas de hooks).
- L'application d'un autre patcheur LAA à `Omsi.exe` modifie son hash et en fait un build inconnu.

<a id="related-pages"></a>
## Pages connexes

- [Limitations connues](known-limitations.md)
- [Statut de validation à l'exécution](../status/runtime-validation-status.md)
- [Installation](../getting-started/installation.md)
