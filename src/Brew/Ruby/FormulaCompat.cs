using Brew.Formulae;

namespace Brew.Ruby;

// Smaller Homebrew and Ruby library pieces formulae use (wave 2/6/8 bottle triage).
public static partial class FormulaLoader
{
    static void DefineFormulaCompat(Interp it, RClass F)
    {
        // Hardware::CPU.bits (clozure-cl, wildfly-as).
        var cpu = (RClass)((RClass)it.ObjectClass.Consts["Hardware"]).Consts["CPU"];
        it.DefS(cpu, "bits", (i, s, a, b) => 64L);

        // Formula#pkg_version (gcc@9, gcc@10: "Homebrew GCC #{pkg_version}").
        it.Def(F, "pkg_version", (i, self, a, b) => Interp.S(SpecOf(((RObject)self).Class).PkgVersion));

        // Formula.names: every formula in the core tap (lit links its package for each python@x.y there).
        it.DefS(F, "names", (i, self, a, b) =>
        {
            var formulae = Path.Combine(new Paths().Taps, "homebrew-core", "Formula");
            var names = Directory.Exists(formulae)
                ? Directory.EnumerateFiles(formulae, "*.rb", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension).Distinct().Order(StringComparer.Ordinal)
                : Enumerable.Empty<string>();
            return new RArray(names.Select(n => (object)Interp.S(n)));
        });

        // File.stat / Pathname#stat / #lstat: a File::Stat with what formulae read (druid compares #ino).
        var P = it.PathnameClass;
        var stat = it.DefineClass("Stat", null, it.FileClass);
        string StatPath(object o) => (string)((RObject)o).Host;
        RObject Stat(Interp i, string path)
        {
            var full = i.Abs(path);
            if (!File.Exists(full) && !Directory.Exists(full) && new FileInfo(full).LinkTarget == null)
                throw i.Error(i.ENOENT, "No such file or directory @ rb_file_s_stat - " + path);
            return new RObject { Class = stat, Host = full };
        }
        it.DefS(it.FileClass, "stat lstat", (i, s, a, b) => Stat(i, i.PathStr(a[0])));
        it.Def(P, "stat lstat", (i, self, a, b) => Stat(i, (string)((RObject)self).Host));
        it.Def(stat, "size", (i, s, a, b) => File.Exists(StatPath(s)) ? new FileInfo(StatPath(s)).Length : 0L);
        it.Def(stat, "file?", (i, s, a, b) => File.Exists(StatPath(s)));
        it.Def(stat, "directory?", (i, s, a, b) => Directory.Exists(StatPath(s)));
        it.Def(stat, "symlink?", (i, s, a, b) => new FileInfo(StatPath(s)).LinkTarget != null);
        it.Def(stat, "mtime", (i, s, a, b) => i.Invoke(it.ObjectClass.Consts["Time"], "at",
            [(double)(File.GetLastWriteTimeUtc(StatPath(s)) - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerSecond], null));
        it.Def(stat, "mode", (i, s, a, b) => Directory.Exists(StatPath(s)) ? 0x41edL : 0x81a4L);
        // An inode stand-in: the same file always has the same number, different paths different ones.
        it.Def(stat, "ino", (i, s, a, b) => (long)(uint)StringComparer.OrdinalIgnoreCase.GetHashCode(
            Path.GetFullPath(new FileInfo(StatPath(s)).ResolveLinkTarget(true)?.FullName ?? StatPath(s))));

        // ENV.fortran (fypp, fprettify tests): Homebrew points FC/F77 at gfortran.
        var env = it.SingletonOf(it.EnvObject);
        it.Def(env, "fortran", (i, s, a, b) =>
        {
            i.Env["FC"] = "gfortran";
            i.Env["F77"] = "gfortran";
            return null;
        });

        // ERB::Util.url_encode (snakeviz's test).
        var erb = it.ObjectClass.Consts.TryGetValue("ERB", out var existing) && existing is RClass e ? e : it.DefineClass("ERB");
        var util = it.DefineModule("Util", erb);
        it.DefS(util, "url_encode u", (i, s, a, b) => Interp.S(Uri.EscapeDataString(i.ToS(a[0]))));

        // Gem::Version (npm-check-updates' test compares versions with it).
        var gem = it.ObjectClass.Consts.TryGetValue("Gem", out var g) && g is RClass gm ? gm : it.DefineModule("Gem");
        var version = it.DefineClass("Version", null, gem);
        version.Includes.Add(it.ComparableModule);
        string V(object o) => (string)((RObject)o).Host;
        string Other(Interp i, object o) => o is RObject { Host: string h } ? h : i.ToS(o);
        it.DefS(version, "new create", (i, s, a, b) => new RObject { Class = version, Host = i.ToS(a[0]).Trim() });
        it.Def(version, "to_s version", (i, s, a, b) => Interp.S(V(s)));
        it.Def(version, "<=>", (i, s, a, b) => a[0] == null ? null : (long)Versions.Compare(V(s), Other(i, a[0])));
        it.Def(version, "== eql?", (i, s, a, b) => a[0] != null && Versions.Compare(V(s), Other(i, a[0])) == 0);
        it.Def(version, "prerelease?", (i, s, a, b) => V(s).Any(char.IsLetter));
        it.Def(version, "segments", (i, s, a, b) => new RArray(System.Text.RegularExpressions.Regex.Matches(V(s), @"\d+|[A-Za-z]+")
            .Select(m => long.TryParse(m.Value, out var n) ? (object)n : Interp.S(m.Value))));
    }
}
