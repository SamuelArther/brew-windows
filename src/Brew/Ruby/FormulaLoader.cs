using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Brew.Formulae;

namespace Brew.Ruby;

// Evaluates a formula .rb file with the mini-Ruby interpreter. See docs/FORMULA_API.md.
public static class FormulaLoader
{
    sealed class Spec
    {
        public string Desc, Homepage, Url, Sha256, Version, License;
        public readonly List<string> Dependencies = new();
        public readonly List<FormulaResource> Resources = new();
    }

    public static FormulaDefinition Load(string path) => OnBigStack(() => LoadCore(Path.GetFullPath(path)));

    // Formula code can recurse deeply through the interpreter; give it room.
    static T OnBigStack<T>(Func<T> fn)
    {
        T result = default;
        ExceptionDispatchInfo error = null;
        var t = new Thread(() =>
        {
            try { result = fn(); }
            catch (Exception e) { error = ExceptionDispatchInfo.Capture(e); }
        }, 256 * 1024 * 1024);
        t.Start();
        t.Join();
        error?.Throw();
        return result;
    }

    // Homebrew's Formulary.class_s
    public static string ClassName(string name)
    {
        var s = name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..].ToLowerInvariant();
        s = Regex.Replace(s, @"[-_.\s][a-zA-Z0-9]", m => char.ToUpperInvariant(m.Value[^1]).ToString());
        s = s.Replace('+', 'x');
        return new Regex(@"(.)@(\d)").Replace(s, "$1AT$2", 1);
    }

    static FormulaDefinition LoadCore(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var it = new Interp();
        var formulaClass = DefineFormula(it);
        try { it.Run(File.ReadAllText(path), path); }
        catch (RubySyntaxError e) { throw new InvalidOperationException($"{name}: {path}: {e.Message}", e); }
        catch (RubyError e) { throw new InvalidOperationException($"{name}: {Describe(it, e)}", e); }

        var className = ClassName(name);
        if (it.ObjectClass.Consts.GetValueOrDefault(className) is not RClass cls || !it.Ancestors(cls).Contains(formulaClass))
        {
            var classes = it.ObjectClass.Consts.Values.OfType<RClass>().Where(c => c.Host is Spec || c.Super == formulaClass).ToList();
            var found = classes.Count == 0 ? "found no classes" : "only found: " + string.Join(", ", classes.Select(c => c.Name.Split("::")[^1]));
            throw new InvalidOperationException($"No available formula with the name \"{name}\".\nIn formula file: {path}\nExpected to find class {className}, but {found}.");
        }

        var spec = SpecOf(cls);
        var version = spec.Version ?? ParseVersion(spec.Url);
        if (spec.Url == null) throw new InvalidOperationException($"{name}: formula requires at least a URL");
        if (version == null) throw new InvalidOperationException($"{name}: version could not be detected from URL: {spec.Url}");
        if (spec.Sha256 == null) throw new InvalidOperationException($"{name}: formula requires a sha256");

        return new FormulaDefinition
        {
            Name = name,
            Version = version,
            Url = spec.Url,
            Sha256 = spec.Sha256,
            Description = spec.Desc,
            Homepage = spec.Homepage,
            License = spec.License,
            Dependencies = spec.Dependencies.ToList(),
            Resources = spec.Resources.ToList(),
            Install = ctx => OnBigStack(() => { RunInstall(it, cls, name, version, ctx); return 0; }),
        };
    }

    static string Describe(Interp it, RubyError e) =>
        e.ClassName is "RuntimeError" ? e.Message : $"{e.Message} ({e.ClassName})";

    static void RunInstall(Interp it, RClass cls, string name, string version, InstallContext ctx)
    {
        var f = new RObject { Class = cls, Host = ctx };
        f.SetIvar("@name", Interp.S(name));
        f.SetIvar("@version", Interp.S(version));
        it.Cwd = ctx.BuildPath;
        if (ctx.BrewPrefix != null)
        {
            it.ObjectClass.Consts["HOMEBREW_PREFIX"] = it.NewPath(ctx.BrewPrefix);
            it.ObjectClass.Consts["HOMEBREW_CELLAR"] = it.NewPath(Path.Combine(ctx.BrewPrefix, "Cellar"));
        }
        var buildRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ctx.BuildPath)) + Path.DirectorySeparatorChar;
        it.InstallHook = (dir, src, newName) =>
        {
            if (!string.Equals(Path.TrimEndingDirectorySeparator(dir), Path.TrimEndingDirectorySeparator(Path.GetFullPath(ctx.Bin)), StringComparison.OrdinalIgnoreCase)
                || !src.StartsWith(buildRoot, StringComparison.OrdinalIgnoreCase) || Directory.Exists(src))
                return false;
            ctx.InstallBin(Path.GetRelativePath(ctx.BuildPath, src), newName);
            return true;
        };
        try { it.Invoke(f, "install", Array.Empty<object>(), null); }
        catch (RubyError e) { throw new InvalidOperationException($"{name}: {Describe(it, e)}", e); }
    }

    static Spec SpecOf(RClass c) => (Spec)(c.Host ??= new Spec());

    static InstallContext Ctx(Interp it, object self) =>
        (self as RObject)?.Host as InstallContext ?? throw it.Error(it.RuntimeError, "this method is only available during install");

    static RClass DefineFormula(Interp it)
    {
        var F = it.DefineClass("Formula");
        var os = it.DefineModule("OS");
        it.DefS(os, "mac? linux?", (i, s, a, b) => false);
        it.DefS(os, "windows?", (i, s, a, b) => true);
        it.DefS(os, "kernel_name", (i, s, a, b) => Interp.S("Windows"));
        var hw = it.DefineModule("Hardware");
        var cpu = it.DefineModule("CPU", hw);
        bool arm = RuntimeInformation.OSArchitecture == Architecture.Arm64;
        it.DefS(cpu, "arm?", (i, s, a, b) => arm);
        it.DefS(cpu, "intel?", (i, s, a, b) => !arm);
        it.DefS(cpu, "arch", (i, s, a, b) => Interp.Sym(arm ? "arm64" : "x86_64"));
        it.DefS(cpu, "cores", (i, s, a, b) => (long)Environment.ProcessorCount);
        it.DefS(cpu, "is_64_bit?", (i, s, a, b) => true);
        var utils = it.DefineModule("Utils");
        it.DefS(utils, "safe_popen_read popen_read", (i, s, a, b) => i.Backtick(string.Join(" ", a.Select(i.ToS).Select(Quote))));

        // ---- class-level DSL ----
        void Field(string names, Action<Spec, string> set) =>
            it.DefS(F, names, (i, self, a, b) =>
            {
                i.Arity(a, 1, 2);
                set(SpecOf((RClass)self), i.ToS(a[0]));
                return null;
            });
        Field("desc", (s, v) => s.Desc = v);
        Field("homepage", (s, v) => s.Homepage = v);
        Field("url", (s, v) => s.Url = v);
        Field("version", (s, v) => s.Version = v);
        Field("sha256", (s, v) => s.Sha256 = v.ToLowerInvariant());
        it.DefS(F, "license", (i, self, a, b) => { SpecOf((RClass)self).License = LicenseString(i, a[0]); return null; });
        it.DefS(F, "depends_on", (i, self, a, b) =>
        {
            var spec = SpecOf((RClass)self);
            foreach (var arg in a)
            {
                if (arg is RString s) spec.Dependencies.Add(s.V);
                else if (arg is RHash h)
                    foreach (var kv in h.D)
                        if (kv.Key is RString dep && !IsTag(i, kv.Value, "test", "optional")) spec.Dependencies.Add(dep.V);
            }
            return null;
        });
        it.DefS(F, "resource", (i, self, a, b) =>
        {
            var r = new Spec();
            var holder = new RClass { Name = "Resource", Class = i.ClassClass, Super = i.ObjectClass, Host = r };
            i.SingletonOf(holder).Super = i.SingletonOf(F);
            if (b != null) i.CallProc(b, Array.Empty<object>(), null, holder, i.SingletonOf(holder));
            SpecOf((RClass)self).Resources.Add(new FormulaResource(i.ToS(a[0]), r.Url, r.Sha256));
            return null;
        });
        it.DefS(F, "stable", (i, self, a, b) => b == null ? null : i.CallProc(b, Array.Empty<object>(), null, self, i.SingletonOf((RClass)self)));
        // Only the Windows (and matching CPU) branches run.
        it.DefS(F, "on_windows", (i, self, a, b) => b == null ? null : i.CallProc(b, Array.Empty<object>(), null, self, i.SingletonOf((RClass)self)));
        it.DefS(F, arm ? "on_arm" : "on_intel", (i, self, a, b) => b == null ? null : i.CallProc(b, Array.Empty<object>(), null, self, i.SingletonOf((RClass)self)));
        it.DefS(F, (arm ? "on_intel" : "on_arm") + " on_macos on_linux on_system on_sequoia on_sonoma on_ventura on_monterey on_big_sur",
            (i, self, a, b) => null);
        it.DefS(F, "head bottle livecheck test mirror revision version_scheme option deprecated_option uses_from_macos conflicts_with " +
                   "keg_only deprecate! disable! patch skip_clean cxxstdlib_check link_overwrite service fails_with needs pour_bottle? " +
                   "compatibility_version no_autobump! pypi_packages env",
            (i, self, a, b) => null);

        // ---- instance methods (install time) ----
        string Prefix(Interp i, object self) => Ctx(i, self).Prefix;
        string Nm(object self) => ((RObject)self).GetIvar("@name").ToString();
        RObject Under(Interp i, object self, string rel) => i.NewPath(Path.Combine(Prefix(i, self), rel));
        it.Def(F, "name full_name", (i, self, a, b) => ((RObject)self).GetIvar("@name"));
        it.Def(F, "version", (i, self, a, b) => ((RObject)self).GetIvar("@version"));
        it.Def(F, "prefix", (i, self, a, b) => i.NewPath(Prefix(i, self)));
        foreach (var (method, rel) in new[]
                 {
                     ("bin", "bin"), ("sbin", "sbin"), ("lib", "lib"), ("libexec", "libexec"), ("include", "include"),
                     ("share", "share"), ("frameworks", "Frameworks"), ("man", "share/man"), ("info", "share/info"),
                     ("elisp", "share/emacs/site-lisp"), ("fish_completion", "share/fish/vendor_completions.d"),
                     ("bash_completion", "etc/bash_completion.d"), ("zsh_completion", "share/zsh/site-functions"),
                 })
        {
            var r = rel;
            it.Def(F, method, (i, self, a, b) => Under(i, self, r));
        }
        for (int n = 1; n <= 8; n++)
        {
            var r = "share/man/man" + n;
            it.Def(F, "man" + n, (i, self, a, b) => Under(i, self, r));
        }
        it.Def(F, "pkgshare", (i, self, a, b) => Under(i, self, "share/" + Nm(self)));
        it.Def(F, "doc", (i, self, a, b) => Under(i, self, "share/doc/" + Nm(self)));
        it.Def(F, "etc", (i, self, a, b) => Under(i, self, "etc"));
        it.Def(F, "var", (i, self, a, b) => Under(i, self, "var"));
        it.Def(F, "buildpath", (i, self, a, b) => i.NewPath(Ctx(i, self).BuildPath));
        it.Def(F, "opt_prefix", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self))));
        it.Def(F, "opt_bin", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), "bin")));
        it.Def(F, "opt_lib", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), "lib")));
        it.Def(F, "opt_libexec", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), "libexec")));
        it.Def(F, "caveats test plist", (i, self, a, b) => null);
        it.Def(F, "build", (i, self, a, b) => i.Main);
        it.Def(F, "head? bottle?", (i, self, a, b) => false);
        it.Def(F, "stable?", (i, self, a, b) => true);
        it.Def(F, "std_cmake_args", (i, self, a, b) => StrArray($"-DCMAKE_INSTALL_PREFIX={Prefix(i, self)}", "-DCMAKE_BUILD_TYPE=Release", "-DBUILD_TESTING=OFF", "-Wno-dev"));
        it.Def(F, "std_configure_args", (i, self, a, b) => StrArray($"--prefix={Prefix(i, self)}", "--disable-debug", "--disable-dependency-tracking"));
        it.Def(F, "system", (i, self, a, b) =>
        {
            var line = string.Join(" ", a.Where(x => x is not RHash).Select(i.ToS));
            Ohai(i, line);
            if (!Interp.Truthy(i.RunSystem(a)))
                throw i.Error(BuildError(i), $"Failed executing: {line}");
            return null;
        });
        it.Def(F, "inreplace", (i, self, a, b) => { Inreplace(i, a, b); return null; });

        // Windows packaging extensions (docs/FORMULA_API.md)
        it.Def(F, "install_tree", (i, self, a, b) =>
        {
            Ctx(i, self).InstallTree(a.Length > 0 ? i.PathStr(a[0]) : ".", a.Length > 1 ? i.PathStr(a[1]) : "libexec");
            return null;
        });
        it.Def(F, "install_resource", (i, self, a, b) => { Ctx(i, self).InstallResource(i.ToS(a[0]), i.PathStr(a[1])); return null; });
        it.Def(F, "windows_exe", (i, self, a, b) =>
        {
            Ctx(i, self).WindowsExe(i.PathStr(a[0]), a.Length > 1 ? i.ToS(a[1]) : null, a.Skip(2).Select(i.ToS).ToArray());
            return null;
        });
        it.Def(F, "node_script", (i, self, a, b) => { Ctx(i, self).NodeScript(i.PathStr(a[0]), i.ToS(a[1])); return null; });
        it.Def(F, "windows_cmd", (i, self, a, b) =>
        {
            i.Arity(a, 2, 4);
            Ctx(i, self).WindowsCmd(i.PathStr(a[0]), i.ToS(a[1]), a.Length > 2 ? i.ToS(a[2]) : null, a.Length > 3 ? i.PathStr(a[3]) : null);
            return null;
        });
        it.Def(F, "install_library", (i, self, a, b) => { Ctx(i, self).InstallLibrary(a.Length > 0 ? i.PathStr(a[0]) : "libexec/ucrt64"); return null; });
        it.Def(F, "msys2_install", (i, self, a, b) => { Ctx(i, self).Msys2Install(a.Length > 0 ? i.PathStr(a[0]) : ""); return null; });
        it.Def(F, "msys2_bin", (i, self, a, b) => { Ctx(i, self).Msys2Bin(i.PathStr(a[0]), a.Length > 1 ? i.ToS(a[1]) : null); return null; });

        // ---- output helpers (Kernel, like Homebrew's utils/output) ----
        it.Def(it.KernelModule, "ohai", (i, self, a, b) => { Ohai(i, string.Join(" ", a.Select(i.ToS))); return null; });
        it.Def(it.KernelModule, "opoo", (i, self, a, b) => { i.Err.WriteLine($"Warning: {i.ToS(a[0])}"); return null; });
        it.Def(it.KernelModule, "onoe", (i, self, a, b) => { i.Err.WriteLine($"Error: {i.ToS(a[0])}"); return null; });
        it.Def(it.KernelModule, "odie", (i, self, a, b) => throw i.Error(i.RuntimeError, i.ToS(a[0])));
        it.Def(it.KernelModule, "odebug", (i, self, a, b) => null);
        it.Def(it.KernelModule, "quiet_system", (i, self, a, b) => i.RunSystem(a));
        return F;
    }

    static RClass BuildError(Interp it) => it.ConstGet(it.ObjectClass, "BuildError") as RClass ?? it.DefineClass("BuildError", it.RuntimeError);

    static void Ohai(Interp it, string text)
    {
        if (Console.IsOutputRedirected) it.Out.WriteLine($"==> {text}");
        else it.Out.WriteLine($"\u001b[34m==>\u001b[0m \u001b[1m{text}\u001b[0m");
    }

    static string Quote(string s) => s.Length > 0 && !s.Any(c => char.IsWhiteSpace(c) || c == '"') ? s : "\"" + s.Replace("\"", "\\\"") + "\"";

    static RArray StrArray(params string[] items) => new(items.Select(x => (object)Interp.S(x)));

    static bool IsTag(Interp it, object v, params string[] tags) => v switch
    {
        RSymbol s => tags.Contains(s.Name),
        RArray a => a.L.Count > 0 && a.L.All(x => x is RSymbol s && tags.Contains(s.Name)),
        _ => false,
    };

    // SPDX-ish rendering, as `brew info` shows it.
    static string LicenseString(Interp it, object v) => v switch
    {
        RString s => s.V,
        RSymbol { Name: "public_domain" } => "Public Domain",
        RSymbol { Name: "cannot_represent" } => "Cannot Represent",
        RSymbol s => s.Name,
        RHash h when h.D.TryGetValue(Interp.Sym("any_of"), out var any) => string.Join(" or ", ((RArray)any).L.Select(x => Wrap(it, x))),
        RHash h when h.D.TryGetValue(Interp.Sym("all_of"), out var all) => string.Join(" and ", ((RArray)all).L.Select(x => Wrap(it, x))),
        RHash h => string.Join(" ", h.D.Select(kv => $"{LicenseString(it, kv.Key)} with {it.ToS(((RHash)kv.Value).D.Values.FirstOrDefault())}")),
        null => null,
        _ => it.ToS(v),
    };

    static string Wrap(Interp it, object v) => v is RHash h && h.D.Keys.Any(k => k is RSymbol { Name: "any_of" or "all_of" })
        ? "(" + LicenseString(it, v) + ")"
        : LicenseString(it, v);

    // Utils::Inreplace.inreplace
    static void Inreplace(Interp it, object[] a, RProc b)
    {
        var paths = a[0] is RArray arr ? arr.L : new List<object> { a[0] };
        object before = a.Length > 1 ? a[1] : null, after = a.Length > 2 ? a[2] : null;
        var errors = new List<string>();
        foreach (var p in paths)
        {
            var file = it.Abs(it.PathStr(p));
            var s = new RString(File.ReadAllText(file));
            var original = s.V;
            if (b != null) it.CallProc(b, new object[] { s });
            else
            {
                var res = it.Invoke(s, "gsub!", new[] { before, after ?? Interp.S("") }, null);
                if (res == null) errors.Add($"{it.PathStr(p)}:\n  expected replacement of {it.Inspect(before)} with {it.Inspect(after)}");
            }
            if (b != null && s.V == original) errors.Add($"{it.PathStr(p)}:\n  expected replacement of {it.Inspect(before)} with {it.Inspect(after)}");
            File.WriteAllText(file, s.V);
        }
        if (errors.Count > 0) throw it.Error(it.RuntimeError, "inreplace failed\n" + string.Join("\n", errors));
    }

    // A small subset of Homebrew's Version.parse.
    public static string ParseVersion(string url)
    {
        if (url == null) return null;
        var path = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.AbsolutePath : url;
        var stem = Regex.Replace(Path.GetFileName(path), @"\.(tar\.(gz|bz2|xz|zst|lz|Z)|t[gbx]z|zip|7z|exe|msi|nupkg|msix|tgz|gem|jar|pkg\.tar\.zst)$", "", RegexOptions.IgnoreCase);
        foreach (var pat in new[]
                 {
                     @"[-_]v?(\d+(?:\.\d+)+(?:[-_.]?(?:alpha|beta|rc|pre|p)\d*)?)(?:[-_.]|$)",
                     @"^v?(\d+(?:\.\d+)+)$",
                     @"[-_]v?(\d+)$",
                 })
        {
            var m = Regex.Match(stem, pat, RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
        }
        var gh = Regex.Match(path, @"/(?:releases/download|archive/refs/tags|archive)/(?:[a-zA-Z-]*?)v?(\d+(?:\.\d+)+)(?:/|\.)");
        return gh.Success ? gh.Groups[1].Value : null;
    }
}
