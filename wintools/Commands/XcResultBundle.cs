using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace WinTools;

// Reader for Xcode .xcresult bundles (shared by xcresultparser and xchtmlreport). Xcode 16 and later
// keep test results in database.sqlite3 (what `xcresulttool get test-results` reads); every bundle also
// has the object store: Info.plist names the root object and Data/data.<id> holds each object,
// zstd-compressed, in xcresulttool's typed encoding (XcNode). Code coverage reports are NSKeyedArchiver
// binary plists in the same store. Both sources load into one model: test plan configurations -> test
// bundles -> suites -> tests, with repetitions and repeated actions merged per test like xcresulttool does.
sealed class XcResultBundle
{
    public readonly string Path;
    public readonly string RootId;

    XcResultBundle(string path, string rootId) { Path = path; RootId = rootId; }

    public static XcResultBundle Open(string path)
    {
        var full = System.IO.Path.GetFullPath(path.TrimEnd('/', '\\'));
        if (!Directory.Exists(full)) throw new IOException($"{path}: no such .xcresult bundle");
        var info = System.IO.Path.Combine(full, "Info.plist");
        if (!File.Exists(info)) throw new IOException($"{path}: not an .xcresult bundle (no Info.plist)");
        var plist = XcResultPlist.Read(File.ReadAllBytes(info)) as Dictionary<string, object>;
        var root = (plist?.GetValueOrDefault("rootId") as Dictionary<string, object>)?.GetValueOrDefault("hash") as string;
        return new XcResultBundle(full, root);
    }

    string Database => System.IO.Path.Combine(Path, "database.sqlite3");

    public bool HasObject(string id) => id != null && File.Exists(System.IO.Path.Combine(Path, "Data", "data." + id));

    public byte[] Raw(string id)
    {
        var file = System.IO.Path.Combine(Path, "Data", "data." + id);
        if (!File.Exists(file)) throw new IOException($"object {id} is missing from the bundle");
        var data = File.ReadAllBytes(file);
        return data.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(data) == 0xFD2FB528 ? XcResultZstd.Decompress(data) : data;
    }

    public XcNode Get(string id) => id == null ? null : XcNode.Parse(Raw(id));

    public XcNode Root => HasObject(RootId) ? Get(RootId) : null;

    public XcrResult LoadResult()
    {
        if (File.Exists(Database))
        {
            using var db = new XcSqlite(Database);
            if (db.HasTable("TestCaseRuns")) return FromDatabase(db);
        }
        var root = Root ?? throw new IOException($"{Path}: the bundle has neither a results database nor its root object");
        return FromObjects(root);
    }

    // ---- database.sqlite3 (Xcode 16+) ----

    static readonly DateTime AppleEpoch = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    static DateTimeOffset? AppleDate(object v) => v is double d ? new DateTimeOffset(AppleEpoch.AddSeconds(d)) : v is long l ? new DateTimeOffset(AppleEpoch.AddSeconds(l)) : null;
    static long Id(object v) => v is long l ? l : -1;
    static string S(object v) => v as string;
    static double D(object v) => v is double d ? d : v is long l ? l : 0;

