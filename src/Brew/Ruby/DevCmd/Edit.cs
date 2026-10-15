using System.Text;

namespace Brew.Ruby.DevCmd;

// Port of Utils::Editor. $HOMEBREW_EDITOR, then $EDITOR, then $VISUAL (as bin/brew exports them),
// else the first of code/codium/cursor/code-insiders/subl on PATH, else notepad.exe.
static class Editor
{
    public static string Command(bool silent = false)
    {
        foreach (var v in new[] { "HOMEBREW_EDITOR", "EDITOR", "VISUAL" })
            if (Environment.GetEnvironmentVariable(v) is { Length: > 0 } e && e != "0") return e;
        var editor = new[] { "code", "codium", "cursor", "code-insiders", "subl" }.FirstOrDefault(c => DevCommands.Which(c) != null) ?? "notepad.exe";
        if (!silent)
            Out.Opoo($"Using {editor} because no editor was set in the environment.\nThis may change in the future, so we recommend setting `$EDITOR`\nor `$HOMEBREW_EDITOR` to your preferred text editor.");
        return editor;
    }

    // String#shellsplit
    public static List<string> ShellSplit(string s)
    {
        var words = new List<string>();
        var cur = new StringBuilder();
        bool inWord = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c))
            {
                if (inWord) { words.Add(cur.ToString()); cur.Clear(); inWord = false; }
                continue;
            }
            inWord = true;
            if (c == '\'')
            {
                int j = s.IndexOf('\'', i + 1);
                if (j < 0) throw new BrewError($"Unmatched quote: {s}");
                cur.Append(s, i + 1, j - i - 1);
                i = j;
            }
            else if (c == '"')
            {
                int j = i + 1;
                for (; j < s.Length && s[j] != '"'; j++)
                {
                    if (s[j] == '\\' && j + 1 < s.Length && s[j + 1] is '"' or '\\' or '$' or '`') j++;
                    cur.Append(s[j]);
                }
                if (j >= s.Length) throw new BrewError($"Unmatched quote: {s}");
                i = j;
            }
            else if (c == '\\' && i + 1 < s.Length && !OperatingSystem.IsWindows()) cur.Append(s[++i]);
            else cur.Append(c);
        }
        if (inWord) words.Add(cur.ToString());
        return words;
    }

    public static void Open(params string[] files)
    {
        Out.Puts($"Editing {string.Join("\n", files)}");
        var words = ShellSplit(Command());
        if (words.Count == 0) throw new BrewError("No editor set.");
        var exe = DevCommands.Which(words[0]) ?? (File.Exists(words[0]) ? words[0] : null);
        var cmdline = string.Join(" ", words.Concat(files));
        if (exe == null) throw new BrewError($"Failure while executing; `{cmdline}` exited with 127.");
        Console.Out.Flush();
        int code;
        try { code = DevCommands.Exec(exe, words.Skip(1).Concat(files)); }
        catch (System.ComponentModel.Win32Exception) { code = 127; }
        if (code != 0) throw new BrewError($"Failure while executing; `{cmdline}` exited with {code}.");
    }
}

[DevCommand("edit")]
sealed class Edit : DevCommand
{
    public override CommandParser Args() => new CommandParser("edit")
        .Description("""
            Open a <formula>, <cask> or <tap> in the editor set by `$EDITOR` or `$HOMEBREW_EDITOR`,
            or open the Homebrew repository for editing if no argument is provided.
            """)
        .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
        .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
        .Switch(new[] { "--print-path" }, "Print the file path to be edited, without opening an editor.")
        .Conflicts("--formula", "--cask")
        .NamedArgs(new[] { "formula", "cask", "tap" });

    static bool CoreFormulaPath(string p) =>
        System.Text.RegularExpressions.Regex.IsMatch(p.Replace('\\', '/'), @"/homebrew-core/Formula/.*\.rb$");

    static bool CoreCaskPath(string p) =>
        System.Text.RegularExpressions.Regex.IsMatch(p.Replace('\\', '/'), @"/homebrew-cask/Casks/.*\.rb$");

