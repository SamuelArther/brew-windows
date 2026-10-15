namespace Brew;

public sealed class OptLinks(Paths paths)
{
    public string PathFor(string name) => Path.Combine(paths.Prefix, "opt", Paths.ValidateName(name));
    public void Set(string name, string keg)
    {
        var link = PathFor(name); Directory.CreateDirectory(Path.GetDirectoryName(link));
        var target = new DirectoryInfo(link).LinkTarget;
        if (target != null)
        {
            var existing = Path.GetFullPath(target, Path.GetDirectoryName(link));
            if (string.Equals(existing, keg, StringComparison.OrdinalIgnoreCase)) return;
            if (!existing.StartsWith(paths.Rack(name) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"Cannot create opt link: {link} already exists.");
            Directory.Delete(link);
        }
        else if (Directory.Exists(link) || File.Exists(link)) throw new IOException($"Cannot create opt link: {link} already exists.");
        Directory.CreateSymbolicLink(link, keg);
    }
    public void Remove(string name, string keg)
    {
        var link = PathFor(name); var target = new DirectoryInfo(link).LinkTarget;
        if (target != null && string.Equals(Path.GetFullPath(target, Path.GetDirectoryName(link)), keg, StringComparison.OrdinalIgnoreCase)) Directory.Delete(link);
    }
}
