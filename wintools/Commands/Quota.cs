using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WinTools;

// quotatool: NTFS disk quotas (NtQuery/NtSetQuotaInformationFile on the volume). NTFS quotas are
// per user and count bytes; quotatool's soft limit is NTFS's warning level and its hard limit the
// quota limit. Inode limits, group quotas and grace periods have no NTFS counterpart.
static class Quota
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("quotatool", "Set or show NTFS disk quotas for a user", Run);
    }

    const string Usage = """
        Usage: quotatool -u uid | -g gid options [...] filesystem
               quotatool -u | -g -i | -b  -t time filesystem
        Options:
          -u uid  : set quota for user (name, DOMAIN\name or SID)
          -g gid  : set quota for group (not available: NTFS quotas are per user)
          -b      : set block limits (the default; sizes take K, M, G, T suffixes, bare numbers are 1K blocks)
          -i      : set inode limits (not available on NTFS)
          -q n    : set soft limit to n (NTFS warning level); +n / -n raise or lower it
          -l n    : set hard limit to n (NTFS quota limit); 0 means no limit
          -t time : set global grace period (not available on NTFS)
          -r      : restore default grace period (not available on NTFS)
          -R      : raise-only, never lower quotas
          -d      : dump quota info
          -n      : don't perform any actions
          -v      : verbose mode
          -V      : version info
          -h      : this message
        filesystem is a drive (C:) or any path on the volume. Changing quotas needs an elevated prompt.
        """;

    static int Run(string[] args)
    {
        string user = null, soft = null, hard = null;
        bool raiseOnly = false, dump = false, dryRun = false, verbose = false, userGiven = false;
        string filesystem = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"option {arg} requires an argument");
            switch (arg)
            {
                case "-h": Console.WriteLine(Usage); return 0;
                case "-V": Console.WriteLine($"quotatool version {Commands.Version} (wintools)"); return 0;
                case "-u":
                    userGiven = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-') && i + 2 < args.Length) user = args[++i];
                    break;
                case "-g": Console.Error.WriteLine("quotatool: NTFS quotas are per user; there are no group quotas on Windows."); return 1;
                case "-i": Console.Error.WriteLine("quotatool: NTFS quotas limit bytes only; there are no inode limits on Windows."); return 1;
                case "-t" or "-r": Console.Error.WriteLine("quotatool: NTFS quotas have no grace period."); return 1;
                case "-b": break;
                case "-q": soft = Value(); break;
                case "-l": hard = Value(); break;
                case "-R": raiseOnly = true; break;
                case "-d": dump = true; break;
                case "-n": dryRun = true; break;
                case "-v": verbose = true; break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"invalid option {arg}\n{Usage}");
                    if (filesystem != null) throw new UsageException(Usage);
                    filesystem = arg;
                    break;
            }
        }
        if (!userGiven || user == null || filesystem == null) throw new UsageException(Usage);
        if (soft == null && hard == null && !dump) throw new UsageException("nothing to do: give -q, -l or -d");
        var volume = Volume(filesystem);
        SecurityIdentifier sid;
        try
        {
            sid = user.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase) ? new SecurityIdentifier(user)
                : (SecurityIdentifier)new NTAccount(user).Translate(typeof(SecurityIdentifier));
        }
        catch (IdentityNotMappedException) { throw new InvalidOperationException($"no such user: {user}"); }
        if (verbose) Console.Error.WriteLine($"quotatool: using filesystem {volume}\nquotatool: user '{user}' is {sid.Value}");
        if (dryRun && !dump && !raiseOnly && soft is null or not ['+' or '-', ..] && hard is null or not ['+' or '-', ..])
        {
            // Absolute values need no current state: report and stop.
            if (verbose) Console.Error.WriteLine($"quotatool: would set soft={Blocks(Size(soft, 0))} hard={Blocks(Size(hard, 0))} (1K blocks)");
            return 0;
        }

        using var handle = OpenVolume(volume, write: !dryRun && !dump);
        var current = Query(handle, sid);
        if (dump)
        {
            // uid filesystem blocks-used soft hard grace inodes-used soft hard grace
            Console.WriteLine($"{user} {volume} {current.Used / 1024} {Blocks(current.Threshold)} {Blocks(current.Limit)} 0 0 0 0 0");
            if (soft == null && hard == null) return 0;
        }
        var newSoft = soft == null ? current.Threshold : Size(soft, current.Threshold);
        var newHard = hard == null ? current.Limit : Size(hard, current.Limit);
        if (raiseOnly)
        {
            newSoft = Raise(current.Threshold, newSoft);
            newHard = Raise(current.Limit, newHard);
        }
        if (verbose)
        {
            Console.Error.WriteLine($"quotatool: current: used={current.Used / 1024} soft={Blocks(current.Threshold)} hard={Blocks(current.Limit)} (1K blocks)");
            Console.Error.WriteLine($"quotatool: new:     soft={Blocks(newSoft)} hard={Blocks(newHard)} (1K blocks)");
        }
        if (dryRun) return 0;
        Set(handle, sid, newSoft, newHard);
        return 0;
    }

    // -1 is NTFS's "no limit", 0 in quotatool's terms.
    static string Blocks(long bytes) => bytes < 0 ? "0" : (bytes / 1024).ToString();

    static long Raise(long current, long wanted) =>
        current < 0 ? current : wanted < 0 ? wanted : Math.Max(current, wanted);

    // "10M", "1.5G", "500" (1K blocks), with + / - relative to the current value.
    internal static long Size(string text, long current)
    {
        if (text == null) return current;
        var sign = text[0] is '+' or '-' ? text[0] : '\0';
        var body = sign != '\0' ? text[1..] : text;
        var digits = body.Length - body.TrimEnd("bBkKmMgGtT".ToCharArray()).Length;
        var suffix = body[^digits..].ToUpperInvariant();
        var unit = suffix switch
        {
            "" => 1024L,                        // bare numbers are 1K blocks
            "B" => 1L,
            "K" or "KB" => 1L << 10,
            "M" or "MB" => 1L << 20,
            "G" or "GB" => 1L << 30,
            "T" or "TB" => 1L << 40,
            _ => throw new UsageException($"bad size '{text}'"),
        };
        if (!double.TryParse(body[..^digits], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number))
            throw new UsageException($"bad size '{text}'");
        var bytes = (long)(number * unit);
        if (sign == '\0') return bytes == 0 ? -1 : bytes;
        var baseline = current < 0 ? 0 : current;
        var result = sign == '+' ? baseline + bytes : Math.Max(0, baseline - bytes);
        return result == 0 ? -1 : result;
    }

    static string Volume(string path)
    {
        var full = Path.GetFullPath(path.Length == 2 && path[1] == ':' ? path + "\\" : path);
        var buffer = new char[260];
        if (!GetVolumePathNameW(full, buffer, buffer.Length)) throw new IOException($"{path}: not a mounted file system");
        return new string(buffer).TrimEnd('\0');
    }

    static SafeHandleZero OpenVolume(string volumePath, bool write)
    {
        var device = @"\\.\" + volumePath.TrimEnd('\\');
        var handle = CreateFileW(device, GENERIC_READ | (write ? GENERIC_WRITE : 0), FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (handle == new IntPtr(-1))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == 5) throw new UnauthorizedAccessException("quotas need an elevated (administrator) prompt");
            throw new IOException($"cannot open {volumePath} (error {error})");
        }
        return new SafeHandleZero(handle);
    }

    sealed class SafeHandleZero(IntPtr handle) : IDisposable
    {
        public readonly IntPtr Handle = handle;
        public void Dispose() => CloseHandle(Handle);
    }

    static unsafe (long Used, long Threshold, long Limit) Query(SafeHandleZero volume, SecurityIdentifier sid)
    {
        var sidBytes = new byte[sid.BinaryLength];
        sid.GetBinaryForm(sidBytes, 0);
        var request = new byte[8 + sidBytes.Length];
        BitConverter.TryWriteBytes(request.AsSpan(4), sidBytes.Length);
        sidBytes.CopyTo(request, 8);
        var buffer = new byte[1024];
        fixed (byte* r = request) fixed (byte* b = buffer)
        {
            var status = NtQueryQuotaInformationFile(volume.Handle, out _, b, (uint)buffer.Length, 1, r, (uint)request.Length, null, 1);
            if (status == STATUS_NO_MORE_ENTRIES) return (0, -1, -1);
            Check(status);
        }
        return (BitConverter.ToInt64(buffer, 16), BitConverter.ToInt64(buffer, 24), BitConverter.ToInt64(buffer, 32));
    }

    static unsafe void Set(SafeHandleZero volume, SecurityIdentifier sid, long threshold, long limit)
    {
        var sidBytes = new byte[sid.BinaryLength];
        sid.GetBinaryForm(sidBytes, 0);
        var info = new byte[40 + sidBytes.Length];
        BitConverter.TryWriteBytes(info.AsSpan(4), sidBytes.Length);
        BitConverter.TryWriteBytes(info.AsSpan(24), threshold);
        BitConverter.TryWriteBytes(info.AsSpan(32), limit);
        sidBytes.CopyTo(info, 40);
        fixed (byte* i = info) Check(NtSetQuotaInformationFile(volume.Handle, out _, i, (uint)info.Length));
    }

    static void Check(int status)
    {
        if (status >= 0) return;
        throw (uint)status switch
        {
            0xC0000022 => new UnauthorizedAccessException("quotas need an elevated (administrator) prompt"),
            0xC0000010 or 0xC00000BB => new InvalidOperationException("quotas are disabled on this volume (an administrator can turn on tracking with: fsutil quota track C:)"),
            _ => new IOException($"quota call failed (NTSTATUS 0x{status:X8})"),
        };
    }

    const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2, OPEN_EXISTING = 3;
    const int STATUS_NO_MORE_ENTRIES = unchecked((int)0x8000001A);

    [StructLayout(LayoutKind.Sequential)]
    struct IO_STATUS_BLOCK { public IntPtr Status, Information; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetVolumePathNameW(string path, char[] volume, int length);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("ntdll.dll")]
    static extern unsafe int NtQueryQuotaInformationFile(IntPtr file, out IO_STATUS_BLOCK io, void* buffer, uint length, byte single, void* sidList, uint sidListLength, void* startSid, byte restart);

    [DllImport("ntdll.dll")]
    static extern unsafe int NtSetQuotaInformationFile(IntPtr file, out IO_STATUS_BLOCK io, void* buffer, uint length);
}
