using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using static Brew.Ruby.Win32Process;

namespace Brew.Ruby;

// IO objects over OS pipe handles: IO.pipe's ends, IO.popen, Open3's stdin/stdout/stderr and PTY.spawn's reader
// and writer. A pump thread drains the read side into a buffer, so reads can wait with a timeout (expect,
// wait_readable, Timeout.timeout) and with the interpreter lock let go; it starts on the first read, so a pipe
// end handed to a child (`spawn ..., in: r`) is never read here.
public static partial class FormulaLoader
{
    static partial class TestSupport
    {
        sealed class PipeIO
        {
            public IntPtr ReadHandle, WriteHandle;
            public bool Pty;                   // a PTY side: reading past the end raises Errno::EIO, "\n" typed is Enter
            public PseudoConsole Console;
            public Child Process;              // IO.popen: close waits for it and sets $?
            public bool Binmode;
            readonly object sync = new();
            byte[] buf = new byte[8192];
            int start, end;
            bool eof, readClosed, writeClosed, pumpDone;
            Thread pump;
            volatile int pumpThreadId;

            bool Text => !Pty && !Binmode;   // CRLF line ends from Windows programs read as "\n"
            public bool ReadClosed => readClosed || ReadHandle == IntPtr.Zero;
            public bool WriteClosed => writeClosed || WriteHandle == IntPtr.Zero;

            public void StartPump()
            {
                lock (sync)
                {
                    if (pump != null || ReadHandle == IntPtr.Zero || readClosed) return;
                    pump = new Thread(PumpLoop) { IsBackground = true, Name = "ruby io" };
                    pump.Start();
                }
            }

            void PumpLoop()
            {
                pumpThreadId = GetCurrentThreadId();
                var chunk = new byte[16384];
                while (!Volatile.Read(ref readClosed))
                {
                    if (!ReadFile(ReadHandle, chunk, chunk.Length, out var n, IntPtr.Zero)) break;   // broken pipe: EOF
                    if (n == 0)
                    {
                        if (Available(ReadHandle) < 0) break;
                        continue;   // a zero-length write
                    }
                    lock (sync)
                    {
                        if (end + n > buf.Length)
                        {
                            var count = end - start;
                            var bigger = count + n > buf.Length ? new byte[Math.Max(buf.Length * 2, count + n)] : buf;
                            Array.Copy(buf, start, bigger, 0, count);
                            buf = bigger;
                            start = 0;
                            end = count;
                        }
                        Array.Copy(chunk, 0, buf, end, n);
                        end += n;
                        Monitor.PulseAll(sync);
                    }
                }
                lock (sync)
                {
                    eof = true;
                    pumpDone = true;
                    if (readClosed) CloseHandle(ReadHandle);
                    Monitor.PulseAll(sync);
                }
            }

            // Waits (lock let go) until ready() holds under the buffer lock; false when the timeout passes first.
            public bool WaitUntil(Interp i, Func<bool> ready, double timeout = -1)
            {
                if (ReadClosed) throw i.Error(i.IOError, "closed stream");
                StartPump();
                return i.WaitFor(ms =>
                {
                    lock (sync)
                    {
                        if (ready()) return true;
                        if (ms > 0) Monitor.Wait(sync, ms);
                        return ready();
                    }
                }, timeout);
            }

            public int Count { get { lock (sync) return end - start; } }
            public bool AtEof { get { lock (sync) return eof && end == start; } }
            bool HasDataOrEof() => end > start || eof;

            byte[] Take(int n)
            {
                lock (sync)
                {
                    n = Math.Min(n, end - start);
                    var r = buf[start..(start + n)];
                    start += n;
                    if (start == end) start = end = 0;
                    return r;
                }
            }

