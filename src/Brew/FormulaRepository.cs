using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Brew.Formulae;

namespace Brew;

public sealed class FormulaRepository(Paths paths)
{
    readonly Dictionary<string, (long Stamp, FormulaDefinition Formula)> loaded = new(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> Files => Directory.Exists(paths.Taps)
        ? Directory.EnumerateFiles(paths.Taps, "*.rb", SearchOption.AllDirectories)
            .Where(f => Path.GetRelativePath(paths.Taps, f).Split(Path.DirectorySeparatorChar).Contains("Formula"))
            .Order(StringComparer.Ordinal) : [];
    public IEnumerable<string> Names => Files.Select(Path.GetFileNameWithoutExtension).Distinct().Order(StringComparer.Ordinal);

    public string Find(string input)
    {
        if (input.EndsWith(".rb", StringComparison.OrdinalIgnoreCase) && File.Exists(input)) return Path.GetFullPath(input);
        var components = input.Split('/');
        var name = Paths.ValidateName(components[^1]);
        string directory;
        if (components.Length == 3) directory = Path.Combine(paths.Taps, TapName(components[0] + "/" + components[1]));
        else if (components.Length == 1) directory = paths.Taps;
        else throw new InvalidOperationException($"Invalid formula name: {input}");
        var core = Path.Combine(paths.Taps, "homebrew-core", "Formula", name[..1], name + ".rb");
        if (components.Length == 1 && File.Exists(core)) return core;
        var matches = Directory.Exists(directory) ? Directory.GetFiles(directory, name + ".rb", SearchOption.AllDirectories)
            .Where(f => f.Split(Path.DirectorySeparatorChar).Contains("Formula")).ToArray() : [];
        if (matches.Length == 1) return matches[0];
        if (matches.Length > 1) throw new InvalidOperationException($"Formulae found in multiple taps: {input}\nPlease use a fully-qualified name.");
        // Resolve aliases through the interpreter, rather than parsing Ruby text separately.
        foreach (var file in Files)
            if (Metadata.Strings(LoadFile(file), "Aliases").Contains(name)) return file;
        throw new InvalidOperationException($"No available formula with the name \"{input}\".");
    }

    public FormulaDefinition Load(string name) => LoadFile(Find(name));
    public FormulaDefinition LoadFile(string file)
    {
        var stamp = File.GetLastWriteTimeUtc(file).Ticks;
        if (loaded.TryGetValue(file, out var cached) && cached.Stamp == stamp) return cached.Formula;
        var type = typeof(FormulaDefinition).Assembly.GetType("Brew.Ruby.FormulaLoader")
            ?? throw new InvalidOperationException("Ruby FormulaLoader is not available; see docs/FORMULA_API.md.");
        var rich = type.GetMethod("LoadSpec", [typeof(string)]);
        var method = rich ?? type.GetMethod("Load", [typeof(string)]) ?? throw new InvalidOperationException("FormulaLoader.Load(string) is not available.");
        try
        {
            var result = method.Invoke(null, [file]);
            var formula = rich == null ? (FormulaDefinition)result : (FormulaDefinition)result.GetType().GetMethod("ToDefinition").Invoke(result, null);
            if (rich != null) Metadata.Attach(formula, result);
            loaded[file] = (stamp, formula);
            return formula;
        }
        catch (TargetInvocationException ex) { ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw(); throw; }
    }

    public static string TapName(string input)
    {
        if (!Regex.IsMatch(input, @"\A[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+\z")) throw new InvalidOperationException($"Invalid tap name: {input}");
        var parts = input.ToLowerInvariant().Split('/');
        return parts[0] + "-" + parts[1].Replace("homebrew-", "");
    }
}

// Additive compatibility while Claude builds the expanded formula metadata object.
public static class Metadata
{
    static readonly ConditionalWeakTable<FormulaDefinition, object> Specs = new();
    public static void Attach(FormulaDefinition formula, object spec) => Specs.Add(formula, spec);
    public static object Spec(object value) => value is FormulaDefinition formula && Specs.TryGetValue(formula, out var attached) ? attached : null;
    public static object Get(object value, params string[] names)
    {
        if (value == null) return null;
        if (names.Contains("Spec") && Spec(value) is object attached) return attached;
        foreach (var name in names)
        {
            var property = value.GetType().GetProperty(name);
            if (property != null) return property.GetValue(value);
            var field = value.GetType().GetField(name);
            if (field != null) return field.GetValue(value);
        }
        var spec = Spec(value) ?? value.GetType().GetProperty("Spec")?.GetValue(value) ?? value.GetType().GetField("Spec")?.GetValue(value);
        if (spec != null && !ReferenceEquals(spec, value)) return Get(spec, names);
        return null;
    }
    public static bool Bool(object value, params string[] names) => Get(value, names) is true;
    public static string Text(object value, params string[] names) => Get(value, names)?.ToString();
    public static string[] Strings(object value, params string[] names) => Get(value, names) is IEnumerable entries
        ? entries.Cast<object>().Select(v => v.ToString()).ToArray() : [];
    public static string Version(FormulaDefinition formula) => Text(formula, "PkgVersion", "PackageVersion") ?? formula.Version;
    public static string StableVersion(FormulaDefinition formula) => Text(Spec(formula), "Version") ?? formula.Version;
    public static object Call(FormulaDefinition formula, string name, params object[] arguments)
    {
        var spec = Spec(formula); if (spec == null) return null;
        var method = spec.GetType().GetMethods().FirstOrDefault(m => m.Name == name && m.GetParameters().Count(p => !p.IsOptional) <= arguments.Length && m.GetParameters().Length >= arguments.Length);
        if (method == null) return null;
        try { return method.Invoke(spec, arguments.Concat(Enumerable.Repeat(Type.Missing, method.GetParameters().Length - arguments.Length)).ToArray()); }
        catch (TargetInvocationException ex) { ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw(); throw; }
    }
}
