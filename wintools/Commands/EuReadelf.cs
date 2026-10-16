using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// eu-readelf: elfutils' ELF dumper. Headers, program headers, sections, symbols (with GNU versions),
// dynamic section, notes, relocations, version sections, string/hex dumps and the DWARF dumps
// (--debug-dump=info|line|loc|abbrev|str) in elfutils' own output format, which tools such as
// abi-dumper parse.
static class EuReadelf
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("eu-readelf", "Print information about ELF files (elfutils)", Run);

    sealed class Options
    {
        public bool Header, Programs, Sections, Symbols, DynSyms, Dynamic, Notes, Relocs, Versions, NoNames;
        public string SymbolSection;
        public List<string> StringDumps = [], HexDumps = [];
        public bool AllStrings;
        public HashSet<string> Debug = [];
    }

    static readonly string[] AllDebug = ["abbrev", "info", "line", "loc", "str"];

    static int Run(string[] args)
    {
        var o = new Options();
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--") { files.AddRange(args[(i + 1)..]); break; }
            string Value(string name)
            {
                if (++i >= args.Length) throw new UsageException($"option '{name}' requires an argument");
                return args[i];
            }
            switch (a)
            {
                case "--help" or "-?":
                    Help();
                    return 0;
                case "--version":
                    Console.WriteLine("eu-readelf (elfutils) 0.196");
                    return 0;
                case "-a" or "--all":
                    o.Header = o.Programs = o.Sections = o.Symbols = o.Dynamic = o.Notes = o.Relocs = o.Versions = true;
                    break;
                case "-h" or "--file-header": o.Header = true; break;
                case "-l" or "--program-headers" or "--segments": o.Programs = true; break;
                case "-S" or "--section-headers" or "--sections": o.Sections = true; break;
                case "-e" or "--headers": o.Header = o.Programs = o.Sections = true; break;
                case "-s" or "--symbols": o.Symbols = true; break;
                case "--dyn-syms": o.DynSyms = true; break;
                case "-d" or "--dynamic": o.Dynamic = true; break;
                case "-n" or "--notes": o.Notes = true; break;
                case "-r" or "--relocs": o.Relocs = true; break;
                case "-V" or "--version-info": o.Versions = true; break;
                case "-N" or "--numeric-addresses": o.NoNames = true; break;
                case "-W" or "--wide" or "-z" or "--decompress" or "-U" or "--unresolved-address-offsets": break;
                case "-w" or "--debug-dump": o.Debug.UnionWith(AllDebug); break;
                case "-p" or "--string-dump" or "--strings": o.AllStrings = true; break;
                case "-x": o.HexDumps.Add(Value("-x")); break;
                default:
                    if (a.StartsWith("--symbols=")) { o.Symbols = true; o.SymbolSection = a[10..]; }
                    else if (a.StartsWith("--debug-dump=") || (a.StartsWith("-w") && a.Length > 2))
                    {
                        var list = a.StartsWith("--") ? a[13..] : a[2..];
                        foreach (var part in list.Split(','))
                        {
                            var name = part switch { "decodedline" or "rawline" => "line", "loclists" => "loc", _ => part };
                            if (name is not ("abbrev" or "info" or "line" or "loc" or "str"))
                                throw new UsageException($"unknown DWARF debug section `{part}'");
                            o.Debug.Add(name);
                        }
                    }
                    else if (a.StartsWith("--strings=") || a.StartsWith("--string-dump=")) o.StringDumps.Add(a[(a.IndexOf('=') + 1)..]);
                    else if (a.StartsWith("-p") && a.Length > 2) o.StringDumps.Add(a[2..]);
                    else if (a.StartsWith("--hex-dump=")) o.HexDumps.Add(a[11..]);
                    else if (a.StartsWith("-x") && a.Length > 2) o.HexDumps.Add(a[2..]);
                    else if (a.StartsWith('-') && a.Length > 1)
                    {
                        // Bundled short options: -hlS, -sW ...
                        var all = a[1..].All(c => "hlSesdnrVNWwaz".Contains(c));
                        if (!all) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                        foreach (var c in a[1..]) ApplyShort(c, o);
                    }
                    else files.Add(a);
                    break;
            }
        }
        if (!(o.Header || o.Programs || o.Sections || o.Symbols || o.DynSyms || o.Dynamic || o.Notes || o.Relocs || o.Versions
              || o.Debug.Count > 0 || o.AllStrings || o.StringDumps.Count > 0 || o.HexDumps.Count > 0))
            throw new UsageException("no operation specified (see eu-readelf --help)");
        if (files.Count == 0) files.Add("a.out");
        var status = 0;
        var w = new StringWriter { NewLine = "\n" };
        var stdout = Console.Out;
        foreach (var file in files)
        {
            ElfKit.Elf elf;
            try { elf = ElfKit.Elf.Load(file); }
            catch (ElfKit.ElfError e)
            {
                Console.Error.WriteLine(e.Message == "not an ELF file"
                    ? $"eu-readelf: failed reading '{file}': not a valid ELF file" : $"eu-readelf: cannot open input file '{file}': {e.Message}");
                status = 1;
                continue;
            }
            if (files.Count > 1) stdout.Write($"\n{file}:\n\n");
            try { Dump(stdout, elf, o); }
            catch (ElfKit.ElfError e)
            {
                stdout.Flush();
                Console.Error.WriteLine($"eu-readelf: {file}: {e.Message}");
                status = 1;
            }
        }
        stdout.Flush();
        return status;
    }

    // Applies one bundled short option (from "-hlS") to the options.
    static void ApplyShort(char c, Options o)
    {
        switch ("-" + c)
        {
            case "-h": o.Header = true; break;
            case "-l": o.Programs = true; break;
            case "-S": o.Sections = true; break;
            case "-e": o.Header = o.Programs = o.Sections = true; break;
            case "-s": o.Symbols = true; break;
            case "-d": o.Dynamic = true; break;
            case "-n": o.Notes = true; break;
            case "-r": o.Relocs = true; break;
            case "-V": o.Versions = true; break;
            case "-N": o.NoNames = true; break;
            case "-w": o.Debug.UnionWith(AllDebug); break;
            case "-a": o.Header = o.Programs = o.Sections = o.Symbols = o.Dynamic = o.Notes = o.Relocs = o.Versions = true; break;
        }
    }

    static void Help() => Console.Write("""
        Usage: eu-readelf [OPTION...] FILE...
        Print information from ELF file in human-readable form.

         ELF input selection:
          -a, --all                  All these plus -p .strtab -p .dynstr -p .comment
          -d, --dynamic              Display the dynamic segment
          -e, --headers              Equivalent to: -h -l -S
          -h, --file-header          Display the ELF file header
          -l, --program-headers      Display the program headers
          -n, --notes                Display the ELF notes
          -r, --relocs               Display relocations
          -S, --section-headers      Display the sections' headers
          -s, --symbols[=SECTION]    Display the symbol table sections
              --dyn-syms             Display (only) the dynamic symbol table
          -V, --version-info         Display versioning information

         Additional output selection:
          -w, --debug-dump[=SECTION] Display DWARF section content.  SECTION can be one
                                     of abbrev, info, line, decodedline, loc, str
          -p, --string-dump[=SECTION], --strings[=SECTION]
                                     Print string contents of sections
          -x, --hex-dump=SECTION     Dump the uninterpreted contents of SECTION, by
                                     number or name
          -N, --numeric-addresses    Do not find symbol names for addresses in DWARF
                                     data
          -W, --wide                 Ignored for compatibility (lines always wide)

          -?, --help                 Give this help list
              --version              Print program version

        """);

    static void Dump(TextWriter w, ElfKit.Elf elf, Options o)
    {
        var names = o.NoNames ? null : new ElfAddressNames(elf);
        if (o.Header) FileHeader(w, elf);
        if (o.Sections) SectionHeaders(w, elf);
        if (o.Programs) ProgramHeaders(w, elf);
        if (o.Relocs) Relocations(w, elf);
        if (o.Dynamic) Dynamic(w, elf);
        if (o.Symbols || o.DynSyms) Symbols(w, elf, o.DynSyms && !o.Symbols ? ".dynsym" : o.SymbolSection);
        if (o.Versions) Versions(w, elf);
        if (o.Notes) Notes(w, elf);
        foreach (var name in o.StringDumps) Strings(w, elf, FindSection(elf, name));
        if (o.AllStrings)
            foreach (var s in elf.Sections.Where(s => s.Type == ElfKit.SHT_STRTAB)) Strings(w, elf, s);
        foreach (var name in o.HexDumps) HexDump(w, elf, FindSection(elf, name));
        if (o.Debug.Count > 0) DebugDumps(w, elf, o.Debug, names);
    }

    static ElfKit.Section FindSection(ElfKit.Elf elf, string name)
    {
        if (int.TryParse(name, out var n) && n >= 0 && n < elf.Sections.Count) return elf.Sections[n];
        return elf.SectionByName(name) ?? throw new ElfKit.ElfError($"cannot get section: {name}");
    }

    static string Hex(ulong v, int width) => "0x" + v.ToString("x").PadLeft(width, '0');

    // ---- -h ----
    static void FileHeader(TextWriter w, ElfKit.Elf e)
    {
        w.WriteLine("ELF Header:");
        w.WriteLine("  Magic:   " + string.Join(" ", e.Data.Take(16).Select(b => b.ToString("x2"))));
        w.WriteLine($"  Class:                             {(e.Is64 ? "ELF64" : "ELF32")}");
        w.WriteLine($"  Data:                              2's complement, {(e.Little ? "little" : "big")} endian");
        w.WriteLine($"  Ident Version:                     {e.IdentVersion}{(e.IdentVersion == 1 ? " (current)" : "")}");
        w.WriteLine($"  OS/ABI:                            {ElfKit.OsAbiName(e.OsAbi)}");
        w.WriteLine($"  ABI Version:                       {e.AbiVersion}");
        w.WriteLine($"  Type:                              {ElfKit.TypeName(e.Type)}");
        w.WriteLine($"  Machine:                           {ElfKit.MachineName(e.Machine)}");
        w.WriteLine($"  Version:                           {e.Version}{(e.Version == 1 ? " (current)" : "")}");
        w.WriteLine($"  Entry point address:               {(e.Entry == 0 ? "0" : $"0x{e.Entry:x}")}");
        w.WriteLine($"  Start of program headers:          {e.PhOff} (bytes into file)");
        w.WriteLine($"  Start of section headers:          {e.ShOff} (bytes into file)");
        w.WriteLine($"  Flags:                             {(e.Flags == 0 ? "" : $"0x{e.Flags:x}")}");
        w.WriteLine($"  Size of this header:               {e.EhSize} (bytes)");
        w.WriteLine($"  Size of program header entries:    {e.PhEntSize} (bytes)");
        w.WriteLine($"  Number of program headers entries: {e.PhNum}");
        w.WriteLine($"  Size of section header entries:    {e.ShEntSize} (bytes)");
        w.WriteLine($"  Number of section headers entries: {e.ShNum}{(e.ShNum == 0 && e.Sections.Count > 0 ? $" ({e.Sections.Count} in [0].sh_size)" : "")}");
        w.WriteLine($"  Section header string table index: {e.ShStrNdx}");
        w.WriteLine();
    }

    // ---- -S ----
    static string SectionFlags(ulong f)
    {
        var sb = new StringBuilder();
        if ((f & 0x1) != 0) sb.Append('W');
        if ((f & 0x2) != 0) sb.Append('A');
        if ((f & 0x4) != 0) sb.Append('X');
        if ((f & 0x10) != 0) sb.Append('M');
        if ((f & 0x20) != 0) sb.Append('S');
        if ((f & 0x40) != 0) sb.Append('I');
        if ((f & 0x80) != 0) sb.Append('L');
        if ((f & 0x100) != 0) sb.Append('N');
        if ((f & 0x200) != 0) sb.Append('G');
        if ((f & 0x400) != 0) sb.Append('T');
        if ((f & 0x800) != 0) sb.Append('C');
        if ((f & 0x80000000) != 0) sb.Append('E');
        return sb.ToString();
    }

    static void SectionHeaders(TextWriter w, ElfKit.Elf e)
    {
        var n = e.Sections.Count;
        w.WriteLine($"There are {n} section headers, starting at offset 0x{e.ShOff:x}:");
        w.WriteLine();
        w.WriteLine("Section Headers:");
        int aw = e.Is64 ? 16 : 8, ow = e.Is64 ? 8 : 6;
        w.WriteLine(e.Is64
            ? "[Nr] Name                 Type         Addr             Off      Size     ES Flags Lk Inf Al"
            : "[Nr] Name                 Type         Addr     Off    Size   ES Flags Lk Inf Al");
        foreach (var s in e.Sections)
        {
            w.WriteLine($"[{s.Index,2}] {s.Name,-20} {ElfKit.SectionTypeName(s.Type),-12} {s.Addr.ToString("x").PadLeft(aw, '0')} " +
                $"{s.Offset.ToString("x").PadLeft(ow, '0')} {s.Size.ToString("x").PadLeft(ow, '0')} {s.EntSize,2} {SectionFlags(s.Flags),-5} " +
                $"{s.Link,2} {s.Info,3} {s.Align,2}");
        }
        w.WriteLine();
    }

    // ---- -l ----
    static void ProgramHeaders(TextWriter w, ElfKit.Elf e)
    {
        if (e.Segments.Count == 0)
        {
            w.WriteLine("\nThere are no program headers in this file.");
            return;
        }
        var aw = e.Is64 ? 16 : 8;
        w.WriteLine("Program Headers:");
        w.WriteLine(e.Is64
            ? "  Type           Offset   VirtAddr           PhysAddr           FileSiz  MemSiz   Flg Align"
            : "  Type           Offset   VirtAddr   PhysAddr   FileSiz  MemSiz   Flg Align");
        foreach (var p in e.Segments)
        {
            var flags = $"{((p.Flags & ElfKit.PF_R) != 0 ? 'R' : ' ')}{((p.Flags & ElfKit.PF_W) != 0 ? 'W' : ' ')}{((p.Flags & ElfKit.PF_X) != 0 ? 'E' : ' ')}";
            w.WriteLine($"  {ElfKit.SegmentTypeName(p.Type),-14} {Hex(p.Offset, 6)} {Hex(p.VAddr, aw)} {Hex(p.PAddr, aw)} {Hex(p.FileSize, 6)} {Hex(p.MemSize, 6)} {flags} 0x{p.Align:x}");
            if (p.Type == ElfKit.PT_INTERP && p.Offset < (ulong)e.Data.Length)
                w.WriteLine($"\t[Requesting program interpreter: {e.CString((long)p.Offset)}]");
        }
        w.WriteLine();
        w.WriteLine(" Section to Segment mapping:");
        w.WriteLine("  Segment Sections...");
        foreach (var p in e.Segments)
        {
            var inside = e.Sections.Where(s => s.Index > 0 && (s.Flags & ElfKit.SHF_ALLOC) != 0 && s.Addr >= p.VAddr
                && s.Addr + (s.Type == ElfKit.SHT_NOBITS ? 0 : s.Size) <= p.VAddr + p.MemSize
                && !(s.Size == 0 && s.Addr == p.VAddr + p.MemSize && p.MemSize > 0)
                && !(p.Type != ElfKit.PT_TLS && (s.Flags & 0x400) != 0 && s.Type == ElfKit.SHT_NOBITS))
                .Select(s => s.Name);
            w.WriteLine($"   {p.Index:00}      {string.Join(" ", inside)}");
        }
    }

    // ---- -r ----
    static string RelocName(ushort machine, uint type) => machine switch
    {
        62 => type switch
        {
            0 => "X86_64_NONE", 1 => "X86_64_64", 2 => "X86_64_PC32", 3 => "X86_64_GOT32", 4 => "X86_64_PLT32", 5 => "X86_64_COPY",
            6 => "X86_64_GLOB_DAT", 7 => "X86_64_JUMP_SLOT", 8 => "X86_64_RELATIVE", 9 => "X86_64_GOTPCREL", 10 => "X86_64_32",
            11 => "X86_64_32S", 16 => "X86_64_DTPMOD64", 17 => "X86_64_DTPOFF64", 18 => "X86_64_TPOFF64", 24 => "X86_64_PC64",
            37 => "X86_64_IRELATIVE", 41 => "X86_64_GOTPCRELX", 42 => "X86_64_REX_GOTPCRELX", _ => $"<unknown {type}>",
        },
        3 => type switch
        {
            0 => "386_NONE", 1 => "386_32", 2 => "386_PC32", 3 => "386_GOT32", 4 => "386_PLT32", 5 => "386_COPY", 6 => "386_GLOB_DAT",
            7 => "386_JMP_SLOT", 8 => "386_RELATIVE", 9 => "386_GOTOFF", 10 => "386_GOTPC", 42 => "386_IRELATIVE", 43 => "386_GOT32X",
            _ => $"<unknown {type}>",
        },
        183 => type switch
        {
            0 => "AARCH64_NONE", 257 => "AARCH64_ABS64", 258 => "AARCH64_ABS32", 1024 => "AARCH64_COPY", 1025 => "AARCH64_GLOB_DAT",
            1026 => "AARCH64_JUMP_SLOT", 1027 => "AARCH64_RELATIVE", 1032 => "AARCH64_IRELATIVE", 283 => "AARCH64_CALL26",
            282 => "AARCH64_JUMP26", 275 => "AARCH64_ADR_PREL_PG_HI21", 277 => "AARCH64_ADD_ABS_LO12_NC", _ => $"<unknown {type}>",
        },
        _ => $"<unknown {type}>",
    };

    static void Relocations(TextWriter w, ElfKit.Elf e)
    {
        var any = false;
        foreach (var rs in e.Sections.Where(s => s.Type is ElfKit.SHT_RELA or ElfKit.SHT_REL))
        {
            any = true;
            var rela = rs.Type == ElfKit.SHT_RELA;
            var entsize = rs.EntSize != 0 ? (long)rs.EntSize : e.Is64 ? (rela ? 24 : 16) : (rela ? 12 : 8);
            var count = (long)rs.Size / entsize;
            var target = rs.Info < e.Sections.Count ? e.Sections[(int)rs.Info] : null;
            var symbols = rs.Link > 0 && rs.Link < e.Sections.Count ? e.Symbols(e.Sections[(int)rs.Link]) : [];
            w.WriteLine();
            w.WriteLine($"Relocation section [{rs.Index,2}] '{rs.Name}' for section [{rs.Info,2}] '{(target?.Index > 0 ? target.Name : "<none>")}' at offset 0x{rs.Offset:x} contains {count} {(count == 1 ? "entry" : "entries")}:");
            var aw = e.Is64 ? 16 : 8;
            w.WriteLine(rela ? (e.Is64 ? "  Offset              Type            Value               Addend Name" : "  Offset      Type            Value       Addend Name")
                             : (e.Is64 ? "  Offset              Type                 Value               Name" : "  Offset      Type                 Value       Name"));
            for (var i = 0; i < count; i++)
            {
                long o = (long)rs.Offset + i * entsize;
                var offset = e.Word(o);
                var info = e.Word(o + e.WordSize);
                var symIndex = e.Is64 ? (long)(info >> 32) : (long)(info >> 8);
                var type = e.Is64 ? (uint)info : (uint)(info & 0xff);
                var sym = symIndex > 0 && symIndex < symbols.Count ? symbols[(int)symIndex] : null;
                var name = sym == null ? "" : sym.Type == 3 && sym.Shndx < e.Sections.Count ? e.Sections[sym.Shndx].Name : sym.Name;
                var value = sym?.Value ?? 0;
                if (rela)
                {
                    var addend = e.Is64 ? (long)e.U64(o + 16) : (int)e.U32(o + 8);
                    w.WriteLine($"  {Hex(offset, aw)}  {RelocName(e.Machine, type),-15} {Hex(value, aw)} {(addend < 0 ? "-" : "+")}{Math.Abs(addend),5} {name}");
                }
                else w.WriteLine($"  {Hex(offset, aw)}  {RelocName(e.Machine, type),-20} {Hex(value, aw)} {name}");
            }
        }
        if (!any) w.WriteLine("\nThere are no relocations in this file.");
    }

    // ---- -d ----
    static void Dynamic(TextWriter w, ElfKit.Elf e)
    {
        var dyns = e.Dynamic();
        if (dyns.Count == 0) return;
        var section = e.DynamicSection;
        var strtab = e.DynStrOffset();
        var count = section != null ? (long)(section.Size / (ulong)(e.Is64 ? 16 : 8)) : dyns.Count;
        w.WriteLine();
        w.WriteLine($"Dynamic segment contains {count} {(count == 1 ? "entry" : "entries")}:");
        if (section != null)
        {
            var link = section.Link < e.Sections.Count ? e.Sections[(int)section.Link].Name : "";
            w.WriteLine($" Addr: {Hex(section.Addr, e.Is64 ? 16 : 8)}  Offset: {Hex(section.Offset, 6)}  Link to section: [{section.Link,2}] '{link}'");
        }
        w.WriteLine("  Type              Value");
        foreach (var d in dyns)
        {
            var name = ElfKit.DynTagName(d.Tag);
            string Str() => strtab >= 0 ? e.CString(strtab + (long)d.Value) : "";
            var value = d.Tag switch
            {
                ElfKit.DT_NEEDED => $"Shared library: [{Str()}]",
                ElfKit.DT_SONAME => $"Library soname: [{Str()}]",
                ElfKit.DT_RPATH => $"Library rpath: [{Str()}]",
                ElfKit.DT_RUNPATH => $"Library runpath: [{Str()}]",
                0 => "",
                2 or 8 or 9 or 10 or 11 or 18 or 19 or 27 or 28 or 33 or 35 or 37 => $"{d.Value} (bytes)",
                0x6ffffffd or 0x6fffffff or 0x6ffffff9 or 0x6ffffffa => $"{d.Value}",
                20 => d.Value == 7 ? "RELA" : d.Value == 17 ? "REL" : $"{d.Value}",
                30 => DynFlags(d.Value, ["ORIGIN", "SYMBOLIC", "TEXTREL", "BIND_NOW", "STATIC_TLS"]),
                0x6ffffffb => DynFlags(d.Value, ["NOW", "GLOBAL", "GROUP", "NODELETE", "LOADFLTR", "INITFIRST", "NOOPEN", "ORIGIN",
                    "DIRECT", "TRANS", "INTERPOSE", "NODEFLIB", "NODUMP", "CONFALT", "ENDFILTEE", "DISPRELDNE", "DISPRELPND",
                    "NODIRECT", "IGNMULDEF", "NOKSYMS", "NOHDR", "EDITED", "NORELOC", "SYMINTPOSE", "GLOBAUDIT", "SINGLETON", "STUB", "PIE"]),
                _ => Hex(d.Value, e.Is64 ? 16 : 8),
            };
            w.WriteLine($"  {name,-17} {value}");
        }
    }

    static string DynFlags(ulong v, string[] names)
    {
        var set = new List<string>();
        for (var i = 0; i < names.Length; i++) if ((v & (1UL << i)) != 0) set.Add(names[i]);
        return string.Join(" ", set);
    }

    // ---- -s ----
    static void Symbols(TextWriter w, ElfKit.Elf e, string only)
    {
        var tables = e.Sections.Where(s => s.Type is ElfKit.SHT_SYMTAB or ElfKit.SHT_DYNSYM).ToList();
        if (only != null) tables = tables.Where(s => s.Name == only || s.Index.ToString() == only).ToList();
        foreach (var table in tables)
        {
            var symbols = e.Symbols(table);
            var strtab = table.Link < e.Sections.Count ? e.Sections[(int)table.Link] : null;
            w.WriteLine();
            w.WriteLine($"Symbol table [{table.Index,2}] '{table.Name}' contains {symbols.Count} {(symbols.Count == 1 ? "entry" : "entries")}:");
            w.WriteLine($" {table.Info} local symbol{(table.Info == 1 ? "" : "s")}  String table: [{table.Link,2}] '{strtab?.Name}'");
            w.WriteLine(e.Is64 ? "  Num:            Value   Size Type    Bind   Vis          Ndx Name" : "  Num:    Value   Size Type    Bind   Vis          Ndx Name");
            foreach (var s in symbols)
            {
                var ndx = s.Shndx switch { 0 => "UNDEF", 0xfff1 => "ABS", 0xfff2 => "COMMON", 0xffff => "XINDEX", _ => s.Shndx.ToString() };
                var name = s.Name;
                if (s.Type == 3 && name.Length == 0 && s.Shndx < e.Sections.Count) name = "";
                if (s.Version != null)
                    name += s.Shndx == 0 || !s.VersionHidden ? (s.Shndx == 0 ? $"@{s.Version} ({s.VersionIndex})" : $"@@{s.Version}") : $"@{s.Version}";
                w.WriteLine($"{s.Index,5}: {s.Value.ToString("x").PadLeft(e.Is64 ? 16 : 8, '0')} {(long)s.Size,6} {ElfKit.SymbolTypeName(s.Type),-7} " +
                    $"{ElfKit.SymbolBindName(s.Bind),-6} {ElfKit.VisibilityName(s.Visibility),-9} {ndx,6} {name}");
            }
        }
    }

    // ---- -V ----
    static void Versions(TextWriter w, ElfKit.Elf e)
    {
        foreach (var s in e.Sections)
        {
            var link = s.Link < e.Sections.Count ? e.Sections[(int)s.Link].Name : "";
            var header = $" Addr: {Hex(s.Addr, e.Is64 ? 16 : 8)}  Offset: {Hex(s.Offset, 6)}  Link to section: [{s.Link,2}] '{link}'";
            if (s.Type == ElfKit.SHT_GNU_versym)
            {
                var count = (int)(s.Size / 2);
                var names = e.VersionNames();
                w.WriteLine();
                w.WriteLine($"Version symbols section [{s.Index,2}] '{s.Name}' contains {count} {(count == 1 ? "entry" : "entries")}:");
                w.WriteLine(header);
                for (var i = 0; i < count; i += 2)
                {
                    var sb = new StringBuilder($" {i,4}:");
                    for (var k = i; k < Math.Min(count, i + 2); k++)
                    {
                        var v = e.U16((long)s.Offset + k * 2);
                        var label = (v & 0x7fff) switch { 0 => "*local*", 1 => "*global*", var x => names.TryGetValue(x, out var n) ? n.Name : "???" };
                        sb.Append($" {v & 0x7fff,2}{((v & 0x8000) != 0 ? 'h' : ' ')}{("(" + label + ")"),-20}");
                    }
                    w.WriteLine(sb.ToString().TrimEnd());
                }
            }
            else if (s.Type == ElfKit.SHT_GNU_verneed)
            {
                w.WriteLine();
                w.WriteLine($"Version needs section [{s.Index,2}] '{s.Name}' contains {s.Info} {(s.Info == 1 ? "entry" : "entries")}:");
                w.WriteLine(header);
                long o = (long)s.Offset;
                for (var n = 0; n < s.Info; n++)
                {
                    var cnt = e.U16(o + 2);
                    w.WriteLine($"  {o - (long)s.Offset:x6}: Version: {e.U16(o)}  File: {e.StringAt((int)s.Link, e.U32(o + 4))}  Cnt: {cnt}");
                    long a = o + e.U32(o + 8);
                    for (var k = 0; k < cnt; k++)
                    {
                        var flags = e.U16(a + 4);
                        w.WriteLine($"  0x{a - (long)s.Offset:x4}: Name: {e.StringAt((int)s.Link, e.U32(a + 8))}  Flags: {(flags == 0 ? "none" : flags == 2 ? "WEAK" : $"0x{flags:x}")}  Version: {e.U16(a + 6)}");
                        var next = e.U32(a + 12);
                        if (next == 0) break;
                        a += next;
                    }
                    var nextNeed = e.U32(o + 12);
                    if (nextNeed == 0) break;
                    o += nextNeed;
                }
            }
            else if (s.Type == ElfKit.SHT_GNU_verdef)
            {
                w.WriteLine();
                w.WriteLine($"Version definition section [{s.Index,2}] '{s.Name}' contains {s.Info} {(s.Info == 1 ? "entry" : "entries")}:");
                w.WriteLine(header);
                long o = (long)s.Offset;
                for (var n = 0; n < s.Info; n++)
                {
                    var flags = e.U16(o + 2);
                    var cnt = e.U16(o + 6);
                    long a = o + e.U32(o + 12);
                    w.WriteLine($"  {o - (long)s.Offset:x6}: Version: {e.U16(o)}  Flags: {(flags == 0 ? "none" : flags == 1 ? "BASE" : flags == 2 ? "WEAK" : $"0x{flags:x}")}  Index: {e.U16(o + 4)}  Cnt: {cnt}  Name: {e.StringAt((int)s.Link, e.U32(a))}");
                    for (var k = 1; k < cnt; k++)
                    {
                        var next = e.U32(a + 4);
                        if (next == 0) break;
                        a += next;
                        w.WriteLine($"  {a - (long)s.Offset:x6}: Parent {k}: {e.StringAt((int)s.Link, e.U32(a))}");
                    }
                    var nextDef = e.U32(o + 16);
                    if (nextDef == 0) break;
                    o += nextDef;
                }
            }
        }
    }

    // ---- -n ----
    static string NoteType(string owner, uint type, ushort elfType) => owner switch
    {
        "GNU" => type switch { 1 => "GNU_ABI_TAG", 2 => "GNU_HWCAP", 3 => "GNU_BUILD_ID", 4 => "GNU_GOLD_VERSION", 5 => "GNU_PROPERTY_TYPE_0", _ => $"<unknown>: {type}" },
        "FDO" => type == 0xcafe1a7e ? "FDO_PACKAGING_METADATA" : $"<unknown>: {type}",
        "stapsdt" => type == 3 ? "Version: 3" : $"Version: {type}",
        "CORE" or "LINUX" when elfType == 4 => type switch { 1 => "PRSTATUS", 2 => "FPREGSET", 3 => "PRPSINFO", 6 => "AUXV", 0x46494c45 => "FILE", _ => $"<unknown>: {type}" },
        _ => type switch { 1 => "VERSION", _ => $"<unknown>: {type}" },
    };

    static void Notes(TextWriter w, ElfKit.Elf e)
    {
        var sections = e.Sections.Where(s => s.Type == ElfKit.SHT_NOTE).ToList();
        IEnumerable<(string Title, ulong Offset, ulong Size, ulong Align)> regions = sections.Count > 0
            ? sections.Select(s => ($"Note section [{s.Index,2}] '{s.Name}' of {s.Size} bytes at offset 0x{s.Offset:x}:", s.Offset, s.Size, s.Align))
            : e.Segments.Where(p => p.Type == ElfKit.PT_NOTE).Select(p => ($"Note segment of {p.FileSize} bytes at offset 0x{p.Offset:x}:", p.Offset, p.FileSize, p.Align));
        foreach (var (title, offset, size, align) in regions)
        {
            w.WriteLine();
            w.WriteLine(title);
            w.WriteLine("  Owner          Data size  Type");
            foreach (var (owner, type, desc) in e.Notes(offset, size, align))
            {
                w.WriteLine($"  {owner,-13} {desc.Length,10}  {NoteType(owner, type, e.Type)}");
                if (owner == "GNU" && type == 3) w.WriteLine($"    Build ID: {Convert.ToHexStringLower(desc)}");
                else if (owner == "GNU" && type == 1 && desc.Length >= 16)
                {
                    var r = new ElfDwarf.Reader(desc, e.Little);
                    var kind = r.U32();
                    w.WriteLine($"    OS: {(kind switch { 0 => "Linux", 1 => "GNU", 2 => "Solaris", 3 => "FreeBSD", _ => $"{kind}" })}, ABI: {r.U32()}.{r.U32()}.{r.U32()}");
                }
                else if (owner == "GNU" && type == 4) w.WriteLine($"    Version: {Encoding.ASCII.GetString(desc).TrimEnd('\0')}");
                else if (owner == "FDO" && type == 0xcafe1a7e) w.WriteLine($"    Packaging Metadata: {Encoding.UTF8.GetString(desc).TrimEnd('\0')}");
            }
        }
    }

    // ---- -p / -x ----
    static void Strings(TextWriter w, ElfKit.Elf e, ElfKit.Section s)
    {
        var data = e.Contents(s);
        w.WriteLine();
        w.WriteLine($"String section [{s.Index,2}] '{s.Name}' contains {data.Length} bytes at offset 0x{s.Offset:x}:");
        var width = Math.Max(1, data.Length.ToString("x").Length);
        var i = 0;
        while (i < data.Length)
        {
            var end = Array.IndexOf(data, (byte)0, i);
            if (end < 0) end = data.Length;
            w.WriteLine($"  [{i.ToString("x").PadLeft(width)}]  {Encoding.UTF8.GetString(data, i, end - i)}");
            i = end + 1;
        }
    }

    static void HexDump(TextWriter w, ElfKit.Elf e, ElfKit.Section s)
    {
        var data = e.Contents(s);
        w.WriteLine();
        w.WriteLine($"Hex dump of section [{s.Index}] '{s.Name}', {data.Length} bytes at offset 0x{s.Offset:x}:");
        var width = Math.Max(4, ((ulong)data.Length).ToString("x").Length);
        for (var i = 0; i < data.Length; i += 16)
        {
            var hex = new StringBuilder();
            var text = new StringBuilder();
            for (var k = 0; k < 16; k++)
            {
                if (i + k < data.Length)
                {
                    hex.Append(data[i + k].ToString("x2"));
                    var c = (char)data[i + k];
                    text.Append(c >= 0x20 && c < 0x7f ? c : '.');
                }
                else hex.Append("  ");
                if (k % 4 == 3) hex.Append(' ');
            }
            w.WriteLine($"  0x{i.ToString("x").PadLeft(width, '0')} {hex}{text}");
        }
    }

    // ---- DWARF ----
    static void DebugDumps(TextWriter w, ElfKit.Elf e, HashSet<string> which, ElfAddressNames names)
    {
        var s = ElfDwarf.Sections.Load(e);
        string Addr(int size, ulong a) => names?.Format(a, size) ?? Hex(a, size * 2);
        if (which.Contains("abbrev")) DumpAbbrev(w, e, s);
        if (which.Contains("info")) DumpInfo(w, e, s, Addr);
        if (which.Contains("line")) DumpLine(w, e, s, Addr);
        if (which.Contains("loc")) DumpLoc(w, e, s, Addr);
        if (which.Contains("str")) DumpStr(w, e);
    }

    static string Title(ElfKit.Elf e, string name)
    {
        var sec = e.SectionByName(name) ?? e.SectionByName(".z" + name[1..]);
        return sec == null ? null : $"\nDWARF section [{sec.Index,2}] '{sec.Name}' at offset 0x{sec.Offset:x}:";
    }

    static void DumpAbbrev(TextWriter w, ElfKit.Elf e, ElfDwarf.Sections s)
    {
        if (s.Abbrev == null) return;
        w.WriteLine(Title(e, ".debug_abbrev"));
        w.WriteLine(" [ Code]");
        var r = new ElfDwarf.Reader(s.Abbrev, s.Little);
        while (!r.AtEnd)
        {
            w.WriteLine();
            w.WriteLine($"Abbreviation section at offset {r.Pos}:");
            while (!r.AtEnd)
            {
                var start = r.Pos;
                var code = r.Uleb();
                if (code == 0) break;
                var tag = r.Uleb();
                var children = r.U8() != 0;
                w.WriteLine($" [{code,5}] offset: {start}, children: {(children ? "yes" : "no")}, tag: {ElfDwarf.TagName(tag)}");
                while (true)
                {
                    var at = r.Pos;
                    var name = r.Uleb();
                    var form = r.Uleb();
                    if (form == ElfDwarf.DW_FORM_implicit_const) r.Sleb();
                    if (name == 0 && form == 0) break;
                    w.WriteLine($"          attr: {ElfDwarf.AttrName(name)}, form: {ElfDwarf.FormName(form)}, offset: 0x{at:x}");
                }
            }
        }
    }

    static void DumpInfo(TextWriter w, ElfKit.Elf e, ElfDwarf.Sections s, Func<int, ulong, string> addr)
    {
        if (s.Info == null && s.Types == null) return;
        var lineCache = new Dictionary<ulong, ElfDwarf.LineTable>();
        var titleShown = false;
        var typesTitleShown = false;
        foreach (var (u, dies) in ElfDwarf.Units(s))
        {
            if (!u.IsTypeSection && !titleShown) { w.WriteLine(Title(e, ".debug_info")); w.WriteLine(" [Offset]"); titleShown = true; }
            if (u.IsTypeSection && !typesTitleShown) { w.WriteLine(Title(e, ".debug_types")); w.WriteLine(" [Offset]"); typesTitleShown = true; }
            if (u.UnitType is 2 or 6 && u.Version < 5)
            {
                w.WriteLine($" Type unit at offset {u.Offset}:");
                w.WriteLine($" Version: {u.Version}, Abbreviation section offset: {u.AbbrevOffset}, Address size: {u.AddressSize}, Offset size: {u.OffsetSize}");
                w.WriteLine($" Type signature: 0x{u.UnitId:x}, Type offset: 0x{u.TypeOffset:x} [{u.Offset + u.TypeOffset,6:x}]");
            }
            else
            {
                w.WriteLine($" {(u.UnitType is 2 or 6 ? "Type" : "Compilation")} unit at offset {u.Offset}:");
                w.WriteLine($" Version: {u.Version}, Abbreviation section offset: {u.AbbrevOffset}, Address size: {u.AddressSize}, Offset size: {u.OffsetSize}");
                if (u.Version >= 5)
                {
                    var extra = u.UnitType is 2 or 4 or 5 or 6 ? $", Unit id: 0x{u.UnitId:x16}" : "";
                    if (u.UnitType is 2 or 6) extra += $", Unit DIE off: 0x{u.Offset + u.TypeOffset:x}";
                    w.WriteLine($" Unit type: {ElfDwarf.UnitTypeName(u.UnitType)} ({u.UnitType}){extra}");
                }
            }
            ElfDwarf.LineTable lines = null;
            if (u.Root?.Get(ElfDwarf.DW_AT_stmt_list) is { } stmt && !lineCache.TryGetValue(stmt.U, out lines))
                lineCache[stmt.U] = lines = SafeLineTable(s, stmt.U, u.AddressSize);
            foreach (var die in dies)
            {
                var indent = new string(' ', die.Level * 2);
                w.WriteLine($" [{die.Offset,6:x}]  {indent}{ElfDwarf.TagName(die.Tag),-20} abbrev: {die.AbbrevCode}");
                foreach (var a in die.Attrs) PrintAttr(w, s, u, die, a, indent, lines, addr);
            }
        }
    }

    static ElfDwarf.LineTable SafeLineTable(ElfDwarf.Sections s, ulong offset, int addressSize)
    {
        try { return ElfDwarf.ReadLineTable(s, offset, addressSize, false); }
        catch (ElfKit.ElfError) { return null; }
    }

    static void PrintAttr(TextWriter w, ElfDwarf.Sections s, ElfDwarf.Unit u, ElfDwarf.Die die, ElfDwarf.Attr a, string indent,
        ElfDwarf.LineTable lines, Func<int, ulong, string> addr)
    {
        var name = ElfDwarf.AttrName(a.Name);
        var form = ElfDwarf.FormName(a.Form);
        var head = $"           {indent}{name,-20} ({form}) ";
        if (ElfDwarf.IsStringForm(a.Form))
        {
            w.WriteLine($"{head}\"{ElfDwarf.String(s, u, a) ?? "???"}\"");
            return;
        }
        if (ElfDwarf.IsAddrForm(a.Form))
        {
            var prefix = a.Form == ElfDwarf.DW_FORM_addr ? "" : $"[{a.U:x}] ";
            w.WriteLine($"{head}{prefix}{addr(u.AddressSize, ElfDwarf.Address(s, u, a))}");
            return;
        }
        if (ElfDwarf.IsRefForm(a.Form))
        {
            w.WriteLine($"{head}{(a.Form == ElfDwarf.DW_FORM_GNU_ref_alt ? "alt " : "")}[{a.U,6:x}]");
            return;
        }
        switch (a.Form)
        {
            case ElfDwarf.DW_FORM_ref_sig8:
                w.WriteLine($"{head}{{{a.U,6:x}}}");
                return;
            case ElfDwarf.DW_FORM_flag:
                w.WriteLine($"{head}{(a.U != 0 ? "yes" : "no")}");
                return;
            case ElfDwarf.DW_FORM_flag_present:
                w.WriteLine($"{head}yes");
                return;
            case ElfDwarf.DW_FORM_exprloc or ElfDwarf.DW_FORM_block or ElfDwarf.DW_FORM_block1 or ElfDwarf.DW_FORM_block2
                or ElfDwarf.DW_FORM_block4 or ElfDwarf.DW_FORM_data16:
                if (a.Form == ElfDwarf.DW_FORM_exprloc || (a.Form != ElfDwarf.DW_FORM_data16 && u.Version < 4 && IsExprAttr(a.Name)))
                {
                    w.WriteLine(head);
                    ElfDwarf.PrintOps(w, a.Block, 12 + die.Level * 2, u, s.Little, v => addr(u.AddressSize, v));
                }
                else w.WriteLine($"{head}{a.Block.Length} byte block: {ElfDwarf.Hex(a.Block)}");
                return;
        }
        // Constants and section offsets.
        var num = a.U;
        switch (a.Name)
        {
            case ElfDwarf.DW_AT_data_member_location when a.Form != ElfDwarf.DW_FORM_sec_offset
                                                         && (u.Version >= 4 || a.Form is not (ElfDwarf.DW_FORM_data4 or ElfDwarf.DW_FORM_data8)):
                w.WriteLine($"{head}{num:x}");
                return;
            case ElfDwarf.DW_AT_location or ElfDwarf.DW_AT_data_member_location or 0x50 or 0x4d or 0x19 or 0x4a or 0x40 or 0x2a
                or 0x48 or 0x46 or 0x2111 or 0x2112 or 0x2113 or 0x2114 or 0x2137:
                w.WriteLine(u.Version < 5 || a.Form == ElfDwarf.DW_FORM_sec_offset
                    ? $"{head}location list [{num,6:x}]" : $"{head}location index [{num,6:x}]");
                return;
            case ElfDwarf.DW_AT_loclists_base:
                w.WriteLine($"{head}location list [{num,6:x}]");
                return;
            case ElfDwarf.DW_AT_ranges or 0x2c:
                w.WriteLine(u.Version < 5 || a.Form == ElfDwarf.DW_FORM_sec_offset
                    ? $"{head}range list [{num,6:x}]" : $"{head}range index [{num,6:x}]");
                return;
            case ElfDwarf.DW_AT_rnglists_base:
                w.WriteLine($"{head}range list [{num,6:x}]");
                return;
            case ElfDwarf.DW_AT_addr_base or ElfDwarf.DW_AT_GNU_addr_base:
                w.WriteLine($"{head}address base [{num,6:x}]");
                return;
            case ElfDwarf.DW_AT_str_offsets_base:
                w.WriteLine($"{head}str offsets base [{num,6:x}]");
                return;
            case ElfDwarf.DW_AT_GNU_dwo_id:
                w.WriteLine($"{head}0x{num:x16}");
                return;
            case ElfDwarf.DW_AT_high_pc when u.Root?.Get(ElfDwarf.DW_AT_low_pc) is not null || die.Get(ElfDwarf.DW_AT_low_pc) is not null:
                var low = die.Get(ElfDwarf.DW_AT_low_pc);
                if (low != null)
                {
                    w.WriteLine($"{head}{num} ({addr(u.AddressSize, ElfDwarf.Address(s, u, low) + num)})");
                    return;
                }
                break;
        }
        string valueName = ElfDwarf.ValueName(a.Name, num);
        if (a.Name is ElfDwarf.DW_AT_decl_file or ElfDwarf.DW_AT_call_file)
        {
            var path = lines?.FilePath(num, false);
            valueName = path == null ? "???" : path[(path.LastIndexOf('/') + 1)..];
        }
        var signed = a.IsSigned;
        var text = signed ? a.S.ToString() : num.ToString();
        w.WriteLine(valueName == null ? $"{head}{text}" : $"{head}{valueName} ({text})");
    }

    static bool IsExprAttr(ulong a) => a is 0x02 or 0x50 or 0x38 or 0x4d or 0x19 or 0x4a or 0x40 or 0x2a or 0x48 or 0x4e or 0x4f
        or 0x0d or 0x0c or 0x2e or 0x0b or 0x51 or 0x37 or 0x22 or 0x2f;

    static void DumpLine(TextWriter w, ElfKit.Elf e, ElfDwarf.Sections s, Func<int, ulong, string> addr)
    {
        if (s.Line == null) return;
        w.WriteLine(Title(e, ".debug_line"));
        // Address size per table comes from its unit (DWARF < 5 has none in the header).
        var unitAddress = new Dictionary<ulong, int>();
        foreach (var (u, _) in ElfDwarf.Units(s, false))
            if (u.Root?.Get(ElfDwarf.DW_AT_stmt_list) is { } st) unitAddress[st.U] = u.AddressSize;
        var defaultSize = e.Is64 ? 8 : 4;
        ulong offset = 0;
        while (offset < (ulong)s.Line.Length)
        {
            var t = ElfDwarf.ReadLineTable(s, offset, unitAddress.GetValueOrDefault(offset, defaultSize), false);
            if (t == null) break;
            var asz = t.AddressSize;
            w.WriteLine();
            w.WriteLine($"Table at offset {offset}:");
            w.WriteLine();
            w.WriteLine($" Length:                         {t.Length}");
            w.WriteLine($" DWARF version:                  {t.Version}");
            w.WriteLine($" Prologue length:                {t.HeaderLength}");
            w.WriteLine($" Address size:                   {asz}");
            w.WriteLine($" Segment selector size:          {t.SegmentSelectorSize}");
            w.WriteLine($" Min instruction length:         {t.MinInstLength}");
            w.WriteLine($" Max operations per instruction: {t.MaxOps}");
            w.WriteLine($" Initial value if 'is_stmt':     {t.DefaultIsStmt}");
            w.WriteLine($" Line base:                      {t.LineBase}");
            w.WriteLine($" Line range:                     {t.LineRange}");
            w.WriteLine($" Opcode base:                    {t.OpcodeBase}");
            w.WriteLine();
            w.WriteLine("Opcodes:");
            var width = t.OpcodeBase.ToString().Length;
            for (var i = 0; i < t.StandardLengths.Length; i++)
                w.WriteLine($"  [{(i + 1).ToString().PadLeft(width)}]  {t.StandardLengths[i]} argument{(t.StandardLengths[i] == 1 ? "" : "s")}");
            w.WriteLine();
            if (t.Version >= 5)
            {
                w.WriteLine("Directory table:");
                w.WriteLine("      [" + string.Join(", ", t.DirFormat.Select(f => $"{LnctName(f.Type)}({ElfDwarf.FormName(f.Form)})")) + "]");
                for (var i = 0; i < t.Dirs.Count; i++)
                    w.WriteLine($" {i,-5} {t.Dirs[i]}{(t.DirOffsets[i] != ulong.MaxValue ? $" ({t.DirOffsets[i]})" : "")}");
                w.WriteLine();
                w.WriteLine("File name table:");
                w.WriteLine("      [" + string.Join(", ", t.FileFormat.Select(f => $"{LnctName(f.Type)}({ElfDwarf.FormName(f.Form)})")) + "]");
                for (var i = 0; i < t.Files.Count; i++)
                {
                    var f = t.Files[i];
                    var fields = new List<string>();
                    foreach (var (type, _) in t.FileFormat)
                    {
                        fields.Add(type switch
                        {
                            1 => f.Name + (f.NameOffset != ulong.MaxValue ? $" ({f.NameOffset})" : ""),
                            2 => $"{f.Dir}",
                            3 => $"{f.MTime}",
                            4 => $"{f.Length}",
                            5 => f.Md5 == null ? "" : Convert.ToHexStringLower(f.Md5),
                            _ => "",
                        });
                    }
                    w.WriteLine($" {i,-5} {string.Join(", ", fields)}");
                }
            }
            else
            {
                w.WriteLine("Directory table:");
                foreach (var d in t.Dirs) w.WriteLine($" {d}");
                w.WriteLine();
                w.WriteLine("File name table:");
                w.WriteLine(" Entry Dir   Time      Size      Name");
                for (var i = 0; i < t.Files.Count; i++)
                    w.WriteLine($" {i + 1,-5} {t.Files[i].Dir,-5} {t.Files[i].MTime,-9} {t.Files[i].Length,-9} {t.Files[i].Name}");
            }
            w.WriteLine();
            w.WriteLine("Line number statements:");
            PrintLineProgram(w, s, t, a => addr(asz, a));
            var next = offset + t.Length + (ulong)(t.OffsetSize == 8 ? 12 : 4);
            if (next <= offset) break;
            offset = next;
        }
    }

    static string LnctName(ulong t) => t switch { 1 => "path", 2 => "directory_index", 3 => "timestamp", 4 => "size", 5 => "MD5", _ => $"??? (0x{t:x})" };

    static void PrintLineProgram(TextWriter w, ElfDwarf.Sections s, ElfDwarf.LineTable t, Func<ulong, string> addr)
    {
        var r = new ElfDwarf.Reader(s.Line, s.Little, t.ProgramStart);
        ulong address = 0;
        long line = 1;
        var isStmt = t.DefaultIsStmt != 0;
        var range = Math.Max((byte)1, t.LineRange);
        while (r.Pos < t.End)
        {
            var at = r.Pos;
            var op = r.U8();
            if (op >= t.OpcodeBase)
            {
                var adj = op - t.OpcodeBase;
                var addrAdvance = (uint)(adj / range * t.MinInstLength);
                var lineAdvance = t.LineBase + adj % range;
                address += addrAdvance;
                line += lineAdvance;
                w.WriteLine($" [{at,6:x}] special opcode {adj}: address+{addrAdvance} = {addr(address)}, line{lineAdvance:+0;-0;+0} = {line}");
                continue;
            }
            switch (op)
            {
                case 0:
                {
                    var len = (int)r.Uleb();
                    var end = r.Pos + len;
                    if (len == 0) { w.WriteLine($" [{at,6:x}] extended opcode 0:  unknown opcode"); break; }
                    var ext = r.U8();
                    w.Write($" [{at,6:x}] extended opcode {ext}: ");
                    switch (ext)
                    {
                        case 1:
                            w.WriteLine(" end of sequence");
                            address = 0; line = 1; isStmt = t.DefaultIsStmt != 0;
                            break;
                        case 2:
                            address = r.Sized(len - 1);
                            w.WriteLine($" set address to {addr(address)}");
                            break;
                        case 3:
                            var name = r.CStr();
                            var dir = r.Uleb(); var mtime = r.Uleb(); var flen = r.Uleb();
                            w.WriteLine($" define new file: dir={dir}, mtime={mtime}, length={flen}, name={name}");
                            break;
                        case 4: w.WriteLine($" set discriminator to {r.Uleb()}"); break;
                        default: w.WriteLine(" unknown opcode"); break;
                    }
                    r.Pos = end;
                    break;
                }
                case 1: w.WriteLine($" [{at,6:x}] copy"); break;
                case 2:
                {
                    var adv = r.Uleb() * t.MinInstLength;
                    address += adv;
                    w.WriteLine($" [{at,6:x}] advance address by {adv} to {addr(address)}");
                    break;
                }
                case 3:
                {
                    var adv = r.Sleb();
                    line += adv;
                    w.WriteLine($" [{at,6:x}] advance line by constant {adv} to {line}");
                    break;
                }
                case 4: w.WriteLine($" [{at,6:x}] set file to {r.Uleb()}"); break;
                case 5: w.WriteLine($" [{at,6:x}] set column to {r.Uleb()}"); break;
                case 6:
                    isStmt = !isStmt;
                    w.WriteLine($" [{at,6:x}] set 'is_stmt' to {(isStmt ? 1 : 0)}");
                    break;
                case 7: w.WriteLine($" [{at,6:x}] set basic block flag"); break;
                case 8:
                {
                    var adv = (uint)((255 - t.OpcodeBase) / range * t.MinInstLength);
                    address += adv;
                    w.WriteLine($" [{at,6:x}] advance address by constant {adv} to {addr(address)}");
                    break;
                }
                case 9:
                {
                    var adv = r.U16();
                    address += adv;
                    w.WriteLine($" [{at,6:x}] advance address by fixed value {adv} to {addr(address)}");
                    break;
                }
                case 10: w.WriteLine($" [{at,6:x}] set prologue end flag"); break;
                case 11: w.WriteLine($" [{at,6:x}] set epilogue begin flag"); break;
                case 12: w.WriteLine($" [{at,6:x}] set isa to {r.Uleb()}"); break;
                default:
                {
                    var n = t.StandardLengths[op - 1];
                    var ps = new List<ulong>();
                    for (var i = 0; i < n; i++) ps.Add(r.Uleb());
                    w.WriteLine($" [{at,6:x}] unknown opcode with {n} parameter{(n == 1 ? "" : "s")}:" + string.Concat(ps.Select(p => $" {p}")));
                    break;
                }
            }
        }
    }

    static void DumpLoc(TextWriter w, ElfKit.Elf e, ElfDwarf.Sections s, Func<int, ulong, string> addr)
    {
        // Units that refer to each list, for base addresses and the address size.
        var units = ElfDwarf.Units(s, false).Select(x => x.Unit).ToList();
        if (s.Loc != null)
        {
            w.WriteLine(Title(e, ".debug_loc"));
            var r = new ElfDwarf.Reader(s.Loc, s.Little);
            var unit = units.FirstOrDefault(u => u.Version < 5) ?? units.FirstOrDefault();
            var asz = unit?.AddressSize ?? (e.Is64 ? 8 : 4);
            var bases = units.Where(u => u.Version < 5).ToList();
            var bu = bases.FirstOrDefault();
            ulong baseAddr = bu?.Root?.Get(ElfDwarf.DW_AT_low_pc) is { } lp ? ElfDwarf.Address(s, bu, lp) : 0;
            if (bu != null) w.WriteLine($"\n CU [{bu.DiesStart,6:x}] base: {addr(asz, baseAddr)}");
            var startOfList = true;
            while (r.Pos + 2 * asz <= s.Loc.Length)
            {
                var at = r.Pos;
                var begin = r.Sized(asz);
                var end = r.Sized(asz);
                var max = asz == 8 ? ulong.MaxValue : uint.MaxValue;
                if (begin == 0 && end == 0)
                {
                    w.WriteLine($" [{at,6:x}] <end of list>");
                    startOfList = true;
                    continue;
                }
                if (begin == max)
                {
                    baseAddr = end;
                    w.WriteLine($" [{at,6:x}] base address\n          {addr(asz, end)}");
                    startOfList = false;
                    continue;
                }
                var len = r.U16();
                var expr = r.Bytes(len);
                w.WriteLine($" [{at,6:x}] range {begin:x}, {end:x}");
                w.WriteLine($"          {addr(asz, baseAddr + begin)}..");
                w.WriteLine($"          {addr(asz, baseAddr + end - 1)}");
                ElfDwarf.PrintOps(w, expr, 11, unit ?? new ElfDwarf.Unit { AddressSize = asz, OffsetSize = 4 }, s.Little, v => addr(asz, v));
                startOfList = false;
            }
            _ = startOfList;
        }
        if (s.LocLists != null)
        {
            w.WriteLine(Title(e, ".debug_loclists"));
            var r = new ElfDwarf.Reader(s.LocLists, s.Little);
            while (r.Pos + 12 <= s.LocLists.Length)
            {
                var tableStart = r.Pos;
                var length = r.InitialLength(out var offsetSize);
                var tableEnd = (int)Math.Min((ulong)s.LocLists.Length, (ulong)r.Pos + length);
                var version = r.U16();
                var asz = r.U8();
                var seg = r.U8();
                var count = r.U32();
                var listsStart = r.Pos;
                w.WriteLine($"Table at Offset 0x{tableStart:x}:");
                w.WriteLine();
                w.WriteLine($" Length:               {length,8}");
                w.WriteLine($" DWARF version:        {version,8}");
                w.WriteLine($" Address size:         {asz,8}");
                w.WriteLine($" Segment size:         {seg,8}");
                w.WriteLine($" Offset entries:       {count,8}");
                var cu = units.FirstOrDefault(u => u.Version >= 5 && (u.LocListsBase == 0 || u.LocListsBase == (ulong)listsStart));
                ulong baseAddr = cu?.Root?.Get(ElfDwarf.DW_AT_low_pc) is { } lp ? ElfDwarf.Address(s, cu, lp) : 0;
                if (cu != null) w.WriteLine($" CU [{(ulong)cu.DiesStart,6:x}] base: {addr(asz, baseAddr)}");
                w.WriteLine();
                if (count > 0)
                {
                    w.WriteLine("  Offsets starting at 0x" + listsStart.ToString("x") + ":");
                    for (var i = 0; i < count; i++) w.WriteLine($"   [{i,6}] 0x{r.Sized(offsetSize):x}");
                    w.WriteLine();
                }
                var unit = cu ?? new ElfDwarf.Unit { AddressSize = asz, OffsetSize = offsetSize, Version = 5 };
                var index = 0;
                var newList = true;
                while (r.Pos < tableEnd)
                {
                    if (newList) { w.WriteLine($"  Offset: {r.Pos:x}, Index: {index++:x}"); newList = false; }
                    var kind = r.U8();
                    void Ops()
                    {
                        var len = (int)r.Uleb();
                        ElfDwarf.PrintOps(w, r.Bytes(len), 8, unit, s.Little, v => addr(asz, v));
                    }
                    switch (kind)
                    {
                        case 0: w.WriteLine("    end_of_list"); newList = true; w.WriteLine(); break;
                        case 1: { var x = r.Uleb(); w.WriteLine($"    base_addressx [{x}]"); break; }
                        case 2: { var a = r.Uleb(); var b = r.Uleb(); w.WriteLine($"    startx_endx {a}, {b}"); Ops(); break; }
                        case 3: { var a = r.Uleb(); var b = r.Uleb(); w.WriteLine($"    startx_length {a}, {b:x}"); Ops(); break; }
                        case 4:
                        {
                            var a = r.Uleb(); var b = r.Uleb();
                            w.WriteLine($"    offset_pair {a:x}, {b:x}");
                            w.WriteLine($"      {addr(asz, baseAddr + a)}..");
                            w.WriteLine($"      {addr(asz, baseAddr + b - 1)}");
                            Ops();
                            break;
                        }
                        case 5: w.WriteLine("    default_location"); Ops(); break;
                        case 6: baseAddr = r.Sized(asz); w.WriteLine($"    base_address 0x{baseAddr:x}"); break;
                        case 7:
                        {
                            var a = r.Sized(asz); var b = r.Sized(asz);
                            w.WriteLine($"    start_end 0x{a:x}, 0x{b:x}");
                            w.WriteLine($"      {addr(asz, a)}..");
                            w.WriteLine($"      {addr(asz, b - 1)}");
                            Ops();
                            break;
                        }
                        case 8:
                        {
                            var a = r.Sized(asz); var b = r.Uleb();
                            w.WriteLine($"    start_length 0x{a:x}, {b:x}");
                            w.WriteLine($"      {addr(asz, a)}..");
                            w.WriteLine($"      {addr(asz, a + b - 1)}");
                            Ops();
                            break;
                        }
                        default: w.WriteLine($"    <unknown location list entry {kind}>"); r.Pos = tableEnd; break;
                    }
                }
                r.Pos = tableEnd;
            }
        }
    }

    static void DumpStr(TextWriter w, ElfKit.Elf e)
    {
        foreach (var name in new[] { ".debug_str", ".debug_line_str" })
        {
            var sec = e.SectionByName(name);
            if (sec == null) continue;
            var data = e.Contents(sec);
            w.WriteLine($"\nDWARF section [{sec.Index,2}] '{sec.Name}' at offset 0x{sec.Offset:x}:");
            w.WriteLine(" Offset  String");
            var i = 0;
            var digits = Math.Max(1, data.Length.ToString("x").Length);
            while (i < data.Length)
            {
                var end = Array.IndexOf(data, (byte)0, i);
                if (end < 0) end = data.Length;
                w.WriteLine($" [{i.ToString("x").PadLeft(digits)}]  \"{Encoding.UTF8.GetString(data, i, end - i)}\"");
                i = end + 1;
            }
        }
    }
}

