namespace OmsiLaunch.Api;

// The single definition of installation-root identity and path containment.
// Every component that decides "is this the same installation" or "is this
// path inside that root" uses these functions, so the answers cannot diverge.
// Semantics are lexical (Windows rules: case-insensitive, "." and ".."
// resolved, "/" and "\" equivalent, repeated separators collapsed). Junctions
// and symbolic links are NOT resolved here; reparse-point policy is applied
// separately where it is required.
public static class InstallationPaths
{
    // Full path without trailing separators, except for a drive root ("C:\").
    public static string NormalizeRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("An installation root is required.", nameof(root));
        var full = Path.GetFullPath(root);
        var trimmed = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return trimmed.Length == 0 || trimmed.EndsWith(':') ? full : trimmed;
    }

    // Stable identity of an installation root. Lexically equivalent spellings
    // of one root share it; different roots never do.
    public static string IdentityKey(string root) => NormalizeRoot(root).ToUpperInvariant();

    // Resolves candidate (relative to root, or absolute) and reports whether it
    // lies strictly below root. The returned relative path uses "\" and is the
    // canonical spelling of the candidate below root.
    public static bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)
    {
        relativePath = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        var normalizedRoot = NormalizeRoot(root);
        var full = Path.GetFullPath(Path.Combine(normalizedRoot, candidate));
        var relative = Path.GetRelativePath(normalizedRoot, full);
        // A different volume yields an absolute path; "." is the root itself.
        if (Path.IsPathRooted(relative) || relative == ".") return false;
        var segments = Segments(relative);
        if (segments.Count == 0 || segments[0] == "..") return false;
        relativePath = string.Join('\\', segments);
        return true;
    }

    public static IReadOnlyList<string> Segments(string relativePath) =>
        relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
}
