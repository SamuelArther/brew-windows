using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text;

namespace WinTools;

// Shared system sampler for the monitoring commands (atop, sysstat, asitop, macmon, mactop, mole status).
// CPU and process figures come straight from NtQuerySystemInformation (no per-process handles needed),
// memory from GlobalMemoryStatusEx/GetPerformanceInfo, network from GetIfTable2, and everything else
// (disks, GPU, power, frequency, run queue) from the built-in performance counters through PDH.
sealed class PerfSampler : IDisposable
{
    public sealed class CpuTicks
    {
        public long Idle, Kernel, User, Dpc, Interrupt, Interrupts;   // 100 ns units; Kernel excludes Idle/Dpc/Interrupt
        public long Busy => Kernel + User + Dpc + Interrupt;
        public long All => Busy + Idle;
    }

    public sealed class Proc
    {
        public int Pid, ParentPid, Threads, Handles, Session, Priority, Running;
        public string Name;
        public long CreateTime, User, Kernel, WorkingSet, Private, Virtual, PageFaults, HardFaults, ContextSwitches;
        public long ReadOps, WriteOps, ReadBytes, WriteBytes;
        public long Cpu => User + Kernel;
    }

    public sealed class NetIf
    {
        public string Name, Description;
        public bool Up, Loopback;
        public long Speed, InBytes, OutBytes, InPackets, OutPackets, InErrors, OutErrors, InDrops, OutDrops, InMulticast;
    }

    // Cumulative IOCTL_DISK_PERFORMANCE counters of one physical drive (times in 100 ns).
    public sealed class DiskRaw
    {
        public string Name;
        public long ReadBytes, WriteBytes, ReadTime, WriteTime, IdleTime, QueryTime, Reads, Writes, Queue;
    }

    public sealed class Memory
    {
        public long Total, Available, Cached, CommitTotal, CommitLimit, KernelPaged, KernelNonPaged;
        public long SwapTotal, SwapUsed;
        public int ProcessCount, ThreadCount, HandleCount;
    }

    public sealed class Sample
    {
        public DateTime Time;
        public long Stamp;                         // Stopwatch ticks
        public CpuTicks[] Cpus;
        public CpuTicks Total;
        public Memory Mem;
        public Dictionary<int, Proc> Procs;
        public Dictionary<string, NetIf> Net;
        public Dictionary<string, DiskRaw> Disks = new();
        public Dictionary<string, Dictionary<string, double>> Counters = new();   // PDH counter -> instance -> value

        public double Seconds(Sample later) => (later.Stamp - Stamp) / (double)Stopwatch.Frequency;

        public double Counter(string path, string instance = "")
            => Counters.TryGetValue(path, out var v) && v.TryGetValue(instance, out var d) ? d : double.NaN;

        public Dictionary<string, double> Instances(string path)
            => Counters.TryGetValue(path, out var v) ? v : new Dictionary<string, double>();
    }

    // Counter groups callers can ask for; each is a list of English PDH paths.
    public static readonly string[] DiskCounters =
    [
        @"\PhysicalDisk(*)\Disk Reads/sec", @"\PhysicalDisk(*)\Disk Writes/sec",
        @"\PhysicalDisk(*)\Disk Read Bytes/sec", @"\PhysicalDisk(*)\Disk Write Bytes/sec",
        @"\PhysicalDisk(*)\Avg. Disk sec/Read", @"\PhysicalDisk(*)\Avg. Disk sec/Write",
        @"\PhysicalDisk(*)\Avg. Disk sec/Transfer", @"\PhysicalDisk(*)\% Idle Time",
        @"\PhysicalDisk(*)\Avg. Disk Queue Length", @"\PhysicalDisk(*)\Split IO/Sec",
    ];
    public static readonly string[] SystemCounters =
    [
        @"\System\Context Switches/sec", @"\System\Processor Queue Length", @"\System\Processes",
        @"\System\Threads", @"\Memory\Page Faults/sec", @"\Memory\Pages Input/sec", @"\Memory\Pages Output/sec",
        @"\Memory\Pages/sec", @"\Memory\Page Reads/sec", @"\Memory\Page Writes/sec", @"\Memory\Cache Faults/sec",
        @"\Memory\Transition Faults/sec", @"\Memory\Demand Zero Faults/sec", @"\Memory\Modified Page List Bytes",
        @"\Memory\Standby Cache Normal Priority Bytes", @"\Memory\Free & Zero Page List Bytes",
        @"\Paging File(_Total)\% Usage",
    ];
    public static readonly string[] SocCounters =
    [
        @"\GPU Engine(*)\Utilization Percentage", @"\GPU Adapter Memory(*)\Dedicated Usage",
        @"\GPU Adapter Memory(*)\Shared Usage", @"\Energy Meter(*)\Power",
        @"\Thermal Zone Information(*)\High Precision Temperature", @"\Thermal Zone Information(*)\Temperature",
        @"\Processor Information(*)\% Processor Performance", @"\Processor Information(*)\Processor Frequency",
    ];
    public static readonly string[] NetCounters =
    [
        @"\TCPv4\Segments Received/sec", @"\TCPv4\Segments Sent/sec", @"\TCPv4\Segments Retransmitted/sec",
        @"\TCPv4\Connections Active", @"\TCPv4\Connections Passive", @"\TCPv4\Connection Failures",
        @"\TCPv4\Connections Established", @"\UDPv4\Datagrams Received/sec", @"\UDPv4\Datagrams Sent/sec",
        @"\UDPv4\Datagrams No Port/sec", @"\UDPv4\Datagrams Received Errors", @"\IPv4\Datagrams Received/sec",
        @"\IPv4\Datagrams Sent/sec", @"\IPv4\Datagrams Forwarded/sec", @"\IPv4\Datagrams Received Discarded",
        @"\TCPv4\Connections Reset", @"\IPv4\Datagrams Received Delivered/sec", @"\IPv4\Fragments Received/sec",
        @"\IPv4\Fragments Re-assembled/sec", @"\IPv4\Fragmented Datagrams/sec", @"\IPv4\Fragments Created/sec",
    ];

