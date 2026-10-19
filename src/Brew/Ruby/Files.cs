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
    // Directories whose installs follow InstallRenamed (HOMEBREW_PREFIX/etc).
    public List<string> InstallRenamedRoots = new();

    public string Abs(string p) => Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(Cwd, p));

    // Ruby on Windows spells paths with "/" (Dir.pwd, File.expand_path): formulae splice them into
    // source code, configs and regexp replacements where a backslash is an escape.
    public RObject NewPath(string p) => new() { Class = PathnameClass, Host = p?.Replace('\\', '/') };

    // bin/"tool" on Windows means tool.exe / tool.cmd: the existing PATHEXT variant, if any.
    public string WithExe(string p)
    {
        if (Path.HasExtension(p) && !p.EndsWith('.')) return null;
        foreach (var e in Env.GetValueOrDefault("PATHEXT", ".COM;.EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries))
            if (File.Exists(p + e)) return p + e;
        return null;
    }

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
        // Always "/": tests rely on (testpath/"x").to_s == "#{testpath}/x", and Windows accepts mixed separators.
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
        IOClass.Includes.Add(EnumerableModule);   // io.any? { |line| ... } (cloudprober's test), over #each
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
        Def(P, "exist?", (it, self, a, b) => { var p = it.Abs(Me(self)); return File.Exists(p) || Directory.Exists(p) || it.WithExe(p) != null; });
        Def(P, "file?", (it, self, a, b) => File.Exists(it.Abs(Me(self))) || it.WithExe(it.Abs(Me(self))) != null);
        Def(P, "directory?", (it, self, a, b) => Directory.Exists(it.Abs(Me(self))));
        Def(P, "symlink?", (it, self, a, b) => IsSymlink(it.Abs(Me(self))));
        Def(P, "executable?", (it, self, a, b) => { var p = it.Abs(Me(self)); return File.Exists(p) && IsExecutableName(p) || Directory.Exists(p) || it.WithExe(p) != null; });
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
        // bin/"tool" is tool.exe here too, as for exist? (sigsum-go's test signs (bin/"sigsum-key").read).
        Def(P, "size", (it, self, a, b) => new FileInfo(BuiltExecutable(it.Abs(Me(self)))).Length);
        Def(P, "size?", (it, self, a, b) => SizeOrNil(BuiltExecutable(it.Abs(Me(self)))));
        Def(P, "read", (it, self, a, b) => S(ReadText(BuiltExecutable(it.Abs(Me(self))))));
        Def(P, "binread", (it, self, a, b) => BinRead(it, BuiltExecutable(it.Abs(Me(self))), a));
        Def(P, "readlines", (it, self, a, b) => new RArray(Lines(ReadText(it.Abs(Me(self)))).Select(l => (object)S(it.Truthy2(a, "chomp") ? l.TrimEnd('\n') : l))));
        Def(P, "each_line", (it, self, a, b) => { foreach (var l in Lines(ReadText(it.Abs(Me(self))))) it.Y1(b, S(l)); return null; });
        // Homebrew's WriteMkpathExtension: refuses to overwrite, creates parent directories.
        Def(P, "write binwrite", (it, self, a, b) =>
        {
            var p = it.Abs(Me(self));
            var s = it.ToS(a[0]);
            bool hasOffset = a.Length > 1 && a[1] is long;
            var mode = a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Sym("mode"), out var m) && m is RString ms ? ms.V : null;
            bool append = mode != null && System.Text.RegularExpressions.Regex.IsMatch(mode, "^a\\+?$");
            if ((File.Exists(p) || Directory.Exists(p)) && !hasOffset && !append)
                throw it.Error(it.RuntimeError, $"Will not overwrite {Me(self)}");
            var dir = Path.GetDirectoryName(p);
            if (dir != null) Directory.CreateDirectory(dir);
            var data = Bytes(a[0], s);
            using (var f = new FileStream(p, append ? FileMode.Append : FileMode.Create)) f.Write(data);
            return (long)data.Length;
        });
        Def(P, "atomic_write", (it, self, a, b) =>
        {
            var p = it.Abs(Me(self));
            var s = it.ToS(a[0]);
            var dir = Path.GetDirectoryName(p);
            if (dir != null) Directory.CreateDirectory(dir);
            var tmp = p + ".tmp" + Guid.NewGuid().ToString("N")[..8];
            File.WriteAllText(tmp, s, new UTF8Encoding(false));
            File.Move(tmp, p, true);
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
        // each_child(with_directory = true): children(with_directory).each, so each_child(false) yields bare names
        // (ethereum, zsync: `(buildpath/"cmd").each_child(false) { |cmd| ... "./cmd/#{cmd}" }`).
        Def(P, "each_child", (it, self, a, b) =>
        {
            var children = (RArray)it.Invoke(self, "children", a, null);
            if (b == null) return it.Invoke(children, "each", Array.Empty<object>(), null);
            foreach (var c in children.L) it.Y1(b, c);
            return children;
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
            // Ruby's Find: depth first, sorted, and a symlinked directory is listed but not entered.
            void Walk(string dir, string shown)
            {
                foreach (var x in Directory.EnumerateFileSystemEntries(dir).Order(StringComparer.Ordinal).ToList())
                {
                    var child = JoinPath(shown, Path.GetFileName(x));
                    items.Add(it.NewPath(child));
                    if (Directory.Exists(x) && !IsSymlink(x)) Walk(x, child);
                }
            }
            if (Directory.Exists(abs) && !IsSymlink(abs)) Walk(abs, p);
            if (b == null) return new RArray(items);
            foreach (var x in items) it.Y1(b, x);
            return null;
        });
        // Ruby 3.1+ returns self (sbtenv: prefix.install_symlink (var/"lib/sbtenv/plugins").mkpath).
        Def(P, "mkpath", (it, self, a, b) => { Directory.CreateDirectory(it.Abs(Me(self))); return self; });
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
            // (bin/"wxrc").unlink removes the built wxrc.exe, as File.exist? finds it.
            var p = BuiltExecutable(it.Abs(Me(self)));
            if (Directory.Exists(p) && !IsSymlink(p)) Directory.Delete(p);
            else if (File.Exists(p) || IsSymlink(p)) File.Delete(p);
            else if (it.DropPendingCommand?.Invoke(p) == true) { }   // a windows_exe command not yet written to bin
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
        Def(P, "chmod", (it, self, a, b) => { Chmod(it.Abs(Me(self)), it.Int(a[0]), false); return 1L; });
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
            // install_symlink_p: a relative source is relative to the directory the link goes in (`bin.install_symlink
            // "lv" => "lgrep"` is bin/lgrep -> lv), and the link is made relative so it survives bottling.
            void Link(string source, string name)
            {
                var target = Path.GetFullPath(source.Replace('/', Path.DirectorySeparatorChar), dir);
                if (!File.Exists(target) && !Directory.Exists(target) && !IsSymlink(target) && File.Exists(target + ".exe"))
                {
                    target += ".exe";   // built executables carry .exe; so does the link, or Windows can't run it
                    if (!Path.HasExtension(name)) name += ".exe";
                }
                // A command with a .cmd launcher beside it (npm's bin/<name>.cmd, env scripts): the link needs one too,
                // or Windows can't run it (`bin.install_symlink libexec/"bin/bpmnlint"`).
                var cmd = target + ".cmd";
                if (!Path.HasExtension(target) && !Path.HasExtension(name) && File.Exists(cmd))
                {
                    var cmdLink = Path.Combine(dir, name + ".cmd");
                    if (File.Exists(cmdLink) || IsSymlink(cmdLink)) File.Delete(cmdLink);
                    it.MakeSymlink(Path.GetRelativePath(dir, cmd), cmdLink);
                    if (!File.Exists(target) && !IsSymlink(target)) return;
                }
                var link = Path.Combine(dir, name);
                if (File.Exists(link) || IsSymlink(link)) { if (Directory.Exists(link)) Directory.Delete(link); else File.Delete(link); }
                it.MakeSymlink(Path.GetRelativePath(Path.GetDirectoryName(link)!, target), link);
            }
            foreach (var x in a)
            {
                if (x is RHash h)
                {
                    foreach (var kv in h.D) Link(it.PathStr(kv.Key), it.PathStr(kv.Value));
                }
                else foreach (var src in x is RArray arr ? arr.L : new List<object> { x })
                    Link(it.PathStr(src), BaseName(it.PathStr(src)));
            }
            return null;
        });
        Def(P, "open", (it, self, a, b) => it.Invoke(it.FileClass, "open", a.Prepend(self).ToArray(), b));
        // Kernel#open with a path is File.open (openssl's test: open("checksum.txt") { |f| f.read(100) }).
        Def(KernelModule, "open", (it, self, a, b) =>
        {
            if (a.Length > 0 && a[0] is RString { V: ['|', ..] }) throw it.Error(it.ArgumentError, "open(\"|command\") is not supported; use IO.popen");
            return it.Invoke(it.FileClass, "open", a, b);
        });

        // ---- File ----
        var F = FileClass;
        DefS(F, "join", (it, self, a, b) => S(it.Flatten(a, -1).Select(it.PathStr).Aggregate("", (acc, x) => acc.Length == 0 ? x : acc.TrimEnd('/') + "/" + x.TrimStart('/'))));
        // "#{bin}/tool" is a built tool.exe (Pathname's predicates take any PATHEXT variant).
        DefS(F, "exist?", (it, self, a, b) => { var p = it.Abs(it.PathStr(a[0])); return File.Exists(p) || Directory.Exists(p) || File.Exists(BuiltExecutable(p)); });
        DefS(F, "file?", (it, self, a, b) => File.Exists(BuiltExecutable(it.Abs(it.PathStr(a[0])))));
        DefS(F, "directory?", (it, self, a, b) => Directory.Exists(it.Abs(it.PathStr(a[0]))));
        DefS(F, "symlink?", (it, self, a, b) => IsSymlink(it.Abs(it.PathStr(a[0]))));
        DefS(F, "executable?", (it, self, a, b) => { var p = it.Abs(it.PathStr(a[0])); return File.Exists(p) && IsExecutableName(p) || it.WithExe(p) != null; });
        DefS(F, "readable? writable?", (it, self, a, b) => File.Exists(BuiltExecutable(it.Abs(it.PathStr(a[0])))));
        DefS(F, "zero? empty?", (it, self, a, b) => { var p = it.Abs(it.PathStr(a[0])); return File.Exists(p) && new FileInfo(p).Length == 0; });
        DefS(F, "size", (it, self, a, b) => new FileInfo(BuiltExecutable(it.Abs(it.PathStr(a[0])))).Length);
        DefS(F, "size?", (it, self, a, b) => SizeOrNil(BuiltExecutable(it.Abs(it.PathStr(a[0])))));
        DefS(F, "read", (it, self, a, b) => S(ReadText(BuiltExecutable(it.Abs(it.PathStr(a[0]))))));
        DefS(F, "binread", (it, self, a, b) => BinRead(it, BuiltExecutable(it.Abs(it.PathStr(a[0]))), a[1..]));
        DefS(F, "readlines", (it, self, a, b) => new RArray(Lines(ReadText(it.Abs(it.PathStr(a[0])))).Select(l => (object)S(it.Truthy2(a, "chomp") ? l.TrimEnd('\n') : l))));
        DefS(F, "foreach", (it, self, a, b) => { foreach (var l in Lines(ReadText(it.Abs(it.PathStr(a[0]))))) it.Y1(b, S(l)); return null; });
        DefS(F, "write binwrite", (it, self, a, b) =>
        {
            var data = Bytes(a[1], it.ToS(a[1]));
            File.WriteAllBytes(it.Abs(it.PathStr(a[0])), data);
            return (long)data.Length;
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
        DefS(F, "delete unlink", (it, self, a, b) => { foreach (var x in a) File.Delete(BuiltExecutable(it.Abs(it.PathStr(x)))); return (long)a.Length; });
        DefS(F, "rename", (it, self, a, b) => { it.MovePath(it.Abs(it.PathStr(a[0])), it.Abs(it.PathStr(a[1]))); return 0L; });
        DefS(F, "symlink", (it, self, a, b) => { it.MakeSymlink(it.PathStr(a[0]), it.Abs(it.PathStr(a[1]))); return 0L; });
        DefS(F, "readlink", (it, self, a, b) => S(new FileInfo(it.Abs(it.PathStr(a[0]))).LinkTarget ?? ""));
        DefS(F, "chmod", (it, self, a, b) =>
        {
            foreach (var x in a.Skip(1)) Chmod(it.Abs(it.PathStr(x)), it.Int(a[0]), false);
            return (long)(a.Length - 1);
        });
        // `*` crosses '/' unless File::FNM_PATHNAME is given, as in Ruby; Pathname#fnmatch? is File.fnmatch(pattern, self).
        DefS(F, "fnmatch fnmatch?", (it, self, a, b) => GlobRegex(it.Str(a[0]), a.Length > 2 && a[2] is long f && (f & 2) != 0).IsMatch(it.PathStr(a[1])));
        Def(P, "fnmatch fnmatch?", (it, self, a, b) => GlobRegex(it.Str(a[0]), a.Length > 1 && a[1] is long f && (f & 2) != 0).IsMatch(Me(self)));
        DefS(F, "open new", (it, self, a, b) =>
        {
            var path = it.Abs(it.PathStr(a[0]));
            var mode = a.Length > 1 && a[1] is RString m ? m.V : "r";
            var handle = new FileHandle { Path = path, Mode = mode, Binary = mode.Contains('b') };
            var f = new RObject { Class = it.FileClass, Host = handle };
            // Binary modes ("rb", "r+b") keep every byte (one char per byte); text mode folds CRLFs.
            if (mode.StartsWith('r')) handle.Buffer.Append(handle.Binary ? Encoding.Latin1.GetString(File.ReadAllBytes(path)) : ReadText(path));
            if (b == null) return f;
            try { return it.Y1(b, f); }
            finally { it.Invoke(f, "close", NoArgs, null); }
        });
        FileHandle FH(object o) => (FileHandle)((RObject)o).Host;
        // Writes land at the position (seek then write overwrites, as lziprecover's test corrupts a byte in place).
        void WriteAt(FileHandle h, string text)
        {
            var overlap = Math.Min(text.Length, h.Buffer.Length - h.Pos);
            if (overlap > 0) h.Buffer.Remove(h.Pos, overlap);
            h.Buffer.Insert(h.Pos, text);
            h.Pos += text.Length;
        }
        Def(F, "write print <<", (it, self, a, b) =>
        {
            foreach (var x in a) { WriteAt(FH(self), it.ToS(x)); FH(self).Binary |= x is RString { Binary: true }; }
            return self;
        });
        Def(F, "puts", (it, self, a, b) => { var sw = new StringWriter(); it.Puts(sw, a); WriteAt(FH(self), sw.ToString()); return null; });
        Def(F, "read", (it, self, a, b) =>
        {
            var h = FH(self);
            var count = a.Length > 0 && a[0] != null ? (int)Math.Min(it.Int(a[0]), h.Buffer.Length - h.Pos) : h.Buffer.Length - h.Pos;
            if (a.Length > 0 && a[0] != null && count <= 0 && it.Int(a[0]) > 0) return null;   // read(n) at EOF is nil
            var s = h.Buffer.ToString(h.Pos, Math.Max(0, count));
            h.Pos += Math.Max(0, count);
            return new RString(s) { Binary = h.Binary };
        });
        Def(F, "seek", (it, self, a, b) =>
        {
            var h = FH(self);
            var whence = a.Length > 1 ? a[1] switch { long w => w, RSymbol { Name: "CUR" } => 1L, RSymbol { Name: "END" } => 2L, _ => 0L } : 0L;
            var origin = whence switch { 1 => h.Pos, 2 => h.Buffer.Length, _ => 0 };
            h.Pos = (int)Math.Max(0, origin + it.Int(a[0]));
            if (h.Pos > h.Buffer.Length) h.Buffer.Append('\0', h.Pos - h.Buffer.Length);
            return 0L;
        });
        Def(F, "pos tell", (it, self, a, b) => (long)FH(self).Pos);
        Def(F, "rewind", (it, self, a, b) => { FH(self).Pos = 0; return 0L; });
        Def(F, "eof? eof", (it, self, a, b) => FH(self).Pos >= FH(self).Buffer.Length);
        Def(F, "each_line each", (it, self, a, b) => { foreach (var l in Lines(FH(self).Buffer.ToString())) it.Y1(b, S(l)); return self; });
        Def(F, "readlines", (it, self, a, b) => new RArray(Lines(FH(self).Buffer.ToString()).Select(l => (object)S(l))));
        Def(F, "path to_path", (it, self, a, b) => S(FH(self).Path));
        Def(F, "close", (it, self, a, b) =>
        {
            var h = FH(self);
            if (h.Closed) return null;
            h.Closed = true;
            Encoding encoding = h.Binary ? Encoding.Latin1 : new UTF8Encoding(false);
            if (h.Mode.StartsWith('w') || (h.Mode.StartsWith('r') && h.Mode.Contains('+'))) File.WriteAllText(h.Path, h.Buffer.ToString(), encoding);
            else if (h.Mode.StartsWith('a')) File.AppendAllText(h.Path, h.Buffer.ToString(), encoding);
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
            foreach (var p in Paths(it, a[0]).Select(BuiltExecutable))
            {
                if (!File.Exists(p) && !IsSymlink(p)) throw it.Error(it.ENOENT, "No such file or directory @ apply2files - " + p);
                File.Delete(p);
            }
            return a[0];
        });
        DefS(FU, "rm_f", (it, self, a, b) => { foreach (var p in Paths(it, a[0]).Select(BuiltExecutable)) if (File.Exists(p) || IsSymlink(p)) File.Delete(p); return a[0]; });
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
            // cp_r "dir/.", dest copies dir's contents into dest (no dest/dir).
            var raw = (a[0] is RArray ra ? ra.L : new List<object> { a[0] }).Select(it.PathStr).ToList();
            if (raw.Count == 1 && (raw[0].EndsWith("/.") || raw[0].EndsWith("\\.")) && Directory.Exists(it.Abs(raw[0])))
            {
                CopyDir(it.Abs(raw[0]), dest);
                return null;
            }
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
        DefS(FU, "chmod", (it, self, a, b) => { foreach (var p in Paths(it, a[1])) Chmod(p, a[0] is long l ? l : 0b110100100, false); return null; });
        DefS(FU, "chmod_R", (it, self, a, b) => { foreach (var p in Paths(it, a[1])) Chmod(p, a[0] is long l ? l : 0b110100100, true); return null; });
        DefS(FU, "chown chown_R", (it, self, a, b) => null);
        DefS(FU, "pwd", (it, self, a, b) => S(it.Cwd.Replace('\\', '/')));
        DefS(FU, "cd chdir", (it, self, a, b) => it.Invoke(it.DirClass, "chdir", new[] { a[0] }, b));
        DefS(FU, "compare_file identical? cmp", (it, self, a, b) =>
            File.ReadAllBytes(it.Abs(it.PathStr(a[0]))).AsSpan().SequenceEqual(File.ReadAllBytes(it.Abs(it.PathStr(a[1])))));
        // `include FileUtils` gets the same functions as instance methods
        foreach (var kv in SingletonOf(FU).Methods) FU.Methods[kv.Key] = kv.Value;

        // ---- $stdout / $stderr ----
        // Without these, `$stderr.puts` resolved to Kernel#puts on nil and wrote to stdout.
        foreach (var (name, fd) in new[] { ("stdout", 1L), ("stderr", 2L) })
        {
            var io = new RObject { Class = IOClass };
            TextWriter W(Interp it) => fd == 1 ? it.Out : it.Err;
            var sc = SingletonOf(io);
            Def(sc, "puts", (it, self, a, b) => { it.Puts(W(it), a); return null; });
            Def(sc, "print", (it, self, a, b) => { foreach (var x in a) W(it).Write(it.ToS(x)); return null; });
            Def(sc, "write", (it, self, a, b) => { long n = 0; foreach (var x in a) { var t = it.ToS(x); W(it).Write(t); n += Encoding.UTF8.GetByteCount(t); } return n; });
            Def(sc, "<<", (it, self, a, b) => { W(it).Write(it.ToS(a[0])); return self; });
            Def(sc, "printf", (it, self, a, b) => { W(it).Write(it.Format(it.Str(a[0]), a[1..])); return null; });
            Def(sc, "flush fsync", (it, self, a, b) => { W(it).Flush(); return self; });
            Def(sc, "sync", (it, self, a, b) => true);
            Def(sc, "sync=", (it, self, a, b) => a[0]);
            Def(sc, "tty? isatty", (it, self, a, b) => false);
            Def(sc, "fileno to_i", (it, self, a, b) => fd);
            Def(sc, "inspect to_s", (it, self, a, b) => S($"#<IO:<{name.ToUpperInvariant()}>>"));
            Globals["$" + name] = io;
            ObjectClass.Consts[name.ToUpperInvariant()] = io;
        }

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
        Def(E, "exclude?", (it, self, a, b) => !it.Env.ContainsKey(it.Str(a[0])));   // ActiveSupport
        Def(E, "delete", (it, self, a, b) => it.Env.Remove(it.Str(a[0]), out var v) ? S(v) : null);
        Def(E, "to_h to_hash", (it, self, a, b) => { var h = new RHash(); foreach (var kv in it.Env) h.D[S(kv.Key)] = S(kv.Value); return h; });
        Def(E, "keys", (it, self, a, b) => new RArray(it.Env.Keys.Select(k => (object)S(k))));
        Def(E, "each each_pair", (it, self, a, b) => { foreach (var kv in it.Env.ToList()) it.Y1(b, Pair(S(kv.Key), S(kv.Value))); return self; });
        Def(E, "inspect to_s", (it, self, a, b) => S("ENV"));
        Def(E, "prepend_path", (it, self, a, b) => { it.EnvJoin(it.Str(a[0]), it.PathStr(a[1]), ";", true); return null; });
        Def(E, "append_path", (it, self, a, b) => { it.EnvJoin(it.Str(a[0]), it.PathStr(a[1]), ";", false); return null; });
        Def(E, "prepend", (it, self, a, b) => { it.EnvJoin(it.Str(a[0]), it.ToS(a[1]), a.Length > 2 ? it.Str(a[2]) : " ", true); return null; });
        Def(E, "append", (it, self, a, b) => { it.EnvJoin(it.Str(a[0]), it.ToS(a[1]), a.Length > 2 ? it.Str(a[2]) : " ", false); return null; });
        Def(E, "permit_arch_flags runtime_cpu_detection cxx11 libcxx O0 O1 O2 O3 Os refurbish_args", (it, self, a, b) => null);
        // Superenv's deparallelize: make runs one job (no MAKEFLAGS, HOMEBREW_MAKE_JOBS=1); with a block, only
        // for the block (ghc@9.4's `ENV.deparallelize { system "make", "install" }`), then both come back.
        Def(E, "deparallelize", (it, self, a, b) =>
        {
            var oldFlags = it.Env.Remove("MAKEFLAGS", out var f) ? f : null;
            var oldJobs = it.Env.TryGetValue("HOMEBREW_MAKE_JOBS", out var j) ? j : null;
            it.Env["HOMEBREW_MAKE_JOBS"] = "1";
            if (b == null) return null;
            try { return it.Yield(b); }
            finally
            {
                if (oldFlags != null) it.Env["MAKEFLAGS"] = oldFlags; else it.Env.Remove("MAKEFLAGS");
                if (oldJobs != null) it.Env["HOMEBREW_MAKE_JOBS"] = oldJobs; else it.Env.Remove("HOMEBREW_MAKE_JOBS");
            }
        });
    }

    // File.read: text mode (CRLF -> LF, a UTF-8 BOM dropped); bytes that aren't UTF-8 stay raw bytes, as in
    // Ruby, instead of turning into U+FFFD (Digest of a read binary file, "\x89PNG" comparisons).
    public static string ReadText(string path)
    {
        var bytes = Win32Process.ReadAllShared(path);
        var span = bytes.AsSpan();
        if (span.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) span = span[3..];
        return DecodeUtf8(span).Replace("\r\n", "\n");
    }

    // IO.binread(name, length = nil, offset = 0): photocraft-cli's test checks (testpath/"test.png").binread(4).
    static object BinRead(Interp it, string path, object[] a)
    {
        var bytes = Win32Process.ReadAllShared(path);
        long offset = a.Length > 1 && a[1] != null ? it.Int(a[1]) : 0;
        if (offset < 0) throw it.Error(it.ArgumentError, $"negative offset {offset} given");
        if (a.Length > 0 && a[0] != null)
        {
            var length = it.Int(a[0]);
            if (length < 0) throw it.Error(it.ArgumentError, $"negative length {length} given");
            if (offset >= bytes.Length && length > 0) return null;
            bytes = bytes.AsSpan((int)Math.Min(offset, bytes.Length), (int)Math.Min(length, Math.Max(0, bytes.Length - offset))).ToArray();
        }
        else bytes = bytes.AsSpan((int)Math.Min(offset, bytes.Length)).ToArray();
        return new RString(Encoding.Latin1.GetString(bytes)) { Binary = true };
    }

    public static string DecodeUtf8(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        while (bytes.Length > 0)
        {
            if (System.Text.Rune.DecodeFromUtf8(bytes, out var rune, out var used) == System.Buffers.OperationStatus.Done)
            {
                if (rune.IsBmp) sb.Append((char)rune.Value); else sb.Append(rune.ToString());
            }
            else { sb.Append((char)(RString.RawByteBase + bytes[0])); used = 1; }
            bytes = bytes[used..];
        }
        return sb.ToString();
    }

    bool Truthy2(object[] a, string kw) => a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Sym(kw), out var v) && Truthy(v);

    void EnvJoin(string key, string value, string sep, bool prepend)
    {
        // Java on Windows splits CLASSPATH at ';': upstream's ":" would cut "C:/.../antlr-complete.jar" at its drive colon.
        if (sep == ":" && key.Equals("CLASSPATH", StringComparison.OrdinalIgnoreCase)) sep = ";";
        if (!Env.TryGetValue(key, out var cur) || cur.Length == 0) { Env[key] = value; return; }
        Env[key] = prepend ? value + sep + cur : cur + sep + value;
    }

    sealed class FileHandle
    {
        public string Path, Mode;
        public readonly StringBuilder Buffer = new();
        public int Pos;
        public bool Closed;
        public bool Binary;   // binary strings were written: save their bytes as-is
    }

    // File.size? / Pathname#size?: nil for a missing or empty file, otherwise its size.
    static object SizeOrNil(string p) => File.Exists(p) && new FileInfo(p).Length is var n and > 0 ? n : null;

    static bool IsSymlink(string p)
    {
        try { return new FileInfo(p).LinkTarget != null || new DirectoryInfo(p).LinkTarget != null; }
        catch (IOException) { return false; }
    }

    // Built executables carry .exe on Windows; `rm bin/"file"` (libmagic drops its file(1)) means file.exe.
    static string BuiltExecutable(string p) =>
        !File.Exists(p) && !Directory.Exists(p) && !IsSymlink(p) && !Path.HasExtension(p) && File.Exists(p + ".exe") ? p + ".exe" : p;

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

    // Windows has no permission bits; the owner-write bit maps to the read-only attribute.
    static void Chmod(string p, long mode, bool recursive)
    {
        bool readOnly = (mode & 0x80) == 0;   // 0200
        if (File.Exists(p)) new FileInfo(p) { IsReadOnly = readOnly };
        else if (recursive && Directory.Exists(p))
            foreach (var f in Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                new FileInfo(f) { IsReadOnly = readOnly };
    }

    public void RemoveTree(string p)
    {
        if (IsWindowsNodePrebuild(p)) return;
        DeleteTree(p);
    }

    // Node formulae drop the prebuilt binaries "for other platforms" by keeping only the macOS or Linux one
    // (`rm_r dir if dir.basename.to_s != "#{os}-#{arch}"` in tweakcc, mimo-code, apify-cli, pi-coding-agent).
    // Here the platform they mean to keep is Windows', so win32-x64 prebuilds stay.
    static bool IsWindowsNodePrebuild(string p) =>
        p.Replace('\\', '/').Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
        && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(Path.TrimEndingDirectorySeparator(p)), @"win32-x64(-msvc)?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // rm -rf that also removes read-only files (git objects, chmod 0444). Symlinks are removed, never
    // followed: a link to a directory elsewhere (or a link loop) mustn't be walked into.
    public static void DeleteTree(string p)
    {
        if (IsSymlink(p)) { if (Directory.Exists(p)) Directory.Delete(p); else File.Delete(p); return; }
        if (Directory.Exists(p))
        {
            ClearReadOnly(p);
            Directory.Delete(p, true);
        }
        else if (File.Exists(p))
        {
            var fi = new FileInfo(p);
            if (fi.IsReadOnly) fi.IsReadOnly = false;
            fi.Delete();
        }
    }

    static void ClearReadOnly(string dir)
    {
        foreach (var e in new DirectoryInfo(dir).EnumerateFileSystemInfos("*", new EnumerationOptions { AttributesToSkip = 0 }))
        {
            if (e.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
            if (e is DirectoryInfo) ClearReadOnly(e.FullName);
            else if (e.Attributes.HasFlag(FileAttributes.ReadOnly)) e.Attributes &= ~FileAttributes.ReadOnly;
        }
    }

    void MovePath(string from, string to)
    {
        if (Directory.Exists(from) && !IsSymlink(from))
        {
            // Never delete what's already there (make install output, a user's var/ data): merge into it.
            if (Directory.Exists(to) && !IsSymlink(to))
            {
                foreach (var entry in Directory.GetFileSystemEntries(from)) MovePath(entry, Path.Combine(to, Path.GetFileName(entry)));
                Directory.Delete(from);
            }
            else Directory.Move(from, to);
        }
        else if (File.Exists(from) || IsSymlink(from)) File.Move(from, to, true);
        // A built executable named without its .exe moves as one (build2: mv "b/b", "b/b-boot" for b.exe).
        else if (WithExe(from) is { } exe)
            File.Move(exe, Path.HasExtension(to) && !to.EndsWith('.') ? to : to + Path.GetExtension(exe).ToLowerInvariant(), true);
        else throw Error(ENOENT, $"No such file or directory @ rb_file_s_rename - ({from}, {to})");
    }

    // cp_r: symlinks are copied as symlinks (FileUtils' default), so a link loop can't recurse forever.
    static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var e in new DirectoryInfo(from).EnumerateFileSystemInfos("*", new EnumerationOptions { AttributesToSkip = 0 }))
        {
            var dest = Path.Combine(to, e.Name);
            if (e.LinkTarget is { } target)
            {
                if (IsSymlink(dest) || File.Exists(dest)) { if (Directory.Exists(dest)) Directory.Delete(dest); else File.Delete(dest); }
                if (e is DirectoryInfo) Directory.CreateSymbolicLink(dest, target); else File.CreateSymbolicLink(dest, target);
            }
            else if (e is DirectoryInfo) CopyDir(e.FullName, dest);
            else File.Copy(e.FullName, dest, true);
        }
    }

    void MakeSymlink(string target, string link)
    {
        var dir = Path.GetDirectoryName(link);
        if (dir != null) Directory.CreateDirectory(dir);
        var resolved = Path.IsPathRooted(target) ? target : Path.Combine(dir ?? Cwd, target);
        // A link to a built executable named without its .exe (kubectx: ln_s bin/"kubectx", bin/"kubectl-ctx") points
        // at the .exe and carries the suffix too, as install_symlink's do, or it dangles and Windows can't run it.
        if (!File.Exists(resolved) && !Directory.Exists(resolved) && !IsSymlink(resolved) && File.Exists(resolved + ".exe"))
        {
            target += ".exe";
            resolved += ".exe";
            if (!Path.HasExtension(link)) link += ".exe";
            if (File.Exists(link) || IsSymlink(link)) File.Delete(link);
        }
        if (Directory.Exists(resolved)) Directory.CreateSymbolicLink(link, target);
        else File.CreateSymbolicLink(link, target);
    }

    // Homebrew's Pathname#install: moves sources (relative to cwd) into dir.
    // A binary (ASCII-8BIT) string's bytes are its chars; anything else is written as UTF-8.
    static byte[] Bytes(object value, string text) =>
        value is RString { Binary: true } ? Encoding.Latin1.GetBytes(text) : RString.Utf8(text);

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
            // Pathname#install(resource): the resource is staged into the directory (pkgshare.install resource("data")).
            case RObject { Class.Name: "FormulaResource" } resource:
                Directory.CreateDirectory(Abs(dir));
                Invoke(resource, "stage", new object[] { NewPath(Abs(dir)) }, null);
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
        // Built executables carry .exe on Windows; formulae name them without it.
        if (!File.Exists(absSrc) && !Directory.Exists(absSrc) && !IsSymlink(absSrc) && File.Exists(absSrc + ".exe"))
        {
            absSrc += ".exe";
            if (!Path.HasExtension(newName)) newName += ".exe";
        }
        if (!File.Exists(absSrc) && !Directory.Exists(absSrc) && !IsSymlink(absSrc))
            throw Error(ENOENT, $"No such file or directory - {src}");
        var absDir = Abs(dir);
        if (InstallHook != null && InstallHook(absDir, absSrc, newName)) return;
        Directory.CreateDirectory(absDir);
        var dest = Path.Combine(absDir, newName);
        // InstallRenamed (etc.install): never clobber a user's config; a changed default lands beside it.
        if (InstallRenamedRoots.Any(r => (absDir + "\\").StartsWith(r + "\\", StringComparison.OrdinalIgnoreCase)))
        {
            if (Directory.Exists(absSrc) && !IsSymlink(absSrc))
            {
                foreach (var child in Directory.GetFileSystemEntries(absSrc)) InstallOne(dest, child, Path.GetFileName(child));
                Directory.Delete(absSrc, true);
                return;
            }
            if (File.Exists(dest))
            {
                if (File.ReadAllBytes(dest).AsSpan().SequenceEqual(File.ReadAllBytes(absSrc))) { File.Delete(absSrc); return; }
                dest += ".default";
            }
        }
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
                if (name.StartsWith('.') || IsSymlink(d)) continue;   // like Ruby, ** doesn't follow symlinked directories
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
