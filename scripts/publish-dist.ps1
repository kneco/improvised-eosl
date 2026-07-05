[CmdletBinding()]
param(
    [ValidatePattern("^[0-9A-Za-z][0-9A-Za-z.-]{0,63}$")]
    [string]$Version = "mvp",
    [ValidateSet("win-x64")]
    [string]$Runtime = "win-x64",
    [string]$DotNet = "dotnet"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj"
$distRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "dist"))
$packageName = "ImprovisedEosl-$Version-$Runtime"
$packageDir = [System.IO.Path]::GetFullPath((Join-Path $distRoot $packageName))
$zipPath = Join-Path $distRoot "$packageName.zip"

$distPrefix = $distRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (-not $packageDir.StartsWith($distPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Package directory must remain under the repository dist directory."
}

if (Test-Path -LiteralPath $packageDir) {
    Remove-Item -LiteralPath $packageDir -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

& $DotNet publish $project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $packageDir `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item `
    -LiteralPath (Join-Path $repoRoot "packaging\README.txt") `
    -Destination (Join-Path $packageDir "README.txt")

Copy-Item `
    -LiteralPath (Join-Path $repoRoot "LICENSE") `
    -Destination (Join-Path $packageDir "LICENSE.txt")

Copy-Item `
    -LiteralPath (Join-Path $repoRoot "THIRD-PARTY-NOTICES.md") `
    -Destination (Join-Path $packageDir "THIRD-PARTY-NOTICES.txt")

Compress-Archive -LiteralPath $packageDir -DestinationPath $zipPath -CompressionLevel Optimal

$zip = Get-Item -LiteralPath $zipPath
Write-Host "Created $($zip.FullName) ($([math]::Round($zip.Length / 1MB, 1)) MiB)"
