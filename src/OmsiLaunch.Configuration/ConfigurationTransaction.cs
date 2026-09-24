using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OmsiLaunch.Configuration;

// OMSI writes its configuration files with the Windows ANSI code page. On .NET,
// the default encoding is UTF-8, which would turn every accented byte into U+FFFD
// when a file is patched. BOM-marked UTF-16/UTF-8 files keep their encoding.
public static class OmsiText
{
    public static readonly Encoding Ansi = CreateAnsi();
    private static Encoding CreateAnsi()
    {
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding(1252); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException) { return Encoding.Latin1; }
    }
}

public sealed record FileSnapshot(string Path, bool Existed, string Sha256, byte[] Bytes, long? LastWriteTimeUtcTicks = null, long? CreationTimeUtcTicks = null, int? Attributes = null, bool SessionDeletion = false);
public sealed record JournalFile(string RelativePath, bool Existed, string Sha256, string BackupPath, string? AppliedSha256 = null, long? LastWriteTimeUtcTicks = null, long? CreationTimeUtcTicks = null, int? Attributes = null, bool SessionDeletion = false);
public enum TransactionState { Prepared, Applied, RuntimeDeployed, HandoffCreated, ProcessStarted, ProcessExited, Restoring, Restored, Completed }
public sealed record TransactionJournal(Guid SessionId, TransactionState State, IReadOnlyList<JournalFile> Files, int? ProcessId, long? ProcessStartFileTimeUtc, string? ExecutablePath);

public interface IConfigurationSnapshot { IReadOnlyList<FileSnapshot> Files { get; } }
public interface IConfigurationOverlay { IReadOnlyCollection<IConfigSemanticPatch> Patches { get; } }
public interface IConfigurationJournal { string SessionId { get; } bool RestorePending { get; } Task WriteAsync(IConfigurationSnapshot snapshot, CancellationToken cancellationToken = default); Task MarkRestoredAsync(CancellationToken cancellationToken = default); }
public interface IConfigurationRecovery { Task<bool> HasPendingRecoveryAsync(CancellationToken cancellationToken = default); Task RestorePendingAsync(CancellationToken cancellationToken = default); }
public interface IConfigurationTransaction : IAsyncDisposable { IReadOnlyList<FileSnapshot> Snapshots { get; } IConfigurationSnapshot Snapshot { get; } Task ApplyAsync(CancellationToken cancellationToken = default); Task RestoreAsync(CancellationToken cancellationToken = default); }
public interface IConfigSemanticPatch { string FileName { get; } byte[] Apply(byte[] source); }

public sealed class BracketTokenDocument
{
    private readonly byte[] original;
    private readonly Encoding encoding;
    private readonly string newline;
    private readonly List<string> lines;

