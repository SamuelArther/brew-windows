using Brew.Formulae;

namespace Brew.Ruby;

// Install-time resources (resource/resources/stage) and Homebrew's Language::Python helpers.
// Brewed Python here is MSYS2's UCRT64 build: venvs use bin/ like upstream, and pip builds
// from source (--no-binary=:all:) exactly as Homebrew does.
public static partial class FormulaLoader
{
    static void DefinePythonDsl(Interp it, RClass F)
    {
        string Prefix(Interp i, object self) => Ctx(i, self).Prefix;
        object Kw(Interp i, object[] a, string key) =>
            a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Interp.Sym(key), out var v) ? v : null;

        // ---- resources ----
        var R = it.DefineClass("FormulaResource");
        (FormulaResource Spec, InstallContext Ctx) Res(object o) => ((FormulaResource, InstallContext))((RObject)o).Host;
        RObject NewResource(Interp i, FormulaResource r, InstallContext ctx) => new() { Class = R, Host = (r, ctx) };
        string Staged(Interp i, object o)
        {
            var (spec, ctx) = Res(o);
            return ctx.Resources.TryGetValue(spec.Name, out var dir) ? dir
                : throw i.Error(i.RuntimeError, $"resource {spec.Name} was not staged");
        }
        it.Def(R, "name", (i, s, a, b) => Interp.S(Res(s).Spec.Name));
        it.Def(R, "url", (i, s, a, b) => Interp.S(Res(s).Spec.Url));
        it.Def(R, "checksum", (i, s, a, b) => Interp.S(Res(s).Spec.Sha256));
        it.Def(R, "version", (i, s, a, b) => VersionObject(i, Versions.Parse(Res(s).Spec.Url) ?? ""));
        it.Def(R, "test?", (i, s, a, b) => false);
        it.Def(R, "inspect to_s", (i, s, a, b) => Interp.S($"#<Resource: {Res(s).Spec.Name}>"));
        it.Def(R, "cached_download", (i, s, a, b) =>
        {
            var dir = Staged(i, s);
            var files = Directory.GetFiles(dir);
            return i.NewPath(files.Length == 1 && Directory.GetDirectories(dir).Length == 0 ? files[0] : dir);
        });
        // stage(target = nil) { ... }: copy the staged tree into target, or run the block inside it.
        it.Def(R, "stage", (i, s, a, b) =>
        {
            var source = Staged(i, s);
            var dir = source;
            if (a.Length > 0 && a[0] != null)
            {
                dir = i.Abs(i.PathStr(a[0]));
                CopyDirectory(source, dir);
            }
            if (b == null) return i.NewPath(dir);
            return i.Invoke(i.DirClass, "chdir", [Interp.S(dir)], b);
        });

        it.Def(F, "resource", (i, self, a, b) =>
        {
            var ctx = Ctx(i, self);
            var name = i.ToS(a[0]);
            var spec = SpecOf(((RObject)self).Class).Resources.FirstOrDefault(r => r.Name == name)
                ?? throw i.Error(i.RuntimeError, $"No resource named \"{name}\"");
            return NewResource(i, spec, ctx);
        });
        it.Def(F, "resources", (i, self, a, b) =>
            new RArray(SpecOf(((RObject)self).Class).Resources.Select(r => (object)NewResource(i, r, Ctx(i, self)))));

        // ---- Language::Python ----
        var language = it.DefineModule("Language");
        var python = it.DefineModule("Python", language);
        var venvMixin = it.DefineModule("Virtualenv", python);
        string MajorMinor(Interp i, object py)
        {
            var output = Capture(i, [Exe(i, i.ToS(py)), "-c", "import sys;print(f'{sys.version_info[0]}.{sys.version_info[1]}')"], []);
            return output.Trim();
        }
        it.DefS(python, "major_minor_version", (i, s, a, b) => VersionObject(i, MajorMinor(i, a[0])));
        it.DefS(python, "site_packages", (i, s, a, b) => Interp.S($"lib/python{MajorMinor(i, a.Length > 0 ? a[0] : Interp.S("python3"))}/site-packages"));

        it.Def(F, "std_pip_args", (i, self, a, b) =>
        {
            var args = new List<string> { "--verbose", "--no-deps", "--no-binary=:all:", "--ignore-installed", "--no-compile", "--uploaded-prior-to=P1D" };
            var prefix = Kw(i, a, "prefix");
            // Brewed Python installs --prefix trees in upstream's layout during builds (see homebrew.pth).
            if (prefix is not false) args.Add("--prefix=" + (prefix == null ? Prefix(i, self) : i.PathStr(prefix)));
            if (!Interp.Truthy(Kw(i, a, "build_isolation"))) args.Add("--no-build-isolation");
            return StrArray(args.ToArray());
        });

        var V = it.DefineClass("Virtualenv", it.ObjectClass, venvMixin);

        it.Def(venvMixin, "python_names", (i, self, a, b) =>
            new RArray(SpecOf(((RObject)self).Class).Dependencies.Select(d => d.Name)
                .Where(n => System.Text.RegularExpressions.Regex.IsMatch(n, @"^python(@\d+\.\d+)?$")).Select(n => (object)Interp.S(n))));

