using System.IO.MemoryMappedFiles;
using System.Buffers.Binary;
using OmsiLaunch.Api;

namespace OmsiLaunch.Plugin;

public interface IPluginNativeServices
{
    bool ValidateBuild(string profileIdentity);
    bool ArmHeadlessStart();
    int StartNewMap(string mapIdentity, int presentedEntrypointIndex, string entrypointIdentity);
    int StartSavedSituation(string situationIdentity);
    bool TryGetLastEntrypointSelection(out NativeEntrypointSelection selection);
    void InstallMainThreadGateway();
}

public sealed record NativeEntrypointSelection(int PresentedIndex, int RawIndex, string PresentedLabel, string RawLabel);

// DNNE-independent runtime semantics. The Current adapter supplies scheduling
// onto OMSI's UI message pump and can later be replaced by a legacy adapter.
public sealed class PluginRuntime
{
    private readonly IPluginNativeServices native;
    private readonly Action<string, IReadOnlyDictionary<string, string>> telemetry;
    private readonly IPluginRuntimeControl? runtimeControl;
    private StartupHandoff? pending;
    private CurrentRuntimeCommandMailbox? mailbox;
    private bool executing;
    private int startupAccepted;
    private long lifecycleEventsNotBefore;
    public bool HasPendingWorld => pending is not null;
    public bool HasRuntimeCommandChannel => mailbox is not null;

    public PluginRuntime(IPluginNativeServices native, Action<string, IReadOnlyDictionary<string, string>>? telemetry = null, IPluginRuntimeControl? runtimeControl = null)
    {
        this.native = native; this.runtimeControl = runtimeControl;
        this.telemetry = telemetry ?? ((_, _) => { });
    }

    public bool Start(Action<Action> scheduleOnUiThread)
    {
        var name = Environment.GetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME");
        if (string.IsNullOrWhiteSpace(name) || !TryReadHandoff(name, out var handoff)) { Emit("plugin.handoff.invalid"); return false; }
        // OMSI may notify the same plugin more than once during bootstrap. A
        // successful handoff owns a one-shot native hook, so only its first
        // accepted Start may validate and arm that hook.
        if (Interlocked.CompareExchange(ref startupAccepted, 1, 0) != 0) return true;
        try
        {
        Emit("plugin.started", ("session_id", handoff.SessionId.ToString("D")));
        if ((handoff.WorldMode is not WorldMode.NewMap and not WorldMode.SavedSituation) || !handoff.HeadlessStart || handoff.PlayerVehicleEnabled || handoff.DateMode != DateTimeMode.Unset || handoff.TimeMode != DateTimeMode.Unset || (handoff.WorldMode == WorldMode.SavedSituation && string.IsNullOrWhiteSpace(handoff.SituationIdentity))) { Emit("plugin.request.unsupported"); Volatile.Write(ref startupAccepted, 0); return false; }
        if (!native.ValidateBuild(handoff.BuildProfileId)) { Emit("plugin.build.invalid"); Volatile.Write(ref startupAccepted, 0); return false; }
        Emit("plugin.build.validated");
        if (!native.ArmHeadlessStart()) { Emit("headless.arm.failed"); Volatile.Write(ref startupAccepted, 0); return false; }
        Emit("headless.armed");
        var runtimeChannel = Environment.GetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL");
        if (!string.IsNullOrWhiteSpace(runtimeChannel) && runtimeControl is not null) mailbox = new CurrentRuntimeCommandMailbox(runtimeChannel, handoff.SessionId);
        native.InstallMainThreadGateway(); pending = handoff;
        scheduleOnUiThread(ConsumePendingWorld);
        return true;
        }
        catch
        {
            Volatile.Write(ref startupAccepted, 0);
            throw;
        }
    }

    // Called by the adapter's OMSI UI-thread timer. No runtime command is ever
    // executed on an IPC worker thread.
    public void PollRuntimeCommands()
    {
        if (runtimeControl is null) return;
        if (Environment.TickCount64 >= Interlocked.Read(ref lifecycleEventsNotBefore))
            foreach (var lifecycleEvent in runtimeControl.PollLifecycle()) telemetry(lifecycleEvent.Name, lifecycleEvent.Data);
        mailbox?.TryDispatch(runtimeControl);
    }

    public void Shutdown()
    {
        mailbox = null;
        pending = null;
        runtimeControl?.Shutdown();
        Volatile.Write(ref startupAccepted, 0);
    }

    private void ConsumePendingWorld()
    {
        if (pending is null || executing) return;
        executing = true;
        try
        {
            if (pending.WorldMode == WorldMode.SavedSituation)
            {
                Emit("world.situation.starting", ("situation", pending.SituationIdentity));
                var savedResult = native.StartSavedSituation(pending.SituationIdentity);
                if (savedResult == 0) { Emit("world.situation.loaded", ("situation", pending.SituationIdentity)); Emit("gameplay.entered", ("situation", pending.SituationIdentity)); DeferLifecycleEvents(); pending = null; return; }
                Emit("world.situation.failed", ("native_status", savedResult.ToString()), ("situation", pending.SituationIdentity)); pending = null; return;
            }
            Emit("world.starting", ("map", pending.MapIdentity), ("presented_index", pending.PresentedEntrypointIndex.ToString()), ("entrypoint_identity", pending.EntrypointIdentity));
            var result = native.StartNewMap(pending.MapIdentity, pending.PresentedEntrypointIndex, pending.EntrypointIdentity);
            if (result == 0) {
                Emit("world.loaded");
                if (native.TryGetLastEntrypointSelection(out var selection))
                {
                    Emit("world.entrypoint.selected", ("presented_index", selection.PresentedIndex.ToString()), ("raw_index", selection.RawIndex.ToString()), ("presented_label", selection.PresentedLabel), ("raw_label", selection.RawLabel));
                    // Current telemetry is a latest-event mailbox. Carry the
                    // selected identity into its terminal readiness event.
                    Emit("gameplay.entered", ("presented_index", selection.PresentedIndex.ToString()), ("raw_index", selection.RawIndex.ToString()), ("presented_label", selection.PresentedLabel), ("raw_label", selection.RawLabel));
                }
                else Emit("gameplay.entered", ("entrypoint_diagnostics", "unavailable"));
                DeferLifecycleEvents();
                pending = null;
            }
            else if (result is 3 or 4) { Emit("world.waiting-native-ready", ("native_status", result.ToString())); }
            else { Emit("world.failed", ("native_status", result.ToString())); pending = null; }
        }
        finally { executing = false; }
    }

    private void DeferLifecycleEvents() => Interlocked.Exchange(ref lifecycleEventsNotBefore, Environment.TickCount64 + 2_000);

    private void Emit(string name, params (string Key, string Value)[] values) => telemetry(name, values.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));
    private static bool TryReadHandoff(string name, out StartupHandoff handoff)
    {
        handoff = null!;
        try
        {
            using var mapping = MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.Read);
            using var view = mapping.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            if (view.Capacity < 64) return false;
            var header = new byte[64]; view.ReadArray(0, header, 0, header.Length);
            var total = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8));
            if (total < 64 || total > view.Capacity || total > 1024 * 1024) return false;
            var data = new byte[checked((int)total)]; view.ReadArray(0, data, 0, data.Length);
            if (!StartupHandoffWire.TryDeserialize(data, out var decoded) || decoded is null) return false;
            handoff = decoded; return true;
        }
        catch (FileNotFoundException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
