using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JellyfinGenreRestriction.Services;

public class LibraryEventMonitor : IHostedService
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
        _libraryManager.ItemAdded += LibraryManager_ItemAddedOrUpdated;
        _libraryManager.ItemUpdated += LibraryManager_ItemAddedOrUpdated;       

        return Task.CompletedTask;
    }

    private async void LibraryManager_ItemAddedOrUpdated(object? sender, ItemChangeEventArgs e)
    {
        if (e.Item == null || e.Item.IsFolder)
        {
            return;
        }

        var config = Plugin.Instance.Configuration;
        if (!config.EnableScheduledTagSync)
        {
            return;
        }

        var genreMap = config.GenreToTagMapList;
        if (genreMap == null || genreMap.Count == 0)
        {
            return;
        }

        var item = e.Item;
        var itemGenres = item.Genres ?? Array.Empty<string>();
        var currentTags = item.Tags?.ToList() ?? new System.Collections.Generic.List<string>();
        bool changed = false;

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
                    _logger.LogInformation("Added tag '{Tag}' to recently updated item '{ItemName}' because of genre '{Genre}'", targetTag, item.Name, genre);  
                }
            }
        }

        if (changed)
        {
            item.Tags = currentTags.ToArray();
            try
            {
                await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update tags for item '{ItemName}'", item.Name);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= LibraryManager_ItemAddedOrUpdated;
        _libraryManager.ItemUpdated -= LibraryManager_ItemAddedOrUpdated;       

        return Task.CompletedTask;
    }
}
