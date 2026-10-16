using System.Security.Cryptography;
using System.Text;

namespace WinTools;

// An Xcode project (Foo.xcodeproj/project.pbxproj) as its object graph: load, look up, change and
// write back in Xcode's own layout (sections by isa, comments after object IDs), so the file diffs
// cleanly against one Xcode saved. Used by xcodegen, xcdiff, xcp and rswift.
sealed class XcProjFile
{
    public string XcodeprojPath;   // the .xcodeproj folder
    public string SourceRoot;      // the folder that contains it ($(SRCROOT))
    public OrderedDictionary<string, object> Root;
    public OrderedDictionary<string, object> Objects;
    Dictionary<string, string> parents;

    public string Name => Path.GetFileNameWithoutExtension(XcodeprojPath);
    public string RootObject => XcPlist.Str(Root, "rootObject");
    public OrderedDictionary<string, object> Project => Obj(RootObject);
    public string PbxprojPath => Path.Combine(XcodeprojPath, "project.pbxproj");

    public static XcProjFile Load(string path)
    {
        var full = Path.GetFullPath(path);
        if (File.Exists(full) && Path.GetFileName(full) == "project.pbxproj") full = Path.GetDirectoryName(full);
        var pbxproj = Path.Combine(full, "project.pbxproj");
        if (!File.Exists(pbxproj))
            throw new XcProjException($"The project doesn't contain a .pbxproj file at path: {path.TrimEnd('/', '\\')}");
        object parsed;
        try { parsed = XcPlist.Load(pbxproj); }
        catch (Exception e) when (e is InvalidDataException or FormatException)
        {
            throw new XcProjException($"The project at path {path} could not be read: {e.Message}");
        }
        var root = XcPlist.Dict(parsed) ?? throw new XcProjException($"The project at path {path} is not a property list dictionary");
        return new XcProjFile
        {
            XcodeprojPath = full,
            SourceRoot = Path.GetDirectoryName(full),
            Root = root,
            Objects = XcPlist.Dict(root, "objects") ?? [],
        };
    }

    // ---- object access ----

    public OrderedDictionary<string, object> Obj(string id) =>
        id != null && Objects.TryGetValue(id, out var o) ? o as OrderedDictionary<string, object> : null;

    public string Isa(string id) => XcPlist.Str(Obj(id), "isa");

    public static string Str(OrderedDictionary<string, object> o, string key) => XcPlist.Str(o, key);

    public List<string> Ids(OrderedDictionary<string, object> o, string key) =>
        XcPlist.List(o, key)?.Select(v => v as string).Where(v => v != null).ToList() ?? [];

    public IEnumerable<string> ObjectsOfIsa(params string[] isas) =>
        Objects.Where(kv => isas.Contains(XcPlist.Str(kv.Value as OrderedDictionary<string, object>, "isa"))).Select(kv => kv.Key);

    public List<string> Targets => Ids(Project, "targets");
    public string TargetName(string id) => Str(Obj(id), "name");
    public string TargetNamed(string name) => Targets.FirstOrDefault(t => TargetName(t) == name);

    public IEnumerable<string> NativeTargets => Targets.Where(t => Isa(t) == "PBXNativeTarget");
    public IEnumerable<string> AggregateTargets => Targets.Where(t => Isa(t) == "PBXAggregateTarget");

    public string MainGroup => Str(Project, "mainGroup");

    public List<string> BuildPhases(string target) => Ids(Obj(target), "buildPhases");

    public string Phase(string target, string isa) => BuildPhases(target).FirstOrDefault(p => Isa(p) == isa);

    // The file element (file reference, variant group...) behind a build file.
    public string BuildFileRef(string buildFile) => Str(Obj(buildFile), "fileRef");

    public List<string> PhaseFiles(string phase) => Ids(Obj(phase), "files");

    // Configurations of a configuration list, by name.
    public string Configuration(string list, string name) =>
        Ids(Obj(list), "buildConfigurations").FirstOrDefault(c => Str(Obj(c), "name") == name);

    public string Parent(string id)
    {
        if (parents == null)
        {
            parents = [];
            foreach (var (key, value) in Objects)
                foreach (var child in Ids(value as OrderedDictionary<string, object>, "children"))
                    parents.TryAdd(child, key);
        }
        return parents.GetValueOrDefault(id);
    }

    public void InvalidateParents() => parents = null;

