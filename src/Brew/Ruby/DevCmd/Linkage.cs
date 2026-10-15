using System.Text;

namespace Brew.Ruby.DevCmd;

[DevCommand("linkage")]
sealed class LinkageCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("linkage")
        .Description("""
            Check the library links from the given <formula> kegs. If no <formula> are
            provided, check all kegs. Raises an error if run on uninstalled formulae.
            """)
        .Switch(new[] { "--test" }, "Show only missing libraries and exit with a non-zero status if any missing libraries are found.")
        .Switch(new[] { "--strict" }, "Exit with a non-zero status if any undeclared dependencies with linkage are found.", dependsOn: "--test")
        .Switch(new[] { "--reverse" }, "For every library that a keg references, print its dylib path followed by the binaries that link to it.")
        .Switch(new[] { "--cached" }, "Print the cached linkage values stored in `$HOMEBREW_CACHE`, set by a previous `brew linkage` run.")
        .NamedArgs("installed_formula");

    public override void Run(ParsedArgs args)
    {
        var kegs = new List<string>();
        if (args.Named.Count == 0)
        {
            if (Directory.Exists(DevCommands.Cellar))
                foreach (var rack in Directory.GetDirectories(DevCommands.Cellar).Order(StringComparer.Ordinal))
                {
                    var name = Path.GetFileName(rack);
                    if (name.StartsWith('.')) continue;
                    var installed = DevCommands.InstalledKegs(name);
                    if (installed.Count > 0) kegs.Add(installed[^1]);
                }
        }
        else
            foreach (var n in args.Named)
            {
                var name = DevCommands.NameOf(n).ToLowerInvariant();
                var installed = DevCommands.InstalledKegs(name);
                if (installed.Count == 0) throw new BrewError($"No such keg: {Path.Combine(DevCommands.Cellar, name)}");
                kegs.Add(installed[^1]);
            }

        foreach (var keg in kegs)
        {
            var name = Path.GetFileName(Path.GetDirectoryName(keg));
            if (kegs.Count > 1) Out.Ohai($"Checking {name} linkage");
            var result = new LinkageChecker(keg, name);
            if (args.Switch("test"))
            {
                result.DisplayTestOutput(args.Switch("strict"));
                if (result.Broken(true, args.Switch("strict"))) DevCommands.Failed = true;
            }
            else if (args.Switch("reverse")) result.DisplayReverseOutput();
            else result.DisplayNormalOutput();
        }
    }
}

// LinkageChecker for Windows: PE import tables instead of Mach-O/ELF load commands. A DLL is a
// system library when Windows resolves it outside Homebrew (API sets, KnownDLLs, System32...),
// a Homebrew library when it resolves into a keg, and missing when nothing provides it.
sealed class LinkageChecker
{
    readonly string keg, name;
    readonly FormulaSpec formula;
    readonly SortedSet<string> systemDylibs = new(StringComparer.Ordinal), brokenDylibs = new(StringComparer.Ordinal);
    readonly SortedDictionary<string, SortedSet<string>> brewedDylibs = new(StringComparer.Ordinal);
    readonly SortedDictionary<string, SortedSet<string>> reverseLinks = new(StringComparer.Ordinal);
    readonly SortedDictionary<string, List<string>> brokenDeps = new(StringComparer.Ordinal);
    List<string> indirectDeps = new(), undeclaredDeps = new(), unnecessaryDeps = new(), versionConflictDeps = new(), noLinkageDeps = new(), unexpectedLinkageDeps = new();

    static readonly string SystemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    static readonly string System32 = Environment.SystemDirectory;

    public LinkageChecker(string keg, string name)
    {
        this.keg = keg;
        this.name = name;
        formula = FormulaAuditor.Load(name);
        if (formula == null) Out.Opoo($"Formula unavailable: {name}");
        CheckDylibs();
    }

    static readonly HashSet<string> KnownDlls = LoadKnownDlls();

