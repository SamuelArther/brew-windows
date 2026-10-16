using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace WinTools;

// objc-run: compile an Objective-C file with clang and run it, script-style (a #! first line is
// ignored). On Windows, Foundation comes from GNUstep: the gnustep-base formula's MSYS2 tree,
// GNUSTEP_ROOT, or a standard GNUstep/MSYS2 install. Like the original, the program is built next
// to the source (refusing to overwrite an existing file), run with the remaining arguments, and
// deleted. Embedded Podfiles need CocoaPods and xcodebuild, which only exist on macOS.
static class ObjcRun
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("objc-run", "Use Objective-C files for shell script-like tasks (clang + GNUstep)", Run);
    }

    const string Usage = """
        objc-run usage: objc-run file.m [params]

        Compiles file.m with clang (-fobjc-arc, Foundation from GNUstep), runs it with params, and
        removes the executable again. A #! first line is ignored, so scripts can start with
          #!/usr/bin/env objc-run

        Environment:
          OBJC_RUN_CC    clang to use (default: clang on PATH)
          GNUSTEP_ROOT   GNUstep install holding include/Foundation (default: the gnustep-base
                         formula, C:\GNUstep or C:\msys64\ucrt64)
        """;

    static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("objc-run usage: objc-run file.m [params]");
            return 1;
        }
        if (args[0] is "-h" or "--help") { Console.WriteLine(Usage); return 0; }
        var source = args[0];
        if (!File.Exists(source)) throw new InvalidOperationException($"{source}: No such file");
        var lines = File.ReadAllLines(source);
        if (lines.Any(l => l.Contains("podfile-start")))
        {
            Console.Error.WriteLine("objc-run: embedded Podfiles need CocoaPods and xcodebuild, which only run on macOS; " +
                                    "build the pod's sources into the script instead");
            return 1;
        }

        // The executable goes next to the source, named after it; never clobber an existing file.
        var dir = Path.GetDirectoryName(source);
        var appName = Path.GetFileNameWithoutExtension(source);
        var target = Path.Combine(dir, appName + ".exe");
        if (File.Exists(target) || File.Exists(Path.Combine(dir, appName)))
        {
            Console.Error.WriteLine($"objc-run: file already exists at {(dir.Length == 0 ? "" : dir.Replace('\\', '/'))}/{appName} ... exiting");
            return 1;
        }

        var clang = FindClang() ?? throw new InvalidOperationException(
            "clang not found; install LLVM (brew install llvm, or winget install MartinStorsjo.LLVM-MinGW.UCRT) or set OBJC_RUN_CC");
        // Every Objective-C program needs the runtime (libobjc2) and Foundation, both from GNUstep.
        var gnustep = FindGnustep() ?? throw new InvalidOperationException(
            "GNUstep (Foundation and the Objective-C runtime) not found; brew install gnustep-base, or set GNUSTEP_ROOT");

        // Like `awk 'NR>1 || !/^#!/'`: drop a shebang, keeping the line so diagnostics line up. GNUstep
        // has no module maps, so `@import Foundation;` becomes the umbrella header it stands for.
        if (lines.Length > 0 && lines[0].StartsWith("#!")) lines[0] = "";
        for (var i = 0; i < lines.Length; i++)
            lines[i] = Regex.Replace(lines[i], @"^\s*@import\s+(\w+)\s*;", "#import <$1/$1.h>");
        var temp = Path.Combine(Path.GetTempPath(), $"objc-run-{Environment.ProcessId}-{appName}.m");
        File.WriteAllLines(temp, lines);
        try
        {
            var cc = new List<string>
            {
                "-o", target, "-Wall", "-std=gnu99", "-fobjc-arc", "-fobjc-runtime=gnustep-2.0", "-fblocks", "-fobjc-exceptions",
                "-DGNUSTEP", "-DGNUSTEP_BASE_LIBRARY=1", "-DGNU_RUNTIME=1", "-D_NATIVE_OBJC_EXCEPTIONS", "-DGSWARN", "-DGSDIAGNOSE",
                "-I" + Path.Combine(gnustep, "include"), "-L" + Path.Combine(gnustep, "lib"),
                "-x", "objective-c", temp, "-lgnustep-base", "-lobjc",
            };
            if (Exec(clang, cc, null) != 0)
            {
                Console.Error.WriteLine("objc-run: clang returned with error");
                return 1;
            }
        }
        finally { File.Delete(temp); }

        try
        {
            // GNUstep's DLLs (and the runtime libraries next to them) go on the program's PATH.
            var path = Path.Combine(gnustep, "bin") + ";" + Environment.GetEnvironmentVariable("PATH");
            // The program's exit status becomes objc-run's.
            return Exec(Path.GetFullPath(target), args[1..], path);
        }
        finally
        {
            try { File.Delete(target); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    static int Exec(string file, IEnumerable<string> args, string path)
    {
        var psi = new ProcessStartInfo(file) { UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        if (path != null) psi.Environment["PATH"] = path;
        using var p = Process.Start(psi);
        p.WaitForExit();
        return p.ExitCode;
    }

    static string FindClang()
    {
        var configured = Environment.GetEnvironmentVariable("OBJC_RUN_CC");
        if (!string.IsNullOrEmpty(configured)) return configured;
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim('"'), "clang.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException) { }
        }
        return null;
    }

    // A GNUstep root is any directory with include/Foundation/Foundation.h (MSYS2 prefixes such as
    // ucrt64/ count, which is how the gnustep-base formula lays out its libexec).
    static string FindGnustep()
    {
        var roots = new List<string>();
        var env = Environment.GetEnvironmentVariable("GNUSTEP_ROOT");
        if (!string.IsNullOrEmpty(env)) roots.Add(env);
        foreach (var prefix in BrewPrefixes())
            roots.Add(Path.Combine(prefix, "opt", "gnustep-base", "libexec"));
        roots.AddRange([@"C:\GNUstep", @"C:\GNUstep\x64\Release", @"C:\msys64"]);
        foreach (var root in roots)
            foreach (var sub in new[] { "", "ucrt64", "clang64", "mingw64", "Local", "System" })
            {
                var candidate = sub.Length == 0 ? root : Path.Combine(root, sub);
                if (File.Exists(Path.Combine(candidate, "include", "Foundation", "Foundation.h"))) return Path.GetFullPath(candidate);
            }
        return null;
    }

    static IEnumerable<string> BrewPrefixes()
    {
        var env = Environment.GetEnvironmentVariable("HOMEBREW_PREFIX");
        if (!string.IsNullOrEmpty(env)) yield return env;
        // wintools.exe itself lives in <prefix>/Cellar/<formula>/<version>/libexec.
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d?.Parent != null; d = d.Parent)
            if (d.Name.Equals("Cellar", StringComparison.OrdinalIgnoreCase)) { yield return d.Parent.FullName; break; }
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Homebrew");
    }
}
