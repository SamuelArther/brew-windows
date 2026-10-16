using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace WinTools;

// maclaunch: list and toggle startup items. On Windows these are the Run keys and the Startup
// folders; enable/disable flips the same StartupApproved flag Task Manager's Startup page uses, so
// nothing is deleted. (Services are systemctl's job.)
static class Maclaunch
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("maclaunch", "List and enable/disable Windows startup items", Run);
    }

    const string Help = """
        Usage: maclaunch <list [filter] | enable <name> | disable <name>>
        List and toggle Windows startup items (Run keys and Startup folders), like Task Manager's Startup page.

          list [filter]    Show startup items; filter matches the name, "user" or "system"
          enable <name>    Let the item start at sign-in
          disable <name>   Keep the item from starting at sign-in (it stays installed)
        Changing system items (HKLM, the all-users Startup folder) needs an elevated prompt.
        """;

    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Run32Key = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    const string Approved = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

    sealed record Item(string Name, string Type, string Command, RegistryKey Hive, string ApprovedKey);

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Console.WriteLine(Help); return args.Length == 0 ? 1 : 0; }
        if (args[0] is "-v" or "--version" or "version") { Console.WriteLine($"maclaunch {Commands.Version} (wintools)"); return 0; }
        switch (args[0])
        {
            case "list" or "ls":
                if (args.Length > 2) throw new UsageException("list takes at most one filter");
                var filter = args.Length > 1 ? args[1] : null;
                foreach (var item in Items().Where(i => filter == null || Matches(i, filter)))
                {
                    Console.WriteLine($"{item.Name} [{item.Type}] {(Enabled(item) ? "enabled" : "disabled")}");
                    Console.WriteLine($"  > {item.Command}");
                }
                return 0;
            case "enable" or "disable":
                if (args.Length != 2) throw new UsageException($"{args[0]} needs exactly one item name");
                var matches = Items().Where(i => i.Name.Equals(args[1], StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 0)
                {
                    Console.Error.WriteLine($"maclaunch: no startup item named '{args[1]}' (see maclaunch list)");
                    return 1;
                }
                foreach (var item in matches)
                {
                    SetEnabled(item, args[0] == "enable");
                    Console.WriteLine($"{(args[0] == "enable" ? "Enabled" : "Disabled")} {item.Name} [{item.Type}]");
                }
                return 0;
            default: throw new UsageException($"unknown command '{args[0]}' (see maclaunch --help)");
        }
    }

    static bool Matches(Item i, string filter) =>
        i.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) || i.Type.StartsWith(filter, StringComparison.OrdinalIgnoreCase);

    static List<Item> Items()
    {
        var items = new List<Item>();
        void FromKey(RegistryKey hive, string path, string type, string approved)
        {
            using var key = hive.OpenSubKey(path);
            if (key == null) return;
            foreach (var name in key.GetValueNames().Where(n => n.Length > 0))
                items.Add(new Item(name, type, key.GetValue(name)?.ToString() ?? "", hive, approved));
        }
        void FromFolder(Environment.SpecialFolder folder, RegistryKey hive, string type)
        {
            var dir = Environment.GetFolderPath(folder);
            if (!Directory.Exists(dir)) return;
            foreach (var file in Directory.GetFiles(dir).Where(f => !Path.GetFileName(f).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)))
                items.Add(new Item(Path.GetFileName(file), type, file, hive, Approved + "StartupFolder"));
        }
        FromKey(Registry.CurrentUser, RunKey, "user agent", Approved + "Run");
        FromFolder(Environment.SpecialFolder.Startup, Registry.CurrentUser, "user agent");
        FromKey(Registry.LocalMachine, RunKey, "system agent", Approved + "Run");
        FromKey(Registry.LocalMachine, Run32Key, "system agent", Approved + "Run32");
        FromFolder(Environment.SpecialFolder.CommonStartup, Registry.LocalMachine, "system agent");
        return items;
    }

    // StartupApproved values: 12 bytes, an even first byte = enabled, odd = disabled; missing = enabled.
    static bool Enabled(Item item)
    {
        using var key = item.Hive.OpenSubKey(item.ApprovedKey);
        return key?.GetValue(item.Name) is not byte[] data || data.Length == 0 || (data[0] & 1) == 0;
    }

    static void SetEnabled(Item item, bool enabled)
    {
        using var key = item.Hive.CreateSubKey(item.ApprovedKey, writable: true);
        var data = new byte[12];
        data[0] = enabled ? (byte)2 : (byte)3;
        // A disabled entry records when it was disabled, as Task Manager does.
        if (!enabled) BitConverter.GetBytes(DateTime.UtcNow.ToFileTimeUtc()).CopyTo(data, 4);
        key.SetValue(item.Name, data, RegistryValueKind.Binary);
    }
}
