using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lastfm.Scrobbler.Core.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm.Providers
{
    public partial class LastfmArtistProvider : IRemoteImageProvider
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Searching for images for artist {Artist}")]
        partial void LogSearchingImages(string artist);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error getting images for artist {Artist}")]
        partial void LogImageError(string artist, Exception e);
        private readonly ILogger<LastfmArtistProvider> _logger;
        private readonly ILastfmApiClient _apiClient;

        public LastfmArtistProvider(ILogger<LastfmArtistProvider> logger, ILastfmApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
        }

        public string Name => "Last.fm";

        public bool Supports(BaseItem item)
        {
            return item is MusicArtist;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[]
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Banner,
                ImageType.Thumb
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            LogSearchingImages(item.Name);

            var artist = (MusicArtist)item;
            var artistName = artist.Name;

            var images = new List<RemoteImageInfo>();

            try
            {
                var artistInfo = await _apiClient.GetArtistInfo(artistName, string.Empty, cancellationToken);
                if (artistInfo?.Artist != null)
                {
                    var lastfmImages = artistInfo.Artist.Images?.Where(i => !string.IsNullOrEmpty(i.Url)).ToList();
                    if (lastfmImages != null)
                    {
                        images.AddRange(lastfmImages.Select(i => new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = i.Url,
                        Type = i.Size switch
                        {
                            "mega" => ImageType.Primary,
                            "extralarge" => ImageType.Backdrop,
                            "large" => ImageType.Banner,
                            "medium" => ImageType.Thumb,
                            _ => ImageType.Primary
                        }
                    }));
                    }
                }
            }
            catch (Exception e)
            {
                LogImageError(artistName, e);
            }

            return images;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient();
            return httpClient.GetAsync(url, cancellationToken);
        }
    }
}
