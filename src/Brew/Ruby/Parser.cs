using System.Globalization;

namespace Brew.Ruby;

public sealed class Parser
{
    sealed class Scope
    {
        public readonly HashSet<string> Vars = new();
        public Scope Parent;
        public bool Hard;   // def/class: outer locals not visible
    }

    static readonly HashSet<string> OpAssignOps = new() { "+=", "-=", "*=", "/=", "%=", "**=", "||=", "&&=", "|=", "&=", "<<=", ">>=" };
    static readonly HashSet<string> ValueKeywords = new() { "nil", "true", "false", "self", "not", "__FILE__", "__dir__", "super", "yield", "def", "case", "begin", "if", "unless", "while", "until" };

    readonly List<Token> toks;
    int p;
    Scope scope;
    int noDo;   // >0 while parsing command args / loop conditions: `do` belongs to the outer call

    Parser(List<Token> tokens, Scope s)
    {
        toks = tokens;
        scope = s;
    }

    public static SeqNode Parse(string source) => new Parser(new Lexer(source).Lex(), new Scope { Hard = true }).ParseProgram();

    SeqNode ParseProgram()
    {
        var body = ParseStatements();
        if (Tok.Type != T.EOF) throw Error("unexpected");
        return body;
    }

    // ---- token helpers ----

    Token Tok => toks[p];
    Token PeekTok(int n = 1) => toks[Math.Min(p + n, toks.Count - 1)];
    bool IsOp(string s) => Tok.Type == T.Op && Tok.Text == s;
    bool IsKw(string s) => Tok.Type == T.Kw && Tok.Text == s;
    bool AcceptOp(string s) { if (!IsOp(s)) return false; p++; return true; }
    bool AcceptKw(string s) { if (!IsKw(s)) return false; p++; return true; }
    void ExpectOp(string s) { if (!AcceptOp(s)) throw Error($"expected '{s}'"); }
    void ExpectKw(string s) { if (!AcceptKw(s)) throw Error($"expected '{s}'"); }
    bool AtTerm => Tok.Type is T.Newline or T.EOF || IsOp(";");
    void SkipNewlines() { while (Tok.Type == T.Newline) p++; }
    void SkipTerms() { while (Tok.Type == T.Newline || IsOp(";")) p++; }

    string ExpectIdent()
    {
        if (Tok.Type != T.Ident) throw Error("expected identifier");
        return toks[p++].Text;
    }

    RubySyntaxError Error(string msg)
    {
        var t = Tok;
        string got = t.Type switch { T.Newline => "newline", T.EOF => "end of file", T.Str => "string", _ => $"'{t.Text}'" };
        return new RubySyntaxError($"{msg} (got {got})", t.Line);
    }

    void PushScope(bool hard) => scope = new Scope { Parent = scope, Hard = hard };
    void PopScope() => scope = scope.Parent;
    void Declare(string name) => scope.Vars.Add(name);

    bool IsLocal(string name)
    {
        for (var s = scope; s != null; s = s.Parent)
        {
            if (s.Vars.Contains(name)) return true;
            if (s.Hard) break;
        }
        return false;
    }

    // ---- statements ----

    bool AtBodyEnd() => Tok.Type == T.EOF
        || (Tok.Type == T.Kw && Tok.Text is "end" or "else" or "elsif" or "when" or "rescue" or "ensure")
        || (Tok.Type == T.Op && Tok.Text is "}" or ")" or "]");

    SeqNode ParseStatements()
    {
        var seq = new SeqNode { Line = Tok.Line };
        int saved = noDo;
        noDo = 0;
        SkipTerms();
        while (!AtBodyEnd())
        {
            seq.Body.Add(ParseStatement());
            if (!AtTerm && !AtBodyEnd()) throw Error("unexpected");
            SkipTerms();
        }
        noDo = saved;
        return seq;
    }

