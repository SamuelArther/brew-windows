using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Brew;

// Bottles: prebuilt kegs packed as <name>--<pkg_version>.x86_64_windows.bottle[.N].tar.gz.
// Text files that mention the build prefix carry @@HOMEBREW_PREFIX@@ and are rewritten on pour.
public static class Bottles
{
    public const string Tag = "x86_64_windows";
    public const string Placeholder = "@@HOMEBREW_PREFIX@@";
    public const string DefaultDomain = "https://github.com/SamuelArther/brew-windows/releases/download";
    const long MaxTextFile = 32 * 1024 * 1024;

    public static string Domain =>
        (Environment.GetEnvironmentVariable("HOMEBREW_BOTTLE_DOMAIN") is { Length: > 0 } d ? d : DefaultDomain).TrimEnd('/');

    public static string FileName(string name, string pkgVersion, int rebuild) =>
        $"{name}--{pkgVersion}.{Tag}.bottle{(rebuild > 0 ? "." + rebuild : "")}.tar.gz";

    // GitHub Releases layout: one release per formula version, tagged <name>-<pkg_version>.
    public static string ReleaseTag(string name, string pkgVersion) => $"{name}-{pkgVersion}";

    public sealed record Bottle(string Url, string Sha256, string Cellar, string FileName);

    public static Bottle For(Brew.Formulae.FormulaDefinition formula)
    {
        if (formula?.Bottle == null || !formula.Bottle.Files.TryGetValue(Tag, out var file)) return null;
        var pkgVersion = Metadata.Version(formula);
        var fileName = FileName(formula.Name, pkgVersion, formula.Bottle.Rebuild);
        var root = formula.Bottle.RootUrl?.TrimEnd('/') ?? $"{Domain}/{ReleaseTag(formula.Name, pkgVersion)}";
        return new Bottle($"{root}/{fileName}", file.Sha256, file.Cellar, fileName);
    }

    // Packs an installed keg. Returns the cellar value for the bottle DSL.
    public static async Task<string> CreateAsync(Paths paths, string keg, string output)
    {
        var name = Path.GetFileName(Path.GetDirectoryName(keg));
        var version = Path.GetFileName(keg);
        var staging = Path.Combine(paths.Prefix, "var", "homebrew", "bottle", Guid.NewGuid().ToString("N"));
        var stagedKeg = Path.Combine(staging, name, version);
        try
        {
            CopyTree(keg, stagedKeg);
            var relocatable = true;
            foreach (var file in Directory.EnumerateFiles(stagedKeg, "*", SearchOption.AllDirectories))
            {
                var bookkeeping = Path.GetFileName(file) == "INSTALL_RECEIPT.json" || file.EndsWith(".brew-shim", StringComparison.OrdinalIgnoreCase);
                if (bookkeeping)
                {
                    Relocate(file, paths.Prefix, true);
                    continue;
                }
                if (Relocate(file, paths.Prefix, true) || RelocateLauncher(file, paths.Prefix, true)) relocatable = false;
                else if (ContainsBytes(file, paths.Prefix)) relocatable = false; // binary with an embedded prefix
            }
            var receipt = Path.Combine(stagedKeg, "INSTALL_RECEIPT.json");
            if (File.Exists(receipt))
            {
                var r = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(receipt));
                ReceiptFile.Write(receipt, r with { Links = [], PouredFromBottle = false, InstalledOnRequest = false });
            }
            if (File.Exists(output)) File.Delete(output);
            var tar = await ProcessRunner.Capture("tar.exe", ["-czf", output, "-C", staging, name + "/" + version]);
            if (tar.Code != 0) throw new IOException("Bottle archive creation failed: " + tar.Error.Trim());
            return relocatable ? "any_skip_relocation" : "any";
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
    }

    public static async Task PourAsync(Paths paths, string archive, string keg, string cellar)
    {
        var name = Path.GetFileName(Path.GetDirectoryName(keg));
        var version = Path.GetFileName(keg);
        var staging = Path.Combine(paths.Prefix, "var", "homebrew", "pour", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var tar = await ProcessRunner.Capture("tar.exe", ["-xzf", archive, "-C", staging]);
            if (tar.Code != 0) throw new IOException("Bottle extraction failed: " + tar.Error.Trim());
            var extracted = Path.Combine(staging, name, version);
            if (!Directory.Exists(extracted)) throw new IOException($"Bottle does not contain {name}/{version}");
            foreach (var file in Directory.EnumerateFiles(extracted, "*", SearchOption.AllDirectories))
            {
                var bookkeeping = Path.GetFileName(file) == "INSTALL_RECEIPT.json" || file.EndsWith(".brew-shim", StringComparison.OrdinalIgnoreCase);
                if (bookkeeping || cellar != "any_skip_relocation")
                    if (!Relocate(file, paths.Prefix, false)) RelocateLauncher(file, paths.Prefix, false);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(keg)!);
            Directory.Move(extracted, keg);
        }
        finally { Directory.Delete(staging, true); }
    }

