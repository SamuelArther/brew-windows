using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace WinTools;

// Shared ELF reader/writer for the ELF tools (chrpath, execstack, elfutils' eu-*): headers, sections,
// segments, symbol tables with GNU symbol versions, the dynamic section and notes. 32/64-bit, either
// byte order. Edits happen in place on the loaded bytes and are written back with Save().
static class ElfKit
{
    public const uint SHT_NULL = 0, SHT_PROGBITS = 1, SHT_SYMTAB = 2, SHT_STRTAB = 3, SHT_RELA = 4, SHT_HASH = 5,
        SHT_DYNAMIC = 6, SHT_NOTE = 7, SHT_NOBITS = 8, SHT_REL = 9, SHT_DYNSYM = 11, SHT_GNU_HASH = 0x6ffffff6,
        SHT_GNU_verdef = 0x6ffffffd, SHT_GNU_verneed = 0x6ffffffe, SHT_GNU_versym = 0x6fffffff;
    public const uint PT_NULL = 0, PT_LOAD = 1, PT_DYNAMIC = 2, PT_INTERP = 3, PT_NOTE = 4, PT_PHDR = 6, PT_TLS = 7,
        PT_GNU_EH_FRAME = 0x6474e550, PT_GNU_STACK = 0x6474e551, PT_GNU_RELRO = 0x6474e552, PT_GNU_PROPERTY = 0x6474e553;
    public const uint PF_X = 1, PF_W = 2, PF_R = 4;
    public const long DT_NULL = 0, DT_NEEDED = 1, DT_STRTAB = 5, DT_SONAME = 14, DT_RPATH = 15, DT_RUNPATH = 29;
    public const ulong SHF_WRITE = 1, SHF_ALLOC = 2, SHF_EXECINSTR = 4, SHF_COMPRESSED = 0x800;

    public sealed class Section
    {
        public int Index;
        public string Name = "";
        public uint NameOffset, Type, Link, Info;
        public ulong Flags, Addr, Offset, Size, Align, EntSize;
        public long HeaderOffset;
    }

    public sealed class Segment
    {
        public int Index;
        public uint Type, Flags;
        public ulong Offset, VAddr, PAddr, FileSize, MemSize, Align;
        public long HeaderOffset;
    }

    public sealed class Symbol
    {
        public int Index;
        public string Name = "";
        public string Version;          // "VER" with Hidden=false means name@@VER
        public bool VersionHidden;
        public int VersionIndex = -1;   // index into the verneed/verdef list (for "(n)" in eu-readelf)
        public ulong Value, Size;
        public byte Info, Other;
        public ushort Shndx;
        public int Type => Info & 0xf;
        public int Bind => Info >> 4;
        public int Visibility => Other & 3;
    }

    public sealed class Dyn
    {
        public long Tag;
        public ulong Value;
        public long Offset;   // file offset of the entry
    }

    // Thrown for files that aren't ELF or are truncated: tools print "<file>: <message>".
    public sealed class ElfError(string message) : Exception(message);

    public sealed class Elf
    {
        public readonly string Path;
        public byte[] Data;
        public bool Is64, Little;
        public byte OsAbi, AbiVersion, IdentVersion;
        public ushort Type, Machine, EhSize, PhEntSize, PhNum, ShEntSize, ShNum, ShStrNdx;
        public uint Version, Flags;
        public ulong Entry, PhOff, ShOff;
        public List<Section> Sections = [];
        public List<Segment> Segments = [];

        public Elf(string path, byte[] data)
        {
            Path = path;
            Data = data;
            if (data.Length < 16 || data[0] != 0x7f || data[1] != 'E' || data[2] != 'L' || data[3] != 'F')
                throw new ElfError("not an ELF file");
            if (data[4] is not (1 or 2)) throw new ElfError("invalid ELF class");
            if (data[5] is not (1 or 2)) throw new ElfError("invalid ELF data encoding");
            Is64 = data[4] == 2;
            Little = data[5] == 1;
            IdentVersion = data[6];
            OsAbi = data[7];
            AbiVersion = data[8];
            if (data.Length < (Is64 ? 64 : 52)) throw new ElfError("file too short for an ELF header");
            Type = U16(16);
            Machine = U16(18);
            Version = U32(20);
            if (Is64)
            {
                Entry = U64(24); PhOff = U64(32); ShOff = U64(40); Flags = U32(48);
                EhSize = U16(52); PhEntSize = U16(54); PhNum = U16(56); ShEntSize = U16(58); ShNum = U16(60); ShStrNdx = U16(62);
            }
            else
            {
                Entry = U32(24); PhOff = U32(28); ShOff = U32(32); Flags = U32(36);
                EhSize = U16(40); PhEntSize = U16(42); PhNum = U16(44); ShEntSize = U16(46); ShNum = U16(48); ShStrNdx = U16(50);
            }
            ReadSections();
            ReadSegments();
        }

