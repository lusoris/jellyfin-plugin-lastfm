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
}

/// <summary>
/// Per-user options for Last.fm features.
/// </summary>
public class LastfmUserOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether scrobbling is enabled.
    /// </summary>
    public bool ScrobbleEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to sync favorites to loved tracks.
    /// </summary>
    public bool SyncFavorites { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to import loved tracks as favorites.
    /// </summary>
    public bool ImportLovedTracks { get; set; } = true;
}
