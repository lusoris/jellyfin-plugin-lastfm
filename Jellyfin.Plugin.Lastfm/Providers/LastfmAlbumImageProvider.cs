// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Providers;

using System.Net.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Models.Responses;
using Services;

/// <summary>
/// Image provider for album images from Last.fm.
/// </summary>
public class LastfmAlbumImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly ILastfmApiClient _lastfmApiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LastfmAlbumImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmAlbumImageProvider"/> class.
    /// </summary>
    public LastfmAlbumImageProvider(
        ILastfmApiClient lastfmApiClient,
        IHttpClientFactory httpClientFactory,
        ILogger<LastfmAlbumImageProvider> logger)
    {
        _lastfmApiClient = lastfmApiClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Last.fm";

    /// <inheritdoc />
    /// After MusicBrainz and AudioDB, but before dynamic providers.
    public int Order => 3;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is MusicAlbum;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var album = item as MusicAlbum;
        if (album == null)
        {
            return [];
        }

        var albumName = album.Name;
        var artistName = album.AlbumArtist ?? album.Artists?.FirstOrDefault();
        var mbid = album.GetProviderId(MetadataProvider.MusicBrainzAlbum)
                   ?? album.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

        if (string.IsNullOrEmpty(albumName) && string.IsNullOrEmpty(mbid))
        {
            _logger.LogDebug("No album name or MBID available for album lookup");
            return [];
        }

        _logger.LogDebug(
            "Fetching Last.fm images for album {AlbumName} by {ArtistName} (MBID: {Mbid})",
            albumName,
            artistName ?? "Unknown",
            mbid ?? "N/A");

        try
        {
            var response = await _lastfmApiClient.GetAlbumInfoAsync(
                artist: artistName,
                album: albumName,
                mbid: mbid,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (response?.Album?.Images == null || response.Album.Images.Count == 0)
            {
                _logger.LogDebug("No images found for album {AlbumName}", albumName);
                return [];
            }

            var images = new List<RemoteImageInfo>();

            // Get the best quality image (extralarge > mega > large > medium > small)
            var sizes = new[] { "extralarge", "mega", "large", "medium", "small" };
            foreach (var size in sizes)
            {
                var image = response.Album.Images.FirstOrDefault(i =>
                    string.Equals(i.Size, size, StringComparison.OrdinalIgnoreCase));

                if (image != null && !string.IsNullOrEmpty(image.Url) && !image.Url.Contains("2a96cbd8b46e442fc41c2b86b821562f"))
                {
                    // Skip the default "no image" placeholder from Last.fm
                    images.Add(new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = image.Url,
                        Type = ImageType.Primary
                    });

                    _logger.LogDebug("Found {Size} image for album {AlbumName}: {Url}", size, albumName, image.Url);
                    break;
                }
            }

            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching images for album {AlbumName}", albumName);
            return [];
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("LastFm");
        return httpClient.GetAsync(new Uri(url), cancellationToken);
    }
}
