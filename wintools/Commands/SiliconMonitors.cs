using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Sample = WinTools.PerfSampler.Sample;

namespace WinTools;

// asitop, macmon and mactop: the Apple Silicon dashboards, reading the same kinds of sensors on Windows.
// E/P core tiers come from the CPU's efficiency classes (Intel hybrid and ARM big.LITTLE parts), power
// from the RAPL energy meters (package, cores, integrated GPU, DRAM), GPU load from the GPU Engine
// counters, temperature from ACPI thermal zones. Sensors a machine doesn't expose read as null/0.
static class SiliconMonitors
{
    const string AsitopVersion = "0.0.24", MacmonVersion = "0.10.0", MactopVersion = "2.1.6";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("asitop", "Performance monitoring dashboard (CPU tiers, GPU, power, memory)", Asitop);
        Commands.Add("macmon", "Sudoless performance monitoring: TUI, JSON pipe and HTTP server", Macmon);
        Commands.Add("mactop", "System monitor top: CPU tiers, GPU, power, network, disk", Mactop);
    }

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ---- one interval's worth of metrics ----

    sealed class Core { public int Index, Tier; public double Active, Mhz; }

    sealed class Tier
    {
        public string Label;
        public List<Core> Cores = new();
        public double Active => Cores.Count == 0 ? 0 : Cores.Average(c => c.Active);
        public double Mhz => Cores.Where(c => c.Mhz > 0).Select(c => c.Mhz).DefaultIfEmpty(0).Average();   // parked cores read 0
    }

    sealed class Metrics
    {
        public DateTime Time;
        public double CpuActive, Gpu, GpuMem, CpuW, GpuW, DramW, PkgW, TempC;
        public List<Tier> Tiers = new();
        public List<Core> Cores = new();
        public long RamTotal, RamUsed, SwapTotal, SwapUsed;
        public double NetIn, NetOut, NetInPk, NetOutPk, DiskR, DiskW, DiskROps, DiskWOps;
        public (int Percent, bool OnAc, bool Charging, bool HasBattery) Battery;
        public List<(PerfSampler.Proc P, double Cpu, double Gpu)> Top = new();
        public Dictionary<int, double> MaxMhz = new();
    }

    static byte[] efficiency;
    static readonly Dictionary<int, double> tierMax = new();

    static Metrics Measure(Sample a, Sample b)
    {
        efficiency ??= PerfSampler.EfficiencyClasses();
        var m = new Metrics { Time = b.Time };
        var secs = Math.Max(a.Seconds(b), 0.001);
        var classes = efficiency.Distinct().Order().ToList();
        var hybrid = classes.Count > 1;
        for (var t = 0; t < classes.Count; t++)
            m.Tiers.Add(new Tier { Label = !hybrid ? "_" : classes.Count == 2 ? (t == 0 ? "E" : "P") : t == 0 ? "E" : t == classes.Count - 1 ? "S" : "P" });
        for (var i = 0; i < b.Cpus.Length; i++)
        {
            var tier = i < efficiency.Length ? classes.IndexOf(efficiency[i]) : 0;
            var core = new Core { Index = i, Tier = tier, Active = PerfSampler.CpuBusy(a.Cpus[i], b.Cpus[i]) / 100, Mhz = PerfSampler.FrequencyMhz(b, PerfSampler.CpuInstance(i)) };
            m.Cores.Add(core);
            m.Tiers[tier].Cores.Add(core);
            tierMax[tier] = Math.Max(tierMax.GetValueOrDefault(tier, PerfSampler.BaseMhz), core.Mhz);
        }
        m.MaxMhz = new Dictionary<int, double>(tierMax);
        m.CpuActive = PerfSampler.CpuBusy(a.Total, b.Total) / 100;
        m.Gpu = PerfSampler.GpuBusy(b, out var gpuByPid) / 100;
        m.GpuMem = PerfSampler.GpuMemory(b);
        m.PkgW = PerfSampler.Watts(b, "PKG");
        m.CpuW = PerfSampler.Watts(b, "PP0");
        m.GpuW = PerfSampler.Watts(b, "PP1");
        m.DramW = PerfSampler.Watts(b, "DRAM");
        m.TempC = PerfSampler.TemperatureC(b);
        m.RamTotal = b.Mem.Total; m.RamUsed = b.Mem.Total - b.Mem.Available;
        m.SwapTotal = b.Mem.SwapTotal; m.SwapUsed = b.Mem.SwapUsed;
        foreach (var n in b.Net.Values.Where(n => !n.Loopback))
        {
            if (!a.Net.TryGetValue(n.Name, out var o)) continue;
            m.NetIn += (n.InBytes - o.InBytes) / secs; m.NetOut += (n.OutBytes - o.OutBytes) / secs;
            m.NetInPk += (n.InPackets - o.InPackets) / secs; m.NetOutPk += (n.OutPackets - o.OutPackets) / secs;
        }
        foreach (var d in b.Disks.Values)
        {
            if (!a.Disks.TryGetValue(d.Name, out var o)) continue;
            m.DiskR += (d.ReadBytes - o.ReadBytes) / secs; m.DiskW += (d.WriteBytes - o.WriteBytes) / secs;
            m.DiskROps += PerfSampler.Count32(o.Reads, d.Reads) / secs; m.DiskWOps += PerfSampler.Count32(o.Writes, d.Writes) / secs;
        }
        m.Battery = PerfSampler.Battery();
        foreach (var p in b.Procs.Values.Where(p => p.Pid != 0))
        {
            a.Procs.TryGetValue(p.Pid, out var q);
            if (q != null && q.CreateTime != p.CreateTime) q = null;
            m.Top.Add((p, PerfSampler.ProcCpu(q, p, secs), gpuByPid.GetValueOrDefault(p.Pid)));
        }
        m.Top = m.Top.OrderByDescending(t => t.Cpu).ThenByDescending(t => t.P.WorkingSet).ToList();
        return m;
    }

    static PerfSampler NewSampler(bool processes) => new(processes, PerfSampler.SocCounters);

    static string Gb(long bytes) => (bytes / 1073741824.0).ToString("F1", Inv);
    static string W(double watts) => double.IsNaN(watts) ? "n/a" : watts.ToString("F2", Inv) + "W";
    static double Z(double v) => double.IsNaN(v) ? 0 : v;

    static string Rate(double bytesPerSec)
    {
        if (bytesPerSec >= 1 << 30) return (bytesPerSec / (1 << 30)).ToString("F1", Inv) + " GB/s";
        if (bytesPerSec >= 1 << 20) return (bytesPerSec / (1 << 20)).ToString("F1", Inv) + " MB/s";
        return (bytesPerSec / 1024).ToString("F1", Inv) + " KB/s";
    }

    static string SystemTitle()
    {
        efficiency ??= PerfSampler.EfficiencyClasses();
        var classes = efficiency.Distinct().Order().ToList();
        var counts = classes.Select(c => efficiency.Count(e => e == c)).ToList();
        var cores = classes.Count > 1 ? $"{counts[0]}E+{string.Join('+', counts.Skip(1).Select(c => c + "P"))}" : $"{efficiency.Length} threads";
        return $"{PerfSampler.CpuName} ({cores}, {GpuName()})";
    }

    static string GpuName()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            if (key == null) return "GPU";
            var names = key.GetSubKeyNames().Where(n => n.Length == 4 && char.IsDigit(n[0]))
                .Select(n => { using var k = key.OpenSubKey(n); return k?.GetValue("DriverDesc") as string; })
                .Where(n => n != null && !n.Contains("Basic Display") && !n.Contains("Remote")).Distinct().ToList();
            return names.Count == 0 ? "GPU" : string.Join(" + ", names);
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException) { return "GPU"; }
    }

    // ---- terminal drawing ----

    static readonly string[] Palette = ["\x1b[37m", "\x1b[31m", "\x1b[32m", "\x1b[33m", "\x1b[34m", "\x1b[35m", "\x1b[36m", "\x1b[97m", "\x1b[90m"];
    const string Reset = "\x1b[0m";

    static void EnterScreen()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Write("\x1b[?1049h\x1b[?25l");
    }

    static void LeaveScreen() => Console.Write("\x1b[?25h\x1b[?1049l");

    static int Width => Console.IsOutputRedirected ? 100 : Math.Max(60, Console.WindowWidth - 1);

    static string BoxTop(string title, int w) => "┌ " + title + " " + new string('─', Math.Max(0, w - title.Length - 4)) + "┐";
    static string BoxLine(string text, int w)
    {
        var visible = VisibleLength(text);
        return "│ " + text + new string(' ', Math.Max(0, w - visible - 3)) + "│";
    }
    static string BoxBottom(int w) => "└" + new string('─', w - 2) + "┘";

    static int VisibleLength(string s)
    {
        var n = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '\x1b') { while (i < s.Length && s[i] != 'm') i++; continue; }
            n++;
        }
        return n;
    }

    // Run a redraw loop until q / Ctrl+C. Redirected output gets one frame per interval, appended.
    static void Loop(double seconds, int count, PerfSampler sampler, Func<Metrics, string> frame)
    {
        var interactive = !Console.IsOutputRedirected;
        var quit = false;
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit = true; };
        if (interactive) EnterScreen();
        try
        {
            var prev = sampler.Take();
            for (var n = 0; (count <= 0 || n < count) && !quit; n++)
            {
                if (PerfSampler.Sleep(seconds, k => quit = k.KeyChar is 'q' or 'Q' || k.Key == ConsoleKey.Escape)) break;
                var cur = sampler.Take();
                var text = frame(Measure(prev, cur));
                prev = cur;
                if (interactive) Console.Write("\x1b[H\x1b[2J" + text);
                else Console.WriteLine(text);
            }
        }
        finally { if (interactive) LeaveScreen(); }
    }

    // =====================================================================================
    // asitop
    // =====================================================================================

    static int Asitop(string[] args)
    {
        double interval = 1, avg = 30;
        int color = 2;
        var showCores = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"argument {a}: expected one argument");
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: asitop [-h] [--interval INTERVAL] [--color COLOR] [--avg AVG] [--show_cores SHOW_CORES]

                        asitop: Performance monitoring CLI tool for Windows (CPU tiers, GPU, power, memory)

                        optional arguments:
                          -h, --help            show this help message and exit
                          --interval INTERVAL   Display interval and sampling interval (seconds)
                          --color COLOR         Choose display color (0~8)
                          --avg AVG             Interval for averaged values (seconds)
                          --show_cores SHOW_CORES
                                                Choose show cores mode
                          --max_count MAX_COUNT
                                                Number of updates before exiting (0 = run until q)
                        """);
                    return 0;
                case "--interval": interval = Num(Next(), a); break;
                case "--color": color = (int)Num(Next(), a); if (color is < 0 or > 8) throw new UsageException("--color must be 0~8"); break;
                case "--avg": avg = Num(Next(), a); break;
                case "--show_cores": showCores = Next() is "True" or "true" or "1"; break;
                case "--max_count": maxCount = (int)Num(Next(), a); break;
                default: throw new UsageException($"unrecognized arguments: {a}");
            }
        }
        if (interval <= 0) throw new UsageException("--interval must be positive");
        using var sampler = NewSampler(false);
        var history = new Queue<(DateTime T, double Cpu, double Gpu, double Pkg)>();
        double peakCpu = 0, peakGpu = 0, peakPkg = 0;
        var title = SystemTitle();
        var c = Console.IsOutputRedirected ? "" : Palette[color];
        Loop(interval, maxCount, sampler, m =>
        {
            history.Enqueue((m.Time, Z(m.CpuW), Z(m.GpuW), Z(m.PkgW)));
            while (history.Count > 1 && (m.Time - history.Peek().T).TotalSeconds > avg) history.Dequeue();
            peakCpu = Math.Max(peakCpu, Z(m.CpuW)); peakGpu = Math.Max(peakGpu, Z(m.GpuW)); peakPkg = Math.Max(peakPkg, Z(m.PkgW));
            var w = Math.Min(Width, 120);
            var half = (w - 6) / 2;
            var sb = new StringBuilder();
            sb.AppendLine(c + BoxTop("asitop - Performance monitoring for Windows", w));
            sb.AppendLine(BoxLine(title, w));
            sb.AppendLine(BoxLine("", w));
            var tiers = m.Tiers;
            for (var t = 0; t < tiers.Count; t += 2)
            {
                string Gauge(Tier tier) => $"{(tier.Label == "_" ? "" : tier.Label + "-")}CPU Usage: {tier.Active * 100,3:F0}% @ {tier.Mhz,4:F0} MHz";
                string Bar(Tier tier) => PerfSampler.Bar(tier.Active * 100, half - 2);
                var left = tiers[t]; var right = t + 1 < tiers.Count ? tiers[t + 1] : null;
                sb.AppendLine(BoxLine(Gauge(left).PadRight(half) + (right == null ? "" : Gauge(right)), w));
                sb.AppendLine(BoxLine(Bar(left).PadRight(half) + (right == null ? "" : Bar(right)), w));
                if (showCores)
                {
                    var cores = left.Cores.Concat(right?.Cores ?? []).ToList();
                    foreach (var chunk in cores.Chunk(4))
                        sb.AppendLine(BoxLine(string.Join("  ", chunk.Select(core => $"Core{core.Index,-2} {core.Active * 100,3:F0}%")), w));
                }
            }
            var gpu = double.IsNaN(m.Gpu) ? "GPU Usage: n/a" : $"GPU Usage: {m.Gpu * 100,3:F0}% ({Gb((long)Z(m.GpuMem))} GB dedicated in use)";
            sb.AppendLine(BoxLine(gpu, w));
            sb.AppendLine(BoxLine(PerfSampler.Bar(Z(m.Gpu) * 100, w - 6), w));
            sb.AppendLine(BoxLine("", w));
            sb.AppendLine(BoxLine($"RAM Usage: {Gb(m.RamUsed)}/{Gb(m.RamTotal)}GB - swap:{Gb(m.SwapUsed)}/{Gb(m.SwapTotal)}GB", w));
            sb.AppendLine(BoxLine(PerfSampler.Bar(100.0 * m.RamUsed / Math.Max(1, m.RamTotal), w - 6), w));
            sb.AppendLine(BoxLine("", w));
            var avgCpu = history.Average(h => h.Cpu); var avgGpu = history.Average(h => h.Gpu); var avgPkg = history.Average(h => h.Pkg);
            sb.AppendLine(BoxLine($"CPU Power: {W(m.CpuW)} (avg: {W(avgCpu)} peak: {W(peakCpu)})".PadRight(half - 1) + $" GPU Power: {W(m.GpuW)} (avg: {W(avgGpu)} peak: {W(peakGpu)})", w));
            sb.AppendLine(BoxLine($"Package Power: {W(m.PkgW)} (avg: {W(avgPkg)} peak: {W(peakPkg)})".PadRight(half - 1) + $" DRAM Power: {W(m.DramW)}", w));
            if (!double.IsNaN(m.TempC)) sb.AppendLine(BoxLine($"Temperature: {m.TempC:F1} °C", w));
            if (m.Battery.HasBattery) sb.AppendLine(BoxLine($"Battery: {m.Battery.Percent}% ({(m.Battery.Charging ? "charging" : m.Battery.OnAc ? "on AC" : "on battery")})", w));
            sb.Append(BoxBottom(w) + (c.Length > 0 ? Reset : ""));
            return sb.ToString();
        });
        return 0;
    }

    static int maxCount;

    static double Num(string s, string opt) =>
        double.TryParse(s, Inv, out var v) ? v : throw new UsageException($"argument {opt}: invalid value: '{s}'");

    // =====================================================================================
    // macmon
    // =====================================================================================

    static int Macmon(string[] args)
    {
        var interval = 1000.0;
        string command = null;
        var samples = 0;
        var socInfo = false;
        var port = 9090;
        var host = "0.0.0.0";
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"a value is required for '{a}'");
            switch (a)
            {
                case "-h" or "--help": MacmonHelp(command); return 0;
                case "-V" or "--version": Console.WriteLine($"macmon {MacmonVersion}"); return 0;
                case "-i" or "--interval": interval = Num(Next(), a); break;
                case "-s" or "--samples": samples = (int)Num(Next(), a); break;
                case "-p" or "--port": port = (int)Num(Next(), a); break;
                case "--host": host = Next(); break;
                case "--soc-info": socInfo = true; break;
                case "--install" or "--uninstall":
                    throw new InvalidOperationException("serve --install is launchd-specific; on Windows start `macmon serve` from Task Scheduler instead");
                case "pipe" or "serve" or "debug" or "stress" or "help" when command == null:
                    command = a;
                    break;
                default: throw new UsageException($"unexpected argument '{a}' found");
            }
        }
        if (interval < 100) interval = 100;
        switch (command)
        {
            case "help": MacmonHelp(null); return 0;
            case "debug": MacmonDebug(); return 0;
            case "stress": return Stress();
            case "pipe":
            {
                using var sampler = NewSampler(false);
                var prev = sampler.Take();
                for (var n = 0; samples <= 0 || n < samples; n++)
                {
                    PerfSampler.Sleep(interval / 1000);
                    var cur = sampler.Take();
                    Console.WriteLine(MacmonJson(Measure(prev, cur), socInfo));
                    prev = cur;
                }
                return 0;
            }
            case "serve": return Serve(host, port, interval, MacmonJson, MacmonPrometheus);
            default:
            {
                using var sampler = NewSampler(true);
                var title = SystemTitle();
                Loop(interval / 1000, 0, sampler, m => MacmonFrame(m, title));
                return 0;
            }
        }
    }

    static string MacmonJson(Metrics m) => MacmonJson(m, true);

    static string MacmonJson(Metrics m, bool socInfo)
    {
        string J(double v) => PerfSampler.Json(v, 6);
        string CoreJson(Core c) => $"{{\"die_id\":0,\"core_id\":{c.Index},\"freq_mhz\":{c.Mhz:F0},\"scaled_ratio\":{J(Scaled(m, c))},\"active_ratio\":{J(c.Active)}}}";
        string TierJson(Tier t) => $"{{\"label\":\"{t.Label}\",\"freq_mhz\":{t.Mhz:F0},\"scaled_ratio\":{J(t.Cores.Count == 0 ? 0 : t.Cores.Average(c => Scaled(m, c)))},\"active_ratio\":{J(t.Active)},\"cores\":[{string.Join(',', t.Cores.Select(CoreJson))}]}}";
        var e = m.Tiers[0];
        var p = m.Tiers[^1];
        var cpuW = Z(m.CpuW); var gpuW = Z(m.GpuW); var ramW = Z(m.DramW);
        var all = double.IsNaN(m.PkgW) ? cpuW + gpuW : m.PkgW;
        var sb = new StringBuilder("{");
        sb.Append($"\"timestamp\":\"{m.Time.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.ffffff+00:00}\",");
        sb.Append($"\"temp\":{{\"cpu_temp_avg\":{J(m.TempC)},\"gpu_temp_avg\":null}},");
        sb.Append($"\"memory\":{{\"ram_total\":{m.RamTotal},\"ram_usage\":{m.RamUsed},\"swap_total\":{m.SwapTotal},\"swap_usage\":{m.SwapUsed}}},");
        sb.Append("\"fans\":[],");
        sb.Append($"\"cpu_scaled_ratio\":{J(m.Cores.Average(c => Scaled(m, c)))},\"cpu_active_ratio\":{J(m.CpuActive)},");
        sb.Append($"\"cpu_tiers\":[{string.Join(',', m.Tiers.Select(TierJson))}],");
        sb.Append($"\"ecpu_freq_mhz\":{e.Mhz:F0},\"ecpu_scaled_ratio\":{J(e.Cores.Average(c => Scaled(m, c)))},\"ecpu_active_ratio\":{J(e.Active)},");
        sb.Append($"\"pcpu_freq_mhz\":{p.Mhz:F0},\"pcpu_scaled_ratio\":{J(p.Cores.Average(c => Scaled(m, c)))},\"pcpu_active_ratio\":{J(p.Active)},");
        sb.Append($"\"ecpu_cores\":[{string.Join(',', e.Cores.Select(CoreJson))}],\"pcpu_cores\":[{string.Join(',', p.Cores.Select(CoreJson))}],");
        sb.Append($"\"ecpu_usage\":[{e.Mhz:F0},{J(e.Active)}],\"pcpu_usage\":[{p.Mhz:F0},{J(p.Active)}],\"gpu_usage\":[0,{J(Z(m.Gpu))}],");
        sb.Append($"\"gpu_freq_mhz\":0,\"gpu_scaled_ratio\":{J(Z(m.Gpu))},\"gpu_active_ratio\":{J(Z(m.Gpu))},");
        sb.Append($"\"cpu_power\":{J(cpuW)},\"gpu_power\":{J(gpuW)},\"ane_power\":0.0,\"all_power\":{J(all)},\"sys_power\":{J(all + ramW)},\"ram_power\":{J(ramW)},\"gpu_ram_power\":0.0");
        if (socInfo)
            sb.Append($",\"soc\":{{\"chip_name\":{PerfSampler.Json(PerfSampler.CpuName)},\"memory_gb\":{m.RamTotal / 1073741824},\"ecpu_cores\":{e.Cores.Count},\"pcpu_cores\":{(m.Tiers.Count > 1 ? p.Cores.Count : 0)},\"gpu_name\":{PerfSampler.Json(GpuName())},\"vm\":false}}");
        return sb.Append('}').ToString();
    }

    // Frequency-scaled ratio: active share times the clock relative to the fastest clock seen on that tier.
    static double Scaled(Metrics m, Core c) => c.Active * Math.Min(1, c.Mhz / Math.Max(1, m.MaxMhz.GetValueOrDefault(c.Tier, c.Mhz)));

    static string MacmonPrometheus(Metrics m)
    {
        var sb = new StringBuilder();
        void G(string name, string help, double v, string labels = "")
        {
            sb.Append($"# HELP macmon_{name} {help}\n# TYPE macmon_{name} gauge\n");
            sb.Append($"macmon_{name}{labels} {PerfSampler.Json(v, 6).Replace("null", "NaN")}\n");
        }
        G("cpu_usage_ratio", "CPU active ratio", m.CpuActive);
        foreach (var t in m.Tiers) G($"cpu_{t.Label.ToLowerInvariant().Replace("_", "all")}_usage_ratio", $"{t.Label} tier active ratio", t.Active);
        G("gpu_usage_ratio", "GPU active ratio", Z(m.Gpu));
        G("cpu_power_watts", "CPU power", Z(m.CpuW));
        G("gpu_power_watts", "GPU power", Z(m.GpuW));
        G("ram_power_watts", "DRAM power", Z(m.DramW));
        G("all_power_watts", "Package power", Z(m.PkgW));
        G("memory_ram_total_bytes", "RAM total", m.RamTotal);
        G("memory_ram_usage_bytes", "RAM used", m.RamUsed);
        G("memory_swap_total_bytes", "Swap total", m.SwapTotal);
        G("memory_swap_usage_bytes", "Swap used", m.SwapUsed);
        if (!double.IsNaN(m.TempC)) G("temp_cpu_celsius", "CPU temperature", m.TempC);
        return sb.ToString();
    }

    static string MacmonFrame(Metrics m, string title)
    {
        var w = Width;
        var sb = new StringBuilder();
        sb.AppendLine(BoxTop(title, w));
        foreach (var t in m.Tiers)
        {
            var name = t.Label == "_" ? "CPU" : t.Label + "-CPU";
            sb.AppendLine(BoxLine($"{name,-6} {PerfSampler.Bar(t.Active * 100, 30)} {t.Active * 100,5:F1}% @ {t.Mhz,4:F0} MHz  ({t.Cores.Count} cores)", w));
        }
        sb.AppendLine(BoxLine($"{"GPU",-6} {PerfSampler.Bar(Z(m.Gpu) * 100, 30)} {Z(m.Gpu) * 100,5:F1}%", w));
        sb.AppendLine(BoxLine($"{"RAM",-6} {PerfSampler.Bar(100.0 * m.RamUsed / Math.Max(1, m.RamTotal), 30)} {Gb(m.RamUsed)} / {Gb(m.RamTotal)} GB   swap {Gb(m.SwapUsed)} / {Gb(m.SwapTotal)} GB", w));
        sb.AppendLine(BoxLine($"Power  CPU {W(m.CpuW)}  GPU {W(m.GpuW)}  DRAM {W(m.DramW)}  Package {W(m.PkgW)}" + (double.IsNaN(m.TempC) ? "" : $"   Temp {m.TempC:F1} °C"), w));
        sb.AppendLine(BoxBottom(w));
        sb.AppendLine($"{"PID",7}  {"CPU%",6}  {"GPU%",5}  {"MEM",9}  NAME");
        var rows = Console.IsOutputRedirected ? 15 : Math.Max(3, Console.WindowHeight - m.Tiers.Count - 10);
        foreach (var (p, cpu, gpu) in m.Top.Take(rows))
            sb.AppendLine($"{p.Pid,7}  {cpu,6:F1}  {gpu,5:F1}  {PerfSampler.HumanBytes(p.WorkingSet),9}  {Path.GetFileNameWithoutExtension(p.Name)}");
        sb.Append("q: quit");
        return sb.ToString();
    }

    static void MacmonDebug()
    {
        efficiency ??= PerfSampler.EfficiencyClasses();
        var classes = efficiency.Distinct().Order().ToList();
        Console.WriteLine($"Chip: {PerfSampler.CpuName}");
        Console.WriteLine($"OS: {PerfSampler.OsName} {PerfSampler.OsVersion} ({PerfSampler.Arch})");
        Console.WriteLine($"hw.ncpu: {efficiency.Length}");
        Console.WriteLine($"hw.nperflevels: {classes.Count}");
        for (var t = 0; t < classes.Count; t++)
            Console.WriteLine($"hw.perflevel{classes.Count - 1 - t}: efficiency class {classes[t]}, {efficiency.Count(e => e == classes[t])} logical cpus");
        Console.WriteLine($"hw.cpufrequency_nominal: {PerfSampler.BaseMhz} MHz");
        Console.WriteLine($"gpu: {GpuName()}");
        using var sampler = NewSampler(false);
        PerfSampler.Sleep(0.3);
        var s = sampler.Take();
        foreach (var path in PerfSampler.SocCounters)
        {
            var inst = s.Instances(path);
            Console.WriteLine($"counter {path}: {(inst.Count == 0 ? "unavailable" : inst.Count + " instance(s)")}");
        }
        var b = PerfSampler.Battery();
        Console.WriteLine($"battery: {(b.HasBattery ? b.Percent + "%" : "none")}");
    }

    // Busy-loop every logical CPU until q / Ctrl+C, to check the readings move.
    static int Stress()
    {
        var stop = false;
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop = true; };
        Console.WriteLine($"Stressing {Environment.ProcessorCount} logical CPUs, press Ctrl+C to stop");
        var threads = Enumerable.Range(0, Environment.ProcessorCount).Select(_ => new Thread(() => { var x = 0.0; while (!stop) x = Math.Sqrt(x + 1); }) { IsBackground = true }).ToList();
        threads.ForEach(t => t.Start());
        while (!stop) Thread.Sleep(100);
        return 0;
    }

    static void MacmonHelp(string command)
    {
        Console.WriteLine(command switch
        {
            "pipe" => "Output metrics in JSON format\n\nUsage: macmon pipe [OPTIONS]\n\nOptions:\n  -s, --samples <SAMPLES>    Number of samples to run for. Set to 0 to run indefinitely [default: 0]\n      --soc-info             Include SoC information in the output\n  -i, --interval <INTERVAL>  Update interval in milliseconds [default: 1000]\n  -h, --help                 Print help",
            "serve" => "Serve metrics over HTTP\n\nUsage: macmon serve [OPTIONS]\n\nOptions:\n  -p, --port <PORT>          Port to listen on [default: 9090]\n      --host <HOST>          Address to bind [default: 0.0.0.0]\n  -i, --interval <INTERVAL>  Update interval in milliseconds [default: 1000]\n  -h, --help                 Print help\n\nEndpoints: /json (same as pipe), /metrics (Prometheus)",
            _ => """
                Sudoless performance monitoring for CPU tiers, GPU, power and memory (Windows)

                Usage: macmon [OPTIONS] [COMMAND]

                Commands:
                  pipe    Output metrics in JSON format
                  serve   Serve metrics over HTTP
                  debug   Print debug information
                  stress  Generate load for testing metrics
                  help    Print this message or the help of the given subcommand(s)

                Options:
                  -i, --interval <INTERVAL>  Update interval in milliseconds [default: 1000]
                  -h, --help                 Print help
                  -V, --version              Print version
                """,
        });
    }

    // Minimal HTTP/1.1 server for macmon serve and mactop --prometheus: GET /json and /metrics.
    static int Serve(string host, int port, double intervalMs, Func<Metrics, string> json, Func<Metrics, string> prometheus)
    {
        if (!IPAddress.TryParse(host == "localhost" ? "127.0.0.1" : host, out var ip)) throw new UsageException($"invalid host '{host}'");
        using var sampler = NewSampler(false);
        Metrics latest = null;
        var gate = new object();
        var sampling = new Thread(() =>
        {
            var prev = sampler.Take();
            while (true)
            {
                PerfSampler.Sleep(intervalMs / 1000);
                var cur = sampler.Take();
                var m = Measure(prev, cur);
                lock (gate) latest = m;
                prev = cur;
            }
        }) { IsBackground = true };
        sampling.Start();
        var listener = new TcpListener(ip, port);
        try { listener.Start(); }
        catch (SocketException e) { throw new IOException($"cannot listen on {host}:{port}: {e.Message}"); }
        Console.WriteLine($"Serving metrics on http://{host}:{port} (/json, /metrics)");
        while (true)
        {
            var client = listener.AcceptTcpClient();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                using (client)
                {
                    try
                    {
                        var stream = client.GetStream();
                        var reader = new StreamReader(stream, Encoding.ASCII);
                        var request = reader.ReadLine() ?? "";
                        while (!string.IsNullOrEmpty(reader.ReadLine())) { }
                        var path = request.Split(' ').ElementAtOrDefault(1) ?? "/";
                        Metrics m;
                        lock (gate) m = latest;
                        string status = "200 OK", type = "application/json", body;
                        if (m == null) { status = "503 Service Unavailable"; type = "text/plain"; body = "warming up\n"; }
                        else if (path.StartsWith("/json")) body = json(m) + "\n";
                        else if (path.StartsWith("/metrics")) { type = "text/plain; version=0.0.4"; body = prometheus(m); }
                        else { status = "404 Not Found"; type = "text/plain"; body = "try /json or /metrics\n"; }
                        var bytes = Encoding.UTF8.GetBytes(body);
                        var head = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: {type}\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                        stream.Write(head);
                        stream.Write(bytes);
                    }
                    catch (IOException) { }
                }
            });
        }
    }

    // =====================================================================================
    // mactop
    // =====================================================================================

    static int Mactop(string[] args)
    {
        var interval = 1000.0;
        bool headless = false, pretty = false;
        var count = 0;
        var format = "json";
        int? prom = null;
        var color = "\x1b[32m";
        var fahrenheit = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.IndexOf('=');
            string inline = null;
            if (a.StartsWith("--") && eq > 0) { inline = a[(eq + 1)..]; a = a[..eq]; }
            string Next() => inline ?? (i + 1 < args.Length ? args[++i] : throw new UsageException($"flag needs an argument: {a}"));
            switch (a)
            {
                case "-h" or "--help": MactopHelp(); return 0;
                case "-v" or "--version": Console.WriteLine($"mactop version: v{MactopVersion}"); return 0;
                case "--test" or "-t": Console.WriteLine($"Test input received: {Next()}"); return 0;
                case "-i" or "--interval": interval = Num(Next(), a); break;
                case "--headless": headless = true; break;
                case "--pretty": pretty = true; break;
                case "--count": count = (int)Num(Next(), a); break;
                case "--format": format = Next().ToLowerInvariant(); if (format is not ("json" or "yaml" or "xml" or "csv" or "toon")) throw new UsageException($"unknown format {format}"); break;
                case "-p" or "--prometheus": prom = (int)Num(Next(), a); break;
                case "--foreground" or "-c" or "--color": color = AnsiColor(Next()); break;
                case "--bg" or "--background" or "--lang" or "--unit-network" or "--unit-disk": Next(); break;
                case "--unit-temp": fahrenheit = Next().StartsWith('f'); break;
                case "--dump-temps":
                {
                    using var s = NewSampler(false);
                    PerfSampler.Sleep(0.3);
                    var sample = s.Take();
                    var zones = sample.Instances(@"\Thermal Zone Information(*)\High Precision Temperature");
                    if (zones.Count == 0) zones = sample.Instances(@"\Thermal Zone Information(*)\Temperature").ToDictionary(kv => kv.Key, kv => kv.Value * 10);
                    if (zones.Count == 0) Console.WriteLine("No ACPI thermal zones are exposed on this machine.");
                    foreach (var (zone, tenthsK) in zones) Console.WriteLine($"{zone}: {tenthsK / 10 - 273.15:F1} °C");
                    return 0;
                }
                case "--dump-debug": MacmonDebug(); return 0;
                case "--fan-status": Console.WriteLine("[]"); return 0;   // Windows has no standard fan sensor API
                case "--fan-control" or "--fan-set" or "--fan-auto" or "--fan-id":
                    throw new InvalidOperationException("fan control writes Apple SMC keys; Windows has no standard fan-control API");
                case "--menubar" or "--overlay" or "--dump-fps":
                    throw new InvalidOperationException($"{a} is a macOS window feature; use the terminal UI or --headless on Windows");
                default: throw new UsageException($"flag provided but not defined: {a}");
            }
        }
        if (interval < 100) interval = 100;
        if (prom is int port)
        {
            var server = new Thread(() => Serve("0.0.0.0", port, interval, MactopJson, m => MacmonPrometheus(m).Replace("macmon_", "mactop_"))) { IsBackground = true };
            server.Start();
        }
        if (headless)
        {
            using var sampler = NewSampler(false);
            var prev = sampler.Take();
            var all = new List<Metrics>();
            for (var n = 0; count <= 0 || n < count; n++)
            {
                PerfSampler.Sleep(interval / 1000);
                var cur = sampler.Take();
                var m = Measure(prev, cur);
                prev = cur;
                if (format == "json" && count > 0) { all.Add(m); continue; }   // a finite run prints one JSON array
                Console.WriteLine(Format(m, format, pretty));
            }
            if (all.Count > 0) Console.WriteLine("[" + string.Join(",", all.Select(m => Format(m, "json", pretty))) + "]");
            return 0;
        }
        using (var sampler = NewSampler(true))
        {
            var title = SystemTitle();
            Loop(interval / 1000, 0, sampler, m => MactopFrame(m, title, color, fahrenheit));
        }
        return 0;
    }

    static string AnsiColor(string name) => name.ToLowerInvariant() switch
    {
        "red" => "\x1b[31m", "green" or "lime" => "\x1b[32m", "yellow" or "gold" => "\x1b[33m", "blue" => "\x1b[34m",
        "magenta" or "violet" or "pink" => "\x1b[35m", "skyblue" or "cyan" => "\x1b[36m", "white" or "silver" => "\x1b[37m",
        "orange" => "\x1b[38;5;208m",
        var hex when hex.StartsWith('#') && hex.Length == 7 =>
            $"\x1b[38;2;{Convert.ToInt32(hex[1..3], 16)};{Convert.ToInt32(hex[3..5], 16)};{Convert.ToInt32(hex[5..7], 16)}m",
        _ => "\x1b[32m",
    };

    static bool csvHeader;

    static string MactopJson(Metrics m) => Format(m, "json", false);

    static string Format(Metrics m, string format, bool pretty)
    {
        var e = m.Tiers[0]; var p = m.Tiers[^1];
        var fields = new List<(string Section, string Key, string Value)>
        {
            ("soc_metrics", "cpu_power", J(m.CpuW)), ("soc_metrics", "gpu_power", J(m.GpuW)), ("soc_metrics", "ane_power", "0"),
            ("soc_metrics", "dram_power", J(m.DramW)), ("soc_metrics", "system_power", J(m.PkgW)),
            ("soc_metrics", "total_power", J(Z(m.PkgW) + Z(m.DramW))), ("soc_metrics", "gpu_freq_mhz", "0"),
            ("soc_metrics", "soc_temp", J(m.TempC)), ("soc_metrics", "cpu_temp", J(m.TempC)), ("soc_metrics", "gpu_temp", "null"),
            ("memory", "total", m.RamTotal.ToString()), ("memory", "used", m.RamUsed.ToString()), ("memory", "available", (m.RamTotal - m.RamUsed).ToString()),
            ("memory", "swap_total", m.SwapTotal.ToString()), ("memory", "swap_used", m.SwapUsed.ToString()),
            ("net_disk", "out_packets_per_sec", J(m.NetOutPk)), ("net_disk", "out_bytes_per_sec", J(m.NetOut)),
            ("net_disk", "in_packets_per_sec", J(m.NetInPk)), ("net_disk", "in_bytes_per_sec", J(m.NetIn)),
            ("net_disk", "read_ops_per_sec", J(m.DiskROps)), ("net_disk", "write_ops_per_sec", J(m.DiskWOps)),
            ("net_disk", "read_kbytes_per_sec", J(m.DiskR / 1024)), ("net_disk", "write_kbytes_per_sec", J(m.DiskW / 1024)),
            ("", "cpu_usage", J(m.CpuActive * 100)), ("", "gpu_usage", J(Z(m.Gpu) * 100)),
            ("", "core_usages", "[" + string.Join(",", m.Cores.Select(c => J(c.Active * 100))) + "]"),
            ("system_info", "name", PerfSampler.Json(PerfSampler.CpuName)), ("system_info", "core_count", m.Cores.Count.ToString()),
            ("system_info", "e_core_count", (m.Tiers.Count > 1 ? e.Cores.Count : 0).ToString()), ("system_info", "p_core_count", p.Cores.Count.ToString()),
            ("system_info", "gpu_core_count", "0"),
        };
        if (m.Battery.HasBattery)
        {
            fields.Add(("battery", "percent", m.Battery.Percent.ToString()));
            fields.Add(("battery", "charging", m.Battery.Charging ? "true" : "false"));
        }
        var ts = PerfSampler.Json(m.Time.ToString("yyyy-MM-ddTHH:mm:sszzz", Inv));
        switch (format)
        {
            case "csv":
                var header = csvHeader ? "" : "timestamp," + string.Join(",", fields.Where(f => !f.Value.StartsWith('[')).Select(f => (f.Section == "" ? "" : f.Section + ".") + f.Key)) + "\n";
                csvHeader = true;
                return header + ts.Trim('"') + "," + string.Join(",", fields.Where(f => !f.Value.StartsWith('[')).Select(f => f.Value.Trim('"')));
            case "yaml" or "toon":
            {
                var sb = new StringBuilder($"- timestamp: {ts}\n");
                foreach (var g in fields.GroupBy(f => f.Section))
                {
                    if (g.Key == "") foreach (var f in g) sb.Append($"  {f.Key}: {f.Value}\n");
                    else { sb.Append($"  {g.Key}:\n"); foreach (var f in g) sb.Append($"    {f.Key}: {f.Value}\n"); }
                }
                return sb.ToString().TrimEnd();
            }
            case "xml":
            {
                var sb = new StringBuilder($"<sample><timestamp>{ts.Trim('"')}</timestamp>");
                foreach (var g in fields.GroupBy(f => f.Section))
                {
                    if (g.Key != "") sb.Append($"<{g.Key}>");
                    foreach (var f in g) sb.Append($"<{f.Key}>{System.Security.SecurityElement.Escape(f.Value.Trim('"', '[', ']'))}</{f.Key}>");
                    if (g.Key != "") sb.Append($"</{g.Key}>");
                }
                return sb.Append("</sample>").ToString();
            }
            default:
            {
                var nl = pretty ? "\n" : "";
                string Ind(int n) => pretty ? new string(' ', n * 2) : "";
                var sep = pretty ? ": " : ":";
                var sb = new StringBuilder("{" + nl + Ind(1) + $"\"timestamp\"{sep}{ts}");
                foreach (var g in fields.GroupBy(f => f.Section))
                {
                    if (g.Key == "")
                        foreach (var f in g) sb.Append("," + nl + Ind(1) + $"\"{f.Key}\"{sep}{f.Value}");
                    else
                        sb.Append("," + nl + Ind(1) + $"\"{g.Key}\"{sep}{{" + nl + string.Join("," + nl, g.Select(f => Ind(2) + $"\"{f.Key}\"{sep}{f.Value}")) + nl + Ind(1) + "}");
                }
                return sb.Append(nl + "}").ToString();
            }
        }
        static string J(double v) => PerfSampler.Json(v, 6);
    }

    static string MactopFrame(Metrics m, string title, string color, bool fahrenheit)
    {
        var w = Width;
        var sb = new StringBuilder(color);
        sb.AppendLine(BoxTop("mactop - " + title, w));
        var tierText = string.Join("   ", m.Tiers.Select(t => $"{(t.Label == "_" ? "" : t.Label + "-")}CPU {t.Active * 100:F0}% @ {t.Mhz:F0} MHz ({t.Cores.Count})"));
        sb.AppendLine(BoxLine($"CPU {PerfSampler.Bar(m.CpuActive * 100, 24)} {m.CpuActive * 100,5:F1}%   {tierText}", w));
        var perRow = Math.Max(1, (w - 4) / 14);
        foreach (var chunk in m.Cores.Chunk(perRow))
            sb.AppendLine(BoxLine(string.Join(" ", chunk.Select(c => $"{c.Index,2}{(m.Tiers.Count > 1 ? m.Tiers[c.Tier].Label : " ")} {PerfSampler.Bar(c.Active * 100, 5)}{c.Active * 100,3:F0}%")), w));
        sb.AppendLine(BoxLine($"GPU {PerfSampler.Bar(Z(m.Gpu) * 100, 24)} {Z(m.Gpu) * 100,5:F1}%   VRAM in use {Gb((long)Z(m.GpuMem))} GB", w));
        sb.AppendLine(BoxLine($"MEM {PerfSampler.Bar(100.0 * m.RamUsed / Math.Max(1, m.RamTotal), 24)} {Gb(m.RamUsed)}/{Gb(m.RamTotal)} GB   swap {Gb(m.SwapUsed)}/{Gb(m.SwapTotal)} GB", w));
        var temp = double.IsNaN(m.TempC) ? "n/a" : fahrenheit ? $"{m.TempC * 9 / 5 + 32:F0}°F" : $"{m.TempC:F0}°C";
        sb.AppendLine(BoxLine($"Power  CPU {W(m.CpuW)}  GPU {W(m.GpuW)}  DRAM {W(m.DramW)}  Total {W(m.PkgW)}   Temp {temp}", w));
        sb.AppendLine(BoxLine($"Net    ↓ {Rate(m.NetIn)}  ↑ {Rate(m.NetOut)}     Disk  R {Rate(m.DiskR)}  W {Rate(m.DiskW)}" + (m.Battery.HasBattery ? $"     Battery {m.Battery.Percent}%{(m.Battery.Charging ? " ⚡" : "")}" : ""), w));
        sb.AppendLine(BoxBottom(w) + Reset);
        sb.AppendLine($"{"PID",7} {"USER",-12} {"CPU%",6} {"GPU%",5} {"RES",9} {"VIRT",8}  COMMAND");
        var rows = Console.IsOutputRedirected ? 15 : Math.Max(3, Console.WindowHeight - 12 - (m.Cores.Count + perRow - 1) / perRow);
        foreach (var (p, cpu, gpu) in m.Top.Take(rows))
        {
            var user = PerfSampler.Owner(p.Pid).User;
            if (user.Length > 12) user = user[..12];
            sb.AppendLine($"{p.Pid,7} {user,-12} {cpu / Math.Max(1, m.Cores.Count),6:F1} {gpu,5:F1} {PerfSampler.HumanBytes(p.WorkingSet),9} {p.Virtual / 1073741824.0,7:F1}G  {Path.GetFileNameWithoutExtension(p.Name)}");
        }
        sb.Append("q: quit");
        return sb.ToString();
    }

    static void MactopHelp() => Console.WriteLine("""
        Usage: mactop [options]

        System monitor top for Windows: CPU tiers (E/P cores), GPU, power (RAPL), memory, network and disk.

        Options:
          -h, --help                 Show this help message
          -v, --version              Show the version of mactop
          -i, --interval <ms>        Update interval in milliseconds (default 1000)
              --headless             Output metrics to stdout instead of the TUI
              --count <n>            Number of samples in headless mode (0 = infinite)
              --pretty               Pretty-print JSON in headless mode
              --format <fmt>         Headless output format: json, yaml, xml, csv, toon
          -p, --prometheus <port>    Serve Prometheus metrics on the given port (/metrics, /json)
              --foreground <color>   UI color (green, red, blue, ... or #RRGGBB)
              --unit-temp <unit>     celsius or fahrenheit
              --dump-temps           Print all thermal zone readings and exit
              --dump-debug           Print sensor availability and exit
              --fan-status           Print fan state as JSON and exit
              --test <input>         Echo test input and exit
        """);
}
