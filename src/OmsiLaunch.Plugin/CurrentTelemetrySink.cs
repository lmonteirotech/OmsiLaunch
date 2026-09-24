using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;

namespace OmsiLaunch.Plugin;

internal static class CurrentTelemetrySink
{
    private const int Capacity = 4096;
    private const int LengthOffset = 0;
    private const int SequenceOffset = 4;
    private const int DataOffset = 8;
    // Plugin callbacks can emit on more than one OMSI callback path. Serialize
    // the length/payload commit so a later event cannot overwrite an earlier one mid-read.
    private static readonly object gate = new();
    private static int sequence;
    public static void Emit(string name, IReadOnlyDictionary<string, string> data)
    {
        var mappingName = Environment.GetEnvironmentVariable("OMSILAUNCH_TELEMETRY_NAME");
        if (string.IsNullOrWhiteSpace(mappingName)) return;
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { name, data }));
        if (payload.Length > Capacity - DataOffset) return;
        try
        {
            lock (gate)
            {
                using var mapping = MemoryMappedFile.OpenExisting(mappingName, MemoryMappedFileRights.ReadWrite);
                using var view = mapping.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.ReadWrite);
                // Commit protocol: invalidate, write payload, publish a new
                // sequence, then publish the length last. A reader that sees the
                // same length and sequence before and after its copy has a
                // consistent sample; identical consecutive events still differ
                // by sequence and are therefore never collapsed.
                view.Write(LengthOffset, 0); view.Flush();
                view.WriteArray(DataOffset, payload, 0, payload.Length); view.Flush();
                view.Write(SequenceOffset, ++sequence); view.Flush();
                view.Write(LengthOffset, payload.Length); view.Flush();
            }
        }
        catch (FileNotFoundException) { }
        catch (UnauthorizedAccessException) { }
    }
}
