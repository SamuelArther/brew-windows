using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace WinTools;

// brightness: get and set display brightness on a 0-1 scale. Built-in laptop panels go through WMI
// (WmiMonitorBrightness / WmiSetBrightness in root\wmi, the same path as the Windows brightness
// slider); external monitors go through DDC/CI (VCP 0x10), shared with ddcutil.
static class Brightness
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("brightness", "Get and set display brightness (built-in panels and DDC/CI monitors)", Run);

    static int Run(string[] args)
    {
        var list = false;
        var verbose = false;
        var mainOnly = false;
        string display = null;
        string level = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: brightness [-m|-d display] [-v] <brightness>
                           or: brightness -l [-v]
                        Brightness is 0 to 1 (e.g. 0.75). -m changes only the main display, -d the display with
                        that number or id from -l; otherwise every display that supports it is changed.
                        Built-in panels use WMI (as the Windows brightness slider does), external monitors DDC/CI.
                        """);
                    return 0;
                case "-l": list = true; break;
                case "-v": verbose = true; break;
                case "-m": mainOnly = true; break;
                case "-d": display = ++i < args.Length ? args[i] : throw new UsageException("-d needs a display number or id"); break;
                default:
                    if (args[i].StartsWith('-') && !double.TryParse(args[i], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                        throw new UsageException($"unknown option {args[i]}");
                    if (level != null) throw new UsageException("only one brightness value may be given");
                    level = args[i];
                    break;
            }
        }
        if (list) return List(verbose);
        if (level == null) throw new UsageException("missing brightness (see brightness --help)");
        if (!double.TryParse(level, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0 || value > 1)
            throw new UsageException($"brightness must be between 0 and 1, not '{level}'");
        return Set(value, mainOnly, display, verbose);
    }

    static int List(bool verbose)
    {
        var panel = PanelBrightness();
        var monitors = Ddcutil.Monitors();
        try
        {
            var screens = Displayplacer.Screens().Where(s => s.Active).ToList();
            foreach (var s in screens)
            {
                var flags = new List<string>();
                if (s.Primary) flags.Add("main");
                flags.Add("active");
                flags.Add("online");
                flags.Add(s.BuiltIn ? "built-in" : "external");
                Console.WriteLine($"display {s.Index}: {string.Join(", ", flags)}, ID {s.PersistentId}");
                if (verbose) Console.WriteLine($"\t{s.Name}, {s.Mode.dmPelsWidth} x {s.Mode.dmPelsHeight} @ ({s.Mode.dmPositionX}, {s.Mode.dmPositionY}), {s.GdiName}");
                var value = Read(s, panel, monitors);
                Console.WriteLine(value is double v
                    ? $"display {s.Index}: brightness {v.ToString("F6", CultureInfo.InvariantCulture)}"
                    : $"display {s.Index}: failed to get brightness of display {s.PersistentId}");
            }
            return 0;
        }
        finally { Ddcutil.Release(monitors); }
    }

    static double? Read(Displayplacer.Screen s, int? panel, List<Ddcutil.Monitor> monitors)
    {
        if (s.BuiltIn) return panel is int p ? p / 100.0 : null;
        var m = monitors.FirstOrDefault(x => x.Screen.GdiName == s.GdiName);
        if (m != null && Ddcutil.TryGet(m, 0x10, out var cur, out var max) && max > 0) return (double)cur / max;
        return null;
    }

    static int Set(double value, bool mainOnly, string display, bool verbose)
    {
        var screens = Displayplacer.Screens().Where(s => s.Active).ToList();
        if (mainOnly) screens = screens.Where(s => s.Primary).ToList();
        if (display != null)
            screens = screens.Where(s => s.Index.ToString(CultureInfo.InvariantCulture) == display
                                         || s.PersistentId.Equals(display, StringComparison.OrdinalIgnoreCase)
                                         || s.GdiName.Equals(display, StringComparison.OrdinalIgnoreCase)).ToList();
        if (screens.Count == 0) throw new InvalidOperationException(display != null ? $"no display {display} (see brightness -l)" : "no displays");
        var monitors = Ddcutil.Monitors();
        try
        {
            var changed = 0;
            foreach (var s in screens)
            {
                if (s.BuiltIn)
                {
                    if (SetPanelBrightness((int)Math.Round(value * 100))) changed++;
                    else Console.Error.WriteLine($"brightness: failed to set brightness of display {s.Index}");
                    continue;
                }
                var m = monitors.FirstOrDefault(x => x.Screen.GdiName == s.GdiName);
                if (m == null || !Ddcutil.TryGet(m, 0x10, out _, out var max) || max == 0)
                {
                    if (display != null || mainOnly) Console.Error.WriteLine($"brightness: display {s.Index} doesn't support brightness over DDC/CI");
                    continue;
                }
                Ddcutil.Set(m, 0x10, (uint)Math.Round(value * max));
                if (verbose) Console.WriteLine($"display {s.Index}: brightness set to {value.ToString("F6", CultureInfo.InvariantCulture)}");
                changed++;
            }
            if (changed == 0) throw new InvalidOperationException("no display supports brightness control");
            return changed == screens.Count || display == null && !mainOnly ? 0 : 1;
        }
        finally { Ddcutil.Release(monitors); }
    }

    // WMI's WmiMonitorBrightness exists only for panels the graphics driver controls (laptops, tablets).
    static int? PanelBrightness()
    {
        var output = PowerShell("(Get-CimInstance -Namespace root/wmi -ClassName WmiMonitorBrightness -ErrorAction Stop | Where-Object Active | Select-Object -First 1).CurrentBrightness");
        return int.TryParse(output?.Trim(), out var percent) ? percent : null;
    }

    static bool SetPanelBrightness(int percent)
    {
        var output = PowerShell($"Get-CimInstance -Namespace root/wmi -ClassName WmiMonitorBrightnessMethods -ErrorAction Stop | Where-Object Active | Invoke-CimMethod -MethodName WmiSetBrightness -Arguments @{{Timeout=[uint32]0; Brightness=[byte]{percent}}} -ErrorAction Stop | Out-Null; 'ok'");
        return output?.Trim() == "ok";
    }

    static string PowerShell(string script)
    {
        var exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
        var info = new ProcessStartInfo(exe) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-Command", script }) info.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(info)!;
            var output = p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit();
            return p.ExitCode == 0 ? output : null;
        }
        catch (System.ComponentModel.Win32Exception) { return null; }
    }
}
