using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// xcode-kotlin: Kotlin/Native debugging and syntax support for Windows' Xcode. The plugin's two
// halves map to LLDB and Visual Studio: `install` copies the Kotlin/Native LLDB formatters
// (konan_lldb.py from a local Kotlin/Native distribution) into %LOCALAPPDATA%\xcode-kotlin and imports
// them from ~/.lldbinit (read by lldb and lldb-dap, which VS Code's debuggers use), and puts a Kotlin
// TextMate grammar where Visual Studio loads extra languages (~\.vs\Extensions\Kotlin\Syntaxes).
static class XcodeKotlin
{
    const string Version = "2.2.1";
    const string Marker = "# xcode-kotlin";

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("xcode-kotlin", "Kotlin/Native LLDB formatters and Visual Studio syntax support", Run);

    // The environment first, so brew test (and other sandboxes) can point the profile elsewhere.
    static string Home => Environment.GetEnvironmentVariable("USERPROFILE") is { Length: > 0 } p ? p : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    static string PluginDir => Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") is { Length: > 0 } l ? l : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "xcode-kotlin");
    static string Formatter => Path.Combine(PluginDir, "konan_lldb.py");
    static string VersionFile => Path.Combine(PluginDir, "version");
    static string LldbInit => Path.Combine(Home, ".lldbinit");
    // LLDB reads ~/.lldbinit-<program> instead of ~/.lldbinit when it exists (Xcode's is .lldbinit-Xcode).
    static string IdeLldbInit => Path.Combine(Home, ".lldbinit-lldb-dap");
    static string LanguageSpec => Path.Combine(Home, ".vs", "Extensions", "Kotlin", "Syntaxes", "Kotlin.tmLanguage.json");

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Help(); return 0; }
        var rest = args[1..];
        return args[0] switch
        {
            "--version" or "version" => Print(Version),
            "info" => Info(rest.Contains("--only")),
            "install" => Install(rest),
            "sync" => Sync(rest),
            "uninstall" => Uninstall(),
            _ => throw new UsageException($"unknown command '{args[0]}' (see xcode-kotlin --help)"),
        };
    }

    static int Print(string s) { Console.WriteLine(s); return 0; }

    static string InstalledVersion()
    {
        try { return File.Exists(VersionFile) && File.Exists(Formatter) ? File.ReadAllText(VersionFile).Trim() : null; }
        catch (IOException) { return null; }
    }

    static bool Contains(string file, string text)
    {
        try { return File.Exists(file) && File.ReadAllText(file).Contains(text, StringComparison.Ordinal); }
        catch (IOException) { return false; }
    }

    static string ImportLine => $"command script import \"{Formatter.Replace('\\', '/')}\"";
    static bool LldbInitInstalled => Contains(LldbInit, ImportLine);
    static bool IdeSourcesMain => Contains(IdeLldbInit, "command source ~/.lldbinit");

    static int Info(bool only)
    {
        Console.WriteLine($"Bundled plugin version:\t\t{Version}");
        Console.WriteLine($"Installed plugin version:\t{InstalledVersion() ?? "none"}");
        Console.WriteLine($"Language spec installed:\t{(File.Exists(LanguageSpec) ? "Yes" : "No")}");
        Console.WriteLine($"LLDB init installed:\t\t{(LldbInitInstalled ? "Yes" : "No")}");
        Console.WriteLine($"LLDB Xcode init sources main LLDB init:\t{(IdeSourcesMain ? "Yes" : "No")}");
        if (only) return 0;
        Console.WriteLine();
        Console.WriteLine("Kotlin/Native distributions:");
        foreach (var dist in Distributions()) Console.WriteLine($"  {dist}");
        Console.WriteLine("Visual Studio installations:");
        foreach (var vs in XcodeVersions.Instances()) Console.WriteLine($"  {vs.Version} ({vs.DisplayName}) {vs.Path}");
        return 0;
    }

    // KONAN_HOME, KONAN_DATA_DIR's or ~/.konan's kotlin-native-prebuilt-* folders, newest first.
    static List<string> Distributions()
    {
        var found = new List<string>();
        if (Environment.GetEnvironmentVariable("KONAN_HOME") is { Length: > 0 } konanHome) found.Add(konanHome);
        var data = Environment.GetEnvironmentVariable("KONAN_DATA_DIR") is { Length: > 0 } d ? d : Path.Combine(Home, ".konan");
        if (Directory.Exists(data))
            found.AddRange(Directory.GetDirectories(data, "kotlin-native-*").OrderByDescending(p => p, StringComparer.Ordinal));
        return found.Where(p => File.Exists(Path.Combine(p, "tools", "konan_lldb.py"))).ToList();
    }

    static int Install(string[] args)
    {
        string source = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--formatter" or "--konan-home")
            {
                var value = i + 1 < args.Length ? args[++i] : throw new UsageException($"{args[i]} needs a path");
                source = Directory.Exists(value) ? Path.Combine(value, "tools", "konan_lldb.py") : value;
            }
            else if (args[i] is not ("-f" or "--force")) throw new UsageException($"unknown option {args[i]}");
        }
        source ??= Distributions().Select(dist => Path.Combine(dist, "tools", "konan_lldb.py")).FirstOrDefault();
        if (source == null || !File.Exists(source))
        {
            Console.Error.WriteLine("xcode-kotlin: no Kotlin/Native distribution with tools\\konan_lldb.py was found.");
            Console.Error.WriteLine("Build a Kotlin/Native target once (Gradle downloads it to ~\\.konan), set KONAN_HOME,");
            Console.Error.WriteLine("or pass --konan-home <distribution> / --formatter <konan_lldb.py>.");
            return 1;
        }

        Directory.CreateDirectory(PluginDir);
        File.Copy(source, Formatter, overwrite: true);
        File.WriteAllText(VersionFile, Version + "\n");
        Console.WriteLine($"Installed LLDB formatters from {source}");

        if (!LldbInitInstalled)
        {
            RemoveMarked(LldbInit);
            File.AppendAllText(LldbInit, $"{Marker}\n{ImportLine}\n");
            Console.WriteLine($"Added the formatters to {LldbInit}");
        }
        if (!IdeSourcesMain)
        {
            File.AppendAllText(IdeLldbInit, $"{Marker}\ncommand source ~/.lldbinit\n");
            Console.WriteLine($"{IdeLldbInit} now sources ~/.lldbinit");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(LanguageSpec));
        File.WriteAllText(LanguageSpec, Grammar);
        Console.WriteLine($"Installed the Kotlin language spec for Visual Studio ({LanguageSpec})");
        return 0;
    }

    // Refresh the formatters from the newest Kotlin/Native distribution.
    static int Sync(string[] args)
    {
        if (InstalledVersion() == null) { Console.Error.WriteLine("xcode-kotlin is not installed; run `xcode-kotlin install` first."); return 1; }
        return Install(args);
    }

    static int Uninstall()
    {
        RemoveMarked(LldbInit);
        RemoveMarked(IdeLldbInit);
        if (File.Exists(IdeLldbInit) && File.ReadAllText(IdeLldbInit).Trim().Length == 0) File.Delete(IdeLldbInit);
        if (Directory.Exists(PluginDir)) Directory.Delete(PluginDir, recursive: true);
        var specDir = Path.GetDirectoryName(Path.GetDirectoryName(LanguageSpec));
        if (Directory.Exists(specDir)) Directory.Delete(specDir, recursive: true);
        Console.WriteLine("Uninstalled the Kotlin LLDB formatters and language spec.");
        return 0;
    }

    // Drop the line after each "# xcode-kotlin" marker, and the marker.
    static void RemoveMarked(string file)
    {
        if (!File.Exists(file)) return;
        var lines = File.ReadAllLines(file).ToList();
        var output = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i] == Marker) { i++; continue; }
            output.Append(lines[i]).Append('\n');
        }
        File.WriteAllText(file, output.ToString());
    }

    static void Help() => Console.WriteLine("""
        Usage: xcode-kotlin <command> [options]

        Kotlin/Native debugging and syntax support for Windows' Xcode (LLDB and Visual Studio).

        Commands:
          install [--konan-home <dir> | --formatter <konan_lldb.py>]
                         Install the Kotlin/Native LLDB formatters (imported from ~/.lldbinit) and
                         the Kotlin language spec for Visual Studio
          sync           Refresh the formatters from the newest Kotlin/Native distribution
          uninstall      Remove everything install added
          info [--only]  Show what is installed (--only: just the status lines)
          version        Print the bundled plugin version

        The formatters come from a Kotlin/Native distribution's tools\konan_lldb.py (KONAN_HOME, or the
        ones Gradle downloads to ~\.konan).
        """);

    // A compact TextMate grammar for Kotlin: comments, strings with templates, annotations, numbers,
    // keywords and declarations.
    const string Grammar = """
        {
          "name": "Kotlin",
          "scopeName": "source.kotlin",
          "fileTypes": ["kt", "kts"],
          "patterns": [
            { "name": "comment.block.documentation.kotlin", "begin": "/\\*\\*", "end": "\\*/" },
            { "name": "comment.block.kotlin", "begin": "/\\*", "end": "\\*/" },
            { "name": "comment.line.double-slash.kotlin", "match": "//.*$" },
            { "name": "string.quoted.triple.kotlin", "begin": "\"\"\"", "end": "\"\"\"",
              "patterns": [{ "include": "#template" }] },
            { "name": "string.quoted.double.kotlin", "begin": "\"", "end": "\"",
              "patterns": [{ "name": "constant.character.escape.kotlin", "match": "\\\\." }, { "include": "#template" }] },
            { "name": "string.quoted.single.kotlin", "match": "'(\\\\.|[^'\\\\])'" },
            { "name": "storage.type.annotation.kotlin", "match": "@[A-Za-z_][A-Za-z0-9_.:]*" },
            { "name": "constant.numeric.kotlin", "match": "\\b(0[xX][0-9A-Fa-f_]+|0[bB][01_]+|[0-9][0-9_]*(\\.[0-9_]+)?([eE][+-]?[0-9]+)?)[LuUfF]*\\b" },
            { "name": "constant.language.kotlin", "match": "\\b(true|false|null|this|super)\\b" },
            { "match": "\\b(class|interface|object|typealias|fun|val|var)\\s+([A-Za-z_][A-Za-z0-9_]*)",
              "captures": { "1": { "name": "storage.type.kotlin" }, "2": { "name": "entity.name.kotlin" } } },
            { "name": "storage.modifier.kotlin", "match": "\\b(public|private|protected|internal|open|final|abstract|sealed|data|enum|annotation|companion|inline|noinline|crossinline|reified|suspend|override|lateinit|const|external|expect|actual|operator|infix|tailrec|vararg|inner|value)\\b" },
            { "name": "keyword.control.kotlin", "match": "\\b(if|else|when|for|while|do|return|break|continue|throw|try|catch|finally)\\b" },
            { "name": "keyword.other.kotlin", "match": "\\b(package|import|as|is|in|out|by|where|init|constructor|get|set|field|it)\\b" },
            { "name": "storage.type.kotlin", "match": "\\b(class|interface|object|typealias|fun|val|var)\\b" }
          ],
          "repository": {
            "template": {
              "patterns": [
                { "name": "variable.other.template.kotlin", "match": "\\$[A-Za-z_][A-Za-z0-9_]*" },
                { "name": "meta.template.expression.kotlin", "begin": "\\$\\{", "end": "\\}" }
              ]
            }
          }
        }
        """;
}
