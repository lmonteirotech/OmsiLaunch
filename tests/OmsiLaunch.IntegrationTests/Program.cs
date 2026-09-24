using System.IO.MemoryMappedFiles;
using System.Buffers.Binary;
using OmsiLaunch.Api;
using OmsiLaunch.Interop;
using OmsiLaunch.Plugin;

var handoff = new StartupHandoff(Guid.NewGuid(), "Omsi23004_692EBFBF", WorldMode.NewMap, "maps\\Grundorf\\global.cfg", -1, true, false, DateTimeMode.Unset, DateTimeMode.Unset, "Nordspitze Bauernhof");
var bytes = StartupHandoffWire.Serialize(handoff); var name = "OmsiLaunch.PluginTest." + handoff.SessionId.ToString("N");
using var mapping = MemoryMappedFile.CreateNew(name, bytes.Length, MemoryMappedFileAccess.ReadWrite);
using (var view = mapping.CreateViewAccessor(0, bytes.Length, MemoryMappedFileAccess.Write)) view.WriteArray(0, bytes, 0, bytes.Length);
Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", name);
var runtimeName = "OmsiLaunch.RuntimeTest." + handoff.SessionId.ToString("N");
using var runtimeMapping = MemoryMappedFile.CreateNew(runtimeName, 65_536, MemoryMappedFileAccess.ReadWrite);
Environment.SetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL", runtimeName);
Action? callback = null; var native = new FakeNative(); var events = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
var runtimeControl = new FakeRuntimeControl();
var runtime = new PluginRuntime(native, (eventName, data) => events.Add((eventName, data)), runtimeControl);
if (!runtime.Start(action => callback = action) || callback is null) throw new InvalidOperationException("PluginRuntime did not accept the portable handoff.");
if (!runtime.Start(_ => throw new InvalidOperationException("Duplicate PluginRuntime.Start must not schedule another startup.")) || native.BuildValidationCount != 1)
    throw new InvalidOperationException("PluginRuntime did not make duplicate startup notification idempotent.");
callback();
if (!native.Armed || native.Map != "maps\\Grundorf\\global.cfg" || native.Index != -1 || native.EntrypointIdentity != "Nordspitze Bauernhof" || !events.Any(x => x.Name == "gameplay.entered" && x.Data.TryGetValue("raw_label", out var label) && label == "Nordspitze Bauernhof")) throw new InvalidOperationException("PluginRuntime did not dispatch the semantic NEW_MAP request.");
Console.WriteLine("PASS plugin-runtime.handoff-new-map");

var savedHandoff = new StartupHandoff(Guid.NewGuid(), "Omsi23004_692EBFBF", WorldMode.SavedSituation, "", -1, true, false, DateTimeMode.Unset, DateTimeMode.Unset, "", "situations\\23A 2005.osn");
var savedBytes = StartupHandoffWire.Serialize(savedHandoff);
if (!StartupHandoffWire.TryDeserialize(savedBytes, out var decodedSaved) || decodedSaved is null || decodedSaved.SituationIdentity != savedHandoff.SituationIdentity || decodedSaved.WorldMode != WorldMode.SavedSituation) throw new InvalidOperationException("Startup handoff v4 did not preserve saved-situation identity.");
var savedName = "OmsiLaunch.PluginTest." + savedHandoff.SessionId.ToString("N");
using var savedMapping = MemoryMappedFile.CreateNew(savedName, savedBytes.Length, MemoryMappedFileAccess.ReadWrite);
using (var view = savedMapping.CreateViewAccessor(0, savedBytes.Length, MemoryMappedFileAccess.Write)) view.WriteArray(0, savedBytes, 0, savedBytes.Length);
Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", savedName);
var savedEvents = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
Action? savedCallback = null; var savedNative = new FakeNative();
if (!new PluginRuntime(savedNative, (eventName, data) => savedEvents.Add((eventName, data))).Start(action => savedCallback = action) || savedCallback is null) throw new InvalidOperationException("PluginRuntime did not accept the profiled SAVED_SITUATION handoff.");
savedCallback();
if (savedNative.Situation != savedHandoff.SituationIdentity || !savedEvents.Any(x => x.Name == "gameplay.entered" && x.Data.TryGetValue("situation", out var situation) && situation == savedHandoff.SituationIdentity)) throw new InvalidOperationException("PluginRuntime did not dispatch the full saved-situation Start flow.");
Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", name);
Console.WriteLine("PASS handoff.saved-situation-v4");

