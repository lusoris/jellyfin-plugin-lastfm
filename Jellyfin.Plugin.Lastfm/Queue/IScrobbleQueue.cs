// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Queue;

using Models;

/// <summary>
/// Queue for storing scrobbles when offline.
/// </summary>
public interface IScrobbleQueue
{
    /// <summary>
    /// Adds a scrobble to the queue.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="scrobble">Scrobble information.</param>
    Task EnqueueAsync(Guid userId, ScrobbleInfo scrobble);

    /// <summary>
    /// Gets all pending scrobbles for a user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <returns>List of pending scrobbles.</returns>
    Task<IReadOnlyList<ScrobbleInfo>> GetPendingAsync(Guid userId);

    /// <summary>
    /// Removes scrobbles from the queue after successful submission.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="count">Number of scrobbles to remove from the front.</param>
    Task DequeueAsync(Guid userId, int count);

    /// <summary>
    /// Gets the total number of pending scrobbles across all users.
    /// </summary>
    Task<int> GetTotalPendingCountAsync();
}
