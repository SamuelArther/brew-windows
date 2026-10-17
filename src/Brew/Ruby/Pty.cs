using System.ComponentModel;
using static Brew.Ruby.Win32Process;

namespace Brew.Ruby;

// PTY.spawn on a ConPTY pseudoconsole (Windows 10 1809+): the child gets a console of its own, `r` reads what it
// draws (VT sequences, "\r\n" line ends, like a Unix pty), `w` types into it ("\n" is sent as Enter). When the
// child exits the console is closed: its last output arrives, then reads see the end (readline/readpartial raise
// Errno::EIO as on Linux, read/each_line just stop). Process.kill("INT", pid) types ^C first, as a terminal would.
public static partial class FormulaLoader
{
    static partial class TestSupport
    {
        static void DefinePty(Interp it)
        {
            var pty = it.ObjectClass.Consts.GetValueOrDefault("PTY") as RClass ?? it.DefineModule("PTY");
            var childExited = pty.Consts.GetValueOrDefault("ChildExited") as RClass ?? it.DefineClass("ChildExited", it.RuntimeError, pty);
            it.Def(childExited, "status", (i, self, a, b) => ((RObject)self).GetIvar("@status"));
            var S = it.SingletonOf(pty);

            it.Def(S, "spawn getpty", (i, self, a, b) =>
            {
                var (env, cmd, opts) = SplitArgs(i, a);
                PseudoConsole console;
                try { console = PseudoConsole.Create(24, 80); }
                catch (Win32Exception e) { throw i.Error(i.NotImplementedError, e.Message); }
                ChildProcess p;
                try { p = Start(i, env, cmd, opts, null, console); }
                catch
                {
                    console.Close();
                    CloseHandle(console.Input);
                    CloseHandle(console.Output);
                    throw;
                }
                var reader = new PipeIO { ReadHandle = console.Output, Pty = true, Console = console };
                var writer = new PipeIO { WriteHandle = console.Input, Pty = true, Console = console };
                // Read from the start: a console nobody drains stalls its child (and ClosePseudoConsole).
                var r = NewPipeIO(i, reader, pump: true);
                var w = NewPipeIO(i, writer);
                AtSessionEnd(console.Close);
                new Thread(() =>
                {
                    p.WaitForExit(-1);
                    console.Close();
                }) { IsBackground = true, Name = "pty" }.Start();
                p.OnSignal = () =>
                {
                    try { writer.WriteBytes(new byte[] { 3 }); } catch (Exception e) when (e is IOException or ObjectDisposedException) { }
                    Task.Delay(3000).ContinueWith(_ => p.Terminate(2));
                };
                var child = new OsChild { Pid = p.Pid, P = p };
                Register(child);
                var res = new object[] { r, w, (long)p.Pid };
                if (b == null) return new RArray(res);
                try { i.CallProc(b, res); }
                finally
                {
                    // Ruby reaps a child that has already exited; one still running stays waitable.
                    if (!child.Reaped && child.Wait(0)) { Reap(child); StatusOf(i, child); }
                }
                return null;
            });

            // PTY.check(pid, raise = false): nil while it runs, else its Process::Status (or PTY::ChildExited raised).
            it.Def(S, "check", (i, self, a, b) =>
            {
                if (WaitPid(i, new object[] { a[0], 1L }) == null) return null;
                var st = i.Globals["$?"];
                if (a.Length > 1 && Interp.Truthy(a[1]))
                {
                    var e = i.NewException(childExited, $"pty - exited: {i.Int(a[0])}");
                    e.SetIvar("@status", st);
                    throw new RubyError(e);
                }
                return st;
            });
            it.Def(S, "open", (i, self, a, b) =>
                throw i.Error(i.NotImplementedError, "PTY.open: a Windows pseudoconsole has no slave side to hand out (use PTY.spawn)"));
        }
    }
}
