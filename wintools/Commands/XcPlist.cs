using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace WinTools;

// Property lists for the Xcode tools (xcodegen, xcdiff, xcp, rswift, swiftgen, xcode-build-server):
// reads OpenStep/ASCII plists (project.pbxproj, .strings), XML plists and binary plists, and writes
// XML plists. Values are string, long, double, bool, DateTime, byte[], List<object> and
// OrderedDictionary<string, object> (key order kept, as Xcode and the generators rely on it).
static class XcPlist
{
    // Reads any of the three formats, chosen by the file's first bytes.
    public static object Load(string path) => Parse(File.ReadAllBytes(path), path);

    public static object Parse(byte[] data, string name = "plist")
    {
        if (data.Length >= 8 && Encoding.ASCII.GetString(data, 0, 8) == "bplist00") return ParseBinary(data);
        var text = Decode(data);
        var trimmed = text.TrimStart();
        if (trimmed.StartsWith("<?xml") || trimmed.StartsWith("<plist") || trimmed.StartsWith("<!DOCTYPE"))
            return ParseXml(text);
        return ParseAscii(text, name);
    }

    // Text with any byte order mark (UTF-8, UTF-16 LE/BE), defaulting to UTF-8.
    public static string Decode(byte[] data)
    {
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE) return Encoding.Unicode.GetString(data, 2, data.Length - 2);
        if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) return Encoding.UTF8.GetString(data, 3, data.Length - 3);
        return Encoding.UTF8.GetString(data);
    }

    // ---- OpenStep / ASCII ----

    // A .strings file is a dictionary without the outer braces; both forms are accepted.
    public static object ParseAscii(string text, string name = "plist")
    {
        var p = new AsciiParser(text, name);
        p.SkipSpace();
        if (p.AtEnd) return new OrderedDictionary<string, object>();
        if (p.Peek == '{' || p.Peek == '(' || p.Peek == '<') { var value = p.Value(); p.SkipSpace(); if (p.AtEnd) return value; }
        return p.TopLevelDictionary();
    }

    // "key" = "value"; pairs of a .strings file, in file order.
    public static OrderedDictionary<string, object> ParseStrings(string text, string name = "strings")
    {
        var p = new AsciiParser(text, name);
        return p.TopLevelDictionary();
    }

    sealed class AsciiParser(string text, string name)
    {
        int pos;
        public bool AtEnd => pos >= text.Length;
        public char Peek => text[pos];

        Exception Error(string what)
        {
            var line = 1 + text.AsSpan(0, Math.Min(pos, text.Length)).Count('\n');
            return new InvalidDataException($"{name}:{line}: {what}");
        }

        public void SkipSpace()
        {
            while (pos < text.Length)
            {
                var c = text[pos];
                if (char.IsWhiteSpace(c) || c == '﻿') { pos++; continue; }
                if (c == '/' && pos + 1 < text.Length && text[pos + 1] == '/')
                {
                    while (pos < text.Length && text[pos] != '\n') pos++;
                    continue;
                }
                if (c == '/' && pos + 1 < text.Length && text[pos + 1] == '*')
                {
                    var end = text.IndexOf("*/", pos + 2, StringComparison.Ordinal);
                    pos = end < 0 ? text.Length : end + 2;
                    continue;
                }
                break;
            }
        }

        void Expect(char c)
        {
            SkipSpace();
            if (AtEnd || text[pos] != c) throw Error($"expected '{c}'");
            pos++;
        }

        public OrderedDictionary<string, object> TopLevelDictionary()
        {
            var dict = new OrderedDictionary<string, object>();
            while (true)
            {
                SkipSpace();
                if (AtEnd) return dict;
                var key = String();
                SkipSpace();
                if (!AtEnd && text[pos] == ';') { pos++; dict[key] = key; continue; }   // "key"; means "key" = "key";
                Expect('=');
                SkipSpace();
                dict[key] = Value();
                Expect(';');
            }
        }

        public object Value()
        {
            SkipSpace();
            if (AtEnd) throw Error("unexpected end of file");
            switch (text[pos])
            {
                case '{':
                {
                    pos++;
                    var dict = new OrderedDictionary<string, object>();
                    while (true)
                    {
                        SkipSpace();
                        if (AtEnd) throw Error("unterminated dictionary");
                        if (text[pos] == '}') { pos++; return dict; }
                        var key = String();
                        Expect('=');
                        dict[key] = Value();
                        Expect(';');
                    }
                }
                case '(':
                {
                    pos++;
                    var list = new List<object>();
                    while (true)
                    {
                        SkipSpace();
                        if (AtEnd) throw Error("unterminated array");
                        if (text[pos] == ')') { pos++; return list; }
                        list.Add(Value());
                        SkipSpace();
                        if (!AtEnd && text[pos] == ',') pos++;
                    }
                }
                case '<':
                {
                    var end = text.IndexOf('>', pos);
                    if (end < 0) throw Error("unterminated data");
                    var hex = new string(text[(pos + 1)..end].Where(Uri.IsHexDigit).ToArray());
                    pos = end + 1;
                    return Convert.FromHexString(hex);
                }
                default:
                    return String();
            }
        }

        string String()
        {
            SkipSpace();
            if (AtEnd) throw Error("expected a string");
            var c = text[pos];
            if (c == '"' || c == '\'')
            {
                var quote = c;
                pos++;
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw Error("unterminated string");
                    c = text[pos++];
                    if (c == quote) return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error("unterminated string");
                    c = text[pos++];
                    switch (c)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'a': sb.Append('\a'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'v': sb.Append('\v'); break;
                        case 'U' or 'u':
                        {
                            var n = 0;
                            var digits = 0;
                            while (digits < 4 && !AtEnd && Uri.IsHexDigit(text[pos])) { n = n * 16 + Convert.ToInt32(text[pos].ToString(), 16); pos++; digits++; }
                            sb.Append((char)n);
                            break;
                        }
                        case >= '0' and <= '7':
                        {
                            var n = c - '0';
                            for (var i = 0; i < 2 && !AtEnd && text[pos] is >= '0' and <= '7'; i++) n = n * 8 + (text[pos++] - '0');
                            sb.Append((char)n);
                            break;
                        }
                        default: sb.Append(c); break;
                    }
                }
            }
            var start = pos;
            while (pos < text.Length && IsUnquoted(text[pos])) pos++;
            if (pos == start) throw Error($"unexpected character '{c}'");
            return text[start..pos];
        }
    }

    public static bool IsUnquoted(char c) =>
        char.IsAsciiLetterOrDigit(c) || c is '_' or '$' or '/' or ':' or '.' or '-' or '+';

    // ---- XML ----

    public static object ParseXml(string text)
    {
        var doc = XDocument.Parse(text);
        var root = doc.Root ?? throw new InvalidDataException("empty plist");
        if (root.Name.LocalName == "plist") root = root.Elements().FirstOrDefault() ?? throw new InvalidDataException("empty plist");
        return FromXml(root);
    }

    static object FromXml(XElement e)
    {
        switch (e.Name.LocalName)
        {
            case "dict":
            {
                var dict = new OrderedDictionary<string, object>();
                string key = null;
                foreach (var child in e.Elements())
                {
                    if (child.Name.LocalName == "key") key = child.Value;
                    else if (key != null) { dict[key] = FromXml(child); key = null; }
                }
                return dict;
            }
            case "array": return e.Elements().Select(FromXml).ToList();
            case "string": return e.Value;
            case "integer": return long.TryParse(e.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : 0L;
            case "real": return double.TryParse(e.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0.0;
            case "true": return true;
            case "false": return false;
            case "date": return DateTime.Parse(e.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            case "data": return Convert.FromBase64String(new string(e.Value.Where(c => !char.IsWhiteSpace(c)).ToArray()));
            default: return e.Value;
        }
    }

    public static string ToXml(object value)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n");
        sb.Append("<plist version=\"1.0\">\n");
        WriteXml(sb, value, 0);
        sb.Append("</plist>\n");
        return sb.ToString();
    }

    static void WriteXml(StringBuilder sb, object value, int depth)
    {
        var indent = new string('\t', depth);
        switch (value)
        {
            case OrderedDictionary<string, object> dict:
                if (dict.Count == 0) { sb.Append(indent).Append("<dict/>\n"); return; }
                sb.Append(indent).Append("<dict>\n");
                foreach (var (k, v) in dict)
                {
                    sb.Append(indent).Append("\t<key>").Append(Escape(k)).Append("</key>\n");
                    WriteXml(sb, v, depth + 1);
                }
                sb.Append(indent).Append("</dict>\n");
                return;
            case List<object> list:
                if (list.Count == 0) { sb.Append(indent).Append("<array/>\n"); return; }
                sb.Append(indent).Append("<array>\n");
                foreach (var v in list) WriteXml(sb, v, depth + 1);
                sb.Append(indent).Append("</array>\n");
                return;
            case bool b: sb.Append(indent).Append(b ? "<true/>\n" : "<false/>\n"); return;
            case long or int: sb.Append(indent).Append("<integer>").Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append("</integer>\n"); return;
            case double d: sb.Append(indent).Append("<real>").Append(d.ToString("R", CultureInfo.InvariantCulture)).Append("</real>\n"); return;
            case DateTime t: sb.Append(indent).Append("<date>").Append(t.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)).Append("</date>\n"); return;
            case byte[] data: sb.Append(indent).Append("<data>").Append(Convert.ToBase64String(data)).Append("</data>\n"); return;
            case null: sb.Append(indent).Append("<string></string>\n"); return;
            default: sb.Append(indent).Append("<string>").Append(Escape(value.ToString())).Append("</string>\n"); return;
        }
    }

    static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // ---- binary (bplist00) ----

    public static object ParseBinary(byte[] data)
    {
        if (data.Length < 40) throw new InvalidDataException("truncated binary plist");
        var trailer = data.AsSpan(data.Length - 32);
        int offsetSize = trailer[6], refSize = trailer[7];
        var count = (long)BinaryPrimitives.ReadUInt64BigEndian(trailer[8..]);
        var top = (long)BinaryPrimitives.ReadUInt64BigEndian(trailer[16..]);
        var table = (long)BinaryPrimitives.ReadUInt64BigEndian(trailer[24..]);
        long ReadInt(long at, int size)
        {
            long v = 0;
            for (var i = 0; i < size; i++) v = (v << 8) | data[at + i];
            return v;
        }
        long Offset(long index) => ReadInt(table + index * offsetSize, offsetSize);
        object Read(long index, int depth)
        {
            if (index < 0 || index >= count || depth > 512) throw new InvalidDataException("bad binary plist reference");
            var at = Offset(index);
            var marker = data[at];
            int high = marker >> 4, low = marker & 0xF;
            long Length(ref long p)
            {
                if (low != 0xF) return low;
                var intMarker = data[p];
                var size = 1 << (intMarker & 0xF);
                var len = ReadInt(p + 1, size);
                p += 1 + size;
                return len;
            }
            var pos = at + 1;
            switch (high)
            {
                case 0x0: return low == 0x9 ? true : low == 0x8 ? false : null;
                case 0x1: return ReadInt(pos, 1 << low);
                case 0x2:
                    return low == 2 ? BinaryPrimitives.ReadSingleBigEndian(data.AsSpan((int)pos)) : BinaryPrimitives.ReadDoubleBigEndian(data.AsSpan((int)pos));
                case 0x3: return new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(BinaryPrimitives.ReadDoubleBigEndian(data.AsSpan((int)pos)));
                case 0x4: { var len = Length(ref pos); return data.AsSpan((int)pos, (int)len).ToArray(); }
                case 0x5: { var len = Length(ref pos); return Encoding.ASCII.GetString(data, (int)pos, (int)len); }
                case 0x6: { var len = Length(ref pos); return Encoding.BigEndianUnicode.GetString(data, (int)pos, (int)len * 2); }
                case 0x7: { var len = Length(ref pos); return Encoding.UTF8.GetString(data, (int)pos, (int)len); }
                case 0x8: return ReadInt(pos, low + 1);
                case 0xA or 0xC:
                {
                    var len = Length(ref pos);
                    var list = new List<object>();
                    for (var i = 0; i < len; i++) list.Add(Read(ReadInt(pos + i * refSize, refSize), depth + 1));
                    return list;
                }
                case 0xD:
                {
                    var len = Length(ref pos);
                    var dict = new OrderedDictionary<string, object>();
                    for (var i = 0; i < len; i++)
                    {
                        var key = Read(ReadInt(pos + i * refSize, refSize), depth + 1)?.ToString() ?? "";
                        dict[key] = Read(ReadInt(pos + (len + i) * refSize, refSize), depth + 1);
                    }
                    return dict;
                }
                default: throw new InvalidDataException($"unknown binary plist object 0x{marker:x2}");
            }
        }
        return Read(top, 0);
    }

    // ---- helpers shared by the tools ----

    public static OrderedDictionary<string, object> Dict(object o) => o as OrderedDictionary<string, object>;
    public static List<object> List(object o) => o as List<object>;

    public static string Str(OrderedDictionary<string, object> d, string key) =>
        d != null && d.TryGetValue(key, out var v) && v != null ? Scalar(v) : null;

    public static OrderedDictionary<string, object> Dict(OrderedDictionary<string, object> d, string key) =>
        d != null && d.TryGetValue(key, out var v) ? v as OrderedDictionary<string, object> : null;

    public static List<object> List(OrderedDictionary<string, object> d, string key) =>
        d != null && d.TryGetValue(key, out var v) ? v as List<object> : null;

    public static string Scalar(object v) => v switch
    {
        null => null,
        string s => s,
        bool b => b ? "YES" : "NO",
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => v.ToString(),
    };
}

// Output for the Xcode tools: UTF-8 when redirected (their reports carry ✅/❌ and source text), the
// console's own writer otherwise.
static class XcOut
{
    public static void Write(string text) => Emit(text, Console.IsOutputRedirected, Console.OpenStandardOutput, Console.Out);

    public static void Error(string text) => Emit(text, Console.IsErrorRedirected, Console.OpenStandardError, Console.Error);

    static void Emit(string text, bool redirected, Func<Stream> open, TextWriter console)
    {
        if (!redirected) { console.Write(text); return; }
        console.Flush();
        var bytes = new UTF8Encoding(false).GetBytes(text);
        using var stream = open();
        stream.Write(bytes);
    }
}
