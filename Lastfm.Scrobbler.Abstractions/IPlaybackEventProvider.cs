// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Abstractions;

/// <summary>
/// Provides playback events from the media server.
/// </summary>
public interface IPlaybackEventProvider
{
    /// <summary>
    /// Occurs when playback starts.
    /// </summary>
    event EventHandler<PlaybackStartedEventArgs>? PlaybackStarted;

    /// <summary>
    /// Occurs when playback stops.
    /// </summary>
    event EventHandler<PlaybackStoppedEventArgs>? PlaybackStopped;
}
