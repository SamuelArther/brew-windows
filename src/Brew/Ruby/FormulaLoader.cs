using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Brew.Formulae;

namespace Brew.Ruby;

// Evaluates a formula .rb file with the mini-Ruby interpreter. See docs/FORMULA_API.md.
public static partial class FormulaLoader
{
    public static FormulaDefinition Load(string path) => LoadSpec(path).ToDefinition();

    public static FormulaSpec LoadSpec(string path) => OnBigStack(() => LoadCore(Path.GetFullPath(path)));

    // Formula code can recurse deeply through the interpreter; give it room.
    internal static T OnBigStack<T>(Func<T> fn)
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

    // An interpreter with the Formula class and its preludes, for tests of the formula API.
    public static Interp NewInterpreterForTests()
    {
        var it = new Interp();
        DefineFormula(it);
        return it;
    }

    static FormulaSpec LoadCore(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var it = new Interp();
        var formulaClass = DefineFormula(it);
        try { it.Run(File.ReadAllText(path), path); }
        catch (RubySyntaxError e) { throw new InvalidOperationException($"{name}: {path}: {e.Message}", e); }
        catch (RubyError e) { throw new InvalidOperationException($"{name}: {Where(it, path)}{Describe(it, e)}", e); }

        var className = ClassName(name);
        if (it.ObjectClass.Consts.GetValueOrDefault(className) is not RClass cls || !it.Ancestors(cls).Contains(formulaClass))
        {
            var classes = it.ObjectClass.Consts.Values.OfType<RClass>().Where(c => c.Host is FormulaSpec || c.Super == formulaClass).ToList();
            var found = classes.Count == 0 ? "found no classes" : "only found: " + string.Join(", ", classes.Select(c => c.Name.Split("::")[^1]));
            throw new InvalidOperationException($"No available formula with the name \"{name}\".\nIn formula file: {path}\nExpected to find class {className}, but {found}.");
        }

        var spec = SpecOf(cls);
        spec.Name = name;
        spec.Path = path;
        spec.ClassName = className;
        spec.Interp = it;
        spec.Class = cls;
        if (spec.Url == null) throw new InvalidOperationException($"{name}: formula requires at least a URL");
        if (spec.Version == null)
        {
            // Homebrew's Version.detect: a git source's version comes from its tag.
            spec.Version = spec.UrlTag != null && GitSources.VersionFromTag(spec.UrlTag) is { } tagged ? tagged : Versions.Parse(spec.Url);
            spec.VersionFromUrl = true;
        }
        if (spec.Version == null) throw new InvalidOperationException($"{name}: version could not be detected from URL: {spec.Url}");
        spec.HasCaveats = it.FindMethod(cls, "caveats")?.Owner != formulaClass;
        spec.HasPostInstall = it.FindMethod(cls, "post_install")?.Owner != formulaClass;
        return spec;
    }

    static string Where(Interp it, string path) => it.CurLine > 0 ? $"{path}:{it.CurLine}: " : "";

    static string Describe(Interp it, RubyError e) =>
        e.ClassName is "RuntimeError" ? e.Message : $"{e.Message} ({e.ClassName})";

