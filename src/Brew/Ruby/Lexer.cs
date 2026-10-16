using System.Text;

namespace Brew.Ruby;

public enum T
{
    EOF, Newline, Ident, Const, IVar, GVar, Symbol, Int, Float, Str, XStr, Regex, Words, Op, Kw,
}

// A string literal may contain interpolation; parts are either string or a token list (code).
public sealed class StrPart
{
    public string Text;
    public string Code;
    public int Line;
}

public sealed class Token
{
    public T Type;
    public string Text;
    public List<StrPart> Parts;       // Str / XStr / Regex / Symbol(dsym)
    public List<string> WordList;     // %w[]
    public List<List<StrPart>> WordParts;  // %W[] / %I[] with #{} interpolation
    public bool SpaceBefore;
    public int Line;
    public override string ToString() => $"{Type}:{Text}@{Line}";
}

public sealed class RubySyntaxError : Exception
{
    public RubySyntaxError(string msg, int line) : base($"syntax error, line {line}: {msg}") { }
}

public sealed class Lexer
{
    static readonly HashSet<string> Keywords = new()
    {
        "class", "module", "def", "end", "do", "if", "elsif", "else", "unless", "while", "until",
        "case", "when", "then", "return", "next", "break", "and", "or", "not", "nil", "true", "false",
        "self", "begin", "rescue", "ensure", "yield", "in", "for", "super", "__FILE__", "__dir__",
    };

    // Longest first.
    static readonly string[] Ops =
    {
        "**=", "<=>", "===", "...", "<<=", ">>=", "&&=", "||=", "&.",
        "**", "==", "!=", ">=", "<=", "&&", "||", "<<", ">>", "=~", "!~", "+=", "-=", "*=", "/=", "%=", "|=", "&=",
        "=>", "->", "::", "..",
        "+", "-", "*", "/", "%", "=", "<", ">", "!", "&", "|", "^", "~", "?", ":", ",", ".", ";",
        "(", ")", "[", "]", "{", "}",
    };

    readonly string src;
    int pos;
    int line = 1;
    int parenDepth;
    readonly List<Token> tokens = new();
    readonly List<(string id, bool squiggly, bool dash, bool raw, Token tok)> pendingHeredocs = new();

    public Lexer(string source)
    {
        src = source.Replace("\r\n", "\n");
        if (src.Length > 0 && src[0] == '﻿') src = src[1..];
    }

    char Cur => pos < src.Length ? src[pos] : '\0';
    char Peek(int n = 1) => pos + n < src.Length ? src[pos + n] : '\0';

    Token Last => tokens.Count > 0 ? tokens[^1] : null;

    bool LastIsValue()
    {
        var t = Last;
        if (t == null) return false;
        switch (t.Type)
        {
            case T.Ident: case T.Const: case T.IVar: case T.GVar: case T.Int: case T.Float:
            case T.Str: case T.XStr: case T.Regex: case T.Words: case T.Symbol:
                return true;
            case T.Kw:
                return t.Text is "end" or "self" or "nil" or "true" or "false" or "__FILE__" or "__dir__";
            case T.Op:
                return t.Text is ")" or "]" or "}";
        }
        return false;
    }

    void Add(T type, string text, bool space)
    {
        tokens.Add(new Token { Type = type, Text = text, SpaceBefore = space, Line = line });
    }

