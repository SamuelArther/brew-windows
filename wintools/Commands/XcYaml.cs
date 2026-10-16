using System.Globalization;
using System.Text;

namespace WinTools;

// A YAML reader for the config files of xcodegen (project.yml) and swiftgen (swiftgen.yml, `run yaml`):
// block mappings and sequences, flow collections, plain/quoted/block scalars, anchors, aliases and
// `<<` merge keys, comments and multiple documents. Maps are OrderedDictionary<string, object>,
// sequences List<object>; plain scalars resolve to null, bool, long, double or string (YAML 1.2 core).
static class XcYaml
{
    public static object Parse(string text) => ParseAll(text).FirstOrDefault();

    public static List<object> ParseAll(string text)
    {
        var docs = new List<object>();
        var current = new List<string>();
        void Flush()
        {
            if (current.Any(l => Content(l).Length > 0)) docs.Add(new Parser(current).Document());
            current = [];
        }
        foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').TrimStart('﻿').Split('\n'))
        {
            if (raw.StartsWith("---") && (raw.Length == 3 || raw[3] == ' ' || raw[3] == '\t'))
            {
                Flush();
                var rest = raw[3..].Trim();
                if (rest.Length > 0) current.Add(rest);
                continue;
            }
            if (raw == "..." || raw.StartsWith("... ")) { Flush(); continue; }
            if (raw.StartsWith('%') && current.Count == 0) continue;   // %YAML / %TAG directives
            current.Add(raw);
        }
        Flush();
        return docs;
    }

    // The line without its comment, trimmed at the end.
    static string Content(string line) => StripComment(line).TrimEnd();

