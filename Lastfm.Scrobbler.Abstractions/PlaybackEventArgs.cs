// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Abstractions;

/// <summary>
/// Event arguments for playback start events.
/// </summary>
public sealed class PlaybackStartedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the media item being played.
    /// </summary>
    public required MediaItemDto Item { get; init; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets or sets the current playback position in ticks.
    /// </summary>
    public long? PositionTicks { get; init; }
}

/// <summary>
/// Event arguments for playback stop events.
/// </summary>
public sealed class PlaybackStoppedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the media item that was played.
    /// </summary>
    public required MediaItemDto Item { get; init; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets or sets the total played time in ticks.
    /// </summary>
    public long PlayedTicks { get; init; }

    /// <summary>
    /// Gets or sets the playback position when stopped in ticks.
    /// </summary>
    public long? PositionTicks { get; init; }
}