        public static Elf Load(string path)
        {
            if (Directory.Exists(path)) throw new ElfError("Is a directory");
            if (!File.Exists(path)) throw new ElfError("No such file or directory");
            return new Elf(path, File.ReadAllBytes(path));
        }

        public static bool IsElf(byte[] data) => data.Length >= 4 && data[0] == 0x7f && data[1] == 'E' && data[2] == 'L' && data[3] == 'F';

        public void Save(string path = null) => File.WriteAllBytes(path ?? Path, Data);

        // ---- primitive access ----
        void Check(long offset, int size)
        {
            if (offset < 0 || offset + size > Data.Length) throw new ElfError("file is truncated or corrupt");
        }
        public ushort U16(long o) { Check(o, 2); var s = Data.AsSpan((int)o); return Little ? BinaryPrimitives.ReadUInt16LittleEndian(s) : BinaryPrimitives.ReadUInt16BigEndian(s); }
        public uint U32(long o) { Check(o, 4); var s = Data.AsSpan((int)o); return Little ? BinaryPrimitives.ReadUInt32LittleEndian(s) : BinaryPrimitives.ReadUInt32BigEndian(s); }
        public ulong U64(long o) { Check(o, 8); var s = Data.AsSpan((int)o); return Little ? BinaryPrimitives.ReadUInt64LittleEndian(s) : BinaryPrimitives.ReadUInt64BigEndian(s); }
        public ulong Word(long o) => Is64 ? U64(o) : U32(o);
        public void W16(long o, ushort v) { Check(o, 2); var s = Data.AsSpan((int)o); if (Little) BinaryPrimitives.WriteUInt16LittleEndian(s, v); else BinaryPrimitives.WriteUInt16BigEndian(s, v); }
        public void W32(long o, uint v) { Check(o, 4); var s = Data.AsSpan((int)o); if (Little) BinaryPrimitives.WriteUInt32LittleEndian(s, v); else BinaryPrimitives.WriteUInt32BigEndian(s, v); }
        public void W64(long o, ulong v) { Check(o, 8); var s = Data.AsSpan((int)o); if (Little) BinaryPrimitives.WriteUInt64LittleEndian(s, v); else BinaryPrimitives.WriteUInt64BigEndian(s, v); }
        public void WWord(long o, ulong v) { if (Is64) W64(o, v); else W32(o, (uint)v); }
        public int WordSize => Is64 ? 8 : 4;

        public string CString(long offset)
        {
            if (offset < 0 || offset >= Data.Length) return "";
            var end = Array.IndexOf(Data, (byte)0, (int)offset);
            if (end < 0) end = Data.Length;
            return Encoding.UTF8.GetString(Data, (int)offset, end - (int)offset);
        }

        void ReadSections()
        {
            if (ShOff == 0) return;
            var count = (int)ShNum;
            // Extended numbering: the real count lives in section 0's sh_size.
            if (count == 0 && ShOff + (ulong)ShEntSize <= (ulong)Data.Length) count = (int)(Is64 ? U64((long)ShOff + 32) : U32((long)ShOff + 20));
            for (var i = 0; i < count; i++)
            {
                long h = (long)ShOff + i * (long)ShEntSize;
                if (h + ShEntSize > Data.Length) throw new ElfError("section header table is truncated");
                var s = new Section { Index = i, HeaderOffset = h, NameOffset = U32(h), Type = U32(h + 4) };
                if (Is64)
                {
                    s.Flags = U64(h + 8); s.Addr = U64(h + 16); s.Offset = U64(h + 24); s.Size = U64(h + 32);
                    s.Link = U32(h + 40); s.Info = U32(h + 44); s.Align = U64(h + 48); s.EntSize = U64(h + 56);
                }
                else
                {
                    s.Flags = U32(h + 8); s.Addr = U32(h + 12); s.Offset = U32(h + 16); s.Size = U32(h + 20);
                    s.Link = U32(h + 24); s.Info = U32(h + 28); s.Align = U32(h + 32); s.EntSize = U32(h + 36);
                }
                Sections.Add(s);
            }
            var strndx = ShStrNdx == 0xffff && Sections.Count > 0 ? (int)Sections[0].Link : ShStrNdx;
            if (strndx < Sections.Count)
            {
                var names = Sections[strndx];
                foreach (var s in Sections)
                    if ((ulong)s.NameOffset < names.Size) s.Name = CString((long)names.Offset + s.NameOffset);
            }
        }

