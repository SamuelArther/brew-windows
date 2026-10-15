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
}

public sealed class InstallContext
{
    public string Prefix { get; }
    public string BuildPath { get; }
    public string Bin => Path.Combine(Prefix, "bin");

    public InstallContext(string prefix, string buildPath)
    {
        Prefix = prefix;
        BuildPath = buildPath;
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
}
