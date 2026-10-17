using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

// YAML / Psych (Ruby 3.4, Psych 5): a YAML 1.1 parser with Psych's scalar resolution and safe_load
// rules, and an emitter that writes what Psych writes for the plain Ruby values.
sealed class YamlParser
{
    readonly Interp it;
    readonly string s;
    readonly string file;
    int p;
    readonly Dictionary<string, object> anchors = new();
    public bool Aliases, Unsafe;
    public HashSet<string> Permitted = new();
    public HashSet<string> PermittedSymbols = new();

    public YamlParser(Interp it, string src, string file)
    {
        this.it = it;
        if (src.Length > 0 && src[0] == '\uFEFF') src = src[1..];
        s = src.Replace("\r\n", "\n").Replace('\r', '\n');
        this.file = file ?? "<unknown>";
    }

    // ---- errors ----

    RubyError Syntax(string msg, int at = -1)
    {
        if (at < 0) at = p;
        at = Math.Min(at, s.Length);
        int line = 1, col = 1;
        for (int i = 0; i < at; i++) { if (s[i] == '\n') { line++; col = 1; } else col++; }
        var e = it.NewException(it.PsychClass("SyntaxError"), $"({file}): {msg} at line {line} column {col}");
        e.SetIvar("@file", file == "<unknown>" ? null : Interp.S(file));
        e.SetIvar("@line", (long)line);
        e.SetIvar("@column", (long)col);
        e.SetIvar("@problem", Interp.S(msg));
        return new RubyError(e);
    }

    RubyError Disallowed(string cls) => it.Error(it.PsychClass("DisallowedClass"), "Tried to load unspecified class: " + cls);

    void Check(string cls)
    {
        if (!Unsafe && !Permitted.Contains(cls)) throw Disallowed(cls);
    }

    // ---- character helpers ----

    char C => p < s.Length ? s[p] : '\0';
    char At(int i) => i >= 0 && i < s.Length ? s[i] : '\0';
    static bool Blank(char c) => c is ' ' or '\t';
    static bool WsOrEnd(char c) => c is ' ' or '\t' or '\n' or '\0';
    static bool FlowInd(char c) => c is ',' or '[' or ']' or '{' or '}';
    int Col(int i)
    {
        int ls = i > 0 ? s.LastIndexOf('\n', i - 1) + 1 : 0;
        return i - ls;
    }
    bool DocMarker(int i, string m) => Col(i) == 0 && string.CompareOrdinal(s, i, m, 0, 3) == 0 && WsOrEnd(At(i + 3));
    bool AtDocBoundary(int i) => DocMarker(i, "---") || DocMarker(i, "...");

    void SkipBlanks() { while (Blank(C)) p++; }

    // Past blanks, comments and line breaks; true when a line break was crossed.
    bool SkipToToken()
    {
        bool crossed = false;
        while (true)
        {
            SkipBlanks();
            if (C == '#' && (p == 0 || WsOrEnd(At(p - 1)))) { while (C != '\n' && C != '\0') p++; }
            if (C == '\n') { p++; crossed = true; continue; }
            return crossed;
        }
    }

    // ---- documents ----

    public List<object> ParseStream()
    {
        var docs = new List<object>();
        while (true)
        {
            SkipToToken();
            while (C == '%' && Col(p) == 0)
            {
                while (C != '\n' && C != '\0') p++;
                SkipToToken();
            }
            if (C == '\0') break;
            if (DocMarker(p, "...")) { p += 3; continue; }
            bool explicitStart = false;
            if (DocMarker(p, "---")) { p += 3; explicitStart = true; }
            var v = ParseNode(-1, false, !explicitStart);
            docs.Add(v);
            SkipToToken();
            if (DocMarker(p, "...")) { p += 3; continue; }
            if (C == '\0') break;
            if (DocMarker(p, "---")) continue;
            throw Syntax("did not find expected <document start>");
        }
        return docs;
    }

    // ---- block nodes ----

    // A node whose content must be indented past `indent`; `seqOk` lets a block sequence sit at
    // `indent` itself (a mapping value: "key:\n- a").
    object ParseNode(int indent, bool seqOk, bool firstInDoc = false)
    {
        bool crossed = SkipToToken() || firstInDoc;
        if (C == '\0' || AtDocBoundary(p)) return null;
        if (crossed && !Fits(indent, seqOk)) return null;

        string anchor = null, tag = null;
        while (true)
        {
            if (C == '&') { p++; int st = p; while (!WsOrEnd(C) && !FlowInd(C)) p++; anchor = s[st..p]; SkipBlanks(); continue; }
            if (C == '!') { int st = p; while (!WsOrEnd(C)) p++; tag = s[st..p]; SkipBlanks(); continue; }
            break;
        }
        // Properties alone on their line belong to the node on the next lines ("&a\nkey: v" anchors
        // the mapping); on the same line as an implicit key they belong to that key.
        bool ownLine = false;
        if ((anchor != null || tag != null) && (C == '\n' || C == '#' || C == '\0'))
        {
            ownLine = true;
            SkipToToken();
            if (C == '\0' || AtDocBoundary(p) || !Fits(indent, seqOk))
                return Finish(anchor, null, ResolveScalar("", false, tag));
        }

        object v;
        char c = C;
        if (c == '-' && WsOrEnd(At(p + 1)))
        {
            if (anchor != null) anchors[anchor] = null;
            v = ParseBlockSeq(Col(p));
        }
        else if (c == '?' && WsOrEnd(At(p + 1))) v = ParseBlockMapping(Col(p), null, false, true);
        else if (c is '|' or '>')
        {
            var text = ParseBlockScalar(indent);
            v = ResolveScalar(text, true, tag);
            tag = null;
        }
        else
        {
            int keyStart = p;
            var (key, isKeyLike) = ReadInlineNode(indent, tag);
            SkipBlanks();
            if (C == ':' && WsOrEnd(At(p + 1)))
            {
                if (!isKeyLike) throw Syntax("mapping keys are not allowed in this context", keyStart);
                if (seqOk && !crossed && !ownLine) throw Syntax("mapping values are not allowed in this context", p);
                if (!ownLine)
                {
                    if (anchor != null) anchors[anchor] = key;
                    anchor = null;
                    tag = null;
                }
                v = ParseBlockMapping(Col(keyStart), key, true, false);
            }
            else
            {
                v = key;
                tag = null;   // ReadInlineNode applied it
            }
        }
        return Finish(anchor, tag, v);
    }

    // The next token starts a node indented past `indent` (or a sequence at it, for a mapping value).
    bool Fits(int indent, bool seqOk)
    {
        int col = Col(p);
        return col > indent || (seqOk && col == indent && C == '-' && WsOrEnd(At(p + 1)));
    }

