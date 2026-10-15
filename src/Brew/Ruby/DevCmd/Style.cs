using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby.DevCmd;

// `brew style`: Homebrew runs RuboCop with its formula cops. We can't, so this file holds a
// small Ruby source model and ports of the cops that matter for formula files, plus the
// handful of Layout/Style cops that matter most. `brew audit` reuses it for its style audit.
[DevCommand("style")]
sealed class StyleCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("style")
        .Description("""
            Check formulae or files for conformance to Homebrew style guidelines.

            Lists of <file>, <tap> and <formula> may not be combined. If none are
            provided, `style` will run style checks on the whole Homebrew library,
            including core code and all formulae.
            """)
        .Switch(new[] { "--fix" }, "Fix style violations automatically using RuboCop's auto-correct feature.")
        .Switch(new[] { "--todo" }, "Add `rubocop:todo` comments for RuboCop violations that remain after auto-correction. Requires `--fix`.", dependsOn: "--fix")
        .Switch(new[] { "--display-cop-names" }, "Include the RuboCop cop name for each violation in the output.", hidden: true)
        .Switch(new[] { "--reset-cache" }, "Reset the RuboCop cache.")
        .Switch(new[] { "--changed" }, "Check files that were changed from the `main` branch.")
        .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
        .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
        .CommaArray("--only-cops", "Specify a comma-separated <cops> list to check for violations of only the listed RuboCop cops.")
        .CommaArray("--except-cops", "Specify a comma-separated <cops> list to skip checking for violations of the listed RuboCop cops.")
        .Conflicts("--formula", "--cask")
        .Conflicts("--only-cops", "--except-cops")
        .NamedArgs(new[] { "file", "tap", "formula", "cask" });

    public override void Run(ParsedArgs args)
    {
        if (args.Switch("changed") && args.Named.Count > 0)
            throw new UsageError("`--changed` and named arguments are mutually exclusive!");

        List<string> target;
        if (args.Switch("changed"))
        {
            target = ChangedRubyFiles();
            if (target.Count == 0)
            {
                Out.Opoo("No style checks are available for the changed files!");
                return;
            }
        }
        else if (args.Named.Count == 0) target = null;
        else target = StyleCheck.NamedToPaths(args.Named, args.Switch("cask"));

        var only = args.Array("only_cops")?.ToList();
        var except = only == null ? args.Array("except_cops")?.ToList() ?? new List<string> { "FormulaAuditStrict" } : null;
        if (args.Switch("reset-cache"))
        {
            var cache = Path.Combine(DevCommands.Paths.Cache, "style");
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
        DevCommands.Failed = !StyleCheck.CheckStyleAndPrint(target ?? new List<string>(), args.Switch("fix"), args.Switch("todo"), only, except);
    }

    static List<string> ChangedRubyFiles()
    {
        var git = DevCommands.Which("git") ?? throw new BrewError("`brew style --changed` must be run inside a git repository!");
        var top = DevCommands.Capture(git, new[] { "rev-parse", "--show-toplevel" });
        if (top.code != 0) throw new BrewError("`brew style --changed` must be run inside a git repository!");
        var repo = top.stdout.Trim();
        var baseRef = DevCommands.Capture(git, new[] { "-C", repo, "merge-base", "origin/HEAD", "HEAD" }).stdout.Trim();
        if (baseRef.Length == 0) baseRef = "main";
        var files = DevCommands.Capture(git, new[] { "-C", repo, "diff", "--name-only", "--no-relative", baseRef }).stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return files.Where(f => f.EndsWith(".rb") || f.EndsWith(".sh") || f.EndsWith(".yml") || f.EndsWith(".rbi"))
            .Select(f => Path.GetFullPath(Path.Combine(repo, f)))
            .Where(File.Exists).ToList();
    }
}

sealed class CopEdit
{
    public int Start, Length;
    public string Text;
}

sealed class CopOffense
{
    public string Cop, Severity = "C", Message;
    public int Line, Column, Length = 1;   // 1-based line and column
    public List<CopEdit> Fix;
    public bool Corrected;
    public bool Correctable => Fix is { Count: > 0 };
    public string Department => Cop.Split('/')[0];
}

// ---------------------------------------------------------------- source model

sealed class RbString
{
    public int Line, Col;          // of the opening delimiter (1-based line, 0-based col)
    public int Start, End;         // literal source range (heredoc: the <<~ID token)
    public int ContentStart, ContentEnd;
    public string Content;
    public bool Dynamic, Heredoc, Single, Percent, IsRegex, InInterp, Words, Symbol;
    public string Flags = "";
    public int HeredocEndLine;     // 1-based line of the terminator
}

sealed class RbComment
{
    public int Line, Col, Start;
    public string Text;
}

sealed class RbStmt
{
    public string Name, Kind;      // Kind: send, block, def, class, other
    public int Line, Col, EndLine; // 1-based lines, 0-based col
    public int Start, End;
    public bool Brace;
    public readonly List<RbStmt> Body = new();
    public RbStmt Parent;
    public bool Multiline => EndLine > Line;
    public IEnumerable<RbStmt> Descendants()
    {
        foreach (var c in Body)
        {
            yield return c;
            foreach (var d in c.Descendants()) yield return d;
        }
    }
}

sealed class RbSource
{
    public readonly string Path, Text;
    public readonly string[] Lines;      // without \n (may keep \r)
    public readonly int[] LineStart;
    public readonly char[] Mask;
    public string[] MLines;
    public readonly List<RbString> Strings = new();
    public readonly List<RbComment> Comments = new();
    public readonly Dictionary<int, RbString> StringAt = new();
    public readonly HashSet<int> HeredocLines = new();   // 1-based lines inside heredoc bodies
    public List<RbStmt> Top = new();
    public RbStmt FormulaClass;
    public int CodeEnd;                  // offset of __END__ (or Text.Length)
    readonly List<(RbString s, string id, bool indented, bool squiggly, bool raw)> pending = new();

    public RbSource(string path, string text)
    {
        Path = path;
        Text = text;
        Lines = text.Split('\n');
        LineStart = new int[Lines.Length + 1];
        for (int i = 0, off = 0; i < Lines.Length; i++) { LineStart[i] = off; off += Lines[i].Length + 1; }
        LineStart[Lines.Length] = text.Length + 1;
        Mask = text.ToCharArray();
        CodeEnd = text.Length;
        try { LexCode(0, false, 0); } catch (IndexOutOfRangeException) { } catch (ArgumentOutOfRangeException) { }
        MLines = new string(Mask).Split('\n');
        Top = ParseBody(0, Lines.Length, null);
        foreach (var s in Top.Concat(Top.SelectMany(t => t.Descendants())))
            if (s.Kind == "class" && FormulaClass == null && Regex.IsMatch(MLine(s.Line), @"^\s*class\s+\S+\s*<\s*[\w:]*Formula\b"))
                FormulaClass = s;
    }