    XcrResult FromDatabase(XcSqlite db)
    {
        var result = new XcrResult();
        var builder = new XcrBuilder();
        var actions = db.Query("SELECT a.rowid AS id, a.name, a.started, a.finished, d.name AS destination, i.scheme FROM Actions a " +
                               "LEFT JOIN RunDestinations d ON d.rowid = a.runDestination_fk LEFT JOIN Invocations i ON i.rowid = a.invocation_fk ORDER BY a.orderInInvocation, a.rowid");
        if (actions.Count > 0)
        {
            var first = actions.FirstOrDefault(a => db.Query($"SELECT rowid FROM TestPlanRuns WHERE action_fk = {Id(a["id"])}").Count > 0) ?? actions[0];
            result.Start = AppleDate(first["started"]);
            result.End = AppleDate(first["finished"]);
            result.DeviceName = S(first["destination"]);
            result.Title = $"{S(first["name"]) ?? "Test"} - {S(first["scheme"])}";
        }
        var configurations = db.Query("SELECT rowid AS id, name FROM TestPlanConfigurations ORDER BY rowid").ToDictionary(r => Id(r["id"]), r => S(r["name"]));
        var testables = db.Query("SELECT rowid AS id, name FROM Testables").ToDictionary(r => Id(r["id"]), r => S(r["name"]));
        var suites = db.Query("SELECT rowid AS id, name, identifier FROM TestSuites").ToDictionary(r => Id(r["id"]));
        var suiteRuns = db.Query("SELECT rowid AS id, testSuite_fk AS suite, parentSuiteRun_fk AS parent, duration FROM TestSuiteRuns").ToDictionary(r => Id(r["id"]));
        var arguments = db.Query("SELECT a.testCaseRun_fk AS run, v.description FROM Arguments a JOIN TestValues v ON v.rowid = a.testValue_fk ORDER BY a.testCaseRun_fk, a.orderInTestCaseRun")
            .GroupBy(r => Id(r["run"])).ToDictionary(g => g.Key, g => string.Join(", ", g.Select(r => S(r["description"]))));
        var issues = db.Query("SELECT ti.testCaseRun_fk AS run, ti.compactDescription AS compact, ti.sanitizedDescription AS sanitized, ti.issueType AS type, " +
                              "l.filePath AS file, l.lineNumber AS line FROM TestIssues ti LEFT JOIN SourceCodeContexts c ON c.rowid = ti.sourceCodeContext_fk " +
                              "LEFT JOIN SourceCodeLocations l ON l.rowid = c.location_fk ORDER BY ti.rowid")
            .Where(r => r["run"] != null).GroupBy(r => Id(r["run"])).ToDictionary(g => g.Key, g => g.ToList());
        var runs = db.Query("SELECT r.rowid AS id, r.result, r.duration, tc.rowid AS tc, tc.name, tc.identifier, tc.orderInTestSuite AS ord, sr.rowid AS suiteRun, " +
                            "sr.testSuite_fk AS suite, tr.testable_fk AS testable, pr.configuration_fk AS config, pr.action_fk AS action FROM TestCaseRuns r " +
                            "JOIN TestCases tc ON tc.rowid = r.testCase_fk JOIN TestSuiteRuns sr ON sr.rowid = r.testSuiteRun_fk " +
                            "JOIN TestableRuns tr ON tr.rowid = sr.testableRun_fk JOIN TestPlanRuns pr ON pr.rowid = tr.testPlanRun_fk ORDER BY pr.action_fk, r.rowid");
        foreach (var r in runs)
        {
            var config = configurations.GetValueOrDefault(Id(r["config"])) ?? result.Title;
            var testable = testables.GetValueOrDefault(Id(r["testable"])) ?? "Tests";
            // Suite chain from the outermost suite down.
            var chain = new List<(string Name, string Identifier, double? Duration)>();
            for (var s = Id(r["suiteRun"]); s >= 0 && suiteRuns.TryGetValue(s, out var suiteRun); s = Id(suiteRun["parent"]))
                if (suites.TryGetValue(Id(suiteRun["suite"]), out var suite))
                    chain.Insert(0, (S(suite["name"]), S(suite["identifier"]), D(suiteRun["duration"])));
            var name = S(r["name"]);
            var identifier = S(r["identifier"]) ?? (chain.Count > 0 ? chain[^1].Name + "/" + name : name);
            if (arguments.TryGetValue(Id(r["id"]), out var args))
            {
                name = XcrBuilder.WithArguments(name, args);
                identifier = XcrBuilder.WithArguments(identifier, args);
            }
            var failures = (issues.GetValueOrDefault(Id(r["id"])) ?? []).Select(i => new XcrFailure
            {
                Message = S(i["compact"]) ?? S(i["sanitized"]) ?? "",
                File = S(i["file"]),
                Line = (int)Id(i["line"]),
                TestIdentifier = identifier,
                TestName = name,
                TargetName = testable,
            }).ToList();
            builder.Add(config, testable, chain, name, identifier, (int)Id(r["ord"]),
                        Id(r["action"]) + ":" + Id(r["suiteRun"]), S(r["result"]), D(r["duration"]), failures);
        }
        builder.Finish(result);

        foreach (var b in db.Query("SELECT b.issueType AS type, b.message, b.producingTarget AS target, b.severity, d.url FROM BuildIssues b " +
                                   "LEFT JOIN DocumentLocations d ON d.rowid = b.documentLocation_fk ORDER BY b.action_fk, b.orderInAction"))
        {
            var issue = new XcrIssue { Type = S(b["type"]) ?? "", Message = S(b["message"]) ?? "", Url = S(b["url"]), ProducingTarget = S(b["target"]) };
            if (issue.Type.Contains("Analyzer", StringComparison.OrdinalIgnoreCase)) result.AnalyzerIssues.Add(issue);
            else if (Id(b["severity"]) == 0) result.ErrorIssues.Add(issue);
            else result.WarningIssues.Add(issue);
        }
        result.Errors = result.ErrorIssues.Count;
        result.Warnings = result.WarningIssues.Count;
        result.AnalyzerWarnings = result.AnalyzerIssues.Count;
        return result;
    }