    Node ParseStatement()
    {
        int line = Tok.Line;
        Node n = Tok.Type == T.Ident && Tok.Text == "alias" && PeekTok().SpaceBefore && PeekTok().Type != T.Newline
            ? ParseAlias()
            : ParseExprStmt();
        if (IsOp(",") && IsAssignable(n)) n = ParseMultiAssign(n);
        else if (IsOp(",") && n is AssignNode { Value: not SplatNode } single)
        {
            var arr = new ArrayNode { Line = single.Value.Line, Elems = { single.Value } };
            while (AcceptOp(",")) { SkipNewlines(); arr.Elems.Add(ParseRhsItem()); }
            single.Value = arr;
        }
        while (true)
        {
            if (AcceptKw("if")) n = new IfNode { Cond = ParseModifierCond(), Then = n, Line = line };
            else if (AcceptKw("unless")) n = new IfNode { Cond = new NotNode { Value = ParseModifierCond(), Line = line }, Then = n, Line = line };
            else if (AcceptKw("while")) n = new WhileNode { Cond = ParseModifierCond(), Body = n, Line = line };
            else if (AcceptKw("until")) n = new WhileNode { Cond = ParseModifierCond(), Body = n, Until = true, Line = line };
            else if (AcceptKw("rescue"))
            {
                var fallback = ParseModifierCond();
                if (n is AssignNode a) a.Value = RescueMod(a.Value, fallback);
                else n = RescueMod(n, fallback);
            }
            else break;
        }
        return n;
    }

    Node ParseModifierCond()
    {
        SkipNewlines();
        return ParseExprStmt();
    }

    static BeginNode RescueMod(Node body, Node fallback) => new()
    {
        Body = body,
        Rescues = { new RescueClause { Body = fallback } },
        Line = body.Line,
    };

    Node ParseAlias()
    {
        int line = Tok.Line;
        p++;
        var a = new SymNode { Name = toks[p++].Text, Line = line };
        var b = new SymNode { Name = toks[p++].Text, Line = line };
        return new CallNode { Name = "alias_method", Args = { a, b }, Line = line };
    }

    Node ParseMultiAssign(Node first)
    {
        var m = new MultiAssignNode { Line = first.Line };
        m.Targets.Add(ToTarget(first));
        while (AcceptOp(",") && !IsOp("="))
        {
            if (AcceptOp("*")) m.Targets.Add(new SplatNode { Value = ToTarget(ParsePostfix()) });
            else m.Targets.Add(ToTarget(ParsePostfix()));
        }
        ExpectOp("=");
        SkipNewlines();
        var first2 = ParseRhsItem();
        if (!IsOp(",")) { m.Value = first2; return m; }
        var arr = new ArrayNode { Line = first2.Line, Elems = { first2 } };
        while (AcceptOp(",")) { SkipNewlines(); arr.Elems.Add(ParseRhsItem()); }
        m.Value = arr;
        return m;
    }

    Node ParseRhsItem() => AcceptOp("*") ? new SplatNode { Value = ParseTernary() } : ParseExpr();

    // and / or / not
    Node ParseExprStmt()
    {
        var left = ParseNotExpr();
        while (IsKw("and") || IsKw("or"))
        {
            bool and = Tok.Text == "and";
            int line = Tok.Line;
            p++;
            SkipNewlines();
            var right = ParseNotExpr();
            left = and ? new AndNode { Left = left, Right = right, Line = line } : new OrNode { Left = left, Right = right, Line = line };
        }
        return left;
    }

    Node ParseNotExpr()
    {
        if (!IsKw("not")) return ParseExpr();
        int line = Tok.Line;
        p++;
        return new NotNode { Value = ParseNotExpr(), Line = line };
    }

    // ---- expressions ----

    static bool IsIdentName(string s) => s.Length > 0 && (char.IsLetter(s[0]) || s[0] == '_');

    static bool IsAssignable(Node n) => n switch
    {
        LocalNode or IVarNode or GVarNode or ConstNode => true,
        CallNode c => c.Block == null && !c.HasParens && (c.Recv == null
            ? c.Args.Count == 0 && IsIdentName(c.Name) && !c.Name.EndsWith('?') && !c.Name.EndsWith('!')
            : c.Name == "[]" || (c.Args.Count == 0 && IsIdentName(c.Name))),
        _ => false,
    };

    Node ToTarget(Node n)
    {
        if (n is CallNode { Recv: null } c && c.Args.Count == 0)
        {
            Declare(c.Name);
            return new LocalNode { Name = c.Name, Line = c.Line };
        }
        if (!IsAssignable(n)) throw new RubySyntaxError("can't assign to this", n.Line);
        return n;
    }

    // assignment level
    Node ParseExpr()
    {
        int line = Tok.Line;
        var left = ParseTernary();
        if (Tok.Type != T.Op || !IsAssignable(left)) return left;
        if (Tok.Text == "=")
        {
            p++;
            SkipNewlines();
            var target = ToTarget(left);
            return new AssignNode { Target = target, Value = ParseRhsItem(), Line = line };
        }
        if (OpAssignOps.Contains(Tok.Text))
        {
            string op = Tok.Text[..^1];
            p++;
            SkipNewlines();
            var target = ToTarget(left);
            return new OpAssignNode { Target = target, Op = op, Value = ParseExpr(), Line = line };
        }
        return left;
    }