    static HashSet<string> LoadKnownDlls()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!OperatingSystem.IsWindows()) return set;
        try
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs");
            if (k != null)
                foreach (var v in k.GetValueNames())
                    if (k.GetValue(v) is string s) set.Add(s);
        }
        catch (Exception) { }
        return set;
    }

    IEnumerable<string> KegFiles()
    {
        var stack = new Stack<string>();
        stack.Push(keg);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            IEnumerable<string> entries;
            try { entries = Directory.EnumerateFileSystemEntries(dir).ToList(); }
            catch (Exception) { continue; }
            foreach (var e in entries)
            {
                var info = new FileInfo(e);
                if (info.LinkTarget != null) continue;
                if (Directory.Exists(e)) stack.Push(e);
                else yield return e;
            }
        }
    }

    // Where the Windows loader would find `dll` for a binary in `dir` (application dir, system dirs, PATH).
    string Resolve(string dll, string dir)
    {
        var local = Path.Combine(dir, dll);
        if (File.Exists(local)) return Path.GetFullPath(local);
        foreach (var sys in new[] { System32, Path.Combine(SystemRoot, "System"), SystemRoot })
        {
            var p = Path.Combine(sys, dll);
            if (File.Exists(p)) return p;
        }
        foreach (var d in Environment.GetEnvironmentVariable("PATH")?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
        {
            try
            {
                var p = Path.Combine(d.Trim('"'), dll);
                if (File.Exists(p)) return Path.GetFullPath(p);
            }
            catch (ArgumentException) { }
        }
        // Homebrew's own lib/bin dirs and other kegs (as `opt` links would make them available).
        foreach (var d in new[] { Path.Combine(DevCommands.Prefix, "bin"), Path.Combine(DevCommands.Prefix, "lib") })
        {
            var p = Path.Combine(d, dll);
            if (File.Exists(p)) return Path.GetFullPath(p);
        }
        return null;
    }

    // Keg.for: the keg directory (Cellar/name/version) containing path.
    static (string name, string keg)? KegFor(string path)
    {
        var real = path;
        try
        {
            var info = new FileInfo(path);
            if (info.LinkTarget != null && info.ResolveLinkTarget(true) is { } t) real = t.FullName;
        }
        catch (Exception) { }
        var cellar = Path.GetFullPath(DevCommands.Cellar) + Path.DirectorySeparatorChar;
        if (!real.StartsWith(cellar, StringComparison.OrdinalIgnoreCase)) return null;
        var parts = real[cellar.Length..].Split(Path.DirectorySeparatorChar);
        if (parts.Length < 3) return null;
        return (parts[0], Path.Combine(DevCommands.Cellar, parts[0], parts[1]));
    }

    static bool SystemApiSet(string dll) => dll.StartsWith("api-ms-win-", StringComparison.OrdinalIgnoreCase) || dll.StartsWith("ext-ms-", StringComparison.OrdinalIgnoreCase);

    void CheckDylibs()
    {
        var checkedDylibs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in KegFiles())
        {
            var imports = PeFile.Imports(file);
            if (imports == null) continue;
            var dir = Path.GetDirectoryName(file);
            foreach (var dll in imports)
            {
                string display;
                string resolved = SystemApiSet(dll) || KnownDlls.Contains(dll) ? null : Resolve(dll, dir);
                display = resolved ?? (SystemApiSet(dll) || KnownDlls.Contains(dll) ? Path.Combine(System32, dll) : dll);
                if (SystemApiSet(dll)) display = dll;
                if (!reverseLinks.TryGetValue(display, out var files)) reverseLinks[display] = files = new SortedSet<string>(StringComparer.Ordinal);
                files.Add(file);
                if (!checkedDylibs.Add(display)) continue;

                if (SystemApiSet(dll) || KnownDlls.Contains(dll))
                {
                    systemDylibs.Add(display);
                    continue;
                }
                if (resolved == null)
                {
                    brokenDylibs.Add(dll);
                    continue;
                }
                var owner = KegFor(resolved);
                if (owner == null)
                {
                    systemDylibs.Add(resolved);
                    continue;
                }
                if (!brewedDylibs.TryGetValue(owner.Value.name, out var set)) brewedDylibs[owner.Value.name] = set = new SortedSet<string>(StringComparer.Ordinal);
                set.Add(resolved);
            }
        }
        CheckFormulaDeps();
    }

    void CheckFormulaDeps()
    {
        if (formula == null) return;
        var declared = formula.Dependencies.Where(d => !d.Build && !d.Test && !d.Optional && !d.Recommended).ToList();
        var declaredNames = declared.Select(d => DevCommands.NameOf(d.Name)).ToList();
        var noLinkage = declared.Where(d => d.Tags.Contains("no_linkage")).Select(d => d.Name).ToList();
        var noLinkageNames = noLinkage.Select(DevCommands.NameOf).ToList();
        var recursive = FormulaAuditor.RecursiveRuntimeDeps(formula);
        foreach (var full in brewedDylibs.Keys)
        {
            var n = DevCommands.NameOf(full);
            if (n == formula.Name) continue;
            if (noLinkageNames.Contains(n)) { unexpectedLinkageDeps.Add(full); continue; }
            if (recursive.Contains(n))
            {
                if (!declaredNames.Contains(n)) indirectDeps.Add(full);
            }
            else undeclaredDeps.Add(full);
        }
        indirectDeps = SortByFullName(indirectDeps);
        undeclaredDeps = SortByFullName(undeclaredDeps);
        unexpectedLinkageDeps = SortByFullName(unexpectedLinkageDeps);
        unnecessaryDeps = declared.Select(d => d.Name).Where(full =>
        {
            var kegs = DevCommands.InstalledKegs(DevCommands.NameOf(full));
            if (kegs.Count > 0 && Directory.Exists(Path.Combine(kegs[^1], "bin"))) return false;
            return !brewedDylibs.Keys.Select(DevCommands.NameOf).Contains(DevCommands.NameOf(full));
        }).Except(noLinkage).ToList();
        var missingDeps = brokenDeps.Keys.ToList();
        unnecessaryDeps = unnecessaryDeps.Except(missingDeps).ToList();
        var versionHash = new Dictionary<string, HashSet<string>>();
        var conflicts = new List<string>();
        foreach (var l in brewedDylibs.Keys)
        {
            var n = DevCommands.NameOf(l);
            var unversioned = n.Split('@')[0];
            if (!versionHash.TryGetValue(unversioned, out var s)) versionHash[unversioned] = s = new HashSet<string>();
            s.Add(n);
            if (s.Count < 2) continue;
            foreach (var x in s) if (!conflicts.Contains(x)) conflicts.Add(x);
        }
        versionConflictDeps = conflicts;
        noLinkageDeps = noLinkage;
    }

    static List<string> SortByFullName(List<string> arr)
    {
        arr.Sort((a, b) => a.Contains('/') && !b.Contains('/') ? 1 : !a.Contains('/') && b.Contains('/') ? -1 : string.CompareOrdinal(a, b));
        return arr;
    }

    static string DisplayItems(string label, IEnumerable<string> things, bool puts = true)
    {
        var list = things.ToList();
        if (list.Count == 0) return null;
        var output = new List<string> { $"{label}:" };
        output.AddRange(list.Order(StringComparer.Ordinal));
        var s = string.Join("\n  ", output);
        if (puts) Console.WriteLine(s);
        return s;
    }

    static string DisplayItems(string label, IDictionary<string, SortedSet<string>> things)
    {
        if (things.Count == 0) return null;
        var output = new List<string> { $"{label}:" };
        foreach (var (listLabel, items) in things)
            foreach (var item in items) output.Add($"{item} ({listLabel})");
        var s = string.Join("\n  ", output);
        Console.WriteLine(s);
        return s;
    }

    static string DisplayItems(string label, IDictionary<string, List<string>> things)
    {
        if (things.Count == 0) return null;
        var output = new List<string> { $"{label}:" };
        foreach (var (listLabel, items) in things)
            foreach (var item in items.Order(StringComparer.Ordinal)) output.Add($"{item} ({listLabel})");
        var s = string.Join("\n  ", output);
        Console.WriteLine(s);
        return s;
    }

    public void DisplayNormalOutput()
    {
        DisplayItems("System libraries", systemDylibs);
        DisplayItems("Homebrew libraries", brewedDylibs);
        DisplayItems("Indirect dependencies with linkage", indirectDeps);
        DisplayItems("Missing libraries", brokenDylibs);
        DisplayItems("Broken dependencies", brokenDeps);
        DisplayItems("Undeclared dependencies with linkage", undeclaredDeps);
        DisplayItems("Dependencies with no linkage", unnecessaryDeps);
        DisplayItems("Homebrew dependencies not requiring linkage", noLinkageDeps);
        DisplayItems("Unexpected linkage for no_linkage dependencies", unexpectedLinkageDeps);
    }

    public void DisplayReverseOutput()
    {
        if (reverseLinks.Count == 0) return;
        var last = reverseLinks.Keys.Last();
        foreach (var (dylib, files) in reverseLinks)
        {
            Console.WriteLine(dylib);
            var prefix = keg + Path.DirectorySeparatorChar;
            foreach (var f in files) Console.WriteLine("  " + (f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? f[prefix.Length..] : f));
            if (dylib != last) Console.WriteLine();
        }
    }

    public void DisplayTestOutput(bool strict)
    {
        DisplayItems("Missing libraries", brokenDylibs);
        DisplayItems("Broken dependencies", brokenDeps);
        DisplayItems("Conflicting libraries", versionConflictDeps);
        DisplayItems("Indirect dependencies with linkage", indirectDeps);
        DisplayItems("Unexpected linkage for no_linkage dependencies", unexpectedLinkageDeps);
        if (!strict) return;
        DisplayItems("Undeclared dependencies with linkage", undeclaredDeps);
    }

    public bool Broken(bool test, bool strict)
    {
        bool any = brokenDeps.Count > 0 || brokenDylibs.Count > 0;
        if (test) any |= versionConflictDeps.Count > 0 || indirectDeps.Count > 0 || unexpectedLinkageDeps.Count > 0;
        if (test && strict) any |= undeclaredDeps.Count > 0;
        return any;
    }
}

