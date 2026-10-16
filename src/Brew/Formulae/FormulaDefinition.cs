namespace Brew.Formulae;

public sealed class FormulaDefinition
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Url { get; set; }
    public string Sha256 { get; set; }
    public string Description { get; set; }
    public string Homepage { get; set; }
    public string License { get; set; }
    public Action<InstallContext> Install { get; set; }
    public List<FormulaResource> Resources { get; set; } = [];
    public List<string> Dependencies { get; set; } = [];
    public BottleSpec Bottle { get; set; }
}

// `bottle do ... end`: root_url, rebuild, and sha256 per tag (cellar, checksum).
public sealed class BottleSpec
{
    public string RootUrl;
    public int Rebuild;
    public readonly Dictionary<string, (string Cellar, string Sha256)> Files = new();
}

public sealed record FormulaResource(string Name, string Url, string Sha256);
public sealed record CommandSpec(string Name, string Target, string[] Arguments);

public sealed class InstallContext
{
    public string Prefix { get; }
    public string BuildPath { get; }
    public string Bin => Path.Combine(Prefix, "bin");
    public List<CommandSpec> Commands { get; } = [];
    public IReadOnlyDictionary<string, string> Resources { get; }
    public string BrewPrefix { get; }
    public bool PouredFromBottle { get; init; }
    // Extra environment for `def install` (build dependencies on PATH, Go/Cargo caches).
    public Dictionary<string, string> BuildEnvironment { get; } = new(StringComparer.OrdinalIgnoreCase);

    public InstallContext(string prefix, string buildPath, IReadOnlyDictionary<string, string> resources = null, string brewPrefix = null)
    {
        Prefix = prefix;
        BuildPath = buildPath;
        Resources = resources ?? new Dictionary<string, string>();
        BrewPrefix = brewPrefix ?? Directory.GetParent(prefix)?.Parent?.Parent?.FullName;
    }

