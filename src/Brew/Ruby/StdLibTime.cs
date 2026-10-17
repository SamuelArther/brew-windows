using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

// Time (always loaded), Date and DateTime (require "date"), Time.parse/iso8601/strptime (require "time").
public enum TimeKind { Local, Utc, Fixed }

public sealed class RTime
{
    public DateTimeOffset T;
    public TimeKind Kind;
}

public sealed class RDate
{
    public DateTimeOffset T;   // a Date is midnight at offset 0; a DateTime carries its time and offset
    public bool Dt;
}

public sealed partial class Interp
{
    public RClass TimeClass, DateClass, DateTimeClass;

    static readonly string[] MonthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    // ---- time zone ----

    TimeZoneInfo LocalZone()
    {
        var tz = Env.TryGetValue("TZ", out var v) ? v : Environment.GetEnvironmentVariable("TZ");
        if (string.IsNullOrEmpty(tz)) return TimeZoneInfo.Local;
        var t = tz.TrimStart(':');
        if (t is "UTC" or "GMT" or "UTC0" or "GMT0" or "Etc/UTC" or "Etc/GMT" or "Z" or "UCT" or "Universal" or "Zulu") return TimeZoneInfo.Utc;
        try { return TimeZoneInfo.FindSystemTimeZoneById(t); }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException) { }
        // POSIX "EST5EDT" / "JST-9": a fixed standard offset is the best we can do.
        var m = Regex.Match(t, @"^([A-Za-z]{3,})([+-]?\d{1,2})(?::(\d\d))?");
        if (m.Success)
        {
            // POSIX offsets count west: "EST5" is UTC-5, "JST-9" is UTC+9.
            int hrs = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            int mins = m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
            var off = -(hrs * 60 + (m.Groups[2].Value.StartsWith('-') ? -mins : mins));
            return TimeZoneInfo.CreateCustomTimeZone(m.Groups[1].Value, TimeSpan.FromMinutes(off), m.Groups[1].Value, m.Groups[1].Value);
        }
        return TimeZoneInfo.Local;
    }

    TimeSpan LocalOffset(DateTime utc) => LocalZone().GetUtcOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));

    // The local offset for a wall-clock time (Time.new / Time.local).
    TimeSpan LocalOffsetForWall(DateTime wall)
    {
        var z = LocalZone();
        var w = DateTime.SpecifyKind(wall, DateTimeKind.Unspecified);
        if (z.IsInvalidTime(w)) w = w.AddHours(1);
        return z.GetUtcOffset(w);
    }

    string ZoneName(RTime t)
    {
        if (t.Kind == TimeKind.Utc) return "UTC";
        if (t.Kind == TimeKind.Fixed) return null;
        var z = LocalZone();
        if (z == TimeZoneInfo.Utc || z.Id == "UTC") return "UTC";
        var name = z.IsDaylightSavingTime(t.T.UtcDateTime) ? z.DaylightName : z.StandardName;
        // Windows names zones in words ("Pacific Daylight Time"); Ruby on Unix says "PDT".
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 1 && words.All(w => char.IsLetter(w[0])) ? string.Concat(words.Select(w => char.ToUpperInvariant(w[0]))) : name;
    }

    public RObject NewTime(DateTimeOffset t, TimeKind kind) => new() { Class = TimeClass, Host = new RTime { T = t, Kind = kind } };

    public RObject LocalTime(DateTime utc) => NewTime(new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToOffset(LocalOffset(utc)), TimeKind.Local);

    RTime TimeOf(object o) => o switch
    {
        RObject { Host: RTime t } => t,
        RObject { Host: DateTime d } => new RTime { T = new DateTimeOffset(DateTime.SpecifyKind(d, DateTimeKind.Utc)), Kind = TimeKind.Utc },
        _ => throw TypeErr($"can't convert {TypeName(o)} into an exact number"),
    };

    static string FormatOffset(TimeSpan off, string sep)
    {
        var sign = off < TimeSpan.Zero ? "-" : "+";
        off = off.Duration();
        return sign + off.Hours.ToString("00", CultureInfo.InvariantCulture) + sep + off.Minutes.ToString("00", CultureInfo.InvariantCulture);
    }

    // "+09:00", "-0500", "UTC", "Z", "A".."Z" military → offset; null if not an offset.
    static TimeSpan? ParseOffset(string s)
    {
        if (s == null) return null;
        s = s.Trim();
        if (s.Length == 0) return null;
        var up = s.ToUpperInvariant();
        if (up is "UTC" or "GMT" or "Z" or "UT") return TimeSpan.Zero;
        var named = up switch
        {
            "EST" => -5, "EDT" => -4, "CST" => -6, "CDT" => -5, "MST" => -7, "MDT" => -6, "PST" => -8, "PDT" => -7,
            "JST" => 9, "CET" => 1, "CEST" => 2, "BST" => 1, "IST" => 5.5, "AEST" => 10, "AEDT" => 11, "HST" => -10, "AKST" => -9, "AKDT" => -8,
            _ => (double?)null,
        };
        if (named != null) return TimeSpan.FromHours(named.Value);
        var m = Regex.Match(s, @"^(?:GMT|UTC)?([+-])(\d{1,2})(?::?(\d{2}))?(?::?(\d{2}))?$");
        if (!m.Success) return null;
        var span = new TimeSpan(int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0,
            m.Groups[4].Success ? int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture) : 0);
        return m.Groups[1].Value == "-" ? -span : span;
    }

    TimeSpan OffsetArg(object o) => o switch
    {
        long l => TimeSpan.FromSeconds(l),
        double d => TimeSpan.FromSeconds(d),
        RString s => ParseOffset(s.V) ?? throw ArgError("\"+HH:MM\", \"-HH:MM\", \"UTC\" or \"A\"..\"I\",\"K\"..\"Z\" expected for utc_offset: " + s.V),
        _ => throw ArgError("invalid offset"),
    };

    // ---- strftime (Time, Date, DateTime) ----

    public static string Strftime(DateTimeOffset t, string zone, string fmt, bool isDate = false)
    {
        var sb = new StringBuilder();
        for (int k = 0; k < fmt.Length; k++)
        {
            if (fmt[k] != '%' || k + 1 >= fmt.Length) { sb.Append(fmt[k]); continue; }
            int start = k++;
            string flags = "";
            while (k < fmt.Length && "-_0^#".IndexOf(fmt[k]) >= 0) flags += fmt[k++];
            int width = -1;
            while (k < fmt.Length && char.IsDigit(fmt[k])) width = Math.Max(width, 0) * 10 + (fmt[k++] - '0');
            int colons = 0;
            while (k < fmt.Length && fmt[k] == ':') { colons++; k++; }
            if (k >= fmt.Length) { sb.Append(fmt[start..]); break; }
            char c = fmt[k];
            string Num(long v, int w, char pad = '0')
            {
                if (flags.Contains('-')) return v.ToString(CultureInfo.InvariantCulture);
                if (flags.Contains('_')) pad = ' ';
                else if (flags.Contains('0')) pad = '0';
                int ww = width >= 0 ? width : w;
                var s = Math.Abs(v).ToString(CultureInfo.InvariantCulture);
                if (v < 0) return "-" + s.PadLeft(ww - 1, pad);
                return s.PadLeft(ww, pad);
            }
            string Txt(string s)
            {
                if (flags.Contains('^')) s = s.ToUpperInvariant();
                if (flags.Contains('#')) s = s.Any(char.IsLower) ? s.ToUpperInvariant() : s.ToLowerInvariant();
                if (width > s.Length) s = s.PadLeft(width, flags.Contains('0') ? '0' : ' ');
                return s;
            }
            int hour12 = t.Hour % 12 == 0 ? 12 : t.Hour % 12;
            long nanos = t.Ticks % TimeSpan.TicksPerSecond * 100;
            string r = c switch
            {
                'Y' => t.Year >= 0 && t.Year < 10000 && width < 0 && !flags.Contains('-') ? t.Year.ToString("0000", CultureInfo.InvariantCulture) : Num(t.Year, 4),
                'C' => Num(t.Year / 100, 2),
                'y' => Num(t.Year % 100, 2),
                'm' => Num(t.Month, 2),
                'B' => Txt(MonthNames[t.Month - 1]),
                'b' or 'h' => Txt(MonthNames[t.Month - 1][..3]),
                'd' => Num(t.Day, 2),
                'e' => Num(t.Day, 2, ' '),
                'j' => Num(t.DayOfYear, 3),
                'H' => Num(t.Hour, 2),
                'k' => Num(t.Hour, 2, ' '),
                'I' => Num(hour12, 2),
                'l' => Num(hour12, 2, ' '),
                'P' => Txt(t.Hour < 12 ? "am" : "pm"),
                'p' => flags.Contains('#') ? (t.Hour < 12 ? "am" : "pm") : Txt(t.Hour < 12 ? "AM" : "PM"),
                'M' => Num(t.Minute, 2),
                'S' => Num(t.Second, 2),
                'L' => Frac(nanos, width < 0 ? 3 : width),
                'N' => Frac(nanos, width < 0 ? 9 : width),
                'z' => Txt(OffsetZ(t.Offset, colons, flags)),
                'Z' => Txt(zone ?? (isDate ? FormatOffset(t.Offset, ":") : "")),
                'A' => Txt(DayNames[(int)t.DayOfWeek]),
                'a' => Txt(DayNames[(int)t.DayOfWeek][..3]),
                'u' => Num(t.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)t.DayOfWeek, 1),
                'w' => Num((int)t.DayOfWeek, 1),
                'G' => Num(ISOWeek.GetYear(t.DateTime), 4),
                'g' => Num(ISOWeek.GetYear(t.DateTime) % 100, 2),
                'V' => Num(ISOWeek.GetWeekOfYear(t.DateTime), 2),
                'U' => Num((t.DayOfYear + 6 - (int)t.DayOfWeek) / 7, 2),
                'W' => Num((t.DayOfYear + 6 - ((int)t.DayOfWeek + 6) % 7) / 7, 2),
                's' => Num(t.ToUnixTimeSeconds(), 1),
                'Q' => Num(t.ToUnixTimeMilliseconds(), 1),
                'n' => "\n",
                't' => "\t",
                '%' => "%",
                'c' => Txt(Strftime(t, zone, "%a %b %e %H:%M:%S %Y", isDate)),
                'D' or 'x' => Txt(Strftime(t, zone, "%m/%d/%y", isDate)),
                'F' => Txt(Strftime(t, zone, "%Y-%m-%d", isDate)),
                'T' or 'X' => Txt(Strftime(t, zone, "%H:%M:%S", isDate)),
                'R' => Txt(Strftime(t, zone, "%H:%M", isDate)),
                'r' => Txt(Strftime(t, zone, "%I:%M:%S %p", isDate)),
                'v' => Txt(Strftime(t, zone, "%e-%^b-%Y", isDate)),
                '+' => Txt(Strftime(t, zone, "%a %b %e %H:%M:%S %Z %Y", isDate)),
                _ => fmt[start..(k + 1)],
            };
            sb.Append(r);
        }
        return sb.ToString();
    }

    static string Frac(long nanos, int digits)
    {
        var s = nanos.ToString("000000000", CultureInfo.InvariantCulture);
        return digits <= 9 ? s[..digits] : s + new string('0', digits - 9);
    }

    static string OffsetZ(TimeSpan off, int colons, string flags)
    {
        var sign = off < TimeSpan.Zero ? "-" : "+";
        var d = off.Duration();
        string hh = d.Hours.ToString(flags.Contains('-') ? "0" : "00", CultureInfo.InvariantCulture), mm = d.Minutes.ToString("00", CultureInfo.InvariantCulture), ss = d.Seconds.ToString("00", CultureInfo.InvariantCulture);
        return colons switch
        {
            0 => sign + hh + mm,
            1 => sign + hh + ":" + mm,
            _ => sign + hh + ":" + mm + ":" + ss,
        };
    }

    // ---- parsing (Date._parse's common shapes) ----

    sealed class DateParts
    {
        public int? Year, Mon, Mday, Hour, Min, Sec, Wday, Yday;
        public long Nanos;
        public TimeSpan? Offset;
        public string Zone;
        public long? Seconds;   // %s
    }

    static int MonthIndex(string s)
    {
        if (s.Length < 3) return 0;
        var p = s[..3].ToLowerInvariant();
        for (int i = 0; i < 12; i++) if (MonthNames[i][..3].ToLowerInvariant() == p) return i + 1;
        return 0;
    }

    static int ComplYear(string digits, int y) => digits.Length <= 2 && !digits.StartsWith('-') ? (y >= 69 ? y + 1900 : y + 2000) : y;

    static int I(string s) => int.Parse(s, CultureInfo.InvariantCulture);

    static long NanosOf(string frac) => frac.Length == 0 ? 0 : long.Parse((frac + "000000000")[..9], CultureInfo.InvariantCulture);

    const string MonRe = "(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*\\.?";

    static DateParts ParseDateText(string str)
    {
        var p = new DateParts();
        var s = str;
        string Cut(Match m) => s[..m.Index] + " " + s[(m.Index + m.Length)..];
        // weekday name
        var wm = Regex.Match(s, @"\b(sun|mon|tue|wed|thu|fri|sat)[a-z]*\.?", RegexOptions.IgnoreCase);
        if (wm.Success)
        {
            p.Wday = Array.FindIndex(DayNames, d => d[..3].Equals(wm.Groups[1].Value, StringComparison.OrdinalIgnoreCase));
            s = Cut(wm);
        }
        // time of day, with optional zone
        var tm = Regex.Match(s, @"(?<![\d:])(\d{1,2}):(\d{2})(?::(\d{2})(?:[.,](\d+))?)?\s*(?:([ap])\.?m\.?\b)?(?:\s*(Z\b|[+-]\d{1,2}(?::?\d{2}(?::?\d{2})?)?|(?:UTC|GMT)(?:[+-]\d{1,2}(?::?\d{2})?)?|[A-Z]{3,4}\b))?", RegexOptions.IgnoreCase);
        // "3pm": groups 2-4 and 6 match empty
        if (!tm.Success) tm = Regex.Match(s, @"(?<!\d)(\d{1,2})()()()\s*([ap])\.?m\.?\b()", RegexOptions.IgnoreCase);
        if (tm.Success)
        {
            p.Hour = I(tm.Groups[1].Value);
            p.Min = tm.Groups[2].Value.Length > 0 ? I(tm.Groups[2].Value) : 0;
            if (tm.Groups[3].Success && tm.Groups[3].Value.Length > 0) p.Sec = I(tm.Groups[3].Value);
            if (tm.Groups[4].Success) p.Nanos = NanosOf(tm.Groups[4].Value);
            if (tm.Groups[5].Success && tm.Groups[5].Value.Length > 0)
            {
                bool pm = char.ToLowerInvariant(tm.Groups[5].Value[0]) == 'p';
                p.Hour = p.Hour % 12 + (pm ? 12 : 0);
            }
            if (tm.Groups[6].Success && tm.Groups[6].Value.Length > 0 && ParseOffset(tm.Groups[6].Value) is { } off)
            {
                p.Offset = off;
                p.Zone = tm.Groups[6].Value;
            }
            s = Cut(tm);
        }
        Match m;
        // ISO 8601 date: 2001-02-03, -2001-02-03, 2001-W05-6 not handled
        if ((m = Regex.Match(s, @"(?<![\d-])([-+]?\d{2,})-(\d{1,2})-(\d{1,2})(?!\d)")).Success)
        {
            p.Year = ComplYear(m.Groups[1].Value, I(m.Groups[1].Value)); p.Mon = I(m.Groups[2].Value); p.Mday = I(m.Groups[3].Value);
            s = Cut(m);
            // a "T" or space-separated time came out already; an ISO "T030405" compact time follows the date
        }
        else if ((m = Regex.Match(s, @"(?<!\d)(\d{1,2})(?:st|nd|rd|th)?\b[^-\d\s']*\s*(?:of\s+)?" + MonRe + @"(?:[^-\d\s']*\s*,?\s*('?-?\d+))?", RegexOptions.IgnoreCase)).Success
                 && MonthIndex(m.Groups[2].Value) > 0)
        {
            p.Mday = I(m.Groups[1].Value); p.Mon = MonthIndex(m.Groups[2].Value);
            if (m.Groups[3].Success) { var y = m.Groups[3].Value.TrimStart('\''); p.Year = ComplYear(y, I(y)); }
            s = Cut(m);
        }
        else if ((m = Regex.Match(s, @"\b" + MonRe + @"\s*(?:(\d{1,2})(?:st|nd|rd|th)?\b(?!:))?(?:\s*,?\s*('?-?\d{3,}|'\d{2}))?", RegexOptions.IgnoreCase)).Success
                 && MonthIndex(m.Groups[1].Value) > 0 && (m.Groups[2].Success || m.Groups[3].Success))
        {
            p.Mon = MonthIndex(m.Groups[1].Value);
            if (m.Groups[2].Success) p.Mday = I(m.Groups[2].Value);
            if (m.Groups[3].Success) { var y = m.Groups[3].Value.TrimStart('\''); p.Year = ComplYear(y, I(y)); }
            if (!m.Groups[2].Success) p.Mday = null;
            s = Cut(m);
        }
        else if ((m = Regex.Match(s, @"(?<!\d)('?-?\d+)/\s*('?\d+)(?:\D\s*('?-?\d+))?")).Success)
        {
            ThreeParts(p, m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Success ? m.Groups[3].Value : null);
            s = Cut(m);
        }
        else if ((m = Regex.Match(s, @"(?<!\d)('?-?\d+)\.\s*('?\d+)\.\s*('?-?\d+)")).Success)
        {
            ThreeParts(p, m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
            s = Cut(m);
        }
        else if ((m = Regex.Match(s, @"(?<![\d.:])(\d{2}|\d{4})?(\d{2})(\d{2})(?:T?(\d{2})(\d{2})(\d{2})?(?:[.,](\d+))?)?\s*(Z|[+-]\d{2}:?\d{2})?(?![\d.:])")).Success
                 && (m.Groups[1].Success || m.Groups[4].Success || p.Hour == null))
        {
            // 20010203, 010203, 20010203T040506Z
            if (m.Groups[1].Success) { p.Year = ComplYear(m.Groups[1].Value, I(m.Groups[1].Value)); p.Mon = I(m.Groups[2].Value); p.Mday = I(m.Groups[3].Value); }
            else { p.Mon = I(m.Groups[2].Value); p.Mday = I(m.Groups[3].Value); }
            if (m.Groups[4].Success) { p.Hour = I(m.Groups[4].Value); p.Min = I(m.Groups[5].Value); if (m.Groups[6].Success) p.Sec = I(m.Groups[6].Value); }
            if (m.Groups[7].Success) p.Nanos = NanosOf(m.Groups[7].Value);
            if (m.Groups[8].Success) { p.Offset = ParseOffset(m.Groups[8].Value); p.Zone = m.Groups[8].Value; }
            s = Cut(m);
        }
        else if (p.Mday == null && (m = Regex.Match(s, @"(?<!\d)(\d{1,2})(?:st|nd|rd|th)\b", RegexOptions.IgnoreCase)).Success)
        {
            p.Mday = I(m.Groups[1].Value);
            s = Cut(m);
        }
        // a lone zone after the date ("2001-02-03 UTC")
        if (p.Offset == null && Regex.Match(s, @"(?<![A-Za-z])(Z|UTC|GMT|[+-]\d{2}:?\d{2})(?![A-Za-z\d])") is { Success: true } zm)
        {
            p.Offset = ParseOffset(zm.Groups[1].Value);
            p.Zone = zm.Groups[1].Value;
        }
        return p;
    }

    static void ThreeParts(DateParts p, string a, string b, string c)
    {
        string A = a.TrimStart('\''), B = b.TrimStart('\'');
        if (c == null)
        {
            // "2001/02" → year/mon, "02/03" → mon/mday
            if (A.TrimStart('-').Length > 2) { p.Year = I(A); p.Mon = I(B); }
            else { p.Mon = I(A); p.Mday = I(B); }
            return;
        }
        var C = c.TrimStart('\'');
        if (A.TrimStart('-').Length > 2 || a.StartsWith('\'')) { p.Year = ComplYear(A, I(A)); p.Mon = I(B); p.Mday = I(C); }
        else if (C.TrimStart('-').Length > 2 || c.StartsWith('\'')) { p.Mday = I(A); p.Mon = I(B); p.Year = ComplYear(C, I(C)); }
        else { p.Year = ComplYear(A, I(A)); p.Mon = I(B); p.Mday = I(C); }
    }

    // Date.strptime / Time.strptime / DateTime.strptime.
    DateParts Strptime(string str, string fmt) => Strptime(str, fmt, out _);

    DateParts Strptime(string str, string fmt, out int rest)
    {
        // Composite conversions are spelled out first.
        fmt = Regex.Replace(fmt, "%[-_0^#]*([TRFDxXrc+v])", m => m.Groups[1].Value switch
        {
            "T" or "X" => "%H:%M:%S", "R" => "%H:%M", "F" => "%Y-%m-%d", "D" or "x" => "%m/%d/%y", "r" => "%I:%M:%S %p",
            "c" => "%a %b %e %H:%M:%S %Y", "+" => "%a %b %e %H:%M:%S %Z %Y", _ => "%e-%b-%Y",
        });
        var p = new DateParts();
        int si = 0;
        bool Fail() => throw ArgError("invalid date or strptime format - '" + str + "' '" + fmt + "'");
        int ReadNum(int max, bool signed = false)
        {
            int st = si;
            if (signed && si < str.Length && str[si] is '-' or '+') si++;
            while (si < str.Length && si - st < max && char.IsDigit(str[si])) si++;
            if (si == st || (signed && si == st + 1 && !char.IsDigit(str[si - 1]))) Fail();
            return int.Parse(str[st..si], CultureInfo.InvariantCulture);
        }
        int ReadName(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                foreach (var cand in new[] { names[i], names[i][..3] })
                    if (string.Compare(str, si, cand, 0, cand.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        si += cand.Length;
                        return i;
                    }
            }
            Fail();
            return 0;
        }
        bool pm = false, hasP = false;
        for (int k = 0; k < fmt.Length; k++)
        {
            char c = fmt[k];
            if (char.IsWhiteSpace(c)) { while (si < str.Length && char.IsWhiteSpace(str[si])) si++; continue; }
            if (c != '%' || k + 1 >= fmt.Length)
            {
                if (si >= str.Length || str[si] != c) Fail();
                si++;
                continue;
            }
            c = fmt[++k];
            while (c is '-' or '_' or '0' or '^' or '#' && k + 1 < fmt.Length) c = fmt[++k];
            int colons = 0;
            while (c == ':' && k + 1 < fmt.Length) { colons++; c = fmt[++k]; }
            switch (c)
            {
                case 'Y': p.Year = ReadNum(5, true); break;
                case 'C': p.Year = ReadNum(2) * 100 + (p.Year ?? 0) % 100; break;
                case 'y': { int y = ReadNum(2); p.Year = y >= 69 ? y + 1900 : y + 2000; break; }
                case 'm': p.Mon = ReadNum(2); break;
                case 'd': case 'e':
                    if (si < str.Length && str[si] == ' ') si++;
                    p.Mday = ReadNum(2);
                    break;
                case 'j': p.Yday = ReadNum(3); break;
                case 'H': case 'k':
                    if (si < str.Length && str[si] == ' ') si++;
                    p.Hour = ReadNum(2);
                    break;
                case 'I': case 'l':
                    if (si < str.Length && str[si] == ' ') si++;
                    p.Hour = ReadNum(2);
                    break;
                case 'M': p.Min = ReadNum(2); break;
                case 'S': p.Sec = ReadNum(2); break;
                case 'L': case 'N':
                {
                    int st = si;
                    while (si < str.Length && char.IsDigit(str[si])) si++;
                    if (si == st) Fail();
                    p.Nanos = NanosOf(str[st..si]);
                    break;
                }
                case 'p': case 'P':
                {
                    var ampm = str[si..].ToLowerInvariant();
                    if (ampm.StartsWith("a.m.") || ampm.StartsWith("p.m.")) { pm = ampm[0] == 'p'; si += 4; }
                    else if (ampm.StartsWith("am") || ampm.StartsWith("pm")) { pm = ampm[0] == 'p'; si += 2; }
                    else Fail();
                    hasP = true;
                    break;
                }
                case 'B': case 'b': case 'h': p.Mon = ReadName(MonthNames) + 1; break;
                case 'A': case 'a': p.Wday = ReadName(DayNames); break;
                case 'u': p.Wday = ReadNum(1) % 7; break;
                case 'w': p.Wday = ReadNum(1); break;
                case 's':
                {
                    int st = si;
                    if (si < str.Length && str[si] == '-') si++;
                    while (si < str.Length && char.IsDigit(str[si])) si++;
                    if (si == st) Fail();
                    p.Seconds = long.Parse(str[st..si], CultureInfo.InvariantCulture);
                    break;
                }
                case 'z': case 'Z':
                {
                    var m = Regex.Match(str[si..], @"^(Z\b|[+-]\d{1,2}(?::?\d{2}(?::?\d{2})?)?|(?:UTC|GMT)(?:[+-]\d{1,2}(?::?\d{2})?)?|[A-Za-z]{3,5}\b)");
                    if (!m.Success) Fail();
                    p.Zone = m.Value;
                    p.Offset = ParseOffset(m.Value);
                    si += m.Length;
                    break;
                }
                case 'n': case 't': while (si < str.Length && char.IsWhiteSpace(str[si])) si++; break;
                case '%': if (si >= str.Length || str[si] != '%') Fail(); si++; break;
                default: Fail(); break;
            }
        }
        if (hasP && p.Hour != null) p.Hour = p.Hour % 12 + (pm ? 12 : 0);
        rest = str.Length - si;
        return p;
    }

    DateTimeOffset CivilOrRaise(int y, int m, int d, int h, int mi, int s, long nanos, TimeSpan off, string what = "invalid date")
    {
        if (m < 0) m += 13;
        if (m < 1 || m > 12 || y < 1 || y > 9999) throw Error(DateError(), what);
        int dim = DateTime.DaysInMonth(y, m);
        if (d < 0) d += dim + 1;
        if (d < 1 || d > dim || h < 0 || h > 24 || mi < 0 || mi > 59 || s < 0 || s > 60) throw Error(DateError(), what);
        var extra = TimeSpan.Zero;
        if (h == 24) { h = 0; extra += TimeSpan.FromDays(1); }
        if (s == 60) { s = 59; extra += TimeSpan.FromSeconds(1); }
        return new DateTimeOffset(y, m, d, h, mi, s, off).AddTicks(nanos / 100) + extra;
    }

    RClass DateError() => DateClass.Consts.GetValueOrDefault("Error") as RClass ?? ArgumentError;

    // ---- Time ----

    void InitTime()
    {
        var T = TimeClass = ObjectClass.Consts.GetValueOrDefault("Time") as RClass ?? DefineClass("Time");
        T.Includes.Add(ComparableModule);
        RTime Me(object o) => TimeOf(o);

        DefS(T, "now", (it, self, a, b) =>
        {
            var inz = Kw(a, "in");
            var now = DateTime.UtcNow;
            return inz != null ? it.InZone(new DateTimeOffset(now), inz) : it.LocalTime(now);
        });
        DefS(T, "at", (it, self, a, b) =>
        {
            var pos = Positional(a);
            var t = pos[0] is RObject { Host: RTime or DateTime } ? it.TimeOf(pos[0]).T : DateTimeOffset.UnixEpoch.AddTicks(it.SecondsToTicks(pos[0]));
            if (pos.Length > 1)
            {
                var unit = pos.Length > 2 ? it.StrOrSym(pos[2]) : "microsecond";
                double mult = unit switch { "millisecond" => 1e4, "usec" or "microsecond" => 10, "nsec" or "nanosecond" => 0.01, _ => throw it.ArgError("unexpected unit: " + unit) };
                t = t.AddTicks((long)Math.Round(it.Num(pos[1]) * mult));
            }
            var inz = Kw(a, "in");
            return inz != null ? it.InZone(t, inz) : it.LocalTime(t.UtcDateTime);
        });
        Native civil(TimeKind kind, bool isNew) => (it, self, a, b) =>
        {
            var pos = Positional(a);
            if (pos.Length == 0 && isNew) return Kw(a, "in") is { } inz ? it.InZone(DateTimeOffset.UtcNow, inz) : it.LocalTime(DateTime.UtcNow);
            if (pos.Length == 0) throw it.ArgCount(a, 1, 8);
            if (pos.Length == 1 && pos[0] is RString str && isNew)
            {
                var parts = ParseDateText(str.V);
                if (parts.Year == null || parts.Mon == null || parts.Mday == null) throw it.ArgError("can't parse: " + it.Inspect(str));
                return it.TimeFromParts(parts, Kw(a, "in"), null);
            }
            int y = pos[0] is RString ys ? int.Parse(ys.V, CultureInfo.InvariantCulture) : (int)it.Int(pos[0]);
            int mo = pos.Length > 1 && pos[1] != null ? it.MonthArg(pos[1]) : 1;
            int d = pos.Length > 2 && pos[2] != null ? (int)it.Int(pos[2]) : 1;
            int h = pos.Length > 3 && pos[3] != null ? (int)it.Int(pos[3]) : 0;
            int mi = pos.Length > 4 && pos[4] != null ? (int)it.Int(pos[4]) : 0;
            double sec = pos.Length > 5 && pos[5] != null ? it.Num(pos[5] is RString ss ? double.Parse(ss.V, CultureInfo.InvariantCulture) : pos[5]) : 0;
            object zone = pos.Length > 6 ? pos[6] : Kw(a, "in");
            int whole = (int)Math.Floor(sec);
            long nanos = (long)Math.Round((sec - whole) * 1e9);
            if (mo < 1 || mo > 12 || d < 1 || d > 31 || h < 0 || h > 24 || mi < 0 || mi > 59 || whole < 0 || whole > 60) throw it.ArgError("argument out of range");
            d = Math.Min(d, DateTime.DaysInMonth(y, mo));
            var wall = new DateTime(y, mo, d, 0, 0, 0).AddHours(h).AddMinutes(mi).AddSeconds(whole).AddTicks(nanos / 100);
            if (kind == TimeKind.Utc) return it.NewTime(new DateTimeOffset(wall, TimeSpan.Zero), TimeKind.Utc);
            if (zone != null)
            {
                if (zone is RString zs && zs.V is "UTC" or "Z" or "-00:00") return it.NewTime(new DateTimeOffset(wall, TimeSpan.Zero), TimeKind.Utc);
                return it.NewTime(new DateTimeOffset(wall, it.OffsetArg(zone)), TimeKind.Fixed);
            }
            return it.NewTime(new DateTimeOffset(wall, it.LocalOffsetForWall(wall)), TimeKind.Local);
        };
        DefS(T, "new", civil(TimeKind.Local, true));
        DefS(T, "local mktime", civil(TimeKind.Local, false));
        DefS(T, "utc gm", civil(TimeKind.Utc, false));
        DefS(T, "parse", (it, self, a, b) =>
        {
            var str = it.Str(a[0]);
            var parts = ParseDateText(str);
            if (parts.Year == null && parts.Mon == null && parts.Mday == null && parts.Hour == null) throw it.ArgError("no time information in " + it.Inspect(a[0]));
            return it.TimeFromParts(parts, null, a.Length > 1 ? a[1] : null);
        });
        DefS(T, "iso8601 xmlschema", (it, self, a, b) =>
        {
            var str = it.Str(a[0]);
            var m = Regex.Match(str, @"^\s*(-?\d{4,})-(\d\d)-(\d\d)(?:T(\d\d):(\d\d):(\d\d)(?:\.(\d+))?(Z|[+-]\d\d:?\d\d)?)?\s*$", RegexOptions.IgnoreCase);
            if (!m.Success) throw it.ArgError("invalid xmlschema format: " + it.Inspect(a[0]));
            var parts = new DateParts { Year = I(m.Groups[1].Value), Mon = I(m.Groups[2].Value), Mday = I(m.Groups[3].Value) };
            if (m.Groups[4].Success) { parts.Hour = I(m.Groups[4].Value); parts.Min = I(m.Groups[5].Value); parts.Sec = I(m.Groups[6].Value); }
            if (m.Groups[7].Success) parts.Nanos = NanosOf(m.Groups[7].Value);
            if (m.Groups[8].Success) { parts.Offset = ParseOffset(m.Groups[8].Value); parts.Zone = m.Groups[8].Value; }
            return it.TimeFromParts(parts, null, null);
        });
        DefS(T, "strptime", (it, self, a, b) =>
        {
            var parts = it.Strptime(it.Str(a[0]), it.Str(a[1]));
            if (parts.Seconds is long secs) return it.LocalTime(DateTimeOffset.FromUnixTimeSeconds(secs).UtcDateTime);
            return it.TimeFromParts(parts, null, a.Length > 2 ? a[2] : null);
        });
        DefS(T, "rfc2822 rfc822 httpdate", (it, self, a, b) => it.TimeFromParts(ParseDateText(it.Str(a[0])), null, null));

        Def(T, "year", (it, self, a, b) => (long)Me(self).T.Year);
        Def(T, "month mon", (it, self, a, b) => (long)Me(self).T.Month);
        Def(T, "day mday", (it, self, a, b) => (long)Me(self).T.Day);
        Def(T, "hour", (it, self, a, b) => (long)Me(self).T.Hour);
        Def(T, "min", (it, self, a, b) => (long)Me(self).T.Minute);
        Def(T, "sec", (it, self, a, b) => (long)Me(self).T.Second);
        Def(T, "usec tv_usec", (it, self, a, b) => Me(self).T.Ticks % TimeSpan.TicksPerSecond / 10);
        Def(T, "nsec tv_nsec", (it, self, a, b) => Me(self).T.Ticks % TimeSpan.TicksPerSecond * 100);
        Def(T, "subsec", (it, self, a, b) => Me(self).T.Ticks % TimeSpan.TicksPerSecond / (double)TimeSpan.TicksPerSecond);
        Def(T, "wday", (it, self, a, b) => (long)Me(self).T.DayOfWeek);
        Def(T, "yday", (it, self, a, b) => (long)Me(self).T.DayOfYear);
        for (int wd = 0; wd < 7; wd++)
        {
            var day = wd;
            Def(T, DayNames[wd].ToLowerInvariant() + "?", (it, self, a, b) => (int)Me(self).T.DayOfWeek == day);
        }
        Def(T, "zone", (it, self, a, b) => it.ZoneName(Me(self)) is { } z ? S(z) : null);
        Def(T, "utc? gmt?", (it, self, a, b) => Me(self).Kind == TimeKind.Utc);
        Def(T, "dst? isdst", (it, self, a, b) => Me(self).Kind == TimeKind.Local && it.LocalZone().IsDaylightSavingTime(Me(self).T.UtcDateTime));
        Def(T, "utc_offset gmt_offset gmtoff", (it, self, a, b) => (long)Me(self).T.Offset.TotalSeconds);
        Def(T, "utc gmtime", (it, self, a, b) =>
        {
            var t = Me(self);
            if (((RObject)self).Host is RTime) { t.T = t.T.ToUniversalTime(); t.Kind = TimeKind.Utc; return self; }
            return it.NewTime(t.T.ToUniversalTime(), TimeKind.Utc);
        });
        Def(T, "getutc getgm", (it, self, a, b) => it.NewTime(Me(self).T.ToUniversalTime(), TimeKind.Utc));
        Def(T, "localtime", (it, self, a, b) =>
        {
            var t = Me(self);
            var nt = a.Length > 0 && a[0] != null ? new RTime { T = t.T.ToOffset(it.OffsetArg(a[0])), Kind = TimeKind.Fixed } : new RTime { T = t.T.ToOffset(it.LocalOffset(t.T.UtcDateTime)), Kind = TimeKind.Local };
            if (((RObject)self).Host is RTime) { t.T = nt.T; t.Kind = nt.Kind; return self; }
            return it.NewTime(nt.T, nt.Kind);
        });
        Def(T, "getlocal", (it, self, a, b) =>
        {
            var t = Me(self);
            return a.Length > 0 && a[0] != null ? it.NewTime(t.T.ToOffset(it.OffsetArg(a[0])), TimeKind.Fixed) : it.LocalTime(t.T.UtcDateTime);
        });
        Def(T, "to_i tv_sec", (it, self, a, b) => FloorDivTicks(Me(self).T.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks));
        Def(T, "to_f to_r", (it, self, a, b) => (Me(self).T.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) / (double)TimeSpan.TicksPerSecond);
        Def(T, "strftime", (it, self, a, b) => S(Strftime(Me(self).T, it.ZoneName(Me(self)), it.Str(a[0]))));
        Def(T, "iso8601 xmlschema", (it, self, a, b) =>
        {
            var t = Me(self);
            int n = a.Length > 0 ? (int)it.Int(a[0]) : 0;
            var frac = n > 0 ? "." + Frac(t.T.Ticks % TimeSpan.TicksPerSecond * 100, n) : "";
            return S(Strftime(t.T, null, "%Y-%m-%dT%H:%M:%S") + frac + (t.Kind == TimeKind.Utc ? "Z" : FormatOffset(t.T.Offset, ":")));
        });
        Def(T, "httpdate", (it, self, a, b) => S(Strftime(Me(self).T.ToUniversalTime(), "GMT", "%a, %d %b %Y %H:%M:%S GMT")));
        Def(T, "rfc2822 rfc822", (it, self, a, b) => S(Strftime(Me(self).T, null, "%a, %d %b %Y %H:%M:%S ") + (Me(self).Kind == TimeKind.Utc ? "-0000" : FormatOffset(Me(self).T.Offset, ""))));
        Def(T, "to_s", (it, self, a, b) =>
        {
            var t = Me(self);
            return S(Strftime(t.T, null, "%Y-%m-%d %H:%M:%S ") + (t.Kind == TimeKind.Utc ? "UTC" : FormatOffset(t.T.Offset, "")));
        });
        Def(T, "inspect", (it, self, a, b) =>
        {
            var t = Me(self);
            long nanos = t.T.Ticks % TimeSpan.TicksPerSecond * 100;
            var frac = nanos == 0 ? "" : "." + Frac(nanos, 9).TrimEnd('0');
            return S(Strftime(t.T, null, "%Y-%m-%d %H:%M:%S") + frac + " " + (t.Kind == TimeKind.Utc ? "UTC" : FormatOffset(t.T.Offset, "")));
        });
        Def(T, "+", (it, self, a, b) =>
        {
            if (a[0] is RObject { Host: RTime }) throw it.TypeErr("time + time?");
            return it.ShiftTime(Me(self), it.SecondsToTicks(a[0]));
        });
        Def(T, "-", (it, self, a, b) =>
        {
            if (a[0] is RObject { Host: RTime or DateTime })
                return (Me(self).T.UtcTicks - it.TimeOf(a[0]).T.UtcTicks) / (double)TimeSpan.TicksPerSecond;
            return it.ShiftTime(Me(self), -it.SecondsToTicks(a[0]));
        });
        Def(T, "succ", (it, self, a, b) => it.ShiftTime(Me(self), TimeSpan.TicksPerSecond));
        Def(T, "<=>", (it, self, a, b) => a[0] is RObject { Host: RTime or DateTime } ? (long)Me(self).T.UtcTicks.CompareTo(it.TimeOf(a[0]).T.UtcTicks) : null);
        Def(T, "== eql?", (it, self, a, b) => a[0] is RObject { Host: RTime or DateTime } && Me(self).T.UtcTicks == it.TimeOf(a[0]).T.UtcTicks);
        Def(T, "hash", (it, self, a, b) => (long)Me(self).T.UtcTicks.GetHashCode());
        Def(T, "round", (it, self, a, b) =>
        {
            int n = a.Length > 0 ? (int)it.Int(a[0]) : 0;
            long unit = (long)Math.Pow(10, Math.Max(0, 7 - n));
            var t = Me(self);
            long ticks = t.T.Ticks;
            long r = (ticks + unit / 2) / unit * unit;
            return it.NewTime(new DateTimeOffset(r, t.T.Offset), t.Kind);
        });
        Def(T, "floor", (it, self, a, b) =>
        {
            int n = a.Length > 0 ? (int)it.Int(a[0]) : 0;
            long unit = (long)Math.Pow(10, Math.Max(0, 7 - n));
            var t = Me(self);
            return it.NewTime(new DateTimeOffset(t.T.Ticks / unit * unit, t.T.Offset), t.Kind);
        });
        Def(T, "to_a", (it, self, a, b) =>
        {
            var t = Me(self);
            return new RArray(new object[] { (long)t.T.Second, (long)t.T.Minute, (long)t.T.Hour, (long)t.T.Day, (long)t.T.Month, (long)t.T.Year,
                (long)t.T.DayOfWeek, (long)t.T.DayOfYear, it.Invoke(self, "dst?", NoArgs, null), it.ZoneName(t) is { } z ? S(z) : null });
        });
        Def(T, "to_time", (it, self, a, b) => self);
        Def(T, "to_date", (it, self, a, b) => it.NewDate(Me(self).T.Year, Me(self).T.Month, Me(self).T.Day));
        Def(T, "to_datetime", (it, self, a, b) => new RObject { Class = it.DateTimeClass, Host = new RDate { T = Me(self).T, Dt = true } });
        Def(T, "dup clone", (it, self, a, b) => it.NewTime(Me(self).T, Me(self).Kind));

        InitDate();
    }

    static long FloorDivTicks(long ticks) => ticks >= 0 ? ticks / TimeSpan.TicksPerSecond : -((-ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond);

    long SecondsToTicks(object o) => o switch
    {
        long l => l * TimeSpan.TicksPerSecond,
        double d => (long)Math.Round(d * TimeSpan.TicksPerSecond),
        BigInteger bi => (long)(bi * TimeSpan.TicksPerSecond),
        _ => RespondTo(o, "to_f") ? (long)Math.Round(Num(Invoke(o, "to_f", NoArgs, null)) * TimeSpan.TicksPerSecond) : throw TypeErr($"can't convert {TypeName(o)} into an exact number"),
    };

    int MonthArg(object o)
    {
        if (o is RString s)
        {
            if (int.TryParse(s.V, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return n;
            int mi = MonthIndex(s.V);
            if (mi == 0) throw ArgError("mon out of range");
            return mi;
        }
        return (int)Int(o);
    }

    RObject ShiftTime(RTime t, long ticks)
    {
        var nt = t.T.AddTicks(ticks);
        return t.Kind == TimeKind.Local ? LocalTime(nt.UtcDateTime) : NewTime(nt, t.Kind);
    }

    RObject InZone(DateTimeOffset t, object zone)
    {
        if (zone is RString zs && zs.V is "UTC" or "Z" or "-00:00") return NewTime(t.ToUniversalTime(), TimeKind.Utc);
        return NewTime(t.ToOffset(OffsetArg(zone)), TimeKind.Fixed);
    }

    // Missing date parts come from `now` (today), missing time parts are 0, a missing zone is local.
    RObject TimeFromParts(DateParts p, object zone, object nowArg)
    {
        DateTimeOffset now = nowArg is RObject { Host: RTime or DateTime } ? TimeOf(nowArg).T : DateTimeOffset.UtcNow.ToOffset(LocalOffset(DateTime.UtcNow));
        if (p.Seconds is long secs) return LocalTime(DateTimeOffset.FromUnixTimeSeconds(secs).UtcDateTime);
        int y = p.Year ?? now.Year;
        int mo = p.Mon ?? (p.Year == null ? now.Month : 1);
        int d = p.Mday ?? (p.Mon == null && p.Year == null ? now.Day : 1);
        if (p.Yday is int yd && p.Mon == null) { var dt = new DateTime(y, 1, 1).AddDays(yd - 1); mo = dt.Month; d = dt.Day; }
        int h = p.Hour ?? 0, mi = p.Min ?? 0, s = p.Sec ?? 0;
        if (mo < 1 || mo > 12 || d < 1 || d > DateTime.DaysInMonth(y, mo) || h > 24 || mi > 59 || s > 60) throw ArgError("argument out of range");
        var wall = new DateTime(y, mo, d).AddHours(h).AddMinutes(mi).AddSeconds(s).AddTicks(p.Nanos / 100);
        if (zone != null) return InZone(new DateTimeOffset(wall, zone is RString zz && ParseOffset(zz.V) is { } zo ? zo : TimeSpan.Zero), zone);
        if (p.Offset is { } off)
        {
            var t = new DateTimeOffset(wall, off);
            // Ruby's Time.parse answers a UTC Time for "Z"/"UTC"/"GMT", local time for a numeric offset matching local.
            if (p.Zone is { } zn && zn.ToUpperInvariant() is "Z" or "UTC" or "GMT" or "UT") return NewTime(t, TimeKind.Utc);
            return off == LocalOffset(t.UtcDateTime) ? NewTime(t, TimeKind.Local) : NewTime(t, TimeKind.Fixed);
        }
        return NewTime(new DateTimeOffset(wall, LocalOffsetForWall(wall)), TimeKind.Local);
    }

    // ---- Date / DateTime ----

    public RObject NewDate(int y, int m, int d) => new() { Class = DateClass, Host = new RDate { T = new DateTimeOffset(y, m, d, 0, 0, 0, TimeSpan.Zero) } };

    RObject DateLike(RClass cls, DateTimeOffset t, bool dt) => new() { Class = cls, Host = new RDate { T = dt ? t : new DateTimeOffset(t.Year, t.Month, t.Day, 0, 0, 0, TimeSpan.Zero), Dt = dt } };

    static long Jd(DateTimeOffset t) => (long)(t.Date - new DateTime(1, 1, 1)).TotalDays + 1721426;

    void InitDate()
    {
        var D = DateClass = DefineClass("Date");
        D.Includes.Add(ComparableModule);
        DefineClass("Error", ArgumentError, D);
        var DT = DateTimeClass = DefineClass("DateTime", D);
        RDate Me(object o) => (RDate)((RObject)o).Host;
        RClass Cls(object o) => ((RObject)o).Class;
        D.Consts["ITALY"] = 2299161L;
        D.Consts["ENGLAND"] = 2361222L;
        D.Consts["GREGORIAN"] = double.NegativeInfinity;
        D.Consts["MONTHNAMES"] = new RArray(new object[] { null }.Concat(MonthNames.Select(n => (object)S(n))));
        D.Consts["ABBR_MONTHNAMES"] = new RArray(new object[] { null }.Concat(MonthNames.Select(n => (object)S(n[..3]))));
        D.Consts["DAYNAMES"] = new RArray(DayNames.Select(n => (object)S(n)));
        D.Consts["ABBR_DAYNAMES"] = new RArray(DayNames.Select(n => (object)S(n[..3])));

        DefS(D, "today", (it, self, a, b) =>
        {
            var now = DateTimeOffset.UtcNow.ToOffset(it.LocalOffset(DateTime.UtcNow));
            return it.DateLike((RClass)self == it.DateTimeClass ? it.DateClass : (RClass)self, now, false);
        });
        DefS(D, "new civil", (it, self, a, b) =>
        {
            bool dt = it.IsSubclass((RClass)self, DT);
            int y = a.Length > 0 ? (int)it.Int(a[0]) : -4712;
            int m = a.Length > 1 ? (int)it.Int(a[1]) : 1;
            int d = a.Length > 2 ? (int)it.Int(a[2]) : 1;
            if (a.Length == 0) y = 1;   // Date.new => -4712-01-01; years before 1 aren't representable
            if (!dt) return it.DateLike((RClass)self, it.CivilOrRaise(y, m, d, 0, 0, 0, 0, TimeSpan.Zero), false);
            int h = a.Length > 3 ? (int)it.Int(a[3]) : 0, mi = a.Length > 4 ? (int)it.Int(a[4]) : 0;
            double sec = a.Length > 5 ? it.Num(a[5]) : 0;
            var off = a.Length > 6 ? it.DtOffset(a[6]) : TimeSpan.Zero;
            if (h < 0) h += 24;
            if (mi < 0) mi += 60;
            int whole = (int)Math.Floor(sec);
            return it.DateLike((RClass)self, it.CivilOrRaise(y, m, d, h, mi, whole, (long)Math.Round((sec - whole) * 1e9), off), true);
        });
        DefS(DT, "now", (it, self, a, b) => it.DateLike((RClass)self, DateTimeOffset.UtcNow.ToOffset(it.LocalOffset(DateTime.UtcNow)), true));
        DefS(D, "jd", (it, self, a, b) =>
        {
            var t = new DateTimeOffset(new DateTime(1, 1, 1).AddDays(it.Int(a.Length > 0 ? a[0] : 0L) - 1721426), TimeSpan.Zero);
            return it.DateLike((RClass)self, t, it.IsSubclass((RClass)self, DT));
        });
        DefS(D, "parse", (it, self, a, b) =>
        {
            var str = it.Str(a[0]);
            var p = ParseDateText(str);
            return it.DateFromParts((RClass)self, p, str);
        });
        DefS(D, "iso8601 xmlschema rfc3339", (it, self, a, b) =>
        {
            var str = it.Str(a[0]);
            if (!Regex.IsMatch(str, @"^\s*[-+]?\d{4,}-?\d\d-?\d\d(?:[T\s]\d\d:?\d\d(?::?\d\d(?:[.,]\d+)?)?\s*(?:Z|[+-]\d\d(?::?\d\d)?)?)?\s*$", RegexOptions.IgnoreCase))
                throw it.Error(it.DateError(), "invalid date");
            return it.DateFromParts((RClass)self, ParseDateText(str), str);
        });
        DefS(D, "strptime", (it, self, a, b) =>
        {
            var str = a.Length > 0 ? it.Str(a[0]) : "-4712-01-01";
            var fmt = a.Length > 1 ? it.Str(a[1]) : it.IsSubclass((RClass)self, DT) ? "%FT%T%z" : "%F";
            DateParts p;
            int rest;
            try { p = it.Strptime(str, fmt, out rest); }
            catch (RubyError) { throw it.Error(it.DateError(), "invalid date"); }
            if (rest != 0) throw it.Error(it.DateError(), "invalid date");
            if (p.Seconds is long secs) return it.DateLike((RClass)self, DateTimeOffset.FromUnixTimeSeconds(secs), it.IsSubclass((RClass)self, DT));
            return it.DateFromParts((RClass)self, p, str);
        });
        DefS(D, "_parse", (it, self, a, b) =>
        {
            var p = ParseDateText(it.Str(a[0]));
            var h = new RHash();
            void Put(string k, object v) { if (v != null) h.D[Sym(k)] = v; }
            Put("wday", p.Wday is int w ? (long)w : null);
            Put("year", p.Year is int y ? (long)y : null);
            Put("mon", p.Mon is int m ? (long)m : null);
            Put("mday", p.Mday is int d ? (long)d : null);
            Put("hour", p.Hour is int hh ? (long)hh : null);
            Put("min", p.Min is int mi ? (long)mi : null);
            Put("sec", p.Sec is int s ? (long)s : null);
            Put("zone", p.Zone is { } z ? S(z) : null);
            Put("offset", p.Offset is { } o ? (long)o.TotalSeconds : null);
            return h;
        });
        DefS(D, "valid_date? valid_civil?", (it, self, a, b) =>
        {
            long y = it.Int(a[0]), m = it.Int(a[1]), d = it.Int(a[2]);
            if (m < 0) m += 13;
            if (y < 1 || y > 9999 || m < 1 || m > 12) return false;
            int dim = DateTime.DaysInMonth((int)y, (int)m);
            if (d < 0) d += dim + 1;
            return d >= 1 && d <= dim;
        });
        DefS(D, "leap? gregorian_leap?", (it, self, a, b) => DateTime.IsLeapYear((int)it.Int(a[0])));

        Def(D, "year", (it, self, a, b) => (long)Me(self).T.Year);
        Def(D, "mon month", (it, self, a, b) => (long)Me(self).T.Month);
        Def(D, "mday day", (it, self, a, b) => (long)Me(self).T.Day);
        Def(D, "wday", (it, self, a, b) => (long)Me(self).T.DayOfWeek);
        Def(D, "yday", (it, self, a, b) => (long)Me(self).T.DayOfYear);
        Def(D, "cwday", (it, self, a, b) => Me(self).T.DayOfWeek == DayOfWeek.Sunday ? 7L : (long)Me(self).T.DayOfWeek);
        Def(D, "cweek", (it, self, a, b) => (long)ISOWeek.GetWeekOfYear(Me(self).T.DateTime));
        Def(D, "cwyear", (it, self, a, b) => (long)ISOWeek.GetYear(Me(self).T.DateTime));
        Def(D, "jd", (it, self, a, b) => Jd(Me(self).T));
        Def(D, "mjd", (it, self, a, b) => Jd(Me(self).T) - 2400001);
        Def(D, "leap?", (it, self, a, b) => DateTime.IsLeapYear(Me(self).T.Year));
        for (int wd = 0; wd < 7; wd++)
        {
            var day = wd;
            Def(D, DayNames[wd].ToLowerInvariant() + "?", (it, self, a, b) => (int)Me(self).T.DayOfWeek == day);
        }
        Def(D, "+", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddTicks((long)Math.Round(it.Num(a[0]) * TimeSpan.TicksPerDay)), Me(self).Dt));
        Def(D, "-", (it, self, a, b) =>
        {
            if (a[0] is RObject { Host: RDate other })
            {
                // Ruby answers a Rational number of days; whole days come back as an Integer here.
                var ticks = Me(self).T.UtcTicks - other.T.UtcTicks;
                return ticks % TimeSpan.TicksPerDay == 0 ? (object)(ticks / TimeSpan.TicksPerDay) : ticks / (double)TimeSpan.TicksPerDay;
            }
            return it.DateLike(Cls(self), Me(self).T.AddTicks(-(long)Math.Round(it.Num(a[0]) * TimeSpan.TicksPerDay)), Me(self).Dt);
        });
        Def(D, ">>", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddMonths((int)it.Int(a[0])), Me(self).Dt));
        Def(D, "<<", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddMonths(-(int)it.Int(a[0])), Me(self).Dt));
        Def(D, "next_day succ next", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddDays(a.Length > 0 ? it.Int(a[0]) : 1), Me(self).Dt));
        Def(D, "prev_day", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddDays(-(a.Length > 0 ? it.Int(a[0]) : 1)), Me(self).Dt));
        Def(D, "next_month", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddMonths(a.Length > 0 ? (int)it.Int(a[0]) : 1), Me(self).Dt));
        Def(D, "prev_month", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddMonths(-(a.Length > 0 ? (int)it.Int(a[0]) : 1)), Me(self).Dt));
        Def(D, "next_year", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddYears(a.Length > 0 ? (int)it.Int(a[0]) : 1), Me(self).Dt));
        Def(D, "prev_year", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.AddYears(-(a.Length > 0 ? (int)it.Int(a[0]) : 1)), Me(self).Dt));
        Def(D, "<=>", (it, self, a, b) => a[0] switch
        {
            RObject { Host: RDate o } => (long)Me(self).T.UtcTicks.CompareTo(o.T.UtcTicks),
            long or double => (long)Jd(Me(self).T).CompareTo((long)it.Num(a[0])),
            _ => null,
        });
        Def(D, "== eql?", (it, self, a, b) => a[0] is RObject { Host: RDate o } && Me(self).T.UtcTicks == o.T.UtcTicks);
        Def(D, "===", (it, self, a, b) => a[0] is RObject { Host: RDate o } && Jd(Me(self).T) == Jd(o.T));
        Def(D, "hash", (it, self, a, b) => (long)Me(self).T.UtcTicks.GetHashCode());
        Def(D, "strftime", (it, self, a, b) => S(Strftime(Me(self).T, Me(self).Dt ? FormatOffset(Me(self).T.Offset, ":") : "+00:00", a.Length > 0 ? it.Str(a[0]) : "%F", true)));
        Def(D, "iso8601 xmlschema to_s", (it, self, a, b) => S(Strftime(Me(self).T, null, "%Y-%m-%d")));
        Def(D, "rfc3339", (it, self, a, b) => S(Strftime(Me(self).T, null, "%Y-%m-%dT%H:%M:%S") + FormatOffset(Me(self).T.Offset, ":")));
        Def(D, "httpdate", (it, self, a, b) => S(Strftime(Me(self).T.ToUniversalTime(), "GMT", "%a, %d %b %Y %H:%M:%S GMT")));
        Def(D, "rfc2822 rfc822", (it, self, a, b) => S(Strftime(Me(self).T, null, "%a, %-d %b %Y %H:%M:%S ") + FormatOffset(Me(self).T.Offset, "")));
        Def(D, "inspect", (it, self, a, b) =>
        {
            var t = Me(self).T;
            if (!Me(self).Dt) return S($"#<{Cls(self).Name}: {Strftime(t, null, "%Y-%m-%d")} (({Jd(t)}j,0s,0n),+0s,2299161j)>");
            var utc = t.ToUniversalTime();
            long secs = (long)utc.TimeOfDay.TotalSeconds;
            return S($"#<{Cls(self).Name}: {Strftime(t, null, "%Y-%m-%dT%H:%M:%S")}{FormatOffset(t.Offset, ":")} (({Jd(utc)}j,{secs}s,{utc.Ticks % TimeSpan.TicksPerSecond * 100}n),{(long)t.Offset.TotalSeconds:+0;-0}s,2299161j)>");
        });
        Def(D, "to_date", (it, self, a, b) => it.DateLike(it.DateClass, Me(self).T, false));
        Def(D, "to_datetime", (it, self, a, b) => it.DateLike(it.DateTimeClass, Me(self).T, true));
        Def(D, "to_time", (it, self, a, b) =>
        {
            var t = Me(self);
            if (t.Dt) return it.NewTime(t.T, TimeKind.Fixed).Also(o => { if (t.T.Offset == it.LocalOffset(t.T.UtcDateTime)) ((RTime)o.Host).Kind = TimeKind.Local; });
            var wall = t.T.DateTime;
            return it.NewTime(new DateTimeOffset(wall, it.LocalOffsetForWall(wall)), TimeKind.Local);
        });
        Def(D, "step", (it, self, a, b) =>
        {
            if (b == null) return it.MakeEnum(self, "step", a, null);
            long by = a.Length > 1 ? it.Int(a[1]) : 1;
            if (by == 0) throw it.ArgError("step can't be 0");
            var cur = self;
            while (by > 0 ? it.Compare(cur, a[0]) <= 0 : it.Compare(cur, a[0]) >= 0)
            {
                it.CallProc(b, new[] { cur });
                cur = it.Invoke(cur, "+", new object[] { by }, null);
            }
            return self;
        });
        Def(D, "upto", (it, self, a, b) => b == null ? it.MakeEnum(self, "upto", a, null) : it.Invoke(self, "step", new[] { a[0], (object)1L }, b));
        Def(D, "downto", (it, self, a, b) => b == null ? it.MakeEnum(self, "downto", a, null) : it.Invoke(self, "step", new[] { a[0], (object)-1L }, b));
        Def(D, "beginning_of_month", (it, self, a, b) => it.DateLike(Cls(self), new DateTimeOffset(Me(self).T.Year, Me(self).T.Month, 1, 0, 0, 0, Me(self).T.Offset), Me(self).Dt));
        Def(D, "dup clone", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T, Me(self).Dt));

        // DateTime's time of day and offset.
        Def(DT, "hour", (it, self, a, b) => (long)Me(self).T.Hour);
        Def(DT, "min minute", (it, self, a, b) => (long)Me(self).T.Minute);
        Def(DT, "sec second", (it, self, a, b) => (long)Me(self).T.Second);
        Def(DT, "sec_fraction second_fraction", (it, self, a, b) => Me(self).T.Ticks % TimeSpan.TicksPerSecond / (double)TimeSpan.TicksPerSecond);
        Def(DT, "offset", (it, self, a, b) => Me(self).T.Offset.TotalDays);
        Def(DT, "zone", (it, self, a, b) => S(FormatOffset(Me(self).T.Offset, ":")));
        Def(DT, "new_offset", (it, self, a, b) => it.DateLike(Cls(self), Me(self).T.ToOffset(a.Length > 0 ? it.DtOffset(a[0]) : TimeSpan.Zero), true));
        Def(DT, "iso8601 xmlschema rfc3339 to_s", (it, self, a, b) =>
        {
            int n = a.Length > 0 ? (int)it.Int(a[0]) : 0;
            var t = Me(self).T;
            var frac = n > 0 ? "." + Frac(t.Ticks % TimeSpan.TicksPerSecond * 100, n) : "";
            return S(Strftime(t, null, "%Y-%m-%dT%H:%M:%S") + frac + FormatOffset(t.Offset, ":"));
        });
        Def(DT, "strftime", (it, self, a, b) => S(Strftime(Me(self).T, FormatOffset(Me(self).T.Offset, ":"), a.Length > 0 ? it.Str(a[0]) : "%FT%T%:z", true)));
    }

    bool IsSubclass(RClass c, RClass of)
    {
        for (var k = c; k != null; k = k.Super) if (k == of) return true;
        return false;
    }

    // DateTime offsets: a fraction of a day (Rational in Ruby; a Float here) or "+09:00".
    TimeSpan DtOffset(object o) => o switch
    {
        long l => TimeSpan.FromDays(l),
        double d => TimeSpan.FromMinutes(Math.Round(d * 24 * 60)),
        RString s => ParseOffset(s.V) ?? throw ArgError("invalid offset"),
        _ => throw ArgError("invalid offset"),
    };

    RObject DateFromParts(RClass cls, DateParts p, string str)
    {
        bool dt = IsSubclass(cls, DateTimeClass);
        if (p.Year == null && p.Mon == null && p.Mday == null && p.Yday == null && p.Wday == null && !(dt && p.Hour != null))
            throw Error(DateError(), "invalid date");
        var today = DateTime.UtcNow.Add(LocalOffset(DateTime.UtcNow));
        int y = p.Year ?? today.Year;
        int m = p.Mon ?? (p.Year == null && p.Mday != null ? today.Month : 1);
        int d = p.Mday ?? 1;
        if (p.Yday is int yd && p.Mon == null) { var t = new DateTime(y, 1, 1).AddDays(yd - 1); m = t.Month; d = t.Day; }
        if (p.Wday is int wd && p.Mon == null && p.Mday == null && p.Year == null)
        {
            // "Monday" alone: that weekday in the current week (Ruby counts weeks from Sunday).
            var t = today.Date.AddDays(wd - (int)today.DayOfWeek);
            y = t.Year; m = t.Month; d = t.Day;
        }
        var off = dt ? p.Offset ?? TimeSpan.Zero : TimeSpan.Zero;
        var v = CivilOrRaise(y, m, d, dt ? p.Hour ?? 0 : 0, dt ? p.Min ?? 0 : 0, dt ? p.Sec ?? 0 : 0, dt ? p.Nanos : 0, off);
        return DateLike(cls, v, dt);
    }
}

static class ObjectExtensions
{
    public static T Also<T>(this T o, Action<T> f) { f(o); return o; }
}
