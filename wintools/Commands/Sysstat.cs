using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Sample = WinTools.PerfSampler.Sample;

namespace WinTools;

// sysstat: iostat, mpstat, pidstat and sar on top of the Windows performance data. Output follows
// sysstat 12's layout. Linux-only fields (iowait, steal, guest, merged requests) are always 0.00.
// sar's data files are recordings (same format as atop -w) written with `sar -o` and read with `sar -f`;
// nothing collects them in the background unless you schedule `sar -o <interval> <count>` yourself.
static class Sysstat
{
    const string Version = "12.8.0";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("iostat", "Report CPU and disk I/O statistics", Iostat);
        Commands.Add("mpstat", "Report per-processor statistics", Mpstat);
        Commands.Add("pidstat", "Report statistics for processes", Pidstat);
        Commands.Add("sar", "Collect, report, or save system activity information", Sar);
    }

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static string Clock(DateTime t) => t.ToString("hh:mm:ss tt", Inv);
    static string F(double v, int dec = 2) => (double.IsNaN(v) ? 0 : v).ToString("F" + dec, Inv);

    static void PrintVersion(string tool) => Console.WriteLine($"sysstat version {Version} (wintools {tool} for Windows)\n(C) Sebastien Godard (sysstat <at> orange.fr)");

    // Interval and count positional arguments shared by all four tools.
    static (double Interval, int Count) Timing(List<string> pos, string tool)
    {
        if (pos.Count == 0) return (0, 1);
        if (!double.TryParse(pos[0], Inv, out var interval) || interval < 0) throw new UsageException($"invalid interval '{pos[0]}' (see {tool} --help)");
        var count = int.MaxValue;
        if (pos.Count > 1 && (!int.TryParse(pos[1], out count) || count < 1)) throw new UsageException($"invalid count '{pos[1]}'");
        if (pos.Count > 2) throw new UsageException($"too many arguments (see {tool} --help)");
        if (interval == 0) count = 1;
        return (interval, count);
    }

    static volatile bool stop;
    static void TrapCtrlC() => Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop = true; };
    static bool Wait(double seconds) { PerfSampler.Sleep(seconds); return stop; }

    // A zero sample at boot time: reports without an interval cover the time since boot.
    static Sample Boot(Sample now)
    {
        var uptime = Environment.TickCount64 / 1000.0;
        return new Sample
        {
            Time = now.Time.AddSeconds(-uptime),
            Stamp = now.Stamp - (long)(uptime * System.Diagnostics.Stopwatch.Frequency),
            Cpus = now.Cpus.Select(_ => new PerfSampler.CpuTicks()).ToArray(),
            Total = new PerfSampler.CpuTicks(),
            Mem = now.Mem,
            Procs = new(),
            Net = now.Net.ToDictionary(kv => kv.Key, kv => new PerfSampler.NetIf { Name = kv.Key }),
            Disks = now.Disks.ToDictionary(kv => kv.Key, kv => new PerfSampler.DiskRaw { Name = kv.Key, QueryTime = kv.Value.QueryTime - (long)(uptime * 1e7) }),
        };
    }

    // CPU percentages between two tick snapshots: usr nice sys iowait irq soft steal guest gnice idle.
    static double[] CpuPct(PerfSampler.CpuTicks a, PerfSampler.CpuTicks b)
    {
        double total = Math.Max(1, b.All - a.All);
        double P(long d) => Math.Clamp(100 * d / total, 0, 100);
        return [P(b.User - a.User), 0, P(b.Kernel - a.Kernel), 0, P(b.Interrupt - a.Interrupt), P(b.Dpc - a.Dpc), 0, 0, 0, P(b.Idle - a.Idle)];
    }

    // =====================================================================================
    // iostat
    // =====================================================================================

    static int Iostat(string[] args)
    {
        bool cpu = true, dev = true, ext = false, mb = false, human = false, stamp = false, skipFirst = false, omitIdle = false, json = false;
        var pos = new List<string>();
        var devices = new List<string>();
        var sawC = false; var sawD = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--help": IostatHelp(); return 0;
                case "-V" or "--version": PrintVersion("iostat"); return 0;
                case "-c": sawC = true; break;
                case "-d": sawD = true; break;
                case "-x": ext = true; break;
                case "-k": mb = false; break;
                case "-m": mb = true; break;
                case "-h" or "--human": human = true; break;
                case "-t": stamp = true; break;
                case "-y": skipFirst = true; break;
                case "-z": omitIdle = true; break;
                case "-N" or "-s" or "--compact": break;
                case "-o": if (++i >= args.Length || args[i] != "JSON") throw new UsageException("-o requires JSON"); json = true; break;
                case "-p": if (i + 1 < args.Length && !char.IsDigit(args[i + 1][0])) i++; break;   // partitions: whole disks only
                default:
                    if (a.StartsWith("--dec=")) break;
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a} (see iostat --help)");
                    if (char.IsDigit(a[0])) pos.Add(a); else devices.Add(a);
                    break;
            }
        }
        if (sawC || sawD) { cpu = sawC; dev = sawD; }
        var (interval, count) = Timing(pos, "iostat");
        TrapCtrlC();

        using var sampler = new PerfSampler(false);
        var cur = sampler.Take();
        var prev = Boot(cur);
        var unit = mb ? 1024.0 * 1024 : 1024.0;
        var u = mb ? "MB" : "kB";
        var jsonStats = new List<string>();
        if (!json) Console.WriteLine(PerfSampler.SysstatHeader() + "\n");
        for (var n = 0; n < count; n++)
        {
            if (n > 0 || skipFirst)
            {
                if (interval <= 0) break;
                if (Wait(interval)) break;
                prev = cur;
                cur = sampler.Take();
            }
            var secs = Math.Max(prev.Seconds(cur), 0.001);
            var c = CpuPct(prev.Total, cur.Total);
            var system = c[2] + c[4] + c[5];   // iostat folds irq and softirq into %system
            var disks = cur.Disks.Values.Where(d => devices.Count == 0 || devices.Contains(d.Name)).OrderBy(d => d.Name, StringComparer.Ordinal).ToList();
            if (json)
            {
                var sb = new StringBuilder("{");
                if (stamp) sb.Append($"\"timestamp\": \"{cur.Time:MM/dd/yyyy hh:mm:ss tt}\", ");
                if (cpu) sb.Append($"\"avg-cpu\": {{\"user\": {F(c[0])}, \"nice\": 0.00, \"system\": {F(system)}, \"iowait\": 0.00, \"steal\": 0.00, \"idle\": {F(c[9])}}}");
                if (dev)
                {
                    if (cpu) sb.Append(", ");
                    sb.Append("\"disk\": [");
                    sb.Append(string.Join(", ", disks.Select(d =>
                    {
                        var x = DiskDelta(prev, d, secs);
                        return ext
                            ? $"{{\"disk_device\": {PerfSampler.Json(d.Name)}, \"r/s\": {F(x.R)}, \"w/s\": {F(x.W)}, \"rkB/s\": {F(x.Rb / 1024)}, \"wkB/s\": {F(x.Wb / 1024)}, \"r_await\": {F(x.RAwait)}, \"w_await\": {F(x.WAwait)}, \"aqu-sz\": {F(x.Queue)}, \"util\": {F(x.Util)}}}"
                            : $"{{\"disk_device\": {PerfSampler.Json(d.Name)}, \"tps\": {F(x.R + x.W)}, \"kB_read/s\": {F(x.Rb / 1024)}, \"kB_wrtn/s\": {F(x.Wb / 1024)}, \"kB_dscd/s\": 0.00, \"kB_read\": {x.RbTotal / 1024:F0}, \"kB_wrtn\": {x.WbTotal / 1024:F0}, \"kB_dscd\": 0}}";
                    })));
                    sb.Append(']');
                }
                jsonStats.Add(sb.Append('}').ToString());
                continue;
            }
            var o = new StringBuilder();
            if (stamp) o.AppendLine(cur.Time.ToString("MM/dd/yyyy hh:mm:ss tt", Inv));
            if (cpu)
            {
                o.AppendLine("avg-cpu:  %user   %nice %system %iowait  %steal   %idle");
                o.AppendLine($"         {F(c[0]),7} {F(0),7} {F(system),7} {F(0),7} {F(0),7} {F(c[9]),7}");
                o.AppendLine();
            }
            if (dev)
            {
                if (ext)
                    o.AppendLine("Device            r/s     rkB/s   rrqm/s  %rrqm r_await rareq-sz     w/s     wkB/s   wrqm/s  %wrqm w_await wareq-sz     d/s     dkB/s   drqm/s  %drqm d_await dareq-sz     f/s f_await  aqu-sz  %util".Replace("kB", u));
                else
                    o.AppendLine($"Device             tps    {u}_read/s    {u}_wrtn/s    {u}_dscd/s    {u}_read    {u}_wrtn    {u}_dscd");
                foreach (var d in disks)
                {
                    var x = DiskDelta(prev, d, secs);
                    if (omitIdle && x.R + x.W == 0) continue;
                    string V(double bytes, int w) => human ? Human(bytes).PadLeft(w) : F(bytes / unit).PadLeft(w);
                    if (ext)
                        o.AppendLine($"{d.Name,-12} {F(x.R),8} {V(x.Rb, 9)} {F(0),8} {F(0),6} {F(x.RAwait),7} {F(x.R > 0 ? x.Rb / x.R / 1024 : 0),8} {F(x.W),7} {V(x.Wb, 9)} {F(0),8} {F(0),6} {F(x.WAwait),7} {F(x.W > 0 ? x.Wb / x.W / 1024 : 0),8} {F(0),7} {F(0),9} {F(0),8} {F(0),6} {F(0),7} {F(0),8} {F(0),7} {F(0),7} {F(x.Queue),7} {F(x.Util),6}");
                    else
                        o.AppendLine($"{d.Name,-12} {F(x.R + x.W),8} {V(x.Rb, 12)} {V(x.Wb, 12)} {V(0, 12)} {(human ? Human(x.RbTotal) : (x.RbTotal / unit).ToString("F0", Inv)),10} {(human ? Human(x.WbTotal) : (x.WbTotal / unit).ToString("F0", Inv)),10} {0,10}");
                }
                o.AppendLine();
            }
            Console.Write(o.ToString());
        }
        if (json) PrintJson(jsonStats);
        return 0;
    }

    static string Human(double bytes)
    {
        if (bytes >= 1L << 30) return F(bytes / (1L << 30), 1) + "G";
        if (bytes >= 1L << 20) return F(bytes / (1L << 20), 1) + "M";
        if (bytes >= 1024) return F(bytes / 1024, 1) + "k";
        return F(bytes, 1) + "B";
    }

    sealed record DiskRates(double R, double W, double Rb, double Wb, double RbTotal, double WbTotal, double RAwait, double WAwait, double Queue, double Util);

    static DiskRates DiskDelta(Sample prev, PerfSampler.DiskRaw d, double secs)
    {
        prev.Disks.TryGetValue(d.Name, out var p);
        p ??= new PerfSampler.DiskRaw { QueryTime = d.QueryTime - (long)(secs * 1e7) };
        var r = PerfSampler.Count32(p.Reads, d.Reads);
        var w = PerfSampler.Count32(p.Writes, d.Writes);
        var rt = (d.ReadTime - p.ReadTime) / 1e4;   // ms
        var wt = (d.WriteTime - p.WriteTime) / 1e4;
        var elapsed = Math.Max(1, d.QueryTime - p.QueryTime);
        var util = Math.Clamp(100 - 100.0 * (d.IdleTime - p.IdleTime) / elapsed, 0, 100);
        return new DiskRates(r / secs, w / secs, (d.ReadBytes - p.ReadBytes) / secs, (d.WriteBytes - p.WriteBytes) / secs,
            d.ReadBytes - p.ReadBytes, d.WriteBytes - p.WriteBytes, r > 0 ? rt / r : 0, w > 0 ? wt / w : 0, (rt + wt) / (secs * 1000), util);
    }

    static void PrintJson(List<string> stats, string extra = null)
    {
        Console.WriteLine("{\"sysstat\": {");
        Console.WriteLine("\t\"hosts\": [");
        Console.WriteLine("\t\t{");
        Console.WriteLine($"\t\t\t\"nodename\": {PerfSampler.Json(Environment.MachineName)},");
        Console.WriteLine("\t\t\t\"sysname\": \"Windows\",");
        Console.WriteLine($"\t\t\t\"release\": \"{PerfSampler.OsVersion}\",");
        Console.WriteLine($"\t\t\t\"machine\": \"{PerfSampler.Arch}\",");
        Console.WriteLine($"\t\t\t\"number-of-cpus\": {Environment.ProcessorCount},");
        Console.WriteLine($"\t\t\t\"date\": \"{DateTime.Now:MM/dd/yyyy}\",");
        if (extra != null) Console.WriteLine(extra);
        Console.WriteLine("\t\t\t\"statistics\": [");
        Console.WriteLine(string.Join(",\n", stats.Select(s => "\t\t\t\t" + s)));
        Console.WriteLine("\t\t\t]");
        Console.WriteLine("\t\t}");
        Console.WriteLine("\t]");
        Console.WriteLine("}}");
    }

    static void IostatHelp() => Console.WriteLine("""
        Usage: iostat [ options ] [ <interval> [ <count> ] ]
        Options are:
        [ -c ] [ -d ] [ -h ] [ -k | -m ] [ -N ] [ -s ] [ -t ] [ -V ] [ -x ] [ -y ] [ -z ]
        [ -o JSON ] [ --dec={ 0 | 1 | 2 } ] [ --human ] [ <device> [...] ]

        Report CPU statistics and input/output statistics for physical disks (disk0, disk1, ...).
        The first report covers the time since boot; -y skips it.
        """);

    // =====================================================================================
    // mpstat
    // =====================================================================================

    static int Mpstat(string[] args)
    {
        var cpus = new List<int>();
        bool allCpus = false, util = false, intr = false, json = false;
        var pos = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"option {a} needs an argument");
            switch (a)
            {
                case "-h" or "--help": MpstatHelp(); return 0;
                case "-V" or "--version": PrintVersion("mpstat"); return 0;
                case "-u": util = true; break;
                case "-A": util = intr = true; allCpus = true; break;
                case "-I": Next(); intr = true; break;
                case "-P": ParseCpuList(Next(), cpus, ref allCpus); break;
                case "-o": if (Next() != "JSON") throw new UsageException("-o requires JSON"); json = true; break;
                case "-N" or "-n" or "-T": if (a == "-N") Next(); break;
                default:
                    if (a.StartsWith("--dec=")) break;
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a} (see mpstat --help)");
                    pos.Add(a);
                    break;
            }
        }
        if (!intr) util = true;
        var (interval, count) = Timing(pos, "mpstat");
        TrapCtrlC();
        using var sampler = new PerfSampler(false);
        var first = sampler.Take();
        var rows = new List<(DateTime Time, Sample A, Sample B)>();
        if (!json) Console.WriteLine(PerfSampler.SysstatHeader());
        if (interval <= 0) rows.Add((first.Time, Boot(first), first));
        else
        {
            var prev = first;
            for (var n = 0; n < count && !Wait(interval); n++)
            {
                var cur = sampler.Take();
                rows.Add((cur.Time, prev, cur));
                if (!json) PrintMpstat(Clock(prev.Time), Clock(cur.Time), prev, cur, cpus, allCpus, util, intr, n == 0);
                prev = cur;
            }
            if (!json && rows.Count > 0) PrintMpstat("Average:   ", "Average:   ", first, rows[^1].B, cpus, allCpus, util, intr, false);
        }
        if (json)
        {
            var stats = rows.Select(r =>
            {
                var list = SelectedCpus(r.B, cpus, allCpus);
                var parts = new List<string>();
                if (util)
                    parts.Add("\"cpu-load\": [" + string.Join(", ", list.Select(i =>
                    {
                        var c = i < 0 ? CpuPct(r.A.Total, r.B.Total) : CpuPct(r.A.Cpus[i], r.B.Cpus[i]);
                        return $"{{\"cpu\": \"{(i < 0 ? "all" : i.ToString())}\", \"usr\": {F(c[0])}, \"nice\": 0.00, \"sys\": {F(c[2])}, \"iowait\": 0.00, \"irq\": {F(c[4])}, \"soft\": {F(c[5])}, \"steal\": 0.00, \"guest\": 0.00, \"gnice\": 0.00, \"idle\": {F(c[9])}}}";
                    })) + "]");
                if (intr)
                {
                    var secs = Math.Max(r.A.Seconds(r.B), 0.001);
                    parts.Add("\"sum-interrupts\": [" + string.Join(", ", list.Select(i =>
                        $"{{\"cpu\": \"{(i < 0 ? "all" : i.ToString())}\", \"intr\": {F(((i < 0 ? r.B.Total.Interrupts - r.A.Total.Interrupts : r.B.Cpus[i].Interrupts - r.A.Cpus[i].Interrupts)) / secs)}}}")) + "]");
                }
                return $"{{\"timestamp\": \"{r.Time:HH:mm:ss}\", " + string.Join(", ", parts) + "}";
            }).ToList();
            PrintJson(stats);
            return 0;
        }
        if (interval <= 0) PrintMpstat(Clock(first.Time), Clock(first.Time), rows[0].A, rows[0].B, cpus, allCpus, util, intr, true);
        return 0;
    }

    static void ParseCpuList(string spec, List<int> cpus, ref bool all)
    {
        foreach (var part in spec.Split(','))
        {
            if (part.Equals("ALL", StringComparison.OrdinalIgnoreCase)) { all = true; continue; }
            if (part.Equals("ON", StringComparison.OrdinalIgnoreCase)) { all = true; continue; }
            var range = part.Split('-');
            if (!int.TryParse(range[0], out var lo) || lo < 0 || lo >= Environment.ProcessorCount) throw new UsageException($"not a valid CPU: {part}");
            var hi = range.Length > 1 && int.TryParse(range[1], out var h) ? Math.Min(h, Environment.ProcessorCount - 1) : lo;
            for (var c = lo; c <= hi; c++) cpus.Add(c);
        }
    }

    // -1 stands for "all".
    static List<int> SelectedCpus(Sample s, List<int> cpus, bool all)
    {
        var list = new List<int> { -1 };
        if (all) list.AddRange(Enumerable.Range(0, s.Cpus.Length));
        else list.AddRange(cpus.Where(c => c < s.Cpus.Length));
        return list.Distinct().ToList();
    }

    // One interval's rows; the column header repeats every interval when more than one row is shown.
    static void PrintMpstat(string hdr, string t, Sample a, Sample b, List<int> cpus, bool all, bool util, bool intr, bool first)
    {
        var list = SelectedCpus(b, cpus, all);
        var header = first || list.Count > 1 || (util && intr);
        var sb = new StringBuilder();
        if (util)
        {
            if (header) sb.AppendLine().AppendLine($"{hdr}  CPU    %usr   %nice    %sys %iowait    %irq   %soft  %steal  %guest  %gnice   %idle");
            foreach (var i in list)
            {
                var c = i < 0 ? CpuPct(a.Total, b.Total) : CpuPct(a.Cpus[i], b.Cpus[i]);
                sb.AppendLine($"{t}  {(i < 0 ? "all" : i.ToString()),3} {string.Join(' ', c.Select(v => F(v).PadLeft(7)))}");
            }
        }
        if (intr)
        {
            var secs = Math.Max(a.Seconds(b), 0.001);
            if (header) sb.AppendLine().AppendLine($"{hdr}  CPU    intr/s");
            foreach (var i in list)
            {
                var d = i < 0 ? b.Total.Interrupts - a.Total.Interrupts : b.Cpus[i].Interrupts - a.Cpus[i].Interrupts;
                sb.AppendLine($"{t}  {(i < 0 ? "all" : i.ToString()),3} {F(d / secs),9}");
            }
        }
        Console.Write(sb.ToString());
    }

    static void MpstatHelp() => Console.WriteLine("""
        Usage: mpstat [ options ] [ <interval> [ <count> ] ]
        Options are:
        [ -A ] [ -n ] [ -T ] [ -u ] [ -V ]
        [ -I { SUM | CPU | ALL } ] [ -N { <node_list> | ALL } ]
        [ --dec={ 0 | 1 | 2 } ] [ -o JSON ] [ -P { <cpu_list> | ALL } ]

        Report processor statistics. Without an interval the report covers the time since boot.
        """);

    // =====================================================================================
    // pidstat
    // =====================================================================================

    static int Pidstat(string[] args)
    {
        bool cpu = false, mem = false, disk = false, csw = false, various = false, cmdline = false, userName = false, horizontal = false;
        string userFilter = null;
        Regex comm = null;
        var pids = new List<int>();
        bool self = false, allPids = false;
        var pos = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"option {a} needs an argument");
            switch (a)
            {
                case "--help": PidstatHelp(); return 0;
                case "-V" or "--version": PrintVersion("pidstat"); return 0;
                case "-u": cpu = true; break;
                case "-r": mem = true; break;
                case "-d": disk = true; break;
                case "-w": csw = true; break;
                case "-v": various = true; break;
                case "-l": cmdline = true; break;
                case "-h": horizontal = true; break;
                case "-I" or "-H" or "-R" or "-s": break;
                case "-t": throw new InvalidOperationException("per-thread statistics (-t) are not available on Windows");
                case "-U":
                    userName = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-') && !char.IsDigit(args[i + 1][0])) userFilter = args[++i];
                    break;
                case "-C": comm = new Regex(Next(), RegexOptions.IgnoreCase); break;
                case "-G": comm = new Regex(Next(), RegexOptions.IgnoreCase); break;
                case "-T": Next(); break;
                case "-p":
                    foreach (var p in Next().Split(','))
                    {
                        if (p == "SELF") self = true;
                        else if (p == "ALL") allPids = true;
                        else if (int.TryParse(p, out var pid)) pids.Add(pid);
                        else throw new UsageException($"invalid PID '{p}'");
                    }
                    break;
                default:
                    if (a.StartsWith("--dec=") || a == "--human") break;
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a} (see pidstat --help)");
                    pos.Add(a);
                    break;
            }
        }
        if (!(mem || disk || csw || various)) cpu = true;
        if (self) pids.Add(Environment.ProcessId);
        var (interval, count) = Timing(pos, "pidstat");
        TrapCtrlC();

        using var sampler = new PerfSampler(true);
        var first = sampler.Take();
        Console.WriteLine(PerfSampler.SysstatHeader());
        var selector = new PidSelector(pids, allPids, comm, userFilter);
        var view = new PidView(cpu, mem, disk, csw, various, cmdline, userName, horizontal);
        if (interval <= 0)
        {
            view.Print(Clock(first.Time), null, first, 0, selector, true);
            return 0;
        }
        var prev = first;
        Sample last = null;
        for (var n = 0; n < count && !Wait(interval); n++)
        {
            last = sampler.Take();
            view.Print(Clock(last.Time), prev, last, prev.Seconds(last), selector, n == 0 || !horizontal);
            prev = last;
        }
        if (last != null && !horizontal) view.Print("Average:   ", first, last, first.Seconds(last), selector, true);
        return 0;
    }

    sealed class PidSelector(List<int> pids, bool all, Regex comm, string user)
    {
        public bool Explicit => pids.Count > 0 || all;
        public bool Match(PerfSampler.Proc p)
        {
            if (p.Pid == 0) return false;
            if (pids.Count > 0 && !pids.Contains(p.Pid)) return false;
            if (comm != null && !comm.IsMatch(p.Name)) return false;
            if (user != null && !PerfSampler.Owner(p.Pid).User.Equals(user, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
        public bool ShowAll => all || pids.Count > 0;
    }

    sealed class PidView(bool cpu, bool mem, bool disk, bool csw, bool various, bool cmdline, bool userName, bool horizontal)
    {
        // prev == null: totals since each process started.
        public void Print(string time, Sample prev, Sample cur, double secs, PidSelector sel, bool header)
        {
            var now = DateTime.Now.ToFileTime();
            var ncpu = cur.Cpus.Length;
            var total = (double)Math.Max(1, cur.Mem.Total);
            var rows = new List<(PerfSampler.Proc P, Dictionary<string, string> V, bool Active)>();
            foreach (var p in cur.Procs.Values.Where(sel.Match).OrderBy(p => p.Pid))
            {
                PerfSampler.Proc q = null;
                if (prev != null && prev.Procs.TryGetValue(p.Pid, out var old) && old.CreateTime == p.CreateTime) q = old;
                var s = prev == null ? Math.Max(0.01, (now - p.CreateTime) / 1e7) : Math.Max(secs, 0.001);
                double R(long after, long before) => (after - before) / s;
                var v = new Dictionary<string, string>();
                var usr = (p.User - (q?.User ?? 0)) / 1e5 / s;
                var sys = (p.Kernel - (q?.Kernel ?? 0)) / 1e5 / s;
                v["%usr"] = F(usr); v["%system"] = F(sys); v["%guest"] = F(0); v["%wait"] = F(0); v["%CPU"] = F(usr + sys); v["CPU"] = "-";
                var minflt = R(p.PageFaults - p.HardFaults, (q?.PageFaults ?? 0) - (q?.HardFaults ?? 0));
                v["minflt/s"] = F(minflt); v["majflt/s"] = F(R(p.HardFaults, q?.HardFaults ?? 0));
                v["VSZ"] = (p.Virtual / 1024).ToString(); v["RSS"] = (p.WorkingSet / 1024).ToString(); v["%MEM"] = F(100 * p.WorkingSet / total);
                v["kB_rd/s"] = F(R(p.ReadBytes, q?.ReadBytes ?? 0) / 1024); v["kB_wr/s"] = F(R(p.WriteBytes, q?.WriteBytes ?? 0) / 1024);
                v["kB_ccwr/s"] = F(0); v["iodelay"] = "0";
                v["cswch/s"] = F(R(p.ContextSwitches, q?.ContextSwitches ?? 0)); v["nvcswch/s"] = F(0);
                v["threads"] = p.Threads.ToString(); v["fd-nr"] = p.Handles.ToString();
                var active = sel.ShowAll || usr + sys > 0 && cpu || (mem && minflt > 0) || (disk && p.ReadBytes + p.WriteBytes - (q?.ReadBytes ?? 0) - (q?.WriteBytes ?? 0) > 0)
                             || (csw && p.ContextSwitches - (q?.ContextSwitches ?? 0) > 0) || various;
                rows.Add((p, v, active));
            }
            var groups = new List<string[]>();
            if (cpu) groups.Add(["%usr", "%system", "%guest", "%wait", "%CPU", "CPU"]);
            if (mem) groups.Add(["minflt/s", "majflt/s", "VSZ", "RSS", "%MEM"]);
            if (disk) groups.Add(["kB_rd/s", "kB_wr/s", "kB_ccwr/s", "iodelay"]);
            if (csw) groups.Add(["cswch/s", "nvcswch/s"]);
            if (various) groups.Add(["threads", "fd-nr"]);
            var idCol = userName ? "    USER" : "   UID";
            string Id(PerfSampler.Proc p) { var (u, uid) = PerfSampler.Owner(p.Pid); return userName ? $" {u,8}" : $" {uid,5}"; }
            string Cmd(PerfSampler.Proc p) => cmdline ? PerfSampler.CommandLine(p.Pid) ?? p.Name : Path.GetFileNameWithoutExtension(p.Name);
            var sb = new StringBuilder();
            if (horizontal)
            {
                var cols = groups.SelectMany(g => g).ToList();
                if (header) sb.AppendLine($"\n# Time      {idCol}       PID {string.Join(' ', cols.Select(c => c.PadLeft(9)))}  Command");
                var epoch = new DateTimeOffset(cur.Time).ToUnixTimeSeconds();
                foreach (var r in rows.Where(r => r.Active))
                    sb.AppendLine($"{epoch,10} {Id(r.P)} {r.P.Pid,9} {string.Join(' ', cols.Select(c => r.V[c].PadLeft(9)))}  {Cmd(r.P)}");
            }
            else
            {
                foreach (var g in groups)
                {
                    sb.AppendLine();
                    sb.AppendLine($"{(time.StartsWith("Average") ? time : Clock(prev?.Time ?? cur.Time))} {idCol}       PID {string.Join(' ', g.Select(c => c.PadLeft(c == "CPU" ? 5 : 9)))}  Command");
                    foreach (var r in rows.Where(r => r.Active))
                        sb.AppendLine($"{time}{Id(r.P)} {r.P.Pid,9} {string.Join(' ', g.Select(c => r.V[c].PadLeft(c == "CPU" ? 5 : 9)))}  {Cmd(r.P)}");
                }
            }
            Console.Write(sb.ToString());
        }
    }

    static void PidstatHelp() => Console.WriteLine("""
        Usage: pidstat [ options ] [ <interval> [ <count> ] ] [ -e <program> <args> ]
        Options are:
        [ -d ] [ -H ] [ -h ] [ -I ] [ -l ] [ -R ] [ -r ] [ -s ] [ -t ] [ -U [ <username> ] ]
        [ -u ] [ -V ] [ -v ] [ -w ] [ -C <command> ] [ -G <process_name> ]
        [ -p { <pid> [,...] | SELF | ALL } ] [ -T { TASK | CHILD | ALL } ]
        [ --dec={ 0 | 1 | 2 } ] [ --human ]

        Report statistics for Windows processes. UID is the last part (RID) of the owner's SID.
        Without an interval, figures cover each process's lifetime.
        """);

    // =====================================================================================
    // sar
    // =====================================================================================

    sealed class Report(string title, string label, string[] columns, Func<Sample, Sample, double, LoadState, List<(string Key, double[] Values)>> rows, int labelWidth = 9)
    {
        public string Title = title, Label = label;
        public string[] Columns = columns;
        public Func<Sample, Sample, double, LoadState, List<(string Key, double[] Values)>> Rows = rows;
        public int LabelWidth = labelWidth;
        public readonly Dictionary<string, (double[] Sum, int N)> Totals = new();
    }

    sealed class LoadState
    {
        public double L1 = -1, L5, L15;
        public void Add(double v, double secs)
        {
            if (L1 < 0) { L1 = L5 = L15 = v; return; }
            L1 = v + (L1 - v) * Math.Exp(-secs / 60); L5 = v + (L5 - v) * Math.Exp(-secs / 300); L15 = v + (L15 - v) * Math.Exp(-secs / 900);
        }
    }

    static string DailyFile(DateTime day) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sysstat", $"sa{day:dd}");

    static int Sar(string[] args)
    {
        var reports = new List<Report>();
        bool uAll = false, perCpu = false;
        var cpus = new List<int>();
        string output = null, input = null;
        var wantOutput = false;
        TimeSpan? start = null, end = null;
        var pos = new List<string>();
        var kinds = new HashSet<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"option {a} needs an argument");
            switch (a)
            {
                case "-h" or "--help": SarHelp(); return 0;
                case "-V" or "--version": PrintVersion("sar"); return 0;
                case "-u":
                    kinds.Add("u");
                    if (i + 1 < args.Length && args[i + 1] == "ALL") { uAll = true; i++; }
                    break;
                case "-P": ParseCpuList(Next(), cpus, ref perCpu); kinds.Add("u"); break;
                case "-r": kinds.Add("r"); if (i + 1 < args.Length && args[i + 1] == "ALL") i++; break;
                case "-S": kinds.Add("S"); break;
                case "-b": kinds.Add("b"); break;
                case "-B": kinds.Add("B"); break;
                case "-d": kinds.Add("d"); break;
                case "-q": kinds.Add("q"); if (i + 1 < args.Length && args[i + 1] is "LOAD" or "ALL") i++; break;
                case "-w": kinds.Add("w"); break;
                case "-W": kinds.Add("W"); break;
                case "-I": Next(); kinds.Add("I"); break;
                case "-n":
                    foreach (var k in Next().Split(','))
                    {
                        if (k == "ALL") kinds.UnionWith(["DEV", "EDEV", "IP", "TCP", "ETCP", "UDP"]);
                        else if (k is "DEV" or "EDEV" or "IP" or "TCP" or "ETCP" or "UDP") kinds.Add(k);
                        else throw new InvalidOperationException($"-n {k} is not available on Windows (supported: DEV, EDEV, IP, TCP, ETCP, UDP, ALL)");
                    }
                    break;
                case "-A":
                    kinds.UnionWith(["u", "r", "S", "b", "B", "d", "q", "w", "W", "I", "DEV", "EDEV", "IP", "TCP", "ETCP", "UDP"]);
                    uAll = perCpu = true;
                    break;
                case "-o":
                    wantOutput = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-') && !double.TryParse(args[i + 1], Inv, out _)) output = args[++i];
                    break;
                case "-f":
                    input = i + 1 < args.Length && !args[i + 1].StartsWith('-') && !double.TryParse(args[i + 1], Inv, out _) ? args[++i] : DailyFile(DateTime.Now);
                    break;
                case "-s": start = ParseClock(Next()); break;
                case "-e": end = ParseClock(Next()); break;
                case "-t" or "-p" or "-x" or "-h" or "-z": break;
                default:
                    if (a.StartsWith("--dec=") || a == "--human" || a.StartsWith("--iface=")) break;
                    if (a.StartsWith('-') && a.Length > 1 && a[1] != '-' && a[1..].All(c => "urSbBdqwW".Contains(c)))
                    {
                        foreach (var c in a[1..]) kinds.Add(c.ToString());
                        break;
                    }
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a} (see sar --help)");
                    pos.Add(a);
                    break;
            }
        }
        if (kinds.Count == 0) kinds.Add("u");
        BuildReports(kinds, uAll, perCpu, cpus, reports);

        if (pos.Count == 0 && !wantOutput)
        {
            // No interval: report from a data file, today's by default.
            input ??= DailyFile(DateTime.Now);
            if (!File.Exists(input))
            {
                Console.Error.WriteLine($"Cannot open {input}: No such file or directory");
                Console.Error.WriteLine("Please check if data collecting is enabled (e.g. run `sar -o <interval> <count>`)");
                return 1;
            }
            var samples = PerfSampler.ReadSamples(input).Where(s => (start == null || s.Time.TimeOfDay >= start) && (end == null || s.Time.TimeOfDay <= end)).ToList();
            if (samples.Count < 2) throw new InvalidOperationException($"{input}: not enough samples to report");
            Console.WriteLine(PerfSampler.SysstatHeader().Replace(DateTime.Now.ToString("MM/dd/yyyy"), samples[0].Time.ToString("MM/dd/yyyy")) + "\n");
            var load = new LoadState();
            for (var i = 1; i < samples.Count; i++) SarRows(reports, samples[i - 1], samples[i], load, i == 1, reports.Count > 1);
            SarAverages(reports);
            return 0;
        }

        var (interval, count) = Timing(pos, "sar");
        if (interval <= 0) throw new UsageException("an interval is required when collecting (see sar --help)");
        if (wantOutput)
        {
            output ??= DailyFile(DateTime.Now);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        }
        TrapCtrlC();
        using var sampler = new PerfSampler(true, PerfSampler.DiskCounters, PerfSampler.SystemCounters, PerfSampler.NetCounters);
        using var file = output == null ? null : new StreamWriter(output, append: File.Exists(output));
        if (file != null && file.BaseStream.Position == 0) file.WriteLine($"{PerfSampler.RecordingMagic} {Environment.MachineName} {Environment.ProcessorCount}");
        var prev = sampler.Take();
        if (file != null) PerfSampler.WriteSample(file, prev);
        Console.WriteLine(PerfSampler.SysstatHeader() + "\n");
        var state = new LoadState();
        for (var n = 0; n < count && !Wait(interval); n++)
        {
            var cur = sampler.Take();
            if (file != null) { PerfSampler.WriteSample(file, cur); file.Flush(); }
            SarRows(reports, prev, cur, state, n == 0, reports.Count > 1);
            prev = cur;
        }
        SarAverages(reports);
        return 0;
    }

    static TimeSpan ParseClock(string s) => TimeSpan.TryParse(s, Inv, out var t) ? t : throw new UsageException($"invalid time '{s}' (use hh:mm:ss)");

    static void SarRows(List<Report> reports, Sample a, Sample b, LoadState load, bool first, bool repeatHeaders)
    {
        var secs = Math.Max(a.Seconds(b), 0.001);
        var runq = b.Counter(@"\System\Processor Queue Length");
        load.Add(double.IsNaN(runq) ? b.Procs.Values.Where(p => p.Pid != 0).Sum(p => p.Running) : runq, secs);
        var sb = new StringBuilder();
        foreach (var r in reports)
        {
            var rows = r.Rows(a, b, secs, load);
            if (first || repeatHeaders) sb.AppendLine($"{Clock(a.Time)} {(r.Label == null ? "" : r.Label.PadLeft(r.LabelWidth) + " ")}{string.Join(' ', r.Columns.Select(c => c.Split(':')[0].PadLeft(9)))}");
            foreach (var (key, values) in rows)
            {
                sb.AppendLine($"{Clock(b.Time)} {(r.Label == null ? "" : key.PadLeft(r.LabelWidth) + " ")}{Values(r, values)}");
                var k = key ?? "";
                if (!r.Totals.TryGetValue(k, out var t)) t = (new double[values.Length], 0);
                for (var i = 0; i < values.Length; i++) t.Sum[i] += double.IsNaN(values[i]) ? 0 : values[i];
                r.Totals[k] = (t.Sum, t.N + 1);
            }
            if (repeatHeaders) sb.AppendLine();
        }
        Console.Write(sb.ToString());
    }

    static string Values(Report r, double[] values) =>
        string.Join(' ', values.Select((v, i) => F(v, r.Columns[i].Contains(':') ? int.Parse(r.Columns[i].Split(':')[1]) : 2).PadLeft(9)));

    static void SarAverages(List<Report> reports)
    {
        var sb = new StringBuilder();
        foreach (var r in reports)
        {
            if (r.Totals.Count == 0) continue;
            sb.AppendLine();
            sb.AppendLine($"Average:    {(r.Label == null ? "" : r.Label.PadLeft(r.LabelWidth) + " ")}{string.Join(' ', r.Columns.Select(c => c.Split(':')[0].PadLeft(9)))}");
            foreach (var (key, (sum, n)) in r.Totals)
                sb.AppendLine($"Average:    {(r.Label == null ? "" : key.PadLeft(r.LabelWidth) + " ")}{Values(r, sum.Select(v => v / n).ToArray())}");
        }
        Console.Write(sb.ToString());
    }

    static void BuildReports(HashSet<string> kinds, bool uAll, bool perCpu, List<int> cpus, List<Report> reports)
    {
        double D(Sample a, Sample b, string counter) { var x = b.Counter(counter); var y = a.Counter(counter); return double.IsNaN(x) || double.IsNaN(y) ? 0 : x - y; }
        double C(Sample b, string counter) { var x = b.Counter(counter); return double.IsNaN(x) ? 0 : x; }

        if (kinds.Contains("u"))
            reports.Add(new Report("cpu", "CPU",
                uAll ? ["%usr", "%nice", "%sys", "%iowait", "%steal", "%irq", "%soft", "%guest", "%gnice", "%idle"]
                     : ["%user", "%nice", "%system", "%iowait", "%steal", "%idle"],
                (a, b, s, _) => SelectedCpus(b, cpus, perCpu).Select(i =>
                {
                    var c = i < 0 ? CpuPct(a.Total, b.Total) : CpuPct(a.Cpus[i], b.Cpus[i]);
                    double[] v = uAll ? [c[0], 0, c[2], 0, 0, c[4], c[5], 0, 0, c[9]] : [c[0], 0, c[2] + c[4] + c[5], 0, 0, c[9]];
                    return (i < 0 ? "all" : i.ToString(), v);
                }).ToList(), 7));
        if (kinds.Contains("I"))
            reports.Add(new Report("intr", "INTR", ["intr/s"], (a, b, s, _) => [("sum", [(b.Total.Interrupts - a.Total.Interrupts) / s])]));
        if (kinds.Contains("w"))
            reports.Add(new Report("pcsw", null, ["proc/s", "cswch/s"], (a, b, s, _) =>
                [(null, [b.Procs.Keys.Count(k => !a.Procs.ContainsKey(k)) / s, C(b, @"\System\Context Switches/sec")])]));
        if (kinds.Contains("W"))
            reports.Add(new Report("swap", null, ["pswpin/s", "pswpout/s"], (a, b, s, _) =>
                [(null, [C(b, @"\Memory\Pages Input/sec"), C(b, @"\Memory\Pages Output/sec")])]));
        if (kinds.Contains("B"))
            reports.Add(new Report("paging", null, ["pgpgin/s", "pgpgout/s", "fault/s", "majflt/s", "pgfree/s", "pgscank/s", "pgscand/s", "pgsteal/s", "%vmeff"], (a, b, s, _) =>
                [(null, [C(b, @"\Memory\Pages Input/sec") * 4, C(b, @"\Memory\Pages Output/sec") * 4, C(b, @"\Memory\Page Faults/sec"), C(b, @"\Memory\Page Reads/sec"), C(b, @"\Memory\Demand Zero Faults/sec"), 0, 0, 0, 0])]));
        if (kinds.Contains("b"))
            reports.Add(new Report("io", null, ["tps", "rtps", "wtps", "dtps", "bread/s", "bwrtn/s", "bdscd/s"], (a, b, s, _) =>
            {
                double r = 0, w = 0, rb = 0, wb = 0;
                foreach (var d in b.Disks.Values)
                {
                    var x = DiskDelta(a, d, s);
                    r += x.R; w += x.W; rb += x.Rb; wb += x.Wb;
                }
                return [(null, [r + w, r, w, 0, rb / 512, wb / 512, 0])];
            }));
        if (kinds.Contains("r"))
            reports.Add(new Report("mem", null, ["kbmemfree:0", "kbavail:0", "kbmemused:0", "%memused", "kbbuffers:0", "kbcached:0", "kbcommit:0", "%commit", "kbactive:0", "kbinact:0", "kbdirty:0"], (a, b, s, _) =>
            {
                var m = b.Mem;
                var free = C(b, @"\Memory\Free & Zero Page List Bytes");
                var standby = C(b, @"\Memory\Standby Cache Normal Priority Bytes");
                return [(null, [(free > 0 ? free : m.Available) / 1024, m.Available / 1024.0, (m.Total - m.Available) / 1024.0, 100.0 * (m.Total - m.Available) / Math.Max(1, m.Total),
                    0, (standby > 0 ? standby : m.Cached) / 1024, m.CommitTotal / 1024.0, 100.0 * m.CommitTotal / Math.Max(1, m.Total + m.SwapTotal), 0, 0, C(b, @"\Memory\Modified Page List Bytes") / 1024])];
            }));
        if (kinds.Contains("S"))
            reports.Add(new Report("swap", null, ["kbswpfree:0", "kbswpused:0", "%swpused", "kbswpcad:0", "%swpcad"], (a, b, s, _) =>
                [(null, [(b.Mem.SwapTotal - b.Mem.SwapUsed) / 1024.0, b.Mem.SwapUsed / 1024.0, 100.0 * b.Mem.SwapUsed / Math.Max(1, b.Mem.SwapTotal), 0, 0])]));
        if (kinds.Contains("q"))
            reports.Add(new Report("queue", null, ["runq-sz:0", "plist-sz:0", "ldavg-1", "ldavg-5", "ldavg-15", "blocked:0"], (a, b, s, load) =>
                [(null, [C(b, @"\System\Processor Queue Length"), b.Mem.ThreadCount, load.L1, load.L5, load.L15, 0])]));
        if (kinds.Contains("d"))
            reports.Add(new Report("disk", "DEV", ["tps", "rkB/s", "wkB/s", "dkB/s", "areq-sz", "aqu-sz", "await", "%util"], (a, b, s, _) =>
                b.Disks.Values.OrderBy(d => d.Name, StringComparer.Ordinal).Select(d =>
                {
                    var x = DiskDelta(a, d, s);
                    var tps = x.R + x.W;
                    var await = tps > 0 ? (x.RAwait * x.R + x.WAwait * x.W) / tps : 0;
                    return (d.Name, new[] { tps, x.Rb / 1024, x.Wb / 1024, 0, tps > 0 ? (x.Rb + x.Wb) / tps / 1024 : 0, x.Queue, await, x.Util });
                }).ToList(), 12));
        if (kinds.Contains("DEV"))
            reports.Add(new Report("net-dev", "IFACE", ["rxpck/s", "txpck/s", "rxkB/s", "txkB/s", "rxcmp/s", "txcmp/s", "rxmcst/s", "%ifutil"], (a, b, s, _) =>
                b.Net.Values.Select(n =>
                {
                    a.Net.TryGetValue(n.Name, out var o);
                    double R(Func<PerfSampler.NetIf, long> f) => (f(n) - (o == null ? f(n) : f(o))) / s;
                    var util = n.Speed > 0 ? Math.Min(100, Math.Max(R(x => x.InBytes), R(x => x.OutBytes)) * 8 * 100 / n.Speed) : 0;
                    return (n.Name, new[] { R(x => x.InPackets), R(x => x.OutPackets), R(x => x.InBytes) / 1024, R(x => x.OutBytes) / 1024, 0, 0, R(x => x.InMulticast), util });
                }).ToList(), 12));
        if (kinds.Contains("EDEV"))
            reports.Add(new Report("net-edev", "IFACE", ["rxerr/s", "txerr/s", "coll/s", "rxdrop/s", "txdrop/s", "txcarr/s", "rxfram/s", "rxfifo/s", "txfifo/s"], (a, b, s, _) =>
                b.Net.Values.Select(n =>
                {
                    a.Net.TryGetValue(n.Name, out var o);
                    double R(Func<PerfSampler.NetIf, long> f) => (f(n) - (o == null ? f(n) : f(o))) / s;
                    return (n.Name, new[] { R(x => x.InErrors), R(x => x.OutErrors), 0, R(x => x.InDrops), R(x => x.OutDrops), 0, 0, 0, 0 });
                }).ToList(), 12));
        if (kinds.Contains("IP"))
            reports.Add(new Report("net-ip", null, ["irec/s", "fwddgm/s", "idel/s", "orq/s", "asmrq/s", "asmok/s", "fragok/s", "fragcrt/s"], (a, b, s, _) =>
                [(null, [C(b, @"\IPv4\Datagrams Received/sec"), C(b, @"\IPv4\Datagrams Forwarded/sec"), C(b, @"\IPv4\Datagrams Received Delivered/sec"),
                    C(b, @"\IPv4\Datagrams Sent/sec"), C(b, @"\IPv4\Fragments Received/sec"), C(b, @"\IPv4\Fragments Re-assembled/sec"),
                    C(b, @"\IPv4\Fragmented Datagrams/sec"), C(b, @"\IPv4\Fragments Created/sec")])]));
        if (kinds.Contains("TCP"))
            reports.Add(new Report("net-tcp", null, ["active/s", "passive/s", "iseg/s", "oseg/s"], (a, b, s, _) =>
                [(null, [D(a, b, @"\TCPv4\Connections Active") / s, D(a, b, @"\TCPv4\Connections Passive") / s, C(b, @"\TCPv4\Segments Received/sec"), C(b, @"\TCPv4\Segments Sent/sec")])]));
        if (kinds.Contains("ETCP"))
            reports.Add(new Report("net-etcp", null, ["atmptf/s", "estres/s", "retrans/s", "isegerr/s", "orsts/s"], (a, b, s, _) =>
                [(null, [D(a, b, @"\TCPv4\Connection Failures") / s, D(a, b, @"\TCPv4\Connections Reset") / s, C(b, @"\TCPv4\Segments Retransmitted/sec"), 0, 0])]));
        if (kinds.Contains("UDP"))
            reports.Add(new Report("net-udp", null, ["idgm/s", "odgm/s", "noport/s", "idgmerr/s"], (a, b, s, _) =>
                [(null, [C(b, @"\UDPv4\Datagrams Received/sec"), C(b, @"\UDPv4\Datagrams Sent/sec"), C(b, @"\UDPv4\Datagrams No Port/sec"), D(a, b, @"\UDPv4\Datagrams Received Errors") / s])]));
    }

    static void SarHelp() => Console.WriteLine("""
        Usage: sar [ options ] [ <interval> [ <count> ] ]
        Main options and reports:
        	-B	Paging statistics
        	-b	I/O and transfer rate statistics
        	-d	Block device statistics
        	-I { SUM | ALL }	Interrupts statistics
        	-n { <keyword> [,...] | ALL }
        		Network statistics
        		Keywords are:
        		DEV	Network interfaces
        		EDEV	Network interfaces (errors)
        		IP	IP traffic
        		TCP	TCP traffic
        		ETCP	TCP traffic (errors)
        		UDP	UDP traffic
        	-q	Queue length and load average statistics
        	-r [ ALL ]	Memory utilization statistics
        	-S	Swap space utilization statistics
        	-u [ ALL ]	CPU utilization statistics
        	-W	Swapping statistics
        	-w	Task creation and system switching statistics
        	-A	All of the above
        	-P { <cpu_list> | ALL }	Per-processor statistics

        Data files:
        	-o [ <filename> ]	Save samples (default: %LOCALAPPDATA%\sysstat\saDD) while reporting
        	-f [ <filename> ]	Report from a saved file (default: today's file)
        	-s [ <hh:mm[:ss]> ]  -e [ <hh:mm[:ss]> ]	Time window when reading a file

        Load averages are computed by sar itself from the run queue; Windows keeps none.
        """);
}
