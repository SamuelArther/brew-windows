using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace WinTools;

// rem: command-line reminders. macOS keeps them in Reminders.app's EventKit store; Windows has no local
// reminders store, so rem keeps them where Windows can actually remind you: each reminder is a Task
// Scheduler task under \Reminders\<list>\, whose description holds the title and notes. A reminder
// with a due time (`rem add --due ...`) gets a time trigger that pops a toast notification then;
// `rem done` disables the task and records the completion, `rem rm` deletes it. Lists are the task
// folders; new reminders go to "Reminders" unless --list says otherwise.
static class Rem
{
    const string Root = @"\Reminders\";
    const string DefaultList = "Reminders";
    static readonly XNamespace Ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("rem", "Command-line reminders that notify through Task Scheduler", Run);

    sealed record Reminder(string Path, string List, string Title, string Notes, DateTime Created, DateTime? Due, bool Completed);

    static int Run(string[] args)
    {
        var command = args.Length == 0 ? "ls" : args[0];
        switch (command)
        {
            case "help" or "-h" or "--help": Usage(); return 0;
            case "version" or "--version": Console.WriteLine("rem Version 0.01"); return 0;
            case "add": return Add(args[1..]);
            case "notify": return args.Length == 2 ? Notify(args[1]) : throw new UsageException("notify takes a task path");
            case "ls" or "rm" or "cat" or "done": break;
            default:
                Console.Error.WriteLine($"rem: Error unknown command {command}");
                Usage();
                return 255;
        }

        var list = args.Length >= 2 ? args[1] : null;
        var id = args.Length >= 3 ? args[2] : null;
        var lists = Load().Where(r => !r.Completed).GroupBy(r => r.List, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Created).ToList(), StringComparer.OrdinalIgnoreCase);

