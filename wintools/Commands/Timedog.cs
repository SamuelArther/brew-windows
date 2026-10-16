using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace WinTools;

// timedog: list the files a backup saved. Time Machine's counterpart on Windows is File History,
// which copies changed files to <target>\FileHistory\<user>\<computer>\Data\<drive>\... named
// "file (2026_10_16 09_30_00 UTC).ext"; every distinct timestamp is one backup. timedog compares a
// backup (the latest by default) with the one before it and prints each saved file's size before
// and after, then the totals.
static class Timedog
{
    const string Version = "1.4";

    static readonly Regex Versioned = new(@"^(?<base>.*) \((?<y>\d{4})_(?<mo>\d\d)_(?<d>\d\d) (?<h>\d\d)_(?<mi>\d\d)_(?<s>\d\d) UTC\)(?<ext>\.[^.]*)?$",
                                          RegexOptions.Compiled);

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("timedog", "List the files saved by a File History backup", Run);

    const string Help = """
        Usage: timedog [-d depth] [-l] [-n] [-s] [-m] [machine-dir] [backup]
        List the files File History saved in a backup (default: the latest), compared with the
        backup before it.
          machine-dir  a File History machine folder (<target>\FileHistory\<user>\<computer>);
                       default: this computer's folder on the configured File History drive
          backup       a backup timestamp, YYYY-MM-DD-HHMMSS (UTC), as listed by -b
          -d depth     summarize changes per folder at the given depth
          -l           ignore symlinks (File History never saves them; accepted for compatibility)
          -n           print sizes in bytes instead of K/M/G
          -s           sort by size, largest first
          -m           print the summary only
          -b           list the backups and exit
        """;

    sealed class Item(string path, long size, DateTime stamp)
    {
        public readonly string Path = path;
        public readonly long Size = size;
        public readonly DateTime Stamp = stamp;
    }

