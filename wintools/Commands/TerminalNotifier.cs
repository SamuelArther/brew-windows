using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace WinTools;

// terminal-notifier: post Windows toast notifications (WinRT ToastNotificationManager) from the command
// line, list and remove the ones still in Notification Center. Toasts are sent under Windows PowerShell's
// app id, which every Windows install registers, unless -sender names another installed app's id.
static class TerminalNotifier
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("terminal-notifier", "Send Windows toast notifications from the command-line", Run);

    internal const string DefaultSender = @"{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe";
    const string Group = "terminal-notifier";

    static int Run(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        var flags = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-help" or "--help" or "-h": Help(); return 0;
                case "-version" or "--version": Console.WriteLine($"terminal-notifier {Commands.Version}."); return 0;
                case "-ignoreDnD" or "-json": flags.Add(arg); break;
                case "-message" or "-title" or "-subtitle" or "-sound" or "-group" or "-remove" or "-list" or "-activate" or "-sender"
                    or "-appIcon" or "-contentImage" or "-open" or "-execute" or "-timeout":
                    options[arg] = ++i < args.Length ? args[i] : throw new UsageException($"{arg} needs a value");
                    break;
                default: throw new UsageException($"unknown option {arg} (see terminal-notifier -help)");
            }
        }
        var sender = options.GetValueOrDefault("-sender", DefaultSender);
        try
        {
            if (options.TryGetValue("-list", out var listId)) return List(sender, listId);
            if (options.TryGetValue("-remove", out var removeId)) return Remove(sender, removeId);
            if (!options.ContainsKey("-message") && Console.IsInputRedirected)
            {
                var piped = Console.In.ReadToEnd().TrimEnd('\r', '\n');
                if (piped.Length > 0) options["-message"] = piped;
            }
            if (!options.ContainsKey("-message"))
            {
                Help();
                return 1;
            }
            return Post(options, flags, sender);
        }
        catch (System.Runtime.InteropServices.COMException e)
        {
            throw new InvalidOperationException($"Windows notifications are not available here (0x{e.HResult:x8})");
        }
    }

    static void Help() => Console.WriteLine("""
        terminal-notifier - Send Windows toast notifications.

        Usage: terminal-notifier -[message|list|remove] [VALUE|ID|ID] [options]

           Either of these is required (unless message data is piped to the tool):

               -help              Display this help banner.
               -version           Display terminal-notifier version.
               -message VALUE     The notification message.
               -remove ID         Removes a notification with the specified 'group' ID.
               -list ID           If the specified 'group' ID exists show when it was delivered,
                                  or use 'ALL' as ID to see all notifications.

           Optional:

               -title VALUE       The notification title. Defaults to 'Terminal'.
               -subtitle VALUE    The notification subtitle.
               -sound NAME        Play a sound: 'default', or one of Windows' notification sounds
                                  (IM, Mail, Reminder, SMS, Alarm, Call, ...). Silent otherwise.
               -group ID          A new notification with the same ID replaces the old one.
               -activate ID       App to activate when clicked: an app id (AUMID) or program name/path.
               -sender ID         Post as this app id (AUMID), e.g. Microsoft.WindowsTerminal_8wekyb3d8bbwe!App.
               -appIcon PATH      Image (file or http URL) to use as the notification's icon.
               -contentImage PATH Image to show inside the notification.
               -open URL          Open URL when the notification is clicked.
               -execute COMMAND   Run a shell command when the notification is clicked.
               -ignoreDnD         Post with high priority (Windows still honours Do not disturb rules).

        -activate and -execute keep terminal-notifier running until the notification is clicked or dismissed.
        """);

    static int Post(Dictionary<string, string> o, HashSet<string> flags, string sender)
    {
        var title = o.GetValueOrDefault("-title", "Terminal");
        var subtitle = o.GetValueOrDefault("-subtitle");
        var message = o["-message"];
        var group = o.GetValueOrDefault("-group");
        var waits = o.ContainsKey("-execute") || o.ContainsKey("-activate");
        var launch = o.TryGetValue("-open", out var url) && !waits ? $" launch=\"{Esc(url)}\" activationType=\"protocol\"" : "";
        var xml = $"<toast displayTimestamp=\"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\"{launch}><visual><binding template=\"ToastGeneric\">"
                  + $"<text>{Esc(title)}</text>"
                  + (subtitle != null ? $"<text>{Esc(subtitle)}</text>" : "")
                  + $"<text>{Esc(message)}</text>"
                  + (o.TryGetValue("-appIcon", out var icon) ? $"<image placement=\"appLogoOverride\" src=\"{Esc(ImageSource(icon))}\"/>" : "")
                  + (o.TryGetValue("-contentImage", out var image) ? $"<image src=\"{Esc(ImageSource(image))}\"/>" : "")
                  + "</binding></visual>"
                  + Audio(o.GetValueOrDefault("-sound"))
                  + "</toast>";
        Show(xml, sender, group, flags.Contains("-ignoreDnD"), waits ? (o.GetValueOrDefault("-execute"), o.GetValueOrDefault("-activate"), url) : null);
        return 0;
    }

    // Posts a simple toast; used by other tools (BluetoothConnector --notify).
    internal static void Notify(string title, string message)
    {
        try { Show($"<toast><visual><binding template=\"ToastGeneric\"><text>{Esc(title)}</text><text>{Esc(message)}</text></binding></visual></toast>", DefaultSender, null, false, null); }
        catch (Exception e) when (e is System.Runtime.InteropServices.COMException or InvalidOperationException) { }
    }

    static void Show(string xml, string sender, string group, bool priority, (string Execute, string Activate, string Url)? onClick)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var toast = new ToastNotification(doc);
        if (group != null) { toast.Tag = Tag(group); toast.Group = Group; }
        else toast.Group = Group;
        if (priority) toast.Priority = ToastNotificationPriority.High;
        using var done = new ManualResetEventSlim();
        var activated = false;
        if (onClick != null)
        {
            toast.Activated += (_, _) => { activated = true; done.Set(); };
            toast.Dismissed += (_, _) => done.Set();
            toast.Failed += (_, _) => done.Set();
        }
        ToastNotificationManager.CreateToastNotifier(sender).Show(toast);
        if (onClick is not var (execute, activate, url)) return;
        done.Wait(TimeSpan.FromMinutes(10));
        if (!activated) return;
        if (url != null) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        if (activate != null)
            Process.Start(activate.Contains('!')
                ? new ProcessStartInfo("explorer.exe", $"shell:AppsFolder\\{activate}") { UseShellExecute = false }
                : new ProcessStartInfo(activate) { UseShellExecute = true });
        if (execute != null)
        {
            var shell = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe") { UseShellExecute = false };
            shell.ArgumentList.Add("/c");
            shell.ArgumentList.Add(execute);
            Process.Start(shell)?.WaitForExit();
        }
    }

    // Tags are limited to 64 characters.
    static string Tag(string id) => id.Length <= 64 ? id : id[..64];

    static string Audio(string sound)
    {
        if (sound == null) return "<audio silent=\"true\"/>";
        if (sound.Equals("default", StringComparison.OrdinalIgnoreCase)) return "<audio src=\"ms-winsoundevent:Notification.Default\"/>";
        var name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(sound.ToLowerInvariant());
        return name switch
        {
            "Im" => "<audio src=\"ms-winsoundevent:Notification.IM\"/>",
            "Sms" => "<audio src=\"ms-winsoundevent:Notification.SMS\"/>",
            "Mail" or "Reminder" => $"<audio src=\"ms-winsoundevent:Notification.{name}\"/>",
            "Alarm" or "Call" => $"<audio src=\"ms-winsoundevent:Notification.Looping.{name}\"/>",
            _ => "<audio src=\"ms-winsoundevent:Notification.Default\"/>",   // macOS sound names (Basso, Glass...) have no Windows twin
        };
    }

    // Local paths become file URIs; remote images are downloaded (desktop toasts only show local files).
    static string ImageSource(string location)
    {
        if (location.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || location.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var dir = Path.Combine(Path.GetTempPath(), "terminal-notifier");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(location))) + Path.GetExtension(new Uri(location).AbsolutePath));
            if (!File.Exists(file))
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                File.WriteAllBytes(file, http.GetByteArrayAsync(location).GetAwaiter().GetResult());
            }
            return new Uri(file).AbsoluteUri;
        }
        if (location.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) return location;
        var full = Path.GetFullPath(location);
        if (!File.Exists(full)) throw new InvalidOperationException($"{location}: No such file");
        return new Uri(full).AbsoluteUri;
    }

    static string Esc(string s) => SecurityElement.Escape(s) ?? "";

    static int List(string sender, string id)
    {
        var history = ToastNotificationManager.History.GetHistory(sender)
            .Where(t => t.Group == Group && (id == "ALL" || t.Tag == Tag(id))).ToList();
        if (history.Count == 0) return 0;
        Console.WriteLine("GroupID\tTitle\tSubtitle\tMessage\tDelivered At");
        foreach (var toast in history)
        {
            var texts = toast.Content.GetElementsByTagName("text").Select(n => n.InnerText).ToList();
            var (title, subtitle, message) = texts.Count >= 3 ? (texts[0], texts[1], texts[2]) : (texts.ElementAtOrDefault(0), "(null)", texts.ElementAtOrDefault(1));
            var stamp = toast.Content.DocumentElement.GetAttribute("displayTimestamp");
            var delivered = DateTimeOffset.TryParse(stamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
                ? at.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture) : "(unknown)";
            Console.WriteLine($"{(string.IsNullOrEmpty(toast.Tag) ? "(null)" : toast.Tag)}\t{title}\t{subtitle}\t{message}\t{delivered}");
        }
        return 0;
    }

    static int Remove(string sender, string id)
    {
        if (id == "ALL") ToastNotificationManager.History.RemoveGroup(Group, sender);
        else ToastNotificationManager.History.Remove(Tag(id), Group, sender);
        return 0;
    }
}
