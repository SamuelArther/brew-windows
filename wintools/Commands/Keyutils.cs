using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// keyutils' keyctl on top of Windows Credential Manager. Every key is a generic credential named
// "keyctl/<keyring>/<type>/<description>"; keyrings made with newring are "keyctl/<parent>/keyring/<name>"
// markers. @u/@us/@s/@p/@t are the built-in rings: @u lives as long as the user profile
// (CRED_PERSIST_LOCAL_MACHINE), the others last until logoff (CRED_PERSIST_SESSION), like Linux session keys.
// Serial numbers are a stable 31-bit hash of the credential name, so they survive between runs.
static class Keyutils
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("keyctl", "Key management on Windows Credential Manager (keyutils)", Run);

    const string Prefix = "keyctl/";
    const string KeyutilsVersion = "1.6.3";

    // Built-in rings: Linux's special serials, the ring name used in credential names, and how long it lasts.
    static readonly (string Spec, int Special, string Name, uint Persist, string Title)[] Builtin =
    [
        ("@t", -1, "thread", CRED_PERSIST_SESSION, "Thread Keyring"),
        ("@p", -2, "process", CRED_PERSIST_SESSION, "Process Keyring"),
        ("@s", -3, "session", CRED_PERSIST_SESSION, "Session Keyring"),
        ("@u", -4, "user", CRED_PERSIST_LOCAL_MACHINE, "User Keyring"),
        ("@us", -5, "user_session", CRED_PERSIST_SESSION, "User Session Keyring"),
    ];

    static readonly string[] Unsupported = ["timeout", "chown", "chgrp", "chmod", "setperm", "link", "move", "security",
        "restrict_keyring", "dh_compute", "dh_compute_kdf", "dh_compute_kdf_oi", "pkey_query", "pkey_encrypt",
        "pkey_decrypt", "pkey_sign", "pkey_verify", "watch", "watch_add", "watch_rm", "watch_session", "watch_sync",
        "get_persistent", "reap", "session", "instantiate", "pinstantiate", "negate", "reject", "request2", "prequest2"];

    // A key or keyring as stored: ring it sits in, type ("keyring" for rings), description.
    sealed record Item(string Ring, string Type, string Desc, uint Persist)
    {
        public string Target => $"{Prefix}{Ring}/{Type}/{Desc}";
        public bool IsRing => Type == "keyring";
        public int Serial => IsRing ? RingSerial(Desc) : Hash(Target);
    }

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("""
                Usage: keyctl <command> [args]
                Keys are kept in Windows Credential Manager (generic credentials named keyctl/...).
                  keyctl --version
                  keyctl supports [<cap>]
                  keyctl show [<keyring>]
                  keyctl add <type> <desc> <data> <keyring>
                  keyctl padd <type> <desc> <keyring>          (data from stdin)
                  keyctl request <type> <desc> [<dest_keyring>]
                  keyctl search <keyring> <type> <desc> [<dest_keyring>]
                  keyctl id <key>
                  keyctl update <key> <data>
                  keyctl pupdate <key>                         (data from stdin)
                  keyctl print|pipe|read <key>
                  keyctl describe|rdescribe <key> [sep]
                  keyctl list|rlist <keyring>
                  keyctl newring <name> <keyring>
                  keyctl new_session [<name>]
                  keyctl unlink <key> [<keyring>]
                  keyctl revoke|invalidate <key>
                  keyctl clear <keyring>
                  keyctl purge <type> | purge [-p] [-i] <type> <desc> | purge -s <type> <desc>
                Types: user, logon, big_key, keyring. Rings: @u @us @s @p @t, a serial, or %:<name>.
                """);
            return 0;
        }
        var cmd = args[0];
        var rest = args[1..];
        if (cmd is "--version" or "-v")
        {
            Console.WriteLine($"keyctl from keyutils-{KeyutilsVersion} (wintools {Commands.Version}, Windows Credential Manager)");
            return 0;
        }
        if (Unsupported.Contains(cmd))
        {
            Console.Error.WriteLine($"keyctl: '{cmd}' isn't available: Windows Credential Manager has no key permissions, timeouts, links or kernel crypto");
            return 1;
        }
        switch (cmd)
        {
            case "supports": return Supports(rest);
            case "show": return Show(rest);
            case "add": Need(rest, 4, 4, "add <type> <desc> <data> <keyring>"); return Add(rest[0], rest[1], Encoding.UTF8.GetBytes(rest[2]), rest[3]);
            case "padd": Need(rest, 3, 3, "padd <type> <desc> <keyring>"); return Add(rest[0], rest[1], ReadStdin(), rest[2]);
            case "request": Need(rest, 2, 3, "request <type> <desc> [<dest_keyring>]"); return Request(rest);
            case "search": Need(rest, 3, 4, "search <keyring> <type> <desc> [<dest_keyring>]"); return Search(rest);
            case "id": Need(rest, 1, 1, "id <key>"); Console.WriteLine(Resolve(rest[0]).Serial); return 0;
            case "update": Need(rest, 2, 2, "update <key> <data>"); return Update(rest[0], Encoding.UTF8.GetBytes(rest[1]));
            case "pupdate": Need(rest, 1, 1, "pupdate <key>"); return Update(rest[0], ReadStdin());
            case "print": Need(rest, 1, 1, "print <key>"); return Read(rest[0], "print");
            case "pipe": Need(rest, 1, 1, "pipe <key>"); return Read(rest[0], "pipe");
            case "read": Need(rest, 1, 1, "read <key>"); return Read(rest[0], "read");
            case "describe": Need(rest, 1, 1, "describe <key>"); return Describe(rest[0]);
            case "rdescribe": Need(rest, 1, 2, "rdescribe <key> [sep]"); return RDescribe(rest[0], rest.Length > 1 ? rest[1] : ";");
            case "list": Need(rest, 1, 1, "list <keyring>"); return List(rest[0], raw: false);
            case "rlist": Need(rest, 1, 1, "rlist <keyring>"); return List(rest[0], raw: true);
            case "newring": Need(rest, 2, 2, "newring <name> <keyring>"); return NewRing(rest[0], rest[1]);
            case "new_session": Need(rest, 0, 1, "new_session [<name>]"); return NewSession(rest);
            case "unlink": Need(rest, 1, 2, "unlink <key> [<keyring>]"); return Unlink(rest);
            case "revoke" or "invalidate": Need(rest, 1, 1, $"{cmd} <key>"); Remove(Resolve(rest[0])); return 0;
            case "clear": Need(rest, 1, 1, "clear <keyring>"); return Clear(rest[0]);
            case "purge": return Purge(rest);
            default: throw new UsageException($"unknown command '{cmd}' (see keyctl --help)");
        }
    }

    static void Need(string[] args, int min, int max, string usage)
    {
        if (args.Length < min || args.Length > max) throw new UsageException($"usage: keyctl {usage}");
    }

    static byte[] ReadStdin()
    {
        using var stdin = Console.OpenStandardInput();
        using var buffer = new MemoryStream();
        stdin.CopyTo(buffer);
        return buffer.ToArray();
    }

    static int Supports(string[] args)
    {
        var caps = new (string Name, bool Have)[]
        {
            ("capabilities", true), ("persistent_keyrings", true), ("dh_compute", false), ("public_key", false),
            ("big_key_type", true), ("key_invalidate", true), ("restrict_keyring", false), ("move_key", false),
            ("ns_keyring_name", false), ("ns_keyring_name_pkey", false), ("notify", false),
        };
        if (args.Length == 0)
        {
            foreach (var (name, have) in caps) Console.WriteLine($"have_{name}={(have ? 1 : 0)}");
            return 0;
        }
        if (args[0] == "--raw") { Console.WriteLine("ff"); return 0; }
        var cap = caps.FirstOrDefault(c => c.Name == args[0]);
        if (cap.Name == null) throw new UsageException($"unknown capability '{args[0]}'");
        return cap.Have ? 0 : 1;
    }

    // --- Resolving names and serials --------------------------------------------------------------

    static int Hash(string text)
    {
        // FNV-1a over the lowercased name (credential names are case-insensitive).
        var hash = 2166136261u;
        foreach (var c in text.ToLowerInvariant()) { hash ^= c; hash *= 16777619; }
        var serial = (int)(hash & 0x7FFFFFFF);
        return serial == 0 ? 1 : serial;
    }

    static int RingSerial(string ring) => Hash("keyctl-ring/" + ring);

    static Item BuiltinRing(string name)
    {
        var b = Builtin.First(r => r.Name == name);
        return new Item("", "keyring", b.Name, b.Persist);
    }

    // A keyring argument: @s, a serial, %:name or a plain name made with newring.
    static Item ResolveRing(string spec)
    {
        var item = Resolve(spec);
        if (!item.IsRing) throw new InvalidOperationException("Not a directory");
        return item;
    }

    static Item Resolve(string spec)
    {
        foreach (var b in Builtin)
            if (spec == b.Spec || spec == b.Special.ToString()) return BuiltinRing(b.Name);
        if (spec == "@g") throw new InvalidOperationException("Required key not available");
        var all = AllItems();
        if (spec.StartsWith('%'))
        {
            // %type:desc, or %:ringname for a keyring
            var colon = spec.IndexOf(':');
            if (colon < 0) throw new UsageException($"bad key specifier '{spec}'");
            var type = colon == 1 ? "keyring" : spec[1..colon];
            var desc = spec[(colon + 1)..];
            if (type == "keyring" && Builtin.Any(b => b.Name == desc)) return BuiltinRing(desc);
            return all.FirstOrDefault(i => i.Type == type && i.Desc.Equals(desc, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException("Required key not available");
        }
        if (!int.TryParse(spec, out var serial)) throw new UsageException($"bad key specifier '{spec}'");
        foreach (var b in Builtin)
            if (RingSerial(b.Name) == serial) return BuiltinRing(b.Name);
        return all.FirstOrDefault(i => i.Serial == serial) ?? throw new InvalidOperationException("Required key not available");
    }

    static uint RingPersist(Item ring) => ring.Persist;

    static IEnumerable<Item> Children(Item ring) =>
        AllItems().Where(i => i.Ring.Equals(ring.Desc, StringComparison.OrdinalIgnoreCase));

    // --- Commands -----------------------------------------------------------------------------------

    static int Add(string type, string desc, byte[] data, string ringSpec)
    {
        if (type == "keyring") throw new UsageException("use newring to make a keyring");
        if (type is not ("user" or "logon" or "big_key"))
            throw new InvalidOperationException($"key type '{type}' isn't available (user, logon and big_key are)");
        if (type == "logon" && !desc.Contains(':')) throw new InvalidOperationException("Invalid argument");
        var ring = ResolveRing(ringSpec);
        var item = new Item(ring.Desc, type, desc, RingPersist(ring));
        Write(item, data);
        Console.WriteLine(item.Serial);
        return 0;
    }

    static int Update(string spec, byte[] data)
    {
        var item = Resolve(spec);
        if (item.IsRing) throw new InvalidOperationException("Operation not supported");
        Write(item, data);
        return 0;
    }

    static int Request(string[] args)
    {
        // request_key looks through the thread, process and session rings, then the user ones.
        var found = Find(args[0], args[1], ["thread", "process", "session", "user_session", "user"]);
        if (found == null) throw new InvalidOperationException("Required key not available");
        if (args.Length > 2) LinkInto(found, ResolveRing(args[2]));
        Console.WriteLine(found.Serial);
        return 0;
    }

    static int Search(string[] args)
    {
        var ring = ResolveRing(args[0]);
        var found = Find(args[1], args[2], [ring.Desc]);
        if (found == null) throw new InvalidOperationException("Required key not available");
        if (args.Length > 3) LinkInto(found, ResolveRing(args[3]));
        Console.WriteLine(found.Serial);
        return 0;
    }

    // Searches the given rings and, recursively, the keyrings inside them.
    static Item Find(string type, string desc, string[] rings)
    {
        var all = AllItems();
        var queue = new Queue<string>(rings);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (queue.Count > 0)
        {
            var ring = queue.Dequeue();
            if (!seen.Add(ring)) continue;
            foreach (var item in all.Where(i => i.Ring.Equals(ring, StringComparison.OrdinalIgnoreCase)))
            {
                if (item.Type == type && item.Desc.Equals(desc, StringComparison.OrdinalIgnoreCase)) return item;
                if (item.IsRing) queue.Enqueue(item.Desc);
            }
        }
        return null;
    }

    // Credential Manager can't share one credential between rings, so "linking" copies the key.
    static void LinkInto(Item item, Item ring)
    {
        if (item.IsRing || item.Ring.Equals(ring.Desc, StringComparison.OrdinalIgnoreCase)) return;
        Write(item with { Ring = ring.Desc, Persist = ring.Persist }, ReadBlob(item));
    }

    static int Read(string spec, string mode)
    {
        var item = Resolve(spec);
        if (item.IsRing)
        {
            // Reading a keyring gives its contents' serials, like Linux.
            var serials = Children(item).Select(i => i.Serial).ToList();
            if (mode == "pipe")
            {
                var bytes = serials.SelectMany(BitConverter.GetBytes).ToArray();
                using var raw = Console.OpenStandardOutput();
                raw.Write(bytes);
            }
            else Console.WriteLine(serials.Count == 0 ? "keyring is empty" : string.Join(' ', serials));
            return 0;
        }
        if (item.Type == "logon") throw new InvalidOperationException("Permission denied");
        var data = ReadBlob(item);
        switch (mode)
        {
            case "pipe":
                using (var raw = Console.OpenStandardOutput()) raw.Write(data);
                break;
            case "read":
                Console.WriteLine($"{data.Length} bytes of data in key:");
                var line = new StringBuilder();
                for (var i = 0; i < data.Length; i++)
                {
                    if (i > 0 && i % 4 == 0) line.Append(i % 32 == 0 ? '\n' : ' ');
                    line.Append(data[i].ToString("x2"));
                }
                if (line.Length > 0) Console.WriteLine(line);
                break;
            default:
                // print: text if it's all printable, otherwise ":hex:<bytes>"
                var printable = data.All(b => b is >= 0x20 and < 0x7F or (byte)'\n' or (byte)'\t' or >= 0x80);
                Console.WriteLine(printable ? Encoding.UTF8.GetString(data) : ":hex:" + Convert.ToHexStringLower(data));
                break;
        }
        return 0;
    }

    static string Perm(Item item) => item.IsRing ? "3f3f0000" : "3f010000";

    static string Description(Item item) => item.IsRing && item.Ring == "" ? Ses(item.Desc) : item.Desc;

    // The built-in rings show Linux's internal names.
    static string Ses(string ring) => ring switch
    {
        "session" => "_ses",
        "process" => "_pid",
        "thread" => "_tid",
        "user" => "_uid." + Environment.UserName,
        "user_session" => "_uid_ses." + Environment.UserName,
        _ => ring,
    };

    static int Describe(string spec)
    {
        var item = Resolve(spec);
        Console.WriteLine($"{item.Serial,9}: alswrv-----v------------    -1    -1 {item.Type}: {Description(item)}");
        return 0;
    }

    static int RDescribe(string spec, string sep)
    {
        var item = Resolve(spec);
        Console.WriteLine(string.Join(sep, item.Type, "-1", "-1", Perm(item), Description(item)));
        return 0;
    }

    static int List(string spec, bool raw)
    {
        var ring = ResolveRing(spec);
        var items = Children(ring).ToList();
        if (raw)
        {
            Console.WriteLine(string.Join(' ', items.Select(i => i.Serial)));
            return 0;
        }
        if (items.Count == 0) { Console.WriteLine("keyring is empty"); return 0; }
        Console.WriteLine(items.Count == 1 ? "1 key in keyring:" : $"{items.Count} keys in keyring:");
        foreach (var item in items)
            Console.WriteLine($"{item.Serial,9}: --alswrv    -1    -1 {item.Type}: {item.Desc}");
        return 0;
    }

    static int Show(string[] args)
    {
        var ring = ResolveRing(args.Length > 0 ? args[0] : "@s");
        var title = ring.Ring == "" ? Builtin.First(b => b.Name == ring.Desc).Title : "Keyring";
        Console.WriteLine(title);
        var all = AllItems();
        void Tree(Item node, int depth, HashSet<string> seen)
        {
            var indent = depth == 0 ? " " : new string(' ', depth * 3 - 1) + "\\_ ";
            Console.WriteLine($"{node.Serial,10} --alswrv    -1    -1 {indent}{node.Type}: {Description(node)}");
            if (!node.IsRing || !seen.Add(node.Desc)) return;
            foreach (var child in all.Where(i => i.Ring.Equals(node.Desc, StringComparison.OrdinalIgnoreCase)))
                Tree(child, depth + 1, seen);
        }
        Tree(ring, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        return 0;
    }

    static int NewRing(string name, string parentSpec)
    {
        if (Builtin.Any(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) || name.Contains('/'))
            throw new InvalidOperationException("Invalid argument");
        var parent = ResolveRing(parentSpec);
        var ring = new Item(parent.Desc, "keyring", name, parent.Persist);
        Write(ring, []);
        Console.WriteLine(ring.Serial);
        return 0;
    }

    // Windows can't give the calling shell a fresh session keyring, so this hands back the logon
    // session's ring (keys in it disappear at logoff, like Linux session keys).
    static int NewSession(string[] args)
    {
        if (args.Length == 1)
        {
            var ring = new Item("session", "keyring", args[0], CRED_PERSIST_SESSION);
            Write(ring, []);
            Console.WriteLine(ring.Serial);
        }
        else Console.WriteLine(RingSerial("session"));
        return 0;
    }

    static int Unlink(string[] args)
    {
        var item = Resolve(args[0]);
        if (args.Length == 2)
        {
            var ring = ResolveRing(args[1]);
            if (!item.Ring.Equals(ring.Desc, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("No such file or directory");
            Remove(item);
            return 0;
        }
        if (item.Ring == "") throw new InvalidOperationException("Operation not permitted");
        Remove(item);
        Console.WriteLine("1 links removed");
        return 0;
    }

    static int Clear(string spec)
    {
        var ring = ResolveRing(spec);
        foreach (var item in Children(ring).ToList()) Remove(item);
        return 0;
    }

    static int Purge(string[] args)
    {
        var search = false;
        var exact = true;
        var list = args.ToList();
        while (list.Count > 0 && list[0].StartsWith('-'))
        {
            switch (list[0])
            {
                case "-s": search = true; break;
                case "-i": exact = false; break;
                case "-p": exact = false; break;
                default: throw new UsageException($"unknown purge option {list[0]}");
            }
            list.RemoveAt(0);
        }
        if (list.Count is 0 or > 2 || (search && list.Count != 2))
            throw new UsageException("usage: keyctl purge <type> | purge [-p] [-i] <type> <desc> | purge -s <type> <desc>");
        var type = list[0];
        var desc = list.Count > 1 ? list[1] : null;
        var matches = AllItems().Where(i => i.Type == type && (desc == null || (exact
            ? i.Desc.Equals(desc, StringComparison.OrdinalIgnoreCase)
            : i.Desc.StartsWith(desc, StringComparison.OrdinalIgnoreCase)))).ToList();
        foreach (var item in matches) Remove(item);
        Console.WriteLine($"purged {matches.Count} keys");
        return 0;
    }

    // --- Credential Manager -------------------------------------------------------------------------

    static List<Item> AllItems()
    {
        var items = new List<Item>();
        if (!CredEnumerateW(Prefix + "*", 0, out var count, out var list))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == ERROR_NOT_FOUND) return items;
            throw new System.ComponentModel.Win32Exception(error);
        }
        try
        {
            for (var n = 0; n < count; n++)
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(Marshal.ReadIntPtr(list, n * IntPtr.Size));
                if (cred.Type != CRED_TYPE_GENERIC) continue;
                var target = Marshal.PtrToStringUni(cred.TargetName) ?? "";
                var parts = target[Prefix.Length..].Split('/', 3);
                if (parts.Length == 3) items.Add(new Item(parts[0], parts[1], parts[2], cred.Persist));
            }
        }
        finally { CredFree(list); }
        return items;
    }

    static byte[] ReadBlob(Item item)
    {
        if (!CredReadW(item.Target, CRED_TYPE_GENERIC, 0, out var ptr))
            throw new InvalidOperationException("Required key not available");
        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
            var data = new byte[cred.CredentialBlobSize];
            if (data.Length > 0) Marshal.Copy(cred.CredentialBlob, data, 0, data.Length);
            return data;
        }
        finally { CredFree(ptr); }
    }

    static unsafe void Write(Item item, byte[] data)
    {
        if (data.Length > CRED_MAX_CREDENTIAL_BLOB_SIZE)
            throw new InvalidOperationException($"key data is {data.Length} bytes; Windows Credential Manager holds at most {CRED_MAX_CREDENTIAL_BLOB_SIZE}");
        var target = Marshal.StringToHGlobalUni(item.Target);
        var user = Marshal.StringToHGlobalUni(Environment.UserName);
        try
        {
            fixed (byte* blob = data)
            {
                var cred = new CREDENTIAL
                {
                    Type = CRED_TYPE_GENERIC,
                    TargetName = target,
                    CredentialBlobSize = (uint)data.Length,
                    CredentialBlob = (IntPtr)blob,
                    Persist = item.Persist,
                    UserName = user,
                };
                if (!CredWriteW(ref cred, 0)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Marshal.FreeHGlobal(target);
            Marshal.FreeHGlobal(user);
        }
    }

    // Removing a keyring removes everything in it too.
    static void Remove(Item item)
    {
        if (item.Ring == "")
        {
            foreach (var child in Children(item).ToList()) Remove(child);
            return;
        }
        if (item.IsRing)
            foreach (var child in Children(item).ToList()) Remove(child);
        if (!CredDeleteW(item.Target, CRED_TYPE_GENERIC, 0) && Marshal.GetLastWin32Error() != ERROR_NOT_FOUND)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    const uint CRED_TYPE_GENERIC = 1;
    const uint CRED_PERSIST_SESSION = 1, CRED_PERSIST_LOCAL_MACHINE = 2;
    const int CRED_MAX_CREDENTIAL_BLOB_SIZE = 5 * 512;
    const int ERROR_NOT_FOUND = 1168;

    [StructLayout(LayoutKind.Sequential)]
    struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredWriteW(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredReadW(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredEnumerateW(string filter, uint flags, out int count, out IntPtr credentials);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredDeleteW(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    static extern void CredFree(IntPtr buffer);
}
