namespace OmsiLaunch.Api;

// Canonical Beta control-surface inventory. Internal implementation details and
// raw native operations are intentionally not published here.
public enum PublicCapabilityClassification : byte
{
    PublicStableBeta,
    PublicExperimental,
    InternalOnly,
    Unsupported
}

public enum PublicCapabilityKind : byte { Read, Write, Action, Event }

// Concrete runtime operations may require values even when the public catalog
// groups them under one capability. This metadata is shared by every frontend
// so a missing value is rejected before it reaches the OMSI mailbox.
public sealed record PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description);
public sealed record PublicRuntimeArgumentValidation(bool Accepted, string? ErrorCode = null, string? Message = null);

public sealed record PublicCapabilityDescriptor(
    string Id,
    string Family,
    PublicCapabilityClassification Classification,
    PublicCapabilityKind Kind,
    bool RequiresSession,
    bool RequiresExactProfile,
    string ApiRoute,
    string CliRoute,
    string RuntimeValidation,
    string Description,
    IReadOnlyList<string>? HandleTypes = null);

public static class PublicCapabilityRegistry
{
    public const string ProtocolVersion = "0.1";

    public static readonly IReadOnlyList<PublicCapabilityDescriptor> All = new PublicCapabilityDescriptor[]
    {
        new("session.plan", "session", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Action, false, true, "session.plan", "/plan", "RUNTIME_PASS", "Compile a LaunchSpec without starting OMSI."),
        new("session.start", "session", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Action, false, true, "session.start", "/new | /saved:<situation.osn> | /spec:<spec.json>", "RUNTIME_PASS", "Start a transactional managed OMSI session."),
        new("session.status", "session", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, false, "session.status", "session status", "RUNTIME_PASS", "Read the semantic lifecycle state."),
        new("session.stop", "session", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Action, true, false, "session.stop", "session stop", "RUNTIME_PASS", "End the session: OMSI is terminated (forced, no OMSI shutdown routine runs) and every session-owned file is restored exactly."),
        new("session.recover", "session", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Action, false, true, "session.recover", "/recovery-status | /recover", "RUNTIME_PASS", "Recover a stale durable transaction."),
        new("time.read", "time", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.time.read", "time get", "RUNTIME_PASS", "Read OMSI clock and calendar fields."),
        new("time.set", "time", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Write, true, true, "runtime.time.set", "time set", "RUNTIME_PASS", "Set the OMSI clock through the profiled native operation."),
        new("weather.read", "weather", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.weather.read", "weather get", "RUNTIME_PASS", "Read profiled weather state."),
        new("weather.set", "weather", PublicCapabilityClassification.Unsupported, PublicCapabilityKind.Write, true, true, "runtime.weather.set", "weather set", "RUNTIME_REJECTED", "Recognized for diagnostics but rejected until OMSI's native weather-application lifecycle is reconciled."),
        new("weather.actual.read", "weather", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Read, true, true, "runtime.weather.actual.read", "weather actual get", "RUNTIME_PASS", "Read actual/ICAO weather-controller state."),
        new("map.read", "map", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.map.read", "map get", "RUNTIME_PASS", "Read stable map identity and load state."),
        new("camera.read", "camera", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.camera.read", "camera get", "RUNTIME_PASS", "Read camera family and supported fields."),
        new("camera.set", "camera", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Write, true, true, "runtime.camera.set", "camera set", "RUNTIME_PASS", "Set supported camera scalars such as FOV."),
        new("camera.lock", "camera", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Action, true, true, "runtime.camera.lock", "camera lock --family=<0..3> [--preset=<n>]", "STATICALLY_VALIDATED", "Session-scoped camera-family policy; optional driver/passenger preset while preserving head-look and FOV."),
        new("vehicles.list", "vehicles", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.road-vehicles.list", "vehicles list", "RUNTIME_PASS", "List opaque RoadVehicle handles.", new[] { "RoadVehicle" }),
        new("vehicles.get", "vehicles", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.road-vehicle.read", "vehicles get", "RUNTIME_PASS", "Read an opaque RoadVehicle snapshot.", new[] { "RoadVehicle" }),
        new("vehicles.summary", "vehicles", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.road-vehicles.read", "vehicles summary", "RUNTIME_PASS", "Read the road-vehicle collection summary (counts and player state) without handles."),
        new("vehicles.spawn", "vehicles", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Action, true, true, "runtime.road-vehicles.spawn", "vehicles spawn --model=<Vehicles\\...\\*.bus>", "RUNTIME_PASS", "Create one RoadVehicle from a canonical .bus identity; this does not assign PlayerVehicle.", new[] { "RoadVehicle" }),
        new("vehicles.place-random", "vehicles", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Action, true, true, "runtime.road-vehicles.place-random", "vehicles place-random", "RUNTIME_PASS", "Invoke profiled PlaceRandomBus."),
        new("player.read", "player", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.player-vehicle.read", "player get", "RUNTIME_PASS", "Read the active PlayerVehicle or semantic null."),
        new("humans.list", "humans", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Read, true, true, "runtime.humans.list", "humans list", "RUNTIME_PASS", "List opaque human handles.", new[] { "Human" }),
        new("humans.get", "humans", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Read, true, true, "runtime.human.read", "humans get", "RUNTIME_PASS", "Read a human snapshot.", new[] { "Human" }),
        new("humans.summary", "humans", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Read, true, true, "runtime.humans.read", "humans summary", "RUNTIME_PASS", "Read the human collection summary (counts) without handles."),
        new("timetable.read", "timetable", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.timetable.read", "timetable get", "RUNTIME_PASS", "Read timetable manager state."),
        new("scripts.numeric", "scripts", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Write, true, true, "runtime.vehicle.variable.*", "scripts variable list | scripts variable get | scripts variable set", "RUNTIME_PASS", "List, read and write numeric script variables.", new[] { "RoadVehicle" }),
        new("scripts.string.read", "scripts", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Read, true, true, "runtime.vehicle.string-variable.*", "scripts string list | scripts string get", "RUNTIME_PASS", "List and read string script variables.", new[] { "RoadVehicle" }),
        new("constants", "constants", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.vehicle.constant.*", "constants list | constants get", "RUNTIME_PASS", "List and read vehicle constants.", new[] { "RoadVehicle" }),
        new("curves", "curves", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.vehicle.curve.*", "curves list | curves evaluate", "RUNTIME_PASS", "List and evaluate vehicle curves.", new[] { "RoadVehicle" }),
        new("hof.read", "hof", PublicCapabilityClassification.PublicStableBeta, PublicCapabilityKind.Read, true, true, "runtime.vehicle.hofs.read", "hof get", "RUNTIME_PASS", "Read vehicle HOF metadata.", new[] { "RoadVehicle" }),
        new("drivers.read", "drivers", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Read, true, true, "runtime.drivers.read", "drivers list", "RUNTIME_PASS", "Read driver records."),
        new("tickets.read", "tickets", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Read, true, true, "runtime.tickets.read", "tickets get", "RUNTIME_PASS", "Read ticket-pack records."),
        new("d3d.texture", "d3d", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Action, true, true, "runtime.d3d.texture.*", "/runtime:d3d.status | /runtime:d3d.texture.create | /runtime:d3d.texture.describe | /runtime:d3d.texture.update | /runtime:d3d.texture.release", "RUNTIME_PASS", "Create, update, describe and release opaque D3D texture resources.", new[] { "D3DTexture" }),
        new("events.read", "events", PublicCapabilityClassification.PublicExperimental, PublicCapabilityKind.Event, true, false, "session.events", "events read | events watch", "RUNTIME_PASS", "Read or observe bounded session runtime events."),
        new("internal.make-basic", "vehicles", PublicCapabilityClassification.InternalOnly, PublicCapabilityKind.Action, true, true, "internal.road-vehicles.make-basic", "", "RUNTIME_PASS", "Profiled research primitive without PlayerVehicle assignment semantics."),
        new("calendar.set-actual-date-time", "time", PublicCapabilityClassification.Unsupported, PublicCapabilityKind.Write, true, true, "", "", "UNSUPPORTED", "Native ABI and postconditions are not closed for Beta."),
        new("player.assign-headless", "player", PublicCapabilityClassification.Unsupported, PublicCapabilityKind.Action, true, true, "", "", "UNSUPPORTED", "Deterministic headless PlayerVehicle assignment is a future extension.")
    };

    // The control plane accepts concrete runtime operation IDs, while the
    // catalog intentionally groups some related operations behind a wildcard.
    // This explicit set prevents an inactive-session error from masking an
    // invalid public command name.
    private static readonly HashSet<string> PublicRuntimeOperations = new(StringComparer.Ordinal)
    {
        "time.read", "time.set", "weather.read", "weather.set", "weather.actual.read",
        "map.read", "camera.read", "camera.set", "camera.lock", "camera.unlock", "road-vehicles.list", "road-vehicle.read", "road-vehicles.read", "road-vehicles.spawn",
        "road-vehicles.place-random", "player-vehicle.read", "humans.list", "human.read", "humans.read",
        "timetable.read", "timetable.tracks.list", "timetable.trips.list", "timetable.lines.list",
        "timetable.rv-files.list", "timetable.track-entries.list", "timetable.bus-stops.list",
        "timetable.station-links.list", "timetable.tours.list", "timetable.profiles.list",
        "timetable.tour-entries.list", "timetable.logs.read", "drivers.read", "tickets.read",
        "vehicle.hofs.read", "vehicle.variables.list", "vehicle.variable.get", "vehicle.variable.set",
        "vehicle.string-variables.list", "vehicle.string-variable.get", "vehicle.constants.list",
        "vehicle.constant.get", "vehicle.curves.list", "vehicle.curve.evaluate", "d3d.status",
        "d3d.texture.create", "d3d.texture.describe", "d3d.texture.update", "d3d.texture.release"
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<PublicRuntimeArgumentDescriptor>> RuntimeArguments =
        new Dictionary<string, IReadOnlyList<PublicRuntimeArgumentDescriptor>>(StringComparer.Ordinal)
        {
            ["road-vehicle.read"] = Required("handle", "Session-scoped RoadVehicle handle."),
            ["road-vehicles.spawn"] = Required("model", "Canonical .bus identity below Vehicles\\."),
            ["camera.lock"] = Required("family", "Camera family: driver=0, passenger=1, external=2, map=3."),
            ["human.read"] = Required("handle", "Session-scoped Human handle."),
            ["vehicle.hofs.read"] = Required("handle", "Session-scoped RoadVehicle handle."),
            ["vehicle.variables.list"] = Required("handle", "Session-scoped RoadVehicle handle."),
            ["vehicle.variable.get"] = Required("handle", "Session-scoped RoadVehicle handle.", "name", "Open numeric script-variable name."),
            ["vehicle.variable.set"] = Required("handle", "Session-scoped RoadVehicle handle.", "name", "Open numeric script-variable name.", "value", "Finite numeric script-variable value."),
            ["vehicle.string-variables.list"] = Required("handle", "Session-scoped RoadVehicle handle."),
            ["vehicle.string-variable.get"] = Required("handle", "Session-scoped RoadVehicle handle.", "name", "Open string script-variable name."),
            ["vehicle.constants.list"] = Required("handle", "Session-scoped RoadVehicle handle."),
            ["vehicle.constant.get"] = Required("handle", "Session-scoped RoadVehicle handle.", "name", "Open constant name."),
            ["vehicle.curves.list"] = Required("handle", "Session-scoped RoadVehicle handle."),
            ["vehicle.curve.evaluate"] = Required("handle", "Session-scoped RoadVehicle handle.", "name", "Open curve name.", "x", "Finite curve input."),
            ["d3d.texture.create"] = Required("width", "Texture width in pixels.", "height", "Texture height in pixels.", "format", "Supported D3D texture format."),
            ["d3d.texture.describe"] = Required("handle", "Session-scoped D3D texture handle."),
            ["d3d.texture.update"] = Required("handle", "Session-scoped D3D texture handle.", "width", "Updated rectangle width in pixels.", "height", "Updated rectangle height in pixels.", "pixels_base64", "Base64 texture pixel payload."),
            ["d3d.texture.release"] = Required("handle", "Session-scoped D3D texture handle.")
        };

    public static bool IsPublicRuntimeOperation(string operation) => PublicRuntimeOperations.Contains(operation);

    // Every concrete runtime operation a public frontend may forward. Internal
    // research primitives are deliberately absent from this list.
    public static IReadOnlyCollection<string> PublicRuntimeOperationIds => PublicRuntimeOperations;

    // Result values that would disclose native layout never cross the public
    // boundary, whichever bridge produced them.
    public static bool IsInternalResultKey(string key) =>
        key.StartsWith("internal_", StringComparison.Ordinal) ||
        key.EndsWith("_address", StringComparison.Ordinal) ||
        key.EndsWith("_pointer", StringComparison.Ordinal) ||
        key.EndsWith("_vmt", StringComparison.Ordinal);

    public static IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation) =>
        RuntimeArguments.TryGetValue(operation, out var arguments) ? arguments : Array.Empty<PublicRuntimeArgumentDescriptor>();

    public static PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string>? arguments)
    {
        if (!IsPublicRuntimeOperation(operation))
            return new(false, "OL_E_RUNTIME_OPERATION_UNKNOWN", "The runtime operation is not part of the public Beta control surface.");

        var missing = GetRuntimeArguments(operation)
            .Where(argument => argument.Required && (arguments is null || !arguments.TryGetValue(argument.Name, out var value) || string.IsNullOrWhiteSpace(value)))
            .Select(argument => argument.Name)
            .ToArray();
        return missing.Length == 0
            ? new(true)
            : new(false, "OL_E_RUNTIME_ARGUMENT_REQUIRED", "Runtime operation requires: " + string.Join(", ", missing) + ".");
    }

    private static IReadOnlyList<PublicRuntimeArgumentDescriptor> Required(params string[] values)
    {
        if (values.Length % 2 != 0) throw new ArgumentException("Argument metadata requires name/description pairs.", nameof(values));
        var result = new PublicRuntimeArgumentDescriptor[values.Length / 2];
        for (var index = 0; index < values.Length; index += 2)
            result[index / 2] = new PublicRuntimeArgumentDescriptor(values[index], true, values[index + 1]);
        return result;
    }
}