            void Unget(byte[] bytes)
            {
                lock (sync)
                {
                    var count = end - start;
                    var nb = new byte[Math.Max(buf.Length, count + bytes.Length)];
                    Array.Copy(bytes, nb, bytes.Length);
                    Array.Copy(buf, start, nb, bytes.Length, count);
                    buf = nb;
                    start = 0;
                    end = count + bytes.Length;
                }
            }

            int IndexOf(byte[] sep)
            {
                for (int k = start; k + sep.Length <= end; k++)
                {
                    int j = 0;
                    while (j < sep.Length && buf[k + j] == sep[j]) j++;
                    if (j == sep.Length) return k - start;
                }
                return -1;
            }

            RString Decode(byte[] bytes, bool text)
            {
                var s = Interp.DecodeUtf8(bytes);
                if (text && Text) s = s.Replace("\r\n", "\n");
                return new RString(s);
            }

            public void WaitEof(Interp i) => WaitUntil(i, () => eof);

            public RString TakeAllText() => Decode(Take(int.MaxValue), true);

            // ---- reads ----

            RubyError EndError(Interp i) => Pty ? i.Error(ErrnoClass(i, "EIO"), "Input/output error") : i.Error(EOFErrorClass(i), "end of file reached");

            // gets: sep null reads to the end; limit caps the bytes; chomp drops the separator.
            public RString GetLine(Interp i, string sep, long limit, bool chomp)
            {
                if (sep == "") sep = "\n\n";   // paragraph mode, near enough
                var sepBytes = sep == null ? null : Encoding.UTF8.GetBytes(sep);
                WaitUntil(i, () => eof || (sepBytes != null && IndexOf(sepBytes) >= 0) || (limit > 0 && end - start >= limit));
                byte[] bytes;
                lock (sync)
                {
                    if (end == start) return null;
                    var at = sepBytes == null ? -1 : IndexOf(sepBytes);
                    var n = at >= 0 ? at + sepBytes.Length : end - start;
                    if (limit > 0 && n > limit) n = (int)limit;
                    bytes = Take(n);
                }
                var line = Decode(bytes, false).V;
                if (Text && sep == "\n" && line.EndsWith("\r\n")) line = line[..^2] + "\n";
                if (chomp && sep != null && line.EndsWith(sep))
                {
                    line = line[..^sep.Length];
                    if (sep == "\n" && line.EndsWith('\r')) line = line[..^1];
                }
                return new RString(line);
            }

            public object Read(Interp i, object len)
            {
                if (len == null)
                {
                    WaitUntil(i, () => eof);
                    return Decode(Take(int.MaxValue), true);
                }
                var n = (int)i.Int(len);
                if (n < 0) throw i.ArgError($"negative length {n} given");
                if (n == 0) return Interp.S("");
                WaitUntil(i, () => eof || end - start >= n);
                if (Count == 0) return null;
                return Decode(Take(n), false);
            }

            public object ReadPartial(Interp i, int max, bool block, bool exception)
            {
                if (block) WaitUntil(i, HasDataOrEof);
                else WaitUntil(i, HasDataOrEof, 0.02);
                lock (sync)
                {
                    if (end > start) return Decode(Take(max), false);
                    if (!eof)
                    {
                        if (!exception) return Interp.Sym("wait_readable");
                        throw i.Error(WaitReadableError(i), "Resource temporarily unavailable - read would block");
                    }
                }
                if (!exception) return null;
                throw EndError(i);
            }

            public RString GetChar(Interp i)
            {
                WaitUntil(i, HasDataOrEof);
                int lead;
                lock (sync)
                {
                    if (end == start) return null;
                    lead = buf[start];
                }
                var len = lead < 0x80 ? 1 : lead >= 0xF0 ? 4 : lead >= 0xE0 ? 3 : lead >= 0xC0 ? 2 : 1;
                if (len > 1) WaitUntil(i, () => eof || end - start >= len);
                return Decode(Take(len), false);
            }