    readonly IntPtr query;
    readonly Dictionary<string, IntPtr> counters = new();
    readonly bool wantProcs;

    public PerfSampler(bool processes, params string[][] counterGroups)
    {
        wantProcs = processes;
        if (counterGroups.Length == 0) return;
        if (PdhOpenQueryW(null, IntPtr.Zero, out query) != 0) { query = IntPtr.Zero; return; }
        foreach (var path in counterGroups.SelectMany(g => g).Distinct())
            if (PdhAddEnglishCounterW(query, path, IntPtr.Zero, out var c) == 0) counters[path] = c;
        PdhCollectQueryData(query);   // rate counters need a first reading
    }

    public void Dispose()
    {
        if (query != IntPtr.Zero) PdhCloseQuery(query);
    }

    public Sample Take()
    {
        var s = new Sample { Time = DateTime.Now, Stamp = Stopwatch.GetTimestamp() };
        s.Cpus = ReadCpus();
        s.Total = new CpuTicks();
        foreach (var c in s.Cpus)
        {
            s.Total.Idle += c.Idle; s.Total.Kernel += c.Kernel; s.Total.User += c.User;
            s.Total.Dpc += c.Dpc; s.Total.Interrupt += c.Interrupt; s.Total.Interrupts += c.Interrupts;
        }
        s.Mem = ReadMemory();
        s.Procs = wantProcs ? ReadProcesses() : new Dictionary<int, Proc>();
        s.Net = ReadNetwork();
        s.Disks = ReadDisks();
        if (query != IntPtr.Zero && PdhCollectQueryData(query) == 0)
            foreach (var (path, handle) in counters) s.Counters[path] = ReadCounter(handle);
        return s;
    }

