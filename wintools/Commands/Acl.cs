using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace WinTools;

// getfacl / setfacl / chacl (acl): NTFS DACLs shown and edited as POSIX ACLs. The file's owner is
// user::, its primary group group::, Everyone other::, every other account a named user:/group:
// entry; inheritable ACEs on folders are the default: ACL (CREATOR OWNER/GROUP for its user::/group::).
// r/w/x map to read-data, write-data and execute rights. Changing an ACL writes it as explicit
// entries (POSIX ACLs have no inheritance from the parent folder).
static class Acl
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("getfacl", "Show file access control lists (NTFS ACLs, POSIX style)", GetFacl);
        Commands.Add("setfacl", "Set file access control lists (NTFS ACLs, POSIX style)", SetFacl);
        Commands.Add("chacl", "Change the access control list of a file or directory", Chacl);
    }

    // One ACL entry: tag is "user", "group", "mask" or "other"; Sid is null for the base entries.
    internal record struct Entry(string Tag, SecurityIdentifier Sid, int Perms);

    sealed class PosixAcl
    {
        public SecurityIdentifier Owner, Group;
        public List<Entry> Access = [], Default = [];
        public bool IsDirectory;
    }

    static readonly SecurityIdentifier Everyone = new(WellKnownSidType.WorldSid, null);
    static readonly SecurityIdentifier CreatorOwner = new(WellKnownSidType.CreatorOwnerSid, null);
    static readonly SecurityIdentifier CreatorGroup = new(WellKnownSidType.CreatorGroupSid, null);

    const int R = 4, W = 2, X = 1;

    // ---- reading and writing ----

    static PosixAcl Read(string path)
    {
        var isDir = Directory.Exists(path);
        if (!isDir && !File.Exists(path)) throw new IOException($"{path}: No such file or directory");
        FileSystemSecurity sec = isDir ? new DirectoryInfo(path).GetAccessControl() : new FileInfo(path).GetAccessControl();
        var acl = new PosixAcl
        {
            IsDirectory = isDir,
            Owner = sec.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier,
            Group = sec.GetGroup(typeof(SecurityIdentifier)) as SecurityIdentifier,
        };
        var access = new Dictionary<SecurityIdentifier, (int Allow, int Deny)>();
        var defaults = new Dictionary<SecurityIdentifier, (int Allow, int Deny)>();
        var order = new List<SecurityIdentifier>();
        var defaultOrder = new List<SecurityIdentifier>();
        foreach (FileSystemAccessRule rule in sec.GetAccessRules(true, true, typeof(SecurityIdentifier)))
        {
            var sid = (SecurityIdentifier)rule.IdentityReference;
            var perms = Perms(rule.FileSystemRights);
            var allow = rule.AccessControlType == AccessControlType.Allow;
            if (!rule.PropagationFlags.HasFlag(PropagationFlags.InheritOnly)) Accumulate(access, order, sid, perms, allow);
            if (isDir && rule.InheritanceFlags != InheritanceFlags.None) Accumulate(defaults, defaultOrder, sid, perms, allow);
        }
        acl.Access = Entries(access, order, acl.Owner, acl.Group);
        if (defaults.Count > 0) acl.Default = Entries(defaults, defaultOrder, CreatorOwner, CreatorGroup);
        return acl;

        static void Accumulate(Dictionary<SecurityIdentifier, (int, int)> map, List<SecurityIdentifier> order, SecurityIdentifier sid, int perms, bool allow)
        {
            if (!map.TryGetValue(sid, out var v)) { v = (0, 0); order.Add(sid); }
            map[sid] = allow ? (v.Item1 | perms, v.Item2) : (v.Item1, v.Item2 | perms);
        }
    }

    // Base entries always exist (--- when the DACL says nothing); named entries follow in ACL order.
    static List<Entry> Entries(Dictionary<SecurityIdentifier, (int Allow, int Deny)> map, List<SecurityIdentifier> order, SecurityIdentifier owner, SecurityIdentifier group)
    {
        int Effective(SecurityIdentifier sid) => map.TryGetValue(sid, out var v) ? v.Allow & ~v.Deny : 0;
        var list = new List<Entry> { new("user", null, Effective(owner)) };
        foreach (var sid in order.Where(s => s != owner && s != group && s != Everyone && !IsGroup(s))) list.Add(new("user", sid, Effective(sid)));
        list.Add(new("group", null, Effective(group)));
        var namedGroups = order.Where(s => s != owner && s != group && s != Everyone && IsGroup(s)).ToList();
        foreach (var sid in namedGroups) list.Add(new("group", sid, Effective(sid)));
        if (list.Any(e => e.Sid != null)) list.Add(new("mask", null, list.Where(e => e.Tag == "group" || e.Sid != null).Aggregate(0, (m, e) => m | e.Perms)));
        list.Add(new("other", null, Effective(Everyone)));
        return list;
    }

    static void Write(string path, PosixAcl acl)
    {
        FileSystemSecurity sec = acl.IsDirectory ? new DirectorySecurity() : new FileSecurity();
        sec.SetAccessRuleProtection(true, false);
        var mask = acl.Access.FirstOrDefault(e => e.Tag == "mask").Tag != null ? acl.Access.First(e => e.Tag == "mask").Perms : 7;
        foreach (var e in acl.Access)
        {
            if (e.Tag == "mask") continue;
            // Accounts whose primary group is themselves: the owner's entry decides, group:: just mirrors it.
            if (e.Tag == "group" && e.Sid == null && acl.Group == acl.Owner) continue;
            var sid = e.Sid ?? (e.Tag == "user" ? acl.Owner : e.Tag == "group" ? acl.Group : Everyone);
            var perms = e.Sid != null || e.Tag == "group" ? e.Perms & mask : e.Perms;
            if (perms != 0 && sid != null) sec.AddAccessRule(new FileSystemAccessRule(sid, Rights(perms, acl.IsDirectory), AccessControlType.Allow));
        }
        if (acl.IsDirectory)
        {
            var dmask = acl.Default.FirstOrDefault(e => e.Tag == "mask").Tag != null ? acl.Default.First(e => e.Tag == "mask").Perms : 7;
            foreach (var e in acl.Default)
            {
                if (e.Tag == "mask") continue;
                var sid = e.Sid ?? (e.Tag == "user" ? CreatorOwner : e.Tag == "group" ? CreatorGroup : Everyone);
                var perms = e.Sid != null || e.Tag == "group" ? e.Perms & dmask : e.Perms;
                if (perms != 0)
                    sec.AddAccessRule(new FileSystemAccessRule(sid, Rights(perms, true), InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.InheritOnly, AccessControlType.Allow));
            }
        }
        if (acl.IsDirectory) new DirectoryInfo(path).SetAccessControl((DirectorySecurity)sec);
        else new FileInfo(path).SetAccessControl((FileSecurity)sec);
    }

    static int Perms(FileSystemRights rights)
    {
        var r = (int)rights;
        var p = 0;
        if ((r & (int)FileSystemRights.ReadData) != 0 || (r & unchecked((int)0x80000000)) != 0 || (r & 0x10000000) != 0) p |= R;
        if ((r & (int)FileSystemRights.WriteData) != 0 || (r & 0x40000000) != 0 || (r & 0x10000000) != 0) p |= W;
        if ((r & (int)FileSystemRights.ExecuteFile) != 0 || (r & 0x20000000) != 0 || (r & 0x10000000) != 0) p |= X;
        return p;
    }

    static FileSystemRights Rights(int perms, bool directory)
    {
        if (perms == 7) return FileSystemRights.FullControl & ~(FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership)
            | FileSystemRights.ReadPermissions;
        FileSystemRights rights = FileSystemRights.Synchronize | FileSystemRights.ReadAttributes | FileSystemRights.ReadPermissions;
        if ((perms & R) != 0) rights |= FileSystemRights.ReadData | FileSystemRights.ReadExtendedAttributes;
        if ((perms & W) != 0)
        {
            rights |= FileSystemRights.WriteData | FileSystemRights.AppendData | FileSystemRights.WriteAttributes | FileSystemRights.WriteExtendedAttributes;
            if (directory) rights |= FileSystemRights.DeleteSubdirectoriesAndFiles;
        }
        if ((perms & X) != 0) rights |= FileSystemRights.ExecuteFile;
        return rights;
    }

    // ---- names ----

    static bool IsGroup(SecurityIdentifier sid) => Lookup(sid).Type is not (1 or 0);

    static readonly Dictionary<SecurityIdentifier, (string Name, int Type)> lookups = [];

    static unsafe (string Name, int Type) Lookup(SecurityIdentifier sid)
    {
        if (lookups.TryGetValue(sid, out var cached)) return cached;
        var binary = new byte[sid.BinaryLength];
        sid.GetBinaryForm(binary, 0);
        var name = stackalloc char[256];
        var domain = stackalloc char[256];
        uint nameLen = 256, domainLen = 256;
        (string, int) result = (null, 0);
        if (LookupAccountSidW(null, binary, name, ref nameLen, domain, ref domainLen, out var use))
        {
            var n = new string(name, 0, (int)nameLen);
            var d = new string(domain, 0, (int)domainLen);
            // Short names for built-in and local accounts; DOMAIN\name for the rest.
            var local = d.Length == 0 || d is "BUILTIN" or "NT AUTHORITY" or "NT SERVICE" or "APPLICATION PACKAGE AUTHORITY"
                || d.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                || d.Equals(Environment.UserDomainName, StringComparison.OrdinalIgnoreCase);
            result = (local ? n : $"{d}\\{n}", use);
        }
        lookups[sid] = result;
        return result;
    }

    static string Name(SecurityIdentifier sid, bool numeric)
    {
        if (sid == null) return "";
        var name = numeric ? null : Lookup(sid).Name;
        return (name ?? sid.Value).Replace(" ", "\\040");
    }

    static SecurityIdentifier Resolve(string name)
    {
        name = name.Replace("\\040", " ");
        if (name.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase)) return new SecurityIdentifier(name);
        try { return (SecurityIdentifier)new NTAccount(name).Translate(typeof(SecurityIdentifier)); }
        catch (IdentityNotMappedException) { throw new InvalidOperationException($"invalid user or group name '{name}'"); }
    }

    // ---- text forms ----

    static string PermString(int p) => $"{((p & R) != 0 ? 'r' : '-')}{((p & W) != 0 ? 'w' : '-')}{((p & X) != 0 ? 'x' : '-')}";

    static int ParsePerms(string s, bool directory)
    {
        if (s.Length == 1 && s[0] is >= '0' and <= '7') return s[0] - '0';
        var p = 0;
        foreach (var c in s)
            p |= c switch { 'r' => R, 'w' => W, 'x' => X, 'X' => directory ? X : 0, '-' => 0, _ => throw new UsageException($"invalid permissions '{s}'") };
        return p;
    }

    // "[d[efault]:][u[ser]:]name[:perms]" entries separated by commas or newlines.
    internal static List<(bool Default, string Tag, string Name, string Perms)> ParseSpec(string spec, bool needPerms)
    {
        var list = new List<(bool, string, string, string)>();
        foreach (var raw in spec.Split([',', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var text = raw.Trim();
            var hash = text.IndexOf('#');
            if (hash >= 0) text = text[..hash].Trim();
            if (text.Length == 0) continue;
            var parts = text.Split(':').ToList();
            var isDefault = false;
            if (parts[0] is "d" or "default") { isDefault = true; parts.RemoveAt(0); }
            if (parts.Count == 0) throw new UsageException($"malformed ACL entry '{raw}'");
            var tag = parts[0] switch
            {
                "u" or "user" => "user",
                "g" or "group" => "group",
                "m" or "mask" => "mask",
                "o" or "other" => "other",
                _ => null,
            };
            if (tag == null) { tag = "user"; parts.Insert(0, "user"); }
            var name = parts.Count > 1 ? parts[1] : "";
            var perms = parts.Count > 2 ? parts[2] : null;
            // "o::r" and "m::r" may also be written "o:r".
            if (tag is "other" or "mask" && parts.Count == 2) { perms = parts[1]; name = ""; }
            if (needPerms && perms == null) throw new UsageException($"missing permissions in ACL entry '{raw}'");
            list.Add((isDefault, tag, name, perms));
        }
        return list;
    }

    static void Apply(PosixAcl acl, List<(bool Default, string Tag, string Name, string Perms)> spec, bool remove)
    {
        foreach (var (isDefault, tag, name, perms) in spec)
        {
            if (isDefault && !acl.IsDirectory) throw new InvalidOperationException("only directories can have default ACLs");
            var list = isDefault ? acl.Default : acl.Access;
            if (isDefault && list.Count == 0)
            {
                // A new default ACL starts as a copy of the access ACL's base entries, as on Linux.
                list.AddRange(acl.Access.Where(e => e.Sid == null && e.Tag != "mask"));
            }
            var sid = name.Length > 0 && tag is "user" or "group" ? Resolve(name) : null;
            var index = list.FindIndex(e => e.Tag == tag && (sid == null ? e.Sid == null : sid.Equals(e.Sid)));
            if (remove)
            {
                if (sid == null && tag != "mask") throw new UsageException("base ACL entries (user::, group::, other::) cannot be removed");
                if (index >= 0) list.RemoveAt(index);
                continue;
            }
            var entry = new Entry(tag, sid, ParsePerms(perms ?? "", acl.IsDirectory));
            if (index >= 0) list[index] = entry;
            else
            {
                // Keep the conventional order: user::, user:x, group::, group:x, mask::, other::.
                var rank = Rank(entry);
                var at = list.FindIndex(e => Rank(e) > rank);
                list.Insert(at < 0 ? list.Count : at, entry);
            }
        }
        RecalculateMask(acl.Access);
        RecalculateMask(acl.Default);
    }

    static int Rank(Entry e) => e.Tag switch { "user" => e.Sid == null ? 0 : 1, "group" => e.Sid == null ? 2 : 3, "mask" => 4, _ => 5 };

    static bool explicitMask;

    // The mask is the union of the group-class entries unless one was given explicitly (setfacl -n
    // keeps it as is).
    static void RecalculateMask(List<Entry> list)
    {
        if (list.Count == 0) return;
        var named = list.Any(e => e.Sid != null);
        var maskIndex = list.FindIndex(e => e.Tag == "mask");
        if (!named) { if (maskIndex >= 0 && !explicitMask) list.RemoveAt(maskIndex); return; }
        var union = list.Where(e => e.Tag == "group" || e.Sid != null).Aggregate(0, (m, e) => m | e.Perms);
        if (maskIndex < 0) list.Insert(list.FindIndex(e => e.Tag == "other") is var o and >= 0 ? o : list.Count, new Entry("mask", null, union));
        else if (!explicitMask) list[maskIndex] = list[maskIndex] with { Perms = union };
    }

    static string Format(PosixAcl acl, string path, bool header, bool access, bool defaults, bool numeric, bool effective)
    {
        var sb = new StringBuilder();
        if (header)
        {
            sb.Append($"# file: {path}\n");
            sb.Append($"# owner: {Name(acl.Owner, numeric)}\n");
            sb.Append($"# group: {Name(acl.Group, numeric)}\n");
        }
        void Lines(List<Entry> list, string prefix)
        {
            var mask = list.FirstOrDefault(e => e.Tag == "mask");
            foreach (var e in list)
            {
                var line = $"{prefix}{e.Tag}:{Name(e.Sid, numeric)}:{PermString(e.Perms)}";
                if (effective && mask.Tag != null && (e.Sid != null || e.Tag == "group") && (e.Perms & ~mask.Perms) != 0)
                    line += $"\t#effective:{PermString(e.Perms & mask.Perms)}";
                sb.Append(line).Append('\n');
            }
        }
        if (access) Lines(acl.Access, "");
        if (defaults && acl.IsDirectory) Lines(acl.Default, "default:");
        sb.Append('\n');
        return sb.ToString();
    }

    static IEnumerable<string> Walk(string path, bool recursive)
    {
        yield return path;
        if (!recursive || !Directory.Exists(path)) yield break;
        foreach (var child in Directory.EnumerateFileSystemEntries(path, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 }))
            yield return child;
    }

    // ---- getfacl ----

    static int GetFacl(string[] args)
    {
        bool header = true, access = true, defaults = true, numeric = false, recursive = false, skipBase = false, effective = true;
        var paths = new List<string>();
        var options = true;
        foreach (var arg in args)
        {
            if (options && arg == "--") { options = false; continue; }
            if (!options || !arg.StartsWith('-') || arg == "-") { paths.Add(arg); continue; }
            var flags = arg.StartsWith("--") ? [arg] : arg[1..].Select(c => "-" + c).ToArray();
            foreach (var flag in flags)
            {
                switch (flag)
                {
                    case "-h" or "--help":
                        Console.WriteLine("""
                            Usage: getfacl [-aceEsRLPnpdh] file ...
                              -a,  --access           display the file access control list only
                              -d,  --default          display the default access control list only
                              -c,  --omit-header      do not display the comment header
                              -e,  --all-effective    print all effective rights
                              -E,  --no-effective     print no effective rights
                              -s,  --skip-base        skip files that only have the base entries
                              -R,  --recursive        recurse into subdirectories
                              -L,  --logical          logical walk, follow symbolic links
                              -P,  --physical         physical walk, do not follow symbolic links
                              -n,  --numeric          print numeric user/group identifiers (SIDs)
                              -p,  --absolute-names   don't strip leading '/' in pathnames
                              -v,  --version          print version and exit
                              -h,  --help             this help text
                            """);
                        return 0;
                    case "-v" or "--version": Console.WriteLine($"getfacl {Commands.Version}"); return 0;
                    case "-a" or "--access": access = true; defaults = false; break;
                    case "-d" or "--default": defaults = true; access = false; break;
                    case "-c" or "--omit-header": header = false; break;
                    case "-e" or "--all-effective": effective = true; break;
                    case "-E" or "--no-effective": effective = false; break;
                    case "-s" or "--skip-base": skipBase = true; break;
                    case "-R" or "--recursive": recursive = true; break;
                    case "-n" or "--numeric": numeric = true; break;
                    case "-L" or "--logical" or "-P" or "--physical" or "-p" or "--absolute-names": break;
                    default: throw new UsageException($"invalid option -- '{flag.TrimStart('-')}'");
                }
            }
        }
        if (paths.Count == 0) throw new UsageException("Usage: getfacl [-aceEsRLPnpdh] file ...");
        var status = 0;
        using var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
        foreach (var path in paths.SelectMany(p => Walk(p, recursive)))
        {
            try
            {
                var acl = Read(path);
                if (skipBase && acl.Access.All(e => e.Sid == null && e.Tag != "mask") && acl.Default.Count == 0) continue;
                stdout.Write(Format(acl, path, header, access, defaults, numeric, effective));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                stdout.Flush();
                Console.Error.WriteLine($"getfacl: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    // ---- setfacl ----

    static int SetFacl(string[] args)
    {
        var ops = new List<(string Op, string Value)>();
        bool recursive = false, test = false, defaultFlag = false;
        string restore = null;
        var paths = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value(string opt)
            {
                var eq = arg.IndexOf('=');
                if (arg.StartsWith("--") && eq > 0) return arg[(eq + 1)..];
                if (!arg.StartsWith("--") && arg.Length > 2) return arg[2..];
                if (++i >= args.Length) throw new UsageException($"option requires an argument -- '{opt}'");
                return args[i];
            }
            var name = arg.StartsWith("--") ? arg.Split('=')[0] : arg.Length >= 2 ? arg[..2] : arg;
            switch (name)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: setfacl [-bkndRLP] { -m|-M|-x|-X ... } file ...
                          -m, --modify=acl        modify the current ACL(s) of file(s)
                          -M, --modify-file=file  read ACL entries to modify from file
                          -x, --remove=acl        remove entries from the ACL(s) of file(s)
                          -X, --remove-file=file  read ACL entries to remove from file
                          -b, --remove-all        remove all extended ACL entries
                          -k, --remove-default    remove the default ACL
                              --set=acl           set the ACL of file(s), replacing the current ACL
                              --set-file=file     read ACL entries to set from file
                              --mask              do recalculate the effective rights mask
                          -n, --no-mask           don't recalculate the effective rights mask
                          -d, --default           operations apply to the default ACL
                          -R, --recursive         recurse into subdirectories
                          -L, --logical           logical walk, follow symbolic links
                          -P, --physical          physical walk, do not follow symbolic links
                              --restore=file      restore ACLs (inverse of `getfacl -R')
                              --test              test mode (ACLs are not modified)
                          -v, --version           print version and exit
                          -h, --help              this help text
                        On Windows the ACL is written as explicit NTFS entries (no inheritance from the parent).
                        """);
                    return 0;
                case "-v" or "--version": Console.WriteLine($"setfacl {Commands.Version}"); return 0;
                case "-m" or "--modify": ops.Add(("m", Value("m"))); break;
                case "-M" or "--modify-file": ops.Add(("m", ReadSpec(Value("M")))); break;
                case "-x" or "--remove": ops.Add(("x", Value("x"))); break;
                case "-X" or "--remove-file": ops.Add(("x", ReadSpec(Value("X")))); break;
                case "-b" or "--remove-all": ops.Add(("b", null)); break;
                case "-k" or "--remove-default": ops.Add(("k", null)); break;
                case "--set": ops.Add(("s", Value("set"))); break;
                case "--set-file": ops.Add(("s", ReadSpec(Value("set-file")))); break;
                case "-n" or "--no-mask": explicitMask = true; break;
                case "--mask": explicitMask = false; break;
                case "-d" or "--default": defaultFlag = true; break;
                case "-R" or "--recursive": recursive = true; break;
                case "-L" or "--logical" or "-P" or "--physical": break;
                case "--restore": restore = Value("restore"); break;
                case "--test": test = true; break;
                default:
                    if (arg.StartsWith('-') && arg != "-") throw new UsageException($"invalid option -- '{arg.TrimStart('-')}'");
                    paths.Add(arg);
                    break;
            }
        }
        if (restore != null) return Restore(restore, test);
        if (ops.Count == 0 || paths.Count == 0) throw new UsageException("Usage: setfacl [-bkndRLP] { -m|-M|-x|-X ... } file ...");
        var status = 0;
        foreach (var path in paths.SelectMany(p => Walk(p, recursive)))
        {
            try
            {
                var acl = Read(path);
                foreach (var (op, value) in ops)
                {
                    switch (op)
                    {
                        case "b":
                            acl.Access.RemoveAll(e => e.Sid != null || e.Tag == "mask");
                            acl.Default.Clear();
                            break;
                        case "k": acl.Default.Clear(); break;
                        case "s":
                            var spec = Defaulted(ParseSpec(value, true), defaultFlag);
                            if (spec.Any(s => !s.Default)) acl.Access.Clear();
                            if (spec.Any(s => s.Default)) acl.Default.Clear();
                            // --set needs complete base entries; missing ones would leave no access at all.
                            foreach (var kind in new[] { false, true }.Where(d => spec.Any(s => s.Default == d)))
                                foreach (var tag in new[] { "user", "group", "other" })
                                    if (!spec.Any(s => s.Default == kind && s.Tag == tag && s.Name.Length == 0))
                                        throw new UsageException($"--set needs a {(kind ? "default:" : "")}{tag}:: entry");
                            Apply(acl, spec, remove: false);
                            break;
                        default:
                            Apply(acl, Defaulted(ParseSpec(value, op == "m"), defaultFlag), remove: op == "x");
                            break;
                    }
                }
                if (test) Console.Write($"{path}: {string.Join(',', acl.Access.Select(Short))}" + (acl.Default.Count > 0 ? "," + string.Join(',', acl.Default.Select(e => "d:" + Short(e))) : "") + "\n");
                else Write(path, acl);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Console.Error.WriteLine($"setfacl: {path}: {e.Message}");
                status = 1;
            }
        }
        return status;

        static string Short(Entry e) => $"{e.Tag[0]}:{Name(e.Sid, false)}:{PermString(e.Perms)}";
    }

    static List<(bool Default, string Tag, string Name, string Perms)> Defaulted(List<(bool Default, string Tag, string Name, string Perms)> spec, bool all) =>
        all ? spec.Select(s => (true, s.Tag, s.Name, s.Perms)).ToList() : spec;

    static string ReadSpec(string file) => file == "-" ? Console.In.ReadToEnd() : File.ReadAllText(file);

    // --restore: getfacl output (with "# file:" headers) applied file by file.
    static int Restore(string file, bool test)
    {
        var status = 0;
        var text = file == "-" ? Console.In.ReadToEnd() : File.ReadAllText(file);
        foreach (var block in text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var lines = block.Split('\n');
            var path = lines.FirstOrDefault(l => l.StartsWith("# file: "))?[8..];
            if (path == null) continue;
            try
            {
                var acl = Read(path);
                var spec = ParseSpec(string.Join('\n', lines.Where(l => !l.StartsWith('#'))), true);
                acl.Access.Clear();
                acl.Default.Clear();
                Apply(acl, spec, remove: false);
                if (!test) Write(path, acl);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Console.Error.WriteLine($"setfacl: {path}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    // ---- chacl ----

    static int Chacl(string[] args)
    {
        if (args.Length == 0) throw new UsageException(ChaclUsage);
        bool recursive = false;
        string mode = null;
        var rest = new List<string>();
        foreach (var arg in args)
        {
            if (arg is "-h" or "--help") { Console.WriteLine(ChaclUsage); return 0; }
            if (arg is "-v" or "--version") { Console.WriteLine($"chacl {Commands.Version}"); return 0; }
            if (arg == "-r") { recursive = true; continue; }
            if (arg is "-b" or "-d" or "-R" or "-D" or "-B" or "-l") { mode = arg; continue; }
            if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"invalid option {arg}\n{ChaclUsage}");
            rest.Add(arg);
        }
        string access = null, defaults = null;
        switch (mode)
        {
            case null: if (rest.Count < 2) throw new UsageException(ChaclUsage); access = rest[0]; rest.RemoveAt(0); break;
            case "-b": if (rest.Count < 3) throw new UsageException(ChaclUsage); access = rest[0]; defaults = rest[1]; rest.RemoveRange(0, 2); break;
            case "-d": if (rest.Count < 2) throw new UsageException(ChaclUsage); defaults = rest[0]; rest.RemoveAt(0); break;
        }
        if (rest.Count == 0) throw new UsageException(ChaclUsage);
        var status = 0;
        foreach (var path in rest.SelectMany(p => Walk(p, recursive)))
        {
            try
            {
                var acl = Read(path);
                switch (mode)
                {
                    case "-l":
                        var text = string.Join(',', acl.Access.Select(ChaclEntry));
                        if (acl.Default.Count > 0) text += "/" + string.Join(',', acl.Default.Select(ChaclEntry));
                        Console.WriteLine($"{path} [{text}]");
                        continue;
                    case "-R": acl.Access.RemoveAll(e => e.Sid != null || e.Tag == "mask"); break;
                    case "-D": acl.Default.Clear(); break;
                    case "-B": acl.Access.RemoveAll(e => e.Sid != null || e.Tag == "mask"); acl.Default.Clear(); break;
                    default:
                        if (access != null)
                        {
                            acl.Access.Clear();
                            Apply(acl, ParseSpec(access, true), remove: false);
                            if (acl.Access.Count(e => e.Sid == null && e.Tag != "mask") < 3) throw new InvalidOperationException("an ACL needs u::, g:: and o:: entries");
                        }
                        if (defaults != null)
                        {
                            if (!acl.IsDirectory) throw new InvalidOperationException("only directories can have default ACLs");
                            acl.Default.Clear();
                            Apply(acl, ParseSpec(defaults, true).Select(s => (true, s.Tag, s.Name, s.Perms)).ToList(), remove: false);
                        }
                        break;
                }
                Write(path, acl);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Console.Error.WriteLine($"chacl: {path}: {e.Message}");
                status = 1;
            }
        }
        return status;

        static string ChaclEntry(Entry e) => $"{e.Tag[0]}:{Name(e.Sid, false)}:{PermString(e.Perms)}";
    }

    const string ChaclUsage = """
        Usage:
        	chacl acl pathname...
        	chacl -b acl dacl pathname...
        	chacl -d dacl pathname...
        	chacl -R pathname...
        	chacl -D pathname...
        	chacl -B pathname...
        	chacl -l pathname...
        	chacl -r pathname...
        """;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern unsafe bool LookupAccountSidW(string system, byte[] sid, char* name, ref uint nameLen, char* domain, ref uint domainLen, out int use);
}
