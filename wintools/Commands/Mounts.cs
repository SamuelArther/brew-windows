using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// bindfs / archivemount / squashfuse / xmount: "mounts" made of what stock Windows has, since it has
// no FUSE. bindfs is a directory junction (or a subst drive letter); archivemount and squashfuse
// unpack into the mount point (archivemount packs changes back on unmount); xmount writes the
// converted image (or a VMDK descriptor that points at it). `<tool> -u <mountpoint>` unmounts.
static class Mounts
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("bindfs", "Mirror a folder at another path (directory junction or drive letter)", Bindfs);
        Commands.Add("archivemount", "Open an archive as a folder; changes are packed back on unmount", Archivemount);
        Commands.Add("squashfuse", "Unpack a squashfs image (or AppImage) read-only into a folder", Squashfuse);
        Commands.Add("xmount", "Convert disk images between raw, VHD and VMDK into a folder", Xmount);
    }

    // ---- mount registry: %LOCALAPPDATA%\wintools\mounts\<id>.mount, key=value lines ----

    static string StateDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "wintools", "mounts");

    static string StateFile(string mountpoint) =>
        Path.Combine(StateDir, Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(mountpoint.ToUpperInvariant())))[..16] + ".mount");

    internal static void Save(string mountpoint, Dictionary<string, string> state)
    {
        Directory.CreateDirectory(StateDir);
        state["mountpoint"] = mountpoint;
        File.WriteAllLines(StateFile(mountpoint), state.Select(kv => $"{kv.Key}={kv.Value.Replace("\n", "\\n")}"));
    }

    internal static void Forget(string mountpoint)
    {
        if (File.Exists(StateFile(mountpoint))) File.Delete(StateFile(mountpoint));
    }

    internal static Dictionary<string, string> Load(string mountpoint)
    {
        var file = StateFile(mountpoint);
        if (!File.Exists(file)) return null;
        return File.ReadAllLines(file).Where(l => l.Contains('=')).ToDictionary(l => l[..l.IndexOf('=')], l => l[(l.IndexOf('=') + 1)..].Replace("\\n", "\n"));
    }

    internal static string MountPath(string path)
    {
        if (path.Length == 2 && path[1] == ':' && char.IsLetter(path[0])) return path.ToUpperInvariant();
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    // FUSE wants an existing, empty directory (unless -o nonempty).
    static void CheckMountpoint(string mountpoint, bool nonempty)
    {
        if (Load(mountpoint) != null) throw new IOException($"{mountpoint} is already mounted (unmount it with -u)");
        if (!Directory.Exists(mountpoint)) throw new IOException($"fuse: bad mount point `{mountpoint}': No such file or directory");
        if (!nonempty && Directory.EnumerateFileSystemEntries(mountpoint).Any())
            throw new IOException($"fuse: mountpoint is not empty: {mountpoint} (use -o nonempty)");
    }

    // Comma-separated -o options, collected from every -o.
    internal static Dictionary<string, string> Options(List<string> raw) =>
        raw.SelectMany(o => o.Split(',', StringSplitOptions.RemoveEmptyEntries))
           .Select(o => o.Split('=', 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last().Length > 1 ? g.Last()[1] : "");

    internal static int Unmount(string tool, string target)
    {
        var mountpoint = MountPath(target);
        var state = Load(mountpoint);
        if (state == null) { Console.Error.WriteLine($"{tool}: {target}: not mounted"); return 1; }
        switch (state["kind"])
        {
            case "junction":
                RemoveJunction(mountpoint);
                if (state.GetValueOrDefault("created") != "1") Directory.CreateDirectory(mountpoint);
                break;
            case "drive":
                if (!DefineDosDeviceW(DDD_REMOVE_DEFINITION | DDD_RAW_TARGET_PATH | DDD_EXACT_MATCH_ON_REMOVE, mountpoint, @"\??\" + state["source"]))
                    throw new IOException($"could not remove drive {mountpoint} (error {Marshal.GetLastWin32Error()})");
                break;
            case "archive":
                if (state.GetValueOrDefault("save") == "1" && Manifest(mountpoint) != state["manifest"]) Repack(mountpoint, state);
                Clear(mountpoint);
                break;
            case "extract":
                Clear(mountpoint);
                break;
            case "xmount":
                foreach (var file in state["files"].Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    if (File.Exists(file)) { File.SetAttributes(file, FileAttributes.Normal); File.Delete(file); }
                break;
            case "rclone":
                RcloneMounts.Stop(state);
                if (state.GetValueOrDefault("recreate") == "1") Directory.CreateDirectory(mountpoint);
                break;
        }
        File.Delete(StateFile(mountpoint));
        return 0;
    }

    static void Clear(string dir)
    {
        foreach (var entry in new DirectoryInfo(dir).EnumerateFileSystemInfos("*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 }))
            if (entry.Attributes.HasFlag(FileAttributes.ReadOnly)) entry.Attributes &= ~FileAttributes.ReadOnly;
        foreach (var entry in new DirectoryInfo(dir).EnumerateFileSystemInfos())
        {
            if (entry is DirectoryInfo d) d.Delete(true);
            else entry.Delete();
        }
    }

    // Sizes and write times of everything below dir, to tell whether an archive mount was changed.
    static string Manifest(string dir) =>
        string.Join("|", new DirectoryInfo(dir).EnumerateFileSystemInfos("*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 })
            .Select(e => $"{Path.GetRelativePath(dir, e.FullName)}:{(e is FileInfo f ? f.Length : -1)}:{e.LastWriteTimeUtc.Ticks}").Order(StringComparer.Ordinal));

    static bool TakeUnmount(string tool, string[] args, out int status)
    {
        status = 0;
        var at = Array.FindIndex(args, a => a is "-u" or "--unmount");
        if (at < 0) return false;
        if (at + 1 >= args.Length) throw new UsageException($"{args[at]} needs a mount point");
        status = Unmount(tool, args[at + 1]);
        return true;
    }

    // Splits "tool [options] source mountpoint" into -o values, flags and positionals.
    static (List<string> Opts, List<string> Flags, List<string> Positional) Parse(string[] args, params string[] valueFlags)
    {
        var opts = new List<string>();
        var flags = new List<string>();
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "-o") { if (++i >= args.Length) throw new UsageException("-o needs options"); opts.Add(args[i]); }
            else if (a.StartsWith("-o") && a.Length > 2) opts.Add(a[2..]);
            else if (valueFlags.Contains(a)) { if (++i >= args.Length) throw new UsageException($"{a} needs a value"); flags.Add(a + "=" + args[i]); }
            else if (a.StartsWith('-') && a.Length > 1) flags.Add(a);
            else positional.Add(a);
        }
        return (opts, flags, positional);
    }

    static bool Version(string tool, List<string> flags)
    {
        if (!flags.Any(f => f is "-V" or "--version")) return false;
        Console.WriteLine($"{tool} {Commands.Version} (wintools)");
        return true;
    }

    // ---- bindfs ----

    static readonly string[] RemapOptions = ["perms", "force-user", "force-group", "mirror", "mirror-only", "map", "map-passwd", "map-group",
        "create-for-user", "create-for-group", "create-with-perms", "chown-", "chgrp-", "chmod-", "xattr-", "ctime-from-mtime", "hide-hard-links",
        "resolve-symlinks", "resolved-symlink-deletion", "realistic-permissions", "uid-offset", "gid-offset", "delete-deny", "rename-deny"];

    static int Bindfs(string[] args)
    {
        if (TakeUnmount("bindfs", args, out var status)) return status;
        var (opts, flags, positional) = Parse(args, "-u", "-g", "-p", "-m", "-M");
        if (flags.Any(f => f is "-h" or "--help"))
        {
            Console.WriteLine("""
                Usage: bindfs [options] dir mountpoint
                Mirrors dir at mountpoint with a directory junction (mountpoint may also be a drive letter,
                e.g. X:, mapped like subst). Unmount with: bindfs -u mountpoint

                Options:
                  -V, --version        print version
                  -h, --help           this text
                  -o nonempty          allow a non-empty mount point (its contents are hidden until unmount)
                Ownership/permission remapping (--perms, -u, -g, --mirror, --map, -r ...) needs a real
                FUSE file system and is not available on Windows.
                """);
            return 0;
        }
        if (Version("bindfs", flags)) return 0;
        var options = Options(opts);
        var remap = flags.FirstOrDefault(f => f is "-r" or "--read-only" || f.StartsWith("-u=") || f.StartsWith("-g=") || f.StartsWith("-p=") || f.StartsWith("-m=") || f.StartsWith("-M=")
            || RemapOptions.Any(r => f.StartsWith("--" + r)))
            ?? options.Keys.FirstOrDefault(k => k is "ro" || RemapOptions.Any(r => k.StartsWith(r)));
        if (remap != null)
        {
            Console.Error.WriteLine($"bindfs: {remap.Split('=')[0]}: a directory junction can't remap permissions, owners or make a read-only view on Windows.");
            return 1;
        }
        var unknown = flags.FirstOrDefault(f => f is not ("-f" or "-d" or "-s" or "--no-allow-other" or "--multithreaded"));
        if (unknown != null) throw new UsageException($"unknown option {unknown}");
        if (positional.Count != 2) throw new UsageException("Usage: bindfs [options] dir mountpoint");
        var source = Path.GetFullPath(positional[0]);
        if (!Directory.Exists(source)) throw new IOException($"{positional[0]}: No such directory");
        var mountpoint = MountPath(positional[1]);
        if (mountpoint.Length == 2)
        {
            if (Directory.Exists(mountpoint + "\\")) throw new IOException($"drive {mountpoint} is already in use");
            if (!DefineDosDeviceW(DDD_RAW_TARGET_PATH, mountpoint, @"\??\" + source))
                throw new IOException($"could not map {mountpoint} (error {Marshal.GetLastWin32Error()})");
            Save(mountpoint, new() { ["kind"] = "drive", ["source"] = source });
            return 0;
        }
        var created = !Directory.Exists(mountpoint);
        if (!created) CheckMountpoint(mountpoint, options.ContainsKey("nonempty"));
        if (!created && Directory.EnumerateFileSystemEntries(mountpoint).Any())
            throw new IOException("Windows can't hide a non-empty folder behind a junction; empty it first");
        if (!created) Directory.Delete(mountpoint);
        CreateJunction(mountpoint, source);
        Save(mountpoint, new() { ["kind"] = "junction", ["source"] = source, ["created"] = created ? "1" : "0" });
        return 0;
    }

    // A mount-point reparse point (what `mklink /J` makes): no privileges needed, unlike symlinks.
    static unsafe void CreateJunction(string link, string target)
    {
        Directory.CreateDirectory(link);
        var substitute = Encoding.Unicode.GetBytes(@"\??\" + Path.TrimEndingDirectorySeparator(target) + "\\");
        var print = Encoding.Unicode.GetBytes(target);
        var buffer = new byte[8 + 8 + substitute.Length + 2 + print.Length + 2];
        BitConverter.TryWriteBytes(buffer.AsSpan(0), IO_REPARSE_TAG_MOUNT_POINT);
        BitConverter.TryWriteBytes(buffer.AsSpan(4), (ushort)(buffer.Length - 8));
        BitConverter.TryWriteBytes(buffer.AsSpan(8), (ushort)0);
        BitConverter.TryWriteBytes(buffer.AsSpan(10), (ushort)substitute.Length);
        BitConverter.TryWriteBytes(buffer.AsSpan(12), (ushort)(substitute.Length + 2));
        BitConverter.TryWriteBytes(buffer.AsSpan(14), (ushort)print.Length);
        substitute.CopyTo(buffer, 16);
        print.CopyTo(buffer, 16 + substitute.Length + 2);
        var h = CreateFileW(link, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);
        if (h == new IntPtr(-1)) { Directory.Delete(link); throw new IOException($"cannot open {link} (error {Marshal.GetLastWin32Error()})"); }
        try
        {
            fixed (byte* p = buffer)
                if (!DeviceIoControl(h, FSCTL_SET_REPARSE_POINT, p, (uint)buffer.Length, null, 0, out _, IntPtr.Zero))
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
        if (!info.Exists) return;
        if (!info.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new IOException($"{link} is not a junction");
        info.Delete();   // removes the link only, never the target's contents
    }

    // ---- archivemount ----

    static string Tar => Path.Combine(Environment.SystemDirectory, "tar.exe");

    static int Archivemount(string[] args)
    {
        if (TakeUnmount("archivemount", args, out var status)) return status;
        var (opts, flags, positional) = Parse(args);
        if (flags.Any(f => f is "-h" or "--help"))
        {
            Console.WriteLine("""
                usage: archivemount archivepath mountpoint [options]
                Unpacks the archive into mountpoint (Windows has no FUSE); on unmount (archivemount -u
                mountpoint) changes are packed back into the archive, the old one kept as archive.orig.
                Archives are read and written with Windows' tar.exe (libarchive: zip, tar, tgz, ...).

                archivemount options:
                    -o readonly            disable write support (files are marked read-only)
                    -o nobackup            remove archive file backups
                    -o nosave              do not save changes upon unmount
                    -o formatraw           treat the input as a single compressed file (.gz)
                    -o nonempty            allow a non-empty mount point
                    -V, --version          print version
                    -h, --help             this text
                """);
            return 0;
        }
        if (Version("archivemount", flags)) return 0;
        if (flags.FirstOrDefault(f => f is not ("-f" or "-d" or "-s")) is { } bad) throw new UsageException($"unknown option {bad}");
        if (positional.Count != 2) throw new UsageException("usage: archivemount archivepath mountpoint [options]");
        var options = Options(opts);
        var archive = Path.GetFullPath(positional[0]);
        if (!File.Exists(archive)) throw new IOException($"{positional[0]}: No such file");
        var mountpoint = MountPath(positional[1]);
        CheckMountpoint(mountpoint, options.ContainsKey("nonempty"));
        var readOnly = options.ContainsKey("readonly") || options.ContainsKey("ro");
        if (options.ContainsKey("formatraw"))
        {
            if (!archive.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("formatraw supports gzip (.gz) files; other raw compressors need their own tools on Windows");
            using var input = new GZipStream(File.OpenRead(archive), CompressionMode.Decompress);
            using var output = File.Create(Path.Combine(mountpoint, "data"));
            input.CopyTo(output);
        }
        else RunTar(["-xf", archive, "-C", mountpoint], "unpack");
        if (readOnly)
            foreach (var f in Directory.EnumerateFiles(mountpoint, "*", SearchOption.AllDirectories)) File.SetAttributes(f, File.GetAttributes(f) | FileAttributes.ReadOnly);
        Save(mountpoint, new()
        {
            ["kind"] = "archive",
            ["source"] = archive,
            ["save"] = readOnly || options.ContainsKey("nosave") ? "0" : "1",
            ["backup"] = options.ContainsKey("nobackup") ? "0" : "1",
            ["raw"] = options.ContainsKey("formatraw") ? "1" : "0",
            ["manifest"] = Manifest(mountpoint),
        });
        return 0;
    }

    static void Repack(string mountpoint, Dictionary<string, string> state)
    {
        var archive = state["source"];
        var temp = Path.Combine(Path.GetDirectoryName(archive), "." + Path.GetFileName(archive) + ".archivemount-" + Environment.ProcessId + ArchiveExtension(archive));
        try
        {
            if (state["raw"] == "1")
            {
                using var input = File.OpenRead(Path.Combine(mountpoint, "data"));
                using var output = new GZipStream(File.Create(temp), CompressionLevel.SmallestSize);
                input.CopyTo(output);
            }
            else
            {
                var entries = Directory.EnumerateFileSystemEntries(mountpoint).Select(Path.GetFileName).ToList();
                RunTar(["-a", "-cf", temp, "-C", mountpoint, .. entries], "pack");
            }
            if (state["backup"] == "1") File.Copy(archive, archive + ".orig", overwrite: true);
            File.Move(temp, archive, overwrite: true);
        }
        catch
        {
            if (File.Exists(temp)) File.Delete(temp);
            throw;   // the unpacked files stay where they are, nothing is lost
        }
    }

    // tar -a picks the output format from the suffix, so the temporary file keeps the archive's.
    static string ArchiveExtension(string archive)
    {
        var name = Path.GetFileName(archive).ToLowerInvariant();
        foreach (var ext in new[] { ".tar.gz", ".tar.bz2", ".tar.xz", ".tar.zst", ".tar.lzma", ".tar.z" })
            if (name.EndsWith(ext)) return ext;
        return Path.GetExtension(name);
    }

    static void RunTar(string[] args, string what)
    {
        if (!File.Exists(Tar)) throw new IOException($"{Tar} is missing (Windows 10 1803 or later ships it)");
        var psi = new ProcessStartInfo(Tar) { UseShellExecute = false, RedirectStandardError = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        var error = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new IOException($"could not {what} the archive: {error.Trim()}");
    }

    // ---- squashfuse ----

    static int Squashfuse(string[] args)
    {
        if (TakeUnmount("squashfuse", args, out var status)) return status;
        var (opts, flags, positional) = Parse(args);
        if (flags.Any(f => f is "-h" or "--help"))
        {
            Console.WriteLine("""
                Usage: squashfuse [options] ARCHIVE MOUNTPOINT
                Unpacks a squashfs image read-only into MOUNTPOINT (Windows has no FUSE).
                Unmount with: squashfuse -u MOUNTPOINT
                Supports gzip/zlib-compressed and uncompressed images (squashfs 4.0).

                Options:
                    -o offset=N            offset of the image in the file (e.g. an AppImage's payload)
                    -o nonempty            allow a non-empty mount point
                    -V, --version          print version
                    -h, --help             this text
                """);
            return 0;
        }
        if (Version("squashfuse", flags)) return 0;
        if (flags.FirstOrDefault(f => f is not ("-f" or "-d" or "-s")) is { } bad) throw new UsageException($"unknown option {bad}");
        if (positional.Count != 2) throw new UsageException("Usage: squashfuse [options] ARCHIVE MOUNTPOINT");
        var options = Options(opts);
        long offset = 0;
        if (options.TryGetValue("offset", out var o) && !long.TryParse(o, out offset)) throw new UsageException("offset must be a number");
        var image = Path.GetFullPath(positional[0]);
        if (!File.Exists(image)) throw new IOException($"{positional[0]}: No such file");
        var mountpoint = MountPath(positional[1]);
        CheckMountpoint(mountpoint, options.ContainsKey("nonempty"));
        try { Squashfs.Extract(image, offset, mountpoint); }
        catch (InvalidDataException e)
        {
            // A damaged compressed block.
            Clear(mountpoint);
            throw new IOException($"damaged image: {e.Message}");
        }
        catch
        {
            Clear(mountpoint);
            throw;
        }
        Save(mountpoint, new() { ["kind"] = "extract", ["source"] = image });
        return 0;
    }

    // ---- xmount ----

    static int Xmount(string[] args)
    {
        if (TakeUnmount("xmount", args, out var status)) return status;
        string inType = null, outType = "raw";
        var inputs = new List<string>();
        long offset = 0, sizeLimit = -1;
        string mountpoint = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: xmount [fopts] <xopts> <mntp>
                          --in <itype> <ifile> [<ifile> ...]   input image (raw/dd split segments are joined; vhd)
                          --out <otype>                        output type: raw (default), dd, vhd, vmdk
                          --offset <off>                       start the output at this byte offset of the input
                          --sizelimit <size>                   limit the output to this many bytes
                          --cache <file>                       accepted (the output is a separate file anyway)
                          -u <mntp>                            unmount (delete the output files)
                          --version                            print version
                        <mntp> receives <name>.<otype> and <name>.info. Windows can attach the VHD output
                        itself (Disk Management, or Mount-DiskImage). E01/AFF/QCOW/VDI inputs need their own
                        tools on Windows.
                        """);
                    return 0;
                case "--version" or "-V": Console.WriteLine($"xmount {Commands.Version} (wintools)"); return 0;
                case "--in":
                    inType = Next().ToLowerInvariant();
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--")) inputs.Add(args[++i]);
                    break;
                case "--out": outType = Next().ToLowerInvariant(); break;
                case "--offset": offset = Number(Next()); break;
                case "--sizelimit": sizeLimit = Number(Next()); break;
                case "--cache" or "--inopts" or "--outopts" or "--morph" or "--morphopts": Next(); break;
                case "-d" or "-f" or "-s" or "--info": break;
                case "-o": Next(); break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    mountpoint = a;
                    break;
            }
        }
        // The last --in argument is the mount point when it was swallowed as an input.
        if (mountpoint == null && inputs.Count > 1) { mountpoint = inputs[^1]; inputs.RemoveAt(inputs.Count - 1); }
        if (inType == null || inputs.Count == 0 || mountpoint == null) throw new UsageException("Usage: xmount [fopts] --in <itype> <ifile> --out <otype> <mntp>");
        if (inType is not ("raw" or "dd" or "vhd"))
        {
            Console.Error.WriteLine($"xmount: input type '{inType}' isn't available on Windows (supported: raw, dd, vhd)");
            return 1;
        }
        if (outType is not ("raw" or "dd" or "vhd" or "vmdk"))
        {
            Console.Error.WriteLine($"xmount: output type '{outType}' isn't available on Windows (supported: raw, dd, vhd, vmdk)");
            return 1;
        }
        var files = inputs.Select(Path.GetFullPath).ToList();
        foreach (var f in files.Where(f => !File.Exists(f))) throw new IOException($"{f}: No such file");
        var mnt = MountPath(mountpoint);
        CheckMountpoint(mnt, true);
        var name = Path.GetFileNameWithoutExtension(files[0]);
        var outFile = Path.Combine(mnt, name + "." + (outType == "dd" ? "dd" : outType));
        var created = new List<string>();
        try
        {
            using var source = new DiskImage(inType, files);
            var length = Math.Max(0, source.Length - offset);
            if (sizeLimit >= 0) length = Math.Min(length, sizeLimit);
            if (outType == "vmdk" && source.IsFlat && offset == 0 && sizeLimit < 0 && files.Count == 1)
            {
                // A monolithicFlat descriptor that points at the input: no copy at all.
                File.WriteAllText(outFile, VmdkDescriptor(length, files[0]));
            }
            else
            {
                var data = outType == "vmdk" ? Path.Combine(mnt, name + "-flat.vmdk") : outFile;
                if (outType == "vmdk") { File.WriteAllText(outFile, VmdkDescriptor(length, data)); created.Add(outFile); }
                using (var output = File.Create(data))
                {
                    created.Add(data);
                    source.CopyTo(output, offset, length);
                    if (outType == "vhd") output.Write(VhdFooter(length));
                }
            }
            if (!created.Contains(outFile)) created.Add(outFile);
            var info = Path.Combine(mnt, name + ".info");
            File.WriteAllText(info, $"""
                ------> The following values are supplied by the used input library(ies) <------
                --> {string.Join(", ", files)} <--
                Input type: {inType}{(source.Detail != null ? " (" + source.Detail + ")" : "")}
                Segments: {files.Count}
                Size: {source.Length} bytes
                ------> Output <------
                Output type: {outType}
                Offset: {offset}
                Size: {length} bytes

                """);
            created.Add(info);
            foreach (var f in created) File.SetAttributes(f, FileAttributes.ReadOnly);
        }
        catch
        {
            foreach (var f in created.Where(File.Exists)) { File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); }
            throw;
        }
        Save(mnt, new() { ["kind"] = "xmount", ["source"] = string.Join("\n", files), ["files"] = string.Join("\n", created) });
        return 0;
    }

    static long Number(string s) => long.TryParse(s, out var n) && n >= 0 ? n : throw new UsageException($"not a byte count: {s}");

    static string VmdkDescriptor(long length, string extent) => $"""
        # Disk DescriptorFile
        version=1
        CID=fffffffe
        parentCID=ffffffff
        createType="monolithicFlat"

        # Extent description
        RW {length / 512} FLAT "{extent}" 0

        # The Disk Data Base
        ddb.virtualHWVersion = "4"
        ddb.adapterType = "ide"

        """;

    // A fixed-size VHD is the raw disk plus this 512-byte footer.
    internal static byte[] VhdFooter(long size)
    {
        var f = new byte[512];
        "conectix"u8.CopyTo(f);
        void BE32(int at, uint v) => System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(f.AsSpan(at), v);
        void BE64(int at, long v) => System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(f.AsSpan(at), v);
        BE32(8, 2);                                   // features: reserved bit
        BE32(12, 0x00010000);                         // format version 1.0
        BE64(16, -1);                                 // data offset: none (fixed disk)
        BE32(24, (uint)(DateTime.UtcNow - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
        "wtls"u8.CopyTo(f.AsSpan(28));                // creator application
        BE32(32, 0x00010000);
        "Wi2k"u8.CopyTo(f.AsSpan(36));
        BE64(40, size);
        BE64(48, size);
        // CHS geometry, per the VHD specification's algorithm.
        var sectors = Math.Min(size / 512, 65535L * 16 * 255);
        long spt, heads, cylTimesHeads;
        if (sectors >= 65535L * 16 * 63) { spt = 255; heads = 16; cylTimesHeads = sectors / spt; }
        else
        {
            spt = 17; cylTimesHeads = sectors / spt; heads = (cylTimesHeads + 1023) / 1024;
            if (heads < 4) heads = 4;
            if (cylTimesHeads >= heads * 1024 || heads > 16) { spt = 31; heads = 16; cylTimesHeads = sectors / spt; }
            if (cylTimesHeads >= heads * 1024) { spt = 63; heads = 16; cylTimesHeads = sectors / spt; }
        }
        var cylinders = cylTimesHeads / heads;
        f[56] = (byte)(cylinders >> 8); f[57] = (byte)cylinders; f[58] = (byte)heads; f[59] = (byte)spt;
        BE32(60, 2);                                  // disk type: fixed
        Guid.NewGuid().ToByteArray().CopyTo(f, 68);
        uint sum = 0;
        foreach (var b in f) sum += b;
        BE32(64, ~sum);
        return f;
    }

    // Raw (split segments joined in order) or VHD (fixed or dynamic) input, read as one flat disk.
    sealed class DiskImage : IDisposable
    {
        readonly List<FileStream> parts;
        readonly long[] starts;
        readonly uint[] bat;
        readonly long blockSize;
        public long Length { get; }
        public string Detail { get; }
        public bool IsFlat => bat == null;

        public DiskImage(string type, List<string> files)
        {
            parts = files.Select(f => new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read)).ToList();
            if (type is "raw" or "dd")
            {
                starts = new long[parts.Count];
                for (var i = 1; i < parts.Count; i++) starts[i] = starts[i - 1] + parts[i - 1].Length;
                Length = starts[^1] + parts[^1].Length;
                return;
            }
            var s = parts[0];
            var footer = new byte[512];
            s.Seek(-512, SeekOrigin.End);
            s.ReadExactly(footer);
            if (!footer.AsSpan(0, 8).SequenceEqual("conectix"u8)) throw new IOException("not a VHD file");
            Length = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(footer.AsSpan(48));
            var diskType = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(footer.AsSpan(60));
            if (diskType == 2) { starts = [0]; Detail = "fixed VHD"; return; }
            if (diskType == 4) throw new IOException("differencing VHDs need their parent; merge them first");
            // Dynamic: header at the footer's data offset, then the block allocation table.
            var header = new byte[1024];
            s.Seek(System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(footer.AsSpan(16)), SeekOrigin.Begin);
            s.ReadExactly(header);
            var tableOffset = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(header.AsSpan(16));
            var entries = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(28));
            blockSize = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(32));
            var table = new byte[entries * 4];
            s.Seek(tableOffset, SeekOrigin.Begin);
            s.ReadExactly(table);
            bat = new uint[entries];
            for (var i = 0; i < entries; i++) bat[i] = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(i * 4));
            Detail = "dynamic VHD";
        }

        public void CopyTo(Stream output, long offset, long length)
        {
            var buffer = new byte[1 << 20];
            while (length > 0)
            {
                var n = (int)Math.Min(buffer.Length, length);
                n = Read(offset, buffer, n);
                output.Write(buffer, 0, n);
                offset += n;
                length -= n;
            }
        }

        // Reads up to count bytes at a disk offset (staying within one segment or block).
        int Read(long offset, byte[] buffer, int count)
        {
            if (bat != null)
            {
                var block = offset / blockSize;
                var within = offset % blockSize;
                count = (int)Math.Min(count, blockSize - within);
                if (bat[block] == 0xFFFFFFFF) { Array.Clear(buffer, 0, count); return count; }
                // Each block starts with a sector bitmap, padded to a sector.
                var bitmap = ((blockSize / 512 / 8) + 511) / 512 * 512;
                parts[0].Seek(bat[block] * 512L + bitmap + within, SeekOrigin.Begin);
                parts[0].ReadExactly(buffer, 0, count);
                return count;
            }
            var index = Array.FindLastIndex(starts, st => st <= offset);
            var part = parts[index];
            var local = offset - starts[index];
            count = (int)Math.Min(count, part.Length - local - (index == 0 && Detail == "fixed VHD" ? 512 : 0));
            part.Seek(local, SeekOrigin.Begin);
            part.ReadExactly(buffer, 0, count);
            return count;
        }

        public void Dispose() { foreach (var p in parts) p.Dispose(); }
    }

    const uint DDD_RAW_TARGET_PATH = 1, DDD_REMOVE_DEFINITION = 2, DDD_EXACT_MATCH_ON_REMOVE = 4;
    const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003, FSCTL_SET_REPARSE_POINT = 0x900A4;
    const uint GENERIC_WRITE = 0x40000000, OPEN_EXISTING = 3, FILE_FLAG_BACKUP_SEMANTICS = 0x02000000, FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool DefineDosDeviceW(uint flags, string device, string target);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe bool DeviceIoControl(IntPtr device, uint code, void* input, uint inputSize, void* output, uint outputSize, out uint returned, IntPtr overlapped);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
