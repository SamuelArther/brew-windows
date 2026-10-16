using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WinTools;

// What a ProjFS provider projects: folders and files addressed by paths relative to the
// virtualization root ("" is the root, "\" separates). Called from ProjFS's thread pool.
abstract class ProjfsSource
{
    public abstract List<ProjfsEntry> List(string dir);              // null: not a folder
    public abstract ProjfsEntry Stat(string path);                   // null: doesn't exist
    public abstract int Read(string path, long offset, Span<byte> buffer);
    public virtual bool ReadOnly => false;
}

sealed class ProjfsEntry(string name, bool isDir, long size, DateTime written)
{
    public readonly string Name = name;
    public readonly bool IsDir = isDir;
    public readonly long Size = size;
    public readonly DateTime Written = written;
}

// FUSE-style mounts on Windows' Projected File System (ProjFS, the optional "Client-ProjFS"
// feature): the mount point is a virtualization root, filled on demand from a ProjfsSource. ProjFS
// keeps local changes in the root itself (writes copy a file up, deletes leave tombstones), which
// is overlay semantics. Mounts run in a background copy of the tool (-f stays in the foreground);
// `<tool> -u <mountpoint>` signals it to stop. State: %LOCALAPPDATA%\wintools\projfs\<id>.mount.
static unsafe class Projfs
{
    const int S_OK = 0;
    const int E_FILE_NOT_FOUND = unchecked((int)0x80070002), E_ACCESS_DENIED = unchecked((int)0x80070005),
              E_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A), E_INVALID_DATA = unchecked((int)0x8007000D),
              E_FAIL = unchecked((int)0x80004005);
    const int RestartScan = 1, ReturnSingleEntry = 2;
    const uint PlaceholderInfoSize = 344;
    // PRJ_NOTIFY_PRE_DELETE | PRE_RENAME | PRE_SET_HARDLINK | FILE_PRE_CONVERT_TO_FULL: the vetoable ones.
    const uint ReadOnlyNotifications = 0x10 | 0x20 | 0x40 | 0x1000;

    static ProjfsSource source;
    static IntPtr context;
    static readonly Dictionary<Guid, Enumeration> enumerations = [];

    sealed class Enumeration
    {
        public List<ProjfsEntry> Entries;
        public int Next;
        public string Filter;
    }

    // ---- availability ----

    // null when ProjFS is usable, else the reason in one line.
    public static string Unavailable()
    {
        if (!NativeLibrary.TryLoad("ProjectedFSLib.dll", typeof(Projfs).Assembly, DllImportSearchPath.System32, out var lib))
            return "needs the Windows Projected File System, an optional Windows feature; turn it on with (as admin): " +
                   "Enable-WindowsOptionalFeature -Online -FeatureName Client-ProjFS -NoRestart";
        NativeLibrary.Free(lib);
        return null;
    }

    // ---- mount state ----

    static string StateDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "wintools", "projfs");

    static string Id(string mountpoint) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(mountpoint.ToUpperInvariant())))[..16];

    static string StateFile(string mountpoint) => Path.Combine(StateDir, Id(mountpoint) + ".mount");

    public static Dictionary<string, string> Load(string mountpoint)
    {
        var file = StateFile(mountpoint);
        if (!File.Exists(file)) return null;
        var state = File.ReadAllLines(file).Where(l => l.Contains('=')).ToDictionary(l => l[..l.IndexOf('=')], l => l[(l.IndexOf('=') + 1)..]);
        // A mount whose process is gone (crash, reboot) is stale.
        if (!state.TryGetValue("pid", out var pid) || !Alive(int.Parse(pid)))
        {
            Cleanup(mountpoint, state);
            return null;
        }
        return state;
    }

    static bool Alive(int pid)
    {
        try { using var p = Process.GetProcessById(pid); return !p.HasExited; }
        catch (ArgumentException) { return false; }
    }

    public static string MountPath(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    public static IEnumerable<Dictionary<string, string>> Mounts(string kind)
    {
        if (!Directory.Exists(StateDir)) yield break;
        foreach (var file in Directory.GetFiles(StateDir, "*.mount"))
        {
            var mountpoint = File.ReadAllLines(file).FirstOrDefault(l => l.StartsWith("mountpoint="))?[11..];
            if (mountpoint != null && Load(mountpoint) is { } state && state.GetValueOrDefault("kind") == kind) yield return state;
        }
    }

    // ---- mounting ----

    // Serves `source` at root. With a junction, the mount point is a directory junction to root
    // (fuse-overlayfs's upperdir), made here and removed on unmount.
    public static int Mount(string tool, string mountpoint, string root, bool junction, bool foreground,
                            string[] childArgs, Func<ProjfsSource> makeSource, bool clean = false, int readyTimeout = 60)
    {
        if (Unavailable() is { } why) { Console.Error.WriteLine($"{tool}: {why}"); return 1; }
        if (Load(mountpoint) != null) { Console.Error.WriteLine($"{tool}: {mountpoint} is already mounted (unmount with {tool} -u)"); return 1; }
        // A mount that empties its folder afterwards takes only an empty one (or its own earlier root).
        if (clean && Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any()
            && !File.GetAttributes(root).HasFlag(FileAttributes.ReparsePoint))
        {
            Console.Error.WriteLine($"{tool}: mountpoint is not empty: {root}");
            return 1;
        }
        if (!foreground)
        {
            // Run the provider in a background copy of this command, and wait until it serves.
            using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, @"Local\wintools-projfs-ready-" + Id(mountpoint));
            var exe = Environment.ProcessPath;
            var psi = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true };
            if (Path.GetFileNameWithoutExtension(exe).Equals("wintools", StringComparison.OrdinalIgnoreCase)) psi.ArgumentList.Add(tool);
            foreach (var a in childArgs) psi.ArgumentList.Add(a);
            psi.ArgumentList.Add("-f");
            psi.RedirectStandardError = true;
            using var child = Process.Start(psi);
            var errors = new StringBuilder();
            child.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (errors) errors.AppendLine(e.Data); };
            child.BeginErrorReadLine();
            var clock = Stopwatch.StartNew();
            while (!ready.WaitOne(200))
            {
                if (child.HasExited || clock.Elapsed > TimeSpan.FromSeconds(readyTimeout))
                {
                    if (!child.HasExited) child.Kill();
                    child.WaitForExit(1000);
                    lock (errors) Console.Error.Write(errors.Length > 0 ? errors.ToString() : $"{tool}: mount failed (timed out)\n");
                    return 1;
                }
            }
            return 0;
        }

        if (junction)
        {
            if (Directory.Exists(mountpoint) && Directory.EnumerateFileSystemEntries(mountpoint).Any())
            { Console.Error.WriteLine($"{tool}: mountpoint is not empty: {mountpoint}"); return 1; }
            if (Directory.Exists(mountpoint)) Directory.Delete(mountpoint);
            CreateJunction(mountpoint, root);
        }
        var state = new Dictionary<string, string>
        {
            ["mountpoint"] = mountpoint, ["root"] = root, ["pid"] = Environment.ProcessId.ToString(),
            ["kind"] = tool, ["junction"] = junction ? "1" : "0",
        };
        try
        {
            source = makeSource();
            Start(root);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            if (junction) RemoveJunction(mountpoint);
            Console.Error.WriteLine($"{tool}: {e.Message}");
            return 1;
        }
        Directory.CreateDirectory(StateDir);
        File.WriteAllLines(StateFile(mountpoint), state.Select(kv => $"{kv.Key}={kv.Value}"));

        using var stop = new EventWaitHandle(false, EventResetMode.ManualReset, @"Local\wintools-projfs-stop-" + Id(mountpoint));
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };
        using (var ready = new EventWaitHandle(false, EventResetMode.ManualReset, @"Local\wintools-projfs-ready-" + Id(mountpoint))) ready.Set();
        stop.WaitOne();
        PrjStopVirtualizing(context);
        (source as IDisposable)?.Dispose();
        Cleanup(mountpoint, state);
        // A cache-like mount (avfs, btfs) leaves nothing behind: the projected and hydrated files go.
        if (clean)
            try
            {
                Directory.Delete(root, recursive: true);
                Directory.CreateDirectory(root);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"{tool}: could not empty {root}: {e.Message}");
            }
        return 0;
    }

    public static int Unmount(string tool, string target)
    {
        var mountpoint = MountPath(target);
        var state = Load(mountpoint);
        if (state == null) { Console.Error.WriteLine($"{tool}: {target}: not mounted"); return 1; }
        using (var stop = new EventWaitHandle(false, EventResetMode.ManualReset, @"Local\wintools-projfs-stop-" + Id(mountpoint))) stop.Set();
        try
        {
            using var p = Process.GetProcessById(int.Parse(state["pid"]));
            if (!p.WaitForExit(15000)) { Console.Error.WriteLine($"{tool}: {mountpoint}: the mount process didn't stop"); return 1; }
        }
        catch (ArgumentException) { }
        Cleanup(mountpoint, state);
        return 0;
    }

    static void Cleanup(string mountpoint, Dictionary<string, string> state)
    {
        if (state.GetValueOrDefault("junction") == "1" && Directory.Exists(mountpoint)
            && new DirectoryInfo(mountpoint).Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            RemoveJunction(mountpoint);
            Directory.CreateDirectory(mountpoint);
        }
        if (File.Exists(StateFile(mountpoint))) File.Delete(StateFile(mountpoint));
    }

    static void Start(string root)
    {
        Directory.CreateDirectory(root);
        // A new root gets a virtualization instance ID; one kept from an earlier mount is reused as is.
        var instance = Guid.NewGuid();
        var marked = PrjMarkDirectoryAsPlaceholder(root, null, IntPtr.Zero, &instance);
        try { File.SetAttributes(root, File.GetAttributes(root) | FileAttributes.NotContentIndexed); }
        catch (IOException) { }

        var callbacks = (PRJ_CALLBACKS*)NativeMemory.AllocZeroed((nuint)sizeof(PRJ_CALLBACKS));
        callbacks->StartDirectoryEnumeration = (IntPtr)(delegate* unmanaged<CallbackData*, Guid*, int>)&StartEnumeration;
        callbacks->EndDirectoryEnumeration = (IntPtr)(delegate* unmanaged<CallbackData*, Guid*, int>)&EndEnumeration;
        callbacks->GetDirectoryEnumeration = (IntPtr)(delegate* unmanaged<CallbackData*, Guid*, char*, IntPtr, int>)&GetEnumeration;
        callbacks->GetPlaceholderInfo = (IntPtr)(delegate* unmanaged<CallbackData*, int>)&GetPlaceholderInfo;
        callbacks->GetFileData = (IntPtr)(delegate* unmanaged<CallbackData*, ulong, uint, int>)&GetFileData;
        callbacks->Notification = (IntPtr)(delegate* unmanaged<CallbackData*, byte, uint, char*, IntPtr, int>)&Notification;

        var options = (StartOptions*)NativeMemory.AllocZeroed((nuint)sizeof(StartOptions));
        if (source.ReadOnly)
        {
            var mapping = (NotificationMapping*)NativeMemory.AllocZeroed((nuint)sizeof(NotificationMapping));
            mapping->Mask = ReadOnlyNotifications;
            mapping->Root = Marshal.StringToHGlobalUni("");
            options->NotificationMappings = mapping;
            options->NotificationMappingsCount = 1;
        }
        IntPtr ctx;
        var hr = PrjStartVirtualizing(root, callbacks, IntPtr.Zero, options, &ctx);
        if (hr != S_OK)
        {
            var reason = hr == unchecked((int)0x80070032) || hr == unchecked((int)0x8007045A)
                ? "the Projected File System driver isn't running (turn on the Client-ProjFS Windows feature)"
                : Marshal.GetExceptionForHR(hr)?.Message ?? $"0x{hr:X8}";
            if (marked != S_OK) reason += $" (marking the root failed: 0x{marked:X8})";
            throw new IOException($"cannot start virtualizing {root}: {reason}");
        }
        context = ctx;
    }

    // ---- callbacks ----

    static string PathOf(CallbackData* data) => data->FilePathName == IntPtr.Zero ? "" : Marshal.PtrToStringUni(data->FilePathName) ?? "";

    static int Compare(string a, string b) => PrjFileNameCompare(a, b);

    [UnmanagedCallersOnly]
    static int StartEnumeration(CallbackData* data, Guid* id)
    {
        try
        {
            var entries = source.List(PathOf(data));
            if (entries == null) return E_FILE_NOT_FOUND;
            entries.Sort((a, b) => Compare(a.Name, b.Name));
            lock (enumerations) enumerations[*id] = new Enumeration { Entries = entries };
            return S_OK;
        }
        catch (Exception) { return E_FAIL; }
    }

    [UnmanagedCallersOnly]
    static int EndEnumeration(CallbackData* data, Guid* id)
    {
        lock (enumerations) enumerations.Remove(*id);
        return S_OK;
    }

    [UnmanagedCallersOnly]
    static int GetEnumeration(CallbackData* data, Guid* id, char* searchExpression, IntPtr buffer)
    {
        try
        {
            Enumeration e;
            lock (enumerations) if (!enumerations.TryGetValue(*id, out e)) return E_INVALID_DATA;
            if ((data->Flags & RestartScan) != 0 || e.Filter == null)
            {
                if ((data->Flags & RestartScan) != 0) e.Next = 0;
                e.Filter = searchExpression == null ? "*" : new string(searchExpression);
                if (e.Filter.Length == 0) e.Filter = "*";
            }
            var added = 0;
            for (; e.Next < e.Entries.Count; e.Next++)
            {
                var entry = e.Entries[e.Next];
                if (e.Filter != "*" && !PrjFileNameMatch(entry.Name, e.Filter)) continue;
                var info = Basic(entry);
                var hr = PrjFillDirEntryBuffer(entry.Name, &info, buffer);
                if (hr == E_INSUFFICIENT_BUFFER) return added == 0 ? hr : S_OK;
                if (hr != S_OK) return hr;
                added++;
                if ((data->Flags & ReturnSingleEntry) != 0) { e.Next++; break; }
            }
            return S_OK;
        }
        catch (Exception) { return E_FAIL; }
    }

    [UnmanagedCallersOnly]
    static int GetPlaceholderInfo(CallbackData* data)
    {
        try
        {
            var path = PathOf(data);
            var entry = source.Stat(path);
            if (entry == null) return E_FILE_NOT_FOUND;
            var slash = path.LastIndexOf('\\');
            var name = (slash < 0 ? "" : path[..(slash + 1)]) + entry.Name;
            var info = (byte*)NativeMemory.AllocZeroed(PlaceholderInfoSize);
            try
            {
                *(BasicInfo*)info = Basic(entry);
                return PrjWritePlaceholderInfo(data->Context, name, info, PlaceholderInfoSize);
            }
            finally { NativeMemory.Free(info); }
        }
        catch (Exception) { return E_FAIL; }
    }

    [UnmanagedCallersOnly]
    static int GetFileData(CallbackData* data, ulong offset, uint length)
    {
        try
        {
            var path = PathOf(data);
            const uint Chunk = 1 << 20;
            var done = 0u;
            while (done < length)
            {
                var size = Math.Min(Chunk, length - done);
                var buffer = PrjAllocateAlignedBuffer(data->Context, size);
                if (buffer == IntPtr.Zero) return unchecked((int)0x8007000E);
                try
                {
                    var read = source.Read(path, (long)(offset + done), new Span<byte>((void*)buffer, (int)size));
                    if (read <= 0) return done == 0 ? E_FAIL : S_OK;
                    var hr = PrjWriteFileData(data->Context, &data->DataStreamId, buffer, offset + done, (uint)read);
                    if (hr != S_OK) return hr;
                    done += (uint)read;
                }
                finally { PrjFreeAlignedBuffer(buffer); }
            }
            return S_OK;
        }
        catch (Exception) { return E_FAIL; }
    }

    [UnmanagedCallersOnly]
    static int Notification(CallbackData* data, byte isDirectory, uint notification, char* destination, IntPtr parameters) =>
        source.ReadOnly && (notification & ReadOnlyNotifications) != 0 ? E_ACCESS_DENIED : S_OK;

    static BasicInfo Basic(ProjfsEntry entry)
    {
        var time = entry.Written.ToFileTimeUtc();
        return new BasicInfo
        {
            IsDirectory = (byte)(entry.IsDir ? 1 : 0),
            FileSize = entry.IsDir ? 0 : entry.Size,
            CreationTime = time, LastAccessTime = time, LastWriteTime = time, ChangeTime = time,
            FileAttributes = entry.IsDir ? 0x10u : 0x80u | (source.ReadOnly ? 1u : 0u),
        };
    }

    // ---- junctions (what `mklink /J` makes; no privileges needed) ----

    static void CreateJunction(string link, string target)
    {
        Directory.CreateDirectory(link);
        var substitute = Encoding.Unicode.GetBytes(@"\??\" + Path.TrimEndingDirectorySeparator(target) + "\\");
        var print = Encoding.Unicode.GetBytes(target);
        var buffer = new byte[16 + substitute.Length + 2 + print.Length + 2];
        BitConverter.TryWriteBytes(buffer.AsSpan(0), 0xA0000003u);
        BitConverter.TryWriteBytes(buffer.AsSpan(4), (ushort)(buffer.Length - 8));
        BitConverter.TryWriteBytes(buffer.AsSpan(10), (ushort)substitute.Length);
        BitConverter.TryWriteBytes(buffer.AsSpan(12), (ushort)(substitute.Length + 2));
        BitConverter.TryWriteBytes(buffer.AsSpan(14), (ushort)print.Length);
        substitute.CopyTo(buffer, 16);
        print.CopyTo(buffer, 16 + substitute.Length + 2);
        var h = CreateFileW(link, 0x40000000, 0, IntPtr.Zero, 3, 0x02000000 | 0x00200000, IntPtr.Zero);
        if (h == new IntPtr(-1)) { Directory.Delete(link); throw new IOException($"cannot open {link} (error {Marshal.GetLastWin32Error()})"); }
        try
        {
            fixed (byte* p = buffer)
                if (!DeviceIoControl(h, 0x000900A4, p, (uint)buffer.Length, null, 0, out _, IntPtr.Zero))
                {
                    var error = Marshal.GetLastWin32Error();
                    CloseHandle(h);
                    h = IntPtr.Zero;
                    Directory.Delete(link);
                    throw new IOException($"cannot create junction {link} (error {error})");
                }
        }
        finally { if (h != IntPtr.Zero) CloseHandle(h); }
    }

    static void RemoveJunction(string link)
    {
        var info = new DirectoryInfo(link);
        if (info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint)) info.Delete();   // the link only
    }

    // ---- interop ----

    [StructLayout(LayoutKind.Sequential)]
    struct PRJ_CALLBACKS
    {
        public IntPtr StartDirectoryEnumeration, EndDirectoryEnumeration, GetDirectoryEnumeration, GetPlaceholderInfo,
                      GetFileData, QueryFileName, Notification, CancelCommand;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CallbackData
    {
        public uint Size;
        public int Flags;
        public IntPtr Context;
        public int CommandId;
        public Guid FileId;
        public Guid DataStreamId;
        public IntPtr FilePathName;
        public IntPtr VersionInfo;
        public uint TriggeringProcessId;
        public IntPtr TriggeringProcessImageFileName;
        public IntPtr InstanceContext;
    }

    [StructLayout(LayoutKind.Explicit, Size = 56)]
    struct BasicInfo
    {
        [FieldOffset(0)] public byte IsDirectory;
        [FieldOffset(8)] public long FileSize;
        [FieldOffset(16)] public long CreationTime;
        [FieldOffset(24)] public long LastAccessTime;
        [FieldOffset(32)] public long LastWriteTime;
        [FieldOffset(40)] public long ChangeTime;
        [FieldOffset(48)] public uint FileAttributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NotificationMapping
    {
        public uint Mask;
        public IntPtr Root;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct StartOptions
    {
        public int Flags;
        public uint PoolThreadCount;
        public uint ConcurrentThreadCount;
        public NotificationMapping* NotificationMappings;
        public uint NotificationMappingsCount;
    }

    [DllImport("ProjectedFSLib.dll", CharSet = CharSet.Unicode)]
    static extern int PrjMarkDirectoryAsPlaceholder(string rootPathName, string targetPathName, IntPtr versionInfo, Guid* instanceId);

    [DllImport("ProjectedFSLib.dll", CharSet = CharSet.Unicode)]
    static extern int PrjStartVirtualizing(string root, PRJ_CALLBACKS* callbacks, IntPtr instanceContext, StartOptions* options, IntPtr* context);

    [DllImport("ProjectedFSLib.dll")]
    static extern void PrjStopVirtualizing(IntPtr context);

    [DllImport("ProjectedFSLib.dll", CharSet = CharSet.Unicode)]
    static extern int PrjWritePlaceholderInfo(IntPtr context, string destinationFileName, byte* placeholderInfo, uint size);

    [DllImport("ProjectedFSLib.dll", CharSet = CharSet.Unicode)]
    static extern int PrjFillDirEntryBuffer(string fileName, BasicInfo* info, IntPtr buffer);

    [DllImport("ProjectedFSLib.dll")]
    static extern IntPtr PrjAllocateAlignedBuffer(IntPtr context, nuint size);

    [DllImport("ProjectedFSLib.dll")]
    static extern void PrjFreeAlignedBuffer(IntPtr buffer);

    [DllImport("ProjectedFSLib.dll")]
    static extern int PrjWriteFileData(IntPtr context, Guid* dataStreamId, IntPtr buffer, ulong offset, uint length);

    [DllImport("ProjectedFSLib.dll", CharSet = CharSet.Unicode)]
    static extern int PrjFileNameCompare(string a, string b);

    [DllImport("ProjectedFSLib.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.U1)]
    static extern bool PrjFileNameMatch(string name, string pattern);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr device, uint code, byte* input, uint inputSize, byte* output, uint outputSize, out uint returned, IntPtr overlapped);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
