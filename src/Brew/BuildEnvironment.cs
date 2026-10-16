using Brew.Formulae;

namespace Brew;

// The environment `def install` runs in when building from source (Homebrew's superenv, Windows edition).
public static class BuildEnvironment
{
    public static void Apply(Paths paths, InstallContext context, IEnumerable<string> dependencies)
    {
        var env = context.BuildEnvironment;
        var cache = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(paths.Cache))!;
        var home = Path.Combine(context.BuildPath, ".brew_home");
        Directory.CreateDirectory(home);

        var dependencyBins = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d), "bin"))
            .Where(Directory.Exists);
        env["PATH"] = string.Join(";", dependencyBins.Append(paths.Bin)
            .Append(Environment.GetEnvironmentVariable("PATH") ?? ""));

        env["HOMEBREW_PREFIX"] = paths.Prefix;
        env["HOMEBREW_CELLAR"] = paths.Cellar;
        env["HOMEBREW_CACHE"] = cache;
        env["HOME"] = home;
        if (Environment.GetEnvironmentVariable("HOMEBREW_MAKE_JOBS") is not { Length: > 0 })
            env["HOMEBREW_MAKE_JOBS"] = Environment.ProcessorCount.ToString();

        // Go: shared module/build caches, never auto-download a different toolchain.
        env["GOPATH"] = Path.Combine(cache, "go_cache");
        env["GOMODCACHE"] = Path.Combine(cache, "go_mod_cache");
        env["GOCACHE"] = Path.Combine(cache, "go_build_cache");
        env["GOTOOLCHAIN"] = "local";
        env["GOFLAGS"] = "-buildvcs=false";

        // Python: brewed CPython installs `pip install --prefix` trees in upstream's layout (see python@3.15).
        env["HOMEBREW_PYTHON_PREFIX_SCHEME"] = "posix";

        // Rust: shared registry cache; static CRT so binaries need no Visual C++ runtime.
        env["CARGO_HOME"] = Path.Combine(cache, "cargo_cache");
        var rustflags = Environment.GetEnvironmentVariable("RUSTFLAGS");
        env["RUSTFLAGS"] = string.IsNullOrEmpty(rustflags) ? "-C target-feature=+crt-static" : rustflags + " -C target-feature=+crt-static";
    }
}
