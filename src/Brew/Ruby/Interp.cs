using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

public sealed partial class Interp
{
    public RClass BasicObjectClass, ObjectClass, ModuleClass, ClassClass, KernelModule, ComparableModule, EnumerableModule,
        NilClass, TrueClass, FalseClass, NumericClass, IntegerClass, FloatClass, StringClass, SymbolClass, ArrayClass, HashClass,
        RangeClass, ProcClass, RegexpClass, MatchDataClass,
        ExceptionClass, ScriptError, NotImplementedError, LoadError, SyntaxErrorClass, StandardError, RuntimeError, ArgumentError,
        TypeError, NameError, NoMethodError, IndexError, KeyError, StopIteration, RangeError, ZeroDivisionError, FrozenError,
        IOError, LocalJumpError, SystemCallError, ErrnoModule, ENOENT, EEXIST, EACCES, SystemExit, SystemStackError;

    public readonly RObject Main;
    public readonly Dictionary<string, object> Globals = new();
    public string Cwd = Directory.GetCurrentDirectory();   // virtual cwd; never changes the process's
    public TextWriter Out = Console.Out, Err = Console.Error;
    public string CurrentFile = "-";
    public int CurLine;
    public Frame Caller;   // frame of the call being dispatched (for natives like block_given?)
    readonly Frame top;
    readonly Dictionary<(RClass, string), RMethod> methodCache = new();
    readonly Dictionary<RegexNode, RRegex> regexCache = new();
    int depth;

    public static readonly object Unset = new();

    public Interp()
    {
        Bootstrap();
        Main = new RObject { Class = ObjectClass };
        top = new Frame { Self = Main, DefTarget = ObjectClass, Cref = new() { ObjectClass } };
        top.MethodFrame = top;
        InitCore();
        InitStrings();
        InitCollections();
        InitFiles();
        InitJson();
        Run(EncodingPrelude, "<encoding>");
    }

    public object Run(string source, string file)
    {
        CurrentFile = file;
        var ast = Parser.Parse(source);
        return Eval(ast, top);
    }

    // ---- classes ----

    void Bootstrap()
    {
        BasicObjectClass = new RClass { Name = "BasicObject" };
        ObjectClass = new RClass { Name = "Object", Super = BasicObjectClass };
        ModuleClass = new RClass { Name = "Module", Super = ObjectClass };
        ClassClass = new RClass { Name = "Class", Super = ModuleClass };
        foreach (var c in new[] { BasicObjectClass, ObjectClass, ModuleClass, ClassClass })
        {
            c.Class = ClassClass;
            ObjectClass.Consts[c.Name] = c;
        }
        KernelModule = DefineModule("Kernel");
        ObjectClass.Includes.Add(KernelModule);
        ComparableModule = DefineModule("Comparable");
        EnumerableModule = DefineModule("Enumerable");
        NilClass = DefineClass("NilClass");
        TrueClass = DefineClass("TrueClass");
        FalseClass = DefineClass("FalseClass");
        NumericClass = DefineClass("Numeric");
        NumericClass.Includes.Add(ComparableModule);
        IntegerClass = DefineClass("Integer", NumericClass);
        FloatClass = DefineClass("Float", NumericClass);
        StringClass = DefineClass("String");
        StringClass.Includes.Add(ComparableModule);
        SymbolClass = DefineClass("Symbol");
        SymbolClass.Includes.Add(ComparableModule);
        ArrayClass = DefineClass("Array");
        ArrayClass.Includes.Add(EnumerableModule);
        HashClass = DefineClass("Hash");
        HashClass.Includes.Add(EnumerableModule);
        RangeClass = DefineClass("Range");
        RangeClass.Includes.Add(EnumerableModule);
        ProcClass = DefineClass("Proc");
        RegexpClass = DefineClass("Regexp");
        MatchDataClass = DefineClass("MatchData");

        ExceptionClass = DefineClass("Exception");
        ScriptError = DefineClass("ScriptError", ExceptionClass);
        NotImplementedError = DefineClass("NotImplementedError", ScriptError);
        LoadError = DefineClass("LoadError", ScriptError);
        SyntaxErrorClass = DefineClass("SyntaxError", ScriptError);
        SystemExit = DefineClass("SystemExit", ExceptionClass);
        SystemStackError = DefineClass("SystemStackError", ExceptionClass);
        StandardError = DefineClass("StandardError", ExceptionClass);
        RuntimeError = DefineClass("RuntimeError", StandardError);
        ArgumentError = DefineClass("ArgumentError", StandardError);
        TypeError = DefineClass("TypeError", StandardError);
        NameError = DefineClass("NameError", StandardError);
        NoMethodError = DefineClass("NoMethodError", NameError);
        IndexError = DefineClass("IndexError", StandardError);
        KeyError = DefineClass("KeyError", IndexError);
        StopIteration = DefineClass("StopIteration", IndexError);
        RangeError = DefineClass("RangeError", StandardError);
        ZeroDivisionError = DefineClass("ZeroDivisionError", StandardError);
        FrozenError = DefineClass("FrozenError", RuntimeError);
        IOError = DefineClass("IOError", StandardError);
        LocalJumpError = DefineClass("LocalJumpError", StandardError);
        SystemCallError = DefineClass("SystemCallError", StandardError);
        ErrnoModule = DefineModule("Errno");
        ENOENT = DefineClass("ENOENT", SystemCallError, ErrnoModule);
        EEXIST = DefineClass("EEXIST", SystemCallError, ErrnoModule);
        EACCES = DefineClass("EACCES", SystemCallError, ErrnoModule);
    }

    public RClass DefineClass(string name, RClass super = null, RClass container = null)
    {
        container ??= ObjectClass;
        var c = new RClass
        {
            Name = container == ObjectClass ? name : $"{container.Name}::{name}",
            Super = super ?? ObjectClass,
            Class = ClassClass,
            LexicalParent = container,
        };
        container.Consts[name] = c;
        return c;
    }

    public RClass DefineModule(string name, RClass container = null)
    {
        container ??= ObjectClass;
        var c = new RClass
        {
            Name = container == ObjectClass ? name : $"{container.Name}::{name}",
            IsModule = true,
            Class = ModuleClass,
            LexicalParent = container,
        };
        container.Consts[name] = c;
        return c;
    }

