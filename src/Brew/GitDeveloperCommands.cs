using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Brew;

public sealed class GitDeveloperCommands(Paths paths)
{
    static readonly string[] Flags = "--no-git --pull-request --github-packages --dry-run --json --keep-old --merge --write --no-commit --no-bottle --upload-only --upload --major --minor --autosquash --warn-on-upload-failure --online --all --linux --linux-arm64 --linux-self-hosted --windows --dependents --eval-all --only-formulae --only-cleanup-before --skip-setup --test-default-formula --no-api --update-non-bundler-gems --build-from-source --skip-relocation".Split(' ');
    static readonly string[] Values = "--repo --tap --workflow --branch --message --root-url --root-url-using --committer --timeout --issue --macos --tag --users --user --repos --org --team --quarter --year --output --update --url --only --test-runner --dependents-shards --named-runner --start-with --since --until".Split(' ');
    readonly FormulaRepository repository = new(paths);
    readonly InstallationStore store = new(paths);

    public async Task<int> Run(string command, string[] raw)
    {
        var args = new CommandArguments(raw, Flags.Concat(Values), Values);
        switch (command)
        {
            case "tap-new": return await TapNew(args);
            case "bottle": await Bottle(args); return 0;
            case "generate-formula-api": case "generate-internal-api": return Generate(command, args);
            case "generate-cask-api": throw new InvalidOperationException("Casks are not supported on Windows.");
            case "generate-analytics-api": throw new InvalidOperationException("Analytics are disabled in this Windows port.");
            case "determine-test-runners":
                var runners = new[] { new { name = "windows-latest", os = "windows", arch = "x86_64", formulae = args.Names } };
                CommandOutput.Header("Runners"); Console.WriteLine(JsonSerializer.Serialize(runners, new JsonSerializerOptions { WriteIndented = true })); return 0;
            case "test-bot": return await TestBot(args);
            case "lgtm":
                if (args.Has("--dry-run")) { Console.WriteLine("dotnet build\nbrew style --changed --fix\nbrew audit --changed --skip-style"); return 0; }
                if (await ProcessRunner.Run("dotnet", ["build"], paths.Repository) != 0) return 1;
                if (await DevBridge.Run("style", ["--changed", "--fix"]) != 0) return 1;
                return await DevBridge.Run("audit", ["--changed", "--skip-style", .. (args.Has("--online") ? new[] { "--online" } : Array.Empty<string>())]);
            case "vendor-gems": throw new InvalidOperationException("This Windows port uses the built-in Ruby interpreter and has no vendored Bundler gems.");
            case "update-python-resources": case "update-perl-resources": return await DevBridge.Run(command, raw);
            case "update-portable-ruby": throw new InvalidOperationException("This Windows port uses the built-in Ruby interpreter, not portable Ruby.");
            case "update-test": return await UpdateTest(args);
            case "update-maintainers": case "update-sponsors": case "update-license-data": return await UpdateData(command, args);
        }
        var repo = await GitHubRepository(args);
        switch (command)
        {
            case "pr-pull":
                args.RequireNames(command);
                foreach (var pr in args.Names)
                    if (await Gh(args, ["pr", "checkout", pr, "--repo", repo]) != 0) return 1;
                return 0;
            case "pr-publish":
                args.RequireNames(command);
                foreach (var pr in args.Names)
                {
                    var number = pr.Split('/')[^1];
                    if (!int.TryParse(number, out _)) throw new InvalidOperationException($"Not a GitHub pull request: {pr}");
                    CommandOutput.Header($"Dispatching {args.Value("--tap", repo)} pull request #{number}");
                    var inputs = new List<string> { "pull_request=" + number, "autosquash=" + args.Has("--autosquash").ToString().ToLowerInvariant() };
                    if (args.Value("--message") is string message) inputs.Add("message=" + message);
                    if (await Dispatch(args, repo, "publish-commit-bottles.yml", inputs) != 0) return 1;
                }
                return 0;
            case "dispatch-build-bottle":
                args.RequireNames(command);
                var runner = args.Has("--linux") ? "ubuntu-latest" : args.Has("--linux-arm64") ? "ubuntu-24.04-arm" : args.Value("--macos") ?? "windows-latest";
                foreach (var name in args.Names)
                {
                    var formula = repository.Load(name);
                    CommandOutput.Header($"Dispatching {args.Value("--tap", repo)} bottling request of formula \"{formula.Name}\" for {runner}");
                    var fields = new List<string> { "runner=" + runner, "formula=" + formula.Name, "upload=" + args.Has("--upload").ToString().ToLowerInvariant() };
                    foreach (var field in new[] { "timeout", "issue" }) if (args.Value("--" + field) is string value) fields.Add(field + "=" + value);
                    if (await Dispatch(args, repo, "dispatch-build-bottle.yml", fields) != 0) return 1;
                }
                return 0;
            case "release": return await Release(args, repo);
            case "contributions":
                return await Gh(args, ["search", "prs", "--repo", repo, "--state", "merged", "--limit", "1000", "--json", "number,title,author,url", .. (args.Value("--users", args.Value("--user")) is string user ? new[] { "--author", user } : Array.Empty<string>())]);
            case "pr-upload": return await Upload(args, repo);
            default: throw new InvalidOperationException($"Unknown command: brew {command}");
        }
    }

