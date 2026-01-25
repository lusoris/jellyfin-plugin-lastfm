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
    public partial class LastfmAlbumProvider : IRemoteImageProvider
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Fetching images for {AlbumName}")]
        partial void LogFetchingImages(string albumName);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Album has no MusicBrainz id")]
        partial void LogNoMusicBrainzId();

        [LoggerMessage(Level = LogLevel.Information, Message = "Searching for images for album {Album}")]
        partial void LogSearchingImages(string album);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Artist name is empty for album {Album}, skipping image search")]
        partial void LogEmptyArtistName(string album);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error getting Last.fm images for album")]
        partial void LogImageFetchError(Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error getting images for album {Album}")]
        partial void LogImageError(string album, Exception e);
        private readonly ILogger<LastfmAlbumProvider> _logger;
        private readonly ILastfmApiClient _apiClient;

        public LastfmAlbumProvider(ILogger<LastfmAlbumProvider> logger, ILastfmApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
        }

        public string Name => "Last.fm";

        public bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
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

            var album = (MusicAlbum)item;
            var artistName = album.AlbumArtist;
            var albumName = album.Name;

            if (string.IsNullOrEmpty(artistName))
            {
                LogEmptyArtistName(albumName);
                return new List<RemoteImageInfo>();
            }

            var images = new List<RemoteImageInfo>();

            try
            {
                var albumInfo = await _apiClient.GetAlbumInfo(artistName, albumName, string.Empty, cancellationToken);
                if (albumInfo?.Album != null)
                {
                    var lastfmImages = albumInfo.Album.Images?.Where(i => !string.IsNullOrEmpty(i.Url)).ToList();
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
                LogImageError(albumName, e);
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