    public RClass ClassOf(object o) => o switch
    {
        null => NilClass,
        bool b => b ? TrueClass : FalseClass,
        long => IntegerClass,
        double => FloatClass,
        RString => StringClass,
        RSymbol => SymbolClass,
        RArray => ArrayClass,
        RHash => HashClass,
        RRange => RangeClass,
        RProc => ProcClass,
        RRegex => RegexpClass,
        RMatch => MatchDataClass,
        RClass c => c.IsModule ? ModuleClass : ClassClass,
        RObject r => r.Class,
        _ => ObjectClass,
    };

    public RClass SingletonOf(RObject o)
    {
        if (o.Singleton != null) return o.Singleton;
        RClass super = o is RClass { IsModule: false } c
            ? (c.Super != null ? SingletonOf(c.Super) : ClassClass)
            : o.Class;
        o.Singleton = new RClass
        {
            Name = $"#<Class:{(o is RClass k ? k.Name : "#<" + o.Class.Name + ">")}>",
            Super = super,
            IsSingleton = true,
            Attached = o,
            Class = ClassClass,
        };
        return o.Singleton;
    }

    public RClass DispatchClass(object o) => o switch
    {
        RClass c => SingletonOf(c),
        RObject r => r.Singleton ?? r.Class,
        _ => ClassOf(o),
    };

    public IEnumerable<RClass> Ancestors(RClass c)
    {
        for (var k = c; k != null; k = k.Super)
        {
            yield return k;
            for (int i = k.Includes.Count - 1; i >= 0; i--)
                foreach (var m in ModuleAncestors(k.Includes[i]))
                    yield return m;
        }
    }

    IEnumerable<RClass> ModuleAncestors(RClass m)
    {
        yield return m;
        for (int i = m.Includes.Count - 1; i >= 0; i--)
            foreach (var x in ModuleAncestors(m.Includes[i]))
                yield return x;
    }

    public void InvalidateMethods() => methodCache.Clear();

    public RMethod FindMethod(RClass c, string name)
    {
        if (methodCache.TryGetValue((c, name), out var m)) return m;
        m = null;
        foreach (var a in Ancestors(c))
            if (a.Methods.TryGetValue(name, out m)) break;
        methodCache[(c, name)] = m;
        return m;
    }

    public bool RespondTo(object o, string name) => FindMethod(DispatchClass(o), name) is { } m && !(m.Fn == null && m.Def == null && m.Body == null);

    public bool IsA(object o, RClass c)
    {
        foreach (var a in Ancestors(DispatchClass(o)))
            if (a == c) return true;
        return false;
    }

    // ---- native definition helpers ----

