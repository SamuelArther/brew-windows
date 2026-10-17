namespace Brew.Ruby;

// Ruby threads: Thread.new, Open3's waiter threads and the child of `fork { ... }` (see ProcessLib.cs).
// The interpreter isn't thread-safe, so like MRI's GVL only the thread holding the global lock runs Ruby code;
// it lets go around blocking waits (sleep, Process.wait, pipe and PTY reads, system) and, when another thread
// is waiting, every 50ms at a method call. Each thread keeps its own call state ($?, $!, Timeout deadlines);
// a fork child also keeps its own process state (ENV, cwd, $stdout/$stderr and where children's stdio goes).
public sealed partial class Interp
{
    // What a fork child copies from its parent and then keeps to itself.
    internal sealed class ProcessState
    {
        public string Cwd;
        public Dictionary<string, string> Env;
        public TextWriter Out, Err;
        public object Stdio;   // ProcessLib's redirected std handles, or null for this process's own
    }

    internal sealed class RubyThread
    {
        public ProcessState Proc;
        public Frame Caller;
        public int CurLine, Depth, Generation;
        public string CurrentFile;
        public List<(DateTime Deadline, RClass Error)> Timeouts = new();
        public object Status, ChildStatus, LastError, LastMatch;
        public volatile bool Killed;   // never runs Ruby code again (its fork child was killed)
        public Action Released;        // called each time it lets go of the lock
        public object Fork;            // ProcessLib's fork child this thread is, if any
    }

    readonly object gvlSync = new();
    RubyThread gvlOwner;
    volatile int gvlWaiting;
    volatile bool threaded;
    int gvlGeneration;
    long sliceStart;
    readonly ThreadLocal<RubyThread> currentThread = new();
    internal object Stdio;   // ProcessLib's redirect state for the running (forked) process

    internal RubyThread CurrentRubyThread => threaded ? currentThread.Value : null;

    // The first extra thread makes the running thread the lock's owner.
    RubyThread EnsureThreaded()
    {
        if (threaded && currentThread.Value is { } t) return t;
        var main = new RubyThread
        {
            Proc = new ProcessState(), Generation = gvlGeneration, Caller = Caller, CurLine = CurLine, CurrentFile = CurrentFile, Depth = depth,
            Timeouts = new List<(DateTime, RClass)>(timeouts), Status = Globals.GetValueOrDefault("$?"), ChildStatus = Globals.GetValueOrDefault("$CHILD_STATUS"),
            LastError = Globals.GetValueOrDefault("$!"), LastMatch = Globals.GetValueOrDefault("$~"),
        };
        currentThread.Value = main;
        if (threaded) { Acquire(main); return main; }   // a new entry into an interpreter that has threads
        lock (gvlSync) gvlOwner = main;
        threaded = true;
        sliceStart = Environment.TickCount64;
        return main;
    }

    // Starts body on a new thread. fork: the thread gets a copy of this process's state instead of sharing it.
    internal RubyThread StartRubyThread(Action<RubyThread> body, bool fork, Action<RubyThread> setup = null)
    {
        var parent = EnsureThreaded();
        var t = new RubyThread
        {
            Generation = gvlGeneration,
            Caller = Caller,
            CurLine = CurLine,
            CurrentFile = CurrentFile,
            Status = Globals.GetValueOrDefault("$?"),
            ChildStatus = Globals.GetValueOrDefault("$CHILD_STATUS"),
        };
        t.Proc = fork
            ? new ProcessState { Cwd = Cwd, Env = new Dictionary<string, string>(Env, StringComparer.OrdinalIgnoreCase), Out = Out, Err = Err, Stdio = Stdio }
            : parent.Proc;
        setup?.Invoke(t);
        var thread = new Thread(() =>
        {
            Acquire(t);
            try { body(t); }
            finally { Release(t); }
        }, 64 * 1024 * 1024) { IsBackground = true, Name = fork ? "ruby fork" : "ruby thread" };
        thread.Start();
        return t;
    }

