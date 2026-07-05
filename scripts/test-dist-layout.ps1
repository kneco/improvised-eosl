[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ZipPath
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression.FileSystem

$resolvedZip = (Resolve-Path -LiteralPath $ZipPath).Path
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedZip)

try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $entrySet = New-Object 'System.Collections.Generic.HashSet[string]' (
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $entries) {
        [void]$entrySet.Add($entry)
    }

    $requiredFiles = @(
        "ImprovisedEosl.Spike.SyncModal.exe",
        "README.txt",
        "LICENSE.txt",
        "THIRD-PARTY-NOTICES.txt",
        "config/compatibility-profiles.json",
        "runtimes/win-x64/native/WebView2Loader.dll"
    )

    foreach ($requiredFile in $requiredFiles) {
        if (-not $entrySet.Contains($requiredFile)) {
            throw "Distribution ZIP is missing required entry '$requiredFile'."
        }
    }

    if (-not ($entries | Where-Object { $_ -like "pages/*.html" })) {
        throw "Distribution ZIP must contain HTML pages under 'pages/'."
    }

    $versionedWrapper = $entries | Where-Object {
        $_ -match '^ImprovisedEosl-[^/]+-win-x64/'
    }
    if ($versionedWrapper) {
        throw "Distribution ZIP contains the removed versioned wrapper directory."
    }

    Write-Host "Distribution layout passed: $resolvedZip ($($entries.Count) entries)"
}
finally {
    $archive.Dispose()
}
