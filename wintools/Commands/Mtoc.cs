using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// mtoc (cctools' and acidanthera's ocmtoc): convert a Mach-O image into a PE/COFF image, the way
// EFI firmware (OpenCore, EDK2 on macOS toolchains) is built from Mach-O objects. Pure file
// conversion, so it runs the same on Windows: each Mach-O segment with sections becomes a PE section
// at the same address relative to the image base, the entry point comes from LC_UNIXTHREAD or
// LC_MAIN, and every pointer the image needs slid (local relocations, rebase opcodes or chained
// fixups) becomes a .reloc base relocation. -d adds a CodeView debug entry naming the debug file.
static class Mtoc
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("mtoc", "Convert a Mach-O image to a PE/COFF (EFI) image", Run);

    sealed class Options
    {
        public ushort Subsystem = 10;   // EFI_APPLICATION
        public uint SectionAlignment = 0x20, FileAlignment = 0x20;
        public string DebugFile, Input, Output;
        public bool Verbose;
    }

    static int Run(string[] args)
    {
        var o = new Options();
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"missing argument to {args[i - 1]}");
            switch (args[i])
            {
                case "-h" or "--help" or "-help":
                    Console.WriteLine("""
                        Usage: mtoc [-subsystem type] [-section_alignment hexvalue] [-align hexvalue]
                                    [-d debug_filename] [-v] input_Mach-O output_pe_file
                        Convert a Mach-O image (MH_PRELOAD, MH_DYLIB, MH_BUNDLE or MH_EXECUTE) to PE/COFF.

                          -subsystem type          application (default), app, UEFI_APPLICATION,
                                                   boot/bsdrv/DXE_DRIVER/UEFI_DRIVER (boot service driver),
                                                   runtime/rtdrv/DXE_RUNTIME_DRIVER, rom, or a number
                          -section_alignment hex   PE section alignment (default 0x20)
                          -align hex               PE file alignment (default 0x20)
                          -d debug_filename        add a CodeView debug entry naming the debug file
                          -v                       print the layout of the image written
                        """);
                    return 0;
                case "-subsystem": o.Subsystem = Subsystem(Value()); break;
                case "-section_alignment": o.SectionAlignment = Hex(Value()); break;
                case "-align": o.FileAlignment = Hex(Value()); break;
                case "-d": o.DebugFile = Value(); break;
                case "-u": Value(); break;   // dSYM UUID file: the UUID comes from LC_UUID instead
                case "-ddebug_filename" or "-generate_debug": break;
                case "-v": o.Verbose = true; break;
                default:
                    if (args[i].StartsWith('-') && args[i].Length > 1) throw new UsageException($"unknown flag: {args[i]}");
                    files.Add(args[i]);
                    break;
            }
        }
        if (files.Count != 2) throw new UsageException("expected an input Mach-O file and an output PE file (see mtoc -h)");
        if (!IsPowerOfTwo(o.SectionAlignment) || !IsPowerOfTwo(o.FileAlignment) || o.FileAlignment > o.SectionAlignment)
            throw new UsageException("alignments must be powers of two, with -align no larger than -section_alignment");
        (o.Input, o.Output) = (files[0], files[1]);
        try
        {
            var image = MachO.Load(o.Input);
            File.WriteAllBytes(o.Output, Convert(image, o));
            return 0;
        }
        catch (MachO.FormatException e)
        {
            Console.Error.WriteLine($"mtoc: {o.Input}: {e.Message}");
            return 1;
        }
    }

    static bool IsPowerOfTwo(uint v) => v != 0 && (v & (v - 1)) == 0;

    static uint Hex(string s)
    {
        var t = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s;
        return uint.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out var v) ? v : throw new UsageException($"bad hex value: {s}");
    }

    static ushort Subsystem(string s) => s switch
    {
        "application" or "app" or "UEFI_APPLICATION" or "APPLICATION" => 10,
        "boot" or "bsdrv" or "DXE_DRIVER" or "SEC" or "peim" or "BASE" or "PEI_CORE" or "PEIM" or "DXE_SMM_DRIVER"
            or "TOOL" or "USER_DEFINED" or "UEFI_DRIVER" or "DXE_CORE" or "SMM_CORE" or "MM_STANDALONE" => 11,
        "runtime" or "rtdrv" or "DXE_RUNTIME_DRIVER" or "DXE_SAL_DRIVER" => 12,
        "rom" => 13,
        _ => ushort.TryParse(s, out var n) ? n : throw new UsageException($"unknown subsystem: {s}"),
    };

    sealed class PeSection
    {
        public string Name;
        public ulong Va;   // Mach-O address
        public uint Rva, VirtualSize, FileOffset, Characteristics;
        public byte[] Raw;
    }

    // Lays the segments out at their Mach-O addresses relative to an image base chosen so the PE
    // headers fit just below the first section.
    static byte[] Convert(MachO m, Options o)
    {
        var (machine, pe64) = m.CpuType switch
        {
            MachO.CpuX86_64 => ((ushort)0x8664, true),
            MachO.CpuArm64 => ((ushort)0xaa64, true),
            MachO.CpuX86 => ((ushort)0x14c, false),
            MachO.CpuArm => ((ushort)0x1c2, false),
            _ => throw new MachO.FormatException($"unsupported architecture {m.Arch}"),
        };
        if (m.FileType is not (2 or 5 or 6 or 8))
            throw new MachO.FormatException($"input must be a linked image (MH_PRELOAD, MH_DYLIB, MH_BUNDLE or MH_EXECUTE), not {m.FileTypeName}");
        if (m.EntryAddress == null && m.FileType is 2 or 5)
            throw new MachO.FormatException("no entry point (LC_UNIXTHREAD or LC_MAIN); link with -e <symbol>");
        var rebases = m.Rebases();   // also resolves chained fixups in m.Data

        var segments = m.Segments.Where(s => s.Sections.Count > 0 && s.Name != "__PAGEZERO" && s.Name != "__LINKEDIT").ToList();
        if (segments.Count == 0) throw new MachO.FormatException("no segments with sections");
        var align = o.SectionAlignment;
        var sections = new List<PeSection>();
        foreach (var seg in segments)
        {
            var start = AlignDown(seg.Sections.Min(s => s.Addr), align);
            var end = seg.Sections.Max(s => s.Addr + s.Size);
            var bytes = new byte[end - start];
            foreach (var s in seg.Sections.Where(s => !s.ZeroFill && s.Size > 0))
                m.SectionData(s).CopyTo(bytes, (long)(s.Addr - start));
            // Trailing zero fill needs no file data.
            var initialized = seg.Sections.Where(s => !s.ZeroFill).Select(s => s.Addr + s.Size).DefaultIfEmpty(start).Max() - start;
            var name = seg.Name switch
            {
                "__TEXT" => ".text",
                "__DATA" => ".data",
                "__DATA_CONST" or "__CONST" => ".rdata",
                _ => seg.Executable ? ".text" : seg.Writable ? ".data" : ".rdata",
            };
            uint flags = seg.Executable ? 0x60000020u                 // CODE | EXECUTE | READ
                       : seg.Writable ? 0xc0000040u                   // INITIALIZED_DATA | READ | WRITE
                       : 0x40000040u;                                 // INITIALIZED_DATA | READ
            sections.Add(new PeSection { Name = name, Va = start, VirtualSize = (uint)(end - start), Raw = bytes[..(int)initialized], Characteristics = flags });
        }
        sections.Sort((a, b) => a.Va.CompareTo(b.Va));
        for (var i = 1; i < sections.Count; i++)
            if (sections[i].Va < sections[i - 1].Va + sections[i - 1].VirtualSize)
                throw new MachO.FormatException($"segments overlap at section alignment 0x{align:x}; try a smaller -section_alignment");

        var debugData = o.DebugFile != null ? CodeView(m.Uuid ?? new byte[16], o.DebugFile) : null;
        var extra = (rebases.Count > 0 ? 1 : 0) + (debugData != null ? 1 : 0);

        const uint dosSize = 0x40;
        var optSize = pe64 ? 240u : 224u;
        var headerSize = dosSize + 4 + 20 + optSize + (uint)(sections.Count + extra) * 40;
        var headersAligned = AlignUp(headerSize, align);
        var firstVa = sections[0].Va;
        if (firstVa < headersAligned)
            throw new MachO.FormatException($"no room below 0x{firstVa:x} for the PE headers; link with a higher -seg1addr");
        var imageBase = firstVa - headersAligned;
        if (sections[^1].Va + sections[^1].VirtualSize - imageBase > 0x7fffffff)
            throw new MachO.FormatException("image is larger than 2 GiB");
        foreach (var s in sections) s.Rva = (uint)(s.Va - imageBase);
        var relocData = BaseRelocations(rebases, sections, pe64, imageBase);
        // Sections must be contiguous: each one's virtual size runs up to the next.
        for (var i = 0; i < sections.Count - 1; i++) sections[i].VirtualSize = sections[i + 1].Rva - sections[i].Rva;

        if (relocData.Length > 0)
        {
            var last = sections[^1];
            sections.Add(new PeSection { Name = ".reloc", Rva = AlignUp(last.Rva + last.VirtualSize, align), VirtualSize = (uint)relocData.Length, Raw = relocData, Characteristics = 0x42000040 });
        }
        PeSection debug = null;
        if (debugData != null)
        {
            var last = sections[^1];
            debug = new PeSection { Name = ".debug", Rva = AlignUp(last.Rva + last.VirtualSize, align), Characteristics = 0x42000040 };
            // IMAGE_DEBUG_DIRECTORY (28 bytes) followed by the CodeView record it points at.
            var dir = new byte[28 + debugData.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(dir.AsSpan(12), 2);                 // IMAGE_DEBUG_TYPE_CODEVIEW
            BinaryPrimitives.WriteUInt32LittleEndian(dir.AsSpan(16), (uint)debugData.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(dir.AsSpan(20), debug.Rva + 28);   // AddressOfRawData
            debugData.CopyTo(dir, 28);
            debug.Raw = dir;
            debug.VirtualSize = (uint)dir.Length;
            sections.Add(debug);
        }

        // File layout: headers, then each section's initialized data at the file alignment.
        var fileAlign = o.FileAlignment;
        var offset = AlignUp(headerSize, fileAlign);
        foreach (var s in sections)
        {
            s.FileOffset = s.Raw.Length == 0 ? 0 : offset;
            offset += AlignUp((uint)s.Raw.Length, fileAlign);
        }
        if (debug != null) BinaryPrimitives.WriteUInt32LittleEndian(debug.Raw.AsSpan(24), debug.FileOffset + 28);   // PointerToRawData
        var image = new byte[offset];
        var w = new Writer(image);

        // DOS header: "MZ" and e_lfanew.
        w.U16(0, 0x5a4d);
        w.U32(0x3c, dosSize);
        var pe = (int)dosSize;
        w.U32(pe, 0x00004550);
        var coff = pe + 4;
        w.U16(coff, machine);
        w.U16(coff + 2, (ushort)sections.Count);
        w.U16(coff + 16, (ushort)optSize);
        // EXECUTABLE_IMAGE | LINE_NUMS_STRIPPED | LOCAL_SYMS_STRIPPED, LARGE_ADDRESS_AWARE or 32BIT_MACHINE,
        // RELOCS_STRIPPED when there are none, DLL for libraries.
        ushort ch = 0x0002 | 0x0004 | 0x0008;
        ch |= pe64 ? (ushort)0x0020 : (ushort)0x0100;
        if (relocData.Length == 0) ch |= 0x0001;
        if (m.FileType is 6 or 8) ch |= 0x2000;
        w.U16(coff + 18, ch);

        var opt = coff + 20;
        w.U16(opt, pe64 ? (ushort)0x20b : (ushort)0x10b);
        uint code = 0, init = 0, uninit = 0;
        foreach (var s in sections)
        {
            if ((s.Characteristics & 0x20) != 0) code += AlignUp(s.VirtualSize, fileAlign);
            else init += AlignUp((uint)s.Raw.Length, fileAlign);
            if ((s.Characteristics & 0x40) != 0 && s.VirtualSize > s.Raw.Length) uninit += s.VirtualSize - (uint)s.Raw.Length;
        }
        w.U32(opt + 4, code);
        w.U32(opt + 8, init);
        w.U32(opt + 12, uninit);
        w.U32(opt + 16, m.EntryAddress is { } e ? (uint)(e - imageBase) : 0);
        var text = sections.FirstOrDefault(s => (s.Characteristics & 0x20) != 0);
        w.U32(opt + 20, text?.Rva ?? 0);                                   // BaseOfCode
        int dd;
        if (pe64)
        {
            w.U64(opt + 24, imageBase);
            dd = opt + 112;
        }
        else
        {
            var data = sections.FirstOrDefault(s => (s.Characteristics & 0x20) == 0 && s.Name != ".reloc");
            w.U32(opt + 24, data?.Rva ?? 0);                               // BaseOfData
            w.U32(opt + 28, (uint)imageBase);
            dd = opt + 96;
        }
        w.U32(opt + 32, align);
        w.U32(opt + 36, fileAlign);
        var lastSection = sections[^1];
        w.U32(opt + 56, AlignUp(lastSection.Rva + lastSection.VirtualSize, align));   // SizeOfImage
        w.U32(opt + 60, AlignUp(headerSize, fileAlign));                                // SizeOfHeaders
        w.U16(opt + 68, o.Subsystem);
        if (pe64) w.U32(opt + 108, 16); else w.U32(opt + 92, 16);                     // NumberOfRvaAndSizes
        var reloc = sections.FirstOrDefault(s => s.Name == ".reloc");
        if (reloc != null) { w.U32(dd + 5 * 8, reloc.Rva); w.U32(dd + 5 * 8 + 4, reloc.VirtualSize); }
        if (debug != null) { w.U32(dd + 6 * 8, debug.Rva); w.U32(dd + 6 * 8 + 4, 28); }

        var table = opt + (int)optSize;
        foreach (var s in sections)
        {
            Encoding.ASCII.GetBytes(s.Name).CopyTo(image, table);
            w.U32(table + 8, s.VirtualSize);
            w.U32(table + 12, s.Rva);
            w.U32(table + 16, AlignUp((uint)s.Raw.Length, fileAlign));
            w.U32(table + 20, s.FileOffset);
            w.U32(table + 36, s.Characteristics);
            s.Raw.CopyTo(image, s.FileOffset);
            table += 40;
        }
        // The PE checksum algorithm (as imagehlp computes it).
        w.U32(opt + 64, Checksum(image, opt + 64));

        if (o.Verbose)
        {
            Console.WriteLine($"{o.Input}: {m.Arch} {m.FileTypeName} -> PE32{(pe64 ? "+" : "")} image base 0x{imageBase:x}, entry 0x{m.EntryAddress ?? 0:x}");
            foreach (var s in sections)
                Console.WriteLine($"  {s.Name,-8} rva 0x{s.Rva:x8} size 0x{s.VirtualSize:x8} file 0x{s.FileOffset:x8}+0x{s.Raw.Length:x}");
            Console.WriteLine($"  {rebases.Count} base relocations");
        }
        return image;
    }

    // IMAGE_BASE_RELOCATION blocks, one per 4 KiB page of RVAs.
    static byte[] BaseRelocations(List<MachO.Rebase> rebases, List<PeSection> sections, bool pe64, ulong imageBase)
    {
        var ms = new MemoryStream();
        foreach (var page in rebases.GroupBy(r => (r.Address - imageBase) & ~0xfffUL).OrderBy(g => g.Key))
        {
            var entries = new List<ushort>();
            foreach (var r in page)
            {
                if (!sections.Any(s => r.Address >= s.Va && r.Address + (ulong)r.Width <= s.Va + s.VirtualSize))
                    throw new MachO.FormatException($"relocated pointer at 0x{r.Address:x} is outside the image's sections");
                if (r.Width == 8 && !pe64) throw new MachO.FormatException("64-bit pointer in a 32-bit image");
                var type = r.Width == 8 ? 10 : 3;   // IMAGE_REL_BASED_DIR64 / HIGHLOW
                entries.Add((ushort)(type << 12 | (int)((r.Address - imageBase) & 0xfff)));
            }
            if (entries.Count % 2 == 1) entries.Add(0);   // IMAGE_REL_BASED_ABSOLUTE padding
            var block = new byte[8 + entries.Count * 2];
            BinaryPrimitives.WriteUInt32LittleEndian(block, (uint)page.Key);
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), (uint)block.Length);
            for (var i = 0; i < entries.Count; i++) BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(8 + i * 2), entries[i]);
            ms.Write(block);
        }
        return ms.ToArray();
    }

    // mtoc's CodeView record: the "MTOC" signature, the Mach-O UUID and the debug file's path.
    static byte[] CodeView(byte[] uuid, string debugFile)
    {
        var name = Encoding.UTF8.GetBytes(debugFile);
        var record = new byte[4 + 16 + name.Length + 1];
        Encoding.ASCII.GetBytes("MTOC").CopyTo(record, 0);
        uuid.CopyTo(record, 4);
        name.CopyTo(record, 20);
        return record;
    }

    static uint Checksum(byte[] image, int checksumOffset)
    {
        ulong sum = 0;
        for (var i = 0; i + 1 < image.Length; i += 2)
        {
            if (i == checksumOffset || i == checksumOffset + 2) continue;
            sum += BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(i));
            sum = (sum & 0xffff) + (sum >> 16);
        }
        if (image.Length % 2 == 1) { sum += image[^1]; sum = (sum & 0xffff) + (sum >> 16); }
        sum = (sum & 0xffff) + (sum >> 16);
        return (uint)sum + (uint)image.Length;
    }

    static ulong AlignDown(ulong v, uint a) => v & ~(ulong)(a - 1);
    static uint AlignUp(uint v, uint a) => (v + a - 1) & ~(a - 1);

    readonly struct Writer(byte[] b)
    {
        public void U16(int p, ushort v) => BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(p), v);
        public void U32(int p, uint v) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(p), v);
        public void U64(int p, ulong v) => BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(p), v);
    }
}