            public object GetByte(Interp i)
            {
                WaitUntil(i, HasDataOrEof);
                lock (sync) return end == start ? null : (long)Take(1)[0];
            }

            public void UngetBytes(byte[] b) => Unget(b);

            public bool Eof(Interp i)
            {
                WaitUntil(i, HasDataOrEof);
                return AtEof;
            }

            public bool WaitReadable(Interp i, double timeout) => WaitUntil(i, HasDataOrEof, timeout);

            public bool Ready
            {
                get
                {
                    StartPump();
                    lock (sync) return HasDataOrEof();
                }
            }

            // IO#expect (require "expect"): reads until the pattern matches what was read; [text, *captures] or nil
            // on timeout/EOF (what was read is gone either way, like Ruby's).
            public object Expect(Interp i, object pat, double timeout)
            {
                var re = pat is RRegex r ? r.R : new Regex(Regex.Escape(i.ToS(pat)));
                string text = null;
                Match m = null;
                bool Matched()
                {
                    text = Interp.DecodeUtf8(buf.AsSpan(start, end - start));
                    m = re.Match(text);
                    return m.Success;
                }
                bool ok = WaitUntil(i, () => Matched() || eof, timeout);
                lock (sync)
                {
                    if (!ok || !Matched())
                    {
                        Take(int.MaxValue);
                        return null;
                    }
                    // The shortest prefix that matches: Ruby checks after every character it reads.
                    int lo = 0, hi = m.Index + m.Length;
                    while (lo < hi)
                    {
                        var mid = (lo + hi) / 2;
                        if (re.IsMatch(text[..mid])) hi = mid; else lo = mid + 1;
                    }
                    var prefix = text[..hi];
                    var pm = re.Match(prefix);
                    Take(RString.Utf8(prefix).Length);
                    var res = new RArray(new object[] { Interp.S(prefix) });
                    for (int g = 1; g < pm.Groups.Count; g++) res.L.Add(pm.Groups[g].Success ? Interp.S(pm.Groups[g].Value) : null);
                    return res;
                }
            }

            // ---- writes ----

            public long WriteBytes(byte[] bytes)
            {
                if (WriteClosed) throw new ObjectDisposedException("pipe");
                if (Pty) bytes = Encoding.Latin1.GetBytes(Encoding.Latin1.GetString(bytes).Replace("\r\n", "\r").Replace('\n', '\r'));
                int done = 0;
                while (done < bytes.Length)
                {
                    var part = done == 0 ? bytes : bytes[done..];
                    if (!WriteFile(WriteHandle, part, part.Length, out var n, IntPtr.Zero)) throw new IOException("Broken pipe");
                    done += n;
                }
                return bytes.Length;
            }

            // ---- closing ----

            public void CloseWrite()
            {
                lock (sync)
                {
                    if (writeClosed || WriteHandle == IntPtr.Zero) return;
                    writeClosed = true;
                }
                CloseHandle(WriteHandle);
            }

            // A pump blocked in ReadFile is woken (CancelSynchronousIo); it closes the handle on its way out.
            public void CloseRead()
            {
                lock (sync)
                {
                    if (readClosed || ReadHandle == IntPtr.Zero) return;
                    readClosed = true;
                    if (pump == null || pumpDone)
                    {
                        CloseHandle(ReadHandle);
                        return;
                    }
                }
                for (int k = 0; k < 40; k++)
                {
                    lock (sync) if (pumpDone) return;
                    if (pumpThreadId != 0) CancelRead(pumpThreadId);
                    Thread.Sleep(5);
                }
            }

            public void Close(Interp i)
            {
                CloseWrite();
                CloseRead();
                if (Process is { Reaped: false } c)
                {
                    i.WaitFor(c.Wait);
                    Reap(c);
                    SetChildStatus(i, StatusOf(i, c));
                }
            }

            public void CloseAll()
            {
                CloseWrite();
                CloseRead();
            }
        }

        static RClass pipeIOClass;