// Address -> "0x... <symbol+0x..>" for eu-readelf's DWARF dumps (what -N turns off).
sealed class ElfAddressNames
{
    readonly List<(ulong Start, ulong Size, string Name)> symbols = [];
    readonly bool relocatable;

    public ElfAddressNames(ElfKit.Elf elf)
    {
        relocatable = elf.Type == 1;
        var table = elf.Sections.FirstOrDefault(s => s.Type == ElfKit.SHT_SYMTAB) ?? elf.Sections.FirstOrDefault(s => s.Type == ElfKit.SHT_DYNSYM);
        if (table == null) return;
        foreach (var sym in elf.Symbols(table))
            if (sym.Type is 1 or 2 or 6 or 10 && sym.Shndx != 0 && sym.Name.Length > 0)
                symbols.Add((sym.Value, sym.Size, sym.Name));
        symbols.Sort((a, b) => a.Start.CompareTo(b.Start));
    }

    public string Format(ulong address, int size)
    {
        var text = "0x" + address.ToString("x").PadLeft(size * 2, '0');
        if (relocatable) return text;
        (ulong Start, ulong Size, string Name) best = default;
        foreach (var s in symbols)
        {
            if (s.Start > address) break;
            if (address < s.Start + Math.Max(1, s.Size)) best = s;
        }
        if (best.Name == null) return text;
        var off = address - best.Start;
        return off == 0 ? $"{text} <{best.Name}>" : $"{text} <{best.Name}+0x{off:x}>";
    }
}