    async Task<string> GitHubRepository(CommandArguments args)
    {
        if (args.Value("--repo") is string explicitRepo) return ValidateRepository(explicitRepo);
        if (args.Value("--tap") is string tap)
        {
            var parts = tap.Split('/'); FormulaRepository.TapName(tap);
            return ValidateRepository(parts[0] + "/homebrew-" + parts[1].Replace("homebrew-", ""));
        }
        var remote = await ProcessRunner.Capture("git", ["-C", paths.Repository, "remote", "get-url", "origin"]);
        var match = Regex.Match(remote.Output.Trim(), @"github\.com[:/](?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?$");
        if (remote.Code != 0 || !match.Success) throw new InvalidOperationException("No GitHub remote found. Specify --repo=OWNER/REPO or --tap=OWNER/NAME.");
        return ValidateRepository(match.Groups["owner"].Value + "/" + match.Groups["repo"].Value);
    }
    static string ValidateRepository(string value) => Regex.IsMatch(value, @"\A[\w.-]+/[\w.-]+\z") ? value : throw new InvalidOperationException($"Invalid GitHub repository: {value}");
    async Task<int> Gh(CommandArguments args, string[] arguments)
    {
        if (args.Has("--dry-run")) { Console.WriteLine("gh " + string.Join(" ", arguments.Select(a => a.Contains(' ') ? JsonSerializer.Serialize(a) : a))); return 0; }
        return await ProcessRunner.Run("gh", arguments, paths.Repository);
    }
    Task<int> Dispatch(CommandArguments args, string repo, string workflow, IEnumerable<string> fields) => Gh(args,
        ["workflow", "run", args.Value("--workflow", workflow), "--repo", repo, "--ref", args.Value("--branch", "main"), .. fields.SelectMany(f => new[] { "-f", f })]);

    async Task<int> TapNew(CommandArguments args)
    {
        if (args.Names.Count != 1) throw new InvalidOperationException("Invalid usage: brew tap-new requires one tap argument.");
        var name = args.Names[0]; var directory = Path.Combine(paths.Taps, FormulaRepository.TapName(name));
        if (Directory.Exists(directory)) throw new InvalidOperationException($"Tap {name} already exists.");
        if (args.Has("--dry-run")) { Console.WriteLine($"Would create {directory}"); return 0; }
        Directory.CreateDirectory(Path.Combine(directory, "Formula"));
        File.WriteAllText(Path.Combine(directory, "README.md"), $"# {name}\n\n```powershell\nbrew tap {name}\nbrew install {name}/<formula>\n```\n\nCI requires a self-hosted Windows runner with brew on PATH.\n");
        var workflow = Path.Combine(directory, ".github", "workflows"); Directory.CreateDirectory(workflow);
        File.WriteAllText(Path.Combine(workflow, "tests.yml"), "name: brew test-bot\non: [push, pull_request]\njobs:\n  test:\n    runs-on: [self-hosted, Windows]\n    steps:\n      - uses: actions/checkout@v4\n      - run: brew test-bot\n");
        if (!args.Has("--no-git"))
        {
            await ProcessRunner.Git(directory, "init"); await ProcessRunner.Git(directory, "add", ".");
            await ProcessRunner.Git(directory, "commit", "-m", $"Create {name} tap");
        }
        Console.WriteLine($"Created {name}\n{directory}"); return 0;
    }

