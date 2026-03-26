using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;

namespace JellyfinGenreRestriction.Tasks;

public sealed class GenreTagSyncTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<GenreTagSyncTask> _logger;

    public string Name => "Genre Restriction: Sync Genre Tags";

    public string Description => "Maps configured genres to tags for parental-control based hiding.";

    public string Category => "Library";

    public string Key => "GenreRestrictionTagSync";

    public GenreTagSyncTask(ILibraryManager libraryManager, ILogger<GenreTagSyncTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = Plugin.Instance.Configuration;
        var genreMap = config.GenreToTagMapList;

        if (genreMap == null || genreMap.Count == 0)
        {
            _logger.LogInformation("No genre-to-tag mappings configured. Skipping sync.");
            progress.Report(100);
            return;
        }

        var itemIds = _libraryManager.GetItemIds(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.Episode, BaseItemKind.Audio, BaseItemKind.MusicVideo, BaseItemKind.Video, BaseItemKind.AudioBook, BaseItemKind.Book },
            IsFolder = false,
            Recursive = true
        });

        int totalItems = itemIds.Count;
        if (totalItems == 0)
        {
            progress.Report(100);
            return;
        }

        int processed = 0;
        int updated = 0;

        foreach (var id in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var item = _libraryManager.GetItemById(id);
                if (item == null) continue;

                bool changed = false;
                var itemGenres = item.Genres ?? Array.Empty<string>();
                var currentTags = item.Tags != null ? item.Tags.ToList() : new List<string>();

                foreach (var genre in itemGenres)
                {
                    var mapEntry = genreMap.FirstOrDefault(kvp => string.Equals(kvp.Genre, genre, StringComparison.OrdinalIgnoreCase));
                    if (mapEntry != null && !string.IsNullOrWhiteSpace(mapEntry.Tag))
                    {
                        var targetTag = mapEntry.Tag;
                        if (!currentTags.Contains(targetTag, StringComparer.OrdinalIgnoreCase))
                        {
                            currentTags.Add(targetTag);
                            changed = true;
                            _logger.LogInformation("Added tag '{Tag}' to item '{ItemName}' because of genre '{Genre}'", targetTag, item.Name, genre);
                        }
                    }
                }

                if (changed)
                {
                    item.Tags = currentTags.ToArray();
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
                    updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load or process item {Id}", id);
            }

            processed++;
            progress.Report((processed / (double)totalItems) * 100);
        }

        _logger.LogInformation("Genre sync complete. Processed {Total} items. Updated {Updated} items.", totalItems, updated);
        progress.Report(100);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }
}
