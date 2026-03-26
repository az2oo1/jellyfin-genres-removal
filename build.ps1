param(
    [switch]$InstallSdk,
    [switch]$Release = $true
)

$ErrorActionPreference = 'Stop'

function Test-Dotnet {
    try {
        dotnet --version | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

if (-not (Test-Dotnet)) {
    if (-not $InstallSdk) {
        Write-Host 'dotnet SDK is not installed. Re-run with -InstallSdk to bootstrap .NET 9.' -ForegroundColor Yellow
        exit 1
    }

    $installer = Join-Path $PSScriptRoot 'dotnet-install.ps1'
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
    & $installer -Version 9.0.100 -InstallDir "$env:USERPROFILE\.dotnet"

    $env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
}

$configuration = if ($Release) { 'Release' } else { 'Debug' }

dotnet restore .\jellyfin-genres-removal.sln --configfile .\NuGet.config
dotnet build .\jellyfin-genres-removal.sln -c $configuration --no-restore
