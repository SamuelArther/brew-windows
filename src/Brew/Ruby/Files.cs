using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

public sealed partial class Interp
{
    public RClass PathnameClass, FileClass, DirClass, FileUtilsModule, IOClass;
    public readonly Dictionary<string, string> Env = new(StringComparer.OrdinalIgnoreCase);
    public RObject EnvObject;

    // Called by Pathname#install: (destination dir, absolute source, destination name) -> handled?
    public Func<string, string, string, bool> InstallHook;

    public string Abs(string p) => Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(Cwd, p));

    public RObject NewPath(string p) => new() { Class = PathnameClass, Host = p };

    public string PathStr(object o) => o switch
    {
        RString s => s.V,
        RObject { Host: string h } => h,
        _ when o != null && RespondTo(o, "to_path") => Str(Invoke(o, "to_path", NoArgs, null)),
        _ => throw TypeErr($"no implicit conversion of {TypeName(o)} into String"),
    };

    static string JoinPath(string a, string b)
    {
        if (b.Length == 0) return a;
        if (Path.IsPathRooted(b) && (b.StartsWith('/') || b.StartsWith('\\') || (b.Length > 1 && b[1] == ':'))) return b;
        if (a.Length == 0 || a == ".") return b;
        if (b == ".") return a;
        return a.EndsWith('/') || a.EndsWith('\\') ? a + b : a + "/" + b;
    }

    static string BaseName(string p)
    {
        var t = p.TrimEnd('/', '\\');
        if (t.Length == 0) return p.Length > 0 ? "/" : "";
        int i = t.LastIndexOfAny(new[] { '/', '\\' });
        return i < 0 ? t : t[(i + 1)..];
    }

    static string DirName(string p)
    {
        var t = p.TrimEnd('/', '\\');
        int i = t.LastIndexOfAny(new[] { '/', '\\' });
        if (i < 0) return ".";
        if (i == 0) return t[..1];
        if (i == 2 && t[1] == ':') return t[..3];
        return t[..i];
    }

    static string ExtName(string p)
    {
        var b = BaseName(p);
        int i = b.LastIndexOf('.');
        return i <= 0 || i == b.Length - 1 ? "" : b[i..];
    }

    void InitFiles()
    {
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            Env[(string)e.Key] = (string)e.Value;

        IOClass = DefineClass("IO");
        FileClass = DefineClass("File", IOClass);
        DirClass = DefineClass("Dir");
        PathnameClass = DefineClass("Pathname");
        PathnameClass.Includes.Add(ComparableModule);
        FileUtilsModule = DefineModule("FileUtils");
        FileClass.Consts["SEPARATOR"] = S("/");
        FileClass.Consts["ALT_SEPARATOR"] = S("\\");
        FileClass.Consts["PATH_SEPARATOR"] = S(";");
        FileClass.Consts["FNM_PATHNAME"] = 2L;
        FileClass.Consts["FNM_DOTMATCH"] = 4L;
        FileClass.Consts["FNM_EXTGLOB"] = 16L;

        Def(KernelModule, "Pathname", (it, self, a, b) => a[0] is RObject { Class: var c } p && c == it.PathnameClass ? p : it.NewPath(it.PathStr(a[0])));

        // ---- Pathname ----
        var P = PathnameClass;
        string Me(object o) => (string)((RObject)o).Host;
        DefS(P, "new", (it, self, a, b) => it.NewPath(it.PathStr(a[0])));
        DefS(P, "pwd getwd", (it, self, a, b) => it.NewPath(it.Cwd));
        DefS(P, "glob", (it, self, a, b) => new RArray(it.Glob(it.PathStr(a[0])).Select(p => (object)it.NewPath(p))));
        Def(P, "to_s to_path to_str", (it, self, a, b) => S(Me(self)));
        Def(P, "inspect", (it, self, a, b) => S($"#<Pathname:{Me(self)}>"));
        Def(P, "+ / join", (it, self, a, b) => it.NewPath(a.Aggregate(Me(self), (acc, x) => JoinPath(acc, it.PathStr(x)))));
        Def(P, "basename", (it, self, a, b) =>
        {
            var n = BaseName(Me(self));
            if (a.Length > 0) { var ext = it.Str(a[0]); if (ext == ".*") n = Path.GetFileNameWithoutExtension(n); else if (n.EndsWith(ext)) n = n[..^ext.Length]; }
            return it.NewPath(n);
        });
        Def(P, "dirname parent", (it, self, a, b) => it.NewPath(DirName(Me(self))));
        Def(P, "extname", (it, self, a, b) => S(ExtName(Me(self))));
        Def(P, "sub_ext", (it, self, a, b) => { var p = Me(self); var e = ExtName(p); return it.NewPath((e.Length > 0 ? p[..^e.Length] : p) + it.Str(a[0])); });
        Def(P, "sub", (it, self, a, b) => it.NewPath(it.Sub(Me(self), a, b, false)));
        Def(P, "exist?", (it, self, a, b) => { var p = it.Abs(Me(self)); return File.Exists(p) || Directory.Exists(p); });
        Def(P, "file?", (it, self, a, b) => File.Exists(it.Abs(Me(self))));
        Def(P, "directory?", (it, self, a, b) => Directory.Exists(it.Abs(Me(self))));
        Def(P, "symlink?", (it, self, a, b) => IsSymlink(it.Abs(Me(self))));
        Def(P, "executable?", (it, self, a, b) => { var p = it.Abs(Me(self)); return File.Exists(p) && IsExecutableName(p) || Directory.Exists(p); });
        Def(P, "readable? writable?", (it, self, a, b) => { var p = it.Abs(Me(self)); return File.Exists(p) || Directory.Exists(p); });
        Def(P, "absolute?", (it, self, a, b) => Path.IsPathFullyQualified(Me(self)) || Me(self).StartsWith('/'));
        Def(P, "relative?", (it, self, a, b) => !(Path.IsPathFullyQualified(Me(self)) || Me(self).StartsWith('/')));
        Def(P, "root?", (it, self, a, b) => Me(self) is "/" or "\\" || Regex.IsMatch(Me(self), @"^[A-Za-z]:[\\/]?$"));
        Def(P, "zero? empty?", (it, self, a, b) =>
        {
            var p = it.Abs(Me(self));
            if (Directory.Exists(p)) return !Directory.EnumerateFileSystemEntries(p).Any();
            return File.Exists(p) && new FileInfo(p).Length == 0;
        });
        Def(P, "size", (it, self, a, b) => new FileInfo(it.Abs(Me(self))).Length);
        Def(P, "read", (it, self, a, b) => S(File.ReadAllText(it.Abs(Me(self))).Replace("\r\n", "\n")));
        Def(P, "binread", (it, self, a, b) => S(Encoding.Latin1.GetString(File.ReadAllBytes(it.Abs(Me(self))))));
        Def(P, "readlines", (it, self, a, b) => new RArray(Lines(File.ReadAllText(it.Abs(Me(self)))).Select(l => (object)S(l))));
        Def(P, "each_line", (it, self, a, b) => { foreach (var l in Lines(File.ReadAllText(it.Abs(Me(self))))) it.Y1(b, S(l)); return null; });
        Def(P, "write atomic_write binwrite", (it, self, a, b) =>
        {
            var p = it.Abs(Me(self));
            var s = it.ToS(a[0]);
            File.WriteAllText(p, s, new UTF8Encoding(false));
            return (long)Encoding.UTF8.GetByteCount(s);
        });
        Def(P, "append_lines", (it, self, a, b) =>
        {
            var p = it.Abs(Me(self));
            File.AppendAllText(p, string.Concat((a[0] is RArray arr ? arr.L : new List<object> { a[0] }).Select(x => it.ToS(x) + "\n")));
            return null;
        });
        Def(P, "children", (it, self, a, b) =>
        {
            bool withDir = a.Length == 0 || Truthy(a[0]);
            var p = Me(self);
            return new RArray(Directory.EnumerateFileSystemEntries(it.Abs(p)).Order(StringComparer.Ordinal)
                .Select(c => (object)it.NewPath(withDir ? JoinPath(p, Path.GetFileName(c)) : Path.GetFileName(c))));
        });
        Def(P, "entries", (it, self, a, b) =>
            new RArray(new[] { ".", ".." }.Concat(Directory.EnumerateFileSystemEntries(it.Abs(Me(self))).Select(Path.GetFileName)).Select(n => (object)it.NewPath(n))));
        Def(P, "each_child", (it, self, a, b) =>
        {
            var p = Me(self);
            foreach (var c in Directory.EnumerateFileSystemEntries(it.Abs(p)).Order(StringComparer.Ordinal).ToList())
                it.Y1(b, it.NewPath(JoinPath(p, Path.GetFileName(c))));
            return null;
        });
        Def(P, "glob", (it, self, a, b) =>
        {
            var p = Me(self);
            var res = it.Glob(JoinPath(p, it.PathStr(a[0]))).Select(x => (object)it.NewPath(x)).ToList();
            if (b != null) { foreach (var x in res) it.Y1(b, x); return null; }
            return new RArray(res);
        });
        Def(P, "find", (it, self, a, b) =>
        {
            var p = Me(self);
            var abs = it.Abs(p);
            var items = new List<object> { it.NewPath(p) };
            if (Directory.Exists(abs))
                items.AddRange(Directory.EnumerateFileSystemEntries(abs, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
                    .Select(x => (object)it.NewPath(JoinPath(p, Path.GetRelativePath(abs, x).Replace('\\', '/')))));
            if (b == null) return new RArray(items);
            foreach (var x in items) it.Y1(b, x);
            return null;
        });
        Def(P, "mkpath", (it, self, a, b) => { Directory.CreateDirectory(it.Abs(Me(self))); return null; });
        Def(P, "mkdir", (it, self, a, b) =>
        {
            var p = it.Abs(Me(self));
            if (Directory.Exists(p) || File.Exists(p)) throw it.Error(it.EEXIST, "File exists @ dir_s_mkdir - " + Me(self));
            Directory.CreateDirectory(p);
            return 0L;
        });
        Def(P, "rmtree", (it, self, a, b) => { it.RemoveTree(it.Abs(Me(self))); return null; });
        Def(P, "unlink delete", (it, self, a, b) =>
        {
            var p = it.Abs(Me(self));
            if (Directory.Exists(p) && !IsSymlink(p)) Directory.Delete(p);
            else if (File.Exists(p) || IsSymlink(p)) File.Delete(p);
            else throw it.Error(it.ENOENT, "No such file or directory @ apply2files - " + Me(self));
            return 1L;
        });
        Def(P, "rmdir", (it, self, a, b) => { Directory.Delete(it.Abs(Me(self))); return 0L; });
        Def(P, "rename", (it, self, a, b) => { it.MovePath(it.Abs(Me(self)), it.Abs(it.PathStr(a[0]))); return 0L; });
        Def(P, "make_symlink", (it, self, a, b) => { it.MakeSymlink(it.PathStr(a[0]), it.Abs(Me(self))); return 0L; });
        Def(P, "make_relative_symlink", (it, self, a, b) =>
        {
            var link = it.Abs(Me(self));
            var target = Path.GetRelativePath(Path.GetDirectoryName(link)!, it.Abs(it.PathStr(a[0])));
            it.MakeSymlink(target, link);
            return null;
        });
        Def(P, "readlink", (it, self, a, b) => it.NewPath(new FileInfo(it.Abs(Me(self))).LinkTarget ?? throw it.Error(it.EEXIST, "not a symlink")));
        Def(P, "realpath", (it, self, a, b) =>
        {
            var p = it.Abs(Me(self));
            var fi = new FileInfo(p);
            var target = fi.Exists || Directory.Exists(p) ? (fi.ResolveLinkTarget(true)?.FullName ?? p) : throw it.Error(it.ENOENT, "No such file or directory @ realpath_rec - " + p);
            return it.NewPath(target);
        });
        Def(P, "expand_path", (it, self, a, b) => it.NewPath(it.Abs(Me(self))));
        Def(P, "cleanpath", (it, self, a, b) => it.NewPath(CleanPath(Me(self))));
        Def(P, "relative_path_from", (it, self, a, b) => it.NewPath(Path.GetRelativePath(it.Abs(it.PathStr(a[0])), it.Abs(Me(self))).Replace('\\', '/')));
        Def(P, "each_filename", (it, self, a, b) => { foreach (var part in Me(self).Split('/', '\\', StringSplitOptions.RemoveEmptyEntries)) it.Y1(b, S(part)); return null; });
        Def(P, "chmod", (it, self, a, b) => 1L);
        Def(P, "== eql? ===", (it, self, a, b) => a[0] is RObject { Host: string h } o && o.Class == it.PathnameClass && h == Me(self));
        Def(P, "<=>", (it, self, a, b) => a[0] is RObject { Host: string h } ? (long)Math.Sign(string.CompareOrdinal(Me(self), h)) : null);
        Def(P, "hash", (it, self, a, b) => (long)Me(self).GetHashCode());
        Def(P, "install", (it, self, a, b) =>
        {
            foreach (var x in a) it.InstallInto(Me(self), x);
            return null;
        });
        Def(P, "install_symlink", (it, self, a, b) =>
        {
            var dir = it.Abs(Me(self));
            Directory.CreateDirectory(dir);
            foreach (var x in a)
            {
                if (x is RHash h)
                {
                    foreach (var kv in h.D) it.MakeSymlink(it.Abs(it.PathStr(kv.Key)), Path.Combine(dir, it.PathStr(kv.Value)));
                }
                else foreach (var src in x is RArray arr ? arr.L : new List<object> { x })
                    it.MakeSymlink(it.Abs(it.PathStr(src)), Path.Combine(dir, BaseName(it.PathStr(src))));
            }
            return null;
        });
        Def(P, "open", (it, self, a, b) => it.Invoke(it.FileClass, "open", a.Prepend(self).ToArray(), b));

        // ---- File ----
        var F = FileClass;
        DefS(F, "join", (it, self, a, b) => S(it.Flatten(a, -1).Select(it.PathStr).Aggregate("", (acc, x) => acc.Length == 0 ? x : acc.TrimEnd('/') + "/" + x.TrimStart('/'))));
        DefS(F, "exist?", (it, self, a, b) => { var p = it.Abs(it.PathStr(a[0])); return File.Exists(p) || Directory.Exists(p); });
        DefS(F, "file?", (it, self, a, b) => File.Exists(it.Abs(it.PathStr(a[0]))));
        DefS(F, "directory?", (it, self, a, b) => Directory.Exists(it.Abs(it.PathStr(a[0]))));
        DefS(F, "symlink?", (it, self, a, b) => IsSymlink(it.Abs(it.PathStr(a[0]))));
        DefS(F, "executable?", (it, self, a, b) => { var p = it.Abs(it.PathStr(a[0])); return File.Exists(p) && IsExecutableName(p); });
        DefS(F, "readable? writable?", (it, self, a, b) => File.Exists(it.Abs(it.PathStr(a[0]))));
        DefS(F, "zero? empty?", (it, self, a, b) => { var p = it.Abs(it.PathStr(a[0])); return File.Exists(p) && new FileInfo(p).Length == 0; });
        DefS(F, "size", (it, self, a, b) => new FileInfo(it.Abs(it.PathStr(a[0]))).Length);
        DefS(F, "read", (it, self, a, b) => S(File.ReadAllText(it.Abs(it.PathStr(a[0]))).Replace("\r\n", "\n")));
        DefS(F, "binread", (it, self, a, b) => S(Encoding.Latin1.GetString(File.ReadAllBytes(it.Abs(it.PathStr(a[0]))))));
        DefS(F, "readlines", (it, self, a, b) => new RArray(Lines(File.ReadAllText(it.Abs(it.PathStr(a[0])))).Select(l => (object)S(it.Truthy2(a, "chomp") ? l.TrimEnd('\n') : l))));
        DefS(F, "foreach", (it, self, a, b) => { foreach (var l in Lines(File.ReadAllText(it.Abs(it.PathStr(a[0]))))) it.Y1(b, S(l)); return null; });
        DefS(F, "write binwrite", (it, self, a, b) =>
        {
            var s = it.ToS(a[1]);
            File.WriteAllText(it.Abs(it.PathStr(a[0])), s, new UTF8Encoding(false));
            return (long)Encoding.UTF8.GetByteCount(s);
        });
        DefS(F, "basename", (it, self, a, b) =>
        {
            var n = BaseName(it.PathStr(a[0]));
            if (a.Length > 1) { var ext = it.Str(a[1]); if (ext == ".*") n = Path.GetFileNameWithoutExtension(n); else if (n.EndsWith(ext)) n = n[..^ext.Length]; }
            return S(n);
        });
        DefS(F, "dirname", (it, self, a, b) => S(DirName(it.PathStr(a[0]))));
        DefS(F, "extname", (it, self, a, b) => S(ExtName(it.PathStr(a[0]))));
        DefS(F, "expand_path absolute_path", (it, self, a, b) =>
        {
            var p = it.PathStr(a[0]);
            if (p.StartsWith('~')) p = it.Env.GetValueOrDefault("USERPROFILE", "") + p[1..];
            var baseDir = a.Length > 1 && a[1] != null ? it.Abs(it.PathStr(a[1])) : it.Cwd;
            return S(Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(baseDir, p)).Replace('\\', '/'));
        });
        DefS(F, "realpath", (it, self, a, b) => S(it.Abs(it.PathStr(a[0])).Replace('\\', '/')));
        DefS(F, "delete unlink", (it, self, a, b) => { foreach (var x in a) File.Delete(it.Abs(it.PathStr(x))); return (long)a.Length; });
        DefS(F, "rename", (it, self, a, b) => { it.MovePath(it.Abs(it.PathStr(a[0])), it.Abs(it.PathStr(a[1]))); return 0L; });
        DefS(F, "symlink", (it, self, a, b) => { it.MakeSymlink(it.PathStr(a[0]), it.Abs(it.PathStr(a[1]))); return 0L; });
        DefS(F, "readlink", (it, self, a, b) => S(new FileInfo(it.Abs(it.PathStr(a[0]))).LinkTarget ?? ""));
        DefS(F, "chmod", (it, self, a, b) => (long)(a.Length - 1));
        DefS(F, "fnmatch fnmatch?", (it, self, a, b) => GlobRegex(it.Str(a[0]), true).IsMatch(it.PathStr(a[1])));
        DefS(F, "open new", (it, self, a, b) =>
        {
            var path = it.Abs(it.PathStr(a[0]));
            var mode = a.Length > 1 && a[1] is RString m ? m.V : "r";
            var f = new RObject { Class = it.FileClass, Host = new FileHandle { Path = path, Mode = mode } };
            if (mode.StartsWith('r')) ((FileHandle)f.Host).Buffer.Append(File.ReadAllText(path).Replace("\r\n", "\n"));
            if (b == null) return f;
            try { return it.Y1(b, f); }
            finally { it.Invoke(f, "close", NoArgs, null); }
        });
        FileHandle FH(object o) => (FileHandle)((RObject)o).Host;
        Def(F, "write print <<", (it, self, a, b) => { foreach (var x in a) FH(self).Buffer.Append(it.ToS(x)); return self; });
        Def(F, "puts", (it, self, a, b) => { var sw = new StringWriter(); it.Puts(sw, a); FH(self).Buffer.Append(sw.ToString()); return null; });
        Def(F, "read", (it, self, a, b) => { var s = FH(self).Buffer.ToString(FH(self).Pos, FH(self).Buffer.Length - FH(self).Pos); FH(self).Pos = FH(self).Buffer.Length; return S(s); });
        Def(F, "each_line", (it, self, a, b) => { foreach (var l in Lines(FH(self).Buffer.ToString())) it.Y1(b, S(l)); return self; });
        Def(F, "readlines", (it, self, a, b) => new RArray(Lines(FH(self).Buffer.ToString()).Select(l => (object)S(l))));
        Def(F, "path to_path", (it, self, a, b) => S(FH(self).Path));
        Def(F, "close", (it, self, a, b) =>
        {
            var h = FH(self);
            if (h.Closed) return null;
            h.Closed = true;
            if (h.Mode.StartsWith('w')) File.WriteAllText(h.Path, h.Buffer.ToString(), new UTF8Encoding(false));
            else if (h.Mode.StartsWith('a')) File.AppendAllText(h.Path, h.Buffer.ToString(), new UTF8Encoding(false));
            return null;
        });

        // ---- Dir ----
        var D = DirClass;
        DefS(D, "glob []", (it, self, a, b) =>
        {
            var pats = a.Where(x => x is not RHash).SelectMany(x => x is RArray arr ? arr.L : new List<object> { x }).Select(it.PathStr).ToList();
            string baseDir = a.LastOrDefault() is RHash h && h.D.TryGetValue(Sym("base"), out var bd) ? it.PathStr(bd) : null;
            var res = pats.SelectMany(p => it.Glob(p, baseDir)).Select(x => (object)S(x)).ToList();
            if (b != null) { foreach (var x in res) it.Y1(b, x); return null; }
            return new RArray(res);
        });
        DefS(D, "pwd getwd", (it, self, a, b) => S(it.Cwd.Replace('\\', '/')));
        DefS(D, "chdir", (it, self, a, b) =>
        {
            var target = it.Abs(a.Length > 0 ? it.PathStr(a[0]) : it.Env.GetValueOrDefault("USERPROFILE", it.Cwd));
            if (!Directory.Exists(target)) throw it.Error(it.ENOENT, "No such file or directory @ dir_s_chdir - " + target);
            if (b == null) { it.Cwd = target; return 0L; }
            var old = it.Cwd;
            it.Cwd = target;
            try { return it.Y1(b, S(target)); }
            finally { it.Cwd = old; }
        });
        DefS(D, "exist?", (it, self, a, b) => Directory.Exists(it.Abs(it.PathStr(a[0]))));
        DefS(D, "empty?", (it, self, a, b) => { var p = it.Abs(it.PathStr(a[0])); return Directory.Exists(p) && !Directory.EnumerateFileSystemEntries(p).Any(); });
        DefS(D, "mkdir", (it, self, a, b) => { Directory.CreateDirectory(it.Abs(it.PathStr(a[0]))); return 0L; });
        DefS(D, "rmdir delete unlink", (it, self, a, b) => { Directory.Delete(it.Abs(it.PathStr(a[0]))); return 0L; });
        DefS(D, "children", (it, self, a, b) => new RArray(Directory.EnumerateFileSystemEntries(it.Abs(it.PathStr(a[0]))).Select(x => (object)S(Path.GetFileName(x)))));
        DefS(D, "entries", (it, self, a, b) => new RArray(new[] { ".", ".." }.Concat(Directory.EnumerateFileSystemEntries(it.Abs(it.PathStr(a[0]))).Select(Path.GetFileName)).Select(x => (object)S(x))));
        DefS(D, "each_child", (it, self, a, b) => { foreach (var x in Directory.EnumerateFileSystemEntries(it.Abs(it.PathStr(a[0]))).ToList()) it.Y1(b, S(Path.GetFileName(x))); return null; });
        DefS(D, "home", (it, self, a, b) => S(it.Env.GetValueOrDefault("USERPROFILE", "").Replace('\\', '/')));
        DefS(D, "tmpdir", (it, self, a, b) => S(Path.GetTempPath().TrimEnd('\\').Replace('\\', '/')));
        DefS(D, "mktmpdir", (it, self, a, b) =>
        {
            var dir = Path.Combine(Path.GetTempPath(), (a.Length > 0 && a[0] is RString pre ? pre.V : "d") + Guid.NewGuid().ToString("N")[..12]);
            Directory.CreateDirectory(dir);
            if (b == null) return S(dir);
            try { return it.Y1(b, S(dir)); }
            finally { it.RemoveTree(dir); }
        });

        // ---- FileUtils ----
        var FU = FileUtilsModule;
        IEnumerable<string> Paths(Interp it, object x) => (x is RArray arr ? arr.L : new List<object> { x }).Select(p => it.Abs(it.PathStr(p)));
        DefS(FU, "mkdir_p makedirs mkpath", (it, self, a, b) => { foreach (var p in Paths(it, a[0])) Directory.CreateDirectory(p); return a[0]; });
        DefS(FU, "mkdir", (it, self, a, b) => { foreach (var p in Paths(it, a[0])) Directory.CreateDirectory(p); return a[0]; });
        DefS(FU, "rm remove", (it, self, a, b) =>
        {
            foreach (var p in Paths(it, a[0]))
            {
                if (!File.Exists(p) && !IsSymlink(p)) throw it.Error(it.ENOENT, "No such file or directory @ apply2files - " + p);
                File.Delete(p);
            }
            return a[0];
        });
        DefS(FU, "rm_f", (it, self, a, b) => { foreach (var p in Paths(it, a[0])) if (File.Exists(p) || IsSymlink(p)) File.Delete(p); return a[0]; });
        DefS(FU, "rm_r rm_rf remove_dir rmtree remove_entry", (it, self, a, b) => { foreach (var p in Paths(it, a[0])) it.RemoveTree(p); return a[0]; });
        DefS(FU, "rmdir", (it, self, a, b) => { foreach (var p in Paths(it, a[0])) Directory.Delete(p); return a[0]; });
        DefS(FU, "cp copy install", (it, self, a, b) =>
        {
            var dest = it.Abs(it.PathStr(a[1]));
            foreach (var p in Paths(it, a[0]))
                File.Copy(p, Directory.Exists(dest) ? Path.Combine(dest, Path.GetFileName(p)) : dest, true);
            return null;
        });
        DefS(FU, "cp_r", (it, self, a, b) =>
        {
            var dest = it.Abs(it.PathStr(a[1]));
            foreach (var p in Paths(it, a[0]))
            {
                var target = Directory.Exists(dest) ? Path.Combine(dest, Path.GetFileName(p)) : dest;
                if (Directory.Exists(p)) CopyDir(p, target);
                else File.Copy(p, target, true);
            }
            return null;
        });
        DefS(FU, "mv move", (it, self, a, b) =>
        {
            var dest = it.Abs(it.PathStr(a[1]));
            foreach (var p in Paths(it, a[0]))
                it.MovePath(p, Directory.Exists(dest) ? Path.Combine(dest, Path.GetFileName(p)) : dest);
            return 0L;
        });
        DefS(FU, "ln_s ln_sf ln", (it, self, a, b) =>
        {
            var dest = it.Abs(it.PathStr(a[1]));
            foreach (var x in a[0] is RArray arr ? arr.L : new List<object> { a[0] })
            {
                var src = it.PathStr(x);
                var link = Directory.Exists(dest) && !IsSymlink(dest) ? Path.Combine(dest, BaseName(src)) : dest;
                if (File.Exists(link) || IsSymlink(link)) File.Delete(link);
                it.MakeSymlink(src, link);
            }
            return 0L;
        });
        DefS(FU, "touch", (it, self, a, b) =>
        {
            foreach (var p in Paths(it, a[0]))
            {
                if (File.Exists(p)) File.SetLastWriteTimeUtc(p, DateTime.UtcNow);
                else File.WriteAllBytes(p, Array.Empty<byte>());
            }
            return a[0];
        });
        DefS(FU, "chmod chmod_R chown chown_R", (it, self, a, b) => null);
        DefS(FU, "pwd", (it, self, a, b) => S(it.Cwd));
        DefS(FU, "cd chdir", (it, self, a, b) => it.Invoke(it.DirClass, "chdir", new[] { a[0] }, b));
        DefS(FU, "compare_file identical? cmp", (it, self, a, b) =>
            File.ReadAllBytes(it.Abs(it.PathStr(a[0]))).AsSpan().SequenceEqual(File.ReadAllBytes(it.Abs(it.PathStr(a[1])))));
        // `include FileUtils` gets the same functions as instance methods
        foreach (var kv in SingletonOf(FU).Methods) FU.Methods[kv.Key] = kv.Value;

        // ---- ENV ----
        EnvObject = new RObject { Class = ObjectClass };
        ObjectClass.Consts["ENV"] = EnvObject;
        var E = SingletonOf(EnvObject);
        Def(E, "[]", (it, self, a, b) => it.Env.TryGetValue(it.Str(a[0]), out var v) ? S(v) : null);
        Def(E, "[]= store", (it, self, a, b) =>
        {
            var k = it.Str(a[0]);
            if (a[1] == null) it.Env.Remove(k);
            else it.Env[k] = it.ToS(a[1]);
            return a[1];
        });
        Def(E, "fetch", (it, self, a, b) =>
        {
            if (it.Env.TryGetValue(it.Str(a[0]), out var v)) return S(v);
            if (b != null) return it.Y1(b, a[0]);
            if (a.Length > 1) return a[1];
            throw it.Error(it.KeyError, $"key not found: {it.Inspect(a[0])}");
        });
        Def(E, "key? has_key? include? member?", (it, self, a, b) => it.Env.ContainsKey(it.Str(a[0])));
        Def(E, "delete", (it, self, a, b) => it.Env.Remove(it.Str(a[0]), out var v) ? S(v) : null);
        Def(E, "to_h to_hash", (it, self, a, b) => { var h = new RHash(); foreach (var kv in it.Env) h.D[S(kv.Key)] = S(kv.Value); return h; });
        Def(E, "keys", (it, self, a, b) => new RArray(it.Env.Keys.Select(k => (object)S(k))));
        Def(E, "each each_pair", (it, self, a, b) => { foreach (var kv in it.Env.ToList()) it.Y1(b, Pair(S(kv.Key), S(kv.Value))); return self; });
        Def(E, "inspect to_s", (it, self, a, b) => S("ENV"));
        Def(E, "prepend_path", (it, self, a, b) => { it.EnvJoin(it.Str(a[0]), it.PathStr(a[1]), ";", true); return null; });
        Def(E, "append_path", (it, self, a, b) => { it.EnvJoin(it.Str(a[0]), it.PathStr(a[1]), ";", false); return null; });
        Def(E, "prepend", (it, self, a, b) => { it.EnvJoin(it.Str(a[0]), it.ToS(a[1]), a.Length > 2 ? it.Str(a[2]) : " ", true); return null; });
        Def(E, "append", (it, self, a, b) => { it.EnvJoin(it.Str(a[0]), it.ToS(a[1]), a.Length > 2 ? it.Str(a[2]) : " ", false); return null; });
        Def(E, "deparallelize permit_arch_flags runtime_cpu_detection cxx11 libcxx O0 O1 O2 O3 Os refurbish_args", (it, self, a, b) => null);
    }

    bool Truthy2(object[] a, string kw) => a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Sym(kw), out var v) && Truthy(v);

    void EnvJoin(string key, string value, string sep, bool prepend)
    {
        if (!Env.TryGetValue(key, out var cur) || cur.Length == 0) { Env[key] = value; return; }
        Env[key] = prepend ? value + sep + cur : cur + sep + value;
    }

    sealed class FileHandle
    {
        public string Path, Mode;
        public readonly StringBuilder Buffer = new();
        public int Pos;
        public bool Closed;
    }

    static bool IsSymlink(string p)
    {
        try { return new FileInfo(p).LinkTarget != null || new DirectoryInfo(p).LinkTarget != null; }
        catch (IOException) { return false; }
    }

    static bool IsExecutableName(string p) =>
        Path.GetExtension(p).ToLowerInvariant() is ".exe" or ".cmd" or ".bat" or ".com" or ".ps1";

    static string CleanPath(string p)
    {
        var rooted = p.StartsWith('/') ? "/" : Regex.Match(p, @"^[A-Za-z]:[\\/]").Value;
        var parts = new List<string>();
        foreach (var part in p[rooted.Length..].Split('/', '\\'))
        {
            if (part is "" or ".") continue;
            if (part == ".." && parts.Count > 0 && parts[^1] != "..") parts.RemoveAt(parts.Count - 1);
            else if (part == ".." && rooted.Length > 0) continue;
            else parts.Add(part);
        }
        var r = rooted + string.Join("/", parts);
        return r.Length == 0 ? "." : r;
    }

    public void RemoveTree(string p)
    {
        if (IsSymlink(p)) { if (Directory.Exists(p)) Directory.Delete(p); else File.Delete(p); return; }
        if (Directory.Exists(p))
        {
            foreach (var f in new DirectoryInfo(p).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                if (f is FileInfo fi && fi.IsReadOnly) fi.IsReadOnly = false;
            Directory.Delete(p, true);
        }
        else if (File.Exists(p)) File.Delete(p);
    }

    void MovePath(string from, string to)
    {
        if (Directory.Exists(from) && !IsSymlink(from))
        {
            if (Directory.Exists(to)) RemoveTree(to);
            Directory.Move(from, to);
        }
        else if (File.Exists(from) || IsSymlink(from)) File.Move(from, to, true);
        else throw Error(ENOENT, $"No such file or directory @ rb_file_s_rename - ({from}, {to})");
    }

    static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.EnumerateFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
        foreach (var d in Directory.EnumerateDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
    }

    void MakeSymlink(string target, string link)
    {
        var dir = Path.GetDirectoryName(link);
        if (dir != null) Directory.CreateDirectory(dir);
        var resolved = Path.IsPathRooted(target) ? target : Path.Combine(dir ?? Cwd, target);
        if (Directory.Exists(resolved)) Directory.CreateSymbolicLink(link, target);
        else File.CreateSymbolicLink(link, target);
    }

    // Homebrew's Pathname#install: moves sources (relative to cwd) into dir.
    void InstallInto(string dir, object src)
    {
        switch (src)
        {
            case RArray arr:
                foreach (var x in arr.L) InstallInto(dir, x);
                return;
            case RHash h:
                foreach (var kv in h.D) InstallOne(dir, PathStr(kv.Key), PathStr(kv.Value));
                return;
            default:
                var s = PathStr(src);
                InstallOne(dir, s, BaseName(s));
                return;
        }
    }

    void InstallOne(string dir, string src, string newName)
    {
        var absSrc = Abs(src);
        if (!File.Exists(absSrc) && !Directory.Exists(absSrc) && !IsSymlink(absSrc))
            throw Error(ENOENT, $"No such file or directory - {src}");
        var absDir = Abs(dir);
        if (InstallHook != null && InstallHook(absDir, absSrc, newName)) return;
        Directory.CreateDirectory(absDir);
        var dest = Path.Combine(absDir, newName);
        if (File.Exists(dest) || IsSymlink(dest)) File.Delete(dest);
        MovePath(absSrc, dest);
    }

    // ---- glob ----

    public List<string> Glob(string pattern, string baseDir = null)
    {
        var results = new List<string>();
        foreach (var pat in ExpandBraces(pattern.Replace('\\', '/')))
        {
            string root;
            string rest;
            var m = Regex.Match(pat, @"^([A-Za-z]:/|/)");
            if (m.Success) { root = m.Value; rest = pat[m.Length..]; }
            else { root = ""; rest = pat; }
            var segs = rest.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
            var start = root.Length > 0 ? root : baseDir != null ? Abs(baseDir) : Cwd;
            GlobWalk(start, root, segs, 0, results);
        }
        return results.Distinct().ToList();
    }

    void GlobWalk(string absDir, string prefix, List<string> segs, int i, List<string> results)
    {
        if (i == segs.Count) return;
        var seg = segs[i];
        bool last = i == segs.Count - 1;
        string Join(string pre, string name) => pre.Length == 0 || pre.EndsWith('/') ? pre + name : pre + "/" + name;
        if (seg == "**")
        {
            GlobWalk(absDir, prefix, segs, i + 1, results);
            if (!Directory.Exists(absDir)) return;
            foreach (var d in Directory.EnumerateDirectories(absDir).Order(StringComparer.Ordinal))
            {
                var name = Path.GetFileName(d);
                if (name.StartsWith('.')) continue;
                GlobWalk(d, Join(prefix, name), segs, i, results);
            }
            return;
        }
        if (!seg.Any(c => c is '*' or '?' or '['))
        {
            var p = Path.Combine(absDir, seg);
            if (last) { if (File.Exists(p) || Directory.Exists(p)) results.Add(Join(prefix, seg)); }
            else if (Directory.Exists(p)) GlobWalk(p, Join(prefix, seg), segs, i + 1, results);
            return;
        }
        if (!Directory.Exists(absDir)) return;
        var re = GlobRegex(seg, false);
        foreach (var e in Directory.EnumerateFileSystemEntries(absDir).Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(e);
            if (name.StartsWith('.') && !seg.StartsWith('.')) continue;
            if (!re.IsMatch(name)) continue;
            if (last) results.Add(Join(prefix, name));
            else if (Directory.Exists(e)) GlobWalk(e, Join(prefix, name), segs, i + 1, results);
        }
    }

    static Regex GlobRegex(string pat, bool pathMode)
    {
        var sb = new StringBuilder("^");
        for (int i = 0; i < pat.Length; i++)
        {
            char c = pat[i];
            switch (c)
            {
                case '*':
                    if (i + 1 < pat.Length && pat[i + 1] == '*') { sb.Append(".*"); i++; if (i + 1 < pat.Length && pat[i + 1] == '/') i++; }
                    else sb.Append(pathMode ? "[^/]*" : ".*");
                    break;
                case '?': sb.Append('.'); break;
                case '[':
                {
                    int e = pat.IndexOf(']', i + 1);
                    if (e < 0) { sb.Append("\\["); break; }
                    var cls = pat[(i + 1)..e];
                    if (cls.StartsWith('!')) cls = "^" + cls[1..];
                    sb.Append('[').Append(cls.Replace("\\", "\\\\")).Append(']');
                    i = e;
                    break;
                }
                default: sb.Append(Regex.Escape(c.ToString())); break;
            }
        }
        return new Regex(sb.Append('$').ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    static IEnumerable<string> ExpandBraces(string pat)
    {
        int open = pat.IndexOf('{');
        if (open < 0) { yield return pat; yield break; }
        int depth = 0, close = -1;
        for (int i = open; i < pat.Length; i++)
        {
            if (pat[i] == '{') depth++;
            else if (pat[i] == '}' && --depth == 0) { close = i; break; }
        }
        if (close < 0) { yield return pat; yield break; }
        var inner = pat[(open + 1)..close];
        var options = new List<string>();
        int d = 0, s = 0;
        for (int i = 0; i < inner.Length; i++)
        {
            if (inner[i] == '{') d++;
            else if (inner[i] == '}') d--;
            else if (inner[i] == ',' && d == 0) { options.Add(inner[s..i]); s = i + 1; }
        }
        options.Add(inner[s..]);
        foreach (var o in options)
            foreach (var r in ExpandBraces(pat[..open] + o + pat[(close + 1)..]))
                yield return r;
    }
}