    // Wait for the next interval, but return early (true) when the user presses q or Ctrl+C.
    public static bool Sleep(double seconds, Func<ConsoleKeyInfo, bool> onKey = null)
    {
        var until = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);
        while (Stopwatch.GetTimestamp() < until)
        {
            if (onKey != null && !Console.IsInputRedirected)
            {
                try
                {
                    while (Console.KeyAvailable)
                        if (onKey(Console.ReadKey(true))) return true;
                }
                catch (InvalidOperationException) { onKey = null; }
            }
            Thread.Sleep(50);
        }
        return false;
    }

    // ---- percentages and rates between two samples ----

    public static double Pct(long part, long whole) => whole <= 0 ? 0 : Math.Clamp(100.0 * part / whole, 0, 100);

    public static double CpuBusy(CpuTicks a, CpuTicks b) => Pct(b.Busy - a.Busy, b.All - a.All);

    // A process's CPU share of one core (100% = one full core), like top/pidstat.
    public static double ProcCpu(Proc a, Proc b, double seconds) => seconds <= 0 ? 0 : (b.Cpu - (a?.Cpu ?? 0)) / (seconds * 1e5);

    public static string HumanBytes(double bytes, int decimals = 1)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        var i = 0;
        while (Math.Abs(bytes) >= 1024 && i < units.Length - 1) { bytes /= 1024; i++; }
        return i == 0 ? $"{bytes:0} B" : bytes.ToString("F" + decimals) + " " + units[i];
    }

    public static string Bar(double pct, int width, char full = '█', char empty = '░')
    {
        var n = (int)Math.Round(Math.Clamp(pct, 0, 100) / 100 * width);
        return new string(full, n) + new string(empty, width - n);
    }

    // ---- static machine facts ----

    public static string CpuName
    {
        get
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return (key?.GetValue("ProcessorNameString") as string)?.Trim() ?? "Unknown CPU";
            }
            catch (System.Security.SecurityException) { return "Unknown CPU"; }
        }
    }

    public static int BaseMhz
    {
        get
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("~MHz") is int mhz ? mhz : 0;
            }
            catch (System.Security.SecurityException) { return 0; }
        }
    }

    public static string OsVersion
    {
        get
        {
            // RTL_OSVERSIONINFOW: size, major, minor, build, platform, then a 128-char service pack string.
            var v = new uint[70];
            v[0] = 276;
            RtlGetVersion(v);
            return $"{v[1]}.{v[2]}.{v[3]}";
        }
    }

    public static string OsName
    {
        get
        {
            var build = int.Parse(OsVersion.Split('.')[2]);
            return build >= 22000 ? "Windows 11" : "Windows 10";
        }
    }

    public static string Arch => RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "x86_64",
        Architecture.Arm64 => "aarch64",
        Architecture.X86 => "i686",
        _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
    };

    // sysstat-style header line: "Windows 10.0.26200 (HOST)   10/16/2026   _x86_64_   (28 CPU)"
    public static string SysstatHeader() =>
        $"Windows {OsVersion} ({Environment.MachineName}) \t{DateTime.Now:MM/dd/yyyy} \t_{Arch}_\t({Environment.ProcessorCount} CPU)";

    // Core efficiency class per logical processor (0 = efficiency/E core, higher = performance).
    // Every value is equal on CPUs without hybrid cores.
    public static byte[] EfficiencyClasses()
    {
        var classes = new byte[Environment.ProcessorCount];
        uint len = 0;
        GetLogicalProcessorInformationEx(0, IntPtr.Zero, ref len);
        if (len == 0) return classes;
        var buf = Marshal.AllocHGlobal((int)len);
        try
        {
            if (!GetLogicalProcessorInformationEx(0, buf, ref len)) return classes;
            for (var off = 0; off < len;)
            {
                var size = Marshal.ReadInt32(buf, off + 4);
                var eff = Marshal.ReadByte(buf, off + 9);
                int groups = Marshal.ReadInt16(buf, off + 30);
                for (var g = 0; g < groups; g++)
                {
                    var mask = (ulong)Marshal.ReadInt64(buf, off + 32 + g * 16);
                    var group = Marshal.ReadInt16(buf, off + 40 + g * 16);
                    for (var bit = 0; bit < 64; bit++)
                    {
                        if ((mask & (1UL << bit)) == 0) continue;
                        var cpu = group * 64 + bit;
                        if (cpu < classes.Length) classes[cpu] = eff;
                    }
                }
                off += size;
            }
        }
        finally { Marshal.FreeHGlobal(buf); }
        return classes;
    }

    public static (int Percent, bool OnAc, bool Charging, bool HasBattery) Battery()
    {
        if (!GetSystemPowerStatus(out var st)) return (0, true, false, false);
        var has = (st.BatteryFlag & 128) == 0 && st.BatteryFlag != 255;
        return (st.BatteryLifePercent == 255 ? 0 : st.BatteryLifePercent, st.ACLineStatus == 1, (st.BatteryFlag & 8) != 0, has);
    }

    // Owner of a process as (user name, RID); RID stands in for a Unix UID in sysstat output.
    static readonly Dictionary<int, (string, int)> ownerCache = new();
    public static (string User, int Uid) Owner(int pid)
    {
        if (ownerCache.TryGetValue(pid, out var cached)) return cached;
        var result = ("?", -1);
        if (pid is 0 or 4) return ownerCache[pid] = ("SYSTEM", 18);
        var h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (h != IntPtr.Zero)
        {
            if (OpenProcessToken(h, TOKEN_QUERY, out var token))
            {
                GetTokenInformation(token, 1, IntPtr.Zero, 0, out var need);
                var buf = Marshal.AllocHGlobal((int)Math.Max(need, 64));
                if (GetTokenInformation(token, 1, buf, need, out _))
                {
                    var sid = Marshal.ReadIntPtr(buf);
                    var name = new StringBuilder(256);
                    var domain = new StringBuilder(256);
                    uint nameLen = 256, domainLen = 256;
                    var user = LookupAccountSidW(null, sid, name, ref nameLen, domain, ref domainLen, out _) ? name.ToString() : "?";
                    var count = Marshal.ReadByte(GetSidSubAuthorityCount(sid));
                    var rid = count > 0 ? Marshal.ReadInt32(GetSidSubAuthority(sid, (uint)(count - 1))) : -1;
                    result = (user, rid);
                }
                Marshal.FreeHGlobal(buf);
                CloseHandle(token);
            }
            CloseHandle(h);
        }
        ownerCache[pid] = result;
        return result;
    }

    public static string CommandLine(int pid)
    {
        var h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (h == IntPtr.Zero) return null;
        try
        {
            var buf = Marshal.AllocHGlobal(65536);
            try
            {
                if (NtQueryInformationProcess(h, 60, buf, 65536, out _) != 0) return null;
                var len = (ushort)Marshal.ReadInt16(buf);
                return Marshal.PtrToStringUni(Marshal.ReadIntPtr(buf, 8), len / 2);
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        finally { CloseHandle(h); }
    }

    // ---- raw readers ----

    static CpuTicks[] ReadCpus()
    {
        var list = new List<CpuTicks>();
        var groups = GetActiveProcessorGroupCount();
        for (ushort g = 0; g < Math.Max((ushort)1, groups); g++)
        {
            var n = (int)GetActiveProcessorCount(g);
            var size = n * 48;
            var buf = Marshal.AllocHGlobal(size);
            try
            {
                var status = groups > 1
                    ? NtQuerySystemInformationEx(8, ref g, 2, buf, (uint)size, out _)
                    : NtQuerySystemInformation(8, buf, (uint)size, out _);
                if (status != 0) break;
                for (var i = 0; i < n; i++)
                {
                    var o = i * 48;
                    long idle = Marshal.ReadInt64(buf, o), kernel = Marshal.ReadInt64(buf, o + 8), user = Marshal.ReadInt64(buf, o + 16);
                    long dpc = Marshal.ReadInt64(buf, o + 24), intr = Marshal.ReadInt64(buf, o + 32);
                    list.Add(new CpuTicks
                    {
                        Idle = idle, User = user, Dpc = dpc, Interrupt = intr,
                        Kernel = Math.Max(0, kernel - idle - dpc - intr), Interrupts = (uint)Marshal.ReadInt32(buf, o + 40),
                    });
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        return list.ToArray();
    }

    static Memory ReadMemory()
    {
        var m = new Memory();
        var ms = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref ms)) { m.Total = (long)ms.ullTotalPhys; m.Available = (long)ms.ullAvailPhys; }
        var pi = new PERFORMANCE_INFORMATION { cb = (uint)Marshal.SizeOf<PERFORMANCE_INFORMATION>() };
        if (GetPerformanceInfo(ref pi, pi.cb))
        {
            var page = (long)pi.PageSize;
            m.Cached = (long)pi.SystemCache * page;
            m.CommitTotal = (long)pi.CommitTotal * page;
            m.CommitLimit = (long)pi.CommitLimit * page;
            m.KernelPaged = (long)pi.KernelPaged * page;
            m.KernelNonPaged = (long)pi.KernelNonpaged * page;
            m.ProcessCount = (int)pi.ProcessCount; m.ThreadCount = (int)pi.ThreadCount; m.HandleCount = (int)pi.HandleCount;
        }
        // Paging files: SYSTEM_PAGEFILE_INFORMATION entries, sizes in pages.
        var buf = Marshal.AllocHGlobal(4096);
        try
        {
            if (NtQuerySystemInformation(18, buf, 4096, out var got) == 0 && got > 0)
            {
                for (var off = 0; ;)
                {
                    m.SwapTotal += (uint)Marshal.ReadInt32(buf, off + 4) * 4096L;
                    m.SwapUsed += (uint)Marshal.ReadInt32(buf, off + 8) * 4096L;
                    var next = Marshal.ReadInt32(buf, off);
                    if (next == 0) break;
                    off += next;
                }
            }
        }
        finally { Marshal.FreeHGlobal(buf); }
        return m;
    }

    static Dictionary<int, Proc> ReadProcesses()
    {
        var result = new Dictionary<int, Proc>();
        var size = 1 << 20;
        IntPtr buf;
        while (true)
        {
            buf = Marshal.AllocHGlobal(size);
            var status = NtQuerySystemInformation(5, buf, (uint)size, out var need);
            if (status == 0) break;
            Marshal.FreeHGlobal(buf);
            if (status != unchecked((int)0xC0000004)) return result;   // STATUS_INFO_LENGTH_MISMATCH
            size = (int)Math.Max(need + 65536, size * 2);
        }
        try
        {
            for (var off = 0; ;)
            {
                var p = buf + off;
                var threads = Marshal.ReadInt32(p, 4);
                var nameLen = (ushort)Marshal.ReadInt16(p, 56);
                var namePtr = Marshal.ReadIntPtr(p, 64);
                var pid = (int)Marshal.ReadIntPtr(p, 80);
                var proc = new Proc
                {
                    Pid = pid,
                    Threads = threads,
                    CreateTime = Marshal.ReadInt64(p, 32),
                    User = Marshal.ReadInt64(p, 40),
                    Kernel = Marshal.ReadInt64(p, 48),
                    Name = namePtr == IntPtr.Zero ? (pid == 0 ? "Idle" : "System") : Marshal.PtrToStringUni(namePtr, nameLen / 2),
                    Priority = Marshal.ReadInt32(p, 72),
                    ParentPid = (int)Marshal.ReadIntPtr(p, 88),
                    Handles = Marshal.ReadInt32(p, 96),
                    Session = Marshal.ReadInt32(p, 100),
                    Virtual = Marshal.ReadInt64(p, 120),
                    PageFaults = (uint)Marshal.ReadInt32(p, 128),
                    HardFaults = (uint)Marshal.ReadInt32(p, 16),
                    WorkingSet = Marshal.ReadInt64(p, 144),
                    Private = Marshal.ReadInt64(p, 184),
                    ReadOps = Marshal.ReadInt64(p, 208),
                    WriteOps = Marshal.ReadInt64(p, 216),
                    ReadBytes = Marshal.ReadInt64(p, 232),
                    WriteBytes = Marshal.ReadInt64(p, 240),
                };
                for (var t = 0; t < threads; t++)
                {
                    var th = p + 256 + t * 80;
                    proc.ContextSwitches += (uint)Marshal.ReadInt32(th, 64);
                    var state = Marshal.ReadInt32(th, 68);
                    if (state is 1 or 2 or 3 or 7) proc.Running++;   // ready, running, standby, deferred ready
                }
                result[pid] = proc;
                var next = Marshal.ReadInt32(p);
                if (next == 0) break;
                off += next;
            }
        }
        finally { Marshal.FreeHGlobal(buf); }
        return result;
    }

    static Dictionary<string, NetIf> ReadNetwork()
    {
        var result = new Dictionary<string, NetIf>();
        if (GetIfTable2(out var table) != 0) return result;
        try
        {
            var n = Marshal.ReadInt32(table);
            for (var i = 0; i < n; i++)
            {
                var row = table + 8 + i * 1352;
                var flags = Marshal.ReadByte(row, 1152);
                var type = Marshal.ReadInt32(row, 1128);
                var hardware = (flags & 1) != 0;
                var filter = (flags & 2) != 0;
                if (filter || (!hardware && type != 24)) continue;   // physical adapters and loopback only
                var alias = Marshal.PtrToStringUni(row + 28) ?? "";
                var name = type == 24 ? "lo" : alias.Replace(' ', '_');
                if (result.ContainsKey(name)) continue;
                result[name] = new NetIf
                {
                    Name = name,
                    Description = Marshal.PtrToStringUni(row + 542),
                    Loopback = type == 24,
                    Up = Marshal.ReadInt32(row, 1156) == 1,
                    Speed = Marshal.ReadInt64(row, 1200),
                    InBytes = Marshal.ReadInt64(row, 1208),
                    InPackets = Marshal.ReadInt64(row, 1216) + Marshal.ReadInt64(row, 1224),
                    InMulticast = Marshal.ReadInt64(row, 1224),
                    InDrops = Marshal.ReadInt64(row, 1232),
                    InErrors = Marshal.ReadInt64(row, 1240),
                    OutBytes = Marshal.ReadInt64(row, 1280),
                    OutPackets = Marshal.ReadInt64(row, 1288) + Marshal.ReadInt64(row, 1296),
                    OutDrops = Marshal.ReadInt64(row, 1304),
                    OutErrors = Marshal.ReadInt64(row, 1312),
                };
            }
        }
        finally { FreeMibTable(table); }
        return result;
    }

    static Dictionary<string, DiskRaw> ReadDisks()
    {
        var result = new Dictionary<string, DiskRaw>();
        var buf = new byte[88];
        for (int n = 0, misses = 0; n < 64 && misses < 8; n++)
        {
            // No access rights are needed for IOCTL_DISK_PERFORMANCE, so this works unelevated.
            var h = CreateFileW($@"\\.\PhysicalDrive{n}", 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (h == new IntPtr(-1)) { misses++; continue; }
            misses = 0;
            if (DeviceIoControl(h, 0x70020, IntPtr.Zero, 0, buf, buf.Length, out _, IntPtr.Zero))
            {
                result["disk" + n] = new DiskRaw
                {
                    Name = "disk" + n,
                    ReadBytes = BitConverter.ToInt64(buf, 0), WriteBytes = BitConverter.ToInt64(buf, 8),
                    ReadTime = BitConverter.ToInt64(buf, 16), WriteTime = BitConverter.ToInt64(buf, 24), IdleTime = BitConverter.ToInt64(buf, 32),
                    Reads = BitConverter.ToUInt32(buf, 40), Writes = BitConverter.ToUInt32(buf, 44), Queue = BitConverter.ToUInt32(buf, 48),
                    QueryTime = BitConverter.ToInt64(buf, 56),
                };
            }
            CloseHandle(h);
        }
        return result;
    }

    // Difference of two 32-bit I/O counts that may have wrapped.
    public static long Count32(long before, long after) => (uint)(after - before);

    static Dictionary<string, double> ReadCounter(IntPtr counter)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        uint size = 0;
        var status = PdhGetFormattedCounterArrayW(counter, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, ref size, out _, IntPtr.Zero);
        if (status != PDH_MORE_DATA || size == 0) return values;
        var buf = Marshal.AllocHGlobal((int)size);
        try
        {
            if (PdhGetFormattedCounterArrayW(counter, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, ref size, out var count, buf) != 0) return values;
            for (var i = 0; i < count; i++)
            {
                var item = buf + i * 24;
                var name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(item)) ?? "";
                if (Marshal.ReadInt32(item, 8) is not (0 or 1)) continue;   // PDH_CSTATUS_VALID_DATA / NEW_DATA
                var value = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(item, 16));
                // Instances with the same name (e.g. several "_Total" rows) are summed.
                values[name] = values.TryGetValue(name, out var prev) ? prev + value : value;
            }
        }
        finally { Marshal.FreeHGlobal(buf); }
        return values;
    }

    // GPU busy % the way Task Manager reports it: per adapter, the busiest engine type summed over processes.
    public static double GpuBusy(Sample s, out Dictionary<int, double> perProcess)
    {
        perProcess = new Dictionary<int, double>();
        var byEngine = new Dictionary<string, double>();
        foreach (var (inst, value) in s.Instances(@"\GPU Engine(*)\Utilization Percentage"))
        {
            // pid_1234_luid_0x0_0xABCD_phys_0_eng_0_engtype_3D
            var parts = inst.Split('_');
            if (parts.Length < 11 || !int.TryParse(parts[1], out var pid)) continue;
            var key = string.Join('_', parts[3..5]) + "/" + parts[^1];
            byEngine[key] = byEngine.GetValueOrDefault(key) + value;
            perProcess[pid] = Math.Max(perProcess.GetValueOrDefault(pid), value);
        }
        return byEngine.Count == 0 ? double.NaN : Math.Min(100, byEngine.Values.Max());
    }

    public static double GpuMemory(Sample s)
        => s.Instances(@"\GPU Adapter Memory(*)\Dedicated Usage").Values.DefaultIfEmpty(double.NaN).Sum();

    // RAPL energy meters (milliwatts) -> watts: PKG = package, PP0 = cores, PP1 = integrated GPU, DRAM.
    public static double Watts(Sample s, string plane)
    {
        var found = s.Instances(@"\Energy Meter(*)\Power").Where(kv => kv.Key.EndsWith("_" + plane, StringComparison.OrdinalIgnoreCase)).ToList();
        return found.Count == 0 ? double.NaN : found.Sum(kv => kv.Value) / 1000;
    }

    // Average ACPI thermal-zone temperature in Celsius, or NaN when the firmware exposes none.
    public static double TemperatureC(Sample s)
    {
        var precise = s.Instances(@"\Thermal Zone Information(*)\High Precision Temperature").Values.Where(v => v > 0).ToList();
        if (precise.Count > 0) return precise.Average() / 10 - 273.15;
        var coarse = s.Instances(@"\Thermal Zone Information(*)\Temperature").Values.Where(v => v > 0).ToList();
        return coarse.Count > 0 ? coarse.Average() - 273.15 : double.NaN;
    }

    // Effective clock of one logical CPU ("group,index") or "_Total", from the % Processor Performance counter.
    public static double FrequencyMhz(Sample s, string instance)
    {
        var perf = s.Counter(@"\Processor Information(*)\% Processor Performance", instance);
        var nominal = s.Counter(@"\Processor Information(*)\Processor Frequency", instance);
        if (double.IsNaN(nominal) || nominal <= 0) nominal = BaseMhz;
        return double.IsNaN(perf) ? nominal : nominal * perf / 100;
    }

    public static string CpuInstance(int index) => $"{index / 64},{index % 64}";

    // Hand-written JSON helpers (no reflection serialization under NativeAOT).
    public static string Json(string s)
    {
        if (s == null) return "null";
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append($"\\u{(int)c:x4}"); else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }

    public static string Json(double d, int decimals = 4)
        => double.IsNaN(d) || double.IsInfinity(d) ? "null" : Math.Round(d, decimals).ToString(System.Globalization.CultureInfo.InvariantCulture);

    // ---- recordings (atop -w, sar -o): one text record per sample, readable by both ----

    public const string RecordingMagic = "ATOPWIN 1";

    public static void WriteSample(TextWriter w, Sample s)
    {
        var inv = CultureInfo.InvariantCulture;
        w.WriteLine($"S {new DateTimeOffset(s.Time).ToUnixTimeMilliseconds()} {(s.Stamp / (double)Stopwatch.Frequency).ToString("R", inv)}");
        foreach (var c in s.Cpus) w.WriteLine($"C {c.Idle} {c.Kernel} {c.User} {c.Dpc} {c.Interrupt} {c.Interrupts}");
        var m = s.Mem;
        w.WriteLine($"M {m.Total} {m.Available} {m.Cached} {m.CommitTotal} {m.CommitLimit} {m.KernelPaged} {m.KernelNonPaged} {m.SwapTotal} {m.SwapUsed} {m.ProcessCount} {m.ThreadCount} {m.HandleCount}");
        foreach (var p in s.Procs.Values)
            w.WriteLine($"P {p.Pid} {p.ParentPid} {p.Threads} {p.Handles} {p.Session} {p.Priority} {p.Running} {p.CreateTime} {p.User} {p.Kernel} {p.WorkingSet} {p.Private} {p.Virtual} {p.PageFaults} {p.ContextSwitches} {p.ReadOps} {p.WriteOps} {p.ReadBytes} {p.WriteBytes} {p.Name}");
        foreach (var d in s.Disks.Values)
            w.WriteLine($"D {d.Name} {d.ReadBytes} {d.WriteBytes} {d.ReadTime} {d.WriteTime} {d.IdleTime} {d.QueryTime} {d.Reads} {d.Writes} {d.Queue}");
        foreach (var n in s.Net.Values)
            w.WriteLine($"N {n.Name} {(n.Up ? 1 : 0)} {(n.Loopback ? 1 : 0)} {n.Speed} {n.InBytes} {n.OutBytes} {n.InPackets} {n.OutPackets} {n.InErrors} {n.OutErrors} {n.InDrops} {n.OutDrops} {n.InMulticast}");
        foreach (var (path, values) in s.Counters)
            foreach (var (inst, v) in values)
                w.WriteLine($"K {path}|{inst}|{v.ToString("R", inv)}");
        w.WriteLine("E");
    }

    public static List<Sample> ReadSamples(string file)
    {
        var list = new List<Sample>();
        Sample s = null;
        List<CpuTicks> cpus = null;
        var inv = CultureInfo.InvariantCulture;
        using var r = new StreamReader(file);
        var header = r.ReadLine();
        if (header == null || !header.StartsWith(RecordingMagic)) throw new InvalidOperationException($"{file}: not a recording written by atop -w or sar -o");
        for (string line; (line = r.ReadLine()) != null;)
        {
            if (line.Length == 0) continue;
            var f = line.Split(' ');
            long L(int i) => long.Parse(f[i], inv);
            switch (line[0])
            {
                case 'S':
                    s = new Sample
                    {
                        Time = DateTimeOffset.FromUnixTimeMilliseconds(L(1)).LocalDateTime,
                        Stamp = (long)(double.Parse(f[2], inv) * Stopwatch.Frequency),
                        Procs = new(), Net = new(),
                    };
                    cpus = new List<CpuTicks>();
                    break;
                case 'C':
                    cpus.Add(new CpuTicks { Idle = L(1), Kernel = L(2), User = L(3), Dpc = L(4), Interrupt = L(5), Interrupts = L(6) });
                    break;
                case 'M':
                    s.Mem = new Memory
                    {
                        Total = L(1), Available = L(2), Cached = L(3), CommitTotal = L(4), CommitLimit = L(5), KernelPaged = L(6),
                        KernelNonPaged = L(7), SwapTotal = L(8), SwapUsed = L(9), ProcessCount = (int)L(10), ThreadCount = (int)L(11), HandleCount = (int)L(12),
                    };
                    break;
                case 'P':
                    var p = new Proc
                    {
                        Pid = (int)L(1), ParentPid = (int)L(2), Threads = (int)L(3), Handles = (int)L(4), Session = (int)L(5), Priority = (int)L(6),
                        Running = (int)L(7), CreateTime = L(8), User = L(9), Kernel = L(10), WorkingSet = L(11), Private = L(12), Virtual = L(13),
                        PageFaults = L(14), ContextSwitches = L(15), ReadOps = L(16), WriteOps = L(17), ReadBytes = L(18), WriteBytes = L(19),
                        Name = string.Join(' ', f[20..]),
                    };
                    s.Procs[p.Pid] = p;
                    break;
                case 'N':
                    s.Net[f[1]] = new NetIf
                    {
                        Name = f[1], Up = f[2] == "1", Loopback = f[3] == "1", Speed = L(4), InBytes = L(5), OutBytes = L(6), InPackets = L(7),
                        OutPackets = L(8), InErrors = L(9), OutErrors = L(10), InDrops = L(11), OutDrops = L(12), InMulticast = L(13),
                    };
                    break;
                case 'D':
                    s.Disks[f[1]] = new DiskRaw
                    {
                        Name = f[1], ReadBytes = L(2), WriteBytes = L(3), ReadTime = L(4), WriteTime = L(5), IdleTime = L(6), QueryTime = L(7),
                        Reads = L(8), Writes = L(9), Queue = L(10),
                    };
                    break;
                case 'K':
                    var k = line[2..].Split('|');
                    if (k.Length != 3) break;
                    if (!s.Counters.TryGetValue(k[0], out var values)) s.Counters[k[0]] = values = new(StringComparer.OrdinalIgnoreCase);
                    values[k[1]] = double.Parse(k[2], inv);
                    break;
                case 'E':
                    s.Cpus = cpus.ToArray();
                    s.Total = new CpuTicks();
                    foreach (var c in s.Cpus)
                    {
                        s.Total.Idle += c.Idle; s.Total.Kernel += c.Kernel; s.Total.User += c.User;
                        s.Total.Dpc += c.Dpc; s.Total.Interrupt += c.Interrupt; s.Total.Interrupts += c.Interrupts;
                    }
                    s.Mem ??= new Memory();
                    list.Add(s);
                    break;
            }
        }
        return list;
    }

    // ---- interop ----

    const uint PDH_FMT_DOUBLE = 0x200, PDH_FMT_NOCAP100 = 0x8000, PDH_MORE_DATA = 0x800007D2;
    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, TOKEN_QUERY = 8;

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORYSTATUSEX
    {
        public uint dwLength, dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PERFORMANCE_INFORMATION
    {
        public uint cb;
        public nuint CommitTotal, CommitLimit, CommitPeak, PhysicalTotal, PhysicalAvailable, SystemCache, KernelTotal, KernelPaged, KernelNonpaged, PageSize;
        public uint HandleCount, ProcessCount, ThreadCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
        public uint BatteryLifeTime, BatteryFullLifeTime;
    }

    [DllImport("ntdll.dll")] static extern int NtQuerySystemInformation(int cls, IntPtr buf, uint len, out uint ret);
    [DllImport("ntdll.dll")] static extern int NtQuerySystemInformationEx(int cls, ref ushort input, uint inputLen, IntPtr buf, uint len, out uint ret);
    [DllImport("ntdll.dll")] static extern int NtQueryInformationProcess(IntPtr h, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] static extern int RtlGetVersion([In, Out] uint[] v);
    [DllImport("kernel32.dll")] static extern ushort GetActiveProcessorGroupCount();
    [DllImport("kernel32.dll")] static extern uint GetActiveProcessorCount(ushort group);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX m);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetLogicalProcessorInformationEx(int rel, IntPtr buf, ref uint len);
    [DllImport("kernel32.dll")] static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS s);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr sa, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr h, uint code, IntPtr inBuf, int inLen, byte[] outBuf, int outLen, out int ret, IntPtr overlapped);
    [DllImport("psapi.dll")] static extern bool GetPerformanceInfo(ref PERFORMANCE_INFORMATION p, uint cb);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool OpenProcessToken(IntPtr h, uint access, out IntPtr token);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool GetTokenInformation(IntPtr token, int cls, IntPtr buf, uint len, out uint ret);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool LookupAccountSidW(string system, IntPtr sid, StringBuilder name, ref uint nameLen, StringBuilder domain, ref uint domainLen, out int use);
    [DllImport("advapi32.dll")] static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);
    [DllImport("advapi32.dll")] static extern IntPtr GetSidSubAuthority(IntPtr sid, uint index);
    [DllImport("iphlpapi.dll")] static extern int GetIfTable2(out IntPtr table);
    [DllImport("iphlpapi.dll")] static extern void FreeMibTable(IntPtr table);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] static extern uint PdhOpenQueryW(string source, IntPtr user, out IntPtr query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] static extern uint PdhAddEnglishCounterW(IntPtr query, string path, IntPtr user, out IntPtr counter);
    [DllImport("pdh.dll")] static extern uint PdhCollectQueryData(IntPtr query);
    [DllImport("pdh.dll")] static extern uint PdhCloseQuery(IntPtr query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    static extern uint PdhGetFormattedCounterArrayW(IntPtr counter, uint format, ref uint size, out uint count, IntPtr buf);
}
