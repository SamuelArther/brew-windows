using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace WinTools;

// xcresultparser: summarize Xcode .xcresult bundles (test results, build issues, code coverage) as
// text, colored cli output, HTML, Markdown, JUnit or Sonar XML, or Code Climate JSON. On macOS it calls
// xcrun xcresulttool/xccov; here the bundle is read directly (XcResultBundle). Line-by-line coverage
// (cobertura, sonar coverage) lives in the separate .xccovarchive format, which is not decoded here.
static class Xcresultparser
{
    const string Version = "2.2.0";

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("xcresultparser", "Parse .xcresult bundles from Xcode builds and test runs", Run);

    sealed class Options
    {
        public string CoverageReportFormat = "methods", OutputFormat, ProjectRoot = "", SummaryFields, Bundle;
        public readonly List<string> CoverageTargets = [], ExcludedPaths = [];
        public bool Coverage, Strict, NoTestResult, FailedOnly, Quiet, TargetInfo;
    }

    const string Help = """
        OVERVIEW: xcresultparser 2.2.0
        Interpret binary .xcresult files and print summary in different formats: txt,
        xml, html or colored cli output.

        USAGE: xcresultparser [<options>] [<xcresult-file>]

        ARGUMENTS:
          <xcresult-file>         The path to the .xcresult file.

        OPTIONS:
          --coverage-report-format <coverage-report-format>
                                  The coverage report format. The Default is 'methods', It
                                  can either be 'totals', 'targets', 'classes' or 'methods'
          -o, --output-format <output-format>
                                  The output format. It can be either 'txt', 'cli',
                                  'html', 'md', 'github', 'xml', 'junit', 'cobertura',
                                  'warnings', 'errors' and 'warnings-and-errors'.
          -p, --project-root <project-root>
                                  The name of the project root. If present paths and urls
                                  are relative to the specified directory.
          -t, --coverage-targets <coverage-targets>
                                  Specify which targets to calculate coverage from.
          -e, --excluded-path <excluded-path>
                                  Specify which path names to exclude.
          -s, --summary-fields <summary-fields>
                                  The fields in the summary. Default is all:
                                  errors|warnings|analyzerWarnings|tests|failed|skipped|duration|date
          -c, --coverage          Whether to print coverage data.
          -x, --exclude-coverage-not-in-project
                                  Omit elements with file pathes, which do not contain
                                  'projectRoot'.
          -n, --no-test-result    Whether to print test results.
          -f, --failed-tests-only Whether to only print failed tests.
          -q, --quiet             Quiet. Don't print status output.
          -i, --target-info       Just print the targets contained in the xcresult.
          -v, --version           Show version number.
          -h, --help              Show help information.
        """;

