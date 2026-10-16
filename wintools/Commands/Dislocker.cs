using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// dislocker / dislocker-fuse / dislocker-file / dislocker-metadata / dislocker-find / dislocker-bek:
// Windows reads BitLocker natively, so unlocking goes through manage-bde / Unlock-BitLocker and
// the unlocked volume is the "mount" (a junction to it is made if a mount point is given). The FVE
// metadata of images and partitions, and .BEK key files, are parsed here directly.
static class Dislocker
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("dislocker", "Unlock a BitLocker volume and make it available at a folder", Fuse);
        Commands.Add("dislocker-fuse", "Unlock a BitLocker volume and make it available at a folder", Fuse);
        Commands.Add("dislocker-file", "Unlock a BitLocker volume and copy its decrypted NTFS image to a file", ToFile);
        Commands.Add("dislocker-metadata", "Show the BitLocker metadata of a volume, partition or image", Metadata);
        Commands.Add("dislocker-find", "List BitLocker-encrypted volumes (or check image files)", Find);
        Commands.Add("dislocker-bek", "Show the contents of a BitLocker .BEK startup key file", Bek);
    }

    const string Usage = """
        Usage: {0} [-hqrsv] [-l LOG_FILE] [-O OFFSET] [-V VOLUME DECRYPTMETHOD -F[N]] [-- ARGS...]
            with DECRYPTMETHOD = -p[RECOVERY_PASSWORD]|-f BEK_FILE|-u[USER_PASSWORD]|-k FVEK_FILE|-K VMK_FILE|-c

        Options:
            -c, --clearkey        decrypt volume using a clear key (protection suspended)
            -f, --bekfile BEKFILE decrypt volume using the bek file (on USB key)
            -F, --force-block=[N] accepted for compatibility
            -h, --help            print this help and exit
            -k, --fvek FVEK_FILE  decrypt volume using the FVEK directly (not possible with Windows' BitLocker)
            -K, --vmk VMK_FILE    decrypt volume using the VMK directly (not possible with Windows' BitLocker)
            -l, --logfile LOG_FILE  accepted for compatibility
            -O, --offset OFFSET   BitLocker partition offset, in bytes (metadata of images)
            -p, --recovery-password=[RECOVERY_PASSWORD]  decrypt volume using the recovery password method
            -q, --quiet           do NOT display anything
            -r, --readonly        accepted (Windows unlocks volumes read-write)
            -s, --stateok         accepted for compatibility
            -u, --user-password=[USER_PASSWORD]  decrypt volume using the user password method
            -v, --verbosity       increase verbosity
            -V, --volume VOLUME   volume to get metadata and keys from (a drive like E: or a volume GUID path)

            --                    end of program options; the mount point follows
        Unlocking needs an elevated (administrator) prompt.
        """;

    sealed class Options
    {
        public string Volume, Method, Secret, BekFile, LogFile;
        public long Offset;
        public bool Quiet, ReadOnly;
        public List<string> Rest = [];
    }

    static Options Parse(string tool, string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"option {a} requires an argument");
            string Attached(string shortName, string longName) =>
                a.StartsWith(longName + "=") ? a[(longName.Length + 1)..] : a.StartsWith(shortName) && a.Length > 2 && !a.StartsWith("--") ? a[2..] : null;
            if (a == "--") { o.Rest.AddRange(args[(i + 1)..]); break; }
            if (a is "-h" or "--help") throw new HelpRequested();
            if (a is "-V" or "--volume") o.Volume = Value();
            else if (a.StartsWith("-p") && !a.StartsWith("--") || a.StartsWith("--recovery-password")) { o.Method = "recovery"; o.Secret = Attached("-p", "--recovery-password"); }
            else if (a.StartsWith("-u") && !a.StartsWith("--") || a.StartsWith("--user-password")) { o.Method = "password"; o.Secret = Attached("-u", "--user-password"); }
            else if (a is "-f" or "--bekfile") { o.Method = "bek"; o.BekFile = Value(); }
            else if (a is "-k" or "--fvek") { o.Method = "fvek"; Value(); }
            else if (a is "-K" or "--vmk") { o.Method = "vmk"; Value(); }
            else if (a is "-c" or "--clearkey") o.Method = "clear";
            else if (a is "-O" or "--offset") o.Offset = long.TryParse(Value(), out var off) ? off : throw new UsageException("the offset must be a number of bytes");
            else if (a is "-l" or "--logfile") o.LogFile = Value();
            else if (a is "-q" or "--quiet") o.Quiet = true;
            else if (a is "-r" or "--readonly") o.ReadOnly = true;
            else if (a.StartsWith("-F") || a.StartsWith("--force-block") || a is "-s" or "--stateok" || a.TrimStart('-').All(c => c == 'v') && a.StartsWith('-')) { }
            else if (a.StartsWith('-')) throw new UsageException($"unknown option {a}\n{string.Format(Usage, tool)}");
            else o.Rest.Add(a);
        }
        return o;
    }

    sealed class HelpRequested : Exception;

    static int WithHelp(string tool, string[] args, Func<Options, int> run)
    {
        Options o;
        try { o = Parse(tool, args); }
        catch (HelpRequested) { Console.WriteLine(string.Format(Usage, tool)); return 0; }
        return run(o);
    }

    static bool IsDrive(string volume) =>
        volume.Length is 2 or 3 && volume[1] == ':' && char.IsLetter(volume[0]) || volume.StartsWith(@"\\?\Volume{", StringComparison.OrdinalIgnoreCase);

    static string DriveRoot(string volume) => volume.Length <= 3 ? volume[..2].ToUpperInvariant() : volume.TrimEnd('\\') + "\\";

    // ---- dislocker / dislocker-fuse ----

    static int Fuse(string[] args) => WithHelp("dislocker", args, o =>
    {
        if (o.Volume == null) throw new UsageException("no volume given (-V)");
        var status = Unlock(o);
        if (status != 0) return status;
        var root = DriveRoot(o.Volume) + (o.Volume.Length <= 3 ? "\\" : "");
        if (o.ReadOnly && !o.Quiet) Console.Error.WriteLine("dislocker: note: Windows unlocks BitLocker volumes read-write");
        if (o.Rest.Count > 0)
        {
            // A junction at the mount point shows the decrypted file system itself (no loop mount needed).
            var mountpoint = Path.GetFullPath(o.Rest[^1]);
            if (Directory.Exists(mountpoint))
            {
                if (Directory.EnumerateFileSystemEntries(mountpoint).Any()) throw new IOException($"{mountpoint} is not empty");
                Directory.Delete(mountpoint);
            }
            var psi = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, RedirectStandardOutput = true };
            foreach (var a in new[] { "/d", "/c", "mklink", "/J", mountpoint, root }) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) throw new IOException($"could not link {mountpoint} to {root}");
            if (!o.Quiet) Console.WriteLine($"{o.Volume} is unlocked; its files are at {mountpoint} (and {root})");
        }
        else if (!o.Quiet) Console.WriteLine($"{o.Volume} is unlocked; its files are at {root}");
        return 0;
    });

    static int Unlock(Options o)
    {
        if (!IsDrive(o.Volume))
        {
            Console.Error.WriteLine("dislocker: Windows unlocks BitLocker on attached volumes only; attach the image first (Mount-DiskImage for VHDs, or write it to a drive)");
            return 1;
        }
        var drive = DriveRoot(o.Volume);
        switch (o.Method)
        {
            case "fvek" or "vmk":
                Console.Error.WriteLine("dislocker: Windows' BitLocker can't take a raw FVEK/VMK; use the recovery password, user password or .BEK file");
                return 1;
            case "clear" or null:
                // Clear-key volumes (protection suspended) are readable as they are.
                return 0;
            case "recovery":
                var recovery = o.Secret ?? Prompt("Enter the recovery password: ", hide: false);
                return Run("manage-bde.exe", ["-unlock", drive, "-RecoveryPassword", recovery], null, o.Quiet);
            case "bek":
                return Run("manage-bde.exe", ["-unlock", drive, "-RecoveryKey", Path.GetFullPath(o.BekFile)], null, o.Quiet);
            default:
                var password = o.Secret ?? Prompt("Enter the user password: ", hide: true);
                // The password goes through stdin, not the command line.
                return Run("powershell.exe", ["-NoProfile", "-NonInteractive", "-Command",
                    $"$p = [Console]::In.ReadLine() | ConvertTo-SecureString -AsPlainText -Force; Unlock-BitLocker -MountPoint '{drive.Replace("'", "''")}' -Password $p | Out-Null"],
                    password, o.Quiet);
        }
    }

    static string Prompt(string text, bool hide)
    {
        Console.Error.Write(text);
        if (!hide || Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (sb.Length > 0) sb.Length--; }
            else sb.Append(key.KeyChar);
        }
        Console.Error.WriteLine();
        return sb.ToString();
    }

    static int Run(string exe, string[] args, string input, bool quiet)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardInput = input != null, RedirectStandardOutput = quiet };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        if (input != null) { p.StandardInput.WriteLine(input); p.StandardInput.Close(); }
        if (quiet) p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode == 0 ? 0 : 1;
    }

    // ---- dislocker-file ----

    static int ToFile(string[] args) => WithHelp("dislocker-file", args, o =>
    {
        if (o.Volume == null || o.Rest.Count != 1) throw new UsageException("Usage: dislocker-file [-V VOLUME DECRYPTMETHOD] NTFS_FILE");
        var status = Unlock(o);
        if (status != 0) return status;
        // Reading an unlocked BitLocker volume's device returns the decrypted sectors.
        var device = o.Volume.Length <= 3 ? @"\\.\" + DriveRoot(o.Volume) : DriveRoot(o.Volume).TrimEnd('\\');
        using var handle = File.OpenHandle(device, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var length = VolumeLength(handle);
        using var input = new FileStream(handle, FileAccess.Read, 0);
        using var output = File.Create(o.Rest[0]);
        var buffer = new byte[4 << 20];
        long done = 0;
        while (done < length)
        {
            var n = input.Read(buffer, 0, (int)Math.Min(buffer.Length, length - done));
            if (n <= 0) break;
            output.Write(buffer, 0, n);
            done += n;
            if (!o.Quiet) Console.Error.Write($"\r{done * 100 / length}% ({done >> 20} of {length >> 20} MiB)");
        }
        if (!o.Quiet) Console.Error.WriteLine();
        return 0;
    });

    static unsafe long VolumeLength(Microsoft.Win32.SafeHandles.SafeFileHandle handle)
    {
        long length;
        if (!DeviceIoControl(handle, IOCTL_DISK_GET_LENGTH_INFO, null, 0, &length, 8, out _, IntPtr.Zero))
            throw new IOException($"cannot get the volume size (error {Marshal.GetLastWin32Error()})");
        return length;
    }

    // ---- dislocker-find ----

    static int Find(string[] args)
    {
        if (args.Any(a => a is "-h" or "--help"))
        {
            Console.WriteLine("Usage: dislocker-find [files...]\nLists BitLocker volumes; given image or partition files, prints those that are BitLocker-encrypted.");
            return 0;
        }
        if (args.Length > 0)
        {
            var found = 0;
            foreach (var file in args)
            {
                try
                {
                    using var s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (Fve.FindMetadata(s, 0) != null) { Console.WriteLine(file); found++; }
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Console.Error.WriteLine($"dislocker-find: {file}: {e.Message}"); }
            }
            return found > 0 ? 0 : 1;
        }
        // Explorer's BitLocker property works without elevation; values other than 0 (none) and 2 (off) mean BitLocker.
        const string script = """
            $shell = New-Object -ComObject Shell.Application
            Get-CimInstance Win32_LogicalDisk | ForEach-Object {
              $p = $shell.NameSpace(17).ParseName($_.DeviceID + '\')
              if ($p) { $v = $p.ExtendedProperty('System.Volume.BitLockerProtection'); if ($v -and $v -ne 2) { $_.DeviceID } }
            }
            """;
        var psi = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-Command", script }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        var volumes = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var v in volumes) Console.WriteLine(v);
        return volumes.Length > 0 ? 0 : 1;
    }

    // ---- dislocker-metadata ----

    static int Metadata(string[] args) => WithHelp("dislocker-metadata", args, o =>
    {
        var volume = o.Volume ?? o.Rest.FirstOrDefault() ?? throw new UsageException("Usage: dislocker-metadata [-O OFFSET] -V VOLUME");
        if (IsDrive(volume) && !File.Exists(volume))
        {
            var drive = DriveRoot(volume);
            var status = Run("manage-bde.exe", ["-status", drive], null, false);
            return status != 0 ? status : Run("manage-bde.exe", ["-protectors", "-get", drive], null, false);
        }
        using var s = new FileStream(volume, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var info = Fve.FindMetadata(s, o.Offset);
        if (info == null) { Console.Error.WriteLine($"dislocker-metadata: {volume}: no BitLocker signature found"); return 1; }
        Console.Write(Fve.Describe(s, o.Offset, info.Value));
        return 0;
    });

    // ---- dislocker-bek ----

    static int Bek(string[] args)
    {
        string file = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "-h" or "--help") { Console.WriteLine("Usage: dislocker-bek -f BEK_FILE\nShows the dataset, external key identifier and key of a BitLocker startup key (.BEK) file."); return 0; }
            if (args[i] is "-f" or "--bekfile" && i + 1 < args.Length) file = args[++i];
            else if (!args[i].StartsWith('-')) file = args[i];
        }
        if (file == null) throw new UsageException("Usage: dislocker-bek -f BEK_FILE");
        var data = File.ReadAllBytes(file);
        Console.Write(Fve.DescribeDataset(data, 0, "BEK file"));
        return 0;
    }

    // BitLocker on-disk structures: the volume header points at three copies of the FVE metadata
    // block; each holds a dataset of typed "datums" (VMKs, the FVEK, descriptions, ...).
    internal static class Fve
    {
        static readonly Guid InformationGuid = new("4967d63b-2e29-4ad8-8399-f6a339e3d001");

        public static (long[] Offsets, string Kind)? FindMetadata(Stream s, long offset)
        {
            var sector = new byte[512];
            s.Seek(offset, SeekOrigin.Begin);
            if (s.Read(sector, 0, 512) < 512) return null;
            if (sector.AsSpan(3, 8).SequenceEqual("-FVE-FS-"u8))
            {
                if (new Guid(sector.AsSpan(0xA0, 16)) == InformationGuid)
                    return ([BitConverter.ToInt64(sector, 0xB0), BitConverter.ToInt64(sector, 0xB8), BitConverter.ToInt64(sector, 0xC0)], "Windows 7 or later");
                // Vista: the metadata offset is stored as a cluster number.
                var cluster = BitConverter.ToUInt16(sector, 0x0B) * sector[0x0D];
                return ([BitConverter.ToInt64(sector, 0x38) * cluster], "Windows Vista");
            }
            if (sector.Length >= 0x1D0 && new Guid(sector.AsSpan(0x1A8, 16)) == InformationGuid)
                return ([BitConverter.ToInt64(sector, 0x1B8), BitConverter.ToInt64(sector, 0x1C0), BitConverter.ToInt64(sector, 0x1C8)], "BitLocker To Go");
            return null;
        }

        public static string Describe(Stream s, long offset, (long[] Offsets, string Kind) info)
        {
            var sb = new StringBuilder();
            sb.Append($"Volume header ({info.Kind}):\n");
            sb.Append($"  Metadata offsets: {string.Join(", ", info.Offsets.Select(o => $"0x{o:x16}"))}\n");
            var block = new byte[65536];
            s.Seek(offset + info.Offsets[0], SeekOrigin.Begin);
            var read = s.Read(block, 0, block.Length);
            if (read < 112 || !block.AsSpan(0, 8).SequenceEqual("-FVE-FS-"u8)) return sb.Append("  (metadata block not found at the first offset)\n").ToString();
            sb.Append("Metadata block:\n");
            sb.Append($"  Version: {BitConverter.ToUInt16(block, 0x0A)}\n");
            sb.Append($"  State: {State(BitConverter.ToUInt16(block, 0x0C))} (next: {State(BitConverter.ToUInt16(block, 0x0E))})\n");
            sb.Append($"  Encrypted volume size: {BitConverter.ToUInt64(block, 0x10)} bytes\n");
            sb.Append($"  Backup sectors: {BitConverter.ToUInt32(block, 0x1C)} at 0x{BitConverter.ToUInt64(block, 0x38):x16}\n");
            return sb.Append(DescribeDataset(block[..read], 0x40, "Dataset")).ToString();
        }

        static string State(ushort s) => s switch { 1 => "decrypted", 2 => "switching", 3 => "EOW activated", 4 => "encrypted", 5 => "switch encryption paused", _ => $"0x{s:x}" };

        public static string Algorithm(ushort a) => a switch
        {
            0x8000 => "AES-128-CBC + Elephant diffuser",
            0x8001 => "AES-256-CBC + Elephant diffuser",
            0x8002 => "AES-128-CBC",
            0x8003 => "AES-256-CBC",
            0x8004 => "AES-128-XTS",
            0x8005 => "AES-256-XTS",
            0x2000 or 0x2001 or 0x2002 or 0x2003 or 0x2004 or 0x2005 => "AES-CCM (key protection)",
            _ => $"unknown (0x{a:x4})",
        };

        public static string DescribeDataset(byte[] data, int at, string title)
        {
            var sb = new StringBuilder();
            if (data.Length < at + 0x30) return $"{title}: too short\n";
            var size = BitConverter.ToInt32(data, at);
            var headerSize = BitConverter.ToInt32(data, at + 8);
            sb.Append($"{title}:\n");
            sb.Append($"  Size: {size}\n");
            sb.Append($"  GUID: {new Guid(data.AsSpan(at + 0x10, 16))}\n");
            sb.Append($"  Next counter: {BitConverter.ToUInt32(data, at + 0x20)}\n");
            sb.Append($"  Encryption: {Algorithm(BitConverter.ToUInt16(data, at + 0x24))}\n");
            sb.Append($"  Created: {Time(BitConverter.ToInt64(data, at + 0x28))}\n");
            Datums(sb, data, at + headerSize, Math.Min(data.Length, at + size), 1);
            return sb.ToString();
        }

        static string Time(long filetime) =>
            filetime > 0 && filetime < DateTime.MaxValue.ToFileTimeUtc() ? DateTime.FromFileTimeUtc(filetime).ToString("yyyy-MM-dd HH:mm:ss 'UTC'") : "-";

        static readonly Dictionary<ushort, string> EntryTypes = new()
        {
            [0] = "property", [2] = "VMK", [3] = "FVEK", [4] = "validation", [6] = "startup key", [7] = "description", [0xB] = "FVEK backup", [0xF] = "volume header block",
        };

        static void Datums(StringBuilder sb, byte[] data, int pos, int end, int depth)
        {
            var indent = new string(' ', depth * 2);
            while (pos + 8 <= end)
            {
                var size = BitConverter.ToUInt16(data, pos);
                if (size < 8 || pos + size > end) break;
                var entry = BitConverter.ToUInt16(data, pos + 2);
                var type = BitConverter.ToUInt16(data, pos + 4);
                var label = EntryTypes.GetValueOrDefault(entry, $"entry 0x{entry:x}");
                var body = pos + 8;
                switch (type)
                {
                    case 1:
                        sb.Append($"{indent}Key ({label}): {Algorithm(BitConverter.ToUInt16(data, body))}, {Convert.ToHexString(data, body + 4, size - 12)}\n");
                        break;
                    case 2:
                        sb.Append($"{indent}String ({label}): {Encoding.Unicode.GetString(data, body, size - 8).TrimEnd('\0')}\n");
                        break;
                    case 3:
                        sb.Append($"{indent}Stretch key: {Algorithm(BitConverter.ToUInt16(data, body))}, salt {Convert.ToHexString(data, body + 4, 16)}\n");
                        Datums(sb, data, body + 20, pos + size, depth + 1);
                        break;
                    case 5:
                        sb.Append($"{indent}AES-CCM encrypted ({label}): nonce {Convert.ToHexString(data, body, 12)}, {size - 36} byte payload\n");
                        break;
                    case 6:
                        sb.Append($"{indent}TPM-sealed blob: {size - 8} bytes\n");
                        break;
                    case 8:
                        var protection = BitConverter.ToUInt16(data, body + 26);
                        sb.Append($"{indent}VMK {new Guid(data.AsSpan(body, 16))}, protected by {Protector(protection)}, last change {Time(BitConverter.ToInt64(data, body + 16))}\n");
                        Datums(sb, data, body + 28, pos + size, depth + 1);
                        break;
                    case 9:
                        sb.Append($"{indent}External key {new Guid(data.AsSpan(body, 16))}, created {Time(BitConverter.ToInt64(data, body + 16))}\n");
                        Datums(sb, data, body + 24, pos + size, depth + 1);
                        break;
                    case 15:
                        sb.Append($"{indent}Offset and size ({label}): 0x{BitConverter.ToInt64(data, body):x}, {BitConverter.ToInt64(data, body + 8)} bytes\n");
                        break;
                    default:
                        sb.Append($"{indent}Datum ({label}), value type {type}, {size - 8} bytes\n");
                        break;
                }
                pos += size;
            }
        }

        static string Protector(ushort p) => p switch
        {
            0x0000 => "clear key (protection suspended)",
            0x0100 => "TPM",
            0x0200 => "startup key",
            0x0500 => "TPM and PIN",
            0x0800 => "recovery password",
            0x2000 => "password",
            _ => $"protector type 0x{p:x4}",
        };
    }

    const uint IOCTL_DISK_GET_LENGTH_INFO = 0x0007405C;

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe bool DeviceIoControl(Microsoft.Win32.SafeHandles.SafeFileHandle device, uint code, void* input, uint inputSize, void* output, uint outputSize, out uint returned, IntPtr overlapped);
}