    object Finish(string anchor, string tag, object v)
    {
        if (tag != null) v = ApplyCollectionTag(tag, v);
        if (anchor != null) anchors[anchor] = v;
        return v;
    }

    // A scalar, alias or flow collection on the current line (a value, or a mapping key).
    (object value, bool keyLike) ReadInlineNode(int indent, string tag)
    {
        char c = C;
        if (c == '*')
        {
            p++;
            int st = p;
            while (!WsOrEnd(C) && !FlowInd(C) && !(C == ':' && WsOrEnd(At(p + 1)))) p++;
            return (Alias(s[st..p], st - 1), true);
        }
        if (c is '[' or '{') return (ApplyCollectionTagMaybe(tag, ParseFlow()), true);
        if (c is '"' or '\'')
        {
            var q = c == '"' ? ReadDouble() : ReadSingle();
            return (ResolveScalar(q, true, tag), true);
        }
        if (c is '@' or '`') throw Syntax("found character that cannot start any token");
        int start = p;
        var text = ReadPlainLine(false);
        SkipBlanks();
        if (C == ':' && WsOrEnd(At(p + 1))) return (ResolveScalar(text, false, tag), true);
        text = ContinuePlain(text, indent);
        return (ResolveScalar(text, false, tag), Col(start) >= 0 && !text.Contains('\n'));
    }

    object ApplyCollectionTagMaybe(string tag, object v) => tag == null ? v : ApplyCollectionTag(tag, v);

    object Alias(string name, int at)
    {
        if (!Aliases && !Unsafe)
            throw it.Error(it.PsychClass("AliasesNotEnabled"), "Alias parsing was not enabled. To enable it, pass `aliases: true` to `Psych::load` or `Psych::safe_load`.");
        if (!anchors.TryGetValue(name, out var v)) throw it.Error(it.PsychClass("AnchorNotDefined"), $"An alias referenced an unknown anchor: {name}");
        return v;
    }

    RHash ParseBlockMapping(int col, object firstKey, bool haveFirst, bool explicitFirst)
    {
        var h = new RHash();
        object key = firstKey;
        bool explicitKey = explicitFirst;
        while (true)
        {
            object value;
            if (explicitKey)
            {
                p++;   // '?'
                key = ParseNode(col, false);
                SkipToToken();
                if (Col(p) == col && C == ':' && WsOrEnd(At(p + 1))) { p++; value = ParseNode(col, true); }
                else value = null;
            }
            else
            {
                if (!haveFirst)
                {
                    key = ParseKey(col);
                }
                haveFirst = false;
                SkipBlanks();
                if (C != ':') throw Syntax("could not find expected ':'");
                p++;
                value = ParseNode(col, true);
            }
            AddPair(h, key, value);
            SkipToToken();
            if (C == '\0' || AtDocBoundary(p)) break;
            int c2 = Col(p);
            if (c2 < col) break;
            if (c2 > col) throw Syntax("did not find expected key while parsing a block mapping");
            if (C == '-' && WsOrEnd(At(p + 1))) break;   // a sequence at the parent's level ends this mapping
            explicitKey = C == '?' && WsOrEnd(At(p + 1));
        }
        return h;
    }

    object ParseKey(int col)
    {
        string anchor = null, tag = null;
        while (true)
        {
            if (C == '&') { p++; int st = p; while (!WsOrEnd(C) && !FlowInd(C)) p++; anchor = s[st..p]; SkipBlanks(); continue; }
            if (C == '!') { int st = p; while (!WsOrEnd(C)) p++; tag = s[st..p]; SkipBlanks(); continue; }
            break;
        }
        object k;
        if (C == '*') { p++; int st = p; while (!WsOrEnd(C) && !FlowInd(C) && !(C == ':' && WsOrEnd(At(p + 1)))) p++; k = Alias(s[st..p], st - 1); }
        else if (C is '[' or '{') k = ParseFlow();
        else if (C is '"' or '\'') k = ResolveScalar(C == '"' ? ReadDouble() : ReadSingle(), true, tag);
        else
        {
            var text = ReadPlainLine(false);
            SkipBlanks();
            if (C != ':') throw Syntax("could not find expected ':' while scanning a simple key");
            k = ResolveScalar(text, false, tag);
        }
        if (anchor != null) anchors[anchor] = k;
        return k;
    }

    void AddPair(RHash h, object key, object value)
    {
        // "<<" merges a mapping (or a list of mappings) in, like Psych's revive_hash.
        if (key is RString { V: "<<" })
        {
            if (value is RHash mh) { foreach (var kv in mh.D) h.D[kv.Key] = kv.Value; return; }
            if (value is RArray ma && ma.L.All(x => x is RHash))
            {
                var merged = new RHash();
                for (int i = ma.L.Count - 1; i >= 0; i--) foreach (var kv in ((RHash)ma.L[i]).D) merged.D[kv.Key] = kv.Value;
                foreach (var kv in merged.D) h.D[kv.Key] = kv.Value;
                return;
            }
        }
        if (key is RString ks && !ks.Frozen) key = new RString(ks.V) { Frozen = true, Binary = ks.Binary };
        if (key == null) return;   // the interpreter's Hash can't hold a nil key
        h.D[key] = value;
    }

    RArray ParseBlockSeq(int col)
    {
        var arr = new RArray();
        while (true)
        {
            p++;   // '-'
            arr.L.Add(ParseNode(col, false));
            SkipToToken();
            if (C == '\0' || AtDocBoundary(p)) break;
            int c2 = Col(p);
            if (c2 == col && C == '-' && WsOrEnd(At(p + 1))) continue;
            if (c2 <= col) break;
            throw Syntax("did not find expected '-' indicator while parsing a block collection");
        }
        return arr;
    }

    // ---- scalars ----

    string ReadPlainLine(bool flow)
    {
        int start = p;
        while (C != '\n' && C != '\0')
        {
            char c = C;
            if (c == ':' && (WsOrEnd(At(p + 1)) || (flow && FlowInd(At(p + 1))))) break;
            if (c == '#' && p > start && Blank(At(p - 1))) break;
            if (flow && FlowInd(c)) break;
            p++;
        }
        int end = p;
        while (end > start && Blank(s[end - 1])) end--;
        return s[start..end];
    }