    public static async Task<string> Sha256Async(string file)
    {
        await using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream));
    }

    // Prefix spellings, most specific first: JSON-escaped, native, forward-slash.
    static (string Path, string Placeholder)[] Forms(string prefix) =>
    [
        (prefix.Replace("\\", "\\\\"), "@@HOMEBREW_PREFIX_ESCAPED@@"),
        (prefix, Placeholder),
        (prefix.Replace('\\', '/'), "@@HOMEBREW_PREFIX_POSIX@@"),
    ];

    // Rewrites prefix <-> placeholders in a text file. Returns whether anything changed.
    static bool Relocate(string file, string prefix, bool toPlaceholder)
    {
        var info = new FileInfo(file);
        if (info.LinkTarget != null || info.Length > MaxTextFile || !IsText(file)) return false;
        var text = File.ReadAllText(file);
        var updated = text;
        foreach (var (path, placeholder) in toPlaceholder ? Forms(prefix) : Forms(prefix))
            updated = toPlaceholder ? updated.Replace(path, placeholder, StringComparison.OrdinalIgnoreCase)
                : updated.Replace(placeholder, path, StringComparison.Ordinal);
        if (updated == text) return false;
        File.WriteAllText(file, updated, new UTF8Encoding(false));
        return true;
    }

    // Python entry-point launchers (distlib) are an exe, a "#!<python>\n" line, then a zip. Only that
    // line names the prefix; distlib locates the zip from the end, so the line may change length.
    static bool RelocateLauncher(string file, string prefix, bool toPlaceholder)
    {
        if (!file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || new FileInfo(file).LinkTarget != null) return false;
        var data = File.ReadAllBytes(file);
        var zip = data.AsSpan().IndexOf("\nPK\u0003\u0004"u8);
        if (zip < 0) return false;
        var start = data.AsSpan(0, zip).LastIndexOf("#!"u8);
        if (start < 0 || zip - start > 4096 || data.AsSpan(start, zip - start).Contains((byte)0)) return false;
        var line = Encoding.UTF8.GetString(data, start, zip - start);
        var updated = line;
        foreach (var (path, placeholder) in Forms(prefix))
            updated = toPlaceholder ? updated.Replace(path, placeholder, StringComparison.OrdinalIgnoreCase)
                : updated.Replace(placeholder, path, StringComparison.Ordinal);
        if (updated == line) return false;
        var output = new MemoryStream();
        output.Write(data, 0, start);
        output.Write(Encoding.UTF8.GetBytes(updated));
        output.Write(data, zip, data.Length - zip);
        File.WriteAllBytes(file, output.ToArray());
        return true;
    }

    static bool IsText(string file)
    {
        Span<byte> head = stackalloc byte[8192];
        using var s = File.OpenRead(file);
        var n = s.Read(head);
        return !head[..n].Contains((byte)0);
    }

    static bool ContainsBytes(string file, string value)
    {
        if (new FileInfo(file).Length > 256 * 1024 * 1024) return false;
        var data = File.ReadAllBytes(file);
        foreach (var form in new[] { value, value.Replace('\\', '/') })
        {
            if (data.AsSpan().IndexOf(Encoding.UTF8.GetBytes(form)) >= 0) return true;
            if (data.AsSpan().IndexOf(Encoding.Unicode.GetBytes(form)) >= 0) return true;
        }
        return false;
    }

    static void CopyTree(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var entry in Directory.EnumerateFileSystemEntries(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(entry));
            var info = new FileInfo(entry);
            if (info.LinkTarget != null)
            {
                if (Directory.Exists(entry)) Directory.CreateSymbolicLink(target, info.LinkTarget);
                else File.CreateSymbolicLink(target, info.LinkTarget);
            }
            else if (Directory.Exists(entry)) CopyTree(entry, target);
            else File.Copy(entry, target);
        }
    }
}
