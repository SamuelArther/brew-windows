# Builds, tests and bottles formulae from source, one at a time (brew test-bot, Windows edition).
# Every formula ends uninstalled with its downloads removed; build dependencies stay for the next one.
# -TestOnly: regression run for formulae already in the tap (normal install, brew test, no bottle).
param(
    [Parameter(Mandatory)] [string[]] $Names,
    [Parameter(Mandatory)] [string] $Out,
    [string] $Brew = (Join-Path $PSScriptRoot '..\..\src\Brew\bin\Release\net10.0\win-x64\brew.exe'),
    [int] $TimeoutMinutes = 45,
    [int] $TestTimeoutMinutes = 10,
    [int] $BudgetMinutes = 240,   # no new formula after this, so the job ends (and uploads) before GitHub's 6-hour limit
    [switch] $TestOnly
)
$scriptStarted = Get-Date
$ErrorActionPreference = 'Stop'
$Brew = (Resolve-Path $Brew).Path
New-Item -ItemType Directory -Force $Out | Out-Null
$Out = (Resolve-Path $Out).Path
$logs = New-Item -ItemType Directory -Force (Join-Path $Out 'logs')

# Each brew run gets a job object that kills everything it started (test servers, daemons) when it ends.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class KillJob {
    [StructLayout(LayoutKind.Sequential)] struct Basic { public long a, b; public uint LimitFlags; public UIntPtr c, d; public uint e; public UIntPtr f; public uint g, h; }
    [StructLayout(LayoutKind.Sequential)] struct Io { public ulong a, b, c, d, e, f; }
    [StructLayout(LayoutKind.Sequential)] struct Extended { public Basic Basic; public Io Io; public UIntPtr x, y, z, w; }
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr CreateJobObject(IntPtr a, string name);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetInformationJobObject(IntPtr job, int cls, ref Extended info, int len);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    [DllImport("kernel32.dll", SetLastError = true)] public static extern bool CloseHandle(IntPtr h);
    public static IntPtr Create(IntPtr process) {
        var job = CreateJobObject(IntPtr.Zero, null);
        var info = new Extended(); info.Basic.LimitFlags = 0x2000; // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        if (!SetInformationJobObject(job, 9, ref info, Marshal.SizeOf(info))) throw new System.ComponentModel.Win32Exception();
        if (!AssignProcessToJobObject(job, process)) throw new System.ComponentModel.Win32Exception();
        return job;
    }
}
'@

function Invoke-Brew([string[]] $Arguments, [string] $Log, [string] $Directory = $Out, [int] $Minutes = $TimeoutMinutes) {
    $quoted = $Arguments | ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }
    $stdout = "$Log.out"; $stderr = "$Log.err"
    $p = Start-Process -FilePath $Brew -ArgumentList $quoted -WorkingDirectory $Directory -NoNewWindow -PassThru `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $null = $p.Handle
    $job = [KillJob]::Create($p.Handle)
    $finished = $p.WaitForExit($Minutes * 60 * 1000)
    $code = if ($finished) { $p.ExitCode } else { 124 }
    [KillJob]::CloseHandle($job) | Out-Null   # kills anything the run left behind
    Start-Sleep -Milliseconds 500
    Add-Content $Log "`n==> brew $($Arguments -join ' ')  (exit $code)"
    foreach ($f in $stdout, $stderr) {
        if (Test-Path $f) { Get-Content $f -ErrorAction SilentlyContinue | Add-Content $Log; Remove-Item $f -ErrorAction SilentlyContinue }
    }
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

$PosixOnly = @(
    'fatal error: (termios|pwd|grp|poll|netdb|sysexits|syslog|dlfcn|libgen|langinfo|glob|fnmatch|err|byteswap|features|curses|ncurses)\.h',
    'fatal error: (sys/(socket|wait|ioctl|mman|resource|select|signal|un|utsname|times|statvfs|endian|termios)|netinet/\w+|arpa/inet)\.h',
    "undefined reference to ``(getuid|getgid|geteuid|random|srandom|fork|pipe|kill|getpwnam|getpwuid|alarm|readlink|symlink|lstat|socket)'",
    "'siginfo_t'|'F_UNLCK'|implicit declaration of function '(srandom|random|fork|getuid|lstat|kill|alarm)'",
    "too many arguments to function 'mkdir'|socket function not found|cannot find library function socket|-rdynamic"
) -join '|'

$results = foreach ($name in $Names) {
    $log = Join-Path $logs "$name.log"
    $started = Get-Date
    if (($started - $scriptStarted).TotalMinutes -gt $BudgetMinutes) {
        Write-Host "$name`ttime`t0s"
        $result = [pscustomobject]@{ name = $name; status = 'time'; seconds = 0 }
        @($result) | ConvertTo-Json -AsArray | Set-Content (Join-Path $Out "report-$name.json")
        $result
        continue
    }
    $status = 'ok'
    $install = if ($TestOnly) { @('install', $name) } else { @('install', '--build-bottle', $name) }
    $built = (Invoke-Brew $install $log) -eq 0
    # Unix programs MinGW can't compile (termios, fork, sockets...): retry against MSYS2's POSIX runtime.
    if (-not $built -and -not $TestOnly -and $env:HOMEBREW_MSYS2_ROOT -and (Select-String -Path $log -Quiet -Pattern $PosixOnly)) {
        Add-Content $log "`n==> retrying with the POSIX runtime (msys-2.0.dll)"
        Invoke-Brew @('uninstall', '--force', '--ignore-dependencies', $name) $log | Out-Null
        $env:HOMEBREW_POSIX_RUNTIME = '1'
        try { $built = (Invoke-Brew $install $log) -eq 0 } finally { Remove-Item Env:HOMEBREW_POSIX_RUNTIME }
    }
    if (-not $built) { $status = 'build' }
    elseif (-not (Install-TestDependencies $name $log)) { $status = 'test-deps' }
    elseif ((Invoke-Brew @('test', $name) $log -Minutes $TestTimeoutMinutes) -ne 0) { $status = 'test' }   # a hung test (TUI, server) shouldn't hold a machine for 45 minutes
    elseif (-not $TestOnly -and (Invoke-Brew @('bottle', '--json', $name) $log) -ne 0) { $status = 'bottle' }
    Invoke-Brew @('uninstall', '--force', '--ignore-dependencies', $name) $log | Out-Null
    Invoke-Brew @('cleanup', '--prune=all') $log | Out-Null
    $seconds = [int]((Get-Date) - $started).TotalSeconds
    Write-Host "$name`t$status`t${seconds}s"
    # One report per formula, written as it finishes, so a chunk that times out still reports.
    $result = [pscustomobject]@{ name = $name; status = $status; seconds = $seconds }
    @($result) | ConvertTo-Json -AsArray | Set-Content (Join-Path $Out "report-$name.json")
    $result
}
