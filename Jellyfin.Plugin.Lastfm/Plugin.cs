// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm;

using System;
using System.Collections.Generic;
using Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

/// <summary>
/// Last.fm scrobbler plugin for Jellyfin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Plugin GUID - do not change.
    /// </summary>
    public static readonly Guid PluginGuid = new("5e7fe7f0-b048-429e-a431-b1a7e69c930d");

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

    /// <inheritdoc />
    public override Guid Id => PluginGuid;

    /// <inheritdoc />
    public override string Name => "Last.fm";

    /// <inheritdoc />
    public override string Description => "Scrobble your music to Last.fm and sync loved tracks";

    /// <summary>
    /// Gets the plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        // Configuration page
        yield return new PluginPageInfo
        {
            Name = "Lastfm",
            EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
        };

        // Recommendations page (appears in menu)
        yield return new PluginPageInfo
        {
            Name = "LastfmRecommendations",
            DisplayName = "Last.fm Recommendations",
            EmbeddedResourcePath = $"{GetType().Namespace}.Pages.recommendations.html",
            EnableInMainMenu = true,
            MenuSection = "music",
            MenuIcon = "auto_awesome"
        };

        // Statistics page (appears in menu)
        yield return new PluginPageInfo
        {
            Name = "LastfmStats",
            DisplayName = "Last.fm Statistics",
            EmbeddedResourcePath = $"{GetType().Namespace}.Pages.statistics.html",
            EnableInMainMenu = true,
            MenuSection = "music",
            MenuIcon = "insert_chart"
        };
    }
}
