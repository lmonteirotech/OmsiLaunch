using System.Security.Cryptography;
using System.Text;
using OmsiLaunch.Api;

namespace OmsiLaunch.Core;

// Session-only visual/file inputs. This class validates every destination before
// it reaches the generic transaction, which remains the sole backup authority.
internal static class SessionVisualAssets
{
    internal sealed record Plan(IReadOnlyDictionary<string, byte[]> Overlays, IReadOnlyList<string> Deletions);
    private static readonly string[] SupportedLanguages = { "PTB", "ENG", "DEU", "FRA" };

    // Persistent OmsiLaunch-owned assets live under the installation state
    // directory. They are not OMSI files and are never removed by restore.
    internal static void EnsureInstallationAssets(string installationRoot)
    {
        var destination = Path.Combine(installationRoot, ".omsilaunch", "assets", "splash");
        Directory.CreateDirectory(destination);
        foreach (var language in SupportedLanguages)
        {
            var target = Path.Combine(destination, language + ".bmp");
            if (File.Exists(target)) continue;
            var source = Path.Combine(ResolvePackagedAssetDirectory(), language + ".bmp");
            if (!File.Exists(source)) throw new InvalidOperationException("OL_E_SPLASH_ASSET_MISSING: " + language);
            var bytes = File.ReadAllBytes(source); ValidateBmp(bytes); File.WriteAllBytes(target, bytes);
        }
    }

