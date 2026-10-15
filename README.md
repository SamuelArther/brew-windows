# brew-windows

A from-scratch C#/.NET 10 Windows port following Homebrew 7.0.9.
Homebrew attribution and its BSD-2-Clause license are in THIRD_PARTY_NOTICES.txt.
No analytics are sent.

Build prerequisites: .NET 10 SDK and LLVM-MinGW `clang` on PATH. MSVC is not required.

```powershell
dotnet build
dotnet publish src/Brew -c Release -o .artifacts/brew
$env:BREW_PREFIX = "$PWD/.test-work/homebrew"
.artifacts/brew/brew.exe install jq
.artifacts/brew/brew.exe list
& "$env:BREW_PREFIX/bin/jq.exe" --version
.artifacts/brew/brew.exe uninstall jq
```

The publish directory includes the self-contained single-file executable, the
core tap, and license notices. Add `%BREW_PREFIX%\bin` to PATH to invoke installed
programs by name. The default prefix is `%LOCALAPPDATA%\Homebrew`.
`BREW_REPOSITORY` overrides tap discovery; `BREW_CACHE` overrides the download cache.
Downloads are HTTPS and always SHA-256 checked, including cached downloads.

The initial CLI supports install, list, uninstall, link, and unlink. jq uses the
official standalone Windows x64 release. DLL-bearing bin executables use the
small native launcher built from shim/shim.c; other files use real symlinks.

Interpreter integration is specified in [docs/FORMULA_API.md](docs/FORMULA_API.md).
The independent package smoke test uses an explicit interpreter test double:

```powershell
dotnet run --project tests/Smoke
```

It downloads and executes real jq, verifies installation and removal, detects
corrupt downloads and link collisions, and checks native shim argument forwarding
and exit status. It leaves artifacts under `.test-work/smoke` for inspection;
remove that directory before rerunning.

Run the full jq integration test against the real Ruby interpreter (PowerShell 7):

```powershell
dotnet build
pwsh -File tests/EndToEnd.ps1
# To verify a published release instead:
pwsh -File tests/EndToEnd.ps1 -BrewExe .artifacts/brew/brew.exe
```

Each integration run uses a new isolated prefix and a shared test download cache.

The [Windows package catalog](docs/WINDOWS_PACKAGES.md) documents the additional
22 packages, their upstream sources, MSYS2 packaging and compatibility limits.
All recipes have pinned SHA-256 checksums. Packages preserve their complete
directory layout, with command launchers in `BREW_PREFIX/bin`. MSYS2 tools bundle
their own pinned runtime dependencies under their keg. Python uses the full
official NuGet distribution; Maven includes a JDK. Firefox is extracted without
running its installer or opening a GUI. AWS CLI MSI extraction executes no MSI actions.

```powershell
dotnet run --project tests/Packages                 # Verify all real Ruby metadata
dotnet run --project tests/Packages -- node yarn    # Install, execute, remove
python tools/render_windows_formulae.py             # Regenerate recipes from pinned catalog
```

Windows 11's `tar.exe` with zstd support is required for MSYS2 package archives.
`brew --prefix readline` and `brew --prefix libyaml` locate development libraries.
