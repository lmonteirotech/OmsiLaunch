using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DNNE;

namespace OmsiLaunch.Plugin;

// Current-only adapter: exports OMSI's plugin ABI and schedules PluginRuntime
// on the original OMSI UI thread. It owns no launch semantics.
public static class CurrentDnneAdapter
{
    private static readonly PluginRuntime Runtime = new(new NativeServices(), CurrentTelemetrySink.Emit, new CurrentRuntimeControl());
    private static readonly TimerProc TimerCallback = OnTimer;
    private static nuint timer;
    private static bool runtimePolling;
    private static int pluginStartInvoked;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "PluginStart")]
    public static void PluginStart(nint owner)
    {
        // OMSI can invoke PluginStart more than once during startup. The first
        // invocation owns the one-shot headless hook; re-entering after that
        // hook changes the profiled VMT would otherwise look like a bad build.
        if (Interlocked.Exchange(ref pluginStartInvoked, 1) != 0) return;
        if (string.Equals(Environment.GetEnvironmentVariable("OMSILAUNCH_INTERNET_TEXTURES_MODE"), "Disabled", StringComparison.OrdinalIgnoreCase) && NativeSuppressInternetTextures() == 0)
            CurrentTelemetrySink.Emit("internet-textures.suppression.failed", new Dictionary<string, string>());
        else if (string.Equals(Environment.GetEnvironmentVariable("OMSILAUNCH_INTERNET_TEXTURES_MODE"), "Disabled", StringComparison.OrdinalIgnoreCase))
            CurrentTelemetrySink.Emit("internet-textures.suppressed", new Dictionary<string, string>());
        Runtime.Start(ScheduleOnUiThread);
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "PluginFinalize")]
    public static void PluginFinalize()
    {
        if (timer != 0) { KillTimer(0, timer); timer = 0; }
        runtimePolling = false;
        Runtime.Shutdown();
        NativeRestoreInternetTextures();
        Volatile.Write(ref pluginStartInvoked, 0);
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "AccessVariable")]
    public static void AccessVariable(ushort variableIndex, nint value, nint writeValue) { }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "AccessTrigger")]
    public static void AccessTrigger(ushort variableIndex, nint triggerScript) { }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "AccessStringVariable")]
    public static void AccessStringVariable(ushort variableIndex, nint value, nint writeValue) { }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "AccessSystemVariable")]
    public static void AccessSystemVariable(ushort variableIndex, nint value, nint writeValue) { }

    private static void ScheduleOnUiThread(Action action)
    {
        pending = action; runtimePolling = false; timer = SetTimer(0, 0, 1, TimerCallback);
    }
    private static Action? pending;
    private static void OnTimer(nint window, uint message, nuint id, uint time)
    {
        var hadPendingWorld = Runtime.HasPendingWorld;
        pending?.Invoke();
        // Startup is retried only while PluginRuntime owns a pending world;
        // runtime polling must not replay a completed startup continuation.
        if (!Runtime.HasPendingWorld) pending = null;
        // Current telemetry is a latest-event mailbox. Do not publish a D3D
        // lifecycle event in the same tick that publishes gameplay.entered,
        // otherwise the host can miss its semantic RUNNING boundary.
        if (!hadPendingWorld) Runtime.PollRuntimeCommands();
        if (!Runtime.HasPendingWorld && Runtime.HasRuntimeCommandChannel && !runtimePolling && timer != 0)
        {
            KillTimer(0, timer); timer = SetTimer(0, 0, 50, TimerCallback); runtimePolling = true;
        }
        if (!Runtime.HasPendingWorld && !Runtime.HasRuntimeCommandChannel && timer != 0) { pending = null; KillTimer(0, timer); timer = 0; }
    }

    private sealed class NativeServices : IPluginNativeServices
    {
        public bool ValidateBuild(string profileIdentity) => string.Equals(profileIdentity, "Omsi23004_692EBFBF", StringComparison.Ordinal) && NativeValidateBuild() != 0;
        public bool ArmHeadlessStart() => NativeArmHeadlessStart() != 0;
        public int StartNewMap(string mapIdentity, int presentedEntrypointIndex, string entrypointIdentity) => NativeStartNewMap(mapIdentity, presentedEntrypointIndex, entrypointIdentity);
        public int StartSavedSituation(string situationIdentity) => NativeStartSavedSituation(situationIdentity);
        public bool TryGetLastEntrypointSelection(out NativeEntrypointSelection selection)
        {
            var presented = new StringBuilder(512); var raw = new StringBuilder(512);
            if (NativeGetLastEntrypointSelection(out var presentedIndex, out var rawIndex, presented, presented.Capacity, raw, raw.Capacity) == 0)
            {
                selection = null!; return false;
            }
            selection = new(presentedIndex, rawIndex, presented.ToString(), raw.ToString()); return true;
        }
        public void InstallMainThreadGateway() => NativeInstallMainThreadGateway();
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeValidateBuild();
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeArmHeadlessStart();
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] private static extern int NativeStartNewMap(string mapIdentity, int presentedIndex, string entrypointIdentity);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] private static extern int NativeStartSavedSituation(string situationIdentity);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] private static extern int NativeGetLastEntrypointSelection(out int presentedIndex, out int rawIndex, StringBuilder presentedName, int presentedNameCapacity, StringBuilder rawName, int rawNameCapacity);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void NativeInstallMainThreadGateway();
    }

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeSuppressInternetTextures();
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void NativeRestoreInternetTextures();

    private delegate void TimerProc(nint window, uint message, nuint id, uint time);
    [DllImport("user32.dll", SetLastError = true)] private static extern nuint SetTimer(nint window, nuint id, uint elapsedMilliseconds, TimerProc callback);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool KillTimer(nint window, nuint id);
}
