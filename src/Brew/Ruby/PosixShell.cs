using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

// Homebrew runs a test's command strings with /bin/sh. This reads the part of sh that formula tests
// lean on: quoting, $VAR expansion, NAME=value prefixes, globs and redirections of one simple
// command. Anything else (pipelines, lists, $(...), heredocs) is left to a real POSIX shell.
public static class PosixShell
{
    public enum Target { Out, Err, Null, File }

    public sealed class Redirect
    {
        public Target Kind;
        public string Path;
        public bool Append;
        public Redirect Copy() => (Redirect)MemberwiseClone();
    }

    public sealed class Command
    {
        public List<string> Argv = new();
        public Dictionary<string, string> Assignments = new();
        public Redirect Stdout = new() { Kind = Target.Out };
        public Redirect Stderr = new() { Kind = Target.Err };
        public string StdinFile;
        public string HereString;
        public bool Redirected => Stdout.Kind != Target.Out || Stderr.Kind != Target.Err || StdinFile != null || HereString != null;
    }

    sealed class Unsupported : Exception { }

    // The command, or null when the line needs more of sh than this reads.
    public static Command Parse(string line, Func<string, string> getenv, string cwd)
    {
        try { return new Reader(line, getenv, cwd).Read(); }
        catch (Unsupported) { return null; }
    }

    sealed class Reader(string s, Func<string, string> getenv, string cwd)
    {
        int at;

        public Command Read()
        {
            var command = new Command();
            var words = new List<string>();
            while (true)
            {
                while (at < s.Length && s[at] is ' ' or '\t') at++;
                if (at >= s.Length) break;
                var c = s[at];
                if (c == '#') break;
                if (c is '|' or ';' or '(' or ')' or '\n' or '\r' or '{' or '}') throw new Unsupported();
                if (c == '&')
                {
                    // &> and &>> send both streams to one place (bash); a lone & is a background job.
                    if (at + 1 < s.Length && s[at + 1] == '>')
                    {
                        at++;
                        var both = RedirectOut(command, 1);
                        command.Stdout = both;
                        command.Stderr = both.Copy();
                        continue;
                    }
                    throw new Unsupported();
                }
                if (c is '<' or '>') { ReadRedirect(command, c == '<' ? 0 : 1); continue; }
                if (char.IsDigit(c) && at + 1 < s.Length && s[at + 1] is '<' or '>')
                {
                    var fd = c - '0';
                    at++;
                    ReadRedirect(command, fd);
                    continue;
                }
                if (words.Count == 0 && Regex.Match(s[at..], "^[A-Za-z_][A-Za-z0-9_]*=") is { Success: true } assignment)
                {
                    // NAME=value, NAME="a b", NAME='x': the value is one word, never globbed.
                    at += assignment.Length;
                    var value = "";
                    if (at < s.Length && s[at] is not (' ' or '\t'))
                    {
                        var (raw, _, isPattern) = Word();
                        value = isPattern ? Regex.Replace(raw, @"\\(.)", "$1") : raw;
                    }
                    command.Assignments[assignment.Value[..^1]] = value;
                    continue;
                }
                var (word, quoted, glob) = Word();
                if (!quoted && word.Length == 0) continue;   // an unquoted expansion of an empty variable is no word
                if (glob) words.AddRange(Glob(word));
                else words.Add(word);
            }
            if (words.Count == 0) throw new Unsupported();
            // Arguments stay as written: MSYS programs (neomutt, tio) understand /dev/null themselves.
            command.Argv = words;
            return command;
        }

        void ReadRedirect(Command command, int fd)
        {
            if (s[at] == '<')
            {
                if (fd != 0) throw new Unsupported();
                if (s.AsSpan(at).StartsWith("<<<")) { at += 3; command.HereString = Target_() + "\n"; return; }
                at++;
                if (at < s.Length && s[at] is '<' or '&' or '>') throw new Unsupported();
                var file = Target_();
                command.StdinFile = file == "/dev/null" ? "NUL" : file;
                return;
            }
            if (fd is not (1 or 2)) throw new Unsupported();
            var r = RedirectOut(command, fd);
            if (fd == 1) command.Stdout = r; else command.Stderr = r;
        }

