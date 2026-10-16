using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text.Json;

namespace WinTools;

// cargo-instruments: `cargo instruments -t <template>` builds a crate's binary with cargo and records
// it with the Windows Performance Recorder (wpr.exe, part of Windows) instead of Xcode's Instruments.
// Instruments templates map to WPR's built-in profiles (time -> CPU, alloc -> Heap + VirtualAllocation,
// ...); any WPR profile name works too. The .etl trace opens in Windows Performance Analyzer when it
// is installed (Windows ADK), the way the original opens Instruments.app.
static class CargoInstruments
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("cargo-instruments", "Profile a Rust crate's binary with Windows Performance Recorder", Run);
    }

    const string Usage = """
        Profile a binary with Windows Performance Recorder (Xcode Instruments on macOS)

        Usage: cargo instruments [OPTIONS] --template <TEMPLATE> [-- <ARGS>...]

        Arguments:
          [ARGS]...  Arguments passed to the target binary

        Options:
          -t, --template <TEMPLATE>      Template to record (see --list-templates); WPR profile names work too
          -l, --list-templates           List available templates
              --bin <NAME>               Binary to run
              --example <NAME>           Example binary to run
              --bench <NAME>             Benchmark target to run
          -p, --package <NAME>           Package to profile
              --release                  Build in release mode
              --profile <NAME>           Build with the given cargo profile
              --features <FEATURES>      Features to activate
              --all-features             Activate all available features
              --no-default-features      Do not activate the default feature
              --manifest-path <PATH>     Path to Cargo.toml
              --target-dir <DIR>         Cargo target directory
              --time-limit <MILLIS>      Stop recording after this many milliseconds
          -o, --output <PATH>            Output .etl file (default target/instruments/<name>_<template>_<time>.etl)
              --open                     Open the trace in Windows Performance Analyzer (the default)
              --no-open                  Do not open the generated trace
          -h, --help                     Print help
          -V, --version                  Print version

        Recording needs an elevated (Administrator) prompt, like any WPR session.
        """;

    // Instruments template (and its abbreviation) -> WPR built-in profiles.
    static readonly (string Abbrev, string Name, string[] Profiles)[] Templates =
    [
        ("time", "Time Profiler", ["CPU"]),
        ("cpu", "CPU Profiler", ["CPU"]),
        ("alloc", "Allocations", ["Heap", "VirtualAllocation"]),
        ("leaks", "Leaks", ["Heap"]),
        ("sys", "System Trace", ["GeneralProfile", "CPU"]),
        ("io", "I/O Activity", ["DiskIO", "FileIO"]),
        ("file", "File Activity", ["FileIO"]),
        ("network", "Network", ["Network"]),
        ("gpu", "Metal System Trace", ["GPU"]),
        ("power", "Energy Log", ["Power"]),
    ];

    static readonly string[] WprProfiles =
    [
        "GeneralProfile", "CPU", "DiskIO", "FileIO", "Registry", "Network", "Heap", "Pool", "VirtualAllocation",
        "Audio", "Video", "Power", "GPU", "Handle", "DesktopComposition", "ReferenceSet", "ResidentSet", "DotNET",
    ];

    sealed class Options
    {
        public string Template, Bin, Example, Bench, Package, Profile, Features, ManifestPath, TargetDir, Output;
        public bool Release, AllFeatures, NoDefaultFeatures, Open = true;
        public int TimeLimit;
        public List<string> TargetArgs = [];
    }

    static int Run(string[] args)
    {
        // cargo runs `cargo-instruments instruments <args>`.
        if (args.Length > 0 && args[0] == "instruments") args = args[1..];
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string value = null;
            var eq = arg.StartsWith("--") ? arg.IndexOf('=') : -1;
            if (eq > 0) { value = arg[(eq + 1)..]; arg = arg[..eq]; }
            string Next() => value ?? (++i < args.Length ? args[i] : throw new UsageException($"a value is required for '{arg}'"));
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(Usage); return 0;
                case "-V" or "--version": Console.WriteLine($"cargo-instruments {Commands.Version} (wintools)"); return 0;
                case "-l" or "--list-templates": ListTemplates(); return 0;
                case "-t" or "--template": o.Template = Next(); break;
                case "--bin": o.Bin = Next(); break;
                case "--example": o.Example = Next(); break;
                case "--bench": o.Bench = Next(); break;
                case "-p" or "--package": o.Package = Next(); break;
                case "--release": o.Release = true; break;
                case "--profile": o.Profile = Next(); break;
                case "--features": o.Features = Next(); break;
                case "--all-features": o.AllFeatures = true; break;
                case "--no-default-features": o.NoDefaultFeatures = true; break;
                case "--manifest-path": o.ManifestPath = Next(); break;
                case "--target-dir": o.TargetDir = Next(); break;
                case "--time-limit":
                    if (!int.TryParse(Next(), out o.TimeLimit) || o.TimeLimit <= 0) throw new UsageException("--time-limit needs a number of milliseconds");
                    break;
                case "-o" or "--output": o.Output = Next(); break;
                case "--open": o.Open = true; break;
                case "--no-open": o.Open = false; break;
                case "--":
                    o.TargetArgs.AddRange(args[(i + 1)..]);
                    i = args.Length;
                    break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unexpected argument '{arg}' found\n\nUsage: cargo instruments [OPTIONS] --template <TEMPLATE> [-- <ARGS>...]");
                    o.TargetArgs.Add(arg);
                    break;
            }
        }
        if (o.Template == null) throw new UsageException("the following required arguments were not provided:\n  --template <TEMPLATE>");
        if (new[] { o.Bin, o.Example, o.Bench }.Count(x => x != null) > 1) throw new UsageException("only one of --bin, --example and --bench can be given");
        var profiles = ResolveTemplate(o.Template)
                       ?? throw new UsageException($"unknown template '{o.Template}' (see cargo instruments --list-templates)");

        var manifest = o.ManifestPath != null ? Path.GetFullPath(o.ManifestPath) : FindManifest(Directory.GetCurrentDirectory());
        if (manifest == null || !File.Exists(manifest))
            throw new InvalidOperationException($"could not find `Cargo.toml` in `{Directory.GetCurrentDirectory()}` or any parent directory");

        var (exe, targetName) = Build(o, manifest);
        Console.Error.WriteLine($"   Profiling {exe} with template '{o.Template}' (WPR: {string.Join(", ", profiles)})");

        var output = o.Output ?? DefaultOutput(o, manifest, targetName);
        output = Path.ChangeExtension(Path.GetFullPath(output), ".etl");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var status = Record(exe, o, profiles, output);
        if (status != 0) return status;
        Console.Error.WriteLine($"Trace file {output}");
        if (o.Open) OpenTrace(output);
        return 0;
    }

    static string[] ResolveTemplate(string template)
    {
        foreach (var (abbrev, name, profiles) in Templates)
            if (template.Equals(abbrev, StringComparison.OrdinalIgnoreCase) || template.Equals(name, StringComparison.OrdinalIgnoreCase))
                return profiles;
        // Raw WPR profiles, optionally several joined by '+' (e.g. CPU+DiskIO).
        var parts = template.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;
        var resolved = new List<string>();
        foreach (var part in parts)
        {
            var match = WprProfiles.FirstOrDefault(p => p.Equals(part, StringComparison.OrdinalIgnoreCase));
            if (match == null) return null;
            resolved.Add(match);
        }
        return [.. resolved];
    }

    static void ListTemplates()
    {
        Console.WriteLine("Instruments templates (recorded with Windows Performance Recorder):\n");
        Console.WriteLine($"{"built-in",-22}{"abbrev",-10}WPR profiles");
        Console.WriteLine(new string('-', 60));
        foreach (var (abbrev, name, profiles) in Templates)
            Console.WriteLine($"{name,-22}{"(" + abbrev + ")",-10}{string.Join(", ", profiles)}");
        Console.WriteLine("\nWPR built-in profiles (usable as templates, combine with '+'):");
        Console.WriteLine("  " + string.Join(", ", WprProfiles));
    }

    static string FindManifest(string dir)
    {
        for (var d = new DirectoryInfo(dir); d != null; d = d.Parent)
        {
            var candidate = Path.Combine(d.FullName, "Cargo.toml");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    // `cargo build --message-format=json-render-diagnostics` prints the built artifacts as JSON on
    // stdout (diagnostics stay human-readable on stderr); pick the executable that was asked for.
    static (string Exe, string Name) Build(Options o, string manifest)
    {
        var cargoArgs = new List<string> { "build", "--message-format=json-render-diagnostics", "--manifest-path", manifest };
        if (o.Release) cargoArgs.Add("--release");
        if (o.Profile != null) cargoArgs.AddRange(["--profile", o.Profile]);
        if (o.Package != null) cargoArgs.AddRange(["--package", o.Package]);
        if (o.Bin != null) cargoArgs.AddRange(["--bin", o.Bin]);
        if (o.Example != null) cargoArgs.AddRange(["--example", o.Example]);
        if (o.Bench != null) cargoArgs.AddRange(["--bench", o.Bench]);
        if (o.Features != null) cargoArgs.AddRange(["--features", o.Features]);
        if (o.AllFeatures) cargoArgs.Add("--all-features");
        if (o.NoDefaultFeatures) cargoArgs.Add("--no-default-features");
        if (o.TargetDir != null) cargoArgs.AddRange(["--target-dir", o.TargetDir]);

        var cargo = Environment.GetEnvironmentVariable("CARGO") ?? "cargo";
        var psi = new ProcessStartInfo(cargo) { UseShellExecute = false, RedirectStandardOutput = true };
        foreach (var a in cargoArgs) psi.ArgumentList.Add(a);
        Process p;
        try { p = Process.Start(psi); }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException("cargo not found on PATH (install Rust from https://rustup.rs)");
        }
        var wantKind = o.Example != null ? "example" : o.Bench != null ? "bench" : "bin";
        var wantName = o.Bin ?? o.Example ?? o.Bench;
        var executables = new List<(string Exe, string Name, string Kind)>();
        using (p)
        {
            string line;
            while ((line = p.StandardOutput.ReadLine()) != null)
            {
                if (!line.StartsWith('{')) continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var r = doc.RootElement;
                    if (r.GetProperty("reason").GetString() != "compiler-artifact") continue;
                    if (!r.TryGetProperty("executable", out var exe) || exe.ValueKind != JsonValueKind.String) continue;
                    var target = r.GetProperty("target");
                    var kind = target.GetProperty("kind").EnumerateArray().Select(k => k.GetString()).FirstOrDefault() ?? "bin";
                    executables.Add((exe.GetString(), target.GetProperty("name").GetString(), kind));
                }
                catch (JsonException) { }
                catch (KeyNotFoundException) { }
            }
            p.WaitForExit();
            if (p.ExitCode != 0) throw new InvalidOperationException("cargo build failed");
        }
        var matches = executables.Where(e => e.Kind == wantKind && (wantName == null || e.Name == wantName)).ToList();
        if (matches.Count == 0)
            throw new InvalidOperationException(wantName != null ? $"no {wantKind} target named `{wantName}` was built"
                                                                 : "no binary target was built (is this a library crate? pass --example or --bench)");
        if (matches.Count > 1)
            throw new UsageException("could not determine which binary to run; use --bin to pick one of: " + string.Join(", ", matches.Select(m => m.Name)));
        return (matches[0].Exe, matches[0].Name);
    }

    static string DefaultOutput(Options o, string manifest, string name)
    {
        var targetDir = o.TargetDir ?? Environment.GetEnvironmentVariable("CARGO_TARGET_DIR") ?? Path.Combine(Path.GetDirectoryName(manifest), "target");
        var template = string.Concat(o.Template.Where(char.IsLetterOrDigit));
        return Path.Combine(targetDir, "instruments", $"{name}_{template}_{DateTime.Now:yyyy-MM-dd-HHmmss}.etl");
    }

    // wpr -start <profile>... -filemode, run the target, wpr -stop <file>. A failed run still saves
    // the trace (useful for crashes); anything that goes wrong before that cancels the session.
    static int Record(string exe, Options o, string[] profiles, string output)
    {
        var wpr = Path.Combine(Environment.SystemDirectory, "wpr.exe");
        if (!File.Exists(wpr)) throw new InvalidOperationException("wpr.exe (Windows Performance Recorder) is not available on this system");
        using (var identity = WindowsIdentity.GetCurrent())
            if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                throw new InvalidOperationException("recording a trace needs an elevated prompt (run the terminal as Administrator)");

        var startArgs = new List<string>();
        foreach (var profile in profiles) startArgs.AddRange(["-start", profile]);
        startArgs.Add("-filemode");
        if (RunTool(wpr, startArgs) != 0)
            throw new InvalidOperationException("wpr could not start recording (is another WPR/xperf session running? `wpr -cancel` stops it)");

        int exitCode;
        try
        {
            var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
            foreach (var a in o.TargetArgs) psi.ArgumentList.Add(a);
            using var target = Process.Start(psi);
            if (o.TimeLimit > 0 && !target.WaitForExit(o.TimeLimit))
            {
                Console.Error.WriteLine($"   Time limit of {o.TimeLimit} ms reached, stopping the target");
                try { target.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            }
            target.WaitForExit();
            exitCode = target.ExitCode;
        }
        catch
        {
            RunTool(wpr, ["-cancel"]);
            throw;
        }
        if (RunTool(wpr, ["-stop", output]) != 0) throw new InvalidOperationException("wpr could not save the trace");
        if (exitCode != 0) Console.Error.WriteLine($"   Target exited with status {exitCode}");
        return 0;
    }

    static int RunTool(string exe, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        p.WaitForExit();
        return p.ExitCode;
    }

    // Windows Performance Analyzer ships with the Windows ADK / Microsoft Store.
    static void OpenTrace(string etl)
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Kits", "10", "Windows Performance Toolkit", "wpa.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Kits", "10", "Windows Performance Toolkit", "wpa.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "wpa.exe"),
        };
        var wpa = candidates.FirstOrDefault(File.Exists);
        if (wpa == null)
        {
            Console.Error.WriteLine("Windows Performance Analyzer is not installed (winget install 9N58QRW40DFW); open the trace with it later.");
            return;
        }
        var psi = new ProcessStartInfo(wpa) { UseShellExecute = false };
        psi.ArgumentList.Add(etl);
        Process.Start(psi)?.Dispose();
    }
}
