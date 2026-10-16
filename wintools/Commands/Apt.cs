using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// apt / apt-get / apt-cache: Debian's package tools on top of winget, Windows' package manager.
// Package names are winget ids (Git.Git) or names; the "archive" in apt's output is the winget source.
static class Apt
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("apt", "Install and manage packages with apt's commands (winget)", args => Run("apt", args));
        Commands.Add("apt-get", "apt-get's package commands on top of winget", args => Run("apt-get", args));
        Commands.Add("apt-cache", "Search and show packages with apt-cache's commands (winget)", args => Run("apt-cache", args));
    }

    sealed class Options
    {
        public bool Yes, Simulate, Quiet, Installed, Upgradable;
        public string Command;
        public readonly List<string> Packages = [];
    }

    static int Run(string tool, string[] args)
    {
        var o = new Options();
        var options = true;
        foreach (var arg in args)
        {
            if (options && arg == "--") { options = false; continue; }
            if (options && arg.StartsWith('-') && arg.Length > 1)
            {
                switch (arg)
                {
                    case "-h" or "--help": Console.WriteLine(Help(tool)); return 0;
                    case "-v" or "--version": Console.WriteLine($"{tool} {Commands.Version} (winget)"); return 0;
                    case "-y" or "--yes" or "--assume-yes": o.Yes = true; break;
                    case "-s" or "--simulate" or "--dry-run" or "--just-print" or "--no-act": o.Simulate = true; break;
                    case "-q" or "-qq" or "--quiet": o.Quiet = true; break;
                    case "--installed": o.Installed = true; break;
                    case "--upgradable" or "--upgradeable": o.Upgradable = true; break;
                    // Recommends, fixing and the like have no winget meaning.
                    case "--no-install-recommends" or "--install-recommends" or "--install-suggests" or "-f" or "--fix-broken"
                        or "--fix-missing" or "-m" or "--allow-downgrades" or "--allow-unauthenticated" or "--no-upgrade" or "--only-upgrade"
                        or "-a" or "--all-versions" or "--full" or "--names-only" or "--purge" or "--auto-remove" or "--autoremove":
                        break;
                    default: throw new UsageException($"Command line option '{arg}' is not understood");
                }
                continue;
            }
            if (o.Command == null) o.Command = arg;
            else o.Packages.Add(arg);
        }
        if (o.Command == null) { Console.WriteLine(Help(tool)); return 1; }

        var command = (tool, o.Command) switch
        {
            ("apt-cache", "search" or "show" or "policy" or "madison" or "showpkg") => o.Command,
            ("apt-cache", _) => throw new UsageException($"Invalid operation {o.Command}"),
            (_, "update" or "upgrade" or "full-upgrade" or "dist-upgrade" or "install" or "reinstall" or "remove" or "purge"
                or "autoremove" or "autopurge" or "clean" or "autoclean" or "check") => o.Command,
            ("apt", "search" or "show" or "list" or "policy") => o.Command,
            _ => throw new UsageException($"Invalid operation {o.Command}"),
        };

        switch (command)
        {
            case "autoremove" or "autopurge":
                Console.WriteLine("0 upgraded, 0 newly installed, 0 to remove and 0 not upgraded.");
                Console.WriteLine($"{tool}: winget doesn't track automatically installed dependencies, so there is nothing to remove");
                return 0;
            case "clean" or "autoclean":
                Console.WriteLine($"{tool}: winget keeps no package cache (installers are downloaded to %TEMP% and removed)");
                return 0;
            case "check":
                Winget.Require(tool);
                Console.WriteLine("Reading package lists... Done");
                return 0;
        }

        // --simulate only prints the winget commands of changes, so it works without winget.
        if (!o.Simulate || command is not ("update" or "upgrade" or "full-upgrade" or "dist-upgrade" or "install" or "reinstall" or "remove" or "purge")) Winget.Require(tool);
        var agree = o.Yes ? new[] { "--accept-package-agreements", "--accept-source-agreements" } : [];
        switch (command)
        {
            case "update":
                return Exec(o, tool, ["source", "update"]);

            case "upgrade" or "full-upgrade" or "dist-upgrade":
            {
                if (o.Packages.Count > 0)
                    return ForEach(o, tool, p => ["upgrade", .. Select(p), .. agree]);
                if (!o.Yes && !o.Simulate)
                {
                    var rows = Winget.Table(tool, "upgrade");
                    if (rows.Count == 0) { Console.WriteLine("0 upgraded, 0 newly installed, 0 to remove and 0 not upgraded."); return 0; }
                    Console.WriteLine("The following packages will be upgraded:");
                    Console.WriteLine("  " + string.Join(' ', rows.Select(r => r.Id)));
                    Console.WriteLine($"{rows.Count} upgraded, 0 newly installed, 0 to remove and 0 not upgraded.");
                    if (!Confirm()) { Console.WriteLine("Abort."); return 1; }
                }
                return Exec(o, tool, ["upgrade", "--all", "--accept-package-agreements", "--accept-source-agreements"]);
            }

            case "install" or "reinstall":
                if (o.Packages.Count == 0) throw new UsageException("no packages given");
                if (!o.Yes && !o.Simulate)
                {
                    Console.WriteLine("The following NEW packages will be installed:");
                    Console.WriteLine("  " + string.Join(' ', o.Packages));
                    if (!Confirm()) { Console.WriteLine("Abort."); return 1; }
                }
                return ForEach(o, tool, p => ["install", .. Select(p), .. command == "reinstall" ? new[] { "--force" } : [],
                    "--accept-package-agreements", "--accept-source-agreements"]);

            case "remove" or "purge":
                if (o.Packages.Count == 0) throw new UsageException("no packages given");
                if (!o.Yes && !o.Simulate)
                {
                    Console.WriteLine("The following packages will be REMOVED:");
                    Console.WriteLine("  " + string.Join(' ', o.Packages));
                    if (!Confirm()) { Console.WriteLine("Abort."); return 1; }
                }
                return ForEach(o, tool, p => ["uninstall", .. Select(p), .. command == "purge" ? new[] { "--purge" } : []]);

            case "search":
            {
                if (o.Packages.Count == 0) throw new UsageException("the search command requires a pattern");
                if (!o.Quiet) { Console.WriteLine("Sorting... Done"); Console.WriteLine("Full Text Search... Done"); }
                var rows = Winget.Table(tool, ["search", string.Join(' ', o.Packages)]);
                foreach (var r in rows)
                {
                    Console.WriteLine($"{r.Id}/{r.Source ?? "winget"} {r.Version}");
                    Console.WriteLine($"  {r.Name}");
                    Console.WriteLine();
                }
                return 0;
            }

            case "list":
            {
                Console.WriteLine("Listing... Done");
                var installed = Winget.Table(tool, "list");
                IEnumerable<Winget.Row> rows;
                if (o.Upgradable) rows = installed.Where(r => !string.IsNullOrEmpty(r.Available));
                else if (o.Installed || o.Packages.Count == 0) rows = installed;
                else rows = Winget.Table(tool, ["search", string.Join(' ', o.Packages)]);
                var installedIds = installed.Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var r in rows)
                {
                    if (o.Packages.Count > 0 && !o.Packages.Any(p => Glob(p, r.Id) || Glob(p, r.Name))) continue;
                    var state = !string.IsNullOrEmpty(r.Available) ? $"[upgradable from: {r.Version}]"
                        : installedIds.Contains(r.Id) ? "[installed]" : "";
                    var version = string.IsNullOrEmpty(r.Available) ? r.Version : r.Available;
                    Console.WriteLine($"{r.Id}/{r.Source ?? "local"} {version} {state}".TrimEnd());
                }
                return 0;
            }

            case "show" or "showpkg":
            {
                if (o.Packages.Count == 0) throw new UsageException("no packages found");
                var status = 0;
                foreach (var p in o.Packages)
                {
                    var (code, lines) = Winget.Capture(["show", .. Select(p)]);
                    if (code != 0) { Console.Error.WriteLine($"N: Unable to locate package {p}"); status = 100; continue; }
                    Console.WriteLine($"Package: {p}");
                    // winget prints "Found Name [Id]" and then "Key: value" fields, which is apt's shape already.
                    foreach (var line in lines.SkipWhile(l => !l.StartsWith("Found ")).Skip(1))
                        Console.WriteLine(ShowField(line));
                    Console.WriteLine();
                }
                return status;
            }

            case "policy" or "madison":
            {
                if (o.Packages.Count == 0) { Console.WriteLine("Package files:\n 100 winget\n 100 msstore"); return 0; }
                var installed = Winget.Table(tool, "list");
                foreach (var p in o.Packages)
                {
                    var mine = installed.FirstOrDefault(r => r.Id.Equals(p, StringComparison.OrdinalIgnoreCase) || r.Name.Equals(p, StringComparison.OrdinalIgnoreCase));
                    var found = Winget.Table(tool, ["search", .. Select(p)]).FirstOrDefault();
                    if (command == "madison")
                    {
                        if (found != null) Console.WriteLine($"{found.Id,10} | {found.Version} | {found.Source ?? "winget"}");
                        continue;
                    }
                    Console.WriteLine($"{p}:");
                    Console.WriteLine($"  Installed: {mine?.Version ?? "(none)"}");
                    Console.WriteLine($"  Candidate: {(string.IsNullOrEmpty(mine?.Available) ? found?.Version ?? mine?.Version ?? "(none)" : mine.Available)}");
                }
                return 0;
            }
        }
        return 0;
    }

    // An id-looking name (Publisher.Package) is matched exactly by id, anything else by name.
    static string[] Select(string package)
    {
        var version = "";
        var eq = package.IndexOf('=');
        if (eq > 0) { version = package[(eq + 1)..]; package = package[..eq]; }
        string[] query = package.Contains('.') ? ["--id", package, "--exact"] : ["--query", package];
        return version.Length > 0 ? [.. query, "--version", version] : query;
    }

    static int ForEach(Options o, string tool, Func<string, string[]> build)
    {
        var status = 0;
        foreach (var p in o.Packages)
            if (Exec(o, tool, build(p)) != 0) status = 100;
        return status;
    }

    static int Exec(Options o, string tool, string[] args)
    {
        if (o.Simulate)
        {
            Console.WriteLine("winget " + string.Join(' ', args.Select(Sandbox.Quote)));
            return 0;
        }
        return Winget.Pass(args) == 0 ? 0 : 100;
    }

    static bool Confirm()
    {
        Console.Write("Do you want to continue? [Y/n] ");
        if (Console.IsInputRedirected) { Console.WriteLine(); return false; }
        var answer = Console.ReadLine()?.Trim() ?? "";
        return answer.Length == 0 || answer.StartsWith('y') || answer.StartsWith('Y');
    }

    static string ShowField(string line)
    {
        var colon = line.IndexOf(':');
        if (colon <= 0 || line.StartsWith(' ')) return line;
        return line[..colon] switch
        {
            "Publisher" => "Maintainer" + line[colon..],
            "Publisher Url" or "Homepage" => "Homepage" + line[colon..],
            _ => line,
        };
    }

    // apt list takes shell globs (apt list 'python*').
    static bool Glob(string pattern, string text)
    {
        if (!pattern.Contains('*') && !pattern.Contains('?')) return text.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    static string Help(string tool) => tool switch
    {
        "apt-cache" => """
            Usage: apt-cache [options] command

            apt-cache queries the winget package sources.

            Commands:
              search - Search the package list for a pattern
              show - Show package details
              policy - Show the installed and candidate versions
              madison - Show available versions
            """,
        _ => $"""
            Usage: {tool} [options] command

            {tool} installs and manages Windows packages through winget. Packages are winget ids
            (Git.Git, Microsoft.PowerShell) or names; pkg=version picks a version.

            Commands:
              {(tool == "apt" ? "list - list packages (--installed, --upgradable)\n  search - search in package descriptions\n  show - show package details\n  " : "")}install - install packages
              reinstall - reinstall packages
              remove - remove packages
              purge - remove packages and their data
              update - update the package sources
              upgrade - upgrade the system by installing/upgrading packages
              full-upgrade - same as upgrade
              autoremove, clean - nothing to do: winget keeps no orphans or cache

            Options:
              -y, --yes        assume yes; also accepts package and source agreements
              -s, --simulate   print the winget commands instead of running them
              -q, --quiet      less output
            """,
    };
}

