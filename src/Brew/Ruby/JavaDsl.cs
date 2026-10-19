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
            WriteCmd(i, i.Abs(i.PathStr(self)), EnvLines(i, env), Invocation(target, args.Length > 0 ? " " + CmdWords(args) : ""));
            return null;
        });
        // Pathname#write_exec_script: a launcher per target, named after it (a .cmd here, not a bash script).
        it.Def(P, "write_exec_script", (i, self, a, b) =>
        {
            var dir = i.Abs(i.PathStr(self));
            Directory.CreateDirectory(dir);
            foreach (var t in a.SelectMany(x => x is RArray arr ? arr.L : new List<object> { x }))
            {
                var target = i.Abs(i.PathStr(t));
                WriteCmd(i, Path.Combine(dir, Path.GetFileName(target)), [], Invocation(target, ""));
            }
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
                $"\"%JAVA_HOME%\\bin\\java.exe\" {CmdWords(options)} -jar \"{Escape(jar)}\" %*".Replace("  ", " "));
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
                WriteCmd(i, Path.Combine(source, group.Key), EnvLines(i, env), Invocation(Path.Combine(destination, group.Key), ""));
            return null;
        });
    }

    // A target as Windows runs it: itself if runnable, else its .exe/.cmd/.bat sibling, else through bash, whose exec
    // honours the #! line (python and perl scripts too, not only shell ones: fastqc). The sibling is looked for when
    // the wrapper runs, since formulae delete one after writing the wrapper (prog8 removes bin/prog8c.bat).
    static string Invocation(string target, string args)
    {
        string[] runnable = [".exe", ".cmd", ".bat", ".com"];
        var ext = Path.GetExtension(target).ToLowerInvariant();
        string Run(string file, string e) => (e is ".cmd" or ".bat" ? "call " : "") + $"\"{Escape(file)}\"{args} %*";
        if (runnable.Contains(ext) && File.Exists(target)) return Run(target, ext);
        if (ext == ".jar") return $"\"%JAVA_HOME%\\bin\\java.exe\" -jar \"{Escape(target)}\"{args} %*";
        var text = new StringBuilder();
        foreach (var e in runnable) text.Append($"if exist \"{Escape(target + e)}\" goto brew_run_{e[1..]}\r\n");
        var posix = Escape(target.Replace('\\', '/'));
        text.Append(ShellScript(target) ? $"bash \"{posix}\"{args} %*\r\n" : $"bash -c \"exec \\\"$0\\\" \\\"$@\\\"\" \"{posix}\"{args} %*\r\n");
        foreach (var e in runnable) text.Append($"exit /b %errorlevel%\r\n:brew_run_{e[1..]}\r\n{Run(target + e, e)}\r\n");
        return text.ToString().TrimEnd();
    }

    // A script bash runs itself: no #! line, or one naming a POSIX shell.
    static bool ShellScript(string file)
    {
        try
        {
            using var reader = new StreamReader(file);
            var first = reader.ReadLine() ?? "";
            if (!first.StartsWith("#!", StringComparison.Ordinal)) return true;
            var words = first[2..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var program = words.Length > 1 && words[0].EndsWith("/env", StringComparison.Ordinal) ? words.Skip(1).FirstOrDefault(w => !w.StartsWith('-')) : words.FirstOrDefault();
            return Path.GetFileName(program ?? "sh") is "sh" or "bash" or "dash" or "ksh" or "zsh";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return true; }
    }

    static readonly Regex DefaultValue = new(@"^\$\{(\w+):-(.*)\}$", RegexOptions.Compiled);

    // `${VAR:-default}` -> set only when unset; `$VAR`/`${VAR}` -> %VAR%; ':' between paths -> ';'.
    static readonly Regex InnerDefault = new(@"\$\{(\w+):-([^{}]*)\}", RegexOptions.Compiled);

    static List<string> EnvLines(Interp i, RHash env)
    {
        var lines = new List<string>();
        var helpers = 0;
        // A default inside a value (leiningen's JAVA_CMD: "${JAVA_CMD:-${JAVA_HOME:-<jdk>}/bin/java}"): cmd has no
        // inline `:-`, so a helper variable takes the default, then the variable's own value when that is set.
        string Defaults(string text)
        {
            for (Match m; (m = InnerDefault.Match(text)).Success;)
            {
                var helper = "BREW_DEFAULT_" + ++helpers;
                lines.Add($"set \"{helper}={Escape(m.Groups[2].Value).Replace("\"", "")}\"");
                lines.Add($"if defined {m.Groups[1].Value} set \"{helper}=%{m.Groups[1].Value}%\"");
                text = text[..m.Index] + "%" + helper + "%" + text[(m.Index + m.Length)..];
            }
            return text;
        }
        foreach (var (key, value) in env.D)
        {
            var name = key is RSymbol s ? s.Name : i.ToS(key);
            var text = i.ToS(value);
            var fallback = DefaultValue.Match(text);
            if (fallback.Success && fallback.Groups[1].Value == name)
            {
                var otherwise = fallback.Groups[2].Value;
                otherwise = InnerDefault.IsMatch(otherwise) ? Regex.Replace(Defaults(otherwise), @"\$\{?(\w+)\}?", "%$1%") : Escape(otherwise);
                lines.Add($"if not defined {name} set \"{name}={otherwise}\"");
                continue;
            }
            text = Regex.Replace(Defaults(text), @"\$\{?(\w+)\}?", "%$1%");
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
        text.Append(command).Append("\r\nexit /b %errorlevel%\r\n");
        File.WriteAllText(path, text.ToString());
    }

    static string Escape(string value) => value.Replace("%", "%%");

    // Shell words for a wrapper's command line (write_jar_script's java_opts, write_env_script's args): bash expands
    // `$JAVA_OPTS` / `${JAVA_OPTS}` there, so the .cmd reads %JAVA_OPTS% (empty when unset) instead of passing
    // the literal text, which java took for the main class (cromwell, ivy, picard-tools, elasticmq, fmpp).
    static string CmdWords(string words) => Regex.Replace(Escape(words), @"\$\{(\w+)\}|\$(\w+)", m => "%" + (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) + "%");
}
