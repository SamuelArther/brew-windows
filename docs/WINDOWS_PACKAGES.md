# Windows package catalog

All downloads are fixed-version HTTPS URLs with SHA-256 checksums. Recipes are
generated from tools/windows-packages.json using tools/render_windows_formulae.py.
MSYS2 dependency archives are pinned as resources, not installed by pacman.

| Package | Version | Windows packaging |
| --- | --- | --- |
| [autoconf](https://gitweb.gentoo.org/proj/autotools-wrappers.git/) | 2.73 | MSYS2 runtime and dependencies bundled per keg. |
| [automake](https://gitweb.gentoo.org/proj/autotools-wrappers.git/) | 1.18.1 | MSYS2 runtime and dependencies bundled per keg. |
| [awscli](https://aws.amazon.com/cli/) | 2.38.1 | Official AWS CLI v2 MSI read as data only. No installer actions or machine-wide installation. |
| [chromedriver](https://developer.chrome.com/docs/chromedriver) | 155.0.8059.79 | Chrome browser is not installed automatically; driver/browser major versions must match. |
| [cmake](https://cmake.org/) | 4.4.4 | Pinned Windows x64 upstream binary archive. |
| [coreutils](https://www.gnu.org/software/coreutils) | 8.32 | GNU coreutils via MSYS2; Windows filesystem and user semantics differ from Unix. |
| [ffmpeg](https://ffmpeg.org/) | 9.0.2 | Pinned Windows x64 upstream binary archive. |
| [firefox](https://www.mozilla.org/firefox/) | 158.0 | Mozilla MSIX payload extracted as a portable application. No Store registration, shortcuts, default-browser changes or GUI launch. |
| [git](https://git-scm.com/) | 2.56.0 | MinGit, the official minimal Git for Windows distribution; GUI tools are omitted. |
| [heroku](https://devcenter.heroku.com/articles/heroku-cli) | 11.12.0 | Official standalone CLI with bundled Node.js. Login/deployment are never run during installation. |
| [libtool](https://www.gnu.org/software/libtool) | 2.6.2 | MSYS2 runtime and dependencies bundled per keg. |
| [libyaml](https://yaml.org/) | 0.2.5 | UCRT64 development library; headers/import libraries are installed in the keg include/lib directories. |
| [maven](https://maven.apache.org/) | 3.10.0 | Includes a pinned Eclipse Temurin JDK 21 resource; JAVA_HOME is scoped to Maven wrappers. |
| [node](https://nodejs.org/) | 26.12.0 | Pinned Windows x64 upstream binary archive. |
| [pkg-config](https://github.com/pkgconf/pkgconf) | 3.0.8 | pkgconf implementation, following Homebrew pkg-config compatibility naming. |
| [python3](https://www.python.org/) | 3.15.0 | Official full CPython NuGet tools distribution, not the restricted embeddable ZIP. |
| [rbenv](https://github.com/rbenv/rbenv) | 1.3.2 | Experimental MSYS2 shell integration. Does not promise native Windows Ruby builds, include ruby-build, or modify shell profiles. |
| [readline](https://tiswww.case.edu/php/chet/readline/rltop.html) | 8.3.003 | UCRT64 development library, not a standalone command. Headers and import libraries are installed in the keg include/lib directories. |
| [redis](https://github.com/redis-windows/redis-windows) | 8.10.2 | Community MSYS2-based Redis Windows port; not an official Redis Windows build. No service is registered or started. |
| [wget](https://www.gnu.org/software/wget/) | 1.25.0 | MSYS2 runtime and dependencies bundled per keg. |
| [yarn](https://classic.yarnpkg.com/) | 1.22.22 | Yarn Classic, matching the standalone Homebrew yarn package. |
| [youtube-dl](https://ytdl-org.github.io/youtube-dl/) | 2021.12.17 | Actual youtube-dl, not yt-dlp. Upstream latest stable is old; modern site compatibility is not guaranteed. |

Each MSYS2-backed formula has its own runtime root under Cellar/name/version/libexec.
No shared MSYS2 installation, pacman database, registry registration, desktop
shortcut, service, or shell-profile change is made. Installed commands are linked
under BREW_PREFIX/bin. Executables that need their original paths or DLLs get
native launchers; batch files get wrappers that preserve their original directory.

Libraries expose headers and import libraries at the keg include/lib directories.
Use `brew --prefix readline` or `brew --prefix libyaml` to locate them. These are
UCRT64 libraries; they are not MSVC import libraries.

The rbenv recipe is experimental under MSYS2 and does not include ruby-build.
It does not establish that native Windows Ruby builds are supported. Redis uses
the named community Windows port. youtube-dl is the actual old upstream stable
release and may not support modern sites. Firefox is extracted from Mozilla MSIX
as an application payload; desktop integration is intentionally absent.

Validation commands (no Firefox GUI is launched):

```powershell
dotnet run --project tests/Packages
dotnet run --project tests/Packages -- node yarn python3 git ffmpeg cmake
dotnet run --project tests/Packages -- coreutils wget autoconf automake libtool readline libyaml rbenv
dotnet run --project tests/Packages -- chromedriver awscli maven heroku firefox
```

The first command loads all 22 recipes through the real Ruby interpreter and
checks their versions, hashes and resource metadata against the pinned catalog.
Named package tests install into a fresh test prefix, execute a version command
(or inspect headers/Firefox payload), and uninstall. Downloads remain in an
ignored shared test cache. Passing a version test does not verify every feature.
