using System.Diagnostics;
using System.IO.Compression;

namespace Brew;

public static class Archive
{
    public static async Task<string> StageAsync(string file, string filename, string destination)
    {
        Directory.CreateDirectory(destination);
        if (filename.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            await MsiArchive.ExtractAsync(file, destination);
        else if (new[] { ".zip", ".nupkg", ".msix" }.Any(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            ZipFile.ExtractToDirectory(file, destination);
        else if (filename.Contains(".tar.", StringComparison.OrdinalIgnoreCase) || filename.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
        {
            // Windows bsdtar supports gzip, xz, bzip2 and zstd, and rejects unsafe archive paths.
            var start = new ProcessStartInfo("tar.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            foreach (var arg in new[] { "-xf", file, "-C", destination, "--no-same-owner", "--no-same-permissions" }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start);
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new IOException($"Could not extract {filename}: {error.Trim()}");
        }
        else { File.Copy(file, Path.Combine(destination, filename)); return destination; }

        // Homebrew stages the contents of a single top-level archive directory.
        var entries = Directory.GetFileSystemEntries(destination);
        if (!filename.Contains(".pkg.tar.", StringComparison.OrdinalIgnoreCase)
            && entries.Length == 1 && Directory.Exists(entries[0]) && new DirectoryInfo(entries[0]).LinkTarget == null)
            return entries[0];
        return destination;
    }
}
