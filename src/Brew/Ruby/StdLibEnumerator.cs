using System.Numerics;

namespace Brew.Ruby;

// Enumerator: what each/map/select/times/... return without a block. It remembers the receiver,
// method and arguments and re-runs that method with a block when iterated, so
// `select.with_index { }` selects (not maps) and `each_slice(2).map { }` maps the slices.
// Enumerator.new { |y| ... } runs its generator block against a Yielder.
public sealed class REnumerator
{
    public object Recv;
    public string Meth;
    public object[] Args;
    public RProc Gen;
    public object Size;   // known size (Integer, Float::INFINITY), or null to count
    public bool Lazy;     // Enumerator::Lazy: Meth is a lazy step over Recv, Gen its block
    public List<object> Buf;
    public int Pos;
}

public sealed partial class Interp
{
    public RClass EnumeratorClass, YielderClass;

    // Natives turned into enumerators when called without a block (name, number of positional
    // arguments that makes the blockless call an enumerator; -1 = any).
    static readonly (string Names, int Args)[] EnumOnNoBlock =
    {
        ("each each_pair each_index each_entry map collect flat_map collect_concat select filter find_all reject filter_map " +
         "each_with_index map! collect! select! filter! reject! delete_if keep_if sort_by sort_by! min_by max_by group_by partition " +
         "reverse_each each_key each_value transform_values transform_keys times each_char each_byte each_line " +
         "find detect take_while drop_while find_index index each_codepoint", 0),
        ("each_slice each_cons each_with_object upto downto with_index each_with_index_map with_object", 1),
        ("cycle", -1),
    };

    // Only the zero-argument form of these makes an enumerator (find_index(x) / index(x) search).
    static readonly HashSet<string> EnumExactArgs = new() { "find_index", "index", "find", "detect", "each_char", "each_byte" };

