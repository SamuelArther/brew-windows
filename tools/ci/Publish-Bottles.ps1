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
        # Skip only an identical upload (a re-run of the same build). A rebuild of the same version has
        # different bytes, and the merge below writes its SHA-256, so the release asset must be replaced.
        $leaf = Split-Path $file -Leaf
        $sha = (Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant()
        $assets = gh api "repos/$Repo/releases/tags/$tag" --jq '.assets[] | .name + " " + (.digest // "")' 2> $null
        if ($LASTEXITCODE -eq 0 -and $assets -contains "$leaf sha256:$sha") { $published += $json.FullName; continue }
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
# Merge in slices: one command line holding every JSON path overflows Windows' 32K limit.
for ($i = 0; $i -lt $published.Count; $i += 50) {
    $slice = $published[$i..([Math]::Min($i + 50, $published.Count) - 1)]
    & $Brew bottle --merge --write --no-commit @slice
    if ($LASTEXITCODE -ne 0) { throw 'bottle merge failed' }
}
Write-Host "published $($published.Count) bottles"
