// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using Models;
using Models.Responses;

/// <summary>
/// Client for the Last.fm API.
/// </summary>
public interface ILastfmApiClient
{
    /// <summary>
    /// Authenticates a user and retrieves a session key.
    /// </summary>
    /// <param name="username">Last.fm username.</param>
    /// <param name="password">Last.fm password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session response with session key.</returns>
    Task<MobileSessionResponse?> GetMobileSessionAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrobbles a track.
    /// </summary>
    Task<ScrobbleResponse?> ScrobbleAsync(ScrobbleInfo scrobble, string sessionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrobbles multiple tracks in a batch (up to 50).
    /// </summary>
    /// <param name="scrobbles">Tracks to scrobble (max 50).</param>
    /// <param name="sessionKey">User session key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scrobble response with accepted/ignored counts.</returns>
    Task<ScrobbleResponse?> ScrobbleBatchAsync(IReadOnlyList<ScrobbleInfo> scrobbles, string sessionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the now playing track.
    /// </summary>
    Task<bool> UpdateNowPlayingAsync(ScrobbleInfo scrobble, string sessionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loves a track.
    /// </summary>
    Task<bool> LoveTrackAsync(string artist, string track, string sessionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloves a track.
    /// </summary>
    Task<bool> UnloveTrackAsync(string artist, string track, string sessionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's loved tracks.
    /// </summary>
    Task<LovedTracksResponse?> GetLovedTracksAsync(string username, int page = 1, int limit = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's top tracks with play counts.
    /// </summary>
    /// <param name="username">Last.fm username.</param>
    /// <param name="period">Time period: overall, 7day, 1month, 3month, 6month, 12month.</param>
    /// <param name="page">Page number.</param>
    /// <param name="limit">Results per page (max 1000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TopTracksResponse?> GetTopTracksAsync(string username, string period = "overall", int page = 1, int limit = 1000, CancellationToken cancellationToken = default);
}
