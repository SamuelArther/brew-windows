using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace WinTools;

// qtwaylandscanner (qtwayland): generates the QtWayland C++ wrappers for a Wayland protocol, the
// QtWayland::<interface> client classes over wayland-scanner's C stubs and the
// QtWaylandServer::<interface> classes with Resource bookkeeping, as Qt's own tool does.
static partial class WaylandScanner
{
    [ModuleInitializer]
    internal static void RegisterQt() =>
        Commands.Add("qtwaylandscanner", "Generate QtWayland C++ classes from Wayland protocol XML", RunQt);

    const string QtHelp = """
        Usage: qtwaylandscanner [client-header|server-header|client-code|server-code] specfile [--header-path=<path>] [--prefix=<prefix>] [--build-macro=<macro>] [--add-include=<include>]

        Generates QtWayland C++ classes for the interfaces in a Wayland protocol XML file and writes
        them to standard output: QtWayland::<interface> (client) or QtWaylandServer::<interface> (server).
        """;

    sealed class QtOptions
    {
        public string Spec, HeaderPath = "", Prefix = "", BuildMacro;
        public List<string> Includes = new();
    }

    static int RunQt(string[] args)
    {
        var o = new QtOptions();
        var positional = new List<string>();
        foreach (var arg in args)
        {
            if (arg is "-h" or "--help") { Console.WriteLine(QtHelp); return 0; }
            if (arg.StartsWith("--header-path=")) o.HeaderPath = arg["--header-path=".Length..];
            else if (arg.StartsWith("--prefix=")) o.Prefix = arg["--prefix=".Length..];
            else if (arg.StartsWith("--build-macro=")) o.BuildMacro = arg["--build-macro=".Length..];
            else if (arg.StartsWith("--add-include=")) o.Includes.Add(arg["--add-include=".Length..]);
            else if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg} (see qtwaylandscanner --help)");
            else positional.Add(arg);
        }
        // Qt 5 took the header path and prefix positionally.
        if (positional.Count is < 2 or > 4) throw new UsageException("expected a mode and a spec file (see --help)");
        if (positional.Count > 2) o.HeaderPath = positional[2];
        if (positional.Count > 3) o.Prefix = positional[3];
        var mode = positional[0];
        if (mode is not ("client-header" or "server-header" or "client-code" or "server-code"))
            throw new UsageException($"unknown mode '{mode}' (see --help)");
        o.Spec = positional[1];
        if (o.HeaderPath.Length > 0 && !o.HeaderPath.EndsWith('/')) o.HeaderPath += "/";

        Protocol protocol;
        try { protocol = Parse(File.ReadAllText(o.Spec), o.Spec, strict: false); }
        catch (ScannerError e) { Console.Error.WriteLine(e.Message); return 1; }
        catch (XmlException e) { Console.Error.WriteLine($"{o.Spec}:{e.LineNumber}: {e.Message}"); return 1; }
        catch (FileNotFoundException) { Console.Error.WriteLine($"Could not open {o.Spec}"); return 1; }