    void InitEnumerator()
    {
        var E = EnumeratorClass = DefineClass("Enumerator");
        E.Includes.Add(EnumerableModule);
        YielderClass = DefineClass("Yielder", ObjectClass, E);
        var lazy = DefineClass("Lazy", E, E);
        REnumerator Me(object o) => (REnumerator)((RObject)o).Host;

        DefS(E, "new", (it, self, a, b) =>
        {
            if (b == null) throw it.ArgError("no block given");
            return new RObject { Class = (RClass)self, Host = new REnumerator { Gen = b, Size = a.Length > 0 ? a[0] : null } };
        });
        Def(E, "each", (it, self, a, b) => b == null ? self : it.EnumEach(Me(self), b));
        Def(E, "with_index each_with_index", (it, self, a, b) =>
        {
            long off = a.Length > 0 && a[0] != null ? it.Int(a[0]) : 0;
            if (b == null) return it.MakeEnum(self, "with_index", new object[] { off }, Me(self).Size);
            long i = off;
            return it.EnumEach(Me(self), new RProc { Fn = (it2, s2, ya, yb) => it2.CallProc(b, new[] { Packed(ya), (object)i++ }) });
        });
        Def(E, "with_object each_with_object", (it, self, a, b) =>
        {
            if (b == null) return it.MakeEnum(self, "with_object", a, Me(self).Size);
            var memo = a[0];
            it.EnumEach(Me(self), new RProc { Fn = (it2, s2, ya, yb) => it2.CallProc(b, new[] { Packed(ya), memo }) });
            return memo;
        });
        Def(E, "next", (it, self, a, b) =>
        {
            var e = Me(self);
            it.Buffer(e);
            if (e.Pos >= e.Buf.Count) throw it.Error(it.StopIteration, "iteration reached an end");
            return e.Buf[e.Pos++];
        });
        Def(E, "peek", (it, self, a, b) =>
        {
            var e = Me(self);
            it.Buffer(e);
            if (e.Pos >= e.Buf.Count) throw it.Error(it.StopIteration, "iteration reached an end");
            return e.Buf[e.Pos];
        });
        Def(E, "rewind", (it, self, a, b) => { Me(self).Pos = 0; Me(self).Buf = null; return self; });
        Def(E, "size", (it, self, a, b) =>
        {
            var e = Me(self);
            if (e.Size != null) return e.Size is RProc p ? it.CallProc(p, NoArgs) : e.Size;
            if (e.Gen != null) return null;
            if (e.Recv is RArray or RHash && e.Meth is not ("each_slice" or "each_cons")) return it.Invoke(e.Recv, "size", NoArgs, null);
            return (long)it.ToList(self).Count;
        });
        Def(E, "inspect to_s", (it, self, a, b) =>
        {
            var e = Me(self);
            if (e.Gen != null) return S($"#<{((RObject)self).Class.Name}: #<Enumerator::Generator>:each>");
            var args = e.Args.Length == 0 ? "" : "(" + string.Join(", ", e.Args.Select(it.Inspect)) + ")";
            return S($"#<{((RObject)self).Class.Name}: {it.Inspect(e.Recv)}:{e.Meth}{args}>");
        });
        // Early-stopping forms, so an endless generator (or a stream) is read only as far as needed.
        Def(E, "first", (it, self, a, b) =>
        {
            if (a.Length == 0) { var one = it.EnumTake(Me(self), 1, null); return one.Count > 0 ? one[0] : null; }
            return new RArray(it.EnumTake(Me(self), it.Int(a[0]), null));
        });
        Def(E, "take", (it, self, a, b) => new RArray(it.EnumTake(Me(self), it.Int(a[0]), null)));
        Def(E, "take_while", (it, self, a, b) => b == null ? it.MakeEnum(self, "take_while", NoArgs, null) : new RArray(it.EnumTake(Me(self), long.MaxValue, x => Truthy(it.CallProc(b, new[] { x })))));
        Def(E, "find detect", (it, self, a, b) =>
        {
            if (b == null) return it.MakeEnum(self, "find", NoArgs, null);
            object found = null;
            it.EnumTake(Me(self), long.MaxValue, x => { if (Truthy(it.CallProc(b, new[] { x }))) { found = x; return false; } return true; });
            return found;
        });
        Def(E, "include? member?", (it, self, a, b) =>
        {
            bool hit = false;
            it.EnumTake(Me(self), long.MaxValue, x => !(hit = it.Equal(x, a[0])));
            return hit;
        });
        Def(E, "lazy", (it, self, a, b) => new RObject { Class = lazy, Host = new REnumerator { Recv = self, Meth = "each", Args = NoArgs, Lazy = true } });
        Def(E, "+", (it, self, a, b) => new RArray(it.ToList(self).Concat(it.ToList(a[0]))));

        // Enumerator::Lazy: map/select/reject/take_while/take/first evaluated element by element.
        foreach (var op in new[] { "map", "collect", "select", "filter", "reject", "take_while", "drop_while", "filter_map", "flat_map", "with_index" })
        {
            var o = op;
            Def(lazy, o, (it, self, a, b) => new RObject { Class = lazy, Host = new REnumerator { Recv = self, Meth = "__lazy_" + o, Args = a, Gen = b, Lazy = true } });
        }
        Def(lazy, "take", (it, self, a, b) => new RObject { Class = lazy, Host = new REnumerator { Recv = self, Meth = "__lazy_take", Args = a, Lazy = true } });
        Def(lazy, "each", (it, self, a, b) => b == null ? self : it.LazyEach(Me(self), b));
        Def(lazy, "force to_a", (it, self, a, b) => new RArray(it.ToList(self)));
        Def(lazy, "lazy", (it, self, a, b) => self);
        Def(lazy, "eager", (it, self, a, b) => it.MakeEnum(self, "each", NoArgs, null));

        Def(YielderClass, "<<", (it, self, a, b) => { it.CallProc((RProc)((RObject)self).Host, new[] { a[0] }); return self; });
        Def(YielderClass, "yield call", (it, self, a, b) => it.CallProc((RProc)((RObject)self).Host, a.Length == 0 ? new object[] { null } : a));
        Def(YielderClass, "to_proc", (it, self, a, b) => new RProc { Fn = (it2, s2, ya, yb) => it2.CallProc((RProc)((RObject)self).Host, ya) });

        // to_enum / enum_for on anything.
        Def(KernelModule, "to_enum enum_for", (it, self, a, b) =>
            it.MakeEnum(self, a.Length > 0 ? it.StrOrSym(a[0]) : "each", a.Length > 1 ? a[1..] : NoArgs, null));
        Def(EnumerableModule, "to_enum enum_for each_entry", (it, self, a, b) =>
            b != null ? it.Invoke(self, "each", a, b) : it.MakeEnum(self, a.Length > 0 ? it.StrOrSym(a[0]) : "each", a.Length > 1 ? a[1..] : NoArgs, null));
        Def(EnumerableModule, "lazy", (it, self, a, b) => new RObject { Class = lazy, Host = new REnumerator { Recv = self, Meth = "each", Args = NoArgs, Lazy = true } });
        Def(EnumerableModule, "with_index each_with_index_map", (it, self, a, b) =>
        {
            // Not Ruby (Arrays have no with_index); kept for code written when blockless map returned an Array.
            long off = a.Length > 0 && a[0] != null ? it.Int(a[0]) : 0;
            var items = it.ToList(self);
            return new RArray(items.Select((x, i) => it.CallProc(b, new[] { x, (object)(off + i) })));
        });

        // Kernel#loop without a block, and with StopIteration's result.
        var loopFn = KernelModule.Methods["loop"].Fn;
        Def(KernelModule, "loop", (it, self, a, b) => b == null
            ? new RObject { Class = E, Host = new REnumerator { Recv = self, Meth = "loop", Args = NoArgs, Size = double.PositiveInfinity } }
            : loopFn(it, self, a, b));

        InitSteps();

        // Blockless iteration methods across the core classes answer an Enumerator.
        foreach (var cls in new[] { EnumerableModule, ArrayClass, HashClass, RangeClass, StringClass, IntegerClass, StructOrNull() })
        {
            if (cls == null) continue;
            foreach (var (names, argc) in EnumOnNoBlock)
                foreach (var n in names.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!cls.Methods.TryGetValue(n, out var m) || m.Fn == null) continue;
                    var old = m.Fn;
                    var name = n;
                    bool exact = EnumExactArgs.Contains(n);
                    Def(cls, n, (it, self, a, b) =>
                    {
                        if (b == null && (argc < 0 || a.Length == argc || (!exact && argc == 0 && a.Length > 0 && a[^1] is RHash)))
                            return it.MakeEnum(self, name, a, it.KnownSize(self, name, a));
                        return old(it, self, a, b);
                    });
                }
        }
    }

    RClass StructOrNull() => ObjectClass.Consts.GetValueOrDefault("Struct") as RClass;

    object KnownSize(object self, string meth, object[] a)
    {
        switch (self)
        {
            case RArray arr when meth is not ("each_slice" or "each_cons" or "cycle" or "find" or "detect" or "find_index" or "index" or "take_while" or "drop_while"):
                return (long)arr.L.Count;
            case RHash h when meth is not ("each_slice" or "each_cons" or "find" or "detect" or "take_while" or "drop_while" or "find_index"):
                return (long)h.D.Count;
            case long n when meth == "times": return Math.Max(0, n);
            case RRange { To: null } when meth == "each": return double.PositiveInfinity;
            case RRange { From: long rf, To: long rt } r when meth == "each": return Math.Max(0, rt - rf + (r.Excl ? 0 : 1));
            case long n when meth == "upto" && a.Length == 1 && a[0] is long to: return Math.Max(0, to - n + 1);
            case long n when meth == "downto" && a.Length == 1 && a[0] is long to: return Math.Max(0, n - to + 1);
        }
        return null;
    }

    public RObject MakeEnum(object recv, string meth, object[] args, object size) =>
        new() { Class = EnumeratorClass, Host = new REnumerator { Recv = recv, Meth = meth, Args = args ?? NoArgs, Size = size } };

    static object Packed(object[] ya) => ya.Length == 1 ? ya[0] : ya.Length == 0 ? null : new RArray(ya);

    object EnumEach(REnumerator e, RProc blk)
    {
        if (e.Lazy) return LazyEach(e, blk);
        if (e.Gen != null && e.Recv == null)
            return CallProc(e.Gen, new object[] { new RObject { Class = YielderClass, Host = blk } });
        if (e.Meth == "loop")
        {
            // `loop` as an enumerator yields nothing but runs forever; each passes no arguments.
            try { while (true) CallProc(blk, NoArgs); }
            catch (RubyError err) when (IsA(err.Exc, StopIteration)) { return err.Exc.GetIvar("@result"); }
        }
        return Invoke(e.Recv, e.Meth, e.Args, blk);
    }

    // Collect up to n items (or while keep says so), stopping the underlying iteration early.
    List<object> EnumTake(REnumerator e, long n, Func<object, bool> keep)
    {
        var res = new List<object>();
        if (n <= 0) return res;
        var tag = new object();
        try
        {
            EnumEach(e, new RProc
            {
                Fn = (it, s, ya, yb) =>
                {
                    var x = Packed(ya);
                    if (keep != null && !keep(x)) throw new ThrowSignal { Tag = tag };
                    res.Add(x);
                    if (res.Count >= n) throw new ThrowSignal { Tag = tag };
                    return null;
                },
            });
        }
        catch (ThrowSignal t) when (ReferenceEquals(t.Tag, tag)) { }
        return res;
    }

    // Enumerator#next buffers the items (Ruby uses a Fiber); an endless source is cut off at a cap.
    void Buffer(REnumerator e)
    {
        if (e.Buf != null) return;
        e.Buf = EnumTake(e, 100_000, null);
    }

    object LazyEach(REnumerator e, RProc blk)
    {
        if (e.Meth == "each") return Invoke(e.Recv, "each", NoArgs, blk);
        var a = e.Args;
        var f = e.Gen;
        var tag = new object();
        long count = 0, index = a.Length > 0 && a[0] is long off ? off : 0;
        bool dropping = true;
        Func<object, object> step = e.Meth switch
        {
            "__lazy_map" or "__lazy_collect" => x => CallProc(blk, new[] { CallProc(f, new[] { x }) }),
            "__lazy_select" or "__lazy_filter" => x => Truthy(CallProc(f, new[] { x })) ? CallProc(blk, new[] { x }) : null,
            "__lazy_reject" => x => !Truthy(CallProc(f, new[] { x })) ? CallProc(blk, new[] { x }) : null,
            "__lazy_filter_map" => x => CallProc(f, new[] { x }) is var y && Truthy(y) ? CallProc(blk, new[] { y }) : null,
            "__lazy_flat_map" => x =>
            {
                var y = CallProc(f, new[] { x });
                foreach (var z in y is RArray ya ? ya.L : new List<object> { y }) CallProc(blk, new[] { z });
                return null;
            },
            "__lazy_with_index" => x => f != null ? CallProc(blk, new[] { CallProc(f, new[] { x, (object)index++ }) }) : CallProc(blk, new[] { new RArray(new[] { x, (object)index++ }) }),
            "__lazy_take_while" => x => Truthy(CallProc(f, new[] { x })) ? CallProc(blk, new[] { x }) : throw new ThrowSignal { Tag = tag },
            "__lazy_drop_while" => x => (dropping = dropping && Truthy(CallProc(f, new[] { x }))) ? null : CallProc(blk, new[] { x }),
            "__lazy_take" => x =>
            {
                long n = Int(a[0]);
                if (count >= n) throw new ThrowSignal { Tag = tag };
                CallProc(blk, new[] { x });
                if (++count >= n) throw new ThrowSignal { Tag = tag };
                return null;
            },
            _ => x => CallProc(blk, new[] { x }),
        };
        if (e.Meth == "__lazy_take" && Int(a[0]) <= 0) return null;
        try
        {
            Invoke(e.Recv, "each", NoArgs, new RProc { Fn = (it, s, ya, yb) => step(Packed(ya)) });
        }
        catch (ThrowSignal t) when (ReferenceEquals(t.Tag, tag)) { }
        return null;
    }

    // ---- Integer#step, Float#step, Range#step / % ----

    void InitSteps()
    {
        Native numStep = (it, self, a, b) =>
        {
            object limit = null, step = 1L;
            var pos = a.Where(x => x is not RHash).ToList();
            if (pos.Count > 0) limit = pos[0];
            if (pos.Count > 1) step = pos[1];
            if (a.Length > 0 && a[^1] is RHash kw)
            {
                if (kw.D.TryGetValue(Sym("by"), out var by)) step = by;
                if (kw.D.TryGetValue(Sym("to"), out var to)) limit = to;
            }
            if (step is long z && z == 0 && b != null) throw it.ArgError("step can't be 0");
            if (b == null) return it.MakeEnum(self, "step", a, null);
            it.NumStep(self, limit, step, false, b);
            return self;
        };
        Def(NumericClass, "step", numStep);
        Def(IntegerClass, "step", numStep);
        Def(FloatClass, "step", numStep);
        Def(RangeClass, "step %", (it, self, a, b) =>
        {
            var r = (RRange)self;
            var step = a.Length > 0 ? a[0] : 1L;
            if (it.Num(step) < 0) throw it.ArgError("step can't be negative");
            if (it.Num(step) == 0) throw it.ArgError("step can't be 0");
            if (b == null) return it.MakeEnum(self, "step", a, null);
            if (r.From is RString)
            {
                long k = 0, n = it.Int(step);
                foreach (var x in it.ToList(self)) if (k++ % n == 0) it.CallProc(b, new[] { x });
                return self;
            }
            it.NumStep(r.From, r.To, step, r.Excl, b);
            return self;
        });
    }

    void NumStep(object from, object limit, object step, bool excl, RProc b)
    {
        if (from is double || limit is double || step is double)
        {
            // Ruby's ruby_float_step: n = floor((end - beg) / unit + err), each value beg + i * unit.
            double beg = Num(from), unit = Num(step);
            double end = limit == null ? (unit < 0 ? double.NegativeInfinity : double.PositiveInfinity) : Num(limit);
            if (double.IsInfinity(end) && !double.IsInfinity(unit))
            {
                if (unit > 0 ? beg <= end : beg >= end)
                    for (long i = 0; ; i++) CallProc(b, new object[] { i * unit + beg });
                return;
            }
            if (double.IsInfinity(unit))
            {
                if (unit > 0 ? beg <= end : beg >= end) CallProc(b, new object[] { beg });
                return;
            }
            double n = (end - beg) / unit;
            double err = (Math.Abs(beg) + Math.Abs(end) + Math.Abs(end - beg)) / Math.Abs(unit) * 2.220446049250313e-16;   // DBL_EPSILON
            if (err > 0.5) err = 0.5;
            if (excl)
            {
                if (n <= 0) return;
                n = n < 1 ? 0 : Math.Floor(n - err);
                double d = (n + 1) * unit + beg;
                if (beg < end ? d < end : beg > end && d > end) n++;
            }
            else
            {
                if (n < 0) return;
                n = Math.Floor(n + err);
            }
            for (long i = 0; i <= (long)n; i++)
            {
                double v = i * unit + beg;
                if (unit >= 0 ? end < v : v < end) v = end;
                CallProc(b, new object[] { v });
            }
            return;
        }
        var s = Big(step);
        bool up = s.Sign > 0;
        if (s.IsZero) throw ArgError("step can't be 0");
        if (from is long f && step is long st && (limit is long || limit == null))
        {
            long lim = limit is long l ? l : (up ? long.MaxValue : long.MinValue);
            for (long x = f; up ? (excl ? x < lim : x <= lim) : (excl ? x > lim : x >= lim);)
            {
                CallProc(b, new object[] { x });
                if (AddInt(x, st) is not long nx) break;
                x = nx;
            }
            return;
        }
        BigInteger cur = Big(from);
        for (; ; cur += s)
        {
            if (limit != null)
            {
                int c = cur.CompareTo(Big(limit));
                if (up ? (excl ? c >= 0 : c > 0) : (excl ? c <= 0 : c < 0)) break;
            }
            CallProc(b, new[] { Norm(cur) });
        }
    }
}