    public string MLine(int line) => line >= 1 && line <= MLines.Length ? MLines[line - 1].TrimEnd('\r') : "";
    public string Line(int line) => line >= 1 && line <= Lines.Length ? Lines[line - 1].TrimEnd('\r') : "";
    public int LineOf(int offset)
    {
        int lo = 0, hi = Lines.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (LineStart[mid] <= offset) lo = mid; else hi = mid - 1;
        }
        return lo + 1;
    }
    public int ColOf(int offset) => offset - LineStart[LineOf(offset) - 1];
    public string Src(int start, int end) => Text.Substring(start, Math.Max(0, Math.Min(end, Text.Length) - start));
    public string Src(RbStmt s) => Src(s.Start, s.End);

    // ---- lexer ----

    void MaskRange(int from, int to)
    {
        for (int k = Math.Max(0, from); k < to && k < Mask.Length; k++)
            if (Mask[k] != '\n' && Mask[k] != '\r') Mask[k] = ' ';
    }

    bool AtLineStart(int i) => i == 0 || Text[i - 1] == '\n';

    int PrevNonSpace(int i)
    {
        for (int k = i - 1; k >= 0; k--)
        {
            if (Text[k] == '\n') return -1;
            if (Text[k] != ' ' && Text[k] != '\t') return k;
        }
        return -1;
    }

    static bool IsIdent(char c) => char.IsLetterOrDigit(c) || c == '_';

    bool PrevIsKeywordOrMethodWithSpace(int i)
    {
        int p = PrevNonSpace(i);
        if (p < 0) return true;
        char c = Text[p];
        if ("(,=[{!&|;~:?+-*<>%^".Contains(c)) return true;
        if (IsIdent(c) || c == '?' || c == '!')
        {
            bool spaced = p < i - 1;
            if (!spaced) return false;
            char next = i + 1 < Text.Length ? Text[i + 1] : ' ';
            return next != ' ' && next != '=';
        }
        return false;
    }

    int LexCode(int i, bool stopAtBrace, int interp)
    {
        int depth = 0;
        int n = Text.Length;
        while (i < n)
        {
            char c = Text[i];
            if (c == '\n') { i = OnNewline(i); continue; }
            if (AtLineStart(i))
            {
                if (string.CompareOrdinal(Text, i, "__END__", 0, 7) == 0 && (i + 7 >= n || Text[i + 7] is '\n' or '\r'))
                {
                    CodeEnd = i;
                    MaskRange(i, n);
                    return n;
                }
                if (string.CompareOrdinal(Text, i, "=begin", 0, 6) == 0)
                {
                    int e = Text.IndexOf("\n=end", i, StringComparison.Ordinal);
                    int stop = e < 0 ? n : Text.IndexOf('\n', e + 1) is var x && x < 0 ? n : x;
                    MaskRange(i, stop);
                    i = stop;
                    continue;
                }
            }
            switch (c)
            {
                case '#' when interp == 0:
                {
                    int eol = Text.IndexOf('\n', i);
                    if (eol < 0) eol = n;
                    var text = Text[i..eol].TrimEnd('\r');
                    Comments.Add(new RbComment { Line = LineOf(i), Col = ColOf(i), Start = i, Text = text });
                    MaskRange(i, eol);
                    i = eol;
                    continue;
                }
                case '{': depth++; i++; continue;
                case '}':
                    if (depth == 0 && stopAtBrace) return i;
                    depth--; i++; continue;
                case '"':
                case '`':
                    i = LexQuoted(i, i + 1, c, '\0', true, false, interp);
                    continue;
                case '\'':
                    i = LexQuoted(i, i + 1, '\'', '\0', false, false, interp);
                    continue;
                case ':' when i + 1 < n && Text[i + 1] == '"' && (i == 0 || Text[i - 1] != ':'):
                    i = LexQuoted(i, i + 2, '"', '\0', true, false, interp, symbol: true);
                    continue;
                case '%' when IsPercentLiteral(i):
                    i = LexPercent(i, interp);
                    continue;
                case '/' when PrevIsKeywordOrMethodWithSpace(i):
                    i = LexQuoted(i, i + 1, '/', '\0', true, true, interp);
                    continue;
                case '<' when i + 2 < n && Text[i + 1] == '<':
                {
                    var m = Regex.Match(Text[i..Math.Min(n, i + 80)], @"^<<([~-]?)([""'`]?)([A-Za-z_]\w*)\2");
                    int p = PrevNonSpace(i);
                    if (m.Success && (p < 0 || p < i - 1 || !IsIdent(Text[p])))
                    {
                        var s = new RbString
                        {
                            Line = LineOf(i), Col = ColOf(i), Start = i, End = i + m.Length, Heredoc = true,
                            InInterp = interp > 0, Single = m.Groups[2].Value == "'",
                        };
                        Add(s);
                        pending.Add((s, m.Groups[3].Value, m.Groups[1].Value.Length > 0, m.Groups[1].Value == "~", m.Groups[2].Value == "'"));
                        i += m.Length;
                        continue;
                    }
                    i += 2;
                    continue;
                }
                case '?' when i + 2 < n && (i == 0 || !IsIdent(Text[i - 1]) && Text[i - 1] != ')' && Text[i - 1] != ']') && Text[i + 1] != ' ' && !IsIdent(Text[i + 2]):
                    i += Text[i + 1] == '\\' ? 3 : 2;
                    continue;
                default:
                    i++;
                    continue;
            }
        }
        return i;
    }

    int OnNewline(int i)
    {
        if (pending.Count == 0) return i + 1;
        int pos = i + 1;
        var todo = pending.ToList();
        pending.Clear();
        foreach (var (s, id, indented, squiggly, raw) in todo)
        {
            int bodyStart = pos;
            int termLine = -1;
            while (pos <= Text.Length)
            {
                int eol = Text.IndexOf('\n', pos);
                if (eol < 0) eol = Text.Length;
                var line = Text[pos..eol].TrimEnd('\r');
                if ((indented ? line.Trim() : line) == id)
                {
                    termLine = LineOf(pos);
                    var body = Text[bodyStart..pos];
                    if (squiggly)
                    {
                        var ls = body.Split('\n');
                        int min = ls.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart(' ', '\t').Length).DefaultIfEmpty(0).Min();
                        body = string.Join("\n", ls.Select(l => l.Length >= min ? l[min..] : l.TrimStart()));
                    }
                    s.Content = body;
                    s.Dynamic = !raw && body.Contains("#{");
                    s.ContentStart = bodyStart;
                    s.ContentEnd = pos;
                    s.HeredocEndLine = termLine;
                    for (int l = LineOf(bodyStart); l <= termLine; l++) HeredocLines.Add(l);
                    MaskRange(bodyStart, eol);
                    pos = eol + 1;
                    break;
                }
                if (eol >= Text.Length) { pos = Text.Length + 1; break; }
                pos = eol + 1;
            }
            if (termLine < 0)
            {
                s.Content = Text[bodyStart..];
                s.ContentStart = bodyStart;
                s.ContentEnd = Text.Length;
                s.HeredocEndLine = Lines.Length;
                MaskRange(bodyStart, Text.Length);
            }
        }
        return Math.Min(pos, Text.Length);
    }

    bool IsPercentLiteral(int i)
    {
        if (i + 1 >= Text.Length) return false;
        char t = Text[i + 1];
        char d = t is 'w' or 'W' or 'i' or 'I' or 'q' or 'Q' or 'r' or 's' && i + 2 < Text.Length ? Text[i + 2] : t;
        if (!"([{<|!/^".Contains(d)) return false;
        int p = PrevNonSpace(i);
        if (p < 0) return true;
        char c = Text[p];
        if (IsIdent(c) || c == ')' || c == ']') return p < i - 1 && i + 2 < Text.Length && Text[i + 1] != ' ' && Text[i + 2] != ' ';
        return true;
    }

    int LexPercent(int i, int interp)
    {
        char t = Text[i + 1];
        bool hasType = char.IsLetter(t);
        int d = hasType ? i + 2 : i + 1;
        char open = Text[d];
        char close = open switch { '(' => ')', '[' => ']', '{' => '}', '<' => '>', _ => open };
        bool interpolates = !hasType || t is 'W' or 'I' or 'Q' or 'r';
        int end = LexQuoted(i, d + 1, close, close == open ? '\0' : open, interpolates, t == 'r', interp, percent: true);
        var s = StringAt[i];
        s.Words = t is 'w' or 'W' or 'i' or 'I';
        return end;
    }

    void Add(RbString s)
    {
        Strings.Add(s);
        StringAt[s.Start] = s;
    }

    int LexQuoted(int start, int i, char close, char open, bool interpolates, bool regex, int interp, bool percent = false, bool symbol = false)
    {
        var s = new RbString
        {
            Line = LineOf(start), Col = ColOf(start), Start = start, ContentStart = i, IsRegex = regex,
            Single = close == '\'' && !percent, Percent = percent, InInterp = interp > 0, Symbol = symbol,
        };
        Add(s);
        var sb = new StringBuilder();
        int nest = 0, n = Text.Length;
        while (i < n)
        {
            char c = Text[i];
            if (c == '\\' && i + 1 < n)
            {
                char e = Text[i + 1];
                if (regex) sb.Append('\\').Append(e);
                else if (!interpolates)
                {
                    if (e == close || e == '\\' || (open != '\0' && e == open)) sb.Append(e);
                    else sb.Append('\\').Append(e);
                }
                else sb.Append(e switch { 'n' => "\n", 't' => "\t", 's' => " ", '0' => "\0", 'e' => "\u001b", 'r' => "\r", '\n' => "", _ => e.ToString() });
                i += 2;
                continue;
            }
            if (interpolates && c == '#' && i + 1 < n && Text[i + 1] == '{')
            {
                int closeIdx = LexCode(i + 2, true, interp + 1);
                if (closeIdx >= n) closeIdx = n - 1;
                sb.Append(Text, i, closeIdx + 1 - i);
                s.Dynamic = true;
                i = closeIdx + 1;
                continue;
            }
            if (open != '\0' && c == open) nest++;
            else if (c == close)
            {
                if (nest == 0) break;
                nest--;
            }
            sb.Append(c);
            i++;
        }
        s.Content = sb.ToString();
        s.ContentEnd = Math.Min(i, n);
        int endPos = Math.Min(i + 1, n);
        if (regex)
            while (endPos < n && char.IsLetter(Text[endPos])) { s.Flags += Text[endPos]; endPos++; }
        s.End = endPos;
        MaskRange(s.ContentStart, s.ContentEnd);
        return endPos;
    }

    // ---- statements (indentation based; formula files are RuboCop-formatted) ----

    static readonly HashSet<string> Keywords = new()
    {
        "if", "unless", "while", "until", "case", "when", "else", "elsif", "end", "begin", "rescue", "ensure",
        "return", "next", "break", "yield", "do", "then", "and", "or", "not", "def", "class", "module", "self",
        "nil", "true", "false", "super", "for", "in", "redo", "retry", "alias", "undef", "defined?",
    };

    static int Indent(string l) => l.Length - l.TrimStart(' ').Length;

    List<RbStmt> ParseBody(int from, int to, RbStmt parent)
    {
        var list = new List<RbStmt>();
        int i = from;
        while (i < to)
        {
            var code = MLines[i].TrimEnd('\r', ' ', '\t');
            if (code.Trim().Length == 0) { i++; continue; }
            int ind = Indent(code);
            var text = code[ind..];
            var s = new RbStmt { Line = i + 1, Col = ind, Parent = parent, Start = LineStart[i] + ind };
            Match m;
            if ((m = Regex.Match(text, @"^def\s+(?:self\.)?([A-Za-z_]\w*[?!=]?)")).Success) { s.Kind = "def"; s.Name = m.Groups[1].Value; }
            else if ((m = Regex.Match(text, @"^class\s+([\w:]+)")).Success) { s.Kind = "class"; s.Name = m.Groups[1].Value; }
            else if ((m = Regex.Match(text, @"^([a-z_][A-Za-z0-9_]*[?!]?)(?=$|[\s(])")).Success && !Keywords.Contains(m.Groups[1].Value)
                     && !Regex.IsMatch(text[m.Length..], @"^\s*(?:[-+*/|&]{0,2}=(?![=~>])|<<\s*\S)"))
            {
                s.Kind = "send";
                s.Name = m.Groups[1].Value;
            }
            else s.Kind = "other";

            bool endless = s.Kind == "def" && (Regex.IsMatch(text, @"^def\s+[\w.?!]+(\([^)]*\))?\s*=[^=]") || Regex.IsMatch(text, @";\s*end$"));
            bool opensDo = Regex.IsMatch(text, @"(^|[\s)\]])do(\s*\|[^|]*\|)?$");
            bool opensBrace = Regex.IsMatch(text, @"\{(\s*\|[^|]*\|)?$");
            bool opensKw = !endless && Regex.IsMatch(text, @"^(def|class|module|if|unless|case|while|until|begin|for)\b") && !Regex.IsMatch(text, @"\bend$")
                           || Regex.IsMatch(text, @"=\s*(if|unless|case|begin)\b") && !Regex.IsMatch(text, @"\bend$");
            if (opensDo || opensBrace || opensKw)
            {
                int j = -1;
                for (int k = i + 1; k < to; k++)
                {
                    var kl = MLines[k].TrimEnd('\r', ' ', '\t');
                    if (kl.Trim().Length == 0 || Indent(kl) != ind) continue;
                    var kt = kl.Trim();
                    if (opensBrace && !opensDo && !opensKw ? kt.StartsWith('}') : Regex.IsMatch(kt, @"^end\b")) { j = k; break; }
                    if (Indent(kl) <= ind && !Regex.IsMatch(kt, @"^(else|elsif|when|in|rescue|ensure|\)|\])\b") && !kt.StartsWith(')') && !kt.StartsWith(']')) break;
                }
                if (j >= 0)
                {
                    if (s.Kind == "send" && (opensDo || opensBrace)) s.Kind = "block";
                    s.Brace = opensBrace && !opensDo;
                    s.Body.AddRange(ParseBody(i + 1, j, s));
                    s.EndLine = j + 1;
                    s.End = LineStart[j] + Lines[j].TrimEnd('\r').Length;
                    list.Add(s);
                    i = j + 1;
                    continue;
                }
            }
            int last = i;
            while (true)
            {
                int k = last + 1;
                while (k < to && MLines[k].Trim().Length == 0) k++;
                if (k >= to) break;
                var prev = MLines[last].TrimEnd('\r', ' ', '\t');
                var next = MLines[k].TrimEnd('\r', ' ', '\t');
                var nt = next.Trim();
                if (Indent(next) > ind || Regex.IsMatch(prev, @"[,\\(\[{|&+\-*.]$") || prev.EndsWith(" do") ||
                    nt.StartsWith('.') || nt.StartsWith("&.") || (Indent(next) == ind && (nt.StartsWith(')') || nt.StartsWith(']') || nt.StartsWith('}'))))
                    last = k;
                else break;
            }
            foreach (var hs in Strings)
                if (hs.Heredoc && hs.Line >= i + 1 && hs.Line <= last + 1 && hs.HeredocEndLine > last + 1)
                    last = hs.HeredocEndLine - 1;
            s.EndLine = last + 1;
            s.End = LineStart[last] + Lines[last].TrimEnd('\r').Length;
            list.Add(s);
            i = last + 1;
        }
        return list;
    }

    // ---- helpers for cops ----

    // Position just after the method name (and an opening paren).
    public int ArgStart(RbStmt s, out bool parens)
    {
        int p = s.Start + (s.Name?.Length ?? 0);
        while (p < s.End && Text[p] is ' ' or '\t') p++;
        parens = p < s.End && Text[p] == '(';
        if (parens) p++;
        while (p < s.End && char.IsWhiteSpace(Text[p])) p++;
        return p;
    }

    public RbString FirstStringArg(RbStmt s)
    {
        if (s == null) return null;
        int p = ArgStart(s, out _);
        return StringAt.TryGetValue(p, out var str) && !str.Symbol && !str.IsRegex && !str.Words ? str : null;
    }

    // Masked text of the arguments of a call (the head part, up to `do`/end of statement).
    public string ArgsMasked(RbStmt s)
    {
        int p = ArgStart(s, out _);
        int end = s.Kind is "block" or "def" or "class" ? LineStart[s.Line - 1] + MLine(s.Line).Length : s.End;
        if (end < p) return "";
        var t = new string(Mask, p, Math.Max(0, Math.Min(end, Mask.Length) - p));
        if (s.Kind == "block") t = Regex.Replace(t, @"\s*(do(\s*\|[^|]*\|)?|\{(\s*\|[^|]*\|)?)\s*$", "");
        return t;
    }

    public string ArgsSource(RbStmt s)
    {
        int p = ArgStart(s, out _);
        int end = s.Kind is "block" or "def" or "class" ? LineStart[s.Line - 1] + Line(s.Line).Length : s.End;
        var t = Src(p, end);
        if (s.Kind == "block") t = Regex.Replace(t, @"\s*(do(\s*\|[^|]*\|)?|\{(\s*\|[^|]*\|)?)\s*$", "");
        return t;
    }

    public IEnumerable<RbString> StringsIn(int start, int end) => Strings.Where(x => x.Start >= start && x.Start < end && !x.InInterp);
}

// ---------------------------------------------------------------- cops