    static int Run(string[] args)
    {
        var o = new Options();
        string Value(string[] a, ref int i, string name, string inline)
        {
            if (inline != null) return inline;
            if (i + 1 >= a.Length) throw new UsageException($"Missing value for '{name}'");
            return a[++i];
        }
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string inline = null;
            if (arg.StartsWith("--") && arg.Contains('=')) { inline = arg[(arg.IndexOf('=') + 1)..]; arg = arg[..arg.IndexOf('=')]; }
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "-v" or "--version": Console.WriteLine(Version); return 0;
                case "--coverage-report-format": o.CoverageReportFormat = Value(args, ref i, arg, inline).ToLowerInvariant(); break;
                case "-o" or "--output-format": o.OutputFormat = Value(args, ref i, arg, inline).ToLowerInvariant(); break;
                case "-p" or "--project-root": o.ProjectRoot = Value(args, ref i, arg, inline); break;
                case "-t" or "--coverage-targets": o.CoverageTargets.Add(Value(args, ref i, arg, inline)); break;
                case "-e" or "--excluded-path": o.ExcludedPaths.Add(Value(args, ref i, arg, inline)); break;
                case "-s" or "--summary-fields": o.SummaryFields = Value(args, ref i, arg, inline); break;
                case "-c" or "--coverage": o.Coverage = true; break;
                case "-x" or "--exclude-coverage-not-in-project": o.Strict = true; break;
                case "-n" or "--no-test-result": o.NoTestResult = true; break;
                case "-f" or "--failed-tests-only": o.FailedOnly = true; break;
                case "-q" or "--quiet": o.Quiet = true; break;
                case "-i" or "--target-info": o.TargetInfo = true; break;
                default:
                    if (arg.Length > 2 && arg[0] == '-' && arg[1] != '-' && arg[1..].All(c => "cxnfqiv".Contains(c)))
                    {
                        foreach (var c in arg[1..])
                        {
                            switch (c)
                            {
                                case 'c': o.Coverage = true; break;
                                case 'x': o.Strict = true; break;
                                case 'n': o.NoTestResult = true; break;
                                case 'f': o.FailedOnly = true; break;
                                case 'q': o.Quiet = true; break;
                                case 'i': o.TargetInfo = true; break;
                                case 'v': Console.WriteLine(Version); return 0;
                            }
                        }
                        break;
                    }
                    if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"Unknown option '{arg}'");
                    if (o.Bundle != null) throw new UsageException($"Unexpected argument '{arg}'");
                    o.Bundle = arg;
                    break;
            }
        }
        if (string.IsNullOrEmpty(o.Bundle)) throw new UsageException("Missing expected argument '<xcresult-file>' (see xcresultparser --help)");
        var bundle = XcResultBundle.Open(o.Bundle);

        if (o.TargetInfo)
        {
            var coverage = bundle.LoadCoverage();
            Write(string.Concat((coverage?.Targets ?? []).Select(t => "\n" + t.Name)));
            return 0;
        }
        var format = o.OutputFormat switch
        {
            "txt" or "cli" or "html" or "xml" or "junit" or "cobertura" or "md" or "github" or "warnings" or "errors" or "warnings-and-errors" => o.OutputFormat,
            _ => "cli",
        };
        switch (format)
        {
            case "xml" when o.Coverage:
            case "cobertura":
                Console.Error.WriteLine("xcresultparser: line-by-line coverage (cobertura, sonar coverage) comes from the bundle's .xccovarchive, which this Windows port does not decode; use -o txt|cli|html|md -c for per-file and per-function coverage");
                return 1;
            case "xml": Write(JunitXml(bundle.LoadResult(), o, sonar: true)); return 0;
            case "junit": Write(JunitXml(bundle.LoadResult(), o, sonar: false)); return 0;
            case "warnings" or "errors" or "warnings-and-errors": Write(IssuesJson(bundle, format, o)); return 0;
        }

        var result = bundle.LoadResult();
        var cov = bundle.LoadCoverage();
        var available = cov?.Targets.Select(t => t.Name).ToList() ?? [];
        var selected = SelectTargets(o.CoverageTargets, available);
        var fmt = new Formatter(format);
        var report = new Report(result, cov, selected, fmt, o);
        WriteLn(fmt.DocumentPrefix("XCResults"));
        WriteLn(report.Summary());
        if (!o.NoTestResult)
        {
            WriteLn(fmt.Divider);
            WriteLn(report.TestDetails());
        }
        if (o.Coverage) WriteLn(report.CoverageDetails());
        WriteLn(fmt.DocumentSuffix);
        return 0;
    }

    // Upstream prints through writeToStdOut (adds "\n") and writeToStdOutLn (adds another).
    static readonly StreamWriter Stdout = new(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
    static void Write(string s) => Stdout.Write(s + "\n");
    static void WriteLn(string s) => Write(s + "\n");

    static HashSet<string> SelectTargets(List<string> filters, List<string> available)
    {
        if (filters.Count == 0) return [.. available];
        static string Stripped(string t) => t.Split('.')[0];
        var unmatched = filters.Where(f => !available.Contains(f) && !available.Select(Stripped).Contains(f)).ToList();
        if (unmatched.Count > 0)
            throw new InvalidOperationException($"Unknown coverage target(s): {string.Join(", ", unmatched.Order())}. Available targets: {string.Join(", ", available.Order())}");
        return [.. available.Where(t => filters.Contains(t) || filters.Contains(Stripped(t)))];
    }

    internal static string Num(double v, int digits) => Math.Round(v, digits, MidpointRounding.ToEven).ToString("0." + new string('#', digits), CultureInfo.InvariantCulture);

    // The per-format pieces (TextResultFormatter, CLIResultFormatter, HTMLResultFormatter, MDResultFormatter).
    sealed class Formatter(string format)
    {
        const string Reset = "\u001B[0m", Red = "\u001B[31m", Green = "\u001B[32m", Yellow = "\u001B[33m", Bold = "\u001B[1m";
        const string Indent = "  ";
        readonly bool github = format == "github";
        public readonly bool Markdown = format is "md" or "github";
        public readonly bool Cli = format == "cli";
        readonly bool html = format == "html";

        static string Rep(int n) => string.Concat(Enumerable.Repeat(Indent, n));
        static string Esc(string s) => s.Replace("\"", "\\\"");
        static string H(string s) => System.Net.WebUtility.HtmlEncode(s).Replace("&#39;", "'");

        public string DocumentPrefix(string title) => html ? HtmlStart.Replace("{TITLE}", H(title)) : "";
        public string DocumentSuffix => html ? HtmlEnd : "";
        public string AccordionOpen => html ? "<div class=\"panel\">" : "";
        public string AccordionClose => html ? "</div>" : "";
        public string TableOpen => html ? "<table>" : "";
        public string TableClose => html ? "</table>" : "";
        public string Divider => format switch { "cli" => "-----------------\n", "html" => "<hr>", "md" or "github" => "\n---------------------\n", _ => "---------------------\n" };

        public string FailIcon => Markdown ? (github ? "🔴 " : "🔴&nbsp;&nbsp;") : "✖︎";
        public string PassIcon => Markdown ? (github ? "🟢 " : "🟢&nbsp;&nbsp;") : "✓";
        public string SkipIcon => Markdown ? (github ? "⚪️ " : "⚪️&nbsp;&nbsp;") : "-";
        public string ExpectedFailureIcon => Markdown ? (github ? "🟡 " : "🟡&nbsp;&nbsp;") : "!";
        public string FlakyIcon => Markdown ? (github ? "🟠 " : "🟠&nbsp;&nbsp;") : "⚠︎";

        public string SummaryLine(string item, bool failed) => format switch
        {
            "cli" => (failed ? Red : "") + Indent + item + Reset,
            "html" => $"<p class=\"{(failed ? "resultSummaryLineFailed" : "resultSummaryLineSuccess")}\">{H(item)}</p>",
            "md" or "github" => "* " + Esc(item),
            _ => Indent + item,
        };

        public string SummaryLineWarning(string item, bool warn) => format switch
        {
            "cli" => (warn ? Yellow : "") + Indent + item + Reset,
            "html" => $"<p class=\"{(warn ? "resultSummaryLineWarning" : "resultSummaryLineSuccess")}\">{H(item)}</p>",
            "md" or "github" => "* " + Esc(item),
            _ => Indent + item,
        };

        public string Configuration(string item) => format switch
        {
            "cli" => Bold + item + Reset,
            "html" => $"<h2>{H(item)}</h2>",
            "md" or "github" => "",
            _ => item,
        };

        public string Target(string item, bool failed) => format switch
        {
            "cli" => (failed ? Red : "") + Indent + item + Reset,
            "html" => $"<p class=\"{(failed ? "testTargetFailed" : "testTargetSuccess")}\">{H(item)}</p>",
            "md" or "github" => "### " + Esc(item),
            _ => Indent + item,
        };

        public string Class(string item, bool failed) => format switch
        {
            "cli" => (failed ? Red : "") + Rep(2) + item + Reset,
            "html" => $"<button class=\"accordion\"><span class=\"{(failed ? "testClassFailed" : "testClassSuccess")}\">{H(item)}</span></button>",
            "md" or "github" => "### " + Esc(item),
            _ => Rep(2) + item,
        };

        public string Test(string item, bool failed) => format switch
        {
            "cli" => (failed ? Red : Green) + Rep(3) + item + Reset,
            "html" => $"<p class=\"{(failed ? "singleTestItemFailed" : "singleTestItemSuccess")}\">{H(item)}</p>",
            "md" => $"* <span style=\\\"color:{(failed ? "red" : "green")}\\\">" + Esc(item) + "</span>",
            "github" => $"$\\textcolor{{{(failed ? "red" : "green")}}}{{\\text{{{Esc(item)}}}}}$",
            _ => Rep(3) + item,
        };

        public string FailedTest(string item, string message) => format switch
        {
            "cli" => Red + Rep(3) + item + "\n" + Rep(4) + message + Reset,
            "html" => $"<button class=\"accordion\"><span class=\"singleTestItemFailedWithMessage\">{H(item)}</span></button>\n<div class=\"panel\"><p class=\"singleTestItemFailedMessage\">{H(message)}</p></div>",
            "md" => "* <span style=\\\"color:red\\\">" + Esc(item) + "</span><br />\n  * <span style=\\\"color:gray\\\">" + Esc(message) + "</span>",
            "github" => $"* $\\textcolor{{red}}{{\\text{{{Esc(item)}}}}}$\n  * $\\textcolor{{gray}}{{\\text{{{Esc(message)}",
            _ => Rep(3) + item + "\n" + Rep(4) + message,
        };

        public string CoverageTarget(string item) => format switch
        {
            "cli" => Indent + Bold + item + Reset,
            "html" => $"<button class=\"accordion\">{H(item)}</button>",
            "md" or "github" => Esc(item),
            _ => item,
        };

        public string CoverageFile(string item) => format switch
        {
            "cli" => Rep(2) + item,
            "html" => $"<button class=\"accordion\">{H(item)}</button>",
            "md" or "github" => "## " + Esc(item),
            _ => Indent + item,
        };

        public string CoverageFunction(string[] items) => format switch
        {
            "cli" => Rep(3) + string.Join(" ", items),
            "html" => "<tr>" + string.Concat(items.Select(i => $"<td>{H(i)}</td>")) + "</tr>",
            "md" or "github" => "### " + Esc(string.Join(" ", items)),
            _ => Rep(2) + string.Join(" ", items),
        };
    }

    // XCResultFormatter: summary, test details and coverage sections.
    sealed class Report(XcrResult result, XcrCoverage coverage, HashSet<string> targets, Formatter f, Options o)
    {
        readonly HashSet<string> fields = [.. (o.SummaryFields ?? "errors|warnings|analyzerWarnings|tests|failed|skipped|duration|date").Split('|')];

        string Duration() => result.Start is { } s && result.End is { } e ? Num(Math.Max(0, (e - s).TotalSeconds), 4) : null;
        string Date() => result.Start?.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        public string Summary()
        {
            if (f.Markdown)
            {
                var sb = new StringBuilder();
                if (fields.Contains("errors")) sb.Append($"Errors: {result.Errors}");
                if (fields.Contains("warnings")) sb.Append($"; Warnings: {result.Warnings}");
                if (fields.Contains("analyzerWarnings")) sb.Append($"; Analyzer Warnings: {result.AnalyzerWarnings}");
                if (fields.Contains("tests")) sb.Append($"; Tests: {result.TestsCount}");
                if (fields.Contains("failed")) sb.Append($"; Failed: {result.TestsFailed}");
                if (fields.Contains("skipped")) sb.Append($"; Skipped: {result.TestsSkipped}");
                if (fields.Contains("duration") && Duration() is { } d) sb.Append($"; Execution time = {d}s");
                if (fields.Contains("date") && Date() is { } dt) sb.Append($"; Execution date = {dt}");
                return sb.ToString();
            }
            var lines = new List<string> { f.Configuration("Summary") };
            if (fields.Contains("errors")) lines.Add(f.SummaryLine($"Number of errors = {result.Errors}", result.Errors != 0));
            if (fields.Contains("warnings")) lines.Add(f.SummaryLineWarning($"Number of warnings = {result.Warnings}", result.Warnings != 0));
            if (fields.Contains("analyzerWarnings")) lines.Add(f.SummaryLineWarning($"Number of analyzer warnings = {result.AnalyzerWarnings}", result.AnalyzerWarnings != 0));
            if (fields.Contains("tests")) lines.Add(f.SummaryLine($"Number of tests = {result.TestsCount}", false));
            if (fields.Contains("failed")) lines.Add(f.SummaryLine($"Number of failed tests = {result.TestsFailed}", result.TestsFailed != 0));
            if (fields.Contains("skipped")) lines.Add(f.SummaryLine($"Number of skipped tests = {result.TestsSkipped}", result.TestsSkipped != 0));
            if (fields.Contains("duration") && Duration() is { } dur) lines.Add(f.SummaryLine($"Execution time = {dur}s", false));
            if (fields.Contains("date") && Date() is { } date) lines.Add(f.SummaryLine($"Execution date = {date}", false));
            return string.Join("\n", lines);
        }

        public string TestDetails()
        {
            var lines = new List<string>();
            foreach (var run in result.Runs)
            {
                lines.Add(f.Configuration(run.Name ?? result.Title ?? "Tests"));
                if (run.Bundles.Count > 0)
                    lines.Add(f.Configuration($"{run.Bundles[0].Name.Replace(".xctest", "")} on '{result.DeviceName ?? "Unknown destination"}'"));
                if (o.FailedOnly && f.Cli && run.Bundles.All(b => !b.HasFailures)) lines.Add("No test failures");
                else foreach (var bundle in run.Bundles) Group(bundle, 0, lines);
                lines.Add(f.Divider);
            }
            return string.Join("\n", lines);
        }

        // Level 0 is the test bundle (not printed), 1 its suites, 2 and deeper nested suites.
        void Group(XcrGroup group, int level, List<string> lines)
        {
            if (o.FailedOnly && !group.HasFailures) return;
            var header = $"{group.Name} ({Clock(group.DisplayDuration)})";
            if (level == 1) lines.Add(f.Target(header, group.HasFailures));
            else if (level >= 2) lines.Add(f.Class(header, group.HasFailures));
            foreach (var sub in group.Groups) Group(sub, level + 1, lines);
            if (f.AccordionOpen != "") lines.Add(f.AccordionOpen);
            foreach (var test in group.Tests)
            {
                if (o.FailedOnly && !test.Failed) continue;
                var icon = test.Flaky ? f.FlakyIcon : test.Failed ? f.FailIcon : test.Skipped ? f.SkipIcon : test.ExpectedFailure ? f.ExpectedFailureIcon : f.PassIcon;
                var title = $"{icon} {test.Name} ({Clock(test.Duration)})";
                var failure = result.Failures.FirstOrDefault(x => !x.SessionLevel && x.TestIdentifier == test.Identifier);
                lines.Add(failure != null ? f.FailedTest(title, failure.Location is { } loc ? $"{loc}: {failure.Message}" : failure.Message) : f.Test(title, test.Failed));
            }
            if (f.AccordionClose != "") lines.Add(f.AccordionClose);
        }

        static string Clock(double? duration)
        {
            if (duration == null) return "";
            var total = Math.Max(0, (int)Math.Round(duration.Value, MidpointRounding.AwayFromZero));
            int h = total / 3600, m = total % 3600 / 60, s = total % 60;
            return h > 0 ? $"{h}h {m}m {s}s" : m > 0 ? $"{m}m {s}s" : $"{s}s";
        }

        public string CoverageDetails()
        {
            var lines = new List<string> { f.Configuration("Coverage report") };
            if (coverage == null) return string.Join("\n", lines);
            long executable = 0, covered = 0;
            var format = o.CoverageReportFormat is "totals" or "targets" or "classes" ? o.CoverageReportFormat : "methods";
            foreach (var target in coverage.Targets.Where(t => targets.Contains(t.Name)))
            {
                executable += target.Executable;
                covered += target.Covered;
                if (format == "totals") continue;
                lines.Add(f.CoverageTarget($"{target.Name}: {Percent(target.Covered, target.Executable)}% ({target.Covered}/{target.Executable})"));
                if (format == "targets") continue;
                if (f.AccordionOpen != "") lines.Add(f.AccordionOpen);
                foreach (var file in target.Files)
                {
                    lines.Add(f.CoverageFile($"{file.Name}: {Percent(file.Covered, file.Executable)}% ({file.Covered}/{file.Executable})"));
                    if (format == "classes") continue;
                    if (f.AccordionOpen != "") lines.Add(f.AccordionOpen);
                    if (f.TableOpen != "") lines.Add(f.TableOpen);
                    foreach (var fn in file.Functions)
                        lines.Add(f.CoverageFunction([$"{Percent(fn.Covered, fn.Executable)}%", $"{fn.Name}:{fn.Line}", $"({fn.Covered}/{fn.Executable})", $"{fn.Executions} times"]));
                    if (f.TableClose != "") lines.Add(f.TableClose);
                    if (f.AccordionClose != "") lines.Add(f.AccordionClose);
                }
                if (f.AccordionClose != "") lines.Add(f.AccordionClose);
            }
            if (executable > 0) lines.Insert(1, f.CoverageTarget($"Total coverage: {Percent(covered, executable)}% ({covered}/{executable})"));
            return string.Join("\n", lines);
        }

        static string Percent(long covered, long executable) => Num(executable == 0 ? 0 : 100.0 * covered / executable, 1);
    }

    // JUnit (testsuites/testsuite/testcase) or Sonar generic test execution XML.
    static string JunitXml(XcrResult result, Options o, bool sonar)
    {
        var testCase = sonar ? "testCase" : "testcase";
        var root = new XElement(sonar ? "testExecutions" : "testsuites");
        var projectRoot = o.ProjectRoot != "" && Directory.Exists(o.ProjectRoot) ? System.IO.Path.GetFullPath(o.ProjectRoot) : null;
        Dictionary<string, string> classPaths = null;
        if (sonar) root.SetAttributeValue("version", "1");
        else
        {
            root.SetAttributeValue("tests", result.TestsCount);
            root.SetAttributeValue("failures", result.TestsFailed);
            root.SetAttributeValue("errors", "0");
        }

        XElement TestElement(XcrTest test, string classname)
        {
            var e = new XElement(testCase, new XAttribute("name", test.Name ?? "No-name"));
            var duration = test.Duration is > 0 ? test.Duration : null;
            if (sonar) e.SetAttributeValue("duration", duration is { } d ? Math.Max(1, (int)(d * 1000)).ToString(CultureInfo.InvariantCulture) : "0");
            else if (duration is { } t) { e.SetAttributeValue("time", Num(t, 3)); e.SetAttributeValue("classname", classname); }
            if (test.Failed)
            {
                var mine = result.Failures.Where(x => !x.SessionLevel && x.TestIdentifier == test.Identifier).ToList();
                if (mine.Count == 0) e.Add(new XElement("failure"));
                foreach (var x in mine) e.Add(FailureElement(x, "Uncategorized", null, test.Identifier.Replace('/', '.'), projectRoot));
            }
            else if (test.Skipped) e.Add(new XElement("skipped"));
            if (test.Flaky)
            {
                if (!sonar) e.SetAttributeValue("flaky", "true");
                foreach (var failure in e.Elements("failure"))
                    failure.SetAttributeValue("message", "[FLAKY] " + ((string)failure.Attribute("message") ?? "passed on retry"));
            }
            return e;
        }

        XElement SonarFile(XcrGroup group, string configuration)
        {
            var path = group.Identifier ?? group.Name ?? "";
            if (projectRoot != null)
            {
                classPaths ??= ClassPaths(projectRoot);
                path = classPaths.GetValueOrDefault(path, path);
            }
            return new XElement("file", new XAttribute("path", path), new XAttribute("configuration", configuration));
        }

        // Tests of a suite and of the suites nested in it (classname: the innermost suite).
        IEnumerable<XElement> Cases(XcrGroup group) =>
            group.Tests.Select(t => TestElement(t, group.Name ?? "No-name")).Concat(group.Groups.SelectMany(Cases));

        foreach (var run in result.Runs)
            foreach (var bundle in run.Bundles)
            {
                var configuration = run.Name ?? "Unnamed configuration";
                if (sonar)
                {
                    if (bundle.Tests.Count > 0)
                    {
                        var file = SonarFile(bundle, configuration);
                        file.Add(bundle.Tests.Select(t => TestElement(t, bundle.Name)));
                        root.Add(file);
                    }
                    foreach (var suite in bundle.Groups)
                    {
                        var file = SonarFile(suite, configuration);
                        file.Add(Cases(suite));
                        root.Add(file);
                    }
                    continue;
                }
                var tests = bundle.AllTests().ToList();
                root.Add(new XElement("testsuite", new XAttribute("name", bundle.Name), new XAttribute("tests", tests.Count),
                    new XAttribute("failures", tests.Count(t => t.Failed)), new XAttribute("errors", "0"), new XAttribute("time", Num(bundle.Duration, 3)),
                    bundle.Tests.Select(t => TestElement(t, bundle.Name)), bundle.Groups.SelectMany(Cases)));
            }
        if (!sonar)
        {
            var total = result.Start is { } s && result.End is { } e ? (e - s).TotalSeconds : 0;
            root.SetAttributeValue("time", Num(total, 3));
            var session = result.Failures.Where(x => x.SessionLevel).ToList();
            if (session.Count > 0)
                root.Add(new XElement("testsuite", new XAttribute("name", "Session-level issues"), new XAttribute("tests", session.Count),
                    new XAttribute("failures", session.Count), new XAttribute("errors", "0"), new XAttribute("time", "0"),
                    session.Select(x => new XElement(testCase,
                        new XAttribute("name", string.IsNullOrEmpty(x.TestName) ? "Unknown test" : x.TestName),
                        new XAttribute("classname", x.TargetName ?? "Session"), new XAttribute("time", "0"),
                        FailureElement(x, "Session-level failure", x.TargetName, x.TestName, projectRoot)))));
        }
        return Xml(root);
    }

    static XElement FailureElement(XcrFailure failure, string type, string target, string testName, string projectRoot)
    {
        var element = new XElement("failure");
        var value = failure.Message ?? "";
        if (failure.File != null)
        {
            var rootPrefix = projectRoot?.Replace('\\', '/').TrimEnd('/') + "/";
            var file = projectRoot != null && failure.File.StartsWith(rootPrefix) ? failure.File[rootPrefix.Length..] : System.IO.Path.GetFileName(failure.File);
            value += $" ({file}:{failure.Line})";
        }
        if (value != "")
        {
            element.Value = value;
            element.SetAttributeValue("message", target != null ? $"{type} in {target}: {testName}" : $"{type}: {testName}");
            element.SetAttributeValue("type", type);
        }
        return element;
    }

    // Sonar wants test files: map class names to the source files that declare them (upstream greps).
    static Dictionary<string, string> ClassPaths(string root)
    {
        var map = new Dictionary<string, string>();
        var pattern = new Regex(@"^(?:public )?(?:final )?(?:public )?(?:(?:class|@implementation|struct) )([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (!(file.EndsWith(".swift") || file.EndsWith(".m") || file.EndsWith(".mm"))) continue;
            string text;
            try { text = File.ReadAllText(file); } catch (IOException) { continue; } catch (UnauthorizedAccessException) { continue; }
            var relative = System.IO.Path.GetRelativePath(root, file).Replace('\\', '/');
            foreach (Match m in pattern.Matches(text))
            {
                var name = m.Groups[1].Value;
                static int Score(string p) => p.Split("est").Length;
                if (!map.TryGetValue(name, out var existing) || Score(existing) <= Score(relative)) map[name] = relative;
            }
        }
        return map;
    }

    static string Xml(XElement root)
    {
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "    ", OmitXmlDeclaration = true, NewLineChars = "\n" };
        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, settings)) root.WriteTo(writer);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + sb.ToString().Replace(" />", "/>");
    }

    // Code Climate issues (GitLab code quality reports) for build warnings and errors.
    static string IssuesJson(XcResultBundle bundle, string format, Options o)
    {
        var result = bundle.LoadResult();
        var info = File.ReadAllBytes(System.IO.Path.Combine(bundle.Path, "Info.plist"));
        var checkName = Convert.ToHexStringLower(MD5.HashData(info));
        var issues = new List<(XcrIssue Issue, string Severity)>();
        if (format == "errors") issues.AddRange(result.ErrorIssues.Select(i => (i, "blocker")));
        else
        {
            issues.AddRange(result.WarningIssues.Select(i => (i, "minor")));
            issues.AddRange(result.AnalyzerIssues.Select(i => (i, "major")));
            if (format == "warnings-and-errors") issues.AddRange(result.ErrorIssues.Select(i => (i, "blocker")));
        }
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            w.WriteStartArray();
            foreach (var (issue, severity) in issues)
            {
                var loc = issue.Location();
                var path = loc?.Path ?? "";
                if (o.ExcludedPaths.Any(path.Contains)) continue;
                if (o.ProjectRoot != "" && path.StartsWith(o.ProjectRoot.TrimEnd('/') + "/")) path = path[(o.ProjectRoot.TrimEnd('/').Length + 1)..];
                var description = $"{issue.Type} • {issue.Message}";
                var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{issue.Type}-{issue.Message}-{path}-{loc?.StartLine ?? 0}-{loc?.StartColumn ?? 0}")));
                w.WriteStartObject();
                w.WriteString("description", description);
                w.WriteString("check_name", checkName);
                w.WriteString("fingerprint", fingerprint);
                w.WriteString("severity", severity);
                w.WriteString("engine_name", "Xcode Result Bundle Tool");
                w.WriteStartObject("location");
                w.WriteString("path", path);
                w.WriteStartObject("lines"); w.WriteNumber("begin", loc?.StartLine ?? 0); w.WriteNumber("end", loc?.EndLine ?? 0); w.WriteEndObject();
                w.WriteStartObject("positions");
                w.WriteStartObject("begin"); w.WriteNumber("line", loc?.StartLine ?? 0); w.WriteNumber("column", loc?.StartColumn ?? 0); w.WriteEndObject();
                w.WriteStartObject("end"); w.WriteNumber("line", loc?.EndLine ?? 0); w.WriteNumber("column", loc?.EndColumn ?? 0); w.WriteEndObject();
                w.WriteEndObject();
                w.WriteEndObject();
                w.WriteString("type", "issue");
                w.WriteStartArray("categories"); w.WriteEndArray();
                w.WriteStartObject("content"); w.WriteString("body", description); w.WriteEndObject();
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    const string HtmlStart = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <title>{TITLE}</title>
            <style type='text/css'>
                .resultSummaryLineFailed { color: red; margin: 0 0 0 16px; }
                .resultSummaryLineWarning { color: orange; margin: 0 0 0 16px; }
                .resultSummaryLineSuccess { margin: 0 0 0 16px; }
                .testTargetFailed { color: red; margin: 0 0 0 16px; font-weight: bold; }
                .testTargetSuccess { margin: 0 0 0 16px; font-weight: bold; }
                .testClassFailed { color: red; margin: 8px 0 0 32px; }
                .testClassSuccess { color: green; margin: 8px 0 0 32px; }
                .singleTestItemFailed { color: red; margin: 0 0 8px 48px; }
                .singleTestItemFailedWithMessage { color: red; margin: 0 0 0 32px; }
                .singleTestItemFailedMessage { color: red; margin: 0 0 8px 48px; }
                .singleTestItemSuccess { color: green; margin: 0 0 8px 48px; }
                .codeCoverageTargetSummary { margin: 0 0 0 16px; }
                .codeCoverageFileSummary { margin: 0 0 0 32px; }
                .codeCoverageFunctionSummary { margin: 0 0 0 48px; }
                .accordion { background-color: #eee; color: #444; cursor: pointer; padding: 8px 0 8px 16px; width: 100%;
                    border: none; text-align: left; outline: none; font-size: 15px; transition: 0.4s; }
                .active, .accordion:hover { background-color: #ccc; }
                .panel { padding: 0 18px; background-color: white; max-height: 0; overflow: hidden; transition: max-height 0.2s ease-out; }
                td { text-align: right; padding: 0 0 0 16px; }
            </style>
        </head>
        <body>
        """;

    const string HtmlEnd = """
            <script>
                var acc = document.getElementsByClassName("accordion");
                for (var i = 0; i < acc.length; i++) {
                    acc[i].addEventListener("click", function() {
                        this.classList.toggle("active");
                        var panel = this.nextElementSibling;
                        if (panel.style.maxHeight) {
                            panel.style.maxHeight = null;
                        } else {
                            panel.style.maxHeight = panel.scrollHeight + "px";
                        }
                    });
                }
            </script>
        </body>
        </html>
        """;
}
