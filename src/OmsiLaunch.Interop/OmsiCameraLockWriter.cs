using System.Globalization;

namespace OmsiLaunch.Interop;

// Session-scoped camera policy. It writes only the family selector and the
// OMSI-owned active driver/passenger preset; head-look and FOV stay untouched.
public sealed class OmsiCameraLockWriter
{
    private readonly IOmsiMemory memory;
    private readonly IReadOnlyDictionary<string, uint> globals;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, uint>> layouts;

    public OmsiCameraLockWriter(IOmsiMemory memory, IReadOnlyDictionary<string, uint> globals, IReadOnlyDictionary<string, IReadOnlyDictionary<string, uint>> layouts)
    {
        this.memory = memory; this.globals = globals; this.layouts = layouts;
    }

    public async ValueTask ApplyAsync(int family, int? preset, CancellationToken cancellationToken = default)
    {
        if (family is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(family), "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        if (preset is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(preset), "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        if (preset is not null && family is not 0 and not 1)
            throw new InvalidOperationException("OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED");

        await memory.WriteValueAsync(globals["CameraFamily"], family, cancellationToken).ConfigureAwait(false);
        if (preset is null) return;

        var playerIndex = await memory.ReadValueAsync<int>(globals["PlayerVehicleIndex"], cancellationToken).ConfigureAwait(false);
        var list = await memory.ReadPointer32Async(globals["RoadVehicles"], cancellationToken).ConfigureAwait(false);
        if (list == 0) throw new InvalidOperationException("OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE");
        var listLayout = Layout("RoadVehicleList");
        var wrapper = await memory.ReadPointer32Async(list + listLayout["Items"], cancellationToken).ConfigureAwait(false);
        if (wrapper == 0) throw new InvalidOperationException("OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE");
        var data = await memory.ReadPointer32Async(wrapper + 4, cancellationToken).ConfigureAwait(false);
        var count = await memory.ReadValueAsync<int>(wrapper + 8, cancellationToken).ConfigureAwait(false);
        if (data == 0 || playerIndex < 0 || playerIndex >= count) throw new InvalidOperationException("OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE");
        var player = await memory.ReadPointer32Async(data + checked((uint)playerIndex * 4), cancellationToken).ConfigureAwait(false);
        if (player == 0) throw new InvalidOperationException("OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE");
        var vehicleLayout = Layout("RoadVehicle");
        await memory.WriteValueAsync(player + vehicleLayout[family == 0 ? "ActiveDriverCamera" : "ActivePassengerCamera"], preset.Value, cancellationToken).ConfigureAwait(false);
    }

    public static IReadOnlyDictionary<string, string> Describe(int family, int? preset) => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["locked"] = "true",
        ["family"] = family.ToString(CultureInfo.InvariantCulture),
        ["preset"] = preset?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        ["head_look"] = "preserved",
        ["field_of_view"] = "preserved"
    };

    private IReadOnlyDictionary<string, uint> Layout(string name) => layouts.TryGetValue(name, out var layout)
        ? layout : throw new InvalidOperationException("Missing profiled layout: " + name);
}
