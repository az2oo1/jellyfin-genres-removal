# jellyfin-genres-removal

Jellyfin plugin scaffold for Jellyfin Server 10.11.6 that supports per-user, genre-based content restriction via genre-to-tag synchronization.

## What It Does

- Maintains plugin configuration for:
  - Per-user blocked genres
  - Genre-to-tag mappings
- Provides a scheduled task that scans library items and adds mapped tags when genres match.
- Designed to work with Jellyfin native parental tag blocking for specific users.

## Why This Approach

This starter uses scheduled tag synchronization because it is safer and more stable than deep query interception for most deployments. It avoids raw SQL and keeps changes incremental to reduce write load.

## Build

```powershell
./build.ps1 -InstallSdk
```

If .NET 9 SDK is already installed:

```powershell
./build.ps1
```

If restore fails with a source error, run using the repository NuGet config:

```powershell
dotnet restore .\jellyfin-genres-removal.sln --configfile .\NuGet.config
dotnet build .\jellyfin-genres-removal.sln -c Release --no-restore
```

## Deploy Plugin

Copy the build output DLLs to one of these paths and restart Jellyfin:

- Linux: `/var/lib/jellyfin/plugins/GenreRestriction/`
- Windows: `%ProgramData%/Jellyfin/Server/plugins/GenreRestriction/`
- Docker: `/config/plugins/GenreRestriction/`

## Usage Pattern

1. Configure `GenreToTagMap` in plugin config.
2. Run scheduled task: `Genre Restriction: Sync Genre Tags`.
3. In Jellyfin admin, block those tags for target users in parental controls.

## Notes

- Target runtime: .NET 9
- Target server line: Jellyfin 10.11.6
- `11.11.6` is not an actual Jellyfin release.

## Publish For Jellyfin URL Install

1. Build and package plugin ZIP:

```powershell
./pack-plugin.ps1
```

Or auto-update manifest with hash/size/version in one step:

```powershell
./pack-plugin.ps1 -UpdateManifest -Version 0.1.0.0 -DownloadUrl "https://YOUR_HOST/releases/JellyfinGenreRestriction_0.1.0.0.zip"
```

2. Open `dist/manifest.json` and update:
- `versions[0].checksum` with printed SHA256.
- `versions[0].downloads[0].size` with printed size.
- `versions[0].downloads[0].url` with your direct hosted ZIP URL.

3. Host both files publicly:
- `dist/manifest.json` at a public direct URL.
- The ZIP file at the URL referenced inside manifest `downloads[0].url`.

4. In Jellyfin: `Dashboard -> Plugins -> Repositories -> +` and paste:

```text
https://raw.githubusercontent.com/az2oo1/jellyfin-genres-removal/main/dist/manifest.json
```
