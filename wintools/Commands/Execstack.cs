using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace WinTools;

// execstack: query, set or clear the executable-stack flag. ELF objects carry it in the PT_GNU_STACK
// program header's PF_X bit; Windows PE images carry the opposite, the NX_COMPAT (DEP) flag in
// DllCharacteristics, so `-s` clears NX_COMPAT and `-c` sets it. Output follows prelink's execstack:
// "X file" (executable stack), "- file" (not), "? file" (no marking).
static class Execstack
{
    const ushort NxCompat = 0x0100;

    [ModuleInitializer]
    internal static void Register() => Commands.Add("execstack", "Set, clear or query the executable stack flag of ELF and PE files", Run);

    static int Run(string[] args)
    {
        char mode = 'q';
        var files = new List<string>();
        foreach (var a in args)
        {
            switch (a)
            {
                case "-?" or "-h" or "--help":
                    Console.Write("""
                        Usage: execstack [OPTION...] OBJECT...
                        execstack -- program to query or set executable stack flag

                          -c, --clear-execstack      Clear executable stack flag bit
                          -q, --query                Query executable stack flag bit
                          -s, --set-execstack        Set executable stack flag bit
                          -?, --help                 Give this help list
                              --usage                Give a short usage message
                          -V, --version              Print program version

                        On Windows PE files (.exe/.dll) the flag is the inverse of NX_COMPAT (DEP).

                        """);
                    return 0;
                case "--usage":
                    Console.WriteLine("Usage: execstack [-cqs?V] [--clear-execstack] [--query] [--set-execstack] [--help] [--usage] [--version] OBJECT...");
                    return 0;
                case "-V" or "--version": Console.WriteLine("execstack 1.0"); return 0;
                case "-q" or "--query": mode = 'q'; break;
                case "-s" or "--set-execstack": mode = 's'; break;
                case "-c" or "--clear-execstack": mode = 'c'; break;
                default:
                    if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    files.Add(a);
                    break;
            }
        }
        if (files.Count == 0) throw new UsageException("no files given (see execstack --help)");
        var status = 0;
        foreach (var file in files)
        {
            try { status |= ProcessFile(file, mode); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ElfKit.ElfError)
            {
                Console.Error.WriteLine($"execstack: {file}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    static int ProcessFile(string file, char mode)
    {
        if (!File.Exists(file)) throw new IOException("No such file or directory");
        var data = File.ReadAllBytes(file);
        if (ElfKit.Elf.IsElf(data)) return Elf(new ElfKit.Elf(file, data), mode);
        if (data.Length > 0x40 && data[0] == 'M' && data[1] == 'Z') return Pe(file, data, mode);
        throw new ElfKit.ElfError("not an ELF or PE file");
    }

    static int Elf(ElfKit.Elf elf, char mode)
    {
        var stack = elf.Segments.FirstOrDefault(p => p.Type == ElfKit.PT_GNU_STACK);
        if (mode == 'q')
        {
            Console.WriteLine($"{(stack == null ? '?' : (stack.Flags & ElfKit.PF_X) != 0 ? 'X' : '-')} {elf.Path}");
            return 0;
        }
        if (stack == null)
        {
            // Turn a spare PT_NULL entry into PT_GNU_STACK, as prelink does when there is room.
            stack = elf.Segments.FirstOrDefault(p => p.Type == ElfKit.PT_NULL)
                ?? throw new ElfKit.ElfError("no PT_GNU_STACK program header and no free program header slot to add one");
            stack.Type = ElfKit.PT_GNU_STACK;
            elf.W32(stack.HeaderOffset, ElfKit.PT_GNU_STACK);
            elf.WriteSegmentFlags(stack, ElfKit.PF_R | ElfKit.PF_W);
        }
        var flags = mode == 's' ? stack.Flags | ElfKit.PF_X : stack.Flags & ~ElfKit.PF_X;
        if (flags != stack.Flags)
        {
            elf.WriteSegmentFlags(stack, flags);
            elf.Save();
        }
        return 0;
    }

    static int Pe(string file, byte[] data, char mode)
    {
        var pe = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x3c));
        if (pe <= 0 || pe + 24 + 72 > data.Length || data[pe] != 'P' || data[pe + 1] != 'E' || data[pe + 2] != 0 || data[pe + 3] != 0)
            throw new ElfKit.ElfError("not an ELF or PE file");
        var optional = pe + 24;
        var dllCharacteristics = optional + 70;
        var value = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(dllCharacteristics));
        if (mode == 'q')
        {
            Console.WriteLine($"{((value & NxCompat) != 0 ? '-' : 'X')} {file}");
            return 0;
        }
        var updated = mode == 's' ? (ushort)(value & ~NxCompat) : (ushort)(value | NxCompat);
        if (updated == value) return 0;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(dllCharacteristics), updated);
        // Keep a present image checksum valid (it is checked for drivers and boot images).
        var checksumOffset = optional + 64;
        if (BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(checksumOffset)) != 0)
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(checksumOffset), PeChecksum(data, checksumOffset));
        File.WriteAllBytes(file, data);
        return 0;
    }

    // The PE image checksum (what CheckSumMappedFile computes): 16-bit one's-complement-style sum of
    // the file with the checksum field treated as zero, plus the file length.
    internal static uint PeChecksum(byte[] data, int checksumOffset)
    {
        ulong sum = 0;
        for (var i = 0; i < data.Length; i += 2)
        {
            if (i == checksumOffset || i == checksumOffset + 2) continue;
            uint word = data[i];
            if (i + 1 < data.Length) word |= (uint)data[i + 1] << 8;
            sum += word;
            sum = (sum & 0xffff) + (sum >> 16);
        }
        sum = (sum & 0xffff) + (sum >> 16);
        return (uint)sum + (uint)data.Length;
    }
}
