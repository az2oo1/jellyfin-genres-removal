using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace JellyfinGenreRestriction.Core;

public static class ParentalTagger
{
    public static bool EvaluateAndTag(BaseItem item, PluginConfiguration config, ILogger logger)
    {
        if (item == null) return false;

        bool changed = false;
        var currentTags = item.Tags != null ? item.Tags.ToList() : new List<string>();
        var itemGenres = item.Genres ?? Array.Empty<string>();
        var itemStudios = item.Studios ?? Array.Empty<string>();
        var title = item.Name ?? string.Empty;
        var overview = item.Overview ?? string.Empty;

        // 1. Keyword Blacklists
        var keywordMaps = config.KeywordToTagMapList ?? new List<KeywordTagMapping>();
        foreach (var kwMap in keywordMaps)
        {
            if (string.IsNullOrWhiteSpace(kwMap.Keyword) || string.IsNullOrWhiteSpace(kwMap.Tag)) continue;
            
            if (title.Contains(kwMap.Keyword, StringComparison.OrdinalIgnoreCase) || 
                overview.Contains(kwMap.Keyword, StringComparison.OrdinalIgnoreCase))
            {
                if (!currentTags.Contains(kwMap.Tag, StringComparer.OrdinalIgnoreCase))
                {
                    currentTags.Add(kwMap.Tag);
                    changed = true;
                    logger.LogInformation("ParentalControl: Added tag '{Tag}' to item '{ItemName}' due to Blacklisted Keyword '{Keyword}'", kwMap.Tag, title, kwMap.Keyword);
                }
            }
        }

        // 2. Genre Blacklists
        var genreMaps = config.GenreToTagMapList ?? new List<GenreTagMapping>();
        foreach (var genre in itemGenres)
        {
            var mapEntry = genreMaps.FirstOrDefault(kvp => string.Equals(kvp.Genre, genre, StringComparison.OrdinalIgnoreCase));
            if (mapEntry != null && !string.IsNullOrWhiteSpace(mapEntry.Tag))
            {
                if (!currentTags.Contains(mapEntry.Tag, StringComparer.OrdinalIgnoreCase))
                {
                    currentTags.Add(mapEntry.Tag);
                    changed = true;
                    logger.LogInformation("ParentalControl: Added tag '{Tag}' to item '{ItemName}' due to Blacklisted Genre '{Genre}'", mapEntry.Tag, title, genre);
                }
            }
        }

        // 3. Studio Blacklists
        var studioMaps = config.StudioToTagMapList ?? new List<StudioTagMapping>();
        foreach (var studio in itemStudios)
        {
            var mapEntry = studioMaps.FirstOrDefault(kvp => string.Equals(kvp.Studio, studio, StringComparison.OrdinalIgnoreCase));
            if (mapEntry != null && !string.IsNullOrWhiteSpace(mapEntry.Tag))
            {
                if (!currentTags.Contains(mapEntry.Tag, StringComparer.OrdinalIgnoreCase))
                {
                    currentTags.Add(mapEntry.Tag);
                    changed = true;
                    logger.LogInformation("ParentalControl: Added tag '{Tag}' to item '{ItemName}' due to Blacklisted Studio '{Studio}'", mapEntry.Tag, title, studio);
                }
            }
        }

        // 4. Whitelist Mode
        var wl = config.Whitelist;
        if (wl != null && wl.Enabled && !string.IsNullOrWhiteSpace(wl.RestrictedTag))
        {
            bool isSafe = false;

            // Check if it has a Safe Genre
            if (wl.SafeGenres != null && wl.SafeGenres.Any(sg => itemGenres.Contains(sg, StringComparer.OrdinalIgnoreCase)))
            {
                isSafe = true;
            }

            // Check if it has a Safe Studio
            if (!isSafe && wl.SafeStudios != null && wl.SafeStudios.Any(ss => itemStudios.Contains(ss, StringComparer.OrdinalIgnoreCase)))
            {
                isSafe = true;
            }

            // Check if it has a Safe Keyword
            if (!isSafe && wl.SafeKeywords != null && wl.SafeKeywords.Any(sk => 
                title.Contains(sk, StringComparison.OrdinalIgnoreCase) || 
                overview.Contains(sk, StringComparison.OrdinalIgnoreCase)))
            {
                isSafe = true;
            }

            if (!isSafe)
            {
                if (!currentTags.Contains(wl.RestrictedTag, StringComparer.OrdinalIgnoreCase))
                {
                    currentTags.Add(wl.RestrictedTag);
                    changed = true;
                    logger.LogInformation("ParentalControl: Added Whitelist Restricted tag '{Tag}' to item '{ItemName}' because it did not match any Safe Genres, Studios, or Keywords.", wl.RestrictedTag, title);
                }
            }
        }

        if (changed)
        {
            item.Tags = currentTags.ToArray();
        }

        return changed;
    }
}
