using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// pinentry-mac: GnuPG's PIN entry program. Speaks the Assuan pinentry protocol on stdin/stdout, like
// every pinentry; GETPIN asks with the Windows credential prompt and CONFIRM/MESSAGE with a message box.
// Point gpg-agent at it with `pinentry-program <path to pinentry-mac>` in gpg-agent.conf.
static class Pinentry
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("pinentry-mac", "GnuPG PIN entry using the Windows credential prompt", Run);

    const string PinentryVersion = "1.3.1";
    const string Cancelled = "ERR 83886179 Operation cancelled <Pinentry>";
    const string NotConfirmed = "ERR 83886194 Not confirmed <Pinentry>";

    // What the client has set with SETxxx/OPTION since the last RESET.
    sealed class Settings
    {
        public string Description, Prompt = "PIN:", Title, Ok, Cancel, NotOk, Error, KeyInfo, Repeat, RepeatError;
    }

    static int Run(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine($"""
                        pinentry-mac (pinentry) {PinentryVersion}
                        Usage: pinentry-mac [options]
                        Ask securely for a secret and print it to stdout, using the Assuan protocol on stdin/stdout.
                        The PIN is asked for with the Windows credential prompt.
                          -d, --debug            Turn on debugging output
                          -D, --display DISPLAY  Accepted for compatibility (ignored)
                          -T, --ttyname FILE     Accepted for compatibility (ignored)
                          -N, --ttytype NAME     Accepted for compatibility (ignored)
                          -C, --lc-ctype STRING  Accepted for compatibility (ignored)
                          -M, --lc-messages STR  Accepted for compatibility (ignored)
                          -o, --timeout SECS     Accepted for compatibility (ignored)
                          -g, --no-global-grab   Accepted for compatibility (ignored)
                          -W, --parent-wid       Accepted for compatibility (ignored)
                              --version          Print the version
                        """);
                    return 0;
                case "--version":
                    Console.WriteLine($"pinentry-mac (pinentry) {PinentryVersion}\nwintools {Commands.Version}: Windows credential prompt flavor");
                    return 0;
                case "-d" or "--debug" or "-g" or "--no-global-grab" or "-e" or "--enhanced": break;
                case "-D" or "--display" or "-T" or "--ttyname" or "-N" or "--ttytype" or "-C" or "--lc-ctype"
                    or "-M" or "--lc-messages" or "-o" or "--timeout" or "-W" or "--parent-wid" or "-c" or "--colors"
                    or "-a" or "--ttyalert":
                    if (++i >= args.Length) throw new UsageException($"option '{args[i - 1]}' requires an argument");
                    break;
                default:
                    if (args[i].StartsWith("--") && args[i].Contains('=')) break;
                    throw new UsageException($"unknown option '{args[i]}'");
            }
        }
        return Serve();
    }

    static int Serve()
    {
        var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
        var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        var settings = new Settings();
        output.WriteLine("OK Pleased to meet you");
        while (input.ReadLine() is { } line)
        {
            line = line.TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var space = line.IndexOf(' ');
            var command = (space < 0 ? line : line[..space]).ToUpperInvariant();
            var arg = space < 0 ? "" : Unescape(line[(space + 1)..]);
            switch (command)
            {
                case "SETDESC": settings.Description = arg; output.WriteLine("OK"); break;
                case "SETPROMPT": settings.Prompt = arg; output.WriteLine("OK"); break;
                case "SETTITLE": settings.Title = arg; output.WriteLine("OK"); break;
                case "SETOK": settings.Ok = arg; output.WriteLine("OK"); break;
                case "SETCANCEL": settings.Cancel = arg; output.WriteLine("OK"); break;
                case "SETNOTOK": settings.NotOk = arg; output.WriteLine("OK"); break;
                case "SETERROR": settings.Error = arg; output.WriteLine("OK"); break;
                case "SETKEYINFO": settings.KeyInfo = arg; output.WriteLine("OK"); break;
                case "SETREPEAT": settings.Repeat = arg.Length > 0 ? arg : "Repeat:"; output.WriteLine("OK"); break;
                case "SETREPEATERROR": settings.RepeatError = arg; output.WriteLine("OK"); break;
                case "SETQUALITYBAR" or "SETQUALITYBAR_TT" or "SETGENPIN" or "SETGENPIN_TT" or "SETTIMEOUT"
                    or "SETCONSTRAINTS" or "SETFORMATTEDPASSPHRASE" or "OPTION" or "NOP":
                    output.WriteLine("OK");
                    break;
                case "GETINFO": GetInfo(output, arg); break;
                case "GETPIN": GetPin(output, settings); break;
                case "CONFIRM": Confirm(output, settings, oneButton: arg.Contains("--one-button")); break;
                case "MESSAGE": Confirm(output, settings, oneButton: true); break;
                case "RESET": settings = new Settings(); output.WriteLine("OK"); break;
                case "HELP":
                    foreach (var name in new[] { "SETDESC", "SETPROMPT", "SETTITLE", "SETOK", "SETCANCEL", "SETNOTOK",
                                 "SETERROR", "SETKEYINFO", "SETREPEAT", "SETREPEATERROR", "OPTION", "GETINFO", "GETPIN",
                                 "CONFIRM", "MESSAGE", "RESET", "NOP", "BYE" })
                        output.WriteLine("# " + name);
                    output.WriteLine("OK");
                    break;
                case "BYE": output.WriteLine("OK closing connection"); return 0;
                default: output.WriteLine("ERR 536871187 Unknown IPC command <User defined source 1>"); break;
            }
        }
        return 0;
    }

    static void GetInfo(StreamWriter output, string what)
    {
        string value = what switch
        {
            "version" => PinentryVersion,
            "pid" => Environment.ProcessId.ToString(),
            "flavor" => "w32",
            "ttyinfo" => "- - - - 0/0 -",
            _ => null,
        };
        if (value == null) { output.WriteLine("ERR 83886360 IPC parameter error <Pinentry>"); return; }
        output.WriteLine("D " + Escape(value));
        output.WriteLine("OK");
    }

    static void GetPin(StreamWriter output, Settings settings)
    {
        var error = settings.Error;
        while (true)
        {
            var pin = Prompt(settings, settings.Prompt, error);
            if (pin == null) { output.WriteLine(Cancelled); break; }
            if (settings.Repeat != null)
            {
                var again = Prompt(settings, settings.Repeat, null);
                if (again == null) { output.WriteLine(Cancelled); break; }
                if (again != pin)
                {
                    error = settings.RepeatError ?? "Passphrases don't match";
                    continue;
                }
                output.WriteLine("S PIN_REPEATED");
            }
            // Assuan data lines are limited to 1000 bytes, so long secrets go out in pieces.
            var escaped = Escape(pin);
            for (var i = 0; i < escaped.Length; i += 900)
                output.WriteLine("D " + escaped.Substring(i, Math.Min(900, escaped.Length - i)));
            output.WriteLine("OK");
            break;
        }
        settings.Error = null;
    }

    static void Confirm(StreamWriter output, Settings settings, bool oneButton)
    {
        var text = settings.Error != null ? settings.Error + "\n\n" + settings.Description : settings.Description ?? "";
        var result = MessageBoxW(IntPtr.Zero, text, settings.Title ?? "pinentry",
            (oneButton ? MB_OK : MB_OKCANCEL) | MB_ICONQUESTION | MB_SETFOREGROUND | MB_TOPMOST);
        output.WriteLine(result == IDOK ? "OK" : NotConfirmed);
        settings.Error = null;
    }

    // The Windows credential prompt with the user name fixed to the prompt text; only the password
    // box is typed into. Returns null when cancelled.
    static unsafe string Prompt(Settings settings, string prompt, string error)
    {
        var message = new StringBuilder();
        if (error != null) message.Append(error).Append("\n\n");
        message.Append(settings.Description ?? "Please enter the passphrase.");
        var info = new CREDUI_INFO
        {
            cbSize = Marshal.SizeOf<CREDUI_INFO>(),
            pszMessageText = message.ToString(),
            pszCaptionText = settings.Title ?? "pinentry",
        };
        var label = Clean(prompt).TrimEnd(':', ' ');
        if (label.Length == 0) label = "PIN";

        // Pre-fill the user name box with the prompt label.
        var inSize = 0;
        CredPackAuthenticationBufferW(0, label, "", IntPtr.Zero, ref inSize);
        var inBuffer = Marshal.AllocCoTaskMem(inSize);
        try
        {
            if (!CredPackAuthenticationBufferW(0, label, "", inBuffer, ref inSize))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            uint package = 0;
            var save = false;
            var result = CredUIPromptForWindowsCredentialsW(ref info, 0, ref package, inBuffer, (uint)inSize,
                out var outBuffer, out var outSize, ref save, CREDUIWIN_GENERIC | CREDUIWIN_IN_CRED_ONLY);
            if (result != 0) return null;   // ERROR_CANCELLED, or no desktop to show the prompt on
            try
            {
                int userLen = 514, domainLen = 338, passLen = 4096;
                var user = new char[userLen];
                var domain = new char[domainLen];
                var pass = new char[passLen];
                fixed (char* u = user, d = domain, p = pass)
                {
                    if (!CredUnPackAuthenticationBufferW(0, outBuffer, outSize, u, ref userLen, d, ref domainLen, p, ref passLen))
                        return null;
                    var pin = new string(p, 0, Math.Max(0, passLen - 1));
                    Array.Clear(pass);
                    return pin;
                }
            }
            finally
            {
                new Span<byte>((void*)outBuffer, (int)outSize).Clear();
                Marshal.FreeCoTaskMem(outBuffer);
            }
        }
        finally { Marshal.FreeCoTaskMem(inBuffer); }
    }

    // Prompts can carry a '_' mnemonic marker (e.g. "_Passphrase:"); the user-name box doesn't use one.
    static string Clean(string text) => text.Replace("__", "\u0001").Replace("_", "").Replace("\u0001", "_");

    static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
            sb.Append(c switch { '%' => "%25", '\r' => "%0D", '\n' => "%0A", _ => c.ToString() });
        return sb.ToString();
    }

    static string Unescape(string text)
    {
        if (!text.Contains('%')) return text;
        var bytes = new List<byte>();
        var raw = Encoding.UTF8.GetBytes(text);
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] == '%' && i + 2 < raw.Length && IsHex(raw[i + 1]) && IsHex(raw[i + 2]))
            {
                bytes.Add(Convert.ToByte(Encoding.ASCII.GetString(raw, i + 1, 2), 16));
                i += 2;
            }
            else bytes.Add(raw[i]);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    static bool IsHex(byte b) => b is >= (byte)'0' and <= (byte)'9' or >= (byte)'a' and <= (byte)'f' or >= (byte)'A' and <= (byte)'F';

    const uint CREDUIWIN_GENERIC = 0x1, CREDUIWIN_IN_CRED_ONLY = 0x20;
    const uint MB_OK = 0x0, MB_OKCANCEL = 0x1, MB_ICONQUESTION = 0x20, MB_SETFOREGROUND = 0x10000, MB_TOPMOST = 0x40000;
    const int IDOK = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct CREDUI_INFO
    {
        public int cbSize;
        public IntPtr hwndParent;
        public string pszMessageText;
        public string pszCaptionText;
        public IntPtr hbmBanner;
    }

    [DllImport("credui.dll", CharSet = CharSet.Unicode)]
    static extern int CredUIPromptForWindowsCredentialsW(ref CREDUI_INFO info, int authError, ref uint authPackage,
        IntPtr inBuffer, uint inSize, out IntPtr outBuffer, out uint outSize, ref bool save, uint flags);

    [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredPackAuthenticationBufferW(uint flags, string user, string password, IntPtr buffer, ref int size);

    [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern unsafe bool CredUnPackAuthenticationBufferW(uint flags, IntPtr buffer, uint size, char* user,
        ref int userLen, char* domain, ref int domainLen, char* password, ref int passwordLen);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int MessageBoxW(IntPtr hwnd, string text, string caption, uint type);
}
