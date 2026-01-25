// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System;
using System.Threading;
using System.Threading.Tasks;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;

namespace Lastfm.Scrobbler.Core
{
    public class ScrobbleHandler
    {
        private readonly ICoreLogger<ScrobbleHandler> _logger;
        private readonly ILastfmApiClient _apiClient;

        public ScrobbleHandler(ICoreLogger<ScrobbleHandler> logger, ILastfmApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
        }

        public bool IsScrobbleEligible(long? duration, long? played)
        {
            if (duration == null || played == null)
            {
                return false;
            }

            var durationTime = TimeSpan.FromTicks(duration.Value);
            if (durationTime.TotalSeconds < 30)
            {
                _logger.LogInformation("Track is too short to be scrobbled");
                return false;
            }

            var playedTime = TimeSpan.FromTicks(played.Value);
            if (playedTime.TotalMinutes < 4 && playedTime.TotalSeconds < durationTime.TotalSeconds / 2)
            {
                _logger.LogInformation("Track was not played long enough to be scrobbled");
                return false;
            }

            return true;
        }

        public async Task Scrobble(Track track, LastfmUser user, CancellationToken cancellationToken = default)
        {
            try
            {
                await _apiClient.Scrobble(track, user, cancellationToken);
                _logger.LogInformation("Successfully scrobbled track: {Track} by {Artist}", track.Name ?? "Unknown", track.Artist ?? "Unknown");
            }
            catch (Exception e)
            {
                _logger.LogError("Error scrobbling track {Track}: {Exception}", track.Name ?? "Unknown", e);
            }
        }
    }
}
