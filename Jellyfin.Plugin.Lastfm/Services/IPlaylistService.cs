// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

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

/// <summary>
/// Result of a playlist creation operation.
/// </summary>
public class PlaylistResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the created playlist ID.
    /// </summary>
    public Guid? PlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the playlist name.
    /// </summary>
    public string? PlaylistName { get; set; }

    /// <summary>
    /// Gets or sets the number of tracks added.
    /// </summary>
    public int TracksAdded { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PlaylistResult SuccessResult(Guid playlistId, string playlistName, int tracksAdded)
    {
        return new PlaylistResult
        {
            Success = true,
            PlaylistId = playlistId,
            PlaylistName = playlistName,
            TracksAdded = tracksAdded
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static PlaylistResult FailureResult(string error)
    {
        return new PlaylistResult
        {
            Success = false,
            Error = error
        };
    }
}
