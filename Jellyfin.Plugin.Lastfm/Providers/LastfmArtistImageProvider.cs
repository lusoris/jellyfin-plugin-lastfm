// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Net.Http;
using Lastfm.Scrobbler.Core.Models.Responses;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Lastfm.Services;

namespace Jellyfin.Plugin.Lastfm.Providers;

/// <summary>
/// Image provider for artist images from Last.fm.
/// </summary>
public sealed partial class LastfmArtistImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly ILastfmApiClient _lastfmApiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LastfmArtistImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmArtistImageProvider"/> class.
    /// </summary>
    public LastfmArtistImageProvider(
        ILastfmApiClient lastfmApiClient,
        IHttpClientFactory httpClientFactory,
        ILogger<LastfmArtistImageProvider> logger)
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
    public bool Supports(BaseItem item) => item is MusicArtist;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var artist = item as MusicArtist;
        if (artist == null)
        {
            return [];
        }

        var artistName = artist.Name;
        var mbid = artist.GetProviderId(MetadataProvider.MusicBrainzArtist);

        LogFetchingImages(artistName, mbid ?? "N/A");

        try
        {
            var response = await _lastfmApiClient.GetArtistInfoAsync(
                artist: artistName,
                mbid: mbid,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (response?.Artist?.Images == null || response.Artist.Images.Count == 0)
            {
                LogNoImagesFound(artistName);
                return [];
            }

            var images = new List<RemoteImageInfo>();

            // Get the best quality image (extralarge > mega > large > medium > small)
            var sizes = new[] { "extralarge", "mega", "large", "medium", "small" };
            foreach (var size in sizes)
            {
                var image = response.Artist.Images.FirstOrDefault(i =>
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

                    LogFoundImage(size, artistName, image.Url);
                    break;
                }
            }

            return images;
        }
        catch (Exception ex)
        {
            LogFetchError(ex, artistName);
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
