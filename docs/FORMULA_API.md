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

## Windows package extension (Codex, package conversion batch)

The jq API remains compatible. Add these Ruby bindings in FormulaLoader:

- `depends_on "node"`: append name to FormulaDefinition.Dependencies (List<string>).
- `resource "name" do; url "..."; sha256 "..."; end`: append
  `new FormulaResource(name, url, sha256)` to FormulaDefinition.Resources.
- `install_tree` / `install_tree "source", "destination"`: context.InstallTree(source=".", destination="libexec").
- `install_resource "name", "destination"`: context.InstallResource(name, destination).
- `windows_exe "libexec/bin/tool.exe", "tool"`: context.WindowsExe(source, name, params string[] arguments).
  Extra positional arguments are fixed arguments before user arguments.
- `node_script "libexec/bin/tool.js", "tool"`: context.NodeScript(script, name); requires depends_on "node".
- `msys2_install`: context.Msys2Install(); copies main and resource archives into an isolated libexec MSYS2 root.
- `msys2_bin "usr/bin/tool.exe", "tool"`: context.Msys2Bin(source, name).
  A source without .exe is run using that isolated root's bash.exe.

Resources are checksum-verified and staged by the installer; each resource gets
its own directory. Archive staging supports zip, nupkg, msix, tar.gz, tar.xz,
tar.bz2 and pkg.tar.zst via Windows bsdtar. Single enclosing directories are
stripped for upstream release archives, but NOT for MSYS2 package archives.
InstallTree recursively copies from buildpath to the keg, preserving layout.
WindowsExe records an exported command; the prefix launcher invokes the real
executable in its original directory. It does not copy that executable to bin.
NodeScript uses the installed node dependency. No installer or GUI is launched.
Resources and Dependencies default to empty lists. Existing jq loaders need only
add these bindings to support the additional formulae; Codex owns the C# types.
- `windows_cmd "libexec/bin/tool.cmd", "tool"`: context.WindowsCmd(source, name).
  Creates a batch wrapper calling the original script, preserving its %~dp0.
  Optional environment variable name and prefix-relative path arguments (e.g.
  "JAVA_HOME", "libexec/jdk") are scoped to that wrapper's child process.
MSI archives are also supported using a read-only Windows Installer database and
embedded cabinet extraction. No MSI actions, registry writes or installers run.

### Bindings added by the 22-package catalog

- `install_library` / `install_library "libexec/ucrt64"`:
  context.InstallLibrary(source="libexec/ucrt64"); installs native headers,
  libraries and DLLs at the keg root for readline and libyaml.
- `msys2_install "opt/rbenv"`: pass optional subdirectory to
  context.Msys2Install(subdirectory=""). Needed to keep rbenv's own bin separate
  from the MSYS2 /bin -> /usr/bin alias.
- `windows_cmd` binding above is required by node, maven and heroku.
- `windows_exe "libexec/**/aws.exe", "aws"` uses a unique recursive match in the
  native context; the Ruby loader passes the string through unchanged.

Integration status: real jq EndToEnd.ps1 passes with Claude's loader. The package
catalog exercises the extension bindings above. Codex does not edit Ruby files.
