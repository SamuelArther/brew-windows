using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace WinTools;

// libabigail (abidw, abidiff, abicompat, abilint) and vtable-dumper for Windows PE binaries (DLLs,
// EXEs, drivers). A PE file's ABI surface is its export table (what it provides) and import table
// (what it needs, per DLL), so the corpus here is symbol-level: abidw writes it as abixml (exports as
// elf-symbol entries, imported DLLs as elf-needed dependencies), abidiff compares two binaries or
// two abixml dumps, abicompat checks an application's imports against a library's exports. Type-level
// comparison needs DWARF, which PE files don't carry (MSVC's PDBs are not read). vtable-dumper lists
// exported C++ virtual tables: MSVC's ??_7...@6B@ vftables and MinGW's _ZTV vtables, with slots found
// through the base relocations that every vtable slot carries.
static class Abi
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("abidw", "Dump a PE binary's ABI (exports, imported DLLs) as abixml (libabigail)", RunAbidw);
        Commands.Add("abidiff", "Compare the ABI of two PE binaries or abixml dumps (libabigail)", RunAbidiff);
        Commands.Add("abicompat", "Check that an application's imports are provided by a library (libabigail)", RunAbicompat);
        Commands.Add("abilint", "Read and validate an abixml dump, or dump a PE binary (libabigail)", RunAbilint);
        Commands.Add("vtable-dumper", "List contents of virtual tables in a DLL", RunVtableDumper);
    }

    const string AbigailVersion = "3.0";
    const int ABIDIFF_ERROR = 1, ABIDIFF_USAGE_ERROR = 2, ABIDIFF_ABI_CHANGE = 4, ABIDIFF_ABI_INCOMPATIBLE_CHANGE = 8;

    // ---- corpus: what abidw writes and abidiff compares ----

    sealed class Symbol
    {
        public string Name;
        public bool Function;
        public string Forward;   // "OTHER.dll.Name" for forwarded exports
    }

    sealed class Corpus
    {
        public string Path, Architecture, Soname;
        public List<string> Needed = [];
        public SortedDictionary<string, Symbol> Symbols = new(StringComparer.Ordinal);
    }

    static Corpus Load(string path)
    {
        if (!File.Exists(path)) throw new IOException($"{path}: No such file");
        using (var f = File.OpenRead(path))
        {
            var head = new byte[2];
            if (f.Read(head) == 2 && head[0] == 'M' && head[1] == 'Z') return FromPe(path);
        }
        return FromXml(path);
    }

    static Corpus FromPe(string path)
    {
        var pe = PeFile.Open(path);
        var corpus = new Corpus
        {
            Path = path,
            Architecture = pe.Architecture,
            Soname = pe.ExportName ?? System.IO.Path.GetFileName(path),
            Needed = pe.Imports.Select(i => i.Dll).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
        };
        foreach (var e in pe.Exports)
        {
            var name = e.Name ?? $"#{e.Ordinal}";
            corpus.Symbols[name] = new Symbol { Name = name, Function = e.Forward != null || pe.IsExecutable(e.Rva), Forward = e.Forward };
        }
        return corpus;
    }

    static Corpus FromXml(string path)
    {
        var doc = new XmlDocument();
        try { doc.Load(path); }
        catch (XmlException e) { throw new InvalidOperationException($"{path}: not a PE binary or a valid abixml file ({e.Message})"); }
        var root = doc.DocumentElement;
        if (root == null || root.Name is not ("abi-corpus" or "abi-corpus-group"))
            throw new InvalidOperationException($"{path}: not an abixml file (no abi-corpus element)");
        if (root.Name == "abi-corpus-group") root = root.SelectSingleNode("abi-corpus") as XmlElement ?? root;
        var corpus = new Corpus
        {
            Path = root.GetAttribute("path"),
            Architecture = root.GetAttribute("architecture"),
            Soname = root.GetAttribute("soname"),
        };
        foreach (XmlElement d in root.SelectNodes("elf-needed/dependency")) corpus.Needed.Add(d.GetAttribute("name"));
        foreach (var (section, function) in new[] { ("elf-function-symbols", true), ("elf-variable-symbols", false) })
            foreach (XmlElement s in root.SelectNodes($"{section}/elf-symbol"))
            {
                var name = s.GetAttribute("name");
                if (name.Length == 0) throw new InvalidOperationException($"{path}: elf-symbol without a name");
                corpus.Symbols[name] = new Symbol { Name = name, Function = function };
            }
        return corpus;
    }

    static string Xml(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;");

    static string ToXml(Corpus c, bool withPath, bool withArchitecture)
    {
        var sb = new StringBuilder("<abi-corpus version='2.3'");
        if (withPath) sb.Append($" path='{Xml(c.Path)}'");
        if (withArchitecture && !string.IsNullOrEmpty(c.Architecture)) sb.Append($" architecture='{Xml(c.Architecture)}'");
        if (!string.IsNullOrEmpty(c.Soname)) sb.Append($" soname='{Xml(c.Soname)}'");
        sb.Append(">\n");
        if (c.Needed.Count > 0)
        {
            sb.Append("  <elf-needed>\n");
            foreach (var d in c.Needed) sb.Append($"    <dependency name='{Xml(d)}'/>\n");
            sb.Append("  </elf-needed>\n");
        }
        foreach (var (section, function, type) in new[] { ("elf-function-symbols", true, "func-type"), ("elf-variable-symbols", false, "object-type") })
        {
            var symbols = c.Symbols.Values.Where(s => s.Function == function).ToList();
            if (symbols.Count == 0) continue;
            sb.Append($"  <{section}>\n");
            foreach (var s in symbols)
                sb.Append($"    <elf-symbol name='{Xml(s.Name)}' type='{type}' binding='global-binding' visibility='default-visibility' is-defined='yes'/>\n");
            sb.Append($"  </{section}>\n");
        }
        return sb.Append("</abi-corpus>\n").ToString();
    }

    // ---- abidw ----

    static int RunAbidw(string[] args)
    {
        string input = null, outFile = null;
        bool noOut = false, withPath = true, withArch = true;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: abidw [options] [<path-to-pe-file>]
                         where options can be:
                          --help|-h  display this message
                          --version|-v  display program version information and exit
                          --out-file|-o <file-path>  write the output to 'file-path'
                          --noout  do not emit anything after reading the binary
                          --no-corpus-path  do not take the path to the corpora into account
                          --no-architecture  do not emit architecture info in the output
                          --debug-info-dir|-d <dir>  accepted; PDB debug info is not read
                        The corpus is symbol-level: exported functions and variables, and imported DLLs.
                        """);
                    return 0;
                case "-v" or "--version": Console.WriteLine($"abidw: {AbigailVersion}.0 (wintools {Commands.Version})"); return 0;
                case "-o" or "--out-file": outFile = Next(args, ref i); break;
                case "--noout": noOut = true; break;
                case "--no-corpus-path": withPath = false; break;
                case "--no-architecture": withArch = false; break;
                case "-d" or "--debug-info-dir" or "--headers-dir" or "--header-file" or "--suppressions" or "--kmi-whitelist" or "-w":
                    Next(args, ref i);
                    break;
                case "--no-show-locs" or "--short-locs" or "--load-all-types" or "--no-linux-kernel-mode" or "--abidiff"
                    or "--annotate" or "--stats" or "--verbose" or "--drop-private-types" or "--no-comp-dir-path"
                    or "--exported-interfaces-only" or "--allow-non-exported-interfaces" or "--no-elf-needed":
                    break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"unrecognized option: {args[i]}");
                    if (input != null) throw new UsageException("only one input file is accepted");
                    input = args[i];
                    break;
            }
        }
        if (input == null) throw new UsageException("no input file given (see abidw --help)");
        var corpus = FromPe(input);
        if (args.Contains("--no-elf-needed")) corpus.Needed.Clear();
        if (noOut) return 0;
        var xml = ToXml(corpus, withPath, withArch);
        if (outFile != null) File.WriteAllText(outFile, xml);
        else Console.Write(xml);
        return 0;
    }

    // ---- abilint ----

    static int RunAbilint(string[] args)
    {
        string input = null;
        bool noOut = false, diff = false, stdin = false;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: abilint [options] [<abi-file1>]
                         where options can be:
                          --help  display this message
                          --version|-v  display program version information and exit
                          --noout  do not display anything on stdout
                          --stdin  read abi-file content from stdin
                          --diff  for xml inputs, perform a text diff between the input and the memory model saved back to disk
                        A PE binary as input is read and dumped as abixml, like abidw.
                        """);
                    return 0;
                case "-v" or "--version": Console.WriteLine($"abilint: {AbigailVersion}.0 (wintools {Commands.Version})"); return 0;
                case "--noout": noOut = true; break;
                case "--diff": diff = true; break;
                case "--stdin": stdin = true; break;
                case "--suppressions" or "--headers-dir" or "--debug-info-dir" or "--annotate" or "--corpus-group" or "--show-locs": break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unrecognized option: {arg}");
                    input = arg;
                    break;
            }
        }
        string temp = null;
        if (stdin)
        {
            temp = Path.GetTempFileName();
            using (var o = File.Create(temp)) Console.OpenStandardInput().CopyTo(o);
            input = temp;
        }
        if (input == null) throw new UsageException("no input file given (see abilint --help)");
        try
        {
            Corpus corpus;
            try { corpus = Load(input); }
            catch (InvalidOperationException e) { Console.Error.WriteLine($"abilint: {e.Message}"); return 1; }
            var xml = ToXml(corpus, true, true);
            if (diff && !IsPe(input))
            {
                var original = File.ReadAllText(input).Replace("\r\n", "\n").Trim();
                if (original != xml.Trim()) { Console.WriteLine("files differ"); return 1; }
                return 0;
            }
            if (!noOut) Console.Write(xml);
            return 0;
        }
        finally { if (temp != null) File.Delete(temp); }
    }

    static bool IsPe(string path)
    {
        using var f = File.OpenRead(path);
        return f.ReadByte() == 'M' && f.ReadByte() == 'Z';
    }

    // ---- abidiff ----

    static int RunAbidiff(string[] args)
    {
        var inputs = new List<string>();
        bool noArch = false, statsOnly = false, noAdded = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: abidiff [options] [<file1> <file2>]
                         where options can be:
                          --help|-h  display this message
                          --version|-v  display program version information and exit
                          --no-architecture  do not take architecture in account
                          --stat  only display the change stats
                          --no-added-syms  do not display added functions or variables
                          --debug-info-dir1|--d1 <path>, --debug-info-dir2|--d2 <path>  accepted (no PDB support)
                        Inputs are PE binaries or abixml files written by abidw.
                        Exit status: 0 no change, 4 ABI change, 12 incompatible change (symbols removed), 1 error, 2 usage error.
                        """);
                    return 0;
                case "-v" or "--version": Console.WriteLine($"abidiff: {AbigailVersion}.0 (wintools {Commands.Version})"); return 0;
                case "--no-architecture": noArch = true; break;
                case "--stat": statsOnly = true; break;
                case "--no-added-syms": noAdded = true; break;
                case "--d1" or "--d2" or "--debug-info-dir1" or "--debug-info-dir2" or "--hd1" or "--hd2" or "--headers-dir1"
                    or "--headers-dir2" or "--suppressions" or "--suppr":
                    Next(args, ref i);
                    break;
                case "--no-default-suppression" or "--no-show-locs" or "--harmless" or "--no-harmful" or "--redundant"
                    or "--no-redundant" or "--leaf-changes-only" or "-l" or "--impacted-interfaces" or "--dump-diff-tree"
                    or "--show-bytes" or "--show-bits" or "--show-hex" or "--show-dec" or "--no-unreferenced-symbols":
                    break;
                default:
                    if (args[i].StartsWith('-')) { Console.Error.WriteLine($"abidiff: unrecognized option: {args[i]}"); return ABIDIFF_USAGE_ERROR; }
                    inputs.Add(args[i]);
                    break;
            }
        }
        if (inputs.Count != 2) { Console.Error.WriteLine("abidiff: two input files are needed (see abidiff --help)"); return ABIDIFF_USAGE_ERROR; }
        Corpus a, b;
        try { a = Load(inputs[0]); b = Load(inputs[1]); }
        catch (Exception e) when (e is IOException or InvalidOperationException)
        {
            Console.Error.WriteLine($"abidiff: {e.Message}");
            return ABIDIFF_ERROR;
        }
        var (text, status) = Diff(a, b, noArch, statsOnly, noAdded, null);
        Console.Write(text);
        return status;
    }

    // Symbol-level diff, worded like libabigail's report for symbols without debug info.
    static (string Text, int Status) Diff(Corpus a, Corpus b, bool noArch, bool statsOnly, bool noAdded, HashSet<string> only)
    {
        var sb = new StringBuilder();
        var status = 0;
        bool Wanted(string n) => only == null || only.Contains(n);
        var removed = a.Symbols.Values.Where(s => Wanted(s.Name) && !b.Symbols.ContainsKey(s.Name)).ToList();
        var added = b.Symbols.Values.Where(s => Wanted(s.Name) && !a.Symbols.ContainsKey(s.Name)).ToList();
        var changed = a.Symbols.Values.Where(s => Wanted(s.Name) && b.Symbols.TryGetValue(s.Name, out var t) && t.Function != s.Function).ToList();
        if (!string.Equals(a.Soname, b.Soname, StringComparison.OrdinalIgnoreCase))
        {
            sb.Append($"ELF SONAME changed\n");
            status |= ABIDIFF_ABI_CHANGE;
        }
        if (!noArch && !string.IsNullOrEmpty(a.Architecture) && !string.IsNullOrEmpty(b.Architecture) && a.Architecture != b.Architecture)
        {
            sb.Append($"architecture changed from '{a.Architecture}' to '{b.Architecture}'\n");
            status |= ABIDIFF_ABI_CHANGE | ABIDIFF_ABI_INCOMPATIBLE_CHANGE;
        }
        var neededRemoved = a.Needed.Except(b.Needed, StringComparer.OrdinalIgnoreCase).ToList();
        var neededAdded = b.Needed.Except(a.Needed, StringComparer.OrdinalIgnoreCase).ToList();
        if (removed.Count + added.Count + changed.Count == 0 && status == 0 && neededAdded.Count + neededRemoved.Count == 0)
            return ("", 0);
        int Count(List<Symbol> l, bool f) => l.Count(s => s.Function == f);
        sb.Append("Functions changes summary: 0 Removed, 0 Changed, 0 Added function\n");
        sb.Append("Variables changes summary: 0 Removed, 0 Changed, 0 Added variable\n");
        sb.Append($"Function symbols changes summary: {Count(removed, true)} Removed, {Count(added, true)} Added function symbol{(Count(added, true) == 1 ? "" : "s")} not referenced by debug info\n");
        sb.Append($"Variable symbols changes summary: {Count(removed, false)} Removed, {Count(added, false)} Added variable symbol{(Count(added, false) == 1 ? "" : "s")} not referenced by debug info\n");
        if (changed.Count > 0) sb.Append($"Symbol kind changes summary: {changed.Count} Changed symbol{(changed.Count == 1 ? "" : "s")}\n");
        if (neededRemoved.Count + neededAdded.Count > 0)
            sb.Append($"DT_NEEDED changes summary: {neededRemoved.Count} Removed, {neededAdded.Count} Added DLL dependencies\n");
        if (!statsOnly)
        {
            void Section(List<Symbol> list, bool function, string verb, char tag)
            {
                var items = list.Where(s => s.Function == function).ToList();
                if (items.Count == 0) return;
                var kind = function ? "function" : "variable";
                sb.Append($"\n{items.Count} {verb} {kind} symbol{(items.Count == 1 ? "" : "s")} not referenced by debug info:\n\n");
                foreach (var s in items)
                {
                    var pretty = Demangle(s.Name);
                    sb.Append($"  [{tag}] {s.Name}{(pretty != s.Name ? $"    {{{pretty}}}" : "")}\n");
                }
            }
            Section(removed, true, "Removed", 'D');
            Section(removed, false, "Removed", 'D');
            if (!noAdded)
            {
                Section(added, true, "Added", 'A');
                Section(added, false, "Added", 'A');
            }
            if (changed.Count > 0)
            {
                sb.Append($"\n{changed.Count} symbol{(changed.Count == 1 ? "" : "s")} changed kind:\n\n");
                foreach (var s in changed) sb.Append($"  [C] {s.Name}: {(s.Function ? "function" : "variable")} became {(s.Function ? "variable" : "function")}\n");
            }
            foreach (var d in neededRemoved) sb.Append($"\n  [D] DT_NEEDED {d}\n");
            foreach (var d in neededAdded) sb.Append($"\n  [A] DT_NEEDED {d}\n");
            sb.Append('\n');
        }
        if (removed.Count + added.Count + changed.Count + neededAdded.Count + neededRemoved.Count > 0) status |= ABIDIFF_ABI_CHANGE;
        if (removed.Count + changed.Count > 0) status |= ABIDIFF_ABI_INCOMPATIBLE_CHANGE;
        return (sb.ToString(), status);
    }

    // ---- abicompat ----

    static int RunAbicompat(string[] args)
    {
        var inputs = new List<string>();
        var weak = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: abicompat [options] [application-path] [lib-v1-path [lib-v2-path]]
                         where options can be:
                          --help|-h  display this help message
                          --version|-v  show program version information and exit
                          --weak-mode  check only that the library provides what the application imports
                          --appd|--libd1|--libd2 <path>  accepted (no PDB support)
                        With one library (or --weak-mode), every symbol the application imports from that
                        library must be exported by it. With two, the libraries' ABI changes are reported,
                        restricted to the symbols the application uses.
                        """);
                    return 0;
                case "-v" or "--version": Console.WriteLine($"abicompat: {AbigailVersion}.0 (wintools {Commands.Version})"); return 0;
                case "--weak-mode": weak = true; break;
                case "--appd" or "--libd1" or "--libd2" or "--suppressions" or "--suppr": Next(args, ref i); break;
                case "--list-undefined-symbols" or "-u" or "--show-base-names" or "-b" or "--app-debug-info-dir" or "--no-show-locs" or "--redundant" or "--no-redundant": break;
                default:
                    if (args[i].StartsWith('-')) { Console.Error.WriteLine($"abicompat: unrecognized option: {args[i]}"); return ABIDIFF_USAGE_ERROR; }
                    inputs.Add(args[i]);
                    break;
            }
        }
        if (inputs.Count is < 2 or > 3) { Console.Error.WriteLine("abicompat: an application and one or two libraries are needed (see abicompat --help)"); return ABIDIFF_USAGE_ERROR; }
        PeFile app;
        Corpus lib1, lib2 = null;
        try
        {
            app = PeFile.Open(inputs[0]);
            lib1 = Load(inputs[1]);
            if (inputs.Count == 3) lib2 = Load(inputs[2]);
        }
        catch (Exception e) when (e is IOException or InvalidOperationException)
        {
            Console.Error.WriteLine($"abicompat: {e.Message}");
            return ABIDIFF_ERROR;
        }
        // What the application takes from this library (matched on the DLL name it imports).
        var soname = lib1.Soname;
        var fileName = Path.GetFileName(inputs[1]);
        var used = app.Imports.Where(i => i.Dll.Equals(soname, StringComparison.OrdinalIgnoreCase) || i.Dll.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            .Select(i => i.Name ?? $"#{i.Ordinal}").ToHashSet(StringComparer.Ordinal);
        if (used.Count == 0)
        {
            Console.Error.WriteLine($"abicompat: '{inputs[0]}' does not import anything from '{soname}'");
            return 0;
        }
        if (lib2 == null || weak)
        {
            var target = lib2 ?? lib1;
            var missing = used.Where(n => !target.Symbols.ContainsKey(n)).OrderBy(n => n, StringComparer.Ordinal).ToList();
            if (missing.Count == 0) return 0;
            Console.WriteLine($"functions defined in library '{target.Path}'\nhave sub-types that are different from what application\n'{inputs[0]}' expects:\n");
            Console.WriteLine($"{missing.Count} symbol{(missing.Count == 1 ? "" : "s")} imported by the application not exported by the library:\n");
            foreach (var n in missing) Console.WriteLine($"  [D] {n}");
            return ABIDIFF_ABI_CHANGE | ABIDIFF_ABI_INCOMPATIBLE_CHANGE;
        }
        var (text, status) = Diff(lib1, lib2, false, false, true, used);
        if (status != 0)
            Console.Write($"ELF file '{inputs[0]}' is not ABI compatible with '{inputs[2]}' due to differences with '{inputs[1]}' below:\n{text}");
        return status;
    }

    // ---- vtable-dumper ----

    static int RunVtableDumper(string[] args)
    {
        string input = null;
        bool mangled = false, demangled = false;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help" or "-help":
                    Console.WriteLine("""
                        NAME:
                          Vtable-Dumper - list contents of virtual tables in a DLL

                        USAGE:
                          vtable-dumper [options] PATH

                        OPTIONS:
                          -mangled|--mangled  show mangled symbol names
                          -demangled|--demangled  show de-mangled symbol names (default)
                          -dumpversion|--dumpversion  print the tool version and exit
                          -help|--help  print this help

                        Exported MSVC vftables (??_7Class@@6B@) and MinGW vtables (_ZTV...) are listed;
                        slots are named after the exported function they point to.
                        """);
                    return 0;
                case "-dumpversion" or "--dumpversion": Console.WriteLine("1.2"); return 0;
                case "-v" or "--version" or "-version": Console.WriteLine($"Vtable-Dumper 1.2 (wintools {Commands.Version})"); return 0;
                case "-mangled" or "--mangled": mangled = true; break;
                case "-demangled" or "--demangled": demangled = true; break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg}");
                    input = arg;
                    break;
            }
        }
        if (input == null) throw new UsageException("no input file given (see vtable-dumper --help)");
        if (!mangled && !demangled) demangled = true;
        var pe = PeFile.Open(input);
        var names = new Dictionary<uint, string>();
        foreach (var e in pe.Exports)
            if (e.Name != null && e.Forward == null) names.TryAdd(e.Rva, e.Name);
        var vtables = pe.Exports.Where(e => e.Name != null && e.Forward == null && (e.Name.StartsWith("??_7") || e.Name.StartsWith("_ZTV")))
            .OrderBy(e => e.Name, StringComparer.Ordinal).ToList();
        var starts = vtables.Select(v => v.Rva).ToHashSet();
        string Show(string name) => name == null ? null : mangled && demangled && Demangle(name) is var d && d != name ? $"{d} [{name}]" : demangled ? Demangle(name) : name;
        foreach (var v in vtables)
        {
            var itanium = v.Name.StartsWith("_ZTV");
            var cls = itanium ? ItaniumClass(v.Name[4..]) : MsvcClass(v.Name);
            var lines = new List<string>();
            var rva = v.Rva;
            var ptr = pe.Is64 ? 8u : 4u;
            if (itanium)
            {
                // Offset to top, then the typeinfo pointer, then the virtual functions.
                lines.Add($"{0,-6}(int (*)(...)){pe.ReadPointer(rva)}");
                var ti = pe.ReadPointer(rva + ptr);
                var tiName = ti >= pe.ImageBase && names.TryGetValue((uint)(ti - pe.ImageBase), out var tn) ? tn : null;
                lines.Add($"{ptr,-6}(int (*)(...))(& {(tiName != null ? tiName : $"0x{ti:x}")})");
                rva += 2 * ptr;
            }
            for (var slot = 0; slot < 4096; slot++, rva += ptr)
            {
                if (slot > 0 && starts.Contains(rva)) break;
                if (!pe.Relocated(rva)) break;
                var target = pe.ReadPointer(rva);
                if (target < pe.ImageBase) break;
                var targetRva = (uint)(target - pe.ImageBase);
                if (!pe.IsExecutable(targetRva)) break;
                var offset = (itanium ? 2 * ptr : 0) + (uint)slot * ptr;
                var label = names.TryGetValue(targetRva, out var n) ? Show(n) : $"<rva 0x{targetRva:x}>";
                lines.Add($"{offset,-6}{(itanium ? "(int (*)(...))" : "")}{label}");
            }
            Console.WriteLine($"Vtable for {cls}");
            Console.WriteLine($"{v.Name}: {lines.Count} entries");
            foreach (var l in lines) Console.WriteLine(l);
            Console.WriteLine();
        }
        return 0;
    }

    // "??_7Foo@bar@@6B@" undecorates to "const bar::Foo::`vftable'" (or "...{for `Base'}").
    static string MsvcClass(string symbol)
    {
        var text = Demangle(symbol);
        if (text.StartsWith("const ")) text = text[6..];
        var at = text.IndexOf("::`vftable'", StringComparison.Ordinal);
        return at > 0 ? text[..at] + text[(at + "::`vftable'".Length)..] : text;
    }

    // Enough of the Itanium mangling to name a class: <source-name> or N <source-name>+ E.
    static string ItaniumClass(string mangled)
    {
        var parts = new List<string>();
        var i = 0;
        var nested = mangled.StartsWith('N');
        if (nested) i++;
        while (i < mangled.Length && char.IsDigit(mangled[i]))
        {
            var start = i;
            while (i < mangled.Length && char.IsDigit(mangled[i])) i++;
            var len = int.Parse(mangled[start..i]);
            if (i + len > mangled.Length) return mangled;
            parts.Add(mangled.Substring(i, len));
            i += len;
            if (!nested) break;
        }
        if (parts.Count == 0) return mangled;
        var rest = i < mangled.Length && !(nested && mangled[i] == 'E' && i == mangled.Length - 1) ? mangled[i..] : "";
        return string.Join("::", parts) + (rest.Length > 0 && rest != "E" ? $" [{rest}]" : "");
    }

    // MSVC names through dbghelp's undecorator; anything else is returned as is.
    internal static string Demangle(string name)
    {
        if (!name.StartsWith('?')) return name;
        var buffer = new StringBuilder(2048);
        return UnDecorateSymbolName(name, buffer, buffer.Capacity, 0) > 0 ? buffer.ToString() : name;
    }

    static string Next(string[] args, ref int i) =>
        ++i < args.Length ? args[i] : throw new UsageException($"option {args[i - 1]} needs an argument");

    [DllImport("dbghelp.dll", CharSet = CharSet.Ansi)]
    static extern uint UnDecorateSymbolName(string name, StringBuilder output, int maxLength, uint flags);

    // ---- PE reader (also used by kmod) ----

    internal sealed class PeFile
    {
        public sealed record Export(string Name, uint Ordinal, uint Rva, string Forward);
        public sealed record Import(string Dll, string Name, uint Ordinal, bool Delayed);

        public string Path;
        public ushort Machine;
        public bool Is64;
        public ulong ImageBase;
        public uint SizeOfImage;
        public string ExportName;
        public List<Export> Exports = [];
        public List<Import> Imports = [];
        byte[] data;
        (uint Va, uint VSize, uint Raw, uint RawSize, uint Flags)[] sections = [];
        uint[] dirRva = new uint[16], dirSize = new uint[16];
        HashSet<uint> relocations;

        public string Architecture => Machine switch
        {
            0x8664 => "pe-amd64",
            0x14c => "pe-i386",
            0xaa64 => "pe-arm64",
            0x1c4 => "pe-armnt",
            _ => $"pe-0x{Machine:x}",
        };

        // headersOnly reads just enough for imports/exports of large files (drivers are small anyway).
        public static PeFile Open(string path)
        {
            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch (FileNotFoundException) { throw new IOException($"{path}: No such file"); }
            var pe = new PeFile { Path = path, data = bytes };
            pe.Parse();
            return pe;
        }

        void Parse()
        {
            if (data.Length < 0x40 || data[0] != 'M' || data[1] != 'Z') throw new InvalidOperationException($"{Path}: not a PE file");
            var peOff = (int)U32(0x3c);
            if (peOff <= 0 || peOff + 24 > data.Length || U32(peOff) != 0x00004550) throw new InvalidOperationException($"{Path}: not a PE file");
            Machine = U16(peOff + 4);
            var nSections = U16(peOff + 6);
            var optSize = U16(peOff + 20);
            var opt = peOff + 24;
            var magic = U16(opt);
            Is64 = magic == 0x20b;
            if (magic is not (0x10b or 0x20b)) throw new InvalidOperationException($"{Path}: unknown optional header");
            ImageBase = Is64 ? U64(opt + 24) : U32(opt + 28);
            SizeOfImage = U32(opt + 56);
            var dirs = opt + (Is64 ? 112 : 96);
            var nDirs = Math.Min(16, (int)U32(opt + (Is64 ? 108 : 92)));
            for (var i = 0; i < nDirs; i++) { dirRva[i] = U32(dirs + i * 8); dirSize[i] = U32(dirs + i * 8 + 4); }
            var sect = opt + optSize;
            sections = new (uint, uint, uint, uint, uint)[nSections];
            for (var i = 0; i < nSections; i++)
            {
                var s = sect + i * 40;
                if (s + 40 > data.Length) break;
                sections[i] = (U32(s + 12), U32(s + 8), U32(s + 20), U32(s + 16), U32(s + 36));
            }
            ReadExports();
            ReadImports();
        }

        public bool IsExecutable(uint rva)
        {
            foreach (var s in sections)
                if (rva >= s.Va && rva < s.Va + Math.Max(s.VSize, s.RawSize)) return (s.Flags & 0x20000000) != 0;
            return false;
        }

        int Offset(uint rva)
        {
            foreach (var s in sections)
                if (rva >= s.Va && rva < s.Va + Math.Max(s.VSize, s.RawSize))
                    return rva - s.Va < s.RawSize ? (int)(s.Raw + rva - s.Va) : -1;
            return rva < data.Length && sections.Length == 0 ? (int)rva : -1;
        }

        public ulong ReadPointer(uint rva)
        {
            var o = Offset(rva);
            if (o < 0 || o + (Is64 ? 8 : 4) > data.Length) return 0;
            return Is64 ? U64(o) : U32(o);
        }

        // Base relocations: every absolute address in the image, which is what a vtable slot holds.
        public bool Relocated(uint rva)
        {
            if (relocations == null)
            {
                relocations = [];
                var o = Offset(dirRva[5]);
                var end = o + (int)dirSize[5];
                while (o >= 0 && o + 8 <= end && o + 8 <= data.Length)
                {
                    var page = U32(o);
                    var size = (int)U32(o + 4);
                    if (size < 8) break;
                    for (var e = o + 8; e + 2 <= o + size && e + 2 <= data.Length; e += 2)
                    {
                        var entry = U16(e);
                        if (entry >> 12 is 3 or 10) relocations.Add(page + (uint)(entry & 0xfff));
                    }
                    o += size;
                }
            }
            return relocations.Contains(rva);
        }

        string CString(uint rva)
        {
            var o = Offset(rva);
            if (o < 0) return null;
            var end = o;
            while (end < data.Length && data[end] != 0) end++;
            return Encoding.ASCII.GetString(data, o, end - o);
        }

        void ReadExports()
        {
            if (dirRva[0] == 0) return;
            var o = Offset(dirRva[0]);
            if (o < 0 || o + 40 > data.Length) return;
            ExportName = CString(U32(o + 12));
            var ordinalBase = U32(o + 16);
            var nFunctions = U32(o + 20);
            var nNames = U32(o + 24);
            var functions = Offset(U32(o + 28));
            var namesOff = Offset(U32(o + 32));
            var ordinals = Offset(U32(o + 36));
            if (functions < 0) return;
            var named = new Dictionary<uint, string>();
            for (uint i = 0; i < nNames && namesOff >= 0 && ordinals >= 0; i++)
                named.TryAdd(U16(ordinals + (int)i * 2), CString(U32(namesOff + (int)i * 4)));
            for (uint i = 0; i < nFunctions && functions + i * 4 + 4 <= data.Length; i++)
            {
                var rva = U32(functions + (int)i * 4);
                if (rva == 0) continue;
                string forward = null;
                if (rva >= dirRva[0] && rva < dirRva[0] + dirSize[0]) forward = CString(rva);
                Exports.Add(new Export(named.GetValueOrDefault(i), ordinalBase + i, rva, forward));
            }
        }

        void ReadImports()
        {
            foreach (var (dir, delayed) in new[] { (1, false), (13, true) })
            {
                if (dirRva[dir] == 0) continue;
                var o = Offset(dirRva[dir]);
                var stride = delayed ? 32 : 20;
                for (; o >= 0 && o + stride <= data.Length; o += stride)
                {
                    var nameRva = U32(o + (delayed ? 4 : 12));
                    if (nameRva == 0) break;
                    var dll = CString(nameRva);
                    if (dll == null) break;
                    var thunks = U32(o + (delayed ? 16 : 0));
                    if (thunks == 0 && !delayed) thunks = U32(o + 16);
                    var t = Offset(thunks);
                    var any = false;
                    for (; t >= 0 && t + (Is64 ? 8 : 4) <= data.Length; t += Is64 ? 8 : 4)
                    {
                        var value = Is64 ? U64(t) : U32(t);
                        if (value == 0) break;
                        any = true;
                        var byOrdinal = Is64 ? (value & 0x8000000000000000) != 0 : (value & 0x80000000) != 0;
                        if (byOrdinal) Imports.Add(new Import(dll, null, (uint)(value & 0xffff), delayed));
                        else Imports.Add(new Import(dll, CString((uint)value + 2), 0, delayed));
                    }
                    if (!any) Imports.Add(new Import(dll, null, 0, delayed));
                }
            }
        }

        ushort U16(int o) => BitConverter.ToUInt16(data, o);
        uint U32(int o) => BitConverter.ToUInt32(data, o);
        ulong U64(int o) => BitConverter.ToUInt64(data, o);
    }
}
