using System.Runtime.CompilerServices;

namespace WinTools;

// wait_on: wait for something to happen to files or directories, then exit (FreeBSD's kqueue(2)
// EVFILT_VNODE tool). Here ReadDirectoryChangesW (FileSystemWatcher) watches each path's folder and the
// change is classified into kqueue's notes: a file's content written/extended, its attributes or ACL
// changed, it was deleted or renamed; a folder is "written" when entries are added, removed or renamed.
// The exit status says what happened (or, with -i, to which argument), 0 meaning the timeout expired.
static class WaitOn
{
    const int ExitTimeout = 0, ExitDelete = 1, ExitWrite = 2, ExitExtend = 3, ExitAttrib = 4, ExitRename = 6;
    const int ExUsage = 64, ExNoInput = 66;

    [ModuleInitializer]
    internal static void Register() => Commands.Add("wait_on", "Wait for files or directories to change (kqueue-style)", Run);

    [Flags]
    internal enum Note { None = 0, Delete = 1, Write = 2, Extend = 4, Attrib = 8, Rename = 32 }

    sealed record Target(int Index, string Argument, string FullPath, bool IsDirectory, long Length, DateTime Written, FileAttributes Attributes);

    static int Run(string[] args)
    {
        bool human = false, which = false, writesOnly = false;
        int? timeout = null;
        var paths = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (paths.Count > 0 || arg == "-" || !arg.StartsWith('-')) { paths.Add(arg); continue; }
            if (arg == "--") { paths.AddRange(args[(i + 1)..]); break; }
            if (arg == "--help") { Usage(Console.Out); return 0; }
            // getopt-style clustered flags: -hiw, -t5, -t 5
            for (var j = 1; j < arg.Length; j++)
            {
                switch (arg[j])
                {
                    case 'c' or 'v':
                        Console.WriteLine("wait_on version 1.1");
                        return 0;
                    case 'h': human = true; break;
                    case 'i': which = true; break;
                    case 'w': writesOnly = true; break;
                    case 't':
                        var value = j + 1 < arg.Length ? arg[(j + 1)..] : ++i < args.Length ? args[i] : null;
                        if (!int.TryParse(value, out var seconds) || seconds < 0)
                        {
                            Console.Error.WriteLine("wait_on: invalid timeout");
                            Usage(Console.Error);
                            return ExUsage;
                        }
                        timeout = seconds;
                        j = arg.Length;
                        break;
                    default:
                        Console.Error.WriteLine($"wait_on: illegal option -- {arg[j]}");
                        Usage(Console.Error);
                        return ExUsage;
                }
            }
        }
        if (paths.Count == 0) { Usage(Console.Error); return ExUsage; }
        if (which && paths.Count >= ExUsage)
            Console.Error.WriteLine($"wait_on: -i specified with >= {ExUsage} files - exit code may be ambiguous");

        var targets = new List<Target>();
        for (var i = 0; i < paths.Count; i++)
        {
            var full = Path.GetFullPath(paths[i]);
            if (Directory.Exists(full))
                targets.Add(new Target(i + 1, paths[i], full.TrimEnd('\\'), true, 0, default, File.GetAttributes(full)));
            else if (File.Exists(full))
            {
                var info = new FileInfo(full);
                targets.Add(new Target(i + 1, paths[i], full, false, info.Length, info.LastWriteTimeUtc, info.Attributes));
            }
            else
            {
                Console.Error.WriteLine($"wait_on: can't open \"{paths[i]}\" for reading: No such file or directory");
                return ExNoInput;
            }
        }

        var fired = new TaskCompletionSource<(Target Target, Note Notes)>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Fire(Target t, Note notes)
        {
            if (writesOnly) notes &= Note.Write;
            if (notes != Note.None) fired.TrySetResult((t, notes));
        }

        var watchers = new List<FileSystemWatcher>();
        try
        {
            foreach (var t in targets)
            {
                // The path itself (deleted, renamed, attributes, content) is seen from its parent folder.
                var parent = Path.GetDirectoryName(t.FullPath);
                if (parent != null)
                {
                    var name = Path.GetFileName(t.FullPath);
                    var w = new FileSystemWatcher(parent, name)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                                     | NotifyFilters.Size | NotifyFilters.Attributes | NotifyFilters.Security,
                    };
                    w.Changed += (_, e) => Fire(t, Classify(t));
                    w.Deleted += (_, _) => Fire(t, Note.Delete);
                    w.Renamed += (_, e) =>
                    {
                        if (string.Equals(e.OldFullPath, t.FullPath, StringComparison.OrdinalIgnoreCase)) Fire(t, Note.Rename);
                    };
                    watchers.Add(w);
                }
                // A folder is written when its entries change.
                if (t.IsDirectory)
                {
                    var w = new FileSystemWatcher(t.FullPath) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName };
                    w.Created += (_, _) => Fire(t, Note.Write);
                    w.Deleted += (_, _) => Fire(t, Note.Write);
                    w.Renamed += (_, _) => Fire(t, Note.Write);
                    watchers.Add(w);
                }
            }
            foreach (var w in watchers) w.EnableRaisingEvents = true;

            var wait = timeout is { } s ? fired.Task.Wait(TimeSpan.FromSeconds(s)) : fired.Task.Wait(Timeout.Infinite);
            if (!wait) return ExitTimeout;
            var (target, notes) = fired.Task.Result;
            if (human) Console.WriteLine(Describe(target.Argument, notes));
            return which ? target.Index : WhatHappened(notes);
        }
        finally
        {
            foreach (var w in watchers) w.Dispose();
        }
    }

    // A Changed event on the path: content (size or write time moved) or only metadata.
    static Note Classify(Target t)
    {
        try
        {
            if (t.IsDirectory) return Note.Attrib;
            var info = new FileInfo(t.FullPath);
            if (!info.Exists) return Note.Delete;
            var notes = Note.None;
            if (info.Length != t.Length || info.LastWriteTimeUtc != t.Written) notes |= Note.Write;
            if (info.Length > t.Length) notes |= Note.Extend;
            if (notes == Note.None) notes = Note.Attrib;
            return notes;
        }
        catch (IOException) { return Note.Write; }
        catch (UnauthorizedAccessException) { return Note.Attrib; }
    }

    // The exit status for the first of kqueue's notes that applies, in wait_on's order.
    internal static int WhatHappened(Note notes) =>
        notes.HasFlag(Note.Delete) ? ExitDelete
        : notes.HasFlag(Note.Write) ? ExitWrite
        : notes.HasFlag(Note.Extend) ? ExitExtend
        : notes.HasFlag(Note.Attrib) ? ExitAttrib
        : notes.HasFlag(Note.Rename) ? ExitRename
        : ExitTimeout;

    internal static string Describe(string path, Note notes)
    {
        var text = path + ":";
        if (notes.HasFlag(Note.Delete)) text += " deleted";
        if (notes.HasFlag(Note.Write)) text += " written";
        if (notes.HasFlag(Note.Extend)) text += " extended";
        if (notes.HasFlag(Note.Attrib)) text += " attributed";
        if (notes.HasFlag(Note.Rename)) text += " renamed";
        return text;
    }

    static void Usage(TextWriter w) => w.WriteLine("usage: wait_on [-chivw] [-t timeout] file1 [...]");
}
