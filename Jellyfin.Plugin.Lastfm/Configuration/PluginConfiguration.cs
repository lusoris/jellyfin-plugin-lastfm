// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Configuration;

using MediaBrowser.Model.Plugins;
using Models;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    // ============================================
    // API Configuration (Admin-only)
    // ============================================

    /// <summary>
    /// Gets or sets the Last.fm API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Last.fm API secret.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    // ============================================
    // Global Defaults
    // ============================================

    /// <summary>
    /// Gets or sets the minimum track duration for scrobbling (seconds).
    /// Default: 30 (Last.fm requirement).
    /// </summary>
    public int MinimumTrackDuration { get; set; } = 30;

    /// <summary>
    /// Gets or sets the scrobble threshold percentage (0-100).
    /// Track must be played this % OR 4 minutes.
    /// Default: 50 (Last.fm requirement).
    /// </summary>
    public int ScrobbleThresholdPercent { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum playtime before scrobble in seconds.
    /// Default: 240 (4 minutes, Last.fm requirement).
    /// </summary>
    public int ScrobbleThresholdSeconds { get; set; } = 240;

    /// <summary>
    /// Gets or sets the duplicate scrobble detection window (seconds).
    /// Default: 15.
    /// </summary>
    public int DuplicateScrobbleWindow { get; set; } = 15;

    /// <summary>
    /// Gets or sets the default play count sync strategy.
    /// </summary>
    public PlayCountSyncStrategy DefaultPlayCountStrategy { get; set; } = PlayCountSyncStrategy.Max;

    /// <summary>
    /// Gets or sets the default favorite conflict resolution.
    /// </summary>
    public ConflictResolution DefaultFavoriteConflict { get; set; } = ConflictResolution.NewestWins;

    // ============================================
    // Per-User Configuration
    // ============================================

    /// <summary>
    /// Gets or sets the configured Last.fm users.
    /// </summary>
    public LastfmUser[] LastfmUsers { get; set; } = [];

    // ============================================
    // Helper Methods
    // ============================================

    /// <summary>
    /// Gets the Last.fm user configuration for a Jellyfin user.
    /// </summary>
    /// <param name="jellyfinUserId">The Jellyfin user ID.</param>
    /// <returns>The user configuration, or null if not found.</returns>
    public LastfmUser? GetUserConfig(Guid jellyfinUserId)
    {
        return Array.Find(LastfmUsers, u => u.JellyfinUserId == jellyfinUserId);
    }

    /// <summary>
    /// Gets a value indicating whether the plugin is configured (has API credentials).
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(ApiSecret);
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
