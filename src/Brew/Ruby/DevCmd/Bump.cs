using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Brew.Ruby.DevCmd;

// Line-based stand-in for Utils::AST::FormulaAST: finds top-level (or `stable do`) stanzas in
// formula source and rewrites them the way Homebrew's AST rewriter would.
sealed class FormulaText
{
    public List<string> Lines;
    readonly bool crlf;

    public FormulaText(string contents)
    {
        crlf = contents.Contains("\r\n");
        Lines = contents.Replace("\r\n", "\n").Split('\n').ToList();
    }

    public string Process() => string.Join(crlf ? "\r\n" : "\n", Lines);

    public sealed record Stanza(string Name, bool Block, int Start, int End, int Indent);

    static readonly Regex StartRe = new(@"^(?<i> +)(?<n>[a-z_][a-zA-Z0-9_]*[!?]?)(?<rest>.*)$");

    // Statements directly inside the lines (from, to) at the given indentation.
    List<Stanza> Statements(int from, int to, int indent)
    {
        var result = new List<Stanza>();
        var pad = new string(' ', indent);
        for (int i = from; i < to; i++)
        {
            var line = Lines[i];
            if (!line.StartsWith(pad) || line.Length <= indent || line[indent] == ' ' || line[indent] == '#') continue;
            var m = StartRe.Match(line);
            if (!m.Success || m.Groups["i"].Length != indent) continue;
            var name = m.Groups["n"].Value;
            if (name is "end" or "else" or "elsif" or "when" or "rescue" or "ensure") continue;
            bool block = Regex.IsMatch(m.Groups["rest"].Value, @"\bdo(\s*\|[^|]*\|)?\s*(#.*)?$");
            int end = i;
            if (block)
            {
                for (int j = i + 1; j < to; j++)
                    if (Lines[j] == pad + "end" || Lines[j].StartsWith(pad + "end ") ) { end = j; break; }
            }
            else
                while (end + 1 < to && Regex.IsMatch(Lines[end], @"[,\\(\[{]\s*$")) end++;
            result.Add(new Stanza(name, block, i, end, indent));
            i = end;
        }
        return result;
    }

    int ClassStart => Lines.FindIndex(l => Regex.IsMatch(l, @"^class \S+ < Formula"));
    int ClassEnd => Lines.FindLastIndex(l => l == "end");

    public List<Stanza> Children => Statements(ClassStart + 1, ClassEnd, 2);

    public List<Stanza> StableChildren
    {
        get
        {
            var children = Children;
            var stable = children.FirstOrDefault(c => c.Name == "stable" && c.Block);
            return stable == null ? children : Statements(stable.Start + 1, stable.End, 4);
        }
    }

    public Stanza StableStanza(string name) => StableChildren.FirstOrDefault(c => c.Name == name && !c.Block);
    public bool HasStableStanza(string name) => StableStanza(name) != null;
    public Stanza TopStanza(string name, bool block = false) => Children.FirstOrDefault(c => c.Name == name && c.Block == block);

    static readonly Regex FirstString = new("\"((?:[^\"\\\\]|\\\\.)*)\"");

    public void ReplaceStableValue(string name, string value)
    {
        var s = StableStanza(name) ?? throw new BrewError($"Could not find '{name}' stanza!");
        var line = Lines[s.Start];
        int at = line.IndexOf(name, StringComparison.Ordinal) + name.Length;
        var m = FirstString.Match(line, at);
        Lines[s.Start] = m.Success ? line[..m.Index] + Create.Inspect(value) + line[(m.Index + m.Length)..] : line[..at] + " " + Create.Inspect(value);
    }

    public void ReplaceStableHashValue(string name, string key, string value)
    {
        var s = StableStanza(name) ?? throw new BrewError($"Could not find '{name}' stanza!");
        for (int i = s.Start; i <= s.End; i++)
        {
            var m = Regex.Match(Lines[i], $"\\b{key}:\\s*(\"(?:[^\"\\\\]|\\\\.)*\")");
            if (!m.Success) continue;
            Lines[i] = Lines[i][..m.Groups[1].Index] + Create.Inspect(value) + Lines[i][(m.Groups[1].Index + m.Groups[1].Length)..];
            return;
        }
        throw new BrewError($"Could not find '{key}' in '{name}' stanza!");
    }

