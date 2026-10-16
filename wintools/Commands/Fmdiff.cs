using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace WinTools;

// fmdiff / fmdiff3 / fmresolve / fmmerge (fmscripts): use a graphical merge tool as the diff and
// merge command of Subversion, Mercurial and git. FileMerge's place is taken by WinMerge; a program
// named `filemerge` on PATH wins over it and is called with FileMerge's -left/-right/-ancestor/-merge.
static class Fmdiff
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("fmdiff", "Show a diff in WinMerge (svn --diff-cmd, hg extdiff, git difftool)", Diff);
        Commands.Add("fmdiff3", "Three-way merge in WinMerge, result to stdout (svn --diff3-cmd)", Diff3);
        Commands.Add("fmresolve", "Resolve a Subversion conflict in WinMerge", Resolve);
        Commands.Add("fmmerge", "Merge local/base/other in WinMerge into local (hg merge tool)", Merge);
    }

    // FileMerge-compatible `filemerge` on PATH, else WinMerge (PATH, App Paths, Program Files).
    static (string Path, bool FileMerge)? Tool()
    {
        var exts = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries);
        var path = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
        string Find(string name) =>
            path.SelectMany(dir => exts.Select(ext => Path.Combine(dir.Trim('"'), name + ext.ToLowerInvariant()))).FirstOrDefault(File.Exists);
        if (Find("filemerge") is { } fm) return (fm, true);
        if (Find("WinMergeU") is { } wm) return (wm, false);
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var key = hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\WinMergeU.exe");
            if (key?.GetValue(null) is string registered && File.Exists(registered.Trim('"'))) return (registered.Trim('"'), false);
        }
        foreach (var root in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.LocalApplicationData })
        {
            var candidate = Path.Combine(Environment.GetFolderPath(root), root == Environment.SpecialFolder.LocalApplicationData ? @"Programs\WinMerge" : "WinMerge", "WinMergeU.exe");
            if (File.Exists(candidate)) return (candidate, false);
        }
        return null;
    }

    static int Launch(string left, string right, string ancestor, string output, string leftLabel = null, string rightLabel = null)
    {
        var tool = Tool();
        if (tool == null)
        {
            Console.Error.WriteLine("fmdiff: WinMerge not found (winget install WinMerge.WinMerge), and no `filemerge` program on PATH");
            return 1;
        }
        var psi = new ProcessStartInfo(tool.Value.Path) { UseShellExecute = false };
        var a = psi.ArgumentList;
        if (tool.Value.FileMerge)
        {
            a.Add("-left"); a.Add(left);
            a.Add("-right"); a.Add(right);
            if (ancestor != null) { a.Add("-ancestor"); a.Add(ancestor); }
            if (output != null) { a.Add("-merge"); a.Add(output); }
        }
        else
        {
            // /e: Esc closes, /u: not in MRU, /fl..: labels; three-way puts the ancestor in the middle.
            a.Add("/e"); a.Add("/u");
            if (output == null) { a.Add("/wl"); a.Add("/wr"); }
            if (leftLabel != null) { a.Add("/dl"); a.Add(leftLabel); }
            if (ancestor != null) { a.Add("/dm"); a.Add("base"); }
            if (rightLabel != null) { a.Add("/dr"); a.Add(rightLabel); }
            a.Add(left);
            if (ancestor != null) a.Add(ancestor);
            a.Add(right);
            if (output != null) { a.Add("/o"); a.Add(output); }
        }
        using var p = Process.Start(psi);
        p.WaitForExit();
        return 0;
    }

    static bool Help(string[] args, string usage)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Console.WriteLine(usage); return true; }
        if (args.Length > 0 && args[0] == "--version") { Console.WriteLine($"fmscripts {Commands.Version} (wintools)"); return true; }
        return false;
    }

    // svn calls diff-cmd as: -u -L "left label" -L "right label" left right (diff-style options first).
    static int Diff(string[] args)
    {
        if (Help(args, "Usage: fmdiff [-u] [-L left-label] [-L right-label] left right\nShows the difference in WinMerge; works as svn --diff-cmd, hg extdiff -p and git difftool -x.")) return 0;
        var labels = new List<string>();
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "-L" && i + 1 < args.Length) labels.Add(args[++i]);
            else if (args[i].StartsWith('-') && args[i].Length > 1) continue;    // -u, -E, -b ... from svn
            else files.Add(args[i]);
        }
        if (files.Count < 2) throw new UsageException("need two files to compare");
        return Launch(files[^2], files[^1], null, null, labels.ElementAtOrDefault(0), labels.ElementAtOrDefault(1));
    }

    // svn calls diff3-cmd as: -E -m -L mine -L older -L yours mine older yours, and reads the merge from stdout.
    static int Diff3(string[] args)
    {
        if (Help(args, "Usage: fmdiff3 [-E] [-m] [-L label]... mine older yours\nThree-way merge in WinMerge; the merged result is written to standard output (svn --diff3-cmd).")) return 0;
        var labels = new List<string>();
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "-L" && i + 1 < args.Length) labels.Add(args[++i]);
            else if (args[i].StartsWith('-') && args[i].Length > 1) continue;
            else files.Add(args[i]);
        }
        if (files.Count < 3) throw new UsageException("need mine, older and yours");
        var (mine, older, yours) = (files[^3], files[^2], files[^1]);
        var output = Path.Combine(Path.GetTempPath(), $"fmdiff3-{Environment.ProcessId}.tmp");
        File.Copy(mine, output, overwrite: true);
        try
        {
            var status = Launch(mine, yours, older, output, labels.ElementAtOrDefault(0), labels.ElementAtOrDefault(2));
            if (status != 0) return status;
            using var stdout = Console.OpenStandardOutput();
            stdout.Write(File.ReadAllBytes(output));
            return 0;
        }
        finally { File.Delete(output); }
    }

    // fmresolve file: merges file.mine with the newest file.rN against the oldest file.rN into file.
    static int Resolve(string[] args)
    {
        if (Help(args, "Usage: fmresolve file...\nResolves Subversion conflicts in WinMerge: FILE.mine and the newest FILE.rN, with the oldest FILE.rN as base, merged into FILE.")) return 0;
        if (args.Length == 0) throw new UsageException("Usage: fmresolve file...");
        foreach (var file in args)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(file));
            var name = Path.GetFileName(file);
            var revisions = Directory.EnumerateFiles(dir, name + ".r*")
                .Select(p => (Path: p, Rev: long.TryParse(Path.GetFileName(p)[(name.Length + 2)..], out var r) ? r : -1))
                .Where(x => x.Rev >= 0).OrderBy(x => x.Rev).ToList();
            var mine = Path.Combine(dir, name + ".mine");
            if (!File.Exists(mine) || revisions.Count < 2) throw new IOException($"{file}: not in conflict (no {name}.mine and {name}.rN files)");
            var status = Launch(mine, revisions[^1].Path, revisions[0].Path, Path.GetFullPath(file), "mine", $"r{revisions[^1].Rev}");
            if (status != 0) return status;
        }
        return 0;
    }

    // hg merge-tools: fmmerge local base other (the result replaces local).
    static int Merge(string[] args)
    {
        if (Help(args, "Usage: fmmerge local base other\nThree-way merge in WinMerge; the result is written to LOCAL (Mercurial merge tool, git mergetool).")) return 0;
        if (args.Length != 3) throw new UsageException("Usage: fmmerge local base other");
        return Launch(args[0], args[2], args[1], Path.GetFullPath(args[0]), "local", "other");
    }
}
