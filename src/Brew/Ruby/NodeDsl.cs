using System.Text.Json;
using System.Text.Json.Nodes;
using Brew.Formulae;

namespace Brew.Ruby;

// Homebrew's Language::Node (std_npm_args). On Windows a global npm install writes its command
// shims to the prefix root instead of bin/, so after `npm install --global --prefix=X` brew writes
// the X/bin/<command>.cmd launchers that npm creates on Unix (absolute paths; bottles relocate them).
public static partial class FormulaLoader
{
    const int ReleaseCooldownDays = 1;

    static void DefineNodeDsl(Interp it, RClass F)
    {
        object Kw(Interp i, object[] a, string key) =>
            a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Interp.Sym(key), out var v) ? v : null;

        var language = (RClass)it.ObjectClass.Consts["Language"];
        var node = it.DefineModule("Node", language);
        var shebang = it.DefineModule("Shebang", node);

        it.Def(F, "std_npm_args", (i, self, a, b) =>
        {
            var ignoreScripts = Kw(i, a, "ignore_scripts") is not false;
            var prefixArg = Kw(i, a, "prefix");
            var prefix = prefixArg is false ? null
                : prefixArg == null ? Path.Combine(Ctx(i, self).Prefix, "libexec") : i.Abs(i.PathStr(prefixArg));
            return StrArray((prefix == null ? LocalNpmArgs(i, ignoreScripts) : GlobalNpmArgs(i, prefix, ignoreScripts)).ToArray());
        });
        it.DefS(node, "std_npm_install_args", (i, s, a, b) =>
            StrArray(GlobalNpmArgs(i, i.Abs(i.PathStr(a[0])), Kw(i, a, "ignore_scripts") is not false).ToArray()));
        it.DefS(node, "local_npm_install_args", (i, s, a, b) =>
            StrArray(LocalNpmArgs(i, Kw(i, a, "ignore_scripts") is not false).ToArray()));
        // Shebangs do not select interpreters on Windows; the .cmd launchers call brewed node directly
        // (rewrite_shebang skips a nil rewrite).
        it.Def(shebang, "detected_node_shebang", (i, s, a, b) => null);
    }

    static List<string> SecurityArgs(Interp i, bool ignoreScripts)
    {
        var cache = Path.Combine(i.Env.GetValueOrDefault("HOMEBREW_CACHE") ?? Path.GetTempPath(), "npm_cache");
        var args = new List<string> { $"--min-release-age={ReleaseCooldownDays}", "--cache=" + cache };
        if (ignoreScripts) args.Add("--ignore-scripts");
        return args;
    }

    static List<string> LocalNpmArgs(Interp i, bool ignoreScripts) =>
        ["--loglevel=silly", "--build-from-source", .. SecurityArgs(i, ignoreScripts)];

    static List<string> GlobalNpmArgs(Interp i, string libexec, bool ignoreScripts)
    {
        var pack = PackForInstallation(i);
        Directory.CreateDirectory(Path.Combine(libexec, "lib"));
        return ["--loglevel=silly", "--global", "--build-from-source", .. SecurityArgs(i, ignoreScripts), "--prefix=" + libexec, pack];
    }

    // Homebrew's pack_for_installation: drop prepare/prepack/postpack, `npm pack`, install the tarball.
    static string PackForInstallation(Interp i)
    {
        var package = Path.Combine(i.Cwd, "package.json");
        if (File.Exists(package))
        {
            var json = JsonNode.Parse(File.ReadAllText(package))!.AsObject();
            if (json["scripts"] is JsonObject scripts && (scripts.Remove("prepare") | scripts.Remove("prepack") | scripts.Remove("postpack")))
                File.WriteAllText(package, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        var output = Capture(i, [i.ResolveProgram("npm"), "pack", "--ignore-scripts"], []);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) throw i.Error(i.RuntimeError, $"npm failed to pack {i.Cwd}");
        return Path.Combine(i.Cwd, lines[^1].Trim());
    }

    // Called by Formula#system after a successful `npm install --global --prefix=X`.
    internal static void WriteNpmBinLinks(Interp i, IReadOnlyList<string> argv)
    {
        if (argv.Count < 2 || !Path.GetFileNameWithoutExtension(argv[0]).Equals("npm", StringComparison.OrdinalIgnoreCase)
            || !argv.Contains("install") || !argv.Contains("--global")) return;
        var prefixArg = argv.LastOrDefault(x => x.StartsWith("--prefix=", StringComparison.Ordinal));
        if (prefixArg == null) return;
        var prefix = i.Abs(prefixArg["--prefix=".Length..]);
        var modules = Path.Combine(prefix, "node_modules");
        if (!Directory.Exists(modules)) return;
        var nodeExe = Path.Combine(i.Env.GetValueOrDefault("HOMEBREW_PREFIX") ?? "", "opt", "node", "libexec", "node.exe");
        var node = File.Exists(nodeExe) ? nodeExe : "node";
        var bin = Path.Combine(prefix, "bin");
        foreach (var packageDir in PackageDirectories(modules))
        {
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(packageDir, "package.json")))!.AsObject();
            var commands = manifest["bin"] switch
            {
                JsonValue v when v.TryGetValue<string>(out var single) => new Dictionary<string, string> { [UnscopedName(manifest["name"]?.ToString() ?? "")] = single },
                JsonObject o => o.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""),
                _ => [],
            };
            foreach (var (name, script) in commands)
            {
                if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) continue;
                var target = Path.GetFullPath(Path.Combine(packageDir, script));
                Directory.CreateDirectory(bin);
                File.WriteAllText(Path.Combine(bin, name + ".cmd"),
                    $"@echo off\r\n\"{node.Replace("%", "%%")}\" \"{target.Replace("%", "%%")}\" %*\r\nexit /b %errorlevel%\r\n");
            }
        }
    }

    static IEnumerable<string> PackageDirectories(string modules)
    {
        foreach (var dir in Directory.GetDirectories(modules))
        {
            if (Path.GetFileName(dir).StartsWith('@'))
                foreach (var scoped in Directory.GetDirectories(dir).Where(d => File.Exists(Path.Combine(d, "package.json")))) yield return scoped;
            else if (File.Exists(Path.Combine(dir, "package.json"))) yield return dir;
        }
    }

    static string UnscopedName(string name) => name.Contains('/') ? name[(name.LastIndexOf('/') + 1)..] : name;
}