        static RObject NewPipeIO(Interp i, PipeIO p, bool pump = false)
        {
            var o = new RObject { Class = pipeIOClass, Host = p };
            AtSessionEnd(p.CloseAll);
            if (pump) p.StartPump();
            return o;
        }

        static RClass ErrnoClass(Interp i, string name) => Errno(i, name);

        static RClass EOFErrorClass(Interp i) =>
            i.ObjectClass.Consts.GetValueOrDefault("EOFError") as RClass ?? i.DefineClass("EOFError", i.IOError);

        static RClass WaitReadableError(Interp i)
        {
            var io = i.IOClass;
            var mod = io.Consts.GetValueOrDefault("WaitReadable") as RClass ?? i.DefineModule("WaitReadable", io);
            if (io.Consts.GetValueOrDefault("EAGAINWaitReadable") is RClass c) return c;
            c = i.DefineClass("EAGAINWaitReadable", Errno(i, "EAGAIN"), io);
            c.Includes.Add(mod);
            return c;
        }

        static void DefinePipeIO(Interp it)
        {
            var C = pipeIOClass = new RClass { Name = "IO", Super = it.IOClass, Class = it.ClassClass };
            EOFErrorClass(it);
            WaitReadableError(it);
            ErrnoClass(it, "EIO");
            ErrnoClass(it, "EPIPE");
            PipeIO Me(object self) => (PipeIO)((RObject)self).Host;
            PipeIO Reader(Interp i, object self)
            {
                var p = Me(self);
                if (p.ReadHandle == IntPtr.Zero) throw i.Error(i.IOError, "not opened for reading");
                return p;
            }
            long Write(Interp i, object self, string s, bool binary)
            {
                var p = Me(self);
                if (p.WriteHandle == IntPtr.Zero) throw i.Error(i.IOError, "not opened for writing");
                if (p.WriteClosed) throw i.Error(i.IOError, "closed stream");
                try { return p.WriteBytes(binary ? Encoding.Latin1.GetBytes(s) : RString.Utf8(s)); }
                catch (IOException) { throw i.Error(ErrnoClass(i, "EPIPE"), "Broken pipe"); }
            }
            long WriteObj(Interp i, object self, object x) => Write(i, self, i.ToS(x), x is RString { Binary: true });
            // gets/readline/each_line arguments: (sep = $/, limit = nil, chomp: false)
            (string Sep, long Limit, bool Chomp) LineArgs(Interp i, object[] a)
            {
                string sep = "\n";
                long limit = 0;
                bool chomp = false;
                var pos = a.ToList();
                if (pos.Count > 0 && pos[^1] is RHash h)
                {
                    chomp = h.D.TryGetValue(Interp.Sym("chomp"), out var c) && Interp.Truthy(c);
                    pos.RemoveAt(pos.Count - 1);
                }
                if (pos.Count > 0)
                {
                    if (pos[0] is long l) limit = l;
                    else sep = pos[0] == null ? null : i.ToS(pos[0]);
                }
                if (pos.Count > 1 && pos[1] is long l2) limit = l2;
                return (sep, limit, chomp);
            }

            it.Def(C, "gets", (i, self, a, b) =>
            {
                var (sep, limit, chomp) = LineArgs(i, a);
                var line = Reader(i, self).GetLine(i, sep, limit, chomp);
                i.Globals["$_"] = line;
                return line;
            });
            it.Def(C, "readline", (i, self, a, b) =>
            {
                var (sep, limit, chomp) = LineArgs(i, a);
                var p = Reader(i, self);
                return p.GetLine(i, sep, limit, chomp) ?? throw (p.Pty ? i.Error(ErrnoClass(i, "EIO"), "Input/output error") : i.Error(EOFErrorClass(i), "end of file reached"));
            });
            it.Def(C, "each_line each", (i, self, a, b) =>
            {
                var (sep, limit, chomp) = LineArgs(i, a);
                var p = Reader(i, self);
                var lines = b == null ? new RArray() : null;
                while (p.GetLine(i, sep, limit, chomp) is { } line)
                    if (b == null) lines.L.Add(line); else i.CallProc(b, new object[] { line });
                return b == null ? lines : self;
            });
            it.Def(C, "readlines to_a", (i, self, a, b) =>
            {
                var (sep, limit, chomp) = LineArgs(i, a);
                var p = Reader(i, self);
                var lines = new RArray();
                while (p.GetLine(i, sep, limit, chomp) is { } line) lines.L.Add(line);
                return lines;
            });
            object WithOutbuf(object[] a, int at, object r)
            {
                if (a.Length > at && a[at] is RString ob && r is RString rs) { ob.V = rs.V; ob.Binary = rs.Binary; return ob; }
                return r;
            }
            it.Def(C, "read", (i, self, a, b) => WithOutbuf(a, 1, Reader(i, self).Read(i, a.Length > 0 ? a[0] : null)));
            it.Def(C, "readpartial sysread", (i, self, a, b) => WithOutbuf(a, 1, Reader(i, self).ReadPartial(i, (int)i.Int(a[0]), true, true)));
            it.Def(C, "read_nonblock", (i, self, a, b) =>
            {
                var exception = !(a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Interp.Sym("exception"), out var e) && e is false);
                return WithOutbuf(a, 1, Reader(i, self).ReadPartial(i, (int)i.Int(a[0]), false, exception));
            });
            it.Def(C, "getc", (i, self, a, b) => Reader(i, self).GetChar(i));
            it.Def(C, "readchar", (i, self, a, b) => Reader(i, self).GetChar(i) ?? throw i.Error(EOFErrorClass(i), "end of file reached"));
            it.Def(C, "each_char", (i, self, a, b) =>
            {
                var p = Reader(i, self);
                while (p.GetChar(i) is { } c) i.CallProc(b, new object[] { c });
                return self;
            });
            it.Def(C, "getbyte", (i, self, a, b) => Reader(i, self).GetByte(i));
            it.Def(C, "readbyte", (i, self, a, b) => Reader(i, self).GetByte(i) ?? throw i.Error(EOFErrorClass(i), "end of file reached"));
            it.Def(C, "each_byte", (i, self, a, b) =>
            {
                var p = Reader(i, self);
                while (p.GetByte(i) is long c) i.CallProc(b, new object[] { c });
                return self;
            });
            it.Def(C, "ungetc ungetbyte", (i, self, a, b) =>
            {
                Reader(i, self).UngetBytes(a[0] is long n ? new[] { (byte)n } : RString.Utf8(i.ToS(a[0])));
                return null;
            });
            it.Def(C, "eof? eof", (i, self, a, b) => Reader(i, self).Eof(i));
            it.Def(C, "wait_readable wait", (i, self, a, b) =>
                Reader(i, self).WaitReadable(i, a.Length > 0 && a[0] != null ? i.Num(a[0]) : -1) ? self : null);
            it.Def(C, "ready?", (i, self, a, b) => Reader(i, self).Ready);
            it.Def(C, "nread", (i, self, a, b) => { var p = Reader(i, self); p.StartPump(); return (long)p.Count; });
            it.Def(C, "expect", (i, self, a, b) =>
            {
                var res = Reader(i, self).Expect(i, a[0], a.Length > 1 && a[1] != null ? i.Num(a[1]) : 9999999);
                return b == null ? res : i.CallProc(b, new[] { res });
            });

