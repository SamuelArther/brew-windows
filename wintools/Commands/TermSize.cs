using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// term-size (macos-term-size): the size of the console window this process is attached to, read
// from CONOUT$ so it works even when stdout is a pipe (like the original reading /dev/tty).
static class TermSize
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("term-size", "Print the terminal window's columns and rows", Run);
    }

    static int Run(string[] args)
    {
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: term-size\nPrints the terminal's width (columns) and height (rows), one per line.");
                    return 0;
                case "-v" or "--version": Console.WriteLine($"term-size {Commands.Version} (wintools)"); return 0;
                default: throw new UsageException($"unknown argument {arg}");
            }
        }
        var console = CreateFileW("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (console == new IntPtr(-1))
        {
            Console.Error.WriteLine("term-size: no console attached");
            return 1;
        }
        try
        {
            if (!GetConsoleScreenBufferInfo(console, out var info))
            {
                Console.Error.WriteLine("term-size: no console attached");
                return 1;
            }
            Console.WriteLine(info.Right - info.Left + 1);
            Console.WriteLine(info.Bottom - info.Top + 1);
            return 0;
        }
        finally { CloseHandle(console); }
    }

    const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2, OPEN_EXISTING = 3;

    [StructLayout(LayoutKind.Sequential)]
    struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public short SizeX, SizeY, CursorX, CursorY;
        public ushort Attributes;
        public short Left, Top, Right, Bottom;
        public short MaxX, MaxY;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleScreenBufferInfo(IntPtr console, out CONSOLE_SCREEN_BUFFER_INFO info);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
