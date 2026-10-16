using System.Diagnostics;
using System.IO.Compression;

namespace Brew;

public static class Archive
{
    // "zip", "tar" (any compression bsdtar reads, or a plain tar), or null.
    static string Sniff(string file)
    {
        var head = new byte[512];
        int read;
        using (var stream = File.OpenRead(file)) read = stream.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
        bool Starts(params byte[] magic) => read >= magic.Length && head.AsSpan(0, magic.Length).SequenceEqual(magic);
        if (Starts(0x50, 0x4B, 0x03, 0x04)) return "zip";
        if (Starts(0x1F, 0x8B) || Starts(0x42, 0x5A, 0x68) || Starts(0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00)
            || Starts(0x28, 0xB5, 0x2F, 0xFD) || Starts(0x5D, 0x00, 0x00)) return "tar";
        if (read >= 262 && System.Text.Encoding.ASCII.GetString(head, 257, 5) == "ustar") return "tar";
        return null;
    }

    public static async Task<string> StageAsync(string file, string filename, string destination)
    {
        // Callers using the URL basename still need the resolved Apache artifact's extension.
        var cachedName = Path.GetFileName(file);
        if (filename is "closer.lua" or "closer.cgi" && cachedName.Length > 66
            && cachedName.Substring(64, 2) == "--" && cachedName[..64].All(Uri.IsHexDigit))
            filename = cachedName[66..];
        Directory.CreateDirectory(destination);
        // Like Homebrew, trust the bytes over the name: .crate, .tbz2, .txz and GitHub /tarball/<ref>
        // downloads are compressed tarballs whatever they're called.
        switch (Sniff(file))
        {
            case "zip" when !new[] { ".zip", ".nupkg", ".msix", ".jar", ".whl", ".vsix", ".apk", ".war" }.Any(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase)):
                if (!filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) filename += ".zip";
                break;
            // (A lone foo.gz/.xz/.bz2/.zst is a compressed single file, not a tarball; it's kept as is.)
            case "tar" when !filename.Contains(".tar", StringComparison.OrdinalIgnoreCase) && !filename.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)
                            && !new[] { ".7z", ".gz", ".xz", ".bz2", ".zst", ".lzma", ".lz" }.Any(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase)):
                filename += ".tar.x";
                break;
        }
        if (filename.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            await MsiArchive.ExtractAsync(file, destination);
        else if (new[] { ".zip", ".nupkg", ".msix" }.Any(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            ZipFile.ExtractToDirectory(file, destination);
        else if (filename.Contains(".tar.", StringComparison.OrdinalIgnoreCase) || filename.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)
            || filename.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
        {
            // Windows bsdtar supports gzip, xz, bzip2 and zstd, and rejects unsafe archive paths.
            var start = new ProcessStartInfo("tar.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            foreach (var arg in new[] { "-xf", file, "-C", destination, "--no-same-owner", "--no-same-permissions" }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start);
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new IOException($"Could not extract {filename}: {error.Trim()}");
        }
        else if (!filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !await InstallerArchive.Extract(file, destination))
        { File.Copy(file, Path.Combine(destination, filename)); return destination; }

        // Homebrew stages the contents of a single top-level archive directory.
        var entries = Directory.GetFileSystemEntries(destination);
        if (!filename.Contains(".pkg.tar.", StringComparison.OrdinalIgnoreCase)
            && entries.Length == 1 && Directory.Exists(entries[0]) && new DirectoryInfo(entries[0]).LinkTarget == null)
            return entries[0];
        return destination;
    }
}