sealed class CopContext
{
    public RbSource Src;
    public string FormulaName;
    public bool CoreTap;
    public readonly List<CopOffense> Offenses = new();
    readonly HashSet<(string, int, int, int)> seen = new();

    public void Add(string cop, int line, int col0, int len, string msg, List<CopEdit> fix = null, string sev = "C")
    {
        if (!seen.Add((cop, line, col0, len))) return;
        Offenses.Add(new CopOffense { Cop = cop, Line = line, Column = col0 + 1, Length = Math.Max(1, len), Message = msg, Fix = fix, Severity = sev });
    }

    public void At(string cop, RbStmt s, string msg, List<CopEdit> fix = null)
    {
        int len = s.EndLine == s.Line ? s.End - s.Start : Src.Line(s.Line).Length - s.Col;
        Add(cop, s.Line, s.Col, len, msg, fix);
    }

    public void At(string cop, RbString s, string msg, List<CopEdit> fix = null)
    {
        int len = Src.LineOf(s.End) == s.Line ? s.End - s.Start : Src.Line(s.Line).Length - s.Col;
        Add(cop, s.Line, s.Col, len, msg, fix);
    }

    // regex_match_group: the matched range inside the string.
    public void AtMatch(string cop, RbString s, Match m, string msg, List<CopEdit> fix = null)
    {
        int abs = s.ContentStart + m.Index;
        Add(cop, Src.LineOf(abs), Src.ColOf(abs), m.Length, msg, fix);
    }

    public static List<CopEdit> Replace(int start, int end, string text) => new() { new CopEdit { Start = start, Length = end - start, Text = text } };
}

static class FormulaCops
{
    const RegexOptions ML = RegexOptions.Multiline;
    const RegexOptions MLI = RegexOptions.Multiline | RegexOptions.IgnoreCase;

    static readonly string[] OnSystemMethods =
        new[] { "intel", "arm", "macos", "linux", "system", "golden_gate", "tahoe", "sequoia", "sonoma", "ventura", "monterey", "big_sur" }
            .Select(m => "on_" + m).ToArray();

    static readonly (string name, string type)[][] Precedence = BuildPrecedence();

    static (string, string)[][] BuildPrecedence()
    {
        var l = new List<(string, string)[]>();
        void A(params (string, string)[] g) => l.Add(g);
        foreach (var n in new[] { "include", "desc", "homepage", "url", "mirror", "version", "sha256", "license", "revision", "version_scheme", "compatibility_version", "head" })
            A((n, "send"));
        A(("stable", "block"));
        A(("livecheck", "block"));
        A(("no_autobump!", "send"));
        A(("bottle", "block"));
        A(("pour_bottle?", "block"));
        A(("head", "block"));
        foreach (var n in new[] { "bottle", "keg_only", "option", "deprecated_option", "deprecate!", "disable!", "depends_on", "uses_from_macos" })
            A((n, "send"));
        A(("on_macos", "block"));
        foreach (var n in new[] { "golden_gate", "tahoe", "sequoia", "sonoma", "ventura", "monterey", "big_sur" }) A(("on_" + n, "block"));
        A(("on_system", "block"));
        A(("on_linux", "block"));
        A(("on_arm", "block"));
        A(("on_intel", "block"));
        foreach (var n in new[] { "conflicts_with", "preserve_rpath", "skip_clean", "cxxstdlib_check", "link_overwrite" }) A((n, "send"));
        A(("fails_with", "send"), ("fails_with", "block"));
        A(("pypi_packages", "send"));
        A(("resource", "block"));
        A(("patch", "send"), ("patch", "block"));
        A(("needs", "send"));
        A(("allow_network_access!", "send"));
        A(("deny_network_access!", "send"));
        A(("install", "def"));
        A(("post_install_steps", "block"));
        A(("post_install", "def"));
        A(("caveats", "def"));
        A(("plist_options", "send"), ("plist", "def"));
        A(("test", "block"));
        return l.ToArray();
    }

    public static readonly string[] AllCops =
    {
        "FormulaAudit/AssertStatements", "FormulaAudit/Checksum", "FormulaAudit/ChecksumCase", "FormulaAudit/ClassInheritance",
        "FormulaAudit/ClassName", "FormulaAudit/Comments", "FormulaAudit/ComponentsOrder", "FormulaAudit/DeprecateDisableDate",
        "FormulaAudit/DeprecateDisableReason", "FormulaAudit/Desc", "FormulaAudit/GitUrls", "FormulaAudit/Homepage",
        "FormulaAudit/HttpUrls", "FormulaAudit/LicenseArrays", "FormulaAudit/Licenses", "FormulaAudit/LivecheckRegexCaseInsensitive",
        "FormulaAudit/LivecheckRegexExtension", "FormulaAudit/LivecheckRegexIfPageMatch", "FormulaAudit/LivecheckRegexParentheses",
        "FormulaAudit/LivecheckSkip", "FormulaAudit/LivecheckUrlProvided", "FormulaAudit/LivecheckUrlSymbol", "FormulaAudit/Miscellaneous",
        "FormulaAudit/PyPiUrls", "FormulaAudit/Test", "FormulaAudit/Text", "FormulaAudit/Urls", "FormulaAudit/Version",
        "FormulaAuditStrict/GitUrls", "FormulaAuditStrict/Text",
        "Layout/EmptyLines", "Layout/EndOfLine", "Layout/IndentationStyle", "Layout/IndentationWidth", "Layout/LineLength",
        "Layout/TrailingEmptyLines", "Layout/TrailingWhitespace", "Style/StringLiterals", "Style/StringLiteralsInInterpolation",
    };

    public static void Run(CopContext c)
    {
        LayoutCops(c);
        var src = c.Src;
        var cls = src.FormulaClass;
        if (cls == null) return;
        var body = cls.Body;
        ClassCops(c, cls);
        Comments(c);
        Desc(c, cls, body);
        Homepage(c, cls, body);
        ComponentsOrder(c, body);
        Checksum(c, cls);
        Version(c, body);
        Urls(c, cls);
        DeprecateDisable(c, body);
        Livecheck(c, body);
        Test(c, cls, body);
        Licenses(c, body);
        Asserts(c, cls);
        Misc(c, cls, body);
        StrictText(c, cls);
    }

    static RbStmt Call(List<RbStmt> body, string name) => body.FirstOrDefault(s => s.Kind == "send" && s.Name == name);
    static RbStmt Block(List<RbStmt> body, string name) => body.FirstOrDefault(s => s.Kind == "block" && s.Name == name);
    // find_every_method_call_by_name: sends anywhere below (block calls count too).
    static IEnumerable<RbStmt> Every(RbStmt root, string name) => root.Descendants().Where(s => s.Name == name && s.Kind is "send" or "block");

    // ---- Layout / Style ----

    static readonly string[] LineLengthAllowed =
    {
        "#: ", " url \"", " mirror \"", " plist_options ", " executable: \"", " font \"", " homepage \"", " name \"", " pkg \"",
        " pkgutil: \"", "    sha256 cellar: ", "    sha256  ", "#{language}", "#{version.", " \"/Library/Application Support/",
        "\"/Library/Caches/", "\"/Library/PreferencePanes/", " \"~/Library/Application Support/", "\"~/Library/Caches/",
        "\"~/Library/Containers", "\"~/Application Support",
    };

    static void LayoutCops(CopContext c)
    {
        var src = c.Src;
        var text = src.Text;
        int codeLines = src.LineOf(src.CodeEnd);
        bool crReported = false;
        for (int ln = 1; ln <= src.Lines.Length; ln++)
        {
            var raw = src.Lines[ln - 1];
            if (ln == src.Lines.Length && raw.Length == 0) break;
            if (src.LineStart[ln - 1] >= src.CodeEnd) break;
            if (!crReported && raw.EndsWith('\r'))
            {
                crReported = true;
                c.Add("Layout/EndOfLine", ln, 0, raw.Length + 1, "Carriage return character detected.");
            }
            var line = raw.TrimEnd('\r');
            int ls = src.LineStart[ln - 1];
            var trimmed = line.TrimEnd(' ', '\t', '\f', '\v');
            if (trimmed.Length < line.Length)
                c.Add("Layout/TrailingWhitespace", ln, trimmed.Length, line.Length - trimmed.Length, "Trailing whitespace detected.",
                    CopContext.Replace(ls + trimmed.Length, ls + line.Length, ""));
            if (!src.HeredocLines.Contains(ln))
            {
                int lead = 0;
                while (lead < line.Length && line[lead] is ' ' or '\t') lead++;
                int tab = line.IndexOf('\t');
                if (tab >= 0 && tab < lead)
                {
                    var ws = line[..lead];
                    int first = ws.IndexOf('\t'), lastTab = ws.LastIndexOf('\t');
                    c.Add("Layout/IndentationStyle", ln, first, lastTab - first + 1, "Tab detected in indentation.",
                        CopContext.Replace(ls, ls + lead, ws.Replace("\t", "  ")));
                }
            }
            if (line.Length > 118)
            {
                bool allowed = LineLengthAllowed.Any(p => line.Contains(p));
                if (!allowed)
                {
                    var uri = Regex.Matches(line, @"(https?|ftp|file)://[^\s""')>]*").LastOrDefault();
                    if (uri != null && uri.Index <= 118 && line[(uri.Index + uri.Length)..].Trim().TrimEnd('"', '\'', ')', ',', '>').Length == 0)
                        allowed = true;
                }
                if (!allowed)
                    c.Add("Layout/LineLength", ln, 118, line.Length - 118, $"Line is too long. [{line.Length}/118]");
            }
            if (ln >= 2 && line.Trim().Length == 0 && src.Line(ln - 1).Trim().Length == 0 && ln <= codeLines
                && !src.HeredocLines.Contains(ln) && !src.HeredocLines.Contains(ln - 1) && ln < src.Lines.Length && InCode(src, ln))
                c.Add("Layout/EmptyLines", ln, 0, 1, "Extra blank line detected.",
                    CopContext.Replace(ls - 1, ls + raw.Length, ""));
        }

        // Layout/TrailingEmptyLines
        if (src.CodeEnd == text.Length && text.Length > 0)
        {
            if (!text.EndsWith('\n'))
            {
                int ln = src.Lines.Length;
                c.Add("Layout/TrailingEmptyLines", ln, src.Line(ln).Length, 1, "Final newline missing.",
                    new List<CopEdit> { new() { Start = text.Length, Length = 0, Text = "\n" } });
            }
            else
            {
                var stripped = text.TrimEnd('\n', '\r', ' ', '\t');
                int blank = text[stripped.Length..].Count(ch => ch == '\n') - 1;
                if (blank > 0 && stripped.Length > 0)
                {
                    int ln = src.LineOf(stripped.Length) + 1;
                    c.Add("Layout/TrailingEmptyLines", ln, 0, 1, $"{blank} trailing blank lines detected.",
                        CopContext.Replace(stripped.Length, text.Length, "\n"));
                }
            }
        }

        // Style/StringLiterals
        foreach (var s in src.Strings)
        {
            if (!s.Single || s.Heredoc || s.Start >= src.CodeEnd) continue;
            var inner = s.Content;
            var rawInner = src.Src(s.ContentStart, s.ContentEnd);
            if (rawInner.Contains('"') || rawInner.Contains('\\') || Regex.IsMatch(rawInner, @"#[@{$]") || rawInner.Contains('\n')) continue;
            var cop = s.InInterp ? "Style/StringLiteralsInInterpolation" : "Style/StringLiterals";
            var msg = s.InInterp ? "Prefer double-quoted strings inside interpolations."
                : "Prefer double-quoted strings unless you need single quotes to avoid extra backslashes for escaping.";
            c.At(cop, s, msg, CopContext.Replace(s.Start, s.End, "\"" + inner + "\""));
        }

        // Layout/IndentationWidth
        void Walk(List<RbStmt> list)
        {
            foreach (var s in list)
            {
                if (s.Body.Count > 0 && s.Kind is "block" or "def" or "class" or "other" or "send")
                {
                    var first = s.Body[0];
                    int width = first.Col - s.Col;
                    if (width != 2 && first.Line > s.Line && !src.HeredocLines.Contains(first.Line))
                    {
                        int ls = src.LineStart[first.Line - 1];
                        var fix = new List<CopEdit>();
                        int delta = 2 - width;
                        for (int ln = first.Line; ln < s.EndLine; ln++)
                        {
                            if (src.HeredocLines.Contains(ln)) continue;
                            var l = src.Line(ln);
                            if (l.Trim().Length == 0) continue;
                            int lead = l.Length - l.TrimStart(' ').Length;
                            int st = src.LineStart[ln - 1];
                            if (delta > 0) fix.Add(new CopEdit { Start = st, Length = 0, Text = new string(' ', delta) });
                            else fix.Add(new CopEdit { Start = st, Length = Math.Min(-delta, lead), Text = "" });
                        }
                        c.Add("Layout/IndentationWidth", first.Line, Math.Min(s.Col, first.Col), Math.Abs(width),
                            $"Use 2 (not {width}) spaces for indentation.", fix);
                    }
                }
                Walk(s.Body);
            }
        }
        Walk(src.Top);
    }

