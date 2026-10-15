using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Brew;

internal static class SharedRuntime
{
    static bool IsMsys(string keg) => Directory.Exists(Path.Combine(keg, "libexec", "usr", "bin"))
        || Directory.Exists(Path.Combine(keg, "libexec", "ucrt64", "bin"));
    static IEnumerable<string> Dlls(string keg) => Directory.EnumerateFiles(Path.Combine(keg, "libexec"), "*.dll",
        new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint });

    public static long Deduplicate(Paths paths, string keg)
    {
        if (!IsMsys(keg)) return 0;
        var candidates = new InstallationStore(paths).All.Where(k => k.Path != keg && IsMsys(k.Path))
            .SelectMany(k => Dlls(k.Path)).Where(f => !File.GetAttributes(f).HasFlag(FileAttributes.ReadOnly))
            .ToLookup(f => (Path.GetFileName(f).ToUpperInvariant(), new FileInfo(f).Length));
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string Hash(string path)
        {
            if (!hashes.TryGetValue(path, out var value))
            { using var stream = File.OpenRead(path); hashes[path] = value = Convert.ToHexStringLower(SHA256.HashData(stream)); }
            return value;
        }
        long saved = 0;
        foreach (var file in Dlls(keg))
        {
            if (File.GetAttributes(file).HasFlag(FileAttributes.ReadOnly)) continue;
            var info = new FileInfo(file);
            foreach (var candidate in candidates[(info.Name.ToUpperInvariant(), info.Length)])
            {
                if (Hash(file) != Hash(candidate)) continue;
                var temporary = file + "." + Guid.NewGuid().ToString("N") + ".hardlink";
                try
                {
                    if (!CreateHardLink(temporary, candidate, IntPtr.Zero)) continue;
                    File.Move(temporary, file, overwrite: true); saved += info.Length; break;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }
        return saved;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CreateHardLink(string link, string target, IntPtr security);
}
