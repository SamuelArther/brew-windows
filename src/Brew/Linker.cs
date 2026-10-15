using System.Text;
using Brew.Formulae;

namespace Brew;

public sealed record BinLink(string Path, string Target, bool Shim, string Arguments = "");

public sealed class Linker(Paths paths)
{
    public void MaterializeCommands(string keg, IEnumerable<CommandSpec> commands)
    {
        var bin = Path.Combine(keg, "bin");
        foreach (var command in commands)
        {
            if (Path.GetFileName(command.Name) != command.Name) throw new IOException("Invalid command name.");
            var destination = Path.Combine(bin, command.Name);
            if (string.Equals(destination, command.Target, StringComparison.OrdinalIgnoreCase) && command.Arguments.Length == 0) continue;
            Directory.CreateDirectory(bin);
            if (Exists(destination) || Exists(destination + ".brew-shim"))
                throw new IOException($"Installed command already exists: {destination}");
            WriteShim(destination, command.Target, string.Join(" ", command.Arguments.Select(QuoteArgument)));
        }
    }

    public List<BinLink> Link(string keg, IEnumerable<CommandSpec> commands = null)
    {
        Directory.CreateDirectory(paths.Bin);
        var links = new List<BinLink>();
        try
        {
            var bin = Path.Combine(keg, "bin");
            var registered = (commands ?? []).ToArray();
            var names = registered.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var entries = (Directory.Exists(bin) ? Directory.GetFiles(bin) : [])
                .Where(f => !Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                .Where(f => !f.EndsWith(".brew-shim", StringComparison.OrdinalIgnoreCase) && !names.Contains(Path.GetFileName(f)))
                .Select(f => new CommandSpec(Path.GetFileName(f), f, []))
                .Concat(registered).OrderBy(c => c.Name, StringComparer.Ordinal);
            foreach (var command in entries)
            {
                var source = command.Target;
                if (Path.GetFileName(command.Name) != command.Name) throw new IOException("Invalid command name.");
                var destination = Path.Combine(paths.Bin, command.Name);
                var sidecar = destination + ".brew-shim";
                if (Exists(destination) || Exists(sidecar))
                    throw new IOException($"Could not symlink {Path.GetRelativePath(keg, source)}\nTarget {destination}\nalready exists.");
                bool shim = command.Arguments.Length > 0 || !string.Equals(Path.GetDirectoryName(source), bin, StringComparison.OrdinalIgnoreCase)
                    || Path.GetExtension(source).Equals(".exe", StringComparison.OrdinalIgnoreCase)
                    && Directory.EnumerateFiles(bin).Any(f => Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase));
                if (shim)
                {
                    var arguments = string.Join(" ", command.Arguments.Select(QuoteArgument));
                    links.Add(new(destination, source, true, arguments));
                    WriteShim(destination, source, arguments);
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

    static void WriteShim(string destination, string source, string arguments)
    {
        using var resource = typeof(Linker).Assembly.GetManifestResourceStream("shim.exe")
            ?? throw new IOException("DLL executable linking requires the embedded shim. Build with LLVM-MinGW clang.");
        using (var output = new FileStream(destination, FileMode.CreateNew)) resource.CopyTo(output);
        File.WriteAllText(destination + ".brew-shim", source + (arguments.Length == 0 ? "" : "\n" + arguments), new UTF8Encoding(false));
    }

    public void PreflightUnlink(IEnumerable<BinLink> links)
    {
        foreach (var link in links)
        {
            if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(link.Path)), paths.Bin, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Invalid link path in install receipt.");
            if (link.Shim)
            {
                var sidecar = link.Path + ".brew-shim";
                if (IsOurShim(link.Path) && (!Exists(sidecar)
                    || File.ReadAllText(sidecar) == link.Target + (link.Arguments.Length == 0 ? "" : "\n" + link.Arguments)))
                {
                    RemovalPreflight.Check(link.Path);
                    if (File.Exists(sidecar)) RemovalPreflight.Check(sidecar);
                }
            }
            else if (new FileInfo(link.Path).LinkTarget is string target
                && string.Equals(Path.GetFullPath(target, paths.Bin), link.Target, StringComparison.OrdinalIgnoreCase))
                RemovalPreflight.Check(link.Path);
        }
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
                if (File.Exists(sidecar) && File.ReadAllText(sidecar) == link.Target + (link.Arguments.Length == 0 ? "" : "\n" + link.Arguments))
                {
                    if (IsOurShim(link.Path))
                    {
                        File.Delete(link.Path);
                        File.Delete(sidecar);
                    }
                    else if (!Exists(link.Path)) File.Delete(sidecar);
                }
                else if (!Exists(sidecar) && File.Exists(link.Path))
                {
                    // A failed sidecar write during linking: delete only our exact embedded binary.
                    if (IsOurShim(link.Path)) File.Delete(link.Path);
                }
            }
            else if (new FileInfo(link.Path).LinkTarget is string target &&
                string.Equals(Path.GetFullPath(target, paths.Bin), link.Target, StringComparison.OrdinalIgnoreCase))
                File.Delete(link.Path);
        }
    }

    static string QuoteArgument(string value)
    {
        if (value.Contains('\n') || value.Contains('\r')) throw new IOException("Command arguments cannot contain newlines.");
        var result = new StringBuilder("\"");
        var slashes = 0;
        foreach (var c in value)
        {
            if (c == '\\') { slashes++; continue; }
            result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes).Append(c);
            slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('"').ToString();
    }

    static bool IsOurShim(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).LinkTarget != null) return false;
        using var resource = typeof(Linker).Assembly.GetManifestResourceStream("shim.exe");
        using var memory = new MemoryStream();
        resource?.CopyTo(memory);
        return resource != null && File.ReadAllBytes(path).AsSpan().SequenceEqual(memory.ToArray());
    }

    static bool Exists(string path) => File.Exists(path) || Directory.Exists(path) || new FileInfo(path).LinkTarget != null;
}
