using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinTools;

// imessage (imessage-ruby): sends a text through the phone paired with Windows. Messages.app's
// Windows counterpart is Phone Link, which handles sms: links; imessage opens one per contact with
// the text filled in, so the message goes out from the phone (iMessage/SMS for iPhones, SMS/RCS
// for Android). Phone Link takes no attachments through links, so --attachment is refused.
static class Imessage
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("imessage", "Send a text through the phone paired with Phone Link", Run);
    }

    const string Help = """
        Usage: imessage [options]

        Specific options:
            -t, --text [TEXT]                The TEXT to deliver
            -a, --attachment [ATTACHMENT]    Add an attachment (not possible through Phone Link)
            -c, --contacts x,y,z             Deliver message to these CONTACTS (phone numbers or addresses)
            -v, --verbose                    Print the sms: link that is opened

        Common options:
            -h, --help                       Show this message
                --version                    Show version

        Phone Link opens each message ready to send from your paired phone.
        """;

    static int Run(string[] args)
    {
        string text = null, attachment = null;
        var contacts = new List<string>();
        var verbose = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"missing argument: {arg}");
            var eq = arg.IndexOf('=');
            if (arg.StartsWith("--") && eq > 0)
            {
                args[i] = arg[(eq + 1)..];
                arg = arg[..eq];
                i--;
            }
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "--version": Console.WriteLine($"imessage v{Commands.Version}"); return 0;
                case "-t" or "--text": text = Value(); break;
                case "-a" or "--attachment": attachment = Value(); break;
                case "-c" or "--contacts":
                    contacts.AddRange(Value().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "-v" or "--verbose" or "-d" or "--debug": verbose = true; break;
                default: throw new UsageException($"invalid option: {arg}");
            }
        }
        if (contacts.Count == 0) throw new UsageException("You must specify at least one contact (-c)");
        if (text == null && attachment == null) throw new UsageException("You must specify a text or an attachment");
        if (attachment != null)
            throw new InvalidOperationException("Phone Link cannot take attachments from other programs; send the text alone, or share the file from Explorer's Share menu");
        if (Registry.ClassesRoot.OpenSubKey("sms") == null)
            throw new InvalidOperationException("no app handles sms: links; install Phone Link (winget install 9NMPJ99VJBWV) and pair your phone");

        foreach (var contact in contacts)
        {
            var uri = SmsUri(contact, text);
            if (verbose) Console.WriteLine(uri);
            var result = ShellExecuteW(IntPtr.Zero, "open", uri, null, null, 1);
            if ((long)result <= 32) throw new InvalidOperationException($"could not open Phone Link for {contact} (error {(long)result})");
        }
        return 0;
    }

    internal static string SmsUri(string contact, string text) =>
        "sms:" + Uri.EscapeDataString(contact).Replace("%2B", "+") + (text != null ? "?body=" + Uri.EscapeDataString(text) : "");

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr ShellExecuteW(IntPtr hwnd, string verb, string file, string parameters, string directory, int show);
}
