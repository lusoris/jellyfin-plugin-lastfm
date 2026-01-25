// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm;

using Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Queue;
using Services;

/// <summary>
/// Registers plugin services with the DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Services
        serviceCollection.AddSingleton<ISignatureGenerator, SignatureGenerator>();
        serviceCollection.AddSingleton<ILastfmApiClient, LastfmApiClient>();
        serviceCollection.AddSingleton<IScrobbleService, ScrobbleService>();
        serviceCollection.AddSingleton<ITrackMatcherService, TrackMatcherService>();
        serviceCollection.AddSingleton<IScrobbleQueue, ScrobbleQueue>();
        serviceCollection.AddSingleton<IPlaylistService, PlaylistService>();

        // Event Handlers (IHostedService)
        serviceCollection.AddHostedService<PlaybackEventHandler>();
        serviceCollection.AddHostedService<UserDataEventHandler>();
    }
}
