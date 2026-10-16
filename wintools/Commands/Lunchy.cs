using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace WinTools;

// lunchy (lunchy and lunchy-go): launchd-style agents on Task Scheduler. Agent definitions are the
// same .plist files as on macOS, kept in ~/Library/LaunchAgents; "start" (launchctl load) converts
// one to a task under \LaunchAgents\ and runs it, "stop" (launchctl unload) ends and removes the task.
static class Lunchy
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("lunchy", "Manage launchd-style agents (plists) with Task Scheduler", Run);
    }

    const string Help = """
        Usage: lunchy COMMAND [OPTIONS] [PATTERN]
        Friendly launchd-style agents on Windows: plists in ~/Library/LaunchAgents run as Task Scheduler tasks.

          list|ls [PATTERN]          List agent plists
          status|ps [PATTERN]        Show loaded agents (tasks under \LaunchAgents\) and their state
          start [-w] [-F] PATTERN    Load agents into Task Scheduler (runs RunAtLoad/KeepAlive ones now)
          stop PATTERN               End and unload agents
          restart PATTERN            Stop, then start
          install|add FILE           Copy a plist into ~/Library/LaunchAgents
          uninstall PATTERN          Stop agents and delete their plists
          remove|rm PATTERN          Delete agents' plists (unloading them first)
          show [--task] PATTERN      Print the plist (--task: the Task Scheduler XML it becomes)
          edit PATTERN               Open the plist in $EDITOR (default Notepad)

        Plist keys used: Label, Program, ProgramArguments, RunAtLoad, KeepAlive, StartInterval,
        StartCalendarInterval, WorkingDirectory, EnvironmentVariables, StandardOutPath,
        StandardErrorPath, Disabled.
        """;

    const string TaskFolder = @"\LaunchAgents\";

    static string AgentsDir
    {
        get
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home)) home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "LaunchAgents");
        }
    }

    static int Run(string[] args)
    {
        bool write = false, force = false, task = false;
        var rest = new List<string>();
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help" or "help": Console.WriteLine(Help); return 0;
                case "-v" or "--version" or "version": Console.WriteLine($"lunchy {Commands.Version} (wintools)"); return 0;
                case "-w" or "--write": write = true; break;
                case "-F" or "--force": force = true; break;
                case "--task" or "-x": task = true; break;
                default:
                    if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"unknown option {a}");
                    rest.Add(a);
                    break;
            }
        }
        if (rest.Count == 0) { Console.WriteLine(Help); return 1; }
        var verb = rest[0];
        var arg = rest.Count > 1 ? rest[1] : null;
        if (rest.Count > 2) throw new UsageException("too many arguments");
        switch (verb)
        {
            case "list" or "ls":
                foreach (var p in Plists(arg)) Console.WriteLine(Label(p));
                return 0;
            case "status" or "ps": return Status(arg);
            case "start": return Each(arg, p => Start(p, write || force));
            case "stop": return Each(arg, p => { if (Stop(Label(p))) Console.WriteLine($"stopped {Label(p)}"); });
            case "restart":
                return Each(arg, p => { Stop(Label(p)); Start(p, write || force); });
            case "install" or "add": return Install(arg ?? throw new UsageException($"{verb}: plist file required"));
            case "uninstall":
                return Each(arg, p =>
                {
                    if (Stop(Label(p))) Console.WriteLine($"stopped {Label(p)}");
                    File.Delete(p);
                    Console.WriteLine($"uninstalled {Label(p)}");
                });
            case "remove" or "rm":
                return Each(arg, p =>
                {
                    Stop(Label(p));
                    File.Delete(p);
                    Console.WriteLine($"removed {p}");
                });
            case "show":
                return Each(arg, p => Console.Write(task ? TaskXml(Agent.Load(p), write || force) : File.ReadAllText(p)));
            case "edit":
                return Each(arg, p =>
                {
                    var editor = Environment.GetEnvironmentVariable("EDITOR") ?? "notepad.exe";
                    using var proc = Process.Start(new ProcessStartInfo(editor) { ArgumentList = { p }, UseShellExecute = false });
                    proc.WaitForExit();
                });
            default: throw new UsageException($"unknown command '{verb}' (see lunchy --help)");
        }
    }

    // Plists in the agents directory whose label matches the pattern (a case-insensitive regex, as in lunchy).
    static List<string> Plists(string pattern)
    {
        if (!Directory.Exists(AgentsDir)) return [];
        Regex re;
        try { re = pattern == null ? null : new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant); }
        catch (ArgumentException) { throw new UsageException($"invalid pattern '{pattern}'"); }
        return Directory.GetFiles(AgentsDir, "*.plist")
            .Where(p => re == null || re.IsMatch(Label(p)))
            .Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    static string Label(string plist) => Path.GetFileNameWithoutExtension(plist);

    static int Each(string pattern, Action<string> action)
    {
        if (pattern == null) throw new UsageException("a name pattern is required");
        var matches = Plists(pattern);
        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"No daemon found matching '{pattern}'");
            return 1;
        }
        foreach (var p in matches) action(p);
        return 0;
    }

    static int Install(string file)
    {
        if (!File.Exists(file)) throw new IOException($"{file}: No such file");
        Agent.Load(file); // validate before copying
        Directory.CreateDirectory(AgentsDir);
        var dest = Path.Combine(AgentsDir, Path.GetFileName(file));
        File.Copy(file, dest, overwrite: true);
        Console.WriteLine($"{file} installed to {AgentsDir}");
        return 0;
    }

    static void Start(string plist, bool overrideDisabled)
    {
        var agent = Agent.Load(plist);
        if (agent.Disabled && !overrideDisabled)
        {
            Console.Error.WriteLine($"{agent.Label} is disabled (Disabled key); use -w to start it anyway");
            return;
        }
        var xml = Path.Combine(Path.GetTempPath(), $"lunchy-{Guid.NewGuid():N}.xml");
        try
        {
            File.WriteAllText(xml, TaskXml(agent, overrideDisabled), Encoding.Unicode);
            var (code, output) = Schtasks("/create", "/tn", TaskFolder + agent.Label, "/xml", xml, "/f");
            if (code != 0) throw new InvalidOperationException($"{agent.Label}: Task Scheduler refused the task: {output.Trim()}");
        }
        finally { File.Delete(xml); }
        if (agent.RunAtLoad || agent.KeepAlive) Schtasks("/run", "/tn", TaskFolder + agent.Label);
        Console.WriteLine($"started {agent.Label}");
    }

    // Ends and unregisters the task; false when it wasn't loaded.
    static bool Stop(string label)
    {
        if (Schtasks("/query", "/tn", TaskFolder + label).Code != 0) return false;
        Schtasks("/end", "/tn", TaskFolder + label);
        var (code, output) = Schtasks("/delete", "/tn", TaskFolder + label, "/f");
        if (code != 0) throw new InvalidOperationException($"{label}: could not unload: {output.Trim()}");
        return true;
    }

    static int Status(string pattern)
    {
        Regex re = pattern == null ? null : new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var (code, output) = Schtasks("/query", "/fo", "csv", "/nh");
        if (code != 0) throw new InvalidOperationException(output.Trim());
        foreach (var line in output.Split('\n'))
        {
            // "TaskName","Next Run Time","Status"
            var fields = Regex.Matches(line, "\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToList();
            if (fields.Count < 3 || !fields[0].StartsWith(TaskFolder, StringComparison.OrdinalIgnoreCase)) continue;
            var label = fields[0][TaskFolder.Length..];
            if (re != null && !re.IsMatch(label)) continue;
            Console.WriteLine($"{fields[2],-10}\t{label}");
        }
        return 0;
    }

    static (int Code, string Output) Schtasks(params string[] args)
    {
        var psi = new ProcessStartInfo("schtasks.exe")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        var err = p.StandardError.ReadToEndAsync();
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output + err.Result);
    }

    // ---- plist -> Task Scheduler ----

    sealed class Agent
    {
        public string Label, Program, WorkingDirectory, StdOut, StdErr, Path;
        public List<string> Arguments = [];
        public bool RunAtLoad, KeepAlive, Disabled;
        public int StartInterval;
        public List<Dictionary<string, int>> Calendar = [];
        public List<(string, string)> Environment = [];

        public static Agent Load(string path)
        {
            XDocument doc;
            try
            {
                using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
                doc = XDocument.Load(reader);
            }
            catch (XmlException e)
            {
                var binary = File.ReadAllBytes(path).AsSpan().StartsWith("bplist"u8);
                throw new InvalidOperationException(binary
                    ? $"{path}: binary plist; convert it to XML first (plutil -convert xml1 on a Mac)"
                    : $"{path}: not a valid plist ({e.Message})");
            }
            var dict = doc.Root?.Element("dict") ?? throw new InvalidOperationException($"{path}: plist has no top-level <dict>");
            var keys = Dict(dict);
            var a = new Agent { Path = path };
            a.Label = Str(keys, "Label") ?? Label(path);
            a.Program = Str(keys, "Program");
            if (keys.TryGetValue("ProgramArguments", out var pa) && pa.Name == "array")
                a.Arguments = pa.Elements().Select(e => e.Value).ToList();
            if (a.Program == null && a.Arguments.Count > 0) { a.Program = a.Arguments[0]; a.Arguments.RemoveAt(0); }
            else if (a.Program != null && a.Arguments.Count > 0) a.Arguments.RemoveAt(0); // argv[0]
            if (a.Program == null) throw new InvalidOperationException($"{path}: neither Program nor ProgramArguments is set");
            a.RunAtLoad = Bool(keys, "RunAtLoad");
            // KeepAlive may be a bool or a dict of conditions; any dict means "keep it running".
            a.KeepAlive = keys.TryGetValue("KeepAlive", out var ka) && (ka.Name == "true" || ka.Name == "dict");
            a.Disabled = Bool(keys, "Disabled");
            a.WorkingDirectory = Expand(Str(keys, "WorkingDirectory"));
            a.StdOut = Expand(Str(keys, "StandardOutPath"));
            a.StdErr = Expand(Str(keys, "StandardErrorPath"));
            if (keys.TryGetValue("StartInterval", out var si) && int.TryParse(si.Value, out var interval)) a.StartInterval = interval;
            if (keys.TryGetValue("StartCalendarInterval", out var sc))
            {
                var entries = sc.Name == "array" ? sc.Elements("dict") : [sc];
                foreach (var entry in entries)
                    a.Calendar.Add(Dict(entry).Where(kv => int.TryParse(kv.Value.Value, out _)).ToDictionary(kv => kv.Key, kv => int.Parse(kv.Value.Value)));
            }
            if (keys.TryGetValue("EnvironmentVariables", out var env) && env.Name == "dict")
                a.Environment = Dict(env).Select(kv => (kv.Key, kv.Value.Value)).ToList();
            a.Program = Expand(a.Program);
            return a;
        }

        // <key>K</key><value/> pairs of a <dict>.
        static Dictionary<string, XElement> Dict(XElement dict)
        {
            var result = new Dictionary<string, XElement>(StringComparer.Ordinal);
            string key = null;
            foreach (var e in dict.Elements())
            {
                if (e.Name == "key") key = e.Value;
                else if (key != null) { result[key] = e; key = null; }
            }
            return result;
        }

        static string Str(Dictionary<string, XElement> d, string k) => d.TryGetValue(k, out var e) && e.Name == "string" ? e.Value : null;
        static bool Bool(Dictionary<string, XElement> d, string k) => d.TryGetValue(k, out var e) && e.Name == "true";

        static string Expand(string s) =>
            s == null ? null :
            s.StartsWith('~') ? System.IO.Path.GetFullPath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + s[1..]) :
            System.Environment.ExpandEnvironmentVariables(s);
    }

    static string TaskXml(Agent a, bool overrideDisabled)
    {
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        var user = $"{Environment.UserDomainName}\\{Environment.UserName}";
        var now = DateTime.Now;
        var triggers = new XElement(ns + "Triggers");
        if (a.RunAtLoad || a.KeepAlive)
            triggers.Add(new XElement(ns + "LogonTrigger", new XElement(ns + "Enabled", "true"), new XElement(ns + "UserId", user)));
        if (a.StartInterval > 0)
        {
            // Task Scheduler's shortest repetition is one minute.
            var seconds = Math.Max(60, a.StartInterval);
            triggers.Add(new XElement(ns + "TimeTrigger",
                new XElement(ns + "Repetition", new XElement(ns + "Interval", $"PT{seconds}S"), new XElement(ns + "StopAtDurationEnd", "false")),
                new XElement(ns + "StartBoundary", now.AddSeconds(seconds).ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement(ns + "Enabled", "true")));
        }
        foreach (var c in a.Calendar) triggers.Add(CalendarTrigger(ns, c, now));

        var (command, arguments) = CommandLine(a);
        var exec = new XElement(ns + "Exec", new XElement(ns + "Command", command));
        if (arguments.Length > 0) exec.Add(new XElement(ns + "Arguments", arguments));
        if (a.WorkingDirectory != null) exec.Add(new XElement(ns + "WorkingDirectory", a.WorkingDirectory));

        var settings = new XElement(ns + "Settings",
            new XElement(ns + "MultipleInstancesPolicy", "IgnoreNew"),
            new XElement(ns + "DisallowStartIfOnBatteries", "false"),
            new XElement(ns + "StopIfGoingOnBatteries", "false"),
            new XElement(ns + "ExecutionTimeLimit", "PT0S"),
            new XElement(ns + "Enabled", a.Disabled && !overrideDisabled ? "false" : "true"));
        // KeepAlive: Task Scheduler can only restart a task that exits with an error.
        if (a.KeepAlive)
            settings.Add(new XElement(ns + "RestartOnFailure", new XElement(ns + "Interval", "PT1M"), new XElement(ns + "Count", "999")));

        var task = new XElement(ns + "Task", new XAttribute("version", "1.2"),
            new XElement(ns + "RegistrationInfo",
                new XElement(ns + "Description", $"launchd agent {a.Label} ({a.Path})"),
                new XElement(ns + "URI", TaskFolder + a.Label)),
            triggers,
            new XElement(ns + "Principals", new XElement(ns + "Principal", new XAttribute("id", "Author"),
                new XElement(ns + "UserId", user),
                new XElement(ns + "LogonType", "InteractiveToken"),
                new XElement(ns + "RunLevel", "LeastPrivilege"))),
            settings,
            new XElement(ns + "Actions", new XAttribute("Context", "Author"), exec));
        return "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" + task.ToString() + "\r\n";
    }

    // launchd's StartCalendarInterval: missing keys are wildcards (Minute defaults to every minute,
    // which nobody means; treat it as 0). Weekday 0/7 = Sunday.
    static XElement CalendarTrigger(XNamespace ns, Dictionary<string, int> c, DateTime now)
    {
        var minute = c.GetValueOrDefault("Minute", 0);
        var hasHour = c.TryGetValue("Hour", out var hour);
        var start = now.Date.AddHours(hasHour ? hour : 0).AddMinutes(minute);
        // The schema wants Repetition, StartBoundary, Enabled, then the schedule, in that order.
        var trigger = new XElement(ns + "CalendarTrigger");
        if (!hasHour)
            trigger.Add(new XElement(ns + "Repetition", new XElement(ns + "Interval", "PT1H"), new XElement(ns + "Duration", "P1D")));
        trigger.Add(new XElement(ns + "StartBoundary", start.ToString("yyyy-MM-ddTHH:mm:ss")));
        trigger.Add(new XElement(ns + "Enabled", "true"));
        if (c.TryGetValue("Weekday", out var weekday))
        {
            string[] days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
            trigger.Add(new XElement(ns + "ScheduleByWeek",
                new XElement(ns + "DaysOfWeek", new XElement(ns + days[weekday % 7])),
                new XElement(ns + "WeeksInterval", "1")));
        }
        else if (c.TryGetValue("Day", out var day))
        {
            string[] months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
            var monthList = c.TryGetValue("Month", out var m) ? [months[(m + 11) % 12]] : months;
            trigger.Add(new XElement(ns + "ScheduleByMonth",
                new XElement(ns + "DaysOfMonth", new XElement(ns + "Day", day)),
                new XElement(ns + "Months", monthList.Select(name => new XElement(ns + name)))));
        }
        else trigger.Add(new XElement(ns + "ScheduleByDay", new XElement(ns + "DaysInterval", "1")));
        return trigger;
    }

    // The program runs directly unless the plist asks for environment variables or output
    // redirection, which need cmd.exe in between.
    static (string Command, string Arguments) CommandLine(Agent a)
    {
        var args = string.Join(" ", a.Arguments.Select(Quote));
        if (a.Environment.Count == 0 && a.StdOut == null && a.StdErr == null) return (a.Program, args);
        var sb = new StringBuilder("/d /s /c \"");
        foreach (var (k, v) in a.Environment) sb.Append($"set \"{k}={v}\"&& ");
        sb.Append(Quote(a.Program));
        if (args.Length > 0) sb.Append(' ').Append(args);
        if (a.StdOut != null) sb.Append($" >> {Quote(a.StdOut)}");
        if (a.StdErr != null) sb.Append(a.StdErr == a.StdOut ? " 2>&1" : $" 2>> {Quote(a.StdErr)}");
        sb.Append('"');
        return (Path.Combine(Environment.SystemDirectory, "cmd.exe"), sb.ToString());
    }

    // Windows command-line quoting (CommandLineToArgvW rules).
    static string Quote(string s)
    {
        if (s.Length > 0 && s.IndexOfAny([' ', '\t', '"']) < 0) return s;
        var sb = new StringBuilder("\"");
        var slashes = 0;
        foreach (var ch in s)
        {
            if (ch == '\\') { slashes++; continue; }
            sb.Append('\\', ch == '"' ? slashes * 2 + 1 : slashes);
            slashes = 0;
            sb.Append(ch);
        }
        sb.Append('\\', slashes * 2).Append('"');
        return sb.ToString();
    }
}
