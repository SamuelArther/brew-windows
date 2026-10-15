using Brew.Formulae;

// Test double for the interpreter boundary. Never included in the brew executable.
namespace Brew.Ruby;

public static class FormulaLoader
{
    public static FormulaDefinition Load(string path) => new()
    {
        Name = "jq",
        Version = "1.8.2",
        Url = "https://github.com/jqlang/jq/releases/download/jq-1.8.2/jq-windows-amd64.exe",
        Sha256 = "a6fc67fedaf9128a3309a1e2ebb8b986aeccf70122ee46d2cb4849e423f0c627",
        Install = context => context.InstallBin("jq-windows-amd64.exe", "jq.exe")
    };
}
