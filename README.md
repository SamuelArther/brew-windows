# brew-windows

A from-scratch C#/.NET 10 Windows port following Homebrew 7.0.9, with **210
formulae** and a built-in Ruby interpreter. No analytics are sent.
Homebrew attribution: [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt).

## Build and publish

Building requires the .NET 10 SDK and LLVM-MinGW `clang` on PATH, without MSVC.
Published builds are self-contained and need no separate .NET installation.

```powershell
dotnet build
dotnet publish src/Brew -c Release -o .artifacts/brew
```

Keep the complete published directory together: `brew.exe`, bundled `tap`
(including aliases and audit-exception JSON), and license notices.

## Setup

Use Windows x64 with Developer Mode enabled for real symlinks. Windows 11's
`tar.exe` with zstd and 7z support is required for the corresponding archives.
The default prefix is `%LOCALAPPDATA%\Homebrew`; keep it short for GCC header paths.

Initialize PowerShell from the repository:

```powershell
$brew = (Resolve-Path .artifacts/brew/brew.exe).Path
$env:PATH = "$(Split-Path $brew);$env:PATH"
& $brew shellenv pwsh | Out-String | Invoke-Expression
brew install jq
jq --version
brew list
brew uninstall jq
```

For persistent setup, put the first three lines in `$PROFILE`, replacing the
first line with `$brew = 'C:\Tools\brew-windows\brew.exe'` or your actual path.
Set `BREW_PREFIX`, `BREW_CACHE`, or `BREW_REPOSITORY` before `shellenv` to override
the prefix, cache, or directory containing `tap`. Kegs live in
`Cellar/<name>/<version>`; `brew --prefix <name>` resolves through `opt` symlinks.

## Commands

- Packages: `install`, `uninstall`, `reinstall`, `upgrade`, `outdated`, `fetch`.
- Queries: `list`, `info`, `search`, `desc`, `deps`, `uses`, `leaves`, `missing`, `options`.
- Maintenance: `link`, `unlink`, `pin`, `unpin`, `cleanup`, `autoremove`.
- System/taps: `update`, `doctor`, `config`, `shellenv`, `commands`, `help`, `home`,
  `tap`, `untap`, `tap-info`, `log`, `--prefix`, `--cellar`, `--cache`,
  `--repository`, `--env`, `--version`.
- Formula development: `create`, `edit`, `cat`, `formula`, `audit`, `style`,
  `readall`, `test`, `livecheck` (`lc`), `bump`, `bump-formula-pr`, `bump-revision`,
  `extract`, `unpack`, `linkage`, `sh`, `irb`, `ruby`.
- Git/GitHub/release handlers include `tap-new`, `pr-pull`, `pr-publish`,
  `pr-upload`, `bottle`, `dispatch-build-bottle`, `release`, `contributions`,
  `lgtm`, API generation, update helpers, `test-bot`, `determine-test-runners`.

Run `brew commands --quiet` for the full registered list and
`brew <command> --help` for arguments. This release candidate has remaining gaps:
casks and source builds are unsupported; Ruby is a subset; GitHub/release
workflows are not fully verified, and some developer operations are explicitly
unsupported. See [developer command status](docs/DEV_COMMANDS.md).

## Packages

Edit recipes directly using [Writing and checking formulae](docs/PACKAGING.md).
**Do not regenerate recipes:** the renderer is disabled, and
`tools/windows-packages.json` is only a legacy package-test fixture.
Regeneration would discard hand-written tests, livecheck rules and fixes.

Downloads use HTTPS and pinned SHA-256 checksums, including caches and mirrors.
Most use official releases; third-party builders include GyanD (FFmpeg),
FireDaemon (OpenSSL), LuaBinaries (Lua), and MSYS2 (GNU tools). Checksums identify
files; builder provenance still matters. The [original catalog](docs/WINDOWS_PACKAGES.md)
records the initial batch; each formula contains its current URLs and resources.

Commands are available in each keg's `bin`. Global commands use symlinks or
native shims for DLL lookup and fixed arguments. Keg-only formulae require
explicit linking. MSYS2 tools bundle pinned runtime resources. MSI extraction
runs no installer actions.

## Verification

```powershell
dotnet run --project tests/Commands
dotnet run --project tests/Smoke
pwsh -File tests/EndToEnd.ps1 -BrewExe .artifacts/brew/brew.exe
& .artifacts/brew/brew.exe readall
& .artifacts/brew/brew.exe audit
& .artifacts/brew/brew.exe style
```

Command tests use isolated prefixes and cached synthetic archives. Smoke tests
run real jq and check rollback and native shims; move or remove their
`.test-work/smoke` directory before rerunning. End-to-end tests use a new prefix.
Verify the exact published executable and its bundled tap before distribution.

Interpreter contract: [FORMULA_API.md](docs/FORMULA_API.md).
Parallel cache use, large-tap benchmarks, installer extraction and optional MSYS2
DLL deduplication: [Scaling notes](docs/SCALING.md).
