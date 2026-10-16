using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// container-compose: Docker Compose files for the WSL2 `container` command. Reads compose.yml /
// docker-compose.yml (a YAML subset: block and flow collections, quoted and block scalars), and runs
// each service as a container labelled with the project, in depends_on order. `up` without -d follows
// the services' logs until Ctrl+C and then stops them, like docker compose.
static class ContainerCompose
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("container-compose", "Manage WSL2 containers with Docker Compose files", Run);
    }

    const string Usage = """
        OVERVIEW: A tool to manage Docker Compose files with the container command (WSL2)

        USAGE: container-compose [-f <file>] [-p <project>] <subcommand>

        SUBCOMMANDS:
          up [-d] [<service> ...]     Create and start the services' containers
          down [-v] [--rmi]           Stop and remove the project's containers
          ps                          List the project's containers
          logs [-f] [<service> ...]   Show the services' output
          start [<service> ...]       Start existing containers
          stop [<service> ...]        Stop running containers
          config                      Show the services as parsed
          version                     Show the version

        OPTIONS:
          -f, --file <file>           Compose file (default: compose.yml, compose.yaml, docker-compose.yml, docker-compose.yaml)
          -p, --project-name <name>   Project name (default: the folder's name)
          -h, --help                  Show help information.
        """;

    static readonly string[] FileNames = ["compose.yml", "compose.yaml", "docker-compose.yml", "docker-compose.yaml"];

    static int Run(string[] args)
    {
        string file = null, project = null;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{args[i]} takes an argument");
            switch (args[i])
            {
                case "-f" or "--file" when rest.Count == 0: file = Next(); break;
                case "-p" or "--project-name" when rest.Count == 0: project = Next(); break;
                case "-h" or "--help" when rest.Count == 0: Console.WriteLine(Usage); return 0;
                default: rest.Add(args[i]); break;
            }
        }
        if (rest.Count == 0) { Console.WriteLine(Usage); return 1; }
        if (rest[0] is "version" or "--version" or "-v") { Console.WriteLine($"container-compose version {Commands.Version}"); return 0; }

        var dir = Directory.GetCurrentDirectory();
        file ??= FileNames.Select(n => Path.Combine(dir, n)).FirstOrDefault(File.Exists);
        if (file == null || !File.Exists(file))
        {
            Console.Error.WriteLine($"Error: compose.yml not found at {(file == null ? dir : Path.GetFullPath(file))}");
            return 1;
        }
        var compose = Load(file, project);
        var sub = rest.Skip(1).ToList();
        return rest[0] switch
        {
            "up" => Up(compose, sub),
            "down" => Down(compose, sub),
            "ps" => Ps(compose),
            "logs" => Logs(compose, sub),
            "start" => Start(compose, sub),
            "stop" => Stop(compose, sub),
            "restart" => Stop(compose, sub) | Start(compose, sub),
            "config" => Config(compose),
            "build" => Fail("container-compose build: building images needs BuildKit; build elsewhere and `container image load` it, then use image:"),
            _ => throw new UsageException($"unknown subcommand '{rest[0]}' (see container-compose --help)"),
        };
    }

    static int Fail(string message) { Console.Error.WriteLine("Error: " + message); return 1; }

    // ---- model ----

    sealed class Service
    {
        public string Name, Image, ContainerName, WorkingDir, User;
        public List<string> Command, Entrypoint, Env = [], Ports = [], Volumes = [], DependsOn = [];
    }

    sealed class Project
    {
        public string Name, Dir;
        public List<Service> Services = [];
        public List<string> Volumes = [];
    }

    static Project Load(string file, string projectName)
    {
        var root = Yaml.Parse(File.ReadAllText(file)) as Dictionary<string, object>
                   ?? throw new InvalidOperationException($"{file}: not a compose file (expected a mapping)");
        var dir = Path.GetDirectoryName(Path.GetFullPath(file));
        var name = projectName ?? Scalar(root.GetValueOrDefault("name")) ?? Path.GetFileName(dir);
        name = Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9_-]", "");
        if (name.Length == 0) name = "default";
        var project = new Project { Name = name, Dir = dir };
        if (root.GetValueOrDefault("volumes") is Dictionary<string, object> vols) project.Volumes.AddRange(vols.Keys);
        if (root.GetValueOrDefault("services") is not Dictionary<string, object> services || services.Count == 0)
            throw new InvalidOperationException($"{file}: no services");
        foreach (var (key, value) in services)
        {
            var s = value as Dictionary<string, object> ?? throw new InvalidOperationException($"service {key}: expected a mapping");
            var service = new Service
            {
                Name = key,
                Image = Scalar(s.GetValueOrDefault("image")),
                ContainerName = Scalar(s.GetValueOrDefault("container_name")),
                WorkingDir = Scalar(s.GetValueOrDefault("working_dir")),
                User = Scalar(s.GetValueOrDefault("user")),
                Command = Words(s.GetValueOrDefault("command")),
                Entrypoint = Words(s.GetValueOrDefault("entrypoint")),
            };
            if (service.Image == null)
                throw new InvalidOperationException(s.ContainsKey("build")
                    ? $"service {key}: build: needs BuildKit; build the image elsewhere, `container image load` it and set image:"
                    : $"service {key}: no image");
            foreach (var envFile in List(s.GetValueOrDefault("env_file")))
            {
                var path = Path.GetFullPath(Path.Combine(dir, Scalar(envFile is Dictionary<string, object> m ? m.GetValueOrDefault("path") : envFile)));
                service.Env.AddRange(File.ReadAllLines(path).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')));
            }
            switch (s.GetValueOrDefault("environment"))
            {
                case Dictionary<string, object> map:
                    service.Env.AddRange(map.Select(kv => kv.Key + "=" + (Scalar(kv.Value) ?? Environment.GetEnvironmentVariable(kv.Key) ?? "")));
                    break;
                case List<object> list:
                    service.Env.AddRange(list.Select(Scalar).Select(e => e.Contains('=') ? e : e + "=" + Environment.GetEnvironmentVariable(e)));
                    break;
            }
            foreach (var port in List(s.GetValueOrDefault("ports")))
                service.Ports.Add(port is Dictionary<string, object> p
                    ? $"{Scalar(p.GetValueOrDefault("published")) ?? Scalar(p.GetValueOrDefault("target"))}:{Scalar(p.GetValueOrDefault("target"))}"
                    : Scalar(port));
            foreach (var volume in List(s.GetValueOrDefault("volumes")))
                service.Volumes.Add(Volume(project, volume));
            service.DependsOn = s.GetValueOrDefault("depends_on") switch
            {
                Dictionary<string, object> map => map.Keys.ToList(),
                List<object> list => list.Select(Scalar).ToList(),
                _ => [],
            };
            project.Services.Add(service);
        }
        return project;
    }

    // "./src:/app:ro" (relative to the compose file), "data:/var/lib/x" (a project volume) or long syntax.
    static string Volume(Project project, object spec)
    {
        string source, target;
        var ro = false;
        if (spec is Dictionary<string, object> m)
        {
            source = Scalar(m.GetValueOrDefault("source"));
            target = Scalar(m.GetValueOrDefault("target"));
            ro = Scalar(m.GetValueOrDefault("read_only")) == "true";
            if (source == null) throw new InvalidOperationException("anonymous volumes aren't supported; give a source");
        }
        else
        {
            var (s, t, r) = Container.SplitVolume(Scalar(spec));
            (source, target, ro) = (s, t, r);
        }
        if (source.StartsWith('.') || source.StartsWith('~') || source.Contains('/') || source.Contains('\\'))
        {
            if (source.StartsWith('~')) source = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + source[1..];
            source = Path.GetFullPath(Path.Combine(project.Dir, source));
        }
        else source = project.Name + "_" + source;
        return $"{source}:{target}" + (ro ? ":ro" : "");
    }

    static string Scalar(object value) => value switch { null => null, string s => s, _ => throw new InvalidOperationException("expected a value, found a collection") };

    static List<object> List(object value) => value switch { null => [], List<object> l => l, _ => [value] };

    // command: "npm start" or ["npm", "start"]
    static List<string> Words(object value) => value switch
    {
        null => null,
        List<object> list => list.Select(Scalar).ToList(),
        string s => SplitWords(s),
        _ => throw new InvalidOperationException("command must be a string or a list"),
    };

    internal static List<string> SplitWords(string s)
    {
        var words = new List<string>();
        var current = new StringBuilder();
        char quote = '\0';
        var any = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
                else if (c == '\\' && quote == '"' && i + 1 < s.Length) current.Append(s[++i]);
                else current.Append(c);
            }
            else if (c is '"' or '\'') { quote = c; any = true; }
            else if (char.IsWhiteSpace(c)) { if (any || current.Length > 0) words.Add(current.ToString()); current.Clear(); any = false; }
            else if (c == '\\' && i + 1 < s.Length) current.Append(s[++i]);
            else current.Append(c);
        }
        if (any || current.Length > 0) words.Add(current.ToString());
        return words;
    }

    static string ContainerId(Project p, Service s) => s.ContainerName ?? $"{p.Name}-{s.Name}";

    // Services ordered so each comes after what it depends on.
    static List<Service> Ordered(Project p, List<string> only)
    {
        var result = new List<Service>();
        var visiting = new HashSet<string>();
        void Visit(Service s)
        {
            if (result.Contains(s)) return;
            if (!visiting.Add(s.Name)) throw new InvalidOperationException($"dependency cycle at service {s.Name}");
            foreach (var d in s.DependsOn)
                Visit(p.Services.FirstOrDefault(x => x.Name == d) ?? throw new InvalidOperationException($"service {s.Name} depends on unknown service {d}"));
            result.Add(s);
        }
        foreach (var s in Select(p, only)) Visit(s);
        return result;
    }

    static List<Service> Select(Project p, List<string> names)
    {
        var picked = names.Where(n => !n.StartsWith('-')).ToList();
        if (picked.Count == 0) return p.Services;
        return picked.Select(n => p.Services.FirstOrDefault(s => s.Name == n) ?? throw new UsageException($"no such service: {n}")).ToList();
    }

    static Container.Box Find(Project p, Service s) => Container.ListBoxes().FirstOrDefault(b => b.Id == ContainerId(p, s));

    // ---- subcommands ----

    static int Up(Project p, List<string> args)
    {
        var detach = args.Any(a => a is "-d" or "--detach");
        var services = Ordered(p, args);
        foreach (var s in services)
        {
            var box = Find(p, s);
            if (box != null)
            {
                if (!Container.IsRunning(box)) { Container.Start(box); Console.WriteLine($"Started {box.Id}"); }
                else Console.WriteLine($"Running {box.Id}");
                continue;
            }
            var run = new List<string> { "run", "--detach", "--quiet", "--name", ContainerId(p, s),
                "--label", "com.docker.compose.project=" + p.Name, "--label", "com.docker.compose.service=" + s.Name };
            foreach (var e in s.Env) run.AddRange(["--env", e]);
            foreach (var port in s.Ports) run.AddRange(["--publish", port]);
            foreach (var v in s.Volumes) run.AddRange(["--volume", v]);
            if (s.WorkingDir != null) run.AddRange(["--workdir", s.WorkingDir]);
            if (s.User != null) run.AddRange(["--user", s.User]);
            if (s.Entrypoint != null) run.AddRange(["--entrypoint", s.Entrypoint.FirstOrDefault() ?? ""]);
            run.Add(s.Image);
            if (s.Entrypoint != null) run.AddRange(s.Entrypoint.Skip(1));
            if (s.Command != null) run.AddRange(s.Command);
            Console.WriteLine($"Creating {ContainerId(p, s)}");
            var code = Container.Run(run.ToArray());
            if (code != 0) return code;
        }
        if (detach) return 0;
        Console.WriteLine("Attaching to " + string.Join(", ", services.Select(s => ContainerId(p, s))) + " (Ctrl+C stops them)");
        var stop = false;
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop = true; };
        Follow(p, services, () => stop);
        Console.WriteLine("Stopping...");
        foreach (var s in Enumerable.Reverse(services))
            if (Find(p, s) is { } box) Container.Stop(box);
        return 0;
    }

    static int Down(Project p, List<string> args)
    {
        var volumes = args.Any(a => a is "-v" or "--volumes");
        var images = args.Contains("--rmi");
        var mine = Container.ListBoxes().Where(b => b.Labels.Contains("com.docker.compose.project=" + p.Name)).ToList();
        foreach (var box in Enumerable.Reverse(mine))
        {
            Container.Delete(box, force: true);
            Console.WriteLine($"Removed {box.Id}");
        }
        if (volumes)
            foreach (var v in p.Volumes)
            {
                var path = Path.Combine(Container.Root, "volumes", p.Name + "_" + v);
                if (Directory.Exists(path)) { Directory.Delete(path, true); Console.WriteLine($"Removed volume {p.Name}_{v}"); }
            }
        if (images)
            foreach (var s in p.Services)
                Container.Run(["image", "delete", s.Image]);
        return 0;
    }

    static int Ps(Project p)
    {
        var rows = new List<string[]> { new[] { "NAME", "SERVICE", "IMAGE", "STATE", "PORTS" } };
        foreach (var box in Container.ListBoxes().Where(b => b.Labels.Contains("com.docker.compose.project=" + p.Name)))
        {
            var service = box.Labels.FirstOrDefault(l => l.StartsWith("com.docker.compose.service=", StringComparison.Ordinal))?.Split('=')[1] ?? "";
            rows.Add([box.Id, service, box.Image, Container.State(box), string.Join(", ", box.Ports)]);
        }
        Container.PrintTable(rows);
        return 0;
    }

    static int Start(Project p, List<string> args)
    {
        foreach (var s in Ordered(p, args))
        {
            var box = Find(p, s) ?? throw new InvalidOperationException($"service {s.Name} has no container (run up first)");
            Container.Start(box);
            Console.WriteLine($"Started {box.Id}");
        }
        return 0;
    }

    static int Stop(Project p, List<string> args)
    {
        foreach (var s in Enumerable.Reverse(Ordered(p, args)))
            if (Find(p, s) is { } box && Container.IsRunning(box))
            {
                Container.Stop(box);
                Console.WriteLine($"Stopped {box.Id}");
            }
        return 0;
    }

    static int Logs(Project p, List<string> args)
    {
        var follow = args.Any(a => a is "-f" or "--follow");
        var services = Select(p, args);
        Follow(p, services, () => !follow || services.All(s => Find(p, s) is not { } b || !Container.IsRunning(b)), fromStart: true);
        return 0;
    }

    // Prints each service's log with a "service | " prefix; polls until done() says to stop.
    static void Follow(Project p, List<Service> services, Func<bool> done, bool fromStart = false)
    {
        var width = services.Max(s => s.Name.Length);
        var offsets = new Dictionary<string, long>();
        var partial = new Dictionary<string, string>();
        foreach (var s in services)
            offsets[s.Name] = !fromStart && Find(p, s) is { } b && File.Exists(b.LogFile) ? new FileInfo(b.LogFile).Length : 0;
        while (true)
        {
            var any = false;
            foreach (var s in services)
            {
                if (Find(p, s) is not { } box || !File.Exists(box.LogFile)) continue;
                using var stream = new FileStream(box.LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (stream.Length <= offsets[s.Name]) continue;
                stream.Position = offsets[s.Name];
                var text = partial.GetValueOrDefault(s.Name, "") + new StreamReader(stream).ReadToEnd();
                offsets[s.Name] = stream.Length;
                var lines = text.Split('\n');
                partial[s.Name] = lines[^1];
                foreach (var line in lines[..^1]) Console.WriteLine($"{s.Name.PadRight(width)} | {line.TrimEnd('\r')}");
                any = true;
            }
            if (!any && done()) break;
            if (!any) Thread.Sleep(250);
        }
    }

    static int Config(Project p)
    {
        Console.WriteLine($"name: {p.Name}\nservices:");
        foreach (var s in p.Services)
        {
            Console.WriteLine($"  {s.Name}:\n    container: {ContainerId(p, s)}\n    image: {s.Image}");
            if (s.Entrypoint != null) Console.WriteLine($"    entrypoint: [{string.Join(", ", s.Entrypoint)}]");
            if (s.Command != null) Console.WriteLine($"    command: [{string.Join(", ", s.Command)}]");
            if (s.WorkingDir != null) Console.WriteLine($"    working_dir: {s.WorkingDir}");
            if (s.User != null) Console.WriteLine($"    user: {s.User}");
            void Items(string name, List<string> items)
            {
                if (items.Count == 0) return;
                Console.WriteLine($"    {name}:");
                foreach (var i in items) Console.WriteLine($"      - {i}");
            }
            Items("environment", s.Env);
            Items("ports", s.Ports);
            Items("volumes", s.Volumes);
            Items("depends_on", s.DependsOn);
        }
        if (p.Volumes.Count > 0) Console.WriteLine("volumes:\n" + string.Join("\n", p.Volumes.Select(v => $"  {v}: {{}}")));
        return 0;
    }

    // ---- YAML subset ----

    // Mappings, sequences (block and flow), plain/quoted scalars and |/> block scalars; values are
    // Dictionary<string, object>, List<object> or string (null for ~/null/empty). No anchors or tags.
    internal static class Yaml
    {
        sealed record Line(int Indent, string Text, string Raw);

        public static object Parse(string text)
        {
            var lines = new List<Line>();
            foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.TrimStart().StartsWith('#') || raw.Trim().Length == 0 || raw.Trim() is "---" or "...") { lines.Add(new Line(-1, "", raw)); continue; }
                if (raw.Contains('\t') && raw.TrimStart(' ').StartsWith('\t')) throw new InvalidOperationException("tabs aren't allowed for YAML indentation");
                var indent = raw.Length - raw.TrimStart(' ').Length;
                lines.Add(new Line(indent, StripComment(raw.Trim()), raw));
            }
            var i = 0;
            Skip(lines, ref i);
            return i < lines.Count ? Block(lines, ref i, lines[i].Indent) : null;
        }

        static void Skip(List<Line> lines, ref int i) { while (i < lines.Count && lines[i].Indent < 0) i++; }

        static string StripComment(string s)
        {
            char quote = '\0';
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (quote != '\0') { if (c == quote) quote = '\0'; continue; }
                if ((c is '"' or '\'') && (i == 0 || " :[{,-".Contains(s[i - 1]))) quote = c;
                else if (c == '#' && i > 0 && s[i - 1] == ' ') return s[..i].TrimEnd();
            }
            return s;
        }

        static object Block(List<Line> lines, ref int i, int indent)
        {
            if (lines[i].Text == "-" || lines[i].Text.StartsWith("- ", StringComparison.Ordinal)) return Sequence(lines, ref i, indent);
            return Mapping(lines, ref i, indent);
        }

        static List<object> Sequence(List<Line> lines, ref int i, int indent)
        {
            var list = new List<object>();
            while (i < lines.Count && lines[i].Indent == indent && (lines[i].Text == "-" || lines[i].Text.StartsWith("- ", StringComparison.Ordinal)))
            {
                var item = lines[i].Text.Length > 1 ? lines[i].Text[2..].TrimStart() : "";
                var itemIndent = indent + lines[i].Text.Length - item.Length;
                if (item.Length == 0)
                {
                    i++;
                    Skip(lines, ref i);
                    list.Add(i < lines.Count && lines[i].Indent > indent ? Block(lines, ref i, lines[i].Indent) : null);
                }
                else if (KeyAt(item) >= 0 && item[0] is not ('"' or '\'' or '[' or '{') || item.StartsWith("- ", StringComparison.Ordinal))
                {
                    // "- key: value" opens a mapping (or "- - x" a sequence) indented to where the item text starts.
                    lines[i] = new Line(itemIndent, item, lines[i].Raw);
                    list.Add(Block(lines, ref i, itemIndent));
                }
                else
                {
                    list.Add(Value(item));
                    i++;
                }
                Skip(lines, ref i);
            }
            return list;
        }

        static Dictionary<string, object> Mapping(List<Line> lines, ref int i, int indent)
        {
            var map = new Dictionary<string, object>();
            while (i < lines.Count && lines[i].Indent == indent)
            {
                var text = lines[i].Text;
                var colon = KeyAt(text);
                if (colon < 0) throw new InvalidOperationException($"YAML: expected 'key: value' at \"{text}\"");
                var key = Unquote(text[..colon].Trim());
                var rest = text[(colon + 1)..].Trim();
                i++;
                Skip(lines, ref i);
                if (rest.Length == 0)
                {
                    if (i < lines.Count && lines[i].Indent > indent) map[key] = Block(lines, ref i, lines[i].Indent);
                    else if (i < lines.Count && lines[i].Indent == indent && lines[i].Text.StartsWith('-')) map[key] = Sequence(lines, ref i, indent);
                    else map[key] = null;
                }
                else if (rest[0] is '|' or '>') map[key] = BlockScalar(lines, ref i, indent, rest);
                else map[key] = Value(rest);
                Skip(lines, ref i);
            }
            if (i < lines.Count && lines[i].Indent > indent) throw new InvalidOperationException($"YAML: unexpected indentation at \"{lines[i].Text}\"");
            return map;
        }

        // Index of the ':' ending a mapping key (followed by a space or the end), outside quotes; -1 if none.
        static int KeyAt(string s)
        {
            char quote = '\0';
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (quote != '\0') { if (c == quote) quote = '\0'; continue; }
                if ((c is '"' or '\'') && i == 0) quote = c;
                else if (c is '[' or '{' && i == 0) return -1;
                else if (c == ':' && (i + 1 == s.Length || s[i + 1] == ' ')) return i;
            }
            return -1;
        }

        static string BlockScalar(List<Line> lines, ref int i, int indent, string header)
        {
            var folded = header[0] == '>';
            var keep = header.Contains('+');
            var strip = header.Contains('-');
            var collected = new List<string>();
            int? blockIndent = null;
            while (i < lines.Count && (lines[i].Indent < 0 || lines[i].Indent > indent))
            {
                var raw = lines[i].Raw;
                if (lines[i].Indent < 0) { collected.Add(""); i++; continue; }
                blockIndent ??= lines[i].Indent;
                collected.Add(raw.Length >= blockIndent ? raw[blockIndent.Value..] : raw.TrimStart());
                i++;
            }
            while (!keep && collected.Count > 0 && collected[^1].Length == 0) collected.RemoveAt(collected.Count - 1);
            var text = folded ? Regex.Replace(string.Join("\n", collected), "(?<!\n)\n(?!\n)", " ") : string.Join("\n", collected);
            return strip ? text : text + "\n";
        }

        static object Value(string s)
        {
            if (s.StartsWith('[')) return Flow(s, '[', ']').Select(Value).ToList<object>();
            if (s.StartsWith('{'))
            {
                var map = new Dictionary<string, object>();
                foreach (var pair in Flow(s, '{', '}'))
                {
                    var colon = KeyAt(pair);
                    if (colon < 0) map[Unquote(pair)] = null;
                    else map[Unquote(pair[..colon].Trim())] = Value(pair[(colon + 1)..].Trim());
                }
                return map;
            }
            return s is "~" or "null" or "" ? null : Unquote(s);
        }

        // Splits "[a, "b, c", [d]]" at top-level commas.
        static List<string> Flow(string s, char open, char close)
        {
            if (!s.EndsWith(close)) throw new InvalidOperationException($"YAML: unterminated {open} in \"{s}\"");
            var items = new List<string>();
            var depth = 0;
            char quote = '\0';
            var start = 1;
            for (var i = 1; i < s.Length - 1; i++)
            {
                var c = s[i];
                if (quote != '\0') { if (c == quote) quote = '\0'; continue; }
                if (c is '"' or '\'') quote = c;
                else if (c is '[' or '{') depth++;
                else if (c is ']' or '}') depth--;
                else if (c == ',' && depth == 0) { items.Add(s[start..i].Trim()); start = i + 1; }
            }
            var last = s[start..^1].Trim();
            if (last.Length > 0) items.Add(last);
            return items;
        }

        static string Unquote(string s)
        {
            if (s.Length >= 2 && s[0] == '\'' && s[^1] == '\'') return s[1..^1].Replace("''", "'");
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
                return Regex.Replace(s[1..^1], @"\\(.)", m => m.Groups[1].Value switch { "n" => "\n", "t" => "\t", "r" => "\r", "0" => "\0", var x => x });
            return s;
        }
    }
}
