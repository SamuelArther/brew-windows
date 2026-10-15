param(
    [string]$BrewExe = "$PSScriptRoot/../src/Brew/bin/Debug/net10.0/win-x64/brew.exe"
)

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath("$PSScriptRoot/..")
$executable = [IO.Path]::GetFullPath($BrewExe)
if (!(Test-Path -LiteralPath $executable)) { throw 'Build brew first with dotnet build.' }
$prefix = Join-Path $repository ".test-work/e2e-$([Guid]::NewGuid().ToString('N'))"
$cache = Join-Path $repository '.test-work/e2e-cache'

function Invoke-TestProcess {
    param([string]$File, [string[]]$Arguments, [string]$InputText = '', [int]$ExpectedCode = 0)
    $start = [Diagnostics.ProcessStartInfo]::new($File)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.RedirectStandardInput = $true
    $start.Environment['BREW_PREFIX'] = $prefix
    $start.Environment['BREW_CACHE'] = $cache
    $start.Environment['BREW_REPOSITORY'] = $repository
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if ($InputText) { $process.StandardInput.WriteLine($InputText) }
        $process.StandardInput.Close()
        $process.WaitForExit()
        $output = $stdout.GetAwaiter().GetResult().Trim()
        $errors = $stderr.GetAwaiter().GetResult().Trim()
        if ($process.ExitCode -ne $ExpectedCode) {
            throw "$File $Arguments exited $($process.ExitCode), expected ${ExpectedCode}.`n$output`n$errors"
        }
        return @{ Output = $output; Errors = $errors }
    }
    finally { $process.Dispose() }
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
}

# This calls the shipped CLI and real Ruby loader; there is no interpreter test double.
$result = Invoke-TestProcess $executable @('list')
Assert-True ($result.Output -eq '') 'New prefix must list no packages.'
$result = Invoke-TestProcess $executable @('install', 'jq')
Write-Host $result.Output
$jq = Join-Path $prefix 'bin/jq.exe'
$keg = Join-Path $prefix 'Cellar/jq/1.8.2'
Assert-True (Test-Path -LiteralPath "$keg/bin/jq.exe") 'jq missing from its versioned Cellar.'
Assert-True ($null -ne ([IO.FileInfo]::new($jq)).LinkTarget) 'jq must be a real symlink.'
$result = Invoke-TestProcess $jq @('-r', '.answer') '{"answer":42}'
Assert-True ($result.Output -eq '42') 'Installed jq failed to process stdin.'
$result = Invoke-TestProcess $executable @('list')
Assert-True ($result.Output -eq 'jq') 'brew list must contain jq.'
$result = Invoke-TestProcess $executable @('list', '--versions')
Assert-True ($result.Output -eq 'jq 1.8.2') 'Version listing mismatch.'
$result = Invoke-TestProcess $executable @('list', 'jq')
Assert-True ($result.Output -eq "$keg\bin\jq.exe") 'Formula file listing mismatch.'
$result = Invoke-TestProcess $executable @('install', 'jq')
Assert-True ($result.Errors.Contains('already installed and up-to-date.')) 'Repeated install must warn.'
$null = Invoke-TestProcess $executable @('unlink', 'jq')
Assert-True (!(Test-Path -LiteralPath $jq)) 'unlink left jq in bin.'
$result = Invoke-TestProcess $executable @('install', 'jq')
Assert-True ($result.Errors.Contains("already installed, it's just not linked.")) 'Unlinked installation warning mismatch.'
Assert-True (!(Test-Path -LiteralPath $jq)) 'Repeated install silently relinked jq.'
$null = Invoke-TestProcess $executable @('link', 'jq')
$result = Invoke-TestProcess $jq @('--version')
Assert-True ($result.Output -eq 'jq-1.8.2') 'Relinked jq failed.'
$null = Invoke-TestProcess $executable @('uninstall', 'jq')
Assert-True (!(Test-Path -LiteralPath $jq)) 'Uninstall left jq in bin.'
Assert-True (!(Test-Path -LiteralPath (Join-Path $prefix 'Cellar/jq'))) 'Uninstall left the jq rack.'
$result = Invoke-TestProcess $executable @('list')
Assert-True ($result.Output -eq '') 'Uninstalled jq still listed.'
$result = Invoke-TestProcess -File $executable -Arguments @('uninstall', 'jq') -ExpectedCode 1
Assert-True ($result.Errors.Contains('No such keg:')) 'Missing keg error mismatch.'
Write-Host "PASS: real Ruby formula -> CLI -> jq -> uninstall. Artifacts: $prefix"