    // Further lines of a multi-line plain scalar: more indented than `indent`, folded with spaces.
    string ContinuePlain(string text, int indent)
    {
        var sb = new StringBuilder(text);
        while (true)
        {
            SkipBlanks();
            if (C != '\n') break;
            int save = p;
            int q = p, breaks = 0, lineStart = 0;
            while (q < s.Length && s[q] == '\n')
            {
                q++;
                breaks++;
                lineStart = q;
                while (q < s.Length && Blank(s[q])) q++;
            }
            if (q >= s.Length) { p = save; break; }
            int lc = q - lineStart;
            if (lc <= indent || s[q] == '#' || (lc == 0 && (DocMarker(lineStart, "---") || DocMarker(lineStart, "...")))) { p = save; break; }
            if (s[q] == '-' && WsOrEnd(At(q + 1)) && lc <= indent + 1) { p = save; break; }
            p = q;
            var piece = ReadPlainLine(false);
            SkipBlanks();
            if (piece.Length == 0 || (C == ':' && WsOrEnd(At(p + 1)))) { p = save; break; }
            if (breaks == 1) sb.Append(' ');
            else sb.Append('\n', breaks - 1);
            sb.Append(piece);
        }
        return sb.ToString();
    }

    string ReadSingle()
    {
        int st = p;
        p++;
        var sb = new StringBuilder();
        while (true)
        {
            if (C == '\0') throw Syntax("found unexpected end of stream while scanning a quoted scalar", st);
            if (C == '\'')
            {
                if (At(p + 1) == '\'') { sb.Append('\''); p += 2; continue; }
                p++;
                break;
            }
            if (C == '\n') { Fold(sb); continue; }
            sb.Append(C);
            p++;
        }
        return sb.ToString();
    }

    // A line break inside a quoted scalar: trailing blanks go, one break is a space, more are newlines.
    void Fold(StringBuilder sb)
    {
        int len = sb.Length;
        while (len > 0 && Blank(sb[len - 1])) len--;
        sb.Length = len;
        int breaks = 0;
        while (C == '\n' || Blank(C))
        {
            if (C == '\n') breaks++;
            p++;
        }
        if (breaks == 1) sb.Append(' ');
        else sb.Append('\n', breaks - 1);
    }

    string ReadDouble()
    {
        int st = p;
        p++;
        var sb = new StringBuilder();
        while (true)
        {
            char c = C;
            if (c == '\0') throw Syntax("found unexpected end of stream while scanning a quoted scalar", st);
            if (c == '"') { p++; break; }
            if (c == '\n') { Fold(sb); continue; }
            if (c != '\\') { sb.Append(c); p++; continue; }
            p++;
            char e = C;
            p++;
            switch (e)
            {
                case '0': sb.Append('\0'); break;
                case 'a': sb.Append('\a'); break;
                case 'b': sb.Append('\b'); break;
                case 't': case '\t': sb.Append('\t'); break;
                case 'n': sb.Append('\n'); break;
                case 'v': sb.Append('\v'); break;
                case 'f': sb.Append('\f'); break;
                case 'r': sb.Append('\r'); break;
                case 'e': sb.Append('\u001b'); break;
                case ' ': sb.Append(' '); break;
                case '"': sb.Append('"'); break;
                case '/': sb.Append('/'); break;
                case '\\': sb.Append('\\'); break;
                case 'N': sb.Append('\u0085'); break;
                case '_': sb.Append('\u00a0'); break;
                case 'L': sb.Append('\u2028'); break;
                case 'P': sb.Append('\u2029'); break;
                case 'x': sb.Append(Hex(2)); break;
                case 'u': sb.Append(Hex(4)); break;
                case 'U': sb.Append(Hex(8)); break;
                case '\n':
                    // escaped line break: joins the lines without a space
                    while (Blank(C)) p++;
                    break;
                default: throw Syntax("found unknown escape character while parsing a quoted scalar", p - 2);
            }
        }
        return sb.ToString();
    }

