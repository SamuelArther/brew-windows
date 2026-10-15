using System.Runtime.InteropServices;

namespace Brew;

public sealed class SystemCommands(Paths paths)
{
    public void Shellenv(string shell)
    {
        shell = (shell ?? "pwsh").TrimStart('-').ToLowerInvariant();
        if (shell is "pwsh" or "powershell")
        {
            static string Q(string value) => value.Replace("'", "''");
            Console.WriteLine($"$env:HOMEBREW_PREFIX='{Q(paths.Prefix)}';\n$env:HOMEBREW_CELLAR='{Q(paths.Cellar)}';\n$env:HOMEBREW_REPOSITORY='{Q(paths.Repository)}';\n$env:PATH='{Q(paths.Bin)};{Q(Path.Combine(paths.Prefix, "sbin"))};' + $env:PATH;");
        }
        else if (shell is "cmd" or "cmd.exe")
        {
            Console.WriteLine($"set \"HOMEBREW_PREFIX={paths.Prefix}\"\nset \"HOMEBREW_CELLAR={paths.Cellar}\"\nset \"HOMEBREW_REPOSITORY={paths.Repository}\"\nset \"PATH={paths.Bin};{Path.Combine(paths.Prefix, "sbin")};%PATH%\"");
        }
        else
        {
            static string P(string value) { var s = value.Replace('\\', '/'); return s.Length > 1 && s[1] == ':' ? "/" + char.ToLowerInvariant(s[0]) + s[2..] : s; }
            static string Q(string value) => value.Replace("'", "'\\''");
            var prefix = Q(P(paths.Prefix)); var cellar = Q(P(paths.Cellar)); var repository = Q(P(paths.Repository));
            if (shell == "fish") Console.WriteLine($"set -gx HOMEBREW_PREFIX '{prefix}';\nset -gx HOMEBREW_CELLAR '{cellar}';\nset -gx HOMEBREW_REPOSITORY '{repository}';\nfish_add_path --global --move --path '{prefix}/bin' '{prefix}/sbin';");
            else Console.WriteLine($"export HOMEBREW_PREFIX='{prefix}';\nexport HOMEBREW_CELLAR='{cellar}';\nexport HOMEBREW_REPOSITORY='{repository}';\nexport PATH=\"{prefix}/bin:{prefix}/sbin${{PATH+:$PATH}}\";\n[ -z \"${{MANPATH-}}\" ] || export MANPATH=\":${{MANPATH#:}}\";\nexport INFOPATH=\"{prefix}/share/info:${{INFOPATH:-}}\";");
        }
    }

    public async Task Config()
    {
        Console.WriteLine("HOMEBREW_VERSION: 7.0.9");
        var git = await ProcessRunner.Capture("git", ["--version"]);
        Console.WriteLine("ORIGIN: " + (await ProcessRunner.Capture("git", ["-C", paths.Repository, "remote", "get-url", "origin"])).Output.Trim());
        Console.WriteLine("HEAD: " + (await ProcessRunner.Capture("git", ["-C", paths.Repository, "rev-parse", "HEAD"])).Output.Trim());
        Console.WriteLine($"HOMEBREW_PREFIX: {paths.Prefix}\nHOMEBREW_REPOSITORY: {paths.Repository}\nHOMEBREW_CELLAR: {paths.Cellar}\nHOMEBREW_CACHE: {paths.Cache}\nHOMEBREW_NO_ANALYTICS: set\nCPU: {Environment.ProcessorCount}-core {RuntimeInformation.ProcessArchitecture}\nWindows: {Environment.OSVersion.Version}\n.NET: {Environment.Version}\nGit: {git.Output.Trim()}");
    }

    public void Env()
    {
        foreach (var name in new[] { "CC", "CXX", "CFLAGS", "CXXFLAGS", "LDFLAGS", "MAKEFLAGS", "PKG_CONFIG_PATH", "PATH" })
        {
            var value = Environment.GetEnvironmentVariable(name) ?? (name == "CC" ? "clang" : name == "CXX" ? "clang++" : "");
            Console.WriteLine($"{name}: {value}");
        }
    }

    public int Doctor()
    {
        var warnings = new List<string>();
        if (!Directory.Exists(paths.Taps)) warnings.Add("No taps are installed.");
        var path = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
        if (Directory.Exists(paths.Bin) && Directory.EnumerateFiles(paths.Bin).Any() && !path.Any(p => string.Equals(Path.TrimEndingDirectorySeparator(p), paths.Bin, StringComparison.OrdinalIgnoreCase)))
            warnings.Add($"Homebrew's bin was not found in your PATH.\nConsider setting your PATH for example like so:\n  {paths.Bin}");
        var probe = Path.Combine(paths.Prefix, "var", "homebrew", ".doctor-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(probe); File.WriteAllText(Path.Combine(probe, "target"), "brew");
            File.CreateSymbolicLink(Path.Combine(probe, "link"), Path.Combine(probe, "target"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { warnings.Add("Cannot create Homebrew symlinks. Enable Windows Developer Mode and ensure the Homebrew prefix is writable."); }
        finally { if (Directory.Exists(probe)) Directory.Delete(probe, true); }
        foreach (var warning in warnings) CommandOutput.Warning(warning);
        if (warnings.Count == 0) Console.WriteLine("Your system is ready to brew.");
        return warnings.Count == 0 ? 0 : 1;
    }
}
