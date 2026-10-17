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
            || Starts(0x28, 0xB5, 0x2F, 0xFD) || Starts(0x5D, 0x00, 0x00)
            || Starts(0x4C, 0x5A, 0x49, 0x50) || Starts(0x1F, 0x9D) || Starts(0x04, 0x22, 0x4D, 0x18)) return "tar";   // + lzip, compress, lz4
        if (read >= 262 && System.Text.Encoding.ASCII.GetString(head, 257, 5) == "ustar") return "tar";
        return null;
    }

    // A gzip stream whose content is a tar archive (".tar.gz" under any name, GitHub's /tarball/<ref>).
    static bool GzippedTar(string file)
    {
        try
        {
            using var input = File.OpenRead(file);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            var head = new byte[512];
            var read = gzip.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
            return read >= 262 && System.Text.Encoding.ASCII.GetString(head, 257, 5) == "ustar";
        }
        catch (InvalidDataException) { return false; }
    }

    static bool IsGzip(string file)
    {
        using var stream = File.OpenRead(file);
        return stream.ReadByte() == 0x1F && stream.ReadByte() == 0x8B;
    }

    // `noUnzip`: `using: :nounzip` resources are staged as the downloaded file.
    public static async Task<string> StageAsync(string file, string filename, string destination, bool noUnzip = false)
    {
        // Callers using the URL basename still need the resolved Apache artifact's extension.
        var cachedName = Path.GetFileName(file);
        if (filename is "closer.lua" or "closer.cgi" && cachedName.Length > 66
            && cachedName.Substring(64, 2) == "--" && cachedName[..64].All(Uri.IsHexDigit))
            filename = cachedName[66..];
        Directory.CreateDirectory(destination);
        if (noUnzip) { File.Copy(file, Path.Combine(destination, filename)); return destination; }
        // Homebrew's Gzip strategy: a gzipped single file (zimfw.zsh.gz, a .xcactivitylog) is unpacked to the name
        // without .gz. Gzipped tarballs fall through to tar.
        if (!filename.Contains(".tar", StringComparison.OrdinalIgnoreCase) && !filename.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)
            && IsGzip(file) && !GzippedTar(file))
        {
            var name = filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) && filename.Length > 3 ? filename[..^3] : filename;
            await using (var input = File.OpenRead(file))
            await using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            await using (var output = File.Create(Path.Combine(destination, name)))
                await gzip.CopyToAsync(output);
            return destination;
        }
        // Like Homebrew, trust the bytes over the name: .crate, .tbz2, .txz and GitHub /tarball/<ref>
        // downloads are compressed tarballs whatever they're called.
        var sniffed = Sniff(file);
        switch (sniffed)
        {
            case "zip" when !new[] { ".zip", ".nupkg", ".msix", ".jar", ".whl", ".vsix", ".apk", ".war" }.Any(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase)):
                if (!filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) filename += ".zip";
                break;
            // (A lone foo.gz/.xz/.bz2/.zst is a compressed single file, not a tarball; it's kept as is.)
            case "tar" when !filename.Contains(".tar", StringComparison.OrdinalIgnoreCase) && !filename.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)
                            && !new[] { ".7z", ".gz", ".xz", ".bz2", ".zst", ".lzma", ".lz", ".lz4", ".Z" }.Any(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase)):
                filename += ".tar.x";
                break;
        }
        if (filename.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            await MsiArchive.ExtractAsync(file, destination);
        else if (new[] { ".zip", ".nupkg", ".msix" }.Any(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            ZipFile.ExtractToDirectory(file, destination);
        // A name with ".tar." in it is only a tarball if the bytes are one: bomctl's test resource
        // bomctl_0.3.0_linux_amd64.tar.gz.spdx.json is JSON, staged as the file it is.
        else if ((filename.Contains(".tar.", StringComparison.OrdinalIgnoreCase) || filename.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) && sniffed == "tar"
            || filename.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
        {
            // Windows bsdtar supports gzip, xz, bzip2 and zstd, and rejects unsafe archive paths.
            var start = new ProcessStartInfo("tar.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            foreach (var arg in new[] { "-xf", file, "-C", destination, "--no-same-owner", "--no-same-permissions" }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start);
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new IOException($"Could not extract {filename}: {error.Trim()}");
            RetypeDirectoryLinks(destination);
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

    // Windows' bsdtar makes every tar symlink a file symlink, since its target may not be extracted yet. One that
    // points at a directory then can't be opened as one ("Access to the path ... is denied" staging baml's
    // languages/rust/baml/types -> ../../../engine/language_client_cffi/types), so it becomes a directory link.
    internal static void RetypeDirectoryLinks(string root)
    {
        // Only file-type reparse points: no per-file link lookups across a big source tree.
        var fileLinks = new System.IO.Enumeration.FileSystemEnumerable<string>(root, (ref System.IO.Enumeration.FileSystemEntry e) => e.ToFullPath(),
            new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0, IgnoreInaccessible = true })
        {
            ShouldIncludePredicate = (ref System.IO.Enumeration.FileSystemEntry e) =>
                (e.Attributes & FileAttributes.ReparsePoint) != 0 && (e.Attributes & FileAttributes.Directory) == 0,
            ShouldRecursePredicate = (ref System.IO.Enumeration.FileSystemEntry e) => (e.Attributes & FileAttributes.ReparsePoint) == 0,
        };
        foreach (var path in fileLinks.ToList())
        {
            if (new FileInfo(path).LinkTarget is not { } target) continue;
            var resolved = Path.GetFullPath(target, Path.GetDirectoryName(path)!);
            // Only links that stay inside the extracted tree (tar rejects others' paths, but not link targets).
            var inside = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(inside, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(resolved)) continue;
            try
            {
                File.Delete(path);
                Directory.CreateSymbolicLink(path, target.Replace('/', '\\'));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }
}