            it.Def(C, "write syswrite write_nonblock", (i, self, a, b) =>
            {
                long n = 0;
                foreach (var x in a.Where(x => x is not RHash)) n += WriteObj(i, self, x);
                return n;
            });
            it.Def(C, "print", (i, self, a, b) => { foreach (var x in a) WriteObj(i, self, x); return null; });
            it.Def(C, "<<", (i, self, a, b) => { WriteObj(i, self, a[0]); return self; });
            it.Def(C, "puts", (i, self, a, b) =>
            {
                var sw = new StringWriter();
                i.Puts(sw, a);
                Write(i, self, sw.ToString(), a.Any(x => x is RString { Binary: true }));
                return null;
            });
            it.Def(C, "printf", (i, self, a, b) => { Write(i, self, i.Format(i.Str(a[0]), a[1..]), false); return null; });
            it.Def(C, "putc", (i, self, a, b) =>
            {
                if (a[0] is long n) Me(self).WriteBytes(new[] { (byte)n });
                else { var s = i.ToS(a[0]); Write(i, self, s.Length > 0 ? char.IsSurrogatePair(s, 0) ? s[..2] : s[..1] : "", false); }
                return a[0];
            });
            it.Def(C, "flush fsync fdatasync iflush oflush ioflush to_io binmode! raw! cooked! set_encoding set_encoding_by_bom", (i, self, a, b) => self);
            it.Def(C, "binmode", (i, self, a, b) => { Me(self).Binmode = true; return self; });
            it.Def(C, "binmode?", (i, self, a, b) => Me(self).Binmode);
            it.Def(C, "sync", (i, self, a, b) => true);
            it.Def(C, "sync= autoclose= close_on_exec= echo=", (i, self, a, b) => a[0]);
            it.Def(C, "external_encoding internal_encoding", (i, self, a, b) => null);
            it.Def(C, "lineno", (i, self, a, b) => 0L);
            it.Def(C, "tty? isatty", (i, self, a, b) => Me(self).Pty);
            it.Def(C, "raw noecho cooked", (i, self, a, b) => b == null ? self : i.CallProc(b, new[] { self }));
            it.Def(C, "winsize", (i, self, a, b) =>
                Me(self).Console is { } c ? new RArray(new object[] { (long)c.Rows, (long)c.Cols }) : throw i.Error(ErrnoClass(i, "ENOTTY"), "Inappropriate ioctl for device"));
            it.Def(C, "winsize=", (i, self, a, b) =>
            {
                var size = i.Ary(a[0]);
                if (Me(self).Console is { } c) c.Resize((short)i.Int(size.L[0]), (short)i.Int(size.L[1]));
                return a[0];
            });
            it.Def(C, "close", (i, self, a, b) => { Me(self).Close(i); return null; });
            it.Def(C, "close_read", (i, self, a, b) => { Me(self).CloseRead(); return null; });
            it.Def(C, "close_write", (i, self, a, b) => { Me(self).CloseWrite(); return null; });
            it.Def(C, "closed?", (i, self, a, b) => Me(self).ReadClosed && Me(self).WriteClosed);
            it.Def(C, "pid", (i, self, a, b) => Me(self).Process?.Pid);
            it.Def(C, "fileno to_i", (i, self, a, b) => (long)(Me(self).ReadHandle != IntPtr.Zero ? Me(self).ReadHandle : Me(self).WriteHandle));
            it.Def(C, "inspect to_s", (i, self, a, b) => Interp.S($"#<IO:fd {(long)(Me(self).ReadHandle != IntPtr.Zero ? Me(self).ReadHandle : Me(self).WriteHandle)}>"));

