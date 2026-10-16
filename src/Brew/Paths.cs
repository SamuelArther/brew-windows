namespace Brew;

public sealed class Paths
{
    public string Prefix { get; } = Path.GetFullPath(Environment.GetEnvironmentVariable("BREW_PREFIX") ?? Environment.GetEnvironmentVariable("HOMEBREW_PREFIX")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Homebrew"));
    public string Cellar => Path.Combine(Prefix, "Cellar");
    public string Bin => Path.Combine(Prefix, "bin");
    // HOMEBREW_CACHE is the cache root (as in Homebrew); downloads go in its downloads folder, and
    // builds keep their Go/Cargo caches beside it.
    // (BREW_CACHE, brew-windows' older setting, names the downloads folder itself.)
    public string CacheRoot => Path.GetFullPath(Environment.GetEnvironmentVariable("HOMEBREW_CACHE")
        ?? (Environment.GetEnvironmentVariable("BREW_CACHE") is { Length: > 0 } downloads ? Path.Combine(downloads, "..") : Path.Combine(Prefix, "cache")));
    public string Cache => Path.GetFullPath(Environment.GetEnvironmentVariable("BREW_CACHE") is { Length: > 0 } downloads
        ? downloads : Path.Combine(CacheRoot, "downloads"));
    public string Taps => Path.Combine(Repository, "tap");
    public string Repository { get; } = FindRepository();

    static string FindRepository()
    {
        var configured = Environment.GetEnvironmentVariable("BREW_REPOSITORY");
        if (!string.IsNullOrEmpty(configured)) return Path.GetFullPath(configured);
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                if (Directory.Exists(Path.Combine(dir.FullName, "tap", "homebrew-core", "Formula")))
                    return dir.FullName;
        return AppContext.BaseDirectory;
    }

    public static string ValidateName(string name)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"\A[a-z0-9][a-z0-9+@._-]*\z"))
            throw new InvalidOperationException($"Invalid formula name: {name}");
        return name;
    }

    public static string ValidateResourceName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains("..") || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || name.Contains('/') || name.Contains('\\') || name.EndsWith('.') || name.EndsWith(' ')
            || System.Text.RegularExpressions.Regex.IsMatch(name, @"\A(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|\z)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new InvalidOperationException($"Invalid resource name: {name}");
        return name;
    }

    public string Rack(string name) => Path.Combine(Cellar, ValidateName(name));
    public static string FormulaName(string fullName) => ValidateName(fullName.Split('/')[^1]);
    // Homebrew CoreTap.new_formula_subdirectory (BSD-2-Clause).
    public static string FormulaShard(string name) => ValidateName(name).StartsWith("lib", StringComparison.Ordinal) ? "lib" : name[..1];
    public string Formula(string name) => Path.Combine(Taps, "homebrew-core", "Formula", FormulaShard(name), name + ".rb");
}
