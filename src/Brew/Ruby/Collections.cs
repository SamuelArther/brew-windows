namespace Brew.Ruby;

public sealed partial class Interp
{
    public List<object> ToList(object o)
    {
        switch (o)
        {
            case RArray a: return a.L.ToList();
            case RHash h: return h.D.Select(kv => (object)Pair(kv.Key, kv.Value)).ToList();
            case RRange r: return RangeItems(r).ToList();
            case null: throw NoMethod(null, "each", false);
        }
        var list = new List<object>();
        Invoke(o, "each", NoArgs, new RProc { Fn = (it, s, a, b) => { list.Add(a.Length == 1 ? a[0] : new RArray(a)); return null; } });
        return list;
    }

    static RArray Pair(object a, object b) => new(new[] { a, b });

    IEnumerable<object> RangeItems(RRange r)
    {
        if (r.From is long from)
        {
            if (r.To == null) { for (long i = from; ; i++) yield return i; }
            long to = Int(r.To is double d ? (long)Math.Floor(d) : r.To);
            if (r.Excl && r.To is long) to--;
            for (long i = from; i <= to; i++) yield return i;
            yield break;
        }
        if (r.From is RString fs && r.To is RString ts)
        {
            var s = fs.V;
            for (int guard = 0; guard < 1_000_000; guard++)
            {
                if (s == ts.V) { if (!r.Excl) yield return S(s); yield break; }
                if (s.Length > ts.V.Length) yield break;
                yield return S(s);
                s = Succ(s);
            }
            yield break;
        }
        // Anything else with #succ (Date, Time...) steps by succ, like Range#each.
        if (r.From is RObject && RespondTo(r.From, "succ"))
        {
            for (var x = r.From; r.To == null || (r.Excl ? Compare(x, r.To) < 0 : Compare(x, r.To) <= 0); x = Invoke(x, "succ", NoArgs, null))
                yield return x;
            yield break;
        }
        throw TypeErr($"can't iterate from {ClassOf(r.From).Name}");
    }

    public bool RangeIncludes(RRange r, object v)
    {
        if (v == null) return false;
        try
        {
            if (r.From != null && Compare(r.From, v) > 0) return false;
            if (r.To == null) return true;
            int c = Compare(v, r.To);
            return r.Excl ? c < 0 : c <= 0;
        }
        catch (RubyError) { return false; }
    }

    public object HashGet(RHash h, object key)
    {
        if (h.D.TryGetValue(key, out var v)) return v;
        if (h.DefaultProc != null) return CallProc(h.DefaultProc, new[] { h, key });
        return h.Default;
    }

    void CheckFrozen(RArray a) { if (a.Frozen) throw Error(FrozenError, $"can't modify frozen Array: {Inspect(a)}"); }
    void CheckFrozen(RHash h) { if (h.Frozen) throw Error(FrozenError, $"can't modify frozen Hash: {Inspect(h)}"); }

    object Y1(RProc b, object x) => CallProc(b, new[] { x });

    // Enumerable-style test: block, or pattern arg (===), or truthiness.
    Func<object, bool> Pred(object[] a, RProc b) =>
        a.Length > 0 ? x => CaseEq(a[0], x) : b != null ? x => Truthy(Y1(b, x)) : x => Truthy(x);