var command = new RuntimeCommand(handoff.SessionId, 7, "time.read"); var commandBytes = RuntimeCommandWire.SerializeRequest(command);
using (var view = runtimeMapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite)) { view.Write(4, commandBytes.Length); view.WriteArray(8, commandBytes, 0, commandBytes.Length); view.Write(0, 1); view.Flush(); }
// Lifecycle probing is deferred until OMSI crosses gameplay readiness.
Thread.Sleep(2_050);
runtime.PollRuntimeCommands();
using (var view = runtimeMapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.Read)) { if (view.ReadInt32(0) != 2) throw new InvalidOperationException("PluginRuntime did not complete runtime request."); var length = view.ReadInt32(4); var response = new byte[length]; view.ReadArray(8, response, 0, length); if (!RuntimeCommandWire.TryDeserializeResponse(response, out var result) || result is null || !result.Succeeded || result.RequestId != 7) throw new InvalidOperationException("Runtime command response did not preserve session binding."); }
if (!events.Any(entry => entry.Name == "d3d.ready")) throw new InvalidOperationException("PluginRuntime did not publish the runtime-control lifecycle event.");
RuntimeCommandResult DispatchFixture(ulong id, string operation, out int state, Guid? sessionOverride = null) => DispatchFixtureSized(id, operation, out state, out _, sessionOverride);
RuntimeCommandResult DispatchFixtureSized(ulong id, string operation, out int state, out int responseBytes, Guid? sessionOverride = null)
{
    responseBytes = 0;
    var request = RuntimeCommandWire.SerializeRequest(new RuntimeCommand(sessionOverride ?? handoff.SessionId, id, operation));
    using (var view = runtimeMapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite)) { view.Write(4, request.Length); view.WriteArray(8, request, 0, request.Length); view.Write(0, 1); view.Flush(); }
    runtime.PollRuntimeCommands();
    using var read = runtimeMapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite);
    state = read.ReadInt32(0);
    if (state != 2) return new RuntimeCommandResult(handoff.SessionId, 0, false);
    var length = read.ReadInt32(4); responseBytes = length; var bytes = new byte[length]; read.ReadArray(8, bytes, 0, length); read.Write(0, 0); read.Flush();
    return RuntimeCommandWire.TryDeserializeResponse(bytes, out var decoded) && decoded is not null ? decoded : throw new InvalidOperationException("fixture response could not be decoded");
}
var oversized = DispatchFixtureSized(21, "fixture.oversize", out var oversizedState, out var oversizedBytes);
if (oversizedState != 2 || oversized.Succeeded || oversized.ErrorCode != "OL_E_RUNTIME_RESPONSE_TOO_LARGE" || oversized.RequestId != 21) throw new InvalidOperationException("Oversized plugin response was not reported as a typed error.");
// The error envelope for "too large" must itself be small and bounded.
if (oversizedBytes <= 0 || oversizedBytes > 512) throw new InvalidOperationException("The too-large error envelope is not bounded: " + oversizedBytes + " bytes.");
var bounded = DispatchFixtureSized(28, "fixture.bounded-list-oversize", out var boundedState, out var boundedBytes);
var boundedRows = bounded.Values?.Keys.Where(key => key.StartsWith("track_entry.", StringComparison.Ordinal)).Select(key => int.Parse(key.Split('.')[1], System.Globalization.CultureInfo.InvariantCulture)).Distinct().OrderBy(row => row).ToArray() ?? Array.Empty<int>();
if (boundedState != 2 || !bounded.Succeeded || bounded.Values!["truncated"] != "true" || bounded.Values["count"] != "3000"
    || int.Parse(bounded.Values["returned_count"], System.Globalization.CultureInfo.InvariantCulture) != boundedRows.Length || boundedRows.Length == 0
    || !boundedRows.SequenceEqual(Enumerable.Range(0, boundedRows.Length)) || boundedBytes > 65_536 - 8)
    throw new InvalidOperationException("An oversized bounded list was not truncated to fit the slot (state " + boundedState + ", code " + bounded.ErrorCode + ", rows " + boundedRows.Length + ").");
