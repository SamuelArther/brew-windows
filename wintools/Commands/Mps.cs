using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WinTools;

// mps (MPlayerShell): play media with MPlayer in a proper native window instead of its bare video
// output. On macOS that meant a Cocoa window driven over mplayer's slave mode; on Windows, MPlayer's
// own Direct3D output already gives a real window (resizable, -fs fullscreen, -ontop float), so mps
// runs mplayer with that output and good window defaults, passing every other option through, and
// mpv (which reads the same options) when MPlayer isn't installed. Keyboard control stays in the
// terminal and the video window, like MPlayerShell.
static class Mps
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("mps", "Play media with MPlayer in a native Windows video window (MPlayerShell)", Run);

    static int Run(string[] args)
    {
        if (args.Length > 0 && args[0] is "--help" or "--mps-help")
        {
            Console.WriteLine("""
                Usage: mps [mplayer options] [path|URL]...
                Play media with MPlayer (or mpv) in a native Windows video window.

                mps adds Direct3D video output, a window title and keeps the window's aspect when
                resized; every option is passed to mplayer unchanged, so -fs (fullscreen),
                -ontop (float on top), -vo, -ao, -ss and the rest work as usual. mps with no
                arguments prints mplayer's own help.

                  --help        show this help
                  --player PATH use this mplayer or mpv executable
                """);
            return 0;
        }
        string player = null;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--player") { player = ++i < args.Length ? args[i] : throw new UsageException("--player needs a path"); continue; }
            if (args[i].StartsWith("--player=")) { player = args[i][9..]; continue; }
            rest.Add(args[i]);
        }
        player ??= Find("mplayer") ?? Find("mpv");
        if (player == null)
        {
            Console.Error.WriteLine("mps: mplayer not found on PATH; install MPlayer or mpv for Windows, or pass --player PATH");
            return 1;
        }
        var mpv = Path.GetFileNameWithoutExtension(player).Equals("mpv", StringComparison.OrdinalIgnoreCase);
        var psi = new ProcessStartInfo(player) { UseShellExecute = false };
        foreach (var a in PlayerArgs(rest, mpv)) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"could not start {player}");
        // Ctrl+C belongs to the player (it quits cleanly); mps just waits for it.
        Console.CancelKeyPress += (_, e) => e.Cancel = true;
        p.WaitForExit();
        return p.ExitCode;
    }

    // The window defaults mps adds in front of the user's options (which override them).
    internal static List<string> PlayerArgs(List<string> user, bool mpv)
    {
        if (user.Count == 0) return mpv ? ["--help"] : [];
        var has = (string opt) => user.Any(a => a.TrimStart('-').Split('=')[0].Equals(opt, StringComparison.OrdinalIgnoreCase));
        var list = new List<string>();
        if (mpv)
        {
            if (!has("vo")) list.Add("--vo=gpu");
            if (!has("title")) list.Add("--title=${media-title} - mps");
            list.Add("--keepaspect-window=yes");
            list.Add("--force-window=yes");
        }
        else
        {
            if (!has("vo")) { list.Add("-vo"); list.Add("direct3d,gl,directx,"); }
            if (!has("title")) { list.Add("-title"); list.Add(Title(user)); }
            list.Add("-keepaspect");
            list.Add("-fixed-vo");   // one window for a whole playlist, as MPlayerShell kept
        }
        list.AddRange(user);
        return list;
    }

    // The window title: the first file or URL named, as MPlayerShell's window showed.
    static string Title(List<string> user)
    {
        var media = user.LastOrDefault(a => !a.StartsWith('-') && (a.Contains("://") || File.Exists(a)));
        if (media == null) return "mps";
        var name = Uri.TryCreate(media, UriKind.Absolute, out var u) && !u.IsFile ? u.Segments.LastOrDefault() ?? media : Path.GetFileName(media);
        return string.IsNullOrEmpty(name) ? "mps" : Uri.UnescapeDataString(name);
    }

    static string Find(string name)
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var exe = Path.Combine(dir.Trim('"'), name + ".exe");
                if (File.Exists(exe)) return exe;
            }
            catch (ArgumentException) { }
        }
        return null;
    }
}
