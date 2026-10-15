using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Brew.Ruby;

public sealed partial class Interp
{
    static readonly object[] NoArgs = Array.Empty<object>();

    void InitCore()
    {
        var K = KernelModule;
        var O = ObjectClass;

        // ---- BasicObject ----
        Def(BasicObjectClass, "initialize", (it, self, a, b) => null);
        Def(BasicObjectClass, "method_missing", (it, self, a, b) => throw it.NoMethod(self, it.StrOrSym(a[0]), false));
        Def(BasicObjectClass, "!", (it, self, a, b) => !Truthy(self));
        Def(BasicObjectClass, "== equal?", (it, self, a, b) => self is RObject ? ReferenceEquals(self, a[0]) : it.Equal(self, a[0]));
        Def(BasicObjectClass, "!=", (it, self, a, b) => !it.Equal(self, a[0]));
        Def(BasicObjectClass, "__send__", (it, self, a, b) => it.Invoke(self, it.StrOrSym(a[0]), a[1..], b));
        Def(BasicObjectClass, "__id__", (it, self, a, b) => (long)RuntimeHelpers.GetHashCode(self));
        Def(BasicObjectClass, "instance_eval instance_exec", (it, self, a, b) =>
            it.CallProc(b ?? throw it.ArgError("no block given"), a.Length > 0 ? a : new[] { self }, null, self,
                self is RObject ro ? (self is RClass rc ? it.SingletonOf(rc) : it.SingletonOf(ro)) : null));

        // ---- Kernel ----
        Def(K, "puts", (it, self, a, b) => { it.Puts(it.Out, a); return null; });
        Def(K, "print", (it, self, a, b) => { foreach (var x in a) it.Out.Write(it.ToS(x)); return null; });
        Def(K, "p pp", (it, self, a, b) =>
        {
            foreach (var x in a) it.Out.WriteLine(it.Inspect(x));
            return a.Length switch { 0 => null, 1 => a[0], _ => new RArray(a) };
        });
        Def(K, "warn", (it, self, a, b) => { it.Puts(it.Err, a.Where(x => x is not RHash).ToArray()); return null; });
        Def(K, "require require_relative load", (it, self, a, b) => true);
        Def(K, "raise fail", (it, self, a, b) => throw new RubyError(it.MakeException(a)));
        Def(K, "loop", (it, self, a, b) =>
        {
            try { while (true) it.CallProc(b, NoArgs); }
            catch (RubyError e) when (it.IsA(e.Exc, it.StopIteration)) { return e.Exc.GetIvar("@result"); }
        });
        Def(K, "lambda", (it, self, a, b) => b == null ? throw it.ArgError("tried to create Proc object without a block")
            : new RProc { Block = b.Block, Frame = b.Frame, Self = b.Self, Fn = b.Fn, Lambda = b.Block != null });
        Def(K, "proc", (it, self, a, b) => b ?? throw it.ArgError("tried to create Proc object without a block"));
        Def(K, "block_given?", (it, self, a, b) => it.Caller?.Block != null);
        Def(K, "__method__", (it, self, a, b) => it.Caller?.Method == null ? null : Sym(it.Caller.Method.Name));
        Def(K, "caller", (it, self, a, b) => new RArray());
        Def(K, "format sprintf", (it, self, a, b) => S(it.Format(it.Str(a[0]), a[1..])));
        Def(K, "Integer", (it, self, a, b) => it.ToInteger(a[0], a.Length > 1 && a[1] is long bs ? (int)bs : 10, a.Length > 1 && a[^1] is RHash h && h.D.TryGetValue(Sym("exception"), out var ex) && !Truthy(ex)));
        Def(K, "Float", (it, self, a, b) => a[0] switch
        {
            long l => (double)l,
            double d => d,
            RString s when double.TryParse(s.V.Replace("_", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
            null => throw it.TypeErr("can't convert nil into Float"),
            _ => throw it.ArgError($"invalid value for Float(): {it.Inspect(a[0])}"),
        });
        Def(K, "String", (it, self, a, b) => S(it.ToS(a[0])));
        Def(K, "Array", (it, self, a, b) => a[0] is RArray ? a[0] : new RArray(it.SplatToList(a[0])));
        Def(K, "Hash", (it, self, a, b) => a[0] as RHash ?? new RHash());
        Def(K, "catch", (it, self, a, b) =>
        {
            var tag = a.Length > 0 ? a[0] : new RObject { Class = it.ObjectClass };
            try { return it.CallProc(b, new[] { tag }); }
            catch (ThrowSignal t) when (it.Equal(t.Tag, tag)) { return t.Value; }
        });
        Def(K, "throw", (it, self, a, b) => throw new ThrowSignal { Tag = a[0], Value = a.Length > 1 ? a[1] : null });
        Def(K, "sleep", (it, self, a, b) => { Thread.Sleep((int)(it.Num(a.Length > 0 ? a[0] : 0L) * 1000)); return 0L; });
        Def(K, "rand", (it, self, a, b) => a.Length == 0 ? Random.Shared.NextDouble() : (object)Random.Shared.NextInt64(it.Int(a[0])));
        Def(K, "at_exit autoload trap", (it, self, a, b) => null);
        Def(K, "binding", (it, self, a, b) => self);
        Def(K, "abort", (it, self, a, b) =>
        {
            if (a.Length > 0) it.Err.WriteLine(it.Str(a[0]));
            throw new RubyError(it.NewException(it.SystemExit, a.Length > 0 ? it.Str(a[0]) : "exit"));
        });
        Def(K, "exit exit!", (it, self, a, b) => throw new RubyError(it.NewException(it.SystemExit, "exit")));
        Def(K, "freeze", (it, self, a, b) =>
        {
            switch (self)
            {
                case RString s: s.Frozen = true; break;
                case RArray ar: ar.Frozen = true; break;
                case RHash h: h.Frozen = true; break;
                case RObject o: o.Frozen = true; break;
            }
            return self;
        });
        Def(K, "frozen?", (it, self, a, b) => self switch
        {
            RString s => s.Frozen,
            RArray ar => ar.Frozen,
            RHash h => h.Frozen,
            RObject o => o.Frozen,
            _ => true,
        });
        Def(K, "nil?", (it, self, a, b) => self == null);
        Def(K, "is_a? kind_of?", (it, self, a, b) => it.IsA(self, a[0] as RClass ?? throw it.TypeErr("class or module required")));
        Def(K, "instance_of?", (it, self, a, b) => it.ClassOf(self) == a[0]);
        Def(K, "respond_to?", (it, self, a, b) => it.RespondTo(self, it.StrOrSym(a[0])));
        Def(K, "send public_send", (it, self, a, b) => it.Invoke(self, it.StrOrSym(a[0]), a[1..], b));
        Def(K, "class", (it, self, a, b) => it.ClassOf(self));
        Def(K, "singleton_class", (it, self, a, b) => it.SingletonOf(self as RObject ?? throw it.TypeErr("can't define singleton")));
        Def(K, "inspect", (it, self, a, b) => S(self is RObject ro && self is not RClass ? (ReferenceEquals(self, it.Main) ? "main" : it.DefaultInspect(ro)) : it.Inspect(self)));
        Def(K, "to_s", (it, self, a, b) => S(self switch
        {
            RClass c => c.Name ?? "#<Class:0x" + RuntimeHelpers.GetHashCode(c).ToString("x16") + ">",
            RObject when ReferenceEquals(self, it.Main) => "main",
            RObject o => $"#<{o.Class.Name}>",
            _ => it.ToS(self),
        }));
        Def(K, "eql?", (it, self, a, b) => Eql(self, a[0]));
        Def(K, "===", (it, self, a, b) => self is RObject ? ReferenceEquals(self, a[0]) || Truthy(it.Invoke(self, "==", a, null)) : it.Equal(self, a[0]));
        Def(K, "=~", (it, self, a, b) => null);
        Def(K, "!~", (it, self, a, b) => !Truthy(it.Invoke(self, "=~", a, null)));
        Def(K, "hash", (it, self, a, b) => (long)KeyComparer.Instance.GetHashCode(self));
        Def(K, "object_id", (it, self, a, b) => (long)RuntimeHelpers.GetHashCode(self));
        Def(K, "instance_variable_get", (it, self, a, b) => (self as RObject)?.GetIvar(it.StrOrSym(a[0])));
        Def(K, "instance_variable_set", (it, self, a, b) => { ((RObject)self).SetIvar(it.StrOrSym(a[0]), a[1]); return a[1]; });
        Def(K, "instance_variable_defined?", (it, self, a, b) => self is RObject { Ivars: not null } o && o.Ivars.ContainsKey(it.StrOrSym(a[0])));
        Def(K, "instance_variables", (it, self, a, b) => new RArray((self as RObject)?.Ivars?.Keys.Select(k => (object)Sym(k)) ?? Enumerable.Empty<object>()));
        Def(K, "tap", (it, self, a, b) => { it.Yield(b, self); return self; });
        Def(K, "then yield_self", (it, self, a, b) => it.Yield(b, self));
        Def(K, "itself", (it, self, a, b) => self);
        Def(K, "dup clone", (it, self, a, b) => it.Dup(self));
        Def(K, "extend", (it, self, a, b) =>
        {
            var sc = it.SingletonOf((RObject)self);
            foreach (var m in a.Cast<RClass>())
            {
                if (!sc.Includes.Contains(m)) sc.Includes.Add(m);
                it.InvalidateMethods();
                it.Invoke(m, "extended", new[] { self }, null);
            }
            return self;
        });
        Def(K, "define_singleton_method", (it, self, a, b) =>
        {
            var sc = it.SingletonOf((RObject)self);
            var name = it.StrOrSym(a[0]);
            sc.Methods[name] = new RMethod { Name = name, Body = a.Length > 1 ? (RProc)a[1] : b, Owner = sc };
            it.InvalidateMethods();
            return Sym(name);
        });
        Def(K, "method", (it, self, a, b) =>
        {
            var name = it.StrOrSym(a[0]);
            var m = it.FindMethod(it.DispatchClass(self), name) ?? throw it.Error(it.NameError, $"undefined method '{name}' for {it.Describe(self)}");
            return new RProc { Lambda = true, Fn = (i2, s2, a2, b2) => i2.CallMethod(m, self, a2, b2) };
        });
        Def(K, "methods public_methods", (it, self, a, b) =>
            new RArray(it.Ancestors(it.DispatchClass(self)).SelectMany(c => c.Methods.Keys).Distinct().Select(n => (object)Sym(n))));
        Def(K, "system", (it, self, a, b) => it.RunSystem(a));
        Def(K, "`", (it, self, a, b) => it.Backtick(it.Str(a[0])));

        // ---- Module / Class ----
        var M = ModuleClass;
        Def(M, "name", (it, self, a, b) => ((RClass)self).Name is { } n ? S(n) : null);
        Def(M, "to_s inspect", (it, self, a, b) => S(((RClass)self).Name ?? "#<Class:0x" + RuntimeHelpers.GetHashCode(self).ToString("x16") + ">"));
        Def(M, "===", (it, self, a, b) => it.IsA(a[0], (RClass)self));
        Def(M, "==", (it, self, a, b) => ReferenceEquals(self, a[0]));
        Def(M, "<", (it, self, a, b) => !ReferenceEquals(self, a[0]) && it.Ancestors((RClass)self).Contains((RClass)a[0]) ? true : it.Ancestors((RClass)a[0]).Contains((RClass)self) ? false : null);
        Def(M, "<=", (it, self, a, b) => it.Ancestors((RClass)self).Contains((RClass)a[0]) ? true : it.Ancestors((RClass)a[0]).Contains((RClass)self) ? false : null);
        Def(M, ">", (it, self, a, b) => it.Invoke(a[0], "<", new[] { self }, null));
        Def(M, ">=", (it, self, a, b) => it.Invoke(a[0], "<=", new[] { self }, null));
        Def(M, "ancestors", (it, self, a, b) => new RArray(it.Ancestors((RClass)self).Distinct()));
        Def(M, "include prepend", (it, self, a, b) =>
        {
            var c = (RClass)self;
            foreach (var m in a.Reverse().Cast<RClass>())
            {
                if (it.Ancestors(c).Contains(m)) continue;
                c.Includes.Add(m);
                it.InvalidateMethods();
                it.Invoke(m, "included", new object[] { c }, null);
            }
            return self;
        });
        Def(M, "include?", (it, self, a, b) => self != a[0] && a[0] is RClass { IsModule: true } m && it.Ancestors((RClass)self).Contains(m));
        Def(M, "included extended inherited method_added prepended", (it, self, a, b) => null);
        Def(M, "attr_reader attr", (it, self, a, b) => { foreach (var x in a) it.DefReader((RClass)self, it.StrOrSym(x)); return null; });
        Def(M, "attr_writer", (it, self, a, b) => { foreach (var x in a) it.DefWriter((RClass)self, it.StrOrSym(x)); return null; });
        Def(M, "attr_accessor", (it, self, a, b) =>
        {
            foreach (var x in a) { it.DefReader((RClass)self, it.StrOrSym(x)); it.DefWriter((RClass)self, it.StrOrSym(x)); }
            return null;
        });
        Def(M, "define_method", (it, self, a, b) =>
        {
            var c = (RClass)self;
            var name = it.StrOrSym(a[0]);
            var body = a.Length > 1 ? (RProc)a[1] : b ?? throw it.ArgError("tried to create Proc object without a block");
            c.Methods[name] = body.Fn != null ? new RMethod { Name = name, Fn = (i2, s2, a2, b2) => body.Fn(i2, s2, a2, b2), Owner = c } : new RMethod { Name = name, Body = body, Owner = c };
            it.InvalidateMethods();
            return Sym(name);
        });
        Def(M, "alias_method", (it, self, a, b) =>
        {
            var c = (RClass)self;
            var old = it.StrOrSym(a[1]);
            var m = it.FindMethod(c, old) ?? throw it.Error(it.NameError, $"undefined method '{old}' for class '{c.Name}'");
            var name = it.StrOrSym(a[0]);
            c.Methods[name] = new RMethod { Name = m.Name, Def = m.Def, Body = m.Body, Fn = m.Fn, Owner = m.Owner, Cref = m.Cref };
            it.InvalidateMethods();
            return Sym(name);
        });
        Def(M, "remove_method undef_method", (it, self, a, b) => { foreach (var x in a) ((RClass)self).Methods.Remove(it.StrOrSym(x)); it.InvalidateMethods(); return self; });
        Def(M, "private public protected private_constant public_constant private_class_method public_class_method", (it, self, a, b) =>
            a.Length switch { 0 => null, 1 => a[0], _ => new RArray(a) });
        Def(M, "module_function", (it, self, a, b) =>
        {
            var c = (RClass)self;
            if (a.Length == 0) c.SetIvar("__module_function", true);
            foreach (var x in a)
            {
                var n = it.StrOrSym(x);
                if (it.FindMethod(c, n) is { } m) it.SingletonOf(c).Methods[n] = m;
            }
            it.InvalidateMethods();
            return null;
        });
        Def(M, "method_defined? public_method_defined? instance_methods_include?", (it, self, a, b) => it.FindMethod((RClass)self, it.StrOrSym(a[0])) != null);
        Def(M, "private_method_defined? protected_method_defined?", (it, self, a, b) => false);
        Def(M, "instance_method", (it, self, a, b) => it.FindMethod((RClass)self, it.StrOrSym(a[0])) is { } m ? new RProc { Lambda = true, Fn = (i2, s2, a2, b2) => i2.CallMethod(m, s2, a2, b2) } : throw it.Error(it.NameError, "undefined method"));
        Def(M, "instance_methods public_instance_methods", (it, self, a, b) =>
        {
            var c = (RClass)self;
            var src = a.Length > 0 && !Truthy(a[0]) ? new[] { c } : it.Ancestors(c).TakeWhile(x => x != it.ObjectClass);
            return new RArray(src.SelectMany(x => x.Methods.Keys).Distinct().Select(n => (object)Sym(n)));
        });
        Def(M, "const_get", (it, self, a, b) =>
        {
            object cur = self;
            foreach (var part in it.StrOrSym(a[0]).Split("::", StringSplitOptions.RemoveEmptyEntries))
                cur = it.ConstGet((RClass)cur, part) ?? throw it.Error(it.NameError, $"uninitialized constant {((RClass)self).Name}::{part}");
            return cur;
        });
        Def(M, "const_set", (it, self, a, b) =>
        {
            var c = (RClass)self;
            var n = it.StrOrSym(a[0]);
            c.Consts[n] = a[1];
            if (a[1] is RClass { Name: null } anon) anon.Name = c == it.ObjectClass ? n : $"{c.Name}::{n}";
            return a[1];
        });
        Def(M, "const_defined?", (it, self, a, b) => it.ConstGet((RClass)self, it.StrOrSym(a[0])) != null);
        Def(M, "constants", (it, self, a, b) => new RArray(((RClass)self).Consts.Keys.Select(k => (object)Sym(k))));
        Def(M, "class_eval module_eval class_exec module_exec", (it, self, a, b) => it.CallProc(b, a.Length > 0 ? a : new[] { self }, null, self, (RClass)self));
        Def(M, "class_variable_get", (it, self, a, b) => ((RClass)self).GetIvar(it.StrOrSym(a[0])));
        Def(M, "class_variable_set", (it, self, a, b) => { ((RClass)self).SetIvar(it.StrOrSym(a[0]), a[1]); return a[1]; });
        Def(M, "class_variable_defined?", (it, self, a, b) => ((RClass)self).Ivars?.ContainsKey(it.StrOrSym(a[0])) == true);
        Def(M, "hash", (it, self, a, b) => (long)RuntimeHelpers.GetHashCode(self));
        Def(M, "freeze", (it, self, a, b) => self);
        DefS(ModuleClass, "new", (it, self, a, b) =>
        {
            var m = new RClass { IsModule = true, Class = it.ModuleClass };
            if (b != null) it.CallProc(b, new object[] { m }, null, m, m);
            return m;
        });

        var C = ClassClass;
        Def(C, "superclass", (it, self, a, b) =>
        {
            var s = ((RClass)self).Super;
            while (s is { IsSingleton: true }) s = s.Super;
            return s;
        });
        Def(C, "allocate", (it, self, a, b) => new RObject { Class = (RClass)self });
        Def(C, "new", (it, self, a, b) =>
        {
            var cls = (RClass)self;
            object obj = it.Allocate(cls);
            it.Invoke(obj, "initialize", a, b);
            return obj;
        });
        DefS(ClassClass, "new", (it, self, a, b) =>
        {
            var super = a.Length > 0 ? (RClass)a[0] : it.ObjectClass;
            var c = new RClass { Super = super, Class = it.ClassClass };
            it.Invoke(super, "inherited", new object[] { c }, null);
            if (b != null) it.CallProc(b, new object[] { c }, null, c, c);
            return c;
        });

        // ---- Comparable ----
        var Cm = ComparableModule;
        Def(Cm, "<", (it, self, a, b) => it.Compare(self, a[0]) < 0);
        Def(Cm, ">", (it, self, a, b) => it.Compare(self, a[0]) > 0);
        Def(Cm, "<=", (it, self, a, b) => it.Compare(self, a[0]) <= 0);
        Def(Cm, ">=", (it, self, a, b) => it.Compare(self, a[0]) >= 0);
        Def(Cm, "==", (it, self, a, b) => ReferenceEquals(self, a[0]) || (it.Invoke(self, "<=>", a, null) is long l && l == 0));
        Def(Cm, "between?", (it, self, a, b) => it.Compare(self, a[0]) >= 0 && it.Compare(self, a[1]) <= 0);
        Def(Cm, "clamp", (it, self, a, b) =>
        {
            object lo = a[0], hi = a.Length > 1 ? a[1] : null;
            if (a.Length == 1 && a[0] is RRange r) { lo = r.From; hi = r.To; }
            if (lo != null && it.Compare(self, lo) < 0) return lo;
            if (hi != null && it.Compare(self, hi) > 0) return hi;
            return self;
        });

        // ---- nil / booleans ----
        Def(NilClass, "to_s", (it, self, a, b) => S(""));
        Def(NilClass, "to_a", (it, self, a, b) => new RArray());
        Def(NilClass, "to_h", (it, self, a, b) => new RHash());
        Def(NilClass, "to_i", (it, self, a, b) => 0L);
        Def(NilClass, "to_f", (it, self, a, b) => 0.0);
        Def(NilClass, "inspect", (it, self, a, b) => S("nil"));
        Def(NilClass, "&", (it, self, a, b) => false);
        Def(NilClass, "|", (it, self, a, b) => Truthy(a[0]));
        foreach (var bc in new[] { TrueClass, FalseClass })
        {
            Def(bc, "to_s inspect", (it, self, a, b) => S((bool)self ? "true" : "false"));
            Def(bc, "&", (it, self, a, b) => (bool)self && Truthy(a[0]));
            Def(bc, "|", (it, self, a, b) => (bool)self || Truthy(a[0]));
            Def(bc, "^", (it, self, a, b) => (bool)self != Truthy(a[0]));
        }

        InitNumbers();

        // ---- Symbol ----
        var Y = SymbolClass;
        Def(Y, "to_s id2name name", (it, self, a, b) => S(((RSymbol)self).Name));
        Def(Y, "to_sym", (it, self, a, b) => self);
        Def(Y, "to_proc", (it, self, a, b) => it.ToProc(self));
        Def(Y, "inspect", (it, self, a, b) => S(it.Inspect(self)));
        Def(Y, "length size", (it, self, a, b) => (long)((RSymbol)self).Name.Length);
        Def(Y, "upcase", (it, self, a, b) => Sym(((RSymbol)self).Name.ToUpperInvariant()));
        Def(Y, "downcase", (it, self, a, b) => Sym(((RSymbol)self).Name.ToLowerInvariant()));
        Def(Y, "<=>", (it, self, a, b) => a[0] is RSymbol o ? (long)Math.Sign(string.CompareOrdinal(((RSymbol)self).Name, o.Name)) : null);
        Def(Y, "==", (it, self, a, b) => ReferenceEquals(self, a[0]));
        foreach (var n in new[] { "[]", "start_with?", "end_with?", "empty?", "capitalize", "=~", "match?", "sub", "gsub", "include?" })
        {
            var name = n;
            Def(Y, name, (it, self, a, b) =>
            {
                var r = it.Invoke(S(((RSymbol)self).Name), name, a, b);
                return name == "capitalize" && r is RString rs ? Sym(rs.V) : r;
            });
        }

        // ---- Proc ----
        Def(ProcClass, "call () yield [] ===", (it, self, a, b) => it.CallProc((RProc)self, a, b));
        Def(ProcClass, "to_proc", (it, self, a, b) => self);
        Def(ProcClass, "lambda?", (it, self, a, b) => ((RProc)self).Lambda);
        Def(ProcClass, "arity", (it, self, a, b) =>
        {
            var p = (RProc)self;
            if (p.Block == null) return -1L;
            var ps = p.Block.Params;
            long req = ps.Count(x => x.Required && !x.Keyword);
            return ps.Any(x => x.Splat || (x.Default != null && p.Lambda)) ? -(req + 1) : req;
        });
        DefS(ProcClass, "new", (it, self, a, b) => b ?? throw it.ArgError("tried to create Proc object without a block"));

        // ---- Exception ----
        var E = ExceptionClass;
        Def(E, "initialize", (it, self, a, b) => { ((RObject)self).SetIvar("@message", a.Length > 0 ? a[0] : null); return null; });
        Def(E, "message to_s", (it, self, a, b) => S(((RObject)self).GetIvar("@message") is { } m ? it.ToS(m) : ((RObject)self).Class.Name));
        Def(E, "detailed_message full_message", (it, self, a, b) =>
            S($"{it.ToS(it.Invoke(self, "message", NoArgs, null))} ({((RObject)self).Class.Name})"));
        Def(E, "inspect", (it, self, a, b) =>
        {
            var msg = it.ToS(it.Invoke(self, "to_s", NoArgs, null));
            var cn = ((RObject)self).Class.Name;
            return S(msg.Length == 0 ? cn : msg == cn ? cn : $"#<{cn}: {msg}>");
        });
        Def(E, "backtrace", (it, self, a, b) => ((RObject)self).GetIvar("@__backtrace"));
        Def(E, "set_backtrace", (it, self, a, b) => { ((RObject)self).SetIvar("@__backtrace", a[0]); return a[0]; });
        Def(E, "backtrace_locations cause", (it, self, a, b) => null);
        Def(E, "exception", (it, self, a, b) =>
        {
            if (a.Length == 0) return self;
            var copy = (RObject)it.Dup(self);
            copy.SetIvar("@message", a[0]);
            return copy;
        });
        Def(E, "==", (it, self, a, b) => a[0] is RObject o && o.Class == ((RObject)self).Class && it.Equal(((RObject)self).GetIvar("@message"), o.GetIvar("@message")));
        DefS(E, "exception", (it, self, a, b) => it.Invoke(self, "new", a, b));
        Def(SystemExit, "status", (it, self, a, b) => 0L);
        Def(SystemExit, "success?", (it, self, a, b) => true);
    }

    public object Allocate(RClass cls)
    {
        foreach (var a in Ancestors(cls))
        {
            if (a == StringClass) return new RString("");
            if (a == ArrayClass) return new RArray();
            if (a == HashClass) return new RHash();
            if (a == ObjectClass || a == ExceptionClass) break;
        }
        return new RObject { Class = cls };
    }

    void DefReader(RClass c, string name)
    {
        var iv = "@" + name;
        c.Methods[name] = new RMethod { Name = name, Owner = c, Fn = (it, self, a, b) => (self as RObject)?.GetIvar(iv) };
        InvalidateMethods();
    }

    void DefWriter(RClass c, string name)
    {
        var iv = "@" + name;
        c.Methods[name + "="] = new RMethod
        {
            Name = name + "=", Owner = c,
            Fn = (it, self, a, b) =>
            {
                var o = (RObject)self;
                if (o.Frozen) throw it.Error(it.FrozenError, $"can't modify frozen {o.Class.Name}");
                o.SetIvar(iv, a[0]);
                return a[0];
            },
        };
        InvalidateMethods();
    }

    public object Dup(object o) => o switch
    {
        RString s => new RString(s.V),
        RArray a => new RArray(a.L),
        RHash h => CopyHash(h),
        RClass => o,
        RObject r => new RObject
        {
            Class = r.Class,
            Ivars = r.Ivars == null ? null : new Dictionary<string, object>(r.Ivars),
            Host = r.Host,
        },
        _ => o,
    };

    public RHash CopyHash(RHash h)
    {
        var r = new RHash { Default = h.Default, DefaultProc = h.DefaultProc };
        foreach (var kv in h.D) r.D[kv.Key] = kv.Value;
        return r;
    }

    public RObject MakeException(object[] a)
    {
        if (a.Length == 0)
            return Globals.GetValueOrDefault("$!") as RObject ?? NewException(RuntimeError, "unhandled exception");
        if (a[0] is RString s) return NewException(RuntimeError, s.V);
        object exc;
        if (a[0] is RClass c) exc = Invoke(c, "new", a.Length > 1 && a[1] is not RHash ? new[] { a[1] } : NoArgs, null);
        else
        {
            exc = a[0];
            if (a.Length > 1) exc = Invoke(exc, "exception", new[] { a[1] }, null);
        }
        if (exc is not RObject ro || !IsA(ro, ExceptionClass)) throw TypeErr("exception class/object expected");
        return ro;
    }

    void Puts(TextWriter w, object[] a)
    {
        if (a.Length == 0) { w.Write('\n'); return; }
        foreach (var x in a)
        {
            if (x is RArray arr)
            {
                if (arr.L.Count == 0) { w.Write('\n'); continue; }
                Puts(w, arr.L.ToArray());
                continue;
            }
            var s = ToS(x);
            w.Write(s);
            if (!s.EndsWith('\n')) w.Write('\n');
        }
    }

    object ToInteger(object v, int radix, bool noException)
    {
        switch (v)
        {
            case long: return v;
            case double d:
                if (double.IsNaN(d) || double.IsInfinity(d)) throw Error(FloatDomainError(), FloatToS(d));
                return (long)Math.Truncate(d);
            case null:
                if (noException) return null;
                throw TypeErr("can't convert nil into Integer");
            case RString s:
            {
                var t = s.V.Trim().Replace("_", "");
                int sign = 1;
                if (t.StartsWith('-')) { sign = -1; t = t[1..]; }
                else if (t.StartsWith('+')) t = t[1..];
                int r = radix;
                if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) { r = 16; t = t[2..]; }
                else if (t.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) { r = 2; t = t[2..]; }
                else if (t.StartsWith("0o", StringComparison.OrdinalIgnoreCase)) { r = 8; t = t[2..]; }
                else if (t.Length > 1 && t[0] == '0' && radix == 10) { r = 8; t = t[1..]; }
                try
                {
                    if (t.Length == 0) throw new FormatException();
                    return sign * Convert.ToInt64(t, r);
                }
                catch (Exception) when (noException) { return null; }
                catch (Exception) { throw ArgError($"invalid value for Integer(): {InspectStr(s.V)}"); }
            }
        }
        if (RespondTo(v, "to_i")) return Invoke(v, "to_i", NoArgs, null);
        throw TypeErr($"can't convert {ClassOf(v).Name} into Integer");
    }

    RClass FloatDomainError() => ConstGet(ObjectClass, "FloatDomainError") as RClass ?? DefineClass("FloatDomainError", RangeError);

    // ---- numbers ----

    void InitNumbers()
    {
        var I = IntegerClass;
        var F = FloatClass;
        FloatClass.Consts["INFINITY"] = double.PositiveInfinity;
        FloatClass.Consts["NAN"] = double.NaN;
        FloatClass.Consts["EPSILON"] = double.Epsilon;
        IntegerClass.Consts["MAX"] = long.MaxValue;

        foreach (var op in new[] { "+", "-", "*", "/", "%", "**", "<", ">", "<=", ">=", "==", "<=>", "divmod", "fdiv", "modulo", "div", "pow" })
        {
            var o = op;
            Native fn = (it, self, a, b) => it.NumOp(self, o, a[0]);
            Def(I, o, fn);
            Def(F, o, fn);
        }
        Def(I, "!=", (it, self, a, b) => !it.Equal(self, a[0]));
        Def(F, "!=", (it, self, a, b) => !it.Equal(self, a[0]));
        Def(I, "-@", (it, self, a, b) => -(long)self);
        Def(F, "-@", (it, self, a, b) => -(double)self);
        Def(I, "~", (it, self, a, b) => ~(long)self);
        Def(I, "&", (it, self, a, b) => (long)self & it.Int(a[0]));
        Def(I, "|", (it, self, a, b) => (long)self | it.Int(a[0]));
        Def(I, "^", (it, self, a, b) => (long)self ^ it.Int(a[0]));
        Def(I, "<<", (it, self, a, b) => (long)self << (int)it.Int(a[0]));
        Def(I, ">>", (it, self, a, b) => (long)self >> (int)it.Int(a[0]));
        Def(I, "[]", (it, self, a, b) => ((long)self >> (int)it.Int(a[0])) & 1);
        Def(I, "abs magnitude", (it, self, a, b) => Math.Abs((long)self));
        Def(F, "abs magnitude", (it, self, a, b) => Math.Abs((double)self));
        Def(I, "succ next", (it, self, a, b) => (long)self + 1);
        Def(I, "pred", (it, self, a, b) => (long)self - 1);
        Def(I, "times", (it, self, a, b) =>
        {
            long n = (long)self;
            if (b == null) return new RArray(Enumerable.Range(0, (int)Math.Max(0, n)).Select(i => (object)(long)i));
            for (long i = 0; i < n; i++) it.CallProc(b, new object[] { i });
            return self;
        });
        Def(I, "upto", (it, self, a, b) =>
        {
            long from = (long)self, to = it.Int(a[0]);
            if (b == null) return new RArray(LongRange(from, to).Select(x => (object)x));
            for (long i = from; i <= to; i++) it.CallProc(b, new object[] { i });
            return self;
        });
        Def(I, "downto", (it, self, a, b) =>
        {
            long from = (long)self, to = it.Int(a[0]);
            if (b == null) return new RArray(LongRange(to, from).Reverse().Select(x => (object)x));
            for (long i = from; i >= to; i--) it.CallProc(b, new object[] { i });
            return self;
        });
        Def(I, "to_s inspect", (it, self, a, b) =>
        {
            long v = (long)self;
            int radix = a.Length > 0 ? (int)it.Int(a[0]) : 10;
            if (radix == 10) return S(v.ToString(CultureInfo.InvariantCulture));
            return S(ToRadix(v, radix));
        });
        Def(I, "to_i to_int floor ceil round truncate ord", (it, self, a, b) => self);
        Def(I, "to_f", (it, self, a, b) => (double)(long)self);
        Def(I, "chr", (it, self, a, b) => S(((char)(long)self).ToString()));
        Def(I, "zero?", (it, self, a, b) => (long)self == 0);
        Def(I, "nonzero?", (it, self, a, b) => (long)self == 0 ? null : self);
        Def(I, "positive?", (it, self, a, b) => (long)self > 0);
        Def(I, "negative?", (it, self, a, b) => (long)self < 0);
        Def(I, "even?", (it, self, a, b) => (long)self % 2 == 0);
        Def(I, "odd?", (it, self, a, b) => (long)self % 2 != 0);
        Def(I, "integer? finite?", (it, self, a, b) => true);
        Def(I, "infinite?", (it, self, a, b) => null);
        Def(I, "nan?", (it, self, a, b) => false);
        Def(I, "hash", (it, self, a, b) => (long)((long)self).GetHashCode());
        Def(I, "gcd", (it, self, a, b) => Gcd((long)self, it.Int(a[0])));
        Def(I, "lcm", (it, self, a, b) => { long x = (long)self, y = it.Int(a[0]); return x == 0 || y == 0 ? 0L : Math.Abs(x / Gcd(x, y) * y); });
        Def(I, "digits", (it, self, a, b) =>
        {
            var r = new RArray();
            long v = (long)self;
            do { r.L.Add(v % 10); v /= 10; } while (v > 0);
            return r;
        });
        Def(I, "size", (it, self, a, b) => 8L);
        Def(I, "bit_length", (it, self, a, b) => 64L - (long)System.Numerics.BitOperations.LeadingZeroCount((ulong)Math.Abs((long)self)));
        Def(I, "coerce", (it, self, a, b) => new RArray(new[] { a[0], self }));

        Def(F, "to_s inspect", (it, self, a, b) => S(FloatToS((double)self)));
        Def(F, "to_f", (it, self, a, b) => self);
        Def(F, "to_i to_int truncate", (it, self, a, b) =>
        {
            var d = (double)self;
            if (double.IsNaN(d) || double.IsInfinity(d)) throw it.Error(it.FloatDomainError(), FloatToS(d));
            return (long)Math.Truncate(d);
        });
        Def(F, "round", (it, self, a, b) =>
        {
            var d = (double)self;
            int digits = a.Length > 0 && a[0] is long l ? (int)l : 0;
            if (digits > 0) return Math.Round(d, Math.Min(digits, 15), MidpointRounding.AwayFromZero);
            return (long)Math.Round(d, MidpointRounding.AwayFromZero);
        });
        Def(F, "floor", (it, self, a, b) => a.Length > 0 && a[0] is long l && l > 0 ? Math.Floor((double)self * Math.Pow(10, l)) / Math.Pow(10, l) : (object)(long)Math.Floor((double)self));
        Def(F, "ceil", (it, self, a, b) => a.Length > 0 && a[0] is long l && l > 0 ? Math.Ceiling((double)self * Math.Pow(10, l)) / Math.Pow(10, l) : (object)(long)Math.Ceiling((double)self));
        Def(F, "nan?", (it, self, a, b) => double.IsNaN((double)self));
        Def(F, "finite?", (it, self, a, b) => double.IsFinite((double)self));
        Def(F, "infinite?", (it, self, a, b) => double.IsPositiveInfinity((double)self) ? 1L : double.IsNegativeInfinity((double)self) ? -1L : null);
        Def(F, "zero?", (it, self, a, b) => (double)self == 0);
        Def(F, "positive?", (it, self, a, b) => (double)self > 0);
        Def(F, "negative?", (it, self, a, b) => (double)self < 0);
        Def(F, "integer?", (it, self, a, b) => false);
        Def(F, "hash", (it, self, a, b) => (long)((double)self).GetHashCode());
        Def(F, "coerce", (it, self, a, b) => new RArray(new object[] { it.Num(a[0]), self }));
    }

    static IEnumerable<long> LongRange(long from, long to)
    {
        for (long i = from; i <= to; i++) yield return i;
    }

    static long Gcd(long a, long b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    static string ToRadix(long v, int radix)
    {
        if (v == 0) return "0";
        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        bool neg = v < 0;
        ulong u = neg ? (ulong)(-v) : (ulong)v;
        var sb = new StringBuilder();
        while (u > 0) { sb.Insert(0, digits[(int)(u % (ulong)radix)]); u /= (ulong)radix; }
        return (neg ? "-" : "") + sb;
    }

    public object NumOp(object a, string op, object b)
    {
        if (b is not (long or double))
        {
            if (op == "==") return false;
            if (op == "<=>") return null;
            if (b != null && RespondTo(b, "coerce") && b is RObject && Invoke(b, "coerce", new[] { a }, null) is RArray co && co.L.Count == 2)
                return Invoke(co.L[0], op, new[] { co.L[1] }, null);
            if (op is "<" or ">" or "<=" or ">=") throw ArgError($"comparison of {ClassOf(a).Name} with {(b == null ? "nil" : Inspect(b))} failed");
            throw TypeErr($"{(b == null ? "nil" : ClassOf(b).Name)} can't be coerced into {ClassOf(a).Name}");
        }
        if (a is long x && b is long y)
        {
            switch (op)
            {
                case "+": return x + y;
                case "-": return x - y;
                case "*": return x * y;
                case "/" or "div":
                    if (y == 0) throw Error(ZeroDivisionError, "divided by 0");
                    return FloorDiv(x, y);
                case "%" or "modulo":
                    if (y == 0) throw Error(ZeroDivisionError, "divided by 0");
                    return x - y * FloorDiv(x, y);
                case "**" or "pow": return y >= 0 ? (object)(long)Math.Pow(x, y) : Math.Pow(x, y);
                case "divmod":
                    if (y == 0) throw Error(ZeroDivisionError, "divided by 0");
                    return new RArray(new object[] { FloorDiv(x, y), x - y * FloorDiv(x, y) });
                case "fdiv": return (double)x / y;
            }
        }
        double dx = Num(a), dy = Num(b);
        return op switch
        {
            "+" => dx + dy,
            "-" => dx - dy,
            "*" => dx * dy,
            "/" => dx / dy,
            "div" => (long)Math.Floor(dx / dy),
            "%" or "modulo" => dx - dy * Math.Floor(dx / dy),
            "**" or "pow" => Math.Pow(dx, dy),
            "fdiv" => dx / dy,
            "divmod" => new RArray(new object[] { Math.Floor(dx / dy), dx - dy * Math.Floor(dx / dy) }),
            "<" => dx < dy,
            ">" => dx > dy,
            "<=" => dx <= dy,
            ">=" => dx >= dy,
            "==" => dx == dy,
            "<=>" => double.IsNaN(dx) || double.IsNaN(dy) ? null : (long)dx.CompareTo(dy),
            _ => throw NoMethod(a, op, false),
        };
    }

    static long FloorDiv(long x, long y)
    {
        long q = x / y;
        if ((x % y != 0) && ((x < 0) != (y < 0))) q--;
        return q;
    }

    // ---- format ----

    public string Format(string fmt, object[] args)
    {
        var sb = new StringBuilder();
        int ai = 0;
        RHash named = args.Length > 0 ? args[^1] as RHash : null;
        for (int i = 0; i < fmt.Length; i++)
        {
            char c = fmt[i];
            if (c != '%') { sb.Append(c); continue; }
            i++;
            if (i >= fmt.Length) { sb.Append('%'); break; }
            if (fmt[i] == '%') { sb.Append('%'); continue; }
            string flags = "";
            while (i < fmt.Length && "-+ 0#".Contains(fmt[i])) flags += fmt[i++];
            object arg = null;
            bool haveArg = false;
            if (i < fmt.Length && (fmt[i] == '<' || fmt[i] == '{'))
            {
                char close = fmt[i] == '<' ? '>' : '}';
                int e = fmt.IndexOf(close, i);
                var key = fmt[(i + 1)..e];
                if (named == null || !named.D.TryGetValue(Sym(key), out arg)) throw Error(KeyError, $"key<{key}> not found");
                haveArg = true;
                bool brace = fmt[i] == '{';
                i = e + 1;
                if (brace) { sb.Append(ToS(arg)); i--; continue; }
                while (i < fmt.Length && "-+ 0#".Contains(fmt[i])) flags += fmt[i++];
            }
            int width = -1, prec = -1;
            int ws = i;
            if (i < fmt.Length && fmt[i] == '*') { width = (int)Int(args[ai++]); i++; }
            else { while (i < fmt.Length && char.IsDigit(fmt[i])) i++; if (i > ws) width = int.Parse(fmt[ws..i]); }
            if (i < fmt.Length && fmt[i] == '.')
            {
                i++;
                int ps = i;
                while (i < fmt.Length && char.IsDigit(fmt[i])) i++;
                prec = i > ps ? int.Parse(fmt[ps..i]) : 0;
            }
            if (i >= fmt.Length) throw ArgError("malformed format string - %" + fmt[^1]);
            char conv = fmt[i];
            if (!haveArg)
            {
                if (ai >= args.Length) throw ArgError("too few arguments");
                arg = args[ai++];
            }
            string body;
            switch (conv)
            {
                case 's': body = ToS(arg); if (prec >= 0 && body.Length > prec) body = body[..prec]; break;
                case 'p': body = Inspect(arg); break;
                case 'd': case 'i': case 'u':
                {
                    long v = arg is double dd ? (long)Math.Floor(dd) : Int(ToInteger(arg, 10, false));
                    body = Math.Abs(v).ToString(CultureInfo.InvariantCulture);
                    if (prec >= 0) body = body.PadLeft(prec, '0');
                    body = (v < 0 ? "-" : flags.Contains('+') ? "+" : flags.Contains(' ') ? " " : "") + body;
                    break;
                }
                case 'f':
                {
                    double v = Num(arg);
                    body = Math.Abs(v).ToString("F" + (prec < 0 ? 6 : prec), CultureInfo.InvariantCulture);
                    body = (v < 0 || (v == 0 && double.IsNegative(v)) ? "-" : flags.Contains('+') ? "+" : flags.Contains(' ') ? " " : "") + body;
                    break;
                }
                case 'e': case 'E':
                {
                    double v = Num(arg);
                    body = v.ToString((conv == 'e' ? "e" : "E") + (prec < 0 ? 6 : prec), CultureInfo.InvariantCulture);
                    body = System.Text.RegularExpressions.Regex.Replace(body, @"([eE][+-])0(\d\d)", "$1$2");
                    break;
                }
                case 'g': case 'G':
                    body = Num(arg).ToString("G" + (prec < 0 ? 6 : prec), CultureInfo.InvariantCulture);
                    break;
                case 'x': case 'X': case 'o': case 'b': case 'B':
                {
                    long v = Int(arg);
                    int radix = conv switch { 'x' or 'X' => 16, 'o' => 8, _ => 2 };
                    body = ToRadix(Math.Abs(v), radix);
                    if (conv == 'X') body = body.ToUpperInvariant();
                    if (prec >= 0) body = body.PadLeft(prec, '0');
                    if (flags.Contains('#')) body = (conv switch { 'x' => "0x", 'X' => "0X", 'o' => "0", _ => "0b" }) + body;
                    if (v < 0) body = "-" + body;
                    else if (flags.Contains('+')) body = "+" + body;
                    break;
                }
                case 'c': body = arg is long cl ? ((char)cl).ToString() : ToS(arg)[..1]; break;
                default: throw ArgError($"malformed format string - %{conv}");
            }
            if (width > body.Length)
            {
                if (flags.Contains('-')) body = body.PadRight(width);
                else if (flags.Contains('0') && conv is not ('s' or 'p' or 'c'))
                {
                    int signLen = body.Length > 0 && body[0] is '-' or '+' or ' ' ? 1 : 0;
                    body = body[..signLen] + body[signLen..].PadLeft(width - signLen, '0');
                }
                else body = body.PadLeft(width);
            }
            sb.Append(body);
        }
        return sb.ToString();
    }

    // ---- processes ----

    public void ApplyEnv(System.Diagnostics.ProcessStartInfo psi)
    {
        psi.Environment.Clear();
        foreach (var kv in Env) psi.Environment[kv.Key] = kv.Value;
    }

    public object RunSystem(object[] a)
    {
        var parts = a.Where(x => x is not RHash).Select(ToS).ToList();
        if (parts.Count == 0) throw ArgCount(a, 1, -1);
        System.Diagnostics.ProcessStartInfo psi;
        if (parts.Count == 1)
            psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/d /s /c \"" + parts[0] + "\"");
        else
        {
            psi = new System.Diagnostics.ProcessStartInfo(parts[0]);
            foreach (var p in parts.Skip(1)) psi.ArgumentList.Add(p);
        }
        psi.UseShellExecute = false;
        psi.WorkingDirectory = Cwd;
        ApplyEnv(psi);
        try
        {
            using var proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit();
            Globals["$?"] = (long)proc.ExitCode;
            return proc.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Globals["$?"] = 127L;
            return null;
        }
    }
}
