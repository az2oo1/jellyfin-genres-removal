param(
    [string]$Configuration = "Release",
    [string]$Version = "0.2.0.4",
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
    
    $existing = $null
    if ($entry.versions) {
        $existing = $entry.versions | Where-Object { $_.version -eq $Version }
    } else {
        $entry | Add-Member -MemberType NoteProperty -Name "versions" -Value @()
    }

    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    
    if ($existing) {
        $existing.checksum = $hash
        $existing.timestamp = $timestamp
        if (-not [string]::IsNullOrWhiteSpace($DownloadUrl)) {
            $existing.sourceUrl = $DownloadUrl
        }
    } else {
        $newVer = [PSCustomObject]@{
            version = $Version
            changelog = "Added new features."
            targetAbi = "10.11.0.0"
            sourceUrl = $DownloadUrl
            checksum = $hash
            timestamp = $timestamp
        }
        $entry.versions = @($newVer) + @($entry.versions)
    }

    $json = "[$($manifest | ConvertTo-Json -Depth 12)]"
    Set-Content -Path $manifestPath -Value $json -Encoding UTF8
    Write-Host "Manifest updated: $manifestPath"
}

Write-Host "ZIP: $zipPath"
Write-Host "MD5: $hash"
Write-Host "Version: $Version"

if (-not $UpdateManifest) {
    Write-Host "Update dist/manifest.json -> versions[0].checksum"
    Write-Host "Update dist/manifest.json -> versions[0].sourceUrl with your hosted ZIP URL"
}
