// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Abstractions;

/// <summary>
/// Manages favorite/loved track status.
/// </summary>
public interface IFavoriteManager
{
    /// <summary>
    /// Gets all favorite tracks for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of favorite media items.</returns>
    Task<IReadOnlyList<MediaItemDto>> GetFavoriteTracksAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a track as favorite.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="itemId">Media item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetFavoriteAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes favorite status from a track.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="itemId">Media item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnsetFavoriteAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default);
}
