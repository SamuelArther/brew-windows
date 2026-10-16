using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WinTools;

// sysprof: a statistical, system-wide profiler. The Windows-native counterpart is the Windows
// Performance Recorder (wpr.exe), which drives the same kind of system-wide ETW CPU/disk/file
// sampling and writes an .etl trace to open in Windows Performance Analyzer. `sysprof record`
// starts a recording, optionally runs a command, and stops into an .etl; `sysprof-cli` is the same.
// Recording needs an elevated prompt (wpr does); --status and --help do not.
static class Sysprof
{
    const string Version = "51.0";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("sysprof", "System-wide profiler (Windows Performance Recorder / ETW)", Run);
        Commands.Add("sysprof-cli", "System-wide profiler, command-line (Windows Performance Recorder / ETW)", Run);
    }

    static int Run(string[] args)
    {
        if (args.Length == 0) { Help(); return 0; }

        string output = "capture.etl";
        var profiles = new List<string>();
        var command = new List<string>();
        var action = "";
        double? duration = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{a} needs a value");
            if (a == "--") { command.AddRange(args[(i + 1)..]); break; }
            switch (a)
            {
                case "-h" or "--help": Help(); return 0;
                case "-V" or "--version": Console.WriteLine($"sysprof {Version} (wintools, Windows Performance Recorder)"); return 0;
                case "record" or "start" or "stop" or "cancel" or "status" or "capture": action = a == "capture" ? "record" : a; break;
                case "-o" or "--output": output = Next(); break;
                case "-p" or "--profile": profiles.Add(Next()); break;
                case "-d" or "--duration": duration = double.Parse(Next()); break;
                case "--command": command.Add(Next()); break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a} (see sysprof --help)");
                    if (action == "") { action = "record"; command.Add(a); }
                    else command.Add(a);
                    break;
            }
        }
        if (action == "") action = "record";

        var wpr = FindWpr();
        if (wpr == null) { Console.Error.WriteLine("sysprof: wpr.exe (Windows Performance Recorder) was not found."); return 1; }

        return action switch
        {
            "status" => Wpr(wpr, "-status", "profiles"),
            "stop" => Wpr(wpr, "-stop", output),
            "cancel" => Wpr(wpr, "-cancel"),
            "start" => StartProfiles(wpr, profiles),
            _ => Record(wpr, output, profiles, command, duration),
        };
    }

    // record: start a system-wide profile, run the command (or wait for the duration / Enter), then
    // stop into the .etl. This is wpr's start/stop cycle, the whole point of the tool.
    static int Record(string wpr, string output, List<string> profiles, List<string> command, double? duration)
    {
        var start = StartProfiles(wpr, profiles);
        if (start != 0) return start;
        try
        {
            if (command.Count > 0)
            {
                Console.WriteLine($"Profiling: {string.Join(' ', command)}");
                RunChild(command);
            }
            else if (duration is double secs)
            {
                Console.WriteLine($"Profiling for {secs:0.##}s. Ctrl+C stops early.");
                PerfSampler.Sleep(secs);
            }
            else
            {
                Console.WriteLine("Profiling system-wide. Press Enter to stop.");
                Console.ReadLine();
            }
        }
        finally { }
        var stop = Wpr(wpr, "-stop", output);
        if (stop == 0) Console.WriteLine($"Wrote {output}. Open it in Windows Performance Analyzer (wpa).");
        return stop;
    }

    static int StartProfiles(string wpr, List<string> profiles)
    {
        var args = new List<string>();
        foreach (var p in profiles.Count > 0 ? profiles : ["CPU", "DiskIO", "FileIO"])
        {
            args.Add("-start");
            args.Add(p);
        }
        return Wpr(wpr, args.ToArray());
    }

    static int Wpr(string wpr, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo(wpr) { UseShellExecute = false };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            p.WaitForExit();
            if (p.ExitCode != 0 && (args.Contains("-start") || args.Contains("-stop")))
                Console.Error.WriteLine("sysprof: recording needs an elevated prompt (run as administrator).");
            return p.ExitCode;
        }
        catch (Win32Exception e) { throw new InvalidOperationException($"could not run wpr: {e.Message}"); }
    }

    static void RunChild(List<string> command)
    {
        try
        {
            var psi = new ProcessStartInfo(command[0]) { UseShellExecute = false };
            for (var i = 1; i < command.Count; i++) psi.ArgumentList.Add(command[i]);
            using var p = Process.Start(psi);
            p.WaitForExit();
        }
        catch (Win32Exception e) { throw new InvalidOperationException($"could not run '{command[0]}': {e.Message}"); }
    }

    static string FindWpr()
    {
        var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var local = Path.Combine(system, "wpr.exe");
        if (File.Exists(local)) return local;
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            try { if (File.Exists(Path.Combine(dir, "wpr.exe"))) return Path.Combine(dir, "wpr.exe"); }
            catch (ArgumentException) { }
        return null;
    }

    static void Help() => Console.WriteLine("""
        Usage: sysprof [COMMAND] [options] [-- program [args...]]

        System-wide statistical profiler for Windows, built on the Windows Performance Recorder
        (wpr.exe) and ETW. Recordings are written as .etl files; open them in Windows Performance
        Analyzer (wpa). Starting or stopping a recording requires an elevated prompt.

        Commands:
          record [-- program]   Start profiling, run the program (or wait), then stop (default)
          start                 Start a profiling session and return
          stop                  Stop the running session and write the trace
          cancel                Discard the running session
          status                Show whether a recording is in progress

        Options:
          -o, --output <file>   Output .etl file (default: capture.etl)
          -p, --profile <name>  wpr profile to record (repeatable; default: CPU DiskIO FileIO)
          -d, --duration <sec>  Record for this many seconds, then stop
          -V, --version         Print version
          -h, --help            Show this help
        """);
}
