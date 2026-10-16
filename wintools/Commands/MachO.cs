using System.Buffers.Binary;
using System.Text;

namespace WinTools;

// Mach-O reader shared by mtoc/ocmtoc and swift-section: thin 32/64-bit little-endian images and fat
// (universal) files, with segments, sections, symbols, the entry point (LC_MAIN or LC_UNIXTHREAD) and
// the rebase locations an image needs when it is moved (classic local relocations, LC_DYLD_INFO rebase
// opcodes or LC_DYLD_CHAINED_FIXUPS chains).
sealed class MachO
{
    public sealed class Segment
    {
        public string Name;
        public ulong VmAddr, VmSize, FileOff, FileSize;
        public uint MaxProt, InitProt, Flags;
        public List<Section> Sections = [];
        public bool Writable => (InitProt & 2) != 0;
        public bool Executable => (InitProt & 4) != 0;
    }

    public sealed class Section
    {
        public string Name, SegName;
        public ulong Addr, Size;
        public uint Offset, Align, RelOff, NReloc, Flags;
        public bool ZeroFill => (Flags & 0xff) is 0x1 or 0xc or 0x12;   // S_ZEROFILL, S_GB_ZEROFILL, S_THREAD_LOCAL_ZEROFILL
    }

    public sealed class Symbol
    {
        public string Name;
        public byte Type, Sect;
        public ulong Value;
        public bool Defined => (Type & 0x0e) == 0x0e;   // N_SECT
        public bool External => (Type & 0x01) != 0;
    }

    // A pointer the image's loader must slide: its address and width (4 or 8 bytes).
    public readonly record struct Rebase(ulong Address, int Width);

    public const uint CpuX86 = 7, CpuX86_64 = 0x01000007, CpuArm = 12, CpuArm64 = 0x0100000c, CpuArm64_32 = 0x0200000c;

    public byte[] Data;          // the thin image (a slice of a fat file)
    public bool Is64;
    public uint CpuType, CpuSubtype, FileType, Flags;
    public List<Segment> Segments = [];
    public List<Section> Sections = [];
    public ulong? EntryAddress;
    public byte[] Uuid;
    public List<string> Dylibs = [];
    public string InstallName;
    uint symoff, nsyms, stroff, strsize, locreloff, nlocrel, extreloff, nextrel;
    uint rebaseOff, rebaseSize, fixupsOff, fixupsSize;
    List<Symbol> symbols;

    public sealed class FormatException(string message) : Exception(message);

    public static bool IsMachO(byte[] data) => data.Length >= 4 && BinaryPrimitives.ReadUInt32BigEndian(data) switch
    {
        0xcafebabe or 0xcffaedfe or 0xcefaedfe => true,
        _ => false,
    };

    // Loads a Mach-O file; for a fat file, the slice for `arch` (or the first slice).
    public static MachO Load(string path, string arch = null) => Parse(File.ReadAllBytes(path), arch);

