namespace Brew.Ruby;

// Argument definitions for Homebrew's user commands (ported from Library/Homebrew/cmd/*.rb `cmd_args`),
// so `brew <cmd> --help` and usage errors match Homebrew exactly. See DevCommands.TryHelp.
public static partial class UserCommandHelp
{
    static readonly Dictionary<string, Func<CommandParser>> Table = new();
    static readonly Dictionary<string, string> AliasTable = new();

    static UserCommandHelp() => Register();

    static partial void Register();

    static void Add(string name, Func<CommandParser> parser, params string[] aliases)
    {
        Table[name] = parser;
        foreach (var a in aliases) AliasTable[a] = name;
    }

    public static string Resolve(string command) => AliasTable.GetValueOrDefault(command, command);

    public static CommandParser Parser(string command) =>
        Table.TryGetValue(Resolve(command), out var f) ? f() : null;
}
