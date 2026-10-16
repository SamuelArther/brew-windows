using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinTools;

// dark-mode: read or switch the Windows app and system theme (AppsUseLightTheme / SystemUsesLightTheme
// under HKCU\...\Themes\Personalize, the values Settings > Personalization > Colors writes), then
// broadcast the ImmersiveColorSet change so running apps and the taskbar repaint.
static class DarkMode
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("dark-mode", "Control Windows dark mode from the command-line", Run);

    const string Key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    static int Run(string[] args)
    {
        var command = args.Length == 0 ? "toggle" : args[0];
        if (args.Length > 1) throw new UsageException("too many arguments (see dark-mode --help)");
        switch (command)
        {
            case "-h" or "--help" or "help":
                Console.WriteLine("""
                    Usage: dark-mode [command]

                    Commands:
                      <none>   Toggle dark mode
                      on       Enable dark mode
                      off      Disable dark mode
                      status   Print "on" or "off"

                    Switches both the app and the Windows (taskbar, Start) theme.
                    """);
                return 0;
            case "--version" or "-v": Console.WriteLine(Commands.Version); return 0;
            case "status": Console.WriteLine(IsDark() ? "on" : "off"); return 0;
            case "on": SetDark(true); return 0;
            case "off": SetDark(false); return 0;
            case "toggle": SetDark(!IsDark()); return 0;
            default: throw new UsageException($"unknown command '{command}' (on, off, status or nothing to toggle)");
        }
    }

    // Light is Windows' default, so a missing value means light.
    static bool IsDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(Key);
        return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
    }

    static void SetDark(bool dark)
    {
        using (var key = Registry.CurrentUser.CreateSubKey(Key))
        {
            key.SetValue("AppsUseLightTheme", dark ? 0 : 1, RegistryValueKind.DWord);
            key.SetValue("SystemUsesLightTheme", dark ? 0 : 1, RegistryValueKind.DWord);
        }
        SendMessageTimeoutW(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet", SMTO_ABORTIFHUNG, 2000, out _);
    }

    static readonly IntPtr HWND_BROADCAST = 0xffff;
    const uint WM_SETTINGCHANGE = 0x1a, SMTO_ABORTIFHUNG = 0x2;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SendMessageTimeoutW(IntPtr hwnd, uint msg, IntPtr wParam, string lParam, uint flags, uint timeout, out IntPtr result);
}
