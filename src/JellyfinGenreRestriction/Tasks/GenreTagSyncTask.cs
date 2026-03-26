using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace JellyfinGenreRestriction.Tasks;

public sealed class GenreTagSyncTask : IScheduledTask
{
    public string Name => "Genre Restriction: Sync Genre Tags";

    public string Description => "Maps configured genres to tags for parental-control based hiding.";

    public string Category => "Library";

    public string Key => "GenreRestrictionTagSync";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        progress.Report(100);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }
}
