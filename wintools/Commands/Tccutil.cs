using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// tccutil (jacobsalmela's tccutil.py): list and change privacy permissions. macOS keeps them in TCC.db;
// Windows keeps them in the Capability Access Manager consent store, HKCU\...\CapabilityAccessManager\
// ConsentStore\<capability>: a "Value" of Allow/Deny per Store app (by package family name) and, for
// desktop apps, one shared NonPackaged toggle plus a usage record per executable path ("C:#dir#app.exe").
// Services take TCC names (kTCCServiceCamera, Microphone, ...) or the Windows capability names
// (webcam, microphone, location, ...). TCC services with no Windows consent (Accessibility,
// AppleEvents, ...) are reported as such.
static class Tccutil
{
    const string Version = "1.5.5";
    const string Store = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";
    const string DefaultService = "kTCCServiceAccessibility";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("tccutil", "List and change Windows privacy permissions (camera, microphone, ...)", Run);

    // TCC service (without the kTCCService prefix) -> Windows capability; null = no Windows counterpart.
    static readonly Dictionary<string, string> Services = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Camera"] = "webcam", ["Microphone"] = "microphone", ["Location"] = "location",
        ["AddressBook"] = "contacts", ["Contacts"] = "contacts", ["Calendar"] = "appointments",
        ["Reminders"] = "userDataTasks", ["Photos"] = "picturesLibrary", ["PhotosAdd"] = "picturesLibrary",
        ["MediaLibrary"] = "musicLibrary", ["SystemPolicyDocumentsFolder"] = "documentsLibrary",
        ["SystemPolicyAllFiles"] = "broadFileSystemAccess", ["ScreenCapture"] = "graphicsCaptureProgrammatic",
        ["Bluetooth"] = "bluetooth", ["BluetoothAlways"] = "bluetooth", ["BluetoothPeripheral"] = "bluetooth",
        ["UserAvailability"] = "userAccountInformation", ["Accessibility"] = null, ["AppleEvents"] = null,
        ["PostEvent"] = null, ["ListenEvent"] = null, ["DeveloperTool"] = null, ["SystemPolicySysAdminFiles"] = null,
    };

    static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error:\n  No arguments.\n");
            Help(Console.Out);
            return 2;
        }
        string service = DefaultService, action = null, user = null;
        bool list = false, digest = false, verbose = false, haveUser = false;
        var insert = new List<string>();
        var remove = new List<string>();
        var enable = new List<string>();
        var disable = new List<string>();
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var eq = arg.StartsWith("--") ? arg.IndexOf('=') : -1;
            var name = eq > 0 ? arg[..eq] : arg;
            string Value() => eq > 0 ? arg[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"argument {name}: expected one argument");
            switch (name)
            {
                case "-h" or "--help": Help(Console.Out); return 0;
                case "--version": Console.WriteLine($"tccutil {Version}"); return 0;
                case "-s" or "--service": service = Value(); break;
                case "-l" or "--list": list = true; break;
                case "--digest": digest = true; break;
                case "-v" or "--verbose": verbose = true; break;
                case "-i" or "--insert": insert.Add(Value()); break;
                case "-r" or "--remove": remove.Add(Value()); break;
                case "-e" or "--enable": enable.Add(Value()); break;
                case "-d" or "--disable": disable.Add(Value()); break;
                case "-u" or "--user":
                    haveUser = true;
                    // nargs="?": the next word is the user unless it is another option.
                    if (eq > 0) user = arg[(eq + 1)..];
                    else if (i + 1 < args.Length && !args[i + 1].StartsWith('-')) user = args[++i];
                    break;
                default:
                    if (arg.StartsWith('-') || action != null) rest.Add(arg);
                    else action = arg;
                    break;
            }
        }

        using var root = OpenUserRoot(haveUser ? user : null);
        if (action != null)
        {
            if (action == "reset") return Reset(root, rest, verbose);
            Console.Error.WriteLine($"Error\n  Unrecognized command \"{action}\"");
        }

        if (digest)
        {
            Console.WriteLine(Digest(root));
            return 0;
        }
        if (list || insert.Count + remove.Count + enable.Count + disable.Count > 0)
        {
            var capability = Capability(service);
            if (list)
            {
                foreach (var client in Clients(root, capability)) Console.WriteLine(client);
                return 0;
            }
            foreach (var client in remove) Remove(root, capability, client, verbose);
            foreach (var client in insert) SetConsent(root, capability, client, "Allow", verbose);
            foreach (var client in enable) SetConsent(root, capability, client, "Allow", verbose);
            foreach (var client in disable) SetConsent(root, capability, client, "Deny", verbose);
        }
        return 0;
    }

    static void Help(TextWriter w) => w.WriteLine($"""
        usage: tccutil [-h] [--service SERVICE] [--list] [--digest] [--insert INSERT] [-v]
                       [-r REMOVE] [-e ENABLE] [-d DISABLE] [--user [USER]] [--version] [ACTION]

        Modify Windows privacy permissions (the Capability Access Manager consent store)

        positional arguments:
          ACTION                "reset [SERVICE] [CLIENT]" forgets the permission decisions for a service

        options:
          -h, --help            show this help message and exit
          --service, -s SERVICE Set the service: a TCC name (kTCCServiceCamera, Microphone, ...)
                                or a Windows capability (webcam, microphone, location, ...)
          --list, -l            List the apps with an entry for the service
          --digest              Print the digest hash of the consent store's layout
          --insert, -i INSERT   Adds the given app (package family name or path), allowed
          -v, --verbose         Outputs additional info for some commands
          -r, --remove REMOVE   Removes the given app's entry
          -e, --enable ENABLE   Allows the given app access
          -d, --disable DISABLE Denies the given app access
          --user, -u [USER]     Modify the permissions of the given (logged-on) user
          --version             Show the version of this script

        A path (or "desktop-apps") means Windows' shared switch for desktop apps: Windows grants
        camera, microphone and the rest to all desktop apps together, per app only to Store apps.
        tccutil {Version}
        """);

    // The current user's hive, or a logged-on user's under HKEY_USERS.
    static RegistryKey OpenUserRoot(string user)
    {
        if (string.IsNullOrEmpty(user)) return Registry.CurrentUser.OpenSubKey("", writable: true) ?? Registry.CurrentUser;
        string sid;
        try { sid = new NTAccount(user).Translate(typeof(SecurityIdentifier)).Value; }
        catch (IdentityNotMappedException)
        {
            throw new InvalidOperationException($"User \"{user}\" does not exist. Do you mean to use \"{user}\" as ACTION?");
        }
        return Registry.Users.OpenSubKey(sid, writable: true)
            ?? throw new InvalidOperationException($"{user}'s registry isn't loaded (the user has to be logged on)");
    }

    internal static string Capability(string service)
    {
        var name = service.StartsWith("kTCCService", StringComparison.OrdinalIgnoreCase) ? service[11..] : service;
        if (Services.TryGetValue(name, out var capability))
            return capability ?? throw new InvalidOperationException($"{service}: Windows has no privacy consent for this service");
        // A Windows capability name as is (matched against the consent store when listing).
        return name;
    }

    // Package family names, and desktop app paths (the store writes '\' as '#').
    static List<string> Clients(RegistryKey root, string capability)
    {
        using var key = root.OpenSubKey($@"{Store}\{capability}");
        if (key == null) return [];
        var clients = key.GetSubKeyNames().Where(n => n != "NonPackaged").ToList();
        using var desktop = key.OpenSubKey("NonPackaged");
        if (desktop != null) clients.AddRange(desktop.GetSubKeyNames().Select(n => n.Replace('#', '\\')));
        return clients;
    }

    internal static bool IsDesktopClient(string client) =>
        client.Equals("desktop-apps", StringComparison.OrdinalIgnoreCase) || client.Contains('\\') || client.Contains('/')
        || client.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

    static void SetConsent(RegistryKey root, string capability, string client, string value, bool verbose)
    {
        using var key = root.CreateSubKey($@"{Store}\{capability}");
        if (IsDesktopClient(client))
        {
            using var desktop = key.CreateSubKey("NonPackaged");
            desktop.SetValue("Value", value, RegistryValueKind.String);
            if (!client.Equals("desktop-apps", StringComparison.OrdinalIgnoreCase))
                Console.Error.WriteLine($"tccutil: note: Windows sets {capability} for all desktop apps at once ({value})");
        }
        else
        {
            using var app = key.CreateSubKey(client);
            app.SetValue("Value", value, RegistryValueKind.String);
        }
        if (verbose) Console.WriteLine($"{(value == "Allow" ? "Enabled" : "Disabled")} {client} for {capability}");
    }

    static void Remove(RegistryKey root, string capability, string client, bool verbose)
    {
        using var key = root.OpenSubKey($@"{Store}\{capability}", writable: true);
        if (key == null) return;
        if (IsDesktopClient(client))
        {
            using var desktop = key.OpenSubKey("NonPackaged", writable: true);
            desktop?.DeleteSubKeyTree(Path.GetFullPath(client).Replace('\\', '#'), throwOnMissingSubKey: false);
        }
        else key.DeleteSubKeyTree(client, throwOnMissingSubKey: false);
        if (verbose) Console.WriteLine($"Removed {client} from {capability}");
    }

    // tccutil reset SERVICE [CLIENT]: forget the per-app decisions (Windows asks again).
    static int Reset(RegistryKey root, List<string> rest, bool verbose)
    {
        if (rest.Count == 0) throw new UsageException("reset needs a service (e.g. tccutil reset Camera)");
        var capabilities = rest[0].Equals("All", StringComparison.OrdinalIgnoreCase)
            ? Services.Values.Where(c => c != null).Distinct().ToList()
            : [Capability(rest[0])];
        foreach (var capability in capabilities)
        {
            var clients = rest.Count > 1 ? [rest[1]] : Clients(root, capability);
            foreach (var client in clients) Remove(root, capability, client, verbose);
        }
        Console.WriteLine($"Successfully reset {rest[0]}" + (rest.Count > 1 ? $" approval status for {rest[1]}" : ""));
        return 0;
    }

    // tccutil.py prints the digest of TCC.db's schema; this hashes the consent store's capability list.
    static string Digest(RegistryKey root)
    {
        using var key = root.OpenSubKey(Store);
        var names = key?.GetSubKeyNames().Order(StringComparer.Ordinal) ?? Enumerable.Empty<string>();
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(string.Join("\n", names)));
        return Convert.ToHexStringLower(hash)[..10];
    }
}
