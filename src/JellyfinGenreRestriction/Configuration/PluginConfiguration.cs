using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace JellyfinGenreRestriction;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public List<UserGenrePolicy> UserPolicies { get; set; } = new();

    // Use a List of KeyValuePairs because XmlSerializer cannot serialize IDictionary
    public List<GenreTagMapping> GenreToTagMapList { get; set; } = new();

    public bool EnableScheduledTagSync { get; set; } = true;
}

public sealed class UserGenrePolicy
{
    public string UserId { get; set; } = string.Empty;

    public List<string> BlockedGenres { get; set; } = new();
}

public sealed class GenreTagMapping
{
    public string Genre { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}
