using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace WinTools;

// s3fs / sshfs: real network mounts through `rclone mount`, which needs the WinFsp driver (the
// FUSE of Windows). The s3fs and sshfs command lines (bucket/host syntax, -o options, the
// passwd-s3fs file, ssh's own config and keys through Windows' OpenSSH) are translated into
// rclone settings; rclone runs in the background like the daemons it replaces.
static class RcloneMounts
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("s3fs", "Mount an S3 bucket as a folder or drive (through rclone and WinFsp)", S3fs);
        Commands.Add("sshfs", "Mount a remote folder over SFTP (through rclone, ssh and WinFsp)", Sshfs);
    }

    // ---- s3fs ----

    static int S3fs(string[] args)
    {
        var mpu = Array.FindIndex(args, a => a is "-u" or "--incomplete-mpu-list" || a.StartsWith("--incomplete-mpu-abort"));
        var unmount = Array.FindIndex(args, a => a == "--unmount");
        if (unmount >= 0)
        {
            if (unmount + 1 >= args.Length) throw new UsageException("--unmount needs a mount point");
            return Mounts.Unmount("s3fs", args[unmount + 1]);
        }
        var (opts, flags, positional) = Split(args);
        if (flags.Any(f => f is "-h" or "--help"))
        {
            Console.WriteLine("""
                Usage: s3fs BUCKET[:/PATH] MOUNTPOINT [OPTION]...
                       s3fs MOUNTPOINT -o bucket=BUCKET [OPTION]...
                       s3fs --unmount MOUNTPOINT
                       s3fs --incomplete-mpu-list (-u) BUCKET
                       s3fs --incomplete-mpu-abort[=all | =<expire date format>] BUCKET

                MOUNTPOINT is a drive letter (X:) or a folder. Mounting runs `rclone mount` in the
                background and needs rclone (brew install rclone) and WinFsp (winget install WinFsp.WinFsp).
                Credentials come from -o passwd_file (default ~/.passwd-s3fs, ACCESS_KEY_ID:SECRET_ACCESS_KEY
                or BUCKET:ACCESS_KEY_ID:SECRET_ACCESS_KEY), AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY, or
                the AWS credentials file (-o profile=NAME).

                Options:
                  -o url=URL                 S3 endpoint URL (default https://s3.amazonaws.com)
                  -o endpoint=REGION         region (default us-east-1)
                  -o use_path_request_style  path-style requests (for S3-compatible servers)
                  -o ro                      read-only mount
                  -o public_bucket=1         anonymous access
                  -o default_acl=ACL, storage_class=CLASS, use_sse, use_cache=DIR, profile=NAME
                  -f                         stay in the foreground
                  -d                         debug output
                  --version                  print the version
                """);
            return 0;
        }
        if (flags.Any(f => f is "--version" or "-V")) { Console.WriteLine($"Amazon Simple Storage Service File System V{Commands.Version} (wintools, using rclone)"); return 0; }
        var options = Mounts.Options(opts);
        var env = new Dictionary<string, string> { ["RCLONE_S3_PROVIDER"] = options.ContainsKey("url") && !options["url"].Contains("amazonaws.com") ? "Other" : "AWS" };
        string bucket, mountpoint = null;
        if (mpu >= 0)
        {
            if (positional.Count != 1) throw new UsageException("give the bucket name");
            bucket = positional[0];
        }
        else if (positional.Count == 2) { bucket = positional[0]; mountpoint = positional[1]; }
        else if (positional.Count == 1 && options.TryGetValue("bucket", out var b)) { bucket = b; mountpoint = positional[0]; }
        else throw new UsageException("Usage: s3fs BUCKET[:/PATH] MOUNTPOINT [OPTION]... (see s3fs --help)");
        var path = "";
        var colon = bucket.IndexOf(':');
        if (colon > 0) { path = bucket[(colon + 1)..].Trim('/'); bucket = bucket[..colon]; }

        if (options.TryGetValue("url", out var url)) env["RCLONE_S3_ENDPOINT"] = url;
        if (options.TryGetValue("endpoint", out var region)) env["RCLONE_S3_REGION"] = region;
        if (options.ContainsKey("use_path_request_style")) env["RCLONE_S3_FORCE_PATH_STYLE"] = "true";
        if (options.TryGetValue("default_acl", out var acl)) env["RCLONE_S3_ACL"] = acl;
        if (options.TryGetValue("storage_class", out var cls)) env["RCLONE_S3_STORAGE_CLASS"] = cls;
        if (options.ContainsKey("use_sse")) env["RCLONE_S3_SERVER_SIDE_ENCRYPTION"] = "AES256";
        if (options.TryGetValue("profile", out var profile)) { env["RCLONE_S3_PROFILE"] = profile; env["RCLONE_S3_ENV_AUTH"] = "true"; }
        else if (options.GetValueOrDefault("public_bucket") == "1") { }
        else if (Credentials(bucket, options.GetValueOrDefault("passwd_file")) is { } keys)
        {
            env["RCLONE_S3_ACCESS_KEY_ID"] = keys.Id;
            env["RCLONE_S3_SECRET_ACCESS_KEY"] = keys.Secret;
        }
        else env["RCLONE_S3_ENV_AUTH"] = "true";
        var remote = $":s3:{bucket}{(path.Length > 0 ? "/" + path : "")}";

        if (mpu >= 0)
        {
            var abort = args[mpu].StartsWith("--incomplete-mpu-abort");
            var rcloneArgs = abort ? new List<string> { "backend", "cleanup", remote } : ["backend", "list-multipart-uploads", remote];
            if (abort && args[mpu].Contains('='))
            {
                var when = args[mpu][(args[mpu].IndexOf('=') + 1)..];
                rcloneArgs.AddRange(["-o", "max-age=" + (when == "all" ? "0s" : when)]);
            }
            return RunRclone(rcloneArgs, env);
        }
        var extra = new List<string>();
        if (options.ContainsKey("ro")) extra.Add("--read-only");
        if (options.TryGetValue("use_cache", out var cache) && cache.Length > 0) extra.AddRange(["--cache-dir", cache]);
        if (options.TryGetValue("retries", out var retries)) extra.AddRange(["--low-level-retries", retries]);
        return Mount("s3fs", remote, mountpoint, env, extra, flags.Contains("-f"), flags.Contains("-d"));
    }

    // ~/.passwd-s3fs (or -o passwd_file): "ID:SECRET" or "BUCKET:ID:SECRET" lines; else the s3fs
    // environment variables.
    static (string Id, string Secret)? Credentials(string bucket, string file)
    {
        file ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".passwd-s3fs");
        if (File.Exists(file))
        {
            (string, string)? general = null;
            foreach (var line in File.ReadAllLines(file).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')))
            {
                var parts = line.Split(':');
                if (parts.Length == 3 && parts[0] == bucket) return (parts[1], parts[2]);
                if (parts.Length == 2) general ??= (parts[0], parts[1]);
            }
            if (general != null) return general;
        }
        var id = Environment.GetEnvironmentVariable("AWSACCESSKEYID");
        var secret = Environment.GetEnvironmentVariable("AWSSECRETACCESSKEY");
        return id != null && secret != null ? (id, secret) : null;
    }

    // ---- sshfs ----

    static int Sshfs(string[] args)
    {
        var unmount = Array.FindIndex(args, a => a is "-u" or "--unmount");
        if (unmount >= 0)
        {
            if (unmount + 1 >= args.Length) throw new UsageException($"{args[unmount]} needs a mount point");
            return Mounts.Unmount("sshfs", args[unmount + 1]);
        }
        var (opts, flags, positional) = Split(args, "-p", "-F");
        if (flags.Any(f => f is "-h" or "--help"))
        {
            Console.WriteLine("""
                usage: sshfs [user@]host:[dir] mountpoint [options]
                       sshfs -u mountpoint

                MOUNTPOINT is a drive letter (X:) or a folder. Mounting runs `rclone mount` in the
                background over Windows' OpenSSH client (your ~/.ssh config, keys and agent apply), and
                needs rclone (brew install rclone) and WinFsp (winget install WinFsp.WinFsp).

                SSHFS options:
                    -h   --help            print help
                    -V   --version         print version
                    -f                     foreground operation
                    -d                     debug output
                    -p PORT                equivalent to '-o port=PORT'
                    -C                     equivalent to '-o compression=yes'
                    -F ssh_configfile      specifies alternative ssh configuration file
                    -o port=PORT           port to connect to
                    -o IdentityFile=FILE   private key (and any other ssh -o option, e.g. ServerAliveInterval=15)
                    -o ssh_command=CMD     execute CMD instead of 'ssh'
                    -o password_stdin      read password from stdin (only for pam_mount!)
                    -o reconnect, follow_symlinks, idmap=user, allow_other ...  accepted
                    -o ro                  read-only mount
                """);
            return 0;
        }
        if (flags.Any(f => f is "-V" or "--version")) { Console.WriteLine($"SSHFS version {Commands.Version} (wintools, using rclone and OpenSSH)"); return 0; }
        if (positional.Count != 2) throw new UsageException("usage: sshfs [user@]host:[dir] mountpoint [options]");
        var target = positional[0];
        var colon = target.LastIndexOf(':');
        if (colon <= 0) throw new UsageException("missing host (use [user@]host:[dir])");
        var host = target[..colon];
        var dir = target[(colon + 1)..];
        var options = Mounts.Options(opts);
        var env = new Dictionary<string, string>();
        var ssh = new List<string> { options.GetValueOrDefault("ssh_command") ?? "ssh" };
        var port = flags.FirstOrDefault(f => f.StartsWith("-p="))?[3..] ?? options.GetValueOrDefault("port");
        if (port != null) ssh.AddRange(["-p", port]);
        if (flags.FirstOrDefault(f => f.StartsWith("-F=")) is { } config) ssh.AddRange(["-F", config[3..]]);
        if (flags.Contains("-C") || options.GetValueOrDefault("compression") == "yes") ssh.Add("-C");
        // Options that are ssh's own pass through as -o.
        var sshfsOnly = new HashSet<string> { "port", "ssh_command", "sftp_server", "directport", "reconnect", "follow_symlinks", "transform_symlinks",
            "idmap", "uidfile", "gidfile", "nomap", "password_stdin", "ro", "allow_other", "allow_root", "default_permissions", "compression", "cache",
            "cache_timeout", "dir_cache", "max_conns", "uid", "gid", "umask", "nonempty", "workaround", "delay_connect", "sshfs_debug", "sync_readdir" };
        foreach (var (key, value) in options.Where(o => !sshfsOnly.Contains(o.Key)))
            ssh.AddRange(["-o", value.Length > 0 ? $"{key}={value}" : key]);
        if (options.ContainsKey("password_stdin"))
        {
            // rclone's own SSH client takes the password (obscured); OpenSSH can't read one from stdin.
            var password = Console.In.ReadLine() ?? "";
            var at = host.IndexOf('@');
            env["RCLONE_SFTP_HOST"] = at >= 0 ? host[(at + 1)..] : host;
            if (at >= 0) env["RCLONE_SFTP_USER"] = host[..at];
            if (port != null) env["RCLONE_SFTP_PORT"] = port;
            env["RCLONE_SFTP_PASS"] = Obscure(password);
        }
        else
        {
            ssh.Add(host);
            env["RCLONE_SFTP_SSH"] = string.Join(' ', ssh.Select(a => a.Contains(' ') || a.Contains('"') ? "\"" + a.Replace("\"", "\"\"") + "\"" : a));
        }
        var extra = new List<string>();
        if (options.ContainsKey("ro")) extra.Add("--read-only");
        if (options.ContainsKey("follow_symlinks")) env["RCLONE_SFTP_SKIP_LINKS"] = "false";
        return Mount("sshfs", ":sftp:" + dir, positional[1], env, extra, flags.Contains("-f"), flags.Contains("-d"));
    }

    static string Obscure(string password)
    {
        var rclone = Rclone() ?? throw new IOException("rclone not found (brew install rclone)");
        var psi = new ProcessStartInfo(rclone) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
        psi.ArgumentList.Add("obscure");
        psi.ArgumentList.Add("-");
        using var p = Process.Start(psi);
        p.StandardInput.WriteLine(password);
        p.StandardInput.Close();
        var result = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return result;
    }

    // ---- shared ----

    static (List<string> Opts, List<string> Flags, List<string> Positional) Split(string[] args, params string[] valueFlags)
    {
        var opts = new List<string>();
        var flags = new List<string>();
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "-o") { if (++i >= args.Length) throw new UsageException("-o needs options"); opts.Add(args[i]); }
            else if (a.StartsWith("-o") && a.Length > 2) opts.Add(a[2..]);
            else if (valueFlags.Contains(a)) { if (++i >= args.Length) throw new UsageException($"{a} needs a value"); flags.Add(a + "=" + args[i]); }
            else if (a.StartsWith('-') && a.Length > 1) flags.Add(a);
            else positional.Add(a);
        }
        return (opts, flags, positional);
    }

    static string Rclone()
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim('"'), "rclone.exe");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    static bool WinFspInstalled()
    {
        using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\WinFsp");
        return key?.GetValue("InstallDir") is string dir && File.Exists(Path.Combine(dir, "bin", "winfsp-x64.dll"));
    }

    static int RunRclone(List<string> args, Dictionary<string, string> env)
    {
        var rclone = Rclone();
        if (rclone == null) { Console.Error.WriteLine("needs rclone: brew install rclone"); return 1; }
        var psi = new ProcessStartInfo(rclone) { UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        foreach (var (k, v) in env) psi.Environment[k] = v;
        using var p = Process.Start(psi);
        p.WaitForExit();
        return p.ExitCode;
    }

    static int Mount(string tool, string remote, string target, Dictionary<string, string> env, List<string> extra, bool foreground, bool debug)
    {
        var rclone = Rclone();
        if (rclone == null) { Console.Error.WriteLine($"{tool}: needs rclone to mount (brew install rclone)"); return 1; }
        if (!WinFspInstalled()) { Console.Error.WriteLine($"{tool}: mounting needs the WinFsp driver, Windows' FUSE (winget install WinFsp.WinFsp)"); return 1; }
        var mountpoint = Mounts.MountPath(target);
        if (Mounts.Load(mountpoint) != null) throw new IOException($"{mountpoint} is already mounted");
        // WinFsp creates the mount folder itself, so an existing empty one is removed first (and put back on unmount).
        var recreate = false;
        if (mountpoint.Length > 2 && Directory.Exists(mountpoint))
        {
            if (Directory.EnumerateFileSystemEntries(mountpoint).Any()) throw new IOException($"fuse: mountpoint is not empty: {mountpoint}");
            Directory.Delete(mountpoint);
            recreate = true;
        }
        var port = FreePort();
        var log = Path.Combine(Path.GetTempPath(), $"{tool}-{port}.log");
        var psi = new ProcessStartInfo(rclone) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var a in new[] { "mount", remote, mountpoint, "--vfs-cache-mode", "writes", "--volname", remote.TrimStart(':'),
                     "--rc", "--rc-addr", $"127.0.0.1:{port}", "--rc-no-auth" }.Concat(extra))
            psi.ArgumentList.Add(a);
        if (!foreground) { psi.ArgumentList.Add("--log-file"); psi.ArgumentList.Add(log); }
        if (debug) psi.ArgumentList.Add("-vv");
        foreach (var (k, v) in env) psi.Environment[k] = v;
        var p = Process.Start(psi);
        if (foreground)
        {
            Mounts.Save(mountpoint, new() { ["kind"] = "rclone", ["pid"] = p.Id.ToString(), ["rc"] = port.ToString(), ["recreate"] = recreate ? "1" : "0" });
            p.WaitForExit();
            Mounts.Forget(mountpoint);
            if (recreate) Directory.CreateDirectory(mountpoint);
            return p.ExitCode;
        }
        // Wait for the mount to appear (or rclone to give up) before returning, like the daemons do.
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (p.HasExited)
            {
                Console.Error.WriteLine($"{tool}: mount failed:");
                if (File.Exists(log)) Console.Error.Write(File.ReadAllText(log));
                if (recreate) Directory.CreateDirectory(mountpoint);
                return 1;
            }
            if (Directory.Exists(mountpoint + (mountpoint.Length == 2 ? "\\" : ""))) break;
            Thread.Sleep(250);
        }
        Mounts.Save(mountpoint, new() { ["kind"] = "rclone", ["pid"] = p.Id.ToString(), ["rc"] = port.ToString(), ["recreate"] = recreate ? "1" : "0", ["log"] = log });
        return 0;
    }

    // Asks rclone to quit through its remote control (it finishes pending uploads), else ends it.
    internal static void Stop(Dictionary<string, string> state)
    {
        Process p = null;
        try { p = Process.GetProcessById(int.Parse(state["pid"])); }
        catch (ArgumentException) { return; }
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.PostAsync($"http://127.0.0.1:{state["rc"]}/core/quit", new System.Net.Http.StringContent("{}")).GetAwaiter().GetResult();
        }
        catch (Exception e) when (e is System.Net.Http.HttpRequestException or TaskCanceledException) { }
        if (!p.WaitForExit(60000)) p.Kill();
    }

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
