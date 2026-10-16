namespace Brew;

public static class CommandOutput
{
    public static bool Quiet { get; set; }
    public static bool Verbose { get; set; }
    public static bool Color => Environment.GetEnvironmentVariable("NO_COLOR") == null
        && Environment.GetEnvironmentVariable("HOMEBREW_NO_COLOR") == null
        && (!Console.IsOutputRedirected || Environment.GetEnvironmentVariable("HOMEBREW_COLOR") != null);
    public static string Bold(string value) => Color ? $"\u001b[1m{value}\u001b[0m" : value;
    public static string Identifier(string value) => Color ? $"\u001b[32m{value}\u001b[39m" : value;
    public static void Header(string value, bool green = false, bool error = false)
    {
        if (!Quiet) (error ? Console.Error : Console.Out).WriteLine(Color ? $"\u001b[{(green ? 32 : 34)}m==>\u001b[0m \u001b[1m{value}\u001b[0m" : "==> " + value);
    }
    public static void Warning(string value) => Console.Error.WriteLine((Color ? "\u001b[33mWarning:\u001b[0m " : "Warning: ") + value);
    public static void Error(string value) => Console.Error.WriteLine((Color ? "\u001b[31mError:\u001b[0m " : "Error: ") + value);
    public static string Badge => Environment.GetEnvironmentVariable("HOMEBREW_NO_EMOJI") != null ? ""
        : (Environment.GetEnvironmentVariable("HOMEBREW_INSTALL_BADGE") ?? "🍺") + "  ";
    public static string Size(double bytes)
    {
        var units = new[] { "B", "KB", "MB", "GB" }; var index = 0;
        while (Math.Round(bytes, 1) >= 1000 && index < units.Length - 1) { bytes /= 1000; index++; }
        return $"{bytes:0.#}{units[index]}";
    }
}
