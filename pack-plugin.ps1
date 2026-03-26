param(
    [string]$Configuration = "Release",
    [string]$Version = "0.1.0.0"
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

$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLower()
$size = (Get-Item $zipPath).Length

Write-Host "ZIP: $zipPath"
Write-Host "SHA256: $hash"
Write-Host "SIZE: $size"
Write-Host "Update dist/manifest.json -> versions[0].checksum and downloads[0].size"
Write-Host "Update dist/manifest.json -> downloads[0].url with your hosted ZIP URL"

if (Test-Path $manifestPath) {
    Write-Host "Manifest template exists at: $manifestPath"
}
