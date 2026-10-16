using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// create-dmg: builds the disk image a Windows user double-clicks to mount - an ISO 9660 + Joliet +
// UDF image made with IMAPI2's file-system image builder (IMAPI2FS, part of every Windows), from a
// source folder plus create-dmg's extra files, volume name, volume icon (autorun.inf) and EULA.
// Finder window layout options (background, icon positions, window size) are accepted and ignored:
// Explorer has no per-volume window layout.
static class CreateDmg
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("create-dmg", "Build mountable disk images (ISO/UDF) from a folder", Run);
    }

    const string Help = """
        create-dmg <version> (Windows: ISO 9660 + Joliet + UDF images built with IMAPI2)

        Creates a disk image that mounts with a double-click in Explorer.

        Usage: create-dmg [options] <output_name.dmg|.iso> <source_folder>

        All contents of <source_folder> will be copied into the disk image.

        Options:
          --volname <name>         set volume name (displayed in Explorer; default: output file name)
          --volicon <icon>         set volume icon (.ico, .icns, .png or any image; via autorun.inf)
          --eula <file>            add the license file to the image (as License.<ext>)
          --add-file <target_name> <file> <x> <y>
                                   add an additional file to the image
          --add-folder <target_name> <folder> <x> <y>
                                   add an additional folder to the image
          --filesystem <fs>        UDF, ISO9660 (ISO 9660 + Joliet) or ALL (default; HFS+/APFS map to ALL)
          --format <fmt>           accepted for compatibility; the image is always an uncompressed ISO
          --hdiutil-verbose, --hdiutil-quiet
                                   more or less output
          --sandbox-safe, --skip-jenkins, --no-internet-enable, --bless
                                   accepted (nothing to do on Windows)
          --version                show create-dmg version number
          -h, --help               display this help screen

        Accepted and ignored (Explorer has no per-volume window layout):
          --background, --window-pos, --window-size, --text-size, --icon-size, --icon,
          --hide-extension, --app-drop-link, --ql-drop-link, --rez, --disk-image-size,
          --encrypt, --encrypt-aes128, --codesign, --notarize
        """;

    static int Run(string[] args)
    {
        string volname = null, volicon = null, eula = null;
        int fileSystems = 7;   // FsiFileSystemISO9660 | Joliet | UDF
        bool quiet = false, verbose = false;
        var extras = new List<(string Target, string Source)>();
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            string Value(int n = 1)
            {
                if (i + n >= args.Length) throw new UsageException($"{args[i]} needs {n} argument(s)");
                var v = args[i + 1];
                i += n;
                return v;
            }
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(Help.Replace("<version>", Commands.Version)); return 0;
                case "--version": Console.WriteLine($"create-dmg {Commands.Version}"); return 0;
                case "--volname": volname = Value(); break;
                case "--volicon": volicon = Value(); break;
                case "--eula": eula = Value(); break;
                case "--add-file" or "--add-folder":
                    var target = args.Length > i + 2 ? args[i + 1] : throw new UsageException($"{args[i]} needs <target_name> <source> <x> <y>");
                    var source = args[i + 2];
                    i += args.Length > i + 4 ? 4 : 2;
                    extras.Add((target, source));
                    break;
                case "--filesystem":
                    fileSystems = Value().ToUpperInvariant() switch
                    {
                        "UDF" => 4,
                        "ISO9660" or "ISO" or "JOLIET" => 3,
                        "ALL" or "HFS+" or "APFS" => 7,
                        var fs => throw new UsageException($"unknown filesystem '{fs}' (UDF, ISO9660, ALL)"),
                    };
                    break;
                case "--hdiutil-quiet": quiet = true; break;
                case "--hdiutil-verbose": verbose = true; break;
                case "--format" or "--background" or "--text-size" or "--icon-size" or "--hide-extension" or "--rez"
                    or "--disk-image-size" or "--codesign" or "--notarize": Value(); break;
                case "--window-pos" or "--window-size" or "--app-drop-link" or "--ql-drop-link": Value(2); break;
                case "--icon": Value(3); break;
                case "--sandbox-safe" or "--skip-jenkins" or "--no-internet-enable" or "--bless" or "--encrypt" or "--encrypt-aes128": break;
                case var a when a.StartsWith("--"): throw new UsageException($"unknown option {a}");
                default: positional.Add(args[i]); break;
            }
        }
        if (positional.Count != 2) throw new UsageException("Not enough arguments. Run 'create-dmg --help' for help.");
        var output = Path.GetFullPath(positional[0]);
        var sourceFolder = Path.GetFullPath(positional[1]);
        if (!Directory.Exists(sourceFolder)) throw new InvalidOperationException($"source folder {positional[1]} does not exist");
        foreach (var (_, src) in extras)
            if (!File.Exists(src) && !Directory.Exists(src)) throw new InvalidOperationException($"{src}: No such file or directory");
        if (eula != null && !File.Exists(eula)) throw new InvalidOperationException($"{eula}: No such file or directory");
        volname ??= Path.GetFileNameWithoutExtension(output);

        if (!quiet) Console.WriteLine("Creating disk image...");
        var staging = Path.Combine(Path.GetTempPath(), "create-dmg-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(staging);
        try
        {
            var files = new List<(string Name, string Path)>();
            if (eula != null) files.Add(("License" + Path.GetExtension(eula), Path.GetFullPath(eula)));
            if (volicon != null)
            {
                var ico = Path.Combine(staging, "VolumeIcon.ico");
                File.WriteAllBytes(ico, ToIco(volicon));
                var autorun = Path.Combine(staging, "autorun.inf");
                File.WriteAllText(autorun, $"[autorun]\r\nicon=VolumeIcon.ico\r\nlabel={volname}\r\n", Encoding.Unicode);
                files.Add(("VolumeIcon.ico", ico));
                files.Add(("autorun.inf", autorun));
            }
            var folders = new List<string>();
            foreach (var (target, src) in extras)
            {
                if (File.Exists(src)) { files.Add((target, Path.GetFullPath(src))); continue; }
                // AddTree names the folder after its source, so stage a correctly named junction-free copy.
                var named = Path.Combine(staging, "folders", target);
                CopyTree(Path.GetFullPath(src), named);
                folders.Add(named);
            }
            Build(output, volname, fileSystems, sourceFolder, folders, files, verbose);
        }
        finally
        {
            try { Directory.Delete(staging, recursive: true); } catch (IOException) { }
        }
        if (!quiet) Console.WriteLine($"Disk image done: {output}");
        return 0;
    }

    static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
        foreach (var d in Directory.GetDirectories(from)) CopyTree(d, Path.Combine(to, Path.GetFileName(d)));
    }

    // Volume icons: .ico as is; .icns -> its largest embedded PNG; anything else decoded by WinRT.
    static byte[] ToIco(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length > 4 && data[0] == 0 && data[1] == 0 && data[2] == 1 && data[3] == 0) return data;
        if (data.Length > 8 && Encoding.ASCII.GetString(data, 0, 4) == "icns")
        {
            byte[] best = null;
            for (var pos = 8; pos + 8 <= data.Length;)
            {
                var len = (data[pos + 4] << 24) | (data[pos + 5] << 16) | (data[pos + 6] << 8) | data[pos + 7];
                if (len < 8 || pos + len > data.Length) break;
                if (len > 16 && data[pos + 8] == 0x89 && data[pos + 9] == (byte)'P' && (best == null || len - 8 > best.Length))
                    best = data[(pos + 8)..(pos + len)];
                pos += len;
            }
            data = best ?? throw new InvalidOperationException($"{path}: no PNG image inside this .icns (only modern .icns files are supported)");
        }
        var images = new List<(int, byte[])>();
        foreach (var size in new[] { 16, 32, 48, 256 })
        {
            var (w, h, bgra) = Icons.Square(data, size);
            images.Add((size, Icons.EncodePng(w, h, bgra)));
        }
        return Icons.BuildIco(images);
    }

    // ---- IMAPI2FS through IDispatch ------------------------------------------------------------

    static void Build(string output, string volname, int fileSystems, string source, List<string> folders,
        List<(string Name, string Path)> files, bool verbose)
    {
        CoInitializeEx(IntPtr.Zero, 0);
        var clsid = new Guid("2C941FC5-975B-59BE-A960-9A2A262853A5");   // MsftFileSystemImage
        var iid = IID_IDispatch;
        var hr = CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var fsi);
        if (hr != 0) throw new InvalidOperationException($"the IMAPI2 file-system image builder is not available (0x{hr:X8})");
        var opened = new List<IntPtr> { fsi };
        try
        {
            Invoke(fsi, "ChooseImageDefaultsForMediaType", DISPATCH_METHOD, I4(18));   // IMAPI_MEDIA_TYPE_BDR
            Invoke(fsi, "FileSystemsToCreate", DISPATCH_PROPERTYPUT, I4(fileSystems));
            Invoke(fsi, "VolumeName", DISPATCH_PROPERTYPUT, Str(volname));
            // Room for everything (2048-byte blocks), whatever the nominal media size.
            var bytes = Size(source) + folders.Sum(Size) + files.Sum(f => new FileInfo(f.Path).Length);
            Invoke(fsi, "FreeMediaBlocks", DISPATCH_PROPERTYPUT, I4((int)Math.Min(int.MaxValue, bytes / 2048 + 65536)));

            var root = Invoke(fsi, "Root", DISPATCH_PROPERTYGET).ptr;
            opened.Add(root);
            if (verbose) Console.WriteLine($"Adding {source}");
            Invoke(root, "AddTree", DISPATCH_METHOD, Str(source), Bool(false));
            foreach (var folder in folders)
            {
                if (verbose) Console.WriteLine($"Adding {folder}");
                Invoke(root, "AddTree", DISPATCH_METHOD, Str(folder), Bool(true));
            }
            foreach (var (name, path) in files)
            {
                if (verbose) Console.WriteLine($"Adding {path} as {name}");
                Check(SHCreateStreamOnFileEx(path, 0x20 /* STGM_READ|STGM_SHARE_DENY_WRITE */, 0, false, IntPtr.Zero, out var stream), path);
                try { Invoke(root, "AddFile", DISPATCH_METHOD, Str(name), Unknown(stream)); }
                finally { Marshal.Release(stream); }
            }

            var result = Invoke(fsi, "CreateResultImage", DISPATCH_METHOD).ptr;
            opened.Add(result);
            var image = Invoke(result, "ImageStream", DISPATCH_PROPERTYGET).ptr;
            opened.Add(image);
            WriteStream(image, output);
        }
        finally
        {
            foreach (var p in opened) if (p != IntPtr.Zero) Marshal.Release(p);
        }
    }

    static long Size(string dir) =>
        new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

    static unsafe void WriteStream(IntPtr stream, string output)
    {
        var tmp = output + ".partial";
        var buffer = new byte[1 << 20];
        using (var file = File.Create(tmp))
        {
            // ISequentialStream::Read is vtable slot 3.
            var read = (delegate* unmanaged<IntPtr, byte*, uint, uint*, int>)(*(IntPtr**)stream)[3];
            while (true)
            {
                uint got;
                int hr;
                fixed (byte* p = buffer) hr = read(stream, p, (uint)buffer.Length, &got);
                if (hr < 0) throw new IOException($"reading the image stream failed (0x{hr:X8})");
                if (got == 0) break;
                file.Write(buffer, 0, (int)got);
            }
        }
        File.Move(tmp, output, overwrite: true);
    }

    static readonly Guid IID_IDispatch = new("00020400-0000-0000-C000-000000000046");
    const ushort DISPATCH_METHOD = 1, DISPATCH_PROPERTYGET = 2, DISPATCH_PROPERTYPUT = 4;
    const ushort VT_I4 = 3, VT_BSTR = 8, VT_BOOL = 11, VT_UNKNOWN = 13;

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    struct VARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr ptr;
        [FieldOffset(8)] public int i4;
        [FieldOffset(8)] public short boolVal;
    }

    static VARIANT I4(int v) => new() { vt = VT_I4, i4 = v };
    static VARIANT Bool(bool v) => new() { vt = VT_BOOL, boolVal = v ? (short)-1 : (short)0 };
    static VARIANT Str(string s) => new() { vt = VT_BSTR, ptr = Marshal.StringToBSTR(s) };
    static VARIANT Unknown(IntPtr p) => new() { vt = VT_UNKNOWN, ptr = p };

    // IDispatch::GetIDsOfNames (slot 5) + Invoke (slot 6). Arguments are given in call order; BSTRs
    // are freed here. The caller owns (and releases) an object returned in the result.
    static unsafe VARIANT Invoke(IntPtr disp, string name, ushort flags, params VARIANT[] args)
    {
        var vtbl = *(IntPtr**)disp;
        var getIds = (delegate* unmanaged<IntPtr, Guid*, char**, uint, uint, int*, int>)vtbl[5];
        var invoke = (delegate* unmanaged<IntPtr, int, Guid*, uint, ushort, DISPPARAMS*, VARIANT*, byte*, uint*, int>)vtbl[6];
        var nullIid = Guid.Empty;
        int dispid;
        int hr;
        fixed (char* n = name)
        {
            var names = n;
            hr = getIds(disp, &nullIid, &names, 1, 0, &dispid);
        }
        if (hr != 0) throw new InvalidOperationException($"IMAPI2: no member {name} (0x{hr:X8})");

        var reversed = Enumerable.Reverse(args).ToArray();
        var putId = -3;   // DISPID_PROPERTYPUT
        var result = new VARIANT();
        var excep = stackalloc byte[64];
        new Span<byte>(excep, 64).Clear();
        uint argErr;
        try
        {
            fixed (VARIANT* a = reversed)
            {
                var dp = new DISPPARAMS
                {
                    rgvarg = a,
                    cArgs = (uint)reversed.Length,
                    rgdispidNamedArgs = flags == DISPATCH_PROPERTYPUT ? &putId : null,
                    cNamedArgs = flags == DISPATCH_PROPERTYPUT ? 1u : 0u,
                };
                hr = invoke(disp, dispid, &nullIid, 0, flags, &dp, &result, excep, &argErr);
            }
        }
        finally
        {
            foreach (var v in args) if (v.vt == VT_BSTR) Marshal.FreeBSTR(v.ptr);
        }
        if (hr != 0)
        {
            var description = hr == unchecked((int)0x80020009) && *(IntPtr*)(excep + 16) != IntPtr.Zero
                ? Marshal.PtrToStringBSTR(*(IntPtr*)(excep + 16)) : null;
            throw new InvalidOperationException($"IMAPI2 {name} failed: {description?.Trim() ?? $"0x{hr:X8}"}");
        }
        return result;
    }

    static void Check(int hr, string what)
    {
        if (hr < 0) throw new IOException($"{what}: cannot open (0x{hr:X8})");
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct DISPPARAMS
    {
        public VARIANT* rgvarg;
        public int* rgdispidNamedArgs;
        public uint cArgs, cNamedArgs;
    }

    [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr reserved, uint coinit);
    [DllImport("ole32.dll")] static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint ctx, ref Guid iid, out IntPtr obj);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateStreamOnFileEx(string file, uint mode, uint attributes, bool create, IntPtr template, out IntPtr stream);
}
