using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinTools;

// macchanger: show or change a network adapter's MAC address. Windows drivers take an override from the
// adapter's "NetworkAddress" registry value (what Device Manager's "Network Address" property sets); the
// adapter is restarted so the driver picks it up. The burned-in address comes from the driver itself
// (OID_802_3_PERMANENT_ADDRESS).
static class Macchanger
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("macchanger", "Show or change a network adapter's MAC address", Run);

    const string AdaptersKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

    static int Run(string[] args)
    {
        string mode = null, mac = null, device = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: macchanger [options] <interface>

                          -h, --help        print this help
                          -v, -V, --version print the version
                          -s, --show        print the current and permanent MAC address
                          -e, --ending      random address, keeping the vendor (first three) bytes
                          -r, --random      fully random (locally administered) address
                          -p, --permanent   reset to the permanent hardware address
                          -m, --mac=XX:XX:XX:XX:XX:XX  set this address

                        <interface> is the adapter's name (as in `ipconfig`, e.g. "Wi-Fi"), description or index.
                        Changing the address needs an elevated (Administrator) prompt and briefly restarts the adapter.
                        """);
                    return 0;
                case "-v" or "-V" or "--version": Console.WriteLine($"macchanger {Commands.Version}"); return 0;
                case "-s" or "--show": mode ??= "show"; break;
                case "-e" or "--ending": mode = "ending"; break;
                case "-r" or "--random": mode = "random"; break;
                case "-p" or "--permanent": mode = "permanent"; break;
                case "-m" or "--mac":
                    mode = "mac";
                    mac = ++i < args.Length ? args[i] : throw new UsageException("option requires an argument -- 'm'");
                    break;
                default:
                    if (arg.StartsWith("--mac=")) { mode = "mac"; mac = arg[6..]; break; }
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg}");
                    if (device != null) throw new UsageException("only one interface can be given");
                    device = arg;
                    break;
            }
        }
        if (device == null)
        {
            Console.Error.WriteLine("Please specify an interface name");
            return 1;
        }
        var requested = mode == "mac" ? Parse(mac) : null;
        var nic = Find(device);
        var current = nic.GetPhysicalAddress().GetAddressBytes();
        var permanent = Permanent(nic.Id);
        Console.WriteLine($"Current MAC:   {Format(current)}");
        Console.WriteLine($"Permanent MAC: {(permanent != null ? Format(permanent) : "unknown")}");
        if (mode is null or "show") return 0;

        var wireless = nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
        byte[] wanted = mode switch
        {
            "mac" => requested,
            "random" => RandomMac(null, wireless),
            "ending" => RandomMac(permanent ?? current, wireless),
            _ => null,   // permanent: remove the override
        };
        if (wanted != null)
        {
            if ((wanted[0] & 1) != 0) throw new InvalidOperationException("that is a multicast address; adapters need a unicast one (first byte even)");
            // Windows' Wi-Fi stack only accepts locally administered addresses: second hex digit 2, 6, A or E.
            if (wireless && (wanted[0] & 0x0F) is not (0x2 or 0x6 or 0xA or 0xE))
                throw new InvalidOperationException("Wi-Fi adapters on Windows only accept addresses whose second hex digit is 2, 6, A or E (try -r)");
        }
        if (!IsUserAnAdmin()) throw new UnauthorizedAccessException("changing the MAC address needs an elevated (Administrator) prompt");

        using (var key = AdapterKey(nic.Id))
        {
            if (wanted != null) key.SetValue("NetworkAddress", Convert.ToHexString(wanted), RegistryValueKind.String);
            else key.DeleteValue("NetworkAddress", throwOnMissingValue: false);
        }
        Restart(nic.Name);

        // The adapter takes a moment to come back with its new address.
        var expected = wanted ?? permanent;
        byte[] now = current;
        for (var tries = 0; tries < 20; tries++)
        {
            Thread.Sleep(500);
            var again = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.Id == nic.Id);
            if (again == null) continue;
            now = again.GetPhysicalAddress().GetAddressBytes();
            if (expected == null || now.AsSpan().SequenceEqual(expected)) break;
        }
        Console.WriteLine($"New MAC:       {Format(now)}");
        if (expected != null && !now.AsSpan().SequenceEqual(expected))
        {
            Console.Error.WriteLine("macchanger: the driver didn't take the new address (some adapters ignore NetworkAddress)");
            return 1;
        }
        return 0;
    }

    static NetworkInterface Find(string device)
    {
        var all = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.GetPhysicalAddress().GetAddressBytes().Length == 6
                && !System.Text.RegularExpressions.Regex.IsMatch(n.Name, @"-\d{4}$")).ToList();   // not NDIS filter layers
        var nic = all.FirstOrDefault(n => n.Name.Equals(device, StringComparison.OrdinalIgnoreCase))
            ?? all.FirstOrDefault(n => n.Id.Trim('{', '}').Equals(device.Trim('{', '}'), StringComparison.OrdinalIgnoreCase))
            ?? all.FirstOrDefault(n => n.Description.Equals(device, StringComparison.OrdinalIgnoreCase))
            ?? all.FirstOrDefault(n => int.TryParse(device, out var index) && Index(n) == index);
        return nic ?? throw new InvalidOperationException(
            $"no such interface '{device}' (adapters: {string.Join(", ", all.Select(n => n.Name))})");
    }

    static int Index(NetworkInterface n)
    {
        try { return n.GetIPProperties().GetIPv4Properties()?.Index ?? -1; }
        catch (NetworkInformationException) { return -1; }
    }

    static RegistryKey AdapterKey(string id)
    {
        using var cls = Registry.LocalMachine.OpenSubKey(AdaptersKey)
            ?? throw new InvalidOperationException("the network adapter class key is missing");
        foreach (var sub in cls.GetSubKeyNames())
        {
            if (!sub.All(char.IsAsciiDigit)) continue;
            RegistryKey key;
            try { key = cls.OpenSubKey(sub, writable: true); }
            catch (System.Security.SecurityException) { continue; }
            if (key == null) continue;
            if (id.Equals(key.GetValue("NetCfgInstanceId") as string, StringComparison.OrdinalIgnoreCase)) return key;
            key.Dispose();
        }
        throw new InvalidOperationException("couldn't find the adapter's driver settings in the registry");
    }

    static void Restart(string name)
    {
        foreach (var state in new[] { "disabled", "enabled" })
        {
            var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "netsh.exe"))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var a in new[] { "interface", "set", "interface", "name=" + name, "admin=" + state }) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) throw new InvalidOperationException($"couldn't restart {name}: {output.Trim()}");
        }
    }

    // The address burned into the hardware, asked of the NDIS driver through the adapter's device object.
    static unsafe byte[] Permanent(string id)
    {
        using var handle = CreateFile(@"\\.\" + id, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (handle.IsInvalid) return null;
        uint oid = OID_802_3_PERMANENT_ADDRESS;
        var buffer = new byte[6];
        fixed (byte* b = buffer)
            if (!DeviceIoControl(handle, IOCTL_NDIS_QUERY_GLOBAL_STATS, &oid, 4, b, 6, out var returned, IntPtr.Zero) || returned != 6)
                return null;
        return buffer;
    }

    internal static byte[] Parse(string text)
    {
        var hex = text.Replace(":", "").Replace("-", "").Replace(".", "");
        if (hex.Length != 12 || !hex.All(char.IsAsciiHexDigit)) throw new UsageException($"invalid MAC address '{text}'");
        return Convert.FromHexString(hex);
    }

    // Random unicast, locally administered address; with a base, its vendor (OUI) bytes are kept.
    internal static byte[] RandomMac(byte[] keepVendor, bool wireless)
    {
        var mac = new byte[6];
        System.Security.Cryptography.RandomNumberGenerator.Fill(mac);
        if (keepVendor != null)
        {
            Array.Copy(keepVendor, mac, 3);
            if (wireless) mac[0] = (byte)((mac[0] & 0xFC) | 0x02);
        }
        else mac[0] = (byte)((mac[0] & 0xFC) | 0x02);
        return mac;
    }

    static string Format(byte[] mac) => mac.Length == 0 ? "none" : string.Join(':', mac.Select(b => b.ToString("x2")));

    const uint IOCTL_NDIS_QUERY_GLOBAL_STATS = 0x00170002, OID_802_3_PERMANENT_ADDRESS = 0x01010101;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security,
        uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern unsafe bool DeviceIoControl(Microsoft.Win32.SafeHandles.SafeFileHandle device, uint code, void* input, uint inputSize,
        void* output, uint outputSize, out uint returned, IntPtr overlapped);

    [DllImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsUserAnAdmin();
}
