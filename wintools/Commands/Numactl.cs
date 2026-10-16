using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// numactl / numastat / memhog on Windows' NUMA APIs. Nodes, their processors and free memory come from
// GetNumaNodeProcessorMaskEx / GetNumaAvailableMemoryNodeEx; node sizes and distances from the ACPI SRAT
// and SLIT tables (GetSystemFirmwareTable), as Linux reads them. Running a program under a policy sets
// its processor-group affinity (--cpunodebind, --physcpubind) and its preferred memory node
// (--membind, --preferred: PROC_THREAD_ATTRIBUTE_PREFERRED_NODE). Windows' preferred node is a strong
// preference, not a hard bind, and it has no per-process interleave policy, so --interleave is refused.
// numastat shows per-node system memory, or a process's resident pages per node (QueryWorkingSetEx).
static unsafe class Numactl
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("numactl", "Control NUMA policy for processes (Windows NUMA nodes and processor groups)", RunNumactl);
        Commands.Add("numastat", "Show per-NUMA-node memory statistics for the system or processes", RunNumastat);
        Commands.Add("memhog", "Allocate and touch memory on a NUMA node", RunMemhog);
    }

    const string NumactlVersion = "2.0.19";

    sealed class Node
    {
        public int Id;
        public List<int> Cpus = [];
        public ulong Free, Size;
        public (ushort Group, ulong Mask) Affinity;
    }

    static List<Node> Nodes()
    {
        GetNumaHighestNodeNumber(out var highest);
        var nodes = new List<Node>();
        for (ushort n = 0; n <= highest; n++)
        {
            if (!GetNumaNodeProcessorMaskEx(n, out var affinity)) continue;
            var node = new Node { Id = n, Affinity = (affinity.Group, (ulong)affinity.Mask) };
            for (var bit = 0; bit < 64; bit++)
                if (((ulong)affinity.Mask & (1UL << bit)) != 0) node.Cpus.Add(affinity.Group * 64 + bit);
            if (GetNumaAvailableMemoryNodeEx(n, out var free)) node.Free = free;
            nodes.Add(node);
        }
        var sizes = SratSizes();
        foreach (var node in nodes) node.Size = sizes.GetValueOrDefault(node.Id);
        if (nodes.Count == 1 && nodes[0].Size == 0)
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)sizeof(MEMORYSTATUSEX) };
            if (GlobalMemoryStatusEx(ref status)) nodes[0].Size = status.ullTotalPhys;
        }
        return nodes;
    }

    static byte[] FirmwareTable(string signature)
    {
        var id = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(signature));
        var size = GetSystemFirmwareTable(ACPI, id, null, 0);
        if (size == 0) return null;
        var buffer = new byte[size];
        return GetSystemFirmwareTable(ACPI, id, buffer, size) == size ? buffer : null;
    }

    // SRAT memory affinity structures: bytes of memory per node.
    static Dictionary<int, ulong> SratSizes()
    {
        var sizes = new Dictionary<int, ulong>();
        var srat = FirmwareTable("SRAT");
        if (srat == null || srat.Length < 48) return sizes;
        for (var o = 48; o + 2 <= srat.Length;)
        {
            int type = srat[o], length = srat[o + 1];
            if (length == 0 || o + length > srat.Length) break;
            if (type == 1 && length >= 40 && (BitConverter.ToUInt32(srat, o + 28) & 1) != 0)
            {
                var domain = BitConverter.ToUInt32(srat, o + 2);
                var bytes = (ulong)BitConverter.ToUInt32(srat, o + 16) | (ulong)BitConverter.ToUInt32(srat, o + 20) << 32;
                if (GetNumaProximityNodeEx(domain, out var node)) sizes[node] = sizes.GetValueOrDefault(node) + bytes;
            }
            o += length;
        }
        return sizes;
    }

    // SLIT distance matrix, by node; 10 on the diagonal and 20 elsewhere when the firmware has none.
    static int[,] Distances(List<Node> nodes)
    {
        var max = nodes.Max(n => n.Id) + 1;
        var d = new int[max, max];
        for (var a = 0; a < max; a++)
            for (var b = 0; b < max; b++) d[a, b] = a == b ? 10 : 20;
        var slit = FirmwareTable("SLIT");
        if (slit == null || slit.Length < 44) return d;
        var count = (int)BitConverter.ToUInt64(slit, 36);
        if (44 + count * count > slit.Length) return d;
        for (var a = 0; a < count; a++)
            for (var b = 0; b < count; b++)
                if (GetNumaProximityNodeEx((uint)a, out var na) && GetNumaProximityNodeEx((uint)b, out var nb) && na < max && nb < max)
                    d[na, nb] = slit[44 + a * count + b];
        return d;
    }

    static string Ranges(IEnumerable<int> values)
    {
        var list = values.Order().ToList();
        var parts = new List<string>();
        for (var i = 0; i < list.Count;)
        {
            var j = i;
            while (j + 1 < list.Count && list[j + 1] == list[j] + 1) j++;
            parts.Add(i == j ? $"{list[i]}" : $"{list[i]}-{list[j]}");
            i = j + 1;
        }
        return string.Join(",", parts);
    }

    // "0-3,8", "all", "+1" (relative to the allowed set is not tracked; + is ignored), "!0" (all but).
    static HashSet<int> ParseList(string text, IEnumerable<int> all, string what)
    {
        var universe = all.ToHashSet();
        if (text == "all") return universe;
        var invert = text.StartsWith('!');
        if (invert) text = text[1..];
        text = text.TrimStart('+');
        var set = new HashSet<int>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var dash = part.IndexOf('-');
            if (dash > 0 && int.TryParse(part[..dash], out var lo) && int.TryParse(part[(dash + 1)..], out var hi) && lo <= hi)
                for (var x = lo; x <= hi; x++) set.Add(x);
            else if (int.TryParse(part, out var one)) set.Add(one);
            else throw new UsageException($"invalid {what} specification {text}");
        }
        foreach (var x in set)
            if (!universe.Contains(x)) throw new UsageException($"{what} argument out of range: {x}");
        return invert ? universe.Except(set).ToHashSet() : set;
    }

    // ---- numactl ----

    static int RunNumactl(string[] args)
    {
        if (args.Length == 0) { NumactlUsage(Console.Error); return 1; }
        HashSet<int> cpus = null;
        int? preferred = null;
        var nodes = Nodes();
        var i = 0;
        for (; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--") { i++; break; }
            if (!arg.StartsWith('-')) break;
            var eq = arg.IndexOf('=');
            var name = eq > 0 ? arg[..eq] : arg;
            string Value()
            {
                if (eq > 0) return arg[(eq + 1)..];
                if (name.Length > 2 && !name.StartsWith("--")) return name[2..];
                return ++i < args.Length ? args[i] : throw new UsageException($"option {arg} requires an argument");
            }
            var key = name.StartsWith("--") ? name : name[..2];
            switch (key)
            {
                case "-h" or "--help": NumactlUsage(Console.Out); return 0;
                case "-V" or "--version": Console.WriteLine($"numactl {NumactlVersion} (wintools {Commands.Version})"); return 0;
                case "-H" or "--hardware": return Hardware(nodes);
                case "-s" or "--show": return Show(nodes);
                case "-N" or "--cpunodebind" or "--cpubind" or "-c":
                {
                    var set = ParseList(Value(), nodes.Select(n => n.Id), "node");
                    cpus = nodes.Where(n => set.Contains(n.Id)).SelectMany(n => n.Cpus).ToHashSet();
                    break;
                }
                case "-C" or "--physcpubind":
                    cpus = ParseList(Value(), nodes.SelectMany(n => n.Cpus), "cpu");
                    break;
                case "-m" or "--membind":
                {
                    var set = ParseList(Value(), nodes.Select(n => n.Id), "node");
                    preferred = set.Min();
                    if (set.Count > 1) Console.Error.WriteLine($"numactl: Windows binds memory to one preferred node; using node {preferred}");
                    break;
                }
                case "-p" or "--preferred":
                    preferred = ParseList(Value(), nodes.Select(n => n.Id), "node").Min();
                    break;
                case "-P" or "--preferred-many":
                    preferred = ParseList(Value(), nodes.Select(n => n.Id), "node").Min();
                    break;
                case "-i" or "--interleave" or "-w" or "--weighted-interleave":
                    Value();
                    Console.Error.WriteLine("numactl: Windows has no per-process interleaved memory policy; use --preferred or --cpunodebind");
                    return 1;
                case "-l" or "--localalloc" or "--local" or "-a" or "--all" or "-b" or "--balancing" or "-t" or "--strict" or "-T" or "--touch":
                    break;   // local allocation is Windows' default policy
                case "-S" or "--shm" or "-f" or "--file" or "-L" or "--length" or "-o" or "--offset" or "-M" or "--shmmode" or "-I" or "--shmid" or "-u" or "--huge":
                    Console.Error.WriteLine($"numactl: {name}: shared-memory policies are not available on Windows");
                    return 1;
                default: throw new UsageException($"unrecognized option '{arg}'");
            }
        }
        if (i >= args.Length) throw new UsageException("no command given (see numactl --help)");
        return Launch(args[i], args[(i + 1)..], cpus, preferred);
    }

    static void NumactlUsage(TextWriter w) => w.WriteLine("""
        usage: numactl [--all | -a] [--balancing | -b] [--interleave= | -i <nodes>]
                       [--preferred= | -p <node>] [--physcpubind= | -C <cpus>]
                       [--cpunodebind= | -N <nodes>] [--membind= | -m <nodes>]
                       [--localalloc | -l] command args ...
               numactl [--show | -s]
               numactl [--hardware | -H]
               numactl [--version | -V]

        <nodes> is a comma delimited list of node numbers or A-B ranges or all.
        <cpus> is a comma delimited list of cpu numbers or A-B ranges or all.
        On Windows, --membind sets the preferred memory node (a strong preference, not a hard bind),
        CPU binding must stay inside one processor group, and --interleave is not available.
        """);

    static int Hardware(List<Node> nodes)
    {
        Console.WriteLine($"available: {nodes.Count} nodes ({Ranges(nodes.Select(n => n.Id))})");
        foreach (var n in nodes)
        {
            Console.WriteLine($"node {n.Id} cpus: {string.Join(' ', n.Cpus)}");
            Console.WriteLine($"node {n.Id} size: {n.Size >> 20} MB");
            Console.WriteLine($"node {n.Id} free: {n.Free >> 20} MB");
        }
        var d = Distances(nodes);
        Console.WriteLine("node distances:");
        Console.WriteLine("node " + string.Concat(nodes.Select(n => $"{n.Id,3} ")));
        foreach (var a in nodes)
            Console.WriteLine($"{a.Id,3}: " + string.Concat(nodes.Select(b => $"{d[a.Id, b.Id],3} ")));
        return 0;
    }

    static int Show(List<Node> nodes)
    {
        var self = Process();
        GetProcessAffinityMask(self, out var processMask, out _);
        var groups = new ushort[64];
        var count = (ushort)groups.Length;
        GetProcessGroupAffinity(self, ref count, groups);
        var group = count > 0 ? groups[0] : (ushort)0;
        var allowed = Enumerable.Range(0, 64).Where(b => ((ulong)processMask & (1UL << b)) != 0).Select(b => group * 64 + b).ToList();
        var bound = nodes.Where(n => n.Cpus.Intersect(allowed).Any()).Select(n => n.Id).ToList();
        Console.WriteLine("policy: default");
        Console.WriteLine("preferred node: current");
        Console.WriteLine($"physcpubind: {string.Join(' ', allowed)} ");
        Console.WriteLine($"cpubind: {string.Join(' ', bound)} ");
        Console.WriteLine($"nodebind: {string.Join(' ', bound)} ");
        Console.WriteLine($"membind: {string.Join(' ', nodes.Select(n => n.Id))} ");
        Console.WriteLine("preferred: ");
        return 0;
    }

    static IntPtr Process() => GetCurrentProcess();

    static int Launch(string program, string[] args, HashSet<int> cpus, int? preferred)
    {
        var path = Sandbox.FindProgram(program) ?? throw new InvalidOperationException($"execution of `{program}': No such file or directory");
        var commandLine = new StringBuilder(Sandbox.Quote(path));
        foreach (var a in args) commandLine.Append(' ').Append(Sandbox.Quote(a));
        string application = path;
        if (Path.GetExtension(path).ToLowerInvariant() is ".cmd" or ".bat")
        {
            application = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            commandLine.Insert(0, "cmd.exe /d /s /c \"").Append('"');
        }

        GROUP_AFFINITY affinity = default;
        if (cpus != null)
        {
            if (cpus.Count == 0) throw new UsageException("no CPUs selected");
            var groupsUsed = cpus.Select(c => c / 64).Distinct().ToList();
            if (groupsUsed.Count > 1) throw new InvalidOperationException("the selected CPUs span several processor groups; Windows binds a process to one group");
            affinity.Group = (ushort)groupsUsed[0];
            foreach (var c in cpus) affinity.Mask |= (nuint)(1UL << (c % 64));
        }
        ushort node = (ushort)(preferred ?? 0);

        // Attribute list: the preferred NUMA node. CPU binding is applied while the process is suspended.
        nuint size = 0;
        var attributes = preferred != null ? 1 : 0;
        InitializeProcThreadAttributeList(IntPtr.Zero, attributes, 0, ref size);
        var list = Marshal.AllocHGlobal((int)size);
        try
        {
            if (!InitializeProcThreadAttributeList(list, attributes, 0, ref size)) throw new System.ComponentModel.Win32Exception();
            if (preferred != null && !UpdateProcThreadAttribute(list, 0, PROC_THREAD_ATTRIBUTE_PREFERRED_NODE, &node, (nuint)sizeof(ushort), IntPtr.Zero, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception();
            var startup = new STARTUPINFOEXW();
            startup.StartupInfo.cb = sizeof(STARTUPINFOEXW);
            startup.lpAttributeList = list;
            var flags = CREATE_SUSPENDED | CREATE_UNICODE_ENVIRONMENT | (attributes > 0 ? EXTENDED_STARTUPINFO_PRESENT : 0);
            if (!CreateProcessW(application, commandLine, IntPtr.Zero, IntPtr.Zero, true, flags, IntPtr.Zero, null, ref startup, out var pi))
                throw new InvalidOperationException($"execution of `{program}': {new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message}");
            // Move the initial thread into the chosen group, then set the process mask that later
            // threads inherit, all before the program runs.
            if (cpus != null && (!SetThreadGroupAffinity(pi.hThread, ref affinity, IntPtr.Zero) || !SetProcessAffinityMask(pi.hProcess, affinity.Mask)))
            {
                var error = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
                TerminateProcess(pi.hProcess, 1);
                throw new InvalidOperationException($"cannot bind to the selected CPUs: {error}");
            }
            ResumeThread(pi.hThread);
            CloseHandle(pi.hThread);
            WaitForSingleObject(pi.hProcess, 0xFFFFFFFF);
            GetExitCodeProcess(pi.hProcess, out var code);
            CloseHandle(pi.hProcess);
            DeleteProcThreadAttributeList(list);
            return (int)code;
        }
        finally { Marshal.FreeHGlobal(list); }
    }

    // ---- numastat ----

    static int RunNumastat(string[] args)
    {
        var pids = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help" or "-?":
                    Console.WriteLine("""
                        Usage: numastat [-c] [-m] [-n] [-p <PID>|<pattern>] [-s<node>] [-v] [-V] [-z] [ <PID>|<pattern>... ]
                        -c to minimize column widths
                        -m to show meminfo-like system-wide memory usage (the default on Windows)
                        -n to show the numastat statistics info (same as -m on Windows)
                        -p <PID>|<pattern> to show process info: resident memory per node
                        -s[<node>] to sort data by total column or <node>
                        -v to make some reports more verbose
                        -V to show the numastat code version
                        -z to skip rows and columns of zeros
                        Windows keeps no NUMA hit/miss counters, so the system report is memory per node.
                        """);
                    return 0;
                case "-V": Console.WriteLine($"numastat version 1.1.0 (numactl {NumactlVersion}, wintools {Commands.Version})"); return 0;
                case "-p": pids.Add(++i < args.Length ? args[i] : throw new UsageException("option -p needs a PID or pattern")); break;
                case "-c" or "-m" or "-n" or "-v" or "-z": break;
                default:
                    if (args[i].StartsWith("-s")) break;
                    if (args[i].StartsWith('-')) throw new UsageException($"unrecognized option '{args[i]}'");
                    pids.Add(args[i]);
                    break;
            }
        }
        var nodes = Nodes();
        if (pids.Count == 0)
        {
            Console.WriteLine("\nPer-node system memory usage (in MBs):");
            Table(nodes, [("MemTotal", nodes.Select(n => n.Size).ToArray()), ("MemFree", nodes.Select(n => n.Free).ToArray()),
                ("MemUsed", nodes.Select(n => n.Size > n.Free ? n.Size - n.Free : 0).ToArray())]);
            return 0;
        }
        var status = 0;
        foreach (var spec in pids)
        {
            var matches = int.TryParse(spec, out var pid)
                ? [(pid, ProcessName(pid))]
                : System.Diagnostics.Process.GetProcesses().Where(p => p.ProcessName.Contains(spec, StringComparison.OrdinalIgnoreCase))
                    .Select(p => (p.Id, p.ProcessName)).ToList();
            if (matches.Count == 0) { Console.Error.WriteLine($"numastat: no process matches '{spec}'"); status = 1; continue; }
            foreach (var (id, name) in matches)
            {
                var perNode = ResidentPerNode(id, nodes);
                if (perNode == null) { Console.Error.WriteLine($"numastat: cannot read process {id}: access denied or exited"); status = 1; continue; }
                Console.WriteLine($"\nPer-node process memory usage (in MBs) for PID {id} ({name})");
                Table(nodes, [("Resident", perNode)]);
            }
        }
        return status;
    }

    static string ProcessName(int pid)
    {
        try { return System.Diagnostics.Process.GetProcessById(pid).ProcessName; }
        catch (ArgumentException) { return "?"; }
    }

    static void Table(List<Node> nodes, (string Label, ulong[] Bytes)[] rows)
    {
        var header = new StringBuilder($"{"",-17}");
        var rule = new StringBuilder($"{"",-17}");
        foreach (var n in nodes) { header.Append($"{"Node " + n.Id,15} "); rule.Append("--------------- "); }
        header.Append($"{"Total",15}");
        rule.Append("---------------");
        Console.WriteLine(header);
        Console.WriteLine(rule);
        foreach (var (label, bytes) in rows)
        {
            var line = new StringBuilder($"{label,-17}");
            foreach (var b in bytes) line.Append($"{b / 1048576.0,15:F2} ");
            line.Append($"{bytes.Aggregate(0UL, (s, b) => s + b) / 1048576.0,15:F2}");
            Console.WriteLine(line);
        }
    }

    // Resident pages of a process, by the node each physical page sits on.
    static ulong[] ResidentPerNode(int pid, List<Node> nodes)
    {
        var process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
        if (process == IntPtr.Zero) return null;
        try
        {
            var entries = 1 << 16;
            nuint[] ws;
            while (true)
            {
                ws = new nuint[entries + 1];
                fixed (nuint* p = ws)
                    if (QueryWorkingSet(process, p, (uint)(ws.Length * sizeof(nuint)))) break;
                if (Marshal.GetLastWin32Error() != 24 || entries > 1 << 26) return null;   // ERROR_BAD_LENGTH
                entries = (int)ws[0] + 4096;
            }
            var count = (int)ws[0];
            var info = new PSAPI_WORKING_SET_EX_INFORMATION[count];
            for (var i = 0; i < count; i++) info[i].VirtualAddress = (IntPtr)(long)(ws[i + 1] & ~(nuint)0xFFF);
            fixed (PSAPI_WORKING_SET_EX_INFORMATION* p = info)
                if (!QueryWorkingSetEx(process, p, (uint)(count * sizeof(PSAPI_WORKING_SET_EX_INFORMATION)))) return null;
            var max = nodes.Max(n => n.Id);
            var perNode = new ulong[nodes.Count];
            foreach (var entry in info)
            {
                var attributes = (ulong)entry.VirtualAttributes;
                if ((attributes & 1) == 0) continue;
                var node = (int)((attributes >> 16) & 0x3F);
                var index = nodes.FindIndex(n => n.Id == node);
                if (index >= 0) perNode[index] += 4096;
            }
            return perNode;
        }
        finally { CloseHandle(process); }
    }

    // ---- memhog ----

    static int RunMemhog(string[] args)
    {
        var repeat = 1;
        string sizeText = null;
        int? node = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "-h" or "--help")
            {
                Console.WriteLine("memhog [-rNUM] size[kmg] [--membind node | --preferred node]\n" +
                                  "Allocate size bytes (on the node, when one is given), touch every page NUM times, then free it.");
                return 0;
            }
            if (a.StartsWith("-r") && int.TryParse(a[2..], out var r)) repeat = r;
            else if (a.StartsWith("-f")) { }
            else if (a is "--membind" or "-m" or "--preferred" or "-p" or "--cpunodebind" or "-N")
                node = ++i < args.Length && int.TryParse(args[i], out var n) ? n : throw new UsageException($"{a} needs a node number");
            else if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
            else sizeText = a;
        }
        if (sizeText == null) throw new UsageException("memhog [-rNUM] size[kmg] [policy]");
        var bytes = ParseSize(sizeText);
        var region = node is int nn
            ? VirtualAllocExNuma(Process(), IntPtr.Zero, (nuint)bytes, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE, (uint)nn)
            : VirtualAlloc(IntPtr.Zero, (nuint)bytes, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (region == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        try
        {
            for (var pass = 0; pass < repeat; pass++)
            {
                var p = (byte*)region;
                for (long off = 0; off < bytes; off += 4096)
                {
                    p[off] = (byte)pass;
                    if (off % (1 << 20) == 0) Console.Write('.');
                }
                Console.WriteLine();
            }
        }
        finally { VirtualFree(region, 0, MEM_RELEASE); }
        return 0;
    }

    static long ParseSize(string text)
    {
        var mult = 1L;
        var last = char.ToLowerInvariant(text[^1]);
        if (last is 'k' or 'm' or 'g' or 't') { mult = last switch { 'k' => 1L << 10, 'm' => 1L << 20, 'g' => 1L << 30, _ => 1L << 40 }; text = text[..^1]; }
        return long.TryParse(text, out var n) && n > 0 ? n * mult : throw new UsageException($"invalid size {text}");
    }

    // ---- Win32 ----

    const uint ACPI = 0x41435049;
    const uint CREATE_SUSPENDED = 0x4, CREATE_UNICODE_ENVIRONMENT = 0x400, EXTENDED_STARTUPINFO_PRESENT = 0x80000;
    const uint PROCESS_QUERY_INFORMATION = 0x400, PROCESS_VM_READ = 0x10;
    const uint MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, MEM_RELEASE = 0x8000, PAGE_READWRITE = 0x4;
    static readonly IntPtr PROC_THREAD_ATTRIBUTE_PREFERRED_NODE = 0x00020004;

    [StructLayout(LayoutKind.Sequential)]
    struct GROUP_AFFINITY { public nuint Mask; public ushort Group; public ushort r1, r2, r3; }

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORYSTATUSEX
    {
        public uint dwLength, dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PSAPI_WORKING_SET_EX_INFORMATION { public IntPtr VirtualAddress; public nuint VirtualAttributes; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFOW
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFOEXW { public STARTUPINFOW StartupInfo; public IntPtr lpAttributeList; }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }

    [DllImport("kernel32.dll")] static extern bool GetNumaHighestNodeNumber(out uint highest);
    [DllImport("kernel32.dll")] static extern bool GetNumaNodeProcessorMaskEx(ushort node, out GROUP_AFFINITY affinity);
    [DllImport("kernel32.dll")] static extern bool GetNumaAvailableMemoryNodeEx(ushort node, out ulong bytes);
    [DllImport("kernel32.dll")] static extern bool GetNumaProximityNodeEx(uint proximityId, out ushort node);
    [DllImport("kernel32.dll")] static extern uint GetSystemFirmwareTable(uint provider, uint id, byte[] buffer, uint size);
    [DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX status);
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")] static extern bool GetProcessAffinityMask(IntPtr process, out nuint processMask, out nuint systemMask);
    [DllImport("kernel32.dll")] static extern bool GetProcessGroupAffinity(IntPtr process, ref ushort count, ushort[] groups);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetProcessAffinityMask(IntPtr process, nuint mask);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetThreadGroupAffinity(IntPtr thread, ref GROUP_AFFINITY affinity, IntPtr previous);
    [DllImport("kernel32.dll")] static extern bool TerminateProcess(IntPtr process, uint code);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", EntryPoint = "K32QueryWorkingSet", SetLastError = true)] static extern bool QueryWorkingSet(IntPtr process, nuint* buffer, uint size);
    [DllImport("kernel32.dll", EntryPoint = "K32QueryWorkingSetEx", SetLastError = true)]
    static extern bool QueryWorkingSetEx(IntPtr process, PSAPI_WORKING_SET_EX_INFORMATION* buffer, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocExNuma(IntPtr process, IntPtr address, nuint size, uint type, uint protect, uint node);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAlloc(IntPtr address, nuint size, uint type, uint protect);
    [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr address, nuint size, uint type);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, uint flags, ref nuint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, void* value, nuint size, IntPtr previous, IntPtr returnSize);
    [DllImport("kernel32.dll")] static extern void DeleteProcThreadAttributeList(IntPtr list);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcessW(string application, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint flags, IntPtr environment, string directory, ref STARTUPINFOEXW startup, out PROCESS_INFORMATION info);
    [DllImport("kernel32.dll")] static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr process, out uint code);
}
