// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Abstractions;

/// <summary>
/// Event arguments for favorite status changes.
/// </summary>
public sealed class FavoriteChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the media item.
    /// </summary>
    public required MediaItemDto Item { get; init; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets whether the item is now a favorite.
    /// </summary>
    public required bool IsFavorite { get; init; }
}
