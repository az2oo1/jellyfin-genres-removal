param(
    [string]$Configuration = "Release",
    [string]$Version = "0.1.0.0",
    [string]$DownloadUrl = "",
    [switch]$UpdateManifest
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$project = Join-Path $root "src\JellyfinGenreRestriction\JellyfinGenreRestriction.csproj"
$outDir = Join-Path $root "artifacts"
$stageDir = Join-Path $outDir "stage"
$zipName = "JellyfinGenreRestriction_$Version.zip"
$zipPath = Join-Path $outDir $zipName
$manifestPath = Join-Path $root "dist\manifest.json"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

# Ensure deterministic restore source.
dotnet restore $project --configfile (Join-Path $root "NuGet.config")
dotnet build $project -c $Configuration --no-restore

$tfmOut = Join-Path $root "src\JellyfinGenreRestriction\bin\$Configuration\net9.0"
Copy-Item (Join-Path $tfmOut "*.dll") $stageDir -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $tfmOut "*.pdb") $stageDir -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $tfmOut "*.json") $stageDir -Force -ErrorAction SilentlyContinue

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath

$hash = (Get-FileHash $zipPath -Algorithm MD5).Hash.ToLower()

if ($UpdateManifest) {
    if (-not (Test-Path $manifestPath)) {
        throw "Manifest file not found: $manifestPath"
    }

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if (-not $manifest -or $manifest.Count -eq 0) {
        throw "Manifest does not contain plugin entries."
    }

    $entry = $manifest[0]
    if (-not $entry.versions -or $entry.versions.Count -eq 0) {
        throw "Manifest does not contain versions entries."
    }

    $entry.versions[0].version = $Version
    $entry.versions[0].checksum = $hash
    $entry.versions[0].timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

    if (-not [string]::IsNullOrWhiteSpace($DownloadUrl)) {
        $entry.versions[0].sourceUrl = $DownloadUrl
    }

    $json = "[$($manifest | ConvertTo-Json -Depth 12)]"
    Set-Content -Path $manifestPath -Value $json -Encoding UTF8
    Write-Host "Manifest updated: $manifestPath"
}

Write-Host "ZIP: $zipPath"
Write-Host "SHA256: $hash"
Write-Host "SIZE: $size"
Write-Host "Version: $Version"

if (-not $UpdateManifest) {
    Write-Host "Update dist/manifest.json -> versions[0].checksum"
    Write-Host "Update dist/manifest.json -> versions[0].sourceUrl with your hosted ZIP URL"
}

if (Test-Path $manifestPath) {
    Write-Host "Manifest template exists at: $manifestPath"
    $manifestValidation = Get-Content $manifestPath -Raw | ConvertFrom-Json     
    $m = $manifestValidation[0].versions[0]
    if ($m.checksum -eq "REPLACE_WITH_MD5" -or $m.sourceUrl -like "https://YOUR_HOST/*") {
        Write-Warning "Manifest might still contain placeholders or non-zip URLs. Make sure sourceUrl points to the exact .zip file."
    }
}
