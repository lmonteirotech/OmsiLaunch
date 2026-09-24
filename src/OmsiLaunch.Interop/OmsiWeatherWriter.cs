using System.Globalization;

namespace OmsiLaunch.Interop;

// Profile-backed scalar weather writer. Complex weather assets, cloud object
// pointers and ActualWeather Delphi strings deliberately remain outside this
// safe release surface.
public sealed class OmsiWeatherWriter
{
    private readonly IOmsiMemory memory;
    private readonly IReadOnlyDictionary<string, uint> globals;
    private readonly IReadOnlyDictionary<string, uint> layout;

    public OmsiWeatherWriter(IOmsiMemory memory, IReadOnlyDictionary<string, uint> globals, IReadOnlyDictionary<string, IReadOnlyDictionary<string, uint>> layouts)
    {
        this.memory = memory;
        this.globals = globals;
        layout = layouts.TryGetValue("Weather", out var value) ? value : throw new InvalidOperationException("Missing profiled weather layout.");
    }

    public async ValueTask WriteAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        if (!globals.TryGetValue("Weather", out var global)) throw new InvalidOperationException("Missing profiled weather global.");
        var address = await memory.ReadPointer32Async(global, cancellationToken).ConfigureAwait(false);
        if (address == 0) throw new InvalidOperationException("OMSI weather object is unavailable.");
        var written = 0;
        foreach (var pair in values)
        {
            switch (pair.Key)
            {
                case "fog_density": await WriteFloatAsync(address, "FogDensity", pair.Value, 0, 100_000, cancellationToken).ConfigureAwait(false); break;
                case "lightness": await WriteFloatAsync(address, "Lightness", pair.Value, 0, 1, cancellationToken).ConfigureAwait(false); break;
                case "primary_light_factor": await WriteFloatAsync(address, "PrimaryLightFactor", pair.Value, 0, 1, cancellationToken).ConfigureAwait(false); break;
                case "secondary_light_factor": await WriteFloatAsync(address, "SecondaryLightFactor", pair.Value, 0, 1, cancellationToken).ConfigureAwait(false); break;
                case "ambient_light_factor": await WriteFloatAsync(address, "AmbientLightFactor", pair.Value, 0, 1, cancellationToken).ConfigureAwait(false); break;
                case "cloud_transparency": await WriteFloatAsync(address, "CloudTransparency", pair.Value, 0, 1, cancellationToken).ConfigureAwait(false); break;
                // ActualWind* is a derived per-frame projection. Persist the
                // requested weather through ActWeather, which OMSI consumes on
                // its normal weather update rather than fighting it every tick.
                case "wind_speed": await WriteFloatAsync(address, "ActiveWindSpeed", pair.Value, 0, 200, cancellationToken).ConfigureAwait(false); break;
                case "wind_direction": await WriteFloatAsync(address, "ActiveWindDirection", pair.Value, 0, 360, cancellationToken).ConfigureAwait(false); break;
                case "relative_humidity": await WriteFloatAsync(address, "RelativeHumidity", pair.Value, 0, 1, cancellationToken).ConfigureAwait(false); break;
                case "absolute_humidity": await WriteFloatAsync(address, "AbsoluteHumidity", pair.Value, 0, 100, cancellationToken).ConfigureAwait(false); break;
                case "precipitation_set": await memory.WriteValueAsync(address + layout["PrecipitationSet"], ParseBoolean(pair.Value), cancellationToken).ConfigureAwait(false); break;
                case "wet_ground": await memory.WriteValueAsync(address + layout["WetGround"], ParseBoolean(pair.Value), cancellationToken).ConfigureAwait(false); break;
                default: throw new InvalidOperationException("OL_E_RUNTIME_SETTING_UNAVAILABLE: " + pair.Key);
            }
            written++;
        }
        if (written == 0) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
    }

    private async ValueTask WriteFloatAsync(uint address, string field, string raw, float minimum, float maximum, CancellationToken cancellationToken)
    {
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || float.IsNaN(value) || float.IsInfinity(value) || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(field, "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        await memory.WriteValueAsync(address + layout[field], value, cancellationToken).ConfigureAwait(false);
    }

    private static bool ParseBoolean(string raw) => raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw == "1"
        ? true
        : raw.Equals("false", StringComparison.OrdinalIgnoreCase) || raw == "0"
            ? false
            : throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", nameof(raw));
}
