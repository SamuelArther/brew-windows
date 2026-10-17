namespace Brew.Ruby;

// Ruby standard library pieces formulae and their tests use (Set, Enumerator, URI, Date/Time,
// YAML, sockets), each in its own StdLib*.cs. `require` is a no-op, so all of it is always loaded.
public sealed partial class Interp
{
    void InitStdLib()
    {
        InitEnumerator();
        InitSet();
        InitTime();
        InitUri();
        InitYaml();
        InitSocket();
    }

    // Keyword argument `name` from a trailing options hash, or null.
    public static object Kw(object[] a, string name) =>
        a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Sym(name), out var v) ? v : null;

    public static object[] Positional(object[] a) => a.Length > 0 && a[^1] is RHash ? a[..^1] : a;

    // ---- Set ----

    public RClass SetClass;

    void InitSet()
    {
        var St = SetClass = DefineClass("Set");
        St.Includes.Add(EnumerableModule);
        RHash Me(object o) => (RHash)((RObject)o).Host;
        RHash Mut(Interp it, object o)
        {
            var r = (RObject)o;
            if (r.Frozen) throw it.Error(it.FrozenError, $"can't modify frozen Set: {it.Inspect(o)}");
            return (RHash)r.Host;
        }
        RObject New(Interp it, RClass cls, IEnumerable<object> items)
        {
            var h = new RHash();
            foreach (var x in items) h.D[FrozenKey(x)] = true;
            return new RObject { Class = cls, Host = h };
        }
        RClass Cls(object self) => ((RObject)self).Class;
        IEnumerable<object> Items(Interp it, object o) => o is RObject { Host: RHash h } r && it.IsA(r, it.SetClass) ? h.D.Keys : it.ToList(o);

        DefS(St, "new", (it, self, a, b) =>
        {
            var items = a.Length > 0 && a[0] != null ? it.ToList(a[0]) : new List<object>();
            if (b != null) items = items.Select(x => it.CallProc(b, new[] { x })).ToList();
            var s = New(it, (RClass)self, items);
            return s;
        });
        DefS(St, "[]", (it, self, a, b) => New(it, (RClass)self, a));
        Def(St, "initialize_copy", (it, self, a, b) => self);
        Def(St, "<< add", (it, self, a, b) => { Mut(it, self).D[FrozenKey(a[0])] = true; return self; });
        Def(St, "add?", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            if (h.D.ContainsKey(a[0])) return null;
            h.D[FrozenKey(a[0])] = true;
            return self;
        });
        Def(St, "delete", (it, self, a, b) => { Mut(it, self).D.Remove(a[0]); return self; });
        Def(St, "delete?", (it, self, a, b) => Mut(it, self).D.Remove(a[0]) ? self : null);
        Def(St, "merge", (it, self, a, b) => { var h = Mut(it, self); foreach (var o in a) foreach (var x in Items(it, o)) h.D[FrozenKey(x)] = true; return self; });
        Def(St, "subtract", (it, self, a, b) => { var h = Mut(it, self); foreach (var x in Items(it, a[0]).ToList()) h.D.Remove(x); return self; });
        Def(St, "clear", (it, self, a, b) => { Mut(it, self).D.Clear(); return self; });
        Def(St, "include? member? === contain? includes?", (it, self, a, b) => Me(self).D.ContainsKey(a[0]));
        Def(St, "size length", (it, self, a, b) => (long)Me(self).D.Count);
        Def(St, "count", (it, self, a, b) => a.Length == 0 && b == null ? (long)Me(self).D.Count : (long)Me(self).D.Keys.Count(it.Pred(a, b)));
        Def(St, "empty?", (it, self, a, b) => Me(self).D.Count == 0);
        Def(St, "any?", (it, self, a, b) => a.Length == 0 && b == null ? Me(self).D.Count > 0 : Me(self).D.Keys.Any(it.Pred(a, b)));
        Def(St, "each", (it, self, a, b) =>
        {
            if (b == null) return it.MakeEnum(self, "each", NoArgs, (long)Me(self).D.Count);
            foreach (var x in Me(self).D.Keys.ToList()) it.CallProc(b, new[] { x });
            return self;
        });
        Def(St, "to_a", (it, self, a, b) => new RArray(Me(self).D.Keys));
        Def(St, "to_set", (it, self, a, b) => b == null ? self : New(it, it.SetClass, Me(self).D.Keys.Select(x => it.CallProc(b, new[] { x }))));
        Def(St, "dup clone", (it, self, a, b) => New(it, Cls(self), Me(self).D.Keys));
        Def(St, "freeze", (it, self, a, b) => { ((RObject)self).Frozen = true; return self; });
        Def(St, "| union +", (it, self, a, b) => New(it, Cls(self), Me(self).D.Keys.Concat(Items(it, a[0]))));
        Def(St, "& intersection", (it, self, a, b) =>
        {
            var other = new HashSet<object>(Items(it, a[0]), KeyComparer.Instance);
            return New(it, Cls(self), Me(self).D.Keys.Where(other.Contains));
        });
        Def(St, "- difference", (it, self, a, b) =>
        {
            var other = new HashSet<object>(Items(it, a[0]), KeyComparer.Instance);
            return New(it, Cls(self), Me(self).D.Keys.Where(x => !other.Contains(x)));
        });
        Def(St, "^", (it, self, a, b) =>
        {
            var mine = Me(self).D;
            var other = Items(it, a[0]).ToList();
            var otherSet = new HashSet<object>(other, KeyComparer.Instance);
            return New(it, Cls(self), other.Where(x => !mine.ContainsKey(x)).Concat(mine.Keys.Where(x => !otherSet.Contains(x))));
        });
        bool SubsetOf(Interp it, object x, object y)
        {
            var other = it.SetArg(y);
            return Me(x).D.Keys.All(other.D.ContainsKey);
        }
        Def(St, "subset? <=", (it, self, a, b) => SubsetOf(it, self, a[0]));
        Def(St, "superset? >=", (it, self, a, b) => SubsetOf(it, a[0], self));
        Def(St, "proper_subset? <", (it, self, a, b) => SubsetOf(it, self, a[0]) && Me(self).D.Count < it.SetArg(a[0]).D.Count);
        Def(St, "proper_superset? >", (it, self, a, b) => SubsetOf(it, a[0], self) && Me(self).D.Count > it.SetArg(a[0]).D.Count);
        Def(St, "<=>", (it, self, a, b) =>
        {
            if (a[0] is not RObject o || !it.IsA(o, it.SetClass)) return null;
            bool sub = SubsetOf(it, self, o), sup = SubsetOf(it, o, self);
            return sub && sup ? 0L : sub ? -1L : sup ? 1L : null;
        });
        Def(St, "disjoint?", (it, self, a, b) => !Items(it, a[0]).Any(Me(self).D.ContainsKey));
        Def(St, "intersect?", (it, self, a, b) => Items(it, a[0]).Any(Me(self).D.ContainsKey));
        Def(St, "== eql?", (it, self, a, b) =>
            ReferenceEquals(self, a[0]) || a[0] is RObject { Host: RHash oh } o && it.IsA(o, it.SetClass) && oh.D.Count == Me(self).D.Count && oh.D.Keys.All(Me(self).D.ContainsKey));
        Def(St, "hash", (it, self, a, b) => (long)Me(self).D.Keys.Aggregate(0, (h, x) => h ^ KeyComparer.Instance.GetHashCode(x)));
        Def(St, "inspect to_s", (it, self, a, b) => S("#<" + Cls(self).Name + ": {" + string.Join(", ", Me(self).D.Keys.Select(it.Inspect)) + "}>"));
        Def(St, "map! collect!", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            var items = h.D.Keys.Select(x => it.CallProc(b, new[] { x })).ToList();
            h.D.Clear();
            foreach (var x in items) h.D[FrozenKey(x)] = true;
            return self;
        });
        Def(St, "select! filter! keep_if", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            var drop = h.D.Keys.Where(x => !Truthy(it.CallProc(b, new[] { x }))).ToList();
            foreach (var x in drop) h.D.Remove(x);
            return n(drop.Count, self);
        });
        Def(St, "reject! delete_if", (it, self, a, b) =>
        {
            var h = Mut(it, self);
            var drop = h.D.Keys.Where(x => Truthy(it.CallProc(b, new[] { x }))).ToList();
            foreach (var x in drop) h.D.Remove(x);
            return n(drop.Count, self);
        });
        static object n(int removed, object self) => removed == 0 ? null : self;
        Def(St, "select filter", (it, self, a, b) => b == null ? it.MakeEnum(self, "select", NoArgs, null) : New(it, Cls(self), Me(self).D.Keys.Where(x => Truthy(it.CallProc(b, new[] { x })))));
        Def(St, "reject", (it, self, a, b) => b == null ? it.MakeEnum(self, "reject", NoArgs, null) : New(it, Cls(self), Me(self).D.Keys.Where(x => !Truthy(it.CallProc(b, new[] { x })))));
        Def(St, "min", (it, self, a, b) => it.MinMax(Me(self).D.Keys.ToList(), b, -1, a));
        Def(St, "max", (it, self, a, b) => it.MinMax(Me(self).D.Keys.ToList(), b, 1, a));
        Def(St, "first", (it, self, a, b) => a.Length == 0 ? Me(self).D.Keys.FirstOrDefault() : new RArray(Me(self).D.Keys.Take((int)it.Int(a[0]))));
        Def(St, "group_by", (it, self, a, b) =>
        {
            var r = new RHash();
            foreach (var x in Me(self).D.Keys)
            {
                var k = it.CallProc(b, new[] { x });
                if (!r.D.TryGetValue(k, out var g)) r.D[k] = g = New(it, Cls(self), Array.Empty<object>());
                Me(g).D[x] = true;
            }
            return r;
        });
        Def(St, "partition", (it, self, a, b) =>
        {
            var yes = New(it, Cls(self), Array.Empty<object>());
            var no = New(it, Cls(self), Array.Empty<object>());
            foreach (var x in Me(self).D.Keys) Me(Truthy(it.CallProc(b, new[] { x })) ? yes : no).D[x] = true;
            return new RArray(new object[] { yes, no });
        });
        Def(St, "sort", (it, self, a, b) => new RArray(it.SortList(Me(self).D.Keys.ToList(), b)));
        Def(St, "join", (it, self, a, b) => it.Invoke(new RArray(Me(self).D.Keys), "join", a, null));
        Def(St, "classify", (it, self, a, b) =>
        {
            var r = new RHash();
            foreach (var x in Me(self).D.Keys)
            {
                var k = it.CallProc(b, new[] { x });
                if (!r.D.TryGetValue(k, out var g)) r.D[k] = g = New(it, Cls(self), Array.Empty<object>());
                Me(g).D[x] = true;
            }
            return r;
        });
        Def(St, "to_json", (it, self, a, b) => it.Invoke(new RArray(Me(self).D.Keys), "to_json", a, null));
        // Array#to_set and Enumerable#to_set with a block.
        Def(EnumerableModule, "to_set", (it, self, a, b) =>
            New(it, it.SetClass, b == null ? it.ToList(self) : it.ToList(self).Select(x => it.CallProc(b, new[] { x }))));
    }

    // A String key is stored frozen (like Hash#[]=), so mutating the original doesn't change the set.
    static object FrozenKey(object x) => x is RString { Frozen: false } s ? new RString(s.V) { Frozen = true, Binary = s.Binary } : x;

    RHash SetArg(object o) => o is RObject { Host: RHash h } r && IsA(r, SetClass) ? h : throw ArgError("value must be a set");
}
