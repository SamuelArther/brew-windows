using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

// Homebrew's Language::Java and Pathname#write_env_script / #write_jar_script / #env_script_all_files.
// Unix wrappers are bash scripts; here they are .cmd files. Upstream's "${JAVA_HOME:-default}" becomes
// `if not defined JAVA_HOME set ...`, and wrappers call a target's .bat/.cmd/.exe sibling when the
// target itself is a Unix shell script (Java tools ship both).
public static partial class FormulaLoader
{
    static void DefineJavaDsl(Interp it, RClass F)
    {
        object Kw(Interp i, object[] a, string key) =>
            a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Interp.Sym(key), out var v) ? v : null;
        var language = (RClass)it.ObjectClass.Consts["Language"];
        var java = it.DefineModule("Java", language);

        string JavaHome(Interp i, object version)
        {
            var prefix = i.Env.GetValueOrDefault("HOMEBREW_PREFIX") ?? "";
            var formula = version == null ? "openjdk" : "openjdk@" + i.ToS(version);
            return Path.Combine(prefix, "opt", formula, "libexec");
        }
        RHash Env(Interp i, string value)
        {
            var h = new RHash();
            h.D[Interp.Sym("JAVA_HOME")] = Interp.S(value);
            return h;
        }
        it.DefS(java, "java_home", (i, s, a, b) => i.NewPath(JavaHome(i, a.Length > 0 ? a[0] : null)));
        it.DefS(java, "java_home_env", (i, s, a, b) => Env(i, JavaHome(i, a.Length > 0 ? a[0] : null)));
        it.DefS(java, "overridable_java_home_env", (i, s, a, b) => Env(i, "${JAVA_HOME:-" + JavaHome(i, a.Length > 0 ? a[0] : null) + "}"));

        var P = it.PathnameClass;
        it.Def(P, "write_env_script", (i, self, a, b) =>
        {
            var target = i.Abs(i.PathStr(a[0]));
            string args = "";
            RHash env;
            if (a.Length > 2) { args = i.ToS(a[1]); env = a[2] as RHash ?? new RHash(); }
            else env = a.Length > 1 ? a[1] as RHash ?? new RHash() : new RHash();
            WriteCmd(i, i.Abs(i.PathStr(self)), EnvLines(i, env), Invocation(target) + (args.Length > 0 ? " " + args : ""));
            return null;
        });
        it.Def(P, "write_jar_script", (i, self, a, b) =>
        {
            var jar = i.Abs(i.PathStr(a[0]));
            var name = i.ToS(a[1]);
            var options = a.Length > 2 && a[2] is RString o ? o.V : "";
            var home = JavaHome(i, Kw(i, a, "java_version"));
            var dir = i.Abs(i.PathStr(self));
            Directory.CreateDirectory(dir);
            WriteCmd(i, Path.Combine(dir, name), [$"if not defined JAVA_HOME set \"JAVA_HOME={Escape(home)}\""],
                $"\"%JAVA_HOME%\\bin\\java.exe\" {options} -jar \"{Escape(jar)}\"".Replace("  ", " "));
            return null;
        });
        it.Def(P, "env_script_all_files", (i, self, a, b) =>
        {
            var source = i.Abs(i.PathStr(self));
            var destination = i.Abs(i.PathStr(a[0]));
            var env = a.Length > 1 ? a[1] as RHash ?? new RHash() : new RHash();
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
            {
                var moved = Path.Combine(destination, Path.GetFileName(file));
                if (File.Exists(moved)) File.Delete(moved);
                File.Move(file, moved);
            }
            foreach (var group in Directory.GetFiles(destination).GroupBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
                WriteCmd(i, Path.Combine(source, group.Key), EnvLines(i, env), Invocation(Path.Combine(destination, group.Key)));
            return null;
        });
    }

    // A target as Windows runs it: itself if runnable, else its .exe/.cmd/.bat sibling, else via bash.
    static string Invocation(string target)
    {
        string[] runnable = [".exe", ".cmd", ".bat", ".com"];
        if (runnable.Contains(Path.GetExtension(target).ToLowerInvariant()) && File.Exists(target)) return $"\"{Escape(target)}\"";
        foreach (var ext in runnable)
            if (File.Exists(target + ext)) return (ext is ".cmd" or ".bat" ? "call " : "") + $"\"{Escape(target + ext)}\"";
        if (target.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) return $"\"%JAVA_HOME%\\bin\\java.exe\" -jar \"{Escape(target)}\"";
        return $"bash \"{Escape(target.Replace('\\', '/'))}\"";
    }

    static readonly Regex DefaultValue = new(@"^\$\{(\w+):-(.*)\}$", RegexOptions.Compiled);

    // `${VAR:-default}` -> set only when unset; `$VAR`/`${VAR}` -> %VAR%; ':' between paths -> ';'.
    static List<string> EnvLines(Interp i, RHash env)
    {
        var lines = new List<string>();
        foreach (var (key, value) in env.D)
        {
            var name = key is RSymbol s ? s.Name : i.ToS(key);
            var text = i.ToS(value);
            var fallback = DefaultValue.Match(text);
            if (fallback.Success && fallback.Groups[1].Value == name)
            {
                lines.Add($"if not defined {name} set \"{name}={Escape(fallback.Groups[2].Value)}\"");
                continue;
            }
            text = Regex.Replace(text, @"\$\{?(\w+)\}?", "%$1%");
            if (name.Equals("PATH", StringComparison.OrdinalIgnoreCase) || name.EndsWith("PATH", StringComparison.Ordinal))
                text = Regex.Replace(text, @"(?<=[^A-Za-z]|^):(?!\\)", ";");
            lines.Add($"set \"{name}={text.Replace("\"", "")}\"");
        }
        return lines;
    }

    static void WriteCmd(Interp i, string path, List<string> environment, string command)
    {
        if (!path.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)) path += ".cmd";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var text = new StringBuilder("@echo off\r\nsetlocal DisableDelayedExpansion\r\n");
        foreach (var line in environment) text.Append(line).Append("\r\n");
        text.Append(command).Append(" %*\r\nexit /b %errorlevel%\r\n");
        File.WriteAllText(path, text.ToString());
    }

    static string Escape(string value) => value.Replace("%", "%%");
}