    // ---- object store (every Xcode version) ----

    // ActionsInvocationRecord -> actions -> actionResult.testsRef (ActionTestPlanRunSummaries) -> summaries
    // (one per configuration) -> testableSummaries -> tests (ActionTestSummaryGroup / ActionTestMetadata).
    XcrResult FromObjects(XcNode root)
    {
        var result = new XcrResult();
        var builder = new XcrBuilder();
        var issues = root["issues"];
        result.ErrorIssues.AddRange(Issues(issues, "errorSummaries"));
        result.WarningIssues.AddRange(Issues(issues, "warningSummaries"));
        result.AnalyzerIssues.AddRange(Issues(issues, "analyzerWarningSummaries"));
        var metrics = root["metrics"];
        result.Errors = metrics?.Int("errorCount") ?? result.ErrorIssues.Count;
        result.Warnings = metrics?.Int("warningCount") ?? result.WarningIssues.Count;
        result.AnalyzerWarnings = metrics?.Int("analyzerWarningCount") ?? result.AnalyzerIssues.Count;
        var actionIndex = 0;
        foreach (var action in root.Array("actions"))
        {
            actionIndex++;
            var actionResult = action["actionResult"];
            var testsRef = actionResult?.RefId("testsRef");
            if (testsRef == null) continue;
            if (result.Start == null)
            {
                result.Start = action.Date("startedTime");
                result.End = action.Date("endedTime");
                result.DeviceName = action["runDestination"]?.Str("displayName");
                result.Title = action.Str("title") ?? action.Str("schemeCommandName") ?? "Test";
            }
            var failures = Issues(actionResult["issues"], "testFailureSummaries").ToList();
            foreach (var plan in Get(testsRef).Array("summaries"))
                foreach (var testable in plan.Array("testableSummaries"))
                {
                    var testableName = testable.Str("name") ?? testable.Str("targetName") ?? "Tests";
                    var runKey = actionIndex + ":" + testableName;
                    foreach (var item in testable.Array("tests"))
                        Walk(builder, plan.Str("name") ?? result.Title, testableName, [], item, runKey, failures);
                }
        }
        builder.Finish(result);
        return result;
    }

    static void Walk(XcrBuilder builder, string config, string testable, List<(string Name, string Identifier, double? Duration)> chain, XcNode item, string runKey, List<XcrIssue> failures)
    {
        if (item.Is("ActionTestMetadata") || item.Is("ActionTestSummary"))
        {
            var identifier = item.Str("identifier") ?? item.Str("name");
            var name = item.Str("name");
            var dotted = identifier.Replace('/', '.');
            var list = failures.Where(f => f.TestCaseName == dotted || f.TestCaseName == $"-[{identifier.Replace('/', ' ')}]").Select(f =>
            {
                var loc = f.Location(1);
                return new XcrFailure { Message = f.Message, File = loc?.Path, Line = loc?.StartLine ?? 0, TestIdentifier = identifier, TestName = name, TargetName = testable };
            }).ToList();
            builder.Add(config, testable, chain, name, identifier, int.MaxValue, runKey, item.Str("testStatus") ?? "Success", item.Num("duration") ?? 0, list);
            return;
        }
        var groupName = item.Str("name") ?? "";
        // "All tests"/"Selected tests" and the test bundle itself are not suites.
        var wrapper = groupName.EndsWith(".xctest") || item.Array("subtests").Any(s => (s.Str("name") ?? "").EndsWith(".xctest"));
        var next = wrapper ? chain : [.. chain, (groupName, item.Str("identifier"), item.Num("duration"))];
        foreach (var sub in item.Array("subtests")) Walk(builder, config, testable, next, sub, runKey, failures);
    }