    void InitCollections()
    {
        var En = EnumerableModule;
        Def(En, "to_a entries", (it, self, a, b) => new RArray(it.ToList(self)));
        Def(En, "each_entry", (it, self, a, b) => { foreach (var x in it.ToList(self)) it.Y1(b, x); return self; });
        Def(En, "each_with_index", (it, self, a, b) =>
        {
            var items = it.ToList(self);
            if (b == null) return new RArray(items.Select((x, i) => (object)Pair(x, (long)i)));
            for (int i = 0; i < items.Count; i++) it.CallProc(b, new[] { items[i], (long)i });
            return self;
        });
        Def(En, "with_index each_with_index_map", (it, self, a, b) =>
        {
            long off = a.Length > 0 ? it.Int(a[0]) : 0;
            var items = it.ToList(self);
            return new RArray(items.Select((x, i) => it.CallProc(b, new[] { x, off + i })));
        });
        Def(En, "each_with_object", (it, self, a, b) =>
        {
            foreach (var x in it.ToList(self)) it.CallProc(b, new[] { x, a[0] });
            return a[0];
        });
        Def(En, "map collect", (it, self, a, b) => b == null ? new RArray(it.ToList(self)) : new RArray(it.ToList(self).Select(x => it.Y1(b, x))));
        Def(En, "flat_map collect_concat", (it, self, a, b) =>
            new RArray(it.ToList(self).SelectMany(x => it.Y1(b, x) is RArray r ? r.L : new List<object> { it.Y1(b, x) })));
        Def(En, "select filter find_all", (it, self, a, b) => new RArray(it.ToList(self).Where(x => Truthy(it.Y1(b, x)))));
        Def(En, "reject", (it, self, a, b) => new RArray(it.ToList(self).Where(x => !Truthy(it.Y1(b, x)))));
        Def(En, "filter_map", (it, self, a, b) => new RArray(it.ToList(self).Select(x => it.Y1(b, x)).Where(Truthy)));
        Def(En, "find detect", (it, self, a, b) => it.ToList(self).FirstOrDefault(x => Truthy(it.Y1(b, x))));
        Def(En, "grep", (it, self, a, b) =>
            new RArray(it.ToList(self).Where(x => it.CaseEq(a[0], x)).Select(x => b != null ? it.Y1(b, x) : x)));
        Def(En, "grep_v", (it, self, a, b) =>
            new RArray(it.ToList(self).Where(x => !it.CaseEq(a[0], x)).Select(x => b != null ? it.Y1(b, x) : x)));
        Def(En, "find_index", (it, self, a, b) =>
        {
            var items = it.ToList(self);
            int i = a.Length > 0 ? items.FindIndex(x => it.Equal(x, a[0])) : items.FindIndex(x => Truthy(it.Y1(b, x)));
            return i < 0 ? null : (long)i;
        });
        Def(En, "include? member?", (it, self, a, b) => it.ToList(self).Any(x => it.Equal(x, a[0])));
        Def(En, "exclude?", (it, self, a, b) => !Truthy(it.Invoke(self, "include?", a, null)));   // ActiveSupport
        Def(En, "any?", (it, self, a, b) => it.ToList(self).Any(it.Pred(a, b)));
        Def(En, "all?", (it, self, a, b) => it.ToList(self).All(it.Pred(a, b)));
        Def(En, "none?", (it, self, a, b) => !it.ToList(self).Any(it.Pred(a, b)));
        Def(En, "one?", (it, self, a, b) => it.ToList(self).Count(it.Pred(a, b)) == 1);
        Def(En, "count", (it, self, a, b) =>
            (long)(a.Length > 0 ? it.ToList(self).Count(x => it.Equal(x, a[0])) : b != null ? it.ToList(self).Count(x => Truthy(it.Y1(b, x))) : it.ToList(self).Count));
        Def(En, "sum", (it, self, a, b) =>
        {
            object acc = a.Length > 0 ? a[0] : 0L;
            foreach (var x in it.ToList(self))
            {
                var v = b != null ? it.Y1(b, x) : x;
                acc = it.TryArith(acc, "+", v, out var r) ? r : it.Invoke(acc, "+", new[] { v }, null);
            }
            return acc;
        });
        Def(En, "inject reduce", (it, self, a, b) =>
        {
            var items = it.ToList(self);
            object acc;
            int start = 0;
            string op = null;
            if (a.Length == 2) { acc = a[0]; op = it.StrOrSym(a[1]); }
            else if (a.Length == 1 && b == null) { op = it.StrOrSym(a[0]); if (items.Count == 0) return null; acc = items[0]; start = 1; }
            else if (a.Length == 1) acc = a[0];
            else { if (items.Count == 0) return null; acc = items[0]; start = 1; }
            for (int i = start; i < items.Count; i++)
                acc = op != null ? it.Invoke(acc, op, new[] { items[i] }, null) : it.CallProc(b, new[] { acc, items[i] });
            return acc;
        });
        Def(En, "min", (it, self, a, b) => it.MinMax(it.ToList(self), b, -1, a));
        Def(En, "max", (it, self, a, b) => it.MinMax(it.ToList(self), b, 1, a));
        Def(En, "min_by", (it, self, a, b) => it.ToList(self).Select(x => (x, k: it.Y1(b, x))).Aggregate<(object x, object k), (object x, object k)?>(null, (best, c) => best == null || it.Compare(c.k, best.Value.k) < 0 ? c : best)?.x);
        Def(En, "max_by", (it, self, a, b) => it.ToList(self).Select(x => (x, k: it.Y1(b, x))).Aggregate<(object x, object k), (object x, object k)?>(null, (best, c) => best == null || it.Compare(c.k, best.Value.k) > 0 ? c : best)?.x);
        Def(En, "minmax", (it, self, a, b) => new RArray(new[] { it.MinMax(it.ToList(self), b, -1, NoArgs), it.MinMax(it.ToList(self), b, 1, NoArgs) }));
        Def(En, "sort", (it, self, a, b) => new RArray(it.SortList(it.ToList(self), b)));
        Def(En, "sort_by", (it, self, a, b) =>
        {
            var keyed = it.ToList(self).Select(x => (x, k: it.Y1(b, x))).ToList();
            return new RArray(StableSort(keyed, (p, q) => it.Compare(p.k, q.k)).Select(p => p.x));
        });
        Def(En, "group_by", (it, self, a, b) =>
        {
            var h = new RHash();
            foreach (var x in it.ToList(self))
            {
                var k = it.Y1(b, x);
                if (!h.D.TryGetValue(k, out var lst)) h.D[k] = lst = new RArray();
                ((RArray)lst).L.Add(x);
            }
            return h;
        });
        Def(En, "partition", (it, self, a, b) =>
        {
            var yes = new RArray();
            var no = new RArray();
            foreach (var x in it.ToList(self)) (Truthy(it.Y1(b, x)) ? yes : no).L.Add(x);
            return Pair(yes, no);
        });
        Def(En, "each_slice", (it, self, a, b) =>
        {
            int n = (int)it.Int(a[0]);
            var slices = it.ToList(self).Chunk(n).Select(c => (object)new RArray(c)).ToList();
            if (b == null) return new RArray(slices);
            foreach (var s in slices) it.Y1(b, s);
            return self;
        });
        Def(En, "each_cons", (it, self, a, b) =>
        {
            int n = (int)it.Int(a[0]);
            var items = it.ToList(self);
            var res = new List<object>();
            for (int i = 0; i + n <= items.Count; i++) res.Add(new RArray(items.Skip(i).Take(n)));
            if (b == null) return new RArray(res);
            foreach (var s in res) it.Y1(b, s);
            return self;
        });
        Def(En, "tally", (it, self, a, b) =>
        {
            var h = new RHash();
            foreach (var x in it.ToList(self)) h.D[x] = (h.D.TryGetValue(x, out var c) ? (long)c : 0L) + 1;
            return h;
        });
        Def(En, "to_h", (it, self, a, b) =>
        {
            var h = new RHash();
            foreach (var x in it.ToList(self))
            {
                var p = b != null ? it.Y1(b, x) : x;
                if (p is not RArray { L.Count: 2 } pa) throw it.TypeErr($"wrong element type {it.ClassOf(p).Name} (expected array)");
                h.D[pa.L[0]] = pa.L[1];
            }
            return h;
        });
        Def(En, "to_set", (it, self, a, b) => it.Invoke(it.ConstGet(it.ObjectClass, "Set"), "new", new[] { self }, null));
        Def(En, "uniq", (it, self, a, b) => new RArray(it.Uniq(it.ToList(self), b)));
        Def(En, "zip", (it, self, a, b) =>
        {
            var items = it.ToList(self);
            var others = a.Select(o => it.ToList(o)).ToList();
            return new RArray(items.Select((x, i) => (object)new RArray(others.Select(o => i < o.Count ? o[i] : null).Prepend(x))));
        });
        Def(En, "first", (it, self, a, b) =>
        {
            if (self is RRange r && a.Length == 0) return r.From;
            var items = a.Length == 0 ? it.ToListLimited(self, 1) : it.ToListLimited(self, (int)it.Int(a[0]));
            return a.Length == 0 ? items.FirstOrDefault() : new RArray(items);
        });
        Def(En, "take", (it, self, a, b) => new RArray(it.ToListLimited(self, (int)it.Int(a[0]))));
        Def(En, "drop", (it, self, a, b) => new RArray(it.ToList(self).Skip((int)it.Int(a[0]))));
        Def(En, "take_while", (it, self, a, b) => new RArray(it.ToList(self).TakeWhile(x => Truthy(it.Y1(b, x)))));
        Def(En, "drop_while", (it, self, a, b) => new RArray(it.ToList(self).SkipWhile(x => Truthy(it.Y1(b, x)))));
        Def(En, "chunk_while", (it, self, a, b) => it.ChunkWhile(self, b, false));
        Def(En, "slice_when", (it, self, a, b) => it.ChunkWhile(self, b, true));
        Def(En, "reverse_each", (it, self, a, b) =>
        {
            if (b == null) return new RArray(Enumerable.Reverse(it.ToList(self)));
            foreach (var x in Enumerable.Reverse(it.ToList(self))) it.Y1(b, x);
            return self;
        });
        Def(En, "lazy each_entry to_enum enum_for", (it, self, a, b) => new RArray(it.ToList(self)));
        Def(En, "cycle", (it, self, a, b) =>
        {
            var items = it.ToList(self);
            long n = a.Length > 0 ? it.Int(a[0]) : long.MaxValue;
            for (long k = 0; k < n && items.Count > 0; k++) foreach (var x in items) it.Y1(b, x);
            return null;
        });

        InitArray();
        InitHash();
        InitRange();
    }

