using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WinTools;

// bluepill / bp: run iOS UI tests on parallel simulators. Everything up to booting simulators works
// here: options and JSON config files, finding the test bundles (an app's PlugIns, -t/-u/-U, or an
// .xctestrun file), listing their tests from the Mach-O symbol tables (-l), include/exclude
// filters, and packing tests into -n simulator bundles by --test-time-estimates-json the way
// bluepill does. --merge-reports combines per-simulator JUnit reports into TEST-FinalReport.xml.
// Running the tests needs the iOS Simulator, which is part of Xcode on macOS, so it stops there.
static class Bluepill
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("bluepill", "Run iOS UI tests on multiple simulators (planning and reports on Windows)", RunBluepill);
        Commands.Add("bp", "Run one bluepill test bundle on a simulator", RunBp);
    }

    // (short, long, takes a value, description); the long name is also the config-file key.
    static readonly (char Short, string Long, bool Value, string Help)[] Options =
    [
        ('a', "app", true, "The path to the host application to execute (your .app)"),
        ('b', "runner-app-path", true, "The path to the test runner app (for UI tests)"),
        ('s', "scheme-path", true, "The scheme to run tests"),
        ('o', "output-dir", true, "Directory where to put output log files (bluepill only)"),
        ('c', "config", true, "Read options from the specified configuration file instead of the command line"),
        ('d', "device", true, "On which device to run the app (default \"iPhone 8\")"),
        ('r', "runtime", true, "What runtime to use (default \"iOS 13.0\")"),
        ('n', "num-sims", true, "Number of simulators to run in parallel (bluepill only) (default 4)"),
        ('i', "include", true, "Include a testcase in the set of tests to run (unless specified in `exclude`)"),
        ('x', "exclude", true, "Exclude a testcase in the set of tests to run (takes priority over `include`)"),
        ('t', "test-bundle-path", true, "The path to the test bundle to execute (single .xctest)"),
        ('X', "xctestrun-path", true, "The path to the .xctestrun file"),
        ('u', "additional-unit-xctests", true, "Additional XCTest bundles to test"),
        ('U', "additional-ui-xctests", true, "Additional XCUITest bundles to test"),
        ('T', "test-timeout", true, "Timeout in seconds for each test"),
        ('R', "error-retries", true, "Number of times to recover from simulator/app crashing/hanging and continue running"),
        ('f', "failure-tolerance", true, "The number of times a test can fail before giving up"),
        ('F', "only-retry-failed", false, "Only retry failed tests instead of all"),
        ('l', "list-tests", false, "Only list tests and exit without executing tests"),
        ('H', "headless", false, "Run in headless mode (no GUI)"),
        ('J', "junit-output", false, "Print results in JUnit format"),
        ('p', "plain-output", false, "Print results in plain text"),
        ('j', "json-output", false, "Print results in JSON format"),
        ('v', "version", false, "Print version"),
        ('h', "help", false, "Show this help"),
        ('\0', "test-time-estimates-json", true, "Path of the input file containing estimated execution times for each test"),
        ('\0', "xcode-path", true, "Path to the Xcode to use"),
        ('\0', "stuck-timeout", true, "Timeout in seconds for a test that seems stuck (no output)"),
        ('\0', "max-sim-count", true, "Maximum number of simulators to create"),
        ('\0', "retry-app-crash-tests", false, "Retry the tests after an app crash"),
        ('\0', "keep-individual-test-reports", false, "Keep the per-simulator test reports"),
        ('\0', "merge-reports", true, "Merge the JUnit reports in a directory into TEST-FinalReport.xml (Windows)"),
    ];

    static readonly HashSet<string> Multi = ["include", "exclude", "additional-unit-xctests", "additional-ui-xctests"];

    static void Usage(string program)
    {
        var sb = new StringBuilder($"Usage: {program} [options]\n");
        foreach (var (s, l, value, help) in Options)
        {
            if (program == "bp" && l is "num-sims" or "output-dir" or "merge-reports" or "max-sim-count") continue;
            var flag = (s != '\0' ? $"-{s}/" : "   ") + $"--{l}" + (value ? " <value>" : "");
            sb.Append($"  {flag,-40}{help}\n");
        }
        sb.Append("\nRunning tests needs the iOS Simulator, which is part of Xcode and only runs on macOS;\n" +
                  "on Windows bluepill lists tests, plans the simulator bundles and merges reports.");
        Console.WriteLine(sb.ToString());
    }

    static Dictionary<string, List<string>> Parse(string program, string[] args)
    {
        var o = new Dictionary<string, List<string>>();
        void Set(string key, string value)
        {
            if (!o.TryGetValue(key, out var list)) o[key] = list = [];
            if (!Multi.Contains(key)) list.Clear();
            list.Add(value);
        }
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            (char Short, string Long, bool Value, string Help) opt = default;
            string inline = null;
            if (arg.StartsWith("--"))
            {
                var name = arg[2..];
                var eq = name.IndexOf('=');
                if (eq >= 0) { inline = name[(eq + 1)..]; name = name[..eq]; }
                opt = Options.FirstOrDefault(x => x.Long == name);
            }
            else if (arg.Length == 2 && arg[0] == '-') opt = Options.FirstOrDefault(x => x.Short == arg[1]);
            if (opt.Long == null) throw new UsageException($"invalid option {arg} (see {program} -h)");
            if (!opt.Value) { Set(opt.Long, "true"); continue; }
            Set(opt.Long, inline ?? (++i < args.Length ? args[i] : throw new UsageException($"option {arg} needs a value")));
        }
        // A JSON config file supplies the same keys; the command line wins.
        if (o.TryGetValue("config", out var config))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(config[0]));
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (o.ContainsKey(p.Name) && !Multi.Contains(p.Name)) continue;
                if (!Options.Any(x => x.Long == p.Name)) throw new InvalidOperationException($"{config[0]}: unknown key \"{p.Name}\"");
                if (p.Value.ValueKind == JsonValueKind.Array) foreach (var v in p.Value.EnumerateArray()) Set(p.Name, JsonText(v));
                else Set(p.Name, JsonText(p.Value));
            }
        }
        return o;
    }

    static string JsonText(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => e.GetRawText(),
    };

    static string Get(Dictionary<string, List<string>> o, string key) => o.TryGetValue(key, out var v) ? v[^1] : null;

    static int RunBp(string[] args)
    {
        var o = Parse("bp", args);
        if (o.ContainsKey("help")) { Usage("bp"); return 0; }
        if (o.ContainsKey("version")) { Console.WriteLine($"bp {Commands.Version} (wintools)"); return 0; }
        if (o.ContainsKey("list-tests")) return ListTests(o);
        Console.Error.WriteLine($"[ERROR] bp cannot run tests: {IosSim.NoSimulator}");
        return 1;
    }

    static int RunBluepill(string[] args)
    {
        var o = Parse("bluepill", args);
        if (o.ContainsKey("help")) { Usage("bluepill"); return 0; }
        if (o.ContainsKey("version")) { Console.WriteLine($"bluepill {Commands.Version} (wintools)"); return 0; }
        if (Get(o, "merge-reports") is { } reports) return MergeReports(reports, Get(o, "output-dir") ?? reports);
        if (o.ContainsKey("list-tests")) return ListTests(o);

        var bundles = TestBundles(o);
        if (bundles.Count == 0) throw new UsageException("no tests to run: give --app, --test-bundle-path or --xctestrun-path (see bluepill -h)");
        var numSims = int.TryParse(Get(o, "num-sims") ?? "4", out var n) && n > 0 ? n : throw new UsageException("--num-sims needs a positive number");
        var estimates = LoadEstimates(Get(o, "test-time-estimates-json"));
        var tests = bundles.SelectMany(b => Filter(b.Tests, o, b.Skip, b.Only)).ToList();
        if (tests.Count == 0) throw new InvalidOperationException("no tests left after applying include/exclude filters");
        var packed = Pack(tests, numSims, estimates);
        Console.WriteLine($"[INFO] Packed {tests.Count} tests from {bundles.Count} test bundle(s) into {packed.Count} simulator bundle(s)" +
                          (estimates.Count > 0 ? " using time estimates" : ""));
        for (var i = 0; i < packed.Count; i++)
        {
            var time = packed[i].Sum(t => estimates.GetValueOrDefault(t, 1.0));
            Console.WriteLine($"[INFO]   Bundle {i + 1}: {packed[i].Count} tests" + (estimates.Count > 0 ? $", estimated {time.ToString("0.00", CultureInfo.InvariantCulture)}s" : ""));
        }
        Console.Error.WriteLine($"[ERROR] Cannot start {packed.Count} simulator(s) ({Get(o, "device") ?? "iPhone 8"}, {Get(o, "runtime") ?? "iOS 13.0"}): {IosSim.NoSimulator}");
        return 1;
    }

    static int ListTests(Dictionary<string, List<string>> o)
    {
        var bundles = TestBundles(o);
        if (bundles.Count == 0) throw new UsageException("no test bundle given (--app, --test-bundle-path or --xctestrun-path)");
        foreach (var b in bundles)
            foreach (var t in Filter(b.Tests, o, b.Skip, b.Only)) Console.WriteLine(t);
        return 0;
    }

    // ---- test bundles ----

    record Bundle(string Path, List<string> Tests, List<string> Skip, List<string> Only);

    static List<Bundle> TestBundles(Dictionary<string, List<string>> o)
    {
        var paths = new List<(string Path, List<string> Skip, List<string> Only)>();
        if (Get(o, "xctestrun-path") is { } xctestrun) paths.AddRange(FromXctestrun(xctestrun));
        if (Get(o, "test-bundle-path") is { } bundle) paths.Add((bundle, [], []));
        foreach (var key in new[] { "additional-unit-xctests", "additional-ui-xctests" })
            if (o.TryGetValue(key, out var extra)) paths.AddRange(extra.Select(p => (p, new List<string>(), new List<string>())));
        if (paths.Count == 0 && Get(o, "app") is { } app)
        {
            var plugins = Path.Combine(app, "PlugIns");
            if (Directory.Exists(plugins)) paths.AddRange(Directory.GetDirectories(plugins, "*.xctest").Select(p => (p, new List<string>(), new List<string>())));
        }
        return paths.Select(p => new Bundle(p.Path, ReadTests(p.Path), p.Skip, p.Only)).ToList();
    }

    // .xctestrun: one dict per test target (format 1), or TestConfigurations[].TestTargets[] (format 2).
    static IEnumerable<(string, List<string>, List<string>)> FromXctestrun(string file)
    {
        if (IosSim.ReadPlist(File.ReadAllBytes(file)) is not Dictionary<string, object> plist)
            throw new InvalidOperationException($"{file}: not an xctestrun property list");
        var root = Path.GetDirectoryName(Path.GetFullPath(file));
        var targets = new List<Dictionary<string, object>>();
        if (plist.GetValueOrDefault("TestConfigurations") is List<object> configs)
        {
            foreach (var c in configs.OfType<Dictionary<string, object>>())
                if (c.GetValueOrDefault("TestTargets") is List<object> tt) targets.AddRange(tt.OfType<Dictionary<string, object>>());
        }
        else targets.AddRange(plist.Where(kv => !kv.Key.StartsWith("__")).Select(kv => kv.Value).OfType<Dictionary<string, object>>());
        foreach (var t in targets)
        {
            if (t.GetValueOrDefault("TestBundlePath") is not string path) continue;
            var host = (t.GetValueOrDefault("TestHostPath") as string ?? "").Replace("__TESTROOT__", root);
            path = path.Replace("__TESTROOT__", root).Replace("__TESTHOST__", host);
            List<string> Strings(string key) => (t.GetValueOrDefault(key) as List<object>)?.OfType<string>().ToList() ?? [];
            yield return (path, Strings("SkipTestIdentifiers"), Strings("OnlyTestIdentifiers"));
        }
    }

    // Test methods from the bundle executable's symbol table: Objective-C `-[Class testName]` and
    // Swift @objc thunks of `func testName()` on a class.
    static List<string> ReadTests(string bundle)
    {
        if (!Directory.Exists(bundle)) throw new InvalidOperationException($"{bundle}: test bundle not found");
        var info = new[] { Path.Combine(bundle, "Info.plist"), Path.Combine(bundle, "Contents", "Info.plist") }.FirstOrDefault(File.Exists);
        var exeName = info != null && IosSim.ReadPlist(File.ReadAllBytes(info)) is Dictionary<string, object> d && d.GetValueOrDefault("CFBundleExecutable") is string e
            ? e : Path.GetFileNameWithoutExtension(bundle);
        var exe = new[] { Path.Combine(bundle, exeName), Path.Combine(bundle, "Contents", "MacOS", exeName) }.FirstOrDefault(File.Exists)
                  ?? throw new InvalidOperationException($"{bundle}: executable {exeName} not found");
        var tests = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var symbol in MachOSymbols(File.ReadAllBytes(exe)))
        {
            var objc = Regex.Match(symbol, @"^-\[(\w+) (test\w*)\]$");
            if (objc.Success) { tests.Add($"{objc.Groups[1].Value}/{objc.Groups[2].Value}"); continue; }
            if (SwiftTest(symbol) is { } swift) tests.Add(swift);
        }
        return [.. tests];
    }

    // $s<module><class>C<method>yyFTo: a Swift class's @objc thunk for `func testX()`. Handles plain
    // identifiers and the `AA` substitution (class named like its module); others are skipped.
    internal static string SwiftTest(string symbol)
    {
        var m = Regex.Match(symbol, @"^_?\$s(\d+)(.*)$");
        if (!m.Success || !symbol.EndsWith("yyFTo")) return null;
        var rest = symbol[(symbol.IndexOf("$s") + 2)..^5];
        var pos = 0;
        string Ident()
        {
            var start = pos;
            while (pos < rest.Length && char.IsDigit(rest[pos])) pos++;
            if (start == pos || rest[start] == '0') return null;
            var len = int.Parse(rest[start..pos]);
            if (pos + len > rest.Length) return null;
            var s = rest.Substring(pos, len);
            pos += len;
            return s;
        }
        var module = Ident();
        if (module == null) return null;
        string cls;
        if (rest.AsSpan(pos).StartsWith("AA")) { cls = module; pos += 2; }
        else cls = Ident();
        if (cls == null || pos >= rest.Length || rest[pos] != 'C') return null;
        pos++;
        var method = Ident();
        return method != null && pos == rest.Length && method.StartsWith("test") ? $"{cls}/{method}" : null;
    }

    // Defined external/local symbols of a (fat or thin) 64-bit Mach-O.
    static IEnumerable<string> MachOSymbols(byte[] data)
    {
        if (data.Length < 32) yield break;
        var offset = 0;
        if (BinaryPrimitives.ReadUInt32BigEndian(data) == 0xCAFEBABE)
        {
            // Fat: take the first 64-bit slice.
            var count = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4));
            for (var i = 0; i < count; i++)
            {
                var arch = data.AsSpan(8 + i * 20);
                var sliceOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(arch[8..]);
                if (sliceOffset + 4 <= data.Length && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(sliceOffset)) == 0xFEEDFACF) { offset = sliceOffset; break; }
            }
        }
        if (BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)) != 0xFEEDFACF)
            throw new InvalidOperationException("test bundle executable is not a 64-bit Mach-O");
        var ncmds = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 16));
        var cmd = offset + 32;
        for (var i = 0; i < ncmds && cmd + 8 <= data.Length; i++)
        {
            var type = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(cmd));
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(cmd + 4));
            if (type == 2)   // LC_SYMTAB
            {
                var symoff = offset + (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(cmd + 8));
                var nsyms = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(cmd + 12));
                var stroff = offset + (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(cmd + 16));
                for (var s = 0; s < nsyms; s++)
                {
                    var entry = symoff + s * 16;
                    if (entry + 16 > data.Length) yield break;
                    var strx = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(entry));
                    var ntype = data[entry + 4];
                    if ((ntype & 0xE0) != 0 || (ntype & 0x0E) != 0x0E) continue;   // stabs, undefined
                    var start = stroff + strx;
                    if (start >= data.Length) continue;
                    var end = Array.IndexOf(data, (byte)0, start);
                    if (end < 0) continue;
                    yield return Encoding.UTF8.GetString(data, start, end - start);
                }
            }
            if (size <= 0) yield break;
            cmd += size;
        }
    }

    // include/exclude take "Class" or "Class/test"; exclude wins. Skip/Only come from .xctestrun.
    static IEnumerable<string> Filter(List<string> tests, Dictionary<string, List<string>> o, List<string> skip, List<string> only)
    {
        static bool Matches(string test, string pattern) => test == pattern || test.StartsWith(pattern.TrimEnd('/') + "/");
        var include = (o.GetValueOrDefault("include") ?? []).Concat(only).ToList();
        var exclude = (o.GetValueOrDefault("exclude") ?? []).Concat(skip).ToList();
        return tests.Where(t => (include.Count == 0 || include.Any(p => Matches(t, p))) && !exclude.Any(p => Matches(t, p)));
    }

    // {"Class/test": seconds, ...}
    static Dictionary<string, double> LoadEstimates(string file)
    {
        var estimates = new Dictionary<string, double>();
        if (file == null) return estimates;
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        foreach (var p in doc.RootElement.EnumerateObject())
            if (p.Value.ValueKind == JsonValueKind.Number) estimates[p.Name.TrimEnd('(', ')')] = p.Value.GetDouble();
        return estimates;
    }

    // With estimates: longest tests first, each into the currently lightest bundle. Without: test
    // classes stay together and are dealt out so bundles get about the same number of tests.
    internal static List<List<string>> Pack(List<string> tests, int bundles, Dictionary<string, double> estimates)
    {
        bundles = Math.Min(bundles, tests.Count);
        var packed = Enumerable.Range(0, bundles).Select(_ => new List<string>()).ToList();
        if (estimates.Count > 0)
        {
            var known = estimates.Values.Count > 0 ? estimates.Values.Average() : 1.0;
            var load = new double[bundles];
            foreach (var t in tests.OrderByDescending(t => estimates.GetValueOrDefault(t, known)).ThenBy(t => t, StringComparer.Ordinal))
            {
                var i = Array.IndexOf(load, load.Min());
                packed[i].Add(t);
                load[i] += estimates.GetValueOrDefault(t, known);
            }
        }
        else
        {
            var perBundle = (int)Math.Ceiling(tests.Count / (double)bundles);
            var i = 0;
            foreach (var group in tests.GroupBy(t => t.Split('/')[0]))
                foreach (var t in group)
                {
                    if (packed[i].Count >= perBundle && i < bundles - 1) i++;
                    packed[i].Add(t);
                }
        }
        return packed.Where(b => b.Count > 0).ToList();
    }

    // Per-simulator JUnit files -> one <testsuites name="AllTestUnits"> report with summed counts.
    static int MergeReports(string dir, string outputDir)
    {
        if (!Directory.Exists(dir)) throw new InvalidOperationException($"{dir}: no such directory");
        var output = Path.Combine(outputDir, "TEST-FinalReport.xml");
        var suites = new List<XElement>();
        var files = Directory.EnumerateFiles(dir, "*.xml", SearchOption.AllDirectories)
                             .Where(f => !Path.GetFullPath(f).Equals(Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase)).Order().ToList();
        foreach (var file in files)
        {
            XDocument doc;
            try { doc = XDocument.Load(file); }
            catch (System.Xml.XmlException) { continue; }
            var root = doc.Root;
            if (root?.Name.LocalName == "testsuites") suites.AddRange(root.Elements("testsuite"));
            else if (root?.Name.LocalName == "testsuite") suites.Add(root);
        }
        if (suites.Count == 0) throw new InvalidOperationException($"no JUnit reports found in {dir}");
        double Sum(string attr) => suites.Sum(s => double.TryParse((string)s.Attribute(attr), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0);
        var merged = new XElement("testsuites",
            new XAttribute("name", "AllTestUnits"),
            new XAttribute("tests", (long)Sum("tests")),
            new XAttribute("failures", (long)Sum("failures")),
            new XAttribute("errors", (long)Sum("errors")),
            new XAttribute("time", Sum("time").ToString("0.000", CultureInfo.InvariantCulture)),
            suites.Select(s => new XElement(s)));
        Directory.CreateDirectory(outputDir);
        File.WriteAllText(output, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + merged + "\n");
        Console.WriteLine($"[INFO] Merged {suites.Count} test suite(s) from {files.Count} report(s): " +
                          $"{(long)Sum("tests")} tests, {(long)Sum("failures")} failures, {(long)Sum("errors")} errors -> {output}");
        return (long)(Sum("failures") + Sum("errors")) > 0 ? 1 : 0;
    }
}