        void ReadSegments()
        {
            if (PhOff == 0) return;
            for (var i = 0; i < PhNum; i++)
            {
                long h = (long)PhOff + i * (long)PhEntSize;
                if (h + PhEntSize > Data.Length) throw new ElfError("program header table is truncated");
                var p = new Segment { Index = i, HeaderOffset = h, Type = U32(h) };
                if (Is64)
                {
                    p.Flags = U32(h + 4); p.Offset = U64(h + 8); p.VAddr = U64(h + 16); p.PAddr = U64(h + 24);
                    p.FileSize = U64(h + 32); p.MemSize = U64(h + 40); p.Align = U64(h + 48);
                }
                else
                {
                    p.Offset = U32(h + 4); p.VAddr = U32(h + 8); p.PAddr = U32(h + 12); p.FileSize = U32(h + 16);
                    p.MemSize = U32(h + 20); p.Flags = U32(h + 24); p.Align = U32(h + 28);
                }
                Segments.Add(p);
            }
        }

        public void WriteSegmentFlags(Segment p, uint flags)
        {
            p.Flags = flags;
            W32(p.HeaderOffset + (Is64 ? 4 : 24), flags);
        }

        public Section SectionByName(string name) => Sections.FirstOrDefault(s => s.Name == name);

        // Section contents; SHF_COMPRESSED sections (zlib) are inflated, .zdebug_* (GNU "ZLIB" header) too.
        public byte[] Contents(Section s)
        {
            if (s.Type == SHT_NOBITS || s.Size == 0) return [];
            if (s.Offset + s.Size > (ulong)Data.Length) throw new ElfError($"section [{s.Index}] '{s.Name}' extends past the end of the file");
            var raw = Data.AsSpan((int)s.Offset, (int)s.Size);
            if ((s.Flags & SHF_COMPRESSED) != 0)
            {
                var chdr = Is64 ? 24 : 12;
                var type = U32((long)s.Offset);
                var size = Is64 ? U64((long)s.Offset + 8) : U32((long)s.Offset + 4);
                if (type != 1) throw new ElfError($"section [{s.Index}] '{s.Name}' uses an unsupported compression type {type}");
                return Inflate(raw[chdr..].ToArray(), (long)size);
            }
            if (s.Name.StartsWith(".zdebug") && raw.Length >= 12 && raw[..4].SequenceEqual("ZLIB"u8))
                return Inflate(raw[12..].ToArray(), (long)BinaryPrimitives.ReadUInt64BigEndian(raw[4..]));
            return raw.ToArray();
        }

        static byte[] Inflate(byte[] input, long size)
        {
            using var z = new ZLibStream(new MemoryStream(input), CompressionMode.Decompress);
            var output = new byte[size];
            z.ReadExactly(output);
            return output;
        }

        // Debug section by DWARF name (".debug_info"), also found as .zdebug_info.
        public byte[] DebugSection(string name)
        {
            var s = SectionByName(name) ?? SectionByName(".z" + name[1..]);
            return s == null ? null : Contents(s);
        }

        public string StringAt(int sectionIndex, ulong offset)
        {
            if (sectionIndex <= 0 || sectionIndex >= Sections.Count) return "";
            var s = Sections[sectionIndex];
            return offset < s.Size ? CString((long)(s.Offset + offset)) : "";
        }