    RArray ChunkWhile(object self, RProc b, bool sliceWhen)
    {
        var items = ToList(self);
        var res = new RArray();
        RArray cur = null;
        for (int i = 0; i < items.Count; i++)
        {
            if (cur == null) { cur = new RArray(); cur.L.Add(items[i]); continue; }
            bool split = Truthy(CallProc(b, new[] { items[i - 1], items[i] })) == sliceWhen;
            if (!split) cur.L.Add(items[i]);
            else { res.L.Add(cur); cur = new RArray(); cur.L.Add(items[i]); }
        }
        if (cur != null) res.L.Add(cur);
        return res;
    }

    List<object> ToListLimited(object self, int n)
    {
        if (self is RRange r) return RangeItems(r).Take(n).ToList();
        return ToList(self).Take(n).ToList();
    }

    object MinMax(List<object> items, RProc b, int dir, object[] a)
    {
        Comparison<object> cmp = b != null ? (x, y) => (int)Int(CallProc(b, new[] { x, y })) : Compare;
        if (a.Length > 0)
        {
            var sorted = StableSort(items, cmp);
            if (dir > 0) sorted.Reverse();
            return new RArray(sorted.Take((int)Int(a[0])));
        }
        object best = null;
        bool first = true;
        foreach (var x in items)
        {
            if (first || Math.Sign(cmp(x, best)) == dir) best = x;
            first = false;
        }
        return best;
    }

    List<object> SortList(List<object> items, RProc b) =>
        StableSort(items, b != null ? (x, y) => (int)Int(CallProc(b, new[] { x, y })) : Compare);

    static List<T> StableSort<T>(List<T> items, Comparison<T> cmp) =>
        items.Select((x, i) => (x, i)).OrderBy(p => p, Comparer<(T x, int i)>.Create((p, q) =>
        {
            int c = cmp(p.x, q.x);
            return c != 0 ? c : p.i.CompareTo(q.i);
        })).Select(p => p.x).ToList();

    List<object> Uniq(List<object> items, RProc b)
    {
        var seen = new HashSet<object>(KeyComparer.Instance);
        var res = new List<object>();
        foreach (var x in items)
            if (seen.Add(b != null ? Y1(b, x) : x)) res.Add(x);
        return res;
    }

    List<object> Flatten(IEnumerable<object> items, int depth)
    {
        var res = new List<object>();
        foreach (var x in items)
        {
            if (x is RArray a && depth != 0) res.AddRange(Flatten(a.L, depth - 1));
            else res.Add(x);
        }
        return res;
    }