            var IOS = it.SingletonOf(it.IOClass);

            // IO.pipe: [reader, writer]; with a block both are closed after it.
            it.Def(IOS, "pipe", (i, self, a, b) =>
            {
                var (r, w) = Pipe();
                var pair = new object[] { NewPipeIO(i, new PipeIO { ReadHandle = r }), NewPipeIO(i, new PipeIO { WriteHandle = w }) };
                if (b == null) return new RArray(pair);
                try { return i.CallProc(b, pair); }
                finally { foreach (RObject x in pair) Me(x).CloseAll(); }
            });

            // IO.select(reads, writes = nil, errors = nil, timeout = nil) over these pipes; anything else counts as ready
            // (or goes to an IO.select defined before this one).
            var previousSelect = IOS.Methods.GetValueOrDefault("select");
            it.Def(IOS, "select", (i, self, a, b) =>
            {
                var reads = a.Length > 0 && a[0] is RArray ra ? ra.L : new List<object>();
                var writes = a.Length > 1 && a[1] is RArray wa ? wa.L : new List<object>();
                var timeout = a.Length > 3 && a[3] != null ? i.Num(a[3]) : -1;
                if (previousSelect != null && reads.Concat(writes).Any(x => x is not RObject { Host: PipeIO }))
                    return previousSelect.Fn(i, self, a, b);
                List<object> ready = null;
                bool Check(int ms)
                {
                    ready = reads.Where(x => x is not RObject { Host: PipeIO p } || p.Ready).ToList();
                    if (ready.Count > 0 || writes.Count > 0 || ms == 0) return ready.Count > 0 || writes.Count > 0;
                    Thread.Sleep(Math.Min(ms, 10));
                    return false;
                }
                if (!(timeout == 0 ? Check(0) : i.WaitFor(Check, timeout))) return null;
                return new RArray(new object[] { new RArray(ready), new RArray(writes), new RArray() });
            });

