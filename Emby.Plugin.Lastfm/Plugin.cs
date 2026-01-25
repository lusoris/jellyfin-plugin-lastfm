using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Emby.Plugin.Lastfm.Configuration;

namespace Emby.Plugin.Lastfm;

/// <summary>
/// Last.fm plugin for Emby Server.
/// Provides scrobbling, loved tracks sync, and smart playlists.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Plugin unique identifier.
    /// </summary>
    public override Guid Id => new Guid("5e7fe7f0-b048-429e-a431-b1a7e69c931d");

    /// <summary>
    /// Plugin name displayed in Emby.
    /// </summary>
    public override string Name => "Last.fm Scrobbler";

    /// <summary>
    /// Plugin description.
    /// </summary>
    public override string Description => "Scrobble music to Last.fm, sync loved tracks, and generate smart playlists";

    /// <summary>
    /// Singleton instance for configuration access.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the plugin's web UI pages.
    /// </summary>
    /// <returns>Collection of plugin page info.</returns>
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
            }
        };
    }
}
