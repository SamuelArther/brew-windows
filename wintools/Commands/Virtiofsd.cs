using System.Runtime.CompilerServices;

namespace WinTools;

// virtiofsd: the vhost-user backend of a virtio-fs device (a host directory shared with a VM). This
// is virtiofsd's command line with its option checks and its capability report for VMMs
// (--print-capabilities, read by libvirt and QEMU's vhost-user.json discovery). Serving a VM can't
// happen on a Windows host: vhost-user gives the backend the guest's memory and the vring eventfds as
// file descriptors over SCM_RIGHTS, which Windows AF_UNIX sockets can't carry, and no Windows VMM
// (QEMU's Windows build has no vhost-user, Hyper-V has no virtio devices) offers a virtio-fs device
// to back. Asked to serve, virtiofsd checks its options and then says so. Linux guests under WSL2
// see Windows folders already (/mnt/c).
static class Virtiofsd
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("virtiofsd", "virtio-fs vhost-user backend options and capability report", Run);

    const string Version = "1.14.0";

    static int Run(string[] args)
    {
        string sharedDir = null, socketPath = null, sandbox = "namespace", cache = "auto", fd = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.StartsWith("--") ? a.IndexOf('=') : -1;
            var name = eq > 0 ? a[..eq] : a;
            string Value() => eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"a value is required for '{name}'");
            switch (name)
            {
                case "-h" or "--help": Usage(); return 0;
                case "-V" or "--version": Console.WriteLine($"virtiofsd {Version}"); return 0;
                case "--print-capabilities":
                    Console.WriteLine("{\n  \"type\": \"fs\",\n  \"features\": [\n    \"migrate-precopy\",\n    \"separate-options\"\n  ]\n}");
                    return 0;
                case "--shared-dir": sharedDir = Value(); break;
                case "--socket-path" or "--socket": socketPath = Value(); break;
                case "--fd": fd = Value(); break;
                case "--sandbox": sandbox = Value(); break;
                case "--cache": cache = Value(); break;
                case "-o":
                    foreach (var opt in Value().Split(','))
                        if (opt.StartsWith("source=")) sharedDir ??= opt[7..];
                    break;
                case "--log-level" or "--thread-pool-size" or "--rlimit-nofile" or "--modcaps" or "--inode-file-handles" or "--socket-group"
                    or "--migration-mode" or "--migration-on-error" or "--translate-uid" or "--translate-gid" or "--shared-dir-id":
                    Value();
                    break;
                case "--announce-submounts" or "--xattr" or "--no-xattr" or "--posix-acl" or "--no-posix-acl" or "--allow-direct-io"
                    or "--writeback" or "--killpriv-v2" or "--no-killpriv-v2" or "--security-label" or "--readonly" or "--syslog"
                    or "--allow-mmap" or "--preserve-noatime" or "-f" or "-d":
                    break;
                default: throw new UsageException($"unexpected argument '{a}' found");
            }
        }
        if (sharedDir == null) throw new UsageException("the following required arguments were not provided:\n  --shared-dir <SHARED_DIR>");
        if (socketPath == null && fd == null) throw new UsageException("the following required arguments were not provided:\n  --socket-path <SOCKET_PATH>");
        if (!Directory.Exists(sharedDir)) throw new InvalidOperationException($"shared directory {sharedDir} is not a directory");
        if (sandbox is not ("namespace" or "chroot" or "none")) throw new UsageException($"invalid value '{sandbox}' for '--sandbox <SANDBOX>'");
        if (cache is not ("never" or "metadata" or "auto" or "always")) throw new UsageException($"invalid value '{cache}' for '--cache <CACHE>'");
        Console.Error.WriteLine("virtiofsd: can't serve a VM on Windows: vhost-user passes guest memory as file descriptors, which Windows sockets can't carry, and no Windows VMM has vhost-user virtio-fs");
        return 1;
    }

    static void Usage() => Console.WriteLine($"""
        virtiofsd backend {Version}
        Launch a virtiofsd backend.

        Usage: virtiofsd [OPTIONS] --shared-dir <SHARED_DIR> --socket-path <SOCKET_PATH>

        Options:
              --shared-dir <SHARED_DIR>    Shared directory path
              --socket-path <SOCKET_PATH>  vhost-user socket path
              --fd <FD>                    File descriptor for the listening socket
              --sandbox <SANDBOX>          Sandbox mechanism (namespace, chroot, none) [default: namespace]
              --cache <CACHE>              The caching policy (auto, always, metadata, never) [default: auto]
              --log-level <LOG_LEVEL>      Log level (error, warn, info, debug, trace, off) [default: info]
              --print-capabilities         Print vhost-user.json backend program capabilities and exit
          -h, --help                       Print help
          -V, --version                    Print version

        On Windows virtiofsd checks its options and reports its capabilities, but can't serve a VM:
        vhost-user needs file descriptor passing that Windows sockets don't have.
        """);
}
