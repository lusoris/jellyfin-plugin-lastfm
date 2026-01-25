// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Providers;

using System.Net.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Services;

/// <summary>
/// Image provider for artist images from Last.fm.
/// </summary>
public class LastfmArtistImageProvider : LastfmImageProviderBase<MusicArtist>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmArtistImageProvider"/> class.
    /// </summary>
    public LastfmArtistImageProvider(
        ILastfmApiClient lastfmApiClient,
        IHttpClientFactory httpClientFactory,
        ILogger<LastfmArtistImageProvider> logger)
        : base(lastfmApiClient, httpClientFactory, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not MusicArtist artist)
        {
            return [];
        }

        var artistName = artist.Name;
        var mbid = artist.GetProviderId(MetadataProvider.MusicBrainzArtist);

        Logger.LogDebug("Fetching Last.fm images for artist {ArtistName} (MBID: {Mbid})", artistName, mbid ?? "N/A");

        try
        {
            var response = await LastfmApiClient.GetArtistInfoAsync(
                artist: artistName,
                mbid: mbid,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ExtractBestImage(response?.Artist?.Images, artistName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching images for artist {ArtistName}", artistName);
            return [];
        }
    }
}
