using Brew.Formulae;

namespace Brew;

// The environment `def install` runs in when building from source (Homebrew's superenv, Windows edition).
public static class BuildEnvironment
{
    public static void Apply(Paths paths, InstallContext context, IEnumerable<string> dependencies)
    {
        var env = context.BuildEnvironment;
        var cache = paths.CacheRoot;
        var home = Path.Combine(context.BuildPath, ".brew_home");
        Directory.CreateDirectory(home);

        // Brewed dependencies first, then the toolchain (if any), then the system.
        var dependencyBins = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d), "bin"))
            .Where(Directory.Exists);
        env["PATH"] = string.Join(";", dependencyBins.Append(paths.Bin).Concat(ToolchainPath())
            .Append(Environment.GetEnvironmentVariable("PATH") ?? ""));

        env["HOMEBREW_PREFIX"] = paths.Prefix;
        env["HOMEBREW_CELLAR"] = paths.Cellar;
        env["HOMEBREW_CACHE"] = cache;
        // Like Homebrew's HOME, the Windows profile folders point into the build too, so pip, npm and
        // cargo don't read the user's config or write caches into their profile. rustup still finds
        // its toolchains in the real profile.
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (Environment.GetEnvironmentVariable("RUSTUP_HOME") is not { Length: > 0 } && Directory.Exists(Path.Combine(profile, ".rustup")))
            env["RUSTUP_HOME"] = Path.Combine(profile, ".rustup");
        env["HOME"] = home;
        env["USERPROFILE"] = home;
        env["APPDATA"] = Path.Combine(home, "AppData", "Roaming");
        env["LOCALAPPDATA"] = Path.Combine(home, "AppData", "Local");
        Directory.CreateDirectory(env["APPDATA"]);
        Directory.CreateDirectory(env["LOCALAPPDATA"]);
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

        // MSVC (setuptools/Cython extensions, cc-rs): setuptools may pick the 32-bit-hosted cl.exe, which
        // runs out of address space compiling huge generated sources with /GL ("C1083: Cannot open
        // compiler generated file: ''"). _CL_ options apply after the command line, so this turns /GL off.
        env["_CL_"] = "/GL-";

        ApplyToolchain(paths, env, dependencies, context.BuildPath);
    }

    // `brew test` gets the compiler half of the build environment (Homebrew's test env is a build env):
    // tests compile against the keg with ENV.cc and pkg-config.
    public static void ApplyForTest(Paths paths, InstallContext context, IEnumerable<string> dependencies)
    {
        if (Msys2Root == null) return;
        var env = context.BuildEnvironment;
        // bin and lib: programs a test compiles load the keg's DLLs (MinGW puts them in either) via PATH.
        var dependencyBins = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(d => new[] { "bin", "lib" }.Select(sub => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d), sub))).Where(Directory.Exists);
        env["PATH"] = string.Join(";", dependencyBins.Append(paths.Bin).Concat(ToolchainPath())
            .Append(Environment.GetEnvironmentVariable("PATH") ?? ""));
        ApplyToolchain(paths, env, dependencies, context.BuildPath);
    }

    // MSYS2's UCRT64 environment is the C/C++ toolchain (what Xcode's Command Line Tools are for
    // Homebrew on macOS): GCC, binutils, CMake/Ninja/Meson, pkgconf, and bash + make for autotools.
    public static string Msys2Root
    {
        get
        {
            if (Environment.GetEnvironmentVariable("HOMEBREW_MSYS2_ROOT") is { Length: > 0 } root
                && File.Exists(Path.Combine(root, "usr", "bin", "bash.exe"))) return root;
            // A default install counts only once it has the UCRT64 compiler (CI images ship a bare one).
            return File.Exists(@"C:\msys64\ucrt64\bin\gcc.exe") ? @"C:\msys64" : null;
        }
    }

    // Build tools the MSYS2 toolchain already supplies (base-devel, CMake, Ninja, Meson, pkgconf), the way
    // macOS's Command Line Tools stand in for some of them upstream. Brewing them again would only put a
    // second copy ahead of the toolchain's on PATH.
    static readonly HashSet<string> ToolchainTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "autoconf", "automake", "libtool", "make", "m4", "bison", "flex", "texinfo", "gawk",
        "pkgconf", "pkg-config", "cmake", "ninja", "meson", "patch", "diffutils",
    };

    public static bool ProvidesBuildTool(string formula) => Msys2Root != null && ToolchainTools.Contains(formula);

    // GCC and friends, then MSYS2's POSIX tools: ahead of the system PATH, where another MSYS runtime
    // (Git for Windows' usrin) would otherwise supply expr/sed/sh and mangle their arguments.
    static IEnumerable<string> ToolchainPath() => Msys2Root is not { } root ? []
        : PosixRuntime ? [Path.Combine(root, "usr", "bin")]
        : [Path.Combine(root, "ucrt64", "bin"), Path.Combine(root, "usr", "bin")];

    // POSIX-runtime builds (HOMEBREW_POSIX_RUNTIME=1) compile with MSYS2's own GCC against msys-2.0.dll,
    // MSYS2's Cygwin-derived POSIX layer: fork, termios, sockets, signals. For Unix programs MinGW can't
    // build; the keg carries msys-2.0.dll like Git for Windows does.
    public static bool PosixRuntime => Environment.GetEnvironmentVariable("HOMEBREW_POSIX_RUNTIME") == "1";

    static void ApplyToolchain(Paths paths, Dictionary<string, string> env, IEnumerable<string> dependencies, string buildPath)
    {
        if (Msys2Root is not { } root) return;
        var posix = PosixRuntime;
        // MSYS tools want /c/x paths in ':'-separated lists; UCRT64 tools take C:/x and ';'.
        string Posix(string p) => posix && p.Length > 1 && p[1] == ':'
            ? "/" + char.ToLowerInvariant(p[0]) + p[2..].Replace('\\', '/') : p.Replace('\\', '/');
        var listSeparator = posix ? ":" : ";";
        var ucrt = posix ? Path.Combine(root, "usr") : Path.Combine(root, "ucrt64");
        var kegs = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d))).Where(Directory.Exists).ToList();

        // macOS spells the GNU tools g-prefixed (glibtoolize, gsed, gmake); formulae call those names.
        var shims = Path.Combine(buildPath, ".brew_shims");
        Directory.CreateDirectory(shims);
        foreach (var (alias, tool) in new[] { ("glibtoolize", "libtoolize"), ("glibtool", "libtool"), ("gsed", "sed"), ("gmake", "make"), ("gm4", "m4") })
            File.WriteAllText(Path.Combine(shims, alias), $"#!/usr/bin/env bash\nexec {tool} \"$@\"\n");
        env["PATH"] = shims + ";" + env["PATH"];

        env["HOMEBREW_MSYS2_ROOT"] = root;
        env["MSYSTEM"] = posix ? "MSYS" : "UCRT64";
        env["MSYS2_PATH_TYPE"] = "inherit";
        env["CHERE_INVOKING"] = "1";
        env["CC"] = "gcc";
        env["CXX"] = "g++";
        env["CPP"] = "cpp";
        env["AR"] = "ar";
        env["RANLIB"] = "ranlib";
        env["CMAKE_GENERATOR"] = "Ninja";
        // MSYS2 ships GCC 15, which defaults to C23 and turns several old warnings into errors. Upstream
        // formulae target older compilers (Apple clang, Linux GCC 12), so build C as gnu17 and keep those
        // diagnostics warnings; a project's own -std flag still comes later and wins.
        env["CFLAGS"] = "-O2 -pipe -std=gnu17 -Wno-error=incompatible-pointer-types -Wno-error=int-conversion"
            + " -Wno-error=implicit-function-declaration -Wno-error=implicit-int -Wno-error=return-mismatch";
        env["CXXFLAGS"] = "-O2 -pipe";
        env["CPPFLAGS"] = string.Join(" ", kegs.Append(paths.Prefix).Select(k => Path.Combine(k, "include")).Where(Directory.Exists).Select(d => "-I" + Posix(d)));
        env["LDFLAGS"] = string.Join(" ", kegs.Append(paths.Prefix).Select(k => Path.Combine(k, "lib")).Where(Directory.Exists).Select(d => "-L" + Posix(d)));
        env["PKG_CONFIG_PATH"] = string.Join(listSeparator, kegs.Append(paths.Prefix)
            .SelectMany(k => new[] { Path.Combine(k, "lib", "pkgconfig"), Path.Combine(k, "share", "pkgconfig") })
            .Where(Directory.Exists).Select(Posix));
        env["PKG_CONFIG_LIBDIR"] = Posix(Path.Combine(ucrt, "lib", "pkgconfig"));
        // aclocal (MSYS perl) splits on ':', so this list always uses /c/x paths.
        string Msys(string p) => p.Length > 1 && p[1] == ':' ? "/" + char.ToLowerInvariant(p[0]) + p[2..].Replace('\\', '/') : p.Replace('\\', '/');
        env["ACLOCAL_PATH"] = string.Join(":", kegs.Append(paths.Prefix).Select(k => Path.Combine(k, "share", "aclocal"))
            .Where(Directory.Exists).Select(Msys));
        env["CMAKE_PREFIX_PATH"] = string.Join(listSeparator, kegs.Append(paths.Prefix).Select(Posix));

        // Old config.guess scripts don't know MinGW or MSYS; tell configure what it is building on.
        var site = Path.Combine(buildPath, ".brew_config.site");
        var triple = posix ? "x86_64-pc-cygwin" : "x86_64-w64-mingw32";
        File.WriteAllText(site, "ac_cv_build=${ac_cv_build:-" + triple + "}\n");
        env["CONFIG_SITE"] = Posix(site);
    }

    // Executables built with MinGW GCC may import its runtime and any UCRT64 libraries the build found.
    // Copy those DLLs next to each binary so the keg (and its bottle) runs without MSYS2.
    public static void BundleToolchainDlls(string keg)
    {
        if (Msys2Root is not { } root) return;
        // UCRT64 runtime DLLs for native builds; msys-*.dll (the POSIX layer) for POSIX-runtime builds.
        var toolchainBin = Path.Combine(root, PosixRuntime ? "usr" : "ucrt64", "bin");
        foreach (var binary in Directory.EnumerateFiles(keg, "*.*", SearchOption.AllDirectories)
                     .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            var directory = Path.GetDirectoryName(binary)!;
            var pending = new Queue<string>(PortableExecutable.Imports(binary));
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (pending.Count > 0)
            {
                var dll = pending.Dequeue();
                if (!seen.Add(dll)) continue;
                var source = Path.Combine(toolchainBin, dll);
                var local = Path.Combine(directory, dll);
                // Each binary's own folder needs the DLL: Windows looks beside the exe, not elsewhere in the keg.
                if (!File.Exists(source) || File.Exists(local)) continue;
                File.Copy(source, local);
                foreach (var next in PortableExecutable.Imports(source)) pending.Enqueue(next);
            }
        }
    }
}

