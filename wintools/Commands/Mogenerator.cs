using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace WinTools;

// mogenerator: reads a Core Data model (.xcdatamodeld bundle, .xcdatamodel folder or its
// `contents` XML) and writes mogenerator's two-class pattern for every entity with a custom class:
// a machine file (_Entity) that is regenerated each run, and a human file (Entity) that is only
// created once. Objective-C by default, Swift with --swift. The model is plain XML, so this is a
// full port; only Xcode's long-retired binary model format is refused.
static class Mogenerator
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("mogenerator", "Generate Objective-C & Swift classes from a Core Data model", Run);

    const string Version = "1.32";

    const string Usage = """
        Usage: mogenerator [options] --model <path>
          -m, --model <path>          Path to the .xcdatamodeld, .xcdatamodel or its contents file
          -C, --configuration <name>  Only consider entities in this model configuration
          -O, --output-dir <dir>      Output directory for both machine and human files
          -M, --machine-dir <dir>     Output directory for machine (_Entity) files
          -H, --human-dir <dir>       Output directory for human (Entity) files
              --swift                 Generate Swift instead of Objective-C
              --base-class <class>    Custom base class instead of NSManagedObject
              --base-class-import <i> Import line for the custom base class
              --base-class-force <c>  Use this base class even for entities with a parent
              --includem <file>       Write an #import of every generated .m file to <file>
              --includeh <file>       Write an #import of every generated .h file to <file>
              --template-var <k=v>    Template variables (arc, literals, modules, frc are honoured)
              --v2                    Same as --template-var arc=true --template-var literals=true
                                      --template-var modules=true
              --orphaned              List machine files with no matching entity, then exit
              --version               Show the version
        """;

    sealed class Attr
    {
        public string Name, Type, ClassName;
        public bool Optional, Scalar, Transient;
    }

    sealed class Rel
    {
        public string Name, Destination, Inverse;
        public bool ToMany, Ordered, Optional;
    }

    sealed class Entity
    {
        public string Name, ClassName, Parent;
        public bool Abstract;
        public List<Attr> Attributes = [];
        public List<Rel> Relationships = [];
        public List<string> FetchRequests = [];
    }

    static int Run(string[] args)
    {
        string model = null, configuration = null, outDir = null, machineDir = null, humanDir = null;
        string baseClass = null, baseImport = null, baseForce = null, includeM = null, includeH = null;
        bool swift = false, orphaned = false;
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{args[i]} needs a value");
            var arg = args[i];
            // getopt_long also accepts --option=value.
            if (arg.StartsWith("--") && arg.Contains('='))
            {
                var eq = arg.IndexOf('=');
                args = [.. args[..i], arg[..eq], arg[(eq + 1)..], .. args[(i + 1)..]];
                arg = args[i];
            }
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(Usage); return 0;
                case "--version":
                    Console.WriteLine($"mogenerator {Version}. By Jonathan 'Wolf' Rentzsch + friends.");
                    return 0;
                case "-m" or "--model": model = Value(); break;
                case "-C" or "--configuration": configuration = Value(); break;
                case "-O" or "--output-dir": outDir = Value(); break;
                case "-M" or "--machine-dir": machineDir = Value(); break;
                case "-H" or "--human-dir": humanDir = Value(); break;
                case "--swift": swift = true; break;
                case "--base-class": baseClass = Value(); break;
                case "--base-class-import": baseImport = Value(); break;
                case "--base-class-force": baseForce = Value(); break;
                case "--includem": includeM = Value(); break;
                case "--includeh": includeH = Value(); break;
                case "--orphaned": orphaned = true; break;
                case "--v2": vars["arc"] = vars["literals"] = vars["modules"] = "true"; break;
                case "--template-var":
                    var kv = Value();
                    var at = kv.IndexOf('=');
                    if (at < 1) throw new UsageException($"--template-var expects key=value, got {kv}");
                    vars[kv[..at]] = kv[(at + 1)..];
                    break;
                // Template selection flags only matter for mogenerator's bundled .motemplate files,
                // whose output these generators reproduce.
                case "--template-path" or "--template-group" or "--machine-template" or "--human-template":
                    Value();
                    break;
                default: throw new UsageException($"unknown option {arg} (see mogenerator --help)");
            }
        }
        if (model == null) throw new UsageException("no model given; use --model <path>");
        machineDir ??= outDir ?? ".";
        humanDir ??= outDir ?? ".";

        var entities = Load(ResolveModel(model), configuration)
            .Where(e => e.ClassName is { Length: > 0 } && e.ClassName != "NSManagedObject")
            .ToList();
        if (baseForce != null) baseClass = baseForce;

        if (orphaned)
        {
            var known = entities.Select(e => "_" + e.ClassName).ToHashSet(StringComparer.Ordinal);
            if (Directory.Exists(machineDir))
                foreach (var file in Directory.EnumerateFiles(machineDir, "_*.*").Order(StringComparer.Ordinal))
                    if (Path.GetExtension(file) is ".h" or ".m" or ".swift" && !known.Contains(Path.GetFileNameWithoutExtension(file)))
                        Console.WriteLine(file);
            return 0;
        }

        Directory.CreateDirectory(machineDir);
        Directory.CreateDirectory(humanDir);
        var allByName = Load(ResolveModel(model), null).ToDictionary(e => e.Name, StringComparer.Ordinal);
        var machine = 0;
        var human = 0;
        var headers = new List<string>();
        var sources = new List<string>();
        foreach (var e in entities)
        {
            var super = baseForce ?? (e.Parent != null && allByName.TryGetValue(e.Parent, out var p) && !string.IsNullOrEmpty(p.ClassName)
                ? p.ClassName : baseClass ?? "NSManagedObject");
            string ClassOf(string entityName) =>
                allByName.TryGetValue(entityName ?? "", out var d) && !string.IsNullOrEmpty(d.ClassName) ? d.ClassName : "NSManagedObject";
            if (swift)
            {
                WriteIfChanged(Path.Combine(machineDir, $"_{e.ClassName}.swift"), SwiftMachine(e, super, ClassOf, baseImport));
                machine++;
                if (WriteIfMissing(Path.Combine(humanDir, $"{e.ClassName}.swift"), SwiftHuman(e))) human++;
            }
            else
            {
                var modules = vars.TryGetValue("modules", out var m) && m == "true";
                WriteIfChanged(Path.Combine(machineDir, $"_{e.ClassName}.h"), ObjcMachineH(e, super, ClassOf, baseImport, modules));
                WriteIfChanged(Path.Combine(machineDir, $"_{e.ClassName}.m"), ObjcMachineM(e, ClassOf));
                machine += 2;
                if (WriteIfMissing(Path.Combine(humanDir, $"{e.ClassName}.h"), $"#import \"_{e.ClassName}.h\"\n\nNS_ASSUME_NONNULL_BEGIN\n\n@interface {e.ClassName} : _{e.ClassName}\n// Custom logic goes here.\n@end\n\nNS_ASSUME_NONNULL_END\n")) human++;
                if (WriteIfMissing(Path.Combine(humanDir, $"{e.ClassName}.m"), $"#import \"{e.ClassName}.h\"\n\n@implementation {e.ClassName}\n\n// Custom logic goes here.\n\n@end\n")) human++;
                headers.Add($"{e.ClassName}.h");
                sources.Add($"{e.ClassName}.m");
                sources.Add($"_{e.ClassName}.m");
            }
        }
        if (includeM != null) File.WriteAllText(includeM, string.Concat(sources.Select(s => $"#import \"{s}\"\n")));
        if (includeH != null) File.WriteAllText(includeH, string.Concat(headers.Select(s => $"#import \"{s}\"\n")));
        Console.WriteLine($"{machine} machine files and {human} human files generated.");
        return 0;
    }

    // An .xcdatamodeld bundle holds versions; .xccurrentversion names the current one.
    static string ResolveModel(string path)
    {
        if (File.Exists(path)) return path;
        if (!Directory.Exists(path)) throw new InvalidOperationException($"{path}: no such model");
        if (path.TrimEnd('/', '\\').EndsWith(".xcdatamodeld", StringComparison.OrdinalIgnoreCase))
        {
            string current = null;
            var marker = Path.Combine(path, ".xccurrentversion");
            if (File.Exists(marker))
            {
                var doc = XDocument.Load(marker);
                var key = doc.Descendants("key").FirstOrDefault(k => k.Value == "_XCCurrentVersionName");
                current = (key?.ElementsAfterSelf().FirstOrDefault() as XElement)?.Value;
            }
            current ??= Directory.EnumerateDirectories(path, "*.xcdatamodel").Select(Path.GetFileName).Order(StringComparer.Ordinal).LastOrDefault();
            if (current == null) throw new InvalidOperationException($"{path}: no .xcdatamodel versions inside");
            path = Path.Combine(path, current);
        }
        var contents = Path.Combine(path, "contents");
        if (File.Exists(contents)) return contents;
        if (File.Exists(Path.Combine(path, "elements")))
            throw new InvalidOperationException($"{path}: binary (pre-Xcode 4) model format; open and save it in a current Xcode first");
        throw new InvalidOperationException($"{path}: no model contents file");
    }

    static List<Entity> Load(string contentsPath, string configuration)
    {
        XDocument doc;
        try
        {
            using var reader = XmlReader.Create(contentsPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
            doc = XDocument.Load(reader);
        }
        catch (XmlException ex) { throw new InvalidOperationException($"{contentsPath}: not a Core Data model ({ex.Message})"); }
        if (doc.Root?.Name.LocalName != "model") throw new InvalidOperationException($"{contentsPath}: not a Core Data model");

        HashSet<string> inConfig = null;
        if (configuration != null)
        {
            var conf = doc.Root.Elements("configuration").FirstOrDefault(c => (string)c.Attribute("name") == configuration)
                ?? throw new InvalidOperationException($"no configuration named {configuration} in the model");
            inConfig = conf.Elements("memberEntity").Select(m => (string)m.Attribute("name")).ToHashSet(StringComparer.Ordinal);
        }

        var list = new List<Entity>();
        foreach (var x in doc.Root.Elements("entity"))
        {
            var e = new Entity
            {
                Name = (string)x.Attribute("name"),
                ClassName = (string)x.Attribute("representedClassName"),
                Parent = (string)x.Attribute("parentEntity"),
                Abstract = (string)x.Attribute("isAbstract") == "YES",
            };
            if (inConfig != null && !inConfig.Contains(e.Name)) continue;
            foreach (var a in x.Elements("attribute"))
                e.Attributes.Add(new Attr
                {
                    Name = (string)a.Attribute("name"),
                    Type = (string)a.Attribute("attributeType") ?? "Undefined",
                    ClassName = (string)a.Attribute("customClassName") ?? (string)a.Attribute("attributeValueClassName"),
                    Optional = (string)a.Attribute("optional") == "YES",
                    Scalar = (string)a.Attribute("usesScalarValueType") == "YES",
                    Transient = (string)a.Attribute("transient") == "YES",
                });
            foreach (var r in x.Elements("relationship"))
                e.Relationships.Add(new Rel
                {
                    Name = (string)r.Attribute("name"),
                    Destination = (string)r.Attribute("destinationEntity"),
                    Inverse = (string)r.Attribute("inverseName"),
                    ToMany = (string)r.Attribute("toMany") == "YES",
                    Ordered = (string)r.Attribute("ordered") == "YES",
                    Optional = (string)r.Attribute("optional") == "YES",
                });
            foreach (var f in x.Elements("fetchedProperty")) e.FetchRequests.Add((string)f.Attribute("name"));
            e.Attributes.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            e.Relationships.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            list.Add(e);
        }
        return list;
    }

    // Core Data attribute type -> (object class, scalar C type, NSNumber accessor, Swift scalar type).
    static (string Obj, string Scalar, string Accessor, string Swift) TypeInfo(Attr a) => a.Type switch
    {
        "Integer 16" => ("NSNumber", "int16_t", "shortValue", "Int16"),
        "Integer 32" => ("NSNumber", "int32_t", "intValue", "Int32"),
        "Integer 64" => ("NSNumber", "int64_t", "longLongValue", "Int64"),
        "Double" => ("NSNumber", "double", "doubleValue", "Double"),
        "Float" => ("NSNumber", "float", "floatValue", "Float"),
        "Boolean" => ("NSNumber", "BOOL", "boolValue", "Bool"),
        "Decimal" => ("NSDecimalNumber", null, null, null),
        "String" => ("NSString", null, null, null),
        "Date" => ("NSDate", null, null, null),
        "Binary" => ("NSData", null, null, null),
        "UUID" => ("NSUUID", null, null, null),
        "URI" => ("NSURL", null, null, null),
        "Transformable" => (a.ClassName ?? "NSObject", null, null, null),
        _ => (a.ClassName ?? "NSObject", null, null, null),
    };

    static string SwiftType(Attr a) => a.Type switch
    {
        "String" => "String",
        "Date" => "Date",
        "Binary" => "Data",
        "UUID" => "UUID",
        "URI" => "URL",
        "Decimal" => "NSDecimalNumber",
        "Transformable" => a.ClassName ?? "AnyObject",
        _ when TypeInfo(a).Swift is { } scalar => a.Scalar && !a.Optional ? scalar : "NSNumber",
        _ => a.ClassName ?? "AnyObject",
    };

    static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    static string ObjcMachineH(Entity e, string super, Func<string, string> classOf, string baseImport, bool modules)
    {
        var s = new StringBuilder();
        s.Append($"// DO NOT EDIT. This file is machine-generated and constantly overwritten.\n// Make changes to {e.ClassName}.h instead.\n\n");
        s.Append(modules ? "@import CoreData;\n" : "#import <CoreData/CoreData.h>\n");
        if (super != "NSManagedObject")
            s.Append(baseImport != null && string.IsNullOrEmpty(e.Parent) ?$"{baseImport}\n" : $"#import \"{super}.h\"\n");
        s.Append("\nNS_ASSUME_NONNULL_BEGIN\n\n");
        foreach (var cls in e.Relationships.Select(r => classOf(r.Destination)).Where(c => c != "NSManagedObject").Distinct().Order(StringComparer.Ordinal))
            s.Append($"@class {cls};\n");
        if (e.Relationships.Count > 0) s.Append('\n');
        var c = e.ClassName;
        var superId = super == "NSManagedObject" ? "NSManagedObjectID" : $"{super}ID";
        s.Append($"@interface {c}ID : {superId} {{}}\n@end\n\n");
        s.Append($"@interface _{c} : {super}\n");
        s.Append("+ (instancetype)insertInManagedObjectContext:(NSManagedObjectContext *)moc_;\n");
        s.Append("+ (NSString*)entityName;\n");
        s.Append("+ (nullable NSEntityDescription*)entityInManagedObjectContext:(NSManagedObjectContext*)moc_;\n");
        s.Append($"@property (nonatomic, readonly, strong) {c}ID *objectID;\n");
        foreach (var a in e.Attributes)
        {
            var t = TypeInfo(a);
            var nullable = a.Optional ? ", nullable" : "";
            s.Append($"\n@property (nonatomic, strong{nullable}) {t.Obj}* {a.Name};\n");
            if (t.Scalar != null)
                s.Append($"\n@property (atomic) {t.Scalar} {a.Name}Value;\n- ({t.Scalar}){a.Name}Value;\n- (void)set{Cap(a.Name)}Value:({t.Scalar})value_;\n");
        }
        foreach (var r in e.Relationships)
        {
            var dest = classOf(r.Destination);
            var nullable = r.Optional ? ", nullable" : "";
            if (!r.ToMany) s.Append($"\n@property (nonatomic, strong{nullable}) {dest} *{r.Name};\n");
            else if (r.Ordered) s.Append($"\n@property (nonatomic, strong{nullable}) NSOrderedSet<{dest}*> *{r.Name};\n- (nullable NSMutableOrderedSet<{dest}*>*){r.Name}Set;\n");
            else s.Append($"\n@property (nonatomic, strong{nullable}) NSSet<{dest}*> *{r.Name};\n- (nullable NSMutableSet<{dest}*>*){r.Name}Set;\n");
        }
        s.Append("\n@end\n");
        foreach (var r in e.Relationships.Where(r => r.ToMany))
        {
            var dest = classOf(r.Destination);
            var coll = r.Ordered ? "NSOrderedSet" : "NSSet";
            var n = Cap(r.Name);
            s.Append($"\n@interface _{c} ({n}CoreDataGeneratedAccessors)\n");
            s.Append($"- (void)add{n}:({coll}<{dest}*>*)value_;\n- (void)remove{n}:({coll}<{dest}*>*)value_;\n");
            s.Append($"- (void)add{n}Object:({dest}*)value_;\n- (void)remove{n}Object:({dest}*)value_;\n");
            if (r.Ordered)
            {
                s.Append($"\n- (void)insertObject:({dest}*)value in{n}AtIndex:(NSUInteger)idx;\n");
                s.Append($"- (void)removeObjectFrom{n}AtIndex:(NSUInteger)idx;\n");
                s.Append($"- (void)replaceObjectIn{n}AtIndex:(NSUInteger)idx withObject:({dest}*)value;\n");
            }
            s.Append("\n@end\n");
        }
        s.Append($"\n@interface _{c} (CoreDataGeneratedPrimitiveAccessors)\n");
        foreach (var a in e.Attributes)
        {
            var t = TypeInfo(a);
            s.Append($"\n- (nullable {t.Obj}*)primitive{Cap(a.Name)};\n- (void)setPrimitive{Cap(a.Name)}:(nullable {t.Obj}*)value;\n");
            if (t.Scalar != null)
                s.Append($"\n- ({t.Scalar})primitive{Cap(a.Name)}Value;\n- (void)setPrimitive{Cap(a.Name)}Value:({t.Scalar})value_;\n");
        }
        foreach (var r in e.Relationships)
        {
            var dest = classOf(r.Destination);
            var type = !r.ToMany ? $"{dest}*" : r.Ordered ? $"NSMutableOrderedSet<{dest}*>*" : $"NSMutableSet<{dest}*>*";
            s.Append($"\n- ({type})primitive{Cap(r.Name)};\n- (void)setPrimitive{Cap(r.Name)}:({type})value;\n");
        }
        s.Append("\n@end\n");
        Constants(s, e, header: true);
        s.Append("\nNS_ASSUME_NONNULL_END\n");
        return s.ToString();
    }

    static void Constants(StringBuilder s, Entity e, bool header)
    {
        void Block(string suffix, IEnumerable<string> names)
        {
            var list = names.ToList();
            if (list.Count == 0) return;
            s.Append(header ? $"\n@interface {e.ClassName}{suffix}: NSObject \n" : $"\n@implementation {e.ClassName}{suffix} \n");
            foreach (var n in list)
                s.Append(header ? $"+ (NSString *){n};\n" : $"+ (NSString *){n} {{\n\treturn @\"{n}\";\n}}\n");
            s.Append("@end\n");
        }
        Block("Attributes", e.Attributes.Select(a => a.Name));
        Block("Relationships", e.Relationships.Select(r => r.Name));
        Block("FetchedProperties", e.FetchRequests);
    }

    static string ObjcMachineM(Entity e, Func<string, string> classOf)
    {
        var c = e.ClassName;
        var s = new StringBuilder();
        s.Append($"// DO NOT EDIT. This file is machine-generated and constantly overwritten.\n// Make changes to {c}.m instead.\n\n");
        s.Append($"#import \"_{c}.h\"\n\n@implementation {c}ID\n@end\n\n@implementation _{c}\n\n");
        s.Append($"+ (instancetype)insertInManagedObjectContext:(NSManagedObjectContext *)moc_ {{\n\tNSParameterAssert(moc_);\n\treturn [NSEntityDescription insertNewObjectForEntityForName:@\"{e.Name}\" inManagedObjectContext:moc_];\n}}\n\n");
        s.Append($"+ (NSString*)entityName {{\n\treturn @\"{e.Name}\";\n}}\n\n");
        s.Append($"+ (NSEntityDescription*)entityInManagedObjectContext:(NSManagedObjectContext*)moc_ {{\n\tNSParameterAssert(moc_);\n\treturn [NSEntityDescription entityForName:@\"{e.Name}\" inManagedObjectContext:moc_];\n}}\n\n");
        s.Append($"- ({c}ID*)objectID {{\n\treturn ({c}ID*)[super objectID];\n}}\n\n");
        s.Append("+ (NSSet*)keyPathsForValuesAffectingValueForKey:(NSString*)key {\n\tNSSet *keyPaths = [super keyPathsForValuesAffectingValueForKey:key];\n\n");
        foreach (var a in e.Attributes.Where(a => TypeInfo(a).Scalar != null))
            s.Append($"\tif ([key isEqualToString:@\"{a.Name}Value\"]) {{\n\t\tNSSet *affectingKey = [NSSet setWithObject:@\"{a.Name}\"];\n\t\tkeyPaths = [keyPaths setByAddingObjectsFromSet:affectingKey];\n\t\treturn keyPaths;\n\t}}\n");
        s.Append("\n\treturn keyPaths;\n}\n");
        foreach (var a in e.Attributes)
        {
            s.Append($"\n@dynamic {a.Name};\n");
            var t = TypeInfo(a);
            if (t.Scalar == null) continue;
            var n = Cap(a.Name);
            s.Append($"\n- ({t.Scalar}){a.Name}Value {{\n\tNSNumber *result = [self {a.Name}];\n\treturn [result {t.Accessor}];\n}}\n");
            s.Append($"\n- (void)set{n}Value:({t.Scalar})value_ {{\n\t[self set{n}:@(value_)];\n}}\n");
            s.Append($"\n- ({t.Scalar})primitive{n}Value {{\n\tNSNumber *result = [self primitive{n}];\n\treturn [result {t.Accessor}];\n}}\n");
            s.Append($"\n- (void)setPrimitive{n}Value:({t.Scalar})value_ {{\n\t[self setPrimitive{n}:@(value_)];\n}}\n");
        }
        foreach (var r in e.Relationships)
        {
            s.Append($"\n@dynamic {r.Name};\n");
            if (!r.ToMany) continue;
            var dest = classOf(r.Destination);
            var (mutable, accessor) = r.Ordered ? ("NSMutableOrderedSet", "mutableOrderedSetValueForKey") : ("NSMutableSet", "mutableSetValueForKey");
            s.Append($"\n- ({mutable}<{dest}*>*){r.Name}Set {{\n\t[self willAccessValueForKey:@\"{r.Name}\"];\n\n");
            s.Append($"\t{mutable}<{dest}*> *result = ({mutable}<{dest}*>*)[self {accessor}:@\"{r.Name}\"];\n\n");
            s.Append($"\t[self didAccessValueForKey:@\"{r.Name}\"];\n\treturn result;\n}}\n");
        }
        s.Append("\n@end\n");
        Constants(s, e, header: false);
        return s.ToString();
    }

    static string SwiftMachine(Entity e, string super, Func<string, string> classOf, string baseImport)
    {
        var c = e.ClassName;
        var s = new StringBuilder();
        s.Append($"// DO NOT EDIT. This file is machine-generated and constantly overwritten.\n// Make changes to {c}.swift instead.\n\n");
        s.Append("import Foundation\nimport CoreData\n");
        if (baseImport != null) s.Append(baseImport).Append('\n');
        s.Append('\n');
        void Enum(string suffix, IEnumerable<string> names)
        {
            var list = names.ToList();
            if (list.Count == 0) return;
            s.Append($"public enum {c}{suffix}: String {{\n");
            foreach (var n in list) s.Append($"    case {n} = \"{n}\"\n");
            s.Append("}\n\n");
        }
        Enum("Attributes", e.Attributes.Select(a => a.Name));
        Enum("Relationships", e.Relationships.Select(r => r.Name));
        Enum("FetchedProperties", e.FetchRequests);
        s.Append($"open class _{c}: {super} {{\n\n");
        s.Append("    // MARK: - Class methods\n\n");
        s.Append($"    {(super == "NSManagedObject" ? "open" : "override open")} class func entityName () -> String {{\n        return \"{e.Name}\"\n    }}\n\n");
        s.Append($"    {(super == "NSManagedObject" ? "open" : "override open")} class func entity(managedObjectContext: NSManagedObjectContext) -> NSEntityDescription? {{\n        return NSEntityDescription.entity(forEntityName: self.entityName(), in: managedObjectContext)\n    }}\n\n");
        s.Append($"    @nonobjc\n    open class func fetchRequest() -> NSFetchRequest<{c}> {{\n        return NSFetchRequest(entityName: self.entityName())\n    }}\n\n");
        s.Append("    // MARK: - Life cycle methods\n\n");
        s.Append("    public override init(entity: NSEntityDescription, insertInto context: NSManagedObjectContext?) {\n        super.init(entity: entity, insertInto: context)\n    }\n\n");
        s.Append($"    public convenience init?(managedObjectContext: NSManagedObjectContext) {{\n        guard let entity = _{c}.entity(managedObjectContext: managedObjectContext) else {{ return nil }}\n        self.init(entity: entity, insertInto: managedObjectContext)\n    }}\n\n");
        if (e.Attributes.Count > 0) s.Append("    // MARK: - Properties\n\n");
        foreach (var a in e.Attributes)
        {
            var type = SwiftType(a);
            var optional = a.Optional || type == "NSNumber" && !a.Scalar ? "?" : "";
            if (!a.Optional && type is "String" or "Date" or "Data" or "UUID" or "URL" or "NSDecimalNumber") optional = "!";
            s.Append($"    @NSManaged open\n    var {a.Name}: {type}{optional}\n\n");
        }
        if (e.Relationships.Count > 0) s.Append("    // MARK: - Relationships\n\n");
        foreach (var r in e.Relationships)
        {
            var dest = classOf(r.Destination);
            if (!r.ToMany)
            {
                s.Append($"    @NSManaged open\n    var {r.Name}: {dest}{(r.Optional ? "?" : "")}\n\n");
                continue;
            }
            var (coll, mutable) = r.Ordered ? ("NSOrderedSet", "NSMutableOrderedSet") : ("NSSet", "NSMutableSet");
            s.Append($"    @NSManaged open\n    var {r.Name}: {coll}\n\n");
            s.Append($"    open func {r.Name}Set() -> {mutable} {{\n        return self.{r.Name}.mutableCopy() as! {mutable}\n    }}\n\n");
        }
        s.Append("}\n");
        foreach (var r in e.Relationships.Where(r => r.ToMany))
        {
            var dest = classOf(r.Destination);
            var n = Cap(r.Name);
            var (coll, mutable) = r.Ordered ? ("NSOrderedSet", "NSMutableOrderedSet") : ("NSSet", "NSMutableSet");
            var union = r.Ordered ? "union(objects)" : "union(objects as Set<NSObject>)";
            var minus = r.Ordered ? "minus(objects)" : "minus(objects as Set<NSObject>)";
            s.Append($"\nextension _{c} {{\n\n");
            s.Append($"    open func add{n}(_ objects: {coll}) {{\n        let mutable = self.{r.Name}.mutableCopy() as! {mutable}\n        mutable.{union}\n        self.{r.Name} = mutable.copy() as! {coll}\n    }}\n\n");
            s.Append($"    open func remove{n}(_ objects: {coll}) {{\n        let mutable = self.{r.Name}.mutableCopy() as! {mutable}\n        mutable.{minus}\n        self.{r.Name} = mutable.copy() as! {coll}\n    }}\n\n");
            s.Append($"    open func add{n}Object(_ value: {dest}) {{\n        let mutable = self.{r.Name}.mutableCopy() as! {mutable}\n        mutable.add(value)\n        self.{r.Name} = mutable.copy() as! {coll}\n    }}\n\n");
            s.Append($"    open func remove{n}Object(_ value: {dest}) {{\n        let mutable = self.{r.Name}.mutableCopy() as! {mutable}\n        mutable.remove(value)\n        self.{r.Name} = mutable.copy() as! {coll}\n    }}\n\n");
            s.Append("}\n");
        }
        return s.ToString();
    }

    static string SwiftHuman(Entity e) =>
        $"import Foundation\n\n@objc({e.ClassName})\nopen class {e.ClassName}: _{e.ClassName} {{\n    // Custom logic goes here.\n}}\n";

    // Machine files are rewritten only when their text changes, so build tools see stable timestamps.
    static void WriteIfChanged(string path, string text)
    {
        if (File.Exists(path) && File.ReadAllText(path) == text) return;
        File.WriteAllText(path, text);
    }

    static bool WriteIfMissing(string path, string text)
    {
        if (File.Exists(path)) return false;
        File.WriteAllText(path, text);
        return true;
    }
}
