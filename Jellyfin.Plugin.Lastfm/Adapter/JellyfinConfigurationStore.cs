// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using Lastfm.Scrobbler.Core.Interfaces;
using Jellyfin.Plugin.Lastfm.Configuration;
using Microsoft.Extensions.Logging;
#pragma warning disable CA1848 // Use LoggerMessage delegates - simple configuration wrapper
namespace Jellyfin.Plugin.Lastfm.Adapter;

/// <summary>
/// An adapter to bridge the core configuration store with Jellyfin's configuration manager.
/// </summary>
public class JellyfinConfigurationStore : IConfigurationStore
{
    private readonly ILogger<JellyfinConfigurationStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinConfigurationStore"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public JellyfinConfigurationStore(ILogger<JellyfinConfigurationStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IPluginConfiguration Configuration => Plugin.Instance?.Configuration ?? throw new InvalidOperationException("Plugin instance not initialized");

    /// <inheritdoc />
    public void Save()
    {
        if (Plugin.Instance == null)
        {
            throw new InvalidOperationException("Plugin instance not initialized");
        }

        _logger.LogInformation("Saving Last.fm configuration");
        Plugin.Instance.UpdateConfiguration(Plugin.Instance.Configuration);
    }
}
