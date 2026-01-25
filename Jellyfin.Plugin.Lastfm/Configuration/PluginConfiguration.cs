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
    public LastfmUser[] LastfmUsers { get; set; } = [];

    /// <summary>
    /// Gets or sets the minimum track duration for scrobbling (seconds).
    /// </summary>
    public int MinimumTrackDuration { get; set; } = 30;

    /// <summary>
    /// Gets or sets the duplicate scrobble detection window (seconds).
    /// </summary>
    public int DuplicateScrobbleWindow { get; set; } = 15;
}