// Minimal PE import-table reader (DLL names a binary imports).
public static class PortableExecutable
{
    public static IEnumerable<string> Imports(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            using var reader = new System.Reflection.PortableExecutable.PEReader(stream);
            var headers = reader.PEHeaders;
            var directory = headers.PEHeader?.ImportTableDirectory;
            if (directory is not { Size: > 0 } table) return [];
            var image = reader.GetEntireImage().GetContent().ToArray();
            int Offset(int rva)
            {
                foreach (var s in headers.SectionHeaders)
                    if (rva >= s.VirtualAddress && rva < s.VirtualAddress + Math.Max(s.VirtualSize, s.SizeOfRawData))
                        return rva - s.VirtualAddress + s.PointerToRawData;
                return -1;
            }
            var names = new List<string>();
            for (var at = Offset(table.RelativeVirtualAddress); at >= 0 && at + 20 <= image.Length; at += 20)
            {
                var nameRva = BitConverter.ToInt32(image.AsSpan(at + 12, 4));
                if (nameRva == 0) break;
                var name = Offset(nameRva);
                if (name < 0) break;
                var end = image.AsSpan(name).IndexOf((byte)0);
                names.Add(System.Text.Encoding.ASCII.GetString(image, name, end));
            }
            return names;
        }
        catch (BadImageFormatException) { return []; }
    }
}
