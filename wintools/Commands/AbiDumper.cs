using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// abi-dumper: dump the ABI of a shared object from its DWARF debug info, in the Perl Data::Dumper
// format abi-compliance-checker reads: exported functions and variables (SymbolInfo, with
// parameters, return types, source lines), every type they reach (TypeInfo: intrinsics, pointers,
// typedefs, structs/classes/unions with member offsets, enums with values, arrays, function
// pointers, cv-qualifiers), needed libraries, sources and symbol lists. Reads ELF objects (e.g.
// Linux libraries being checked on Windows) and Windows DLLs built by MinGW-w64 GCC/Clang with -g,
// which carry DWARF in PE sections; their exports come from the PE export table. MSVC's PDB debug
// info is a different format and is not read.
static class AbiDumper
{
    const string Version = "1.4";
    const string DumpVersion = "3.5";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("abi-dumper", "Dump the ABI of an ELF or PE (MinGW) object with DWARF debug info", Run);

    static int Run(string[] args)
    {
        string input = null, output = "ABI.dump", lver = null, lname = null;
        var all = false;
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "-h" or "-help" or "--help":
                    Console.WriteLine("""
                        ABI Dumper 1.4
                        Dump ABI of an ELF or PE object containing DWARF debug info

                        Usage: abi-dumper [options] [object path]
                        Example:
                          abi-dumper libTest.so -o ABI.dump
                          abi-dumper test.dll -lver 1.0

                          -o, -dump-path PATH   write the dump here (default: ./ABI.dump)
                          -lver, -vnum NUM      library version recorded in the dump
                          -lname NAME           library name recorded in the dump
                          -all                  dump all symbols with debug info, not only exported ones
                          -v, -version          print the version
                        """);
                    return 0;
                case "-v" or "-version" or "--version": Console.WriteLine($"ABI Dumper {Version}"); return 0;
                case "-o" or "-dump-path" or "-output": output = Value(); break;
                case "-lver" or "-vnum": lver = Value(); break;
                case "-lname": lname = Value(); break;
                case "-all" or "-all-symbols": all = true; break;
                case "-public-headers" or "-ignore-tags" or "-symbols-list" or "-search-debuginfo" or "-debuginfo-dir" or "-kernel-export": Value(); break;
                case "-sort" or "-quiet" or "-bin-only" or "-extra-info" or "-mixed-headers" or "-reimplement-std" or "-lambda" or "-loud": break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"unknown option {args[i]}");
                    input = args[i];
                    break;
            }
        }
        if (input == null) throw new UsageException("object path is not specified (see abi-dumper -help)");
        if (!File.Exists(input)) { Console.Error.WriteLine($"ERROR: can't access '{input}'"); return 4; }
        var data = File.ReadAllBytes(input);
        Image image;
        try
        {
            image = ElfKit.Elf.IsElf(data) ? FromElf(new ElfKit.Elf(input, data))
                : data.Length > 0x40 && data[0] == 'M' && data[1] == 'Z' ? FromPe(input, data)
                : throw new ElfKit.ElfError("not an ELF or PE object");
        }
        catch (ElfKit.ElfError e) { Console.Error.WriteLine($"ERROR: {input}: {e.Message}"); return 4; }
        if (image.Dwarf.Info == null)
        {
            Console.Error.WriteLine($"ERROR: can't find debug info in object(s)");
            return 2;
        }
        var dump = Build(image, Path.GetFileName(input), lname, lver, all);
        File.WriteAllText(output, "$VAR1 = " + Perl(dump, 0) + ";\n");
        Console.WriteLine($"Creating ABI dump ...\nThe object ABI has been dumped to:\n  {output}");
        return 0;
    }

    sealed class Image
    {
        public ElfDwarf.Sections Dwarf;
        public HashSet<string> Exports = [];
        public HashSet<string> Undefined = [];
        public List<string> Needed = [];
        public string Arch, Target;
        public int WordSize;
        public string SoName;
    }

    static Image FromElf(ElfKit.Elf elf)
    {
        var img = new Image { Dwarf = ElfDwarf.Sections.Load(elf), WordSize = elf.WordSize, Target = "unix" };
        img.Arch = elf.Machine switch { 62 => "x86_64", 3 => "x86", 183 => "aarch64", 40 => "arm", 243 => "riscv64", 8 => "mips", 21 => "ppc64", 20 => "ppc", 22 => "s390", _ => ElfKit.MachineName(elf.Machine) };
        var table = elf.Sections.FirstOrDefault(s => s.Type == ElfKit.SHT_DYNSYM) ?? elf.Sections.FirstOrDefault(s => s.Type == ElfKit.SHT_SYMTAB);
        if (table != null)
            foreach (var s in elf.Symbols(table))
            {
                if (s.Name.Length == 0 || s.Type is not (1 or 2 or 10)) { if (s.Shndx == 0 && s.Name.Length > 0) img.Undefined.Add(s.Name); continue; }   // OBJECT, FUNC, IFUNC
                if (s.Shndx == 0) img.Undefined.Add(s.Name);
                else if (s.Bind is 1 or 2 && s.Visibility is 0 or 3) img.Exports.Add(s.Name);
            }
        foreach (var d in elf.Dynamic())
        {
            if (d.Tag == ElfKit.DT_NEEDED) img.Needed.Add(elf.CString(elf.DynStrOffset() + (long)d.Value));
            if (d.Tag == ElfKit.DT_SONAME) img.SoName = elf.CString(elf.DynStrOffset() + (long)d.Value);
        }
        return img;
    }

    // MinGW DLLs and EXEs keep DWARF in .debug_* sections (long names through the COFF string
    // table); they are repackaged as a small in-memory ELF image for the shared DWARF reader.
    static Image FromPe(string path, byte[] d)
    {
        var pe = BinaryPrimitives.ReadInt32LittleEndian(d.AsSpan(0x3c));
        if (pe <= 0 || pe + 24 > d.Length || BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(pe)) != 0x4550) throw new ElfKit.ElfError("not a PE file");
        var machine = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(pe + 4));
        var count = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(pe + 6));
        var symtab = BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(pe + 12));
        var nsyms = BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(pe + 16));
        var optSize = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(pe + 20));
        var opt = pe + 24;
        var pe64 = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(opt)) == 0x20b;
        var strtab = symtab + nsyms * 18;
        var sections = new List<(string Name, uint Rva, uint VSize, uint Raw, uint RawSize)>();
        for (var i = 0; i < count; i++)
        {
            var h = opt + optSize + i * 40;
            var name = Encoding.ASCII.GetString(d, h, 8).TrimEnd('\0');
            if (name.StartsWith('/') && uint.TryParse(name[1..], out var off) && strtab + off < d.Length) name = MachO.CString(d, (int)(strtab + off));
            sections.Add((name, BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(h + 12)), BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(h + 8)),
                          BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(h + 20)), BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(h + 16))));
        }
        byte[] Section((string Name, uint Rva, uint VSize, uint Raw, uint RawSize) s)
        {
            var len = (int)Math.Min(s.VSize == 0 ? s.RawSize : s.VSize, s.RawSize);
            return s.Raw + len <= d.Length ? d.AsSpan((int)s.Raw, len).ToArray() : [];
        }
        long Offset(uint rva)
        {
            foreach (var s in sections) if (rva >= s.Rva && rva < s.Rva + Math.Max(s.VSize, s.RawSize)) return s.Raw + (rva - s.Rva);
            return -1;
        }
        string StringAt(uint rva) => Offset(rva) is var o and >= 0 ? MachO.CString(d, (int)o) : "";

        var debug = sections.Where(s => s.Name.StartsWith(".debug_")).Select(s => (s.Name, Data: Section(s))).ToList();
        var elf = new ElfKit.Elf(path, ElfContainer(debug, pe64));
        var img = new Image
        {
            Dwarf = ElfDwarf.Sections.Load(elf),
            WordSize = pe64 ? 8 : 4,
            Target = "windows",
            Arch = machine switch { 0x8664 => "x86_64", 0x14c => "x86", 0xaa64 => "aarch64", 0x1c4 => "arm", _ => $"pe-{machine:x}" },
        };
        var dirs = opt + (pe64 ? 112 : 96);
        // Export directory: DLL name, then the name pointer table.
        var exportRva = BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(dirs));
        if (exportRva != 0 && Offset(exportRva) is var e and >= 0)
        {
            img.SoName = StringAt(BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan((int)e + 12)));
            var names = BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan((int)e + 24));
            var table = Offset(BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan((int)e + 32)));
            for (var i = 0; i < names && table >= 0; i++)
                img.Exports.Add(StringAt(BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan((int)table + i * 4))));
        }
        // Import descriptors: the DLLs this one needs (and their imported names as undefined symbols).
        var importRva = BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(dirs + 8));
        if (importRva != 0 && Offset(importRva) is var imp and >= 0)
            for (var p = (int)imp; p + 20 <= d.Length; p += 20)
            {
                var nameRva = BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p + 12));
                if (nameRva == 0) break;
                img.Needed.Add(StringAt(nameRva));
                var thunks = BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p)) is var orig and not 0 ? orig : BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p + 16));
                var t = Offset(thunks);
                for (var k = 0; t >= 0 && k < 100000; k++)
                {
                    var entry = pe64 ? BinaryPrimitives.ReadUInt64LittleEndian(d.AsSpan((int)t + k * 8)) : BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan((int)t + k * 4));
                    if (entry == 0) break;
                    var ordinal = pe64 ? (entry >> 63) != 0 : (entry >> 31) != 0;
                    if (!ordinal) img.Undefined.Add(StringAt((uint)entry + 2));
                }
            }
        return img;
    }

    // ELF64 (or ELF32) little-endian image holding only the given sections, plus .shstrtab.
    static byte[] ElfContainer(List<(string Name, byte[] Data)> sections, bool is64)
    {
        var shstr = new MemoryStream();
        shstr.WriteByte(0);
        var nameOffsets = new List<int>();
        foreach (var (name, _) in sections) { nameOffsets.Add((int)shstr.Length); shstr.Write(Encoding.ASCII.GetBytes(name)); shstr.WriteByte(0); }
        var shstrName = (int)shstr.Length;
        shstr.Write(".shstrtab\0"u8);
        var all = sections.Select(s => s.Data).Append(shstr.ToArray()).ToList();
        var ehsize = is64 ? 64 : 52;
        var shentsize = is64 ? 64 : 40;
        var offsets = new List<long>();
        long pos = ehsize;
        foreach (var data in all) { pos = (pos + 7) & ~7L; offsets.Add(pos); pos += data.Length; }
        pos = (pos + 7) & ~7L;
        var shoff = pos;
        var shnum = all.Count + 1;
        var image = new byte[shoff + shnum * shentsize];
        image[0] = 0x7f; image[1] = (byte)'E'; image[2] = (byte)'L'; image[3] = (byte)'F';
        image[4] = (byte)(is64 ? 2 : 1); image[5] = 1; image[6] = 1;
        var w = image.AsSpan();
        BinaryPrimitives.WriteUInt16LittleEndian(w[16..], 2);    // ET_EXEC: no relocations to apply
        BinaryPrimitives.WriteUInt32LittleEndian(w[20..], 1);
        if (is64)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(w[40..], (ulong)shoff);
            BinaryPrimitives.WriteUInt16LittleEndian(w[52..], (ushort)ehsize);
            BinaryPrimitives.WriteUInt16LittleEndian(w[58..], (ushort)shentsize);
            BinaryPrimitives.WriteUInt16LittleEndian(w[60..], (ushort)shnum);
            BinaryPrimitives.WriteUInt16LittleEndian(w[62..], (ushort)(shnum - 1));
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(w[32..], (uint)shoff);
            BinaryPrimitives.WriteUInt16LittleEndian(w[40..], (ushort)ehsize);
            BinaryPrimitives.WriteUInt16LittleEndian(w[46..], (ushort)shentsize);
            BinaryPrimitives.WriteUInt16LittleEndian(w[48..], (ushort)shnum);
            BinaryPrimitives.WriteUInt16LittleEndian(w[50..], (ushort)(shnum - 1));
        }
        for (var i = 0; i < all.Count; i++)
        {
            all[i].CopyTo(image, offsets[i]);
            var h = w[(int)(shoff + (i + 1) * shentsize)..];
            var nameOffset = i < sections.Count ? nameOffsets[i] : shstrName;
            BinaryPrimitives.WriteUInt32LittleEndian(h, (uint)nameOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(h[4..], i < sections.Count ? 1u : 3u);   // PROGBITS / STRTAB
            if (is64)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(h[24..], (ulong)offsets[i]);
                BinaryPrimitives.WriteUInt64LittleEndian(h[32..], (ulong)all[i].Length);
                BinaryPrimitives.WriteUInt64LittleEndian(h[48..], 1);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(h[16..], (uint)offsets[i]);
                BinaryPrimitives.WriteUInt32LittleEndian(h[20..], (uint)all[i].Length);
                BinaryPrimitives.WriteUInt32LittleEndian(h[32..], 1);
            }
        }
        return image;
    }

    // ---- the dump ----

    const ulong TagArray = 0x01, TagClass = 0x02, TagEnum = 0x04, TagFormalParameter = 0x05, TagMember = 0x0d, TagPointer = 0x0f,
                TagReference = 0x10, TagCompileUnit = 0x11, TagStruct = 0x13, TagSubroutineType = 0x15, TagTypedef = 0x16, TagUnion = 0x17,
                TagUnspecifiedParams = 0x18, TagInheritance = 0x1c, TagSubrange = 0x21, TagBaseType = 0x24, TagConst = 0x26, TagEnumerator = 0x28,
                TagSubprogram = 0x2e, TagVariable = 0x34, TagVolatile = 0x35, TagRestrict = 0x37, TagNamespace = 0x39, TagRvalueReference = 0x42,
                TagAtomic = 0x47, TagUnspecifiedType = 0x3b;
    const ulong AtName = 0x03, AtByteSize = 0x0b, AtLowPc = 0x11, AtLanguage = 0x13, AtProducer = 0x25, AtUpperBound = 0x2f, AtCount = 0x37,
                AtConstValue = 0x1c, AtDataMemberLocation = 0x38, AtDeclFile = 0x3a, AtDeclLine = 0x3b, AtDeclaration = 0x3c, AtExternal = 0x3f,
                AtSpecification = 0x47, AtType = 0x49, AtLinkageName = 0x6e, AtMipsLinkageName = 0x2007, AtStmtList = 0x10, AtCompDir = 0x1b,
                AtAbstractOrigin = 0x31;

    sealed class Ctx
    {
        public Image Img;
        public Dictionary<ulong, ElfDwarf.Die> Dies = [];
        public Dictionary<ulong, List<ElfDwarf.Die>> Children = [];
        public Dictionary<ulong, ElfDwarf.Die> Parent = [];
        public OrderedDictionary<string, object> Types = [];
        public Dictionary<ElfDwarf.Unit, ElfDwarf.LineTable> Lines = [];
        public HashSet<string> Sources = [];
        public HashSet<string> Headers = [];
        public string Language = "C";
        public string Producer;
    }

    static OrderedDictionary<string, object> Build(Image img, string file, string lname, string lver, bool all)
    {
        var c = new Ctx { Img = img };
        foreach (var (unit, dies) in ElfDwarf.Units(img.Dwarf))
        {
            var stack = new List<ElfDwarf.Die>();
            foreach (var die in dies)
            {
                c.Dies[die.Offset] = die;
                while (stack.Count > die.Level) stack.RemoveAt(stack.Count - 1);
                if (stack.Count > 0)
                {
                    c.Parent[die.Offset] = stack[^1];
                    if (!c.Children.TryGetValue(stack[^1].Offset, out var list)) c.Children[stack[^1].Offset] = list = [];
                    list.Add(die);
                }
                if (die.HasChildren) stack.Add(die);
            }
            if (unit.Root is { } cu)
            {
                var name = Str(c, cu, AtName);
                if (name != null) c.Sources.Add(Path.GetFileName(name.Replace('\\', '/')));
                c.Producer ??= Str(c, cu, AtProducer);
                if (cu.Get(AtLanguage)?.U is 0x4 or 0x1a or 0x21 or 0x2b) c.Language = "C++";   // C++, C++03/11/14
                if (cu.Get(AtStmtList) is { } stmt)
                    c.Lines[unit] = ElfDwarf.ReadLineTable(img.Dwarf, stmt.U, unit.AddressSize, false, Str(c, cu, AtCompDir));
            }
        }

        var symbolInfo = new OrderedDictionary<string, object>();
        var seen = new HashSet<string>();
        foreach (var die in c.Dies.Values.Where(d => d.Tag is TagSubprogram or TagVariable).OrderBy(d => d.Offset))
        {
            // Definitions point at their declaration through DW_AT_specification/abstract_origin.
            var decl = die.Get(AtSpecification) is { } spec && c.Dies.TryGetValue(spec.U, out var sd) ? sd
                : die.Get(AtAbstractOrigin) is { } ao && c.Dies.TryGetValue(ao.U, out var ad) ? ad : die;
            var name = Str(c, decl, AtName);
            if (name == null) continue;
            var mangled = Str(c, die, AtLinkageName) ?? Str(c, die, AtMipsLinkageName) ?? Str(c, decl, AtLinkageName) ?? Str(c, decl, AtMipsLinkageName);
            var symbol = mangled ?? name;
            if (die.Tag == TagVariable && c.Parent.TryGetValue(die.Offset, out var vp) && vp.Tag == TagSubprogram) continue;   // locals
            if (die.Get(AtDeclaration) != null) continue;
            if (die.Tag == TagSubprogram && die.Get(AtLowPc) == null && die.Get(0x55) == null && !all) continue;            // no code: not a definition
            var exported = img.Exports.Contains(symbol) || img.Exports.Contains("_" + symbol);
            if (!all && !exported) continue;
            if (!seen.Add(symbol)) continue;
            var info = new OrderedDictionary<string, object>();
            var line = (decl.Get(AtDeclLine) ?? die.Get(AtDeclLine))?.U;
            var src = SourceFile(c, decl);
            if (src != null)
            {
                var isHeader = src.EndsWith(".h") || src.EndsWith(".hpp") || src.EndsWith(".hh") || src.EndsWith(".hxx");
                info[isHeader ? "Header" : "Source"] = Path.GetFileName(src);
                if (isHeader) c.Headers.Add(Path.GetFileName(src)); else c.Sources.Add(Path.GetFileName(src));
                if (line is { } l) info[isHeader ? "Line" : "SourceLine"] = l.ToString();
            }
            if (mangled != null && mangled != name) info["MnglName"] = mangled;
            info["ShortName"] = name;
            if (c.Parent.TryGetValue(decl.Offset, out var owner) && owner.Tag is TagStruct or TagClass or TagUnion)
                info["Class"] = TypeId(c, owner);
            if (c.Parent.TryGetValue(decl.Offset, out var nsDie) && nsDie.Tag == TagNamespace && Str(c, nsDie, AtName) is { } ns)
                info["NameSpace"] = ns;
            if (die.Tag == TagVariable)
            {
                info["Data"] = "1";
                info["Return"] = TypeOf(c, decl);
            }
            else
            {
                info["Return"] = TypeOf(c, decl);
                var parms = new OrderedDictionary<string, object>();
                var pos = 0;
                foreach (var p in c.Children.GetValueOrDefault(decl.Offset) ?? [])
                {
                    if (p.Tag == TagUnspecifiedParams) { info["Variadic"] = "1"; continue; }
                    if (p.Tag != TagFormalParameter) continue;
                    var pi = new OrderedDictionary<string, object>();
                    var pname = Str(c, p, AtName);
                    if (pname == "this" || p.Get(0x34) != null) { info["Class"] ??= ThisClass(c, p); continue; }   // DW_AT_artificial
                    pi["name"] = pname ?? $"p{pos + 1}";
                    pi["type"] = TypeOf(c, p);
                    parms[pos.ToString()] = pi;
                    pos++;
                }
                if (parms.Count > 0) info["Param"] = parms;
            }
            symbolInfo[die.Offset.ToString()] = info;
        }

        var libName = lname ?? img.SoName ?? file;
        var exportsList = new OrderedDictionary<string, object>();
        foreach (var e in img.Exports.Order(StringComparer.Ordinal)) exportsList[e] = "1";
        var undefined = new OrderedDictionary<string, object>();
        foreach (var u in img.Undefined.Order(StringComparer.Ordinal)) undefined[u] = "0";
        var needed = new OrderedDictionary<string, object>();
        foreach (var n in img.Needed.Distinct().Order(StringComparer.Ordinal)) needed[n] = "1";
        var sources = new OrderedDictionary<string, object>();
        foreach (var s in c.Sources.Order(StringComparer.Ordinal)) sources[s] = "1";
        var headers = new OrderedDictionary<string, object>();
        foreach (var h in c.Headers.Order(StringComparer.Ordinal)) headers[h] = "1";

        return new OrderedDictionary<string, object>
        {
            ["ABI_DUMPER_VERSION"] = Version,
            ["ABI_DUMP_VERSION"] = DumpVersion,
            ["Arch"] = img.Arch,
            ["Compiler"] = c.Producer,
            ["GccVersion"] = c.Producer is { } producer && System.Text.RegularExpressions.Regex.Match(producer, @"\d+\.\d+(\.\d+)?") is { Success: true } m ? m.Value : null,
            ["Headers"] = headers,
            ["Language"] = c.Language,
            ["LibraryName"] = libName,
            ["LibraryVersion"] = lver ?? GuessVersion(libName),
            ["MissedOffsets"] = "1",
            ["MissedRegs"] = "1",
            ["NameSpaces"] = new OrderedDictionary<string, object>(),
            ["Needed"] = needed,
            ["PublicABI"] = all ? "0" : "1",
            ["Sources"] = sources,
            ["SymbolInfo"] = symbolInfo,
            ["SymbolVersion"] = new OrderedDictionary<string, object>(),
            ["Symbols"] = new OrderedDictionary<string, object> { [libName] = exportsList },
            ["Target"] = img.Target,
            ["TypeInfo"] = c.Types,
            ["UndefinedSymbols"] = new OrderedDictionary<string, object> { [libName] = undefined },
            ["WordSize"] = img.WordSize.ToString(),
        };
    }

    // libfoo.so.1.2.3 -> 1.2.3; foo-2.1.dll -> 2.1.
    static string GuessVersion(string name)
    {
        var m = System.Text.RegularExpressions.Regex.Match(name, @"\.so\.([\d.]+)$|[-_]([\d.]+\d)\.(?:dll|so|dylib)$");
        return m.Success ? (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) : "";
    }

    static string Str(Ctx c, ElfDwarf.Die die, ulong attr) => die.Get(attr) is { } a ? ElfDwarf.String(c.Img.Dwarf, die.Unit, a) : null;

    static string SourceFile(Ctx c, ElfDwarf.Die die)
    {
        if (die.Get(AtDeclFile) is not { } f || !c.Lines.TryGetValue(die.Unit, out var lt) || lt == null) return null;
        return lt.FilePath(f.U, false);
    }

    static string ThisClass(Ctx c, ElfDwarf.Die p)
    {
        // `this` is a pointer to the class: its target type id.
        var t = p.Get(AtType) is { } a && c.Dies.TryGetValue(a.U, out var td) ? td : null;
        while (t != null && t.Tag is TagPointer or TagConst or TagVolatile && t.Get(AtType) is { } next && c.Dies.TryGetValue(next.U, out var nt)) t = nt;
        return t == null ? null : TypeId(c, t);
    }

    static string TypeOf(Ctx c, ElfDwarf.Die die) =>
        die.Get(AtType) is { } a && c.Dies.TryGetValue(a.U, out var t) ? TypeId(c, t) : VoidId(c);

    static string VoidId(Ctx c)
    {
        if (!c.Types.ContainsKey("1")) c.Types["1"] = new OrderedDictionary<string, object> { ["Name"] = "void", ["Type"] = "Intrinsic" };
        return "1";
    }

    // Adds the type (and what it refers to) to TypeInfo, returning its id (the DIE offset).
    static string TypeId(Ctx c, ElfDwarf.Die die)
    {
        // Declarations resolve to their definitions where one exists.
        var id = die.Offset.ToString();
        if (c.Types.ContainsKey(id)) return id;
        var t = new OrderedDictionary<string, object>();
        c.Types[id] = t;   // placed first: recursive types refer back to it
        string Base() => TypeOf(c, die);
        string BaseName() => Name(c, Base());
        var name = Str(c, die, AtName);
        var size = die.Get(AtByteSize)?.U;
        switch (die.Tag)
        {
            case TagBaseType: t["Name"] = name; t["Type"] = "Intrinsic"; break;
            case TagUnspecifiedType: t["Name"] = name ?? "void"; t["Type"] = "Intrinsic"; break;
            case TagPointer:
            {
                var b = Base();
                t["BaseType"] = b;
                t["Name"] = Name(c, b) + "*";
                t["Type"] = c.Types[b] is OrderedDictionary<string, object> bt && (string)bt["Type"] == "Func" ? "FuncPtr" : "Pointer";
                size ??= (ulong)c.Img.WordSize;
                break;
            }
            case TagReference: t["BaseType"] = Base(); t["Name"] = BaseName() + "&"; t["Type"] = "Ref"; size ??= (ulong)c.Img.WordSize; break;
            case TagRvalueReference: t["BaseType"] = Base(); t["Name"] = BaseName() + "&&"; t["Type"] = "RvalueRef"; size ??= (ulong)c.Img.WordSize; break;
            case TagConst: t["BaseType"] = Base(); t["Name"] = BaseName() + " const"; t["Type"] = "Const"; break;
            case TagVolatile: t["BaseType"] = Base(); t["Name"] = BaseName() + " volatile"; t["Type"] = "Volatile"; break;
            case TagRestrict: t["BaseType"] = Base(); t["Name"] = BaseName() + " restrict"; t["Type"] = "Restrict"; break;
            case TagAtomic: t["BaseType"] = Base(); t["Name"] = "_Atomic " + BaseName(); t["Type"] = "Const"; break;
            case TagTypedef:
                t["BaseType"] = Base();
                t["Name"] = Qualified(c, die, name);
                t["Type"] = "Typedef";
                Decl(c, die, t);
                break;
            case TagStruct or TagClass or TagUnion:
            {
                var kind = die.Tag == TagStruct ? "Struct" : die.Tag == TagClass ? "Class" : "Union";
                var prefix = c.Language == "C" ? (die.Tag == TagUnion ? "union " : "struct ") : "";
                t["Name"] = name == null ? $"anon-{kind.ToLowerInvariant()}-{id}" : prefix + Qualified(c, die, name);
                t["Type"] = kind;
                Decl(c, die, t);
                var members = new OrderedDictionary<string, object>();
                var bases = new OrderedDictionary<string, object>();
                foreach (var m in c.Children.GetValueOrDefault(die.Offset) ?? [])
                {
                    if (m.Tag == TagInheritance && m.Get(AtType) is { } bt2 && c.Dies.TryGetValue(bt2.U, out var bd))
                    {
                        bases[TypeId(c, bd)] = new OrderedDictionary<string, object> { ["pos"] = bases.Count.ToString() };
                        continue;
                    }
                    if (m.Tag != TagMember || m.Get(0x34) != null && Str(c, m, AtName)?.StartsWith("_vptr") == true) continue;
                    if (m.Get(AtExternal) != null || m.Get(AtDeclaration) != null) continue;   // static members
                    var mi = new OrderedDictionary<string, object>
                    {
                        ["name"] = Str(c, m, AtName) ?? "",
                        ["type"] = TypeOf(c, m),
                    };
                    if (m.Get(AtDataMemberLocation) is { } loc && loc.Block == null) mi["offset"] = loc.U.ToString();
                    else if (m.Get(0x6b) is { } bitOffset) mi["offset"] = (bitOffset.U / 8).ToString();   // DW_AT_data_bit_offset
                    else if (die.Tag == TagUnion) mi["offset"] = "0";
                    if (m.Get(0x0d) is { } bits) mi["bitfield"] = bits.U.ToString();   // DW_AT_bit_size
                    members[members.Count.ToString()] = mi;
                }
                if (bases.Count > 0) t["Base"] = bases;
                if (members.Count > 0) t["Memb"] = members;
                break;
            }
            case TagEnum:
            {
                t["Name"] = name == null ? $"anon-enum-{id}" : (c.Language == "C" ? "enum " : "") + Qualified(c, die, name);
                t["Type"] = "Enum";
                if (die.Get(AtType) != null) t["BaseType"] = Base();
                Decl(c, die, t);
                var members = new OrderedDictionary<string, object>();
                foreach (var e in (c.Children.GetValueOrDefault(die.Offset) ?? []).Where(e => e.Tag == TagEnumerator))
                {
                    var v = e.Get(AtConstValue);
                    members[members.Count.ToString()] = new OrderedDictionary<string, object>
                    {
                        ["name"] = Str(c, e, AtName) ?? "",
                        ["value"] = v == null ? "0" : v.IsSigned ? v.S.ToString() : v.Form == ElfDwarf.DW_FORM_data1 ? ((sbyte)v.U).ToString() : v.U.ToString(),
                    };
                }
                t["Memb"] = members;
                break;
            }
            case TagArray:
            {
                var b = Base();
                var dims = "";
                ulong total = 1;
                foreach (var sr in (c.Children.GetValueOrDefault(die.Offset) ?? []).Where(x => x.Tag == TagSubrange))
                {
                    ulong? n = sr.Get(AtCount)?.U ?? (sr.Get(AtUpperBound) is { } ub ? ub.U + 1 : null);
                    dims += n is { } k ? $"[{k}]" : "[]";
                    total *= n ?? 0;
                }
                t["BaseType"] = b;
                t["Name"] = Name(c, b) + (dims.Length == 0 ? "[]" : dims);
                t["Type"] = "Array";
                if (size == null && c.Types[b] is OrderedDictionary<string, object> et && et.TryGetValue("Size", out var es) && ulong.TryParse((string)es, out var esz))
                    size = esz * total;
                break;
            }
            case TagSubroutineType:
            {
                t["Return"] = Base();
                var parms = new OrderedDictionary<string, object>();
                foreach (var p in (c.Children.GetValueOrDefault(die.Offset) ?? []).Where(x => x.Tag == TagFormalParameter))
                    parms[parms.Count.ToString()] = new OrderedDictionary<string, object> { ["type"] = TypeOf(c, p) };
                if (parms.Count > 0) t["Param"] = parms;
                t["Name"] = $"{Name(c, (string)t["Return"])}(*)({string.Join(", ", parms.Values.Select(p => Name(c, (string)((OrderedDictionary<string, object>)p)["type"])))})";
                t["Type"] = "Func";
                break;
            }
            default:
                t["Name"] = name ?? $"unknown-{id}";
                t["Type"] = "Intrinsic";
                break;
        }
        if (size is { } sz) t["Size"] = sz.ToString();
        return id;
    }

    static void Decl(Ctx c, ElfDwarf.Die die, OrderedDictionary<string, object> t)
    {
        if (SourceFile(c, die) is { } src)
        {
            t["Header"] = Path.GetFileName(src);
            if (die.Get(AtDeclLine) is { } l) t["Line"] = l.U.ToString();
        }
    }

    // C++ names carry their enclosing namespaces and classes.
    static string Qualified(Ctx c, ElfDwarf.Die die, string name)
    {
        if (name == null || c.Language == "C") return name;
        var parts = new List<string> { name };
        var d = die;
        while (c.Parent.TryGetValue(d.Offset, out var p) && p.Tag is TagNamespace or TagStruct or TagClass or TagUnion)
        {
            parts.Insert(0, Str(c, p, AtName) ?? "(anonymous namespace)");
            d = p;
        }
        return string.Join("::", parts);
    }

    static string Name(Ctx c, string id) => c.Types.TryGetValue(id, out var t) && t is OrderedDictionary<string, object> d && d.TryGetValue("Name", out var n) ? n as string ?? "" : "";

    // ---- Data::Dumper output ----

    static string Perl(object value, int indent)
    {
        var pad = new string(' ', indent + 2);
        return value switch
        {
            null => "undef",
            string s => "'" + s.Replace("\\", "\\\\").Replace("'", "\\'") + "'",
            OrderedDictionary<string, object> d when d.Count == 0 => "{}",
            OrderedDictionary<string, object> d => "{\n" + string.Join(",\n",
                d.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{pad}{Perl(kv.Key, 0)} => {Perl(kv.Value, indent + 2)}")) + "\n" + new string(' ', indent) + "}",
            _ => Perl(value.ToString(), indent),
        };
    }
}
