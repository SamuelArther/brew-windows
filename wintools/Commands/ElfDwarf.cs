using System.Buffers.Binary;
using System.Text;

namespace WinTools;

// DWARF 2-5 reader for the elfutils tools: .debug_abbrev/.debug_info units and DIEs (all forms,
// str_offsets/addr bases), .debug_line programs, location expressions and the name tables
// eu-readelf prints. Relocatable objects (.o) get their debug-section relocations applied first.
static class ElfDwarf
{
    // ---- byte reader ----
    public sealed class Reader(byte[] data, bool little, int position = 0)
    {
        public readonly byte[] Data = data;
        public readonly bool Little = little;
        public int Pos = position;
        public bool AtEnd => Pos >= Data.Length;

        void Need(int n) { if (Pos < 0 || Pos + n > Data.Length) throw new ElfKit.ElfError("invalid DWARF (read past the end of a section)"); }
        public byte U8() { Need(1); return Data[Pos++]; }
        public ushort U16() { Need(2); var v = Little ? BinaryPrimitives.ReadUInt16LittleEndian(Data.AsSpan(Pos)) : BinaryPrimitives.ReadUInt16BigEndian(Data.AsSpan(Pos)); Pos += 2; return v; }
        public uint U24() { Need(3); uint v = Little ? (uint)(Data[Pos] | Data[Pos + 1] << 8 | Data[Pos + 2] << 16) : (uint)(Data[Pos] << 16 | Data[Pos + 1] << 8 | Data[Pos + 2]); Pos += 3; return v; }
        public uint U32() { Need(4); var v = Little ? BinaryPrimitives.ReadUInt32LittleEndian(Data.AsSpan(Pos)) : BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(Pos)); Pos += 4; return v; }
        public ulong U64() { Need(8); var v = Little ? BinaryPrimitives.ReadUInt64LittleEndian(Data.AsSpan(Pos)) : BinaryPrimitives.ReadUInt64BigEndian(Data.AsSpan(Pos)); Pos += 8; return v; }
        public ulong Sized(int size) => size switch { 1 => U8(), 2 => U16(), 3 => U24(), 4 => U32(), 8 => U64(), _ => throw new ElfKit.ElfError($"invalid DWARF (unsupported size {size})") };
        public ulong Uleb()
        {
            ulong result = 0; var shift = 0;
            while (true)
            {
                var b = U8();
                if (shift < 64) result |= (ulong)(b & 0x7f) << shift;
                shift += 7;
                if ((b & 0x80) == 0) return result;
            }
        }
        public long Sleb()
        {
            long result = 0; var shift = 0; byte b;
            do
            {
                b = U8();
                if (shift < 64) result |= (long)(b & 0x7f) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            if (shift < 64 && (b & 0x40) != 0) result |= -1L << shift;
            return result;
        }
        public string CStr()
        {
            var end = Array.IndexOf(Data, (byte)0, Pos);
            if (end < 0) throw new ElfKit.ElfError("invalid DWARF (unterminated string)");
            var s = Encoding.UTF8.GetString(Data, Pos, end - Pos);
            Pos = end + 1;
            return s;
        }
        public byte[] Bytes(int n) { Need(n); var b = Data.AsSpan(Pos, n).ToArray(); Pos += n; return b; }
        // Initial length: returns the length and sets the offset size (4 or 8).
        public ulong InitialLength(out int offsetSize)
        {
            var len = U32();
            if (len == 0xffffffff) { offsetSize = 8; return U64(); }
            offsetSize = 4;
            return len;
        }
    }

    public static string CStrAt(byte[] data, ulong offset)
    {
        if (data == null || offset >= (ulong)data.Length) return null;
        var end = Array.IndexOf(data, (byte)0, (int)offset);
        if (end < 0) end = data.Length;
        return Encoding.UTF8.GetString(data, (int)offset, end - (int)offset);
    }

    // ---- sections ----
    public sealed class Sections
    {
        public ElfKit.Elf Elf;
        public byte[] Info, Abbrev, Str, LineStr, StrOffsets, Addr, Line, Loc, LocLists, Ranges, RngLists, Types;
        public bool Little => Elf.Little;

        public static Sections Load(ElfKit.Elf elf) => new()
        {
            Elf = elf,
            Info = Get(elf, ".debug_info"), Abbrev = Get(elf, ".debug_abbrev"), Str = Get(elf, ".debug_str"),
            LineStr = Get(elf, ".debug_line_str"), StrOffsets = Get(elf, ".debug_str_offsets"), Addr = Get(elf, ".debug_addr"),
            Line = Get(elf, ".debug_line"), Loc = Get(elf, ".debug_loc"), LocLists = Get(elf, ".debug_loclists"),
            Ranges = Get(elf, ".debug_ranges"), RngLists = Get(elf, ".debug_rnglists"), Types = Get(elf, ".debug_types"),
        };

        public static byte[] Get(ElfKit.Elf elf, string name)
        {
            var section = elf.SectionByName(name) ?? elf.SectionByName(".z" + name[1..]);
            if (section == null) return null;
            var data = elf.Contents(section);
            if (elf.Type == 1) ApplyRelocations(elf, section, data);
            return data;
        }
    }

    // In relocatable objects the debug sections' cross-section offsets live in relocations (RELA
    // addends on x86-64/AArch64/RISC-V). Apply the absolute ones so offsets and addresses are right.
    static void ApplyRelocations(ElfKit.Elf elf, ElfKit.Section target, byte[] data)
    {
        foreach (var rs in elf.Sections.Where(s => (s.Type == ElfKit.SHT_RELA || s.Type == ElfKit.SHT_REL) && s.Info == (uint)target.Index))
        {
            if (rs.Link >= elf.Sections.Count) continue;
            var symbols = elf.Symbols(elf.Sections[(int)rs.Link]);
            var rela = rs.Type == ElfKit.SHT_RELA;
            var entsize = rs.EntSize != 0 ? (long)rs.EntSize : elf.Is64 ? (rela ? 24 : 16) : (rela ? 12 : 8);
            for (long o = (long)rs.Offset; o + entsize <= (long)(rs.Offset + rs.Size); o += entsize)
            {
                var offset = elf.Word(o);
                var info = elf.Word(o + elf.WordSize);
                long symIndex = elf.Is64 ? (long)(info >> 32) : (long)(info >> 8);
                var type = elf.Is64 ? (uint)(info & 0xffffffff) : (uint)(info & 0xff);
                var size = AbsRelocSize(elf.Machine, type);
                if (size == 0 || offset + (ulong)size > (ulong)data.Length) continue;
                long addend;
                if (rela) addend = elf.Is64 ? (long)elf.U64(o + 16) : (int)elf.U32(o + 8);
                else addend = size == 8 ? (long)Read(data, (int)offset, 8, elf.Little) : (int)Read(data, (int)offset, 4, elf.Little);
                var s = symIndex < symbols.Count ? symbols[(int)symIndex].Value : 0;
                Write(data, (int)offset, size, (ulong)((long)s + addend), elf.Little);
            }
        }
    }

