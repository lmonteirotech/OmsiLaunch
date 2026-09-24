#include <windows.h>
#include <shellapi.h>
#include <nethost.h>
#include <hostfxr.h>

#include <string>
#include <vector>

// CommandLineToArgvW lives in shell32 (a Windows system library). The console
// project does not list it explicitly, so bind it from the source file.
#pragma comment(lib, "shell32.lib")

// Shim failure codes. They sit outside the public CLI exit codes defined by
// OmsiLaunch.Api.PublicExitCode (0, 2, 3, 4, 5, 6, 7, 8, 10) so a caller can
// tell a host-startup failure apart from a managed controller result.
//
//   Code  Meaning
//   ----  -------------------------------------------------------------
//   100   executable path could not be resolved (GetModuleFileNameW)
//   101   command line could not be tokenized (CommandLineToArgvW)
//   102   hostfxr location probe failed (get_hostfxr_path size query)
//   103   hostfxr path could not be retrieved (get_hostfxr_path)
//   104   hostfxr library could not be loaded (LoadLibraryW)
//   105   required hostfxr exports are missing (GetProcAddress)
//   106   managed host could not be initialized
//         (hostfxr_initialize_for_dotnet_command_line)
namespace ShimExit
{
    constexpr int ExecutablePathUnavailable = 100;
    constexpr int CommandLineUnavailable = 101;
    constexpr int HostfxrProbeFailed = 102;
    constexpr int HostfxrPathUnavailable = 103;
    constexpr int HostfxrLoadFailed = 104;
    constexpr int HostfxrExportsMissing = 105;
    constexpr int HostInitializationFailed = 106;
}

namespace
{
    // Resolves the full path of this executable. The buffer grows until the
    // path fits so long install paths (beyond MAX_PATH) are supported.
    std::wstring GetExecutablePath()
    {
        // 64K wide characters is beyond the longest extended path Windows
        // accepts; it only guards against an unexpected runaway loop.
        constexpr size_t maximumLength = 65536;
        std::vector<wchar_t> buffer(MAX_PATH);
        for (;;)
        {
            const auto length = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
            if (length == 0)
            {
                return {};
            }
            if (length < buffer.size())
            {
                return std::wstring(buffer.data(), length);
            }
            if (buffer.size() >= maximumLength)
            {
                return {};
            }
            buffer.resize(buffer.size() * 2);
        }
    }
}

int wmain()
{
    const auto executablePath = GetExecutablePath();
    if (executablePath.empty())
    {
        return ShimExit::ExecutablePathUnavailable;
    }

    std::wstring directory = executablePath;
    directory.erase(directory.find_last_of(L"\\/") + 1);
    const auto controller = directory + L"OmsiLaunch.Controller.dll";

    // Both shims tokenize the raw command line with CommandLineToArgvW so the
    // console and GUI hosts hand identical arguments to the controller.
    int argc = 0;
    const auto argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argv == nullptr)
    {
        return ShimExit::CommandLineUnavailable;
    }

    size_t hostfxrPathSize = 0;
    // nethost documents the first null-buffer call as the size probe. The
    // numeric error constant is intentionally not exported by older headers.
    if (get_hostfxr_path(nullptr, &hostfxrPathSize, nullptr) == 0 || hostfxrPathSize == 0)
    {
        LocalFree(argv);
        return ShimExit::HostfxrProbeFailed;
    }
    std::vector<wchar_t> hostfxrPath(hostfxrPathSize);
    if (get_hostfxr_path(hostfxrPath.data(), &hostfxrPathSize, nullptr) != 0)
    {
        LocalFree(argv);
        return ShimExit::HostfxrPathUnavailable;
    }

    const auto hostfxr = LoadLibraryW(hostfxrPath.data());
    if (hostfxr == nullptr)
    {
        LocalFree(argv);
        return ShimExit::HostfxrLoadFailed;
    }
    const auto initializeForCommandLine = reinterpret_cast<hostfxr_initialize_for_dotnet_command_line_fn>(
        GetProcAddress(hostfxr, "hostfxr_initialize_for_dotnet_command_line"));
    const auto runApp = reinterpret_cast<hostfxr_run_app_fn>(GetProcAddress(hostfxr, "hostfxr_run_app"));
    const auto closeHostContext = reinterpret_cast<hostfxr_close_fn>(GetProcAddress(hostfxr, "hostfxr_close"));
    if (initializeForCommandLine == nullptr || runApp == nullptr || closeHostContext == nullptr)
    {
        FreeLibrary(hostfxr);
        LocalFree(argv);
        return ShimExit::HostfxrExportsMissing;
    }

    // The command-line initializer takes the managed assembly first and passes
    // only the remaining values to its managed Main method. This keeps the
    // controller path out of the public CLI argument list.
    std::vector<const wchar_t*> hostArguments;
    hostArguments.reserve(static_cast<size_t>(argc));
    hostArguments.push_back(controller.c_str());
    for (auto index = 1; index < argc; ++index)
    {
        hostArguments.push_back(argv[index]);
    }

    hostfxr_initialize_parameters parameters = {};
    parameters.size = sizeof(parameters);
    parameters.host_path = executablePath.c_str();
    hostfxr_handle context = nullptr;
    if (initializeForCommandLine(static_cast<int>(hostArguments.size()), hostArguments.data(), &parameters, &context) != 0 || context == nullptr)
    {
        FreeLibrary(hostfxr);
        LocalFree(argv);
        return ShimExit::HostInitializationFailed;
    }

    const auto exitCode = runApp(context);
    closeHostContext(context);
    FreeLibrary(hostfxr);
    LocalFree(argv);
    return exitCode;
}
