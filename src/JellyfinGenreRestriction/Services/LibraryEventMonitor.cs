using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using Jellyfin.Data.Enums;
using JellyfinGenreRestriction.Core;

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
        _logger.LogInformation("Library Event Monitor started for real-time Ultimate Parental Control tagging.");
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

        bool changed = ParentalTagger.EvaluateAndTag(item, config, _logger);

        // Fire-and-forget sync saving
        if (changed)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update item {ItemName} after adding Parental Control tag.", item.Name);
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
