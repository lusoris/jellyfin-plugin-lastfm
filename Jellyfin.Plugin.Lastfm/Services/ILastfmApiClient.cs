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

    /// <summary>
    /// Gets artists similar to the specified artist.
    /// </summary>
    /// <param name="artist">Artist name.</param>
    /// <param name="limit">Max number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SimilarArtistsResponse?> GetSimilarArtistsAsync(string artist, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tracks similar to the specified track.
    /// </summary>
    /// <param name="artist">Artist name.</param>
    /// <param name="track">Track name.</param>
    /// <param name="limit">Max number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SimilarTracksResponse?> GetSimilarTracksAsync(string artist, string track, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets artist information including images and bio.
    /// </summary>
    /// <param name="artist">Artist name.</param>
    /// <param name="mbid">MusicBrainz ID (optional, preferred over name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ArtistInfoResponse?> GetArtistInfoAsync(string? artist = null, string? mbid = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets album information including images.
    /// </summary>
    /// <param name="artist">Artist name.</param>
    /// <param name="album">Album name.</param>
    /// <param name="mbid">MusicBrainz ID (optional, preferred over name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AlbumInfoResponse?> GetAlbumInfoAsync(string? artist = null, string? album = null, string? mbid = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's weekly track chart.
    /// </summary>
    /// <param name="username">Last.fm username.</param>
    /// <param name="from">Unix timestamp for start of week (optional).</param>
    /// <param name="to">Unix timestamp for end of week (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<WeeklyTrackChartResponse?> GetWeeklyTrackChartAsync(string username, long? from = null, long? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's top tags.
    /// </summary>
    /// <param name="username">Last.fm username.</param>
    /// <param name="limit">Max number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<UserTopTagsResponse?> GetUserTopTagsAsync(string username, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top tracks for a tag.
    /// </summary>
    /// <param name="tag">Tag name.</param>
    /// <param name="limit">Max number of results.</param>
    /// <param name="page">Page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TagTopTracksResponse?> GetTagTopTracksAsync(string tag, int limit = 50, int page = 1, CancellationToken cancellationToken = default);
}
