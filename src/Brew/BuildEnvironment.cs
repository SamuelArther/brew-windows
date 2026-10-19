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
        // Like superenv's deps (the recursive dependencies), the dependencies' own runtime dependencies count too:
        // fizz finds folly's CMake config, which then needs double-conversion's.
        var direct = dependencies.ToList();
        dependencies = WithRuntimeDependencies(paths, direct);

        // Brewed dependencies first, then the toolchain (if any), then the system. A brewed copy of the toolchain's
        // own compiler (the gcc formula, which openblas depends on) doesn't go ahead of it.
        var dependencyBins = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(d => Msys2Root == null || !IsCompilerKeg(Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d))))
            .Select(d => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d), "bin"))
            .Where(Directory.Exists);
        env["PATH"] = string.Join(";", dependencyBins.Concat(PythonLibexecBins(paths, dependencies)).Append(paths.Bin).Concat(ToolchainPath())
            .Concat(Msys2DllDirectories(paths, dependencies))
            .Append(Environment.GetEnvironmentVariable("PATH") ?? ""));
        SetExtensionDllDirectories(paths, env, dependencies);

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

        ApplyToolchain(paths, env, dependencies, context.BuildPath, direct);
    }

    // `brew test` gets the compiler half of the build environment (Homebrew's test env is a build env):
    // tests compile against the keg with ENV.cc and pkg-config.
    public static void ApplyForTest(Paths paths, InstallContext context, IEnumerable<string> dependencies, string tested = null)
    {
        var env = context.BuildEnvironment;
        // bin and lib: programs a test compiles load the keg's DLLs (MinGW puts them in either) via PATH,
        // and the DLLs those import, so the dependencies' own runtime dependencies too (gd's test links libgd,
        // which loads libpng's and freetype's DLLs).
        dependencies = WithRuntimeDependencies(paths, dependencies);
        var dependencyBins = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(d => new[] { "bin", "lib", "libexec/ucrt64/bin" }.Select(sub => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d), sub))).Where(Directory.Exists);
        // Last, brew itself: Homebrew's tests find `brew` in HOMEBREW_PREFIX/bin (gpk, taproom and frizbee run it).
        var brewDir = Path.GetDirectoryName(Environment.ProcessPath);
        env["PATH"] = string.Join(";", dependencyBins.Concat(PythonLibexecBins(paths, dependencies)).Append(paths.Bin).Concat(ToolchainPath())
            .Append(Environment.GetEnvironmentVariable("PATH") ?? "")
            .Concat(brewDir != null && File.Exists(Path.Combine(brewDir, "brew.exe")) ? [brewDir] : []));
        SetExtensionDllDirectories(paths, env, dependencies);
        // Formula#common_sandbox_env: tests share the package managers' caches with builds (cargo-audit's test
        // looks for its advisory database in HOMEBREW_CACHE/cargo_cache).
        var cache = paths.CacheRoot;
        env["HOMEBREW_PREFIX"] = paths.Prefix;
        env["HOMEBREW_CACHE"] = cache;
        env["GOPATH"] = Path.Combine(cache, "go_cache");
        env["GOMODCACHE"] = Path.Combine(cache, "go_mod_cache");
        env["GOCACHE"] = Path.Combine(cache, "go_build_cache");
        env["GOTOOLCHAIN"] = "local";
        env["CARGO_HOME"] = Path.Combine(cache, "cargo_cache");
        // Without MSYS2, ENV.cc is whatever compiler is on PATH (LLVM-MinGW clang); the DLL paths above still apply.
        if (Msys2Root != null) ApplyToolchain(paths, env, dependencies, context.BuildPath, tested: tested);
    }

    // Superenv#homebrew_extra_paths: the python@3.x dependencies' libexec/bin (unversioned python and python3),
    // newest first, after the dependencies' bin. Meson's python.find_installation('python3') and plain `python3`
    // calls then get the brewed Python that python-setuptools is linked for, not MSYS2's own (glib's
    // gobject-introspection: "python3 is missing modules: setuptools") or whichever python3 HOMEBREW_PREFIX/bin has.
    public static IEnumerable<string> PythonLibexecBins(Paths paths, IEnumerable<string> dependencies) =>
        dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => (Name: d, Match: System.Text.RegularExpressions.Regex.Match(d, @"^python(?:@(\d+(?:\.\d+)*))?$")))
            .Where(p => p.Match.Success)
            .OrderByDescending(p => Version.TryParse(p.Match.Groups[1].Success ? p.Match.Groups[1].Value : "0", out var v) ? v : new Version())
            .Select(p => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(p.Name), "libexec", "bin"))
            .Where(Directory.Exists).ToList();

    // Python 3.8+ ignores PATH when loading an extension module's DLLs, so a MinGW-built .pyd (gobject-introspection's
    // _giscanner, which needs libintl, pcre2 and GCC's runtime) failed with "DLL load failed while importing". GLib's
    // tools add GI_EXTRA_BASE_DLL_DIRS with os.add_dll_directory: the dependencies' and toolchain's DLL folders.
    static void SetExtensionDllDirectories(Paths paths, Dictionary<string, string> env, IEnumerable<string> dependencies)
    {
        if (env.ContainsKey("GI_EXTRA_BASE_DLL_DIRS") || Environment.GetEnvironmentVariable("GI_EXTRA_BASE_DLL_DIRS") is { Length: > 0 }) return;
        var dirs = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(d => new[] { "bin", "lib", "libexec/ucrt64/bin" }.Select(sub => Path.GetFullPath(Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d), sub))))
            .Append(paths.Bin)
            .Concat(Msys2Root is { } root ? [Path.Combine(root, PosixRuntime ? "usr" : "ucrt64", "bin")] : [])
            .Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (dirs.Count == 0) return;
        env["GI_EXTRA_BASE_DLL_DIRS"] = string.Join(";", dirs);
        // PyGObject's gi/__init__.py reads its own variable for the same thing (_gi.pyd needs libgirepository and GLib).
        if (!env.ContainsKey("PYGI_DLL_PATH") && Environment.GetEnvironmentVariable("PYGI_DLL_PATH") is not { Length: > 0 })
            env["PYGI_DLL_PATH"] = env["GI_EXTRA_BASE_DLL_DIRS"];
    }

    // An MSYS2-packaged keg that carries the C runtime's headers: a GCC of its own, not a library.
    static bool IsCompilerKeg(string keg) => File.Exists(Path.Combine(keg, "libexec", "ucrt64", "include", "stdlib.h"));

    // Formulae packaged from MSYS2 (msys2_install) keep their DLLs in libexec/ucrt64/bin, which programs a build
    // runs need on PATH (fltk@1.3's fluid.exe loads libpng's libpng16-16.dll). After the toolchain: these folders
    // also hold the packages' own tools, which shouldn't shadow anything.
    static IEnumerable<string> Msys2DllDirectories(Paths paths, IEnumerable<string> dependencies) =>
        dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d), "libexec", "ucrt64", "bin"))
            .Where(Directory.Exists).ToList();

    // The names plus every installed keg's recorded runtime dependencies, recursively (receipts list direct ones).
    public static List<string> WithRuntimeDependencies(Paths paths, IEnumerable<string> names)
    {
        var all = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>(names);
        while (pending.Count > 0)
        {
            var name = pending.Dequeue();
            if (!seen.Add(name)) continue;
            all.Add(name);
            var receipt = Path.Combine(paths.Prefix, "opt", Paths.FormulaName(name), "INSTALL_RECEIPT.json");
            try
            {
                if (!File.Exists(receipt)) continue;
                using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(receipt));
                if (json.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                    && json.RootElement.TryGetProperty("Dependencies", out var list) && list.ValueKind == System.Text.Json.JsonValueKind.Array)
                    foreach (var dependency in list.EnumerateArray())
                        if (dependency.ValueKind == System.Text.Json.JsonValueKind.String) pending.Enqueue(dependency.GetString()!);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { }
        }
        return all;
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

    // Where a toolchain-provided tool lives, standing in for its opt prefix (libtecla copies
    // #{formula_opt_prefix("automake")}/share/automake-1.18/config.guess). Null when it's not a toolchain tool.
    public static string ToolchainPrefix(string formula) =>
        !ProvidesBuildTool(formula) ? null
        : Path.Combine(Msys2Root, !PosixRuntime && formula is "pkgconf" or "pkg-config" or "cmake" or "ninja" or "meson" ? "ucrt64" : "usr");

    // Old config.guess/config.sub scripts don't know MinGW or MSYS ("can not guess host type", "machine
    // `x86_64-w64' not recognized"). Like the gnu-config copies some formulae make for new platforms, every
    // copy in the source tree older than the toolchain's automake one is replaced before the build.
    public static void RefreshConfigScripts(string sourceTree)
    {
        if (Msys2Root is not { } root || !Directory.Exists(Path.Combine(root, "usr", "share"))) return;
        var fresh = Directory.GetDirectories(Path.Combine(root, "usr", "share"), "automake-*")
            .OrderByDescending(d => Version.TryParse(Path.GetFileName(d)["automake-".Length..], out var v) ? v : new Version())
            .Select(d => (Guess: Path.Combine(d, "config.guess"), Sub: Path.Combine(d, "config.sub")))
            .FirstOrDefault(p => File.Exists(p.Guess) && File.Exists(p.Sub));
        if (fresh.Guess == null) return;
        var latest = new Dictionary<string, (string Path, string Stamp)>
        {
            ["config.guess"] = (fresh.Guess, ConfigStamp(fresh.Guess)), ["config.sub"] = (fresh.Sub, ConfigStamp(fresh.Sub)),
        };
        foreach (var file in Paths.TreeFiles(sourceTree).Where(f => latest.ContainsKey(Path.GetFileName(f).ToLowerInvariant())).ToList())
        {
            var (source, stamp) = latest[Path.GetFileName(file).ToLowerInvariant()];
            var own = ConfigStamp(file);
            // Only GNU's scripts (they carry a timestamp='YYYY-MM-DD' line), and only older ones.
            if (own == null || stamp == null || string.CompareOrdinal(own, stamp) >= 0) continue;
            File.Copy(source, file, true);
        }
    }

    static string ConfigStamp(string file)
    {
        try
        {
            using var reader = new StreamReader(file);
            for (var n = 0; n < 60 && reader.ReadLine() is { } line; n++)
                if (System.Text.RegularExpressions.Regex.Match(line, @"^timestamp='(\d{4}-\d{2}-\d{2})'") is { Success: true } m) return m.Groups[1].Value;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return null;
    }

    // Visual Studio's native x64 tools (vcvars64), for Python extension builds. setuptools otherwise always
    // cross-compiles with the 32-bit-hosted cl.exe, which runs out of address space on huge generated
    // sources such as Cython's ("C1083: Cannot open compiler generated file: ''"). DISTUTILS_USE_SDK makes
    // it use the environment it's given. Null when Visual Studio's C++ tools aren't installed.
    public static IReadOnlyDictionary<string, string> MsvcX64 => msvcX64.Value;
    static readonly Lazy<IReadOnlyDictionary<string, string>> msvcX64 = new(() =>
    {
        try
        {
            var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (!File.Exists(vswhere)) return null;
            var install = Run(vswhere, ["-latest", "-products", "*", "-requires", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-property", "installationPath"]).Trim();
            var vcvars = Path.Combine(install, "VC", "Auxiliary", "Build", "vcvars64.bat");
            if (install.Length == 0 || !File.Exists(vcvars)) return null;
            var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in Run("cmd.exe", ["/d", "/s", "/c", $"\"\"{vcvars}\" >nul && set\""]).Split('\n'))
                if (line.IndexOf('=') is > 0 and var eq) env[line[..eq]] = line[(eq + 1)..].TrimEnd('\r');
            if (!env.ContainsKey("INCLUDE") || !env.ContainsKey("LIB")) return null;
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DISTUTILS_USE_SDK"] = "1", ["MSSdk"] = "1",
                ["INCLUDE"] = env["INCLUDE"], ["LIB"] = env["LIB"], ["LIBPATH"] = env.GetValueOrDefault("LIBPATH", ""),
                // Only the directories vcvars adds; PipInstall puts them first on PATH for the build.
                ["__MSVC_PATH"] = string.Join(";", env.GetValueOrDefault("PATH", "").Split(';')
                    .Except((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'), StringComparer.OrdinalIgnoreCase)),
            };
        }
        catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception or InvalidOperationException) { return null; }

        static string Run(string file, string[] args)
        {
            var start = new System.Diagnostics.ProcessStartInfo(file) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in args) start.ArgumentList.Add(arg);
            if (file == "cmd.exe") { start.ArgumentList.Clear(); start.Arguments = string.Join(" ", args); }
            using var process = System.Diagnostics.Process.Start(start)!;
            var output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : "";
        }
    });

    // GCC and friends, then MSYS2's POSIX tools: ahead of the system PATH, where another MSYS runtime
    // (Git for Windows' usrin) would otherwise supply expr/sed/sh and mangle their arguments.
    // Perl's own tools (pod2man, which OpenSSL's `make install` runs, pod2text ...) live in usr/bin/*_perl, as on Arch.
    static IEnumerable<string> ToolchainPath() => Msys2Root is not { } root ? []
        : (PosixRuntime ? [Path.Combine(root, "usr", "bin")] : new[] { Path.Combine(root, "ucrt64", "bin"), Path.Combine(root, "usr", "bin") })
            .Concat(new[] { "core_perl", "vendor_perl", "site_perl" }.Select(d => Path.Combine(root, "usr", "bin", d)).Where(Directory.Exists));

    // POSIX-runtime builds (HOMEBREW_POSIX_RUNTIME=1) compile with MSYS2's own GCC against msys-2.0.dll,
    // MSYS2's Cygwin-derived POSIX layer: fork, termios, sockets, signals. For Unix programs MinGW can't
    // build; the keg carries msys-2.0.dll like Git for Windows does.
    public static bool PosixRuntime => Environment.GetEnvironmentVariable("HOMEBREW_POSIX_RUNTIME") == "1";

    static void ApplyToolchain(Paths paths, Dictionary<string, string> env, IEnumerable<string> dependencies, string buildPath, IEnumerable<string> direct = null, string tested = null)
    {
        if (Msys2Root is not { } root) return;
        var posix = PosixRuntime;
        // MSYS tools want /c/x paths in ':'-separated lists; UCRT64 tools take C:/x and ';'.
        string Posix(string p) => posix && p.Length > 1 && p[1] == ':'
            ? "/" + char.ToLowerInvariant(p[0]) + p[2..].Replace('\\', '/') : p.Replace('\\', '/');
        var listSeparator = posix ? ":" : ";";
        var ucrt = posix ? Path.Combine(root, "usr") : Path.Combine(root, "ucrt64");
        var kegs = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d))).Where(Directory.Exists)
            // Formulae packaged from MSYS2 (msys2_install) keep their headers, import libraries and .pc files
            // under libexec/ucrt64; dependents build against that tree like any other prefix.
            .SelectMany(k => posix || !Directory.Exists(Path.Combine(k, "libexec", "ucrt64")) ? [k] : new[] { k, Path.Combine(k, "libexec", "ucrt64") })
            .ToList();
        // A brewed compiler (the gcc formula, MSYS2's GCC) carries the C runtime's own headers. As a user include
        // directory that tree comes before libstdc++'s, whose <cstdlib> then can't #include_next <stdlib.h>
        // (ntfs2btrfs). It is the compiler's system directory, not a library's.
        var includeKegs = kegs.Where(k => !File.Exists(Path.Combine(k, "include", "stdlib.h"))).ToList();

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
        // Projects' own -Werror also meets warnings those compilers don't have (GCC 14/15's
        // -Wcalloc-transposed-args in simg2img, -Wunterminated-string-initialization in jerryscript) and MSVC
        // pragmas MinGW's _WIN32 lets through (jinx's #pragma warning): those stay warnings too.
        const string newerWarnings = " -Wno-error=calloc-transposed-args -Wno-error=unknown-pragmas";
        env["CFLAGS"] = "-O2 -pipe -std=gnu17 -Wno-error=incompatible-pointer-types -Wno-error=int-conversion"
            + " -Wno-error=implicit-function-declaration -Wno-error=implicit-int -Wno-error=return-mismatch"
            + " -Wno-error=unterminated-string-initialization" + newerWarnings;
        // GCC 15+ rejects errors in uninstantiated template bodies (-Wtemplate-body: rapidjson 1.1's
        // GenericStringRef::operator=, bundled by msgpack-tools and rttr); older compilers only checked instantiations.
        env["CXXFLAGS"] = "-O2 -pipe -Wno-template-body" + newerWarnings;
        env["CPPFLAGS"] = string.Join(" ", includeKegs.Append(paths.Prefix).Select(k => Path.Combine(k, "include")).Where(Directory.Exists).Select(d => "-I" + Posix(d)));
        env["LDFLAGS"] = string.Join(" ", kegs.Append(paths.Prefix).Select(k => Path.Combine(k, "lib")).Where(Directory.Exists).Select(d => "-L" + Posix(d)));
        env["PKG_CONFIG_PATH"] = string.Join(listSeparator, kegs.Append(paths.Prefix)
            .SelectMany(k => new[] { Path.Combine(k, "lib", "pkgconfig"), Path.Combine(k, "share", "pkgconfig") })
            .Where(Directory.Exists).Select(Posix));
        env["PKG_CONFIG_LIBDIR"] = Posix(Path.Combine(ucrt, "lib", "pkgconfig"));
        // aclocal (MSYS perl) splits on ':', so this list always uses /c/x paths.
        string Msys(string p) => p.Length > 1 && p[1] == ':' ? "/" + char.ToLowerInvariant(p[0]) + p[2..].Replace('\\', '/') : p.Replace('\\', '/');
        env["ACLOCAL_PATH"] = string.Join(":", kegs.Append(paths.Prefix).Select(k => Path.Combine(k, "share", "aclocal"))
            .Where(Directory.Exists).Select(Msys));
        // Not the tested formula's own keg: like upstream, its tests find it through HOMEBREW_PREFIX
        // (extra-cmake-modules asserts ECM_DIR is HOMEBREW_PREFIX/share/ECM/cmake).
        var testedKeg = tested == null ? null : Path.Combine(paths.Prefix, "opt", Paths.FormulaName(tested));
        env["CMAKE_PREFIX_PATH"] = string.Join(listSeparator, kegs
            .Where(k => testedKeg == null || !(k.Equals(testedKeg, StringComparison.OrdinalIgnoreCase)
                || k.StartsWith(testedKeg + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            .Append(paths.Prefix).Select(Posix));

        // Old config.guess scripts don't know MinGW or MSYS; tell configure what it is building on.
        var site = Path.Combine(buildPath, ".brew_config.site");
        var triple = posix ? "x86_64-pc-cygwin" : "x86_64-w64-mingw32";
        // Autoconf 2.5x configures (Thomas Dickey's mawk, vile, vttest ...) default the host to ac_cv_build_alias,
        // which only config.guess sets: with the build cached it stayed empty ("config.sub  failed").
        File.WriteAllText(site, "ac_cv_build=${ac_cv_build:-" + triple + "}\nac_cv_build_alias=${ac_cv_build_alias:-" + triple + "}\n");
        env["CONFIG_SITE"] = Posix(site);
        // CMake 4 refuses projects declaring cmake_minimum_required < 3.5 (rapidjson, termcolor); upstream formulae
        // that need it pass -DCMAKE_POLICY_VERSION_MINIMUM=3.5 one by one. A formula's own -D still wins.
        if (!env.ContainsKey("CMAKE_POLICY_VERSION_MINIMUM")) env["CMAKE_POLICY_VERSION_MINIMUM"] = "3.5";
        // Superenv's cc shim adds the prefix's and dependencies' include/lib dirs to every compiler call; GCC reads
        // the same lists from CPATH and LIBRARY_PATH, so plain Makefiles and `gcc test.c -lfoo` in tests find them.
        // Never set empty: GCC reads an empty list element as the current directory (build2 rejected the
        // relative "./x86_64-w64-mingw32/16.2.0" that `g++ -print-search-dirs` then listed).
        foreach (var (name, list) in new[]
                 {
                     ("CPATH", includeKegs.Append(paths.Prefix).Select(k => Path.Combine(k, "include"))),
                     ("LIBRARY_PATH", kegs.Append(paths.Prefix).Select(k => Path.Combine(k, "lib"))),
                 })
        {
            var value = string.Join(listSeparator, list.Where(Directory.Exists).Select(Posix));
            if (value.Length > 0) env[name] = value;
            else env.Remove(name);
        }
        // GNU make runs as many jobs as Homebrew's superenv allows (MAKEFLAGS=-j<make_jobs>); a formula's own -j or
        // ENV.deparallelize still wins. Single-job OpenSSL builds ran past the 45-minute limit.
        if (!env.ContainsKey("MAKEFLAGS") && Environment.GetEnvironmentVariable("MAKEFLAGS") is not { Length: > 0 })
            env["MAKEFLAGS"] = "-j" + (env.TryGetValue("HOMEBREW_MAKE_JOBS", out var jobs) ? jobs
                : Environment.GetEnvironmentVariable("HOMEBREW_MAKE_JOBS") is { Length: > 0 } set ? set : Environment.ProcessorCount.ToString());
        if ((direct ?? dependencies).Any(d => d is "rust" or "rustup"))
        {
            PointCcRsAtMsvc(env);
            PointOpensslSysAtOpenssl(paths, env, dependencies, buildPath);
        }
    }

    // Rust's toolchain targets x86_64-pc-windows-msvc, and cc-rs (the C build of *-sys crates) reads CC and AR before
    // looking for Visual Studio: MSYS2's gcc and `ar` then got lib.exe's "-out:" ("ar: invalid option -- :", turso's
    // libmimalloc-sys and zstd-sys). The target-specific variables, which cc-rs reads first, name MSVC's own tools.
    // openssl-sys never asks pkg-config on the MSVC target (it does on macOS and Linux, where superenv's
    // PKG_CONFIG_PATH finds the brewed OpenSSL): cryptography failed with "Could not find directory of OpenSSL
    // installation". The newest openssl dependency is OPENSSL_DIR, as cargo-c's formula sets it by hand.
    static void PointOpensslSysAtOpenssl(Paths paths, Dictionary<string, string> env, IEnumerable<string> dependencies, string buildPath)
    {
        if (env.ContainsKey("OPENSSL_DIR") || Environment.GetEnvironmentVariable("OPENSSL_DIR") is { Length: > 0 }) return;
        var openssl = dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(d => (Name: d, Match: System.Text.RegularExpressions.Regex.Match(d, @"^openssl(?:@(\d+(?:\.\d+)*))?$")))
            .Where(p => p.Match.Success)
            .OrderByDescending(p => Version.TryParse(p.Match.Groups[1].Success ? p.Match.Groups[1].Value + (p.Match.Groups[1].Value.Contains('.') ? "" : ".0") : "0.0", out var v) ? v : new Version())
            .Select(p => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(p.Name)))
            .FirstOrDefault(k => Directory.Exists(Path.Combine(k, "include", "openssl")));
        if (openssl == null) return;
        // On the MSVC target it links libssl.lib and libcrypto.lib, where a MinGW-built OpenSSL has libssl.dll.a and
        // libcrypto.dll.a ("does not contain the required files to either statically or dynamically link OpenSSL").
        // Both are COFF import libraries: copies under the MSVC names, in the build, serve link.exe.
        var lib = Path.Combine(openssl, "lib");
        if (!File.Exists(Path.Combine(lib, "libssl.lib")) && File.Exists(Path.Combine(lib, "libssl.dll.a")) && File.Exists(Path.Combine(lib, "libcrypto.dll.a")))
        {
            var shims = Path.Combine(buildPath, ".brew_openssl_lib");
            Directory.CreateDirectory(shims);
            foreach (var name in new[] { "libssl", "libcrypto" })
                File.Copy(Path.Combine(lib, name + ".dll.a"), Path.Combine(shims, name + ".lib"), true);
            env["OPENSSL_LIB_DIR"] = shims;
            env["OPENSSL_INCLUDE_DIR"] = Path.Combine(openssl, "include");
        }
        else env["OPENSSL_DIR"] = openssl;
    }

    static void PointCcRsAtMsvc(Dictionary<string, string> env)
    {
        if (MsvcX64 is not { } msvc || !msvc.TryGetValue("__MSVC_PATH", out var dirs)) return;
        string Tool(string exe) => dirs.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(d => Path.Combine(d.Trim('"'), exe)).FirstOrDefault(File.Exists);
        if (Tool("cl.exe") is null || Tool("lib.exe") is not { } lib) return;
        // cl by its plain name: build scripts that pick assembly by compiler name (blake3 in cargo-c accepts only
        // "", "cl" or "cl.exe") took a full path for GCC and fed cl its *_windows_gnu.S files (LNK1181). Visual
        // Studio's folders go last on PATH, so they shadow neither MSYS2's tools nor its link.exe (rustc finds
        // Visual Studio's linker itself).
        foreach (var (name, value) in new[] { ("CC", "cl.exe"), ("CXX", "cl.exe"), ("AR", lib) })
            if (!env.ContainsKey(name + "_x86_64_pc_windows_msvc")) env[name + "_x86_64_pc_windows_msvc"] = value;
        env["PATH"] = env.GetValueOrDefault("PATH", Environment.GetEnvironmentVariable("PATH") ?? "") + ";" + dirs;
        // A compiler named this way is run as is: cc-rs only sets up Visual Studio's environment for a cl it found
        // itself, so cl needs vcvars' header and library lists ("C1034: stdarg.h: no include path set" in ring,
        // aws-lc-sys, libsqlite3-sys). MinGW GCC and ld don't read these variables.
        foreach (var name in new[] { "INCLUDE", "LIB", "LIBPATH" })
            if (!env.ContainsKey(name) && msvc.TryGetValue(name, out var value)) env[name] = value;
        // cc-rs passes CFLAGS/CXXFLAGS to cl together with the target-specific ones (it reads them all, so an empty
        // CFLAGS_x86_64_pc_windows_msvc hides nothing), and GCC's -Wno-error=/-Wno-template-body are a fatal D8021
        // to cl (turso, cargo-c, ring in whodb-cli and nfd2nfc, pydantic-core). Rust builds keep what both accept.
        env["CFLAGS"] = MsvcSafeFlags(env.GetValueOrDefault("CFLAGS"));
        env["CXXFLAGS"] = MsvcSafeFlags(env.GetValueOrDefault("CXXFLAGS"));
    }

    // Drops the GCC-only flags (-W..., -std=, -pipe) that cl.exe rejects or warns about; keeps the rest (-O2).
    public static string MsvcSafeFlags(string flags) => string.Join(" ", (flags ?? "")
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Where(f => !f.StartsWith("-W", StringComparison.Ordinal) && !f.StartsWith("-std=", StringComparison.Ordinal) && f != "-pipe"));

    // Executables built with MinGW GCC may import its runtime and any UCRT64 libraries the build found.
    // Copy those DLLs next to each binary so the keg (and its bottle) runs without MSYS2.
    static bool IsProgramFolder(string directory) =>
        Path.GetFileName(directory).Equals("bin", StringComparison.OrdinalIgnoreCase)
        || Directory.EnumerateFiles(directory, "*.exe").Any();

    public static void BundleToolchainDlls(string keg, IEnumerable<string> dependencyKegs = null)
    {
        // Where an imported DLL can come from: the keg's dependencies (their bin and lib), then the
        // toolchain's runtime (UCRT64 for native builds, msys-*.dll for POSIX-runtime builds). Copies
        // keep every bottle self-contained; Windows doesn't search other kegs.
        var sources = (dependencyKegs ?? []).SelectMany(k => new[] { Path.Combine(k, "bin"), Path.Combine(k, "lib"), Path.Combine(k, "libexec", "ucrt64", "bin") })
            .Where(Directory.Exists).ToList();
        if (Msys2Root is { } root) sources.Add(Path.Combine(root, PosixRuntime ? "usr" : "ucrt64", "bin"));
        if (sources.Count == 0) return;
        foreach (var binary in Paths.TreeFiles(keg)
                     // Python extension modules too: Python 3.8+ doesn't load their DLLs from PATH (numpy's
                     // _multiarray_umath needs libopenblas and GCC's runtime: "DLL load failed while importing").
                     // Not plugin DLLs in folders without programs (gdk-pixbuf's loaders, GIO modules): their host's own
                     // folder supplies those, and copies beside them loaded GLib twice ("cannot register existing type"),
                     // which hung gdk-pixbuf-query-loaders.
                     .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".pyd", StringComparison.OrdinalIgnoreCase)
                                 || f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && IsProgramFolder(Path.GetDirectoryName(f)!)).ToList())
        {
            var directory = Path.GetDirectoryName(binary)!;
            var pending = new Queue<string>(PortableExecutable.Imports(binary));
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (pending.Count > 0)
            {
                var dll = pending.Dequeue();
                if (!seen.Add(dll)) continue;
                // Each binary's own folder needs the DLL: Windows looks beside the exe, not elsewhere in the keg.
                if (File.Exists(Path.Combine(directory, dll))) continue;
                var source = sources.Select(d => Path.Combine(d, dll)).FirstOrDefault(File.Exists);
                if (source == null) continue;
                File.Copy(source, Path.Combine(directory, dll));
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
        // A dangling link (libzzip's CMake links lib/libzzip.dll to a DLL it installed in bin/) imports nothing.
        catch (Exception e) when (e is BadImageFormatException or IOException or UnauthorizedAccessException) { return []; }
    }
}
