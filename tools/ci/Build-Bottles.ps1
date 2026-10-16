# Builds, tests and bottles formulae from source, one at a time (brew test-bot, Windows edition).
# Every formula ends uninstalled with its downloads removed; build dependencies stay for the next one.
param(
    [Parameter(Mandatory)] [string[]] $Names,
    [Parameter(Mandatory)] [string] $Out,
    [string] $Brew = (Join-Path $PSScriptRoot '..\..\src\Brew\bin\Release\net10.0\win-x64\brew.exe'),
    [int] $TimeoutMinutes = 45
)
$ErrorActionPreference = 'Stop'
$Brew = (Resolve-Path $Brew).Path
New-Item -ItemType Directory -Force $Out | Out-Null
$Out = (Resolve-Path $Out).Path
$logs = New-Item -ItemType Directory -Force (Join-Path $Out 'logs')

function Invoke-Brew([string[]] $Arguments, [string] $Log, [string] $Directory = $Out) {
    $quoted = $Arguments | ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }
    $stdout = "$Log.out"; $stderr = "$Log.err"
    $p = Start-Process -FilePath $Brew -ArgumentList $quoted -WorkingDirectory $Directory -NoNewWindow -PassThru `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $null = $p.Handle
    $finished = $p.WaitForExit($TimeoutMinutes * 60 * 1000)
    if (-not $finished) { & taskkill /T /F /PID $p.Id | Out-Null; $code = 124 } else { $code = $p.ExitCode }
    Add-Content $Log "`n==> brew $($Arguments -join ' ')  (exit $code)"
    foreach ($f in $stdout, $stderr) { if (Test-Path $f) { Get-Content $f | Add-Content $Log; Remove-Item $f } }
    return $code
}

# brew test-bot installs `depends_on ... => :test` formulae before testing.
function Install-TestDependencies([string] $Name, [string] $Log) {
    $all = & $Brew deps --direct --include-test $Name 2>> $Log
    if ($LASTEXITCODE -ne 0) { return $false }
    $runtime = & $Brew deps --direct $Name 2>> $Log
    foreach ($dependency in $all | Where-Object { $runtime -notcontains $_ }) {
        if ((Invoke-Brew @('install', $dependency) $Log) -ne 0) { return $false }
    }
    return $true
}

$results = foreach ($name in $Names) {
    $log = Join-Path $logs "$name.log"
    $started = Get-Date
    $status = 'ok'
    if ((Invoke-Brew @('install', '--build-bottle', $name) $log) -ne 0) { $status = 'build' }
    elseif (-not (Install-TestDependencies $name $log)) { $status = 'test-deps' }
    elseif ((Invoke-Brew @('test', $name) $log) -ne 0) { $status = 'test' }
    elseif ((Invoke-Brew @('bottle', '--json', $name) $log) -ne 0) { $status = 'bottle' }
    Invoke-Brew @('uninstall', '--force', '--ignore-dependencies', $name) $log | Out-Null
    Invoke-Brew @('cleanup', '--prune=all') $log | Out-Null
    $seconds = [int]((Get-Date) - $started).TotalSeconds
    Write-Host "$name`t$status`t${seconds}s"
    [pscustomobject]@{ name = $name; status = $status; seconds = $seconds }
}
$results | ConvertTo-Json -AsArray | Set-Content (Join-Path $Out "report-$([guid]::NewGuid().ToString('N')).json")