// Minimal PE/COFF reader: the DLL names in a binary's import table (not delay-load imports,
// which, like weak links on macOS, may legitimately be absent).
static class PeFile
{
    public static List<string> Imports(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (fs.Length < 0x40) return null;
            using var r = new BinaryReader(fs);
            if (r.ReadUInt16() != 0x5A4D) return null;            // "MZ"
            fs.Position = 0x3C;
            uint peOff = r.ReadUInt32();
            if (peOff + 24 > fs.Length) return null;
            fs.Position = peOff;
            if (r.ReadUInt32() != 0x00004550) return null;        // "PE\0\0"
            r.ReadUInt16();                                        // machine
            int sections = r.ReadUInt16();
            fs.Position += 12;
            int optSize = r.ReadUInt16();
            r.ReadUInt16();                                        // characteristics
            long opt = fs.Position;
            ushort magic = r.ReadUInt16();
            int ddOffset = magic switch { 0x10B => 96, 0x20B => 112, _ => -1 };
            if (ddOffset < 0 || optSize < ddOffset + 16) return null;
            fs.Position = opt + ddOffset - 4;
            uint dirCount = r.ReadUInt32();
            if (dirCount < 2) return new List<string>();
            fs.Position = opt + ddOffset + 8;                      // data directory 1: import table
            uint importRva = r.ReadUInt32();
            r.ReadUInt32();
            fs.Position = opt + optSize;
            var secs = new List<(uint va, uint vsize, uint raw, uint rawSize)>();
            for (int i = 0; i < sections; i++)
            {
                fs.Position += 8;
                uint vsize = r.ReadUInt32(), va = r.ReadUInt32(), rawSize = r.ReadUInt32(), raw = r.ReadUInt32();
                fs.Position += 16;
                secs.Add((va, vsize, raw, rawSize));
            }
            long Off(uint rva)
            {
                foreach (var s in secs)
                    if (rva >= s.va && rva < s.va + Math.Max(s.vsize, s.rawSize)) return rva - s.va + s.raw;
                return -1;
            }
            var result = new List<string>();
            if (importRva == 0) return result;
            long desc = Off(importRva);
            if (desc < 0) return result;
            for (int i = 0; i < 4096; i++, desc += 20)
            {
                if (desc + 20 > fs.Length) break;
                fs.Position = desc;
                uint lookup = r.ReadUInt32(), stamp = r.ReadUInt32(), chain = r.ReadUInt32(), nameRva = r.ReadUInt32(), iat = r.ReadUInt32();
                if (lookup == 0 && nameRva == 0 && iat == 0) break;
                long no = Off(nameRva);
                if (no < 0 || no >= fs.Length) continue;
                fs.Position = no;
                var sb = new StringBuilder();
                for (int b; sb.Length < 260 && (b = fs.ReadByte()) > 0;) sb.Append((char)b);
                if (sb.Length > 0 && !result.Contains(sb.ToString(), StringComparer.OrdinalIgnoreCase)) result.Add(sb.ToString());
            }
            return result;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }
}
