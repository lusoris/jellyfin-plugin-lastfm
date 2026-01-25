// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using MediaBrowser.Controller.Entities.Audio;
using Models;

/// <summary>
/// Matches Jellyfin tracks to Last.fm tracks.
/// </summary>
public interface ITrackMatcherService
{
    /// <summary>
    /// Attempts to find a matching Jellyfin track for a Last.fm track.
    /// </summary>
    /// <param name="lastfmArtist">Artist name from Last.fm.</param>
    /// <param name="lastfmTrack">Track name from Last.fm.</param>
    /// <param name="lastfmMbid">MusicBrainz ID from Last.fm (optional).</param>
    /// <param name="userId">Jellyfin user ID for library access.</param>
    /// <returns>Matching Audio item or null.</returns>
    Task<Audio?> FindMatchingTrackAsync(string lastfmArtist, string lastfmTrack, string? lastfmMbid, Guid userId);

    /// <summary>
    /// Checks if two strings are "like" each other (fuzzy match).
    /// </summary>
    /// <param name="source">Source string.</param>
    /// <param name="target">Target string.</param>
    /// <returns>True if strings are similar.</returns>
    bool IsLike(string source, string target);
}
