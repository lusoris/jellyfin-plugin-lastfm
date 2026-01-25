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
/// Image provider for album images from Last.fm.
/// </summary>
public class LastfmAlbumImageProvider : LastfmImageProviderBase<MusicAlbum>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmAlbumImageProvider"/> class.
    /// </summary>
    public LastfmAlbumImageProvider(
        ILastfmApiClient lastfmApiClient,
        IHttpClientFactory httpClientFactory,
        ILogger<LastfmAlbumImageProvider> logger)
        : base(lastfmApiClient, httpClientFactory, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not MusicAlbum album)
        {
            return [];
        }

        var albumName = album.Name;
        var artistName = album.AlbumArtist ?? album.Artists?.FirstOrDefault();
        var mbid = album.GetProviderId(MetadataProvider.MusicBrainzAlbum)
                   ?? album.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

        if (string.IsNullOrEmpty(albumName) && string.IsNullOrEmpty(mbid))
        {
            Logger.LogDebug("No album name or MBID available for album lookup");
            return [];
        }

        Logger.LogDebug(
            "Fetching Last.fm images for album {AlbumName} by {ArtistName} (MBID: {Mbid})",
            albumName,
            artistName ?? "Unknown",
            mbid ?? "N/A");

        try
        {
            var response = await LastfmApiClient.GetAlbumInfoAsync(
                artist: artistName,
                album: albumName,
                mbid: mbid,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ExtractBestImage(response?.Album?.Images, albumName ?? "Unknown Album");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching images for album {AlbumName}", albumName);
            return [];
        }
    }
}
