using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// bpftop: a live view of the eBPF programs loaded in the system. On Windows eBPF is Microsoft's
// eBPF for Windows; its user-mode library (EbpfApi.dll, libbpf's API) enumerates the programs
// (bpf_prog_get_next_id, bpf_obj_get_info_by_fd, ebpf_program_query_info) the way bpftop does
// through the Linux bpf() syscall. eBPF for Windows keeps no per-program run counts or run time
// (Linux's BPF_ENABLE_STATS), so instead of runtime/events/CPU columns the table shows what Windows
// knows: program type, execution mode (JIT, interpreted, native driver), attached links, maps and
// the object file and section it came from. Refreshes every --delay seconds; q quits.
static unsafe class Bpftop
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("bpftop", "Live view of running eBPF programs (eBPF for Windows)", Run);

    const string Version = "0.9.0";

    sealed class Program
    {
        public uint Id, MapCount, LinkCount, PinCount;
        public string Name, Type, Execution, File, Section;
    }

    static nint library;
    static delegate* unmanaged<uint, uint*, int> getNextId;
    static delegate* unmanaged<uint, int> getFdById;
    static delegate* unmanaged<int, void*, uint*, int> getInfoByFd;
    static delegate* unmanaged<int, int> closeFd;
    static delegate* unmanaged<Guid*, nint> typeName;
    static delegate* unmanaged<int, int*, nint*, nint*, int> queryInfo;
    static delegate* unmanaged<nint, void> freeString;

    static int Run(string[] args)
    {
        var delay = 1.0;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        bpftop provides a dynamic real-time view of running eBPF programs

                        Usage: bpftop [OPTIONS]

                        Options:
                          -d, --delay <SECONDS>  Refresh interval [default: 1]
                          -h, --help             Print help
                          -V, --version          Print version

                        On Windows the programs are eBPF for Windows' (its runtime must be installed).
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine($"bpftop {Version}"); return 0;
                case "-d" or "--delay":
                    if (++i >= args.Length || !double.TryParse(args[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out delay) || delay <= 0)
                        throw new UsageException("invalid value for '--delay <SECONDS>'");
                    break;
                default: throw new UsageException($"unexpected argument '{args[i]}' found");
            }
        }
        if (!IsUserAnAdmin())
        {
            Console.Error.WriteLine("Error: This program must be run as root (an elevated Administrator prompt on Windows)");
            return 1;
        }
        if (!Load())
        {
            Console.Error.WriteLine("Error: eBPF for Windows is not installed (EbpfApi.dll not found); install its runtime from https://github.com/microsoft/ebpf-for-windows/releases");
            return 1;
        }
        if (Console.IsOutputRedirected)
        {
            Print(Console.Out, Programs(), width: 200, selected: -1);
            return 0;
        }
        return Interactive(delay);
    }

    static bool Load()
    {
        var candidates = new List<string> { "EbpfApi.dll" };
        foreach (var root in new[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramW6432") })
            if (!string.IsNullOrEmpty(root)) candidates.Add(Path.Combine(root, "ebpf-for-windows", "EbpfApi.dll"));
        foreach (var c in candidates)
            if (NativeLibrary.TryLoad(c, out library)) break;
        if (library == 0) return false;
        nint F(string name) => NativeLibrary.TryGetExport(library, name, out var f) ? f : 0;
        getNextId = (delegate* unmanaged<uint, uint*, int>)F("bpf_prog_get_next_id");
        getFdById = (delegate* unmanaged<uint, int>)F("bpf_prog_get_fd_by_id");
        getInfoByFd = (delegate* unmanaged<int, void*, uint*, int>)F("bpf_obj_get_info_by_fd");
        closeFd = (delegate* unmanaged<int, int>)F("ebpf_close_fd");
        typeName = (delegate* unmanaged<Guid*, nint>)F("ebpf_get_program_type_name");
        queryInfo = (delegate* unmanaged<int, int*, nint*, nint*, int>)F("ebpf_program_query_info");
        freeString = (delegate* unmanaged<nint, void>)F("ebpf_free_string");
        return getNextId != null && getFdById != null && getInfoByFd != null;
    }

    // eBPF for Windows' struct bpf_prog_info (ebpf_structs.h).
    [StructLayout(LayoutKind.Sequential)]
    struct ProgInfo
    {
        public uint id;
        public int type;
        public uint nr_map_ids;
        public uint padding;
        public ulong map_ids;
        public fixed byte name[64];
        public Guid type_uuid;
        public Guid attach_type_uuid;
        public uint pinned_path_count;
        public uint link_count;
    }

    static List<Program> Programs()
    {
        var list = new List<Program>();
        uint id = 0, next;
        while (getNextId(id, &next) == 0)
        {
            id = next;
            var fd = getFdById(id);
            if (fd < 0) continue;
            var info = new ProgInfo();
            var length = (uint)sizeof(ProgInfo);
            if (getInfoByFd(fd, &info, &length) == 0)
            {
                var p = new Program
                {
                    Id = info.id,
                    Name = Encoding.UTF8.GetString(info.name, 64).TrimEnd('\0'),
                    MapCount = info.nr_map_ids,
                    LinkCount = info.link_count,
                    PinCount = info.pinned_path_count,
                    Type = typeName != null && typeName(&info.type_uuid) is var t and not 0 ? Marshal.PtrToStringUTF8(t) : $"type {info.type}",
                };
                if (queryInfo != null)
                {
                    int execution;
                    nint file, section;
                    if (queryInfo(fd, &execution, &file, &section) == 0)
                    {
                        p.Execution = execution switch { 1 => "JIT", 2 => "Interpret", 3 => "Native", _ => "Any" };
                        p.File = file != 0 ? Marshal.PtrToStringUTF8(file) : "";
                        p.Section = section != 0 ? Marshal.PtrToStringUTF8(section) : "";
                        if (freeString != null) { if (file != 0) freeString(file); if (section != 0) freeString(section); }
                    }
                }
                list.Add(p);
            }
            if (closeFd != null) closeFd(fd);
        }
        return list;
    }

    static void Print(TextWriter w, List<Program> programs, int width, int selected)
    {
        var header = $"{"Program",-7} {"Type",-22} {"Name",-24} {"Execution",-10} {"Links",5} {"Maps",5} {"Pins",5}  Object";
        w.WriteLine(header.Length > width ? header[..width] : header);
        for (var k = 0; k < programs.Count; k++)
        {
            var p = programs[k];
            var obj = p.File is { Length: > 0 } ? $"{Path.GetFileName(p.File)}:{p.Section}" : "";
            var line = $"{p.Id,-7} {Clip(p.Type, 22),-22} {Clip(p.Name, 24),-24} {p.Execution ?? "-",-10} {p.LinkCount,5} {p.MapCount,5} {p.PinCount,5}  {obj}";
            if (line.Length > width) line = line[..width];
            w.WriteLine(k == selected ? "\x1b[7m" + line.PadRight(width) + "\x1b[0m" : line);
        }
        if (programs.Count == 0) w.WriteLine("(no eBPF programs loaded)");
    }

    static string Clip(string s, int n) => s.Length > n ? s[..(n - 1)] + "…" : s;

    static int Interactive(double delay)
    {
        var selected = 0;
        Console.Write("\x1b[?1049h\x1b[?25l");
        Console.CancelKeyPress += (_, e) => { Console.Write("\x1b[?25h\x1b[?1049l"); };
        try
        {
            for (;;)
            {
                var programs = Programs();
                selected = Math.Clamp(selected, 0, Math.Max(0, programs.Count - 1));
                var screen = new StringWriter();
                screen.Write("\x1b[H\x1b[2J");
                screen.WriteLine($"bpftop {Version} - eBPF for Windows - {programs.Count} program(s) - {DateTime.Now:HH:mm:ss}");
                screen.WriteLine("(eBPF for Windows keeps no per-program run time or event counts)");
                screen.WriteLine();
                Print(screen, programs, Math.Max(40, Console.WindowWidth - 1), selected);
                if (programs.Count > 0)
                {
                    var p = programs[selected];
                    screen.WriteLine();
                    screen.WriteLine($"Program {p.Id}: {p.Name}  type {p.Type}  {p.Execution}  file {p.File}  section {p.Section}");
                }
                screen.WriteLine();
                screen.Write("q: quit   up/down: select");
                Console.Write(screen.ToString());
                var until = DateTime.UtcNow.AddSeconds(delay);
                while (DateTime.UtcNow < until)
                {
                    if (!Console.KeyAvailable) { Thread.Sleep(50); continue; }
                    var key = Console.ReadKey(true);
                    if (key.Key is ConsoleKey.Q or ConsoleKey.Escape) return 0;
                    if (key.Key == ConsoleKey.UpArrow) { selected--; break; }
                    if (key.Key == ConsoleKey.DownArrow) { selected++; break; }
                }
            }
        }
        finally { Console.Write("\x1b[?25h\x1b[?1049l"); }
    }

    [DllImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsUserAnAdmin();
}
