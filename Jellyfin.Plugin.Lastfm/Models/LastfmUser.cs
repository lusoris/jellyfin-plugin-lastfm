// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models;

/// <summary>
/// Last.fm user configuration.
/// </summary>
public class LastfmUser
{
    /// <summary>
    /// Gets or sets the Last.fm username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Last.fm session key.
    /// </summary>
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the associated Jellyfin user ID.
    /// </summary>
    public Guid JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets the user options.
    /// </summary>
    public LastfmUserOptions Options { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether this user has a valid session.
    /// </summary>
    public bool HasValidSession => !string.IsNullOrEmpty(SessionKey);
}

/// <summary>
/// Per-user options for Last.fm features.
/// </summary>
public class LastfmUserOptions
{
    // ============================================
    // Scrobbling (JF → Last.fm)
    // ============================================

    /// <summary>
    /// Gets or sets a value indicating whether scrobbling is enabled.
    /// </summary>
    public bool ScrobbleEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to send now playing updates.
    /// </summary>
    public bool NowPlayingEnabled { get; set; } = true;

    // ============================================
    // Sync JF → Last.fm
    // ============================================

    /// <summary>
    /// Gets or sets a value indicating whether to sync favorites to loved tracks.
    /// </summary>
    public bool SyncFavoritesToLoved { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to sync unfavorites to unloved tracks.
    /// </summary>
    public bool SyncUnfavoritesToUnloved { get; set; } = true;

    // ============================================
    // Sync Last.fm → JF
    // ============================================

    /// <summary>
    /// Gets or sets a value indicating whether to import loved tracks as favorites.
    /// </summary>
    public bool ImportLovedTracks { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to import play counts.
    /// </summary>
    public bool ImportPlayCounts { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to import last played dates.
    /// </summary>
    public bool ImportLastPlayedDates { get; set; } = false;

    // ============================================
    // Sync State (Internal)
    // ============================================

    /// <summary>
    /// Gets or sets the last time loved tracks were synced from Last.fm.
    /// </summary>
    public DateTime? LastLovedTracksSyncTime { get; set; }

    /// <summary>
    /// Gets or sets the last time play counts were synced from Last.fm.
    /// </summary>
    public DateTime? LastPlayCountSyncTime { get; set; }
}
