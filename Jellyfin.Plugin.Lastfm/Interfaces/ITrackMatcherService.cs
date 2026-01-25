using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Lastfm.Interfaces
{
    /// <summary>
    /// Service for matching Last.fm tracks to Jellyfin library items.
    /// </summary>
    public interface ITrackMatcherService
    {
        /// <summary>
        /// Finds a matching Jellyfin audio track for the given Last.fm track information.
        /// </summary>
        /// <param name="artist">Artist name.</param>
        /// <param name="track">Track name.</param>
        /// <param name="musicBrainzId">MusicBrainz ID (optional).</param>
        /// <param name="userId">Jellyfin user ID.</param>
        /// <returns>Matching Audio track or null if not found.</returns>
        Task<Audio?> FindMatchingTrackAsync(string artist, string track, string? musicBrainzId, Guid userId);

        /// <summary>
        /// Finds multiple matching tracks in a single batch operation for better performance.
        /// </summary>
        /// <param name="tracks">Collection of tracks to match (Artist, Track, MusicBrainzId).</param>
        /// <param name="userId">User ID for library access.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary mapping "Artist:Track" keys to matched Audio items.</returns>
        Task<IReadOnlyDictionary<string, Audio>> FindMatchingTracksAsync(
            IEnumerable<(string Artist, string Track, string? MusicBrainzId)> tracks,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