    public void Def(RClass c, string names, Native fn)
    {
        foreach (var n in names.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            c.Methods[n] = new RMethod { Name = n, Fn = fn, Owner = c };
        InvalidateMethods();
    }

    public void DefS(RClass c, string names, Native fn) => Def(SingletonOf(c), names, fn);

    // ---- errors ----

    public RObject NewException(RClass cls, string msg)
    {
        var e = new RObject { Class = cls };
        e.SetIvar("@message", new RString(msg ?? cls.Name));
        return e;
    }

    public RubyError Error(RClass cls, string msg) => new(NewException(cls, msg));
    public RubyError ArgError(string msg) => Error(ArgumentError, msg);
    public RubyError TypeErr(string msg) => Error(TypeError, msg);

    public RubyError ArgCount(object[] args, int min, int max)
    {
        string exp = min == max ? min.ToString() : max < 0 ? $"{min}+" : $"{min}..{max}";
        return ArgError($"wrong number of arguments (given {args.Length}, expected {exp})");
    }

    public void Arity(object[] args, int min, int max = -2)
    {
        if (max == -2) max = min;
        if (args.Length < min || (max >= 0 && args.Length > max)) throw ArgCount(args, min, max);
    }

    public string Describe(object o) => o switch
    {
        null => "nil",
        true => "true",
        false => "false",
        RClass c => (c.IsModule ? "module " : "class ") + c.Name,
        _ when ReferenceEquals(o, Main) => "main",
        _ => "an instance of " + ClassOf(o).Name,
    };

    public RubyError NoMethod(object self, string name, bool vcall) => vcall
        ? Error(NameError, $"undefined local variable or method '{name}' for {Describe(self)}")
        : Error(NoMethodError, $"undefined method '{name}' for {Describe(self)}");

    public string TypeName(object o) => o switch { null => "nil", true => "true", false => "false", _ => ClassOf(o).Name };

    // ---- conversions ----

    public static bool Truthy(object o) => o != null && !(o is bool b && !b);

    public string Str(object o) => o switch
    {
        RString s => s.V,
        RObject r when r is not RClass && RespondTo(r, "to_str") && Invoke(r, "to_str", Array.Empty<object>(), null) is RString rs => rs.V,
        _ => throw TypeErr($"no implicit conversion of {TypeName(o)} into String"),
    };

    public long Int(object o) => o switch
    {
        long l => l,
        double d => (long)d,
        _ => throw TypeErr($"no implicit conversion of {TypeName(o)} into Integer"),
    };

    public double Num(object o) => o switch
    {
        long l => l,
        double d => d,
        _ => throw TypeErr($"can't convert {TypeName(o)} into Float"),
    };

    public RArray Ary(object o) => o as RArray ?? throw TypeErr($"no implicit conversion of {TypeName(o)} into Array");

    public string StrOrSym(object o) => o switch
    {
        RString s => s.V,
        RSymbol s => s.Name,
        _ => throw TypeErr($"{Inspect(o)} is not a symbol nor a string"),
    };

    public static RString S(string s) => new(s);
    public static RSymbol Sym(string s) => RSymbol.Get(s);

    public string ToS(object o)
    {
        switch (o)
        {
            case RString s: return s.V;
            case string str: return str;
            case null: return "";
            case long l: return l.ToString(CultureInfo.InvariantCulture);
            case double d: return FloatToS(d);
            case RSymbol y: return y.Name;
            case bool b: return b ? "true" : "false";
            case RClass c when FindMethod(DispatchClass(c), "to_s")?.Fn != null: return c.Name;
        }
        var r = Invoke(o, "to_s", Array.Empty<object>(), null);
        return r is RString rs ? rs.V : $"#<{ClassOf(o).Name}>";
    }

    public static string FloatToS(double d)
    {
        if (double.IsNaN(d)) return "NaN";
        if (double.IsPositiveInfinity(d)) return "Infinity";
        if (double.IsNegativeInfinity(d)) return "-Infinity";
        if (d == Math.Floor(d) && Math.Abs(d) < 1e16) return ((long)d).ToString(CultureInfo.InvariantCulture) + ".0";
        var s = d.ToString("R", CultureInfo.InvariantCulture);
        if (s.Contains('E'))
        {
            var parts = s.Split('E');
            var mant = parts[0].Contains('.') ? parts[0] : parts[0] + ".0";
            var exp = parts[1].StartsWith('-') ? "-" + parts[1][1..].PadLeft(2, '0') : "+" + parts[1].TrimStart('+').PadLeft(2, '0');
            return mant + "e" + exp;
        }
        return s;
    }

    public string Inspect(object o)
    {
        switch (o)
        {
            case null: return "nil";
            case bool b: return b ? "true" : "false";
            case long or double: return ToS(o);
            case RString s: return InspectStr(s.V);
            case string str: return InspectStr(str);
            case RSymbol y: return ":" + SymName(y.Name);
            case RArray a: return "[" + string.Join(", ", a.L.Select(Inspect)) + "]";
            case RHash h:
                if (h.D.Count == 0) return "{}";
                return "{" + string.Join(", ", h.D.Select(kv => kv.Key is RSymbol ks
                    ? (IsPlainSymbol(ks.Name) ? ks.Name : InspectStr(ks.Name)) + ": " + Inspect(kv.Value)
                    : Inspect(kv.Key) + " => " + Inspect(kv.Value))) + "}";
            case RRange r: return Inspect(r.From).Replace("nil", "") + (r.Excl ? "..." : "..") + (r.To == null ? "" : Inspect(r.To));
            case RClass c when FindMethod(DispatchClass(c), "inspect")?.Fn != null: return c.Name;
            case RRegex re: return "/" + System.Text.RegularExpressions.Regex.Replace(re.Source, @"(?<!\\)/", "\\/") + "/" + re.Flags;
        }
        var r2 = Invoke(o, "inspect", Array.Empty<object>(), null);
        return r2 is RString rs ? rs.V : "#<" + ClassOf(o).Name + ">";
    }

    public string DefaultInspect(RObject o)
    {
        if (o.Ivars == null || o.Ivars.Count == 0) return $"#<{o.Class.Name}>";
        return $"#<{o.Class.Name} " + string.Join(", ", o.Ivars.Select(kv => kv.Key + "=" + Inspect(kv.Value))) + ">";
    }

    static bool IsPlainSymbol(string n) => n.Length > 0 && (char.IsLetter(n[0]) || n[0] == '_') && n.Skip(1).Take(n.Length - 2).All(c => char.IsLetterOrDigit(c) || c == '_')
        && (char.IsLetterOrDigit(n[^1]) || n[^1] is '_' or '?' or '!');

    static string SymName(string n)
    {
        if (IsPlainSymbol(n) || (n.EndsWith('=') && IsPlainSymbol(n[..^1]))) return n;
        if (n is "+" or "-" or "*" or "/" or "%" or "**" or "==" or "!=" or "<" or ">" or "<=" or ">=" or "<=>" or "===" or "[]" or "[]=" or "<<" or ">>" or "!" or "=~" or "-@" or "+@") return n;
        if (n.Length > 1 && (n[0] == '@' || n[0] == '$') && IsPlainSymbol(n.TrimStart('@', '$'))) return n;
        return InspectStr(n);
    }

    public static string InspectStr(string s)
    {
        var sb = new StringBuilder("\"");
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\r': sb.Append("\\r"); break;
                case '\u001b': sb.Append("\\e"); break;
                case '\0': sb.Append("\\0"); break;
                case '#' when i + 1 < s.Length && s[i + 1] is '{' or '$' or '@': sb.Append("\\#"); break;
                default:
                    if (c < 0x20 || c == 0x7f) sb.Append($"\\x{(int)c:X2}");
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }

    // ---- equality ----

    public static bool Eql(object a, object b) => (a, b) switch
    {
        (null, null) => true,
        (RString x, RString y) => x.V == y.V,
        (long x, long y) => x == y,
        (double x, double y) => x.Equals(y),
        (bool x, bool y) => x == y,
        (RArray x, RArray y) => x.L.Count == y.L.Count && x.L.Zip(y.L).All(p => Eql(p.First, p.Second)),
        _ => ReferenceEquals(a, b),
    };

    public bool Equal(object a, object b)
    {
        switch (a)
        {
            case null: return b == null;
            case bool x: return b is bool yb && x == yb;
            case long x: return b is long yl ? x == yl : b is double dy && x == dy;
            case double x: return b is double yd ? x == yd : b is long ly && x == ly;
            case RString x: return b is RString ys && x.V == ys.V;
            case RSymbol: return ReferenceEquals(a, b);
            case RArray x: return b is RArray ya && x.L.Count == ya.L.Count && x.L.Zip(ya.L).All(p => Equal(p.First, p.Second));
            case RHash x:
                return b is RHash yh && x.D.Count == yh.D.Count && x.D.All(kv => yh.D.TryGetValue(kv.Key, out var v) && Equal(kv.Value, v));
            case RRange x: return b is RRange yr && x.Excl == yr.Excl && Equal(x.From, yr.From) && Equal(x.To, yr.To);
            case RClass: return ReferenceEquals(a, b);
        }
        return Truthy(Invoke(a, "==", new[] { b }, null));
    }

    public int Compare(object a, object b)
    {
        switch (a, b)
        {
            case (long x, long y): return x.CompareTo(y);
            case (long or double, long or double): return Num(a).CompareTo(Num(b));
            case (RString x, RString y): return string.CompareOrdinal(x.V, y.V) switch { < 0 => -1, > 0 => 1, _ => 0 };
        }
        var r = Invoke(a, "<=>", new[] { b }, null);
        if (r is long l) return Math.Sign(l);
        throw ArgError($"comparison of {ClassOf(a).Name} with {(b == null ? "nil" : Inspect(b))} failed");
    }

    // ---- calls ----

    public object Call(object self, string name, params object[] args) => Invoke(self, name, args, null);

    public object Invoke(object self, string name, object[] args, RProc blk, bool fcall = false)
    {
        var cls = DispatchClass(self);
        var m = FindMethod(cls, name);
        if (m == null)
        {
            var mm = FindMethod(cls, "method_missing");
            if (mm != null && mm.Fn == null)
                return CallMethod(mm, self, args.Prepend(Sym(name)).ToArray(), blk);
            throw NoMethod(self, name, fcall && args.Length == 0);
        }
        return CallMethod(m, self, args, blk);
    }

    public object CallMethod(RMethod m, object self, object[] args, RProc blk)
    {
        if (m.Fn != null) return m.Fn(this, self, args, blk);
        if (m.Body != null) return CallProc(m.Body, args, blk, self, m.Owner, m);
        if (++depth > 10000) { depth = 0; throw Error(SystemStackError, "stack level too deep"); }
        var f = new Frame { Self = self, DefTarget = m.Cref?[0] ?? m.Owner, Block = blk, Method = m, Args = args, Cref = m.Cref };
        f.MethodFrame = f;
        try
        {
            BindParams(m.Def.Params, args, f, blk, true);
            return Eval(m.Def.Body, f);
        }
        catch (ReturnSignal r) when (r.Target == f) { return r.Value; }
        finally { depth--; }
    }

    public object CallProc(RProc proc, object[] args, RProc blk = null, object self = null, RClass defTarget = null, RMethod asMethod = null)
    {
        if (proc.Fn != null) return proc.Fn(this, self ?? proc.Self, args, blk);
        var pf = proc.Frame;
        bool lambda = proc.Lambda || asMethod != null;
        var f = new Frame
        {
            Parent = pf,
            Self = self ?? proc.Self,
            DefTarget = defTarget ?? pf.DefTarget,
            Block = asMethod != null ? blk : pf.Block,
            Method = asMethod ?? pf.Method,
            Args = asMethod != null ? args : pf.Args,
            Cref = defTarget != null && !defTarget.IsSingleton ? pf.Cref.Prepend(defTarget).ToList() : pf.Cref,
            MethodFrame = pf.MethodFrame,
            Proc = proc,
        };
        if (lambda) { f.MethodFrame = f; f.IsLambda = true; }
        var ps = proc.Block.Params;
        if (!lambda && args.Length == 1 && args[0] is RArray arr && ps.Count(p => !p.Keyword && !p.DoubleSplat && !p.Block) > 1)
            args = arr.L.ToArray();
        if (ps.Count == 0 && args.Length > 0) f.Locals["it"] = args[0];
        if (++depth > 10000) { depth = 0; throw Error(SystemStackError, "stack level too deep"); }
        try
        {
            BindParams(ps, args, f, blk, lambda);
            return Eval(proc.Block.Body, f);
        }
        catch (NextSignal n) { return n.Value; }
        catch (ReturnSignal r) when (lambda && r.Target == f) { return r.Value; }
        catch (BreakSignal b) when (lambda && b.Proc == proc) { return b.Value; }
        finally { depth--; }
    }

    public object Yield(RProc blk, params object[] args)
    {
        if (blk == null) throw Error(LocalJumpError, "no block given (yield)");
        return CallProc(blk, args);
    }

    void BindParams(List<Param> ps, object[] args, Frame f, RProc blk, bool strict)
    {
        if (ps.Count == 0)
        {
            if (strict && args.Length > 0) throw ArgCount(args, 0, 0);
            return;
        }
        var list = args;
        RHash kw = null;
        bool hasKw = false, hasKwRest = false;
        foreach (var p in ps) { if (p.Keyword) hasKw = true; if (p.DoubleSplat) hasKwRest = true; }
        if ((hasKw || hasKwRest) && list.Length > 0 && list[^1] is RHash h && h.D.Keys.All(k => k is RSymbol))
        {
            kw = h;
            list = list[..^1];
        }
        var positional = ps.Where(p => !p.Keyword && !p.DoubleSplat && !p.Block).ToList();
        int required = positional.Count(p => !p.Splat && p.Default == null);
        int optional = positional.Count(p => p.Default != null);
        int splatIdx = positional.FindIndex(p => p.Splat);
        if (strict && (list.Length < required || (splatIdx < 0 && list.Length > required + optional)))
        {
            if (hasKw && kw != null && list.Length < required) { list = args; kw = null; }
            if (list.Length < required || (splatIdx < 0 && list.Length > required + optional))
                throw ArgCount(list, required, splatIdx >= 0 ? -1 : required + optional);
        }
        int postReq = splatIdx < 0 ? 0 : positional.Skip(splatIdx + 1).Count(p => p.Default == null);
        int optAvail = Math.Max(0, list.Length - required);
        int idx = 0;
        foreach (var p in positional)
        {
            object v;
            if (p.Splat)
            {
                int n = Math.Max(0, list.Length - idx - postReq);
                v = new RArray(list.Skip(idx).Take(n));
                idx += n;
            }
            else if (p.Default != null)
            {
                if (optAvail > 0 && idx < list.Length) { v = list[idx++]; optAvail--; }
                else v = Unset;
            }
            else v = idx < list.Length ? list[idx++] : null;

            if (v == Unset) v = Eval(p.Default, f);
            if (p.Destructure != null) BindParams(p.Destructure, v is RArray da ? da.L.ToArray() : new[] { v }, f, null, false);
            else if (p.Name != null) f.Locals[p.Name] = v;
        }
        if (hasKw || hasKwRest)
        {
            var rest = new RHash();
            if (kw != null) foreach (var kv in kw.D) rest.D[kv.Key] = kv.Value;
            foreach (var p in ps.Where(p => p.Keyword))
            {
                var key = Sym(p.Name);
                if (rest.D.Remove(key, out var v)) f.Locals[p.Name] = v;
                else if (p.Required) throw ArgError($"missing keyword: :{p.Name}");
                else f.Locals[p.Name] = Eval(p.Default, f);
            }
            var ks = ps.FirstOrDefault(p => p.DoubleSplat);
            if (ks?.Name != null) f.Locals[ks.Name] = rest;
            else if (ks == null && strict && rest.D.Count > 0)
                throw ArgError((rest.D.Count == 1 ? "unknown keyword: " : "unknown keywords: ") + string.Join(", ", rest.D.Keys.Select(Inspect)));
        }
        var bp = ps.FirstOrDefault(p => p.Block);
        if (bp?.Name != null) f.Locals[bp.Name] = blk;
    }

    // ---- evaluation ----

    public object Eval(Node n, Frame f)
    {
        switch (n)
        {
            case null: return null;
            case SeqNode s:
            {
                object r = null;
                foreach (var x in s.Body) r = Eval(x, f);
                return r;
            }
            case CallNode c: return EvalCall(c, f);
            case LocalNode l: return f.TryGet(l.Name, out var lv) ? lv : null;
            case StrNode s: return new RString(Interpolate(s, f));
            case SymNode s: return Sym(s.Name);
            case IntNode i: return i.Value;
            case FloatNode fl: return fl.Value;
            case NilNode: return null;
            case TrueNode: return true;
            case FalseNode: return false;
            case SelfNode: return f.Self;
            case IVarNode iv: return GetIvar(f, iv.Name);
            case GVarNode g: return GetGlobal(g.Name);
            case ConstNode c: return LookupConst(c, f);
            case DSymNode d: return Sym(Interpolate(d.Str, f));
            case XStrNode x: return Backtick(Interpolate(x.Str, f));
            case RegexNode r:
            {
                if (regexCache.TryGetValue(r, out var cached)) return cached;
                var re = MakeRegex(Interpolate(r.Str, f), r.Flags);
                if (r.Str.Parts.All(p => p is string)) regexCache[r] = re;
                return re;
            }
            case ArrayNode a: return new RArray(EvalArgs(a.Elems, f, out _));
            case HashNode h: return EvalHash(h, f);
            case RangeNode r: return new RRange { From = Eval(r.From, f), To = Eval(r.To, f), Excl = r.Exclusive };
            case AssignNode a:
            {
                var v = Eval(a.Value, f);
                if (a.Value is SplatNode) v = new RArray(SplatToList(v));
                Assign(a.Target, v, f);
                return v;
            }
            case MultiAssignNode m: return EvalMultiAssign(m, f);
            case OpAssignNode o: return EvalOpAssign(o, f);
            case AndNode a: { var l = Eval(a.Left, f); return Truthy(l) ? Eval(a.Right, f) : l; }
            case OrNode o: { var l = Eval(o.Left, f); return Truthy(l) ? l : Eval(o.Right, f); }
            case NotNode nn: return !Truthy(Eval(nn.Value, f));
            case DefinedNode d: return Defined(d.Value, f);
            case IfNode i: return Truthy(Eval(i.Cond, f)) ? Eval(i.Then, f) : Eval(i.Else, f);
            case WhileNode w: return EvalWhile(w, f);
            case ForNode fo: return EvalFor(fo, f);
            case CaseNode c: return EvalCase(c, f);
            case ReturnNode r: throw new ReturnSignal { Target = f.MethodFrame, Value = Eval(r.Value, f) };
            case NextNode nx: throw new NextSignal { Value = Eval(nx.Value, f) };
            case BreakNode b: throw new BreakSignal { Proc = f.Proc, Value = Eval(b.Value, f) };
            case YieldNode y:
                if (f.Block == null) throw Error(LocalJumpError, "no block given (yield)");
                return CallProc(f.Block, EvalArgs(y.Args, f, out _));
            case SuperNode s: return EvalSuper(s, f);
            case BeginNode b: return EvalBegin(b, f);
            case DefNode d: return EvalDef(d, f);
            case ClassNode c: return EvalClass(c, f);
            case LambdaNode l: return new RProc { Block = l.Block, Frame = f, Self = f.Self, Lambda = true };
            case SplatNode s: return Eval(s.Value, f);
        }
        throw new NotSupportedException(n.GetType().Name);
    }

    public string Interpolate(StrNode s, Frame f)
    {
        if (s.Parts.Count == 1 && s.Parts[0] is string only) return only;
        var sb = new StringBuilder();
        foreach (var part in s.Parts)
            sb.Append(part is string str ? str : ToS(Eval((Node)part, f)));
        return sb.ToString();
    }

    public List<object> SplatToList(object v) => v switch
    {
        null => new(),
        RArray a => a.L.ToList(),
        RHash h => h.D.Select(kv => (object)new RArray(new[] { kv.Key, kv.Value })).ToList(),
        RRange => ToList(v),
        _ when RespondTo(v, "to_a") && v is RObject => SplatToList(Invoke(v, "to_a", Array.Empty<object>(), null)),
        _ => new() { v },
    };

    object[] EvalArgs(List<Node> args, Frame f, out RProc blk)
    {
        blk = null;
        if (args.Count == 0) return Array.Empty<object>();
        var list = new List<object>(args.Count);
        foreach (var a in args)
        {
            switch (a)
            {
                case SplatNode s: list.AddRange(SplatToList(Eval(s.Value, f))); break;
                case BlockPassNode bp: blk = ToProc(bp.Value == null ? f.Block : Eval(bp.Value, f)); break;
                default: list.Add(Eval(a, f)); break;
            }
        }
        return list.ToArray();
    }

    public RProc ToProc(object v)
    {
        switch (v)
        {
            case null: return null;
            case RProc p: return p;
            case RSymbol s:
                return new RProc { Fn = (it, self, a, b) => it.Invoke(a[0], s.Name, a.Skip(1).ToArray(), b) };
            case RHash h:
                return new RProc { Fn = (it, self, a, b) => it.HashGet(h, a[0]) };
        }
        if (RespondTo(v, "to_proc") && Invoke(v, "to_proc", Array.Empty<object>(), null) is RProc rp) return rp;
        throw TypeErr($"wrong argument type {ClassOf(v).Name} (expected Proc)");
    }

    RHash EvalHash(HashNode h, Frame f)
    {
        var r = new RHash();
        foreach (var (k, v) in h.Pairs)
        {
            if (k is DoubleSplatNode ds)
            {
                if (Eval(ds.Value, f) is RHash other) foreach (var kv in other.D) r.D[kv.Key] = kv.Value;
                continue;
            }
            var key = Eval(k, f);
            if (key is RString ks && !ks.Frozen) key = new RString(ks.V) { Frozen = true };
            r.D[key] = Eval(v, f);
        }
        return r;
    }

    object EvalCall(CallNode c, Frame f)
    {
        CurLine = c.Line;
        // `it` in blocks, and locals the parser couldn't see (assigned on an earlier REPL line)
        if (c.Recv == null && c.Args.Count == 0 && c.Block == null && !c.HasParens && f.TryGet(c.Name is "_1" ? "it" : c.Name, out var lv))
            return lv;
        object recv;
        bool fcall = c.Recv == null;
        if (fcall) recv = f.Self;
        else
        {
            recv = Eval(c.Recv, f);
            if (c.SafeNav && recv == null) return null;
        }
        if (recv is long or double && c.Args.Count == 1 && c.Block == null && c.Args[0] is not (SplatNode or BlockPassNode or HashNode { Braces: false }))
        {
            var arg = Eval(c.Args[0], f);
            if (TryArith(recv, c.Name, arg, out var res)) return res;
            Caller = f;
            return Invoke(recv, c.Name, new[] { arg }, null);
        }
        var args = EvalArgs(c.Args, f, out var blk);
        RProc literal = null;
        if (c.Block != null) blk = literal = new RProc { Block = c.Block, Frame = f, Self = f.Self };
        Caller = f;
        CurLine = c.Line;
        if (literal == null) return Invoke(recv, c.Name, args, blk, fcall);
        try { return Invoke(recv, c.Name, args, blk, fcall); }
        catch (BreakSignal b) when (b.Proc == literal) { return b.Value; }
    }

    bool TryArith(object a, string op, object b, out object r)
    {
        r = null;
        if (a is long x && b is long y)
        {
            switch (op)
            {
                case "+": r = x + y; return true;
                case "-": r = x - y; return true;
                case "*": r = x * y; return true;
                case "<": r = x < y; return true;
                case ">": r = x > y; return true;
                case "<=": r = x <= y; return true;
                case ">=": r = x >= y; return true;
                case "==": r = x == y; return true;
                case "!=": r = x != y; return true;
            }
            return false;
        }
        if (a is long or double && b is long or double)
        {
            double dx = Num(a), dy = Num(b);
            switch (op)
            {
                case "+": r = dx + dy; return true;
                case "-": r = dx - dy; return true;
                case "*": r = dx * dy; return true;
                case "/": r = dx / dy; return true;
                case "<": r = dx < dy; return true;
                case ">": r = dx > dy; return true;
                case "<=": r = dx <= dy; return true;
                case ">=": r = dx >= dy; return true;
                case "==": r = dx == dy; return true;
                case "!=": r = dx != dy; return true;
            }
        }
        return false;
    }

    object EvalSuper(SuperNode s, Frame f)
    {
        var m = f.Method ?? throw Error(RuntimeError, "super called outside of method");
        RProc bp = null;
        var args = s.Args == null ? f.Args : EvalArgs(s.Args, f, out bp);
        var blk = s.Block != null ? new RProc { Block = s.Block, Frame = f, Self = f.Self } : bp ?? f.Block;
        bool past = false;
        foreach (var a in Ancestors(DispatchClass(f.Self)))
        {
            if (!past) { past = a == m.Owner; continue; }
            if (a.Methods.TryGetValue(m.Name, out var sm)) return CallMethod(sm, f.Self, args, blk);
        }
        throw Error(NoMethodError, $"super: no superclass method '{m.Name}' for {Describe(f.Self)}");
    }

    object EvalDef(DefNode d, Frame f)
    {
        RClass target = d.Singleton
            ? SingletonOf(f.Self as RObject ?? throw TypeErr("can't define singleton"))
            : f.DefTarget ?? ObjectClass;
        target.Methods[d.Name] = new RMethod { Name = d.Name, Def = d, Owner = target, Cref = f.Cref };
        InvalidateMethods();
        if (target.Attached == null && target.GetIvar("__module_function") is true)
            SingletonOf(target).Methods[d.Name] = target.Methods[d.Name];
        return Sym(d.Name);
    }

    object EvalClass(ClassNode c, Frame f)
    {
        if (c.SelfClass)
            return EvalBody(c.Body, SingletonOf(f.Self as RObject ?? throw TypeErr("can't define singleton")), f);
        RClass container = c.Path.Scope != null
            ? Eval(c.Path.Scope, f) as RClass ?? throw TypeErr("not a class/module")
            : c.Path.TopLevel ? ObjectClass : f.Cref[0];
        string name = c.Path.Name;
        RClass cls;
        if (container.Consts.TryGetValue(name, out var existing))
        {
            cls = existing as RClass ?? throw TypeErr($"{name} is not a {(c.IsModule ? "module" : "class")}");
            if (cls.IsModule != c.IsModule) throw TypeErr($"{name} is not a {(c.IsModule ? "module" : "class")}");
            if (c.Super != null && Eval(c.Super, f) != cls.Super) throw TypeErr($"superclass mismatch for class {name}");
        }
        else if (c.IsModule) cls = DefineModule(name, container);
        else
        {
            var super = c.Super == null ? ObjectClass : Eval(c.Super, f) as RClass ?? throw TypeErr("superclass must be a Class");
            cls = DefineClass(name, super, container);
            Invoke(super, "inherited", new object[] { cls }, null);
        }
        return EvalBody(c.Body, cls, f);
    }

    object EvalBody(Node body, RClass cls, Frame f)
    {
        var bf = new Frame { Self = cls, DefTarget = cls, Cref = f.Cref.Prepend(cls).ToList() };
        bf.MethodFrame = bf;
        return Eval(body, bf);
    }

    object EvalWhile(WhileNode w, Frame f)
    {
        while (Truthy(Eval(w.Cond, f)) != w.Until)
        {
            try { Eval(w.Body, f); }
            catch (NextSignal) { }
            catch (BreakSignal b) when (b.Proc == f.Proc) { return b.Value; }
        }
        return null;
    }

    object EvalFor(ForNode fo, Frame f)
    {
        var iter = Eval(fo.Iter, f);
        foreach (var item in ToList(iter))
        {
            if (fo.Vars.Count == 1) f.Set(fo.Vars[0], item);
            else
            {
                var parts = item is RArray a ? a.L : new List<object> { item };
                for (int i = 0; i < fo.Vars.Count; i++) f.Set(fo.Vars[i], i < parts.Count ? parts[i] : null);
            }
            try { Eval(fo.Body, f); }
            catch (NextSignal) { }
            catch (BreakSignal b) when (b.Proc == f.Proc) { return b.Value; }
        }
        return iter;
    }

    object EvalCase(CaseNode c, Frame f)
    {
        bool hasSubject = c.Subject != null;
        var subject = Eval(c.Subject, f);
        foreach (var (conds, body) in c.Whens)
        {
            foreach (var cond in conds)
            {
                var tests = cond is SplatNode s ? SplatToList(Eval(s.Value, f)) : new List<object> { Eval(cond, f) };
                foreach (var t in tests)
                    if (hasSubject ? CaseEq(t, subject) : Truthy(t)) return Eval(body, f);
            }
        }
        return Eval(c.Else, f);
    }

    public bool CaseEq(object pattern, object v) => pattern switch
    {
        RClass c => IsA(v, c),
        RRegex r => v is RString or RSymbol && RegexMatch(r, StrOrSym(v), 0) != null,
        RRange rg => RangeIncludes(rg, v),
        RString or long or double or RSymbol or null or bool => Equal(pattern, v),
        RProc p => Truthy(CallProc(p, new[] { v })),
        _ => Truthy(Invoke(pattern, "===", new[] { v }, null)),
    };

    object EvalBegin(BeginNode b, Frame f)
    {
        while (true)
        {
            try
            {
                object r;
                try { r = Eval(b.Body, f); }
                catch (Exception ex) when (b.Rescues.Count > 0 && IsCatchable(ex))
                {
                    var exc = ToRubyException(ex);
                    RescueClause hit = null;
                    foreach (var rc in b.Rescues)
                        if (RescueMatches(rc, exc, f)) { hit = rc; break; }
                    if (hit == null) throw;
                    if (hit.Var != null) f.Set(hit.Var, exc);
                    Globals["$!"] = exc;
                    try { return Eval(hit.Body, f); }
                    catch (RetrySignal) { continue; }
                }
                if (b.ElseBody != null) r = Eval(b.ElseBody, f);
                return r;
            }
            finally
            {
                if (b.Ensure != null) Eval(b.Ensure, f);
            }
        }
    }

    static bool IsCatchable(Exception ex) => ex is not (BreakSignal or NextSignal or ReturnSignal or ThrowSignal or RetrySignal or OutOfMemoryException);

    bool RescueMatches(RescueClause rc, RObject exc, Frame f)
    {
        if (rc.Classes.Count == 0) return IsA(exc, StandardError);
        foreach (var cn in rc.Classes)
        {
            var cv = Eval(cn, f);
            foreach (var c in cv is RArray a ? a.L : new List<object> { cv })
                if (c is RClass k && IsA(exc, k)) return true;
        }
        return false;
    }

    public RObject ToRubyException(Exception ex) => ex switch
    {
        RubyError re => re.Exc,
        FileNotFoundException fe => NewException(ENOENT, "No such file or directory @ rb_sysopen - " + (fe.FileName ?? fe.Message)),
        DirectoryNotFoundException de => NewException(ENOENT, "No such file or directory - " + de.Message),
        UnauthorizedAccessException ue => NewException(EACCES, "Permission denied - " + ue.Message),
        IOException ie => NewException(IOError, ie.Message),
        RubySyntaxError se => NewException(SyntaxErrorClass, se.Message),
        InvalidCastException ce => NewException(TypeError, ce.Message),
        DivideByZeroException => NewException(ZeroDivisionError, "divided by 0"),
        _ => NewException(StandardError, ex.Message),
    };

    // ---- variables / constants ----

    object GetIvar(Frame f, string name)
    {
        if (name.StartsWith("@@")) return ClassVarHolder(f).GetIvar(name);
        return f.Self is RObject o ? o.GetIvar(name) : null;
    }

    RClass ClassVarHolder(Frame f)
    {
        var c = f.Cref[0];
        return c.IsSingleton && c.Attached is RClass a ? a : c;
    }

    object GetGlobal(string name)
    {
        if (name.Length == 2 && char.IsDigit(name[1]))
        {
            if (Globals.GetValueOrDefault("$~") is not RMatch m) return null;
            var g = m.M.Groups[name[1] - '0'];
            return g.Success ? S(g.Value) : null;
        }
        return Globals.GetValueOrDefault(name);
    }

    void Assign(Node target, object v, Frame f)
    {
        switch (target)
        {
            case LocalNode l: f.Set(l.Name, v); break;
            case IVarNode iv:
                if (iv.Name.StartsWith("@@")) ClassVarHolder(f).SetIvar(iv.Name, v);
                else if (f.Self is RObject o)
                {
                    if (o.Frozen) throw Error(FrozenError, $"can't modify frozen {o.Class.Name}: {Inspect(o)}");
                    o.SetIvar(iv.Name, v);
                }
                else throw Error(FrozenError, $"can't modify frozen {ClassOf(f.Self).Name}");
                break;
            case GVarNode g: Globals[g.Name] = v; break;
            case ConstNode c:
            {
                var container = c.Scope != null ? (RClass)Eval(c.Scope, f) : c.TopLevel ? ObjectClass : f.Cref[0];
                container.Consts[c.Name] = v;
                if (v is RClass { Name: null } anon) anon.Name = container == ObjectClass ? c.Name : $"{container.Name}::{c.Name}";
                break;
            }
            case CallNode { Name: "[]" } ix:
            {
                var recv = Eval(ix.Recv, f);
                var args = EvalArgs(ix.Args, f, out _);
                Invoke(recv, "[]=", args.Append(v).ToArray(), null);
                break;
            }
            case CallNode attr:
            {
                var recv = Eval(attr.Recv, f);
                if (attr.SafeNav && recv == null) return;
                Invoke(recv, attr.Name + "=", new[] { v }, null);
                break;
            }
            case SplatNode s: Assign(s.Value, v is RArray ? v : new RArray(SplatToList(v)), f); break;
            default: throw new RubySyntaxError("can't assign", target.Line);
        }
    }

    object EvalMultiAssign(MultiAssignNode m, Frame f)
    {
        var value = Eval(m.Value, f);
        var items = value is RArray a ? a.L.ToList() : new List<object> { value };
        int splat = m.Targets.FindIndex(t => t is SplatNode);
        int after = splat < 0 ? 0 : m.Targets.Count - splat - 1;
        for (int i = 0; i < m.Targets.Count; i++)
        {
            if (i == splat)
            {
                int n = Math.Max(0, items.Count - i - after);
                Assign(((SplatNode)m.Targets[i]).Value, new RArray(items.Skip(i).Take(n)), f);
            }
            else if (splat >= 0 && i > splat)
            {
                int idx = items.Count - (m.Targets.Count - i);
                Assign(m.Targets[i], idx >= 0 && idx > splat - 1 ? items[idx] : null, f);
            }
            else Assign(m.Targets[i], i < items.Count ? items[i] : null, f);
        }
        return value;
    }

    object EvalOpAssign(OpAssignNode o, Frame f)
    {
        object recv = null;
        object[] idxArgs = null;
        Func<object> read;
        Action<object> write;
        switch (o.Target)
        {
            case CallNode { Name: "[]" } ix:
                recv = Eval(ix.Recv, f);
                idxArgs = EvalArgs(ix.Args, f, out _);
                read = () => Invoke(recv, "[]", idxArgs, null);
                write = v => Invoke(recv, "[]=", idxArgs.Append(v).ToArray(), null);
                break;
            case CallNode attr:
                recv = Eval(attr.Recv, f);
                if (attr.SafeNav && recv == null) return null;
                read = () => Invoke(recv, attr.Name, Array.Empty<object>(), null);
                write = v => Invoke(recv, attr.Name + "=", new[] { v }, null);
                break;
            case ConstNode c:
                read = () => { try { return LookupConst(c, f); } catch (RubyError) { return null; } };
                write = v => Assign(c, v, f);
                break;
            default:
                read = () => Eval(o.Target, f);
                write = v => Assign(o.Target, v, f);
                break;
        }
        var cur = read();
        object result;
        if (o.Op == "||") { if (Truthy(cur)) return cur; result = Eval(o.Value, f); }
        else if (o.Op == "&&") { if (!Truthy(cur)) return cur; result = Eval(o.Value, f); }
        else
        {
            var rhs = Eval(o.Value, f);
            result = TryArith(cur, o.Op, rhs, out var r) ? r : Invoke(cur, o.Op, new[] { rhs }, null);
        }
        write(result);
        return result;
    }

    public object LookupConst(ConstNode c, Frame f)
    {
        if (c.Scope != null)
        {
            var sc = Eval(c.Scope, f);
            if (sc is not RClass m) throw TypeErr($"{Inspect(sc)} is not a class/module");
            return ConstGet(m, c.Name) ?? throw Error(NameError, $"uninitialized constant {m.Name}::{c.Name}");
        }
        if (c.TopLevel) return ConstGet(ObjectClass, c.Name) ?? throw Error(NameError, $"uninitialized constant {c.Name}");
        foreach (var k in f.Cref)
            if (k.Consts.TryGetValue(c.Name, out var v)) return v;
        var found = ConstGet(f.Cref[0], c.Name) ?? ConstGet(ObjectClass, c.Name);
        if (found != null) return found;
        var outer = f.Cref[0];
        throw Error(NameError, outer == ObjectClass || outer.IsSingleton ? $"uninitialized constant {c.Name}" : $"uninitialized constant {outer.Name}::{c.Name}");
    }

    public object ConstGet(RClass m, string name)
    {
        foreach (var a in Ancestors(m))
            if (a.Consts.TryGetValue(name, out var v)) return v;
        if (m.IsModule && ObjectClass.Consts.TryGetValue(name, out var ov)) return ov;
        return null;
    }

    object Defined(Node v, Frame f)
    {
        switch (v)
        {
            case LocalNode: return S("local-variable");
            case IVarNode iv: return f.Self is RObject o && o.Ivars != null && o.Ivars.ContainsKey(iv.Name) ? S("instance-variable") : null;
            case GVarNode g: return Globals.ContainsKey(g.Name) ? S("global-variable") : null;
            case ConstNode c:
                try { LookupConst(c, f); return S("constant"); }
                catch (RubyError) { return null; }
            case CallNode { Recv: null } c:
                if (c.Name == "it" && f.TryGet("it", out _)) return S("local-variable");
                return FindMethod(DispatchClass(f.Self), c.Name) != null ? S("method") : null;
            case CallNode c:
                try { return RespondTo(Eval(c.Recv, f), c.Name) ? S("method") : null; }
                catch (RubyError) { return null; }
            case SelfNode: return S("self");
            case NilNode: return S("expression");
            case YieldNode: return f.Block != null ? S("yield") : null;
            case SuperNode:
                if (f.Method == null) return null;
                return Ancestors(DispatchClass(f.Self)).SkipWhile(a => a != f.Method.Owner).Skip(1).Any(a => a.Methods.ContainsKey(f.Method.Name)) ? S("super") : null;
            case AssignNode or OpAssignNode or MultiAssignNode: return S("assignment");
        }
        return S("expression");
    }

    // ---- regex ----

    static readonly Dictionary<string, string> PosixClasses = new()
    {
        ["alpha"] = "a-zA-Z", ["digit"] = "0-9", ["alnum"] = "a-zA-Z0-9", ["upper"] = "A-Z", ["lower"] = "a-z",
        ["space"] = "\\s", ["xdigit"] = "0-9a-fA-F", ["punct"] = "!-/:-@\\[-`{-~", ["word"] = "\\w", ["blank"] = " \\t", ["cntrl"] = "\\x00-\\x1f",
    };

    public RRegex MakeRegex(string src, string flags)
    {
        var opts = RegexOptions.Multiline | RegexOptions.CultureInvariant;
        if (flags.Contains('i')) opts |= RegexOptions.IgnoreCase;
        if (flags.Contains('m')) opts |= RegexOptions.Singleline;
        if (flags.Contains('x')) opts |= RegexOptions.IgnorePatternWhitespace;
        var net = Regex.Replace(src, @"\\\\|\\h|\\H|\[:(\^?)(\w+):\]", m => m.Value switch
        {
            "\\\\" => "\\\\",
            "\\h" => "[0-9a-fA-F]",
            "\\H" => "[^0-9a-fA-F]",
            _ => PosixClasses.TryGetValue(m.Groups[2].Value, out var cls) ? (m.Groups[1].Value == "^" ? "^" + cls : cls) : m.Value,
        });
        try { return new RRegex { R = new Regex(net, opts), Source = src, Flags = flags.Replace("o", "") }; }
        catch (ArgumentException e) { throw Error(SyntaxErrorClass, $"invalid regex /{src}/: {e.Message}"); }
    }

    public RMatch RegexMatch(RRegex r, string s, int start)
    {
        if (start < 0) start += s.Length;
        if (start < 0 || start > s.Length) { Globals["$~"] = null; return null; }
        var m = r.R.Match(s, start);
        var rm = m.Success ? new RMatch { M = m, Input = s } : null;
        Globals["$~"] = rm;
        return rm;
    }

    public object Backtick(string cmd)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/d /s /c \"" + cmd + "\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = Cwd,
        };
        ApplyEnv(psi);
        using var proc = System.Diagnostics.Process.Start(psi)!;
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        SetStatus(proc.ExitCode, proc.Id);
        return S(output.Replace("\r\n", "\n"));
    }
}
