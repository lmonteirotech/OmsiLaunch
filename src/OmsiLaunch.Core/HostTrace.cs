using System.Diagnostics;

namespace OmsiLaunch.Core;

internal sealed class HostTrace
{
    // Session diagnostics are retained for the most recent sessions only. Older
    // session-scoped files are removed when a new session trace is created.
    internal const int RetainedSessions = 50;
    private readonly string path; private readonly Stopwatch clock = Stopwatch.StartNew(); private readonly Guid sessionId; private readonly object gate = new();
    public HostTrace(string installationRoot, Guid sessionId) { this.sessionId = sessionId; var directory = System.IO.Path.Combine(installationRoot, ".omsilaunch", "diagnostics"); Directory.CreateDirectory(directory); Prune(directory, RetainedSessions); path = System.IO.Path.Combine(directory, sessionId.ToString("N") + "-host.log"); Write("TRACE_CREATED"); }
    public void Write(string boundary, string? data = null) { lock (gate) File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O}\t{clock.ElapsedMilliseconds}\t{sessionId:D}\t{Environment.CurrentManagedThreadId}\t{boundary}\t{data ?? ""}{Environment.NewLine}"); }
    public string Path => path;

    // Only files that carry a session prefix ("<32 hex>-*") are candidates;
    // validation evidence and unrelated files in the directory are untouched.
    internal static void Prune(string directory, int keep)
    {
        try
        {
            var sessions = Directory.EnumerateFiles(directory, "*-host.log", SearchOption.TopDirectoryOnly)
                .Select(file => new FileInfo(file))
                .Where(file => file.Name.Length >= 41 && file.Name[32] == '-' && file.Name[..32].All(Uri.IsHexDigit))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Skip(keep)
                .Select(file => file.Name[..32])
                .ToArray();
            foreach (var session in sessions)
                foreach (var stale in Directory.EnumerateFiles(directory, session + "-*", SearchOption.TopDirectoryOnly))
                    try { File.Delete(stale); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