    static bool InCode(RbSource src, int ln)
    {
        // A blank line inside a multi-line string literal isn't a Layout/EmptyLines offense.
        int off = src.LineStart[ln - 1];
        return !src.Strings.Any(s => !s.Heredoc && s.ContentStart <= off && s.ContentEnd > off);
    }

    // ---- FormulaAudit ----

    static void ClassCops(CopContext c, RbStmt cls)
    {
        var line = c.Src.Line(cls.Line);
        var m = Regex.Match(line, @"^(\s*class\s+)([\w:]+)(\s*)<(\s*)([\w:]+)");
        if (!m.Success) return;
        int parentCol = m.Groups[5].Index;
        var parent = m.Groups[5].Value;
        if (m.Groups[3].Value != " " || m.Groups[4].Value != " ")
        {
            var fname = c.FormulaName.Length == 0 ? "" : char.ToUpperInvariant(c.FormulaName[0]) + c.FormulaName[1..].ToLowerInvariant();
            c.Add("FormulaAudit/ClassInheritance", cls.Line, parentCol, parent.Length, $"Use a space in class inheritance: class {fname} < {parent}");
        }
        if (parent is "GithubGistFormula" or "ScriptFileFormula" or "AmazonWebServicesFormula")
        {
            int abs = c.Src.LineStart[cls.Line - 1] + parentCol;
            c.Add("FormulaAudit/ClassName", cls.Line, parentCol, parent.Length, $"`{parent}` is deprecated, use `Formula` instead",
                CopContext.Replace(abs, abs + parent.Length, "Formula"));
        }
    }

    static readonly string[] TemplateComments =
    {
        "# PLEASE REMOVE", "# Documentation:", "# if this fails, try separate make/make install steps", "# The URL of the archive",
        "## Naming --", "# if your formula fails when building in parallel", "# Remove unrecognized options if warned by configure", "# system \"cmake",
    };

    static void Comments(CopContext c)
    {
        foreach (var cm in c.Src.Comments)
        {
            if (TemplateComments.Any(t => cm.Text.Contains(t)))
                c.Add("FormulaAudit/Comments", cm.Line, cm.Col, cm.Text.Length, "Please remove default template comments");
        }
        foreach (var cm in c.Src.Comments)
        {
            var m = Regex.Match(cm.Text, @"#\s*depends_on\s+(.+)\s*$", ML);
            if (m.Success) c.Add("FormulaAudit/Comments", cm.Line, cm.Col, cm.Text.Length, $"Commented-out dependency {m.Groups[1].Value}");
        }
        if (!c.CoreTap) return;
        foreach (var cm in c.Src.Comments)
        {
            var m = Regex.Match(cm.Text, @"#\s*(cite(?=\s*\w+:)|doi(?=\s*['""])|tag(?=\s*['""]))");
            if (m.Success) c.Add("FormulaAudit/Comments", cm.Line, cm.Col, cm.Text.Length, $"Formulae in homebrew/core should not use `{m.Groups[1].Value}` comments");
        }
    }

    static readonly string[] ValidLowercaseWords = { "iOS", "iPhone", "macOS" };