// winget, run with its output captured and parsed (shared by apt and mas).
static class Winget
{
    internal sealed class Row
    {
        public string Name, Id, Version, Available, Source;
    }

    internal static string Path => Sandbox.FindProgram("winget.exe");

    internal static void Require(string tool)
    {
        if (Path == null) throw new InvalidOperationException("winget isn't installed (get App Installer from the Microsoft Store)");
    }

    // Runs winget on the console.
    internal static int Pass(string[] args)
    {
        var start = new ProcessStartInfo(Path) { UseShellExecute = false };
        foreach (var a in args) start.ArgumentList.Add(a);
        using var p = Process.Start(start);
        p.WaitForExit();
        return p.ExitCode;
    }

    // Runs winget non-interactively and returns its output lines without the progress spinners.
    internal static (int Code, List<string> Lines) Capture(string[] args)
    {
        var start = new ProcessStartInfo(Path)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) start.ArgumentList.Add(a);
        start.ArgumentList.Add("--disable-interactivity");
        start.ArgumentList.Add("--accept-source-agreements");
        using var p = Process.Start(start);
        var stderr = p.StandardError.ReadToEndAsync();
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        _ = stderr.Result;
        var lines = new List<string>();
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var cr = line.LastIndexOf('\r');
            if (cr >= 0) line = line[(cr + 1)..];
            line = line.TrimEnd();
            if (line.Trim() is "-" or "\\" or "|" or "/") continue;
            if (line.Contains('█') || line.Contains('▒')) continue;   // download progress bars
            lines.Add(line);
        }
        return (p.ExitCode, lines);
    }

    internal static List<Row> Table(string tool, params string[] args)
    {
        var (_, lines) = Capture(args);
        return Parse(lines);
    }

    // winget's tables: a header row, a row of dashes, then rows aligned to the header's columns.
    internal static List<Row> Parse(List<string> lines)
    {
        var rows = new List<Row>();
        var dashes = lines.FindIndex(l => l.Length >= 10 && l.All(c => c == '-'));
        if (dashes < 1) return rows;
        var header = lines[dashes - 1];
        var starts = new List<int>();
        for (var i = 0; i < header.Length; i++)
            if (header[i] != ' ' && (i == 0 || header[i - 1] == ' ')) starts.Add(i);
        if (starts.Count < 2) return rows;
        string Cell(string line, int column)
        {
            var from = starts[column];
            if (from >= line.Length) return "";
            var to = column + 1 < starts.Count ? Math.Min(starts[column + 1], line.Length) : line.Length;
            return line[from..to].Trim();
        }
        foreach (var line in lines.Skip(dashes + 1))
        {
            if (line.Length <= starts[1] || line[starts[1] - 1] != ' ') break;   // footer ("15 upgrades available.")
            var row = new Row { Name = Cell(line, 0), Id = Cell(line, 1) };
            if (starts.Count > 2) row.Version = Cell(line, 2);
            // list/upgrade: Name Id Version Available Source; search: Name Id Version [Match] Source.
            var names = Enumerable.Range(0, starts.Count).Select(c => HeaderName(header, starts, c)).ToList();
            for (var c = 3; c < starts.Count; c++)
            {
                var value = Cell(line, c);
                if (c == starts.Count - 1 && value is "winget" or "msstore") row.Source = value;
                else if (c == 3 && names[c].StartsWith("Avail", StringComparison.OrdinalIgnoreCase)) row.Available = value;
            }
            if (row.Id.Length > 0) rows.Add(row);
        }
        return rows;
    }

    static string HeaderName(string header, List<int> starts, int column) =>
        header[starts[column]..(column + 1 < starts.Count ? starts[column + 1] : header.Length)].Trim();
}