    static List<XcrIssue> Issues(XcNode issues, string key) =>
        issues == null ? [] : issues.Array(key).Select(i => new XcrIssue
        {
            Type = i.Str("issueType") ?? "",
            Message = i.Str("message") ?? "",
            Url = i["documentLocationInCreatingWorkspace"]?.Str("url"),
            TestCaseName = i.Str("testCaseName"),
            ProducingTarget = i.Str("producingTarget"),
        }).ToList();

    // Code coverage: actionResult.coverage.reportRef is an archived IDESchemeActionCodeCoverage.
    public XcrCoverage LoadCoverage()
    {
        var root = Root;
        if (root == null) return null;
        var reportRef = root.Array("actions").Select(a => a["actionResult"]?["coverage"]?.RefId("reportRef")).FirstOrDefault(r => r != null);
        if (reportRef == null || !HasObject(reportRef)) return null;
        if (XcResultPlist.Unarchive(Raw(reportRef)) is not Dictionary<string, object> report) return null;
        var coverage = new XcrCoverage { Covered = Long(report, "coveredLines"), Executable = Long(report, "executableLines") };
        foreach (var t in List(report, "buildableCoverageObjects"))
        {
            var name = t.GetValueOrDefault("name") as string ?? "";
            var product = t.GetValueOrDefault("productPath") as string ?? "";
            if (product.Contains("/" + name + ".xctest/")) name += ".xctest";
            var target = new XcrCoverageTarget { Name = name, Covered = Long(t, "coveredLines"), Executable = Long(t, "executableLines") };
            foreach (var f in List(t, "sourceFiles"))
            {
                var file = new XcrCoverageFile
                {
                    Name = f.GetValueOrDefault("name") as string ?? "",
                    Path = f.GetValueOrDefault("documentLocation") as string ?? "",
                    Covered = Long(f, "coveredLines"),
                    Executable = Long(f, "executableLines"),
                };
                // Functions are packed: count, count x (covered, executable, line, executable, executions, 0)
                // as little-endian uint32s, then the names, NUL-terminated, in the same order.
                if (f.GetValueOrDefault("functions") is byte[] blob && blob.Length >= 4)
                {
                    var count = BitConverter.ToInt32(blob, 0);
                    var names = 4 + 24 * count;
                    for (var i = 0; i < count && names <= blob.Length; i++)
                    {
                        var at = 4 + 24 * i;
                        var end = Array.IndexOf(blob, (byte)0, names);
                        if (end < 0) end = blob.Length;
                        file.Functions.Add(new XcrCoverageFunction
                        {
                            Covered = BitConverter.ToInt32(blob, at),
                            Executable = BitConverter.ToInt32(blob, at + 4),
                            Line = BitConverter.ToInt32(blob, at + 8),
                            Executions = BitConverter.ToInt32(blob, at + 16),
                            Name = Encoding.UTF8.GetString(blob, names, end - names),
                        });
                        names = end + 1;
                    }
                }
                target.Files.Add(file);
            }
            coverage.Targets.Add(target);
        }
        return coverage;
    }

    static long Long(Dictionary<string, object> d, string key) => d.GetValueOrDefault(key) switch { long l => l, double v => (long)v, _ => 0 };
    static IEnumerable<Dictionary<string, object>> List(Dictionary<string, object> d, string key) =>
        (d.GetValueOrDefault(key) as List<object> ?? []).OfType<Dictionary<string, object>>();
}

// Collects test runs (from either source) into the merged tree.
sealed class XcrBuilder
{
    sealed class Entry
    {
        public XcrTest Test;
        public readonly Dictionary<string, List<(string Status, double Duration)>> Runs = [];
    }

    readonly List<XcrRun> runs = [];
    readonly Dictionary<string, Entry> tests = [];
    readonly List<XcrFailure> sessionFailures = [];

    static bool SessionLevel(string identifier) => identifier == "Issues recorded without an associated test or suite" || identifier.Contains("«unknown»");