    static void Desc(CopContext c, RbStmt cls, List<RbStmt> body)
    {
        const string cop = "FormulaAudit/Desc";
        var desc = Call(body, "desc");
        if (desc == null)
        {
            c.At(cop, cls, "Formula should have a `desc` (description).");
            return;
        }
        var s = c.Src.FirstStringArg(desc);
        if (s == null) return;
        var content = s.Content;
        if (content.Length == 0)
        {
            c.At(cop, desc, "The `desc` (description) should not be an empty string.");
            return;
        }
        var name = c.FormulaName;
        bool matchedAny = false;
        void DescProblem(Match m, string msg)
        {
            matchedAny = true;
            c.AtMatch(cop, s, m, msg, DescFix(c, s, name));
        }
        Match mm;
        if ((mm = Regex.Match(content, @"^\s+", ML)).Success) DescProblem(mm, "Description shouldn't have leading spaces.");
        if ((mm = Regex.Match(content, @"\s+$", ML)).Success) DescProblem(mm, "Description shouldn't have trailing spaces.");
        if ((mm = Regex.Match(content, "command ?line", MLI)).Success)
            DescProblem(mm, $"Description should use \"{mm.Value[0]}ommand-line\" instead of \"{mm.Value}\".");
        if ((mm = Regex.Match(content, @"^(?:the|an?)(?=\s)", MLI)).Success) DescProblem(mm, "Description shouldn't start with an article.");
        var firstWord = content.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!ValidLowercaseWords.Contains(firstWord) && (mm = Regex.Match(content, "^[a-z]", ML)).Success)
            DescProblem(mm, "Description should start with a capital letter.");
        if (!string.IsNullOrEmpty(name))
        {
            var pat = "^" + string.Join(@"[\s\-]?", name.Replace("-", "").Select(ch => Regex.Escape(ch.ToString()))) + @"\b";
            if ((mm = Regex.Match(content, pat, MLI)).Success) DescProblem(mm, "Description shouldn't start with the formula name.");
        }
        if ((mm = Regex.Match(content, @"\.$", ML)).Success && !content.EndsWith("etc.")) DescProblem(mm, "Description shouldn't end with a full stop.");
        if ((mm = Regex.Match(content, @"\p{So}")).Success) DescProblem(mm, "Description shouldn't contain Unicode emojis or symbols.");
        var len = new StringInfo(content).LengthInTextElements;
        if (len > 80)
        {
            var msg = $"Description is too long. It should be less than 80 characters. The current length is {len}.";
            if (matchedAny) c.At(cop, s, msg); else c.At(cop, desc, msg);
        }
    }

    static List<CopEdit> DescFix(CopContext c, RbString s, string name)
    {
        var source = c.Src.Src(s.Start, s.End);
        var md = Regex.Match(source, @"\A(?<quote>[""'])(?<correction>.*)(?:\k<quote>)\z", RegexOptions.Singleline);
        if (!md.Success || source.Contains('\n')) return null;
        var orig = md.Groups["correction"].Value;
        var quote = md.Groups["quote"].Value;
        var corr = orig;
        corr = Regex.Replace(corr, @"^\s+", "", ML);
        corr = Regex.Replace(corr, @"\s+$", "", ML);
        corr = new Regex(@"^(?:the|an?)\s+", MLI).Replace(corr, "", 1);
        var fw = corr.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!ValidLowercaseWords.Contains(fw) && corr.Length > 0) corr = char.ToUpperInvariant(corr[0]) + corr[1..];
        corr = Regex.Replace(corr, "ommand ?line", "ommand-line", RegexOptions.IgnoreCase);
        if (!string.IsNullOrEmpty(name)) corr = Regex.Replace(corr, $"(^|[^a-z]){Regex.Escape(name)}([^a-z]|$)", "$1$2", MLI);
        corr = Regex.Replace(corr, @"\s?\p{So}", "");
        corr = Regex.Replace(corr, @"^\s+", "", ML);
        corr = Regex.Replace(corr, @"\s+$", "", ML);
        corr = Regex.Replace(corr, @"\.$", "", ML);
        if (corr == orig) return null;
        return CopContext.Replace(s.Start, s.End, quote + corr + quote);
    }

    static void Homepage(CopContext c, RbStmt cls, List<RbStmt> body)
    {
        const string cop = "FormulaAudit/Homepage";
        var hp = Call(body, "homepage");
        if (hp == null)
        {
            c.At(cop, cls, "Formula should have a homepage.");
            return;
        }
        var p = c.Src.FirstStringArg(hp);
        if (p == null) return;
        var content = p.Content;
        if (content.Length == 0) c.At(cop, hp, "Formula should have a `homepage`.");
        if (!Regex.IsMatch(content, "^https?://", ML)) c.At(cop, p, "The `homepage` should start with http or https.");
        List<CopEdit> Fix(string v) => CopContext.Replace(p.Start, p.End, $"\"{v}\"");
        Match m;
        if (Regex.IsMatch(content, @"^http://(?:(?:www|nice|libopenraw|liboil|telepathy|xorg)\.)?freedesktop\.org/(?:wiki/)?", ML))
            c.At(cop, p, content.Contains("Software")
                ? "Freedesktop homepages should be styled: https://wiki.freedesktop.org/www/Software/project_name"
                : "Freedesktop homepages should be styled: https://wiki.freedesktop.org/project_name");
        else if (Regex.IsMatch(content, @"^https?://code\.google\.com/p/[^/]+[^/]$", ML))
            c.At(cop, p, "Google Code homepages should end with a slash", Fix(content + "/"));
        else if ((m = Regex.Match(content, @"^http://([^/]*)\.(?:sf|sourceforge)\.net(?:/|$)", ML)).Success)
        {
            var fixd = $"https://{m.Groups[1].Value}.sourceforge.io/";
            c.At(cop, p, $"SourceForge homepages should be: {fixd}", Fix(fixd));
        }
        else if (content.Contains("readthedocs.org"))
        {
            var fixd = new Regex(Regex.Escape("readthedocs.org")).Replace(content, "readthedocs.io", 1);
            c.At(cop, p, $"Readthedocs homepages should be: {fixd}", Fix(fixd));
        }
        else if (Regex.IsMatch(content, @"^https://github\.com.*\.git$", ML))
            c.At(cop, p, "GitHub homepages should not end with .git", Fix(content[..^4]));
        else if (new[]
                 {
                     @"^http://github\.com/", @"^http://[^/]*\.github\.io/", @"^http://savannah\.nongnu\.org/", @"^http://[^/]*\.sourceforge\.io/",
                     @"^http://(?:(?:build|cloud|developer|download|extensions|git|glade|help|library|live|nagios|news|people|projects|rt|static|wiki|www)\.)?gnome\.org",
                     @"^http://[^/]*\.apache\.org", @"^http://packages\.debian\.org", @"^http://wiki\.freedesktop\.org/", @"^http://(?:www\.)?gnupg\.org/",
                     @"^http://ietf\.org", @"^http://[^/.]+\.ietf\.org", @"^http://[^/.]+\.tools\.ietf\.org", @"^http://www\.gnu\.org/",
                     @"^http://code\.google\.com/", @"^http://bitbucket\.org/", @"^http://(?:[^/]*\.)?archive\.org",
                 }.Any(r => Regex.IsMatch(content, r, ML)))
            c.At(cop, p, $"Please use https:// for {content}", Fix(new Regex("http").Replace(content, "https", 1)));
    }

    static string Format(RbStmt s) => s.Name;

    static (List<List<RbStmt>> present, (RbStmt, RbStmt)? off) CheckOrder(List<RbStmt> body)
    {
        var present = Precedence.Select(g => g.SelectMany(comp => body.Where(s => s.Name == comp.name && s.Kind == comp.type)).ToList()).ToList();
        for (int p = 0; p < present.Count - 1; p++)
        {
            if (present[p].Count == 0) continue;
            for (int q = p + 1; q < present.Count; q++)
            {
                if (present[q].Count == 0) continue;
                foreach (var next in present[q])
                    foreach (var first in present[p])
                        if (first.Line >= next.Line) return (present, (first, next));
            }
        }
        return (present, null);
    }

    static void ComponentProblem(CopContext c, List<List<RbStmt>> present, RbStmt n1, RbStmt n2)
    {
        c.At("FormulaAudit/ComponentsOrder", n1, $"`{Format(n1)}` (line {n1.Line}) should be put before `{Format(n2)}` (line {n2.Line})",
            ReorderFix(c.Src, present, n1, n2));
    }

    static List<CopEdit> ReorderFix(RbSource src, List<List<RbStmt>> present, RbStmt n1, RbStmt n2)
    {
        int orderIdx = present.FindIndex(g => g.Contains(n1));
        var group = present[orderIdx];
        int cur = group.IndexOf(n1);
        var n1src = src.Src(n1);
        var edits = new List<CopEdit>();
        if (cur > 0)
        {
            var prev = group[cur - 1];
            var breaks = prev.Multiline ? "\n\n" : "\n";
            edits.Add(new CopEdit { Start = prev.End, Length = 0, Text = breaks + new string(' ', prev.Col) + n1src });
        }
        else
        {
            var breaks = orderIdx > 8 ? "\n\n" : "\n";
            edits.Add(new CopEdit { Start = n2.Start, Length = 0, Text = n1src + breaks + new string(' ', n2.Col) });
        }
        int left = n1.Start;
        while (left > 0 && char.IsWhiteSpace(src.Text[left - 1])) left--;
        edits.Add(new CopEdit { Start = left, Length = n1.End - left, Text = "" });
        // Edits must not overlap (e.g. moving a node to directly before its neighbour).
        var sorted = edits.OrderBy(e => e.Start).ToList();
        for (int i = 1; i < sorted.Count; i++)
            if (sorted[i].Start < sorted[i - 1].Start + sorted[i - 1].Length) return null;
        return edits;
    }

    static void ComponentsOrder(CopContext c, List<RbStmt> body)
    {
        var (present, off) = CheckOrder(body);
        if (off is var (a, b)) ComponentProblem(c, present, a, b);
        foreach (var head in body.Where(s => s.Kind == "block" && s.Name == "head"))
        {
            var (hp, ho) = CheckOrder(head.Body);
            if (ho is var (x, y)) ComponentProblem(c, hp, x, y);
        }
        foreach (var on in OnSystemMethods)
        {
            var blocks = body.Where(s => s.Kind == "block" && s.Name == on).ToList();
            if (blocks.Count == 0) continue;
            if (blocks.Count > 1) c.At("FormulaAudit/ComponentsOrder", blocks[1], $"There can only be one `{on}` block in a formula.");
            var blk = blocks[0];
            var (op, oo) = CheckOrder(blk.Body);
            if (oo is var (x, y)) ComponentProblem(c, op, x, y);
            var allowed = new List<string> { "livecheck", "keg_only", "disable!", "deprecate!", "depends_on", "conflicts_with", "fails_with", "resource", "patch", "pour_bottle?" };
            allowed.AddRange(OnSystemMethods);
            foreach (var child in blk.Body)
            {
                if (child.Kind is not ("send" or "block")) continue;
                if (allowed.Contains(child.Name)) continue;
                c.At("FormulaAudit/ComponentsOrder", child,
                    $"`{on}` cannot include `{child.Name}`. Only {Out.ToSentence(allowed.Select(a => $"`{a}`").ToList())} are allowed.");
            }
        }
        foreach (var res in body.Where(s => s.Kind == "block" && s.Name == "resource"))
        {
            var (rp, ro) = CheckOrder(res.Body);
            if (ro is var (x, y)) ComponentProblem(c, rp, x, y);
            foreach (var on in OnSystemMethods)
                if (res.Body.Count(s => s.Kind == "block" && s.Name == on) > 1)
                    c.At("FormulaAudit/ComponentsOrder", res, $"There can only be one `{on}` block in a resource block.");
        }
    }

    static void Checksum(CopContext c, RbStmt cls)
    {
        var md5 = Every(cls, "md5").FirstOrDefault();
        if (md5 != null) c.At("FormulaAudit/Checksum", md5, "MD5 checksums are deprecated, please use SHA-256");
        var sha1 = Every(cls, "sha1").FirstOrDefault();
        if (sha1 != null) c.At("FormulaAudit/Checksum", sha1, "SHA1 checksums are deprecated, please use SHA-256");
        foreach (var call in Every(cls, "sha256"))
        {
            var s = c.Src.FirstStringArg(call);
            if (s == null) continue;
            var v = s.Content;
            if (v.Length == 0)
            {
                c.At("FormulaAudit/Checksum", s, "`sha256` is empty");
                continue;
            }
            if (v.Length != 64 && Regex.IsMatch(v, @"^\w*$", ML)) c.At("FormulaAudit/Checksum", s, "`sha256` should be 64 characters");
            var bad = Regex.Match(v, "[^a-f0-9]+", RegexOptions.IgnoreCase);
            if (bad.Success) c.AtMatch("FormulaAudit/Checksum", s, bad, "`sha256` contains invalid characters");
            var upper = Regex.Match(v, "[A-F]+");
            if (upper.Success)
                c.AtMatch("FormulaAudit/ChecksumCase", s, upper, "`sha256` should be lowercase",
                    CopContext.Replace(s.Start, s.End, c.Src.Src(s.Start, s.End).ToLowerInvariant()));
        }
    }

    static void Version(CopContext c, List<RbStmt> body)
    {
        var v = Call(body, "version");
        if (v == null) return;
        var s = c.Src.FirstStringArg(v);
        if (s == null) return;
        var version = s.Content;
        if (version.Length == 0) c.At("FormulaAudit/Version", v, "Version is set to an empty string");
        if (version.StartsWith('v')) c.At("FormulaAudit/Version", v, $"Version {version} should not have a leading 'v'");
        if (Regex.IsMatch(version, @"_\d+$", ML)) c.At("FormulaAudit/Version", v, $"Version {version} should not end with an underline and a number");
    }

    static void Urls(CopContext c, RbStmt cls)
    {
        var src = c.Src;
        var urls = Every(cls, "url").ToList();
        var mirrors = Every(cls, "mirror").ToList();
        var livecheckUrls = new List<string>();
        foreach (var lc in Every(cls, "livecheck"))
        {
            var u = Every(lc, "url").FirstOrDefault();
            var us = u == null ? null : src.FirstStringArg(u);
            if (us != null) livecheckUrls.Add(us.Content);
        }
        const string cop = "FormulaAudit/Urls";
        void AuditUrls(List<RbStmt> nodes, string pattern, RegexOptions opts, Action<Match, string, int, RbStmt, RbString> yield)
        {
            for (int idx = 0; idx < nodes.Count; idx++)
            {
                var sn = src.FirstStringArg(nodes[idx]);
                if (sn == null) continue;
                var m = Regex.Match(sn.Content, pattern, opts);
                if (!m.Success) continue;
                yield(m, sn.Content, idx, nodes[idx], sn);
            }
        }
        void P(RbStmt node, string msg, List<CopEdit> fix = null) => c.At(cop, node, msg, fix);

        AuditUrls(urls, @"[^\p{IsBasicLatin}]+", ML, (m, url, i, n, s) => P(n, $"Please use the ASCII (Punycode-encoded host, URL-encoded path and query) version of {url}."));
        AuditUrls(urls, @"^(?:https?|ftp)://ftp\.gnu\.org/(?:gnu/)?(.*)", ML, (m, url, i, n, s) => P(n, $"{url} should be: https://ftpmirror.gnu.org/{m.Groups[1].Value}"));
        AuditUrls(urls, @"^https?://fossies\.org/", ML, (m, url, i, n, s) => P(n, "Please don't use \"fossies.org\" in the `url` (using as a mirror is fine)"));
        AuditUrls(urls, @"^https?://(?:dist\.apache\.org/repos/dist/release/|(?:dlcdn|downloads)\.apache\.org/|(?:[^/]*\.)?apache\.org/(?:dyn/(?:.*/)?(?:closer|mirrors)\.cgi\?(?:action=download&)?(?:filename|path)=/?|dist/))(.*)",
            MLI, (m, url, i, n, s) =>
            {
                if (livecheckUrls.Contains(url)) return;
                var fixd = $"https://www.apache.org/dyn/closer.lua?path={m.Groups[1].Value}";
                P(n, $"{url} should be: {fixd}", CopContext.Replace(s.Start, s.End, $"\"{fixd}\""));
            });
        AuditUrls(urls, "^(cvs|bzr|hg|fossil)://", ML, (m, url, i, n, s) => P(n, m.Groups[1].Value == "bzr"
            ? "Use of the \"bzr://\" scheme is deprecated, use Git or a stable archive URL instead"
            : $"Use of the \"{m.Groups[1].Value}://\" scheme is deprecated, pass `using: :{m.Groups[1].Value}` instead"));
        AuditUrls(urls, @"^svn\+http://", ML, (m, url, i, n, s) => P(n, "Use of the \"svn+http://\" scheme is deprecated, pass `using: :svn` instead"));
        AuditUrls(mirrors, ".*", ML, (m, mirror, i, n, s) =>
        {
            foreach (var u in urls)
                if (src.FirstStringArg(u)?.Content == mirror) P(n, $"URL should not be duplicated as a mirror: {mirror}");
        });
        var all = urls.Concat(mirrors).ToList();
        var httpToHttps = string.Join("|", new[]
        {
            @"^http://ftp\.gnu\.org/", @"^http://ftpmirror\.gnu\.org/", @"^http://download\.savannah\.gnu\.org/", @"^http://download-mirror\.savannah\.gnu\.org/",
            @"^http://(?:[^/]*\.)?apache\.org/", @"^http://code\.google\.com/", @"^http://fossies\.org/", @"^http://mirrors\.kernel\.org/",
            @"^http://mirrors\.ocf\.berkeley\.edu/", @"^http://(?:[^/]*\.)?bintray\.com/", @"^http://tools\.ietf\.org/", @"^http://launchpad\.net/",
            @"^http://github\.com/", @"^http://bitbucket\.org/", @"^http://anonscm\.debian\.org/", @"^http://cpan\.metacpan\.org/",
            @"^http://hackage\.haskell\.org/", @"^http://(?:[^/]*\.)?archive\.org", @"^http://(?:[^/]*\.)?freedesktop\.org",
            @"^http://(?:[^/]*\.)?mirrorservice\.org/", @"^http://downloads?\.sourceforge\.net/",
        }.Select(p => $"(?:{p})"));
        AuditUrls(all, httpToHttps, ML, (m, url, index, n, s) =>
        {
            var https = url.Insert(4, "s");
            int? httpsIndex = null;
            for (int k = 0; k < all.Count; k++)
                if (src.FirstStringArg(all[k]) is { } hs && Regex.IsMatch(hs.Content, https, ML)) httpsIndex = k;
            if (httpsIndex == null || httpsIndex > index) P(n, $"Please use https:// for {url}");
        });
        AuditUrls(mirrors, @"^https?://(?:[^/]*\.)?apache\.org/dyn/closer\.(?:cgi|lua)\?path=/?(.*)", MLI, (m, mirror, i, n, s) => P(n, $"{mirror} should be: https://archive.apache.org/dist/{m.Groups[1].Value}"));
        AuditUrls(all, @"^http://search\.mcpan\.org/CPAN/(.*)", MLI, (m, url, i, n, s) => P(n, $"{url} should be: https://cpan.metacpan.org/{m.Groups[1].Value}"));
        AuditUrls(all, @"^(?:http|ftp)://ftp\.gnome\.org/pub/gnome/(.*)", MLI, (m, url, i, n, s) => P(n, $"{url} should be: https://download.gnome.org/{m.Groups[1].Value}"));
        AuditUrls(all, @"^git://anonscm\.debian\.org/users/(.*)", MLI, (m, url, i, n, s) => P(n, $"{url} should be: https://anonscm.debian.org/git/users/{m.Groups[1].Value}"));
        AuditUrls(all, @"^ftp://ftp\.mirrorservice\.org", ML, (m, url, i, n, s) => P(n, $"Please use https:// for {url}"));
        AuditUrls(all, @"^ftp://ftp\.cpan\.org/pub/CPAN(.*)", MLI, (m, url, i, n, s) => P(n, $"{url} should be: http://search.cpan.org/CPAN{m.Groups[1].Value}"));
        AuditUrls(all, @"^https?://.*\b(?:sourceforge|sf)\.(?:com|net)", ML, (m, url, i, n, s) =>
        {
            if (url.Contains("/svnroot/") || url.Contains("svn.sourceforge") || url.Contains("/p/")) return;
            var um = Regex.Match(url, @"(\?|&)use_mirror=");
            if (um.Success) P(n, $"Don't use \"{um.Groups[1].Value}use_mirror\" in SourceForge URLs (`url` is {url}).");
            if (url.EndsWith("/download")) P(n, $"Don't use \"/download\" in SourceForge URLs (`url` is {url}).");
            if (Regex.IsMatch(url, @"^https?://(sourceforge|sf)\.", ML) && !livecheckUrls.Contains(url))
                P(n, $"Use \"https://downloads.sourceforge.net\" to get geolocation (`url` is {url}).");
            if (Regex.IsMatch(url, @"^https?://prdownloads\.", ML)) P(n, $"Don't use \"prdownloads\" in SourceForge URLs (`url` is {url}).");
            if (Regex.IsMatch(url, @"^http://\w+\.dl\.", ML)) P(n, $"Don't use specific \"dl\" mirrors in SourceForge URLs (`url` is {url}).");
            if (Regex.IsMatch(url, @"^https?://downloads?\.sf\.net", ML)) P(n, $"Use \"https://downloads.sourceforge.net\" instead of \"downloads.sf.net\" (`url` is {url})");
        });
        AuditUrls(all, @"^http://http\.debian\.net/debian/(.*)", MLI, (m, url, i, n, s) =>
            P(n, $"Please use a secure mirror for Debian URLs.\nWe recommend:\n  https://deb.debian.org/debian/{m.Groups[1].Value}\n"));
        AuditUrls(all, @"(?:^https://mirrors\.kernel\.org/debian/)|(?:^https://mirrors\.ocf\.berkeley\.edu/debian/)|(?:^https://(?:[^/]*\.)?mirrorservice\.org/sites/ftp\.debian\.org/debian/)", ML,
            (m, url, i, n, s) => P(n, $"Please use https://deb.debian.org/debian/ for {url}"));
        AuditUrls(all, @"(?:^http://[A-Za-z0-9\-.]*\.googlecode\.com/files.*)|(?:^http://code\.google\.com/)", ML, (m, url, i, n, s) => P(n, $"Please use https:// for {url}"));
        AuditUrls(all, @"^git://[^/]*github\.com/", ML, (m, url, i, n, s) => P(n, $"Please use https:// for {url}"));
        AuditUrls(all, @"^git://[^/]*gitorious\.org/", ML, (m, url, i, n, s) => P(n, $"Please use https:// for {url}"));
        AuditUrls(all, @"^http://github\.com/.*\.git$", ML, (m, url, i, n, s) => P(n, $"Please use https:// for {url}"));
        AuditUrls(all, @"^https://github\.com/.*archive/(?:main|master)\.(?:tar\.gz|zip)$", ML, (m, url, i, n, s) => P(n, "Use versioned rather than branch tarballs for stable checksums."));
        AuditUrls(all, @"https://.*github.*/(?:tar|zip)ball/", ML, (m, url, i, n, s) =>
        {
            if (!url.EndsWith(".git")) P(n, $"Use /archive/ URLs for GitHub tarballs (`url` is {url}).");
        });
        AuditUrls(all, @"https://.*github.+/archive/(?![a-fA-F0-9]{40})(?!refs/(?:tags|heads)/)(.*)\.tar\.gz$", ML, (m, url, i, n, s) =>
        {
            if (!url.EndsWith(".git"))
                P(n, $"Use \"refs/tags/{m.Groups[1].Value}\" or \"refs/heads/{m.Groups[1].Value}\" for GitHub references (`url` is {url}).");
        });
        AuditUrls(all, @"https://.*github.*/(?:archive|releases)/.*\.zip$", ML, (m, url, i, n, s) =>
        {
            if (Regex.IsMatch(url, @"raw\.githubusercontent\.com/.*/.*/(main|master|HEAD)/") || url.Contains("releases/download") ||
                url.Contains("desktop.githubusercontent.com/releases/")) return;
            P(n, $"Use GitHub tarballs rather than zipballs (`url` is {url}).");
        });
        AuditUrls(all, @"https?://codeload\.github\.com/(.+)/(.+)/(?:tar\.gz|zip)/(.+)", ML, (m, url, i, n, s) =>
            P(n, $"Use GitHub archive URLs:\n  https://github.com/{m.Groups[1].Value}/{m.Groups[2].Value}/archive/{m.Groups[3].Value}.tar.gz\nRather than codeload:\n  {url}\n"));
        AuditUrls(all, @"https?://(?:central|repo\d+)\.maven\.org/maven2/(.+)$", ML, (m, url, i, n, s) =>
            P(n, $"{url} should be: https://search.maven.org/remotecontent?filepath={m.Groups[1].Value}"));

        if (c.CoreTap)
        {
            AuditUrls(urls, "(?:darwin|macos|osx)", MLI, (m, url, i, n, s) =>
            {
                if (c.FormulaName.Contains(m.Value.ToLowerInvariant())) return;
                if (Regex.IsMatch(url, @".(patch|diff)(\?full_index=1)?$", ML)) return;
                var gh = Regex.Match(url, @"^https://github\.com/[\w-]+/[\w.-]+/(.*)$", MLI);
                if (gh.Success && !Regex.IsMatch(gh.Groups[1].Value, "(?:darwin|macos|osx)", RegexOptions.IgnoreCase)) return;
                P(n, $"{url} looks like a binary package, not a source archive; homebrew/core is source-only.");
            });

            // HttpUrls
            if (!Every(cls, "deprecate!").Any() && !Every(cls, "disable!").Any())
                foreach (var u in urls)
                {
                    var s = src.FirstStringArg(u);
                    if (s == null || !s.Content.StartsWith("http://") || livecheckUrls.Contains(s.Content)) continue;
                    var source = src.Src(s.Start, s.End);
                    c.At("FormulaAudit/HttpUrls", s, "Formulae in homebrew/core should not use http:// URLs",
                        CopContext.Replace(s.Start, s.End, new Regex("http://").Replace(source, "https://", 1)));
                }

            // GitUrls (direct children only, like find_method_calls_by_name)
            foreach (var u in cls.Body.Where(x => x.Kind == "send" && x.Name == "url"))
            {
                var s = src.FirstStringArg(u);
                if (s == null || !Regex.IsMatch(s.Content, @"\.git$", ML)) continue;
                var args = src.ArgsMasked(u);
                if (!Regex.IsMatch(args, @"\brevision:\s*""")) c.At("FormulaAudit/GitUrls", u, "Formulae in homebrew/core should specify a revision for Git URLs");
                if (!Regex.IsMatch(args, @"\btag:\s*""")) c.At("FormulaAuditStrict/GitUrls", u, "Formulae in homebrew/core should specify a tag for Git URLs");
            }
        }

        // PyPiUrls
        foreach (var (pattern, _) in new[] { (@"^https?://pypi\.python\.org/", 0), (@"^https?://files\.pythonhosted\.org/packages/source/", 1) })
            AuditUrls(all, pattern, ML, (m, url, i, n, s) =>
            {
                var file = url[(url.LastIndexOf('/') + 1)..];
                var pm = Regex.Match(file, @"^(.+)-[a-z0-9.]+$", ML);
                var pkg = pm.Success ? pm.Groups[1].Value : file;
                c.At("FormulaAudit/PyPiUrls", n, $"Use the \"Source\" URL found on the PyPI downloads page (https://pypi.org/project/{pkg}/#files)");
            });
    }

    static void DeprecateDisable(CopContext c, List<RbStmt> body)
    {
        var src = c.Src;
        foreach (var method in new[] { "deprecate!", "disable!" })
        {
            var node = Call(body, method);
            if (node == null) continue;
            var args = src.ArgsMasked(node);
            int argStart = src.ArgStart(node, out _);
            var dm = Regex.Match(args, @"\bdate:\s*(?="")");
            if (dm.Success && src.StringAt.TryGetValue(argStart + dm.Index + dm.Length, out var ds))
            {
                var v = ds.Content;
                if (!Regex.IsMatch(v, @"^\d{4}-\d{2}-\d{2}$") || !DateOnly.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    {
                        var fixd = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        c.At("FormulaAudit/DeprecateDisableDate", ds, $"Use `{fixd}` to comply with ISO 8601", CopContext.Replace(ds.Start, ds.End, $"\"{fixd}\""));
                    }
                }
            }
            var rm = Regex.Match(args, @"\bbecause:\s*");
            if (rm.Success)
            {
                int p = argStart + rm.Index + rm.Length;
                if (src.StringAt.TryGetValue(p, out var rs) && !rs.Symbol)
                {
                    var reason = rs.Content;
                    if (reason.StartsWith("it "))
                        c.At("FormulaAudit/DeprecateDisableReason", rs, "Do not start the reason with `it`", CopContext.Replace(rs.Start, rs.End, $"\"{reason[3..]}\""));
                    if (reason.Length > 0 && ".!?".Contains(reason[^1]))
                        c.At("FormulaAudit/DeprecateDisableReason", rs, "Do not end the reason with a punctuation mark", CopContext.Replace(rs.Start, rs.End, $"\"{reason[..^1]}\""));
                }
            }
            else
                c.At("FormulaAudit/DeprecateDisableReason", node, method == "deprecate!"
                    ? "Add a reason for deprecation: `deprecate! because: \"...\"`"
                    : "Add a reason for disabling: `disable! because: \"...\"`");
        }
    }

    static readonly Regex TarPattern = new(@"\\?\.t(?:ar|(?:g|l|x)z$|[bz2]{2,4}$)(?:\\?\.(?:g|l|x)z|[bz2]{2,4}|Z)?$", MLI);

    static void Livecheck(CopContext c, List<RbStmt> body)
    {
        var src = c.Src;
        var lc = Block(body, "livecheck");
        if (lc == null) return;
        var skip = Every(lc, "skip").FirstOrDefault();
        if (skip != null)
        {
            int sends = 1 + lc.Descendants().Count(d => d.Kind is "send" or "block");
            if (sends >= 3)
            {
                var msg = src.FirstStringArg(skip)?.Content;
                var repl = "livecheck do\n    skip" + (string.IsNullOrEmpty(msg) ? "" : $" \"{msg}\"") + "\n  end";
                c.At("FormulaAudit/LivecheckSkip", lc, "Skipped formulae must not contain other livecheck information.", CopContext.Replace(lc.Start, lc.End, repl));
            }
        }
        var url = Every(lc, "url").FirstOrDefault();
        var regex = Every(lc, "regex").FirstOrDefault();
        var strategy = Every(lc, "strategy").FirstOrDefault();
        if (url == null && (regex != null || strategy != null))
            c.At("FormulaAudit/LivecheckUrlProvided", lc, "A `url` should be provided when `regex` or `strategy` are used.");
        if (skip != null) return;

        if (url != null && src.StringsIn(url.Start, url.End).FirstOrDefault() is { } lus)
        {
            var lurl = lus.Content;
            var cls = src.FormulaClass;
            string head = null, stable = null, homepage = null;
            var headNode = Every(cls, "head").FirstOrDefault();
            if (headNode != null)
            {
                head = headNode.Kind == "send" ? src.StringsIn(headNode.Start, headNode.End).FirstOrDefault()?.Content : null;
            }
            var stableNode = Every(cls, "url").FirstOrDefault();
            stable = stableNode == null ? null : src.StringsIn(stableNode.Start, stableNode.End).FirstOrDefault()?.Content;
            var hp = Every(cls, "homepage").FirstOrDefault();
            if (hp != null) homepage = src.StringsIn(hp.Start, hp.End).FirstOrDefault()?.Content;
            foreach (var (sym, u) in new[] { ("head", head), ("stable", stable), ("homepage", homepage) })
            {
                if (u == null) continue;
                if (u != lurl && u != lurl + "/" && u + "/" != lurl) continue;
                c.At("FormulaAudit/LivecheckUrlSymbol", url, $"Use `url :{sym}`", CopContext.Replace(url.Start, url.End, $"url :{sym}"));
                break;
            }
        }

        if (regex != null)
        {
            src.ArgStart(regex, out var parens);
            if (!parens)
            {
                var pattern = string.Concat(src.Src(regex).Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Skip(1));
                c.At("FormulaAudit/LivecheckRegexParentheses", regex, "The `regex` call should always use parentheses.",
                    CopContext.Replace(regex.Start, regex.End, $"regex({pattern})"));
            }
            int p = src.ArgStart(regex, out _);
            if (src.StringAt.TryGetValue(p, out var rx) && rx.IsRegex)
            {
                var tm = TarPattern.Match(rx.Content);
                if (tm.Success && !rx.Dynamic)
                {
                    var source = src.Src(rx.Start, rx.End);
                    c.At("FormulaAudit/LivecheckRegexExtension", rx, $"Use `\\.t` instead of `{tm.Value}`",
                        CopContext.Replace(rx.Start, rx.End, source[..(rx.ContentStart - rx.Start)] + TarPattern.Replace(rx.Content, @"\.t") + source[(rx.ContentEnd - rx.Start)..]));
                }
                if (!rx.Flags.Contains('i'))
                {
                    var flags = string.Concat(("i" + rx.Flags).Order());
                    c.At("FormulaAudit/LivecheckRegexCaseInsensitive", rx,
                        "Regexes should be case-insensitive unless sensitivity is explicitly required for proper matching.",
                        CopContext.Replace(rx.End - rx.Flags.Length, rx.End, flags));
                }
            }
        }
        if (strategy != null && regex == null && src.ArgsSource(strategy).Trim().StartsWith(":page_match") &&
            Regex.IsMatch(src.ArgsSource(strategy).Trim(), @"^:page_match\b"))
            c.At("FormulaAudit/LivecheckRegexIfPageMatch", lc, "A `regex` is required if `strategy :page_match` is present.");
    }

    static void Test(CopContext c, RbStmt cls, List<RbStmt> body)
    {
        var src = c.Src;
        var test = Block(body, "test");
        if (test == null)
        {
            if (Call(body, "disable!") != null) return;
            c.At("FormulaAudit/Test", cls, "A `test do` test block should be added");
            return;
        }
        if (test.Body.Count == 0)
        {
            c.At("FormulaAudit/Test", test, "`test do` should not be empty");
            return;
        }
        if (test.Body.Count == 1 && !test.Body[0].Multiline && src.Src(test.Body[0]).Trim() == "true")
            c.At("FormulaAudit/Test", test, "`test do` should contain a real test");
        foreach (var call in test.Descendants().Where(d => d.Name is "system" or "shell_output" or "pipe_output" && d.Kind == "send"))
        {
            var args = SplitArgs(src, call);
            if (args.Count == 0) continue;
            var (p1s, p1e) = args[0];
            if (src.StringAt.TryGetValue(p1s, out var p1))
            {
                var mm = Regex.Match(p1.Content, "(/usr/local/(s?bin))");
                if (mm.Success)
                {
                    var source = src.Src(p1.Start, p1.End);
                    c.At("FormulaAudit/Test", p1, $"Use `#{{{mm.Groups[2].Value}}}` instead of `{mm.Groups[1].Value}` in `{call.Name}`",
                        CopContext.Replace(p1.Start, p1.End, new Regex(Regex.Escape(mm.Groups[1].Value)).Replace(source, "#{" + mm.Groups[2].Value + "}", 1)));
                }
            }
            if (call.Name == "shell_output" && args.Count >= 2 && src.Src(args[1].s, args[1].e).Trim() == "0")
            {
                int left = args[1].s;
                while (left > 0 && src.Text[left - 1] is ' ' or ',') left--;
                c.Add("FormulaAudit/Test", src.LineOf(args[1].s), src.ColOf(args[1].s), 1, "Passing 0 to `shell_output` is redundant",
                    CopContext.Replace(left, args[1].e, ""));
            }
        }
    }

    // Top-level comma-separated arguments of a call: (start, end) absolute offsets.
    static List<(int s, int e)> SplitArgs(RbSource src, RbStmt call)
    {
        var res = new List<(int, int)>();
        int p = src.ArgStart(call, out bool parens);
        int end = call.End;
        int depth = 0, argStart = p;
        for (int k = p; k < end; k++)
        {
            char ch = src.Mask[k];
            if (ch is '(' or '[' or '{') depth++;
            else if (ch is ')' or ']' or '}')
            {
                if (depth == 0) { end = k; break; }
                depth--;
            }
            else if (ch == ',' && depth == 0)
            {
                res.Add((argStart, k));
                argStart = k + 1;
                while (argStart < end && char.IsWhiteSpace(src.Text[argStart])) argStart++;
            }
            else if (!parens && depth == 0 && (ch == '\n' && k > 0 && src.Mask[k - 1] != ',' || Regex.IsMatch(new string(src.Mask, k, Math.Min(4, end - k)), @"^ (do|if|unless)\b")))
            {
                end = k;
                break;
            }
        }
        var tail = src.Src(argStart, end).TrimEnd();
        if (tail.Length > 0) res.Add((argStart, argStart + tail.Length));
        return res;
    }

    static void Licenses(CopContext c, List<RbStmt> body)
    {
        var src = c.Src;
        var lic = Call(body, "license");
        if (lic == null) return;
        var args = src.ArgsSource(lic).Trim();
        if (args.StartsWith('['))
            c.At("FormulaAudit/LicenseArrays", lic, $"Use `license any_of: {args}` instead of `license {args}`",
                CopContext.Replace(lic.Start, lic.End, $"license any_of: {args}"));
        if (!lic.Multiline)
        {
            var masked = src.ArgsMasked(lic);
            var keys = Regex.Matches(masked, @"(\w+):\s").Select(m => m.Groups[1].Value).ToList();
            if (keys.Count > 1 && keys.Skip(1).Any(k => k != "with"))
                c.At("FormulaAudit/Licenses", lic, "Split nested license declarations onto multiple lines");
        }
    }

    static void Asserts(CopContext c, RbStmt cls)
    {
        var src = c.Src;
        const string cop = "FormulaAudit/AssertStatements";
        foreach (var m in Every(cls, "assert").Where(x => x.Kind == "send"))
        {
            var masked = src.ArgsMasked(m);
            int argPos = src.ArgStart(m, out _);
            bool neg = masked.TrimStart().StartsWith('!') || Regex.IsMatch(masked, @"\.!\b|\bnot\b");
            var source = src.Src(m);
            void At(string word, string msg)
            {
                int idx = masked.IndexOf(word, StringComparison.Ordinal);
                c.Add(cop, src.LineOf(argPos), src.ColOf(argPos), Math.Max(1, idx + word.Length), msg);
            }
            if (masked.Contains(".include?") && !neg) At(".include?", "Use `assert_match` instead of `assert ...include?`");
            if (masked.Contains(".exist?") && !neg) At(".exist?", $"Use `assert_path_exists <path_to_file>` instead of `{source}`");
            if (masked.Contains(".exist?") && neg) At(".exist?", $"Use `refute_path_exists <path_to_file>` instead of `{source}`");
            if (masked.Contains(".executable?") && !neg) At(".executable?", $"Use `assert_predicate <path_to_file>, :executable?` instead of `{source}`");
        }
        foreach (var m in Every(cls, "assert_equal").Where(x => x.Kind == "send"))
        {
            var args = SplitArgs(src, m);
            if (args.Count > 0 && src.Src(args[0].s, args[0].e).Trim() == "nil")
                c.At(cop, m, "Use `assert_nil` instead of `assert_equal` with a nil expected value");
        }
        foreach (var (name, repl) in new[] { ("assert_predicate", "assert_path_exists"), ("refute_predicate", "refute_path_exists") })
            foreach (var m in Every(cls, name).Where(x => x.Kind == "send"))
            {
                var args = SplitArgs(src, m);
                if (args.Count < 2 || src.Src(args[1].s, args[1].e).Trim() != ":exist?") continue;
                var correct = $"{repl} {src.Src(args[0].s, args[0].e)}";
                if (args.Count == 3) correct += $", {src.Src(args[2].s, args[2].e)}";
                c.At(cop, m, $"Use `{repl} <path_to_file>` instead of `{src.Src(m)}`", CopContext.Replace(m.Start, m.End, correct));
            }
    }

    static void Misc(CopContext c, RbStmt cls, List<RbStmt> body)
    {
        var src = c.Src;
        foreach (Match m in Regex.Matches(new string(src.Mask, cls.Start, cls.End - cls.Start), @"(?<![\w:.])FileUtils\.([a-z_]\w*[?!]?)"))
        {
            int abs = cls.Start + m.Index;
            c.Add("FormulaAudit/Miscellaneous", src.LineOf(abs), src.ColOf(abs), m.Length, $"No need for `FileUtils.` before `{m.Groups[1].Value}`");
        }
        foreach (var b in Every(cls, "inreplace").Where(x => x.Kind == "block"))
        {
            var bm = Regex.Match(src.MLine(b.Line), @"\|\s*([^|,\s]+)");
            if (bm.Success && bm.Groups[1].Value.Length > 1)
                c.At("FormulaAudit/Miscellaneous", b, $"`inreplace <filenames> do |s|` is preferred over `|{bm.Groups[1].Value}|`.");
        }
        foreach (var name in new[] { "rebuild", "version_scheme" })
            foreach (var m in Every(cls, name).Where(x => x.Kind == "send" && src.ArgsSource(x).Trim() == "0"))
                c.At("FormulaAudit/Miscellaneous", m, $"`{name} 0` should be removed");

        var req = Regex.Match(src.Text, @"^require ['""]formula['""]$", ML);
        if (req.Success)
        {
            int ln = src.LineOf(req.Index);
            int end = req.Index + req.Length;
            while (end < src.Text.Length && char.IsWhiteSpace(src.Text[end])) end++;
            c.Add("FormulaAudit/Text", ln, 0, req.Length, $"`{req.Value}` is now unnecessary", CopContext.Replace(req.Index, end, ""));
        }
        if (body.Any(s => s.Kind == "def" && s.Name == "plist"))
            c.At("FormulaAudit/Text", body.First(s => s.Kind == "def" && s.Name == "plist"),
                "`def plist` is deprecated. Please use services instead: https://docs.brew.sh/Formula-Cookbook#service-files");
        foreach (var m in Every(cls, "revision").Where(x => x.Kind == "send" && src.ArgsSource(x).Trim() == "0"))
            c.At("FormulaAudit/Text", m, "`revision 0` is unnecessary");
        foreach (Match m in Regex.Matches(new string(src.Mask, cls.Start, cls.End - cls.Start), @"\bFormula\.factory\b"))
        {
            int abs = cls.Start + m.Index;
            c.Add("FormulaAudit/Text", src.LineOf(abs), src.ColOf(abs), m.Length, "`Formula.factory(name)` is deprecated in favour of `Formula[name]`");
        }
    }

    static void StrictText(CopContext c, RbStmt cls)
    {
        var src = c.Src;
        var name = c.FormulaName;
        foreach (var s in src.StringsIn(cls.Start, cls.End))
        {
            if (s.Single || s.Heredoc || s.IsRegex || s.Words) continue;
            var raw = src.Src(s.ContentStart, s.ContentEnd);
            var m = Regex.Match(raw, @"^#\{bin\}(/[^#]*)$");
            if (!m.Success) continue;
            var path = m.Groups[1].Value;
            if (path.Contains(' ') || !Regex.IsMatch(path, $"^{Regex.Escape("/" + name)}(/|-|$)")) continue;
            int stmtStart = src.LineStart[s.Line - 1];
            var before = new string(src.Mask, stmtStart, s.Start - stmtStart);
            if (before.Count(ch => ch == '[') > before.Count(ch => ch == ']')) continue;
            var cmd = path[1..];
            c.At("FormulaAuditStrict/Text", s, $"Use `bin/\"{cmd}\"` instead of `\"#{{bin}}/{cmd}\"`", CopContext.Replace(s.Start, s.End, $"bin/\"{cmd}\""));
        }
        var env = src.Text.Substring(cls.Start, cls.End - cls.Start);
        foreach (var e in Every(cls, "env").Where(x => x.Kind == "send"))
        {
            var a = src.ArgsSource(e).Trim();
            if (a == ":userpaths") c.At("FormulaAuditStrict/Text", e, "`env :userpaths` in homebrew/core formulae is deprecated");
            if (a == ":std" && c.CoreTap) c.At("FormulaAuditStrict/Text", e, "`env :std` in homebrew/core formulae is deprecated");
        }
    }
}

