# brew-windows installer:
#   irm https://raw.githubusercontent.com/SamuelArther/brew-windows/main/install.ps1 | iex
#
# Clones the repository (formulae update with `brew update`), downloads the latest brew.exe release
# into it, and puts brew and installed commands on your user PATH. Nothing needs administrator rights.
$ErrorActionPreference = 'Stop'
$Repo = if ($env:BREW_WINDOWS_REPO) { $env:BREW_WINDOWS_REPO } else { 'SamuelArther/brew-windows' }
$Home_ = if ($env:BREW_WINDOWS_HOME) { $env:BREW_WINDOWS_HOME } else { Join-Path $env:LOCALAPPDATA 'brew-windows' }
$Prefix = if ($env:BREW_PREFIX) { $env:BREW_PREFIX } else { Join-Path $env:LOCALAPPDATA 'Homebrew' }

function Say($text) { Write-Host "==> " -ForegroundColor Blue -NoNewline; Write-Host $text -ForegroundColor White }

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
    git -C $Home_ pull --ff-only --quiet
} else {
    Say "Cloning https://github.com/$Repo into $Home_"
    git clone --quiet --depth 1 "https://github.com/$Repo.git" $Home_
}

Say 'Downloading brew'
$release = Invoke-RestMethod "https://api.github.com/repos/$Repo/releases?per_page=100" |
    Where-Object { $_.tag_name -like 'brew-windows-*' -and -not $_.prerelease } | Select-Object -First 1
if (-not $release) { throw "No brew-windows release found in $Repo." }
$asset = $release.assets | Where-Object name -like '*.zip' | Select-Object -First 1
$zip = Join-Path $env:TEMP $asset.name
Invoke-WebRequest $asset.browser_download_url -OutFile $zip -UseBasicParsing
$bin = Join-Path $Home_ 'bin'
New-Item -ItemType Directory -Force $bin | Out-Null
Expand-Archive $zip $bin -Force
Remove-Item $zip

# User PATH: brew itself, then the commands formulae install.
$path = [Environment]::GetEnvironmentVariable('Path', 'User')
$entries = @($path -split ';' | Where-Object { $_ })
foreach ($dir in @((Join-Path $Prefix 'bin'), $bin)) {
    if ($entries -notcontains $dir) { $entries = @($dir) + $entries }
}
[Environment]::SetEnvironmentVariable('Path', ($entries -join ';'), 'User')
$env:Path = "$bin;$(Join-Path $Prefix 'bin');$env:Path"

Say "Installed $($release.tag_name -replace '^brew-windows-', 'brew-windows ')"
& (Join-Path $bin 'brew.exe') --version
Write-Host ''
Write-Host 'Next steps:'
Write-Host '  Open a new terminal, then try:  brew install jq'
Write-Host '  Update formulae any time with:  brew update'