    Node ParseTernary()
    {
        var cond = ParseRange();
        if (!IsOp("?")) return cond;
        int line = Tok.Line;
        p++;
        SkipNewlines();
        var a = ParseExpr();
        SkipNewlines();
        ExpectOp(":");
        SkipNewlines();
        var b = ParseExpr();
        return new IfNode { Cond = cond, Then = a, Else = b, Line = line };
    }

    bool CanStartValue()
    {
        var t = Tok;
        return t.Type switch
        {
            T.Newline or T.EOF => false,
            T.Kw => ValueKeywords.Contains(t.Text),
            T.Op => t.Text is "(" or "[" or "{" or "-" or "!" or "::" or "->" or "*" or "~",
            _ => true,
        };
    }

    Node ParseRange()
    {
        var left = ParseOrOp();
        if (!IsOp("..") && !IsOp("...")) return left;
        bool excl = Tok.Text == "...";
        int line = Tok.Line;
        p++;
        var right = CanStartValue() ? ParseOrOp() : null;
        return new RangeNode { From = left, To = right, Exclusive = excl, Line = line };
    }

    Node Binary(Func<Node> next, params string[] ops)
    {
        var left = next();
        while (Tok.Type == T.Op && Array.IndexOf(ops, Tok.Text) >= 0)
        {
            string op = Tok.Text;
            int line = Tok.Line;
            p++;
            SkipNewlines();
            var right = next();
            left = op switch
            {
                "||" => new OrNode { Left = left, Right = right, Line = line },
                "&&" => new AndNode { Left = left, Right = right, Line = line },
                _ => new CallNode { Recv = left, Name = op, Args = { right }, Line = line },
            };
        }
        return left;
    }

    Node ParseOrOp() => Binary(ParseAndOp, "||");
    Node ParseAndOp() => Binary(ParseEquality, "&&");
    Node ParseEquality() => Binary(ParseComparison, "<=>", "==", "===", "!=", "=~", "!~");
    Node ParseComparison() => Binary(ParseBitOr, "<", "<=", ">", ">=");
    Node ParseBitOr() => Binary(ParseBitAnd, "|", "^");
    Node ParseBitAnd() => Binary(ParseShift, "&");
    Node ParseShift() => Binary(ParseAdditive, "<<", ">>");
    Node ParseAdditive() => Binary(ParseMultiplicative, "+", "-");
    Node ParseMultiplicative() => Binary(ParseUnaryMinus, "*", "/", "%");

    Node ParseUnaryMinus()
    {
        if (!IsOp("-")) return ParsePow();
        int line = Tok.Line;
        p++;
        var v = ParseUnaryMinus();
        return v switch
        {
            IntNode i => new IntNode { Value = -i.Value, Line = line },
            BigIntNode i => Interp.Norm(-i.Value) is long nl ? new IntNode { Value = nl, Line = line } : new BigIntNode { Value = -i.Value, Line = line },
            FloatNode f => new FloatNode { Value = -f.Value, Line = line },
            _ => new CallNode { Recv = v, Name = "-@", Line = line },
        };
    }

    Node ParsePow()
    {
        var b = ParseUnary();
        if (!IsOp("**")) return b;
        int line = Tok.Line;
        p++;
        SkipNewlines();
        return new CallNode { Recv = b, Name = "**", Args = { ParseUnaryMinus() }, Line = line };
    }

    Node ParseUnary()
    {
        int line = Tok.Line;
        if (AcceptOp("!")) return new NotNode { Value = ParseUnary(), Line = line };
        if (AcceptOp("~")) return new CallNode { Recv = ParseUnary(), Name = "~", Line = line };
        if (AcceptOp("+")) return ParseUnary();
        return ParsePostfix();
    }

    Node ParsePostfix()
    {
        var n = ParsePrimary();
        while (true)
        {
            if (Tok.Type == T.Newline)
            {
                // leading-dot method chains
                int q = p;
                while (toks[q].Type == T.Newline) q++;
                if (toks[q].Type == T.Op && toks[q].Text is "." or "&.") p = q;
                else break;
            }
            if (IsOp(".") || IsOp("&."))
            {
                bool safe = Tok.Text == "&.";
                p++;
                SkipNewlines();
                n = ParseMethodCall(n, safe);
            }
            else if (IsOp("::"))
            {
                p++;
                if (Tok.Type == T.Const && !(PeekTok() is { Type: T.Op, Text: "(" } nx && !nx.SpaceBefore))
                {
                    n = new ConstNode { Scope = n, Name = Tok.Text, Line = Tok.Line };
                    p++;
                }
                else n = ParseMethodCall(n, false);
            }
            // After a block's closing brace or `end`, `[` indexes the result even after a space (openshift-cli: `find { } ["x"]`).
            else if (IsOp("[") && (!Tok.SpaceBefore || n is CallNode { Block: not null }))
            {
                int line = Tok.Line;
                p++;
                n = new CallNode { Recv = n, Name = "[]", Args = ParseArgList("]"), Line = line };
            }
            else break;
        }
        return n;
    }

