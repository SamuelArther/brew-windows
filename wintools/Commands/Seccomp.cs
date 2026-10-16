using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// scmp_sys_resolver (libseccomp): resolves Linux system call names to numbers and back for an
// architecture, the lookup seccomp filter authors and container tools (runc, crun, podman
// profiles) need. Pure table lookup, so it works the same on Windows; the tables are the
// kernel's syscall tables for x86_64, x32, x86, aarch64 and riscv64.
static class Seccomp
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("scmp_sys_resolver", "Resolve Linux syscall names and numbers for an architecture", Run);

    const int X32Bit = 0x40000000;

    // Space-separated names in number order starting at 0; "-" marks an unused number.
    const string X86_64 =
        "read write open close stat fstat lstat poll lseek mmap mprotect munmap brk rt_sigaction rt_sigprocmask " +
        "rt_sigreturn ioctl pread64 pwrite64 readv writev access pipe select sched_yield mremap msync mincore madvise " +
        "shmget shmat shmctl dup dup2 pause nanosleep getitimer alarm setitimer getpid sendfile socket connect accept " +
        "sendto recvfrom sendmsg recvmsg shutdown bind listen getsockname getpeername socketpair setsockopt getsockopt " +
        "clone fork vfork execve exit wait4 kill uname semget semop semctl shmdt msgget msgsnd msgrcv msgctl fcntl flock " +
        "fsync fdatasync truncate ftruncate getdents getcwd chdir fchdir rename mkdir rmdir creat link unlink symlink " +
        "readlink chmod fchmod chown fchown lchown umask gettimeofday getrlimit getrusage sysinfo times ptrace getuid " +
        "syslog getgid setuid setgid geteuid getegid setpgid getppid getpgrp setsid setreuid setregid getgroups setgroups " +
        "setresuid getresuid setresgid getresgid getpgid setfsuid setfsgid getsid capget capset rt_sigpending " +
        "rt_sigtimedwait rt_sigqueueinfo rt_sigsuspend sigaltstack utime mknod uselib personality ustat statfs fstatfs " +
        "sysfs getpriority setpriority sched_setparam sched_getparam sched_setscheduler sched_getscheduler " +
        "sched_get_priority_max sched_get_priority_min sched_rr_get_interval mlock munlock mlockall munlockall vhangup " +
        "modify_ldt pivot_root _sysctl prctl arch_prctl adjtimex setrlimit chroot sync acct settimeofday mount umount2 " +
        "swapon swapoff reboot sethostname setdomainname iopl ioperm create_module init_module delete_module " +
        "get_kernel_syms query_module quotactl nfsservctl getpmsg putpmsg afs_syscall tuxcall security gettid readahead " +
        "setxattr lsetxattr fsetxattr getxattr lgetxattr fgetxattr listxattr llistxattr flistxattr removexattr " +
        "lremovexattr fremovexattr tkill time futex sched_setaffinity sched_getaffinity set_thread_area io_setup " +
        "io_destroy io_getevents io_submit io_cancel get_thread_area lookup_dcookie epoll_create epoll_ctl_old " +
        "epoll_wait_old remap_file_pages getdents64 set_tid_address restart_syscall semtimedop fadvise64 timer_create " +
        "timer_settime timer_gettime timer_getoverrun timer_delete clock_settime clock_gettime clock_getres " +
        "clock_nanosleep exit_group epoll_wait epoll_ctl tgkill utimes vserver mbind set_mempolicy get_mempolicy mq_open " +
        "mq_unlink mq_timedsend mq_timedreceive mq_notify mq_getsetattr kexec_load waitid add_key request_key keyctl " +
        "ioprio_set ioprio_get inotify_init inotify_add_watch inotify_rm_watch migrate_pages openat mkdirat mknodat " +
        "fchownat futimesat newfstatat unlinkat renameat linkat symlinkat readlinkat fchmodat faccessat pselect6 ppoll " +
        "unshare set_robust_list get_robust_list splice tee sync_file_range vmsplice move_pages utimensat epoll_pwait " +
        "signalfd timerfd_create eventfd fallocate timerfd_settime timerfd_gettime accept4 signalfd4 eventfd2 " +
        "epoll_create1 dup3 pipe2 inotify_init1 preadv pwritev rt_tgsigqueueinfo perf_event_open recvmmsg fanotify_init " +
        "fanotify_mark prlimit64 name_to_handle_at open_by_handle_at clock_adjtime syncfs sendmmsg setns getcpu " +
        "process_vm_readv process_vm_writev kcmp finit_module sched_setattr sched_getattr renameat2 seccomp getrandom " +
        "memfd_create kexec_file_load bpf execveat userfaultfd membarrier mlock2 copy_file_range preadv2 pwritev2 " +
        "pkey_mprotect pkey_alloc pkey_free statx io_pgetevents rseq uretprobe";

    // x86_64 calls that exist only for the 64-bit ABI (x32 has its own numbers or none).
    const string Only64 =
        "rt_sigaction rt_sigreturn ioctl readv writev recvfrom sendmsg recvmsg setsockopt getsockopt execve ptrace " +
        "rt_sigpending rt_sigtimedwait rt_sigqueueinfo sigaltstack uselib _sysctl create_module get_kernel_syms " +
        "query_module nfsservctl set_thread_area io_setup io_submit get_thread_area epoll_ctl_old epoll_wait_old " +
        "timer_create vserver mq_notify kexec_load waitid set_robust_list get_robust_list vmsplice move_pages preadv " +
        "pwritev rt_tgsigqueueinfo recvmmsg sendmmsg process_vm_readv process_vm_writev execveat preadv2 pwritev2 uretprobe";

    // x32's own numbers, from 512.
    const string X32Specific =
        "rt_sigaction rt_sigreturn ioctl readv writev recvfrom sendmsg recvmsg execve ptrace rt_sigpending " +
        "rt_sigtimedwait rt_sigqueueinfo sigaltstack timer_create mq_notify kexec_load waitid set_robust_list " +
        "get_robust_list vmsplice move_pages preadv pwritev rt_tgsigqueueinfo recvmmsg sendmmsg process_vm_readv " +
        "process_vm_writev setsockopt getsockopt io_setup io_submit execveat preadv2 pwritev2";

    const string X86 =
        "restart_syscall exit fork read write open close waitpid creat link unlink execve chdir time mknod chmod lchown " +
        "break oldstat lseek getpid mount umount setuid getuid stime ptrace alarm oldfstat pause utime stty gtty access " +
        "nice ftime sync kill rename mkdir rmdir dup pipe times prof brk setgid getgid signal geteuid getegid acct " +
        "umount2 lock ioctl fcntl mpx setpgid ulimit oldolduname umask chroot ustat dup2 getppid getpgrp setsid " +
        "sigaction sgetmask ssetmask setreuid setregid sigsuspend sigpending sethostname setrlimit getrlimit getrusage " +
        "gettimeofday settimeofday getgroups setgroups select symlink oldlstat readlink uselib swapon reboot readdir mmap " +
        "munmap truncate ftruncate fchmod fchown getpriority setpriority profil statfs fstatfs ioperm socketcall syslog " +
        "setitimer getitimer stat lstat fstat olduname iopl vhangup idle vm86old wait4 swapoff sysinfo ipc fsync " +
        "sigreturn clone setdomainname uname modify_ldt adjtimex mprotect sigprocmask create_module init_module " +
        "delete_module get_kernel_syms quotactl getpgid fchdir bdflush sysfs personality afs_syscall setfsuid setfsgid " +
        "_llseek getdents _newselect flock msync readv writev getsid fdatasync _sysctl mlock munlock mlockall munlockall " +
        "sched_setparam sched_getparam sched_setscheduler sched_getscheduler sched_yield sched_get_priority_max " +
        "sched_get_priority_min sched_rr_get_interval nanosleep mremap setresuid getresuid vm86 query_module poll " +
        "nfsservctl setresgid getresgid prctl rt_sigreturn rt_sigaction rt_sigprocmask rt_sigpending rt_sigtimedwait " +
        "rt_sigqueueinfo rt_sigsuspend pread64 pwrite64 chown getcwd capget capset sigaltstack sendfile getpmsg putpmsg " +
        "vfork ugetrlimit mmap2 truncate64 ftruncate64 stat64 lstat64 fstat64 lchown32 getuid32 getgid32 geteuid32 " +
        "getegid32 setreuid32 setregid32 getgroups32 setgroups32 fchown32 setresuid32 getresuid32 setresgid32 " +
        "getresgid32 chown32 setuid32 setgid32 setfsuid32 setfsgid32 pivot_root mincore madvise getdents64 fcntl64 - - " +
        "gettid readahead setxattr lsetxattr fsetxattr getxattr lgetxattr fgetxattr listxattr llistxattr flistxattr " +
        "removexattr lremovexattr fremovexattr tkill sendfile64 futex sched_setaffinity sched_getaffinity set_thread_area " +
        "get_thread_area io_setup io_destroy io_getevents io_submit io_cancel fadvise64 - exit_group lookup_dcookie " +
        "epoll_create epoll_ctl epoll_wait remap_file_pages set_tid_address timer_create timer_settime timer_gettime " +
        "timer_getoverrun timer_delete clock_settime clock_gettime clock_getres clock_nanosleep statfs64 fstatfs64 tgkill " +
        "utimes fadvise64_64 vserver mbind get_mempolicy set_mempolicy mq_open mq_unlink mq_timedsend mq_timedreceive " +
        "mq_notify mq_getsetattr kexec_load waitid - add_key request_key keyctl ioprio_set ioprio_get inotify_init " +
        "inotify_add_watch inotify_rm_watch migrate_pages openat mkdirat mknodat fchownat futimesat fstatat64 unlinkat " +
        "renameat linkat symlinkat readlinkat fchmodat faccessat pselect6 ppoll unshare set_robust_list get_robust_list " +
        "splice sync_file_range tee vmsplice move_pages getcpu epoll_pwait utimensat signalfd timerfd_create eventfd " +
        "fallocate timerfd_settime timerfd_gettime signalfd4 eventfd2 epoll_create1 dup3 pipe2 inotify_init1 preadv " +
        "pwritev rt_tgsigqueueinfo perf_event_open recvmmsg fanotify_init fanotify_mark prlimit64 name_to_handle_at " +
        "open_by_handle_at clock_adjtime syncfs sendmmsg setns process_vm_readv process_vm_writev kcmp finit_module " +
        "sched_setattr sched_getattr renameat2 seccomp getrandom memfd_create bpf execveat socket socketpair bind connect " +
        "listen accept4 getsockopt setsockopt getsockname getpeername sendto sendmsg recvfrom recvmsg shutdown " +
        "userfaultfd membarrier mlock2 copy_file_range preadv2 pwritev2 pkey_mprotect pkey_alloc pkey_free statx " +
        "arch_prctl io_pgetevents rseq - - - - - - semget semctl shmget shmctl shmat shmdt msgget msgsnd msgrcv msgctl " +
        "clock_gettime64 clock_settime64 clock_adjtime64 clock_getres_time64 clock_nanosleep_time64 timer_gettime64 " +
        "timer_settime64 timerfd_gettime64 timerfd_settime64 utimensat_time64 pselect6_time64 ppoll_time64 - " +
        "io_pgetevents_time64 recvmmsg_time64 mq_timedsend_time64 mq_timedreceive_time64 semtimedop_time64 " +
        "rt_sigtimedwait_time64 futex_time64 sched_rr_get_interval_time64";

    // asm-generic/unistd.h, the table aarch64 and riscv64 use; 244-259 are per-architecture.
    const string Generic =
        "io_setup io_destroy io_submit io_cancel io_getevents setxattr lsetxattr fsetxattr getxattr lgetxattr fgetxattr " +
        "listxattr llistxattr flistxattr removexattr lremovexattr fremovexattr getcwd lookup_dcookie eventfd2 " +
        "epoll_create1 epoll_ctl epoll_pwait dup dup3 fcntl inotify_init1 inotify_add_watch inotify_rm_watch ioctl " +
        "ioprio_set ioprio_get flock mknodat mkdirat unlinkat symlinkat linkat renameat umount2 mount pivot_root " +
        "nfsservctl statfs fstatfs truncate ftruncate fallocate faccessat chdir fchdir chroot fchmod fchmodat fchownat " +
        "fchown openat close vhangup pipe2 quotactl getdents64 lseek read write readv writev pread64 pwrite64 preadv " +
        "pwritev sendfile pselect6 ppoll signalfd4 vmsplice splice tee readlinkat newfstatat fstat sync fsync fdatasync " +
        "sync_file_range timerfd_create timerfd_settime timerfd_gettime utimensat acct capget capset personality exit " +
        "exit_group waitid set_tid_address unshare futex set_robust_list get_robust_list nanosleep getitimer setitimer " +
        "kexec_load init_module delete_module timer_create timer_gettime timer_getoverrun timer_settime timer_delete " +
        "clock_settime clock_gettime clock_getres clock_nanosleep syslog ptrace sched_setparam sched_setscheduler " +
        "sched_getscheduler sched_getparam sched_setaffinity sched_getaffinity sched_yield sched_get_priority_max " +
        "sched_get_priority_min sched_rr_get_interval restart_syscall kill tkill tgkill sigaltstack rt_sigsuspend " +
        "rt_sigaction rt_sigprocmask rt_sigpending rt_sigtimedwait rt_sigqueueinfo rt_sigreturn setpriority getpriority " +
        "reboot setregid setgid setreuid setuid setresuid getresuid setresgid getresgid setfsuid setfsgid times setpgid " +
        "getpgid getsid setsid getgroups setgroups uname sethostname setdomainname getrlimit setrlimit getrusage umask " +
        "prctl getcpu gettimeofday settimeofday adjtimex getpid getppid getuid geteuid getgid getegid gettid sysinfo " +
        "mq_open mq_unlink mq_timedsend mq_timedreceive mq_notify mq_getsetattr msgget msgctl msgrcv msgsnd semget " +
        "semctl semtimedop semop shmget shmctl shmat shmdt socket socketpair bind listen accept connect getsockname " +
        "getpeername sendto recvfrom setsockopt getsockopt shutdown sendmsg recvmsg readahead brk munmap mremap add_key " +
        "request_key keyctl clone execve mmap fadvise64 swapon swapoff mprotect msync mlock munlock mlockall munlockall " +
        "mincore madvise remap_file_pages mbind get_mempolicy set_mempolicy migrate_pages move_pages rt_tgsigqueueinfo " +
        "perf_event_open accept4 recvmmsg - - - - - - - - - - - - - - - - wait4 prlimit64 fanotify_init fanotify_mark " +
        "name_to_handle_at open_by_handle_at clock_adjtime syncfs setns sendmmsg process_vm_readv process_vm_writev kcmp " +
        "finit_module sched_setattr sched_getattr renameat2 seccomp getrandom memfd_create bpf execveat userfaultfd " +
        "membarrier mlock2 copy_file_range preadv2 pwritev2 pkey_mprotect pkey_alloc pkey_free statx io_pgetevents rseq " +
        "kexec_file_load";

    // Every architecture from 424 on.
    const string Common424 =
        "pidfd_send_signal io_uring_setup io_uring_enter io_uring_register open_tree move_mount fsopen fsconfig fsmount " +
        "fspick pidfd_open clone3 close_range openat2 pidfd_getfd faccessat2 process_madvise epoll_pwait2 mount_setattr " +
        "quotactl_fd landlock_create_ruleset landlock_add_rule landlock_restrict_self memfd_secret process_mrelease " +
        "futex_waitv set_mempolicy_home_node cachestat fchmodat2 map_shadow_stack futex_wake futex_wait futex_requeue " +
        "statmount listmount lsm_get_self_attr lsm_set_self_attr lsm_list_modules mseal setxattrat getxattrat " +
        "listxattrat removexattrat";

    static readonly string[] ArchNames = ["x86_64", "x32", "x86", "aarch64", "riscv64"];

    // x86's socketcall(2) and ipc(2) multiplex these.
    static readonly HashSet<string> SocketCalls =
    [
        "socket", "bind", "connect", "listen", "accept", "getsockname", "getpeername", "socketpair", "send", "recv",
        "sendto", "recvfrom", "shutdown", "setsockopt", "getsockopt", "sendmsg", "recvmsg", "accept4", "recvmmsg", "sendmmsg",
    ];

    static readonly HashSet<string> IpcCalls =
    [
        "semop", "semget", "semctl", "semtimedop", "msgsnd", "msgrcv", "msgget", "msgctl", "shmat", "shmdt", "shmget", "shmctl",
    ];

    static Dictionary<string, int> Table(string arch)
    {
        var table = new Dictionary<string, int>(StringComparer.Ordinal);
        void Add(string names, int start, int offset = 0, HashSet<string> skip = null)
        {
            var n = start;
            foreach (var name in names.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (name != "-" && (skip == null || !skip.Contains(name))) table.TryAdd(name, n + offset);
                n++;
            }
        }
        switch (arch)
        {
            case "x86_64":
                Add(X86_64, 0);
                Add(Common424, 424);
                break;
            case "x32":
                Add(X32Specific, 512, X32Bit);
                Add(X86_64, 0, X32Bit, new HashSet<string>(Only64.Split(' ')));
                Add(Common424, 424, X32Bit);
                break;
            case "x86":
                Add(X86, 0);
                Add(Common424, 424);
                break;
            case "aarch64":
                Add(Generic, 0);
                Add(Common424, 424);
                break;
            case "riscv64":
                // riscv64 has no plain renameat, and its own calls in the per-architecture range.
                Add(Generic, 0, 0, ["renameat"]);
                table["riscv_hwprobe"] = 258;
                table["riscv_flush_icache"] = 259;
                Add(Common424, 424);
                break;
        }
        return table;
    }

    static string NativeArch() => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.Arm64 => "aarch64",
        Architecture.X86 => "x86",
        _ => "x86_64",
    };

    static int Run(string[] args)
    {
        string arch = NativeArch(), what = null;
        var translate = false;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "-h" or "--help")
            {
                Console.WriteLine("usage: scmp_sys_resolver [-h] [-a <arch>] [-t] <name>|<number>\n\n" +
                                  "Resolve a Linux system call name to its number, or a number to its name.\n" +
                                  $"Architectures: {string.Join(", ", ArchNames)} (default {NativeArch()}).");
                return 0;
            }
            if (a == "-t") translate = true;
            else if (a == "-a" || a.StartsWith("-a"))
            {
                var value = a == "-a" ? (i + 1 < args.Length ? args[++i] : null) : a[2..];
                if (value == null) throw new UsageException("option -a needs an architecture");
                // libseccomp's aliases for the same tables.
                arch = value switch { "amd64" => "x86_64", "i386" or "i686" => "x86", "arm64" => "aarch64", _ => value };
                if (!ArchNames.Contains(arch)) throw new UsageException($"unknown architecture '{value}'");
            }
            else if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"unknown option {a}");
            else if (what == null) what = a;
            else throw new UsageException("only one name or number may be given");
        }
        if (what == null) throw new UsageException("usage: scmp_sys_resolver [-h] [-a <arch>] [-t] <name>|<number>");

        var table = Table(arch);
        if (what[0] is >= '0' and <= '9')
        {
            var number = int.TryParse(what, out var n) ? n : -1;
            var name = table.FirstOrDefault(kv => kv.Value == number).Key;
            Console.WriteLine(name ?? "UNKNOWN");
            return 0;
        }
        // -t rewrites calls that x86 multiplexes to the multiplexer, as libseccomp's filters do.
        if (translate && arch == "x86")
        {
            if (SocketCalls.Contains(what)) { Console.WriteLine(102); return 0; }
            if (IpcCalls.Contains(what)) { Console.WriteLine(117); return 0; }
        }
        Console.WriteLine(table.TryGetValue(what, out var num) ? num : -1);
        return 0;
    }
}
