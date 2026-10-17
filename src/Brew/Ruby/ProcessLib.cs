using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using static Brew.Ruby.Win32Process;

namespace Brew.Ruby;

// `exec` inside a fork child: the program is running, so the child's Ruby code stops here (never rescued).
public sealed class ForkExecSignal : Exception { }

// Ruby's process API for `test do` blocks, on Windows: spawn/exec/fork, Process.wait/kill/detach, Process::Status,
// IO.pipe, IO.popen, Open3, $stdout.reopen, Signal. PTY.spawn is in Pty.cs; the Win32 side in Win32Process.cs.
//
// fork: Windows can't fork. `fork { ... }` runs the block on a Ruby thread (RubyThreads.cs) that has its own copy of
// the process state (ENV, cwd, $stdout/$stderr), so it behaves like the child for what tests do with it: start a
// server with `exec` (which spawns the program and ends the child's Ruby code) or `system`, or serve a socket from
// Ruby. fork returns the exec'd program's pid when the child got that far before it first blocked, else a stand-in
// pid; Process.kill/wait/detach take either. Not emulated: the child's own copy of Ruby objects (it shares the
// parent's heap; changes it makes to shared objects are visible to the parent), and `fork` without a block.
public static partial class FormulaLoader
{
    static partial class TestSupport
    {
        // ---- children ----

        // A child of this Ruby "process": a real one, or a fork child (a Ruby thread, maybe with an exec'd program).
        abstract class Child
        {
            public long Pid;
            public bool Reaped, Detached;
            public object StatusObj;
            public abstract bool Wait(int ms);   // true once it has exited
            public abstract void Kill(Interp i, int sig);
            public abstract (int? Code, int? Sig) Result();
        }

        sealed class OsChild : Child
        {
            public ChildProcess P;
            public override bool Wait(int ms) => P.WaitForExit(ms);
            public override void Kill(Interp i, int sig)
            {
                if (sig == 2 && P.OnSignal != null) P.OnSignal();
                else P.Terminate(sig);
            }
            // A console program ended by ^C exits with STATUS_CONTROL_C_EXIT: Ruby would see SIGINT.
            public override (int? Code, int? Sig) Result() =>
                P.TermSig is { } s ? (null, s) : P.ExitCode == unchecked((int)0xC000013A) ? (null, 2) : (P.ExitCode, null);
        }

        sealed class ForkChild : Child
        {
            public Interp.RubyThread Thread;
            public volatile ChildProcess Exec;
            public readonly ManualResetEventSlim Settled = new(), Done = new();
            public int Code;
            public int? Sig;
            public readonly List<ChildProcess> Spawned = new();
            public readonly List<Process> NetSpawned = new();
            public readonly List<Action> Owned = new();   // handles its $stdout.reopen opened: closed when it ends
            public override bool Wait(int ms)
            {
                var start = Environment.TickCount64;
                if (!Done.Wait(ms)) return false;
                if (Sig != null || Exec == null) return true;
                var left = ms < 0 ? -1 : (int)Math.Max(0, ms - (Environment.TickCount64 - start));
                return Exec.WaitForExit(left);
            }
            public override void Kill(Interp i, int sig)
            {
                if (Done.IsSet && (Exec == null || Exec.HasExited)) return;
                Sig ??= sig;
                Thread.Killed = true;
                Exec?.Terminate(sig);
                lock (Spawned) foreach (var p in Spawned) p.Terminate(sig);
                lock (NetSpawned)
                    foreach (var p in NetSpawned)
                        try { if (!p.HasExited) p.Kill(true); } catch (Exception e) when (e is InvalidOperationException or Win32Exception) { }
                Done.Set();
            }
            public override (int? Code, int? Sig) Result() =>
                Sig is { } s ? (null, s) : Exec is { } e ? (e.TermSig is { } t ? (null, t) : (e.ExitCode, null)) : (Code, null);
        }

        static readonly object childLock = new();
        static readonly Dictionary<long, Child> children = new();
        static readonly HashSet<long> reapedPids = new();
        static long nextForkPid = 0x7FFF0000;

        // Open pipe ends, PTYs and reopened files of the current test, closed when it ends.
        static readonly List<Action> sessionCleanup = new();

        static void Register(Child c) { lock (childLock) children[c.Pid] = c; }

        static void AtSessionEnd(Action a) { lock (sessionCleanup) sessionCleanup.Add(a); }

        // ---- the test session (Formula#run_test) ----

        sealed class Session { public TextWriter Out, Err; }

        static object BeginProcessSession(Interp i)
        {
            Win32Process.BeginSession();
            return new Session { Out = i.Out, Err = i.Err };
        }

        // Whatever the test started is killed, so a hung daemon never outlives `brew test`.
        static void EndProcessSession(Interp i, object token)
        {
            List<Child> all;
            lock (childLock) { all = children.Values.ToList(); children.Clear(); reapedPids.Clear(); }
            foreach (var c in all)
                try { c.Kill(i, 9); } catch (Exception e) when (e is Win32Exception or InvalidOperationException) { }
            Win32Process.EndSession();
            try { i.Out.Flush(); i.Err.Flush(); } catch (Exception e) when (e is IOException or ObjectDisposedException) { }
            if (token is Session s) { i.Out = s.Out; i.Err = s.Err; }
            i.Stdio = null;
            List<Action> cleanup;
            lock (sessionCleanup) { cleanup = sessionCleanup.ToList(); sessionCleanup.Clear(); }
            cleanup.Reverse();   // a PTY's console closes while its reader still drains it
            foreach (var a in cleanup)
                try { a(); } catch (Exception e) when (e is IOException or Win32Exception or ObjectDisposedException) { }
            i.EndRubyThreads();
        }

        // ---- where a child's stdio goes ----

