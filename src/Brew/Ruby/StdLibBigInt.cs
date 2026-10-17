using System.Globalization;
using System.Numerics;

namespace Brew.Ruby;

// Arbitrary-precision Integer, like Ruby: a value that fits in a long stays a long (the fast path);
// anything bigger is a System.Numerics.BigInteger. Every result goes through Norm, so a given value
// has exactly one representation and long/BigInteger never need comparing with each other for equality.
public sealed class BigIntNode : Node { public BigInteger Value; }

public sealed partial class Interp
{
    public static object Norm(BigInteger v) => v >= long.MinValue && v <= long.MaxValue ? (object)(long)v : v;

    public static bool IsInteger(object o) => o is long or BigInteger;

    public BigInteger Big(object o) => o switch
    {
        long l => l,
        BigInteger b => b,
        double d => DoubleToBig(d),
        _ => throw TypeErr($"no implicit conversion of {TypeName(o)} into Integer"),
    };

    BigInteger DoubleToBig(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) throw Error(FloatDomainError(), FloatToS(d));
        return new BigInteger(Math.Truncate(d));
    }

    // A Float's integer part as an Integer (Float#to_i, Integer(1e20)), promoting past long's range.
    public object FloatToInt(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) throw Error(FloatDomainError(), FloatToS(d));
        d = Math.Truncate(d);
        return d >= -9.2233720368547758e18 && d < 9.2233720368547758e18 ? (long)d : Norm(new BigInteger(d));
    }

    public static object AddInt(long x, long y)
    {
        long r = unchecked(x + y);
        return ((x ^ r) & (y ^ r)) < 0 ? (object)((BigInteger)x + y) : r;
    }

    public static object SubInt(long x, long y)
    {
        long r = unchecked(x - y);
        return ((x ^ y) & (x ^ r)) < 0 ? (object)((BigInteger)x - y) : r;
    }

    public static object MulInt(long x, long y)
    {
        long hi = Math.BigMul(x, y, out long lo);
        return hi != (lo >> 63) ? (object)((BigInteger)x * y) : lo;
    }

    object PowInt(BigInteger x, BigInteger y)
    {
        if (y.Sign < 0) return Math.Pow((double)x, (double)y);
        if (x.IsZero || x.IsOne) return Norm(y.IsZero ? BigInteger.One : x);
        if (x == BigInteger.MinusOne) return y.IsEven ? 1L : -1L;
        // Ruby warns "in a**b, b may be too big" and answers Infinity; a result past ~32M bits is that here too.
        if (y > 32 * 1024 * 1024 || (double)y * BigInteger.Log(BigInteger.Abs(x), 2) > 32 * 1024 * 1024)
            return x.Sign < 0 && !y.IsEven ? double.NegativeInfinity : double.PositiveInfinity;
        return Norm(BigInteger.Pow(x, (int)y));
    }

    static BigInteger FloorDivBig(BigInteger x, BigInteger y)
    {
        var q = BigInteger.DivRem(x, y, out var r);
        if (!r.IsZero && (r.Sign < 0) != (y.Sign < 0)) q -= 1;
        return q;
    }

    // Integer op Integer where either side may be a Bignum, or long op long the plain path can't do exactly.
    // Returns false when the operands aren't both numeric, for NumOp's coercion and error handling.
    bool IntOp(object a, string op, object b, out object r)
    {
        r = null;
        if (a is double || b is double)
        {
            if (!(a is double or long or BigInteger) || !(b is double or long or BigInteger)) return false;
            if (a is not BigInteger && b is not BigInteger) return false;
            if (op is "==" or "<=>" or "<" or ">" or "<=" or ">=" && (a is BigInteger || b is BigInteger))
            {
                // Compare exactly: a huge Integer against a Float compares by value, not after rounding to double.
                double d = a is double da ? da : (double)b;
                if (double.IsNaN(d)) { r = op == "<=>" ? null : false; return true; }
                int c;
                if (double.IsInfinity(d)) c = a is double ? (d > 0 ? 1 : -1) : (d > 0 ? -1 : 1);
                else
                {
                    var bigSide = a is double ? Big(b) : Big(a);
                    var dBig = new BigInteger(Math.Truncate(d));
                    var frac = d - Math.Truncate(d);
                    int cmp = bigSide.CompareTo(dBig);
                    if (cmp == 0 && frac != 0) cmp = frac > 0 ? -1 : 1;
                    c = a is double ? -cmp : cmp;
                }
                r = CmpResult(op, c);
                return true;
            }
            r = NumOpDouble(Num(a), op, Num(b));
            return true;
        }
        if (!IsInteger(a) || !IsInteger(b)) return false;
        if (a is long lx && b is long ly)
        {
            switch (op)
            {
                case "+": r = AddInt(lx, ly); return true;
                case "-": r = SubInt(lx, ly); return true;
                case "*": r = MulInt(lx, ly); return true;
                case "**" or "pow": r = PowInt(lx, ly); return true;
                case "<": r = lx < ly; return true;
                case ">": r = lx > ly; return true;
                case "<=": r = lx <= ly; return true;
                case ">=": r = lx >= ly; return true;
                case "==": r = lx == ly; return true;
                case "<=>": r = (long)lx.CompareTo(ly); return true;
                case "/" or "div" or "%" or "modulo" or "divmod" when ly == -1 && lx == long.MinValue: break;   // overflows long
                default: return false;
            }
        }
        BigInteger x = Big(a), y = Big(b);
        switch (op)
        {
            case "+": r = Norm(x + y); return true;
            case "-": r = Norm(x - y); return true;
            case "*": r = Norm(x * y); return true;
            case "**" or "pow": r = PowInt(x, y); return true;
            case "/" or "div":
                if (y.IsZero) throw Error(ZeroDivisionError, "divided by 0");
                r = Norm(FloorDivBig(x, y));
                return true;
            case "%" or "modulo":
                if (y.IsZero) throw Error(ZeroDivisionError, "divided by 0");
                r = Norm(x - y * FloorDivBig(x, y));
                return true;
            case "divmod":
                if (y.IsZero) throw Error(ZeroDivisionError, "divided by 0");
                var q = FloorDivBig(x, y);
                r = new RArray(new object[] { Norm(q), Norm(x - y * q) });
                return true;
            case "fdiv": r = (double)x / (double)y; return true;
            case "==": r = x == y; return true;
            case "<=>" or "<" or ">" or "<=" or ">=": r = CmpResult(op, x.CompareTo(y)); return true;
        }
        return false;
    }

    static object CmpResult(string op, int c) => op switch
    {
        "<" => c < 0,
        ">" => c > 0,
        "<=" => c <= 0,
        ">=" => c >= 0,
        "==" => c == 0,
        _ => (object)(long)Math.Sign(c),
    };

    object NumOpDouble(double dx, string op, double dy) => op switch
    {
        "+" => dx + dy,
        "-" => dx - dy,
        "*" => dx * dy,
        "/" => dx / dy,
        "div" => FloatToInt(Math.Floor(dx / dy)),
        "%" or "modulo" => dx - dy * Math.Floor(dx / dy),
        "**" or "pow" => Math.Pow(dx, dy),
        "fdiv" => dx / dy,
        "divmod" => new RArray(new object[] { FloatToInt(Math.Floor(dx / dy)), dx - dy * Math.Floor(dx / dy) }),
        _ => throw NoMethod(dx, op, false),
    };

    // Digits (no sign, no prefix, underscores already removed) in radix 2..36; null if any digit is invalid.
    public static BigInteger? ParseDigits(string digits, int radix)
    {
        if (digits.Length == 0) return null;
        if (radix == 10 && digits.Length < 19 && long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var small)) return small;
        BigInteger v = BigInteger.Zero;
        foreach (var ch0 in digits)
        {
            var ch = char.ToLowerInvariant(ch0);
            int d = ch is >= '0' and <= '9' ? ch - '0' : ch is >= 'a' and <= 'z' ? ch - 'a' + 10 : 99;
            if (d >= radix) return null;
            v = v * radix + d;
        }
        return v;
    }

    static string BigToRadix(BigInteger v, int radix)
    {
        if (radix == 10) return v.ToString(CultureInfo.InvariantCulture);
        if (v.IsZero) return "0";
        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        bool neg = v.Sign < 0;
        var u = BigInteger.Abs(v);
        var sb = new System.Text.StringBuilder();
        while (!u.IsZero) { u = BigInteger.DivRem(u, radix, out var rem); sb.Insert(0, digits[(int)rem]); }
        return (neg ? "-" : "") + sb;
    }

    void InitBigInt()
    {
        var I = IntegerClass;
        var F = FloatClass;
        // Natives that cast self to long: a Bignum self takes the BigInteger path, a long the original one.
        void Wrap(RClass c, string names, Func<Interp, BigInteger, object[], RProc, object> big)
        {
            foreach (var n in names.Split(' '))
            {
                var old = c.Methods.TryGetValue(n, out var m) ? m.Fn : null;
                Def(c, n, (it, self, a, b) => self is BigInteger bi ? big(it, bi, a, b) : old != null ? old(it, self, a, b) : throw it.NoMethod(self, n, false));
            }
        }
        Wrap(I, "-@", (it, x, a, b) => Norm(-x));
        Wrap(I, "~", (it, x, a, b) => Norm(-x - 1));
        Wrap(I, "abs magnitude", (it, x, a, b) => Norm(BigInteger.Abs(x)));
        Wrap(I, "succ next", (it, x, a, b) => Norm(x + 1));
        Wrap(I, "pred", (it, x, a, b) => Norm(x - 1));
        Wrap(I, "to_s inspect", (it, x, a, b) => S(BigToRadix(x, a.Length > 0 ? (int)it.Int(a[0]) : 10)));
        Wrap(I, "to_f", (it, x, a, b) => (double)x);
        Wrap(I, "zero?", (it, x, a, b) => false);
        Wrap(I, "nonzero?", (it, x, a, b) => x);
        Wrap(I, "positive?", (it, x, a, b) => x.Sign > 0);
        Wrap(I, "negative?", (it, x, a, b) => x.Sign < 0);
        Wrap(I, "even?", (it, x, a, b) => x.IsEven);
        Wrap(I, "odd?", (it, x, a, b) => !x.IsEven);
        Wrap(I, "hash", (it, x, a, b) => (long)x.GetHashCode());
        Wrap(I, "size", (it, x, a, b) => (long)((x.GetByteCount() + 7) / 8 * 8));
        Wrap(I, "bit_length", (it, x, a, b) => (long)(x.Sign < 0 ? (-x - 1).GetBitLength() : x.GetBitLength()));
        Wrap(I, "digits", (it, x, a, b) =>
        {
            if (x.Sign < 0) throw it.Error(it.MathDomainError(), "out of domain");
            long radix = a.Length > 0 ? it.Int(a[0]) : 10;
            var r = new RArray();
            while (!x.IsZero) { x = BigInteger.DivRem(x, radix, out var rem); r.L.Add((long)rem); }
            return r;
        });
        Wrap(I, "gcd", (it, x, a, b) => Norm(BigInteger.GreatestCommonDivisor(x, it.Big(a[0]))));
        Wrap(I, "lcm", (it, x, a, b) => { var y = it.Big(a[0]); return y.IsZero ? 0L : Norm(BigInteger.Abs(x / BigInteger.GreatestCommonDivisor(x, y) * y)); });
        Wrap(I, "coerce", (it, x, a, b) => a[0] is double ? new RArray(new object[] { a[0], (double)x }) : new RArray(new[] { a[0], (object)x }));

        // Bit ops and shifts on either representation (a long << 64 promotes).
        Def(I, "&", (it, self, a, b) => self is long x && a[0] is long y ? x & y : Norm(it.Big(self) & it.Big(it.IntArg(a[0]))));
        Def(I, "|", (it, self, a, b) => self is long x && a[0] is long y ? x | y : Norm(it.Big(self) | it.Big(it.IntArg(a[0]))));
        Def(I, "^", (it, self, a, b) => self is long x && a[0] is long y ? x ^ y : Norm(it.Big(self) ^ it.Big(it.IntArg(a[0]))));
        Def(I, "<<", (it, self, a, b) => it.Shift(self, it.IntArg(a[0]), true));
        Def(I, ">>", (it, self, a, b) => it.Shift(self, it.IntArg(a[0]), false));
        Def(I, "[]", (it, self, a, b) =>
        {
            var n = it.IntArg(a[0]);
            if (n is BigInteger nb) return nb.Sign < 0 ? 0L : (it.Big(self).Sign < 0 ? 1L : 0L);
            long i = (long)n;
            if (i < 0) return 0L;
            if (self is long x) return i >= 64 ? (x < 0 ? 1L : 0L) : (x >> (int)i) & 1;
            return (long)(int)((((BigInteger)self) >> (int)Math.Min(i, int.MaxValue)) & 1);
        });
        Def(I, "pow", (it, self, a, b) =>
        {
            if (a.Length < 2) return it.NumOp(self, "**", a[0]);
            BigInteger m = it.Big(it.IntArg(a[1]));
            if (m.IsZero) throw it.Error(it.ZeroDivisionError, "divided by 0");
            var e = it.Big(it.IntArg(a[0]));
            if (e.Sign < 0) throw it.Error(it.RangeError, "Integer#pow() 2nd argument not allowed to be negative when 3rd argument specified");
            var r = BigInteger.ModPow(it.Big(self), e, BigInteger.Abs(m));
            if (!r.IsZero && (r.Sign < 0) != (m.Sign < 0)) r += m;
            return Norm(r);
        });

        // Float -> Integer conversions past long's range promote instead of saturating.
        Wrap2(F, "to_i to_int truncate", (it, d, a) => a.Length == 0 ? it.FloatToInt(d) : null);
        Wrap2(F, "round", (it, d, a) => a.Length == 0 || (a[0] is long l && l == 0) ? it.FloatToInt(Math.Round(d, MidpointRounding.AwayFromZero)) : null);
        Wrap2(F, "floor", (it, d, a) => a.Length == 0 ? it.FloatToInt(Math.Floor(d)) : null);
        Wrap2(F, "ceil", (it, d, a) => a.Length == 0 ? it.FloatToInt(Math.Ceiling(d)) : null);
        void Wrap2(RClass c, string names, Func<Interp, double, object[], object> big)
        {
            foreach (var n in names.Split(' '))
            {
                var old = c.Methods[n].Fn;
                Def(c, n, (it, self, a, b) => Math.Abs((double)self) >= 9.2e18 && big(it, (double)self, a) is { } r ? r : old(it, self, a, b));
            }
        }
    }

    object IntArg(object o) => o is long or BigInteger ? o : o is double d ? FloatToInt(d) : throw TypeErr($"no implicit conversion of {TypeName(o)} into Integer");

    RClass MathDomainError()
    {
        var math = ConstGet(ObjectClass, "Math") as RClass ?? DefineModule("Math");
        return math.Consts.GetValueOrDefault("DomainError") as RClass ?? DefineClass("DomainError", ArgumentError, math);
    }

    object Shift(object self, object n, bool left)
    {
        if (n is BigInteger nb)
        {
            if (left == (nb.Sign > 0)) throw Error(RangeError, "shift width too big");
            return Big(self).Sign < 0 ? -1L : 0L;
        }
        long s = (long)n;
        if (!left) s = -s;
        if (s == 0) return self;
        if (s < 0)
        {
            if (self is long x) return s <= -64 ? (x < 0 ? -1L : 0L) : x >> (int)-s;
            return Norm(Big(self) >> (int)Math.Min(-s, int.MaxValue));
        }
        if (s > int.MaxValue) throw Error(RangeError, "shift width too big");
        if (self is long l && s < 63 && (l >= 0 ? l >> (int)(63 - s) == 0 : (l >> (int)(63 - s)) == -1)) return l << (int)s;
        return Norm(Big(self) << (int)s);
    }
}
