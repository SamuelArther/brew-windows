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
        }
        finally
        {
            foreach (var name in new[] { "python@3.9", "python@3.14", "cc-gettext" }) Directory.Delete(Opt(name), true);
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
