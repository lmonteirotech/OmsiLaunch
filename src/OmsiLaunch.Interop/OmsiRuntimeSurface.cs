// OmsiHook domain inventory translated into profile-neutral operation names.
// Individual operations are enabled only by a matching OmsiBuildProfile and
// a native guard; this catalog deliberately contains no numeric address.
namespace OmsiLaunch.Interop;

public enum OmsiRuntimeDomain : byte
{
    ProgramManager,
    Map,
    Time,
    Weather,
    RoadVehicles,
    Humans,
    TimeTable,
    Camera,
    PlayerVehicle,
    Sound,
    Content,
    D3D,
}

public sealed record OmsiRuntimeOperation(
    string Id,
    OmsiRuntimeDomain Domain,
    bool MutatesWorld,
    string RequiredProfileEvidence,
    string UpstreamEvidence);

public sealed record OmsiRuntimeOperationAvailability(OmsiRuntimeOperation Operation, bool Available, string Reason);

public static class OmsiRuntimeSurface
{
    // This is the semantic successor to the useful OmsiHook surface. The
    // catalog is exhaustive by domain but does not claim each operation is
    // enabled for every Omsi.exe build.
    public static readonly IReadOnlyList<OmsiRuntimeOperation> Operations = new OmsiRuntimeOperation[]
    {
        new("program.close-map", OmsiRuntimeDomain.ProgramManager, true, "CloseMap", "OmsiProgMan.CloseMap"),
        new("program.new-situation", OmsiRuntimeDomain.ProgramManager, true, "NewSituation", "OmsiProgMan.NewSituation"),
        new("program.calculate-ai-sum", OmsiRuntimeDomain.ProgramManager, true, "CalcAISum", "OmsiProgMan.CalcAISum"),
        new("program.set-ai-vehicles", OmsiRuntimeDomain.ProgramManager, true, "SetAIVehicles", "OmsiProgMan.SetAIVehicles"),
        new("program.initialize-scheduled-ai", OmsiRuntimeDomain.ProgramManager, true, "InitializeScheduledAI", "OmsiProgMan.InitializeScheduledAI"),
        new("map.read", OmsiRuntimeDomain.Map, false, "Map global and fields", "OmsiMap"),
        new("map.load-global-file", OmsiRuntimeDomain.Map, true, "MapLoadGlobalFile", "OmsiMap.LoadGlobalFile"),
        new("time.read", OmsiRuntimeDomain.Time, false, "OmsiTime fields", "OmsiTime"),
        new("time.set", OmsiRuntimeDomain.Time, true, "SetTime", "OmsiTime"),
        new("time.set-actual-date-time", OmsiRuntimeDomain.Time, true, "SetActualDateTime", "OmsiTime"),
        new("weather.read", OmsiRuntimeDomain.Weather, false, "OmsiWeather fields", "OmsiWeather/OmsiActuWeather"),
        new("weather.write", OmsiRuntimeDomain.Weather, true, "OmsiWeather field guards", "OmsiWeather/OmsiActuWeather"),
        new("road-vehicles.read", OmsiRuntimeDomain.RoadVehicles, false, "RoadVehicles global and layouts", "OmsiRoadVehicleInst"),
        new("road-vehicles.list", OmsiRuntimeDomain.RoadVehicles, false, "RoadVehicles MemArrayList layout", "OmsiMyOmsiList.FList"),
        new("road-vehicle.read", OmsiRuntimeDomain.RoadVehicles, false, "OmsiRoadVehicleInst read layout", "OmsiRoadVehicleInst/OmsiMovingMapObjInst"),
        new("vehicle.variable.get", OmsiRuntimeDomain.RoadVehicles, false, "script variable table layouts", "OmsiComplMapObjInst.GetVariable"),
        new("vehicle.variable.set", OmsiRuntimeDomain.RoadVehicles, true, "profiled public-variable slot", "OmsiComplMapObjInst.SetVariable"),
        new("vehicle.variables.list", OmsiRuntimeDomain.RoadVehicles, false, "script variable name table layout", "OmsiComplMapObj.VarStrings"),
        new("vehicle.string-variable.get", OmsiRuntimeDomain.RoadVehicles, false, "script string-variable table layouts", "OmsiComplMapObjInst.GetStringVariable"),
        new("vehicle.string-variables.list", OmsiRuntimeDomain.RoadVehicles, false, "script string-variable name table layout", "OmsiComplMapObj.SVarStrings"),
        new("vehicle.constant.get", OmsiRuntimeDomain.RoadVehicles, false, "ScriptConstants layout", "OmsiComplMapObjInst.GetConst"),
        new("vehicle.curve.evaluate", OmsiRuntimeDomain.RoadVehicles, false, "ScriptConstants layout", "OmsiComplMapObjInst.GetCurve"),
        new("vehicle.hofs.read", OmsiRuntimeDomain.Content, false, "RoadVehicleDefinition layout", "OmsiRoadVehicle.Hoefe"),
        new("road-vehicles.spawn", OmsiRuntimeDomain.RoadVehicles, true, "TProgManMakeVehicle", "OmsiRemoteMethods.MakeVehicle"),
        new("internal.road-vehicles.make-basic", OmsiRuntimeDomain.RoadVehicles, true, "TProgManMakeVehicle", "OmsiRemoteMethods.MakeVehicle"),
        new("road-vehicles.place-random", OmsiRuntimeDomain.RoadVehicles, true, "TProgManPlaceRandomBus", "OmsiRemoteMethods.PlaceRandomBus"),
        new("humans.read", OmsiRuntimeDomain.Humans, false, "Humans global and layouts", "OmsiHumanBeingInst"),
        new("humans.list", OmsiRuntimeDomain.Humans, false, "Humans pointer-array layout", "OmsiGlobals.Humans"),
        new("human.read", OmsiRuntimeDomain.Humans, false, "OmsiHumanBeingInst read layout", "OmsiHumanBeingInst"),
        new("timetable.read", OmsiRuntimeDomain.TimeTable, false, "TimeTableManager global and layouts", "OmsiTimeTableMan"),
        new("timetable.tracks.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTTrackInternal records", "OmsiTimeTableMan.Tracks"),
        new("timetable.trips.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTTripInternal records", "OmsiTimeTableMan.Trips"),
        new("timetable.lines.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTLineInternal records", "OmsiTimeTableMan.Lines"),
        new("timetable.rv-files.list", OmsiRuntimeDomain.TimeTable, false, "OmsiRVFileInternal records", "OmsiTimeTableMan.RVFiles"),
        new("timetable.track-entries.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTTrackEntryInternal records", "OmsiTimeTableMan.Tracks.TrackEntrys"),
        new("timetable.bus-stops.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTBusstopListEntryInternal records", "OmsiTimeTableMan.BusstopList"),
        new("timetable.station-links.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTStnLinkInternal records", "OmsiTimeTableMan.StnLinks"),
        new("timetable.tours.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTTourInternal records", "OmsiTimeTableMan.Lines.Tours"),
        new("timetable.profiles.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTProfileInternal records", "OmsiTimeTableMan.Trips.Profiles"),
        new("timetable.tour-entries.list", OmsiRuntimeDomain.TimeTable, false, "OmsiTTTourEntryInternal records", "OmsiTimeTableMan.Lines.Tours.Entrys"),
        new("drivers.read", OmsiRuntimeDomain.Content, false, "Driver layout", "OmsiGlobals.Drivers"),
        new("tickets.read", OmsiRuntimeDomain.Content, false, "TicketPack and Ticket layouts", "OmsiGlobals.TicketPack"),
        new("timetable.logs.read", OmsiRuntimeDomain.TimeTable, false, "TTLogDetailed layout", "OmsiGlobals.OmsiTTLogs"),
        new("camera.read", OmsiRuntimeDomain.Camera, false, "Camera global and fields", "OmsiCamera"),
        new("camera.write", OmsiRuntimeDomain.Camera, true, "Camera field guards", "OmsiCamera"),
        new("camera.lock", OmsiRuntimeDomain.Camera, true, "CameraFamily and RoadVehicle active camera offsets", "OmsiGlobals.Camera_Family/OmsiVehicleInst.Act_Camera_*"),
        new("d3d.status", OmsiRuntimeDomain.D3D, false, "D3DDevice", "OmsiHook DXHook::HookD3D"),
        new("d3d.texture.create", OmsiRuntimeDomain.D3D, true, "D3DDevice", "OmsiHook DXHook::CreateTexture"),
        new("d3d.texture.update", OmsiRuntimeDomain.D3D, true, "D3DDevice", "OmsiHook DXHook::UpdateSubresource"),
        new("d3d.texture.describe", OmsiRuntimeDomain.D3D, false, "D3DDevice", "OmsiHook DXHook::GetTextureDesc/GetLevelCount"),
        new("d3d.texture.release", OmsiRuntimeDomain.D3D, true, "D3DDevice", "OmsiHook DXHook::ReleaseTexture"),
        new("player-vehicle.read", OmsiRuntimeDomain.PlayerVehicle, false, "PlayerVehicle global and layouts", "OmsiRoadVehicleInst"),
        new("player-vehicle.assign", OmsiRuntimeDomain.PlayerVehicle, true, "MakeVehicle postconditions", "OmsiRemoteMethods.MakeVehicle"),
        new("sound.read", OmsiRuntimeDomain.Sound, false, "Sound layouts", "OmsiSound/OmsiSoundPack"),
        new("content.hof.read", OmsiRuntimeDomain.Content, false, "HOF layout/parser", "OmsiHOF"),
    };

    public static IReadOnlyList<OmsiRuntimeOperation> ForDomain(OmsiRuntimeDomain domain) => Operations.Where(operation => operation.Domain == domain).ToArray();

    public static IReadOnlyList<OmsiRuntimeOperationAvailability> Resolve(Func<string, bool> hasProfileEvidence)
    {
        ArgumentNullException.ThrowIfNull(hasProfileEvidence);
        return Operations.Select(operation => hasProfileEvidence(operation.RequiredProfileEvidence)
            ? new OmsiRuntimeOperationAvailability(operation, true, "Profile evidence present.")
            : new OmsiRuntimeOperationAvailability(operation, false, "Profile evidence or native guard is absent.")).ToArray();
    }
}
