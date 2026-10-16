# Uploads each bottle to its own GitHub release (<name>-<pkg_version>), paced under GitHub's
# content-creation limits, then writes the bottle blocks into the formulae.
param(
    [Parameter(Mandatory)] [string] $Dir,
    [Parameter(Mandatory)] [string] $Repo,
    [string] $Brew = (Join-Path $PSScriptRoot '..\..\src\Brew\bin\Release\net10.0\win-x64\brew.exe'),
    [int] $DelaySeconds = 8
)
$ErrorActionPreference = 'Stop'
$published = @()
foreach ($json in Get-ChildItem $Dir -Filter '*.bottle.json') {
    $data = Get-Content $json.FullName -Raw | ConvertFrom-Json
    foreach ($entry in $data.PSObject.Properties) {
        $name = $entry.Name
        $version = $entry.Value.formula.pkg_version
        $file = Join-Path $Dir $entry.Value.bottle.tags.x86_64_windows.local_filename
        $tag = "$name-$version"
        gh release view $tag --repo $Repo *> $null
        if ($LASTEXITCODE -eq 0) { gh release upload $tag $file --repo $Repo --clobber }
        else {
            gh release create $tag $file --repo $Repo --title "$name $version" `
                --notes "Windows (x86_64) bottle for $name $version, built from upstream source by the bottles workflow."
        }
        if ($LASTEXITCODE -ne 0) { Write-Warning "upload failed: $name"; continue }
        $published += $json.FullName
        Start-Sleep -Seconds $DelaySeconds
    }
}
if ($published.Count -gt 0) {
    & $Brew bottle --merge --write --no-commit @published
    if ($LASTEXITCODE -ne 0) { throw 'bottle merge failed' }
}
Write-Host "published $($published.Count) bottles"