    public void Add(string config, string testable, List<(string Name, string Identifier, double? Duration)> chain, string name, string identifier, int order,
                    string runKey, string status, double duration, List<XcrFailure> failures)
    {
        if (SessionLevel(identifier ?? "") || SessionLevel(name ?? ""))
        {
            foreach (var f in failures) { f.SessionLevel = true; f.TestName = name; sessionFailures.Add(f); }
            return;
        }
        var key = $"{config}\u0001{testable}\u0001{string.Join("\u0001", chain.Select(c => c.Name))}\u0001{identifier}";
        if (!tests.TryGetValue(key, out var entry))
        {
            var run = runs.FirstOrDefault(r => r.Name == config);
            if (run == null) runs.Add(run = new XcrRun { Name = config });
            var bundleName = testable.EndsWith(".xctest") ? testable : testable + ".xctest";
            var group = run.Bundles.FirstOrDefault(b => b.Name == bundleName);
            if (group == null) run.Bundles.Add(group = new XcrGroup { Name = bundleName, Identifier = bundleName });
            foreach (var (suiteName, suiteId, suiteDuration) in chain)
            {
                var sub = group.Groups.FirstOrDefault(g => g.Name == suiteName);
                if (sub == null) group.Groups.Add(sub = new XcrGroup { Name = suiteName, Identifier = suiteId ?? suiteName, OwnDuration = suiteDuration });
                group = sub;
            }
            entry = new Entry { Test = new XcrTest { Name = name, Identifier = identifier, Order = order } };
            group.Tests.Add(entry.Test);
            tests[key] = entry;
        }
        if (!entry.Runs.TryGetValue(runKey, out var list)) entry.Runs[runKey] = list = [];
        list.Add((status, duration));
        foreach (var f in failures)
            if (!entry.Test.Failures.Any(e => e.Message == f.Message && e.Line == f.Line)) entry.Test.Failures.Add(f);
    }

    // Repetitions inside one run add up; separate runs (repeated actions, merged bundles) average out.
    // A test is flaky when its repetitions both passed and failed.
    public void Finish(XcrResult result)
    {
        foreach (var entry in tests.Values)
        {
            var t = entry.Test;
            var all = entry.Runs.Values.SelectMany(r => r).ToList();
            t.Duration = entry.Runs.Values.Average(r => r.Sum(x => x.Duration));
            t.Repetitions = all.Count;
            t.Flaky = all.Any(r => r.Status == "Failure") && all.Any(r => r.Status == "Success");
            var last = entry.Runs.Values.Select(r => r[^1].Status).ToList();
            t.Status = last.Contains("Failure") ? "Failure" : last.Distinct().Count() == 1 ? last[0] : last.FirstOrDefault(s => s != "Success") ?? "Success";
        }
        foreach (var run in runs) foreach (var bundle in run.Bundles) Sort(bundle);
        result.Runs.AddRange(runs);
        var all2 = result.AllTests().ToList();
        var sessionTests = sessionFailures.Select(f => f.TestName).Distinct().Count();
        result.TestsCount = all2.Count + sessionTests;
        result.TestsFailed = all2.Count(t => t.Failed) + sessionTests;
        result.TestsSkipped = all2.Count(t => t.Skipped);
        foreach (var t in all2.Where(t => t.Failed))
            result.Failures.Add(t.Failures.FirstOrDefault() ?? new XcrFailure { TestIdentifier = t.Identifier, TestName = t.Name, Message = "" });
        result.Failures.AddRange(sessionFailures);
    }

    static void Sort(XcrGroup group)
    {
        var ordered = group.Tests.OrderBy(t => t.Order).ToList();
        group.Tests.Clear();
        group.Tests.AddRange(ordered);
        foreach (var g in group.Groups) Sort(g);
    }

    // Swift Testing parameterized tests: "test(a:b:)" with arguments "1, 2" -> "test(a: 1, b: 2)".
    public static string WithArguments(string name, string values)
    {
        int open = name.IndexOf('('), close = name.LastIndexOf(')');
        if (open < 0 || close < open) return $"{name} [{values}]";
        var labels = new List<string>();
        var token = new StringBuilder();
        foreach (var c in name[(open + 1)..close])
        {
            if (c == ',') { token.Clear(); continue; }
            if (c == ':')
            {
                var label = token.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                if (!string.IsNullOrEmpty(label)) labels.Add(label);
                token.Clear();
                continue;
            }
            token.Append(c);
        }
        if (labels.Count == 0) return $"{name} [{values}]";
        var split = SplitValues(values);
        string body;
        if (labels.Count == split.Count) body = string.Join(", ", labels.Zip(split, (l, v) => $"{l}: {v}"));
        else if (labels.Count == 1) body = $"{labels[0]}: {values.Trim()}";
        else return $"{name} [{values}]";
        return $"{name[..open]}({body})";
    }

