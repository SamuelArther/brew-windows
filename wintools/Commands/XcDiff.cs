using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// xcdiff: compare two Xcode projects (.xcodeproj) the way Bloomberg's xcdiff does: file references,
// targets, build phases, sources, headers, resources, run scripts, copy-files phases, configurations,
// build settings, source trees, dependencies, linked dependencies, attributes and Swift packages,
// rendered as console, markdown, JSON or HTML. Reads project.pbxproj directly, so it runs anywhere;
// only RESOLVED_SETTINGS (which asks xcodebuild) needs a Mac.
static class XcDiff
{
    const string Version = "0.14.0";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("xcdiff", "Diff two Xcode projects (.xcodeproj)", Run);

    sealed class Result
    {
        public string Tag;
        public List<string> Context = [];
        public string Description;
        public List<string> OnlyInFirst = [];
        public List<string> OnlyInSecond = [];
        public List<(string Context, string First, string Second)> Different = [];
        public bool Same => OnlyInFirst.Count == 0 && OnlyInSecond.Count == 0 && Different.Count == 0;
    }

    // -t/-g/-c values: null = all, otherwise the listed names.
    sealed class Option(List<string> values)
    {
        public readonly List<string> Values = values;
        public bool Contains(string v) => Values == null || Values.Contains(v);
        public static Option From(string arg) =>
            new(arg == null ? null : arg.Contains(',') ? arg.Split(',').Select(v => v.Trim()).ToList() : [arg]);
    }

    static readonly string[] AllTags =
    [
        "file_references", "build_phases", "copy_files", "targets", "headers", "sources", "resources", "run_scripts",
        "configurations", "settings", "resolved_settings", "source_trees", "dependencies", "linked_dependencies",
        "attributes", "swift_packages",
    ];

