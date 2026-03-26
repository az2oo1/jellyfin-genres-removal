using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;
using JellyfinGenreRestriction.Core;

namespace JellyfinGenreRestriction.Tasks;

public sealed class GenreTagSyncTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<GenreTagSyncTask> _logger;

    public string Name => "Parental Control: Sync Ultimate Tags";

    public string Description => "Scans library and applies Advanced Keyword, Genre, Studio, and Whitelist tags for parental-control hiding.";

    public string Category => "Library";

    public string Key => "GenreRestrictionUltimateSync";

    public GenreTagSyncTask(ILibraryManager libraryManager, ILogger<GenreTagSyncTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = Plugin.Instance.Configuration;

        // Fail fast if no configurations exist
        if ((config.GenreToTagMapList == null || config.GenreToTagMapList.Count == 0) &&
            (config.KeywordToTagMapList == null || config.KeywordToTagMapList.Count == 0) &&
            (config.StudioToTagMapList == null || config.StudioToTagMapList.Count == 0) &&
            (config.Whitelist == null || !config.Whitelist.Enabled))
        {
            _logger.LogInformation("No Parental Control mappings configured. Skipping task.");
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

                bool changed = ParentalTagger.EvaluateAndTag(item, config, _logger);

                if (changed)
                {
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

        _logger.LogInformation("Parental Control sync complete. Processed {Total} items. Updated {Updated} items.", totalItems, updated);
        progress.Report(100);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }
}
