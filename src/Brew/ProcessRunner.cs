using System.Diagnostics;

namespace Brew;

public static class ProcessRunner
{
    public static async Task<(int Code, string Output, string Error)> Capture(string file, IEnumerable<string> args, string directory = null)
    {
        var start = new ProcessStartInfo(file) { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory ?? Environment.CurrentDirectory };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_PAGER"] = "cat";
        start.Environment["GH_PROMPT_DISABLED"] = "1";
        using var process = Process.Start(start);
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await output, await error);
    }
    public static async Task<int> Run(string file, IEnumerable<string> args, string directory = null)
    {
        var result = await Capture(file, args, directory);
        Console.Write(result.Output); Console.Error.Write(result.Error);
        return result.Code;
    }
    public static async Task<string> Git(string directory, params string[] args)
    {
        var result = await Capture("git", new[] { "-C", directory }.Concat(args));
        if (result.Code != 0) throw new InvalidOperationException(result.Error.Trim());
        return result.Output.TrimEnd();
    }
}