    static int Run(string[] args)
    {
        string p1 = null, p2 = null, format = "console", target = null, tag = null, configuration = null;
        bool verbose = false, differencesOnly = false, list = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value() => i + 1 < args.Length ? args[++i] : throw new UsageException($"Missing value for '{arg}'");
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        OVERVIEW: Compare Xcode project (.xcodeproj) files.

                        USAGE: xcdiff [options]

                        OPTIONS:
                          -l, --list              List all available comparators (tags)
                          -v, --verbose           Verbose mode
                          -d, --differences-only  Differences only in the output
                          -p1, --path1 <path1>    Absolute or relative path to the first Xcode project (.xcodeproj file)
                          -p2, --path2 <path2>    Absolute or relative path to the second Xcode project (.xcodeproj file)
                          -f, --format <format>   Output format (console | html | htmlSideBySide | json | markdown) (default: console)
                          -t, --target <target>   Target name
                          -g, --tag <tag>         Tag name
                          -c, --configuration <c> Configuration name
                          --version               Show the version.
                          -h, --help              Show help information.
                        """);
                    return 0;
                case "--version": Console.WriteLine(Version); return 0;
                case "-p1" or "--path1": p1 = Value(); break;
                case "-p2" or "--path2": p2 = Value(); break;
                case "-f" or "--format": format = Value(); break;
                case "-t" or "--target": target = Value(); break;
                case "-g" or "--tag": tag = Value(); break;
                case "-c" or "--configuration": configuration = Value(); break;
                case "-v" or "--verbose": verbose = true; break;
                case "-d" or "--differences-only": differencesOnly = true; break;
                case "-l" or "--list": list = true; break;
                default:
                    // Combined short flags (-vd).
                    if (arg.Length > 2 && arg[0] == '-' && arg[1] != '-' && arg[1..].All(c => c is 'v' or 'd' or 'l'))
                    {
                        verbose |= arg.Contains('v'); differencesOnly |= arg.Contains('d'); list |= arg.Contains('l');
                        break;
                    }
                    XcOut.Error($"Error: Unknown option '{arg}'\nUsage: [options]\n  See 'xcdiff --help' for more information.\n");
                    return 1;
            }
        }
        if (format is not ("console" or "markdown" or "json" or "html" or "htmlSideBySide"))
            throw new UsageException($"The value '{format}' is invalid for '-f <format>'");
        if (list) { PrintList(); return 0; }
        try
        {
            var (path1, path2) = Paths(p1, p2);
            var tags = Tags(tag);
            var targets = Option.From(target);
            var configurations = Option.From(configuration);
            var first = XcProjFile.Load(path1);
            var second = XcProjFile.Load(path2);
            var results = new List<Result>();
            foreach (var t in tags) results.AddRange(Compare(t, first, second, targets, configurations));
            if (differencesOnly) results = results.Where(r => !r.Same).ToList();
            var output = format switch
            {
                "json" => Json(results),
                "markdown" => Text(results, verbose, markdown: true),
                "html" or "htmlSideBySide" => Html(results, verbose),
                _ => Text(results, verbose, markdown: false),
            };
            XcOut.Write(output + "\n");
            return results.All(r => r.Same) ? 0 : 2;
        }
        catch (XcProjException e)
        {
            XcOut.Error($"ERROR: {e.Message}\n");
            return 1;
        }
    }

    static void PrintList()
    {
        var sb = new StringBuilder();
        sb.Append("The following list shows all available comparator tags along with their default\ninclusion status when tags aren't explicitly specified.\n\n");
        sb.Append("  " + "COMPARTOR TAG".PadRight(30) + " | " + "INCLUDED".PadRight(10) + "\n");
        sb.Append(new string('-', 2 * 2 + 30 + 3 + 10) + "\n");
        sb.Append(string.Join("\n", AllTags.Select(t => "- " + t.ToUpperInvariant().PadRight(30) + " | " + (t == "resolved_settings" ? "No" : "Yes"))));
        XcOut.Write(sb + "\n");
    }

    static (string, string) Paths(string p1, string p2)
    {
        if (p1 != null && p2 != null)
        {
            if (!Directory.Exists(p1) && !File.Exists(p1)) throw new XcProjException($"-p1 \"{Absolute(p1)}\" project does not exist");
            if (!Directory.Exists(p2) && !File.Exists(p2)) throw new XcProjException($"-p2 \"{Absolute(p2)}\" project does not exist");
            return (p1, p2);
        }
        var found = Directory.GetDirectories(".", "*.xcodeproj").Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        if (found.Count == 2 && p1 == null && p2 == null) return (found[0], found[1]);
        throw new XcProjException("Could not find 2 projects in the current directory,\nuse `-p1` and `-p2` to specify the projects paths to compare");
    }

    static string Absolute(string path) => path.StartsWith('/') ? path : Path.GetFullPath(path);

    static List<string> Tags(string arg)
    {
        if (arg == null) return AllTags.Where(t => t != "resolved_settings").ToList();
        var requested = arg.Split(',').Select(t => t.Trim().ToUpperInvariant().Replace(' ', '_')).ToList();
        var known = AllTags.Select(t => t.ToUpperInvariant()).ToHashSet();
        var unknown = requested.Where(t => !known.Contains(t)).Distinct().Order(StringComparer.Ordinal).ToList();
        if (unknown.Count > 0)
            throw new XcProjException(requested.Count > 1 && arg.Contains(',')
                ? $"Unsupported tag(s) {string.Join(", ", unknown.Select(t => $"\"{t}\""))}"
                : $"Unsupported tag \"{unknown[0]}\"");
        return AllTags.Where(t => requested.Contains(t.ToUpperInvariant())).ToList();
    }

    // ---- comparators ----

    static IEnumerable<Result> Compare(string tag, XcProjFile a, XcProjFile b, Option targets, Option configurations)
    {
        Result R(params string[] context) => new() { Tag = tag, Context = [.. context] };
        Result Sets(Result r, IEnumerable<string> first, IEnumerable<string> second)
        {
            var f = first.ToHashSet();
            var s = second.ToHashSet();
            r.OnlyInFirst = f.Except(s).Order(StringComparer.Ordinal).ToList();
            r.OnlyInSecond = s.Except(f).Order(StringComparer.Ordinal).ToList();
            return r;
        }
        string T(string name) => $"\"{name}\" target";

        switch (tag)
        {
            case "file_references":
                yield return Sets(R(), FileReferences(a), FileReferences(b));
                break;
            case "targets":
            {
                ValidateTargets(AllTargetNames(a).Union(AllTargetNames(b)), targets);
                var common = CommonTargets(a, b, new Option(null)).Where(p => targets.Contains(p.Name));
                var native = Sets(R("NATIVE targets"), NativeNames(a).Where(targets.Contains), NativeNames(b).Where(targets.Contains));
                foreach (var (name, ta, tb) in common)
                {
                    var pa = XcProjFile.Str(a.Obj(ta), "productType");
                    var pb = XcProjFile.Str(b.Obj(tb), "productType");
                    if (pa != pb) native.Different.Add(($"{name} product type", pa ?? "nil", pb ?? "nil"));
                }
                yield return native;
                yield return Sets(R("AGGREGATE targets"), AggregateNames(a).Where(targets.Contains), AggregateNames(b).Where(targets.Contains));
                break;
            }
            case "build_phases":
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                    yield return BuildPhases(R(T(name)), a, ta, b, tb);
                break;
            case "copy_files":
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                    foreach (var r in CopyFiles(tag, T(name), a, ta, b, tb)) yield return r;
                break;
            case "headers":
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                {
                    var ha = PhaseEntries(a, ta, "PBXHeadersBuildPhase", "ATTRIBUTES");
                    var hb = PhaseEntries(b, tb, "PBXHeadersBuildPhase", "ATTRIBUTES");
                    var r = Sets(R(T(name)), ha.Select(h => h.Path), hb.Select(h => h.Path));
                    var mb = hb.GroupBy(h => h.Path).ToDictionary(g => g.Key, g => g.First());
                    foreach (var h in ha.GroupBy(h => h.Path).Select(g => g.First()).Where(h => mb.ContainsKey(h.Path)).OrderBy(h => h.Path, StringComparer.Ordinal))
                        if (h.Setting != mb[h.Path].Setting)
                            r.Different.Add(($"{h.Path} attributes", h.Setting ?? "nil (Project)", mb[h.Path].Setting ?? "nil (Project)"));
                    yield return r;
                }
                break;
            case "sources":
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                {
                    var sa = PhaseEntries(a, ta, "PBXSourcesBuildPhase", "COMPILER_FLAGS");
                    var sb = PhaseEntries(b, tb, "PBXSourcesBuildPhase", "COMPILER_FLAGS");
                    var r = Sets(R(T(name)), sa.Select(s => s.Path), sb.Select(s => s.Path));
                    var mb = sb.GroupBy(s => s.Path).ToDictionary(g => g.Key, g => g.First());
                    foreach (var s in sa.GroupBy(s => s.Path).Select(g => g.First()).Where(s => mb.ContainsKey(s.Path)).OrderBy(s => s.Path, StringComparer.Ordinal))
                        if (s.Setting != mb[s.Path].Setting)
                            r.Different.Add(($"{s.Path} compiler flags", s.Setting ?? "nil", mb[s.Path].Setting ?? "nil"));
                    yield return r;
                }
                break;
            case "resources":
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                    yield return Sets(R(T(name)), PhaseEntries(a, ta, "PBXResourcesBuildPhase", null).Select(e => e.Path),
                        PhaseEntries(b, tb, "PBXResourcesBuildPhase", null).Select(e => e.Path));
                break;
            case "run_scripts":
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                    foreach (var r in RunScripts(tag, T(name), a, ta, b, tb)) yield return r;
                break;
            case "configurations":
                yield return Sets(R("Root project"), ConfigurationNames(a).Where(configurations.Contains), ConfigurationNames(b).Where(configurations.Contains));
                break;
            case "settings":
            {
                var common = ConfigurationNames(a).Intersect(ConfigurationNames(b)).Where(configurations.Contains).Order(StringComparer.Ordinal).ToList();
                foreach (var config in common)
                    foreach (var r in Settings(tag, ["Root project"], a, XcProjFile.Str(a.Project, "buildConfigurationList"), b, XcProjFile.Str(b.Project, "buildConfigurationList"), config))
                        yield return r;
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                    foreach (var config in common)
                        foreach (var r in Settings(tag, [T(name)], a, XcProjFile.Str(a.Obj(ta), "buildConfigurationList"), b, XcProjFile.Str(b.Obj(tb), "buildConfigurationList"), config))
                            yield return r;
                break;
            }
            case "resolved_settings":
                throw new XcProjException("RESOLVED_SETTINGS asks xcodebuild for the resolved build settings, and xcodebuild only exists on macOS");
            case "source_trees":
                yield return SourceTrees(R("Root project"), a, b);
                break;
            case "dependencies":
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                    yield return Sets(R(T(name)), Dependencies(a, ta), Dependencies(b, tb));
                break;
            case "linked_dependencies":
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                    yield return LinkedDependencies(R(T(name)), a, ta, b, tb);
                break;
            case "attributes":
            {
                yield return Attributes(R("Root project"), ProjectAttributes(a), ProjectAttributes(b));
                foreach (var (name, ta, tb) in CommonTargets(a, b, targets))
                    yield return Attributes(R(T(name)), TargetAttributes(a, ta), TargetAttributes(b, tb));
                break;
            }
            case "swift_packages":
            {
                var pa = Packages(a);
                var pb = Packages(b);
                var r = R();
                var da = pa.GroupBy(p => p.Url).ToDictionary(g => g.Key, g => g.Last());
                var db = pb.GroupBy(p => p.Url).ToDictionary(g => g.Key, g => g.Last());
                r.OnlyInFirst = pa.Where(p => !db.ContainsKey(p.Url)).Select(p => p.ToString()).ToList();
                r.OnlyInSecond = pb.Where(p => !da.ContainsKey(p.Url)).Select(p => p.ToString()).ToList();
                foreach (var p in pa.Where(p => db.ContainsKey(p.Url)))
                    if (da[p.Url] != db[p.Url]) r.Different.Add(($"{p.Name} ({p.Url})", da[p.Url].Difference(db[p.Url]), db[p.Url].Difference(da[p.Url])));
                yield return r;
                break;
            }
        }
    }

    static IEnumerable<string> NativeNames(XcProjFile p) => p.ObjectsOfIsa("PBXNativeTarget").Select(p.TargetName);
    static IEnumerable<string> AggregateNames(XcProjFile p) => p.ObjectsOfIsa("PBXAggregateTarget").Select(p.TargetName);
    static HashSet<string> AllTargetNames(XcProjFile p) => NativeNames(p).Concat(AggregateNames(p)).ToHashSet();

    static void ValidateTargets(IEnumerable<string> available, Option option)
    {
        if (option.Values == null) return;
        var unknown = option.Values.Except(available).Order(StringComparer.Ordinal).ToList();
        if (unknown.Count > 0)
            throw new XcProjException($"Cannot find target{(unknown.Count > 1 ? "s" : "")} {string.Join(", ", unknown.Select(u => $"\"{u}\""))} in both projects");
    }

    static List<(string Name, string A, string B)> CommonTargets(XcProjFile a, XcProjFile b, Option targets)
    {
        var common = AllTargetNames(a).Intersect(AllTargetNames(b)).ToList();
        ValidateTargets(common, targets);
        string Find(XcProjFile p, string name) =>
            p.ObjectsOfIsa("PBXNativeTarget", "PBXAggregateTarget", "PBXLegacyTarget").First(t => p.TargetName(t) == name);
        return common.Where(targets.Contains).Order(StringComparer.Ordinal).Select(n => (n, Find(a, n), Find(b, n))).ToList();
    }

    static string PathOf(XcProjFile p, string element) => element == null ? null : p.FullPath(element) ?? XcProjFile.Str(p.Obj(element), "path");

    static IEnumerable<string> FileReferences(XcProjFile p) =>
        p.ObjectsOfIsa("PBXFileReference").Select(f => PathOf(p, f)).Where(f => f != null);

    // Files of a target's first phase of `isa`, with one build-file setting (COMPILER_FLAGS, ATTRIBUTES).
    static List<(string Path, string Setting)> PhaseEntries(XcProjFile p, string target, string isa, string setting)
    {
        var phase = p.Phase(target, isa);
        if (phase == null) return [];
        var result = new List<(string, string)>();
        foreach (var bf in p.PhaseFiles(phase))
        {
            var o = p.Obj(bf);
            var file = XcProjFile.Str(o, "fileRef");
            if (setting == null && file == null) continue;
            string value = null;
            if (setting != null && XcPlist.Dict(o, "settings") is { } settings && settings.TryGetValue(setting, out var v))
                value = v is List<object> l ? string.Join(", ", l.Select(XcPlist.Scalar)) : XcPlist.Scalar(v);
            var path = PathOf(p, file);
            if (setting == null && path == null) continue;
            result.Add((path ?? "", value));
        }
        return result;
    }

    sealed record PhaseId(string Name, string Type)
    {
        public override string ToString() => Name.Equals(Type, StringComparison.OrdinalIgnoreCase) ? Name : $"{Name} ({Type})";
    }

    static string PhaseType(string isa) => isa switch
    {
        "PBXSourcesBuildPhase" => "sources",
        "PBXFrameworksBuildPhase" => "frameworks",
        "PBXResourcesBuildPhase" => "resources",
        "PBXCopyFilesBuildPhase" => "copyFiles",
        "PBXShellScriptBuildPhase" => "runScript",
        "PBXHeadersBuildPhase" => "headers",
        "PBXRezBuildPhase" => "carbonResources",
        _ => isa,
    };

    static Result BuildPhases(Result r, XcProjFile a, string ta, XcProjFile b, string tb)
    {
        (PhaseId Id, string Props)[] Descriptors(XcProjFile p, string t) => p.BuildPhases(t).Select(ph =>
        {
            var o = p.Obj(ph);
            return (new PhaseId(p.PhaseName(ph), PhaseType(XcProjFile.Str(o, "isa"))), Props(o));
        }).ToArray();
        static string Props(OrderedDictionary<string, object> o) =>
            $"{XcProjFile.Str(o, "runOnlyForDeploymentPostprocessing") ?? "0"}|{List(o, "inputFileListPaths")}|{List(o, "outputFileListPaths")}";
        var da = Descriptors(a, ta);
        var db = Descriptors(b, tb);
        var ia = da.Select(d => d.Id).ToList();
        var ib = db.Select(d => d.Id).ToList();
        r.OnlyInFirst = Remove(ia, ib).Select(i => i.ToString()).ToList();
        r.OnlyInSecond = Remove(ib, ia).Select(i => i.ToString()).ToList();
        var ca = Common(ia, ib);
        var cb = Common(ib, ia);
        var n = Math.Min(ca.Count, cb.Count);
        if (!ca.Take(n).SequenceEqual(cb.Take(n)))
            r.Different.Add(("Different order", string.Join(", ", ca.Take(n)), string.Join(", ", cb.Take(n))));
        foreach (var id in ia.Intersect(ib))
        {
            var fa = da.Where(d => d.Id == id).ToList();
            var fb = db.Where(d => d.Id == id).ToList();
            for (var i = 0; i < Math.Min(fa.Count, fb.Count); i++)
            {
                if (fa[i].Props == fb[i].Props) continue;
                r.Different.Add(($"Different properties in \"{id}\"", PropsDiff(fa[i].Props, fb[i].Props), PropsDiff(fb[i].Props, fa[i].Props)));
            }
        }
        return r;

        static string PropsDiff(string x, string y)
        {
            var px = x.Split('|');
            var py = y.Split('|');
            var parts = new List<string>();
            if (px[0] != py[0]) parts.Add($"runOnlyForDeploymentPostprocessing = {(px[0] == "1" ? "true" : "false")}");
            if (px[1] != py[1]) parts.Add($"inputFileListPaths = {(px[1].Length == 0 ? "nil" : px[1])}");
            if (px[2] != py[2]) parts.Add($"outputFileListPaths = {(px[2].Length == 0 ? "nil" : px[2])}");
            return string.Join(", ", parts);
        }
        static List<PhaseId> Remove(List<PhaseId> from, List<PhaseId> other)
        {
            var result = new List<PhaseId>(from);
            foreach (var v in other) { var idx = result.IndexOf(v); if (idx >= 0) result.RemoveAt(idx); }
            return result;
        }
        static List<PhaseId> Common(List<PhaseId> first, List<PhaseId> second)
        {
            var rest = new List<PhaseId>(second);
            var result = new List<PhaseId>();
            foreach (var v in first) { var idx = rest.IndexOf(v); if (idx < 0) continue; result.Add(v); rest.RemoveAt(idx); }
            return result;
        }
    }

    // Swift's description of an optional string array: nil, or ["a", "b"].
    static string List(OrderedDictionary<string, object> o, string key) =>
        XcPlist.List(o, key) is { } l ? "[" + string.Join(", ", l.Select(v => $"\"{XcPlist.Scalar(v)}\"")) + "]" : "";

    static string Describe(OrderedDictionary<string, object> o, string key) =>
        XcPlist.List(o, key) is { } l ? "[" + string.Join(", ", l.Select(v => $"\"{XcPlist.Scalar(v)}\"")) + "]" : XcProjFile.Str(o, key) ?? "nil";

    static IEnumerable<Result> CopyFiles(string tag, string targetContext, XcProjFile a, string ta, XcProjFile b, string tb)
    {
        var ga = a.BuildPhases(ta).Where(p => a.Isa(p) == "PBXCopyFilesBuildPhase").GroupBy(a.PhaseName).ToDictionary(g => g.Key, g => g.ToList());
        var gb = b.BuildPhases(tb).Where(p => b.Isa(p) == "PBXCopyFilesBuildPhase").GroupBy(b.PhaseName).ToDictionary(g => g.Key, g => g.ToList());
        var results = new List<Result>();
        foreach (var name in ga.Keys.Intersect(gb.Keys).Order(StringComparer.Ordinal))
        {
            var la = ga[name];
            var lb = gb[name];
            for (var i = 0; i < Math.Max(la.Count, lb.Count); i++)
            {
                var r = new Result { Tag = tag, Context = [targetContext, name] };
                if (i >= la.Count) { r.Description = "The build phase does not exist in the first project"; r.OnlyInSecond = ["Duplicated build phase name"]; }
                else if (i >= lb.Count) { r.Description = "The build phase does not exist in the second project"; r.OnlyInFirst = ["Duplicated build phase name"]; }
                else
                {
                    var oa = a.Obj(la[i]);
                    var ob = b.Obj(lb[i]);
                    var fa = CopyEntries(a, la[i]);
                    var fb = CopyEntries(b, lb[i]);
                    r.OnlyInFirst = fa.Keys.Except(fb.Keys).Order(StringComparer.Ordinal).ToList();
                    r.OnlyInSecond = fb.Keys.Except(fa.Keys).Order(StringComparer.Ordinal).ToList();
                    if ((XcProjFile.Str(oa, "runOnlyForDeploymentPostprocessing") ?? "0") != (XcProjFile.Str(ob, "runOnlyForDeploymentPostprocessing") ?? "0"))
                        r.Different.Add(("runOnlyForDeploymentPostprocessing", Bool(oa), Bool(ob)));
                    foreach (var key in new[] { "inputFileListPaths", "outputFileListPaths", "dstPath", "dstSubfolderSpec" })
                        if (Describe(oa, key) != Describe(ob, key)) r.Different.Add((key, Describe(oa, key), Describe(ob, key)));
                    foreach (var path in fa.Keys.Intersect(fb.Keys).Order(StringComparer.Ordinal))
                    {
                        var (pfA, atA) = fa[path];
                        var (pfB, atB) = fb[path];
                        if (pfA == pfB && atA == atB) continue;
                        r.Different.Add((path, FileProps(pfA, atA, pfB, atB), FileProps(pfB, atB, pfA, atA)));
                    }
                }
                results.Add(r);
            }
        }
        if (results.Count == 0) results.Add(new Result { Tag = tag, Context = [targetContext] });
        return results;

        static string Bool(OrderedDictionary<string, object> o) => XcProjFile.Str(o, "runOnlyForDeploymentPostprocessing") == "1" ? "true" : "false";
        static string FileProps(string pf, string attrs, string otherPf, string otherAttrs)
        {
            var parts = new List<string>();
            if (pf != otherPf) parts.Add($"platformFilter = {pf ?? "nil"}");
            if (attrs != otherAttrs)
            {
                var mine = attrs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                var theirs = otherAttrs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                parts.Add($"attributes = [{string.Join(", ", mine.Except(theirs).Order(StringComparer.Ordinal).Select(x => $"\"{x}\""))}]");
            }
            return string.Join(", ", parts);
        }
    }

    static Dictionary<string, (string PlatformFilter, string Attributes)> CopyEntries(XcProjFile p, string phase)
    {
        var result = new Dictionary<string, (string, string)>();
        foreach (var bf in p.PhaseFiles(phase))
        {
            var o = p.Obj(bf);
            var path = PathOf(p, XcProjFile.Str(o, "fileRef"));
            if (path == null) continue;
            var attrs = XcPlist.List(XcPlist.Dict(o, "settings"), "ATTRIBUTES")?.Select(XcPlist.Scalar) ?? [];
            if (result.ContainsKey(path))
                throw new XcProjException($"{p.PhaseName(phase)} Build Phase contains 2 references to the same file (path = {path})");
            result[path] = (XcProjFile.Str(o, "platformFilter"), string.Join(",", attrs));
        }
        return result;
    }

    static IEnumerable<Result> RunScripts(string tag, string targetContext, XcProjFile a, string ta, XcProjFile b, string tb)
    {
        var ga = a.BuildPhases(ta).Where(p => a.Isa(p) == "PBXShellScriptBuildPhase").GroupBy(a.PhaseName).ToDictionary(g => g.Key, g => g.ToList());
        var gb = b.BuildPhases(tb).Where(p => b.Isa(p) == "PBXShellScriptBuildPhase").GroupBy(b.PhaseName).ToDictionary(g => g.Key, g => g.ToList());
        var results = new List<Result>();
        foreach (var name in ga.Keys.Intersect(gb.Keys).Order(StringComparer.Ordinal))
        {
            var la = ga[name];
            var lb = gb[name];
            for (var i = 0; i < Math.Max(la.Count, lb.Count); i++)
            {
                var r = new Result { Tag = tag, Context = [targetContext, $"\"{name}\" build phase"] };
                if (i >= la.Count) { r.Description = "The build phase does not exist in the first project"; r.OnlyInSecond = ["Duplicated build phase name"]; }
                else if (i >= lb.Count) { r.Description = "The build phase does not exist in the second project"; r.OnlyInFirst = ["Duplicated build phase name"]; }
                else
                {
                    var oa = a.Obj(la[i]);
                    var ob = b.Obj(lb[i]);
                    foreach (var key in new[] { "inputPaths", "outputPaths", "shellPath", "shellScript", "showEnvVarsInLog", "alwaysOutOfDate", "dependencyFile", "inputFileListPaths", "outputFileListPaths" })
                    {
                        string Value(OrderedDictionary<string, object> o) => key switch
                        {
                            "showEnvVarsInLog" => XcProjFile.Str(o, key) == "0" ? "false" : "true",
                            "alwaysOutOfDate" => XcProjFile.Str(o, key) == "1" ? "true" : "false",
                            "inputPaths" or "outputPaths" or "inputFileListPaths" or "outputFileListPaths" =>
                                XcPlist.List(o, key) is { } l ? "[" + string.Join(", ", l.Select(v => $"\"{XcPlist.Scalar(v)}\"")) + "]" : "[]",
                            _ => XcProjFile.Str(o, key) ?? "nil",
                        };
                        var va = Value(oa);
                        var vb = Value(ob);
                        if (va != vb) r.Different.Add((key, va, vb));
                    }
                }
                results.Add(r);
            }
        }
        if (results.Count == 0) results.Add(new Result { Tag = tag, Context = [targetContext] });
        return results;
    }

    static IEnumerable<string> ConfigurationNames(XcProjFile p) =>
        p.ObjectsOfIsa("XCConfigurationList").SelectMany(l => p.Ids(p.Obj(l), "buildConfigurations")).Select(c => XcProjFile.Str(p.Obj(c), "name")).Where(n => n != null).Distinct();

    static IEnumerable<Result> Settings(string tag, List<string> parent, XcProjFile a, string la, XcProjFile b, string lb, string config)
    {
        var context = parent.Append($"\"{config}\" configuration").ToList();
        var ca = la == null ? null : a.Configuration(la, config);
        var cb = lb == null ? null : b.Configuration(lb, config);
        if (ca == null || cb == null)
        {
            var r = new Result { Tag = tag, Context = context };
            if (ca == null && cb != null) r.OnlyInSecond = [$"\"{config}\" configuration"];
            if (ca != null && cb == null) r.OnlyInFirst = [$"\"{config}\" configuration"];
            yield return r;
            yield break;
        }
        var oa = a.Obj(ca);
        var ob = b.Obj(cb);
        var baseResult = new Result { Tag = tag, Context = [.. context, "Base configuration"] };
        var bca = BaseConfiguration(a, oa);
        var bcb = BaseConfiguration(b, ob);
        if (bca != bcb) baseResult.Different.Add(("Path to .xcconfig", bca ?? "nil", bcb ?? "nil"));
        yield return baseResult;

        var sa = XcPlist.Dict(oa, "buildSettings") ?? [];
        var sb = XcPlist.Dict(ob, "buildSettings") ?? [];
        var values = new Result { Tag = tag, Context = [.. context, "Values"] };
        values.OnlyInFirst = sa.Keys.Except(sb.Keys).Order(StringComparer.Ordinal).Select(k => $"{k} = {SettingText(sa[k], true)}").ToList();
        values.OnlyInSecond = sb.Keys.Except(sa.Keys).Order(StringComparer.Ordinal).Select(k => $"{k} = {SettingText(sb[k], true)}").ToList();
        var nameA = XcPlist.Str(sa, "PROJECT_NAME");
        var nameB = XcPlist.Str(sb, "PROJECT_NAME");
        foreach (var key in sa.Keys.Intersect(sb.Keys).Order(StringComparer.Ordinal))
        {
            var va = SettingText(sa[key], false);
            var vb = SettingText(sb[key], false);
            if (va == vb) continue;
            if (nameA != null && nameB != null && SuffixDiff(nameA, nameB) == SuffixDiff(va, vb)) continue;
            values.Different.Add((key, va, vb));
        }
        yield return values;
    }

    // xcdiff prints a setting's value as Swift does: strings as-is, arrays joined (values) or as
    // NSArray descriptions (only-in lists).
    static string SettingText(object value, bool describe) => value switch
    {
        List<object> l when describe => "[" + string.Join(", ", l.Select(v => "\"" + XcPlist.Scalar(v) + "\"")) + "]",
        List<object> l => string.Join(" ", l.Select(XcPlist.Scalar)),
        _ => XcPlist.Scalar(value) ?? "nil",
    };

    static string SwiftDescribe(string s) =>
        s.Length > 0 && s.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '/' or '$') ? s : "\"" + s.Replace("\"", "\\\"") + "\"";

    // The (prefix, suffix) difference of two strings (xcdiff's SuffixStringDiff), so settings that
    // only differ by the project name compare equal.
    static string SuffixDiff(string a, string b)
    {
        var prefix = 0;
        while (prefix < a.Length && prefix < b.Length && a[prefix] == b[prefix]) prefix++;
        var suffix = 0;
        while (suffix < a.Length - prefix && suffix < b.Length - prefix && a[a.Length - 1 - suffix] == b[b.Length - 1 - suffix]) suffix++;
        return a.Substring(prefix, a.Length - prefix - suffix) + "\0" + b.Substring(prefix, b.Length - prefix - suffix);
    }

    static string BaseConfiguration(XcProjFile p, OrderedDictionary<string, object> config)
    {
        var reference = XcProjFile.Str(config, "baseConfigurationReference");
        return reference == null ? null : PathOf(p, reference);
    }

    static Result SourceTrees(Result r, XcProjFile a, XcProjFile b)
    {
        r.Description = "Output format: (<path>, <name>, <source_tree>)";
        List<List<(string Full, string Path, string Name, string Tree)>> Chains(XcProjFile p)
        {
            var chains = new List<List<(string, string, string, string)>>();
            foreach (var f in p.ObjectsOfIsa("PBXFileReference"))
            {
                var chain = new List<(string, string, string, string)>();
                for (var e = f; e != null; e = p.Parent(e))
                {
                    var o = p.Obj(e);
                    chain.Add((p.FullPath(e), XcProjFile.Str(o, "path"), XcProjFile.Str(o, "name"), XcProjFile.Str(o, "sourceTree")));
                }
                chains.Add(chain);
            }
            return chains;
        }
        static string Key(List<(string Full, string Path, string Name, string Tree)> c) => string.Join("\u0001", c.Select(e => $"{e.Full}\u0002{e.Path}\u0002{e.Name}\u0002{e.Tree}"));
        static string Text(List<(string Full, string Path, string Name, string Tree)> c) =>
            string.Join(" → ", c.Select(e => $"({e.Path ?? "nil"}, {e.Name ?? "nil"}, {e.Tree ?? "nil"})"));
        var ca = Chains(a).GroupBy(Key).Select(g => g.First()).ToList();
        var cb = Chains(b).GroupBy(Key).Select(g => g.First()).ToList();
        var common = ca.Select(Key).Intersect(cb.Select(Key)).ToHashSet();
        var da = ca.Where(c => !common.Contains(Key(c))).GroupBy(c => c[0].Full ?? "\0nil").ToDictionary(g => g.Key, g => g.ToList());
        var db = cb.Where(c => !common.Contains(Key(c))).GroupBy(c => c[0].Full ?? "\0nil").ToDictionary(g => g.Key, g => g.ToList());
        r.OnlyInFirst = da.Where(kv => kv.Key == "\0nil" || !db.ContainsKey(kv.Key)).SelectMany(kv => kv.Value)
            .OrderBy(c => c[0].Path ?? "", StringComparer.Ordinal).Select(Text).ToList();
        r.OnlyInSecond = db.Where(kv => kv.Key == "\0nil" || !da.ContainsKey(kv.Key)).SelectMany(kv => kv.Value)
            .OrderBy(c => c[0].Path ?? "", StringComparer.Ordinal).Select(Text).ToList();
        foreach (var key in da.Keys.Where(k => k != "\0nil" && db.ContainsKey(k)).Order(StringComparer.Ordinal))
            r.Different.Add((key, string.Join(", ", da[key].Select(Text)), string.Join(", ", db[key].Select(Text))));
        return r;
    }

    static IEnumerable<string> Dependencies(XcProjFile p, string target)
    {
        foreach (var dep in p.Ids(p.Obj(target), "dependencies"))
        {
            var o = p.Obj(dep);
            var name = XcProjFile.Str(o, "name");
            var productName = XcProjFile.Str(p.Obj(XcProjFile.Str(o, "productRef")), "productName");
            var parts = new List<string>();
            var targetId = XcProjFile.Str(o, "target");
            string context;
            if (targetId != null && p.Obj(targetId) != null)
            {
                var targetName = p.TargetName(targetId);
                if (name != null && name != targetName) parts.Add($"name={name}");
                context = $"target={targetName}";
            }
            else
            {
                if (name != null) parts.Add($"name={name}");
                var proxy = p.Obj(XcProjFile.Str(o, "targetProxy"));
                if (proxy != null)
                {
                    var ctx = new List<string>();
                    var type = XcProjFile.Str(proxy, "proxyType");
                    if (type != null) ctx.Add(type == "1" ? "proxy_type=native_target" : type == "2" ? "proxy_type=reference" : "proxy_type=other");
                    var portal = XcProjFile.Str(proxy, "containerPortal");
                    ctx.Add(p.Isa(portal) switch
                    {
                        "PBXProject" => $"proxy_project={p.Name}",
                        "PBXFileReference" => $"proxy_file_reference(path={PathOf(p, portal) ?? "nil"})",
                        _ => "proxy_unknown_object",
                    });
                    context = string.Join(", ", ctx);
                }
                else context = "context=none";
            }
            if (productName != null) parts.Add($"product_name={productName}");
            parts.Add(context);
            yield return "(" + string.Join(", ", parts) + ")";
        }
    }

    sealed record Package(string Name, string Url, string Version)
    {
        public override string ToString() => $"{Name} ({Url}) {Version}";

        public string Difference(Package other)
        {
            if (other == null) return ToString();
            if (other == this) return null;
            var parts = new List<string>();
            if (Name != other.Name) parts.Add($"name = {Name}");
            if (Url != other.Url) parts.Add($"url = {Url}");
            if (Version != other.Version) parts.Add($"version = {Version}");
            return string.Join(", ", parts);
        }
    }

    static Package PackageOf(XcProjFile p, string id)
    {
        var o = p.Obj(id);
        if (o == null) return null;
        var url = XcProjFile.Str(o, "repositoryURL") ?? "nil";
        var name = url == "nil" ? "nil" : url.TrimEnd('/').Split('/')[^1].Replace(".git", "");
        var req = XcPlist.Dict(o, "requirement");
        string version = "nil";
        if (req != null)
        {
            var kind = XcPlist.Str(req, "kind");
            version = kind switch
            {
                "upToNextMajorVersion" => $".upToNextMajorVersion({XcPlist.Str(req, "minimumVersion")})",
                "upToNextMinorVersion" => $".upToNextMinorVersion({XcPlist.Str(req, "minimumVersion")})",
                "exactVersion" => $".exact({XcPlist.Str(req, "version")})",
                "versionRange" => $".range(from: {XcPlist.Str(req, "minimumVersion")}, to: {XcPlist.Str(req, "maximumVersion")})",
                "branch" => $".branch({XcPlist.Str(req, "branch")})",
                "revision" => $".revision({XcPlist.Str(req, "revision")})",
                _ => kind ?? "nil",
            };
        }
        return new Package(name, url, version);
    }

    static List<Package> Packages(XcProjFile p) =>
        p.Ids(p.Project, "packageReferences").Where(id => p.Isa(id) == "XCRemoteSwiftPackageReference").Select(id => PackageOf(p, id)).ToList();

    static Result LinkedDependencies(Result r, XcProjFile a, string ta, XcProjFile b, string tb)
    {
        List<(string Key, string Type, Package Package)> Deps(XcProjFile p, string t)
        {
            var phase = p.Phase(t, "PBXFrameworksBuildPhase");
            var list = new List<(string, string, Package)>();
            if (phase == null) return list;
            foreach (var bf in p.PhaseFiles(phase))
            {
                var o = p.Obj(bf);
                var type = o.ContainsKey("settings") ? "optional" : "required";
                var file = p.Obj(XcProjFile.Str(o, "fileRef"));
                if (file != null && (XcProjFile.Str(file, "name") ?? XcProjFile.Str(file, "path")) is { } key) { list.Add((key, type, null)); continue; }
                var product = p.Obj(XcProjFile.Str(o, "productRef"));
                if (product != null && XcProjFile.Str(product, "productName") is { } productName)
                    list.Add((productName, type, PackageOf(p, XcProjFile.Str(product, "package"))));
            }
            return list;
        }
        var da = Deps(a, ta);
        var db = Deps(b, tb);
        r.OnlyInFirst = da.Select(d => d.Key).Except(db.Select(d => d.Key)).Order(StringComparer.Ordinal).ToList();
        r.OnlyInSecond = db.Select(d => d.Key).Except(da.Select(d => d.Key)).Order(StringComparer.Ordinal).ToList();
        var ma = da.GroupBy(d => d.Key).ToDictionary(g => g.Key, g => g.First());
        var mb = db.GroupBy(d => d.Key).ToDictionary(g => g.Key, g => g.First());
        var common = ma.Keys.Intersect(mb.Keys).Order(StringComparer.Ordinal).ToList();
        foreach (var key in common)
            if (ma[key].Type != mb[key].Type) r.Different.Add(($"{key} attributes", ma[key].Type, mb[key].Type));
        foreach (var key in common)
            if (ma[key].Package != mb[key].Package)
                r.Different.Add(($"{key} package reference", ma[key].Package?.Difference(mb[key].Package) ?? "nil", mb[key].Package?.Difference(ma[key].Package) ?? "nil"));
        return r;
    }

    static Dictionary<string, string> ProjectAttributes(XcProjFile p)
    {
        var attrs = XcPlist.Dict(p.Project, "attributes") ?? [];
        return attrs.Where(kv => kv.Key != "TargetAttributes").ToDictionary(kv => kv.Key, kv => AttributeText(p, kv.Value, true));
    }

    static Dictionary<string, string> TargetAttributes(XcProjFile p, string target)
    {
        var all = XcPlist.Dict(XcPlist.Dict(p.Project, "attributes"), "TargetAttributes");
        var attrs = XcPlist.Dict(all, target) ?? [];
        return attrs.ToDictionary(kv => kv.Key, kv => AttributeText(p, kv.Value, true));
    }

    static string AttributeText(XcProjFile p, object value, bool top)
    {
        if (value is OrderedDictionary<string, object> d)
            return "[" + string.Join(", ", d.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"\"{kv.Key}\": {AttributeText(p, kv.Value, false)}")) + "]";
        var s = value is List<object> l ? "[" + string.Join(", ", l.Select(v => $"\"{XcPlist.Scalar(v)}\"")) + "]" : XcPlist.Scalar(value) ?? "nil";
        if (p.Obj(s) is { } o && XcProjFile.Str(o, "isa")?.EndsWith("Target") == true) s = XcProjFile.Str(o, "name");
        return top ? s : $"\"{s}\"";
    }

    static Result Attributes(Result r, Dictionary<string, string> a, Dictionary<string, string> b)
    {
        r.OnlyInFirst = a.Keys.Except(b.Keys).Order(StringComparer.Ordinal).Select(k => $"{k} = {a[k]}").ToList();
        r.OnlyInSecond = b.Keys.Except(a.Keys).Order(StringComparer.Ordinal).Select(k => $"{k} = {b[k]}").ToList();
        foreach (var k in a.Keys.Intersect(b.Keys).Order(StringComparer.Ordinal))
            if (a[k] != b[k]) r.Different.Add((k, a[k], b[k]));
        return r;
    }

    // ---- rendering ----

    static string Title(Result r) => r.Tag.ToUpperInvariant() + (r.Context.Count > 0 ? " > " + string.Join(" > ", r.Context) : "");

    static string Text(List<Result> results, bool verbose, bool markdown)
    {
        var sb = new StringBuilder();
        var indent = 0;
        void H2(string t) { if (markdown) sb.Append($"\n## {t}\n\n"); else sb.Append(t).Append('\n'); }
        void H3(string t) { if (markdown) sb.Append($"\n### {t}\n\n"); else sb.Append($"\n{t}\n\n"); }
        void Pre(string t) { if (markdown) sb.Append($"`{t}`\n"); else sb.Append(t).Append('\n'); }
        void Item(Action content)
        {
            if (markdown) sb.Append(new string(' ', 2 * indent)).Append("- ");
            else sb.Append(new string(' ', 2 * indent)).Append(indent == 1 ? "• " : "◦ ");
            content();
        }
        void TextItem(string t) => Item(() => { if (markdown) Pre(t); else sb.Append(t).Append('\n'); });
        void ListOf(Action content) { indent++; content(); indent--; sb.Append('\n'); }

        foreach (var r in results)
        {
            if (r.Same) { H2($"✅ {Title(r)}"); continue; }
            H2($"❌ {Title(r)}");
            if (!verbose) continue;
            if (r.Description != null) sb.Append(r.Description).Append('\n');
            if (r.OnlyInFirst.Count > 0)
            {
                H3($"⚠️  Only in first ({r.OnlyInFirst.Count}):");
                ListOf(() => r.OnlyInFirst.ForEach(TextItem));
            }
            if (r.OnlyInSecond.Count > 0)
            {
                H3($"⚠️  Only in second ({r.OnlyInSecond.Count}):");
                ListOf(() => r.OnlyInSecond.ForEach(TextItem));
            }
            if (r.Different.Count > 0)
            {
                H3($"⚠️  Value mismatch ({r.Different.Count}):");
                ListOf(() =>
                {
                    foreach (var d in r.Different)
                        Item(() =>
                        {
                            Pre(d.Context);
                            ListOf(() => { TextItem(d.First); TextItem(d.Second); });
                        });
                });
            }
            if (r.Different.Count == 0) sb.Append('\n');
        }
        return sb.ToString();
    }

    // JSON as Swift's JSONEncoder writes it with .prettyPrinted and .sortedKeys.
    static string Json(List<Result> results)
    {
        var sb = new StringBuilder();
        static string S(string s)
        {
            var e = new StringBuilder("\"");
            foreach (var c in s)
                e.Append(c switch { '"' => "\\\"", '\\' => "\\\\", '/' => "\\/", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", < ' ' => $"\\u{(int)c:x4}", _ => c.ToString() });
            return e.Append('"').ToString();
        }
        void Array(IReadOnlyList<string> items, string pad)
        {
            if (items.Count == 0) { sb.Append("[\n\n").Append(pad).Append(']'); return; }
            sb.Append("[\n");
            for (var i = 0; i < items.Count; i++) sb.Append(pad).Append("  ").Append(items[i]).Append(i + 1 < items.Count ? ",\n" : "\n");
            sb.Append(pad).Append(']');
        }
        if (results.Count == 0) return "[\n\n]";
        sb.Append("[\n");
        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            sb.Append("  {\n    \"context\" : ");
            Array(r.Context.Select(S).ToList(), "    ");
            sb.Append(",\n");
            if (r.Description != null) sb.Append("    \"description\" : ").Append(S(r.Description)).Append(",\n");
            sb.Append("    \"differentValues\" : ");
            Array(r.Different.Select(d => "{\n        \"context\" : " + S(d.Context) + ",\n        \"first\" : " + S(d.First) + ",\n        \"second\" : " + S(d.Second) + "\n      }").ToList(), "    ");
            sb.Append(",\n    \"onlyInFirst\" : ");
            Array(r.OnlyInFirst.Select(S).ToList(), "    ");
            sb.Append(",\n    \"onlyInSecond\" : ");
            Array(r.OnlyInSecond.Select(S).ToList(), "    ");
            sb.Append(",\n    \"tag\" : ").Append(S(r.Tag)).Append("\n  }").Append(i + 1 < results.Count ? ",\n" : "\n");
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string Html(List<Result> results, bool verbose)
    {
        static string E(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n<title>xcdiff results</title>\n<style>\n")
          .Append("body{font-family:-apple-system,Segoe UI,Helvetica,Arial,sans-serif;margin:24px}.success{color:#1a7f37}.warning{color:#cf222e}code{background:#f6f8fa;padding:1px 4px}\n")
          .Append("</style>\n</head>\n<body>\n");
        foreach (var r in results)
        {
            sb.Append(r.Same ? "<h2 class=\"success\">✅ " : "<h2 class=\"warning\">❌ ").Append(E(Title(r))).Append("</h2>\n");
            if (r.Same || !verbose) continue;
            if (r.Description != null) sb.Append("<p>").Append(E(r.Description)).Append("</p>\n");
            void Section(string title, IEnumerable<string> items)
            {
                var list = items.ToList();
                if (list.Count == 0) return;
                sb.Append("<h3>⚠️  ").Append(E(title)).Append($" ({list.Count}):</h3>\n<ul>\n");
                foreach (var item in list) sb.Append("<li><code>").Append(E(item)).Append("</code></li>\n");
                sb.Append("</ul>\n");
            }
            Section("Only in first", r.OnlyInFirst);
            Section("Only in second", r.OnlyInSecond);
            if (r.Different.Count > 0)
            {
                sb.Append($"<h3>⚠️  Value mismatch ({r.Different.Count}):</h3>\n<ul>\n");
                foreach (var d in r.Different)
                    sb.Append("<li><code>").Append(E(d.Context)).Append("</code><ul><li><code>").Append(E(d.First))
                      .Append("</code></li><li><code>").Append(E(d.Second)).Append("</code></li></ul></li>\n");
                sb.Append("</ul>\n");
            }
        }
        sb.Append("</body>\n</html>");
        return sb.ToString();
    }
}
