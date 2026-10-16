using System.Text.RegularExpressions;

namespace Brew.Ruby;

public delegate object Native(Interp it, object self, object[] args, RProc blk);

public sealed class RSymbol
{
    static readonly Dictionary<string, RSymbol> Table = new();
    public readonly string Name;
    RSymbol(string name) { Name = name; }

    public static RSymbol Get(string name)
    {
        lock (Table)
        {
            if (!Table.TryGetValue(name, out var s)) Table[name] = s = new RSymbol(name);
            return s;
        }
    }

    public override string ToString() => Name;
}

public sealed class RString
{
    public string V;
    public bool Frozen;
    // Bytes that aren't UTF-8 (a child's binary output), one char per byte (Latin-1).
    public bool Binary;
    public RString(string v) { V = v; }
    public override string ToString() => V;
    // Like Ruby: the same bytes in the same encoding, or ASCII-only text in either ("\x89PNG" isn't "\x89PNG".b).
    public override bool Equals(object o) => o is RString s && s.V == V && (s.Binary == Binary || V.All(c => c < 0x80));
    public override int GetHashCode() => V.GetHashCode();

    // A "\x89" escape that isn't part of valid UTF-8 is kept as the raw byte, stored as U+DC89
    // (Python's surrogateescape), so a UTF-8 string can still carry it.
    public const char RawByteBase = '\uDC00';
    public static bool IsRawByte(char c) => c >= '\uDC80' && c <= '\uDCFF';
    static bool HasRawBytes(string s) => s.Any(IsRawByte);

    public static byte[] Bytes(RString s) => s.Binary ? System.Text.Encoding.Latin1.GetBytes(s.V) : Utf8(s.V);

    public static byte[] Utf8(string s)
    {
        if (!HasRawBytes(s)) return System.Text.Encoding.UTF8.GetBytes(s);
        var bytes = new List<byte>(s.Length + 8);
        int start = 0;
        for (int i = 0; i <= s.Length; i++)
        {
            if (i < s.Length && !IsRawByte(s[i])) continue;
            bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(s[start..i]));
            if (i < s.Length) bytes.Add((byte)(s[i] - RawByteBase));
            start = i + 1;
        }
        return bytes.ToArray();
    }
}

public sealed class RArray
{
    public List<object> L;
    public bool Frozen;
    public RArray() { L = new(); }
    public RArray(IEnumerable<object> items) { L = new(items); }
}

public sealed class RHash
{
    public OrderedDictionary<object, object> D = new(KeyComparer.Instance);
    public object Default;
    public RProc DefaultProc;
    public bool Frozen;
}

public sealed class RRange
{
    public object From, To;
    public bool Excl;
}

public sealed class RRegex
{
    public Regex R;
    public string Source;
    public string Flags;
}

public sealed class RMatch
{
    public Match M;
    public string Input;
}

public sealed class RProc
{
    public BlockNode Block;
    public Frame Frame;
    public object Self;
    public bool Lambda;
    public Native Fn;   // native procs (Symbol#to_proc, method objects)
}

public class RObject
{
    public RClass Class;
    public RClass Singleton;
    public Dictionary<string, object> Ivars;
    public bool Frozen;
    public object Host;   // native payload (Pathname string, exception data, ...)

    public object GetIvar(string n) => Ivars != null && Ivars.TryGetValue(n, out var v) ? v : null;
    public void SetIvar(string n, object v) => (Ivars ??= new())[n] = v;
}

public sealed class RClass : RObject
{
    public string Name;
    public RClass Super;
    public bool IsModule;
    public bool IsSingleton;
    public object Attached;   // for singleton classes
    public readonly Dictionary<string, RMethod> Methods = new();
    public readonly Dictionary<string, object> Consts = new();
    public readonly List<RClass> Includes = new();   // in include order
    public RClass LexicalParent;

    public override string ToString() => Name;
}

public sealed class RMethod
{
    public string Name;
    public DefNode Def;
    public RProc Body;   // define_method
    public Native Fn;
    public RClass Owner;
    public List<RClass> Cref;
}

public sealed class Frame
{
    public object Self;
    public Dictionary<string, object> Locals = new();
    public Frame Parent;       // lexical parent (blocks)
    public Frame MethodFrame;  // frame `return` exits
    public RClass DefTarget;
    public RProc Block;
    public RMethod Method;
    public object[] Args;
    public List<RClass> Cref;
    public bool IsLambda;
    public RProc Proc;         // the block this frame runs (target of `break`)

    public bool TryGet(string name, out object v)
    {
        for (var f = this; f != null; f = f.Parent)
            if (f.Locals.TryGetValue(name, out v)) return true;
        v = null;
        return false;
    }

    public void Set(string name, object v)
    {
        for (var f = this; f != null; f = f.Parent)
            if (f.Locals.ContainsKey(name)) { f.Locals[name] = v; return; }
        Locals[name] = v;
    }
}

public sealed class KeyComparer : IEqualityComparer<object>
{
    public static readonly KeyComparer Instance = new();

    public new bool Equals(object a, object b) => Interp.Eql(a, b);

    public int GetHashCode(object o) => o switch
    {
        null => 0,
        RString s => s.V.GetHashCode(),
        long l => l.GetHashCode(),
        double d => d.GetHashCode(),
        RArray a => a.L.Aggregate(17, (h, x) => h * 31 + GetHashCode(x)),
        _ => o.GetHashCode(),
    };
}

// ---- control flow ----

public sealed class BreakSignal : Exception { public RProc Proc; public object Value; }
public sealed class NextSignal : Exception { public object Value; }
public sealed class ReturnSignal : Exception { public Frame Target; public object Value; }
public sealed class ThrowSignal : Exception { public object Tag; public object Value; }
public sealed class RetrySignal : Exception { }

public sealed class RubyError : Exception
{
    public readonly RObject Exc;
    public RubyError(RObject exc) { Exc = exc; }
    public override string Message => Exc.GetIvar("@message") is RString s ? s.V : Exc.Class.Name;
    public string ClassName => Exc.Class.Name;
}
