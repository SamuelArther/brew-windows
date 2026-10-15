# Scaling and installer extraction

## Shared download cache

Download locks live under `BREW_CACHE/.locks`. Writers serialize by canonical
cache filename, recheck SHA-256 after taking the lock, write `<file>.incomplete`,
then rename only verified content. A crashed writer's partial file is discarded
by the next lock holder. Lock files persist to avoid file-object replacement races.
Cleanup skips active download locks. Tests launch four separate processes and
kill a writer to check recovery.

## Timing a large tap

Run `pwsh -File tests/Benchmark.ps1 -BrewExe <absolute-path-to-brew.exe>`.
The script creates an isolated synthetic 1,500-formula tap and records three
samples per command, including process startup. Installs use a verified cached
jq download and a fresh prefix each time. Update has no remote, so network time
and merging are not measured. Synthetic formulae do not model every Ruby DSL cost.

Published Release-build median milliseconds on this machine with 2,000 formulae
(`-FormulaCount 2000`):

| search | info | cached install jq | outdated | deps | update |
| --- | --- | --- | --- | --- | --- |
| 330 | 371 | 417 | 359 | 315 | 328 |

## Installer-only distributions

The archive layer recognizes Inno Setup and NSIS signatures in EXE files.
It runs extraction tools, never the downloaded setup EXE. Unknown EXEs remain
standalone payloads, preserving tools such as jq.

Tools are fetched on demand from upstream HTTPS URLs, SHA-256 verified, and
staged under `BREW_CACHE/.tools`, with their license files retained:

- innoextract 1.9: `https://constexpr.org/innoextract/files/innoextract-1.9-windows.zip`;
  SHA-256 `6989342c9b026a00a72a38f23b62a8e6a22cc5de69805cf47d68ac2fec993065`.
  This upstream version supports Inno Setup through 6.2.2; newer installers may
  require a newer extractor selected with `BREW_INNOEXTRACT=<absolute-exe-path>`.
- 7-Zip 26.04: `https://www.7-zip.org/a/7z2604-x64.msi`;
  SHA-256 `0b01334a418654293513449f61d6bfc99e5196ca371e3c3dc961eda57cd535c6`.
  The MSI is read as data to obtain 7z.exe/7z.dll, without installation actions.
  `BREW_7ZIP=<absolute-exe-path>` selects an existing extractor instead.

Real integration checks extract Notepad++ 8.8.2 (NSIS) and Inno Setup 6.2.2,
checking their application payloads without executing either installer.

## Optional MSYS2 DLL deduplication

Set `BREW_DEDUPLICATE_MSYS2=1` before installing to hardlink byte-identical DLLs
from already-installed MSYS2 kegs in the same prefix. Filename, size and SHA-256
must match. Unsupported filesystems fall back to separate files. No shared
store is retained after uninstalling the final keg.

This is opt-in because hardlinked files share modifications: changing one DLL
changes every linked copy. Use it only for packages treated as immutable.
Deleting one keg's link leaves the other keg's DLL intact. Different-content
DLLs are never combined.