Console.WriteLine("PASS plugin-runtime.bounded-list-fits-slot");
var afterOversize = DispatchFixture(24, "time.read", out var afterOversizeState);
if (afterOversizeState != 2 || !afterOversize.Succeeded || afterOversize.RequestId != 24) throw new InvalidOperationException("Mailbox was not usable after an oversized response.");
var thrown = DispatchFixture(25, "fixture.throw", out var thrownState);
if (thrownState != 2 || thrown.Succeeded || thrown.ErrorCode != "OL_E_RUNTIME_OPERATION_FAILED" || thrown.RequestId != 25) throw new InvalidOperationException("An operation exception was not reported as a typed error.");
var foreign = DispatchFixture(26, "time.read", out var foreignState, Guid.NewGuid());
if (foreignState != 2 || foreign.Succeeded || foreign.ErrorCode != "OL_E_RUNTIME_SESSION_MISMATCH" || foreign.RequestId != 26) throw new InvalidOperationException("A request for another session was not reported as a typed error.");
var afterForeign = DispatchFixture(27, "time.read", out var afterForeignState);
if (afterForeignState != 2 || !afterForeign.Succeeded || afterForeign.RequestId != 27) throw new InvalidOperationException("Mailbox was not usable after a rejected request.");
DispatchFixture(22, "fixture.abandon", out var abandonedState);
if (abandonedState != 0) throw new InvalidOperationException("Plugin published a response for a request the host had abandoned.");
var afterAbandon = DispatchFixture(23, "time.read", out var afterState);
if (afterState != 2 || !afterAbandon.Succeeded || afterAbandon.RequestId != 23) throw new InvalidOperationException("Mailbox was not usable after an abandoned request.");
Console.WriteLine("PASS plugin-runtime.oversized-and-abandoned-responses");
runtime.Shutdown();
if (!runtimeControl.ShutdownObserved) throw new InvalidOperationException("PluginRuntime did not transfer shutdown ownership to runtime control.");
Console.WriteLine("PASS plugin-runtime.command-channel");

var memory = new FakeMemory();
memory.WriteUInt32(0x100, 0x200); memory.WriteInt32(0x1FC, 4); memory.WriteBytes(0x200, System.Text.Encoding.Unicode.GetBytes("Test"));
if (await memory.ReadStringAsync(0x100, OmsiStringEncoding.Unicode) != "Test") throw new InvalidOperationException("UnicodeString marshalling failed.");
memory.WriteUInt32(0x300, 0x400); memory.WriteInt32(0x3FC, 2); memory.WriteUInt32(0x400, 0x500); memory.WriteUInt32(0x404, 0x600);
memory.WriteInt32(0x4FC, 1); memory.WriteBytes(0x500, System.Text.Encoding.Unicode.GetBytes("A")); memory.WriteInt32(0x5FC, 1); memory.WriteBytes(0x600, System.Text.Encoding.Unicode.GetBytes("B"));
var strings = await memory.ReadStringArrayAsync(0x300, OmsiStringEncoding.Unicode);
if (strings.Length != 2 || strings[0] != "A" || strings[1] != "B") throw new InvalidOperationException("UnicodeString array marshalling failed.");
memory.WriteUInt32(0x700 + 0x04, 0x800); memory.WriteInt32(0x700 + 0x08, 2); memory.WriteInt32(0x7FC, 2); memory.WriteUInt32(0x800, 0x900); memory.WriteUInt32(0x804, 0);
var objects = await new OmsiPointerList<FakeObject>(memory, 0x700, OmsiPointerListLayout.DelphiTList, (source, address) => new FakeObject(source, address)).SnapshotAsync();
if (objects.Count != 2 || objects[0]?.Address != 0x900 || objects[1] is not null) throw new InvalidOperationException("TList pointer collection marshalling failed.");
memory.WriteUInt32(0xA00, 0xB00); memory.WriteInt32(0xA10, 42);
var profiled = new OmsiProfiledObject(memory, 0xA00, new OmsiObjectLayout("TestObject", 0xB00, new Dictionary<string, uint> { ["Answer"] = 0x10 }));
if (!await profiled.ValidateAsync() || await profiled.ReadFieldAsync<int>("Answer") != 42) throw new InvalidOperationException("Profiled object field read failed.");
await profiled.WriteFieldAsync("Answer", 43); if (await profiled.ReadFieldAsync<int>("Answer") != 43) throw new InvalidOperationException("Profiled object field write failed.");
memory.WriteUInt32(0xB10 + 0x04, 0xB40); memory.WriteInt32(0xB10 + 0x08, 1); memory.WriteUInt32(0xB40, 0xB80); memory.WriteInt32(0xB7C, 2); memory.WriteBytes(0xB80, new byte[] { (byte)'P', (byte)'T' });
var ansiList = await new OmsiStringList(memory, 0xB10, OmsiPointerListLayout.DelphiTList, OmsiStringEncoding.Ansi).SnapshotAsync();
if (ansiList.Count != 1 || ansiList[0] != "PT") throw new InvalidOperationException("TList string marshalling failed.");
var availability = OmsiRuntimeSurface.Resolve(symbol => symbol == "NewSituation");
if (!availability.Single(entry => entry.Operation.Id == "program.new-situation").Available || availability.Single(entry => entry.Operation.Id == "weather.write").Available) throw new InvalidOperationException("Runtime surface capability resolution failed.");
Console.WriteLine("PASS interop.delphi-memory-primitives");

