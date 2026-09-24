using System.Runtime.InteropServices;

// Native imports resolve from the plugin's own directory (plugins\) or the
// Windows system directory only; the OMSI root and PATH are never probed.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