    Node ParseMethodCall(Node recv, bool safe)
    {
        var t = Tok;
        string name;
        if (t.Type is T.Ident or T.Const or T.Kw) { name = t.Text; p++; }
        else if (t.Type == T.Op && t.Text == "(") name = "call";   // recv.()
        else if (t.Type == T.Op && t.Text == "[" && PeekTok() is { Type: T.Op, Text: "]" })
        {
            p += 2;
            name = "[]";
            if (IsOp("=") && !Tok.SpaceBefore && PeekTok() is { Type: T.Op, Text: "(" }) { p++; name = "[]="; }
        }
        else if (t.Type == T.Op && t.Text is "+" or "-" or "*" or "/" or "%" or "**" or "==" or "!=" or "<" or ">" or "<=" or ">=" or "<=>" or "===" or "<<" or "!")
        { name = t.Text; p++; }
        else throw Error("expected method name");
        var call = new CallNode { Recv = recv, Name = name, SafeNav = safe, Line = t.Line };
        ParseCallRest(call);
        return call;
    }

    void ParseCallRest(CallNode call)
    {
        if (IsOp("(") && !Tok.SpaceBefore)
        {
            p++;
            call.HasParens = true;
            call.Args = ParseArgList(")");
        }
        else if (CanStartCommandArg())
        {
            call.Args = ParseCommandArgs();
        }
        call.Block = ParseBlockOpt();
    }

    // Is the current token (after an identifier) the start of a paren-less argument list?
    bool CanStartCommandArg()
    {
        var t = Tok;
        if (!t.SpaceBefore) return false;
        switch (t.Type)
        {
            case T.Str: case T.XStr: case T.Int: case T.Float: case T.Symbol: case T.IVar: case T.GVar:
            case T.Const: case T.Ident: case T.Words: case T.Regex:
                return true;
            case T.Kw:
                return t.Text is "nil" or "true" or "false" or "self" or "not" or "__FILE__" or "__dir__" or "super" or "yield" or "def" or "case" or "begin";
            case T.Op:
            {
                var nx = PeekTok();
                return t.Text switch
                {
                    "[" or "(" or "->" or "::" => true,
                    "-" or "*" or "**" or "&" or "!" or ":" => !nx.SpaceBefore && nx.Type != T.Newline,
                    _ => false,
                };
            }
        }
        return false;
    }

    List<Node> ParseCommandArgs()
    {
        noDo++;
        var args = new List<Node>();
        HashNode bare = null;
        while (true)
        {
            ParseOneArg(args, ref bare);
            if (!IsOp(",")) break;
            p++;
            SkipNewlines();
        }
        noDo--;
        return args;
    }

    // After the opening bracket; consumes the closer.
    List<Node> ParseArgList(string closer)
    {
        int saved = noDo;
        noDo = 0;
        var args = new List<Node>();
        HashNode bare = null;
        SkipNewlines();
        while (!IsOp(closer))
        {
            ParseOneArg(args, ref bare);
            SkipNewlines();
            if (!AcceptOp(",")) break;
            SkipNewlines();
        }
        SkipNewlines();
        ExpectOp(closer);
        noDo = saved;
        return args;
    }

    void ParseOneArg(List<Node> args, ref HashNode bare)
    {
        int line = Tok.Line;
        if (AcceptOp("*")) { args.Add(new SplatNode { Value = ParseTernary(), Line = line }); return; }
        if (AcceptOp("**"))
        {
            bare ??= NewBareHash(args, line);
            bare.Pairs.Add((new DoubleSplatNode { Value = ParseTernary(), Line = line }, null));
            return;
        }
        if (AcceptOp("&"))
        {
            args.Add(new BlockPassNode { Value = IsOp(")") || IsOp(",") ? null : ParseTernary(), Line = line });
            return;
        }
        var v = ParseNotExpr();
        if (!AcceptOp("=>")) { args.Add(v); return; }
        bare ??= NewBareHash(args, line);
        var shorthand = ShorthandValue(v);
        if (shorthand == null) SkipNewlines();
        bare.Pairs.Add((v, shorthand ?? ParseNotExpr()));
    }