    static string StripComment(string line)
    {
        char quote = '\0';
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quote != '\0')
            {
                if (c == quote)
                {
                    if (quote == '\'' && i + 1 < line.Length && line[i + 1] == '\'') { i++; continue; }
                    quote = '\0';
                }
                else if (c == '\\' && quote == '"') i++;
                continue;
            }
            if ((c == '"' || c == '\'') && (i == 0 || " \t[{,:-?".Contains(line[i - 1]))) quote = c;
            else if (c == '#' && (i == 0 || line[i - 1] == ' ' || line[i - 1] == '\t')) return line[..i];
        }
        return line;
    }

    static int Indent(string line)
    {
        var n = 0;
        while (n < line.Length && line[n] == ' ') n++;
        return n;
    }

    sealed class Parser(List<string> source)
    {
        readonly List<string> lines = source;
        readonly Dictionary<string, object> anchors = [];
        int li;

        public object Document() => Block(0) ;

        Exception Error(string what) => new InvalidDataException($"YAML line {li + 1}: {what}");

        // Moves to the next line with content; false at the end.
        bool NextContent()
        {
            while (li < lines.Count && Content(lines[li]).Trim().Length == 0) li++;
            return li < lines.Count;
        }

        // A node whose first line is indented at least `min`.
        object Block(int min)
        {
            if (!NextContent()) return null;
            var line = lines[li];
            var indent = Indent(line);
            if (indent < min) return null;
            var content = Content(line)[indent..];
            if (content == "-" || content.StartsWith("- ") || content.StartsWith("-\t")) return Sequence(indent);
            if (MappingKeyEnd(content) >= 0) return Mapping(indent);
            // A lone scalar or flow collection, possibly with properties.
            li++;
            return InlineValue(content, indent);
        }

        object Sequence(int indent)
        {
            var list = new List<object>();
            while (NextContent())
            {
                var line = lines[li];
                if (Indent(line) != indent) break;
                var content = Content(line)[indent..];
                if (!(content == "-" || content.StartsWith("- ") || content.StartsWith("-\t"))) break;
                var rest = content[1..];
                var pad = rest.Length - rest.TrimStart().Length;
                rest = rest.TrimStart();
                if (rest.Length == 0)
                {
                    li++;
                    list.Add(Block(indent + 1));
                    continue;
                }
                var (anchor, value) = Properties(rest);
                if (value.Length == 0)
                {
                    li++;
                    list.Add(Anchor(anchor, Block(indent + 1)));
                    continue;
                }
                if (value.StartsWith("- ") || value == "-" || MappingKeyEnd(value) >= 0)
                {
                    // "- key: value" or "- - x": the rest of the line becomes a node indented past the dash.
                    var column = indent + 1 + pad + (rest.Length - value.Length);
                    lines[li] = new string(' ', column) + value;
                    list.Add(Anchor(anchor, Block(column)));
                    continue;
                }
                li++;
                list.Add(Anchor(anchor, InlineValue(value, indent)));
            }
            return list;
        }

        object Mapping(int indent)
        {
            var map = new OrderedDictionary<string, object>();
            while (NextContent())
            {
                var line = lines[li];
                if (Indent(line) != indent) break;
                var content = Content(line)[indent..];
                var end = MappingKeyEnd(content);
                if (end < 0) break;
                var key = KeyText(content[..end].Trim());
                var rest = content[(end + 1)..].Trim();
                var (anchor, value) = Properties(rest);
                object result;
                if (value.Length == 0)
                {
                    li++;
                    // "key:" followed by a block (or a sequence at the same indentation).
                    if (NextContent() && Indent(lines[li]) == indent && Content(lines[li])[indent..] is var next
                        && (next == "-" || next.StartsWith("- ")))
                        result = Sequence(indent);
                    else
                        result = Block(indent + 1);
                }
                else
                {
                    li++;
                    result = InlineValue(value, indent);
                }
                result = Anchor(anchor, result);
                if (key == "<<") Merge(map, result);
                else map[key] = result;
            }
            return map;
        }

        static void Merge(OrderedDictionary<string, object> map, object source)
        {
            if (source is List<object> list) { foreach (var item in list) Merge(map, item); return; }
            if (source is OrderedDictionary<string, object> dict)
                foreach (var (k, v) in dict) map.TryAdd(k, v);
        }

        object Anchor(string anchor, object value)
        {
            if (anchor != null) anchors[anchor] = value;
            return value;
        }

        // Splits "&anchor !tag value" into the anchor name and the value text.
        static (string Anchor, string Value) Properties(string text)
        {
            string anchor = null;
            while (text.Length > 0 && (text[0] == '&' || text[0] == '!'))
            {
                var space = text.IndexOfAny([' ', '\t']);
                var token = space < 0 ? text : text[..space];
                if (token[0] == '&') anchor = token[1..];
                text = space < 0 ? "" : text[(space + 1)..].TrimStart();
            }
            return (anchor, text);
        }

        // A value that starts on the current line (already consumed): scalars, flow collections,
        // block scalars, aliases, and plain or quoted scalars continued on more-indented lines.
        object InlineValue(string text, int parentIndent)
        {
            if (text.StartsWith('*')) return anchors.TryGetValue(text[1..].Trim(), out var aliased) ? aliased : throw Error($"unknown alias {text}");
            if (text[0] == '|' || text[0] == '>') return BlockScalar(text, parentIndent);
            if (text[0] == '[' || text[0] == '{')
            {
                var sb = new StringBuilder(text);
                while (!Balanced(sb.ToString()) && li < lines.Count) sb.Append(' ').Append(Content(lines[li++]).Trim());
                var flow = new Flow(sb.ToString(), anchors);
                return flow.Value();
            }
            if (text[0] == '"' || text[0] == '\'')
            {
                var sb = new StringBuilder(text);
                while (!QuoteClosed(sb.ToString()) && li < lines.Count) sb.Append('\n').Append(lines[li++].Trim());
                var full = sb.ToString();
                return Quoted(full, out _);
            }
            // Plain scalar, folded over continuation lines that are more indented.
            var plain = new StringBuilder(text.Trim());
            while (li < lines.Count)
            {
                var next = lines[li];
                var content = Content(next);
                if (content.Trim().Length == 0) break;
                if (Indent(next) <= parentIndent) break;
                var trimmed = content.Trim();
                if (MappingKeyEnd(trimmed) >= 0 || trimmed.StartsWith("- ")) break;
                plain.Append(' ').Append(trimmed);
                li++;
            }
            return Resolve(plain.ToString());
        }

        object BlockScalar(string header, int parentIndent)
        {
            var literal = header[0] == '|';
            var chomp = header.Contains('-') ? '-' : header.Contains('+') ? '+' : ' ';
            var explicitIndent = header.FirstOrDefault(char.IsAsciiDigit);
            var collected = new List<string>();
            int blockIndent = explicitIndent != '\0' ? parentIndent + (explicitIndent - '0') : -1;
            while (li < lines.Count)
            {
                var line = lines[li];
                if (line.Trim().Length == 0) { collected.Add(""); li++; continue; }
                var indent = Indent(line);
                if (blockIndent < 0) blockIndent = indent;
                if (indent < blockIndent || indent <= parentIndent) break;
                collected.Add(line[blockIndent..]);
                li++;
            }
            var trailing = 0;
            while (collected.Count > 0 && collected[^1].Length == 0) { collected.RemoveAt(collected.Count - 1); trailing++; }
            string body;
            if (literal) body = string.Join("\n", collected);
            else
            {
                var sb = new StringBuilder();
                for (var i = 0; i < collected.Count; i++)
                {
                    if (i > 0)
                    {
                        if (collected[i].Length == 0 || collected[i - 1].Length == 0 || collected[i].StartsWith(' ')) sb.Append('\n');
                        else sb.Append(' ');
                    }
                    sb.Append(collected[i]);
                }
                body = sb.ToString().Replace("\n\n", "\n");
            }
            if (collected.Count == 0) return "";
            return chomp switch
            {
                '-' => body,
                '+' => body + new string('\n', trailing + 1),
                _ => body + "\n",
            };
        }
    }

    // Index of the ':' that ends a mapping key on this line, or -1 when the line is not "key: value".
    static int MappingKeyEnd(string content)
    {
        if (content.Length == 0 || content[0] == '[' || content[0] == '{' || content[0] == '|' || content[0] == '>' || content[0] == '*') return -1;
        if (content.StartsWith("- ")) return -1;
        var i = 0;
        if (content[0] == '"' || content[0] == '\'')
        {
            Quoted(content, out var length);
            i = length;
            while (i < content.Length && content[i] == ' ') i++;
            return i < content.Length && content[i] == ':' && (i + 1 == content.Length || content[i + 1] is ' ' or '\t') ? i : -1;
        }
        for (; i < content.Length; i++)
        {
            if (content[i] == ':' && (i + 1 == content.Length || content[i + 1] is ' ' or '\t')) return i;
        }
        return -1;
    }

    static string KeyText(string key) =>
        key.Length > 0 && (key[0] == '"' || key[0] == '\'') ? Quoted(key, out _) : key;

    static bool QuoteClosed(string text)
    {
        try { Quoted(text, out _); return true; }
        catch (InvalidDataException) { return false; }
    }

    // A quoted scalar at the start of `text`; `length` is how many characters it used.
    static string Quoted(string text, out int length)
    {
        var quote = text[0];
        var sb = new StringBuilder();
        for (var i = 1; i < text.Length; i++)
        {
            var c = text[i];
            if (quote == '\'')
            {
                if (c == '\'')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\'') { sb.Append('\''); i++; continue; }
                    length = i + 1;
                    return Fold(sb.ToString());
                }
                sb.Append(c);
                continue;
            }
            if (c == '"') { length = i + 1; return Fold(sb.ToString()); }
            if (c != '\\') { sb.Append(c); continue; }
            if (++i >= text.Length) break;
            c = text[i];
            switch (c)
            {
                case 'n': sb.Append('\n'); break;
                case 't': sb.Append('\t'); break;
                case 'r': sb.Append('\r'); break;
                case '0': sb.Append('\0'); break;
                case 'e': sb.Append('\x1b'); break;
                case 'x': sb.Append((char)Convert.ToInt32(text.Substring(i + 1, 2), 16)); i += 2; break;
                case 'u': sb.Append((char)Convert.ToInt32(text.Substring(i + 1, 4), 16)); i += 4; break;
                case 'U': sb.Append(char.ConvertFromUtf32(Convert.ToInt32(text.Substring(i + 1, 8), 16))); i += 8; break;
                case '\n': while (i + 1 < text.Length && text[i + 1] == ' ') i++; break;
                default: sb.Append(c); break;
            }
        }
        throw new InvalidDataException("unterminated quoted string");

        // Line breaks inside quoted scalars fold to spaces (empty lines to newlines).
        static string Fold(string s) => s.Contains('\n') ? string.Join(" ", s.Split('\n').Select(l => l.Trim())).Replace("  ", "\n") : s;
    }

    static bool Balanced(string text)
    {
        var depth = 0;
        char quote = '\0';
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quote != '\0') { if (c == quote) quote = '\0'; else if (c == '\\' && quote == '"') i++; continue; }
            if (c is '"' or '\'') quote = c;
            else if (c is '[' or '{') depth++;
            else if (c is ']' or '}') depth--;
        }
        return depth <= 0;
    }

    // Plain scalars: null, booleans, integers and floats per the YAML 1.2 core schema.
    public static object Resolve(string s)
    {
        switch (s)
        {
            case "" or "~" or "null" or "Null" or "NULL": return null;
            case "true" or "True" or "TRUE": return true;
            case "false" or "False" or "FALSE": return false;
            case ".inf" or ".Inf" or ".INF" or "+.inf": return double.PositiveInfinity;
            case "-.inf" or "-.Inf" or "-.INF": return double.NegativeInfinity;
            case ".nan" or ".NaN" or ".NAN": return double.NaN;
        }
        if (s.StartsWith("0x") && long.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex)) return hex;
        if (s.StartsWith("0o") && s.Length > 2 && s[2..].All(c => c is >= '0' and <= '7')) return Convert.ToInt64(s[2..], 8);
        var body = s.TrimStart('-', '+');
        if (body.Length > 0 && body.All(char.IsAsciiDigit) && long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l)) return l;
        if (body.Length > 0 && char.IsAsciiDigit(body[0]) || body.StartsWith('.') && body.Length > 1 && char.IsAsciiDigit(body[1]))
            if (body.All(c => char.IsAsciiDigit(c) || c is '.' or 'e' or 'E' or '-' or '+') && body.Count(c => c == '.') <= 1
                && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
        return s;
    }

    sealed class Flow(string text, Dictionary<string, object> anchors)
    {
        int pos;

        void Space() { while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++; }

        public object Value()
        {
            Space();
            if (pos >= text.Length) return null;
            var c = text[pos];
            if (c == '[')
            {
                pos++;
                var list = new List<object>();
                while (true)
                {
                    Space();
                    if (pos >= text.Length) throw new InvalidDataException("unterminated flow sequence");
                    if (text[pos] == ']') { pos++; return list; }
                    var item = Value();
                    Space();
                    // [a: b] is a single-pair mapping inside a sequence.
                    if (pos < text.Length && text[pos] == ':')
                    {
                        pos++;
                        var pair = new OrderedDictionary<string, object> { [XcPlist.Scalar(item) ?? ""] = Value() };
                        item = pair;
                        Space();
                    }
                    list.Add(item);
                    if (pos < text.Length && text[pos] == ',') pos++;
                }
            }
            if (c == '{')
            {
                pos++;
                var map = new OrderedDictionary<string, object>();
                while (true)
                {
                    Space();
                    if (pos >= text.Length) throw new InvalidDataException("unterminated flow mapping");
                    if (text[pos] == '}') { pos++; return map; }
                    var key = XcPlist.Scalar(Value()) ?? "";
                    Space();
                    object value = null;
                    if (pos < text.Length && text[pos] == ':') { pos++; value = Value(); }
                    if (key == "<<" && value is OrderedDictionary<string, object> merged) { foreach (var (k, v) in merged) map.TryAdd(k, v); }
                    else map[key] = value;
                    Space();
                    if (pos < text.Length && text[pos] == ',') pos++;
                }
            }
            if (c == '"' || c == '\'')
            {
                var s = Quoted(text[pos..], out var length);
                pos += length;
                return s;
            }
            if (c == '*')
            {
                var start = ++pos;
                while (pos < text.Length && !(text[pos] is ',' or ']' or '}' or ' ')) pos++;
                return anchors.TryGetValue(text[start..pos], out var v) ? v : null;
            }
            {
                var start = pos;
                while (pos < text.Length)
                {
                    var ch = text[pos];
                    if (ch is ',' or ']' or '}') break;
                    if (ch == ':' && (pos + 1 == text.Length || text[pos + 1] is ' ' or ',' or ']' or '}')) break;
                    pos++;
                }
                return Resolve(text[start..pos].Trim());
            }
        }
    }

    // ---- writing (xcodegen dump) ----

    public static string Dump(object value)
    {
        var sb = new StringBuilder();
        Write(sb, value, 0, false);
        return sb.ToString();
    }

    static void Write(StringBuilder sb, object value, int indent, bool inline)
    {
        var pad = new string(' ', indent);
        switch (value)
        {
            case OrderedDictionary<string, object> map when map.Count > 0:
            {
                var first = true;
                foreach (var (k, v) in map)
                {
                    if (!(inline && first)) sb.Append(pad);
                    first = false;
                    sb.Append(ScalarText(k)).Append(':');
                    if (v is OrderedDictionary<string, object> { Count: > 0 }) { sb.Append('\n'); Write(sb, v, indent + 2, false); }
                    else if (v is List<object> { Count: > 0 }) { sb.Append('\n'); Write(sb, v, indent, false); }
                    else { sb.Append(' '); Write(sb, v, indent + 2, true); }
                }
                return;
            }
            case List<object> list when list.Count > 0:
                foreach (var item in list)
                {
                    sb.Append(pad).Append("- ");
                    if (item is OrderedDictionary<string, object> { Count: > 0 } or List<object> { Count: > 0 })
                    {
                        if (item is List<object>) { sb.Append('\n'); Write(sb, item, indent + 2, false); }
                        else Write(sb, item, indent + 2, true);
                    }
                    else Write(sb, item, indent + 2, true);
                }
                return;
            case OrderedDictionary<string, object>: sb.Append("{}\n"); return;
            case List<object>: sb.Append("[]\n"); return;
            default: sb.Append(ScalarText(value)).Append('\n'); return;
        }
    }

    static string ScalarText(object value)
    {
        switch (value)
        {
            case null: return "null";
            case bool b: return b ? "true" : "false";
            case string s:
                var needsQuotes = s.Length == 0 || Resolve(s) is not string || s.Trim() != s || s.IndexOfAny([':', '#', '\n', '"', '\'', '{', '}', '[', ']', ',', '&', '*', '!', '|', '>', '%', '@', '`']) >= 0
                    || s.StartsWith('-') || s.StartsWith('?');
                return needsQuotes ? "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"" : s;
            default: return XcPlist.Scalar(value);
        }
    }
}
