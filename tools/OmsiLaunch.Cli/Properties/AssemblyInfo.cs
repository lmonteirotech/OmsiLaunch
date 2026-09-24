using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("OmsiLaunch.WindowsUiTests")]
[assembly: InternalsVisibleTo("OmsiLaunch.DocumentationTests")]
// user32/kernel32 resolve from the system directory only.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
