using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Xml;

namespace WinTools;

// Linux-PAM's helper programs on Windows' own authentication stack. unix_chkpwd checks a password
// against the account database (LogonUser, a network logon: nothing is started and no profile is
// loaded); faillock shows the failed logons Windows recorded (Security log event 4625) and the
// account's lockout state, and --reset unlocks it; mkhomedir_helper creates a user's home, which on
// Windows is the user profile (CreateProfile copies C:\Users\Default, Windows' skeleton directory).
// Exit statuses are PAM's (PAM_AUTH_ERR = 7, PAM_USER_UNKNOWN = 10, ...).
static unsafe class LinuxPam
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("unix_chkpwd", "Check a user's password (read from stdin) against Windows accounts (Linux-PAM)", UnixChkpwd);
        Commands.Add("faillock", "Show failed logons and lockout state of Windows accounts, or reset them (Linux-PAM)", Faillock);
        Commands.Add("mkhomedir_helper", "Create a user's home directory (Windows user profile) (Linux-PAM)", Mkhomedir);
    }

    const int PAM_SUCCESS = 0, PAM_SYSTEM_ERR = 4, PAM_PERM_DENIED = 6, PAM_AUTH_ERR = 7, PAM_USER_UNKNOWN = 10,
        PAM_NEW_AUTHTOK_REQD = 12, PAM_ACCT_EXPIRED = 13, PAM_SESSION_ERR = 14, PAM_AUTHINFO_UNAVAIL = 9;

    static SecurityIdentifier Lookup(string user)
    {
        try { return (SecurityIdentifier)new NTAccount(user).Translate(typeof(SecurityIdentifier)); }
        catch (Exception e) when (e is IdentityNotMappedException or SystemException) { return null; }
    }

    static (string Domain, string Name) Split(string user)
    {
        var slash = user.IndexOf('\\');
        if (slash > 0) return (user[..slash], user[(slash + 1)..]);
        var at = user.IndexOf('@');
        return at > 0 ? (null, user) : (".", user);
    }

    // ---- unix_chkpwd ----

    static int UnixChkpwd(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            Console.WriteLine("usage: unix_chkpwd <username> [nullok|nonull|chkexpiry]\n" +
                              "The password is read from stdin (up to a NUL or end of input). Exit status 0 when it is right,\n" +
                              "7 (PAM_AUTH_ERR) when it is wrong; chkexpiry reports expired passwords and accounts instead.");
            return 0;
        }
        if (args.Length is < 1 or > 2) { Console.Error.WriteLine("This binary is not designed for running in this way\n-- the system administrator has been informed"); return PAM_SYSTEM_ERR; }
        var user = args[0];
        var mode = args.Length > 1 ? args[1] : "nonull";
        if (Lookup(user) == null) return PAM_USER_UNKNOWN;
        var (domain, name) = Split(user);
        if (mode == "chkexpiry") return Expiry(domain, name);
        if (mode is not ("nullok" or "nonull")) { Console.Error.WriteLine($"unix_chkpwd: unknown option {mode}"); return PAM_SYSTEM_ERR; }
        if (!Console.IsInputRedirected) { Console.Error.WriteLine("This binary is not designed for running in this way\n-- the system administrator has been informed"); return PAM_SYSTEM_ERR; }
        var input = new StringBuilder();
        int c;
        using (var stdin = new StreamReader(Console.OpenStandardInput()))
            while ((c = stdin.Read()) >= 0 && c != 0) input.Append((char)c);
        var password = input.ToString().TrimEnd('\r', '\n');
        if (password.Length == 0 && mode == "nonull") return PAM_AUTH_ERR;
        if (LogonUserW(name, domain, password, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, out var token))
        {
            CloseHandle(token);
            return PAM_SUCCESS;
        }
        return Marshal.GetLastWin32Error() switch
        {
            ERROR_PASSWORD_EXPIRED or ERROR_PASSWORD_MUST_CHANGE => PAM_NEW_AUTHTOK_REQD,
            ERROR_ACCOUNT_EXPIRED or ERROR_ACCOUNT_DISABLED or ERROR_ACCOUNT_LOCKED_OUT => PAM_ACCT_EXPIRED,
            _ => PAM_AUTH_ERR,
        };
    }

    // chkexpiry: 0 when fine, PAM_NEW_AUTHTOK_REQD when the password has expired, PAM_ACCT_EXPIRED for the account.
    static unsafe int Expiry(string domain, string name)
    {
        var info = UserInfo(domain, name);
        if (info == null) return PAM_AUTHINFO_UNAVAIL;
        var (flags, _, passwordExpired, accountExpires) = info.Value;
        if ((flags & UF_ACCOUNTDISABLE) != 0) return PAM_ACCT_EXPIRED;
        if (accountExpires != TIMEQ_FOREVER && DateTimeOffset.FromUnixTimeSeconds(accountExpires) < DateTimeOffset.UtcNow) return PAM_ACCT_EXPIRED;
        return passwordExpired ? PAM_NEW_AUTHTOK_REQD : PAM_SUCCESS;
    }

    // USER_INFO_3 fields: flags, bad password count, password expired, account expiry (seconds since 1970).
    static unsafe (uint Flags, uint BadPasswords, bool PasswordExpired, uint AccountExpires)? UserInfo(string domain, string name)
    {
        var server = domain is null or "." || domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ? null : domain;
        if (NetUserGetInfo(server, name, 3, out var buffer) != 0) return null;
        try
        {
            var u = (USER_INFO_3*)buffer;
            return (u->flags, u->bad_pw_count, u->password_expired != 0, u->acct_expires);
        }
        finally { NetApiBufferFree(buffer); }
    }

    // ---- faillock ----

    static int Faillock(string[] args)
    {
        string user = null;
        bool reset = false, legacy = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: faillock [--dir /path/to/tally-directory] [--user username] [--reset] [--legacy-output]\n" +
                                      "Failed logons come from the Windows Security log (event 4625, needs an elevated prompt);\n" +
                                      "--reset unlocks the account and clears its bad-password count.");
                    return 0;
                case "--user": user = ++i < args.Length ? args[i] : throw new UsageException("--user needs a user name"); break;
                case "--dir": i++; break;
                case "--reset": reset = true; break;
                case "--legacy-output": legacy = true; break;
                default: throw new UsageException($"Unknown option: {args[i]}");
            }
        }
        var users = user != null ? [user] : LocalUsers();
        if (reset)
        {
            if (user == null) throw new UsageException("--reset needs --user (resetting every account is not supported)");
            if (Lookup(user) == null) { Console.Error.WriteLine($"faillock: Unknown user: {user}"); return 1; }
            var (domain, name) = Split(user);
            var info = UserInfo(domain, name) ?? throw new InvalidOperationException($"cannot read account {user}");
            var level = new USER_INFO_1008 { usri1008_flags = info.Flags & ~UF_LOCKOUT };
            var rc = NetUserSetInfo(domain is null or "." ? null : domain, name, 1008, ref level, out _);
            if (rc != 0) { Console.Error.WriteLine($"faillock: Error resetting {user}: {new System.ComponentModel.Win32Exception((int)rc).Message}"); return 1; }
            return 0;
        }
        var status = 0;
        foreach (var u in users)
        {
            if (Lookup(u) == null) { Console.Error.WriteLine($"faillock: Unknown user: {u}"); status = 1; continue; }
            var (domain, name) = Split(u);
            var info = UserInfo(domain, name);
            var events = FailedLogons(name);
            Console.WriteLine($"{u}:");
            if (!legacy) Console.WriteLine($"{"When",-19} {"Type",-5} {"Source",-48} Valid");
            else Console.WriteLine("Login           Failures    Latest failure     From");
            if (legacy)
            {
                var last = events.FirstOrDefault();
                Console.WriteLine($"{u,-15} {info?.BadPasswords ?? (uint)events.Count,8}    {(last.When == default ? "" : last.When.ToString("MM/dd/yy HH:mm:ss")),-18} {last.Source}");
                continue;
            }
            foreach (var (when, type, source) in events)
                Console.WriteLine($"{when:yyyy-MM-dd HH:mm:ss} {type,-5} {source,-48} V");
            if (info is { } i && (i.Flags & UF_LOCKOUT) != 0) Console.WriteLine("(account is locked out)");
        }
        return status;
    }

    static unsafe List<string> LocalUsers()
    {
        var list = new List<string>();
        uint resume = 0;
        while (true)
        {
            var rc = NetUserEnum(null, 0, FILTER_NORMAL_ACCOUNT, out var buffer, 0xFFFFFFFF, out var read, out _, ref resume);
            if (rc is not (0 or ERROR_MORE_DATA)) break;
            for (var i = 0; i < read; i++) list.Add(Marshal.PtrToStringUni(*(IntPtr*)((byte*)buffer + i * IntPtr.Size)));
            NetApiBufferFree(buffer);
            if (rc == 0) break;
        }
        return list;
    }

    // Security log event 4625 (an account failed to log on) for this user, newest first.
    static List<(DateTime When, string Type, string Source)> FailedLogons(string name)
    {
        var list = new List<(DateTime, string, string)>();
        var query = $"*[System[EventID=4625] and EventData[Data[@Name='TargetUserName']='{name.Replace("'", "")}']]";
        var handle = EvtQuery(IntPtr.Zero, "Security", query, EvtQueryChannelPath | EvtQueryReverseDirection);
        if (handle == IntPtr.Zero) return list;
        try
        {
            var events = new IntPtr[32];
            while (list.Count < 100 && EvtNext(handle, (uint)events.Length, events, 1000, 0, out var returned))
            {
                for (var i = 0; i < returned; i++)
                {
                    try
                    {
                        var xml = Render(events[i]);
                        if (xml == null) continue;
                        var doc = new XmlDocument();
                        doc.LoadXml(xml);
                        var ns = new XmlNamespaceManager(doc.NameTable);
                        ns.AddNamespace("e", "http://schemas.microsoft.com/win/2004/08/events/event");
                        var time = (doc.SelectSingleNode("//e:System/e:TimeCreated/@SystemTime", ns) as XmlAttribute)?.Value;
                        string Data(string n) => doc.SelectSingleNode($"//e:EventData/e:Data[@Name='{n}']", ns)?.InnerText;
                        var ip = Data("IpAddress");
                        var workstation = Data("WorkstationName");
                        var remote = ip is { Length: > 1 } && ip is not ("127.0.0.1" or "::1");
                        var source = remote ? ip : workstation is { Length: > 1 } ? workstation : Data("LogonProcessName")?.Trim() ?? "";
                        list.Add((DateTime.TryParse(time, out var when) ? when.ToLocalTime() : default, remote ? "RHOST" : "TTY", source));
                    }
                    finally { EvtClose(events[i]); }
                }
            }
        }
        finally { EvtClose(handle); }
        return list;
    }

    static string Render(IntPtr evt)
    {
        EvtRender(IntPtr.Zero, evt, EvtRenderEventXml, 0, IntPtr.Zero, out var size, out _);
        if (size == 0) return null;
        var buffer = Marshal.AllocHGlobal((int)size);
        try { return EvtRender(IntPtr.Zero, evt, EvtRenderEventXml, size, buffer, out _, out _) ? Marshal.PtrToStringUni(buffer) : null; }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    // ---- mkhomedir_helper ----

    static int Mkhomedir(string[] args)
    {
        if (args.Length is < 1 or > 4 || args[0].StartsWith('-') && args[0] is not ("-h" or "--help"))
        {
            Console.Error.WriteLine("Usage: mkhomedir_helper <username> [<umask> [<skeldir> [<home_dir>]]] [<home_mode>]");
            return PAM_SESSION_ERR;
        }
        if (args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage: mkhomedir_helper <username> [<umask> [<skeldir> [<home_dir>]]] [<home_mode>]\n" +
                              "Creates the user's Windows profile (copied from C:\\Users\\Default) if it doesn't exist yet;\n" +
                              "files in <skeldir> are copied on top. Needs an elevated prompt.");
            return 0;
        }
        var user = args[0];
        var sid = Lookup(user);
        if (sid == null) { Console.Error.WriteLine($"mkhomedir_helper: User unknown: {user}"); return PAM_USER_UNKNOWN; }
        var home = ProfilePath(sid);
        if (home == null)
        {
            var buffer = new StringBuilder(260);
            var hr = CreateProfile(sid.Value, Split(user).Name, buffer, (uint)buffer.Capacity);
            if (hr != 0 && hr != unchecked((int)0x800700B7))   // already exists
            {
                Console.Error.WriteLine($"mkhomedir_helper: Unable to create the profile for {user}: {new System.ComponentModel.Win32Exception(hr & 0xFFFF).Message}");
                return (hr & 0xFFFF) == 5 ? PAM_PERM_DENIED : PAM_SYSTEM_ERR;
            }
            home = buffer.ToString();
            if (home.Length == 0) home = ProfilePath(sid);
        }
        var skel = args.Length > 2 ? args[2] : null;
        if (skel != null && home != null && Directory.Exists(skel))
            foreach (var file in Directory.EnumerateFiles(skel, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(home, Path.GetRelativePath(skel, file));
                if (File.Exists(target)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
        return PAM_SUCCESS;
    }

    static string ProfilePath(SecurityIdentifier sid)
    {
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{sid.Value}");
        return key?.GetValue("ProfileImagePath") is string p ? Environment.ExpandEnvironmentVariables(p) : null;
    }

    // ---- Win32 ----

    const int LOGON32_LOGON_NETWORK = 3, LOGON32_PROVIDER_DEFAULT = 0;
    const int ERROR_PASSWORD_EXPIRED = 1330, ERROR_PASSWORD_MUST_CHANGE = 1907, ERROR_ACCOUNT_EXPIRED = 1793, ERROR_ACCOUNT_DISABLED = 1331,
        ERROR_ACCOUNT_LOCKED_OUT = 1909;
    const uint UF_ACCOUNTDISABLE = 0x2, UF_LOCKOUT = 0x10, TIMEQ_FOREVER = 0xFFFFFFFF, FILTER_NORMAL_ACCOUNT = 0x2, ERROR_MORE_DATA = 234;
    const uint EvtQueryChannelPath = 1, EvtQueryReverseDirection = 0x200, EvtRenderEventXml = 1;

    [StructLayout(LayoutKind.Sequential)]
    struct USER_INFO_1008 { public uint usri1008_flags; }

    [StructLayout(LayoutKind.Sequential)]
    struct USER_INFO_3
    {
        public IntPtr name, password;
        public uint password_age, priv;
        public IntPtr home_dir, comment;
        public uint flags;
        public IntPtr script_path;
        public uint auth_flags;
        public IntPtr full_name, usr_comment, parms, workstations;
        public uint last_logon, last_logoff, acct_expires, max_storage, units_per_week;
        public IntPtr logon_hours;
        public uint bad_pw_count, num_logons;
        public IntPtr logon_server;
        public uint country_code, code_page, user_id, primary_group_id;
        public IntPtr profile, home_dir_drive;
        public uint password_expired;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool LogonUserW(string user, string domain, string password, int type, int provider, out IntPtr token);

    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
    static extern uint NetUserGetInfo(string server, string user, uint level, out IntPtr buffer);

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
    static extern uint NetUserSetInfo(string server, string user, uint level, ref USER_INFO_1008 info, out uint error);

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
    static extern uint NetUserEnum(string server, uint level, uint filter, out IntPtr buffer, uint maxLength, out uint read, out uint total, ref uint resume);

    [DllImport("netapi32.dll")] static extern uint NetApiBufferFree(IntPtr buffer);

    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    static extern int CreateProfile(string sid, string user, StringBuilder profilePath, uint length);

    [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr EvtQuery(IntPtr session, string path, string query, uint flags);

    [DllImport("wevtapi.dll", SetLastError = true)]
    static extern bool EvtNext(IntPtr resultSet, uint count, IntPtr[] events, uint timeout, uint flags, out uint returned);

    [DllImport("wevtapi.dll", SetLastError = true)]
    static extern bool EvtRender(IntPtr context, IntPtr fragment, uint flags, uint bufferSize, IntPtr buffer, out uint used, out uint propertyCount);

    [DllImport("wevtapi.dll")] static extern bool EvtClose(IntPtr handle);
}