        // ---- symbols ----
        public List<Symbol> Symbols(Section table)
        {
            var list = new List<Symbol>();
            if (table.Type == SHT_NOBITS) return list;
            var entsize = table.EntSize != 0 ? (long)table.EntSize : Is64 ? 24 : 16;
            var count = (long)table.Size / entsize;
            var versions = table.Type == SHT_DYNSYM ? VersionNames() : null;
            var versym = table.Type == SHT_DYNSYM ? Sections.FirstOrDefault(s => s.Type == SHT_GNU_versym) : null;
            for (var i = 0; i < count; i++)
            {
                long o = (long)table.Offset + i * entsize;
                var sym = new Symbol { Index = i };
                uint name;
                if (Is64)
                {
                    name = U32(o); sym.Info = Data[o + 4]; sym.Other = Data[o + 5]; sym.Shndx = U16(o + 6);
                    sym.Value = U64(o + 8); sym.Size = U64(o + 16);
                }
                else
                {
                    name = U32(o); sym.Value = U32(o + 4); sym.Size = U32(o + 8);
                    sym.Info = Data[o + 12]; sym.Other = Data[o + 13]; sym.Shndx = U16(o + 14);
                }
                sym.Name = StringAt((int)table.Link, name);
                if (versym != null && versions != null && i > 0 && (ulong)(i * 2) < versym.Size)
                {
                    var v = U16((long)versym.Offset + i * 2);
                    var index = v & 0x7fff;
                    if (index > 1 && versions.TryGetValue(index, out var ver))
                    {
                        sym.Version = ver.Name;
                        sym.VersionIndex = index;
                        // Undefined symbols reference a needed version: printed name@VER (n).
                        sym.VersionHidden = (v & 0x8000) != 0 || sym.Shndx == 0 || !ver.Defined;
                    }
                }
                list.Add(sym);
            }
            return list;
        }

        // Version index -> (name, defined here?) from .gnu.version_d / .gnu.version_r.
        public Dictionary<int, (string Name, bool Defined)> VersionNames()
        {
            var map = new Dictionary<int, (string, bool)>();
            foreach (var s in Sections)
            {
                if (s.Type == SHT_GNU_verdef)
                {
                    long o = (long)s.Offset;
                    for (var n = 0; n < (int)s.Info && o < (long)(s.Offset + s.Size); n++)
                    {
                        var ndx = U16(o + 4);
                        var aux = U32(o + 12);
                        var next = U32(o + 16);
                        var name = StringAt((int)s.Link, U32(o + aux));
                        map[ndx & 0x7fff] = (name, true);
                        if (next == 0) break;
                        o += next;
                    }
                }
                else if (s.Type == SHT_GNU_verneed)
                {
                    long o = (long)s.Offset;
                    for (var n = 0; n < (int)s.Info && o < (long)(s.Offset + s.Size); n++)
                    {
                        var cnt = U16(o + 2);
                        var aux = U32(o + 8);
                        var next = U32(o + 12);
                        long a = o + aux;
                        for (var k = 0; k < cnt; k++)
                        {
                            var other = U16(a + 6);
                            map[other & 0x7fff] = (StringAt((int)s.Link, U32(a + 8)), false);
                            var anext = U32(a + 12);
                            if (anext == 0) break;
                            a += anext;
                        }
                        if (next == 0) break;
                        o += next;
                    }
                }
            }
            return map;
        }

        // ---- dynamic section ----
        public Section DynamicSection => Sections.FirstOrDefault(s => s.Type == SHT_DYNAMIC);

        public List<Dyn> Dynamic()
        {
            var list = new List<Dyn>();
            ulong offset, size;
            if (DynamicSection is { } d) { offset = d.Offset; size = d.Size; }
            else if (Segments.FirstOrDefault(p => p.Type == PT_DYNAMIC) is { } p) { offset = p.Offset; size = p.FileSize; }
            else return list;
            var entsize = Is64 ? 16 : 8;
            for (long o = (long)offset; o + entsize <= (long)(offset + size); o += entsize)
            {
                var tag = Is64 ? (long)U64(o) : (int)U32(o);
                list.Add(new Dyn { Tag = tag, Value = Word(o + WordSize), Offset = o });
                if (tag == DT_NULL) break;
            }
            return list;
        }

