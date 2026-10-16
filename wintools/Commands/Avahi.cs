using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// avahi's command-line tools on the mDNS/DNS-SD stack built into Windows 10 1903+ (dnsapi's
// DnsServiceBrowse/Resolve/Register). Windows' DNS Client service is the responder, so there is no
// daemon to run and no libavahi; these are the user-facing tools only.
static unsafe class Avahi
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("avahi-browse", "Browse for mDNS/DNS-SD services on the local network", Browse);
        Commands.Add("avahi-resolve", "Resolve host names and addresses over mDNS", Resolve);
        Commands.Add("avahi-resolve-host-name", "Resolve host names over mDNS", args => Resolve(["-n", .. args]));
        Commands.Add("avahi-resolve-address", "Resolve addresses to host names over mDNS", args => Resolve(["-a", .. args]));
        Commands.Add("avahi-publish", "Register an mDNS/DNS-SD service", Publish);
        Commands.Add("avahi-publish-service", "Register an mDNS/DNS-SD service", args => Publish(["-s", .. args]));
        Commands.Add("avahi-publish-address", "Register an mDNS address record (not available on Windows)", args => Publish(["-a", .. args]));
        Commands.Add("avahi-daemon", "Check the mDNS responder (Windows' DNS Client service)", Daemon);
    }

    const string AvahiVersion = "0.8";
    const uint DNS_REQUEST_PENDING = 9506, ERROR_SUCCESS = 0;
    const ushort DNS_TYPE_PTR = 12;

    // avahi-browse shows these descriptions unless -k is given.
    static readonly Dictionary<string, string> TypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["_http._tcp"] = "Web Site", ["_https._tcp"] = "Secure Web Site", ["_ipp._tcp"] = "Internet Printer",
        ["_ipps._tcp"] = "Secure Internet Printer", ["_printer._tcp"] = "UNIX Printer", ["_pdl-datastream._tcp"] = "PDL Printer",
        ["_ssh._tcp"] = "SSH Remote Terminal", ["_sftp-ssh._tcp"] = "SFTP File Transfer", ["_smb._tcp"] = "Microsoft Windows Network",
        ["_afpovertcp._tcp"] = "Apple File Sharing", ["_device-info._tcp"] = "Device Info", ["_workstation._tcp"] = "Workstation",
        ["_raop._tcp"] = "AirTunes Remote Audio", ["_airplay._tcp"] = "AirPlay", ["_googlecast._tcp"] = "Google Cast",
        ["_spotify-connect._tcp"] = "Spotify Connect", ["_scanner._tcp"] = "Scanner", ["_uscan._tcp"] = "Scanner (eSCL)",
        ["_daap._tcp"] = "iTunes Audio Access", ["_rfb._tcp"] = "VNC Remote Access", ["_nfs._tcp"] = "Network File System",
        ["_webdav._tcp"] = "WebDAV File Share", ["_homekit._tcp"] = "HomeKit Accessory", ["_hap._tcp"] = "HomeKit Accessory",
    };

    // --- avahi-browse ---------------------------------------------------------------------------------

    sealed class Options
    {
        public bool Resolve, Terminate, Parsable, NoDbLookup, All;
    }

    sealed class BrowseJob
    {
        public string Interface, Protocol, Type, Domain = "local";
        public uint Index;
        public IntPtr Request, Name, Cancel;
    }

    sealed class ResolveJob
    {
        public BrowseJob Browse;
        public string Instance;
        public IntPtr Request, Name, Cancel;
    }

    static readonly object Gate = new();
    static readonly List<BrowseJob> Browses = [];
    static readonly List<ResolveJob> Resolves = [];
    static readonly HashSet<string> Seen = new(StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> SeenTypes = new(StringComparer.OrdinalIgnoreCase);
    static Options Opts;
    static int Pending;

    static int Browse(string[] args)
    {
        var opts = new Options();
        string type = null;
        double wait = 3;
        foreach (var arg in args)
        {
            if (arg is "-h" or "--help")
            {
                Console.WriteLine("""
                    Usage: avahi-browse [options] <service type>
                           avahi-browse [options] -a
                    Browse for mDNS/DNS-SD services with Windows' built-in mDNS stack.
                      -a --all            Show all services, regardless of the type
                      -d --domain=DOMAIN  The domain to browse in (only "local" is supported)
                      -t --terminate      Terminate after dumping a more or less complete list
                      -c --cache          Same as -t (Windows keeps no separate cache)
                      -r --resolve        Resolve services found
                      -k --no-db-lookup   Don't lookup service types
                      -p --parsable       Output in parsable format
                      -l --ignore-local   Accepted for compatibility
                      -v --verbose        Accepted for compatibility
                      -V --version        Show version
                    """);
                return 0;
            }
            if (arg is "-V" or "--version") { Console.WriteLine($"avahi-browse {AvahiVersion} (wintools {Commands.Version})"); return 0; }
            if (arg.StartsWith("--domain="))
            {
                if (!arg[9..].TrimEnd('.').Equals("local", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("only the .local domain can be browsed with mDNS on Windows");
                continue;
            }
            if (arg.StartsWith("--"))
            {
                switch (arg)
                {
                    case "--all": opts.All = true; break;
                    case "--terminate" or "--cache": opts.Terminate = true; break;
                    case "--resolve": opts.Resolve = true; break;
                    case "--no-db-lookup": opts.NoDbLookup = true; break;
                    case "--parsable": opts.Parsable = true; break;
                    case "--ignore-local" or "--verbose" or "--no-fail" or "--ignore-unresolved": break;
                    default: throw new UsageException($"unrecognized option '{arg}'");
                }
                continue;
            }
            if (arg.StartsWith('-') && arg.Length > 1)
            {
                foreach (var c in arg[1..])
                    switch (c)
                    {
                        case 'a': opts.All = true; break;
                        case 't' or 'c': opts.Terminate = true; break;
                        case 'r': opts.Resolve = true; break;
                        case 'k': opts.NoDbLookup = true; break;
                        case 'p': opts.Parsable = true; break;
                        case 'l' or 'v' or 'f': break;
                        default: throw new UsageException($"invalid option -- '{c}'");
                    }
                continue;
            }
            if (type != null) throw new UsageException("too many arguments");
            type = arg;
        }
        if (!opts.All && type == null) throw new UsageException("too few arguments (see avahi-browse --help)");
        Opts = opts;

        var interfaces = Interfaces();
        if (interfaces.Count == 0) throw new InvalidOperationException("no network interface that can do multicast is up");
        using var stop = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };
        try
        {
            lock (Gate)
                foreach (var (name, index, protocol) in interfaces)
                    if (opts.All) StartBrowse(name, index, protocol, "_services._dns-sd._udp");
                    else StartBrowse(name, index, protocol, TrimType(type));
        }
        catch (EntryPointNotFoundException)
        {
            throw new InvalidOperationException("mDNS browsing needs Windows 10 version 1903 or later");
        }

        if (opts.Terminate)
        {
            // Windows has no "all for now" signal: give answers a few seconds, then let resolves finish.
            stop.Wait(TimeSpan.FromSeconds(wait));
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (Volatile.Read(ref Pending) > 0 && DateTime.UtcNow < deadline && !stop.IsSet) stop.Wait(100);
        }
        else stop.Wait();

        lock (Gate)
        {
            foreach (var job in Browses) DnsServiceBrowseCancel(job.Cancel);
            foreach (var job in Resolves) DnsServiceResolveCancel(job.Cancel);
        }
        Thread.Sleep(100);   // let callbacks that were already running finish before the process exits
        return 0;
    }

    static string TrimType(string type)
    {
        type = type.TrimEnd('.');
        return type.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ? type[..^6] : type;
    }

    static List<(string Name, uint Index, string Protocol)> Interfaces()
    {
        var list = new List<(string, uint, string)>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback || !nic.SupportsMulticast)
                continue;
            var props = nic.GetIPProperties();
            if (nic.Supports(NetworkInterfaceComponent.IPv4))
                list.Add((nic.Name, (uint)props.GetIPv4Properties().Index, "IPv4"));
            else if (nic.Supports(NetworkInterfaceComponent.IPv6))
                list.Add((nic.Name, (uint)props.GetIPv6Properties().Index, "IPv6"));
        }
        return list;
    }

    // Caller holds Gate.
    static void StartBrowse(string iface, uint index, string protocol, string type)
    {
        var job = new BrowseJob { Interface = iface, Index = index, Protocol = protocol, Type = type };
        job.Name = Marshal.StringToHGlobalUni(type + ".local");
        job.Cancel = Marshal.AllocHGlobal(IntPtr.Size);
        *(IntPtr*)job.Cancel = IntPtr.Zero;
        // DNS_SERVICE_BROWSE_REQUEST: Version, InterfaceIndex, QueryName, pBrowseCallback, pQueryContext
        job.Request = Marshal.AllocHGlobal(32);
        var p = (byte*)job.Request;
        *(uint*)p = 1;
        *(uint*)(p + 4) = index;
        *(IntPtr*)(p + 8) = job.Name;
        *(IntPtr*)(p + 16) = (IntPtr)(delegate* unmanaged<uint, IntPtr, IntPtr, void>)&BrowseCallback;
        *(IntPtr*)(p + 24) = Browses.Count;
        Browses.Add(job);
        var status = DnsServiceBrowse(job.Request, job.Cancel);
        if (status != DNS_REQUEST_PENDING && status != ERROR_SUCCESS)
            Console.Error.WriteLine($"avahi-browse: browsing {type} on {iface} failed: {new System.ComponentModel.Win32Exception((int)status).Message}");
    }

    [UnmanagedCallersOnly]
    static void BrowseCallback(uint status, IntPtr context, IntPtr records)
    {
        try
        {
            if (status != ERROR_SUCCESS || records == IntPtr.Zero) return;
            lock (Gate)
            {
                var job = Browses[(int)context];
                for (var r = (byte*)records; r != null; r = *(byte**)r)
                {
                    if (*(ushort*)(r + 16) != DNS_TYPE_PTR) continue;
                    var target = Marshal.PtrToStringUni(*(IntPtr*)(r + 32))?.TrimEnd('.');
                    if (string.IsNullOrEmpty(target)) continue;
                    var removed = *(uint*)(r + 24) == 0;
                    if (job.Type == "_services._dns-sd._udp") FoundType(job, TrimType(target), removed);
                    else FoundService(job, target, removed);
                }
            }
        }
        catch (Exception e) { Console.Error.WriteLine($"avahi-browse: {e.Message}"); }
        finally { if (records != IntPtr.Zero) DnsRecordListFree(records, 1); }
    }

    // -a: each service type found starts a browse for that type on every interface.
    static void FoundType(BrowseJob job, string type, bool removed)
    {
        if (removed || !SeenTypes.Add(type)) return;
        foreach (var (name, index, protocol) in Interfaces()) StartBrowse(name, index, protocol, type);
    }

    static void FoundService(BrowseJob job, string fullName, bool removed)
    {
        var suffix = "." + job.Type + ".local";
        var instance = fullName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? fullName[..^suffix.Length] : fullName;
        var key = job.Index + "|" + fullName;
        if (removed ? !Seen.Remove(key) : !Seen.Add(key)) return;
        Line(removed ? '-' : '+', job, instance, null);
        if (!removed && Opts.Resolve) StartResolve(job, instance, fullName);
    }

    static void StartResolve(BrowseJob browse, string instance, string fullName)
    {
        var job = new ResolveJob { Browse = browse, Instance = instance };
        job.Name = Marshal.StringToHGlobalUni(fullName);
        job.Cancel = Marshal.AllocHGlobal(IntPtr.Size);
        *(IntPtr*)job.Cancel = IntPtr.Zero;
        // DNS_SERVICE_RESOLVE_REQUEST: Version, InterfaceIndex, QueryName, pResolveCompletionCallback, pQueryContext
        job.Request = Marshal.AllocHGlobal(32);
        var p = (byte*)job.Request;
        *(uint*)p = 1;
        *(uint*)(p + 4) = browse.Index;
        *(IntPtr*)(p + 8) = job.Name;
        *(IntPtr*)(p + 16) = (IntPtr)(delegate* unmanaged<uint, IntPtr, IntPtr, void>)&ResolveCallback;
        *(IntPtr*)(p + 24) = Resolves.Count;
        Resolves.Add(job);
        Interlocked.Increment(ref Pending);
        var status = DnsServiceResolve(job.Request, job.Cancel);
        if (status != DNS_REQUEST_PENDING && status != ERROR_SUCCESS)
        {
            Interlocked.Decrement(ref Pending);
            Console.Error.WriteLine($"Failed to resolve service '{instance}' of type '{browse.Type}' in domain 'local': {new System.ComponentModel.Win32Exception((int)status).Message}");
        }
    }

    [UnmanagedCallersOnly]
    static void ResolveCallback(uint status, IntPtr context, IntPtr instance)
    {
        try
        {
            lock (Gate)
            {
                var job = Resolves[(int)context];
                if (status != ERROR_SUCCESS || instance == IntPtr.Zero)
                {
                    if (status != 1223)   // ERROR_CANCELLED
                        Console.Error.WriteLine($"Failed to resolve service '{job.Instance}' of type '{job.Browse.Type}' in domain 'local': {new System.ComponentModel.Win32Exception((int)status).Message}");
                    return;
                }
                // DNS_SERVICE_INSTANCE
                var p = (byte*)instance;
                var host = Marshal.PtrToStringUni(*(IntPtr*)(p + 8))?.TrimEnd('.') ?? "";
                IPAddress address = null;
                if (*(byte**)(p + 16) != null) address = new IPAddress(new ReadOnlySpan<byte>(*(byte**)(p + 16), 4));
                else if (*(byte**)(p + 24) != null) address = new IPAddress(new ReadOnlySpan<byte>(*(byte**)(p + 24), 16));
                var port = *(ushort*)(p + 32);
                var count = *(uint*)(p + 40);
                var txt = new List<string>();
                for (var i = 0; i < count; i++)
                {
                    var k = Marshal.PtrToStringUni((*(IntPtr**)(p + 48))[i]) ?? "";
                    var v = Marshal.PtrToStringUni((*(IntPtr**)(p + 56))[i]);
                    txt.Add(string.IsNullOrEmpty(v) ? k : k + "=" + v);
                }
                Line('=', job.Browse, job.Instance, (host, address, port, txt));
            }
        }
        catch (Exception e) { Console.Error.WriteLine($"avahi-browse: {e.Message}"); }
        finally
        {
            if (instance != IntPtr.Zero) DnsServiceFreeInstance(instance);
            Interlocked.Decrement(ref Pending);
        }
    }

    static void Line(char mark, BrowseJob job, string instance, (string Host, IPAddress Address, ushort Port, List<string> Txt)? resolved)
    {
        var protocol = resolved?.Address?.AddressFamily == AddressFamily.InterNetworkV6 ? "IPv6"
            : resolved?.Address != null ? "IPv4" : job.Protocol;
        var txt = resolved == null ? "" : string.Join(' ', resolved.Value.Txt.Select(t => "\"" + t + "\""));
        if (Opts.Parsable)
        {
            var fields = new List<string> { mark.ToString(), job.Interface, protocol, Escape(instance), job.Type, job.Domain };
            if (resolved is { } r) fields.AddRange([r.Host, r.Address?.ToString() ?? "", r.Port.ToString(), txt]);
            Console.WriteLine(string.Join(';', fields));
            return;
        }
        var type = Opts.NoDbLookup ? job.Type : TypeNames.GetValueOrDefault(job.Type, job.Type);
        Console.WriteLine($"{mark} {job.Interface,6} {protocol,4} {instance,-40} {type,-20} {job.Domain}");
        if (resolved is { } res)
        {
            Console.WriteLine($"   hostname = [{res.Host}]");
            Console.WriteLine($"   address = [{res.Address}]");
            Console.WriteLine($"   port = [{res.Port}]");
            Console.WriteLine($"   txt = [{txt}]");
        }
    }

    // DNS-SD label escaping as avahi-browse -p prints it: '.' and '\' backslashed, other non-printables as \DDD.
    static string Escape(string label)
    {
        var sb = new StringBuilder();
        foreach (var b in Encoding.UTF8.GetBytes(label))
        {
            if (b is (byte)'.' or (byte)'\\') sb.Append('\\').Append((char)b);
            else if (b <= 32 || b == 127) sb.Append('\\').Append(b.ToString("D3"));
            else sb.Append((char)b);
        }
        return Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(sb.ToString()));
    }

    // --- avahi-resolve --------------------------------------------------------------------------------

    static int Resolve(string[] args)
    {
        string mode = null;
        AddressFamily? family = null;
        var names = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: avahi-resolve [options] -n <host name>...
                               avahi-resolve [options] -a <address>...
                        Resolve with Windows' resolver, which asks over mDNS for .local names.
                          -n --name         Resolve host name
                          -a --address      Resolve address
                          -4                Look up IPv4 address
                          -6                Look up IPv6 address
                          -v --verbose      Accepted for compatibility
                          -V --version      Show version
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine($"avahi-resolve {AvahiVersion} (wintools {Commands.Version})"); return 0;
                case "-n" or "--name": mode = "name"; break;
                case "-a" or "--address": mode = "address"; break;
                case "-4": family = AddressFamily.InterNetwork; break;
                case "-6": family = AddressFamily.InterNetworkV6; break;
                case "-v" or "--verbose": break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"invalid option '{arg}'");
                    names.Add(arg);
                    break;
            }
        }
        if (mode == null) throw new UsageException("specify -n (host name) or -a (address)");
        if (names.Count == 0) throw new UsageException("too few arguments");
        var status = 0;
        foreach (var name in names)
        {
            try
            {
                if (mode == "name")
                {
                    var addresses = Dns.GetHostAddresses(name).Where(a => family == null || a.AddressFamily == family).ToList();
                    if (addresses.Count == 0) throw new SocketException(11001);
                    Console.WriteLine($"{name}\t{addresses[0]}");
                }
                else
                {
                    if (!IPAddress.TryParse(name, out var address)) throw new UsageException($"failed to parse address '{name}'");
                    Console.WriteLine($"{name}\t{Dns.GetHostEntry(address).HostName}");
                }
            }
            catch (SocketException e)
            {
                Console.Error.WriteLine(mode == "name"
                    ? $"Failed to resolve host name '{name}': {e.Message}"
                    : $"Failed to resolve address '{name}': {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    // --- avahi-publish --------------------------------------------------------------------------------

    static uint Published = uint.MaxValue;
    static ManualResetEventSlim PublishDone;

    static int Publish(string[] args)
    {
        string mode = null, host = null;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: avahi-publish -s [options] <name> <type> <port> [<txt ...>]
                        Register a DNS-SD service with Windows' mDNS responder until Ctrl+C.
                          -s --service         Publish service
                          -H --host=HOST       Host where the service resides (default: this computer)
                          -d --domain=DOMAIN   Domain (only "local")
                          -a --address         Publish an address record (not available on Windows)
                          -R --no-reverse      Accepted for compatibility
                          -f --no-fail         Accepted for compatibility
                          -V --version         Show version
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine($"avahi-publish {AvahiVersion} (wintools {Commands.Version})"); return 0;
                case "-s" or "--service": mode = "service"; break;
                case "-a" or "--address": mode = "address"; break;
                case "-H": host = ++i < args.Length ? args[i] : throw new UsageException("-H needs a host"); break;
                case "-d": if (++i >= args.Length) throw new UsageException("-d needs a domain"); break;
                case "-R" or "--no-reverse" or "-f" or "--no-fail" or "-v" or "--verbose": break;
                default:
                    if (args[i].StartsWith("--host=")) host = args[i][7..];
                    else if (args[i].StartsWith("--domain=") || args[i].StartsWith("--subtype=")) { }
                    else if (args[i].StartsWith('-')) throw new UsageException($"invalid option '{args[i]}'");
                    else rest.Add(args[i]);
                    break;
            }
        }
        if (mode == "address")
        {
            Console.Error.WriteLine("avahi-publish: Windows' mDNS responder only answers for this computer's own name; it can't publish other address records");
            return 1;
        }
        if (mode != "service") throw new UsageException("specify -s (service) (see avahi-publish --help)");
        if (rest.Count < 3) throw new UsageException("too few arguments: <name> <type> <port> [<txt ...>]");
        if (!ushort.TryParse(rest[2], out var port)) throw new UsageException($"failed to parse port number: {rest[2]}");
        var name = rest[0];
        var type = TrimType(rest[1]);
        host ??= Environment.MachineName.ToLowerInvariant() + ".local";
        if (!host.Contains('.')) host += ".local";

        var keys = new List<IntPtr>();
        var values = new List<IntPtr>();
        foreach (var record in rest.Skip(3))
        {
            var eq = record.IndexOf('=');
            keys.Add(Marshal.StringToHGlobalUni(eq < 0 ? record : record[..eq]));
            values.Add(Marshal.StringToHGlobalUni(eq < 0 ? "" : record[(eq + 1)..]));
        }
        var ip4 = Interfaces().Select(i => NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.Name == i.Name))
            .Where(n => n != null)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Select(u => u.Address)
            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
        uint ip4Value = ip4 == null ? 0 : BitConverter.ToUInt32(ip4.GetAddressBytes());

        IntPtr instance;
        var keyArray = keys.ToArray();
        var valueArray = values.ToArray();
        fixed (IntPtr* k = keyArray, v = valueArray)
        {
            try
            {
                instance = DnsServiceConstructInstance($"{name}.{type}.local", host, ip4 == null ? null : &ip4Value, null,
                    port, 0, 0, (uint)keys.Count, k, v);
            }
            catch (EntryPointNotFoundException)
            {
                throw new InvalidOperationException("mDNS service registration needs Windows 10 version 1903 or later");
            }
        }
        if (instance == IntPtr.Zero) throw new InvalidOperationException("couldn't build the service record");

        // DNS_SERVICE_REGISTER_REQUEST: Version, InterfaceIndex, pServiceInstance, callback, context, hCredentials, unicastEnabled
        var request = Marshal.AllocHGlobal(48);
        new Span<byte>((void*)request, 48).Clear();
        var p = (byte*)request;
        *(uint*)p = 1;
        *(IntPtr*)(p + 8) = instance;
        *(IntPtr*)(p + 16) = (IntPtr)(delegate* unmanaged<uint, IntPtr, IntPtr, void>)&RegisterCallback;
        PublishDone = new ManualResetEventSlim();
        using var stop = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };
        try
        {
            var status = DnsServiceRegister(request, IntPtr.Zero);
            if (status != DNS_REQUEST_PENDING && status != ERROR_SUCCESS)
                throw new InvalidOperationException($"Failed to add service: {new System.ComponentModel.Win32Exception((int)status).Message}");
            PublishDone.Wait(TimeSpan.FromSeconds(10));
            if (Published != ERROR_SUCCESS)
                throw new InvalidOperationException(Published == uint.MaxValue ? "Failed to add service: no answer from the DNS Client service"
                    : $"Failed to add service: {new System.ComponentModel.Win32Exception((int)Published).Message}");
            Console.WriteLine($"Established under name '{name}'");
            stop.Wait();
            PublishDone.Reset();
            if (DnsServiceDeRegister(request, IntPtr.Zero) == DNS_REQUEST_PENDING) PublishDone.Wait(TimeSpan.FromSeconds(3));
            return 0;
        }
        finally
        {
            DnsServiceFreeInstance(instance);
            Marshal.FreeHGlobal(request);
            foreach (var ptr in keys.Concat(values)) Marshal.FreeHGlobal(ptr);
        }
    }

    [UnmanagedCallersOnly]
    static void RegisterCallback(uint status, IntPtr context, IntPtr instance)
    {
        Published = status;
        if (instance != IntPtr.Zero) DnsServiceFreeInstance(instance);
        PublishDone?.Set();
    }

    // --- avahi-daemon ---------------------------------------------------------------------------------

    static int Daemon(string[] args)
    {
        if (args.Length == 1 && args[0] is "-h" or "--help")
        {
            Console.WriteLine("""
                Usage: avahi-daemon -c|--check | -V|--version
                Windows' DNS Client service (Dnscache) is the mDNS responder, so there is no daemon to start.
                  -c --check     Return 0 if the DNS Client service is running
                  -V --version   Show version
                """);
            return 0;
        }
        if (args.Length == 1 && args[0] is "-V" or "--version") { Console.WriteLine($"avahi-daemon {AvahiVersion} (wintools {Commands.Version})"); return 0; }
        if (args.Length == 1 && args[0] is "-c" or "--check") return DnsClientRunning() ? 0 : 1;
        Console.Error.WriteLine("avahi-daemon: Windows' DNS Client service (Dnscache) is the mDNS responder here; there is no separate daemon to start or stop");
        return 1;
    }

    static bool DnsClientRunning()
    {
        var manager = OpenSCManagerW(null, null, 1);   // SC_MANAGER_CONNECT
        if (manager == IntPtr.Zero) return false;
        try
        {
            var service = OpenServiceW(manager, "Dnscache", 4);   // SERVICE_QUERY_STATUS
            if (service == IntPtr.Zero) return false;
            try
            {
                var status = stackalloc uint[7];
                return QueryServiceStatus(service, status) && status[1] == 4;   // SERVICE_RUNNING
            }
            finally { CloseServiceHandle(service); }
        }
        finally { CloseServiceHandle(manager); }
    }

    [DllImport("dnsapi.dll")]
    static extern uint DnsServiceBrowse(IntPtr request, IntPtr cancel);

    [DllImport("dnsapi.dll")]
    static extern uint DnsServiceBrowseCancel(IntPtr cancel);

    [DllImport("dnsapi.dll")]
    static extern uint DnsServiceResolve(IntPtr request, IntPtr cancel);

    [DllImport("dnsapi.dll")]
    static extern uint DnsServiceResolveCancel(IntPtr cancel);

    [DllImport("dnsapi.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr DnsServiceConstructInstance(string serviceName, string hostName, uint* ip4, byte* ip6,
        ushort port, ushort priority, ushort weight, uint propertyCount, IntPtr* keys, IntPtr* values);

    [DllImport("dnsapi.dll")]
    static extern void DnsServiceFreeInstance(IntPtr instance);

    [DllImport("dnsapi.dll")]
    static extern uint DnsServiceRegister(IntPtr request, IntPtr cancel);

    [DllImport("dnsapi.dll")]
    static extern uint DnsServiceDeRegister(IntPtr request, IntPtr cancel);

    [DllImport("dnsapi.dll")]
    static extern void DnsRecordListFree(IntPtr records, int freeType);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenSCManagerW(string machine, string database, uint access);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenServiceW(IntPtr manager, string name, uint access);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool QueryServiceStatus(IntPtr service, uint* status);

    [DllImport("advapi32.dll")]
    static extern bool CloseServiceHandle(IntPtr handle);
}
