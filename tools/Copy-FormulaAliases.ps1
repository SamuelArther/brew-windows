param([Parameter(Mandatory)][string]$Source, [Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
foreach ($tap in Get-ChildItem -LiteralPath $Source -Directory) {
    $aliases = Join-Path $tap.FullName 'Aliases'
    if (-not (Test-Path -LiteralPath $aliases)) { continue }
    foreach ($alias in Get-ChildItem -LiteralPath $aliases -File) {
        $target = $alias.LinkTarget
        if (-not $target -and $alias.LinkType -eq 'SymbolicLink') { $target = [string](@($alias.Target)[0]) }
        if (-not $target) { $target = [IO.File]::ReadAllText($alias.FullName).Trim() }
        $outputDirectory = Join-Path (Join-Path $Destination $tap.Name) 'Aliases'
        [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
        # Ship portable Git-style alias text rather than dereferencing the symlink into Ruby source.
        [IO.File]::WriteAllText((Join-Path $outputDirectory $alias.Name), $target, [Text.UTF8Encoding]::new($false))
    }
}