        public void WriteDyn(Dyn d)
        {
            if (Is64) { W64(d.Offset, (ulong)d.Tag); W64(d.Offset + 8, d.Value); }
            else { W32(d.Offset, (uint)d.Tag); W32(d.Offset + 4, (uint)d.Value); }
        }

        // File offset of the dynamic string table (DT_STRTAB is a virtual address).
        public long DynStrOffset()
        {
            if (DynamicSection is { } d && d.Link < Sections.Count && d.Link > 0) return (long)Sections[(int)d.Link].Offset;
            var strtab = Dynamic().FirstOrDefault(x => x.Tag == DT_STRTAB);
            return strtab == null ? -1 : VirtualToFile(strtab.Value);
        }

        public long VirtualToFile(ulong address)
        {
            foreach (var p in Segments)
                if (p.Type == PT_LOAD && address >= p.VAddr && address < p.VAddr + p.FileSize)
                    return (long)(address - p.VAddr + p.Offset);
            return -1;
        }

        // ---- notes ----
        public IEnumerable<(string Owner, uint Type, byte[] Desc)> Notes(ulong offset, ulong size, ulong align)
        {
            var a = align == 8 ? 8 : 4;
            long o = (long)offset, end = (long)(offset + size);
            while (o + 12 <= end)
            {
                var namesz = U32(o); var descsz = U32(o + 4); var type = U32(o + 8);
                o += 12;
                var name = namesz > 0 ? Encoding.ASCII.GetString(Data, (int)o, (int)Math.Max(0, namesz - 1)) : "";
                o += (namesz + a - 1) / a * a;
                var desc = Data.AsSpan((int)o, (int)Math.Min(descsz, (uint)(end - o))).ToArray();
                o += (descsz + a - 1) / a * a;
                yield return (name, type, desc);
            }
        }
    }

    // ---- names, shared by eu-readelf / eu-nm ----
    public static string MachineName(ushort m) => m switch
    {
        0 => "None",
        2 => "SUN SPARC",
        3 => "Intel 80386",
        8 => "MIPS R3000 big-endian",
        20 => "PowerPC",
        21 => "PowerPC 64-bit",
        22 => "IBM S/390",
        40 => "ARM",
        42 => "Hitachi SH",
        43 => "SPARC v9 64-bit",
        50 => "Intel Merced",
        62 => "AMD x86-64",
        183 => "AARCH64",
        243 => "RISC-V",
        247 => "Linux BPF",
        258 => "LoongArch",
        _ => $"<unknown>: 0x{m:x}",
    };

    public static string TypeName(ushort t) => t switch
    {
        0 => "NONE (None)",
        1 => "REL (Relocatable file)",
        2 => "EXEC (Executable file)",
        3 => "DYN (Shared object file)",
        4 => "CORE (Core file)",
        _ => $"OS Specific: ({t:x})",
    };

    public static string OsAbiName(byte o) => o switch
    {
        0 => "UNIX - System V",
        1 => "HP-UX",
        2 => "NetBSD",
        3 => "Linux",
        6 => "Solaris",
        9 => "FreeBSD",
        12 => "OpenBSD",
        97 => "ARM",
        255 => "Stand alone (embedded) application",
        _ => $"<unknown>: {o}",
    };

    public static string SectionTypeName(uint t) => t switch
    {
        0 => "NULL", 1 => "PROGBITS", 2 => "SYMTAB", 3 => "STRTAB", 4 => "RELA", 5 => "HASH", 6 => "DYNAMIC",
        7 => "NOTE", 8 => "NOBITS", 9 => "REL", 10 => "SHLIB", 11 => "DYNSYM", 14 => "INIT_ARRAY", 15 => "FINI_ARRAY",
        16 => "PREINIT_ARRAY", 17 => "GROUP", 18 => "SYMTAB_SHNDX", 19 => "RELR",
        0x6ffffff5 => "GNU_ATTRIBUTES", 0x6ffffff6 => "GNU_HASH", 0x6ffffff7 => "GNU_LIBLIST", 0x6ffffff8 => "CHECKSUM",
        0x6ffffffd => "GNU_verdef", 0x6ffffffe => "GNU_verneed", 0x6fffffff => "GNU_versym",
        0x6fff4c00 => "LLVM_ODRTAB", 0x6fff4c01 => "LLVM_LINKER_OPTIONS", 0x6fff4c03 => "LLVM_ADDRSIG",
        0x6fff4c04 => "LLVM_DEPENDENT_LIBRARIES", 0x6fff4c09 => "LLVM_BB_ADDR_MAP",
        0x70000001 => "X86_64_UNWIND",
        _ => t >= 0x60000000 && t < 0x70000000 ? $"LOOS+{t - 0x60000000:x}" : t >= 0x70000000 && t < 0x80000000 ? $"LOPROC+{t - 0x70000000:x}" : $"<unknown>: {t}",
    };