        // At '>': >file, >>file, >|file, >&N, >/dev/null
        Redirect RedirectOut(Command command, int fd)
        {
            at++;
            var append = false;
            if (at < s.Length && s[at] == '>') { append = true; at++; }
            else if (at < s.Length && s[at] == '|') at++;
            if (at < s.Length && s[at] == '&')
            {
                at++;
                if (at < s.Length && s[at] == '1') { at++; return command.Stdout.Copy(); }
                if (at < s.Length && s[at] == '2') { at++; return command.Stderr.Copy(); }
                throw new Unsupported();
            }
            var file = Target_();
            if (file == "/dev/null") return new Redirect { Kind = Target.Null };
            return new Redirect { Kind = Target.File, Path = Path.GetFullPath(Path.Combine(cwd, file)), Append = append };
        }

        string Target_()
        {
            while (at < s.Length && s[at] is ' ' or '\t') at++;
            if (at >= s.Length || s[at] is '|' or '&' or ';' or '<' or '>' or '(' or ')') throw new Unsupported();
            var (word, _, glob) = Word();
            if (glob) throw new Unsupported();
            return word;
        }

        (string Word, bool Quoted, bool Glob) Word()
        {
            var sb = new StringBuilder();
            var pattern = new StringBuilder();   // the word as a glob: quoted characters escaped
            bool quoted = false, glob = false, start = true;
            void Lit(string t) { sb.Append(t); foreach (var ch in t) pattern.Append(ch is '*' or '?' or '\\' ? "\\" + ch : ch.ToString()); }
            while (at < s.Length)
            {
                var c = s[at];
                if (c is ' ' or '\t' or '|' or '&' or ';' or '<' or '>' or '(' or ')' or '\n' or '\r') break;
                if (c == '`') throw new Unsupported();
                if (c == '~' && start && (at + 1 == s.Length || s[at + 1] is '/' or ' ' or '\t'))
                {
                    at++;
                    Lit(getenv("HOME") ?? "~");
                }
                else if (c == '\'')
                {
                    quoted = true;
                    var end = s.IndexOf('\'', at + 1);
                    if (end < 0) throw new Unsupported();
                    Lit(s[(at + 1)..end]);
                    at = end + 1;
                }
                else if (c == '"')
                {
                    quoted = true;
                    at++;
                    while (true)
                    {
                        if (at >= s.Length) throw new Unsupported();
                        var d = s[at];
                        if (d == '"') { at++; break; }
                        if (d == '`') throw new Unsupported();
                        if (d == '\\' && at + 1 < s.Length && s[at + 1] is '$' or '`' or '"' or '\\')
                        {
                            Lit(s[at + 1].ToString());
                            at += 2;
                        }
                        else if (d == '\\' && at + 1 < s.Length && s[at + 1] == '\n') at += 2;
                        else if (d == '$') Lit(Dollar());
                        else { Lit(d.ToString()); at++; }
                    }
                }
                else if (c == '\\' && at + 1 < s.Length && (char.IsLetterOrDigit(s[at + 1]) || s[at + 1] is '.' or '_' or '-'))
                {
                    // A path separator: "#{bin}/tool" interpolates C:\x\bin. (sh would drop the backslash.)
                    Lit("\\");
                    at++;
                }
                else if (c == '\\')
                {
                    if (at + 1 >= s.Length) throw new Unsupported();
                    if (s[at + 1] != '\n') Lit(s[at + 1].ToString());
                    quoted = true;
                    at += 2;
                }
                else if (c == '$')
                {
                    // Unquoted, sh splits the value into fields on whitespace: leave that to a real shell.
                    var value = Dollar();
                    if (value.Any(ch => ch is ' ' or '\t' or '\n')) throw new Unsupported();
                    Lit(value);
                }
                else
                {
                    if (c is '*' or '?') { glob = true; pattern.Append(c); sb.Append(c); }
                    else { sb.Append(c); pattern.Append(c); }
                    at++;
                }
                start = false;
            }
            return glob ? (pattern.ToString(), quoted, true) : (sb.ToString(), quoted, false);
        }

