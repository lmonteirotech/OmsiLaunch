using System.Globalization;
using OmsiLaunch.Api;

static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

Assert(WindowsUiStrings.Resolve(CultureInfo.GetCultureInfo("pt-BR"))["Tray.Running"] == "OmsiLaunch em execução", "pt-BR tray string regression.");
Assert(WindowsUiStrings.Resolve(CultureInfo.GetCultureInfo("en-GB"))["Tray.EndSession"] == "End session", "en-GB must use English.");
Assert(WindowsUiStrings.Resolve(CultureInfo.GetCultureInfo("de-AT"))["Tray.Status"] == "Status", "German language fallback regression.");
Assert(WindowsUiStrings.Resolve(CultureInfo.GetCultureInfo("fr-CA"))["Tray.EndSession"] == "Terminer la session", "French language fallback regression.");
Assert(WindowsUiStrings.Resolve(CultureInfo.GetCultureInfo("pl-PL"))["Status.Close"] == "Zamknij", "Polish locale regression.");
Assert(WindowsUiStrings.Resolve(CultureInfo.GetCultureInfo("pt-PT"))["Tray.Running"] == "OmsiLaunch is running", "pt-PT must not incorrectly use pt-BR.");
Assert(WindowsUiStrings.Resolve(CultureInfo.GetCultureInfo("ja-JP"))["Tray.Running"] == "OmsiLaunch is running", "Unknown locale must use English.");

var spec = new LaunchSpec(
    new InstallationSpec("C:\\OMSI"),
    new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set("maps\\Grundorf\\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    new DateSpec(DateTimeMode.Explicit, OptionalValue<SemanticDate>.Set(new(2026, 9, 20))),
    new TimeSpec(DateTimeMode.Explicit, OptionalValue<SemanticTime>.Set(new(17, 30, 0))),
    OptionalValue<PlayerVehicleSpec>.Unset,
    new EnvironmentSpec(new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>> { ["graphics.maxFPS"] = OptionalValue<string>.Set("40") }, new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>()),
    new LaunchBehaviorSpec(),
    Presentation: new SessionPresentationSpec(SuppressTrayIcon: false));
var plan = new SessionPlan(Guid.NewGuid(), "Omsi23004_692EBFBF", spec, new RuntimePlatformInfo("Windows", "", "x64", "x64", "x86", "x86", true, false, true, true, true, true, true, true, true), new[] { new ContentIdentity("maps\\Grundorf\\global.cfg", "map", "Grundorf") }, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<Capability>(), Array.Empty<Capability>(), Array.Empty<PlannedMutation>(), Array.Empty<LaunchDiagnostic>(), true);
var view = SessionStatusPresenter.Create(plan, new SessionStatus(plan.SessionId, SessionState.Running, Array.Empty<LaunchDiagnostic>()), WindowsUiStrings.Resolve(CultureInfo.GetCultureInfo("en-US")));
Assert(view.Heading == "Session is running", "Status heading must reflect resolved state.");
Assert(view.Sections.Single(section => section.Title == "Session").Fields.Any(field => field.Value == "Grundorf"), "Status must use resolved map display name.");
Assert(view.Sections.Single(section => section.Title == "Configuration").Fields.Single().Value == "40", "Status must include defined settings only.");
Assert(!view.Sections.Any(section => section.Title == "Vehicle"), "Absent vehicle must not produce an empty section.");
Assert(!new SessionPresentationSpec().SuppressTrayIcon && new SessionPresentationSpec(SuppressTrayIcon: true).SuppressTrayIcon, "SuppressTrayIcon default/API override regression.");
Console.WriteLine("PASS windows-ui.localization-and-status");
CliHardeningTests.Run();
