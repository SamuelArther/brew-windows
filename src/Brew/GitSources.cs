using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Brew;

// Homebrew's GitDownloadStrategy: `url "https://.../x.git", tag: "v1.2", revision: "<sha>"`. The tag is
// cloned shallowly (with submodules) and HEAD must be the pinned revision, which stands in for a sha256.
public static class GitSources
{
    public static bool IsGit(string url, string @using, string reference) =>
        url != null && (@using == "git" || (@using == null && (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("git://", StringComparison.OrdinalIgnoreCase) || (reference != null && !LooksLikeArchive(url)))));

    static bool LooksLikeArchive(string url) =>
        Regex.IsMatch(url, @"\.(tar(\.\w+)?|tgz|tbz2?|txz|zip|7z|gz|xz|bz2|zst|crate|whl|jar|msi|exe|nupkg)(\?.*)?$", RegexOptions.IgnoreCase);

    // Version.detect for a tag: "v0.4.0" -> 0.4.0, "pcre2-10.42" -> 10.42, "release_1_2" -> 1.2.
    public static string VersionFromTag(string tag)
    {
        var m = Regex.Match(tag, @"(\d+(?:[._-]\d+)*(?:[-._]?(?:alpha|beta|rc|pre|preview|p)[-._]?\d*)?)$", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var version = m.Groups[1].Value;
        return version.Contains('.') || !version.Contains('_') ? version : version.Replace('_', '.');
    }

    public static async Task<string> StageAsync(string url, string tag, string revision, string branch, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(destination))!);
        var reference = tag ?? (string.IsNullOrEmpty(branch) ? null : branch);
        if (reference != null)
            await Git(null, ["clone", "--quiet", "--depth", "1", "--branch", reference, "--recurse-submodules", "--shallow-submodules",
                "-c", "advice.detachedHead=false", url, destination]);
        else
        {
            await Git(null, ["init", "--quiet", destination]);
            await Git(destination, ["fetch", "--quiet", "--depth", "1", url, revision ?? "HEAD"]);
            await Git(destination, ["-c", "advice.detachedHead=false", "checkout", "--quiet", "FETCH_HEAD"]);
            await Git(destination, ["submodule", "update", "--quiet", "--init", "--recursive", "--depth", "1"]);
        }
        if (revision != null)
        {
            var head = (await Git(destination, ["rev-parse", "HEAD"])).Trim();
            if (!head.Equals(revision, StringComparison.OrdinalIgnoreCase) && !head.StartsWith(revision, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{tag ?? url} checked out {head}, but the formula pins revision {revision}");
        }
        return destination;
    }

    static async Task<string> Git(string directory, IEnumerable<string> args)
    {
        var start = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        if (directory != null) { start.ArgumentList.Add("-C"); start.ArgumentList.Add(directory); }
        foreach (var arg in args) start.ArgumentList.Add(arg);
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        using var process = Process.Start(start) ?? throw new IOException("git could not be started");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new IOException($"git {string.Join(" ", args)} failed: {error.Trim()}");
        return await output;
    }
}
