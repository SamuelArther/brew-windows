using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Brew;

internal static class RemovalPreflight
{
    public static void Tree(string root)
    {
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.TryPop(out var path))
        {
            var attributes = File.GetAttributes(path);
            Check(path);
            if (attributes.HasFlag(FileAttributes.Directory) && !attributes.HasFlag(FileAttributes.ReparsePoint))
                foreach (var entry in Directory.EnumerateFileSystemEntries(path)) pending.Push(entry);
        }
    }

    public static void Check(string path)
    {
        var attributes = File.GetAttributes(path);
        if (!attributes.HasFlag(FileAttributes.Directory) && attributes.HasFlag(FileAttributes.ReadOnly))
            throw new IOException($"Cannot remove read-only file: {path}");
        // Request DELETE access without deleting anything. Open the link itself, never its target.
        using var handle = CreateFile(path, 0x00010000, FileShare.ReadWrite | FileShare.Delete,
            IntPtr.Zero, FileMode.Open, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid)
            throw new IOException($"Cannot remove {path}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFile(string path, uint access, FileShare share, IntPtr security,
        FileMode creation, uint flags, IntPtr template);
}
