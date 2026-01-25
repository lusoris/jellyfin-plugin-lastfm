// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm;

using Adapters;
using global::Lastfm.Scrobbler.Abstractions;
using global::Lastfm.Scrobbler.Core;
using global::Lastfm.Scrobbler.Core.Api;
using global::Lastfm.Scrobbler.Core.Interfaces;
using global::Lastfm.Scrobbler.Core.Utilities;
using Interfaces;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Providers;
using Services;

/// <summary>
/// Registers plugin services with the DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Adapters (Platform-specific implementations)
        serviceCollection.AddSingleton<IMediaServerAdapter, JellyfinMediaServerAdapter>();
        serviceCollection.AddSingleton<IPlaybackEventProvider, JellyfinPlaybackEventProvider>();
        serviceCollection.AddSingleton<IFavoriteManager, JellyfinFavoriteManager>();
        serviceCollection.AddSingleton<IFavoriteEventProvider>(sp => sp.GetRequiredService<IFavoriteManager>() as IFavoriteEventProvider ?? throw new InvalidOperationException());

        // Core Services
        serviceCollection.AddSingleton<ISignatureGenerator, SignatureGenerator>();
        serviceCollection.AddSingleton(typeof(IScrobbleQueue<,>), typeof(ScrobbleQueue<,>));

        // Plugin Services
        serviceCollection.AddSingleton<Services.ILastfmApiClient, Services.LastfmApiClient>();
        serviceCollection.AddSingleton<IPlaylistService, PlaylistService>();

        // Adapter Services
        serviceCollection.AddSingleton<ITrackMatcherService, TrackMatcherService>();
        serviceCollection.AddSingleton<LibraryCacheService>(); // Performance: Caching layer
        serviceCollection.AddMemoryCache(); // Required for LibraryCacheService

        // Image Providers
        serviceCollection.AddSingleton<IRemoteImageProvider, LastfmArtistProvider>();
        serviceCollection.AddSingleton<IRemoteImageProvider, LastfmAlbumProvider>();
    }
}