    public static string SegmentTypeName(uint t) => t switch
    {
        0 => "NULL", 1 => "LOAD", 2 => "DYNAMIC", 3 => "INTERP", 4 => "NOTE", 5 => "SHLIB", 6 => "PHDR", 7 => "TLS",
        0x6474e550 => "GNU_EH_FRAME", 0x6474e551 => "GNU_STACK", 0x6474e552 => "GNU_RELRO", 0x6474e553 => "GNU_PROPERTY",
        0x6474e554 => "GNU_SFRAME", 0x65a3dbe6 => "OPENBSD_RANDOMIZE", 0x65a3dbe7 => "OPENBSD_WXNEEDED",
        _ => t >= 0x60000000 && t < 0x70000000 ? $"LOOS+{t - 0x60000000:x}" : $"<unknown>: 0x{t:x}",
    };

    public static string SymbolTypeName(int t) => t switch
    {
        0 => "NOTYPE", 1 => "OBJECT", 2 => "FUNC", 3 => "SECTION", 4 => "FILE", 5 => "COMMON", 6 => "TLS",
        10 => "GNU_IFUNC", _ => $"<unknown>: {t}",
    };

    public static string SymbolBindName(int b) => b switch
    {
        0 => "LOCAL", 1 => "GLOBAL", 2 => "WEAK", 10 => "GNU_UNIQUE", _ => $"<unknown>: {b}",
    };

    public static string VisibilityName(int v) => v switch
    {
        0 => "DEFAULT", 1 => "INTERNAL", 2 => "HIDDEN", _ => "PROTECTED",
    };

    public static string DynTagName(long tag) => tag switch
    {
        0 => "NULL", 1 => "NEEDED", 2 => "PLTRELSZ", 3 => "PLTGOT", 4 => "HASH", 5 => "STRTAB", 6 => "SYMTAB",
        7 => "RELA", 8 => "RELASZ", 9 => "RELAENT", 10 => "STRSZ", 11 => "SYMENT", 12 => "INIT", 13 => "FINI",
        14 => "SONAME", 15 => "RPATH", 16 => "SYMBOLIC", 17 => "REL", 18 => "RELSZ", 19 => "RELENT", 20 => "PLTREL",
        21 => "DEBUG", 22 => "TEXTREL", 23 => "JMPREL", 24 => "BIND_NOW", 25 => "INIT_ARRAY", 26 => "FINI_ARRAY",
        27 => "INIT_ARRAYSZ", 28 => "FINI_ARRAYSZ", 29 => "RUNPATH", 30 => "FLAGS", 32 => "PREINIT_ARRAY",
        33 => "PREINIT_ARRAYSZ", 34 => "SYMTAB_SHNDX", 35 => "RELRSZ", 36 => "RELR", 37 => "RELRENT",
        0x6ffffef5 => "GNU_HASH", 0x6ffffffb => "FLAGS_1", 0x6ffffff0 => "VERSYM", 0x6ffffff9 => "RELACOUNT",
        0x6ffffffa => "RELCOUNT", 0x6ffffffc => "VERDEF", 0x6ffffffd => "VERDEFNUM", 0x6ffffffe => "VERNEED",
        0x6fffffff => "VERNEEDNUM", 0x6ffffdf5 => "GNU_PRELINKED", 0x6ffffdf6 => "GNU_CONFLICTSZ",
        0x6ffffdf7 => "GNU_LIBLISTSZ", 0x6ffffdf8 => "CHECKSUM", 0x6ffffefe => "GNU_LIBLIST", 0x7ffffffd => "AUXILIARY",
        0x7fffffff => "FILTER",
        _ => $"<unknown>: 0x{tag:x}",
    };
}