    public void InstallBin(string source, string destinationName = null)
    {
        var sourcePath = Path.GetFullPath(Path.Combine(BuildPath, source));
        if (!sourcePath.StartsWith(Path.GetFullPath(BuildPath) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Install source must be inside buildpath.");
        destinationName ??= Path.GetFileName(source);
        if (Path.GetFileName(destinationName) != destinationName || destinationName is "." or "..")
            throw new InvalidOperationException("Invalid bin destination name.");
        Directory.CreateDirectory(Bin);
        File.Copy(sourcePath, Path.Combine(Bin, destinationName));
    }

    public void InstallTree(string source = ".", string destination = "libexec") =>
        CopyTree(Inside(BuildPath, source), Inside(Prefix, destination));

    public void InstallResource(string name, string destination)
    {
        if (!Resources.TryGetValue(name, out var path)) throw new InvalidOperationException($"Unknown resource: {name}");
        CopyTree(path, Inside(Prefix, destination));
    }

    public void WindowsExe(string source, string name = null, params string[] arguments)
    {
        var target = InstalledPath(source);
        if (!File.Exists(target)) throw new IOException($"Installed command not found: {source}");
        name ??= Path.GetFileName(source);
        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name += ".exe";
        if (Path.GetFileName(name) != name || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new IOException("Invalid command name.");
        Commands.Add(new(name, target, arguments));
    }

    public void NodeScript(string script, string name)
    {
        var target = Path.Combine(BrewPrefix, "bin", "node.exe");
        if (!File.Exists(target)) throw new IOException("node must be installed before linking a Node.js script.");
        Commands.Add(new(name + ".exe", target, [Inside(Prefix, script)]));
    }

    public void WindowsCmd(string source, string name, string environmentName = null, string environmentPath = null)
    {
        var target = Inside(Prefix, source);
        if (!File.Exists(target)) throw new IOException($"Installed script not found: {source}");
        if (Path.GetFileName(name) != name || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new IOException("Invalid command name.");
        var environment = "";
        if (environmentName != null)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(environmentName, @"\A[A-Z_][A-Z0-9_]*\z"))
                throw new IOException("Invalid environment variable.");
            environment = $"if not defined {environmentName} set \"{environmentName}={Inside(Prefix, environmentPath).Replace("%", "%%")}\"\r\n";
        }
        Directory.CreateDirectory(Bin);
        File.WriteAllText(Path.Combine(Bin, name + ".cmd"), $"@echo off\r\nsetlocal DisableDelayedExpansion\r\n{environment}call \"{target.Replace("%", "%%")}\" %*\r\nexit /b %errorlevel%\r\n");
    }

    public void Msys2Install(string subdirectory = "")
    {
        InstallTree(".", string.IsNullOrEmpty(subdirectory) ? "libexec" : "libexec/" + subdirectory);
        foreach (var resource in Resources.Keys) InstallResource(resource, "libexec");
        var root = Path.Combine(Prefix, "libexec");
        Directory.CreateDirectory(Path.Combine(root, "tmp"));
        Directory.CreateDirectory(Path.Combine(root, "etc"));
        // MSYS2's standard layout aliases /bin to /usr/bin.
        if (Directory.Exists(Path.Combine(root, "usr", "bin")) && !Directory.Exists(Path.Combine(root, "bin")))
            Directory.CreateSymbolicLink(Path.Combine(root, "bin"), Path.Combine(root, "usr", "bin"));
    }

    public void InstallLibrary(string source = "libexec/ucrt64")
    {
        var root = Inside(Prefix, source);
        foreach (var name in new[] { "include", "lib", "bin", "share" })
            if (Directory.Exists(Path.Combine(root, name))) CopyTree(Path.Combine(root, name), Path.Combine(Prefix, name));
    }

    public void Msys2Bin(string source, string name = null)
    {
        name ??= Path.GetFileNameWithoutExtension(source);
        if (source.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) WindowsExe("libexec/" + source, name);
        else
        {
            var script = Inside(Prefix, "libexec/" + source);
            if (!File.Exists(script)) throw new IOException($"Installed MSYS2 script not found: {source}");
            var posixPath = "/" + Path.GetRelativePath(Path.Combine(Prefix, "libexec"), script).Replace('\\', '/');
            WindowsExe("libexec/usr/bin/bash.exe", name, posixPath);
        }
    }

    string InstalledPath(string source)
    {
        if (!source.Contains("**/")) return Inside(Prefix, source);
        var parts = source.Split("**/", 2);
        var matches = Directory.GetFiles(Inside(Prefix, parts[0]), parts[1], SearchOption.AllDirectories);
        if (matches.Length != 1) throw new IOException($"Expected one installed command matching {source}, found {matches.Length}.");
        return matches[0];
    }

    static string Inside(string root, string relative)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var path = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (path != fullRoot && !path.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Path must stay inside its installation directory.");
        return path;
    }

    static void CopyTree(string source, string destination, string root = null, HashSet<string> active = null)
    {
        root ??= Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
        active ??= new(StringComparer.OrdinalIgnoreCase);
        if (!active.Add(Path.GetFullPath(source))) throw new IOException("Circular directory link in archive.");
        try
        {
            Directory.CreateDirectory(destination);
            foreach (var entry in Directory.EnumerateFileSystemEntries(source))
            {
                var name = Path.GetFileName(entry);
                if (name is ".resources" or ".PKGINFO" or ".MTREE" or ".INSTALL" or ".BUILDINFO") continue;
                var target = Path.Combine(destination, name);
                var info = new FileInfo(entry);
                if (info.LinkTarget != null)
                {
                    var resolved = info.ResolveLinkTarget(true)?.FullName ?? throw new IOException($"Broken archive link: {entry}");
                    if (!resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && resolved != root)
                        throw new IOException($"Archive link escapes its package: {entry}");
                    if (Directory.Exists(resolved)) CopyTree(resolved, target, root, active);
                    else File.Copy(resolved, target, overwrite: true);
                }
                else if (Directory.Exists(entry)) CopyTree(entry, target, root, active);
                else File.Copy(entry, target, overwrite: true);
            }
        }
        finally { active.Remove(Path.GetFullPath(source)); }
    }
}
