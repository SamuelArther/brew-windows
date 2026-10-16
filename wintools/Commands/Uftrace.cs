using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// uftrace: a function graph tracer. Linux uftrace instruments functions (-pg/-finstrument-functions
// or, with -P, dynamic patching) and records their entries and exits. Here every traced program is
// patched dynamically through the Windows debugging API: the functions named by -P (default: all
// of the program's own functions) get a breakpoint on their first instruction; on entry the return
// address is swapped for a trampoline so the exit is seen too (timestamps, the return value in RAX,
// the first arguments in RCX/RDX/R8/R9 and on the stack for -A). Function names come from the
// program's PDB (through DbgHelp) or its COFF symbol table (MinGW builds), and -P patterns also
// patch matching exports of the DLLs it loads (library calls such as printf). Records are kept in
// uftrace.data (record/replay/report/graph/info/dump); live records and replays at exit. Timings
// include the debugger round trip of each event, so they are larger than the code's own run time.
// C++ exceptions unwinding through traced frames see the trampoline instead of the caller, so
// programs that throw across traced functions may misbehave; trace those with narrower -P patterns.
static unsafe class Uftrace
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("uftrace", "Trace and analyze function calls of a program (function graph tracer)", Run);

    const string Version = "v0.20";

    sealed class Options
    {
        public string Command = "live", DataDir = "uftrace.data";
        public List<string> Patch = new(), Filter = new(), NoTrace = new(), Args = new(), Retvals = new();
        public int Depth = int.MaxValue;
        public double TimeFilterNs;
        public bool AutoArgs, NoLibcall;
        public List<string> Program = new();
    }

    static int Run(string[] args)
    {
        var o = new Options();
        var i = 0;
        if (args.Length > 0 && args[0] is "record" or "replay" or "live" or "report" or "info" or "dump" or "graph" or "tui" or "recv" or "script")
            o.Command = args[i++];
        for (; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--") { i++; break; }
            if (!a.StartsWith('-') || a == "-") break;
            var eq = a.StartsWith("--") ? a.IndexOf('=') : -1;
            var name = eq > 0 ? a[..eq] : a;
            string attached = !a.StartsWith("--") && a.Length > 2 ? a[2..] : null;
            if (attached != null) name = a[..2];
            string Value() => attached ?? (eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"option '{name}' requires an argument"));
            switch (name)
            {
                case "-h" or "--help": Usage(); return 0;
                case "-V" or "--version": Console.WriteLine($"uftrace {Version} ( x86_64 pdb coff dynamic )"); return 0;
                case "-P" or "--patch": o.Patch.Add(Value()); break;
                case "-F" or "--filter": o.Filter.Add(Value()); break;
                case "-N" or "--notrace": o.NoTrace.Add(Value()); break;
                case "-A" or "--argument": o.Args.Add(Value()); break;
                case "-R" or "--retval": o.Retvals.Add(Value()); break;
                case "-a" or "--auto-args": o.AutoArgs = true; break;
                case "-D" or "--depth": o.Depth = int.Parse(Value()); break;
                case "-t" or "--time-filter": o.TimeFilterNs = ParseTime(Value()); break;
                case "-d" or "--data": o.DataDir = Value(); break;
                case "--no-libcall": o.NoLibcall = true; break;
                case "--force" or "--no-pager" or "--no-comment" or "-e" or "--estimate-return" or "--srcline" or "--nest-libcall" or "--libname": break;
                case "-T" or "--trigger" or "-U" or "--unpatch" or "-C" or "--caller-filter" or "--match" or "-s" or "--sort" or "--output-fields" or "-f" or "--tid" or "-E" or "--event":
                    Value();
                    break;
                default: throw new UsageException($"unrecognized option '{a}'");
            }
        }
        o.Program = args[i..].ToList();
        switch (o.Command)
        {
            case "record":
            {
                if (o.Program.Count == 0) throw new UsageException("no program to record (see uftrace --help)");
                var (trace, code) = Trace(o);
                Save(o.DataDir, o, trace);
                return code;
            }
            case "live":
            {
                if (o.Program.Count == 0) { Usage(); return 1; }
                var (trace, code) = Trace(o);
                Replay(Console.Out, trace, o);
                return code;
            }
            case "replay": Replay(Console.Out, Load(o.DataDir), o); return 0;
            case "report": Report(Load(o.DataDir), o); return 0;
            case "graph": Graph(Load(o.DataDir), o.Program.FirstOrDefault()); return 0;
            case "info": Info(o.DataDir); return 0;
            case "dump": Dump(Load(o.DataDir)); return 0;
            default:
                Console.Error.WriteLine($"uftrace: {o.Command} isn't available in the Windows build; use replay, report, graph, info or dump");
                return 1;
        }
    }

    static double ParseTime(string s)
    {
        var m = Regex.Match(s, @"^([\d.]+)\s*(ns|us|ms|s|m)?$");
        if (!m.Success) throw new UsageException($"invalid time: {s}");
        var n = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        return n * (m.Groups[2].Value switch { "us" => 1e3, "ms" => 1e6, "s" => 1e9, "m" => 60e9, _ => 1 });
    }

    static void Usage() => Console.WriteLine($"""
        Usage: uftrace [COMMAND] [OPTION...] [<program>]

         uftrace -- function (graph) tracer for userspace (Windows: dynamic patching through the debugger API)

         COMMAND:
           record          Run a program and saves the trace data
           replay          Show program execution in the trace data
           report          Show performance statistics in the trace data
           live            Do record and replay in a row (default)
           info            Show system and program info in the trace data
           dump            Show low-level trace data
           graph           Show function call graph in the trace data

         OPTION:
          -A, --argument=FUNC@arg[,arg,...]   Show function arguments (. for all functions)
          -a, --auto-args            Show arguments and return value of known functions
          -d, --data=DATA            Use this DATA instead of uftrace.data
          -D, --depth=DEPTH          Trace functions within DEPTH
          -F, --filter=FUNC          Only trace those FUNCs (regex)
          -N, --notrace=FUNC         Don't trace those FUNCs (regex)
          -P, --patch=FUNC           Apply dynamic patching for FUNCs (regex; also DLL exports) [default: all of the program's functions]
          -R, --retval=FUNC[@retval] Show function return value (. for all functions)
          -t, --time-filter=TIME     Hide small functions run less than the TIME
              --no-libcall           Don't trace library function calls
          -h, --help                 Give this help list
          -V, --version              Print program version

        uftrace {Version}
        """);

    // ---- events ----

    sealed class Event
    {
        public int Tid, Depth;
        public long Time;          // ns since start
        public bool Exit;
        public string Function;
        public string Args, Retval;
    }

    sealed class TraceData
    {
        public List<Event> Events = new();
        public string Exe = "", CommandLine = "";
        public int ExitCode;
        public DateTime Started = DateTime.Now;
        public int Patched;
    }

    // ---- tracing ----

    sealed class Thread_
    {
        public nint Handle;
        public Stack<(string Function, ulong Return, ulong Rsp, long Time)> Shadow = new();
        public ulong Rearm;
    }

    static (TraceData, int) Trace(Options o)
    {
        var data = new TraceData { CommandLine = string.Join(' ', o.Program) };
        var program = ResolveExecutable(o.Program[0]);
        data.Exe = program;
        var line = new StringBuilder(Quote(program));
        foreach (var a in o.Program.Skip(1)) line.Append(' ').Append(Quote(a));
        var si = new STARTUPINFOW { cb = sizeof(STARTUPINFOW) };
        PROCESS_INFORMATION pi;
        fixed (char* cmd = line.Append('\0').ToString())
            if (!CreateProcessW(program, cmd, 0, 0, true, DEBUG_ONLY_THIS_PROCESS, 0, null, &si, &pi))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), $"uftrace: cannot run {o.Program[0]}");
        CloseHandle(pi.hThread);

        var patches = new Dictionary<ulong, (string Name, byte Original)>();
        var threads = new Dictionary<int, Thread_>();
        var patchRegex = (o.Patch.Count > 0 ? o.Patch : new List<string> { "." }).Select(p => new Regex(p)).ToList();
        var filterRegex = o.Filter.Select(p => new Regex(p)).ToList();
        var noTraceRegex = o.NoTrace.Select(p => new Regex(p)).ToList();
        bool Wanted(string name) => !noTraceRegex.Any(r => r.IsMatch(name));
        var argFuncs = o.Args.Select(SpecFunction).ToList();
        var retFuncs = o.Retvals.Select(SpecFunction).ToList();
        bool Matches(List<Regex> list, string name) => list.Any(r => r.IsMatch(name));
        ulong trampoline = 0;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        long Now() => (long)((System.Diagnostics.Stopwatch.GetTimestamp() - started) * (1e9 / System.Diagnostics.Stopwatch.Frequency));
        var context = (byte*)NativeMemory.AlignedAlloc(1232, 16);
        var ev = stackalloc byte[192];
        var firstBreakpoint = true;
        var exitCode = 0;
        nint process = pi.hProcess;

        void Patch(ulong address, string name)
        {
            if (patches.ContainsKey(address) || !Wanted(name)) return;
            byte original;
            byte int3 = 0xCC;
            if (!ReadProcessMemory(process, address, &original, 1, out _)) return;
            if (!WriteProcessMemory(process, address, &int3, 1, out _)) return;
            patches[address] = (name, original);
        }

        try
        {
            for (var running = true; running;)
            {
                if (!WaitForDebugEvent(ev, INFINITE)) break;
                var code = *(int*)ev;
                var tid = *(int*)(ev + 8);
                var status = DBG_CONTINUE;
                switch (code)
                {
                    case 3:   // CREATE_PROCESS_DEBUG_EVENT
                    {
                        var file = *(nint*)(ev + 16);
                        var baseAddress = *(ulong*)(ev + 40);
                        threads[tid] = new Thread_ { Handle = *(nint*)(ev + 32) };
                        foreach (var (address, name) in ModuleFunctions(process, file, baseAddress, program))
                            if (Matches(patchRegex, name)) Patch(address, name);
                        if (file != 0) CloseHandle(file);
                        // The trampoline: a page of INT3s in the target that traced functions return to.
                        trampoline = VirtualAllocEx(process, 0, 4096, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                        var page = new byte[4096];
                        Array.Fill(page, (byte)0xCC);
                        fixed (byte* p = page) WriteProcessMemory(process, trampoline, p, 4096, out _);
                        FlushInstructionCache(process, 0, 0);
                        data.Patched = patches.Count;
                        break;
                    }
                    case 2:   // CREATE_THREAD_DEBUG_EVENT
                        threads[tid] = new Thread_ { Handle = *(nint*)(ev + 16) };
                        break;
                    case 6:   // LOAD_DLL_DEBUG_EVENT: library calls (exports matching explicit -P patterns)
                    {
                        var file = *(nint*)(ev + 16);
                        if (!o.NoLibcall && o.Patch.Count > 0)
                        {
                            foreach (var (address, name) in Exports(process, *(ulong*)(ev + 24)))
                                if (Matches(patchRegex, name) && o.Patch.Any(p => p != ".")) Patch(address, name);
                            FlushInstructionCache(process, 0, 0);
                        }
                        if (file != 0) CloseHandle(file);
                        break;
                    }
                    case 5:   // EXIT_PROCESS_DEBUG_EVENT
                        exitCode = *(int*)(ev + 16);
                        running = false;
                        break;
                    case 1:   // EXCEPTION_DEBUG_EVENT
                    {
                        var exception = *(uint*)(ev + 16);
                        var address = *(ulong*)(ev + 32);
                        threads.TryGetValue(tid, out var thread);
                        if (exception == 0x80000003 && firstBreakpoint) { firstBreakpoint = false; break; }
                        if (thread == null) { status = DBG_EXCEPTION_NOT_HANDLED; break; }
                        if ((exception == 0x80000004u || exception == 0x4000001Eu) && thread.Rearm != 0)   // single step: put the breakpoint back
                        {
                            byte int3 = 0xCC;
                            WriteProcessMemory(process, thread.Rearm, &int3, 1, out _);
                            FlushInstructionCache(process, thread.Rearm, 1);
                            thread.Rearm = 0;
                            break;
                        }
                        if (exception != 0x80000003 && exception != 0x4000001F) { status = DBG_EXCEPTION_NOT_HANDLED; break; }
                        *(uint*)(context + 0x30) = CONTEXT_FULL;
                        if (!GetThreadContext(thread.Handle, context)) { status = DBG_EXCEPTION_NOT_HANDLED; break; }
                        ref var rip = ref *(ulong*)(context + 0xF8);
                        ref var rsp = ref *(ulong*)(context + 0x98);
                        if (address >= trampoline && address < trampoline + 4096 && trampoline != 0)
                        {
                            // A traced function returned: frames unwound past (exceptions, longjmp) close too.
                            var now = Now();
                            while (thread.Shadow.Count > 0)
                            {
                                var frame = thread.Shadow.Pop();
                                var depth = thread.Shadow.Count;
                                var proper = frame.Rsp + 8 == rsp;
                                string retval = null;
                                if (proper && (o.Retvals.Count > 0 && Matches(retFuncs, frame.Function) || o.AutoArgs))
                                    retval = Hex(*(ulong*)(context + 0x78));
                                data.Events.Add(new Event { Tid = tid, Depth = depth, Time = now, Exit = true, Function = frame.Function, Retval = retval });
                                if (proper || frame.Rsp + 8 > rsp)
                                {
                                    rip = frame.Return;
                                    break;
                                }
                            }
                            SetThreadContext(thread.Handle, context);
                            break;
                        }
                        if (!patches.TryGetValue(address, out var patch)) { status = DBG_EXCEPTION_NOT_HANDLED; break; }
                        {
                            var now = Now();
                            ulong returnAddress;
                            ReadProcessMemory(process, rsp, &returnAddress, 8, out _);
                            string arguments = null;
                            if (o.Args.Count > 0 && Matches(argFuncs, patch.Name) || o.AutoArgs)
                            {
                                var values = new List<string> { Hex(*(ulong*)(context + 0x80)), Hex(*(ulong*)(context + 0x88)), Hex(*(ulong*)(context + 0xB8)), Hex(*(ulong*)(context + 0xC0)) };
                                arguments = string.Join(", ", values);
                            }
                            data.Events.Add(new Event { Tid = tid, Depth = thread.Shadow.Count, Time = now, Function = patch.Name, Args = arguments });
                            thread.Shadow.Push((patch.Name, returnAddress, rsp, now));
                            var slot = trampoline;
                            WriteProcessMemory(process, rsp, &slot, 8, out _);
                            // Run the original first instruction once, then re-insert the breakpoint (single step).
                            var original = patch.Original;
                            WriteProcessMemory(process, address, &original, 1, out _);
                            FlushInstructionCache(process, address, 1);
                            rip = address;
                            *(uint*)(context + 0x44) |= 0x100;   // EFlags.TF
                            thread.Rearm = address;
                            SetThreadContext(thread.Handle, context);
                        }
                        break;
                    }
                }
                ContinueDebugEvent(pi.dwProcessId, tid, status);
            }
        }
        finally
        {
            NativeMemory.AlignedFree(context);
            CloseHandle(pi.hProcess);
        }
        data.ExitCode = exitCode;
        // -F keeps only calls under (and including) matching functions.
        if (filterRegex.Count > 0) data.Events = FilterEvents(data.Events, filterRegex);
        return (data, exitCode);
    }

    static Regex SpecFunction(string spec)
    {
        var at = spec.IndexOf('@');
        return new Regex(at >= 0 ? spec[..at] : spec);
    }

    static string Hex(ulong v) => v < 100000 ? v.ToString(CultureInfo.InvariantCulture) : "0x" + v.ToString("x", CultureInfo.InvariantCulture);

    static List<Event> FilterEvents(List<Event> events, List<Regex> filters)
    {
        var result = new List<Event>();
        foreach (var group in events.GroupBy(e => e.Tid))
        {
            var inside = 0;
            var baseDepth = 0;
            foreach (var e in group)
            {
                if (!e.Exit && inside == 0 && filters.Any(f => f.IsMatch(e.Function))) { inside = 1; baseDepth = e.Depth; result.Add(Rebase(e, baseDepth)); continue; }
                if (inside == 0) continue;
                if (!e.Exit) { inside++; result.Add(Rebase(e, baseDepth)); }
                else { inside--; result.Add(Rebase(e, baseDepth)); }
            }
        }
        return result.OrderBy(e => e.Time).ToList();
    }

    static Event Rebase(Event e, int baseDepth) => new() { Tid = e.Tid, Depth = e.Depth - baseDepth, Time = e.Time, Exit = e.Exit, Function = e.Function, Args = e.Args, Retval = e.Retval };

    static string ResolveExecutable(string name)
    {
        if (File.Exists(name)) return Path.GetFullPath(name);
        if (File.Exists(name + ".exe")) return Path.GetFullPath(name + ".exe");
        var sb = new StringBuilder(1024);
        if (SearchPathW(null, name, ".exe", sb.Capacity, sb, 0) > 0) return sb.ToString();
        throw new InvalidOperationException($"cannot find the program {name}");
    }

    static string Quote(string arg)
    {
        if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return arg;
        var sb = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var c in arg)
        {
            if (c == '\\') { backslashes++; continue; }
            if (c == '"') sb.Append('\\', backslashes * 2 + 1).Append('"');
            else sb.Append('\\', backslashes).Append(c);
            backslashes = 0;
        }
        return sb.Append('\\', backslashes * 2).Append('"').ToString();
    }

    // ---- symbols ----

    // The program's functions: from its PDB through DbgHelp (full symbols, or code publics), else its
    // COFF symbol table (MinGW builds).
    sealed class Collector
    {
        public readonly List<(ulong, string)> Functions = new();
        public List<(ulong Start, ulong End)> Code = new();
    }

    static List<(ulong Address, string Name)> ModuleFunctions(nint process, nint file, ulong baseAddress, string path)
    {
        var collector = new Collector { Code = CodeRanges(path, baseAddress) };
        try
        {
            SymSetOptions(0x2 /* SYMOPT_UNDNAME */ | 0x80000 /* SYMOPT_NO_PROMPTS */);
            if (SymInitializeW(process, null, false))
            {
                if (SymLoadModuleExW(process, file, path, null, baseAddress, 0, 0, 0) != 0)
                {
                    var handle = GCHandle.Alloc(collector);
                    SymEnumSymbolsW(process, baseAddress, "*", &CollectSymbol, GCHandle.ToIntPtr(handle));
                    handle.Free();
                }
                SymCleanup(process);
            }
        }
        catch (DllNotFoundException) { }
        var functions = collector.Functions;
        if (functions.Count == 0) functions.AddRange(CoffFunctions(path, baseAddress));
        // Very large programs: keep the first 20000 (breakpoints cost a debugger round trip each).
        return functions.DistinctBy(f => f.Item1).Take(20000).ToList();
    }

    [UnmanagedCallersOnly]
    static int CollectSymbol(SYMBOL_INFOW* symbol, uint size, nint context)
    {
        var collector = (Collector)GCHandle.FromIntPtr(context).Target!;
        var address = symbol->Address;
        // Functions, and public symbols that point into code (import thunks excluded).
        var name = new string(&symbol->Name, 0, (int)symbol->NameLen).TrimEnd('\0');
        if (address != 0 && (symbol->Tag == 5 /* SymTagFunction */ ||
            symbol->Tag == 10 /* SymTagPublicSymbol */ && !name.StartsWith("_imp_") && !name.StartsWith("__imp_")
            && collector.Code.Any(c => address >= c.Start && address < c.End)))
            collector.Functions.Add((address, name));
        return 1;
    }

    static List<(ulong, ulong)> CodeRanges(string path, ulong baseAddress)
    {
        var ranges = new List<(ulong, ulong)>();
        try
        {
            var image = File.ReadAllBytes(path);
            var pe = BitConverter.ToInt32(image, 0x3c);
            var sections = pe + 24 + BitConverter.ToUInt16(image, pe + 20);
            for (var s = 0; s < BitConverter.ToUInt16(image, pe + 6); s++)
            {
                var at = sections + s * 40;
                if ((BitConverter.ToUInt32(image, at + 36) & 0x20000000) == 0) continue;   // IMAGE_SCN_MEM_EXECUTE
                var start = baseAddress + BitConverter.ToUInt32(image, at + 12);
                ranges.Add((start, start + BitConverter.ToUInt32(image, at + 8)));
            }
        }
        catch (IOException) { }
        return ranges;
    }

    static IEnumerable<(ulong, string)> CoffFunctions(string path, ulong baseAddress)
    {
        var image = File.ReadAllBytes(path);
        var pe = BitConverter.ToInt32(image, 0x3c);
        var sectionCount = BitConverter.ToUInt16(image, pe + 6);
        var symbolTable = BitConverter.ToInt32(image, pe + 12);
        var symbolCount = BitConverter.ToInt32(image, pe + 16);
        var optionalSize = BitConverter.ToUInt16(image, pe + 20);
        if (symbolTable == 0 || symbolCount == 0) yield break;
        var sections = pe + 24 + optionalSize;
        var strings = symbolTable + symbolCount * 18;
        for (var k = 0; k < symbolCount; k++)
        {
            var s = symbolTable + k * 18;
            if (s + 18 > image.Length) yield break;
            var value = BitConverter.ToUInt32(image, s + 8);
            var section = BitConverter.ToInt16(image, s + 12);
            var type = BitConverter.ToUInt16(image, s + 14);
            var storage = image[s + 16];
            var aux = image[s + 17];
            if (section > 0 && section <= sectionCount && (type & 0x30) == 0x20 && storage is 2 or 3)
            {
                string name;
                if (BitConverter.ToUInt32(image, s) == 0)
                {
                    var offset = strings + (int)BitConverter.ToUInt32(image, s + 4);
                    var end = Array.IndexOf(image, (byte)0, offset);
                    name = Encoding.UTF8.GetString(image, offset, end - offset);
                }
                else name = Encoding.UTF8.GetString(image, s, 8).TrimEnd('\0');
                var rva = BitConverter.ToUInt32(image, sections + (section - 1) * 40 + 12);
                yield return (baseAddress + rva + value, name);
            }
            k += aux;
        }
    }

    static List<(ulong, string)> Exports(nint process, ulong baseAddress)
    {
        var result = new List<(ulong, string)>();
        byte[] Read(ulong address, int length)
        {
            var buffer = new byte[length];
            fixed (byte* p = buffer) ReadProcessMemory(process, address, p, (nuint)length, out _);
            return buffer;
        }
        var header = Read(baseAddress, 4096);
        var pe = BitConverter.ToInt32(header, 0x3c);
        if (pe <= 0 || pe > 3800) return result;
        var exportRva = BitConverter.ToUInt32(header, pe + 24 + 112);
        var exportSize = BitConverter.ToUInt32(header, pe + 24 + 116);
        if (exportRva == 0) return result;
        // Only code: forwarders (RVAs inside the export directory) and data exports aren't functions.
        var sections = pe + 24 + BitConverter.ToUInt16(header, pe + 20);
        var code = new List<(uint Start, uint End)>();
        for (var s = 0; s < BitConverter.ToUInt16(header, pe + 6) && sections + s * 40 + 40 <= header.Length; s++)
        {
            var at = sections + s * 40;
            if ((BitConverter.ToUInt32(header, at + 36) & 0x20000000) != 0)   // IMAGE_SCN_MEM_EXECUTE
                code.Add((BitConverter.ToUInt32(header, at + 12), BitConverter.ToUInt32(header, at + 12) + BitConverter.ToUInt32(header, at + 8)));
        }
        var directory = Read(baseAddress + exportRva, 40);
        // ntdll runs the loader, exception dispatch and the debugger's own handshake: never patched.
        var dllName = Read(baseAddress + BitConverter.ToUInt32(directory, 12), 64);
        if (Encoding.ASCII.GetString(dllName, 0, Math.Max(0, Array.IndexOf(dllName, (byte)0))).Equals("ntdll.dll", StringComparison.OrdinalIgnoreCase)) return result;
        var names = BitConverter.ToUInt32(directory, 24);
        var functionsRva = BitConverter.ToUInt32(directory, 28);
        var namesRva = BitConverter.ToUInt32(directory, 32);
        var ordinalsRva = BitConverter.ToUInt32(directory, 36);
        if (names > 65536) return result;
        var nameTable = Read(baseAddress + namesRva, (int)names * 4);
        var ordinals = Read(baseAddress + ordinalsRva, (int)names * 2);
        for (var k = 0; k < names; k++)
        {
            var nameBytes = Read(baseAddress + BitConverter.ToUInt32(nameTable, k * 4), 256);
            var name = Encoding.ASCII.GetString(nameBytes, 0, Math.Max(0, Array.IndexOf(nameBytes, (byte)0)));
            var ordinal = BitConverter.ToUInt16(ordinals, k * 2);
            var function = BitConverter.ToUInt32(Read(baseAddress + functionsRva + (ulong)ordinal * 4, 4), 0);
            if (function >= exportRva && function < exportRva + exportSize) continue;
            if (!code.Any(c => function >= c.Start && function < c.End)) continue;
            result.Add((baseAddress + function, name));
        }
        return result;
    }

    // ---- output ----

    static string Duration(long ns)
    {
        if (ns < 1000000) return $"{ns / 1000.0,7:0.000} us";
        if (ns < 1000000000) return $"{ns / 1e6,7:0.000} ms";
        return $"{ns / 1e9,7:0.000}  s";
    }

    static void Replay(TextWriter w, TraceData data, Options o)
    {

        w.WriteLine("# DURATION     TID     FUNCTION");
        var events = data.Events;
        // Pair entries with exits per thread to know durations and leaves.
        var exitOf = new Dictionary<int, int>();
        foreach (var group in events.Select((e, k) => (e, k)).GroupBy(x => x.e.Tid))
        {
            var stack = new Stack<int>();
            foreach (var (e, k) in group)
                if (!e.Exit) stack.Push(k);
                else if (stack.Count > 0) exitOf[stack.Pop()] = k;
        }
        var skip = new HashSet<int>();
        for (var k = 0; k < events.Count; k++)
        {
            if (skip.Contains(k)) continue;
            var e = events[k];
            if (e.Depth >= o.Depth) continue;
            var indent = new string(' ', e.Depth * 2);
            var tid = $"[{e.Tid,6}]";
            if (!e.Exit)
            {
                var hasExit = exitOf.TryGetValue(k, out var x);
                var duration = hasExit ? events[x].Time - e.Time : 0;
                if (o.TimeFilterNs > 0 && hasExit && duration < o.TimeFilterNs)
                {
                    for (var j = k; j <= x; j++) skip.Add(j);
                    continue;
                }
                var call = $"{e.Function}({e.Args ?? ""})";
                // A leaf: its exit follows right after its entry on this thread.
                var next = events.Skip(k + 1).FirstOrDefault(n => n.Tid == e.Tid);
                if (hasExit && next != null && events[x] == next)
                {
                    w.WriteLine($"{Duration(duration)} {tid} | {indent}{call}{(next.Retval != null ? " = " + next.Retval : "")};");
                    skip.Add(x);
                }
                else w.WriteLine($"{new string(' ', 10)} {tid} | {indent}{call} {{");
            }
            else
            {
                var entry = exitOf.FirstOrDefault(p => p.Value == k);
                var duration = exitOf.ContainsValue(k) ? e.Time - events[entry.Key].Time : 0;
                w.WriteLine($"{Duration(duration)} {tid} | {indent}}}{(e.Retval != null ? " = " + e.Retval + ";" : "")} /* {e.Function} */");
            }
        }
    }

    static void Report(TraceData data, Options o)
    {
        var stats = new Dictionary<string, (long Total, long Self, int Calls)>();
        foreach (var group in data.Events.GroupBy(e => e.Tid))
        {
            var stack = new Stack<(Event Entry, long Children)>();
            foreach (var e in group)
            {
                if (!e.Exit) { stack.Push((e, 0)); continue; }
                if (stack.Count == 0) continue;
                var (entry, children) = stack.Pop();
                var total = e.Time - entry.Time;
                var s = stats.GetValueOrDefault(entry.Function);
                stats[entry.Function] = (s.Total + total, s.Self + total - children, s.Calls + 1);
                if (stack.Count > 0) { var parent = stack.Pop(); stack.Push((parent.Entry, parent.Children + total)); }
            }
        }
        Console.WriteLine("  Total time   Self time       Calls  Function");
        Console.WriteLine("  ==========  ==========  ==========  ====================");
        foreach (var (name, s) in stats.OrderByDescending(kv => kv.Value.Total))
            Console.WriteLine($"  {Duration(s.Total)}  {Duration(s.Self)}  {s.Calls,10}  {name}");
    }

    static void Graph(TraceData data, string root)
    {
        // Aggregate call paths: (path) -> (calls, total time).
        var nodes = new Dictionary<string, (int Calls, long Time, int Depth, string Name)>();
        var order = new List<string>();
        foreach (var group in data.Events.GroupBy(e => e.Tid))
        {
            var stack = new Stack<(string Path, long Time)>();
            foreach (var e in group)
            {
                if (!e.Exit)
                {
                    var path = (stack.Count > 0 ? stack.Peek().Path + "/" : "") + e.Function;
                    stack.Push((path, e.Time));
                    if (!nodes.ContainsKey(path)) { nodes[path] = (0, 0, stack.Count - 1, e.Function); order.Add(path); }
                    continue;
                }
                if (stack.Count == 0) continue;
                var (p, t) = stack.Pop();
                var n = nodes[p];
                nodes[p] = (n.Calls + 1, n.Time + (e.Time - t), n.Depth, n.Name);
            }
        }
        Console.WriteLine($"# Function Call Graph for '{root ?? data.Exe}' (session: windows)");
        Console.WriteLine("========== FUNCTION CALL GRAPH ==========");
        Console.WriteLine("# TOTAL TIME   FUNCTION");
        foreach (var path in order)
        {
            var n = nodes[path];
            if (root != null && !path.Split('/').Contains(root)) continue;
            Console.WriteLine($"  {Duration(n.Time)} : {(n.Depth == 0 ? "" : new string(' ', n.Depth - 1) + " +-")}({n.Calls}) {n.Name}");
        }
    }

    static void Dump(TraceData data)
    {
        Console.WriteLine($"uftrace file header: windows, {data.Events.Count} records");
        foreach (var e in data.Events)
            Console.WriteLine($"{e.Time / 1e9,14:0.000000000} {e.Tid,6}: [{(e.Exit ? "exit " : "entry")}] {e.Function} depth: {e.Depth}{(e.Args != null ? " args: " + e.Args : "")}{(e.Retval != null ? " retval: " + e.Retval : "")}");
    }

    // ---- uftrace.data ----

    static void Save(string dir, Options o, TraceData data)
    {
        Directory.CreateDirectory(dir);
        using (var w = new StreamWriter(Path.Combine(dir, "events.txt"), false, new UTF8Encoding(false)))
            foreach (var e in data.Events)
                w.WriteLine(string.Join('\t', e.Tid, e.Time, e.Exit ? "X" : "E", e.Depth, e.Function, e.Args ?? "", e.Retval ?? ""));
        File.WriteAllText(Path.Combine(dir, "info"), string.Join('\n',
            "exename:" + data.Exe, "cmdline:" + data.CommandLine, "exit_status:" + data.ExitCode, "record_date:" + data.Started.ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture),
            "patched:" + data.Patched, "threads:" + data.Events.Select(e => e.Tid).Distinct().Count(), "version:" + Version) + "\n");
    }

    static TraceData Load(string dir)
    {
        var path = Path.Combine(dir, "events.txt");
        if (!File.Exists(path)) throw new InvalidOperationException($"cannot open record data: {dir}: No such file or directory");
        var data = new TraceData();
        foreach (var line in File.ReadAllLines(path))
        {
            var f = line.Split('\t');
            if (f.Length < 7) continue;
            data.Events.Add(new Event
            {
                Tid = int.Parse(f[0]), Time = long.Parse(f[1]), Exit = f[2] == "X", Depth = int.Parse(f[3]), Function = f[4],
                Args = f[5].Length > 0 ? f[5] : null, Retval = f[6].Length > 0 ? f[6] : null,
            });
        }
        var info = Path.Combine(dir, "info");
        if (File.Exists(info))
            foreach (var l in File.ReadAllLines(info))
                if (l.StartsWith("exename:")) data.Exe = l[8..];
        return data;
    }

    static void Info(string dir)
    {
        var info = Path.Combine(dir, "info");
        if (!File.Exists(info)) throw new InvalidOperationException($"cannot open record data: {dir}: No such file or directory");
        Console.WriteLine("# system information");
        Console.WriteLine("# ==================");
        Console.WriteLine($"# program version     : uftrace {Version}");
        Console.WriteLine($"# machine info        : Windows {Environment.OSVersion.Version} x86_64");
        Console.WriteLine($"# number of CPUs      : {Environment.ProcessorCount}");
        Console.WriteLine();
        Console.WriteLine("# process information");
        Console.WriteLine("# ===================");
        foreach (var line in File.ReadAllLines(info))
        {
            var colon = line.IndexOf(':');
            if (colon < 0) continue;
            var key = line[..colon] switch
            {
                "exename" => "program path", "cmdline" => "cmdline", "exit_status" => "exit status", "record_date" => "record date",
                "patched" => "patched functions", "threads" => "number of tasks", _ => null,
            };
            if (key != null) Console.WriteLine($"# {key,-19} : {line[(colon + 1)..]}");
        }
    }

    // ---- Win32 ----

    const uint DEBUG_ONLY_THIS_PROCESS = 0x2, INFINITE = 0xFFFFFFFF, DBG_CONTINUE = 0x00010002, DBG_EXCEPTION_NOT_HANDLED = 0x80010001;
    const uint CONTEXT_FULL = 0x10000B, MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, PAGE_EXECUTE_READWRITE = 0x40;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFOW
    {
        public int cb;
        public nint lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public nint lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public nint hProcess, hThread; public int dwProcessId, dwThreadId; }

    [StructLayout(LayoutKind.Sequential)]
    struct SYMBOL_INFOW
    {
        public uint SizeOfStruct, TypeIndex;
        public ulong Reserved0, Reserved1;
        public uint Index, Size;
        public ulong ModBase;
        public uint Flags;
        public ulong Value, Address;
        public uint Register, Scope, Tag, NameLen, MaxNameLen;
        public char Name;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcessW(string application, char* commandLine, nint processAttributes, nint threadAttributes,
        bool inheritHandles, uint flags, nint environment, string directory, STARTUPINFOW* startup, PROCESS_INFORMATION* info);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WaitForDebugEvent(byte* debugEvent, uint ms);

    [DllImport("kernel32.dll")]
    static extern bool ContinueDebugEvent(int pid, int tid, uint status);

    [DllImport("kernel32.dll")]
    static extern bool ReadProcessMemory(nint process, ulong address, void* buffer, nuint size, out nuint read);

    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(nint process, ulong address, void* buffer, nuint size, out nuint written);

    [DllImport("kernel32.dll")]
    static extern bool FlushInstructionCache(nint process, ulong address, nuint size);

    [DllImport("kernel32.dll")]
    static extern ulong VirtualAllocEx(nint process, ulong address, nuint size, uint type, uint protect);

    [DllImport("kernel32.dll")]
    static extern bool GetThreadContext(nint thread, byte* context);

    [DllImport("kernel32.dll")]
    static extern bool SetThreadContext(nint thread, byte* context);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(nint handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int SearchPathW(string path, string fileName, string extension, int bufferLength, StringBuilder buffer, nint filePart);

    [DllImport("dbghelp.dll")]
    static extern uint SymSetOptions(uint options);

    [DllImport("dbghelp.dll", CharSet = CharSet.Unicode)]
    static extern bool SymInitializeW(nint process, string searchPath, bool invadeProcess);

    [DllImport("dbghelp.dll", CharSet = CharSet.Unicode)]
    static extern ulong SymLoadModuleExW(nint process, nint file, string imageName, string moduleName, ulong baseOfDll, uint dllSize, nint data, uint flags);

    [DllImport("dbghelp.dll", CharSet = CharSet.Unicode)]
    static extern bool SymEnumSymbolsW(nint process, ulong baseOfDll, string mask, delegate* unmanaged<SYMBOL_INFOW*, uint, nint, int> callback, nint context);

    [DllImport("dbghelp.dll")]
    static extern bool SymCleanup(nint process);
}