            // IO.popen([env,] cmd, mode = "r" [, opts]) { |io| ... }: cmd is a shell line or an argv array.
            it.Def(IOS, "popen", (i, self, a, b) =>
            {
                var args = a.ToList();
                RHash env = args.Count > 1 && args[0] is RHash e ? e : null;
                if (env != null) args.RemoveAt(0);
                RHash opts = args.Count > 1 && args[^1] is RHash oh ? oh : null;
                if (opts != null) args.RemoveAt(args.Count - 1);
                var mode = args.Count > 1 ? ModeOf(i, args[1]) : opts != null && opts.D.TryGetValue(Interp.Sym("mode"), out var mo) ? ModeOf(i, mo) : "r";
                List<object> cmd;
                if (args[0] is RArray arr)
                {
                    cmd = arr.L.ToList();
                    if (cmd.Count > 0 && cmd[0] is RHash ae) { env = ae; cmd.RemoveAt(0); }
                    if (cmd.Count > 1 && cmd[^1] is RHash ao) { opts = ao; cmd.RemoveAt(cmd.Count - 1); }
                }
                else
                {
                    if (i.ToS(args[0]) == "-") throw i.Error(i.NotImplementedError, "IO.popen(\"-\") forks, which Windows can't");
                    cmd = new List<object> { args[0] };
                }
                bool reading = mode.Contains('r') || mode.Contains('+'), writing = mode.Contains('w') || mode.Contains('+');
                var (outR, outW) = reading ? Pipe() : (IntPtr.Zero, IntPtr.Zero);
                var (inR, inW) = writing ? Pipe() : (IntPtr.Zero, IntPtr.Zero);
                ChildProcess p;
                try { p = Start(i, env, cmd, opts, new[] { inR, outW, IntPtr.Zero }); }
                catch
                {
                    if (outR != IntPtr.Zero) CloseHandle(outR);
                    if (inW != IntPtr.Zero) CloseHandle(inW);
                    throw;
                }
                finally
                {
                    if (outW != IntPtr.Zero) CloseHandle(outW);
                    if (inR != IntPtr.Zero) CloseHandle(inR);
                }
                var child = new OsChild { Pid = p.Pid, P = p };
                Register(child);
                var io = NewPipeIO(i, new PipeIO { ReadHandle = outR, WriteHandle = inW, Process = child, Binmode = mode.Contains('b') }, pump: reading);
                if (b == null) return io;
                try { return i.CallProc(b, new object[] { io }); }
                finally { Me(io).Close(i); }
            });
        }
    }
}
