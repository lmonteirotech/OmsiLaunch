using OmsiLaunch.Api;
using OmsiLaunch.Configuration;

internal sealed record StatusField(string Label, string Value);
internal sealed record StatusSection(string Title, IReadOnlyList<StatusField> Fields);
internal sealed record SessionStatusView(string Heading, IReadOnlyList<StatusSection> Sections);

// Converts only resolved public session data into a compact human view. It does
// not parse command-line arguments or query live simulation telemetry.
internal static class SessionStatusPresenter
{
    public static SessionStatusView Create(SessionPlan plan, SessionStatus status, WindowsUiStrings ui)
    {
        var spec = plan.Spec;
        var sections = new List<StatusSection>();
        var session = new List<StatusField> { new(ui["Status.Field.Mode"], ModeName(spec.World.Mode, ui)) };
        var map = plan.ResolvedContent.FirstOrDefault(content => content.Kind.Equals("map", StringComparison.OrdinalIgnoreCase));
        if (map is not null) session.Add(new(ui["Status.Field.Map"], Display(map)));
        if (spec.World.Mode == WorldMode.SavedSituation && spec.World.SituationIdentity.IsSet) session.Add(new(ui["Status.Field.Situation"], FileName(spec.World.SituationIdentity.Value!)));
        if (spec.World.EntrypointIdentity.IsSet) session.Add(new(ui["Status.Field.EntryPoint"], spec.World.EntrypointIdentity.Value!));
        else if (spec.World.PresentedEntrypointIndex.IsSet) session.Add(new(ui["Status.Field.EntryPoint"], spec.World.PresentedEntrypointIndex.Value!.ToString()));
        sections.Add(new(ui["Status.Section.Session"], session));

        if (spec.SessionProfile is { } profile)
        {
            var fields = new List<StatusField> { new(ui["Status.Field.SessionProfile"], profile.Name) };
            if (!string.IsNullOrWhiteSpace(profile.PresetName)) fields.Add(new(ui["Status.Field.Preset"], profile.PresetName));
            sections.Add(new(ui["Status.Section.Profile"], fields));
        }

        var environment = Environment(spec, ui);
        if (environment.Count > 0) sections.Add(new(ui["Status.Section.Environment"], environment));
        var vehicle = Vehicle(spec, ui);
        if (vehicle.Count > 0) sections.Add(new(ui["Status.Section.Vehicle"], vehicle));
        var configuration = Configuration(spec);
        if (configuration.Count > 0) sections.Add(new(ui["Status.Section.Configuration"], configuration));
        var presentation = Presentation(spec, ui);
        if (presentation.Count > 0) sections.Add(new(ui["Status.Section.Presentation"], presentation));
        return new SessionStatusView(status.State == SessionState.Running ? ui["Status.SessionRunning"] : status.State.ToString(), sections);
    }

    private static List<StatusField> Environment(LaunchSpec spec, WindowsUiStrings ui)
    {
        var fields = new List<StatusField>();
        if (spec.Date.Mode == DateTimeMode.Explicit && spec.Date.Value.IsSet) fields.Add(new(ui["Status.Field.Date"], $"{spec.Date.Value.Value!.Day:D2}/{spec.Date.Value.Value.Month:D2}/{spec.Date.Value.Value.Year:D4}"));
        else if (spec.Date.Mode == DateTimeMode.System) fields.Add(new(ui["Status.Field.Date"], "System"));
        if (spec.Time.Mode == DateTimeMode.Explicit && spec.Time.Value.IsSet) fields.Add(new(ui["Status.Field.Time"], $"{spec.Time.Value.Value!.Hour:D2}:{spec.Time.Value.Value.Minute:D2}:{spec.Time.Value.Value.Second:D2}"));
        else if (spec.Time.Mode == DateTimeMode.System) fields.Add(new(ui["Status.Field.Time"], "System"));
        var weather = spec.EffectiveWeather;
        if (weather.Mode == WeatherMode.Icao && weather.Icao.IsSet) fields.Add(new(ui["Status.Field.Weather"], weather.Icao.Value!));
        else if (weather.Mode == WeatherMode.Preset && weather.Preset.IsSet) fields.Add(new(ui["Status.Field.Weather"], weather.Preset.Value!));
        else if (weather.Mode == WeatherMode.RealCurrent) fields.Add(new(ui["Status.Field.Weather"], "Real/current"));
        return fields;
    }

