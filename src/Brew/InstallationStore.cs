using System.Text.Json;
using System.Text.RegularExpressions;

namespace Brew;

public sealed record InstalledKeg(string Path, Receipt Receipt);

public sealed class InstallationStore(Paths paths)
{
    public InstalledKeg[] All => Directory.Exists(paths.Cellar)
        ? Directory.EnumerateDirectories(paths.Cellar).SelectMany(r => Directory.EnumerateDirectories(r))
            .Where(k => !System.IO.Path.GetFileName(k).StartsWith('.'))
            .Where(k => File.Exists(System.IO.Path.Combine(k, "INSTALL_RECEIPT.json")))
            .Select(k => new InstalledKeg(k, JsonSerializer.Deserialize<Receipt>(File.ReadAllText(System.IO.Path.Combine(k, "INSTALL_RECEIPT.json")))))
            .OrderBy(k => k.Receipt.Name, StringComparer.Ordinal).ThenBy(k => k.Receipt.Version, VersionOrder.Instance).ToArray() : [];
    public InstalledKeg[] Kegs(string name) => All.Where(k => k.Receipt.Name == Paths.ValidateName(name.Split('/')[^1])).ToArray();
    public InstalledKeg Latest(string name) => Kegs(name).LastOrDefault();
    public string PinDirectory => System.IO.Path.Combine(paths.Prefix, "var", "homebrew", "pinned");
    public bool IsPinned(string name) => File.Exists(System.IO.Path.Combine(PinDirectory, Paths.ValidateName(name)));
    public void Pin(string name, bool unpin)
    {
        var keg = Latest(name);
        if (keg == null) throw new InvalidOperationException($"{name} not installed");
        var file = System.IO.Path.Combine(PinDirectory, name);
        if (unpin)
        {
            if (!File.Exists(file)) CommandOutput.Warning($"{name} not pinned");
            else File.Delete(file);
        }
        else if (File.Exists(file)) CommandOutput.Warning($"{name} already pinned");
        else { Directory.CreateDirectory(PinDirectory); File.WriteAllText(file, keg.Receipt.Version); }
    }
    public static void Save(InstalledKeg keg, Receipt receipt) => ReceiptFile.Write(System.IO.Path.Combine(keg.Path, "INSTALL_RECEIPT.json"), receipt);
}

public sealed class VersionOrder : IComparer<string>
{
    public static VersionOrder Instance { get; } = new();
    public int Compare(string left, string right)
    {
        if (left == right) return 0;
        var native = typeof(Receipt).Assembly.GetType("Brew.Ruby.Versions")?.GetMethod("Compare", [typeof(string), typeof(string)]);
        if (native != null) return (int)native.Invoke(null, [left, right]);
        var a = Regex.Matches(left ?? "", @"\d+|[a-zA-Z]+|_").Select(m => m.Value.ToLowerInvariant()).ToArray();
        var b = Regex.Matches(right ?? "", @"\d+|[a-zA-Z]+|_").Select(m => m.Value.ToLowerInvariant()).ToArray();
        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var x = i < a.Length ? a[i] : null; var y = i < b.Length ? b[i] : null;
            if (x == y) continue;
            if (x == null) return y == "_" || char.IsDigit(y[0]) ? -1 : 1;
            if (y == null) return x == "_" || char.IsDigit(x[0]) ? 1 : -1;
            if (System.Numerics.BigInteger.TryParse(x, out var xn) && System.Numerics.BigInteger.TryParse(y, out var yn))
            { var numeric = xn.CompareTo(yn); if (numeric != 0) return numeric; }
            else { var text = string.CompareOrdinal(x, y); if (text != 0) return text; }
        }
        return 0;
    }
}
