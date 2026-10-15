using System.Reflection;
using System.Text.Json;
using Brew.Formulae;

namespace Brew;

public sealed record Receipt(string Name, string Version, string Url, string Sha256, List<BinLink> Links);

public sealed class PackageManager(Paths paths)
{
    const string ReceiptName = "INSTALL_RECEIPT.json";
    readonly Linker linker = new(paths);

    public async Task InstallAsync(string name)
    {
        var file = paths.Formula(name);
        if (!File.Exists(file)) throw new InvalidOperationException($"No available formula with the name \"{name}\".");
        var type = typeof(FormulaDefinition).Assembly.GetType("Brew.Ruby.FormulaLoader")
            ?? throw new InvalidOperationException("Ruby FormulaLoader is not available; see docs/FORMULA_API.md.");
        FormulaDefinition formula;
        try { formula = (FormulaDefinition)type.GetMethod("Load", [typeof(string)]).Invoke(null, [file]); }
        catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
        if (formula == null || formula.Name != name || formula.Install == null)
            throw new InvalidOperationException("Invalid formula definition; see docs/FORMULA_API.md.");
        if (string.IsNullOrEmpty(formula.Version) || formula.Version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || formula.Version is "." or "..") throw new InvalidOperationException("Invalid formula version.");
        var rack = paths.Rack(name);
        var keg = Path.Combine(rack, formula.Version);
        if (File.Exists(Path.Combine(keg, ReceiptName)))
        {
            Console.Error.WriteLine($"Warning: {name} {formula.Version} is already installed and up-to-date.\nTo reinstall {formula.Version}, run:\n  brew reinstall {name}");
            return;
        }
        if (Directory.Exists(keg)) throw new IOException($"Incomplete keg already exists: {keg}");
        var download = await new Downloader(paths.Cache).FetchAsync(formula.Url, formula.Sha256);
        var build = Path.Combine(paths.Prefix, "var", "homebrew", "build", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(build);
        var createdKeg = false;
        List<BinLink> links = [];
        try
        {
            var filename = Path.GetFileName(new Uri(formula.Url).LocalPath);
            File.Copy(download, Path.Combine(build, filename));
            Console.WriteLine($"==> Installing {name}");
            Directory.CreateDirectory(keg);
            createdKeg = true;
            formula.Install(new InstallContext(keg, build));
            links = linker.Link(keg);
            File.WriteAllText(Path.Combine(keg, ReceiptName), JsonSerializer.Serialize(
                new Receipt(name, formula.Version, formula.Url, formula.Sha256, links), new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"{keg}: {Abbreviation(keg)}");
        }
        catch
        {
            linker.Unlink(links);
            if (createdKeg) Directory.Delete(keg, true);
            RemoveEmptyRack(rack);
            throw;
        }
        finally { Directory.Delete(build, true); }
    }

    public void List(string[] names, bool versions)
    {
        if (names.Length == 0)
        {
            if (!Directory.Exists(paths.Cellar)) return;
            names = Directory.GetDirectories(paths.Cellar).Select(Path.GetFileName)
                .Where(n => InstalledKegs(n).Length > 0).Order(StringComparer.Ordinal).ToArray();
            foreach (var name in names)
                Console.WriteLine(versions ? name + " " + string.Join(" ", InstalledKegs(name).Select(Path.GetFileName)) : name);
            return;
        }
        foreach (var name in names)
        {
            var kegs = RequireKegs(name);
            if (versions) Console.WriteLine(name + " " + string.Join(" ", kegs.Select(Path.GetFileName)));
            else foreach (var keg in kegs)
                foreach (var file in Directory.EnumerateFiles(keg, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                    if (Path.GetFileName(file) != ReceiptName) Console.WriteLine(file);
        }
    }

    public void Uninstall(string name, bool force)
    {
        var kegs = InstalledKegs(name);
        if (kegs.Length == 0 && !force) throw new InvalidOperationException($"No such keg: {paths.Rack(name)}");
        foreach (var keg in kegs)
        {
            var receipt = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(Path.Combine(keg, ReceiptName)))
                ?? throw new IOException($"Invalid install receipt: {keg}");
            Console.WriteLine($"Uninstalling {keg}... ({Abbreviation(keg)})");
            linker.Unlink(receipt.Links);
            Directory.Delete(keg, true);
        }
        RemoveEmptyRack(paths.Rack(name));
    }

    public void Link(string name, bool unlink)
    {
        foreach (var keg in RequireKegs(name))
        {
            var receiptPath = Path.Combine(keg, ReceiptName);
            var receipt = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(receiptPath));
            if (unlink)
            {
                linker.Unlink(receipt.Links);
                Console.WriteLine($"Unlinking {keg}... {receipt.Links.Count} symlinks removed.");
                receipt = receipt with { Links = [] };
            }
            else
            {
                if (receipt.Links.Count > 0) { Console.Error.WriteLine($"Warning: Already linked: {keg}"); continue; }
                receipt = receipt with { Links = linker.Link(keg) };
                Console.WriteLine($"Linking {keg}... {receipt.Links.Count} symlinks created.");
            }
            File.WriteAllText(receiptPath, JsonSerializer.Serialize(receipt));
        }
    }

    string[] RequireKegs(string name)
    {
        var kegs = InstalledKegs(name);
        if (kegs.Length == 0) throw new InvalidOperationException($"No such keg: {paths.Rack(name)}");
        return kegs;
    }

    string[] InstalledKegs(string name) => Directory.Exists(paths.Rack(name))
        ? Directory.GetDirectories(paths.Rack(name)).Where(k => File.Exists(Path.Combine(k, ReceiptName))).Order(StringComparer.Ordinal).ToArray() : [];

    static void RemoveEmptyRack(string rack)
    {
        if (Directory.Exists(rack) && !Directory.EnumerateFileSystemEntries(rack).Any()) Directory.Delete(rack);
    }

    static string Abbreviation(string keg)
    {
        var files = Directory.GetFiles(keg, "*", SearchOption.AllDirectories);
        double bytes = files.Sum(f => new FileInfo(f).Length);
        var units = new[] { "B", "KB", "MB", "GB" };
        var unit = 0;
        while (Math.Round(bytes, 1) >= 1000 && unit < units.Length - 1) { bytes /= 1000; unit++; }
        return (files.Length > 1 ? $"{files.Length:N0} files, " : "") + $"{bytes:0.#}{units[unit]}";
    }
}