    public List<Token> Lex()
    {
        while (true)
        {
            bool space = false;
            // whitespace / comments / line continuations
            while (true)
            {
                if (Cur == ' ' || Cur == '\t' || Cur == '\r') { pos++; space = true; continue; }
                if (Cur == '\\' && Peek() == '\n') { pos += 2; line++; space = true; continue; }
                if (Cur == '#') { while (pos < src.Length && Cur != '\n') pos++; continue; }
                if (Cur == '\n' && AtLineStart() && src.AsSpan(pos + 1).StartsWith("=begin"))
                {
                    int e = src.IndexOf("\n=end", pos + 1, StringComparison.Ordinal);
                    if (e < 0) throw new RubySyntaxError("unterminated =begin", line);
                    for (int i = pos; i < e + 5; i++) if (src[i] == '\n') line++;
                    pos = e + 5;
                    while (pos < src.Length && Cur != '\n') pos++;
                    continue;
                }
                break;
            }
            if (pos >= src.Length) { Add(T.Newline, "\n", space); Add(T.EOF, "", space); break; }
            if (src.AsSpan(pos).StartsWith("__END__") && AtLineStart()) { Add(T.Newline, "\n", space); Add(T.EOF, "", space); break; }

            char c = Cur;
            if (c == '\n')
            {
                pos++;
                Add(T.Newline, "\n", space);
                line++;
                if (pendingHeredocs.Count > 0) ReadHeredocBodies();
                continue;
            }

            int startLine = line;

            if (char.IsDigit(c)) { LexNumber(space); continue; }

            if (c == '"' || c == '`') { pos++; var parts = ReadDelimited(c, c, true); Push(c == '`' ? T.XStr : T.Str, parts, space, startLine); continue; }
            if (c == '\'') { pos++; var parts = ReadDelimited('\'', '\'', false); Push(T.Str, parts, space, startLine); continue; }

            if (c == '%' && (!LastIsValue() || (space && Peek() != ' ' && Peek() != '=')))
            {
                if (TryPercentLiteral(space)) continue;
            }

            if (c == '<' && Peek() == '<' && (Peek(2) == '~' || Peek(2) == '-' || char.IsUpper(Peek(2)) || Peek(2) == '\'' || Peek(2) == '"')
                && (!LastIsValue() || space))
            {
                if (TryHeredoc(space)) continue;
            }

            if (c == '/' && !(Last is { Type: T.Kw, Text: "def" }) && (!LastIsValue() || (space && Peek() != ' ' && Peek() != '=' && Last.Type == T.Ident)))
            {
                pos++;
                var parts = ReadDelimited('/', '/', true, regex: true);
                string flags = "";
                while (char.IsLetter(Cur)) flags += src[pos++];
                var tok = new Token { Type = T.Regex, Parts = parts, Text = flags, SpaceBefore = space, Line = startLine };
                tokens.Add(tok);
                continue;
            }

            if (c == ':' && (Peek() == '"' || Peek() == '\''))
            {
                char q = Peek();
                pos += 2;
                var parts = ReadDelimited(q, q, q == '"');
                tokens.Add(new Token { Type = T.Symbol, Parts = parts, SpaceBefore = space, Line = startLine });
                continue;
            }
            if (c == ':' && (IsIdentStart(Peek())) && Peek() != ':')
            {
                // "a ? b :c" would lex wrong here; rare in formulae.
                pos++;
                int s = pos;
                while (IsIdentChar(Cur)) pos++;
                if (Cur == '?' || Cur == '!' || Cur == '=') { if (Cur != '=' || Peek() != '>') pos++; }
                Add(T.Symbol, src[s..pos], space);
                continue;
            }
            if (c == ':' && (Peek() == '@' || Peek() == '$') && (IsIdentStart(Peek(2)) || Peek(2) == '@'))
            {
                pos++;
                int s = pos;
                while (Cur == '@' || Cur == '$') pos++;
                while (IsIdentChar(Cur)) pos++;
                Add(T.Symbol, src[s..pos], space);
                continue;
            }
            if (c == ':' && "+-*/<=>!%[`~&|^".Contains(Peek()) && !LastIsValue())
            {
                pos++;
                int s = pos;
                foreach (var op in new[] { "[]=", "[]", "<=>", "===", "==", "=~", "!~", "!=", "<=", ">=", "<<", ">>", "**", "+@", "-@", "+", "-", "*", "/", "%", "<", ">", "!", "`", "~", "&", "|", "^" })
                    if (src.AsSpan(pos).StartsWith(op)) { pos += op.Length; break; }
                Add(T.Symbol, src[s..pos], space);
                continue;
            }

            if (c == '?' && pos + 1 < src.Length && !LastIsValue() && !char.IsWhiteSpace(Peek()) && !IsIdentChar(Peek(2)))
            {
                // character literal ?a
                Push(T.Str, new List<StrPart> { new() { Text = Peek().ToString() } }, space, startLine);
                pos += 2;
                continue;
            }

            if (c == '@')
            {
                int s = pos;
                pos++;
                if (Cur == '@') pos++;
                while (IsIdentChar(Cur)) pos++;
                Add(T.IVar, src[s..pos], space);
                continue;
            }
            if (c == '$')
            {
                int s = pos;
                pos++;
                if (IsIdentStart(Cur)) while (IsIdentChar(Cur)) pos++;
                else pos++;
                Add(T.GVar, src[s..pos], space);
                continue;
            }

            if (IsIdentStart(c))
            {
                int s = pos;
                while (IsIdentChar(Cur)) pos++;
                if ((Cur == '?' || Cur == '!') && Peek() != '=' ) pos++;
                else if ((Cur == '?' || Cur == '!') && Peek() == '=' && Peek(2) == '=') pos++;
                string word = src[s..pos];
                // label "foo:" (hash key) – but not "foo::"
                if (Cur == ':' && Peek() != ':' && !(Last != null && Last.Type == T.Op && Last.Text == "?"))
                {
                    var prev = Last;
                    bool afterDot = prev != null && prev.Type == T.Op && (prev.Text == "." || prev.Text == "&.");
                    if (!afterDot)
                    {
                        pos++;
                        Add(T.Symbol, word, space);
                        tokens[^1].Text = word;
                        tokens.Add(new Token { Type = T.Op, Text = "=>", SpaceBefore = true, Line = line });
                        continue;
                    }
                }
                bool afterDot2 = Last != null && Last.Type == T.Op && (Last.Text == "." || Last.Text == "&.");
                if (!afterDot2 && Keywords.Contains(word)) Add(T.Kw, word, space);
                else if (char.IsUpper(word[0])) Add(T.Const, word, space);
                else Add(T.Ident, word, space);
                continue;
            }

            // operators
            bool matched = false;
            foreach (var op in Ops)
            {
                if (src.AsSpan(pos).StartsWith(op))
                {
                    pos += op.Length;
                    if (op == "(" || op == "[" || op == "{") parenDepth++;
                    if (op == ")" || op == "]" || op == "}") parenDepth--;
                    Add(T.Op, op, space);
                    matched = true;
                    break;
                }
            }
            if (!matched) throw new RubySyntaxError($"unexpected character '{c}'", line);
        }
        return tokens;
    }

