using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace WinTools;

// launch_socket_server: holds the public listening socket and proxies every connection to the
// program's own address (LAUNCH_PROGRAM_TCP_ADDRESS), so the program never binds the public port
// itself. On macOS launchd hands over the privileged socket; Windows has no privileged ports, so the
// socket is bound here directly from LAUNCH_SOCKET_ADDRESS (or -l). "-" proxies to an already running
// program instead of starting one.
static class LaunchSocketServer
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("launch_socket_server", "Proxy a public port to a server running without it (socket activation)", Run);
    }

    const string Usage = """
        usage: launch_socket_server [-l <address>]... <program> [<arguments>...]
               launch_socket_server [-l <address>]... -

        Listens on LAUNCH_SOCKET_ADDRESS (default 127.0.0.1:80; -l may be given several times, and the
        variable may list several addresses separated by commas) and proxies each connection to
        LAUNCH_PROGRAM_TCP_ADDRESS, where <program> listens. With "-" no program is started.

        Environment:
          LAUNCH_SOCKET_ADDRESS         public address(es) to listen on, host:port
          LAUNCH_PROGRAM_TCP_ADDRESS    the program's own address, host:port (required)
          LAUNCH_PROGRAM_STARTUP_TIMEOUT  seconds to keep retrying the program's address (default 30)
        """;

    static int Run(string[] args)
    {
        var listen = new List<string>();
        var i = 0;
        for (; i < args.Length; i++)
        {
            if (args[i] is "-h" or "--help") { Console.WriteLine(Usage); return 0; }
            if (args[i] is "-l" or "--listen")
            {
                if (i + 1 >= args.Length) return UsageError("-l takes an address");
                listen.Add(args[++i]);
                continue;
            }
            break;
        }
        if (i >= args.Length) return UsageError(null);
        var program = args[i];
        var programArgs = args[(i + 1)..];

        var target = Environment.GetEnvironmentVariable("LAUNCH_PROGRAM_TCP_ADDRESS");
        if (string.IsNullOrWhiteSpace(target)) return UsageError("LAUNCH_PROGRAM_TCP_ADDRESS must be set");
        var targetEndpoint = ParseEndpoint(target);
        if (listen.Count == 0)
            listen.AddRange((Environment.GetEnvironmentVariable("LAUNCH_SOCKET_ADDRESS") ?? "127.0.0.1:80")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var timeout = int.TryParse(Environment.GetEnvironmentVariable("LAUNCH_PROGRAM_STARTUP_TIMEOUT"), out var t) ? t : 30;

        var listeners = new List<TcpListener>();
        foreach (var address in listen)
        {
            var endpoint = ParseEndpoint(address);
            if (endpoint.Equals(targetEndpoint)) throw new InvalidOperationException($"{address} is also LAUNCH_PROGRAM_TCP_ADDRESS");
            var listener = new TcpListener(endpoint);
            try { listener.Start(); }
            catch (SocketException e) { throw new InvalidOperationException($"can't listen on {address}: {e.Message}"); }
            listeners.Add(listener);
            Console.Error.WriteLine($"launch_socket_server: listening on {listener.LocalEndpoint}, proxying to {target}");
        }

        Process child = null;
        if (program != "-")
        {
            var start = new ProcessStartInfo(program) { UseShellExecute = false };
            foreach (var a in programArgs) start.ArgumentList.Add(a);
            try { child = Process.Start(start); }
            catch (System.ComponentModel.Win32Exception e) { throw new InvalidOperationException($"{program}: {e.Message}"); }
        }

        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        foreach (var listener in listeners) _ = Accept(listener, targetEndpoint, timeout, stop.Token);

        var code = 0;
        if (child != null)
        {
            try { child.WaitForExitAsync(stop.Token).GetAwaiter().GetResult(); code = child.ExitCode; }
            catch (OperationCanceledException) { if (!child.HasExited) child.Kill(entireProcessTree: true); }
        }
        else stop.Token.WaitHandle.WaitOne();
        foreach (var listener in listeners) listener.Stop();
        return code;
    }

    static int UsageError(string message)
    {
        if (message != null) Console.Error.WriteLine($"launch_socket_server: {message}");
        Console.Error.WriteLine(Usage.Split('\n')[0] + "\n" + Usage.Split('\n')[1]);
        return 2;
    }

    // "host:port", "[::1]:port" or ":port" (all interfaces).
    internal static IPEndPoint ParseEndpoint(string address)
    {
        address = address.Replace("tcp://", "");
        var colon = address.LastIndexOf(':');
        if (colon < 0 || !int.TryParse(address[(colon + 1)..], out var port) || port is < 0 or > 65535)
            throw new UsageException($"invalid address '{address}' (expected host:port)");
        var host = address[..colon].Trim('[', ']');
        if (host is "" or "*" or "0.0.0.0") return new IPEndPoint(IPAddress.Any, port);
        if (host == "localhost") return new IPEndPoint(IPAddress.Loopback, port);
        if (IPAddress.TryParse(host, out var ip)) return new IPEndPoint(ip, port);
        try { return new IPEndPoint(Dns.GetHostAddresses(host).First(), port); }
        catch (Exception e) when (e is SocketException or InvalidOperationException) { throw new InvalidOperationException($"can't resolve {host}"); }
    }

    static async Task Accept(TcpListener listener, IPEndPoint target, int timeout, CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(stop); }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException) { return; }
            _ = Proxy(client, target, timeout, stop);
        }
    }

    // Connects to the program (retrying while it starts up) and copies bytes both ways until either side closes.
    static async Task Proxy(TcpClient client, IPEndPoint target, int timeout, CancellationToken stop)
    {
        using var downstream = client;
        using var upstream = new TcpClient(target.AddressFamily);
        var deadline = DateTime.UtcNow.AddSeconds(timeout);
        while (true)
        {
            try { await upstream.ConnectAsync(target, stop); break; }
            catch (SocketException) when (DateTime.UtcNow < deadline) { await Task.Delay(200, stop).ContinueWith(_ => { }); }
            catch (Exception e) when (e is SocketException or OperationCanceledException)
            {
                Console.Error.WriteLine($"launch_socket_server: can't reach {target}: {e.Message}");
                return;
            }
        }
        var a = downstream.GetStream();
        var b = upstream.GetStream();
        async Task Pipe(NetworkStream from, NetworkStream to, Socket toSocket)
        {
            try { await from.CopyToAsync(to, stop); toSocket.Shutdown(SocketShutdown.Send); }
            catch (Exception e) when (e is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { }
        }
        await Task.WhenAll(Pipe(a, b, upstream.Client), Pipe(b, a, downstream.Client));
    }
}
