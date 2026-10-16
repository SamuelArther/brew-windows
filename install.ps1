# brew-windows installer:
#   irm https://raw.githubusercontent.com/SamuelArther/brew-windows/main/install.ps1 | iex
#
# Clones the repository (formulae update with `brew update`), downloads the latest brew.exe release
# into it, and puts brew and installed commands on your user PATH. Nothing needs administrator rights.
#
# Runs in its own scope, so `| iex` leaves the caller's session settings alone. Works in Windows
# PowerShell 5.1 (what a fresh Windows 10/11 has) and PowerShell 7.
& {
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # Windows PowerShell's progress bar slows downloads to a crawl
# Older Windows PowerShell setups don't offer TLS 1.2, which GitHub requires.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
$Repo = if ($env:BREW_WINDOWS_REPO) { $env:BREW_WINDOWS_REPO } else { 'SamuelArther/brew-windows' }
$Home_ = if ($env:BREW_WINDOWS_HOME) { $env:BREW_WINDOWS_HOME } else { Join-Path $env:LOCALAPPDATA 'brew-windows' }
$Prefix = if ($env:BREW_PREFIX) { $env:BREW_PREFIX } else { Join-Path $env:LOCALAPPDATA 'Homebrew' }
$Api = "https://api.github.com/repos/$Repo"

function Say($text) { Write-Host "==> " -ForegroundColor Blue -NoNewline; Write-Host $text -ForegroundColor White }
# $ErrorActionPreference doesn't cover programs like git: check their exit codes.
function Invoke-Git { git @args; if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed (exit code $LASTEXITCODE)." } }

if (-not [Environment]::Is64BitOperatingSystem) { throw 'brew-windows needs 64-bit Windows.' }
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'brew-windows needs Git. Install it (for example: winget install --id Git.Git -e), then run this again.'
}

# Symlinks without administrator rights need Developer Mode (Settings > System > For developers).
$devMode = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -Name AllowDevelopmentWithoutDevLicense -ErrorAction SilentlyContinue
if (-not $devMode -or $devMode.AllowDevelopmentWithoutDevLicense -ne 1) {
    Write-Warning 'Developer Mode is off. Turn it on (Settings > System > For developers) so brew can create symlinks.'
}

if (Test-Path (Join-Path $Home_ '.git')) {
    Say "Updating $Home_"
    Invoke-Git -C $Home_ pull --ff-only --quiet
} elseif ((Test-Path $Home_) -and (Get-ChildItem -Force $Home_ | Select-Object -First 1)) {
    throw "$Home_ already exists and isn't a brew-windows clone. Move it away (or set BREW_WINDOWS_HOME), then run this again."
} else {
    Say "Cloning https://github.com/$Repo into $Home_"
    Invoke-Git clone --quiet --depth 1 "https://github.com/$Repo.git" $Home_
}

# The repository also publishes a release per bottle, so look brew-windows releases up by tag,
# newest version first. (Assign before looping: Windows PowerShell's Invoke-RestMethod passes a
# JSON array down the pipeline as one object.)
Say 'Downloading brew'
$refs = Invoke-RestMethod "$Api/git/matching-refs/tags/brew-windows-"
$tags = foreach ($ref in $refs) { if ($ref.ref -match '^refs/tags/(brew-windows-(\d+(\.\d+){1,3}))$') { [pscustomobject]@{ Tag = $Matches[1]; Version = [version]$Matches[2] } } }
$release = $null
foreach ($tag in @($tags | Sort-Object Version -Descending)) {
    try { $candidate = Invoke-RestMethod "$Api/releases/tags/$($tag.Tag)" } catch { continue }   # a tag without a release
    if (-not $candidate.prerelease -and -not $candidate.draft) { $release = $candidate; break }
}
if (-not $release) { throw "No brew-windows release found in $Repo." }
$asset = @($release.assets | Where-Object { $_.name -eq "$($release.tag_name).zip" }) + @($release.assets | Where-Object { $_.name -like '*.zip' }) | Select-Object -First 1
if (-not $asset) { throw "Release $($release.tag_name) has no .zip download." }
if ($asset.digest -notmatch '^sha256:([0-9a-fA-F]{64})$') { throw "Release $($release.tag_name) has no SHA-256 digest to check the download against." }
$expected = $Matches[1]
$work = Join-Path ([IO.Path]::GetTempPath()) "brew-windows-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force $work | Out-Null
try {
    $zip = Join-Path $work $asset.name
    Invoke-WebRequest $asset.browser_download_url -OutFile $zip -UseBasicParsing
    if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne $expected) { throw "Checksum mismatch for $($asset.name); not installing it." }
    $staging = Join-Path $work 'files'
    Expand-Archive $zip $staging
    if (-not (Test-Path (Join-Path $staging 'brew.exe'))) { throw "$($asset.name) has no brew.exe." }
    $bin = Join-Path $Home_ 'bin'
    New-Item -ItemType Directory -Force $bin | Out-Null
    # A running brew.exe can be renamed but not overwritten; brew deletes brew.exe.old on its next run.
    $exe = Join-Path $bin 'brew.exe'
    if (Test-Path $exe) {
        Remove-Item "$exe.old" -Force -ErrorAction SilentlyContinue
        Move-Item $exe "$exe.old" -Force
    }
    Copy-Item (Join-Path $staging '*') $bin -Recurse -Force
} finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

# User PATH: brew itself, then the commands formulae install. Read and write the raw registry value:
# [Environment]::Get/SetEnvironmentVariable would expand %VARIABLES% in it and store it as plain REG_SZ.
$key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Environment')
try {
    $path = [string]$key.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
    $kind = if ($key.GetValueNames() -contains 'Path') { $key.GetValueKind('Path') } else { [Microsoft.Win32.RegistryValueKind]::ExpandString }
    $entries = @($path -split ';' | Where-Object { $_ })
    $changed = $false
    foreach ($dir in @((Join-Path $Prefix 'bin'), $bin)) {
        $present = $entries | Where-Object { [Environment]::ExpandEnvironmentVariables($_).TrimEnd('\') -eq $dir.TrimEnd('\') }
        if (-not $present) { $entries = @($dir) + $entries; $changed = $true }
    }
    if ($changed) { $key.SetValue('Path', ($entries -join ';'), $kind) }
} finally { $key.Close() }
if ($changed) {
    # Setting (then removing) a user variable this way broadcasts WM_SETTINGCHANGE, so new
    # terminals opened from Explorer see the new PATH without signing out.
    [Environment]::SetEnvironmentVariable('BREW_WINDOWS_INSTALLING', '1', 'User')
    [Environment]::SetEnvironmentVariable('BREW_WINDOWS_INSTALLING', $null, 'User')
}
$env:Path = "$bin;$(Join-Path $Prefix 'bin');$env:Path"

Say "Installed $($release.tag_name -replace '^brew-windows-', 'brew-windows ')"
& $exe --version
Write-Host ''
Write-Host 'Next steps:'
Write-Host '  Open a new terminal, then try:  brew install jq'
Write-Host '  Update formulae any time with:  brew update'
}