// ---------------------------------------------------------------- runner / output

static class StyleCheck
{
    public static string TapDir => Path.Combine(DevCommands.Repository, "tap", "homebrew-core");

    // args.named.to_paths for style: files, taps or formula names.
    public static List<string> NamedToPaths(IReadOnlyList<string> named, bool cask)
    {
        var res = new List<string>();
        foreach (var n in named)
        {
            if (File.Exists(n) || Directory.Exists(n)) { res.Add(Path.GetFullPath(n)); continue; }
            if (Regex.IsMatch(n, @"^[\w-]+/[\w-]+$"))
            {
                if (n.Equals("homebrew/core", StringComparison.OrdinalIgnoreCase)) { res.Add(TapDir); continue; }
                throw new BrewError($"No available tap {n}.");
            }
            if (cask) throw new BrewError($"No available cask with the name \"{n}\".");
            var p = DevCommands.FormulaPath(n) ?? throw new BrewError($"No available formula or cask with the name \"{n}\".");
            res.Add(p);
        }
        return res;
    }

    static IEnumerable<string> Expand(List<string> files)
    {
        if (files.Count == 0)
        {
            foreach (var f in DevCommands.AllFormulaFiles()) yield return f;
            yield break;
        }
        foreach (var f in files)
        {
            if (Directory.Exists(f))
            {
                foreach (var r in Directory.EnumerateFiles(f, "*.rb", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                    if (!r.Replace('\\', '/').Contains("/vendor/")) yield return Path.GetFullPath(r);
            }
            else yield return Path.GetFullPath(f);
        }
    }

    static bool Selected(string cop, List<string> only, List<string> except)
    {
        bool Match(string pat) => pat == cop || pat == cop.Split('/')[0] || (!pat.Contains('/') && cop.EndsWith("/" + pat));
        if (only != null) return only.Any(Match);
        if (except != null) return !except.Any(Match);
        return true;
    }

    // rubocop:disable / rubocop:todo directives.
    static bool Disabled(RbSource src, CopOffense o)
    {
        bool Names(string list) => list.Split(',').Select(x => x.Trim()).Any(x => x == "all" || x == o.Cop || x == o.Department);
        var state = new Dictionary<string, bool>();
        foreach (var cm in src.Comments)
        {
            var m = Regex.Match(cm.Text, @"#\s*rubocop\s*:\s*(disable|todo|enable)\s+([\w/, ]+)");
            if (!m.Success) continue;
            bool own = src.Line(cm.Line)[..cm.Col].Trim().Length == 0;
            bool disable = m.Groups[1].Value != "enable";
            if (!own)
            {
                if (cm.Line == o.Line && disable && Names(m.Groups[2].Value)) return true;
                continue;
            }
            if (cm.Line > o.Line) break;
            if (Names(m.Groups[2].Value)) state["x"] = disable;
        }
        return state.TryGetValue("x", out var d) && d;
    }

    public static List<CopOffense> Inspect(string path, string text, List<string> only, List<string> except)
    {
        var src = new RbSource(path, text);
        var norm = path.Replace('\\', '/');
        var ctx = new CopContext
        {
            Src = src,
            FormulaName = Path.GetFileNameWithoutExtension(path),
            CoreTap = Regex.IsMatch(norm, @"(?:/Taps/[\w-]+|^|/tap)/homebrew-core/"),
        };
        FormulaCops.Run(ctx);
        return ctx.Offenses.Where(o => Selected(o.Cop, only, except) && !Disabled(src, o))
            .OrderBy(o => o.Line).ThenBy(o => o.Column).ThenBy(o => o.Cop, StringComparer.Ordinal).ThenBy(o => o.Message, StringComparer.Ordinal).ToList();
    }

    static string ApplyEdits(string text, IEnumerable<CopEdit> edits)
    {
        var sb = new StringBuilder(text);
        foreach (var e in edits.OrderByDescending(e => e.Start).ThenByDescending(e => e.Length))
        {
            sb.Remove(e.Start, Math.Min(e.Length, sb.Length - e.Start));
            sb.Insert(e.Start, e.Text);
        }
        return sb.ToString();
    }

    // Inspects (and with fix, autocorrects) one file. Returns the offenses to report.
    public static List<CopOffense> Check(string path, bool fix, List<string> only, List<string> except)
    {
        var text = File.ReadAllText(path);
        var offenses = Inspect(path, text, only, except);
        if (!fix) return offenses;
        var report = new List<CopOffense>(offenses);
        var original = text;
        for (int iter = 0; iter < 50; iter++)
        {
            var accepted = new List<CopEdit>();
            var acceptedOffenses = new List<CopOffense>();
            foreach (var o in offenses.Where(o => o.Correctable))
            {
                bool overlaps = o.Fix.Any(e => accepted.Any(a => e.Start <= a.Start + a.Length && a.Start <= e.Start + e.Length));
                if (overlaps) continue;
                accepted.AddRange(o.Fix);
                acceptedOffenses.Add(o);
                if (o.Fix.Any(e => e.Length >= text.Length)) break;
            }
            if (accepted.Count == 0) break;
            foreach (var o in acceptedOffenses)
            {
                var r = report.FirstOrDefault(x => !x.Corrected && x.Cop == o.Cop && x.Message == o.Message) ?? o;
                r.Corrected = true;
                if (!report.Contains(r)) report.Add(r);
            }
            text = ApplyEdits(text, accepted);
            offenses = Inspect(path, text, only, except);
        }
        report.RemoveAll(r => !r.Corrected);
        report.AddRange(offenses);
        if (text != original) File.WriteAllText(path, text);
        report.Sort((a, b) => a.Line != b.Line ? a.Line.CompareTo(b.Line) : a.Column != b.Column ? a.Column.CompareTo(b.Column) : string.CompareOrdinal(a.Cop, b.Cop));
        return report;
    }

    static void AddTodoComments(string path, List<CopOffense> offenses)
    {
        var remaining = offenses.Where(o => !o.Corrected).ToList();
        if (remaining.Count == 0) return;
        var lines = File.ReadAllText(path).Split('\n');
        foreach (var g in remaining.GroupBy(o => o.Line))
        {
            int i = g.Key - 1;
            if (i < 0 || i >= lines.Length) continue;
            var cops = string.Join(", ", g.Select(o => o.Cop).Distinct());
            lines[i] = lines[i].TrimEnd('\r') + $" # rubocop:todo {cops}" + (lines[i].EndsWith('\r') ? "\r" : "");
        }
        File.WriteAllText(path, string.Join("\n", lines));
    }

    // Style.check_style_json: path => offenses (only files with offenses).
    public static Dictionary<string, List<CopOffense>> CheckStyleJson(List<string> files, bool fix, List<string> only, List<string> except)
    {
        Validate(only);
        var res = new Dictionary<string, List<CopOffense>>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in Expand(files))
        {
            var offs = Check(f, fix, only, except);
            if (offs.Count > 0) res[f] = offs;
        }
        return res;
    }

    static void Validate(List<string> only)
    {
        if (only == null) return;
        if (!only.Any(p => FormulaCops.AllCops.Any(cp => cp == p || cp.Split('/')[0] == p || (!p.Contains('/') && cp.EndsWith("/" + p)))))
            throw new BrewError($"RuboCops {string.Join(",", only)} were not found");
    }

    static string SmartPath(string path)
    {
        var cwd = Environment.CurrentDirectory;
        var baseDir = path.StartsWith(TapDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? TapDir : cwd;
        var rel = Path.GetRelativePath(baseDir, path);
        return (rel.StartsWith("..") ? path : rel).Replace('\\', '/');
    }

    // RuboCop's progress formatter (several files) or clang formatter (a single file).
    public static bool CheckStyleAndPrint(List<string> files, bool fix, bool todo, List<string> only, List<string> except)
    {
        Validate(only);
        var all = Expand(files).ToList();
        bool clang = Environment.GetEnvironmentVariable("CI") != null || files.Count(f => !Directory.Exists(f)) == 1 && files.Count == 1;
        var o = Console.Out;
        if (!clang) o.WriteLine($"Inspecting {Out.Pluralize("file", all.Count, true)}");
        var results = new List<(string path, List<CopOffense> offs)>();
        int total = 0, corrected = 0, correctable = 0;
        foreach (var f in all)
        {
            List<CopOffense> offs;
            try { offs = Check(f, fix, only, except); }
            catch (IOException e) { Out.Onoe(e.Message); offs = new List<CopOffense>(); }
            if (todo) AddTodoComments(f, offs);
            total += offs.Count;
            corrected += offs.Count(x => x.Corrected);
            correctable += offs.Count(x => x.Correctable && !x.Corrected);
            if (clang)
            {
                ReportFile(f, offs);
                continue;
            }
            if (offs.Count > 0) results.Add((f, offs));
            o.Write(offs.Count == 0 ? Formatter.Success(".") : Mark(offs));
        }
        if (!clang)
        {
            o.WriteLine();
            if (results.Count > 0)
            {
                o.WriteLine();
                o.WriteLine("Offenses:");
                o.WriteLine();
                foreach (var (p, offs) in results) ReportFile(p, offs);
            }
        }
        o.WriteLine();
        string Plural(int n, string w) => n == 1 ? $"{n} {w}" : $"{n} {w}s";
        var offensesText = total == 0 ? "no offenses" : Plural(total, "offense");
        offensesText = total == 0 ? Formatter.Success(offensesText) : $"{Tty.Red}{offensesText}{Tty.Reset}";
        var summary = $"{Plural(all.Count, "file")} inspected, {offensesText} detected";
        if (corrected > 0) summary += $", {Formatter.Success(Plural(corrected, "offense"))} corrected";
        if (correctable > 0) summary += $", {Tty.Yellow}{Plural(correctable, "offense")}{Tty.Reset} autocorrectable";
        o.WriteLine(summary);
        return total - corrected == 0;
    }

    static readonly string Sevs = "RCWEF";

    static string Colored(string sev) => sev switch
    {
        "C" or "R" => $"{Tty.Yellow}{sev}{Tty.Reset}",
        "W" => $"{Tty.Magenta}{sev}{Tty.Reset}",
        _ => $"{Tty.Red}{sev}{Tty.Reset}",
    };

    static string Mark(List<CopOffense> offs) => Colored(offs.OrderByDescending(x => Sevs.IndexOf(x.Severity)).First().Severity);

    static void ReportFile(string path, List<CopOffense> offs)
    {
        var o = Console.Out;
        string[] lines = null;
        try { lines = File.ReadAllText(path).Split('\n'); } catch (IOException) { }
        foreach (var x in offs)
        {
            var status = x.Corrected ? $"{Tty.Green}[Corrected]{Tty.Reset} " : x.Correctable ? $"{Tty.Yellow}[Correctable]{Tty.Reset} " : "";
            o.WriteLine($"{Tty.Cyan}{SmartPath(path)}{Tty.Reset}:{x.Line}:{x.Column}: {Colored(x.Severity)}: {status}{x.Cop}: {x.Message}");
            if (x.Corrected || lines == null || x.Line - 1 >= lines.Length) continue;
            var src = lines[x.Line - 1].TrimEnd('\r');
            if (src.Trim().Length == 0) continue;
            o.WriteLine(src);
            int len = Math.Max(1, Math.Min(x.Length, Math.Max(1, src.Length - (x.Column - 1))));
            o.WriteLine(new string(' ', Math.Max(0, x.Column - 1)) + new string('^', len));
        }
    }
}
