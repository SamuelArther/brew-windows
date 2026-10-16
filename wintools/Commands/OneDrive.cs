using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace WinTools;

// onedrive (abraunegg's OneDrive client for Linux) for the OneDrive client built into Windows: checks the
// service is reachable, shows the signed-in accounts and their folders, counts what's online-only,
// always-kept and local, and starts or resets the built-in client. Syncing itself is the built-in
// client's job (it always runs continuously; there is no one-shot sync).
static class OneDrive
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("onedrive", "Folder synchronization with OneDrive (drives the built-in client)", Run);

    const uint FILE_ATTRIBUTE_OFFLINE = 0x1000, FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x40000,
        FILE_ATTRIBUTE_PINNED = 0x80000, FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x400000;

    sealed record Account(string Key, string Email, string Name, string Folder, List<string> ExtraFolders);

    static int Run(string[] args)
    {
        string singleDirectory = null;
        string action = null;
        bool dryRun = false, resyncAuth = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string value = null;
            var eq = arg.IndexOf('=');
            if (arg.StartsWith("--") && eq > 0) { value = arg[(eq + 1)..]; arg = arg[..eq]; }
            string Value() => value ?? (++i < args.Length ? args[i] : throw new UsageException($"option '{arg}' requires an argument"));
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: onedrive [options]
                        Drives the OneDrive client built into Windows (sign in and choose folders in its own settings).
                        With no options, checks that the OneDrive service can be reached.
                          -s, --sync                  Start the built-in client if it isn't running (it syncs continuously)
                          -m, --monitor               Same as --sync: the built-in client always monitors for changes
                              --display-config        Show the client, the signed-in accounts and their folders
                              --display-sync-status   Count online-only, always-kept and local files
                              --single-directory DIR  Limit --display-sync-status to DIR (relative to the OneDrive folder)
                              --resync                Reset the built-in client (OneDrive.exe /reset); asks first
                              --resync-auth           Don't ask before --resync
                              --dry-run               Say what --sync/--resync would do without doing it
                              --logout, --reauth      Not available (use the client's settings)
                              --get-file-link FILE    Not available (use File Explorer's Share)
                              --version               Print the version
                        """);
                    return 0;
                case "--version":
                    Console.WriteLine($"onedrive v{Commands.Version} (wintools; drives the built-in Windows OneDrive client)");
                    return 0;
                case "-s" or "--sync" or "--synchronize": action ??= "sync"; break;
                case "-m" or "--monitor": action = "monitor"; break;
                case "--display-config": action = "config"; break;
                case "--display-sync-status": action = "status"; break;
                case "--resync": action = "resync"; break;
                case "--resync-auth": resyncAuth = true; break;
                case "--dry-run": dryRun = true; break;
                case "--single-directory": singleDirectory = Value(); break;
                case "--logout" or "--reauth":
                    Console.Error.WriteLine("onedrive: sign in or out in the OneDrive client's settings (Account > Unlink this PC); Windows doesn't let other programs do it");
                    return 1;
                case "--get-file-link" or "--create-share-link" or "--get-sharepoint-drive-id" or "--get-O365-drive-id":
                    Value();
                    Console.Error.WriteLine("onedrive: the built-in client has no command line for sharing; right-click the file in File Explorer and choose Share or Copy link");
                    return 1;
                case "-v" or "--verbose" or "--download-only" or "--upload-only" or "--local-first" or "--check-for-nomount"
                    or "--check-for-nosync" or "--skip-dot-files" or "--skip-symlinks" or "--no-remote-delete":
                    break;   // the built-in client's own settings decide these
                case "--confdir" or "--syncdir" or "--skip-dir" or "--skip-file" or "--log-dir" or "--classify-as-big-delete":
                    Value();
                    break;
                default: throw new UsageException($"unknown option '{args[i]}' (see onedrive --help)");
            }
        }
        return action switch
        {
            "config" => DisplayConfig(),
            "status" => DisplayStatus(singleDirectory),
            "sync" or "monitor" => Start(dryRun),
            "resync" => Resync(dryRun, resyncAuth),
            _ => Check(),
        };
    }

    // Same opening lines as the Linux client, then where things stand.
    static int Check()
    {
        Console.WriteLine("Using IPv4 and IPv6 (if configured) for all network operations");
        Console.WriteLine("Attempting to contact the Microsoft OneDrive Service");
        if (!Reachable())
        {
            Console.Error.WriteLine("Unable to reach the Microsoft OneDrive Service: check the network connection");
            return 1;
        }
        Console.WriteLine("Successfully reached the Microsoft OneDrive Service");
        var accounts = Accounts();
        if (accounts.Count == 0)
            Console.WriteLine("No account is signed in to the built-in OneDrive client; sign in from the OneDrive app");
        foreach (var account in accounts)
            Console.WriteLine($"Signed in: {account.Email ?? account.Key} -> {account.Folder}");
        if (ClientPath() == null) Console.WriteLine("The OneDrive client isn't installed (winget install Microsoft.OneDrive)");
        else Console.WriteLine(Running() ? "The OneDrive client is running and syncing" : "The OneDrive client isn't running; start it with --sync or --monitor");
        return 0;
    }

    static bool Reachable()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        foreach (var url in new[] { "https://login.microsoftonline.com/common/discovery/keys", "https://graph.microsoft.com/v1.0/$metadata" })
        {
            try
            {
                using var response = http.Send(new HttpRequestMessage(HttpMethod.Head, url));
                return true;   // any HTTP answer means the service is reachable
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
        }
        return false;
    }

    static int DisplayConfig()
    {
        var exe = ClientPath();
        Row("Application version", $"onedrive v{Commands.Version} (wintools)");
        Row("OneDrive client", exe == null ? "not installed" : $"{exe} ({FileVersionInfo.GetVersionInfo(exe).FileVersion})");
        Row("OneDrive client running", Running() ? "true" : "false");
        var accounts = Accounts();
        if (accounts.Count == 0) Row("Accounts", "none signed in");
        foreach (var account in accounts)
        {
            Row($"Account '{account.Key}'", account.Email ?? "(no e-mail recorded)");
            if (account.Name != null) Row("  Organisation", account.Name);
            Row("  Config option 'sync_dir'", account.Folder ?? "(not set)");
            foreach (var extra in account.ExtraFolders) Row("  Shared library folder", extra);
        }
        return 0;
    }

    static void Row(string name, string value) => Console.WriteLine($"{name,-40} = {value}");

    static int DisplayStatus(string singleDirectory)
    {
        var accounts = Accounts().Where(a => a.Folder != null).ToList();
        if (accounts.Count == 0) throw new InvalidOperationException("no OneDrive folder is set up (sign in from the OneDrive app)");
        foreach (var account in accounts)
        {
            var root = singleDirectory == null ? account.Folder
                : Path.IsPathRooted(singleDirectory) ? singleDirectory : Path.Combine(account.Folder, singleDirectory);
            if (!Directory.Exists(root))
            {
                if (singleDirectory != null && accounts.Count > 1) continue;
                throw new InvalidOperationException($"{root}: No such directory");
            }
            long online = 0, pinned = 0, local = 0, onlineBytes = 0, pinnedBytes = 0, localBytes = 0;
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = 0 };
            // Enumerating only reads directory entries, so online-only files aren't downloaded.
            foreach (var file in new DirectoryInfo(root).EnumerateFiles("*", options))
            {
                var attributes = (uint)file.Attributes;
                if ((attributes & FILE_ATTRIBUTE_PINNED) != 0) { pinned++; pinnedBytes += file.Length; }
                else if ((attributes & (FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_RECALL_ON_OPEN | FILE_ATTRIBUTE_OFFLINE)) != 0)
                { online++; onlineBytes += file.Length; }
                else { local++; localBytes += file.Length; }
            }
            Console.WriteLine($"Getting local sync status for {root}");
            Console.WriteLine($"  Always keep on this device: {pinned} files ({Size(pinnedBytes)})");
            Console.WriteLine($"  Available on this device:   {local} files ({Size(localBytes)})");
            Console.WriteLine($"  Online-only:                {online} files ({Size(onlineBytes)})");
            Console.WriteLine(Running() ? "The OneDrive client is running and keeps these in sync"
                : "The OneDrive client isn't running: changes won't sync until it starts (onedrive --sync)");
        }
        return 0;
    }

    static string Size(long bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return unit == 0 ? $"{bytes} B" : $"{value:0.0} {units[unit]}";
    }

    static int Start(bool dryRun)
    {
        var exe = ClientPath() ?? throw new InvalidOperationException("the OneDrive client isn't installed (winget install Microsoft.OneDrive)");
        if (Running())
        {
            Console.WriteLine("The OneDrive client is already running and syncing continuously");
            return 0;
        }
        if (dryRun) { Console.WriteLine($"Would run: \"{exe}\" /background"); return 0; }
        Process.Start(new ProcessStartInfo(exe, "/background") { UseShellExecute = false })?.Dispose();
        Console.WriteLine("Started the OneDrive client; it syncs in the background from now on");
        return 0;
    }

    static int Resync(bool dryRun, bool authorised)
    {
        var exe = ClientPath() ?? throw new InvalidOperationException("the OneDrive client isn't installed");
        if (dryRun) { Console.WriteLine($"Would run: \"{exe}\" /reset"); return 0; }
        if (!authorised)
        {
            Console.WriteLine("The use of --resync resets the OneDrive client: it forgets its sync state and checks every file again.");
            Console.Write("Are you sure you wish to proceed with --resync? [Y/N] ");
            var answer = Console.ReadLine()?.Trim();
            if (answer is not ("y" or "Y" or "yes" or "Yes")) { Console.WriteLine("Resync cancelled"); return 1; }
        }
        using (var reset = Process.Start(new ProcessStartInfo(exe, "/reset") { UseShellExecute = false }))
            reset?.WaitForExit(120_000);
        Console.WriteLine("The OneDrive client was reset; it restarts by itself (or run onedrive --sync)");
        return 0;
    }

    static bool Running()
    {
        var processes = Process.GetProcessesByName("OneDrive");
        var running = processes.Length > 0;
        foreach (var p in processes) p.Dispose();
        return running;
    }

    static string ClientPath()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive"))
            if (key?.GetValue("OneDriveTrigger") is string trigger && File.Exists(trigger)) return trigger;
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\OneDrive\OneDrive.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft OneDrive\OneDrive.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft OneDrive\OneDrive.exe"),
                 })
            if (File.Exists(candidate)) return candidate;
        return null;
    }

    // HKCU\Software\Microsoft\OneDrive\Accounts\<Personal|Business1...>; a key without UserFolder isn't signed in.
    static List<Account> Accounts()
    {
        var accounts = new List<Account>();
        using var root = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive\Accounts");
        if (root == null) return accounts;
        foreach (var name in root.GetSubKeyNames())
        {
            using var key = root.OpenSubKey(name);
            if (key?.GetValue("UserFolder") is not string folder) continue;
            var extras = new List<string>();
            using (var tenants = key.OpenSubKey("Tenants"))
                if (tenants != null)
                    foreach (var tenant in tenants.GetSubKeyNames())
                    {
                        using var t = tenants.OpenSubKey(tenant);
                        if (t == null) continue;
                        foreach (var value in t.GetValueNames())
                            if (Directory.Exists(value) && !value.Equals(folder, StringComparison.OrdinalIgnoreCase)) extras.Add(value);
                    }
            accounts.Add(new Account(name, key.GetValue("UserEmail") as string, key.GetValue("DisplayName") as string, folder, extras));
        }
        return accounts;
    }
}