    static bool SamePath(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)), StringComparison.OrdinalIgnoreCase);

    // args.named.to_paths
    static string ToPath(string name, bool cask, bool formula)
    {
        if (name.EndsWith(".rb", StringComparison.OrdinalIgnoreCase) && (File.Exists(name) || name.Contains('/') || name.Contains('\\')))
            return Path.GetFullPath(name);
        var parts = name.Split('/');
        if (parts.Length == 2 && !cask && !formula) return DevTap.Path(name);
        if (cask)
        {
            var token = parts[^1].ToLowerInvariant();
            var tapDir = parts.Length == 3 ? DevTap.Path(parts[0] + "/" + parts[1]) : DevTap.Path("homebrew/cask");
            return Path.Combine(tapDir, "Casks", token[..1], token + ".rb");
        }
        if (parts.Length == 3)
        {
            var tapDir = DevTap.Path(parts[0] + "/" + parts[1]);
            var n = parts[2].ToLowerInvariant();
            var found = Directory.Exists(tapDir) ? DevTap.FormulaFiles(tapDir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == n) : null;
            return found ?? DevTap.NewFormulaPath(parts[0] + "/" + parts[1], n);
        }
        return DevCommands.FormulaPath(name) ?? DevCommands.ExpectedFormulaPath(name);
    }

    public override void Run(ParsedArgs args)
    {
        if (!Directory.Exists(Path.Combine(DevCommands.Repository, ".git")))
            throw new BrewError("Changes will be lost!\nThe first time you `brew update`, all local changes will be lost; you should\nthus `brew update` before you `brew edit`!");

        List<string> paths;
        if (args.Named.Count == 0)
        {
            var editor = Editor.Command(silent: true);
            paths = editor == "subl"
                ? new List<string> { "--project", Path.Combine(DevCommands.Repository, ".sublime", "homebrew.sublime-project") }
                : new List<string> { DevCommands.Repository };
        }
        else
        {
            paths = args.Named.Select(n => ToPath(n, args.Switch("cask"), args.Switch("formula"))).ToList();
            foreach (var p in paths)
                if (!File.Exists(p) && !Directory.Exists(p)) RaiseWithMessage(p, args.Switch("cask"));
        }

        if (args.Switch("print-path"))
        {
            foreach (var p in paths) Out.Puts(p);
            return;
        }

        Editor.Open(paths.ToArray());

        bool isFormula = false;
        if (Environment.GetEnvironmentVariable("HOMEBREW_NO_ENV_HINTS") is not { Length: > 0 } && paths.Any(p =>
            {
                if (p == "--project") return false;
                isFormula = CoreFormulaPath(p);
                return isFormula || CoreCaskPath(p) || SamePath(p, DevTap.Path("homebrew/core")) || SamePath(p, DevTap.Path("homebrew/cask"));
            }))
        {
            var fromSource = isFormula ? " --build-from-source" : "";
            var noApi = Environment.GetEnvironmentVariable("HOMEBREW_NO_INSTALL_FROM_API") is { Length: > 0 } ? "" : "HOMEBREW_NO_INSTALL_FROM_API=1 ";
            Out.Puts($"To test your local edits, run:\n  {noApi}brew install{fromSource} --verbose --debug {string.Join(" ", args.Named)}");
        }
    }

    static void RaiseWithMessage(string path, bool cask)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        string command, action;
        if (System.Text.RegularExpressions.Regex.IsMatch(path.Replace('\\', '/'), @"/tap/[^/]+$"))
            throw new BrewError(DevTap.TapUnavailableMessage(DevTap.FromPath(path) ?? path));
        if (cask || CoreCaskPath(path))
        {
            command = $"brew create --cask --set-name {name} $URL";
            action = "create a new cask";
        }
        else
        {
            command = $"brew create --set-name {name} $URL";
            action = "create a new formula";
        }
        throw new UsageError($"{name} doesn't exist on disk.\nRun {Formatter.Identifier(command)} to {action}!");
    }
}
