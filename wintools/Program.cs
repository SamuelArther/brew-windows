using System.Runtime.CompilerServices;

namespace WinTools;

// Multi-call entry point: `wintools <command> [args]`, or a copy/link of wintools.exe named after the
// command. Every command registers itself from its own file with a [ModuleInitializer].
public static class Program
{
    public static int Main(string[] args)
    {
        var self = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "wintools");
        if (!self.Equals("wintools", StringComparison.OrdinalIgnoreCase) && Commands.All.ContainsKey(self))
            return Commands.Run(self, args);
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage: wintools <command> [arguments]\n\nCommands:");
            foreach (var name in Commands.All.Keys.Order(StringComparer.Ordinal))
                Console.WriteLine($"  {name,-24}{Commands.Descriptions.GetValueOrDefault(name)}");
            return 0;
        }
        if (args[0] == "--version") { Console.WriteLine(Commands.Version); return 0; }
        if (!Commands.All.ContainsKey(args[0]))
        {
            Console.Error.WriteLine($"wintools: unknown command '{args[0]}'");
            return 127;
        }
        return Commands.Run(args[0], args[1..]);
    }
}

public static class Commands
{
    public static readonly Dictionary<string, Func<string[], int>> All = new(StringComparer.OrdinalIgnoreCase);
    public static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase);

    public static string Version => typeof(Commands).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    // Called from each command file's module initializer.
    public static void Add(string name, string description, Func<string[], int> run)
    {
        All[name] = run;
        Descriptions[name] = description;
    }

    public static int Run(string name, string[] args)
    {
        try { return All[name](args); }
        catch (UsageException e)
        {
            Console.Error.WriteLine($"{name}: {e.Message}");
            return 2;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException
                                       or System.ComponentModel.Win32Exception or ArgumentException)
        {
            Console.Error.WriteLine($"{name}: {e.Message}");
            return 1;
        }
    }
}

// Bad command-line usage: printed as "<command>: <message>", exit status 2.
public sealed class UsageException(string message) : Exception(message);
