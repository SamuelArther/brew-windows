using Brew;
using Brew.Formulae;
using Brew.Ruby;

// Generic causes behind the glib/gobject-introspection, python@3.15 extension and sphinx-doc failures.
static class CoreChain
{
    public static int Run(string root)
    {
        var count = 0;
        var dir = Path.Combine(root, "core-chain");
        Directory.CreateDirectory(dir);
        void Check(bool ok, string label)
        {
            if (!ok) throw new Exception("FAIL " + label);
            count++;
        }

        // Superenv's homebrew_extra_paths: python@3.x dependencies' libexec/bin, newest first, after the
        // dependencies' bin and ahead of HOMEBREW_PREFIX/bin and the toolchain (meson found MSYS2's python3).
        var paths = new Paths();
        string Opt(string name, params string[] sub) => Path.Combine([paths.Prefix, "opt", name, .. sub]);
        Directory.CreateDirectory(Opt("python@3.9", "libexec", "bin"));
        Directory.CreateDirectory(Opt("python@3.14", "libexec", "bin"));
        Directory.CreateDirectory(Opt("cc-gettext", "bin"));
        Directory.CreateDirectory(Opt("cc-gettext", "libexec", "ucrt64", "bin"));
        try
        {
            Check(BuildEnvironment.PythonLibexecBins(paths, ["cc-gettext", "python@3.9", "python@3.14", "python@3.12"])
                    .SequenceEqual([Opt("python@3.14", "libexec", "bin"), Opt("python@3.9", "libexec", "bin")]),
                "python dependencies' libexec/bin, newest first, only those installed");
            Directory.CreateDirectory(Path.Combine(dir, "build"));
            var context = new InstallContext(Path.Combine(dir, "keg"), Path.Combine(dir, "build"), null, paths.Prefix);
            BuildEnvironment.Apply(paths, context, ["cc-gettext", "python@3.14"]);
            var pathList = context.BuildEnvironment["PATH"].Split(';').ToList();
            int At(string p) => pathList.FindIndex(x => x.Equals(p, StringComparison.OrdinalIgnoreCase));
            Check(At(Opt("cc-gettext", "bin")) >= 0 && At(Opt("python@3.14", "libexec", "bin")) > At(Opt("cc-gettext", "bin"))
                  && At(paths.Bin) > At(Opt("python@3.14", "libexec", "bin")), "python's libexec/bin follows the dependencies' bin and precedes the prefix's: " + context.BuildEnvironment["PATH"]);

            // Python 3.8+ extension modules don't load DLLs from PATH; GLib's tools read GI_EXTRA_BASE_DLL_DIRS.
            var dlls = context.BuildEnvironment.GetValueOrDefault("GI_EXTRA_BASE_DLL_DIRS") ?? "";
            Check(dlls.Split(';').Contains(Opt("cc-gettext", "libexec", "ucrt64", "bin"), StringComparer.OrdinalIgnoreCase)
                  && dlls.Split(';').All(Directory.Exists), "GI_EXTRA_BASE_DLL_DIRS lists the dependencies' existing DLL folders: " + dlls);
            var test = new InstallContext(Path.Combine(dir, "keg"), Path.Combine(dir, "build"), null, paths.Prefix);
            BuildEnvironment.ApplyForTest(paths, test, ["cc-gettext", "python@3.14"]);
            Check(test.BuildEnvironment["PATH"].Split(';').Contains(Opt("python@3.14", "libexec", "bin"), StringComparer.OrdinalIgnoreCase)
                  && test.BuildEnvironment.ContainsKey("GI_EXTRA_BASE_DLL_DIRS"), "the test environment gets both too");
            Check(test.BuildEnvironment.GetValueOrDefault("PYGI_DLL_PATH") == test.BuildEnvironment["GI_EXTRA_BASE_DLL_DIRS"], "PyGObject's PYGI_DLL_PATH lists the same folders");
        }
        finally
        {
            foreach (var name in new[] { "python@3.9", "python@3.14", "cc-gettext" }) Directory.Delete(Opt(name), true);
        }

        // A test's CMAKE_PREFIX_PATH leaves out the tested keg itself: upstream's tests find it through HOMEBREW_PREFIX.
        var savedMsys = Environment.GetEnvironmentVariable("HOMEBREW_MSYS2_ROOT");
        var msys = Path.Combine(dir, "msys64");
        Directory.CreateDirectory(Path.Combine(msys, "usr", "bin"));
        File.WriteAllText(Path.Combine(msys, "usr", "bin", "bash.exe"), "");
        Directory.CreateDirectory(Opt("cc-ecm", "share"));
        Directory.CreateDirectory(Opt("cc-dep", "lib"));
        try
        {
            Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", msys);
            var test = new InstallContext(Path.Combine(dir, "keg3"), Path.Combine(dir, "build"), null, paths.Prefix);
            BuildEnvironment.ApplyForTest(paths, test, ["cc-dep", "cc-ecm"], "cc-ecm");
            var cmake = test.BuildEnvironment["CMAKE_PREFIX_PATH"].Replace('\\', '/');
            Check(!cmake.Contains("/opt/cc-ecm") && cmake.Contains("/opt/cc-dep") && cmake.Contains(paths.Prefix.Replace('\\', '/')),
                "the tested formula isn't in its own test's CMAKE_PREFIX_PATH: " + cmake);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", savedMsys);
            Directory.Delete(Opt("cc-ecm"), true);
            Directory.Delete(Opt("cc-dep"), true);
        }

        // Python extension modules get their non-system DLLs beside them, as executables do (numpy's
        // _multiarray_umath.pyd and libopenblas). Any PE works as the module: cmd.exe's first import stands in.
        var sample = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        if (PortableExecutable.Imports(sample).FirstOrDefault() is { } imported)
        {
            var keg = Path.Combine(dir, "pyd-keg", "lib", "python3.14", "site-packages", "pkg");
            var depBin = Path.Combine(dir, "pyd-dep", "bin");
            Directory.CreateDirectory(keg);
            Directory.CreateDirectory(depBin);
            File.Copy(sample, Path.Combine(keg, "_mod.cp314-win_amd64.pyd"), true);
            File.WriteAllText(Path.Combine(depBin, imported), "not a PE");
            BuildEnvironment.BundleToolchainDlls(Path.Combine(dir, "pyd-keg"), [Path.Combine(dir, "pyd-dep")]);
            Check(File.Exists(Path.Combine(keg, imported)), "a .pyd's imported DLL is copied beside it: " + imported);

            // Utils.binary_linked_to_library? (cargo-c's test) reads the import table; versioned DLL names match.
            var it = FormulaLoader.NewInterpreterForTests();
            it.Cwd = dir;
            var stem = Path.GetFileNameWithoutExtension(imported);
            var probe = $"""require "utils/linkage"; [Utils.binary_linked_to_library?("{sample.Replace('\\', '/')}", "C:/x/lib/{imported}"), Utils.binary_linked_to_library?("{sample.Replace('\\', '/')}", "C:/x/lib/notimported.dll")].inspect""";
            Check(it.ToS(it.Run(probe, Path.Combine(dir, "probe.rb"))) == "[true, false]", "Utils.binary_linked_to_library? follows the import table");
            Check(FormulaLoader.BinaryLinkedToLibrary(sample, "C:/x/" + stem.ToLowerInvariant() + ".dll") && !FormulaLoader.BinaryLinkedToLibrary(sample, "C:/x/" + stem + "-other-name-xyz.dll"),
                "import names compare case-insensitively");
        }

        // Visual Studio builds of Python extensions get the dependencies' headers and libraries (pygit2: git2.h,
        // git2.lib), with MinGW's libfoo.dll.a offered as foo.lib.
        {
            var git2 = Path.Combine(dir, "msvc-git2");
            Directory.CreateDirectory(Path.Combine(git2, "include"));
            Directory.CreateDirectory(Path.Combine(git2, "lib"));
            File.WriteAllText(Path.Combine(git2, "lib", "libgit2.dll.a"), "!<arch>\n");
            Directory.CreateDirectory(Path.Combine(dir, "msvc-home"));
            var it = FormulaLoader.NewInterpreterForTests();
            it.Env["CPATH"] = Path.Combine(git2, "include").Replace('\\', '/');
            it.Env["LIBRARY_PATH"] = Path.Combine(git2, "lib").Replace('\\', '/');
            it.Env["INCLUDE"] = @"C:\VS\include";
            it.Env["LIB"] = Path.Combine(dir, "msvc-home");
            it.Env["HOME"] = Path.Combine(dir, "msvc-home");
            FormulaLoader.AddDependenciesForMsvc(it);
            Check(it.Env["INCLUDE"].Split(';').Contains(Path.Combine(git2, "include"), StringComparer.OrdinalIgnoreCase), "MSVC INCLUDE gets the dependencies' include folders: " + it.Env["INCLUDE"]);
            var shim = Path.Combine(dir, "msvc-home", ".brew_msvc_lib");
            Check(File.Exists(Path.Combine(shim, "git2.lib")) && it.Env["LIB"].Split(';').Contains(shim, StringComparer.OrdinalIgnoreCase)
                  && it.Env["LIB"].Split(';')[0] == Path.Combine(dir, "msvc-home"), "MSVC LIB keeps Visual Studio's first and finds libgit2.dll.a as git2.lib: " + it.Env["LIB"]);
        }

        // python@3.15's libexec/bin/python is a link to libexec/tools/python.exe, which imports python315.dll from
        // the link's own folder: the formula links the DLLs too.
        var python315 = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../../../tap/homebrew-core/Formula/p/python@3.15.rb"));
        Check(python315.Contains("""(libexec/"tools").glob("*.dll").each { |dll| (libexec/"bin").install_symlink dll }"""), "python@3.15 links its DLLs beside libexec/bin/python");

        // Windows-only requirements upstream's resources never list (colorama for sphinx): only marker-gated
        // requirements that hold on Windows and aren't installed. Needs a Python with pip to run.
        if (DevCommands.Which("python") is { } python && !python.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
        {
            var venv = Path.Combine(dir, "venv");
            var made = DevCommands.Capture(python, ["-m", "venv", venv]);
            var venvPython = Path.Combine(venv, "Scripts", "python.exe");
            if (made.code == 0 && File.Exists(venvPython))
            {
                var site = Path.Combine(venv, "Lib", "site-packages");
                void Dist(string name, params string[] requires)
                {
                    var info = Path.Combine(site, name + "-1.0.dist-info");
                    Directory.CreateDirectory(info);
                    File.WriteAllText(Path.Combine(info, "METADATA"), $"Metadata-Version: 2.1\nName: {name}\nVersion: 1.0\n" + string.Concat(requires.Select(r => $"Requires-Dist: {r}\n")));
                }
                Dist("cc-tool", "colorama>=0.4.6; sys_platform == \"win32\"", "cc-linux-only; sys_platform == \"linux\"", "cc-plain",
                    "cc-extra; sys_platform == \"win32\" and extra == \"docs\"", "pywin32-ctypes; os_name == 'nt'", "cc-dep; platform_system == \"Windows\"");
                Dist("cc-dep");
                var result = DevCommands.Capture(venvPython, ["-c", FormulaLoader.WindowsRequirementsScript]);
                Check(result.code == 0 && result.stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).SequenceEqual(["colorama", "pywin32-ctypes"]),
                    "only missing Windows-marker requirements are reported: " + result.stdout + result.stderr);
            }
        }
        return count;
    }
}
