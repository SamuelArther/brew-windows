using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// The other elfutils programs over the shared ELF/DWARF reader: eu-nm, eu-size, eu-strings,
// eu-elfclassify, eu-elfcmp, eu-addr2line, eu-srcfiles and eu-strip.
static class EuTools
{
    const string Version = "eu-{0} (elfutils) 0.196";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("eu-nm", "List symbols from ELF files (elfutils)", Nm);
        Commands.Add("eu-size", "List section sizes of ELF files (elfutils)", Size);
        Commands.Add("eu-strings", "Print the strings of printable characters in files (elfutils)", Strings);
        Commands.Add("eu-elfclassify", "Classify ELF files: executable, shared library, debug-only... (elfutils)", Classify);
        Commands.Add("eu-elfcmp", "Compare the relevant parts of two ELF files (elfutils)", Cmp);
        Commands.Add("eu-addr2line", "Map addresses to source file and line using DWARF (elfutils)", Addr2line);
        Commands.Add("eu-srcfiles", "List the source files of an ELF file's DWARF (elfutils)", Srcfiles);
        Commands.Add("eu-strip", "Discard symbols and debug info from ELF files (elfutils)", Strip);
    }

    static ElfKit.Elf Load(string tool, string file)
    {
        try { return ElfKit.Elf.Load(file); }
        catch (ElfKit.ElfError e)
        {
            Console.Error.WriteLine(e.Message == "not an ELF file" ? $"eu-{tool}: {file}: File format not recognized" : $"eu-{tool}: cannot open '{file}': {e.Message}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"eu-{tool}: cannot open '{file}': {e.Message}");
        }
        return null;
    }

    static bool Common(string tool, string arg, string usage)
    {
        if (arg is "--help" or "-?" or "-h" && !(tool is "nm" or "size" && arg == "-h"))
        {
            Console.Write(usage);
            return true;
        }
        if (arg is "--version" or "-V")
        {
            Console.WriteLine(string.Format(Version, tool));
            return true;
        }
        return false;
    }

    // ---- eu-nm ----
    static int Nm(string[] args)
    {
        string format = "sysv", sort = "name", radix = "x";
        bool all = false, fileNames = false, dynamic = false, externOnly = false, undefinedOnly = false, definedOnly = false, reverse = false, printSize = false;
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (Common("nm", a, """
                Usage: eu-nm [OPTION...] [FILE...]
                List symbols from FILEs (a.out by default).

                  -a, --debug-syms           Display debugger-only symbols
                  -A, -o, --print-file-name  Print name of the input file before every symbol
                  -B                         Same as --format=bsd
                  -D, --dynamic              Display dynamic symbols instead of normal symbols
                  -f, --format=FORMAT        Use the output format FORMAT: bsd, sysv or posix (default sysv)
                  -g, --extern-only          Display only external symbols
                  -u, --undefined-only       Display only undefined symbols
                      --defined-only         Display only defined symbols
                  -n, -v                     Sort symbols numerically by address
                  -p, --no-sort              Do not sort the symbols
                  -r, --reverse-sort         Reverse the sense of the sort
                  -P, --portability          Same as --format=posix
                  -S, --print-size           Print size of defined symbols
                  -t, --radix=RADIX          Use RADIX for printing symbol values (d, o or x)
                  -C, --demangle             Accepted for compatibility (names are printed as stored)

                """)) return 0;
            switch (a)
            {
                case "-a" or "--debug-syms": all = true; break;
                case "-A" or "-o" or "--print-file-name": fileNames = true; break;
                case "-B": format = "bsd"; break;
                case "-P" or "--portability": format = "posix"; break;
                case "-D" or "--dynamic": dynamic = true; break;
                case "-g" or "--extern-only": externOnly = true; break;
                case "-u" or "--undefined-only": undefinedOnly = true; break;
                case "--defined-only": definedOnly = true; break;
                case "-n" or "-v" or "--numeric-sort": sort = "address"; break;
                case "-p" or "--no-sort": sort = "none"; break;
                case "-r" or "--reverse-sort": reverse = true; break;
                case "-S" or "--print-size": printSize = true; break;
                case "-C" or "--demangle" or "-l" or "--line-numbers" or "--mark-special" or "-s" or "--print-armap": break;
                case "-f" or "-t":
                    if (++i >= args.Length) throw new UsageException($"option requires an argument -- '{a[1]}'");
                    if (a == "-f") format = args[i]; else radix = args[i];
                    break;
                default:
                    if (a.StartsWith("--format=")) format = a[9..];
                    else if (a.StartsWith("--radix=")) radix = a[8..];
                    else if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    else files.Add(a);
                    break;
            }
        }
        if (format is not ("sysv" or "bsd" or "posix")) throw new UsageException($"invalid format: {format}");
        if (radix is not ("x" or "d" or "o")) throw new UsageException($"invalid radix: {radix}");
        if (files.Count == 0) files.Add("a.out");
        var status = 0;
        foreach (var file in files)
        {
            var elf = Load("nm", file);
            if (elf == null) { status = 1; continue; }
            var table = elf.Sections.FirstOrDefault(s => s.Type == (dynamic ? ElfKit.SHT_DYNSYM : ElfKit.SHT_SYMTAB));
            if (table == null)
            {
                Console.Error.WriteLine($"eu-nm: {file}: no symbols");
                status = 1;
                continue;
            }
            var syms = elf.Symbols(table).Skip(1)
                .Where(s => all || s.Type is not (3 or 4))
                .Where(s => !externOnly || s.Bind != 0)
                .Where(s => !undefinedOnly || s.Shndx == 0)
                .Where(s => !definedOnly || s.Shndx != 0)
                .ToList();
            if (sort == "name") syms = syms.OrderBy(s => s.Name, StringComparer.Ordinal).ThenBy(s => s.Value).ToList();
            else if (sort == "address") syms = syms.OrderBy(s => s.Value).ThenBy(s => s.Name, StringComparer.Ordinal).ToList();
            if (reverse) syms.Reverse();
            var digits = elf.Is64 ? 16 : 8;
            string Num(ulong v, int width) => radix switch
            {
                "d" => v.ToString().PadLeft(width, '0'),
                "o" => Convert.ToString((long)v, 8).PadLeft(width, '0'),
                _ => v.ToString("x").PadLeft(width, '0'),
            };
            string Name(ElfKit.Symbol s) => s.Type == 3 && s.Name.Length == 0 && s.Shndx < elf.Sections.Count ? elf.Sections[s.Shndx].Name
                : s.Version == null ? s.Name : s.Name + (s.Shndx == 0 || s.VersionHidden ? "@" : "@@") + s.Version;
            var prefix = fileNames ? file + ":" : "";
            if (format == "sysv")
            {
                var longest = Math.Max(4, syms.Select(s => Name(s).Length).DefaultIfEmpty(0).Max());
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"Symbols from {file}:");
                Console.WriteLine();
                Console.WriteLine($"{"Name".PadRight(longest)} {"Value".PadRight(digits)} Class  Type     {"Size".PadRight(digits)} Line  Section");
                Console.WriteLine();
                foreach (var s in syms)
                {
                    var section = s.Shndx switch
                    {
                        0 => "UNDEF", 0xfff1 => "ABS", 0xfff2 => "COMMON",
                        _ => s.Shndx < elf.Sections.Count ? elf.Sections[s.Shndx].Name : "",
                    };
                    var value = s.Shndx == 0 ? new string(' ', digits) : Num(s.Value, digits);
                    var size = s.Size == 0 && s.Shndx == 0 ? new string(' ', digits) : Num(s.Size, digits);
                    Console.WriteLine($"{prefix}{Name(s).PadRight(longest)}|{value}|{ElfKit.SymbolBindName(s.Bind),-6}|{ElfKit.SymbolTypeName(s.Type),-8}|{size}|     |{section}");
                }
            }
            else
            {
                if (!fileNames && files.Count > 1) Console.WriteLine($"\n{file}:");
                foreach (var s in syms)
                {
                    var letter = TypeLetter(elf, s);
                    if (format == "posix")
                    {
                        var tail = s.Shndx == 0 ? "" : $" {Num(s.Value, digits)} {Num(s.Size, digits)}";
                        Console.WriteLine($"{prefix}{Name(s)} {letter}{tail}");
                    }
                    else
                    {
                        var value = s.Shndx == 0 ? new string(' ', digits) : Num(s.Value, digits);
                        var size = printSize && s.Shndx != 0 ? " " + Num(s.Size, digits) : "";
                        Console.WriteLine($"{prefix}{value}{size} {letter} {Name(s)}");
                    }
                }
            }
        }
        return status;
    }

    static char TypeLetter(ElfKit.Elf elf, ElfKit.Symbol s)
    {
        if (s.Bind == 10) return 'u';
        if (s.Type == 10) return 'i';
        if (s.Shndx == 0) return s.Bind == 2 ? (s.Type == 1 ? 'v' : 'w') : 'U';
        if (s.Bind == 2) return s.Type == 1 ? 'V' : 'W';
        char c;
        if (s.Shndx == 0xfff1) c = 'A';
        else if (s.Shndx == 0xfff2) c = 'C';
        else if (s.Shndx >= elf.Sections.Count) c = '?';
        else
        {
            var sec = elf.Sections[s.Shndx];
            if (sec.Name.StartsWith(".debug") || (sec.Flags & ElfKit.SHF_ALLOC) == 0) c = 'N';
            else if ((sec.Flags & ElfKit.SHF_EXECINSTR) != 0) c = 'T';
            else if (sec.Type == ElfKit.SHT_NOBITS) c = 'B';
            else if ((sec.Flags & ElfKit.SHF_WRITE) != 0) c = 'D';
            else c = 'R';
        }
        return s.Bind == 0 ? char.ToLowerInvariant(c) : c;
    }

    // ---- eu-size ----
    static int Size(string[] args)
    {
        string format = "bsd", radix = "d";
        var totals = false;
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (Common("size", a, """
                Usage: eu-size [OPTION...] [FILE...]
                List section sizes of FILEs (a.out by default).

                  -A                    Same as `--format=sysv'
                  -B                    Same as `--format=bsd'
                  -d                    Same as `--radix=10'
                  -f                    Similar to `--format=sysv' output but in one line
                      --format=FORMAT   Use the output format FORMAT: `bsd' (default) or `sysv'
                  -o                    Same as `--radix=8'
                      --radix=RADIX     Use RADIX for printing symbol values (8, 10 or 16)
                  -t, --totals          Display the total sizes (bsd only)
                  -x                    Same as `--radix=16'

                """)) return 0;
            switch (a)
            {
                case "-A": format = "sysv"; break;
                case "-B": format = "bsd"; break;
                case "-f": format = "oneline"; break;
                case "-d": radix = "d"; break;
                case "-o": radix = "o"; break;
                case "-x": radix = "x"; break;
                case "-t" or "--totals": totals = true; break;
                default:
                    if (a.StartsWith("--format=")) format = a[9..] switch { "sysv" or "s" => "sysv", "bsd" or "b" => "bsd", var f => throw new UsageException($"invalid format: {f}") };
                    else if (a.StartsWith("--radix=")) radix = a[8..] switch { "8" or "o" => "o", "10" or "d" => "d", "16" or "x" => "x", var r => throw new UsageException($"invalid radix: {r}") };
                    else if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    else files.Add(a);
                    break;
            }
        }
        if (files.Count == 0) files.Add("a.out");
        string N(ulong v) => radix switch { "o" => "0" + Convert.ToString((long)v, 8), "x" => "0x" + v.ToString("x"), _ => v.ToString() };
        var status = 0;
        ulong tt = 0, td = 0, tb = 0;
        var headerShown = false;
        foreach (var file in files)
        {
            var elf = Load("size", file);
            if (elf == null) { status = 1; continue; }
            ulong text = 0, data = 0, bss = 0;
            foreach (var s in elf.Sections.Where(s => (s.Flags & ElfKit.SHF_ALLOC) != 0))
            {
                if (s.Type == ElfKit.SHT_NOBITS) bss += s.Size;
                else if ((s.Flags & ElfKit.SHF_WRITE) != 0) data += s.Size;
                else text += s.Size;
            }
            if (format == "bsd")
            {
                if (!headerShown)
                {
                    Console.WriteLine(radix == "o" ? "   text\t   data\t    bss\t    oct\t    hex\tfilename" : "   text\t   data\t    bss\t    dec\t    hex\tfilename");
                    headerShown = true;
                }
                var sum = text + data + bss;
                Console.WriteLine($"{N(text),7}\t{N(data),7}\t{N(bss),7}\t{(radix == "o" ? N(sum) : sum.ToString()),7}\t{sum,7:x}\t{file}");
                tt += text; td += data; tb += bss;
            }
            else if (format == "oneline")
            {
                var parts = elf.Sections.Where(s => (s.Flags & ElfKit.SHF_ALLOC) != 0).Select(s => $"{N(s.Size)}({s.Name})");
                Console.WriteLine(string.Join(" + ", parts) + $" = {N(text + data + bss)}");
            }
            else
            {
                Console.WriteLine($"{file}  :");
                var secs = elf.Sections.Where(s => (s.Flags & ElfKit.SHF_ALLOC) != 0).ToList();
                var nameWidth = Math.Max(7, secs.Select(s => s.Name.Length).DefaultIfEmpty(0).Max());
                Console.WriteLine($"{"section".PadRight(nameWidth)} {"size",10} {"addr",10}");
                foreach (var s in secs) Console.WriteLine($"{s.Name.PadRight(nameWidth)} {N(s.Size),10} {N(s.Addr),10}");
                Console.WriteLine($"{"Total".PadRight(nameWidth)} {N(text + data + bss),10}");
                Console.WriteLine();
            }
        }
        if (totals && format == "bsd")
        {
            var sum = tt + td + tb;
            Console.WriteLine($"{N(tt),7}\t{N(td),7}\t{N(tb),7}\t{(radix == "o" ? N(sum) : sum.ToString()),7}\t{sum,7:x}\t(TOTALS)");
        }
        return status;
    }

    // ---- eu-strings ----
    static int Strings(string[] args)
    {
        var min = 4;
        string radix = null;
        var encoding = 's';
        bool printName = false, loadedOnly = false;
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (Common("strings", a, """
                Usage: eu-strings [OPTION...] [FILE...]
                Print the strings of printable characters in files.

                  -a, --all                  Scan entire file, not only loaded sections (default)
                  -d                         Only NUL-terminated sequences in loaded sections are printed
                  -e, --encoding=SELECTOR    Character size and endianness: s (7-bit), S (8-bit),
                                             b/l (16-bit big/little endian), B/L (32-bit)
                  -f, --print-file-name      Print name of the file before each string
                  -n, --bytes=MIN-LEN        Print sequences of at least MIN-LEN characters (default 4)
                  -o                         Alias for --radix=o
                  -t, --radix={o,d,x}        Print location of the string in base 8, 10, or 16

                """)) return 0;
            switch (a)
            {
                case "-a" or "--all": loadedOnly = false; break;
                case "-d" or "--data": loadedOnly = true; break;
                case "-f" or "--print-file-name": printName = true; break;
                case "-o": radix = "o"; break;
                case "-n" or "-t" or "-e":
                    if (++i >= args.Length) throw new UsageException($"option requires an argument -- '{a[1]}'");
                    if (a == "-n") min = int.Parse(args[i]);
                    else if (a == "-t") radix = args[i];
                    else encoding = args[i][0];
                    break;
                default:
                    if (a.StartsWith("--bytes=")) min = int.Parse(a[8..]);
                    else if (a.StartsWith("--radix=")) radix = a[8..];
                    else if (a.StartsWith("--encoding=")) encoding = a[11];
                    else if (a.Length > 1 && a[0] == '-' && char.IsDigit(a[1])) min = int.Parse(a[1..]);
                    else if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    else files.Add(a);
                    break;
            }
        }
        if (min < 1) throw new UsageException("invalid minimum length of matched string size");
        if (radix is not (null or "o" or "d" or "x")) throw new UsageException($"invalid value '{radix}' for --radix");
        if ("sSbBlL".IndexOf(encoding) < 0) throw new UsageException($"invalid value '{encoding}' for --encoding");
        var status = 0;
        var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = false };
        void Scan(string name, byte[] data, IEnumerable<(long Start, long End)> ranges)
        {
            var width = encoding switch { 'b' or 'l' => 2, 'B' or 'L' => 4, _ => 1 };
            var big = encoding is 'b' or 'B';
            foreach (var (start, end) in ranges)
            {
                var sb = new StringBuilder();
                long runStart = -1;
                for (var p = start; p + width <= end; p += width)
                {
                    uint c = width switch
                    {
                        1 => data[p],
                        2 => big ? (uint)(data[p] << 8 | data[p + 1]) : (uint)(data[p] | data[p + 1] << 8),
                        _ => big ? (uint)(data[p] << 24 | data[p + 1] << 16 | data[p + 2] << 8 | data[p + 3]) : (uint)(data[p] | data[p + 1] << 8 | data[p + 2] << 16 | data[p + 3] << 24),
                    };
                    var printable = c == '\t' || (c >= 0x20 && c < 0x7f) || (encoding == 'S' && c >= 0xa0 && c <= 0xff);
                    if (printable)
                    {
                        if (runStart < 0) runStart = p;
                        sb.Append((char)c);
                        continue;
                    }
                    Flush(p);
                }
                Flush(end);

                void Flush(long _)
                {
                    if (runStart >= 0 && sb.Length >= min)
                    {
                        var where = radix switch
                        {
                            "o" => Convert.ToString(runStart, 8).PadLeft(7) + " ",
                            "d" => runStart.ToString().PadLeft(7) + " ",
                            "x" => runStart.ToString("x").PadLeft(7) + " ",
                            _ => "",
                        };
                        stdout.Write(printName ? $"{name}: {where}{sb}\n" : $"{where}{sb}\n");
                    }
                    sb.Clear();
                    runStart = -1;
                }
            }
        }
        if (files.Count == 0)
        {
            using var ms = new MemoryStream();
            Console.OpenStandardInput().CopyTo(ms);
            Scan("{standard input}", ms.ToArray(), [(0, ms.Length)]);
        }
        foreach (var file in files)
        {
            byte[] data;
            try { data = File.ReadAllBytes(file); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"eu-strings: '{file}': {e.Message}");
                status = 1;
                continue;
            }
            IEnumerable<(long, long)> ranges = [(0, data.Length)];
            if (loadedOnly && ElfKit.Elf.IsElf(data))
            {
                try
                {
                    var elf = new ElfKit.Elf(file, data);
                    ranges = elf.Segments.Where(p => p.Type == ElfKit.PT_LOAD)
                        .Select(p => ((long)p.Offset, (long)Math.Min((ulong)data.Length, p.Offset + p.FileSize))).ToList();
                }
                catch (ElfKit.ElfError) { }
            }
            Scan(file, data, ranges);
        }
        stdout.Flush();
        return status;
    }

    // ---- eu-elfclassify ----
    sealed class Classification
    {
        public bool Elf, File, Archive, Core, Program, Shared, KernelModule, DebugOnly, Loadable, Unstripped;
    }

    static Classification ClassifyFile(string path)
    {
        var c = new Classification();
        byte[] data;
        try { data = File.ReadAllBytes(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return null; }
        if (data.Length >= 8 && Encoding.ASCII.GetString(data, 0, 8) == "!<arch>\n")
        {
            c.Archive = c.Elf = true;
            return c;
        }
        if (!ElfKit.Elf.IsElf(data)) return c;
        ElfKit.Elf elf;
        try { elf = new ElfKit.Elf(path, data); }
        catch (ElfKit.ElfError) { return c; }
        c.Elf = c.File = true;
        c.Core = elf.Type == 4;
        var interp = elf.Segments.Any(p => p.Type == ElfKit.PT_INTERP);
        var dyns = elf.Dynamic();
        var soname = dyns.Any(d => d.Tag == ElfKit.DT_SONAME);
        var pie = dyns.Any(d => d.Tag == 0x6ffffffb && (d.Value & 0x08000000) != 0);
        var hasDebug = elf.Sections.Any(s => s.Name.StartsWith(".debug_") || s.Name.StartsWith(".zdebug_"));
        var hasSymtab = elf.Sections.Any(s => s.Type == ElfKit.SHT_SYMTAB);
        var allocProgbits = elf.Sections.Any(s => (s.Flags & ElfKit.SHF_ALLOC) != 0 && s.Type != ElfKit.SHT_NOBITS && s.Type != ElfKit.SHT_NOTE && s.Size > 0);
        c.KernelModule = elf.Type == 1 && elf.SectionByName(".modinfo") != null && elf.SectionByName(".gnu.linkonce.this_module") != null;
        c.DebugOnly = (hasDebug || hasSymtab) && !allocProgbits && elf.Type is 2 or 3 or 1;
        c.Program = !c.DebugOnly && (elf.Type == 2 || (elf.Type == 3 && (pie || (interp && !soname))));
        c.Shared = !c.DebugOnly && elf.Type == 3 && !pie && (soname || !interp);
        c.Loadable = !c.DebugOnly && (elf.Type is 2 or 3 || c.KernelModule);
        c.Unstripped = !c.DebugOnly && (hasSymtab || hasDebug);
        return c;
    }

    static int Classify(string[] args)
    {
        var tests = new List<(string Name, bool Want)>();
        bool matching = true, quiet = false, print = false, print0 = false, fromStdin = false, fromStdin0 = false;
        var files = new List<string>();
        var negate = false;
        foreach (var a in args)
        {
            if (Common("elfclassify", a, """
                Usage: eu-elfclassify [OPTION...] FILE...
                Determine the type of an ELF file.

                All of the classification options must apply at the same time to a particular
                file. Exit status 0 means all files match, 1 means at least one did not, 2 is an error.

                Classification options:
                      --core                 File is an ELF core dump file
                      --debug-only           File is a debug-only ELF file (separate debuginfo)
                      --elf                  File looks like an ELF object or archive/static library (default)
                      --elf-archive          File is an ELF archive or static library
                      --elf-file             File is an regular ELF object (not an archive/static library)
                      --executable           File is an ELF executable or shared object (PIE)
                      --linux-kernel-module  File is a linux kernel module
                      --loadable             File is a loadable ELF object (program or shared object)
                      --program              File is an ELF program (executable or PIE)
                      --shared               File is an ELF shared object (DSO)
                      --unstripped           File contains symbol table or debug information
                      --not-<option>         Negate the following classification (e.g. --not-shared)

                Input flags:
                  -f, --file                 Only classify regular (not symlink nor special device) files
                      --stdin                Also read file names to process from standard input, separated by newlines
                      --stdin0               Also read file names to process from standard input, separated by ASCII NUL bytes

                Output flags:
                      --matching             If printing file names, print matching files (the default)
                      --no-print             Do not output file names
                      --not-matching         If printing file names, print files that do not match
                      --print                Output names of files, separated by newline
                      --print0               Output names of files, separated by ASCII NUL

                  -q, --quiet                Suppress some error output (counterpart to --verbose)
                  -v, --verbose              Output additional information (can be specified multiple times)

                """)) return 0;
            var name = a;
            if (a.StartsWith("--not-") && a is not "--not-matching") { name = "--" + a[6..]; negate = true; }
            switch (name)
            {
                case "--elf" or "--elf-file" or "--elf-archive" or "--core" or "--program" or "--executable" or "--shared" or "--library"
                    or "--linux-kernel-module" or "--debug-only" or "--loadable" or "--unstripped" or "--file":
                    tests.Add((name, !negate));
                    negate = false;
                    break;
                case "-f": tests.Add(("--file", true)); break;
                case "--matching": matching = true; break;
                case "--not-matching": matching = false; break;
                case "--print": print = true; break;
                case "--print0": print0 = true; break;
                case "--no-print": print = print0 = false; break;
                case "--stdin": fromStdin = true; break;
                case "--stdin0": fromStdin0 = true; break;
                case "-q" or "--quiet": quiet = true; break;
                case "-v" or "--verbose": break;
                default:
                    if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"unrecognized option '{a}'");
                    files.Add(a);
                    break;
            }
        }
        if (tests.Count == 0) tests.Add(("--elf", true));
        if (fromStdin || fromStdin0)
        {
            var input = Console.In.ReadToEnd();
            files.AddRange(input.Split(fromStdin0 ? '\0' : '\n', StringSplitOptions.RemoveEmptyEntries));
        }
        var status = 0;
        foreach (var file in files)
        {
            var isRegular = File.Exists(file) && !new FileInfo(file).Attributes.HasFlag(FileAttributes.ReparsePoint);
            var c = File.Exists(file) ? ClassifyFile(file) : null;
            if (c == null)
            {
                if (!quiet) Console.Error.WriteLine($"eu-elfclassify: opening {file}: No such file or directory");
                status = Math.Max(status, 2);
                continue;
            }
            var ok = tests.All(t => t.Want == t.Name switch
            {
                "--elf" => c.Elf,
                "--elf-file" => c.File,
                "--elf-archive" => c.Archive,
                "--core" => c.Core,
                "--program" or "--executable" => c.Program,
                "--shared" or "--library" => c.Shared,
                "--linux-kernel-module" => c.KernelModule,
                "--debug-only" => c.DebugOnly,
                "--loadable" => c.Loadable,
                "--unstripped" => c.Unstripped,
                "--file" => isRegular,
                _ => false,
            });
            if (!ok) status = Math.Max(status, 1);
            if (ok == matching)
            {
                if (print) Console.Write(file + "\n");
                else if (print0) Console.Write(file + "\0");
            }
        }
        return status;
    }

    // ---- eu-elfcmp ----
    static int Cmp(string[] args)
    {
        bool quiet = false, verbose = false, ignoreBuildId = false;
        var files = new List<string>();
        foreach (var a in args)
        {
            if (Common("elfcmp", a, """
                Usage: eu-elfcmp [OPTION...] FILE1 FILE2
                Compare relevant parts of two ELF files for equality.

                      --gaps=ACTION          Control treatment of gaps in loadable segments [ignore|match] (default: ignore)
                      --hash-inexact         Ignore permutation of buckets in SHT_HASH section
                      --ignore-build-id      Ignore differences in build ID
                  -l, --verbose              Output all differences, not just the first
                  -q, --quiet                Output nothing; yield exit status only

                """)) return 0;
            switch (a)
            {
                case "-q" or "--quiet": quiet = true; break;
                case "-l" or "--verbose": verbose = true; break;
                case "--ignore-build-id": ignoreBuildId = true; break;
                case "--hash-inexact": break;
                default:
                    if (a.StartsWith("--gaps=")) { if (a[7..] is not ("ignore" or "match")) throw new UsageException($"invalid value '{a[7..]}' for --gaps parameter"); break; }
                    if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    files.Add(a);
                    break;
            }
        }
        if (files.Count != 2) throw new UsageException("Invalid number of parameters.");
        var a1 = Load("elfcmp", files[0]);
        var a2 = Load("elfcmp", files[1]);
        if (a1 == null || a2 == null) return 2;
        var different = false;
        bool Differ(string what)
        {
            different = true;
            if (!quiet) Console.Error.WriteLine($"eu-elfcmp: {files[0]} {files[1]} differ: {what}");
            return !verbose;
        }
        if (a1.Is64 != a2.Is64 || a1.Little != a2.Little || a1.OsAbi != a2.OsAbi || a1.Type != a2.Type || a1.Machine != a2.Machine
            || a1.Version != a2.Version || a1.Entry != a2.Entry || a1.Flags != a2.Flags || a1.PhNum != a2.PhNum)
        {
            if (Differ("ELF header")) return 1;
        }
        static bool Important(ElfKit.Section s) => (s.Flags & ElfKit.SHF_ALLOC) != 0 || s.Type is ElfKit.SHT_SYMTAB;
        var s1 = a1.Sections.Where(Important).ToList();
        var s2 = a2.Sections.Where(Important).ToList();
        if (s1.Count != s2.Count && Differ("unequal amount of important sections")) return 1;
        for (var i = 0; i < Math.Min(s1.Count, s2.Count); i++)
        {
            var x = s1[i];
            var y = s2[i];
            if (x.Name != y.Name || x.Type != y.Type || x.Flags != y.Flags || x.Addr != y.Addr || x.Size != y.Size || x.EntSize != y.EntSize)
            {
                if (Differ($"section [{x.Index}] '{x.Name}' header")) return 1;
                continue;
            }
            if (x.Type == ElfKit.SHT_NOBITS) continue;
            var cx = a1.Contents(x);
            var cy = a2.Contents(y);
            if (ignoreBuildId && x.Type == ElfKit.SHT_NOTE && x.Name == ".note.gnu.build-id") continue;
            if (!cx.AsSpan().SequenceEqual(cy))
            {
                var what = x.Type is ElfKit.SHT_SYMTAB or ElfKit.SHT_DYNSYM ? $"symbol table [{x.Index}]"
                    : x.Name == ".note.gnu.build-id" ? "build ID content" : $"section [{x.Index}] '{x.Name}' content";
                if (Differ(what)) return 1;
            }
        }
        for (var i = 0; i < Math.Min(a1.Segments.Count, a2.Segments.Count); i++)
        {
            var p = a1.Segments[i];
            var q = a2.Segments[i];
            if (p.Type != q.Type || p.Flags != q.Flags || p.Offset != q.Offset || p.VAddr != q.VAddr || p.PAddr != q.PAddr
                || p.FileSize != q.FileSize || p.MemSize != q.MemSize || p.Align != q.Align)
                if (Differ($"program header {i}")) return 1;
        }
        return different ? 1 : 0;
    }

    // ---- eu-addr2line / eu-srcfiles: DWARF line tables ----
    sealed class DebugInfo
    {
        public List<(ElfDwarf.Unit Unit, ElfDwarf.LineTable Lines, string CompDir)> Units = [];
        public List<(ulong Low, ulong High, string Name, ElfDwarf.Unit Unit)> Functions = [];
    }

    static DebugInfo ReadDebugInfo(ElfKit.Elf elf, bool functions)
    {
        var info = new DebugInfo();
        var s = ElfDwarf.Sections.Load(elf);
        foreach (var (u, dies) in ElfDwarf.Units(s, false))
        {
            var root = u.Root;
            if (root == null) continue;
            var compDir = root.Get(ElfDwarf.DW_AT_comp_dir) is { } cd ? ElfDwarf.String(s, u, cd) : null;
            ElfDwarf.LineTable lines = null;
            if (root.Get(ElfDwarf.DW_AT_stmt_list) is { } stmt)
            {
                try { lines = ElfDwarf.ReadLineTable(s, stmt.U, u.AddressSize, true); }
                catch (ElfKit.ElfError) { }
            }
            info.Units.Add((u, lines, compDir));
            if (!functions) continue;
            foreach (var die in dies.Where(d => d.Tag is ElfDwarf.DW_TAG_subprogram))
            {
                var low = die.Get(ElfDwarf.DW_AT_low_pc);
                var high = die.Get(ElfDwarf.DW_AT_high_pc);
                if (low == null || high == null) continue;
                var lo = ElfDwarf.Address(s, u, low);
                var hi = ElfDwarf.IsAddrForm(high.Form) ? ElfDwarf.Address(s, u, high) : lo + high.U;
                var nameAttr = die.Get(ElfDwarf.DW_AT_name);
                var name = nameAttr != null ? ElfDwarf.String(s, u, nameAttr) : null;
                if (name == null && (die.Get(ElfDwarf.DW_AT_abstract_origin) ?? die.Get(ElfDwarf.DW_AT_specification)) is { } origin)
                {
                    var target = dies.FirstOrDefault(d => d.Offset == origin.U);
                    if (target?.Get(ElfDwarf.DW_AT_name) is { } n) name = ElfDwarf.String(s, u, n);
                }
                info.Functions.Add((lo, hi, name, u));
            }
        }
        return info;
    }

    static string FullPath(ElfDwarf.LineTable t, ulong file, string compDir)
    {
        var path = t.FilePath(file, true);
        if (path == null) return null;
        var absolute = path.StartsWith('/') || (path.Length > 1 && path[1] == ':');
        return absolute || string.IsNullOrEmpty(compDir) ? path : compDir.TrimEnd('/') + "/" + path;
    }

    static int Addr2line(string[] args)
    {
        string file = "a.out";
        bool functions = false, basenames = false, showAddresses = false, pretty = false, absolute = false;
        var addresses = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (Common("addr2line", a, """
                Usage: eu-addr2line [OPTION...] [ADDR...]
                Locate source files and line information for ADDRs (in a.out by default).

                  -e, --executable=FILE      Find addresses in FILE
                  -a, --addresses            Print address before each entry
                  -A, --absolute             Show absolute file names using compilation directory
                  -f, --functions            Also show function names
                  -i, --inlines              Show all source locations that caused inline expansion (outermost only here)
                  -p, --pretty-print         Show all information on one line
                  -s, --basenames            Show only base names of source files
                  -C, --demangle             Accepted for compatibility

                If no ADDR is given, addresses are read from standard input.

                """)) return 0;
            switch (a)
            {
                case "-e":
                    if (++i >= args.Length) throw new UsageException("option requires an argument -- 'e'");
                    file = args[i];
                    break;
                case "-f" or "--functions": functions = true; break;
                case "-s" or "--basenames": basenames = true; break;
                case "-a" or "--addresses": showAddresses = true; break;
                case "-p" or "--pretty-print": pretty = true; break;
                case "-A" or "--absolute": absolute = true; break;
                case "-i" or "--inlines" or "-C" or "--demangle" or "-S" or "--symbols" or "--symbols-sections" or "-x": break;
                default:
                    if (a.StartsWith("--executable=")) file = a[13..];
                    else if (a.StartsWith("-e") && a.Length > 2) file = a[2..];
                    else if (a.StartsWith('-') && a.Length > 1 && !char.IsDigit(a[1])) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    else addresses.Add(a);
                    break;
            }
        }
        var elf = Load("addr2line", file);
        if (elf == null) return 1;
        var info = ReadDebugInfo(elf, true);
        var symbols = new ElfAddressNames(elf);
        var input = addresses.Count > 0 ? addresses : ReadLines();
        foreach (var text in input)
        {
            if (!TryParseAddress(text, out var address))
            {
                Console.Error.WriteLine($"eu-addr2line: cannot interpret '{text}' as an address");
                continue;
            }
            string location = null;
            foreach (var (u, lines, compDir) in info.Units)
            {
                if (lines == null) continue;
                var rows = lines.Rows;
                for (var k = 0; k + 1 < rows.Count; k++)
                {
                    if (rows[k].EndSequence) continue;
                    if (rows[k].Address <= address && address < rows[k + 1].Address)
                    {
                        var path = absolute || !basenames ? FullPath(lines, rows[k].File, absolute || true ? compDir : null) : lines.FilePath(rows[k].File, false);
                        if (basenames && path != null) path = path[(path.LastIndexOfAny(['/', '\\']) + 1)..];
                        location = $"{path ?? "??"}:{rows[k].Line}" + (rows[k].Column > 0 ? $":{rows[k].Column}" : "");
                        break;
                    }
                }
                if (location != null) break;
            }
            location ??= "??:0";
            string function = null;
            if (functions)
            {
                function = info.Functions.Where(f => f.Low <= address && address < f.High).Select(f => f.Name).FirstOrDefault();
                if (function == null)
                {
                    var named = symbols.Format(address, 8);
                    var lt = named.IndexOf('<');
                    function = lt < 0 ? "??" : named[(lt + 1)..^1];
                }
            }
            var addr = showAddresses ? $"0x{address.ToString("x").PadLeft(elf.Is64 ? 16 : 8, '0')}" : null;
            if (pretty)
                Console.WriteLine((addr != null ? addr + ": " : "") + (function != null ? $"{function} at {location}" : location));
            else
            {
                if (addr != null) Console.WriteLine(addr);
                if (function != null) Console.WriteLine(function);
                Console.WriteLine(location);
            }
        }
        return 0;
    }

    static IEnumerable<string> ReadLines()
    {
        string line;
        while ((line = Console.In.ReadLine()) != null)
            foreach (var part in line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)) yield return part;
    }

    static bool TryParseAddress(string text, out ulong value)
    {
        var t = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text;
        return ulong.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out value);
    }

    static int Srcfiles(string[] args)
    {
        bool nul = false, cuOnly = false;
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (Common("srcfiles", a, """
                Usage: eu-srcfiles [OPTION...] [FILE...]
                Lists the source files of DWARF/ELF FILEs (a.out by default).

                  -0, --null                 Separate items by a null instead of a newline
                  -c, --cu-only              Only list the CU names
                  -e, --executable=FILE      Find addresses in FILE
                  -v, --verbose              Increase verbosity of logging messages

                """)) return 0;
            switch (a)
            {
                case "-0" or "--null": nul = true; break;
                case "-c" or "--cu-only": cuOnly = true; break;
                case "-v" or "--verbose": break;
                case "-e":
                    if (++i >= args.Length) throw new UsageException("option requires an argument -- 'e'");
                    files.Add(args[i]);
                    break;
                default:
                    if (a.StartsWith("--executable=")) files.Add(a[13..]);
                    else if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    else files.Add(a);
                    break;
            }
        }
        if (files.Count == 0) files.Add("a.out");
        var set = new SortedSet<string>(StringComparer.Ordinal);
        var status = 0;
        foreach (var file in files)
        {
            var elf = Load("srcfiles", file);
            if (elf == null) { status = 1; continue; }
            var s = ElfDwarf.Sections.Load(elf);
            foreach (var (u, lines, compDir) in ReadDebugInfo(elf, false).Units)
            {
                if (cuOnly || lines == null)
                {
                    if (u.Root?.Get(ElfDwarf.DW_AT_name) is { } n && ElfDwarf.String(s, u, n) is { } name)
                    {
                        var abs = name.StartsWith('/') || (name.Length > 1 && name[1] == ':');
                        set.Add(abs || compDir == null ? name : compDir.TrimEnd('/') + "/" + name);
                    }
                    continue;
                }
                var count = (ulong)lines.Files.Count;
                for (ulong k = 0; k < count; k++)
                {
                    var p = FullPath(lines, lines.Version >= 5 ? k : k + 1, compDir);
                    if (p != null && !p.EndsWith("<built-in>")) set.Add(p);
                }
            }
        }
        foreach (var p in set) Console.Write(p + (nul ? "\0" : "\n"));
        return status;
    }

    // ---- eu-strip ----
    static int Strip(string[] args)
    {
        string output = null, debugFile = null;
        bool debugOnly = false, keepDebug = false, removeComment = false, preserveDates = false;
        var remove = new List<string>();
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (Common("strip", a, """
                Usage: eu-strip [OPTION...] FILE...
                Discard symbols from object files.

                  -o FILE                    Place stripped output into FILE
                  -f FILE                    Place stripped debug information into FILE (and add .gnu_debuglink)
                  -g, -d, -S, --strip-debug  Remove all debugging symbols
                      --only-keep-debug      Similar to --strip-all, but only keep debug sections
                  -R, --remove-section=SECTION
                                             Remove the named section
                      --remove-comment       Remove .comment section
                  -p, --preserve-dates       Copy modified/access timestamps to the output

                """)) return 0;
            switch (a)
            {
                case "-o" or "-f" or "-F" or "-R":
                    if (++i >= args.Length) throw new UsageException($"option requires an argument -- '{a[1]}'");
                    if (a == "-o") output = args[i];
                    else if (a == "-R") remove.Add(args[i]);
                    else debugFile = args[i];
                    break;
                case "-g" or "-d" or "-S" or "--strip-debug": debugOnly = true; break;
                case "--only-keep-debug": keepDebug = true; break;
                case "--remove-comment": removeComment = true; break;
                case "-p" or "--preserve-dates": preserveDates = true; break;
                case "--strip-sections" or "--permissive" or "--reloc-debug-sections" or "--reloc-debug-sections-only": break;
                default:
                    if (a.StartsWith("--remove-section=")) remove.Add(a[17..]);
                    else if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    else files.Add(a);
                    break;
            }
        }
        if (files.Count == 0) files.Add("a.out");
        if (output != null && files.Count > 1) throw new UsageException("Only one input file allowed together with '-o' and '-f'");
        var status = 0;
        foreach (var file in files)
        {
            var elf = Load("strip", file);
            if (elf == null) { status = 1; continue; }
            try
            {
                var times = (File.GetLastAccessTimeUtc(file), File.GetLastWriteTimeUtc(file));
                var target = output ?? file;
                if (debugFile != null)
                {
                    File.WriteAllBytes(debugFile, StripElf(elf, false, true, false, [], null));
                }
                var stripped = StripElf(elf, debugOnly, keepDebug, removeComment, remove, debugFile);
                File.WriteAllBytes(target, stripped);
                if (preserveDates) { File.SetLastAccessTimeUtc(target, times.Item1); File.SetLastWriteTimeUtc(target, times.Item2); }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ElfKit.ElfError)
            {
                Console.Error.WriteLine($"eu-strip: {file}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    static bool IsDebugSection(ElfKit.Section s) => s.Name.StartsWith(".debug") || s.Name.StartsWith(".zdebug") || s.Name.StartsWith(".gnu.debuglto")
        || s.Name == ".stab" || s.Name == ".stabstr" || s.Name == ".gdb_index";

    // Rebuilds the file without the dropped sections: everything that segments load stays byte for byte,
    // kept non-allocated sections are re-laid after it, then a fresh .shstrtab and section header table.
    static byte[] StripElf(ElfKit.Elf elf, bool debugOnly, bool keepDebug, bool removeComment, List<string> remove, string debugLink)
    {
        var relocatable = elf.Type == 1;
        var drop = new HashSet<int>();
        foreach (var s in elf.Sections.Skip(1))
        {
            if (remove.Contains(s.Name)) drop.Add(s.Index);
            else if (removeComment && s.Name == ".comment") drop.Add(s.Index);
            else if (keepDebug) { if ((s.Flags & ElfKit.SHF_ALLOC) != 0 && s.Type != ElfKit.SHT_NOTE) drop.Add(s.Index); }
            else if (IsDebugSection(s)) drop.Add(s.Index);
            else if (!debugOnly && !relocatable && (s.Type == ElfKit.SHT_SYMTAB || s.Name == ".strtab")) drop.Add(s.Index);
        }
        // In .o files, relocation sections for dropped sections go too.
        foreach (var s in elf.Sections)
            if (s.Type is ElfKit.SHT_RELA or ElfKit.SHT_REL && drop.Contains((int)s.Info)) drop.Add(s.Index);
        var shstrndx = elf.ShStrNdx;
        drop.Remove(shstrndx);
        if (keepDebug)
        {
            // Keep allocated sections as NOBITS so addresses still line up (what --only-keep-debug does).
            drop.Clear();
        }
        var kept = elf.Sections.Where(s => !drop.Contains(s.Index)).ToList();
        var newIndex = new Dictionary<int, int>();
        for (var i = 0; i < kept.Count; i++) newIndex[kept[i].Index] = i;

        // Bytes covered by segments (and the headers) are kept verbatim.
        ulong loadedEnd = (ulong)(elf.Is64 ? 64 : 52);
        foreach (var p in elf.Segments) loadedEnd = Math.Max(loadedEnd, p.Offset + p.FileSize);
        loadedEnd = Math.Max(loadedEnd, elf.PhOff + (ulong)(elf.PhNum * elf.PhEntSize));
        foreach (var s in kept.Where(s => (s.Flags & ElfKit.SHF_ALLOC) != 0 && s.Type != ElfKit.SHT_NOBITS && !keepDebug))
            loadedEnd = Math.Max(loadedEnd, s.Offset + s.Size);
        if (relocatable) loadedEnd = (ulong)elf.EhSize;
        var output = new MemoryStream();
        output.Write(elf.Data, 0, (int)Math.Min(loadedEnd, (ulong)elf.Data.Length));

        var names = new MemoryStream();
        names.WriteByte(0);
        var nameOffsets = new Dictionary<string, uint>();
        uint NameOffset(string n)
        {
            if (n.Length == 0) return 0;
            if (nameOffsets.TryGetValue(n, out var off)) return off;
            off = (uint)names.Length;
            var b = Encoding.UTF8.GetBytes(n);
            names.Write(b);
            names.WriteByte(0);
            nameOffsets[n] = off;
            return off;
        }

        var headers = new List<(ElfKit.Section S, ulong Offset, ulong Size, uint Type, string Name)>();
        foreach (var s in kept)
        {
            var type = s.Type;
            ulong offset = s.Offset, size = s.Size;
            var inLoaded = !relocatable && (s.Flags & ElfKit.SHF_ALLOC) != 0;
            if (keepDebug && (s.Flags & ElfKit.SHF_ALLOC) != 0 && s.Type != ElfKit.SHT_NOTE) { type = ElfKit.SHT_NOBITS; inLoaded = true; offset = Math.Min(s.Offset, (ulong)output.Length); }
            if (s.Index == 0 || s.Index == shstrndx) { headers.Add((s, 0, 0, type, s.Name)); continue; }
            if (!inLoaded && type != ElfKit.SHT_NOBITS)
            {
                var align = Math.Max(1UL, s.Align);
                Pad(output, align);
                offset = (ulong)output.Length;
                output.Write(elf.Data, (int)s.Offset, (int)s.Size);
            }
            headers.Add((s, offset, size, type, s.Name));
        }
        ElfKit.Section linkSection = null;
        ulong linkOffset = 0, linkSize = 0;
        if (debugLink != null)
        {
            var nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(debugLink));
            var crc = Crc32(File.ReadAllBytes(debugLink));
            Pad(output, 4);
            linkOffset = (ulong)output.Length;
            output.Write(nameBytes);
            output.WriteByte(0);
            Pad(output, 4);
            var crcBytes = new byte[4];
            if (elf.Little) BinaryPrimitives.WriteUInt32LittleEndian(crcBytes, crc);
            else BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
            output.Write(crcBytes);
            linkSize = (ulong)output.Length - linkOffset;
            linkSection = new ElfKit.Section { Name = ".gnu_debuglink", Type = ElfKit.SHT_PROGBITS, Align = 4 };
        }
        foreach (var h in headers) NameOffset(h.Name);
        if (linkSection != null) NameOffset(linkSection.Name);
        var shstrOffset = (ulong)output.Length;
        var shstrBytes = names.ToArray();
        output.Write(shstrBytes);
        Pad(output, (ulong)elf.WordSize);
        var shoff = (ulong)output.Length;
        var count = headers.Count + (linkSection != null ? 1 : 0);
        var entsize = elf.Is64 ? 64 : 40;
        var table = new byte[count * entsize];
        void WriteHeader(int i, uint name, uint type, ulong flags, ulong addr, ulong offset, ulong size, uint link, uint info, ulong align, ulong entSize)
        {
            var at = i * entsize;
            void W32(int o, uint v) { if (elf.Little) BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan(at + o), v); else BinaryPrimitives.WriteUInt32BigEndian(table.AsSpan(at + o), v); }
            void W64(int o, ulong v) { if (elf.Little) BinaryPrimitives.WriteUInt64LittleEndian(table.AsSpan(at + o), v); else BinaryPrimitives.WriteUInt64BigEndian(table.AsSpan(at + o), v); }
            W32(0, name); W32(4, type);
            if (elf.Is64)
            {
                W64(8, flags); W64(16, addr); W64(24, offset); W64(32, size);
                W32(40, link); W32(44, info); W64(48, align); W64(56, entSize);
            }
            else
            {
                W32(8, (uint)flags); W32(12, (uint)addr); W32(16, (uint)offset); W32(20, (uint)size);
                W32(24, link); W32(28, info); W32(32, (uint)align); W32(36, (uint)entSize);
            }
        }
        for (var i = 0; i < headers.Count; i++)
        {
            var (s, offset, size, type, name) = headers[i];
            if (s.Index == 0) { WriteHeader(0, 0, 0, 0, 0, 0, s.Size, s.Link, 0, 0, 0); continue; }
            var link = s.Link != 0 && newIndex.TryGetValue((int)s.Link, out var l) ? (uint)l : 0;
            var info = s.Info;
            if ((s.Type is ElfKit.SHT_RELA or ElfKit.SHT_REL || (s.Flags & 0x40) != 0) && s.Info != 0)
                info = newIndex.TryGetValue((int)s.Info, out var ni) ? (uint)ni : 0;
            if (s.Index == shstrndx) { offset = shstrOffset; size = (ulong)shstrBytes.Length; }
            WriteHeader(i, NameOffset(name), type, s.Flags, s.Addr, offset, size, link, info, s.Align, s.EntSize);
        }
        if (linkSection != null)
            WriteHeader(headers.Count, NameOffset(linkSection.Name), ElfKit.SHT_PROGBITS, 0, 0, linkOffset, linkSize, 0, 0, 4, 0);
        output.Write(table);
        var result = output.ToArray();
        // ELF header: new section table offset/count/string index.
        void H16(int o, ushort v) { if (elf.Little) BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(o), v); else BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(o), v); }
        void H32(int o, uint v) { if (elf.Little) BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(o), v); else BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(o), v); }
        void H64(int o, ulong v) { if (elf.Little) BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(o), v); else BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(o), v); }
        if (elf.Is64) { H64(40, shoff); H16(60, (ushort)count); H16(62, (ushort)newIndex[shstrndx]); }
        else { H32(32, (uint)shoff); H16(48, (ushort)count); H16(50, (ushort)newIndex[shstrndx]); }
        return result;
    }

    // CRC-32 (IEEE) as .gnu_debuglink records it.
    static uint Crc32(byte[] data)
    {
        var crc = 0xffffffffu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320u : crc >> 1;
        }
        return ~crc;
    }

    static void Pad(MemoryStream s, ulong align)
    {
        while ((ulong)s.Length % align != 0) s.WriteByte(0);
    }
}