    void InitArray()
    {
        var A = ArrayClass;
        RArray Self(object o) => (RArray)o;
        RArray Mut(Interp it, object o) { var a = (RArray)o; it.CheckFrozen(a); return a; }

        DefS(A, "new", (it, self, a, b) =>
        {
            var arr = new RArray();
            if (a.Length > 0 && a[0] is RArray src) { arr.L.AddRange(src.L); return arr; }
            long n = a.Length > 0 ? it.Int(a[0]) : 0;
            for (long i = 0; i < n; i++) arr.L.Add(b != null ? it.Y1(b, i) : a.Length > 1 ? a[1] : null);
            return arr;
        });
        DefS(A, "[]", (it, self, a, b) => new RArray(a));
        Def(A, "[] slice", (it, self, a, b) =>
        {
            var l = Self(self).L;
            var (start, len) = it.SliceBounds(l.Count, a);
            if (a.Length == 1 && a[0] is long) return start < 0 ? null : l[start];
            if (start < 0) return null;
            return new RArray(l.GetRange(start, len));
        });
        Def(A, "[]=", (it, self, a, b) =>
        {
            var arr = Mut(it, self);
            var l = arr.L;
            var v = a[^1];
            if (a.Length == 2 && a[0] is long li)
            {
                int i = (int)li;
                if (i < 0) i += l.Count;
                if (i < 0) throw it.Error(it.IndexError, $"index {li} too small for array");
                while (l.Count <= i) l.Add(null);
                l[i] = v;
                return v;
            }
            var (start, len) = it.SliceBounds(l.Count, a[..^1]);
            if (start < 0) throw it.Error(it.IndexError, "index out of range");
            l.RemoveRange(start, len);
            l.InsertRange(start, v is RArray va ? va.L : new List<object> { v });
            return v;
        });
        Def(A, "at", (it, self, a, b) => { var l = Self(self).L; int i = (int)it.Int(a[0]); if (i < 0) i += l.Count; return i >= 0 && i < l.Count ? l[i] : null; });
        Def(A, "dig", (it, self, a, b) =>
        {
            object cur = self;
            foreach (var k in a)
            {
                if (cur == null) return null;
                cur = it.Invoke(cur, "[]", new[] { k }, null);
            }
            return cur;
        });
        Def(A, "fetch", (it, self, a, b) =>
        {
            var l = Self(self).L;
            int i = (int)it.Int(a[0]);
            int j = i < 0 ? i + l.Count : i;
            if (j >= 0 && j < l.Count) return l[j];
            if (b != null) return it.Y1(b, a[0]);
            if (a.Length > 1) return a[1];
            throw it.Error(it.IndexError, $"index {i} outside of array bounds: {-l.Count}...{l.Count}");
        });
        Def(A, "<< push append", (it, self, a, b) => { Mut(it, self).L.AddRange(a); return self; });
        Def(A, "pop", (it, self, a, b) =>
        {
            var l = Mut(it, self).L;
            if (a.Length > 0) { int n = Math.Min((int)it.Int(a[0]), l.Count); var r = l.GetRange(l.Count - n, n); l.RemoveRange(l.Count - n, n); return new RArray(r); }
            if (l.Count == 0) return null;
            var x = l[^1];
            l.RemoveAt(l.Count - 1);
            return x;
        });
        Def(A, "shift", (it, self, a, b) =>
        {
            var l = Mut(it, self).L;
            if (a.Length > 0) { int n = Math.Min((int)it.Int(a[0]), l.Count); var r = l.GetRange(0, n); l.RemoveRange(0, n); return new RArray(r); }
            if (l.Count == 0) return null;
            var x = l[0];
            l.RemoveAt(0);
            return x;
        });
        Def(A, "unshift prepend", (it, self, a, b) => { Mut(it, self).L.InsertRange(0, a); return self; });
        Def(A, "insert", (it, self, a, b) =>
        {
            var l = Mut(it, self).L;
            int i = (int)it.Int(a[0]);
            if (i < 0) i += l.Count + 1;
            while (l.Count < i) l.Add(null);
            l.InsertRange(i, a[1..]);
            return self;
        });
        Def(A, "concat", (it, self, a, b) => { foreach (var x in a) Mut(it, self).L.AddRange(it.Ary(x).L); return self; });
        Def(A, "length size", (it, self, a, b) => (long)Self(self).L.Count);
        Def(A, "empty?", (it, self, a, b) => Self(self).L.Count == 0);
        Def(A, "first", (it, self, a, b) => a.Length == 0 ? Self(self).L.FirstOrDefault() : new RArray(Self(self).L.Take((int)it.Int(a[0]))));
        Def(A, "last", (it, self, a, b) => a.Length == 0 ? Self(self).L.LastOrDefault() : new RArray(Self(self).L.TakeLast((int)it.Int(a[0]))));
        // Homebrew's extend/array.rb (sftpgo: `read.lines.second`).
        foreach (var (method, index) in new[] { ("second", 1), ("third", 2), ("fourth", 3), ("fifth", 4) })
            Def(A, method, (it, self, a, b) => Self(self).L.ElementAtOrDefault(index));
        Def(A, "each", (it, self, a, b) =>
        {
            if (b == null) return new RArray(Self(self).L);
            var l = Self(self).L;
            for (int i = 0; i < l.Count; i++) it.Y1(b, l[i]);
            return self;
        });
        Def(A, "each_index", (it, self, a, b) => { for (int i = 0; i < Self(self).L.Count; i++) it.Y1(b, (long)i); return self; });
        Def(A, "map! collect!", (it, self, a, b) => { var l = Mut(it, self).L; for (int i = 0; i < l.Count; i++) l[i] = it.Y1(b, l[i]); return self; });
        Def(A, "select! filter! keep_if", (it, self, a, b) => { var l = Mut(it, self).L; l.RemoveAll(x => !Truthy(it.Y1(b, x))); return self; });
        Def(A, "reject! delete_if", (it, self, a, b) => { var l = Mut(it, self).L; int n = l.RemoveAll(x => Truthy(it.Y1(b, x))); return n == 0 ? null : self; });
        Def(A, "compact", (it, self, a, b) => new RArray(Self(self).L.Where(x => x != null)));
        Def(A, "compact!", (it, self, a, b) => Mut(it, self).L.RemoveAll(x => x == null) == 0 ? null : self);
        Def(A, "flatten", (it, self, a, b) => new RArray(it.Flatten(Self(self).L, a.Length > 0 ? (int)it.Int(a[0]) : -1)));
        Def(A, "flatten!", (it, self, a, b) => { var l = Mut(it, self); l.L = it.Flatten(l.L, a.Length > 0 ? (int)it.Int(a[0]) : -1); return self; });
        Def(A, "uniq!", (it, self, a, b) => { var l = Mut(it, self); var u = it.Uniq(l.L, b); if (u.Count == l.L.Count) return null; l.L = u; return self; });
        Def(A, "reverse", (it, self, a, b) => new RArray(Enumerable.Reverse(Self(self).L)));
        Def(A, "reverse!", (it, self, a, b) => { Mut(it, self).L.Reverse(); return self; });
        Def(A, "rotate", (it, self, a, b) =>
        {
            var l = Self(self).L;
            if (l.Count == 0) return new RArray();
            int n = (int)(((a.Length > 0 ? it.Int(a[0]) : 1) % l.Count + l.Count) % l.Count);
            return new RArray(l.Skip(n).Concat(l.Take(n)));
        });
        Def(A, "join", (it, self, a, b) =>
        {
            var sep = a.Length > 0 && a[0] != null ? it.Str(a[0]) : "";
            return S(string.Join(sep, it.Flatten(Self(self).L, -1).Select(it.ToS)));
        });
        Def(A, "+", (it, self, a, b) => new RArray(Self(self).L.Concat(it.Ary(a[0]).L)));
        Def(A, "-", (it, self, a, b) =>
        {
            var other = new HashSet<object>(it.Ary(a[0]).L, KeyComparer.Instance);
            return new RArray(Self(self).L.Where(x => !other.Contains(x)));
        });
        Def(A, "difference", (it, self, a, b) =>
        {
            var other = new HashSet<object>(a.SelectMany(x => it.Ary(x).L), KeyComparer.Instance);
            return new RArray(Self(self).L.Where(x => !other.Contains(x)));
        });
        Def(A, "*", (it, self, a, b) => a[0] is RString sep
            ? S(string.Join(sep.V, Self(self).L.Select(it.ToS)))
            : new RArray(Enumerable.Repeat(Self(self).L, (int)it.Int(a[0])).SelectMany(x => x)));
        Def(A, "& intersection", (it, self, a, b) =>
        {
            var other = new HashSet<object>(it.Ary(a[0]).L, KeyComparer.Instance);
            return new RArray(it.Uniq(Self(self).L.Where(other.Contains).ToList(), null));
        });
        Def(A, "intersect?", (it, self, a, b) => { var other = new HashSet<object>(it.Ary(a[0]).L, KeyComparer.Instance); return Self(self).L.Any(other.Contains); });
        Def(A, "| union", (it, self, a, b) => new RArray(it.Uniq(Self(self).L.Concat(a.SelectMany(x => it.Ary(x).L)).ToList(), null)));
        Def(A, "==", (it, self, a, b) => it.Equal(self, a[0]));
        Def(A, "eql?", (it, self, a, b) => Eql(self, a[0]));
        Def(A, "hash", (it, self, a, b) => (long)KeyComparer.Instance.GetHashCode(self));
        Def(A, "<=>", (it, self, a, b) =>
        {
            if (a[0] is not RArray o) return null;
            var l = Self(self).L;
            for (int i = 0; i < Math.Min(l.Count, o.L.Count); i++)
            {
                int c = it.Compare(l[i], o.L[i]);
                if (c != 0) return (long)c;
            }
            return (long)l.Count.CompareTo(o.L.Count);
        });
        Def(A, "delete", (it, self, a, b) =>
        {
            var l = Mut(it, self).L;
            object found = null;
            bool any = false;
            for (int i = l.Count - 1; i >= 0; i--)
                if (it.Equal(l[i], a[0])) { found = l[i]; any = true; l.RemoveAt(i); }
            return any ? found : b != null ? it.Y1(b, a[0]) : null;
        });
        Def(A, "delete_at", (it, self, a, b) =>
        {
            var l = Mut(it, self).L;
            int i = (int)it.Int(a[0]);
            if (i < 0) i += l.Count;
            if (i < 0 || i >= l.Count) return null;
            var x = l[i];
            l.RemoveAt(i);
            return x;
        });
        Def(A, "clear", (it, self, a, b) => { Mut(it, self).L.Clear(); return self; });
        Def(A, "replace", (it, self, a, b) => { Mut(it, self).L = it.Ary(a[0]).L.ToList(); return self; });
        Def(A, "fill", (it, self, a, b) => { var l = Mut(it, self).L; for (int i = 0; i < l.Count; i++) l[i] = b != null ? it.Y1(b, (long)i) : a[0]; return self; });
        Def(A, "index find_index", (it, self, a, b) =>
        {
            var l = Self(self).L;
            int i = a.Length > 0 ? l.FindIndex(x => it.Equal(x, a[0])) : l.FindIndex(x => Truthy(it.Y1(b, x)));
            return i < 0 ? null : (long)i;
        });
        Def(A, "rindex", (it, self, a, b) =>
        {
            var l = Self(self).L;
            int i = a.Length > 0 ? l.FindLastIndex(x => it.Equal(x, a[0])) : l.FindLastIndex(x => Truthy(it.Y1(b, x)));
            return i < 0 ? null : (long)i;
        });
        Def(A, "include? member?", (it, self, a, b) => Self(self).L.Any(x => it.Equal(x, a[0])));
        Def(A, "to_a to_ary entries", (it, self, a, b) => self);
        Def(A, "inspect to_s", (it, self, a, b) => S(it.Inspect(self)));
        Def(A, "sort!", (it, self, a, b) => { var l = Mut(it, self); l.L = it.SortList(l.L, b); return self; });
        Def(A, "sort_by!", (it, self, a, b) => { var l = Mut(it, self); l.L = ((RArray)it.Invoke(new RArray(l.L), "sort_by", NoArgs, b)).L; return self; });
        Def(A, "sample", (it, self, a, b) => Self(self).L.Count == 0 ? null : Self(self).L[Random.Shared.Next(Self(self).L.Count)]);
        Def(A, "shuffle", (it, self, a, b) => new RArray(Self(self).L.OrderBy(_ => Random.Shared.Next())));
        Def(A, "product", (it, self, a, b) =>
        {
            IEnumerable<List<object>> acc = Self(self).L.Select(x => new List<object> { x });
            foreach (var o in a) { var ol = it.Ary(o).L; acc = acc.SelectMany(p => ol.Select(x => p.Append(x).ToList())); }
            return new RArray(acc.Select(p => (object)new RArray(p)));
        });
        Def(A, "transpose", (it, self, a, b) =>
        {
            var rows = Self(self).L.Select(r => it.Ary(r).L).ToList();
            if (rows.Count == 0) return new RArray();
            return new RArray(Enumerable.Range(0, rows[0].Count).Select(i => (object)new RArray(rows.Select(r => r[i]))));
        });
        Def(A, "assoc", (it, self, a, b) => Self(self).L.FirstOrDefault(x => x is RArray p && p.L.Count > 0 && it.Equal(p.L[0], a[0])));
        Def(A, "values_at", (it, self, a, b) => new RArray(a.Select(i => { var l = Self(self).L; int j = (int)it.Int(i); if (j < 0) j += l.Count; return j >= 0 && j < l.Count ? l[j] : null; })));
        Def(A, "combination", (it, self, a, b) =>
        {
            int k = (int)it.Int(a[0]);
            var l = Self(self).L;
            var res = new RArray();
            void Rec(int start, List<object> cur)
            {
                if (cur.Count == k) { res.L.Add(new RArray(cur)); return; }
                for (int i = start; i < l.Count; i++) { cur.Add(l[i]); Rec(i + 1, cur); cur.RemoveAt(cur.Count - 1); }
            }
            Rec(0, new List<object>());
            if (b == null) return res;
            foreach (var c in res.L) it.Y1(b, c);
            return self;
        });
        Def(A, "pack", (it, self, a, b) => it.Pack(((RArray)self).L, it.Str(a[0])));
        Def(A, "sum", (it, self, a, b) =>
        {
            object acc = a.Length > 0 ? a[0] : 0L;
            foreach (var x in Self(self).L)
            {
                var v = b != null ? it.Y1(b, x) : x;
                acc = it.TryArith(acc, "+", v, out var r) ? r : it.Invoke(acc, "+", new[] { v }, null);
            }
            return acc;
        });
        Def(A, "freeze", (it, self, a, b) => { Self(self).Frozen = true; return self; });
    }