    // Creates a formula instance bound to ctx and runs fn with it (on a big stack).
    internal static T WithInstance<T>(FormulaSpec spec, InstallContext ctx, Func<Interp, RObject, T> fn) => OnBigStack(() =>
    {
        var it = spec.Interp;
        var f = new RObject { Class = spec.Class, Host = ctx };
        f.SetIvar("@name", Interp.S(spec.Name));
        f.SetIvar("@version", Interp.S(spec.Version));
        it.Cwd = ctx.BuildPath;
        foreach (var kv in ctx.BuildEnvironment) it.Env[kv.Key] = kv.Value;
        if (ctx.BrewPrefix != null)
        {
            it.ObjectClass.Consts["HOMEBREW_PREFIX"] = it.NewPath(ctx.BrewPrefix);
            it.ObjectClass.Consts["HOMEBREW_CELLAR"] = it.NewPath(Path.Combine(ctx.BrewPrefix, "Cellar"));
            it.InstallRenamedRoots.Add(Path.GetFullPath(Path.Combine(ctx.BrewPrefix, "etc")));
        }
        it.PendingCommand = full =>
        {
            var bin = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ctx.Bin));
            if (!string.Equals(Path.GetDirectoryName(full), bin, StringComparison.OrdinalIgnoreCase)) return null;
            var name = Path.GetFileName(full);
            return ctx.Commands.LastOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileNameWithoutExtension(c.Name), name, StringComparison.OrdinalIgnoreCase))?.Target;
        };
        it.DropPendingCommand = full =>
        {
            if (!string.Equals(Path.GetDirectoryName(full), Path.TrimEndingDirectorySeparator(Path.GetFullPath(ctx.Bin)), StringComparison.OrdinalIgnoreCase))
                return false;
            var name = Path.GetFileName(full);
            return ctx.Commands.RemoveAll(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileNameWithoutExtension(c.Name), name, StringComparison.OrdinalIgnoreCase)) > 0;
        };
        var buildRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ctx.BuildPath)) + Path.DirectorySeparatorChar;
        it.InstallHook = (dir, src, newName) =>
        {
            if (!string.Equals(Path.TrimEndingDirectorySeparator(dir), Path.TrimEndingDirectorySeparator(Path.GetFullPath(ctx.Bin)), StringComparison.OrdinalIgnoreCase)
                || !src.StartsWith(buildRoot, StringComparison.OrdinalIgnoreCase) || Directory.Exists(src))
                return false;
            ctx.InstallBin(Path.GetRelativePath(ctx.BuildPath, src), newName);
            return true;
        };
        try { return fn(it, f); }
        catch (RubyError e) { throw new InvalidOperationException($"{spec.Name}: {Describe(it, e)}", e); }
        catch (RubySyntaxError e) { throw new InvalidOperationException($"{spec.Name}: {e.Message}", e); }
    });

    // Homebrew runs a formula's own `def fetch` (network allowed) before `def install`.
    internal static void RunInstall(FormulaSpec spec, InstallContext ctx) =>
        WithInstance(spec, ctx, (it, f) =>
        {
            if (DefinesFetch(spec)) it.Invoke(f, "fetch", Array.Empty<object>(), null);
            return it.Invoke(f, "install", Array.Empty<object>(), null);
        });

    internal static bool DefinesFetch(FormulaSpec spec) =>
        spec.Interp.FindMethod(spec.Class, "fetch") is { } m && m.Owner != spec.Interp.ObjectClass.Consts["Formula"];

    internal static void RunPostInstall(FormulaSpec spec, string prefix, string brewPrefix)
    {
        var steps = HasPostInstallSteps(spec);
        if (!spec.HasPostInstall && !steps) return;
        WithInstance(spec, new InstallContext(prefix, prefix, null, brewPrefix), (it, f) =>
        {
            if (steps) it.Invoke(f, "__run_post_install_steps", Array.Empty<object>(), null);
            return spec.HasPostInstall ? it.Invoke(f, "post_install", Array.Empty<object>(), null) : null;
        });
    }

    internal static void RunTest(FormulaSpec spec, string prefix, string brewPrefix, string testPath)
    {
        if (spec.TestBlock == null) throw new InvalidOperationException($"{spec.Name} defines no test");
        Directory.CreateDirectory(testPath);
        var context = new InstallContext(prefix, testPath, null, brewPrefix);
        if (brewPrefix != null)
            BuildEnvironment.ApplyForTest(new Paths(), context, spec.Dependencies.Select(d => d.Name).Append(spec.Name));
        WithInstance(spec, context, (it, f) =>
        {
            f.SetIvar("@__testpath", Interp.S(testPath));
            it.Env["HOME"] = testPath;
            it.Env["USERPROFILE"] = testPath;
            // Windows tools look up their config/cache dirs under the profile.
            var local = Path.Combine(testPath, "AppData", "Local");
            var roaming = Path.Combine(testPath, "AppData", "Roaming");
            Directory.CreateDirectory(local);
            Directory.CreateDirectory(roaming);
            it.Env["LOCALAPPDATA"] = local;
            it.Env["APPDATA"] = roaming;
            return it.CallProc(spec.TestBlock, Array.Empty<object>(), null, f, null);
        });
    }

    static FormulaSpec SpecOf(RClass c) => (FormulaSpec)(c.Host ??= new FormulaSpec());

    // Implemented in TestHelpers.cs (assertions, shell_output, testpath, ...).
    static partial void DefineTestHelpers(Interp it, RClass formula);

    static InstallContext Ctx(Interp it, object self) =>
        (self as RObject)?.Host as InstallContext ?? throw it.Error(it.RuntimeError, "this method is only available during install");

    static RClass DefineFormula(Interp it)
    {
        var F = it.DefineClass("Formula");
        F.Includes.Add(it.FileUtilsModule);   // Homebrew's Formula includes FileUtils (mv, cp, rm_r, ...)
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
        // DevelopmentTools: the toolchain is MSYS2's GCC, so there is no Apple clang or ld64 to version.
        var devTools = it.DefineModule("DevelopmentTools");
        it.DefS(devTools, "clang_build_version clang_version llvm_clang_build_version ld64_version", (i, s, a, b) => VersionObject(i, "0"));
        it.DefS(devTools, "default_compiler", (i, s, a, b) => Interp.Sym("gcc"));
        it.DefS(devTools, "installed?", (i, s, a, b) => BuildEnvironment.Msys2Root != null);
        it.DefS(devTools, "locate", (i, s, a, b) =>
        {
            var found = i.ResolveProgram(i.ToS(a[0]));
            return File.Exists(found) ? i.NewPath(found) : null;
        });
        it.DefS(devTools, "gcc_version", (i, s, a, b) =>
        {
            var cc = a.Length > 0 ? i.ToS(a[0]) : "gcc";
            var found = i.ResolveProgram(cc.StartsWith("gcc") ? "gcc" : cc);
            if (!File.Exists(found)) return VersionObject(i, "0");
            var version = i.Backtick(Quote(found) + " -dumpversion").ToString()?.Trim();
            return VersionObject(i, string.IsNullOrEmpty(version) ? "0" : version);
        });
        var utils = it.DefineModule("Utils");
        // Utils.git_head(repo, length: nil)
        it.DefS(utils, "git_head git_short_head", (i, s, a, b) =>
        {
            var repo = a.Length > 0 && a[0] is not RHash ? i.Abs(i.PathStr(a[0])) : i.Cwd;
            var head = i.Backtick($"git -C \"{repo}\" rev-parse HEAD") is RString r ? r.V.Trim() : "";
            if (head.Length == 0) return null;
            var length = a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Interp.Sym("length"), out var l) && l is long n ? (int)n : head.Length;
            return Interp.S(head[..Math.Min(length, head.Length)]);
        });
        it.DefS(utils, "safe_popen_read popen_read",(i, s, a, b) => i.Backtick(string.Join(" ", a.Select(i.ToS).Select(Quote))));
        // Utils::Gzip.compress(*paths): each file becomes file.gz; returns the .gz paths.
        var gzip = it.DefineModule("Gzip", utils);
        it.DefS(gzip, "compress", (i, s, a, b) => new RArray(a.Where(x => x is not RHash).Select(x =>
        {
            var source = i.Abs(i.PathStr(x));
            var target = source + ".gz";
            using (var input = File.OpenRead(source))
            using (var output = File.Create(target))
            using (var stream = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.SmallestSize))
                input.CopyTo(stream);
            File.Delete(source);
            return (object)i.NewPath(target);
        }).ToList()));

        // ---- class-level DSL ----
        FormulaSpec Sp(object self) => SpecOf((RClass)self);
        object Kw(Interp i, object[] a, string key) =>
            a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Interp.Sym(key), out var v) ? v : null;
        void Field(string names, Action<FormulaSpec, string> set) =>
            it.DefS(F, names, (i, self, a, b) =>
            {
                i.Arity(a, 1, 2);
                set(Sp(self), i.ToS(a[0]));
                return null;
            });
        Field("desc", (s, v) => s.Desc = v);
        Field("homepage", (s, v) => s.Homepage = v);
        it.DefS(F, "url", (i, self, a, b) =>
        {
            var spec = Sp(self);
            spec.Url = i.ToS(a[0]);
            string Opt(string key) => Kw(i, a, key) is { } v ? (v is RSymbol sym ? sym.Name : i.ToS(v)) : null;
            spec.UrlTag = Opt("tag");
            spec.UrlRevision = Opt("revision");
            spec.UrlBranch = Opt("branch");
            spec.UrlUsing = Opt("using");
            return null;
        });
        Field("version", (s, v) => s.Version = v);
        Field("sha256", (s, v) => s.Sha256 = v.ToLowerInvariant());
        Field("mirror", (s, v) => s.Mirrors.Add(v));
        it.DefS(F, "license", (i, self, a, b) => { Sp(self).License = LicenseString(i, a[0]); return null; });
        it.DefS(F, "revision", (i, self, a, b) => { Sp(self).Revision = (int)i.Int(a[0]); return null; });
        it.DefS(F, "version_scheme", (i, self, a, b) => { Sp(self).VersionScheme = (int)i.Int(a[0]); return null; });
        it.DefS(F, "depends_on", (i, self, a, b) => { AddDeps(i, Sp(self).Dependencies, a); return null; });
        it.DefS(F, "uses_from_macos", (i, self, a, b) => { AddDeps(i, Sp(self).UsesFromMacos, a); return null; });
        it.DefS(F, "conflicts_with", (i, self, a, b) =>
        {
            var because = Kw(i, a, "because") is { } bc ? i.ToS(bc) : null;
            foreach (var x in a) if (x is RString s) Sp(self).Conflicts.Add(new Conflict(s.V, because));
            return null;
        });
        it.DefS(F, "keg_only", (i, self, a, b) =>
        {
            var spec = Sp(self);
            spec.KegOnlyReason = a[0] is RSymbol ? a[0] : i.ToS(a[0]);
            spec.KegOnlyExplanation = a.Length > 1 ? i.ToS(a[1]) : "";
            return null;
        });
        it.DefS(F, "option", (i, self, a, b) => { Sp(self).Options.Add((i.ToS(a[0]), a.Length > 1 ? i.ToS(a[1]) : "")); return null; });
        it.DefS(F, "deprecate!", (i, self, a, b) =>
        {
            var spec = Sp(self);
            spec.DeprecationDate = ParseDate(i, Kw(i, a, "date"));
            if (spec.DeprecationDate > DateOnly.FromDateTime(DateTime.Today)) return null;
            spec.Deprecated = true;
            spec.DeprecationReason = Reason(i, Kw(i, a, "because"));
            spec.DeprecationReplacement = Replacement(i, a, Kw);
            return null;
        });
        it.DefS(F, "disable!", (i, self, a, b) =>
        {
            var spec = Sp(self);
            spec.DisableDate = ParseDate(i, Kw(i, a, "date"));
            var reason = Reason(i, Kw(i, a, "because"));
            if (spec.DisableDate > DateOnly.FromDateTime(DateTime.Today))
            {
                spec.Deprecated = true;
                spec.DeprecationReason = reason;
                spec.DeprecationReplacement = Replacement(i, a, Kw);
                return null;
            }
            spec.Disabled = true;
            spec.Deprecated = false;
            spec.DisableReason = reason;
            spec.DisableReplacement = Replacement(i, a, Kw);
            return null;
        });
        it.DefS(F, "head", (i, self, a, b) =>
        {
            if (a.Length > 0) Sp(self).HeadUrl = i.ToS(a[0]);
            if (b != null)
            {
                var holder = NewHolder(i, F, new FormulaSpec());
                i.CallProc(b, Array.Empty<object>(), null, holder, i.SingletonOf(holder));
                Sp(self).HeadUrl = ((FormulaSpec)holder.Host).Url;
            }
            return null;
        });
        it.DefS(F, "resource", (i, self, a, b) =>
        {
            var holder = NewHolder(i, F, new FormulaSpec());
            // Resource livecheck blocks reference livecheck-time constants (LATEST_VERSION); they are not evaluated here.
            i.Def(i.SingletonOf(holder), "livecheck", (_, _, _, _) => null);
            if (b != null) i.CallProc(b, Array.Empty<object>(), null, holder, i.SingletonOf(holder));
            var r = (FormulaSpec)holder.Host;
            Sp(self).Resources.Add(new FormulaResource(i.ToS(a[0]), r.Url, r.Sha256, r.UrlTag, r.UrlRevision, r.UrlBranch, r.UrlUsing));
            return null;
        });
        it.DefS(F, "patch", (i, self, a, b) =>
        {
            var patch = new PatchSpec();
            var args = a.Where(x => x is not RHash).ToList();
            if (args.Count > 0 && args[0] is RSymbol strip && strip.Name.Length > 1 && strip.Name[0] == 'p' && int.TryParse(strip.Name[1..], out var level))
            {
                patch.Strip = level;
                args.RemoveAt(0);
            }
            if (args.Count > 0) patch.Data = args[0] is RSymbol { Name: "DATA" } ? FormulaSpec.DataMarker : i.ToS(args[0]);
            if (b != null)
            {
                var holder = new RObject { Class = i.ObjectClass, Host = patch };
                var s = i.SingletonOf(holder);
                i.Def(s, "url", (i2, _, a2, _) => { patch.Url = i2.ToS(a2[0]); return null; });
                i.Def(s, "sha256", (i2, _, a2, _) => { patch.Sha256 = i2.ToS(a2[0]).ToLowerInvariant(); return null; });
                i.Def(s, "directory", (i2, _, a2, _) => { patch.Directory = i2.ToS(a2[0]); return null; });
                i.Def(s, "file", (i2, _, a2, _) => { patch.File = i2.ToS(a2[0]); return null; });
                i.Def(s, "apply", (i2, _, a2, _) => { foreach (var x in a2.SelectMany(v => v is RArray arr ? arr.L : [v])) patch.Apply.Add(i2.ToS(x)); return null; });
                i.Def(s, "mirror resolves type", (_, _, _, _) => null);
                i.CallProc(b, Array.Empty<object>(), null, holder, s);
            }
            Sp(self).Patches.Add(patch);
            return null;
        });
        it.DefS(F, "test", (i, self, a, b) => { Sp(self).TestBlock = b; return null; });
        it.DefS(F, "livecheck", (i, self, a, b) =>
        {
            var lc = Sp(self).Livecheck = new LivecheckSpec();
            if (b == null) return null;
            var holder = new RObject { Class = LivecheckClass(i), Host = lc };
            i.CallProc(b, Array.Empty<object>(), null, holder, i.SingletonOf(holder));
            return null;
        });
        it.DefS(F, "stable", (i, self, a, b) => b == null ? null : i.CallProc(b, Array.Empty<object>(), null, self, i.SingletonOf((RClass)self)));
        // Only the Windows (and matching CPU) branches run.
        it.DefS(F, "on_windows", (i, self, a, b) => b == null ? null : i.CallProc(b, Array.Empty<object>(), null, self, i.SingletonOf((RClass)self)));
        it.DefS(F, arm ? "on_arm" : "on_intel", (i, self, a, b) => b == null ? null : i.CallProc(b, Array.Empty<object>(), null, self, i.SingletonOf((RClass)self)));
        it.DefS(F, (arm ? "on_intel" : "on_arm") + " on_macos on_linux on_system on_tahoe on_sequoia on_sonoma on_ventura on_monterey on_big_sur",
            (i, self, a, b) => null);
        // The same branches inside other DSL blocks (patch, resource) and inside methods (def install).
        it.Def(it.ObjectClass, "on_windows " + (arm ? "on_arm" : "on_intel"), (i, self, a, b) => b == null ? null : i.CallProc(b, Array.Empty<object>()));
        it.Def(it.ObjectClass, (arm ? "on_intel" : "on_arm") + " on_macos on_linux on_system on_tahoe on_sequoia on_sonoma on_ventura on_monterey on_big_sur",
            (i, self, a, b) => null);
        it.DefS(F, "deprecated_option skip_clean cxxstdlib_check link_overwrite service fails_with needs pour_bottle? " +
                   "compatibility_version no_autobump! pypi_packages env allow_network_access! deny_network_access!",
            (i, self, a, b) => null);

        // ---- instance methods (install time) ----
        string Prefix(Interp i, object self) => Ctx(i, self).Prefix;
        string Nm(object self) => ((RObject)self).GetIvar("@name").ToString();
        RObject Under(Interp i, object self, string rel) => i.NewPath(Path.Combine(Prefix(i, self), rel));
        it.Def(F, "name full_name", (i, self, a, b) => ((RObject)self).GetIvar("@name"));
        it.Def(F, "version", (i, self, a, b) => VersionObject(i, ((RObject)self).GetIvar("@version").ToString()));
        it.DefS(F, "[]", (i, self, a, b) => OtherFormula(i, i.StrOrSym(a[0])));
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
        // Shared, not per keg: config and state outlive upgrades (EtcVar carries them in bottles).
        it.Def(F, "etc", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? Prefix(i, self), "etc")));
        it.Def(F, "pkgetc", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? Prefix(i, self), "etc", Nm(self))));
        it.Def(F, "var", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? Prefix(i, self), "var")));
        it.Def(F, "buildpath", (i, self, a, b) => i.NewPath(Ctx(i, self).BuildPath));
        // Utils::Path::FormulaHelpers: another formula's stable opt paths by name.
        foreach (var (method, sub) in new[] { ("formula_opt_prefix", ""), ("formula_opt_bin", "bin"), ("formula_opt_lib", "lib"),
                     ("formula_opt_libexec", "libexec"), ("formula_opt_include", "include") })
        {
            var subdir = sub;
            it.Def(F, method, (i, self, a, b) =>
            {
                var name = i.ToS(a[0]).Split('/')[^1];
                var brewPrefix = i.ConstGet(i.ObjectClass, "HOMEBREW_PREFIX") is RObject { Host: string hp } ? hp : (self as RObject)?.Host is InstallContext c ? c.BrewPrefix : "";
                return i.NewPath(Path.Combine(brewPrefix ?? "", "opt", name, subdir).TrimEnd('\\'));
            });
        }
        // Service file locations (Formula#plist_name, #launchd_service_path, #systemd_service_path).
        it.Def(F, "plist_name service_name", (i, self, a, b) => Interp.S("homebrew.mxcl." + Nm(self)));
        it.Def(F, "launchd_service_path", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), $"homebrew.mxcl.{Nm(self)}.plist")));
        it.Def(F, "systemd_service_path", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), $"homebrew.{Nm(self)}.service")));
        // Formula#stable: the SoftwareSpec (url, version, checksum, specs; stage fetches and unpacks it).
        it.Def(F, "stable", (i, self, a, b) =>
        {
            var spec = SpecOf(((RObject)self).Class);
            var cls = i.ConstGet(i.ObjectClass, "SoftwareSpec") as RClass;
            if (cls == null)
            {
                cls = i.DefineClass("SoftwareSpec");
                FormulaSpec Sw(object o) => (FormulaSpec)((RObject)o).Host;
                i.Def(cls, "url", (ii, s, _, _) => Interp.S(Sw(s).Url));
                i.Def(cls, "version", (ii, s, _, _) => VersionObject(ii, Sw(s).Version));
                i.Def(cls, "mirrors", (ii, s, _, _) => new RArray(Sw(s).Mirrors.Select(m => (object)Interp.S(m))));
                i.Def(cls, "checksum", (ii, s, _, _) => Sw(s).Sha256 == null ? null : Interp.S(Sw(s).Sha256));
                i.Def(cls, "specs", (ii, s, _, _) =>
                {
                    var h = new RHash();
                    if (Sw(s).UrlTag != null) h.D[Interp.Sym("tag")] = Interp.S(Sw(s).UrlTag);
                    if (Sw(s).UrlRevision != null) h.D[Interp.Sym("revision")] = Interp.S(Sw(s).UrlRevision);
                    return h;
                });
                i.Def(cls, "stage", (ii, s, args, blk) =>
                {
                    var sw = Sw(s);
                    var into = args.Length > 0 && args[0] is not RHash ? ii.Abs(ii.PathStr(args[0])) : Path.Combine(ii.Cwd, "stable-" + Guid.NewGuid().ToString("N")[..8]);
                    string staged;
                    if (sw.GitSource) staged = GitSources.StageAsync(sw.Url, sw.UrlTag, sw.UrlRevision, sw.UrlBranch ?? "", into).GetAwaiter().GetResult();
                    else
                    {
                        var file = new Downloader(new Paths().Cache).FetchAsync(sw.Url, sw.Sha256, sw.Mirrors).GetAwaiter().GetResult();
                        staged = Archive.StageAsync(file, Downloader.FileName(sw.Url), into).GetAwaiter().GetResult();
                    }
                    return blk == null ? ii.NewPath(staged) : ii.Invoke(ii.DirClass, "chdir", new object[] { ii.NewPath(staged) }, blk);
                });
            }
            return new RObject { Class = cls, Host = spec };
        });
        // Formula#deps: Dependency objects (name, tags, to_formula).
        it.Def(F, "deps", (i, self, a, b) =>
        {
            var depClass = i.ConstGet(i.ObjectClass, "Dependency") as RClass;
            if (depClass == null)
            {
                depClass = i.DefineClass("Dependency");
                string N(object o) => ((Dependency)((RObject)o).Host).Name;
                i.Def(depClass, "name to_s", (ii, s, _, _) => Interp.S(N(s)));
                i.Def(depClass, "to_formula", (ii, s, _, _) => OtherFormula(ii, N(s)));
                i.Def(depClass, "tags", (ii, s, _, _) => new RArray(((Dependency)((RObject)s).Host).Tags.Select(t => (object)Interp.Sym(t))));
                i.Def(depClass, "build?", (ii, s, _, _) => ((Dependency)((RObject)s).Host).Tags.Contains("build"));
                i.Def(depClass, "test?", (ii, s, _, _) => ((Dependency)((RObject)s).Host).Tags.Contains("test"));
                i.Def(depClass, "optional?", (ii, s, _, _) => ((Dependency)((RObject)s).Host).Tags.Contains("optional"));
                i.Def(depClass, "recommended?", (ii, s, _, _) => ((Dependency)((RObject)s).Host).Tags.Contains("recommended"));
            }
            return new RArray(SpecOf(((RObject)self).Class).Dependencies.Select(d => (object)new RObject { Class = depClass, Host = d }));
        });
        it.Def(F, "opt_prefix", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self))));
        it.Def(F, "opt_bin", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), "bin")));
        it.Def(F, "opt_lib", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), "lib")));
        it.Def(F, "opt_libexec", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), "libexec")));
        foreach (var (method, rel) in new[] { ("opt_sbin", "sbin"), ("opt_include", "include"), ("opt_share", "share"),
                     ("opt_frameworks", "Frameworks"), ("opt_elisp", "share/emacs/site-lisp") })
        {
            var r = rel;
            it.Def(F, method, (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), r)));
        }
        it.Def(F, "opt_pkgshare", (i, self, a, b) => i.NewPath(Path.Combine(Ctx(i, self).BrewPrefix ?? "", "opt", Nm(self), "share", Nm(self))));
        it.Def(F, "caveats post_install plist", (i, self, a, b) => null);
        // Homebrew's BuildOptions: stable builds, no options; bottle? while building one.
        it.Def(F, "build", (i, self, a, b) =>
        {
            var options = new RObject { Class = i.ObjectClass };
            var s = i.SingletonOf(options);
            var bottling = (self as RObject)?.Host is InstallContext ctx && (ctx.PouredFromBottle || i.Env.ContainsKey("HOMEBREW_BUILD_BOTTLE"));
            i.Def(s, "head?", (_, _, _, _) => false);
            i.Def(s, "stable?", (_, _, _, _) => true);
            i.Def(s, "bottle?", (_, _, _, _) => bottling);
            i.Def(s, "with? include?", (_, _, a2, _) => false);
            i.Def(s, "without?", (_, _, a2, _) => true);
            i.Def(s, "used_options unused_options", (_, _, _, _) => new RArray());
            return options;
        });
        it.Def(F, "head?", (i, self, a, b) => false);
        it.Def(F, "stable?", (i, self, a, b) => true);
        // Upstream's argument lists; paths use forward slashes (CMake and configure scripts treat '\' as an escape).
        string Forward(string p) => p.Replace('\\', '/');
        it.Def(F, "std_cmake_args", (i, self, a, b) =>
        {
            var installPrefix = Kw(i, a, "install_prefix") is { } ip ? i.PathStr(ip) : Prefix(i, self);
            var libdir = Kw(i, a, "install_libdir") is { } ld ? i.PathStr(ld) : "lib";
            var framework = Kw(i, a, "find_framework") is { } ff ? i.ToS(ff) : "LAST";
            return StrArray($"-DCMAKE_INSTALL_PREFIX={Forward(installPrefix)}", $"-DCMAKE_INSTALL_LIBDIR={Forward(libdir)}", "-DCMAKE_BUILD_TYPE=Release",
                $"-DCMAKE_FIND_FRAMEWORK={framework}", "-DCMAKE_VERBOSE_MAKEFILE=ON", "-Wno-author", "-DBUILD_TESTING=OFF", "-DCCACHE_FOUND=OFF");
        });
        it.Def(F, "std_configure_args", (i, self, a, b) =>
        {
            var prefix = Kw(i, a, "prefix") is { } p ? i.PathStr(p) : Prefix(i, self);
            var libdir = Kw(i, a, "libdir") is { } l ? i.PathStr(l) : "lib";
            var fullLibdir = Path.IsPathRooted(libdir) ? libdir : Path.GetFullPath(Path.Combine(prefix, libdir));
            return StrArray("--disable-debug", "--disable-dependency-tracking", $"--prefix={Forward(prefix)}", $"--libdir={Forward(fullLibdir)}");
        });
        it.Def(F, "system", (i, self, a, b) =>
        {
            var line = string.Join(" ", a.Where(x => x is not RHash).Select(i.ToS));
            Ohai(i, line);
            if (!Interp.Truthy(i.RunSystem(a)))
                throw i.Error(BuildError(i), $"Failed executing: {line}");
            WriteNpmBinLinks(i, a.Where(x => x is not RHash).Select(i.ToS).ToList());
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
        DefineTestHelpers(it, F);
        DefineBuildDsl(it, F);
        DefinePythonDsl(it, F);
        DefineNodeDsl(it, F);
        DefineJavaDsl(it, F);
        it.Run(InstallStepsPrelude, "<install_steps>");
        return F;
    }

    // Homebrew's Version: a string-like object with major/minor/patch helpers.
    internal static RObject VersionObject(Interp it, string v)
    {
        var cls = it.ConstGet(it.ObjectClass, "Version") as RClass;
        if (cls == null)
        {
            cls = it.DefineClass("Version");
            cls.Includes.Add(it.ComparableModule);
            string V(object o) => (string)((RObject)o).Host;
            string Other(Interp i, object o) => o is RObject { Host: string h } ? h : i.ToS(o);
            it.DefS(cls, "new", (i, self, a, b) => VersionObject(i, i.ToS(a[0])));
            it.Def(cls, "to_s to_str", (i, self, a, b) => Interp.S(V(self)));
            it.Def(cls, "inspect", (i, self, a, b) => Interp.S($"#<Version {V(self)}>"));
            it.Def(cls, "<=>", (i, self, a, b) => a[0] == null ? null : (long)Versions.Compare(V(self), Other(i, a[0])));
            it.Def(cls, "== eql?", (i, self, a, b) => a[0] != null && Versions.Compare(V(self), Other(i, a[0])) == 0);
            it.Def(cls, "hash", (i, self, a, b) => (long)V(self).GetHashCode());
            it.Def(cls, "major", (i, self, a, b) => Ver(i, Versions.Major(V(self))));
            it.Def(cls, "minor", (i, self, a, b) => Ver(i, Versions.Minor(V(self))));
            it.Def(cls, "patch", (i, self, a, b) => Ver(i, Versions.Patch(V(self))));
            it.Def(cls, "major_minor", (i, self, a, b) => Ver(i, Versions.MajorMinor(V(self))));
            it.Def(cls, "major_minor_patch", (i, self, a, b) => Ver(i, Versions.MajorMinorPatch(V(self))));
            it.Def(cls, "head?", (i, self, a, b) => V(self).StartsWith("HEAD"));
            it.Def(cls, "null?", (i, self, a, b) => false);
            it.Def(cls, "to_i", (i, self, a, b) => i.Invoke(Interp.S(V(self)), "to_i", Array.Empty<object>(), null));
            it.Def(cls, "to_f", (i, self, a, b) => i.Invoke(Interp.S(V(self)), "to_f", Array.Empty<object>(), null));
            foreach (var m in new[] { "sub", "gsub", "split", "tr", "start_with?", "end_with?", "include?", "=~", "match?", "length", "+", "delete" })
            {
                var name = m;
                it.Def(cls, name, (i, self, a, b) => i.Invoke(Interp.S(V(self)), name, a, b));
            }
        }
        return new RObject { Class = cls, Host = v };
    }

    static object Ver(Interp it, string v) => v == null ? null : VersionObject(it, v);

    // Formula["name"] from within a formula: another formula of the same tap, at its opt prefix.
    static RObject OtherFormula(Interp it, string name)
    {
        var self = it.Main;
        var current = it.CurrentFile;
        var formulaDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(current)));
        var path = Path.Combine(formulaDir ?? "", DevCommands.Shard(name), name + ".rb");
        if (!File.Exists(path)) path = DevCommands.FormulaPath(name) ?? path;
        if (!File.Exists(path)) throw it.Error(it.ConstGet(it.ObjectClass, "FormulaUnavailableError") as RClass ?? it.DefineClass("FormulaUnavailableError", it.RuntimeError),
            $"No available formula with the name \"{name}\".");
        var spec = LoadSpec(path);
        var cls = it.ConstGet(it.ObjectClass, "FormulaReference") as RClass;
        if (cls == null)
        {
            cls = it.DefineClass("FormulaReference");
            FormulaSpec S(object o) => (FormulaSpec)((RObject)o).Host;
            string Opt(Interp i, object o, string sub)
            {
                var prefix = i.ConstGet(i.ObjectClass, "HOMEBREW_PREFIX") is RObject { Host: string hp } ? hp : DevCommands.Prefix;
                return Path.Combine(prefix, "opt", S(o).Name, sub);
            }
            it.Def(cls, "name full_name to_s", (i, s, a, b) => Interp.S(S(s).Name));
            it.Def(cls, "version", (i, s, a, b) => VersionObject(i, S(s).Version));
            it.Def(cls, "pkg_version", (i, s, a, b) => Interp.S(S(s).PkgVersion));
            it.Def(cls, "opt_prefix prefix", (i, s, a, b) => i.NewPath(Opt(i, s, "")));
            foreach (var (m, sub) in new[] { ("opt_bin", "bin"), ("bin", "bin"), ("opt_lib", "lib"), ("lib", "lib"), ("opt_libexec", "libexec"), ("libexec", "libexec"), ("opt_include", "include"), ("include", "include"), ("opt_share", "share"), ("share", "share"), ("opt_pkgshare", "share") })
            {
                var sb = sub;
                var isPkg = m == "opt_pkgshare";
                it.Def(cls, m, (i, s, a, b) => i.NewPath(isPkg ? Path.Combine(Opt(i, s, sb), S(s).Name) : Opt(i, s, sb)));
            }
            it.Def(cls, "any_version_installed? latest_version_installed? installed?", (i, s, a, b) => Directory.Exists(Opt(i, s, "")));
        }
        return new RObject { Class = cls, Host = spec };
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

    // SPDX.license_expression_to_string (utils/spdx.rb), as `brew info` and its JSON show it.
    static string LicenseString(Interp it, object v, bool bracket = false, object hashType = null)
    {
        switch (v)
        {
            case RString s: return s.V;
            case RSymbol s: return "LicenseRef-Homebrew-" + s.Name.Replace('_', '-');
            case RHash h:
                var expressions = new List<string>();
                if (h.D.Count == 1)
                {
                    var (key, value) = h.D.First();
                    hashType = key;
                    if (key is RString ks)
                        expressions.Add($"{ks.V} WITH {it.ToS(value is RHash w && w.D.TryGetValue(Interp.Sym("with"), out var with) ? with : null)}");
                    else
                        expressions.AddRange(((RArray)value).L.Select(x => LicenseString(it, x, true, hashType)));
                }
                else
                {
                    bracket = false;
                    foreach (var kv in h.D)
                    {
                        var single = new RHash(); single.D[kv.Key] = kv.Value;
                        expressions.Add(LicenseString(it, single, true));
                    }
                }
                var joined = string.Join(hashType is RSymbol { Name: "any_of" } ? " OR " : " AND ", expressions);
                return bracket ? $"({joined})" : joined;
            case null: return null;
            default: return it.ToS(v);
        }
    }

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

    static void AddDeps(Interp it, List<Dependency> list, object[] a)
    {
        foreach (var arg in a)
        {
            if (arg is RString s) list.Add(new Dependency(s.V, Array.Empty<string>()));
            else if (arg is RHash h)
                foreach (var kv in h.D)
                    if (kv.Key is RString dep)
                        list.Add(new Dependency(dep.V, (kv.Value is RArray arr ? arr.L : new List<object> { kv.Value })
                            .OfType<RSymbol>().Select(x => x.Name).ToList()));
        }
    }

    static RObject NewHolder(Interp it, RClass formula, FormulaSpec spec)
    {
        var holder = new RClass { Name = "Resource", Class = it.ClassClass, Super = it.ObjectClass, Host = spec };
        it.SingletonOf(holder).Super = it.SingletonOf(formula);
        return holder;
    }

    // Ruby's Date.parse accepts many formats; style (not loading) complains about non-ISO ones.
    static DateOnly? ParseDate(Interp it, object v)
    {
        if (v == null) return null;
        var s = it.ToS(v);
        if (DateOnly.TryParseExact(s, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d)) return d;
        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt)) return DateOnly.FromDateTime(dt);
        throw it.Error(it.ArgumentError, $"invalid date: {Interp.InspectStr(s)}");
    }

    static object Reason(Interp it, object v) => v switch { null => null, RSymbol s => s, _ => it.ToS(v) };

    static string Replacement(Interp it, object[] a, Func<Interp, object[], string, object> kw) =>
        (kw(it, a, "replacement_formula") ?? kw(it, a, "replacement")) is { } f ? it.ToS(f)
        : kw(it, a, "replacement_cask") is { } c ? "--cask " + it.ToS(c) : null;

    static RClass LivecheckClass(Interp it)
    {
        if (it.ConstGet(it.ObjectClass, "Livecheck") is RClass existing) return existing;
        var L = it.DefineClass("Livecheck");
        LivecheckSpec Lc(object self) => (LivecheckSpec)((RObject)self).Host;
        it.Def(L, "url", (i, self, a, b) =>
        {
            if (a[0] is RSymbol s) Lc(self).UrlSymbol = s.Name;
            else Lc(self).Url = i.ToS(a[0]);
            return null;
        });
        it.Def(L, "regex", (i, self, a, b) =>
        {
            var r = (RRegex)a[0];
            Lc(self).Regex = r.Source;
            Lc(self).RegexFlags = r.Flags;
            return null;
        });
        it.Def(L, "strategy", (i, self, a, b) =>
        {
            Lc(self).Strategy = i.StrOrSym(a[0]);
            Lc(self).StrategyBlock = b;
            return null;
        });
        it.Def(L, "skip", (i, self, a, b) => { Lc(self).Skip = a.Length > 0 ? i.ToS(a[0]) : ""; return null; });
        it.Def(L, "formula", (i, self, a, b) => { Lc(self).Formula = i.StrOrSym(a[0]); return null; });
        it.Def(L, "throttle", (i, self, a, b) => null);
        return L;
    }
}
