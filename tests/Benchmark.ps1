param([string]$BrewExe = "$PSScriptRoot/../src/Brew/bin/Debug/net10.0/win-x64/brew.exe", [int]$FormulaCount = 1500)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath("$PSScriptRoot/..")
$brew = [IO.Path]::GetFullPath($BrewExe)
$root = Join-Path $workspace ('.test-work/benchmark-' + [Guid]::NewGuid().ToString('N'))
$repo = Join-Path $root 'repo'; $prefix = Join-Path $root 'prefix'; $cache = Join-Path $root 'cache'
$formula = Join-Path $repo 'tap/homebrew-core/Formula'
New-Item -ItemType Directory -Path (Join-Path $formula 'j'),(Join-Path $formula 'b'),$cache -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $workspace 'tap/homebrew-core/Formula/j/jq.rb') -Destination (Join-Path $formula 'j')
for ($i = 1; $i -lt $FormulaCount; $i++) {
    $name = 'bench{0:d4}' -f $i
    $text = "class $($name.Substring(0,1).ToUpper()+$name.Substring(1)) < Formula`n  desc `"Synthetic benchmark fixture`"`n  homepage `"https://example.invalid`"`n  url `"https://example.invalid/fixture.zip`"`n  version `"1.0`"`n  sha256 `"$('a' * 64)`"`n  license `"MIT`"`n  def install`n    install_tree`n  end`nend`n"
    [IO.File]::WriteAllText((Join-Path $formula "b/$name.rb"),$text)
}
$cachedJq = Join-Path $workspace '.test-work/e2e-cache/a6fc67fedaf9128a3309a1e2ebb8b986aeccf70122ee46d2cb4849e423f0c627--jq-windows-amd64.exe'
if (-not (Test-Path -LiteralPath $cachedJq)) { throw 'Run the jq end-to-end test first to seed its verified cache.' }
Copy-Item -LiteralPath $cachedJq -Destination $cache
function Measure-Brew([string[]]$Arguments, [string]$TargetPrefix = $prefix) {
    $start = [Diagnostics.ProcessStartInfo]::new($brew)
    $start.UseShellExecute = $false; $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
    $start.Environment['BREW_REPOSITORY'] = $repo; $start.Environment['BREW_PREFIX'] = $TargetPrefix; $start.Environment['BREW_CACHE'] = $cache
    foreach ($arg in $Arguments) { $start.ArgumentList.Add($arg) }
    $timer = [Diagnostics.Stopwatch]::StartNew(); $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit(); $timer.Stop()
    if ($process.ExitCode -ne 0) { throw ($stderr.Result + $stdout.Result) }
    $process.Dispose(); return [Math]::Round($timer.Elapsed.TotalMilliseconds,1)
}
Measure-Brew @('install','jq') | Out-Null
$results = foreach ($command in @('search','info','install','outdated','deps','update')) {
    $samples = for ($i = 0; $i -lt 3; $i++) {
        $arguments = if ($command -in @('search','info','install','deps')) { @($command,'jq') } else { @($command) }
        if ($command -eq 'install') { Measure-Brew $arguments (Join-Path $root "install-$i") }
        else { Measure-Brew $arguments }
    }
    [pscustomobject]@{Command=$command;MedianMs=(@($samples | Sort-Object))[1];MaxMs=($samples | Measure-Object -Maximum).Maximum}
}
$results | Format-Table -AutoSize
[pscustomobject]@{FormulaCount=$FormulaCount;Executable=$brew;Repository=$repo;Note='Synthetic tap; cached jq; process startup included; update has no remote';Results=@($results)} |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $root 'results.json')
"Results: $root"
