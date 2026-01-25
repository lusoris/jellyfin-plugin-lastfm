// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Abstractions;

/// <summary>
/// Adapter for media server-specific operations.
/// </summary>
public interface IMediaServerAdapter
{
    /// <summary>
    /// Gets the server type (Jellyfin, Emby, Plex).
    /// </summary>
    string ServerType { get; }

    /// <summary>
    /// Finds a track in the media library by artist and track name.
    /// </summary>
    /// <param name="artist">Artist name.</param>
    /// <param name="track">Track name.</param>
    /// <param name="userId">User ID for library filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching media item, or null if not found.</returns>
    Task<MediaItemDto?> FindTrackAsync(
        string artist,
        string track,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a track by MusicBrainz Recording ID.
    /// </summary>
    /// <param name="musicBrainzId">MusicBrainz Recording ID.</param>
    /// <param name="userId">User ID for library filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching media item, or null if not found.</returns>
    Task<MediaItemDto?> FindTrackByMusicBrainzIdAsync(
        string musicBrainzId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audio tracks for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of media items.</returns>
    Task<IReadOnlyList<MediaItemDto>> GetAllTracksAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
