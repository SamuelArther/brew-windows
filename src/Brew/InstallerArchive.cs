using System.IO.Compression;
using System.Text;

namespace Brew;

internal static class InstallerArchive
{
    static readonly byte[] Inno = Encoding.ASCII.GetBytes("Inno Setup Setup Data (");
    static readonly byte[] Nsis = Encoding.ASCII.GetBytes("NullsoftInst");

    public static async Task<bool> Extract(string file, string destination)
    {
        var kind = Detect(file);
        if (kind == null) return false;
        var tool = await Tool(kind);
        var args = kind == "innoextract"
            ? new[] { "--extract", "--silent", "--output-dir", destination, "--", file }
            : new[] { "x", "-y", "-bd", "-bso0", "-o" + destination, "--", file };
        var result = await ProcessRunner.Capture(tool, args);
        if (result.Code != 0) throw new IOException($"Could not extract {kind} installer: {result.Error.Trim()} {result.Output.Trim()}");
        if (!Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories).Any()) throw new IOException("Installer contains no extractable files.");
        return true;
    }

    static string Detect(string file)
    {
        using var stream = File.OpenRead(file);
        var buffer = new byte[65536 + Inno.Length]; var carry = 0;
        while (true)
        {
            var read = stream.Read(buffer, carry, 65536);
            if (read == 0) return null;
            var data = buffer.AsSpan(0, carry + read);
            if (data.IndexOf(Inno) >= 0) return "innoextract";
            if (data.IndexOf(Nsis) >= 0) return "7zip";
            carry = Math.Min(Inno.Length - 1, data.Length);
            data[^carry..].CopyTo(buffer);
        }
    }

    static async Task<string> Tool(string kind)
    {
        var executable = kind == "innoextract" ? "innoextract.exe" : "7z.exe";
        var configured = Environment.GetEnvironmentVariable(kind == "innoextract" ? "BREW_INNOEXTRACT" : "BREW_7ZIP");
        if (configured != null) return File.Exists(configured) ? Path.GetFullPath(configured) : throw new IOException($"Extraction tool not found: {configured}");
        var cache = new Paths().Cache;
        var tools = Path.Combine(cache, ".tools"); Directory.CreateDirectory(tools);
        var version = kind == "innoextract" ? "1.9" : "26.04";
        using var toolLock = await DownloadLock.Acquire(tools, kind + "-" + version);
        var installed = Path.Combine(tools, kind + "-" + version);
        if (Directory.Exists(installed)) return Directory.GetFiles(installed, executable, SearchOption.AllDirectories).First();
        var url = kind == "innoextract" ? "https://constexpr.org/innoextract/files/innoextract-1.9-windows.zip" : "https://www.7-zip.org/a/7z2604-x64.msi";
        var sha = kind == "innoextract" ? "6989342c9b026a00a72a38f23b62a8e6a22cc5de69805cf47d68ac2fec993065" : "0b01334a418654293513449f61d6bfc99e5196ca371e3c3dc961eda57cd535c6";
        var archive = await new Downloader(cache).FetchAsync(url, sha);
        var work = installed + "." + Guid.NewGuid().ToString("N");
        try
        {
            Directory.CreateDirectory(work);
            if (kind == "innoextract") ZipFile.ExtractToDirectory(archive, work);
            else await MsiArchive.ExtractAsync(archive, work);
            var relative = Path.GetRelativePath(work, Directory.GetFiles(work, executable, SearchOption.AllDirectories).First());
            PackageManager.MoveTree(work, installed);
            return Path.Combine(installed, relative);
        }
        finally
        {
            if (Directory.Exists(work) && Path.GetFullPath(work).StartsWith(Path.GetFullPath(tools) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Directory.Delete(work, true);
        }
    }
}
