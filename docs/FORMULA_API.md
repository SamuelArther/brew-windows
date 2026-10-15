# Formula API — Codex / Claude contract

Codex owns src/Brew/Formulae and everything outside src/Brew/Ruby.
Claude owns src/Brew/Ruby only. Do not change each other's files.

Claude: implement `public static Brew.Formulae.FormulaDefinition
Brew.Ruby.FormulaLoader.Load(string path)` (throws on invalid Ruby).
Codex supplies these concrete types in src/Brew/Formulae/FormulaDefinition.cs:

```csharp
namespace Brew.Formulae;
public sealed class FormulaDefinition {
  public string Name { get; set; }
  public string Version { get; set; }
  public string Url { get; set; }
  public string Sha256 { get; set; }
  public string Description { get; set; }
  public string Homepage { get; set; }
  public string License { get; set; }
  public Action<InstallContext> Install { get; set; }
}
public sealed class InstallContext {
  public string Prefix { get; } // final Cellar/name/version
  public string BuildPath { get; } // downloaded file or extracted archive directory
  public string Bin { get; } // Prefix/bin
  public void InstallBin(string source, string destinationName = null);
}
```

Load evaluates class metadata, and returns an Install callback which executes
`def install` against the context. Name defaults to the .rb filename.
Required metadata: version, url, sha256. No network or installs during Load.
Exceptions propagate as user-facing CLI errors.

Initial formula subset (jq.rb):
```ruby
class Jq < Formula
  desc "Lightweight and flexible command-line JSON processor"
  homepage "https://jqlang.org/"
  version "1.8.2"
  url "https://github.com/jqlang/jq/releases/download/jq-1.8.2/jq-windows-amd64.exe"
  sha256 "<64 lowercase hex characters>"
  license "MIT"

  def install
    bin.install "jq-windows-amd64.exe" => "jq.exe"
  end
end
```
`bin.install` string and hash forms copy from BuildPath via InstallBin.
No process-global current-directory changes required. Runtime should bind prefix,
bin and buildpath to this context. Path methods can delegate to System.IO.

The CLI discovers FormulaLoader through its exact fully-qualified type name to
allow the initial non-Ruby code to compile while Claude implements the loader.
Once present, the loader is always used; there is no alternate formula parser.
