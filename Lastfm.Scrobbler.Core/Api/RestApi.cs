// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Threading;
using System.Threading.Tasks;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using Lastfm.Scrobbler.Core.Models.Requests;
using Lastfm.Scrobbler.Core.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Lastfm.Scrobbler.Core.Api
{
    public class RestApi : BaseLastfmApiClient
    {
        public RestApi(ICoreHttpClient httpClient, ILogger<RestApi> logger)
            : base(httpClient, logger)
        {
        }

        public Task<MobileSessionResponse> GetMobileSession(string username, string password, CancellationToken cancellationToken)
        {
            var request = new MobileSessionRequest
            {
                Username = username,
                Password = password
            };

            return Post<MobileSessionRequest, MobileSessionResponse>(request);
        }

        public Task<ScrobbleResponse> Scrobble(Scrobble scrobble, string sessionKey, CancellationToken cancellationToken)
        {
            var request = new ScrobbleRequest
            {
                Artist = scrobble.Artist,
                Track = scrobble.Track,
                Timestamp = scrobble.Timestamp.ToString(),
                Album = scrobble.Album,
                SessionKey = sessionKey
            };

            return Post<ScrobbleRequest, ScrobbleResponse>(request);
        }

        public Task<NowPlayingResponse> NowPlaying(Scrobble scrobble, string sessionKey, CancellationToken cancellationToken)
        {
            var request = new NowPlayingRequest
            {
                Artist = scrobble.Artist,
                Track = scrobble.Track,
                Album = scrobble.Album,
                SessionKey = sessionKey
            };

            return Post<NowPlayingRequest, NowPlayingResponse>(request);
        }

        public Task<LovedTracksResponse> GetLovedTracks(string user, CancellationToken cancellationToken)
        {
            var request = new GetLovedTracksRequest
            {
                User = user
            };

            return Get<GetLovedTracksRequest, LovedTracksResponse>(request);
        }

        public Task<GetTracksResponse> GetTracks(string user, CancellationToken cancellationToken)
        {
            var request = new GetTracksRequest
            {
                User = user
            };

            return Get<GetTracksRequest, GetTracksResponse>(request);
        }

        public Task<BaseResponse> LoveTrack(string track, string artist, string sessionKey, CancellationToken cancellationToken)
        {
            var request = new TrackLoveRequest
            {
                Track = track,
                Artist = artist,
                SessionKey = sessionKey
            };

            return Post<TrackLoveRequest, BaseResponse>(request);
        }

        public Task<BaseResponse> UnloveTrack(string track, string artist, string sessionKey, CancellationToken cancellationToken)
        {
            var request = new TrackLoveRequest
            {
                Track = track,
                Artist = artist,
                SessionKey = sessionKey,
                Love = false
            };

            return Post<TrackLoveRequest, BaseResponse>(request);
        }
    }
}
