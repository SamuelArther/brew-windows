using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// macosvpn: create and delete VPN connections from the command line, as Windows' built-in VPN client
// entries (Settings > Network > VPN). `create` uses the stock Add-VpnConnection cmdlet (fed on stdin, so
// secrets never appear on a command line) and stores the username and password with RasSetCredentials;
// `delete` and `list` use the RAS phonebook API directly. Windows has no Cisco IPSec (XAuth) client.
static class Macosvpn
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("macosvpn", "Create and delete Windows VPN connections from the command line", Run);

    const string Help = """
        Usage: macosvpn create --l2tp|--ikev2|--sstp|--pptp <name> --endpoint <host> [options] [<another service> ...]
               macosvpn delete --name <name> [--name <name> ...] | --all
               macosvpn list
               macosvpn version

        create options (they apply to the service named before them):
          -l, --l2tp NAME          L2TP over IPSec (needs --sharedsecret)
              --ikev2 NAME         IKEv2 (Windows asks for the credentials on first connect)
              --sstp NAME          SSTP
              --pptp NAME          PPTP
          -e, --endpoint HOST      server address
          -u, --username USER
          -p, --password PASS
          -s, --sharedsecret PSK   L2TP pre-shared key
          -x, --split              split tunnel: only the VPN's own networks go through it
          -o, --force              replace an existing connection with the same name
        """;

    sealed class Service
    {
        public string Kind, Name, Endpoint, Username, Password, Secret;
        public bool Split;
    }

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(Help);
            return 0;
        }
        switch (args[0])
        {
            case "version" or "--version" or "-v": Console.WriteLine(Commands.Version); return 0;
            case "create": return Create(args[1..]);
            case "delete": return Delete(args[1..]);
            case "list": return List();
            default: throw new UsageException($"unknown command '{args[0]}' (see macosvpn help)");
        }
    }

    static int Create(string[] args)
    {
        var services = new List<Service>();
        var force = false;
        Service current = null;
        Service Need(string opt) => current ?? throw new UsageException($"{opt} must follow --l2tp, --ikev2, --sstp or --pptp <name>");
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{arg} requires a value");
            switch (arg)
            {
                case "-l" or "--l2tp": services.Add(current = new Service { Kind = "L2tp", Name = Value() }); break;
                case "--ikev2": services.Add(current = new Service { Kind = "Ikev2", Name = Value() }); break;
                case "--sstp": services.Add(current = new Service { Kind = "Sstp", Name = Value() }); break;
                case "--pptp": services.Add(current = new Service { Kind = "Pptp", Name = Value() }); break;
                case "-c" or "--cisco":
                    throw new InvalidOperationException("Windows has no built-in Cisco IPSec (XAuth) client; use --ikev2 or --l2tp, or Cisco's own client");
                case "-e" or "--endpoint": Need(arg).Endpoint = Value(); break;
                case "-u" or "--username": Need(arg).Username = Value(); break;
                case "-p" or "--password": Need(arg).Password = Value(); break;
                case "-s" or "--sharedsecret": Need(arg).Secret = Value(); break;
                case "-g" or "--groupname": Value(); break;   // Cisco group name: no Windows equivalent
                case "-x" or "--split": Need(arg).Split = true; break;
                case "-o" or "--force": force = true; break;
                case "-t" or "--disconnect" or "-i" or "--enable-mppe": break;
                default: throw new UsageException($"unknown option {arg}");
            }
        }
        if (services.Count == 0) throw new UsageException("no VPN service given (use --l2tp, --ikev2, --sstp or --pptp <name>)");
        foreach (var s in services)
        {
            if (string.IsNullOrEmpty(s.Endpoint)) throw new UsageException($"{s.Name}: --endpoint is required");
            if (s.Kind == "L2tp" && string.IsNullOrEmpty(s.Secret)) throw new UsageException($"{s.Name}: L2TP needs --sharedsecret");
        }

        var existing = Entries().Select(e => e.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var s in services)
        {
            if (existing.Contains(s.Name))
            {
                if (!force) throw new InvalidOperationException($"a VPN named \"{s.Name}\" already exists (use --force to replace it)");
                DeleteEntry(s.Name);
            }
            PowerShell(Script(s));
            if (s.Username != null && s.Kind != "Ikev2") SetCredentials(s.Name, s.Username, s.Password ?? "");
            Console.WriteLine($"Created {s.Kind.ToUpperInvariant()} VPN \"{s.Name}\" ({s.Endpoint})");
            if (s.Username != null && s.Kind == "Ikev2")
                Console.WriteLine("  IKEv2 uses EAP, whose credentials Windows asks for (and remembers) on the first connect");
        }
        return 0;
    }

    static string Script(Service s)
    {
        static string Q(string v) => "'" + v.Replace("'", "''") + "'";
        var sb = new StringBuilder("$ErrorActionPreference = 'Stop'\n");
        sb.Append($"Add-VpnConnection -Name {Q(s.Name)} -ServerAddress {Q(s.Endpoint)} -TunnelType {s.Kind} -Force -RememberCredential");
        sb.Append(s.Split ? " -SplitTunneling $true" : " -SplitTunneling $false");
        sb.Append(s.Kind switch
        {
            "L2tp" => $" -L2tpPsk {Q(s.Secret)} -AuthenticationMethod MSChapv2 -EncryptionLevel Required",
            "Ikev2" => " -AuthenticationMethod Eap -EncryptionLevel Required",
            _ => " -AuthenticationMethod MSChapv2 -EncryptionLevel Required",
        });
        return sb.Append('\n').ToString();
    }

    static int Delete(string[] args)
    {
        var names = new List<string>();
        var all = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-n" or "--name":
                    names.Add(++i < args.Length ? args[i] : throw new UsageException("--name requires a value"));
                    break;
                case "-a" or "--all": all = true; break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        var entries = Entries();
        if (all) names = entries.Where(e => !e.allUsers).Select(e => e.name).ToList();
        if (names.Count == 0) throw new UsageException(all ? "there are no VPN connections to delete" : "give --name <name> or --all");
        var status = 0;
        foreach (var name in names)
        {
            if (!entries.Any(e => e.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine($"macosvpn: no VPN named \"{name}\"");
                status = 1;
                continue;
            }
            DeleteEntry(name);
            Console.WriteLine($"Deleted VPN \"{name}\"");
        }
        return status;
    }

    static int List()
    {
        foreach (var (name, allUsers, phonebook) in Entries())
        {
            var (server, kind) = PhonebookInfo(phonebook, name);
            Console.WriteLine($"{name,-30} {kind,-6} {server,-30} {(allUsers ? "all users" : "this user")}");
        }
        return 0;
    }

    // The phone book is an INI file; PhoneNumber is the server and VpnStrategy the tunnel type.
    static (string server, string kind) PhonebookInfo(string path, string entry)
    {
        string server = "", kind = "";
        if (!File.Exists(path)) return (server, kind);
        var inSection = false;
        foreach (var line in File.ReadLines(path))
        {
            if (line.StartsWith('[')) { inSection = line == "[" + entry + "]"; continue; }
            if (!inSection) continue;
            if (line.StartsWith("PhoneNumber=")) server = line[12..];
            if (line.StartsWith("VpnStrategy="))
                kind = line[12..] switch { "1" or "2" => "PPTP", "3" or "4" => "L2TP", "5" or "6" => "SSTP", "7" or "8" => "IKEv2", "14" => "GRE", _ => "auto" };
        }
        return (server, kind);
    }

    // ---- RAS phone book -------------------------------------------------------------------------------

    static List<(string name, bool allUsers, string phonebook)> Entries()
    {
        const int size = 1048;   // RASENTRYNAMEW
        var list = new List<(string, bool, string)>();
        var count = 16;
        while (true)
        {
            var bytes = (uint)(size * count);
            var buffer = Marshal.AllocHGlobal((int)bytes);
            try
            {
                Marshal.WriteInt32(buffer, size);
                var error = RasEnumEntries(null, null, buffer, ref bytes, out var entries);
                if (error == 603) { count = (int)(bytes / size) + 1; continue; }   // ERROR_BUFFER_TOO_SMALL
                if (error != 0) throw new InvalidOperationException($"couldn't list VPN connections (RAS error {error})");
                for (var i = 0; i < entries; i++)
                {
                    var e = buffer + i * size;
                    list.Add((Marshal.PtrToStringUni(e + 4) ?? "", Marshal.ReadInt32(e, 520) == 1, Marshal.PtrToStringUni(e + 524) ?? ""));
                }
                return list;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
    }

    static void DeleteEntry(string name)
    {
        var entry = Entries().FirstOrDefault(e => e.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        var error = RasDeleteEntry(entry.phonebook, entry.name ?? name);
        if (error == 5) throw new UnauthorizedAccessException($"deleting \"{name}\" (an all-users connection) needs an elevated (Administrator) prompt");
        if (error != 0) throw new InvalidOperationException($"couldn't delete \"{name}\" (RAS error {error})");
    }

    static void SetCredentials(string name, string user, string password)
    {
        var entry = Entries().FirstOrDefault(e => e.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (user.Length > 256 || password.Length > 256) throw new UsageException("the username and password must be at most 256 characters");
        // RASCREDENTIALSW: dwSize, dwMask, szUserName[257], szPassword[257], szDomain[16].
        const int size = 8 + 257 * 2 + 257 * 2 + 16 * 2;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            unsafe { new Span<byte>((void*)buffer, size).Clear(); }
            Marshal.WriteInt32(buffer, size);
            Marshal.WriteInt32(buffer, 4, 1 | 2);   // RASCM_UserName | RASCM_Password
            Marshal.Copy(user.ToCharArray(), 0, buffer + 8, user.Length);
            Marshal.Copy(password.ToCharArray(), 0, buffer + 8 + 514, password.Length);
            var error = RasSetCredentials(entry.phonebook, name, buffer, false);
            if (error != 0) throw new InvalidOperationException($"the VPN was created, but saving its credentials failed (RAS error {error})");
        }
        finally
        {
            unsafe { new Span<byte>((void*)buffer, size).Clear(); }
            Marshal.FreeHGlobal(buffer);
        }
    }

    static void PowerShell(string script)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"))
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", "-" }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        p.StandardInput.Write(script);
        p.StandardInput.Close();
        var output = p.StandardOutput.ReadToEnd();
        var error = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0 || error.Trim().Length > 0)
            throw new InvalidOperationException((error.Trim().Length > 0 ? error : output).Trim().Split('\n')[0].Trim());
    }

    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RasEnumEntries(string reserved, string phonebook, IntPtr entries, ref uint size, out int count);

    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RasDeleteEntry(string phonebook, string entry);

    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RasSetCredentials(string phonebook, string entry, IntPtr credentials, [MarshalAs(UnmanagedType.Bool)] bool clear);
}
