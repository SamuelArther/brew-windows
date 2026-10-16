namespace Brew;

// Formulae write config to HOMEBREW_PREFIX/etc and state to HOMEBREW_PREFIX/var, outside the keg.
// Like Homebrew, files a build adds there are also kept in <keg>/.bottle so the bottle carries them,
// and pouring copies them back without overwriting what a user has changed (InstallRenamed).
public static class EtcVar
{
    static IEnumerable<string> Roots(string prefix) => new[] { "etc", "var" }.Select(d => Path.Combine(prefix, d)).Where(Directory.Exists);

    // brew's own working state (build trees, locks) lives in var/homebrew and is never a formula's.
    static bool Ours(string prefix, string file) =>
        file.StartsWith(Path.Combine(prefix, "var", "homebrew") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    static IEnumerable<string> Files(string prefix) => Roots(prefix)
        .SelectMany(r => Directory.EnumerateFiles(r, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 }))
        .Where(f => !Ours(prefix, f));

    public static HashSet<string> Snapshot(string prefix) => new(Files(prefix), StringComparer.OrdinalIgnoreCase);

    public static void Preserve(string prefix, string keg, HashSet<string> before)
    {
        foreach (var file in Files(prefix).Where(f => !before.Contains(f)))
        {
            var copy = Path.Combine(keg, ".bottle", Path.GetRelativePath(prefix, file));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy, true);
        }
    }

    public static void Restore(string prefix, string keg)
    {
        var saved = Path.Combine(keg, ".bottle");
        if (!Directory.Exists(saved)) return;
        foreach (var file in Directory.EnumerateFiles(saved, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(prefix, Path.GetRelativePath(saved, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (File.Exists(target))
            {
                if (File.ReadAllBytes(target).AsSpan().SequenceEqual(File.ReadAllBytes(file))) continue;
                target += ".default";
            }
            File.Copy(file, target, true);
        }
    }
}
