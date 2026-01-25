// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Interfaces;

/// <summary>
/// Interface for a service that stores and retrieves plugin configuration.
/// </summary>
public interface IConfigurationStore
{
    /// <summary>
    /// Gets the plugin configuration.
    /// </summary>
    IPluginConfiguration Configuration { get; }

    /// <summary>
    /// Saves the plugin configuration.
    /// </summary>
    void Save();
}