    static int Run(string[] args)
    {
        int depth = -1;
        bool numeric = false, bySize = false, summary = false, listBackups = false;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "-v" or "--version": Console.WriteLine($"timedog {Version}"); return 0;
                case "-d":
                    if (++i >= args.Length || !int.TryParse(args[i], out depth) || depth < 0) throw new UsageException("-d needs a depth (0 or more)");
                    break;
                case "-l": break;
                case "-n": numeric = true; break;
                case "-s": bySize = true; break;
                case "-m": summary = true; break;
                case "-b": listBackups = true; break;
                default:
                    if (args[i].StartsWith('-') && args[i].Length > 1) throw new UsageException($"unknown option {args[i]}");
                    positional.Add(args[i]);
                    break;
            }
        }
        if (positional.Count > 2) throw new UsageException("too many arguments (see timedog --help)");

        string machine = null, wanted = null;
        foreach (var p in positional)
        {
            if (machine == null && Directory.Exists(p)) machine = Path.GetFullPath(p);
            else if (wanted == null) wanted = p;
            else throw new UsageException($"{p}: not a folder");
        }
        machine ??= DefaultMachineDirectory();
        if (machine == null || !Directory.Exists(Path.Combine(machine, "Data")))
        {
            Console.Error.WriteLine($"No machine directory found for host {Environment.MachineName}");
            return 1;
        }

        var items = Scan(Path.Combine(machine, "Data"));
        var stamps = items.Select(i => i.Stamp).Distinct().Order().ToList();
        if (stamps.Count == 0)
        {
            Console.Error.WriteLine($"No backups found in {machine}");
            return 1;
        }
        if (listBackups)
        {
            foreach (var s in stamps) Console.WriteLine(Name(s));
            return 0;
        }

        var target = stamps[^1];
        if (wanted != null)
        {
            var match = stamps.Where(s => Name(s) == wanted).ToList();
            if (match.Count == 0)
            {
                Console.Error.WriteLine($"No backup {wanted} in {machine} (timedog -b lists them)");
                return 1;
            }
            target = match[0];
        }
        var index = stamps.IndexOf(target);
        Console.WriteLine(index > 0
            ? $"==> Comparing FH backup {Name(target)} to {Name(stamps[index - 1])}"
            : $"==> FH backup {Name(target)} (the first backup: nothing to compare with)");

        // The latest earlier copy of each file, for the "before" size.
        var previous = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.Where(i => i.Stamp < target).OrderBy(i => i.Stamp)) previous[item.Path] = item.Size;
        var saved = items.Where(i => i.Stamp == target).ToList();

        var rows = new List<(string Path, long Before, long After, int Files)>();
        if (depth >= 0)
        {
            foreach (var group in saved.GroupBy(i => Prefix(i.Path, depth), StringComparer.OrdinalIgnoreCase))
                rows.Add((group.Key, group.Sum(i => previous.GetValueOrDefault(i.Path, 0)), group.Sum(i => i.Size), group.Count()));
        }
        else
        {
            foreach (var i in saved) rows.Add((i.Path, previous.TryGetValue(i.Path, out var b) ? b : -1, i.Size, 1));
        }
        rows = bySize ? rows.OrderByDescending(r => r.After).ToList() : rows.OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase).ToList();

        if (!summary)
            foreach (var r in rows)
            {
                var before = r.Before < 0 ? "" : Size(r.Before, numeric);
                var line = $"{before,9}->{Size(r.After, numeric),9}    {r.Path}";
                if (depth >= 0) line += $" ({r.Files} file{(r.Files == 1 ? "" : "s")})";
                Console.WriteLine(line);
            }
        Console.WriteLine($"==> Total Backup: {saved.Count} changed file{(saved.Count == 1 ? "" : "s")}, {Size(saved.Sum(i => i.Size), numeric).Trim()}");
        return 0;
    }

    static string Name(DateTime stamp) => stamp.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);

    // Data\C\Users\me\file (2026_10_16 09_30_00 UTC).txt -> C:\Users\me\file.txt at that time.
    static List<Item> Scan(string data)
    {
        var list = new List<Item>();
        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(data, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }).ToList(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return list; }
        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            var m = Versioned.Match(name);
            if (!m.Success) continue;
            var stamp = new DateTime(int.Parse(m.Groups["y"].Value), int.Parse(m.Groups["mo"].Value), int.Parse(m.Groups["d"].Value),
                                     int.Parse(m.Groups["h"].Value), int.Parse(m.Groups["mi"].Value), int.Parse(m.Groups["s"].Value), DateTimeKind.Utc);
            var relative = Path.GetRelativePath(data, Path.GetDirectoryName(file));
            var original = Path.Combine(relative == "." ? "" : relative, m.Groups["base"].Value + m.Groups["ext"].Value);
            // The first folder is the drive letter (or "$OF" etc. for other locations).
            if (original.Length > 1 && char.IsLetter(original[0]) && (original.Length == 1 || original[1] == '\\'))
                original = original[0] + ":" + original[1..];
            long size;
            try { size = new FileInfo(file).Length; }
            catch (IOException) { continue; }
            list.Add(new Item(original, size, stamp));
        }
        return list;
    }

    static string Prefix(string path, int depth)
    {
        var parts = path.Split('\\');
        var keep = Math.Min(parts.Length - 1, depth + 1);   // the drive counts as the top level
        return keep <= 0 ? parts[0] : string.Join('\\', parts.Take(keep));
    }

    static string Size(long bytes, bool numeric)
    {
        if (numeric) return bytes.ToString(CultureInfo.InvariantCulture) + "B";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var u = 0;
        while (value >= 1024 && u < units.Length - 1) { value /= 1024; u++; }
        return u == 0 ? $"{bytes}B" : value.ToString("0.0", CultureInfo.InvariantCulture) + units[u];
    }

    // This user's File History target (Config1.xml names it) and this computer's folder on it.
    static string DefaultMachineDirectory()
    {
        var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                  "Microsoft", "Windows", "FileHistory", "Configuration");
        var targets = new List<string>();
        try
        {
            foreach (var xml in Directory.EnumerateFiles(config, "Config*.xml"))
                foreach (Match m in Regex.Matches(File.ReadAllText(xml), @"<TargetUrl>([^<]+)</TargetUrl>"))
                    targets.Add(System.Net.WebUtility.HtmlDecode(m.Groups[1].Value));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        foreach (var target in targets.Distinct())
        {
            var machine = Path.Combine(target, "FileHistory", Environment.UserName, Environment.MachineName);
            if (Directory.Exists(machine)) return machine;
        }
        return null;
    }
}