    bool AtLineStart() => pos == 0 || src[pos - 1] == '\n' || (Cur == '\n');

    static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';
    static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    void Push(T type, List<StrPart> parts, bool space, int startLine)
    {
        foreach (var part in parts)
            if (part.Text != null && part.Text.Any(RString.IsRawByte)) part.Text = DecodeRawBytes(part.Text);
        tokens.Add(new Token { Type = type, Parts = parts, SpaceBefore = space, Line = startLine });
    }

    // Runs of \x escapes that form valid UTF-8 become those characters; the rest stay raw bytes.
    static string DecodeRawBytes(string text)
    {
        var sb = new StringBuilder(text.Length);
        var strict = new UTF8Encoding(false, true);
        for (int i = 0; i < text.Length;)
        {
            if (!RString.IsRawByte(text[i])) { sb.Append(text[i++]); continue; }
            int end = i;
            while (end < text.Length && RString.IsRawByte(text[end])) end++;
            var run = text[i..end].Select(c => (byte)(c - RString.RawByteBase)).ToArray();
            try { sb.Append(strict.GetString(run)); }
            catch (DecoderFallbackException) { sb.Append(text, i, end - i); }
            i = end;
        }
        return sb.ToString();
    }

    void LexNumber(bool space)
    {
        int s = pos;
        if (Cur == '0' && (Peek() == 'x' || Peek() == 'X'))
        {
            pos += 2;
            while (Uri.IsHexDigit(Cur) || Cur == '_') pos++;
            Add(T.Int, Convert.ToInt64(src[(s + 2)..pos].Replace("_", ""), 16).ToString(), space);
            return;
        }
        if (Cur == '0' && (Peek() == 'o' || char.IsDigit(Peek())))
        {
            pos++;
            if (Cur == 'o') pos++;
            int ds = pos;
            while (char.IsDigit(Cur) || Cur == '_') pos++;
            Add(T.Int, Convert.ToInt64(src[ds..pos].Replace("_", ""), 8).ToString(), space);
            return;
        }
        while (char.IsDigit(Cur) || (Cur == '_' && char.IsDigit(Peek()))) pos++;
        bool isFloat = false;
        if (Cur == '.' && char.IsDigit(Peek()))
        {
            isFloat = true;
            pos++;
            while (char.IsDigit(Cur) || (Cur == '_' && char.IsDigit(Peek()))) pos++;
        }
        if ((Cur == 'e' || Cur == 'E') && (char.IsDigit(Peek()) || ((Peek() == '-' || Peek() == '+') && char.IsDigit(Peek(2)))))
        {
            isFloat = true;
            pos += 2;
            while (char.IsDigit(Cur)) pos++;
        }
        Add(isFloat ? T.Float : T.Int, src[s..pos].Replace("_", ""), space);
    }

    static char Closer(char open) => open switch { '(' => ')', '[' => ']', '{' => '}', '<' => '>', _ => open };

