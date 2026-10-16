# brew-windows

**Homebrew, natively on Windows.** `brew install`, `brew upgrade`, `brew list` and the rest of
the commands you know from macOS, running as real Windows programs, with no WSL and no Linux VM.

```powershell
irm https://raw.githubusercontent.com/SamuelArther/brew-windows/main/install.ps1 | iex
brew install jq ripgrep fd
```

brew-windows is an independent project. It is **not affiliated with or endorsed by Homebrew**;
it follows Homebrew's behaviour and uses its formulae under their BSD-2-Clause license
(see [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt)). Thank you, Homebrew maintainers.

## What you get

- **Homebrew's own formulae, built for Windows.** Formulae come from
  [homebrew-core](https://github.com/Homebrew/homebrew-core) unchanged wherever possible. Each one
  is built from source on GitHub Actions, must pass its own `brew test`, and is published as a
  Windows bottle. Only formulae that build and pass make it into the tap.
- **Go, Rust, Python, Node, Java and C/C++ tools.** C and C++ build with MSYS2's UCRT64 GCC.
  Unix-only programs (the ones that need `fork`, terminals or sockets) build against MSYS2's
  POSIX layer, the same approach Git for Windows uses. Every bottle carries the DLLs it needs.
- **Mac and Linux-only tools rewritten for Windows.** `trash`, `lsusb`, `iostat`, `ip`,
  `systemctl`, `wifi-password`, `brightness` and more run as native Windows tools under their
  original names ([wintools](wintools)).
- **Daily updates.** A bot follows homebrew-core every day, rebuilds what changed, and only ships
  what passes. Run `brew update` to get it.
- **Familiar commands.** `install`, `uninstall`, `upgrade`, `outdated`, `list`, `info`, `search`,
  `deps`, `uses`, `leaves`, `link`, `pin`, `cleanup`, `autoremove`, `doctor`, `shellenv`, `tap`,
  plus the formula developer commands (`create`, `edit`, `audit`, `style`, `test`, `livecheck`,
  `bottle` ...). `brew commands` lists them all.

## Install

Requirements: 64-bit Windows 10 or 11, [Git](https://git-scm.com/download/win)
(`winget install --id Git.Git -e`), and **Developer Mode** turned on (Settings → System → For
developers) so brew can create symlinks without administrator rights.

```powershell
irm https://raw.githubusercontent.com/SamuelArther/brew-windows/main/install.ps1 | iex
```

The installer clones this repository to `%LOCALAPPDATA%\brew-windows` (formula updates come
through `git`), downloads the latest `brew.exe` release next to it, and adds brew and
`%LOCALAPPDATA%\Homebrew\bin` to your user `PATH`. Open a new terminal afterwards.

Packages live under `%LOCALAPPDATA%\Homebrew` (`Cellar\<name>\<version>`, linked into `bin`,
`lib`, `share` ...), just like on macOS. `brew update` updates the formulae and brew itself.

## How it works

- **brew.exe** is written from scratch in C# (.NET 10) and ships as one self-contained file.
- **Formulae are Ruby.** brew-windows contains its own Ruby interpreter that runs formula files
  the way Homebrew does: the formula DSL, `def install`, `test do`, `post_install_steps`, and
  Homebrew's helpers (`std_cargo_args`, `virtualenv_install_with_resources`, `std_npm_args`,
  `inreplace`, `shell_output` ...).
- **Builds** run in a "superenv" like Homebrew's: Go/Cargo caches, MSYS2 compilers, CMake/Ninja,
  pkg-config and the formula's dependencies on `PATH`.
- **Bottles** are built by [`.github/workflows/bottles.yml`](.github/workflows/bottles.yml):
  import an upstream formula, build it, run `brew test`, bottle it, and upload it as a GitHub
  release. Bots then commit the formula with its bottle block.
- **Windows-specific fixes** to a formula live in `tools/windows-patches/<name>.patch`, so
  upstream updates keep flowing.

## Building from source

Needs the .NET 10 SDK and [LLVM-MinGW](https://github.com/mstorsjo/llvm-mingw) `clang` on `PATH`
(for the small native launcher brew embeds).

```powershell
dotnet build
dotnet run --project tests/Commands     # command tests in isolated prefixes
dotnet run --project tests/Smoke        # real install/uninstall round trip
dotnet publish src/Brew -c Release -o .artifacts/brew
```

## Contributing

Bug reports and pull requests are welcome. If a formula fails to build or misbehaves on
Windows, open an issue with the output of `brew install --verbose <name>` and `brew config`.
Formula fixes usually belong in `tools/windows-patches/`; see [docs/PACKAGING.md](docs/PACKAGING.md)
and [docs/FORMULA_API.md](docs/FORMULA_API.md).

## License

brew-windows is released under the [BSD 2-Clause License](LICENSE). Formulae and behaviour derived
from Homebrew remain under Homebrew's BSD 2-Clause License ([notices](THIRD_PARTY_NOTICES.txt)).
Packages you install keep their own licenses.
