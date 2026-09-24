using System.Runtime.InteropServices;

// Native imports resolve from the assembly directory or the Windows system
// directory only; the OMSI root and PATH are never probed.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