        // At '$': $NAME or ${NAME}. $(...), $?, $1 and ${NAME:-...} need a real shell.
        string Dollar()
        {
            at++;
            if (at < s.Length && s[at] == '{')
            {
                var end = s.IndexOf('}', at);
                if (end < 0) throw new Unsupported();
                var name = s[(at + 1)..end];
                if (!Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$")) throw new Unsupported();
                at = end + 1;
                return getenv(name) ?? "";
            }
            var m = Regex.Match(s[at..], "^[A-Za-z_][A-Za-z0-9_]*");
            if (m.Success) { at += m.Length; return getenv(m.Value) ?? ""; }
            if (at < s.Length && !(s[at] is ' ' or '\t' or '"' or '/' or '.' or ',' or ':' or '=' or '-')) throw new Unsupported();
            return "$";
        }

        // sh pathname expansion of a word whose wildcards sit in its last component.
        IEnumerable<string> Glob(string pattern)
        {
            var literal = Regex.Replace(pattern, @"\\(.)", "$1");
            var slash = pattern.LastIndexOf('/');
            var dir = slash < 0 ? "" : pattern[..(slash + 1)];
            var name = pattern[(slash + 1)..];
            if (Regex.IsMatch(dir, @"(?<!\\)[*?]")) throw new Unsupported();
            dir = Regex.Replace(dir, @"\\(.)", "$1");
            var regex = new Regex("^" + Regex.Replace(name, @"\\(.)|([*?])|(.)", m =>
                m.Groups[1].Success ? Regex.Escape(m.Groups[1].Value)
                : m.Groups[2].Value == "*" ? ".*" : m.Groups[2].Success ? "." : Regex.Escape(m.Groups[3].Value)) + "$",
                RegexOptions.IgnoreCase);
            var root = Path.Combine(cwd, dir);
            if (!Directory.Exists(root)) return [literal];
            var matches = Directory.EnumerateFileSystemEntries(root).Select(Path.GetFileName)
                .Where(n => regex.IsMatch(n) && (!n.StartsWith('.') || name.StartsWith('.')))
                .OrderBy(n => n, StringComparer.Ordinal).Select(n => dir + n).ToList();
            return matches.Count > 0 ? matches : [literal];
        }
    }

    // A POSIX shell to hand whole lines to: the build toolchain's MSYS2, else Git for Windows' bash.
    public static string FindShell(Func<string, string> getenv)
    {
        var candidates = new List<string>();
        if (getenv("HOMEBREW_MSYS2_ROOT") is { Length: > 0 } msys) candidates.Add(Path.Combine(msys, "usr", "bin", "bash.exe"));
        if (BuildEnvironment.Msys2Root is { } root) candidates.Add(Path.Combine(root, "usr", "bin", "bash.exe"));
        foreach (var dir in (getenv("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string git;
            try { git = Path.Combine(dir.Trim('"'), "git.exe"); } catch (ArgumentException) { continue; }
            if (!File.Exists(git)) continue;
            var home = Path.GetDirectoryName(Path.GetDirectoryName(git)!);
            if (home != null) candidates.Add(Path.Combine(home, "bin", "bash.exe"));
            if (home != null && Path.GetDirectoryName(home) is { } up) candidates.Add(Path.Combine(up, "bin", "bash.exe"));
        }
        foreach (var pf in new[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramW6432") })
            if (!string.IsNullOrEmpty(pf)) candidates.Add(Path.Combine(pf, "Git", "bin", "bash.exe"));
        return candidates.FirstOrDefault(File.Exists);
    }

    static readonly Regex WindowsPath = new(@"(?<![A-Za-z0-9])[A-Za-z]:\\[^\s'""|&;<>()]*");

    // A line for bash: Windows paths get forward slashes (a backslash escapes in sh), and
    // C:/x/bin/tool picks up the .cmd/.bat extension bash won't look for itself.
    public static string ForBash(string line, string[] pathExts) => WindowsPath.Replace(line, m =>
    {
        var path = m.Value;
        if (!File.Exists(path) && !Directory.Exists(path))
            foreach (var e in pathExts)
                if (e is ".CMD" or ".BAT" or ".cmd" or ".bat" && File.Exists(path + e)) { path += e.ToLowerInvariant(); break; }
        return path.Replace('\\', '/');
    });
}