    static int AbsRelocSize(ushort machine, uint type) => (machine, type) switch
    {
        (62, 1) => 8, (62, 10) => 4, (62, 11) => 4,      // R_X86_64_64, _32, _32S
        (3, 1) => 4,                                      // R_386_32
        (183, 257) => 8, (183, 258) => 4,                 // R_AARCH64_ABS64, ABS32
        (243, 1) => 4, (243, 2) => 8,                     // R_RISCV_32, _64
        (40, 2) => 4,                                     // R_ARM_ABS32
        (21, 38) => 8, (21, 1) => 4, (20, 1) => 4,        // R_PPC64_ADDR64, ADDR32; R_PPC_ADDR32
        (22, 22) => 8, (22, 4) => 4,                      // R_390_64, R_390_32
        (258, 1) => 4, (258, 2) => 8,                     // R_LARCH_32, _64
        _ => 0,
    };

    static ulong Read(byte[] d, int o, int size, bool little) => size == 8
        ? little ? BinaryPrimitives.ReadUInt64LittleEndian(d.AsSpan(o)) : BinaryPrimitives.ReadUInt64BigEndian(d.AsSpan(o))
        : little ? BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(o)) : BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(o));

    static void Write(byte[] d, int o, int size, ulong v, bool little)
    {
        if (size == 8) { if (little) BinaryPrimitives.WriteUInt64LittleEndian(d.AsSpan(o), v); else BinaryPrimitives.WriteUInt64BigEndian(d.AsSpan(o), v); }
        else { if (little) BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(o), (uint)v); else BinaryPrimitives.WriteUInt32BigEndian(d.AsSpan(o), (uint)v); }
    }

    // ---- abbreviations ----
    public sealed class Abbrev
    {
        public ulong Code, Tag;
        public bool Children;
        public List<(ulong Name, ulong Form, long Implicit)> Specs = [];
    }

    public static Dictionary<ulong, Abbrev> ReadAbbrevs(byte[] section, ulong offset, bool little)
    {
        var map = new Dictionary<ulong, Abbrev>();
        if (section == null) return map;
        var r = new Reader(section, little, (int)offset);
        while (!r.AtEnd)
        {
            var code = r.Uleb();
            if (code == 0) break;
            var a = new Abbrev { Code = code, Tag = r.Uleb(), Children = r.U8() != 0 };
            while (true)
            {
                var name = r.Uleb();
                var form = r.Uleb();
                long implicitValue = form == DW_FORM_implicit_const ? r.Sleb() : 0;
                if (name == 0 && form == 0) break;
                a.Specs.Add((name, form, implicitValue));
            }
            map[code] = a;
        }
        return map;
    }

    // ---- units and DIEs ----
    public const ulong DW_FORM_addr = 0x01, DW_FORM_block2 = 0x03, DW_FORM_block4 = 0x04, DW_FORM_data2 = 0x05, DW_FORM_data4 = 0x06,
        DW_FORM_data8 = 0x07, DW_FORM_string = 0x08, DW_FORM_block = 0x09, DW_FORM_block1 = 0x0a, DW_FORM_data1 = 0x0b,
        DW_FORM_flag = 0x0c, DW_FORM_sdata = 0x0d, DW_FORM_strp = 0x0e, DW_FORM_udata = 0x0f, DW_FORM_ref_addr = 0x10,
        DW_FORM_ref1 = 0x11, DW_FORM_ref2 = 0x12, DW_FORM_ref4 = 0x13, DW_FORM_ref8 = 0x14, DW_FORM_ref_udata = 0x15,
        DW_FORM_indirect = 0x16, DW_FORM_sec_offset = 0x17, DW_FORM_exprloc = 0x18, DW_FORM_flag_present = 0x19,
        DW_FORM_strx = 0x1a, DW_FORM_addrx = 0x1b, DW_FORM_ref_sup4 = 0x1c, DW_FORM_strp_sup = 0x1d, DW_FORM_data16 = 0x1e,
        DW_FORM_line_strp = 0x1f, DW_FORM_ref_sig8 = 0x20, DW_FORM_implicit_const = 0x21, DW_FORM_loclistx = 0x22,
        DW_FORM_rnglistx = 0x23, DW_FORM_ref_sup8 = 0x24, DW_FORM_strx1 = 0x25, DW_FORM_strx2 = 0x26, DW_FORM_strx3 = 0x27,
        DW_FORM_strx4 = 0x28, DW_FORM_addrx1 = 0x29, DW_FORM_addrx2 = 0x2a, DW_FORM_addrx3 = 0x2b, DW_FORM_addrx4 = 0x2c,
        DW_FORM_GNU_addr_index = 0x1f01, DW_FORM_GNU_str_index = 0x1f02, DW_FORM_GNU_ref_alt = 0x1f20, DW_FORM_GNU_strp_alt = 0x1f21;

    public const ulong DW_AT_sibling = 0x01, DW_AT_location = 0x02, DW_AT_name = 0x03, DW_AT_byte_size = 0x0b, DW_AT_stmt_list = 0x10,
        DW_AT_low_pc = 0x11, DW_AT_high_pc = 0x12, DW_AT_language = 0x13, DW_AT_comp_dir = 0x1b, DW_AT_const_value = 0x1c,
        DW_AT_producer = 0x25, DW_AT_data_member_location = 0x38, DW_AT_decl_file = 0x3a, DW_AT_call_file = 0x58,
        DW_AT_type = 0x49, DW_AT_ranges = 0x55, DW_AT_str_offsets_base = 0x72, DW_AT_addr_base = 0x73,
        DW_AT_rnglists_base = 0x74, DW_AT_loclists_base = 0x8c, DW_AT_GNU_addr_base = 0x2133, DW_AT_GNU_dwo_id = 0x2131,
        DW_AT_linkage_name = 0x6e, DW_AT_MIPS_linkage_name = 0x2007, DW_AT_abstract_origin = 0x31, DW_AT_specification = 0x47;

    public const ulong DW_TAG_compile_unit = 0x11, DW_TAG_partial_unit = 0x3c, DW_TAG_type_unit = 0x41, DW_TAG_skeleton_unit = 0x4a,
        DW_TAG_subprogram = 0x2e, DW_TAG_inlined_subroutine = 0x1d, DW_TAG_variable = 0x34, DW_TAG_base_type = 0x24;

    public sealed class Unit
    {
        public ulong Offset, Length, AbbrevOffset, UnitId, TypeOffset;
        public int Version, UnitType, AddressSize, OffsetSize;
        public int DiesStart, End;
        public bool IsTypeSection;   // from .debug_types (DWARF 4)
        public ulong StrOffsetsBase, AddrBase, LocListsBase, RngListsBase;
        public Dictionary<ulong, Abbrev> Abbrevs;
        public Die Root;
    }

    public sealed class Attr
    {
        public ulong Name, Form;
        public ulong U;          // constants, offsets, indexes, flags, addresses
        public long S;           // sdata / implicit_const
        public string Str;       // inline strings (resolved lazily for strx/strp)
        public byte[] Block;     // blocks, exprloc, data16
        public bool IsSigned => Form is DW_FORM_sdata or DW_FORM_implicit_const;
    }

    public sealed class Die
    {
        public ulong Offset;
        public int Level;
        public ulong Tag, AbbrevCode;
        public bool HasChildren;
        public List<Attr> Attrs = [];
        public Unit Unit;
        public Attr Get(ulong name) => Attrs.FirstOrDefault(a => a.Name == name);
    }

    // Every unit in .debug_info (and .debug_types), each with its DIEs in file order.
    public static IEnumerable<(Unit Unit, List<Die> Dies)> Units(Sections s, bool includeTypes = true)
    {
        foreach (var x in UnitsIn(s, s.Info, false)) yield return x;
        if (includeTypes && s.Types != null)
            foreach (var x in UnitsIn(s, s.Types, true)) yield return x;
    }

    static IEnumerable<(Unit, List<Die>)> UnitsIn(Sections s, byte[] info, bool typeSection)
    {
        if (info == null) yield break;
        var r = new Reader(info, s.Little);
        while (r.Pos + 4 <= info.Length)
        {
            var u = new Unit { Offset = (ulong)r.Pos, IsTypeSection = typeSection };
            u.Length = r.InitialLength(out u.OffsetSize);
            u.End = (int)Math.Min((ulong)info.Length, (ulong)r.Pos + u.Length);
            u.Version = r.U16();
            if (u.Version >= 5)
            {
                u.UnitType = r.U8();
                u.AddressSize = r.U8();
                u.AbbrevOffset = r.Sized(u.OffsetSize);
                if (u.UnitType is 4 or 5) u.UnitId = r.U64();
                else if (u.UnitType is 2 or 6) { u.UnitId = r.U64(); u.TypeOffset = r.Sized(u.OffsetSize); }
            }
            else
            {
                u.AbbrevOffset = r.Sized(u.OffsetSize);
                u.AddressSize = r.U8();
                u.UnitType = typeSection ? 2 : 1;
                if (typeSection) { u.UnitId = r.U64(); u.TypeOffset = r.Sized(u.OffsetSize); }
            }
            u.DiesStart = r.Pos;
            u.Abbrevs = ReadAbbrevs(s.Abbrev, u.AbbrevOffset, s.Little);
            var dies = ReadDies(s, info, u);
            u.Root = dies.FirstOrDefault();
            if (u.Root != null)
            {
                // DWARF 5 bases default to just past the respective section headers.
                u.StrOffsetsBase = u.Root.Get(DW_AT_str_offsets_base)?.U ?? (u.Version >= 5 ? (ulong)(u.OffsetSize == 8 ? 16 : 8) : 0);
                u.AddrBase = (u.Root.Get(DW_AT_addr_base) ?? u.Root.Get(DW_AT_GNU_addr_base))?.U ?? (u.Version >= 5 ? (ulong)(u.OffsetSize == 8 ? 16 : 8) : 0);
                u.LocListsBase = u.Root.Get(DW_AT_loclists_base)?.U ?? 0;
                u.RngListsBase = u.Root.Get(DW_AT_rnglists_base)?.U ?? 0;
            }
            yield return (u, dies);
            r.Pos = u.End;
            if (u.Length == 0) break;
        }
    }

    static List<Die> ReadDies(Sections s, byte[] info, Unit u)
    {
        var dies = new List<Die>();
        var r = new Reader(info, s.Little, u.DiesStart);
        var level = 0;
        while (r.Pos < u.End)
        {
            var offset = (ulong)r.Pos;
            var code = r.Uleb();
            if (code == 0)
            {
                if (level > 0) level--;
                continue;
            }
            if (!u.Abbrevs.TryGetValue(code, out var abbrev))
                throw new ElfKit.ElfError($"invalid DWARF (cannot get abbrev {code} for DIE [{offset:x}])");
            var die = new Die { Offset = offset, Level = level, Tag = abbrev.Tag, AbbrevCode = code, HasChildren = abbrev.Children, Unit = u };
            foreach (var (name, form, implicitValue) in abbrev.Specs)
                die.Attrs.Add(ReadAttr(r, u, name, form, implicitValue));
            dies.Add(die);
            if (abbrev.Children) level++;
        }
        return dies;
    }

    static Attr ReadAttr(Reader r, Unit u, ulong name, ulong form, long implicitValue)
    {
        var a = new Attr { Name = name, Form = form };
        switch (form)
        {
            case DW_FORM_addr: a.U = r.Sized(u.AddressSize); break;
            case DW_FORM_data1 or DW_FORM_ref1 or DW_FORM_flag or DW_FORM_strx1 or DW_FORM_addrx1: a.U = r.U8(); break;
            case DW_FORM_data2 or DW_FORM_ref2 or DW_FORM_strx2 or DW_FORM_addrx2: a.U = r.U16(); break;
            case DW_FORM_strx3 or DW_FORM_addrx3: a.U = r.U24(); break;
            case DW_FORM_data4 or DW_FORM_ref4 or DW_FORM_strx4 or DW_FORM_addrx4 or DW_FORM_ref_sup4: a.U = r.U32(); break;
            case DW_FORM_data8 or DW_FORM_ref8 or DW_FORM_ref_sig8 or DW_FORM_ref_sup8: a.U = r.U64(); break;
            case DW_FORM_data16: a.Block = r.Bytes(16); break;
            case DW_FORM_sdata: a.S = r.Sleb(); a.U = (ulong)a.S; break;
            case DW_FORM_udata or DW_FORM_ref_udata or DW_FORM_strx or DW_FORM_addrx or DW_FORM_loclistx or DW_FORM_rnglistx
                or DW_FORM_GNU_addr_index or DW_FORM_GNU_str_index: a.U = r.Uleb(); break;
            case DW_FORM_string: a.Str = r.CStr(); break;
            case DW_FORM_strp or DW_FORM_line_strp or DW_FORM_sec_offset or DW_FORM_strp_sup or DW_FORM_GNU_ref_alt or DW_FORM_GNU_strp_alt:
                a.U = r.Sized(u.OffsetSize); break;
            case DW_FORM_ref_addr: a.U = r.Sized(u.Version <= 2 ? u.AddressSize : u.OffsetSize); break;
            case DW_FORM_block1: a.Block = r.Bytes(r.U8()); break;
            case DW_FORM_block2: a.Block = r.Bytes(r.U16()); break;
            case DW_FORM_block4: a.Block = r.Bytes((int)r.U32()); break;
            case DW_FORM_block or DW_FORM_exprloc: a.Block = r.Bytes((int)r.Uleb()); break;
            case DW_FORM_flag_present: a.U = 1; break;
            case DW_FORM_implicit_const: a.S = implicitValue; a.U = (ulong)implicitValue; break;
            case DW_FORM_indirect:
                var actual = r.Uleb();
                var inner = ReadAttr(r, u, name, actual, 0);
                return inner;
            default: throw new ElfKit.ElfError($"invalid DWARF (unknown form 0x{form:x})");
        }
        if (form is DW_FORM_ref1 or DW_FORM_ref2 or DW_FORM_ref4 or DW_FORM_ref8 or DW_FORM_ref_udata)
            a.U += u.Offset;   // unit-relative references become section offsets
        return a;
    }

    public static bool IsStringForm(ulong f) => f is DW_FORM_string or DW_FORM_strp or DW_FORM_line_strp or DW_FORM_strx or DW_FORM_strx1
        or DW_FORM_strx2 or DW_FORM_strx3 or DW_FORM_strx4 or DW_FORM_GNU_str_index or DW_FORM_GNU_strp_alt or DW_FORM_strp_sup;
    public static bool IsAddrForm(ulong f) => f is DW_FORM_addr or DW_FORM_addrx or DW_FORM_addrx1 or DW_FORM_addrx2 or DW_FORM_addrx3
        or DW_FORM_addrx4 or DW_FORM_GNU_addr_index;
    public static bool IsRefForm(ulong f) => f is DW_FORM_ref1 or DW_FORM_ref2 or DW_FORM_ref4 or DW_FORM_ref8 or DW_FORM_ref_udata
        or DW_FORM_ref_addr or DW_FORM_GNU_ref_alt or DW_FORM_ref_sup4 or DW_FORM_ref_sup8;

    public static string String(Sections s, Unit u, Attr a)
    {
        switch (a.Form)
        {
            case DW_FORM_string: return a.Str;
            case DW_FORM_strp: return CStrAt(s.Str, a.U);
            case DW_FORM_line_strp: return CStrAt(s.LineStr, a.U);
            case DW_FORM_strx or DW_FORM_strx1 or DW_FORM_strx2 or DW_FORM_strx3 or DW_FORM_strx4 or DW_FORM_GNU_str_index:
                if (s.StrOffsets == null) return null;
                var at = u.StrOffsetsBase + a.U * (ulong)u.OffsetSize;
                if (at + (ulong)u.OffsetSize > (ulong)s.StrOffsets.Length) return null;
                var r = new Reader(s.StrOffsets, s.Little, (int)at);
                return CStrAt(s.Str, r.Sized(u.OffsetSize));
            default: return null;
        }
    }

    public static ulong Address(Sections s, Unit u, Attr a)
    {
        if (a.Form == DW_FORM_addr) return a.U;
        if (s.Addr == null) return 0;
        var at = u.AddrBase + a.U * (ulong)u.AddressSize;
        if (at + (ulong)u.AddressSize > (ulong)s.Addr.Length) return 0;
        return new Reader(s.Addr, s.Little, (int)at).Sized(u.AddressSize);
    }

    // ---- line programs ----
    public sealed class LineFile
    {
        public string Name;
        public ulong Dir, MTime, Length;
        public ulong NameOffset = ulong.MaxValue;   // line_strp/strp offset, printed by eu-readelf
        public ulong NameForm;
        public byte[] Md5;
    }

    public sealed class LineRow
    {
        public ulong Address;
        public ulong File = 1, Line = 1, Column, Discriminator;
        public bool IsStmt, BasicBlock, EndSequence, PrologueEnd, EpilogueBegin;
    }

    public sealed class LineTable
    {
        public ulong Offset, Length, HeaderLength;
        public int Version, OffsetSize, AddressSize, SegmentSelectorSize;
        public byte MinInstLength, MaxOps = 1, DefaultIsStmt, OpcodeBase, LineRange;
        public sbyte LineBase;
        public byte[] StandardLengths = [];
        public List<string> Dirs = [];
        public List<ulong> DirOffsets = [];
        public List<(ulong Type, ulong Form)> DirFormat = [], FileFormat = [];
        public List<LineFile> Files = [];
        public int ProgramStart, End;
        public List<LineRow> Rows = [];

        // File name for a DW_AT_decl_file index (1-based before DWARF 5, 0-based from 5).
        public string FilePath(ulong index, bool withDir)
        {
            var i = Version >= 5 ? (long)index : (long)index - 1;
            if (i < 0 || i >= Files.Count) return null;
            var f = Files[(int)i];
            if (!withDir || f.Name.StartsWith('/') || (f.Name.Length > 1 && f.Name[1] == ':')) return f.Name;
            var dirIndex = Version >= 5 ? (long)f.Dir : (long)f.Dir - 1;
            var dir = Version < 5 && f.Dir == 0 ? (Dirs.Count > 0 ? Dirs[0] : null) : dirIndex >= 0 && dirIndex < Dirs.Count ? Dirs[(int)dirIndex] : null;
            return string.IsNullOrEmpty(dir) ? f.Name : dir.TrimEnd('/') + "/" + f.Name;
        }
    }

    // Parses one line-number program header (and, with rows=true, runs the state machine).
    // compDir/compName fill DWARF 2-4's implicit directory 0 and file 0.
    public static LineTable ReadLineTable(Sections s, ulong offset, int addressSize, bool rows, string compDir = null)
    {
        var data = s.Line;
        if (data == null || offset >= (ulong)data.Length) return null;
        var r = new Reader(data, s.Little, (int)offset);
        var t = new LineTable { Offset = offset, AddressSize = addressSize };
        t.Length = r.InitialLength(out t.OffsetSize);
        t.End = (int)Math.Min((ulong)data.Length, (ulong)r.Pos + t.Length);
        t.Version = r.U16();
        if (t.Version >= 5) { t.AddressSize = r.U8(); t.SegmentSelectorSize = r.U8(); }
        t.HeaderLength = r.Sized(t.OffsetSize);
        t.ProgramStart = (int)((ulong)r.Pos + t.HeaderLength);
        t.MinInstLength = r.U8();
        if (t.Version >= 4) t.MaxOps = r.U8();
        t.DefaultIsStmt = r.U8();
        t.LineBase = (sbyte)r.U8();
        t.LineRange = r.U8();
        t.OpcodeBase = r.U8();
        t.StandardLengths = new byte[Math.Max(0, t.OpcodeBase - 1)];
        for (var i = 0; i < t.StandardLengths.Length; i++) t.StandardLengths[i] = r.U8();
        if (t.Version >= 5)
        {
            t.DirFormat = ReadEntryFormat(r);
            var dirCount = r.Uleb();
            for (ulong i = 0; i < dirCount; i++)
            {
                var f = ReadEntry(s, r, t, t.DirFormat);
                t.Dirs.Add(f.Name);
                t.DirOffsets.Add(f.NameOffset);
            }
            t.FileFormat = ReadEntryFormat(r);
            var fileCount = r.Uleb();
            for (ulong i = 0; i < fileCount; i++) t.Files.Add(ReadEntry(s, r, t, t.FileFormat));
        }
        else
        {
            if (compDir != null) { }
            while (true)
            {
                var dir = r.CStr();
                if (dir.Length == 0) break;
                t.Dirs.Add(dir);
            }
            while (true)
            {
                var name = r.CStr();
                if (name.Length == 0) break;
                t.Files.Add(new LineFile { Name = name, Dir = r.Uleb(), MTime = r.Uleb(), Length = r.Uleb() });
            }
        }
        if (rows) RunProgram(s, t, r);
        return t;
    }

    static List<(ulong, ulong)> ReadEntryFormat(Reader r)
    {
        var count = r.U8();
        var list = new List<(ulong, ulong)>();
        for (var i = 0; i < count; i++) list.Add((r.Uleb(), r.Uleb()));
        return list;
    }

    // DW_LNCT_path = 1, directory_index = 2, timestamp = 3, size = 4, MD5 = 5.
    static LineFile ReadEntry(Sections s, Reader r, LineTable t, List<(ulong Type, ulong Form)> format)
    {
        var f = new LineFile();
        var unit = new Unit { OffsetSize = t.OffsetSize, AddressSize = t.AddressSize, Version = 5 };
        foreach (var (type, form) in format)
        {
            var a = ReadAttr(r, unit, type, form, 0);
            switch (type)
            {
                case 1:
                    f.Name = IsStringForm(form) ? String(s, unit, a) ?? "???" : "???";
                    f.NameForm = form;
                    if (form is DW_FORM_line_strp or DW_FORM_strp) f.NameOffset = a.U;
                    break;
                case 2: f.Dir = a.U; break;
                case 3: f.MTime = a.U; break;
                case 4: f.Length = a.U; break;
                case 5: f.Md5 = a.Block; break;
            }
        }
        f.Name ??= "";
        return f;
    }

    static void RunProgram(Sections s, LineTable t, Reader r)
    {
        r.Pos = t.ProgramStart;
        var row = NewRow(t);
        while (r.Pos < t.End)
        {
            var op = r.U8();
            if (op >= t.OpcodeBase)
            {
                var adj = op - t.OpcodeBase;
                row.Address += (ulong)(adj / Math.Max((byte)1, t.LineRange) * t.MinInstLength);
                row.Line = (ulong)((long)row.Line + t.LineBase + adj % Math.Max((byte)1, t.LineRange));
                Emit(t, row);
                row.BasicBlock = row.PrologueEnd = row.EpilogueBegin = false;
                row.Discriminator = 0;
                continue;
            }
            switch (op)
            {
                case 0:
                    var len = (int)r.Uleb();
                    var end = r.Pos + len;
                    if (len == 0) break;
                    var ext = r.U8();
                    switch (ext)
                    {
                        case 1:
                            row.EndSequence = true;
                            Emit(t, row);
                            row = NewRow(t);
                            break;
                        case 2: row.Address = r.Sized(len - 1); break;
                        case 3:
                            var name = r.CStr();
                            t.Files.Add(new LineFile { Name = name, Dir = r.Uleb(), MTime = r.Uleb(), Length = r.Uleb() });
                            break;
                        case 4: row.Discriminator = r.Uleb(); break;
                    }
                    r.Pos = end;
                    break;
                case 1: Emit(t, row); row.BasicBlock = row.PrologueEnd = row.EpilogueBegin = false; row.Discriminator = 0; break;
                case 2: row.Address += r.Uleb() * t.MinInstLength; break;
                case 3: row.Line = (ulong)((long)row.Line + r.Sleb()); break;
                case 4: row.File = r.Uleb(); break;
                case 5: row.Column = r.Uleb(); break;
                case 6: row.IsStmt = !row.IsStmt; break;
                case 7: row.BasicBlock = true; break;
                case 8: row.Address += (ulong)((255 - t.OpcodeBase) / Math.Max((byte)1, t.LineRange) * t.MinInstLength); break;
                case 9: row.Address += r.U16(); break;
                case 10: row.PrologueEnd = true; break;
                case 11: row.EpilogueBegin = true; break;
                case 12: r.Uleb(); break;
                default:
                    for (var i = 0; i < t.StandardLengths[op - 1]; i++) r.Uleb();
                    break;
            }
        }
    }

    static LineRow NewRow(LineTable t) => new() { IsStmt = t.DefaultIsStmt != 0, File = 1, Line = 1 };

    static void Emit(LineTable t, LineRow row) => t.Rows.Add(new LineRow
    {
        Address = row.Address, File = row.File, Line = row.Line, Column = row.Column, Discriminator = row.Discriminator,
        IsStmt = row.IsStmt, BasicBlock = row.BasicBlock, EndSequence = row.EndSequence, PrologueEnd = row.PrologueEnd,
        EpilogueBegin = row.EpilogueBegin,
    });

    // ---- names (as elfutils' dwarf_*_name print them) ----
    static readonly Dictionary<ulong, string> TagNames = new()
    {
        [0x01] = "array_type", [0x02] = "class_type", [0x03] = "entry_point", [0x04] = "enumeration_type", [0x05] = "formal_parameter",
        [0x08] = "imported_declaration", [0x0a] = "label", [0x0b] = "lexical_block", [0x0d] = "member", [0x0f] = "pointer_type",
        [0x10] = "reference_type", [0x11] = "compile_unit", [0x12] = "string_type", [0x13] = "structure_type", [0x15] = "subroutine_type",
        [0x16] = "typedef", [0x17] = "union_type", [0x18] = "unspecified_parameters", [0x19] = "variant", [0x1a] = "common_block",
        [0x1b] = "common_inclusion", [0x1c] = "inheritance", [0x1d] = "inlined_subroutine", [0x1e] = "module", [0x1f] = "ptr_to_member_type",
        [0x20] = "set_type", [0x21] = "subrange_type", [0x22] = "with_stmt", [0x23] = "access_declaration", [0x24] = "base_type",
        [0x25] = "catch_block", [0x26] = "const_type", [0x27] = "constant", [0x28] = "enumerator", [0x29] = "file_type", [0x2a] = "friend",
        [0x2b] = "namelist", [0x2c] = "namelist_item", [0x2d] = "packed_type", [0x2e] = "subprogram", [0x2f] = "template_type_parameter",
        [0x30] = "template_value_parameter", [0x31] = "thrown_type", [0x32] = "try_block", [0x33] = "variant_part", [0x34] = "variable",
        [0x35] = "volatile_type", [0x36] = "dwarf_procedure", [0x37] = "restrict_type", [0x38] = "interface_type", [0x39] = "namespace",
        [0x3a] = "imported_module", [0x3b] = "unspecified_type", [0x3c] = "partial_unit", [0x3d] = "imported_unit", [0x3f] = "condition",
        [0x40] = "shared_type", [0x41] = "type_unit", [0x42] = "rvalue_reference_type", [0x43] = "template_alias", [0x44] = "coarray_type",
        [0x45] = "generic_subrange", [0x46] = "dynamic_type", [0x47] = "atomic_type", [0x48] = "call_site", [0x49] = "call_site_parameter",
        [0x4a] = "skeleton_unit", [0x4b] = "immutable_type", [0x4081] = "MIPS_loop", [0x4101] = "format_label", [0x4102] = "function_template",
        [0x4103] = "class_template", [0x4104] = "GNU_BINCL", [0x4105] = "GNU_EINCL", [0x4106] = "GNU_template_template_param",
        [0x4107] = "GNU_template_parameter_pack", [0x4108] = "GNU_formal_parameter_pack", [0x4109] = "GNU_call_site",
        [0x410a] = "GNU_call_site_parameter",
    };

    static readonly Dictionary<ulong, string> AttrNames = new()
    {
        [0x01] = "sibling", [0x02] = "location", [0x03] = "name", [0x09] = "ordering", [0x0b] = "byte_size", [0x0c] = "bit_offset",
        [0x0d] = "bit_size", [0x10] = "stmt_list", [0x11] = "low_pc", [0x12] = "high_pc", [0x13] = "language", [0x15] = "discr",
        [0x16] = "discr_value", [0x17] = "visibility", [0x18] = "import", [0x19] = "string_length", [0x1a] = "common_reference",
        [0x1b] = "comp_dir", [0x1c] = "const_value", [0x1d] = "containing_type", [0x1e] = "default_value", [0x20] = "inline",
        [0x21] = "is_optional", [0x22] = "lower_bound", [0x25] = "producer", [0x27] = "prototyped", [0x2a] = "return_addr",
        [0x2c] = "start_scope", [0x2e] = "bit_stride", [0x2f] = "upper_bound", [0x31] = "abstract_origin", [0x32] = "accessibility",
        [0x33] = "address_class", [0x34] = "artificial", [0x35] = "base_types", [0x36] = "calling_convention", [0x37] = "count",
        [0x38] = "data_member_location", [0x39] = "decl_column", [0x3a] = "decl_file", [0x3b] = "decl_line", [0x3c] = "declaration",
        [0x3d] = "discr_list", [0x3e] = "encoding", [0x3f] = "external", [0x40] = "frame_base", [0x41] = "friend",
        [0x42] = "identifier_case", [0x43] = "macro_info", [0x44] = "namelist_item", [0x45] = "priority", [0x46] = "segment",
        [0x47] = "specification", [0x48] = "static_link", [0x49] = "type", [0x4a] = "use_location", [0x4b] = "variable_parameter",
        [0x4c] = "virtuality", [0x4d] = "vtable_elem_location", [0x4e] = "allocated", [0x4f] = "associated", [0x50] = "data_location",
        [0x51] = "byte_stride", [0x52] = "entry_pc", [0x53] = "use_UTF8", [0x54] = "extension", [0x55] = "ranges", [0x56] = "trampoline",
        [0x57] = "call_column", [0x58] = "call_file", [0x59] = "call_line", [0x5a] = "description", [0x5b] = "binary_scale",
        [0x5c] = "decimal_scale", [0x5d] = "small", [0x5e] = "decimal_sign", [0x5f] = "digit_count", [0x60] = "picture_string",
        [0x61] = "mutable", [0x62] = "threads_scaled", [0x63] = "explicit", [0x64] = "object_pointer", [0x65] = "endianity",
        [0x66] = "elemental", [0x67] = "pure", [0x68] = "recursive", [0x69] = "signature", [0x6a] = "main_subprogram",
        [0x6b] = "data_bit_offset", [0x6c] = "const_expr", [0x6d] = "enum_class", [0x6e] = "linkage_name",
        [0x6f] = "string_length_bit_size", [0x70] = "string_length_byte_size", [0x71] = "rank", [0x72] = "str_offsets_base",
        [0x73] = "addr_base", [0x74] = "rnglists_base", [0x76] = "dwo_name", [0x77] = "reference", [0x78] = "rvalue_reference",
        [0x79] = "macros", [0x7a] = "call_all_calls", [0x7b] = "call_all_source_calls", [0x7c] = "call_all_tail_calls",
        [0x7d] = "call_return_pc", [0x7e] = "call_value", [0x7f] = "call_origin", [0x80] = "call_parameter", [0x81] = "call_pc",
        [0x82] = "call_tail_call", [0x83] = "call_target", [0x84] = "call_target_clobbered", [0x85] = "call_data_location",
        [0x86] = "call_data_value", [0x87] = "noreturn", [0x88] = "alignment", [0x89] = "export_symbols", [0x8a] = "deleted",
        [0x8b] = "defaulted", [0x8c] = "loclists_base",
        [0x2007] = "MIPS_linkage_name", [0x2101] = "sf_names", [0x2102] = "src_info", [0x2103] = "mac_info", [0x2104] = "src_coords",
        [0x2105] = "body_begin", [0x2106] = "body_end", [0x2107] = "GNU_vector", [0x2108] = "GNU_guarded_by",
        [0x2109] = "GNU_pt_guarded_by", [0x210a] = "GNU_guarded", [0x210b] = "GNU_pt_guarded", [0x210c] = "GNU_locks_excluded",
        [0x210d] = "GNU_exclusive_locks_required", [0x210e] = "GNU_shared_locks_required", [0x210f] = "GNU_odr_signature",
        [0x2110] = "GNU_template_name", [0x2111] = "GNU_call_site_value", [0x2112] = "GNU_call_site_data_value",
        [0x2113] = "GNU_call_site_target", [0x2114] = "GNU_call_site_target_clobbered", [0x2115] = "GNU_tail_call",
        [0x2116] = "GNU_all_tail_call_sites", [0x2117] = "GNU_all_call_sites", [0x2118] = "GNU_all_source_call_sites",
        [0x2119] = "GNU_macros", [0x211a] = "GNU_deleted", [0x2130] = "GNU_dwo_name", [0x2131] = "GNU_dwo_id",
        [0x2132] = "GNU_ranges_base", [0x2133] = "GNU_addr_base", [0x2134] = "GNU_pubnames", [0x2135] = "GNU_pubtypes",
        [0x2136] = "GNU_discriminator", [0x2137] = "GNU_locviews", [0x2138] = "GNU_entry_view",
    };

    static readonly Dictionary<ulong, string> FormNames = new()
    {
        [0x01] = "addr", [0x03] = "block2", [0x04] = "block4", [0x05] = "data2", [0x06] = "data4", [0x07] = "data8", [0x08] = "string",
        [0x09] = "block", [0x0a] = "block1", [0x0b] = "data1", [0x0c] = "flag", [0x0d] = "sdata", [0x0e] = "strp", [0x0f] = "udata",
        [0x10] = "ref_addr", [0x11] = "ref1", [0x12] = "ref2", [0x13] = "ref4", [0x14] = "ref8", [0x15] = "ref_udata", [0x16] = "indirect",
        [0x17] = "sec_offset", [0x18] = "exprloc", [0x19] = "flag_present", [0x1a] = "strx", [0x1b] = "addrx", [0x1c] = "ref_sup4",
        [0x1d] = "strp_sup", [0x1e] = "data16", [0x1f] = "line_strp", [0x20] = "ref_sig8", [0x21] = "implicit_const",
        [0x22] = "loclistx", [0x23] = "rnglistx", [0x24] = "ref_sup8", [0x25] = "strx1", [0x26] = "strx2", [0x27] = "strx3",
        [0x28] = "strx4", [0x29] = "addrx1", [0x2a] = "addrx2", [0x2b] = "addrx3", [0x2c] = "addrx4", [0x1f01] = "GNU_addr_index",
        [0x1f02] = "GNU_str_index", [0x1f20] = "GNU_ref_alt", [0x1f21] = "GNU_strp_alt",
    };

    static readonly string[] Languages =
    [
        "", "C89", "C", "Ada83", "C_plus_plus", "Cobol74", "Cobol85", "Fortran77", "Fortran90", "Pascal83", "Modula2", "Java", "C99",
        "Ada95", "Fortran95", "PLI", "ObjC", "ObjC_plus_plus", "UPC", "D", "Python", "OpenCL", "Go", "Modula3", "Haskell",
        "C_plus_plus_03", "C_plus_plus_11", "OCaml", "Rust", "C11", "Swift", "Julia", "Dylan", "C_plus_plus_14", "Fortran03",
        "Fortran08", "RenderScript", "BLISS", "Kotlin", "Zig", "Crystal", "", "C_plus_plus_17", "C_plus_plus_20", "C17", "Fortran18",
        "Ada2005", "Ada2012", "HIP", "Assembly", "C_sharp", "Mojo", "GLSL", "GLSL_ES", "HLSL", "OpenCL_CPP", "CPP_for_OpenCL", "SYCL",
        "C_plus_plus_23", "Odin", "P4", "Metal", "C23", "Fortran23", "Ruby", "Move", "Hylo",
    ];

    public static string TagName(ulong t) => TagNames.TryGetValue(t, out var n) ? n
        : t >= 0x4080 && t <= 0xffff ? $"lo_user+0x{t - 0x4080:x}" : $"??? (0x{t:x})";
    public static string AttrName(ulong a) => AttrNames.TryGetValue(a, out var n) ? n
        : a >= 0x2000 && a <= 0x3fff ? $"lo_user+0x{a - 0x2000:x}" : $"??? (0x{a:x})";
    public static string FormName(ulong f) => FormNames.TryGetValue(f, out var n) ? n : $"??? (0x{f:x})";
    public static string LanguageName(ulong l) => l < (ulong)Languages.Length && Languages[l].Length > 0 ? Languages[l]
        : l == 0x8001 ? "Mips_Assembler" : $"??? (0x{l:x})";
    public static string UnitTypeName(int t) => t switch
    {
        1 => "compile", 2 => "type", 3 => "partial", 4 => "skeleton", 5 => "split_compile", 6 => "split_type", _ => "???",
    };

    // Value names for the enumerated attributes eu-readelf decodes ("signed (5)").
    public static string ValueName(ulong attr, ulong v) => attr switch
    {
        0x13 => LanguageName(v),
        0x3e => v switch
        {
            1 => "address", 2 => "boolean", 3 => "complex_float", 4 => "float", 5 => "signed", 6 => "signed_char", 7 => "unsigned",
            8 => "unsigned_char", 9 => "imaginary_float", 10 => "packed_decimal", 11 => "numeric_string", 12 => "edited",
            13 => "signed_fixed", 14 => "unsigned_fixed", 15 => "decimal_float", 16 => "UTF", 17 => "UCS", 18 => "ASCII", _ => "???",
        },
        0x32 => v switch { 1 => "public", 2 => "protected", 3 => "private", _ => "???" },
        0x8b => v switch { 0 => "no", 1 => "in_class", 2 => "out_of_class", _ => "???" },
        0x17 => v switch { 1 => "local", 2 => "exported", 3 => "qualified", _ => "???" },
        0x4c => v switch { 0 => "none", 1 => "virtual", 2 => "pure_virtual", _ => "???" },
        0x42 => v switch { 0 => "case_sensitive", 1 => "up_case", 2 => "down_case", 3 => "case_insensitive", _ => "???" },
        0x36 => v switch { 1 => "normal", 2 => "program", 3 => "nocall", 4 => "pass_by_reference", 5 => "pass_by_value", _ => "???" },
        0x20 => v switch { 0 => "not_inlined", 1 => "inlined", 2 => "declared_not_inlined", 3 => "declared_inlined", _ => "???" },
        0x09 => v switch { 0 => "row_major", 1 => "col_major", _ => "???" },
        _ => null,
    };

    // ---- location expressions ----
    static string OpName(byte op) => op switch
    {
        0x03 => "addr", 0x06 => "deref", 0x08 => "const1u", 0x09 => "const1s", 0x0a => "const2u", 0x0b => "const2s",
        0x0c => "const4u", 0x0d => "const4s", 0x0e => "const8u", 0x0f => "const8s", 0x10 => "constu", 0x11 => "consts",
        0x12 => "dup", 0x13 => "drop", 0x14 => "over", 0x15 => "pick", 0x16 => "swap", 0x17 => "rot", 0x18 => "xderef",
        0x19 => "abs", 0x1a => "and", 0x1b => "div", 0x1c => "minus", 0x1d => "mod", 0x1e => "mul", 0x1f => "neg", 0x20 => "not",
        0x21 => "or", 0x22 => "plus", 0x23 => "plus_uconst", 0x24 => "shl", 0x25 => "shr", 0x26 => "shra", 0x27 => "xor",
        0x28 => "bra", 0x29 => "eq", 0x2a => "ge", 0x2b => "gt", 0x2c => "le", 0x2d => "lt", 0x2e => "ne", 0x2f => "skip",
        >= 0x30 and <= 0x4f => $"lit{op - 0x30}", >= 0x50 and <= 0x6f => $"reg{op - 0x50}", >= 0x70 and <= 0x8f => $"breg{op - 0x70}",
        0x90 => "regx", 0x91 => "fbreg", 0x92 => "bregx", 0x93 => "piece", 0x94 => "deref_size", 0x95 => "xderef_size",
        0x96 => "nop", 0x97 => "push_object_address", 0x98 => "call2", 0x99 => "call4", 0x9a => "call_ref",
        0x9b => "form_tls_address", 0x9c => "call_frame_cfa", 0x9d => "bit_piece", 0x9e => "implicit_value", 0x9f => "stack_value",
        0xa0 => "implicit_pointer", 0xa1 => "addrx", 0xa2 => "constx", 0xa3 => "entry_value", 0xa4 => "const_type",
        0xa5 => "regval_type", 0xa6 => "deref_type", 0xa7 => "xderef_type", 0xa8 => "convert", 0xa9 => "reinterpret",
        0xe0 => "GNU_push_tls_address", 0xf0 => "GNU_uninit", 0xf1 => "GNU_encoded_addr", 0xf2 => "GNU_implicit_pointer",
        0xf3 => "GNU_entry_value", 0xf4 => "GNU_const_type", 0xf5 => "GNU_regval_type", 0xf6 => "GNU_deref_type",
        0xf7 => "GNU_convert", 0xf9 => "GNU_reinterpret", 0xfa => "GNU_parameter_ref", 0xfb => "GNU_addr_index",
        0xfc => "GNU_const_index", 0xfd => "GNU_variable_value",
        _ => $"??? (0x{op:x})",
    };

    // eu-readelf's print_ops: one "[off] op operands" line per operation, at the given indent.
    public static void PrintOps(TextWriter w, byte[] expr, int indent, Unit u, bool little, Func<ulong, string> address)
    {
        var r = new Reader(expr, little);
        while (!r.AtEnd)
        {
            var at = r.Pos;
            var op = r.U8();
            var name = OpName(op);
            var pad = new string(' ', indent);
            string operands;
            try
            {
                switch (op)
                {
                    case 0x03: operands = " " + address(r.Sized(u.AddressSize)); break;
                    case 0x08 or 0x15 or 0x94 or 0x95: operands = $" {r.U8()}"; break;
                    case 0x09: operands = $" {(sbyte)r.U8()}"; break;
                    case 0x0a: operands = $" {r.U16()}"; break;
                    case 0x0b: operands = $" {(short)r.U16()}"; break;
                    case 0x0c: operands = $" {r.U32()}"; break;
                    case 0x0d: operands = $" {(int)r.U32()}"; break;
                    case 0x0e: operands = $" {r.U64()}"; break;
                    case 0x0f: operands = $" {(long)r.U64()}"; break;
                    case 0x10 or 0x23 or 0x90 or 0x93 or 0xa1 or 0xa2 or 0xa8 or 0xa9 or 0xf7 or 0xf9 or 0xfb or 0xfc: operands = $" {r.Uleb()}"; break;
                    case 0x11 or 0x91 or (>= 0x70 and <= 0x8f): operands = $" {r.Sleb()}"; break;
                    case 0x92: { var reg = r.Uleb(); operands = $" {reg} {r.Sleb()}"; break; }
                    case 0x28 or 0x2f: { var skip = (short)r.U16(); operands = $" {(long)r.Pos + skip}"; break; }
                    case 0x98: operands = $" [{r.U16(),6:x}]"; break;
                    case 0x99: operands = $" [{r.U32(),6:x}]"; break;
                    case 0x9a or 0xfd: operands = $" [{r.Sized(u.OffsetSize),6:x}]"; break;
                    case 0x9d: { var size = r.Uleb(); operands = $" {size} offset {r.Uleb()}"; break; }
                    case 0x9e: { var len = (int)r.Uleb(); operands = $" {len} " + Hex(r.Bytes(len)); break; }
                    case 0xa0 or 0xf2: { var die = r.Sized(u.OffsetSize); operands = $" [{die,6:x}] {r.Sleb()}"; break; }
                    case 0xa3 or 0xf3:
                    {
                        var len = (int)r.Uleb();
                        var inner = r.Bytes(len);
                        w.WriteLine($"{pad}[{at,2}] {name}:");
                        PrintOps(w, inner, indent + 6, u, little, address);
                        continue;
                    }
                    case 0xa4 or 0xf4: { var type = r.Uleb(); var len = r.U8(); operands = $" [{type,6:x}] " + Hex(r.Bytes(len)); break; }
                    case 0xa5 or 0xf5: { var reg = r.Uleb(); operands = $" {reg} [{r.Uleb(),6:x}]"; break; }
                    case 0xa6 or 0xa7 or 0xf6: { var size = r.U8(); operands = $" {size} [{r.Uleb(),6:x}]"; break; }
                    case 0xfa: operands = $" {r.U32()}"; break;
                    case 0xf1: operands = $" {r.U8()}"; break;
                    default: operands = ""; break;
                }
            }
            catch (ElfKit.ElfError)
            {
                w.WriteLine($"{pad}[{at,2}] {name} <TRUNCATED>");
                return;
            }
            w.WriteLine($"{pad}[{at,2}] {name}{operands}");
        }
    }

    public static string Hex(byte[] b) => string.Join(" ", b.Select(x => x.ToString("x2")));
}
