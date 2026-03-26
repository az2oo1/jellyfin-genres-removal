using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;

namespace JellyfinGenreRestriction.Services;

public interface IGenrePolicyService
{
    bool IsBlockedForUser(Guid userId, BaseItem item);
    IReadOnlySet<string> GetBlockedGenresForUser(Guid userId);
}

public sealed class GenrePolicyService : IGenrePolicyService
{
    public bool IsBlockedForUser(Guid userId, BaseItem item)
    {
        var blocked = GetBlockedGenresForUser(userId);
        if (blocked.Count == 0)
        {
            return false;
        }

        var itemGenres = item.Genres ?? Array.Empty<string>();
        for (var i = 0; i < itemGenres.Length; i++)
        {
            if (blocked.Contains(itemGenres[i]))
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlySet<string> GetBlockedGenresForUser(Guid userId)
    {
        var cfg = Plugin.Instance.Configuration;
        var compactUserId = userId.ToString("N");
        var fullUserId = userId.ToString();

        var policy = cfg.UserPolicies.FirstOrDefault(p =>
            string.Equals(p.UserId, compactUserId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.UserId, fullUserId, StringComparison.OrdinalIgnoreCase));

        if (policy is null || policy.BlockedGenres.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return new HashSet<string>(
            policy.BlockedGenres.Where(g => !string.IsNullOrWhiteSpace(g)),
            StringComparer.OrdinalIgnoreCase);
    }
}
