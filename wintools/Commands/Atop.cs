using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// atop: system and process monitor. Each screen covers the last interval (atop's first "since boot"
// screen is skipped: Windows keeps most of these figures only as rates). Samples can be recorded with -w and replayed with -r; -P prints
// atop's parseable output (CPU, cpu, CPL, MEM, SWP, PAG, DSK, NET, PRG, PRC, PRM, PRD, ALL).
static class Atop
{
    const string Version = "2.13.0";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("atop", "System and process monitor (Windows performance counters)", Run);

    sealed class Options
    {
        public double Interval = 10;
        public int Samples = int.MaxValue;
        public string WriteFile, ReadFile;
        public HashSet<string> Parse;
        public char View = 'g', Sort = 'A';
        public bool All, CommandLine, PerSecond;
        public TimeSpan? Begin, End;
    }

    static int Run(string[] args)
    {
        var o = new Options();
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"option {a} needs an argument");
            if (a is "-h" or "--help") { Help(); return 0; }
            if (a == "-V") { Console.WriteLine($"Version: {Version} (wintools) - {DateTime.Now:yyyy/MM/dd}   <wintools atop for Windows>"); return 0; }
            if (!a.StartsWith('-') || a.Length == 1) { positional.Add(a); continue; }
            if (a.StartsWith("-P")) { o.Parse = ParseLabels(a.Length > 2 ? a[2..] : Next()); continue; }
            if (a == "-w") { o.WriteFile = Next(); continue; }
            if (a == "-r") { o.ReadFile = i + 1 < args.Length && !args[i + 1].StartsWith('-') && !IsNumber(args[i + 1]) ? args[++i] : DefaultLog(); continue; }
            if (a == "-b") { o.Begin = ParseTime(Next()); continue; }
            if (a == "-e") { o.End = ParseTime(Next()); continue; }
            foreach (var f in a[1..])
            {
                switch (f)
                {
                    case 'g' or 'm' or 'd' or 'n' or 's' or 'v' or 'c' or 'o': if (f == 'c') o.CommandLine = true; else o.View = f; break;
                    case 'C' or 'M' or 'D' or 'N' or 'A': o.Sort = f; break;
                    case 'a': o.All = true; break;
                    case '1': o.PerSecond = true; break;
                    case 'f' or 'F' or 'G' or 'u' or 'p' or 'j' or 'y' or 'x' or 'L' or 'l' or 'R' or 'S' or 'z': break;   // accepted
                    default: throw new UsageException($"unknown flag -{f} (see atop -h)");
                }
            }
        }
        if (positional.Count > 0 && !double.TryParse(positional[0], CultureInfo.InvariantCulture, out o.Interval))
            throw new UsageException($"invalid interval '{positional[0]}'");
        if (positional.Count > 1 && !int.TryParse(positional[1], out o.Samples))
            throw new UsageException($"invalid number of samples '{positional[1]}'");
        if (o.Interval <= 0) throw new UsageException("interval must be positive");

        return o.ReadFile != null ? Replay(o) : Live(o);
    }

    static bool IsNumber(string s) => double.TryParse(s, CultureInfo.InvariantCulture, out _);
    static string DefaultLog() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "atop", $"atop_{DateTime.Now:yyyyMMdd}");

    static TimeSpan ParseTime(string s)
    {
        var t = s.Contains(' ') ? s.Split(' ')[^1] : s;
        return TimeSpan.TryParse(t, CultureInfo.InvariantCulture, out var ts) ? ts : throw new UsageException($"invalid time '{s}' (use hh:mm)");
    }

    static HashSet<string> ParseLabels(string list)
    {
        string[] known = ["CPU", "cpu", "CPL", "MEM", "SWP", "PAG", "DSK", "NET", "PRG", "PRC", "PRM", "PRD", "PRN", "PRE"];
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var label in list.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (label == "ALL") set.UnionWith(known);
            else if (known.Contains(label)) set.Add(label);
            else throw new UsageException($"unknown parseable label '{label}'");
        }
        return set;
    }

    static int Live(Options o)
    {
        using var sampler = new PerfSampler(true, PerfSampler.DiskCounters, PerfSampler.SystemCounters, PerfSampler.NetCounters, PerfSampler.SocCounters);
        StreamWriter raw = null;
        if (o.WriteFile != null)
        {
            raw = new StreamWriter(o.WriteFile, append: File.Exists(o.WriteFile) && new FileInfo(o.WriteFile).Length > 0);
            if (raw.BaseStream.Position == 0) raw.WriteLine($"{PerfSampler.RecordingMagic} {Environment.MachineName} {Environment.ProcessorCount}");
        }
        var interactive = o.Parse == null && raw == null && !Console.IsOutputRedirected;
        var cur = sampler.Take();
        if (raw != null) PerfSampler.WriteSample(raw, cur);
        var load = new LoadAverage();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit = true; };
        if (o.Parse != null) Console.WriteLine("RESET");
        for (var n = 0; n < o.Samples && !quit; n++)
        {
            if (PerfSampler.Sleep(o.Interval, interactive ? k => Key(k, o) : null) && quit) break;
            var prev = cur;
            cur = sampler.Take();
            if (raw != null) { PerfSampler.WriteSample(raw, cur); raw.Flush(); }
            else Show(o, prev, cur, load, interactive);
        }
        raw?.Dispose();
        return 0;
    }

    static volatile bool quit;

    static bool Key(ConsoleKeyInfo k, Options o)
    {
        switch (k.KeyChar)
        {
            case 'q': quit = true; return true;
            case 'g' or 'm' or 'd' or 'n' or 's' or 'v': o.View = k.KeyChar; return true;
            case 'c': o.CommandLine = !o.CommandLine; return true;
            case 'C' or 'M' or 'D' or 'N' or 'A': o.Sort = k.KeyChar; return true;
            case 'a': o.All = !o.All; return true;
            case '1': o.PerSecond = !o.PerSecond; return true;
        }
        return false;
    }

    static int Replay(Options o)
    {
        if (!File.Exists(o.ReadFile)) throw new IOException($"{o.ReadFile}: No such file or directory");
        var samples = PerfSampler.ReadSamples(o.ReadFile);
        if (samples.Count == 0) throw new InvalidOperationException($"{o.ReadFile}: no samples recorded");
        var load = new LoadAverage();
        if (o.Parse != null) Console.WriteLine("RESET");
        // The first record of a recording is its baseline; every later one is shown against its predecessor.
        for (var i = 1; i < samples.Count; i++)
        {
            var tod = samples[i].Time.TimeOfDay;
            if ((o.Begin == null || tod >= o.Begin) && (o.End == null || tod <= o.End))
                Show(o, samples[i - 1], samples[i], load, false);
        }
        return 0;
    }

    sealed class LoadAverage
    {
        public double L1 = -1, L5, L15;
        public void Add(double runnable, double seconds)
        {
            if (L1 < 0) { L1 = L5 = L15 = runnable; return; }
            L1 = runnable + (L1 - runnable) * Math.Exp(-seconds / 60);
            L5 = runnable + (L5 - runnable) * Math.Exp(-seconds / 300);
            L15 = runnable + (L15 - runnable) * Math.Exp(-seconds / 900);
        }
    }

    // ---- screen output ----

    static void Show(Options o, PerfSampler.Sample a, PerfSampler.Sample b, LoadAverage load, bool interactive)
    {
        var secs = Math.Max(a.Seconds(b), 0.001);
        var runnable = b.Procs.Values.Where(p => p.Pid != 0).Sum(p => p.Running);
        load.Add(Math.Max(0, runnable - 1), secs);   // minus atop's own sampling thread
        if (o.Parse != null) { Parseable(o, a, b, secs, load); return; }

        var sb = new StringBuilder();
        if (interactive) sb.Append("\x1b[H\x1b[2J");
        var w = interactive ? Math.Max(80, Console.WindowWidth - 1) : 140;
        sb.AppendLine($"ATOP - {Environment.MachineName,-16}{b.Time:yyyy/MM/dd  HH:mm:ss}{new string(' ', 8)}{new string('-', 16)}{new string(' ', 8)}{secs,6:F0}s elapsed");

        var procs = b.Procs.Values.Where(p => p.Pid != 0).ToList();
        long sysTicks = 0, userTicks = 0;
        foreach (var p in procs)
        {
            a.Procs.TryGetValue(p.Pid, out var old);
            if (old != null && old.CreateTime != p.CreateTime) old = null;
            sysTicks += p.Kernel - (old?.Kernel ?? 0);
            userTicks += p.User - (old?.User ?? 0);
        }
        var exited = a.Procs.Keys.Count(pid => !b.Procs.ContainsKey(pid));
        sb.AppendLine(Line("PRC", w, $"sys {sysTicks / 1e7,7:F2}s", $"user {userTicks / 1e7,6:F2}s", $"#proc {procs.Count,6}",
            $"#trun {runnable,6}", $"#tslpi {b.Mem.ThreadCount - runnable,5}", $"#zombie {0,4}", $"#exit {exited,6}"));

        var n = b.Cpus.Length;
        var all = Math.Max(1, (b.Total.All - a.Total.All) / (double)n);
        string P(long d) => $"{100.0 * d / all,5:F0}%";
        sb.AppendLine(Line("CPU", w, $"sys {P(b.Total.Kernel - a.Total.Kernel)}", $"user {P(b.Total.User - a.Total.User)}",
            $"irq {P(b.Total.Interrupt - a.Total.Interrupt + b.Total.Dpc - a.Total.Dpc)}", $"idle {P(b.Total.Idle - a.Total.Idle)}",
            $"wait {0,4}%", $"curf {PerfSampler.FrequencyMhz(b, "_Total") / 1000,4:F2}GHz"));
        var cpuRows = Enumerable.Range(0, n).Select(i => (i, busy: PerfSampler.CpuBusy(a.Cpus[i], b.Cpus[i])))
            .Where(x => o.All || x.busy >= 1).OrderByDescending(x => x.busy).Take(interactive ? 4 : n).OrderBy(x => x.i).ToList();
        foreach (var (i, _) in cpuRows)
        {
            var ca = a.Cpus[i]; var cb = b.Cpus[i];
            var tot = Math.Max(1, cb.All - ca.All);
            string Q(long d) => $"{100.0 * d / tot,5:F0}%";
            sb.AppendLine(Line("cpu", w, $"sys {Q(cb.Kernel - ca.Kernel)}", $"user {Q(cb.User - ca.User)}",
                $"irq {Q(cb.Interrupt - ca.Interrupt + cb.Dpc - ca.Dpc)}", $"idle {Q(cb.Idle - ca.Idle)}", $"cpu{i:D3} w  0%",
                $"curf {PerfSampler.FrequencyMhz(b, PerfSampler.CpuInstance(i)) / 1000,4:F2}GHz"));
        }
        var csw = b.Counter(@"\System\Context Switches/sec");
        sb.AppendLine(Line("CPL", w, $"numcpu {n,5}", $"avg1 {load.L1,7:F2}", $"avg5 {load.L5,7:F2}", $"avg15 {load.L15,6:F2}",
            $"csw {Num(double.IsNaN(csw) ? 0 : csw * secs),8}", $"intr {Num(b.Total.Interrupts - a.Total.Interrupts),7}"));

        var m = b.Mem;
        sb.AppendLine(Line("MEM", w, $"tot {Size(m.Total),8}", $"free {Size(m.Available),7}", $"cache {Size(m.Cached),6}",
            $"buff {Size(0),7}", $"slab {Size(m.KernelPaged + m.KernelNonPaged),7}", $"numnode {1,4}"));
        sb.AppendLine(Line("SWP", w, $"tot {Size(m.SwapTotal),8}", $"free {Size(m.SwapTotal - m.SwapUsed),7}",
            $"vmcom {Size(m.CommitTotal),6}", $"vmlim {Size(m.CommitLimit),6}"));
        double Rate(string c) { var v = b.Counter(c); return double.IsNaN(v) ? 0 : v * secs; }
        sb.AppendLine(Line("PAG", w, $"faults {Num(Rate(@"\Memory\Page Faults/sec")),6}", $"pgin {Num(Rate(@"\Memory\Pages Input/sec")),7}",
            $"pgout {Num(Rate(@"\Memory\Pages Output/sec")),6}", $"swin {Num(Rate(@"\Memory\Page Reads/sec")),7}", $"swout {Num(Rate(@"\Memory\Page Writes/sec")),6}"));

        foreach (var d in Disks(b))
        {
            var reads = d.Reads * secs; var writes = d.Writes * secs;
            sb.AppendLine(Line("DSK", w, $"{d.Name,12}", $"busy {d.Busy,6:F0}%", $"read {Num(reads),7}", $"write {Num(writes),6}",
                $"KiB/r {(d.Reads > 0 ? d.ReadBytes / d.Reads / 1024 : 0),6:F0}", $"KiB/w {(d.Writes > 0 ? d.WriteBytes / d.Writes / 1024 : 0),6:F0}",
                $"MBr/s {d.ReadBytes / 1048576,6:F1}", $"MBw/s {d.WriteBytes / 1048576,6:F1}", $"avio {d.Avio,4:F1} ms"));
        }
        sb.AppendLine(Line("NET", w, "transport   ", $"tcpi {Num(Rate(@"\TCPv4\Segments Received/sec")),7}",
            $"tcpo {Num(Rate(@"\TCPv4\Segments Sent/sec")),7}", $"udpi {Num(Rate(@"\UDPv4\Datagrams Received/sec")),7}",
            $"udpo {Num(Rate(@"\UDPv4\Datagrams Sent/sec")),7}", $"tcprs {Num(Rate(@"\TCPv4\Segments Retransmitted/sec")),6}"));
        sb.AppendLine(Line("NET", w, "network     ", $"ipi {Num(Rate(@"\IPv4\Datagrams Received/sec")),8}",
            $"ipo {Num(Rate(@"\IPv4\Datagrams Sent/sec")),8}", $"ipfrw {Num(Rate(@"\IPv4\Datagrams Forwarded/sec")),6}"));
        foreach (var nb in b.Net.Values.Where(x => x.Up))
        {
            a.Net.TryGetValue(nb.Name, out var na);
            long D(Func<PerfSampler.NetIf, long> f) => f(nb) - (na == null ? 0 : f(na));
            var name = nb.Name.Length > 8 ? nb.Name[..8] : nb.Name;
            sb.AppendLine(Line("NET", w, $"{name,-8} {Pct(nb),3}%", $"pcki {Num(D(x => x.InPackets)),7}", $"pcko {Num(D(x => x.OutPackets)),7}",
                $"sp {Speed(nb.Speed),7}", $"si {Speed(D(x => x.InBytes) * 8 / secs),7}", $"so {Speed(D(x => x.OutBytes) * 8 / secs),7}",
                $"erri {Num(D(x => x.InErrors)),6}", $"erro {Num(D(x => x.OutErrors)),6}", $"drpo {Num(D(x => x.OutDrops)),6}"));
            int Pct(PerfSampler.NetIf x) => x.Speed > 0 ? (int)Math.Min(100, (D(y => y.InBytes) + D(y => y.OutBytes)) * 8 / secs * 100 / x.Speed) : 0;
        }
        sb.AppendLine();
        ProcessList(sb, o, a, b, secs, w, interactive);
        Console.Write(sb.ToString());
    }

    static string Line(string label, int width, params string[] fields)
    {
        var sb = new StringBuilder(label).Append(" |");
        foreach (var f in fields)
        {
            if (sb.Length + f.Length + 3 > width) break;
            sb.Append(' ').Append(f).Append(" |");
        }
        return sb.ToString();
    }

    static string Num(double v) => v >= 1e7 ? $"{v / 1e6:F0}e6" : v >= 1e5 ? $"{v / 1e3:F0}e3" : $"{v:F0}";

    static string Size(long bytes)
    {
        double v = bytes;
        if (v >= 1L << 40) return $"{v / (1L << 40):F1}T";
        if (v >= 1L << 30) return $"{v / (1L << 30):F1}G";
        if (v >= 1L << 20) return $"{v / (1L << 20):F1}M";
        return $"{v / 1024:F0}K";
    }

    static string Speed(double bitsPerSec)
    {
        if (bitsPerSec >= 1e9) return $"{bitsPerSec / 1e9:F0} Gbps";
        if (bitsPerSec >= 1e6) return $"{bitsPerSec / 1e6:F0} Mbps";
        return $"{bitsPerSec / 1e3:F0} Kbps";
    }

    sealed record Disk(string Name, double Reads, double Writes, double ReadBytes, double WriteBytes, double Busy, double Avio, double Queue);

    // Per-disk rates from the PhysicalDisk counters; instance "0 C:" becomes disk0.
    static List<Disk> Disks(PerfSampler.Sample s)
    {
        var list = new List<Disk>();
        foreach (var inst in s.Instances(@"\PhysicalDisk(*)\Disk Reads/sec").Keys.Where(k => k != "_Total").Order())
        {
            double C(string c) { var v = s.Counter(@"\PhysicalDisk(*)\" + c, inst); return double.IsNaN(v) ? 0 : v; }
            var name = "disk" + inst.Split(' ')[0];
            list.Add(new Disk(name, C("Disk Reads/sec"), C("Disk Writes/sec"), C("Disk Read Bytes/sec"), C("Disk Write Bytes/sec"),
                Math.Clamp(100 - C("% Idle Time"), 0, 100), C("Avg. Disk sec/Transfer") * 1000, C("Avg. Disk Queue Length")));
        }
        return list;
    }

    sealed record Row(PerfSampler.Proc P, double Sys, double Usr, long Rd, long Wr, long Rops, long Wops, long Vgrow, long Rgrow, long Faults, long Csw, double Cpu, bool New);

    static void ProcessList(StringBuilder sb, Options o, PerfSampler.Sample a, PerfSampler.Sample b, double secs, int width, bool interactive)
    {
        var rows = new List<Row>();
        foreach (var p in b.Procs.Values.Where(p => p.Pid != 0))
        {
            a.Procs.TryGetValue(p.Pid, out var q);
            if (q != null && q.CreateTime != p.CreateTime) q = null;
            var cpu = PerfSampler.ProcCpu(q, p, secs) / b.Cpus.Length;
            rows.Add(new Row(p, (p.Kernel - (q?.Kernel ?? 0)) / 1e7, (p.User - (q?.User ?? 0)) / 1e7,
                p.ReadBytes - (q?.ReadBytes ?? 0), p.WriteBytes - (q?.WriteBytes ?? 0), p.ReadOps - (q?.ReadOps ?? 0), p.WriteOps - (q?.WriteOps ?? 0),
                p.Virtual - (q?.Virtual ?? 0), p.WorkingSet - (q?.WorkingSet ?? 0), p.PageFaults - (q?.PageFaults ?? 0),
                p.ContextSwitches - (q?.ContextSwitches ?? 0), cpu, q == null && a.Procs.Count > 0));
        }
        var sort = o.Sort == 'A' ? o.View switch { 'm' => 'M', 'd' => 'D', _ => 'C' } : o.Sort;
        IEnumerable<Row> sorted = sort switch
        {
            'M' => rows.OrderByDescending(r => r.P.WorkingSet),
            'D' => rows.OrderByDescending(r => r.Rd + r.Wr),
            _ => rows.OrderByDescending(r => r.Cpu).ThenByDescending(r => r.P.WorkingSet),
        };
        if (!o.All) sorted = sorted.Where(r => r.Sys + r.Usr > 0 || r.Rd + r.Wr > 0 || r.Rgrow != 0 || sort == 'M');
        var list = sorted.Take(interactive ? Math.Max(5, Console.WindowHeight - 22) : int.MaxValue).ToList();
        var totalMem = (double)Math.Max(1, b.Mem.Total);
        var diskTotal = Math.Max(1, rows.Sum(r => r.Rd + r.Wr));
        string Cmd(Row r) => o.CommandLine ? PerfSampler.CommandLine(r.P.Pid) ?? r.P.Name : Path.GetFileNameWithoutExtension(r.P.Name);
        string Cut(string s) => s.Length > width ? s[..width] : s;
        switch (o.View)
        {
            case 'm':
                sb.AppendLine("    PID  MINFLT  VSIZE  RSIZE  PSIZE   VGROW   RGROW  RUID        ST  MEM  CMD");
                foreach (var r in list)
                    sb.AppendLine(Cut($"{r.P.Pid,7} {Num(r.Faults),7} {Size(r.P.Virtual),6} {Size(r.P.WorkingSet),6} {Size(r.P.Private),6} {Size(r.Vgrow),7} {Size(r.Rgrow),7}  {User(r.P),-10} {(r.New ? "N-" : "--")} {100 * r.P.WorkingSet / totalMem,3:F0}%  {Cmd(r)}"));
                break;
            case 'd':
                sb.AppendLine("    PID    RDDSK    WRDSK   RDOPS   WROPS   DSK  CMD");
                foreach (var r in list)
                    sb.AppendLine(Cut($"{r.P.Pid,7} {Size(r.Rd),8} {Size(r.Wr),8} {Num(r.Rops),7} {Num(r.Wops),7}  {100.0 * (r.Rd + r.Wr) / diskTotal,3:F0}%  {Cmd(r)}"));
                break;
            case 's':
                sb.AppendLine("    PID  TRUN  THR  HANDLES     CSW   PRI  S   CPU  CMD");
                foreach (var r in list)
                    sb.AppendLine(Cut($"{r.P.Pid,7} {r.P.Running,5} {r.P.Threads,4} {r.P.Handles,8} {Num(r.Csw),7} {r.P.Priority,5}  {(r.P.Running > 0 ? 'R' : 'S')} {r.Cpu,4:F0}%  {Cmd(r)}"));
                break;
            case 'n':
                sb.AppendLine("Per-process network counters are not kept by Windows; use the NET lines above (or Resource Monitor).");
                break;
            case 'v':
                sb.AppendLine("    PID   PPID  RUID         SESSION  ST  CMD");
                foreach (var r in list)
                    sb.AppendLine(Cut($"{r.P.Pid,7} {r.P.ParentPid,6}  {User(r.P),-12} {r.P.Session,7}  {(r.New ? "N-" : "--")}  {Cmd(r)}"));
                break;
            default:
                sb.AppendLine("    PID SYSCPU  USRCPU   VGROW   RGROW   RDDSK   WRDSK  RUID        ST  THR  S   CPU  CMD");
                foreach (var r in list)
                    sb.AppendLine(Cut($"{r.P.Pid,7} {r.Sys,5:F2}s {r.Usr,6:F2}s {Size(r.Vgrow),7} {Size(r.Rgrow),7} {Size(r.Rd),7} {Size(r.Wr),7}  {User(r.P),-10} {(r.New ? "N-" : "--")} {r.P.Threads,4}  {(r.P.Running > 0 ? 'R' : 'S')} {r.Cpu,4:F0}%  {Cmd(r)}"));
                break;
        }
    }

    static string User(PerfSampler.Proc p)
    {
        var u = PerfSampler.Owner(p.Pid).User;
        return u.Length > 10 ? u[..10] : u;
    }

    // ---- parseable output (-P) ----

    static void Parseable(Options o, PerfSampler.Sample a, PerfSampler.Sample b, double secs, LoadAverage load)
    {
        var epoch = new DateTimeOffset(b.Time).ToUnixTimeSeconds();
        var head = $"{Environment.MachineName} {epoch} {b.Time:yyyy/MM/dd HH:mm:ss} {secs:F0}";
        const int hz = 100;   // atop reports clock ticks; 1 tick = 10 ms
        long T(long hundredNs) => hundredNs / 100000;
        var sb = new StringBuilder();
        var n = b.Cpus.Length;
        var freq = PerfSampler.FrequencyMhz(b, "_Total");
        if (o.Parse.Contains("CPU"))
            sb.AppendLine($"CPU {head} {hz} {n} {T(b.Total.Kernel - a.Total.Kernel)} {T(b.Total.User - a.Total.User)} 0 {T(b.Total.Idle - a.Total.Idle)} 0 {T(b.Total.Interrupt - a.Total.Interrupt)} {T(b.Total.Dpc - a.Total.Dpc)} 0 0 {freq:F0} {freq * 100 / Math.Max(1, PerfSampler.BaseMhz):F0} 0 0");
        if (o.Parse.Contains("cpu"))
            for (var i = 0; i < n; i++)
            {
                var ca = a.Cpus[i]; var cb = b.Cpus[i];
                var f = PerfSampler.FrequencyMhz(b, PerfSampler.CpuInstance(i));
                sb.AppendLine($"cpu {head} {hz} {i} {T(cb.Kernel - ca.Kernel)} {T(cb.User - ca.User)} 0 {T(cb.Idle - ca.Idle)} 0 {T(cb.Interrupt - ca.Interrupt)} {T(cb.Dpc - ca.Dpc)} 0 0 {f:F0} {f * 100 / Math.Max(1, PerfSampler.BaseMhz):F0} 0 0");
            }
        double Rate(string c) { var v = b.Counter(c); return double.IsNaN(v) ? 0 : v * secs; }
        if (o.Parse.Contains("CPL"))
            sb.AppendLine($"CPL {head} {n} {load.L1:F2} {load.L5:F2} {load.L15:F2} {Rate(@"\System\Context Switches/sec"):F0} {b.Total.Interrupts - a.Total.Interrupts}");
        var m = b.Mem;
        const long page = 4096;
        if (o.Parse.Contains("MEM"))
            sb.AppendLine($"MEM {head} {page} {m.Total / page} {m.Available / page} {m.Cached / page} 0 {(m.KernelPaged + m.KernelNonPaged) / page} 0 0 0 0 0 0 0 0 0 0");
        if (o.Parse.Contains("SWP"))
            sb.AppendLine($"SWP {head} {page} {m.SwapTotal / page} {(m.SwapTotal - m.SwapUsed) / page} 0 {m.CommitTotal / page} {m.CommitLimit / page} 0 0");
        if (o.Parse.Contains("PAG"))
            sb.AppendLine($"PAG {head} {page} 0 0 {Rate(@"\Memory\Page Faults/sec"):F0} 0 {Rate(@"\Memory\Page Reads/sec"):F0} {Rate(@"\Memory\Page Writes/sec"):F0} 0 0 0");
        if (o.Parse.Contains("DSK"))
            foreach (var d in Disks(b))
                sb.AppendLine($"DSK {head} {d.Name} {d.Busy * secs * 10:F0} {d.Reads * secs:F0} {d.ReadBytes * secs / 512:F0} {d.Writes * secs:F0} {d.WriteBytes * secs / 512:F0} 0 0 0 0");
        if (o.Parse.Contains("NET"))
        {
            sb.AppendLine($"NET {head} upper {Rate(@"\TCPv4\Segments Received/sec"):F0} {Rate(@"\TCPv4\Segments Sent/sec"):F0} {Rate(@"\UDPv4\Datagrams Received/sec"):F0} {Rate(@"\UDPv4\Datagrams Sent/sec"):F0} {Rate(@"\IPv4\Datagrams Received/sec"):F0} {Rate(@"\IPv4\Datagrams Sent/sec"):F0} 0 {Rate(@"\IPv4\Datagrams Forwarded/sec"):F0} 0 0");
            foreach (var nb in b.Net.Values)
            {
                a.Net.TryGetValue(nb.Name, out var na);
                long D(Func<PerfSampler.NetIf, long> f) => f(nb) - (na == null ? 0 : f(na));
                sb.AppendLine($"NET {head} {nb.Name} {D(x => x.InPackets)} {D(x => x.InBytes)} {D(x => x.OutPackets)} {D(x => x.OutBytes)} {nb.Speed / 1000000} 2");
            }
        }
        var wantProc = o.Parse.Overlaps(["PRG", "PRC", "PRM", "PRD", "PRN", "PRE"]);
        if (wantProc)
        {
            foreach (var p in b.Procs.Values.Where(p => p.Pid != 0).OrderBy(p => p.Pid))
            {
                a.Procs.TryGetValue(p.Pid, out var q);
                if (q != null && q.CreateTime != p.CreateTime) q = null;
                var name = $"({Path.GetFileNameWithoutExtension(p.Name)})";
                var st = p.Running > 0 ? "R" : "S";
                var (_, uid) = PerfSampler.Owner(p.Pid);
                if (o.Parse.Contains("PRG"))
                    sb.AppendLine($"PRG {head} {p.Pid} {name} {st} {uid} {uid} {p.Pid} {p.Threads} 0 {DateTime.FromFileTime(p.CreateTime).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds:F0} ({PerfSampler.CommandLine(p.Pid) ?? p.Name}) {p.ParentPid} {uid} {uid} {uid} {uid} {uid} {uid} {p.Running} {p.Threads - p.Running} 0 0 n");
                if (o.Parse.Contains("PRC"))
                    sb.AppendLine($"PRC {head} {p.Pid} {name} {st} {hz} {T(p.User - (q?.User ?? 0))} {T(p.Kernel - (q?.Kernel ?? 0))} 0 {p.Priority} 0 0 0 0 0 y");
                if (o.Parse.Contains("PRM"))
                    sb.AppendLine($"PRM {head} {p.Pid} {name} {st} {page} {p.Virtual / 1024} {p.WorkingSet / 1024} 0 {(p.Virtual - (q?.Virtual ?? 0)) / 1024} {(p.WorkingSet - (q?.WorkingSet ?? 0)) / 1024} {p.PageFaults - (q?.PageFaults ?? 0)} 0 0 0 0 0 0 0 y");
                if (o.Parse.Contains("PRD"))
                    sb.AppendLine($"PRD {head} {p.Pid} {name} {st} n y {p.ReadOps - (q?.ReadOps ?? 0)} {(p.ReadBytes - (q?.ReadBytes ?? 0)) / 512} {p.WriteOps - (q?.WriteOps ?? 0)} {(p.WriteBytes - (q?.WriteBytes ?? 0)) / 512} 0 y");
            }
        }
        sb.AppendLine("SEP");
        Console.Write(sb.ToString());
    }

    static void Help() => Console.WriteLine("""
        Usage: atop [-flags] [interval [samples]]
                        or
        Usage: atop -w  file  [-S] [-a] [interval [samples]]
               atop -r [file] [-b [YYYYMMDD]hh:mm] [-e [YYYYMMDD]hh:mm] [-flags]

        System and process monitor for Windows (performance counters and NtQuerySystemInformation).

        	generic flags:
        	  -V  show version information
        	  -a  show or log all processes (i.s.o. active processes only)
        	  -P  generate parseable output for specified label(s): CPU,cpu,CPL,MEM,SWP,PAG,DSK,NET,PRG,PRC,PRM,PRD,ALL
        	  -1  show average-per-second i.s.o. total values

        	specific flags for raw logfiles:
        	  -w  write raw data to   file (compressed)
        	  -r  read  raw data from file (compressed)
        	  -b  begin showing data from specified time
        	  -e  finish showing data after specified time

        	  interval: number of seconds   (minimum 0)
        	  samples:  number of intervals (minimum 1)

        	process views: -g generic  -m memory  -d disk  -s scheduling  -v various  -c command line
        	sort order:    -C cpu  -M memory  -D disk  -A automatic
        	interactive keys: same letters as the flags, q quits
        """);
}
