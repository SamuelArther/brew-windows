using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Brew;

internal static class DownloadLock
{
    public static FileStream TryAcquire(string cache, string filename)
    {
        var directory = Path.Combine(cache, ".locks"); Directory.CreateDirectory(directory);
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(filename.ToUpperInvariant())));
        try { return new FileStream(Path.Combine(directory, key + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException ex) when ((ex.HResult & 0xffff) is 32 or 33) { return null; }
    }

    public static async Task<FileStream> Acquire(string cache, string filename)
    {
        var timer = Stopwatch.StartNew();
        while (true)
        {
            var stream = TryAcquire(cache, filename);
            if (stream != null) return stream;
            if (timer.Elapsed > TimeSpan.FromMinutes(15)) throw new IOException($"Timed out waiting for download lock: {filename}");
            await Task.Delay(50);
        }
        // Keep the lock file: deleting it after closing permits independent locks on different file objects.
    }
}