        var w = new StringBuilder();
        var server = mode.StartsWith("server");
        if (mode.EndsWith("header")) QtHeader(w, protocol, o, server);
        else QtCode(w, protocol, o, server);
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(new UTF8Encoding(false).GetBytes(w.ToString()));
        return 0;
    }

    // Event/request handler names drop the protocol prefix: wl_surface_enter -> surface_enter.
    static string Stripped(string name, QtOptions o)
    {
        if (o.Prefix.Length > 0 && name.StartsWith(o.Prefix)) return name[o.Prefix.Length..];
        if (name.StartsWith("qt_") || name.StartsWith("wl_")) return name[3..];
        return name;
    }

    static IEnumerable<Interface> QtInterfaces(Protocol p) => p.Interfaces.Where(i => i.Name is not ("wl_display" or "wl_registry"));

    static string ExportMacro(Protocol p, QtOptions o, bool server) =>
        o.BuildMacro ?? $"Q_WAYLAND_{(server ? "SERVER" : "CLIENT")}_{p.Name.Replace('-', '_').ToUpperInvariant()}_EXPORT";

    // C++ type of an argument; "in" is what the generated method takes, the other what the
    // C callback hands over.
    static string QtType(Arg a, bool server, bool cppSide)
    {
        switch (a.Type)
        {
            case ArgType.Fixed: return "wl_fixed_t";
            case ArgType.Int or ArgType.Fd: return "int32_t";
            case ArgType.Unsigned: return "uint32_t";
            case ArgType.String: return cppSide ? "const QString &" : "const char *";
            case ArgType.Array: return cppSide ? "const QByteArray &" : "wl_array *";
            case ArgType.Object or ArgType.NewId:
                if (server) return a.Type == ArgType.NewId ? "uint32_t" : "struct ::wl_resource *";
                return a.InterfaceName == null ? "void *" : $"struct ::{a.InterfaceName} *";
            default: return "int32_t";
        }
    }

    // Events reach client code as QString for strings and as the raw wl_array for arrays.
    static string ClientEventType(Arg a) => a.Type == ArgType.String ? "const QString &" : QtType(a, false, false);

    static string Join(string type, string name) => type.EndsWith('*') || type.EndsWith('&') ? type + name : type + " " + name;

    static void QtEnums(StringBuilder w, Interface i)
    {
        foreach (var e in i.Enums)
        {
            w.Append($"        enum {e.Name} {{\n");
            foreach (var entry in e.Entries)
            {
                w.Append($"            {e.Name}_{entry.Name} = {entry.Value},");
                if (entry.Summary != null) w.Append($" // {entry.Summary}");
                w.Append('\n');
            }
            w.Append("        };\n\n");
        }
    }

    static void QtPreamble(StringBuilder w, Protocol p, QtOptions o, bool server, bool header)
    {
        w.Append("// This file was generated by qtwaylandscanner\n");
        w.Append($"// source file is {o.Spec}\n\n");
    }

    static string GuardName(Protocol p, bool server) =>
        $"QT_WAYLAND_{(server ? "SERVER_" : "")}{p.Name.Replace('-', '_').ToUpperInvariant()}";

    static string FileStem(Protocol p) => p.Name.Replace('_', '-');

    // ---- headers ----

    static void QtHeader(StringBuilder w, Protocol p, QtOptions o, bool server)
    {
        QtPreamble(w, p, o, server, header: true);
        var guard = GuardName(p, server);
        var export = ExportMacro(p, o, server);
        w.Append($"#ifndef {guard}\n#define {guard}\n\n");
        if (server)
        {
            w.Append("#include \"wayland-server-core.h\"\n");
            w.Append($"#include <{o.HeaderPath}wayland-{FileStem(p)}-server-protocol.h>\n");
            w.Append("#include <QByteArray>\n#include <QMultiMap>\n#include <QString>\n\n");
            w.Append("#ifndef WAYLAND_VERSION_CHECK\n#define WAYLAND_VERSION_CHECK(major, minor, micro) \\\n");
            w.Append("    ((WAYLAND_VERSION_MAJOR > (major)) || \\\n");
            w.Append("    (WAYLAND_VERSION_MAJOR == (major) && WAYLAND_VERSION_MINOR > (minor)) || \\\n");
            w.Append("    (WAYLAND_VERSION_MAJOR == (major) && WAYLAND_VERSION_MINOR == (minor) && WAYLAND_VERSION_MICRO >= (micro)))\n#endif\n\n");
        }
        else
        {
            w.Append($"#include \"{o.HeaderPath}wayland-{FileStem(p)}-client-protocol.h\"\n");
            w.Append("#include <QByteArray>\n#include <QString>\n\nstruct wl_registry;\n\n");
        }
        foreach (var inc in o.Includes) w.Append($"#include {inc}\n");
        w.Append("QT_BEGIN_NAMESPACE\nQT_WARNING_PUSH\nQT_WARNING_DISABLE_GCC(\"-Wmissing-field-initializers\")\nQT_WARNING_DISABLE_CLANG(\"-Wmissing-field-initializers\")\n\n");
        if (o.BuildMacro == null)
        {
            w.Append($"#if !defined({export})\n#  if defined(QT_SHARED) && !defined(QT_STATIC)\n#    define {export} Q_DECL_EXPORT\n");
            w.Append($"#  else\n#    define {export}\n#  endif\n#endif\n\n");
        }
        w.Append(server ? "namespace QtWaylandServer {\n" : "namespace QtWayland {\n");
        var first = true;
        foreach (var i in QtInterfaces(p))
        {
            if (!first) w.Append('\n');
            first = false;
            if (server) QtServerClass(w, i, o, export);
            else QtClientClass(w, i, o, export);
        }
        w.Append("}\n\nQT_WARNING_POP\nQT_END_NAMESPACE\n\n#endif\n");
    }

    static void QtClientClass(StringBuilder w, Interface i, QtOptions o, string export)
    {
        var n = i.Name;
        var s = Stripped(n, o);
        w.Append($"    class {export} {n}\n    {{\n    public:\n");
        w.Append($"        {n}(struct ::wl_registry *registry, uint32_t id, int version);\n");
        w.Append($"        {n}(struct ::{n} *object);\n        {n}();\n\n        virtual ~{n}();\n\n");
        w.Append($"        void init(struct ::wl_registry *registry, uint32_t id, int version);\n");
        w.Append($"        void init(struct ::{n} *object);\n\n");
        w.Append($"        struct ::{n} *object() {{ return m_{n}; }}\n");
        w.Append($"        const struct ::{n} *object() const {{ return m_{n}; }}\n");
        w.Append($"        static {n} *fromObject(struct ::{n} *object);\n\n");
        w.Append("        bool isInitialized() const;\n\n        uint32_t version() const;\n");
        w.Append("        static const struct ::wl_interface *interface();\n\n");
        QtEnums(w, i);
        foreach (var m in i.Requests)
        {
            var ret = m.Args.FirstOrDefault(a => a.Type == ArgType.NewId);
            var type = ret == null ? "void " : ret.InterfaceName == null ? "void *" : $"struct ::{ret.InterfaceName} *";
            w.Append($"        {type}{m.Name}({ClientRequestParams(m)});\n");
        }
        if (i.Requests.Count > 0) w.Append('\n');
        if (i.Events.Count > 0)
        {
            w.Append("    protected:\n");
            foreach (var m in i.Events)
                w.Append($"        virtual void {s}_{m.Name}({string.Join(", ", m.Args.Select(a => Join(ClientEventType(a), a.Name)))});\n");
            w.Append('\n');
        }
        w.Append("    private:\n");
        if (i.Events.Count > 0)
        {
            w.Append("        void init_listener();\n");
            w.Append($"        static const struct {n}_listener m_{n}_listener;\n");
            foreach (var m in i.Events)
            {
                w.Append($"        static void handle_{m.Name}(\n            void *data,\n            struct ::{n} *object");
                foreach (var a in m.Args) w.Append($",\n            {Join(QtType(a, false, false), a.Name)}");
                w.Append(");\n");
            }
        }
        w.Append($"        struct ::{n} *m_{n};\n    }};\n");
    }

    static string ClientRequestParams(Message m)
    {
        var parts = new List<string>();
        foreach (var a in m.Args)
        {
            if (a.Type == ArgType.NewId)
            {
                if (a.InterfaceName == null) parts.Add("const struct ::wl_interface *interface, uint32_t version");
                continue;
            }
            parts.Add(Join(QtType(a, false, true), a.Name));
        }
        return string.Join(", ", parts);
    }

    static void QtServerClass(StringBuilder w, Interface i, QtOptions o, string export)
    {
        var n = i.Name;
        var s = Stripped(n, o);
        w.Append($"    class {export} {n}\n    {{\n    public:\n");
        w.Append($"        {n}(struct ::wl_client *client, uint32_t id, int version);\n");
        w.Append($"        {n}(struct ::wl_display *display, int version);\n");
        w.Append($"        {n}(struct ::wl_resource *resource);\n        {n}();\n\n        virtual ~{n}();\n\n");
        w.Append("        class Resource\n        {\n        public:\n");
        w.Append($"            Resource() : {s}_object(nullptr), handle(nullptr) {{}}\n            virtual ~Resource() {{}}\n\n");
        w.Append($"            {n} *{s}_object;\n            {n} *object() {{ return {s}_object; }} \n");
        w.Append("            struct ::wl_resource *handle;\n\n");
        w.Append("            struct ::wl_client *client() const { return wl_resource_get_client(handle); }\n");
        w.Append("            int version() const { return wl_resource_get_version(handle); }\n\n");
        w.Append("            static Resource *fromResource(struct ::wl_resource *resource);\n        };\n\n");
        w.Append("        void init(struct ::wl_client *client, uint32_t id, int version);\n");
        w.Append("        void init(struct ::wl_display *display, int version);\n");
        w.Append("        void init(struct ::wl_resource *resource);\n\n");
        w.Append("        Resource *add(struct ::wl_client *client, int version);\n");
        w.Append("        Resource *add(struct ::wl_client *client, uint32_t id, int version);\n");
        w.Append("        Resource *add(struct wl_list *resource_list, struct ::wl_client *client, uint32_t id, int version);\n\n");
        w.Append("        Resource *resource() { return m_resource; }\n        const Resource *resource() const { return m_resource; }\n\n");
        w.Append("        QMultiMap<struct ::wl_client*, Resource*> resourceMap() { return m_resource_map; }\n");
        w.Append("        const QMultiMap<struct ::wl_client*, Resource*> resourceMap() const { return m_resource_map; }\n\n");
        w.Append("        bool isGlobal() const { return m_global != nullptr; }\n        bool isResource() const { return m_resource != nullptr; }\n\n");
        w.Append("        static const struct ::wl_interface *interface();\n");
        w.Append("        static QByteArray interfaceName() { return interface()->name; }\n");
        w.Append("        static int interfaceVersion() { return interface()->version; }\n\n");
        QtEnums(w, i);
        foreach (var m in i.Events)
        {
            var parms = string.Join(", ", m.Args.Select(a => Join(QtType(a, true, true), a.Name)));
            w.Append($"        void send_{m.Name}({parms});\n");
            w.Append($"        void send_{m.Name}(struct ::wl_resource *resource{(parms.Length > 0 ? ", " + parms : "")});\n");
        }
        if (i.Events.Count > 0) w.Append('\n');
        w.Append($"    protected:\n        virtual Resource *{s}_allocate();\n\n");
        w.Append($"        virtual void {s}_bind_resource(Resource *resource);\n");
        w.Append($"        virtual void {s}_destroy_resource(Resource *resource);\n\n");
        foreach (var m in i.Requests)
        {
            w.Append($"        virtual void {s}_{m.Name}(Resource *resource");
            foreach (var a in m.Args) w.Append(", ").Append(Join(ServerRequestType(a), a.Name));
            w.Append(");\n");
        }
        if (i.Requests.Count > 0) w.Append('\n');
        w.Append("    private:\n");
        w.Append("        static void bind_func(struct ::wl_client *client, void *data, uint32_t version, uint32_t id);\n");
        w.Append("        static void destroy_func(struct ::wl_resource *client_resource);\n");
        w.Append("        static void display_destroy_func(struct ::wl_listener *listener, void *data);\n\n");
        w.Append("        Resource *bind(struct ::wl_client *client, uint32_t id, int version);\n");
        w.Append("        Resource *bind(struct ::wl_resource *handle);\n\n");
        if (i.Requests.Count > 0)
        {
            w.Append($"        static const struct ::{n}_interface m_{n}_interface;\n\n");
            foreach (var m in i.Requests)
            {
                w.Append($"        static void handle_{m.Name}(\n            ::wl_client *client,\n            struct wl_resource *resource");
                foreach (var a in m.Args) w.Append($",\n            {Join(ServerCallbackType(a), a.Name)}");
                w.Append(");\n");
            }
            w.Append('\n');
        }
        w.Append("        QMultiMap<struct ::wl_client*, Resource*> m_resource_map;\n        Resource *m_resource;\n");
        w.Append("        struct ::wl_global *m_global;\n");
        w.Append($"        struct DisplayDestroyedListener : ::wl_listener {{\n            {n} *parent;\n        }};\n");
        w.Append("        DisplayDestroyedListener m_displayDestroyedListener;\n    };\n");
    }

    // A request's arguments as the C++ handler receives them, and as libwayland-server passes them.
    static string ServerRequestType(Arg a) => a.Type switch
    {
        ArgType.String => "const QString &",
        ArgType.Array => "wl_array *",
        ArgType.Object => "struct ::wl_resource *",
        ArgType.NewId => a.InterfaceName == null ? "const QString &interface, uint32_t version, uint32_t" : "uint32_t",
        ArgType.Fixed => "wl_fixed_t",
        ArgType.Unsigned => "uint32_t",
        _ => "int32_t",
    };

    static string ServerCallbackType(Arg a) => a.Type switch
    {
        ArgType.String => "const char *",
        ArgType.NewId when a.InterfaceName == null => "const char *interface, uint32_t version, uint32_t",
        _ => ServerRequestType(a),
    };

    // ---- code ----

    static void QtCode(StringBuilder w, Protocol p, QtOptions o, bool server)
    {
        QtPreamble(w, p, o, server, header: false);
        w.Append(server ? $"#include \"qwayland-server-{FileStem(p)}.h\"\n\n" : $"#include \"{o.HeaderPath}qwayland-{FileStem(p)}.h\"\n\n");
        w.Append("QT_BEGIN_NAMESPACE\nQT_WARNING_PUSH\nQT_WARNING_DISABLE_GCC(\"-Wmissing-field-initializers\")\nQT_WARNING_DISABLE_CLANG(\"-Wmissing-field-initializers\")\n\n");
        w.Append(server ? "namespace QtWaylandServer {\n" : "namespace QtWayland {\n\n");
        if (!server)
        {
            w.Append("static inline void *wlRegistryBind(struct ::wl_registry *registry, uint32_t name, const struct ::wl_interface *interface, uint32_t version)\n{\n");
            w.Append("    const uint32_t bindOpCode = 0;\n");
            w.Append("    return (void *) wl_proxy_marshal_constructor_versioned((struct wl_proxy *) registry,\n");
            w.Append("        bindOpCode, interface, version, name, interface->name, version, nullptr);\n}\n\n");
        }
        var first = true;
        foreach (var i in QtInterfaces(p))
        {
            if (!first) w.Append('\n');
            first = false;
            if (server) QtServerCode(w, i, o);
            else QtClientCode(w, i, o);
        }
        w.Append("}\n\nQT_WARNING_POP\nQT_END_NAMESPACE\n");
    }

    static void QtClientCode(StringBuilder w, Interface i, QtOptions o)
    {
        var n = i.Name;
        var s = Stripped(n, o);
        var events = i.Events.Count > 0;
        var listen = events ? "\n        init_listener();" : "";
        w.Append($"    {n}::{n}(struct ::wl_registry *registry, uint32_t id, int version)\n    {{\n        init(registry, id, version);\n    }}\n\n");
        w.Append($"    {n}::{n}(struct ::{n} *obj)\n        : m_{n}(obj)\n    {{{listen}\n    }}\n\n");
        w.Append($"    {n}::{n}()\n        : m_{n}(nullptr)\n    {{\n    }}\n\n");
        w.Append($"    {n}::~{n}()\n    {{\n    }}\n\n");
        w.Append($"    void {n}::init(struct ::wl_registry *registry, uint32_t id, int version)\n    {{\n");
        w.Append($"        m_{n} = static_cast<struct ::{n} *>(wlRegistryBind(registry, id, &::{n}_interface, version));{listen}\n    }}\n\n");
        w.Append($"    void {n}::init(struct ::{n} *obj)\n    {{\n        m_{n} = obj;{listen}\n    }}\n\n");
        w.Append($"    {n} *{n}::fromObject(struct ::{n} *object)\n    {{\n");
        if (events)
            w.Append($"        if (wl_proxy_get_listener((struct ::wl_proxy *)object) != (void *)&m_{n}_listener)\n            return nullptr;\n");
        w.Append($"        return static_cast<{n} *>({n}_get_user_data(object));\n    }}\n\n");
        w.Append($"    bool {n}::isInitialized() const\n    {{\n        return m_{n} != nullptr;\n    }}\n\n");
        w.Append($"    uint32_t {n}::version() const\n    {{\n        return wl_proxy_get_version(reinterpret_cast<wl_proxy*>(m_{n}));\n    }}\n\n");
        w.Append($"    const struct wl_interface *{n}::interface()\n    {{\n        return &::{n}_interface;\n    }}\n");

        foreach (var m in i.Requests)
        {
            var ret = m.Args.FirstOrDefault(a => a.Type == ArgType.NewId);
            var type = ret == null ? "void " : ret.InterfaceName == null ? "void *" : $"struct ::{ret.InterfaceName} *";
            w.Append($"\n    {type}{n}::{m.Name}({ClientRequestParams(m)})\n    {{\n");
            foreach (var a in m.Args.Where(a => a.Type == ArgType.Array))
                w.Append($"        struct wl_array {a.Name}_data;\n        {a.Name}_data.size = {a.Name}.size();\n" +
                         $"        {a.Name}_data.data = static_cast<void *>(const_cast<char *>({a.Name}.constData()));\n        {a.Name}_data.alloc = 0;\n\n");
            var call = new StringBuilder();
            call.Append($"::{n}_{m.Name}(\n            m_{n}");
            foreach (var a in m.Args)
            {
                if (a.Type == ArgType.NewId)
                {
                    if (a.InterfaceName == null) call.Append(",\n            interface,\n            version");
                    continue;
                }
                var value = a.Type switch
                {
                    ArgType.String => $"{a.Name}.toUtf8().constData()",
                    ArgType.Array => $"&{a.Name}_data",
                    _ => a.Name,
                };
                call.Append($",\n            {value}");
            }
            call.Append(')');
            if (ret != null) w.Append($"        return {call};\n");
            else w.Append($"        {call};\n");
            if (m.Destructor) w.Append($"        m_{n} = nullptr;\n");
            w.Append("    }\n");
        }

        foreach (var m in i.Events)
        {
            w.Append($"\n    void {n}::{s}_{m.Name}({string.Join(", ", m.Args.Select(ClientEventType))})\n    {{\n    }}\n\n");
            w.Append($"    void {n}::handle_{m.Name}(\n        void *data,\n        struct ::{n} *object");
            foreach (var a in m.Args) w.Append($",\n        {Join(QtType(a, false, false), a.Name)}");
            w.Append($")\n    {{\n        Q_UNUSED(object);\n        static_cast<{n} *>(data)->{s}_{m.Name}(");
            w.Append(string.Join(",", m.Args.Select(a => "\n            " + (a.Type == ArgType.String ? $"QString::fromUtf8({a.Name})" : a.Name))));
            w.Append(");\n    }\n");
        }
        if (events)
        {
            w.Append($"\n    const struct {n}_listener {n}::m_{n}_listener = {{\n");
            foreach (var m in i.Events) w.Append($"        {n}::handle_{m.Name},\n");
            w.Append("    };\n\n");
            w.Append($"    void {n}::init_listener()\n    {{\n        {n}_add_listener(m_{n}, &m_{n}_listener, this);\n    }}\n");
        }
    }

    static void QtServerCode(StringBuilder w, Interface i, QtOptions o)
    {
        var n = i.Name;
        var s = Stripped(n, o);
        const string init = "\n        : m_resource_map()\n        , m_resource(nullptr)\n        , m_global(nullptr)\n";
        w.Append($"    {n}::{n}(struct ::wl_client *client, uint32_t id, int version){init}    {{\n        init(client, id, version);\n    }}\n\n");
        w.Append($"    {n}::{n}(struct ::wl_display *display, int version){init}    {{\n        init(display, version);\n    }}\n\n");
        w.Append($"    {n}::{n}(struct ::wl_resource *resource){init}    {{\n        init(resource);\n    }}\n\n");
        w.Append($"    {n}::{n}(){init}    {{\n    }}\n\n");
        w.Append($"    {n}::~{n}()\n    {{\n        for (auto resource : std::as_const(m_resource_map))\n            resource->{s}_object = nullptr;\n\n");
        w.Append($"        if (m_resource)\n            m_resource->{s}_object = nullptr;\n\n");
        w.Append("        if (m_global) {\n            wl_global_destroy(m_global);\n            wl_list_remove(&m_displayDestroyedListener.link);\n        }\n    }\n\n");
        w.Append($"    void {n}::init(struct ::wl_client *client, uint32_t id, int version)\n    {{\n        m_resource = bind(client, id, version);\n    }}\n\n");
        w.Append($"    void {n}::init(struct ::wl_resource *resource)\n    {{\n        m_resource = bind(resource);\n    }}\n\n");
        w.Append($"    {n}::Resource *{n}::add(struct ::wl_client *client, int version)\n    {{\n        Resource *resource = bind(client, 0, version);\n        m_resource_map.insert(client, resource);\n        return resource;\n    }}\n\n");
        w.Append($"    {n}::Resource *{n}::add(struct ::wl_client *client, uint32_t id, int version)\n    {{\n        Resource *resource = bind(client, id, version);\n        m_resource_map.insert(client, resource);\n        return resource;\n    }}\n\n");
        w.Append($"    {n}::Resource *{n}::add(struct wl_list *resource_list, struct ::wl_client *client, uint32_t id, int version)\n    {{\n");
        w.Append("        Resource *resource = bind(client, id, version);\n        wl_list_insert(resource_list, wl_resource_get_link(resource->handle));\n        return resource;\n    }\n\n");
        w.Append($"    void {n}::init(struct ::wl_display *display, int version)\n    {{\n");
        w.Append($"        m_global = wl_global_create(display, &::{n}_interface, version, this, bind_func);\n");
        w.Append($"        m_displayDestroyedListener.notify = {n}::display_destroy_func;\n        m_displayDestroyedListener.parent = this;\n");
        w.Append("        wl_display_add_destroy_listener(display, &m_displayDestroyedListener);\n    }\n\n");
        w.Append($"    const struct wl_interface *{n}::interface()\n    {{\n        return &::{n}_interface;\n    }}\n\n");
        w.Append($"    {n}::Resource *{n}::{s}_allocate()\n    {{\n        return new Resource;\n    }}\n\n");
        w.Append($"    void {n}::{s}_bind_resource(Resource *)\n    {{\n    }}\n\n");
        w.Append($"    void {n}::{s}_destroy_resource(Resource *)\n    {{\n    }}\n\n");
        w.Append($"    void {n}::bind_func(struct ::wl_client *client, void *data, uint32_t version, uint32_t id)\n    {{\n");
        w.Append($"        {n} *that = static_cast<{n} *>(data);\n        that->add(client, id, version);\n    }}\n\n");
        w.Append($"    void {n}::display_destroy_func(struct ::wl_listener *listener, void *data)\n    {{\n        Q_UNUSED(data);\n");
        w.Append($"        {n} *that = static_cast<{n}::DisplayDestroyedListener *>(listener)->parent;\n        that->m_global = nullptr;\n    }}\n\n");
        w.Append($"    void {n}::destroy_func(struct ::wl_resource *client_resource)\n    {{\n");
        w.Append("        Resource *resource = Resource::fromResource(client_resource);\n        Q_ASSERT(resource);\n");
        w.Append($"        {n} *that = resource->{s}_object;\n        if (Q_LIKELY(that)) {{\n");
        w.Append($"            that->m_resource_map.remove(resource->client(), resource);\n            that->{s}_destroy_resource(resource);\n\n");
        w.Append($"            that = resource->{s}_object;\n            if (that && that->m_resource == resource)\n                that->m_resource = nullptr;\n        }}\n        delete resource;\n    }}\n\n");
        w.Append($"    {n}::Resource *{n}::bind(struct ::wl_client *client, uint32_t id, int version)\n    {{\n");
        w.Append("        Q_ASSERT_X(!wl_client_get_object(client, id), \"QWaylandObject bind\", QStringLiteral(\"binding to object %1 more than once\").arg(id).toLocal8Bit().constData());\n");
        w.Append($"        struct ::wl_resource *handle = wl_resource_create(client, &::{n}_interface, version, id);\n        return bind(handle);\n    }}\n\n");
        w.Append($"    {n}::Resource *{n}::bind(struct ::wl_resource *handle)\n    {{\n        Resource *resource = {s}_allocate();\n        resource->{s}_object = this;\n\n");
        w.Append(i.Requests.Count > 0
            ? $"        wl_resource_set_implementation(handle, &m_{n}_interface, resource, destroy_func);\n"
            : "        wl_resource_set_implementation(handle, nullptr, resource, destroy_func);\n");
        w.Append($"        resource->handle = handle;\n        {s}_bind_resource(resource);\n        return resource;\n    }}\n");
        w.Append($"    {n}::Resource *{n}::Resource::fromResource(struct ::wl_resource *resource)\n    {{\n        if (Q_UNLIKELY(!resource))\n            return nullptr;\n");
        w.Append(i.Requests.Count > 0
            ? $"        if (wl_resource_instance_of(resource, &::{n}_interface, &m_{n}_interface))\n"
            : $"        if (wl_resource_instance_of(resource, &::{n}_interface, nullptr))\n");
        w.Append("            return static_cast<Resource *>(wl_resource_get_user_data(resource));\n        return nullptr;\n    }\n");

        if (i.Requests.Count > 0)
        {
            w.Append($"\n    const struct ::{n}_interface {n}::m_{n}_interface = {{\n");
            foreach (var m in i.Requests) w.Append($"        {n}::handle_{m.Name},\n");
            w.Append("    };\n");
            foreach (var m in i.Requests)
            {
                w.Append($"\n    void {n}::{s}_{m.Name}(Resource *");
                foreach (var a in m.Args)
                    w.Append(", ").Append(a.Type == ArgType.NewId && a.InterfaceName == null ? "const QString &, uint32_t, uint32_t" : ServerRequestType(a).TrimEnd());
                w.Append(")\n    {\n    }\n");
            }
            foreach (var m in i.Requests)
            {
                w.Append($"\n\n    void {n}::handle_{m.Name}(\n        ::wl_client *client,\n        struct wl_resource *resource");
                foreach (var a in m.Args) w.Append($",\n        {Join(ServerCallbackType(a), a.Name)}");
                w.Append(")\n    {\n        Q_UNUSED(client);\n        Resource *r = Resource::fromResource(resource);\n");
                w.Append($"        if (Q_UNLIKELY(!r->{s}_object)) {{\n");
                if (m.Destructor) w.Append("            wl_resource_destroy(resource);\n");
                w.Append("            return;\n        }\n");
                w.Append($"        static_cast<{n} *>(r->{s}_object)->{s}_{m.Name}(\n            r");
                foreach (var a in m.Args)
                {
                    if (a.Type == ArgType.NewId && a.InterfaceName == null)
                        w.Append($",\n            QString::fromUtf8(interface),\n            version,\n            {a.Name}");
                    else
                        w.Append(",\n            ").Append(a.Type == ArgType.String ? $"QString::fromUtf8({a.Name})" : a.Name);
                }
                w.Append(");\n    }\n");
            }
        }

        foreach (var m in i.Events)
        {
            var parms = string.Join(", ", m.Args.Select(a => Join(QtType(a, true, true), a.Name)));
            w.Append($"\n    void {n}::send_{m.Name}({parms})\n    {{\n");
            w.Append($"        Q_ASSERT_X(m_resource, \"{n}::{m.Name}\", \"Uninitialised resource\");\n");
            w.Append($"        if (Q_UNLIKELY(!m_resource)) {{\n            qWarning(\"could not call {n}::{m.Name} as it's not initialised\");\n            return;\n        }}\n");
            w.Append($"        send_{m.Name}(\n            m_resource->handle");
            foreach (var a in m.Args) w.Append($",\n            {a.Name}");
            w.Append(");\n    }\n\n");
            w.Append($"    void {n}::send_{m.Name}(struct ::wl_resource *resource{(parms.Length > 0 ? ", " + parms : "")})\n    {{\n");
            foreach (var a in m.Args.Where(a => a.Type == ArgType.Array))
                w.Append($"        struct wl_array {a.Name}_data;\n        {a.Name}_data.size = {a.Name}.size();\n" +
                         $"        {a.Name}_data.data = static_cast<void *>(const_cast<char *>({a.Name}.constData()));\n        {a.Name}_data.alloc = 0;\n\n");
            w.Append($"        {n}_send_{m.Name}(\n            resource");
            foreach (var a in m.Args)
                w.Append(",\n            ").Append(a.Type switch
                {
                    ArgType.String => $"{a.Name}.toUtf8().constData()",
                    ArgType.Array => $"&{a.Name}_data",
                    _ => a.Name,
                });
            w.Append(");\n    }\n");
        }
    }
}
