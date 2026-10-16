using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.ApplicationModel.Appointments;
using Windows.ApplicationModel.UserDataTasks;

namespace WinTools;

// icalBuddy (ical-buddy): events and tasks from the Windows calendar database - the appointment
// and task stores behind the Windows Calendar/Outlook accounts (Outlook.com, Microsoft 365,
// Google, iCloud...) - through WinRT, printed in icalBuddy's formats with its main options.
static class IcalBuddy
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("icalBuddy", "Get events and tasks from the Windows calendar database", Run);
    }

    const string Help = """
        USAGE: icalBuddy [options] <command>

        <command> specifies the general action icalBuddy should take:

          'eventsToday'               Print events occurring today
          'eventsToday+NUM'           Print events occurring between today and NUM days into the future
          'eventsNow'                 Print events occurring at present time
          'eventsFrom:START to:END'   Print events occurring between the two specified dates
          'uncompletedTasks'          Print uncompleted tasks
          'undatedUncompletedTasks'   Print uncompleted tasks that have no due date
          'tasksDueBefore:DATE'       Print uncompleted tasks that are due before the given date
          'calendars'                 Print all calendars (and task lists)
          'strEncodings'              Print all the possible string encodings

        Some of the [options] you can use are:
          -V            Print version number and exit
          -sc           Separate by calendar
          -sd           Separate by date (events only)
          -sed          Show empty dates (with -sd)
          -std          Separate tasks by due date
          -f            Format output (ANSI colors and bold)
          -nc           No calendar names
          -nrd          No relative dates
          -npn          No property names
          -n            Include only events from now on
          -ea           Exclude all-day events
          -eed          Exclude end datetimes
          -uid          Show event/task UIDs
          -li NUM       Limit items (to NUM)
          -ic CALS      Include only these calendars/lists (comma-separated names or UIDs)
          -ec CALS      Exclude these calendars/lists
          -iep PROPS    Include only these properties (title, location, notes, url, attendees,
                        datetime, priority, uid)
          -eep PROPS    Exclude these properties
          -po PROPS     Property order
          -b STR        Bullet point for items (default "• ")
          -ab STR       Bullet point for high-priority/alarm items (default "! ")
          -ss STR       Section separator (default "------------------------")
          -ps "/SEPS/"  Property separators, delimited by their first character (default "\n    ")
          -tf FMT       Time format (strftime, default %H:%M)
          -df FMT       Date format (strftime, default %Y-%m-%d)
          -nnr STR      Replace newlines in property values with STR
          --strEncoding NAME   Output string encoding (see strEncodings)

        Dates for eventsFrom/to and tasksDueBefore: YYYY-MM-DD [HH:MM[:SS]], today, tomorrow,
        yesterday or today+N / today-N.
        """;

    sealed class Options
    {
        public bool SepCalendar, SepDate, ShowEmptyDates, SepTaskDue, Format, NoCalNames, NoRelDates, NoPropNames,
            FromNow, ExcludeAllDay, ExcludeEnd, ShowUid;
        public int Limit = int.MaxValue;
        public List<string> IncludeCals, ExcludeCals, IncludeProps, ExcludeProps, PropOrder;
        public string Bullet = "• ", AlertBullet = "! ", SectionSep = "------------------------", TimeFormat = "%H:%M",
            DateFormat = "%Y-%m-%d", NewlineReplacement, Encoding;
        public List<string> PropSeparators = ["\n    "];
    }

    static readonly string[] DefaultOrder = ["title", "location", "notes", "url", "attendees", "datetime", "priority", "uid"];

    static int Run(string[] args)
    {
        var o = new Options();
        var command = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"option {arg} needs a value");
            List<string> ListValue() => Value().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "-V" or "--version": Console.WriteLine(Commands.Version); return 0;
                case "-u": Console.WriteLine("icalBuddy: updates come from brew (brew upgrade ical-buddy)"); return 0;
                case "-sc": o.SepCalendar = true; break;
                case "-sd": o.SepDate = true; break;
                case "-sed": o.ShowEmptyDates = true; break;
                case "-std": o.SepTaskDue = true; break;
                case "-f": o.Format = true; break;
                case "-nc": o.NoCalNames = true; break;
                case "-nrd": o.NoRelDates = true; break;
                case "-npn": o.NoPropNames = true; break;
                case "-n": o.FromNow = true; break;
                case "-ea": o.ExcludeAllDay = true; break;
                case "-eed": o.ExcludeEnd = true; break;
                case "-uid": o.ShowUid = true; break;
                case "-li":
                    if (!int.TryParse(Value(), out o.Limit) || o.Limit < 0) throw new UsageException("-li needs a number");
                    break;
                case "-ic": o.IncludeCals = ListValue(); break;
                case "-ec": o.ExcludeCals = ListValue(); break;
                case "-iep": o.IncludeProps = ListValue(); break;
                case "-eep": o.ExcludeProps = ListValue(); break;
                case "-po": o.PropOrder = ListValue(); break;
                case "-b": o.Bullet = Value(); break;
                case "-ab": o.AlertBullet = Value(); break;
                case "-ss": o.SectionSep = Value(); break;
                case "-ps":
                    var ps = Value();
                    o.PropSeparators = ps.Length >= 2 && ps[0] == ps[^1]
                        ? ps[1..^1].Split(ps[0]).Select(Unescape).ToList()
                        : [Unescape(ps)];
                    break;
                case "-tf": o.TimeFormat = Value(); break;
                case "-df": o.DateFormat = Value(); break;
                case "-nnr": o.NewlineReplacement = Value(); break;
                case "--strEncoding": o.Encoding = Value(); break;
                case "-cf": Value(); break;   // icalBuddy config files (formatting presets) are not read
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg} (see icalBuddy -h)");
                    command.Add(arg);
                    break;
            }
        }
        if (command.Count == 0) { Console.WriteLine(Help); return 2; }

        var output = new StringBuilder();
        var cmd = command[0];
        var now = DateTimeOffset.Now;
        var today = new DateTimeOffset(now.Date, now.Offset);
        if (cmd == "strEncodings")
        {
            foreach (var name in Encodings) output.AppendLine(name);
        }
        else if (cmd == "calendars") Calendars(output);
        else if (cmd == "eventsToday") Events(output, o, today, today.AddDays(1), now);
        else if (cmd.StartsWith("eventsToday+", StringComparison.Ordinal))
        {
            if (!int.TryParse(cmd[12..], out var days) || days < 0) throw new UsageException($"bad day count in '{cmd}'");
            Events(output, o, today, today.AddDays(days + 1), now);
        }
        else if (cmd == "eventsNow") Events(output, o, now, now.AddSeconds(1), now);
        else if (cmd.StartsWith("eventsFrom:", StringComparison.Ordinal))
        {
            var to = command.Skip(1).FirstOrDefault(c => c.StartsWith("to:", StringComparison.Ordinal))
                     ?? throw new UsageException("eventsFrom: needs a 'to:END' argument");
            var start = ParseDate(cmd[11..], today);
            var end = ParseDate(to[3..], today);
            if (end.TimeOfDay == TimeSpan.Zero) end = end.AddDays(1);   // a date alone means the whole day
            if (end <= start) throw new UsageException("the end date must be after the start date");
            Events(output, o, start, end, now);
        }
        else if (cmd == "uncompletedTasks") Tasks(output, o, null, false, today);
        else if (cmd == "undatedUncompletedTasks") Tasks(output, o, null, true, today);
        else if (cmd.StartsWith("tasksDueBefore:", StringComparison.Ordinal)) Tasks(output, o, ParseDate(cmd[15..], today), false, today);
        else throw new UsageException($"unknown command '{cmd}' (see icalBuddy -h)");

        Write(output.ToString(), o.Encoding);
        return 0;
    }

    static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t");

    // ---- output encodings ------------------------------------------------------------------

    static readonly string[] Encodings =
    [
        "ASCII", "Non-lossy ASCII", "Western (ISO Latin 1)", "Unicode (UTF-8)", "Unicode (UTF-16)",
        "Unicode (UTF-16BE)", "Unicode (UTF-16LE)", "Unicode (UTF-32)",
    ];

    static void Write(string text, string encoding)
    {
        // Default: UTF-8 like icalBuddy, or straight to the console as UTF-16 (whatever its code page).
        if (encoding == null && !Console.IsOutputRedirected && WriteConsoleW(GetStdHandle(-11), text, text.Length, out _, IntPtr.Zero)) return;
        byte[] bytes = (encoding ?? "Unicode (UTF-8)") switch
        {
            "ASCII" => System.Text.Encoding.ASCII.GetBytes(text),
            "Non-lossy ASCII" => System.Text.Encoding.ASCII.GetBytes(NonLossyAscii(text)),
            "Western (ISO Latin 1)" => System.Text.Encoding.Latin1.GetBytes(text),
            "Unicode (UTF-8)" => System.Text.Encoding.UTF8.GetBytes(text),
            "Unicode (UTF-16)" or "Unicode (UTF-16LE)" => System.Text.Encoding.Unicode.GetBytes(text),
            "Unicode (UTF-16BE)" => System.Text.Encoding.BigEndianUnicode.GetBytes(text),
            "Unicode (UTF-32)" => System.Text.Encoding.UTF32.GetBytes(text),
            _ => throw new UsageException($"unknown string encoding '{encoding}' (see icalBuddy strEncodings)"),
        };
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(bytes);
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern IntPtr GetStdHandle(int which);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern bool WriteConsoleW(IntPtr console, string text, int length, out int written, IntPtr reserved);

    // NSNonLossyASCIIStringEncoding: backslash escaped as \\, non-ASCII as \uXXXX.
    internal static string NonLossyAscii(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            if (c == '\\') sb.Append("\\\\");
            else if (c < 0x80) sb.Append(c);
            else sb.Append($"\\u{(int)c:x4}");
        }
        return sb.ToString();
    }

    // ---- dates -----------------------------------------------------------------------------

    internal static DateTimeOffset ParseDate(string s, DateTimeOffset today)
    {
        s = s.Trim();
        var lower = s.ToLowerInvariant();
        if (lower == "today") return today;
        if (lower == "tomorrow") return today.AddDays(1);
        if (lower == "yesterday") return today.AddDays(-1);
        if (lower.StartsWith("today") && int.TryParse(lower[5..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n))
            return today.AddDays(n);
        string[] formats = ["yyyy-MM-dd HH:mm:ss zzz", "yyyy-MM-dd HH:mm:ss zz", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm"];
        if (DateTimeOffset.TryParseExact(s.Replace(" +", " +").Replace(" -0", " -0"), formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d))
            return d;
        // icalBuddy's "2026-10-16 10:00:00 +0200" (offset without colon).
        if (s.Length > 6 && (s[^5] == '+' || s[^5] == '-') && DateTimeOffset.TryParseExact(s.Insert(s.Length - 2, ":"), formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d))
            return d;
        throw new UsageException($"cannot parse date '{s}' (use YYYY-MM-DD [HH:MM[:SS]], today, tomorrow or today+N)");
    }

    // strftime subset for -tf/-df.
    internal static string Strftime(DateTimeOffset t, string format)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        for (var i = 0; i < format.Length; i++)
        {
            if (format[i] != '%' || i + 1 >= format.Length) { sb.Append(format[i]); continue; }
            var c = format[++i];
            sb.Append(c switch
            {
                'Y' => t.ToString("yyyy", inv),
                'y' => t.ToString("yy", inv),
                'm' => t.ToString("MM", inv),
                'd' => t.ToString("dd", inv),
                'e' => t.Day.ToString(inv).PadLeft(2),
                'H' => t.ToString("HH", inv),
                'I' => t.ToString("hh", inv),
                'M' => t.ToString("mm", inv),
                'S' => t.ToString("ss", inv),
                'p' => t.Hour < 12 ? "AM" : "PM",
                'A' => t.ToString("dddd", inv),
                'a' => t.ToString("ddd", inv),
                'B' => t.ToString("MMMM", inv),
                'b' or 'h' => t.ToString("MMM", inv),
                'j' => t.DayOfYear.ToString("000", inv),
                'z' => t.ToString("zzz", inv).Replace(":", ""),
                '%' => "%",
                _ => "%" + c,
            });
        }
        return sb.ToString();
    }

    static string FormatDate(DateTimeOffset date, DateTimeOffset today, Options o)
    {
        if (!o.NoRelDates)
        {
            var days = (date.Date - today.Date).Days;
            switch (days)
            {
                case 0: return "today";
                case 1: return "tomorrow";
                case -1: return "yesterday";
                case 2: return "day after tomorrow";
                case -2: return "day before yesterday";
            }
        }
        return Strftime(date, o.DateFormat);
    }

    // ---- stores ----------------------------------------------------------------------------

    static T Await<T>(Windows.Foundation.IAsyncOperation<T> op) => op.AsTask().GetAwaiter().GetResult();

    static AppointmentStore Appointments()
    {
        try { return Await(AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly)); }
        catch (Exception e) when (e is not UsageException)
        {
            throw new InvalidOperationException($"no calendar store is accessible ({e.Message.Trim()}); allow calendar access in Settings > Privacy & security > Calendar");
        }
    }

    static UserDataTaskStore TaskStore()
    {
        try { return Await(UserDataTaskManager.GetDefault().RequestStoreAsync(UserDataTaskStoreAccessType.AllTasksLimitedReadWrite)); }
        catch (Exception e) when (e is not UsageException)
        {
            throw new InvalidOperationException($"no task store is accessible ({e.Message.Trim()}); allow task access in Settings > Privacy & security > Tasks");
        }
    }

    static bool CalendarSelected(string name, string id, Options o) =>
        (o.IncludeCals == null || o.IncludeCals.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase) || c == id))
        && (o.ExcludeCals == null || !o.ExcludeCals.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase) || c == id));

    static void Calendars(StringBuilder output)
    {
        foreach (var cal in Await(Appointments().FindAppointmentCalendarsAsync()))
        {
            output.AppendLine($"• {cal.DisplayName}");
            output.AppendLine($"  type: {(string.IsNullOrEmpty(cal.SourceDisplayName) ? "Local" : cal.SourceDisplayName)}");
            output.AppendLine($"  UID: {cal.LocalId}");
        }
        try
        {
            foreach (var list in Await(TaskStore().FindListsAsync()))
            {
                output.AppendLine($"• {list.DisplayName}");
                output.AppendLine($"  type: {(string.IsNullOrEmpty(list.SourceDisplayName) ? "Tasks" : list.SourceDisplayName + " (tasks)")}");
                output.AppendLine($"  UID: {list.Id}");
            }
        }
        catch (InvalidOperationException) { }   // no task store: calendars alone
    }

    // ---- events ----------------------------------------------------------------------------

    sealed record Item(string Title, string Calendar, string Location, string Notes, string Url, string Attendees,
        DateTimeOffset? Start, DateTimeOffset? End, bool AllDay, string Uid, string Priority, bool IsTask);

    static void Events(StringBuilder output, Options o, DateTimeOffset start, DateTimeOffset end, DateTimeOffset now)
    {
        var store = Appointments();
        var calendars = Await(store.FindAppointmentCalendarsAsync()).ToDictionary(c => c.LocalId, c => c.DisplayName);
        var options = new FindAppointmentsOptions { MaxCount = 1000 };
        foreach (var p in new[]
                 {
                     AppointmentProperties.Subject, AppointmentProperties.Location, AppointmentProperties.StartTime,
                     AppointmentProperties.Duration, AppointmentProperties.AllDay, AppointmentProperties.Details,
                     AppointmentProperties.Uri, AppointmentProperties.Invitees,
                 })
            options.FetchProperties.Add(p);
        var items = new List<Item>();
        foreach (var a in Await(store.FindAppointmentsAsync(start, end - start, options)))
        {
            var calName = calendars.GetValueOrDefault(a.CalendarId) ?? "";
            if (!CalendarSelected(calName, a.CalendarId, o)) continue;
            if (o.ExcludeAllDay && a.AllDay) continue;
            var itemEnd = a.StartTime + a.Duration;
            if (o.FromNow && itemEnd <= now) continue;
            var attendees = string.Join(", ", a.Invitees.Select(i => string.IsNullOrEmpty(i.DisplayName) ? i.Address : i.DisplayName));
            items.Add(new Item(a.Subject, calName, a.Location, a.Details, a.Uri?.ToString(), attendees, a.StartTime, itemEnd,
                a.AllDay, string.IsNullOrEmpty(a.RoamingId) ? a.LocalId : a.RoamingId,
                null, false));
        }
        items = items.OrderBy(i => i.Start).ThenBy(i => i.Title, StringComparer.CurrentCulture).Take(o.Limit).ToList();
        var today = new DateTimeOffset(now.Date, now.Offset);

        var first = true;
        if (o.SepDate)
        {
            for (var day = new DateTimeOffset(start.Date, start.Offset); day < end; day = day.AddDays(1))
            {
                var dayEnd = day.AddDays(1);
                var onDay = items.Where(i => i.Start < dayEnd && (i.End > day || (i.End == i.Start && i.Start >= day))).ToList();
                if (onDay.Count == 0 && !o.ShowEmptyDates) continue;
                Section(output, FormatDate(day, today, o) + ":", o, ref first);
                foreach (var item in onDay) Render(output, item, o, true, today, day);
            }
        }
        else if (o.SepCalendar)
        {
            foreach (var group in items.GroupBy(i => i.Calendar))
            {
                Section(output, group.Key + ":", o, ref first);
                foreach (var item in group) Render(output, item, o, false, today);
            }
        }
        else foreach (var item in items) Render(output, item, o, true, today);
    }

    static void Section(StringBuilder output, string title, Options o, ref bool first)
    {
        if (!first) output.AppendLine();
        first = false;
        output.AppendLine(o.Format ? $"\e[1m{title}\e[0m" : title);
        if (o.SectionSep.Length > 0) output.AppendLine(o.SectionSep);
    }

    // One item: bullet + title line, then each further property after the property separators.
    static void Render(StringBuilder output, Item item, Options o, bool withCalendar, DateTimeOffset today, DateTimeOffset? sectionDay = null)
    {
        var order = (o.PropOrder ?? []).Concat(DefaultOrder).Distinct().ToList();
        var props = new List<string>();
        foreach (var name in order)
        {
            if (name == "uid" && !o.ShowUid && o.IncludeProps?.Contains("uid") != true) continue;
            if (o.IncludeProps != null && name != "title" && !o.IncludeProps.Contains(name)) continue;
            if (o.ExcludeProps != null && name != "title" && o.ExcludeProps.Contains(name)) continue;
            var value = name switch
            {
                "title" => Title(item, o, withCalendar),
                "location" => item.Location,
                "notes" => item.Notes,
                "url" => item.Url,
                "attendees" => item.Attendees,
                "datetime" => DateTimeText(item, o, today, sectionDay),
                "priority" => item.Priority,
                "uid" => item.Uid,
                _ => null,
            };
            if (string.IsNullOrWhiteSpace(value)) continue;
            value = value.Trim().Replace("\r\n", "\n");
            if (o.NewlineReplacement != null) value = value.Replace("\n", o.NewlineReplacement);
            var label = name is "title" or "datetime" || o.NoPropNames ? "" : $"{name}: ";
            if (o.Format && label.Length > 0) label = $"\e[36m{label}\e[0m";
            props.Add(label + value);
        }
        output.Append(item.Priority == "high" ? o.AlertBullet : o.Bullet).Append(props[0]);
        for (var i = 1; i < props.Count; i++)
            output.Append(o.PropSeparators[Math.Min(i - 1, o.PropSeparators.Count - 1)]).Append(props[i]);
        output.AppendLine();
    }

    static string Title(Item item, Options o, bool withCalendar)
    {
        var title = string.IsNullOrEmpty(item.Title) ? "(no title)" : item.Title;
        if (o.Format) title = $"\e[1m{title}\e[0m";
        return withCalendar && !o.NoCalNames && !string.IsNullOrEmpty(item.Calendar) ? $"{title} ({item.Calendar})" : title;
    }

    static string DateTimeText(Item item, Options o, DateTimeOffset today, DateTimeOffset? sectionDay)
    {
        if (item.IsTask) return item.Start is { } due ? (o.NoPropNames ? "" : "due: ") + FormatDate(due, today, o) : null;
        var start = item.Start!.Value;
        var end = item.End!.Value;
        string Time(DateTimeOffset t) => Strftime(t, o.TimeFormat);
        if (item.AllDay)
        {
            if (sectionDay != null) return null;
            var lastDay = end.AddDays(-1);
            return lastDay.Date > start.Date && !o.ExcludeEnd
                ? $"{FormatDate(start, today, o)} - {FormatDate(lastDay, today, o)}"
                : FormatDate(start, today, o);
        }
        if (sectionDay != null) return o.ExcludeEnd ? Time(start) : $"{Time(start)} - {Time(end)}";
        var startText = $"{FormatDate(start, today, o)} at {Time(start)}";
        if (o.ExcludeEnd) return startText;
        return end.Date == start.Date
            ? $"{startText} - {Time(end)}"
            : $"{startText} - {FormatDate(end, today, o)} at {Time(end)}";
    }

    // ---- tasks -----------------------------------------------------------------------------

    static void Tasks(StringBuilder output, Options o, DateTimeOffset? dueBefore, bool undatedOnly, DateTimeOffset today)
    {
        var store = TaskStore();
        var items = new List<Item>();
        foreach (var list in Await(store.FindListsAsync()))
        {
            if (!CalendarSelected(list.DisplayName, list.Id, o)) continue;
            var reader = list.GetTaskReader();
            while (true)
            {
                var batch = Await(reader.ReadBatchAsync());
                if (batch.Tasks.Count == 0) break;
                foreach (var t in batch.Tasks)
                {
                    if (t.CompletedDate != null) continue;
                    var due = t.DueDate;
                    if (undatedOnly && due != null) continue;
                    if (dueBefore != null && (due == null || due.Value >= dueBefore.Value)) continue;
                    var priority = t.Priority switch { UserDataTaskPriority.High => "high", UserDataTaskPriority.Low => "low", _ => null };
                    items.Add(new Item(t.Subject, list.DisplayName, null, t.Details, null, null, due, due, false, t.Id, priority, true));
                }
            }
        }
        // Dated tasks first by due date, then undated; high priority first within a date.
        items = items.OrderBy(i => i.Start == null).ThenBy(i => i.Start)
            .ThenBy(i => i.Priority == "high" ? 0 : i.Priority == null ? 1 : 2).Take(o.Limit).ToList();

        var first = true;
        if (o.SepCalendar || o.SepTaskDue)
        {
            var groups = o.SepCalendar
                ? items.GroupBy(i => i.Calendar)
                : items.GroupBy(i => i.Start is { } d ? FormatDate(d, today, o) : "no due date");
            foreach (var g in groups)
            {
                Section(output, g.Key + ":", o, ref first);
                foreach (var item in g) Render(output, item, o, !o.SepCalendar, today);
            }
        }
        else foreach (var item in items) Render(output, item, o, true, today);
    }
}