    private static List<StatusField> Vehicle(LaunchSpec spec, WindowsUiStrings ui)
    {
        if (!spec.PlayerVehicle.IsSet || spec.PlayerVehicle.Value is null) return new();
        var vehicle = spec.PlayerVehicle.Value;
        var fields = new List<StatusField>();
        Add(fields, ui["Status.Field.Vehicle"], vehicle.Model); Add(fields, ui["Status.Field.Repaint"], vehicle.Repaint); Add(fields, ui["Status.Field.Hof"], vehicle.Hof); Add(fields, ui["Status.Field.Fleet"], vehicle.FleetNumber); Add(fields, ui["Status.Field.Registration"], vehicle.Registration);
        return fields;
    }

    private static List<StatusField> Configuration(LaunchSpec spec)
    {
        var fields = new List<StatusField>();
        foreach (var setting in AllSettings(spec))
        {
            if (!setting.Value.IsSet || setting.Value.Value is null) continue;
            if (!ConfigurationCatalog.TryGet(setting.Key, out var metadata)) continue;
            fields.Add(new(Humanize(metadata.Key), FormatSetting(metadata, setting.Value.Value)));
        }
        return fields;
    }

    private static IEnumerable<KeyValuePair<string, OptionalValue<string>>> AllSettings(LaunchSpec spec) => spec.Environment.General.Concat(spec.Environment.Advanced).Concat(spec.Environment.Graphics).Concat(spec.Environment.AdvancedGraphics).Concat(spec.Environment.Sound).Concat(spec.Environment.AiPassengers).Concat(spec.Environment.Keyboard).Concat(spec.Environment.Controllers);

    private static List<StatusField> Presentation(LaunchSpec spec, WindowsUiStrings ui)
    {
        var presentation = spec.EffectivePresentation;
        var fields = new List<StatusField> { new(ui["Status.Field.Splash"], presentation.Splash == SplashMode.Managed ? ui["Presentation.Managed"] : ui["Presentation.Unset"]) };
        var internet = spec.EffectiveInternetTextures.Mode switch { InternetTexturesMode.Disabled => ui["InternetTextures.Disabled"], InternetTexturesMode.Override => ui["InternetTextures.Override"], _ => ui["InternetTextures.Native"] };
        fields.Add(new(ui["Status.Field.InternetTextures"], internet));
        return fields;
    }

    private static void Add(List<StatusField> fields, string label, OptionalValue<string> value) { if (value.IsSet && !string.IsNullOrWhiteSpace(value.Value)) fields.Add(new(label, value.Value!)); }
    private static string Display(ContentIdentity content) => string.IsNullOrWhiteSpace(content.DisplayName) ? FileName(content.Identity) : content.DisplayName!;
    private static string FileName(string value) => Path.GetFileNameWithoutExtension(value.Replace('/', '\\'));
    private static string ModeName(WorldMode mode, WindowsUiStrings ui) => mode switch { WorldMode.NewMap => ui["Mode.NewMap"], WorldMode.SavedSituation => ui["Mode.SavedSituation"], _ => ui["Mode.LastMapState"] };
    private static string Humanize(string key) => string.Join(" ", key.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    private static string FormatSetting(ConfigurationSetting setting, string value) => setting.Key.EndsWith("Percent", StringComparison.OrdinalIgnoreCase) ? value + "%" : setting.Key.EndsWith("DistanceMeters", StringComparison.OrdinalIgnoreCase) ? value + " m" : value;
}
