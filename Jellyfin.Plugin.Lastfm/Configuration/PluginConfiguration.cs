// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Lastfm.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration, IPluginConfiguration
{
    /// <summary>
    /// Gets or sets the Last.fm API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Last.fm API secret.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configured Last.fm users.
    /// </summary>
    public List<LastfmUser> LastfmUsers { get; set; } = new();

    /// <summary>
    /// Gets or sets the default play count sync strategy.
    /// </summary>
    public PlayCountSyncStrategy DefaultPlayCountStrategy { get; set; } = PlayCountSyncStrategy.Max;

    /// <summary>
    /// Gets a value indicating whether the plugin is configured.
    /// </summary>
    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(ApiSecret);
    }

    /// <summary>
    /// Gets the Last.fm user configuration for a specific Jellyfin user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <returns>Last.fm user configuration or null if not found.</returns>
    public LastfmUser? GetUserConfig(Guid userId)
    {
        return LastfmUsers.FirstOrDefault(u => u.JellyfinUserId == userId);
    }
}

/// <summary>
/// Strategies for syncing play counts from Last.fm.
/// </summary>
public enum PlayCountSyncStrategy
{
    /// <summary>
    /// Add Last.fm count to existing Jellyfin count.
    /// </summary>
    Add,

    /// <summary>
    /// Replace Jellyfin count with Last.fm count.
    /// </summary>
    Replace,

    /// <summary>
    /// Use the higher of the two counts.
    /// </summary>
    Max
}

/// <summary>
/// Conflict resolution strategies for bidirectional sync.
/// </summary>
public enum ConflictResolution
{
    /// <summary>
    /// Last.fm data takes precedence.
    /// </summary>
    LastfmWins,

    /// <summary>
    /// Jellyfin data takes precedence.
    /// </summary>
    JellyfinWins,

    /// <summary>
    /// Most recently modified data wins.
    /// </summary>
    NewestWins
}
