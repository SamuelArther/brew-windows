using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WinTools;

// carthage: Carthage's dependency management, done natively. Cartfile/Cartfile.private are parsed,
// versions are resolved (tags come from the git smart-HTTP ref list, so GitHub needs no API calls
// or git), Cartfile.resolved is written and sources are checked out into Carthage/Checkouts from
// GitHub's zip archives (or with git, for other git origins). The build step installs prebuilt
// frameworks (binary origins and GitHub release assets); compiling Xcode schemes needs xcodebuild,
// which only exists on macOS, so those dependencies are reported and the build exits 1.
static class Carthage
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("carthage", "Decentralized dependency manager for Cocoa", Run);
    }

    const string Help = """
        Available commands:

           archive      Archives built frameworks into a zip that Carthage can use
           bootstrap    Check out and build the project's dependencies
           build        Build the project's dependencies
           checkout     Check out the project's dependencies
           fetch        Clones or fetches a Git repository ahead of time
           help         Display general or command-specific help
           outdated     Check for compatible updates to the project's dependencies
           update       Update and rebuild the project's dependencies
           validate     Validate that the versions in Cartfile.resolved are compatible with the Cartfile requirements
           version      Display the current version of Carthage

        Common options: --project-directory <dir>, --no-build, --no-checkout, --no-use-binaries,
        --use-xcframeworks, --verbose. Xcode-only options (--platform, --configuration, --toolchain,
        --derived-data, --cache-builds, --new-resolver, --log-path) are accepted.

        On Windows, dependencies resolve and check out normally and prebuilt frameworks are installed;
        building Xcode schemes needs xcodebuild, which only runs on macOS.
        """;

    // ---- model ----

    enum Kind { GitHub, Git, Binary }

    sealed record Dependency(Kind Kind, string Location)
    {
        public string Name => Kind switch
        {
            Kind.GitHub when !IsUrl(Location) => Location.Split('/').Last(),
            Kind.Binary => Path.GetFileNameWithoutExtension(new Uri(Location).AbsolutePath),
            _ => Regex.Replace(Location.TrimEnd('/').Split('/', ':').Last(), @"\.git$", ""),
        };

        public override string ToString() => Kind switch
        {
            Kind.GitHub => $"github \"{Location}\"",
            Kind.Git => $"git \"{Location}\"",
            _ => $"binary \"{Location}\"",
        };

        // github "owner/repo"; github "https://ghe.example.com/owner/repo" behaves as a git origin.
        public string GitUrl => Kind == Kind.GitHub && !IsUrl(Location) ? $"https://github.com/{Location}.git"
                              : Kind == Kind.GitHub ? Location.TrimEnd('/') + ".git" : Location;

        public bool OnGitHub => Kind == Kind.GitHub && !IsUrl(Location);
    }

    static bool IsUrl(string s) => s.Contains("://") || s.StartsWith("git@");

    sealed record SemVer(int Major, int Minor, int Patch, string Pre, int Components) : IComparable<SemVer>
    {
        public int CompareTo(SemVer o)
        {
            var c = Major.CompareTo(o.Major);
            if (c == 0) c = Minor.CompareTo(o.Minor);
            if (c == 0) c = Patch.CompareTo(o.Patch);
            if (c != 0) return c;
            if (Pre == null || o.Pre == null) return (Pre == null ? 1 : 0) - (o.Pre == null ? 1 : 0);
            return string.CompareOrdinal(Pre, o.Pre);
        }

        public override string ToString() => $"{Major}.{Minor}.{Patch}{(Pre != null ? "-" + Pre : "")}";

        public static SemVer Parse(string tag)
        {
            var m = Regex.Match(tag, @"^v?(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?$");
            if (!m.Success) return null;
            int Part(int i) => m.Groups[i].Success ? int.Parse(m.Groups[i].Value) : 0;
            var components = m.Groups[3].Success ? 3 : m.Groups[2].Success ? 2 : 1;
            return new SemVer(Part(1), Part(2), Part(3), m.Groups[4].Success ? m.Groups[4].Value : null, components);
        }
    }

    // ~> 1.2 / >= 1.0 / == 2.0 / "branch-or-sha" / nothing.
    sealed record Requirement(string Op, SemVer Version, string Ref)
    {
        public bool Satisfied(SemVer v)
        {
            if (Op == "any") return v.Pre == null;
            if (Op == "==") return v.CompareTo(Version) == 0;
            if (v.Pre != null && !(Version.Pre != null && v.Major == Version.Major && v.Minor == Version.Minor && v.Patch == Version.Patch)) return false;
            if (v.CompareTo(Version) < 0) return false;
            if (Op == ">=") return true;
            // ~>: same major (and same minor when the patch is given or the major is 0).
            if (v.Major != Version.Major) return false;
            return !(Version.Major == 0 || Version.Components == 3) || v.Minor == Version.Minor;
        }

        public override string ToString() => Op switch { "any" => "(any)", "ref" => $"\"{Ref}\"", _ => $"{Op} {Version}" };
    }

    sealed record Pin(string Name, string Sha, SemVer Version);   // Name: the tag or ref written to Cartfile.resolved

    sealed record Entry(Dependency Dependency, Requirement Requirement);

    sealed class Options
    {
        public string ProjectDirectory = ".";
        public bool NoBuild, NoCheckout, UseBinaries = true, UseXcframeworks, Verbose, UseSsh;
        public string Output;
        public List<string> Names = [];
    }

    static readonly HttpClient Http = CreateHttp();

    static HttpClient CreateHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"carthage-wintools/{Commands.Version}");
        return http;
    }

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "-h" or "--help") { Console.WriteLine(Help); return 0; }
        if (args[0] is "version" or "--version") { Console.WriteLine($"0.40.0 (wintools {Commands.Version})"); return 0; }
        var o = ParseOptions(args[0], args[1..]);
        try
        {
            return args[0] switch
            {
                "update" => Update(o),
                "bootstrap" => Bootstrap(o),
                "checkout" => CheckoutCommand(o),
                "build" => Build(o, ReadResolved(o)),
                "outdated" => Outdated(o),
                "validate" => Validate(o),
                "fetch" => Fetch(o),
                "archive" => Archive(o),
                _ => throw new UsageException($"Unrecognized command: '{args[0]}'. See `carthage help`."),
            };
        }
        catch (HttpRequestException e) { throw new InvalidOperationException("network error: " + e.Message); }
        catch (TaskCanceledException) { throw new InvalidOperationException("network request timed out"); }
    }

    static Options ParseOptions(string command, string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(Help); Environment.Exit(0); break;
                case "--project-directory": o.ProjectDirectory = Next(); break;
                case "--no-build": o.NoBuild = true; break;
                case "--no-checkout": o.NoCheckout = true; break;
                case "--use-binaries": o.UseBinaries = true; break;
                case "--no-use-binaries": o.UseBinaries = false; break;
                case "--use-xcframeworks": o.UseXcframeworks = true; break;
                case "--verbose": o.Verbose = true; break;
                case "--use-ssh": o.UseSsh = true; break;
                case "--output": o.Output = Next(); break;
                case "--platform" or "--configuration" or "--toolchain" or "--derived-data" or "--log-path": Next(); break;
                case "--cache-builds" or "--new-resolver" or "--use-netrc" or "--color" or "--no-skip-current" or "--skip-current": break;
                case "--use-submodules":
                    throw new UsageException("--use-submodules needs Carthage's git submodule workflow; check out with plain directories instead");
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"Unrecognized arguments: {args[i]}");
                    o.Names.Add(args[i]);
                    break;
            }
        }
        if (!Directory.Exists(o.ProjectDirectory)) throw new InvalidOperationException($"{o.ProjectDirectory}: no such directory");
        o.ProjectDirectory = Path.GetFullPath(o.ProjectDirectory);
        return o;
    }

    // ---- Cartfile parsing ----

    static List<Entry> ParseCartfile(string text, string file)
    {
        var entries = new List<Entry>();
        var lineNo = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            var line = Regex.Replace(raw, @"#.*$", "").Trim();   // '#' never appears inside Cartfile values
            if (line.Length == 0) continue;
            var m = Regex.Match(line, @"^(github|git|binary)\s+""([^""]+)""\s*(.*)$");
            if (!m.Success) throw new InvalidOperationException($"{file}:{lineNo}: unrecognized line: {raw.Trim()}");
            var kind = m.Groups[1].Value switch { "github" => Kind.GitHub, "git" => Kind.Git, _ => Kind.Binary };
            var location = m.Groups[2].Value;
            if (kind == Kind.GitHub && Regex.IsMatch(location, @"^https?://github\.com/", RegexOptions.IgnoreCase))
                location = Regex.Replace(location, @"^https?://github\.com/|\.git$|/$", "", RegexOptions.IgnoreCase);
            entries.Add(new Entry(new Dependency(kind, location), ParseRequirement(m.Groups[3].Value.Trim(), file, lineNo)));
        }
        return entries;
    }

    static Requirement ParseRequirement(string spec, string file, int line)
    {
        if (spec.Length == 0) return new Requirement("any", null, null);
        var quoted = Regex.Match(spec, @"^""([^""]+)""$");
        if (quoted.Success) return new Requirement("ref", null, quoted.Groups[1].Value);
        var m = Regex.Match(spec, @"^(~>|>=|==)\s*(\S+)$");
        var version = m.Success ? SemVer.Parse(m.Groups[2].Value) : null;
        if (version == null) throw new InvalidOperationException($"{file}:{line}: invalid version requirement: {spec}");
        return new Requirement(m.Groups[1].Value, version, null);
    }

    static List<Entry> ReadRootCartfile(Options o, bool includePrivate = true)
    {
        var entries = new List<Entry>();
        var found = false;
        foreach (var name in includePrivate ? new[] { "Cartfile", "Cartfile.private" } : ["Cartfile"])
        {
            var path = Path.Combine(o.ProjectDirectory, name);
            if (!File.Exists(path)) continue;
            found = true;
            entries.AddRange(ParseCartfile(File.ReadAllText(path), name));
        }
        if (!found) throw new InvalidOperationException($"No Cartfile found in {o.ProjectDirectory}");
        return entries;
    }

    static string ResolvedPath(Options o) => Path.Combine(o.ProjectDirectory, "Cartfile.resolved");

    static List<(Dependency Dependency, Pin Pin)> ReadResolved(Options o)
    {
        var path = ResolvedPath(o);
        if (!File.Exists(path)) throw new InvalidOperationException("No Cartfile.resolved found; run `carthage update` first");
        return ParseCartfile(File.ReadAllText(path), "Cartfile.resolved")
            .Select(e => (e.Dependency, new Pin(e.Requirement.Ref ?? e.Requirement.Version?.ToString(), null,
                                                SemVer.Parse(e.Requirement.Ref ?? ""))))
            .ToList();
    }

    static void WriteResolved(Options o, IEnumerable<(Dependency Dependency, Pin Pin)> pins)
    {
        var sb = new StringBuilder();
        foreach (var (dep, pin) in pins.OrderBy(p => p.Dependency.Kind == Kind.Binary ? 0 : p.Dependency.Kind == Kind.Git ? 1 : 2)
                                       .ThenBy(p => p.Dependency.Location, StringComparer.Ordinal))
            sb.Append($"{dep} \"{pin.Name}\"\n");
        File.WriteAllText(ResolvedPath(o), sb.ToString());
    }

    // ---- remote versions ----

    static readonly Dictionary<string, List<(string Ref, string Sha)>> RefCache = [];

    // `git ls-remote` over smart HTTP: GET <repo>.git/info/refs?service=git-upload-pack and read the
    // pkt-lines. Falls back to git itself for ssh/file origins.
    static List<(string Ref, string Sha)> ListRefs(Dependency dep, Options o)
    {
        var url = dep.GitUrl;
        if (RefCache.TryGetValue(url, out var cached)) return cached;
        var refs = new List<(string, string)>();
        if (url.StartsWith("https://") || url.StartsWith("http://"))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url + "/info/refs?service=git-upload-pack");
            request.Headers.UserAgent.ParseAdd("git/2.47.0");
            using var response = Http.Send(request);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized)
                throw new InvalidOperationException($"Failed to fetch {dep.Name}: repository {url} not found");
            response.EnsureSuccessStatusCode();
            var body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            for (var pos = 0; pos + 4 <= body.Length;)
            {
                var len = Convert.ToInt32(Encoding.ASCII.GetString(body, pos, 4), 16);
                if (len == 0) { pos += 4; continue; }
                var line = Encoding.UTF8.GetString(body, pos + 4, len - 4).TrimEnd('\n');
                pos += len;
                if (line.StartsWith('#')) continue;
                var nul = line.IndexOf('\0');
                if (nul >= 0) line = line[..nul];
                var parts = line.Split(' ', 2);
                if (parts.Length == 2 && parts[0].Length == 40) refs.Add((parts[1], parts[0]));
            }
        }
        else
        {
            foreach (var line in Git(["ls-remote", url]).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('\t');
                if (parts.Length == 2) refs.Add((parts[1].Trim(), parts[0].Trim()));
            }
        }
        return RefCache[url] = refs;
    }

    static List<Pin> ListVersions(Dependency dep, Options o)
    {
        if (dep.Kind == Kind.Binary)
            return BinarySpec(dep).Keys.Select(k => (k, v: SemVer.Parse(k))).Where(x => x.v != null)
                                  .Select(x => new Pin(x.k, null, x.v)).ToList();
        var tags = new Dictionary<string, string>();
        foreach (var (name, sha) in ListRefs(dep, o))
        {
            if (!name.StartsWith("refs/tags/")) continue;
            var tag = name["refs/tags/".Length..];
            // Annotated tags are listed twice; the peeled ^{} line holds the commit.
            if (tag.EndsWith("^{}")) tags[tag[..^3]] = sha;
            else tags.TryAdd(tag, sha);
        }
        return tags.Select(t => new Pin(t.Key, t.Value, SemVer.Parse(t.Key))).Where(p => p.Version != null).ToList();
    }

    static Pin ResolveRef(Dependency dep, string reference, Options o)
    {
        if (dep.Kind == Kind.Binary) throw new InvalidOperationException($"binary dependency {dep.Name} can't be pinned to \"{reference}\"");
        foreach (var prefix in new[] { "refs/heads/", "refs/tags/" })
        {
            var refs = ListRefs(dep, o);
            var hit = refs.FirstOrDefault(r => r.Ref == prefix + reference + "^{}");
            if (hit.Sha == null) hit = refs.FirstOrDefault(r => r.Ref == prefix + reference);
            if (hit.Sha != null) return new Pin(hit.Sha, hit.Sha, null);
        }
        if (Regex.IsMatch(reference, "^[0-9a-f]{7,40}$")) return new Pin(reference, reference, null);
        throw new InvalidOperationException($"No tag, branch or commit \"{reference}\" found in {dep.Name}");
    }

    static readonly Dictionary<string, Dictionary<string, string>> BinarySpecs = [];

    // A binary origin is JSON: { "1.0.0": "https://.../Framework.zip", ... }.
    static Dictionary<string, string> BinarySpec(Dependency dep)
    {
        if (BinarySpecs.TryGetValue(dep.Location, out var spec)) return spec;
        var text = dep.Location.StartsWith("file://") ? File.ReadAllText(new Uri(dep.Location).LocalPath)
                                                      : Http.GetStringAsync(dep.Location).GetAwaiter().GetResult();
        spec = [];
        try
        {
            using var doc = JsonDocument.Parse(text);
            foreach (var p in doc.RootElement.EnumerateObject())
                if (p.Value.ValueKind == JsonValueKind.String) spec[p.Name] = p.Value.GetString();
        }
        catch (JsonException e) { throw new InvalidOperationException($"{dep.Location}: invalid binary specification ({e.Message})"); }
        return BinarySpecs[dep.Location] = spec;
    }

    // The dependency's own Cartfile at a version (Cartfile.private is never inherited).
    static List<Entry> RemoteCartfile(Dependency dep, Pin pin, Options o)
    {
        if (dep.Kind == Kind.Binary) return [];
        string text;
        if (dep.OnGitHub)
        {
            using var response = Http.GetAsync($"https://raw.githubusercontent.com/{dep.Location}/{pin.Sha ?? pin.Name}/Cartfile").GetAwaiter().GetResult();
            if (response.StatusCode == HttpStatusCode.NotFound) return [];
            response.EnsureSuccessStatusCode();
            text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        else
        {
            var cache = GitCache(dep);
            try { text = Git(["--git-dir", cache, "show", $"{pin.Sha ?? pin.Name}:Cartfile"]); }
            catch (InvalidOperationException) { return []; }
        }
        return ParseCartfile(text, $"{dep.Name}/Cartfile");
    }

    // ---- resolution ----

    // Pick versions until nothing changes: every pinned dependency contributes the requirements of
    // its own Cartfile, and each dependency gets the newest version that satisfies all of them.
    static List<(Dependency Dependency, Pin Pin)> Resolve(Options o, List<Entry> root, Dictionary<string, Pin> locked)
    {
        var pins = new Dictionary<string, (Dependency Dependency, Pin Pin)>();
        var cartfiles = new Dictionary<string, List<Entry>>();
        var fetched = new HashSet<string>();
        for (var round = 0; round < 64; round++)
        {
            var requirements = new Dictionary<string, (Dependency Dependency, List<(Requirement Req, string From)> Reqs)>();
            void AddAll(IEnumerable<Entry> entries, string from)
            {
                foreach (var e in entries)
                {
                    if (!requirements.TryGetValue(e.Dependency.Name, out var r)) requirements[e.Dependency.Name] = r = (e.Dependency, []);
                    r.Reqs.Add((e.Requirement, from));
                }
            }
            AddAll(root, "Cartfile");
            foreach (var (name, (dep, pin)) in pins)
            {
                var key = $"{name}@{pin.Name}";
                if (!cartfiles.TryGetValue(key, out var entries)) cartfiles[key] = entries = RemoteCartfile(dep, pin, o);
                AddAll(entries, name);
            }

            var changed = false;
            foreach (var (name, (dep, reqs)) in requirements)
            {
                if (fetched.Add(name)) Console.WriteLine($"*** Fetching {name}");
                var pin = Choose(dep, reqs, locked.GetValueOrDefault(name), o);
                if (!pins.TryGetValue(name, out var current) || current.Pin.Name != pin.Name) { pins[name] = (dep, pin); changed = true; }
            }
            foreach (var stale in pins.Keys.Where(k => !requirements.ContainsKey(k)).ToList()) { pins.Remove(stale); changed = true; }
            if (!changed) return [.. pins.Values];
        }
        throw new InvalidOperationException("Could not resolve dependencies: versions keep changing (circular requirements?)");
    }

    static Pin Choose(Dependency dep, List<(Requirement Req, string From)> reqs, Pin locked, Options o)
    {
        var refs = reqs.Where(r => r.Req.Op == "ref").Select(r => r.Req.Ref).Distinct().ToList();
        if (refs.Count > 1)
            throw new InvalidOperationException($"Could not pick a version for {dep.Name}, due to mutually incompatible requirements:\n" +
                                                string.Join("\n", reqs.Select(r => $"\t{r.Req} (from {r.From})")));
        if (refs.Count == 1)
        {
            if (locked != null && !o.Names.Contains(dep.Name) && o.Names.Count > 0) return locked;
            return ResolveRef(dep, refs[0], o);
        }
        // Updating only some dependencies keeps the others at their resolved version when it still fits.
        if (locked?.Version != null && o.Names.Count > 0 && !o.Names.Contains(dep.Name) && reqs.All(r => r.Req.Satisfied(locked.Version)))
            return locked;
        var best = ListVersions(dep, o).Where(p => reqs.All(r => r.Req.Satisfied(p.Version))).MaxBy(p => p.Version);
        if (best == null)
            throw new InvalidOperationException($"Could not pick a version for {dep.Name}, due to mutually incompatible requirements:\n" +
                                                string.Join("\n", reqs.Select(r => $"\t{r.Req} (from {r.From})")));
        return best;
    }

    // ---- commands ----

    static int Update(Options o)
    {
        var root = ReadRootCartfile(o);
        var locked = new Dictionary<string, Pin>();
        if (o.Names.Count > 0 && File.Exists(ResolvedPath(o)))
            foreach (var (dep, pin) in ReadResolved(o)) locked[dep.Name] = pin;
        foreach (var name in o.Names)
            if (!root.Any(e => e.Dependency.Name == name) && !locked.ContainsKey(name))
                throw new InvalidOperationException($"Unknown dependency: {name}");
        var resolved = Resolve(o, root, locked);
        WriteResolved(o, resolved);
        if (!o.NoCheckout) Checkout(o, resolved);
        return o.NoBuild ? 0 : Build(o, resolved);
    }

    static int Bootstrap(Options o)
    {
        if (!File.Exists(ResolvedPath(o))) return Update(o);
        var resolved = ReadResolved(o);
        if (o.Names.Count > 0) resolved = resolved.Where(r => o.Names.Contains(r.Dependency.Name)).ToList();
        if (!o.NoCheckout) Checkout(o, resolved);
        return o.NoBuild ? 0 : Build(o, resolved);
    }

    static int CheckoutCommand(Options o)
    {
        var resolved = ReadResolved(o);
        if (o.Names.Count > 0) resolved = resolved.Where(r => o.Names.Contains(r.Dependency.Name)).ToList();
        Checkout(o, resolved);
        return 0;
    }

    static void Checkout(Options o, List<(Dependency Dependency, Pin Pin)> resolved)
    {
        var checkouts = Path.Combine(o.ProjectDirectory, "Carthage", "Checkouts");
        foreach (var (dep, pin) in resolved)
        {
            if (dep.Kind == Kind.Binary) continue;
            Console.WriteLine($"*** Checking out {dep.Name} at \"{pin.Name}\"");
            var dest = Path.Combine(checkouts, dep.Name);
            var staging = Path.Combine(Path.GetTempPath(), $"carthage-{Environment.ProcessId}-{dep.Name}");
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);
            try
            {
                var zip = Path.Combine(staging, "source.zip");
                if (dep.OnGitHub) Download($"https://codeload.github.com/{dep.Location}/zip/{pin.Sha ?? pin.Name}", zip);
                else Git(["--git-dir", GitCache(dep), "archive", "--format=zip", "-o", zip, pin.Sha ?? pin.Name]);
                var tree = Path.Combine(staging, "tree");
                ZipFile.ExtractToDirectory(zip, tree);
                // GitHub archives wrap everything in "<repo>-<ref>/"; git archive does not.
                var entries = Directory.GetFileSystemEntries(tree);
                var top = dep.OnGitHub && entries.Length == 1 && Directory.Exists(entries[0]) ? entries[0] : tree;
                if (Directory.Exists(dest)) Directory.Delete(dest, true);
                Directory.CreateDirectory(checkouts);
                Directory.Move(top, dest);
            }
            finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
        }
    }

    static int Build(Options o, List<(Dependency Dependency, Pin Pin)> resolved)
    {
        if (o.Names.Count > 0) resolved = resolved.Where(r => o.Names.Contains(r.Dependency.Name)).ToList();
        var buildDir = Path.Combine(o.ProjectDirectory, "Carthage", "Build");
        var needXcode = new List<string>();
        foreach (var (dep, pin) in resolved)
        {
            if (dep.Kind == Kind.Binary)
            {
                var spec = BinarySpec(dep);
                var url = spec.GetValueOrDefault(pin.Name) ?? spec.FirstOrDefault(kv => SemVer.Parse(kv.Key)?.CompareTo(pin.Version) == 0).Value
                          ?? throw new InvalidOperationException($"{dep.Name}: version {pin.Name} is not in {dep.Location}");
                Console.WriteLine($"*** Downloading binary-only framework {dep.Name} at \"{pin.Name}\"");
                InstallFrameworkZip(url.Split("?alt=")[0], buildDir, o);
                continue;
            }
            if (o.UseBinaries && dep.OnGitHub && pin.Version != null && TryReleaseBinary(dep, pin, buildDir, o)) continue;

            var checkout = Path.Combine(o.ProjectDirectory, "Carthage", "Checkouts", dep.Name);
            if (!Directory.Exists(checkout))
            {
                Console.WriteLine($"*** Skipped building {dep.Name}: it is not checked out (run carthage checkout)");
                continue;
            }
            if (!HasFrameworkSchemes(checkout))
            {
                Console.WriteLine($"*** Skipped building {dep.Name} due to the error:\nDependency \"{dep.Name}\" has no shared framework schemes");
                continue;
            }
            needXcode.Add(dep.Name);
        }
        if (needXcode.Count == 0) return 0;
        Console.Error.WriteLine($"carthage: building the Xcode framework schemes of {string.Join(", ", needXcode)} needs xcodebuild, which only runs on macOS");
        return 1;
    }

    // Projects whose shared schemes build a framework target (what Carthage builds).
    static bool HasFrameworkSchemes(string checkout)
    {
        foreach (var project in Directory.EnumerateDirectories(checkout, "*.xcodeproj", SearchOption.AllDirectories))
        {
            if (Path.GetRelativePath(checkout, project).Contains(Path.Combine("Carthage", "Checkouts"))) continue;
            var schemes = Path.Combine(project, "xcshareddata", "xcschemes");
            var pbx = Path.Combine(project, "project.pbxproj");
            if (Directory.Exists(schemes) && Directory.EnumerateFiles(schemes, "*.xcscheme").Any() &&
                File.Exists(pbx) && File.ReadAllText(pbx).Contains("com.apple.product-type.framework"))
                return true;
        }
        return false;
    }

    // Carthage's --use-binaries: a GitHub release for the tag with a *.framework.zip/*.xcframework.zip asset.
    static bool TryReleaseBinary(Dependency dep, Pin pin, string buildDir, Options o)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{dep.Location}/releases/tags/{Uri.EscapeDataString(pin.Name)}");
        var token = Environment.GetEnvironmentVariable("GITHUB_ACCESS_TOKEN") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new("Bearer", token);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        try
        {
            using var response = Http.Send(request);
            if (!response.IsSuccessStatusCode) return false;
            using var doc = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var assets = doc.RootElement.GetProperty("assets").EnumerateArray()
                .Select(a => (Name: a.GetProperty("name").GetString(), Url: a.GetProperty("browser_download_url").GetString()))
                .Where(a => a.Name.EndsWith(".framework.zip") || a.Name.EndsWith(".xcframework.zip")).ToList();
            var pick = assets.FirstOrDefault(a => a.Name.EndsWith(".xcframework.zip") == o.UseXcframeworks);
            if (pick.Url == null) pick = assets.FirstOrDefault();
            if (pick.Url == null) return false;
            Console.WriteLine($"*** Downloading {dep.Name} binary at \"{pin.Name}\"");
            InstallFrameworkZip(pick.Url, buildDir, o);
            return true;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            if (o.Verbose) Console.Error.WriteLine($"*** No binary for {dep.Name}: {e.Message}");
            return false;
        }
    }

    // Framework zips either hold "Carthage/Build/..." paths or bare *.framework/*.xcframework folders.
    static void InstallFrameworkZip(string url, string buildDir, Options o)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"carthage-{Environment.ProcessId}-binary.zip");
        try
        {
            if (url.StartsWith("file://")) File.Copy(new Uri(url).LocalPath, temp, true);
            else Download(url, temp);
            using var zip = ZipFile.OpenRead(temp);
            foreach (var entry in zip.Entries)
            {
                var name = entry.FullName.Replace('\\', '/');
                if (name.StartsWith("__MACOSX/")) continue;
                var idx = name.IndexOf("Carthage/Build/", StringComparison.Ordinal);
                var relative = idx >= 0 ? name[(idx + "Carthage/Build/".Length)..] : name;
                if (relative.Length == 0) continue;
                var dest = Path.GetFullPath(Path.Combine(buildDir, relative));
                if (!dest.StartsWith(Path.GetFullPath(buildDir), StringComparison.OrdinalIgnoreCase)) continue;
                if (name.EndsWith('/')) { Directory.CreateDirectory(dest); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                entry.ExtractToFile(dest, true);
            }
        }
        finally { File.Delete(temp); }
    }

    static int Outdated(Options o)
    {
        var root = ReadRootCartfile(o);
        var resolved = ReadResolved(o);
        // Requirements also come from the resolved dependencies' own Cartfiles.
        var inherited = resolved.SelectMany(r => RemoteCartfile(r.Dependency, r.Pin, o)).ToList();
        var lines = new List<string>();
        foreach (var (dep, pin) in resolved)
        {
            if (pin.Version == null) continue;   // pinned to a branch or commit
            Console.WriteLine($"*** Fetching {dep.Name}");
            var versions = ListVersions(dep, o);
            var reqs = root.Concat(inherited).Where(e => e.Dependency.Name == dep.Name).Select(e => e.Requirement).ToList();
            var applicable = versions.Where(v => reqs.All(r => r.Op == "ref" || r.Satisfied(v.Version))).MaxBy(v => v.Version);
            var latest = versions.Where(v => v.Version.Pre == null).MaxBy(v => v.Version);
            if (latest == null) continue;
            if (pin.Version.CompareTo(latest.Version) < 0 || (applicable != null && pin.Version.CompareTo(applicable.Version) < 0))
                lines.Add($"{dep.Name} \"{pin.Name}\" -> \"{applicable?.Name ?? pin.Name}\" (Latest: \"{latest.Name}\")");
        }
        if (lines.Count == 0) { Console.WriteLine("All dependencies are up to date."); return 0; }
        Console.WriteLine("The following dependencies are outdated:");
        foreach (var line in lines) Console.WriteLine(line);
        return 0;
    }

    static int Validate(Options o)
    {
        var root = ReadRootCartfile(o);
        var resolved = ReadResolved(o).ToDictionary(r => r.Dependency.Name, r => r.Pin);
        var problems = new List<string>();
        foreach (var e in root)
        {
            if (!resolved.TryGetValue(e.Dependency.Name, out var pin)) { problems.Add($"{e.Dependency.Name} is missing from Cartfile.resolved"); continue; }
            if (e.Requirement.Op is "any" or "ref") continue;
            if (pin.Version == null || !e.Requirement.Satisfied(pin.Version))
                problems.Add($"{e.Dependency.Name}: \"{pin.Name}\" doesn't satisfy {e.Requirement}");
        }
        if (problems.Count == 0) { Console.WriteLine("No incompatibilities found in Cartfile.resolved"); return 0; }
        Console.Error.WriteLine("The following incompatibilities were found in Cartfile.resolved:");
        foreach (var p in problems) Console.Error.WriteLine("  " + p);
        return 1;
    }

    // fetch <repository URL>: list the repository's versions (and mirror it for git origins).
    static int Fetch(Options o)
    {
        if (o.Names.Count != 1) throw new UsageException("Usage: carthage fetch <repository URL>");
        var url = o.Names[0];
        var dep = Regex.IsMatch(url, @"^[\w.-]+/[\w.-]+$") ? new Dependency(Kind.GitHub, url) : new Dependency(Kind.Git, url);
        Console.WriteLine($"*** Fetching {dep.Name}");
        if (!dep.OnGitHub && Directory.Exists(GitCache(dep))) Console.WriteLine($"*** Cached in {GitCache(dep)}");
        foreach (var pin in ListVersions(dep, o).OrderByDescending(p => p.Version)) Console.WriteLine($"{pin.Name}\t{pin.Sha}");
        return 0;
    }

    // archive: zip Carthage/Build frameworks so they can be attached to a GitHub release.
    static int Archive(Options o)
    {
        var buildDir = Path.Combine(o.ProjectDirectory, "Carthage", "Build");
        if (!Directory.Exists(buildDir)) throw new InvalidOperationException("Carthage/Build does not exist; nothing to archive");
        var frameworks = Directory.EnumerateDirectories(buildDir, "*", SearchOption.AllDirectories)
            .Where(d => d.EndsWith(".framework") || d.EndsWith(".xcframework"))
            // Only outermost bundles: an xcframework's per-platform .framework folders travel inside it.
            .Where(d => !Path.GetRelativePath(buildDir, d).Split(Path.DirectorySeparatorChar).SkipLast(1)
                             .Any(s => s.EndsWith(".framework") || s.EndsWith(".xcframework")))
            .Where(d => o.Names.Count == 0 || o.Names.Contains(Path.GetFileNameWithoutExtension(d)))
            .ToList();
        if (frameworks.Count == 0) throw new InvalidOperationException("Could not find any frameworks to archive");
        var output = o.Output ?? Path.Combine(o.ProjectDirectory,
            (o.Names.Count == 1 ? o.Names[0] : Path.GetFileNameWithoutExtension(frameworks[0])) +
            (frameworks.All(f => f.EndsWith(".xcframework")) ? ".xcframework.zip" : ".framework.zip"));
        if (File.Exists(output)) File.Delete(output);
        using (var zip = ZipFile.Open(output, ZipArchiveMode.Create))
            foreach (var framework in frameworks)
                foreach (var file in Directory.EnumerateFiles(framework, "*", SearchOption.AllDirectories))
                {
                    var relative = "Carthage/Build/" + Path.GetRelativePath(buildDir, file).Replace('\\', '/');
                    zip.CreateEntryFromFile(file, relative);
                    if (o.Verbose) Console.WriteLine(relative);
                }
        Console.WriteLine($"*** Created {output}");
        return 0;
    }

    // ---- helpers ----

    static void Download(string url, string file)
    {
        using var response = Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"download failed ({(int)response.StatusCode}): {url}");
        using var input = response.Content.ReadAsStream();
        using var output = File.Create(file);
        input.CopyTo(output);
    }

    // Non-GitHub git origins are mirrored where Carthage keeps them.
    static string GitCache(Dependency dep)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "org.carthage.CarthageKit", "dependencies", dep.Name);
        if (Directory.Exists(dir)) Git(["--git-dir", dir, "fetch", "--prune", "--quiet", dep.GitUrl, "+refs/heads/*:refs/heads/*", "+refs/tags/*:refs/tags/*"]);
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dir));
            Git(["clone", "--mirror", "--quiet", dep.GitUrl, dir]);
        }
        return dir;
    }

    static string Git(IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        Process p;
        try { p = Process.Start(psi); }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException("git is needed for non-GitHub git dependencies but was not found on PATH (winget install Git.Git)");
        }
        using (p)
        {
            var stderr = p.StandardError.ReadToEndAsync();
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) throw new InvalidOperationException($"git {string.Join(' ', psi.ArgumentList)} failed: {stderr.Result.Trim()}");
            return stdout;
        }
    }
}
