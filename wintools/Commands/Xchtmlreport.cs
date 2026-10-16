using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// xchtmlreport (XCTestHTMLReport): turn .xcresult bundles into an Xcode-like HTML test report:
// a summary (device, counts, duration), then each test plan run -> test bundle -> suite -> test with
// its status, duration and failure messages with their source locations, filterable to all, passed,
// failed or skipped tests. The report is one self-contained index.html (styles and script inline).
// -j also writes a JUnit report. The .xcresult reading is shared with xcresultparser.
static class Xchtmlreport
{
    const string Version = "2.5.1";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("xchtmlreport", "Xcode-like HTML report for unit and UI tests in .xcresult bundles", Run);

    static int Run(string[] args)
    {
        var results = new List<string>();
        string output = ".";
        var junit = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: xchtmlreport [options]
                          -r, --result-bundle-path <path>  an .xcresult bundle (repeat for several)
                          -o, --output <dir>               where to write index.html (default: current folder)
                          -j, --junit                      also write report.junit
                          -i, --inline-assets              accepted (the report is always self-contained)
                          -z, --delete-unattached          accepted (attachments aren't copied)
                          --version                        show the version
                        """);
                    return 0;
                case "--version" or "-V": Console.WriteLine(Version); return 0;
                case "-r" or "--result-bundle-path": results.Add(++i < args.Length ? args[i] : throw new UsageException("-r needs a path")); break;
                case "-o" or "--output": output = ++i < args.Length ? args[i] : throw new UsageException("-o needs a folder"); break;
                case "-j" or "--junit": junit = true; break;
                case "-i" or "--inline-assets" or "-z" or "--delete-unattached" or "-v" or "--verbose": break;
                default:
                    if (args[i].EndsWith(".xcresult", StringComparison.OrdinalIgnoreCase) || args[i].EndsWith(".xcresult/")) { results.Add(args[i]); break; }
                    throw new UsageException($"unknown option {args[i]}");
            }
        }
        if (results.Count == 0) throw new UsageException("no result bundle given (-r path.xcresult)");
        var loaded = results.Select(r => (Name: Path.GetFileName(r.TrimEnd('/', '\\')), Result: XcResultBundle.Open(r).LoadResult())).ToList();
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "index.html"), Html(loaded));
        if (junit) File.WriteAllText(Path.Combine(output, "report.junit"), Junit(loaded));
        Console.WriteLine($"\nReport successfully created at {Path.GetFullPath(Path.Combine(output, "index.html"))}");
        return 0;
    }

    static string E(string s) => WebUtility.HtmlEncode(s ?? "");
    static string Dur(double? s) => s is { } d ? (d < 1 ? $"{d * 1000:0}ms" : d < 60 ? string.Create(CultureInfo.InvariantCulture, $"{d:0.00}s") : $"{(int)(d / 60)}m {d % 60:0}s") : "";

    static string Status(XcrTest t) => t.Failed ? "failure" : t.Skipped ? "skipped" : t.ExpectedFailure ? "expected" : "success";

    static List<XcrFailure> FailuresOf(XcrResult r, XcrTest t) =>
        t.Failures.Count > 0 ? t.Failures
        : r.Failures.Where(f => f.TestIdentifier != null && f.TestIdentifier == t.Identifier
                                || f.TestName != null && (f.TestName == t.Name || f.TestName.EndsWith("." + t.Name) || t.Identifier?.Replace('/', '.').EndsWith(f.TestName) == true)).ToList();

    static string Html(List<(string Name, XcrResult Result)> loaded)
    {
        var all = loaded.SelectMany(l => l.Result.AllTests()).ToList();
        int passed = all.Count(t => !t.Failed && !t.Skipped), failed = all.Count(t => t.Failed), skipped = all.Count(t => t.Skipped);
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>Test Results</title>\n<style>\n")
          .Append("""
            :root { --bg: #fff; --fg: #1d1d1f; --muted: #6e6e73; --line: #e5e5ea; --row: #f5f5f7; --ok: #28a745; --bad: #d70015; --skip: #8e8e93; }
            @media (prefers-color-scheme: dark) { :root { --bg: #1e1e1e; --fg: #f2f2f7; --muted: #98989d; --line: #38383a; --row: #2c2c2e; } }
            body { margin: 0; font: 14px -apple-system, "Segoe UI", system-ui, sans-serif; background: var(--bg); color: var(--fg); }
            header { padding: 20px 24px; border-bottom: 1px solid var(--line); }
            h1 { margin: 0 0 6px; font-size: 20px; } .meta { color: var(--muted); }
            .counts { display: flex; gap: 18px; margin-top: 12px; flex-wrap: wrap; }
            .counts button { border: 1px solid var(--line); background: var(--row); color: var(--fg); border-radius: 6px; padding: 6px 12px; cursor: pointer; font: inherit; }
            .counts button.active { outline: 2px solid #0a84ff; }
            main { padding: 12px 24px 40px; }
            details { margin-left: 16px; } summary { cursor: pointer; padding: 4px 0; } details > summary { list-style: none; } details > summary::before { content: "\25B8  "; color: var(--muted); } details[open] > summary::before { content: "\25BE  "; }
            .run > summary { font-weight: 600; font-size: 15px; margin-left: -16px; }
            .test { display: flex; gap: 10px; align-items: baseline; padding: 4px 8px; margin-left: 16px; border-radius: 6px; }
            .test:nth-child(odd) { background: var(--row); }
            .dot { width: 10px; height: 10px; border-radius: 50%; flex: none; display: inline-block; }
            .success .dot { background: var(--ok); } .failure .dot { background: var(--bad); } .skipped .dot, .expected .dot { background: var(--skip); }
            .name { flex: 1; word-break: break-all; } .time { color: var(--muted); white-space: nowrap; }
            .failure-msg { margin: 2px 0 6px 42px; color: var(--bad); font-family: ui-monospace, Consolas, monospace; font-size: 12px; white-space: pre-wrap; }
            .loc { color: var(--muted); }
            .hidden { display: none !important; }
            """)
          .Append("</style>\n</head>\n<body>\n<header>\n");
        var first = loaded[0].Result;
        sb.Append($"<h1>{E(first.Title ?? "Test Results")}</h1>\n<div class=\"meta\">");
        if (first.DeviceName != null) sb.Append($"{E(first.DeviceName)} &middot; ");
        if (first.Start is { } start) sb.Append($"{E(start.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture))} &middot; ");
        if (first.Start is { } s0 && first.End is { } e0) sb.Append($"{Dur((e0 - s0).TotalSeconds)} &middot; ");
        sb.Append($"{E(string.Join(", ", loaded.Select(l => l.Name)))}</div>\n<div class=\"counts\">\n")
          .Append($"<button class=\"active\" data-filter=\"all\">All {all.Count}</button>\n")
          .Append($"<button data-filter=\"success\">Passed {passed}</button>\n")
          .Append($"<button data-filter=\"failure\">Failed {failed}</button>\n")
          .Append($"<button data-filter=\"skipped\">Skipped {skipped}</button>\n</div>\n</header>\n<main>\n");
        foreach (var (name, result) in loaded)
            foreach (var run in result.Runs)
            {
                var runTests = run.Bundles.SelectMany(b => b.AllTests()).ToList();
                sb.Append($"<details class=\"run\" open><summary>{E(run.Name ?? name)} &mdash; {runTests.Count} tests, {runTests.Count(t => t.Failed)} failed</summary>\n");
                foreach (var b in run.Bundles) Group(sb, result, b);
                sb.Append("</details>\n");
            }
        sb.Append("""
            </main>
            <script>
            document.querySelectorAll('.counts button').forEach(function (b) {
              b.addEventListener('click', function () {
                document.querySelectorAll('.counts button').forEach(function (x) { x.classList.toggle('active', x === b); });
                var f = b.dataset.filter;
                document.querySelectorAll('.test').forEach(function (t) {
                  var show = f === 'all' || t.classList.contains(f) || (f === 'skipped' && t.classList.contains('expected'));
                  t.classList.toggle('hidden', !show);
                  var next = t.nextElementSibling;
                  while (next && next.classList.contains('failure-msg')) { next.classList.toggle('hidden', !show); next = next.nextElementSibling; }
                });
                document.querySelectorAll('details:not(.run)').forEach(function (d) {
                  d.classList.toggle('hidden', !d.querySelector('.test:not(.hidden)'));
                  if (f !== 'all') d.open = true;
                });
              });
            });
            </script>
            </body>
            </html>

            """);
        return sb.ToString();
    }

    static void Group(StringBuilder sb, XcrResult r, XcrGroup g)
    {
        var tests = g.AllTests().ToList();
        var failed = tests.Count(t => t.Failed);
        sb.Append($"<details{(failed > 0 ? " open" : "")}><summary><span class=\"{(failed > 0 ? "failure" : "success")}\"><span class=\"dot\"></span></span> {E(g.Name)} <span class=\"time\">{tests.Count} tests &middot; {Dur(g.DisplayDuration)}</span></summary>\n");
        foreach (var sub in g.Groups) Group(sb, r, sub);
        foreach (var t in g.Tests.OrderBy(t => t.Order))
        {
            sb.Append($"<div class=\"test {Status(t)}\"><span class=\"dot\"></span><span class=\"name\">{E(t.Name)}{(t.Flaky ? " (flaky)" : "")}{(t.Repetitions > 1 ? $" &times;{t.Repetitions}" : "")}</span><span class=\"time\">{Dur(t.Duration)}</span></div>\n");
            foreach (var f in FailuresOf(r, t))
                sb.Append($"<div class=\"failure-msg\">{E(f.Message)}{(f.Location != null ? $" <span class=\"loc\">({E(f.Location)})</span>" : "")}</div>\n");
        }
        sb.Append("</details>\n");
    }

    static string Junit(List<(string Name, XcrResult Result)> loaded)
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        var all = loaded.SelectMany(l => l.Result.AllTests()).ToList();
        sb.Append($"<testsuites name=\"{E(loaded[0].Name)}\" tests=\"{all.Count}\" failures=\"{all.Count(t => t.Failed)}\" skipped=\"{all.Count(t => t.Skipped)}\">\n");
        foreach (var (_, result) in loaded)
            foreach (var bundle in result.Runs.SelectMany(r => r.Bundles))
                Suites(sb, result, bundle);
        sb.Append("</testsuites>\n");
        return sb.ToString();
    }

    static void Suites(StringBuilder sb, XcrResult r, XcrGroup g)
    {
        if (g.Tests.Count > 0)
        {
            sb.Append($"  <testsuite name=\"{E(g.Name)}\" tests=\"{g.Tests.Count}\" failures=\"{g.Tests.Count(t => t.Failed)}\" time=\"{(g.Tests.Sum(t => t.Duration ?? 0)).ToString(CultureInfo.InvariantCulture)}\">\n");
            foreach (var t in g.Tests)
            {
                sb.Append($"    <testcase classname=\"{E(g.Name)}\" name=\"{E(t.Name)}\" time=\"{(t.Duration ?? 0).ToString(CultureInfo.InvariantCulture)}\"");
                var failures = FailuresOf(r, t);
                if (t.Failed)
                {
                    sb.Append(">\n");
                    foreach (var f in failures.DefaultIfEmpty(new XcrFailure { Message = "Test failed" }))
                        sb.Append($"      <failure message=\"{E(f.Message)}\">{E(f.Location)}</failure>\n");
                    sb.Append("    </testcase>\n");
                }
                else if (t.Skipped) sb.Append(">\n      <skipped/>\n    </testcase>\n");
                else sb.Append("/>\n");
            }
            sb.Append("  </testsuite>\n");
        }
        foreach (var sub in g.Groups) Suites(sb, r, sub);
    }
}
