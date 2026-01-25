// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Providers;

using System.Net.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Models.Responses;
using Services;

/// <summary>
/// Base class for Last.fm image providers with shared functionality.
/// </summary>
/// <typeparam name="TItem">The type of item this provider supports.</typeparam>
public abstract class LastfmImageProviderBase<TItem> : IRemoteImageProvider, IHasOrder
    where TItem : BaseItem
{
    /// <summary>
    /// Placeholder image hash used by Last.fm for items without images.
    /// </summary>
    protected const string NoImagePlaceholder = "2a96cbd8b46e442fc41c2b86b821562f";

    /// <summary>
    /// Image size priority order (best quality first).
    /// </summary>
    private static readonly string[] SizePriority = ["extralarge", "mega", "large", "medium", "small"];

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmImageProviderBase{TItem}"/> class.
    /// </summary>
    protected LastfmImageProviderBase(
        ILastfmApiClient lastfmApiClient,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        LastfmApiClient = lastfmApiClient;
        _httpClientFactory = httpClientFactory;
        Logger = logger;
    }

    /// <summary>
    /// Gets the Last.fm API client.
    /// </summary>
    protected ILastfmApiClient LastfmApiClient { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public string Name => "Last.fm";

    /// <inheritdoc />
    /// After MusicBrainz and AudioDB, but before dynamic providers.
    public int Order => 3;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is TItem;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    /// <inheritdoc />
    public abstract Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken);

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("LastFm");
        return httpClient.GetAsync(new Uri(url), cancellationToken);
    }

    /// <summary>
    /// Extracts the best quality image from a list of Last.fm images.
    /// </summary>
    /// <param name="images">The list of images from Last.fm.</param>
    /// <param name="itemName">The item name for logging.</param>
    /// <returns>A list of remote image info (0-1 items).</returns>
    protected List<RemoteImageInfo> ExtractBestImage(List<LastfmImage>? images, string itemName)
    {
        var result = new List<RemoteImageInfo>();

        if (images == null || images.Count == 0)
        {
            Logger.LogDebug("No images found for {ItemName}", itemName);
            return result;
        }

        foreach (var size in SizePriority)
        {
            var image = images.FirstOrDefault(i =>
                string.Equals(i.Size, size, StringComparison.OrdinalIgnoreCase));

            if (image != null && !string.IsNullOrEmpty(image.Url) && !image.Url.Contains(NoImagePlaceholder))
            {
                result.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = image.Url,
                    Type = ImageType.Primary
                });

                Logger.LogDebug("Found {Size} image for {ItemName}: {Url}", size, itemName, image.Url);
                break;
            }
        }

        return result;
    }
}