    void InitHash()
    {
        var H = HashClass;
        RHash Self(object o) => (RHash)o;
        RHash Mut(Interp it, object o) { var h = (RHash)o; it.CheckFrozen(h); return h; }
        RArray PairOf(KeyValuePair<object, object> kv) => Pair(kv.Key, kv.Value);
        bool Test(Interp it, RProc b, KeyValuePair<object, object> kv) => Truthy(it.CallProc(b, new object[] { kv.Key, kv.Value }));

        DefS(H, "new", (it, self, a, b) =>
        {
            var h = new RHash { DefaultProc = b, Default = a.Length > 0 && a[0] is not RHash { D.Count: > 0 } ? a[0] : null };
            return h;
        });
        DefS(H, "[]", (it, self, a, b) =>
        {
            var h = new RHash();
            if (a.Length == 1 && a[0] is RHash src) { foreach (var kv in src.D) h.D[kv.Key] = kv.Value; return h; }
            if (a.Length == 1 && a[0] is RArray pairs) { foreach (var p in pairs.L) { var pa = it.Ary(p); h.D[pa.L[0]] = pa.L.Count > 1 ? pa.L[1] : null; } return h; }
            for (int i = 0; i + 1 < a.Length; i += 2) h.D[a[i]] = a[i + 1];
            return h;
        });
        Def(H, "[]", (it, self, a, b) => it.HashGet(Self(self), a[0]));
        Def(H, "[]= store", (it, self, a, b) =>
        {
            var key = a[0] is RString ks && !ks.Frozen ? new RString(ks.V) { Frozen = true } : a[0];
            Mut(it, self).D[key] = a[1];
            return a[1];
        });
        Def(H, "fetch", (it, self, a, b) =>
        {
            if (Self(self).D.TryGetValue(a[0], out var v)) return v;
            if (b != null) return it.Y1(b, a[0]);
            if (a.Length > 1) return a[1];
            throw it.Error(it.KeyError, $"key not found: {it.Inspect(a[0])}");
        });
        Def(H, "key? has_key? include? member?", (it, self, a, b) => Self(self).D.ContainsKey(a[0]));
        Def(H, "value? has_value?", (it, self, a, b) => Self(self).D.Values.Any(v => it.Equal(v, a[0])));
        Def(H, "key", (it, self, a, b) => Self(self).D.FirstOrDefault(kv => it.Equal(kv.Value, a[0])).Key);
        Def(H, "keys", (it, self, a, b) => new RArray(Self(self).D.Keys));
        Def(H, "values", (it, self, a, b) => new RArray(Self(self).D.Values));
        Def(H, "values_at", (it, self, a, b) => new RArray(a.Select(k => it.HashGet(Self(self), k))));
        Def(H, "fetch_values", (it, self, a, b) => new RArray(a.Select(k => it.Invoke(self, "fetch", new[] { k }, b))));
        Def(H, "length size", (it, self, a, b) => (long)Self(self).D.Count);
        Def(H, "count", (it, self, a, b) => b == null && a.Length == 0 ? (long)Self(self).D.Count : (long)Self(self).D.Count(kv => Test(it, b, kv)));
        Def(H, "empty?", (it, self, a, b) => Self(self).D.Count == 0);
        Def(H, "each each_pair", (it, self, a, b) =>
        {
            if (b == null) return new RArray(Self(self).D.Select(kv => (object)PairOf(kv)));
            foreach (var kv in Self(self).D.ToList()) it.Y1(b, PairOf(kv));
            return self;
        });
        Def(H, "each_key", (it, self, a, b) => { if (b == null) return new RArray(Self(self).D.Keys); foreach (var k in Self(self).D.Keys.ToList()) it.Y1(b, k); return self; });
        Def(H, "each_value", (it, self, a, b) => { if (b == null) return new RArray(Self(self).D.Values); foreach (var v in Self(self).D.Values.ToList()) it.Y1(b, v); return self; });
        Def(H, "delete", (it, self, a, b) => Mut(it, self).D.Remove(a[0], out var v) ? v : b != null ? it.Y1(b, a[0]) : null);
        Def(H, "delete_if reject!", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            var del = h.D.Where(kv => Test(it, b, kv)).Select(kv => kv.Key).ToList();
            foreach (var k in del) h.D.Remove(k);
            return self;
        });
        Def(H, "select! filter! keep_if", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            var del = h.D.Where(kv => !Test(it, b, kv)).Select(kv => kv.Key).ToList();
            foreach (var k in del) h.D.Remove(k);
            return self;
        });
        Def(H, "select filter", (it, self, a, b) =>
        {
            var r = new RHash();
            foreach (var kv in Self(self).D) if (Test(it, b, kv)) r.D[kv.Key] = kv.Value;
            return r;
        });
        Def(H, "reject", (it, self, a, b) =>
        {
            var r = new RHash();
            foreach (var kv in Self(self).D) if (!Test(it, b, kv)) r.D[kv.Key] = kv.Value;
            return r;
        });
        Def(H, "merge", (it, self, a, b) =>
        {
            var r = it.CopyHash(Self(self));
            foreach (var o in a) it.MergeInto(r, (RHash)o, b);
            return r;
        });
        Def(H, "merge! update", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            foreach (var o in a) it.MergeInto(h, (RHash)o, b);
            return h;
        });
        Def(H, "to_h", (it, self, a, b) =>
        {
            if (b == null) return self;
            var r = new RHash();
            foreach (var kv in Self(self).D)
            {
                var p = it.Ary(it.CallProc(b, new[] { kv.Key, kv.Value }));
                r.D[p.L[0]] = p.L[1];
            }
            return r;
        });
        Def(H, "to_a", (it, self, a, b) => new RArray(Self(self).D.Select(kv => (object)PairOf(kv))));
        Def(H, "transform_values", (it, self, a, b) =>
        {
            var r = new RHash();
            foreach (var kv in Self(self).D) r.D[kv.Key] = it.Y1(b, kv.Value);
            return r;
        });
        Def(H, "transform_values!", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            foreach (var k in h.D.Keys.ToList()) h.D[k] = it.Y1(b, h.D[k]);
            return h;
        });
        Def(H, "transform_keys", (it, self, a, b) =>
        {
            var r = new RHash();
            var map = a.Length > 0 ? a[0] as RHash : null;
            foreach (var kv in Self(self).D)
                r.D[map != null && map.D.TryGetValue(kv.Key, out var nk) ? nk : b != null ? it.Y1(b, kv.Key) : kv.Key] = kv.Value;
            return r;
        });
        Def(H, "transform_keys!", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            var r = (RHash)it.Invoke(h, "transform_keys", a, b);
            h.D = r.D;
            return h;
        });
        Def(H, "invert", (it, self, a, b) => { var r = new RHash(); foreach (var kv in Self(self).D) r.D[kv.Value] = kv.Key; return r; });
        Def(H, "dig", (it, self, a, b) =>
        {
            object cur = self;
            foreach (var k in a)
            {
                if (cur == null) return null;
                cur = it.Invoke(cur, "[]", new[] { k }, null);
            }
            return cur;
        });
        Def(H, "default", (it, self, a, b) => Self(self).Default);
        Def(H, "default=", (it, self, a, b) => Self(self).Default = a[0]);
        Def(H, "default_proc", (it, self, a, b) => Self(self).DefaultProc);
        Def(H, "compact", (it, self, a, b) => { var r = new RHash(); foreach (var kv in Self(self).D) if (kv.Value != null) r.D[kv.Key] = kv.Value; return r; });
        Def(H, "slice", (it, self, a, b) => { var r = new RHash(); foreach (var k in a) if (Self(self).D.TryGetValue(k, out var v)) r.D[k] = v; return r; });
        Def(H, "except", (it, self, a, b) => { var r = it.CopyHash(Self(self)); foreach (var k in a) r.D.Remove(k); return r; });
        Def(H, "any?", (it, self, a, b) => b == null ? Self(self).D.Count > 0 : Self(self).D.Any(kv => Test(it, b, kv)));
        Def(H, "all?", (it, self, a, b) => b == null || Self(self).D.All(kv => Test(it, b, kv)));
        Def(H, "find detect", (it, self, a, b) => Self(self).D.Where(kv => Test(it, b, kv)).Select(PairOf).FirstOrDefault());
        Def(H, "partition", (it, self, a, b) =>
        {
            var yes = new RArray();
            var no = new RArray();
            foreach (var kv in Self(self).D) (Test(it, b, kv) ? yes : no).L.Add(PairOf(kv));
            return Pair(yes, no);
        });
        Def(H, "clear", (it, self, a, b) => { Mut(it, self).D.Clear(); return self; });
        Def(H, "replace", (it, self, a, b) => { var h = Mut(it, self); h.D.Clear(); foreach (var kv in ((RHash)a[0]).D) h.D[kv.Key] = kv.Value; return h; });
        Def(H, "shift", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            if (h.D.Count == 0) return null;
            var kv = h.D.GetAt(0);
            h.D.RemoveAt(0);
            return PairOf(kv);
        });
        Def(H, "==", (it, self, a, b) => it.Equal(self, a[0]));
        Def(H, "inspect to_s", (it, self, a, b) => S(it.Inspect(self)));
        Def(H, "hash", (it, self, a, b) => (long)Self(self).D.Count);
        Def(H, "compare_by_identity rehash", (it, self, a, b) => self);
        Def(H, "freeze", (it, self, a, b) => { Self(self).Frozen = true; return self; });
        Def(H, "sum", (it, self, a, b) =>
        {
            object acc = a.Length > 0 ? a[0] : 0L;
            foreach (var kv in Self(self).D)
            {
                var v = b != null ? it.CallProc(b, new[] { kv.Key, kv.Value }) : PairOf(kv);
                acc = it.TryArith(acc, "+", v, out var r) ? r : it.Invoke(acc, "+", new[] { v }, null);
            }
            return acc;
        });
    }

    void MergeInto(RHash target, RHash other, RProc b)
    {
        foreach (var kv in other.D)
            target.D[kv.Key] = b != null && target.D.TryGetValue(kv.Key, out var old)
                ? CallProc(b, new[] { kv.Key, old, kv.Value })
                : kv.Value;
    }

    void InitRange()
    {
        var Rg = RangeClass;
        RRange Self(object o) => (RRange)o;
        DefS(Rg, "new", (it, self, a, b) => new RRange { From = a[0], To = a[1], Excl = a.Length > 2 && Truthy(a[2]) });
        Def(Rg, "begin", (it, self, a, b) => Self(self).From);
        Def(Rg, "end", (it, self, a, b) => Self(self).To);
        Def(Rg, "last", (it, self, a, b) =>
        {
            if (a.Length == 0) return Self(self).To;
            return new RArray(it.ToList(self).TakeLast((int)it.Int(a[0])));
        });
        Def(Rg, "min", (it, self, a, b) => b != null || a.Length > 0 ? it.MinMax(it.ToList(self), b, -1, a) : it.RangeItems(Self(self)).FirstOrDefault());
        Def(Rg, "max", (it, self, a, b) => b != null || a.Length > 0 ? it.MinMax(it.ToList(self), b, 1, a) : it.ToList(self).LastOrDefault());
        Def(Rg, "size count", (it, self, a, b) => b != null || a.Length > 0 ? (long)it.ToList(self).Count(it.Pred(a, b)) : (long)it.ToList(self).Count);
        Def(Rg, "each", (it, self, a, b) =>
        {
            if (b == null) return new RArray(it.ToList(self));
            foreach (var x in it.RangeItems(Self(self))) it.Y1(b, x);
            return self;
        });
        Def(Rg, "step %", (it, self, a, b) =>
        {
            var r = Self(self);
            var step = a[0];
            var res = new RArray();
            for (object x = r.From; r.To == null || (r.Excl ? it.Compare(x, r.To) < 0 : it.Compare(x, r.To) <= 0); x = it.NumOp(x, "+", step))
            {
                if (b != null) it.Y1(b, x);
                else res.L.Add(x);
            }
            return b != null ? self : res;
        });
        Def(Rg, "include? member? cover? ===", (it, self, a, b) => a[0] is RRange inner
            ? it.RangeIncludes(Self(self), inner.From) && it.RangeIncludes(Self(self), inner.To)
            : it.RangeIncludes(Self(self), a[0]));
        Def(Rg, "exclude_end?", (it, self, a, b) => Self(self).Excl);
        Def(Rg, "to_a to_ary entries", (it, self, a, b) => new RArray(it.ToList(self)));
        Def(Rg, "inspect", (it, self, a, b) => S(it.Inspect(self)));
        Def(Rg, "to_s", (it, self, a, b) => S(it.ToS(Self(self).From) + (Self(self).Excl ? "..." : "..") + it.ToS(Self(self).To)));
        Def(Rg, "==", (it, self, a, b) => it.Equal(self, a[0]));
        Def(Rg, "sum", (it, self, a, b) =>
        {
            var r = Self(self);
            if (b == null && a.Length == 0 && r.From is long f && r.To is long t)
            {
                if (r.Excl) t--;
                return t < f ? 0L : (f + t) * (t - f + 1) / 2;
            }
            return it.Invoke(new RArray(it.ToList(self)), "sum", a, b);
        });
    }
}