    void Acquire(RubyThread t)
    {
        lock (gvlSync)
        {
            while (true)
            {
                // A killed thread, or one left over from a finished test, parks here for good (not counted as waiting).
                if (t.Killed || t.Generation != gvlGeneration) { Monitor.Wait(gvlSync); continue; }
                if (gvlOwner == null) break;
                gvlWaiting++;
                Monitor.Wait(gvlSync);
                gvlWaiting--;
            }
            gvlOwner = t;
        }
        currentThread.Value = t;
        Caller = t.Caller;
        CurLine = t.CurLine;
        CurrentFile = t.CurrentFile;
        depth = t.Depth;
        timeouts.Clear();
        timeouts.AddRange(t.Timeouts);
        SetThreadGlobal("$?", t.Status);
        SetThreadGlobal("$CHILD_STATUS", t.ChildStatus);
        SetThreadGlobal("$!", t.LastError);
        SetThreadGlobal("$~", t.LastMatch);
        if (t.Proc.Env != null)
        {
            Cwd = t.Proc.Cwd;
            Env.Clear();
            foreach (var kv in t.Proc.Env) Env[kv.Key] = kv.Value;
            Out = t.Proc.Out;
            Err = t.Proc.Err;
            Stdio = t.Proc.Stdio;
        }
        sliceStart = Environment.TickCount64;
    }

    void Release(RubyThread t)
    {
        t.Caller = Caller;
        t.CurLine = CurLine;
        t.CurrentFile = CurrentFile;
        t.Depth = depth;
        t.Timeouts = new List<(DateTime, RClass)>(timeouts);
        t.Status = Globals.GetValueOrDefault("$?");
        t.ChildStatus = Globals.GetValueOrDefault("$CHILD_STATUS");
        t.LastError = Globals.GetValueOrDefault("$!");
        t.LastMatch = Globals.GetValueOrDefault("$~");
        t.Proc.Cwd = Cwd;
        t.Proc.Env = new Dictionary<string, string>(Env, StringComparer.OrdinalIgnoreCase);
        t.Proc.Out = Out;
        t.Proc.Err = Err;
        t.Proc.Stdio = Stdio;
        lock (gvlSync)
        {
            if (gvlOwner == t) gvlOwner = null;
            Monitor.PulseAll(gvlSync);
        }
        t.Released?.Invoke();
    }

    void SetThreadGlobal(string name, object v)
    {
        if (v == null) Globals.Remove(name); else Globals[name] = v;
    }

    // Runs f with the lock let go, so other Ruby threads (a fork child's server) run meanwhile.
    public T Blocking<T>(Func<T> f)
    {
        var t = threaded ? currentThread.Value : null;
        if (t == null || gvlOwner != t) return f();
        Release(t);
        try { return f(); }
        finally { Acquire(t); }
    }

    public void Blocking(Action f) => Blocking(() => { f(); return true; });

    // Waits until slice(ms) reports done, the lock let go, Timeout.timeout deadlines still firing.
    // timeout < 0 waits for good; returns false when the timeout passes first.
    public bool WaitFor(Func<int, bool> slice, double timeoutSeconds = -1)
    {
        var until = timeoutSeconds < 0 ? long.MaxValue : Environment.TickCount64 + (long)(timeoutSeconds * 1000);
        while (true)
        {
            var left = until - Environment.TickCount64;
            var ms = (int)Math.Clamp(left, 0, 50);
            if (Blocking(() => slice(ms))) return true;
            CheckDeadline();
            if (Environment.TickCount64 >= until) return false;
        }
    }

    // A System.Diagnostics.Process started by system/backticks/shell_output: joins the test session's job and is
    // waited for with the lock let go.
    public void WaitForExit(System.Diagnostics.Process p)
    {
        Win32Process.Track(p);
        TrackSpawned?.Invoke(p);
        WaitFor(p.WaitForExit);
        p.WaitForExit();   // drains redirected output
    }

    internal Action<System.Diagnostics.Process> TrackSpawned;

    // Called from method calls and loops: hands the lock to a waiting thread once this one has had 50ms.
    void PreemptIfWaiting()
    {
        if (gvlWaiting == 0 || !threaded || Environment.TickCount64 - sliceStart < 50) return;
        var t = currentThread.Value;
        if (t == null || gvlOwner != t) return;
        Release(t);
        Thread.Sleep(1);
        Acquire(t);
    }

    // The end of a test or formula call: threads it left behind never run again.
    public void EndRubyThreads()
    {
        if (!threaded) return;
        lock (gvlSync)
        {
            gvlGeneration++;
            gvlOwner = null;
            threaded = false;
            Monitor.PulseAll(gvlSync);
        }
        currentThread.Value = null;
    }
}