    bool TryPercentLiteral(bool space)
    {
        int startLine = line;
        char kind = Peek();
        char open;
        int skip;
        if (char.IsLetter(kind)) { open = Peek(2); skip = 3; }
        else { open = kind; kind = 'Q'; skip = 2; }
        if (open == '\0' || char.IsLetterOrDigit(open) || char.IsWhiteSpace(open)) return false;
        if (!"wWiIqQrsx".Contains(kind)) return false;
        pos += skip;
        char close = Closer(open);
        switch (kind)
        {
            case 'w': case 'W': case 'i': case 'I':
            {
                var parts = ReadDelimited(open, close, kind == 'W' || kind == 'I');
                // Split on whitespace in literal text only; #{...} parts stay inside their word.
                var wordParts = new List<List<StrPart>>();
                var current = new List<StrPart>();
                var text = new StringBuilder();
                void FlushText() { if (text.Length > 0) { current.Add(new StrPart { Text = text.ToString() }); text.Clear(); } }
                void FlushWord() { FlushText(); if (current.Count > 0) { wordParts.Add(current); current = new List<StrPart>(); } }
                foreach (var part in parts)
                {
                    if (part.Code != null) { FlushText(); current.Add(part); continue; }
                    foreach (var ch in part.Text)
                        if (char.IsWhiteSpace(ch)) FlushWord(); else text.Append(ch);
                }
                FlushWord();
                var interpolated = wordParts.Any(w => w.Any(p => p.Code != null));
                tokens.Add(new Token
                {
                    Type = T.Words, Text = (kind == 'i' || kind == 'I') ? "i" : "w", SpaceBefore = space, Line = startLine,
                    WordList = wordParts.Select(w => string.Concat(w.Select(p => p.Text ?? ""))).ToList(),
                    WordParts = interpolated ? wordParts : null,
                });
                return true;
            }
            case 'q':
                Push(T.Str, ReadDelimited(open, close, false), space, startLine);
                return true;
            case 'Q':
                Push(T.Str, ReadDelimited(open, close, true), space, startLine);
                return true;
            case 'x':
                Push(T.XStr, ReadDelimited(open, close, true), space, startLine);
                return true;
            case 'r':
            {
                var parts = ReadDelimited(open, close, true, regex: true);
                string flags = "";
                while (char.IsLetter(Cur)) flags += src[pos++];
                tokens.Add(new Token { Type = T.Regex, Parts = parts, Text = flags, SpaceBefore = space, Line = startLine });
                return true;
            }
            case 's':
            {
                var parts = ReadDelimited(open, close, false);
                Add(T.Symbol, string.Concat(parts.Select(p => p.Text)), space);
                return true;
            }
        }
        return false;
    }

    // Reads until the closing delimiter; supports nesting for bracket pairs and #{} interpolation.
    List<StrPart> ReadDelimited(char open, char close, bool interp, bool regex = false)
    {
        var parts = new List<StrPart>();
        var sb = new StringBuilder();
        int depth = 0;
        while (true)
        {
            if (pos >= src.Length) throw new RubySyntaxError("unterminated string", line);
            char c = src[pos];
            if (c == '\\')
            {
                char n = Peek();
                pos += 2;
                if (n == '\n') { line++; if (!interp) sb.Append("\\\n"); continue; }
                if (regex)
                {
                    if (n == close && close == '/') sb.Append('/');
                    else { sb.Append('\\').Append(n); }
                    continue;
                }
                if (!interp)
                {
                    if (n == close || n == '\\' || (n == open && open != close)) sb.Append(n);
                    else sb.Append('\\').Append(n);
                    continue;
                }
                sb.Append(Unescape(n));
                continue;
            }
            if (c == '\n') line++;
            if (interp && c == '#' && Peek() == '{')
            {
                if (sb.Length > 0) { parts.Add(new StrPart { Text = sb.ToString() }); sb.Clear(); }
                pos += 2;
                int codeLine = line;
                parts.Add(new StrPart { Code = ReadInterpolation(), Line = codeLine });
                continue;
            }
            if (open != close && c == open) { depth++; sb.Append(c); pos++; continue; }
            if (c == close)
            {
                if (depth == 0) { pos++; break; }
                depth--;
                sb.Append(c);
                pos++;
                continue;
            }
            sb.Append(c);
            pos++;
        }
        if (sb.Length > 0 || parts.Count == 0) parts.Add(new StrPart { Text = sb.ToString() });
        return parts;
    }

