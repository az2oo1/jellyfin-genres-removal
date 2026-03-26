using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace JellyfinGenreRestriction;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public List<UserGenrePolicy> UserPolicies { get; set; } = new();

    public Dictionary<string, string> GenreToTagMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool EnableScheduledTagSync { get; set; } = true;
}

public sealed class UserGenrePolicy
{
    public string UserId { get; set; } = string.Empty;

    public List<string> BlockedGenres { get; set; } = new();
}