    internal static Plan Build(LaunchSpec spec)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase); var deletions = new List<string>();
        if (spec.EffectivePresentation.Splash == SplashMode.Managed)
        {
            var language = NormalizeLanguage(spec.EffectivePresentation.Language.IsSet ? spec.EffectivePresentation.Language.Value : ReadLanguage(spec.Installation.RootPath));
            var assetDirectory = ResolveAssetDirectory(spec);
            var english = ReadSplashAsset(assetDirectory, "ENG");
            var localized = language == "ENG" ? english : ReadSplashAsset(assetDirectory, language);
            // Every managed session provides the English fallback plus the
            // language-specific target. The latter is transaction-created when
            // a stock installation has no matching NewSplashscreen_<ISO3>.bmp.
            files["GUI\\NewSplashscreen_ENG.bmp"] = english;
            files["GUI\\NewSplashscreen_" + language + ".bmp"] = localized;
        }

        var internet = spec.EffectiveInternetTextures;
        if (internet.Mode == InternetTexturesMode.Override)
        {
            if (!internet.OverrideProfilePath.IsSet || string.IsNullOrWhiteSpace(internet.OverrideProfilePath.Value)) throw new InvalidOperationException("OL_E_ITX_PROFILE_REQUIRED");
            var profile = Path.GetFullPath(internet.OverrideProfilePath.Value!);
            if (!File.Exists(profile)) throw new FileNotFoundException("OL_E_ITX_PROFILE_MISSING", profile);
            var bytes = File.ReadAllBytes(profile);
            // Targets are recorded in their canonical spelling so that two
            // spellings of one file can never become two transaction entries.
            foreach (var target in ParseItxTargets(bytes)) { var canonical = ValidateTextureTarget(spec.Installation.RootPath, target); if (!deletions.Contains(canonical, StringComparer.OrdinalIgnoreCase)) deletions.Add(canonical); }
            deletions.Add("Texture\\standard.ipr");
            files["Texture\\standard.itx"] = bytes;
        }
        return new Plan(files, deletions);
    }

    internal static IReadOnlyList<string> DescribeTouched(LaunchSpec spec) => Build(spec).Overlays.Keys.Concat(Build(spec).Deletions).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    internal static string NormalizeLanguage(string? value) => value?.Trim().ToUpperInvariant() switch { "PTB" or "PT-BR" => "PTB", "DEU" or "DE" => "DEU", "FRA" or "FR" => "FRA", "ENG" or "EN" => "ENG", _ => "ENG" };
    private static string ReadLanguage(string root)
    {
        var path = Path.Combine(root, "options.cfg"); if (!File.Exists(path)) return "ENG";
        var lines = File.ReadAllLines(path); for (var i = 0; i + 1 < lines.Length; i++) if (string.Equals(lines[i].Trim(), "[language]", StringComparison.OrdinalIgnoreCase)) return lines[i + 1].Trim();
        return "ENG";
    }
    private static string ResolveAssetDirectory(LaunchSpec spec)
    {
        if (spec.EffectivePresentation.CustomAssetDirectory.IsSet)
        {
            var configured = spec.EffectivePresentation.CustomAssetDirectory.Value!;
            var resolved = Path.IsPathRooted(configured) ? configured : Path.Combine(spec.Installation.RootPath, configured);
            if (!Directory.Exists(resolved)) throw new InvalidOperationException("OL_E_SPLASH_ASSET_DIRECTORY_MISSING: " + resolved);
            return resolved;
        }
        var installationAssets = Path.Combine(spec.Installation.RootPath, ".omsilaunch", "assets", "splash");
        return Directory.Exists(installationAssets) ? installationAssets : ResolvePackagedAssetDirectory();
    }
    private static string ResolvePackagedAssetDirectory()
    {
        var installed = Path.Combine(AppContext.BaseDirectory, ".omsilaunch", "assets", "splash");
        return Directory.Exists(installed) ? installed : Path.Combine(AppContext.BaseDirectory, "assets", "splash");
    }
    private static void ValidateBmp(byte[] bytes)
    {
        if (bytes.Length < 54 || Encoding.ASCII.GetString(bytes, 0, 2) != "BM" || BitConverter.ToInt32(bytes, 18) != 640 || BitConverter.ToInt32(bytes, 22) != 480 || BitConverter.ToInt16(bytes, 28) != 24) throw new InvalidDataException("OL_E_SPLASH_FORMAT_UNSUPPORTED");
    }
    private static byte[] ReadSplashAsset(string directory, string language)
    {
        var path = Path.Combine(directory, language + ".bmp");
        if (!File.Exists(path)) throw new InvalidOperationException("OL_E_SPLASH_ASSET_MISSING: " + language);
        var bytes = File.ReadAllBytes(path);
        ValidateBmp(bytes);
        return bytes;
    }
    private static IEnumerable<string> ParseItxTargets(byte[] bytes)
    {
        var lines = Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        if (lines.Length == 0 || lines.Length % 2 != 0) throw new InvalidDataException("OL_E_ITX_PROFILE_INVALID");
        for (var i = 0; i < lines.Length; i += 2) { if (!Uri.TryCreate(lines[i], UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) throw new InvalidDataException("OL_E_ITX_PROFILE_INVALID"); yield return lines[i + 1]; }
    }
    // canonical root -> canonical target -> relative path -> segment rules.
    // Only a directory segment named exactly "texture" (Windows casing rules)
    // below the installation root satisfies the rule; "texture" elsewhere in the
    // absolute root, or names such as "mytexture"/"texture2", never do.
    internal static string ValidateTextureTarget(string root, string target)
    {
        if (string.IsNullOrWhiteSpace(target) || Path.IsPathRooted(target) || target.StartsWith("\\", StringComparison.Ordinal) || target.StartsWith("/", StringComparison.Ordinal) || InstallationPaths.Segments(target).Any(segment => segment == "..")) throw new InvalidDataException("OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH");
        if (!InstallationPaths.TryGetContainedRelativePath(root, target, out var relative)) throw new InvalidDataException("OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH");
        var directories = InstallationPaths.Segments(relative).SkipLast(1);
        if (!directories.Any(segment => string.Equals(segment, "texture", StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException("OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH");
        if (PathConfinement.ContainsReparsePoint(root, Path.Combine(InstallationPaths.NormalizeRoot(root), relative))) throw new InvalidDataException("OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH");
        return relative;
    }
}