    string Unescape(char n)
    {
        switch (n)
        {
            case 'n': return "\n";
            case 't': return "\t";
            case 'r': return "\r";
            case '0': return "\0";
            case 'e': return "\u001b";
            case 's': return " ";
            case 'a': return "\a";
            case 'b': return "\b";
            case 'u':
            {
                if (Cur == '{')
                {
                    int e = src.IndexOf('}', pos);
                    var hex = src[(pos + 1)..e];
                    pos = e + 1;
                    return string.Concat(hex.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(h => char.ConvertFromUtf32(Convert.ToInt32(h, 16))));
                }
                var h4 = src.Substring(pos, 4);
                pos += 4;
                return ((char)Convert.ToInt32(h4, 16)).ToString();
            }
            case 'x':
            {
                int s = pos;
                while (pos - s < 2 && Uri.IsHexDigit(Cur)) pos++;
                int value = Convert.ToInt32(src[s..pos], 16);
                // A high byte is raw until Push sees whether it's part of a UTF-8 sequence ("\xC3\xA9" is é).
                return (value < 0x80 ? (char)value : (char)(RString.RawByteBase + value)).ToString();
            }
            default: return n.ToString();
        }
    }

    string ReadInterpolation()
    {
        int depth = 1;
        int s = pos;
        while (pos < src.Length)
        {
            char c = src[pos];
            if (c == '"' || c == '\'')
            {
                // skip nested string
                char q = c;
                pos++;
                while (pos < src.Length && src[pos] != q)
                {
                    if (src[pos] == '\\') pos++;
                    else if (q == '"' && src[pos] == '#' && Peek() == '{')
                    {
                        pos += 2;
                        ReadInterpolation();
                        continue;
                    }
                    pos++;
                }
                pos++;
                continue;
            }
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) { var code = src[s..pos]; pos++; return code; }
            }
            else if (c == '\n') line++;
            pos++;
        }
        throw new RubySyntaxError("unterminated interpolation", line);
    }

    bool TryHeredoc(bool space)
    {
        int save = pos;
        pos += 2;
        bool squiggly = false, dash = false, raw = false;
        if (Cur == '~') { squiggly = true; pos++; }
        else if (Cur == '-') { dash = true; pos++; }
        string id;
        if (Cur == '\'' || Cur == '"')
        {
            char q = Cur;
            raw = q == '\'';
            pos++;
            int s = pos;
            while (pos < src.Length && Cur != q) pos++;
            id = src[s..pos];
            pos++;
        }
        else
        {
            if (!char.IsUpper(Cur) && Cur != '_') { pos = save; return false; }
            int s = pos;
            while (IsIdentChar(Cur)) pos++;
            id = src[s..pos];
        }
        var tok = new Token { Type = T.Str, SpaceBefore = space, Line = line, Parts = new List<StrPart>() };
        tokens.Add(tok);
        pendingHeredocs.Add((id, squiggly, dash, raw, tok));
        return true;
    }

    void ReadHeredocBodies()
    {
        foreach (var (id, squiggly, dash, raw, tok) in pendingHeredocs)
        {
            var lines = new List<string>();
            while (true)
            {
                if (pos >= src.Length) throw new RubySyntaxError($"unterminated heredoc {id}", line);
                int e = src.IndexOf('\n', pos);
                if (e < 0) e = src.Length;
                string l = src[pos..e];
                pos = Math.Min(e + 1, src.Length);
                line++;
                if ((squiggly || dash ? l.Trim() : l) == id) break;
                lines.Add(l);
            }
            if (squiggly)
            {
                int indent = int.MaxValue;
                foreach (var l in lines)
                {
                    if (l.Trim().Length == 0) continue;
                    int n = 0;
                    while (n < l.Length && (l[n] == ' ' || l[n] == '\t')) n++;
                    indent = Math.Min(indent, n);
                }
                if (indent == int.MaxValue) indent = 0;
                lines = lines.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart()).ToList();
            }
            string body = lines.Count == 0 ? "" : string.Join("\n", lines) + "\n";
            if (raw) tok.Parts.Add(new StrPart { Text = body });
            else
            {
                // Re-lex body as a double-quoted string for escapes/interpolation.
                var sub = new Lexer(body + "\u0001");
                sub.pos = 0;
                var parts = sub.ReadDelimited('\u0002', '\u0001', true);
                tok.Parts.AddRange(parts);
            }
        }
        pendingHeredocs.Clear();
    }
}
