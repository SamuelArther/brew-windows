# Writing formulae for brew-windows

Formulae are Ruby files in `tap/homebrew-core/Formula/<shard>/<name>.rb`, written the
same way as homebrew-core's and evaluated by brew's own Ruby interpreter
(`src/Brew/Ruby`). The shard is the first letter of the name, or `lib` for names
starting with `lib` (Homebrew's CoreTap rule).

About 210 formulae are hand-written. The rest were bootstrapped from homebrew-core's
metadata onto prebuilt Windows packages (MSYS2's UCRT64/MSYS repositories, or a project's
own Windows x64 GitHub release asset). A generated formula was kept only if `brew install`,
`brew test` and `brew uninstall` all passed with no files left behind. Test strength
varies, from strongest to weakest: homebrew-core's own `test do` block; the version printed
by `--version` (or a similar flag); the command running with `--version`/`--help` and
exiting 0; or, for programs that can't run headless, only checking that the command is
installed. Libraries get file checks instead.

Metadata must match `brew info --json=v2` from real Homebrew: `desc`, `homepage`,
`license`, `keg_only`, `conflicts_with` (homebrew-core's own list, never inferred),
`version_scheme`, and `revision` when the version is the same. Aliases match homebrew-core's
`Aliases` entries, and `formula_renames.json` holds its renames, for formulae present here.
Versions, URLs and dependencies follow the Windows build. Command names follow Homebrew's
executables, never the release asset's file name. Formulae whose tests change system state
(services, drivers, DNS) or that Microsoft Defender flags are left out.

## Rules

- **Same name as homebrew-core.** Check `https://formulae.brew.sh/api/formula/<name>.json`
  and copy `desc`, `homepage` and `license` from it. Versioned formulae follow homebrew-core
  too: `node@22` is class `NodeAT22` with `keg_only :versioned_formula`.
- **Official upstream Windows x64 builds**, from the project's releases or download site.
  If a project has no official Windows build, use a well-known source and explain it in
  a comment at the top (examples: openssl@3 via FireDaemon, GNU tools via MSYS2).
- **Pinned and verified.** Fixed-version HTTPS URL plus `sha256` of the file you downloaded.
- **Nothing system-wide.** No installers run, no GUI is launched, and PATH, the registry and
  services are untouched. MSI/MSIX packages are read as archives only.

## Layout (homebrew-core component order)

```ruby
class Ripgrep < Formula
  desc "Search tool like grep and The Silver Searcher"
  homepage "https://github.com/BurntSushi/ripgrep"
  url "https://github.com/BurntSushi/ripgrep/releases/download/15.2.0/ripgrep-15.2.0-x86_64-pc-windows-msvc.zip"
  sha256 "..."
  license "Unlicense"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/rg.exe", "rg"
  end

  test do
    (testpath/"Hello.txt").write("Hello World!")
    system bin/"rg", "Hello World!", testpath
  end
end
```

Order: `desc`, `homepage`, `url`, `mirror`, `version`, `sha256`, `license`, `revision`,
`livecheck`, `keg_only`, `depends_on`, `uses_from_macos`, `conflicts_with`, `resource`,
`def install`, `def caveats`, `test`. Only write `version` when it can't be read from the
URL; `brew audit` reports a redundant one. Apache projects use
`url "https://www.apache.org/dyn/closer.lua?path=..."` with an `archive.apache.org` `mirror`.

## Install helpers

| Helper | Use for |
| --- | --- |
| `bin.install "tool.exe"` / `bin.install "x.exe" => "tool.exe"` | a standalone .exe with no DLLs |
| `install_tree` then `windows_exe "libexec/path/tool.exe", "tool", *fixed_args` | anything with DLLs or data files; runs in place |
| `windows_cmd "libexec/bin/tool.cmd", "tool"[, "ENV_VAR", "libexec/path"]` | .cmd/.bat entry points (optionally with an env var such as JAVA_HOME) |
| `node_script "libexec/bin/x.js", "x"` | Node scripts (needs `depends_on "node"`) |
| `msys2_install[ "subdir"]` then `msys2_bin "usr/bin/tool.exe", "tool"` | GNU tools from MSYS2; pin every runtime package as a `resource` |
| `install_library["libexec/ucrt64"]` | UCRT64 development libraries (include/lib/bin/share) |
| `install_resource "name", "dest"` | copy a staged `resource` into the keg |

Archives (zip, tar.gz/xz/bz2/zst, 7z, nupkg, msix, msi) are extracted automatically; a
single top-level directory is stripped (except in MSYS2 packages). The usual Ruby/Homebrew
tools work inside `install`: `prefix`, `bin`, `libexec`, `share`, `etc`, `buildpath`, `system`,
`inreplace`, `mv`/`cp`/`rm`/`mkdir_p`, `Pathname` methods, `ENV`, `Formula["other"].opt_bin`,
`version.major_minor`.

## Tests

Every formula has a `test do` block modeled on homebrew-core's. They run in a temporary
`testpath` with `HOME`/`USERPROFILE`/`APPDATA`/`LOCALAPPDATA` pointed into it. Use
`shell_output`, `pipe_output`, `system bin/"tool"`, and Minitest assertions (`assert_match`,
`assert_equal`, `assert_path_exists`, ...). `bin/"tool"` resolves to `tool.exe`/`tool.cmd`.
Tests must never open a GUI window or leave a process running.

## Checking a formula

```powershell
brew style <name>            # formatting (RuboCop-style subset)
brew audit --strict <name>   # metadata, order, license, versions
brew install <name>
brew test <name>
& "$(brew --prefix <name>)\bin\<cmd>.exe" --version
brew uninstall <name>
brew livecheck <name>        # is there a newer upstream version?
```

`brew readall` loads every formula; `brew audit` and `brew style` with no arguments check
the whole tap. To update a formula, use `brew bump-formula-pr --write-only --url=<new url> <name>`
(it downloads the file, fills in the new `sha256` and edits the version).
