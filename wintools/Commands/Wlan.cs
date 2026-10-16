using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// wifi-password and wavemon, both on the Native Wifi API (wlanapi.dll). wifi-password reads a saved
// network's key from its Wi-Fi profile (the plain-text key needs an elevated prompt for all-user
// profiles); wavemon is a live console monitor of the current connection and the nearby access points.
static class Wlan
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("wifi-password", "Show the current (or a saved) Wi-Fi network's password", WifiPassword);
        Commands.Add("wavemon", "Monitor the Wi-Fi connection and nearby access points", Wavemon);
    }

    // ---- wifi-password --------------------------------------------------------------------------------

    static int WifiPassword(string[] args)
    {
        var quiet = false;
        string ssid = null;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: wifi-password [-q|--quiet] [ssid]\nShow the password of the current Wi-Fi network, or of a saved one.");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"wifi-password {Commands.Version}"); return 0;
                case "-q" or "--quiet": quiet = true; break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg}");
                    ssid = ssid == null ? arg : ssid + " " + arg;
                    break;
            }
        }
        using var client = Client.Open();
        var ifaces = client.Interfaces();
        if (ifaces.Count == 0) throw new InvalidOperationException("no Wi-Fi adapter found");

        string profile = null;
        Guid iface = default;
        if (ssid == null)
        {
            foreach (var (guid, _, _) in ifaces)
            {
                if (client.Connection(guid) is not { } conn) continue;
                (iface, profile, ssid) = (guid, conn.Profile, conn.Ssid);
                break;
            }
            if (ssid == null) throw new InvalidOperationException("not connected to a Wi-Fi network (name one: wifi-password <ssid>)");
        }
        else
        {
            // Profiles are usually named after their SSID, but look inside each one to be sure.
            foreach (var (guid, _, _) in ifaces)
            {
                foreach (var name in client.Profiles(guid))
                {
                    var xml = client.ProfileXml(guid, name, plainText: false);
                    if (name == ssid || XmlValue(xml, "name") == ssid) { (iface, profile) = (guid, name); break; }
                }
                if (profile != null) break;
            }
            if (profile == null) throw new InvalidOperationException($"no saved Wi-Fi network named \"{ssid}\"");
        }

        if (!quiet) Console.WriteLine($"... getting password for \"{ssid}\".");
        var profileXml = client.ProfileXml(iface, profile, plainText: true);
        var key = XmlValue(profileXml, "keyMaterial");
        if (key == null)
        {
            Console.Error.WriteLine($"wifi-password: \"{ssid}\" has no password (it is an open or enterprise network)");
            return 1;
        }
        if (XmlValue(profileXml, "protected") == "true")
            throw new UnauthorizedAccessException("Windows only decrypts this network's key for an elevated (Administrator) prompt");
        Console.WriteLine(quiet ? key : $"\"{key}\"");
        return 0;
    }

    static string XmlValue(string xml, string element)
    {
        var start = xml.IndexOf("<" + element + ">", StringComparison.Ordinal);
        if (start < 0) return null;
        start += element.Length + 2;
        var end = xml.IndexOf("</" + element + ">", start, StringComparison.Ordinal);
        return end < 0 ? null : System.Net.WebUtility.HtmlDecode(xml[start..end]);
    }

    // ---- wavemon --------------------------------------------------------------------------------------

    static int Wavemon(string[] args)
    {
        string want = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                    Console.WriteLine("""
                        Usage: wavemon [-h] [-g] [-i <interface>] [-v]
                          -g  accepted for compatibility (the screen adapts to the console size)
                          -i  the Wi-Fi interface to monitor (name, description or GUID)
                          -v  print the version
                        Keys: q or F10 quits.
                        """);
                    return 0;
                case "-v": Console.WriteLine($"wavemon {Commands.Version}"); return 0;
                case "-g": break;
                case "-i":
                    if (++i >= args.Length) throw new UsageException("option requires an argument -- 'i'");
                    want = args[i];
                    break;
                default: throw new UsageException($"invalid option -- '{args[i].TrimStart('-')}'");
            }
        }
        Client client;
        try { client = Client.Open(); }
        catch (InvalidOperationException) { throw new InvalidOperationException("no supported wireless interfaces found"); }
        using (client)
        {
            var ifaces = client.Interfaces();
            var pick = ifaces.FirstOrDefault(f => want == null ? f.state == 1 : Matches(f.guid, f.description, want));
            if (pick.guid == Guid.Empty && want == null && ifaces.Count > 0) pick = ifaces[0];
            if (pick.guid == Guid.Empty)
                throw new InvalidOperationException(want == null ? "no supported wireless interfaces found" : $"no wireless interface '{want}'");

            if (Console.IsOutputRedirected)
            {
                Console.Write(Screen(client, pick.guid, pick.description, int.MaxValue));
                return 0;
            }
            Console.Clear();
            Console.CursorVisible = false;
            try
            {
                while (true)
                {
                    var text = Screen(client, pick.guid, pick.description, Console.WindowWidth - 1);
                    Console.SetCursorPosition(0, 0);
                    var lines = text.Split('\n');
                    var height = Console.WindowHeight - 1;
                    var sb = new StringBuilder();
                    for (var l = 0; l < height; l++)
                        sb.Append((l < lines.Length ? lines[l] : "").PadRight(Console.WindowWidth - 1)).Append(l < height - 1 ? "\n" : "");
                    Console.Write(sb.ToString());
                    // Redraw once a second; poll the keyboard in between.
                    for (var t = 0; t < 10; t++)
                    {
                        Thread.Sleep(100);
                        while (!Console.IsInputRedirected && Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(intercept: true);
                            if (key.Key is ConsoleKey.Q or ConsoleKey.F10 or ConsoleKey.Escape) return 0;
                        }
                    }
                }
            }
            finally
            {
                Console.CursorVisible = true;
                Console.Clear();
            }
        }
    }

    static bool Matches(Guid guid, string description, string want)
    {
        if (Guid.TryParse(want, out var g)) return g == guid;
        if (description.Contains(want, StringComparison.OrdinalIgnoreCase)) return true;
        var nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.Id.Trim('{', '}').Equals(guid.ToString(), StringComparison.OrdinalIgnoreCase));
        return nic != null && nic.Name.Equals(want, StringComparison.OrdinalIgnoreCase);
    }

    static string Screen(Client client, Guid guid, string description, int width)
    {
        var sb = new StringBuilder();
        void Section(string title) => sb.Append("\n").Append(("-- " + title + " ").PadRight(Math.Min(width, 72), '-')).Append('\n');

        var nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.Id.Trim('{', '}').Equals(guid.ToString(), StringComparison.OrdinalIgnoreCase));
        sb.Append($"wavemon {Commands.Version} - {nic?.Name ?? "Wi-Fi"} ({description})   [q quits]\n");
        var conn = client.Connection(guid);
        Section("Levels");
        if (conn is { } c)
        {
            var rssi = client.Rssi(guid);
            var bar = new string('#', (int)c.Quality / 5).PadRight(20, '.');
            sb.Append($"  link quality: {c.Quality,3}%  [{bar}]\n");
            sb.Append($"  signal level: {(rssi is { } r ? $"{r} dBm" : "n/a")}\n");
            Section("Info");
            sb.Append($"  ESSID: \"{c.Ssid}\"   mode: {(c.BssType == 1 ? "managed" : "ad-hoc")}   access point: {Mac(c.Bssid)}\n");
            var channel = client.Channel(guid);
            var freq = client.BssList(guid).FirstOrDefault(b => b.Bssid.AsSpan().SequenceEqual(c.Bssid)).FrequencyKhz;
            sb.Append($"  channel: {(channel is { } ch ? ch.ToString() : "?")} ({Band(freq)})   phy: {Phy(c.PhyType)}\n");
            sb.Append($"  rx rate: {c.RxRate / 1000.0:0.#} Mbit/s   tx rate: {c.TxRate / 1000.0:0.#} Mbit/s\n");
            sb.Append($"  security: {(c.Security ? Auth(c.Auth) + ", " + Cipher(c.Cipher) : "open")}{(c.OneX ? " (802.1X)" : "")}   profile: {c.Profile}\n");
        }
        else sb.Append("  not connected\n");

        Section("Network");
        if (nic != null)
        {
            var props = nic.GetIPProperties();
            var addrs = props.UnicastAddresses.Where(u => u.Address.AddressFamily == AddressFamily.InterNetwork || !u.Address.IsIPv6LinkLocal)
                .Select(u => $"{u.Address}/{u.PrefixLength}");
            sb.Append($"  if: {nic.Name}   mac: {Mac(nic.GetPhysicalAddress().GetAddressBytes())}   status: {nic.OperationalStatus.ToString().ToLowerInvariant()}\n");
            sb.Append($"  ip: {string.Join(", ", addrs)}\n");
            var stats = nic.GetIPStatistics();
            sb.Append($"  rx: {stats.BytesReceived:N0} bytes   tx: {stats.BytesSent:N0} bytes\n");
        }

        Section("Access points (last scan)");
        var bss = client.BssList(guid).OrderByDescending(b => b.Rssi).ToList();
        sb.Append($"  {"SSID",-32} {"BSSID",-17} {"chan",4} {"signal",8} {"qual",4}\n");
        foreach (var b in bss)
        {
            var name = b.Ssid.Length == 0 ? "<hidden>" : b.Ssid;
            sb.Append($"  {(name.Length > 32 ? name[..32] : name),-32} {Mac(b.Bssid),-17} {ChannelOf(b.FrequencyKhz),4} {b.Rssi + " dBm",8} {b.Quality + "%",4}\n");
        }
        if (bss.Count == 0) sb.Append("  none\n");
        return sb.ToString();
    }

    static string Mac(byte[] mac) => string.Join(':', mac.Select(b => b.ToString("x2")));

    static int ChannelOf(uint khz)
    {
        var mhz = (int)(khz / 1000);
        return mhz switch
        {
            2484 => 14,
            >= 2412 and < 2484 => (mhz - 2407) / 5,
            >= 5950 and <= 7125 => (mhz - 5950) / 5,
            >= 5000 and < 5950 => (mhz - 5000) / 5,
            _ => 0,
        };
    }

    static string Band(uint khz) => (khz / 1000) switch
    {
        0 => "?",
        < 3000 => $"{khz / 1e6:0.###} GHz, 2.4 GHz band",
        < 5925 => $"{khz / 1e6:0.###} GHz, 5 GHz band",
        _ => $"{khz / 1e6:0.###} GHz, 6 GHz band",
    };

    static string Phy(int phy) => phy switch
    {
        4 => "802.11a", 5 => "802.11b", 6 => "802.11g", 7 => "802.11n", 8 => "802.11ac", 9 => "802.11ad",
        10 => "802.11ax", 11 => "802.11be", _ => "unknown",
    };

    static string Auth(int auth) => auth switch
    {
        1 => "open", 2 => "shared key", 3 => "WPA-Enterprise", 4 => "WPA-PSK", 6 => "WPA2-Enterprise", 7 => "WPA2-PSK",
        8 => "WPA3-Enterprise 192", 9 => "WPA3-SAE", 10 => "OWE", 11 => "WPA3-Enterprise", _ => "unknown",
    };

    static string Cipher(int cipher) => cipher switch
    {
        0 => "none", 1 => "WEP-40", 2 => "TKIP", 4 => "CCMP", 5 => "WEP-104", 8 => "GCMP", 9 => "GCMP-256",
        10 => "CCMP-256", 0x101 => "WEP", _ => "unknown",
    };

    // ---- Native Wifi API ------------------------------------------------------------------------------

    record struct Connection(string Profile, string Ssid, int BssType, byte[] Bssid, int PhyType, uint Quality, uint RxRate, uint TxRate,
        bool Security, bool OneX, int Auth, int Cipher);

    record struct Bss(string Ssid, byte[] Bssid, int Rssi, uint Quality, uint FrequencyKhz);

    sealed class Client : IDisposable
    {
        IntPtr handle;

        public static Client Open()
        {
            int error;
            IntPtr handle;
            try { error = WlanOpenHandle(2, IntPtr.Zero, out _, out handle); }
            catch (DllNotFoundException) { throw new InvalidOperationException("this Windows has no Wi-Fi support (wlanapi.dll is missing)"); }
            if (error == 1062) throw new InvalidOperationException("the WLAN AutoConfig service (WlanSvc) isn't running, so there is no Wi-Fi");
            if (error != 0) throw new InvalidOperationException($"couldn't open the Wi-Fi API (error {error})");
            return new Client { handle = handle };
        }

        public void Dispose()
        {
            if (handle != IntPtr.Zero) WlanCloseHandle(handle, IntPtr.Zero);
            handle = IntPtr.Zero;
        }

        public List<(Guid guid, string description, int state)> Interfaces()
        {
            var list = new List<(Guid, string, int)>();
            if (WlanEnumInterfaces(handle, IntPtr.Zero, out var ptr) != 0) return list;
            try
            {
                var count = Marshal.ReadInt32(ptr);
                for (var i = 0; i < count; i++)
                {
                    var item = ptr + 8 + i * 532;   // WLAN_INTERFACE_INFO: GUID, WCHAR[256], state
                    var guid = Marshal.PtrToStructure<Guid>(item);
                    var description = Marshal.PtrToStringUni(item + 16) ?? "";
                    list.Add((guid, description, Marshal.ReadInt32(item, 528)));
                }
            }
            finally { WlanFreeMemory(ptr); }
            return list;
        }

        public Connection? Connection(Guid guid)
        {
            if (WlanQueryInterface(handle, ref guid, 7, IntPtr.Zero, out _, out var p, out _) != 0) return null;   // current_connection
            try
            {
                // WLAN_CONNECTION_ATTRIBUTES, with WLAN_ASSOCIATION_ATTRIBUTES at 520 and security at 588.
                if (Marshal.ReadInt32(p) != 1) return null;
                var ssidLength = Math.Min(Marshal.ReadInt32(p, 520), 32);
                var ssid = new byte[ssidLength];
                Marshal.Copy(p + 524, ssid, 0, ssidLength);
                var bssid = new byte[6];
                Marshal.Copy(p + 560, bssid, 0, 6);
                return new Connection(
                    Marshal.PtrToStringUni(p + 8) ?? "", Encoding.UTF8.GetString(ssid), Marshal.ReadInt32(p, 556), bssid,
                    Marshal.ReadInt32(p, 568), (uint)Marshal.ReadInt32(p, 576), (uint)Marshal.ReadInt32(p, 580), (uint)Marshal.ReadInt32(p, 584),
                    Marshal.ReadInt32(p, 588) != 0, Marshal.ReadInt32(p, 592) != 0, Marshal.ReadInt32(p, 596), Marshal.ReadInt32(p, 600));
            }
            finally { WlanFreeMemory(p); }
        }

        int? QueryInt(Guid guid, uint opcode)
        {
            if (WlanQueryInterface(handle, ref guid, opcode, IntPtr.Zero, out _, out var p, out _) != 0) return null;
            try { return Marshal.ReadInt32(p); }
            finally { WlanFreeMemory(p); }
        }

        public uint? Channel(Guid guid) => (uint?)QueryInt(guid, 8);
        public int? Rssi(Guid guid) => QueryInt(guid, 0x10000102);

        public List<Bss> BssList(Guid guid)
        {
            var list = new List<Bss>();
            if (WlanGetNetworkBssList(handle, ref guid, IntPtr.Zero, 3, false, IntPtr.Zero, out var p) != 0) return list;
            try
            {
                var count = Marshal.ReadInt32(p, 4);
                for (var i = 0; i < count; i++)
                {
                    var e = p + 8 + i * 360;   // WLAN_BSS_ENTRY
                    var len = Math.Min(Marshal.ReadInt32(e), 32);
                    var ssid = new byte[len];
                    Marshal.Copy(e + 4, ssid, 0, len);
                    var bssid = new byte[6];
                    Marshal.Copy(e + 40, bssid, 0, 6);
                    list.Add(new Bss(Encoding.UTF8.GetString(ssid), bssid, Marshal.ReadInt32(e, 56), (uint)Marshal.ReadInt32(e, 60),
                        (uint)Marshal.ReadInt32(e, 92)));
                }
            }
            finally { WlanFreeMemory(p); }
            return list;
        }

        public List<string> Profiles(Guid guid)
        {
            var list = new List<string>();
            if (WlanGetProfileList(handle, ref guid, IntPtr.Zero, out var p) != 0) return list;
            try
            {
                var count = Marshal.ReadInt32(p);
                for (var i = 0; i < count; i++) list.Add(Marshal.PtrToStringUni(p + 8 + i * 516) ?? "");
            }
            finally { WlanFreeMemory(p); }
            return list;
        }

        public string ProfileXml(Guid guid, string name, bool plainText)
        {
            uint flags = plainText ? 4u : 0u;   // WLAN_PROFILE_GET_PLAINTEXT_KEY
            var error = WlanGetProfile(handle, ref guid, name, IntPtr.Zero, out var xml, ref flags, out _);
            if (error == 1168) throw new InvalidOperationException($"no saved Wi-Fi network named \"{name}\"");
            if (error == 5) throw new UnauthorizedAccessException("access to the Wi-Fi profile was denied");
            if (error != 0) throw new InvalidOperationException($"couldn't read the Wi-Fi profile (error {error})");
            try { return Marshal.PtrToStringUni(xml) ?? ""; }
            finally { WlanFreeMemory(xml); }
        }
    }

    [DllImport("wlanapi.dll")]
    static extern int WlanOpenHandle(uint clientVersion, IntPtr reserved, out uint negotiatedVersion, out IntPtr handle);

    [DllImport("wlanapi.dll")]
    static extern int WlanCloseHandle(IntPtr handle, IntPtr reserved);

    [DllImport("wlanapi.dll")]
    static extern int WlanEnumInterfaces(IntPtr handle, IntPtr reserved, out IntPtr list);

    [DllImport("wlanapi.dll")]
    static extern int WlanQueryInterface(IntPtr handle, ref Guid iface, uint opcode, IntPtr reserved, out uint size, out IntPtr data, out int type);

    [DllImport("wlanapi.dll")]
    static extern int WlanGetNetworkBssList(IntPtr handle, ref Guid iface, IntPtr ssid, int bssType,
        [MarshalAs(UnmanagedType.Bool)] bool securityEnabled, IntPtr reserved, out IntPtr list);

    [DllImport("wlanapi.dll")]
    static extern int WlanGetProfileList(IntPtr handle, ref Guid iface, IntPtr reserved, out IntPtr list);

    [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)]
    static extern int WlanGetProfile(IntPtr handle, ref Guid iface, string profileName, IntPtr reserved, out IntPtr xml, ref uint flags, out uint access);

    [DllImport("wlanapi.dll")]
    static extern void WlanFreeMemory(IntPtr memory);
}
