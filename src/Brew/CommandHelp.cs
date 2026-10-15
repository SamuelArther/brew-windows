using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace Brew;

public static class CommandHelp
{
    // Homebrew 7.0.9 Library/Homebrew/help.sh (BSD-2-Clause; see THIRD_PARTY_NOTICES.txt).
    public const string General = """
        Example usage:
          brew search TEXT|/REGEX/
          brew info [FORMULA|CASK...]
          brew install FORMULA|CASK...
          brew update
          brew upgrade [FORMULA|CASK...]
          brew uninstall FORMULA|CASK...
          brew list [FORMULA|CASK...]

        Troubleshooting:
          brew config
          brew doctor
          brew install --verbose --debug FORMULA|CASK

        Contributing:
          brew create URL [--no-fetch]
          brew edit [FORMULA|CASK...]

        Further help:
          brew commands
          brew help [COMMAND]
          man brew
          https://docs.brew.sh
        """;

    public static readonly string[] UserCommands = "install uninstall reinstall upgrade outdated list info search deps uses leaves missing autoremove link unlink pin unpin cleanup fetch update doctor config shellenv commands help home desc options tap untap tap-info log".Split(' ');
    public static readonly HashSet<string> RubyCommands = "create edit cat formula audit style livecheck test bump bump-formula-pr bump-revision extract unpack linkage irb ruby sh readall options".Split(' ').ToHashSet();
    public static readonly string[] NativeDevCommands = "tap-new pr-pull pr-publish pr-upload bottle dispatch-build-bottle release contributions lgtm generate-formula-api generate-cask-api generate-internal-api generate-advisories-api generate-analytics-api update-maintainers update-sponsors update-license-data update-python-resources update-perl-resources update-portable-ruby update-test vendor-gems test-bot determine-test-runners".Split(' ');
    public static IEnumerable<string> All => UserCommands.Concat(RubyCommands).Concat(NativeDevCommands).Distinct().Order(StringComparer.Ordinal);

    public static int Unknown(string command, IEnumerable<string> aliases = null)
    {
        Console.Error.WriteLine(General);
        Console.Error.WriteLine();
        var suggestions = Suggestions.Correct(command, All.Concat(aliases ?? []));
        CommandOutput.Error($"Invalid usage: Unknown command: brew {command}"
            + (suggestions.Length == 0 ? "" : "\nDid you mean " + Suggestions.Sentence(suggestions, "or") + "?"));
        return 1;
    }

    public static async Task<int> Print(string command, string[] args)
    {
        if (command == null) { Console.WriteLine(General); return 0; }
        if (RubyCommands.Contains(command)) return await DevBridge.Run(command, [.. args, "--help"]);
        var type = typeof(Cli).Assembly.GetType("Brew.Ruby.DevCommands");
        var help = type?.GetMethod("TryHelp", [typeof(string), typeof(string[])]);
        if (help != null && (bool)DevBridge.Invoke(help, [command, args])) return 0;
        using var resource = typeof(Cli).Assembly.GetManifestResourceStream("command-help.json");
        if (resource != null)
        {
            using var document = JsonDocument.Parse(resource);
            if (document.RootElement.TryGetProperty(command, out var entry))
            {
                Console.WriteLine($"Usage: brew {command} [options]" + (entry.TryGetProperty("arguments", out var named) ? " " + named.GetString() : ""));
                Console.WriteLine(); Console.WriteLine(entry.GetProperty("description").GetString());
                return 0;
            }
        }
        if (!All.Contains(command)) return Unknown(command);
        Console.WriteLine($"Usage: brew {command} [options]");
        return 0;
    }
}

public static class DevBridge
{
    public static object Invoke(MethodInfo method, object[] args)
    {
        try { return method.Invoke(null, args); }
        catch (TargetInvocationException ex) { ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw(); throw; }
    }
    public static async Task<int> Run(string command, string[] args)
    {
        var type = typeof(Cli).Assembly.GetType("Brew.Ruby.DevCommands");
        var method = type?.GetMethod("Run", [typeof(string), typeof(string[])]);
        if (method == null) throw new InvalidOperationException($"Developer command runtime is not available: brew {command}");
        return Invoke(method, [command, args]) switch { Task<int> task => await task, int code => code, _ => 0 };
    }
}
