# Windows UI Localization

The Windows session indicator resolves `CultureInfo.CurrentUICulture`, first by
exact locale, then by a supported language family, then English. It never uses
the OMSI language, content language, CLI arguments, or session-profile locale.
The distributed localized documentation languages are English, Brazilian
Portuguese, German, French, and Polish. `pt-PT` intentionally falls back to
English rather than incorrectly using `pt-BR`.

The canonical runtime resource registry is `tools/OmsiLaunch.Cli/WindowsUiStrings.cs`.
The following glossary covers every Windows UI string introduced for Beta 3.

| Key | EN | PT-BR | DE | FR | PL |
|---|---|---|---|---|---|
| `Tray.Running` | OmsiLaunch is running | OmsiLaunch em execução | OmsiLaunch wird ausgeführt | OmsiLaunch est en cours d'exécution | OmsiLaunch jest uruchomiony |
| `Tray.Status` | Status | Status | Status | État | Stan |
| `Tray.EndSession` | End session | Encerrar sessão | Sitzung beenden | Terminer la session | Zakończ sesję |
| `Tray.EndSessionDescription` | Ends OMSI 2 and the OmsiLaunch session | Encerra o OMSI 2 e a sessão do OmsiLaunch | Beendet OMSI 2 und die OmsiLaunch-Sitzung | Ferme OMSI 2 et la session OmsiLaunch | Kończy OMSI 2 i sesję OmsiLaunch |
| `Status.Title` / `Status.SessionRunning` | OmsiLaunch session status / Session is running | Status da sessão OmsiLaunch / Sessão em execução | OmsiLaunch-Sitzungsstatus / Sitzung wird ausgeführt | État de la session OmsiLaunch / La session est en cours | Stan sesji OmsiLaunch / Sesja jest uruchomiona |
| `Status.Section.*` | Session; Session profile; Environment; Vehicle; Configuration; Presentation | Sessão; Perfil de sessão; Ambiente; Veículo; Configuração; Apresentação | Sitzung; Sitzungsprofil; Umgebung; Fahrzeug; Konfiguration; Darstellung | Session; Profil de session; Environnement; Véhicule; Configuration; Présentation | Sesja; Profil sesji; Środowisko; Pojazd; Konfiguracja; Prezentacja |
| `Status.Field.*` | Mode, Map, Entry point, Situation, Profile, Preset, Date, Time, Weather, Vehicle, Repaint, HOF, Fleet number, Registration, Splash, Internet textures | Modo, Mapa, Ponto de entrada, Situação, Perfil, Predefinição, Data, Hora, Clima, Veículo, Pintura, HOF, Número de frota, Matrícula, Splash, Texturas da internet | Modus, Karte, Einstiegspunkt, Situation, Profil, Voreinstellung, Datum, Zeit, Wetter, Fahrzeug, Lackierung, HOF, Fuhrparknummer, Kennzeichen, Startbild, Internettexturen | Mode, Carte, Point d'entrée, Situation, Profil, Préréglage, Date, Heure, Météo, Véhicule, Livrée, HOF, Numéro de flotte, Immatriculation, Écran de démarrage, Textures Internet | Tryb, Mapa, Punkt wejścia, Sytuacja, Profil, Ustawienie, Data, Czas, Pogoda, Pojazd, Malowanie, HOF, Numer floty, Rejestracja, Ekran startowy, Tekstury internetowe |
| `Status.Close` | Close | Fechar | Schließen | Fermer | Zamknij |
| `Mode.*` | New session; Saved situation; Last map state | Nova sessão; Situação salva; Último estado do mapa | Neue Sitzung; Gespeicherte Situation; Letzter Kartenstatus | Nouvelle session; Situation enregistrée; Dernier état de la carte | Nowa sesja; Zapisana sytuacja; Ostatni stan mapy |
| `Presentation.*` / `InternetTextures.*` | Managed; Original OMSI; Disabled; Override | Gerenciado; OMSI original; Desativadas; Substituição | Verwaltet; Originales OMSI; Deaktiviert; Überschreiben | Gérée; OMSI d'origine; Désactivées; Remplacement | Zarządzany; Oryginalny OMSI; Wyłączone; Zastąpienie |
| `Stop.Title` / `Stop.Message` | End session? / OMSI 2 will be closed and the OmsiLaunch managed session will end. | Encerrar sessão? / O OMSI 2 será encerrado e a sessão gerenciada pelo OmsiLaunch será finalizada. | Sitzung beenden? / OMSI 2 wird geschlossen und die von OmsiLaunch verwaltete Sitzung wird beendet. | Terminer la session ? / OMSI 2 sera fermé et la session gérée par OmsiLaunch prendra fin. | Zakończyć sesję? / OMSI 2 zostanie zamknięty, a zarządzana sesja OmsiLaunch zostanie zakończona. |
| `Stop.Confirm` / `Stop.Cancel` | End session / Cancel | Encerrar sessão / Cancelar | Sitzung beenden / Abbrechen | Terminer la session / Annuler | Zakończ sesję / Anuluj |
| `Stop.Failed` | The session could not be ended. OMSI and its managed session remain active. | Não foi possível encerrar a sessão. O OMSI e sua sessão gerenciada continuam ativos. | Die Sitzung konnte nicht beendet werden. OMSI und die verwaltete Sitzung bleiben aktiv. | La session n'a pas pu être terminée. OMSI et sa session gérée restent actifs. | Nie można było zakończyć sesji. OMSI i zarządzana sesja pozostają aktywne. |
