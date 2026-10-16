using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WinTools;

// journalctl on the Windows Event Log (System and Application channels, read through wevtapi).
// Event levels map to syslog priorities; "-u NAME" matches events from a provider with the
// service's name and the Service Control Manager's events about that service.
static class Journal
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("journalctl", "Query the Windows Event Log the journalctl way", Run);
    }

    const string Help = """
        Usage: journalctl [OPTIONS...]
        Query the Windows Event Log (System and Application channels).

          -u --unit=UNIT          Show events for a service (repeatable)
          -t --identifier=NAME    Show events from this provider (repeatable)
          -k --dmesg              Show kernel events only
          -b --boot[=ID]          Show events from the current boot (0) or an earlier one (-1, -2 ...)
             --list-boots         List boots recorded in the System log
          -p --priority=RANGE     Show events of this priority or more important (emerg..debug or 0..7)
          -S --since=DATE         Show events not older than DATE ("2026-10-16 08:00", "today", "1 hour ago")
          -U --until=DATE         Show events not newer than DATE
          -g --grep=PATTERN       Show events whose message matches the regular expression
          -n --lines[=N]          Show the newest N events (default 10)
          -e --pager-end          Same as -n 1000
          -r --reverse            Newest first
          -f --follow             Keep printing new events
          -o --output=FORMAT      short, short-iso, short-precise, cat, json
             --no-hostname        Don't show the host name
          -q --quiet              Don't print "-- No entries --"
             --no-pager, -x, -a, --system, --user  Accepted for compatibility
        """;

    internal sealed class Entry
    {
        public DateTime Time;
        public string Provider, Host, Channel, Message;
        public uint Pid;
        public int Level;
        public ushort EventId;
        public ulong RecordId;
        public int Priority => Level switch { 1 => 2, 2 => 3, 3 => 4, 5 => 7, _ => 6 };
    }

    sealed class Filter
    {
        public List<string> Units = [], Identifiers = [];
        public bool Kernel;
        public DateTime? Since, Until;
        public int MinPriority = 0, MaxPriority = 7;
        public Regex Grep;
    }

    static int Run(string[] args)
    {
        var f = new Filter();
        int? lines = null;
        bool reverse = false, follow = false, quiet = false, noHost = false, listBoots = false;
        string output = "short";
        int? boot = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string value = null;
            if (a.StartsWith("--") && a.Contains('=')) { value = a[(a.IndexOf('=') + 1)..]; a = a[..a.IndexOf('=')]; }
            else if (a.Length > 2 && a[0] == '-' && a[1] != '-' && "utnpSUgob".Contains(a[1])) { value = a[2..]; a = a[..2]; }
            string Next() => value ?? (i + 1 < args.Length ? args[++i] : throw new UsageException($"option '{a}' requires an argument"));
            switch (a)
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "--version": Console.WriteLine($"systemd 256 (wintools {Commands.Version})\n+WINDOWS-EVENTLOG"); return 0;
                case "-u" or "--unit": f.Units.Add(Systemd.ServiceName(Next())); break;
                case "-t" or "--identifier": f.Identifiers.Add(Next()); break;
                case "-k" or "--dmesg": f.Kernel = true; boot ??= 0; break;
                case "-S" or "--since": f.Since = ParseTime(Next()); break;
                case "-U" or "--until": f.Until = ParseTime(Next()); break;
                case "-g" or "--grep":
                    var pattern = Next();
                    // journalctl's --grep is case-insensitive when the pattern is all lowercase.
                    f.Grep = new Regex(pattern, (pattern.Any(char.IsUpper) ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.CultureInvariant);
                    break;
                case "-p" or "--priority": ParsePriority(Next(), f); break;
                case "-o" or "--output":
                    output = Next();
                    if (output is not ("short" or "short-iso" or "short-precise" or "short-full" or "cat" or "json" or "json-pretty"))
                        throw new UsageException($"unknown output format '{output}'");
                    break;
                case "-n" or "--lines":
                    // -n takes an optional argument: "-n" alone means 10.
                    if (value == null && i + 1 < args.Length && int.TryParse(args[i + 1], out var n)) { lines = n; i++; }
                    else lines = value == null ? 10 : int.TryParse(value, out var v) && v >= 0 ? v : throw new UsageException($"invalid number '{value}'");
                    break;
                case "-b" or "--boot":
                    if (value == null && i + 1 < args.Length && int.TryParse(args[i + 1], out var b)) { boot = b; i++; }
                    else boot = value == null ? 0 : int.TryParse(value, out var bv) ? bv : throw new UsageException($"invalid boot '{value}' (use 0, -1, -2 ...)");
                    break;
                case "--list-boots": listBoots = true; break;
                case "-e" or "--pager-end": lines ??= 1000; break;
                case "-r" or "--reverse": reverse = true; break;
                case "-f" or "--follow": follow = true; break;
                case "-q" or "--quiet": quiet = true; break;
                case "--no-hostname": noHost = true; break;
                case "--no-pager" or "-x" or "--catalog" or "-a" or "--all" or "--system" or "--user" or "-l" or "--full" or "-m" or "--merge": break;
                default: throw new UsageException($"unrecognized option '{args[i]}'");
            }
        }

        if (listBoots) return ListBoots();
        // A unit also matches the SCM's events about it, which name the service by display name.
        foreach (var unit in f.Units.ToList())
        {
            using var service = Systemd.Open(unit, Systemd.SERVICE_QUERY_CONFIG);
            if (service != null && Systemd.QueryConfig(service).DisplayName is string display) f.Units.Add("\0" + display);
        }
        if (boot is int bootIndex)
        {
            var boots = Boots();
            if (bootIndex > 0 || -bootIndex >= Math.Max(1, boots.Count)) throw new UsageException($"no boot {bootIndex} in the System log (see --list-boots)");
            var start = boots.Count > 0 ? boots[-bootIndex] : DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);
            if (f.Since == null || f.Since < start) f.Since = start;
            if (bootIndex < 0 && (f.Until == null || f.Until > boots[-bootIndex - 1])) f.Until = boots[-bootIndex - 1];
        }
        if (follow) lines ??= 10;

        var entries = Read(f, lines);
        if (reverse) entries.Reverse();
        foreach (var e in entries) Print(e, output, noHost);
        if (entries.Count == 0 && !quiet && !follow && output.StartsWith("short")) Console.WriteLine("-- No entries --");
        if (!follow) return 0;

        // Follow: poll each channel for records newer than the last one seen.
        var last = Channels(f).ToDictionary(c => c, c => entries.Where(e => e.Channel == c).Select(e => e.RecordId).DefaultIfEmpty(LatestRecord(c)).Max());
        while (true)
        {
            Thread.Sleep(1000);
            var fresh = new List<Entry>();
            foreach (var channel in last.Keys.ToList())
            {
                var got = Query(channel, XPath(f, last[channel]), reverse: false, max: int.MaxValue, f);
                if (got.Count > 0) last[channel] = got.Max(e => e.RecordId);
                fresh.AddRange(got);
            }
            foreach (var e in fresh.OrderBy(e => e.Time)) Print(e, output, noHost);
        }
    }

    // The newest `lines` matching entries (or all of them), oldest first.
    static List<Entry> Read(Filter f, int? lines)
    {
        var all = new List<Entry>();
        foreach (var channel in Channels(f))
            all.AddRange(Query(channel, XPath(f, null), reverse: lines != null, max: lines ?? int.MaxValue, f));
        all.Sort((a, b) => a.Time != b.Time ? a.Time.CompareTo(b.Time) : a.RecordId.CompareTo(b.RecordId));
        return lines is int n && all.Count > n ? all.GetRange(all.Count - n, n) : all;
    }

    static IEnumerable<string> Channels(Filter f) => f.Kernel ? ["System"] : ["System", "Application"];

    // Recent log lines for `systemctl status`.
    internal static List<string> ForUnit(string service, string displayName, int lines)
    {
        var f = new Filter { Units = [service] };
        if (displayName != null) f.Units.Add("\0" + displayName);
        try { return Read(f, lines).Select(e => Format(e, "short", false)).ToList(); }
        catch (System.ComponentModel.Win32Exception) { return []; }
    }

    static string XPath(Filter f, ulong? afterRecord)
    {
        var conditions = new List<string>();
        if (afterRecord is ulong r) conditions.Add($"System[EventRecordID>{r}]");
        if (f.Since is DateTime s) conditions.Add($"System[TimeCreated[@SystemTime>='{s.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}']]");
        if (f.Until is DateTime u) conditions.Add($"System[TimeCreated[@SystemTime<='{u.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}']]");
        if (f.MinPriority > 0 || f.MaxPriority < 7)
        {
            var levels = new[] { 0, 1, 2, 3, 4, 5 }.Where(l =>
                new Entry { Level = l }.Priority is var p && p >= f.MinPriority && p <= f.MaxPriority).Select(l => $"Level={l}").ToList();
            conditions.Add(levels.Count == 0 ? "System[Level=99]" : $"System[({string.Join(" or ", levels)})]");
        }
        // Units: events from a provider named after the service (or its display name), plus the
        // Service Control Manager's events about it (state changes name the service by display name
        // in param1, start-type changes by key name in param4).
        var keys = f.Units.Where(n => !n.StartsWith('\0')).ToList();
        var displays = f.Units.Where(n => n.StartsWith('\0')).Select(n => n[1..]).ToList();
        var names = keys.Concat(displays).Concat(f.Identifiers).ToList();
        if (names.Count > 0)
        {
            var any = $"System[Provider[{string.Join(" or ", names.Select(n => $"@Name={Literal(n)}"))}]]";
            var scm = displays.Select(d => $"Data[@Name='param1']={Literal(d)}").Concat(keys.Select(k => $"Data[@Name='param4']={Literal(k)}")).ToList();
            if (scm.Count > 0)
                any = $"({any} or (System[Provider[@Name='Service Control Manager']] and EventData[{string.Join(" or ", scm)}]))";
            conditions.Add(any);
        }
        return conditions.Count == 0 ? "*" : $"*[{string.Join(" and ", conditions)}]";
    }

    static string Literal(string s) => s.Contains('\'') ? $"\"{s}\"" : $"'{s}'";

    static List<Entry> Query(string channel, string xpath, bool reverse, int max, Filter f)
    {
        var result = new List<Entry>();
        var query = EvtQuery(IntPtr.Zero, channel, xpath, EvtQueryChannelPath | (reverse ? EvtQueryReverseDirection : 0));
        if (query == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            if (error is ERROR_EVT_CHANNEL_NOT_FOUND or ERROR_ACCESS_DENIED) return result;
            throw new System.ComponentModel.Win32Exception(error);
        }
        var context = EvtCreateRenderContext(0, IntPtr.Zero, EvtRenderContextSystem);
        var handles = new IntPtr[64];
        try
        {
            while (result.Count < max)
            {
                if (!EvtNext(query, (uint)handles.Length, handles, 2000, 0, out var count))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error is ERROR_NO_MORE_ITEMS or ERROR_TIMEOUT) break;
                    throw new System.ComponentModel.Win32Exception(error);
                }
                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        if (result.Count >= max) continue;
                        var e = Render(context, handles[i], channel);
                        if (f.Kernel && !e.Provider.StartsWith("Microsoft-Windows-Kernel", StringComparison.OrdinalIgnoreCase)) continue;
                        if (f.Grep != null && !f.Grep.IsMatch(e.Message)) continue;
                        result.Add(e);
                    }
                    finally { EvtClose(handles[i]); }
                }
            }
        }
        finally
        {
            EvtClose(context);
            EvtClose(query);
        }
        return result;
    }

    static ulong LatestRecord(string channel)
    {
        var got = Query(channel, "*", reverse: true, max: 1, new Filter());
        return got.Count > 0 ? got[0].RecordId : 0;
    }

    static Entry Render(IntPtr context, IntPtr evt, string channel)
    {
        EvtRender(context, evt, EvtRenderEventValues, 0, IntPtr.Zero, out var used, out _);
        var buf = Marshal.AllocHGlobal((int)used);
        try
        {
            if (!EvtRender(context, evt, EvtRenderEventValues, used, buf, out _, out _)) throw new System.ComponentModel.Win32Exception();
            var e = new Entry
            {
                Channel = channel,
                Provider = VarString(buf, 0) ?? "",
                EventId = (ushort)VarInt(buf, 2),
                Level = (int)VarInt(buf, 4),
                RecordId = VarInt(buf, 9),
                Pid = (uint)VarInt(buf, 12),
                Host = (VarString(buf, 15) ?? Environment.MachineName).Split('.')[0],
            };
            var ft = VarInt(buf, 8);
            e.Time = ft == 0 ? DateTime.MinValue : DateTime.FromFileTimeUtc((long)ft).ToLocalTime();
            e.Message = Message(evt, e.Provider);
            return e;
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    // EVT_VARIANT: an 8-byte value, a count and a type (16 bytes).
    static string VarString(IntPtr buf, int index)
    {
        var type = Marshal.ReadInt32(buf, index * 16 + 12);
        return type == 1 ? Marshal.PtrToStringUni(Marshal.ReadIntPtr(buf, index * 16)) : null;
    }

    static ulong VarInt(IntPtr buf, int index) => Marshal.ReadInt32(buf, index * 16 + 12) switch
    {
        3 or 4 => Marshal.ReadByte(buf, index * 16),
        5 or 6 => (ushort)Marshal.ReadInt16(buf, index * 16),
        7 or 8 or 20 => (uint)Marshal.ReadInt32(buf, index * 16),
        9 or 10 or 17 or 21 => (ulong)Marshal.ReadInt64(buf, index * 16),
        _ => 0,
    };

    static readonly Dictionary<string, IntPtr> publishers = new(StringComparer.OrdinalIgnoreCase);

    static string Message(IntPtr evt, string provider)
    {
        if (!publishers.TryGetValue(provider, out var meta))
            publishers[provider] = meta = EvtOpenPublisherMetadata(IntPtr.Zero, provider, null, 0, 0);
        if (meta != IntPtr.Zero)
        {
            EvtFormatMessage(meta, evt, 0, 0, IntPtr.Zero, EvtFormatMessageEvent, 0, null, out var needed);
            if (needed > 0)
            {
                var chars = new char[needed];
                if (EvtFormatMessage(meta, evt, 0, 0, IntPtr.Zero, EvtFormatMessageEvent, needed, chars, out _))
                    return Clean(new string(chars, 0, (int)Math.Max(0, needed - 1)));
            }
        }
        // No message resource: fall back to the event's data values, like Event Viewer does.
        EvtRender(IntPtr.Zero, evt, EvtRenderEventXml, 0, IntPtr.Zero, out var size, out _);
        var xml = new char[size / 2 + 1];
        var handle = GCHandle.Alloc(xml, GCHandleType.Pinned);
        try
        {
            if (!EvtRender(IntPtr.Zero, evt, EvtRenderEventXml, size, handle.AddrOfPinnedObject(), out _, out _)) return "";
        }
        finally { handle.Free(); }
        var data = Regex.Matches(new string(xml).TrimEnd('\0'), @"<Data(?: Name='([^']*)')?>([^<]*)</Data>")
            .Select(m => m.Groups[1].Success ? $"{m.Groups[1].Value}={System.Net.WebUtility.HtmlDecode(m.Groups[2].Value)}" : System.Net.WebUtility.HtmlDecode(m.Groups[2].Value));
        return string.Join(" ", data);
    }

    static string Clean(string message) =>
        string.Join("\n", message.Replace("\0", "").ReplaceLineEndings("\n").Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0)).Trim();

    static void Print(Entry e, string output, bool noHost)
    {
        if (output is "json" or "json-pretty")
        {
            using var stream = Console.OpenStandardOutput();
            using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = output == "json-pretty" }))
            {
                w.WriteStartObject();
                w.WriteString("__REALTIME_TIMESTAMP", ((e.Time.ToUniversalTime() - DateTime.UnixEpoch).Ticks / 10).ToString());
                w.WriteString("_HOSTNAME", e.Host);
                w.WriteString("SYSLOG_IDENTIFIER", e.Provider);
                w.WriteString("_PID", e.Pid.ToString());
                w.WriteString("PRIORITY", e.Priority.ToString());
                w.WriteString("MESSAGE", e.Message);
                w.WriteString("_TRANSPORT", "eventlog");
                w.WriteString("WINDOWS_CHANNEL", e.Channel);
                w.WriteString("WINDOWS_EVENT_ID", e.EventId.ToString());
                w.WriteString("WINDOWS_RECORD_ID", e.RecordId.ToString());
                w.WriteEndObject();
            }
            stream.Write("\n"u8);
            return;
        }
        Console.WriteLine(Format(e, output, noHost));
    }

    static string Format(Entry e, string output, bool noHost)
    {
        if (output == "cat") return e.Message;
        var time = output switch
        {
            "short-iso" => e.Time.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            "short-precise" => e.Time.ToString("MMM dd HH:mm:ss.ffffff"),
            "short-full" => e.Time.ToString("ddd yyyy-MM-dd HH:mm:ss"),
            _ => e.Time.ToString("MMM dd HH:mm:ss"),
        };
        var prefix = $"{time}{(noHost ? "" : " " + e.Host)} {e.Provider}[{e.Pid}]: ";
        // Continuation lines line up under the first line's text, as journalctl does.
        return prefix + e.Message.Replace("\n", "\n" + new string(' ', prefix.Length));
    }

    // Boot start times from the kernel's "operating system started" events, newest first.
    static List<DateTime> Boots()
    {
        var xpath = "*[System[Provider[@Name='Microsoft-Windows-Kernel-General'] and EventID=12]]";
        return Query("System", xpath, reverse: true, max: 1000, new Filter()).Select(e => e.Time).ToList();
    }

    static int ListBoots()
    {
        var boots = Boots();
        if (boots.Count == 0) { Console.WriteLine("-- No boots recorded in the System log --"); return 0; }
        Console.WriteLine("IDX FIRST ENTRY                  LAST ENTRY");
        for (var i = boots.Count - 1; i >= 0; i--)
        {
            var end = i == 0 ? DateTime.Now : boots[i - 1];
            Console.WriteLine($"{-i,3} {boots[i]:ddd yyyy-MM-dd HH:mm:ss}      {end:ddd yyyy-MM-dd HH:mm:ss}");
        }
        return 0;
    }

    static readonly string[] priorityNames = ["emerg", "alert", "crit", "err", "warning", "notice", "info", "debug"];

    static void ParsePriority(string spec, Filter f)
    {
        static int One(string s) =>
            int.TryParse(s, out var n) && n is >= 0 and <= 7 ? n :
            Array.IndexOf(priorityNames, s.ToLowerInvariant()) is var i && i >= 0 ? i :
            throw new UsageException($"unknown priority '{s}'");
        var range = spec.Split("..");
        if (range.Length == 2) { f.MinPriority = One(range[0]); f.MaxPriority = One(range[1]); }
        else f.MaxPriority = One(spec);
    }

    // systemd.time(7) subset: absolute dates/times, today/yesterday/tomorrow/now, "N units ago", "-N units", "+N units".
    internal static DateTime ParseTime(string s)
    {
        s = s.Trim();
        var now = DateTime.Now;
        switch (s.ToLowerInvariant())
        {
            case "now": return now;
            case "today": return now.Date;
            case "yesterday": return now.Date.AddDays(-1);
            case "tomorrow": return now.Date.AddDays(1);
        }
        var m = Regex.Match(s, @"^([+-]?)(\d+)\s*(s|sec|secs|seconds?|m|min|mins|minutes?|h|hr|hours?|d|days?|w|weeks?|months?|y|years?)(\s+ago)?$", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var n = int.Parse(m.Groups[2].Value);
            var sign = m.Groups[1].Value == "+" && !m.Groups[4].Success ? 1 : -1;
            if (m.Groups[1].Value == "" && !m.Groups[4].Success) throw new UsageException($"failed to parse timestamp '{s}' (say \"{s} ago\")");
            var unit = m.Groups[3].Value.ToLowerInvariant();
            return unit[0] switch
            {
                's' => now.AddSeconds(sign * n),
                'h' => now.AddHours(sign * n),
                'd' => now.AddDays(sign * n),
                'w' => now.AddDays(sign * 7 * n),
                'y' => now.AddYears(sign * n),
                _ => unit.StartsWith("mo") ? now.AddMonths(sign * n) : now.AddMinutes(sign * n),
            };
        }
        string[] formats = ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "HH:mm:ss", "HH:mm"];
        if (DateTime.TryParseExact(s, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out var t))
            return t;
        throw new UsageException($"failed to parse timestamp '{s}'");
    }

    const uint EvtQueryChannelPath = 1, EvtQueryReverseDirection = 0x200;
    const uint EvtRenderContextSystem = 1, EvtRenderEventValues = 0, EvtRenderEventXml = 1, EvtFormatMessageEvent = 1;
    const int ERROR_NO_MORE_ITEMS = 259, ERROR_TIMEOUT = 1460, ERROR_ACCESS_DENIED = 5, ERROR_EVT_CHANNEL_NOT_FOUND = 15007;

    [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr EvtQuery(IntPtr session, string path, string query, uint flags);

    [DllImport("wevtapi.dll", SetLastError = true)]
    static extern bool EvtNext(IntPtr resultSet, uint count, [Out] IntPtr[] events, uint timeout, uint flags, out uint returned);

    [DllImport("wevtapi.dll", SetLastError = true)]
    static extern IntPtr EvtCreateRenderContext(uint count, IntPtr paths, uint flags);

    [DllImport("wevtapi.dll", SetLastError = true)]
    static extern bool EvtRender(IntPtr context, IntPtr fragment, uint flags, uint bufferSize, IntPtr buffer, out uint used, out uint propertyCount);

    [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr EvtOpenPublisherMetadata(IntPtr session, string publisher, string logFile, uint locale, uint flags);

    [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool EvtFormatMessage(IntPtr publisherMetadata, IntPtr evt, uint messageId, uint valueCount, IntPtr values,
        uint flags, uint bufferSize, [Out] char[] buffer, out uint used);

    [DllImport("wevtapi.dll")]
    static extern bool EvtClose(IntPtr handle);
}
