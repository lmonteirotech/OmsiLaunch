using OmsiLaunch.Api;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.Content;
using OmsiLaunch.Configuration;
using OmsiLaunch.Process;

namespace OmsiLaunch.Core;

public sealed class SessionPlanner
{
    private readonly IRuntimePlatform platform;
    public SessionPlanner(IRuntimePlatform platform) => this.platform = platform;

    public Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = new List<LaunchDiagnostic>(LaunchValidation.Validate(spec)); var mutations = new List<PlannedMutation>();
        if (spec.SessionProfile is { } sessionProfile)
            diagnostics.Add(new("session_profile.selected", sessionProfile.Id, new Dictionary<string, string>
            {
                ["session_profile.id"] = sessionProfile.Id, ["session_profile.name"] = sessionProfile.Name,
                ["session_profile.version"] = sessionProfile.Version, ["session_profile.author"] = sessionProfile.Author,
                ["session_profile.preset_id"] = sessionProfile.PresetId, ["session_profile.preset_index"] = sessionProfile.PresetIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["session_profile.preset_name"] = sessionProfile.PresetName, ["session_profile.path"] = sessionProfile.PackagePath
            }));
        var resolved = new List<ContentIdentity>(); var required = new List<Capability>(); var unsupported = new List<Capability>();
        var detected = platform.Detect(spec.Installation.RootPath);
        Require("runtime.current-windows-x64", detected.CurrentPlatformSupported, "Current Windows x64 platform validation", required, diagnostics, "OL_E_UNSUPPORTED_OPERATING_SYSTEM");
        Require("transaction.exact-restore", detected.ExactRestoreSupported && detected.InstallationWritable, "installation transaction and exact restore", required, diagnostics, "OL_E_INSTALLATION_NOT_WRITABLE");
        var executable = new FileInfo(Path.Combine(spec.Installation.RootPath, "Omsi.exe"));
        Require("omsi.profile.OMS I23004".Replace(" ", string.Empty), executable.Exists && Omsi23004.Profile.MatchesExecutable(executable), "exact OMSI executable profile", required, diagnostics, "OL_E_UNSUPPORTED_BUILD");
        var catalog = new FileSystemContentCatalog(spec.Installation.RootPath);
        if (spec.World.Mode == WorldMode.NewMap && spec.World.MapIdentity.IsSet)
        {
            try
            {
                var map = catalog.ResolveMap(spec.World.MapIdentity.Value!); resolved.Add(new(map.Identity, "map", map.DisplayName)); Require("world.new-map", true, "native NEW_MAP pipeline", required, diagnostics, "");
                if (spec.World.EntrypointIdentity.IsSet) RequireOptional("world.entrypoint-identity", true, false, "RUNTIME_PARTIAL", "A raw entrypoint label is not a unique canonical identity and its presented-list correlation is not yet closed.", unsupported, diagnostics);
                else Require("world.presented-entrypoint", spec.World.PresentedEntrypointIndex.IsSet, "presented-index entrypoint selection", required, diagnostics, "OL_E_ENTRYPOINT_REQUIRED");
                Require("boot.headless-start", true, "synchronous Start hook", required, diagnostics, "");
            }
            catch (FileNotFoundException) { Require("content.map", false, "requested map identity", required, diagnostics, "OL_E_MAP_NOT_FOUND"); }
        }
        if (spec.World.Mode == WorldMode.SavedSituation)
        {
            TryResolveSituation(spec, catalog, resolved, required, unsupported, diagnostics);
        }
        if (spec.World.Mode == WorldMode.LastMapState)
        {
            unsupported.Add(new("world.last-map-state", false, "UNSUPPORTED_FOR_CURRENT_PROFILE", "Exact native last-map-state branch is not closed."));
            diagnostics.Add(new("OL_E_CAPABILITY_UNAVAILABLE", "LAST_MAP_STATE is not available for this profile; no timestamp-based .osn fallback is permitted."));
        }
        RequireOptional("world.explicit-date", spec.Date.Mode != DateTimeMode.Unset, false, "STATICALLY_PARTIAL", "Explicit/system Start-form date preparation is not implemented.", unsupported, diagnostics);
        RequireOptional("world.explicit-time", spec.Time.Mode != DateTimeMode.Unset, false, "STATICALLY_PARTIAL", "Explicit/system Start-form time preparation is not implemented.", unsupported, diagnostics);
        RequireOptional("world.explicit-year", spec.EffectiveYear.Mode != DateTimeMode.Unset, false, "STATICALLY_PARTIAL", "Explicit/system Start-form year preparation is not implemented.", unsupported, diagnostics);
        RequireOptional("weather", spec.EffectiveWeather.Mode != WeatherMode.Unset, false, "STATICALLY_PARTIAL", "Requested weather control is not implemented for this profile.", unsupported, diagnostics);
        if (spec.PlayerVehicle.IsSet)
        {
            var player = spec.PlayerVehicle.Value!;
            if (player.Model.IsSet)
            {
                try { var vehicle = catalog.ResolveVehicle(player.Model.Value!); resolved.Add(new(vehicle.Identity, "vehicle", vehicle.DisplayName)); }
                catch (FileNotFoundException) { Require("content.vehicle", false, "requested player vehicle identity", required, diagnostics, "OL_E_VEHICLE_NOT_FOUND"); }
            }
            if (player.Repaint.IsSet && player.Model.IsSet)
            {
                try { var repaint = catalog.EnumerateRepaints(player.Model.Value!).SingleOrDefault(x => string.Equals(x.Identity, player.Repaint.Value, StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException(); resolved.Add(new(repaint.Identity, "repaint", repaint.DisplayName)); }
                catch (FileNotFoundException) { Require("content.repaint", false, "requested repaint identity", required, diagnostics, "OL_E_REPAINT_NOT_FOUND"); }
            }
            if (player.Hof.IsSet)
            {
                try { var hof = catalog.ResolveHof(player.Hof.Value!); resolved.Add(new(hof.Identity, "hof")); }
                catch (FileNotFoundException) { Require("content.hof", false, "requested HOF identity", required, diagnostics, "OL_E_HOF_NOT_FOUND"); }
            }
            RequireOptional("player-vehicle.model", player.Model.IsSet, false, "STATICALLY_PARTIAL", "Player vehicle model creation is not implemented in the Current plugin runtime.", unsupported, diagnostics);
            RequireOptional("player-vehicle.repaint", player.Repaint.IsSet, false, "STATICALLY_PARTIAL", "Player vehicle repaint selection is not implemented in the Current plugin runtime.", unsupported, diagnostics);
            RequireOptional("player-vehicle.hof", player.Hof.IsSet, false, "STATICALLY_PARTIAL", "Player vehicle HOF assignment is not implemented in the Current plugin runtime.", unsupported, diagnostics);
            RequireOptional("player-vehicle.fleet-number", player.FleetNumber.IsSet, false, "STATICALLY_PARTIAL", "Player vehicle fleet number assignment is not implemented in the Current plugin runtime.", unsupported, diagnostics);
            RequireOptional("player-vehicle.registration", player.Registration.IsSet, false, "STATICALLY_PARTIAL", "Player vehicle registration assignment is not implemented in the Current plugin runtime.", unsupported, diagnostics);
        }
        if (spec.EffectiveInput.KeyboardDocument.IsSet) RequireOptional("input.keyboard", true, false, "STATICALLY_PARTIAL", "Keyboard PATCH/REPLACE execution is not implemented.", unsupported, diagnostics);
        if (spec.EffectiveInput.ControllerDocument.IsSet) RequireOptional("input.controller", true, false, "STATICALLY_PARTIAL", "Controller PATCH/REPLACE execution is not implemented.", unsupported, diagnostics);
        foreach (var setting in AllSettings(spec))
        {
            if (!ConfigurationCatalog.TryGet(setting.Key, out var definition)) { diagnostics.Add(new("OL_E_UNKNOWN_SETTING", "Unknown semantic setting: " + setting.Key)); continue; }
            if (!definition.Writable) { diagnostics.Add(new("OL_E_SETTING_NOT_WRITABLE", "Known setting is not writable: " + setting.Key)); continue; }
            mutations.Add(new(definition.FileName, setting.Key, setting.Value.Value!, definition.VectorIndex is null ? "token-patch" : "vector-component-patch"));
        }
        var artifacts = new[] { "permanent plugins\\OmsiLaunch.Plugin.opl", "permanent plugins\\OmsiLaunch.PluginNE.dll", "permanent plugins\\OmsiLaunch.Native.x86.dll", "OmsiLaunch startup handoff" };
        try
        {
            foreach (var path in SessionVisualAssets.DescribeTouched(spec)) mutations.Add(new(path,
                path.EndsWith(".itx", StringComparison.OrdinalIgnoreCase) ? "internet-textures.override" :
                path.EndsWith(".ipr", StringComparison.OrdinalIgnoreCase) ? "internet-textures.cache" :
                path.Contains("\\texture\\", StringComparison.OrdinalIgnoreCase) ? "internet-textures.target" :
                "session-presentation.splash", "session", "exact-file-overlay"));
            if (spec.EffectiveInternetTextures.Mode == InternetTexturesMode.Disabled) Require("internet-textures.disabled", true, "profiled in-process downloader suppression", required, diagnostics, "");
        }
        catch (Exception exception) { diagnostics.Add(new("OL_E_SESSION_PRESENTATION_INVALID", exception.Message)); }
        var touched = mutations.Select(x => x.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        // Informational provenance (for example session_profile.selected) must
        // not turn an otherwise valid plan into a non-runnable plan. Public
        // planning errors are explicitly identified by the stable OL_E_ code.
        var runnable = diagnostics.All(diagnostic => !diagnostic.Code.StartsWith("OL_E_", StringComparison.Ordinal));
        return Task.FromResult(new SessionPlan(Guid.NewGuid(), Omsi23004.ProfileIdentity, spec, detected, resolved, touched, artifacts, required, unsupported, mutations, diagnostics, runnable));
    }

    private static void Require(string name, bool available, string reason, List<Capability> required, List<LaunchDiagnostic> diagnostics, string code)
    {
        required.Add(new(name, available, available ? "STATICALLY_VALIDATED" : "UNAVAILABLE", reason));
        if (!available && !string.IsNullOrWhiteSpace(code)) diagnostics.Add(new(code, "Required capability unavailable: " + name));
    }

    private static void TryResolveSituation(LaunchSpec spec, FileSystemContentCatalog catalog, List<ContentIdentity> resolved, List<Capability> required, List<Capability> unsupported, List<LaunchDiagnostic> diagnostics)
    {
        try
        {
            var situation = catalog.ResolveSituation(spec.World.SituationIdentity.Value!);
            resolved.Add(new(situation.Identity, "situation", situation.MapIdentity));
            if (!string.IsNullOrWhiteSpace(situation.MapIdentity))
            {
                try { var map = catalog.ResolveMap(situation.MapIdentity); resolved.Add(new(map.Identity, "situation-map", map.DisplayName)); }
                catch (FileNotFoundException) { Require("content.situation-map", false, "map referenced by the selected .osn", required, diagnostics, "OL_E_SITUATION_MAP_NOT_FOUND"); }
            }
            Require("world.saved-situation", true, "profiled Start-form situation selection and Button1Click", required, diagnostics, "");
            Require("boot.headless-start", true, "synchronous Start hook", required, diagnostics, "");
        }
        catch (FileNotFoundException)
        {
            Require("content.situation", false, "requested situation identity", required, diagnostics, "OL_E_SITUATION_NOT_FOUND");
        }
    }

    private static void RequireOptional(string name, bool requested, bool available, string evidence, string reason, List<Capability> unsupported, List<LaunchDiagnostic> diagnostics)
    {
        if (!requested) return;
        unsupported.Add(new(name, available, evidence, reason));
        if (!available) diagnostics.Add(new("OL_E_CAPABILITY_UNAVAILABLE", "Requested capability unavailable: " + name));
    }
    private static IEnumerable<KeyValuePair<string, OptionalValue<string>>> AllSettings(LaunchSpec spec) => spec.Environment.General.Concat(spec.Environment.Advanced).Concat(spec.Environment.Graphics).Concat(spec.Environment.AdvancedGraphics).Concat(spec.Environment.Sound).Concat(spec.Environment.AiPassengers).Concat(spec.Environment.Keyboard).Concat(spec.Environment.Controllers).Where(x => x.Value.IsSet);
}
