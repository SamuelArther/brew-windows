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
    // Bottles live in their own repository's releases (the brew-windows repository keeps brew's own releases).
    public const string DefaultDomain = "https://github.com/SamuelArther/brew-windows-bottles/releases/download";
    // Where bottles were published before that: still tried when the default domain doesn't have the file yet.
    public const string LegacyDomain = "https://github.com/SamuelArther/brew-windows/releases/download";
    const long MaxTextFile = 32 * 1024 * 1024;

    public static string Domain =>
        (Environment.GetEnvironmentVariable("HOMEBREW_BOTTLE_DOMAIN") is { Length: > 0 } d ? d : DefaultDomain).TrimEnd('/');

    public static string FileName(string name, string pkgVersion, int rebuild) =>
        $"{name}--{pkgVersion}.{Tag}.bottle{(rebuild > 0 ? "." + rebuild : "")}.tar.gz";

    // GitHub Releases layout: one release per formula version, tagged <name>-<pkg_version>.
    public static string ReleaseTag(string name, string pkgVersion) => $"{name}-{pkgVersion}";

    public sealed record Bottle(string Url, string Sha256, string Cellar, string FileName, string[] Mirrors);

    public static Bottle For(Brew.Formulae.FormulaDefinition formula)
    {
        if (formula?.Bottle == null || !formula.Bottle.Files.TryGetValue(Tag, out var file)) return null;
        var pkgVersion = Metadata.Version(formula);
        var fileName = FileName(formula.Name, pkgVersion, formula.Bottle.Rebuild);
        var tag = ReleaseTag(formula.Name, pkgVersion);
        var root = formula.Bottle.RootUrl?.TrimEnd('/') ?? $"{Domain}/{tag}";
        string[] mirrors = formula.Bottle.RootUrl == null && Domain == DefaultDomain ? [$"{LegacyDomain}/{tag}/{fileName}"] : [];
        return new Bottle($"{root}/{fileName}", file.Sha256, file.Cellar, fileName, mirrors);
    }

    // Packs an installed keg. Returns the cellar value for the bottle DSL.
    public static async Task<string> CreateAsync(Paths paths, string keg, string output)
    {
        var name = Path.GetFileName(Path.GetDirectoryName(keg));
        var version = Path.GetFileName(keg);
        var staging = Path.Combine(paths.Prefix, "var", "homebrew", "bottle", Guid.NewGuid().ToString("N")[..8]);
        var stagedKeg = Path.Combine(staging, name, version);
        try
        {
            CopyTree(keg, stagedKeg);
            File.Delete(Path.Combine(stagedKeg, PouredReceiptName));
            var relocatable = true;
            foreach (var file in Paths.TreeFiles(stagedKeg))
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
        finally { PackageManager.DeleteTree(staging); }
    }

    public const string PouredReceiptName = ".brew-bottle-receipt.json";

    public static async Task PourAsync(Paths paths, string archive, string keg, string cellar)
    {
        var name = Path.GetFileName(Path.GetDirectoryName(keg));
        var version = Path.GetFileName(keg);
        var staging = Path.Combine(paths.Prefix, "var", "homebrew", "pour", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(staging);
        try
        {
            var tar = await ProcessRunner.Capture("tar.exe", ["-xzf", archive, "-C", staging]);
            if (tar.Code != 0) throw new IOException("Bottle extraction failed: " + tar.Error.Trim());
            var extracted = Path.Combine(staging, name, version);
            if (!Directory.Exists(extracted)) throw new IOException($"Bottle does not contain {name}/{version}");
            foreach (var file in Paths.TreeFiles(extracted))
            {
                var bookkeeping = Path.GetFileName(file) == "INSTALL_RECEIPT.json" || file.EndsWith(".brew-shim", StringComparison.OrdinalIgnoreCase);
                if (bookkeeping || cellar != "any_skip_relocation")
                    if (!Relocate(file, paths.Prefix, false)) RelocateLauncher(file, paths.Prefix, false);
            }
            // The bottle's receipt moves aside: a keg only counts as installed once its own receipt is
            // written, last (a keg without a valid tab isn't installed, as in Homebrew).
            var receipt = Path.Combine(extracted, "INSTALL_RECEIPT.json");
            if (File.Exists(receipt)) File.Move(receipt, Path.Combine(extracted, PouredReceiptName));
            Directory.CreateDirectory(Path.GetDirectoryName(keg)!);
            Directory.Move(extracted, keg);
        }
        finally { PackageManager.DeleteTree(staging); }
    }

    public static async Task<string> Sha256Async(string file)
    {
        await using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream));
    }

    // Relocation edits bytes, like Homebrew's: files are read as Latin-1 (one char per byte) and the
    // prefix is matched by its UTF-8 bytes, so text in any encoding (or a BOM) is otherwise untouched.
    static readonly Encoding Bytes = Encoding.Latin1;

    // Prefix spellings, most specific first: JSON-escaped, native, forward-slash.
    static (string Path, string Placeholder)[] Forms(string prefix) =>
    [
        (Raw(prefix.Replace("\\", "\\\\")), "@@HOMEBREW_PREFIX_ESCAPED@@"),
        (Raw(prefix), Placeholder),
        (Raw(prefix.Replace('\\', '/')), "@@HOMEBREW_PREFIX_POSIX@@"),
    ];
    static string Raw(string text) => Bytes.GetString(Encoding.UTF8.GetBytes(text));

    // Rewrites prefix <-> placeholders in a text file. Returns whether anything changed.
    public static bool Relocate(string file, string prefix, bool toPlaceholder)
    {
        var info = new FileInfo(file);
        if (info.LinkTarget != null || info.Length > MaxTextFile || !IsText(file)) return false;
        var text = Bytes.GetString(File.ReadAllBytes(file));
        var updated = text;
        foreach (var (path, placeholder) in Forms(prefix))
            updated = toPlaceholder ? updated.Replace(path, placeholder, StringComparison.OrdinalIgnoreCase)
                : updated.Replace(placeholder, path, StringComparison.Ordinal);
        if (updated == text) return false;
        Writable(file);
        File.WriteAllBytes(file, Bytes.GetBytes(updated));
        return true;
    }

    // Python entry-point launchers (distlib) are an exe, a "#!<python>\n" line, then a zip. Only that
    // line names the prefix; distlib locates the zip from the end, so the line may change length.
    public static bool RelocateLauncher(string file, string prefix, bool toPlaceholder)
    {
        if (!file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || new FileInfo(file).LinkTarget != null) return false;
        var data = File.ReadAllBytes(file);
        var zip = data.AsSpan().IndexOf("\nPK\u0003\u0004"u8);
        if (zip < 0) return false;
        var start = data.AsSpan(0, zip).LastIndexOf("#!"u8);
        if (start < 0 || zip - start > 4096 || data.AsSpan(start, zip - start).Contains((byte)0)) return false;
        var line = Bytes.GetString(data, start, zip - start);
        var updated = line;
        foreach (var (path, placeholder) in Forms(prefix))
            updated = toPlaceholder ? updated.Replace(path, placeholder, StringComparison.OrdinalIgnoreCase)
                : updated.Replace(placeholder, path, StringComparison.Ordinal);
        if (updated == line) return false;
        var output = new MemoryStream();
        output.Write(data, 0, start);
        output.Write(Bytes.GetBytes(updated));
        output.Write(data, zip, data.Length - zip);
        Writable(file);
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

    // An absolute link into the keg (Directory.CreateSymbolicLink(root/"bin", root/"usr/bin")) is made
    // relative: copied as is it would name this machine's Cellar, and point the staged tree back at it.
    static void CopyTree(string source, string destination, string keg = null)
    {
        keg ??= Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
        Directory.CreateDirectory(destination);
        foreach (var entry in Directory.EnumerateFileSystemEntries(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(entry));
            var info = new FileInfo(entry);
            if (info.LinkTarget is { } link)
            {
                var resolved = Path.GetFullPath(link, Path.GetDirectoryName(entry)!);
                if (Path.IsPathRooted(link) && (resolved.Equals(keg, StringComparison.OrdinalIgnoreCase)
                    || resolved.StartsWith(keg + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                    link = Path.GetRelativePath(Path.GetDirectoryName(entry)!, resolved);
                if (Directory.Exists(entry)) Directory.CreateSymbolicLink(target, link);
                else File.CreateSymbolicLink(target, link);
            }
            else if (Directory.Exists(entry)) CopyTree(entry, target, keg);
            else File.Copy(entry, target);
        }
    }

    // A keg file installed read-only (install -m 444) still has to take relocated paths.
    static void Writable(string file)
    {
        var attributes = File.GetAttributes(file);
        if (attributes.HasFlag(FileAttributes.ReadOnly)) File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
    }
}
