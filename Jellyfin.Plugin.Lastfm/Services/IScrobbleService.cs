// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using Models;

/// <summary>
/// Validates and manages scrobbles.
/// </summary>
public interface IScrobbleService
{
    /// <summary>
    /// Determines if a track is eligible for scrobbling.
    /// </summary>
    /// <param name="trackLengthTicks">Total track length in ticks.</param>
    /// <param name="playedTicks">Amount played in ticks.</param>
    /// <returns>True if eligible for scrobbling.</returns>
    bool IsScrobbleEligible(long trackLengthTicks, long playedTicks);

    /// <summary>
    /// Checks if this scrobble is a duplicate of a recent scrobble.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="trackId">Track item ID.</param>
    /// <returns>True if this is a duplicate.</returns>
    bool IsDuplicateScrobble(Guid userId, Guid trackId);

    /// <summary>
    /// Records a scrobble to prevent duplicates.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="trackId">Track item ID.</param>
    void RecordScrobble(Guid userId, Guid trackId);
}
