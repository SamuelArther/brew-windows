# Formula developer commands: status

These commands live in `src/Brew/Ruby/DevCmd` and run through `Brew.Ruby.DevCommands.Run`.
Their `--help` text and usage errors come from the same renderer as Homebrew's
(`CommandParser`, a port of `Homebrew::CLI::Parser`). The git/GitHub/release developer
commands (pr-pull, tap-new, bottle, ...) are separate and owned by the CLI side.

| Command | Status | Known gaps |
| --- | --- | --- |
| `cat`, `formula`, `edit`, `options`, `readall` | complete | `edit` falls back to VS Code (`code`), then Notepad, when no editor is set |
| `create` | complete | `--python` doesn't fill in PyPI resources; template installs with `install_tree` + `windows_exe` |
| `audit` | most offline audits | online audits (`--online`) are accepted but skipped; cask audits don't apply |
| `style` | ~30 formula cops + common Layout/Style cops | not real RuboCop; `--fix` only covers the ported cops |
| `test` | complete | assertion messages follow Minitest from memory (not checked against its source) |
| `livecheck` (`lc`) | Git, GithubLatest, GithubReleases, PageMatch, HeaderMatch, Json, Apache, Gnu, Sourceforge | `-r/--resources` is a no-op; no Npm/Pypi/Sparkle/ExtractPlist strategies; no throttling |
| `bump` | formula table output | Repology lookups untested (unreachable here); `--open-pr`, release cooldown and `--auto` not done |
| `bump-formula-pr` | edits url/sha256/version/mirror/revision; `--dry-run`, `--write-only` | fork/push/PR steps via `git` + `gh` untested; doesn't update resources or patches |
| `bump-revision` | complete | |
| `extract` | complete | local patch files aren't copied |
| `unpack` | complete | `--patch` has nothing to apply (`patch` is ignored by the loader) |
| `linkage` | reads PE import tables; system / Homebrew / missing DLLs; `--test` | `--cached` is a no-op (as in Homebrew's current code) |
| `sh` | cmd.exe (or PowerShell via `$SHELL`) with brew's environment | no superenv compiler shims |
| `irb`, `ruby` | brew's mini-Ruby with `"jq".f` formula lookup | not full Ruby: no gems, no Homebrew library classes, no pattern matching (`case`/`in`) |

## Formula DSL limits

- `patch`, `bottle`, `service`, `pour_bottle?` and `fails_with` are accepted and ignored.
- `on_macos`/`on_linux` blocks are skipped; `on_windows` and the matching `on_intel`/`on_arm` blocks run.
- There is no superenv build environment: formulae install prebuilt Windows binaries
  instead of compiling from source.
