using System;
using MediaBrowser.Model.Plugins;
using Lastfm.Scrobbler.Core.Models;

namespace Emby.Plugin.Lastfm.Configuration;

/// <summary>
/// Plugin configuration for Last.fm settings.
/// Same structure as Jellyfin version for compatibility.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the list of Last.fm users configured for this Emby instance.
    /// </summary>
#pragma warning disable CA1825 // Avoid zero-length array allocations - .NET Standard 2.0 compatibility
    public LastfmUser[] LastfmUsers { get; set; } = new LastfmUser[0];
#pragma warning restore CA1825
}