        // This (forked) process's fds 0-2 after $stdout.reopen; children it starts inherit them. Copied on write,
        // since a fork child starts out sharing its parent's.
        sealed class StdioState
        {
            public readonly IntPtr[] Handles = new IntPtr[3];
            public readonly string[] Paths = new string[3];
            public StdioState Copy()
            {
                var c = new StdioState();
                Array.Copy(Handles, c.Handles, 3);
                Array.Copy(Paths, c.Paths, 3);
                return c;
            }
        }

        static IntPtr StdFor(Interp i, int fd) =>
            (i.Stdio as StdioState)?.Handles[fd] is { } h && h != IntPtr.Zero ? h : Win32Process.StdHandle(fd);

        static bool Reopened(Interp i, int fd) => (i.Stdio as StdioState)?.Handles[fd] is { } h && h != IntPtr.Zero;

        // A handle opened for a redirect or reopen; closed when the test ends (or once the child has it).
        static IntPtr Open(Interp i, string path, string mode, List<IntPtr> toClose)
        {
            try
            {
                var h = OpenFile(path == "NUL" || path == "/dev/null" ? path : i.Abs(path), mode);
                toClose?.Add(h);
                return h;
            }
            catch (Win32Exception e)
            {
                throw i.Error(e.NativeErrorCode == 5 ? i.EACCES : i.ENOENT, $"No such file or directory @ rb_sysopen - {path}");
            }
        }

        static string ModeOf(Interp i, object m) => m switch
        {
            null => null,
            long f => (f & 1024) != 0 ? "a" : (f & 3) != 0 ? ((f & 2) != 0 ? "w+" : "w") : "r",
            _ => i.ToS(m),
        };

        // fds a spawn option key names: :in/:out/:err, 0-2, $stdin/$stdout/$stderr, or an array of them.
        static int[] Fds(Interp i, object key)
        {
            switch (key)
            {
                case RSymbol { Name: "in" }: return new[] { 0 };
                case RSymbol { Name: "out" }: return new[] { 1 };
                case RSymbol { Name: "err" }: return new[] { 2 };
                case long n when n is >= 0 and <= 2: return new[] { (int)n };
                case RArray arr:
                    var all = arr.L.Select(x => Fds(i, x)).ToList();
                    return all.Any(f => f == null) ? null : all.SelectMany(f => f).ToArray();
                case RObject o when ReferenceEquals(o, i.Globals.GetValueOrDefault("$stdout")): return new[] { 1 };
                case RObject o when ReferenceEquals(o, i.Globals.GetValueOrDefault("$stderr")): return new[] { 2 };
                case RObject { Host: PipeIO }: return Array.Empty<int>();   // another fd number: Windows has only 0-2
                default: return null;
            }
        }

        // The handles a child gets for fds 0-2: the defaults, then spawn's redirect options, then `forced` (Open3's
        // and IO.popen's pipes); [:child, :out] refers to the result.
        static IntPtr[] Redirects(Interp i, RHash opts, IntPtr[] defaults, List<IntPtr> toClose, IntPtr[] forced)
        {
            var h = (IntPtr[])defaults.Clone();
            void Force()
            {
                if (forced != null) for (int fd = 0; fd < 3; fd++) if (forced[fd] != IntPtr.Zero) h[fd] = forced[fd];
            }
            if (opts == null) { Force(); return h; }
            var childRefs = new List<(int Fd, int To)>();
            foreach (var kv in opts.D)
            {
                var fds = Fds(i, kv.Key);
                if (fds == null || fds.Length == 0) continue;
                if (kv.Value is RArray { L: [RSymbol { Name: "child" }, var to] })
                {
                    foreach (var fd in fds) childRefs.Add((fd, Fds(i, to)?.FirstOrDefault() ?? 1));
                    continue;
                }
                // One handle for all the fds of a key: [:out, :err] => "log" opens the file once, like Ruby.
                var reading = fds[0] == 0;
                IntPtr handle;
                switch (kv.Value)
                {
                    case RArray arr when arr.L.Count > 0:
                        handle = Open(i, i.PathStr(arr.L[0]), ModeOf(i, arr.L.ElementAtOrDefault(1)) ?? (reading ? "r" : "w"), toClose);
                        break;
                    case RSymbol { Name: "close" }:
                        handle = IntPtr.Zero;
                        break;
                    case RSymbol or long:
                        handle = Fds(i, kv.Value) is [var src] ? StdFor(i, src) : h[fds[0]];
                        break;
                    case RObject { Host: PipeIO p }:
                        handle = reading ? (p.ReadHandle != IntPtr.Zero ? p.ReadHandle : p.WriteHandle) : (p.WriteHandle != IntPtr.Zero ? p.WriteHandle : p.ReadHandle);
                        break;
                    case RObject o when Fds(i, o) is [var std]:
                        handle = StdFor(i, std);
                        break;
                    case RObject o when i.IsA(o, i.FileClass):
                        // Our File objects buffer until close: write that out, then the child appends.
                        var path = i.ToS(i.Invoke(o, "path", None, null));
                        i.Invoke(o, "close", None, null);
                        handle = Open(i, path, reading ? "r" : "a", toClose);
                        break;
                    default:
                        handle = Open(i, i.PathStr(kv.Value), reading ? "r" : "w", toClose);
                        break;
                }
                foreach (var fd in fds) h[fd] = handle;
            }
            Force();
            foreach (var (fd, to) in childRefs) h[fd] = h[to];
            return h;
        }

        // ---- command lines ----