    async Task Bottle(CommandArguments args)
    {
        args.RequireNames("bottle");
        if (args.Has("--merge")) { MergeBottles(args); return; }
        foreach (var name in args.Names)
        {
            var keg = store.Latest(name) ?? throw new InvalidOperationException($"{name} is not installed");
            var formula = repository.Load(keg.Receipt.Name);
            var rebuild = args.Has("--no-rebuild") || formula.Bottle == null ? 0 : formula.Bottle.Rebuild;
            var filename = Bottles.FileName(keg.Receipt.Name, keg.Receipt.Version, rebuild);
            if (args.Has("--dry-run")) { Console.WriteLine(filename); continue; }
            var file = Path.GetFullPath(filename);
            CommandOutput.Header($"Bottling {filename}...");
            var cellar = await Bottles.CreateAsync(paths, keg.Path, file);
            if (args.Has("--skip-relocation")) cellar = "any_skip_relocation";
            var checksum = await Bottles.Sha256Async(file);
            Console.WriteLine("./" + filename);
            Console.WriteLine("  bottle do");
            if (rebuild > 0) Console.WriteLine($"    rebuild {rebuild}");
            Console.WriteLine($"    {Sha256Line(cellar, checksum)}");
            Console.WriteLine("  end");
            if (!args.Has("--json")) continue;
            var rootUrl = args.Value("--root-url") ?? $"{Bottles.Domain}/{Bottles.ReleaseTag(keg.Receipt.Name, keg.Receipt.Version)}";
            var data = new Dictionary<string, object>
            {
                [keg.Receipt.Name] = new Dictionary<string, object>
                {
                    ["formula"] = new Dictionary<string, object>
                    {
                        ["name"] = keg.Receipt.Name, ["pkg_version"] = keg.Receipt.Version,
                        ["path"] = Path.GetRelativePath(paths.Repository, keg.Receipt.SourcePath ?? repository.Find(keg.Receipt.Name)).Replace(Path.DirectorySeparatorChar, '/'),
                        ["desc"] = formula.Description, ["license"] = formula.License, ["homepage"] = formula.Homepage,
                    },
                    ["bottle"] = new Dictionary<string, object>
                    {
                        ["root_url"] = rootUrl, ["cellar"] = cellar, ["rebuild"] = rebuild,
                        ["date"] = File.GetLastWriteTimeUtc(file).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        ["tags"] = new Dictionary<string, object>
                        {
                            [Bottles.Tag] = new Dictionary<string, object>
                            {
                                ["filename"] = Uri.EscapeDataString(filename), ["local_filename"] = filename, ["sha256"] = checksum,
                                ["installed_size"] = Paths.TreeFiles(keg.Path).Sum(f => new FileInfo(f).Length),
                            },
                        },
                    },
                },
            };
            File.WriteAllText(Path.ChangeExtension(Path.ChangeExtension(file, null), ".json"),
                JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    static string Sha256Line(string cellar, string checksum) =>
        cellar is "any" or "any_skip_relocation" ? $"sha256 cellar: :{cellar}, {Bottles.Tag}: \"{checksum}\""
            : $"sha256 {Bottles.Tag}: \"{checksum}\"";

    // brew bottle --merge [--write [--no-commit]] *.bottle.json
    void MergeBottles(CommandArguments args)
    {
        foreach (var json in args.Names)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(json));
            foreach (var entry in doc.RootElement.EnumerateObject())
            {
                var bottle = entry.Value.GetProperty("bottle");
                var tag = bottle.GetProperty("tags").GetProperty(Bottles.Tag);
                var rebuild = bottle.GetProperty("rebuild").GetInt32();
                var root = bottle.GetProperty("root_url").GetString();
                var pkgVersion = entry.Value.GetProperty("formula").GetProperty("pkg_version").GetString();
                var lines = new List<string> { "  bottle do" };
                if (root != $"{Bottles.Domain}/{Bottles.ReleaseTag(entry.Name, pkgVersion)}") lines.Add($"    root_url \"{root}\"");
                if (rebuild > 0) lines.Add($"    rebuild {rebuild}");
                lines.Add("    " + Sha256Line(bottle.GetProperty("cellar").GetString(), tag.GetProperty("sha256").GetString()));
                lines.Add("  end");
                CommandOutput.Header(entry.Name);
                Console.WriteLine(string.Join("\n", lines));
                if (!args.Has("--write")) continue;
                var path = repository.Find(entry.Name);
                var source = File.ReadAllText(path).Replace("\r\n", "\n");
                var existing = new Regex(@"^  bottle do\n.*?^  end\n", RegexOptions.Multiline | RegexOptions.Singleline);
                var updated = existing.IsMatch(source);
                source = updated ? existing.Replace(source, string.Join("\n", lines) + "\n", 1) : InsertBottle(source, string.Join("\n", lines));
                File.WriteAllText(path, source);
                if (args.Has("--no-commit")) continue;
                ProcessRunner.Git(Path.GetDirectoryName(path), "add", path).GetAwaiter().GetResult();
                ProcessRunner.Git(Path.GetDirectoryName(path), "commit", "--no-edit", "--quiet",
                    $"--message={entry.Name}: {(updated ? "update" : "add")} {pkgVersion} bottle.", "--", path).GetAwaiter().GetResult();
            }
        }
    }

    // Homebrew's component order: the bottle block follows livecheck, else the url/version stanza.
    static string InsertBottle(string source, string block)
    {
        var livecheck = Regex.Match(source, @"^  livecheck do\n.*?^  end\n", RegexOptions.Multiline | RegexOptions.Singleline);
        if (livecheck.Success) return source.Insert(livecheck.Index + livecheck.Length, "\n" + block + "\n");
        var next = Regex.Match(source, @"^\n(?=  (?:head|depends_on|uses_from_macos|on_\w+|resource|conflicts_with|keg_only|deprecate!|disable!|option|patch|fails_with|link_overwrite|skip_clean|def |service|test do|pypi_packages|no_autobump!)\b)", RegexOptions.Multiline);
        if (next.Success) return source.Insert(next.Index, "\n" + block + "\n");
        var end = source.LastIndexOf("\nend", StringComparison.Ordinal);
        return source.Insert(end, "\n\n" + block);
    }

    int Generate(string command, CommandArguments args)
    {
        var file = args.Value("--output", Path.Combine(paths.Repository, "api", command == "generate-formula-api" ? "formula.json" : "commands.json"));
        object value = command == "generate-formula-api" ? repository.Names.Select(n => new QueryCommands(paths).JsonFormula(repository.Load(n))).ToArray() : CommandHelp.All.ToArray();
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        if (args.Has("--dry-run")) Console.WriteLine(json);
        else { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))); File.WriteAllText(file, json + "\n"); Console.WriteLine(file); }
        return 0;
    }

    async Task<int> TestBot(CommandArguments args)
    {
        var names = args.Names.Count == 0 ? (await ProcessRunner.Git(paths.Repository, "diff", "--name-only", "HEAD")).Split('\n')
            .Where(f => f.Contains("Formula/") && f.EndsWith(".rb")).Select(Path.GetFileNameWithoutExtension).ToArray() : args.Names.ToArray();
        foreach (var name in names)
        {
            if (args.Has("--dry-run")) { Console.WriteLine($"brew install {name}\nbrew test {name}\nbrew bottle --json {name}"); continue; }
            using (Cli.Lock(paths)) await new PackageManager(paths).InstallAsync(name);
            if (await DevBridge.Run("test", [name]) != 0) return 1;
            await Bottle(new CommandArguments(["--json", name], ["--json"]));
        }
        return 0;
    }

    async Task<int> Release(CommandArguments args, string repo)
    {
        if (args.Has("--major") && args.Has("--minor")) throw new InvalidOperationException("Invalid usage: Options --major and --minor are mutually exclusive.");
        if (args.Has("--dry-run")) return await Gh(args, ["workflow", "run", "release.yml", "--repo", repo, "-f", "major=" + args.Has("--major").ToString().ToLowerInvariant(), "-f", "minor=" + args.Has("--minor").ToString().ToLowerInvariant()]);
        var blockers = await ProcessRunner.Capture("gh", ["issue", "list", "--repo", repo, "--label", "release blocker", "--state", "open", "--json", "url"]);
        if (blockers.Code != 0) throw new InvalidOperationException("Unable to check for release blockers: " + blockers.Error.Trim());
        using (var issues = JsonDocument.Parse(blockers.Output))
            if (issues.RootElement.GetArrayLength() > 0) throw new InvalidOperationException("Open issues or pull requests are blocking this release:\n" + string.Join("\n", issues.RootElement.EnumerateArray().Select(i => i.GetProperty("url").GetString())));
        var latest = await ProcessRunner.Capture("gh", ["release", "view", "--repo", repo, "--json", "tagName", "--jq", ".tagName"]);
        if (latest.Code != 0) throw new InvalidOperationException("No existing releases found!");
        var version = Version.Parse(latest.Output.Trim().TrimStart('v'));
        var next = args.Has("--major") ? $"{version.Major + 1}.0.0" : args.Has("--minor") ? $"{version.Major}.{version.Minor + 1}.0" : $"{version.Major}.{version.Minor}.{version.Build + 1}";
        CommandOutput.Header("Generating release notes for " + next);
        var notes = await ProcessRunner.Capture("gh", ["api", $"repos/{repo}/releases/generate-notes", "-f", "tag_name=" + next, "-f", "previous_tag_name=" + latest.Output.Trim(), "--jq", ".body"]);
        if (notes.Code != 0) throw new InvalidOperationException(notes.Error.Trim());
        Console.WriteLine(notes.Output);
        if (!args.Has("--force", "-f")) { CommandOutput.Warning("Use `brew release --force` to trigger the release workflow and create the draft release."); return 0; }
        return await Dispatch(args, repo, "release.yml", ["tag=" + next]);
    }

    async Task<int> Upload(CommandArguments args, string repo)
    {
        var files = Directory.GetFiles(Environment.CurrentDirectory, "*.bottle.json");
        if (files.Length == 0) throw new InvalidOperationException("No bottle JSON files found in the current working directory");
        foreach (var file in files)
        {
            using var data = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var formula in data.RootElement.EnumerateObject())
            {
                var bottle = formula.Value.GetProperty("bottle");
                var root = args.Value("--root-url") ?? (bottle.TryGetProperty("root_url", out var url) ? url.GetString() : null);
                var match = Regex.Match(root ?? "", @"https://github\.com/([\w.-]+/[\w.-]+)/releases/download/([^/]+)");
                if (!match.Success) throw new InvalidOperationException("Service specified by root_url is not recognized. Set a GitHub Releases --root-url for this Windows bottle uploader.");
                foreach (var tag in bottle.GetProperty("tags").EnumerateObject())
                {
                    var artifact = tag.Value.GetProperty("local_filename").GetString();
                    await using var input = File.OpenRead(artifact);
                    var actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(input));
                    if (actual != tag.Value.GetProperty("sha256").GetString()) throw new InvalidOperationException($"SHA256 mismatch: {artifact}");
                    if (await Gh(args, ["release", "upload", match.Groups[2].Value, artifact, "--repo", match.Groups[1].Value]) != 0) return 1;
                }
            }
        }
        return 0;
    }

    async Task<int> UpdateData(string command, CommandArguments args)
    {
        if (command == "update-sponsors") throw new InvalidOperationException("Updating sponsor data is not implemented for this Windows port.");
        var url = args.Value("--url", command == "update-license-data" ? "https://raw.githubusercontent.com/spdx/license-list-data/main/json/licenses.json" : null);
        if (url == null)
        {
            var repo = await GitHubRepository(args);
            return await Gh(args, ["api", $"repos/{repo}/contributors", "--paginate"]);
        }
        if (args.Has("--dry-run")) { Console.WriteLine(url); return 0; }
        using var client = new HttpClient(); var json = await client.GetStringAsync(url); using var valid = JsonDocument.Parse(json);
        var output = Path.GetFullPath(args.Value("--output", Path.Combine(paths.Repository, "data", "licenses.json")));
        Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, json); Console.WriteLine(output); return 0;
    }
    async Task<int> UpdateTest(CommandArguments args)
    {
        if (args.Has("--dry-run")) { Console.WriteLine("dotnet build\ndotnet run --project tests/Smoke"); return 0; }
        if (await ProcessRunner.Run("dotnet", ["build"], paths.Repository) != 0) return 1;
        return await ProcessRunner.Run("dotnet", ["run", "--project", "tests/Smoke"], paths.Repository);
    }
}
