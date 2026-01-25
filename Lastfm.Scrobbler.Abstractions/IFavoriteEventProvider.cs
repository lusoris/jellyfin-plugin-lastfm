// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Abstractions;

/// <summary>
/// Provides favorite status change events.
/// </summary>
public interface IFavoriteEventProvider
{
    /// <summary>
    /// Occurs when a track's favorite status changes.
    /// </summary>
    event EventHandler<FavoriteChangedEventArgs>? FavoriteChanged;
}