        if (command == "ls" && list == null) { Print(lists, null); return 0; }
        if (list == null) throw new UsageException($"{command} needs a list and a reminder number (see rem help)");
        if (!lists.TryGetValue(list, out var reminders))
        {
            Console.Error.WriteLine($"rem: Error - Unknown Reminder List: \"{list}\"");
            return 255;
        }
        if (command == "ls") { Print(lists, list); return 0; }
        if (!int.TryParse(id, out var n) || n < 1 || n > reminders.Count)
        {
            Console.Error.WriteLine($"rem: Error - ID Out of Range for Reminder List: {list}");
            return 255;
        }
        var reminder = reminders[n - 1];
        switch (command)
        {
            case "cat": Show(reminder); return 0;
            case "rm":
                var (code, output) = Schtasks("/delete", "/tn", reminder.Path, "/f");
                if (code != 0) Console.Error.WriteLine($"rem: Error removing Reminder ({id}) from list {list}\n\t{output.Trim()}");
                return code == 0 ? 0 : 1;
            default:
                return Complete(reminder, id);
        }
    }

    static void Usage() => Console.WriteLine("""
        Usage:
        	rem <ls> [list]
        		List reminders
        	rem rm [list] [reminder]
        		Remove reminder from list
        	rem add [--list list] [--due when] [--notes text] [reminder]
        		Add reminder to your default list (--due: "2026-10-17 09:00", +30m, +2h, +1d)
        	rem cat [list] [item]
        		Show reminder detail
        	rem done [list] [item]
        		Mark reminder as complete
        	rem help
        		Show this text
        	rem version
        		Show version information
        """);

    // Every reminder, from the XML of all scheduled tasks (each preceded by a <!-- \path --> comment).
    static List<Reminder> Load()
    {
        var (code, xml) = Schtasks("/query", "/xml", "ONE");
        if (code != 0) throw new InvalidOperationException("can't read the Task Scheduler library: " + xml.Trim());
        var result = new List<Reminder>();
        string path = null;
        foreach (var node in XDocument.Parse(xml).Root!.Nodes())
        {
            if (node is XComment comment) { path = comment.Value.Trim(); continue; }
            if (node is not XElement task || path == null || !path.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) continue;
            var reminder = Parse(path, task);
            if (reminder != null) result.Add(reminder);
        }
        return result;
    }

    static Reminder Parse(string path, XElement task)
    {
        var relative = path[Root.Length..];
        var slash = relative.LastIndexOf('\\');
        var list = slash < 0 ? DefaultList : relative[..slash];
        var info = task.Element(Ns + "RegistrationInfo");
        if (info?.Element(Ns + "Source")?.Value != "rem") return null;
        var (title, notes) = SplitDescription(info.Element(Ns + "Description")?.Value ?? relative[(slash + 1)..]);
        var created = DateTime.TryParse(info.Element(Ns + "Date")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var c) ? c : DateTime.MinValue;
        DateTime? due = DateTime.TryParse(task.Descendants(Ns + "StartBoundary").FirstOrDefault()?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
        var completed = info.Element(Ns + "Documentation")?.Value.StartsWith("completed") == true;
        return new Reminder(path, list, title, notes, created, due, completed);
    }

    internal static (string Title, string Notes) SplitDescription(string description)
    {
        var parts = description.Replace("\r\n", "\n").Split("\n\n", 2);
        return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : null);
    }

    static void Print(Dictionary<string, List<Reminder>> lists, string only)
    {
        var sb = new StringBuilder("Reminders\n");
        var names = only != null ? [lists.Keys.First(k => k.Equals(only, StringComparison.OrdinalIgnoreCase))] : lists.Keys.Order(StringComparer.OrdinalIgnoreCase).ToList();
        for (var i = 0; i < names.Count; i++)
            AppendList(sb, names[i], lists[names[i]].Select(r => r.Title).ToList(), i == names.Count - 1);
        Write(sb.ToString());
    }

    // rem's tree: "├── List", "│  ├── 1. title" ... "└── List", "   └── 2. title".
    internal static void AppendList(StringBuilder sb, string name, List<string> titles, bool lastList)
    {
        sb.Append(lastList ? "└──" : "├──").Append(' ').Append(name).Append('\n');
        for (var i = 0; i < titles.Count; i++)
            sb.Append(lastList ? "   " : "│  ").Append(i == titles.Count - 1 ? "└──" : "├──").Append($" {i + 1}. {titles[i]}\n");
    }

    static void Show(Reminder r)
    {
        var sb = new StringBuilder();
        sb.Append($"Reminder: {r.Title}\n\tList: {r.List}\n");
        if (r.Created != DateTime.MinValue) sb.Append($"\tCreated On: {r.Created:yyyy-MM-dd HH:mm}\n");
        if (r.Due is { } due) sb.Append($"\tDue On: {due:yyyy-MM-dd HH:mm}\n");
        if (!string.IsNullOrEmpty(r.Notes)) sb.Append($"\tNotes: {r.Notes}\n");
        Write(sb.ToString());
    }

    // UTF-8 when piped; a console that isn't UTF-8 gets the tree in ASCII.
    static void Write(string text)
    {
        if (!Console.IsOutputRedirected && Console.OutputEncoding.CodePage != 65001)
        {
            Console.Write(text.Replace("├──", "|--").Replace("└──", "`--").Replace("│", "|").Replace("\n", Environment.NewLine));
            return;
        }
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(Encoding.UTF8.GetBytes(text));
    }

    static int Add(string[] args)
    {
        string list = DefaultList, notes = null;
        DateTime? due = null;
        var words = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "--list" or "-l": list = Value(); break;
                case "--notes" or "-n": notes = Value(); break;
                case "--due" or "-d": due = ParseDue(Value(), DateTime.Now); break;
                default: words.Add(args[i]); break;
            }
        }
        var title = string.Join(" ", words).Trim();
        if (title.Length == 0) throw new UsageException("add needs the reminder's text");
        if (list.IndexOfAny(['\\', '/']) >= 0) throw new UsageException("list names can't contain slashes");

        var path = Root + list + "\\" + TaskName(title);
        var xml = TaskXml(path, title, notes, due, DateTime.Now, completed: null);
        var (code, output) = CreateTask(path, xml);
        if (code != 0)
        {
            Console.Error.WriteLine($"rem: Error adding Reminder ({title})\n\t{output.Trim()}");
            return 1;
        }
        return 0;
    }

    // "2026-10-17 09:00", "17:30" (today, or tomorrow if past), or relative "+30m", "+2h", "+1d".
    internal static DateTime ParseDue(string text, DateTime now)
    {
        if (text.Length > 2 && text[0] == '+' && int.TryParse(text[1..^1], out var amount))
        {
            switch (char.ToLowerInvariant(text[^1]))
            {
                case 'm': return now.AddMinutes(amount);
                case 'h': return now.AddHours(amount);
                case 'd': return now.AddDays(amount);
                case 'w': return now.AddDays(7 * amount);
            }
        }
        if (TimeSpan.TryParseExact(text, ["h\\:mm", "hh\\:mm"], CultureInfo.InvariantCulture, out var time))
        {
            var at = now.Date + time;
            return at > now ? at : at.AddDays(1);
        }
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var when)) return when;
        throw new UsageException($"can't read the due date \"{text}\" (try \"2026-10-17 09:00\" or +2h)");
    }

    // Task names can't hold \ / : * ? " < > |; a short suffix keeps same-titled reminders apart.
    internal static string TaskName(string title)
    {
        var clean = new string(title.Select(c => "\\/:*?\"<>|".Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim();
        if (clean.Length > 80) clean = clean[..80].Trim();
        return $"{clean} ({Guid.NewGuid().ToString("N")[..6]})";
    }

    static string TaskXml(string path, string title, string notes, DateTime? due, DateTime created, DateTime? completed)
    {
        var user = $"{Environment.UserDomainName}\\{Environment.UserName}";
        var info = new XElement(Ns + "RegistrationInfo",
            new XElement(Ns + "Date", created.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
            new XElement(Ns + "Author", user),
            new XElement(Ns + "Description", string.IsNullOrEmpty(notes) ? title : title + "\n\n" + notes),
            new XElement(Ns + "Source", "rem"),
            new XElement(Ns + "URI", path));
        if (completed is { } done) info.Add(new XElement(Ns + "Documentation", "completed " + done.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)));
        // No due date, no trigger: the task only holds the reminder.
        var triggers = due is { } at
            ? new XElement(Ns + "Triggers", new XElement(Ns + "TimeTrigger",
                new XElement(Ns + "StartBoundary", at.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                new XElement(Ns + "Enabled", "true")))
            : null;
        // conhost --headless keeps the console window of the notifier from flashing up.
        var conhost = Path.Combine(Environment.SystemDirectory, "conhost.exe");
        var task = new XElement(Ns + "Task", new XAttribute("version", "1.2"),
            info,
            triggers,
            new XElement(Ns + "Principals", new XElement(Ns + "Principal", new XAttribute("id", "Author"),
                new XElement(Ns + "UserId", user),
                new XElement(Ns + "LogonType", "InteractiveToken"),
                new XElement(Ns + "RunLevel", "LeastPrivilege"))),
            new XElement(Ns + "Settings",
                new XElement(Ns + "MultipleInstancesPolicy", "IgnoreNew"),
                new XElement(Ns + "DisallowStartIfOnBatteries", "false"),
                new XElement(Ns + "StopIfGoingOnBatteries", "false"),
                new XElement(Ns + "StartWhenAvailable", "true"),
                new XElement(Ns + "ExecutionTimeLimit", "PT5M"),
                new XElement(Ns + "Enabled", completed == null ? "true" : "false")),
            new XElement(Ns + "Actions", new XAttribute("Context", "Author"),
                new XElement(Ns + "Exec",
                    new XElement(Ns + "Command", conhost),
                    new XElement(Ns + "Arguments", $"--headless {Reattach.Quote(StableExePath())} rem notify {Reattach.Quote(path)}"))));
        return "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" + task + "\r\n";
    }

    // The task outlives this keg version: point it at Homebrew's opt link when there is one.
    static string StableExePath()
    {
        var exe = Environment.ProcessPath ?? "wintools.exe";
        var parts = exe.Split('\\');
        var cellar = Array.FindIndex(parts, p => p.Equals("Cellar", StringComparison.OrdinalIgnoreCase));
        if (cellar > 0 && cellar + 3 < parts.Length)
        {
            var opt = string.Join('\\', parts[..cellar].Append("opt").Append(parts[cellar + 1]).Concat(parts[(cellar + 3)..]));
            if (File.Exists(opt)) return opt;
        }
        return exe;
    }

    static int Complete(Reminder r, string id)
    {
        // Re-registered disabled, with the completion recorded.
        var created = r.Created == DateTime.MinValue ? DateTime.Now : r.Created;
        var (code, output) = CreateTask(r.Path, TaskXml(r.Path, r.Title, r.Notes, r.Due, created, DateTime.Now));
        if (code != 0)
        {
            Console.Error.WriteLine($"rem: Error marking Reminder ({id}) from list {r.List}\n\t{output.Trim()}");
            return 1;
        }
        return 0;
    }

    // Run by the task at the due time: the reminder as a toast.
    static int Notify(string path)
    {
        var (code, xml) = Schtasks("/query", "/tn", path, "/xml", "ONE");
        if (code != 0) throw new InvalidOperationException($"no reminder {path}");
        var reminder = Parse(path, XDocument.Parse(xml).Root!.Descendants(Ns + "Task").FirstOrDefault() ?? XDocument.Parse(xml).Root!)
            ?? throw new InvalidOperationException($"{path} isn't a rem reminder");
        TerminalNotifier.Notify(reminder.List, reminder.Title + (string.IsNullOrEmpty(reminder.Notes) ? "" : "\n" + reminder.Notes));
        return 0;
    }

    static (int Code, string Output) CreateTask(string path, string xml)
    {
        var file = Path.Combine(Path.GetTempPath(), $"rem-{Guid.NewGuid():N}.xml");
        File.WriteAllText(file, xml, Encoding.Unicode);
        try { return Schtasks("/create", "/tn", path, "/xml", file, "/f"); }
        finally { File.Delete(file); }
    }

    static (int Code, string Output) Schtasks(params string[] args)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"))
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEndAsync();
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output + err.Result);
    }
}
