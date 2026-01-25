using Lastfm.Scrobbler.Core.Models;
using Lastfm.Scrobbler.Core.Models.Requests;
using Lastfm.Scrobbler.Core.Models.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lastfm.Scrobbler.Core.Interfaces;

/// <summary>
/// Interface for Last.fm API communication.
/// </summary>
public interface ILastfmApiClient
{
    /// <summary>
    /// Requests a mobile session token for authentication.
    /// </summary>
    /// <param name="username">The Last.fm username.</param>
    /// <param name="password">The Last.fm password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session response with authentication token.</returns>
    Task<MobileSessionResponse> RequestSession(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrobbles a single track.
    /// </summary>
    /// <param name="item">The track to scrobble.</param>
    /// <param name="user">The Last.fm user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task Scrobble(Track item, LastfmUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrobbles multiple tracks in a batch (max 50 per request).
    /// </summary>
    /// <param name="items">The tracks to scrobble.</param>
    /// <param name="user">The Last.fm user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ScrobbleBatch(IEnumerable<Track> items, LastfmUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates "Now Playing" status.
    /// </summary>
    /// <param name="item">The currently playing track.</param>
    /// <param name="user">The Last.fm user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task NowPlaying(Track item, LastfmUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loves or unloves a track.
    /// </summary>
    /// <param name="item">The track to love/unlove.</param>
    /// <param name="user">The Last.fm user.</param>
    /// <param name="love">True to love, false to unlove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> LoveTrack(Track item, LastfmUser user, bool love = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets loved tracks for a user with pagination.
    /// </summary>
    /// <param name="user">The Last.fm user.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="limit">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loved tracks response.</returns>
    Task<LovedTracksResponse> GetLovedTracks(LastfmUser user, int page, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent tracks for a user with pagination.
    /// </summary>
    /// <param name="user">The Last.fm user.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="limit">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent tracks response.</returns>
    Task<GetTracksResponse> GetRecentTracks(LastfmUser user, int page, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top tracks for a user with pagination.
    /// </summary>
    /// <param name="user">The Last.fm user.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="limit">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Top tracks response.</returns>
    Task<GetTracksResponse> GetTracks(LastfmUser user, int page, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top tracks for an artist.
    /// </summary>
    /// <param name="user">The Last.fm user.</param>
    /// <param name="artist">The artist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tracks response.</returns>
    Task<GetTracksResponse> GetTracks(LastfmUser user, Artist artist, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets album information.
    /// </summary>
    /// <param name="artist">The artist name.</param>
    /// <param name="album">The album name.</param>
    /// <param name="username">Optional username for user-specific data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Album information.</returns>
    Task<AlbumInfoResponse> GetAlbumInfo(string artist, string album, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets artist information.
    /// </summary>
    /// <param name="artist">The artist name.</param>
    /// <param name="username">Optional username for user-specific data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Artist information.</returns>
    Task<ArtistInfoResponse> GetArtistInfo(string artist, string username, CancellationToken cancellationToken = default);
}
