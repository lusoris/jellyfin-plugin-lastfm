// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using Jellyfin.Plugin.Lastfm.Models;

namespace Jellyfin.Plugin.Lastfm.Services;

using Models;

/// <summary>
/// Service for creating and managing Last.fm-based playlists.
/// </summary>
public interface IPlaylistService
{
    /// <summary>
    /// Creates a playlist based on similar artists to the user's top artists.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="playlistName">Name for the playlist.</param>
    /// <param name="maxTracks">Maximum number of tracks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Playlist creation result.</returns>
    Task<PlaylistResult> CreateSimilarArtistsPlaylistAsync(
        Guid userId,
        string playlistName,
        int maxTracks = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a playlist based on similar tracks to the user's top tracks.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="playlistName">Name for the playlist.</param>
    /// <param name="maxTracks">Maximum number of tracks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Playlist creation result.</returns>
    Task<PlaylistResult> CreateSimilarTracksPlaylistAsync(
        Guid userId,
        string playlistName,
        int maxTracks = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a playlist to rediscover old favorites.
    /// Finds tracks from artists the user loved but hasn't played recently.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="playlistName">Name for the playlist.</param>
    /// <param name="maxTracks">Maximum number of tracks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Playlist creation result.</returns>
    Task<PlaylistResult> CreateRediscoverFavoritesPlaylistAsync(
        Guid userId,
        string playlistName,
        int maxTracks = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a weekly mixtape playlist based on recent listening history.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="playlistName">Name for the playlist.</param>
    /// <param name="maxTracks">Maximum number of tracks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Playlist creation result.</returns>
    Task<PlaylistResult> CreateWeeklyMixtapeAsync(
        Guid userId,
        string playlistName,
        int maxTracks = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a tag discovery playlist based on user's favorite tags.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="playlistName">Name for the playlist.</param>
    /// <param name="maxTracks">Maximum number of tracks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Playlist creation result.</returns>
    Task<PlaylistResult> CreateTagDiscoveryPlaylistAsync(
        Guid userId,
        string playlistName,
        int maxTracks = 50,
        CancellationToken cancellationToken = default);
}
