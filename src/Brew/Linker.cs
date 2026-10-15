using System.Text;

namespace Brew;

public sealed record BinLink(string Path, string Target, bool Shim);

public sealed class Linker(Paths paths)
{
    public List<BinLink> Link(string keg)
    {
        Directory.CreateDirectory(paths.Bin);
        var links = new List<BinLink>();
        try
        {
            var bin = Path.Combine(keg, "bin");
            if (!Directory.Exists(bin)) return links;
            foreach (var source in Directory.GetFiles(bin).Order(StringComparer.Ordinal))
            {
                if (Path.GetExtension(source).Equals(".dll", StringComparison.OrdinalIgnoreCase)) continue;
                var destination = Path.Combine(paths.Bin, Path.GetFileName(source));
                var sidecar = destination + ".brew-shim";
                if (Exists(destination) || Exists(sidecar))
                    throw new IOException($"Could not symlink {Path.GetRelativePath(keg, source)}\nTarget {destination}\nalready exists.");
                bool shim = Path.GetExtension(source).Equals(".exe", StringComparison.OrdinalIgnoreCase)
                    && Directory.EnumerateFiles(bin).Any(f => Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase));
                if (shim)
                {
                    using var resource = typeof(Linker).Assembly.GetManifestResourceStream("shim.exe")
                        ?? throw new IOException("DLL executable linking requires the embedded shim. Build with LLVM-MinGW clang.");
                    using (var output = new FileStream(destination, FileMode.CreateNew)) resource.CopyTo(output);
                    links.Add(new(destination, source, true));
                    File.WriteAllText(sidecar, source, new UTF8Encoding(false));
                }
                else
                {
                    File.CreateSymbolicLink(destination, source);
                    links.Add(new(destination, source, false));
                }
            }
            return links;
        }
        catch { Unlink(links); throw; }
    }

    public void Unlink(IEnumerable<BinLink> links)
    {
        foreach (var link in links)
        {
            // Receipts are data: never allow one to delete outside prefix/bin or replace user files.
            if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(link.Path)), paths.Bin, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Invalid link path in install receipt.");
            if (link.Shim)
            {
                var sidecar = link.Path + ".brew-shim";
                if (File.Exists(sidecar) && File.ReadAllText(sidecar) == link.Target)
                {
                    File.Delete(link.Path);
                    File.Delete(sidecar);
                }
                else if (!Exists(sidecar) && File.Exists(link.Path))
                {
                    // A failed sidecar write during linking: delete only our exact embedded binary.
                    using var resource = typeof(Linker).Assembly.GetManifestResourceStream("shim.exe");
                    using var memory = new MemoryStream();
                    resource?.CopyTo(memory);
                    if (resource != null && File.ReadAllBytes(link.Path).AsSpan().SequenceEqual(memory.ToArray()))
                        File.Delete(link.Path);
                }
            }
            else if (new FileInfo(link.Path).LinkTarget is string target &&
                string.Equals(Path.GetFullPath(target, paths.Bin), link.Target, StringComparison.OrdinalIgnoreCase))
                File.Delete(link.Path);
        }
    }

    static bool Exists(string path) => File.Exists(path) || Directory.Exists(path) || new FileInfo(path).LinkTarget != null;
}
