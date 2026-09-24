using System.Globalization;

// Windows-only strings live in one registry. They deliberately follow the
// Windows UI culture, never OMSI content or session-profile language.
internal sealed class WindowsUiStrings
{
    private readonly IReadOnlyDictionary<string, string> values;

    private WindowsUiStrings(string locale, IReadOnlyDictionary<string, string> values)
    {
        Locale = locale;
        this.values = values;
    }

    public string Locale { get; }
    public string this[string key] => values.TryGetValue(key, out var value) ? value : English[key];

    public static WindowsUiStrings Resolve(CultureInfo? culture = null)
    {
        var name = (culture ?? CultureInfo.CurrentUICulture).Name;
        if (Translations.TryGetValue(name, out var exact)) return new WindowsUiStrings(name, exact);
        var language = (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName;
        if (Translations.TryGetValue(language, out var family)) return new WindowsUiStrings(language, family);
        return new WindowsUiStrings("en", English);
    }

    private static readonly IReadOnlyDictionary<string, string> English = New(
        ("Tray.Running", "OmsiLaunch is running"), ("Tray.Status", "Status"), ("Tray.EndSession", "End session"), ("Tray.EndSessionDescription", "Ends OMSI 2 and the OmsiLaunch session"),
        ("Status.Title", "OmsiLaunch session status"), ("Status.SessionRunning", "Session is running"), ("Status.Section.Session", "Session"), ("Status.Section.Profile", "Session profile"), ("Status.Section.Environment", "Environment"), ("Status.Section.Vehicle", "Vehicle"), ("Status.Section.Configuration", "Configuration"), ("Status.Section.Presentation", "Presentation"),
        ("Status.Field.Mode", "Mode"), ("Status.Field.Map", "Map"), ("Status.Field.EntryPoint", "Entry point"), ("Status.Field.Situation", "Situation"), ("Status.Field.SessionProfile", "Profile"), ("Status.Field.Preset", "Preset"), ("Status.Field.Date", "Date"), ("Status.Field.Time", "Time"), ("Status.Field.Weather", "Weather"), ("Status.Field.Vehicle", "Vehicle"), ("Status.Field.Repaint", "Repaint"), ("Status.Field.Hof", "HOF"), ("Status.Field.Fleet", "Fleet number"), ("Status.Field.Registration", "Registration"), ("Status.Field.Splash", "Splash"), ("Status.Field.InternetTextures", "Internet textures"),
        ("Status.Close", "Close"), ("Mode.NewMap", "New session"), ("Mode.SavedSituation", "Saved situation"), ("Mode.LastMapState", "Last map state"), ("Presentation.Managed", "Managed"), ("Presentation.Unset", "Original OMSI"), ("InternetTextures.Native", "Original OMSI"), ("InternetTextures.Disabled", "Disabled"), ("InternetTextures.Override", "Override"),
        ("Stop.Title", "End session?"), ("Stop.Message", "OMSI 2 will be closed and the OmsiLaunch managed session will end."), ("Stop.Confirm", "End session"), ("Stop.Cancel", "Cancel"), ("Stop.Failed", "The session could not be ended. OMSI and its managed session remain active."));

    private static readonly IReadOnlyDictionary<string, string> PortugueseBrazil = New(
        ("Tray.Running", "OmsiLaunch em execução"), ("Tray.Status", "Status"), ("Tray.EndSession", "Encerrar sessão"), ("Tray.EndSessionDescription", "Encerra o OMSI 2 e a sessão do OmsiLaunch"),
        ("Status.Title", "Status da sessão OmsiLaunch"), ("Status.SessionRunning", "Sessão em execução"), ("Status.Section.Session", "Sessão"), ("Status.Section.Profile", "Perfil de sessão"), ("Status.Section.Environment", "Ambiente"), ("Status.Section.Vehicle", "Veículo"), ("Status.Section.Configuration", "Configuração"), ("Status.Section.Presentation", "Apresentação"),
        ("Status.Field.Mode", "Modo"), ("Status.Field.Map", "Mapa"), ("Status.Field.EntryPoint", "Ponto de entrada"), ("Status.Field.Situation", "Situação"), ("Status.Field.SessionProfile", "Perfil"), ("Status.Field.Preset", "Predefinição"), ("Status.Field.Date", "Data"), ("Status.Field.Time", "Hora"), ("Status.Field.Weather", "Clima"), ("Status.Field.Vehicle", "Veículo"), ("Status.Field.Repaint", "Pintura"), ("Status.Field.Hof", "HOF"), ("Status.Field.Fleet", "Número de frota"), ("Status.Field.Registration", "Matrícula"), ("Status.Field.Splash", "Splash"), ("Status.Field.InternetTextures", "Texturas da internet"),
        ("Status.Close", "Fechar"), ("Mode.NewMap", "Nova sessão"), ("Mode.SavedSituation", "Situação salva"), ("Mode.LastMapState", "Último estado do mapa"), ("Presentation.Managed", "Gerenciado"), ("Presentation.Unset", "OMSI original"), ("InternetTextures.Native", "OMSI original"), ("InternetTextures.Disabled", "Desativadas"), ("InternetTextures.Override", "Substituição"),
        ("Stop.Title", "Encerrar sessão?"), ("Stop.Message", "O OMSI 2 será encerrado e a sessão gerenciada pelo OmsiLaunch será finalizada."), ("Stop.Confirm", "Encerrar sessão"), ("Stop.Cancel", "Cancelar"), ("Stop.Failed", "Não foi possível encerrar a sessão. O OMSI e sua sessão gerenciada continuam ativos."));

    private static readonly IReadOnlyDictionary<string, string> German = New(
        ("Tray.Running", "OmsiLaunch wird ausgeführt"), ("Tray.Status", "Status"), ("Tray.EndSession", "Sitzung beenden"), ("Tray.EndSessionDescription", "Beendet OMSI 2 und die OmsiLaunch-Sitzung"),
        ("Status.Title", "OmsiLaunch-Sitzungsstatus"), ("Status.SessionRunning", "Sitzung wird ausgeführt"), ("Status.Section.Session", "Sitzung"), ("Status.Section.Profile", "Sitzungsprofil"), ("Status.Section.Environment", "Umgebung"), ("Status.Section.Vehicle", "Fahrzeug"), ("Status.Section.Configuration", "Konfiguration"), ("Status.Section.Presentation", "Darstellung"),
        ("Status.Field.Mode", "Modus"), ("Status.Field.Map", "Karte"), ("Status.Field.EntryPoint", "Einstiegspunkt"), ("Status.Field.Situation", "Situation"), ("Status.Field.SessionProfile", "Profil"), ("Status.Field.Preset", "Voreinstellung"), ("Status.Field.Date", "Datum"), ("Status.Field.Time", "Zeit"), ("Status.Field.Weather", "Wetter"), ("Status.Field.Vehicle", "Fahrzeug"), ("Status.Field.Repaint", "Lackierung"), ("Status.Field.Hof", "HOF"), ("Status.Field.Fleet", "Fuhrparknummer"), ("Status.Field.Registration", "Kennzeichen"), ("Status.Field.Splash", "Startbild"), ("Status.Field.InternetTextures", "Internettexturen"),
        ("Status.Close", "Schließen"), ("Mode.NewMap", "Neue Sitzung"), ("Mode.SavedSituation", "Gespeicherte Situation"), ("Mode.LastMapState", "Letzter Kartenstatus"), ("Presentation.Managed", "Verwaltet"), ("Presentation.Unset", "Originales OMSI"), ("InternetTextures.Native", "Originales OMSI"), ("InternetTextures.Disabled", "Deaktiviert"), ("InternetTextures.Override", "Überschreiben"),
        ("Stop.Title", "Sitzung beenden?"), ("Stop.Message", "OMSI 2 wird geschlossen und die von OmsiLaunch verwaltete Sitzung wird beendet."), ("Stop.Confirm", "Sitzung beenden"), ("Stop.Cancel", "Abbrechen"), ("Stop.Failed", "Die Sitzung konnte nicht beendet werden. OMSI und die verwaltete Sitzung bleiben aktiv."));

    private static readonly IReadOnlyDictionary<string, string> French = New(
        ("Tray.Running", "OmsiLaunch est en cours d'exécution"), ("Tray.Status", "État"), ("Tray.EndSession", "Terminer la session"), ("Tray.EndSessionDescription", "Ferme OMSI 2 et la session OmsiLaunch"),
        ("Status.Title", "État de la session OmsiLaunch"), ("Status.SessionRunning", "La session est en cours"), ("Status.Section.Session", "Session"), ("Status.Section.Profile", "Profil de session"), ("Status.Section.Environment", "Environnement"), ("Status.Section.Vehicle", "Véhicule"), ("Status.Section.Configuration", "Configuration"), ("Status.Section.Presentation", "Présentation"),
        ("Status.Field.Mode", "Mode"), ("Status.Field.Map", "Carte"), ("Status.Field.EntryPoint", "Point d'entrée"), ("Status.Field.Situation", "Situation"), ("Status.Field.SessionProfile", "Profil"), ("Status.Field.Preset", "Préréglage"), ("Status.Field.Date", "Date"), ("Status.Field.Time", "Heure"), ("Status.Field.Weather", "Météo"), ("Status.Field.Vehicle", "Véhicule"), ("Status.Field.Repaint", "Livrée"), ("Status.Field.Hof", "HOF"), ("Status.Field.Fleet", "Numéro de flotte"), ("Status.Field.Registration", "Immatriculation"), ("Status.Field.Splash", "Écran de démarrage"), ("Status.Field.InternetTextures", "Textures Internet"),
        ("Status.Close", "Fermer"), ("Mode.NewMap", "Nouvelle session"), ("Mode.SavedSituation", "Situation enregistrée"), ("Mode.LastMapState", "Dernier état de la carte"), ("Presentation.Managed", "Gérée"), ("Presentation.Unset", "OMSI d'origine"), ("InternetTextures.Native", "OMSI d'origine"), ("InternetTextures.Disabled", "Désactivées"), ("InternetTextures.Override", "Remplacement"),
        ("Stop.Title", "Terminer la session ?"), ("Stop.Message", "OMSI 2 sera fermé et la session gérée par OmsiLaunch prendra fin."), ("Stop.Confirm", "Terminer la session"), ("Stop.Cancel", "Annuler"), ("Stop.Failed", "La session n'a pas pu être terminée. OMSI et sa session gérée restent actifs."));

    private static readonly IReadOnlyDictionary<string, string> Polish = New(
        ("Tray.Running", "OmsiLaunch jest uruchomiony"), ("Tray.Status", "Stan"), ("Tray.EndSession", "Zakończ sesję"), ("Tray.EndSessionDescription", "Kończy OMSI 2 i sesję OmsiLaunch"),
        ("Status.Title", "Stan sesji OmsiLaunch"), ("Status.SessionRunning", "Sesja jest uruchomiona"), ("Status.Section.Session", "Sesja"), ("Status.Section.Profile", "Profil sesji"), ("Status.Section.Environment", "Środowisko"), ("Status.Section.Vehicle", "Pojazd"), ("Status.Section.Configuration", "Konfiguracja"), ("Status.Section.Presentation", "Prezentacja"),
        ("Status.Field.Mode", "Tryb"), ("Status.Field.Map", "Mapa"), ("Status.Field.EntryPoint", "Punkt wejścia"), ("Status.Field.Situation", "Sytuacja"), ("Status.Field.SessionProfile", "Profil"), ("Status.Field.Preset", "Ustawienie"), ("Status.Field.Date", "Data"), ("Status.Field.Time", "Czas"), ("Status.Field.Weather", "Pogoda"), ("Status.Field.Vehicle", "Pojazd"), ("Status.Field.Repaint", "Malowanie"), ("Status.Field.Hof", "HOF"), ("Status.Field.Fleet", "Numer floty"), ("Status.Field.Registration", "Rejestracja"), ("Status.Field.Splash", "Ekran startowy"), ("Status.Field.InternetTextures", "Tekstury internetowe"),
        ("Status.Close", "Zamknij"), ("Mode.NewMap", "Nowa sesja"), ("Mode.SavedSituation", "Zapisana sytuacja"), ("Mode.LastMapState", "Ostatni stan mapy"), ("Presentation.Managed", "Zarządzany"), ("Presentation.Unset", "Oryginalny OMSI"), ("InternetTextures.Native", "Oryginalny OMSI"), ("InternetTextures.Disabled", "Wyłączone"), ("InternetTextures.Override", "Zastąpienie"),
        ("Stop.Title", "Zakończyć sesję?"), ("Stop.Message", "OMSI 2 zostanie zamknięty, a zarządzana sesja OmsiLaunch zostanie zakończona."), ("Stop.Confirm", "Zakończ sesję"), ("Stop.Cancel", "Anuluj"), ("Stop.Failed", "Nie można było zakończyć sesji. OMSI i zarządzana sesja pozostają aktywne."));

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Translations = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = English, ["en-US"] = English, ["en-GB"] = English,
        // Do not silently apply Brazilian Portuguese to Portugal. The project
        // currently ships pt-BR only, so pt-PT correctly falls back to English.
        ["pt-BR"] = PortugueseBrazil,
        ["de"] = German, ["de-DE"] = German,
        ["fr"] = French, ["fr-FR"] = French,
        ["pl"] = Polish, ["pl-PL"] = Polish
    };

    private static IReadOnlyDictionary<string, string> New(params (string Key, string Value)[] items) => items.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
}