    string Hex(int n)
    {
        if (p + n > s.Length || !s.Substring(p, n).All(Uri.IsHexDigit)) throw Syntax("did not find expected hexdecimal number while parsing a quoted scalar");
        int v = int.Parse(s.Substring(p, n), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        p += n;
        if (n == 2 && v >= 0x80) return ((char)(RString.RawByteBase + v)).ToString();
        return char.ConvertFromUtf32(v);
    }

    string ParseBlockScalar(int indent)
    {
        bool literal = C == '|';
        p++;
        char chomp = 'c';
        int inc = 0;
        for (int k = 0; k < 2; k++)
        {
            if (C is '+' or '-') { chomp = C; p++; }
            else if (C is >= '1' and <= '9') { inc = C - '0'; p++; }
        }
        SkipBlanks();
        if (C == '#') while (C != '\n' && C != '\0') p++;
        if (C != '\n' && C != '\0') throw Syntax("did not find expected comment or line break while scanning a block scalar");
        if (C == '\n') p++;
        int baseIndent = Math.Max(indent, 0);
        int contentIndent = inc > 0 ? (indent < 0 ? inc : baseIndent + inc) : -1;
        var lines = new List<string>();
        while (p < s.Length)
        {
            int ls = p, q = p;
            while (q < s.Length && s[q] == ' ') q++;
            int lineEnd = s.IndexOf('\n', ls);
            if (lineEnd < 0) lineEnd = s.Length;
            bool empty = q >= lineEnd || s[q] == '\n';
            int ind = q - ls;
            if (empty)
            {
                lines.Add(contentIndent >= 0 && lineEnd - ls > contentIndent ? s[(ls + contentIndent)..lineEnd] : "");
                p = Math.Min(lineEnd + 1, s.Length);
                if (lineEnd == s.Length) break;
                continue;
            }
            if (contentIndent < 0)
            {
                if (ind <= indent) break;
                contentIndent = ind;
            }
            if (ind < contentIndent || (ind == 0 && AtDocBoundary(ls))) { p = ls; break; }
            lines.Add(s[(ls + contentIndent)..lineEnd]);
            p = Math.Min(lineEnd + 1, s.Length);
            if (lineEnd == s.Length) break;
        }
        // p is at the first line not in the scalar; step back onto the previous line break so the
        // caller's token skip sees the following key at its own column
        if (p > 0 && p <= s.Length && At(p - 1) == '\n') p--;
        int last = lines.Count - 1;
        while (last >= 0 && lines[last].Length == 0) last--;
        int trailing = lines.Count - 1 - last;
        var body = new StringBuilder();
        if (literal)
        {
            for (int i = 0; i <= last; i++) { if (i > 0) body.Append('\n'); body.Append(lines[i]); }
        }
        else
        {
            int empties = 0;
            bool any = false, prevNormal = false;
            for (int i = 0; i <= last; i++)
            {
                var line = lines[i];
                if (line.Length == 0) { empties++; continue; }
                bool more = line[0] is ' ' or '\t';
                if (any)
                {
                    if (prevNormal && !more) { if (empties == 0) body.Append(' '); else body.Append('\n', empties); }
                    else body.Append('\n', empties + 1);
                }
                else body.Append('\n', empties);
                body.Append(line);
                any = true;
                empties = 0;
                prevNormal = !more;
            }
        }
        if (last < 0)
        {
            // only empty lines
            return chomp == '+' ? new string('\n', lines.Count) : "";
        }
        return chomp switch
        {
            '-' => body.ToString(),
            '+' => body.Append('\n').Append('\n', trailing).ToString(),
            _ => body.Append('\n').ToString(),
        };
    }

    // ---- flow collections ----

    void SkipFlowWs()
    {
        while (true)
        {
            while (C is ' ' or '\t' or '\n') p++;
            if (C == '#') { while (C != '\n' && C != '\0') p++; continue; }
            return;
        }
    }

    object ParseFlow()
    {
        if (C == '[')
        {
            int st = p;
            p++;
            var arr = new RArray();
            while (true)
            {
                SkipFlowWs();
                if (C == '\0') throw Syntax("did not find expected ',' or ']' while parsing a flow sequence", st);
                if (C == ']') { p++; break; }
                object item;
                if (C == ':' && (WsOrEnd(At(p + 1)) || FlowInd(At(p + 1)))) item = null;
                else item = ParseFlowNode();
                SkipFlowWs();
                if (C == ':')
                {
                    p++;
                    SkipFlowWs();
                    var v = C is ',' or ']' ? null : ParseFlowNode();
                    var pair = new RHash();
                    AddPair(pair, item, v);
                    item = pair;
                    SkipFlowWs();
                }
                arr.L.Add(item);
                if (C == ',') { p++; continue; }
                if (C == ']') { p++; break; }
                throw Syntax("did not find expected ',' or ']' while parsing a flow sequence", st);
            }
            return arr;
        }
        else
        {
            int st = p;
            p++;
            var h = new RHash();
            while (true)
            {
                SkipFlowWs();
                if (C == '\0') throw Syntax("did not find expected ',' or '}' while parsing a flow mapping", st);
                if (C == '}') { p++; break; }
                if (C == '?' && WsOrEnd(At(p + 1))) { p++; SkipFlowWs(); }
                object key = C == ':' ? null : ParseFlowNode();
                SkipFlowWs();
                object value = null;
                if (C == ':')
                {
                    p++;
                    SkipFlowWs();
                    if (C is not (',' or '}')) value = ParseFlowNode();
                    SkipFlowWs();
                }
                AddPair(h, key, value);
                if (C == ',') { p++; continue; }
                if (C == '}') { p++; break; }
                throw Syntax("did not find expected ',' or '}' while parsing a flow mapping", st);
            }
            return h;
        }
    }

    object ParseFlowNode()
    {
        string anchor = null, tag = null;
        while (true)
        {
            if (C == '&') { p++; int st = p; while (!WsOrEnd(C) && !FlowInd(C)) p++; anchor = s[st..p]; SkipFlowWs(); continue; }
            if (C == '!') { int st = p; while (!WsOrEnd(C) && !FlowInd(C)) p++; tag = s[st..p]; SkipFlowWs(); continue; }
            break;
        }
        object v;
        if (C is '[' or '{') { v = ParseFlow(); if (tag != null) v = ApplyCollectionTag(tag, v); }
        else if (C == '*') { p++; int st = p; while (!WsOrEnd(C) && !FlowInd(C)) p++; v = Alias(s[st..p], st - 1); }
        else if (C is '"' or '\'') v = ResolveScalar(C == '"' ? ReadDouble() : ReadSingle(), true, tag);
        else
        {
            // plain, possibly over several lines
            var sb = new StringBuilder(ReadPlainLine(true));
            while (C == '\n' || Blank(C))
            {
                int save = p;
                int breaks = 0;
                while (C is '\n' or ' ' or '\t') { if (C == '\n') breaks++; p++; }
                if (C == '#' || C == '\0' || FlowInd(C) || (C == ':' && (WsOrEnd(At(p + 1)) || FlowInd(At(p + 1))))) { p = save; break; }
                var piece = ReadPlainLine(true);
                if (piece.Length == 0) { p = save; break; }
                if (breaks <= 1) sb.Append(' ');
                else sb.Append('\n', breaks - 1);
                sb.Append(piece);
            }
            v = ResolveScalar(sb.ToString(), false, tag);
        }
        if (anchor != null) anchors[anchor] = v;
        return v;
    }

    // ---- resolution ----

    static readonly Regex IntLegacy = new(@"^(?:[-+]?0b[0-1_,]+|[-+]?0[0-7_,]+|[-+]?(?:0|[1-9](?:[0-9]|,[0-9]|_[0-9])*)|[-+]?0x[0-9a-fA-F_,]+)$");
    static readonly Regex FloatRe = new(@"^(?:[-+]?([0-9][0-9_,]*)?\.[0-9]*([eE][-+][0-9]+)?)$");
    static readonly Regex TimeRe = new(@"^-?\d{4}-\d{1,2}-\d{1,2}(?:[Tt]|\s+)\d{1,2}:\d\d:\d\d(?:\.\d*)?(?:\s*(?:Z|[-+]\d{1,2}:?(?:\d\d)?))?$");
    static readonly Regex DateRe = new(@"^\d{4}-(?:1[012]|0\d|\d)-(?:[12]\d|3[01]|0\d|\d)$");
    static readonly Regex StringLike = new(@"^[^\d.:-]?[\p{L}\p{M}_\s!@#$%\^&*(){}<>|/\\~;=]+");

    object ResolveScalar(string text, bool quoted, string tag)
    {
        if (tag != null) return ApplyScalarTag(tag, text, quoted);
        if (quoted) return Interp.S(text);
        return Tokenize(text);
    }

    // Psych::ScalarScanner#tokenize
    public object Tokenize(string str)
    {
        if (str.Length == 0) return null;
        if (StringLike.IsMatch(str) || str.Contains('\n'))
        {
            if (str.Length > 5) return Interp.S(str);
            if (Regex.IsMatch(str, "^[^ytonf~]", RegexOptions.IgnoreCase)) return Interp.S(str);
            if (str == "~" || str.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;
            if (Regex.IsMatch(str, "^(yes|true|on)$", RegexOptions.IgnoreCase)) return true;
            if (Regex.IsMatch(str, "^(no|false|off)$", RegexOptions.IgnoreCase)) return false;
            return Interp.S(str);
        }
        if (TimeRe.IsMatch(str))
        {
            try
            {
                var t = ParseYamlTime(str);
                Check("Time");
                return t;
            }
            catch (FormatException) { return Interp.S(str); }
        }
        if (DateRe.IsMatch(str))
        {
            var parts = str.Split('-');
            int y = int.Parse(parts[0]), m = int.Parse(parts[1]), d = int.Parse(parts[2]);
            if (m < 1 || m > 12 || d < 1 || d > DateTime.DaysInMonth(y, m)) return Interp.S(str);
            Check("Date");
            return it.NewDate(y, m, d);
        }
        if (Regex.IsMatch(str, @"^\+?\.inf$", RegexOptions.IgnoreCase)) return double.PositiveInfinity;
        if (Regex.IsMatch(str, @"^-\.inf$", RegexOptions.IgnoreCase)) return double.NegativeInfinity;
        if (Regex.IsMatch(str, @"^\.nan$", RegexOptions.IgnoreCase)) return double.NaN;
        if (str.Length > 1 && str[0] == ':')
        {
            var m = Regex.Match(str, @"^:([""'])(.*)\1$");
            var name = m.Success ? m.Groups[2].Value : str[1..];
            return Symbol(name);
        }
        if (Regex.IsMatch(str, @"^[-+]?[0-9][0-9_]*(:[0-5]?[0-9]){1,2}$"))
        {
            // base 60: 1:30 is 90
            BigInteger v = 0;
            bool neg = str[0] == '-';
            foreach (var part in str.TrimStart('-', '+').Split(':')) v = v * 60 + BigInteger.Parse(part.Replace("_", ""));
            return Interp.Norm(neg ? -v : v);
        }
        if (Regex.IsMatch(str, @"^[-+]?[0-9][0-9_]*(:[0-5]?[0-9]){1,2}\.[0-9_]*$"))
        {
            double v = 0;
            bool neg = str[0] == '-';
            foreach (var part in str.TrimStart('-', '+').Split(':')) v = v * 60 + double.Parse(part.Replace("_", ""), CultureInfo.InvariantCulture);
            return neg ? -v : v;
        }
        if (FloatRe.IsMatch(str))
        {
            if (Regex.IsMatch(str, @"\A[-+]?\.\z")) return Interp.S(str);
            var f = Regex.Replace(str.Replace(",", "").Replace("_", ""), @"\.([Ee]|$)", "$1");
            return double.Parse(f, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        if (IntLegacy.IsMatch(str)) return ParseInt(str);
        return Interp.S(str);
    }

    object ParseInt(string str)
    {
        var t = str.Replace(",", "").Replace("_", "");
        bool neg = t.StartsWith('-');
        if (t[0] is '-' or '+') t = t[1..];
        int radix = 10;
        if (t.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) { radix = 2; t = t[2..]; }
        else if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) { radix = 16; t = t[2..]; }
        else if (t.Length > 1 && t[0] == '0') { radix = 8; t = t[1..]; }
        var v = Interp.ParseDigits(t, radix) ?? BigInteger.Zero;
        return Interp.Norm(neg ? -v : v);
    }

    object Symbol(string name)
    {
        Check("Symbol");
        if (!Unsafe && PermittedSymbols.Count > 0 && !PermittedSymbols.Contains(name)) throw Disallowed("Symbol");
        return Interp.Sym(name);
    }

    RObject ParseYamlTime(string str)
    {
        var m = Regex.Match(str, @"^(-?\d{4})-(\d{1,2})-(\d{1,2})(?:[Tt]|\s+)(\d{1,2}):(\d\d):(\d\d)(?:\.(\d*))?\s*(Z|[-+]\d{1,2}(?::?\d\d)?)?$");
        if (!m.Success) throw new FormatException();
        int y = int.Parse(m.Groups[1].Value), mo = int.Parse(m.Groups[2].Value), d = int.Parse(m.Groups[3].Value);
        if (mo < 1 || mo > 12 || d < 1 || d > DateTime.DaysInMonth(y, mo)) throw new FormatException();
        var frac = m.Groups[7].Value;
        long ticks = frac.Length == 0 ? 0 : long.Parse((frac + "0000000")[..7]);
        var wall = new DateTime(y, mo, d, int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value)).AddTicks(ticks);
        var z = m.Groups[8].Value;
        // Psych's parse_time: "Z" is a UTC Time, no zone is that instant (read as UTC) in local
        // time, an offset is a Time at that fixed offset.
        if (z == "Z") return it.NewTime(new DateTimeOffset(wall, TimeSpan.Zero), TimeKind.Utc);
        if (z.Length == 0) return it.LocalTime(wall);
        var zm = Regex.Match(z, @"^([-+])(\d{1,2}):?(\d\d)?$");
        var off = new TimeSpan(int.Parse(zm.Groups[2].Value), zm.Groups[3].Success ? int.Parse(zm.Groups[3].Value) : 0, 0);
        if (zm.Groups[1].Value == "-") off = -off;
        return it.NewTime(new DateTimeOffset(wall, off), TimeKind.Fixed);
    }

    static string TagName(string tag)
    {
        if (tag.StartsWith("!!")) return "tag:yaml.org,2002:" + tag[2..];
        if (tag.StartsWith("!<") && tag.EndsWith(">")) return tag[2..^1];
        return tag;
    }

    object ApplyScalarTag(string tag, string text, bool quoted)
    {
        var t = TagName(tag);
        switch (t)
        {
            case "tag:yaml.org,2002:str" or "!str": return Interp.S(text);
            case "tag:yaml.org,2002:null": return null;
            case "tag:yaml.org,2002:bool": return Tokenize(text) is bool b ? b : Interp.S(text);
            case "tag:yaml.org,2002:int":
            {
                if (IntLegacy.IsMatch(text)) return ParseInt(text);
                return Tokenize(text);
            }
            case "tag:yaml.org,2002:float":
            {
                var v = Tokenize(text);
                return v is long l ? (double)l : v is double ? v : double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : Interp.S(text);
            }
            case "tag:yaml.org,2002:binary" or "!binary":
                try { return new RString(Encoding.Latin1.GetString(Convert.FromBase64String(Regex.Replace(text, @"\s", "")))) { Binary = true }; }
                catch (FormatException) { throw Syntax("invalid base64"); }
            case "tag:yaml.org,2002:timestamp": return quoted ? Interp.S(text) : Tokenize(text);
            case "!ruby/symbol" or "!ruby/sym": return Symbol(text);
            case "!ruby/string" or "tag:yaml.org,2002:ruby/string": return Interp.S(text);
            case "!ruby/regexp":
            {
                Check("Regexp");
                var m = Regex.Match(text, @"^/(.*)/([mixn]*)$", RegexOptions.Singleline);
                return it.Invoke(it.RegexpClass, "new", new object[] { Interp.S(m.Success ? m.Groups[1].Value : text) }, null);
            }
            case "!ruby/class" or "!ruby/module":
                Check("Class");
                return it.ConstGet(it.ObjectClass, text) ?? throw it.ArgError("undefined class/module " + text);
        }
        if (t.StartsWith("!ruby/object:") || t.StartsWith("!ruby/exception:") || t.StartsWith("!ruby/struct:"))
        {
            var cls = t[(t.IndexOf(':') + 1)..];
            Check(cls);
            return quoted ? Interp.S(text) : Tokenize(text);
        }
        // unknown tags: the plain value
        return quoted ? Interp.S(text) : Tokenize(text);
    }

    object ApplyCollectionTag(string tag, object v)
    {
        var t = TagName(tag);
        if (v is not (RHash or RArray)) return v is RString rs && t is "tag:yaml.org,2002:str" ? rs : v;
        if (t == "tag:yaml.org,2002:set" && v is RHash sh)
        {
            Check("Set");
            return it.Invoke(it.SetClass, "new", new object[] { new RArray(sh.D.Keys) }, null);
        }
        if (t.StartsWith("!ruby/object:") || t.StartsWith("!ruby/exception:") || t.StartsWith("!ruby/struct:"))
        {
            var name = t[(t.IndexOf(':') + 1)..];
            Check(name);
            if (v is not RHash h) return v;
            if (name == "Set") return it.Invoke(it.SetClass, "new", new object[] { h.D.TryGetValue(Interp.S("hash"), out var inner) && inner is RHash ih ? new RArray(ih.D.Keys) : new RArray() }, null);
            var cls = ResolveClass(name);
            if (cls == null) throw it.ArgError("undefined class/module " + name);
            var obj = new RObject { Class = cls };
            if (t.StartsWith("!ruby/exception:") && h.D.TryGetValue(Interp.S("message"), out var msg)) obj.SetIvar("@message", msg);
            foreach (var kv in h.D)
                if (kv.Key is RString ks && !(t.StartsWith("!ruby/exception:") && ks.V == "message")) obj.SetIvar("@" + ks.V, kv.Value);
            return obj;
        }
        if (t is "!ruby/range" && v is RHash rh)
        {
            Check("Range");
            rh.D.TryGetValue(Interp.S("begin"), out var b);
            rh.D.TryGetValue(Interp.S("end"), out var e);
            rh.D.TryGetValue(Interp.S("excl"), out var x);
            return new RRange { From = b, To = e, Excl = Interp.Truthy(x) };
        }
        if (t.StartsWith("!ruby/hash:") || t.StartsWith("!ruby/array:")) Check(t[(t.IndexOf(':') + 1)..]);
        return v;
    }

    RClass ResolveClass(string path)
    {
        RClass cur = it.ObjectClass;
        foreach (var part in path.Split("::"))
        {
            cur = it.ConstGet(cur, part) as RClass;
            if (cur == null) return null;
        }
        return cur;
    }
}

public sealed partial class Interp
{
    RClass psych;

    public RClass PsychClass(string name) => (RClass)psych.Consts[name];

    void InitYaml()
    {
        var P = psych = DefineModule("Psych");
        ObjectClass.Consts["YAML"] = P;
        var pex = DefineClass("Exception", RuntimeError, P);
        var badAlias = DefineClass("BadAlias", pex, P);
        DefineClass("AliasesNotEnabled", badAlias, P);
        DefineClass("AnchorNotDefined", badAlias, P);
        DefineClass("DisallowedClass", pex, P);
        var syn = DefineClass("SyntaxError", pex, P);
        foreach (var n in new[] { "file", "line", "column", "problem" })
        {
            var iv = "@" + n;
            Def(syn, n, (it, self, a, b) => ((RObject)self).GetIvar(iv));
        }
        P.Consts["VERSION"] = S("5.2.2");
        P.Consts["LIBYAML_VERSION"] = S("0.2.5");

        DefS(P, "load", (it, self, a, b) => it.YamlLoad(a, unsafeLoad: false, defaultPermitted: new[] { "Symbol" }));
        DefS(P, "safe_load", (it, self, a, b) => it.YamlLoad(a, unsafeLoad: false, defaultPermitted: Array.Empty<string>()));
        DefS(P, "unsafe_load", (it, self, a, b) => it.YamlLoad(a, unsafeLoad: true, defaultPermitted: Array.Empty<string>()));
        foreach (var (name, target) in new[] { ("load_file", "load"), ("safe_load_file", "safe_load"), ("unsafe_load_file", "unsafe_load") })
        {
            var tgt = target;
            DefS(P, name, (it, self, a, b) =>
            {
                var path = it.Abs(it.PathStr(a[0]));
                if (!File.Exists(path)) throw it.Error(it.ENOENT, "No such file or directory @ rb_sysopen - " + it.PathStr(a[0]));
                var text = File.ReadAllText(path);
                var rest = a.Skip(1).ToList();
                var kw = rest.Count > 0 && rest[^1] is RHash h0 ? it.CopyHash(h0) : new RHash();
                if (rest.Count > 0 && rest[^1] is RHash) rest.RemoveAt(rest.Count - 1);
                kw.D[Sym("filename")] = S(it.PathStr(a[0]));
                if (tgt == "unsafe_load" && !kw.D.ContainsKey(Sym("fallback"))) kw.D[Sym("fallback")] = false;
                return it.Invoke(self, tgt, new object[] { S(text) }.Concat(rest).Append(kw).ToArray(), null);
            });
        }
        DefS(P, "load_stream", (it, self, a, b) =>
        {
            var parser = new YamlParser(it, it.YamlSource(a[0]), Kw(a, "filename") is RString fn ? fn.V : null) { Unsafe = true, Aliases = true };
            var docs = parser.ParseStream();
            if (b != null) { foreach (var d in docs) it.CallProc(b, new[] { d }); return null; }
            return new RArray(docs);
        });
        DefS(P, "dump safe_dump", (it, self, a, b) =>
        {
            var text = it.YamlDump(a[0]);
            if (a.Length > 1 && a[1] is not RHash && a[1] != null) { it.Invoke(a[1], "write", new object[] { S(text) }, null); return a[1]; }
            return S(text);
        });
        DefS(P, "dump_stream", (it, self, a, b) => S(string.Concat(a.Select(it.YamlDump))));
        DefS(P, "to_json", (it, self, a, b) => it.Invoke(a[0], "to_json", NoArgs, null));
        Def(ObjectClass, "to_yaml", (it, self, a, b) => S(it.YamlDump(self)));
    }

    string YamlSource(object o)
    {
        if (o is RString s) return s.V;
        if (o != null && RespondTo(o, "read")) return ToS(Invoke(o, "read", NoArgs, null));
        return Str(o);
    }

    object YamlLoad(object[] a, bool unsafeLoad, string[] defaultPermitted)
    {
        if (a.Length == 0) throw ArgCount(a, 1, 1);
        var src = YamlSource(a[0]);
        var pos = Positional(a);
        var parser = new YamlParser(this, src, Kw(a, "filename") is { } fn && fn != null ? ToS(fn) : null) { Unsafe = unsafeLoad };
        object permitted = Kw(a, "permitted_classes") ?? (pos.Length > 1 ? pos[1] : null);
        object syms = Kw(a, "permitted_symbols") ?? (pos.Length > 2 ? pos[2] : null);
        object aliases = Kw(a, "aliases") ?? (pos.Length > 3 ? pos[3] : null);
        foreach (var c in defaultPermitted) parser.Permitted.Add(c);
        if (permitted is RArray pa)
        {
            parser.Permitted.Clear();
            foreach (var c in pa.L) parser.Permitted.Add(c is RClass k ? k.Name : ToS(c));
        }
        if (syms is RArray sa) foreach (var x in sa.L) parser.PermittedSymbols.Add(x is RSymbol y ? y.Name : ToS(x));
        parser.Aliases = Truthy(aliases);
        var docs = parser.ParseStream();
        object result;
        if (docs.Count == 0) result = a.Length > 0 && a[^1] is RHash kw && kw.D.TryGetValue(Sym("fallback"), out var fb) ? fb : null;
        else result = docs[0];
        if (Truthy(Kw(a, "symbolize_names"))) result = SymbolizeNames(result);
        if (Truthy(Kw(a, "freeze"))) DeepFreeze(result);
        return result;
    }

    object SymbolizeNames(object o)
    {
        switch (o)
        {
            case RHash h:
            {
                var r = new RHash();
                foreach (var kv in h.D) r.D[kv.Key is RString ks ? Sym(ks.V) : kv.Key] = SymbolizeNames(kv.Value);
                return r;
            }
            case RArray arr:
                for (int i = 0; i < arr.L.Count; i++) arr.L[i] = SymbolizeNames(arr.L[i]);
                return arr;
        }
        return o;
    }

    static void DeepFreeze(object o)
    {
        switch (o)
        {
            case RString s: s.Frozen = true; break;
            case RArray a: a.Frozen = true; foreach (var x in a.L) DeepFreeze(x); break;
            case RHash h: h.Frozen = true; foreach (var kv in h.D) { DeepFreeze(kv.Key); DeepFreeze(kv.Value); } break;
            case RObject r: r.Frozen = true; break;
        }
    }

    // ---- emitter ----

    public string YamlDump(object o)
    {
        var sb = new StringBuilder("---");
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        EmitNode(sb, o, 0, YamlCtx.Top, seen);
        return sb.ToString();
    }

    enum YamlCtx { Top, MapValue, SeqItem }

    void EmitNode(StringBuilder sb, object o, int col, YamlCtx ctx, HashSet<object> seen)
    {
        switch (o)
        {
            case RHash h when h.D.Count == 0: sb.Append(" {}\n"); return;
            case RArray a when a.L.Count == 0: sb.Append(" []\n"); return;
            case RHash h:
                if (!seen.Add(h)) throw ArgError("recursive structure can't be dumped");
                if (ctx == YamlCtx.SeqItem) { sb.Append(' '); EmitMapping(sb, h, col + 2, true, seen); }
                else { sb.Append('\n'); EmitMapping(sb, h, ctx == YamlCtx.Top ? 0 : col + 2, false, seen); }
                seen.Remove(h);
                return;
            case RArray a:
                if (!seen.Add(a)) throw ArgError("recursive structure can't be dumped");
                if (ctx == YamlCtx.SeqItem) { sb.Append(' '); EmitSeq(sb, a, col + 2, true, seen); }
                else { sb.Append('\n'); EmitSeq(sb, a, ctx == YamlCtx.Top ? 0 : col, false, seen); }
                seen.Remove(a);
                return;
            case null:
                sb.Append(ctx == YamlCtx.Top ? "\n...\n" : "\n");
                return;
        }
        if (o is RObject ro && YamlObjectTag(ro) is { } tagged)
        {
            var (tag, fields) = tagged;
            sb.Append(' ').Append(tag);
            if (fields == null || fields.D.Count == 0) { sb.Append(fields == null ? "\n" : " {}\n"); return; }
            sb.Append('\n');
            EmitMapping(sb, fields, ctx == YamlCtx.Top ? 0 : col + 2, false, seen);
            return;
        }
        if (o is RString str && str.V.Contains('\n') && Regex.IsMatch(str.V, @"\n(?!\z)") && YamlLiteralOk(str))
        {
            var v = str.V;
            var chomp = v.EndsWith("\n\n") ? "+" : v.EndsWith('\n') ? "" : "-";
            var body = v.EndsWith('\n') ? v[..^1] : v;
            if (chomp == "+") body = v[..^1];
            sb.Append(" |").Append(body.Length > 0 && body[0] == ' ' ? "2" : "").Append(chomp).Append('\n');
            int ind = ctx == YamlCtx.Top ? 2 : col + 2;
            foreach (var line in body.Split('\n'))
            {
                if (line.Length > 0) sb.Append(' ', ind).Append(line);
                sb.Append('\n');
            }
            return;
        }
        sb.Append(' ').Append(YamlScalar(o)).Append('\n');
    }

    void EmitMapping(StringBuilder sb, RHash h, int col, bool firstInline, HashSet<object> seen)
    {
        bool first = true;
        foreach (var kv in h.D)
        {
            if (!(first && firstInline)) sb.Append(' ', col);
            first = false;
            sb.Append(YamlKey(kv.Key)).Append(':');
            EmitNode(sb, kv.Value, col, YamlCtx.MapValue, seen);
        }
    }

    void EmitSeq(StringBuilder sb, RArray a, int col, bool firstInline, HashSet<object> seen)
    {
        bool first = true;
        foreach (var x in a.L)
        {
            if (!(first && firstInline)) sb.Append(' ', col);
            first = false;
            sb.Append('-');
            EmitNode(sb, x, col, YamlCtx.SeqItem, seen);
        }
    }

    (string tag, RHash fields)? YamlObjectTag(RObject o)
    {
        if (o.Host is RTime or RDate or RUri) return null;
        if (o is RClass c) return ($"!ruby/class '{c.Name}'", null);
        var cls = o.Class;
        if (IsA(o, SetClass))
        {
            var inner = new RHash();
            foreach (var x in ((RHash)o.Host).D.Keys) inner.D[x] = true;
            var f = new RHash();
            f.D[S("hash")] = inner;
            return ("!ruby/object:" + cls.Name, f);
        }
        var fields = new RHash();
        if (IsA(o, ExceptionClass))
        {
            fields.D[S("message")] = o.GetIvar("@message");
            if (o.Ivars != null) foreach (var kv in o.Ivars) if (kv.Key != "@message" && !kv.Key.StartsWith("@__")) fields.D[S(kv.Key.TrimStart('@'))] = kv.Value;
            return ("!ruby/exception:" + cls.Name, fields);
        }
        if (o.Host is string path && cls.Name == "Pathname") { fields.D[S("path")] = S(path); return ("!ruby/object:Pathname", fields); }
        if (o.Ivars != null) foreach (var kv in o.Ivars) fields.D[S(kv.Key.TrimStart('@'))] = kv.Value;
        return ("!ruby/object:" + cls.Name, fields);
    }

    static bool YamlLiteralOk(RString s) =>
        !s.Binary && s.V.All(c => c == '\n' || Printable(c))
        && !s.V.Split('\n').Any(l => l.EndsWith(' ') || l.EndsWith('\t'));

    string YamlKey(object k)
    {
        if (k is RArray or RHash) return "? " + Inspect(k);
        if (k == null) return "''";   // never produced by Ruby, but stay parseable
        var s = YamlScalar(k);
        return s;
    }

    string YamlScalar(object o)
    {
        switch (o)
        {
            case null: return "";
            case bool b: return b ? "true" : "false";
            case long or BigInteger: return ToS(o);
            case double d:
                if (double.IsNaN(d)) return ".nan";
                if (double.IsPositiveInfinity(d)) return ".inf";
                if (double.IsNegativeInfinity(d)) return "-.inf";
                return FloatToS(d);
            case RSymbol y: return y.Name.Length == 0 ? "!ruby/symbol ''" : YamlStringStyle(":" + y.Name, false);
            case RString s:
                if (s.Binary && (s.V.Any(c => c >= 0x80) || s.V.Any(c => c < 0x20 && c is not ('\n' or '\t' or '\r'))))
                    return "!binary |-\n  " + Convert.ToBase64String(RString.Bytes(s));
                return YamlStringStyle(s.V, true);
            case RObject { Host: RDate dd } r:
                return dd.Dt ? Strftime(dd.T, null, "%Y-%m-%d %H:%M:%S.%N ") + FormatOffset(dd.T.Offset, ":") : Strftime(dd.T, null, "%Y-%m-%d");
            case RObject { Host: RTime or DateTime } r:
            {
                var t = TimeOf(r);
                return Strftime(t.T, null, "%Y-%m-%d %H:%M:%S.%N ") + (t.Kind == TimeKind.Utc ? "Z" : FormatOffset(t.T.Offset, ":"));
            }
            case RObject { Host: RUri u }: return "!ruby/object:" + ((RObject)o).Class.Name + " " + YamlStringStyle(UriToS(u), true);
            case RRegex re: return "!ruby/regexp " + Inspect(re);
            case RRange rg: return "!ruby/range " + YamlStringStyle(Inspect(rg), true);
        }
        return YamlStringStyle(ToS(o), true);
    }

    // Psych's YAMLTree#visit_String style choice, then libyaml's check whether plain is allowed.
    string YamlStringStyle(string v, bool isString)
    {
        if (isString)
        {
            if (v == "<<") return "! '<<'";
            if (v is "y" or "Y" or "n" or "N") return DoubleQuote(v);
            if (Regex.IsMatch(v, @"^[^\p{L}\p{N}_][^""]*$")) return DoubleQuote(v);
            var probe = new YamlParser(this, "", null) { Unsafe = true };
            object tok;
            try { tok = probe.Tokenize(v); }
            catch (RubyError) { tok = null; }
            if (tok is not RString || Regex.IsMatch(v, @"\A0[0-7]*[89]")) return SingleQuote(v) ?? DoubleQuote(v);
        }
        if (PlainOk(v)) return v;
        return SingleQuote(v) ?? DoubleQuote(v);
    }

    static bool Printable(char c) => (c >= ' ' && c != '\u007f' && !(c >= '\u0080' && c < '\u00a0') && !RString.IsRawByte(c) && c != '\uFEFF' && c != '\u2028' && c != '\u2029');

    static bool PlainOk(string v)
    {
        if (v.Length == 0 || !v.All(c => Printable(c) && c != '\t')) return false;
        if (v[0] == ' ' || v[^1] == ' ') return false;
        if ("-?:,[]{}#&*!|>'\"%@`".IndexOf(v[0]) >= 0)
        {
            // "-x", ":x", "?x" may start a plain scalar; the others never
            if (!(v[0] is '-' or '?' or ':') || v.Length == 1 || v[1] == ' ') return false;
        }
        if (v.Contains(": ") || v.Contains(" #") || v.EndsWith(':')) return false;
        if (v.StartsWith("---") || v.StartsWith("...")) return false;
        return true;
    }

    static string SingleQuote(string v)
    {
        if (!v.All(Printable) || v.Contains('\n')) return null;
        return "'" + v.Replace("'", "''") + "'";
    }

    static string DoubleQuote(string v)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in v)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\r': sb.Append("\\r"); break;
                case '\0': sb.Append("\\0"); break;
                case '\a': sb.Append("\\a"); break;
                case '\b': sb.Append("\\b"); break;
                case '\v': sb.Append("\\v"); break;
                case '\f': sb.Append("\\f"); break;
                case '\u001b': sb.Append("\\e"); break;
                case '\u0085': sb.Append("\\N"); break;
                case '\u00a0': sb.Append("\\_"); break;
                case '\u2028': sb.Append("\\L"); break;
                case '\u2029': sb.Append("\\P"); break;
                default:
                    if (RString.IsRawByte(c)) sb.Append($"\\x{c - RString.RawByteBase:X2}");
                    else if (c < ' ' || c == '\u007f' || (c >= '\u0080' && c < '\u00a0') || c == '\uFEFF') sb.Append(c <= 'ÿ' ? $"\\x{(int)c:X2}" : $"\\u{(int)c:X4}");
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }
}