sealed class FakeNative : IPluginNativeServices
{
    public bool Armed { get; private set; } public string? Map { get; private set; } public int Index { get; private set; } public string? EntrypointIdentity { get; private set; }
    public int BuildValidationCount { get; private set; }
    public bool ValidateBuild(string profileIdentity) { BuildValidationCount++; return profileIdentity == "Omsi23004_692EBFBF"; }
    public bool ArmHeadlessStart() { Armed = true; return true; }
    public int StartNewMap(string mapIdentity, int presentedEntrypointIndex, string entrypointIdentity) { Map = mapIdentity; Index = presentedEntrypointIndex; EntrypointIdentity = entrypointIdentity; return 0; }
    public string? Situation { get; private set; }
    public int StartSavedSituation(string situationIdentity) { Situation = situationIdentity; return 0; }
    public bool TryGetLastEntrypointSelection(out NativeEntrypointSelection selection) { selection = new(1, 0, "", "Nordspitze Bauernhof"); return true; }
    public void InstallMainThreadGateway() { }
}

sealed class FakeRuntimeControl : IPluginRuntimeControl
{
    private bool emitted;
    public bool ShutdownObserved { get; private set; }
    public RuntimeCommandResult Execute(RuntimeCommand command)
    {
        // Correction-pass fixtures: an oversized result, and a request that the
        // host abandons (timeout reset) while the plugin is still executing it.
        if (command.Operation == "fixture.throw") throw new InvalidOperationException("simulated native fault inside an operation");
        if (command.Operation == "fixture.oversize") return new(command.SessionId, command.RequestId, true, Values: new Dictionary<string, string> { ["payload"] = new string('x', 70_000) });
        // Documentation audit BUG-05: a bounded list whose row bound still exceeds the slot.
        if (command.Operation == "fixture.bounded-list-oversize")
        {
            var rows = new Dictionary<string, string> { ["count"] = "3000", ["returned_count"] = "2000", ["truncated"] = "true" };
            for (var row = 0; row < 2000; row++) { rows["track_entry." + row + ".track"] = row.ToString(System.Globalization.CultureInfo.InvariantCulture); rows["track_entry." + row + ".name"] = new string('n', 60); }
            return new(command.SessionId, command.RequestId, true, Values: rows);
        }
        if (command.Operation == "fixture.abandon")
        {
            using var mapping = MemoryMappedFile.OpenExisting(Environment.GetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL")!, MemoryMappedFileRights.ReadWrite);
            using var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite); view.Write(0, 0); view.Flush();
        }
        return new(command.SessionId, command.RequestId, true, Values: new Dictionary<string, string> { ["source"] = "fake" });
    }
    public IReadOnlyList<PluginRuntimeEvent> PollLifecycle()
    {
        if (emitted) return Array.Empty<PluginRuntimeEvent>();
        emitted = true;
        return new[] { new PluginRuntimeEvent("d3d.ready", new Dictionary<string, string> { ["state"] = "READY" }) };
    }
    public void Shutdown() => ShutdownObserved = true;
}

sealed class FakeMemory : IOmsiMemory
{
    private readonly byte[] bytes = new byte[0x1000];
    public ValueTask<int> ReadAsync(nint address, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        bytes.AsMemory((int)address, destination.Length).CopyTo(destination);
        return ValueTask.FromResult(destination.Length);
    }
    public ValueTask WriteAsync(nint address, ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        source.CopyTo(bytes.AsMemory((int)address, source.Length));
        return ValueTask.CompletedTask;
    }
    public void WriteUInt32(int address, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(address), value);
    public void WriteInt32(int address, int value) => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(address), value);
    public void WriteBytes(int address, byte[] value) => value.CopyTo(bytes, address);
}

sealed class FakeObject : OmsiRemoteObject
{
    public FakeObject(IOmsiMemory memory, uint address) : base(memory, address) { }
}