    static HashNode NewBareHash(List<Node> args, int line)
    {
        var h = new HashNode { Line = line };
        args.Add(h);
        return h;
    }

    // `{x:, y:}` – Ruby 3.1 value omission
    Node ShorthandValue(Node key)
    {
        if (key is not SymNode s || !(IsOp(",") || IsOp("}") || IsOp(")") || Tok.Type == T.Newline)) return null;
        return IsLocal(s.Name) ? new LocalNode { Name = s.Name, Line = s.Line } : new CallNode { Name = s.Name, Line = s.Line };
    }

    BlockNode ParseBlockOpt()
    {
        if (IsOp("{")) return ParseBlock(true);
        if (IsKw("do") && noDo == 0) return ParseBlock(false);
        return null;
    }

    BlockNode ParseBlock(bool brace)
    {
        var b = new BlockNode { Line = Tok.Line };
        p++;
        int saved = noDo;
        noDo = 0;
        PushScope(false);
        if (AcceptOp("||")) { }
        else if (AcceptOp("|")) b.Params = ParseParamList("|");
        if (brace) { b.Body = ParseStatements(); ExpectOp("}"); }
        else { b.Body = ParseBodyWithRescue(); ExpectKw("end"); }
        PopScope();
        noDo = saved;
        return b;
    }

    // closer: ")" / "|" / null (ends at newline, `=` or `{`)
    List<Param> ParseParamList(string closer)
    {
        var list = new List<Param>();
        bool AtEnd() => closer != null ? IsOp(closer) : AtTerm || IsOp("=") || IsOp("{") || IsKw("do");
        if (closer != null) SkipNewlines();
        while (!AtEnd())
        {
            var prm = new Param();
            if (AcceptOp("*")) { prm.Splat = true; if (Tok.Type == T.Ident) prm.Name = toks[p++].Text; }
            else if (AcceptOp("**")) { prm.DoubleSplat = true; if (Tok.Type == T.Ident) prm.Name = toks[p++].Text; else AcceptKw("nil"); }
            else if (AcceptOp("&")) { prm.Block = true; if (Tok.Type == T.Ident) prm.Name = toks[p++].Text; }
            else if (AcceptOp("...")) { prm.Splat = true; prm.Name = "..."; }
            else if (AcceptOp("(")) prm.Destructure = ParseParamList(")");
            else if (Tok.Type == T.Symbol && Tok.Parts == null && PeekTok() is { Type: T.Op, Text: "=>" })
            {
                prm.Keyword = true;
                prm.Name = Tok.Text;
                p += 2;
                if (IsOp(",") || AtEnd() || Tok.Type == T.Newline) prm.Required = true;
                else prm.Default = closer == "|" ? ParseAdditive() : ParseTernary();
            }
            else
            {
                prm.Name = ExpectIdent();
                if (AcceptOp("=")) prm.Default = closer == "|" ? ParseAdditive() : ParseTernary();
                else prm.Required = true;
            }
            if (prm.Name != null) Declare(prm.Name);
            list.Add(prm);
            if (closer != null) SkipNewlines();
            if (!AcceptOp(",")) break;
            if (closer != null) SkipNewlines();
        }
        if (closer == "|" && AcceptOp(";")) { do Declare(ExpectIdent()); while (AcceptOp(",")); }   // block-locals
        if (closer != null) { SkipNewlines(); ExpectOp(closer); }
        return list;
    }

    // ---- primaries ----

