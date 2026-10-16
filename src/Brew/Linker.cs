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

    // Windows can't run a `#!` script by name, so each script in bin/ and sbin/ gets a name.cmd that
    // starts its interpreter. Paths are absolute (a linked .cmd's %~dp0 is the link's directory) and
    // bottle relocation rewrites them like any other text file.
    public static void WriteScriptLaunchers(string keg)
    {
        foreach (var dir in new[] { "bin", "sbin" }.Select(d => Path.Combine(keg, d)).Where(Directory.Exists))
            foreach (var script in Directory.GetFiles(dir).Where(f => Path.GetExtension(f).Length == 0 || !Path.GetExtension(f)[1..].All(char.IsLetter)))
            {
                if (new FileInfo(script).LinkTarget != null) continue;
                if (new[] { ".cmd", ".exe", ".bat" }.Any(e => File.Exists(script + e))) continue;
                string first;
                try
                {
                    using var reader = new StreamReader(script);
                    var head = new char[2];
                    if (reader.Read(head, 0, 2) != 2 || head[0] != '#' || head[1] != '!') continue;
                    first = reader.ReadLine()?.Trim() ?? "";
                }
                catch (IOException) { continue; }
                if (Launcher(first, script) is { } text) File.WriteAllText(script + ".cmd", text);
            }
    }

    static string Launcher(string shebang, string script)
    {
        var words = shebang.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (words.Count == 0) return null;
        if (words[0].EndsWith("/env", StringComparison.Ordinal))
        {
            words.RemoveAt(0);
            words.RemoveAll(w => w.StartsWith('-'));
            if (words.Count == 0) return null;
        }
        var interpreter = words[0];
        var extra = string.Join(" ", words.Skip(1).Select(w => "\"" + w + "\""));
        var name = Path.GetFileNameWithoutExtension(interpreter.Replace('\\', '/').Split('/')[^1]).ToLowerInvariant();
        // A brewed interpreter named by absolute path (rewrite_shebang): use it directly.
        if (System.Text.RegularExpressions.Regex.IsMatch(interpreter, @"^[A-Za-z]:[\\/]"))
        {
            var native = interpreter.Replace('/', '\\');
            var found = new[] { "", ".exe", ".cmd", ".bat" }.Select(e => native + e).FirstOrDefault(File.Exists) ?? native + ".exe";
            return $"@\"{found}\" {extra} \"{script}\" %*\r\n".Replace("  ", " ");
        }
        if (name is "sh" or "bash" or "dash" or "ksh")
            return "@echo off\r\nsetlocal\r\n"
                + "set \"BREW_SH=%HOMEBREW_MSYS2_ROOT%\\usr\\bin\\bash.exe\"\r\n"
                + "if not exist \"%BREW_SH%\" set \"BREW_SH=%ProgramFiles%\\Git\\bin\\bash.exe\"\r\n"
                + "if not exist \"%BREW_SH%\" set \"BREW_SH=bash\"\r\n"
                + $"\"%BREW_SH%\" {extra} \"{script}\" %*\r\n".Replace("  ", " ");
        if (name.StartsWith("python", StringComparison.Ordinal)) name = "python";
        return $"@{name} {extra} \"{script}\" %*\r\n".Replace("  ", " ");
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
                    throw new IOException(ConflictMessage(keg, source, destination));
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
            foreach (var top in TreeDirectories)
                if (Directory.Exists(Path.Combine(keg, top))) LinkTree(keg, Path.Combine(keg, top), links);
            return links;
        }
        catch { Unlink(links); throw; }
    }

    // Homebrew links etc, include, lib, sbin, share and Frameworks into the prefix. Here every
    // file gets its own symlink (directories are always created, never linked).
    static readonly string[] TreeDirectories = ["etc", "include", "lib", "sbin", "share", "Frameworks"];

    static bool SkipFile(string relative) =>
        relative is "lib/charset.alias" or "share/locale/locale.alias" or "share/info/dir"
        || System.Text.RegularExpressions.Regex.IsMatch(relative, @"^share/icons/.*/icon-theme\.cache$");

    void LinkTree(string keg, string directory, List<BinLink> links)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.Ordinal))
        {
            var info = new FileInfo(entry);
            if (Directory.Exists(entry) && info.LinkTarget == null) { LinkTree(keg, entry, links); continue; }
            var relative = Path.GetRelativePath(keg, entry).Replace(Path.DirectorySeparatorChar, '/');
            if (SkipFile(relative)) continue;
            var destination = Path.Combine(paths.Prefix, relative.Replace('/', Path.DirectorySeparatorChar));
            if (Exists(destination)) throw new IOException(ConflictMessage(keg, entry, destination));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (Directory.Exists(entry)) Directory.CreateSymbolicLink(destination, entry);
            else File.CreateSymbolicLink(destination, entry);
            links.Add(new(destination, entry, false));
        }
    }

    // Receipts are data: a link path must sit in prefix/bin or under one of the linked trees.
    bool IsLinkPath(string path)
    {
        var full = Path.GetFullPath(path);
        if (string.Equals(Path.GetDirectoryName(full), paths.Bin, StringComparison.OrdinalIgnoreCase)) return true;
        return TreeDirectories.Any(top => full.StartsWith(Path.Combine(paths.Prefix, top) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    void RemoveEmptyParents(string path)
    {
        var stop = TreeDirectories.Select(top => Path.Combine(paths.Prefix, top)).Append(paths.Bin).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var dir = Path.GetDirectoryName(Path.GetFullPath(path)); dir != null && !stop.Contains(dir); dir = Path.GetDirectoryName(dir))
        {
            if (!Directory.Exists(dir) || Directory.EnumerateFileSystemEntries(dir).Any()) break;
            Directory.Delete(dir);
        }
    }

    string ConflictMessage(string keg, string source, string destination)
    {
        var target = new FileInfo(destination).LinkTarget;
        var linkDirectory = Path.GetDirectoryName(destination);
        var sidecar = destination + ".brew-shim";
        var contents = File.Exists(sidecar) ? File.ReadAllText(sidecar) : null;
        var owner = new InstallationStore(paths).All.FirstOrDefault(k => k.Receipt.Links.Any(l =>
            string.Equals(l.Path, destination, StringComparison.OrdinalIgnoreCase)
            && (l.Shim ? IsOurShim(destination) && contents == l.Target + (l.Arguments.Length == 0 ? "" : "\n" + l.Arguments)
                : target != null && string.Equals(Path.GetFullPath(target, linkDirectory), l.Target, StringComparison.OrdinalIgnoreCase))))?.Receipt.Name;
        var suggestion = owner == null ? $"already exists. You may want to remove it:\n  rm '{destination}'\n"
            : $"is a symlink belonging to {owner}. You can unlink it:\n  brew unlink {owner}\n";
        return $"Could not symlink {Path.GetRelativePath(keg, source)}\nTarget {destination}\n{suggestion}\n"
            + $"To force the link and overwrite all conflicting files:\n  brew link --overwrite {Path.GetFileName(Path.GetDirectoryName(keg))}\n\n"
            + $"To list all files that would be deleted:\n  brew link --overwrite {Path.GetFileName(Path.GetDirectoryName(keg))} --dry-run\n";
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
            if (!IsLinkPath(link.Path)) throw new IOException("Invalid link path in install receipt.");
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
                && string.Equals(Path.GetFullPath(target, Path.GetDirectoryName(Path.GetFullPath(link.Path))), link.Target, StringComparison.OrdinalIgnoreCase))
                RemovalPreflight.Check(link.Path);
        }
    }

    public void Unlink(IEnumerable<BinLink> links)
    {
        foreach (var link in links)
        {
            // Receipts are data: never allow one to delete outside the linked directories or replace user files.
            if (!IsLinkPath(link.Path)) throw new IOException("Invalid link path in install receipt.");
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
                string.Equals(Path.GetFullPath(target, Path.GetDirectoryName(Path.GetFullPath(link.Path))), link.Target, StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.Exists(link.Path)) Directory.Delete(link.Path); else File.Delete(link.Path);
                RemoveEmptyParents(link.Path);
            }
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
