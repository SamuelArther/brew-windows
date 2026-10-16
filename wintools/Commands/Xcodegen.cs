using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// xcodegen: generate an Xcode project (Name.xcodeproj) from a project.yml spec and the folder
// structure, so the project file needn't be kept in version control. Pure file generation, so it
// works on Windows the same (the generated project is opened in Xcode on a Mac or in CI). Supports
// the spec's core: name, include, options (bundleIdPrefix, deploymentTarget, developmentLanguage,
// createIntermediateGroups), settings, configs, packages and targets with type, platform, sources
// (paths with excludes, buildPhase, type), dependencies (target, framework, sdk, package), settings,
// info/entitlements (Info.plist generation), pre/post build scripts, scheme (shared schemes);
// plus `xcodegen dump`. Project objects get deterministic IDs, as XcodeGen does.
static class Xcodegen
{
    const string Version = "2.44.1";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("xcodegen", "Generate an Xcode project from a spec file and the folder structure", Run);

    static int Run(string[] args)
    {
        var sub = "generate";
        var rest = args.ToList();
        if (rest.Count > 0 && rest[0] is "generate" or "dump" or "version" or "help") { sub = rest[0]; rest.RemoveAt(0); }
        var specs = new List<string>();
        string project = null, projectRoot = null, dumpType = "yaml", dumpFile = null;
        var quiet = false;
        for (var i = 0; i < rest.Count; i++)
        {
            string Value() => ++i < rest.Count ? rest[i] : throw new UsageException($"missing value for {rest[i - 1]}");
            switch (rest[i])
            {
                case "-h" or "--help": sub = "help"; break;
                case "--version": sub = "version"; break;
                case "-s" or "--spec": specs.AddRange(Value().Split(',')); break;
                case "-p" or "--project": project = Value(); break;
                case "-r" or "--project-root": projectRoot = Value(); break;
                case "-q" or "--quiet": quiet = true; break;
                case "-c" or "--use-cache" or "--only-plists" or "--no-env": break;
                case "--cache-path": Value(); break;
                case "--type": dumpType = Value(); break;
                case "--file": dumpFile = Value(); break;
                default: throw new UsageException($"Unknown option {rest[i]}");
            }
        }
        if (sub == "help")
        {
            Console.WriteLine("""
                Usage: xcodegen [generate|dump|version] [options]
                Generate an Xcode project from a spec (project.yml) and the folder structure.

                  -s, --spec <path>          the spec file (default: project.yml)
                  -p, --project <dir>        where to write the .xcodeproj (default: the spec's folder)
                  -r, --project-root <dir>   the folder the spec's paths are relative to
                  -q, --quiet                no output
                  --version                  show the version

                xcodegen dump [--spec <path>] [--type yaml|json|summary] [--file <path>]
                  print the resolved spec (includes merged)
                """);
            return 0;
        }
        if (sub == "version") { Console.WriteLine($"Version: {Version}"); return 0; }
        if (specs.Count == 0) specs.Add("project.yml");
        try
        {
            var spec = new OrderedDictionary<string, object>();
            foreach (var s in specs) Merge(spec, LoadSpec(Path.GetFullPath(s)));
            var specDir = Path.GetDirectoryName(Path.GetFullPath(specs[0]));
            var root = Path.GetFullPath(projectRoot ?? specDir);
            if (sub == "dump") return Dump(spec, dumpType, dumpFile);
            var name = XcPlist.Str(spec, "name") ?? throw new XcProjException("Spec is missing a name");
            var outDir = Path.GetFullPath(project ?? specDir);
            if (!quiet) Console.WriteLine("Loaded project:\n  Name: " + name + "\n  Targets: " + (XcPlist.Dict(spec, "targets")?.Count ?? 0));
            if (!quiet) Console.WriteLine("Generating plists...");
            if (!quiet) Console.WriteLine("Generating project...");
            var gen = new Generator(spec, root, outDir);
            var proj = gen.Build();
            if (!quiet) Console.WriteLine("Writing project...");
            Directory.CreateDirectory(proj.XcodeprojPath);
            proj.Save();
            var ws = Path.Combine(proj.XcodeprojPath, "project.xcworkspace");
            Directory.CreateDirectory(ws);
            File.WriteAllText(Path.Combine(ws, "contents.xcworkspacedata"),
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Workspace\n   version = \"1.0\">\n   <FileRef\n      location = \"self:\">\n   </FileRef>\n</Workspace>\n");
            gen.WriteSchemes(proj);
            if (!quiet) Console.WriteLine($"Created project at {proj.XcodeprojPath}");
            return 0;
        }
        catch (XcProjException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    // A spec file with its `include:` files merged in (included first, the including file wins).
    static OrderedDictionary<string, object> LoadSpec(string path)
    {
        if (!File.Exists(path)) throw new XcProjException($"No project spec found at {path}");
        var text = File.ReadAllText(path);
        var parsed = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? XcYaml.Parse(text) : XcYaml.Parse(text);
        var spec = parsed as OrderedDictionary<string, object> ?? throw new XcProjException($"{path} is not a mapping");
        var merged = new OrderedDictionary<string, object>();
        var includes = spec.TryGetValue("include", out var inc) ? inc is List<object> l ? l : [inc] : [];
        foreach (var item in includes)
        {
            var rel = item is OrderedDictionary<string, object> d ? XcPlist.Str(d, "path") : XcPlist.Scalar(item);
            if (rel == null) continue;
            Merge(merged, LoadSpec(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, rel))));
        }
        spec.Remove("include");
        Merge(merged, spec);
        return merged;
    }

    static void Merge(OrderedDictionary<string, object> into, OrderedDictionary<string, object> from)
    {
        foreach (var (k, v) in from)
        {
            if (into.TryGetValue(k, out var existing) && existing is OrderedDictionary<string, object> a && v is OrderedDictionary<string, object> b) Merge(a, b);
            else if (into.TryGetValue(k, out existing) && existing is List<object> la && v is List<object> lb) la.AddRange(lb);
            else into[k] = v;
        }
    }