    Node ParsePrimary()
    {
        var t = Tok;
        int line = t.Line;
        switch (t.Type)
        {
            case T.Int:
                p++;
                return long.TryParse(t.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                    ? new IntNode { Value = l, Line = line }
                    : new BigIntNode { Value = System.Numerics.BigInteger.Parse(t.Text, CultureInfo.InvariantCulture), Line = line };
            case T.Float:
                p++;
                return new FloatNode { Value = double.Parse(t.Text, CultureInfo.InvariantCulture), Line = line };
            case T.Str:
                return ParseStringLiteral();
            case T.XStr:
                p++;
                return new XStrNode { Str = MakeStr(t), Line = line };
            case T.Regex:
                p++;
                return new RegexNode { Str = MakeStr(t), Flags = t.Text, Line = line };
            case T.Symbol:
                p++;
                return t.Parts != null ? new DSymNode { Str = MakeStr(t), Line = line } : new SymNode { Name = t.Text, Line = line };
            case T.Words:
            {
                p++;
                var arr = new ArrayNode { Line = line };
                if (t.WordParts != null)
                {
                    foreach (var word in t.WordParts)
                    {
                        var str = MakeStr(new Token { Parts = word, Line = t.Line });
                        arr.Elems.Add(t.Text == "i" ? new DSymNode { Str = str, Line = line } : str);
                    }
                    return arr;
                }
                foreach (var w in t.WordList)
                    arr.Elems.Add(t.Text == "i" ? new SymNode { Name = w, Line = line } : new StrNode { Parts = { w }, Line = line });
                return arr;
            }
            case T.IVar:
                p++;
                return new IVarNode { Name = t.Text, Line = line };
            case T.GVar:
                p++;
                return new GVarNode { Name = t.Text, Line = line };
            case T.Const:
            {
                p++;
                if (IsOp("(") && !Tok.SpaceBefore)
                {
                    var c = new CallNode { Name = t.Text, Line = line };
                    ParseCallRest(c);
                    return c;
                }
                return new ConstNode { Name = t.Text, Line = line };
            }
            case T.Ident:
                return ParseIdentifier();
            case T.Kw:
                return ParseKeyword();
            case T.Op:
                return ParseOpPrimary();
        }
        throw Error("unexpected");
    }

    Node ParseStringLiteral()
    {
        var s = MakeStr(Tok);
        p++;
        while (Tok.Type == T.Str)   // "a" "b" concatenation
        {
            s.Parts.AddRange(MakeStr(Tok).Parts);
            p++;
        }
        return s;
    }

    StrNode MakeStr(Token t)
    {
        var s = new StrNode { Line = t.Line };
        foreach (var part in t.Parts)
        {
            if (part.Code != null)
            {
                var sub = new Lexer(part.Code).Lex();
                foreach (var st in sub) st.Line += part.Line - 1;
                s.Parts.Add(new Parser(sub, scope).ParseProgram());
            }
            else if (part.Text.Length > 0) s.Parts.Add(part.Text);
        }
        return s;
    }

    Node ParseIdentifier()
    {
        var t = Tok;
        int line = t.Line;
        p++;
        if (t.Text == "defined?")
        {
            Node v;
            if (AcceptOp("(")) { SkipNewlines(); v = ParseExprStmt(); SkipNewlines(); ExpectOp(")"); }
            else v = ParseExpr();
            return new DefinedNode { Value = v, Line = line };
        }
        bool parenCall = IsOp("(") && !Tok.SpaceBefore;
        // a local followed by a literal (`ohai "x"`) is still a method call
        if (!parenCall && IsLocal(t.Text) && !(Tok.SpaceBefore && Tok.Type is T.Str or T.XStr or T.Int or T.Float or T.Symbol or T.Ident or T.Const or T.IVar or T.Words))
            return new LocalNode { Name = t.Text, Line = line };
        var call = new CallNode { Name = t.Text, Line = line };
        ParseCallRest(call);
        return call;
    }

    Node ParseKeyword()
    {
        var t = Tok;
        int line = t.Line;
        switch (t.Text)
        {
            case "nil": p++; return new NilNode { Line = line };
            case "true": p++; return new TrueNode { Line = line };
            case "false": p++; return new FalseNode { Line = line };
            case "self": p++; return new SelfNode { Line = line };
            case "__FILE__": case "__dir__": p++; return new CallNode { Name = t.Text, Line = line };
            case "if": return ParseIf(false);
            case "unless": return ParseIf(true);
            case "while": case "until": return ParseWhile();
            case "case": return ParseCase();
            case "for": return ParseFor();
            case "begin":
            {
                p++;
                var body = ParseBodyWithRescue();
                ExpectKw("end");
                return body as BeginNode ?? new BeginNode { Body = body, Line = line };
            }
            case "def": return ParseDef();
            case "class": return ParseClass(false);
            case "module": return ParseClass(true);
            case "return": case "next": case "break": return ParseJump();
            case "yield":
            {
                p++;
                var y = new YieldNode { Line = line };
                if (IsOp("(") && !Tok.SpaceBefore) { p++; y.Args = ParseArgList(")"); }
                else if (CanStartCommandArg()) y.Args = ParseCommandArgs();
                return y;
            }
            case "super":
            {
                p++;
                var s = new SuperNode { Line = line };
                if (IsOp("(") && !Tok.SpaceBefore) { p++; s.Args = ParseArgList(")"); }
                else if (CanStartCommandArg()) s.Args = ParseCommandArgs();
                s.Block = ParseBlockOpt();
                return s;
            }
            case "not":
                p++;
                return new NotNode { Value = ParseExpr(), Line = line };
        }
        throw Error("unexpected keyword");
    }

    Node ParseIf(bool unless)
    {
        int line = Tok.Line;
        p++;
        var cond = ParseExprStmt();
        AcceptKw("then");
        var node = new IfNode { Cond = unless ? new NotNode { Value = cond, Line = line } : cond, Then = ParseStatements(), Line = line };
        var cur = node;
        while (!unless && IsKw("elsif"))
        {
            int l2 = Tok.Line;
            p++;
            var c2 = ParseExprStmt();
            AcceptKw("then");
            var n2 = new IfNode { Cond = c2, Then = ParseStatements(), Line = l2 };
            cur.Else = n2;
            cur = n2;
        }
        if (AcceptKw("else")) cur.Else = ParseStatements();
        ExpectKw("end");
        return node;
    }

    Node ParseWhile()
    {
        int line = Tok.Line;
        bool until = Tok.Text == "until";
        p++;
        noDo++;
        var cond = ParseExprStmt();
        noDo--;
        AcceptKw("do");
        var body = ParseStatements();
        ExpectKw("end");
        return new WhileNode { Cond = cond, Body = body, Until = until, Line = line };
    }

    Node ParseFor()
    {
        int line = Tok.Line;
        p++;
        var vars = new List<string>();
        do { var v = ExpectIdent(); Declare(v); vars.Add(v); } while (AcceptOp(","));
        ExpectKw("in");
        noDo++;
        var iter = ParseExprStmt();
        noDo--;
        AcceptKw("do");
        var body = ParseStatements();
        ExpectKw("end");
        return new ForNode { Vars = vars, Iter = iter, Body = body, Line = line };
    }

    Node ParseCase()
    {
        int line = Tok.Line;
        p++;
        var node = new CaseNode { Line = line, Subject = AtTerm ? null : ParseExprStmt() };
        SkipTerms();
        if (IsKw("in")) throw Error("pattern matching is not supported");
        while (AcceptKw("when"))
        {
            var conds = new List<Node>();
            do
            {
                SkipNewlines();
                if (AcceptOp("*")) conds.Add(new SplatNode { Value = ParseTernary(), Line = Tok.Line });
                else conds.Add(ParseTernary());
            } while (AcceptOp(","));
            AcceptKw("then");
            node.Whens.Add((conds, ParseStatements()));
        }
        if (AcceptKw("else")) node.Else = ParseStatements();
        ExpectKw("end");
        return node;
    }

    Node ParseBodyWithRescue()
    {
        int line = Tok.Line;
        var body = ParseStatements();
        if (!IsKw("rescue") && !IsKw("ensure")) return body;
        var b = new BeginNode { Body = body, Line = line };
        while (AcceptKw("rescue"))
        {
            var rc = new RescueClause();
            if (!AtTerm && !IsOp("=>") && !IsKw("then"))
                do
                {
                    SkipNewlines();
                    rc.Classes.Add(AcceptOp("*") ? new SplatNode { Value = ParseTernary(), Line = Tok.Line } : ParseTernary());
                } while (AcceptOp(","));
            if (AcceptOp("=>")) { rc.Var = ExpectIdent(); Declare(rc.Var); }
            AcceptKw("then");
            rc.Body = ParseStatements();
            b.Rescues.Add(rc);
        }
        if (AcceptKw("else")) b.ElseBody = ParseStatements();
        if (AcceptKw("ensure")) b.Ensure = ParseStatements();
        return b;
    }

    Node ParseDef()
    {
        int line = Tok.Line;
        p++;
        var d = new DefNode { Line = line };
        if (IsKw("self") && PeekTok() is { Type: T.Op, Text: "." }) { p += 2; d.Singleton = true; }
        var t = Tok;
        if (t.Type is T.Ident or T.Const or T.Kw)
        {
            p++;
            d.Name = t.Text;
            if (IsOp("=") && !Tok.SpaceBefore && PeekTok() is { Type: T.Op, Text: "(" } && !PeekTok().SpaceBefore) { p++; d.Name += "="; }
        }
        else if (t.Type == T.Op && t.Text == "[")
        {
            p++;
            ExpectOp("]");
            d.Name = "[]";
            if (IsOp("=") && !Tok.SpaceBefore) { p++; d.Name = "[]="; }
        }
        else if (t.Type == T.Op) { p++; d.Name = t.Text; }
        else throw Error("expected method name");

        PushScope(true);
        if (AcceptOp("(")) { d.Params = ParseParamList(")"); }
        else d.Params = ParseParamList(null);
        if (AcceptOp("="))   // endless def
        {
            SkipNewlines();
            d.Body = ParseStatement();
        }
        else
        {
            d.Body = ParseBodyWithRescue();
            ExpectKw("end");
        }
        PopScope();
        return d;
    }

    Node ParseClass(bool isModule)
    {
        int line = Tok.Line;
        p++;
        var c = new ClassNode { IsModule = isModule, Line = line };
        if (!isModule && AcceptOp("<<"))
        {
            ParseExpr();   // only `class << self` is supported
            c.SelfClass = true;
        }
        else
        {
            ConstNode path = null;
            bool top = AcceptOp("::");
            do
            {
                if (Tok.Type != T.Const) throw Error("expected constant name");
                path = new ConstNode { Scope = path, Name = Tok.Text, TopLevel = top && path == null, Line = Tok.Line };
                p++;
            } while (AcceptOp("::"));
            c.Path = path;
            if (!isModule && AcceptOp("<")) c.Super = ParseExpr();
        }
        PushScope(true);
        c.Body = ParseBodyWithRescue();
        PopScope();
        ExpectKw("end");
        return c;
    }

    Node ParseJump()
    {
        var t = Tok;
        p++;
        Node v = null;
        if (CanStartValue() && !(Tok.Type == T.Kw && Tok.Text is "if" or "unless" or "while" or "until"))
        {
            v = ParseExpr();
            if (IsOp(","))
            {
                var arr = new ArrayNode { Line = v.Line, Elems = { v } };
                while (AcceptOp(",")) { SkipNewlines(); arr.Elems.Add(ParseRhsItem()); }
                v = arr;
            }
        }
        return t.Text switch
        {
            "return" => new ReturnNode { Value = v, Line = t.Line },
            "next" => new NextNode { Value = v, Line = t.Line },
            _ => new BreakNode { Value = v, Line = t.Line },
        };
    }

    Node ParseOpPrimary()
    {
        var t = Tok;
        int line = t.Line;
        switch (t.Text)
        {
            case "(":
            {
                p++;
                int saved = noDo;
                noDo = 0;
                var seq = ParseStatements();
                ExpectOp(")");
                noDo = saved;
                return seq.Body.Count switch { 0 => new NilNode { Line = line }, 1 => seq.Body[0], _ => seq };
            }
            case "[":
                p++;
                return new ArrayNode { Elems = ParseArgList("]"), Line = line };
            case "{":
                return ParseHash();
            case "->":
                return ParseLambda();
            case "::":
                p++;
                if (Tok.Type != T.Const) throw Error("expected constant");
                return new ConstNode { Name = toks[p++].Text, TopLevel = true, Line = line };
            case "..": case "...":
                p++;
                return new RangeNode { From = null, To = ParseOrOp(), Exclusive = t.Text == "...", Line = line };
        }
        throw Error("unexpected");
    }

    Node ParseHash()
    {
        var h = new HashNode { Braces = true, Line = Tok.Line };
        p++;
        int saved = noDo;
        noDo = 0;
        SkipNewlines();
        while (!IsOp("}"))
        {
            if (AcceptOp("**")) h.Pairs.Add((new DoubleSplatNode { Value = ParseTernary() }, null));
            else
            {
                var k = ParseNotExpr();
                if (k is StrNode s && IsOp(":") && !Tok.SpaceBefore) { p++; k = new DSymNode { Str = s, Line = s.Line }; }   // "a": 1
                else ExpectOp("=>");
                var shorthand = ShorthandValue(k);
                if (shorthand == null) SkipNewlines();
                h.Pairs.Add((k, shorthand ?? ParseNotExpr()));
            }
            SkipNewlines();
            if (!AcceptOp(",")) break;
            SkipNewlines();
        }
        SkipNewlines();
        ExpectOp("}");
        noDo = saved;
        return h;
    }

    Node ParseLambda()
    {
        int line = Tok.Line;
        p++;
        PushScope(false);
        var b = new BlockNode { Line = line };
        if (AcceptOp("(")) b.Params = ParseParamList(")");
        else if (Tok.Type == T.Ident) b.Params = ParseParamList(null);
        int saved = noDo;
        noDo = 0;
        if (AcceptOp("{")) { b.Body = ParseStatements(); ExpectOp("}"); }
        else { ExpectKw("do"); b.Body = ParseBodyWithRescue(); ExpectKw("end"); }
        noDo = saved;
        PopScope();
        return new LambdaNode { Block = b, Line = line };
    }
}
