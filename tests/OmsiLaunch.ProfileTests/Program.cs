using OmsiLaunch.Api;
using OmsiLaunch.Core;

var root = Path.Combine(Path.GetTempPath(), "omsilaunch-profile-tests-" + Guid.NewGuid().ToString("N"));
try
{
    var package = Path.Combine(root, ".omsilaunch", "session-profiles", "grundorf");
    Directory.CreateDirectory(Path.Combine(package, "assets", "splash"));
    File.WriteAllText(Path.Combine(package, "profile.yaml"), "schema: omsilaunch.session-profile/v1\nid: grundorf\nname: Grundorf\nauthor: Tests\nversion: '1'\ncompatibility:\n  maps:\n    - maps\\Grundorf\\global.cfg\nnew:\n  map: maps\\Grundorf\\global.cfg\n  entrypoint-index: 1\npresets:\n  - index: 1\n    id: low\n    name: Low\n    settings:\n      graphics.maxFPS: 30\n    presentation:\n      splash:\n        mode: managed\n        assets: assets\\splash\n");
    var profile = SessionProfileCompiler.Load(root, "grundorf", 1);
    var none = new Dictionary<string, OptionalValue<string>>();
    var seed = new LaunchSpec(new(root), new(WorldMode.NewMap, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new());
    var applied = SessionProfileCompiler.Apply(profile, seed, WorldMode.NewMap);
    if (applied.World.MapIdentity.Value != "maps\\Grundorf\\global.cfg" || applied.Environment.General["graphics.maxFPS"].Value != "30" || applied.SessionProfile?.PresetId != "low") throw new InvalidOperationException("Profile did not compile into LaunchSpec.");
    var saved = SessionProfileCompiler.Apply(profile, seed with { World = seed.World with { Mode = WorldMode.SavedSituation, SituationIdentity = OptionalValue<string>.Set("situations\\sample.osn") } }, WorldMode.SavedSituation);
    if (saved.World.MapIdentity.IsSet || saved.World.PresentedEntrypointIndex.IsSet) throw new InvalidOperationException("The new block leaked into saved-situation semantics.");
    try { SessionProfileCompiler.Load(root, "..", 1); throw new InvalidOperationException("Profile traversal was accepted."); }
    catch (SessionProfileException exception) when (exception.Code == "OL_E_SESSION_PROFILE_PATH_ESCAPE") { }
    var invalidRoot = Path.Combine(root, ".omsilaunch", "session-profiles");
    WriteProfile(invalidRoot, "unknown-schema", "schema: future/v2\nid: unknown-schema\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n");
    Expect("OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED", () => SessionProfileCompiler.Load(root, "unknown-schema", 1));
    WriteProfile(invalidRoot, "duplicate-index", "schema: omsilaunch.session-profile/v1\nid: duplicate-index\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n  - index: 1\n    id: two\n    name: Two\n");
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "duplicate-index", 1));
    WriteProfile(invalidRoot, "unknown-setting", "schema: omsilaunch.session-profile/v1\nid: unknown-setting\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    settings:\n      graphics.maxFSP: 30\n");
    Expect("OL_E_SESSION_PROFILE_SETTING_UNKNOWN", () => SessionProfileCompiler.Load(root, "unknown-setting", 1));
    WriteProfile(invalidRoot, "path-escape", "schema: omsilaunch.session-profile/v1\nid: path-escape\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    presentation:\n      splash:\n        mode: managed\n        assets: ..\\outside\n");
    Expect("OL_E_SESSION_PROFILE_PATH_ESCAPE", () => SessionProfileCompiler.Load(root, "path-escape", 1));
    WriteProfile(invalidRoot, "unknown-key", "schema: omsilaunch.session-profile/v1\nid: unknown-key\nname: Test\nauthor: Test\nversion: '1'\nrun: anything\npresets:\n  - index: 1\n    id: one\n    name: One\n");
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "unknown-key", 1));
    Console.WriteLine("PASS session-profiles.strict-compiler");

    // S-17: lexical confinement is not enough; a junction inside the package
    // resolves outside it and must be rejected.
    var junctionPackage = Path.Combine(invalidRoot, "junction-escape"); Directory.CreateDirectory(junctionPackage);
    var outside = Path.Combine(root, "outside-assets"); Directory.CreateDirectory(outside);
    var link = Path.Combine(junctionPackage, "assets");
    var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + outside + "\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
    mklink.WaitForExit();
    if (mklink.ExitCode != 0 || !Directory.Exists(link)) throw new InvalidOperationException("junction fixture could not be created: " + mklink.StandardError.ReadToEnd());
    File.WriteAllText(Path.Combine(junctionPackage, "profile.yaml"), "schema: omsilaunch.session-profile/v1\nid: junction-escape\nname: Test\nauthor: Test\nversion: \'1\'\npresets:\n  - index: 1\n    id: one\n    name: One\n    presentation:\n      splash:\n        mode: managed\n        assets: assets\n");
    try { Expect("OL_E_SESSION_PROFILE_PATH_ESCAPE", () => SessionProfileCompiler.Load(root, "junction-escape", 1)); }
    finally { Directory.Delete(link); }
    Console.WriteLine("PASS session-profiles.reparse-point-rejected");
}
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }

static void WriteProfile(string profilesRoot, string id, string yaml)
{
    var package = Path.Combine(profilesRoot, id);
    Directory.CreateDirectory(package);
    File.WriteAllText(Path.Combine(package, "profile.yaml"), yaml);
}

static void Expect(string code, Action action)
{
    try { action(); throw new InvalidOperationException("Expected profile error " + code); }
    catch (SessionProfileException exception) when (exception.Code == code) { }
}