    private BracketTokenDocument(byte[] original, Encoding encoding, string newline, List<string> lines) { this.original = original; this.encoding = encoding; this.newline = newline; this.lines = lines; }
    public static BracketTokenDocument Parse(byte[] bytes)
    {
        var encoding = DetectEncoding(bytes);
        var text = encoding.GetString(StripBom(bytes));
        return new BracketTokenDocument(bytes, encoding, text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n", text.Replace("\r\n", "\n").Split('\n').ToList());
    }
    public bool HasToken(string token) => FindToken(token) >= 0;
    public string? GetValue(string token) { var index = FindToken(token); return index >= 0 && index + 1 < lines.Count && !IsToken(lines[index + 1]) ? lines[index + 1] : null; }
    public IReadOnlyList<string> GetValues(string token, int count)
    {
        var index = FindToken(token);
        if (index < 0) return Array.Empty<string>();
        var values = new List<string>();
        for (var valueIndex = index + 1; valueIndex < lines.Count && values.Count < count && !IsToken(lines[valueIndex]); valueIndex++) values.Add(lines[valueIndex]);
        return values;
    }
    public void SetPresence(string token, bool present) { var index = FindToken(token); if (present && index < 0) lines.Add("[" + token + "]"); if (!present && index >= 0) RemoveTokenAt(index); }
    public void SetValue(string token, string value)
    {
        var index = FindToken(token);
        if (index < 0) { lines.Add("[" + token + "]"); lines.Add(value); return; }
        if (index + 1 < lines.Count && !IsToken(lines[index + 1])) lines[index + 1] = value; else lines.Insert(index + 1, value);
    }
    public void SetValues(string token, IReadOnlyList<string> values)
    {
        var index = FindToken(token);
        if (index < 0)
        {
            lines.Add("[" + token + "]");
            lines.AddRange(values);
            return;
        }

        var removeAt = index + 1;
        while (removeAt < lines.Count && !IsToken(lines[removeAt])) lines.RemoveAt(removeAt);
        lines.InsertRange(removeAt, values);
    }
    public byte[] Serialize(bool unchanged)
    {
        if (unchanged) return original;
        var text = string.Join(newline, lines);
        return WithBom(encoding, encoding.GetBytes(text));
    }
    private int FindToken(string token) => lines.FindIndex(x => string.Equals(x.Trim(), "[" + token + "]", StringComparison.OrdinalIgnoreCase));
    private void RemoveTokenAt(int index) { lines.RemoveAt(index); if (index < lines.Count && !IsToken(lines[index])) lines.RemoveAt(index); }
    private static bool IsToken(string value) => value.Trim().StartsWith("[", StringComparison.Ordinal) && value.Trim().EndsWith("]", StringComparison.Ordinal);
    private static Encoding DetectEncoding(byte[] bytes) => bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE ? Encoding.Unicode : bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? new UTF8Encoding(true) : OmsiText.Ansi;
    private static byte[] StripBom(byte[] bytes) { var offset = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE ? 2 : bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0; return bytes[offset..]; }
    private static byte[] WithBom(Encoding encoding, byte[] body) { var bom = encoding.GetPreamble(); return bom.Length == 0 ? body : bom.Concat(body).ToArray(); }
}

public sealed record TokenPatch(string FileName, string Token, string? Value, bool? Presence = null) : IConfigSemanticPatch
{
    public byte[] Apply(byte[] source)
    {
        var document = BracketTokenDocument.Parse(source);
        if (Presence is { } present) document.SetPresence(Token, present); else if (Value is not null) document.SetValue(Token, Value);
        return document.Serialize(false);
    }
}

public sealed class OptionsDocument
{
    private readonly BracketTokenDocument document;
    private bool changed;
    public OptionsDocument(byte[] bytes) => document = BracketTokenDocument.Parse(bytes);
    public void SetBoolean(string semanticName, bool value)
    {
        var (token, inverted) = semanticName switch {
            "collision_player_terrain" => ("no_collision_terrain", true), "collision_player_vehicle" => ("no_collision_vehToVeh", true),
            "collision_pedestrians" => ("no_collision_pedastrians", true), "reduced_multithreading_calculate" => ("no_multithreading_calculate", false),
            "reduced_multithreading_texload" => ("no_multithreading_texload", false), _ => (semanticName, false) };
        document.SetPresence(token, inverted ? !value : value); changed = true;
    }
    public void SetValue(string semanticName, string value) { document.SetValue(semanticName, value); changed = true; }
    public void SetAiMaxCountRandom(int roadTraffic, int humans)
    {
        var values = GetAiMaxCountRandomValues();
        values[0] = roadTraffic.ToString(System.Globalization.CultureInfo.InvariantCulture);
        values[1] = humans.ToString(System.Globalization.CultureInfo.InvariantCulture);
        document.SetValues("AIMaxCountRandom", values); changed = true;
    }
    public void SetAiMaxCountRandomComponent(int index, int value)
    {
        if (index is < 0 or > 8) throw new ArgumentOutOfRangeException(nameof(index));
        var values = GetAiMaxCountRandomValues();
        values[index] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        document.SetValues("AIMaxCountRandom", values); changed = true;
    }
    private List<string> GetAiMaxCountRandomValues()
    {
        // OMSI stores AIMaxCountRandom as a multiline block. Keep every
        // existing component and only add the missing standard components.
        var values = document.GetValues("AIMaxCountRandom", int.MaxValue).ToList();
        while (values.Count < 9) values.Add("0");
        return values;
    }
    public byte[] Serialize() => document.Serialize(!changed);
}
public sealed record KeyboardBinding(string OmsiEventId, int Key, bool Ctrl, bool Shift);
public sealed class KeyboardDocument
{
    private readonly byte[] original; private readonly Encoding encoding; private readonly string newline; private readonly List<string> lines; private bool changed;
    public KeyboardDocument(byte[] bytes)
    {
        original = bytes; encoding = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE ? Encoding.Unicode : OmsiText.Ansi;
        var offset = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE ? 2 : 0; var text = encoding.GetString(bytes[offset..]); newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n"; lines = text.Replace("\r\n", "\n").Split('\n').ToList();
    }
    public IReadOnlyList<KeyboardBinding> Bindings => ReadBindings().ToArray();
    public void SetBinding(KeyboardBinding binding)
    {
        var index = FindEntry(binding.OmsiEventId);
        var flags = (binding.Ctrl ? 4 : 0) | (binding.Shift ? 2 : 0);
        if (index < 0) { lines.Add("[entry]"); lines.Add(binding.OmsiEventId); lines.Add(binding.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)); lines.Add(flags.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        else { lines[index + 2] = binding.Key.ToString(System.Globalization.CultureInfo.InvariantCulture); var prior = int.TryParse(lines[index + 3], out var value) ? value : 0; lines[index + 3] = ((prior & ~6) | flags).ToString(System.Globalization.CultureInfo.InvariantCulture); }
        changed = true;
    }
    public byte[] Serialize() { if (!changed) return original; var body = encoding.GetBytes(string.Join(newline, lines)); var bom = encoding.GetPreamble(); return bom.Length == 0 ? body : bom.Concat(body).ToArray(); }
    public byte[] SerializeNoOp() => original;
    private IEnumerable<KeyboardBinding> ReadBindings()
    {
        for (var index = 0; index + 3 < lines.Count; index++) if (string.Equals(lines[index].Trim(), "[entry]", StringComparison.OrdinalIgnoreCase) && int.TryParse(lines[index + 2], out var key) && int.TryParse(lines[index + 3], out var flags)) yield return new(lines[index + 1], key, (flags & 4) != 0, (flags & 2) != 0);
    }
    private int FindEntry(string eventId)
    {
        for (var index = 0; index + 3 < lines.Count; index++) if (string.Equals(lines[index].Trim(), "[entry]", StringComparison.OrdinalIgnoreCase) && string.Equals(lines[index + 1], eventId, StringComparison.OrdinalIgnoreCase)) return index;
        return -1;
    }
}
public sealed record ControllerDevice(string Name, bool Active, string ForceFeedbackScale, string ForceFeedbackCentering);
public sealed class GameControllerDocument
{
    private readonly byte[] original; private readonly Encoding encoding; private readonly string newline; private readonly List<string> lines; private bool changed;
    public GameControllerDocument(byte[] bytes)
    {
        original = bytes; encoding = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE ? Encoding.Unicode : OmsiText.Ansi;
        var offset = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE ? 2 : 0; var text = encoding.GetString(bytes[offset..]); newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n"; lines = text.Replace("\r\n", "\n").Split('\n').ToList();
    }
    public IReadOnlyList<ControllerDevice> Devices => ReadDevices().ToArray();
    public void SetActive(string name, bool active) { var ctrl = FindController(name); if (ctrl < 0) throw new KeyNotFoundException("Controller not found: " + name); lines[ctrl + 2] = active ? "1" : "0"; changed = true; }
    public void SetForceFeedback(string name, string scale, string centering)
    {
        var ctrl = FindController(name); if (ctrl < 0) throw new KeyNotFoundException("Controller not found: " + name); var ff = FindSection(ctrl, "[FFScale]"); if (ff < 0 || ff + 2 >= lines.Count) throw new InvalidDataException("Controller has no valid FFScale block: " + name); lines[ff + 1] = scale; lines[ff + 2] = centering; changed = true;
    }
    public byte[] Serialize() { if (!changed) return original; var body = encoding.GetBytes(string.Join(newline, lines)); var bom = encoding.GetPreamble(); return bom.Length == 0 ? body : bom.Concat(body).ToArray(); }
    public byte[] SerializeNoOp() => original;
    private IEnumerable<ControllerDevice> ReadDevices()
    {
        for (var index = 0; index + 2 < lines.Count; index++)
        {
            if (!string.Equals(lines[index].Trim(), "[ctrl]", StringComparison.OrdinalIgnoreCase) || !int.TryParse(lines[index + 2], out var active)) continue;
            var ff = FindSection(index, "[FFScale]"); if (ff >= 0 && ff + 2 < lines.Count) yield return new(lines[index + 1], active != 0, lines[ff + 1], lines[ff + 2]);
        }
    }
    private int FindController(string name) { for (var index = 0; index + 2 < lines.Count; index++) if (string.Equals(lines[index].Trim(), "[ctrl]", StringComparison.OrdinalIgnoreCase) && string.Equals(lines[index + 1], name, StringComparison.OrdinalIgnoreCase)) return index; return -1; }
    private int FindSection(int start, string section) { for (var index = start + 1; index < lines.Count; index++) { if (index > start + 1 && string.Equals(lines[index].Trim(), "[ctrl]", StringComparison.OrdinalIgnoreCase)) return -1; if (string.Equals(lines[index].Trim(), section, StringComparison.OrdinalIgnoreCase)) return index; } return -1; }
}

public sealed record AiMaxCountRandomPatch(int Index, int Value) : IConfigSemanticPatch
{
    public string FileName => "options.cfg";
    public byte[] Apply(byte[] source) { var document = new OptionsDocument(source); document.SetAiMaxCountRandomComponent(Index, Value); return document.Serialize(); }
}

// OMSI stores this UI choice as two independent negative presence tokens.
public sealed record ReducedMultithreadingPatch(bool Enabled) : IConfigSemanticPatch
{
    public string FileName => "options.cfg";
    public byte[] Apply(byte[] source)
    {
        var document = BracketTokenDocument.Parse(source);
        document.SetPresence("no_multithreading_calculate", Enabled);
        document.SetPresence("no_multithreading_texload", Enabled);
        return document.Serialize(false);
    }
}

public sealed record BooleanValueTokenPatch(string Token, bool Value, string TrueValue, string FalseValue) : IConfigSemanticPatch
{
    public string FileName => "options.cfg";
    public byte[] Apply(byte[] source)
    {
        var document = BracketTokenDocument.Parse(source);
        document.SetValue(Token, Value ? TrueValue : FalseValue);
        return document.Serialize(false);
    }
}

// The four smoke-system values are a single options.cfg block, not unrelated tokens.
public sealed record SmokeSystemsPatch(bool Enabled, int MaxPerEmitter, bool PlayerVehicleOnly, bool InReflections) : IConfigSemanticPatch
{
    public string FileName => "options.cfg";
    public byte[] Apply(byte[] source)
    {
        var document = BracketTokenDocument.Parse(source);
        document.SetValues("smokesystems", new[]
        {
            Enabled ? "1" : "0",
            MaxPerEmitter.ToString(System.Globalization.CultureInfo.InvariantCulture),
            PlayerVehicleOnly ? "1" : "0",
            InReflections ? "0" : "1"
        });
        return document.Serialize(false);
    }
}

// A restore note is a non-fatal observation produced while restoring. The
// session surfaces it as a diagnostic so nothing is removed or retained silently.
public sealed record RestoreNote(string Code, string RelativePath, string? Sha256);

public sealed class FileConfigurationTransaction : IConfigurationTransaction, IConfigurationSnapshot, IConfigurationRecovery
{
    private readonly string root; private readonly string journalPath; private readonly Dictionary<string, byte[]> overlays; private readonly Dictionary<string, string?> appliedHashes; private readonly HashSet<string> deletions; private readonly Guid sessionId;
    private readonly HashSet<string> backupRoots = new(StringComparer.OrdinalIgnoreCase); private readonly List<RestoreNote> restoreNotes = new();
    private TransactionState state = TransactionState.Prepared;
    public IReadOnlyList<FileSnapshot> Snapshots { get; private set; } = Array.Empty<FileSnapshot>(); public IConfigurationSnapshot Snapshot => this; IReadOnlyList<FileSnapshot> IConfigurationSnapshot.Files => Snapshots;
    public IReadOnlyList<RestoreNote> RestoreNotes => restoreNotes;
    public TransactionState State => state;
    public FileConfigurationTransaction(string installationRoot, IReadOnlyDictionary<string, byte[]> overlays, IEnumerable<string>? deletions = null, Guid? sessionId = null) { root = Path.GetFullPath(installationRoot); this.overlays = new(overlays, StringComparer.OrdinalIgnoreCase); appliedHashes = new(StringComparer.OrdinalIgnoreCase); ResetAppliedHashesForNewTransaction(); this.deletions = new(deletions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase); if (this.deletions.Overlaps(this.overlays.Keys)) throw new InvalidOperationException("A transaction file cannot be staged and deleted."); this.sessionId = sessionId ?? Guid.NewGuid(); journalPath = Path.Combine(root, ".omsilaunch", "journal.json"); }
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(journalPath)!);
        // The prepared journal, including its immutable source bytes, is the
        // recovery obligation. It must exist before an original is replaced.
        Snapshots = overlays.Keys.Concat(deletions).Distinct(StringComparer.OrdinalIgnoreCase).Select(SnapshotFile).ToArray(); await Persist(TransactionState.Prepared, cancellationToken).ConfigureAwait(false);
        foreach (var (relative, bytes) in overlays) await AtomicWrite(Path.Combine(root, relative), bytes, cancellationToken).ConfigureAwait(false);
        foreach (var relative in deletions) DeleteFile(Path.Combine(root, relative));
        await Persist(TransactionState.Applied, cancellationToken).ConfigureAwait(false);
    }
    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        // A session with no owned file mutations still has a durable lifecycle
        // journal. Completing that empty transaction is a verified no-op.
        if (Snapshots.Count == 0)
        {
            if (!File.Exists(journalPath)) return;
            await Persist(TransactionState.Restoring, cancellationToken).ConfigureAwait(false);
            await Persist(TransactionState.Restored, cancellationToken).ConfigureAwait(false);
            RemoveJournal(); CleanupBackups();
            return;
        }
        if (!File.Exists(journalPath)) throw new IOException("OL_E_RECOVERY_JOURNAL_MISSING");
        // Only a session whose OMSI process was actually started can have
        // produced by-products on a path it asked to keep absent.
        var processOwnedSession = state >= TransactionState.ProcessStarted;
        await Persist(TransactionState.Restoring, cancellationToken).ConfigureAwait(false);
        var retained = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Snapshots)
        {
            var path = Path.Combine(root, file.Path);
            if (file.Existed)
            {
                // The backup is verified against the snapshot fingerprint before
                // it is allowed to replace anything.
                if (!string.Equals(Hash(file.Bytes), file.Sha256, StringComparison.OrdinalIgnoreCase)) throw new IOException("OL_E_RECOVERY_BACKUP_CORRUPT: " + file.Path);
                await AtomicWrite(path, file.Bytes, cancellationToken).ConfigureAwait(false);
                RestoreMetadata(path, file);
            }
            else if (File.Exists(path))
            {
                var current = Hash(File.ReadAllBytes(path));
                if (appliedHashes.TryGetValue(file.Path, out var expectedHash) && !string.IsNullOrWhiteSpace(expectedHash))
                {
                    if (!string.Equals(current, expectedHash, StringComparison.OrdinalIgnoreCase)) throw new IOException("OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH: " + file.Path);
                    DeleteFile(path);
                }
                else if (file.SessionDeletion && processOwnedSession)
                {
                    // The transaction asked for this path to stay absent during
                    // the session and held the installation lease. Whatever OMSI
                    // wrote there is a session by-product; restoring original
                    // absence is exact restore. The removed content is recorded.
                    restoreNotes.Add(new("restore.session-artifact-removed", file.Path, current));
                    DeleteFile(path);
                }
                else if (file.SessionDeletion)
                {
                    // No OMSI process ran under this transaction, so the file
                    // came from outside the session. It is retained, reported,
                    // and never allowed to wedge recovery.
                    restoreNotes.Add(new("OL_W_RESTORE_FOREIGN_FILE_RETAINED", file.Path, current));
                    retained.Add(file.Path);
                }
                else throw new IOException("OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED: " + file.Path);
            }
        }
        VerifyRestoredSnapshots(retained);
        // Persist the verified state before deleting the journal. A crash in
        // this narrow window only causes an idempotent recovery replay.
        await Persist(TransactionState.Restored, cancellationToken).ConfigureAwait(false);
        RemoveJournal(); CleanupBackups();
    }
    public Task<bool> HasPendingRecoveryAsync(CancellationToken cancellationToken = default) => Task.FromResult(File.Exists(journalPath));
    public async Task RestorePendingAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(journalPath)) return; var journal = JsonSerializer.Deserialize<TransactionJournal>(await File.ReadAllTextAsync(journalPath, cancellationToken).ConfigureAwait(false)) ?? throw new InvalidDataException("Invalid OmsiLaunch journal.");
        if (OwnerIsAlive(journal)) throw new IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.");
        Snapshots = journal.Files.Select(x => new FileSnapshot(x.RelativePath, x.Existed, x.Sha256, x.Existed ? File.ReadAllBytes(x.BackupPath) : Array.Empty<byte>(), x.LastWriteTimeUtcTicks, x.CreationTimeUtcTicks, x.Attributes, x.SessionDeletion)).ToArray();
        foreach (var file in journal.Files) { var backupDirectory = Path.GetDirectoryName(file.BackupPath); if (backupDirectory is not null) backupRoots.Add(backupDirectory); }
        state = journal.State;
        appliedHashes.Clear();
        foreach (var file in journal.Files)
        {
            // Pre-fingerprint journals are recoverable only when this new
            // transaction independently supplies the exact same planned bytes.
            // Otherwise absence ownership is unknown and recovery must remain
            // pending rather than deleting a possible third-party file.
            appliedHashes[file.RelativePath] = file.AppliedSha256
                ?? (!file.Existed && overlays.TryGetValue(file.RelativePath, out var overlay) ? Hash(overlay) : null);
        }
        await RestoreAsync(cancellationToken).ConfigureAwait(false);
        // Recovery is for the prior journal. Restore the ownership evidence
        // for this newly constructed transaction before its later ApplyAsync.
        Snapshots = Array.Empty<FileSnapshot>();
        state = TransactionState.Prepared;
        ResetAppliedHashesForNewTransaction();
    }
    public Task MarkStateAsync(TransactionState state, CancellationToken cancellationToken = default) => Persist(state, cancellationToken);
    public Task RecordProcessAsync(int processId, DateTimeOffset creationTimeUtc, string executablePath, CancellationToken cancellationToken = default) => Persist(TransactionState.ProcessStarted, cancellationToken, processId, creationTimeUtc.UtcDateTime.Ticks, executablePath);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    private FileSnapshot SnapshotFile(string relative)
    {
        var path = Path.Combine(root, relative); var exists = File.Exists(path); var bytes = exists ? File.ReadAllBytes(path) : Array.Empty<byte>();
        var info = exists ? new FileInfo(path) : null;
        return new(relative, exists, Hash(bytes), bytes, info?.LastWriteTimeUtc.Ticks, info?.CreationTimeUtc.Ticks, info is null ? null : (int)info.Attributes, deletions.Contains(relative));
    }
    private async Task Persist(TransactionState state, CancellationToken cancellationToken, int? processId = null, long? processStartFileTimeUtc = null, string? executablePath = null)
    {
        var backupRoot = Path.Combine(Path.GetDirectoryName(journalPath)!, "backup", sessionId.ToString("N")); Directory.CreateDirectory(backupRoot); backupRoots.Add(backupRoot);
        var files = new List<JournalFile>(Snapshots.Count);
        foreach (var snapshot in Snapshots)
        {
            var backup = Path.Combine(backupRoot, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot.Path))) + ".bin");
            if (snapshot.Existed) await AtomicWrite(backup, snapshot.Bytes, cancellationToken).ConfigureAwait(false);
            files.Add(new JournalFile(snapshot.Path, snapshot.Existed, snapshot.Sha256, backup, appliedHashes.GetValueOrDefault(snapshot.Path), snapshot.LastWriteTimeUtcTicks, snapshot.CreationTimeUtcTicks, snapshot.Attributes, snapshot.SessionDeletion));
        }
        await AtomicWrite(journalPath, JsonSerializer.SerializeToUtf8Bytes(new TransactionJournal(sessionId, state, files, processId, processStartFileTimeUtc, executablePath)), cancellationToken).ConfigureAwait(false);
        this.state = state;
    }
    private void VerifyRestoredSnapshots(IReadOnlySet<string> retained)
    {
        foreach (var file in Snapshots)
        {
            var path = Path.Combine(root, file.Path);
            if (!file.Existed)
            {
                if (File.Exists(path) && !retained.Contains(file.Path)) throw new IOException("Restore presence mismatch: " + file.Path);
                continue;
            }
            if (!File.Exists(path) || !string.Equals(Hash(File.ReadAllBytes(path)), file.Sha256, StringComparison.OrdinalIgnoreCase)) throw new IOException("Restore hash mismatch: " + file.Path);
        }
    }
    private void ResetAppliedHashesForNewTransaction()
    {
        appliedHashes.Clear();
        foreach (var overlay in overlays) appliedHashes[overlay.Key] = Hash(overlay.Value);
    }
    private void RemoveJournal() { File.Delete(journalPath); if (File.Exists(journalPath)) throw new IOException("OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"); }
    // Backups are only removed after the journal that referenced them is gone;
    // a failure here is cosmetic and must never undo a verified restore.
    private void CleanupBackups()
    {
        var backupBase = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(journalPath)!, "backup")) + Path.DirectorySeparatorChar;
        foreach (var directory in backupRoots.ToArray())
        {
            var full = Path.GetFullPath(directory);
            if (!full.StartsWith(backupBase, StringComparison.OrdinalIgnoreCase)) continue;
            try { if (Directory.Exists(full)) Directory.Delete(full, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            backupRoots.Remove(directory);
        }
    }
    private static void RestoreMetadata(string path, FileSnapshot file)
    {
        try
        {
            if (file.CreationTimeUtcTicks is { } created) File.SetCreationTimeUtc(path, new DateTime(created, DateTimeKind.Utc));
            if (file.LastWriteTimeUtcTicks is { } written) File.SetLastWriteTimeUtc(path, new DateTime(written, DateTimeKind.Utc));
            if (file.Attributes is { } attributes) File.SetAttributes(path, (FileAttributes)attributes);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    private static void DeleteFile(string path)
    {
        Transient(() =>
        {
            if (!File.Exists(path)) return;
            var attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReadOnly)) File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            File.Delete(path);
        });
    }
    private static async Task AtomicWrite(string path, byte[] bytes, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); var temporary = path + ".omsilaunch.tmp";
        try
        {
            // Write-through plus an explicit disk flush: a journal or backup that
            // only exists in the OS cache is not a recovery obligation yet.
            await TransientAsync(async () =>
            {
                await using var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
                await stream.WriteAsync(bytes, token).ConfigureAwait(false); stream.Flush(true);
            }, token).ConfigureAwait(false);
            await TransientAsync(() =>
            {
                if (File.Exists(path)) { var attributes = File.GetAttributes(path); if (attributes.HasFlag(FileAttributes.ReadOnly)) File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly); }
                File.Move(temporary, path, true);
                return Task.CompletedTask;
            }, token).ConfigureAwait(false);
        }
        finally { if (File.Exists(temporary)) { try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { } } }
    }
    // Antivirus scanners, indexers and backup agents briefly open files that
    // were just written. Those sharing/lock violations and transient
    // access-denied results are retried for a bounded time (about 1.5 s); a
    // file that stays locked still fails the operation exactly as before.
    private const int TransientAttempts = 8;
    private static bool IsTransient(Exception exception) =>
        exception is UnauthorizedAccessException ||
        (exception is IOException io && (io.HResult & 0xFFFF) is 32 or 33);
    private static void Transient(Action action)
    {
        for (var attempt = 1; ; attempt++)
        {
            try { action(); return; }
            catch (Exception exception) when (attempt < TransientAttempts && IsTransient(exception)) { Thread.Sleep(Math.Min(400, 25 << attempt)); }
        }
    }
    private static async Task TransientAsync(Func<Task> action, CancellationToken token)
    {
        for (var attempt = 1; ; attempt++)
        {
            try { await action().ConfigureAwait(false); return; }
            catch (Exception exception) when (attempt < TransientAttempts && IsTransient(exception)) { await Task.Delay(Math.Min(400, 25 << attempt), token).ConfigureAwait(false); }
        }
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private bool OwnerIsAlive(TransactionJournal journal)
    {
        if (journal.ProcessId is not { } processId || journal.ProcessStartFileTimeUtc is not { } expected)
        {
            // A journal that reached the handoff but never recorded a PID belongs
            // to a host that died between CreateProcess and the journal write.
            // Any OMSI running from this installation is then treated as its owner.
            return journal.State >= TransactionState.HandoffCreated && journal.State < TransactionState.ProcessExited && AnyOmsiRunningFromRoot();
        }
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != expected) return false;
            if (string.IsNullOrWhiteSpace(journal.ExecutablePath)) return true;
            // PID and start time reject reuse; the recorded executable path
            // prevents a live unrelated process from retaining this lease.
            return string.Equals(process.MainModule?.FileName, journal.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }
    private bool AnyOmsiRunningFromRoot()
    {
        var executable = Path.Combine(root, "Omsi.exe");
        foreach (var process in System.Diagnostics.Process.GetProcessesByName("Omsi"))
        {
            using (process)
            {
                try { if (!process.HasExited && string.Equals(process.MainModule?.FileName, executable, StringComparison.OrdinalIgnoreCase)) return true; }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }
        return false;
    }
}
