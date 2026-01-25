using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lastfm.Scrobbler.Core.Interfaces;

/// <summary>
/// Interface for scrobbling service handling playback events.
/// </summary>
/// <typeparam name="TUserId">The user identifier type.</typeparam>
/// <typeparam name="TTrack">The track type.</typeparam>
public interface IScrobbleService<TUserId, TTrack>
{
    /// <summary>
    /// Handles playback start event.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="track">The track that started playing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task OnPlaybackStart(TUserId userId, TTrack track, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles playback stopped event.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="track">The track that stopped playing.</param>
    /// <param name="positionTicks">The playback position in ticks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task OnPlaybackStopped(TUserId userId, TTrack track, long positionTicks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles user data saved event (e.g., favorite changes).
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="track">The track whose data was saved.</param>
    /// <param name="isFavorite">Whether the track is marked as favorite.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task OnUserDataSaved(TUserId userId, TTrack track, bool isFavorite, CancellationToken cancellationToken = default);
}
