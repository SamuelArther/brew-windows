using Brew;
using System.Text;

Console.OutputEncoding = new UTF8Encoding(false);
// `brew update` leaves the previous brew.exe behind as brew.exe.old (it can't delete itself while running).
try { if (Environment.ProcessPath is { } exe && File.Exists(exe + ".old")) File.Delete(exe + ".old"); }
catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
return await Cli.RunAsync(args);
