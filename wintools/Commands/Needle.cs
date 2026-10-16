using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// needle (Uber's Swift DI code generator): parses Swift sources for Component<Dependency> /
// BootstrapComponent subclasses, their Dependency protocols, the properties each component
// exposes and the `Child(parent: self)` instantiations that form the tree, then writes the
// dependency providers and registerProviderFactories() that NeedleFoundation calls at launch.
// Upstream parses with SwiftSyntax; this port uses a comment- and string-aware scanner, which
// covers the declaration shapes needle supports. Pluginized components get their dependency
// providers; their plugin extension providers are not generated.
static partial class Needle
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("needle", "Compile-time safe Swift dependency injection code generator", Run);

    const string Version = "0.25.1";

    const string Usage = """
        Usage: needle <command> [options]

        Commands:
          generate <destination.swift> <source roots...>
                  Generate the dependency providers and registration code
              --header-doc <path>          File whose text goes at the top of the output
              --additional-imports <m...>  Extra modules to import in the output
              --exclude-suffixes <s...>    Skip files whose names end with these suffixes
              --exclude-paths <p...>       Skip files whose paths contain these strings
              --sources-list-format <fmt>  Source roots that are list files: newline or minescaping
              --pluginized                 Accept pluginized components (plugin extensions not generated)
          print-dependency-tree <source roots...>
                  Print the component tree
          version Show needle's version
        """;

    sealed class Component
    {
        public string Name, Dependency, File;
        public bool Root;
        public Dictionary<string, string> Properties = new(StringComparer.Ordinal);
        public List<string> Children = [];
    }

    sealed class Graph
    {
        public Dictionary<string, Component> Components = new(StringComparer.Ordinal);
        public Dictionary<string, List<(string Name, string Type)>> Dependencies = new(StringComparer.Ordinal);
        public SortedSet<string> Imports = new(StringComparer.Ordinal);
    }

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help") { Console.WriteLine(Usage); return 0; }
        var command = args[0];
        if (command is "version" or "--version") { Console.WriteLine(Version); return 0; }
        var positional = new List<string>();
        var options = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] is "-h" or "--help") { Console.WriteLine(Usage); return 0; }
            if (args[i].StartsWith("--"))
            {
                var name = args[i][2..];
                var values = new List<string>();
                if (name is "pluginized" or "collect-parsing-info" or "allow-multiple-inheritance") { options[name] = values; continue; }
                while (i + 1 < args.Length && !args[i + 1].StartsWith("--")) values.Add(args[++i]);
                // Single-valued options take one argument; the rest stay positional.
                if (name is "header-doc" or "sources-list-format" or "timeout" or "concurrency-limit" or "output-path" or "lint-rule")
                {
                    if (values.Count == 0) throw new UsageException($"--{name} needs a value");
                    positional.AddRange(values.Skip(1));
                    values = values.Take(1).ToList();
                }
                options[name] = values;
            }
            else positional.Add(args[i]);
        }
        var excludeSuffixes = options.GetValueOrDefault("exclude-suffixes") ?? [];
        var excludePaths = options.GetValueOrDefault("exclude-paths") ?? [];
        var listFormat = options.GetValueOrDefault("sources-list-format")?.FirstOrDefault();
        switch (command)
        {
            case "generate":
            {
                if (positional.Count < 2) throw new UsageException("generate needs a destination file and at least one source root");
                var graph = Parse(Sources(positional.Skip(1), excludeSuffixes, excludePaths, listFormat));
                var header = options.TryGetValue("header-doc", out var hd) ? File.ReadAllText(hd[0]) : "";
                foreach (var import in options.GetValueOrDefault("additional-imports") ?? []) graph.Imports.Add(import);
                var code = Generate(graph, header);
                var dir = Path.GetDirectoryName(Path.GetFullPath(positional[0]));
                Directory.CreateDirectory(dir!);
                File.WriteAllText(positional[0], code);
                Console.WriteLine($"Generated {positional[0]} ({graph.Components.Count} components)");
                return 0;
            }
            case "print-dependency-tree":
            {
                if (positional.Count < 1) throw new UsageException("print-dependency-tree needs at least one source root");
                var graph = Parse(Sources(positional, excludeSuffixes, excludePaths, listFormat));
                var sb = new StringBuilder("Root\n");
                void Print(string name, int depth, HashSet<string> seen)
                {
                    sb.Append(new string(' ', depth * 2)).Append(name).Append('\n');
                    if (!seen.Add(name)) return;
                    foreach (var child in graph.Components[name].Children) Print(child, depth + 1, seen);
                    seen.Remove(name);
                }
                foreach (var root in graph.Components.Values.Where(c => c.Root).Select(c => c.Name).Order(StringComparer.Ordinal))
                    Print(root, 1, []);
                Console.Write(sb.ToString());
                return 0;
            }
            default:
                throw new UsageException($"unknown command '{command}' (see needle --help)");
        }
    }

    static List<string> Sources(IEnumerable<string> roots, List<string> excludeSuffixes, List<string> excludePaths, string listFormat)
    {
        var files = new List<string>();
        foreach (var root in roots)
        {
            if (Directory.Exists(root))
                files.AddRange(Directory.EnumerateFiles(root, "*.swift", SearchOption.AllDirectories).Order(StringComparer.Ordinal));
            else if (File.Exists(root) && root.EndsWith(".swift", StringComparison.OrdinalIgnoreCase))
                files.Add(root);
            else if (File.Exists(root))
            {
                // A sources list: one path per line ("newline") or space-separated with escaped spaces.
                var text = File.ReadAllText(root);
                var entries = listFormat == "minescaping"
                    ? Regex.Split(text.Trim(), @"(?<!\\)\s+").Select(s => s.Replace("\\ ", " "))
                    : text.Split('\n').Select(s => s.Trim());
                files.AddRange(entries.Where(e => e.Length > 0 && e.EndsWith(".swift", StringComparison.OrdinalIgnoreCase)));
            }
            else throw new InvalidOperationException($"{root}: no such file or directory");
        }
        return files
            .Where(f => !excludeSuffixes.Any(s => Path.GetFileNameWithoutExtension(f).EndsWith(s, StringComparison.Ordinal)))
            .Where(f => !excludePaths.Any(p => f.Replace('\\', '/').Contains(p.Replace('\\', '/'), StringComparison.Ordinal)))
            .Distinct().ToList();
    }

    static Graph Parse(List<string> files)
    {
        var graph = new Graph();
        var bodies = new List<(Component Component, string Body)>();
        foreach (var file in files)
        {
            var text = Clean(File.ReadAllText(file));
            var hasNeedle = false;
            foreach (Match m in ClassDecl().Matches(text))
            {
                var inherits = TopLevelSplit(m.Groups[2].Value).FirstOrDefault()?.Trim() ?? "";
                var kind = ComponentKind().Match(inherits);
                if (!kind.Success) continue;
                var generic = kind.Groups[2].Value;
                var body = BraceBody(text, m.Index + m.Length - 1);
                var c = new Component
                {
                    Name = m.Groups[1].Value,
                    File = file,
                    Root = kind.Groups[1].Value == "BootstrapComponent",
                    Dependency = generic.Length > 0 ? TopLevelSplit(generic)[0].Trim() : null,
                };
                foreach (var (name, type) in Properties(body)) c.Properties.TryAdd(name, type);
                graph.Components[c.Name] = c;
                bodies.Add((c, body));
                hasNeedle = true;
            }
            foreach (Match m in ProtocolDecl().Matches(text))
            {
                var parents = TopLevelSplit(m.Groups[2].Value).Select(p => p.Trim());
                if (!parents.Any(p => p is "Dependency" or "NeedleFoundation.Dependency")) continue;
                var body = BraceBody(text, m.Index + m.Length - 1);
                graph.Dependencies[m.Groups[1].Value] = DependencyVar().Matches(body)
                    .Select(v => (v.Groups[1].Value, Norm(v.Groups[2].Value))).ToList();
                hasNeedle = true;
            }
            if (hasNeedle)
                foreach (Match m in ImportDecl().Matches(text))
                    if (m.Groups[1].Value != "NeedleFoundation") graph.Imports.Add(m.Groups[1].Value);
        }
        foreach (var (component, body) in bodies)
            foreach (Match m in ChildInit().Matches(body))
                if (graph.Components.ContainsKey(m.Groups[1].Value) && !component.Children.Contains(m.Groups[1].Value))
                    component.Children.Add(m.Groups[1].Value);
        return graph;
    }

    static string Generate(Graph graph, string header)
    {
        // Every path from a root component down the tree, as needle's "^->A->B" strings.
        var paths = new List<List<Component>>();
        void Walk(List<Component> path)
        {
            paths.Add(path);
            foreach (var child in path[^1].Children)
                if (path.All(p => p.Name != child)) Walk([.. path, graph.Components[child]]);
        }
        foreach (var root in graph.Components.Values.Where(c => c.Root).OrderBy(c => c.Name, StringComparer.Ordinal)) Walk([root]);

        var providers = new StringBuilder();
        var registrations = new List<string>();
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        var maxLevel = 0;
        foreach (var path in paths)
        {
            var component = path[^1];
            var pathString = "^->" + string.Join("->", path.Select(p => p.Name));
            var props = component.Dependency != null && graph.Dependencies.TryGetValue(component.Dependency, out var d) ? d : [];
            if (component.Dependency != null && component.Dependency != "EmptyDependency" && !graph.Dependencies.ContainsKey(component.Dependency))
                throw new InvalidOperationException($"{component.File}: dependency protocol {component.Dependency} of {component.Name} was not found in the sources");
            if (props.Count == 0)
            {
                registrations.Add($"    registerProviderFactory(\"{pathString}\", factoryEmptyDependencyProvider)");
                continue;
            }
            // Resolve each dependency property at the nearest ancestor that exposes it.
            var sources = new List<(string Name, string Type, int Level, Component Owner)>();
            foreach (var (name, type) in props)
            {
                var found = false;
                for (var level = 1; level < path.Count; level++)
                {
                    var ancestor = path[^(level + 1)];
                    if (ancestor.Properties.TryGetValue(name, out var t) && Norm(t) == type)
                    {
                        sources.Add((name, type, level, ancestor));
                        maxLevel = Math.Max(maxLevel, level);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    throw new InvalidOperationException($"Could not find a provider for ({name}: {type}) which was required by {component.Dependency}, along the DI branch of {pathString}.");
            }
            var ancestors = sources.Select(s => (s.Level, s.Owner)).Distinct().OrderBy(a => a.Level).ToList();
            string Var((int Level, Component Owner) a) =>
                char.ToLowerInvariant(a.Owner.Name[0]) + a.Owner.Name[1..] + (ancestors.Count(x => x.Owner.Name == a.Owner.Name) > 1 ? a.Level.ToString() : "");
            var signature = component.Dependency + "|" + string.Join(";", sources.Select(s => $"{s.Name}:{s.Type}@{s.Level}:{s.Owner.Name}"));
            var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(signature)))[..20];
            var provider = $"{component.Dependency}{hash}Provider";
            if (emitted.Add(provider))
            {
                providers.Append($"private class {provider}: {component.Dependency} {{\n");
                foreach (var s in sources)
                    providers.Append($"    var {s.Name}: {s.Type} {{\n        return {Var((s.Level, s.Owner))}.{s.Name}\n    }}\n");
                foreach (var a in ancestors) providers.Append($"    private let {Var(a)}: {a.Owner.Name}\n");
                providers.Append($"    init({string.Join(", ", ancestors.Select(a => $"{Var(a)}: {a.Owner.Name}"))}) {{\n");
                foreach (var a in ancestors) providers.Append($"        self.{Var(a)} = {Var(a)}\n");
                providers.Append("    }\n}\n");
                providers.Append($"/// {pathString}\nprivate func factory{hash}(_ component: NeedleFoundation.Scope) -> AnyObject {{\n");
                providers.Append($"    return {provider}({string.Join(", ", ancestors.Select(a => $"{Var(a)}: parent{a.Level}(component) as! {a.Owner.Name}"))})\n}}\n\n");
            }
            registrations.Add($"    registerProviderFactory(\"{pathString}\", factory{hash})");
        }

        var sb = new StringBuilder();
        if (header.Length > 0) sb.Append(header.TrimEnd()).Append("\n\n");
        sb.Append("\n\nimport NeedleFoundation\n");
        foreach (var import in graph.Imports) sb.Append($"import {import}\n");
        sb.Append("\n// swiftlint:disable unused_declaration\nprivate let needleDependenciesHash : String? = nil\n\n");
        sb.Append("// MARK: - Traversal Helpers\n\n");
        for (var level = 1; level <= maxLevel; level++)
            sb.Append($"private func parent{level}(_ component: NeedleFoundation.Scope) -> NeedleFoundation.Scope {{\n    return component{string.Concat(Enumerable.Repeat(".parent", level))}\n}}\n\n");
        sb.Append("// MARK: - Providers\n\n#if !NEEDLE_DYNAMIC\n\n").Append(providers).Append("\n#endif\n\n");
        sb.Append("private func factoryEmptyDependencyProvider(_ component: NeedleFoundation.Scope) -> AnyObject {\n    return EmptyDependencyProvider(component: component)\n}\n\n");
        sb.Append("// MARK: - Registration\nprivate func registerProviderFactory(_ componentPath: String, _ factory: @escaping (NeedleFoundation.Scope) -> AnyObject) {\n");
        sb.Append("    __DependencyProviderRegistry.instance.registerDependencyProviderFactory(for: componentPath, factory)\n}\n\n");
        sb.Append("#if !NEEDLE_DYNAMIC\n\n@inline(never) private func register1() {\n");
        foreach (var r in registrations) sb.Append(r).Append('\n');
        sb.Append("}\n#endif\n\npublic func registerProviderFactories() {\n#if !NEEDLE_DYNAMIC\n    register1()\n#endif\n}\n");
        return sb.ToString();
    }

    // Component properties visible to children: non-private var/let declarations at the top level of the class body.
    static IEnumerable<(string Name, string Type)> Properties(string body)
    {
        var top = new StringBuilder();
        var depth = 0;
        foreach (var ch in body)
        {
            if (ch == '{') { if (depth++ == 0) top.Append("{\n"); continue; }
            if (ch == '}') { depth--; continue; }
            if (depth == 0) top.Append(ch);
        }
        foreach (Match m in PropertyDecl().Matches(top.ToString()))
        {
            var modifiers = m.Groups[1].Value;
            if (Regex.IsMatch(modifiers, @"\b(private|fileprivate|static|class)\b")) continue;
            yield return (m.Groups[2].Value, Norm(m.Groups[3].Value));
        }
    }

    static string Norm(string type) => Regex.Replace(type.Trim(), @"\s+", " ");

    static List<string> TopLevelSplit(string text)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '<' or '(' or '[') depth++;
            else if (text[i] is '>' or ')' or ']') depth--;
            else if (text[i] == ',' && depth == 0) { parts.Add(text[start..i]); start = i + 1; }
        }
        parts.Add(text[start..]);
        return parts;
    }

    static string BraceBody(string text, int open)
    {
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}' && --depth == 0) return text[(open + 1)..i];
        }
        return text[(open + 1)..];
    }

    // Removes comments and blanks string literal contents so braces inside them don't count.
    static string Clean(string source)
    {
        var sb = new StringBuilder(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                while (i < source.Length && source[i] != '\n') i++;
                sb.Append('\n');
                continue;
            }
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
            {
                var depth = 0;
                for (; i < source.Length; i++)
                {
                    if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '*') { depth++; i++; }
                    else if (source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/') { i++; if (--depth == 0) break; }
                    else if (source[i] == '\n') sb.Append('\n');
                }
                sb.Append(' ');
                continue;
            }
            if (c == '"')
            {
                var triple = source.AsSpan(i).StartsWith("\"\"\"");
                var close = triple ? "\"\"\"" : "\"";
                i += close.Length;
                while (i < source.Length && !source.AsSpan(i).StartsWith(close))
                {
                    if (source[i] == '\\') i++;
                    else if (source[i] == '\n') sb.Append('\n');
                    i++;
                }
                i += close.Length - 1;
                sb.Append("\"\"");
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    [GeneratedRegex(@"\bclass\s+(\w+)\s*(?:<[^>{]*>)?\s*:\s*([^{]+)\{")]
    private static partial Regex ClassDecl();

    [GeneratedRegex(@"^(?:NeedleFoundation\.)?(BootstrapComponent|Component|PluginizedComponent|NonCoreComponent)\s*(?:<(.+)>)?\s*$", RegexOptions.Singleline)]
    private static partial Regex ComponentKind();

    [GeneratedRegex(@"\bprotocol\s+(\w+)\s*:\s*([^{]+)\{")]
    private static partial Regex ProtocolDecl();

    [GeneratedRegex(@"\bvar\s+(\w+)\s*:\s*([^{]+?)\s*\{\s*get\s*\}")]
    private static partial Regex DependencyVar();

    [GeneratedRegex(@"(?m)^\s*((?:@\w+(?:\([^)]*\))?\s+|(?:public|open|internal|private|fileprivate|final|override|lazy|static|class|weak|unowned|nonisolated)(?:\(set\))?\s+)*)(?:var|let)\s+(\w+)\s*:\s*([^={\n]+)")]
    private static partial Regex PropertyDecl();

    [GeneratedRegex(@"\b(\w+)\s*\(\s*parent\s*:\s*self\b")]
    private static partial Regex ChildInit();

    [GeneratedRegex(@"(?m)^\s*(?:@\w+\s+)*import\s+(?:(?:class|struct|enum|protocol|func|var|typealias)\s+)?([\w.]+)")]
    private static partial Regex ImportDecl();
}
