// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using Lastfm.Scrobbler.Core.Models.Requests;
using Lastfm.Scrobbler.Core.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Lastfm.Scrobbler.Core.Api
{
    public class LastfmApiClient : BaseLastfmApiClient, ILastfmApiClient
    {
        public LastfmApiClient(ICoreHttpClient httpClient, ILogger<LastfmApiClient> logger)
            : base(httpClient, logger)
        {
        }

        public Task<MobileSessionResponse> RequestSession(string username, string password, CancellationToken cancellationToken = default)
        {
            var request = new MobileSessionRequest
            {
                Username = username,
                Password = password
            };

            return Post<MobileSessionRequest, MobileSessionResponse>(request, cancellationToken);
        }

        public Task Scrobble(Track item, LastfmUser user, CancellationToken cancellationToken = default)
        {
            var request = new ScrobbleRequest
            {
                Artist = item.Artist,
                Track = item.Name,
                Timestamp = item.Timestamp.ToString(),
                Album = item.Album,
                SessionKey = user.SessionKey
            };

            return Post<ScrobbleRequest, ScrobbleResponse>(request, cancellationToken);
        }

        public async Task ScrobbleBatch(IEnumerable<Track> items, LastfmUser user, CancellationToken cancellationToken = default)
        {
            // Last.fm supports max 50 scrobbles per request
            var tracks = items.ToList();
            for (int i = 0; i < tracks.Count; i += 50)
            {
                var batch = tracks.Skip(i).Take(50);
                foreach (var track in batch)
                {
                    await Scrobble(track, user, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public Task NowPlaying(Track item, LastfmUser user, CancellationToken cancellationToken = default)
        {
            var request = new NowPlayingRequest
            {
                Artist = item.Artist,
                Track = item.Name,
                Album = item.Album,
                SessionKey = user.SessionKey
            };

            return Post<NowPlayingRequest, NowPlayingResponse>(request, cancellationToken);
        }

        public async Task<bool> LoveTrack(Track item, LastfmUser user, bool love = true, CancellationToken cancellationToken = default)
        {
            var request = new TrackLoveRequest
            {
                Track = item.Name,
                Artist = item.Artist,
                SessionKey = user.SessionKey,
                Love = love
            };

            var response = await Post<TrackLoveRequest, BaseResponse>(request, cancellationToken).ConfigureAwait(false);
            return response != null;
        }

        public Task<LovedTracksResponse> GetLovedTracks(LastfmUser user, int page, int limit, CancellationToken cancellationToken = default)
        {
            var request = new GetLovedTracksRequest
            {
                User = user.Username,
                Page = page,
                Limit = limit
            };

            return Get<GetLovedTracksRequest, LovedTracksResponse>(request, cancellationToken);
        }

        public Task<GetTracksResponse> GetRecentTracks(LastfmUser user, int page, int limit, CancellationToken cancellationToken = default)
        {
            var request = new GetTracksRequest
            {
                User = user.Username,
                Page = page,
                Limit = limit
            };

            return Get<GetTracksRequest, GetTracksResponse>(request, cancellationToken);
        }

        public Task<GetTracksResponse> GetTracks(LastfmUser user, int page, int limit, CancellationToken cancellationToken = default)
        {
            var request = new GetTracksRequest
            {
                User = user.Username,
                Page = page,
                Limit = limit
            };

            return Get<GetTracksRequest, GetTracksResponse>(request, cancellationToken);
        }

        public Task<GetTracksResponse> GetTracks(LastfmUser user, Artist artist, CancellationToken cancellationToken = default)
        {
            var request = new GetTracksRequest
            {
                User = user.Username,
                Artist = artist.Name
            };

            return Get<GetTracksRequest, GetTracksResponse>(request, cancellationToken);
        }

        public Task<AlbumInfoResponse> GetAlbumInfo(string artist, string album, string username, CancellationToken cancellationToken = default)
        {
            var request = new AlbumInfoRequest
            {
                Artist = artist,
                Album = album,
                Username = username
            };

            return Get<AlbumInfoRequest, AlbumInfoResponse>(request, cancellationToken);
        }

        public Task<ArtistInfoResponse> GetArtistInfo(string artist, string username, CancellationToken cancellationToken = default)
        {
            var request = new ArtistInfoRequest
            {
                Artist = artist,
                Username = username
            };

            return Get<ArtistInfoRequest, ArtistInfoResponse>(request, cancellationToken);
        }
    }
}