    static int Dump(OrderedDictionary<string, object> spec, string type, string file)
    {
        string text = type switch
        {
            "yaml" => XcYaml.Dump(spec),
            "json" or "parsed-json" => Json(spec, 0) + "\n",
            "summary" => $"Name: {XcPlist.Str(spec, "name")}\nTargets:\n" + string.Join("", (XcPlist.Dict(spec, "targets") ?? []).Select(t =>
                $"  {t.Key}: {XcPlist.Str(t.Value as OrderedDictionary<string, object>, "type")} {XcPlist.Str(t.Value as OrderedDictionary<string, object>, "platform")}\n")),
            _ => throw new UsageException($"unknown dump type {type} (yaml, json, summary)"),
        };
        if (file != null) File.WriteAllText(file, text); else Console.Write(text);
        return 0;
    }

    static string Json(object v, int indent)
    {
        var pad = new string(' ', indent + 2);
        return v switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            long or double or int => XcPlist.Scalar(v),
            string s => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"",
            OrderedDictionary<string, object> d => d.Count == 0 ? "{}" : "{\n" + string.Join(",\n", d.Select(kv => pad + Json(kv.Key, 0) + " : " + Json(kv.Value, indent + 2))) + "\n" + new string(' ', indent) + "}",
            List<object> l => l.Count == 0 ? "[]" : "[\n" + string.Join(",\n", l.Select(x => pad + Json(x, indent + 2))) + "\n" + new string(' ', indent) + "]",
            _ => Json(XcPlist.Scalar(v), indent),
        };
    }

    // ---- generation ----

    sealed class Generator(OrderedDictionary<string, object> spec, string root, string outDir)
    {
        XcProjFile p;
        readonly Dictionary<string, string> targetIds = [];
        readonly Dictionary<string, string> productRefs = [];
        readonly Dictionary<string, string> fileRefs = new(StringComparer.OrdinalIgnoreCase);   // absolute path -> ref
        readonly Dictionary<string, string> groups = new(StringComparer.OrdinalIgnoreCase);     // absolute dir -> group
        readonly Dictionary<string, string> packageRefs = [];
        string mainGroup, productsGroup, frameworksGroup;
        OrderedDictionary<string, object> options;
        List<string> configNames;
        Dictionary<string, string> configTypes;

        string Rel(string abs) => XcProjFile.Normalize(Path.GetRelativePath(outDir, abs));

        public XcProjFile Build()
        {
            var name = XcPlist.Str(spec, "name");
            options = XcPlist.Dict(spec, "options") ?? [];
            p = new XcProjFile
            {
                XcodeprojPath = Path.Combine(outDir, name + ".xcodeproj"),
                SourceRoot = outDir,
                Root = new OrderedDictionary<string, object>(),
                Objects = new OrderedDictionary<string, object>(),
            };
            var configs = XcPlist.Dict(spec, "configs");
            configTypes = configs == null
                ? new Dictionary<string, string> { ["Debug"] = "debug", ["Release"] = "release" }
                : configs.ToDictionary(kv => kv.Key, kv => (XcPlist.Scalar(kv.Value) ?? "").ToLowerInvariant());
            configNames = configTypes.Keys.ToList();

            mainGroup = Add(XcProjFile.NewObject("PBXGroup", ("children", new List<object>()), ("sourceTree", "<group>")), "group:main");
            productsGroup = Add(XcProjFile.NewObject("PBXGroup", ("children", new List<object>()), ("name", "Products"), ("sourceTree", "<group>")), "group:products");

            // Packages first, so dependencies can name them.
            foreach (var (pkgName, pkg) in XcPlist.Dict(spec, "packages") ?? [])
            {
                var d = pkg as OrderedDictionary<string, object> ?? [];
                string id;
                if (XcPlist.Str(d, "path") is { } local)
                    id = Add(XcProjFile.NewObject("XCLocalSwiftPackageReference", ("relativePath", local)), "package:" + pkgName);
                else
                {
                    var req = new OrderedDictionary<string, object>();
                    if (XcPlist.Str(d, "from") is { } from) { req["kind"] = "upToNextMajorVersion"; req["minimumVersion"] = from; }
                    else if (XcPlist.Str(d, "majorVersion") is { } major) { req["kind"] = "upToNextMajorVersion"; req["minimumVersion"] = major; }
                    else if (XcPlist.Str(d, "minorVersion") is { } minor) { req["kind"] = "upToNextMinorVersion"; req["minimumVersion"] = minor; }
                    else if ((XcPlist.Str(d, "exactVersion") ?? XcPlist.Str(d, "version")) is { } exact) { req["kind"] = "exactVersion"; req["version"] = exact; }
                    else if (XcPlist.Str(d, "branch") is { } branch) { req["kind"] = "branch"; req["branch"] = branch; }
                    else if (XcPlist.Str(d, "revision") is { } rev) { req["kind"] = "revision"; req["revision"] = rev; }
                    var url = XcPlist.Str(d, "url") ?? (XcPlist.Str(d, "github") is { } gh ? $"https://github.com/{gh}" : null)
                              ?? throw new XcProjException($"Package {pkgName} needs a url, github or path");
                    id = Add(XcProjFile.NewObject("XCRemoteSwiftPackageReference", ("repositoryURL", url), ("requirement", req)), "package:" + pkgName);
                }
                packageRefs[pkgName] = id;
            }

            var targets = XcPlist.Dict(spec, "targets") ?? [];
            foreach (var (tname, _) in targets) targetIds[tname] = p.NewId("target:" + tname);
            foreach (var (tname, t) in targets) BuildTarget(tname, t as OrderedDictionary<string, object> ?? []);

            // Main group: source groups, Frameworks, Products.
            var main = Children(mainGroup);
            var ordered = main.OrderBy(c => Order((string)c)).ToList();   // groups first, stable
            main.Clear();
            main.AddRange(ordered);
            if (frameworksGroup != null) main.Add(frameworksGroup);
            main.Add(productsGroup);

            var attributes = new OrderedDictionary<string, object>
            {
                ["BuildIndependentTargetsInParallel"] = "YES",
                ["LastUpgradeCheck"] = XcPlist.Str(options, "xcodeVersion")?.Replace(".", "").PadRight(4, '0') ?? "1430",
            };
            if (XcPlist.Str(options, "organizationName") is { } org) attributes["ORGANIZATIONNAME"] = org;
            var project = XcProjFile.NewObject("PBXProject",
                ("attributes", attributes),
                ("buildConfigurationList", ConfigList("project", ProjectSettings(), "PBXProject")),
                ("compatibilityVersion", "Xcode 14.0"),
                ("developmentRegion", XcPlist.Str(options, "developmentLanguage") ?? "en"),
                ("hasScannedForEncodings", "0"),
                ("knownRegions", new List<object> { "Base", XcPlist.Str(options, "developmentLanguage") ?? "en" }),
                ("mainGroup", mainGroup),
                ("minimizedProjectReferenceProxies", "1"),
                ("productRefGroup", productsGroup),
                ("projectDirPath", ""),
                ("projectRoot", ""),
                ("targets", targets.Keys.Select(t => (object)targetIds[t]).ToList()));
            if (packageRefs.Count > 0) project["packageReferences"] = packageRefs.Values.Select(v => (object)v).ToList();
            var projectId = Add(project, "project:" + name);
            foreach (var dep in p.Objects.Values.OfType<OrderedDictionary<string, object>>().Where(o => XcPlist.Str(o, "isa") == "PBXContainerItemProxy"))
                dep["containerPortal"] = projectId;
            p.Root["archiveVersion"] = "1";
            p.Root["classes"] = new OrderedDictionary<string, object>();
            p.Root["objectVersion"] = "54";
            p.Root["objects"] = p.Objects;
            p.Root["rootObject"] = projectId;
            return p;
        }

        int Order(string id) => XcPlist.Str(p.Obj(id), "isa") == "PBXGroup" ? 0 : 1;

        string Add(OrderedDictionary<string, object> o, string seed) => p.Add(o, seed);
        List<object> Children(string group) => XcPlist.List(p.Obj(group), "children");

        // ---- settings ----

        static readonly string[] CommonProject =
        [
            "ALWAYS_SEARCH_USER_PATHS=NO", "CLANG_ANALYZER_NONNULL=YES", "CLANG_CXX_LANGUAGE_STANDARD=gnu++14", "CLANG_ENABLE_MODULES=YES",
            "CLANG_ENABLE_OBJC_ARC=YES", "CLANG_ENABLE_OBJC_WEAK=YES", "CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING=YES", "CLANG_WARN_BOOL_CONVERSION=YES",
            "CLANG_WARN_COMMA=YES", "CLANG_WARN_CONSTANT_CONVERSION=YES", "CLANG_WARN_DIRECT_OBJC_ISA_USAGE=YES_ERROR", "CLANG_WARN_DOCUMENTATION_COMMENTS=YES",
            "CLANG_WARN_EMPTY_BODY=YES", "CLANG_WARN_ENUM_CONVERSION=YES", "CLANG_WARN_INFINITE_RECURSION=YES", "CLANG_WARN_INT_CONVERSION=YES",
            "CLANG_WARN_OBJC_ROOT_CLASS=YES_ERROR", "CLANG_WARN_STRICT_PROTOTYPES=YES", "CLANG_WARN_SUSPICIOUS_MOVE=YES", "CLANG_WARN_UNREACHABLE_CODE=YES",
            "COPY_PHASE_STRIP=NO", "ENABLE_STRICT_OBJC_MSGSEND=YES", "GCC_C_LANGUAGE_STANDARD=gnu11", "GCC_NO_COMMON_BLOCKS=YES",
            "GCC_WARN_64_TO_32_BIT_CONVERSION=YES", "GCC_WARN_ABOUT_RETURN_TYPE=YES_ERROR", "GCC_WARN_UNDECLARED_SELECTOR=YES",
            "GCC_WARN_UNINITIALIZED_AUTOS=YES_AGGRESSIVE", "GCC_WARN_UNUSED_FUNCTION=YES", "GCC_WARN_UNUSED_VARIABLE=YES", "MTL_FAST_MATH=YES",
            "PRODUCT_NAME=$(TARGET_NAME)", "SWIFT_VERSION=5.0",
        ];
        static readonly string[] DebugProject =
        [
            "DEBUG_INFORMATION_FORMAT=dwarf", "ENABLE_TESTABILITY=YES", "GCC_DYNAMIC_NO_PIC=NO", "GCC_OPTIMIZATION_LEVEL=0",
            "GCC_PREPROCESSOR_DEFINITIONS=$(inherited) DEBUG=1", "MTL_ENABLE_DEBUG_INFO=INCLUDE_SOURCE", "ONLY_ACTIVE_ARCH=YES",
            "SWIFT_ACTIVE_COMPILATION_CONDITIONS=DEBUG", "SWIFT_OPTIMIZATION_LEVEL=-Onone",
        ];
        static readonly string[] ReleaseProject =
        [
            "DEBUG_INFORMATION_FORMAT=dwarf-with-dsym", "ENABLE_NS_ASSERTIONS=NO", "MTL_ENABLE_DEBUG_INFO=NO",
            "SWIFT_COMPILATION_MODE=wholemodule", "SWIFT_OPTIMIZATION_LEVEL=-O", "VALIDATE_PRODUCT=YES",
        ];

        Dictionary<string, OrderedDictionary<string, object>> ProjectSettings()
        {
            var result = new Dictionary<string, OrderedDictionary<string, object>>();
            foreach (var c in configNames)
            {
                var s = Preset(CommonProject);
                foreach (var kv in Preset(configTypes[c] == "release" ? ReleaseProject : DebugProject)) s[kv.Key] = kv.Value;
                Apply(s, XcPlist.Dict(spec, "settings"), c);
                result[c] = s;
            }
            return result;
        }

        static OrderedDictionary<string, object> Preset(IEnumerable<string> items)
        {
            var d = new OrderedDictionary<string, object>();
            foreach (var item in items) { var eq = item.IndexOf('='); d[item[..eq]] = item[(eq + 1)..]; }
            return d;
        }

        // A spec `settings:` value: plain key/values, or {base:, configs: {Debug: ...}}.
        void Apply(OrderedDictionary<string, object> into, OrderedDictionary<string, object> settings, string config)
        {
            if (settings == null) return;
            var structured = settings.ContainsKey("base") || settings.ContainsKey("configs") || settings.ContainsKey("groups");
            void Put(OrderedDictionary<string, object> s)
            {
                if (s == null) return;
                foreach (var (k, v) in s) into[k] = v is List<object> l ? l.Select(x => (object)XcPlist.Scalar(x)).ToList() : XcPlist.Scalar(v);
            }
            if (!structured) { Put(settings); return; }
            Put(XcPlist.Dict(settings, "base"));
            var configs = XcPlist.Dict(settings, "configs");
            if (configs != null)
                foreach (var (k, v) in configs)
                    if (k.Equals(config, StringComparison.OrdinalIgnoreCase) || k.Equals(configTypes[config], StringComparison.OrdinalIgnoreCase))
                        Put(v as OrderedDictionary<string, object>);
        }

        string ConfigList(string seed, Dictionary<string, OrderedDictionary<string, object>> settings, string owner)
        {
            var ids = new List<object>();
            foreach (var c in configNames)
                ids.Add(Add(XcProjFile.NewObject("XCBuildConfiguration", ("buildSettings", settings[c]), ("name", c)), $"config:{seed}:{c}"));
            return Add(XcProjFile.NewObject("XCConfigurationList", ("buildConfigurations", ids),
                ("defaultConfigurationIsVisible", "0"), ("defaultConfigurationName", configNames.Contains("Release") ? "Release" : configNames[^1])), $"configlist:{seed}");
        }

        // ---- targets ----

        static (string ProductType, string Extension, string FileType) Product(string type) => type switch
        {
            "application" => ("com.apple.product-type.application", "app", "wrapper.application"),
            "framework" => ("com.apple.product-type.framework", "framework", "wrapper.framework"),
            "framework.static" => ("com.apple.product-type.framework.static", "framework", "wrapper.framework"),
            "library.static" => ("com.apple.product-type.library.static", "a", "archive.ar"),
            "library.dynamic" => ("com.apple.product-type.library.dynamic", "dylib", "compiled.mach-o.dylib"),
            "bundle" => ("com.apple.product-type.bundle", "bundle", "wrapper.cfbundle"),
            "bundle.unit-test" => ("com.apple.product-type.bundle.unit-test", "xctest", "wrapper.cfbundle"),
            "bundle.ui-testing" => ("com.apple.product-type.bundle.ui-testing", "xctest", "wrapper.cfbundle"),
            "app-extension" => ("com.apple.product-type.app-extension", "appex", "wrapper.app-extension"),
            "extensionkit-extension" => ("com.apple.product-type.extensionkit-extension", "appex", "wrapper.extensionkit-extension"),
            "tool" or "commandline-tool" => ("com.apple.product-type.tool", "", "compiled.mach-o.executable"),
            "watchkit2-extension" => ("com.apple.product-type.watchkit2-extension", "appex", "wrapper.app-extension"),
            "application.watchapp2" => ("com.apple.product-type.application.watchapp2", "app", "wrapper.application"),
            "application.on-demand-install-capable" => ("com.apple.product-type.application.on-demand-install-capable", "app", "wrapper.application"),
            "xpc-service" => ("com.apple.product-type.xpc-service", "xpc", "wrapper.xpc-service"),
            "xcode-extension" => ("com.apple.product-type.xcode-extension", "appex", "wrapper.app-extension"),
            "instruments-package" => ("com.apple.product-type.instruments-package", "instrpkg", "com.apple.instruments.instrdst"),
            "driver-extension" => ("com.apple.product-type.driver-extension", "dext", "wrapper.driver-extension"),
            "system-extension" => ("com.apple.product-type.system-extension", "systemextension", "wrapper.system-extension"),
            _ => throw new XcProjException($"Unknown target type \"{type}\""),
        };

        static (string Sdk, string DeploymentKey) Platform(string platform) => platform switch
        {
            "iOS" => ("iphoneos", "IPHONEOS_DEPLOYMENT_TARGET"),
            "macOS" => ("macosx", "MACOSX_DEPLOYMENT_TARGET"),
            "tvOS" => ("appletvos", "TVOS_DEPLOYMENT_TARGET"),
            "watchOS" => ("watchos", "WATCHOS_DEPLOYMENT_TARGET"),
            "visionOS" => ("xros", "XROS_DEPLOYMENT_TARGET"),
            _ => throw new XcProjException($"Unknown platform \"{platform}\""),
        };

        void BuildTarget(string name, OrderedDictionary<string, object> t)
        {
            var type = XcPlist.Str(t, "type") ?? throw new XcProjException($"Target \"{name}\" is missing a type");
            var platform = XcPlist.Str(t, "platform") ?? throw new XcProjException($"Target \"{name}\" is missing a platform");
            var (productType, ext, productFileType) = Product(type);
            var (sdk, deploymentKey) = Platform(platform);
            var productName = XcPlist.Str(t, "productName") ?? name;
            var productFile = type.StartsWith("library.") ? $"lib{productName}.{ext}" : ext.Length > 0 ? $"{productName}.{ext}" : productName;
            var productRef = Add(XcProjFile.NewObject("PBXFileReference", ("explicitFileType", productFileType), ("includeInIndex", "0"),
                ("path", productFile), ("sourceTree", "BUILT_PRODUCTS_DIR")), "product:" + name);
            productRefs[name] = productRef;
            Children(productsGroup).Add(productRef);

            var sources = new List<string>();
            var resources = new List<string>();
            var headers = new List<string>();
            foreach (var entry in XcPlist.List(t, "sources") ?? (XcPlist.Str(t, "sources") is { } single ? [single] : []))
                AddSource(entry, sources, resources, headers, type);

            var phases = new List<object>();
            phases.AddRange(Scripts(name, XcPlist.List(t, "preBuildScripts") ?? XcPlist.List(t, "prebuildScripts")));
            if (headers.Count > 0 && type.StartsWith("framework")) phases.Add(Phase("PBXHeadersBuildPhase", headers, name));
            phases.Add(Phase("PBXSourcesBuildPhase", sources, name));
            phases.AddRange(Scripts(name, XcPlist.List(t, "postCompileScripts")));

            // Dependencies.
            var frameworks = new List<string>();
            var embed = new List<string>();
            var deps = new List<object>();
            var packageProducts = new List<object>();
            foreach (var d in XcPlist.List(t, "dependencies") ?? [])
            {
                var dep = d as OrderedDictionary<string, object> ?? [];
                var embedFlag = dep.TryGetValue("embed", out var e) ? XcPlist.Scalar(e) == "YES" : (bool?)null;
                if (XcPlist.Str(dep, "target") is { } other)
                {
                    if (!targetIds.TryGetValue(other, out var otherId)) throw new XcProjException($"Target \"{name}\" has a dependency on unknown target \"{other}\"");
                    var proxy = Add(XcProjFile.NewObject("PBXContainerItemProxy", ("containerPortal", ""), ("proxyType", "1"),
                        ("remoteGlobalIDString", otherId), ("remoteInfo", other)), $"proxy:{name}:{other}");
                    deps.Add(Add(XcProjFile.NewObject("PBXTargetDependency", ("target", otherId), ("targetProxy", proxy)), $"dependency:{name}:{other}"));
                    var otherType = XcPlist.Str(XcPlist.Dict(XcPlist.Dict(spec, "targets"), other), "type") ?? "";
                    if (otherType.StartsWith("framework") || otherType.StartsWith("library"))
                    {
                        frameworks.Add(productRefs.GetValueOrDefault(other) ?? p.NewId("product:" + other));
                        if ((embedFlag ?? otherType is "framework" or "library.dynamic") && type is "application" or "bundle.unit-test" or "bundle.ui-testing" or "app-extension")
                            embed.Add(productRefs.GetValueOrDefault(other) ?? p.NewId("product:" + other));
                    }
                    else if (otherType is "app-extension" or "extensionkit-extension" && type == "application")
                        embed.Add(productRefs.GetValueOrDefault(other) ?? p.NewId("product:" + other));
                }
                else if (XcPlist.Str(dep, "framework") is { } fw)
                {
                    var abs = Path.GetFullPath(Path.Combine(root, fw));
                    var r = FileRef(abs, inGroup: FrameworksGroup());
                    frameworks.Add(r);
                    if (embedFlag ?? (type == "application" && !fw.EndsWith(".a"))) embed.Add(r);
                }
                else if (XcPlist.Str(dep, "sdk") is { } sdkName)
                {
                    var path = sdkName.EndsWith(".tbd") || sdkName.EndsWith(".dylib") ? "usr/lib/" + sdkName : "System/Library/Frameworks/" + sdkName;
                    var r = Add(XcProjFile.NewObject("PBXFileReference", ("lastKnownFileType", XcProjFile.FileType(sdkName)), ("name", sdkName),
                        ("path", path), ("sourceTree", "SDKROOT")), "sdk:" + sdkName);
                    if (!Children(FrameworksGroup()).Contains(r)) Children(FrameworksGroup()).Add(r);
                    frameworks.Add(r);
                }
                else if (XcPlist.Str(dep, "package") is { } pkg)
                {
                    if (!packageRefs.TryGetValue(pkg, out var pkgRef)) throw new XcProjException($"Target \"{name}\" depends on unknown package \"{pkg}\"");
                    var products = XcPlist.List(dep, "products")?.Select(XcPlist.Scalar).ToList() ?? [XcPlist.Str(dep, "product") ?? pkg];
                    foreach (var product in products)
                    {
                        var dependency = Add(XcProjFile.NewObject("XCSwiftPackageProductDependency", ("package", pkgRef), ("productName", product)), $"packageproduct:{name}:{product}");
                        packageProducts.Add(dependency);
                        frameworks.Add("pkg:" + dependency);
                    }
                }
            }
            phases.Add(FrameworksPhase(frameworks, name));
            if (type is not ("library.static" or "tool")) phases.Add(Phase("PBXResourcesBuildPhase", resources, name));
            if (embed.Count > 0)
            {
                var files = embed.Select(r => (object)Add(XcProjFile.NewObject("PBXBuildFile", ("fileRef", r),
                    ("settings", new OrderedDictionary<string, object> { ["ATTRIBUTES"] = new List<object> { "CodeSignOnCopy", "RemoveHeadersOnCopy" } })), $"embed:{name}:{r}")).ToList();
                var exts = embed.Any(r => (XcPlist.Str(p.Obj(r), "path") ?? "").EndsWith(".appex"));
                phases.Add(Add(XcProjFile.NewObject("PBXCopyFilesBuildPhase", ("buildActionMask", "2147483647"), ("dstPath", ""),
                    ("dstSubfolderSpec", exts ? "13" : "10"), ("files", files), ("name", exts ? "Embed Foundation Extensions" : "Embed Frameworks"),
                    ("runOnlyForDeploymentPostprocessing", "0")), $"embedphase:{name}"));
            }
            phases.AddRange(Scripts(name, XcPlist.List(t, "postBuildScripts") ?? XcPlist.List(t, "postbuildScripts")));

            // Settings: platform and product presets, then project-wide target settings, then the target's.
            var settings = new Dictionary<string, OrderedDictionary<string, object>>();
            var deployment = XcPlist.Str(t, "deploymentTarget") ?? XcPlist.Str(XcPlist.Dict(options, "deploymentTarget"), platform);
            var info = InfoPlist(name, t);
            var entitlements = Entitlements(name, t);
            foreach (var c in configNames)
            {
                var s = new OrderedDictionary<string, object> { ["SDKROOT"] = sdk };
                if (platform == "iOS") { s["TARGETED_DEVICE_FAMILY"] = "1,2"; s["LD_RUNPATH_SEARCH_PATHS"] = new List<object> { "$(inherited)", "@executable_path/Frameworks" }; }
                if (platform == "macOS") { s["COMBINE_HIDPI_IMAGES"] = "YES"; s["LD_RUNPATH_SEARCH_PATHS"] = new List<object> { "$(inherited)", "@executable_path/../Frameworks" }; }
                if (deployment != null) s[deploymentKey] = deployment;
                if (type is "application" or "app-extension" or "framework" or "bundle" or "bundle.unit-test" or "bundle.ui-testing")
                    s["PRODUCT_BUNDLE_IDENTIFIER"] = (XcPlist.Str(options, "bundleIdPrefix") is { } prefix ? prefix + "." : "") + name.Replace("_", "-");
                if (type == "application") s["ASSETCATALOG_COMPILER_APPICON_NAME"] = "AppIcon";
                if (type.StartsWith("framework")) { s["DEFINES_MODULE"] = "YES"; s["DYLIB_INSTALL_NAME_BASE"] = "@rpath"; s["SKIP_INSTALL"] = "YES"; s["CURRENT_PROJECT_VERSION"] = "1"; s["VERSIONING_SYSTEM"] = "apple-generic"; }
                if (type is "bundle.unit-test" or "bundle.ui-testing")
                {
                    var host = (XcPlist.List(t, "dependencies") ?? []).OfType<OrderedDictionary<string, object>>().Select(d => XcPlist.Str(d, "target"))
                        .FirstOrDefault(x => x != null && XcPlist.Str(XcPlist.Dict(XcPlist.Dict(spec, "targets"), x), "type") == "application");
                    if (host != null && type == "bundle.unit-test")
                    {
                        s["BUNDLE_LOADER"] = "$(TEST_HOST)";
                        s["TEST_HOST"] = platform == "macOS" ? $"$(BUILT_PRODUCTS_DIR)/{host}.app/Contents/MacOS/{host}" : $"$(BUILT_PRODUCTS_DIR)/{host}.app/{host}";
                    }
                    if (host != null && type == "bundle.ui-testing") s["TEST_TARGET_NAME"] = host;
                }
                if (info != null) s["INFOPLIST_FILE"] = info;
                else if (type is "application" or "app-extension" or "framework" or "bundle" or "bundle.unit-test" or "bundle.ui-testing") s["GENERATE_INFOPLIST_FILE"] = "YES";
                if (entitlements != null) s["CODE_SIGN_ENTITLEMENTS"] = entitlements;
                Apply(s, XcPlist.Dict(t, "settings"), c);
                settings[c] = s;
            }

            var target = XcProjFile.NewObject("PBXNativeTarget",
                ("buildConfigurationList", ConfigList("target:" + name, settings, "PBXNativeTarget")),
                ("buildPhases", phases),
                ("buildRules", new List<object>()),
                ("dependencies", deps),
                ("name", name),
                ("productName", productName),
                ("productReference", productRef),
                ("productType", productType));
            if (packageProducts.Count > 0) target["packageProductDependencies"] = packageProducts;
            p.Objects[targetIds[name]] = target;
        }

        string FrameworksGroup() => frameworksGroup ??= Add(XcProjFile.NewObject("PBXGroup", ("children", new List<object>()),
            ("name", "Frameworks"), ("sourceTree", "<group>")), "group:frameworks");

        string Phase(string isa, List<string> refs, string target)
        {
            var files = refs.Distinct().Select(r => (object)Add(XcProjFile.NewObject("PBXBuildFile", ("fileRef", r)), $"buildfile:{target}:{isa}:{r}")).ToList();
            return Add(XcProjFile.NewObject(isa, ("buildActionMask", "2147483647"), ("files", files), ("runOnlyForDeploymentPostprocessing", "0")), $"phase:{target}:{isa}");
        }

        string FrameworksPhase(List<string> refs, string target)
        {
            var files = refs.Distinct().Select(r => (object)Add(r.StartsWith("pkg:")
                ? XcProjFile.NewObject("PBXBuildFile", ("productRef", r[4..]))
                : XcProjFile.NewObject("PBXBuildFile", ("fileRef", r)), $"buildfile:{target}:frameworks:{r}")).ToList();
            return Add(XcProjFile.NewObject("PBXFrameworksBuildPhase", ("buildActionMask", "2147483647"), ("files", files),
                ("runOnlyForDeploymentPostprocessing", "0")), $"phase:{target}:PBXFrameworksBuildPhase");
        }

        IEnumerable<object> Scripts(string target, List<object> scripts)
        {
            if (scripts == null) yield break;
            var n = 0;
            foreach (var s in scripts.OfType<OrderedDictionary<string, object>>())
            {
                var body = XcPlist.Str(s, "script") ?? (XcPlist.Str(s, "path") is { } path ? File.ReadAllText(Path.Combine(root, path)) : "");
                List<object> Strings(string key) => XcPlist.List(s, key)?.Select(x => (object)XcPlist.Scalar(x)).ToList() ?? [];
                yield return Add(XcProjFile.NewObject("PBXShellScriptBuildPhase",
                    ("buildActionMask", "2147483647"), ("files", new List<object>()),
                    ("inputFileListPaths", Strings("inputFileLists")), ("inputPaths", Strings("inputFiles")),
                    ("name", XcPlist.Str(s, "name") ?? "Run Script"),
                    ("outputFileListPaths", Strings("outputFileLists")), ("outputPaths", Strings("outputFiles")),
                    ("runOnlyForDeploymentPostprocessing", XcPlist.Str(s, "runOnlyWhenInstalling") == "YES" ? "1" : "0"),
                    ("shellPath", XcPlist.Str(s, "shell") ?? "/bin/sh"), ("shellScript", body.EndsWith('\n') ? body : body + "\n"),
                    ("showEnvVarsInLog", XcPlist.Str(s, "showEnvVars") == "NO" ? "0" : null)), $"script:{target}:{n++}");
            }
        }

        // Writes `info:` (path + properties) as an XML plist; returns INFOPLIST_FILE.
        string InfoPlist(string target, OrderedDictionary<string, object> t)
        {
            var info = XcPlist.Dict(t, "info");
            if (info == null) return null;
            var path = XcPlist.Str(info, "path") ?? throw new XcProjException($"Target \"{target}\" info needs a path");
            var abs = Path.GetFullPath(Path.Combine(root, path));
            var props = new OrderedDictionary<string, object>
            {
                ["CFBundleDevelopmentRegion"] = "$(DEVELOPMENT_LANGUAGE)",
                ["CFBundleExecutable"] = "$(EXECUTABLE_NAME)",
                ["CFBundleIdentifier"] = "$(PRODUCT_BUNDLE_IDENTIFIER)",
                ["CFBundleInfoDictionaryVersion"] = "6.0",
                ["CFBundleName"] = "$(PRODUCT_NAME)",
                ["CFBundlePackageType"] = "$(PRODUCT_BUNDLE_PACKAGE_TYPE)",
                ["CFBundleShortVersionString"] = "1.0",
                ["CFBundleVersion"] = "1",
            };
            foreach (var (k, v) in XcPlist.Dict(info, "properties") ?? []) props[k] = v;
            Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
            File.WriteAllText(abs, XcPlist.ToXml(props));
            return Rel(abs);
        }

        string Entitlements(string target, OrderedDictionary<string, object> t)
        {
            var ent = XcPlist.Dict(t, "entitlements");
            if (ent == null) return null;
            var path = XcPlist.Str(ent, "path") ?? throw new XcProjException($"Target \"{target}\" entitlements needs a path");
            var abs = Path.GetFullPath(Path.Combine(root, path));
            Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
            File.WriteAllText(abs, XcPlist.ToXml(XcPlist.Dict(ent, "properties") ?? new OrderedDictionary<string, object>()));
            return Rel(abs);
        }

        // ---- sources and groups ----

        // Folders Xcode treats as single files.
        static bool IsBundleDir(string path) => Path.GetExtension(path).ToLowerInvariant() is ".xcassets" or ".xcdatamodeld" or ".bundle" or ".framework"
            or ".xcframework" or ".playground" or ".docc" or ".scnassets" or ".rcproject" or ".mlpackage" or ".xcstrings" or ".app" or ".icon";

        void AddSource(object entry, List<string> sources, List<string> resources, List<string> headers, string targetType)
        {
            var d = entry as OrderedDictionary<string, object>;
            var path = d != null ? XcPlist.Str(d, "path") : XcPlist.Scalar(entry);
            if (path == null) return;
            var abs = Path.GetFullPath(Path.Combine(root, path));
            var excludes = (XcPlist.List(d, "excludes") ?? []).Select(x => Glob(XcPlist.Scalar(x))).ToList();
            var includes = XcPlist.List(d, "includes")?.Select(x => Glob(XcPlist.Scalar(x))).ToList();
            var phase = XcPlist.Str(d, "buildPhase");
            var optional = XcPlist.Str(d, "optional") == "YES";
            var parent = ParentGroup(Path.GetDirectoryName(abs)!);
            if (Directory.Exists(abs) && !IsBundleDir(abs))
            {
                var group = Group(abs, parent, XcPlist.Str(d, "name"));
                Walk(abs, abs, group, excludes, includes, phase, sources, resources, headers, targetType);
            }
            else if (File.Exists(abs) || Directory.Exists(abs))
                Classify(FileRef(abs, parent), abs, phase, sources, resources, headers, targetType);
            else if (!optional) throw new XcProjException($"Spec validation error: Target source \"{path}\" doesn't exist");
        }

        // The group a top-level source sits in: the main group, or with createIntermediateGroups
        // groups for each folder between the project root and it.
        string ParentGroup(string dir)
        {
            if (XcPlist.Str(options, "createIntermediateGroups") != "YES") return mainGroup;
            var rel = Path.GetRelativePath(root, dir);
            if (rel == "." || rel.StartsWith("..")) return mainGroup;
            var current = mainGroup;
            var at = root;
            foreach (var part in rel.Split(Path.DirectorySeparatorChar))
            {
                at = Path.Combine(at, part);
                current = Group(at, current, null);
            }
            return current;
        }

        string Group(string abs, string parent, string name)
        {
            if (groups.TryGetValue(abs, out var g)) return g;
            var parentPath = parent == mainGroup ? outDir : groups.FirstOrDefault(kv => kv.Value == parent).Key ?? outDir;
            var relPath = XcProjFile.Normalize(Path.GetRelativePath(parentPath, abs));
            g = Add(XcProjFile.NewObject("PBXGroup", ("children", new List<object>()), ("name", name ?? (relPath.Contains('/') ? Path.GetFileName(abs) : null)),
                ("path", relPath), ("sourceTree", "<group>")), "group:" + Rel(abs));
            groups[abs] = g;
            Children(parent).Add(g);
            return g;
        }

        void Walk(string dir, string top, string group, List<Func<string, bool>> excludes, List<Func<string, bool>> includes, string phase,
                  List<string> sources, List<string> resources, List<string> headers, string targetType)
        {
            var entries = Directory.EnumerateFileSystemEntries(dir).OrderBy(e => e, StringComparer.Ordinal).ToList();
            var variants = new Dictionary<string, string>();
            foreach (var e in entries)
            {
                var name = Path.GetFileName(e);
                if (name.StartsWith('.')) continue;
                var rel = XcProjFile.Normalize(Path.GetRelativePath(top, e));
                if (excludes.Any(x => x(rel))) continue;
                if (Directory.Exists(e) && !IsBundleDir(e))
                {
                    if (name.EndsWith(".lproj", StringComparison.OrdinalIgnoreCase))
                    {
                        // Localized files: one PBXVariantGroup per file name, children per language.
                        foreach (var f in Directory.EnumerateFiles(e).OrderBy(x => x, StringComparer.Ordinal))
                        {
                            var fname = Path.GetFileName(f);
                            if (fname.StartsWith('.')) continue;
                            var baseName = Path.GetExtension(fname) is ".storyboard" or ".xib" ? Path.GetFileNameWithoutExtension(fname) : fname;
                            if (!variants.TryGetValue(baseName, out var vg))
                            {
                                vg = Add(XcProjFile.NewObject("PBXVariantGroup", ("children", new List<object>()), ("name", fname), ("sourceTree", "<group>")), "variant:" + Rel(Path.Combine(dir, fname)));
                                variants[baseName] = vg;
                                Children(group).Add(vg);
                                resources.Add(vg);
                            }
                            var lang = Path.GetFileNameWithoutExtension(name);
                            var r = Add(XcProjFile.NewObject("PBXFileReference", ("lastKnownFileType", XcProjFile.FileType(f)), ("name", lang),
                                ("path", name + "/" + fname), ("sourceTree", "<group>")), "file:" + Rel(f));
                            fileRefs[f] = r;
                            Children(vg).Add(r);
                        }
                        continue;
                    }
                    var sub = Group(e, group, null);
                    Walk(e, top, sub, excludes, includes, phase, sources, resources, headers, targetType);
                    continue;
                }
                if (includes != null && !includes.Any(x => x(rel))) continue;
                Classify(FileRef(e, group), e, phase, sources, resources, headers, targetType);
            }
        }

        string FileRef(string abs, string inGroup)
        {
            if (fileRefs.TryGetValue(abs, out var r)) return r;
            var parentPath = inGroup == mainGroup || inGroup == frameworksGroup ? outDir : groups.FirstOrDefault(kv => kv.Value == inGroup).Key ?? outDir;
            var rel = XcProjFile.Normalize(Path.GetRelativePath(parentPath, abs));
            r = Add(XcProjFile.NewObject("PBXFileReference", ("lastKnownFileType", XcProjFile.FileType(abs)),
                ("name", rel.Contains('/') ? Path.GetFileName(abs) : null), ("path", rel), ("sourceTree", "<group>")), "file:" + Rel(abs));
            fileRefs[abs] = r;
            Children(inGroup).Add(r);
            return r;
        }

        static void Classify(string fileRef, string abs, string phase, List<string> sources, List<string> resources, List<string> headers, string targetType)
        {
            switch (phase ?? DefaultPhase(abs))
            {
                case "sources": sources.Add(fileRef); break;
                case "resources": resources.Add(fileRef); break;
                case "headers": headers.Add(fileRef); break;
            }
        }

        static string DefaultPhase(string path)
        {
            var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            return ext switch
            {
                "swift" or "m" or "mm" or "c" or "cc" or "cpp" or "cxx" or "s" or "metal" or "xcdatamodeld" or "xcmappingmodel" or "intentdefinition"
                    or "mlmodel" or "mlpackage" or "rcproject" or "docc" or "iig" or "d" or "def" or "r" => "sources",
                "h" or "hh" or "hpp" or "hxx" or "ipp" or "tpp" or "pch" => "headers",
                "plist" or "entitlements" or "xcconfig" or "md" or "modulemap" or "xcfilelist" or "gpx" or "apns" or "xctestplan"
                    or "framework" or "xcframework" or "a" or "dylib" or "tbd" or "yml" or "yaml" => "none",
                _ => "resources",
            };
        }

        static Func<string, bool> Glob(string pattern)
        {
            // ** matches across folders, * within one, ? one character; a bare folder excludes its contents.
            var rx = "^" + System.Text.RegularExpressions.Regex.Escape(pattern.TrimEnd('/'))
                .Replace(@"\*\*/", "(.*/)?").Replace(@"\*\*", ".*").Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]") + "(/.*)?$";
            var re = new System.Text.RegularExpressions.Regex(rx, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return s => re.IsMatch(s);
        }

        // ---- schemes ----

        // Targets with `scheme:` get a shared scheme (build, test, run, profile, analyze, archive).
        public void WriteSchemes(XcProjFile proj)
        {
            foreach (var (name, value) in XcPlist.Dict(spec, "targets") ?? [])
            {
                var t = value as OrderedDictionary<string, object>;
                if (t == null || !t.ContainsKey("scheme")) continue;
                var scheme = XcPlist.Dict(t, "scheme") ?? [];
                var dir = Path.Combine(proj.XcodeprojPath, "xcshareddata", "xcschemes");
                Directory.CreateDirectory(dir);
                var product = XcPlist.Str(proj.Obj(productRefs[name]), "path");
                string Ref(string target, string prod) => $"""
                         <BuildableReference
                            BuildableIdentifier = "primary"
                            BlueprintIdentifier = "{targetIds[target]}"
                            BuildableName = "{prod}"
                            BlueprintName = "{target}"
                            ReferencedContainer = "container:{proj.Name}.xcodeproj">
                         </BuildableReference>
                   """;
                var tests = new StringBuilder();
                foreach (var tt in XcPlist.List(scheme, "testTargets") ?? [])
                {
                    var testName = tt is OrderedDictionary<string, object> td ? XcPlist.Str(td, "name") : XcPlist.Scalar(tt);
                    if (testName == null || !targetIds.ContainsKey(testName)) continue;
                    tests.Append($"""
                               <TestableReference
                                  skipped = "NO">
                        {Ref(testName, XcPlist.Str(proj.Obj(productRefs[testName]), "path")).Replace("\n", "\n      ")}
                               </TestableReference>

                        """);
                }
                var xml = $"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <Scheme
                       LastUpgradeVersion = "1430"
                       version = "1.7">
                       <BuildAction
                          parallelizeBuildables = "YES"
                          buildImplicitDependencies = "YES">
                          <BuildActionEntries>
                             <BuildActionEntry
                                buildForTesting = "YES"
                                buildForRunning = "YES"
                                buildForProfiling = "YES"
                                buildForArchiving = "YES"
                                buildForAnalyzing = "YES">
                    {Ref(name, product)}
                             </BuildActionEntry>
                          </BuildActionEntries>
                       </BuildAction>
                       <TestAction
                          buildConfiguration = "Debug"
                          selectedDebuggerIdentifier = "Xcode.DebuggerFoundation.Debugger.LLDB"
                          selectedLauncherIdentifier = "Xcode.DebuggerFoundation.Launcher.LLDB"
                          shouldUseLaunchSchemeArgsEnv = "YES">
                          <Testables>
                    {tests}      </Testables>
                       </TestAction>
                       <LaunchAction
                          buildConfiguration = "Debug"
                          selectedDebuggerIdentifier = "Xcode.DebuggerFoundation.Debugger.LLDB"
                          selectedLauncherIdentifier = "Xcode.DebuggerFoundation.Launcher.LLDB"
                          launchStyle = "0"
                          useCustomWorkingDirectory = "NO"
                          ignoresPersistentStateOnLaunch = "NO"
                          debugDocumentVersioning = "YES"
                          debugServiceExtension = "internal"
                          allowLocationSimulation = "YES">
                          <BuildableProductRunnable
                             runnableDebuggingMode = "0">
                    {Ref(name, product).Replace("\n   ", "\n")}
                          </BuildableProductRunnable>
                       </LaunchAction>
                       <ProfileAction
                          buildConfiguration = "Release"
                          shouldUseLaunchSchemeArgsEnv = "YES"
                          savedToolIdentifier = ""
                          useCustomWorkingDirectory = "NO"
                          debugDocumentVersioning = "YES">
                       </ProfileAction>
                       <AnalyzeAction
                          buildConfiguration = "Debug">
                       </AnalyzeAction>
                       <ArchiveAction
                          buildConfiguration = "Release"
                          revealArchiveInOrganizer = "YES">
                       </ArchiveAction>
                    </Scheme>

                    """;
                File.WriteAllText(Path.Combine(dir, name + ".xcscheme"), xml);
            }
        }
    }
}
