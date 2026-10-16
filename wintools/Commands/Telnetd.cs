using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace WinTools;

// telnetd: a TELNET server. Windows has no inetd, so it always runs standalone (`telnetd -debug [port]`,
// as BSD telnetd does for debugging). It negotiates ECHO, SGA and NAWS, asks for login/Password, checks
// them with LogonUser, and then runs the shell (%COMSPEC%) under a pseudoconsole (ConPTY) relayed over the
// connection. It runs as an ordinary user, so it only logs in the account that started it.
// TELNET sends passwords in the clear: prefer OpenSSH Server (sshd) for anything off this machine.
static class Telnetd
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("telnetd", "TELNET server (standalone; logs in the account running it)", Run);

    const string Usage = "usage: telnetd -debug [port] [-b address] [-4 | -6] [-h]";

    static int Run(string[] args)
    {
        var debug = false;
        var port = 23;
        IPAddress bind = null;
        var ipv6 = false;
        var banner = true;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-help":
                    Console.WriteLine($"""
                        {Usage}
                        TELNET server. Windows has no inetd, so it runs standalone and serves connections itself.
                          -debug [port]  Listen on port (default 23)
                          -b address     Listen on this address only (default: all)
                          -4, -6         Listen on IPv4 (default) or IPv6
                          -h             Don't print host information before the login prompt
                        Only the account running telnetd can log in; the shell is %COMSPEC% (cmd.exe).
                        """);
                    return 0;
                case "-debug":
                    debug = true;
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                    {
                        if (p is < 1 or > 65535) return Fail();
                        port = p;
                        i++;
                    }
                    break;
                case "-b":
                    if (++i >= args.Length || !IPAddress.TryParse(args[i], out bind)) return Fail();
                    break;
                case "-4": ipv6 = false; break;
                case "-6": ipv6 = true; break;
                case "-h": banner = false; break;
                default: return Fail();
            }
        }
        if (!debug)
        {
            Console.Error.WriteLine("telnetd: Windows has no inetd to start telnetd per connection; run it standalone with telnetd -debug [port]");
            return 1;
        }
        bind ??= ipv6 ? IPAddress.IPv6Any : IPAddress.Any;
        var listener = new TcpListener(bind, port);
        try { listener.Start(); }
        catch (SocketException e) { throw new InvalidOperationException($"can't listen on {bind}:{port}: {e.Message}"); }
        Console.Error.WriteLine($"telnetd: listening on {listener.LocalEndpoint}");
        while (true)
        {
            var client = listener.AcceptTcpClient();
            new Thread(() =>
            {
                try { new Session(client, banner).Serve(); }
                catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or InvalidOperationException) { }
                finally { client.Dispose(); }
            }) { IsBackground = true }.Start();
        }
    }

    static int Fail()
    {
        Console.Error.WriteLine(Usage);
        return 1;
    }

    sealed class Session(TcpClient client, bool banner)
    {
        const byte SE = 240, AYT = 246, IP = 244, SB = 250, WILL = 251, WONT = 252, DO = 253, DONT = 254, IAC = 255;
        const byte ECHO = 1, SGA = 3, NAWS = 31;

        readonly NetworkStream net = client.GetStream();
        readonly object sendLock = new();
        readonly HashSet<(byte, byte)> answered = [];
        readonly List<byte> subnegotiation = [];
        readonly byte[] readBuffer = new byte[4096];
        int state;
        byte verb;
        bool lastCR;
        short cols = 80, rows = 24;
        IntPtr pty;

        public void Serve()
        {
            Send([IAC, WILL, ECHO, IAC, WILL, SGA, IAC, DO, NAWS]);
            if (banner) Send($"\r\nMicrosoft Windows {Environment.OSVersion.Version} ({Dns.GetHostName()})\r\n\r\n");            for (var attempt = 0; attempt < 3; attempt++)
            {
                Send("login: ");
                var user = ReadLine(echo: true);
                if (user == null) return;
                if (user.Length == 0) { attempt--; continue; }
                Send("Password:");
                var password = ReadLine(echo: false);
                Send("\r\n");
                if (password == null) return;
                switch (Logon(user, password))
                {
                    case true: Shell(); return;
                    case null:
                        Send($"This telnetd runs as {Environment.UserName} and can only log that account in; run telnetd as {user} to log in as them.\r\n");
                        return;
                }
                Thread.Sleep(1000);
                Send("Login incorrect\r\n");
            }
        }

        // true: the right password for the account running telnetd; null: right password, other account.
        static bool? Logon(string user, string password)
        {
            string domain = ".";
            var slash = user.IndexOf('\\');
            if (slash >= 0) { domain = user[..slash]; user = user[(slash + 1)..]; }
            else if (user.Contains('@')) domain = null;
            if (!LogonUserW(user, domain, password, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, out var token)) return false;
            try
            {
                using var them = new WindowsIdentity(token);
                using var me = WindowsIdentity.GetCurrent();
                return them.User == me.User ? true : null;
            }
            finally { CloseHandle(token); }
        }

        // --- TELNET input ---------------------------------------------------------------------------

        // Reads from the connection, handles TELNET commands, and returns the data bytes (null at EOF).
        List<byte> Receive()
        {
            var n = net.Read(readBuffer, 0, readBuffer.Length);
            if (n <= 0) return null;
            var data = new List<byte>(n);
            for (var i = 0; i < n; i++)
            {
                var b = readBuffer[i];
                switch (state)
                {
                    case 0:
                        if (b == IAC) state = 1;
                        else if (lastCR && b is 0 or 10) lastCR = false;   // CR NUL and CR LF are one Enter
                        else { data.Add(b); lastCR = b == 13; }
                        break;
                    case 1:
                        state = 0;
                        if (b == IAC) data.Add(IAC);
                        else if (b is WILL or WONT or DO or DONT) { verb = b; state = 2; }
                        else if (b == SB) { subnegotiation.Clear(); state = 3; }
                        else if (b == IP) data.Add(3);   // Interrupt Process -> Ctrl+C
                        else if (b == AYT) Send("\r\n[Yes]\r\n");
                        break;
                    case 2:
                        Negotiate(verb, b);
                        state = 0;
                        break;
                    case 3:
                        if (b == IAC) state = 4; else subnegotiation.Add(b);
                        break;
                    case 4:
                        if (b == SE) { Subnegotiation(); state = 0; }
                        else if (b == IAC) { subnegotiation.Add(IAC); state = 3; }
                        else state = 0;
                        break;
                }
            }
            return data;
        }

        void Negotiate(byte command, byte option)
        {
            // Our own WILL ECHO/SGA and DO NAWS come back as DO/WILL acknowledgements: nothing to answer.
            if (command == DO && option is ECHO or SGA) return;
            if (command == WILL && option == NAWS) return;
            if (command is WONT or DONT || !answered.Add((command, option))) return;
            if (command == WILL) Send([IAC, option == SGA ? DO : DONT, option]);
            else Send([IAC, WONT, option]);
        }

        void Subnegotiation()
        {
            if (subnegotiation.Count < 5 || subnegotiation[0] != NAWS) return;
            cols = (short)Math.Clamp(subnegotiation[1] << 8 | subnegotiation[2], 1, 1000);
            rows = (short)Math.Clamp(subnegotiation[3] << 8 | subnegotiation[4], 1, 1000);
            if (pty != IntPtr.Zero) ResizePseudoConsole(pty, Coord(cols, rows));
        }

        // Login prompts: line editing is ours until the shell starts.
        string ReadLine(bool echo)
        {
            var line = new List<byte>();
            while (true)
            {
                var data = Receive();
                if (data == null) return null;
                foreach (var b in data)
                {
                    switch (b)
                    {
                        case 13 or 10:
                            if (echo) Send("\r\n");
                            return Encoding.UTF8.GetString(line.ToArray());
                        case 8 or 127:
                            if (line.Count == 0) break;
                            // drop a whole UTF-8 sequence
                            while (line.Count > 0 && (line[^1] & 0xC0) == 0x80) line.RemoveAt(line.Count - 1);
                            if (line.Count > 0) line.RemoveAt(line.Count - 1);
                            if (echo) Send("\b \b");
                            break;
                        case 3 or 4: return null;
                        default:
                            if (b < 32 || line.Count >= 256) break;
                            line.Add(b);
                            if (echo) Send([b]);
                            break;
                    }
                }
            }
        }

        void Send(string text) => Send(Encoding.UTF8.GetBytes(text));

        void Send(byte[] bytes)
        {
            lock (sendLock) net.Write(bytes);
        }

        // --- The shell under a pseudoconsole --------------------------------------------------------

        unsafe void Shell()
        {
            if (!CreatePipe(out var inRead, out var inWrite, IntPtr.Zero, 0) || !CreatePipe(out var outRead, out var outWrite, IntPtr.Zero, 0))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var hr = CreatePseudoConsole(Coord(cols, rows), inRead, outWrite, 0, out pty);
            if (hr != 0) throw new System.ComponentModel.Win32Exception(hr);

            var size = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
            var attributes = Marshal.AllocHGlobal(size);
            PROCESS_INFORMATION process;
            try
            {
                if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref size) ||
                    !UpdateProcThreadAttribute(attributes, 0, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, pty, IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                var startup = new STARTUPINFOEX { lpAttributeList = attributes };
                startup.StartupInfo.cb = sizeof(STARTUPINFOEX);
                // No std handles of our own: otherwise the shell could pick up telnetd's redirected ones.
                startup.StartupInfo.dwFlags = STARTF_USESTDHANDLES;
                var shell = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
                var commandLine = ("\"" + shell + "\"\0").ToCharArray();
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                fixed (char* cmd = commandLine)
                    if (!CreateProcessW(null, cmd, IntPtr.Zero, IntPtr.Zero, false, EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero,
                            Directory.Exists(home) ? home : null, ref startup, out process))
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                DeleteProcThreadAttributeList(attributes);
                Marshal.FreeHGlobal(attributes);
                CloseHandle(inRead);
                CloseHandle(outWrite);
            }
            CloseHandle(process.hThread);

            var closed = 0;
            void ClosePty()
            {
                if (Interlocked.Exchange(ref closed, 1) == 0) ClosePseudoConsole(pty);
            }
            using var toShell = new FileStream(new SafeFileHandle(inWrite, true), FileAccess.Write, 1);
            using var fromShell = new FileStream(new SafeFileHandle(outRead, true), FileAccess.Read, 1);

            // Shell output -> connection, with IAC bytes doubled.
            var output = new Thread(() =>
            {
                var buffer = new byte[8192];
                try
                {
                    int n;
                    while ((n = fromShell.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        var chunk = new List<byte>(n);
                        for (var i = 0; i < n; i++)
                        {
                            chunk.Add(buffer[i]);
                            if (buffer[i] == IAC) chunk.Add(IAC);
                        }
                        Send(chunk.ToArray());
                    }
                }
                catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException) { }
            }) { IsBackground = true };
            output.Start();

            // Connection -> shell; Enter arrives as CR (CR LF and CR NUL are folded by Receive).
            var input = new Thread(() =>
            {
                try
                {
                    while (Receive() is { } data)
                        if (data.Count > 0) { toShell.Write(data.ToArray()); toShell.Flush(); }
                }
                catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException) { }
                ClosePty();   // the client went away: closing the pseudoconsole ends the shell
            }) { IsBackground = true };
            input.Start();

            WaitForSingleObject(process.hProcess, INFINITE);
            CloseHandle(process.hProcess);
            ClosePty();
            output.Join(5000);
            client.Client.Shutdown(SocketShutdown.Both);
        }

        static int Coord(short x, short y) => (ushort)x | (y << 16);
    }

    const int LOGON32_LOGON_NETWORK = 3, LOGON32_PROVIDER_DEFAULT = 0;
    const uint EXTENDED_STARTUPINFO_PRESENT = 0x80000, STARTF_USESTDHANDLES = 0x100, INFINITE = 0xFFFFFFFF;
    static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x20016;

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public ushort wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public uint dwProcessId, dwThreadId;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool LogonUserW(string user, string domain, string password, int logonType, int provider, out IntPtr token);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CreatePipe(out IntPtr read, out IntPtr write, IntPtr attributes, uint size);

    [DllImport("kernel32.dll")]
    static extern int CreatePseudoConsole(int size, IntPtr input, IntPtr output, uint flags, out IntPtr pty);

    [DllImport("kernel32.dll")]
    static extern int ResizePseudoConsole(IntPtr pty, int size);

    [DllImport("kernel32.dll")]
    static extern void ClosePseudoConsole(IntPtr pty);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returnSize);

    [DllImport("kernel32.dll")]
    static extern void DeleteProcThreadAttributeList(IntPtr list);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern unsafe bool CreateProcessW(string application, char* commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint flags, IntPtr environment, string directory, ref STARTUPINFOEX startup, out PROCESS_INFORMATION info);

    [DllImport("kernel32.dll")]
    static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
