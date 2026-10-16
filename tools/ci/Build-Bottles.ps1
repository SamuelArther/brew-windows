# Builds, tests and bottles formulae from source, one at a time (brew test-bot, Windows edition).
# Every formula ends uninstalled with its downloads removed; build dependencies stay for the next one.
# -TestOnly: regression run for formulae already in the tap (normal install, brew test, no bottle).
param(
    [Parameter(Mandatory)] [string[]] $Names,
    [Parameter(Mandatory)] [string] $Out,
    [string] $Brew = (Join-Path $PSScriptRoot '..\..\src\Brew\bin\Release\net10.0\win-x64\brew.exe'),
    [int] $TimeoutMinutes = 45,
    [switch] $TestOnly
)
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

function Invoke-Brew([string[]] $Arguments, [string] $Log, [string] $Directory = $Out) {
    $quoted = $Arguments | ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }
    $stdout = "$Log.out"; $stderr = "$Log.err"
    $p = Start-Process -FilePath $Brew -ArgumentList $quoted -WorkingDirectory $Directory -NoNewWindow -PassThru `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $null = $p.Handle
    $job = [KillJob]::Create($p.Handle)
    $finished = $p.WaitForExit($TimeoutMinutes * 60 * 1000)
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

$results = foreach ($name in $Names) {
    $log = Join-Path $logs "$name.log"
    $started = Get-Date
    $status = 'ok'
    $install = if ($TestOnly) { @('install', $name) } else { @('install', '--build-bottle', $name) }
    if ((Invoke-Brew $install $log) -ne 0) { $status = 'build' }
    elseif (-not (Install-TestDependencies $name $log)) { $status = 'test-deps' }
    elseif ((Invoke-Brew @('test', $name) $log) -ne 0) { $status = 'test' }
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
