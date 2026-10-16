using System.Globalization;
using System.Runtime.CompilerServices;

namespace WinTools;

// xcp (XcodeProjectCLI): manage the files, groups, targets, build settings and asset catalogs of an
// Xcode project from the command line. Edits project.pbxproj directly (and the files on disk unless
// --project-only), so it works on Windows checkouts of iOS/macOS projects.
static class Xcp
{
    const string Version = "1.2.3";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("xcp", "Manage Xcode project files, groups, targets and assets", Run);

    // Bad usage: "Error: <message>" plus a usage hint, exit 64 (ArgumentParser's ValidationError).
    sealed class XcpUsage(string message) : Exception(message);

    static readonly (string Name, string Abstract, string Usage)[] Subcommands =
    [
        ("list-targets", "List project targets.", "[<xcode-project>] [--file <file>] [--group <group>]"),
        ("set-target", "Set target for existing file.", "[<xcode-project>] [--project-only] --file <file> --targets <targets>"),
        ("add-group", "Add group.", "[<xcode-project>] [--project-only] --group <group-path> [--create-groups]"),
        ("delete-group", "Delete group.", "[<xcode-project>] [--project-only] --group <group-path>"),
        ("move-group", "Move group.", "[<xcode-project>] [--project-only] --group <group-path> --dest <group-path>"),
        ("rename-group", "Rename group.", "[<xcode-project>] [--project-only] --group <group-path> --name <name>"),
        ("add-file", "Add file to specified targets.", "[<xcode-project>] [--project-only] --file <file-path> [--targets <targets>] [--guess-target] [--create-groups] [--print-targets]"),
        ("delete-file", "Delete file.", "[<xcode-project>] [--project-only] --file <file-path>"),
        ("move-file", "Move file.", "[<xcode-project>] [--project-only] --file <file-path> --dest <file-path> [--print-targets]"),
        ("rename-file", "Rename file.", "[<xcode-project>] [--project-only] --file <file-path> --name <name>"),
        ("get-build-setting", "Get build setting.", "[<xcode-project>] --target <target> [--config <config>] --key <key>"),
        ("set-build-setting", "Set build setting.", "[<xcode-project>] [--targets <targets>] [--configs <configs>] [--append] --key <key> --value <value>"),
        ("add-image-asset", "Add image asset.", "<xcassets-path> --file <file-path> [--dark-file <file-path>] --asset-path <asset-path> [--mode <default|template|original>]"),
        ("add-data-asset", "Add data asset.", "<xcassets-path> --file <file> --asset-path <asset-path>"),
        ("add-color-asset", "Add color asset.", "<xcassets-path> --color <color> [--dark-color <dark-color>] [--color-space <color-space>] --asset-path <asset-path>"),
        ("list-assets", "List all assets.", "<xcassets-path> [--type <color|data|image>]"),
        ("move-asset", "Move asset.", "<xcassets-path> --asset-path <asset-path> --dest <dest>"),
        ("delete-asset", "Delete asset.", "<xcassets-path> --asset-path <asset-path>"),
    ];

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" || args[0] == "help" && args.Length == 1) { Help(); return 0; }
        if (args[0] == "--version") { Console.WriteLine(Version); return 0; }
        var command = args[0];
        if (command == "help") { command = args[1]; args = [command, "--help"]; }
        var sub = Subcommands.FirstOrDefault(s => s.Name == command);
        if (sub.Name == null)
        {
            XcOut.Error($"Error: Unexpected argument '{command}'\nUsage: xcp <subcommand>\n  See 'xcp --help' for more information.\n");
            return 64;
        }
        var opts = new Opts(args[1..]);
        if (opts.Has("help")) { Console.WriteLine($"OVERVIEW: {sub.Abstract}\n\nUSAGE: xcp {sub.Name} {sub.Usage}"); return 0; }
        try
        {
            return command switch
            {
                "list-targets" => ListTargets(opts),
                "set-target" => SetTarget(opts),
                "add-group" => AddGroup(opts),
                "delete-group" => DeleteGroup(opts),
                "move-group" => MoveGroup(opts),
                "rename-group" => RenameGroup(opts),
                "add-file" => AddFile(opts),
                "delete-file" => DeleteFile(opts),
                "move-file" => MoveFile(opts),
                "rename-file" => RenameFile(opts),
                "get-build-setting" => GetBuildSetting(opts),
                "set-build-setting" => SetBuildSetting(opts),
                "add-image-asset" => AddImageAsset(opts),
                "add-data-asset" => AddDataAsset(opts),
                "add-color-asset" => AddColorAsset(opts),
                "list-assets" => ListAssets(opts),
                "move-asset" => MoveAsset(opts),
                "delete-asset" => DeleteAsset(opts),
                _ => 64,
            };
        }
        catch (XcpUsage e)
        {
            XcOut.Error($"Error: {e.Message}\nUsage: xcp {sub.Name} {sub.Usage}\n  See 'xcp help {sub.Name}' for more information.\n");
            return 64;
        }
        catch (Exception e) when (e is XcProjException or IOException or UnauthorizedAccessException)
        {
            XcOut.Error($"Error: {e.Message}\n");
            return 1;
        }
    }

    static void Help() => Console.WriteLine($"""
        OVERVIEW: XcodeProjectCLI

        USAGE: xcp <subcommand>

        OPTIONS:
          --version               Show the version.
          -h, --help              Show help information.

        TARGET SUBCOMMANDS:
        {Lines("list-targets", "set-target")}

        GROUP SUBCOMMANDS:
        {Lines("add-group", "delete-group", "move-group", "rename-group")}

        FILE SUBCOMMANDS:
        {Lines("add-file", "delete-file", "move-file", "rename-file")}

        BUILD SETTINGS SUBCOMMANDS:
        {Lines("get-build-setting", "set-build-setting")}

        ASSETS SUBCOMMANDS:
        {Lines("add-image-asset", "add-data-asset", "add-color-asset", "list-assets", "move-asset", "delete-asset")}

          See 'xcp help <subcommand>' for detailed help.
        """);

    static string Lines(params string[] names) =>
        string.Join("\n", names.Select(n => "  " + n.PadRight(22) + Subcommands.First(s => s.Name == n).Abstract));

    // --name value / --flag options and positional arguments.
    sealed class Opts
    {
        readonly Dictionary<string, string> values = [];
        readonly HashSet<string> flags = [];
        public readonly List<string> Positional = [];
        static readonly HashSet<string> Flags = ["project-only", "guess-target", "create-groups", "print-targets", "append", "help"];

        public Opts(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a is "-h") { flags.Add("help"); continue; }
                if (!a.StartsWith("--")) { Positional.Add(a); continue; }
                var name = a[2..];
                string inline = null;
                if (name.Contains('=')) { inline = name[(name.IndexOf('=') + 1)..]; name = name[..name.IndexOf('=')]; }
                if (Flags.Contains(name)) { flags.Add(name); continue; }
                values[name] = inline ?? (i + 1 < args.Length ? args[++i] : throw new XcpUsage($"Missing value for '--{name}'"));
            }
        }

        public bool Has(string flag) => flags.Contains(flag);
        public string Get(string name) => values.GetValueOrDefault(name);
        public string Require(string name) => Get(name) ?? throw new XcpUsage($"Missing expected argument '--{name} <{name}>'");
        public string ProjectArg => Positional.FirstOrDefault();
    }

    // ---- project + paths ----

    sealed class Proj
    {
        public XcProjFile File;
        public string Root;   // the folder holding the .xcodeproj

        public static Proj Open(string projectPath)
        {
            var cwd = Directory.GetCurrentDirectory();
            string path;
            if (projectPath == null)
            {
                var found = Directory.GetDirectories(cwd, "*.xcodeproj").Order(StringComparer.Ordinal).FirstOrDefault();
                path = found ?? cwd;
            }
            else path = Path.GetFullPath(projectPath);
            var file = XcProjFile.Load(path);
            return new Proj { File = file, Root = Path.GetDirectoryName(Path.GetFullPath(path).TrimEnd('\\', '/')) };
        }

        // A path from the command line, relative to the project root with forward slashes.
        public string Rel(string input)
        {
            var p = input.Replace('\\', '/').TrimEnd('/');
            if (Path.IsPathRooted(input))
            {
                var root = Root.Replace('\\', '/').TrimEnd('/') + "/";
                var full = Path.GetFullPath(input).Replace('\\', '/');
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return XcProjFile.Normalize(full[root.Length..]);
                return XcProjFile.Normalize(full);
            }
            return XcProjFile.Normalize(p);
        }

        public string Abs(string rel) => Path.IsPathRooted(rel) ? rel : Path.GetFullPath(Path.Combine(Root, rel));

        public void Save() => File.Save();
    }

    static string Dir(string rel) => rel.Contains('/') ? rel[..rel.LastIndexOf('/')] : "";
    static string Last(string rel) => rel.Contains('/') ? rel[(rel.LastIndexOf('/') + 1)..] : rel;
    static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    static string FindFile(Proj p, string rel) =>
        p.File.ObjectsOfIsa("PBXFileReference").FirstOrDefault(f => p.File.FullPath(f) == rel);

    static IEnumerable<string> GroupChildren(XcProjFile f, string group) =>
        f.Ids(f.Obj(group), "children").Where(c => f.Isa(c) is "PBXGroup" or "PBXVariantGroup" or "XCVersionGroup");

    static IEnumerable<string> AllGroups(XcProjFile f, string group)
    {
        foreach (var child in GroupChildren(f, group))
        {
            yield return child;
            foreach (var nested in AllGroups(f, child)) yield return nested;
        }
    }

    static string FindGroup(Proj p, string rel)
    {
        var main = p.File.MainGroup ?? throw new XcProjException("Root group not found in the project.");
        return AllGroups(p.File, main).FirstOrDefault(g => p.File.FullPath(g) == rel);
    }

    static string Subgroup(XcProjFile f, string group, string name) =>
        GroupChildren(f, group).FirstOrDefault(g => XcProjFile.Str(f.Obj(g), "path") is { } path && Last(path.Replace('\\', '/')) == name);

    static string CreateGroupHierarchy(Proj p, string rel)
    {
        var current = p.File.MainGroup ?? throw new XcProjException("Root group not found in the project.");
        foreach (var component in rel.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var existing = Subgroup(p.File, current, component);
            if (existing == null)
            {
                existing = p.File.Add(XcProjFile.NewObject("PBXGroup", ("children", new List<object>()), ("path", component), ("sourceTree", "<group>")));
                AddChild(p.File, current, existing);
            }
            current = existing;
        }
        return current;
    }

    static void AddChild(XcProjFile f, string group, string child)
    {
        var o = f.Obj(group);
        if (XcPlist.List(o, "children") is not { } children) o["children"] = children = [];
        children.Add(child);
        f.InvalidateParents();
    }

    static void RemoveChild(XcProjFile f, string child)
    {
        var parent = f.Parent(child);
        if (parent != null) XcPlist.List(f.Obj(parent), "children")?.Remove(child);
        f.InvalidateParents();
    }

    static void SetGroupSourceTree(XcProjFile f, string element)
    {
        var o = f.Obj(element);
        if (XcProjFile.Str(o, "sourceTree") != "<group>")
        {
            o["sourceTree"] = "<group>";
            o.Remove("name");
            if (XcProjFile.Str(o, "path") is { } path) o["path"] = Last(path.Replace('\\', '/'));
        }
        foreach (var child in f.Ids(o, "children")) SetGroupSourceTree(f, child);
    }

    // ---- targets ----

    static List<string> NativeTargets(XcProjFile f) => f.ObjectsOfIsa("PBXNativeTarget").OrderBy(f.TargetName, StringComparer.Ordinal).ToList();

    static IEnumerable<string> TargetFiles(XcProjFile f, string target)
    {
        foreach (var isa in new[] { "PBXSourcesBuildPhase", "PBXResourcesBuildPhase" })
            if (f.Phase(target, isa) is { } phase)
                foreach (var bf in f.PhaseFiles(phase))
                    if (f.BuildFileRef(bf) is { } file) yield return file;
    }

    static Dictionary<string, List<string>> FileToTargets(XcProjFile f)
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var t in f.ObjectsOfIsa("PBXNativeTarget"))
            foreach (var file in TargetFiles(f, t))
                if (f.FullPath(file) is { Length: > 0 } path)
                {
                    if (!map.TryGetValue(path, out var list)) map[path] = list = [];
                    if (!list.Contains(t)) list.Add(t);
                }
        return map;
    }

    static List<string> FindTargets(XcProjFile f, string rel) =>
        FileToTargets(f).GetValueOrDefault(rel)?.OrderBy(f.TargetName, StringComparer.Ordinal).ToList() ?? [];

    static List<string> GuessTargetsForGroup(Proj p, string groupRel, bool fallbackToParent = true)
    {
        var f = p.File;
        var group = FindGroup(p, groupRel) ?? throw new XcProjException(GroupNotInProject(p, groupRel));
        var map = FileToTargets(f);
        var swiftFiles = f.Ids(f.Obj(group), "children").Where(c => XcProjFile.Str(f.Obj(c), "path")?.EndsWith("swift") == true)
            .Select(f.FullPath).Where(x => x != null).ToList();
        foreach (var file in swiftFiles)
            if (map.TryGetValue(file, out var targets) && targets.Count > 0) return targets.OrderBy(f.TargetName, StringComparer.Ordinal).ToList();
        if (swiftFiles.Count == 0 && fallbackToParent && f.Parent(group) is { } parent && f.FullPath(parent) is { } parentPath)
        {
            var result = GuessTargetsForGroup(p, parentPath, false);
            if (result.Count > 0) return result;
        }
        var first = groupRel.Split('/')[0];
        return f.Targets.Where(t => f.TargetName(t) == first).ToList();
    }

    static string FileNotInProject(Proj p, string rel) => $"File not found in the project: {p.Abs(rel)}";
    static string FileNotOnDisk(Proj p, string rel) => $"File not found on disk: {p.Abs(rel)}";
    static string GroupNotInProject(Proj p, string rel) => $"Group not found in the project: {p.Abs(rel)}. Use --create-groups to create missing groups.";
    static string GroupNotOnDisk(Proj p, string rel) => $"Group not found on disk: {p.Abs(rel)}";

    static void RemoveFromTarget(XcProjFile f, string rel, string target)
    {
        foreach (var isa in new[] { "PBXSourcesBuildPhase", "PBXResourcesBuildPhase" })
        {
            if (f.Phase(target, isa) is not { } phase || XcPlist.List(f.Obj(phase), "files") is not { } files) continue;
            foreach (var bf in files.OfType<string>().ToList())
                if (f.BuildFileRef(bf) is { } file && f.FullPath(file) == rel) { files.Remove(bf); f.Remove(bf); }
        }
    }

    static void SetTargets(Proj p, List<string> targets, string rel)
    {
        var f = p.File;
        var file = FindFile(p, rel) ?? throw new XcProjException(FileNotInProject(p, rel));
        var dest = targets.SelectMany(n => f.Targets.Where(t => f.TargetName(t) == n)).ToList();
        if (dest.Count < targets.Count)
            throw new XcProjException($"Targets not found in the project: {string.Join(", ", targets.Except(dest.Select(f.TargetName)))}");
        foreach (var t in FindTargets(f, rel))
            if (!targets.Contains(f.TargetName(t))) RemoveFromTarget(f, rel, t);
        var ext = Path.GetExtension(rel).TrimStart('.').ToLowerInvariant();
        var isSource = ext is "swift" or "m" or "mm" or "c" or "cpp" or "h" or "hpp";
        foreach (var t in dest)
        {
            var phase = f.Phase(t, isSource ? "PBXSourcesBuildPhase" : "PBXResourcesBuildPhase");
            if (phase == null) continue;
            var o = f.Obj(phase);
            if (XcPlist.List(o, "files") is not { } files) o["files"] = files = [];
            if (files.OfType<string>().Any(bf => f.BuildFileRef(bf) == file)) continue;
            files.Add(f.Add(XcProjFile.NewObject("PBXBuildFile", ("fileRef", file))));
        }
    }

    static List<string> ParseList(string value) => value?.Split(',').Where(v => v.Length > 0).ToList() ?? [];

    // ---- commands ----

    static int ListTargets(Opts o)
    {
        var file = o.Get("file");
        var group = o.Get("group");
        if (file != null && group != null) throw new XcpUsage("Only one of file-path or group-path can be provided");
        var p = Proj.Open(o.ProjectArg);
        var f = p.File;
        List<string> targets;
        if (group != null) targets = GuessTargetsForGroup(p, p.Rel(group));
        else if (file != null)
        {
            var rel = p.Rel(file);
            if (FindFile(p, rel) == null) throw new XcProjException(FileNotInProject(p, rel));
            targets = f.ObjectsOfIsa("PBXNativeTarget").Where(t => TargetFiles(f, t).Any(x => f.FullPath(x) == rel)).OrderBy(f.TargetName, StringComparer.Ordinal).ToList();
        }
        else targets = NativeTargets(f);
        foreach (var t in targets) Console.WriteLine(f.TargetName(t));
        return 0;
    }

    static int SetTarget(Opts o)
    {
        var targets = ParseList(o.Require("targets"));
        var file = o.Require("file");
        if (targets.Count == 0) throw new XcpUsage("targets field required");
        var p = Proj.Open(o.ProjectArg);
        var rel = p.Rel(file);
        if (!o.Has("project-only") && !Exists(p.Abs(rel))) throw new XcProjException(FileNotOnDisk(p, rel));
        SetTargets(p, targets, rel);
        p.Save();
        return 0;
    }

    static int AddFile(Opts o)
    {
        var file = o.Require("file");
        var targets = ParseList(o.Get("targets"));
        if (targets.Count == 0 && !o.Has("guess-target")) throw new XcpUsage("--targets parameter is required unless --guess-target is specified");
        var p = Proj.Open(o.ProjectArg);
        var f = p.File;
        var rel = p.Rel(file);
        if (!o.Has("project-only") && !Exists(p.Abs(rel))) throw new XcProjException(FileNotOnDisk(p, rel));
        var groupRel = Dir(rel);
        var group = FindGroup(p, groupRel);
        if (group == null && !o.Has("create-groups")) throw new XcProjException(GroupNotInProject(p, groupRel));
        group ??= CreateGroupHierarchy(p, groupRel);
        if (targets.Count == 0 && o.Has("guess-target"))
        {
            var guessed = GuessTargetsForGroup(p, groupRel);
            if (guessed.Count == 0) Console.WriteLine($"Warning: Could not guess any targets for the file: {p.Abs(rel)}.");
            else targets = guessed.Select(f.TargetName).ToList();
        }
        if (FindFile(p, rel) == null)
        {
            // The path is stored relative to the group's folder.
            var groupPath = f.FullPath(group) ?? "";
            var relative = groupPath.Length > 0 && rel.StartsWith(groupPath + "/") ? rel[(groupPath.Length + 1)..] : Path.GetRelativePath(p.Abs(groupPath), p.Abs(rel)).Replace('\\', '/');
            var reference = f.Add(XcProjFile.NewObject("PBXFileReference",
                ("lastKnownFileType", XcProjFile.FileType(rel)), ("path", relative), ("sourceTree", "<group>")));
            AddChild(f, group, reference);
        }
        SetTargets(p, targets, rel);
        p.Save();
        if (o.Has("print-targets")) foreach (var t in targets) Console.WriteLine(t);
        return 0;
    }

    static void RemoveFile(Proj p, string rel)
    {
        var f = p.File;
        var file = FindFile(p, rel) ?? throw new XcProjException(FileNotInProject(p, rel));
        foreach (var t in f.ObjectsOfIsa("PBXNativeTarget").ToList())
            if (TargetFiles(f, t).Any(x => f.FullPath(x) == rel)) RemoveFromTarget(f, rel, t);
        // Any other build files (headers, frameworks, copy phases) pointing at it go too.
        foreach (var (id, obj) in f.Objects.ToList())
        {
            if (obj is not OrderedDictionary<string, object> phase || XcPlist.List(phase, "files") is not { } files) continue;
            foreach (var bf in files.OfType<string>().ToList())
                if (f.BuildFileRef(bf) == file) { files.Remove(bf); f.Remove(bf); }
        }
        RemoveChild(f, file);
        f.Remove(file);
    }

    static int DeleteFile(Opts o)
    {
        var file = o.Require("file");
        var p = Proj.Open(o.ProjectArg);
        var rel = p.Rel(file);
        var onDisk = p.Abs(rel);
        if (!o.Has("project-only") && !Exists(onDisk)) throw new XcProjException(FileNotOnDisk(p, rel));
        RemoveFile(p, rel);
        if (!o.Has("project-only")) { if (Directory.Exists(onDisk)) Directory.Delete(onDisk, true); else File.Delete(onDisk); }
        p.Save();
        return 0;
    }

    static int MoveFile(Opts o)
    {
        var file = o.Require("file");
        var dest = o.Require("dest");
        var p = Proj.Open(o.ProjectArg);
        var f = p.File;
        var rel = p.Rel(file);
        var destRel = p.Rel(dest);
        if (!o.Has("project-only") && !Exists(p.Abs(rel))) throw new XcProjException(FileNotOnDisk(p, rel));
        var targets = FindTargets(f, rel);
        var reference = FindFile(p, rel) ?? throw new XcProjException(FileNotInProject(p, rel));
        var destGroup = FindGroup(p, Dir(destRel)) ?? CreateGroupHierarchy(p, Dir(destRel));
        RemoveChild(f, reference);
        AddChild(f, destGroup, reference);
        if (Last(rel) != Last(destRel))
        {
            var obj = f.Obj(reference);
            obj.Remove("name");
            obj["path"] = Last(destRel);
            SetGroupSourceTree(f, reference);
        }
        if (o.Has("print-targets")) foreach (var t in targets) Console.WriteLine(f.TargetName(t));
        if (!o.Has("project-only"))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(p.Abs(destRel)));
            if (Directory.Exists(p.Abs(rel))) Directory.Move(p.Abs(rel), p.Abs(destRel));
            else File.Move(p.Abs(rel), p.Abs(destRel));
        }
        p.Save();
        return 0;
    }

    static int RenameFile(Opts o)
    {
        var file = o.Require("file");
        var name = o.Require("name");
        var p = Proj.Open(o.ProjectArg);
        var rel = p.Rel(file);
        if (!o.Has("project-only") && !Exists(p.Abs(rel))) throw new XcProjException(FileNotOnDisk(p, rel));
        var reference = FindFile(p, rel) ?? throw new XcProjException(FileNotInProject(p, rel));
        var obj = p.File.Obj(reference);
        obj.Remove("name");
        obj["path"] = name;
        SetGroupSourceTree(p.File, reference);
        if (!o.Has("project-only"))
        {
            var target = Path.Combine(Path.GetDirectoryName(p.Abs(rel)), name);
            if (Directory.Exists(p.Abs(rel))) Directory.Move(p.Abs(rel), target); else File.Move(p.Abs(rel), target);
        }
        p.Save();
        return 0;
    }

    static int AddGroup(Opts o)
    {
        var group = o.Require("group");
        var p = Proj.Open(o.ProjectArg);
        var rel = p.Rel(group);
        if (FindGroup(p, rel) != null) Console.WriteLine($"Warning: Group already exists in the project: {p.Abs(rel)}");
        else CreateGroupHierarchy(p, rel);
        if (o.Has("project-only") || Directory.Exists(p.Abs(rel))) { p.Save(); return 0; }
        if (!o.Has("create-groups")) throw new XcProjException(GroupNotOnDisk(p, rel));
        Directory.CreateDirectory(p.Abs(rel));
        p.Save();
        return 0;
    }

    static void RemoveGroupRecursively(Proj p, string group)
    {
        var f = p.File;
        foreach (var child in f.Ids(f.Obj(group), "children").AsEnumerable().Reverse().ToList())
        {
            var isa = f.Isa(child);
            if (isa is "PBXGroup" or "PBXVariantGroup" or "XCVersionGroup") RemoveGroupRecursively(p, child);
            else if (isa == "PBXFileReference" && f.FullPath(child) is { } path) RemoveFile(p, path);
        }
        RemoveChild(f, group);
        f.Remove(group);
    }

    static int DeleteGroup(Opts o)
    {
        var group = o.Require("group");
        var p = Proj.Open(o.ProjectArg);
        var rel = p.Rel(group);
        if (!o.Has("project-only") && !Directory.Exists(p.Abs(rel))) throw new XcProjException(GroupNotOnDisk(p, rel));
        var id = FindGroup(p, rel) ?? throw new XcProjException(GroupNotInProject(p, rel));
        RemoveGroupRecursively(p, id);
        if (!o.Has("project-only")) Directory.Delete(p.Abs(rel), true);
        p.Save();
        return 0;
    }

    static int RenameGroup(Opts o)
    {
        var group = o.Require("group");
        var name = o.Require("name");
        var p = Proj.Open(o.ProjectArg);
        var rel = p.Rel(group);
        if (!o.Has("project-only") && !Directory.Exists(p.Abs(rel))) throw new XcProjException(GroupNotOnDisk(p, rel));
        var id = FindGroup(p, rel) ?? throw new XcProjException(GroupNotInProject(p, rel));
        var obj = p.File.Obj(id);
        obj.Remove("name");
        obj["path"] = name;
        SetGroupSourceTree(p.File, id);
        if (!o.Has("project-only")) Directory.Move(p.Abs(rel), Path.Combine(Path.GetDirectoryName(p.Abs(rel)), name));
        p.Save();
        return 0;
    }

    static int MoveGroup(Opts o)
    {
        var group = o.Require("group");
        var dest = o.Require("dest");
        var p = Proj.Open(o.ProjectArg);
        var f = p.File;
        var rel = p.Rel(group);
        var destRel = p.Rel(dest);
        if (!o.Has("project-only") && !Directory.Exists(p.Abs(rel))) throw new XcProjException(GroupNotOnDisk(p, rel));
        var id = FindGroup(p, rel) ?? throw new XcProjException(GroupNotInProject(p, rel));
        RemoveChild(f, id);
        var destGroup = FindGroup(p, destRel) ?? CreateGroupHierarchy(p, destRel);
        var existing = Subgroup(f, destGroup, Last(rel));
        if (existing != null) { MergeGroups(p, id, existing); f.Remove(id); }
        else { SetGroupSourceTree(f, id); AddChild(f, destGroup, id); }
        if (!o.Has("project-only"))
        {
            var newPath = Path.Combine(p.Abs(destRel), Last(rel));
            if (Directory.Exists(newPath))
            {
                Console.WriteLine($"Warning: Merging group '{Last(rel)}' into existing group at destination.");
                MergeDirectories(p.Abs(rel), newPath);
                Directory.Delete(p.Abs(rel), true);
            }
            else
            {
                Directory.CreateDirectory(p.Abs(destRel));
                Directory.Move(p.Abs(rel), newPath);
            }
        }
        p.Save();
        return 0;
    }

    static void MergeGroups(Proj p, string source, string target)
    {
        var f = p.File;
        foreach (var child in f.Ids(f.Obj(source), "children"))
        {
            var isa = f.Isa(child);
            if (isa == "PBXGroup")
            {
                var name = Last((f.FullPath(child) ?? "").TrimEnd('/'));
                var existing = Subgroup(f, target, name);
                if (existing != null) { MergeGroups(p, child, existing); f.Remove(child); }
                else { SetGroupSourceTree(f, child); AddChild(f, target, child); }
            }
            else if (isa == "PBXFileReference")
            {
                var name = Last(f.FullPath(child) ?? "");
                var clash = f.Ids(f.Obj(target), "children").Any(c => f.Isa(c) == "PBXFileReference" && Last(f.FullPath(c) ?? "") == name);
                if (clash && f.FullPath(child) is { } path) RemoveFile(p, path);
                else { SetGroupSourceTree(f, child); AddChild(f, target, child); }
            }
            else AddChild(f, target, child);
        }
        f.Obj(source)["children"] = new List<object>();
        f.InvalidateParents();
    }

    static void MergeDirectories(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from))
        {
            var target = Path.Combine(to, Path.GetFileName(file));
            if (!File.Exists(target)) File.Move(file, target);
        }
        foreach (var dir in Directory.GetDirectories(from)) MergeDirectories(dir, Path.Combine(to, Path.GetFileName(dir)));
    }

    static int GetBuildSetting(Opts o)
    {
        var target = o.Require("target");
        var key = o.Require("key");
        var config = o.Get("config");
        var p = Proj.Open(o.ProjectArg);
        var f = p.File;
        var t = f.Targets.FirstOrDefault(x => f.TargetName(x) == target) ?? throw new XcProjException($"Targets not found in the project: {target}");
        var list = XcProjFile.Str(f.Obj(t), "buildConfigurationList");
        var configs = f.Ids(f.Obj(list), "buildConfigurations");
        string id;
        if (config != null) id = configs.FirstOrDefault(c => XcProjFile.Str(f.Obj(c), "name") == config) ?? throw new XcProjException($"Build configuration not found: {config}");
        else id = configs.FirstOrDefault();
        var value = XcPlist.Dict(f.Obj(id), "buildSettings") is { } settings && settings.TryGetValue(key, out var v)
            ? v is List<object> l ? string.Join("\n", l.Select(XcPlist.Scalar)) : XcPlist.Scalar(v) : "";
        Console.WriteLine(value);
        return 0;
    }

    static int SetBuildSetting(Opts o)
    {
        var key = o.Require("key");
        var value = o.Require("value");
        var p = Proj.Open(o.ProjectArg);
        var f = p.File;
        var names = ParseList(o.Get("targets"));
        if (names.Count == 0) names = NativeTargets(f).Select(f.TargetName).ToList();
        var configs = ParseList(o.Get("configs"));
        foreach (var name in names)
        {
            var matches = f.Targets.Where(t => f.TargetName(t) == name).ToList();
            if (matches.Count == 0) throw new XcProjException($"Targets not found in the project: {name}");
            foreach (var t in matches)
            {
                var list = XcProjFile.Str(f.Obj(t), "buildConfigurationList");
                foreach (var c in f.Ids(f.Obj(list), "buildConfigurations"))
                {
                    var co = f.Obj(c);
                    if (configs.Count > 0 && !configs.Contains(XcProjFile.Str(co, "name"))) continue;
                    if (XcPlist.Dict(co, "buildSettings") is not { } settings) co["buildSettings"] = settings = [];
                    settings.TryGetValue(key, out var existing);
                    settings[key] = existing switch
                    {
                        List<object> l => o.Has("append") ? [.. l, value] : new List<object> { value },
                        string s when o.Has("append") => $"{s} {value}",
                        _ => value,
                    };
                }
            }
        }
        p.Save();
        return 0;
    }

    // ---- asset catalogs ----

    static readonly (string Ext, string Title, string Type)[] AssetTypes = [("imageset", "Images", "image"), ("dataset", "Data Files", "data"), ("colorset", "Colors", "color")];

    static string Catalog(Opts o)
    {
        var path = o.Positional.FirstOrDefault() ?? throw new XcpUsage("Missing expected argument '<xcassets-path>'");
        var full = Path.GetFullPath(path);
        if (!Directory.Exists(full)) throw new XcProjException($"xcassets folder not found on disk: {full}");
        return full;
    }

    // <catalog>/<folder>/<name>.<kind>set for an asset path like "Folder/Name.png".
    static (string Dir, string FileName, string DarkFileName) AssetLocation(string catalog, string assetPath, string ext)
    {
        var fileName = Path.GetFileName(assetPath);
        var bare = Path.GetFileNameWithoutExtension(fileName);
        var fileExt = Path.GetExtension(fileName);
        var folder = Path.GetDirectoryName(assetPath) ?? "";
        return (Path.Combine(catalog, folder, $"{bare}.{ext}"), fileName, $"{bare}_dark{fileExt}");
    }

    const string Info = "  \"info\" : {\n    \"author\" : \"xcode\",\n    \"version\" : 1\n  }";

    static int AddImageAsset(Opts o)
    {
        var catalog = Catalog(o);
        var file = Path.GetFullPath(o.Require("file"));
        var dark = o.Get("dark-file") is { } d ? Path.GetFullPath(d) : null;
        var assetPath = o.Require("asset-path");
        var mode = o.Get("mode") ?? "default";
        if (mode is not ("default" or "template" or "original")) throw new XcpUsage($"The value '{mode}' is invalid for '--mode <default|template|original>'");
        if (!File.Exists(file)) throw new XcProjException($"File not found on disk: {file}");
        var (dir, name, darkName) = AssetLocation(catalog, assetPath, "imageset");
        Directory.CreateDirectory(dir);
        File.Copy(file, Path.Combine(dir, name));
        if (dark != null) File.Copy(dark, Path.Combine(dir, darkName));
        var darkJson = dark == null ? "" : $",\n    {{\n      \"appearances\" : [\n        {{\n          \"appearance\" : \"luminosity\",\n          \"value\" : \"dark\"\n        }}\n      ],\n      \"filename\" : \"{darkName}\",\n      \"idiom\" : \"universal\"\n    }}";
        var rendering = mode == "default" ? "" : $",\n  \"properties\" : {{\n    \"template-rendering-intent\" : \"{mode}\"\n  }}";
        File.WriteAllText(Path.Combine(dir, "Contents.json"),
            $"{{\n  \"images\" : [\n    {{\n      \"filename\" : \"{name}\",\n      \"idiom\" : \"universal\"\n    }}{darkJson}\n  ],\n{Info}{rendering}\n}}\n");
        return 0;
    }

    static int AddDataAsset(Opts o)
    {
        var catalog = Catalog(o);
        var file = Path.GetFullPath(o.Require("file"));
        var assetPath = o.Require("asset-path");
        if (!File.Exists(file)) throw new XcProjException($"File not found on disk: {file}");
        var (dir, name, _) = AssetLocation(catalog, assetPath, "dataset");
        Directory.CreateDirectory(dir);
        File.Copy(file, Path.Combine(dir, name));
        File.WriteAllText(Path.Combine(dir, "Contents.json"),
            $"{{\n  \"data\" : [\n    {{\n      \"filename\" : \"{name}\",\n      \"idiom\" : \"universal\"\n    }}\n  ],\n{Info}\n}}\n");
        return 0;
    }

    static (string R, string G, string B, string A)? Hex(string hex)
    {
        if (hex == null) return null;
        var s = hex.Trim().ToUpperInvariant().TrimStart('#');
        if (s.Length != 6 && s.Length != 8) return null;
        var alpha = "1.000";
        if (s.Length == 8)
        {
            var a = int.TryParse(s[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v) ? v : 255;
            alpha = (a / 255.0).ToString("0.000", CultureInfo.InvariantCulture);
            s = s[2..];
        }
        return (s[..2], s[2..4], s[4..6], alpha);
    }

    static int AddColorAsset(Opts o)
    {
        var catalog = Catalog(o);
        var color = o.Require("color");
        var darkColor = o.Get("dark-color");
        var space = o.Get("color-space") ?? "srgb";
        var assetPath = o.Require("asset-path");
        var dir = Path.Combine(catalog, Path.GetDirectoryName(assetPath) ?? "", Path.GetFileNameWithoutExtension(assetPath) + ".colorset");
        Directory.CreateDirectory(dir);
        string Components((string R, string G, string B, string A) c, string pad) =>
            $"{pad}\"color\" : {{\n{pad}  \"color-space\" : \"{space}\",\n{pad}  \"components\" : {{\n{pad}    \"alpha\" : \"{c.A}\",\n{pad}    \"blue\" : \"0x{c.B}\",\n{pad}    \"green\" : \"0x{c.G}\",\n{pad}    \"red\" : \"0x{c.R}\"\n{pad}  }}\n{pad}}},\n";
        var json = "";
        if (Hex(color) is { } main)
        {
            var darkJson = Hex(darkColor) is { } dark
                ? ",\n    {\n      \"appearances\" : [\n        {\n          \"appearance\" : \"luminosity\",\n          \"value\" : \"dark\"\n        }\n      ],\n" + Components(dark, "      ") + "      \"idiom\" : \"universal\"\n    }"
                : "";
            json = "{\n  \"colors\" : [\n    {\n" + Components(main, "      ") + "      \"idiom\" : \"universal\"\n    }" + darkJson + "\n  ],\n" + Info + "\n}\n";
        }
        File.WriteAllText(Path.Combine(dir, "Contents.json"), json);
        return 0;
    }

    static int ListAssets(Opts o)
    {
        var catalog = Catalog(o);
        var type = o.Get("type");
        var all = new Dictionary<string, List<string>>();
        foreach (var dir in Directory.EnumerateDirectories(catalog, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(dir).TrimStart('.');
            if (AssetTypes.All(t => t.Ext != ext)) continue;
            var rel = Path.GetRelativePath(catalog, dir).Replace('\\', '/');
            rel = rel[..^(ext.Length + 1)];
            if (!all.TryGetValue(ext, out var list)) all[ext] = list = [];
            list.Add(rel);
        }
        var filter = type?.ToLowerInvariant();
        var first = true;
        foreach (var t in AssetTypes)
        {
            if (filter != null && t.Type != filter) continue;
            if (!all.TryGetValue(t.Ext, out var list)) continue;
            if (!first) Console.WriteLine();
            first = false;
            if (type == null) Console.WriteLine($"> {t.Title}");
            foreach (var path in list.Order(StringComparer.Ordinal)) Console.WriteLine(path);
        }
        return 0;
    }

    static string FindAsset(string catalog, string assetPath)
    {
        foreach (var t in AssetTypes)
        {
            var dir = Path.Combine(catalog, assetPath + "." + t.Ext);
            if (Directory.Exists(dir)) return t.Ext;
        }
        throw new XcProjException($"Asset not found: {assetPath}");
    }

    static int MoveAsset(Opts o)
    {
        var catalog = Catalog(o);
        var assetPath = o.Require("asset-path");
        var dest = o.Require("dest");
        var ext = FindAsset(catalog, assetPath);
        var from = Path.Combine(catalog, assetPath + "." + ext);
        var to = Path.Combine(catalog, dest + "." + ext);
        Directory.CreateDirectory(Path.GetDirectoryName(to));
        Directory.Move(from, to);
        return 0;
    }

    static int DeleteAsset(Opts o)
    {
        var catalog = Catalog(o);
        var assetPath = o.Require("asset-path");
        var ext = FindAsset(catalog, assetPath);
        Directory.Delete(Path.Combine(catalog, assetPath + "." + ext), true);
        return 0;
    }
}