        // Kernel#spawn-style arguments: [env,] command... [,options]
        static (RHash Env, List<object> Cmd, RHash Opts) SplitArgs(Interp i, object[] a)
        {
            var args = a.ToList();
            RHash env = args.Count > 1 && args[0] is RHash e ? e : null;
            if (env != null) args.RemoveAt(0);
            RHash opts = args.Count > 1 && args[^1] is RHash o ? o : null;
            if (opts != null) args.RemoveAt(args.Count - 1);
            if (args.Count == 0) throw i.ArgCount(a, 1, -1);
            return (env, args, opts);
        }

        // One string is a shell line (resolved the way `system` does); several are a program and its arguments.
        static ProcessStartInfo CommandPsi(Interp i, List<object> cmd)
        {
            if (cmd.Count == 1 && cmd[0] is RArray { L: [var prog, _] })
                return Direct(i, Resolve(i, i.PathStr(prog)), new List<string>());
            var parts = cmd.SelectMany(x => x is RArray arr ? arr.L : new List<object> { x }).Select(i.ToS).ToList();
            if (parts.Count > 1) return Direct(i, Resolve(i, parts[0]), parts.Skip(1).ToList());
            var line = LinePsi(i, parts[0]);
            // A plain command line that names a #! script ("#{bin}/start.sh") still needs the shell.
            return line.ArgumentList.Count > 0 || line.Arguments.Length == 0 ? Direct(i, line.FileName, line.ArgumentList.ToList()) : line;
        }

        // A script with a #! line runs through the POSIX shell, which honours it.
        static ProcessStartInfo Direct(Interp i, string program, List<string> args)
        {
            var ext = Path.GetExtension(program).ToLowerInvariant();
            if (ext is not (".exe" or ".com" or ".cmd" or ".bat") && File.Exists(program) && HasShebang(program)
                && PosixShell.FindShell(k => i.Env.GetValueOrDefault(k)) is { } bash)
            {
                var sh = new ProcessStartInfo(bash);
                foreach (var x in new[] { "-c", "exec \"$0\" \"$@\"", program.Replace('\\', '/') }.Concat(args)) sh.ArgumentList.Add(x);
                return sh;
            }
            var psi = new ProcessStartInfo(program);
            foreach (var x in args) psi.ArgumentList.Add(x);
            return psi;
        }