    // The element's path relative to the source root (forward slashes), as XcodeProj's fullPath
    // computes it; null for SDK, developer and build-products references.
    public string FullPath(string id)
    {
        var o = Obj(id);
        if (o == null) return null;
        var path = Str(o, "path");
        var tree = Str(o, "sourceTree") ?? "<group>";
        switch (tree)
        {
            case "<absolute>": return path;
            case "SOURCE_ROOT": return Normalize(path ?? "");
            case "<group>":
            {
                var parent = Parent(id);
                var parentPath = parent == null ? "" : FullPath(parent);
                if (parentPath == null) return null;
                return Normalize(path == null ? parentPath : parentPath.Length == 0 ? path : parentPath + "/" + path);
            }
            default: return null;
        }
    }

    public string AbsolutePath(string id)
    {
        var rel = FullPath(id);
        if (rel == null) return null;
        return Path.IsPathRooted(rel) ? rel : Path.GetFullPath(Path.Combine(SourceRoot, rel));
    }

    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path)) return path ?? "";
        path = path.Replace('\\', '/');
        var rooted = path.StartsWith('/');
        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part.Length == 0 || part == ".") continue;
            if (part == ".." && parts.Count > 0 && parts[^1] != "..") { parts.RemoveAt(parts.Count - 1); continue; }
            parts.Add(part);
        }
        return (rooted ? "/" : "") + string.Join("/", parts);
    }

    // Display name of a file element: its name, else the last component of its path.
    public string ElementName(string id)
    {
        var o = Obj(id);
        var name = Str(o, "name");
        if (name != null) return name;
        var path = Str(o, "path");
        return path == null ? null : path.Replace('\\', '/').Split('/')[^1];
    }

    // ---- changes ----

    public string NewId(string seed = null)
    {
        while (true)
        {
            var bytes = seed == null ? RandomNumberGenerator.GetBytes(12) : MD5.HashData(Encoding.UTF8.GetBytes(seed))[..12];
            var id = Convert.ToHexString(bytes);
            if (!Objects.ContainsKey(id)) return id;
            seed = seed == null ? null : seed + "+";
        }
    }

    public string Add(OrderedDictionary<string, object> obj, string seed = null)
    {
        var id = NewId(seed);
        Objects[id] = obj;
        return id;
    }

    public void Remove(string id)
    {
        Objects.Remove(id);
        parents = null;
    }

    public static OrderedDictionary<string, object> NewObject(string isa, params (string Key, object Value)[] fields)
    {
        var o = new OrderedDictionary<string, object> { ["isa"] = isa };
        foreach (var (k, v) in fields) if (v != null) o[k] = v;
        return o;
    }

    public void Save() => File.WriteAllText(PbxprojPath, Serialize(), new UTF8Encoding(false));

    // ---- writing in Xcode's layout ----

    static readonly HashSet<string> SingleLine = ["PBXBuildFile", "PBXFileReference"];

    public string Serialize()
    {
        phaseOf = [];
        foreach (var (key, value) in Objects)
            foreach (var file in Ids(value as OrderedDictionary<string, object>, "files")) phaseOf.TryAdd(file, key);
        try { return SerializeObjects(); }
        finally { phaseOf = null; }
    }

    Dictionary<string, string> phaseOf;

    string SerializeObjects()
    {
        var sb = new StringBuilder();
        sb.Append("// !$*UTF8*$!\n{\n");
        foreach (var (key, value) in Root.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (key == "objects")
            {
                sb.Append("\tobjects = {\n");
                foreach (var section in Objects.GroupBy(kv => XcPlist.Str(kv.Value as OrderedDictionary<string, object>, "isa") ?? "")
                             .OrderBy(g => g.Key, StringComparer.Ordinal))
                {
                    sb.Append($"\n/* Begin {section.Key} section */\n");
                    foreach (var (id, obj) in section.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                    {
                        sb.Append("\t\t").Append(Reference(id)).Append(" = ");
                        if (SingleLine.Contains(section.Key)) WriteInline(sb, obj);
                        else WriteValue(sb, obj, 2, true);
                        sb.Append(";\n");
                    }
                    sb.Append($"/* End {section.Key} section */\n");
                }
                sb.Append("\t};\n");
                continue;
            }
            sb.Append('\t').Append(Quote(key)).Append(" = ");
            WriteValue(sb, value, 1, false);
            sb.Append(";\n");
        }
        sb.Append("}\n");
        return sb.ToString();
    }

    void WriteValue(StringBuilder sb, object value, int depth, bool isObject, string key = null)
    {
        var indent = new string('\t', depth);
        switch (value)
        {
            case OrderedDictionary<string, object> dict:
                sb.Append("{\n");
                foreach (var (k, v) in OrderKeys(dict, isObject))
                {
                    sb.Append(indent).Append('\t').Append(Quote(k)).Append(" = ");
                    WriteValue(sb, v, depth + 1, false, k);
                    sb.Append(";\n");
                }
                sb.Append(indent).Append('}');
                return;
            case List<object> list:
                sb.Append("(\n");
                foreach (var item in list)
                {
                    sb.Append(indent).Append('\t');
                    WriteValue(sb, item, depth + 1, false, key);
                    sb.Append(",\n");
                }
                sb.Append(indent).Append(')');
                return;
            default:
                sb.Append(ValueText(value, key));
                return;
        }
    }

    void WriteInline(StringBuilder sb, object value, string key = null)
    {
        switch (value)
        {
            case OrderedDictionary<string, object> dict:
                sb.Append('{');
                foreach (var (k, v) in OrderKeys(dict, true))
                {
                    sb.Append(Quote(k)).Append(" = ");
                    WriteInline(sb, v, k);
                    sb.Append("; ");
                }
                sb.Append('}');
                return;
            case List<object> list:
                sb.Append('(');
                foreach (var item in list) { WriteInline(sb, item, key); sb.Append(", "); }
                sb.Append(')');
                return;
            default:
                sb.Append(ValueText(value, key));
                return;
        }
    }

    static IEnumerable<KeyValuePair<string, object>> OrderKeys(OrderedDictionary<string, object> dict, bool isaFirst) =>
        dict.OrderBy(kv => isaFirst && kv.Key == "isa" ? 0 : 1).ThenBy(kv => kv.Key, StringComparer.Ordinal);

    static readonly HashSet<string> UncommentedKeys = ["remoteGlobalIDString", "TestTargetID"];

    string ValueText(object value, string key)
    {
        var s = XcPlist.Scalar(value) ?? "";
        if (value is byte[] data) return "<" + Convert.ToHexString(data).ToLowerInvariant() + ">";
        if (key != null && !UncommentedKeys.Contains(key) && Objects.ContainsKey(s)) return Reference(s);
        return Quote(s);
    }

    // An object ID with its /* comment */, as Xcode writes references.
    string Reference(string id)
    {
        var comment = Comment(id);
        return comment == null ? id : $"{id} /* {comment} */";
    }

    public string Comment(string id)
    {
        var o = Obj(id);
        if (o == null) return null;
        var isa = Str(o, "isa");
        switch (isa)
        {
            case "PBXProject": return "Project object";
            case "PBXBuildFile":
            {
                var file = Str(o, "fileRef");
                var name = file != null ? ElementName(file) : Str(Obj(Str(o, "productRef")), "productName");
                var phase = phaseOf != null ? phaseOf.GetValueOrDefault(id)
                    : Objects.FirstOrDefault(kv => Ids(kv.Value as OrderedDictionary<string, object>, "files").Contains(id)).Key;
                return phase == null ? name : $"{name ?? "(null)"} in {PhaseName(phase)}";
            }
            case "XCConfigurationList":
            {
                foreach (var (ownerId, owner) in Objects)
                {
                    if (owner is not OrderedDictionary<string, object> od || Str(od, "buildConfigurationList") != id) continue;
                    var ownerIsa = Str(od, "isa");
                    var ownerName = ownerIsa == "PBXProject" ? Name : Str(od, "name");
                    return $"Build configuration list for {ownerIsa} \"{ownerName}\"";
                }
                return null;
            }
            case "PBXTargetDependency" or "PBXContainerItemProxy": return isa;
            case "XCRemoteSwiftPackageReference":
            {
                var url = Str(o, "repositoryURL") ?? "";
                var repo = url.TrimEnd('/').Split('/')[^1];
                if (repo.EndsWith(".git")) repo = repo[..^4];
                return $"XCRemoteSwiftPackageReference \"{repo}\"";
            }
            case "XCLocalSwiftPackageReference": return $"XCLocalSwiftPackageReference \"{Str(o, "relativePath") ?? Str(o, "path")}\"";
            case "XCSwiftPackageProductDependency": return Str(o, "productName");
            case "XCBuildConfiguration": return Str(o, "name");
            default:
                if (isa != null && isa.EndsWith("BuildPhase")) return PhaseName(id);
                return Str(o, "name") ?? ElementName(id);
        }
    }

    public string PhaseName(string phase)
    {
        var o = Obj(phase);
        var name = Str(o, "name");
        if (name != null) return name;
        return Str(o, "isa") switch
        {
            "PBXSourcesBuildPhase" => "Sources",
            "PBXFrameworksBuildPhase" => "Frameworks",
            "PBXResourcesBuildPhase" => "Resources",
            "PBXHeadersBuildPhase" => "Headers",
            "PBXCopyFilesBuildPhase" => "CopyFiles",
            "PBXShellScriptBuildPhase" => "ShellScript",
            "PBXRezBuildPhase" => "Rez",
            "PBXAppleScriptBuildPhase" => "AppleScript",
            _ => "BuildPhase",
        };
    }

    // Xcode quotes everything but plain identifiers and paths.
    public static string Quote(string s)
    {
        if (s.Length > 0 && s.All(XcPlist.IsUnquoted) && !s.Contains("//") && !s.Contains("___") && s != "+") return s;
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\r': sb.Append("\\r"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.Append('"').ToString();
    }

    // ---- file types (lastKnownFileType / explicitFileType), shared by xcodegen and xcp ----

    public static string FileType(string path)
    {
        var name = Path.GetFileName(path.TrimEnd('/', '\\'));
        var ext = Path.GetExtension(name).TrimStart('.').ToLowerInvariant();
        if (name == "Podfile" || name == "Cartfile") return "text";
        return ext switch
        {
            "swift" => "sourcecode.swift",
            "m" => "sourcecode.c.objc",
            "mm" => "sourcecode.cpp.objcpp",
            "c" => "sourcecode.c.c",
            "cpp" or "cc" or "cxx" => "sourcecode.cpp.cpp",
            "h" => "sourcecode.c.h",
            "hpp" or "hh" or "hxx" => "sourcecode.cpp.h",
            "metal" => "sourcecode.metal",
            "s" => "sourcecode.asm",
            "modulemap" => "sourcecode.module-map",
            "xcassets" => "folder.assetcatalog",
            "xcstrings" => "text.json.xcstrings",
            "storyboard" => "file.storyboard",
            "xib" => "file.xib",
            "strings" => "text.plist.strings",
            "stringsdict" => "text.plist.stringsdict",
            "plist" => "text.plist.xml",
            "entitlements" => "text.plist.entitlements",
            "xcconfig" => "text.xcconfig",
            "json" => "text.json",
            "md" => "net.daringfireball.markdown",
            "txt" => "text",
            "html" => "text.html",
            "css" => "text.css",
            "js" => "sourcecode.javascript",
            "sh" => "text.script.sh",
            "py" => "text.script.python",
            "rb" => "text.script.ruby",
            "yml" or "yaml" => "text.yaml",
            "xml" => "text.xml",
            "png" => "image.png",
            "jpg" or "jpeg" => "image.jpeg",
            "gif" => "image.gif",
            "pdf" => "image.pdf",
            "svg" => "image.svg",
            "heic" => "image.heic",
            "tif" or "tiff" => "image.tiff",
            "ttf" => "file",
            "otf" => "file",
            "mp3" => "audio.mp3",
            "wav" => "audio.wav",
            "caf" => "audio",
            "m4a" => "audio.m4a",
            "mp4" => "video.mp4",
            "mov" => "video.quicktime",
            "framework" => "wrapper.framework",
            "xcframework" => "wrapper.xcframework",
            "bundle" => "wrapper.plug-in",
            "app" => "wrapper.application",
            "appex" => "wrapper.app-extension",
            "a" => "archive.ar",
            "dylib" => "compiled.mach-o.dylib",
            "tbd" => "sourcecode.text-based-dylib-definition",
            "xcdatamodeld" => "wrapper.xcdatamodeld",
            "xcdatamodel" => "wrapper.xcdatamodel",
            "xcmappingmodel" => "wrapper.xcmappingmodel",
            "mlmodel" => "file.mlmodel",
            "mlpackage" => "folder.mlpackage",
            "intentdefinition" => "file.intentdefinition",
            "playground" => "file.playground",
            "rcproject" => "file.rcproject",
            "docc" => "folder.documentationcatalog",
            "xctestplan" => "text",
            "xcodeproj" => "wrapper.pb-project",
            "xcworkspace" => "wrapper.workspace",
            "lproj" => "folder",
            "gpx" => "text.xml",
            "zip" => "archive.zip",
            "icon" => "folder.iconcomposer.icon",
            _ => "file",
        };
    }
}

// An error that the Xcode tools print as "Error: <message>" and exit 1.
sealed class XcProjException(string message) : Exception(message);