    public static MachO Parse(byte[] data, string arch = null)
    {
        if (data.Length < 28) throw new FormatException("file too small to be a Mach-O file");
        var magic = BinaryPrimitives.ReadUInt32BigEndian(data);
        if (magic == 0xcafebabe)
        {
            var n = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4));
            if (n == 0 || n > 64) throw new FormatException("bad fat header");
            (int Off, int Size)? chosen = null;
            for (var i = 0; i < n; i++)
            {
                var e = data.AsSpan(8 + i * 20);
                var cpu = BinaryPrimitives.ReadUInt32BigEndian(e);
                var off = (int)BinaryPrimitives.ReadUInt32BigEndian(e[8..]);
                var size = (int)BinaryPrimitives.ReadUInt32BigEndian(e[12..]);
                if (arch == null || ArchName(cpu) == arch) { chosen = (off, size); break; }
            }
            if (chosen is not { } c) throw new FormatException($"fat file does not contain architecture {arch}");
            if (c.Off + c.Size > data.Length) throw new FormatException("truncated fat file");
            return Parse(data.AsSpan(c.Off, c.Size).ToArray());
        }
        var m = new MachO { Data = data };
        var le = BinaryPrimitives.ReadUInt32LittleEndian(data);
        if (le == 0xfeedfacf) m.Is64 = true;
        else if (le != 0xfeedface) throw new FormatException("not a Mach-O file");
        m.CpuType = U32(data, 4);
        m.CpuSubtype = U32(data, 8);
        m.FileType = U32(data, 12);
        var ncmds = U32(data, 16);
        m.Flags = U32(data, 24);
        var p = m.Is64 ? 32 : 28;
        for (var i = 0; i < ncmds; i++)
        {
            if (p + 8 > data.Length) throw new FormatException("truncated load commands");
            var cmd = U32(data, p);
            var size = (int)U32(data, p + 4);
            if (size < 8 || p + size > data.Length) throw new FormatException("bad load command size");
            m.LoadCommand(cmd, p, size);
            p += size;
        }
        return m;
    }

    void LoadCommand(uint cmd, int p, int size)
    {
        var d = Data;
        switch (cmd)
        {
            case 0x1 or 0x19:   // LC_SEGMENT, LC_SEGMENT_64
            {
                var w = cmd == 0x19;
                var seg = new Segment { Name = Name16(d, p + 8) };
                var q = p + 24;
                if (w)
                {
                    seg.VmAddr = U64(d, q); seg.VmSize = U64(d, q + 8); seg.FileOff = U64(d, q + 16); seg.FileSize = U64(d, q + 24);
                    q += 32;
                }
                else
                {
                    seg.VmAddr = U32(d, q); seg.VmSize = U32(d, q + 4); seg.FileOff = U32(d, q + 8); seg.FileSize = U32(d, q + 12);
                    q += 16;
                }
                seg.MaxProt = U32(d, q); seg.InitProt = U32(d, q + 4);
                var nsects = U32(d, q + 8);
                seg.Flags = U32(d, q + 12);
                q += 16;
                for (var s = 0; s < nsects; s++)
                {
                    var sec = new Section { Name = Name16(d, q), SegName = Name16(d, q + 16) };
                    var r = q + 32;
                    if (w) { sec.Addr = U64(d, r); sec.Size = U64(d, r + 8); r += 16; }
                    else { sec.Addr = U32(d, r); sec.Size = U32(d, r + 4); r += 8; }
                    sec.Offset = U32(d, r); sec.Align = U32(d, r + 4); sec.RelOff = U32(d, r + 8); sec.NReloc = U32(d, r + 12); sec.Flags = U32(d, r + 16);
                    seg.Sections.Add(sec);
                    Sections.Add(sec);
                    q += w ? 80 : 68;
                }
                Segments.Add(seg);
                break;
            }
            case 0x2:   // LC_SYMTAB
                symoff = U32(d, p + 8); nsyms = U32(d, p + 12); stroff = U32(d, p + 16); strsize = U32(d, p + 20);
                break;
            case 0xb:   // LC_DYSYMTAB
                extreloff = U32(d, p + 64); nextrel = U32(d, p + 68); locreloff = U32(d, p + 72); nlocrel = U32(d, p + 76);
                break;
            case 0x22 or 0x80000022:   // LC_DYLD_INFO(_ONLY)
                rebaseOff = U32(d, p + 8); rebaseSize = U32(d, p + 12);
                break;
            case 0x80000034:   // LC_DYLD_CHAINED_FIXUPS
                fixupsOff = U32(d, p + 8); fixupsSize = U32(d, p + 12);
                break;
            case 0x1b:   // LC_UUID
                Uuid = d.AsSpan(p + 8, 16).ToArray();
                break;
            case 0x80000028:   // LC_MAIN: entryoff is a file offset into __TEXT
            {
                var off = U64(d, p + 8);
                var text = Segments.FirstOrDefault(s => s.FileOff == 0 && s.FileSize > 0) ?? Segments.FirstOrDefault(s => s.Name == "__TEXT");
                if (text != null) EntryAddress = text.VmAddr + off;
                break;
            }
            case 0x4 or 0x5:   // LC_THREAD, LC_UNIXTHREAD: the program counter of the first thread state
                EntryAddress = ThreadPc(p + 8, p + size);
                break;
            case 0xc or 0x18 or 0x1f or 0x80000018 or 0x80000023:   // LC_LOAD_DYLIB, WEAK, REEXPORT, LAZY, UPWARD
                Dylibs.Add(CString(d, p + (int)U32(d, p + 8)));
                break;
            case 0xd:   // LC_ID_DYLIB
                InstallName = CString(d, p + (int)U32(d, p + 8));
                break;
        }
    }

    ulong? ThreadPc(int p, int end)
    {
        if (p + 8 > end) return null;
        var flavor = U32(Data, p);
        var s = p + 8;
        // Register index of the pc in each architecture's thread state.
        return (CpuType, flavor) switch
        {
            (CpuX86_64, 4) => U64(Data, s + 16 * 8),   // x86_THREAD_STATE64: rax..r15, rip
            (CpuX86, 1) => U32(Data, s + 10 * 4),      // i386_THREAD_STATE: eax..ss, eip
            (CpuArm64, 6) => U64(Data, s + 32 * 8),    // ARM_THREAD_STATE64: x0-x28, fp, lr, sp, pc
            (CpuArm, 1) => U32(Data, s + 15 * 4),      // ARM_THREAD_STATE: r0-r12, sp, lr, pc
            _ => null,
        };
    }

    public static string ArchName(uint cpu) => cpu switch
    {
        CpuX86 => "i386",
        CpuX86_64 => "x86_64",
        CpuArm => "arm",
        CpuArm64 => "arm64",
        CpuArm64_32 => "arm64_32",
        _ => $"cpu{cpu:x}",
    };

    public string Arch => ArchName(CpuType);

    public string FileTypeName => FileType switch
    {
        1 => "MH_OBJECT", 2 => "MH_EXECUTE", 3 => "MH_FVMLIB", 4 => "MH_CORE", 5 => "MH_PRELOAD", 6 => "MH_DYLIB",
        7 => "MH_DYLINKER", 8 => "MH_BUNDLE", 9 => "MH_DYLIB_STUB", 10 => "MH_DSYM", 11 => "MH_KEXT_BUNDLE",
        _ => $"filetype {FileType}",
    };

    public Section FindSection(string seg, string sect) =>
        Sections.FirstOrDefault(s => s.SegName == seg && s.Name == sect);

    public Segment SegmentAt(ulong addr) =>
        Segments.FirstOrDefault(s => addr >= s.VmAddr && addr < s.VmAddr + s.VmSize);

    // File offset of a virtual address, or -1 when it isn't backed by file data (zero fill, unmapped).
    public long FileOffset(ulong addr)
    {
        var seg = SegmentAt(addr);
        if (seg == null || addr - seg.VmAddr >= seg.FileSize) return -1;
        return (long)(seg.FileOff + (addr - seg.VmAddr));
    }

    public byte[] SectionData(Section s)
    {
        if (s.ZeroFill || s.Offset == 0 && FileOffset(s.Addr) < 0) return new byte[s.Size];
        var off = s.Offset != 0 ? s.Offset : FileOffset(s.Addr);
        var len = (long)Math.Min(s.Size, (ulong)Math.Max(0, Data.Length - off));
        var bytes = new byte[s.Size];
        Array.Copy(Data, off, bytes, 0, len);
        return bytes;
    }

    public List<Symbol> Symbols
    {
        get
        {
            if (symbols != null) return symbols;
            symbols = [];
            var entry = Is64 ? 16 : 12;
            for (var i = 0; i < nsyms; i++)
            {
                var p = (int)symoff + i * entry;
                if (p + entry > Data.Length) break;
                var strx = U32(Data, p);
                var sym = new Symbol
                {
                    Type = Data[p + 4],
                    Sect = Data[p + 5],
                    Value = Is64 ? U64(Data, p + 8) : U32(Data, p + 8),
                    Name = strx < strsize ? CString(Data, (int)(stroff + strx)) : "",
                };
                if ((sym.Type & 0xe0) == 0) symbols.Add(sym);   // skip stabs
            }
            return symbols;
        }
    }

    // ---- rebases ----

    public int PointerSize => Is64 ? 8 : 4;

    // Every location the loader must slide, from whichever mechanism the image uses. Chained fixups
    // are resolved in place (each chain entry is rewritten to the plain pointer it encodes), so the
    // image bytes in Data are what an unslid load would see. Binds to other images are errors when
    // strict (a preloaded or firmware image has no dynamic loader to resolve them), else left as 0.
    public List<Rebase> Rebases(bool strict = true)
    {
        var list = new List<Rebase>();
        if (fixupsSize != 0) ChainedFixups(list, strict);
        if (rebaseSize != 0) RebaseOpcodes(list);
        if (nlocrel != 0 || nextrel != 0) ClassicRelocations(list, strict);
        foreach (var sec in Sections.Where(s => s.NReloc != 0 && FileType == 1))
            throw new FormatException($"relocatable object files are not supported (section {sec.SegName},{sec.Name}); link it first");
        return list.Distinct().OrderBy(r => r.Address).ToList();
    }

    void ClassicRelocations(List<Rebase> list, bool strict)
    {
        // r_address is relative to the first segment, or to the first writable one for x86_64 and
        // split-segment images (as dyld reads it).
        var firstWritable = Segments.FirstOrDefault(s => s.Writable)?.VmAddr ?? 0;
        var first = Segments.FirstOrDefault(s => s.Name != "__PAGEZERO")?.VmAddr ?? 0;
        var baseAddr = CpuType is CpuX86_64 or CpuArm64 || (Flags & 0x20) != 0 ? firstWritable : first;
        if (nextrel > 0 && strict)
        {
            var sym = U32(Data, (int)extreloff + 4) & 0xffffff;
            var name = sym < Symbols.Count ? Symbols[(int)sym].Name : $"#{sym}";
            throw new FormatException($"external relocation to undefined symbol {name}");
        }
        for (var i = 0; i < nlocrel; i++)
        {
            var p = (int)locreloff + i * 8;
            var word0 = U32(Data, p);
            if ((word0 & 0x80000000) != 0)
            {
                // Scattered relocation (32-bit): r_address:24, r_type:4, r_length:2, r_pcrel:1.
                var type = (word0 >> 24) & 0xf;
                var length = (word0 >> 28) & 3;
                var pcrel = (word0 >> 30) & 1;
                if (pcrel == 0 && length == 2 && type == 0) list.Add(new Rebase(first + (word0 & 0xffffff), 4));
                continue;
            }
            var info = U32(Data, p + 4);
            var rpcrel = (info >> 24) & 1;
            var rlength = (info >> 25) & 3;
            var rtype = info >> 28;
            if (rpcrel != 0 || rtype != 0) continue;   // only absolute pointers need sliding
            if (rlength is not (2 or 3)) throw new FormatException($"unsupported relocation length {1 << (int)rlength}");
            list.Add(new Rebase(baseAddr + word0, rlength == 3 ? 8 : 4));
        }
    }

    void RebaseOpcodes(List<Rebase> list)
    {
        var p = (int)rebaseOff;
        var end = p + (int)rebaseSize;
        int type = 1, seg = 0;
        ulong addr = 0;
        var ptr = (ulong)PointerSize;
        ulong SegAddr(int index) => index < Segments.Count ? Segments[index].VmAddr : throw new FormatException("rebase segment index out of range");
        void Add(ulong a) => list.Add(new Rebase(a, type == 2 ? 4 : PointerSize));   // REBASE_TYPE_TEXT_ABSOLUTE32
        while (p < end)
        {
            var b = Data[p++];
            var op = b & 0xf0;
            var imm = b & 0x0f;
            switch (op)
            {
                case 0x00: return;                                         // DONE
                case 0x10: type = imm; break;                              // SET_TYPE_IMM
                case 0x20: seg = imm; addr = SegAddr(seg) + Uleb(ref p); break;   // SET_SEGMENT_AND_OFFSET_ULEB
                case 0x30: addr += Uleb(ref p); break;                     // ADD_ADDR_ULEB
                case 0x40: addr += (ulong)imm * ptr; break;                // ADD_ADDR_IMM_SCALED
                case 0x50: for (var i = 0; i < imm; i++) { Add(addr); addr += ptr; } break;   // DO_REBASE_IMM_TIMES
                case 0x60: { var n = Uleb(ref p); for (ulong i = 0; i < n; i++) { Add(addr); addr += ptr; } break; }
                case 0x70: Add(addr); addr += Uleb(ref p) + ptr; break;    // DO_REBASE_ADD_ADDR_ULEB
                case 0x80:                                                 // DO_REBASE_ULEB_TIMES_SKIPPING_ULEB
                {
                    var n = Uleb(ref p);
                    var skip = Uleb(ref p);
                    for (ulong i = 0; i < n; i++) { Add(addr); addr += skip + ptr; }
                    break;
                }
                default: throw new FormatException($"bad rebase opcode 0x{b:x2}");
            }
        }
    }

    void ChainedFixups(List<Rebase> list, bool strict)
    {
        var h = (int)fixupsOff;
        var startsOffset = (int)U32(Data, h + 4);
        var importsCount = U32(Data, h + 16);
        var segs = h + startsOffset;
        var segCount = U32(Data, segs);
        for (var i = 0; i < segCount; i++)
        {
            var info = U32(Data, segs + 4 + i * 4);
            if (info == 0) continue;
            var si = segs + (int)info;
            var pageSize = U16(Data, si + 4);
            var format = U16(Data, si + 6);
            var segOffset = U64(Data, si + 8);
            var pageCount = U16(Data, si + 20);
            // DYLD_CHAINED_PTR_64 and _64_OFFSET (x86_64, arm64); arm64e's authenticated pointers have no
            // meaning outside Apple's loaders.
            if (format is not (2 or 6))
                throw new FormatException($"unsupported chained fixup pointer format {format}");
            var seg = Segments.FirstOrDefault(s => s.FileOff == segOffset && s.FileSize > 0)
                      ?? Segments.First(s => s.FileOff <= segOffset && segOffset < s.FileOff + s.FileSize);
            for (var page = 0; page < pageCount; page++)
            {
                var start = U16(Data, si + 22 + page * 2);
                if (start == 0xffff) continue;   // DYLD_CHAINED_PTR_START_NONE
                if ((start & 0x8000) != 0) throw new FormatException("multi-start chained fixup pages are not supported");
                var off = (long)segOffset + (long)page * pageSize + start;
                while (true)
                {
                    var raw = U64(Data, (int)off);
                    var bind = (raw >> 63) != 0;
                    if (bind && !strict)
                    {
                        BinaryPrimitives.WriteUInt64LittleEndian(Data.AsSpan((int)off), 0);
                        var skip = (raw >> 51) & 0xfff;
                        if (skip == 0) break;
                        off += (long)(skip * 4);
                        continue;
                    }
                    if (bind)
                        throw new FormatException($"chained fixup binds to another image ({importsCount} imports); firmware images must be self-contained");
                    // target:36, high8:8, reserved:7, next:12 (4-byte strides), bind:1. The target is a vmaddr
                    // for format 2 and an offset from the image start for format 6.
                    var target = (raw & 0xfffffffffUL) | ((raw >> 36) & 0xff) << 56;
                    if (format == 6) target += Segments.First(s => s.Name != "__PAGEZERO").VmAddr;
                    var next = (raw >> 51) & 0xfff;
                    BinaryPrimitives.WriteUInt64LittleEndian(Data.AsSpan((int)off), target);
                    list.Add(new Rebase(seg.VmAddr + (ulong)off - seg.FileOff, 8));
                    if (next == 0) break;
                    off += (long)(next * 4);
                }
            }
        }
    }

    ulong Uleb(ref int p)
    {
        ulong v = 0;
        var shift = 0;
        byte b;
        do { b = Data[p++]; v |= (ulong)(b & 0x7f) << shift; shift += 7; } while ((b & 0x80) != 0);
        return v;
    }

    // ---- primitives ----

    public static uint U32(byte[] d, int p) => BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p));
    public static ulong U64(byte[] d, int p) => BinaryPrimitives.ReadUInt64LittleEndian(d.AsSpan(p));
    public static ushort U16(byte[] d, int p) => BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(p));

    static string Name16(byte[] d, int p)
    {
        var len = Array.IndexOf(d, (byte)0, p, 16) is var z and >= 0 ? z - p : 16;
        return Encoding.ASCII.GetString(d, p, len);
    }

    public static string CString(byte[] d, int p)
    {
        if (p < 0 || p >= d.Length) return "";
        var z = Array.IndexOf(d, (byte)0, p);
        return Encoding.UTF8.GetString(d, p, (z < 0 ? d.Length : z) - p);
    }
}