        it.Def(venvMixin, "virtualenv_create", (i, self, a, b) =>
        {
            var root = i.Abs(i.PathStr(a[0]));
            var py = a.Length > 1 && a[1] is not RHash ? i.ToS(a[1]) : "python";
            var systemSite = Kw(i, a, "system_site_packages") is not false;
            return CreateVenv(i, self, root, py, systemSite);
        });

        it.Def(venvMixin, "virtualenv_install_with_resources", (i, self, a, b) =>
        {
            var spec = SpecOf(((RObject)self).Class);
            var py = Kw(i, a, "using") is { } u ? i.ToS(u)
                : spec.Dependencies.Select(d => d.Name).Where(n => n.StartsWith("python", StringComparison.Ordinal)).ToList() switch
                {
                    [var only] => only == "python" ? "python3" : only,
                    [] => throw i.Error(i.RuntimeError, $"{spec.Name}: no python dependency for virtualenv_install_with_resources"),
                    _ => throw i.Error(i.RuntimeError, $"{spec.Name}: ambiguous python dependency; pass using:"),
                };
            var resources = spec.Resources.ToList();
            List<FormulaResource> Slice(object names)
            {
                var wanted = names == null ? [] : Strings(i, names).ToList();
                var taken = resources.Where(r => wanted.Contains(r.Name)).ToList();
                resources.RemoveAll(taken.Contains);
                return taken;
            }
            Slice(Kw(i, a, "without"));
            var start = Slice(Kw(i, a, "start_with"));
            var end = Slice(Kw(i, a, "end_with"));
            var ordered = start.Concat(resources).Concat(end).ToList();

            var venv = CreateVenv(i, self, Path.Combine(Prefix(i, self), "libexec"), py.Replace("@", ""), Kw(i, a, "system_site_packages") is not false);
            var ctx = Ctx(i, self);
            foreach (var r in ordered)
            {
                var dir = ctx.Resources[r.Name];
                var wheel = Directory.GetFiles(dir, "*.whl").FirstOrDefault();
                PipInstall(i, venv, [wheel ?? dir], buildIsolation: true);
            }
            PipInstallAndLink(i, self, venv, [ctx.BuildPath], Kw(i, a, "link_manpages") is not false);
            return VenvObject(i, venv);
        });

        // Venv objects returned by virtualenv_create.
        it.Def(V, "root", (i, s, a, b) => i.NewPath((string)((RObject)s).Host));
        it.Def(V, "site_packages", (i, s, a, b) => i.NewPath(VenvSitePackages(i, (string)((RObject)s).Host)));
        it.Def(V, "pip_install", (i, s, a, b) =>
        {
            PipInstall(i, (string)((RObject)s).Host, PipTargets(i, a[0]), Kw(i, a, "build_isolation") is not false);
            return null;
        });
        it.Def(V, "pip_install_and_link", (i, s, a, b) =>
        {
            var formula = i.Globals.GetValueOrDefault("$__formula") ?? throw i.Error(i.RuntimeError, "pip_install_and_link outside install");
            PipInstallAndLink(i, formula, (string)((RObject)s).Host, PipTargets(i, a[0]), Kw(i, a, "link_manpages") is not false);
            return null;
        });

        string CreateVenv(Interp i, object self, string root, string py, bool systemSite)
        {
            i.Globals["$__formula"] = self;
            if (File.Exists(Path.Combine(VenvBin(root), "python.exe"))) return root;
            var args = new List<string> { BasePython(i, py), "-m", "venv" };
            if (systemSite) args.Add("--system-site-packages");
            args.Add(root);
            Capture(i, args, []);
            // Homebrew removes the activation scripts; keep pip (the venv's own pip drives installs).
            foreach (var f in Directory.GetFiles(VenvBin(root)).Where(f => Path.GetFileName(f).StartsWith("activate", StringComparison.OrdinalIgnoreCase)
                         || Path.GetFileName(f).Equals("deactivate.bat", StringComparison.OrdinalIgnoreCase)))
                File.Delete(f);
            // CPython venvs use Scripts\ and Lib\site-packages; upstream formulae use bin/ and
            // lib/pythonX.Y/site-packages. Relative links make both spellings work (and survive bottling).
            var scripts = Path.Combine(root, "Scripts");
            if (Directory.Exists(scripts) && !Directory.Exists(Path.Combine(root, "bin")))
            {
                Directory.CreateSymbolicLink(Path.Combine(root, "bin"), "Scripts");
                var version = Capture(i, [Path.Combine(scripts, "python.exe"), "-c", "import sys;print(f'{sys.version_info[0]}.{sys.version_info[1]}')"], []).Trim();
                var versioned = Path.Combine(root, "Lib", "python" + version);
                Directory.CreateDirectory(versioned);
                Directory.CreateSymbolicLink(Path.Combine(versioned, "site-packages"), Path.Combine("..", "site-packages"));
            }
            return root;
        }

