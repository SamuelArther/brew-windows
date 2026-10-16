using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// chrpath: list, replace, convert or delete the DT_RPATH/DT_RUNPATH entry of ELF binaries. Pure file
// editing, so it works on Windows exactly as on Linux (e.g. on cross-compiled or WSL binaries).
// Messages and exit codes follow chrpath 0.18.
static class Chrpath
{
    const string Version = "0.18";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("chrpath", "Edit the rpath in ELF binaries", Run);

    static int Run(string[] args)
    {
        string newPath = null;
        bool delete = false, convert = false, keepGoing = false;
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-v" or "--version":
                    Console.WriteLine($"chrpath version {Version}");
                    return 0;
                case "-h" or "--help":
                    Usage(Console.Out);
                    return 0;
                case "-d" or "--delete": delete = true; break;
                case "-c" or "--convert": convert = true; break;
                case "-l" or "--list": break;
                case "-k" or "--keepgoing": keepGoing = true; break;
                case "-r" or "--replace":
                    if (++i >= args.Length) throw new UsageException("option requires an argument -- 'r'");
                    newPath = args[i];
                    break;
                default:
                    if (a.StartsWith("--replace=")) { newPath = a["--replace=".Length..]; break; }
                    if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    files.Add(a);
                    break;
            }
        }
        if (files.Count == 0) { Usage(Console.Error); return 1; }
        var status = 0;
        foreach (var file in files)
        {
            if (status != 0 && !keepGoing) break;
            status |= delete ? Delete(file) : List(file, newPath, convert);
        }
        return status;
    }

    static void Usage(TextWriter w) => w.Write("""
        Usage: chrpath [-v|-d|-c|-r <path>] <program> [<program> ...]

           -v|--version                Display program version number
           -d|--delete                 Delete current rpath/runpath setting
           -c|--convert                Convert rpath to runpath
           -r <path>|--replace <path>  Replace current rpath/runpath setting
                                       with the path given
           -l|--list                   List the current rpath/runpath (default)
           -k|--keepgoing              Continue as much as possible after an error.
           -h|--help                   Show this usage information.

        """);

    static ElfKit.Elf Open(string file)
    {
        try { return ElfKit.Elf.Load(file); }
        catch (ElfKit.ElfError e) { Console.Error.WriteLine($"{file}: {e.Message}"); }
        catch (IOException e) { Console.Error.WriteLine($"open: {e.Message}"); }
        catch (UnauthorizedAccessException e) { Console.Error.WriteLine($"open: {e.Message}"); }
        return null;
    }

    static int List(string file, string newPath, bool convert)
    {
        var elf = Open(file);
        if (elf == null) return 1;
        var dyns = elf.Dynamic();
        if (dyns.Count == 0) { Console.Error.WriteLine($"{file}: no dynamic section found"); return 1; }
        var entry = dyns.FirstOrDefault(d => d.Tag is ElfKit.DT_RPATH or ElfKit.DT_RUNPATH);
        if (entry == null)
        {
            Console.WriteLine($"{file}: no rpath or runpath tag found.");
            return 2;
        }
        var strtab = elf.DynStrOffset();
        if (strtab < 0) { Console.Error.WriteLine($"{file}: no dynamic string table found"); return 5; }
        var offset = strtab + (long)entry.Value;
        var rpath = elf.CString(offset);
        var changed = false;
        if (convert && entry.Tag == ElfKit.DT_RPATH)
        {
            entry.Tag = ElfKit.DT_RUNPATH;
            elf.WriteDyn(entry);
            changed = true;
            Console.WriteLine($"{file}: RPATH converted to RUNPATH");
        }
        Console.WriteLine($"{file}: {ElfKit.DynTagName(entry.Tag)}={rpath}");
        if (newPath != null)
        {
            var bytes = Encoding.UTF8.GetBytes(newPath);
            var room = Encoding.UTF8.GetByteCount(rpath);
            if (bytes.Length > room)
            {
                Console.Error.WriteLine($"new rpath '{newPath}' too large; maximum length {room}");
                return 7;
            }
            // Overwrite in place, zero-filling the rest of the old string (the string table can't grow).
            Array.Clear(elf.Data, (int)offset, room);
            bytes.CopyTo(elf.Data, offset);
            changed = true;
            Console.WriteLine($"{file}: new {ElfKit.DynTagName(entry.Tag)}: {newPath}");
        }
        if (changed && !Write(elf)) return 1;
        return 0;
    }

    // Drop every DT_RPATH/DT_RUNPATH entry, shifting the rest down and padding with DT_NULL.
    static int Delete(string file)
    {
        var elf = Open(file);
        if (elf == null) return 1;
        var dyns = elf.Dynamic();
        if (dyns.Count == 0) { Console.Error.WriteLine($"{file}: no dynamic section found"); return 1; }
        var kept = dyns.Where(d => d.Tag is not (ElfKit.DT_RPATH or ElfKit.DT_RUNPATH)).ToList();
        if (kept.Count == dyns.Count) return 0;
        for (var i = 0; i < dyns.Count; i++)
        {
            var slot = new ElfKit.Dyn { Offset = dyns[i].Offset };
            if (i < kept.Count) { slot.Tag = kept[i].Tag; slot.Value = kept[i].Value; }
            elf.WriteDyn(slot);
        }
        return Write(elf) ? 0 : 1;
    }

    static bool Write(ElfKit.Elf elf)
    {
        try { elf.Save(); return true; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"{elf.Path}: {e.Message}");
            return false;
        }
    }
}