    static List<string> SplitValues(string raw)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        foreach (var c in raw)
        {
            if (c == '"') quoted = !quoted;
            if (c == ',' && !quoted)
            {
                if (current.ToString().Trim() is { Length: > 0 } v) values.Add(v);
                current.Clear();
                continue;
            }
            current.Append(c);
        }
        if (current.ToString().Trim() is { Length: > 0 } last) values.Add(last);
        return values;
    }
}

sealed class XcrResult
{
    public int TestsCount, TestsFailed, TestsSkipped, Warnings, Errors, AnalyzerWarnings;
    public DateTimeOffset? Start, End;
    public string DeviceName, Title;
    public readonly List<XcrRun> Runs = [];
    public readonly List<XcrFailure> Failures = [];
    public readonly List<XcrIssue> ErrorIssues = [], WarningIssues = [], AnalyzerIssues = [];

    public IEnumerable<XcrTest> AllTests() => Runs.SelectMany(r => r.Bundles).SelectMany(g => g.AllTests());
}

sealed class XcrRun
{
    public string Name;
    public readonly List<XcrGroup> Bundles = [];
}

sealed class XcrGroup
{
    public string Name, Identifier;
    public double? OwnDuration;   // the suite's recorded duration (first run), when the source has one
    public readonly List<XcrGroup> Groups = [];
    public readonly List<XcrTest> Tests = [];

    public IEnumerable<XcrTest> AllTests() => Tests.Concat(Groups.SelectMany(g => g.AllTests()));
    public bool HasFailures => AllTests().Any(t => t.Failed);
    public double Duration => AllTests().Sum(t => t.Duration ?? 0);
    public double DisplayDuration => OwnDuration ?? Duration;
}

sealed class XcrTest
{
    public string Name, Identifier, Status;
    public double? Duration;
    public bool Flaky;
    public int Order, Repetitions;
    public readonly List<XcrFailure> Failures = [];
    public bool Failed => Status == "Failure";
    public bool Skipped => Status == "Skipped";
    public bool ExpectedFailure => Status == "Expected Failure";
}

sealed class XcrFailure
{
    public string Message, File, TestIdentifier, TestName, TargetName;
    public int Line;
    public bool SessionLevel;
    public string Location => File == null ? null : $"{System.IO.Path.GetFileName(File)}:{Line}";
}

// A build issue or test failure summary from the object store.
sealed class XcrIssue
{
    public string Type, Message, Url, TestCaseName, ProducingTarget;

    // "file:///path/File.swift#...&StartingLineNumber=108...": test failures count lines from zero (shown
    // as 109), build issues are reported with the numbers as stored.
    public (string Path, int StartLine, int EndLine, int StartColumn, int EndColumn)? Location(int lineOffset = 0)
    {
        if (string.IsNullOrEmpty(Url)) return null;
        var hash = Url.IndexOf('#');
        var path = Uri.UnescapeDataString(hash < 0 ? Url : Url[..hash]);
        if (path.StartsWith("file://")) path = path[7..];
        var query = hash < 0 ? "" : Url[(hash + 1)..];
        int Get(string key, int offset)
        {
            foreach (var part in query.Split('&'))
                if (part.StartsWith(key + "=") && int.TryParse(part[(key.Length + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v + offset;
            return -1;
        }
        var startLine = Get("StartingLineNumber", lineOffset);
        var endLine = Get("EndingLineNumber", lineOffset);
        return (path, Math.Max(startLine, 0), endLine < 0 ? Math.Max(startLine, 0) : endLine,
                Math.Max(Get("StartingColumnNumber", 0), 0), Math.Max(Get("EndingColumnNumber", 0), 0));
    }
}

sealed class XcrCoverage
{
    public long Covered, Executable;
    public readonly List<XcrCoverageTarget> Targets = [];
}

sealed class XcrCoverageTarget
{
    public string Name;
    public long Covered, Executable;
    public readonly List<XcrCoverageFile> Files = [];
}

sealed class XcrCoverageFile
{
    public string Name, Path;
    public long Covered, Executable;
    public readonly List<XcrCoverageFunction> Functions = [];
}

sealed class XcrCoverageFunction
{
    public string Name;
    public int Line, Covered, Executable, Executions;
}
