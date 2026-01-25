// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using Lastfm.Scrobbler.Core.Models;

namespace Lastfm.Scrobbler.Core.Interfaces;

/// <summary>
/// Interface for the plugin configuration.
/// </summary>
public interface IPluginConfiguration
{
    /// <summary>
    /// Gets or sets the Last.fm users.
    /// </summary>
    List<LastfmUser> LastfmUsers { get; set; }
}
