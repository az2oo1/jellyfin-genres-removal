using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using Jellyfin.Data.Enums;
using System.Collections.Generic;

namespace JellyfinGenreRestriction.Services;

public class LibraryEventMonitor : IHostedService, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryEventMonitor> _logger;

    public LibraryEventMonitor(ILibraryManager libraryManager, ILogger<LibraryEventMonitor> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnItemAdded;
        _libraryManager.ItemUpdated += OnItemUpdated;
        _logger.LogInformation("Library Event Monitor started for real-time genre-tag sync.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        _libraryManager.ItemUpdated -= OnItemUpdated;
        _logger.LogInformation("Library Event Monitor stopped.");
        return Task.CompletedTask;
    }

    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        ProcessItem(e.Item, e.UpdateReason);
    }

    private void OnItemUpdated(object? sender, ItemChangeEventArgs e)
    {
        ProcessItem(e.Item, e.UpdateReason);
    }

    private void ProcessItem(BaseItem? item, ItemUpdateType updateReason)
    {
        if (item == null || item.IsFolder) return;

        // Skip metadata updates caused by this very plugin
        if (updateReason == ItemUpdateType.MetadataEdit) return;

        var config = Plugin.Instance.Configuration;
        var genreMap = config.GenreToTagMapList;

        if (genreMap == null || genreMap.Count == 0) return;

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
                    _logger.LogInformation("Real-time: Added tag '{Tag}' to item '{ItemName}' because of genre '{Genre}'", targetTag, item.Name, genre);
                }
            }
        }

        if (changed)
        {
            item.Tags = currentTags.ToArray();
            
            // Fire-and-forget sync saving to prevent event handler blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update item {ItemName} after adding genre restriction tag.", item.Name);
                }
            });
        }
    }

    public void Dispose()
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        _libraryManager.ItemUpdated -= OnItemUpdated;
    }
}