    public string StableHashValue(string name, string key)
    {
        var s = StableStanza(name);
        if (s == null) return null;
        for (int i = s.Start; i <= s.End; i++)
            if (Regex.Match(Lines[i], $"\\b{key}:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"") is { Success: true } m) return m.Groups[1].Value;
        return null;
    }

    void RemoveRange(int start, int end)
    {
        Lines.RemoveRange(start, end - start + 1);
        // Don't leave two blank lines (or a blank line right after `class ... do`/before `end`).
        if (start < Lines.Count && start > 0 && Lines[start].Trim().Length == 0 && (Lines[start - 1].Trim().Length == 0 || Regex.IsMatch(Lines[start - 1], @"(< Formula|\bdo)\s*$")))
            Lines.RemoveAt(start);
        else if (start > 0 && start < Lines.Count && Lines[start - 1].Trim().Length == 0 && Lines[start].Trim() == "end")
            Lines.RemoveAt(start - 1);
    }

    public void RemoveStanza(string name, bool block = false)
    {
        var s = TopStanza(name, block) ?? throw new BrewError($"Could not find '{name}' stanza!");
        RemoveRange(s.Start, s.End);
    }

    public void RemoveStableStanza(string name)
    {
        var s = StableStanza(name) ?? throw new BrewError($"Could not find '{name}' stanza!");
        RemoveRange(s.Start, s.End);
    }

    public void RemoveStableStanzas(string name)
    {
        while (StableStanza(name) is { } s) RemoveRange(s.Start, s.End);
    }

    public void AddStableStanzasAfter(string after, List<string> texts)
    {
        if (texts.Count == 0) return;
        var s = StableChildren.Where(c => c.Name == after && !c.Block).LastOrDefault() ?? throw new BrewError($"Could not find '{after}' stanza!");
        var pad = new string(' ', s.Indent);
        Lines.InsertRange(s.End + 1, texts.Select(t => pad + t));
    }

    public void ReplaceStanza(string name, string text)
    {
        var s = TopStanza(name) ?? throw new BrewError($"Could not find '{name}' stanza!");
        Lines.RemoveRange(s.Start, s.End - s.Start + 1);
        Lines.Insert(s.Start, "  " + text);
    }

    static readonly string[] Precedence = { "include", "desc", "homepage", "url", "mirror", "version", "sha256", "license", "revision" };

    // FormulaAST#add_stanza: after the last component that sorts before it.
    public void AddStanza(string name, string text)
    {
        var children = Children;
        int target = Array.IndexOf(Precedence, name);
        Stanza preceding = children.FirstOrDefault();
        foreach (var c in children.Skip(1))
        {
            int idx = Array.IndexOf(Precedence, c.Name);
            if (c.Block || idx < 0 || idx >= target) break;
            preceding = c;
        }
        if (preceding == null) throw new BrewError($"Could not find a place for '{name}'!");
        Lines.Insert(preceding.End + 1, "  " + text);
    }

    public bool Contains(string s) => Lines.Any(l => l.Contains(s));
}

// Small unified diff, shown for `--dry-run` since nothing is written.
static class TextDiff
{
    public static string Unified(string oldText, string newText, string path, int context = 3)
    {
        var a = oldText.Replace("\r\n", "\n").Split('\n');
        var b = newText.Replace("\r\n", "\n").Split('\n');
        int n = a.Length, m = b.Length;
        var lcs = new int[n + 1, m + 1];
        for (int i = n - 1; i >= 0; i--)
            for (int j = m - 1; j >= 0; j--)
                lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
        var ops = new List<(char op, string line, int ai, int bi)>();
        {
            int i = 0, j = 0;
            while (i < n || j < m)
            {
                if (i < n && j < m && a[i] == b[j]) { ops.Add((' ', a[i], i, j)); i++; j++; }
                else if (i < n && (j == m || lcs[i + 1, j] >= lcs[i, j + 1])) { ops.Add(('-', a[i], i, j)); i++; }
                else { ops.Add(('+', b[j], i, j)); j++; }
            }
        }
        var sb = new StringBuilder();
        var rel = path.Replace('\\', '/');
        sb.Append($"{Tty.Bold}--- a/{rel}{Tty.Reset}\n{Tty.Bold}+++ b/{rel}{Tty.Reset}\n");
        for (int k = 0; k < ops.Count;)
        {
            if (ops[k].op == ' ') { k++; continue; }
            int start = Math.Max(0, k - context), end = k;
            while (true)
            {
                while (end < ops.Count && ops[end].op != ' ') end++;
                int next = end;
                while (next < ops.Count && ops[next].op == ' ' && next - end < context * 2) next++;
                if (next < ops.Count && ops[next].op != ' ') { end = next; continue; }
                end = Math.Min(ops.Count, end + context);
                break;
            }
            var hunk = ops.GetRange(start, end - start);
            int aStart = hunk[0].ai + 1, bStart = hunk[0].bi + 1;
            int aLen = hunk.Count(h => h.op != '+'), bLen = hunk.Count(h => h.op != '-');
            sb.Append($"{Tty.Cyan}@@ -{aStart},{aLen} +{bStart},{bLen} @@{Tty.Reset}\n");
            foreach (var h in hunk)
                sb.Append(h.op switch
                {
                    '+' => $"{Tty.Green}+{h.line}{Tty.Reset}\n",
                    '-' => $"{Tty.Red}-{h.line}{Tty.Reset}\n",
                    _ => $" {h.line}\n",
                });
            k = end;
        }
        return sb.ToString().TrimEnd('\n');
    }
}

// GitHub/git helpers shared by the bump commands (utils/github.rb, bump.rb).
static class BumpSupport
{
    // Tap#remote_repository: "Owner/Repo" from origin's URL.
    public static string RemoteRepository(string tapDir)
    {
        var url = DevTap.GitRead(tapDir, "remote", "get-url", "origin").Trim();
        var m = Regex.Match(url, @"github\.com[/:]([^/]+)/([^/]+?)(?:\.git)?/?$");
        return m.Success ? $"{m.Groups[1].Value}/{m.Groups[2].Value}" : null;
    }

    public static string OriginBranch(string tapDir)
    {
        var head = DevTap.GitRead(tapDir, "symbolic-ref", "-q", "refs/remotes/origin/HEAD").Trim();
        if (head.Length > 0) return head.Replace("refs/remotes/origin/", "");
        var cur = DevTap.GitRead(tapDir, "rev-parse", "--abbrev-ref", "HEAD").Trim();
        return cur.Length > 0 && cur != "HEAD" ? cur : "main";
    }

    static Regex TitleRegex(string name, string version) => string.IsNullOrEmpty(version)
        ? new Regex($@"(?:^|\s){Regex.Escape(name)}(?::|,|\s|$)", RegexOptions.IgnoreCase)
        : new Regex($@"(?:^|\s){Regex.Escape(name)}(?::|,|\s)(?:.*\s)?{Regex.Escape(version)}(?::|,|\s|$)", RegexOptions.IgnoreCase);

    // GitHub.fetch_pull_requests (search REST API)
    public static List<JsonObject> FetchPullRequests(string name, string repo, string state = null, string version = null)
    {
        if (DevHttp.NoGitHubApi || repo == null) return new List<JsonObject>();
        var q = $"is:pr {name} {version}".Trim() + $" repo:{repo} in:title" + (state != null ? $" state:{state}" : "");
        JsonNode json;
        try { json = DevHttp.GitHubJson($"{DevHttp.ApiUrl}/search/issues?q={Uri.EscapeDataString(q)}&per_page=100"); }
        catch (GitHubApiError e) when (e.Status == 422) { return new List<JsonObject>(); }
        var regex = TitleRegex(name, version);
        return (json?["items"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
            .Where(pr => ((string)pr["html_url"] ?? "").Contains("/pull/") && regex.IsMatch((string)pr["title"] ?? "")).ToList();
    }

    // GitHub.check_for_duplicate_pull_requests
    public static void CheckForDuplicatePullRequests(string name, string repo, string file, bool quiet, string state = null, string version = null, bool officialTap = false)
    {
        var prs = FetchPullRequests(name, repo, state, version).Where(pr =>
        {
            try
            {
                var files = DevHttp.GitHubJson($"{DevHttp.ApiUrl}/repos/{repo}/pulls/{pr["number"]}/files?per_page=100") as JsonArray;
                return files != null && files.Any(f => (string)f?["filename"] == file);
            }
            catch (GitHubApiError) { return false; }
        }).ToList();
        if (prs.Count == 0) return;
        var confidence = version != null ? "are" : "might be";
        var duplicates = $"These {(state != null ? state + " " : "")}pull requests {confidence} duplicates:\n" +
                         string.Join("\n", prs.Select(pr => $"{pr["title"]} {pr["html_url"]}"));
        const string error = "Duplicate PRs must not be opened.\nManually open these PRs if you are sure that they are not duplicates (and tell us that in the PR).";
        if (version != null && officialTap) throw new BrewError($"{duplicates}\n{error}");
        if (!officialTap) Out.Opoo(duplicates);
        else if (quiet) Out.Opoo(error);
        else Out.Opoo($"{duplicates}\n{error}");
    }

    public static string Gh => DevCommands.Which("gh");

    public static string Run(string file, string cwd, params string[] args)
    {
        var (code, stdout, stderr) = DevCommands.Capture(file, args, cwd);
        if (code != 0)
            throw new BrewError($"Failure while executing; `{Path.GetFileNameWithoutExtension(file)} {string.Join(" ", args)}` exited with {code}.{(stderr.Length > 0 ? "\n" + stderr.TrimEnd() : "")}");
        return stdout;
    }

    public static void SafeSystem(string file, string cwd, params string[] args)
    {
        Console.Out.Flush();
        var code = DevCommands.Exec(file, args, cwd);
        if (code != 0) throw new BrewError($"Failure while executing; `{Path.GetFileNameWithoutExtension(file)} {string.Join(" ", args)}` exited with {code}.");
    }

    public static string Redact(string url) =>
        DevHttp.GitHubToken is { } t ? url.Replace(t, "******") : url;

    public sealed record Commit(string Path, string OldContents, string Message);

    // Homebrew::Bump.create_pr; returns the PR URL (null for --dry-run / --commit).
    public static string CreatePr(string tapName, string tapDir, string remoteRepo, string branch, string title, string body,
        List<Commit> commits, bool dryRun, bool noFork, string forkOrg, bool commit)
    {
        if (remoteRepo == null) throw new BrewError($"The tap {tapName} does not have a remote repository!");
        var remoteBranch = OriginBranch(tapDir);
        var git = DevTap.Git;
        string remoteUrl, username;
        void Revert()
        {
            foreach (var c in commits) File.WriteAllText(c.Path, c.OldContents);
        }
        if (noFork)
        {
            remoteUrl = DevTap.GitRead(tapDir, "remote", "get-url", "--push", "origin").Trim();
            username = remoteRepo.Split('/')[0];
            if (Environment.GetEnvironmentVariable("HOMEBREW_GITHUB_API_TOKEN") is { Length: > 0 } tok)
                remoteUrl = Regex.Replace(remoteUrl, "^https://github.com/", $"https://x-access-token:{tok}@github.com/");
        }
        else if (dryRun)
        {
            // Homebrew forks before printing; a dry run here never touches GitHub.
            username = forkOrg ?? (DevHttp.GitHubToken != null ? TryLogin() : null) ?? "<your-github-user>";
            remoteUrl = $"https://github.com/{username}/{remoteRepo.Split('/')[1]}.git";
        }
        else
        {
            var gh = Gh;
            if (gh == null)
            {
                Revert();
                throw new BrewError("Unable to fork: the GitHub CLI (`gh`) is required to fork and open pull requests on Windows!\nInstall it with `brew install gh`, run `gh auth login`, or pass `--no-fork`.");
            }
            try
            {
                var forkArgs = new List<string> { "repo", "fork", remoteRepo, "--clone=false" };
                if (forkOrg != null) { forkArgs.Add("--org"); forkArgs.Add(forkOrg); }
                Run(gh, tapDir, forkArgs.ToArray());
                username = forkOrg ?? Run(gh, tapDir, "api", "user", "--jq", ".login").Trim();
            }
            catch (BrewError e)
            {
                Revert();
                throw new BrewError($"Unable to fork: {e.Message}!");
            }
            remoteUrl = $"https://github.com/{username}/{remoteRepo.Split('/')[1]}.git";
        }

        var gitDir = DevTap.GitRead(tapDir, "rev-parse", "--absolute-git-dir").Trim();
        bool shallow = gitDir.Length > 0 && File.Exists(Path.Combine(gitDir, "shallow"));
        if (!dryRun)
        {
            if (!commit && shallow) SafeSystem(git, tapDir, "fetch", "--unshallow", "origin");
            if (!commit) SafeSystem(git, tapDir, "checkout", "--no-track", "-b", branch, $"origin/{remoteBranch}");
        }

        foreach (var c in commits)
        {
            var dir = Path.GetDirectoryName(c.Path)!;
            if (dryRun)
            {
                Out.Ohai($"git checkout --no-track -b {branch} origin/{remoteBranch}");
                if (shallow) Out.Ohai("git fetch --unshallow origin");
                Out.Ohai($"git add {c.Path}");
                Out.Ohai($"git commit --no-edit --verbose --message='{c.Message}' -- {c.Path}");
                Out.Ohai($"git push --set-upstream {Redact(remoteUrl)} {branch}:{branch}");
                Out.Ohai("git checkout --quiet -");
                Out.Ohai($"create pull request with GitHub API (base branch: {remoteBranch})");
            }
            else
            {
                SafeSystem(git, dir, "add", c.Path);
                SafeSystem(git, dir, "commit", "--no-edit", "--verbose", $"--message={c.Message}", "--", c.Path);
            }
        }
        if (commit || dryRun) return null;

        SafeSystem(git, tapDir, "push", "--set-upstream", remoteUrl, $"{branch}:{branch}");
        SafeSystem(git, tapDir, "checkout", "--quiet", "-");
        try
        {
            var gh = Gh ?? throw new BrewError("the GitHub CLI (`gh`) is required");
            return Run(gh, tapDir, "pr", "create", "--repo", remoteRepo, "--base", remoteBranch, "--head", $"{username}:{branch}",
                "--title", title, "--body", body).Trim().Split('\n').Last();
        }
        catch (BrewError e)
        {
            Revert();
            throw new BrewError($"Unable to open pull request for {remoteRepo}: {e.Message}!");
        }
    }

    static string TryLogin()
    {
        try { return (string)DevHttp.GitHubJson($"{DevHttp.ApiUrl}/user")?["login"]; }
        catch (GitHubApiError) { return null; }
    }

    public static void OpenBrowser(string url)
    {
        var browser = Environment.GetEnvironmentVariable("HOMEBREW_BROWSER") ?? Environment.GetEnvironmentVariable("BROWSER");
        if (!string.IsNullOrEmpty(browser)) DevCommands.Exec(DevCommands.Which(browser) ?? browser, new[] { url });
        else System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
    }

    public static string PrMessage(string command, string userMessage)
    {
        var msg = "";
        if (!string.IsNullOrEmpty(userMessage)) msg += $"{userMessage}\n\n---\n\n";
        return msg + $"Created with `brew {command}`.";
    }

    // Loads a formula from (possibly modified) source text without touching its real file.
    public static FormulaSpec FromContents(string name, string contents)
    {
        var dir = Path.Combine(Path.GetTempPath(), "brew-bump-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var p = Path.Combine(dir, name + ".rb");
            File.WriteAllText(p, contents);
            return FormulaLoader.LoadSpec(p);
        }
        finally { Directory.Delete(dir, true); }
    }
}

[DevCommand("bump-formula-pr")]
sealed class BumpFormulaPr : DevCommand
{
    public override CommandParser Args() => new CommandParser("bump-formula-pr")
        .Description("""
            Create a pull request to update <formula> with a new URL or a new tag.

            If a <URL> is specified, the <SHA-256> checksum of the new download should also
            be specified. A best effort to determine the <SHA-256> will be made if not supplied
            by the user.

            If a <tag> is specified, the Git commit <revision> corresponding to that tag
            should also be specified. A best effort to determine the <revision> will be made
            if the value is not supplied by the user.

            If a <version> is specified, a best effort to determine the <URL> and <SHA-256> or
            the <tag> and <revision> will be made if both values are not supplied by the user.

            *Note:* this command cannot be used to transition a formula from a
            URL-and-SHA-256 style specification into a tag-and-revision style specification,
            nor vice versa. It must use whichever style specification the formula already uses.
            """)
        .Switch(new[] { "-n", "--dry-run" }, "Print what would be done rather than doing it.")
        .Switch(new[] { "--write-only" }, "Make the expected file modifications without taking any Git actions.")
        .Switch(new[] { "--commit" }, "When passed with `--write-only`, generate a new commit after writing changes to the formula file.", dependsOn: "--write-only")
        .Switch(new[] { "--no-audit" }, "Don't run `brew audit` before opening the PR.")
        .Switch(new[] { "--strict" }, "Run `brew audit --strict` before opening the PR.")
        .Switch(new[] { "--online" }, "Run `brew audit --online` before opening the PR.")
        .Switch(new[] { "--no-browse" }, "Print the pull request URL instead of opening in a browser.")
        .Switch(new[] { "--no-fork" }, "Don't try to fork the repository.")
        .CommaArray("--mirror", "Use the specified <URL> as a mirror URL. If <URL> is a comma-separated list of URLs, multiple mirrors will be added.")
        .Flag(new[] { "--fork-org=" }, "Use the specified GitHub organisation for forking.")
        .Flag(new[] { "--version=" }, "Use the specified <version> to override the value parsed from the URL or tag. Note that " +
                                      "`--version=0` can be used to delete an existing version override from a formula if it has become redundant.")
        .Flag(new[] { "--message=" }, "Prepend <message> to the default pull request message.")
        .Flag(new[] { "--url=" }, "Specify the <URL> for the new download. If a <URL> is specified, the <SHA-256> checksum of the new download should also be specified.")
        .Flag(new[] { "--sha256=" }, "Specify the <SHA-256> checksum of the new download.", dependsOn: "--url")
        .Flag(new[] { "--tag=" }, "Specify the new git commit <tag> for the formula.")
        .Flag(new[] { "--revision=" }, "Specify the new commit <revision> corresponding to the specified git <tag> or specified <version>.")
        .Switch(new[] { "-f", "--force" }, "Remove all mirrors if `--mirror` was not specified.")
        .Switch(new[] { "--install-dependencies" }, "Install missing dependencies required to update resources.")
        .Flag(new[] { "--python-package-name=" }, "Use the specified <package-name> when finding Python resources for <formula>. " +
                                                  "If no package name is specified, it will be inferred from the formula's stable URL.")
        .CommaArray("--python-extra-packages=", "Include these additional Python packages when finding resources.")
        .CommaArray("--python-exclude-packages=", "Exclude these Python packages when finding resources.")
        .CommaArray("--bump-synced=", null, hidden: true)
        .Flag(new[] { "--resource-versions=" }, "JSON-encoded resource version data from `brew bump`.", hidden: true)
        .Conflicts("--dry-run", "--write-only")
        .Conflicts("--no-audit", "--strict")
        .Conflicts("--no-audit", "--online")
        .Conflicts("--url", "--tag")
        .NamedArgs("formula", max: 1);

    // bump-formula-pr's update_url
    public static string UpdateUrl(string oldUrl, string oldVersion, string newVersion)
    {
        var newUrl = oldUrl.Replace(oldVersion, newVersion);
        if (oldVersion.Contains('.')) newUrl = newUrl.Replace(oldVersion.Replace('.', '_'), newVersion.Replace('.', '_'));
        var oldParts = oldVersion.Split('.');
        var newParts = newVersion.Split('.');
        if (oldParts.Length < 2 || newParts.Length != oldParts.Length) return newUrl;
        var partialOld = string.Join(".", oldParts[..^1]);
        var partialNew = string.Join(".", newParts[..^1]);
        if (partialOld.Length == 0 || partialNew.Length == 0) return newUrl;
        return Regex.Replace(newUrl, $"/(v?){Regex.Escape(partialOld)}/", m => $"/{m.Groups[1].Value}{partialNew}/");
    }

    static string DetermineMirror(string url)
    {
        if (Regex.IsMatch(url, ".*ftp\\.gnu\\.org/gnu.*")) return ReplaceFirst(url, "ftp.gnu.org/gnu", "ftpmirror.gnu.org");
        if (Regex.IsMatch(url, ".*download\\.savannah\\.gnu\\.org/*")) return ReplaceFirst(url, "download.savannah.gnu.org", "download-mirror.savannah.gnu.org");
        if (Regex.IsMatch(url, ".*www\\.apache\\.org/dyn/closer\\.lua\\?path=.*")) return ReplaceFirst(url, "www.apache.org/dyn/closer.lua?path=", "archive.apache.org/dist/");
        if (Regex.IsMatch(url, ".*mirrors\\.ocf\\.berkeley\\.edu/debian.*")) return ReplaceFirst(url, "mirrors.ocf.berkeley.edu/debian", "mirrorservice.org/sites/ftp.debian.org/debian");
        return null;
    }

    static string ReplaceFirst(string s, string a, string b)
    {
        int i = s.IndexOf(a, StringComparison.Ordinal);
        return i < 0 ? s : s[..i] + b + s[(i + a.Length)..];
    }

    public override void Run(ParsedArgs args)
    {
        bool dryRun = args.Switch("dry-run"), writeOnly = args.Switch("write-only");
        if (!string.IsNullOrEmpty(args.Flag("revision")) && args.Flag("tag") == null && args.Flag("version") == null)
            throw new UsageError("`--revision` must be passed with either `--tag` or `--version`!");
        if (args.Named.Count == 0) throw new UsageError("This command requires a formula argument.");

        var formula = DevTap.LoadFormula(args.Named[0]);
        if (formula.Disabled) throw new BrewError("This formula is disabled!");
        if (formula.DeprecationReason is RSymbol { Name: "does_not_build" }) throw new BrewError("This formula is deprecated and does not build!");
        var tap = DevTap.FromPath(formula.Path) ?? throw new BrewError("This formula is not in a tap!");
        var tapDir = DevTap.Path(tap);
        if (!DevTap.IsGitRepo(tapDir)) throw new BrewError("This formula's tap is not a Git repository!");

        var remoteRepo = BumpSupport.RemoteRepository(tapDir);
        if (remoteRepo == null && !writeOnly) throw new BrewError($"{tap} tap does not have a remote repository!");
        var gitRoot = DevTap.GitRead(tapDir, "rev-parse", "--show-toplevel").Trim();
        var relFile = Path.GetRelativePath(gitRoot.Length > 0 ? gitRoot : tapDir, formula.Path).Replace('\\', '/');
        bool official = tap.StartsWith("homebrew/");

        if (!writeOnly)
            BumpSupport.CheckForDuplicatePullRequests(formula.Name, remoteRepo, relFile, args.Quiet, state: "open", officialTap: official);

        var newUrl = args.Flag("url");
        var newVersion = args.Flag("version");
        void CheckNewVersion(string version = null, string url = null, string tag = null)
        {
            if (version == null)
            {
                if (string.IsNullOrEmpty(url)) return;
                version = Versions.Detect(url, tag);
                if (string.IsNullOrEmpty(version)) return;
            }
            if (!writeOnly) BumpSupport.CheckForDuplicatePullRequests(formula.Name, remoteRepo, relFile, args.Quiet, version: version, officialTap: official);
        }
        if (!string.IsNullOrEmpty(newVersion)) CheckNewVersion(version: newVersion);

        if (formula.Resources.Any(r => !(r.Name ?? "").StartsWith("homebrew-")))
            Out.Opoo("This formula has resources that may need to be updated.");

        var oldMirrors = formula.Mirrors.ToList();
        var newMirrors = args.Array("mirror")?.ToList();
        if (!string.IsNullOrEmpty(newUrl) && DetermineMirror(newUrl) is { } mirror)
        {
            newMirrors ??= new List<string> { mirror };
            if (newMirrors.Count == 0 && oldMirrors.Count > 0)
            {
                if (args.Switch("force")) Out.Opoo($"{formula.Name}: Removing all mirrors because a `--mirror=` argument was not specified.");
                else throw new BrewError($"{formula.Name}: a `--mirror=` argument for updating the mirror URL(s) was not specified.\nUse `--force` to remove all mirrors.");
            }
        }

        var oldContents = File.ReadAllText(formula.Path);
        var ast = new FormulaText(oldContents);
        var oldHash = formula.Sha256;
        var newHash = args.Flag("sha256");
        var newTag = args.Flag("tag");
        var newRevision = args.Flag("revision");
        var oldUrl = formula.Url ?? throw new BrewError($"{formula.Name}: no stable URL found!");
        var oldTag = ast.StableHashValue("url", "tag");
        var oldVersion = formula.Version;
        bool forcedVersion = !string.IsNullOrEmpty(newVersion);
        bool urlHash;
        if (!string.IsNullOrEmpty(newUrl) && !string.IsNullOrEmpty(newHash))
        {
            if (string.IsNullOrEmpty(newVersion)) CheckNewVersion(url: newUrl);
            urlHash = true;
        }
        else if (!string.IsNullOrEmpty(newTag) && !string.IsNullOrEmpty(newRevision))
        {
            if (string.IsNullOrEmpty(newVersion)) CheckNewVersion(url: oldUrl, tag: newTag);
            urlHash = false;
        }
        else if (string.IsNullOrEmpty(oldHash))
        {
            if (string.IsNullOrEmpty(newTag) && string.IsNullOrEmpty(newVersion) && string.IsNullOrEmpty(newRevision))
                throw new UsageError($"{formula.Name}: no `--tag` or `--version` argument specified!");
            if (oldTag != null)
            {
                newTag ??= oldTag.Replace(oldVersion, newVersion);
                if (newTag == oldTag) throw new BrewError($"You need to bump this formula manually since the new tag\nand old tag are both {newTag}.");
                if (string.IsNullOrEmpty(newVersion)) CheckNewVersion(url: oldUrl, tag: newTag);
                var tmp = Path.Combine(Path.GetTempPath(), "brew-bump-git-" + Guid.NewGuid().ToString("N")[..8]);
                try
                {
                    Out.Ohai($"Cloning {oldUrl}");
                    BumpSupport.Run(DevTap.Git, null, "clone", "--quiet", "--depth", "1", "--branch", newTag, oldUrl, tmp);
                    newRevision = DevTap.GitRead(tmp, "rev-parse", "-q", "--verify", "HEAD").Trim();
                }
                finally { if (Directory.Exists(tmp)) try { Directory.Delete(tmp, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
                forcedVersion = !string.IsNullOrEmpty(newVersion) && newVersion != Versions.Detect(oldUrl, newTag);
            }
            else if (string.IsNullOrEmpty(newRevision))
                throw new BrewError($"{formula.Name}: the current URL requires specifying a `--revision=` argument.");
            urlHash = false;
        }
        else if (string.IsNullOrEmpty(newUrl) && string.IsNullOrEmpty(newVersion))
            throw new UsageError($"{formula.Name}: no `--url` or `--version` argument specified!");
        else
        {
            if (string.IsNullOrEmpty(newUrl) && !string.IsNullOrEmpty(newVersion))
            {
                newUrl = UpdateUrl(oldUrl, oldVersion, newVersion);
                if ((newMirrors == null || newMirrors.Count == 0) && oldMirrors.Count > 0)
                    newMirrors = oldMirrors.Select(m => UpdateUrl(m, oldVersion, newVersion)).ToList();
            }
            if (newUrl == oldUrl)
                throw new BrewError($"You need to bump this formula manually since the new URL\nand old URL are both:\n  {newUrl}");
            if (string.IsNullOrEmpty(newUrl)) throw new BrewError("There was an issue generating the updated url, you may need to create the PR manually");
            if (string.IsNullOrEmpty(newVersion)) CheckNewVersion(url: newUrl);
            var resourceVersion = Versions.Parse(newUrl);
            forcedVersion = !string.IsNullOrEmpty(newVersion) && newVersion != resourceVersion;
            if (string.IsNullOrEmpty(newVersion) && string.IsNullOrEmpty(resourceVersion))
                throw new BrewError("Couldn't identify version, specify it using `--version=`.");
            newHash = DevHttp.FetchWithoutChecksum(newUrl, formula.Name, newVersion ?? resourceVersion).sha256;
            urlHash = true;
        }

        if (formula.Revision != 0 && ast.TopStanza("revision") != null) ast.RemoveStanza("revision");
        if (oldMirrors.Count > 0) ast.RemoveStableStanzas("mirror");
        if (urlHash)
        {
            ast.ReplaceStableValue("url", newUrl);
            ast.ReplaceStableValue("sha256", newHash);
        }
        else
        {
            if (newRevision == null) throw new BrewError($"{formula.Name}: no new revision found!");
            if (!string.IsNullOrEmpty(newTag)) ast.ReplaceStableHashValue("url", "tag", newTag);
            else if (!string.IsNullOrEmpty(newUrl)) ast.ReplaceStableValue("url", newUrl);
            ast.ReplaceStableHashValue("url", "revision", newRevision);
        }

        // Windows formulae usually pin `version` explicitly (their download names don't parse),
        // so keep that stanza in step with the new download.
        if (!forcedVersion && ast.HasStableStanza("version") && newVersion != "0")
        {
            newVersion ??= urlHash ? Versions.Parse(newUrl) : Versions.Detect(oldUrl, newTag);
            if (!string.IsNullOrEmpty(newVersion) && newVersion != oldVersion) forcedVersion = true;
        }

        var toAdd = new List<string>();
        if (!string.IsNullOrEmpty(newUrl) && newMirrors != null) toAdd.AddRange(newMirrors.Select(m => $"mirror {Create.Inspect(m)}"));
        if (forcedVersion && newVersion != null && newVersion != "0")
        {
            if (ast.HasStableStanza("version")) ast.ReplaceStableValue("version", newVersion);
            else toAdd.Add($"version {Create.Inspect(newVersion)}");
        }
        else if (forcedVersion && newVersion == "0" && ast.HasStableStanza("version")) ast.RemoveStableStanza("version");
        ast.AddStableStanzasAfter("url", toAdd);
        var newContents = ast.Process();

        var newFormulaVersion = BumpSupport.FromContents(formula.Name, newContents).Version;
        if (Versions.Compare(newFormulaVersion, oldVersion) < 0)
            throw new BrewError($"You need to bump this formula manually since changing the version\nfrom {oldVersion} to {newFormulaVersion} would be a downgrade.");
        if (Versions.Compare(newFormulaVersion, oldVersion) == 0)
            throw new BrewError($"You need to bump this formula manually since the new version\nand old version are both {newFormulaVersion}.");

        if (dryRun) Out.Puts(TextDiff.Unified(oldContents, newContents, relFile));
        else File.WriteAllText(formula.Path, newContents);

        var checkboxes = new List<string>();
        if (new FormulaText(newContents).Lines.Any(l => Regex.IsMatch(l, @"^\s*patch\b")))
        {
            Out.Opoo("This formula has patches that may be resolved upstream.");
            checkboxes.Add("- [ ] `patch` blocks have been checked.");
        }
        if (formula.Resources.Any(r => !(r.Name ?? "").StartsWith("homebrew-")))
            checkboxes.Add("- [ ] `resource` blocks have been checked for updates.");
        if (new[] { "TODO", "FIXME" }.Any(s => newContents.Contains(s + ":")))
            checkboxes.Add("- [ ] TODO and FIXME comments have been checked.");
        var formulaPrMessage = checkboxes.Count > 0 ? "\n\n" + string.Join("\n", checkboxes) + "\n" : "";

        if (RunAudit(args, formula))
        {
            if (!dryRun && !writeOnly) File.WriteAllText(formula.Path, oldContents);
            throw new BrewError($"`brew audit` failed for {formula.Name}!");
        }

        var prTitle = $"{formula.Name} {newFormulaVersion}";
        var prMessage = BumpSupport.PrMessage("bump-formula-pr", args.Flag("message"));
        if (formulaPrMessage.Length > 0) prMessage += formulaPrMessage + "<hr>";
        if (writeOnly && !args.Switch("commit")) return;

        var url = BumpSupport.CreatePr(tap, tapDir, remoteRepo ?? (writeOnly ? "" : null), $"bump-{formula.Name}-{newFormulaVersion}", prTitle, prMessage,
            new List<BumpSupport.Commit> { new(formula.Path, oldContents, $"{formula.Name} {newFormulaVersion}") },
            dryRun, args.Switch("no-fork") || writeOnly, args.Flag("fork-org"), args.Switch("commit"));
        if (string.IsNullOrEmpty(url)) return;
        if (args.Switch("no-browse")) Out.Puts(url);
        else BumpSupport.OpenBrowser(url);
    }

    // run_audit; true when the audit failed.
    static bool RunAudit(ParsedArgs args, FormulaSpec formula)
    {
        var auditArgs = new List<string> { "--formula" };
        if (args.Switch("strict")) auditArgs.Add("--strict");
        if (args.Switch("online")) auditArgs.Add("--online");
        if (args.Switch("dry-run"))
        {
            if (args.Switch("no-audit")) Out.Ohai("Skipping `brew audit`");
            else Out.Ohai($"brew audit {string.Join(" ", auditArgs)} {Path.GetFileName(formula.Path)}");
            return false;
        }
        if (args.Switch("no-audit"))
        {
            Out.Ohai("Skipping `brew audit`");
            return false;
        }
        auditArgs.Add(LivecheckEngine.FullName(formula));
        var self = Environment.ProcessPath;
        if (self != null && Path.GetFileNameWithoutExtension(self).Equals("brew", StringComparison.OrdinalIgnoreCase))
            return DevCommands.Exec(self, new[] { "audit" }.Concat(auditArgs)) != 0;
        if (DevCommands.Handles("audit"))
        {
            var failed = DevCommands.Failed;
            var code = DevCommands.Run("audit", auditArgs.ToArray());
            DevCommands.Failed = failed;
            return code != 0;
        }
        if (DevCommands.Which("brew") is { } brew) return DevCommands.Exec(brew, new[] { "audit" }.Concat(auditArgs)) != 0;
        Out.Opoo("Skipping `brew audit` because it is not available.");
        return false;
    }
}

[DevCommand("bump-revision")]
sealed class BumpRevision : DevCommand
{
    public override CommandParser Args() => new CommandParser("bump-revision")
        .Description("""
            Create a commit to increment the revision of <formula>. If no revision is
            present, "revision 1" will be added.
            """)
        .Switch(new[] { "-n", "--dry-run" }, "Print what would be done rather than doing it.")
        .Switch(new[] { "--remove-bottle-block" }, "Remove the bottle block in addition to bumping the revision.")
        .Switch(new[] { "--write-only" }, "Make the expected file modifications without taking any Git actions.")
        .Flag(new[] { "--message=" }, "Append <message> to the default commit message.")
        .Conflicts("--dry-run", "--write-only")
        .NamedArgs("formula", min: 1);

    public override void Run(ParsedArgs args)
    {
        var formulae = args.Named.Select(DevTap.LoadFormula).ToList();
        foreach (var formula in formulae)
        {
            int current = formula.Revision, next = current + 1;
            if (args.Switch("dry-run"))
            {
                if (!args.Quiet)
                {
                    var oldText = $"revision {current}";
                    var newText = $"revision {next}";
                    if (current == 0) Out.Ohai($"add {Create.Inspect(newText)}");
                    else Out.Ohai($"replace {Create.Inspect(oldText)} with {Create.Inspect(newText)}");
                }
            }
            else
            {
                var ast = new FormulaText(File.ReadAllText(formula.Path));
                if (current == 0) ast.AddStanza("revision", $"revision {next}");
                else ast.ReplaceStanza("revision", $"revision {next}");
                if (args.Switch("remove-bottle-block") && ast.TopStanza("bottle", block: true) != null) ast.RemoveStanza("bottle", block: true);
                File.WriteAllText(formula.Path, ast.Process());
            }

            var message = $"{formula.Name}: revision bump {args.Flag("message")}";
            if (args.Switch("dry-run"))
                Out.Ohai($"git commit --no-edit --verbose --message={message} -- {formula.Path}");
            else if (!args.Switch("write-only"))
                BumpSupport.SafeSystem(DevTap.Git, Path.GetDirectoryName(formula.Path)!, "commit", "--no-edit", "--verbose", $"--message={message}", "--", formula.Path);
        }
    }
}

[DevCommand("bump")]
sealed class Bump : DevCommand
{
    const string NewerThanUpstreamMsg = " (newer than upstream)";
    static readonly Regex LivecheckMessageRegex = new(@"^(?:error:|skipped|unable to get(?: throttled)? versions)", RegexOptions.IgnoreCase);

    public override CommandParser Args() => new CommandParser("bump")
        .Description("""
            Displays out-of-date packages and the latest version available. If the
            returned current and livecheck versions differ or when querying specific
            packages, also displays whether a pull request has been opened with the URL.
            """)
        .Switch(new[] { "--full-name" }, "Print formulae/casks with fully-qualified names.")
        .Switch(new[] { "--no-pull-requests" }, "Do not retrieve pull requests from GitHub.")
        .Switch(new[] { "--auto" }, "Read the list of formulae/casks from the tap autobump list.", hidden: true)
        .Switch(new[] { "--no-autobump" }, "Ignore formulae/casks in autobump list (official repositories only).")
        .Switch(new[] { "--formula", "--formulae" }, "Check only formulae.")
        .Switch(new[] { "--cask", "--casks" }, "Check only casks.")
        .Switch(new[] { "--eval-all" }, null, hidden: true)
        .Switch(new[] { "--repology" }, "Use Repology to check for outdated packages.")
        .Flag(new[] { "--tap=" }, "Check formulae and casks within the given tap, specified as <user>`/`<repo>.")
        .Switch(new[] { "--installed" }, "Check formulae and casks that are currently installed.")
        .Switch(new[] { "--no-fork" }, "Don't try to fork the repository.")
        .Switch(new[] { "--open-pr" }, "Open a pull request for the new version if none have been opened yet.")
        .Flag(new[] { "--start-with=" }, "Letter or word that the list of package results should alphabetically follow.")
        .Switch(new[] { "--bump-synced" }, "Bump additional formulae marked as synced with the given formulae.")
        .Conflicts("--formula", "--cask")
        .Conflicts("--tap", "--installed")
        .Conflicts("--tap", "--no-autobump")
        .Conflicts("--installed", "--eval-all")
        .Conflicts("--installed", "--auto")
        .Conflicts("--no-pull-requests", "--open-pr")
        .NamedArgs(new[] { "formula", "cask" });

    static bool Message(string v) => v != null && LivecheckMessageRegex.IsMatch(v);

    public override void Run(ParsedArgs args)
    {
        if (args.Switch("eval-all"))
            throw new BrewError("Calling `brew bump --eval-all` is disabled! Use the default trusted-tap behaviour instead.");
        var formulae = new List<FormulaSpec>();
        if (args.Switch("auto"))
        {
            if (!args.Switch("formula") && !args.Switch("cask")) throw new UsageError("`--formula` or `--cask` must be passed with `--auto`.");
            if (string.IsNullOrEmpty(args.Flag("tap"))) throw new UsageError("`--tap=` must be passed with `--auto`.");
            throw new UsageError($"No autobumped {(args.Switch("cask") ? "casks" : "formulae")} found.");
        }
        if (args.Flag("tap") is { } tapName)
        {
            var tap = DevTap.Normalize(tapName);
            if (tap.StartsWith("homebrew/")) throw new UsageError("`--tap` requires `--auto` for official taps.");
            var dir = DevTap.Path(tap);
            if (!args.Switch("cask")) formulae.AddRange(DevTap.FormulaFiles(dir).Select(FormulaLoader.LoadSpec));
        }
        else if (args.Switch("installed"))
        {
            if (!args.Switch("cask") && Directory.Exists(DevCommands.Cellar))
                foreach (var rack in Directory.GetDirectories(DevCommands.Cellar).Order(StringComparer.Ordinal))
                    if (Directory.GetDirectories(rack).Length > 0 && DevCommands.FormulaPath(Path.GetFileName(rack)) is { } p)
                        formulae.Add(FormulaLoader.LoadSpec(p));
        }
        else if (args.Named.Count > 0)
        {
            foreach (var n in args.Named)
            {
                if (args.Switch("cask")) throw new BrewError($"No available cask with the name \"{n}\".");
                formulae.Add(FormulaLoader.LoadSpec(DevTap.FormulaPath(n) ?? throw new BrewError($"No available formula or cask with the name \"{DevCommands.NameOf(n)}\".")));
            }
        }
        else if (!args.Switch("cask"))
        {
            foreach (var file in DevCommands.AllFormulaFiles())
            {
                try { formulae.Add(FormulaLoader.LoadSpec(file)); }
                catch (InvalidOperationException e) { Out.Opoo($"{Path.GetFileNameWithoutExtension(file)}: {e.Message}"); }
            }
        }

        if (args.Flag("start-with") is { } start) formulae = formulae.Where(f => f.Name.StartsWith(start, StringComparison.Ordinal)).ToList();
        formulae = formulae.OrderBy(f => f.Name, StringComparer.Ordinal).ToList();

        var ambiguous = args.Switch("full-name") ? new HashSet<FormulaSpec>()
            : formulae.GroupBy(f => f.Name).Where(g => g.Count() > 1).SelectMany(g => g).ToHashSet();
        for (int i = 0; i < formulae.Count; i++)
        {
            if (i > 0) Out.Puts();
            var f = formulae[i];
            if (SkipIneligible(f)) continue;
            var name = args.Switch("full-name") || ambiguous.Contains(f) ? LivecheckEngine.FullName(f) : f.Name;
            JsonArray repositories = null;
            if (!SkipRepology(args, f)) repositories = RepologyQuery(name);
            try { RetrieveAndDisplay(args, f, name, repositories); }
            catch (GitHubApiError e) { Out.Onoe($"{name}: skipped after a GitHub API error: {e.Message}"); }
        }
    }

    static bool SkipIneligible(FormulaSpec f)
    {
        if (!f.Disabled) return false;
        Out.Ohai(f.Name);
        Out.Puts("Formula is disabled so not accepting updates.");
        return true;
    }

    static bool SkipRepology(ParsedArgs args, FormulaSpec f) =>
        !args.Switch("repology") || (Environment.GetEnvironmentVariable("CI") is { Length: > 0 } && args.Switch("open-pr") && f.HasLivecheck) || LivecheckEngine.Versioned(f);

    static JsonArray RepologyQuery(string name)
    {
        try
        {
            var (content, _, _) = DevHttp.PageContent($"https://repology.org/api/v1/project/{Uri.EscapeDataString(name)}");
            return content == null ? null : JsonNode.Parse(content) as JsonArray;
        }
        catch (System.Text.Json.JsonException) { return null; }
    }

    static object RepologyLatest(JsonArray repos)
    {
        if (repos == null || repos.Count == 0) return "not found";
        var homebrew = repos.OfType<JsonObject>().ToList();
        if (homebrew.Any(r => (string)r["status"] == "unique")) return "present only in Homebrew";
        var newest = homebrew.FirstOrDefault(r => (string)r["status"] == "newest");
        return newest == null ? "no latest version" : new VersionValue((string)newest["version"]);
    }

    sealed record VersionValue(string V)
    {
        public override string ToString() => V;
    }

    // bump.rb livecheck_result: a version, or a message string.
    static (string value, bool isVersion) LivecheckResult(FormulaSpec f)
    {
        try
        {
            var (referenced, refs) = LivecheckEngine.ResolveReference(f, false, false);
            var skip = referenced != null ? LivecheckEngine.ReferencedSkipInformation(referenced, f.Name, false) : null;
            skip ??= LivecheckEngine.SkipInformation(f, f.Name);
            if (skip != null)
            {
                var status = (string)skip["status"];
                var messages = skip["messages"] as JsonArray;
                var msg = messages is { Count: > 0 } ? string.Join("; ", messages.Select(m => (string)m)) : null;
                if (status == "error" && msg != null) return ($"error: {msg}", false);
                return ($"skipped - {msg ?? status}", false);
            }
            var info = LivecheckEngine.LatestVersion(f, referenced, refs, json: true, fullName: false, verbose: true, debug: false);
            if (info?.Latest == null) return ("unable to get versions", false);
            return (info.Latest, true);
        }
        catch (Exception e) when (e is BrewError or LivecheckError or GitHubApiError or InvalidOperationException or IOException or HttpRequestException)
        {
            return ($"error: {e.Message}", false);
        }
    }

    static string RetrievePullRequests(FormulaSpec f, string name, string state = null, string version = null)
    {
        var tap = DevTap.FromPath(f.Path);
        var repo = tap == null ? null : BumpSupport.RemoteRepository(DevTap.Path(tap));
        if (repo == null) return null;
        var prs = BumpSupport.FetchPullRequests(name, repo, state, version);
        if (string.IsNullOrEmpty(version))
            prs = prs.Where(pr => (string)pr["title"] is { } t && t.StartsWith($"{name} ") && !t.Contains(": ")).ToList();
        return prs.Count == 0 ? null : string.Join(", ", prs.Select(pr => $"{pr["title"]} ({Formatter.Url((string)pr["html_url"])})"));
    }

    void RetrieveAndDisplay(ParsedArgs args, FormulaSpec f, string name, JsonArray repositories)
    {
        var current = f.Version;
        var repologyLatest = repositories != null ? RepologyLatest(repositories) : "not found";
        var (livecheckLatest, isVersion) = LivecheckResult(f);

        string newVersion;
        if ((isVersion && Versions.Compare(livecheckLatest, current) >= 0) || current == "latest" || Message(livecheckLatest))
            newVersion = livecheckLatest;
        else if (repologyLatest is VersionValue rv && !f.HasLivecheck && Versions.Compare(rv.V, current) > 0)
            newVersion = rv.V;
        else newVersion = null;
        if (newVersion == null && isVersion) newVersion = livecheckLatest;
        if (newVersion == null && repologyLatest is VersionValue rv2 && !f.HasLivecheck) newVersion = rv2.V;
        newVersion ??= "unable to get versions";

        bool newIsMessage = Message(newVersion);
        bool newerThanUpstream = !newIsMessage && Versions.Compare(current, newVersion) > 0;
        bool versionsEqual = newVersion == current;

        string duplicates = null, openBump = null;
        if (!args.Switch("no-pull-requests") && !newerThanUpstream && !newIsMessage && newVersion != current)
        {
            duplicates = RetrievePullRequests(f, name, version: newVersion);
            if (duplicates == null) openBump = RetrievePullRequests(f, name, state: "open");
        }

        var title = (repologyLatest is not VersionValue || repologyLatest.ToString() == current) && versionsEqual
            ? $"{name} {Tty.Green}is up to date!{Tty.Reset}"
            : name;
        var currentS = current + (newerThanUpstream ? NewerThanUpstreamMsg : "") + (f.Deprecated ? " (deprecated)" : "");
        Out.Ohai(title);
        Out.Puts($"Current formula version:  {currentS}");
        Out.Puts($"Latest livecheck version: {newVersion}");
        if (!SkipRepology(args, f)) Out.Puts($"Latest Repology version:  {repologyLatest}");

        if (!args.Switch("no-pull-requests") && !newIsMessage && !versionsEqual && !newerThanUpstream)
        {
            string dupText, openText = null;
            if (duplicates != null) dupText = duplicates;
            else if (openBump != null) { dupText = "none"; openText = openBump; }
            else { dupText = "none"; openText = "none"; }
            Out.Puts($"Duplicate pull requests:  {dupText}");
            if (openText != null) Out.Puts($"Open bump pull requests:  {openText}");
        }

        if (!args.Switch("open-pr") || newIsMessage || newerThanUpstream) return;
        if (repologyLatest is VersionValue r && Versions.Compare(r.V, current) > 0 && Versions.Compare(r.V, newVersion) > 0 && f.HasLivecheck)
            Out.Puts($"{name} was not bumped to the Repology version because it has a `livecheck` block.");
        if (versionsEqual || !isVersion && repologyLatest is not VersionValue) return;
        if (!string.IsNullOrEmpty(duplicates) || !string.IsNullOrEmpty(openBump)) return;

        var bumpArgs = new List<string> { name, $"--version={newVersion}", "--no-browse", "--message=Created by `brew bump`" };
        if (args.Switch("no-fork")) bumpArgs.Add("--no-fork");
        var self = Environment.ProcessPath;
        int code;
        if (self != null && Path.GetFileNameWithoutExtension(self).Equals("brew", StringComparison.OrdinalIgnoreCase))
            code = DevCommands.Exec(self, new[] { "bump-formula-pr" }.Concat(bumpArgs));
        else
        {
            var failed = DevCommands.Failed;
            code = DevCommands.Run("bump-formula-pr", bumpArgs.ToArray());
            DevCommands.Failed = failed;
        }
        if (code != 0) DevCommands.Failed = true;
    }
}
