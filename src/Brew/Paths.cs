namespace Brew;

public sealed class Paths
{
    public string Prefix { get; } = Path.GetFullPath(Environment.GetEnvironmentVariable("BREW_PREFIX") ?? Environment.GetEnvironmentVariable("HOMEBREW_PREFIX")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Homebrew"));
    public string Cellar => Path.Combine(Prefix, "Cellar");
    public string Bin => Path.Combine(Prefix, "bin");
    public string Cache => Path.GetFullPath(Environment.GetEnvironmentVariable("BREW_CACHE") ?? Environment.GetEnvironmentVariable("HOMEBREW_CACHE") ?? Path.Combine(Prefix, "cache", "downloads"));
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

    public string Rack(string name) => Path.Combine(Cellar, ValidateName(name));
    // Homebrew CoreTap.new_formula_subdirectory (BSD-2-Clause).
    public static string FormulaShard(string name) => ValidateName(name).StartsWith("lib", StringComparison.Ordinal) ? "lib" : name[..1];
    public string Formula(string name) => Path.Combine(Taps, "homebrew-core", "Formula", FormulaShard(name), name + ".rb");
}