        RObject VenvObject(Interp i, string root) => new() { Class = V, Host = root };
    }

    static List<string> PipTargets(Interp i, object targets) => targets switch
    {
        RArray arr => arr.L.Select(t => PipTarget(i, t)).ToList(),
        _ => [PipTarget(i, targets)],
    };

    static string PipTarget(Interp i, object t)
    {
        if (t is RObject { Host: (FormulaResource spec, InstallContext ctx) })
        {
            var dir = ctx.Resources[spec.Name];
            return Directory.GetFiles(dir, "*.whl").FirstOrDefault() ?? dir;
        }
        return t is RObject ? i.Abs(i.PathStr(t)) : i.ToS(t);
    }

    static void PipInstall(Interp i, string venv, List<string> targets, bool buildIsolation)
    {
        var args = new List<string> { Path.Combine(VenvBin(venv), "python.exe"), "-m", "pip", "install",
            "--verbose", "--no-deps", "--no-binary=:all:", "--ignore-installed", "--no-compile", "--uploaded-prior-to=P1D" };
        if (!buildIsolation) args.Add("--no-build-isolation");
        args.AddRange(targets);
        Ohai(i, string.Join(" ", args));
        // Like Formula#system: output streams to the build log; failure is a BuildError.
        if (!Interp.Truthy(i.RunSystem(args.Select(a => (object)Interp.S(a)).ToArray())))
            throw i.Error(BuildError(i), $"Failed executing: {string.Join(" ", args)}");
    }

    static void PipInstallAndLink(Interp i, object formula, string venv, List<string> targets, bool linkManpages)
    {
        var bin = VenvBin(venv);
        var man = Path.Combine(venv, "share", "man");
        var binBefore = Directory.GetFiles(bin).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var manBefore = Directory.Exists(man) ? Directory.GetFiles(man, "*", SearchOption.AllDirectories).ToHashSet() : [];
        PipInstall(i, venv, targets, buildIsolation: true);
        var prefix = Ctx(i, formula).Prefix;
        foreach (var script in Directory.GetFiles(bin).Where(f => !binBefore.Contains(f)))
        {
            // Entry points: launchers (.exe) become commands; the -script.py/.pya companions stay in the venv.
            if (!script.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;
            Ctx(i, formula).WindowsExe(Path.GetRelativePath(prefix, script), Path.GetFileNameWithoutExtension(script));
        }
        if (!linkManpages || !Directory.Exists(man)) return;
        foreach (var page in Directory.GetFiles(man, "*", SearchOption.AllDirectories).Where(f => !manBefore.Contains(f)))
        {
            var target = Path.Combine(prefix, "share", "man", Path.GetFileName(Path.GetDirectoryName(page))!, Path.GetFileName(page));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.CreateSymbolicLink(target, page);
        }
    }

    static string VenvBin(string venv) =>
        Directory.Exists(Path.Combine(venv, "Scripts")) ? Path.Combine(venv, "Scripts") : Path.Combine(venv, "bin");

    static string VenvSitePackages(Interp i, string venv) =>
        Directory.Exists(Path.Combine(venv, "Lib", "site-packages")) ? Path.Combine(venv, "Lib", "site-packages")
            : Directory.GetDirectories(Path.Combine(venv, "lib"), "python*").Select(d => Path.Combine(d, "site-packages")).First(Directory.Exists);

    // "python3.14" / "python@3.14" -> the brewed interpreter's real executable, via the stable opt path.
    static string BasePython(Interp i, string py)
    {
        var version = System.Text.RegularExpressions.Regex.Match(py, @"(\d+\.\d+)").Groups[1].Value;
        var prefix = i.Env.GetValueOrDefault("HOMEBREW_PREFIX") ?? throw i.Error(i.RuntimeError, "HOMEBREW_PREFIX is not set");
        var formula = version.Length > 0 ? "python@" + version : Directory.GetDirectories(Path.Combine(prefix, "opt"), "python@3.*").Select(Path.GetFileName).Max();
        var opt = Path.Combine(prefix, "opt", formula);
        var candidates = (version.Length > 0 ? new[] { "python" + version } : []).Concat(["python3", "python"])
            .Select(n => Path.Combine(opt, "bin", n + ".exe")).ToList();
        var command = candidates.FirstOrDefault(c => File.Exists(c + ".brew-shim")) ?? candidates.FirstOrDefault(File.Exists)
            ?? throw i.Error(i.RuntimeError, $"{formula} is not installed");
        var sidecar = command + ".brew-shim";
        if (!File.Exists(sidecar)) return command;
        var real = File.ReadAllText(sidecar).Split('\n')[0].Trim();
        var keg = Path.Combine(prefix, "Cellar", formula);
        // Point the venv at opt/, not the versioned keg, so it survives Python patch upgrades.
        var relative = Path.GetRelativePath(keg, real);
        var parts = relative.Split(Path.DirectorySeparatorChar, 2);
        return parts.Length == 2 && !relative.StartsWith("..") ? Path.Combine(opt, parts[1]) : real;
    }

    static string Exe(Interp i, string program) => i.ResolveProgram(program);

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source)) CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }
}
