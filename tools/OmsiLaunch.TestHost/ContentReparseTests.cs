using OmsiLaunch.Content;
using System.Diagnostics;
using System.Text;

// Regression tests for S-23 (reparse-point cycles during discovery) and the
// Content half of S-07 (Windows-1252 decoding of OMSI content files).
internal static class ContentReparseTests
{
    // A directory junction maps\A\loop -> maps forms an infinite cycle. Discovery
    // must skip the reparse point, finish promptly, and report the map exactly once.
    public static void DiscoveryJunctionCycle()
    {
        var root = CreateRoot();
        var link = Path.Combine(root, "maps", "A", "loop");
        try
        {
            WriteText(root, "maps\\A\\global.cfg", "[entrypoints]\r\n0\r\n");
            CreateJunction(link, Path.Combine(root, "maps"));
            Require(Directory.Exists(Path.Combine(link, "A")), "junction fixture did not resolve back into maps\\A");

            var discovery = Task.Run(() => new FileSystemContentCatalog(root).EnumerateMaps());
            if (!discovery.Wait(TimeSpan.FromSeconds(10)))
                throw new InvalidOperationException("discovery followed a junction cycle: EnumerateMaps did not finish within 10 seconds");

            var maps = discovery.Result;
            Require(maps.Count == 1, "expected exactly one map but discovery reported " + maps.Count);
            Require(string.Equals(maps[0].Identity, "maps\\A\\global.cfg", StringComparison.OrdinalIgnoreCase), "unexpected map identity: " + maps[0].Identity);
            Require(!maps.Any(map => map.RelativePath.Contains("\\loop\\", StringComparison.OrdinalIgnoreCase)), "content behind the junction was duplicated into discovery results");
        }
        finally
        {
            // Directory.Delete on a junction removes the link itself and does not follow
            // it, so the real maps\ tree is untouched until the recursive delete below.
            // Best effort: if the watchdog fired, the runaway enumeration may still hold
            // handles under root, and a cleanup failure must not mask the real assertion.
            try { if (Directory.Exists(link)) Directory.Delete(link); } catch (IOException) { }
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }

    // OMSI content is Windows-1252 ANSI. Byte 0xE7 is 'ç' in CP1252 but an invalid
    // UTF-8 lead byte, so a UTF-8 reader turns the friendly name into U+FFFD.
    public static void ReadLinesCp1252()
    {
        var root = CreateRoot();
        try
        {
            // Latin1 shares the 0x00-0xFF -> U+0000-U+00FF mapping with CP1252 for 'ç',
            // so it produces the exact ANSI bytes OMSI would have written without
            // requiring the code page provider inside the test itself.
            var bytes = Encoding.Latin1.GetBytes("[friendlyname]\r\nFrançois\r\nBus\r\n[number]\r\nnumbers.org\r\n");
            Require(bytes.Contains((byte)0xE7), "fixture did not contain byte 0xE7");
            WriteBytes(root, "Vehicles\\Demo\\demo.bus", bytes);

            var vehicles = new FileSystemContentCatalog(root).EnumerateVehicles();
            Require(vehicles.Count == 1, "expected exactly one vehicle but discovery reported " + vehicles.Count);
            var name = vehicles[0].DisplayName ?? string.Empty;
            Require(name.Contains('ç'), "friendly name was not decoded as Windows-1252: '" + name + "'");
            Require(!name.Contains('�'), "friendly name contains a replacement character: '" + name + "'");
            Require(name == "François Bus", "unexpected friendly name: '" + name + "'");
        }
        finally { Directory.Delete(root, true); }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Reparse-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void WriteText(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private static void WriteBytes(string root, string relative, byte[] bytes)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    // Directory junctions do not require elevation, unlike symbolic links.
    private static void CreateJunction(string link, string target)
    {
        var start = new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("mklink could not be started");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 || !Directory.Exists(link))
            throw new InvalidOperationException("mklink /J failed (exit " + process.ExitCode + "): " + stdout.Trim() + " " + stderr.Trim());
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