        static bool HasShebang(string file)
        {
            try
            {
                using var s = File.OpenRead(file);
                return s.ReadByte() == '#' && s.ReadByte() == '!';
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
        }

        static string CommandLineOf(ProcessStartInfo psi) =>
            psi.ArgumentList.Count > 0 || psi.Arguments.Length == 0
                ? string.Join(" ", psi.ArgumentList.Prepend(psi.FileName).Select(Win32Process.Quote))
                : Win32Process.Quote(psi.FileName) + " " + psi.Arguments;

        // Starts a child: fds 0-2 from `std` (null entries: this process's), the spawn options applied on top.
        static ChildProcess Start(Interp i, RHash env, List<object> cmd, RHash opts, IntPtr[] std = null, PseudoConsole pty = null)
        {
            var psi = CommandPsi(i, cmd);
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (opts == null || !(opts.D.TryGetValue(Interp.Sym("unsetenv_others"), out var uo) && Interp.Truthy(uo)))
                foreach (var kv in i.Env) vars[kv.Key] = kv.Value;
            if (psi.FileName.EndsWith("bash.exe", StringComparison.OrdinalIgnoreCase))
                foreach (var kv in ShellEnv) vars[kv.Key] = kv.Value;
            if (env != null)
                foreach (var kv in env.D)
                    if (kv.Value == null) vars.Remove(i.StrOrSym(kv.Key)); else vars[i.StrOrSym(kv.Key)] = i.ToS(kv.Value);
            var cwd = i.Cwd;
            if (opts != null && opts.D.TryGetValue(Interp.Sym("chdir"), out var cd) && cd != null) cwd = i.Abs(i.PathStr(cd));
            var group = opts != null && (opts.D.TryGetValue(Interp.Sym("pgroup"), out var pg) && Interp.Truthy(pg)
                || opts.D.TryGetValue(Interp.Sym("new_pgroup"), out var npg) && Interp.Truthy(npg));
            var defaults = new IntPtr[3];   // a PTY child's unredirected fds are its console
            if (pty == null) for (int fd = 0; fd < 3; fd++) defaults[fd] = StdFor(i, fd);
            var toClose = new List<IntPtr>();
            try
            {
                var h = Redirects(i, opts, defaults, toClose, std);
                if (!Directory.Exists(cwd)) throw i.Error(i.ENOENT, $"No such file or directory @ dir_s_chdir - {cwd}");
                i.Out.Flush();
                i.Err.Flush();
                var child = Spawn(new Spec { CommandLine = CommandLineOf(psi), Cwd = cwd, Env = vars, StdIn = h[0], StdOut = h[1], StdErr = h[2], Pty = pty, NewProcessGroup = group });
                if (i.CurrentRubyThread?.Fork is ForkChild fc) lock (fc.Spawned) fc.Spawned.Add(child);
                return child;
            }
            catch (Win32Exception)
            {
                throw i.Error(i.ENOENT, "No such file or directory - " + (cmd[0] is RArray { L: [var p, _] } ? i.ToS(p) : i.ToS(cmd[0])));
            }
            finally
            {
                foreach (var x in toClose) CloseHandle(x);
            }
        }

        static long SpawnRegistered(Interp i, object[] a)
        {
            var (env, cmd, opts) = SplitArgs(i, a);
            var p = Start(i, env, cmd, opts);
            Register(new OsChild { Pid = p.Pid, P = p });
            return p.Pid;
        }

        // ---- Process::Status ----

        sealed class ExitInfo { public int Pid; public int? Code, Sig; }

        static (int Pid, int? Code, int? Sig) St(object self) => ((RObject)self).Host switch
        {
            ExitInfo e => (e.Pid, e.Code, e.Sig),
            ValueTuple<int, int> t => (t.Item2, t.Item1, null),
            _ => (0, 0, null),
        };

        static readonly (string Name, int Number)[] Signals =
        {
            ("EXIT", 0), ("HUP", 1), ("INT", 2), ("QUIT", 3), ("ILL", 4), ("TRAP", 5), ("ABRT", 6), ("IOT", 6), ("BUS", 7),
            ("FPE", 8), ("KILL", 9), ("USR1", 10), ("SEGV", 11), ("USR2", 12), ("PIPE", 13), ("ALRM", 14), ("TERM", 15),
            ("CHLD", 17), ("CLD", 17), ("CONT", 18), ("STOP", 19), ("TSTP", 20), ("TTIN", 21), ("TTOU", 22), ("URG", 23),
            ("XCPU", 24), ("XFSZ", 25), ("VTALRM", 26), ("PROF", 27), ("WINCH", 28), ("IO", 29), ("POLL", 29), ("PWR", 30), ("SYS", 31),
        };

        static int SignalNumber(Interp i, object sig)
        {
            if (sig is long n)
            {
                if (n < 0 || n > 64) throw i.ArgError($"invalid signal number ({n})");
                return (int)n;
            }
            var name = i.StrOrSym(sig);
            var bare = name.StartsWith("SIG") ? name[3..] : name;
            foreach (var (nm, num) in Signals) if (nm == bare) return num;
            throw i.ArgError($"unsupported signal 'SIG{bare}'");
        }

        static string SignalName(int n) => Signals.FirstOrDefault(s => s.Number == n && s.Name != "IOT" && s.Name != "CLD" && s.Name != "POLL").Name;

        static RObject NewStatus(Interp i, long pid, int? code, int? sig) =>
            new() { Class = i.ProcessStatusClass(), Host = new ExitInfo { Pid = (int)pid, Code = code, Sig = sig } };

        static void SetChildStatus(Interp i, object status)
        {
            i.Globals["$?"] = status;
            i.Globals["$CHILD_STATUS"] = status;
        }

        static RObject StatusOf(Interp i, Child c)
        {
            if (c.StatusObj is RObject s) return s;
            var (code, sig) = c.Result();
            var st = NewStatus(i, c.Pid, code, sig);
            c.StatusObj = st;
            return st;
        }

        static void DefineStatus(Interp it)
        {
            var c = it.ProcessStatusClass();
            it.Def(c, "exitstatus", (i, self, a, b) => St(self).Code is { } n ? (long)n : null);
            it.Def(c, "to_i", (i, self, a, b) => (long)(St(self).Code ?? St(self).Sig ?? 0));
            it.Def(c, "pid", (i, self, a, b) => (long)St(self).Pid);
            it.Def(c, "success?", (i, self, a, b) => St(self).Code is { } n ? n == 0 : null);
            it.Def(c, "exited?", (i, self, a, b) => St(self).Sig == null);
            it.Def(c, "signaled?", (i, self, a, b) => St(self).Sig != null);
            it.Def(c, "termsig", (i, self, a, b) => St(self).Sig is { } n ? (long)n : null);
            it.Def(c, "stopped? coredump?", (i, self, a, b) => false);
            it.Def(c, "stopsig", (i, self, a, b) => null);
            it.Def(c, "==", (i, self, a, b) => a[0] is long l ? l == (St(self).Code ?? St(self).Sig) : ReferenceEquals(self, a[0]));
            string Text(object self)
            {
                var (pid, code, sig) = St(self);
                return sig is { } s ? $"pid {pid} SIG{SignalName(s)} (signal {s})" : $"pid {pid} exit {code}";
            }
            it.Def(c, "to_s", (i, self, a, b) => Interp.S(Text(self)));
            it.Def(c, "inspect", (i, self, a, b) => Interp.S($"#<Process::Status: {Text(self)}>"));
            // Process::Status.wait(pid = -1, flags = 0): like Process.wait2, without setting $?.
            it.DefS(c, "wait", (i, self, a, b) =>
            {
                var saved = (i.Globals.GetValueOrDefault("$?"), i.Globals.GetValueOrDefault("$CHILD_STATUS"));
                var r = WaitPid(i, a);
                var st = r == null ? null : i.Globals.GetValueOrDefault("$?");
                i.Globals["$?"] = saved.Item1;
                i.Globals["$CHILD_STATUS"] = saved.Item2;
                return st;
            });
        }

        // ---- Process.wait ----

        static Child FindChild(Interp i, long pid, bool reaping)
        {
            lock (childLock)
            {
                if (pid > 0) return children.TryGetValue(pid, out var c) && !(reaping && c.Detached) ? c : null;
                return children.Values.FirstOrDefault(c => !c.Reaped && !c.Detached);
            }
        }

        static RubyError NoChild(Interp i) => i.Error(Errno(i, "ECHILD"), "No child processes");

        static RClass Errno(Interp i, string name) =>
            i.ErrnoModule.Consts.GetValueOrDefault(name) as RClass ?? i.DefineClass(name, i.SystemCallError, i.ErrnoModule);

        // Process.wait(pid = -1, flags = 0): the pid once it has exited ($? set), or nil with WNOHANG while it runs.
        static object WaitPid(Interp i, object[] a)
        {
            var pid = a.Length > 0 && a[0] != null ? i.Int(a[0]) : -1;
            var nohang = a.Length > 1 && a[1] is long f && (f & 1) != 0;
            Child c;
            if (pid > 0 || pid < -1)
            {
                c = FindChild(i, Math.Abs(pid), true) ?? throw NoChild(i);
                if (!(nohang ? c.Wait(0) : i.WaitFor(c.Wait))) return null;
            }
            else
            {
                if (FindChild(i, -1, true) == null) throw NoChild(i);
                c = null;
                bool Any(int ms)
                {
                    List<Child> live;
                    lock (childLock) live = children.Values.Where(x => !x.Reaped && !x.Detached).ToList();
                    c = live.FirstOrDefault(x => x.Wait(0));
                    if (c != null || ms == 0) return c != null;
                    Thread.Sleep(Math.Min(ms, 20));
                    return false;
                }
                if (!(nohang ? Any(0) : i.WaitFor(Any))) return null;
            }
            Reap(c);
            SetChildStatus(i, StatusOf(i, c));
            return c.Pid;
        }

        static void Reap(Child c)
        {
            c.Reaped = true;
            lock (childLock) reapedPids.Add(c.Pid);
            lock (childLock) if (children.TryGetValue(c.Pid, out var x) && x == c) children.Remove(c.Pid);
        }

        // ---- Thread, and the waiter threads of Open3 / Process.detach ----

        sealed class ThreadInfo
        {
            public Interp.RubyThread T;
            public readonly ManualResetEventSlim Done = new();
            public object Value;
            public RubyError Error;
            public readonly RHash Locals = new();
        }

        static RClass threadClass, waiterClass;
        static readonly ThreadLocal<RObject> currentThreadObject = new();

        static RObject NewWaiter(Interp i, Child c) => new() { Class = waiterClass, Host = c };

        static void DefineThreads(Interp it)
        {
            threadClass = it.ObjectClass.Consts.GetValueOrDefault("Thread") as RClass ?? it.DefineClass("Thread");
            var P = (RClass)it.ObjectClass.Consts["Process"];
            waiterClass = P.Consts.GetValueOrDefault("Waiter") as RClass ?? it.DefineClass("Waiter", threadClass, P);
            void DefMissing(RClass c, string names, Native fn)
            {
                foreach (var n in names.Split(' ')) if (!c.Methods.ContainsKey(n)) it.Def(c, n, fn);
            }
            var TS = it.SingletonOf(threadClass);
            DefMissing(TS, "new start fork", (i, self, a, b) =>
            {
                if (b == null) throw i.Error(i.ArgumentError, "must be called with a block");
                var info = new ThreadInfo();
                info.T = i.StartRubyThread(t =>
                {
                    try { info.Value = i.CallProc(b, a); }
                    catch (RubyError e) when (!i.IsA(e.Exc, i.SystemExit))
                    {
                        info.Error = e;
                        i.Err.WriteLine($"#<Thread:0x{info.GetHashCode():x}> terminated with exception (report_on_exception is true):");
                        i.Err.WriteLine($"{e.Message} ({e.ClassName})");
                    }
                    catch (RubyError) { }
                    catch (Exception e) when (e is BreakSignal or NextSignal or ThrowSignal or ReturnSignal or ForkExecSignal) { }
                    finally { info.Done.Set(); }
                }, fork: false);
                return new RObject { Class = threadClass, Host = info };
            });
            DefMissing(TS, "current main", (i, self, a, b) => currentThreadObject.Value ??= new RObject { Class = threadClass, Host = new ThreadInfo() });
            DefMissing(TS, "pass", (i, self, a, b) => { i.Blocking(() => Thread.Sleep(1)); return null; });

            ThreadInfo TI(object self) => ((RObject)self).Host as ThreadInfo;
            bool Finished(object self, int ms) => ((RObject)self).Host switch
            {
                ThreadInfo t => t.T == null || t.Done.Wait(ms),
                Child c => c.Wait(ms),
                _ => true,
            };
            object Join(Interp i, object self, object[] a)
            {
                var limit = a.Length > 0 && a[0] != null ? i.Num(a[0]) : -1;
                if (!i.WaitFor(ms => Finished(self, ms), limit)) return null;
                if (((RObject)self).Host is Child c) { Reap(c); StatusOf(i, c); }
                if (TI(self)?.Error is { } err) throw err;
                return self;
            }
            DefMissing(threadClass, "join", (i, self, a, b) => Join(i, self, a));
            DefMissing(threadClass, "value", (i, self, a, b) =>
            {
                Join(i, self, None);
                return ((RObject)self).Host switch { Child c => StatusOf(i, c), ThreadInfo t => t.Value, _ => null };
            });
            DefMissing(threadClass, "alive?", (i, self, a, b) => !Finished(self, 0));
            DefMissing(threadClass, "status", (i, self, a, b) => Finished(self, 0) ? (TI(self)?.Error != null ? null : false) : Interp.S("run"));
            DefMissing(threadClass, "pid", (i, self, a, b) => ((RObject)self).Host is Child c ? c.Pid : null);
            DefMissing(threadClass, "[]", (i, self, a, b) =>
                ((RObject)self).Host is Child c && a[0] is RSymbol { Name: "pid" } ? c.Pid
                : TI(self) is { } t && t.Locals.D.TryGetValue(a[0] is RString s ? Interp.Sym(s.V) : a[0], out var v) ? v : null);
            DefMissing(threadClass, "[]=", (i, self, a, b) =>
            {
                if (TI(self) is { } t) t.Locals.D[a[0] is RString s ? Interp.Sym(s.V) : a[0]] = a[1];
                return a[1];
            });
            DefMissing(threadClass, "kill terminate exit", (i, self, a, b) =>
            {
                if (TI(self)?.T is { } t) { t.Killed = true; TI(self).Done.Set(); }
                return self;
            });
            DefMissing(threadClass, "report_on_exception= abort_on_exception= name= priority=", (i, self, a, b) => a[0]);
            DefMissing(threadClass, "wakeup run", (i, self, a, b) => self);
        }

        // ---- fork / exec ----

        static object Fork(Interp i, object self, RProc b)
        {
            if (b == null) throw i.Error(i.NotImplementedError, "fork() function is unimplemented on this machine (only `fork { ... }` is, on Windows)");
            var fc = new ForkChild { Pid = Interlocked.Add(ref nextForkPid, 4) };
            i.StartRubyThread(t => RunForkChild(i, fc, b), fork: true, setup: t =>
            {
                t.Fork = fc;
                t.Released = () => fc.Settled.Set();
                fc.Thread = t;
            });
            // The child runs first, until it execs, ends or blocks: then fork returns, with the program's pid if any.
            i.Blocking(() => fc.Settled.Wait());
            if (fc.Exec is { } e) fc.Pid = e.Pid;
            Register(fc);
            return fc.Pid;
        }

        static void RunForkChild(Interp i, ForkChild fc, RProc b)
        {
            int code = 0;
            try { i.CallProc(b, None); }
            catch (ForkExecSignal) { }
            catch (RubyError e) when (i.IsA(e.Exc, i.SystemExit)) { code = e.Exc.GetIvar("@status") is long s ? (int)s : 0; }
            catch (RubyError e)
            {
                i.Err.WriteLine($"{i.CurrentFile}:{i.CurLine}: {e.Message} ({e.ClassName})");
                code = 1;
            }
            catch (Exception e) when (e is BreakSignal or NextSignal or ThrowSignal or ReturnSignal or RetrySignal) { code = 1; }
            catch (Exception e) when (e is IOException or InvalidOperationException or Win32Exception)
            {
                i.Err.WriteLine($"{i.CurrentFile}:{i.CurLine}: {e.Message}");
                code = 1;
            }
            finally
            {
                try { i.Out.Flush(); i.Err.Flush(); } catch (Exception e) when (e is IOException or ObjectDisposedException) { }
                lock (fc.Owned) foreach (var close in fc.Owned) close();
                fc.Code = code;
                fc.Done.Set();
            }
        }

        // Kernel#exec: in a fork child the program takes over the child (fork's pid is its pid); anywhere else it
        // runs to the end and the Ruby side exits with its status, as if it had been replaced.
        static object Exec(Interp i, object[] a)
        {
            var (env, cmd, opts) = SplitArgs(i, a);
            var p = Start(i, env, cmd, opts);
            if (i.CurrentRubyThread?.Fork is ForkChild fc)
            {
                lock (fc.Spawned) fc.Spawned.Remove(p);
                fc.Exec = p;
                throw new ForkExecSignal();
            }
            i.WaitFor(p.WaitForExit);
            var exit = i.NewException(i.SystemExit, "exit");
            exit.SetIvar("@status", (long)(p.TermSig ?? p.ExitCode));
            throw new RubyError(exit);
        }

        // ---- Process.kill ----

        static object Kill(Interp i, object[] a)
        {
            if (a.Length < 2) throw i.ArgCount(a, 2, -1);
            var sig = SignalNumber(i, a[0]);
            foreach (var pidObj in a.Skip(1))
            {
                var pid = Math.Abs(i.Int(pidObj));   // a negative pid is its process group: the child's tree
                if (pid == Environment.ProcessId || pid == 0)
                {
                    if (sig == 0) continue;
                    throw i.Error(SignalExceptionClass(i, sig == 2 ? "Interrupt" : "SignalException"), sig == 2 ? "" : $"SIG{SignalName(sig)}");
                }
                // A reaped child's pid may already be another process's (Windows reuses them quickly).
                lock (childLock) if (reapedPids.Contains(pid) && !children.ContainsKey(pid)) throw Esrch(i);
                var c = FindChild(i, pid, false);
                if (c != null)
                {
                    if (c.Reaped || c.Wait(0)) { if (sig != 0 && c.Reaped) throw Esrch(i); continue; }
                    if (sig != 0) c.Kill(i, sig);
                    continue;
                }
                Process proc;
                try { proc = Process.GetProcessById((int)pid); }
                catch (ArgumentException) { throw Esrch(i); }
                using (proc)
                {
                    if (sig == 0) continue;
                    try { if (!proc.HasExited) proc.Kill(true); }
                    catch (Exception e) when (e is InvalidOperationException or Win32Exception) { throw i.Error(Errno(i, "EPERM"), "Operation not permitted"); }
                }
            }
            return (long)(a.Length - 1);
        }

        static RubyError Esrch(Interp i) => i.Error(Errno(i, "ESRCH"), "No such process");

        static RClass SignalExceptionClass(Interp i, string name)
        {
            var se = i.ObjectClass.Consts.GetValueOrDefault("SignalException") as RClass ?? i.DefineClass("SignalException", i.ExceptionClass);
            if (name == "SignalException") return se;
            return i.ObjectClass.Consts.GetValueOrDefault(name) as RClass ?? i.DefineClass(name, se);
        }

        // ---- the API ----

        static void DefineProcessApi(Interp it)
        {
            var K = it.KernelModule;
            var P = it.ConstGet(it.ObjectClass, "Process") as RClass ?? it.DefineModule("Process");
            var PS = it.SingletonOf(P);
            P.Consts["WNOHANG"] = 1L;
            P.Consts["WUNTRACED"] = 2L;
            P.Consts["CLOCK_REALTIME"] = 0L;
            P.Consts["CLOCK_MONOTONIC"] = 1L;
            P.Consts["CLOCK_PROCESS_CPUTIME_ID"] = 2L;
            P.Consts["CLOCK_THREAD_CPUTIME_ID"] = 3L;
            foreach (var name in new[] { "ECHILD", "ESRCH", "EPERM", "EIO", "EPIPE", "EAGAIN", "ENOTTY", "EINTR" }) Errno(it, name);
            DefineStatus(it);
            DefineThreads(it);
            DefinePipeIO(it);
            it.TrackSpawned = p =>
            {
                if (it.CurrentRubyThread?.Fork is ForkChild fc) lock (fc.NetSpawned) fc.NetSpawned.Add(p);
            };

            Native spawn = (i, self, a, b) => SpawnRegistered(i, a);
            it.Def(K, "spawn", spawn);
            it.Def(PS, "spawn", spawn);
            Native exec = (i, self, a, b) => Exec(i, a);
            it.Def(K, "exec", exec);
            it.Def(PS, "exec", exec);
            Native fork = (i, self, a, b) => Fork(i, self, b);
            it.Def(K, "fork", fork);
            it.Def(PS, "fork", fork);
            it.Def(PS, "kill", (i, self, a, b) => Kill(i, a));
            it.Def(PS, "wait waitpid", (i, self, a, b) => WaitPid(i, a));
            it.Def(PS, "wait2 waitpid2", (i, self, a, b) => WaitPid(i, a) is { } pid ? new RArray(new[] { pid, i.Globals["$?"] }) : null);
            it.Def(PS, "waitall", (i, self, a, b) =>
            {
                var res = new RArray();
                while (FindChild(i, -1, true) != null && WaitPid(i, None) is { } pid) res.L.Add(new RArray(new[] { pid, i.Globals["$?"] }));
                return res;
            });
            it.Def(PS, "detach", (i, self, a, b) =>
            {
                var c = FindChild(i, i.Int(a[0]), false) ?? throw NoChild(i);
                c.Detached = true;
                return NewWaiter(i, c);
            });
            it.Def(PS, "pid", (i, self, a, b) => (long)Environment.ProcessId);
            it.Def(PS, "getpgid", (i, self, a, b) => a.Length > 0 && a[0] is long p && p != 0 ? p : (long)Environment.ProcessId);
            it.Def(PS, "getpgrp setsid", (i, self, a, b) => (long)Environment.ProcessId);
            it.Def(PS, "setpgid setpgrp", (i, self, a, b) => 0L);
            foreach (var name in new[] { "uid", "gid", "euid", "egid" })
                if (!PS.Methods.ContainsKey(name)) it.Def(PS, name, (i, self, a, b) => 0L);
            it.Def(PS, "exit exit!", (i, self, a, b) => i.Invoke(i.Main, "exit", a, null, true));
            it.Def(PS, "clock_gettime", (i, self, a, b) =>
            {
                var id = a.Length > 0 ? i.Int(a[0]) : 0;
                double secs = id switch
                {
                    1 => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency,
                    2 or 3 => Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds,
                    _ => (DateTime.UtcNow - DateTime.UnixEpoch).Ticks / (double)TimeSpan.TicksPerSecond,
                };
                return (a.Length > 1 ? i.StrOrSym(a[1]) : "float_second") switch
                {
                    "float_second" => (object)secs,
                    "float_millisecond" => secs * 1e3,
                    "float_microsecond" => secs * 1e6,
                    "second" => (long)Math.Floor(secs),
                    "millisecond" => (long)Math.Floor(secs * 1e3),
                    "microsecond" => (long)Math.Floor(secs * 1e6),
                    "nanosecond" => (long)Math.Floor(secs * 1e9),
                    var u => throw i.ArgError($"unexpected unit: {u}"),
                };
            });

            // Signal: Windows delivers none of them to us; trap is accepted and does nothing.
            var sigModule = it.ObjectClass.Consts.GetValueOrDefault("Signal") as RClass ?? it.DefineModule("Signal");
            var SS = it.SingletonOf(sigModule);
            it.Def(SS, "list", (i, self, a, b) =>
            {
                var h = new RHash();
                foreach (var (name, num) in Signals) h.D[Interp.S(name)] = (long)num;
                return h;
            });
            it.Def(SS, "trap", (i, self, a, b) => Interp.S("DEFAULT"));
            it.Def(SS, "signame", (i, self, a, b) => SignalName((int)i.Int(a[0])) is { } n ? Interp.S(n) : null);
            SignalExceptionClass(it, "Interrupt");

            // $stdout.reopen / $stderr.reopen: Ruby-side output and the children this process starts go there.
            foreach (var (name, fd) in new[] { ("$stdout", 1), ("$stderr", 2) })
                if (it.Globals.GetValueOrDefault(name) is RObject io)
                    it.Def(it.SingletonOf(io), "reopen", (i, self, a, b) => Reopen(i, self, fd, a));

            DefineOpen3(it);
            DefinePty(it);
        }

        static object Reopen(Interp i, object self, int fd, object[] a)
        {
            if (a.Length == 0) throw i.ArgCount(a, 1, 2);
            var target = a[0];
            var mode = a.Length > 1 && a[1] != null ? ModeOf(i, a[1]) : "w";
            var st = (i.Stdio as StdioState)?.Copy() ?? new StdioState();
            IntPtr h;
            string path = null;
            switch (target)
            {
                case RObject o when Fds(i, o) is [var other]:
                    h = StdFor(i, other);
                    path = st.Paths[other];
                    break;
                case RObject { Host: PipeIO p }:
                    h = p.WriteHandle != IntPtr.Zero ? p.WriteHandle : p.ReadHandle;
                    break;
                default:
                    if (target is RObject f && i.IsA(f, i.FileClass))
                    {
                        path = i.Abs(i.ToS(i.Invoke(f, "path", None, null)));
                        i.Invoke(f, "close", None, null);
                        mode = "a";
                    }
                    else path = i.Abs(i.PathStr(target));
                    var same = Enumerable.Range(0, 3).FirstOrDefault(k => k != fd && st.Paths[k] != null
                        && string.Equals(Path.GetFullPath(st.Paths[k]), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase), -1);
                    if (same >= 0) h = st.Handles[same];
                    else
                    {
                        h = Open(i, path, mode.StartsWith('r') ? "r+" : mode, null);
                        var owned = h;
                        int closed = 0;
                        Action close = () => { if (Interlocked.Exchange(ref closed, 1) == 0) CloseHandle(owned); };
                        AtSessionEnd(close);
                        if (i.CurrentRubyThread?.Fork is ForkChild fc) lock (fc.Owned) fc.Owned.Add(close);
                    }
                    break;
            }
            st.Handles[fd] = h;
            st.Paths[fd] = path;
            i.Stdio = st;
            var writer = new StreamWriter(new HandleStream(h), new UTF8Encoding(false)) { AutoFlush = true };
            if (fd == 1) { i.Out.Flush(); i.Out = writer; }
            else { i.Err.Flush(); i.Err = writer; }
            return self;
        }

        // Writes straight to a handle (no position of its own), so a child sharing the handle and the Ruby side
        // append to the same file in turn instead of overwriting each other.
        sealed class HandleStream : Stream
        {
            readonly IntPtr h;
            public HandleStream(IntPtr handle) { h = handle; }
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count)
            {
                var chunk = offset == 0 ? buffer : buffer[offset..(offset + count)];
                int done = 0;
                while (done < count)
                {
                    var part = done == 0 ? chunk : chunk[done..count];
                    if (!WriteFile(h, part, count - done, out var n, IntPtr.Zero) || n == 0) throw new IOException("write failed", System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    done += n;
                }
            }
        }

        // ---- Open3 ----

        static void DefineOpen3(Interp it)
        {
            var O = it.ObjectClass.Consts.GetValueOrDefault("Open3") as RClass ?? it.DefineModule("Open3");
            var OS = it.SingletonOf(O);
            // kind 3: popen3 (stdin, stdout, stderr, thr); 2: popen2 (stdin, stdout, thr); 1: popen2e (stdin, stdout+stderr, thr)
            (RObject[] Ios, RObject Waiter) Popen(Interp i, object[] a, int kind)
            {
                var (env, cmd, opts) = SplitArgs(i, a);
                var (inR, inW) = Pipe();
                var (outR, outW) = Pipe();
                var (errR, errW) = kind == 3 ? Pipe() : (IntPtr.Zero, IntPtr.Zero);
                ChildProcess p;
                try { p = Start(i, env, cmd, opts, new[] { inR, outW, kind == 3 ? errW : kind == 1 ? outW : IntPtr.Zero }); }
                catch
                {
                    foreach (var h in new[] { inW, outR, errR }) if (h != IntPtr.Zero) CloseHandle(h);
                    throw;
                }
                finally
                {
                    foreach (var h in new[] { inR, outW, errW }) if (h != IntPtr.Zero) CloseHandle(h);
                }
                var child = new OsChild { Pid = p.Pid, P = p };
                Register(child);
                var ios = new List<RObject> { NewPipeIO(i, new PipeIO { WriteHandle = inW }), NewPipeIO(i, new PipeIO { ReadHandle = outR }, pump: true) };
                if (kind == 3) ios.Add(NewPipeIO(i, new PipeIO { ReadHandle = errR }, pump: true));
                return (ios.ToArray(), NewWaiter(i, child));
            }
            object Run(Interp i, object[] a, RProc b, int kind)
            {
                var (ios, waiter) = Popen(i, a, kind);
                var all = ios.Cast<object>().Append(waiter).ToArray();
                if (b == null) return new RArray(all);
                try { return i.CallProc(b, all); }
                finally
                {
                    foreach (var io in ios) ((PipeIO)io.Host).Close(i);
                    i.Invoke(waiter, "join", None, null);
                }
            }
            it.Def(OS, "popen3", (i, self, a, b) => Run(i, a, b, 3));
            it.Def(OS, "popen2", (i, self, a, b) => Run(i, a, b, 2));
            it.Def(OS, "popen2e", (i, self, a, b) => Run(i, a, b, 1));

            // capture3/capture2/capture2e(cmd..., stdin_data: "", binmode: false, chdir: ...)
            object Capture(Interp i, object[] a, int kind)
            {
                var args = a.ToList();
                string input = "";
                bool inputBinary = false;
                if (args.Count > 1 && args[^1] is RHash o)
                {
                    var rest = new RHash();
                    foreach (var kv in o.D)
                    {
                        if (kv.Key is RSymbol { Name: "stdin_data" }) { input = kv.Value == null ? "" : i.ToS(kv.Value); inputBinary = kv.Value is RString { Binary: true }; }
                        else if (kv.Key is not RSymbol { Name: "binmode" }) rest.D[kv.Key] = kv.Value;
                    }
                    args[^1] = rest;
                    if (rest.D.Count == 0) args.RemoveAt(args.Count - 1);
                }
                var (ios, waiter) = Popen(i, args.ToArray(), kind);
                var stdin = (PipeIO)ios[0].Host;
                var bytes = inputBinary ? Encoding.Latin1.GetBytes(input) : RString.Utf8(input);
                var writer = Task.Run(() =>
                {
                    try { stdin.WriteBytes(bytes); } catch (IOException) { }
                    stdin.CloseWrite();
                });
                var outs = ios.Skip(1).Select(x => (PipeIO)x.Host).ToList();
                foreach (var x in outs) x.WaitEof(i);
                i.Blocking(() => writer.Wait());
                var res = outs.Select(x => (object)x.TakeAllText()).ToList();
                foreach (var x in outs) x.Close(i);
                res.Add(i.Invoke(waiter, "value", None, null));
                return new RArray(res);
            }
            it.Def(OS, "capture3", (i, self, a, b) => Capture(i, a, 3));
            it.Def(OS, "capture2", (i, self, a, b) => Capture(i, a, 2));
            it.Def(OS, "capture2e", (i, self, a, b) => Capture(i, a, 1));
        }
    }
}
