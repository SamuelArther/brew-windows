namespace Brew;

public static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var paths = new Paths();
            if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
            {
                Console.WriteLine("Example usage:\n  brew install FORMULA...\n  brew list [FORMULA...]\n  brew uninstall FORMULA...\n  brew link FORMULA...\n  brew unlink FORMULA...");
                return 0;
            }
            if (args[0] is "--version" or "-v") { Console.WriteLine("Homebrew 7.0.9 (Windows port)"); return 0; }
            if (args[0] is "--prefix" or "--cellar" or "--repository")
            {
                Console.WriteLine(args[0] == "--prefix" ? paths.Prefix : args[0] == "--cellar" ? paths.Cellar : paths.Repository);
                return 0;
            }
            var command = args[0];
            var allowed = command switch
            {
                "list" or "ls" => new[] { "--formula", "--formulae", "--versions", "-1" },
                "uninstall" or "remove" or "rm" => new[] { "--formula", "--force", "-f" },
                _ => new[] { "--formula" }
            };
            var unknown = args.Skip(1).FirstOrDefault(a => a.StartsWith('-') && !allowed.Contains(a));
            if (unknown != null) throw new InvalidOperationException($"invalid option: {unknown}");
            var names = args.Skip(1).Where(a => !a.StartsWith('-')).Select(Paths.ValidateName).ToArray();
            var manager = new PackageManager(paths);
            // One writer per prefix, including between separate brew processes.
            Directory.CreateDirectory(paths.Prefix);
            using var gate = new FileStream(Path.Combine(paths.Prefix, ".brew.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            switch (command)
            {
                case "install":
                    RequireNames(names, command);
                    foreach (var name in names) await manager.InstallAsync(name);
                    break;
                case "list": case "ls": manager.List(names, args.Contains("--versions")); break;
                case "uninstall": case "remove": case "rm":
                    RequireNames(names, command);
                    foreach (var name in names) manager.Uninstall(name, args.Contains("--force") || args.Contains("-f"));
                    break;
                case "link": case "unlink":
                    RequireNames(names, command);
                    foreach (var name in names) manager.Link(name, command == "unlink");
                    break;
                default: throw new InvalidOperationException($"Unknown command: brew {command}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static void RequireNames(string[] names, string command)
    {
        if (names.Length == 0) throw new InvalidOperationException($"Invalid usage: brew {command} requires at least one formula argument.");
    }
}
