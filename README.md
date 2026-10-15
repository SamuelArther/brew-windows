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
The interpreter must supply FormulaLoader before `brew install` can evaluate Ruby.
The independent package smoke test uses an explicit interpreter test double:

```powershell
dotnet run --project tests/Smoke
```

It downloads and executes real jq, verifies installation and removal, detects
corrupt downloads and link collisions, and checks native shim argument forwarding
and exit status. It leaves artifacts under `.test-work/smoke` for inspection;
remove that directory before rerunning.
