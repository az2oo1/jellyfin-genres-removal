using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace JellyfinGenreRestriction;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public List<UserGenrePolicy> UserPolicies { get; set; } = new();

    public List<GenreTagMapping> GenreToTagMapList { get; set; } = new();       

    public List<KeywordTagMapping> KeywordToTagMapList { get; set; } = new();

    public List<StudioTagMapping> StudioToTagMapList { get; set; } = new();

    public WhitelistSettings Whitelist { get; set; } = new();

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

public sealed class KeywordTagMapping
{
    public string Keyword { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}

public sealed class StudioTagMapping
{
    public string Studio { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}

public sealed class WhitelistSettings
{
    public bool Enabled { get; set; } = false;
    public List<string> SafeGenres { get; set; } = new();
    public List<string> SafeKeywords { get; set; } = new();
    public List<string> SafeStudios { get; set; } = new();
    public string RestrictedTag { get; set; } = string.Empty;
}
